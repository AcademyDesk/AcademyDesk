using System.Data.Common;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AcademyDesk.Api.Data;
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
    private sealed class SecondAccountAuditFault : DbTransactionInterceptor
    {
        private int commits;
        public override ValueTask<InterceptionResult> TransactionCommittingAsync(DbTransaction transaction, TransactionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default)
        {
            if (Interlocked.Increment(ref commits) == 2) throw new InvalidOperationException("Synthetic second account audit failure");
            return ValueTask.FromResult(result);
        }
    }
    private static async Task VerifyMeetingOAuthAccountsAsync(QaApiFactory factory, QaRunManifest manifest)
    {
        string connection; Guid academy;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); connection = db.Database.GetConnectionString()!;
            academy = await db.Academies.Where(x => x.Name == "Synthetic Academy A").Select(x => x.Id).SingleAsync();
        }
        var target = new SqlConnectionStringBuilder(connection);
        if (target.DataSource != manifest.SqlServer || target.InitialCatalog != manifest.Database || target.UserID != manifest.RuntimeLogin) throw new InvalidOperationException("Account checks require exact owned target");
        var actorId = await CreateAccessActorAsync(factory, "qa-oauth-account@example.invalid", "AcademyAdmin", academy);
        var actor = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, actorId.ToString()) }, "synthetic-account"));
        var clock = new OperationClock(); var keys = factory.Services.GetRequiredService<IDataProtectionProvider>(); const string access = "synthetic-account-access";
        AcademyDeskDbContext Open(IInterceptor? interceptor = null) { var options = new DbContextOptionsBuilder<AcademyDeskDbContext>().UseSqlServer(connection); if (interceptor is not null) options.AddInterceptors(interceptor); return new(options.Options); }
        async Task<T> Run<T>(Func<MeetingOAuthAttemptStore, Task<T>> action, IInterceptor? interceptor = null)
        {
            using var scope = factory.Services.CreateScope(); await using var db = Open(interceptor); var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            return await action(new(db, users, new(db, users, clock), keys, clock));
        }
        MeetingOAuthRegistration Registration(string provider = "Zoom")
        {
            var profile = provider switch { "GoogleWorkspace" => "google-calendar-owned-web/1", "Microsoft365" => "microsoft-online-meetings-work/1", _ => "zoom-public-pkce/1" };
            return MeetingOAuthRegistrationRegistry.Create("https://api.academy.invalid", "https://app.academy.invalid", [new(provider, 1, "synthetic-client", "https://api.academy.invalid" + MeetingOAuthRegistrationRegistry.CallbackPath(provider),
                "https://app.academy.invalid/communication-settings", profile, provider == "Microsoft365" ? "019acc9a-6200-7000-8000-000000000001" : null)]).Resolve(provider)!;
        }
        async Task<(MeetingOAuthExchangePermit Permit, int Version)> Quarantine(MeetingOAuthRegistration registration)
        {
            int version; await using (var db = Open()) version = await db.MeetingProviderBindings.Where(x => x.AcademyId == academy && x.Provider == registration.Provider).Select(x => x.Version).SingleAsync();
            return await Run(async store =>
            {
                var browser = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32)); var prepared = await store.PrepareAsync(actor, academy, registration.Provider, version, browser, registration.Fingerprint);
                if (prepared.StatusCode != 201 || (await store.CaptureAsync(academy, registration.Provider, prepared.Material!.State, browser, registration.Fingerprint, "synthetic-account-code", false)).StatusCode != 202) throw new InvalidOperationException("Account fixture capture failed");
                var claim = await store.ResumeAsync(actor, academy, registration.Provider, prepared.Material.State, browser, registration.Fingerprint);
                var start = await store.BeginExchangeAsync(actor, academy, registration, claim);
                if (start.StatusCode != 201 || (await store.FinishExchangeAsync(actor, academy, registration, start.Permit!, new(200, "TokensReceivedAccountUnverified", Tokens: new(access, "synthetic-account-refresh", clock.Now.AddHours(1), registration.Flow!.RequiredTokenScopes)))).StatusCode != 200) throw new InvalidOperationException("Account fixture quarantine failed");
                return (start.Permit!, version + 2);
            });
        }
        byte[] Body(string provider) => JsonSerializer.SerializeToUtf8Bytes(provider switch
        {
            "GoogleWorkspace" => (object)new { sub = "synthetic-google-user", email_verified = true },
            "Microsoft365" => new { id = "019acc9a-6200-7000-8000-000000000002" },
            _ => new { id = "synthetic-zoom-user", account_id = "synthetic-zoom-account", status = "active" }
        });
        async Task<MeetingOAuthAccountOutcome> Probe(MeetingOAuthRegistration registration, MeetingOAuthExchangePermit permit, Func<Task>? during = null, IInterceptor? interceptor = null)
        {
            using var transport = new HttpMessageInvoker(new ExchangeTransport(during ?? (() => Task.CompletedTask), Body(registration.Provider)));
            return await Run(s => new MeetingOAuthAccountProbe(s, transport, clock).InspectAsync(actor, academy, registration, permit), interceptor);
        }
        var checks = new List<string>(); void Check(string label, bool passed) { if (!passed) throw new InvalidOperationException("MEETING ACCOUNT " + label); checks.Add(label); Console.WriteLine("MEETING ACCOUNT CASE " + label + " PASS"); }
        foreach (var provider in new[] { "GoogleWorkspace", "Microsoft365", "Zoom" })
        {
            var registration = Registration(provider); var (permit, version) = await Quarantine(registration); string encrypted;
            await using (var db = Open()) encrypted = (await db.MeetingOAuthExchanges.SingleAsync(x => x.Id == permit.Id)).ProtectedEnvelope;
            var observation = await Probe(registration, permit, async () =>
            {
                await using var db = Open(); if (await db.AuditLogs.CountAsync(x => x.EntityId == permit.Id && x.Action == "MeetingOAuthQuarantinedTokensRead") != 1) throw new InvalidOperationException("Provider request before required read audit");
            });
            Check(provider + "-account-observed-not-connected", observation.StatusCode == 200 && observation.Observation is not null && !observation.Connected && !observation.CanExecute);
            Check(provider + "-identity-private", !JsonSerializer.Serialize(observation).Contains("synthetic-") && JsonSerializer.Serialize(observation.Observation) == "{}");
            await using (var db = Open()) Check(provider + "-two-read-audits-quarantine-unchanged", await db.AuditLogs.CountAsync(x => x.EntityId == permit.Id && x.Action == "MeetingOAuthQuarantinedTokensRead") == 2 && (await db.MeetingOAuthExchanges.SingleAsync(x => x.Id == permit.Id)).ProtectedEnvelope == encrypted);
            Check(provider + "-unconfirmed-disconnect-denied", (await Run(s => s.DisconnectLocallyAsync(actor, academy, provider, version, false))).StatusCode == 400);
            Check(provider + "-stale-disconnect-denied", (await Run(s => s.DisconnectLocallyAsync(actor, academy, provider, version - 1, true))).StatusCode == 409);
            var disconnected = await Run(s => s.DisconnectLocallyAsync(actor, academy, provider, version, true));
            Check(provider + "-local-disconnect-not-provider-revocation", disconnected.StatusCode == 200 && disconnected.Code == "DisconnectedLocallyProviderRevocationRequired");
            Check(provider + "-old-credentials-fenced", (await Run(s => s.ReadQuarantinedTokensAsync(actor, academy, registration, permit))).StatusCode == 409);
            await using (var db = Open()) Check(provider + "-ciphertext-retained-generation-advanced", (await db.MeetingOAuthExchanges.SingleAsync(x => x.Id == permit.Id)).ProtectedEnvelope == encrypted &&
                await db.MeetingProviderBindings.AnyAsync(x => x.AcademyId == academy && x.Provider == provider && x.Status == "Disconnected" && x.Version == version + 1) &&
                await db.AuditLogs.CountAsync(x => x.ActorUserId == actorId && x.Action == "MeetingDisconnectedLocallyRevocationRequired" && x.EntityId == db.MeetingProviderBindings.Where(b => b.AcademyId == academy && b.Provider == provider).Select(b => b.Id).Single()) == 1);
        }
        var reg = Registration(); var race = await Quarantine(reg); var barrier = new ClaimBarrier();
        var disconnects = await Task.WhenAll(Run(s => s.DisconnectLocallyAsync(actor, academy, "Zoom", race.Version, true), barrier), Run(s => s.DisconnectLocallyAsync(actor, academy, "Zoom", race.Version, true), barrier));
        Check("concurrent-disconnect-one-winner", disconnects.Select(x => x.StatusCode).Order().SequenceEqual(new[] { 200, 409 }));
        await using (var db = Open()) Check("disconnect-race-one-version-one-audit", await db.MeetingProviderBindings.AnyAsync(x => x.AcademyId == academy && x.Provider == "Zoom" && x.Version == race.Version + 1) && await db.AuditLogs.CountAsync(x => x.ActorUserId == actorId && x.Action == "MeetingDisconnectedLocallyRevocationRequired") == 4);
        var fault = await Quarantine(reg); var failed = false;
        try { await Run(s => s.DisconnectLocallyAsync(actor, academy, "Zoom", fault.Version, true), new OperationCommitFault()); } catch (InvalidOperationException ex) when (ex.Message == "Synthetic operation commit failure after SQL writes") { failed = true; }
        await using (var db = Open()) Check("disconnect-commit-fault-rollback", failed && await db.MeetingProviderBindings.AnyAsync(x => x.AcademyId == academy && x.Provider == "Zoom" && x.Version == fault.Version && x.Status == "VerificationRequired") && await db.AuditLogs.CountAsync(x => x.ActorUserId == actorId && x.Action == "MeetingDisconnectedLocallyRevocationRequired") == 4);
        var calls = 0; failed = false;
        try { await Probe(reg, fault.Permit, () => { calls++; return Task.CompletedTask; }, new OperationCommitFault()); } catch (InvalidOperationException ex) when (ex.Message == "Synthetic operation commit failure after SQL writes") { failed = true; }
        await using (var db = Open()) Check("first-read-audit-fault-no-provider-call", failed && calls == 0 && !await db.AuditLogs.AnyAsync(x => x.EntityId == fault.Permit.Id && x.Action == "MeetingOAuthQuarantinedTokensRead"));
        failed = false;
        try { await Probe(reg, fault.Permit, () => { calls++; return Task.CompletedTask; }, new SecondAccountAuditFault()); } catch (InvalidOperationException ex) when (ex.Message == "Synthetic second account audit failure") { failed = true; }
        await using (var db = Open()) Check("second-read-audit-fault-no-identity-release", failed && calls == 1 && await db.AuditLogs.CountAsync(x => x.EntityId == fault.Permit.Id && x.Action == "MeetingOAuthQuarantinedTokensRead") == 1);
        var changed = await Probe(reg, fault.Permit, async () => { if ((await Run(s => s.DisconnectLocallyAsync(actor, academy, "Zoom", fault.Version, true))).StatusCode != 200) throw new InvalidOperationException("In-flight disconnect fixture failed"); });
        Check("disconnect-during-response-no-identity-release", changed.StatusCode == 409 && changed.Observation is null);
        var role = await Quarantine(reg);
        var revoked = await Probe(reg, role.Permit, async () =>
        {
            using var scope = factory.Services.CreateScope(); var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(); var user = (await users.FindByIdAsync(actorId.ToString()))!;
            if (!(await users.RemoveFromRoleAsync(user, "AcademyAdmin")).Succeeded) throw new InvalidOperationException("Account role revocation fixture failed");
        });
        Check("role-revoked-during-response-no-identity-release", revoked.StatusCode == 403 && revoked.Observation is null);
        if (checks.Count != 31) throw new InvalidOperationException("Account SQL case count mismatch " + checks.Count);
        var sources = new[] { "apps/api/Infrastructure/Meetings/MeetingOAuthAccountProbe.cs", "apps/api/Infrastructure/Meetings/MeetingOAuthDisconnect.cs", "apps/api/Infrastructure/Meetings/MeetingOAuthExchangeStore.cs", "apps/api/Infrastructure/Meetings/MeetingOAuthAttemptStore.cs", "apps/api/Infrastructure/Meetings/MeetingReadinessService.cs", "apps/api/Infrastructure/Meetings/MeetingOAuthCodeExchange.cs", "QA/tools/SqlHarness/MeetingOAuthAccountRegression.cs" }.ToDictionary(p => p, p => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(p))));
        var evidence = Path.Combine("QA", "EVIDENCE", "meeting-oauth-account-sql-" + manifest.RunId.ToString("N")); Directory.CreateDirectory(evidence);
        await File.WriteAllTextAsync(Path.Combine(evidence, "result.json"), JsonSerializer.Serialize(new { checks, sources, scope = "Owned real SQL/Identity with fake provider GET only; no live provider, academy ownership, entitlement, external revocation or Connected acceptance" }, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine("MEETING ACCOUNT PASS:31SQL/Identity cases; fake provider only, local disconnect not provider revocation.");
    }
}
