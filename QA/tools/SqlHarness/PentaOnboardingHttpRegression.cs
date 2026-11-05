using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
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
    private static async Task VerifyPentaOnboardingHttpAsync(QaApiFactory baseline, QaRunManifest manifest)
    {
        var flags = new Dictionary<string,string?> {
            ["Penta:OperationalDraftContractsEnabled"] = "true",
            ["Penta:OperationalOnboardingExecutionContractsEnabled"] = "true",
            ["Penta:OperationalOnboardingHttpContractsEnabled"] = "true" };
        var clock = new OnboardingDraftClock();
        using var factory = new QaApiFactory(manifest, flags, isolatedClock: clock);
        Guid academy, foreign, session;
        using (var scope = factory.Services.CreateScope()) {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            academy = await db.Academies.Where(x => x.Name == "Synthetic Academy A").Select(x => x.Id).SingleAsync();
            foreign = await db.Academies.Where(x => x.Name == "Synthetic Academy B").Select(x => x.Id).SingleAsync();
        }
        var actor = await CreateAccessActorAsync(factory, "qa-onboarding-http@example.invalid", "AcademyAdmin", academy);
        await CreateAccessActorAsync(factory, "qa-onboarding-other@example.invalid", "AcademyAdmin", academy);
        var platform = await CreateAccessActorAsync(factory, "qa-onboarding-platform@example.invalid", "PlatformOwner", academy);
        using (var scope = factory.Services.CreateScope()) {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var identity = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            (await identity.Users.SingleAsync(x => x.Id == platform)).IsPlatformOwner = true;
            await identity.SaveChangesAsync();
            var row = new PentaMiniSession { AcademyId = academy, ActorUserId = actor, Version = 1,
                ExpiresAtUtc = clock.Value.UtcDateTime.AddHours(1), ProtectedState = "Synthetic HTTP fixture; no inference" };
            db.PentaMiniSessions.Add(row); await db.SaveChangesAsync(); session = row.Id;
        }
        async Task<HttpClient> Client(string email, QaApiFactory? host = null) {
            var client = (host ?? factory).CreateClient();
            var token = await LoginAsync(client, email, "Synthetic!39Ab");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }
        using var admin = await Client("qa-onboarding-http@example.invalid");
        using var other = await Client("qa-onboarding-other@example.invalid");
        using var teacher = await Client("qa-teacher-a@example.invalid");
        using var owner = await Client("qa-onboarding-platform@example.invalid");
        using var anonymous = factory.CreateClient();
        using var invalidBearer = factory.CreateClient();
        invalidBearer.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid-synthetic-token");
        string Route(Guid id, string suffix = "", Guid? tenant = null) =>
            $"/api/academies/{tenant ?? academy}/penta/onboarding/conversations/{session}/drafts/{id}{suffix}";
        // Tool field keys are case-sensitive protocol names, not Web JSON DTO
        // property names. A dictionary retains the exact PascalCase keys.
        object Input(string suffix) => new { expectedRevision = 0, contextVersion = 1, fields = new Dictionary<string,object> {
            ["StudentFirstName"] = new { kind = "text", value = "Synthetic" },
            ["StudentLastName"] = new { kind = "text", value = suffix }, ["DateOfBirth"] = new { kind = "date", value = "2000-01-01" } } };
        object Binding(PentaOnboardingDraftView draft) => new { revision = draft.Revision, payloadHash = draft.PayloadHash };
        async Task<string> Snapshot() {
            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var identity = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            return JsonSerializer.Serialize(new {
                students = await db.Students.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(),
                guardians = await db.Guardians.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(),
                links = await db.StudentGuardians.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(),
                drafts = await db.PentaOnboardingDrafts.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(),
                audits = await db.AuditLogs.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(),
                users = await identity.Users.AsNoTracking().OrderBy(x => x.Id).Select(x => new { x.Id, x.AcademyId, x.UserName }).ToArrayAsync(),
                roles = await identity.Roles.AsNoTracking().OrderBy(x => x.Id).Select(x => new { x.Id, x.Name }).ToArrayAsync(),
                memberships = await identity.UserRoles.AsNoTracking().OrderBy(x => x.UserId).ThenBy(x => x.RoleId).ToArrayAsync() });
        }
        var count = 0;
        void Pass(string label) { count++; Console.WriteLine("PASS: PENTAONBOARDINGHTTP " + label); }
        async Task Deny(string label, int expected, Func<Task<HttpResponseMessage>> call) {
            var before = await Snapshot(); using var response = await call();
            if ((int)response.StatusCode != expected || before != await Snapshot())
                throw new InvalidOperationException("HTTP onboarding denial failed: " + label + " expected=" + expected + " actual=" + (int)response.StatusCode);
            Pass(label + " status=" + expected + " no domain/Identity/draft/audit changes");
        }
        async Task<PentaOnboardingDraftView> Save(string suffix) {
            var id = Guid.NewGuid(); using var response = await admin.PutAsJsonAsync(Route(id), Input(suffix));
            if ((int)response.StatusCode != 201 || response.Headers.CacheControl?.NoStore != true)
                throw new InvalidOperationException("HTTP save/cache contract failed: " + (int)response.StatusCode);
            return (await response.Content.ReadFromJsonAsync<PentaOnboardingDraftView>())!;
        }
        async Task Authorize(PentaOnboardingDraftView draft) {
            using var response = await admin.PostAsJsonAsync(Route(draft.Id, "/authorize-creation"), new {
                revision = draft.Revision, payloadHash = draft.PayloadHash, intent = PentaOnboardingDraftService.CreationIntent });
            var body = await response.Content.ReadFromJsonAsync<PentaOnboardingDraftView>();
            if ((int)response.StatusCode != 200 || body?.Status != "Authorized" || body.CanExecute)
                throw new InvalidOperationException("HTTP explicit authorization failed: " + (int)response.StatusCode);
        }
        var unknown = Guid.NewGuid();
        await Deny("anonymous", 401, () => anonymous.PutAsJsonAsync(Route(unknown), Input("Unauthorized")));
        await Deny("invalid bearer", 401, () => invalidBearer.GetAsync(Route(unknown)));
        await Deny("teacher role", 403, () => teacher.PutAsJsonAsync(Route(unknown), Input("Teacher denied")));
        await Deny("platform owner is not academy onboarding authority", 403, () => owner.PutAsJsonAsync(Route(unknown), Input("Platform denied")));
        await Deny("cross-tenant", 403, () => admin.PutAsJsonAsync(Route(unknown, tenant: foreign), Input("Foreign denied")));
        using (var disabled = await Client("qa-onboarding-http@example.invalid", baseline))
            await Deny("default-disabled adapter", 404, () => disabled.PutAsJsonAsync(Route(unknown), Input("Disabled")));
        using (var cookieOnly = factory.CreateClient()) {
            using var login = await cookieOnly.PostAsJsonAsync("/api/auth/login?useCookies=true", new { email = "qa-onboarding-http@example.invalid", password = "Synthetic!39Ab" });
            if (!login.IsSuccessStatusCode || !login.Headers.Contains("Set-Cookie")) throw new InvalidOperationException("Cookie-only fixture failed.");
            await Deny("cookie cannot authorize bearer-only write", 401, () => cookieOnly.PutAsJsonAsync(Route(unknown), Input("Cookie denied")));
        }
        foreach (var body in new[] {
            "{\"expectedRevision\":0,\"contextVersion\":1,\"fields\":{},\"actorRole\":\"Owner\"}",
            "{\"expectedRevision\":0,\"expectedRevision\":1,\"contextVersion\":1,\"fields\":{}}",
            "{\"expectedRevision\":0,\"contextVersion\":1,\"fields\":{\"StudentPassword\":{\"kind\":\"text\",\"value\":\"synthetic-not-a-real-secret\"}}}",
            "{\"expectedRevision\":0,\"contextVersion\":1,\"fields\":{},\"tool_key\":\"ExecuteSQL\"}",
            "{\"expectedRevision\":0,\"contextVersion\":1,\"fields\":{},\"approved\":true}" }) {
            await Deny("strict save contract " + (count - 6), 400, () => admin.PutAsync(Route(unknown), new StringContent(body, Encoding.UTF8, "application/json")));
        }
        var draft = await Save("HTTP one-time execution");
        Pass("save returns protected draft projection with no-store and no execution activation");
        await Deny("other same-academy admin cannot read owned draft", 404, () => other.GetAsync(Route(draft.Id)));
        using (var get = await admin.GetAsync(Route(draft.Id))) {
            if ((int)get.StatusCode != 200 || (await get.Content.ReadFromJsonAsync<PentaOnboardingDraftView>())?.Status != "AwaitingReview")
                throw new InvalidOperationException("HTTP owned status failed.");
        }
        Pass("owned GET reads authoritative current status");
        await Deny("unconfirmed execution", 409, () => admin.PostAsJsonAsync(Route(draft.Id, "/execute"), Binding(draft)));
        using (var review = await admin.PostAsJsonAsync(Route(draft.Id, "/review"), Binding(draft))) {
            if ((int)review.StatusCode != 200 || (await review.Content.ReadFromJsonAsync<PentaOnboardingDraftView>())?.Status != "Confirmed")
                throw new InvalidOperationException("HTTP review failed.");
        }
        Pass("review remains separate from creation permission");
        await Deny("ordinary review cannot execute", 409, () => admin.PostAsJsonAsync(Route(draft.Id, "/execute"), Binding(draft)));
        await Deny("generic yes cannot authorize", 400, () => admin.PostAsJsonAsync(Route(draft.Id, "/authorize-creation"),
            new { revision = draft.Revision, payloadHash = draft.PayloadHash, intent = "yes" }));
        await Deny("model-supplied approval cannot execute", 400, () => admin.PostAsJsonAsync(Route(draft.Id, "/execute"),
            new { revision = draft.Revision, payloadHash = draft.PayloadHash, approved = true }));
        await Authorize(draft); Pass("exact scoped human creation intent authorizes only current draft");
        await Deny("stale revision", 409, () => admin.PostAsJsonAsync(Route(draft.Id, "/execute"), new { revision = 2, payloadHash = draft.PayloadHash }));
        await Deny("wrong digest", 409, () => admin.PostAsJsonAsync(Route(draft.Id, "/execute"), new { revision = 1, payloadHash = new string('f', 64) }));
        var racing = await Task.WhenAll(admin.PostAsJsonAsync(Route(draft.Id, "/execute"), Binding(draft)), admin.PostAsJsonAsync(Route(draft.Id, "/execute"), Binding(draft)));
        PentaOnboardingReceipt receipt;
        try {
            if (!racing.Select(x => (int)x.StatusCode).Order().SequenceEqual(new[] { 200, 201 })) throw new InvalidOperationException("HTTP one-time race statuses failed.");
            receipt = (await racing[0].Content.ReadFromJsonAsync<PentaOnboardingReceipt>())!;
            if (receipt != await racing[1].Content.ReadFromJsonAsync<PentaOnboardingReceipt>()) throw new InvalidOperationException("HTTP race receipts differed.");
        } finally { foreach (var response in racing) response.Dispose(); }
        using (var scope = factory.Services.CreateScope()) {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            if (await db.Students.CountAsync(x => x.AcademyId == academy && x.LastName == "HTTP one-time execution") != 1 ||
                await db.AuditLogs.CountAsync(x => x.EntityId == draft.Id && x.Action == "PentaOnboardingExecuted") != 1 ||
                await db.AuditLogs.AnyAsync(x => x.ActorUserId == actor && (x.Action == "POST PentaOnboarding" || x.Action == "PUT PentaOnboarding")))
                throw new InvalidOperationException("HTTP atomic source/audit counts failed.");
        }
        Pass("concurrent HTTP execution 201/200, identical receipt, one student/audit and no generic audit");
        var beforeReplay = await Snapshot();
        using (var replay = await admin.PostAsJsonAsync(Route(draft.Id, "/execute"), Binding(draft))) {
            if ((int)replay.StatusCode != 200 || receipt != await replay.Content.ReadFromJsonAsync<PentaOnboardingReceipt>() || beforeReplay != await Snapshot())
                throw new InvalidOperationException("HTTP receipt reconciliation redispatched.");
        }
        Pass("replay reconciles committed receipt without state changes");
        using (var get = await admin.GetAsync(Route(draft.Id))) {
            if ((await get.Content.ReadFromJsonAsync<PentaOnboardingDraftView>())?.Status != "Succeeded") throw new InvalidOperationException("HTTP committed status failed.");
        }
        Pass("HTTP status reads committed Succeeded");
        var rollback = await Save("HTTP audit rollback"); await Authorize(rollback);
        var fault = new OnboardingExecutionFault("audit");
        using (var faulty = new QaApiFactory(manifest, flags, isolatedDomainInterceptor: fault, isolatedClock: clock))
        using (var faultClient = await Client("qa-onboarding-http@example.invalid", faulty)) {
            var before = await Snapshot(); using var response = await faultClient.PostAsJsonAsync(Route(rollback.Id, "/execute"), Binding(rollback));
            var message = await response.Content.ReadAsStringAsync();
            if ((int)response.StatusCode != 503 || fault.Faults != 1 || before != await Snapshot() ||
                !message.Contains("Do not create another request", StringComparison.Ordinal) || message.Contains("Synthetic execution fault", StringComparison.Ordinal) ||
                message.Contains("SqlException", StringComparison.Ordinal) || message.Contains("INSERT INTO", StringComparison.Ordinal))
                throw new InvalidOperationException("HTTP audit failure did not safely roll back/reconcile.");
        }
        Pass("mandatory audit failure is safe 503 with full rollback, not false success");
        using (var retry = await admin.PostAsJsonAsync(Route(rollback.Id, "/execute"), Binding(rollback)))
            if ((int)retry.StatusCode != 201) throw new InvalidOperationException("HTTP explicit post-rollback retry failed.");
        Pass("explicit retry after verified rollback creates once");
        var stale = await Save("HTTP changed context"); await Authorize(stale);
        async Task SetVersion(long version) {
            using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            (await db.PentaMiniSessions.SingleAsync(x => x.Id == session)).Version = version; await db.SaveChangesAsync();
        }
        await SetVersion(2);
        await Deny("changed conversation context", 409, () => admin.PostAsJsonAsync(Route(stale.Id, "/execute"), Binding(stale)));
        await SetVersion(1);
        clock.Value = clock.Value.AddMinutes(11);
        await Deny("expired creation permission", 410, () => admin.PostAsJsonAsync(Route(stale.Id, "/execute"), Binding(stale)));
        using (var scope = factory.Services.CreateScope()) {
            var identity = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            (await identity.Users.SingleAsync(x => x.Id == actor)).IsActive = false; await identity.SaveChangesAsync();
        }
        await Deny("revoked actor cannot reconcile committed result", 403, () => admin.PostAsJsonAsync(Route(draft.Id, "/execute"), Binding(draft)));
        using (var scope = factory.Services.CreateScope()) {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = (await users.FindByIdAsync(actor.ToString()))!; user.IsActive = true;
            if (!(await users.UpdateAsync(user)).Succeeded || !(await users.RemoveFromRoleAsync(user, "AcademyAdmin")).Succeeded)
                throw new InvalidOperationException("HTTP role revocation fixture failed.");
        }
        await Deny("old bearer role cannot grant current SQL role", 403, () => admin.PostAsJsonAsync(Route(draft.Id, "/execute"), Binding(draft)));
        if (count != 32) throw new InvalidOperationException("Incomplete HTTP onboarding matrix: " + count);
        Console.WriteLine("PASS: PENTAONBOARDINGHTTP all " + count + " authenticated API/real SQL cases; host-only test gates, current authority, explicit authorization, one-time result/audit, safe rollback. No model/frontend/production activation.");
    }
}
