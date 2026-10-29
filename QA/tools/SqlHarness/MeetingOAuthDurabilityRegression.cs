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
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;

internal static partial class SqlHarnessEntryPoint
{
    private static async Task VerifyMeetingOAuthExchangeMigrationAsync(AcademyDeskDbContext db)
    {
        var migrations = db.Database.GetMigrations().ToArray(); var latest = migrations.Single(x => x.EndsWith("_MeetingOAuthExchangeQuarantine", StringComparison.Ordinal));
        var previous = migrations[Array.IndexOf(migrations, latest) - 1]; var migrator = db.GetService<IMigrator>(); await migrator.MigrateAsync(latest);
        var binding = new MeetingProviderBinding { AcademyId = Guid.NewGuid(), Provider = "Zoom", Generation = 1, Version = 1 };
        var attempt = new MeetingOAuthAttempt { AcademyId = binding.AcademyId, ActorUserId = Guid.NewGuid(), BindingId = binding.Id, Provider = "Zoom", Generation = 1,
            StateHash = new string('E', 64), ProtectedEnvelope = "synthetic-migration-only", ExpiresAtUtc = DateTime.UtcNow.AddMinutes(5) };
        var row = new MeetingOAuthExchange { AcademyId = binding.AcademyId, ActorUserId = attempt.ActorUserId, BindingId = binding.Id, AttemptId = attempt.Id,
            Provider = "Zoom", Generation = 1, BindingVersion = 1, Fence = Guid.NewGuid(), RegistrationFingerprint = new string('F', 64), DeadlineUtc = DateTime.UtcNow.AddSeconds(30), ProtectedEnvelope = "synthetic-migration-only" };
        db.AddRange(binding, attempt, row); await db.SaveChangesAsync(); var blocked = false;
        try { await migrator.MigrateAsync(previous); } catch (SqlException ex) when (ex.Number == 51000) { blocked = true; }
        if (!blocked || !await db.MeetingOAuthExchanges.AnyAsync(x => x.Id == row.Id) || !await db.MeetingOAuthAttempts.AnyAsync(x => x.Id == attempt.Id)) throw new InvalidOperationException("Exchange downgrade lost evidence");
        db.Remove(row); await db.SaveChangesAsync(); db.Remove(attempt); await db.SaveChangesAsync(); db.Remove(binding); await db.SaveChangesAsync(); // Exact owned migration probes only.
        await migrator.MigrateAsync(previous); await migrator.MigrateAsync(latest);
        Console.WriteLine("MEETING OAUTH EXCHANGE MIGRATION PASS: upgrade, populated-down refusal, exact probe removal and empty down/up.");
    }
    private sealed class ExchangeTransport(Func<Task> before, byte[] bytes) : HttpMessageHandler
    {
        public int Calls;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            Calls++; await before(); var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(bytes) };
            response.Content.Headers.ContentType = new("application/json"); return response;
        }
    }
    private static async Task VerifyMeetingOAuthDurabilityAsync(QaApiFactory factory, QaRunManifest manifest)
    {
        Guid academy, foreign; string connection;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); connection = db.Database.GetConnectionString()!;
            academy = await db.Academies.Where(x => x.Name == "Synthetic Academy A").Select(x => x.Id).SingleAsync(); foreign = await db.Academies.Where(x => x.Name == "Synthetic Academy B").Select(x => x.Id).SingleAsync();
        }
        var target = new SqlConnectionStringBuilder(connection);
        if (target.DataSource != manifest.SqlServer || target.InitialCatalog != manifest.Database || target.UserID != manifest.RuntimeLogin) throw new InvalidOperationException("Exchange persistence requires exact owned target");
        var actorId = await CreateAccessActorAsync(factory, "qa-oauth-vault@example.invalid", "AcademyAdmin", academy);
        var otherId = await CreateAccessActorAsync(factory, "qa-oauth-vault-other@example.invalid", "AcademyAdmin", academy);
        ClaimsPrincipal Actor(Guid id) => new(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, id.ToString()) }, "synthetic-vault"));
        var actor = Actor(actorId); var clock = new OperationClock(); var keys = factory.Services.GetRequiredService<IDataProtectionProvider>();
        const string access = "synthetic-durable-access", refresh = "synthetic-durable-refresh", code = "synthetic-durable-code";
        MeetingOAuthRegistration Registration(string provider = "Zoom")
        {
            var profile = provider switch { "GoogleWorkspace" => "google-calendar-owned-web/1", "Microsoft365" => "microsoft-online-meetings-work/1", _ => "zoom-public-pkce/1" };
            return MeetingOAuthRegistrationRegistry.Create("https://api.academy.invalid", "https://app.academy.invalid", [new(provider, 1, "synthetic-client", "https://api.academy.invalid" + MeetingOAuthRegistrationRegistry.CallbackPath(provider),
                "https://app.academy.invalid/communication-settings", profile, provider == "Microsoft365" ? "019acc9a-6200-7000-8000-000000000001" : null)]).Resolve(provider)!;
        }
        AcademyDeskDbContext Open(IInterceptor? interceptor = null) { var options = new DbContextOptionsBuilder<AcademyDeskDbContext>().UseSqlServer(connection); if (interceptor is not null) options.AddInterceptors(interceptor); return new(options.Options); }
        async Task<T> Run<T>(Func<MeetingOAuthAttemptStore, Task<T>> work, IInterceptor? interceptor = null)
        {
            using var scope = factory.Services.CreateScope(); await using var db = Open(interceptor); var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            return await work(new(db, users, new(db, users, clock), keys, clock));
        }
        async Task<MeetingOAuthOutcome> Claim(MeetingOAuthRegistration registration)
        {
            int version; await using (var db = Open()) version = await db.MeetingProviderBindings.Where(x => x.AcademyId == academy && x.Provider == registration.Provider).Select(x => x.Version).SingleAsync();
            var browser = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
            using var scope = factory.Services.CreateScope(); await using var current = Open(); var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(); var store = new MeetingOAuthAttemptStore(current, users, new(current, users, clock), keys, clock);
            var prepared = await store.PrepareAsync(actor, academy, registration.Provider, version, browser, registration.Fingerprint);
            if (prepared.StatusCode != 201 || (await store.CaptureAsync(academy, registration.Provider, prepared.Material!.State, browser, registration.Fingerprint, code, false)).StatusCode != 202) throw new InvalidOperationException("Vault claim fixture failed");
            var result = await store.ResumeAsync(actor, academy, registration.Provider, prepared.Material.State, browser, registration.Fingerprint);
            if (result.StatusCode != 200) throw new InvalidOperationException("Vault resume fixture failed"); return result;
        }
        MeetingOAuthExchangeOutcome Tokens(MeetingOAuthRegistration registration) => new(200, "TokensReceivedAccountUnverified", Tokens: new(access, refresh, clock.Now.AddHours(1), registration.Flow!.RequiredTokenScopes));
        var checks = new List<string>();
        void Check(string label, bool passed) { if (!passed) throw new InvalidOperationException("MEETING OAUTH DURABILITY " + label); checks.Add(label); Console.WriteLine("MEETING OAUTH DURABILITY CASE " + label + " PASS"); }
        foreach (var provider in new[] { "GoogleWorkspace", "Microsoft365", "Zoom" })
        {
            var registration = Registration(provider); var claim = await Claim(registration); var start = await Run(s => s.BeginExchangeAsync(actor, academy, registration, claim));
            Check(provider + "-durable-start", start.StatusCode == 201 && start.Permit is not null);
            Check(provider + "-restart-start-replay-denied", (await Run(s => s.BeginExchangeAsync(actor, academy, registration, claim))).StatusCode == 409);
            Check(provider + "-tokens-quarantined-not-connected", (await Run(s => s.FinishExchangeAsync(actor, academy, registration, start.Permit!, Tokens(registration)))).StatusCode == 200);
            var read = await Run(s => s.ReadQuarantinedTokensAsync(actor, academy, registration, start.Permit!));
            Check(provider + "-private-authorized-read", read.StatusCode == 200 && read.Tokens!.AccessToken == access && !JsonSerializer.Serialize(read).Contains(access) && !read.ToString().Contains(refresh));
            Check(provider + "-other-actor-read-denied", (await Run(s => s.ReadQuarantinedTokensAsync(Actor(otherId), academy, registration, start.Permit!))).StatusCode == 404);
            await using var db = Open(); var row = await db.MeetingOAuthExchanges.SingleAsync(x => x.Id == start.Permit!.Id);
            Check(provider + "-ciphertext-audit-no-connection", !row.ProtectedEnvelope.Contains(access) && !row.ProtectedEnvelope.Contains(refresh) && !row.ProtectedEnvelope.Contains(code) &&
                row.Status == "TokensQuarantined" && row.Version == 1 && await db.AuditLogs.CountAsync(x => x.EntityId == row.Id) == 3 &&
                (await db.MeetingProviderBindings.SingleAsync(x => x.Id == row.BindingId)).Status == "VerificationRequired");
        }
        var reg = Registration(); var raceClaim = await Claim(reg); var startBarrier = new ClaimBarrier();
        var starts = await Task.WhenAll(Run(s => s.BeginExchangeAsync(actor, academy, reg, raceClaim), startBarrier), Run(s => s.BeginExchangeAsync(actor, academy, reg, raceClaim), startBarrier));
        Check("two-starts-exactly-one-durable-permit", starts.Select(x => x.StatusCode).Order().SequenceEqual(new[] { 201, 409 }));
        var permit = starts.Single(x => x.Permit is not null).Permit!; var finishBarrier = new ClaimBarrier();
        var finishes = await Task.WhenAll(Run(s => s.FinishExchangeAsync(actor, academy, reg, permit, Tokens(reg)), finishBarrier), Run(s => s.FinishExchangeAsync(actor, academy, reg, permit, Tokens(reg)), finishBarrier));
        Check("two-finishes-one-token-commit", finishes.Select(x => x.StatusCode).Order().SequenceEqual(new[] { 200, 409 }));
        await using (var db = Open()) Check("race-one-row-two-audits", await db.MeetingOAuthExchanges.CountAsync(x => x.Id == permit.Id) == 1 && await db.AuditLogs.CountAsync(x => x.EntityId == permit.Id) == 2);
        var faultClaim = await Claim(reg); var failed = false; var networkCalls = 0;
        using (var scope = factory.Services.CreateScope())
        {
            await using var db = Open(new OperationCommitFault()); var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var bytes = JsonSerializer.SerializeToUtf8Bytes(new { access_token = access, refresh_token = refresh, token_type = "Bearer", expires_in = 3600, scope = string.Join(' ', reg.Flow!.RequiredTokenScopes) });
            var handler = new ExchangeTransport(() => { networkCalls++; return Task.CompletedTask; }, bytes); using var transport = new HttpMessageInvoker(handler);
            var store = new MeetingOAuthAttemptStore(db, users, new(db, users, clock), keys, clock); var coordinator = new MeetingOAuthExchangeCoordinator(store, new(db, users, new(db, users, clock), transport, clock));
            try { await coordinator.ExecuteAsync(actor, academy, reg, faultClaim, null); } catch (InvalidOperationException ex) when (ex.Message == "Synthetic operation commit failure after SQL writes") { failed = true; }
        }
        await using (var db = Open())
        {
            var hash = Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(faultClaim.Material!.State)));
            var attemptId = await db.MeetingOAuthAttempts.Where(a => a.StateHash == hash).Select(a => a.Id).SingleAsync();
            Check("start-commit-fault-no-send-no-row-no-audit", failed && networkCalls == 0 && !await db.MeetingOAuthExchanges.AnyAsync(x => x.AttemptId == attemptId) &&
                await db.AuditLogs.CountAsync(x => x.ActorUserId == actorId && x.Action == "MeetingOAuthExchangeStarted") == 4);
        }
        var faultStart = await Run(s => s.BeginExchangeAsync(actor, academy, reg, faultClaim)); Check("start-after-proved-rollback", faultStart.StatusCode == 201);
        failed = false;
        try { await Run(s => s.FinishExchangeAsync(actor, academy, reg, faultStart.Permit!, Tokens(reg)), new OperationCommitFault()); } catch (InvalidOperationException ex) when (ex.Message == "Synthetic operation commit failure after SQL writes") { failed = true; }
        await using (var db = Open())
        {
            var row = await db.MeetingOAuthExchanges.SingleAsync(x => x.Id == faultStart.Permit!.Id);
            Check("token-commit-fault-row-audit-rollback", failed && row.Status == "Started" && row.Version == 0 && await db.AuditLogs.CountAsync(x => x.EntityId == row.Id) == 1);
        }
        clock.Now = clock.Now.AddSeconds(30);
        Check("restart-expired-exchange-unknown", await Run(s => s.RecoverExpiredExchangeAsync(academy, faultStart.Permit!.Id)));
        Check("unknown-exchange-never-restarted", (await Run(s => s.BeginExchangeAsync(actor, academy, reg, faultClaim))).StatusCode == 409);
        Check("unknown-credentials-not-readable", (await Run(s => s.ReadQuarantinedTokensAsync(actor, academy, reg, faultStart.Permit!))).StatusCode == 409);
        Check("recovery-idempotent", !await Run(s => s.RecoverExpiredExchangeAsync(academy, faultStart.Permit!.Id)));
        var currentClaim = await Claim(reg); var currentStart = await Run(s => s.BeginExchangeAsync(actor, academy, reg, currentClaim)); var currentPermit = currentStart.Permit!;
        Check("foreign-finish-denied", (await Run(s => s.FinishExchangeAsync(actor, foreign, reg, currentPermit, Tokens(reg)))).StatusCode == 403);
        Check("wrong-fence-finish-denied", (await Run(s => s.FinishExchangeAsync(actor, academy, reg, currentPermit with { Fence = Guid.NewGuid() }, Tokens(reg)))).StatusCode == 409);
        Check("finish-after-denials", (await Run(s => s.FinishExchangeAsync(actor, academy, reg, currentPermit, Tokens(reg)))).StatusCode == 200);
        var readFailed = false;
        try { await Run(s => s.ReadQuarantinedTokensAsync(actor, academy, reg, currentPermit), new OperationCommitFault()); } catch (InvalidOperationException ex) when (ex.Message == "Synthetic operation commit failure after SQL writes") { readFailed = true; }
        await using (var db = Open()) Check("credential-read-audit-fault-no-release", readFailed && await db.AuditLogs.CountAsync(x => x.EntityId == currentPermit.Id) == 2);
        using (var scope = factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(); var user = (await users.FindByIdAsync(actorId.ToString()))!;
            if (!(await users.RemoveFromRoleAsync(user, "AcademyAdmin")).Succeeded) throw new InvalidOperationException("Vault revocation fixture failed");
        }
        Check("revoked-credential-read-denied", (await Run(s => s.ReadQuarantinedTokensAsync(actor, academy, reg, currentPermit))).StatusCode == 403);
        await using (var db = Open()) Check("no-provider-or-notification-side-effect", !await db.Notifications.AnyAsync() && !await db.MeetingProviderBindings.AnyAsync(x => x.Status == "Connected"));
        MeetingOAuthExchange source;
        await using (var db = Open()) source = await db.MeetingOAuthExchanges.AsNoTracking().SingleAsync(x => x.Id == currentPermit.Id);
        async Task Constraint(string label, Action<MeetingOAuthExchange> mutate, bool newAttempt, int expected)
        {
            await using var db = Open(); var attemptId = source.AttemptId;
            if (newAttempt)
            {
                var probe = new MeetingOAuthAttempt { AcademyId = academy, ActorUserId = actorId, BindingId = source.BindingId, Provider = "Zoom", Generation = source.Generation,
                    StateHash = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)), ProtectedEnvelope = "synthetic-constraint-only", ExpiresAtUtc = clock.Now.UtcDateTime.AddMinutes(5) };
                db.Add(probe); await db.SaveChangesAsync(); attemptId = probe.Id;
            }
            var row = new MeetingOAuthExchange { AcademyId = academy, ActorUserId = actorId, AttemptId = attemptId, BindingId = source.BindingId, Provider = "Zoom", Generation = source.Generation,
                BindingVersion = source.BindingVersion, Fence = Guid.NewGuid(), RegistrationFingerprint = source.RegistrationFingerprint, DeadlineUtc = clock.Now.UtcDateTime.AddSeconds(30), ProtectedEnvelope = "synthetic-constraint-only" };
            mutate(row); db.Add(row); var rejected = false;
            try { await db.SaveChangesAsync(); } catch (DbUpdateException ex) when (ex.InnerException is SqlException sql && sql.Number == expected) { rejected = true; }
            Check(label, rejected);
        }
        await Constraint("exchange-cross-academy-FK", x => x.AcademyId = foreign, true, 547);
        await Constraint("exchange-cross-actor-FK", x => x.ActorUserId = otherId, true, 547);
        await Constraint("exchange-cross-provider-FK", x => x.Provider = "Microsoft365", true, 547);
        await Constraint("exchange-duplicate-attempt-unique", _ => { }, false, 2601);
        await Constraint("exchange-connected-state-refused", x => x.Status = "Connected", true, 547);
        await Constraint("exchange-zero-generation-refused", x => x.Generation = 0, true, 547);
        if (checks.Count != 40) throw new InvalidOperationException("OAuth durability case count mismatch " + checks.Count);
        var evidence = Path.Combine("QA", "EVIDENCE", "meeting-oauth-durability-sql-" + manifest.RunId.ToString("N")); Directory.CreateDirectory(evidence);
        var sources = new[] { "apps/api/Infrastructure/Meetings/MeetingOAuthExchangeStore.cs", "apps/api/Infrastructure/Meetings/MeetingOAuthExchangeCoordinator.cs", "apps/api/Infrastructure/Meetings/MeetingOAuthAttemptStore.cs", "apps/api/Domain/Entities/MeetingOAuthExchange.cs", "apps/api/Data/AcademyDeskDbContext.cs", "QA/tools/SqlHarness/MeetingOAuthDurabilityRegression.cs" }
            .ToDictionary(p => p, p => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(p))));
        await File.WriteAllTextAsync(Path.Combine(evidence, "result.json"), JsonSerializer.Serialize(new { checks, sources, scope = "Real owned SQL/Identity durable exchange/quarantine and fake transport only; NOT live provider/account verification or connection acceptance" }, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine("MEETING OAUTH DURABILITY PASS:40SQL/Identity cases; no provider access or Connected state.");
    }
}
