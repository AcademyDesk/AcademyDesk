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
    private const string MicrosoftIdentityAudit = "MeetingMicrosoftIdentityVerifiedEntitlementRequired";
    private sealed record MicrosoftIdentitySource(MeetingOAuthRegistration Registration, MeetingAccountPolicyRegistry Policies, MeetingAccountReviewState Review, MeetingOAuthExchangePermit Permit, int Binding);
    private sealed class MicrosoftIdentityHandler(string keyUrl, string keyJson, string access, string objectId, Func<Task>? duringKeys = null, Func<Task>? duringAccount = null) : HttpMessageHandler
    {
        internal int KeyCalls, AccountCalls;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            if (request.Method != HttpMethod.Get || request.RequestUri!.Query.Length != 0) throw new InvalidOperationException("Unsafe Microsoft identity request");
            if (request.RequestUri.AbsoluteUri == keyUrl)
            { KeyCalls++; if (request.Headers.Authorization is not null) throw new InvalidOperationException("Credential on public keys request"); if (duringKeys is not null) await duringKeys(); return new(HttpStatusCode.OK) { Content = new StringContent(keyJson, Encoding.UTF8, "application/json") }; }
            if (request.RequestUri.AbsoluteUri != "https://graph.microsoft.com/v1.0/me" || request.Headers.Authorization?.Scheme != "Bearer" || request.Headers.Authorization.Parameter != access) throw new InvalidOperationException("Unsafe Microsoft account request");
            AccountCalls++; if (duringAccount is not null) await duringAccount();
            return new(HttpStatusCode.OK) { Content = new StringContent(JsonSerializer.Serialize(new { id = objectId }), Encoding.UTF8, "application/json") };
        }
    }
    private sealed class MicrosoftIdentityAuditFault : DbTransactionInterceptor
    {
        public override ValueTask<InterceptionResult> TransactionCommittingAsync(DbTransaction transaction, TransactionEventData data, InterceptionResult result, CancellationToken token = default)
        {
            if (data.Context!.ChangeTracker.Entries<AuditLog>().Any(x => x.State == EntityState.Added && x.Entity.Action == MicrosoftIdentityAudit)) throw new InvalidOperationException("Synthetic Microsoft identity audit commit failure");
            return ValueTask.FromResult(result);
        }
    }
    private sealed class MicrosoftIdentityFinalBarrier : SaveChangesInterceptor
    {
        private readonly ClaimBarrier barrier = new();
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData data, InterceptionResult<int> result, CancellationToken token = default) =>
            data.Context!.ChangeTracker.Entries<AuditLog>().Any(x => x.State == EntityState.Added && (x.Entity.Action == MicrosoftIdentityAudit || x.Entity.Action == "MeetingDisconnectedLocallyRevocationRequired"))
                ? barrier.SavingChangesAsync(data, result, token) : ValueTask.FromResult(result);
    }
    private static async Task VerifyMeetingMicrosoftIdentityAsync(QaApiFactory factory, QaRunManifest manifest)
    {
        string connection; using (var scope = factory.Services.CreateScope()) connection = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>().Database.GetConnectionString()!;
        var target = new SqlConnectionStringBuilder(connection); if (target.DataSource != manifest.SqlServer || target.InitialCatalog != manifest.Database || target.UserID != manifest.RuntimeLogin) throw new InvalidOperationException("Microsoft identity requires exact owned SQL target");
        AcademyDeskDbContext Open(IInterceptor? interceptor = null) { var b = new DbContextOptionsBuilder<AcademyDeskDbContext>().UseSqlServer(connection); if (interceptor is not null) b.AddInterceptors(interceptor); return new(b.Options); }
        var tenant = new Academy { Name = "Synthetic Microsoft Identity Academy", EnabledModulesJson = "[\"Core\",\"Engagement\"]" }; await using (var db = Open()) { db.Add(tenant); await db.SaveChangesAsync(); }
        var academy = tenant.Id; var actorId = await CreateAccessActorAsync(factory, "qa-ms-identity@example.invalid", "AcademyAdmin", academy); var otherId = await CreateAccessActorAsync(factory, "qa-ms-identity-other@example.invalid", "AcademyAdmin", academy);
        ClaimsPrincipal Actor(Guid id) => new(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, id.ToString()) }, "synthetic-ms-identity"));
        var actor = Actor(actorId); var clock = new OperationClock(); var protection = factory.Services.GetRequiredService<IDataProtectionProvider>();
        async Task<T> Run<T>(Func<MeetingOAuthAttemptStore, Task<T>> action, IInterceptor? interceptor = null)
        { using var scope = factory.Services.CreateScope(); await using var db = Open(interceptor); var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(); return await action(new(db, users, new(db, users, clock), protection, clock)); }
        const string directory = "019acc9a-6200-7000-8000-000000000001", objectId = "019acc9a-6200-7000-8000-000000000002", clientId = "019acc9a-6200-7000-8000-000000000003", access = "synthetic-opaque-ms-access";
        MeetingOAuthRegistration Registration(bool identity) => MeetingOAuthRegistrationRegistry.Create("https://api.academy.invalid", "https://app.academy.invalid", [new("Microsoft365", 1, clientId, "https://api.academy.invalid/api/meetings/oauth/Microsoft365/callback", "https://app.academy.invalid/communication-settings", identity ? MeetingOAuthFlowProfile.MicrosoftIdentityProfile : "microsoft-online-meetings-work/1", directory)]).Resolve("Microsoft365")!;
        using var rsa = RSA.Create(2048); var parameters = rsa.ExportParameters(false); string Encode(byte[] bytes) => WebEncoders.Base64UrlEncode(bytes);
        var keys = JsonSerializer.Serialize(new { keys = new[] { new { kty = "RSA", use = "sig", kid = "synthetic-ms-key", n = Encode(parameters.Modulus!), e = Encode(parameters.Exponent!), issuer = MeetingMicrosoftIdentityVerifier.IssuerTemplate } } });
        var checks = new List<string>(); void Check(string name, bool pass) { if (!pass) throw new InvalidOperationException("MEETING MICROSOFT IDENTITY " + name); checks.Add(name); Console.WriteLine("MEETING MICROSOFT IDENTITY CASE " + name + " PASS"); }
        async Task<MicrosoftIdentitySource> Source(string mode = "ok")
        {
            var reg = Registration(mode != "old-profile"); int version; await using (var db = Open()) version = await db.MeetingProviderBindings.Where(x => x.AcademyId == academy && x.Provider == reg.Provider).Select(x => (int?)x.Version).SingleOrDefaultAsync() ?? 0;
            return await Run(async store =>
            {
                var browser = Encode(RandomNumberGenerator.GetBytes(32)); var intent = await store.PrepareAsync(actor, academy, reg.Provider, version, browser, reg.Fingerprint);
                if (intent.StatusCode != 201 || (await store.CaptureAsync(academy, reg.Provider, intent.Material!.State, browser, reg.Fingerprint, "synthetic-ms-code", false)).StatusCode != 202) throw new InvalidOperationException("Microsoft identity source capture failed");
                var claim = await store.ResumeAsync(actor, academy, reg.Provider, intent.Material.State, browser, reg.Fingerprint);
                if (claim.Material?.IdentityNonce != intent.Material.IdentityNonce || !MeetingOAuthRegistrationRegistry.ValidRandom(claim.Material?.IdentityNonce)) throw new InvalidOperationException("Microsoft nonce not retained through resume");
                var permit = (await store.BeginExchangeAsync(actor, academy, reg, claim)).Permit!;
                var head = Encode(JsonSerializer.SerializeToUtf8Bytes(new { alg = "RS256", typ = "JWT", kid = "synthetic-ms-key" }));
                var payload = Encode(JsonSerializer.SerializeToUtf8Bytes(new { iss = MeetingMicrosoftIdentityVerifier.Issuer(reg), aud = clientId, tid = mode == "wrong-directory" ? objectId : directory,
                    oid = mode == "wrong-object" ? directory : objectId, sub = "pairwise-ms-subject-not-graph-id", ver = "2.0", nonce = mode == "wrong-nonce" ? Encode(RandomNumberGenerator.GetBytes(32)) : intent.Material.IdentityNonce,
                    iat = clock.Now.ToUnixTimeSeconds(), nbf = clock.Now.AddSeconds(-30).ToUnixTimeSeconds(), exp = clock.Now.AddHours(1).ToUnixTimeSeconds(), roles = new[] { "Owner", "PlatformOwner" } }));
                var data = head + "." + payload; var signature = rsa.SignData(Encoding.ASCII.GetBytes(data), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1); if (mode == "bad-signature") signature[0] ^= 1;
                var jwt = data + "." + Encode(signature);
                if ((await store.FinishExchangeAsync(actor, academy, reg, permit, new(200, "TokensReceivedAccountUnverified", Tokens: new(access, "synthetic-ms-refresh", clock.Now.AddHours(1), reg.Flow!.RequiredTokenScopes, mode is "old-profile" or "no-token" ? null : jwt)))).StatusCode != 200) throw new InvalidOperationException("Microsoft quarantine failed");
                var review = (await store.PrepareAccountReviewAsync(actor, academy, reg, permit, new(objectId, null, clock.Now))).Review;
                if (review is null || (await store.ConfirmAccountReviewAsync(actor, academy, reg, review.Id, 0, review.ReviewHash, true)).StatusCode != 200) throw new InvalidOperationException("Microsoft review failed");
                var policy = MeetingAccountPolicyRegistry.Create([new(academy, reg.Provider, 1, reg.Fingerprint, objectId, null, directory, clock.Now.AddMinutes(-1), clock.Now.AddHours(4))]);
                return new MicrosoftIdentitySource(reg, policy, review, permit, version + 2);
            });
        }
        async Task<MeetingMicrosoftIdentityOutcome> Evaluate(MicrosoftIdentitySource source, Func<Task>? duringKeys = null, Func<Task>? duringAccount = null, IInterceptor? interceptor = null, string? account = null, ClaimsPrincipal? who = null, Guid? route = null, bool noAccountExpected = false)
        {
            using var handler = new MicrosoftIdentityHandler(MeetingMicrosoftIdentityVerifier.KeysEndpoint(source.Registration), keys, access, account ?? objectId, duringKeys, duringAccount); using var transport = new HttpMessageInvoker(handler);
            var result = await Run(s => s.CheckMicrosoftIdentityAsync(who ?? actor, route ?? academy, source.Registration, source.Review.Id, source.Policies, new(transport, clock), transport), interceptor);
            if (result.ProviderVerified || result.Connected || result.CanExecute || JsonSerializer.Serialize(result).Contains(objectId)) throw new InvalidOperationException("Microsoft identity result granted permission or leaked account");
            if (noAccountExpected && handler.AccountCalls != 0) throw new InvalidOperationException("Unverified context used Graph credential"); return result;
        }
        var old = await Source("old-profile"); Check("old-profile-requires-new-identity-consent-no-account-call", (await Evaluate(old, noAccountExpected: true)).Code == "MicrosoftIdentityConsentRequired");
        var source = await Source(); var verified = await Evaluate(source);
        Check("signed-directory-object-and-current-graph-account-still-503", verified is { StatusCode: 503, IdentityVerified: true, DirectoryAccountMatched: true });
        await using (var db = Open())
        {
            var audit = await db.AuditLogs.SingleAsync(x => x.EntityId == source.Review.Id && x.Action == MicrosoftIdentityAudit);
            Check("private-audit-only-fingerprints-no-directory-object-access", !audit.MetadataJson!.Contains(directory) && !audit.MetadataJson.Contains(objectId) && !audit.MetadataJson.Contains(access));
            Check("identity-does-not-enable-or-change-binding-source-version", await db.MeetingProviderBindings.AnyAsync(x => x.AcademyId == academy && x.Status == "VerificationRequired" && x.Version == source.Binding) && await db.MeetingOAuthExchanges.AnyAsync(x => x.Id == source.Permit.Id && x.Version == 1 && x.Status == "TokensQuarantined"));
            Check("graph-account-is-current-and-credential-read-audited-twice", await db.AuditLogs.CountAsync(x => x.EntityId == source.Permit.Id && x.Action == "MeetingOAuthQuarantinedTokensRead") == 2);
        }
        foreach (var mode in new[] { "wrong-directory", "wrong-object", "wrong-nonce", "bad-signature" })
        { var bad = await Source(mode); var denied = await Evaluate(bad, noAccountExpected: true); Check(mode + "-no-identity-no-graph-call", denied.StatusCode == 502 && !denied.IdentityVerified); }
        var wrongAccount = await Source(); Check("signed-object-does-not-match-different-graph-account", (await Evaluate(wrongAccount, account: directory)) is { StatusCode: 409, IdentityVerified: false });
        var missing = await Source("no-token"); Check("missing-id-token-no-graph-call", (await Evaluate(missing, noAccountExpected: true)).Code == "FreshMicrosoftIdentityEvidenceRequired");
        var current = await Source();
        Check("foreign-academy-denied-no-graph", (await Evaluate(current, route: Guid.NewGuid(), noAccountExpected: true)).StatusCode == 403);
        Check("other-current-admin-denied-no-graph", (await Evaluate(current, who: Actor(otherId), noAccountExpected: true)).StatusCode == 404);
        Check("anonymous-denied-no-graph", (await Evaluate(current, who: new ClaimsPrincipal(), noAccountExpected: true)).StatusCode == 401);
        var failed = false; try { await Evaluate(current, interceptor: new MicrosoftIdentityAuditFault()); } catch (InvalidOperationException ex) when (ex.Message == "Synthetic Microsoft identity audit commit failure") { failed = true; }
        await using (var db = Open()) Check("post-write-final-audit-fault-rolled-back", failed && !await db.AuditLogs.AnyAsync(x => x.EntityId == current.Review.Id && x.Action == MicrosoftIdentityAudit) && await db.MeetingOAuthExchanges.AnyAsync(x => x.Id == current.Permit.Id && x.Version == 1));
        Check("read-after-proved-rollback-remains-non-executable", (await Evaluate(current)) is { StatusCode: 503, IdentityVerified: true, CanExecute: false });
        var refresh = await Source(); MeetingRefreshMaterial? material = null;
        var refreshResult = await Evaluate(refresh, duringAccount: async () => { var start = await Run(s => s.BeginRefreshAsync(actor, academy, refresh.Registration, refresh.Permit, 1)); if (start.StatusCode != 201) throw new InvalidOperationException("Microsoft refresh source failed"); material = start.Material; });
        Check("refresh-during-graph-response-denies-identity", refreshResult.StatusCode == 409 && !refreshResult.IdentityVerified);
        var finished = await Run(s => s.FinishRefreshAsync(actor, academy, refresh.Registration, material!, new(200, "TokensReceivedAccountUnverified", Tokens: new("synthetic-rotated-ms-access", "synthetic-rotated-ms-refresh", clock.Now.AddHours(1), refresh.Registration.Flow!.RequiredTokenScopes))));
        if (finished.StatusCode != 200 || finished.Permit is null) throw new InvalidOperationException("Microsoft refresh fixture completion failed");
        var renewed = await Run(s => s.PrepareAccountReviewAsync(actor, academy, refresh.Registration, finished.Permit, new(objectId, null, clock.Now)));
        if (renewed.StatusCode != 201 || renewed.Review is null || (await Run(s => s.ConfirmAccountReviewAsync(actor, academy, refresh.Registration, renewed.Review.Id, 0, renewed.Review.ReviewHash, true))).StatusCode != 200) throw new InvalidOperationException("Microsoft renewed review fixture failed");
        Check("refreshed-credential-new-confirmation-cannot-inherit-old-identity", (await Evaluate(refresh with { Permit = finished.Permit, Review = renewed.Review }, noAccountExpected: true)).Code == "FreshMicrosoftIdentityEvidenceRequired");
        var disconnect = await Source(); var dropped = await Evaluate(disconnect, duringAccount: async () => { if ((await Run(s => s.DisconnectLocallyAsync(actor, academy, disconnect.Registration.Provider, disconnect.Binding, true))).StatusCode != 200) throw new InvalidOperationException("Microsoft disconnect fixture failed"); });
        Check("disconnect-during-graph-response-denies-identity", dropped.StatusCode == 409 && !dropped.IdentityVerified);
        var race = await Source(); var barrier = new MicrosoftIdentityFinalBarrier(); var checking = Evaluate(race, interceptor: barrier); var dropping = Run(s => s.DisconnectLocallyAsync(actor, academy, race.Registration.Provider, race.Binding, true), barrier);
        await Task.WhenAll(checking, dropping);
        await using (var db = Open()) Check("final-audit-disconnect-race-never-enables-connection", (await dropping).StatusCode == 200 && (await checking).StatusCode is 409 or 503 && !(await checking).CanExecute && await db.MeetingProviderBindings.AnyAsync(x => x.AcademyId == academy && x.Status == "Disconnected" && x.Version == race.Binding + 1));
        var keysRole = await Source();
        async Task SetRole(bool present) { using var scope = factory.Services.CreateScope(); var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(); var user = (await users.FindByIdAsync(actorId.ToString()))!; var changed = present ? await users.AddToRoleAsync(user, "AcademyAdmin") : await users.RemoveFromRoleAsync(user, "AcademyAdmin"); if (!changed.Succeeded) throw new InvalidOperationException("Microsoft role fixture failed"); }
        var keysDenied = await Evaluate(keysRole, duringKeys: () => SetRole(false), noAccountExpected: true); Check("model-token-owner-roles-cannot-bypass-role-revocation-before-graph", keysDenied.StatusCode == 403 && !keysDenied.IdentityVerified); await SetRole(true);
        var expiry = await Source(); var priorTime = clock.Now; var expired = await Evaluate(expiry, duringAccount: () => { clock.Now = clock.Now.AddHours(2); return Task.CompletedTask; });
        Check("expiry-during-graph-response-denies-identity", !expired.IdentityVerified && expired.StatusCode == 409); clock.Now = priorTime;
        var stamp = await Source(); var stampResult = await Evaluate(stamp, duringAccount: async () => { using var scope = factory.Services.CreateScope(); var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(); if (!(await users.UpdateSecurityStampAsync((await users.FindByIdAsync(actorId.ToString()))!)).Succeeded) throw new InvalidOperationException("Microsoft stamp fixture failed"); });
        Check("stamp-changed-during-graph-response-denies-identity", stampResult.StatusCode == 409 && !stampResult.IdentityVerified);
        var role = await Source(); var roleResult = await Evaluate(role, duringAccount: () => SetRole(false)); Check("role-revoked-during-graph-response-denies-identity", roleResult.StatusCode == 403 && !roleResult.IdentityVerified);
        Check("new-profile-and-policy-cannot-reuse-old-registration", old.Registration.Fingerprint != source.Registration.Fingerprint && source.Policies.Resolve(academy, old.Registration, clock.Now) is null);
        if (checks.Count != 25) throw new InvalidOperationException("Microsoft identity case count mismatch " + checks.Count);
        var sources = new[] { "apps/api/Infrastructure/Meetings/MeetingMicrosoftIdentityVerifier.cs", "apps/api/Infrastructure/Meetings/MeetingMicrosoftIdentityStore.cs", "apps/api/Infrastructure/Meetings/MeetingOAuthFlowProfile.cs", "apps/api/Infrastructure/Meetings/MeetingOAuthRegistrationRegistry.cs", "apps/api/Infrastructure/Meetings/MeetingOAuthAttemptStore.cs", "apps/api/Infrastructure/Meetings/MeetingOAuthCodeExchange.cs", "apps/api/Infrastructure/Meetings/MeetingOAuthExchangeStore.cs", "apps/api/Infrastructure/Meetings/MeetingRefreshStore.cs", "tests/AcademyDesk.Api.Tests/MeetingMicrosoftIdentityTests.cs", "QA/tools/SqlHarness/MeetingMicrosoftIdentityRegression.cs" }.ToDictionary(p => p, p => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(p))));
        var path = Path.Combine("QA", "EVIDENCE", "meeting-microsoft-identity-sql-" + manifest.RunId.ToString("N")); Directory.CreateDirectory(path);
        await File.WriteAllTextAsync(Path.Combine(path, "result.json"), JsonSerializer.Serialize(new { checks, sources, scope = "Owned SQL/Identity with synthetic RSA ID tokens and fixed fake Microsoft keys/Graph account transport; not live tenant, Teams consent/license/entitlement, Connected, browser or release acceptance" }, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine("MEETING MICROSOFT IDENTITY PASS:25 SQL/Identity cases; directory/object match is not Teams entitlement.");
    }
}
