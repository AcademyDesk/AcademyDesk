using System.Text.Json;
using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Tests;

public sealed class CoursePrerequisiteTests
{
    // Edge X:Y means X requires Y. Include legacy cycles to prove traversal terminates.
    [Theory]
    [InlineData("", 0, 1, 200)]
    [InlineData("0:1", 1, 0, 400)]
    [InlineData("0:1,1:2", 2, 0, 400)]
    [InlineData("0:1,1:2,2:3,3:4", 4, 0, 400)]
    [InlineData("0:1,1:2", 0, 2, 200)]
    [InlineData("0:1,0:2,1:3", 2, 3, 200)]
    [InlineData("0:1,0:2,1:3,2:3", 3, 0, 400)]
    [InlineData("0:1", 2, 3, 200)]
    [InlineData("0:1", 0, 1, 409)]
    [InlineData("", 0, 0, 400)]
    [InlineData("0:1,1:0", 2, 0, 200)]
    [InlineData("0:1,1:0,1:2", 2, 0, 400)]
    [InlineData("0:1,1:0", 2, 3, 200)]
    public async Task Graph_validation_preserves_existing_rows(string graph, int course, int required, int status)
    {
        var options = Options(); await using var db = new AcademyDeskDbContext(options);
        var academy = Guid.NewGuid(); var foreign = Guid.NewGuid();
        var ids = Enumerable.Range(0, 6).Select(_ => Guid.NewGuid()).ToArray();
        db.Courses.AddRange(ids.Select((id, i) => new ProgramCourse { Id = id, AcademyId = academy, Name = $"Course {i}" }));
        foreach (var pair in graph.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            var edge = pair.Split(':').Select(int.Parse).ToArray();
            db.CoursePrerequisites.Add(new CoursePrerequisite { AcademyId = academy, CourseId = ids[edge[0]], RequiredCourseId = ids[edge[1]] });
        }
        // Foreign corrupt edge with the same node IDs must not affect own-tenant reachability.
        db.CoursePrerequisites.Add(new CoursePrerequisite { AcademyId = foreign, CourseId = ids[required], RequiredCourseId = ids[course] });
        await db.SaveChangesAsync();
        var before = await State(db); var beforeRows = await db.CoursePrerequisites.AsNoTracking().OrderBy(x => x.Id).ToListAsync();
        var result = await new AcademicGovernanceController(db).AddPrerequisite(academy, new(ids[course], ids[required]), CancellationToken.None);
        Assert.Equal(status, Status(result));
        await using var fresh = new AcademyDeskDbContext(options);
        Assert.Equal(before.Courses, (await State(fresh)).Courses);
        var afterRows = await fresh.CoursePrerequisites.AsNoTracking().OrderBy(x => x.Id).ToListAsync();
        if (status != 200) Assert.Equal(before.Edges, JsonSerializer.Serialize(afterRows));
        else
        {
            var added = Assert.Single(afterRows, x => beforeRows.All(y => y.Id != x.Id));
            Assert.Equal(academy, added.AcademyId); Assert.Equal(ids[course], added.CourseId);
            Assert.Equal(ids[required], added.RequiredCourseId); Assert.True(added.MustBeCompleted);
            Assert.Equal(before.Edges, JsonSerializer.Serialize(afterRows.Where(x => x.Id != added.Id)));
        }
    }

    [Theory]
    [InlineData("missing-course")]
    [InlineData("missing-required")]
    [InlineData("foreign-course")]
    [InlineData("foreign-required")]
    [InlineData("foreign-route")]
    public async Task Invalid_course_membership_returns_400_without_writes(string mode)
    {
        var options = Options(); await using var db = new AcademyDeskDbContext(options);
        var academy = Guid.NewGuid(); var foreign = Guid.NewGuid();
        var a = new ProgramCourse { AcademyId = academy, Name = "A" }; var b = new ProgramCourse { AcademyId = academy, Name = "B" };
        var f = new ProgramCourse { AcademyId = foreign, Name = "Foreign" }; db.AddRange(a, b, f); await db.SaveChangesAsync();
        var before = await State(db);
        var result = await new AcademicGovernanceController(db).AddPrerequisite(mode == "foreign-route" ? foreign : academy,
            new(mode == "missing-course" ? Guid.NewGuid() : mode == "foreign-course" ? f.Id : a.Id,
                mode == "missing-required" ? Guid.NewGuid() : mode == "foreign-required" ? f.Id : b.Id), CancellationToken.None);
        Assert.Equal(400, Status(result)); await using var fresh = new AcademyDeskDbContext(options);
        Assert.Equal(before, await State(fresh));
    }

    [Fact]
    public void Only_prerequisite_write_opts_into_domain_audit_boundary()
    {
        var action = new ControllerActionDescriptor { MethodInfo = typeof(AcademicGovernanceController).GetMethod(nameof(AcademicGovernanceController.AddPrerequisite))! };
        Assert.True(AcademyAccessFilter.UsesAtomicBoundary(nameof(AcademicGovernanceController), action));
        Assert.False(AcademyAccessFilter.IncludesIdentity(action));
        Assert.Null(Attribute.GetCustomAttribute(typeof(AcademicGovernanceController).GetMethod(nameof(AcademicGovernanceController.AddScheme))!, typeof(AtomicAcademyMutationAttribute)));
    }

    private static int Status(ActionResult result) => result switch
    {
        StatusCodeResult r => r.StatusCode, ObjectResult r => r.StatusCode ?? 200,
        _ => throw new InvalidOperationException(result.GetType().Name)
    };
    private static async Task<(string Courses, string Edges)> State(AcademyDeskDbContext db) =>
        (JsonSerializer.Serialize(await db.Courses.AsNoTracking().OrderBy(x => x.Id).ToListAsync()),
         JsonSerializer.Serialize(await db.CoursePrerequisites.AsNoTracking().OrderBy(x => x.Id).ToListAsync()));
    private static DbContextOptions<AcademyDeskDbContext> Options() =>
        new DbContextOptionsBuilder<AcademyDeskDbContext>().UseInMemoryDatabase($"prerequisites-{Guid.NewGuid()}").Options;
}
