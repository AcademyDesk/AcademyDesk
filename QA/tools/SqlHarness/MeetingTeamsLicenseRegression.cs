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
    private const string TeamsLicenseAudit = "MeetingTeamsPlanObservedExecutionQualificationRequired";
    private sealed class TeamsLicenseHandler(string body, string access, Func<Task>? during = null, HttpStatusCode status = HttpStatusCode.OK) : HttpMessageHandler
    {
        internal int Calls;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            Calls++;
            if (request.Method != HttpMethod.Get || request.RequestUri!.AbsoluteUri != MeetingTeamsLicenseProbe.Endpoint || request.Headers.Authorization?.Scheme != "Bearer" || request.Headers.Authorization.Parameter != access || request.Content is not null || request.RequestUri.AbsoluteUri.Contains(access)) throw new InvalidOperationException("Unsafe Teams license request");
            if (during is not null) await during(); return new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
        }
    }
    private sealed class TeamsLicenseAuditFault : DbTransactionInterceptor
    {
        public override ValueTask<InterceptionResult> TransactionCommittingAsync(DbTransaction transaction, TransactionEventData data, InterceptionResult result, CancellationToken token = default)
        { if (data.Context!.ChangeTracker.Entries<AuditLog>().Any(x => x.State == EntityState.Added && x.Entity.Action == TeamsLicenseAudit)) throw new InvalidOperationException("Synthetic Teams license audit commit failure"); return ValueTask.FromResult(result); }
    }
    private sealed class TeamsLicenseFinalBarrier : SaveChangesInterceptor
    {
        private readonly ClaimBarrier barrier = new();
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData data, InterceptionResult<int> result, CancellationToken token = default) =>
            data.Context!.ChangeTracker.Entries<AuditLog>().Any(x => x.State == EntityState.Added && (x.Entity.Action == TeamsLicenseAudit || x.Entity.Action == "MeetingDisconnectedLocallyRevocationRequired"))
                ? barrier.SavingChangesAsync(data, result, token) : ValueTask.FromResult(result);
    }
    private static async Task VerifyMeetingTeamsLicenseAsync(QaApiFactory factory, QaRunManifest manifest)
    {
        string connection; using (var scope = factory.Services.CreateScope()) connection = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>().Database.GetConnectionString()!;
        var target = new SqlConnectionStringBuilder(connection); if (target.DataSource != manifest.SqlServer || target.InitialCatalog != manifest.Database || target.UserID != manifest.RuntimeLogin) throw new InvalidOperationException("Teams license requires exact owned SQL target");
        AcademyDeskDbContext Open(IInterceptor? interceptor = null) { var b = new DbContextOptionsBuilder<AcademyDeskDbContext>().UseSqlServer(connection); if (interceptor is not null) b.AddInterceptors(interceptor); return new(b.Options); }
        var tenant = new Academy { Name = "Synthetic Teams License Academy", EnabledModulesJson = "[\"Core\",\"Engagement\"]" }; await using (var db = Open()) { db.Add(tenant); await db.SaveChangesAsync(); }
        var academy = tenant.Id; var actorId = await CreateAccessActorAsync(factory, "qa-teams-license@example.invalid", "AcademyAdmin", academy); var otherId = await CreateAccessActorAsync(factory, "qa-teams-license-other@example.invalid", "AcademyAdmin", academy);
        ClaimsPrincipal Actor(Guid id) => new(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, id.ToString()) }, "synthetic-license"));
        var actor = Actor(actorId); var clock = new OperationClock(); var protection = factory.Services.GetRequiredService<IDataProtectionProvider>();
        async Task<T> Run<T>(Func<MeetingOAuthAttemptStore, Task<T>> action, IInterceptor? interceptor = null)
        { using var scope = factory.Services.CreateScope(); await using var db = Open(interceptor); var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(); return await action(new(db, users, new(db, users, clock), protection, clock)); }
        const string directory = "019acc9a-6200-7000-8000-000000000001", objectId = "019acc9a-6200-7000-8000-000000000002", client = "019acc9a-6200-7000-8000-000000000003", access = "synthetic-teams-license-access";
        MeetingOAuthRegistration Registration(bool identity) => MeetingOAuthRegistrationRegistry.Create("https://api.academy.invalid", "https://app.academy.invalid", [new("Microsoft365", 1, client, "https://api.academy.invalid/api/meetings/oauth/Microsoft365/callback", "https://app.academy.invalid/communication-settings", identity ? MeetingOAuthFlowProfile.MicrosoftIdentityProfile : "microsoft-online-meetings-work/1", directory)]).Resolve("Microsoft365")!;
        using var rsa = RSA.Create(2048); var p = rsa.ExportParameters(false); string Encode(byte[] bytes) => WebEncoders.Base64UrlEncode(bytes);
        var keys = JsonSerializer.Serialize(new { keys = new[] { new { kty = "RSA", use = "sig", kid = "synthetic-license-key", n = Encode(p.Modulus!), e = Encode(p.Exponent!), issuer = MeetingMicrosoftIdentityVerifier.IssuerTemplate } } });
        var body = JsonSerializer.Serialize(new { value = new[] { new { servicePlans = new[] { new { servicePlanId = MeetingTeamsLicenseProbe.TeamsPlan, servicePlanName = "TEAMS1", provisioningStatus = "Success", appliesTo = "User" } } } } });
        async Task<MicrosoftIdentitySource> Source(bool identity = true)
        {
            var reg = Registration(identity); int version; await using (var db = Open()) version = await db.MeetingProviderBindings.Where(x => x.AcademyId == academy && x.Provider == reg.Provider).Select(x => (int?)x.Version).SingleOrDefaultAsync() ?? 0;
            return await Run(async store =>
            {
                var browser = Encode(RandomNumberGenerator.GetBytes(32)); var intent = await store.PrepareAsync(actor, academy, reg.Provider, version, browser, reg.Fingerprint);
                if (intent.StatusCode != 201 || (await store.CaptureAsync(academy, reg.Provider, intent.Material!.State, browser, reg.Fingerprint, "synthetic-license-code", false)).StatusCode != 202) throw new InvalidOperationException("Teams license source capture failed");
                var claim = await store.ResumeAsync(actor, academy, reg.Provider, intent.Material.State, browser, reg.Fingerprint); var permit = (await store.BeginExchangeAsync(actor, academy, reg, claim)).Permit!;
                var header = Encode(JsonSerializer.SerializeToUtf8Bytes(new { alg = "RS256", typ = "JWT", kid = "synthetic-license-key" }));
                var claims = Encode(JsonSerializer.SerializeToUtf8Bytes(new { iss = MeetingMicrosoftIdentityVerifier.Issuer(reg), aud = client, tid = directory, oid = objectId, sub = "pairwise-teams-license", ver = "2.0", nonce = intent.Material.IdentityNonce,
                    iat = clock.Now.ToUnixTimeSeconds(), nbf = clock.Now.AddSeconds(-30).ToUnixTimeSeconds(), exp = clock.Now.AddHours(1).ToUnixTimeSeconds() }));
                var data = header + "." + claims; var jwt = data + "." + Encode(rsa.SignData(Encoding.ASCII.GetBytes(data), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
                if ((await store.FinishExchangeAsync(actor, academy, reg, permit, new(200, "TokensReceivedAccountUnverified", Tokens: new(access, "synthetic-license-refresh", clock.Now.AddHours(1), reg.Flow!.RequiredTokenScopes, identity ? jwt : null)))).StatusCode != 200) throw new InvalidOperationException("Teams license quarantine failed");
                var review = (await store.PrepareAccountReviewAsync(actor, academy, reg, permit, new(objectId, null, clock.Now))).Review;
                if (review is null || (await store.ConfirmAccountReviewAsync(actor, academy, reg, review.Id, 0, review.ReviewHash, true)).StatusCode != 200) throw new InvalidOperationException("Teams license review failed");
                return new MicrosoftIdentitySource(reg, MeetingAccountPolicyRegistry.Create([new(academy, reg.Provider, 1, reg.Fingerprint, objectId, null, directory, clock.Now.AddMinutes(-1), clock.Now.AddHours(4))]), review, permit, version + 2);
            });
        }
        async Task<MeetingTeamsLicenseOutcome> Evaluate(MicrosoftIdentitySource source, Func<Task>? during = null, IInterceptor? interceptor = null, string? response = null, HttpStatusCode status = HttpStatusCode.OK, ClaimsPrincipal? who = null, Guid? route = null, bool noLicenseExpected = false)
        {
            using var identityHandler = new MicrosoftIdentityHandler(MeetingMicrosoftIdentityVerifier.KeysEndpoint(source.Registration), keys, access, objectId); using var identityTransport = new HttpMessageInvoker(identityHandler);
            using var licenseHandler = new TeamsLicenseHandler(response ?? body, access, during, status); using var licenseTransport = new HttpMessageInvoker(licenseHandler);
            var result = await Run(s => s.CheckTeamsLicenseAsync(who ?? actor, route ?? academy, source.Registration, source.Review.Id, source.Policies, new(identityTransport, clock), identityTransport, new(licenseTransport)), interceptor);
            if (result.Connected || result.ProviderVerified || result.CanExecute || result.CanRetry || JsonSerializer.Serialize(result).Contains(objectId)) throw new InvalidOperationException("License observation leaked account or granted execution");
            if (noLicenseExpected && licenseHandler.Calls != 0) throw new InvalidOperationException("Unverified identity used license credential"); return result;
        }
        var checks = new List<string>(); void Check(string name, bool pass) { if (!pass) throw new InvalidOperationException("MEETING TEAMS LICENSE " + name); checks.Add(name); Console.WriteLine("MEETING TEAMS LICENSE CASE " + name + " PASS"); }
        var source = await Source(); var observed = await Evaluate(source); Check("recognized-provisioned-plan-still-503-not-executable", observed is { StatusCode: 503, LicenseReadAuthorized: true, TeamsPlanProvisioned: true });
        await using (var db = Open())
        {
            var audit = await db.AuditLogs.SingleAsync(x => x.EntityId == source.Review.Id && x.Action == TeamsLicenseAudit);
            Check("private-audit-no-object-directory-access-or-license-SKU", !audit.MetadataJson!.Contains(objectId) && !audit.MetadataJson.Contains(directory) && !audit.MetadataJson.Contains(access) && !audit.MetadataJson.Contains(MeetingTeamsLicenseProbe.TeamsPlan));
            Check("no-binding-or-source-version-or-scope-expansion", await db.MeetingProviderBindings.AnyAsync(x => x.AcademyId == academy && x.Status == "VerificationRequired" && x.Version == source.Binding) && await db.MeetingOAuthExchanges.AnyAsync(x => x.Id == source.Permit.Id && x.Version == 1) && !source.Registration.Flow!.RequestedScopes.Contains("LicenseAssignment.Read.All"));
            Check("required-credential-audit-before-license-bearer", await db.AuditLogs.CountAsync(x => x.EntityId == source.Permit.Id && x.Action == "MeetingOAuthQuarantinedTokensRead") == 3);
        }
        var old = await Source(false); Check("old-profile-denied-without-license-call", (await Evaluate(old, noLicenseExpected: true)).Code == "MicrosoftIdentityConsentRequired");
        source = await Source();
        async Task Denied(string label, Func<Task<MeetingTeamsLicenseOutcome>> request, int status)
        {
            int before; await using (var db = Open()) before = await db.AuditLogs.CountAsync(x => x.Action == TeamsLicenseAudit); var result = await request();
            await using (var db = Open()) Check(label, result.StatusCode == status && !result.TeamsPlanProvisioned && await db.AuditLogs.CountAsync(x => x.Action == TeamsLicenseAudit) == before);
        }
        await Denied("license-permission-denial-no-role-or-scope-fallback", () => Evaluate(source, status: HttpStatusCode.Forbidden), 502);
        await Denied("disabled-plan-not-provisioned", () => Evaluate(source, response: body.Replace("Success", "Disabled")), 409);
        await Denied("name-only-unknown-plan-not-recognized", () => Evaluate(source, response: body.Replace(MeetingTeamsLicenseProbe.TeamsPlan, objectId)), 409);
        await Denied("paginated-evidence-not-followed-or-partially-accepted", () => Evaluate(source, response: body[..^1] + ",\"@odata.nextLink\":\"https://attacker.invalid\"}"), 502);
        await Denied("known-plan-followed-by-malformed-license-not-partial-success", () => Evaluate(source, response: body[..^2] + ",{}]}"), 502);
        await Denied("foreign-academy-no-license-read", () => Evaluate(source, route: Guid.NewGuid(), noLicenseExpected: true), 403);
        await Denied("other-current-admin-no-license-read", () => Evaluate(source, who: Actor(otherId), noLicenseExpected: true), 404);
        await Denied("anonymous-no-license-read", () => Evaluate(source, who: new ClaimsPrincipal(), noLicenseExpected: true), 401);
        var fault = await Source(); var failed = false;
        try { await Evaluate(fault, interceptor: new TeamsLicenseAuditFault()); } catch (InvalidOperationException ex) when (ex.Message == "Synthetic Teams license audit commit failure") { failed = true; }
        await using (var db = Open()) Check("post-SQL-final-audit-failure-rolls-back-no-success", failed && !await db.AuditLogs.AnyAsync(x => x.EntityId == fault.Review.Id && x.Action == TeamsLicenseAudit) && await db.MeetingOAuthExchanges.AnyAsync(x => x.Id == fault.Permit.Id && x.Version == 1));
        Check("read-after-proved-rollback-still-non-executable", (await Evaluate(fault)) is { StatusCode: 503, TeamsPlanProvisioned: true, CanExecute: false });
        var refresh = await Source(); MeetingRefreshMaterial? material = null;
        await Denied("refresh-during-license-read-invalidates-observation", () => Evaluate(refresh, async () => { var start = await Run(s => s.BeginRefreshAsync(actor, academy, refresh.Registration, refresh.Permit, 1)); if (start.StatusCode != 201) throw new InvalidOperationException("Teams license refresh fixture failed"); material = start.Material; }), 409);
        var finished = await Run(s => s.FinishRefreshAsync(actor, academy, refresh.Registration, material!, new(200, "TokensReceivedAccountUnverified", Tokens: new("synthetic-new-license-access", "synthetic-new-license-refresh", clock.Now.AddHours(1), refresh.Registration.Flow!.RequiredTokenScopes))));
        if (finished.StatusCode != 200 || finished.Permit is null) throw new InvalidOperationException("Teams license finish refresh fixture failed");
        var renewed = await Run(s => s.PrepareAccountReviewAsync(actor, academy, refresh.Registration, finished.Permit, new(objectId, null, clock.Now)));
        if (renewed.StatusCode != 201 || renewed.Review is null || (await Run(s => s.ConfirmAccountReviewAsync(actor, academy, refresh.Registration, renewed.Review.Id, 0, renewed.Review.ReviewHash, true))).StatusCode != 200) throw new InvalidOperationException("Teams license renewed review fixture failed");
        Check("refreshed-confirmed-credential-needs-fresh-identity-before-license", (await Evaluate(refresh with { Review = renewed.Review, Permit = finished.Permit }, noLicenseExpected: true)).Code == "FreshMicrosoftIdentityEvidenceRequired");
        var disconnect = await Source(); await Denied("disconnect-during-license-read-invalidates-observation", () => Evaluate(disconnect, async () => { if ((await Run(s => s.DisconnectLocallyAsync(actor, academy, disconnect.Registration.Provider, disconnect.Binding, true))).StatusCode != 200) throw new InvalidOperationException("Teams license disconnect fixture failed"); }), 409);
        var race = await Source(); var barrier = new TeamsLicenseFinalBarrier(); var reading = Evaluate(race, interceptor: barrier); var dropping = Run(s => s.DisconnectLocallyAsync(actor, academy, race.Registration.Provider, race.Binding, true), barrier); await Task.WhenAll(reading, dropping);
        await using (var db = Open()) Check("final-audit-disconnect-race-never-enables-binding", (await dropping).StatusCode == 200 && (await reading).StatusCode is 409 or 503 && !(await reading).CanExecute && await db.MeetingProviderBindings.AnyAsync(x => x.AcademyId == academy && x.Status == "Disconnected" && x.Version == race.Binding + 1));
        var expiry = await Source(); var priorTime = clock.Now; await Denied("expiry-during-license-read-invalidates-observation", () => Evaluate(expiry, () => { clock.Now = clock.Now.AddHours(2); return Task.CompletedTask; }), 410); clock.Now = priorTime;
        var stamp = await Source(); await Denied("stamp-change-during-license-read-invalidates-observation", () => Evaluate(stamp, async () => { using var scope = factory.Services.CreateScope(); var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(); if (!(await users.UpdateSecurityStampAsync((await users.FindByIdAsync(actorId.ToString()))!)).Succeeded) throw new InvalidOperationException("Teams license stamp fixture failed"); }), 409);
        var role = await Source(); await Denied("role-revoked-during-license-read-invalidates-observation", () => Evaluate(role, async () => { using var scope = factory.Services.CreateScope(); var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(); if (!(await users.RemoveFromRoleAsync((await users.FindByIdAsync(actorId.ToString()))!, "AcademyAdmin")).Succeeded) throw new InvalidOperationException("Teams license role fixture failed"); }), 403);
        if (checks.Count != 22) throw new InvalidOperationException("Teams license case count mismatch " + checks.Count);
        var sources = new[] { "apps/api/Infrastructure/Meetings/MeetingTeamsLicenseProbe.cs", "apps/api/Infrastructure/Meetings/MeetingTeamsLicenseStore.cs", "apps/api/Infrastructure/Meetings/MeetingMicrosoftIdentityStore.cs", "apps/api/Infrastructure/Meetings/MeetingMicrosoftIdentityVerifier.cs", "tests/AcademyDesk.Api.Tests/MeetingTeamsLicenseTests.cs", "QA/tools/SqlHarness/MeetingTeamsLicenseRegression.cs" }.ToDictionary(p => p, p => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(p))));
        var path = Path.Combine("QA", "EVIDENCE", "meeting-teams-license-sql-" + manifest.RunId.ToString("N")); Directory.CreateDirectory(path);
        await File.WriteAllTextAsync(Path.Combine(path, "result.json"), JsonSerializer.Serialize(new { checks, sources, scope = "Owned SQL/Identity, synthetic RSA identities and fixed fake Graph licence responses; not live directory roles/consent/Teams organizer/write entitlement, Connected or release acceptance" }, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine("MEETING TEAMS LICENSE PASS:22 SQL/Identity cases; recognized plan is not meeting execution entitlement.");
    }
}
