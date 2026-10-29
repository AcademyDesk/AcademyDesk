using System.Text.Json;
using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Tests;

public sealed class EnrollmentAdmissionTests
{
    private static AcademyDeskDbContext Store() => new(new DbContextOptionsBuilder<AcademyDeskDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    private static async Task<(Guid Academy, Batch Batch, Enrollment Target)> Seed(AcademyDeskDbContext db, string status = "Waitlisted")
    {
        var academy = Guid.NewGuid(); var batch = new Batch { AcademyId = academy, Name = "Synthetic", CourseId = Guid.NewGuid(), Capacity = 1 };
        var target = new Enrollment { AcademyId = academy, BatchId = batch.Id, StudentId = Guid.NewGuid(), Status = status };
        db.AddRange(batch, target); await db.SaveChangesAsync(); return (academy, batch, target);
    }
    private static async Task<string> State(AcademyDeskDbContext db) => JsonSerializer.Serialize(new {
        Batches = await db.Batches.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
        Enrollments = await db.Enrollments.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
        Promotions = await db.BatchPromotions.AsNoTracking().OrderBy(x => x.Id).ToListAsync()
    });

    [Theory]
    [InlineData("Waitlisted")]
    [InlineData("Paused")]
    [InlineData("Completed")]
    [InlineData("Withdrawn")]
    [InlineData("Cancelled")]
    public async Task Activation_into_full_batch_is_conflict_and_persists_nothing(string status)
    {
        await using var db = Store(); var f = await Seed(db, status);
        db.Enrollments.Add(new Enrollment { AcademyId = f.Academy, BatchId = f.Batch.Id, StudentId = Guid.NewGuid(), Status = "Active" }); await db.SaveChangesAsync();
        var before = await State(db);
        var result = await new EnrollmentsController(db).Update(f.Academy, f.Target.Id, new("Active", null, null), default);
        Assert.IsType<ConflictObjectResult>(result.Result); Assert.Equal(before, await State(db));
    }

    [Theory]
    [InlineData("active")]
    [InlineData(" Active ")]
    public async Task Valid_activation_canonicalizes_status_and_keeps_null_optional_fields(string status)
    {
        await using var db = Store(); var f = await Seed(db);
        var result = await new EnrollmentsController(db).Update(f.Academy, f.Target.Id, new(status, null, null), default);
        Assert.IsType<OkObjectResult>(result.Result); db.ChangeTracker.Clear();
        var saved = await db.Enrollments.SingleAsync(); Assert.Equal("Active", saved.Status); Assert.Null(saved.EndDate); Assert.Null(saved.LifecycleReason);
    }

    [Theory]
    [InlineData(false, "Open")]
    [InlineData(true, "Closed")]
    [InlineData(true, "Waitlist")]
    public async Task Activation_requires_active_open_target_without_writes(bool active, string open)
    {
        await using var db = Store(); var f = await Seed(db); f.Batch.IsActive = active; f.Batch.EnrollmentStatus = open; await db.SaveChangesAsync();
        var before = await State(db); Assert.IsType<ConflictObjectResult>((await new EnrollmentsController(db).Update(f.Academy, f.Target.Id, new("Active", null, null), default)).Result);
        Assert.Equal(before, await State(db));
    }

    [Fact]
    public async Task Reaffirming_existing_active_does_not_consume_another_place()
    {
        await using var db = Store(); var f = await Seed(db, "Active");
        Assert.IsType<OkObjectResult>((await new EnrollmentsController(db).Update(f.Academy, f.Target.Id, new("Active", null, null), default)).Result);
        Assert.Single(await db.Enrollments.ToListAsync());
    }

    [Fact]
    public async Task Foreign_activation_is_not_found_and_cannot_mutate_owned_record()
    {
        await using var db = Store(); var f = await Seed(db); var before = await State(db);
        Assert.IsType<NotFoundResult>((await new EnrollmentsController(db).Update(Guid.NewGuid(), f.Target.Id, new("Active", null, null), default)).Result);
        Assert.Equal(before, await State(db));
    }

    [Fact]
    public async Task Promotion_into_full_target_keeps_source_and_pending_decision_unchanged()
    {
        await using var db = Store(); var f = await Seed(db, "Active"); var target = new Batch { AcademyId = f.Academy, Name = "Target", CourseId = Guid.NewGuid(), Capacity = 1 };
        var promotion = new BatchPromotion { AcademyId = f.Academy, StudentId = f.Target.StudentId, SourceBatchId = f.Batch.Id, TargetBatchId = target.Id, EffectiveDate = new(2026, 10, 23) };
        db.AddRange(target, promotion, new Enrollment { AcademyId = f.Academy, BatchId = target.Id, StudentId = Guid.NewGuid(), Status = "Active" }); await db.SaveChangesAsync(); var before = await State(db);
        Assert.IsType<ConflictObjectResult>(await new BatchPromotionsController(db).Decide(f.Academy, promotion.Id, new("Approved", null), default));
        Assert.Equal(before, await State(db));
    }

    [Theory]
    [InlineData(typeof(EnrollmentsController), "Create")]
    [InlineData(typeof(EnrollmentsController), "Update")]
    [InlineData(typeof(EnrollmentsController), "Transfer")]
    [InlineData(typeof(BatchPromotionsController), "Decide")]
    [InlineData(typeof(BatchesController), "Update")]
    public void Every_capacity_writer_joins_domain_save_and_required_audit_transaction(Type controller, string method)
    {
        var action = new ControllerActionDescriptor { MethodInfo = controller.GetMethod(method)! };
        Assert.True(AcademyAccessFilter.UsesAtomicBoundary(controller.Name, action)); Assert.False(AcademyAccessFilter.IncludesIdentity(action));
        Assert.False(AcademyAccessFilter.IncludesPlatformOwner(action)); // Existing platform-owner bypass policy unchanged.
    }
}
