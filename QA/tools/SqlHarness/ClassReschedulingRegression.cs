using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Intelligence.Penta;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

internal static partial class SqlHarnessEntryPoint
{
    private static async Task VerifyClassReschedulingAsync(QaApiFactory factory, QaRunManifest manifest, Guid academy, Guid foreign,
        Guid actor, Guid conversation, Guid batch, Guid foreignBatch, Guid teacher, Guid branch, DateTime start,
        HttpClient admin, HttpClient manual, HttpClient teacherClient, HttpClient anonymous, IReadOnlyDictionary<string, string?> flags)
    {
        const string adminEmail = "qa-admin-a@example.invalid";
        const string approveMove = "ApproveClassSessionReschedule";
        const string moveAudit = "PentaClassSessionRescheduled";
        const string meeting = "https://meeting.example.invalid/moved";
        using var createOnlyHost = new QaApiFactory(manifest, new Dictionary<string, string?>
        {
            ["Penta:Enabled"] = "true", ["Penta:ClassSessionSchedulingEnabled"] = "true"
        });
        using var createOnly = await SignIn(createOnlyHost);
        var count = 0;
        var sessions = $"/api/academies/{academy}/sessions";
        string NewDraft(Guid? tenant = null) =>
            $"/api/academies/{tenant ?? academy}/penta/class-scheduling/conversations/{conversation}/drafts/{Guid.NewGuid()}";

        static async Task<HttpClient> SignIn(QaApiFactory host)
        {
            var client = host.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await LoginAsync(client, adminEmail, "Synthetic!39Ab"));
            return client;
        }

        async Task<T> WithDb<T>(Func<AcademyDeskDbContext, Task<T>> work)
        {
            using var scope = factory.Services.CreateScope();
            return await work(scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>());
        }

        Task<string> State(bool full = true) => WithDb(async db => JsonSerializer.Serialize(new
        {
            classes = await db.ClassSessions.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
            attendance = await db.AttendanceRecords.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
            notices = await db.Notifications.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
            drafts = full ? await db.PentaOnboardingDrafts.AsNoTracking().OrderBy(x => x.Id).ToListAsync() : null,
            audits = full ? await db.AuditLogs.AsNoTracking().OrderBy(x => x.Id).ToListAsync() : null
        }));
        void Pass(string label) { count++; Console.WriteLine($"SCHEDULEDEFAULTS MOVE CASE {label} PASS"); }
        async Task Refuse(Func<Task<HttpResponseMessage>> send, HttpStatusCode status, string label)
        {
            var before = await State();
            using var response = await send();
            RequireFinanceStatus(response, status, label);
            if (before != await State()) throw new InvalidOperationException(label + " changed SQL state.");
            Pass(label);
        }
        Task<ClassSession> Seed(DateTime time, string status = "Scheduled", bool foreignSession = false) => WithDb(async db =>
        {
            var row = new ClassSession { AcademyId = foreignSession ? foreign : academy, BatchId = foreignSession ? foreignBatch : batch,
                TeacherId = foreignSession ? null : teacher, BranchId = foreignSession ? null : branch,
                StartUtc = time, EndUtc = time.AddHours(1), RoomName = "Initial studio", Status = status };
            db.ClassSessions.Add(row);
            await db.SaveChangesAsync();
            return row;
        });
        Task<HttpResponseMessage> Prepare(HttpClient client, string route, ClassSession source, DateTime slot, string mode = "Offline",
            string? location = "Moved studio", string? injected = null, long context = 0)
        {
            var fields = new Dictionary<string, object>
            {
                ["SessionId"] = new { kind = "reference", value = source.Id.ToString("D") },
                ["StartUtc"] = new { kind = "text", value = slot.ToString("O") },
                ["EndUtc"] = new { kind = "text", value = slot.AddHours(1).ToString("O") },
                ["DeliveryMode"] = new { kind = "text", value = mode }
            };
            if (location is not null) fields[mode == "Offline" ? "RoomName" : "MeetingLink"] = new { kind = "text", value = location };
            if (injected is not null) fields[injected] = new { kind = "text", value = "Cancelled" };
            return client.PutAsJsonAsync(route, new { contextVersion = context,
                payload = new { tool_key = "AcademyDesk.ClassSession.Reschedule", tool_revision = 1, fields } }, new JsonSerializerOptions());
        }
        static Task<HttpResponseMessage> Approve(HttpClient client, string route, PentaClassSessionDraftView draft, string intent = approveMove) =>
            client.PostAsJsonAsync(route + "/approve", new { revision = draft.Revision, payloadHash = draft.PayloadHash, intent });
        static Task<HttpResponseMessage> Execute(HttpClient client, string route, PentaClassSessionDraftView draft, Guid? approval = null) =>
            client.PostAsJsonAsync(route + "/execute", new { revision = draft.Revision, payloadHash = draft.PayloadHash, approvalId = approval ?? draft.ApprovalId });
        static async Task<PentaClassSessionDraftView> Read(Task<HttpResponseMessage> send, HttpStatusCode status, string label)
        {
            using var response = await send;
            RequireFinanceStatus(response, status, label);
            if (response.Headers.CacheControl?.NoStore != true) throw new InvalidOperationException("Reschedule response must be no-store.");
            return (await response.Content.ReadFromJsonAsync<PentaClassSessionDraftView>())!;
        }
        async Task EditManually(ClassSession source, DateTime slot)
        {
            using var response = await manual.PutAsJsonAsync($"{sessions}/{source.Id}",
                new UpdateClassSessionRequest(slot, slot.AddHours(1), source.DeliveryMode, source.RoomName, source.Status));
            RequireFinanceStatus(response, HttpStatusCode.OK, "Manual source update");
        }

