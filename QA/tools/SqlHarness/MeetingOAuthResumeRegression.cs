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
    private static async Task VerifyMeetingOAuthCallbackMigrationAsync(AcademyDeskDbContext db)
    {
        var migrations = db.Database.GetMigrations().ToArray(); var latest = migrations.Single(x => x.EndsWith("_MeetingOAuthCallbackCapture", StringComparison.Ordinal));
        var previous = migrations[Array.IndexOf(migrations, latest) - 1]; var migrator = db.GetService<IMigrator>();
        await migrator.MigrateAsync(latest);
        var binding = new MeetingProviderBinding { AcademyId = Guid.NewGuid(), Provider = "Zoom", Generation = 1, Version = 1 };
        var attempt = new MeetingOAuthAttempt { AcademyId = binding.AcademyId, BindingId = binding.Id, ActorUserId = Guid.NewGuid(), Provider = "Zoom", Generation = 1,
            StateHash = new string('C', 64), ProtectedEnvelope = "synthetic-migration-only", ExpiresAtUtc = DateTime.UtcNow.AddMinutes(5) };
        var callback = new MeetingOAuthCallback { AcademyId = attempt.AcademyId, ActorUserId = attempt.ActorUserId, AttemptId = attempt.Id, Provider = "Zoom", ProtectedEnvelope = "synthetic-migration-only", ExpiresAtUtc = attempt.ExpiresAtUtc };
        db.AddRange(binding, attempt, callback); await db.SaveChangesAsync(); var blocked = false;
        try { await migrator.MigrateAsync(previous); } catch (SqlException ex) when (ex.Number == 51000) { blocked = true; }
        if (!blocked || !await db.MeetingOAuthCallbacks.AnyAsync(x => x.Id == callback.Id) || !await db.MeetingOAuthAttempts.AnyAsync(x => x.Id == attempt.Id)) throw new InvalidOperationException("Callback downgrade lost evidence");
        db.Remove(callback); await db.SaveChangesAsync(); attempt.Status = "Claimed"; attempt.Version = 2; attempt.ClaimedAtUtc = DateTime.UtcNow; await db.SaveChangesAsync(); blocked = false;
        try { await migrator.MigrateAsync(previous); } catch (SqlException ex) when (ex.Number == 51000) { blocked = true; }
        if (!blocked || !await db.MeetingOAuthAttempts.AnyAsync(x => x.Id == attempt.Id && x.Version == 2)) throw new InvalidOperationException("Callback downgrade lost version2 evidence");
        db.Remove(attempt); await db.SaveChangesAsync(); db.Remove(binding); await db.SaveChangesAsync(); // Only these owned migration probes.
        await migrator.MigrateAsync(previous); await migrator.MigrateAsync(latest);
        Console.WriteLine("MEETING OAUTH CALLBACK MIGRATION PASS: upgrade, populated/version2 down refusal, exact probe removal, empty down/up.");
    }

    private static async Task VerifyMeetingOAuthResumeAsync(QaApiFactory factory, QaRunManifest manifest)
    {
        Guid academy, foreign; string connection;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); connection = db.Database.GetConnectionString()!;
            academy = await db.Academies.Where(x => x.Name == "Synthetic Academy A").Select(x => x.Id).SingleAsync();
            foreign = await db.Academies.Where(x => x.Name == "Synthetic Academy B").Select(x => x.Id).SingleAsync();
        }
        var target = new SqlConnectionStringBuilder(connection);
        if (target.DataSource != manifest.SqlServer || target.InitialCatalog != manifest.Database || target.UserID != manifest.RuntimeLogin) throw new InvalidOperationException("Resume requires owned runtime SQL target");
        var id = await CreateAccessActorAsync(factory, "qa-oauth-resume@example.invalid", "AcademyAdmin", academy);
        var other = await CreateAccessActorAsync(factory, "qa-oauth-resume-other@example.invalid", "AcademyAdmin", academy);
        ClaimsPrincipal Actor(Guid user) => new(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, user.ToString()) }, "synthetic-resume"));
        var actor = Actor(id); var clock = new OperationClock(); var keys = factory.Services.GetRequiredService<IDataProtectionProvider>();
        var registration = new string('C', 64); const string code = "synthetic-captured-private-code";
        AcademyDeskDbContext Open(IInterceptor? interceptor = null)
        {
            var options = new DbContextOptionsBuilder<AcademyDeskDbContext>().UseSqlServer(connection); if (interceptor is not null) options.AddInterceptors(interceptor); return new(options.Options);
        }
        async Task<MeetingOAuthOutcome> Run(Func<MeetingOAuthAttemptStore, Task<MeetingOAuthOutcome>> work, IInterceptor? interceptor = null)
        {
            using var scope = factory.Services.CreateScope(); await using var db = Open(interceptor); var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            return await work(new(db, users, new(db, users, clock), keys, clock));
        }
        async Task<(string State, string Browser)> Prepare(string provider = "Zoom")
        {
            int version; await using (var db = Open()) version = await db.MeetingProviderBindings.Where(x => x.AcademyId == academy && x.Provider == provider).Select(x => x.Version).SingleAsync();
            var browser = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
            var result = await Run(s => s.PrepareAsync(actor, academy, provider, version, browser, registration));
            if (result.StatusCode != 201) throw new InvalidOperationException("Resume fixture prepare failed"); return (result.Material!.State, browser);
        }
        Task<MeetingOAuthOutcome> Capture((string State, string Browser) value, string provider = "Zoom", IInterceptor? interceptor = null)
            => Run(s => s.CaptureAsync(academy, provider, value.State, value.Browser, registration, code, false), interceptor);
        Task<MeetingOAuthOutcome> Resume((string State, string Browser) value, string provider = "Zoom", IInterceptor? interceptor = null)
            => Run(s => s.ResumeAsync(actor, academy, provider, value.State, value.Browser, registration), interceptor);
        var checks = new List<string>();
        void Check(string label, bool passed) { if (!passed) throw new InvalidOperationException("MEETING OAUTH RESUME " + label); checks.Add(label); Console.WriteLine("MEETING OAUTH RESUME CASE " + label + " PASS"); }
        foreach (var provider in new[] { "GoogleWorkspace", "Microsoft365", "Zoom" })
        {
            var value = await Prepare(provider); Check(provider + "-capture-quarantine", (await Capture(value, provider)).StatusCode == 202);
            Guid callbackId;
            await using (var db = Open())
            {
                var row = await db.MeetingOAuthCallbacks.SingleAsync(x => x.ActorUserId == id && x.Provider == provider); callbackId = row.Id;
                Check(provider + "-encrypted-code-no-authority", !row.ProtectedEnvelope.Contains(code) && row.Status == "Captured" && (await db.MeetingProviderBindings.SingleAsync(x => x.AcademyId == academy && x.Provider == provider)).Status == "AuthorizationPending" &&
                    (await db.AuditLogs.SingleAsync(x => x.EntityId == callbackId)).ActorUserId is null);
            }
            Check(provider + "-anonymous-resume-denied", (await Run(s => s.ResumeAsync(new(new ClaimsIdentity()), academy, provider, value.State, value.Browser, registration))).StatusCode == 401);
            Check(provider + "-direct-claim-bypass-denied", (await Run(s => s.ClaimAsync(actor, academy, provider, value.State, value.Browser, registration))).StatusCode == 409);
            var resumed = await Resume(value, provider);
            Check(provider + "-authenticated-code-once-not-connected", resumed.StatusCode == 200 && resumed.AuthorizationCode == code && resumed.Material!.Verifier is not null && !JsonSerializer.Serialize(resumed).Contains(code) && !resumed.ToString().Contains(code));
            await using (var db = Open())
            {
                var row = await db.MeetingOAuthCallbacks.SingleAsync(x => x.Id == callbackId); var attempt = await db.MeetingOAuthAttempts.SingleAsync(x => x.Id == row.AttemptId);
                var protector = keys.CreateProtector("academy.meeting-oauth-callback.v1", academy.ToString("D"), id.ToString("D"), provider, row.AttemptId.ToString("D"), row.Id.ToString("D"));
                using var clear = JsonDocument.Parse(protector.Unprotect(row.ProtectedEnvelope));
                Check(provider + "-durable-erasure-and-audit", row.Status == "Consumed" && row.Version == 1 && attempt.Status == "Claimed" && attempt.Version == 2 && clear.RootElement.GetProperty("AuthorizationCode").ValueKind == JsonValueKind.Null &&
                    await db.AuditLogs.CountAsync(x => (x.EntityId == row.Id || x.EntityId == attempt.Id)) == 4);
            }
            Check(provider + "-resume-replay-denied", (await Resume(value, provider)).StatusCode == 409);
        }
        var captureRace = await Prepare(); var barrier = new ClaimBarrier();
        var captures = await Task.WhenAll(Capture(captureRace, interceptor: barrier), Capture(captureRace, interceptor: barrier));
        Check("two-captures-one-winner", captures.Select(x => x.StatusCode).Order().SequenceEqual(new[] { 202, 409 }));
        var resumeBarrier = new ClaimBarrier();
        var resumes = await Task.WhenAll(Resume(captureRace, interceptor: resumeBarrier), Resume(captureRace, interceptor: resumeBarrier));
        Check("two-resumes-one-code-release", resumes.Select(x => x.StatusCode).Order().SequenceEqual(new[] { 200, 409 }) && resumes.Count(x => x.AuthorizationCode is not null) == 1);
        await using (var db = Open())
        {
            var attempt = await db.MeetingOAuthAttempts.SingleAsync(x => x.StateHash == Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(captureRace.State))));
            var callback = await db.MeetingOAuthCallbacks.SingleAsync(x => x.AttemptId == attempt.Id);
            Check("races-one-callback-four-audits", await db.AuditLogs.CountAsync(x => x.EntityId == attempt.Id || x.EntityId == callback.Id) == 4 && callback.Status == "Consumed" && attempt.Version == 2);
        }
        var captureFault = await Prepare(); var failed = false;
        try { await Capture(captureFault, interceptor: new OperationCommitFault()); } catch (InvalidOperationException ex) when (ex.Message == "Synthetic operation commit failure after SQL writes") { failed = true; }
        await using (var db = Open())
        {
            var attempt = await db.MeetingOAuthAttempts.SingleAsync(x => x.StateHash == Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(captureFault.State))));
            Check("capture-post-SQL-rollback", failed && attempt.Status == "Pending" && attempt.Version == 0 && !await db.MeetingOAuthCallbacks.AnyAsync(x => x.AttemptId == attempt.Id) && await db.AuditLogs.CountAsync(x => x.EntityId == attempt.Id) == 1);
        }
        Check("capture-after-proven-rollback", (await Capture(captureFault)).StatusCode == 202);
        failed = false;
        try { await Resume(captureFault, interceptor: new OperationCommitFault()); } catch (InvalidOperationException ex) when (ex.Message == "Synthetic operation commit failure after SQL writes") { failed = true; }
        await using (var db = Open())
        {
            var attempt = await db.MeetingOAuthAttempts.SingleAsync(x => x.StateHash == Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(captureFault.State))));
            var callback = await db.MeetingOAuthCallbacks.SingleAsync(x => x.AttemptId == attempt.Id);
            Check("resume-post-SQL-rollback", failed && attempt.Status == "CallbackCaptured" && attempt.Version == 1 && callback.Status == "Captured" && callback.Version == 0 && await db.AuditLogs.CountAsync(x => x.EntityId == callback.Id || x.EntityId == attempt.Id) == 2);
        }
        Check("resume-after-proven-rollback", (await Resume(captureFault)).StatusCode == 200);
        var restricted = await Prepare(); Check("restricted-capture", (await Capture(restricted)).StatusCode == 202);
        Check("other-actor-denied", (await Run(s => s.ResumeAsync(Actor(other), academy, "Zoom", restricted.State, restricted.Browser, registration))).StatusCode == 404);
        Check("foreign-academy-denied", (await Run(s => s.ResumeAsync(actor, foreign, "Zoom", restricted.State, restricted.Browser, registration))).StatusCode == 403);
        Check("wrong-browser-denied", (await Run(s => s.ResumeAsync(actor, academy, "Zoom", restricted.State, WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32)), registration))).StatusCode == 409);
        Check("changed-registration-denied", (await Run(s => s.ResumeAsync(actor, academy, "Zoom", restricted.State, restricted.Browser, new string('D', 64)))).StatusCode == 409);
        using (var scope = factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(); var user = (await users.FindByIdAsync(id.ToString()))!;
            if (!(await users.RemoveFromRoleAsync(user, "AcademyAdmin")).Succeeded) throw new InvalidOperationException("Resume revocation fixture failed");
        }
        Check("current-role-revoked-denied", (await Resume(restricted)).StatusCode == 403);
        await using (var db = Open()) Check("no-provider-or-notification-side-effects", !await db.Notifications.AnyAsync() && !await db.MeetingProviderBindings.AnyAsync(x => x.Status == "Connected"));
        MeetingOAuthCallback source;
        await using (var db = Open()) source = await db.MeetingOAuthCallbacks.AsNoTracking().SingleAsync(x => x.ActorUserId == id && x.Status == "Captured");
        async Task Constraint(string label, Action<MeetingOAuthCallback> mutate, int number)
        {
            await using var db = Open(); var row = new MeetingOAuthCallback { AcademyId = academy, Provider = source.Provider, ActorUserId = id, AttemptId = source.AttemptId, ProtectedEnvelope = "synthetic-constraint-only", ExpiresAtUtc = source.ExpiresAtUtc };
            mutate(row); db.Add(row); var rejected = false;
            try { await db.SaveChangesAsync(); } catch (DbUpdateException ex) when (ex.InnerException is SqlException sql && sql.Number == number) { rejected = true; }
            Check(label, rejected);
        }
        // Remove no existing evidence: use different primary IDs but the same
        // attempt. SQL may check the unique index before the FK; use separate
        // owned attempt rows for each FK probe below.
        async Task ForeignKey(string label, Action<MeetingOAuthCallback> mutate)
        {
            await using var db = Open(); var original = await db.MeetingOAuthAttempts.AsNoTracking().SingleAsync(x => x.Id == source.AttemptId);
            var probe = new MeetingOAuthAttempt { AcademyId = academy, ActorUserId = id, Provider = original.Provider, BindingId = original.BindingId, Generation = original.Generation,
                StateHash = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)), ProtectedEnvelope = "synthetic-constraint-only", ExpiresAtUtc = original.ExpiresAtUtc };
            db.Add(probe); await db.SaveChangesAsync(); var row = new MeetingOAuthCallback { AcademyId = academy, ActorUserId = id, Provider = probe.Provider, AttemptId = probe.Id, ProtectedEnvelope = "synthetic-constraint-only", ExpiresAtUtc = probe.ExpiresAtUtc };
            mutate(row); db.Add(row); var rejected = false;
            try { await db.SaveChangesAsync(); } catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 547 }) { rejected = true; }
            Check(label, rejected);
        }
        await ForeignKey("callback-cross-academy-FK", row => row.AcademyId = foreign);
        await ForeignKey("callback-cross-actor-FK", row => row.ActorUserId = other);
        await ForeignKey("callback-cross-provider-FK", row => row.Provider = "Microsoft365");
        await Constraint("callback-duplicate-attempt-refused", _ => { }, 2601);
        // Update the exact existing callback in a fresh context; a rejected SQL
        // statement cannot change its captured version or protected envelope.
        var statusRejected = false;
        await using (var db = Open())
        {
            var row = await db.MeetingOAuthCallbacks.SingleAsync(x => x.Id == source.Id); row.Status = "Consumed";
            try { await db.SaveChangesAsync(); } catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 547 }) { statusRejected = true; }
        }
        Check("callback-invalid-status-version-refused", statusRejected);
        if (checks.Count != 40) throw new InvalidOperationException("Resume SQL count mismatch " + checks.Count);
        var evidence = Path.Combine("QA", "EVIDENCE", "meeting-oauth-resume-sql-" + manifest.RunId.ToString("N")); Directory.CreateDirectory(evidence);
        var sources = new[] { "apps/api/Infrastructure/Meetings/MeetingOAuthCallbackStore.cs", "apps/api/Infrastructure/Meetings/MeetingOAuthAttemptStore.cs", "apps/api/Infrastructure/Meetings/MeetingOAuthBrowserBridge.cs", "apps/api/Domain/Entities/MeetingOAuthCallback.cs", "apps/api/Data/AcademyDeskDbContext.cs", "QA/tools/SqlHarness/MeetingOAuthResumeRegression.cs" }
            .ToDictionary(p => p, p => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(p))));
        await File.WriteAllTextAsync(Path.Combine(evidence, "result.json"), JsonSerializer.Serialize(new { checks, sources, scope = "Internal capture/resume with real owned SQL/Identity, not public OAuth routes/provider exchange/real-browser/device acceptance" }, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine("MEETING OAUTH RESUME PASS:40realSQL/Identity cases, no public OAuth or provider calls.");
    }
}
