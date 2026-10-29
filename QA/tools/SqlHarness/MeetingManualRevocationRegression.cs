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
    private static async Task VerifyMeetingManualRevocationMigrationAsync(AcademyDeskDbContext db)
    {
        var migrations = db.Database.GetMigrations().ToArray(); var latest = migrations.Single(x => x.EndsWith("_MeetingManualRevocationReports", StringComparison.Ordinal));
        var previous = migrations[Array.IndexOf(migrations, latest) - 1]; var migrator = db.GetService<IMigrator>(); await migrator.MigrateAsync(latest);
        var binding = new MeetingProviderBinding { AcademyId = Guid.NewGuid(), Provider = "Zoom", Generation = 1, Version = 1 };
        var attempt = new MeetingOAuthAttempt { AcademyId = binding.AcademyId, BindingId = binding.Id, ActorUserId = Guid.NewGuid(), Provider = "Zoom", Generation = 1,
            StateHash = new string('D', 64), ProtectedEnvelope = "synthetic-migration-only", ExpiresAtUtc = DateTime.UtcNow.AddMinutes(5) };
        var exchange = new MeetingOAuthExchange { AcademyId = binding.AcademyId, ActorUserId = attempt.ActorUserId, AttemptId = attempt.Id, BindingId = binding.Id,
            Provider = "Zoom", Generation = 1, BindingVersion = 1, Fence = Guid.NewGuid(), RegistrationFingerprint = new string('E', 64), DeadlineUtc = DateTime.UtcNow.AddSeconds(30), ProtectedEnvelope = "synthetic-migration-only" };
        var row = new MeetingCredentialRevocation { AcademyId = binding.AcademyId, ActorUserId = attempt.ActorUserId, ExchangeId = exchange.Id, BindingId = binding.Id, Provider = "Zoom", Generation = 1,
            BindingVersion = 1, Fence = Guid.NewGuid(), RegistrationFingerprint = exchange.RegistrationFingerprint, ProtectedEnvelope = "synthetic-migration-only", DeadlineUtc = DateTime.UtcNow.AddSeconds(30), Status = "ManualReportedUnverified", Version = 1 };
        db.AddRange(binding, attempt, exchange, row); await db.SaveChangesAsync(); var blocked = false;
        try { await migrator.MigrateAsync(previous); } catch (SqlException ex) when (ex.Number == 51000) { blocked = true; }
        if (!blocked || !await db.MeetingCredentialRevocations.AnyAsync(x => x.Id == row.Id && x.Status == "ManualReportedUnverified")) throw new InvalidOperationException("Manual report downgrade lost evidence");
        db.Remove(row); await db.SaveChangesAsync(); db.Remove(exchange); await db.SaveChangesAsync(); db.Remove(attempt); await db.SaveChangesAsync(); db.Remove(binding); await db.SaveChangesAsync(); // Exact owned probes only.
        await migrator.MigrateAsync(previous); await migrator.MigrateAsync(latest);
        Console.WriteLine("MEETING MANUAL REVOCATION MIGRATION PASS: upgrade, populated-down refusal, exact probe removal and empty down/up.");
    }
    private static async Task VerifyMeetingManualRevocationsAsync(QaApiFactory factory, QaRunManifest manifest)
    {
        Guid foreign; string connection;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); connection = db.Database.GetConnectionString()!;
            foreign = await db.Academies.Where(x => x.Name == "Synthetic Academy A").Select(x => x.Id).SingleAsync();
        }
        var target = new SqlConnectionStringBuilder(connection);
        if (target.DataSource != manifest.SqlServer || target.InitialCatalog != manifest.Database || target.UserID != manifest.RuntimeLogin) throw new InvalidOperationException("Manual reports require exact owned target");
        AcademyDeskDbContext Open(IInterceptor? interceptor = null) { var options = new DbContextOptionsBuilder<AcademyDeskDbContext>().UseSqlServer(connection); if (interceptor is not null) options.AddInterceptors(interceptor); return new(options.Options); }
        // A distinct owned synthetic academy avoids reusing the deliberately
        // unresolved Google project-grant evidence from the retained M3h suite.
        var tenant = new Academy { Name = "Synthetic Manual Revocation Academy", EnabledModulesJson = "[\"Core\",\"Engagement\"]" };
        await using (var db = Open()) { db.Add(tenant); await db.SaveChangesAsync(); }
        var academy = tenant.Id; var actorId = await CreateAccessActorAsync(factory, "qa-manual-revocation@example.invalid", "AcademyAdmin", academy);
        var otherId = await CreateAccessActorAsync(factory, "qa-manual-revocation-other@example.invalid", "AcademyAdmin", academy);
        ClaimsPrincipal Actor(Guid id) => new(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, id.ToString()) }, "synthetic-manual-revocation"));
        var actor = Actor(actorId); var clock = new OperationClock(); var keys = factory.Services.GetRequiredService<IDataProtectionProvider>();
        async Task<T> Run<T>(Func<MeetingOAuthAttemptStore, Task<T>> action, IInterceptor? interceptor = null)
        {
            using var scope = factory.Services.CreateScope(); await using var db = Open(interceptor); var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            return await action(new(db, users, new(db, users, clock), keys, clock));
        }
        MeetingOAuthRegistration Registration(string provider = "Microsoft365") => MeetingOAuthRegistrationRegistry.Create("https://api.academy.invalid", "https://app.academy.invalid",
            [new(provider, 1, "synthetic-client", "https://api.academy.invalid" + MeetingOAuthRegistrationRegistry.CallbackPath(provider), "https://app.academy.invalid/communication-settings",
                provider == "GoogleWorkspace" ? "google-calendar-owned-web/1" : provider == "Microsoft365" ? "microsoft-online-meetings-work/1" : "zoom-public-pkce/1",
                provider == "Microsoft365" ? "019acc9a-6200-7000-8000-000000000001" : null)]).Resolve(provider)!;
        async Task<(Guid Id, MeetingOAuthExchangePermit Permit)> Source(MeetingOAuthRegistration reg)
        {
            int version; await using (var db = Open()) version = await db.MeetingProviderBindings.Where(x => x.AcademyId == academy && x.Provider == reg.Provider).Select(x => (int?)x.Version).SingleOrDefaultAsync() ?? 0;
            return await Run(async s =>
            {
                var browser = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32)); var intent = await s.PrepareAsync(actor, academy, reg.Provider, version, browser, reg.Fingerprint);
                if (intent.StatusCode != 201 || (await s.CaptureAsync(academy, reg.Provider, intent.Material!.State, browser, reg.Fingerprint, "synthetic-manual-code", false)).StatusCode != 202) throw new InvalidOperationException("Manual report source capture failed");
                var claim = await s.ResumeAsync(actor, academy, reg.Provider, intent.Material.State, browser, reg.Fingerprint); var start = await s.BeginExchangeAsync(actor, academy, reg, claim);
                if ((await s.FinishExchangeAsync(actor, academy, reg, start.Permit!, new(200, "TokensReceivedAccountUnverified", Tokens: new("synthetic-manual-access", "synthetic-manual-refresh", clock.Now.AddHours(1), reg.Flow!.RequiredTokenScopes)))).StatusCode != 200) throw new InvalidOperationException("Manual report quarantine failed");
                if ((await s.BeginRevocationAsync(actor, academy, reg, start.Permit!, version + 2, true, true)).StatusCode != (reg.Provider == "GoogleWorkspace" ? 201 : 202)) throw new InvalidOperationException("Manual report revocation admission failed");
                await using var db = Open(); var id = await db.MeetingCredentialRevocations.Where(x => x.ExchangeId == start.Permit!.Id).Select(x => x.Id).SingleAsync();
                return (id, start.Permit!);
            });
        }
        var checks = new List<string>(); void Check(string label, bool passed) { if (!passed) throw new InvalidOperationException("MEETING MANUAL REVOCATION " + label); checks.Add(label); Console.WriteLine("MEETING MANUAL REVOCATION CASE " + label + " PASS"); }
        Task<MeetingManualRevocationOutcome> Report(MeetingOAuthRegistration reg, MeetingManualRevocationReview review, IInterceptor? interceptor = null) => Run(s => s.ReportManualRevocationAsync(actor, academy, reg, review.Id, review.Version, review.ReviewHash, review.Ticket, true), interceptor);
        foreach (var provider in new[] { "Microsoft365", "Zoom", "GoogleWorkspace" })
        {
            var reg = Registration(provider); var source = await Source(reg);
            if (provider == "GoogleWorkspace")
            {
                Check("google-started-not-manual-reportable", (await Run(s => s.PrepareManualRevocationReviewAsync(actor, academy, reg, source.Id))).StatusCode == 409);
                clock.Now = clock.Now.AddSeconds(30); if (!await Run(s => s.RecoverRevocationAsync(academy, source.Id))) throw new InvalidOperationException("Manual Google recovery fixture failed");
            }
            var prepared = await Run(s => s.PrepareManualRevocationReviewAsync(actor, academy, reg, source.Id)); var review = prepared.Review!;
            await using (var db = Open()) Check(provider + "-review-audit-before-private-ticket", prepared.StatusCode == 200 && !review.ProviderVerified && !JsonSerializer.Serialize(prepared).Contains(review.Ticket) && await db.AuditLogs.CountAsync(x => x.EntityId == source.Id && x.Action == "MeetingManualRevocationReviewPrepared") == 1);
            var result = await Report(reg, review);
            Check(provider + "-human-report-not-provider-verification", result.StatusCode == 200 && result.Code == "ManualRevocationReportedProviderNotVerified" && !result.ProviderVerified && !result.Connected && !result.CanRetry && result.Review is null);
            await using (var db = Open()) Check(provider + "-protected-report-and-one-required-audit", await db.MeetingCredentialRevocations.AnyAsync(x => x.Id == source.Id && x.Status == "ManualReportedUnverified" && x.Version == (provider == "GoogleWorkspace" ? 2 : 1) && !x.ProtectedEnvelope.Contains("synthetic-manual-refresh")) && await db.AuditLogs.CountAsync(x => x.EntityId == source.Id && x.Action == "MeetingRevocationManuallyReportedUnverified") == 1);
            Check(provider + "-replay-and-recovery-denied", (await Report(reg, review)).StatusCode == 409 && !await Run(s => s.RecoverRevocationAsync(academy, source.Id)));
            Check(provider + "-old-credentials-remain-fenced", (await Run(s => s.ReadQuarantinedTokensAsync(actor, academy, reg, source.Permit))).StatusCode == 409);
            int bindingVersion; await using (var db = Open()) bindingVersion = await db.MeetingProviderBindings.Where(x => x.AcademyId == academy && x.Provider == provider).Select(x => x.Version).SingleAsync();
            var reconnect = await Run(s => s.PrepareAsync(actor, academy, provider, bindingVersion, WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32)), reg.Fingerprint));
            Check(provider + "-reconnect-policy-unchanged-not-a-report-grant", provider == "GoogleWorkspace" ? reconnect.Code == "ProviderRevocationNeedsReview" : reconnect.StatusCode == 201);
        }
        var registration = Registration(); var race = await Source(registration); var raceReview = (await Run(s => s.PrepareManualRevocationReviewAsync(actor, academy, registration, race.Id))).Review!; var barrier = new ClaimBarrier();
        var results = await Task.WhenAll(Report(registration, raceReview, barrier), Report(registration, raceReview, barrier));
        Check("two-reports-exactly-200-409", results.Select(x => x.StatusCode).Order().SequenceEqual(new[] { 200, 409 }));
        await using (var db = Open()) Check("race-one-state-one-report-audit", await db.MeetingCredentialRevocations.AnyAsync(x => x.Id == race.Id && x.Status == "ManualReportedUnverified" && x.Version == 1) && await db.AuditLogs.CountAsync(x => x.EntityId == race.Id && x.Action == "MeetingRevocationManuallyReportedUnverified") == 1);
        var fault = await Source(registration); var failed = false;
        try { await Run(s => s.PrepareManualRevocationReviewAsync(actor, academy, registration, fault.Id), new OperationCommitFault()); } catch (InvalidOperationException ex) when (ex.Message == "Synthetic operation commit failure after SQL writes") { failed = true; }
        await using (var db = Open()) Check("review-audit-fault-no-ticket-no-audit", failed && !await db.AuditLogs.AnyAsync(x => x.EntityId == fault.Id && x.Action == "MeetingManualRevocationReviewPrepared") && await db.MeetingCredentialRevocations.AnyAsync(x => x.Id == fault.Id && x.Status == "ManualRequired" && x.Version == 0));
        var faultReviewResult = await Run(s => s.PrepareManualRevocationReviewAsync(actor, academy, registration, fault.Id)); var faultReview = faultReviewResult.Review!;
        Check("review-after-proved-rollback", faultReviewResult.StatusCode == 200);
        failed = false;
        try { await Report(registration, faultReview, new OperationCommitFault()); } catch (InvalidOperationException ex) when (ex.Message == "Synthetic operation commit failure after SQL writes") { failed = true; }
        await using (var db = Open()) Check("report-audit-fault-rolls-back-state-and-audit", failed && await db.MeetingCredentialRevocations.AnyAsync(x => x.Id == fault.Id && x.Status == "ManualRequired" && x.Version == 0) && !await db.AuditLogs.AnyAsync(x => x.EntityId == fault.Id && x.Action == "MeetingRevocationManuallyReportedUnverified"));
        Check("report-after-proved-rollback", (await Report(registration, faultReview)).StatusCode == 200);
        var denied = await Source(registration); var reviewDenied = (await Run(s => s.PrepareManualRevocationReviewAsync(actor, academy, registration, denied.Id))).Review!;
        clock.Now = clock.Now.AddMinutes(5);
        Check("exact-expiry-denied", (await Report(registration, reviewDenied)).StatusCode == 410);
        reviewDenied = (await Run(s => s.PrepareManualRevocationReviewAsync(actor, academy, registration, denied.Id))).Review!;
        Check("wrong-hash-denied", (await Run(s => s.ReportManualRevocationAsync(actor, academy, registration, denied.Id, 0, new string('A', 64), reviewDenied.Ticket, true))).StatusCode == 409);
        Check("forged-ticket-denied", (await Run(s => s.ReportManualRevocationAsync(actor, academy, registration, denied.Id, 0, reviewDenied.ReviewHash, "forged", true))).StatusCode == 409);
        Check("no-human-declaration-denied", (await Run(s => s.ReportManualRevocationAsync(actor, academy, registration, denied.Id, 0, reviewDenied.ReviewHash, reviewDenied.Ticket, false))).StatusCode == 400);
        Check("foreign-academy-denied", (await Run(s => s.ReportManualRevocationAsync(actor, foreign, registration, denied.Id, 0, reviewDenied.ReviewHash, reviewDenied.Ticket, true))).StatusCode == 403);
        Check("different-current-admin-denied", (await Run(s => s.ReportManualRevocationAsync(Actor(otherId), academy, registration, denied.Id, 0, reviewDenied.ReviewHash, reviewDenied.Ticket, true))).StatusCode == 409);
        await using (var db = Open()) { var b = await db.MeetingProviderBindings.SingleAsync(x => x.AcademyId == academy && x.Provider == registration.Provider); b.Generation++; b.Version++; await db.SaveChangesAsync(); }
        Check("changed-generation-denied", (await Report(registration, reviewDenied)).StatusCode == 409);
        var roleSource = await Source(registration); var roleReview = (await Run(s => s.PrepareManualRevocationReviewAsync(actor, academy, registration, roleSource.Id))).Review!;
        using (var scope = factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(); var user = (await users.FindByIdAsync(actorId.ToString()))!;
            if (!(await users.RemoveFromRoleAsync(user, "AcademyAdmin")).Succeeded) throw new InvalidOperationException("Manual report role fixture failed");
        }
        Check("revoked-role-cannot-read-or-report", (await Report(registration, roleReview)).StatusCode == 403 && (await Run(s => s.PrepareManualRevocationReviewAsync(actor, academy, registration, roleSource.Id))).StatusCode == 403);
        foreach (var pair in new[] { ("ManualReportedUnverified", 0), ("ManualReportedUnverified", 2), ("ManualReportedUnverified", 3), ("Connected", 1), ("ManualReportedUnverified", -1), ("ManualRequired", 1) })
        {
            await using var db = Open(); var row = await db.MeetingCredentialRevocations.SingleAsync(x => x.Id == roleSource.Id); row.Status = pair.Item1; row.Version = pair.Item2; var rejected = false;
            try { await db.SaveChangesAsync(); } catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 547 }) { rejected = true; }
            Check("SQL-state-" + pair.Item1 + "-version-" + pair.Item2, rejected);
        }
        if (checks.Count != 39) throw new InvalidOperationException("Manual revocation case count mismatch " + checks.Count);
        var sources = new[] { "apps/api/Infrastructure/Meetings/MeetingManualRevocationStore.cs", "apps/api/Infrastructure/Meetings/MeetingRevocationStore.cs", "apps/api/Infrastructure/Meetings/MeetingOAuthAttemptStore.cs", "apps/api/Data/AcademyDeskDbContext.cs", "QA/tools/SqlHarness/MeetingManualRevocationRegression.cs", "apps/api/Migrations/AcademyDeskDb/20261016045457_MeetingManualRevocationReports.cs" }.ToDictionary(p => p, p => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(p))));
        var evidence = Path.Combine("QA", "EVIDENCE", "meeting-manual-revocation-sql-" + manifest.RunId.ToString("N")); Directory.CreateDirectory(evidence);
        await File.WriteAllTextAsync(Path.Combine(evidence, "result.json"), JsonSerializer.Serialize(new { checks, sources, scope = "Real owned SQL/Identity; internal human-report evidence only, no external requests, verified revocation, Google unlock, Connected, public routes or UI enablement" }, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine("MEETING MANUAL REVOCATION PASS:39 SQL/Identity cases; reported is not independently verified.");
    }
}
