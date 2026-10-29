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
    private sealed class GoogleKeysHandler(string keys, Func<Task>? during = null) : HttpMessageHandler
    {
        internal int Calls;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            Calls++; if (request.RequestUri!.AbsoluteUri != MeetingGoogleIdentityVerifier.KeysEndpoint || request.Headers.Authorization is not null) throw new InvalidOperationException("Unsafe identity key request");
            if (during is not null) await during();
            return new(HttpStatusCode.OK) { Content = new StringContent(keys, Encoding.UTF8, "application/json") };
        }
    }
    private sealed class GoogleIdentityAuditCommitFault : DbTransactionInterceptor
    {
        public override ValueTask<InterceptionResult> TransactionCommittingAsync(DbTransaction transaction, TransactionEventData data, InterceptionResult result, CancellationToken token = default)
        {
            if (data.Context!.ChangeTracker.Entries<AuditLog>().Any(x => x.State == EntityState.Added && x.Entity.Action == "MeetingGoogleIdentityVerifiedEntitlementRequired"))
                throw new InvalidOperationException("Synthetic Google identity audit commit failure");
            return ValueTask.FromResult(result);
        }
    }
    private sealed class GoogleIdentityFinalBarrier : SaveChangesInterceptor
    {
        private readonly ClaimBarrier barrier = new();
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData data, InterceptionResult<int> result, CancellationToken token = default) =>
            data.Context!.ChangeTracker.Entries<AuditLog>().Any(x => x.State == EntityState.Added && x.Entity.Action is "MeetingGoogleIdentityVerifiedEntitlementRequired" or "MeetingDisconnectedLocallyRevocationRequired")
                ? barrier.SavingChangesAsync(data, result, token) : ValueTask.FromResult(result);
    }
    private static async Task VerifyMeetingGoogleIdentityAsync(QaApiFactory factory, QaRunManifest manifest)
    {
        string connection; using (var scope = factory.Services.CreateScope()) connection = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>().Database.GetConnectionString()!;
        var target = new SqlConnectionStringBuilder(connection);
        if (target.DataSource != manifest.SqlServer || target.InitialCatalog != manifest.Database || target.UserID != manifest.RuntimeLogin) throw new InvalidOperationException("Google identity requires exact owned target");
        AcademyDeskDbContext Open(IInterceptor? interceptor = null) { var b = new DbContextOptionsBuilder<AcademyDeskDbContext>().UseSqlServer(connection); if (interceptor is not null) b.AddInterceptors(interceptor); return new(b.Options); }
        var tenant = new Academy { Name = "Synthetic Google Identity Academy", EnabledModulesJson = "[\"Core\",\"Engagement\"]" };
        await using (var db = Open()) { db.Add(tenant); await db.SaveChangesAsync(); }
        var academy = tenant.Id; var actorId = await CreateAccessActorAsync(factory, "qa-google-identity@example.invalid", "AcademyAdmin", academy);
        var otherId = await CreateAccessActorAsync(factory, "qa-google-identity-other@example.invalid", "AcademyAdmin", academy);
        ClaimsPrincipal Actor(Guid id) => new(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, id.ToString()) }, "synthetic-identity"));
        var actor = Actor(actorId); var clock = new OperationClock(); var protection = factory.Services.GetRequiredService<IDataProtectionProvider>();
        async Task<T> Run<T>(Func<MeetingOAuthAttemptStore, Task<T>> action, IInterceptor? interceptor = null)
        { using var scope = factory.Services.CreateScope(); await using var db = Open(interceptor); var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(); return await action(new(db, users, new(db, users, clock), protection, clock)); }
        var reg = MeetingOAuthRegistrationRegistry.Create("https://api.academy.invalid", "https://app.academy.invalid", [new("GoogleWorkspace", 1, "synthetic-client", "https://api.academy.invalid/api/meetings/oauth/GoogleWorkspace/callback", "https://app.academy.invalid/communication-settings", "google-calendar-owned-web/1")]).Resolve("GoogleWorkspace")!;
        const string subject = "synthetic-google-identity", access = "synthetic-identity-access";
        var policies = MeetingAccountPolicyRegistry.Create([new(academy, reg.Provider, 1, reg.Fingerprint, subject, null, null, clock.Now.AddMinutes(-1), clock.Now.AddHours(4))]);
        using var rsa = RSA.Create(2048); var parameters = rsa.ExportParameters(false);
        string Encode(byte[] bytes) => WebEncoders.Base64UrlEncode(bytes);
        var keyJson = JsonSerializer.Serialize(new { keys = new[] { new { kty = "RSA", alg = "RS256", use = "sig", kid = "synthetic-key", n = Encode(parameters.Modulus!), e = Encode(parameters.Exponent!) } } });
        var checks = new List<string>(); void Check(string name, bool pass) { if (!pass) throw new InvalidOperationException("MEETING GOOGLE IDENTITY " + name); checks.Add(name); Console.WriteLine("MEETING GOOGLE IDENTITY CASE " + name + " PASS"); }
        async Task<(MeetingAccountReviewState Review, MeetingOAuthExchangePermit Permit, int Binding, string Jwt, string Nonce)> Source(string mode = "ok")
        {
            int version; await using (var db = Open()) version = await db.MeetingProviderBindings.Where(x => x.AcademyId == academy && x.Provider == reg.Provider).Select(x => (int?)x.Version).SingleOrDefaultAsync() ?? 0;
            return await Run(async store =>
            {
                var browser = Encode(RandomNumberGenerator.GetBytes(32)); var intent = await store.PrepareAsync(actor, academy, reg.Provider, version, browser, reg.Fingerprint);
                if (intent.StatusCode != 201) throw new InvalidOperationException("Identity intent failed");
                var nonce = intent.Material!.IdentityNonce!;
                if ((await store.CaptureAsync(academy, reg.Provider, intent.Material.State, browser, reg.Fingerprint, "synthetic-identity-code", false)).StatusCode != 202) throw new InvalidOperationException("Identity capture failed");
                var claim = await store.ResumeAsync(actor, academy, reg.Provider, intent.Material.State, browser, reg.Fingerprint);
                if (claim.Material?.IdentityNonce != nonce) throw new InvalidOperationException("Nonce changed through authenticated resume");
                var permit = (await store.BeginExchangeAsync(actor, academy, reg, claim)).Permit!;
                var head = Encode(JsonSerializer.SerializeToUtf8Bytes(new { alg = "RS256", kid = "synthetic-key", typ = "JWT" }));
                var payload = Encode(JsonSerializer.SerializeToUtf8Bytes(new { iss = "https://accounts.google.com", aud = "synthetic-client", sub = subject, iat = clock.Now.ToUnixTimeSeconds(), exp = clock.Now.AddHours(1).ToUnixTimeSeconds(),
                    nonce = mode == "wrong-nonce" ? Encode(RandomNumberGenerator.GetBytes(32)) : nonce, at_hash = Encode(SHA256.HashData(Encoding.ASCII.GetBytes(access)).AsSpan(0, 16).ToArray()) }));
                var data = head + "." + payload; var signature = rsa.SignData(Encoding.ASCII.GetBytes(data), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                if (mode == "bad-signature") signature[0] ^= 1;
                var jwt = data + "." + Encode(signature);
                if ((await store.FinishExchangeAsync(actor, academy, reg, permit, new(200, "TokensReceivedAccountUnverified", Tokens: new(access, "synthetic-identity-refresh", clock.Now.AddHours(1), reg.Flow!.RequiredTokenScopes, mode == "no-token" ? null : jwt)))).StatusCode != 200) throw new InvalidOperationException("Identity quarantine failed");
                var review = (await store.PrepareAccountReviewAsync(actor, academy, reg, permit, new(subject, null, clock.Now))).Review!;
                if ((await store.ConfirmAccountReviewAsync(actor, academy, reg, review.Id, 0, review.ReviewHash, true)).StatusCode != 200) throw new InvalidOperationException("Identity review failed");
                return (review, permit, version + 2, jwt, nonce);
            });
        }
        async Task<MeetingGoogleIdentityOutcome> Evaluate(Guid review, Func<Task>? during = null, IInterceptor? interceptor = null, ClaimsPrincipal? who = null, Guid? route = null, string? keys = null)
        { using var handler = new GoogleKeysHandler(keys ?? keyJson, during); using var transport = new HttpMessageInvoker(handler); return await Run(s => s.CheckGoogleIdentityAsync(who ?? actor, route ?? academy, reg, review, policies, new(transport, clock)), interceptor); }
        var source = await Source(); var result = await Evaluate(source.Review.Id);
        Check("signed-identity-only-still-503-not-connected-not-executable", result.StatusCode == 503 && result.IdentityVerified && !result.ProviderVerified && !result.Connected && !result.CanExecute);
        await using (var db = Open())
        {
            var audit = await db.AuditLogs.SingleAsync(x => x.EntityId == source.Review.Id && x.Action == "MeetingGoogleIdentityVerifiedEntitlementRequired"); var exchange = await db.MeetingOAuthExchanges.SingleAsync(x => x.Id == source.Permit.Id);
            Check("audit-only-fingerprints-no-identity-token-nonce-access", !audit.MetadataJson!.Contains(subject) && !audit.MetadataJson.Contains(source.Jwt) && !audit.MetadataJson.Contains(source.Nonce) && !audit.MetadataJson.Contains(access));
            Check("encrypted-quarantine-and-unchanged-verification-required-binding", !exchange.ProtectedEnvelope.Contains(source.Jwt) && exchange.Version == 1 && await db.MeetingProviderBindings.AnyAsync(x => x.AcademyId == academy && x.Status == "VerificationRequired" && x.Version == source.Binding));
        }
        foreach (var mode in new[] { "wrong-nonce", "bad-signature", "no-token" })
        {
            var bad = await Source(mode); var denied = await Evaluate(bad.Review.Id);
            await using var db = Open(); Check(mode + "-no-identity-proof-audit", !denied.IdentityVerified && !denied.CanExecute && !await db.AuditLogs.AnyAsync(x => x.EntityId == bad.Review.Id && x.Action == "MeetingGoogleIdentityVerifiedEntitlementRequired"));
        }
        var current = await Source();
        Check("new-attempt-has-distinct-host-nonce", current.Nonce != source.Nonce);
        Check("foreign-academy-denied", (await Evaluate(current.Review.Id, route: Guid.NewGuid())).StatusCode == 403);
        Check("other-admin-denied", (await Evaluate(current.Review.Id, who: Actor(otherId))).StatusCode == 404);
        Check("anonymous-denied", (await Evaluate(current.Review.Id, who: new ClaimsPrincipal())).StatusCode == 401);
        Check("invalid-public-keys-no-proof", !(await Evaluate(current.Review.Id, keys: "{\"keys\":[]}")).IdentityVerified);
        Check("superseded-source-denied", !(await Evaluate(source.Review.Id)).IdentityVerified);
        var failed = false;
        try { await Evaluate(current.Review.Id, interceptor: new GoogleIdentityAuditCommitFault()); } catch (InvalidOperationException ex) when (ex.Message == "Synthetic Google identity audit commit failure") { failed = true; }
        await using (var db = Open()) Check("required-final-audit-post-write-commit-fault-rolled-back", failed && !await db.AuditLogs.AnyAsync(x => x.EntityId == current.Review.Id && x.Action == "MeetingGoogleIdentityVerifiedEntitlementRequired") && await db.MeetingOAuthExchanges.AnyAsync(x => x.Id == current.Permit.Id && x.Version == 1 && x.Status == "TokensQuarantined"));
        Check("retry-read-after-proved-rollback-still-no-execution", (await Evaluate(current.Review.Id)) is { IdentityVerified: true, StatusCode: 503, CanExecute: false });
        var refresh = await Source();
        var refreshed = await Evaluate(refresh.Review.Id, async () => { if ((await Run(s => s.BeginRefreshAsync(actor, academy, reg, refresh.Permit, 1))).StatusCode != 201) throw new InvalidOperationException("Refresh callback failed"); });
        Check("refresh-during-signature-check-denies-proof", !refreshed.IdentityVerified && refreshed.StatusCode == 409);
        var disconnect = await Source();
        var disconnected = await Evaluate(disconnect.Review.Id, async () => { if ((await Run(s => s.DisconnectLocallyAsync(actor, academy, reg.Provider, disconnect.Binding, true))).StatusCode != 200) throw new InvalidOperationException("Disconnect callback failed"); });
        Check("disconnect-during-key-fetch-denies-proof", !disconnected.IdentityVerified && disconnected.StatusCode == 409);
        var race = await Source(); var barrier = new GoogleIdentityFinalBarrier();
        var checking = Evaluate(race.Review.Id, interceptor: barrier); var dropping = Run(s => s.DisconnectLocallyAsync(actor, academy, reg.Provider, race.Binding, true), barrier);
        await Task.WhenAll(checking, dropping); var checkedResult = await checking;
        await using (var db = Open()) Check("final-audit-disconnect-race-no-enabled-binding", (await dropping).StatusCode == 200 && checkedResult.StatusCode is 409 or 503 && !checkedResult.CanExecute && await db.MeetingProviderBindings.AnyAsync(x => x.AcademyId == academy && x.Status == "Disconnected" && x.Version == race.Binding + 1));
        var stamp = await Source();
        var stampResult = await Evaluate(stamp.Review.Id, async () => { using var scope = factory.Services.CreateScope(); var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(); if (!(await users.UpdateSecurityStampAsync((await users.FindByIdAsync(actorId.ToString()))!)).Succeeded) throw new InvalidOperationException("Stamp fixture failed"); });
        Check("security-stamp-changed-during-key-fetch-denied", !stampResult.IdentityVerified && stampResult.StatusCode == 409);
        var revoked = await Source();
        var revokedResult = await Evaluate(revoked.Review.Id, async () => { using var scope = factory.Services.CreateScope(); var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(); if (!(await users.RemoveFromRoleAsync((await users.FindByIdAsync(actorId.ToString()))!, "AcademyAdmin")).Succeeded) throw new InvalidOperationException("Role fixture failed"); });
        Check("role-revoked-during-key-fetch-denied", !revokedResult.IdentityVerified && revokedResult.StatusCode == 403);
        if (checks.Count != 19) throw new InvalidOperationException("Google identity case count mismatch " + checks.Count);
        var sources = new[] { "apps/api/Infrastructure/Meetings/MeetingGoogleIdentityVerifier.cs", "apps/api/Infrastructure/Meetings/MeetingGoogleIdentityStore.cs", "apps/api/Infrastructure/Meetings/MeetingOAuthAttemptStore.cs", "apps/api/Infrastructure/Meetings/MeetingOAuthCodeExchange.cs", "apps/api/Infrastructure/Meetings/MeetingOAuthExchangeStore.cs", "tests/AcademyDesk.Api.Tests/MeetingGoogleIdentityTests.cs", "QA/tools/SqlHarness/MeetingGoogleIdentityRegression.cs" }.ToDictionary(p => p, p => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(p))));
        var path = Path.Combine("QA", "EVIDENCE", "meeting-google-identity-sql-" + manifest.RunId.ToString("N")); Directory.CreateDirectory(path);
        await File.WriteAllTextAsync(Path.Combine(path, "result.json"), JsonSerializer.Serialize(new { checks, sources, scope = "Owned real SQL/Identity with synthetic RSA issuer keys/tokens and fixed fake key transport; not live Google, consent/entitlement, Connected, public browser or release acceptance" }, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine("MEETING GOOGLE IDENTITY PASS:19 SQL/Identity cases; signed identity is not meeting entitlement.");
    }
}
