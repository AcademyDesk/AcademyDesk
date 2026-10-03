using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Domain.Identity;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

internal static partial class SqlHarnessEntryPoint
{
    private static async Task VerifyMakeupLocationAsync(QaApiFactory factory, HttpClient client)
    {
        Guid academy, foreign, student, teacher, foreignStudent, foreignTeacher, foreignBatch, course, parent, actor;
        const string link = "https://meeting.example.invalid/makeup";
        var start = DateTime.UtcNow.Date.AddDays(30).AddHours(4); var count = 0;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            academy = await db.Academies.Where(x => x.Name == "Synthetic Academy A").Select(x => x.Id).SingleAsync();
            foreign = await db.Academies.Where(x => x.Name == "Synthetic Academy B").Select(x => x.Id).SingleAsync();
            student = await db.Students.Where(x => x.AcademyId == academy).Select(x => x.Id).SingleAsync();
            foreignStudent = await db.Students.Where(x => x.AcademyId == foreign).Select(x => x.Id).SingleAsync();
            var original = await db.Batches.SingleAsync(x => x.AcademyId == academy); course = original.CourseId; teacher = original.TeacherId!.Value;
            var foreignT = new Teacher { AcademyId = foreign, FirstName = "Foreign", LastName = "Makeup" }; foreignTeacher = foreignT.Id;
            var foreignB = new Batch { AcademyId = foreign, CourseId = Guid.NewGuid(), Name = "Foreign makeup" }; foreignBatch = foreignB.Id;
            db.AddRange(foreignT, foreignB, new MakeupClass { AcademyId = foreign, StudentId = foreignStudent, BatchId = foreignBatch, StartUtc = start, EndUtc = start.AddHours(1), DeliveryMode = "Offline", Venue = "Foreign preserved room", Status = "Completed" });
            var allowed = new Guardian { AcademyId = academy, FirstName = "Allowed", LastName = "Parent" }; parent = allowed.Id; db.Add(allowed);
            db.Add(new StudentGuardian { AcademyId = academy, StudentId = student, GuardianId = parent, CanAccessPortal = true });
            foreach (var variant in new[] { "revoked", "disabled", "foreign" })
            {
                var guardian = new Guardian { AcademyId = variant == "foreign" ? foreign : academy, FirstName = variant, LastName = "Parent" }; db.Add(guardian);
                db.Add(new StudentGuardian { AcademyId = guardian.AcademyId, StudentId = student, GuardianId = guardian.Id, CanAccessPortal = variant != "disabled", AccessRevokedAtUtc = variant == "revoked" ? DateTime.UtcNow : null });
            }
            await db.SaveChangesAsync();
            actor = (await scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>().FindByEmailAsync("qa-admin-a@example.invalid"))!.Id;
        }
        var delegatedActor = await CreateAccessActorAsync(factory, "qa-makeup-granted@example.invalid", "QA-MakeupGranted", academy, permissions: "[\"makeup.manage\"]");
        await CreateAccessActorAsync(factory, "qa-makeup-denied@example.invalid", "QA-NoMakeup", academy);
        var token = await LoginAsync(client, "qa-admin-a@example.invalid", "Synthetic!39Ab");
        var foreignToken = await LoginAsync(client, "qa-admin-b@example.invalid", "Synthetic!39Ab");
        var teacherToken = await LoginAsync(client, "qa-teacher-a@example.invalid", "Synthetic!39Ab");
        var delegated = await LoginAsync(client, "qa-makeup-granted@example.invalid", "Synthetic!39Ab");
        var deniedOperations = await LoginAsync(client, "qa-makeup-denied@example.invalid", "Synthetic!39Ab");
        client.DefaultRequestHeaders.Authorization = null;
        async Task<Guid> Prepare(string sourceMode, string? location, bool nullTeacher = false, bool noNext = false)
        {
            using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var batch = new Batch { AcademyId = academy, CourseId = course, Name = "Synthetic makeup " + count, TeacherId = teacher, SessionMinutes = 75 }; db.Add(batch);
            db.AddRange(new ClassSession { AcademyId = academy, BatchId = batch.Id, TeacherId = nullTeacher ? null : teacher, StartUtc = start, EndUtc = start.AddMinutes(45), DeliveryMode = sourceMode, RoomName = location, Status = noNext ? "Cancelled" : "Scheduled" },
                new ClassSession { AcademyId = academy, BatchId = batch.Id, TeacherId = teacher, StartUtc = start.AddDays(1), EndUtc = start.AddDays(1).AddMinutes(45), DeliveryMode = "Online", RoomName = "Wrong later link", Status = noNext ? "Cancelled" : "Scheduled" },
                new ClassSession { AcademyId = foreign, BatchId = batch.Id, StartUtc = start.AddHours(-1), EndUtc = start, DeliveryMode = "Offline", RoomName = "Wrong foreign room" },
                new ClassSession { AcademyId = academy, BatchId = batch.Id, StartUtc = DateTime.UtcNow.AddDays(-1), EndUtc = DateTime.UtcNow.AddHours(-23), DeliveryMode = "Offline", RoomName = "Wrong past room" });
            await db.SaveChangesAsync(); return batch.Id;
        }
        object Payload(Guid batch, bool next, string? mode = "Offline", string? location = link, Guid? sid = null, Guid? tid = null, bool missingDate = false) =>
            new { studentId = sid ?? student, batchId = batch, teacherId = tid, startUtc = missingDate ? (DateTime?)null : start, deliveryMode = mode, venue = " Studio A ", meetingLink = location, useNextScheduledClass = next, notes = " Notes " };
        async Task<string> Snapshot()
        {
            using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            return JsonSerializer.Serialize(new { makeups = await db.MakeupClasses.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), notifications = await db.Notifications.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), audits = await db.AuditLogs.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), batches = await db.Batches.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), sessions = await db.ClassSessions.AsNoTracking().OrderBy(x => x.Id).ToListAsync() });
        }
        async Task<HttpResponseMessage> Send(HttpMethod method, Guid target, object? payload, string? auth)
        {
            await Task.Delay(550); using var request = new HttpRequestMessage(method, $"/api/academies/{target}/makeup-classes");
            if (auth is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth);
            if (payload is not null) request.Content = JsonContent.Create(payload); return await client.SendAsync(request);
        }
        void Pass(string label) { count++; Console.WriteLine($"MAKEUPLOCATION CASE {label} PASS."); }
        async Task Valid(string label, Guid batch, bool next, string? requestMode, string canonical, string? location, string? auth = null, Guid? expectedActor = null)
        {
            using var before = JsonDocument.Parse(await Snapshot());
            using var response = await Send(HttpMethod.Post, academy, Payload(batch, next, requestMode), auth ?? token); RequireFinanceStatus(response, HttpStatusCode.OK, label);
            using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync()); var id = body.RootElement.GetProperty("id").GetGuid();
            using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var all = await db.MakeupClasses.AsNoTracking().OrderBy(x => x.Id).ToListAsync(); var row = all.Single(x => x.Id == id);
            if (all.Count != before.RootElement.GetProperty("makeups").GetArrayLength() + 1 || JsonSerializer.Serialize(all.Where(x => x.Id != id)) != before.RootElement.GetProperty("makeups").GetRawText() || row.AcademyId != academy || row.StudentId != student || row.BatchId != batch || row.TeacherId != (next ? teacher : (Guid?)null) || row.StartUtc != start || row.EndUtc != start.AddMinutes(next ? 45 : 75) || row.DeliveryMode != canonical || row.Venue != (canonical == "Offline" ? location : null) || row.MeetingLink != (canonical == "Offline" ? null : location) || row.UsesNextScheduledClass != next || row.Status != "Scheduled" || row.Notes != "Notes") throw new InvalidOperationException("Makeup fresh SQL/inheritance/mode mismatch: " + label);
            var summary = new AcademyDesk.Api.Controllers.MakeupSummary(row.Id, row.StudentId, row.BatchId, row.TeacherId, row.StartUtc, row.EndUtc, row.DeliveryMode, row.Venue, row.MeetingLink, row.UsesNextScheduledClass, row.Status, row.Notes);
            // SQL datetime2 preserves UTC ticks but not DateTime.Kind; compare typed fields, not textual Z suffixes.
            if (body.RootElement.Deserialize<AcademyDesk.Api.Controllers.MakeupSummary>(new JsonSerializerOptions(JsonSerializerDefaults.Web)) != summary) throw new InvalidOperationException("Response fields differ from fresh SQL summary");
            var notifications = await db.Notifications.AsNoTracking().OrderBy(x => x.Id).ToListAsync();
            var oldNotices = before.RootElement.GetProperty("notifications"); var oldIds = oldNotices.EnumerateArray().Select(x => x.GetProperty("Id").GetGuid()).ToHashSet(); var added = notifications.Where(x => !oldIds.Contains(x.Id)).ToArray();
            if (added.Length != 2 || JsonSerializer.Serialize(notifications.Where(x => oldIds.Contains(x.Id))) != oldNotices.GetRawText() || !added.Any(x => x.RecipientId == student && x.RecipientType == "Student") || !added.Any(x => x.RecipientId == parent && x.RecipientType == "Parent")) throw new InvalidOperationException("Recipient isolation mismatch");
            foreach (var notice in added) if (notice.AcademyId != academy || notice.Channel != "InApp" || notice.Status != "Queued" || notice.SentAtUtc != null || !notice.Message.EndsWith(canonical + (location == null ? "" : " · " + location), StringComparison.Ordinal) || !notice.Message.Contains("IST") || notice.Message.Contains("Wrong ")) throw new InvalidOperationException("Queued notice lost/used wrong location");
            var audits = await db.AuditLogs.AsNoTracking().OrderBy(x => x.Id).ToListAsync(); var oldAudits = before.RootElement.GetProperty("audits"); var oldAuditIds = oldAudits.EnumerateArray().Select(x => x.GetProperty("Id").GetGuid()).ToHashSet();
            var audit = audits.Single(x => !oldAuditIds.Contains(x.Id));
            if (JsonSerializer.Serialize(audits.Where(x => oldAuditIds.Contains(x.Id))) != oldAudits.GetRawText() || audit.AcademyId != academy || audit.ActorUserId != (expectedActor ?? actor) || audit.Action != "POST MakeupClasses" || audit.EntityType != "MakeupClasses") throw new InvalidOperationException("Success audit mismatch");
            using var after = JsonDocument.Parse(await Snapshot()); foreach (var field in new[] { "batches", "sessions" }) if (before.RootElement.GetProperty(field).GetRawText() != after.RootElement.GetProperty(field).GetRawText()) throw new InvalidOperationException("Makeup changed scheduled source");
            Pass(label);
        }
        async Task Denied(string label, object payload, Guid? target = null, string? auth = null, HttpStatusCode expected = HttpStatusCode.BadRequest)
        {
            var before = await Snapshot(); using var response = await Send(HttpMethod.Post, target ?? academy, payload, auth); RequireFinanceStatus(response, expected, label);
            if (before != await Snapshot()) throw new InvalidOperationException("Denied request changed makeup/notification/audit/source rows: " + label); Pass(label);
        }
        foreach (var mode in new string?[] { "Offline", " offline ", "Online", " online ", "Hybrid", " hybrid ", null })
        { var canonical = mode?.Trim().Equals("Online", StringComparison.OrdinalIgnoreCase) == true ? "Online" : mode?.Trim().Equals("Hybrid", StringComparison.OrdinalIgnoreCase) == true ? "Hybrid" : "Offline"; var batch = await Prepare("InPerson", "Unused source"); await Valid("manual-" + count + "-" + canonical, batch, false, mode, canonical, canonical == "Offline" ? "Studio A" : link); }
        foreach (var mode in new[] { "Offline", " offline ", "Online", " online ", "Hybrid", " hybrid ", "InPerson", " inperson " })
        { var canonical = mode.Trim().Equals("Online", StringComparison.OrdinalIgnoreCase) ? "Online" : mode.Trim().Equals("Hybrid", StringComparison.OrdinalIgnoreCase) ? "Hybrid" : "Offline"; var location = canonical == "Offline" ? "Studio A" : link; var batch = await Prepare(mode, " " + location + " "); await Valid("inherited-" + count + "-" + canonical, batch, true, "Offline", canonical, location); }
        foreach (var (mode, room) in new[] { ("InPerson", (string?)null), ("Offline", ""), (" inperson ", "  ") }) { var batch = await Prepare(mode, room); await Valid("optional-room-" + count, batch, true, "Offline", "Offline", null); }
        var fallbackBatch = await Prepare("InPerson", "Studio A", nullTeacher: true); await Valid("inherited-batch-teacher-fallback", fallbackBatch, true, "Offline", "Offline", "Studio A");
        var grantedBatch = await Prepare("Hybrid", link); await Valid("existing-makeup-manage-grant", grantedBatch, true, "Offline", "Hybrid", link, delegated, delegatedActor);
        foreach (var next in new[] { false, true }) foreach (var mode in new[] { "Online", " hybrid " }) foreach (var missing in new string?[] { null, "", "  " })
        { var batch = await Prepare(mode, missing); await Denied("missing-virtual-link-" + count, Payload(batch, next, mode, missing), auth: token); }
        foreach (var mode in new[] { "Unknown", "", "  ", "InPerson" }) { var batch = await Prepare("Offline", "Studio A"); await Denied("unsupported-manual-" + count, Payload(batch, false, mode), auth: token); }
        foreach (var mode in new[] { "Unknown", "", "  " }) { var batch = await Prepare(mode, "Studio A"); await Denied("unsupported-source-" + count, Payload(batch, true), auth: token); }
        var noNext = await Prepare("Offline", "Studio A", noNext: true); await Denied("no-owned-upcoming-scheduled-class", Payload(noNext, true), auth: token);
        var guardBatch = await Prepare("InPerson", "Studio A");
        await Denied("foreign-student", Payload(guardBatch, false, sid: foreignStudent), auth: token);
        await Denied("foreign-batch", Payload(foreignBatch, false), auth: token);
        await Denied("foreign-teacher", Payload(guardBatch, false, tid: foreignTeacher), auth: token);
        await Denied("missing-manual-date", Payload(guardBatch, false, missingDate: true), auth: token);
        await Denied("foreign-route", Payload(foreignBatch, false, sid: foreignStudent), foreign, token, HttpStatusCode.Forbidden);
        await Denied("anonymous", Payload(guardBatch, false), expected: HttpStatusCode.Unauthorized);
        await Denied("teacher-no-grant", Payload(guardBatch, false), auth: teacherToken, expected: HttpStatusCode.Forbidden);
        await Denied("custom-role-no-grant", Payload(guardBatch, false), auth: deniedOperations, expected: HttpStatusCode.Forbidden);
        JsonElement owned = default;
        foreach (var target in new[] { (Id: academy, Token: token), (Id: foreign, Token: foreignToken) })
        {
            var before = await Snapshot(); using var response = await Send(HttpMethod.Get, target.Id, null, target.Token); RequireFinanceStatus(response, HttpStatusCode.OK, "owned-list");
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var rows = await db.MakeupClasses.AsNoTracking().Where(x => x.AcademyId == target.Id).OrderBy(x => x.StartUtc).Select(x => new AcademyDesk.Api.Controllers.MakeupSummary(x.Id, x.StudentId, x.BatchId, x.TeacherId, x.StartUtc, x.EndUtc, x.DeliveryMode, x.Venue, x.MeetingLink, x.UsesNextScheduledClass, x.Status, x.Notes)).ToListAsync();
            // Ties have no promised secondary ordering; compare complete summaries by ID.
            var actual = json.RootElement.EnumerateArray().OrderBy(x => x.GetProperty("id").GetGuid()).Select(x => x.GetRawText()).ToArray(); var expected = rows.OrderBy(x => x.Id).Select(x => JsonSerializer.Serialize(x, new JsonSerializerOptions(JsonSerializerDefaults.Web))).ToArray();
            if (!actual.SequenceEqual(expected) || before != await Snapshot()) throw new InvalidOperationException("GET scope/readback/source mismatch");
            if (target.Id == academy) owned = json.RootElement.Clone(); Pass(target.Id == academy ? "owned-list-response-fresh-SQL" : "foreign-owned-list-preserved-row");
        }
        Console.WriteLine("MAKEUPLOCATION FIXTURE " + JsonSerializer.Serialize(new { makeups = owned }, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
        if (count != 50) throw new InvalidOperationException("Unexpected makeup case count: " + count);
        Console.WriteLine("MAKEUPLOCATION REGRESSION PASS:50 cases; manual/inherited Offline Online Hybrid and InPerson alias, only relevant location, optional physical rooms/required virtual links, teacher fallback and earliest owned future selection, scoped GET/fresh SQL/response/queued student-parent notices/success audit, no rejected writes; no external notification delivery; browser/status/concurrency/audit rollback/release pending.");
    }
}
