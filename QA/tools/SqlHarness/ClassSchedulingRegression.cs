using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Domain.Identity;
using AcademyDesk.Api.Intelligence.Penta;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

internal static partial class SqlHarnessEntryPoint
{
    private static async Task VerifyClassSchedulingAsync(QaApiFactory factory, QaRunManifest manifest, Guid academy, Guid foreign)
    {
        const string adminEmail = "qa-admin-a@example.invalid";
        const string meeting = "https://meeting.example.invalid/schedule";
        const string pentaAudit = "PentaClassSessionScheduled";
        var flags = new Dictionary<string, string?> { ["Penta:Enabled"] = "true", ["Penta:ClassSessionSchedulingEnabled"] = "true", ["Penta:ClassSessionReschedulingEnabled"] = "true" };
        using var host = new QaApiFactory(manifest, flags);
        using var admin = await SignIn(host, adminEmail);
        using var manual = await SignIn(factory, adminEmail);
        using var teacherClient = await SignIn(host, "qa-teacher-a@example.invalid");
        using var anonymous = host.CreateClient();
        Guid actor;
        using (var scope = factory.Services.CreateScope())
            actor = (await scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>().FindByEmailAsync(adminEmail))!.Id;

        var conversation = new PentaMiniSession { AcademyId = academy, ActorUserId = actor,
            ProtectedState = "Synthetic class scheduling fixture", ExpiresAtUtc = DateTime.UtcNow.AddHours(1) };
        var branchA = new Branch { AcademyId = academy, Name = "Scheduling QA A" };
        var branchB = new Branch { AcademyId = academy, Name = "Scheduling QA B" };
        var teacherA = new Teacher { AcademyId = academy, FirstName = "Scheduling", LastName = "QA A" };
        var teacherB = new Teacher { AcademyId = academy, FirstName = "Scheduling", LastName = "QA B" };
        var teacherC = new Teacher { AcademyId = academy, FirstName = "Scheduling", LastName = "QA C" };
        var foreignTeacher = new Teacher { AcademyId = foreign, FirstName = "Scheduling", LastName = "QA foreign" };
        var course = Guid.NewGuid();
        var batch = new Batch { AcademyId = academy, Name = "Scheduling QA defaults", CourseId = course,
            TeacherId = teacherA.Id, BranchId = branchA.Id };
        var parallelBatch = new Batch { AcademyId = academy, Name = "Scheduling QA alternate", CourseId = course,
            TeacherId = teacherB.Id, BranchId = branchA.Id };
        var unassigned = new Batch { AcademyId = academy, Name = "Scheduling QA unassigned", CourseId = course };
        var foreignBatch = new Batch { AcademyId = foreign, Name = "Scheduling QA foreign", CourseId = course,
            TeacherId = foreignTeacher.Id };
        await WithDb(async db => {
            db.AddRange(conversation, branchA, branchB, teacherA, teacherB, teacherC, foreignTeacher, batch, parallelBatch, unassigned, foreignBatch);
            await db.SaveChangesAsync();
            return true;
        });

        var cases = 0;
        var start = DateTime.UtcNow.Date.AddDays(40).AddHours(4);
        var sessions = $"/api/academies/{academy}/sessions";
        string DraftRoute(Guid? tenant = null) => $"/api/academies/{tenant ?? academy}/penta/class-scheduling/conversations/{conversation.Id}/drafts/{Guid.NewGuid()}";
        static async Task<HttpClient> SignIn(QaApiFactory owner, string email)
        {
            var client = owner.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await LoginAsync(client, email, "Synthetic!39Ab"));
            return client;
        }
        async Task<T> WithDb<T>(Func<AcademyDeskDbContext, Task<T>> work)
        {
            using var scope = factory.Services.CreateScope();
            return await work(scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>());
        }
        Task<string> Snapshot(bool includeWorkflow = true) => WithDb(async db => JsonSerializer.Serialize(new {
            classes = await db.ClassSessions.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
            drafts = includeWorkflow ? await db.PentaOnboardingDrafts.AsNoTracking().OrderBy(x => x.Id).ToListAsync() : null,
            audits = includeWorkflow ? await db.AuditLogs.AsNoTracking().OrderBy(x => x.Id).ToListAsync() : null
        }));
        void Pass(string label) { cases++; Console.WriteLine($"SCHEDULEDEFAULTS SHARED CASE {label} PASS"); }
        async Task Refuse(Func<Task<HttpResponseMessage>> send, HttpStatusCode expected, string label, string? reason = null)
        {
            var before = await Snapshot();
            using var response = await send();
            RequireFinanceStatus(response, expected, label);
            if (reason is not null && !(await response.Content.ReadAsStringAsync()).Contains(reason, StringComparison.Ordinal))
                throw new InvalidOperationException(label + " returned the wrong conflict message.");
            if (before != await Snapshot()) throw new InvalidOperationException(label + " changed SQL state.");
            Pass(label);
        }
        Task<HttpResponseMessage> Manual(HttpClient client, Guid batchId, DateTime from, string? room = null,
            Guid? teacher = null, Guid? branch = null, string mode = "InPerson") =>
            client.PostAsJsonAsync(sessions, new CreateClassSessionRequest(batchId, teacher, branch, from, from.AddHours(1), mode, room));
        Task<HttpResponseMessage> Move(Guid id, DateTime from, string? room, string status = "Scheduled") =>
            manual.PutAsJsonAsync($"{sessions}/{id}", new UpdateClassSessionRequest(from, from.AddHours(1), "InPerson", room, status));
        async Task<ClassSessionSummary> Saved(Task<HttpResponseMessage> send, HttpStatusCode expected, string label)
        {
            using var response = await send;
            RequireFinanceStatus(response, expected, label);
            var result = (await response.Content.ReadFromJsonAsync<ClassSessionSummary>())!;
            var row = await WithDb(db => db.ClassSessions.AsNoTracking().SingleAsync(x => x.Id == result.Id && x.AcademyId == academy));
            if (result != new ClassSessionSummary(row.Id, row.BatchId, row.TeacherId, row.BranchId, row.StartUtc, row.EndUtc,
                row.DeliveryMode, row.RoomName, row.Status)) throw new InvalidOperationException(label + " receipt differs from fresh SQL.");
            Pass(label);
            return result;
        }
        Task<HttpResponseMessage> Prepare(HttpClient client, string route, DateTime from, string mode = "Offline",
            string? location = " Studio protected ", Guid? batchId = null, string? rawStart = null, long context = 0)
        {
            var fields = new Dictionary<string, object> {
                ["BatchId"] = new { kind = "reference", value = (batchId ?? batch.Id).ToString("D") },
                ["StartUtc"] = new { kind = "text", value = rawStart ?? from.ToString("O") },
                ["EndUtc"] = new { kind = "text", value = from.AddHours(1).ToString("O") },
                ["DeliveryMode"] = new { kind = "text", value = mode }
            };
            if (location is not null)
                fields[mode == "Offline" ? "RoomName" : "MeetingLink"] = new { kind = "text", value = location };
            return client.PutAsJsonAsync(route, new { contextVersion = context,
                payload = new { tool_key = "AcademyDesk.ClassSession.Create", tool_revision = 1, fields } }, new JsonSerializerOptions());
        }
        static Task<HttpResponseMessage> Approve(HttpClient client, string route, PentaClassSessionDraftView draft,
            string intent = "ApproveClassSessionSchedule", string? hash = null) =>
            client.PostAsJsonAsync(route + "/approve", new { revision = draft.Revision, payloadHash = hash ?? draft.PayloadHash, intent });
        static Task<HttpResponseMessage> Execute(HttpClient client, string route, PentaClassSessionDraftView draft, Guid? approval = null) =>
            client.PostAsJsonAsync(route + "/execute", new { revision = draft.Revision, payloadHash = draft.PayloadHash, approvalId = approval ?? draft.ApprovalId });
        static async Task<PentaClassSessionDraftView> Read(Task<HttpResponseMessage> send, HttpStatusCode expected, string label)
        {
            using var response = await send;
            RequireFinanceStatus(response, expected, label);
            if (response.Headers.CacheControl?.NoStore != true) throw new InvalidOperationException("Scheduling draft must be no-store.");
            return (await response.Content.ReadFromJsonAsync<PentaClassSessionDraftView>())!;
        }

