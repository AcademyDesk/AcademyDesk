using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
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
    private sealed record GoogleRevocationSource(Guid Academy, Guid ActorId, MeetingOAuthRegistration Registration, MeetingOAuthExchangePermit Permit);
    private static async Task VerifyMeetingGoogleRevocationAsync(QaApiFactory factory, QaRunManifest manifest)
    {
        string connection; using (var scope = factory.Services.CreateScope()) connection = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>().Database.GetConnectionString()!;
        var target = new SqlConnectionStringBuilder(connection); if (target.DataSource != manifest.SqlServer || target.InitialCatalog != manifest.Database || target.UserID != manifest.RuntimeLogin) throw new InvalidOperationException("Google metadata revocation requires exact owned SQL target");
        AcademyDeskDbContext Open(IInterceptor? interceptor = null) { var b = new DbContextOptionsBuilder<AcademyDeskDbContext>().UseSqlServer(connection); if (interceptor is not null) b.AddInterceptors(interceptor); return new(b.Options); }
        var clock = new OperationClock(); var protection = factory.Services.GetRequiredService<IDataProtectionProvider>();
        ClaimsPrincipal Actor(Guid id) => new(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, id.ToString()) }, "synthetic-google-revocation"));
        async Task<T> Run<T>(Func<MeetingOAuthAttemptStore, Task<T>> action, IInterceptor? interceptor = null)
        { using var scope = factory.Services.CreateScope(); await using var db = Open(interceptor); var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(); return await action(new(db, users, new(db, users, clock), protection, clock)); }
        const string refresh = "synthetic-metadata-revoke-refresh";
        MeetingOAuthRegistration Reg(bool metadata = true) => MeetingOAuthRegistrationRegistry.Create("https://api.academy.invalid", "https://app.academy.invalid", [new("GoogleWorkspace", 1, "synthetic-metadata-revoke-client", "https://api.academy.invalid/api/meetings/oauth/GoogleWorkspace/callback", "https://app.academy.invalid/communication-settings", metadata ? MeetingOAuthFlowProfile.GoogleMetadataProfile : "google-calendar-owned-web/1")]).Resolve("GoogleWorkspace")!;
        async Task<GoogleRevocationSource> Source()
        {
            var tenant = new Academy { Name = "Synthetic Google metadata revocation " + Guid.NewGuid().ToString("N"), EnabledModulesJson = "[\"Core\",\"Engagement\"]" }; await using (var db = Open()) { db.Add(tenant); await db.SaveChangesAsync(); }
            var actorId = await CreateAccessActorAsync(factory, "qa-google-revocation-" + Guid.NewGuid().ToString("N") + "@example.invalid", "AcademyAdmin", tenant.Id); var actor = Actor(actorId); var reg = Reg();
            return await Run(async store => {
                var browser = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32)); var intent = await store.PrepareAsync(actor, tenant.Id, reg.Provider, 0, browser, reg.Fingerprint);
                if (intent.StatusCode != 201 || (await store.CaptureAsync(tenant.Id, reg.Provider, intent.Material!.State, browser, reg.Fingerprint, "synthetic-metadata-revoke-code", false)).StatusCode != 202) throw new InvalidOperationException("Metadata revocation capture fixture failed");
                var claim = await store.ResumeAsync(actor, tenant.Id, reg.Provider, intent.Material.State, browser, reg.Fingerprint); var permit = (await store.BeginExchangeAsync(actor, tenant.Id, reg, claim)).Permit!;
                if ((await store.FinishExchangeAsync(actor, tenant.Id, reg, permit, new(200, "TokensReceivedAccountUnverified", Tokens: new("synthetic-metadata-revoke-access", refresh, clock.Now.AddHours(1), reg.Flow!.RequiredTokenScopes)))).StatusCode != 200) throw new InvalidOperationException("Metadata revocation quarantine failed");
                return new GoogleRevocationSource(tenant.Id, actorId, reg, permit);
            });
        }
        Task<MeetingRevocationOutcome> Begin(GoogleRevocationSource x, IInterceptor? interceptor = null) => Run(s => s.BeginRevocationAsync(Actor(x.ActorId), x.Academy, x.Registration, x.Permit, 2, true, true), interceptor);
        Task<MeetingOAuthOutcome> Reconnect(GoogleRevocationSource x, MeetingOAuthRegistration? registration = null) => Run(s => s.PrepareAsync(Actor(x.ActorId), x.Academy, x.Registration.Provider, 3, WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32)), (registration ?? x.Registration).Fingerprint));
        HttpResponseMessage Empty() => new(HttpStatusCode.OK) { Content = new ByteArrayContent([]) };
        var checks = new List<string>(); void Check(string name, bool pass) { if (!pass) throw new InvalidOperationException("MEETING GOOGLE REVOCATION " + name); checks.Add(name); Console.WriteLine("MEETING GOOGLE REVOCATION CASE " + name + " PASS"); }
        var source = await Source(); var admitted = await Begin(source); Check("metadata-profile-durable-revocation-admission", admitted.StatusCode == 201 && admitted.Material is not null);
        using var handler = new RefreshTransport(async request => {
            if (request.Method != HttpMethod.Post || request.RequestUri!.AbsoluteUri != "https://oauth2.googleapis.com/revoke" || request.Headers.Authorization is not null || request.RequestUri.AbsoluteUri.Contains(refresh) || await request.Content!.ReadAsStringAsync() != "token=" + refresh || request.Headers.CacheControl?.NoStore != true) throw new InvalidOperationException("Unsafe metadata revocation request");
            await using var db = Open(); Check("fixed-body-only-revoke-after-durable-disconnect-audit", await db.MeetingProviderBindings.AnyAsync(x => x.AcademyId == source.Academy && x.Status == "Disconnected" && x.Version == 3) && await db.AuditLogs.AnyAsync(x => x.EntityId == admitted.Material!.Id && x.Action == "MeetingRevocationStarted")); return Empty(); });
        using var transport = new HttpMessageInvoker(handler); var result = await Run(s => new MeetingProviderRevocation(s, transport).SendStartedAsync(Actor(source.ActorId), source.Academy, source.Registration, admitted.Material!));
        Check("only-provider-acknowledgement-not-connected-or-retry", result is { StatusCode: 200, Connected: false, CanRetry: false, Material: null } && handler.Calls == 1 && !JsonSerializer.Serialize(result).Contains(refresh));
        await using (var db = Open()) Check("ack-v1-one-completion-audit-no-token-duplication", await db.MeetingCredentialRevocations.AnyAsync(x => x.Id == admitted.Material!.Id && x.Status == "ProviderAcknowledged" && x.Version == 1 && !x.ProtectedEnvelope.Contains(refresh)) && await db.AuditLogs.CountAsync(x => x.EntityId == admitted.Material!.Id && x.Action == "MeetingRevocationProviderAcknowledged") == 1);
        Check("ack-replay-never-resends-and-old-credentials-inaccessible", (await Run(s => new MeetingProviderRevocation(s, transport).SendStartedAsync(Actor(source.ActorId), source.Academy, source.Registration, admitted.Material!))).StatusCode == 409 && handler.Calls == 1 && (await Run(s => s.ReadQuarantinedTokensAsync(Actor(source.ActorId), source.Academy, source.Registration, source.Permit))).StatusCode == 409);
        // Acknowledgement is not independent proof of full project-wide effect.
        // These reconnect denials must neither mutate state nor mint a new intent.
        async Task<string> Snapshot()
        {
            await using var db = Open();
            return JsonSerializer.Serialize(new {
                bindings = await db.MeetingProviderBindings.AsNoTracking().Where(x => x.AcademyId == source.Academy).OrderBy(x => x.Id).ToListAsync(),
                attempts = await db.MeetingOAuthAttempts.AsNoTracking().Where(x => x.AcademyId == source.Academy).OrderBy(x => x.Id).ToListAsync(),
                revocations = await db.MeetingCredentialRevocations.AsNoTracking().Where(x => x.AcademyId == source.Academy).OrderBy(x => x.Id).ToListAsync(),
                audits = await db.AuditLogs.AsNoTracking().Where(x => x.AcademyId == source.Academy).OrderBy(x => x.Id).ToListAsync() });
        }
        var snapshot = await Snapshot();
        Check("acknowledgement-blocks-metadata-reconnect", (await Reconnect(source)) is { StatusCode: 409, Code: "ProviderRevocationNeedsReview", Material: null });
        Check("acknowledgement-blocks-old-profile-reconnect", (await Reconnect(source, Reg(false))) is { StatusCode: 409, Code: "ProviderRevocationNeedsReview", Material: null });
        var otherAdmin = await CreateAccessActorAsync(factory, "qa-google-reconnect-other-" + Guid.NewGuid().ToString("N") + "@example.invalid", "AcademyAdmin", source.Academy);
        Check("acknowledgement-blocks-other-current-admin", (await Run(s => s.PrepareAsync(Actor(otherAdmin), source.Academy, source.Registration.Provider, 3, WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32)), source.Registration.Fingerprint))).Code == "ProviderRevocationNeedsReview");
        var reconnects = await Task.WhenAll(Reconnect(source), Reconnect(source));
        Check("simultaneous-reconnects-both-denied-no-permits", reconnects.All(x => x.StatusCode == 409 && x.Code == "ProviderRevocationNeedsReview" && x.Material is null));
        Check("acknowledgement-not-convertible-to-manual-clear", (await Run(s => s.PrepareManualRevocationReviewAsync(Actor(source.ActorId), source.Academy, source.Registration, admitted.Material!.Id))).StatusCode == 409);
        var changedRegistration = MeetingOAuthRegistrationRegistry.Create("https://api.academy.invalid", "https://app.academy.invalid", [new("GoogleWorkspace", 2, "synthetic-different-reconnect-client", "https://api.academy.invalid/api/meetings/oauth/GoogleWorkspace/callback", "https://app.academy.invalid/communication-settings", MeetingOAuthFlowProfile.GoogleMetadataProfile)]).Resolve("GoogleWorkspace")!;
        Check("changed-registration-cannot-clear-project-evidence", (await Reconnect(source, changedRegistration)).Code == "ProviderRevocationNeedsReview");
        var acknowledgedAt = clock.Now; clock.Now = clock.Now.AddDays(365);
        Check("elapsed-time-and-token-expiry-not-reconciliation", (await Reconnect(source)).Code == "ProviderRevocationNeedsReview" && (await Reconnect(source, Reg(false))).Code == "ProviderRevocationNeedsReview"); clock.Now = acknowledgedAt;
        Check("denials-retain-exact-binding-attempt-revocation-audit-snapshot", await Snapshot() == snapshot && handler.Calls == 1);
        var microsoft = MeetingOAuthRegistrationRegistry.Create("https://api.academy.invalid", "https://app.academy.invalid", [new("Microsoft365", 1, "synthetic-unrelated-client", "https://api.academy.invalid/api/meetings/oauth/Microsoft365/callback", "https://app.academy.invalid/communication-settings", "microsoft-online-meetings-work/1", "019acc9a-6200-7000-8000-000000000001")]).Resolve("Microsoft365")!;
        Check("google-review-does-not-block-separate-provider-intent", (await Run(s => s.PrepareAsync(Actor(source.ActorId), source.Academy, microsoft.Provider, 0, WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32)), microsoft.Fingerprint))) is { StatusCode: 201, Code: "OAuthIntentOnlyNotConnected" });
        var unrelated = await Source();
        await using (var db = Open()) Check("google-review-does-not-block-unrelated-academy", unrelated.Academy != source.Academy && await db.MeetingProviderBindings.AnyAsync(x => x.AcademyId == unrelated.Academy && x.Status == "VerificationRequired") && await db.MeetingCredentialRevocations.AnyAsync(x => x.Id == admitted.Material!.Id && x.Status == "ProviderAcknowledged"));
        var consent = await Source(); Check("separate-project-wide-consent-required-no-disconnect", (await Run(s => s.BeginRevocationAsync(Actor(consent.ActorId), consent.Academy, consent.Registration, consent.Permit, 2, true, false))).StatusCode == 400);
        await using (var db = Open()) Check("missing-consent-no-row-and-binding-unchanged", !await db.MeetingCredentialRevocations.AnyAsync(x => x.AcademyId == consent.Academy) && await db.MeetingProviderBindings.AnyAsync(x => x.AcademyId == consent.Academy && x.Version == 2 && x.Status == "VerificationRequired"));
        var race = await Source(); var barrier = new ClaimBarrier(); var starts = await Task.WhenAll(Begin(race, barrier), Begin(race, barrier)); Check("simultaneous-start-exactly-201-409", starts.Select(x => x.StatusCode).Order().SequenceEqual(new[] { 201, 409 }));
        var material = starts.Single(x => x.Material is not null).Material!;
        await using (var db = Open()) Check("one-local-disconnect-and-one-revocation-row", await db.MeetingCredentialRevocations.CountAsync(x => x.AcademyId == race.Academy) == 1 && await db.AuditLogs.CountAsync(x => x.AcademyId == race.Academy && x.Action == "MeetingDisconnectedLocallyRevocationRequired") == 1);
        var finishBarrier = new ClaimBarrier(); var finishes = await Task.WhenAll(Run(s => s.FinishRevocationAsync(Actor(race.ActorId), race.Academy, race.Registration, material, true), finishBarrier), Run(s => s.FinishRevocationAsync(Actor(race.ActorId), race.Academy, race.Registration, material, true), finishBarrier));
        Check("simultaneous-acknowledgements-exactly-200-409", finishes.Select(x => x.StatusCode).Order().SequenceEqual(new[] { 200, 409 }));
        await using (var db = Open()) Check("one-acknowledgement-audit-and-version", await db.MeetingCredentialRevocations.AnyAsync(x => x.Id == material.Id && x.Version == 1 && x.Status == "ProviderAcknowledged") && await db.AuditLogs.CountAsync(x => x.EntityId == material.Id && x.Action == "MeetingRevocationProviderAcknowledged") == 1);
        var fault = await Source(); var failed = false; var calls = 0;
        using (var never = new HttpMessageInvoker(new RefreshTransport(_ => { calls++; return Task.FromResult(Empty()); })))
        { try { await Run(s => new MeetingProviderRevocation(s, never).ExecuteAsync(Actor(fault.ActorId), fault.Academy, fault.Registration, fault.Permit, 2, true, true), new OperationCommitFault()); } catch (InvalidOperationException ex) when (ex.Message == "Synthetic operation commit failure after SQL writes") { failed = true; } }
        await using (var db = Open()) Check("start-audit-fault-no-send-or-disconnect", failed && calls == 0 && !await db.MeetingCredentialRevocations.AnyAsync(x => x.AcademyId == fault.Academy) && await db.MeetingProviderBindings.AnyAsync(x => x.AcademyId == fault.Academy && x.Version == 2 && x.Status == "VerificationRequired"));
        var faultStart = await Begin(fault); Check("start-after-proved-rollback", faultStart.StatusCode == 201); failed = false;
        try { await Run(s => s.FinishRevocationAsync(Actor(fault.ActorId), fault.Academy, fault.Registration, faultStart.Material!, true), new OperationCommitFault()); } catch (InvalidOperationException ex) when (ex.Message == "Synthetic operation commit failure after SQL writes") { failed = true; }
        await using (var db = Open()) Check("completion-audit-fault-retains-started-no-false-ack", failed && await db.MeetingCredentialRevocations.AnyAsync(x => x.Id == faultStart.Material!.Id && x.Version == 0 && x.Status == "Started") && !await db.AuditLogs.AnyAsync(x => x.EntityId == faultStart.Material!.Id && x.Action == "MeetingRevocationProviderAcknowledged"));
        Check("started-project-grant-blocks-reconnect", (await Reconnect(fault)).Code == "ProviderRevocationNeedsReview");
        var previous = clock.Now; clock.Now = clock.Now.AddSeconds(30); Check("interrupted-revoke-recovers-unknown-once", await Run(s => s.RecoverRevocationAsync(fault.Academy, faultStart.Material!.Id)) && !await Run(s => s.RecoverRevocationAsync(fault.Academy, faultStart.Material!.Id)));
        Check("unknown-cannot-resend-or-late-acknowledge", (await Run(s => s.FinishRevocationAsync(Actor(fault.ActorId), fault.Academy, fault.Registration, faultStart.Material!, true))).StatusCode == 409);
        var review = (await Run(s => s.PrepareManualRevocationReviewAsync(Actor(fault.ActorId), fault.Academy, fault.Registration, faultStart.Material!.Id))).Review!;
        Check("unknown-metadata-grant-human-report-stays-unverified", review is not null && (await Run(s => s.ReportManualRevocationAsync(Actor(fault.ActorId), fault.Academy, fault.Registration, review.Id, 1, review.ReviewHash, review.Ticket, true))) is { StatusCode: 200, ProviderVerified: false, Connected: false });
        Check("human-report-does-not-clear-project-reconnect-block", (await Reconnect(fault)).Code == "ProviderRevocationNeedsReview");
        Check("old-profile-cannot-bypass-unresolved-project-grant", (await Reconnect(fault, Reg(false))).Code == "ProviderRevocationNeedsReview"); clock.Now = previous;
        var mixed = await Source(); var mixedStart = await Begin(mixed);
        using var mixedHandler = new RefreshTransport(_ => throw new InvalidOperationException("Old registration must not send metadata revoke")); using var mixedTransport = new HttpMessageInvoker(mixedHandler);
        Check("old-profile-cannot-use-new-profile-revocation-material", (await Run(s => new MeetingProviderRevocation(s, mixedTransport).SendStartedAsync(Actor(mixed.ActorId), mixed.Academy, Reg(false), mixedStart.Material!))).StatusCode == 409 && mixedHandler.Calls == 0);
        var foreign = await Source(); var foreignStart = await Begin(foreign); Check("foreign-academy-cannot-acknowledge", (await Run(s => s.FinishRevocationAsync(Actor(foreign.ActorId), Guid.NewGuid(), foreign.Registration, foreignStart.Material!, true))).StatusCode == 403);
        var changed = await Source(); var changedStart = await Begin(changed);
        using var changedHandler = new RefreshTransport(async _ => { using var scope = factory.Services.CreateScope(); var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(); if (!(await users.UpdateSecurityStampAsync((await users.FindByIdAsync(changed.ActorId.ToString()))!)).Succeeded) throw new InvalidOperationException("Metadata revoke stamp fixture failed"); return Empty(); }); using var changedTransport = new HttpMessageInvoker(changedHandler);
        Check("stamp-change-during-provider-response-prevents-ack", (await Run(s => new MeetingProviderRevocation(s, changedTransport).SendStartedAsync(Actor(changed.ActorId), changed.Academy, changed.Registration, changedStart.Material!))).StatusCode == 409);
        var denied = await Source(); var deniedStart = await Begin(denied); using var unavailableHandler = new RefreshTransport(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.TooManyRequests) { Content = new ByteArrayContent([]) })); using var unavailableTransport = new HttpMessageInvoker(unavailableHandler);
        Check("provider-throttling-is-unknown-not-auto-retried", (await Run(s => new MeetingProviderRevocation(s, unavailableTransport).SendStartedAsync(Actor(denied.ActorId), denied.Academy, denied.Registration, deniedStart.Material!))).StatusCode == 409 && (await Run(s => new MeetingProviderRevocation(s, unavailableTransport).SendStartedAsync(Actor(denied.ActorId), denied.Academy, denied.Registration, deniedStart.Material!))).StatusCode == 409 && unavailableHandler.Calls == 1);
        if (checks.Count != 34) throw new InvalidOperationException("Google metadata revocation case count mismatch " + checks.Count);
        var sources = new[] { "apps/api/Infrastructure/Meetings/MeetingRevocationStore.cs", "apps/api/Infrastructure/Meetings/MeetingProviderRevocation.cs", "apps/api/Infrastructure/Meetings/MeetingOAuthFlowProfile.cs", "apps/api/Infrastructure/Meetings/MeetingOAuthAttemptStore.cs", "tests/AcademyDesk.Api.Tests/MeetingGoogleRevocationTests.cs", "QA/tools/SqlHarness/MeetingGoogleRevocationRegression.cs" }.ToDictionary(p => p, p => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(p))));
        var path = Path.Combine("QA", "EVIDENCE", "meeting-google-revocation-sql-" + manifest.RunId.ToString("N")); Directory.CreateDirectory(path);
        await File.WriteAllTextAsync(Path.Combine(path, "result.json"), JsonSerializer.Serialize(new { checks, sources, scope = "Owned SQL/Identity with synthetic Google revocation acknowledgement only; no real project consent/revocation/grant reconciliation/Connected/public UI/release acceptance" }, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine("MEETING GOOGLE REVOCATION PASS:34 SQL/Identity cases; acknowledgement does not clear project reconciliation safeguards.");
    }
}
