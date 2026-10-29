using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using AcademyDesk.Api.Infrastructure.Meetings;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Tests;

public sealed class MeetingAccountReviewTests
{
    private sealed class Clock : TimeProvider { public DateTimeOffset Now = DateTimeOffset.UtcNow; public override DateTimeOffset GetUtcNow() => Now; }
    private static MeetingOAuthRegistration Registration(string provider = "Zoom", int revision = 1)
    {
        var profile = provider switch { "GoogleWorkspace" => "google-calendar-owned-web/1", "Microsoft365" => "microsoft-online-meetings-work/1", _ => "zoom-public-pkce/1" };
        return MeetingOAuthRegistrationRegistry.Create("https://api.academy.invalid", "https://app.academy.invalid", [new(provider, revision, "synthetic-client",
            "https://api.academy.invalid" + MeetingOAuthRegistrationRegistry.CallbackPath(provider), "https://app.academy.invalid/communication-settings", profile,
            provider == "Microsoft365" ? "019acc9a-6200-7000-8000-000000000001" : null)]).Resolve(provider)!;
    }
    private sealed class Handler(string provider) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            object body = provider switch { "GoogleWorkspace" => new { sub = "synthetic-google-user", email_verified = true },
                "Microsoft365" => new { id = "019acc9a-6200-7000-8000-000000000002" }, _ => new { id = "synthetic-zoom-user", account_id = "synthetic-zoom-account", status = "active" } };
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json") });
        }
    }
    private static async Task<(MeetingOAuthAttemptStore Store, MeetingOAuthExchangePermit Permit, MeetingAccountReviewState Review)> Setup(MeetingReadinessTests.Fixture f, Clock clock, MeetingOAuthRegistration registration)
    {
        var store = new MeetingOAuthAttemptStore(f.Db, f.Users, f.Readiness, new EphemeralDataProtectionProvider(), clock);
        var browser = WebEncoders.Base64UrlEncode(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
        var prepared = await store.PrepareAsync(f.Actor, f.Academy.Id, registration.Provider, 0, browser, registration.Fingerprint); Assert.Equal(201, prepared.StatusCode);
        Assert.Equal(202, (await store.CaptureAsync(f.Academy.Id, registration.Provider, prepared.Material!.State, browser, registration.Fingerprint, "synthetic-review-code", false)).StatusCode);
        var claim = await store.ResumeAsync(f.Actor, f.Academy.Id, registration.Provider, prepared.Material.State, browser, registration.Fingerprint);
        var start = await store.BeginExchangeAsync(f.Actor, f.Academy.Id, registration, claim); Assert.Equal(201, start.StatusCode);
        Assert.Equal(200, (await store.FinishExchangeAsync(f.Actor, f.Academy.Id, registration, start.Permit!, new(200, "TokensReceivedAccountUnverified",
            Tokens: new("synthetic-review-access", "synthetic-review-refresh", clock.Now.AddHours(1), registration.Flow!.RequiredTokenScopes)))).StatusCode);
        using var transport = new HttpMessageInvoker(new Handler(registration.Provider));
        var review = await new MeetingOAuthAccountProbe(store, transport, clock).PrepareReviewAsync(f.Actor, f.Academy.Id, registration, start.Permit!);
        Assert.Equal(201, review.StatusCode); return (store, start.Permit!, review.Review!);
    }
    [Theory] [InlineData("GoogleWorkspace")] [InlineData("Microsoft365")] [InlineData("Zoom")]
    public async Task All_providers_have_encrypted_current_actor_review_and_explicit_confirmation_never_connected(string provider)
    {
        await using var f = await MeetingReadinessTests.Fixture.Create(); var clock = new Clock(); var registration = Registration(provider); var (store, permit, review) = await Setup(f, clock, registration);
        Assert.Equal("AwaitingConfirmation", review.Status); Assert.Equal(0, review.Version); Assert.Equal(DateTimeKind.Utc, review.ExpiresAtUtc.Kind);
        var row = await f.Db.MeetingAccountReviews.SingleAsync(); Assert.DoesNotContain("synthetic-", row.ProtectedEnvelope); Assert.DoesNotContain("synthetic-", JsonSerializer.Serialize(review));
        Assert.DoesNotContain("synthetic-", review.ToString()); Assert.False(review.Connected); Assert.False(review.CanExecute);
        Assert.Equal(200, (await store.ReadAccountReviewAsync(f.Actor, f.Academy.Id, registration, review.Id)).StatusCode);
        Assert.Equal(400, (await store.ConfirmAccountReviewAsync(f.Actor, f.Academy.Id, registration, review.Id, 0, review.ReviewHash, false)).StatusCode);
        var confirmed = await store.ConfirmAccountReviewAsync(f.Actor, f.Academy.Id, registration, review.Id, 0, review.ReviewHash, true);
        Assert.Equal(200, confirmed.StatusCode); Assert.Equal("ConfirmedNotConnected", confirmed.Review!.Status); Assert.Equal(1, confirmed.Review.Version); Assert.NotNull(row.ConfirmedAtUtc);
        Assert.Equal(409, (await store.ConfirmAccountReviewAsync(f.Actor, f.Academy.Id, registration, review.Id, 0, review.ReviewHash, true)).StatusCode);
        Assert.Equal("ConfirmedNotConnected", (await store.ReadAccountReviewAsync(f.Actor, f.Academy.Id, registration, review.Id)).Review!.Status);
        Assert.Equal(4, await f.Db.AuditLogs.CountAsync(x => x.EntityId == row.Id));
        Assert.Equal("VerificationRequired", (await f.Db.MeetingProviderBindings.SingleAsync()).Status); Assert.Equal("TokensQuarantined", (await f.Db.MeetingOAuthExchanges.SingleAsync()).Status);
        Assert.Equal(409, (await store.PrepareAccountReviewAsync(f.Actor, f.Academy.Id, registration, permit, review.Account)).StatusCode);
    }

    [Theory] [InlineData("anonymous")] [InlineData("foreign")] [InlineData("other-actor")] [InlineData("role")] [InlineData("stamp")]
    [InlineData("registration")] [InlineData("generation")] [InlineData("disconnected")] [InlineData("expiry")] [InlineData("ciphertext")]
    [InlineData("hash")] [InlineData("version")] [InlineData("exchange")] [InlineData("source-ciphertext")]
    public async Task Changed_context_or_evidence_cannot_read_or_confirm(string mode)
    {
        await using var f = await MeetingReadinessTests.Fixture.Create(); var clock = new Clock(); var reg = Registration(); var (store, _, review) = await Setup(f, clock, reg); var actor = f.Actor;
        if (mode == "anonymous") actor = new ClaimsPrincipal();
        if (mode == "foreign") { /* Different academy supplied below. */ }
        if (mode == "other-actor") actor = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()) }, "synthetic"));
        if (mode == "role") await f.Users.RemoveFromRoleAsync(f.User, "Owner");
        if (mode == "stamp") await f.Users.UpdateSecurityStampAsync(f.User);
        if (mode == "registration") reg = Registration(revision: 2);
        if (mode == "generation") { var b = await f.Db.MeetingProviderBindings.SingleAsync(); b.Generation++; b.Version++; await f.Db.SaveChangesAsync(); }
        if (mode == "disconnected") Assert.Equal(200, (await store.DisconnectLocallyAsync(f.Actor, f.Academy.Id, "Zoom", 2, true)).StatusCode);
        if (mode == "expiry") clock.Now = clock.Now.AddMinutes(5);
        var row = await f.Db.MeetingAccountReviews.SingleAsync();
        if (mode == "ciphertext") row.ProtectedEnvelope = "forged";
        if (mode == "hash") row.ReviewHash = new string('A', 64);
        if (mode == "version") row.Version++;
        if (mode == "exchange") row.ExchangeId = Guid.NewGuid();
        if (mode == "source-ciphertext") (await f.Db.MeetingOAuthExchanges.SingleAsync()).ProtectedEnvelope = "forged";
        await f.Db.SaveChangesAsync(); var before = await f.Db.AuditLogs.CountAsync(); var academy = mode == "foreign" ? Guid.NewGuid() : f.Academy.Id;
        var read = await store.ReadAccountReviewAsync(actor, academy, reg, review.Id);
        var confirm = await store.ConfirmAccountReviewAsync(actor, academy, reg, review.Id, 0, review.ReviewHash, true);
        Assert.Contains(read.StatusCode, new[] { 401, 403, 404, 409, 410 }); Assert.Contains(confirm.StatusCode, new[] { 401, 403, 404, 409, 410 });
        Assert.Null(read.Review); Assert.Null(confirm.Review); Assert.Equal(before, await f.Db.AuditLogs.CountAsync()); Assert.Null(row.ConfirmedAtUtc);
    }

    [Theory] [InlineData("unconfirmed")] [InlineData("version")] [InlineData("malformed-hash")] [InlineData("wrong-hash")]
    public async Task Confirmation_requires_exact_review_and_explicit_decision(string mode)
    {
        await using var f = await MeetingReadinessTests.Fixture.Create(); var clock = new Clock(); var reg = Registration(); var (store, _, review) = await Setup(f, clock, reg);
        var result = await store.ConfirmAccountReviewAsync(f.Actor, f.Academy.Id, reg, review.Id, mode == "version" ? 1 : 0,
            mode == "malformed-hash" ? "bad" : mode == "wrong-hash" ? new string('A', 64) : review.ReviewHash, mode != "unconfirmed");
        Assert.Equal(mode == "wrong-hash" ? 409 : 400, result.StatusCode); Assert.Null(result.Review); Assert.Equal(1, await f.Db.AuditLogs.CountAsync(x => x.EntityId == review.Id));
    }
    [Theory] [InlineData("future")] [InlineData("old")] [InlineData("invalid-subject")]
    public async Task Trusted_observation_handoff_still_rejects_stale_or_invalid_identity(string mode)
    {
        await using var f = await MeetingReadinessTests.Fixture.Create(); var clock = new Clock(); var reg = Registration(); var (store, permit, review) = await Setup(f, clock, reg);
        var observed = new MeetingOAuthAccountObservation(mode == "invalid-subject" ? "invalid\nsubject" : review.Account.Subject, review.Account.Account,
            mode == "future" ? clock.Now.AddSeconds(1) : clock.Now.AddSeconds(-31));
        Assert.Equal(409, (await store.PrepareAccountReviewAsync(f.Actor, f.Academy.Id, reg, permit, observed)).StatusCode); Assert.Equal(1, await f.Db.MeetingAccountReviews.CountAsync());
    }
}
