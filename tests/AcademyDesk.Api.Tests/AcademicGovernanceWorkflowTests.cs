using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Tests;

public sealed class AcademicGovernanceWorkflowTests
{
    [Fact]
    public async Task Term_outside_its_academic_year_is_rejected()
    {
        await using var db = CreateDb();
        var academyId = Guid.NewGuid();
        var year = new AcademicYear { AcademyId = academyId, Name = "2026-27", StartDate = new DateOnly(2026, 6, 1), EndDate = new DateOnly(2027, 5, 31), IsCurrent = true };
        db.AcademicYears.Add(year);
        await db.SaveChangesAsync();

        var result = await new AcademicPeriodsController(db).CreateTerm(academyId, new AcademicTermRequest(year.Id, "Term 1", new DateOnly(2026, 5, 1), new DateOnly(2026, 9, 30)), CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task Assessment_cannot_use_an_inactive_grading_scheme()
    {
        await using var db = CreateDb();
        var academyId = Guid.NewGuid();
        var batch = new Batch { AcademyId = academyId, Name = "Piano Level 1", CourseId = Guid.NewGuid() };
        var scheme = new GradingScheme { AcademyId = academyId, Name = "Retired scheme", PassingPercent = 50m, IsActive = false };
        db.AddRange(batch, scheme);
        await db.SaveChangesAsync();

        var result = await new AssessmentsController(db).Create(academyId, new CreateAssessmentRequest(batch.Id, "Theory test", "Test", 100m, scheme.Id, null, false), CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task Approved_promotion_completes_source_and_creates_target_enrolment()
    {
        await using var db = CreateDb();
        var academyId = Guid.NewGuid(); var studentId = Guid.NewGuid();
        var source = new Batch { AcademyId = academyId, Name = "Foundation", CourseId = Guid.NewGuid() };
        var target = new Batch { AcademyId = academyId, Name = "Intermediate", CourseId = Guid.NewGuid() };
        db.AddRange(source, target);
        await db.SaveChangesAsync();
        var sourceEnrolment = new Enrollment { AcademyId = academyId, StudentId = studentId, BatchId = source.Id, Status = "Active" };
        var promotion = new BatchPromotion { AcademyId = academyId, StudentId = studentId, SourceBatchId = source.Id, TargetBatchId = target.Id, EffectiveDate = new DateOnly(2026, 9, 1) };
        db.AddRange(sourceEnrolment, promotion);
        await db.SaveChangesAsync();

        var result = await new BatchPromotionsController(db).Decide(academyId, promotion.Id, new PromotionDecision("Approved", "Progression assessment passed"), CancellationToken.None);

        Assert.IsType<OkResult>(result);
        Assert.Equal("Completed", (await db.Enrollments.SingleAsync(x => x.Id == sourceEnrolment.Id)).Status);
        Assert.Contains(await db.Enrollments.ToListAsync(), item => item.StudentId == studentId && item.BatchId == target.Id && item.Status == "Active");
        Assert.Equal("Approved", (await db.BatchPromotions.SingleAsync(x => x.Id == promotion.Id)).Status);
    }

    private static AcademyDeskDbContext CreateDb() => new(new DbContextOptionsBuilder<AcademyDeskDbContext>().UseInMemoryDatabase($"academic-tests-{Guid.NewGuid()}").Options);
}
