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
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;

internal static partial class SqlHarnessEntryPoint
{
    private static async Task VerifyMeetingOAuthMigrationAsync(AcademyDeskDbContext db)
    {
        var migrations = db.Database.GetMigrations().ToArray();
        var latest = migrations.Single(x => x.EndsWith("_MeetingProviderOAuthIntents", StringComparison.Ordinal));
        var previous = migrations[Array.IndexOf(migrations, latest) - 1]; var migrator = db.GetService<IMigrator>();
        await migrator.MigrateAsync(latest);
        var binding = new MeetingProviderBinding { AcademyId = Guid.NewGuid(), Provider = "Zoom", Generation = 1, Version = 1 };
        var attempt = new MeetingOAuthAttempt { AcademyId = binding.AcademyId, BindingId = binding.Id, ActorUserId = Guid.NewGuid(), Provider = "Zoom", Generation = 1,
            StateHash = new string('A', 64), ProtectedEnvelope = "synthetic-migration-only", ExpiresAtUtc = DateTime.UtcNow.AddMinutes(5) };
        db.AddRange(binding, attempt); await db.SaveChangesAsync(); var blocked = false;
        try { await migrator.MigrateAsync(previous); } catch (SqlException ex) when (ex.Number == 51000) { blocked = true; }
        if (!blocked || await db.MeetingProviderBindings.CountAsync() != 1 || await db.MeetingOAuthAttempts.CountAsync() != 1) throw new InvalidOperationException("OAuth downgrade lost authorization evidence");
        db.Remove(attempt); await db.SaveChangesAsync(); db.Remove(binding); await db.SaveChangesAsync(); // Exact synthetic probes only, owned database.
        await migrator.MigrateAsync(previous); await migrator.MigrateAsync(latest);
        Console.WriteLine("MEETING OAUTH MIGRATION PASS: upgrade, populated-down refusal retains both rows, empty down/up.");
    }

