using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using AcademyDesk.Api.Infrastructure.Meetings;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Tests;

public sealed class MeetingGoogleRevocationTests
{
    private const string Refresh = "synthetic-metadata-revoke-refresh";
    private sealed class Clock : TimeProvider { public DateTimeOffset Now = DateTimeOffset.UtcNow; public override DateTimeOffset GetUtcNow() => Now; }
    private static MeetingOAuthRegistration Reg(bool metadata = true, int revision = 1) => MeetingOAuthRegistrationRegistry.Create("https://api.academy.invalid", "https://app.academy.invalid", [new("GoogleWorkspace", revision, "synthetic-metadata-revoke-client",
        "https://api.academy.invalid/api/meetings/oauth/GoogleWorkspace/callback", "https://app.academy.invalid/communication-settings", metadata ? MeetingOAuthFlowProfile.GoogleMetadataProfile : "google-calendar-owned-web/1")]).Resolve("GoogleWorkspace")!;
    private static async Task<(MeetingOAuthAttemptStore Store, MeetingOAuthExchangePermit Permit)> Source(MeetingReadinessTests.Fixture f, Clock clock, MeetingOAuthRegistration reg)
    {
        var store = new MeetingOAuthAttemptStore(f.Db, f.Users, f.Readiness, new EphemeralDataProtectionProvider(), clock); var browser = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        var intent = await store.PrepareAsync(f.Actor, f.Academy.Id, reg.Provider, 0, browser, reg.Fingerprint); Assert.Equal(201, intent.StatusCode);
        Assert.Equal(202, (await store.CaptureAsync(f.Academy.Id, reg.Provider, intent.Material!.State, browser, reg.Fingerprint, "synthetic-metadata-revoke-code", false)).StatusCode);
        var claim = await store.ResumeAsync(f.Actor, f.Academy.Id, reg.Provider, intent.Material.State, browser, reg.Fingerprint); var permit = (await store.BeginExchangeAsync(f.Actor, f.Academy.Id, reg, claim)).Permit!;
        Assert.Equal(200, (await store.FinishExchangeAsync(f.Actor, f.Academy.Id, reg, permit, new(200, "TokensReceivedAccountUnverified", Tokens: new("synthetic-metadata-revoke-access", Refresh, clock.Now.AddHours(1), reg.Flow!.RequiredTokenScopes)))).StatusCode);
        return (store, permit);
    }
    private sealed class Handler(Func<HttpRequestMessage, Task<HttpResponseMessage>> send) : HttpMessageHandler
    { internal int Calls; protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token) { Calls++; return send(request); } }
    private static HttpResponseMessage Empty() => new(HttpStatusCode.OK) { Content = new ByteArrayContent([]) };
    [Theory]
    [InlineData(false, false, 0)] [InlineData(false, true, 0)] [InlineData(true, false, 0)] [InlineData(true, true, 0)]
    [InlineData(false, false, 365)] [InlineData(false, true, 365)] [InlineData(true, false, 365)] [InlineData(true, true, 365)]
    public async Task Acknowledgement_is_not_completed_project_reconciliation(bool metadata, bool requestedMetadata, int days)
    {
        await using var f = await MeetingReadinessTests.Fixture.Create(); var clock = new Clock(); var reg = Reg(metadata); var (store, permit) = await Source(f, clock, reg);
        using var handler = new Handler(_ => Task.FromResult(Empty())); using var transport = new HttpMessageInvoker(handler);
        var outcome = await new MeetingProviderRevocation(store, transport).ExecuteAsync(f.Actor, f.Academy.Id, reg, permit, 2, true, true);
        Assert.Equal(200, outcome.StatusCode); Assert.Equal("ProviderAcknowledgedLocallyDisconnected", outcome.Code); Assert.Equal(1, handler.Calls);
        clock.Now = clock.Now.AddDays(days); var before = await f.Db.AuditLogs.CountAsync();
        var reconnect = await store.PrepareAsync(f.Actor, f.Academy.Id, reg.Provider, 3, WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32)), Reg(requestedMetadata, revision: 2).Fingerprint);
        Assert.Equal(409, reconnect.StatusCode); Assert.Equal("ProviderRevocationNeedsReview", reconnect.Code); Assert.Null(reconnect.Material);
        Assert.Equal(before, await f.Db.AuditLogs.CountAsync()); Assert.Equal(1, await f.Db.MeetingOAuthAttempts.CountAsync());
        var binding = await f.Db.MeetingProviderBindings.SingleAsync(); Assert.Equal("Disconnected", binding.Status); Assert.Equal(3, binding.Version); Assert.Equal(2, binding.Generation);
        var row = await f.Db.MeetingCredentialRevocations.SingleAsync(); Assert.Equal("ProviderAcknowledged", row.Status); Assert.Equal(1, row.Version);
        Assert.Equal(409, (await store.PrepareManualRevocationReviewAsync(f.Actor, f.Academy.Id, reg, row.Id)).StatusCode);
        Assert.Equal(409, (await store.ReadQuarantinedTokensAsync(f.Actor, f.Academy.Id, reg, permit)).StatusCode); Assert.Equal(1, handler.Calls);
    }
    [Fact] public async Task Metadata_profile_can_complete_its_own_consented_revocation_with_no_connected_claim()
    {
        await using var f = await MeetingReadinessTests.Fixture.Create(); var clock = new Clock(); var reg = Reg(); var (store, permit) = await Source(f, clock, reg);
        using var handler = new Handler(async request => {
            Assert.Equal(HttpMethod.Post, request.Method); Assert.Equal("https://oauth2.googleapis.com/revoke", request.RequestUri!.AbsoluteUri); Assert.Null(request.Headers.Authorization);
            Assert.Equal("token=" + Refresh, await request.Content!.ReadAsStringAsync()); Assert.DoesNotContain(Refresh, request.RequestUri.AbsoluteUri); Assert.True(request.Headers.CacheControl!.NoStore);
            Assert.Equal("Disconnected", (await f.Db.MeetingProviderBindings.SingleAsync()).Status); Assert.Equal("Started", (await f.Db.MeetingCredentialRevocations.SingleAsync()).Status);
            Assert.Equal(1, await f.Db.AuditLogs.CountAsync(x => x.Action == "MeetingRevocationStarted")); return Empty(); });
        using var transport = new HttpMessageInvoker(handler); var result = await new MeetingProviderRevocation(store, transport).ExecuteAsync(f.Actor, f.Academy.Id, reg, permit, 2, true, true);
        Assert.Equal(200, result.StatusCode); Assert.Equal(1, handler.Calls); Assert.False(result.Connected); Assert.False(result.CanRetry);
        Assert.Equal("ProviderAcknowledged", (await f.Db.MeetingCredentialRevocations.SingleAsync()).Status); Assert.Equal(409, (await store.ReadQuarantinedTokensAsync(f.Actor, f.Academy.Id, reg, permit)).StatusCode);
    }
    [Theory] [InlineData("disconnect")] [InlineData("project-consent")] [InlineData("anonymous")] [InlineData("foreign")] [InlineData("role")] [InlineData("stamp")]
    [InlineData("old-profile")] [InlineData("registration")] [InlineData("version")] [InlineData("fence")]
    public async Task Metadata_profile_does_not_bypass_consent_or_current_source_authority(string mode)
    {
        await using var f = await MeetingReadinessTests.Fixture.Create(); var clock = new Clock(); var reg = Reg(); var (store, permit) = await Source(f, clock, reg);
        if (mode == "role") await f.Users.RemoveFromRoleAsync(f.User, "Owner"); if (mode == "stamp") await f.Users.UpdateSecurityStampAsync(f.User);
        using var handler = new Handler(_ => throw new InvalidOperationException("Denied request must not send")); using var transport = new HttpMessageInvoker(handler);
        var result = await new MeetingProviderRevocation(store, transport).ExecuteAsync(mode == "anonymous" ? new ClaimsPrincipal() : f.Actor, mode == "foreign" ? Guid.NewGuid() : f.Academy.Id,
            mode == "old-profile" ? Reg(false) : mode == "registration" ? Reg(revision: 2) : reg, mode == "fence" ? permit with { Fence = Guid.NewGuid() } : permit, mode == "version" ? 1 : 2, mode != "disconnect", mode != "project-consent");
        Assert.Contains(result.StatusCode, new[] { 400, 401, 403, 409 }); Assert.Equal(0, handler.Calls); Assert.Null(result.Material); Assert.False(result.Connected);
        Assert.Empty(await f.Db.MeetingCredentialRevocations.ToArrayAsync()); Assert.Equal("VerificationRequired", (await f.Db.MeetingProviderBindings.SingleAsync()).Status);
    }
    [Theory] [InlineData("role-before")] [InlineData("stamp-before")] [InlineData("old-profile")] [InlineData("fence")] [InlineData("wrong-token")] [InlineData("source-cipher")]
    [InlineData("revocation-cipher")] [InlineData("generation")] [InlineData("deadline-before")]
    [InlineData("role-during")] [InlineData("stamp-during")] [InlineData("deadline-during")] [InlineData("generation-during")]
    [InlineData("network")] [InlineData("redirect")] [InlineData("429")] [InlineData("body")] [InlineData("encoding")]
    public async Task Uncertain_or_stale_metadata_revocation_never_retries_or_claims_acknowledgement(string mode)
    {
        await using var f = await MeetingReadinessTests.Fixture.Create(); var clock = new Clock(); var reg = Reg(); var (store, permit) = await Source(f, clock, reg);
        var start = await store.BeginRevocationAsync(f.Actor, f.Academy.Id, reg, permit, 2, true, true); Assert.Equal(201, start.StatusCode); var material = start.Material!;
        if (mode == "role-before") await f.Users.RemoveFromRoleAsync(f.User, "Owner"); if (mode == "stamp-before") await f.Users.UpdateSecurityStampAsync(f.User);
        if (mode == "fence") material = new(material.Id, Guid.NewGuid(), Refresh); if (mode == "wrong-token") material = new(material.Id, material.Fence, "synthetic-wrong-token");
        if (mode == "source-cipher") (await f.Db.MeetingOAuthExchanges.SingleAsync()).ProtectedEnvelope = "corrupt";
        if (mode == "revocation-cipher") (await f.Db.MeetingCredentialRevocations.SingleAsync()).ProtectedEnvelope = "corrupt";
        if (mode == "generation") { var binding = await f.Db.MeetingProviderBindings.SingleAsync(); binding.Version++; binding.Generation++; }
        if (mode == "deadline-before") clock.Now = clock.Now.AddSeconds(30); await f.Db.SaveChangesAsync();
        using var handler = new Handler(async _ => {
            if (mode == "role-during") await f.Users.RemoveFromRoleAsync(f.User, "Owner"); if (mode == "stamp-during") await f.Users.UpdateSecurityStampAsync(f.User);
            if (mode == "deadline-during") clock.Now = clock.Now.AddSeconds(30);
            if (mode == "generation-during") { var binding = await f.Db.MeetingProviderBindings.SingleAsync(); binding.Version++; binding.Generation++; await f.Db.SaveChangesAsync(); }
            if (mode == "network") throw new HttpRequestException(Refresh); var response = Empty();
            if (mode == "redirect") response.StatusCode = HttpStatusCode.Redirect; if (mode == "429") response.StatusCode = HttpStatusCode.TooManyRequests;
            if (mode == "body") response.Content = new StringContent("private"); if (mode == "encoding") response.Content.Headers.ContentEncoding.Add("gzip"); return response; });
        using var transport = new HttpMessageInvoker(handler); var service = new MeetingProviderRevocation(store, transport);
        var currentReg = mode == "old-profile" ? Reg(false) : reg; var outcome = await service.SendStartedAsync(f.Actor, f.Academy.Id, currentReg, material);
        Assert.Contains(outcome.StatusCode, new[] { 403, 409 }); Assert.False(outcome.Connected); Assert.False(outcome.CanRetry); Assert.DoesNotContain(Refresh, JsonSerializer.Serialize(outcome));
        var noSend = mode is "role-before" or "stamp-before" or "old-profile" or "fence" or "wrong-token" or "source-cipher" or "revocation-cipher" or "generation" or "deadline-before";
        Assert.Equal(noSend ? 0 : 1, handler.Calls); Assert.Empty(await f.Db.AuditLogs.Where(x => x.Action == "MeetingRevocationProviderAcknowledged").ToArrayAsync());
        Assert.NotEqual(200, (await service.SendStartedAsync(f.Actor, f.Academy.Id, currentReg, material)).StatusCode); Assert.Equal(noSend ? 0 : 1, handler.Calls);
    }
    [Fact] public async Task Metadata_unknown_recovery_and_manual_report_do_not_clear_project_reconnect_block()
    {
        await using var f = await MeetingReadinessTests.Fixture.Create(); var clock = new Clock(); var reg = Reg(); var (store, permit) = await Source(f, clock, reg);
        var start = await store.BeginRevocationAsync(f.Actor, f.Academy.Id, reg, permit, 2, true, true); var id = start.Material!.Id;
        var browser = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        Assert.Equal("ProviderRevocationNeedsReview", (await store.PrepareAsync(f.Actor, f.Academy.Id, reg.Provider, 3, browser, reg.Fingerprint)).Code);
        clock.Now = clock.Now.AddSeconds(30); Assert.True(await store.RecoverRevocationAsync(f.Academy.Id, id)); Assert.False(await store.RecoverRevocationAsync(f.Academy.Id, id));
        Assert.Equal(409, (await store.FinishRevocationAsync(f.Actor, f.Academy.Id, reg, start.Material, true)).StatusCode);
        var review = (await store.PrepareManualRevocationReviewAsync(f.Actor, f.Academy.Id, reg, id)).Review!;
        var report = await store.ReportManualRevocationAsync(f.Actor, f.Academy.Id, reg, id, 1, review.ReviewHash, review.Ticket, true);
        Assert.Equal(200, report.StatusCode); Assert.False(report.ProviderVerified); Assert.Equal("ManualReportedUnverified", (await f.Db.MeetingCredentialRevocations.SingleAsync()).Status);
        Assert.Equal("ProviderRevocationNeedsReview", (await store.PrepareAsync(f.Actor, f.Academy.Id, reg.Provider, 3, browser, reg.Fingerprint)).Code);
        Assert.Equal("ProviderRevocationNeedsReview", (await store.PrepareAsync(f.Actor, f.Academy.Id, reg.Provider, 3, browser, Reg(false).Fingerprint)).Code);
    }
}