        var first = await Saved(Manual(manual, batch.Id, start, " Main studio "), HttpStatusCode.Created, "effective defaults persisted");
        if (first.TeacherId != teacherA.Id || first.BranchId != branchA.Id || first.RoomName != "Main studio")
            throw new InvalidOperationException("Manual class defaults or normalized room differ.");
        await Refuse(() => Manual(manual, batch.Id, start.AddMinutes(15)), HttpStatusCode.Conflict, "inherited teacher clash", "Teacher clash");
        await Refuse(() => Manual(manual, parallelBatch.Id, start.AddMinutes(15), "Main studio"), HttpStatusCode.Conflict, "inherited branch clash", "Room clash");
        await Refuse(() => Manual(manual, parallelBatch.Id, start.AddMinutes(15), teacher: teacherA.Id), HttpStatusCode.Conflict, "explicit teacher clash", "Teacher clash");
        await Saved(Manual(manual, parallelBatch.Id, start, "Main studio", teacherC.Id, branchB.Id), HttpStatusCode.Created, "same room at different branch");
        await Saved(Manual(manual, batch.Id, start.AddHours(1), "Main studio"), HttpStatusCode.Created, "adjacent class");
        var movable = await Saved(Manual(manual, parallelBatch.Id, start.AddDays(1), "Main studio"), HttpStatusCode.Created, "reschedule fixture");
        await Refuse(() => Move(movable.Id, start, "Main studio"), HttpStatusCode.Conflict, "reschedule room clash", "Room clash");
        await Saved(Move(movable.Id, start.AddHours(2), "Main studio"), HttpStatusCode.OK, "reschedule to free slot");
        await Saved(Move(first.Id, start, "Main studio", "Cancelled"), HttpStatusCode.OK, "cancellation releases slot");
        await Saved(Manual(manual, batch.Id, start, "Main studio"), HttpStatusCode.Created, "reuse cancelled slot");
        await Refuse(() => Move(first.Id, start, "Main studio"), HttpStatusCode.Conflict, "reopen checks slot", "Teacher clash");
        await Refuse(() => Manual(manual, batch.Id, start.AddDays(2), teacher: foreignTeacher.Id), HttpStatusCode.BadRequest, "foreign explicit teacher");
        await Refuse(() => Manual(manual, batch.Id, start.AddDays(2), mode: " Online "), HttpStatusCode.BadRequest, "normalized online requires link");

