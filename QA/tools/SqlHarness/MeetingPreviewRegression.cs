using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Domain.Identity;
using AcademyDesk.Api.Infrastructure.Meetings;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

internal static partial class SqlHarnessEntryPoint
{
    private static async Task VerifyMeetingDraftPreviewAsync(QaApiFactory factory, HttpClient client)
    {
        Guid academy, foreign, batch, teacher, student, foreignStudent; string database;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            database = db.Database.GetDbConnection().Database;
            academy = await db.Academies.Where(x => x.Name == "Synthetic Academy A").Select(x => x.Id).SingleAsync();
            foreign = await db.Academies.Where(x => x.Name == "Synthetic Academy B").Select(x => x.Id).SingleAsync();
            var t = await db.Teachers.SingleAsync(x => x.AcademyId == academy); t.Email = "teacher@example.invalid"; teacher = t.Id;
            var s = await db.Students.SingleAsync(x => x.AcademyId == academy); s.Email = "student@example.invalid"; student = s.Id;
            foreignStudent = await db.Students.Where(x => x.AcademyId == foreign).Select(x => x.Id).SingleAsync();
            var b = await db.Batches.SingleAsync(x => x.AcademyId == academy); b.DeliveryMode = "Online"; batch = b.Id;
            db.Enrollments.Add(new Enrollment { AcademyId = academy, BatchId = batch, StudentId = student }); await db.SaveChangesAsync();
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(); var u = (await users.FindByEmailAsync("qa-admin-a@example.invalid"))!; u.IsActive = true; u.AcademyId = academy;
            if (!(await users.UpdateAsync(u)).Succeeded) throw new InvalidOperationException("Meeting preview identity reset failed");
        }
        var auth = await LoginAsync(client, "qa-admin-a@example.invalid", "Synthetic!39Ab");
        var teacherAuth = await LoginAsync(client, "qa-teacher-a@example.invalid", "Synthetic!39Ab");
        client.DefaultRequestHeaders.Authorization = null;
        var start = new DateTimeOffset(DateTime.SpecifyKind(DateTime.UtcNow.Date.AddDays(2).AddHours(17).AddMinutes(43), DateTimeKind.Unspecified), TimeSpan.FromMinutes(330));
        var body = new MeetingPreviewRequest("GoogleWorkspace", batch, teacher, " Synthetic meeting draft ", start, "Asia/Kolkata", 60, [student], "Once", "InviteesOnly", "Disabled", "Off");
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web); var checks = new List<object>();
        async Task<(string Sources, int Audits)> Snapshot()
        {
            using var s = factory.Services.CreateScope(); var db = s.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            return (JsonSerializer.Serialize(new { batches = await db.Batches.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(), students = await db.Students.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(), enrollments = await db.Enrollments.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(), channels = await db.CommunicationChannels.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(), notifications = await db.Notifications.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync() }), await db.AuditLogs.CountAsync());
        }
        async Task<MeetingDraftPreview?> Check(string label, object? payload, string? token, Guid target, HttpStatusCode expected, string? raw = null)
        {
            await Task.Delay(700); var before = await Snapshot(); using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/academies/{target}/meetings/draft-preview");
            if (token is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Content = raw is null ? JsonContent.Create(payload, options: options) : new StringContent(raw, Encoding.UTF8, "application/json");
            using var response = await client.SendAsync(request);
            if (response.StatusCode != expected) throw new InvalidOperationException($"MEETING PREVIEW {label}: expected {(int)expected}, got {(int)response.StatusCode}");
            var resultBody = await response.Content.ReadAsStringAsync(); var after = await Snapshot();
            if (before.Sources != after.Sources || after.Audits != before.Audits + (expected == HttpStatusCode.OK ? 1 : 0)) throw new InvalidOperationException("Draft source/audit mutation mismatch");
            MeetingDraftPreview? draft = null;
            if (expected == HttpStatusCode.OK)
            {
                draft = JsonSerializer.Deserialize<MeetingDraftPreview>(resultBody, options)!;
                if (draft.CanExecute || draft.ApprovalStatus != "NotIssued" || draft.StartUtc != start.ToUniversalTime() || draft.StartUtc.Offset != TimeSpan.Zero || draft.Title != "Synthetic meeting draft" || !draft.AttendeeEmails.SequenceEqual(new[] { "student@example.invalid", "teacher@example.invalid" }) || draft.SourceVersion.Length != 64 || response.Headers.CacheControl?.NoStore != true) throw new InvalidOperationException("Unexpected executable/incorrect draft result");
                using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); var audit = await db.AuditLogs.OrderByDescending(x => x.OccurredAtUtc).FirstAsync();
                if (audit.ActorUserId is null || audit.MetadataJson?.Contains("student@example.invalid", StringComparison.Ordinal) == true) throw new InvalidOperationException("Unsafe draft audit");
            }
            else if (resultBody.Contains("student@example.invalid", StringComparison.Ordinal)) throw new InvalidOperationException("Denied preview leaked recipients");
            checks.Add(new { label, status = (int)expected, sourcesUnchanged = true, requiredAudit = expected == HttpStatusCode.OK });
            Console.WriteLine($"MEETING PREVIEW CASE {label} PASS status={(int)expected}; sources unchanged, audit verified."); return draft;
        }
        var first = await Check("owned-valid-full-minute", body, auth, academy, HttpStatusCode.OK);
        var same = await Check("repeat-draft-no-provider-write", body, auth, academy, HttpStatusCode.OK);
        if (first!.SourceVersion != same!.SourceVersion) throw new InvalidOperationException("Repeated unchanged preview changed source version");
        await Check("teacher-denied", body, teacherAuth, academy, HttpStatusCode.Forbidden);
        await Check("foreign-route", body, auth, foreign, HttpStatusCode.Forbidden);
        await Check("anonymous", body, null, academy, HttpStatusCode.Unauthorized);
        foreach (var field in new[] { "academyId", "role", "approved", "guestEmails", "sql" })
        { var json = JsonSerializer.SerializeToNode(body, options)!.AsObject(); json[field] = true; await Check("unknown-" + field, null, auth, academy, HttpStatusCode.BadRequest, json.ToJsonString()); }
        var missingOffset = JsonSerializer.SerializeToNode(body, options)!.AsObject(); missingOffset["start"] = start.ToString("yyyy-MM-ddTHH:mm:ss");
        await Check("missing-timezone-offset", null, auth, academy, HttpStatusCode.BadRequest, missingOffset.ToJsonString());
        await Check("null-body", null, auth, academy, HttpStatusCode.BadRequest);
        await Check("foreign-recipient", body with { StudentIds = [foreignStudent] }, auth, academy, HttpStatusCode.BadRequest);
        await Check("duplicate-recipient", body with { StudentIds = [student, student] }, auth, academy, HttpStatusCode.BadRequest);
        await Check("unqualified-recording", body with { Recording = "Auto" }, auth, academy, HttpStatusCode.BadRequest);
        await Check("unqualified-recurrence", body with { Recurrence = "Weekly" }, auth, academy, HttpStatusCode.BadRequest);
        await Check("offset-mismatch", body with { Start = start.ToOffset(TimeSpan.Zero) }, auth, academy, HttpStatusCode.BadRequest);
        using (var s = factory.Services.CreateScope()) { var db = s.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); (await db.Enrollments.SingleAsync(x => x.StudentId == student)).Status = "Completed"; await db.SaveChangesAsync(); }
        await Check("completed-enrollment", body, auth, academy, HttpStatusCode.BadRequest);
        using (var s = factory.Services.CreateScope()) { var db = s.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); (await db.Enrollments.SingleAsync(x => x.StudentId == student)).Status = "Active"; (await db.Batches.SingleAsync(x => x.Id == batch)).DeliveryMode = "Hybrid"; await db.SaveChangesAsync(); }
        var hybrid = await Check("hybrid-source-changed", body, auth, academy, HttpStatusCode.OK);
        if (first.SourceVersion == hybrid!.SourceVersion) throw new InvalidOperationException("Changed batch did not invalidate fingerprint");
        using (var s = factory.Services.CreateScope()) { var users = s.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(); var u = (await users.FindByEmailAsync("qa-admin-a@example.invalid"))!; if (!(await users.RemoveFromRoleAsync(u, "AcademyAdmin")).Succeeded) throw new InvalidOperationException("Preview role revoke failed"); }
        await Check("role-revoked-old-ticket", body, auth, academy, HttpStatusCode.Forbidden);
        if (checks.Count != 20) throw new InvalidOperationException("Meeting preview case count mismatch " + checks.Count);
        var evidence = Path.Combine("QA", "EVIDENCE", "meeting-preview-sql-" + database["AcademyDesk_QA_".Length..]); Directory.CreateDirectory(evidence);
        var sources = new[] { "apps/api/Controllers/MeetingsController.cs", "apps/api/Infrastructure/Meetings/MeetingPreviewService.cs", "apps/api/Infrastructure/Meetings/MeetingStartConverter.cs", "QA/tools/SqlHarness/MeetingPreviewRegression.cs" }.ToDictionary(path => path, path => Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(path))));
        await File.WriteAllTextAsync(Path.Combine(evidence, "result.json"), JsonSerializer.Serialize(new { database, checks, sources, transport = "Real Identity/HTTP/disposable SQL; draft only, no provider/approval/execution" }, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine("MEETING PREVIEW PASS: 20 real HTTP/SQL cases; required audit, no source/provider writes or execution.");
    }
}
