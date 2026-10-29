using System.Security.Claims;
using System.Text.Json;
using AcademyDesk.Api.Infrastructure.Meetings;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Tests;

public sealed class MeetingManualRevocationTests
{
    private const string Access = "synthetic-manual-access", Refresh = "synthetic-manual-refresh";
    private sealed class Clock : TimeProvider { public DateTimeOffset Now = DateTimeOffset.UtcNow; public override DateTimeOffset GetUtcNow() => Now; }
    private static MeetingOAuthRegistration Registration(string provider = "Microsoft365", int revision = 1) => MeetingOAuthRegistrationRegistry.Create("https://api.academy.invalid", "https://app.academy.invalid",
        [new(provider, revision, "synthetic-client", "https://api.academy.invalid" + MeetingOAuthRegistrationRegistry.CallbackPath(provider), "https://app.academy.invalid/communication-settings",
            provider == "GoogleWorkspace" ? "google-calendar-owned-web/1" : provider == "Microsoft365" ? "microsoft-online-meetings-work/1" : "zoom-public-pkce/1",
            provider == "Microsoft365" ? "019acc9a-6200-7000-8000-000000000001" : null)]).Resolve(provider)!;
    private static async Task<(MeetingOAuthAttemptStore Store, MeetingOAuthExchangePermit Permit, Guid Id)> Setup(MeetingReadinessTests.Fixture f, Clock clock, MeetingOAuthRegistration reg, bool recoverGoogle = true)
    {
        var store = new MeetingOAuthAttemptStore(f.Db, f.Users, f.Readiness, new EphemeralDataProtectionProvider(), clock);
        var browser = WebEncoders.Base64UrlEncode(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
        var intent = await store.PrepareAsync(f.Actor, f.Academy.Id, reg.Provider, 0, browser, reg.Fingerprint); Assert.Equal(201, intent.StatusCode);
        Assert.Equal(202, (await store.CaptureAsync(f.Academy.Id, reg.Provider, intent.Material!.State, browser, reg.Fingerprint, "synthetic-manual-code", false)).StatusCode);
        var claim = await store.ResumeAsync(f.Actor, f.Academy.Id, reg.Provider, intent.Material.State, browser, reg.Fingerprint); var start = await store.BeginExchangeAsync(f.Actor, f.Academy.Id, reg, claim);
        Assert.Equal(200, (await store.FinishExchangeAsync(f.Actor, f.Academy.Id, reg, start.Permit!, new(200, "TokensReceivedAccountUnverified", Tokens: new(Access, Refresh, clock.Now.AddHours(1), reg.Flow!.RequiredTokenScopes)))).StatusCode);
        Assert.Equal(reg.Provider == "GoogleWorkspace" ? 201 : 202, (await store.BeginRevocationAsync(f.Actor, f.Academy.Id, reg, start.Permit!, 2, true, true)).StatusCode);
        var row = await f.Db.MeetingCredentialRevocations.SingleAsync();
        if (reg.Provider == "GoogleWorkspace" && recoverGoogle) { clock.Now = clock.Now.AddSeconds(30); Assert.True(await store.RecoverRevocationAsync(f.Academy.Id, row.Id)); }
        return (store, start.Permit!, row.Id);
    }

    [Theory] [InlineData("GoogleWorkspace", 2)] [InlineData("Microsoft365", 1)] [InlineData("Zoom", 1)]
    public async Task Manual_report_is_explicit_private_audited_not_verified_and_never_connected(string provider, int expectedVersion)
    {
        await using var f = await MeetingReadinessTests.Fixture.Create(); var clock = new Clock(); var reg = Registration(provider); var (store, permit, id) = await Setup(f, clock, reg);
        clock.Now = clock.Now.AddHours(1); // Expired access is not proof of revocation and does not prevent reporting.
        var encryptedSource = (await f.Db.MeetingOAuthExchanges.SingleAsync()).ProtectedEnvelope;
        var prepared = await store.PrepareManualRevocationReviewAsync(f.Actor, f.Academy.Id, reg, id); Assert.Equal(200, prepared.StatusCode); var review = prepared.Review!;
        Assert.Equal(64, review.ReviewHash.Length); Assert.Equal(clock.Now.UtcDateTime.AddMinutes(5), review.ExpiresAtUtc); Assert.False(review.ProviderVerified); Assert.False(review.Connected);
        Assert.DoesNotContain(review.Ticket, JsonSerializer.Serialize(prepared)); Assert.DoesNotContain(Access, JsonSerializer.Serialize(prepared)); Assert.DoesNotContain(Refresh, prepared.ToString());
        Assert.Equal(1, await f.Db.AuditLogs.CountAsync(x => x.Action == "MeetingManualRevocationReviewPrepared"));
        var result = await store.ReportManualRevocationAsync(f.Actor, f.Academy.Id, reg, id, review.Version, review.ReviewHash, review.Ticket, true);
        Assert.Equal(200, result.StatusCode); Assert.Equal("ManualRevocationReportedProviderNotVerified", result.Code); Assert.Null(result.Review); Assert.False(result.ProviderVerified); Assert.False(result.Connected); Assert.False(result.CanRetry);
        var row = await f.Db.MeetingCredentialRevocations.SingleAsync(); Assert.Equal("ManualReportedUnverified", row.Status); Assert.Equal(expectedVersion, row.Version);
        Assert.DoesNotContain(Access, row.ProtectedEnvelope); Assert.DoesNotContain(Refresh, row.ProtectedEnvelope); Assert.Equal(encryptedSource, (await f.Db.MeetingOAuthExchanges.SingleAsync()).ProtectedEnvelope);
        Assert.Equal(1, await f.Db.AuditLogs.CountAsync(x => x.Action == "MeetingRevocationManuallyReportedUnverified")); Assert.Equal("Disconnected", (await f.Db.MeetingProviderBindings.SingleAsync()).Status);
        Assert.Equal(409, (await store.ReportManualRevocationAsync(f.Actor, f.Academy.Id, reg, id, review.Version, review.ReviewHash, review.Ticket, true)).StatusCode);
        Assert.Equal(409, (await store.PrepareManualRevocationReviewAsync(f.Actor, f.Academy.Id, reg, id)).StatusCode);
        Assert.Equal(409, (await store.ReadQuarantinedTokensAsync(f.Actor, f.Academy.Id, reg, permit)).StatusCode); Assert.False(await store.RecoverRevocationAsync(f.Academy.Id, id));
        var browser = WebEncoders.Base64UrlEncode(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
        var reconnect = await store.PrepareAsync(f.Actor, f.Academy.Id, reg.Provider, 3, browser, reg.Fingerprint);
        Assert.Equal(provider == "GoogleWorkspace" ? 409 : 201, reconnect.StatusCode);
        if (provider == "GoogleWorkspace") Assert.Equal("ProviderRevocationNeedsReview", reconnect.Code);
    }

    [Theory] [InlineData("unconfirmed")] [InlineData("version")] [InlineData("hash")] [InlineData("ticket")] [InlineData("expired")]
    [InlineData("future")] [InlineData("anonymous")] [InlineData("foreign")] [InlineData("other-actor")] [InlineData("role")]
    [InlineData("stamp")] [InlineData("registration")] [InlineData("provider")] [InlineData("generation")] [InlineData("binding-version")]
    [InlineData("binding-status")] [InlineData("row-cipher")] [InlineData("row-fence")] [InlineData("source-cipher")] [InlineData("source-fence")] [InlineData("missing")]
    public async Task Stale_tampered_or_unapproved_reports_do_not_change_state_or_audit(string mode)
    {
        await using var f = await MeetingReadinessTests.Fixture.Create(); var clock = new Clock(); var reg = Registration(); var (store, _, id) = await Setup(f, clock, reg);
        var review = (await store.PrepareManualRevocationReviewAsync(f.Actor, f.Academy.Id, reg, id)).Review!;
        if (mode == "expired") clock.Now = clock.Now.AddMinutes(5); if (mode == "future") clock.Now = clock.Now.AddSeconds(-1);
        if (mode == "role") await f.Users.RemoveFromRoleAsync(f.User, "Owner"); if (mode == "stamp") await f.Users.UpdateSecurityStampAsync(f.User);
        if (mode == "registration") reg = Registration(revision: 2); if (mode == "provider") reg = Registration("Zoom");
        if (mode.StartsWith("binding-", StringComparison.Ordinal) || mode == "generation")
        {
            var b = await f.Db.MeetingProviderBindings.SingleAsync(); if (mode == "generation") b.Generation++; if (mode == "binding-version") b.Version++; if (mode == "binding-status") b.Status = "AuthorizationPending"; await f.Db.SaveChangesAsync();
        }
        if (mode == "row-cipher") { (await f.Db.MeetingCredentialRevocations.SingleAsync()).ProtectedEnvelope = "forged"; await f.Db.SaveChangesAsync(); }
        if (mode == "row-fence") { (await f.Db.MeetingCredentialRevocations.SingleAsync()).Fence = Guid.NewGuid(); await f.Db.SaveChangesAsync(); }
        if (mode == "source-cipher") { (await f.Db.MeetingOAuthExchanges.SingleAsync()).ProtectedEnvelope = "forged"; await f.Db.SaveChangesAsync(); }
        if (mode == "source-fence") { (await f.Db.MeetingOAuthExchanges.SingleAsync()).Fence = Guid.NewGuid(); await f.Db.SaveChangesAsync(); }
        var actor = mode == "anonymous" ? new ClaimsPrincipal() : mode == "other-actor" ? new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()) }, "synthetic")) : f.Actor;
        var audits = await f.Db.AuditLogs.CountAsync();
        var result = await store.ReportManualRevocationAsync(actor, mode == "foreign" ? Guid.NewGuid() : f.Academy.Id, reg, mode == "missing" ? Guid.NewGuid() : id,
            mode == "version" ? 1 : review.Version, mode == "hash" ? new string('A', 64) : review.ReviewHash, mode == "ticket" ? "forged" : review.Ticket, mode != "unconfirmed");
        Assert.Contains(result.StatusCode, new[] { 400, 401, 403, 409, 410 }); Assert.Null(result.Review); Assert.False(result.ProviderVerified);
        Assert.Equal("ManualRequired", (await f.Db.MeetingCredentialRevocations.SingleAsync()).Status); Assert.Equal(audits, await f.Db.AuditLogs.CountAsync());
    }

    [Theory] [InlineData("role")] [InlineData("stamp")] [InlineData("source")]
    public async Task Invalid_context_cannot_obtain_a_review_ticket(string mode)
    {
        await using var f = await MeetingReadinessTests.Fixture.Create(); var clock = new Clock(); var reg = Registration(); var (store, _, id) = await Setup(f, clock, reg);
        if (mode == "role") await f.Users.RemoveFromRoleAsync(f.User, "Owner"); if (mode == "stamp") await f.Users.UpdateSecurityStampAsync(f.User);
        if (mode == "source") { (await f.Db.MeetingOAuthExchanges.SingleAsync()).ProtectedEnvelope = "forged"; await f.Db.SaveChangesAsync(); }
        var audits = await f.Db.AuditLogs.CountAsync(); var result = await store.PrepareManualRevocationReviewAsync(f.Actor, f.Academy.Id, reg, id);
        Assert.Contains(result.StatusCode, new[] { 403, 409 }); Assert.Null(result.Review); Assert.Equal(audits, await f.Db.AuditLogs.CountAsync());
    }
    [Theory] [InlineData(false)] [InlineData(true)]
    public async Task Google_in_progress_or_provider_acknowledged_is_not_manual_reportable(bool acknowledged)
    {
        await using var f = await MeetingReadinessTests.Fixture.Create(); var clock = new Clock(); var reg = Registration("GoogleWorkspace"); var (store, _, id) = await Setup(f, clock, reg, false);
        if (acknowledged) { var row = await f.Db.MeetingCredentialRevocations.SingleAsync(); Assert.Equal(200, (await store.FinishRevocationAsync(f.Actor, f.Academy.Id, reg, new(row.Id, row.Fence, Refresh), true)).StatusCode); }
        var audits = await f.Db.AuditLogs.CountAsync(); var result = await store.PrepareManualRevocationReviewAsync(f.Actor, f.Academy.Id, reg, id);
        Assert.Equal(409, result.StatusCode); Assert.Null(result.Review); Assert.Equal(audits, await f.Db.AuditLogs.CountAsync());
    }
}
