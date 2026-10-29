using System.Data.Common;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Domain.Identity;
using AcademyDesk.Api.Infrastructure.Meetings;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

internal static partial class SqlHarnessEntryPoint
{
    private const string ZoomAccountAudit = "MeetingZoomPlanObservedExecutionQualificationRequired";
    private sealed record ZoomAccountSource(MeetingOAuthRegistration Registration, MeetingAccountPolicyRegistry Policies, MeetingAccountReviewState Review, MeetingOAuthExchangePermit Permit, int Binding);
    private sealed class ZoomAccountHandler(string body, string access, Func<Task>? during, HttpStatusCode status) : HttpMessageHandler
    {
        internal int Calls;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            Calls++;
            if (request.Method != HttpMethod.Get || request.RequestUri!.AbsoluteUri != MeetingZoomAccountProbe.Endpoint || request.Headers.Authorization?.Scheme != "Bearer" || request.Headers.Authorization.Parameter != access || request.Content is not null || request.RequestUri.AbsoluteUri.Contains(access)) throw new InvalidOperationException("Unsafe Zoom account request");
            if (during is not null) await during(); return new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
        }
    }
    private sealed class ZoomAccountAuditFault(string action = ZoomAccountAudit) : DbTransactionInterceptor
    {
        public override ValueTask<InterceptionResult> TransactionCommittingAsync(DbTransaction transaction, TransactionEventData data, InterceptionResult result, CancellationToken token = default)
        { if (data.Context!.ChangeTracker.Entries<AuditLog>().Any(x => x.State == EntityState.Added && x.Entity.Action == action)) throw new InvalidOperationException("Synthetic Zoom account audit commit failure"); return ValueTask.FromResult(result); }
    }
    private sealed class ZoomAccountFinalBarrier : SaveChangesInterceptor
    {
        private readonly ClaimBarrier barrier = new();
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData data, InterceptionResult<int> result, CancellationToken token = default) =>
            data.Context!.ChangeTracker.Entries<AuditLog>().Any(x => x.State == EntityState.Added && (x.Entity.Action == ZoomAccountAudit || x.Entity.Action == "MeetingDisconnectedLocallyRevocationRequired"))
                ? barrier.SavingChangesAsync(data, result, token) : ValueTask.FromResult(result);
    }
    private static async Task VerifyMeetingZoomAccountAsync(QaApiFactory factory, QaRunManifest manifest)
    {
        string connection; using (var scope = factory.Services.CreateScope()) connection = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>().Database.GetConnectionString()!;
        var target = new SqlConnectionStringBuilder(connection); if (target.DataSource != manifest.SqlServer || target.InitialCatalog != manifest.Database || target.UserID != manifest.RuntimeLogin) throw new InvalidOperationException("Zoom account requires exact owned SQL target");
        AcademyDeskDbContext Open(IInterceptor? interceptor = null) { var b = new DbContextOptionsBuilder<AcademyDeskDbContext>().UseSqlServer(connection); if (interceptor is not null) b.AddInterceptors(interceptor); return new(b.Options); }
        var tenant = new Academy { Name = "Synthetic Zoom Account Academy", EnabledModulesJson = "[\"Core\",\"Engagement\"]" }; await using (var db = Open()) { db.Add(tenant); await db.SaveChangesAsync(); }
        var academy = tenant.Id; var actorId = await CreateAccessActorAsync(factory, "qa-zoom-account@example.invalid", "AcademyAdmin", academy); var otherId = await CreateAccessActorAsync(factory, "qa-zoom-account-other@example.invalid", "AcademyAdmin", academy);
        ClaimsPrincipal Actor(Guid id) => new(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, id.ToString()) }, "synthetic-zoom-account"));
        var actor = Actor(actorId); var clock = new OperationClock(); var protection = factory.Services.GetRequiredService<IDataProtectionProvider>();
        async Task<T> Run<T>(Func<MeetingOAuthAttemptStore, Task<T>> action, IInterceptor? interceptor = null)
        { using var scope = factory.Services.CreateScope(); await using var db = Open(interceptor); var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(); return await action(new(db, users, new(db, users, clock), protection, clock)); }
        const string subject = "synthetic-zoom-user", account = "synthetic-zoom-account", access = "synthetic-zoom-access";
        var reg = MeetingOAuthRegistrationRegistry.Create("https://api.academy.invalid", "https://app.academy.invalid", [new("Zoom", 1, "synthetic-zoom-client", "https://api.academy.invalid/api/meetings/oauth/Zoom/callback", "https://app.academy.invalid/communication-settings", "zoom-public-pkce/1")]).Resolve("Zoom")!;
        string Body(int type = 2, string status = "active", string user = subject, string owner = account) => JsonSerializer.Serialize(new { id = user, account_id = owner, status, type });
        async Task<ZoomAccountSource> Source(bool confirm = true)
        {
            int version; await using (var db = Open()) version = await db.MeetingProviderBindings.Where(x => x.AcademyId == academy && x.Provider == reg.Provider).Select(x => (int?)x.Version).SingleOrDefaultAsync() ?? 0;
            return await Run(async store =>
            {
                var browser = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32)); var intent = await store.PrepareAsync(actor, academy, reg.Provider, version, browser, reg.Fingerprint);
                if (intent.StatusCode != 201 || (await store.CaptureAsync(academy, reg.Provider, intent.Material!.State, browser, reg.Fingerprint, "synthetic-zoom-code", false)).StatusCode != 202) throw new InvalidOperationException("Zoom source capture failed");
                var claim = await store.ResumeAsync(actor, academy, reg.Provider, intent.Material.State, browser, reg.Fingerprint); var permit = (await store.BeginExchangeAsync(actor, academy, reg, claim)).Permit!;
                if ((await store.FinishExchangeAsync(actor, academy, reg, permit, new(200, "TokensReceivedAccountUnverified", Tokens: new(access, "synthetic-zoom-refresh", clock.Now.AddHours(1), reg.Flow!.RequiredTokenScopes)))).StatusCode != 200) throw new InvalidOperationException("Zoom quarantine failed");
                var review = (await store.PrepareAccountReviewAsync(actor, academy, reg, permit, new(subject, account, clock.Now))).Review;
                if (review is null || (confirm && (await store.ConfirmAccountReviewAsync(actor, academy, reg, review.Id, 0, review.ReviewHash, true)).StatusCode != 200)) throw new InvalidOperationException("Zoom review failed");
                return new ZoomAccountSource(reg, MeetingAccountPolicyRegistry.Create([new(academy, reg.Provider, 1, reg.Fingerprint, subject, account, null, clock.Now.AddMinutes(-1), clock.Now.AddHours(4))]), review, permit, version + 2);
            });
        }
        async Task<MeetingZoomAccountOutcome> Evaluate(ZoomAccountSource source, Func<Task>? during = null, IInterceptor? interceptor = null, string? response = null, HttpStatusCode status = HttpStatusCode.OK, ClaimsPrincipal? who = null, Guid? route = null, bool noCall = false)
        {
            using var handler = new ZoomAccountHandler(response ?? Body(), access, during, status); using var transport = new HttpMessageInvoker(handler);
            var result = await Run(s => s.CheckZoomAccountAsync(who ?? actor, route ?? academy, source.Registration, source.Review.Id, source.Policies, new(transport)), interceptor);
            if (result.Connected || result.ProviderVerified || result.CanExecute || result.CanRetry || JsonSerializer.Serialize(result).Contains(subject) || JsonSerializer.Serialize(result).Contains(account)) throw new InvalidOperationException("Zoom observation leaked identity or enabled execution");
            if (noCall && handler.Calls != 0) throw new InvalidOperationException("Unauthorized Zoom bearer use"); return result;
        }
        var checks = new List<string>(); void Check(string name, bool pass) { if (!pass) throw new InvalidOperationException("MEETING ZOOM ACCOUNT " + name); checks.Add(name); Console.WriteLine("MEETING ZOOM ACCOUNT CASE " + name + " PASS"); }
        var source = await Source(); Check("licensed-plan-is-observation-still-503-not-executable", (await Evaluate(source)) is { StatusCode: 503, AccountMatched: true, MeetingPlanObserved: true });
        Check("basic-plan-not-mistaken-for-unassigned-still-non-executable", (await Evaluate(source, response: Body(1))) is { StatusCode: 503, MeetingPlanObserved: true });
        await using (var db = Open())
        {
            var audits = await db.AuditLogs.Where(x => x.EntityId == source.Review.Id && x.Action == ZoomAccountAudit).ToArrayAsync();
            Check("private-audit-no-user-account-token-or-provider-data", audits.Length == 2 && audits.All(x => !x.MetadataJson!.Contains(subject) && !x.MetadataJson.Contains(account) && !x.MetadataJson.Contains(access)));
            Check("no-binding-source-version-or-scope-expansion", await db.MeetingProviderBindings.AnyAsync(x => x.AcademyId == academy && x.Status == "VerificationRequired" && x.Version == source.Binding) && await db.MeetingOAuthExchanges.AnyAsync(x => x.Id == source.Permit.Id && x.Version == 1) && reg.Flow!.RequestedScopes.All(x => !x.EndsWith(":admin")));
            Check("required-credential-read-audit-before-each-provider-read", await db.AuditLogs.CountAsync(x => x.EntityId == source.Permit.Id && x.Action == "MeetingOAuthQuarantinedTokensRead") == 2);
        }
        async Task Denied(string label, Func<Task<MeetingZoomAccountOutcome>> request, int status)
        {
            int before; await using (var db = Open()) before = await db.AuditLogs.CountAsync(x => x.Action == ZoomAccountAudit); var result = await request();
            await using (var db = Open()) Check(label, result.StatusCode == status && !result.MeetingPlanObserved && await db.AuditLogs.CountAsync(x => x.Action == ZoomAccountAudit) == before);
        }
        await Denied("inactive-account-not-observed", () => Evaluate(source, response: Body(status: "inactive")), 409);
        await Denied("unassigned-without-meetings-basic-not-observed", () => Evaluate(source, response: Body(4)), 409);
        await Denied("legacy-none-type-not-observed", () => Evaluate(source, response: Body(99)), 409);
        await Denied("changed-provider-user-denied", () => Evaluate(source, response: Body(user: "other-user")), 409);
        await Denied("changed-provider-account-denied", () => Evaluate(source, response: Body(owner: "other-account")), 409);
        await Denied("permission-denial-no-scope-or-role-fallback", () => Evaluate(source, status: HttpStatusCode.Forbidden), 502);
        await Denied("pagination-not-followed-or-partially-accepted", () => Evaluate(source, response: Body()[..^1] + ",\"next_page_token\":\"private\"}"), 502);
        await Denied("malformed-late-nested-data-not-partial-success", () => Evaluate(source, response: Body()[..^1] + ",\"extra\":{\"id\":1,\"ID\":2}}"), 502);
        await Denied("foreign-academy-no-provider-call", () => Evaluate(source, route: Guid.NewGuid(), noCall: true), 403);
        await Denied("other-current-admin-no-provider-call", () => Evaluate(source, who: Actor(otherId), noCall: true), 404);
        await Denied("anonymous-no-provider-call", () => Evaluate(source, who: new ClaimsPrincipal(), noCall: true), 401);
        var unconfirmed = await Source(false); await Denied("unconfirmed-review-no-provider-call", () => Evaluate(unconfirmed, noCall: true), 409);
        source = await Source(); await Denied("missing-policy-no-provider-call", () => Evaluate(source with { Policies = MeetingAccountPolicyRegistry.Disabled }, noCall: true), 503);
        var fault = await Source(); var failed = false;
        try { await Evaluate(fault, interceptor: new ZoomAccountAuditFault()); } catch (InvalidOperationException ex) when (ex.Message == "Synthetic Zoom account audit commit failure") { failed = true; }
        await using (var db = Open()) Check("post-SQL-final-audit-fault-rolls-back-no-success", failed && !await db.AuditLogs.AnyAsync(x => x.EntityId == fault.Review.Id && x.Action == ZoomAccountAudit) && await db.MeetingOAuthExchanges.AnyAsync(x => x.Id == fault.Permit.Id && x.Version == 1));
        Check("read-after-proved-rollback-remains-unavailable", (await Evaluate(fault)) is { StatusCode: 503, MeetingPlanObserved: true, CanExecute: false });
        var readFault = await Source(); var readFailed = false; var calls = 0;
        try { await Evaluate(readFault, during: () => { calls++; return Task.CompletedTask; }, interceptor: new ZoomAccountAuditFault("MeetingOAuthQuarantinedTokensRead")); } catch (InvalidOperationException ex) when (ex.Message == "Synthetic Zoom account audit commit failure") { readFailed = true; }
        await using (var db = Open()) Check("read-audit-fault-prevents-bearer-use", readFailed && calls == 0 && !await db.AuditLogs.AnyAsync(x => x.EntityId == readFault.Permit.Id && x.Action == "MeetingOAuthQuarantinedTokensRead"));
        var refresh = await Source(); Check("public-refresh-remains-explicitly-unqualified", (await Run(s => s.BeginRefreshAsync(actor, academy, reg, refresh.Permit, 1))) is { StatusCode: 503, Code: "ProviderRefreshUnqualified", Material: null });
        var disconnect = await Source(); await Denied("disconnect-during-read-invalidates-observation", () => Evaluate(disconnect, async () => { if ((await Run(s => s.DisconnectLocallyAsync(actor, academy, reg.Provider, disconnect.Binding, true))).StatusCode != 200) throw new InvalidOperationException("Zoom disconnect fixture failed"); }), 409);
        var race = await Source(); var barrier = new ZoomAccountFinalBarrier(); var reading = Evaluate(race, interceptor: barrier); var dropping = Run(s => s.DisconnectLocallyAsync(actor, academy, reg.Provider, race.Binding, true), barrier); await Task.WhenAll(reading, dropping);
        await using (var db = Open()) Check("final-audit-disconnect-race-never-enables-binding", (await dropping).StatusCode == 200 && (await reading).StatusCode is 409 or 503 && !(await reading).CanExecute && await db.MeetingProviderBindings.AnyAsync(x => x.AcademyId == academy && x.Status == "Disconnected" && x.Version == race.Binding + 1));
        var corrupt = await Source(); await using (var db = Open()) { var row = await db.MeetingOAuthExchanges.SingleAsync(x => x.Id == corrupt.Permit.Id); row.ProtectedEnvelope = "corrupt"; await db.SaveChangesAsync(); }
        await Denied("corrupt-source-no-provider-call", () => Evaluate(corrupt, noCall: true), 409);
        var expiry = await Source(); var priorTime = clock.Now; await Denied("expiry-during-read-invalidates-observation", () => Evaluate(expiry, () => { clock.Now = clock.Now.AddHours(2); return Task.CompletedTask; }), 410); clock.Now = priorTime;
        var stamp = await Source(); await Denied("stamp-change-during-read-invalidates-observation", () => Evaluate(stamp, async () => { using var scope = factory.Services.CreateScope(); var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(); if (!(await users.UpdateSecurityStampAsync((await users.FindByIdAsync(actorId.ToString()))!)).Succeeded) throw new InvalidOperationException("Zoom stamp fixture failed"); }), 409);
        var role = await Source(); await Denied("role-revoked-during-read-invalidates-observation", () => Evaluate(role, async () => { using var scope = factory.Services.CreateScope(); var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(); if (!(await users.RemoveFromRoleAsync((await users.FindByIdAsync(actorId.ToString()))!, "AcademyAdmin")).Succeeded) throw new InvalidOperationException("Zoom role fixture failed"); }), 403);
        if (checks.Count != 28) throw new InvalidOperationException("Zoom account case count mismatch " + checks.Count);
        var sources = new[] { "apps/api/Infrastructure/Meetings/MeetingZoomAccountProbe.cs", "apps/api/Infrastructure/Meetings/MeetingZoomAccountStore.cs", "apps/api/Infrastructure/Meetings/MeetingOAuthFlowProfile.cs", "apps/api/Infrastructure/Meetings/MeetingAccountPolicyStore.cs", "tests/AcademyDesk.Api.Tests/MeetingZoomAccountTests.cs", "QA/tools/SqlHarness/MeetingZoomAccountRegression.cs" }.ToDictionary(p => p, p => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(p))));
        var path = Path.Combine("QA", "EVIDENCE", "meeting-zoom-account-sql-" + manifest.RunId.ToString("N")); Directory.CreateDirectory(path);
        await File.WriteAllTextAsync(Path.Combine(path, "result.json"), JsonSerializer.Serialize(new { checks, sources, scope = "Owned SQL/Identity, fixed synthetic Zoom user/account/plan responses only; not live consent/meeting limits/write entitlement, Connected or release acceptance" }, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine("MEETING ZOOM ACCOUNT PASS:28 SQL/Identity cases; plan observation is not execution entitlement.");
    }
}
