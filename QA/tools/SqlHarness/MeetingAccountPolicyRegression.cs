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
using Microsoft.Extensions.DependencyInjection;

internal static partial class SqlHarnessEntryPoint
{
    private static async Task VerifyMeetingAccountPoliciesAsync(QaApiFactory factory, QaRunManifest manifest)
    {
        Guid foreign; string connection;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); connection = db.Database.GetConnectionString()!;
            foreign = await db.Academies.Where(x => x.Name == "Synthetic Academy A").Select(x => x.Id).SingleAsync();
        }
        var target = new SqlConnectionStringBuilder(connection);
        if (target.DataSource != manifest.SqlServer || target.InitialCatalog != manifest.Database || target.UserID != manifest.RuntimeLogin) throw new InvalidOperationException("Account policy requires exact owned target");
        AcademyDeskDbContext Open(IInterceptor? interceptor = null) { var options = new DbContextOptionsBuilder<AcademyDeskDbContext>().UseSqlServer(connection); if (interceptor is not null) options.AddInterceptors(interceptor); return new(options.Options); }
        var tenant = new Academy { Name = "Synthetic Account Policy Academy", EnabledModulesJson = "[\"Core\",\"Engagement\"]" };
        await using (var db = Open()) { db.Add(tenant); await db.SaveChangesAsync(); }
        var academy = tenant.Id; var actorId = await CreateAccessActorAsync(factory, "qa-account-policy@example.invalid", "AcademyAdmin", academy);
        var otherId = await CreateAccessActorAsync(factory, "qa-account-policy-other@example.invalid", "AcademyAdmin", academy);
        ClaimsPrincipal Actor(Guid id) => new(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, id.ToString()) }, "synthetic-policy"));
        var actor = Actor(actorId); var clock = new OperationClock(); var keys = factory.Services.GetRequiredService<IDataProtectionProvider>();
        async Task<T> Run<T>(Func<MeetingOAuthAttemptStore, Task<T>> action, IInterceptor? interceptor = null)
        {
            using var scope = factory.Services.CreateScope(); await using var db = Open(interceptor); var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            return await action(new(db, users, new(db, users, clock), keys, clock));
        }
        MeetingOAuthRegistration Registration(string provider = "GoogleWorkspace", int revision = 1) => MeetingOAuthRegistrationRegistry.Create("https://api.academy.invalid", "https://app.academy.invalid",
            [new(provider, revision, "synthetic-client", "https://api.academy.invalid" + MeetingOAuthRegistrationRegistry.CallbackPath(provider), "https://app.academy.invalid/communication-settings",
                provider == "GoogleWorkspace" ? "google-calendar-owned-web/1" : provider == "Microsoft365" ? "microsoft-online-meetings-work/1" : "zoom-public-pkce/1", provider == "Microsoft365" ? "019acc9a-6200-7000-8000-000000000001" : null)]).Resolve(provider)!;
        MeetingAccountPolicyInput Policy(MeetingOAuthRegistration reg) => new(academy, reg.Provider, 1, reg.Fingerprint, reg.Provider == "Microsoft365" ? "019acc9a-6200-7000-8000-000000000002" : "synthetic-policy-subject",
            reg.Provider == "Zoom" ? "synthetic-policy-account" : null, reg.Flow!.DirectoryTenant, clock.Now.AddMinutes(-1), clock.Now.AddHours(1));
        async Task<(MeetingAccountReviewState Review, int BindingVersion)> Source(MeetingOAuthRegistration reg)
        {
            int version; await using (var db = Open()) version = await db.MeetingProviderBindings.Where(x => x.AcademyId == academy && x.Provider == reg.Provider).Select(x => (int?)x.Version).SingleOrDefaultAsync() ?? 0;
            return await Run(async s =>
            {
                var browser = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32)); var intent = await s.PrepareAsync(actor, academy, reg.Provider, version, browser, reg.Fingerprint);
                if (intent.StatusCode != 201 || (await s.CaptureAsync(academy, reg.Provider, intent.Material!.State, browser, reg.Fingerprint, "synthetic-policy-code", false)).StatusCode != 202) throw new InvalidOperationException("Policy source capture failed");
                var claim = await s.ResumeAsync(actor, academy, reg.Provider, intent.Material.State, browser, reg.Fingerprint); var exchange = await s.BeginExchangeAsync(actor, academy, reg, claim);
                if ((await s.FinishExchangeAsync(actor, academy, reg, exchange.Permit!, new(200, "TokensReceivedAccountUnverified", Tokens: new("synthetic-policy-access", "synthetic-policy-refresh", clock.Now.AddHours(1), reg.Flow!.RequiredTokenScopes)))).StatusCode != 200) throw new InvalidOperationException("Policy source quarantine failed");
                var input = Policy(reg); var result = await s.PrepareAccountReviewAsync(actor, academy, reg, exchange.Permit!, new(input.Subject, input.Account, clock.Now));
                if (result.StatusCode != 201) throw new InvalidOperationException("Policy source review failed"); return (result.Review!, version + 2);
            });
        }
        async Task Confirm(MeetingOAuthRegistration reg, MeetingAccountReviewState review)
        {
            if ((await Run(s => s.ConfirmAccountReviewAsync(actor, academy, reg, review.Id, 0, review.ReviewHash, true))).StatusCode != 200) throw new InvalidOperationException("Policy source confirmation failed");
        }
        Task<MeetingAccountPolicyOutcome> Evaluate(MeetingOAuthRegistration reg, Guid id, MeetingAccountPolicyRegistry registry, IInterceptor? interceptor = null) => Run(s => s.CheckAccountPolicyAsync(actor, academy, reg, id, registry), interceptor);
        var checks = new List<string>(); void Check(string label, bool passed) { if (!passed) throw new InvalidOperationException("MEETING ACCOUNT POLICY " + label); checks.Add(label); Console.WriteLine("MEETING ACCOUNT POLICY CASE " + label + " PASS"); }
        async Task Denial(string label, Func<Task<MeetingAccountPolicyOutcome>> request, int status)
        {
            int before; await using (var db = Open()) before = await db.AuditLogs.CountAsync(); var result = await request();
            await using (var db = Open()) Check(label, result.StatusCode == status && !result.AccountPolicyMatched && result.PolicyFingerprint is null && !result.Connected && !result.CanExecute && await db.AuditLogs.CountAsync() == before);
        }
        foreach (var provider in new[] { "GoogleWorkspace", "Microsoft365", "Zoom" })
        {
            var reg = Registration(provider); var source = await Source(reg); var input = Policy(reg); var registry = MeetingAccountPolicyRegistry.Create([input]);
            await Denial(provider + "-unconfirmed-human-review-denied", () => Evaluate(reg, source.Review.Id, registry), 409);
            await Confirm(reg, source.Review); var result = await Evaluate(reg, source.Review.Id, registry);
            Check(provider + "-matching-policy-still-unavailable-for-execution", result.StatusCode == 503 && result.Code == "IndependentProviderEvidenceRequired" && result.AccountPolicyMatched && !result.ProviderVerified && !result.Connected && !result.CanExecute && !result.CanRetry);
            await using (var db = Open())
            {
                var audit = await db.AuditLogs.SingleAsync(x => x.EntityId == source.Review.Id && x.Action == "MeetingAccountPolicyMatchedEvidenceRequired");
                Check(provider + "-audit-provenance-no-identity-or-token-no-connected-state", audit.MetadataJson!.Contains(result.PolicyFingerprint!) && !audit.MetadataJson.Contains(input.Subject) && !audit.MetadataJson.Contains("synthetic-policy-access") && await db.MeetingProviderBindings.AnyAsync(x => x.AcademyId == academy && x.Provider == provider && x.Status == "VerificationRequired" && x.Version == source.BindingVersion));
            }
            var mismatched = input with { Subject = provider == "Microsoft365" ? "019acc9a-6200-7000-8000-000000000003" : "synthetic-wrong-subject" };
            await Denial(provider + "-subject-policy-mismatch-no-audit", () => Evaluate(reg, source.Review.Id, MeetingAccountPolicyRegistry.Create([mismatched])), 409);
            await Denial(provider + "-expired-policy-no-match", () => Evaluate(reg, source.Review.Id, MeetingAccountPolicyRegistry.Create([input with { ValidFrom = clock.Now.AddHours(-1), ValidUntil = clock.Now }])), 503);
            await Denial(provider + "-another-academy-policy-no-fallback", () => Evaluate(reg, source.Review.Id, MeetingAccountPolicyRegistry.Create([input with { Academy = foreign }])), 503);
            await Denial(provider + "-changed-registration-invalidates-policy", () => Evaluate(Registration(provider, 2), source.Review.Id, registry), 503);
        }
        var registration = Registration(); var fault = await Source(registration); await Confirm(registration, fault.Review); var faultInput = Policy(registration); var faultPolicy = MeetingAccountPolicyRegistry.Create([faultInput]);
        var failed = false; int faultBefore; await using (var db = Open()) faultBefore = await db.AuditLogs.CountAsync();
        try { await Evaluate(registration, fault.Review.Id, faultPolicy, new OperationCommitFault()); } catch (InvalidOperationException ex) when (ex.Message == "Synthetic operation commit failure after SQL writes") { failed = true; }
        await using (var db = Open()) Check("required-audit-fault-no-policy-match-result-no-audit", failed && await db.AuditLogs.CountAsync() == faultBefore && await db.MeetingProviderBindings.AnyAsync(x => x.AcademyId == academy && x.Provider == registration.Provider && x.Status == "VerificationRequired" && x.Version == fault.BindingVersion));
        Check("check-after-proved-audit-rollback-still-unavailable", (await Evaluate(registration, fault.Review.Id, faultPolicy)).Code == "IndependentProviderEvidenceRequired");
        Check("shared-subject-across-academies-disables-entire-snapshot", !MeetingAccountPolicyRegistry.Create([faultInput, faultInput with { Academy = foreign }]).IsValid);
        var zoomPolicy = Policy(Registration("Zoom"));
        Check("shared-zoom-account-with-different-users-disabled", !MeetingAccountPolicyRegistry.Create([zoomPolicy, zoomPolicy with { Academy = foreign, Subject = "another-zoom-user" }]).IsValid);
        var race = await Source(registration); await Confirm(registration, race.Review); var racePolicy = MeetingAccountPolicyRegistry.Create([Policy(registration)]); var barrier = new ClaimBarrier();
        var checking = Evaluate(registration, race.Review.Id, racePolicy, barrier);
        var disconnecting = Run(s => s.DisconnectLocallyAsync(actor, academy, registration.Provider, race.BindingVersion, true), barrier);
        await Task.WhenAll(checking, disconnecting); var checkedResult = await checking; var disconnected = await disconnecting;
        await using (var db = Open()) Check("concurrent-disconnect-no-executable-result-and-disconnected-binding", disconnected.StatusCode == 200 && (checkedResult.StatusCode == 409 || checkedResult.StatusCode == 503 && checkedResult.AccountPolicyMatched) && !checkedResult.CanExecute && !checkedResult.Connected && await db.MeetingProviderBindings.AnyAsync(x => x.AcademyId == academy && x.Provider == registration.Provider && x.Status == "Disconnected" && x.Version == race.BindingVersion + 1));
        var authoritySource = await Source(registration); await Confirm(registration, authoritySource.Review); var authorityPolicy = MeetingAccountPolicyRegistry.Create([Policy(registration)]);
        await Denial("foreign-academy-cannot-probe-policy-or-review", () => Run(s => s.CheckAccountPolicyAsync(actor, foreign, registration, authoritySource.Review.Id, authorityPolicy)), 403);
        await Denial("other-current-admin-cannot-inherit-review", () => Run(s => s.CheckAccountPolicyAsync(Actor(otherId), academy, registration, authoritySource.Review.Id, authorityPolicy)), 404);
        clock.Now = clock.Now.AddMinutes(5);
        await Denial("exact-review-expiry-cannot-match-policy", () => Evaluate(registration, authoritySource.Review.Id, authorityPolicy), 410);
        using (var scope = factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(); var user = (await users.FindByIdAsync(actorId.ToString()))!;
            if (!(await users.RemoveFromRoleAsync(user, "AcademyAdmin")).Succeeded) throw new InvalidOperationException("Policy role fixture failed");
        }
        await Denial("revoked-role-cannot-use-matching-policy", () => Evaluate(registration, authoritySource.Review.Id, authorityPolicy), 403);
        if (checks.Count != 30) throw new InvalidOperationException("Account policy case count mismatch " + checks.Count);
        var sources = new[] { "apps/api/Infrastructure/Meetings/MeetingAccountPolicy.cs", "apps/api/Infrastructure/Meetings/MeetingAccountPolicyStore.cs", "apps/api/Infrastructure/Meetings/MeetingAccountReviewStore.cs", "tests/AcademyDesk.Api.Tests/MeetingAccountPolicyTests.cs", "QA/tools/SqlHarness/MeetingAccountPolicyRegression.cs" }.ToDictionary(p => p, p => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(p))));
        var evidence = Path.Combine("QA", "EVIDENCE", "meeting-account-policy-sql-" + manifest.RunId.ToString("N")); Directory.CreateDirectory(evidence);
        await File.WriteAllTextAsync(Path.Combine(evidence, "result.json"), JsonSerializer.Serialize(new { checks, sources, scope = "Real owned SQL/Identity; private configured-account policy check only, not independently verified ownership/directory/consent/license/grant impact, live provider, Connected or UI activation" }, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine("MEETING ACCOUNT POLICY PASS:30 SQL/Identity cases; matching configuration never enables execution.");
    }
}