        var first = await Seed(start);
        var foreignClass = await Seed(start, foreignSession: true);
        var cancelled = await Seed(start.AddDays(1), "Cancelled");
        var completed = await Seed(start.AddDays(2), "Completed");
        var past = await Seed(DateTime.UtcNow.Date.AddDays(-2));
        var blocked = await Seed(start.AddDays(3));
        var target = start.AddDays(4);

        await Refuse(() => Prepare(createOnly, NewDraft(), first, target), HttpStatusCode.NotFound, "move disabled while create remains enabled");
        await Refuse(() => Prepare(anonymous, NewDraft(), first, target), HttpStatusCode.Unauthorized, "anonymous move");
        await Refuse(() => Prepare(teacherClient, NewDraft(), first, target), HttpStatusCode.Forbidden, "teacher cannot approve admin schedule");
        await Refuse(() => Prepare(admin, NewDraft(foreign), foreignClass, target), HttpStatusCode.Forbidden, "foreign academy route");
        await Refuse(() => Prepare(admin, NewDraft(), foreignClass, target), HttpStatusCode.NotFound, "foreign session id");
        await Refuse(() => Prepare(admin, NewDraft(), cancelled, target), HttpStatusCode.Conflict, "cancelled class cannot move");
        await Refuse(() => Prepare(admin, NewDraft(), completed, target), HttpStatusCode.Conflict, "completed class cannot move");
        await Refuse(() => Prepare(admin, NewDraft(), past, target), HttpStatusCode.Conflict, "past class cannot move");
        await Refuse(() => Prepare(admin, NewDraft(), first, DateTime.UtcNow.Date.AddDays(-3)), HttpStatusCode.BadRequest, "past destination");
        await Refuse(() => Prepare(admin, NewDraft(), first, start, location: first.RoomName), HttpStatusCode.BadRequest, "no change");
        await Refuse(() => Prepare(admin, NewDraft(), first, target, "Hybrid", "http://meeting.example.invalid"), HttpStatusCode.BadRequest, "unsafe meeting link");
        await Refuse(() => Prepare(admin, NewDraft(), first, target, injected: "Status"), HttpStatusCode.BadRequest, "payload cannot change status");
        await Refuse(() => Prepare(admin, NewDraft(), first, target, injected: "TeacherId"), HttpStatusCode.BadRequest, "payload cannot change assignment");
        await Refuse(() => Prepare(admin, NewDraft(), first, blocked.StartUtc), HttpStatusCode.Conflict, "clashing destination");

