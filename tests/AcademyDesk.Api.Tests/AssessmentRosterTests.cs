using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Tests;

public sealed class AssessmentRosterTests
{
    [Theory]
    [InlineData("never", false)]
    [InlineData("never", true)]
    [InlineData("wrong-batch", false)]
    [InlineData("wrong-batch", true)]
    [InlineData("foreign-enrollment", false)]
    [InlineData("foreign-enrollment", true)]
    [InlineData("wrong-student", false)]
    [InlineData("wrong-student", true)]
    [InlineData("foreign-student", false)]
    [InlineData("foreign-assessment", false)]
    [InlineData("missing-assessment", false)]
    public async Task Unrelated_membership_cannot_create_or_overwrite_a_result(string scenario, bool existing)
    {
        var options = new DbContextOptionsBuilder<AcademyDeskDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var db = new AcademyDeskDbContext(options);
        var academy = Guid.NewGuid(); var foreign = Guid.NewGuid();
        var student = new Student { AcademyId = scenario == "foreign-student" ? foreign : academy, FirstName = "Synthetic", LastName = "Learner" };
        var assessment = new Assessment { AcademyId = scenario == "foreign-assessment" ? foreign : academy, BatchId = Guid.NewGuid(), Title = "Roster fixture" };
        db.AddRange(student, assessment);
        if (scenario is not "never") db.Add(new Enrollment {
            AcademyId = scenario == "foreign-enrollment" ? foreign : academy,
            BatchId = scenario == "wrong-batch" ? Guid.NewGuid() : assessment.BatchId,
            StudentId = scenario == "wrong-student" ? Guid.NewGuid() : student.Id });
        if (existing) db.Add(new AssessmentResult { AcademyId = academy, AssessmentId = assessment.Id, StudentId = student.Id, Score = 25, Grade = "Preserve", Remarks = "Original", IsPublished = true });
        await db.SaveChangesAsync();
        var response = await new AssessmentResultsController(db).Upsert(academy, scenario == "missing-assessment" ? Guid.NewGuid() : assessment.Id,
            new(student.Id, 80, "Changed", "Changed", false, true), default);
        if (scenario is "foreign-assessment" or "missing-assessment") Assert.IsType<NotFoundResult>(response.Result);
        else Assert.IsType<BadRequestObjectResult>(response.Result);
        await using var fresh = new AcademyDeskDbContext(options); var rows = await fresh.AssessmentResults.ToListAsync();
        Assert.Equal(existing ? 1 : 0, rows.Count);
        if (existing) { Assert.Equal(25, rows[0].Score); Assert.Equal("Preserve", rows[0].Grade); Assert.Equal("Original", rows[0].Remarks); Assert.True(rows[0].IsPublished); }
        Assert.DoesNotContain(db.ChangeTracker.Entries(), x => x.State is EntityState.Modified or EntityState.Added or EntityState.Deleted);
    }

    [Fact]
    public async Task Active_exact_membership_can_create_and_update_without_duplicate_results()
    {
        var options = new DbContextOptionsBuilder<AcademyDeskDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var db = new AcademyDeskDbContext(options); var academy = Guid.NewGuid();
        var student = new Student { AcademyId = academy, FirstName = "Synthetic", LastName = "Learner" };
        var assessment = new Assessment { AcademyId = academy, BatchId = Guid.NewGuid(), Title = "Roster fixture" };
        db.AddRange(student, assessment, new Enrollment { AcademyId = academy, BatchId = assessment.BatchId, StudentId = student.Id }); await db.SaveChangesAsync();
        var controller = new AssessmentResultsController(db);
        foreach (var score in new[] { 0m, 80m }) {
            Assert.IsType<OkObjectResult>((await controller.Upsert(academy, assessment.Id, new(student.Id, score, null, null, false, false), default)).Result);
            await using var fresh = new AcademyDeskDbContext(options); Assert.Equal(score, (await fresh.AssessmentResults.SingleAsync()).Score);
        }
    }
}