        var raceTime = start.AddDays(3);
        var race = await Task.WhenAll(Manual(manual, batch.Id, raceTime), Manual(manual, batch.Id, raceTime));
        using (race[0]) using (race[1])
            if (!race.Select(x => (int)x.StatusCode).Order().SequenceEqual(new[] { 201, 409 }))
                throw new InvalidOperationException("Manual race should admit one class.");
        if (await WithDb(db => db.ClassSessions.CountAsync(x => x.AcademyId == academy && x.StartUtc == raceTime)) != 1)
            throw new InvalidOperationException("Manual race committed the wrong number of sessions.");
        Pass("Manual concurrent class creation");

        var slot = start.AddDays(5);
        await Saved(Manual(manual, unassigned.Id, slot), HttpStatusCode.Created, "unassigned class");
        await Refuse(() => Manual(manual, unassigned.Id, slot), HttpStatusCode.Conflict,
            "duplicate unassigned class", "already has a session");
        await Refuse(() => Prepare(manual, DraftRoute(), slot), HttpStatusCode.NotFound, "PENTA default disabled");
        await Refuse(() => Prepare(anonymous, DraftRoute(), slot), HttpStatusCode.Unauthorized, "PENTA anonymous");
        await Refuse(() => Prepare(teacherClient, DraftRoute(), slot), HttpStatusCode.Forbidden, "PENTA teacher cannot schedule");
        await Refuse(() => Prepare(admin, DraftRoute(foreign), slot), HttpStatusCode.Forbidden, "PENTA foreign route");
        await Refuse(() => Prepare(admin, DraftRoute(), slot, batchId: foreignBatch.Id), HttpStatusCode.BadRequest, "PENTA foreign batch");
        await Refuse(() => Prepare(admin, DraftRoute(), slot, "Online", "javascript:alert(1)"), HttpStatusCode.BadRequest, "PENTA unsafe meeting link");
        await Refuse(() => Prepare(admin, DraftRoute(), slot, "Hybrid", null), HttpStatusCode.BadRequest, "PENTA missing Hybrid link");
        await Refuse(() => Prepare(admin, DraftRoute(), slot, rawStart: slot.ToString("s")), HttpStatusCode.BadRequest, "PENTA explicit UTC required");
        await Refuse(() => Prepare(admin, DraftRoute(), slot, context: 1), HttpStatusCode.Conflict, "PENTA stale context");
        await Refuse(() => Prepare(admin, DraftRoute(), raceTime), HttpStatusCode.Conflict, "PENTA sees Manual clash");

