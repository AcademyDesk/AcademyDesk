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
    private static async Task VerifyMeetingAccountReviewMigrationAsync(AcademyDeskDbContext db)
    {
        var migrations = db.Database.GetMigrations().ToArray(); var latest = migrations.Single(x => x.EndsWith("_MeetingAccountReviewConfirmation", StringComparison.Ordinal));
        var previous = migrations[Array.IndexOf(migrations, latest) - 1]; var migrator = db.GetService<IMigrator>(); await migrator.MigrateAsync(latest);
        var binding = new MeetingProviderBinding { AcademyId = Guid.NewGuid(), Provider = "Zoom", Generation = 1, Version = 1 };
        var attempt = new MeetingOAuthAttempt { AcademyId = binding.AcademyId, BindingId = binding.Id, ActorUserId = Guid.NewGuid(), Provider = "Zoom", Generation = 1,
            StateHash = new string('D', 64), ProtectedEnvelope = "synthetic-migration-only", ExpiresAtUtc = DateTime.UtcNow.AddMinutes(5) };
        var exchange = new MeetingOAuthExchange { AcademyId = binding.AcademyId, ActorUserId = attempt.ActorUserId, AttemptId = attempt.Id, BindingId = binding.Id,
            Provider = "Zoom", Generation = 1, BindingVersion = 1, Fence = Guid.NewGuid(), RegistrationFingerprint = new string('E', 64), DeadlineUtc = DateTime.UtcNow.AddSeconds(30), ProtectedEnvelope = "synthetic-migration-only" };
        var row = new MeetingAccountReview { AcademyId = binding.AcademyId, ActorUserId = attempt.ActorUserId, ExchangeId = exchange.Id, BindingId = binding.Id, Provider = "Zoom",
            Generation = 1, BindingVersion = 1, RegistrationFingerprint = exchange.RegistrationFingerprint, ReviewHash = new string('F', 64), ProtectedEnvelope = "synthetic-migration-only", ExpiresAtUtc = DateTime.UtcNow.AddMinutes(5) };
        db.AddRange(binding, attempt, exchange, row); await db.SaveChangesAsync(); var blocked = false;
        try { await migrator.MigrateAsync(previous); } catch (SqlException ex) when (ex.Number == 51000) { blocked = true; }
        if (!blocked || !await db.MeetingAccountReviews.AnyAsync(x => x.Id == row.Id) || !await db.MeetingOAuthExchanges.AnyAsync(x => x.Id == exchange.Id)) throw new InvalidOperationException("Account review downgrade lost evidence");
        db.Remove(row); await db.SaveChangesAsync(); db.Remove(exchange); await db.SaveChangesAsync(); db.Remove(attempt); await db.SaveChangesAsync(); db.Remove(binding); await db.SaveChangesAsync(); // Exact owned migration probes only.
        await migrator.MigrateAsync(previous); await migrator.MigrateAsync(latest);
        Console.WriteLine("MEETING ACCOUNT REVIEW MIGRATION PASS: upgrade, populated-down refusal, exact probe removal and empty down/up.");
    }
    private static async Task VerifyMeetingAccountReviewsAsync(QaApiFactory factory, QaRunManifest manifest)
    {
        Guid academy, foreign; string connection;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); connection = db.Database.GetConnectionString()!;
            academy = await db.Academies.Where(x => x.Name == "Synthetic Academy A").Select(x => x.Id).SingleAsync(); foreign = await db.Academies.Where(x => x.Name == "Synthetic Academy B").Select(x => x.Id).SingleAsync();
        }
        var target = new SqlConnectionStringBuilder(connection);
        if (target.DataSource != manifest.SqlServer || target.InitialCatalog != manifest.Database || target.UserID != manifest.RuntimeLogin) throw new InvalidOperationException("Account reviews require exact owned target");
        var actorId = await CreateAccessActorAsync(factory, "qa-account-review@example.invalid", "AcademyAdmin", academy);
        var otherId = await CreateAccessActorAsync(factory, "qa-account-review-other@example.invalid", "AcademyAdmin", academy);
        ClaimsPrincipal Actor(Guid id) => new(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, id.ToString()) }, "synthetic-review"));
        var actor = Actor(actorId); var clock = new OperationClock(); var keys = factory.Services.GetRequiredService<IDataProtectionProvider>();
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
        async Task<MeetingOAuthExchangePermit> Quarantine(MeetingOAuthRegistration registration)
        {
            int version; await using (var db = Open()) version = await db.MeetingProviderBindings.Where(x => x.AcademyId == academy && x.Provider == registration.Provider).Select(x => x.Version).SingleAsync();
            return await Run(async store =>
            {
                var browser = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32)); var prepared = await store.PrepareAsync(actor, academy, registration.Provider, version, browser, registration.Fingerprint);
                if (prepared.StatusCode != 201 || (await store.CaptureAsync(academy, registration.Provider, prepared.Material!.State, browser, registration.Fingerprint, "synthetic-review-code", false)).StatusCode != 202) throw new InvalidOperationException("Review capture fixture failed");
                var claim = await store.ResumeAsync(actor, academy, registration.Provider, prepared.Material.State, browser, registration.Fingerprint); var start = await store.BeginExchangeAsync(actor, academy, registration, claim);
                if (start.StatusCode != 201 || (await store.FinishExchangeAsync(actor, academy, registration, start.Permit!, new(200, "TokensReceivedAccountUnverified", Tokens: new("synthetic-review-access", "synthetic-review-refresh", clock.Now.AddHours(1), registration.Flow!.RequiredTokenScopes)))).StatusCode != 200) throw new InvalidOperationException("Review quarantine fixture failed");
                return start.Permit!;
            });
        }
        MeetingOAuthAccountObservation Observation() => new("synthetic-review-subject", "synthetic-review-account", clock.Now);
        async Task<MeetingAccountReviewState> Prepare(MeetingOAuthRegistration reg, MeetingOAuthExchangePermit permit)
        {
            var result = await Run(s => s.PrepareAccountReviewAsync(actor, academy, reg, permit, Observation()));
            if (result.StatusCode != 201) throw new InvalidOperationException("Review preparation fixture failed"); return result.Review!;
        }
        var checks = new List<string>(); void Check(string label, bool passed) { if (!passed) throw new InvalidOperationException("MEETING ACCOUNT REVIEW " + label); checks.Add(label); Console.WriteLine("MEETING ACCOUNT REVIEW CASE " + label + " PASS"); }
        foreach (var provider in new[] { "GoogleWorkspace", "Microsoft365", "Zoom" })
        {
            var reg = Registration(provider); var permit = await Quarantine(reg);
            object body = provider switch { "GoogleWorkspace" => new { sub = "synthetic-google-user", email_verified = true }, "Microsoft365" => new { id = "019acc9a-6200-7000-8000-000000000002" }, _ => new { id = "synthetic-zoom-user", account_id = "synthetic-zoom-account", status = "active" } };
            using var transport = new HttpMessageInvoker(new ExchangeTransport(() => Task.CompletedTask, JsonSerializer.SerializeToUtf8Bytes(body)));
            var prepared = await Run(s => new MeetingOAuthAccountProbe(s, transport, clock).PrepareReviewAsync(actor, academy, reg, permit)); var review = prepared.Review!;
            Check(provider + "-durable-review-from-fake-probe", prepared.StatusCode == 201 && review.Status == "AwaitingConfirmation" && !review.Connected && !review.CanExecute);
            var read = await Run(s => s.ReadAccountReviewAsync(actor, academy, reg, review.Id));
            Check(provider + "-private-audited-restart-read", read.StatusCode == 200 && read.Review!.ExpiresAtUtc.Kind == DateTimeKind.Utc && !JsonSerializer.Serialize(read).Contains("synthetic-"));
            var confirmed = await Run(s => s.ConfirmAccountReviewAsync(actor, academy, reg, review.Id, 0, review.ReviewHash, true));
            Check(provider + "-explicit-confirmation-not-connected", confirmed.StatusCode == 200 && confirmed.Review!.Status == "ConfirmedNotConnected" && !confirmed.Review.Connected);
            Check(provider + "-confirmation-replay-denied", (await Run(s => s.ConfirmAccountReviewAsync(actor, academy, reg, review.Id, 0, review.ReviewHash, true))).StatusCode == 409);
            await using (var db = Open())
            {
                var row = await db.MeetingAccountReviews.SingleAsync(x => x.Id == review.Id);
                Check(provider + "-encrypted-evidence-three-audits", !row.ProtectedEnvelope.Contains("synthetic-") && row.Status == "ConfirmedNotConnected" && row.Version == 1 && row.ConfirmedAtUtc is not null && await db.AuditLogs.CountAsync(x => x.EntityId == row.Id) == 3 && await db.MeetingProviderBindings.AnyAsync(x => x.Id == row.BindingId && x.Status == "VerificationRequired"));
            }
            int version; await using (var db = Open()) version = await db.MeetingProviderBindings.Where(x => x.AcademyId == academy && x.Provider == provider).Select(x => x.Version).SingleAsync();
            if ((await Run(s => s.DisconnectLocallyAsync(actor, academy, provider, version, true))).StatusCode != 200) throw new InvalidOperationException("Review disconnect fixture failed");
            Check(provider + "-disconnect-fences-confirmed-review", (await Run(s => s.ReadAccountReviewAsync(actor, academy, reg, review.Id))).StatusCode == 409);
        }
        var registration = Registration(); var racePermit = await Quarantine(registration); var barrier = new ClaimBarrier();
        var prepares = await Task.WhenAll(Run(s => s.PrepareAccountReviewAsync(actor, academy, registration, racePermit, Observation()), barrier), Run(s => s.PrepareAccountReviewAsync(actor, academy, registration, racePermit, Observation()), barrier));
        Check("two-preparations-one-review", prepares.Select(x => x.StatusCode).Order().SequenceEqual(new[] { 201, 409 }));
        var race = prepares.Single(x => x.Review is not null).Review!; var confirmBarrier = new ClaimBarrier();
        var confirms = await Task.WhenAll(Run(s => s.ConfirmAccountReviewAsync(actor, academy, registration, race.Id, 0, race.ReviewHash, true), confirmBarrier), Run(s => s.ConfirmAccountReviewAsync(actor, academy, registration, race.Id, 0, race.ReviewHash, true), confirmBarrier));
        Check("two-confirmations-one-decision", confirms.Select(x => x.StatusCode).Order().SequenceEqual(new[] { 200, 409 }));
        await using (var db = Open()) Check("race-one-row-two-audits", await db.MeetingAccountReviews.CountAsync(x => x.ExchangeId == racePermit.Id) == 1 && await db.AuditLogs.CountAsync(x => x.EntityId == race.Id) == 2);
        var faultPermit = await Quarantine(registration); var failed = false;
        try { await Run(s => s.PrepareAccountReviewAsync(actor, academy, registration, faultPermit, Observation()), new OperationCommitFault()); } catch (InvalidOperationException ex) when (ex.Message == "Synthetic operation commit failure after SQL writes") { failed = true; }
        await using (var db = Open()) Check("prepare-commit-fault-rollback", failed && !await db.MeetingAccountReviews.AnyAsync(x => x.ExchangeId == faultPermit.Id) && await db.AuditLogs.CountAsync(x => x.ActorUserId == actorId && x.Action == "MeetingAccountReviewPreparedNotConnected") == 4);
        var fault = await Prepare(registration, faultPermit); Check("prepare-after-proved-rollback", fault.Version == 0);
        failed = false;
        try { await Run(s => s.ConfirmAccountReviewAsync(actor, academy, registration, fault.Id, 0, fault.ReviewHash, true), new OperationCommitFault()); } catch (InvalidOperationException ex) when (ex.Message == "Synthetic operation commit failure after SQL writes") { failed = true; }
        await using (var db = Open()) Check("confirmation-audit-commit-rollback", failed && await db.MeetingAccountReviews.AnyAsync(x => x.Id == fault.Id && x.Version == 0 && x.ConfirmedAtUtc == null) && await db.AuditLogs.CountAsync(x => x.EntityId == fault.Id) == 1);
        failed = false;
        try { await Run(s => s.ReadAccountReviewAsync(actor, academy, registration, fault.Id), new OperationCommitFault()); } catch (InvalidOperationException ex) when (ex.Message == "Synthetic operation commit failure after SQL writes") { failed = true; }
        await using (var db = Open()) Check("read-audit-failure-no-identity-release", failed && await db.AuditLogs.CountAsync(x => x.EntityId == fault.Id) == 1);
        Check("other-actor-cannot-confirm", (await Run(s => s.ConfirmAccountReviewAsync(Actor(otherId), academy, registration, fault.Id, 0, fault.ReviewHash, true))).StatusCode == 404);
        Check("foreign-academy-cannot-confirm", (await Run(s => s.ConfirmAccountReviewAsync(actor, foreign, registration, fault.Id, 0, fault.ReviewHash, true))).StatusCode == 403);
        Check("wrong-review-hash-denied", (await Run(s => s.ConfirmAccountReviewAsync(actor, academy, registration, fault.Id, 0, new string('A', 64), true))).StatusCode == 409);
        Check("confirmation-not-implied", (await Run(s => s.ConfirmAccountReviewAsync(actor, academy, registration, fault.Id, 0, fault.ReviewHash, false))).StatusCode == 400);
        clock.Now = clock.Now.AddMinutes(5);
        Check("exact-expiry-cannot-confirm", (await Run(s => s.ConfirmAccountReviewAsync(actor, academy, registration, fault.Id, 0, fault.ReviewHash, true))).StatusCode == 410);
        var rolePermit = await Quarantine(registration); var roleReview = await Prepare(registration, rolePermit);
        using (var scope = factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(); var user = (await users.FindByIdAsync(actorId.ToString()))!;
            if (!(await users.RemoveFromRoleAsync(user, "AcademyAdmin")).Succeeded) throw new InvalidOperationException("Review role fixture failed");
        }
        Check("revoked-role-cannot-confirm", (await Run(s => s.ConfirmAccountReviewAsync(actor, academy, registration, roleReview.Id, 0, roleReview.ReviewHash, true))).StatusCode == 403);
        MeetingAccountReview source; await using (var db = Open()) source = await db.MeetingAccountReviews.AsNoTracking().SingleAsync(x => x.Id == roleReview.Id);
        async Task Constraint(string label, Action<MeetingAccountReview> mutate, bool fresh, int expected)
        {
            await using var db = Open(); var exchangeId = source.ExchangeId;
            if (fresh)
            {
                var attempt = new MeetingOAuthAttempt { AcademyId = academy, ActorUserId = actorId, BindingId = source.BindingId, Provider = "Zoom", Generation = source.Generation,
                    StateHash = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)), ProtectedEnvelope = "synthetic-constraint-only", ExpiresAtUtc = clock.Now.UtcDateTime.AddMinutes(5) };
                var exchange = new MeetingOAuthExchange { AcademyId = academy, ActorUserId = actorId, AttemptId = attempt.Id, BindingId = source.BindingId, Provider = "Zoom", Generation = source.Generation,
                    BindingVersion = source.BindingVersion, Fence = Guid.NewGuid(), RegistrationFingerprint = source.RegistrationFingerprint, ProtectedEnvelope = "synthetic-constraint-only", DeadlineUtc = clock.Now.UtcDateTime.AddSeconds(30) };
                db.AddRange(attempt, exchange); await db.SaveChangesAsync(); exchangeId = exchange.Id;
            }
            var row = new MeetingAccountReview { AcademyId = academy, ActorUserId = actorId, ExchangeId = exchangeId, BindingId = source.BindingId, Provider = "Zoom", Generation = source.Generation,
                BindingVersion = source.BindingVersion, RegistrationFingerprint = source.RegistrationFingerprint, ReviewHash = new string('F', 64), ProtectedEnvelope = "synthetic-constraint-only", ExpiresAtUtc = clock.Now.UtcDateTime.AddMinutes(5) };
            mutate(row); db.Add(row); var rejected = false;
            try { await db.SaveChangesAsync(); } catch (DbUpdateException ex) when (ex.InnerException is SqlException sql && sql.Number == expected) { rejected = true; }
            Check(label, rejected);
        }
        await Constraint("unique-exchange-review", _ => { }, false, 2601);
        await Constraint("cross-academy-FK", x => x.AcademyId = foreign, true, 547);
        await Constraint("cross-actor-FK", x => x.ActorUserId = otherId, true, 547);
        await Constraint("cross-provider-FK", x => x.Provider = "Microsoft365", true, 547);
        await Constraint("connected-state-refused", x => x.Status = "Connected", true, 547);
        await Constraint("zero-generation-refused", x => x.Generation = 0, true, 547);
        if (checks.Count != 37) throw new InvalidOperationException("Account review case count mismatch " + checks.Count);
        var sources = new[] { "apps/api/Infrastructure/Meetings/MeetingAccountReviewStore.cs", "apps/api/Infrastructure/Meetings/MeetingOAuthAccountProbe.cs", "apps/api/Domain/Entities/MeetingAccountReview.cs", "apps/api/Data/AcademyDeskDbContext.cs", "QA/tools/SqlHarness/MeetingAccountReviewRegression.cs", "apps/api/Migrations/AcademyDeskDb/20261015175401_MeetingAccountReviewConfirmation.cs" }.ToDictionary(p => p, p => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(p))));
        var evidence = Path.Combine("QA", "EVIDENCE", "meeting-account-review-sql-" + manifest.RunId.ToString("N")); Directory.CreateDirectory(evidence);
        await File.WriteAllTextAsync(Path.Combine(evidence, "result.json"), JsonSerializer.Serialize(new { checks, sources, scope = "Real owned SQL/Identity durable account review/confirmation with fake provider only; not live account ownership, issuer, consent, entitlement or Connected acceptance" }, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine("MEETING ACCOUNT REVIEW PASS:37SQL/Identity cases; confirmation evidence only, no Connected/provider access.");
    }
}
