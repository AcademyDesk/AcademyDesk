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

public sealed class MeetingZoomAccountTests
{
    private const string Subject = "synthetic-zoom-user", Account = "synthetic-zoom-account", Access = "synthetic-zoom-access";
    private sealed class Clock : TimeProvider { public DateTimeOffset Now = DateTimeOffset.UtcNow; public override DateTimeOffset GetUtcNow() => Now; }
    private static MeetingOAuthRegistration Registration(bool profile = true) => MeetingOAuthRegistrationRegistry.Create("https://api.academy.invalid", "https://app.academy.invalid", [new("Zoom", 1, "synthetic-zoom-client",
        "https://api.academy.invalid/api/meetings/oauth/Zoom/callback", "https://app.academy.invalid/communication-settings", profile ? "zoom-public-pkce/1" : null)]).Resolve("Zoom")!;
    private static string Body(int type = 2, string status = "active", string subject = Subject, string account = Account) => JsonSerializer.Serialize(new { id = subject, account_id = account, status, type });
    private sealed class Handler(string? body = null, string mode = "ok", Func<Task>? during = null) : HttpMessageHandler
    {
        internal int Calls;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            Calls++; Assert.Equal(HttpMethod.Get, request.Method); Assert.Equal(MeetingZoomAccountProbe.Endpoint, request.RequestUri!.AbsoluteUri);
            Assert.Equal("Bearer", request.Headers.Authorization!.Scheme); Assert.Equal(Access, request.Headers.Authorization.Parameter);
            Assert.Null(request.Content); Assert.DoesNotContain(Access, request.RequestUri.AbsoluteUri); Assert.True(request.Headers.CacheControl!.NoStore);
            if (during is not null) await during(); if (mode == "network") throw new HttpRequestException("synthetic-private-provider-error"); if (mode == "timeout") throw new TaskCanceledException();
            var status = mode switch { "unauthorized" => HttpStatusCode.Unauthorized, "forbidden" => HttpStatusCode.Forbidden, "redirect" => HttpStatusCode.Redirect, "throttled" => HttpStatusCode.TooManyRequests, "unavailable" => HttpStatusCode.ServiceUnavailable, _ => HttpStatusCode.OK };
            var result = new HttpResponseMessage(status) { Content = new StringContent(mode == "oversize" ? new string('x', 65537) : body ?? Body(), Encoding.UTF8, mode == "mime" ? "text/html" : "application/json") };
            if (mode == "encoding") result.Content.Headers.ContentEncoding.Add("gzip"); return result;
        }
    }
    [Theory] [InlineData("licensed")] [InlineData("basic")] [InlineData("no-meeting")] [InlineData("legacy-none")] [InlineData("inactive")] [InlineData("pending")]
    [InlineData("foreign-user")] [InlineData("foreign-account")] [InlineData("missing-type")] [InlineData("string-type")] [InlineData("float-type")] [InlineData("unknown-type")]
    [InlineData("unknown-status")] [InlineData("empty-id")] [InlineData("bad-id")] [InlineData("missing-account")] [InlineData("long-account")]
    [InlineData("null")] [InlineData("array")] [InlineData("malformed")] [InlineData("duplicate-root")] [InlineData("duplicate-nested")]
    [InlineData("provider-error")] [InlineData("provider-code")] [InlineData("provider-message")] [InlineData("pagination")] [InlineData("null-pagination")]
    [InlineData("next-link")] [InlineData("oversize")] [InlineData("deep")] [InlineData("ignored-role-is-not-authority")]
    public void Exact_account_and_known_plan_are_observations_not_permissions(string mode)
    {
        var json = mode switch {
            "basic" => Body(1), "no-meeting" => Body(4), "legacy-none" => Body(99), "inactive" => Body(status: "inactive"), "pending" => Body(status: "pending"),
            "foreign-user" => Body(subject: "other-user"), "foreign-account" => Body(account: "other-account"), "missing-type" => "{\"id\":\"" + Subject + "\",\"account_id\":\"" + Account + "\",\"status\":\"active\"}",
            "string-type" => Body().Replace("\"type\":2", "\"type\":\"2\""), "float-type" => Body().Replace("\"type\":2", "\"type\":2.0"), "unknown-type" => Body(3),
            "unknown-status" => Body(status: "enabled"), "empty-id" => Body(subject: ""), "bad-id" => Body(subject: "../me"), "missing-account" => "{}", "long-account" => Body(account: new string('a', 256)),
            "null" => "null", "array" => "[]", "malformed" => "{", "duplicate-root" => Body()[..^1] + ",\"ID\":\"other\"}",
            "duplicate-nested" => Body()[..^1] + ",\"extra\":{\"role\":1,\"Role\":2}}", "provider-error" => Body()[..^1] + ",\"error\":null}",
            "provider-code" => Body()[..^1] + ",\"code\":0}", "provider-message" => Body()[..^1] + ",\"message\":\"private\"}",
            "pagination" => Body()[..^1] + ",\"next_page_token\":\"private\"}", "null-pagination" => Body()[..^1] + ",\"NEXT_PAGE_TOKEN\":null}",
            "next-link" => Body()[..^1] + ",\"next\":\"https://attacker.invalid\"}", "oversize" => new string('x', 65537),
            "deep" => Body()[..^1] + ",\"extra\":" + new string('[', 9) + "0" + new string(']', 9) + "}",
            "ignored-role-is-not-authority" => Body()[..^1] + ",\"role_name\":\"Owner\",\"permissions\":[\"all\"],\"email\":\"private@example.invalid\"}", _ => Body() };
        var result = MeetingZoomAccountProbe.Parse(Encoding.UTF8.GetBytes(json), Subject, Account);
        var observed = mode is "licensed" or "basic" or "ignored-role-is-not-authority";
        var denied = mode is "no-meeting" or "legacy-none" or "inactive" or "pending" or "foreign-user" or "foreign-account";
        Assert.Equal(observed ? 200 : denied ? 409 : 502, result.StatusCode); Assert.Equal(observed, result.MeetingPlanObserved);
        Assert.False(result.ProviderVerified); Assert.False(result.Connected); Assert.False(result.CanExecute); Assert.False(result.CanRetry);
        Assert.DoesNotContain(Subject, JsonSerializer.Serialize(result)); Assert.DoesNotContain(Account, JsonSerializer.Serialize(result)); Assert.DoesNotContain("private", JsonSerializer.Serialize(result));
    }
    [Theory] [InlineData("ok")] [InlineData("unauthorized")] [InlineData("forbidden")] [InlineData("redirect")] [InlineData("throttled")] [InlineData("unavailable")]
    [InlineData("network")] [InlineData("timeout")] [InlineData("mime")] [InlineData("encoding")] [InlineData("oversize")]
    public async Task Fixed_read_never_retries_follows_redirects_or_sends_credentials_in_URL(string mode)
    {
        using var handler = new Handler(mode: mode); using var transport = new HttpMessageInvoker(handler);
        var result = await new MeetingZoomAccountProbe(transport).InspectAsync(Access, Subject, Account);
        Assert.Equal(mode == "ok" ? 200 : 502, result.StatusCode); Assert.Equal(1, handler.Calls); Assert.False(result.CanExecute); Assert.False(result.CanRetry);
        Assert.DoesNotContain(Access, JsonSerializer.Serialize(result)); Assert.DoesNotContain("private", JsonSerializer.Serialize(result));
    }
    [Theory] [InlineData("")] [InlineData("bad\r\nheader")] [InlineData("bad token")]
    public async Task Malformed_credential_never_reaches_transport(string access)
    {
        using var handler = new Handler(); using var transport = new HttpMessageInvoker(handler);
        Assert.Equal(409, (await new MeetingZoomAccountProbe(transport).InspectAsync(access, Subject, Account)).StatusCode); Assert.Equal(0, handler.Calls);
    }
    [Theory] [InlineData("ok")] [InlineData("basic")] [InlineData("no-profile")] [InlineData("policy")] [InlineData("unconfirmed")] [InlineData("forbidden")]
    [InlineData("foreign-user")] [InlineData("foreign-account")] [InlineData("no-meeting")] [InlineData("pagination")] [InlineData("role")] [InlineData("stamp")]
    [InlineData("disconnect")] [InlineData("expired")] [InlineData("foreign")] [InlineData("other-actor")] [InlineData("anonymous")] [InlineData("source-cipher")]
    public async Task Host_current_review_policy_authority_and_source_gate_every_observation(string mode)
    {
        await using var f = await MeetingReadinessTests.Fixture.Create(); var clock = new Clock(); var reg = Registration();
        var store = new MeetingOAuthAttemptStore(f.Db, f.Users, f.Readiness, new EphemeralDataProtectionProvider(), clock); var browser = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        var intent = await store.PrepareAsync(f.Actor, f.Academy.Id, reg.Provider, 0, browser, reg.Fingerprint);
        Assert.Equal(202, (await store.CaptureAsync(f.Academy.Id, reg.Provider, intent.Material!.State, browser, reg.Fingerprint, "synthetic-zoom-code", false)).StatusCode);
        var claim = await store.ResumeAsync(f.Actor, f.Academy.Id, reg.Provider, intent.Material.State, browser, reg.Fingerprint);
        var permit = (await store.BeginExchangeAsync(f.Actor, f.Academy.Id, reg, claim)).Permit!;
        Assert.Equal(200, (await store.FinishExchangeAsync(f.Actor, f.Academy.Id, reg, permit, new(200, "TokensReceivedAccountUnverified", Tokens: new(Access, "synthetic-zoom-refresh", clock.Now.AddHours(1), reg.Flow!.RequiredTokenScopes)))).StatusCode);
        var review = (await store.PrepareAccountReviewAsync(f.Actor, f.Academy.Id, reg, permit, new(Subject, Account, clock.Now))).Review!;
        if (mode != "unconfirmed") Assert.Equal(200, (await store.ConfirmAccountReviewAsync(f.Actor, f.Academy.Id, reg, review.Id, 0, review.ReviewHash, true)).StatusCode);
        var policies = mode == "policy" ? MeetingAccountPolicyRegistry.Disabled : MeetingAccountPolicyRegistry.Create([new(f.Academy.Id, reg.Provider, 1, reg.Fingerprint, Subject, Account, null, clock.Now.AddMinutes(-1), clock.Now.AddHours(3))]);
        if (mode == "source-cipher") { (await f.Db.MeetingOAuthExchanges.SingleAsync()).ProtectedEnvelope = "corrupt"; await f.Db.SaveChangesAsync(); }
        async Task During()
        {
            if (mode == "role") Assert.True((await f.Users.RemoveFromRoleAsync(f.User, "Owner")).Succeeded);
            if (mode == "stamp") Assert.True((await f.Users.UpdateSecurityStampAsync(f.User)).Succeeded);
            if (mode == "disconnect") Assert.Equal(200, (await store.DisconnectLocallyAsync(f.Actor, f.Academy.Id, reg.Provider, 2, true)).StatusCode);
            if (mode == "expired") clock.Now = clock.Now.AddHours(2);
        }
        var body = mode switch { "basic" => Body(1), "no-meeting" => Body(4), "foreign-user" => Body(subject: "other-user"), "foreign-account" => Body(account: "other-account"), "pagination" => Body()[..^1] + ",\"next_page_token\":\"next\"}", _ => Body() };
        using var handler = new Handler(body, mode, During); using var transport = new HttpMessageInvoker(handler);
        var actor = mode == "anonymous" ? new ClaimsPrincipal() : mode == "other-actor" ? new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()) }, "synthetic")) : f.Actor;
        var result = await store.CheckZoomAccountAsync(actor, mode == "foreign" ? Guid.NewGuid() : f.Academy.Id, mode == "no-profile" ? Registration(false) : reg, review.Id, policies, new(transport));
        Assert.Equal(mode is "ok" or "basic", result.MeetingPlanObserved); Assert.False(result.CanExecute); Assert.False(result.Connected); Assert.False(result.ProviderVerified);
        var audits = await f.Db.AuditLogs.Where(x => x.Action == "MeetingZoomPlanObservedExecutionQualificationRequired").ToArrayAsync();
        if (mode is "ok" or "basic") { Assert.Equal(503, result.StatusCode); var audit = Assert.Single(audits); Assert.DoesNotContain(Subject, audit.MetadataJson!); Assert.DoesNotContain(Account, audit.MetadataJson!); Assert.DoesNotContain(Access, audit.MetadataJson!); Assert.Equal(1, handler.Calls); }
        else { Assert.NotEqual(200, result.StatusCode); Assert.Empty(audits); if (mode is "no-profile" or "policy" or "unconfirmed" or "foreign" or "other-actor" or "anonymous" or "source-cipher") Assert.Equal(0, handler.Calls); }
    }
}
