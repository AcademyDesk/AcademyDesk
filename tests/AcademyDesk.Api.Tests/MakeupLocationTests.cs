using System.Text.Json;
using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Tests;

public sealed class MakeupLocationTests
{
    public static IEnumerable<object[]> ValidModes()
    {
        foreach (var next in new[] { false, true })
        foreach (var mode in new[] { "Offline", " offline ", "Online", " online ", "Hybrid", " hybrid ", "InPerson", " inperson " })
            if (next || !mode.Trim().Equals("InPerson", StringComparison.OrdinalIgnoreCase)) yield return [next, mode];
    }

    [Theory, MemberData(nameof(ValidModes))]
    public async Task Resolved_mode_keeps_only_its_location_in_response_SQL_list_and_queued_notices(bool next, string mode)
    {
        var physical = mode.Trim().Equals("Offline", StringComparison.OrdinalIgnoreCase) || mode.Trim().Equals("InPerson", StringComparison.OrdinalIgnoreCase);
        var canonical = physical ? "Offline" : mode.Trim().Equals("Online", StringComparison.OrdinalIgnoreCase) ? "Online" : "Hybrid";
        var location = physical ? "Studio A" : "https://meeting.example.invalid/makeup";
        var f = await SeedAsync(mode, " " + location + " ");
        await using var db = new AcademyDeskDbContext(f.Options);
        var beforeSources = await Sources(db);
        var request = new CreateMakeupRequest(f.Student, f.Batch, null, f.Start.AddDays(2), next ? "Offline" : mode,
            " Studio A ", " https://meeting.example.invalid/makeup ", next, " Notes ");
        var controller = new MakeupClassesController(db);
        var summary = Assert.IsType<MakeupSummary>(Assert.IsType<OkObjectResult>(await controller.Create(f.Academy, request, default)).Value);
        await using var fresh = new AcademyDeskDbContext(f.Options);
        var row = Assert.Single(await fresh.MakeupClasses.ToListAsync());
        Assert.Equal(canonical, row.DeliveryMode); Assert.Equal(physical ? location : null, row.Venue); Assert.Equal(physical ? null : location, row.MeetingLink);
        Assert.Equal(next ? f.Start : f.Start.AddDays(2), row.StartUtc); Assert.Equal(row.StartUtc.AddMinutes(next ? 45 : 75), row.EndUtc);
        Assert.Equal(next ? f.Teacher : (Guid?)null, row.TeacherId); Assert.Equal(next, row.UsesNextScheduledClass); Assert.Equal("Notes", row.Notes);
        Assert.Equal("Scheduled", row.Status); Assert.Equal(row.Id, summary.Id); Assert.Equal(row.Venue, summary.Venue); Assert.Equal(row.MeetingLink, summary.MeetingLink); Assert.Equal(row.DeliveryMode, summary.DeliveryMode);
        var listed = Assert.IsAssignableFrom<IEnumerable<MakeupSummary>>(Assert.IsType<OkObjectResult>(await controller.List(f.Academy, default)).Value);
        Assert.Equal(summary, Assert.Single(listed)); Assert.Equal(beforeSources, await Sources(fresh));
        var notifications = await fresh.Notifications.ToListAsync(); Assert.Equal(2, notifications.Count);
        Assert.Contains(notifications, x => x.RecipientType == "Student" && x.RecipientId == f.Student);
        Assert.Contains(notifications, x => x.RecipientType == "Parent" && x.RecipientId == f.Parent);
        foreach (var notice in notifications) { Assert.Equal(f.Academy, notice.AcademyId); Assert.Equal("InApp", notice.Channel); Assert.Equal("Queued", notice.Status); Assert.Null(notice.SentAtUtc); Assert.Contains(canonical + " · " + location, notice.Message); Assert.Contains("IST", notice.Message); }
    }

    [Theory]
    [InlineData("InPerson", null)] [InlineData("Offline", "")] [InlineData(" inperson ", "  ")]
    public async Task Inherited_physical_room_remains_optional(string mode, string? location)
    {
        var f = await SeedAsync(mode, location); await using var db = new AcademyDeskDbContext(f.Options);
        Assert.IsType<OkObjectResult>(await new MakeupClassesController(db).Create(f.Academy, Request(f, true), default));
        await using var fresh = new AcademyDeskDbContext(f.Options); var row = Assert.Single(await fresh.MakeupClasses.ToListAsync());
        Assert.Equal("Offline", row.DeliveryMode); Assert.Null(row.Venue); Assert.Null(row.MeetingLink);
        Assert.All(await fresh.Notifications.ToListAsync(), x => Assert.EndsWith("Offline", x.Message));
    }

    public static IEnumerable<object?[]> EmptyVirtualLinks()
    {
        foreach (var next in new[] { false, true }) foreach (var mode in new[] { "Online", " hybrid " }) foreach (var link in new string?[] { null, "", "  " }) yield return [next, mode, link];
    }
    [Theory, MemberData(nameof(EmptyVirtualLinks))]
    public async Task Virtual_link_is_required_after_normalizing_manual_or_inherited_mode(bool next, string mode, string? link)
    {
        var f = await SeedAsync(mode, link); await Reject(f, Request(f, next, mode, link: link));
    }

    [Theory]
    [InlineData(false, "Unknown")] [InlineData(false, "")] [InlineData(false, "  ")] [InlineData(false, "InPerson")]
    [InlineData(true, "Unknown")] [InlineData(true, "")] [InlineData(true, "  ")]
    public async Task Unsupported_resolved_mode_rejects_without_makeup_or_notice(bool next, string mode)
    {
        var f = await SeedAsync(mode, "Location"); await Reject(f, Request(f, next, next ? "Offline" : mode));
    }

