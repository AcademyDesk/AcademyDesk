using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Intelligence.Penta;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

internal static partial class SqlHarnessEntryPoint
{
    private static async Task VerifyPentaClassBrowserAsync(QaApiFactory factory, QaRunManifest manifest, Guid academy, Guid foreign)
    {
        const string email = "qa-class-browser-admin@example.invalid";
        const string query = "Synthetic browser scheduling";
        const string createIntent = "ApproveClassSessionSchedule";
        const string moveIntent = "ApproveClassSessionReschedule";
        const string searched = "PentaClassSchedulingSearched";
        const string started = "PentaClassSchedulingConversationStarted";
        const string scheduled = "PentaClassSessionScheduled";
        const string rescheduled = "PentaClassSessionRescheduled";

        var flags = new Dictionary<string, string?>
        {
            ["Penta:Enabled"] = "true",
            ["Penta:ClassSessionSchedulingEnabled"] = "true",
            ["Penta:ClassSessionReschedulingEnabled"] = "true",
            ["Penta:ClassSchedulingBrowserContractsEnabled"] = "true"
        };
        var actor = await CreateAccessActorAsync(factory, email, "AcademyAdmin", academy);
        using var host = new QaApiFactory(manifest, flags);
        using var admin = await PentaBrowserSignIn(host, email);
        using var other = await PentaBrowserSignIn(host, "qa-admin-a@example.invalid");
        using var teacher = await PentaBrowserSignIn(host, "qa-teacher-a@example.invalid");
        using var disabled = await PentaBrowserSignIn(factory, email);
        var root = $"/api/academies/{academy:D}/penta/class-scheduling/browser";
        var start = DateTime.UtcNow.Date.AddDays(40).AddHours(4);
        var batch = new Batch { AcademyId = academy, Name = query, CourseId = Guid.NewGuid() };
        var foreignBatch = new Batch { AcademyId = foreign, Name = query, CourseId = Guid.NewGuid() };
        var unrelated = new ClassSession { AcademyId = foreign, BatchId = foreignBatch.Id, StartUtc = start, EndUtc = start.AddHours(1), Status = "Scheduled" };
        await Db(async db => { db.AddRange(batch, foreignBatch, unrelated); return await db.SaveChangesAsync(); });

        var checks = 0;
        var key = Guid.NewGuid();
        var startRoute = root + $"/conversations/{key:D}";
        var recovery = root + $"/starts/{key:D}";
        using (var readiness = await PentaBrowserRead<JsonDocument>(admin.GetAsync(root + "/readiness"), HttpStatusCode.OK, "class readiness"))
            PentaRequire(readiness.RootElement.GetProperty("protocol").GetString() == "class-scheduling-browser.v1" &&
                readiness.RootElement.GetProperty("requiresApproval").GetBoolean(), "Wrong class scheduling readiness contract.");
        await PentaBrowserStatus(disabled.GetAsync(root + "/readiness"), HttpStatusCode.NotFound, "disabled scheduling browser");
        await PentaBrowserStatus(admin.GetAsync(recovery), HttpStatusCode.NotFound, "readiness does not create sessions");
        await PentaBrowserStatus(teacher.GetAsync(root + "/batches?q=Synthetic"), HttpStatusCode.Forbidden, "teacher scheduling lookup");
        await PentaBrowserStatus(admin.GetAsync($"/api/academies/{foreign:D}/penta/class-scheduling/browser/readiness"),
            HttpStatusCode.Forbidden, "foreign scheduling readiness");
        PentaRequire(await Audits(started) == 0 && await Audits(searched) == 0, "Readiness or denied lookup wrote an audit.");
        Pass("readiness, flag, role and academy boundaries");

        var lookup = await PentaBrowserRead<PentaClassBrowserLookup<PentaClassBrowserBatch>>(
            admin.GetAsync(root + "/batches?q=" + Uri.EscapeDataString(query)), HttpStatusCode.OK, "browser batch lookup");
        PentaRequire(lookup.AcademyId == academy && lookup.Rows.Single().SourceId == batch.Id &&
            lookup.AsOfUtc.Kind == DateTimeKind.Utc && lookup.MaxRows == 10 && !lookup.HasMore && await Audits(searched) == 1,
            "Batch lookup leaked foreign data, lost its bounds or missed its audit.");
        Pass("private bounded batch lookup with required audit");

        var starts = (await Task.WhenAll(Start(), Start())).OrderBy(x => (int)x.StatusCode).ToArray();
        var session = await PentaBrowserRead<MiniSessionView>(Task.FromResult(starts[1]), HttpStatusCode.Created, "winning class start");
        PentaRequire(session == await PentaBrowserRead<MiniSessionView>(Task.FromResult(starts[0]), HttpStatusCode.OK, "replayed class start") &&
            session == await PentaBrowserRead<MiniSessionView>(admin.GetAsync(recovery), HttpStatusCode.OK, "recover class start") &&
            session.Version == 0 && await Audits(started) == 1, "Class start replay changed the bound session.");
        await PentaBrowserStatus(other.GetAsync(recovery), HttpStatusCode.NotFound, "another actor cannot recover class start");
        Pass("concurrent start and recovery return one private session");

        var createRoute = Draft();
        var create = await PentaBrowserRead<PentaClassSessionDraftView>(
            PentaBrowserPrepare(admin, createRoute, "AcademyDesk.ClassSession.Create", Fields("BatchId", batch.Id, start)),
            HttpStatusCode.Created, "prepare class");
        PentaRequire(create.Plan.StartUtc == start && create.Plan.StartUtc.Kind == DateTimeKind.Utc &&
            create.Plan.BatchId == batch.Id && create.Plan.RoomName == "Synthetic studio" && create.Receipt is null && create.Source is null,
            "Prepared class does not match the exact slot.");
        await PentaBrowserStatus(Execute(createRoute, create, Guid.NewGuid()), HttpStatusCode.Conflict, "unapproved class execute");
        await PentaBrowserStatus(Approve(createRoute, create, moveIntent), HttpStatusCode.Conflict, "wrong class approval intent");
        var confirmed = await PentaBrowserRead<PentaClassSessionDraftView>(Approve(createRoute, create, createIntent), HttpStatusCode.OK, "approve class");
        PentaRequire(confirmed.Plan == create.Plan && confirmed.ApprovalId.HasValue && confirmed.Receipt is null && await Sessions() == 0,
            "Class approval changed the schedule or its terms.");
        Pass("exact draft and separate approval without a schedule write");

        var saved = await PentaBrowserRead<PentaClassSessionDraftView>(Execute(createRoute, confirmed), HttpStatusCode.Created, "schedule class");
        var created = saved.Receipt ?? throw new InvalidOperationException("Missing class creation receipt.");
        PentaRequire(saved == await PentaBrowserRead<PentaClassSessionDraftView>(admin.GetAsync(createRoute), HttpStatusCode.OK, "recover class receipt") &&
            await Sessions() == 1 && (await Stored(created.ClassSessionId)).StartUtc == start && await Audits(scheduled, created.AuditId) == 1,
            "Scheduled class and recovered receipt differ.");
        Pass("scheduled one class and recovered its audited receipt");

        var source = await PentaBrowserRead<PentaClassBrowserLookup<PentaClassBrowserSource>>(
            admin.GetAsync(root + "/sessions?q=" + Uri.EscapeDataString(query)), HttpStatusCode.OK, "class source lookup");
        PentaRequire(source.Rows.Single() is { } row && row.SourceId == created.ClassSessionId &&
            row.StartUtc == start && row.StartUtc.Kind == DateTimeKind.Utc && row.EndUtc.Kind == DateTimeKind.Utc && await Audits(searched) == 2,
            "Session lookup leaked a foreign session or returned the wrong source.");
        Pass("private source lookup uses the real committed class");

        var target = start.AddDays(1);
        var moveRoute = Draft();
        var move = await PentaBrowserRead<PentaClassSessionDraftView>(
            PentaBrowserPrepare(admin, moveRoute, "AcademyDesk.ClassSession.Reschedule", Fields("SessionId", created.ClassSessionId, target)),
            HttpStatusCode.Created, "prepare class move");
        PentaRequire(move.Source?.StartUtc == start && move.Source?.Id == created.ClassSessionId && move.Plan.StartUtc == target,
            "Move comparison lost the source or destination.");
        var approvedMove = await PentaBrowserRead<PentaClassSessionDraftView>(Approve(moveRoute, move, moveIntent), HttpStatusCode.OK, "approve class move");
        var fault = new EnrollmentSharedAuditFault(rescheduled);
        using (var faultHost = new QaApiFactory(manifest, flags, isolatedDomainInterceptor: fault))
        using (var faulty = await PentaBrowserSignIn(faultHost, email))
            await PentaBrowserStatus(PentaBrowserExecute(faulty, moveRoute, approvedMove.Revision, approvedMove.PayloadHash, approvedMove.ApprovalId),
                HttpStatusCode.ServiceUnavailable, "move audit failure");
        PentaRequire(fault.Hits == 1 && (await Stored(created.ClassSessionId)).StartUtc == start && await Audits(rescheduled) == 0 &&
            await PentaBrowserRead<PentaClassSessionDraftView>(admin.GetAsync(moveRoute), HttpStatusCode.OK, "recover approval after rollback") == approvedMove,
            "Required audit failure did not roll back the move and preserve its approval.");
        Pass("reschedule audit fault rolls back SQL and keeps approval");

        var moved = (await Task.WhenAll(Execute(moveRoute, approvedMove), Execute(moveRoute, approvedMove))).OrderBy(x => (int)x.StatusCode).ToArray();
        var completed = await PentaBrowserRead<PentaClassSessionDraftView>(Task.FromResult(moved[1]), HttpStatusCode.Created, "winning class move");
        var receipt = completed.Receipt ?? throw new InvalidOperationException("Missing reschedule receipt.");
        PentaRequire(completed == await PentaBrowserRead<PentaClassSessionDraftView>(Task.FromResult(moved[0]), HttpStatusCode.OK, "replayed class move") &&
            receipt.ClassSessionId == created.ClassSessionId && (await Stored(receipt.ClassSessionId)).StartUtc == target &&
            await Sessions() == 1 && await Audits(rescheduled, receipt.AuditId) == 1 && await Audits(rescheduled) == 1,
            "Concurrent move failed to retain one row and one audited result.");
        Pass("concurrent reschedule writes one result and replays one receipt");

        Console.WriteLine($"PASS: PENTA CLASS SCHEDULING BROWSER {checks} native HTTP/SQL checks");

        Task<HttpResponseMessage> Start() => admin.PutAsJsonAsync(startRoute, new Dictionary<string, string>());
        string Draft() => root + $"/conversations/{session.ConversationId:D}/drafts/{Guid.NewGuid():D}";
        Task<HttpResponseMessage> Approve(string route, PentaClassSessionDraftView draft, string intent) =>
            PentaBrowserApprove(admin, route, draft.Revision, draft.PayloadHash, intent);
        Task<HttpResponseMessage> Execute(string route, PentaClassSessionDraftView draft, Guid? approval = null) =>
            PentaBrowserExecute(admin, route, draft.Revision, draft.PayloadHash, approval ?? draft.ApprovalId);
        static Dictionary<string, object> Fields(string reference, Guid id, DateTime slot) => new()
        {
            [reference] = new { kind = "reference", value = id.ToString("D") },
            ["StartUtc"] = new { kind = "text", value = slot.ToString("O") },
            ["EndUtc"] = new { kind = "text", value = slot.AddHours(1).ToString("O") },
            ["DeliveryMode"] = new { kind = "text", value = "Offline" },
            ["RoomName"] = new { kind = "text", value = "Synthetic studio" }
        };
        Task<int> Audits(string action, Guid? id = null) => Db(db => db.AuditLogs.CountAsync(x =>
            x.AcademyId == academy && x.ActorUserId == actor && x.Action == action && (!id.HasValue || x.Id == id)));
        Task<int> Sessions() => Db(db => db.ClassSessions.CountAsync(x => x.AcademyId == academy && x.BatchId == batch.Id));
        Task<ClassSession> Stored(Guid id) => Db(db => db.ClassSessions.AsNoTracking().SingleAsync(x => x.AcademyId == academy && x.Id == id));
        async Task<T> Db<T>(Func<AcademyDeskDbContext, Task<T>> action)
        {
            using var scope = factory.Services.CreateScope();
            return await action(scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>());
        }
        void Pass(string label)
        {
            checks++;
            Console.WriteLine("PASS: PENTA CLASS SCHEDULING BROWSER " + label);
        }
    }
}
