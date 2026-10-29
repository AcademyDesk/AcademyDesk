using System.Text.Json;
using AcademyDesk.Api.Infrastructure.Meetings;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Tests;

public sealed class MeetingAccountContinuityTests
{
    private sealed class Clock : TimeProvider { public DateTimeOffset Now = DateTimeOffset.UtcNow; public override DateTimeOffset GetUtcNow() => Now; }
    private static MeetingOAuthRegistration Registration(string provider) => MeetingOAuthRegistrationRegistry.Create("https://api.academy.invalid", "https://app.academy.invalid",
        [new(provider, 1, "synthetic-client", "https://api.academy.invalid" + MeetingOAuthRegistrationRegistry.CallbackPath(provider), "https://app.academy.invalid/communication-settings",
            provider == "GoogleWorkspace" ? "google-calendar-owned-web/1" : "microsoft-online-meetings-work/1", provider == "Microsoft365" ? "019acc9a-6200-7000-8000-000000000001" : null)]).Resolve(provider)!;
    private static async Task<(MeetingOAuthAttemptStore Store, MeetingOAuthExchangePermit Permit)> Setup(MeetingReadinessTests.Fixture f, Clock clock, MeetingOAuthRegistration reg, IDataProtectionProvider keys)
    {
        var store = new MeetingOAuthAttemptStore(f.Db, f.Users, f.Readiness, keys, clock); var browser = WebEncoders.Base64UrlEncode(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
        var intent = await store.PrepareAsync(f.Actor, f.Academy.Id, reg.Provider, 0, browser, reg.Fingerprint); Assert.Equal(201, intent.StatusCode);
        Assert.Equal(202, (await store.CaptureAsync(f.Academy.Id, reg.Provider, intent.Material!.State, browser, reg.Fingerprint, "synthetic-continuity-code", false)).StatusCode);
        var claim = await store.ResumeAsync(f.Actor, f.Academy.Id, reg.Provider, intent.Material.State, browser, reg.Fingerprint); var exchange = await store.BeginExchangeAsync(f.Actor, f.Academy.Id, reg, claim);
        Assert.Equal(200, (await store.FinishExchangeAsync(f.Actor, f.Academy.Id, reg, exchange.Permit!, Tokens(clock, reg, "initial"))).StatusCode); return (store, exchange.Permit!);
    }
    private static MeetingOAuthExchangeOutcome Tokens(Clock clock, MeetingOAuthRegistration reg, string suffix) => new(200, "TokensReceivedAccountUnverified", Tokens: new("synthetic-continuity-access-" + suffix, "synthetic-continuity-refresh-" + suffix, clock.Now.AddHours(1), reg.Flow!.RequiredTokenScopes));
    private static async Task<MeetingOAuthExchangePermit> Rotate(MeetingReadinessTests.Fixture f, Clock clock, MeetingOAuthRegistration reg, MeetingOAuthAttemptStore store, MeetingOAuthExchangePermit permit, int version = 1)
    {
        var start = await store.BeginRefreshAsync(f.Actor, f.Academy.Id, reg, permit, version); Assert.Equal(201, start.StatusCode);
        var result = await store.FinishRefreshAsync(f.Actor, f.Academy.Id, reg, start.Material!, Tokens(clock, reg, "rotated" + version)); Assert.Equal(200, result.StatusCode); return result.Permit!;
    }
    [Theory] [InlineData("GoogleWorkspace", false)] [InlineData("GoogleWorkspace", true)] [InlineData("Microsoft365", false)] [InlineData("Microsoft365", true)]
    public async Task Refreshed_same_account_needs_new_confirmation_but_changed_identity_is_denied(string provider, bool confirmed)
    {
        await using var f = await MeetingReadinessTests.Fixture.Create(); var clock = new Clock(); var reg = Registration(provider); var keys = new EphemeralDataProtectionProvider(); var (store, permit) = await Setup(f, clock, reg, keys);
        var observation = new MeetingOAuthAccountObservation("synthetic-subject", null, clock.Now);
        var first = (await store.PrepareAccountReviewAsync(f.Actor, f.Academy.Id, reg, permit, observation)).Review!;
        if (confirmed) Assert.Equal(200, (await store.ConfirmAccountReviewAsync(f.Actor, f.Academy.Id, reg, first.Id, 0, first.ReviewHash, true)).StatusCode);
        clock.Now = clock.Now.AddMinutes(6); permit = await Rotate(f, clock, reg, store, permit); var audits = await f.Db.AuditLogs.CountAsync();
        var changed = await store.PrepareAccountReviewAsync(f.Actor, f.Academy.Id, reg, permit, new("synthetic-other-subject", null, clock.Now));
        Assert.Equal(409, changed.StatusCode); Assert.Equal("ProviderAccountChangedReauthorizationRequired", changed.Code); Assert.Null(changed.Review); Assert.DoesNotContain("synthetic-subject", JsonSerializer.Serialize(changed)); Assert.DoesNotContain("synthetic-other-subject", changed.ToString());
        Assert.Equal(1, await f.Db.MeetingAccountReviews.CountAsync()); Assert.Equal(audits, await f.Db.AuditLogs.CountAsync());
        var same = await store.PrepareAccountReviewAsync(f.Actor, f.Academy.Id, reg, permit, new("synthetic-subject", null, clock.Now)); Assert.Equal(201, same.StatusCode); Assert.False(same.Review!.Connected); Assert.Equal("AwaitingConfirmation", same.Review.Status);
        Assert.Equal(200, (await store.ConfirmAccountReviewAsync(f.Actor, f.Academy.Id, reg, same.Review.Id, 0, same.Review.ReviewHash, true)).StatusCode);
        permit = await Rotate(f, clock, reg, store, permit, 3);
        Assert.Equal(409, (await store.PrepareAccountReviewAsync(f.Actor, f.Academy.Id, reg, permit, new("synthetic-other-subject", null, clock.Now))).StatusCode);
        Assert.Equal(201, (await store.PrepareAccountReviewAsync(f.Actor, f.Academy.Id, reg, permit, new("synthetic-subject", null, clock.Now))).StatusCode);
        Assert.Equal("VerificationRequired", (await f.Db.MeetingProviderBindings.SingleAsync()).Status);
    }
    [Theory] [InlineData(null, "account-added")] [InlineData("account-old", null)] [InlineData("account-old", "account-new")] [InlineData("Account", "account")]
    public async Task Account_component_is_compared_exactly_including_null(string? oldAccount, string? nextAccount)
    {
        await using var f = await MeetingReadinessTests.Fixture.Create(); var clock = new Clock(); var reg = Registration("GoogleWorkspace"); var (store, permit) = await Setup(f, clock, reg, new EphemeralDataProtectionProvider());
        Assert.Equal(201, (await store.PrepareAccountReviewAsync(f.Actor, f.Academy.Id, reg, permit, new("synthetic-subject", oldAccount, clock.Now))).StatusCode);
        permit = await Rotate(f, clock, reg, store, permit);
        Assert.Equal("ProviderAccountChangedReauthorizationRequired", (await store.PrepareAccountReviewAsync(f.Actor, f.Academy.Id, reg, permit, new("synthetic-subject", nextAccount, clock.Now))).Code);
        Assert.Equal(201, (await store.PrepareAccountReviewAsync(f.Actor, f.Academy.Id, reg, permit, new("synthetic-subject", oldAccount, clock.Now))).StatusCode);
    }
    [Theory] [InlineData("cipher")] [InlineData("hash")] [InlineData("status")] [InlineData("version")] [InlineData("binding-version")] [InlineData("registration")]
    public async Task Corrupt_prior_history_does_not_allow_a_new_baseline(string mode)
    {
        await using var f = await MeetingReadinessTests.Fixture.Create(); var clock = new Clock(); var reg = Registration("GoogleWorkspace"); var (store, permit) = await Setup(f, clock, reg, new EphemeralDataProtectionProvider());
        var first = (await store.PrepareAccountReviewAsync(f.Actor, f.Academy.Id, reg, permit, new("synthetic-subject", null, clock.Now))).Review!;
        permit = await Rotate(f, clock, reg, store, permit); var row = await f.Db.MeetingAccountReviews.SingleAsync(x => x.Id == first.Id);
        if (mode == "cipher") row.ProtectedEnvelope = "forged"; if (mode == "hash") row.ReviewHash = new string('A', 64); if (mode == "status") row.Status = "ConfirmedNotConnected";
        if (mode == "version") row.Version++; if (mode == "binding-version") row.BindingVersion++; if (mode == "registration") row.RegistrationFingerprint = new string('B', 64); await f.Db.SaveChangesAsync();
        var audits = await f.Db.AuditLogs.CountAsync(); var result = await store.PrepareAccountReviewAsync(f.Actor, f.Academy.Id, reg, permit, new("synthetic-subject", null, clock.Now));
        Assert.Equal(409, result.StatusCode); Assert.Equal("AccountContinuityHistoryInvalid", result.Code); Assert.Null(result.Review); Assert.Equal(audits, await f.Db.AuditLogs.CountAsync());
    }
    [Theory] [InlineData(false)] [InlineData(true)]
    public async Task History_is_revalidated_for_current_review_read_and_confirmation(bool confirm)
    {
        await using var f = await MeetingReadinessTests.Fixture.Create(); var clock = new Clock(); var reg = Registration("GoogleWorkspace"); var (store, permit) = await Setup(f, clock, reg, new EphemeralDataProtectionProvider());
        var first = (await store.PrepareAccountReviewAsync(f.Actor, f.Academy.Id, reg, permit, new("synthetic-subject", null, clock.Now))).Review!;
        permit = await Rotate(f, clock, reg, store, permit); var current = (await store.PrepareAccountReviewAsync(f.Actor, f.Academy.Id, reg, permit, new("synthetic-subject", null, clock.Now))).Review!;
        (await f.Db.MeetingAccountReviews.SingleAsync(x => x.Id == first.Id)).ProtectedEnvelope = "forged"; await f.Db.SaveChangesAsync(); var audits = await f.Db.AuditLogs.CountAsync();
        var result = confirm ? await store.ConfirmAccountReviewAsync(f.Actor, f.Academy.Id, reg, current.Id, 0, current.ReviewHash, true) : await store.ReadAccountReviewAsync(f.Actor, f.Academy.Id, reg, current.Id);
        Assert.Equal("AccountContinuityHistoryInvalid", result.Code); Assert.Equal(409, result.StatusCode); Assert.Null(result.Review); Assert.Equal(audits, await f.Db.AuditLogs.CountAsync()); Assert.Equal("AwaitingConfirmation", (await f.Db.MeetingAccountReviews.SingleAsync(x => x.Id == current.Id)).Status);
    }
    [Fact] public async Task No_prior_observation_allows_first_review_after_refresh_without_fabricating_a_match()
    {
        await using var f = await MeetingReadinessTests.Fixture.Create(); var clock = new Clock(); var reg = Registration("GoogleWorkspace"); var (store, permit) = await Setup(f, clock, reg, new EphemeralDataProtectionProvider()); permit = await Rotate(f, clock, reg, store, permit);
        var first = await store.PrepareAccountReviewAsync(f.Actor, f.Academy.Id, reg, permit, new("synthetic-first-observation", null, clock.Now)); Assert.Equal(201, first.StatusCode); Assert.False(first.Review!.Connected); Assert.Equal("AwaitingConfirmation", first.Review.Status);
    }
}
