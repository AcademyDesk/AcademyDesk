using System.Text.Json;
using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Tests;

// Direct controller + isolated EF InMemory; not HTTP authorization or SQL proof.
public sealed class CoursePreservationTests
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);
    private static DbContextOptions<AcademyDeskDbContext> Options() => new DbContextOptionsBuilder<AcademyDeskDbContext>()
        .UseInMemoryDatabase($"CourseContract_{Guid.NewGuid():N}").Options;
    private static ProgramCourse Course(Guid academy, string type = "Music", bool populated = true, bool active = true) => new()
    {
        AcademyId = academy, Name = "Synthetic course", AcademyType = type, IsActive = active,
        CourseCode = populated ? "QA-C1" : null, SubjectArea = populated ? "QA subject" : null,
        Level = populated ? "Level1" : null, Description = populated ? "QA description" : null,
        DurationMonths = populated ? 9 : null, WeeklySessions = populated ? 2 : null,
        SessionMinutes = populated ? 45 : null, MinimumAge = populated ? 0 : null,
        MaximumAge = populated ? 18 : null, DeliveryMode = populated ? "Hybrid" : null,
        Prerequisites = populated ? "QA prerequisite text" : null, LearningOutcomes = populated ? "QA outcomes" : null,
        IsPublished = populated
    };
    private static ProgramCourse Copy(ProgramCourse x) => JsonSerializer.Deserialize<ProgramCourse>(JsonSerializer.Serialize(x))!;
    private static UpdateCourseRequest Request(ProgramCourse x) => new(x.Name, x.CourseCode, x.AcademyType,
        x.SubjectArea, x.Level, x.Description, x.DurationMonths, x.WeeklySessions, x.SessionMinutes,
        x.MinimumAge, x.MaximumAge, x.DeliveryMode, x.Prerequisites, x.LearningOutcomes, x.IsPublished, x.IsActive);
    private static CourseSummary Summary(ProgramCourse x) => new(x.Id, x.Name, x.CourseCode, x.AcademyType,
        x.SubjectArea, x.Level, x.Description, x.DurationMonths, x.WeeklySessions, x.SessionMinutes,
        x.MinimumAge, x.MaximumAge, x.DeliveryMode, x.Prerequisites, x.LearningOutcomes, x.IsPublished, x.IsActive);
    private static void Same(ProgramCourse expected, ProgramCourse actual) => Assert.Equal(JsonSerializer.Serialize(expected), JsonSerializer.Serialize(actual));
    public static IEnumerable<object[]> RoundTrips() => from type in new[] { "Music", "Tuition", "Coaching" }
        from populated in new[] { true, false } from action in new[] { "edit", "deactivate", "reactivate" }
        select new object[] { type, populated, action };
    public static IEnumerable<object[]> Creates() => from type in new[] { "Music", "Tuition", "Coaching" }
        from populated in new[] { true, false } select new object[] { type, populated };

    [Theory, MemberData(nameof(RoundTrips))]
    public async Task List_binding_update_and_fresh_read_preserve_unedited_fields(string type, bool populated, string action)
    {
        var options = Options(); using var db = new AcademyDeskDbContext(options);
        var academy = new Academy { Name = "Synthetic A" }; var row = Course(academy.Id, type, populated, action != "reactivate");
        var foreign = Course(Guid.NewGuid()); var prerequisite = new CoursePrerequisite { AcademyId = academy.Id, CourseId = row.Id, RequiredCourseId = Guid.NewGuid() };
        db.AddRange(academy, row, foreign, prerequisite); await db.SaveChangesAsync();
        var expected = Copy(row); var foreignBefore = Copy(foreign); var edgeBefore = JsonSerializer.Serialize(prerequisite);
        var controller = new CoursesController(db);
        var list = Assert.IsAssignableFrom<IReadOnlyList<CourseSummary>>(Assert.IsType<OkObjectResult>((await controller.List(academy.Id, default)).Result).Value);
        var summary = Assert.Single(list); Assert.Equal(Summary(expected), summary);
        using (var read = new AcademyDeskDbContext(options)) Same(expected, await read.Courses.SingleAsync(x => x.Id == row.Id));
        // Web-style camelCase JSON, including inherited request fields; not MVC HTTP binding.
        var request = JsonSerializer.Deserialize<UpdateCourseRequest>(JsonSerializer.Serialize(summary, Web), Web)!;
        if (action == "edit") { request = request with { Name = "Renamed synthetic" }; expected.Name = request.Name; }
        else { request = request with { IsActive = !request.IsActive }; expected.IsActive = request.IsActive; }
        var result = await controller.Update(academy.Id, row.Id, request, default);
        Assert.Equal(Summary(expected), Assert.IsType<CourseSummary>(Assert.IsType<OkObjectResult>(result.Result).Value));
        using var fresh = new AcademyDeskDbContext(options);
        Same(expected, await fresh.Courses.SingleAsync(x => x.Id == row.Id));
        Same(foreignBefore, await fresh.Courses.SingleAsync(x => x.Id == foreign.Id));
        Assert.Equal(edgeBefore, JsonSerializer.Serialize(await fresh.CoursePrerequisites.SingleAsync()));
        Assert.Equal(2, await fresh.Courses.CountAsync());
    }

    [Theory, MemberData(nameof(Creates))]
    public async Task Create_returns_and_stores_populated_or_optional_null_settings(string type, bool populated)
    {
        var options = Options(); using var db = new AcademyDeskDbContext(options);
        var academy = new Academy { Name = "Synthetic A" }; db.Add(academy); await db.SaveChangesAsync();
        var expected = Course(academy.Id, type, populated);
        var request = JsonSerializer.Deserialize<CreateCourseRequest>(JsonSerializer.Serialize(Request(expected), Web), Web)!;
        var result = Assert.IsType<CreatedResult>((await new CoursesController(db).Create(academy.Id, request, default)).Result);
        var summary = Assert.IsType<CourseSummary>(result.Value);
        Assert.Equal($"/api/academies/{academy.Id}/courses/{summary.Id}", result.Location);
        using var fresh = new AcademyDeskDbContext(options); var saved = await fresh.Courses.SingleAsync();
        Assert.Equal(academy.Id, saved.AcademyId); Assert.True(saved.IsActive); Assert.Null(saved.UpdatedAtUtc);
        Assert.Equal(Summary(expected) with { Id = saved.Id }, summary); Assert.Equal(summary, Summary(saved));
    }

    [Theory]
    [InlineData("Music"), InlineData("Tuition"), InlineData("Coaching")]
    public async Task Intentional_level_clear_preserves_all_other_entity_fields(string type)
    {
        var options = Options(); using var db = new AcademyDeskDbContext(options); var row = Course(Guid.NewGuid(), type);
        db.Add(row); await db.SaveChangesAsync(); var expected = Copy(row); expected.Level = null;
        Assert.IsType<OkObjectResult>((await new CoursesController(db).Update(row.AcademyId, row.Id, Request(row) with { Level = null }, default)).Result);
        using var fresh = new AcademyDeskDbContext(options); Same(expected, await fresh.Courses.SingleAsync());
    }

    [Fact]
    public async Task Explicit_full_clear_retains_replacement_semantics_and_metadata()
    {
        var options = Options(); using var db = new AcademyDeskDbContext(options); var row = Course(Guid.NewGuid());
        db.Add(row); await db.SaveChangesAsync(); var expected = Course(row.AcademyId, populated: false);
        expected.Id = row.Id; expected.CreatedAtUtc = row.CreatedAtUtc; expected.UpdatedAtUtc = row.UpdatedAtUtc;
        Assert.IsType<OkObjectResult>((await new CoursesController(db).Update(row.AcademyId, row.Id, Request(expected), default)).Result);
        using var fresh = new AcademyDeskDbContext(options); Same(expected, await fresh.Courses.SingleAsync());
    }

    [Theory]
    [InlineData(false), InlineData(true)]
    public async Task Foreign_or_missing_target_returns_not_found_without_writing(bool missing)
    {
        var options = Options(); using var db = new AcademyDeskDbContext(options); var row = Course(Guid.NewGuid());
        db.Add(row); await db.SaveChangesAsync(); var before = Copy(row);
        Assert.IsType<NotFoundResult>((await new CoursesController(db).Update(Guid.NewGuid(), missing ? Guid.NewGuid() : row.Id,
            Request(row) with { Name = "Must not write" }, default)).Result);
        using var fresh = new AcademyDeskDbContext(options); Same(before, await fresh.Courses.SingleAsync());
    }

    [Theory]
    [InlineData("name"), InlineData("type"), InlineData("weekly-low"), InlineData("weekly-high")]
    [InlineData("session-low"), InlineData("session-high"), InlineData("min-low"), InlineData("min-high")]
    [InlineData("max-low"), InlineData("max-high"), InlineData("age-reversed"), InlineData("duplicate-code")]
    public async Task Invalid_replacement_is_rejected_without_changing_any_row(string invalid)
    {
        var options = Options(); using var db = new AcademyDeskDbContext(options); var academy = Guid.NewGuid();
        var row = Course(academy); var sibling = Course(academy); sibling.CourseCode = "QA-OTHER";
        db.AddRange(row, sibling); await db.SaveChangesAsync(); var before = Copy(row); var siblingBefore = Copy(sibling);
        var request = invalid switch
        {
            "name" => Request(row) with { Name = "  " }, "type" => Request(row) with { AcademyType = "Invalid" },
            "weekly-low" => Request(row) with { WeeklySessions = 0 }, "weekly-high" => Request(row) with { WeeklySessions = 15 },
            "session-low" => Request(row) with { SessionMinutes = 14 }, "session-high" => Request(row) with { SessionMinutes = 481 },
            "min-low" => Request(row) with { MinimumAge = -1 }, "min-high" => Request(row) with { MinimumAge = 121 },
            "max-low" => Request(row) with { MaximumAge = -1 }, "max-high" => Request(row) with { MaximumAge = 121 },
            "age-reversed" => Request(row) with { MinimumAge = 19, MaximumAge = 18 },
            _ => Request(row) with { CourseCode = " QA-OTHER " }
        };
        Assert.IsType<BadRequestObjectResult>((await new CoursesController(db).Update(academy, row.Id, request, default)).Result);
        using var fresh = new AcademyDeskDbContext(options);
        Same(before, await fresh.Courses.SingleAsync(x => x.Id == row.Id)); Same(siblingBefore, await fresh.Courses.SingleAsync(x => x.Id == sibling.Id));
        Assert.Equal(2, await fresh.Courses.CountAsync());
    }

    [Fact]
    public async Task Same_code_in_other_academy_does_not_reject_valid_update()
    {
        var options = Options(); using var db = new AcademyDeskDbContext(options); var row = Course(Guid.NewGuid()); var foreign = Course(Guid.NewGuid());
        db.AddRange(row, foreign); await db.SaveChangesAsync(); var before = Copy(foreign);
        Assert.IsType<OkObjectResult>((await new CoursesController(db).Update(row.AcademyId, row.Id, Request(row) with { Name = "Renamed" }, default)).Result);
        using var fresh = new AcademyDeskDbContext(options); Same(before, await fresh.Courses.SingleAsync(x => x.Id == foreign.Id));
        Assert.Equal("Renamed", (await fresh.Courses.SingleAsync(x => x.Id == row.Id)).Name);
    }

    [Fact]
    public async Task String_normalization_is_intentional_and_retains_numeric_and_publication_fields()
    {
        var options = Options(); using var db = new AcademyDeskDbContext(options); var row = Course(Guid.NewGuid());
        db.Add(row); await db.SaveChangesAsync(); var expected = Copy(row); expected.Name = "Renamed"; expected.Level = null;
        var request = Request(row) with { Name = " Renamed ", CourseCode = " QA-C1 ", AcademyType = " Music ", SubjectArea = " QA subject ",
            Level = "  ", Description = " QA description ", DeliveryMode = " Hybrid ", Prerequisites = " QA prerequisite text ", LearningOutcomes = " QA outcomes " };
        Assert.IsType<OkObjectResult>((await new CoursesController(db).Update(row.AcademyId, row.Id, request, default)).Result);
        using var fresh = new AcademyDeskDbContext(options); Same(expected, await fresh.Courses.SingleAsync());
    }

    [Fact]
    public async Task Legacy_abbreviated_payload_reproduces_hidden_setting_loss_without_server_patch_semantics()
    {
        var options = Options(); using var db = new AcademyDeskDbContext(options); var row = Course(Guid.NewGuid());
        db.Add(row); await db.SaveChangesAsync();
        var request = JsonSerializer.Deserialize<UpdateCourseRequest>(JsonSerializer.Serialize(new {
            name = row.Name, academyType = row.AcademyType, level = row.Level, description = row.Description, durationMonths = (int?)null, isActive = row.IsActive }), Web)!;
        Assert.IsType<OkObjectResult>((await new CoursesController(db).Update(row.AcademyId, row.Id, request, default)).Result);
        using var fresh = new AcademyDeskDbContext(options); var saved = await fresh.Courses.SingleAsync();
        Assert.Null(saved.CourseCode); Assert.Null(saved.SubjectArea); Assert.Null(saved.DurationMonths); Assert.Null(saved.WeeklySessions);
        Assert.Null(saved.SessionMinutes); Assert.Null(saved.MinimumAge); Assert.Null(saved.MaximumAge); Assert.Null(saved.DeliveryMode);
        Assert.Null(saved.Prerequisites); Assert.Null(saved.LearningOutcomes); Assert.False(saved.IsPublished);
    }
}
