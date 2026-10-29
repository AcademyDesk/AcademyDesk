using System.Text.Json;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Infrastructure.Meetings;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Tests;

public sealed class MeetingPreviewTests
{
    private static async Task<(MeetingReadinessTests.Fixture F, MeetingPreviewRequest Request)> Seed(string role = "Owner")
    {
        var f = await MeetingReadinessTests.Fixture.Create(role);
        var teacher = new Teacher { AcademyId = f.Academy.Id, FirstName = "Synthetic", LastName = "Teacher", Email = "teacher@example.invalid" };
        var batch = new Batch { AcademyId = f.Academy.Id, Name = "Synthetic Online", DeliveryMode = "Online", TeacherId = teacher.Id };
        var student = new Student { AcademyId = f.Academy.Id, FirstName = "Synthetic", LastName = "Student", Email = "student@example.invalid" };
        f.Db.AddRange(teacher, batch, student, new Enrollment { AcademyId = f.Academy.Id, BatchId = batch.Id, StudentId = student.Id });
        await f.Db.SaveChangesAsync();
        var local = DateTime.UtcNow.Date.AddDays(2).AddHours(17); // Explicit offset below, never inferred for conversion.
        return (f, new MeetingPreviewRequest("GoogleWorkspace", batch.Id, teacher.Id, " Synthetic draft ", new DateTimeOffset(DateTime.SpecifyKind(local, DateTimeKind.Unspecified), TimeSpan.FromMinutes(330)), "Asia/Kolkata", 60, [student.Id], "Once", "InviteesOnly", "Disabled", "Off"));
    }
    private static Task<MeetingPreviewDecision> Prepare(MeetingReadinessTests.Fixture f, MeetingPreviewRequest r)
        => new MeetingPreviewService(f.Db, f.Users, f.Readiness, TimeProvider.System).PrepareAsync(f.Actor, f.Academy.Id, r, default);

