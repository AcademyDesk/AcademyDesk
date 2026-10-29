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
    private const string GoogleCalendarAudit = "MeetingGoogleMeetAdvertisedExecutionQualificationRequired";
    private sealed record GoogleCalendarSource(MeetingOAuthRegistration Registration, MeetingAccountPolicyRegistry Policies, MeetingAccountReviewState Review, MeetingOAuthExchangePermit Permit, int Binding);
    private sealed class GoogleCalendarHandler(string metadata, string access, Func<Task>? during = null, HttpStatusCode status = HttpStatusCode.OK) : HttpMessageHandler
    {
        internal int Calls;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            Calls++;
            if (request.Method != HttpMethod.Get || request.RequestUri!.AbsoluteUri != MeetingGoogleCalendarProbe.Endpoint || request.Headers.Authorization?.Scheme != "Bearer" ||
                request.Headers.Authorization.Parameter != access || request.RequestUri.AbsoluteUri.Contains(access) || request.Content is not null)
                throw new InvalidOperationException("Unsafe calendar metadata request");
            if (during is not null) await during();
            return new(status) { Content = new StringContent(metadata, Encoding.UTF8, "application/json") };
        }
    }
    private sealed class GoogleCalendarAuditFault : DbTransactionInterceptor
    {
        public override ValueTask<InterceptionResult> TransactionCommittingAsync(DbTransaction transaction, TransactionEventData data, InterceptionResult result, CancellationToken token = default)
        {
            if (data.Context!.ChangeTracker.Entries<AuditLog>().Any(x => x.State == EntityState.Added && x.Entity.Action == GoogleCalendarAudit)) throw new InvalidOperationException("Synthetic calendar audit commit failure");
            return ValueTask.FromResult(result);
        }
    }
    private sealed class GoogleCalendarFinalBarrier : SaveChangesInterceptor
    {
        private readonly ClaimBarrier barrier = new();
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData data, InterceptionResult<int> result, CancellationToken token = default) =>
            data.Context!.ChangeTracker.Entries<AuditLog>().Any(x => x.State == EntityState.Added && (x.Entity.Action == GoogleCalendarAudit || x.Entity.Action == "MeetingDisconnectedLocallyRevocationRequired"))
                ? barrier.SavingChangesAsync(data, result, token) : ValueTask.FromResult(result);
    }
    private static async Task VerifyMeetingGoogleCalendarAsync(QaApiFactory factory, QaRunManifest manifest)
    {
        string connection; using (var scope = factory.Services.CreateScope()) connection = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>().Database.GetConnectionString()!;
        var target = new SqlConnectionStringBuilder(connection);
        if (target.DataSource != manifest.SqlServer || target.InitialCatalog != manifest.Database || target.UserID != manifest.RuntimeLogin) throw new InvalidOperationException("Calendar evidence requires exact owned SQL target");
        AcademyDeskDbContext Open(IInterceptor? interceptor = null) { var b = new DbContextOptionsBuilder<AcademyDeskDbContext>().UseSqlServer(connection); if (interceptor is not null) b.AddInterceptors(interceptor); return new(b.Options); }
        var tenant = new Academy { Name = "Synthetic Google Calendar Evidence Academy", EnabledModulesJson = "[\"Core\",\"Engagement\"]" };
        await using (var db = Open()) { db.Add(tenant); await db.SaveChangesAsync(); }
        var academy = tenant.Id; var actorId = await CreateAccessActorAsync(factory, "qa-google-calendar@example.invalid", "AcademyAdmin", academy);
        var otherId = await CreateAccessActorAsync(factory, "qa-google-calendar-other@example.invalid", "AcademyAdmin", academy);
        ClaimsPrincipal Actor(Guid id) => new(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, id.ToString()) }, "synthetic-calendar"));
        var actor = Actor(actorId); var clock = new OperationClock(); var protection = factory.Services.GetRequiredService<IDataProtectionProvider>();
        async Task<T> Run<T>(Func<MeetingOAuthAttemptStore, Task<T>> action, IInterceptor? interceptor = null)
        { using var scope = factory.Services.CreateScope(); await using var db = Open(interceptor); var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(); return await action(new(db, users, new(db, users, clock), protection, clock)); }
        MeetingOAuthRegistration Registration(bool metadata) => MeetingOAuthRegistrationRegistry.Create("https://api.academy.invalid", "https://app.academy.invalid", [new("GoogleWorkspace", 1, "synthetic-client", "https://api.academy.invalid/api/meetings/oauth/GoogleWorkspace/callback", "https://app.academy.invalid/communication-settings", metadata ? MeetingOAuthFlowProfile.GoogleMetadataProfile : "google-calendar-owned-web/1")]).Resolve("GoogleWorkspace")!;
        const string subject = "synthetic-calendar-subject", access = "synthetic-calendar-access", calendar = "private-calendar@example.invalid";
        var metadata = JsonSerializer.Serialize(new { kind = "calendar#calendar", id = calendar, conferenceProperties = new { allowedConferenceSolutionTypes = new[] { "hangoutsMeet" } } });
        using var rsa = RSA.Create(2048); var parameters = rsa.ExportParameters(false); string Encode(byte[] bytes) => WebEncoders.Base64UrlEncode(bytes);
        var keyJson = JsonSerializer.Serialize(new { keys = new[] { new { kty = "RSA", alg = "RS256", use = "sig", kid = "synthetic-key", n = Encode(parameters.Modulus!), e = Encode(parameters.Exponent!) } } });
        var checks = new List<string>(); void Check(string name, bool pass) { if (!pass) throw new InvalidOperationException("MEETING GOOGLE CALENDAR " + name); checks.Add(name); Console.WriteLine("MEETING GOOGLE CALENDAR CASE " + name + " PASS"); }
        async Task<GoogleCalendarSource> Source(bool metadataProfile = true)
        {
            var reg = Registration(metadataProfile); int version;
            await using (var db = Open()) version = await db.MeetingProviderBindings.Where(x => x.AcademyId == academy && x.Provider == reg.Provider).Select(x => (int?)x.Version).SingleOrDefaultAsync() ?? 0;
            return await Run(async store =>
            {
                var browser = Encode(RandomNumberGenerator.GetBytes(32)); var intent = await store.PrepareAsync(actor, academy, reg.Provider, version, browser, reg.Fingerprint);
                if (intent.StatusCode != 201 || (await store.CaptureAsync(academy, reg.Provider, intent.Material!.State, browser, reg.Fingerprint, "synthetic-calendar-code", false)).StatusCode != 202) throw new InvalidOperationException("Calendar source capture failed");
                var claim = await store.ResumeAsync(actor, academy, reg.Provider, intent.Material.State, browser, reg.Fingerprint); var permit = (await store.BeginExchangeAsync(actor, academy, reg, claim)).Permit!;
                var head = Encode(JsonSerializer.SerializeToUtf8Bytes(new { alg = "RS256", kid = "synthetic-key", typ = "JWT" }));
                var payload = Encode(JsonSerializer.SerializeToUtf8Bytes(new { iss = "https://accounts.google.com", aud = "synthetic-client", sub = subject, iat = clock.Now.ToUnixTimeSeconds(), exp = clock.Now.AddHours(1).ToUnixTimeSeconds(), nonce = intent.Material.IdentityNonce,
                    at_hash = Encode(SHA256.HashData(Encoding.ASCII.GetBytes(access)).AsSpan(0, 16).ToArray()) }));
                var data = head + "." + payload; var jwt = data + "." + Encode(rsa.SignData(Encoding.ASCII.GetBytes(data), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
                if ((await store.FinishExchangeAsync(actor, academy, reg, permit, new(200, "TokensReceivedAccountUnverified", Tokens: new(access, "synthetic-calendar-refresh", clock.Now.AddHours(1), reg.Flow!.RequiredTokenScopes, jwt)))).StatusCode != 200) throw new InvalidOperationException("Calendar quarantine failed");
                var review = (await store.PrepareAccountReviewAsync(actor, academy, reg, permit, new(subject, null, clock.Now))).Review!;
                if ((await store.ConfirmAccountReviewAsync(actor, academy, reg, review.Id, 0, review.ReviewHash, true)).StatusCode != 200) throw new InvalidOperationException("Calendar review failed");
                var policy = MeetingAccountPolicyRegistry.Create([new(academy, reg.Provider, 1, reg.Fingerprint, subject, null, null, clock.Now.AddMinutes(-1), clock.Now.AddHours(4))]);
                return new GoogleCalendarSource(reg, policy, review, permit, version + 2);
            });
        }
        async Task<MeetingGoogleCalendarOutcome> Evaluate(GoogleCalendarSource source, Func<Task>? during = null, IInterceptor? interceptor = null,
            string? body = null, HttpStatusCode status = HttpStatusCode.OK, ClaimsPrincipal? who = null, Guid? route = null)
        {
            using var keysHandler = new GoogleKeysHandler(keyJson); using var keyTransport = new HttpMessageInvoker(keysHandler);
            using var calendarHandler = new GoogleCalendarHandler(body ?? metadata, access, during, status); using var calendarTransport = new HttpMessageInvoker(calendarHandler);
            var result = await Run(store => store.CheckGoogleCalendarAsync(who ?? actor, route ?? academy, source.Registration, source.Review.Id, source.Policies, new(keyTransport, clock), new(calendarTransport)), interceptor);
            if (source.Registration.Flow!.Id == "google-calendar-owned-web/1" && (keysHandler.Calls != 0 || calendarHandler.Calls != 0)) throw new InvalidOperationException("Old profile attempted additional metadata scope");
            if (result.Connected || result.ProviderVerified || result.CanExecute || result.CalendarFingerprint is not null || JsonSerializer.Serialize(result).Contains(calendar)) throw new InvalidOperationException("Capability observation leaked or granted execution");
            return result;
        }
        var old = await Source(false); Check("old-profile-needs-fresh-metadata-consent-without-network", (await Evaluate(old)).Code == "GoogleCalendarMetadataConsentRequired");
        var source = await Source(); var observed = await Evaluate(source);
        Check("metadata-meet-advertised-still-503-not-provider-qualified", observed is { StatusCode: 503, MetadataReadAuthorized: true, MeetSupported: true });
        await using (var db = Open())
        {
            var audit = await db.AuditLogs.SingleAsync(x => x.EntityId == source.Review.Id && x.Action == GoogleCalendarAudit);
            Check("final-audit-fingerprints-only-no-calendar-subject-access", !audit.MetadataJson!.Contains(calendar) && !audit.MetadataJson.Contains(subject) && !audit.MetadataJson.Contains(access));
            Check("required-audited-quarantine-read-before-bearer-metadata", await db.AuditLogs.AnyAsync(x => x.EntityId == source.Permit.Id && x.Action == "MeetingOAuthQuarantinedTokensRead"));
            Check("binding-remains-verification-required-and-source-version-one", await db.MeetingProviderBindings.AnyAsync(x => x.AcademyId == academy && x.Status == "VerificationRequired" && x.Version == source.Binding) && await db.MeetingOAuthExchanges.AnyAsync(x => x.Id == source.Permit.Id && x.Version == 1));
        }
        async Task Denied(string label, Func<Task<MeetingGoogleCalendarOutcome>> request, int status)
        {
            int before; await using (var db = Open()) before = await db.AuditLogs.CountAsync(x => x.Action == GoogleCalendarAudit);
            var result = await request(); await using (var db = Open()) Check(label, result.StatusCode == status && !result.MeetSupported && await db.AuditLogs.CountAsync(x => x.Action == GoogleCalendarAudit) == before);
        }
        await Denied("no-advertised-meet-no-success-audit", () => Evaluate(source, body: metadata.Replace("hangoutsMeet", "eventHangout")), 409);
        await Denied("malformed-metadata-no-success-audit", () => Evaluate(source, body: "{\"kind\":\"calendar#calendar\",\"id\":123}"), 502);
        await Denied("provider-403-no-capability-or-success-audit", () => Evaluate(source, status: HttpStatusCode.Forbidden), 502);
        await Denied("provider-redirect-not-followed-or-enabled", () => Evaluate(source, status: HttpStatusCode.Redirect), 502);
        await Denied("foreign-academy-denied", () => Evaluate(source, route: Guid.NewGuid()), 403);
        await Denied("other-current-admin-denied", () => Evaluate(source, who: Actor(otherId)), 404);
        await Denied("anonymous-denied", () => Evaluate(source, who: new ClaimsPrincipal()), 401);
        var failed = false; var fault = await Source();
        try { await Evaluate(fault, interceptor: new GoogleCalendarAuditFault()); } catch (InvalidOperationException ex) when (ex.Message == "Synthetic calendar audit commit failure") { failed = true; }
        await using (var db = Open()) Check("final-post-write-audit-fault-rolled-back-no-success", failed && !await db.AuditLogs.AnyAsync(x => x.EntityId == fault.Review.Id && x.Action == GoogleCalendarAudit) && await db.MeetingOAuthExchanges.AnyAsync(x => x.Id == fault.Permit.Id && x.Version == 1));
        Check("read-after-proved-rollback-still-unavailable", (await Evaluate(fault)) is { StatusCode: 503, MeetSupported: true, CanExecute: false });
        var refresh = await Source(); MeetingRefreshMaterial? refreshMaterial = null;
        await Denied("refresh-during-calendar-read-invalidates-capability", () => Evaluate(refresh, async () =>
        { var start = await Run(s => s.BeginRefreshAsync(actor, academy, refresh.Registration, refresh.Permit, 1)); if (start.StatusCode != 201) throw new InvalidOperationException("Metadata refresh fixture failed"); refreshMaterial = start.Material; }), 409);
        var finishedRefresh = await Run(s => s.FinishRefreshAsync(actor, academy, refresh.Registration, refreshMaterial!, new(200, "TokensReceivedAccountUnverified", Tokens: new("synthetic-new-calendar-access", "synthetic-calendar-refresh", clock.Now.AddHours(1), refresh.Registration.Flow!.RequiredTokenScopes))));
        if (finishedRefresh.StatusCode != 200 || finishedRefresh.Permit is null) throw new InvalidOperationException("Metadata finish refresh failed");
        // Old permits are fenced by refresh. Exercise the new confirmed review
        // using the actual newly issued permit, never the superseded fixture.
        var renewed = await Run(s => s.PrepareAccountReviewAsync(actor, academy, refresh.Registration, finishedRefresh.Permit, new(subject, null, clock.Now)));
        if (renewed.StatusCode != 201 || renewed.Review is null) throw new InvalidOperationException("Renewed calendar preparation failed: " + renewed.Code);
        if ((await Run(s => s.ConfirmAccountReviewAsync(actor, academy, refresh.Registration, renewed.Review!.Id, 0, renewed.Review.ReviewHash, true))).StatusCode != 200) throw new InvalidOperationException("Renewed calendar confirmation failed");
        var renewedSource = refresh with { Review = renewed.Review!, Permit = finishedRefresh.Permit };
        Check("refresh-cannot-inherit-old-signed-identity-even-after-new-confirmation", (await Evaluate(renewedSource)).Code == "FreshGoogleIdentityEvidenceRequired");
        var disconnect = await Source();
        await Denied("disconnect-during-calendar-read-invalidates-capability", () => Evaluate(disconnect, async () => { if ((await Run(s => s.DisconnectLocallyAsync(actor, academy, disconnect.Registration.Provider, disconnect.Binding, true))).StatusCode != 200) throw new InvalidOperationException("Calendar disconnect failed"); }), 409);
        var race = await Source(); var barrier = new GoogleCalendarFinalBarrier();
        var checking = Evaluate(race, interceptor: barrier); var dropping = Run(s => s.DisconnectLocallyAsync(actor, academy, race.Registration.Provider, race.Binding, true), barrier);
        await Task.WhenAll(checking, dropping);
        await using (var db = Open()) Check("final-audit-disconnect-race-never-enables-binding", (await dropping).StatusCode == 200 && (await checking).StatusCode is 409 or 503 && !(await checking).CanExecute && await db.MeetingProviderBindings.AnyAsync(x => x.AcademyId == academy && x.Status == "Disconnected" && x.Version == race.Binding + 1));
        var expiry = await Source(); var priorTime = clock.Now;
        await Denied("expiry-during-calendar-read-invalidates-capability", () => Evaluate(expiry, () => { clock.Now = clock.Now.AddHours(2); return Task.CompletedTask; }), 410); clock.Now = priorTime;
        var stamp = await Source();
        await Denied("security-stamp-changed-during-calendar-read-denied", () => Evaluate(stamp, async () => { using var scope = factory.Services.CreateScope(); var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(); if (!(await users.UpdateSecurityStampAsync((await users.FindByIdAsync(actorId.ToString()))!)).Succeeded) throw new InvalidOperationException("Calendar stamp fixture failed"); }), 409);
        var role = await Source();
        await Denied("role-revoked-during-calendar-read-denied", () => Evaluate(role, async () => { using var scope = factory.Services.CreateScope(); var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(); if (!(await users.RemoveFromRoleAsync((await users.FindByIdAsync(actorId.ToString()))!, "AcademyAdmin")).Succeeded) throw new InvalidOperationException("Calendar role fixture failed"); }), 403);
        Check("new-registration-and-policy-fingerprints-not-old-profile", old.Registration.Fingerprint != source.Registration.Fingerprint && source.Policies.Resolve(academy, old.Registration, clock.Now) is null);
        if (checks.Count != 22) throw new InvalidOperationException("Google calendar case count mismatch " + checks.Count);
        var sources = new[] { "apps/api/Infrastructure/Meetings/MeetingGoogleCalendarProbe.cs", "apps/api/Infrastructure/Meetings/MeetingGoogleCalendarStore.cs", "apps/api/Infrastructure/Meetings/MeetingOAuthFlowProfile.cs", "apps/api/Infrastructure/Meetings/MeetingGoogleIdentityStore.cs", "apps/api/Infrastructure/Meetings/MeetingGoogleIdentityVerifier.cs", "apps/api/Infrastructure/Meetings/MeetingOAuthCodeExchange.cs", "apps/api/Infrastructure/Meetings/MeetingRefreshStore.cs", "tests/AcademyDesk.Api.Tests/MeetingGoogleCalendarTests.cs", "QA/tools/SqlHarness/MeetingGoogleCalendarRegression.cs" }.ToDictionary(p => p, p => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(p))));
        var path = Path.Combine("QA", "EVIDENCE", "meeting-google-calendar-sql-" + manifest.RunId.ToString("N")); Directory.CreateDirectory(path);
        await File.WriteAllTextAsync(Path.Combine(path, "result.json"), JsonSerializer.Serialize(new { checks, sources, scope = "Owned SQL/Identity with synthetic RSA identities and fixed fake metadata transport; not live Google, actual write/admin-consent/license/grant isolation, Connected, public browser or release acceptance" }, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine("MEETING GOOGLE CALENDAR PASS:22 SQL/Identity cases; advertised Meet support is not execution qualification.");
    }
}
