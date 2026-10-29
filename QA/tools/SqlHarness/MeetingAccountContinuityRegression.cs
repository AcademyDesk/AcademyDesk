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
    private static async Task VerifyMeetingAccountContinuityAsync(QaApiFactory factory, QaRunManifest manifest)
    {
        Guid foreign; string connection;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); connection = db.Database.GetConnectionString()!;
            foreign = await db.Academies.Where(x => x.Name == "Synthetic Academy A").Select(x => x.Id).SingleAsync();
        }
        var target = new SqlConnectionStringBuilder(connection);
        if (target.DataSource != manifest.SqlServer || target.InitialCatalog != manifest.Database || target.UserID != manifest.RuntimeLogin) throw new InvalidOperationException("Account continuity requires exact owned target");
        AcademyDeskDbContext Open(IInterceptor? interceptor = null) { var options = new DbContextOptionsBuilder<AcademyDeskDbContext>().UseSqlServer(connection); if (interceptor is not null) options.AddInterceptors(interceptor); return new(options.Options); }
        var tenant = new Academy { Name = "Synthetic Account Continuity Academy", EnabledModulesJson = "[\"Core\",\"Engagement\"]" };
        await using (var db = Open()) { db.Add(tenant); await db.SaveChangesAsync(); }
        var academy = tenant.Id; var actorId = await CreateAccessActorAsync(factory, "qa-continuity@example.invalid", "AcademyAdmin", academy);
        var otherId = await CreateAccessActorAsync(factory, "qa-continuity-other@example.invalid", "AcademyAdmin", academy);
        ClaimsPrincipal Actor(Guid id) => new(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, id.ToString()) }, "synthetic-continuity"));
        var actor = Actor(actorId); var clock = new OperationClock(); var keys = factory.Services.GetRequiredService<IDataProtectionProvider>();
        async Task<T> Run<T>(Func<MeetingOAuthAttemptStore, Task<T>> action, IInterceptor? interceptor = null)
        {
            using var scope = factory.Services.CreateScope(); await using var db = Open(interceptor); var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            return await action(new(db, users, new(db, users, clock), keys, clock));
        }
        MeetingOAuthRegistration Registration(string provider = "GoogleWorkspace") => MeetingOAuthRegistrationRegistry.Create("https://api.academy.invalid", "https://app.academy.invalid",
            [new(provider, 1, "synthetic-client", "https://api.academy.invalid" + MeetingOAuthRegistrationRegistry.CallbackPath(provider), "https://app.academy.invalid/communication-settings",
                provider == "GoogleWorkspace" ? "google-calendar-owned-web/1" : "microsoft-online-meetings-work/1", provider == "Microsoft365" ? "019acc9a-6200-7000-8000-000000000001" : null)]).Resolve(provider)!;
        MeetingOAuthExchangeOutcome Tokens(MeetingOAuthRegistration reg, string suffix) => new(200, "TokensReceivedAccountUnverified", Tokens: new("synthetic-continuity-access-" + suffix, "synthetic-continuity-refresh-" + suffix, clock.Now.AddHours(1), reg.Flow!.RequiredTokenScopes));
        async Task<MeetingOAuthExchangePermit> Source(MeetingOAuthRegistration reg)
        {
            int version; await using (var db = Open()) version = await db.MeetingProviderBindings.Where(x => x.AcademyId == academy && x.Provider == reg.Provider).Select(x => (int?)x.Version).SingleOrDefaultAsync() ?? 0;
            return await Run(async s =>
            {
                var browser = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32)); var intent = await s.PrepareAsync(actor, academy, reg.Provider, version, browser, reg.Fingerprint);
                if (intent.StatusCode != 201 || (await s.CaptureAsync(academy, reg.Provider, intent.Material!.State, browser, reg.Fingerprint, "synthetic-continuity-code", false)).StatusCode != 202) throw new InvalidOperationException("Continuity source capture failed");
                var claim = await s.ResumeAsync(actor, academy, reg.Provider, intent.Material.State, browser, reg.Fingerprint); var exchange = await s.BeginExchangeAsync(actor, academy, reg, claim);
                if ((await s.FinishExchangeAsync(actor, academy, reg, exchange.Permit!, Tokens(reg, "initial"))).StatusCode != 200) throw new InvalidOperationException("Continuity source quarantine failed");
                return exchange.Permit!;
            });
        }
        async Task<MeetingOAuthExchangePermit> Rotate(MeetingOAuthRegistration reg, MeetingOAuthExchangePermit permit, int version = 1)
        {
            var start = await Run(s => s.BeginRefreshAsync(actor, academy, reg, permit, version)); if (start.StatusCode != 201) throw new InvalidOperationException("Continuity refresh admission failed");
            var result = await Run(s => s.FinishRefreshAsync(actor, academy, reg, start.Material!, Tokens(reg, "rotation" + version)));
            if (result.StatusCode != 200) throw new InvalidOperationException("Continuity refresh completion failed"); return result.Permit!;
        }
        Task<MeetingAccountReviewOutcome> Prepare(MeetingOAuthRegistration reg, MeetingOAuthExchangePermit permit, string subject, string? account = null, IInterceptor? interceptor = null) => Run(s => s.PrepareAccountReviewAsync(actor, academy, reg, permit, new(subject, account, clock.Now)), interceptor);
        var checks = new List<string>(); void Check(string label, bool passed) { if (!passed) throw new InvalidOperationException("MEETING ACCOUNT CONTINUITY " + label); checks.Add(label); Console.WriteLine("MEETING ACCOUNT CONTINUITY CASE " + label + " PASS"); }
        foreach (var provider in new[] { "GoogleWorkspace", "Microsoft365" })
        {
            var reg = Registration(provider); var initial = await Source(reg); var subject = provider == "GoogleWorkspace" ? "synthetic-original-subject" : "019acc9a-6200-7000-8000-000000000002";
            var first = (await Prepare(reg, initial, subject)).Review!;
            Check(provider + "-initial-human-confirmation-not-connected", (await Run(s => s.ConfirmAccountReviewAsync(actor, academy, reg, first.Id, 0, first.ReviewHash, true))).StatusCode == 200);
            clock.Now = clock.Now.AddMinutes(6); var rotated = await Rotate(reg, initial);
            int before; await using (var db = Open()) before = await db.AuditLogs.CountAsync();
            var changed = await Prepare(reg, rotated, "synthetic-changed-subject");
            await using (var db = Open()) Check(provider + "-changed-subject-no-review-no-audit-no-identity-leak", changed.StatusCode == 409 && changed.Code == "ProviderAccountChangedReauthorizationRequired" && changed.Review is null && !JsonSerializer.Serialize(changed).Contains(subject) && await db.MeetingAccountReviews.CountAsync(x => x.ExchangeId == initial.Id) == 1 && await db.AuditLogs.CountAsync() == before);
            var added = await Prepare(reg, rotated, subject, "synthetic-added-account");
            Check(provider + "-changed-account-component-denied", added.StatusCode == 409 && added.Code == "ProviderAccountChangedReauthorizationRequired");
            object response = provider == "GoogleWorkspace" ? new { sub = "synthetic-changed-subject", email_verified = true } : new { id = "019acc9a-6200-7000-8000-000000000003" };
            using (var transport = new HttpMessageInvoker(new ExchangeTransport(() => Task.CompletedTask, JsonSerializer.SerializeToUtf8Bytes(response))))
            {
                var fakeProbe = await Run(s => new MeetingOAuthAccountProbe(s, transport, clock).PrepareReviewAsync(actor, academy, reg, rotated));
                Check(provider + "-fixed-fake-account-probe-cannot-rebind-refresh", fakeProbe.StatusCode == 409 && fakeProbe.Code == "ProviderAccountChangedReauthorizationRequired" && fakeProbe.Review is null);
            }
            var current = await Prepare(reg, rotated, subject); Check(provider + "-expired-baseline-same-identity-needs-new-review", current.StatusCode == 201 && current.Review!.Status == "AwaitingConfirmation" && !current.Review.Connected);
            Check(provider + "-same-identity-confirmation-still-not-connected", (await Run(s => s.ConfirmAccountReviewAsync(actor, academy, reg, current.Review!.Id, 0, current.Review.ReviewHash, true))).StatusCode == 200);
            var again = await Rotate(reg, rotated, 3);
            Check(provider + "-second-refresh-still-pinned-to-original", (await Prepare(reg, again, "synthetic-changed-subject")).StatusCode == 409 && (await Prepare(reg, again, subject)).StatusCode == 201);
            var reauth = await Source(reg); var switched = await Prepare(reg, reauth, "synthetic-new-authorized-exchange-subject");
            Check(provider + "-separate-reauthorization-has-independent-unconfirmed-baseline", reauth.Id != initial.Id && switched.StatusCode == 201 && switched.Review!.Status == "AwaitingConfirmation" && !switched.Review.Connected);
        }
        var registration = Registration(); var faultSource = await Source(registration); var original = (await Prepare(registration, faultSource, "synthetic-fault-subject")).Review!;
        var after = await Rotate(registration, faultSource); var failed = false;
        try { await Prepare(registration, after, "synthetic-fault-subject", interceptor: new OperationCommitFault()); } catch (InvalidOperationException ex) when (ex.Message == "Synthetic operation commit failure after SQL writes") { failed = true; }
        await using (var db = Open()) Check("new-review-audit-failure-preserves-only-baseline", failed && await db.MeetingAccountReviews.CountAsync(x => x.ExchangeId == faultSource.Id) == 1);
        var barrier = new ClaimBarrier(); var contenders = await Task.WhenAll(Prepare(registration, after, "synthetic-fault-subject", interceptor: barrier), Prepare(registration, after, "synthetic-fault-subject", interceptor: barrier));
        Check("two-continuous-reviews-exactly-201-409", contenders.Select(x => x.StatusCode).Order().SequenceEqual(new[] { 201, 409 }));
        var currentReview = contenders.Single(x => x.Review is not null).Review!;
        await using (var db = Open()) Check("race-history-two-revisions-one-new-audit", await db.MeetingAccountReviews.CountAsync(x => x.ExchangeId == faultSource.Id) == 2 && await db.AuditLogs.CountAsync(x => x.EntityId == currentReview.Id && x.Action == "MeetingAccountReviewPreparedNotConnected") == 1);
        await using (var db = Open()) { var old = await db.MeetingAccountReviews.SingleAsync(x => x.Id == original.Id); old.ProtectedEnvelope = "synthetic-forged-history"; await db.SaveChangesAsync(); }
        var historyRead = await Run(s => s.ReadAccountReviewAsync(actor, academy, registration, currentReview.Id));
        Check("corrupt-old-history-denies-current-private-read", historyRead.StatusCode == 409 && historyRead.Code == "AccountContinuityHistoryInvalid" && historyRead.Review is null);
        var historyConfirm = await Run(s => s.ConfirmAccountReviewAsync(actor, academy, registration, currentReview.Id, 0, currentReview.ReviewHash, true));
        await using (var db = Open()) Check("corrupt-old-history-denies-current-confirmation-without-audit", historyConfirm.StatusCode == 409 && historyConfirm.Code == "AccountContinuityHistoryInvalid" && !await db.AuditLogs.AnyAsync(x => x.EntityId == currentReview.Id && x.Action == "MeetingAccountConfirmedNotConnected") && await db.MeetingAccountReviews.AnyAsync(x => x.Id == currentReview.Id && x.Status == "AwaitingConfirmation"));
        var further = await Rotate(registration, after, 3); var corruptNew = await Prepare(registration, further, "synthetic-fault-subject");
        Check("corrupt-old-history-does-not-reset-baseline-after-another-refresh", corruptNew.StatusCode == 409 && corruptNew.Code == "AccountContinuityHistoryInvalid");
        var permission = await Source(registration); await Prepare(registration, permission, "synthetic-permission-subject"); var permissionRotated = await Rotate(registration, permission);
        Check("foreign-academy-cannot-read-continuity-context", (await Run(s => s.PrepareAccountReviewAsync(actor, foreign, registration, permissionRotated, new("synthetic-permission-subject", null, clock.Now)))).StatusCode == 403);
        Check("other-current-admin-cannot-inherit-continuity-context", (await Run(s => s.PrepareAccountReviewAsync(Actor(otherId), academy, registration, permissionRotated, new("synthetic-permission-subject", null, clock.Now)))).StatusCode == 409);
        using (var scope = factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(); var user = (await users.FindByIdAsync(actorId.ToString()))!;
            if (!(await users.RemoveFromRoleAsync(user, "AcademyAdmin")).Succeeded) throw new InvalidOperationException("Continuity role fixture failed");
        }
        Check("revoked-role-cannot-use-matching-history", (await Prepare(registration, permissionRotated, "synthetic-permission-subject")).StatusCode == 403);
        if (checks.Count != 25) throw new InvalidOperationException("Account continuity case count mismatch " + checks.Count);
        var sources = new[] { "apps/api/Infrastructure/Meetings/MeetingAccountReviewStore.cs", "apps/api/Infrastructure/Meetings/MeetingOAuthAccountProbe.cs", "tests/AcademyDesk.Api.Tests/MeetingAccountContinuityTests.cs", "QA/tools/SqlHarness/MeetingAccountContinuityRegression.cs" }.ToDictionary(p => p, p => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(p))));
        var evidence = Path.Combine("QA", "EVIDENCE", "meeting-account-continuity-sql-" + manifest.RunId.ToString("N")); Directory.CreateDirectory(evidence);
        await File.WriteAllTextAsync(Path.Combine(evidence, "result.json"), JsonSerializer.Serialize(new { checks, sources, scope = "Real owned SQL/Identity plus fixed fake account probes; identity continuity is not provider ownership, directory/license entitlement, Connected or live qualification" }, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine("MEETING ACCOUNT CONTINUITY PASS:25 SQL/Identity cases; no silent account switch or permission grant.");
    }
}