    [Fact] public async Task Canonical_draft_is_scoped_non_executable_not_approved_and_does_not_write()
    {
        var (f, r) = await Seed(); await using var owned = f;
        var result = await Prepare(f, r); Assert.Equal(200, result.StatusCode); var draft = Assert.IsType<MeetingDraftPreview>(result.Draft);
        Assert.Equal("Synthetic draft", draft.Title); Assert.Equal(TimeSpan.Zero, draft.StartUtc.Offset); Assert.Equal(TimeSpan.FromMinutes(60), draft.EndUtc - draft.StartUtc);
        Assert.Equal(new[] { "student@example.invalid", "teacher@example.invalid" }, draft.AttendeeEmails); Assert.False(draft.CanExecute); Assert.Equal("NotIssued", draft.ApprovalStatus);
        Assert.Equal(64, draft.SourceVersion.Length); Assert.Equal(64, draft.RequestHash.Length); Assert.InRange(draft.ExpiresAtUtc, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddMinutes(6));
        Assert.Empty(await f.Db.AuditLogs.ToListAsync()); Assert.Empty(await f.Db.Notifications.ToListAsync()); Assert.DoesNotContain(f.Db.ChangeTracker.Entries(), x => x.State != EntityState.Unchanged);
    }
    [Theory] [InlineData("title")] [InlineData("long-title")] [InlineData("control-title")] [InlineData("provider")]
    [InlineData("duration")] [InlineData("empty-batch")] [InlineData("empty-organizer")]
    [InlineData("null-students")] [InlineData("empty-students")] [InlineData("duplicate-students")]
    [InlineData("empty-id")] [InlineData("too-many")]
    public async Task Strict_bounded_inputs_are_rejected(string kind)
    {
        var (f, r) = await Seed(); await using var owned = f;
        r = kind switch { "title" => r with { Title = " " }, "long-title" => r with { Title = new string('x', 201) }, "control-title" => r with { Title = "bad\ntitle" }, "provider" => r with { Provider = "https://untrusted.invalid" }, "duration" => r with { DurationMinutes = 0 }, "empty-batch" => r with { BatchId = Guid.Empty }, "empty-organizer" => r with { OrganizerId = Guid.Empty }, "null-students" => r with { StudentIds = null }, "empty-students" => r with { StudentIds = [] }, "duplicate-students" => r with { StudentIds = [r.StudentIds![0], r.StudentIds[0]] }, "empty-id" => r with { StudentIds = [Guid.Empty] }, _ => r with { StudentIds = Enumerable.Range(0, 101).Select(_ => Guid.NewGuid()).ToArray() } };
        var result = await Prepare(f, r); Assert.Equal(400, result.StatusCode); Assert.Equal("InvalidDraft", result.ReasonCode); Assert.Null(result.Draft);
    }
    [Theory] [InlineData("recurrence")] [InlineData("access")] [InlineData("chat")] [InlineData("recording")]
    public async Task Unqualified_controls_never_silently_downgrade(string kind)
    { var (f, r) = await Seed(); await using var owned = f; r = kind switch { "recurrence" => r with { Recurrence = "Weekly" }, "access" => r with { Access = "Open" }, "chat" => r with { Chat = "Enabled" }, _ => r with { Recording = "Auto" } }; Assert.Equal("UnqualifiedControls", (await Prepare(f, r)).ReasonCode); }
    [Theory] [InlineData("zone")] [InlineData("offset")] [InlineData("past")] [InlineData("seconds")]
    public async Task Explicit_zone_offset_and_future_minute_are_required(string kind)
    { var (f, r) = await Seed(); await using var owned = f; r = kind switch { "zone" => r with { TimeZone = "UTC" }, "offset" => r with { Start = r.Start.ToOffset(TimeSpan.Zero) }, "past" => r with { Start = r.Start.AddYears(-1) }, _ => r with { Start = r.Start.AddSeconds(1) } }; Assert.Equal(400, (await Prepare(f, r)).StatusCode); }
    [Theory] [InlineData("foreign-batch")] [InlineData("foreign-organizer")] [InlineData("foreign-student")]
    [InlineData("offline")] [InlineData("inactive-batch")] [InlineData("existing-link")]
    [InlineData("unassigned-organizer")] [InlineData("inactive-teacher")]
    [InlineData("completed-enrollment")] [InlineData("inactive-student")]
    public async Task Current_domain_sources_enforce_scope_and_eligibility(string kind)
    {
        var (f, r) = await Seed(); await using var owned = f;
        var b = await f.Db.Batches.SingleAsync(); var t = await f.Db.Teachers.SingleAsync(); var s = await f.Db.Students.SingleAsync(); var e = await f.Db.Enrollments.SingleAsync();
        switch(kind) { case "foreign-batch": b.AcademyId = Guid.NewGuid(); break; case "foreign-organizer": t.AcademyId = Guid.NewGuid(); break; case "foreign-student": s.AcademyId = Guid.NewGuid(); break; case "offline": b.DeliveryMode = "Offline"; break; case "inactive-batch": b.IsActive = false; break; case "existing-link": b.MeetingLink = "https://example.invalid/meeting"; break; case "unassigned-organizer": b.TeacherId = null; break; case "inactive-teacher": t.IsActive = false; break; case "completed-enrollment": e.Status = "Completed"; break; case "inactive-student": s.IsActive = false; break; }
        await f.Db.SaveChangesAsync(); var result = await Prepare(f, r); Assert.Equal(400, result.StatusCode); Assert.Equal("IneligibleSource", result.ReasonCode); Assert.Null(result.Draft);
    }
    [Theory] [InlineData("Teacher")] [InlineData("Student")] [InlineData("Manager")]
    public async Task Draft_preparation_cannot_expand_current_roles(string role)
    { var (f, r) = await Seed(role); await using var owned = f; Assert.Equal(403, (await Prepare(f, r)).StatusCode); }
    [Fact] public async Task Hashes_bind_inputs_and_current_source_versions_but_not_authority()
    {
        var (f, r) = await Seed(); await using var owned = f; var a = (await Prepare(f, r)).Draft!; var same = (await Prepare(f, r)).Draft!;
        Assert.Equal(a.SourceVersion, same.SourceVersion); Assert.Equal(a.RequestHash, same.RequestHash);
        var changed = (await Prepare(f, r with { Title = "Changed" })).Draft!; Assert.NotEqual(a.RequestHash, changed.RequestHash); Assert.NotEqual(a.SourceVersion, changed.SourceVersion);
        (await f.Db.Students.SingleAsync()).Email = "changed@example.invalid"; await f.Db.SaveChangesAsync(); var source = (await Prepare(f, r)).Draft!; Assert.Equal(a.RequestHash, source.RequestHash); Assert.NotEqual(a.SourceVersion, source.SourceVersion);
        await f.Users.RemoveFromRoleAsync(f.User, "Owner"); var denied = await Prepare(f, r); Assert.Equal(403, denied.StatusCode); Assert.Null(denied.Draft);
    }
    [Fact] public void Unknown_authority_guest_and_approval_fields_are_rejected_by_json_contract()
    {
        foreach (var key in new[] { "academyId", "role", "approved", "guestEmails", "sql", "timeZoneFromBrowser" })
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<MeetingPreviewRequest>("{\"" + key + "\":true}", new JsonSerializerOptions(JsonSerializerDefaults.Web)));
    }
    [Fact] public async Task Missing_or_malformed_recipient_is_not_silently_omitted()
    { var (f, r) = await Seed(); await using var owned = f; (await f.Db.Students.SingleAsync()).Email = null; await f.Db.SaveChangesAsync(); Assert.Equal("InvalidRecipient", (await Prepare(f, r)).ReasonCode); }

    [Theory] [InlineData("batch-start")] [InlineData("batch-end")] [InlineData("enrollment-start")] [InlineData("enrollment-end")]
    public async Task Meeting_date_must_be_inside_current_batch_and_enrollment_window(string kind)
    {
        var (f, r) = await Seed(); await using var owned = f; var date = DateOnly.FromDateTime(r.Start.DateTime);
        var b = await f.Db.Batches.SingleAsync(); var e = await f.Db.Enrollments.SingleAsync();
        switch(kind) { case "batch-start": b.StartDate = date.AddDays(1); break; case "batch-end": b.EndDate = date.AddDays(-1); break; case "enrollment-start": e.StartDate = date.AddDays(1); break; case "enrollment-end": e.EndDate = date.AddDays(-1); break; }
        await f.Db.SaveChangesAsync(); Assert.Equal("IneligibleSource", (await Prepare(f, r)).ReasonCode);
    }
    [Fact] public async Task Hybrid_and_full_minute_offset_are_supported_without_rounding()
    { var (f, r) = await Seed(); await using var owned = f; (await f.Db.Batches.SingleAsync()).DeliveryMode = "Hybrid"; await f.Db.SaveChangesAsync(); r = r with { Start = r.Start.AddMinutes(43) }; var result = await Prepare(f, r); Assert.Equal(200, result.StatusCode); Assert.Equal(r.Start.UtcDateTime, result.Draft!.StartUtc.UtcDateTime); }
    [Fact] public async Task Connection_generation_change_invalidates_fingerprint_without_enabling_execution()
    {
        var (f, r) = await Seed(); await using var owned = f; var before = (await Prepare(f, r)).Draft!;
        f.Db.CommunicationChannels.Add(new CommunicationChannel { AcademyId = f.Academy.Id, Channel = "Meeting", Provider = "GoogleWorkspace", Status = "Configured", HasSecureConnection = true }); await f.Db.SaveChangesAsync();
        var after = (await Prepare(f, r)).Draft!; Assert.NotEqual(before.SourceVersion, after.SourceVersion); Assert.False(after.CanExecute); Assert.Equal("NotIssued", after.ApprovalStatus);
    }
    [Fact] public async Task Nonexistent_DST_local_time_is_rejected_instead_of_shifted()
    {
        var (f, r) = await Seed(); await using var owned = f; f.Academy.TimeZone = "America/New_York"; await f.Db.SaveChangesAsync();
        r = r with { TimeZone = f.Academy.TimeZone, Start = new DateTimeOffset(2030, 3, 10, 2, 30, 0, TimeSpan.FromHours(-5)) };
        Assert.Equal("InvalidStart", (await Prepare(f, r)).ReasonCode);
    }
    [Fact] public async Task Explicit_DST_overlap_offsets_remain_distinct_instants()
    {
        var (f, r) = await Seed(); await using var owned = f; f.Academy.TimeZone = "America/New_York"; await f.Db.SaveChangesAsync();
        r = r with { TimeZone = f.Academy.TimeZone, Start = new DateTimeOffset(2030, 11, 3, 1, 30, 0, TimeSpan.FromHours(-4)) };
        var first = (await Prepare(f, r)).Draft!; var second = (await Prepare(f, r with { Start = new DateTimeOffset(2030, 11, 3, 1, 30, 0, TimeSpan.FromHours(-5)) })).Draft!;
        Assert.Equal(TimeSpan.FromHours(1), second.StartUtc - first.StartUtc); Assert.NotEqual(first.RequestHash, second.RequestHash);
    }
    [Theory] [InlineData("2030-10-15T17:00:00")] [InlineData("2030-10-15")] [InlineData("bad-time")]
    public void Json_start_must_have_explicit_offset(string value)
        => Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<MeetingPreviewRequest>("{\"start\":\"" + value + "\"}", new JsonSerializerOptions(JsonSerializerDefaults.Web)));
    [Theory] [InlineData("2030-10-15T17:00:00+05:30")] [InlineData("2030-10-15T11:30:00Z")]
    public void Explicit_offset_start_parses_without_server_timezone(string value)
    { var r = JsonSerializer.Deserialize<MeetingPreviewRequest>("{\"start\":\"" + value + "\"}", new JsonSerializerOptions(JsonSerializerDefaults.Web)); Assert.Equal(new DateTimeOffset(2030,10,15,11,30,0,TimeSpan.Zero), r!.Start.ToUniversalTime()); }
}
