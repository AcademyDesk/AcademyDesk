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
    private sealed record ZoomRefreshSource(MeetingOAuthRegistration Registration, MeetingOAuthExchangePermit Permit, MeetingAccountReviewState Review, int Binding);
    private sealed class ZoomRefreshFinalBarrier : SaveChangesInterceptor
    {
        private readonly ClaimBarrier barrier = new();
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData data, InterceptionResult<int> result, CancellationToken token = default) =>
            data.Context!.ChangeTracker.Entries<AuditLog>().Any(x => x.State == EntityState.Added && (x.Entity.Action == "MeetingRefreshTokensQuarantined" || x.Entity.Action == "MeetingDisconnectedLocallyRevocationRequired"))
                ? barrier.SavingChangesAsync(data, result, token) : ValueTask.FromResult(result);
    }
    private static async Task VerifyMeetingZoomRefreshAsync(QaApiFactory factory, QaRunManifest manifest)
    {
        string connection; using (var scope = factory.Services.CreateScope()) connection = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>().Database.GetConnectionString()!;
        var target = new SqlConnectionStringBuilder(connection); if (target.DataSource != manifest.SqlServer || target.InitialCatalog != manifest.Database || target.UserID != manifest.RuntimeLogin) throw new InvalidOperationException("Zoom refresh requires exact owned SQL target");
        AcademyDeskDbContext Open(IInterceptor? interceptor = null) { var b = new DbContextOptionsBuilder<AcademyDeskDbContext>().UseSqlServer(connection); if (interceptor is not null) b.AddInterceptors(interceptor); return new(b.Options); }
        var tenant = new Academy { Name = "Synthetic Zoom Backend Refresh Academy", EnabledModulesJson = "[\"Core\",\"Engagement\"]" }; await using (var db = Open()) { db.Add(tenant); await db.SaveChangesAsync(); }
        var academy = tenant.Id; var actorId = await CreateAccessActorAsync(factory, "qa-zoom-refresh@example.invalid", "AcademyAdmin", academy); var otherId = await CreateAccessActorAsync(factory, "qa-zoom-refresh-other@example.invalid", "AcademyAdmin", academy);
        ClaimsPrincipal Actor(Guid id) => new(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, id.ToString()) }, "synthetic-zoom-refresh"));
        var actor = Actor(actorId); var clock = new OperationClock(); var protection = factory.Services.GetRequiredService<IDataProtectionProvider>();
        async Task<T> Run<T>(Func<MeetingOAuthAttemptStore, Task<T>> action, IInterceptor? interceptor = null)
        { using var scope = factory.Services.CreateScope(); await using var db = Open(interceptor); var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(); return await action(new(db, users, new(db, users, clock), protection, clock)); }
        const string secret = "synthetic-backend-secret", oldRefresh = "synthetic-zoom-refresh-old", nextRefresh = "synthetic-zoom-refresh-next", access = "synthetic-zoom-access-next", subject = "synthetic-backend-user", account = "synthetic-backend-account";
        MeetingOAuthRegistration Registration(bool backend = true) => MeetingOAuthRegistrationRegistry.Create("https://api.academy.invalid", "https://app.academy.invalid", [new("Zoom", 1, "synthetic-backend-client", "https://api.academy.invalid/api/meetings/oauth/Zoom/callback", "https://app.academy.invalid/communication-settings", backend ? MeetingOAuthFlowProfile.ZoomConfidentialProfile : "zoom-public-pkce/1")]).Resolve("Zoom")!;
        var reg = Registration();
        string Body(string refresh = nextRefresh, string bearer = access) => JsonSerializer.Serialize(new { access_token = bearer, refresh_token = refresh, token_type = "Bearer", expires_in = 3600, scope = string.Join(' ', reg.Flow!.RequiredTokenScopes), api_url = "https://api.zoom.us" });
        HttpResponseMessage Response(string body) => new(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
        async Task CheckRequest(HttpRequestMessage request, string grant, string refresh = oldRefresh)
        {
            var form = await request.Content!.ReadAsStringAsync();
            if (request.Method != HttpMethod.Post || request.RequestUri!.AbsoluteUri != "https://zoom.us/oauth/token" || request.Headers.Authorization?.Scheme != "Basic" ||
                Encoding.UTF8.GetString(Convert.FromBase64String(request.Headers.Authorization.Parameter!)) != reg.ClientId + ":" + secret || form.Contains("client_secret") || form.Contains(secret) || !form.Contains("grant_type=" + grant) ||
                (grant == "refresh_token" && (!form.Contains("refresh_token=" + refresh) || form.Contains("code_verifier") || form.Contains("scope="))) || request.Headers.CacheControl?.NoStore != true) throw new InvalidOperationException("Unsafe Zoom backend token request");
        }
        async Task<ZoomRefreshSource> Source()
        {
            int version; await using (var db = Open()) version = await db.MeetingProviderBindings.Where(x => x.AcademyId == academy && x.Provider == reg.Provider).Select(x => (int?)x.Version).SingleOrDefaultAsync() ?? 0;
            using var scope = factory.Services.CreateScope(); await using var context = Open(); var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(); var store = new MeetingOAuthAttemptStore(context, users, new(context, users, clock), protection, clock);
            var browser = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32)); var start = await store.PrepareAsync(actor, academy, reg.Provider, version, browser, reg.Fingerprint);
            if (start.StatusCode != 201 || (await store.CaptureAsync(academy, reg.Provider, start.Material!.State, browser, reg.Fingerprint, "synthetic-backend-code", false)).StatusCode != 202) throw new InvalidOperationException("Zoom backend capture fixture failed");
            var claim = await store.ResumeAsync(actor, academy, reg.Provider, start.Material.State, browser, reg.Fingerprint);
            using var handler = new RefreshTransport(async request => {
                await CheckRequest(request, "authorization_code"); var form = await request.Content!.ReadAsStringAsync();
                var stateHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(start.Material.State))); var attemptId = await context.MeetingOAuthAttempts.Where(x => x.StateHash == stateHash).Select(x => x.Id).SingleAsync();
                await using var db = Open(); if (!form.Contains("code_verifier=" + claim.Material!.Verifier) || !await db.MeetingOAuthExchanges.AnyAsync(x => x.AttemptId == attemptId && x.Status == "Started")) throw new InvalidOperationException("Zoom backend exchange not durably admitted with PKCE");
                return Response(Body(oldRefresh, "synthetic-zoom-old-access")); });
            using var transport = new HttpMessageInvoker(handler); var admitted = await store.BeginExchangeAsync(actor, academy, reg, claim);
            if (admitted.StatusCode != 201 || admitted.Permit is null) throw new InvalidOperationException("Zoom backend exchange admission fixture failed");
            var exchanged = await new MeetingOAuthCodeExchange(context, users, new(context, users, clock), transport, clock).ExchangeAsync(actor, academy, reg, claim, secret);
            var result = await store.FinishExchangeAsync(actor, academy, reg, admitted.Permit, exchanged);
            if (result.StatusCode != 200 || handler.Calls != 1) throw new InvalidOperationException("Zoom backend code exchange fixture failed");
            var review = (await store.PrepareAccountReviewAsync(actor, academy, reg, admitted.Permit, new(subject, account, clock.Now))).Review!;
            if ((await store.ConfirmAccountReviewAsync(actor, academy, reg, review.Id, 0, review.ReviewHash, true)).StatusCode != 200) throw new InvalidOperationException("Zoom backend review fixture failed");
            return new(reg, admitted.Permit, review, version + 2);
        }
        MeetingOAuthExchangeOutcome Tokens(string refresh = nextRefresh) => new(200, "TokensReceivedAccountUnverified", Tokens: new(access, refresh, clock.Now.AddHours(1), reg.Flow!.RequiredTokenScopes));
        var checks = new List<string>(); void Check(string label, bool pass) { if (!pass) throw new InvalidOperationException("MEETING ZOOM REFRESH " + label); checks.Add(label); Console.WriteLine("MEETING ZOOM REFRESH CASE " + label + " PASS"); }
        Check("new-profile-fingerprint-same-scopes-public-refresh-still-blocked", reg.Fingerprint != Registration(false).Fingerprint && reg.Flow!.RequestedScopes.SequenceEqual(Registration(false).Flow!.RequestedScopes) && !MeetingOAuthAttemptStore.SupportsRefresh(Registration(false)));
        var source = await Source(); Check("code-exchange-uses-basic-PKCE-after-durable-admission", source.Permit is not null);
        using var firstHandler = new RefreshTransport(async request => {
            await CheckRequest(request, "refresh_token"); await using var db = Open();
            Check("refresh-request-only-after-durable-start-and-audit", await db.MeetingOAuthExchanges.AnyAsync(x => x.Id == source.Permit!.Id && x.Status == "RefreshStarted" && x.Version == 2) && await db.AuditLogs.AnyAsync(x => x.EntityId == source.Permit!.Id && x.Action == "MeetingRefreshStarted"));
            return Response(Body()); });
        using var firstTransport = new HttpMessageInvoker(firstHandler); var result = await Run(s => new MeetingProviderRefresh(s, firstTransport, clock).ExecuteAsync(actor, academy, reg, source.Permit!, 1, secret));
        Check("rotated-credential-quarantined-new-fence-no-connected-or-secret-result", result.StatusCode == 200 && result.Permit is not null && result.Permit.Fence != source.Permit!.Fence && !result.Connected && !result.CanRetry && !JsonSerializer.Serialize(result).Contains(access) && firstHandler.Calls == 1);
        await using (var db = Open()) Check("encrypted-v3-and-verification-required-binding", await db.MeetingOAuthExchanges.AnyAsync(x => x.Id == source.Permit!.Id && x.Status == "TokensQuarantined" && x.Version == 3 && !x.ProtectedEnvelope.Contains(access) && !x.ProtectedEnvelope.Contains(nextRefresh)) && await db.MeetingProviderBindings.AnyAsync(x => x.AcademyId == academy && x.Provider == reg.Provider && x.Version == source.Binding + 1 && x.Status == "VerificationRequired"));
        var read = await Run(s => s.ReadQuarantinedTokensAsync(actor, academy, reg, result.Permit!)); Check("private-read-uses-only-latest-refresh-token", read.StatusCode == 200 && read.Tokens!.RefreshToken == nextRefresh);
        Check("old-permit-review-and-refresh-version-fenced", (await Run(s => s.ReadQuarantinedTokensAsync(actor, academy, reg, source.Permit!))).StatusCode == 409 && (await Run(s => s.ReadAccountReviewAsync(actor, academy, reg, source.Review.Id))).StatusCode == 409 && (await Run(s => s.BeginRefreshAsync(actor, academy, reg, source.Permit!, 1))).StatusCode == 409);
        var renewed = (await Run(s => s.PrepareAccountReviewAsync(actor, academy, reg, result.Permit!, new(subject, account, clock.Now)))).Review!;
        var policies = MeetingAccountPolicyRegistry.Create([new(academy, reg.Provider, 1, reg.Fingerprint, subject, account, null, clock.Now.AddMinutes(-1), clock.Now.AddHours(3))]);
        using var accountHandler = new ZoomAccountHandler(JsonSerializer.Serialize(new { id = subject, account_id = account, status = "active", type = 2 }), access, null, HttpStatusCode.OK); using var accountTransport = new HttpMessageInvoker(accountHandler);
        Check("new-unconfirmed-review-cannot-inherit-plan-evidence", renewed.Status == "AwaitingConfirmation" && (await Run(s => s.CheckZoomAccountAsync(actor, academy, reg, renewed.Id, policies, new(accountTransport)))).StatusCode == 409 && accountHandler.Calls == 0);
        if ((await Run(s => s.ConfirmAccountReviewAsync(actor, academy, reg, renewed.Id, 0, renewed.ReviewHash, true))).StatusCode != 200) throw new InvalidOperationException("Zoom refreshed confirmation fixture failed");
        Check("fresh-confirmation-and-plan-read-still-unavailable", (await Run(s => s.CheckZoomAccountAsync(actor, academy, reg, renewed.Id, policies, new(accountTransport)))) is { StatusCode: 503, MeetingPlanObserved: true, CanExecute: false } && accountHandler.Calls == 1);
        using var secondHandler = new RefreshTransport(async request => { await CheckRequest(request, "refresh_token", nextRefresh); return Response(Body("synthetic-zoom-third-refresh")); }); using var secondTransport = new HttpMessageInvoker(secondHandler);
        var second = await Run(s => new MeetingProviderRefresh(s, secondTransport, clock).ExecuteAsync(actor, academy, reg, result.Permit!, 3, secret));
        await using (var db = Open()) Check("second-cycle-v4-v5-latest-token-only-history-retained", second.StatusCode == 200 && secondHandler.Calls == 1 && await db.MeetingOAuthExchanges.AnyAsync(x => x.Id == source.Permit!.Id && x.Version == 5) && await db.MeetingAccountReviews.CountAsync(x => x.ExchangeId == source.Permit!.Id) == 2);
        var race = await Source(); var barrier = new ClaimBarrier(); var starts = await Task.WhenAll(Run(s => s.BeginRefreshAsync(actor, academy, reg, race.Permit, 1), barrier), Run(s => s.BeginRefreshAsync(actor, academy, reg, race.Permit, 1), barrier));
        Check("simultaneous-starts-exactly-201-409", starts.Select(x => x.StatusCode).Order().SequenceEqual(new[] { 201, 409 }));
        var material = starts.Single(x => x.Material is not null).Material!;
        await using (var db = Open()) Check("one-start-row-version-and-audit", await db.MeetingOAuthExchanges.AnyAsync(x => x.Id == race.Permit.Id && x.Version == 2) && await db.AuditLogs.CountAsync(x => x.EntityId == race.Permit.Id && x.Action == "MeetingRefreshStarted") == 1);
        var finishBarrier = new ClaimBarrier(); var finishes = await Task.WhenAll(Run(s => s.FinishRefreshAsync(actor, academy, reg, material, Tokens()), finishBarrier), Run(s => s.FinishRefreshAsync(actor, academy, reg, material, Tokens()), finishBarrier));
        Check("simultaneous-finishes-exactly-200-409", finishes.Select(x => x.StatusCode).Order().SequenceEqual(new[] { 200, 409 }));
        await using (var db = Open()) Check("one-completion-v3-and-audit", await db.MeetingOAuthExchanges.AnyAsync(x => x.Id == race.Permit.Id && x.Version == 3) && await db.AuditLogs.CountAsync(x => x.EntityId == race.Permit.Id && x.Action == "MeetingRefreshTokensQuarantined") == 1);
        var fault = await Source(); var failed = false; var calls = 0;
        using (var transport = new HttpMessageInvoker(new RefreshTransport(_ => { calls++; return Task.FromResult(Response(Body())); })))
        { try { await Run(s => new MeetingProviderRefresh(s, transport, clock).ExecuteAsync(actor, academy, reg, fault.Permit, 1, secret), new OperationCommitFault()); } catch (InvalidOperationException ex) when (ex.Message == "Synthetic operation commit failure after SQL writes") { failed = true; } }
        await using (var db = Open()) Check("start-audit-fault-no-bearer-send-and-complete-rollback", failed && calls == 0 && await db.MeetingOAuthExchanges.AnyAsync(x => x.Id == fault.Permit.Id && x.Version == 1 && x.Status == "TokensQuarantined") && !await db.AuditLogs.AnyAsync(x => x.EntityId == fault.Permit.Id && x.Action == "MeetingRefreshStarted"));
        var admitted = await Run(s => s.BeginRefreshAsync(actor, academy, reg, fault.Permit, 1)); Check("start-after-proved-rollback", admitted.StatusCode == 201); failed = false;
        try { await Run(s => s.FinishRefreshAsync(actor, academy, reg, admitted.Material!, Tokens()), new OperationCommitFault()); } catch (InvalidOperationException ex) when (ex.Message == "Synthetic operation commit failure after SQL writes") { failed = true; }
        await using (var db = Open()) Check("completion-audit-fault-retains-started-no-false-commit", failed && await db.MeetingOAuthExchanges.AnyAsync(x => x.Id == fault.Permit.Id && x.Version == 2 && x.Status == "RefreshStarted") && !await db.AuditLogs.AnyAsync(x => x.EntityId == fault.Permit.Id && x.Action == "MeetingRefreshTokensQuarantined"));
        var oldTime = clock.Now; clock.Now = clock.Now.AddSeconds(30); Check("interrupted-rotation-recovers-unknown-not-retryable", await Run(s => s.RecoverRefreshAsync(academy, fault.Permit.Id)) && (await Run(s => s.BeginRefreshAsync(actor, academy, reg, new(fault.Permit.Id, admitted.Material!.Fence), 3))).StatusCode == 409); clock.Now = oldTime;
        Check("unknown-rotation-local-containment-manual-family-followup", (await Run(s => s.DisconnectUnknownRefreshAsync(actor, academy, reg, new(fault.Permit.Id, admitted.Material!.Fence), 3, fault.Binding + 1, true))).StatusCode == 202);
        foreach (var invalid in new[] { "same-refresh", "missing-refresh", "scope-expanded" })
        {
            var bad = await Source(); var started = await Run(s => s.BeginRefreshAsync(actor, academy, reg, bad.Permit, 1));
            var outcome = invalid == "missing-refresh" ? new MeetingOAuthExchangeOutcome(200, "TokensReceivedAccountUnverified", Tokens: new(access, "", clock.Now.AddHours(1), reg.Flow!.RequiredTokenScopes)) : invalid == "scope-expanded" ? Tokens() with { Tokens = new(access, nextRefresh, clock.Now.AddHours(1), reg.Flow!.RequiredTokenScopes.Concat(["user:read:user:admin"]).ToArray()) } : Tokens(oldRefresh);
            var rejected = await Run(s => s.FinishRefreshAsync(actor, academy, reg, started.Material!, outcome));
            await using var db = Open(); Check(invalid + "-cannot-bypass-store-rotation-guard", rejected.StatusCode == 409 && rejected.Permit is null && await db.MeetingOAuthExchanges.AnyAsync(x => x.Id == bad.Permit.Id && x.Status == "RefreshUnknown"));
        }
        var denied = await Source();
        Check("foreign-academy-cannot-start-refresh", (await Run(s => s.BeginRefreshAsync(actor, Guid.NewGuid(), reg, denied.Permit, 1))).StatusCode == 403);
        Check("other-current-admin-cannot-start-refresh", (await Run(s => s.BeginRefreshAsync(Actor(otherId), academy, reg, denied.Permit, 1))).StatusCode == 409);
        Check("anonymous-cannot-start-refresh", (await Run(s => s.BeginRefreshAsync(new ClaimsPrincipal(), academy, reg, denied.Permit, 1))).StatusCode == 401);
        Check("old-public-profile-cannot-refresh-or-reuse-backend-permit", (await Run(s => s.BeginRefreshAsync(actor, academy, Registration(false), denied.Permit, 1))).StatusCode == 503);
        using (var transport = new HttpMessageInvoker(new RefreshTransport(_ => throw new InvalidOperationException("Missing secret must not send")))) Check("missing-server-secret-no-admission-or-send", (await Run(s => new MeetingProviderRefresh(s, transport, clock).ExecuteAsync(actor, academy, reg, denied.Permit, 1, null))).StatusCode == 503);
        var uncertain = await Source(); using var network = new RefreshTransport(_ => throw new HttpRequestException("synthetic-private-refresh-error")); using var networkTransport = new HttpMessageInvoker(network);
        var unknown = await Run(s => new MeetingProviderRefresh(s, networkTransport, clock).ExecuteAsync(actor, academy, reg, uncertain.Permit, 1, secret));
        Check("network-uncertainty-no-blind-resend", unknown.StatusCode == 409 && unknown.Permit is null && !unknown.CanRetry && (await Run(s => new MeetingProviderRefresh(s, networkTransport, clock).ExecuteAsync(actor, academy, reg, uncertain.Permit, 1, secret))).StatusCode == 409 && network.Calls == 1);
        var disconnected = await Source(); using var disconnectHandler = new RefreshTransport(async request => { await CheckRequest(request, "refresh_token"); if ((await Run(s => s.DisconnectLocallyAsync(actor, academy, reg.Provider, disconnected.Binding + 1, true))).StatusCode != 200) throw new InvalidOperationException("Zoom refresh disconnect fixture failed"); return Response(Body()); }); using var disconnectTransport = new HttpMessageInvoker(disconnectHandler);
        Check("disconnect-during-token-response-prevents-commit", (await Run(s => new MeetingProviderRefresh(s, disconnectTransport, clock).ExecuteAsync(actor, academy, reg, disconnected.Permit, 1, secret))).StatusCode == 409);
        var final = await Source(); var finalStart = await Run(s => s.BeginRefreshAsync(actor, academy, reg, final.Permit, 1)); var finalBarrier = new ZoomRefreshFinalBarrier();
        var committing = Run(s => s.FinishRefreshAsync(actor, academy, reg, finalStart.Material!, Tokens()), finalBarrier); var dropping = Run(s => s.DisconnectLocallyAsync(actor, academy, reg.Provider, final.Binding + 1, true), finalBarrier); await Task.WhenAll(committing, dropping);
        await using (var db = Open()) Check("final-token-commit-disconnect-race-never-connects", (await dropping).StatusCode == 200 && (await committing).StatusCode is 200 or 409 && !(await committing).Connected && await db.MeetingProviderBindings.AnyAsync(x => x.AcademyId == academy && x.Status == "Disconnected" && x.Version == final.Binding + 2));
        var expiry = await Source(); oldTime = clock.Now; using var expiryHandler = new RefreshTransport(_ => { clock.Now = clock.Now.AddSeconds(30); return Task.FromResult(Response(Body())); }); using var expiryTransport = new HttpMessageInvoker(expiryHandler);
        Check("expired-send-deadline-cannot-commit", (await Run(s => new MeetingProviderRefresh(s, expiryTransport, clock).ExecuteAsync(actor, academy, reg, expiry.Permit, 1, secret))).StatusCode == 409); clock.Now = oldTime;
        var stamp = await Source(); using var stampHandler = new RefreshTransport(async _ => { using var scope = factory.Services.CreateScope(); var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(); if (!(await users.UpdateSecurityStampAsync((await users.FindByIdAsync(actorId.ToString()))!)).Succeeded) throw new InvalidOperationException("Zoom refresh stamp fixture failed"); return Response(Body()); }); using var stampTransport = new HttpMessageInvoker(stampHandler);
        Check("stamp-revocation-during-response-prevents-commit", (await Run(s => new MeetingProviderRefresh(s, stampTransport, clock).ExecuteAsync(actor, academy, reg, stamp.Permit, 1, secret))).StatusCode == 409);
        var role = await Source(); using var roleHandler = new RefreshTransport(async _ => { using var scope = factory.Services.CreateScope(); var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(); if (!(await users.RemoveFromRoleAsync((await users.FindByIdAsync(actorId.ToString()))!, "AcademyAdmin")).Succeeded) throw new InvalidOperationException("Zoom refresh role fixture failed"); return Response(Body()); }); using var roleTransport = new HttpMessageInvoker(roleHandler);
        Check("role-revocation-during-response-prevents-commit", (await Run(s => new MeetingProviderRefresh(s, roleTransport, clock).ExecuteAsync(actor, academy, reg, role.Permit, 1, secret))).StatusCode == 403);
        if (checks.Count != 33) throw new InvalidOperationException("Zoom refresh case count mismatch " + checks.Count);
        var sources = new[] { "apps/api/Infrastructure/Meetings/MeetingOAuthFlowProfile.cs", "apps/api/Infrastructure/Meetings/MeetingOAuthCodeExchange.cs", "apps/api/Infrastructure/Meetings/MeetingProviderRefresh.cs", "apps/api/Infrastructure/Meetings/MeetingRefreshStore.cs", "apps/api/Infrastructure/Meetings/MeetingZoomAccountStore.cs", "tests/AcademyDesk.Api.Tests/MeetingZoomRefreshTests.cs", "QA/tools/SqlHarness/MeetingZoomRefreshRegression.cs" }.ToDictionary(p => p, p => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(p))));
        var path = Path.Combine("QA", "EVIDENCE", "meeting-zoom-refresh-sql-" + manifest.RunId.ToString("N")); Directory.CreateDirectory(path);
        await File.WriteAllTextAsync(Path.Combine(path, "result.json"), JsonSerializer.Serialize(new { checks, sources, scope = "Owned SQL/Identity, synthetic confidential Zoom PKCE exchange/rotation only; old public profile remains unqualified, no live provider/registration/Connected/write/browser/release acceptance" }, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine("MEETING ZOOM REFRESH PASS:33 SQL/Identity cases; inactive confidential candidate only, public refresh unqualified.");
    }
}
