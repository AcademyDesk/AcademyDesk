using System.Text.Json;
using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Tests;

public sealed class BatchPromotionDecisionTests
{
    [Theory]
    [InlineData("Approved", 200)]
    [InlineData("Rejected", 200)]
    [InlineData("Pending", 400)]
    [InlineData("", 400)]
    public async Task Pending_decision_has_expected_transition(string decision, int expected)
    {
        var options = Options(); await using var db = new AcademyDeskDbContext(options); var fixture = await Seed(db);
        var before = await State(db); var result = await new BatchPromotionsController(db).Decide(fixture.Academy, fixture.Promotion.Id, new(decision, " Decision note "), CancellationToken.None);
        Assert.Equal(expected, Status(result)); await using var fresh = new AcademyDeskDbContext(options); var after = await State(fresh);
        if (expected != 200) Assert.Equal(before, after);
        else if (decision == "Rejected")
        {
            Assert.Equal("Rejected", (await fresh.BatchPromotions.SingleAsync()).Status); Assert.Equal("Decision note", (await fresh.BatchPromotions.SingleAsync()).Notes);
            Assert.Single(await fresh.Enrollments.ToListAsync()); Assert.Equal("Active", (await fresh.Enrollments.SingleAsync()).Status);
        }
        else
        {
            var rows = await fresh.Enrollments.OrderBy(x => x.Id).ToListAsync(); Assert.Equal(2, rows.Count);
            Assert.Equal("Completed", rows.Single(x => x.Id == fixture.SourceEnrollment.Id).Status); Assert.Equal(fixture.Promotion.EffectiveDate, rows.Single(x => x.Id == fixture.SourceEnrollment.Id).EndDate);
            var target = rows.Single(x => x.BatchId == fixture.Target.Id); Assert.Equal("Active", target.Status); Assert.Equal(fixture.Promotion.EffectiveDate, target.StartDate);
            Assert.Equal("Approved", (await fresh.BatchPromotions.SingleAsync()).Status); Assert.Equal("Decision note", (await fresh.BatchPromotions.SingleAsync()).Notes);
        }
    }

    [Theory]
    [InlineData("Approved", "Rejected")]
    [InlineData("Rejected", "Approved")]
    [InlineData("Approved", "Approved")]
    [InlineData("Rejected", "Rejected")]
    public async Task Terminal_decision_is_not_rewritten_or_replayed(string first, string second)
    {
        var options = Options(); await using var db = new AcademyDeskDbContext(options); var fixture = await Seed(db);
        Assert.Equal(200, Status(await new BatchPromotionsController(db).Decide(fixture.Academy, fixture.Promotion.Id, new(first, "First"), CancellationToken.None)));
        await using var beforeDb = new AcademyDeskDbContext(options); var before = await State(beforeDb);
        var retry = await new BatchPromotionsController(db).Decide(fixture.Academy, fixture.Promotion.Id, new(second, "Second"), CancellationToken.None);
        Assert.Equal(409, Status(retry)); await using var afterDb = new AcademyDeskDbContext(options); Assert.Equal(before, await State(afterDb));
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("foreign")]
    public async Task Missing_or_foreign_promotion_returns_not_found_without_writes(string mode)
    {
        var options = Options(); await using var db = new AcademyDeskDbContext(options); var fixture = await Seed(db); var before = await State(db);
        var result = await new BatchPromotionsController(db).Decide(mode == "foreign" ? Guid.NewGuid() : fixture.Academy, mode == "missing" ? Guid.NewGuid() : fixture.Promotion.Id, new("Approved", null), CancellationToken.None);
        Assert.Equal(404, Status(result)); await using var fresh = new AcademyDeskDbContext(options); Assert.Equal(before, await State(fresh));
    }

    [Theory]
    [InlineData("source-missing")]
    [InlineData("target-active")]
    public async Task Inconsistent_pending_state_is_conflict_without_writes(string mode)
    {
        var options = Options(); await using var db = new AcademyDeskDbContext(options); var fixture = await Seed(db);
        if (mode == "source-missing") (await db.Enrollments.SingleAsync()).Status = "Completed";
        else db.Enrollments.Add(new Enrollment { AcademyId = fixture.Academy, StudentId = fixture.Student, BatchId = fixture.Target.Id, Status = "Active" });
        await db.SaveChangesAsync(); var before = await State(db);
        var result = await new BatchPromotionsController(db).Decide(fixture.Academy, fixture.Promotion.Id, new("Approved", null), CancellationToken.None);
        Assert.Equal(409, Status(result)); await using var fresh = new AcademyDeskDbContext(options); Assert.Equal(before, await State(fresh));
    }

    [Fact]
    public void Decision_only_uses_domain_audit_boundary()
    {
        var action = new ControllerActionDescriptor { MethodInfo = typeof(BatchPromotionsController).GetMethod(nameof(BatchPromotionsController.Decide))! };
        Assert.True(AcademyAccessFilter.UsesAtomicBoundary(nameof(BatchPromotionsController), action)); Assert.False(AcademyAccessFilter.IncludesIdentity(action));
        Assert.Null(Attribute.GetCustomAttribute(typeof(BatchPromotionsController).GetMethod(nameof(BatchPromotionsController.Create))!, typeof(AtomicAcademyMutationAttribute)));
    }

    private static async Task<(Guid Academy, Guid Student, Batch Source, Batch Target, Enrollment SourceEnrollment, BatchPromotion Promotion)> Seed(AcademyDeskDbContext db)
    {
        var academy = Guid.NewGuid(); var student = Guid.NewGuid(); var source = new Batch { AcademyId = academy, Name = "Source", CourseId = Guid.NewGuid() }; var target = new Batch { AcademyId = academy, Name = "Target", CourseId = Guid.NewGuid() };
        var enrollment = new Enrollment { AcademyId = academy, StudentId = student, BatchId = source.Id, Status = "Active" }; var promotion = new BatchPromotion { AcademyId = academy, StudentId = student, SourceBatchId = source.Id, TargetBatchId = target.Id, EffectiveDate = new DateOnly(2026, 9, 1), Notes = "Original" };
        db.AddRange(source, target, enrollment, promotion); await db.SaveChangesAsync(); return (academy, student, source, target, enrollment, promotion);
    }
    private static DbContextOptions<AcademyDeskDbContext> Options() => new DbContextOptionsBuilder<AcademyDeskDbContext>().UseInMemoryDatabase("promotion-" + Guid.NewGuid()).Options;
    private static int Status(ActionResult result) => result switch { StatusCodeResult r => r.StatusCode, ObjectResult r => r.StatusCode ?? 200, _ => throw new InvalidOperationException(result.GetType().Name) };
    private static async Task<string> State(AcademyDeskDbContext db) => JsonSerializer.Serialize(new { Promotions = await db.BatchPromotions.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), Enrollments = await db.Enrollments.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), Batches = await db.Batches.AsNoTracking().OrderBy(x => x.Id).ToListAsync() });
}
