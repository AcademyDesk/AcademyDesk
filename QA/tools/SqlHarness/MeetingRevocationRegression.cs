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
    private static async Task VerifyMeetingRevocationMigrationAsync(AcademyDeskDbContext db)
    {
        var migrations = db.Database.GetMigrations().ToArray(); var latest = migrations.Single(x => x.EndsWith("_MeetingCredentialRevocation", StringComparison.Ordinal));
        var previous = migrations[Array.IndexOf(migrations, latest) - 1]; var migrator = db.GetService<IMigrator>(); await migrator.MigrateAsync(latest);
        var binding = new MeetingProviderBinding { AcademyId = Guid.NewGuid(), Provider = "Zoom", Generation = 1, Version = 1 };
        var attempt = new MeetingOAuthAttempt { AcademyId = binding.AcademyId, BindingId = binding.Id, ActorUserId = Guid.NewGuid(), Provider = "Zoom", Generation = 1,
            StateHash = new string('D', 64), ProtectedEnvelope = "synthetic-migration-only", ExpiresAtUtc = DateTime.UtcNow.AddMinutes(5) };
        var exchange = new MeetingOAuthExchange { AcademyId = binding.AcademyId, ActorUserId = attempt.ActorUserId, AttemptId = attempt.Id, BindingId = binding.Id,
            Provider = "Zoom", Generation = 1, BindingVersion = 1, Fence = Guid.NewGuid(), RegistrationFingerprint = new string('E', 64), DeadlineUtc = DateTime.UtcNow.AddSeconds(30), ProtectedEnvelope = "synthetic-migration-only" };
        var row = new MeetingCredentialRevocation { AcademyId = binding.AcademyId, ActorUserId = attempt.ActorUserId, ExchangeId = exchange.Id, BindingId = binding.Id, Provider = "Zoom", Generation = 1,
            BindingVersion = 1, Fence = Guid.NewGuid(), RegistrationFingerprint = exchange.RegistrationFingerprint, ProtectedEnvelope = "synthetic-migration-only", DeadlineUtc = DateTime.UtcNow.AddSeconds(30) };
        db.AddRange(binding, attempt, exchange, row); await db.SaveChangesAsync(); var blocked = false;
        try { await migrator.MigrateAsync(previous); } catch (SqlException ex) when (ex.Number == 51000) { blocked = true; }
        if (!blocked || !await db.MeetingCredentialRevocations.AnyAsync(x => x.Id == row.Id) || !await db.MeetingOAuthExchanges.AnyAsync(x => x.Id == exchange.Id)) throw new InvalidOperationException("Revocation downgrade lost evidence");
        db.Remove(row); await db.SaveChangesAsync(); db.Remove(exchange); await db.SaveChangesAsync(); db.Remove(attempt); await db.SaveChangesAsync(); db.Remove(binding); await db.SaveChangesAsync(); // Exact owned probes only.
        await migrator.MigrateAsync(previous); await migrator.MigrateAsync(latest);
        Console.WriteLine("MEETING REVOCATION MIGRATION PASS: upgrade, populated-down refusal, exact probe removal and empty down/up.");
    }
    private static async Task VerifyMeetingRevocationsAsync(QaApiFactory factory, QaRunManifest manifest)
    {
        Guid academy, foreign; string connection;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); connection = db.Database.GetConnectionString()!;
            academy = await db.Academies.Where(x => x.Name == "Synthetic Academy A").Select(x => x.Id).SingleAsync(); foreign = await db.Academies.Where(x => x.Name == "Synthetic Academy B").Select(x => x.Id).SingleAsync();
        }
        var target = new SqlConnectionStringBuilder(connection);
        if (target.DataSource != manifest.SqlServer || target.InitialCatalog != manifest.Database || target.UserID != manifest.RuntimeLogin) throw new InvalidOperationException("Revocation requires exact owned target");
        var actorId = await CreateAccessActorAsync(factory, "qa-revocation@example.invalid", "AcademyAdmin", academy);
        var otherId = await CreateAccessActorAsync(factory, "qa-revocation-other@example.invalid", "AcademyAdmin", academy);
        var actor = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, actorId.ToString()) }, "synthetic-revocation"));
        var clock = new OperationClock(); var keys = factory.Services.GetRequiredService<IDataProtectionProvider>(); const string refresh = "synthetic-revocation-refresh";
        AcademyDeskDbContext Open(IInterceptor? interceptor = null) { var options = new DbContextOptionsBuilder<AcademyDeskDbContext>().UseSqlServer(connection); if (interceptor is not null) options.AddInterceptors(interceptor); return new(options.Options); }
        async Task<T> Run<T>(Func<MeetingOAuthAttemptStore, Task<T>> action, IInterceptor? interceptor = null)
        {
            using var scope = factory.Services.CreateScope(); await using var db = Open(interceptor); var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            return await action(new(db, users, new(db, users, clock), keys, clock));
        }
        MeetingOAuthRegistration Registration(string provider = "GoogleWorkspace")
        {
            var profile = provider switch { "GoogleWorkspace" => "google-calendar-owned-web/1", "Microsoft365" => "microsoft-online-meetings-work/1", _ => "zoom-public-pkce/1" };
            return MeetingOAuthRegistrationRegistry.Create("https://api.academy.invalid", "https://app.academy.invalid", [new(provider, 1, "synthetic-client", "https://api.academy.invalid" + MeetingOAuthRegistrationRegistry.CallbackPath(provider),
                "https://app.academy.invalid/communication-settings", profile, provider == "Microsoft365" ? "019acc9a-6200-7000-8000-000000000001" : null)]).Resolve(provider)!;
        }
        async Task<(MeetingOAuthExchangePermit Permit, int Version)> Quarantine(MeetingOAuthRegistration registration)
        {
            // Separate successful revocation scenarios must not reconnect a
            // still-unreconciled project grant. Preserve all prior evidence;
            // use a distinct run-owned academy and current identity instead.
            await using (var db = Open())
            {
                if (registration.Provider == "GoogleWorkspace" && await db.MeetingCredentialRevocations.AnyAsync(x => x.AcademyId == academy && x.Provider == registration.Provider && x.Status == "ProviderAcknowledged"))
                {
                    var tenant = new Academy { Name = "Synthetic isolated revocation " + Guid.NewGuid().ToString("N"), EnabledModulesJson = "[\"Core\",\"Engagement\"]" };
                    db.Add(tenant); await db.SaveChangesAsync(); academy = tenant.Id;
                    actorId = await CreateAccessActorAsync(factory, "qa-revocation-" + Guid.NewGuid().ToString("N") + "@example.invalid", "AcademyAdmin", academy);
                    otherId = await CreateAccessActorAsync(factory, "qa-revocation-other-" + Guid.NewGuid().ToString("N") + "@example.invalid", "AcademyAdmin", academy);
                    actor = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, actorId.ToString()) }, "synthetic-revocation"));
                }
            }
            int version; await using (var db = Open()) version = await db.MeetingProviderBindings.Where(x => x.AcademyId == academy && x.Provider == registration.Provider).Select(x => x.Version).SingleOrDefaultAsync();
            return await Run(async store =>
            {
                var browser = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32)); var prepared = await store.PrepareAsync(actor, academy, registration.Provider, version, browser, registration.Fingerprint);
                if (prepared.StatusCode != 201 || (await store.CaptureAsync(academy, registration.Provider, prepared.Material!.State, browser, registration.Fingerprint, "synthetic-revocation-code", false)).StatusCode != 202) throw new InvalidOperationException("Revocation capture fixture failed");
                var claim = await store.ResumeAsync(actor, academy, registration.Provider, prepared.Material.State, browser, registration.Fingerprint); var start = await store.BeginExchangeAsync(actor, academy, registration, claim);
                if (start.StatusCode != 201 || (await store.FinishExchangeAsync(actor, academy, registration, start.Permit!, new(200, "TokensReceivedAccountUnverified", Tokens: new("synthetic-revocation-access", refresh, clock.Now.AddHours(1), registration.Flow!.RequiredTokenScopes)))).StatusCode != 200) throw new InvalidOperationException("Revocation quarantine fixture failed");
                return (start.Permit!, version + 2);
            });
        }
        var checks = new List<string>(); void Check(string label, bool passed) { if (!passed) throw new InvalidOperationException("MEETING REVOCATION " + label); checks.Add(label); Console.WriteLine("MEETING REVOCATION CASE " + label + " PASS"); }
        Guid manual = Guid.Empty, manualAcademy = Guid.Empty;
        foreach (var provider in new[] { "GoogleWorkspace", "Microsoft365", "Zoom" })
        {
            var reg = Registration(provider); var source = await Quarantine(reg); var start = await Run(s => s.BeginRevocationAsync(actor, academy, reg, source.Permit, source.Version, true, true));
            await using (var db = Open()) Check(provider + "-atomic-local-disconnect-and-audits", start.StatusCode == (provider == "GoogleWorkspace" ? 201 : 202) && await db.MeetingProviderBindings.AnyAsync(x => x.AcademyId == academy && x.Provider == provider && x.Status == "Disconnected" && x.Version == source.Version + 1));
            await using (var db = Open())
            {
                var row = await db.MeetingCredentialRevocations.SingleAsync(x => x.ExchangeId == source.Permit.Id);
                if (provider == "Microsoft365") { manual = row.Id; manualAcademy = academy; }
                Check(provider + "-no-stored-token-or-false-ack", !row.ProtectedEnvelope.Contains(refresh) && row.Status == (provider == "GoogleWorkspace" ? "Started" : "ManualRequired") && JsonSerializer.Serialize(start.Material) is "{}" or "null" && (provider == "GoogleWorkspace" || start.Material is null) && await db.AuditLogs.CountAsync(x => x.EntityId == row.Id) == 1);
            }
            Check(provider + "-start-replay-denied", (await Run(s => s.BeginRevocationAsync(actor, academy, reg, source.Permit, source.Version, true, true))).StatusCode == 409);
            Check(provider + "-old-credential-read-fenced", (await Run(s => s.ReadQuarantinedTokensAsync(actor, academy, reg, source.Permit))).StatusCode == 409);
            if (provider == "GoogleWorkspace")
            {
                using var transport = new HttpMessageInvoker(new ExchangeTransport(async () =>
                {
                    await using var db = Open(); if (!await db.AuditLogs.AnyAsync(x => x.EntityId == start.Material!.Id && x.Action == "MeetingRevocationStarted")) throw new InvalidOperationException("Revocation sent before durable audit");
                }, []));
                var result = await Run(s => new MeetingProviderRevocation(s, transport).SendStartedAsync(actor, academy, reg, start.Material!));
                Check("google-fake-provider-ack-after-durable-start", result.StatusCode == 200 && !result.CanRetry && result.Material is null);
            }
        }
        var registration = Registration(); var raceSource = await Quarantine(registration); var barrier = new ClaimBarrier();
        var starts = await Task.WhenAll(Run(s => s.BeginRevocationAsync(actor, academy, registration, raceSource.Permit, raceSource.Version, true, true), barrier), Run(s => s.BeginRevocationAsync(actor, academy, registration, raceSource.Permit, raceSource.Version, true, true), barrier));
        Check("two-starts-one-disconnect-permit", starts.Select(x => x.StatusCode).Order().SequenceEqual(new[] { 201, 409 }));
        var material = starts.Single(x => x.Material is not null).Material!;
        await using (var db = Open()) Check("race-one-row-one-local-audit", await db.MeetingCredentialRevocations.CountAsync(x => x.ExchangeId == raceSource.Permit.Id) == 1 && await db.AuditLogs.CountAsync(x => x.EntityId == material.Id) == 1 && await db.MeetingProviderBindings.AnyAsync(x => x.AcademyId == academy && x.Provider == "GoogleWorkspace" && x.Version == raceSource.Version + 1));
        var finishBarrier = new ClaimBarrier(); var finishes = await Task.WhenAll(Run(s => s.FinishRevocationAsync(actor, academy, registration, material, true), finishBarrier), Run(s => s.FinishRevocationAsync(actor, academy, registration, material, true), finishBarrier));
        Check("two-finishes-one-acknowledgement", finishes.Select(x => x.StatusCode).Order().SequenceEqual(new[] { 200, 409 }));
        await using (var db = Open()) Check("race-ack-one-state-two-audits", await db.MeetingCredentialRevocations.AnyAsync(x => x.Id == material.Id && x.Status == "ProviderAcknowledged" && x.Version == 1) && await db.AuditLogs.CountAsync(x => x.EntityId == material.Id) == 2);
        var faultSource = await Quarantine(registration); var failed = false; var calls = 0;
        Check("separate-project-grant-consent-required", (await Run(s => s.BeginRevocationAsync(actor, academy, registration, faultSource.Permit, faultSource.Version, true, false))).StatusCode == 400);
        Check("foreign-academy-denied", (await Run(s => s.BeginRevocationAsync(actor, foreign, registration, faultSource.Permit, faultSource.Version, true, true))).StatusCode == 403);
        using (var transport = new HttpMessageInvoker(new ExchangeTransport(() => { calls++; return Task.CompletedTask; }, [])))
        {
            try { await Run(s => new MeetingProviderRevocation(s, transport).ExecuteAsync(actor, academy, registration, faultSource.Permit, faultSource.Version, true, true), new OperationCommitFault()); } catch (InvalidOperationException ex) when (ex.Message == "Synthetic operation commit failure after SQL writes") { failed = true; }
        }
        await using (var db = Open()) Check("start-audit-fault-no-send-no-disconnect", failed && calls == 0 && !await db.MeetingCredentialRevocations.AnyAsync(x => x.ExchangeId == faultSource.Permit.Id) && await db.MeetingProviderBindings.AnyAsync(x => x.AcademyId == academy && x.Provider == "GoogleWorkspace" && x.Version == faultSource.Version && x.Status == "VerificationRequired"));
        var faultStart = await Run(s => s.BeginRevocationAsync(actor, academy, registration, faultSource.Permit, faultSource.Version, true, true)); Check("start-after-proved-rollback", faultStart.StatusCode == 201);
        Check("wrong-fence-cannot-acknowledge", (await Run(s => s.FinishRevocationAsync(actor, academy, registration, new(faultStart.Material!.Id, Guid.NewGuid(), refresh), true))).StatusCode == 409);
        failed = false;
        try { await Run(s => s.FinishRevocationAsync(actor, academy, registration, faultStart.Material!, true), new OperationCommitFault()); } catch (InvalidOperationException ex) when (ex.Message == "Synthetic operation commit failure after SQL writes") { failed = true; }
        await using (var db = Open()) Check("completion-audit-fault-rollback", failed && await db.MeetingCredentialRevocations.AnyAsync(x => x.Id == faultStart.Material!.Id && x.Status == "Started" && x.Version == 0) && await db.AuditLogs.CountAsync(x => x.EntityId == faultStart.Material!.Id) == 1);
        var browser = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        Check("in-progress-project-revoke-blocks-reconnect", (await Run(s => s.PrepareAsync(actor, academy, registration.Provider, faultSource.Version + 1, browser, registration.Fingerprint))).Code == "ProviderRevocationNeedsReview");
        clock.Now = clock.Now.AddSeconds(30);
        Check("restart-expired-revocation-unknown-idempotent", await Run(s => s.RecoverRevocationAsync(academy, faultStart.Material!.Id)) && !await Run(s => s.RecoverRevocationAsync(academy, faultStart.Material!.Id)));
        Check("uncertain-revocation-never-restarted", (await Run(s => s.BeginRevocationAsync(actor, academy, registration, faultSource.Permit, faultSource.Version, true, true))).StatusCode == 409);
        Check("manual-revocation-not-automatically-acknowledged", !await Run(s => s.RecoverRevocationAsync(manualAcademy, manual)));
        Check("unknown-project-revoke-blocks-reconnect", (await Run(s => s.PrepareAsync(actor, academy, registration.Provider, faultSource.Version + 1, browser, registration.Fingerprint))).Code == "ProviderRevocationNeedsReview");
        using (var scope = factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(); var user = (await users.FindByIdAsync(actorId.ToString()))!;
            if (!(await users.RemoveFromRoleAsync(user, "AcademyAdmin")).Succeeded) throw new InvalidOperationException("Revocation role fixture failed");
        }
        Check("revoked-role-cannot-acknowledge", (await Run(s => s.FinishRevocationAsync(actor, academy, registration, faultStart.Material!, true))).StatusCode == 403);
        MeetingCredentialRevocation template; await using (var db = Open()) template = await db.MeetingCredentialRevocations.AsNoTracking().SingleAsync(x => x.Id == faultStart.Material!.Id);
        async Task Constraint(string label, Action<MeetingCredentialRevocation> mutate, bool fresh, int expected)
        {
            await using var db = Open(); var exchangeId = template.ExchangeId;
            if (fresh)
            {
                var attempt = new MeetingOAuthAttempt { AcademyId = academy, ActorUserId = actorId, BindingId = template.BindingId, Provider = "GoogleWorkspace", Generation = template.Generation,
                    StateHash = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)), ProtectedEnvelope = "synthetic-constraint-only", ExpiresAtUtc = clock.Now.UtcDateTime.AddMinutes(5) };
                var exchange = new MeetingOAuthExchange { AcademyId = academy, ActorUserId = actorId, AttemptId = attempt.Id, BindingId = template.BindingId, Provider = "GoogleWorkspace", Generation = template.Generation,
                    BindingVersion = template.BindingVersion, Fence = Guid.NewGuid(), RegistrationFingerprint = template.RegistrationFingerprint, ProtectedEnvelope = "synthetic-constraint-only", DeadlineUtc = clock.Now.UtcDateTime.AddSeconds(30) };
                db.AddRange(attempt, exchange); await db.SaveChangesAsync(); exchangeId = exchange.Id;
            }
            var row = new MeetingCredentialRevocation { AcademyId = academy, ActorUserId = actorId, ExchangeId = exchangeId, BindingId = template.BindingId, Provider = "GoogleWorkspace", Generation = template.Generation,
                BindingVersion = template.BindingVersion, Fence = Guid.NewGuid(), RegistrationFingerprint = template.RegistrationFingerprint, ProtectedEnvelope = "synthetic-constraint-only", DeadlineUtc = clock.Now.UtcDateTime.AddSeconds(30) };
            mutate(row); db.Add(row); var rejected = false;
            try { await db.SaveChangesAsync(); } catch (DbUpdateException ex) when (ex.InnerException is SqlException sql && sql.Number == expected) { rejected = true; }
            Check(label, rejected);
        }
        await Constraint("unique-exchange-revocation", _ => { }, false, 2601);
        await Constraint("cross-academy-FK", x => x.AcademyId = foreign, true, 547);
        await Constraint("cross-actor-FK", x => x.ActorUserId = otherId, true, 547);
        await Constraint("cross-provider-FK", x => x.Provider = "Zoom", true, 547);
        await Constraint("connected-state-refused", x => x.Status = "Connected", true, 547);
        await Constraint("zero-generation-refused", x => x.Generation = 0, true, 547);
        if (checks.Count != 35) throw new InvalidOperationException("Revocation case count mismatch " + checks.Count);
        var sources = new[] { "apps/api/Infrastructure/Meetings/MeetingRevocationStore.cs", "apps/api/Infrastructure/Meetings/MeetingProviderRevocation.cs", "apps/api/Infrastructure/Meetings/MeetingOAuthAttemptStore.cs", "apps/api/Domain/Entities/MeetingCredentialRevocation.cs", "apps/api/Data/AcademyDeskDbContext.cs", "QA/tools/SqlHarness/MeetingRevocationRegression.cs", "apps/api/Migrations/AcademyDeskDb/20261015180749_MeetingCredentialRevocation.cs" }.ToDictionary(p => p, p => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(p))));
        var evidence = Path.Combine("QA", "EVIDENCE", "meeting-revocation-sql-" + manifest.RunId.ToString("N")); Directory.CreateDirectory(evidence);
        await File.WriteAllTextAsync(Path.Combine(evidence, "result.json"), JsonSerializer.Serialize(new { checks, sources, scope = "Real owned SQL/Identity, fake Google revoke only; Microsoft/Zoom manual-required, no live providers, Connected or global revocation proof" }, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine("MEETING REVOCATION PASS:35SQL/Identity cases; fake Google only, unsupported revocation remains manual.");
    }
}
