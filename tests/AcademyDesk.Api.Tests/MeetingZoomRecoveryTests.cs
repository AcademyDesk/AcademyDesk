using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using AcademyDesk.Api.Infrastructure.Meetings;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Tests;

public sealed class MeetingZoomRecoveryTests
{
    private sealed class Clock : TimeProvider { public DateTimeOffset Now = DateTimeOffset.UtcNow; public override DateTimeOffset GetUtcNow() => Now; }
    private static MeetingOAuthRegistration Reg(bool backend = true, int revision = 1) => MeetingOAuthRegistrationRegistry.Create("https://api.academy.invalid", "https://app.academy.invalid", [new("Zoom", revision, "synthetic-recovery-client",
        "https://api.academy.invalid/api/meetings/oauth/Zoom/callback", "https://app.academy.invalid/communication-settings", backend ? MeetingOAuthFlowProfile.ZoomConfidentialProfile : "zoom-public-pkce/1")]).Resolve("Zoom")!;
    private static async Task<(MeetingOAuthAttemptStore Store, Guid Id, MeetingOAuthExchangePermit Permit)> Retire(MeetingReadinessTests.Fixture f, Clock clock, MeetingOAuthRegistration reg)
    {
        var store = new MeetingOAuthAttemptStore(f.Db, f.Users, f.Readiness, new EphemeralDataProtectionProvider(), clock); var browser = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        var intent = await store.PrepareAsync(f.Actor, f.Academy.Id, reg.Provider, 0, browser, reg.Fingerprint);
        Assert.Equal(202, (await store.CaptureAsync(f.Academy.Id, reg.Provider, intent.Material!.State, browser, reg.Fingerprint, "synthetic-recovery-code", false)).StatusCode);
        var claim = await store.ResumeAsync(f.Actor, f.Academy.Id, reg.Provider, intent.Material.State, browser, reg.Fingerprint);
        var permit = (await store.BeginExchangeAsync(f.Actor, f.Academy.Id, reg, claim)).Permit!;
        Assert.Equal(200, (await store.FinishExchangeAsync(f.Actor, f.Academy.Id, reg, permit, new(200, "TokensReceivedAccountUnverified", Tokens: new("synthetic-recovery-access", "synthetic-recovery-refresh", clock.Now.AddHours(1), reg.Flow!.RequiredTokenScopes)))).StatusCode);
        var start = await store.BeginRefreshAsync(f.Actor, f.Academy.Id, reg, permit, 1); Assert.Equal(201, start.StatusCode);
        Assert.Equal(409, (await store.FinishRefreshAsync(f.Actor, f.Academy.Id, reg, start.Material!, new(502, "SyntheticLostRefreshResponse", true))).StatusCode);
        var current = new MeetingOAuthExchangePermit(permit.Id, start.Material!.Fence);
        Assert.Equal(202, (await store.DisconnectUnknownRefreshAsync(f.Actor, f.Academy.Id, reg, current, 3, 3, true)).StatusCode);
        return (store, (await f.Db.MeetingCredentialRevocations.SingleAsync()).Id, current);
    }
    [Fact] public async Task Legitimate_Zoom_unknown_family_can_be_reviewed_and_reported_but_never_verified()
    {
        await using var f = await MeetingReadinessTests.Fixture.Create(); var clock = new Clock(); var reg = Reg(); var (store, id, permit) = await Retire(f, clock, reg);
        var prepared = await store.PrepareManualRevocationReviewAsync(f.Actor, f.Academy.Id, reg, id); Assert.Equal(200, prepared.StatusCode);
        var review = Assert.IsType<MeetingManualRevocationReview>(prepared.Review); Assert.False(review.ProviderVerified); Assert.DoesNotContain(review.Ticket, JsonSerializer.Serialize(prepared));
        var reported = await store.ReportManualRevocationAsync(f.Actor, f.Academy.Id, reg, id, 0, review.ReviewHash, review.Ticket, true);
        Assert.Equal(200, reported.StatusCode); Assert.False(reported.ProviderVerified); Assert.False(reported.Connected); Assert.False(reported.CanRetry);
        Assert.Equal("ManualReportedUnverified", (await f.Db.MeetingCredentialRevocations.SingleAsync()).Status);
        Assert.Equal("RefreshAbandoned", (await f.Db.MeetingOAuthExchanges.SingleAsync()).Status); Assert.Equal("Disconnected", (await f.Db.MeetingProviderBindings.SingleAsync()).Status);
        Assert.Equal(409, (await store.ReadQuarantinedTokensAsync(f.Actor, f.Academy.Id, reg, permit)).StatusCode);
        Assert.Equal(409, (await store.BeginRefreshAsync(f.Actor, f.Academy.Id, reg, permit, 3)).StatusCode);
        Assert.Equal(409, (await store.PrepareManualRevocationReviewAsync(f.Actor, f.Academy.Id, reg, id)).StatusCode);
        Assert.False(await store.RecoverRevocationAsync(f.Academy.Id, id));
    }

