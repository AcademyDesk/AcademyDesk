using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using AcademyDesk.Api.Infrastructure.Meetings;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Tests;

public sealed class MeetingRefreshTests
{
    private const string OldRefresh = "synthetic-old-refresh", NewRefresh = "synthetic-next-refresh", NewAccess = "synthetic-next-access";
    private sealed class Clock : TimeProvider { public DateTimeOffset Now = DateTimeOffset.UtcNow; public override DateTimeOffset GetUtcNow() => Now; }
    private static MeetingOAuthRegistration Registration(string provider = "GoogleWorkspace", int revision = 1)
    {
        var profile = provider switch { "GoogleWorkspace" => "google-calendar-owned-web/1", "Microsoft365" => "microsoft-online-meetings-work/1", _ => "zoom-public-pkce/1" };
        return MeetingOAuthRegistrationRegistry.Create("https://api.academy.invalid", "https://app.academy.invalid", [new(provider, revision, "synthetic-client", "https://api.academy.invalid" + MeetingOAuthRegistrationRegistry.CallbackPath(provider),
            "https://app.academy.invalid/communication-settings", profile, provider == "Microsoft365" ? "019acc9a-6200-7000-8000-000000000001" : null)]).Resolve(provider)!;
    }
    private static async Task<(MeetingOAuthAttemptStore Store, MeetingOAuthExchangePermit Permit)> Setup(MeetingReadinessTests.Fixture f, Clock clock, MeetingOAuthRegistration registration)
    {
        var store = new MeetingOAuthAttemptStore(f.Db, f.Users, f.Readiness, new EphemeralDataProtectionProvider(), clock);
        var browser = WebEncoders.Base64UrlEncode(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
        var prepared = await store.PrepareAsync(f.Actor, f.Academy.Id, registration.Provider, 0, browser, registration.Fingerprint); Assert.Equal(201, prepared.StatusCode);
        Assert.Equal(202, (await store.CaptureAsync(f.Academy.Id, registration.Provider, prepared.Material!.State, browser, registration.Fingerprint, "synthetic-refresh-code", false)).StatusCode);
        var claim = await store.ResumeAsync(f.Actor, f.Academy.Id, registration.Provider, prepared.Material.State, browser, registration.Fingerprint);
        var start = await store.BeginExchangeAsync(f.Actor, f.Academy.Id, registration, claim); Assert.Equal(201, start.StatusCode);
        Assert.Equal(200, (await store.FinishExchangeAsync(f.Actor, f.Academy.Id, registration, start.Permit!, new(200, "TokensReceivedAccountUnverified",
            Tokens: new("synthetic-old-access", OldRefresh, clock.Now.AddHours(1), registration.Flow!.RequiredTokenScopes)))).StatusCode);
        return (store, start.Permit!);
    }
    private sealed class Handler(Func<HttpRequestMessage, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        public int Calls;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token) { Calls++; return send(request); }
    }
    private static HttpResponseMessage Response(string json) => new(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
    private static MeetingOAuthExchangeOutcome Tokens(Clock clock, MeetingOAuthRegistration reg) => new(200, "TokensReceivedAccountUnverified", Tokens: new(NewAccess, NewRefresh, clock.Now.AddHours(1), reg.Flow!.RequiredTokenScopes));

    [Theory] [InlineData("GoogleWorkspace")] [InlineData("Microsoft365")]
    public async Task Refresh_is_durable_before_fake_HTTP_rotates_handles_and_requires_new_account_review(string provider)
    {
        await using var f = await MeetingReadinessTests.Fixture.Create(); var clock = new Clock(); var reg = Registration(provider); var (store, oldPermit) = await Setup(f, clock, reg);
        var account = new MeetingOAuthAccountObservation(provider == "Microsoft365" ? "019acc9a-6200-7000-8000-000000000002" : "synthetic-subject", null, clock.Now);
        var review = (await store.PrepareAccountReviewAsync(f.Actor, f.Academy.Id, reg, oldPermit, account)).Review!;
        Assert.Equal(200, (await store.ConfirmAccountReviewAsync(f.Actor, f.Academy.Id, reg, review.Id, 0, review.ReviewHash, true)).StatusCode);
        var handler = new Handler(async request =>
        {
            Assert.Equal(HttpMethod.Post, request.Method); Assert.Equal(reg.Flow!.TokenEndpoint, request.RequestUri); Assert.Equal("", request.RequestUri!.Query); Assert.Null(request.Headers.Authorization);
            var form = await request.Content!.ReadAsStringAsync(); Assert.Contains("grant_type=refresh_token", form); Assert.Contains("refresh_token=" + OldRefresh, form); Assert.Contains("client_secret=synthetic-secret", form);
            Assert.DoesNotContain("authorization_code", form); Assert.True(request.Headers.CacheControl!.NoStore);
            var row = await f.Db.MeetingOAuthExchanges.SingleAsync(); Assert.Equal("RefreshStarted", row.Status); Assert.Equal(2, row.Version); Assert.Equal(3, row.BindingVersion);
            Assert.Equal(1, await f.Db.AuditLogs.CountAsync(x => x.Action == "MeetingRefreshStarted")); Assert.Equal(409, (await store.ReadQuarantinedTokensAsync(f.Actor, f.Academy.Id, reg, oldPermit)).StatusCode);
            return Response(provider == "GoogleWorkspace" ? "{\"access_token\":\"" + NewAccess + "\",\"token_type\":\"Bearer\",\"expires_in\":3600}"
                : JsonSerializer.Serialize(new { access_token = NewAccess, refresh_token = NewRefresh, token_type = "Bearer", expires_in = 3600, scope = string.Join(' ', reg.Flow.RequiredTokenScopes) }));
        });
        using var transport = new HttpMessageInvoker(handler); var result = await new MeetingProviderRefresh(store, transport, clock).ExecuteAsync(f.Actor, f.Academy.Id, reg, oldPermit, 1, "synthetic-secret");
        Assert.Equal(200, result.StatusCode); Assert.NotNull(result.Permit); Assert.False(result.Connected); Assert.False(result.CanRetry); Assert.Equal(1, handler.Calls);
        Assert.DoesNotContain(NewAccess, JsonSerializer.Serialize(result)); Assert.DoesNotContain(NewRefresh, result.ToString());
        var saved = await f.Db.MeetingOAuthExchanges.SingleAsync(); Assert.Equal("TokensQuarantined", saved.Status); Assert.Equal(3, saved.Version); Assert.DoesNotContain(NewAccess, saved.ProtectedEnvelope);
        Assert.NotEqual(oldPermit.Fence, result.Permit!.Fence); var read = await store.ReadQuarantinedTokensAsync(f.Actor, f.Academy.Id, reg, result.Permit);
        Assert.Equal(200, read.StatusCode); Assert.Equal(provider == "GoogleWorkspace" ? OldRefresh : NewRefresh, read.Tokens!.RefreshToken);
        Assert.Equal(409, (await store.ReadAccountReviewAsync(f.Actor, f.Academy.Id, reg, review.Id)).StatusCode);
        var newReview = await store.PrepareAccountReviewAsync(f.Actor, f.Academy.Id, reg, result.Permit, account); Assert.Equal(201, newReview.StatusCode); Assert.Equal("AwaitingConfirmation", newReview.Review!.Status);
        Assert.Equal(2, await f.Db.MeetingAccountReviews.CountAsync()); Assert.Equal("VerificationRequired", (await f.Db.MeetingProviderBindings.SingleAsync()).Status);
        var second = await store.BeginRefreshAsync(f.Actor, f.Academy.Id, reg, result.Permit, 3); Assert.Equal(201, second.StatusCode);
        Assert.Equal(200, (await store.FinishRefreshAsync(f.Actor, f.Academy.Id, reg, second.Material!, Tokens(clock, reg) with { Tokens = new(NewAccess, "synthetic-third-refresh", clock.Now.AddHours(1), reg.Flow!.RequiredTokenScopes) })).StatusCode);
        Assert.Equal(5, saved.Version); Assert.Equal(409, (await store.BeginRefreshAsync(f.Actor, f.Academy.Id, reg, oldPermit, 1)).StatusCode);
    }

    [Theory] [InlineData("anonymous")] [InlineData("foreign")] [InlineData("role")] [InlineData("stamp")] [InlineData("registration")]
    [InlineData("generation")] [InlineData("fence")] [InlineData("version")] [InlineData("overflow")] [InlineData("tamper")] [InlineData("disconnected")]
    public async Task Invalid_refresh_context_does_not_change_credentials_or_binding(string mode)
    {
        await using var f = await MeetingReadinessTests.Fixture.Create(); var clock = new Clock(); var reg = Registration(); var (store, permit) = await Setup(f, clock, reg);
        if (mode == "role") await f.Users.RemoveFromRoleAsync(f.User, "Owner"); if (mode == "stamp") await f.Users.UpdateSecurityStampAsync(f.User);
        if (mode == "registration") reg = Registration(revision: 2); if (mode == "fence") permit = permit with { Fence = Guid.NewGuid() };
        if (mode == "generation") { var b = await f.Db.MeetingProviderBindings.SingleAsync(); b.Version++; b.Generation++; await f.Db.SaveChangesAsync(); }
        if (mode == "tamper") { (await f.Db.MeetingOAuthExchanges.SingleAsync()).ProtectedEnvelope = "forged"; await f.Db.SaveChangesAsync(); }
        if (mode == "disconnected") await store.DisconnectLocallyAsync(f.Actor, f.Academy.Id, reg.Provider, 2, true);
        var before = await f.Db.AuditLogs.CountAsync(); var result = await store.BeginRefreshAsync(mode == "anonymous" ? new ClaimsPrincipal() : f.Actor, mode == "foreign" ? Guid.NewGuid() : f.Academy.Id,
            reg, permit, mode == "version" ? 0 : mode == "overflow" ? int.MaxValue : 1);
        Assert.Contains(result.StatusCode, new[] { 400, 401, 403, 409 }); Assert.Null(result.Material); Assert.Equal("TokensQuarantined", (await f.Db.MeetingOAuthExchanges.SingleAsync()).Status); Assert.Equal(before, await f.Db.AuditLogs.CountAsync());
    }

    [Theory] [InlineData("Zoom", "synthetic-secret")] [InlineData("GoogleWorkspace", null)] [InlineData("Microsoft365", "bad\nsecret")]
    public async Task Unsupported_profile_or_missing_server_credential_makes_no_request_or_write(string provider, string? secret)
    {
        await using var f = await MeetingReadinessTests.Fixture.Create(); var clock = new Clock(); var reg = Registration(provider); var (store, permit) = await Setup(f, clock, reg);
        var handler = new Handler(_ => Task.FromResult(Response("{}"))); using var transport = new HttpMessageInvoker(handler);
        Assert.Equal(503, (await new MeetingProviderRefresh(store, transport, clock).ExecuteAsync(f.Actor, f.Academy.Id, reg, permit, 1, secret)).StatusCode);
        Assert.Equal(0, handler.Calls); Assert.Equal(6, await f.Db.AuditLogs.CountAsync()); Assert.Equal(1, (await f.Db.MeetingOAuthExchanges.SingleAsync()).Version);
    }

    [Theory] [InlineData("network")] [InlineData("cancel")] [InlineData("redirect")] [InlineData("429")] [InlineData("malformed")] [InlineData("oversize")] [InlineData("encoding")]
    public async Task Uncertain_refresh_never_retries_or_releases_previous_credentials(string mode)
    {
        await using var f = await MeetingReadinessTests.Fixture.Create(); var clock = new Clock(); var reg = Registration(); var (store, permit) = await Setup(f, clock, reg);
        var start = await store.BeginRefreshAsync(f.Actor, f.Academy.Id, reg, permit, 1); Assert.Equal("{}", JsonSerializer.Serialize(start.Material));
        var handler = new Handler(_ =>
        {
            if (mode == "network") throw new HttpRequestException(OldRefresh); if (mode == "cancel") throw new OperationCanceledException(OldRefresh);
            var response = Response(mode == "oversize" ? new string('x', 65537) : "not-json");
            response.StatusCode = mode switch { "redirect" => HttpStatusCode.Redirect, "429" => HttpStatusCode.TooManyRequests, _ => HttpStatusCode.OK };
            if (mode == "encoding") response.Content.Headers.ContentEncoding.Add("gzip"); return Task.FromResult(response);
        });
        using var transport = new HttpMessageInvoker(handler); var service = new MeetingProviderRefresh(store, transport, clock);
        Assert.Equal(409, (await service.SendStartedAsync(f.Actor, f.Academy.Id, reg, start.Material!, "synthetic-secret")).StatusCode);
        Assert.Equal("RefreshUnknown", (await f.Db.MeetingOAuthExchanges.SingleAsync()).Status);
        Assert.Equal(409, (await service.SendStartedAsync(f.Actor, f.Academy.Id, reg, start.Material!, "synthetic-secret")).StatusCode); Assert.Equal(1, handler.Calls);
        Assert.Equal(409, (await store.ReadQuarantinedTokensAsync(f.Actor, f.Academy.Id, reg, new(permit.Id, start.Material!.Fence))).StatusCode);
    }

    [Theory] [InlineData("role")] [InlineData("stamp")] [InlineData("disconnect")] [InlineData("deadline")]
    public async Task Authority_change_during_refresh_prevents_credential_commit(string mode)
    {
        await using var f = await MeetingReadinessTests.Fixture.Create(); var clock = new Clock(); var reg = Registration(); var (store, permit) = await Setup(f, clock, reg);
        var handler = new Handler(async _ =>
        {
            if (mode == "role") await f.Users.RemoveFromRoleAsync(f.User, "Owner"); if (mode == "stamp") await f.Users.UpdateSecurityStampAsync(f.User);
            if (mode == "disconnect") Assert.Equal(200, (await store.DisconnectLocallyAsync(f.Actor, f.Academy.Id, reg.Provider, 3, true)).StatusCode);
            if (mode == "deadline") clock.Now = clock.Now.AddSeconds(30);
            return Response("{\"access_token\":\"" + NewAccess + "\",\"token_type\":\"Bearer\",\"expires_in\":3600}");
        });
        using var transport = new HttpMessageInvoker(handler); var result = await new MeetingProviderRefresh(store, transport, clock).ExecuteAsync(f.Actor, f.Academy.Id, reg, permit, 1, "synthetic-secret");
        Assert.Contains(result.StatusCode, new[] { 403, 409 }); Assert.Null(result.Permit); Assert.Equal("RefreshStarted", (await f.Db.MeetingOAuthExchanges.SingleAsync()).Status);
        Assert.DoesNotContain(NewAccess, (await f.Db.MeetingOAuthExchanges.SingleAsync()).ProtectedEnvelope);
    }

    [Fact] public async Task Expired_access_can_refresh_but_interrupted_attempt_recovers_unknown_never_restarts()
    {
        await using var f = await MeetingReadinessTests.Fixture.Create(); var clock = new Clock(); var reg = Registration(); var (store, permit) = await Setup(f, clock, reg); clock.Now = clock.Now.AddHours(1);
        var start = await store.BeginRefreshAsync(f.Actor, f.Academy.Id, reg, permit, 1); Assert.Equal(201, start.StatusCode);
        Assert.False(await store.RecoverRefreshAsync(f.Academy.Id, permit.Id)); clock.Now = clock.Now.AddSeconds(30);
        Assert.False(await store.RecoverRefreshAsync(Guid.NewGuid(), permit.Id)); Assert.True(await store.RecoverRefreshAsync(f.Academy.Id, permit.Id)); Assert.False(await store.RecoverRefreshAsync(f.Academy.Id, permit.Id));
        Assert.Equal("RefreshUnknown", (await f.Db.MeetingOAuthExchanges.SingleAsync()).Status);
        Assert.Equal(409, (await store.BeginRefreshAsync(f.Actor, f.Academy.Id, reg, new(permit.Id, start.Material!.Fence), 3)).StatusCode);
        Assert.Equal(409, (await store.FinishRefreshAsync(f.Actor, f.Academy.Id, reg, start.Material, Tokens(clock, reg))).StatusCode);
    }

    [Theory]
    [InlineData("GoogleWorkspace", "{\"access_token\":\"next\",\"token_type\":\"Bearer\",\"expires_in\":3600}", true)]
    [InlineData("Microsoft365", "{\"access_token\":\"next\",\"token_type\":\"Bearer\",\"expires_in\":3600}", false)]
    [InlineData("Microsoft365", "{\"access_token\":\"next\",\"refresh_token\":\"rotated\",\"token_type\":\"Bearer\",\"expires_in\":3600}", true)]
    [InlineData("Microsoft365", "{\"access_token\":\"next\",\"refresh_token\":\"synthetic-old-refresh\",\"token_type\":\"Bearer\",\"expires_in\":3600}", false)]
    [InlineData("GoogleWorkspace", "{\"access_token\":\"next\",\"scope\":null,\"token_type\":\"Bearer\",\"expires_in\":3600}", false)]
    [InlineData("GoogleWorkspace", "{\"access_token\":\"next\",\"refresh_token\":null,\"token_type\":\"Bearer\",\"expires_in\":3600}", false)]
    [InlineData("GoogleWorkspace", "{\"access_token\":\"next\",\"scope\":\"email https://www.googleapis.com/auth/calendar.events.owned\",\"token_type\":\"Bearer\",\"expires_in\":3600}", false)]
    [InlineData("GoogleWorkspace", "{\"access_token\":\"next\",\"ACCESS_TOKEN\":\"other\",\"token_type\":\"Bearer\",\"expires_in\":3600}", false)]
    [InlineData("GoogleWorkspace", "{\"access_token\":\"next\",\"token_type\":\"Bearer\",\"expires_in\":3600,\"error\":null}", false)]
    [InlineData("GoogleWorkspace", "{\"access_token\":\"next\",\"token_type\":\"Bearer\",\"expires_in\":1}", false)]
    [InlineData("GoogleWorkspace", "[]", false)] [InlineData("GoogleWorkspace", "null", false)]
    public void Parser_carries_only_stored_grant_and_rejects_scope_expansion_or_missing_rotation(string provider, string json, bool accepted)
    {
        var reg = Registration(provider); var source = new MeetingRefreshMaterial(Guid.NewGuid(), Guid.NewGuid(), 2, OldRefresh, reg.Flow!.RequiredTokenScopes);
        var result = MeetingProviderRefresh.Parse(Encoding.UTF8.GetBytes(json), reg, source, DateTimeOffset.UtcNow);
        Assert.Equal(accepted ? 200 : 502, result.StatusCode); Assert.Equal(accepted, result.Tokens is not null);
    }
}
