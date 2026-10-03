using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Tests;

public sealed class AssessmentGradingTests
{
    [Theory]
    [InlineData(0, "Fail")]
    [InlineData(49.99, "Fail")]
    [InlineData(50, "Pass")]
    [InlineData(80, "Pass")]
    [InlineData(100, "Pass")]
    public async Task Automatic_grade_tracks_threshold_and_ignores_loaded_grade(decimal score, string grade)
    {
        await using var db = Database(); var a = Seed(db); await db.SaveChangesAsync();
        var r = await AssessmentGrading.ResolveAsync(db, a, score, "Stale override", false, default);
        Assert.Equal(grade, r.Grade); Assert.False(r.IsManual); Assert.Null(r.Error);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public async Task Legacy_blank_grade_requests_use_automatic_calculation(string? grade)
    {
        await using var db = Database(); var a = Seed(db); await db.SaveChangesAsync();
        var r = await AssessmentGrading.ResolveAsync(db, a, 80, grade, null, default);
        Assert.Equal("Pass", r.Grade); Assert.False(r.IsManual); Assert.Null(r.Error);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(null)]
    public async Task Explicit_and_legacy_manual_values_are_preserved(bool? mode)
    {
        await using var db = Database(); var a = Seed(db); await db.SaveChangesAsync();
        var r = await AssessmentGrading.ResolveAsync(db, a, 0, " Merit ", mode, default);
        Assert.Equal("Merit", r.Grade); Assert.True(r.IsManual); Assert.Null(r.Error);
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("foreign")]
    [InlineData("zero-max")]
    [InlineData("negative")]
    [InlineData("above-max")]
    [InlineData("excess-precision")]
    [InlineData("blank-manual")]
    [InlineData("long-manual")]
    public async Task Invalid_grading_returns_guidance_without_writes(string mode)
    {
        await using var db = Database(); var a = Seed(db);
        if (mode == "missing") a.GradingSchemeId = Guid.NewGuid();
        if (mode == "foreign") db.ChangeTracker.Entries<GradingScheme>().Single().Entity.AcademyId = Guid.NewGuid();
        if (mode == "zero-max") a.MaxScore = 0;
        await db.SaveChangesAsync();
        var r = await AssessmentGrading.ResolveAsync(db, a, mode == "negative" ? -1 : mode == "above-max" ? 101 : mode == "excess-precision" ? 49.999m : 20,
            mode == "long-manual" ? new string('A', 31) : null, mode is "blank-manual" or "long-manual", default);
        Assert.NotNull(r.Error); Assert.Empty(await db.AssessmentResults.ToListAsync());
    }

    [Fact]
    public async Task No_scheme_automatic_mode_has_no_fabricated_grade()
    {
        await using var db = Database(); var a = Seed(db); a.GradingSchemeId = null; await db.SaveChangesAsync();
        var r = await AssessmentGrading.ResolveAsync(db, a, 50, "Pass", false, default);
        Assert.Null(r.Grade); Assert.False(r.IsManual); Assert.Null(r.Error);
    }

    [Fact]
    public async Task Admin_roundtrip_recalculates_auto_and_preserves_manual_grade()
    {
        var options = new DbContextOptionsBuilder<AcademyDeskDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var db = new AcademyDeskDbContext(options); var a = Seed(db);
        var student = new Student { AcademyId = a.AcademyId, FirstName = "Synthetic", LastName = "Learner" }; db.Add(student);
        db.Add(new Enrollment { AcademyId = a.AcademyId, BatchId = a.BatchId, StudentId = student.Id }); await db.SaveChangesAsync();
        var controller = new AssessmentResultsController(db);
        foreach (var (score, grade, manual, expected) in new (decimal, string?, bool, string)[] { (80, null, false, "Pass"), (20, "Pass", false, "Fail"), (0, " Distinction ", true, "Distinction"), (99, "Distinction", true, "Distinction"), (0, "Distinction", false, "Fail") })
        {
            var response = await controller.Upsert(a.AcademyId, a.Id, new(student.Id, score, grade, " Note ", false, manual), default);
            var summary = Assert.IsType<AssessmentResultSummary>(Assert.IsType<OkObjectResult>(response.Result).Value);
            Assert.Equal(expected, summary.Grade); Assert.Equal(manual, summary.IsGradeManual);
            await using var fresh = new AcademyDeskDbContext(options); var persisted = await fresh.AssessmentResults.SingleAsync();
            Assert.Equal(expected, persisted.Grade); Assert.Equal(manual, persisted.IsGradeManual); Assert.Equal(score, persisted.Score); Assert.Equal("Note", persisted.Remarks);
        }
    }
    private static AcademyDeskDbContext Database() => new(new DbContextOptionsBuilder<AcademyDeskDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    private static Assessment Seed(AcademyDeskDbContext db)
    {
        var academy = Guid.NewGuid(); var scheme = new GradingScheme { AcademyId = academy, Name = "Synthetic threshold", PassingPercent = 50 };
        var assessment = new Assessment { AcademyId = academy, BatchId = Guid.NewGuid(), Title = "Synthetic assessment", MaxScore = 100, GradingSchemeId = scheme.Id }; db.AddRange(scheme, assessment); return assessment;
    }
}
