using System.Net;
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
    private static async Task VerifyMeetingRefreshMigrationAsync(AcademyDeskDbContext db)
    {
        var migrations = db.Database.GetMigrations().ToArray(); var latest = migrations.Single(x => x.EndsWith("_MeetingCredentialRefresh", StringComparison.Ordinal));
        var previous = migrations[Array.IndexOf(migrations, latest) - 1]; var migrator = db.GetService<IMigrator>(); await migrator.MigrateAsync(latest);
        var binding = new MeetingProviderBinding { AcademyId = Guid.NewGuid(), Provider = "Zoom", Generation = 1, Version = 1 };
        var attempt = new MeetingOAuthAttempt { AcademyId = binding.AcademyId, BindingId = binding.Id, ActorUserId = Guid.NewGuid(), Provider = "Zoom", Generation = 1,
            StateHash = new string('D', 64), ProtectedEnvelope = "synthetic-migration-only", ExpiresAtUtc = DateTime.UtcNow.AddMinutes(5) };
        var exchange = new MeetingOAuthExchange { AcademyId = binding.AcademyId, ActorUserId = attempt.ActorUserId, AttemptId = attempt.Id, BindingId = binding.Id,
            Provider = "Zoom", Generation = 1, BindingVersion = 1, Fence = Guid.NewGuid(), RegistrationFingerprint = new string('E', 64), DeadlineUtc = DateTime.UtcNow.AddSeconds(30),
            Status = "RefreshStarted", Version = 2, ProtectedEnvelope = "synthetic-migration-only" };
        db.AddRange(binding, attempt, exchange); await db.SaveChangesAsync(); var blocked = false;
        try { await migrator.MigrateAsync(previous); } catch (SqlException ex) when (ex.Number == 51000) { blocked = true; }
        if (!blocked || !await db.MeetingOAuthExchanges.AnyAsync(x => x.Id == exchange.Id && x.Version == 2)) throw new InvalidOperationException("Refresh downgrade lost evidence");
        exchange.Status = "Started"; exchange.Version = 0;
        MeetingAccountReview Review(int version) => new() { AcademyId = binding.AcademyId, ActorUserId = attempt.ActorUserId, ExchangeId = exchange.Id, BindingId = binding.Id, Provider = binding.Provider,
            Generation = 1, BindingVersion = version, RegistrationFingerprint = exchange.RegistrationFingerprint, ReviewHash = new string('F', 64), ProtectedEnvelope = "synthetic-migration-only", ExpiresAtUtc = DateTime.UtcNow.AddMinutes(5) };
        var reviews = new[] { Review(1), Review(2) }; db.AddRange(reviews); await db.SaveChangesAsync(); blocked = false;
        try { await migrator.MigrateAsync(previous); } catch (SqlException ex) when (ex.Number == 51000) { blocked = true; }
        if (!blocked || await db.MeetingAccountReviews.CountAsync(x => x.ExchangeId == exchange.Id) != 2) throw new InvalidOperationException("Refresh downgrade lost review history");
        db.RemoveRange(reviews); await db.SaveChangesAsync(); db.Remove(exchange); await db.SaveChangesAsync(); db.Remove(attempt); await db.SaveChangesAsync(); db.Remove(binding); await db.SaveChangesAsync(); // Exact owned probes only.
        await migrator.MigrateAsync(previous); await migrator.MigrateAsync(latest);
        Console.WriteLine("MEETING REFRESH MIGRATION PASS: upgrade, populated-down refusal, exact probe removal and empty down/up.");
    }
    private sealed class RefreshTransport(Func<HttpRequestMessage, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        public int Calls;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token) { Calls++; return send(request); }
    }
    private static async Task VerifyMeetingRefreshesAsync(QaApiFactory factory, QaRunManifest manifest)
    {
        Guid academy, foreign; string connection;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); connection = db.Database.GetConnectionString()!;
            academy = await db.Academies.Where(x => x.Name == "Synthetic Academy A").Select(x => x.Id).SingleAsync(); foreign = await db.Academies.Where(x => x.Name == "Synthetic Academy B").Select(x => x.Id).SingleAsync();
        }
        var target = new SqlConnectionStringBuilder(connection);
        if (target.DataSource != manifest.SqlServer || target.InitialCatalog != manifest.Database || target.UserID != manifest.RuntimeLogin) throw new InvalidOperationException("Refresh requires exact owned target");
        var actorId = await CreateAccessActorAsync(factory, "qa-refresh@example.invalid", "AcademyAdmin", academy);
        var actor = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, actorId.ToString()) }, "synthetic-refresh"));
        var clock = new OperationClock(); var keys = factory.Services.GetRequiredService<IDataProtectionProvider>();
        const string oldRefresh = "synthetic-refresh-old", newRefresh = "synthetic-refresh-next", newAccess = "synthetic-refresh-access-next";
        AcademyDeskDbContext Open(IInterceptor? interceptor = null) { var options = new DbContextOptionsBuilder<AcademyDeskDbContext>().UseSqlServer(connection); if (interceptor is not null) options.AddInterceptors(interceptor); return new(options.Options); }
        async Task<T> Run<T>(Func<MeetingOAuthAttemptStore, Task<T>> action, IInterceptor? interceptor = null)
        {
            using var scope = factory.Services.CreateScope(); await using var db = Open(interceptor); var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            return await action(new(db, users, new(db, users, clock), keys, clock));
        }
        MeetingOAuthRegistration Registration(string provider = "GoogleWorkspace") => MeetingOAuthRegistrationRegistry.Create("https://api.academy.invalid", "https://app.academy.invalid",
            [new(provider, 1, "synthetic-client", "https://api.academy.invalid" + MeetingOAuthRegistrationRegistry.CallbackPath(provider), "https://app.academy.invalid/communication-settings",
                provider == "GoogleWorkspace" ? "google-calendar-owned-web/1" : provider == "Microsoft365" ? "microsoft-online-meetings-work/1" : "zoom-public-pkce/1",
                provider == "Microsoft365" ? "019acc9a-6200-7000-8000-000000000001" : null)]).Resolve(provider)!;
        async Task<MeetingOAuthExchangePermit> Quarantine(MeetingOAuthRegistration reg)
        {
            int version; await using (var db = Open()) version = await db.MeetingProviderBindings.Where(x => x.AcademyId == academy && x.Provider == reg.Provider).Select(x => x.Version).SingleAsync();
            return await Run(async store =>
            {
                var browser = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32)); var prepared = await store.PrepareAsync(actor, academy, reg.Provider, version, browser, reg.Fingerprint);
                if (prepared.StatusCode != 201 || (await store.CaptureAsync(academy, reg.Provider, prepared.Material!.State, browser, reg.Fingerprint, "synthetic-refresh-code", false)).StatusCode != 202) throw new InvalidOperationException("Refresh capture fixture failed");
                var claim = await store.ResumeAsync(actor, academy, reg.Provider, prepared.Material.State, browser, reg.Fingerprint); var start = await store.BeginExchangeAsync(actor, academy, reg, claim);
                if (start.StatusCode != 201 || (await store.FinishExchangeAsync(actor, academy, reg, start.Permit!, new(200, "TokensReceivedAccountUnverified", Tokens: new("synthetic-refresh-access-old", oldRefresh, clock.Now.AddHours(1), reg.Flow!.RequiredTokenScopes)))).StatusCode != 200) throw new InvalidOperationException("Refresh quarantine fixture failed");
                return start.Permit!;
            });
        }
        MeetingOAuthExchangeOutcome Tokens(MeetingOAuthRegistration reg, string refresh = newRefresh) => new(200, "TokensReceivedAccountUnverified", Tokens: new(newAccess, refresh, clock.Now.AddHours(1), reg.Flow!.RequiredTokenScopes));
        HttpResponseMessage Response(string json) => new(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
        var checks = new List<string>(); void Check(string label, bool passed) { if (!passed) throw new InvalidOperationException("MEETING REFRESH " + label); checks.Add(label); Console.WriteLine("MEETING REFRESH CASE " + label + " PASS"); }
        foreach (var provider in new[] { "GoogleWorkspace", "Microsoft365" })
        {
            var reg = Registration(provider); var permit = await Quarantine(reg); var observation = new MeetingOAuthAccountObservation(provider == "Microsoft365" ? "019acc9a-6200-7000-8000-000000000002" : "synthetic-subject", null, clock.Now);
            var review = (await Run(s => s.PrepareAccountReviewAsync(actor, academy, reg, permit, observation))).Review!;
            Check(provider + "-source-review-confirmed", (await Run(s => s.ConfirmAccountReviewAsync(actor, academy, reg, review.Id, 0, review.ReviewHash, true))).StatusCode == 200);
            var handler = new RefreshTransport(async request =>
            {
                var form = await request.Content!.ReadAsStringAsync(); await using var db = Open();
                if (request.Method != HttpMethod.Post || request.RequestUri != reg.Flow!.TokenEndpoint || request.RequestUri!.Query != "" || request.Headers.Authorization is not null || !form.Contains("grant_type=refresh_token") || !form.Contains("client_secret=synthetic-secret") ||
                    !await db.MeetingOAuthExchanges.AnyAsync(x => x.Id == permit.Id && x.Status == "RefreshStarted" && x.Version == 2) || !await db.AuditLogs.AnyAsync(x => x.EntityId == permit.Id && x.Action == "MeetingRefreshStarted")) throw new InvalidOperationException("Refresh sent before durable admission or unsafe request");
                Check(provider + "-fixed-fake-request-after-durable-start", true);
                return Response(provider == "GoogleWorkspace" ? JsonSerializer.Serialize(new { access_token = newAccess, token_type = "Bearer", expires_in = 3600 })
                    : JsonSerializer.Serialize(new { access_token = newAccess, refresh_token = newRefresh, token_type = "Bearer", expires_in = 3600, scope = string.Join(' ', reg.Flow.RequiredTokenScopes) }));
            });
            using var transport = new HttpMessageInvoker(handler); var result = await Run(s => new MeetingProviderRefresh(s, transport, clock).ExecuteAsync(actor, academy, reg, permit, 1, "synthetic-secret"));
            Check(provider + "-new-fence-no-connected-no-serialized-secrets", result.StatusCode == 200 && result.Permit is not null && result.Permit.Fence != permit.Fence && !result.Connected && !result.CanRetry && handler.Calls == 1 && !JsonSerializer.Serialize(result).Contains(newAccess));
            await using (var db = Open()) Check(provider + "-encrypted-v3-two-refresh-audits", await db.MeetingOAuthExchanges.AnyAsync(x => x.Id == permit.Id && x.Status == "TokensQuarantined" && x.Version == 3 && !x.ProtectedEnvelope.Contains(newAccess) && !x.ProtectedEnvelope.Contains(newRefresh)) && await db.AuditLogs.CountAsync(x => x.EntityId == permit.Id && x.Action.StartsWith("MeetingRefresh")) == 2);
            var read = await Run(s => s.ReadQuarantinedTokensAsync(actor, academy, reg, result.Permit!));
            Check(provider + "-current-private-read-inherits-or-rotates-refresh", read.StatusCode == 200 && read.Tokens!.RefreshToken == (provider == "GoogleWorkspace" ? oldRefresh : newRefresh));
            Check(provider + "-old-permit-and-confirmation-fenced", (await Run(s => s.ReadQuarantinedTokensAsync(actor, academy, reg, permit))).StatusCode == 409 && (await Run(s => s.ReadAccountReviewAsync(actor, academy, reg, review.Id))).StatusCode == 409);
            Check(provider + "-new-review-required-and-history-retained", (await Run(s => s.PrepareAccountReviewAsync(actor, academy, reg, result.Permit!, observation))).StatusCode == 201);
            await using (var db = Open()) Check(provider + "-two-review-versions-no-connected-binding", await db.MeetingAccountReviews.CountAsync(x => x.ExchangeId == permit.Id) == 2 && await db.MeetingProviderBindings.AnyAsync(x => x.AcademyId == academy && x.Provider == provider && x.Status == "VerificationRequired"));
            var second = await Run(s => s.BeginRefreshAsync(actor, academy, reg, result.Permit!, 3));
            Check(provider + "-second-cycle-v4-v5", second.StatusCode == 201 && (await Run(s => s.FinishRefreshAsync(actor, academy, reg, second.Material!, Tokens(reg, "synthetic-third-refresh")))).StatusCode == 200);
        }
        var registration = Registration(); var race = await Quarantine(registration); var barrier = new ClaimBarrier();
        var starts = await Task.WhenAll(Run(s => s.BeginRefreshAsync(actor, academy, registration, race, 1), barrier), Run(s => s.BeginRefreshAsync(actor, academy, registration, race, 1), barrier));
        Check("two-starts-exactly-201-409", starts.Select(x => x.StatusCode).Order().SequenceEqual(new[] { 201, 409 }));
        var material = starts.Single(x => x.Material is not null).Material!;
        await using (var db = Open()) Check("race-one-start-one-v2", await db.MeetingOAuthExchanges.AnyAsync(x => x.Id == race.Id && x.Version == 2) && await db.AuditLogs.CountAsync(x => x.EntityId == race.Id && x.Action == "MeetingRefreshStarted") == 1);
        var finishBarrier = new ClaimBarrier(); var finishes = await Task.WhenAll(Run(s => s.FinishRefreshAsync(actor, academy, registration, material, Tokens(registration)), finishBarrier), Run(s => s.FinishRefreshAsync(actor, academy, registration, material, Tokens(registration)), finishBarrier));
        Check("two-completions-exactly-200-409", finishes.Select(x => x.StatusCode).Order().SequenceEqual(new[] { 200, 409 }));
        await using (var db = Open()) Check("race-one-v3-one-completion-audit", await db.MeetingOAuthExchanges.AnyAsync(x => x.Id == race.Id && x.Version == 3) && await db.AuditLogs.CountAsync(x => x.EntityId == race.Id && x.Action == "MeetingRefreshTokensQuarantined") == 1);
        var fault = await Quarantine(registration); var failed = false; var calls = 0; int bindingVersion;
        await using (var db = Open()) bindingVersion = await db.MeetingProviderBindings.Where(x => x.AcademyId == academy && x.Provider == registration.Provider).Select(x => x.Version).SingleAsync();
        using (var transport = new HttpMessageInvoker(new RefreshTransport(_ => { calls++; return Task.FromResult(Response("{}")); })))
        {
            try { await Run(s => new MeetingProviderRefresh(s, transport, clock).ExecuteAsync(actor, academy, registration, fault, 1, "synthetic-secret"), new OperationCommitFault()); } catch (InvalidOperationException ex) when (ex.Message == "Synthetic operation commit failure after SQL writes") { failed = true; }
        }
        await using (var db = Open()) Check("start-audit-fault-no-send-no-state-change", failed && calls == 0 && await db.MeetingOAuthExchanges.AnyAsync(x => x.Id == fault.Id && x.Status == "TokensQuarantined" && x.Version == 1) && !await db.AuditLogs.AnyAsync(x => x.EntityId == fault.Id && x.Action == "MeetingRefreshStarted") && await db.MeetingProviderBindings.AnyAsync(x => x.AcademyId == academy && x.Provider == registration.Provider && x.Version == bindingVersion));
        var admitted = await Run(s => s.BeginRefreshAsync(actor, academy, registration, fault, 1)); Check("start-after-proved-rollback", admitted.StatusCode == 201);
        failed = false;
        try { await Run(s => s.FinishRefreshAsync(actor, academy, registration, admitted.Material!, Tokens(registration)), new OperationCommitFault()); } catch (InvalidOperationException ex) when (ex.Message == "Synthetic operation commit failure after SQL writes") { failed = true; }
        await using (var db = Open()) Check("completion-audit-fault-preserves-start", failed && await db.MeetingOAuthExchanges.AnyAsync(x => x.Id == fault.Id && x.Status == "RefreshStarted" && x.Version == 2) && await db.AuditLogs.CountAsync(x => x.EntityId == fault.Id && x.Action.StartsWith("MeetingRefresh")) == 1);
        Check("wrong-fence-cannot-complete", (await Run(s => s.FinishRefreshAsync(actor, academy, registration, new(fault.Id, Guid.NewGuid(), 2, oldRefresh, registration.Flow!.RequiredTokenScopes), Tokens(registration)))).StatusCode == 409);
        clock.Now = clock.Now.AddSeconds(30);
        Check("restart-expired-refresh-unknown-idempotent", await Run(s => s.RecoverRefreshAsync(academy, fault.Id)) && !await Run(s => s.RecoverRefreshAsync(academy, fault.Id)));
        Check("unknown-never-restarts-or-releases-old-tokens", (await Run(s => s.BeginRefreshAsync(actor, academy, registration, fault, 1))).StatusCode == 409 && (await Run(s => s.ReadQuarantinedTokensAsync(actor, academy, registration, fault))).StatusCode == 409);
        Check("late-completion-cannot-resurrect-unknown", (await Run(s => s.FinishRefreshAsync(actor, academy, registration, admitted.Material!, Tokens(registration)))).StatusCode == 409);
        var denied = await Quarantine(registration);
        Check("foreign-academy-denied", (await Run(s => s.BeginRefreshAsync(actor, foreign, registration, denied, 1))).StatusCode == 403);
        Check("wrong-fence-denied", (await Run(s => s.BeginRefreshAsync(actor, academy, registration, denied with { Fence = Guid.NewGuid() }, 1))).StatusCode == 409);
        Check("invalid-version-denied", (await Run(s => s.BeginRefreshAsync(actor, academy, registration, denied, 2))).StatusCode == 400);
        var zoom = await Quarantine(Registration("Zoom"));
        Check("zoom-public-refresh-unqualified", (await Run(s => s.BeginRefreshAsync(actor, academy, Registration("Zoom"), zoom, 1))).StatusCode == 503);
        var changed = await Quarantine(registration);
        var handlerChanged = new RefreshTransport(async _ =>
        {
            await using var db = Open(); var b = await db.MeetingProviderBindings.SingleAsync(x => x.AcademyId == academy && x.Provider == registration.Provider); b.Version++; b.Generation++; await db.SaveChangesAsync();
            return Response(JsonSerializer.Serialize(new { access_token = newAccess, token_type = "Bearer", expires_in = 3600 }));
        });
        using (var transport = new HttpMessageInvoker(handlerChanged)) Check("post-response-generation-change-no-new-credentials", (await Run(s => new MeetingProviderRefresh(s, transport, clock).ExecuteAsync(actor, academy, registration, changed, 1, "synthetic-secret"))).StatusCode == 409);
        var roleSource = await Quarantine(registration);
        using (var scope = factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(); var user = (await users.FindByIdAsync(actorId.ToString()))!;
            if (!(await users.RemoveFromRoleAsync(user, "AcademyAdmin")).Succeeded) throw new InvalidOperationException("Refresh role fixture failed");
        }
        Check("revoked-role-refresh-denied", (await Run(s => s.BeginRefreshAsync(actor, academy, registration, roleSource, 1))).StatusCode == 403);
        foreach (var pair in new[] { ("RefreshStarted", 1), ("RefreshStarted", 3), ("RefreshUnknown", 2), ("TokensQuarantined", 2), ("Connected", 3), ("TokensQuarantined", -1) })
        {
            await using var db = Open(); var row = await db.MeetingOAuthExchanges.SingleAsync(x => x.Id == roleSource.Id); row.Status = pair.Item1; row.Version = pair.Item2; var rejected = false;
            try { await db.SaveChangesAsync(); } catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 547 }) { rejected = true; }
            Check("SQL-state-" + pair.Item1 + "-version-" + pair.Item2, rejected);
        }
        await using (var db = Open())
        {
            var source = await db.MeetingAccountReviews.AsNoTracking().FirstAsync(x => x.ActorUserId == actorId);
            db.Add(new MeetingAccountReview { AcademyId = source.AcademyId, ActorUserId = source.ActorUserId, ExchangeId = source.ExchangeId, BindingId = source.BindingId, Provider = source.Provider,
                Generation = source.Generation, BindingVersion = source.BindingVersion, RegistrationFingerprint = source.RegistrationFingerprint, ReviewHash = source.ReviewHash, ProtectedEnvelope = "synthetic-duplicate-only", ExpiresAtUtc = source.ExpiresAtUtc });
            var rejected = false; try { await db.SaveChangesAsync(); } catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 }) { rejected = true; }
            Check("SQL-duplicate-review-same-exchange-version-refused", rejected);
        }
        if (checks.Count != 42) throw new InvalidOperationException("Refresh case count mismatch " + checks.Count);
        var sources = new[] { "apps/api/Infrastructure/Meetings/MeetingRefreshStore.cs", "apps/api/Infrastructure/Meetings/MeetingProviderRefresh.cs", "apps/api/Infrastructure/Meetings/MeetingOAuthExchangeStore.cs", "apps/api/Infrastructure/Meetings/MeetingAccountReviewStore.cs", "apps/api/Data/AcademyDeskDbContext.cs", "QA/tools/SqlHarness/MeetingRefreshRegression.cs", "apps/api/Migrations/AcademyDeskDb/20261016044240_MeetingCredentialRefresh.cs" }.ToDictionary(p => p, p => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(p))));
        var evidence = Path.Combine("QA", "EVIDENCE", "meeting-refresh-sql-" + manifest.RunId.ToString("N")); Directory.CreateDirectory(evidence);
        await File.WriteAllTextAsync(Path.Combine(evidence, "result.json"), JsonSerializer.Serialize(new { checks, sources, scope = "Real owned SQL/Identity with fake refresh HTTP only; no live providers, Zoom refresh, Connected, public route or UI enablement" }, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine("MEETING REFRESH PASS:42 SQL/Identity cases; fake Google/Microsoft only, Zoom refresh unqualified.");
    }
}
