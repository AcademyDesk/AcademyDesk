using System.Security.Claims;
using System.Text.Json;
using AcademyDesk.Api.Infrastructure.Meetings;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;

namespace AcademyDesk.Api.Tests;

public sealed class MeetingOAuthResumeTests
{
    private const string Api = "https://api.academy.invalid", Workspace = "https://app.academy.invalid", Code = "synthetic-private-oauth-code";
    private sealed class Clock : TimeProvider { public DateTimeOffset Now = DateTimeOffset.UtcNow; public override DateTimeOffset GetUtcNow() => Now; }
    private static MeetingOAuthRegistrationRegistry Registry(int revision = 1) => MeetingOAuthRegistrationRegistry.Create(Api, Workspace,
        new[] { "GoogleWorkspace", "Microsoft365", "Zoom" }.Select(p => new MeetingOAuthRegistrationInput(p, revision, "synthetic-client", Api + MeetingOAuthRegistrationRegistry.CallbackPath(p), Workspace + "/communication-settings")).ToArray());
    private static DefaultHttpContext Context(ClaimsPrincipal actor, string method = "POST")
    {
        var context = new DefaultHttpContext { User = actor }; context.Request.Scheme = "https";
        context.Request.Host = new("api.academy.invalid"); context.Request.Method = method; context.Request.Headers.Origin = Workspace; return context;
    }
    private static string Cookie(DefaultHttpContext context) => SetCookieHeaderValue.Parse(context.Response.Headers.SetCookie.ToString()).ToString().Split(';')[0];
    private static MeetingOAuthBrowserBridge Bridge(MeetingReadinessTests.Fixture f, Clock clock, IDataProtectionProvider keys, int revision = 1) => new(new(f.Db, f.Users, f.Readiness, keys, clock), Registry(revision), keys, clock);
    private static async Task<(string State, string Cookies, DefaultHttpContext Capture)> Prepare(MeetingReadinessTests.Fixture f, MeetingOAuthBrowserBridge bridge, string provider = "Zoom", bool deny = false)
    {
        var begin = Context(f.Actor); var prepared = await bridge.BeginAsync(begin, f.Academy.Id, provider, 0); Assert.Equal(201, prepared.StatusCode);
        var capture = Context(new(new ClaimsIdentity()), "GET"); capture.Request.Path = MeetingOAuthRegistrationRegistry.CallbackPath(provider);
        capture.Request.QueryString = new("?state=" + prepared.Material!.State + (deny ? "&error=access_denied&error_description=private-provider-text" : "&code=" + Code));
        capture.Request.Headers.Cookie = Cookie(begin);
        Assert.Equal(202, (await bridge.CaptureCallbackAsync(capture, provider)).StatusCode);
        return (prepared.Material.State, Cookie(begin) + "; " + Cookie(capture), capture);
    }