        var student = await WithDb(db => db.Students.Where(x => x.AcademyId == academy).Select(x => x.Id).FirstAsync());
        var attendance = new AttendanceRecord { AcademyId = academy, StudentId = student, ClassSessionId = first.Id,
            Status = "Excused", Notes = "Synthetic existing note" };
        await WithDb(db => { db.AttendanceRecords.Add(attendance); return db.SaveChangesAsync(); });
        var initial = await State(false);
        var route = NewDraft();
        var draft = await Read(Prepare(admin, route, first, target, "Hybrid", meeting), HttpStatusCode.Created, "move preview");
        if (draft.Source is not { } source || source.Id != first.Id || source.StartUtc != first.StartUtc || source.RoomName != "Initial studio" ||
            draft.Plan.Id != first.Id || draft.Plan.StartUtc != target || draft.Plan.EndUtc != target.AddHours(1) ||
            draft.Plan.TeacherId != teacher || draft.Plan.BranchId != branch || draft.Plan.BatchId != batch ||
            draft.Plan.DeliveryMode != "Hybrid" || draft.Plan.RoomName != meeting || draft.Plan.Status != "Scheduled" || initial != await State(false))
            throw new InvalidOperationException("Move preview does not match the exact source, target or no-write rule.");
        Pass("exact before and after preview without changing class or attendance");
        await Refuse(() => Approve(admin, route, draft, "ApproveClassSessionSchedule"), HttpStatusCode.Conflict, "create intent cannot approve move");
        await Refuse(() => Execute(admin, route, draft, Guid.NewGuid()), HttpStatusCode.Conflict, "unapproved move cannot execute");
        await Refuse(() => Prepare(admin, route, first, target.AddHours(1)), HttpStatusCode.Conflict, "retry key cannot change plan");
        var approved = await Read(Approve(admin, route, draft), HttpStatusCode.OK, "approve move");
        if (approved.ApprovalId is null || initial != await State(false))
            throw new InvalidOperationException("Move approval changed business state.");
        Pass("separate approval preserves business rows");
        await Refuse(() => createOnly.GetAsync(route), HttpStatusCode.NotFound, "move draft access follows feature flag");
        await Refuse(() => Execute(admin, route, approved, Guid.NewGuid()), HttpStatusCode.Conflict, "wrong move approval id");

        var fault = new EnrollmentSharedAuditFault(moveAudit);
        using (var faultyHost = new QaApiFactory(manifest, flags, isolatedDomainInterceptor: fault))
        using (var faulty = await SignIn(faultyHost))
            await Refuse(() => Execute(faulty, route, approved), HttpStatusCode.ServiceUnavailable, "required audit rollback");
        if (fault.Hits != 1) throw new InvalidOperationException("Reschedule audit failure was not reached.");

        var notices = await WithDb(db => db.Notifications.CountAsync());
        var pair = await Task.WhenAll(Execute(admin, route, approved), Execute(admin, route, approved));
        using (pair[0]) using (pair[1])
        {
            if (!pair.Select(x => (int)x.StatusCode).Order().SequenceEqual(new[] { 200, 201 }))
                throw new InvalidOperationException("Same draft should commit one move and replay one receipt.");
            var reply = (await pair[0].Content.ReadFromJsonAsync<PentaClassSessionDraftView>())!;
            if (reply.Receipt is not { } receipt || receipt.ClassSessionId != first.Id ||
                receipt != (await pair[1].Content.ReadFromJsonAsync<PentaClassSessionDraftView>())?.Receipt)
                throw new InvalidOperationException("Move receipts differ.");
            await WithDb(async db =>
            {
                var row = await db.ClassSessions.SingleAsync(x => x.Id == first.Id && x.AcademyId == academy);
                var prior = await db.AttendanceRecords.SingleAsync(x => x.Id == attendance.Id);
                if (row.BatchId != batch || row.TeacherId != teacher || row.BranchId != branch || row.Status != "Scheduled" ||
                    row.StartUtc != target || row.EndUtc != target.AddHours(1) || row.DeliveryMode != "Hybrid" || row.RoomName != meeting ||
                    prior.ClassSessionId != row.Id || prior.Status != "Excused" || prior.Notes != attendance.Notes ||
                    await db.Notifications.CountAsync() != notices || !await db.AuditLogs.AnyAsync(x => x.Id == receipt.AuditId &&
                        x.AcademyId == academy && x.ActorUserId == actor && x.Action == moveAudit))
                    throw new InvalidOperationException("Moved class, preserved attendance or audit differs from the preview.");
                return true;
            });
            var after = await State();
            if ((await Read(admin.GetAsync(route), HttpStatusCode.OK, "read move receipt")).Receipt != receipt || after != await State())
                throw new InvalidOperationException("Receipt read changed SQL.");
        }
        Pass("same approved move commits once, keeps attendance and recovers receipt");

