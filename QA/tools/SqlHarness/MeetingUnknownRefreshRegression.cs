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
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;

internal static partial class SqlHarnessEntryPoint
{
    private static async Task VerifyMeetingUnknownRefreshMigrationAsync(AcademyDeskDbContext db)
    {
        var migrations = db.Database.GetMigrations().ToArray(); var latest = migrations.Single(x => x.EndsWith("_MeetingUnknownRefreshRetirement", StringComparison.Ordinal));
        var previous = migrations[Array.IndexOf(migrations, latest) - 1]; var migrator = db.GetService<IMigrator>(); await migrator.MigrateAsync(latest);
        var binding = new MeetingProviderBinding { AcademyId = Guid.NewGuid(), Provider = "GoogleWorkspace", Generation = 1, Version = 1 };
        var attempt = new MeetingOAuthAttempt { AcademyId = binding.AcademyId, BindingId = binding.Id, ActorUserId = Guid.NewGuid(), Provider = binding.Provider, Generation = 1,
            StateHash = new string('D', 64), ProtectedEnvelope = "synthetic-migration-only", ExpiresAtUtc = DateTime.UtcNow.AddMinutes(5) };
        var exchange = new MeetingOAuthExchange { AcademyId = binding.AcademyId, ActorUserId = attempt.ActorUserId, AttemptId = attempt.Id, BindingId = binding.Id,
            Provider = binding.Provider, Generation = 1, BindingVersion = 1, Fence = Guid.NewGuid(), RegistrationFingerprint = new string('E', 64), DeadlineUtc = DateTime.UtcNow.AddSeconds(30),
            Status = "RefreshAbandoned", Version = 4, ProtectedEnvelope = "synthetic-migration-only" };
        db.AddRange(binding, attempt, exchange); await db.SaveChangesAsync(); var blocked = false;
        try { await migrator.MigrateAsync(previous); } catch (SqlException ex) when (ex.Number == 51000) { blocked = true; }
        if (!blocked || !await db.MeetingOAuthExchanges.AnyAsync(x => x.Id == exchange.Id && x.Status == "RefreshAbandoned")) throw new InvalidOperationException("Unknown refresh downgrade lost evidence");
        exchange.Status = "Started"; exchange.Version = 0;
        var row = new MeetingCredentialRevocation { AcademyId = binding.AcademyId, ActorUserId = attempt.ActorUserId, ExchangeId = exchange.Id, BindingId = binding.Id, Provider = binding.Provider, Generation = 1,
            BindingVersion = 1, Fence = Guid.NewGuid(), RegistrationFingerprint = exchange.RegistrationFingerprint, ProtectedEnvelope = "synthetic-migration-only", DeadlineUtc = DateTime.UtcNow.AddSeconds(30), Status = "ManualReportedUnverified", Version = 1 };
        db.Add(row); await db.SaveChangesAsync(); blocked = false;
        try { await migrator.MigrateAsync(previous); } catch (SqlException ex) when (ex.Number == 51000) { blocked = true; }
        if (!blocked || !await db.MeetingCredentialRevocations.AnyAsync(x => x.Id == row.Id)) throw new InvalidOperationException("Unknown family report downgrade lost evidence");
        db.Remove(row); await db.SaveChangesAsync(); db.Remove(exchange); await db.SaveChangesAsync(); db.Remove(attempt); await db.SaveChangesAsync(); db.Remove(binding); await db.SaveChangesAsync(); // Exact owned probes only.
        await migrator.MigrateAsync(previous); await migrator.MigrateAsync(latest);
        Console.WriteLine("MEETING UNKNOWN REFRESH MIGRATION PASS: both populated-down refusals, exact probe removal and empty down/up.");
    }
    private static async Task VerifyMeetingUnknownRefreshesAsync(QaApiFactory factory, QaRunManifest manifest)
    {
        Guid foreign; string connection;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); connection = db.Database.GetConnectionString()!;
            foreign = await db.Academies.Where(x => x.Name == "Synthetic Academy A").Select(x => x.Id).SingleAsync();
        }
        var target = new SqlConnectionStringBuilder(connection);
        if (target.DataSource != manifest.SqlServer || target.InitialCatalog != manifest.Database || target.UserID != manifest.RuntimeLogin) throw new InvalidOperationException("Unknown refresh requires exact owned target");
        AcademyDeskDbContext Open(IInterceptor? interceptor = null) { var options = new DbContextOptionsBuilder<AcademyDeskDbContext>().UseSqlServer(connection); if (interceptor is not null) options.AddInterceptors(interceptor); return new(options.Options); }
        var tenant = new Academy { Name = "Synthetic Unknown Refresh Academy", EnabledModulesJson = "[\"Core\",\"Engagement\"]" };
        await using (var db = Open()) { db.Add(tenant); await db.SaveChangesAsync(); }
        var academy = tenant.Id; var actorId = await CreateAccessActorAsync(factory, "qa-unknown-refresh@example.invalid", "AcademyAdmin", academy);
        var otherId = await CreateAccessActorAsync(factory, "qa-unknown-refresh-other@example.invalid", "AcademyAdmin", academy);
        ClaimsPrincipal Actor(Guid id) => new(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, id.ToString()) }, "synthetic-unknown-refresh"));
        var actor = Actor(actorId); var clock = new OperationClock(); var keys = factory.Services.GetRequiredService<IDataProtectionProvider>();
        async Task<T> Run<T>(Func<MeetingOAuthAttemptStore, Task<T>> action, IInterceptor? interceptor = null)
        {
            using var scope = factory.Services.CreateScope(); await using var db = Open(interceptor); var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            return await action(new(db, users, new(db, users, clock), keys, clock));
        }
        MeetingOAuthRegistration Registration(string provider = "Microsoft365") => MeetingOAuthRegistrationRegistry.Create("https://api.academy.invalid", "https://app.academy.invalid",
            [new(provider, 1, "synthetic-client", "https://api.academy.invalid" + MeetingOAuthRegistrationRegistry.CallbackPath(provider), "https://app.academy.invalid/communication-settings",
                provider == "GoogleWorkspace" ? "google-calendar-owned-web/1" : "microsoft-online-meetings-work/1", provider == "Microsoft365" ? "019acc9a-6200-7000-8000-000000000001" : null)]).Resolve(provider)!;
        async Task<(MeetingOAuthExchangePermit Permit, MeetingRefreshMaterial Material, int BindingVersion, int Generation)> Source(MeetingOAuthRegistration reg, bool pending = false)
        {
            int version; await using (var db = Open()) version = await db.MeetingProviderBindings.Where(x => x.AcademyId == academy && x.Provider == reg.Provider).Select(x => (int?)x.Version).SingleOrDefaultAsync() ?? 0;
            return await Run(async s =>
            {
                var browser = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32)); var intent = await s.PrepareAsync(actor, academy, reg.Provider, version, browser, reg.Fingerprint);
                if (intent.StatusCode != 201 || (await s.CaptureAsync(academy, reg.Provider, intent.Material!.State, browser, reg.Fingerprint, "synthetic-unknown-code", false)).StatusCode != 202) throw new InvalidOperationException("Unknown refresh source capture failed");
                var claim = await s.ResumeAsync(actor, academy, reg.Provider, intent.Material.State, browser, reg.Fingerprint); var exchange = await s.BeginExchangeAsync(actor, academy, reg, claim);
                if ((await s.FinishExchangeAsync(actor, academy, reg, exchange.Permit!, new(200, "TokensReceivedAccountUnverified", Tokens: new("synthetic-unknown-access", "synthetic-unknown-refresh", clock.Now.AddHours(1), reg.Flow!.RequiredTokenScopes)))).StatusCode != 200) throw new InvalidOperationException("Unknown refresh quarantine failed");
                var start = await s.BeginRefreshAsync(actor, academy, reg, exchange.Permit!, 1); if (start.StatusCode != 201) throw new InvalidOperationException("Unknown refresh admission failed");
                var permit = new MeetingOAuthExchangePermit(start.Material!.Id, start.Material.Fence);
                if (!pending && (await s.FinishRefreshAsync(actor, academy, reg, start.Material, new(502, "RefreshOutcomeUnknown", true))).StatusCode != 409) throw new InvalidOperationException("Unknown refresh outcome fixture failed");
                await using var db = Open(); var binding = await db.MeetingProviderBindings.SingleAsync(x => x.AcademyId == academy && x.Provider == reg.Provider);
                return (permit, start.Material, binding.Version, binding.Generation);
            });
        }
        var checks = new List<string>(); void Check(string label, bool passed) { if (!passed) throw new InvalidOperationException("MEETING UNKNOWN REFRESH " + label); checks.Add(label); Console.WriteLine("MEETING UNKNOWN REFRESH CASE " + label + " PASS"); }
        Task<MeetingRevocationOutcome> Retire(MeetingOAuthRegistration reg, MeetingOAuthExchangePermit permit, int bindingVersion, IInterceptor? interceptor = null) => Run(s => s.DisconnectUnknownRefreshAsync(actor, academy, reg, permit, 3, bindingVersion, true), interceptor);
        foreach (var provider in new[] { "GoogleWorkspace", "Microsoft365" })
        {
            var reg = Registration(provider); var source = await Source(reg, true);
            Check(provider + "-pending-not-retired", (await Retire(reg, source.Permit, source.BindingVersion)).StatusCode == 409);
            clock.Now = clock.Now.AddSeconds(30);
            Check(provider + "-expired-start-recovers-unknown-once", await Run(s => s.RecoverRefreshAsync(academy, source.Permit.Id)) && !await Run(s => s.RecoverRefreshAsync(academy, source.Permit.Id)));
            int before; await using (var db = Open()) before = await db.AuditLogs.CountAsync();
            var result = await Retire(reg, source.Permit, source.BindingVersion);
            Check(provider + "-confirmed-retirement-no-provider-material", result.StatusCode == 202 && !result.Connected && !result.CanRetry && result.Material is null);
            Guid revocationId;
            await using (var db = Open())
            {
                var r = await db.MeetingCredentialRevocations.SingleAsync(x => x.ExchangeId == source.Permit.Id); revocationId = r.Id;
                Check(provider + "-atomic-exchange-binding-manual-row-three-audits", r.Status == "ManualRequired" && r.Version == 0 && !r.ProtectedEnvelope.Contains("synthetic-unknown-refresh") && await db.MeetingOAuthExchanges.AnyAsync(x => x.Id == source.Permit.Id && x.Status == "RefreshAbandoned" && x.Version == 4) && await db.MeetingProviderBindings.AnyAsync(x => x.AcademyId == academy && x.Provider == provider && x.Status == "Disconnected" && x.Version == source.BindingVersion + 1 && x.Generation == source.Generation + 1) && await db.AuditLogs.CountAsync() == before + 3);
            }
            Check(provider + "-old-material-replay-read-completion-and-recovery-fenced", (await Retire(reg, source.Permit, source.BindingVersion)).StatusCode == 409 && (await Run(s => s.ReadQuarantinedTokensAsync(actor, academy, reg, source.Permit))).StatusCode == 409 && (await Run(s => s.FinishRefreshAsync(actor, academy, reg, source.Material, new(502, "RefreshOutcomeUnknown", true)))).StatusCode == 409 && !await Run(s => s.RecoverRefreshAsync(academy, source.Permit.Id)) && !await Run(s => s.RecoverRevocationAsync(academy, revocationId)));
            var beforeReportBlocked = provider != "GoogleWorkspace" || (await Run(s => s.PrepareAsync(actor, academy, provider, source.BindingVersion + 1, WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32)), reg.Fingerprint))).Code == "ProviderRevocationNeedsReview";
            var review = (await Run(s => s.PrepareManualRevocationReviewAsync(actor, academy, reg, revocationId))).Review!;
            var report = await Run(s => s.ReportManualRevocationAsync(actor, academy, reg, revocationId, 0, review.ReviewHash, review.Ticket, true));
            Check(provider + "-manual-report-unverified-not-automatic-revoke", report.StatusCode == 200 && !report.ProviderVerified && !report.Connected);
            var reconnect = await Run(s => s.PrepareAsync(actor, academy, provider, source.BindingVersion + 1, WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32)), reg.Fingerprint));
            Check(provider + "-google-manual-required-and-reported-still-blocked", beforeReportBlocked && (provider == "GoogleWorkspace" ? reconnect.Code == "ProviderRevocationNeedsReview" : reconnect.StatusCode == 201));
        }
        var registration = Registration(); var retained = await Source(registration);
        if ((await Run(s => s.DisconnectLocallyAsync(actor, academy, registration.Provider, retained.BindingVersion, true))).StatusCode != 200) throw new InvalidOperationException("Existing local disconnect fixture failed");
        var retainedResult = await Retire(registration, retained.Permit, retained.BindingVersion + 1);
        await using (var db = Open()) Check("already-disconnected-generation-retained-not-incremented-twice", retainedResult.StatusCode == 202 && await db.MeetingProviderBindings.AnyAsync(x => x.AcademyId == academy && x.Provider == registration.Provider && x.Version == retained.BindingVersion + 1 && x.Generation == retained.Generation + 1) && await db.AuditLogs.AnyAsync(x => x.Action == "MeetingUnknownRefreshLocalDisconnectRetained"));
        var race = await Source(registration); var barrier = new ClaimBarrier(); int raceAuditBefore; await using (var db = Open()) raceAuditBefore = await db.AuditLogs.CountAsync();
        var results = await Task.WhenAll(Retire(registration, race.Permit, race.BindingVersion, barrier), Retire(registration, race.Permit, race.BindingVersion, barrier));
        Check("two-retirements-exactly-202-409", results.Select(x => x.StatusCode).Order().SequenceEqual(new[] { 202, 409 }));
        await using (var db = Open()) Check("race-one-retirement-one-manual-row-three-audits", await db.MeetingOAuthExchanges.AnyAsync(x => x.Id == race.Permit.Id && x.Status == "RefreshAbandoned" && x.Version == 4) && await db.MeetingCredentialRevocations.CountAsync(x => x.ExchangeId == race.Permit.Id) == 1 && await db.AuditLogs.CountAsync() == raceAuditBefore + 3);
        var fault = await Source(registration); var failed = false; int faultAuditBefore; await using (var db = Open()) faultAuditBefore = await db.AuditLogs.CountAsync();
        try { await Retire(registration, fault.Permit, fault.BindingVersion, new OperationCommitFault()); } catch (InvalidOperationException ex) when (ex.Message == "Synthetic operation commit failure after SQL writes") { failed = true; }
        await using (var db = Open()) Check("commit-fault-rolls-back-exchange-binding-manual-row-and-audits", failed && await db.MeetingOAuthExchanges.AnyAsync(x => x.Id == fault.Permit.Id && x.Status == "RefreshUnknown" && x.Version == 3) && await db.MeetingProviderBindings.AnyAsync(x => x.AcademyId == academy && x.Provider == registration.Provider && x.Status == "VerificationRequired" && x.Version == fault.BindingVersion && x.Generation == fault.Generation) && !await db.MeetingCredentialRevocations.AnyAsync(x => x.ExchangeId == fault.Permit.Id) && await db.AuditLogs.CountAsync() == faultAuditBefore);
        Check("retirement-after-proved-rollback", (await Retire(registration, fault.Permit, fault.BindingVersion)).StatusCode == 202);
        var denied = await Source(registration);
        async Task<string> Snapshot() { await using var db = Open(); return JsonSerializer.Serialize(new { e = await db.MeetingOAuthExchanges.AsNoTracking().SingleAsync(x => x.Id == denied.Permit.Id), b = await db.MeetingProviderBindings.AsNoTracking().SingleAsync(x => x.AcademyId == academy && x.Provider == registration.Provider), a = await db.AuditLogs.CountAsync(), r = await db.MeetingCredentialRevocations.CountAsync(x => x.ExchangeId == denied.Permit.Id) }); }
        async Task Denied(string label, Func<Task<MeetingRevocationOutcome>> request, int status) { var before = await Snapshot(); var response = await request(); Check(label, response.StatusCode == status && response.Material is null && before == await Snapshot()); }
        await Denied("foreign-academy-no-write", () => Run(s => s.DisconnectUnknownRefreshAsync(actor, foreign, registration, denied.Permit, 3, denied.BindingVersion, true)), 403);
        await Denied("wrong-fence-no-write", () => Retire(registration, denied.Permit with { Fence = Guid.NewGuid() }, denied.BindingVersion), 409);
        await Denied("wrong-binding-version-no-write", () => Retire(registration, denied.Permit, denied.BindingVersion - 1), 409);
        await Denied("confirmation-required-no-write", () => Run(s => s.DisconnectUnknownRefreshAsync(actor, academy, registration, denied.Permit, 3, denied.BindingVersion, false)), 400);
        await Denied("other-current-admin-no-write", () => Run(s => s.DisconnectUnknownRefreshAsync(Actor(otherId), academy, registration, denied.Permit, 3, denied.BindingVersion, true)), 409);
        await using (var db = Open()) { var b = await db.MeetingProviderBindings.SingleAsync(x => x.AcademyId == academy && x.Provider == registration.Provider); b.Generation++; b.Version++; await db.SaveChangesAsync(); }
        await Denied("newer-generation-no-write", () => Retire(registration, denied.Permit, denied.BindingVersion + 1), 409);
        var role = await Source(registration);
        using (var scope = factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(); var user = (await users.FindByIdAsync(actorId.ToString()))!;
            if (!(await users.RemoveFromRoleAsync(user, "AcademyAdmin")).Succeeded) throw new InvalidOperationException("Unknown refresh role fixture failed");
        }
        Check("revoked-role-cannot-retire", (await Retire(registration, role.Permit, role.BindingVersion)).StatusCode == 403);
        foreach (var pair in new[] { ("RefreshAbandoned", 2), ("RefreshAbandoned", 3), ("RefreshAbandoned", 5), ("RefreshAbandoned", -1), ("RefreshUnknown", 4), ("TokensQuarantined", 4) })
        {
            await using var db = Open(); var row = await db.MeetingOAuthExchanges.SingleAsync(x => x.Id == role.Permit.Id); row.Status = pair.Item1; row.Version = pair.Item2; var rejected = false;
            try { await db.SaveChangesAsync(); } catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 547 }) { rejected = true; }
            Check("SQL-state-" + pair.Item1 + "-version-" + pair.Item2, rejected);
        }
        if (checks.Count != 32) throw new InvalidOperationException("Unknown refresh case count mismatch " + checks.Count);
        var sources = new[] { "apps/api/Infrastructure/Meetings/MeetingUnknownRefreshStore.cs", "apps/api/Infrastructure/Meetings/MeetingOAuthExchangeStore.cs", "apps/api/Infrastructure/Meetings/MeetingRevocationStore.cs", "apps/api/Infrastructure/Meetings/MeetingManualRevocationStore.cs", "apps/api/Infrastructure/Meetings/MeetingOAuthAttemptStore.cs", "apps/api/Data/AcademyDeskDbContext.cs", "QA/tools/SqlHarness/MeetingUnknownRefreshRegression.cs", "apps/api/Migrations/AcademyDeskDb/20261016051324_MeetingUnknownRefreshRetirement.cs" }.ToDictionary(p => p, p => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(p))));
        var evidence = Path.Combine("QA", "EVIDENCE", "meeting-unknown-refresh-sql-" + manifest.RunId.ToString("N")); Directory.CreateDirectory(evidence);
        await File.WriteAllTextAsync(Path.Combine(evidence, "result.json"), JsonSerializer.Serialize(new { checks, sources, scope = "Real owned SQL/Identity; local unknown-refresh retirement/manual follow-up only, no network, provider verification, Google unlock, Connected or public UI" }, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine("MEETING UNKNOWN REFRESH PASS:32 SQL/Identity cases; local retirement is not provider revocation.");
    }
}
