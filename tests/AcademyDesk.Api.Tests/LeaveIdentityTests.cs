using System.Text.Json;
using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Tests;

public sealed class LeaveIdentityTests
{
    private static readonly DateOnly Start = new(2026, 10, 8);
    public static IEnumerable<object[]> ValidTypes()
    {
        foreach (var type in new[] { "Student", "student", "STUDENT", " \t Student \n", "Teacher", "teacher", "TEACHER", " \t Teacher \n" })
            foreach (var active in new[] { true, false }) yield return [type, active];
    }

    [Theory, MemberData(nameof(ValidTypes))]
    public async Task Canonical_case_space_and_inactive_owned_person_preserve_existing_eligibility(string type, bool active)
    {
        var fixture = await SeedAsync(active);
        await using var db = new AcademyDeskDbContext(fixture.Options);
        var student = type.Trim().Equals("Student", StringComparison.OrdinalIgnoreCase);
        var request = new CreateLeaveRequest(type, student ? fixture.Student : null, student ? null : fixture.Teacher, Start, Start, "  Synthetic reason  ");
        Assert.IsType<OkResult>(await new LeaveRequestsController(db).Create(fixture.Academy, request, default));
        await using var fresh = new AcademyDeskDbContext(fixture.Options);
        var row = await fresh.LeaveRequests.SingleAsync();
        Assert.Equal(student ? "Student" : "Teacher", row.RequesterType);
        Assert.Equal(student ? fixture.Student : (Guid?)null, row.StudentId);
        Assert.Equal(student ? (Guid?)null : fixture.Teacher, row.TeacherId);
        Assert.Equal("Synthetic reason", row.Reason); Assert.Equal(Start, row.StartDate); Assert.Equal(Start, row.EndDate);
        Assert.Equal("Requested", row.Status); Assert.Null(row.DecisionNotes);
    }

    public static IEnumerable<object?[]> UnsupportedTypes()
    {
        foreach (var type in new string?[] { null, "", "  ", "Guardian", "Staff", "StudentBad", "1" })
            foreach (var ids in new[] { false, true }) yield return [type, ids];
    }

    [Theory, MemberData(nameof(UnsupportedTypes))]
    public async Task Unsupported_discriminator_cannot_bypass_person_validation(string? type, bool ids)
    {
        var f = await SeedAsync();
        await RejectAsync(f, new(type!, ids ? f.Student : null, ids ? f.Teacher : null, Start, Start, "Synthetic reason"));
    }

    public static IEnumerable<object[]> InvalidIdentities()
    {
        foreach (var type in new[] { "student", " TEACHER " })
            foreach (var scenario in new[] { "missing", "empty-guid", "opposite-only", "both-local", "both-foreign", "extra-empty-guid", "foreign", "random" }) yield return [type, scenario];
    }

    [Theory, MemberData(nameof(InvalidIdentities))]
    public async Task Exactly_one_matching_owned_person_is_required(string type, string scenario)
    {
        var f = await SeedAsync(); var studentType = type.Trim().Equals("student", StringComparison.OrdinalIgnoreCase);
        Guid? matching = scenario switch { "missing" or "opposite-only" => null, "empty-guid" => Guid.Empty,
            "foreign" => studentType ? f.ForeignStudent : f.ForeignTeacher, "random" => Guid.NewGuid(), _ => studentType ? f.Student : f.Teacher };
        Guid? opposite = scenario switch { "opposite-only" or "both-local" => studentType ? f.Teacher : f.Student,
            "both-foreign" => studentType ? f.ForeignTeacher : f.ForeignStudent, "extra-empty-guid" => Guid.Empty, _ => null };
        await RejectAsync(f, new(type, studentType ? matching : opposite, studentType ? opposite : matching, Start, Start, "Synthetic reason"));
    }

    [Theory]
    [InlineData(null)] [InlineData("")] [InlineData(" \t ")]
    public async Task Empty_reason_remains_rejected(string? reason)
    {
        var f = await SeedAsync(); await RejectAsync(f, new("Student", f.Student, null, Start, Start, reason!));
    }

    [Fact]
    public async Task Reversed_dates_remain_rejected()
    {
        var f = await SeedAsync(); await RejectAsync(f, new("Teacher", null, f.Teacher, Start, Start.AddDays(-1), "Synthetic reason"));
    }

    [Fact]
    public async Task Canonical_saved_types_remain_visible_only_in_their_academy_list()
    {
        var f = await SeedAsync(); await using var db = new AcademyDeskDbContext(f.Options); var controller = new LeaveRequestsController(db);
        Assert.IsType<OkResult>(await controller.Create(f.Academy, new(" student ", f.Student, null, Start, Start, "Own"), default));
        Assert.IsType<OkResult>(await controller.Create(f.Foreign, new(" teacher ", null, f.ForeignTeacher, Start, Start, "Foreign"), default));
        var own = Assert.IsAssignableFrom<IEnumerable<LeaveSummary>>(Assert.IsType<OkObjectResult>(await controller.List(f.Academy, default)).Value);
        var row = Assert.Single(own); Assert.Equal("Student", row.RequesterType); Assert.Equal(f.Student, row.StudentId); Assert.Equal("Own", row.Reason);
    }

    private static async Task RejectAsync(Fixture f, CreateLeaveRequest request)
    {
        await using var db = new AcademyDeskDbContext(f.Options);
        db.Add(new LeaveRequest { AcademyId = f.Academy, RequesterType = "Student", StudentId = f.Student,
            StartDate = Start, EndDate = Start, Reason = "Preserve", Status = "Approved", DecisionNotes = "Existing decision" });
        await db.SaveChangesAsync();
        var before = JsonSerializer.Serialize(await db.LeaveRequests.AsNoTracking().OrderBy(x => x.Id).ToListAsync());
        var result = Assert.IsType<BadRequestObjectResult>(await new LeaveRequestsController(db).Create(f.Academy, request, default));
        Assert.False(string.IsNullOrWhiteSpace(JsonSerializer.Serialize(result.Value)));
        await using var fresh = new AcademyDeskDbContext(f.Options);
        Assert.Equal(before, JsonSerializer.Serialize(await fresh.LeaveRequests.AsNoTracking().OrderBy(x => x.Id).ToListAsync()));
        Assert.Empty(await fresh.AuditLogs.ToListAsync());
        Assert.DoesNotContain(db.ChangeTracker.Entries(), x => x.State is EntityState.Added or EntityState.Modified or EntityState.Deleted);
    }

    private sealed record Fixture(DbContextOptions<AcademyDeskDbContext> Options, Guid Academy, Guid Foreign,
        Guid Student, Guid Teacher, Guid ForeignStudent, Guid ForeignTeacher);

    private static async Task<Fixture> SeedAsync(bool active = true)
    {
        var options = new DbContextOptionsBuilder<AcademyDeskDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var db = new AcademyDeskDbContext(options); var academy = Guid.NewGuid(); var foreign = Guid.NewGuid();
        var student = new Student { AcademyId = academy, FirstName = "Synthetic", LastName = "Student", IsActive = active };
        var teacher = new Teacher { AcademyId = academy, FirstName = "Synthetic", LastName = "Teacher", IsActive = active };
        var foreignStudent = new Student { AcademyId = foreign, FirstName = "Foreign", LastName = "Student" };
        var foreignTeacher = new Teacher { AcademyId = foreign, FirstName = "Foreign", LastName = "Teacher" };
        db.AddRange(student, teacher, foreignStudent, foreignTeacher); await db.SaveChangesAsync();
        return new(options, academy, foreign, student.Id, teacher.Id, foreignStudent.Id, foreignTeacher.Id);
    }
}
