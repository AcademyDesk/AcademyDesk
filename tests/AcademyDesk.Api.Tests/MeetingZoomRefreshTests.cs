using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AcademyDesk.Api.Infrastructure.Meetings;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Tests;

public sealed class MeetingZoomRefreshTests
{
    private const string Secret = "synthetic-backend-secret", OldRefresh = "synthetic-zoom-refresh-old", NewRefresh = "synthetic-zoom-refresh-new", Access = "synthetic-zoom-access-new";
    private sealed class Clock : TimeProvider { public DateTimeOffset Now = DateTimeOffset.UtcNow; public override DateTimeOffset GetUtcNow() => Now; }
    private static MeetingOAuthRegistration Reg(bool backend = true, int revision = 1) => MeetingOAuthRegistrationRegistry.Create("https://api.academy.invalid", "https://app.academy.invalid", [new("Zoom", revision, "synthetic-backend-client",
        "https://api.academy.invalid/api/meetings/oauth/Zoom/callback", "https://app.academy.invalid/communication-settings", backend ? MeetingOAuthFlowProfile.ZoomConfidentialProfile : "zoom-public-pkce/1")]).Resolve("Zoom")!;
    private static string Json(MeetingOAuthRegistration reg, string refresh = NewRefresh) => JsonSerializer.Serialize(new { access_token = Access, refresh_token = refresh, token_type = "Bearer", expires_in = 3600, scope = string.Join(' ', reg.Flow!.RequiredTokenScopes), api_url = "https://api.zoom.us" });
    private sealed class Handler(Func<HttpRequestMessage, Task<HttpResponseMessage>> send) : HttpMessageHandler
    { internal int Calls; protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token) { Calls++; return send(request); } }
    private static HttpResponseMessage Response(string json) => new(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
    private static async Task<MeetingOAuthOutcome> Claim(MeetingReadinessTests.Fixture f, Clock clock, MeetingOAuthAttemptStore store, MeetingOAuthRegistration reg)
    {
        var browser = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32)); var start = await store.PrepareAsync(f.Actor, f.Academy.Id, reg.Provider, 0, browser, reg.Fingerprint);
        Assert.Equal(201, start.StatusCode); Assert.Equal(202, (await store.CaptureAsync(f.Academy.Id, reg.Provider, start.Material!.State, browser, reg.Fingerprint, "synthetic-zoom-code", false)).StatusCode);
        return await store.ResumeAsync(f.Actor, f.Academy.Id, reg.Provider, start.Material.State, browser, reg.Fingerprint);
    }
    private static async Task<(MeetingOAuthAttemptStore Store, MeetingOAuthExchangePermit Permit)> Source(MeetingReadinessTests.Fixture f, Clock clock, MeetingOAuthRegistration reg)
    {
        var store = new MeetingOAuthAttemptStore(f.Db, f.Users, f.Readiness, new EphemeralDataProtectionProvider(), clock); var claim = await Claim(f, clock, store, reg);
        var permit = (await store.BeginExchangeAsync(f.Actor, f.Academy.Id, reg, claim)).Permit!;
        Assert.Equal(200, (await store.FinishExchangeAsync(f.Actor, f.Academy.Id, reg, permit, new(200, "TokensReceivedAccountUnverified", Tokens: new("synthetic-zoom-old-access", OldRefresh, clock.Now.AddHours(1), reg.Flow!.RequiredTokenScopes)))).StatusCode);
        return (store, permit);
    }
    [Fact] public void Separate_backend_profile_preserves_public_configuration_and_scopes()
    {
        var backend = Reg(); var original = Reg(false); Assert.NotEqual(backend.Fingerprint, original.Fingerprint);
        Assert.True(backend.Flow!.RequiresClientSecret); Assert.False(original.Flow!.RequiresClientSecret);
        Assert.Equal(original.Flow.RequestedScopes, backend.Flow.RequestedScopes); Assert.Equal(original.Flow.RequiredTokenScopes, backend.Flow.RequiredTokenScopes);
        Assert.True(MeetingOAuthAttemptStore.SupportsRefresh(backend)); Assert.False(MeetingOAuthAttemptStore.SupportsRefresh(original));
        Assert.Equal(original.Flow.TokenEndpoint, backend.Flow.TokenEndpoint); Assert.Equal(original.Flow.AccountEndpoint, backend.Flow.AccountEndpoint);
    }
    [Theory] [InlineData("ok")] [InlineData("missing-secret")] [InlineData("wrong-profile")] [InlineData("malformed")]
    public async Task Backend_code_exchange_uses_basic_header_and_PKCE_only_after_durable_start(string mode)
    {
        await using var f = await MeetingReadinessTests.Fixture.Create(); var clock = new Clock(); var reg = Reg(); var store = new MeetingOAuthAttemptStore(f.Db, f.Users, f.Readiness, new EphemeralDataProtectionProvider(), clock);
        var claim = await Claim(f, clock, store, reg);
        using var handler = new Handler(async request => {
            Assert.Equal("https://zoom.us/oauth/token", request.RequestUri!.AbsoluteUri); Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal("Basic", request.Headers.Authorization!.Scheme); Assert.Equal(reg.ClientId + ":" + Secret, Encoding.UTF8.GetString(Convert.FromBase64String(request.Headers.Authorization.Parameter!)));
            var form = await request.Content!.ReadAsStringAsync(); Assert.Contains("grant_type=authorization_code", form); Assert.Contains("code_verifier=" + claim.Material!.Verifier, form);
            Assert.DoesNotContain("client_secret", form); Assert.DoesNotContain(Secret, form); Assert.DoesNotContain(Secret, request.RequestUri.AbsoluteUri);
            Assert.Equal("Started", (await f.Db.MeetingOAuthExchanges.SingleAsync()).Status); Assert.Equal(1, await f.Db.AuditLogs.CountAsync(x => x.Action == "MeetingOAuthExchangeStarted"));
            return Response(mode == "malformed" ? "{}" : Json(reg)); });
        using var transport = new HttpMessageInvoker(handler); var result = await new MeetingOAuthExchangeCoordinator(store, new(f.Db, f.Users, f.Readiness, transport, clock)).ExecuteAsync(f.Actor, f.Academy.Id, mode == "wrong-profile" ? Reg(false) : reg, claim, mode == "missing-secret" ? null : Secret);
        Assert.Equal(mode == "ok" ? 200 : 409, result.StatusCode); Assert.Equal(mode is "ok" or "malformed" ? 1 : 0, handler.Calls);
        Assert.DoesNotContain(Secret, JsonSerializer.Serialize(result)); Assert.Equal("VerificationRequired", (await f.Db.MeetingProviderBindings.SingleAsync()).Status);
    }
    [Theory] [InlineData("ok")] [InlineData("scope-omitted")] [InlineData("same-refresh")] [InlineData("missing-refresh")] [InlineData("null-refresh")]
    [InlineData("scope-null")] [InlineData("scope-empty")] [InlineData("scope-expanded")] [InlineData("scope-reduced")] [InlineData("duplicate")]
    [InlineData("error")] [InlineData("unknown-api")] [InlineData("expires-short")] [InlineData("expires-long")] [InlineData("malformed")]
    [InlineData("public-profile")] [InlineData("oversize")]
    public void Rotation_is_mandatory_and_scope_inheritance_is_source_only(string mode)
    {
        var reg = Reg(mode != "public-profile"); var json = mode switch {
            "same-refresh" => Json(reg, OldRefresh), "missing-refresh" => Json(reg).Replace("\"refresh_token\":\"" + NewRefresh + "\",", ""), "null-refresh" => Json(reg).Replace("\"" + NewRefresh + "\"", "null"),
            "scope-omitted" => Json(reg).Replace("\"scope\":\"" + string.Join(' ', reg.Flow!.RequiredTokenScopes) + "\",", ""),
            "scope-null" => Json(reg).Replace("\"" + string.Join(' ', reg.Flow!.RequiredTokenScopes) + "\"", "null"),
            "scope-empty" => Json(reg).Replace(string.Join(' ', reg.Flow!.RequiredTokenScopes), ""), "scope-expanded" => Json(reg).Replace("user:read:user", "user:read:user user:read:user:admin"),
            "scope-reduced" => Json(reg).Replace("meeting:write:meeting", ""), "duplicate" => Json(reg)[..^1] + ",\"ACCESS_TOKEN\":\"other\"}",
            "error" => Json(reg)[..^1] + ",\"error\":null}", "unknown-api" => Json(reg).Replace("https://api.zoom.us", "https://attacker.invalid"),
            "expires-short" => Json(reg).Replace("3600", "59"), "expires-long" => Json(reg).Replace("3600", "86401"), "malformed" => "[]", "oversize" => new string('x', 65537), _ => Json(reg) };
        var material = new MeetingRefreshMaterial(Guid.NewGuid(), Guid.NewGuid(), 2, OldRefresh, reg.Flow!.RequiredTokenScopes);
        var result = MeetingProviderRefresh.Parse(Encoding.UTF8.GetBytes(json), reg, material, DateTimeOffset.UtcNow); var accepted = mode is "ok" or "scope-omitted";
        Assert.Equal(accepted ? 200 : 502, result.StatusCode); Assert.Equal(!accepted, result.OutcomeUnknown); Assert.Equal(accepted, result.Tokens is not null);
        Assert.DoesNotContain(NewRefresh, JsonSerializer.Serialize(result));
    }
    [Theory] [InlineData("ok")] [InlineData("missing-secret")] [InlineData("public-profile")] [InlineData("foreign")] [InlineData("anonymous")]
    [InlineData("role-before")] [InlineData("stamp-before")] [InlineData("wrong-version")] [InlineData("wrong-fence")]
    [InlineData("role-during")] [InlineData("stamp-during")] [InlineData("disconnect-during")] [InlineData("deadline")]
    [InlineData("network")] [InlineData("429")] [InlineData("redirect")] [InlineData("mime")] [InlineData("encoding")]
    [InlineData("oversize")] [InlineData("same-refresh")]
    public async Task Refresh_is_current_authorized_single_send_and_quarantines_uncertainty(string mode)
    {
        await using var f = await MeetingReadinessTests.Fixture.Create(); var clock = new Clock(); var reg = Reg(mode != "public-profile"); var (store, permit) = await Source(f, clock, reg);
        var review = (await store.PrepareAccountReviewAsync(f.Actor, f.Academy.Id, reg, permit, new("synthetic-user", "synthetic-account", clock.Now))).Review!;
        Assert.Equal(200, (await store.ConfirmAccountReviewAsync(f.Actor, f.Academy.Id, reg, review.Id, 0, review.ReviewHash, true)).StatusCode);
        if (mode == "role-before") Assert.True((await f.Users.RemoveFromRoleAsync(f.User, "Owner")).Succeeded);
        if (mode == "stamp-before") Assert.True((await f.Users.UpdateSecurityStampAsync(f.User)).Succeeded);
        using var handler = new Handler(async request => {
            Assert.Equal(HttpMethod.Post, request.Method); Assert.Equal("https://zoom.us/oauth/token", request.RequestUri!.AbsoluteUri); Assert.Equal("Basic", request.Headers.Authorization!.Scheme);
            Assert.Equal(reg.ClientId + ":" + Secret, Encoding.UTF8.GetString(Convert.FromBase64String(request.Headers.Authorization.Parameter!)));
            var form = await request.Content!.ReadAsStringAsync(); Assert.Contains("grant_type=refresh_token", form); Assert.Contains("refresh_token=" + OldRefresh, form);
            Assert.DoesNotContain("client_secret", form); Assert.DoesNotContain("scope=", form); Assert.DoesNotContain("code_verifier", form); Assert.DoesNotContain(Secret, form);
            var row = await f.Db.MeetingOAuthExchanges.SingleAsync(); Assert.Equal("RefreshStarted", row.Status); Assert.Equal(2, row.Version); Assert.True(request.Headers.CacheControl!.NoStore);
            if (mode == "role-during") await f.Users.RemoveFromRoleAsync(f.User, "Owner"); if (mode == "stamp-during") await f.Users.UpdateSecurityStampAsync(f.User);
            if (mode == "disconnect-during") Assert.Equal(200, (await store.DisconnectLocallyAsync(f.Actor, f.Academy.Id, reg.Provider, 3, true)).StatusCode);
            if (mode == "deadline") clock.Now = clock.Now.AddSeconds(30); if (mode == "network") throw new HttpRequestException(OldRefresh);
            var response = Response(mode == "oversize" ? new string('x', 65537) : Json(reg, mode == "same-refresh" ? OldRefresh : NewRefresh));
            response.StatusCode = mode == "429" ? HttpStatusCode.TooManyRequests : mode == "redirect" ? HttpStatusCode.Redirect : HttpStatusCode.OK;
            if (mode == "mime") response.Content.Headers.ContentType!.MediaType = "text/html"; if (mode == "encoding") response.Content.Headers.ContentEncoding.Add("gzip"); return response;
        });
        using var transport = new HttpMessageInvoker(handler); var service = new MeetingProviderRefresh(store, transport, clock);
        var result = await service.ExecuteAsync(mode == "anonymous" ? new ClaimsPrincipal() : f.Actor, mode == "foreign" ? Guid.NewGuid() : f.Academy.Id, reg, mode == "wrong-fence" ? permit with { Fence = Guid.NewGuid() } : permit, mode == "wrong-version" ? 3 : 1, mode == "missing-secret" ? null : Secret);
        var noCall = mode is "missing-secret" or "public-profile" or "foreign" or "anonymous" or "role-before" or "stamp-before" or "wrong-version" or "wrong-fence";
        Assert.Equal(noCall ? 0 : 1, handler.Calls); Assert.False(result.Connected); Assert.False(result.CanRetry); Assert.DoesNotContain(OldRefresh, JsonSerializer.Serialize(result)); Assert.DoesNotContain(NewRefresh, result.ToString());
        if (mode == "ok")
        {
            Assert.Equal(200, result.StatusCode); Assert.Equal(3, (await f.Db.MeetingOAuthExchanges.SingleAsync()).Version);
            Assert.Equal(NewRefresh, (await store.ReadQuarantinedTokensAsync(f.Actor, f.Academy.Id, reg, result.Permit!)).Tokens!.RefreshToken);
            Assert.Equal(409, (await store.ReadAccountReviewAsync(f.Actor, f.Academy.Id, reg, review.Id)).StatusCode);
            Assert.Equal(409, (await store.BeginRefreshAsync(f.Actor, f.Academy.Id, reg, permit, 1)).StatusCode);
            var pending = (await store.PrepareAccountReviewAsync(f.Actor, f.Academy.Id, reg, result.Permit!, new("synthetic-user", "synthetic-account", clock.Now))).Review!;
            var policy = MeetingAccountPolicyRegistry.Create([new(f.Academy.Id, reg.Provider, 1, reg.Fingerprint, "synthetic-user", "synthetic-account", null, clock.Now.AddMinutes(-1), clock.Now.AddHours(2))]);
            using var probeTransport = new HttpMessageInvoker(new Handler(_ => throw new InvalidOperationException("Unconfirmed review must not call provider")));
            Assert.Equal(409, (await store.CheckZoomAccountAsync(f.Actor, f.Academy.Id, reg, pending.Id, policy, new(probeTransport))).StatusCode);
        }
        else Assert.NotEqual(200, result.StatusCode);
        if (!noCall) { var again = await service.ExecuteAsync(f.Actor, f.Academy.Id, reg, permit, 1, Secret); Assert.NotEqual(200, again.StatusCode); Assert.Equal(1, handler.Calls); }
    }
    [Fact] public async Task Direct_completion_cannot_bypass_rotation_and_unknown_family_can_be_locally_retired()
    {
        await using var f = await MeetingReadinessTests.Fixture.Create(); var clock = new Clock(); var reg = Reg(); var (store, permit) = await Source(f, clock, reg);
        var started = await store.BeginRefreshAsync(f.Actor, f.Academy.Id, reg, permit, 1); var outcome = new MeetingOAuthExchangeOutcome(200, "TokensReceivedAccountUnverified", Tokens: new(Access, OldRefresh, clock.Now.AddHours(1), reg.Flow!.RequiredTokenScopes));
        Assert.Equal(409, (await store.FinishRefreshAsync(f.Actor, f.Academy.Id, reg, started.Material!, outcome)).StatusCode);
        Assert.Equal("RefreshUnknown", (await f.Db.MeetingOAuthExchanges.SingleAsync()).Status);
        Assert.Equal(202, (await store.DisconnectUnknownRefreshAsync(f.Actor, f.Academy.Id, reg, new(permit.Id, started.Material!.Fence), 3, 3, true)).StatusCode);
        Assert.Equal("ManualRequired", (await f.Db.MeetingCredentialRevocations.SingleAsync()).Status); Assert.Equal("RefreshAbandoned", (await f.Db.MeetingOAuthExchanges.SingleAsync()).Status);
    }
}