    [Fact]
    public async Task Null_manual_mode_preserves_Offline_default_and_ignores_link()
    {
        var f = await SeedAsync("InPerson", "Studio"); await using var db = new AcademyDeskDbContext(f.Options);
        Assert.IsType<OkObjectResult>(await new MakeupClassesController(db).Create(f.Academy, Request(f, false, null, " Online stale link "), default));
        await using var fresh = new AcademyDeskDbContext(f.Options); var row = Assert.Single(await fresh.MakeupClasses.ToListAsync());
        Assert.Equal("Offline", row.DeliveryMode); Assert.Equal("Studio", row.Venue); Assert.Null(row.MeetingLink);
    }

    [Fact]
    public async Task No_upcoming_owned_scheduled_session_does_not_fall_back_to_foreign_past_or_cancelled()
    {
        var f = await SeedAsync("InPerson", "Studio");
        await using (var db = new AcademyDeskDbContext(f.Options)) { (await db.ClassSessions.SingleAsync()).Status = "Cancelled"; db.Add(new ClassSession { AcademyId = Guid.NewGuid(), BatchId = f.Batch, StartUtc = f.Start, EndUtc = f.Start.AddHours(1), DeliveryMode = "Offline", RoomName = "Foreign" }); db.Add(new ClassSession { AcademyId = f.Academy, BatchId = f.Batch, StartUtc = DateTime.UtcNow.AddDays(-1), EndUtc = DateTime.UtcNow.AddHours(-23), DeliveryMode = "Offline", RoomName = "Past" }); await db.SaveChangesAsync(); }
        await Reject(f, Request(f, true));
    }

    [Fact]
    public async Task Null_inherited_teacher_still_uses_existing_batch_default()
    {
        var f = await SeedAsync("InPerson", "Studio"); await using var db = new AcademyDeskDbContext(f.Options);
        (await db.ClassSessions.SingleAsync()).TeacherId = null; await db.SaveChangesAsync();
        Assert.IsType<OkObjectResult>(await new MakeupClassesController(db).Create(f.Academy, Request(f, true), default));
        await using var fresh = new AcademyDeskDbContext(f.Options); Assert.Equal(f.Teacher, (await fresh.MakeupClasses.SingleAsync()).TeacherId);
    }

    private static CreateMakeupRequest Request(Fixture f, bool next, string? mode = "Offline", string? link = "https://ignored.example.invalid") => new(f.Student, f.Batch, null, f.Start.AddDays(2), mode, " Studio ", link, next, null);
    private static async Task<string> Sources(AcademyDeskDbContext db) => JsonSerializer.Serialize(new { Batches = await db.Batches.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), Sessions = await db.ClassSessions.AsNoTracking().OrderBy(x => x.Id).ToListAsync() });
    private static async Task Reject(Fixture f, CreateMakeupRequest request)
    {
        await using var db = new AcademyDeskDbContext(f.Options); var before = await Sources(db);
        Assert.IsType<BadRequestObjectResult>(await new MakeupClassesController(db).Create(f.Academy, request, default));
        await using var fresh = new AcademyDeskDbContext(f.Options); Assert.Empty(await fresh.MakeupClasses.ToListAsync()); Assert.Empty(await fresh.Notifications.ToListAsync()); Assert.Equal(before, await Sources(fresh));
        Assert.DoesNotContain(db.ChangeTracker.Entries(), x => x.State is EntityState.Added or EntityState.Modified or EntityState.Deleted);
    }
    private sealed record Fixture(DbContextOptions<AcademyDeskDbContext> Options, Guid Academy, Guid Student, Guid Teacher, Guid Batch, Guid Parent, DateTime Start);
    private static async Task<Fixture> SeedAsync(string mode, string? location)
    {
        var options = new DbContextOptionsBuilder<AcademyDeskDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var db = new AcademyDeskDbContext(options); var academy = Guid.NewGuid(); var start = DateTime.UtcNow.Date.AddDays(30);
        var student = new Student { AcademyId = academy, FirstName = "Synthetic", LastName = "Student" };
        var teacher = new Teacher { AcademyId = academy, FirstName = "Synthetic", LastName = "Teacher" };
        var batch = new Batch { AcademyId = academy, CourseId = Guid.NewGuid(), Name = "Synthetic batch", TeacherId = teacher.Id, SessionMinutes = 75 };
        var parent = new Guardian { AcademyId = academy, FirstName = "Allowed", LastName = "Parent" };
        db.AddRange(student, teacher, batch, parent, new StudentGuardian { AcademyId = academy, StudentId = student.Id, GuardianId = parent.Id, CanAccessPortal = true },
            new StudentGuardian { AcademyId = academy, StudentId = student.Id, GuardianId = Guid.NewGuid(), CanAccessPortal = true, AccessRevokedAtUtc = DateTime.UtcNow },
            new StudentGuardian { AcademyId = academy, StudentId = student.Id, GuardianId = Guid.NewGuid(), CanAccessPortal = false },
            new StudentGuardian { AcademyId = Guid.NewGuid(), StudentId = student.Id, GuardianId = Guid.NewGuid(), CanAccessPortal = true },
            new ClassSession { AcademyId = academy, BatchId = batch.Id, TeacherId = teacher.Id, StartUtc = start, EndUtc = start.AddMinutes(45), DeliveryMode = mode, RoomName = location });
        await db.SaveChangesAsync(); return new(options, academy, student.Id, teacher.Id, batch.Id, parent.Id, start);
    }
}