        foreach (var mode in new[] { "Offline", "Online", "Hybrid" })
        {
            slot = slot.AddDays(1);
            var route = DraftRoute();
            var location = mode == "Offline" ? "Protected studio" : meeting;
            var before = await Snapshot(includeWorkflow: false);
            var draft = await Read(Prepare(admin, route, slot, mode, location), HttpStatusCode.Created, "prepare " + mode);
            if (draft.Status != "Prepared" || draft.FollowsBatchTeacher != true || draft.Plan.Id != Guid.Empty || draft.Plan.BatchId != batch.Id ||
                draft.Plan.TeacherId != teacherA.Id || draft.Plan.BranchId != branchA.Id || draft.Plan.StartUtc != slot ||
                draft.Plan.EndUtc != slot.AddHours(1) || draft.Plan.RoomName != location ||
                draft.Plan.DeliveryMode != (mode == "Offline" ? "InPerson" : mode) || before != await Snapshot(includeWorkflow: false))
                throw new InvalidOperationException(mode + " preview changed SQL or resolved different fields.");
            Pass(mode + " exact preview with no class");
            if (mode == "Offline")
            {
                await Refuse(() => Approve(admin, route, draft, "yes"), HttpStatusCode.BadRequest, "conversation yes cannot approve");
                await Refuse(() => Approve(admin, route, draft, hash: new string('0', 64)), HttpStatusCode.Conflict, "changed digest");
                await Refuse(() => Execute(admin, route, draft, Guid.NewGuid()), HttpStatusCode.Conflict, "forged approval");
            }
            var approved = await Read(Approve(admin, route, draft), HttpStatusCode.OK, "approve " + mode);
            if (approved.Status != "Authorized" || approved.ApprovalId is null || before != await Snapshot(includeWorkflow: false))
                throw new InvalidOperationException("Approval created a class.");
            Pass(mode + " explicit approval with no class");
            if (mode == "Hybrid")
            {
                var fault = new EnrollmentSharedAuditFault(pentaAudit);
                using var faultyHost = new QaApiFactory(manifest, flags, isolatedDomainInterceptor: fault);
                using var faulty = await SignIn(faultyHost, adminEmail);
                await Refuse(() => Execute(faulty, route, approved), HttpStatusCode.ServiceUnavailable, "PENTA audit rollback");
                if (fault.Hits != 1) throw new InvalidOperationException("PENTA audit failure was not reached.");
            }
            var pair = await Task.WhenAll(Execute(admin, route, approved), Execute(admin, route, approved));
            using var a = pair[0];
            using var b = pair[1];
            if (!pair.Select(x => (int)x.StatusCode).Order().SequenceEqual(new[] { 200, 201 }))
                throw new InvalidOperationException("PENTA should commit once and replay once.");
            var outcome = (await a.Content.ReadFromJsonAsync<PentaClassSessionDraftView>())!;
            if (outcome.Status != "Succeeded" || outcome.Receipt is not { } receipt ||
                receipt != (await b.Content.ReadFromJsonAsync<PentaClassSessionDraftView>())?.Receipt)
                throw new InvalidOperationException("PENTA returned different receipts.");
            await WithDb(async db => {
                var row = await db.ClassSessions.AsNoTracking().SingleAsync(x => x.AcademyId == academy && x.StartUtc == slot);
                if (row.Id != receipt.ClassSessionId || row.FollowsBatchTeacher != true || row.BatchId != draft.Plan.BatchId || row.TeacherId != draft.Plan.TeacherId ||
                    row.BranchId != draft.Plan.BranchId || row.RoomName != draft.Plan.RoomName || row.EndUtc != draft.Plan.EndUtc ||
                    row.DeliveryMode != draft.Plan.DeliveryMode || !await db.AuditLogs.AnyAsync(x => x.Id == receipt.AuditId &&
                        x.ActorUserId == actor && x.AcademyId == academy && x.Action == pentaAudit))
                    throw new InvalidOperationException("PENTA committed class or audit differs from the reviewed plan.");
                return true;
            });
            var saved = await Snapshot();
            if ((await Read(admin.GetAsync(route), HttpStatusCode.OK, "receipt recovery")).Receipt != receipt || saved != await Snapshot())
                throw new InvalidOperationException("PENTA receipt recovery rewrote SQL.");
            Pass(mode + " concurrent execution and durable readback");
        }

