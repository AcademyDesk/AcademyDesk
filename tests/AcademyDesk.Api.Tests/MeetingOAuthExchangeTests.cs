using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using AcademyDesk.Api.Infrastructure.Meetings;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Tests;

public sealed class MeetingOAuthExchangeTests
{
    private const string Api = "https://api.academy.invalid", Workspace = "https://app.academy.invalid", Tenant = "019acc9a-6200-7000-8000-000000000001";
    private const string Code = "synthetic-code", Secret = "synthetic-secret", Access = "synthetic-access-token", Refresh = "synthetic-refresh-token";
    private sealed class FixedClock(DateTimeOffset now) : TimeProvider { public override DateTimeOffset GetUtcNow() => now; }
    private sealed class NonSeekableStream(byte[] bytes) : MemoryStream(bytes) { public override bool CanSeek => false; }
    private static string Profile(string provider) => provider switch { "GoogleWorkspace" => "google-calendar-owned-web/1", "Microsoft365" => "microsoft-online-meetings-work/1", _ => "zoom-public-pkce/1" };
    private static MeetingOAuthRegistrationInput Input(string provider) => new(provider, 1, "synthetic-client", Api + MeetingOAuthRegistrationRegistry.CallbackPath(provider), Workspace + "/communication-settings", Profile(provider), provider == "Microsoft365" ? Tenant : null);
    private static MeetingOAuthRegistration Registration(string provider) => MeetingOAuthRegistrationRegistry.Create(Api, Workspace, [Input(provider)]).Resolve(provider)!;
    private static byte[] Body(MeetingOAuthFlowProfile profile) => JsonSerializer.SerializeToUtf8Bytes(new { access_token = Access, refresh_token = Refresh, token_type = "Bearer", expires_in = 3600, scope = string.Join(' ', profile.RequiredTokenScopes) });
    private sealed class FakeHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        public int Calls;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellation) { Calls++; return send(request, cancellation); }
    }
    private static HttpResponseMessage Response(byte[] body) { var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(body) }; response.Content.Headers.ContentType = new("application/json"); return response; }
    private static async Task<MeetingOAuthOutcome> Claim(MeetingReadinessTests.Fixture f, MeetingOAuthRegistration registration)
    {
        var store = new MeetingOAuthAttemptStore(f.Db, f.Users, f.Readiness, new EphemeralDataProtectionProvider(), TimeProvider.System);
        var browser = WebEncoders.Base64UrlEncode(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
        var prepared = await store.PrepareAsync(f.Actor, f.Academy.Id, registration.Provider, 0, browser, registration.Fingerprint);
        Assert.Equal(201, prepared.StatusCode);
        Assert.Equal(202, (await store.CaptureAsync(f.Academy.Id, registration.Provider, prepared.Material!.State, browser, registration.Fingerprint, Code, false)).StatusCode);
        return await store.ResumeAsync(f.Actor, f.Academy.Id, registration.Provider, prepared.Material.State, browser, registration.Fingerprint);
    }
    [Theory] [InlineData("GoogleWorkspace")] [InlineData("Microsoft365")] [InlineData("Zoom")]
    public async Task Real_host_claim_builds_fixed_secret_safe_request_once_and_tokens_are_not_account_verification(string provider)
    {
        await using var f = await MeetingReadinessTests.Fixture.Create(); var registration = Registration(provider); var claim = await Claim(f, registration);
        var handler = new FakeHandler(async (request, token) =>
        {
            Assert.Equal(HttpMethod.Post, request.Method); Assert.Equal(registration.Flow!.TokenEndpoint, request.RequestUri); Assert.Empty(request.RequestUri!.Query);
            Assert.Null(request.Headers.Authorization); Assert.True(request.Headers.CacheControl!.NoStore); Assert.DoesNotContain(Code, request.ToString());
            var fields = QueryHelpers.ParseQuery(await request.Content!.ReadAsStringAsync(token));
            Assert.Equal("authorization_code", fields["grant_type"]); Assert.Equal(Code, fields["code"]); Assert.Equal(claim.Material!.Verifier, fields["code_verifier"]);
            Assert.Equal(registration.Callback.AbsoluteUri, fields["redirect_uri"]); Assert.Equal(registration.ClientId, fields["client_id"]);
            Assert.Equal(provider != "Zoom", fields.ContainsKey("client_secret")); if (provider != "Zoom") Assert.Equal(Secret, fields["client_secret"]);
            Assert.Equal(provider == "Microsoft365", fields.ContainsKey("scope"));
            return Response(Body(registration.Flow));
        });
        using var transport = new HttpMessageInvoker(handler); var service = new MeetingOAuthCodeExchange(f.Db, f.Users, f.Readiness, transport, TimeProvider.System);
        var result = await service.ExchangeAsync(f.Actor, f.Academy.Id, registration, claim, provider == "Zoom" ? null : Secret);
        Assert.Equal(200, result.StatusCode); Assert.Equal("TokensReceivedAccountUnverified", result.Code); Assert.False(result.CanRetry); Assert.False(result.OutcomeUnknown);
        Assert.Equal(Access, result.Tokens!.AccessToken); Assert.Equal(Refresh, result.Tokens.RefreshToken);
        foreach (var value in new[] { Access, Refresh, Secret, Code }) { Assert.DoesNotContain(value, result.ToString()); Assert.DoesNotContain(value, JsonSerializer.Serialize(result)); Assert.DoesNotContain(value, JsonSerializer.Serialize(result.Tokens)); }
        Assert.Equal(409, (await service.ExchangeAsync(f.Actor, f.Academy.Id, registration, claim, provider == "Zoom" ? null : Secret)).StatusCode); Assert.Equal(1, handler.Calls);
        Assert.Equal("VerificationRequired", (await f.Db.MeetingProviderBindings.SingleAsync()).Status); Assert.Equal(4, await f.Db.AuditLogs.CountAsync());
        Assert.Empty(await f.Db.MeetingOperations.ToListAsync()); Assert.Empty(await f.Db.Notifications.ToListAsync());
    }

    [Theory] [InlineData("anonymous")] [InlineData("foreign")] [InlineData("role")] [InlineData("stamp")]
    [InlineData("expiry")] [InlineData("profile")] [InlineData("secret")] [InlineData("provider-secret")]
    [InlineData("generation")] [InlineData("claim-status")] [InlineData("claim-code")] [InlineData("claim-material")]
    public async Task Invalid_or_revoked_context_never_sends(string mode)
    {
        await using var f = await MeetingReadinessTests.Fixture.Create(); var registration = Registration(mode == "secret" ? "GoogleWorkspace" : "Zoom"); var claim = await Claim(f, registration); var actor = f.Actor;
        if (mode == "anonymous") actor = new(new ClaimsIdentity());
        if (mode == "role") Assert.True((await f.Users.RemoveFromRoleAsync(f.User, "Owner")).Succeeded);
        if (mode == "stamp") Assert.True((await f.Users.UpdateSecurityStampAsync(f.User)).Succeeded);
        if (mode == "profile") registration = MeetingOAuthRegistrationRegistry.Create(Api, Workspace, [Input("Zoom") with { FlowProfileId = null }]).Resolve("Zoom")!;
        if (mode == "generation") { var binding = await f.Db.MeetingProviderBindings.SingleAsync(); binding.Generation++; binding.Version++; await f.Db.SaveChangesAsync(); }
        if (mode == "claim-status") claim = claim with { StatusCode = 202 };
        if (mode == "claim-code") claim = claim with { AuthorizationCode = "code\nheader" };
        if (mode == "claim-material") claim = claim with { Material = null };
        var handler = new FakeHandler((_, _) => throw new InvalidOperationException("Network must not be called")); using var transport = new HttpMessageInvoker(handler);
        var clock = mode == "expiry" ? new FixedClock(DateTimeOffset.UtcNow.AddMinutes(5)) : TimeProvider.System;
        var result = await new MeetingOAuthCodeExchange(f.Db, f.Users, f.Readiness, transport, clock).ExchangeAsync(actor, mode == "foreign" ? Guid.NewGuid() : f.Academy.Id, registration, claim, mode == "provider-secret" ? Secret : null);
        Assert.Contains(result.StatusCode, new[] { 401, 403, 409, 503 }); Assert.Null(result.Tokens); Assert.Equal(0, handler.Calls);
    }

    [Theory] [InlineData("redirect")] [InlineData("rate-limit")] [InlineData("provider-error")] [InlineData("content-type")]
    [InlineData("encoding")] [InlineData("large-body")] [InlineData("network")] [InlineData("cancel")]
    [InlineData("invalid-json")] [InlineData("revocation-during-exchange")] [InlineData("stamp-during-exchange")] [InlineData("generation-during-exchange")]
    [InlineData("chunked-large-body")]
    public async Task Failure_or_concurrent_revocation_is_not_retried_or_connected(string mode)
    {
        await using var f = await MeetingReadinessTests.Fixture.Create(); var registration = Registration("Zoom"); var claim = await Claim(f, registration);
        var handler = new FakeHandler(async (_, _) =>
        {
            if (mode == "network") throw new HttpRequestException("private-network-code-should-not-leak");
            if (mode == "cancel") throw new TaskCanceledException("private-cancel-should-not-leak");
            if (mode == "revocation-during-exchange") Assert.True((await f.Users.RemoveFromRoleAsync(f.User, "Owner")).Succeeded);
            if (mode == "stamp-during-exchange") Assert.True((await f.Users.UpdateSecurityStampAsync(f.User)).Succeeded);
            if (mode == "generation-during-exchange") { var row = await f.Db.MeetingProviderBindings.SingleAsync(); row.Version++; row.Generation++; await f.Db.SaveChangesAsync(); }
            var response = Response(mode == "invalid-json" ? Encoding.UTF8.GetBytes("secret-not-json") : mode == "large-body" ? new byte[65537] : Body(registration.Flow!));
            if (mode == "chunked-large-body") { response.Content = new StreamContent(new NonSeekableStream(new byte[65537])); response.Content.Headers.ContentType = new("application/json"); Assert.Null(response.Content.Headers.ContentLength); }
            if (mode == "redirect") { response.StatusCode = HttpStatusCode.Found; response.Headers.Location = new("https://attacker.invalid"); }
            if (mode == "rate-limit") response.StatusCode = HttpStatusCode.TooManyRequests;
            if (mode == "provider-error") response.StatusCode = HttpStatusCode.BadRequest;
            if (mode == "content-type") response.Content.Headers.ContentType = new("text/html");
            if (mode == "encoding") response.Content.Headers.ContentEncoding.Add("gzip");
            return response;
        });
        using var transport = new HttpMessageInvoker(handler); var service = new MeetingOAuthCodeExchange(f.Db, f.Users, f.Readiness, transport, TimeProvider.System);
        var result = await service.ExchangeAsync(f.Actor, f.Academy.Id, registration, claim, null);
        Assert.Contains(result.StatusCode, new[] { 403, 409, 502 }); Assert.True(result.OutcomeUnknown); Assert.False(result.CanRetry); Assert.Null(result.Tokens);
        Assert.DoesNotContain("private", result.ToString());
        Assert.NotEqual(200, (await service.ExchangeAsync(f.Actor, f.Academy.Id, registration, claim, null)).StatusCode); Assert.Equal(1, handler.Calls);
        Assert.Equal("VerificationRequired", (await f.Db.MeetingProviderBindings.SingleAsync()).Status);
    }

    [Theory] [InlineData("GoogleWorkspace")] [InlineData("Microsoft365")] [InlineData("Zoom")]
    public void Scoped_parser_accepts_only_profile_scopes_and_redacts_tokens(string provider)
    {
        var profile = Registration(provider).Flow!; var now = DateTimeOffset.UtcNow; var result = MeetingOAuthCodeExchange.Parse(Body(profile), profile, now);
        Assert.Equal(200, result.StatusCode); Assert.Equal(now.AddHours(1), result.Tokens!.ExpiresAt); Assert.Equal("{}", JsonSerializer.Serialize(result.Tokens));
        Assert.False(result.CanRetry); Assert.DoesNotContain(Access, result.ToString()); Assert.DoesNotContain(Refresh, result.Tokens.ToString());
    }

    [Theory] [InlineData("array")] [InlineData("null")] [InlineData("malformed")] [InlineData("duplicate")]
    [InlineData("duplicate-case")] [InlineData("error")] [InlineData("missing-refresh")] [InlineData("empty-access")]
    [InlineData("newline-token")] [InlineData("large-token")] [InlineData("expiry-string")] [InlineData("expiry-null")]
    [InlineData("expiry-negative")] [InlineData("expiry-overflow")] [InlineData("expiry-short")] [InlineData("expiry-long")]
    [InlineData("token-type")] [InlineData("missing-scope")] [InlineData("extra-scope")] [InlineData("partial-scope")]
    [InlineData("duplicate-scope")] [InlineData("malicious-api")] [InlineData("scope-type")] [InlineData("depth")] [InlineData("over-limit")]
    [InlineData("quote-token")] [InlineData("embedded-padding")] [InlineData("blank-token")]
    public void Malformed_or_overprivileged_token_body_never_yields_usable_tokens(string mode)
    {
        var profile = Registration("Zoom").Flow!; var json = Encoding.UTF8.GetString(Body(profile));
        if (mode == "array") json = "[]"; if (mode == "null") json = "null"; if (mode == "malformed") json = "{";
        if (mode == "duplicate") json = json.Replace("\"access_token\":", "\"access_token\":\"other\",\"access_token\":");
        if (mode == "duplicate-case") json = json.Replace("\"access_token\":", "\"ACCESS_TOKEN\":\"other\",\"access_token\":");
        if (mode == "error") json = json[..^1] + ",\"error\":\"private-error\"}";
        if (mode == "missing-refresh") json = json.Replace("refresh_token", "ignored");
        if (mode == "empty-access") json = json.Replace(Access, "");
        if (mode == "newline-token") json = json.Replace(Access, "bad\\ntoken");
        if (mode == "large-token") json = json.Replace(Access, new string('x', 16385));
        if (mode == "quote-token") json = json.Replace(Access, "bad\\\"token");
        if (mode == "embedded-padding") json = json.Replace(Access, "bad=token");
        if (mode == "blank-token") json = json.Replace(Access, "===");
        if (mode == "expiry-string") json = json.Replace("3600", "\"3600\""); if (mode == "expiry-null") json = json.Replace("3600", "null");
        if (mode == "expiry-negative") json = json.Replace("3600", "-1"); if (mode == "expiry-overflow") json = json.Replace("3600", "99999999999999999999");
        if (mode == "expiry-short") json = json.Replace("3600", "59"); if (mode == "expiry-long") json = json.Replace("3600", "86401");
        if (mode == "token-type") json = json.Replace("Bearer", "Basic"); if (mode == "missing-scope") json = json.Replace("\"scope\"", "\"ignored-scope\"");
        if (mode == "extra-scope") json = json.Replace("user:read:user", "user:read:user user:write:user:admin");
        if (mode == "partial-scope") json = json.Replace("meeting:write:meeting", "");
        if (mode == "duplicate-scope") json = json.Replace("user:read:user", "user:read:user user:read:user");
        if (mode == "malicious-api") json = json[..^1] + ",\"api_url\":\"https://attacker.invalid\"}";
        if (mode == "scope-type") json = json.Replace("\"scope\":", "\"scope\":{},\"ignored\":");
        if (mode == "depth") json = "{\"ignored\":" + new string('[', 10) + "0" + new string(']', 10) + "}";
        if (mode == "over-limit") json = new string(' ', 65537);
        var result = MeetingOAuthCodeExchange.Parse(Encoding.UTF8.GetBytes(json), profile, DateTimeOffset.UtcNow);
        Assert.Equal(502, result.StatusCode); Assert.True(result.OutcomeUnknown); Assert.False(result.CanRetry); Assert.Null(result.Tokens);
    }

    [Theory] [InlineData("common")] [InlineData("organizations")] [InlineData("consumers")] [InlineData("url")]
    [InlineData("empty-guid")] [InlineData("wrong-profile")] [InlineData("google-tenant")] [InlineData("zoom-tenant")]
    public void Unreviewed_profiles_or_tenant_aliases_disable_registration(string mode)
    {
        var input = Input("Microsoft365");
        if (mode is "common" or "organizations" or "consumers") input = input with { DirectoryTenant = mode };
        if (mode == "url") input = input with { DirectoryTenant = "https://attacker.invalid" };
        if (mode == "empty-guid") input = input with { DirectoryTenant = Guid.Empty.ToString("D") };
        if (mode == "wrong-profile") input = input with { FlowProfileId = "zoom-public-pkce/1" };
        if (mode == "google-tenant") input = Input("GoogleWorkspace") with { DirectoryTenant = Tenant };
        if (mode == "zoom-tenant") input = Input("Zoom") with { DirectoryTenant = Tenant };
        Assert.False(MeetingOAuthRegistrationRegistry.Create(Api, Workspace, [input]).IsValid);
    }

    [Fact] public void Flow_and_tenant_are_fingerprint_bound_and_transport_disables_unsafe_defaults()
    {
        var input = Input("Microsoft365"); var profile = Registration("Microsoft365");
        var changed = MeetingOAuthRegistrationRegistry.Create(Api, Workspace, [input with { DirectoryTenant = "019acc9a-6200-7000-8000-000000000002" }]).Resolve("Microsoft365")!;
        var old = MeetingOAuthRegistrationRegistry.Create(Api, Workspace, [input with { FlowProfileId = null, DirectoryTenant = null }]).Resolve("Microsoft365")!;
        Assert.NotEqual(profile.Fingerprint, changed.Fingerprint); Assert.NotEqual(profile.Fingerprint, old.Fingerprint); Assert.Null(old.Flow);
        using var handler = MeetingOAuthCodeExchange.CreateSecureHandler(); Assert.False(handler.AllowAutoRedirect); Assert.False(handler.UseProxy); Assert.False(handler.UseCookies);
        Assert.Equal(DecompressionMethods.None, handler.AutomaticDecompression); Assert.Null(handler.SslOptions.RemoteCertificateValidationCallback);
        Assert.Equal(TimeSpan.FromSeconds(5), handler.ConnectTimeout); Assert.Equal("{}", JsonSerializer.Serialize(input)); Assert.Equal("{}", JsonSerializer.Serialize(profile));
        var legacyInput = input with { FlowProfileId = null, DirectoryTenant = null };
        var legacyFields = new object[] { "meeting-registration/0.1-unqualified", legacyInput.Provider, legacyInput.Revision, legacyInput.ClientId, legacyInput.CallbackUri, legacyInput.WorkspaceReturnUri };
        Assert.Equal(Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(legacyFields)))), old.Fingerprint);
    }

    [Fact] public async Task Two_calls_sharing_one_claim_make_only_one_outbound_attempt()
    {
        await using var f = await MeetingReadinessTests.Fixture.Create(); var registration = Registration("Zoom"); var claim = await Claim(f, registration);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); var finish = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var handler = new FakeHandler(async (_, token) => { entered.SetResult(); await finish.Task.WaitAsync(token); return Response(Body(registration.Flow!)); });
        using var transport = new HttpMessageInvoker(handler); var service = new MeetingOAuthCodeExchange(f.Db, f.Users, f.Readiness, transport, TimeProvider.System);
        var first = service.ExchangeAsync(f.Actor, f.Academy.Id, registration, claim, null); await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var second = await service.ExchangeAsync(f.Actor, f.Academy.Id, registration, claim, null); Assert.Equal(409, second.StatusCode); Assert.Equal("ExchangeAlreadyAttempted", second.Code);
        finish.SetResult(); Assert.Equal(200, (await first).StatusCode); Assert.Equal(1, handler.Calls);
    }

    [Theory] [InlineData("GoogleWorkspace")] [InlineData("Microsoft365")] [InlineData("Zoom")]
    public async Task Profile_changes_invalidate_protected_browser_registration_without_consuming_intent(string provider)
    {
        await using var f = await MeetingReadinessTests.Fixture.Create(); var keys = new EphemeralDataProtectionProvider();
        var oldInput = Input(provider) with { FlowProfileId = null, DirectoryTenant = null };
        var oldRegistry = MeetingOAuthRegistrationRegistry.Create(Api, Workspace, [oldInput]); var registry = MeetingOAuthRegistrationRegistry.Create(Api, Workspace, [Input(provider)]);
        var store = new MeetingOAuthAttemptStore(f.Db, f.Users, f.Readiness, keys, TimeProvider.System);
        var begin = new DefaultHttpContext { User = f.Actor }; begin.Request.Scheme = "https"; begin.Request.Host = new("api.academy.invalid"); begin.Request.Method = "POST"; begin.Request.Headers.Origin = Workspace;
        var prepared = await new MeetingOAuthBrowserBridge(store, oldRegistry, keys, TimeProvider.System).BeginAsync(begin, f.Academy.Id, provider, 0);
        Assert.Equal(201, prepared.StatusCode);
        var callback = new DefaultHttpContext { User = f.Actor }; callback.Request.Scheme = "https"; callback.Request.Host = new("api.academy.invalid"); callback.Request.Method = "GET";
        callback.Request.Path = MeetingOAuthRegistrationRegistry.CallbackPath(provider); callback.Request.QueryString = new("?state=" + prepared.Material!.State + "&code=" + Code);
        callback.Request.Headers.Cookie = begin.Response.Headers.SetCookie.ToString().Split(';')[0];
        Assert.Equal(409, (await new MeetingOAuthBrowserBridge(store, registry, keys, TimeProvider.System).AcceptAuthenticatedCallbackAsync(callback, f.Academy.Id, provider)).StatusCode);
        Assert.Equal("Pending", (await f.Db.MeetingOAuthAttempts.SingleAsync()).Status); Assert.Single(await f.Db.AuditLogs.ToListAsync());
    }
}
