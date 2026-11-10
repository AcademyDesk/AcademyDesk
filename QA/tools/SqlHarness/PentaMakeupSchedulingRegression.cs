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
    private static async Task VerifyPentaMakeupAsync(QaApiFactory factory, QaRunManifest manifest, Guid academy, Guid foreign,
        Guid student, Guid foreignStudent, Guid guardian, Guid actor, DateTime start, Func<string, string?, Task<Guid>> seedBatch)
    {
        const string adminEmail = "qa-admin-a@example.invalid";
        const string link = "https://meeting.example.invalid/protected-makeup";
        const string auditAction = "PentaMakeupScheduled";
        var flags = new Dictionary<string, string?> { ["Penta:Enabled"] = "true", ["Penta:MakeupSchedulingEnabled"] = "true" };
        using var host = new QaApiFactory(manifest, flags);
        using var admin = await SignIn(host, adminEmail);
        using var disabled = await SignIn(factory, adminEmail);
        using var teacher = await SignIn(host, "qa-teacher-a@example.invalid");
        using var delegateUser = await SignIn(host, "qa-makeup-granted@example.invalid");
        using var anonymous = host.CreateClient();
        var session = new PentaMiniSession { AcademyId = academy, ActorUserId = actor,
            ProtectedState = "Synthetic make-up conversation fixture", ExpiresAtUtc = DateTime.UtcNow.AddHours(1) };
        await ChangeDb(db => { db.PentaMiniSessions.Add(session); return db.SaveChangesAsync(); });
        var cases = 0;

        static async Task<HttpClient> SignIn(QaApiFactory owner, string email)
        {
            var client = owner.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await LoginAsync(client, email, "Synthetic!39Ab"));
            return client;
        }

        async Task ChangeDb(Func<AcademyDeskDbContext, Task> change)
        {
            using var scope = factory.Services.CreateScope();
            await change(scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>());
        }

        string NewRoute(Guid? tenant = null) => $"/api/academies/{tenant ?? academy}/penta/makeup/conversations/{session.Id}/drafts/{Guid.NewGuid()}";
        var exactNames = new JsonSerializerOptions();
        Task<HttpResponseMessage> Prepare(HttpClient client, string route, Guid batch, string mode = "Offline", string? meeting = link,
            bool useNext = false, string? time = null, Guid? learner = null)
        {
            var fields = new Dictionary<string, object> {
                ["StudentId"] = new { kind = "reference", value = (learner ?? student).ToString("D") },
                ["BatchId"] = new { kind = "reference", value = batch.ToString("D") },
                ["DeliveryMode"] = new { kind = "text", value = mode },
                ["Venue"] = new { kind = "text", value = " Approved studio " },
                ["UseNextScheduledClass"] = new { kind = "boolean", value = useNext } };
            if (!useNext) fields["StartUtc"] = new { kind = "text", value = time ?? start.ToString("O") };
            if (meeting is not null) fields["MeetingLink"] = new { kind = "text", value = meeting };
            return client.PutAsJsonAsync(route, new { contextVersion = 0, payload = new { tool_key = "AcademyDesk.Makeup.Create", tool_revision = 1, fields } }, exactNames);
        }
        static Task<HttpResponseMessage> Approve(HttpClient client, string route, PentaMakeupDraftView draft, string intent = "ApproveMakeupScheduleAndQueueInApp", string? hash = null) =>
            client.PostAsJsonAsync(route + "/approve", new { revision = draft.Revision, payloadHash = hash ?? draft.PayloadHash, intent });
        static Task<HttpResponseMessage> Execute(HttpClient client, string route, PentaMakeupDraftView draft, Guid? approval = null) =>
            client.PostAsJsonAsync(route + "/execute", new { revision = draft.Revision, payloadHash = draft.PayloadHash, approvalId = approval ?? draft.ApprovalId });

        async Task<string> Snapshot(bool includeWorkflow = true)
        {
            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            return JsonSerializer.Serialize(new { makeups = await db.MakeupClasses.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
                notices = await db.Notifications.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
                drafts = includeWorkflow ? await db.PentaOnboardingDrafts.AsNoTracking().OrderBy(x => x.Id).ToListAsync() : null,
                audits = includeWorkflow ? await db.AuditLogs.AsNoTracking().OrderBy(x => x.Id).ToListAsync() : null });
        }

        void Pass(string name) { cases++; Console.WriteLine($"MAKEUPLOCATION PENTA CASE {name} PASS"); }
        async Task Refuse(Func<Task<HttpResponseMessage>> send, HttpStatusCode status, string label)
        {
            var before = await Snapshot();
            using var response = await send();
            RequireFinanceStatus(response, status, label);
            if (before != await Snapshot()) throw new InvalidOperationException(label + " changed SQL state.");
            Pass(label);
        }
        static async Task<PentaMakeupDraftView> Read(Task<HttpResponseMessage> send, HttpStatusCode status, string label)
        {
            using var response = await send;
            RequireFinanceStatus(response, status, label);
            if (response.Headers.CacheControl?.NoStore != true) throw new InvalidOperationException("Make-up draft must be no-store.");
            return (await response.Content.ReadFromJsonAsync<PentaMakeupDraftView>())!;
        }

        var batch = await seedBatch("InPerson", "Approved inherited room");
        await Refuse(() => Prepare(disabled, NewRoute(), batch), HttpStatusCode.NotFound, "default-disabled");
        await Refuse(() => Prepare(anonymous, NewRoute(), batch), HttpStatusCode.Unauthorized, "anonymous");
        await Refuse(() => Prepare(teacher, NewRoute(), batch), HttpStatusCode.Forbidden, "teacher");
        await Refuse(() => Prepare(delegateUser, NewRoute(), batch), HttpStatusCode.Forbidden, "manual grant cannot use PENTA");
        await Refuse(() => Prepare(admin, NewRoute(foreign), batch), HttpStatusCode.Forbidden, "foreign route");
        await Refuse(() => Prepare(admin, NewRoute(), batch, learner: foreignStudent), HttpStatusCode.BadRequest, "foreign learner");
        await Refuse(() => Prepare(admin, NewRoute(), batch, "Online", null), HttpStatusCode.BadRequest, "missing Online link");
        await Refuse(() => Prepare(admin, NewRoute(), batch, "Hybrid", "javascript:alert(1)"), HttpStatusCode.BadRequest, "invalid Hybrid link");
        await Refuse(() => Prepare(admin, NewRoute(), batch, time: "2026-12-30T04:00:00"), HttpStatusCode.BadRequest, "timestamp without UTC");
        await Refuse(() => Prepare(admin, NewRoute(), batch, time: "9999-12-31T23:59:00Z"), HttpStatusCode.BadRequest, "timestamp overflow");
        await Refuse(() => Prepare(admin, NewRoute(), batch, time: DateTime.UtcNow.AddDays(-1).ToString("O")), HttpStatusCode.BadRequest, "past schedule");

        foreach (var mode in new[] { "Offline", "Online", "Hybrid" })
        {
            var target = await seedBatch(mode, mode == "Offline" ? "Approved inherited room" : link);
            var route = NewRoute();
            var useNext = mode == "Hybrid";
            var before = await Snapshot(includeWorkflow: false);
            var draft = await Read(Prepare(admin, route, target, mode, useNext: useNext), HttpStatusCode.Created, "prepare " + mode);
            var expectedLocation = mode == "Offline" ? "Approved studio" : link;
            if (draft.Status != "Prepared" || draft.Plan.DeliveryMode != mode || draft.Plan.StudentId != student || draft.Plan.BatchId != target ||
                draft.Plan.StartUtc != start || draft.Plan.EndUtc != start.AddMinutes(useNext ? 45 : 75) || draft.Plan.UsesNextScheduledClass != useNext ||
                draft.Plan.Venue != (mode == "Offline" ? expectedLocation : null) || draft.Plan.MeetingLink != (mode == "Offline" ? null : expectedLocation) ||
                draft.NotificationChannel != "InApp" || !draft.Plan.GuardianIds.SequenceEqual(new[] { guardian }) ||
                !draft.Plan.NotificationMessage.EndsWith(expectedLocation, StringComparison.Ordinal) || before != await Snapshot(includeWorkflow: false))
                throw new InvalidOperationException("Make-up preview or effect differs from reviewed details: " + mode);
            Pass(mode + " exact preview with no class or notices");
            if (mode == "Offline")
            {
                var unchanged = await Snapshot();
                var same = await Read(Prepare(admin, route, target, mode), HttpStatusCode.OK, "prepare replay");
                if (same.PayloadHash != draft.PayloadHash || unchanged != await Snapshot()) throw new InvalidOperationException("Preparation replay changed state.");
                Pass("prepare replay stable");
                await Refuse(() => Approve(admin, route, draft, "yes"), HttpStatusCode.BadRequest, "conversational yes");
                await Refuse(() => Approve(admin, route, draft, hash: new string('0', 64)), HttpStatusCode.Conflict, "wrong digest");
                await Refuse(() => Execute(admin, route, draft, Guid.NewGuid()), HttpStatusCode.Conflict, "unapproved execution");
            }
            var approved = await Read(Approve(admin, route, draft), HttpStatusCode.OK, "approve " + mode);
            if (approved.Status != "Authorized" || approved.ApprovalId is null || before != await Snapshot(includeWorkflow: false))
                throw new InvalidOperationException("Approval scheduled or notified before execute.");
            Pass(mode + " explicit approval without scheduling");
            if (mode == "Hybrid")
            {
                var fault = new EnrollmentSharedAuditFault(auditAction);
                using var faultyHost = new QaApiFactory(manifest, flags, isolatedDomainInterceptor: fault);
                using var faulty = await SignIn(faultyHost, adminEmail);
                await Refuse(() => Execute(faulty, route, approved), HttpStatusCode.ServiceUnavailable, "audit rollback preserves class and notices");
                if (fault.Hits != 1) throw new InvalidOperationException("Make-up audit fault not reached.");
            }

            var pair = await Task.WhenAll(Execute(admin, route, approved), Execute(admin, route, approved));
            using var first = pair[0]; using var second = pair[1];
            if (!pair.Select(x => (int)x.StatusCode).Order().SequenceEqual(new[] { 200, 201 })) throw new InvalidOperationException("Expected one commit and one replay.");
            var committed = (await first.Content.ReadFromJsonAsync<PentaMakeupDraftView>())!;
            if (committed.Status != "Succeeded" || committed.Receipt is not { NotificationsQueued: 2 } receipt ||
                receipt != (await second.Content.ReadFromJsonAsync<PentaMakeupDraftView>())?.Receipt) throw new InvalidOperationException("Make-up receipts differ.");
            using (var original = JsonDocument.Parse(before))
            {
                var prior = original.RootElement.GetProperty("notices").EnumerateArray().Select(x => x.GetProperty("Id").GetGuid()).ToHashSet();
                await ChangeDb(async db => {
                    var row = await db.MakeupClasses.AsNoTracking().SingleAsync(x => x.AcademyId == academy && x.BatchId == target);
                    var notices = (await db.Notifications.AsNoTracking().ToListAsync()).Where(x => !prior.Contains(x.Id)).ToArray();
                    if (row.Id != receipt.MakeupId || row.Venue != draft.Plan.Venue || row.MeetingLink != draft.Plan.MeetingLink ||
                        notices.Length != 2 || !notices.Any(x => x.RecipientId == student && x.RecipientType == "Student") ||
                        !notices.Any(x => x.RecipientId == guardian && x.RecipientType == "Parent") ||
                        notices.Any(x => x.AcademyId != academy || x.Channel != "InApp" || x.Status != "Queued" || x.SentAtUtc != null || x.Message != draft.Plan.NotificationMessage) ||
                        !await db.AuditLogs.AnyAsync(x => x.Id == receipt.AuditId && x.ActorUserId == actor && x.AcademyId == academy && x.Action == auditAction))
                        throw new InvalidOperationException("Persisted class, queue or attributed audit differs.");
                });
            }
            var savedState = await Snapshot();
            if ((await Read(admin.GetAsync(route), HttpStatusCode.OK, "recover " + mode)).Receipt != receipt || savedState != await Snapshot())
                throw new InvalidOperationException("Receipt recovery changed SQL state.");
            Pass(mode + " concurrent execute, real queue and read recovery");
            await Refuse(() => Prepare(admin, NewRoute(), target, mode, useNext: useNext), HttpStatusCode.Conflict, mode + " duplicate slot");
        }

        var staleBatch = await seedBatch("Online", link);
        var staleRoute = NewRoute();
        var stale = await Read(Prepare(admin, staleRoute, staleBatch, useNext: true), HttpStatusCode.Created, "prepare stale source");
        var staleApproval = await Read(Approve(admin, staleRoute, stale), HttpStatusCode.OK, "approve stale source");
        await ChangeDb(db => db.ClassSessions.Where(x => x.Id == stale.Plan.SourceSessionId).ExecuteUpdateAsync(x => x.SetProperty(s => s.RoomName, "https://meeting.example.invalid/replaced")));
        await Refuse(() => Execute(admin, staleRoute, staleApproval), HttpStatusCode.Conflict, "changed meeting link invalidates approval");

        var accessBatch = await seedBatch("InPerson", "Other studio");
        var accessRoute = NewRoute();
        var access = await Read(Prepare(admin, accessRoute, accessBatch), HttpStatusCode.Created, "prepare recipient drift");
        var accessApproval = await Read(Approve(admin, accessRoute, access), HttpStatusCode.OK, "approve recipient drift");
        await ChangeDb(db => db.StudentGuardians.Where(x => x.AcademyId == academy && x.StudentId == student && x.GuardianId == guardian)
            .ExecuteUpdateAsync(x => x.SetProperty(p => p.CanAccessPortal, false)));
        await Refuse(() => Execute(admin, accessRoute, accessApproval), HttpStatusCode.Conflict, "revoked recipient invalidates approval");

        var manualBatch = await seedBatch("Offline", "Manual rollback room");
        var manualFault = new EnrollmentSharedAuditFault("POST MakeupClasses");
        using (var faultyManualHost = new QaApiFactory(manifest, isolatedDomainInterceptor: manualFault))
        using (var faultyManual = await SignIn(faultyManualHost, adminEmail))
            await Refuse(() => faultyManual.PostAsJsonAsync($"/api/academies/{academy}/makeup-classes",
                new CreateMakeupRequest(student, manualBatch, null, start, "Offline", "Manual rollback room", null, false, null)),
                HttpStatusCode.InternalServerError, "Manual required audit rollback");
        if (manualFault.Hits != 1) throw new InvalidOperationException("Manual make-up audit fault not reached.");
        Console.WriteLine($"MAKEUPLOCATION PENTA SUMMARY {cases} native HTTP/SQL checks PASS.");
    }
}