        slot = slot.AddDays(1);
        var staleRoute = DraftRoute();
        var staleDraft = await Read(Prepare(admin, staleRoute, slot), HttpStatusCode.Created, "prepare default drift");
        var staleApproval = await Read(Approve(admin, staleRoute, staleDraft), HttpStatusCode.OK, "approve default drift");
        await WithDb(db => db.Batches.Where(x => x.Id == batch.Id).ExecuteUpdateAsync(x => x.SetProperty(y => y.TeacherId, teacherB.Id)));
        await Refuse(() => Execute(admin, staleRoute, staleApproval), HttpStatusCode.Conflict, "default change invalidates approval");

        slot = slot.AddDays(1);
        var raceRoute = DraftRoute();
        var raceDraft = await Read(Prepare(admin, raceRoute, slot, location: null, batchId: unassigned.Id), HttpStatusCode.Created, "prepare mixed race");
        var raceApproval = await Read(Approve(admin, raceRoute, raceDraft), HttpStatusCode.OK, "approve mixed race");
        var mixed = await Task.WhenAll(Manual(manual, unassigned.Id, slot), Execute(admin, raceRoute, raceApproval));
        using (mixed[0]) using (mixed[1])
            if (!mixed.Select(x => (int)x.StatusCode).Order().SequenceEqual(new[] { 201, 409 }))
                throw new InvalidOperationException("Mixed race should admit one class.");
        if (await WithDb(db => db.ClassSessions.CountAsync(x => x.AcademyId == academy && x.StartUtc == slot)) != 1)
            throw new InvalidOperationException("Mixed race committed the wrong number of sessions.");
        Pass("Manual and PENTA contend on the same slot");

        var manualFault = new EnrollmentSharedAuditFault("POST ClassSessions");
        using var manualFaultHost = new QaApiFactory(manifest, isolatedDomainInterceptor: manualFault);
        using var faultyManual = await SignIn(manualFaultHost, adminEmail);
        await Refuse(() => Manual(faultyManual, batch.Id, slot.AddDays(1)), HttpStatusCode.InternalServerError, "Manual create audit rollback");
        if (manualFault.Hits != 1) throw new InvalidOperationException("Manual audit failure was not reached.");

        if (cases != 43) throw new InvalidOperationException("Unexpected shared scheduling check count: " + cases);
        Console.WriteLine($"SCHEDULEDEFAULTS SHARED SUMMARY {cases} native HTTP/SQL checks PASS.");
        await VerifyClassReschedulingAsync(factory, manifest, academy, foreign, actor, conversation.Id, batch.Id, foreignBatch.Id,
            teacherB.Id, branchA.Id, start.AddDays(30), admin, manual, teacherClient, anonymous, flags);
    }
}