    private static async Task VerifyMeetingOAuthAttemptsAsync(QaApiFactory factory, QaRunManifest manifest)
    {
        Guid academy, foreign;
        string connection;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); connection = db.Database.GetConnectionString()!;
            academy = await db.Academies.Where(x => x.Name == "Synthetic Academy A").Select(x => x.Id).SingleAsync();
            foreign = await db.Academies.Where(x => x.Name == "Synthetic Academy B").Select(x => x.Id).SingleAsync();
        }
        var target = new SqlConnectionStringBuilder(connection);
        if (target.DataSource != manifest.SqlServer || target.InitialCatalog != manifest.Database || target.UserID != manifest.RuntimeLogin) throw new InvalidOperationException("OAuth requires exact owned runtime target");
        var actorId = await CreateAccessActorAsync(factory, "qa-oauth-admin@example.invalid", "AcademyAdmin", academy);
        var otherId = await CreateAccessActorAsync(factory, "qa-oauth-other@example.invalid", "AcademyAdmin", academy);
        ClaimsPrincipal Principal(Guid id) => new(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, id.ToString()) }, "synthetic-oauth-fixture"));
        var actor = Principal(actorId); var clock = new OperationClock();
        var protection = factory.Services.GetRequiredService<IDataProtectionProvider>();
        string RandomValue() => WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        var browser = RandomValue(); var registration = new string('A', 64);
        AcademyDeskDbContext Open(IInterceptor? interceptor = null)
        {
            var options = new DbContextOptionsBuilder<AcademyDeskDbContext>().UseSqlServer(connection);
            if (interceptor is not null) options.AddInterceptors(interceptor);
            return new(options.Options);
        }
        async Task<MeetingOAuthOutcome> Run(Func<MeetingOAuthAttemptStore, Task<MeetingOAuthOutcome>> work, IInterceptor? interceptor = null)
        {
            using var scope = factory.Services.CreateScope(); await using var db = Open(interceptor);
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            return await work(new(db, users, new(db, users, clock), protection, clock));
        }
        Task<MeetingOAuthOutcome> Prepare(string provider, int version, IInterceptor? interceptor = null)
            => Run(store => store.PrepareAsync(actor, academy, provider, version, browser, registration), interceptor);
        Task<MeetingOAuthOutcome> Claim(string provider, string state, IInterceptor? interceptor = null)
            => Run(store => store.ClaimAsync(actor, academy, provider, state, browser, registration), interceptor);
        var checks = new List<string>();
        void Check(string label, bool result) { if (!result) throw new InvalidOperationException("MEETING OAUTH " + label); checks.Add(label); Console.WriteLine("MEETING OAUTH CASE " + label + " PASS"); }

        var materials = new Dictionary<string, MeetingOAuthMaterial>();
        foreach (var provider in new[] { "GoogleWorkspace", "Microsoft365", "Zoom" })
        {
            var prepared = await Prepare(provider, 0); Check(provider + "-prepare", prepared.StatusCode == 201); materials[provider] = prepared.Material!;
            var claimed = await Claim(provider, prepared.Material!.State);
            Check(provider + "-claim-PKCE", claimed.StatusCode == 200 && claimed.Material!.Verifier is not null &&
                prepared.Material.Challenge == WebEncoders.Base64UrlEncode(SHA256.HashData(Encoding.ASCII.GetBytes(claimed.Material.Verifier))));
            Check(provider + "-replay-denied", (await Claim(provider, prepared.Material.State)).StatusCode == 409);
        }
        await using (var db = Open())
        {
            Check("three-independent-bindings-not-connected", await db.MeetingProviderBindings.CountAsync(x => x.AcademyId == academy && x.Status == "VerificationRequired" && x.Version == 2 && x.Generation == 1) == 3);
            var rows = await db.MeetingOAuthAttempts.Where(x => x.AcademyId == academy).ToListAsync();
            Check("no-raw-state-browser-or-verifier-in-evidence", rows.All(row => !row.ProtectedEnvelope.Contains(browser, StringComparison.Ordinal) && materials.Values.All(m => !row.ProtectedEnvelope.Contains(m.State, StringComparison.Ordinal))) && materials.Values.All(m => JsonSerializer.Serialize(m) == "{}" && !m.ToString().Contains(m.State, StringComparison.Ordinal)));
        }
        var old = (await Prepare("GoogleWorkspace", 2)).Material!;
        var current = (await Prepare("GoogleWorkspace", 3)).Material!;
        Check("superseded-pending-intent-denied", (await Claim("GoogleWorkspace", old.State)).Code == "ConnectionGenerationChanged");
        var barrier = new ClaimBarrier(); var race = await Task.WhenAll(Claim("GoogleWorkspace", current.State, barrier), Claim("GoogleWorkspace", current.State, barrier));
        Check("two-callbacks-exactly-one-claim", race.Count(x => x.StatusCode == 200) == 1 && race.Count(x => x.StatusCode == 409) == 1);
        await using (var db = Open())
        {
            var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(current.State)));
            var row = await db.MeetingOAuthAttempts.SingleAsync(x => x.StateHash == hash);
            Check("one-claim-audit-after-callback-race", row.Status == "Claimed" && row.Version == 1 && await db.AuditLogs.CountAsync(x => x.EntityId == row.Id && x.Action == "MeetingOAuthIntentClaimedNotConnected") == 1);
        }
        var fault = (await Prepare("Zoom", 2)).Material!;
        Check("wrong-browser-denied", (await Run(s => s.ClaimAsync(actor, academy, "Zoom", fault.State, RandomValue(), registration))).StatusCode == 409);
        Check("changed-registration-denied", (await Run(s => s.ClaimAsync(actor, academy, "Zoom", fault.State, browser, new string('B', 64)))).StatusCode == 409);
        Check("cross-provider-state-denied", (await Claim("Microsoft365", fault.State)).StatusCode == 404);
        Check("foreign-academy-denied", (await Run(s => s.ClaimAsync(actor, foreign, "Zoom", fault.State, browser, registration))).StatusCode == 403);
        Check("other-admin-state-denied", (await Run(s => s.ClaimAsync(Principal(otherId), academy, "Zoom", fault.State, browser, registration))).StatusCode == 404);
        clock.Now = clock.Now.AddMinutes(5);
        Check("exact-expiry-denied", (await Claim("Zoom", fault.State)).StatusCode == 410);
        clock.Now = clock.Now.AddMinutes(-5);
        var failed = false;
        try { await Claim("Zoom", fault.State, new OperationCommitFault()); }
        catch (InvalidOperationException ex) when (ex.Message == "Synthetic operation commit failure after SQL writes") { failed = true; }
        await using (var db = Open())
        {
            var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(fault.State)));
            var row = await db.MeetingOAuthAttempts.SingleAsync(x => x.StateHash == hash);
            var binding = await db.MeetingProviderBindings.SingleAsync(x => x.Id == row.BindingId);
            Check("post-SQL-commit-fault-rollback", failed && row.Status == "Pending" && row.Version == 0 && binding.Status == "AuthorizationPending" && binding.Version == 3 && !await db.AuditLogs.AnyAsync(x => x.EntityId == row.Id && x.Action == "MeetingOAuthIntentClaimedNotConnected"));
        }
        Check("callback-claim-after-confirmed-rollback", (await Claim("Zoom", fault.State)).StatusCode == 200);
        var tampered = (await Prepare("Microsoft365", 2)).Material!;
        await using (var db = Open())
        {
            var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(tampered.State)));
            (await db.MeetingOAuthAttempts.SingleAsync(x => x.StateHash == hash)).ProtectedEnvelope = "synthetic-tamper"; await db.SaveChangesAsync();
        }
        Check("tampered-envelope-denied", (await Claim("Microsoft365", tampered.State)).StatusCode == 409);

        var preparationBarrier = new ClaimBarrier();
        var prepares = await Task.WhenAll(Prepare("GoogleWorkspace", 5, preparationBarrier), Prepare("GoogleWorkspace", 5, preparationBarrier));
        Check("two-reconnect-intents-one-winner", prepares.Count(x => x.StatusCode == 201) == 1 && prepares.Count(x => x.StatusCode == 409) == 1);
        await using (var db = Open())
        {
            var binding = await db.MeetingProviderBindings.SingleAsync(x => x.AcademyId == academy && x.Provider == "GoogleWorkspace");
            Check("one-generation-and-audit-for-reconnect-race", binding.Generation == 4 && binding.Version == 6 && await db.MeetingOAuthAttempts.CountAsync(x => x.BindingId == binding.Id) == 4 &&
                await db.AuditLogs.CountAsync(x => x.ActorUserId == actorId && x.Action == "MeetingOAuthIntentPrepared" && db.MeetingOAuthAttempts.Any(a => a.Id == x.EntityId && a.BindingId == binding.Id)) == 4);
        }
        using (var scope = factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = (await users.FindByIdAsync(actorId.ToString()))!;
            if (!(await users.RemoveFromRoleAsync(user, "AcademyAdmin")).Succeeded) throw new InvalidOperationException("OAuth revoke fixture failed");
        }
        Check("revoked-actor-cannot-claim", (await Claim("GoogleWorkspace", prepares.Single(x => x.StatusCode == 201).Material!.State)).StatusCode == 403);

        MeetingProviderBinding zoom;
        await using (var db = Open()) zoom = await db.MeetingProviderBindings.AsNoTracking().SingleAsync(x => x.AcademyId == academy && x.Provider == "Zoom");
        async Task Constraint(string label, Action<AcademyDeskDbContext> stage, int number)
        {
            var rejected = false; await using var db = Open(); stage(db);
            try { await db.SaveChangesAsync(); } catch (DbUpdateException ex) when (ex.InnerException is SqlException sql && sql.Number == number) { rejected = true; }
            Check(label, rejected);
        }
        MeetingOAuthAttempt Probe(Guid a, string p) => new() { AcademyId = a, BindingId = zoom.Id, ActorUserId = actorId, Provider = p, Generation = 1, StateHash = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)), ProtectedEnvelope = "synthetic-constraint-only", ExpiresAtUtc = clock.Now.UtcDateTime.AddMinutes(5) };
        await Constraint("cross-academy-binding-FK", db => db.Add(Probe(foreign, "Zoom")), 547);
        await Constraint("cross-provider-binding-FK", db => db.Add(Probe(academy, "Microsoft365")), 547);
        await Constraint("duplicate-provider-binding", db => db.Add(new MeetingProviderBinding { AcademyId = academy, Provider = "Zoom" }), 2601);
        await Constraint("unverified-cannot-be-labelled-connected", db => db.Add(new MeetingProviderBinding { AcademyId = foreign, Provider = "Zoom", Status = "Connected" }), 547);
        await Constraint("global-state-collision-refused", db => { var probe = Probe(academy, "Zoom"); probe.StateHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(fault.State))); db.Add(probe); }, 2601);
        await using (var db = Open()) Check("no-provider-side-effects", !await db.Notifications.AnyAsync() && !await db.MeetingProviderBindings.AnyAsync(x => x.Status == "Connected"));
        if (checks.Count != 32) throw new InvalidOperationException("OAuth SQL case count mismatch " + checks.Count);
        var evidence = Path.Combine("QA", "EVIDENCE", "meeting-oauth-sql-" + manifest.RunId.ToString("N")); Directory.CreateDirectory(evidence);
        var sources = new[] { "apps/api/Infrastructure/Meetings/MeetingOAuthAttemptStore.cs", "apps/api/Domain/Entities/MeetingProviderBinding.cs", "apps/api/Data/AcademyDeskDbContext.cs", "QA/tools/SqlHarness/MeetingOAuthRegression.cs" }
            .ToDictionary(p => p, p => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(p))));
        await File.WriteAllTextAsync(Path.Combine(evidence, "result.json"), JsonSerializer.Serialize(new { checks, sources, scope = "Internal intent/claim persistence with real Identity and owned SQL; NOT public OAuth/cookies/token exchange or connected accounts" }, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine("MEETING OAUTH PASS:32realSQL/Identity cases, no public callbacks, token exchange or provider access.");
    }
}
