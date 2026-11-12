using System.Net;
using System.Net.Http.Headers;
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
    private static async Task VerifyEnrollmentBrowserAsync(QaApiFactory factory, QaRunManifest manifest, Guid academy,
        Guid foreign, Func<Task<EnrollmentFixture>> seed)
    {
        const string email = "qa-enrollment-browser-admin@example.invalid";
        const string started = "PentaEnrollmentConversationStarted";
        const string executed = "PentaEnrollmentExecuted";
        var flags = new Dictionary<string, string?>
        {
            ["Penta:Enabled"] = "true", ["Penta:EnrollmentCreationEnabled"] = "true",
            ["Penta:EnrollmentBrowserContractsEnabled"] = "true"
        };
        var clock = new OnboardingDraftClock();
        var actor = await CreateAccessActorAsync(factory, email, "AcademyAdmin", academy);
        using var host = new QaApiFactory(manifest, flags, isolatedClock: clock);
        using var admin = await Client(host, email);
        using var other = await Client(host, "qa-admin-a@example.invalid");
        using var teacher = await Client(host, "qa-teacher-a@example.invalid");
        using var disabled = await Client(factory, email);
        using var anonymous = host.CreateClient();
        var root = $"/api/academies/{academy:D}/penta/enrollments";
        var readiness = root + "/browser-readiness";
        var checks = 0;

        var before = await State();
        using (var available = await Read<JsonDocument>(admin.GetAsync(readiness), 200, "readiness"))
        {
            PentaRequire(available.RootElement.GetProperty("protocol").GetString() == "enrollment-create-browser.v1" &&
                available.RootElement.GetProperty("requiresApproval").GetBoolean() && await State() == before,
                "Readiness advertised the wrong contract or created state.");
        }
        Pass("readiness is no-store, requires approval and creates no state");

        var startKey = Guid.NewGuid();
        await Refuse(() => admin.GetAsync(Recovery(startKey)), 404, "missing start");
        await Refuse(() => Start(startKey, disabled), 404, "disabled host");
        await Refuse(() => Start(startKey, anonymous), 401, "anonymous start");
        await Refuse(() => Start(startKey, teacher), 403, "teacher start");
        await Refuse(() => admin.GetAsync($"/api/academies/{foreign:D}/penta/enrollments/browser-readiness"), 403, "foreign academy");
        Pass("missing recovery, disabled, anonymous, teacher and foreign scope refuse without writes");

        var fault = new EnrollmentSharedAuditFault(started);
        using (var failingHost = new QaApiFactory(manifest, flags, isolatedDomainInterceptor: fault, isolatedClock: clock))
        using (var failing = await Client(failingHost, email))
            await Refuse(() => Start(startKey, failing), 503, "start audit fault");
        PentaRequire(fault.Hits == 1, "Start did not reach its required audit.");
        Pass("required start audit failure rolls back its session");

        var starts = (await Task.WhenAll(Start(startKey), Start(startKey))).OrderBy(x => (int)x.StatusCode).ToArray();
        var session = await Read<MiniSessionView>(Task.FromResult(starts[1]), 201, "winning start");
        var startReplay = await Read<MiniSessionView>(Task.FromResult(starts[0]), 200, "concurrent start replay");
        PentaRequire(session == startReplay && session.Version == 0 && session.ExpiresAtUtc.Kind == DateTimeKind.Utc &&
            await WithDb(db => db.PentaMiniSessions.CountAsync(x => x.Id == session.ConversationId && x.AcademyId == academy && x.ActorUserId == actor)) == 1 &&
            await WithDb(db => db.AuditLogs.CountAsync(x => x.Action == started && x.EntityId == session.ConversationId && x.AcademyId == academy && x.ActorUserId == actor)) == 1,
            "Concurrent start did not return one purpose-bound session and start audit.");
        Pass("same-key concurrent starts create one session and one audit");

        before = await State();
        PentaRequire(session == await Read<MiniSessionView>(admin.GetAsync(Recovery(startKey)), 200, "recover start") &&
            session == await Read<MiniSessionView>(Start(startKey), 200, "replay start") && before == await State(),
            "Recovery or replay changed the session.");
        await Refuse(() => other.GetAsync(Recovery(startKey)), 404, "another actor cannot recover a start");
        Pass("GET and PUT recover the same session without new state");

        var active = await seed();
        var path = Draft(session.ConversationId);
        var legacy = new PentaMiniSession { AcademyId = academy, ActorUserId = actor,
            ExpiresAtUtc = clock.Value.AddHours(1).UtcDateTime, ProtectedState = "Synthetic legacy session" };
        await WithDb(async db => { db.PentaMiniSessions.Add(legacy); return await db.SaveChangesAsync(); });
        var legacyDraft = Draft(legacy.Id);
        await Refuse(() => Prepare(legacyDraft, active, "Active"), 404, "legacy session cannot prepare a browser draft");
        await Refuse(() => other.GetAsync(path), 404, "another actor cannot read a draft");
        Pass("browser draft rejects legacy and other-actor sessions");

        await WithDb(db => db.PentaMiniSessions.Where(x => x.Id == session.ConversationId)
            .ExecuteUpdateAsync(set => set.SetProperty(x => x.Version, 1)));
        await Refuse(() => admin.GetAsync(Recovery(startKey)), 404, "tampered conversation version");
        await WithDb(db => db.PentaMiniSessions.Where(x => x.Id == session.ConversationId)
            .ExecuteUpdateAsync(set => set.SetProperty(x => x.Version, 0)));
        Pass("tampering with the bound conversation version is refused");

        var reviewed = await Read<PentaEnrollmentDraftView>(Prepare(path, active, "Active"), 201, "review active admission");
        PentaRequire(reviewed.Status == "Prepared" && reviewed.Receipt is null && reviewed.ApprovalId is null &&
            reviewed.Admission is { } review && review.StudentId == active.Student && review.BatchId == active.Target && review.Status == "Active",
            "Browser review did not return the exact admission.");
        await Refuse(() => Execute(path, reviewed with { ApprovalId = Guid.NewGuid() }), 409, "execution before approval");
        var approved = await Read<PentaEnrollmentDraftView>(Approve(path, reviewed), 200, "authorize active admission");
        PentaRequire(approved.Status == "Authorized" && approved.ApprovalId.HasValue && approved.Receipt is null &&
            approved.Admission == reviewed.Admission && approved.PayloadHash == reviewed.PayloadHash && await Enrollments(active) == 0,
            "Review or approval created an enrollment or changed its terms.");
        Pass("exact review and approval leave the domain untouched");

        var writeFault = new EnrollmentSharedAuditFault(executed);
        using (var failingHost = new QaApiFactory(manifest, flags, isolatedDomainInterceptor: writeFault, isolatedClock: clock))
        using (var failing = await Client(failingHost, email))
            await Refuse(() => Execute(path, approved, failing), 503, "execution audit fault");
        PentaRequire(writeFault.Hits == 1 && await Enrollments(active) == 0, "Enrollment or audit survived the fault.");
        Pass("required execution audit failure rolls back enrollment and draft transition");

        var responses = (await Task.WhenAll(Execute(path, approved), Execute(path, approved))).OrderBy(x => (int)x.StatusCode).ToArray();
        var saved = await Read<PentaEnrollmentDraftView>(Task.FromResult(responses[1]), 201, "winning active execution");
        PentaRequire(saved == await Read<PentaEnrollmentDraftView>(Task.FromResult(responses[0]), 200, "concurrent execution replay"),
            "Concurrent execution returned different receipts.");
        await VerifySaved(saved, active, "Active");
        before = await State();
        PentaRequire(saved == await Read<PentaEnrollmentDraftView>(admin.GetAsync(path), 200, "recover active receipt") &&
            before == await State(), "Reading the receipt changed SQL state.");
        Pass("concurrent execute and GET recovery return one active enrollment and audit");

        var waiting = await seed();
        var waitingPath = Draft(session.ConversationId);
        var waitDraft = await Read<PentaEnrollmentDraftView>(Prepare(waitingPath, waiting, "Waitlisted"), 201, "review waitlist");
        var waitApproval = await Read<PentaEnrollmentDraftView>(Approve(waitingPath, waitDraft), 200, "authorize waitlist");
        await VerifySaved(await Read<PentaEnrollmentDraftView>(Execute(waitingPath, waitApproval), 201, "execute waitlist"), waiting, "Waitlisted");
        Pass("explicit waitlisted enrollment follows the same approval and receipt path");

        var changed = await seed();
        var changedPath = Draft(session.ConversationId);
        var changedDraft = await Read<PentaEnrollmentDraftView>(Prepare(changedPath, changed, "Active"), 201, "review changing capacity");
        var changedApproval = await Read<PentaEnrollmentDraftView>(Approve(changedPath, changedDraft), 200, "authorize changing capacity");
        await WithDb(db => db.Batches.Where(x => x.Id == changed.Target).ExecuteUpdateAsync(set => set.SetProperty(x => x.Capacity, 0)));
        await Refuse(() => Execute(changedPath, changedApproval), 409, "capacity changed after approval");
        Pass("changed capacity cannot execute the previously authorized request");

        clock.Value = clock.Value.AddHours(25);
        // Advance the conversation clock while using current bearer credentials for the expiry check.
        using var advancedAdmin = await Client(host, email);
        using var advancedOther = await Client(host, "qa-admin-a@example.invalid");
        await Refuse(() => advancedAdmin.GetAsync(Recovery(startKey)), 410, "expired start recovery");
        await Refuse(() => Start(startKey, advancedAdmin), 410, "expired start replay");
        Pass("expired starts cannot be recovered or recreated");

        var quotaKey = Guid.NewGuid();
        await FillQuota(actor, 20);
        await Refuse(() => Start(quotaKey, advancedAdmin), 429, "actor daily quota");
        await FillQuota(actor, 80);
        await Refuse(() => Start(Guid.NewGuid(), advancedOther), 429, "academy daily quota independent of actor");
        Pass("actor and academy start quotas are enforced independently");

        using (var scope = factory.Services.CreateScope())
        {
            var identity = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            await identity.UserRoles.Where(x => x.UserId == actor).ExecuteDeleteAsync();
        }
        await Refuse(() => advancedAdmin.GetAsync(readiness), 403, "revoked role with existing bearer");
        Pass("the same bearer cannot read readiness after role revocation");
        Console.WriteLine($"PASS: PENTA ENROLLMENT BROWSER {checks} native HTTP/SQL checks");

        string Recovery(Guid key) => root + $"/browser-starts/{key:D}";
        string Draft(Guid id) => root + $"/browser-conversations/{id:D}/drafts/{Guid.NewGuid():D}";
        Task<HttpResponseMessage> Start(Guid key, HttpClient? client = null) =>
            (client ?? admin).PutAsJsonAsync(root + $"/browser-conversations/{key:D}", new Dictionary<string, string>());
        Task<HttpResponseMessage> Prepare(string route, EnrollmentFixture fixture, string status) =>
            admin.PutAsJsonAsync(route, new { contextVersion = 0, payload = new
            {
                tool_key = PentaEnrollmentFields.CreateTool, tool_revision = 1,
                fields = new
                {
                    StudentId = new { kind = "reference", value = fixture.Student.ToString("D") },
                    BatchId = new { kind = "reference", value = fixture.Target.ToString("D") },
                    StartDate = new { kind = "date", value = "2026-11-01" },
                    Status = new { kind = "text", value = status }
                }
            } }, new JsonSerializerOptions());
        Task<HttpResponseMessage> Approve(string route, PentaEnrollmentDraftView view) =>
            admin.PostAsJsonAsync(route + "/approve", new { view.Revision, view.PayloadHash, intent = PentaEnrollmentCreationWorkflow.ApprovalIntent });
        Task<HttpResponseMessage> Execute(string route, PentaEnrollmentDraftView view, HttpClient? client = null) =>
            (client ?? admin).PostAsJsonAsync(route + "/execute", new { view.Revision, view.PayloadHash, view.ApprovalId });

        static async Task<HttpClient> Client(QaApiFactory api, string account)
        {
            var client = api.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await LoginAsync(client, account, "Synthetic!39Ab"));
            return client;
        }

        static async Task<T> Read<T>(Task<HttpResponseMessage> request, int status, string label) where T : class
        {
            using var response = await request;
            RequireFinanceStatus(response, (HttpStatusCode)status, label);
            PentaRequire(response.Headers.CacheControl?.NoStore == true, "Browser response may be cached.");
            return (await response.Content.ReadFromJsonAsync<T>()) ?? throw new InvalidOperationException("Missing browser response body.");
        }

        async Task Refuse(Func<Task<HttpResponseMessage>> request, int status, string label)
        {
            var snapshot = await State();
            using var response = await request();
            RequireFinanceStatus(response, (HttpStatusCode)status, label);
            PentaRequire(snapshot == await State(), "Refused enrollment changed SQL state: " + label);
        }

        async Task<T> WithDb<T>(Func<AcademyDeskDbContext, Task<T>> read)
        {
            using var scope = factory.Services.CreateScope();
            return await read(scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>());
        }

        Task<(int Sessions, int Drafts, int Enrollments, int Audits)> State() => WithDb(async db =>
            (await db.PentaMiniSessions.CountAsync(), await db.PentaOnboardingDrafts.CountAsync(),
             await db.Enrollments.CountAsync(), await db.AuditLogs.CountAsync()));
        Task<int> Enrollments(EnrollmentFixture fixture) => WithDb(db =>
            db.Enrollments.CountAsync(x => x.AcademyId == academy && x.StudentId == fixture.Student && x.BatchId == fixture.Target));

        async Task VerifySaved(PentaEnrollmentDraftView saved, EnrollmentFixture fixture, string status)
        {
            var receipt = saved.Receipt ?? throw new InvalidOperationException("Missing enrollment receipt.");
            PentaRequire(saved.Status == "Succeeded" && receipt.Enrollment is { } result && result.StudentId == fixture.Student &&
                result.BatchId == fixture.Target && result.Status == status && await Enrollments(fixture) == 1 &&
                await WithDb(db => db.Enrollments.AnyAsync(x => x.Id == receipt.Enrollment.Id && x.Status == status)) &&
                await WithDb(db => db.AuditLogs.CountAsync(x => x.Id == receipt.AuditId && x.Action == executed &&
                    x.AcademyId == academy && x.ActorUserId == actor)) == 1, "Enrollment receipt and stored effect did not agree.");
        }

        Task<int> FillQuota(Guid owner, int count) => WithDb(async db =>
        {
            var time = clock.Value.UtcDateTime;
            db.PentaMiniSessions.AddRange(Enumerable.Range(0, count).Select(_ => new PentaMiniSession
            {
                AcademyId = academy, ActorUserId = owner, CreatedAtUtc = time, ExpiresAtUtc = time.AddHours(24),
                ProtectedState = "Synthetic quota fixture"
            }));
            return await db.SaveChangesAsync();
        });

        void Pass(string label)
        {
            checks++;
            Console.WriteLine("PASS: PENTA ENROLLMENT BROWSER " + label);
        }
    }
}