    [Theory] [InlineData("declaration", 400)] [InlineData("hash", 409)] [InlineData("ticket", 409)] [InlineData("version", 409)]
    [InlineData("foreign", 403)] [InlineData("anonymous", 401)] [InlineData("other-actor", 409)] [InlineData("public-profile", 409)] [InlineData("registration", 409)]
    [InlineData("expired", 410)] [InlineData("stamp", 409)] [InlineData("role", 403)] [InlineData("source-cipher", 409)] [InlineData("revocation-cipher", 409)]
    [InlineData("row-status", 409)] [InlineData("source-version", 409)] [InlineData("binding-generation", 409)] [InlineData("wrong-row", 409)] [InlineData("replay", 409)]
    public async Task Current_scoped_human_ticket_is_required_without_any_provider_verification(string mode, int status)
    {
        await using var f = await MeetingReadinessTests.Fixture.Create(); var clock = new Clock(); var reg = Reg(); var (store, id, _) = await Retire(f, clock, reg);
        var review = (await store.PrepareManualRevocationReviewAsync(f.Actor, f.Academy.Id, reg, id)).Review!; Assert.NotNull(review);
        var actor = f.Actor;
        if (mode == "anonymous") actor = new ClaimsPrincipal();
        if (mode == "other-actor")
        {
            var other = new AcademyDesk.Api.Domain.Identity.ApplicationUser { Id = Guid.NewGuid(), AcademyId = f.Academy.Id, UserName = "synthetic-recovery-other", DisplayName = "Synthetic recovery actor" };
            Assert.True((await f.Users.CreateAsync(other)).Succeeded); Assert.True((await f.Users.AddToRoleAsync(other, "Owner")).Succeeded);
            actor = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, other.Id.ToString()), new Claim(ClaimTypes.Role, "Owner") }, "synthetic"));
        }
        if (mode == "expired") clock.Now = clock.Now.AddMinutes(5);
        if (mode == "stamp") Assert.True((await f.Users.UpdateSecurityStampAsync(f.User)).Succeeded);
        if (mode == "role") Assert.True((await f.Users.RemoveFromRoleAsync(f.User, "Owner")).Succeeded);
        if (mode == "source-cipher") (await f.Db.MeetingOAuthExchanges.SingleAsync()).ProtectedEnvelope = "corrupt";
        if (mode == "revocation-cipher") (await f.Db.MeetingCredentialRevocations.SingleAsync()).ProtectedEnvelope = "corrupt";
        if (mode == "row-status") (await f.Db.MeetingCredentialRevocations.SingleAsync()).Status = "ProviderAcknowledged";
        if (mode == "source-version") (await f.Db.MeetingOAuthExchanges.SingleAsync()).Version += 2;
        if (mode == "binding-generation") { var binding = await f.Db.MeetingProviderBindings.SingleAsync(); binding.Version++; binding.Generation++; }
        await f.Db.SaveChangesAsync();
        if (mode == "replay") Assert.Equal(200, (await store.ReportManualRevocationAsync(actor, f.Academy.Id, reg, id, 0, review.ReviewHash, review.Ticket, true)).StatusCode);
        var before = await f.Db.AuditLogs.CountAsync(x => x.Action == "MeetingRevocationManuallyReportedUnverified");
        var outcome = await store.ReportManualRevocationAsync(actor, mode == "foreign" ? Guid.NewGuid() : f.Academy.Id, mode == "public-profile" ? Reg(false) : mode == "registration" ? Reg(revision: 2) : reg,
            mode == "wrong-row" ? Guid.NewGuid() : id, mode == "version" ? 1 : 0, mode == "hash" ? new string('A', 64) : review.ReviewHash, mode == "ticket" ? "forged" : review.Ticket, mode != "declaration");
        Assert.Equal(status, outcome.StatusCode); Assert.False(outcome.ProviderVerified); Assert.False(outcome.Connected); Assert.False(outcome.CanRetry);
        Assert.Equal(before, await f.Db.AuditLogs.CountAsync(x => x.Action == "MeetingRevocationManuallyReportedUnverified"));
        Assert.DoesNotContain(review.Ticket, JsonSerializer.Serialize(outcome));
    }

    [Theory] [InlineData("public-profile")] [InlineData("foreign")] [InlineData("anonymous")] [InlineData("role")] [InlineData("stamp")]
    public async Task Unauthorized_review_does_not_release_a_ticket_or_add_a_review_audit(string mode)
    {
        await using var f = await MeetingReadinessTests.Fixture.Create(); var clock = new Clock(); var reg = Reg(); var (store, id, _) = await Retire(f, clock, reg);
        if (mode == "role") Assert.True((await f.Users.RemoveFromRoleAsync(f.User, "Owner")).Succeeded);
        if (mode == "stamp") Assert.True((await f.Users.UpdateSecurityStampAsync(f.User)).Succeeded);
        var outcome = await store.PrepareManualRevocationReviewAsync(mode == "anonymous" ? new ClaimsPrincipal() : f.Actor, mode == "foreign" ? Guid.NewGuid() : f.Academy.Id, mode == "public-profile" ? Reg(false) : reg, id);
        Assert.NotEqual(200, outcome.StatusCode); Assert.Null(outcome.Review); Assert.False(outcome.ProviderVerified);
        Assert.Empty(await f.Db.AuditLogs.Where(x => x.Action == "MeetingManualRevocationReviewPrepared").ToArrayAsync());
    }
}