    [Theory] [InlineData("GoogleWorkspace")] [InlineData("Microsoft365")] [InlineData("Zoom")]
    public async Task Anonymous_capture_never_exchanges_and_current_actor_resumes_exactly_once(string provider)
    {
        await using var f = await MeetingReadinessTests.Fixture.Create(); var clock = new Clock(); var keys = new EphemeralDataProtectionProvider(); var bridge = Bridge(f, clock, keys);
        var prepared = await Prepare(f, bridge, provider);
        var captured = await f.Db.MeetingOAuthCallbacks.SingleAsync(); var attempt = await f.Db.MeetingOAuthAttempts.SingleAsync();
        Assert.DoesNotContain(Code, captured.ProtectedEnvelope); Assert.DoesNotContain(Code, prepared.Cookies);
        Assert.Equal("CallbackCaptured", attempt.Status); Assert.Equal(1, attempt.Version); Assert.Null(attempt.ClaimedAtUtc);
        Assert.Equal("AuthorizationPending", (await f.Db.MeetingProviderBindings.SingleAsync()).Status);
        Assert.Null((await f.Db.AuditLogs.SingleAsync(x => x.EntityId == captured.Id)).ActorUserId);
        var ticket = SetCookieHeaderValue.Parse(prepared.Capture.Response.Headers.SetCookie.ToString());
        Assert.True(ticket.Secure); Assert.True(ticket.HttpOnly); Assert.False(ticket.Domain.HasValue); Assert.Equal("/", ticket.Path.ToString());
        Assert.Equal(409, (await bridge.CaptureCallbackAsync(prepared.Capture, provider)).StatusCode);
        var resume = Context(f.Actor); resume.Request.Headers.Cookie = prepared.Cookies;
        var result = await bridge.ResumeCapturedAsync(resume, f.Academy.Id, provider);
        Assert.Equal(200, result.StatusCode); Assert.Equal("OAuthResumedNotConnected", result.Code); Assert.Equal(Code, result.AuthorizationCode); Assert.NotNull(result.Material!.Verifier);
        Assert.DoesNotContain(Code, JsonSerializer.Serialize(result)); Assert.DoesNotContain(Code, result.ToString());
        Assert.Equal("Consumed", captured.Status); Assert.Equal(1, captured.Version);
        var protector = keys.CreateProtector("academy.meeting-oauth-callback.v1", captured.AcademyId.ToString("D"), captured.ActorUserId.ToString("D"), provider, captured.AttemptId.ToString("D"), captured.Id.ToString("D"));
        using var consumed = JsonDocument.Parse(protector.Unprotect(captured.ProtectedEnvelope));
        Assert.Equal(JsonValueKind.Null, consumed.RootElement.GetProperty("AuthorizationCode").ValueKind);
        Assert.DoesNotContain(Code, captured.ProtectedEnvelope); Assert.Equal("Claimed", attempt.Status); Assert.Equal(2, attempt.Version);
        Assert.Equal(2, resume.Response.Headers.SetCookie.Count); Assert.Equal(409, (await bridge.ResumeCapturedAsync(ContextWithCookies(f.Actor, prepared.Cookies), f.Academy.Id, provider)).StatusCode);
        Assert.Equal(4, await f.Db.AuditLogs.CountAsync()); Assert.Empty(await f.Db.MeetingOperations.ToListAsync()); Assert.Empty(await f.Db.Notifications.ToListAsync());
        Assert.Equal("VerificationRequired", (await f.Db.MeetingProviderBindings.SingleAsync()).Status);
    }
    private static DefaultHttpContext ContextWithCookies(ClaimsPrincipal actor, string cookies) { var context = Context(actor); context.Request.Headers.Cookie = cookies; return context; }

    [Theory] [InlineData("anonymous")] [InlineData("missing-resume")] [InlineData("missing-browser")] [InlineData("duplicate-resume")]
    [InlineData("forged-resume")] [InlineData("origin")] [InlineData("host")] [InlineData("http")]
    [InlineData("method")] [InlineData("query")] [InlineData("registration")] [InlineData("keys")]
    [InlineData("foreign")] [InlineData("provider")] [InlineData("expiry")] [InlineData("stamp")]
    [InlineData("role")] [InlineData("tenant")] [InlineData("inactive")] [InlineData("module")]
    [InlineData("envelope")] [InlineData("callback-expiry")] [InlineData("generation")]
    public async Task Manipulated_resume_cannot_release_codes_or_consume_pending_capture(string mode)
    {
        await using var f = await MeetingReadinessTests.Fixture.Create(); var clock = new Clock(); var keys = new EphemeralDataProtectionProvider(); var bridge = Bridge(f, clock, keys);
        var prepared = await Prepare(f, bridge); var context = ContextWithCookies(f.Actor, prepared.Cookies);
        var cookies = prepared.Cookies.Split("; ");
        if (mode == "anonymous") context.User = new(new ClaimsIdentity());
        if (mode == "missing-resume") context.Request.Headers.Cookie = cookies[0];
        if (mode == "missing-browser") context.Request.Headers.Cookie = cookies[1];
        if (mode == "duplicate-resume") context.Request.Headers.Cookie = prepared.Cookies + "; " + cookies[1];
        if (mode == "forged-resume") context.Request.Headers.Cookie = cookies[0] + "; " + MeetingOAuthBrowserBridge.ResumeCookieName("Zoom") + "=forged";
        if (mode == "origin") context.Request.Headers.Origin = "https://attacker.invalid";
        if (mode == "host") context.Request.Host = new("attacker.invalid");
        if (mode == "http") context.Request.Scheme = "http";
        if (mode == "method") context.Request.Method = "GET";
        if (mode == "query") context.Request.QueryString = new("?approved=true");
        if (mode == "registration") bridge = Bridge(f, clock, keys, 2);
        if (mode == "keys") bridge = Bridge(f, clock, new EphemeralDataProtectionProvider());
        if (mode == "expiry") clock.Now = clock.Now.AddMinutes(5);
        if (mode == "stamp") Assert.True((await f.Users.UpdateSecurityStampAsync(f.User)).Succeeded);
        if (mode == "role") Assert.True((await f.Users.RemoveFromRoleAsync(f.User, "Owner")).Succeeded);
        if (mode == "tenant") { f.User.AcademyId = Guid.NewGuid(); Assert.True((await f.Users.UpdateAsync(f.User)).Succeeded); }
        if (mode == "inactive") { f.User.IsActive = false; Assert.True((await f.Users.UpdateAsync(f.User)).Succeeded); }
        if (mode == "module") { f.Academy.EnabledModulesJson = "[]"; await f.Db.SaveChangesAsync(); }
        var captured = await f.Db.MeetingOAuthCallbacks.SingleAsync();
        if (mode == "envelope") { captured.ProtectedEnvelope = "forged"; await f.Db.SaveChangesAsync(); }
        if (mode == "callback-expiry") { captured.ExpiresAtUtc = captured.ExpiresAtUtc.AddMinutes(1); await f.Db.SaveChangesAsync(); }
        if (mode == "generation") { var binding = await f.Db.MeetingProviderBindings.SingleAsync(); binding.Generation++; binding.Version++; await f.Db.SaveChangesAsync(); }
        var result = await bridge.ResumeCapturedAsync(context, mode == "foreign" ? Guid.NewGuid() : f.Academy.Id, mode == "provider" ? "Microsoft365" : "Zoom");
        Assert.Contains(result.StatusCode, new[] { 400, 401, 403, 404, 409, 410 }); Assert.Null(result.AuthorizationCode); Assert.Null(result.Material);
        Assert.Equal("Captured", captured.Status); Assert.Equal("CallbackCaptured", (await f.Db.MeetingOAuthAttempts.SingleAsync()).Status);
        Assert.Equal(2, await f.Db.AuditLogs.CountAsync()); Assert.Equal(0, context.Response.Headers.SetCookie.Count);
    }

