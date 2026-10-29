using System.Security.Claims;
using System.Text.Json;
using AcademyDesk.Api.Infrastructure.Meetings;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Tests;

public sealed class MeetingUnknownRefreshTests
{
    private const string Access = "synthetic-unknown-access", Refresh = "synthetic-unknown-refresh";
    private sealed class Clock : TimeProvider { public DateTimeOffset Now = DateTimeOffset.UtcNow; public override DateTimeOffset GetUtcNow() => Now; }
    private static MeetingOAuthRegistration Registration(string provider = "GoogleWorkspace", int revision = 1) => MeetingOAuthRegistrationRegistry.Create("https://api.academy.invalid", "https://app.academy.invalid",
        [new(provider, revision, "synthetic-client", "https://api.academy.invalid" + MeetingOAuthRegistrationRegistry.CallbackPath(provider), "https://app.academy.invalid/communication-settings",
            provider == "GoogleWorkspace" ? "google-calendar-owned-web/1" : provider == "Microsoft365" ? "microsoft-online-meetings-work/1" : "zoom-public-pkce/1",
            provider == "Microsoft365" ? "019acc9a-6200-7000-8000-000000000001" : null)]).Resolve(provider)!;
    private static MeetingOAuthExchangeOutcome Tokens(Clock clock, MeetingOAuthRegistration reg) => new(200, "TokensReceivedAccountUnverified", Tokens: new(Access, "synthetic-unknown-rotated", clock.Now.AddHours(1), reg.Flow!.RequiredTokenScopes));
    private static async Task<(MeetingOAuthAttemptStore Store, MeetingOAuthExchangePermit Old, MeetingOAuthExchangePermit Current, MeetingRefreshMaterial? Material)> Setup(MeetingReadinessTests.Fixture f, Clock clock, MeetingOAuthRegistration reg, string stage = "Unknown")
    {
        var store = new MeetingOAuthAttemptStore(f.Db, f.Users, f.Readiness, new EphemeralDataProtectionProvider(), clock);
        var browser = WebEncoders.Base64UrlEncode(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)); var intent = await store.PrepareAsync(f.Actor, f.Academy.Id, reg.Provider, 0, browser, reg.Fingerprint); Assert.Equal(201, intent.StatusCode);
        Assert.Equal(202, (await store.CaptureAsync(f.Academy.Id, reg.Provider, intent.Material!.State, browser, reg.Fingerprint, "synthetic-unknown-code", false)).StatusCode);
        var claim = await store.ResumeAsync(f.Actor, f.Academy.Id, reg.Provider, intent.Material.State, browser, reg.Fingerprint); var exchange = await store.BeginExchangeAsync(f.Actor, f.Academy.Id, reg, claim);
        Assert.Equal(200, (await store.FinishExchangeAsync(f.Actor, f.Academy.Id, reg, exchange.Permit!, new(200, "TokensReceivedAccountUnverified", Tokens: new(Access, Refresh, clock.Now.AddHours(1), reg.Flow!.RequiredTokenScopes)))).StatusCode);
        if (stage == "Quarantined") return (store, exchange.Permit!, exchange.Permit!, null);
        var refresh = await store.BeginRefreshAsync(f.Actor, f.Academy.Id, reg, exchange.Permit!, 1); Assert.Equal(201, refresh.StatusCode);
        var current = new MeetingOAuthExchangePermit(refresh.Material!.Id, refresh.Material.Fence);
        if (stage == "Started") return (store, exchange.Permit!, current, refresh.Material);
        if (stage == "Recovered") { clock.Now = clock.Now.AddSeconds(30); Assert.True(await store.RecoverRefreshAsync(f.Academy.Id, current.Id)); }
        else if (stage == "Completed") { Assert.Equal(200, (await store.FinishRefreshAsync(f.Actor, f.Academy.Id, reg, refresh.Material, Tokens(clock, reg))).StatusCode); }
        else Assert.Equal(409, (await store.FinishRefreshAsync(f.Actor, f.Academy.Id, reg, refresh.Material, new(502, "RefreshOutcomeUnknown", true))).StatusCode);
        return (store, exchange.Permit!, current, refresh.Material);
    }
    [Theory] [InlineData("GoogleWorkspace", "Unknown")] [InlineData("GoogleWorkspace", "Recovered")] [InlineData("Microsoft365", "Unknown")] [InlineData("Microsoft365", "Recovered")]
    public async Task Confirmed_unknown_refresh_disconnect_is_atomic_manual_only_and_never_revocation_proof(string provider, string stage)
    {
        await using var f = await MeetingReadinessTests.Fixture.Create(); var clock = new Clock(); var reg = Registration(provider); var (store, old, current, material) = await Setup(f, clock, reg, stage);
        var before = await f.Db.AuditLogs.CountAsync(); var result = await store.DisconnectUnknownRefreshAsync(f.Actor, f.Academy.Id, reg, current, 3, 3, true);
        Assert.Equal(202, result.StatusCode); Assert.Equal("LocallyDisconnectedRefreshFamilyRevocationManualRequired", result.Code); Assert.Null(result.Material); Assert.False(result.CanRetry); Assert.False(result.Connected);
        var exchange = await f.Db.MeetingOAuthExchanges.SingleAsync(); Assert.Equal("RefreshAbandoned", exchange.Status); Assert.Equal(4, exchange.Version); Assert.Equal(3, exchange.BindingVersion);
        Assert.DoesNotContain(Refresh, exchange.ProtectedEnvelope); var binding = await f.Db.MeetingProviderBindings.SingleAsync(); Assert.Equal("Disconnected", binding.Status); Assert.Equal(4, binding.Version); Assert.Equal(2, binding.Generation);
        var row = await f.Db.MeetingCredentialRevocations.SingleAsync(); Assert.Equal("ManualRequired", row.Status); Assert.Equal(0, row.Version); Assert.DoesNotContain(Refresh, row.ProtectedEnvelope); Assert.Equal(before + 3, await f.Db.AuditLogs.CountAsync());
        Assert.Equal(409, (await store.DisconnectUnknownRefreshAsync(f.Actor, f.Academy.Id, reg, current, 3, 3, true)).StatusCode);
        Assert.Equal(409, (await store.ReadQuarantinedTokensAsync(f.Actor, f.Academy.Id, reg, current)).StatusCode); Assert.Equal(409, (await store.ReadQuarantinedTokensAsync(f.Actor, f.Academy.Id, reg, old)).StatusCode);
        Assert.Equal(409, (await store.FinishRefreshAsync(f.Actor, f.Academy.Id, reg, material!, Tokens(clock, reg))).StatusCode); Assert.False(await store.RecoverRefreshAsync(f.Academy.Id, current.Id));
        Assert.False(await store.RecoverRevocationAsync(f.Academy.Id, row.Id));
        if (provider == "GoogleWorkspace") Assert.Equal("ProviderRevocationNeedsReview", (await store.PrepareAsync(f.Actor, f.Academy.Id, provider, 4, WebEncoders.Base64UrlEncode(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)), reg.Fingerprint)).Code);
        var reviewResult = await store.PrepareManualRevocationReviewAsync(f.Actor, f.Academy.Id, reg, row.Id); Assert.Equal(200, reviewResult.StatusCode); var review = reviewResult.Review!;
        Assert.DoesNotContain(review.Ticket, JsonSerializer.Serialize(reviewResult)); var reported = await store.ReportManualRevocationAsync(f.Actor, f.Academy.Id, reg, row.Id, 0, review.ReviewHash, review.Ticket, true);
        Assert.Equal(200, reported.StatusCode); Assert.False(reported.ProviderVerified); Assert.Equal("ManualReportedUnverified", row.Status); Assert.Equal(1, row.Version);
        Assert.Equal(409, (await store.FinishRevocationAsync(f.Actor, f.Academy.Id, reg, new(row.Id, row.Fence, Refresh), true)).StatusCode);
        var reconnect = await store.PrepareAsync(f.Actor, f.Academy.Id, provider, 4, WebEncoders.Base64UrlEncode(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)), reg.Fingerprint);
        Assert.Equal(provider == "GoogleWorkspace" ? 409 : 201, reconnect.StatusCode);
    }
    [Theory] [InlineData("GoogleWorkspace")] [InlineData("Microsoft365")]
    public async Task Existing_exact_local_disconnect_is_retained_not_applied_twice(string provider)
    {
        await using var f = await MeetingReadinessTests.Fixture.Create(); var clock = new Clock(); var reg = Registration(provider); var (store, _, current, _) = await Setup(f, clock, reg);
        Assert.Equal(200, (await store.DisconnectLocallyAsync(f.Actor, f.Academy.Id, provider, 3, true)).StatusCode);
        Assert.Equal(202, (await store.DisconnectUnknownRefreshAsync(f.Actor, f.Academy.Id, reg, current, 3, 4, true)).StatusCode);
        var b = await f.Db.MeetingProviderBindings.SingleAsync(); Assert.Equal(2, b.Generation); Assert.Equal(4, b.Version);
        Assert.Equal(1, await f.Db.AuditLogs.CountAsync(x => x.Action == "MeetingUnknownRefreshLocalDisconnectRetained"));
        var row = await f.Db.MeetingCredentialRevocations.SingleAsync(); Assert.Equal(200, (await store.PrepareManualRevocationReviewAsync(f.Actor, f.Academy.Id, reg, row.Id)).StatusCode);
    }
    [Theory] [InlineData("GoogleWorkspace")] [InlineData("Microsoft365")]
    public async Task Later_refresh_cycles_retire_the_current_odd_unknown_version(string provider)
    {
        await using var f = await MeetingReadinessTests.Fixture.Create(); var clock = new Clock(); var reg = Registration(provider); var (store, _, current, _) = await Setup(f, clock, reg, "Completed");
        var start = await store.BeginRefreshAsync(f.Actor, f.Academy.Id, reg, current, 3); Assert.Equal(201, start.StatusCode);
        Assert.Equal(409, (await store.FinishRefreshAsync(f.Actor, f.Academy.Id, reg, start.Material!, new(502, "RefreshOutcomeUnknown", true))).StatusCode);
        Assert.Equal(202, (await store.DisconnectUnknownRefreshAsync(f.Actor, f.Academy.Id, reg, new(start.Material!.Id, start.Material.Fence), 5, 4, true)).StatusCode);
        Assert.Equal(6, (await f.Db.MeetingOAuthExchanges.SingleAsync()).Version);
    }
    [Theory] [InlineData("unconfirmed")] [InlineData("anonymous")] [InlineData("foreign")] [InlineData("role")] [InlineData("stamp")]
    [InlineData("registration")] [InlineData("fence")] [InlineData("old-fence")] [InlineData("binding-version")] [InlineData("exchange-version")]
    [InlineData("overflow")] [InlineData("tamper")] [InlineData("generation")] [InlineData("binding-status")] [InlineData("binding-overflow")]
    [InlineData("source-binding")] [InlineData("missing")] [InlineData("other-actor")]
    public async Task Invalid_context_cannot_retire_or_disconnect_or_add_manual_evidence(string mode)
    {
        await using var f = await MeetingReadinessTests.Fixture.Create(); var clock = new Clock(); var reg = Registration(); var (store, old, current, _) = await Setup(f, clock, reg);
        if (mode == "role") await f.Users.RemoveFromRoleAsync(f.User, "Owner"); if (mode == "stamp") await f.Users.UpdateSecurityStampAsync(f.User);
        if (mode == "registration") reg = Registration(revision: 2); if (mode == "fence") current = current with { Fence = Guid.NewGuid() }; if (mode == "old-fence") current = old;
        if (mode == "missing") current = current with { Id = Guid.NewGuid() };
        if (mode == "generation" || mode == "binding-status" || mode == "binding-overflow") { var b = await f.Db.MeetingProviderBindings.SingleAsync(); if (mode == "generation") b.Generation++; if (mode == "binding-status") b.Status = "AuthorizationPending"; if (mode == "binding-overflow") b.Version = int.MaxValue; await f.Db.SaveChangesAsync(); }
        if (mode == "tamper") { (await f.Db.MeetingOAuthExchanges.SingleAsync()).ProtectedEnvelope = "forged"; await f.Db.SaveChangesAsync(); }
        if (mode == "source-binding") { (await f.Db.MeetingOAuthExchanges.SingleAsync()).BindingVersion++; await f.Db.SaveChangesAsync(); }
        var actor = mode == "anonymous" ? new ClaimsPrincipal() : mode == "other-actor" ? new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()) }, "synthetic")) : f.Actor;
        var before = await f.Db.AuditLogs.CountAsync(); var result = await store.DisconnectUnknownRefreshAsync(actor, mode == "foreign" ? Guid.NewGuid() : f.Academy.Id, reg, current, mode == "exchange-version" ? 1 : mode == "overflow" ? int.MaxValue : 3, mode == "binding-version" ? 2 : 3, mode != "unconfirmed");
        Assert.Contains(result.StatusCode, new[] { 400, 401, 403, 409 }); Assert.Null(result.Material); Assert.Equal(before, await f.Db.AuditLogs.CountAsync()); Assert.Empty(await f.Db.MeetingCredentialRevocations.ToListAsync()); Assert.Equal("RefreshUnknown", (await f.Db.MeetingOAuthExchanges.SingleAsync()).Status);
    }
    [Theory] [InlineData("Quarantined")] [InlineData("Started")]
    public async Task Known_or_still_pending_refresh_must_not_be_retired_as_unknown(string stage)
    {
        await using var f = await MeetingReadinessTests.Fixture.Create(); var clock = new Clock(); var reg = Registration(); var (store, _, current, _) = await Setup(f, clock, reg, stage);
        var before = await f.Db.AuditLogs.CountAsync(); Assert.Equal(409, (await store.DisconnectUnknownRefreshAsync(f.Actor, f.Academy.Id, reg, current, 3, 3, true)).StatusCode);
        Assert.Equal(before, await f.Db.AuditLogs.CountAsync()); Assert.Empty(await f.Db.MeetingCredentialRevocations.ToListAsync());
    }
    [Fact] public async Task Unqualified_zoom_refresh_cannot_get_an_unknown_family_retirement()
    {
        await using var f = await MeetingReadinessTests.Fixture.Create(); var clock = new Clock(); var reg = Registration("Zoom"); var (store, _, current, _) = await Setup(f, clock, reg, "Quarantined");
        Assert.Equal(503, (await store.DisconnectUnknownRefreshAsync(f.Actor, f.Academy.Id, reg, current, 3, 3, true)).StatusCode); Assert.Empty(await f.Db.MeetingCredentialRevocations.ToListAsync());
    }
}