        var edited = await Seed(start.AddDays(5));
        var editedRoute = NewDraft();
        var oldDraft = await Read(Prepare(admin, editedRoute, edited, start.AddDays(6)), HttpStatusCode.Created, "preview before manual edit");
        var oldApproval = await Read(Approve(admin, editedRoute, oldDraft), HttpStatusCode.OK, "approve before manual edit");
        await EditManually(edited, edited.StartUtc.AddHours(2));
        await EditManually(edited, edited.StartUtc);
        await Refuse(() => Execute(admin, editedRoute, oldApproval), HttpStatusCode.Conflict, "move away and back makes earlier approval stale");

        var open = await Seed(start.AddDays(7));
        var clashRoute = NewDraft();
        var clashTime = start.AddDays(8);
        var clash = await Read(Prepare(admin, clashRoute, open, clashTime), HttpStatusCode.Created, "preview before late clash");
        var clashApproval = await Read(Approve(admin, clashRoute, clash), HttpStatusCode.OK, "approve before late clash");
        using (var occupied = await manual.PostAsJsonAsync(sessions,
            new CreateClassSessionRequest(batch, teacher, branch, clashTime, clashTime.AddHours(1), "InPerson", "Later studio")))
            RequireFinanceStatus(occupied, HttpStatusCode.Created, "Manual destination claim");
        await Refuse(() => Execute(admin, clashRoute, clashApproval), HttpStatusCode.Conflict, "new Manual clash invalidates approved move");

        var competing = await Seed(start.AddDays(9));
        var leftRoute = NewDraft();
        var rightRoute = NewDraft();
        var left = await Read(Prepare(admin, leftRoute, competing, start.AddDays(10)), HttpStatusCode.Created, "first competing preview");
        var right = await Read(Prepare(admin, rightRoute, competing, start.AddDays(11)), HttpStatusCode.Created, "second competing preview");
        var leftApproval = await Read(Approve(admin, leftRoute, left), HttpStatusCode.OK, "first competing approval");
        var rightApproval = await Read(Approve(admin, rightRoute, right), HttpStatusCode.OK, "second competing approval");
        var auditCount = await WithDb(db => db.AuditLogs.CountAsync(x => x.AcademyId == academy && x.Action == moveAudit));
        var competingPair = await Task.WhenAll(Execute(admin, leftRoute, leftApproval), Execute(admin, rightRoute, rightApproval));
        using (competingPair[0]) using (competingPair[1])
            if (!competingPair.Select(x => (int)x.StatusCode).Order().SequenceEqual(new[] { 201, 409 }))
                throw new InvalidOperationException("Two approved moves cannot both change the same source.");
        if (await WithDb(db => db.AuditLogs.CountAsync(x => x.AcademyId == academy && x.Action == moveAudit)) != auditCount + 1)
            throw new InvalidOperationException("Competing moves emitted the wrong number of audits.");
        Pass("competing approvals move the source once");

        if (count != 26) throw new InvalidOperationException("Unexpected protected reschedule check count: " + count);
        Console.WriteLine($"SCHEDULEDEFAULTS MOVE SUMMARY {count} native HTTP/SQL checks PASS.");
    }
}
