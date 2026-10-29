using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Domain.Identity;
using AcademyDesk.Api.Infrastructure.Meetings;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

internal static partial class SqlHarnessEntryPoint
{
    private sealed record ProtectedMeetingSource(MeetingOAuthRegistration Registration, MeetingOAuthExchangePermit Original, MeetingOAuthExchange OldRow);
    private static async Task VerifyMeetingProtectedKeysAsync(QaApiFactory factory, QaRunManifest manifest)
    {
        string connection; using (var scope = factory.Services.CreateScope()) connection = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>().Database.GetConnectionString()!;
        var target = new SqlConnectionStringBuilder(connection);
        if (target.DataSource != manifest.SqlServer || target.InitialCatalog != manifest.Database || target.UserID != manifest.RuntimeLogin) throw new InvalidOperationException("Protected meeting keys require exact owned SQL target");
        AcademyDeskDbContext Open(IInterceptor? fault = null) { var b = new DbContextOptionsBuilder<AcademyDeskDbContext>().UseSqlServer(connection); if (fault is not null) b.AddInterceptors(fault); return new(b.Options); }
        var tenant = new Academy { Name = "Synthetic encrypted meeting storage " + Guid.NewGuid().ToString("N"), EnabledModulesJson = "[\"Core\",\"Engagement\"]" };
        await using (var db = Open()) { db.Add(tenant); await db.SaveChangesAsync(); }
        var actorId = await CreateAccessActorAsync(factory, "qa-meeting-keys-" + Guid.NewGuid().ToString("N") + "@example.invalid", "AcademyAdmin", tenant.Id);
        var actor = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, actorId.ToString()) }, "synthetic-meeting-keys")); var clock = new OperationClock();
        async Task<T> Run<T>(ServiceProvider keyServices, Func<MeetingOAuthAttemptStore, Task<T>> action, IInterceptor? fault = null)
        { using var scope = factory.Services.CreateScope(); await using var db = Open(fault); var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(); return await action(new(db, users, new(db, users, clock), keyServices.GetRequiredService<IDataProtectionProvider>(), clock)); }
        var registrations = MeetingOAuthRegistrationRegistry.Create("https://api.academy.invalid", "https://app.academy.invalid", new[] {
            new MeetingOAuthRegistrationInput("GoogleWorkspace", 1, "synthetic-key-client", "https://api.academy.invalid/api/meetings/oauth/GoogleWorkspace/callback", "https://app.academy.invalid/communication-settings", MeetingOAuthFlowProfile.GoogleMetadataProfile),
            new MeetingOAuthRegistrationInput("Microsoft365", 1, "019acc9a-6200-7000-8000-000000000001", "https://api.academy.invalid/api/meetings/oauth/Microsoft365/callback", "https://app.academy.invalid/communication-settings", MeetingOAuthFlowProfile.MicrosoftIdentityProfile, "019acc9a-6200-7000-8000-000000000002"),
            new MeetingOAuthRegistrationInput("Zoom", 1, "synthetic-key-client", "https://api.academy.invalid/api/meetings/oauth/Zoom/callback", "https://app.academy.invalid/communication-settings", MeetingOAuthFlowProfile.ZoomConfidentialProfile) });
        var sources = new List<ProtectedMeetingSource>(); var current = new Dictionary<string, MeetingOAuthExchangePermit>();
        var checks = new List<string>(); void Check(string name, bool pass) { if (!pass) throw new InvalidOperationException("MEETING PROTECTED KEYS " + name); checks.Add(name); Console.WriteLine("MEETING PROTECTED KEYS CASE " + name + " PASS"); }
        Guid KeyId(string payload) => new(WebEncoders.Base64UrlDecode(payload).AsSpan(4, 16));
        string Access(string provider, bool rotated = false) => "synthetic-key-access-" + (rotated ? "rotated-" : "") + provider;
        string Refresh(string provider, bool rotated = false) => "synthetic-key-refresh-" + (rotated ? "rotated-" : "") + provider;
        MeetingOAuthExchangeOutcome Tokens(MeetingOAuthRegistration reg, bool rotated = false) => new(200, "TokensReceivedAccountUnverified", Tokens: new(Access(reg.Provider, rotated), Refresh(reg.Provider, rotated), clock.Now.AddHours(1), reg.Flow!.RequiredTokenScopes));
        using var keys = new QaMeetingKeyRing();
        using (var writer = keys.Provider())
        foreach (var provider in new[] { "GoogleWorkspace", "Microsoft365", "Zoom" })
        {
            var reg = registrations.Resolve(provider)!;
            var permit = await Run(writer, async store => {
                var browser = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32)); var intent = await store.PrepareAsync(actor, tenant.Id, provider, 0, browser, reg.Fingerprint);
                if (intent.StatusCode != 201 || (await store.CaptureAsync(tenant.Id, provider, intent.Material!.State, browser, reg.Fingerprint, "synthetic-key-code", false)).StatusCode != 202) throw new InvalidOperationException("Protected key fixture capture failed");
                var claim = await store.ResumeAsync(actor, tenant.Id, provider, intent.Material.State, browser, reg.Fingerprint); var started = await store.BeginExchangeAsync(actor, tenant.Id, reg, claim);
                if (started.Permit is null || (await store.FinishExchangeAsync(actor, tenant.Id, reg, started.Permit, Tokens(reg))).StatusCode != 200) throw new InvalidOperationException("Protected key fixture quarantine failed"); return started.Permit;
            });
            await using var db = Open(); var row = await db.MeetingOAuthExchanges.AsNoTracking().SingleAsync(x => x.Id == permit.Id);
            sources.Add(new(reg, permit, row));
            Check(provider + "-quarantined-under-persisted-encrypted-key", row.Status == "TokensQuarantined" && row.Version == 1 && !row.ProtectedEnvelope.Contains(Access(provider)) && !row.ProtectedEnvelope.Contains(Refresh(provider)));
        }
        var stale = keys.Snapshot("old-snapshot");
        using (var restart = keys.Provider())
        foreach (var source in sources)
        {
            var result = await Run(restart, s => s.ReadQuarantinedTokensAsync(actor, tenant.Id, source.Registration, source.Original));
            Check(source.Registration.Provider + "-fresh-provider-recovers-SQL-credential", result.StatusCode == 200 && result.Tokens?.RefreshToken == Refresh(source.Registration.Provider) && !JsonSerializer.Serialize(result).Contains(Refresh(source.Registration.Provider)));
        }
        var replacement = keys.Rotate();
        using (var rotated = keys.Provider(rotated: true))
        foreach (var source in sources)
        {
            var reg = source.Registration;
            var result = await Run(rotated, async store => {
                var start = await store.BeginRefreshAsync(actor, tenant.Id, reg, source.Original, 1);
                if (start.Material is null) throw new InvalidOperationException("Old encrypted meeting credential not recoverable for synthetic rotation");
                return await store.FinishRefreshAsync(actor, tenant.Id, reg, start.Material, Tokens(reg, true));
            });
            if (result.Permit is not null) current.Add(reg.Provider, result.Permit);
            await using var db = Open(); var row = await db.MeetingOAuthExchanges.SingleAsync(x => x.Id == source.Original.Id);
            Check(reg.Provider + "-refresh-reencrypts-with-actual-replacement-key", result is { StatusCode: 200, Connected: false, CanRetry: false, Permit: not null } && row.Version == 3 && KeyId(row.ProtectedEnvelope) == replacement && KeyId(source.OldRow.ProtectedEnvelope) != replacement && await db.MeetingProviderBindings.AnyAsync(x => x.Id == row.BindingId && x.Status == "VerificationRequired" && x.Version == 3));
        }
        Check("both-generations-encrypted-at-rest-no-plaintext-key", keys.EncryptedFilesOnly());
        var backup = keys.Snapshot("current-backup"); var restored = keys.Snapshot("restored", backup);
        using (var reader = keys.Provider(rotated: true, directory: restored))
        foreach (var source in sources)
        {
            var provider = source.Registration.Provider; var result = await Run(reader, s => s.ReadQuarantinedTokensAsync(actor, tenant.Id, source.Registration, current[provider]));
            Check(provider + "-restored-keys-recover-current-SQL-credential", result.StatusCode == 200 && result.Tokens?.AccessToken == Access(provider, true) && result.Tokens.RefreshToken == Refresh(provider, true));
            Check(provider + "-restore-does-not-resurrect-old-permit", (await Run(reader, s => s.ReadQuarantinedTokensAsync(actor, tenant.Id, source.Registration, source.Original))).StatusCode == 409);
        }
        using (var reader = keys.Provider(rotated: true, keepOriginal: false, directory: restored))
        foreach (var source in sources)
        {
            var oldCannotRecover = false;
            try { reader.GetDataProtector("academy.meeting-oauth-exchange.v1", tenant.Id.ToString("D"), actorId.ToString("D"), source.Registration.Provider, source.OldRow.AttemptId.ToString("D"), source.OldRow.Id.ToString("D")).Unprotect(source.OldRow.ProtectedEnvelope); }
            catch (CryptographicException) { oldCannotRecover = true; }
            Check(source.Registration.Provider + "-retired-certificate-still-required-for-old-history", oldCannotRecover && (await Run(reader, s => s.ReadQuarantinedTokensAsync(actor, tenant.Id, source.Registration, current[source.Registration.Provider]))).StatusCode == 200);
        }
        async Task<string> Snapshot()
        {
            await using var db = Open();
            return JsonSerializer.Serialize(new { bindings = await db.MeetingProviderBindings.AsNoTracking().Where(x => x.AcademyId == tenant.Id).OrderBy(x => x.Id).ToArrayAsync(),
                attempts = await db.MeetingOAuthAttempts.AsNoTracking().Where(x => x.AcademyId == tenant.Id).OrderBy(x => x.Id).ToArrayAsync(),
                callbacks = await db.MeetingOAuthCallbacks.AsNoTracking().Where(x => x.AcademyId == tenant.Id).OrderBy(x => x.Id).ToArrayAsync(),
                exchanges = await db.MeetingOAuthExchanges.AsNoTracking().Where(x => x.AcademyId == tenant.Id).OrderBy(x => x.Id).ToArrayAsync(),
                audits = await db.AuditLogs.AsNoTracking().Where(x => x.AcademyId == tenant.Id).OrderBy(x => x.Id).ToArrayAsync() });
        }
        var before = await Snapshot();
        foreach (var mode in new[] { "wrong-environment", "missing-ring", "stale-backup" })
        {
            using var reader = keys.Provider(rotated: true, directory: mode == "missing-ring" ? keys.Empty("empty") : mode == "stale-backup" ? stale : restored, environment: mode == "wrong-environment" ? "SyntheticOtherEnvironment" : "Testing");
            var denied = true;
            foreach (var source in sources)
            {
                var result = await Run(reader, s => s.ReadQuarantinedTokensAsync(actor, tenant.Id, source.Registration, current[source.Registration.Provider]));
                var refresh = await Run(reader, s => s.BeginRefreshAsync(actor, tenant.Id, source.Registration, current[source.Registration.Provider], 3));
                denied &= result.StatusCode == 409 && result.Tokens is null && refresh is { StatusCode: 409, Material: null, Permit: null };
            }
            Check(mode + "-fails-closed-no-SQL-or-audit-writes", denied && await Snapshot() == before);
        }
        var faulted = false;
        using (var reader = keys.Provider(rotated: true, directory: restored))
        try { await Run(reader, s => s.ReadQuarantinedTokensAsync(actor, tenant.Id, sources[0].Registration, current[sources[0].Registration.Provider]), new OperationCommitFault()); }
        catch (InvalidOperationException ex) when (ex.Message == "Synthetic operation commit failure after SQL writes") { faulted = true; }
        Check("restored-credential-audit-fault-rolls-back-without-release", faulted && await Snapshot() == before);
        using (var revoker = keys.Provider(rotated: true, directory: restored)) revoker.GetRequiredService<IKeyManager>().RevokeAllKeys(DateTimeOffset.UtcNow.AddSeconds(1), "Synthetic run-owned meeting key test only");
        using (var reader = keys.Provider(rotated: true, directory: restored))
        {
            var denied = true;
            foreach (var source in sources) { var result = await Run(reader, s => s.ReadQuarantinedTokensAsync(actor, tenant.Id, source.Registration, current[source.Registration.Provider])); denied &= result.StatusCode == 409 && result.Tokens is null; }
            Check("restarted-revoked-key-provider-denies-all-credentials-no-writes", denied && await Snapshot() == before);
            foreach (var source in sources)
            {
                var result = await Run(reader, s => s.DisconnectLocallyAsync(actor, tenant.Id, source.Registration.Provider, 3, true));
                Check(source.Registration.Provider + "-local-disconnect-still-available-without-decryption", result.StatusCode == 200 && (await Run(reader, s => s.ReadQuarantinedTokensAsync(actor, tenant.Id, source.Registration, current[source.Registration.Provider]))).StatusCode == 409);
            }
        }
        await using (var db = Open()) Check("encrypted-history-retained-and-no-connected-binding", await db.MeetingOAuthExchanges.CountAsync(x => x.AcademyId == tenant.Id) == 3 && await db.MeetingOAuthAttempts.CountAsync(x => x.AcademyId == tenant.Id) == 3 && await db.MeetingOAuthCallbacks.CountAsync(x => x.AcademyId == tenant.Id) == 3 && await db.MeetingProviderBindings.CountAsync(x => x.AcademyId == tenant.Id && x.Status == "Disconnected" && x.Version == 4) == 3 && !await db.MeetingCredentialRevocations.AnyAsync(x => x.AcademyId == tenant.Id));
        keys.Dispose(); Check("owned-synthetic-certificates-and-key-files-removed", !keys.Exists);
        if (checks.Count != 29) throw new InvalidOperationException("Protected meeting key case count mismatch " + checks.Count);
        var hashes = new[] { "apps/api/Infrastructure/ProtectedKeyRingRegistration.cs", "apps/api/Infrastructure/Meetings/MeetingOAuthExchangeStore.cs", "apps/api/Infrastructure/Meetings/MeetingRefreshStore.cs", "apps/api/Infrastructure/Meetings/MeetingOAuthDisconnect.cs", "tests/AcademyDesk.Api.Tests/Infrastructure/QaMeetingKeyRing.cs", "tests/AcademyDesk.Api.Tests/MeetingProtectedKeysTests.cs", "QA/tools/SqlHarness/MeetingProtectedKeysRegression.cs", "QA/tools/SqlHarness/SqlHarness.csproj" }.ToDictionary(p => p, p => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(p))));
        var path = Path.Combine("QA", "EVIDENCE", "meeting-protected-keys-sql-" + manifest.RunId.ToString("N")); Directory.CreateDirectory(path);
        await File.WriteAllTextAsync(Path.Combine(path, "result.json"), JsonSerializer.Serialize(new { checks, sources = hashes, scope = "Real owned SQL/Identity and real isolated certificate-encrypted key files, fresh in-process protection providers; synthetic OAuth tokens only. Not container restart, production backup/ACL/replica/retention/provider/release qualification" }, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine("MEETING PROTECTED KEYS PASS:29 SQL/Identity cases; isolated encrypted restart/rotation/restore and fail-closed recovery only.");
    }
}
