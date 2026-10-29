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
    private sealed record ZoomRecoverySource(Guid Id, MeetingOAuthExchangePermit Permit, int BindingVersion);
    private static async Task VerifyMeetingZoomRecoveryAsync(QaApiFactory factory, QaRunManifest manifest)
    {
        string connection; using (var scope = factory.Services.CreateScope()) connection = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>().Database.GetConnectionString()!;
        var target = new SqlConnectionStringBuilder(connection); if (target.DataSource != manifest.SqlServer || target.InitialCatalog != manifest.Database || target.UserID != manifest.RuntimeLogin) throw new InvalidOperationException("Zoom recovery requires exact owned SQL target");
        AcademyDeskDbContext Open(IInterceptor? interceptor = null) { var b = new DbContextOptionsBuilder<AcademyDeskDbContext>().UseSqlServer(connection); if (interceptor is not null) b.AddInterceptors(interceptor); return new(b.Options); }
        var tenant = new Academy { Name = "Synthetic Zoom Manual Recovery Academy", EnabledModulesJson = "[\"Core\",\"Engagement\"]" }; await using (var db = Open()) { db.Add(tenant); await db.SaveChangesAsync(); }
        var academy = tenant.Id; var actorId = await CreateAccessActorAsync(factory, "qa-zoom-recovery@example.invalid", "AcademyAdmin", academy); var otherId = await CreateAccessActorAsync(factory, "qa-zoom-recovery-other@example.invalid", "AcademyAdmin", academy);
        ClaimsPrincipal Actor(Guid id) => new(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, id.ToString()) }, "synthetic-zoom-recovery"));
        var actor = Actor(actorId); var clock = new OperationClock(); var protection = factory.Services.GetRequiredService<IDataProtectionProvider>();
        async Task<T> Run<T>(Func<MeetingOAuthAttemptStore, Task<T>> action, IInterceptor? interceptor = null)
        { using var scope = factory.Services.CreateScope(); await using var db = Open(interceptor); var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(); return await action(new(db, users, new(db, users, clock), protection, clock)); }
        MeetingOAuthRegistration Registration(bool backend = true, int revision = 1) => MeetingOAuthRegistrationRegistry.Create("https://api.academy.invalid", "https://app.academy.invalid", [new("Zoom", revision, "synthetic-recovery-client", "https://api.academy.invalid/api/meetings/oauth/Zoom/callback", "https://app.academy.invalid/communication-settings", backend ? MeetingOAuthFlowProfile.ZoomConfidentialProfile : "zoom-public-pkce/1")]).Resolve("Zoom")!;
        var reg = Registration();
        async Task<ZoomRecoverySource> Source()
        {
            int version; await using (var db = Open()) version = await db.MeetingProviderBindings.Where(x => x.AcademyId == academy && x.Provider == reg.Provider).Select(x => (int?)x.Version).SingleOrDefaultAsync() ?? 0;
            return await Run(async store => {
                var browser = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32)); var intent = await store.PrepareAsync(actor, academy, reg.Provider, version, browser, reg.Fingerprint);
                if (intent.StatusCode != 201 || (await store.CaptureAsync(academy, reg.Provider, intent.Material!.State, browser, reg.Fingerprint, "synthetic-recovery-code", false)).StatusCode != 202) throw new InvalidOperationException("Zoom recovery source capture failed");
                var claim = await store.ResumeAsync(actor, academy, reg.Provider, intent.Material.State, browser, reg.Fingerprint); var permit = (await store.BeginExchangeAsync(actor, academy, reg, claim)).Permit!;
                if ((await store.FinishExchangeAsync(actor, academy, reg, permit, new(200, "TokensReceivedAccountUnverified", Tokens: new("synthetic-recovery-access", "synthetic-recovery-refresh", clock.Now.AddHours(1), reg.Flow!.RequiredTokenScopes)))).StatusCode != 200) throw new InvalidOperationException("Zoom recovery quarantine failed");
                var started = await store.BeginRefreshAsync(actor, academy, reg, permit, 1);
                if (started.StatusCode != 201 || (await store.FinishRefreshAsync(actor, academy, reg, started.Material!, new(502, "SyntheticUnknownRefresh", true))).StatusCode != 409) throw new InvalidOperationException("Zoom recovery unknown fixture failed");
                var current = new MeetingOAuthExchangePermit(permit.Id, started.Material!.Fence);
                if ((await store.DisconnectUnknownRefreshAsync(actor, academy, reg, current, 3, version + 3, true)).StatusCode != 202) throw new InvalidOperationException("Zoom recovery containment failed");
                await using var db = Open(); var id = await db.MeetingCredentialRevocations.Where(x => x.ExchangeId == permit.Id).Select(x => x.Id).SingleAsync(); return new ZoomRecoverySource(id, current, version + 4);
            });
        }
        Task<MeetingManualRevocationOutcome> Review(ZoomRecoverySource source, IInterceptor? interceptor = null) => Run(s => s.PrepareManualRevocationReviewAsync(actor, academy, reg, source.Id), interceptor);
        Task<MeetingManualRevocationOutcome> Report(MeetingManualRevocationReview review, IInterceptor? interceptor = null) => Run(s => s.ReportManualRevocationAsync(actor, academy, reg, review.Id, review.Version, review.ReviewHash, review.Ticket, true), interceptor);
        var checks = new List<string>(); void Check(string label, bool pass) { if (!pass) throw new InvalidOperationException("MEETING ZOOM RECOVERY " + label); checks.Add(label); Console.WriteLine("MEETING ZOOM RECOVERY CASE " + label + " PASS"); }
        var source = await Source(); var prepared = await Review(source); var review = prepared.Review;
        Check("legitimate-confidential-unknown-family-review-succeeds", prepared.StatusCode == 200 && review is not null && !review.ProviderVerified);
        await using (var db = Open()) Check("private-ticket-requires-audit-and-never-serializes", !JsonSerializer.Serialize(prepared).Contains(review!.Ticket) && !prepared.ToString().Contains(review.Ticket) && await db.AuditLogs.CountAsync(x => x.EntityId == source.Id && x.Action == "MeetingManualRevocationReviewPrepared") == 1);
        var reported = await Report(review!); Check("human-report-never-provider-verification-or-connected", reported is { StatusCode: 200, ProviderVerified: false, Connected: false, CanRetry: false });
        await using (var db = Open()) Check("durable-reported-v1-disconnected-and-no-provider-ack", await db.MeetingCredentialRevocations.AnyAsync(x => x.Id == source.Id && x.Status == "ManualReportedUnverified" && x.Version == 1) && !await db.MeetingCredentialRevocations.AnyAsync(x => x.Id == source.Id && x.Status == "ProviderAcknowledged") && await db.MeetingProviderBindings.AnyAsync(x => x.AcademyId == academy && x.Status == "Disconnected" && x.Version == source.BindingVersion));
        Check("retired-credentials-and-refresh-remain-inaccessible", (await Run(s => s.ReadQuarantinedTokensAsync(actor, academy, reg, source.Permit))).StatusCode == 409 && (await Run(s => s.BeginRefreshAsync(actor, academy, reg, source.Permit, 3))).StatusCode == 409);
        Check("report-replay-and-review-reuse-denied", (await Report(review!)).StatusCode == 409 && (await Review(source)).StatusCode == 409);
        Check("reported-is-not-recovered-as-provider-ack", !await Run(s => s.RecoverRevocationAsync(academy, source.Id)));
        var race = await Source(); var raceReview = (await Review(race)).Review!; var barrier = new ClaimBarrier(); var outcomes = await Task.WhenAll(Report(raceReview, barrier), Report(raceReview, barrier));
        Check("simultaneous-human-reports-exactly-200-409", outcomes.Select(x => x.StatusCode).Order().SequenceEqual(new[] { 200, 409 }));
        await using (var db = Open()) Check("race-one-v1-and-one-required-report-audit", await db.MeetingCredentialRevocations.AnyAsync(x => x.Id == race.Id && x.Status == "ManualReportedUnverified" && x.Version == 1) && await db.AuditLogs.CountAsync(x => x.EntityId == race.Id && x.Action == "MeetingRevocationManuallyReportedUnverified") == 1);
        var fault = await Source(); var failed = false;
        try { await Review(fault, new OperationCommitFault()); } catch (InvalidOperationException ex) when (ex.Message == "Synthetic operation commit failure after SQL writes") { failed = true; }
        await using (var db = Open()) Check("review-audit-fault-no-ticket-or-audit", failed && await db.MeetingCredentialRevocations.AnyAsync(x => x.Id == fault.Id && x.Status == "ManualRequired" && x.Version == 0) && !await db.AuditLogs.AnyAsync(x => x.EntityId == fault.Id && x.Action == "MeetingManualRevocationReviewPrepared"));
        var retried = await Review(fault); var faultReview = retried.Review!; Check("review-after-proved-rollback", retried.StatusCode == 200); failed = false;
        try { await Report(faultReview, new OperationCommitFault()); } catch (InvalidOperationException ex) when (ex.Message == "Synthetic operation commit failure after SQL writes") { failed = true; }
        await using (var db = Open()) Check("report-audit-fault-completely-rolls-back-state", failed && await db.MeetingCredentialRevocations.AnyAsync(x => x.Id == fault.Id && x.Status == "ManualRequired" && x.Version == 0) && !await db.AuditLogs.AnyAsync(x => x.EntityId == fault.Id && x.Action == "MeetingRevocationManuallyReportedUnverified"));
        Check("report-after-proved-rollback-remains-unverified", (await Report(faultReview)) is { StatusCode: 200, ProviderVerified: false });
        var denied = await Source(); var ticket = (await Review(denied)).Review!; var before = clock.Now; clock.Now = clock.Now.AddMinutes(5);
        Check("exact-ticket-expiry-denied", (await Report(ticket)).StatusCode == 410); clock.Now = before;
        Check("wrong-hash-denied", (await Run(s => s.ReportManualRevocationAsync(actor, academy, reg, denied.Id, 0, new string('A', 64), ticket.Ticket, true))).StatusCode == 409);
        Check("tampered-ticket-denied", (await Run(s => s.ReportManualRevocationAsync(actor, academy, reg, denied.Id, 0, ticket.ReviewHash, "forged", true))).StatusCode == 409);
        Check("explicit-human-declaration-required", (await Run(s => s.ReportManualRevocationAsync(actor, academy, reg, denied.Id, 0, ticket.ReviewHash, ticket.Ticket, false))).StatusCode == 400);
        Check("public-profile-cannot-inherit-backend-recovery", (await Run(s => s.ReportManualRevocationAsync(actor, academy, Registration(false), denied.Id, 0, ticket.ReviewHash, ticket.Ticket, true))).StatusCode == 409);
        Check("changed-registration-cannot-inherit-ticket", (await Run(s => s.ReportManualRevocationAsync(actor, academy, Registration(revision: 2), denied.Id, 0, ticket.ReviewHash, ticket.Ticket, true))).StatusCode == 409);
        Check("foreign-academy-cannot-read-or-report", (await Run(s => s.ReportManualRevocationAsync(actor, Guid.NewGuid(), reg, denied.Id, 0, ticket.ReviewHash, ticket.Ticket, true))).StatusCode == 403 && (await Run(s => s.PrepareManualRevocationReviewAsync(actor, Guid.NewGuid(), reg, denied.Id))).StatusCode == 403);
        Check("other-current-admin-cannot-read-or-report", (await Run(s => s.ReportManualRevocationAsync(Actor(otherId), academy, reg, denied.Id, 0, ticket.ReviewHash, ticket.Ticket, true))).StatusCode == 409 && (await Run(s => s.PrepareManualRevocationReviewAsync(Actor(otherId), academy, reg, denied.Id))).StatusCode == 409);
        Check("anonymous-cannot-read-or-report", (await Run(s => s.ReportManualRevocationAsync(new ClaimsPrincipal(), academy, reg, denied.Id, 0, ticket.ReviewHash, ticket.Ticket, true))).StatusCode == 401 && (await Run(s => s.PrepareManualRevocationReviewAsync(new ClaimsPrincipal(), academy, reg, denied.Id))).StatusCode == 401);
        Check("wrong-version-does-not-report", (await Run(s => s.ReportManualRevocationAsync(actor, academy, reg, denied.Id, 1, ticket.ReviewHash, ticket.Ticket, true))).StatusCode == 409);
        Check("ticket-cannot-target-another-row", (await Run(s => s.ReportManualRevocationAsync(actor, academy, reg, Guid.NewGuid(), 0, ticket.ReviewHash, ticket.Ticket, true))).StatusCode == 409);
        var sourceCorrupt = await Source(); var sourceTicket = (await Review(sourceCorrupt)).Review!; await using (var db = Open()) { (await db.MeetingOAuthExchanges.SingleAsync(x => x.Id == sourceCorrupt.Permit.Id)).ProtectedEnvelope = "corrupt"; await db.SaveChangesAsync(); }
        Check("corrupt-source-invalidates-private-ticket", (await Report(sourceTicket)).StatusCode == 409);
        var rowCorrupt = await Source(); var rowTicket = (await Review(rowCorrupt)).Review!; await using (var db = Open()) { (await db.MeetingCredentialRevocations.SingleAsync(x => x.Id == rowCorrupt.Id)).ProtectedEnvelope = "corrupt"; await db.SaveChangesAsync(); }
        Check("corrupt-revocation-invalidates-private-ticket", (await Report(rowTicket)).StatusCode == 409);
        var changed = await Source(); var changedTicket = (await Review(changed)).Review!; await using (var db = Open()) { var binding = await db.MeetingProviderBindings.SingleAsync(x => x.AcademyId == academy && x.Provider == reg.Provider); binding.Version++; binding.Generation++; await db.SaveChangesAsync(); }
        Check("changed-binding-generation-invalidates-report", (await Report(changedTicket)).StatusCode == 409);
        var stamp = await Source(); var stampTicket = (await Review(stamp)).Review!; using (var scope = factory.Services.CreateScope()) { var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(); if (!(await users.UpdateSecurityStampAsync((await users.FindByIdAsync(actorId.ToString()))!)).Succeeded) throw new InvalidOperationException("Zoom recovery stamp fixture failed"); }
        Check("revoked-stamp-cannot-read-or-report", (await Report(stampTicket)).StatusCode == 409 && (await Review(stamp)).StatusCode == 409);
        var role = await Source(); var roleTicket = (await Review(role)).Review!; using (var scope = factory.Services.CreateScope()) { var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(); if (!(await users.RemoveFromRoleAsync((await users.FindByIdAsync(actorId.ToString()))!, "AcademyAdmin")).Succeeded) throw new InvalidOperationException("Zoom recovery role fixture failed"); }
        Check("revoked-role-cannot-read-or-report", (await Report(roleTicket)).StatusCode == 403 && (await Review(role)).StatusCode == 403);
        if (checks.Count != 29) throw new InvalidOperationException("Zoom recovery case count mismatch " + checks.Count);
        var sources = new[] { "apps/api/Infrastructure/Meetings/MeetingRevocationStore.cs", "apps/api/Infrastructure/Meetings/MeetingManualRevocationStore.cs", "apps/api/Infrastructure/Meetings/MeetingUnknownRefreshStore.cs", "apps/api/Infrastructure/Meetings/MeetingRefreshStore.cs", "tests/AcademyDesk.Api.Tests/MeetingZoomRecoveryTests.cs", "QA/tools/SqlHarness/MeetingZoomRecoveryRegression.cs" }.ToDictionary(p => p, p => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(p))));
        var path = Path.Combine("QA", "EVIDENCE", "meeting-zoom-recovery-sql-" + manifest.RunId.ToString("N")); Directory.CreateDirectory(path);
        await File.WriteAllTextAsync(Path.Combine(path, "result.json"), JsonSerializer.Serialize(new { checks, sources, scope = "Owned SQL/Identity; private Zoom unknown-refresh manual report only, no external provider calls/independent verified revocation/reconnect permission/Connected/public UI/release acceptance" }, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine("MEETING ZOOM RECOVERY PASS:29 SQL/Identity cases; manual reports remain provider-unverified.");
    }
}