    [Fact] public async Task Provider_denial_is_authenticated_consumed_and_never_returns_code()
    {
        await using var f = await MeetingReadinessTests.Fixture.Create(); var bridge = Bridge(f, new Clock(), new EphemeralDataProtectionProvider()); var prepared = await Prepare(f, bridge, deny: true);
        var result = await bridge.ResumeCapturedAsync(ContextWithCookies(f.Actor, prepared.Cookies), f.Academy.Id, "Zoom");
        Assert.Equal(400, result.StatusCode); Assert.Equal("ProviderAuthorizationDeclined", result.Code); Assert.Null(result.Material); Assert.Null(result.AuthorizationCode);
        Assert.Equal("Consumed", (await f.Db.MeetingOAuthCallbacks.SingleAsync()).Status); Assert.Equal(4, await f.Db.AuditLogs.CountAsync());
    }

    [Fact] public async Task Capture_does_not_extend_original_expiry_or_allow_direct_claim_bypass()
    {
        await using var f = await MeetingReadinessTests.Fixture.Create(); var clock = new Clock(); var keys = new EphemeralDataProtectionProvider(); var bridge = Bridge(f, clock, keys);
        var prepared = await Prepare(f, bridge); var captured = await f.Db.MeetingOAuthCallbacks.SingleAsync(); var attempt = await f.Db.MeetingOAuthAttempts.SingleAsync();
        Assert.Equal(attempt.ExpiresAtUtc, captured.ExpiresAtUtc);
        var direct = Context(f.Actor, "GET"); direct.Request.Path = MeetingOAuthRegistrationRegistry.CallbackPath("Zoom");
        direct.Request.QueryString = new("?state=" + prepared.State + "&code=other-code"); direct.Request.Headers.Cookie = prepared.Cookies.Split("; ")[0];
        Assert.Equal(409, (await bridge.AcceptAuthenticatedCallbackAsync(direct, f.Academy.Id, "Zoom")).StatusCode);
        clock.Now = clock.Now.AddMinutes(5);
        Assert.Equal(410, (await bridge.ResumeCapturedAsync(ContextWithCookies(f.Actor, prepared.Cookies), f.Academy.Id, "Zoom")).StatusCode);
        Assert.Equal("Captured", captured.Status); Assert.Equal("CallbackCaptured", attempt.Status);
    }
}
