using System.Security.Claims;
using System.Text.Json;
using AcademyDesk.Api.Infrastructure.Meetings;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;

namespace AcademyDesk.Api.Tests;

public sealed class MeetingOAuthBrowserTests
{
    private const string Api = "https://api.academy.invalid", Workspace = "https://app.academy.invalid";
    private sealed class Clock : TimeProvider { public DateTimeOffset Now = DateTimeOffset.UtcNow; public override DateTimeOffset GetUtcNow() => Now; }
    private static MeetingOAuthRegistrationInput Input(string provider = "Zoom") => new(provider, 1, "synthetic-client", Api + MeetingOAuthRegistrationRegistry.CallbackPath(provider), Workspace + "/communication-settings");
    private static MeetingOAuthRegistrationRegistry Registry(params MeetingOAuthRegistrationInput[] inputs) => MeetingOAuthRegistrationRegistry.Create(Api, Workspace, inputs.Length == 0 ? new[] { Input() } : inputs);
    private static MeetingOAuthBrowserBridge Bridge(MeetingReadinessTests.Fixture f, Clock clock, IDataProtectionProvider keys, MeetingOAuthRegistrationRegistry? registry = null) =>
        new(new(f.Db, f.Users, f.Readiness, keys, clock), registry ?? Registry(), keys, clock);
    private static DefaultHttpContext BeginContext(ClaimsPrincipal actor)
    {
        var context = new DefaultHttpContext { User = actor }; context.Request.Scheme = "https";
        context.Request.Host = new("api.academy.invalid"); context.Request.Method = "POST";
        context.Request.Headers.Origin = Workspace; return context;
    }
    private static DefaultHttpContext Callback(ClaimsPrincipal actor, string provider, string state, string cookie)
    {
        var context = BeginContext(actor); context.Request.Method = "GET"; context.Request.Headers.Remove("Origin");
        context.Request.Path = MeetingOAuthRegistrationRegistry.CallbackPath(provider);
        context.Request.QueryString = new("?state=" + state + "&code=synthetic-code"); context.Request.Headers.Cookie = cookie; return context;
    }
    private static string Cookie(DefaultHttpContext context) => SetCookieHeaderValue.Parse(context.Response.Headers.SetCookie.ToString()).ToString().Split(';')[0];

    [Theory] [InlineData("GoogleWorkspace")] [InlineData("Microsoft365")] [InlineData("Zoom")]
    public async Task Registered_provider_is_browser_bound_once_and_never_connected(string provider)
    {
        await using var f = await MeetingReadinessTests.Fixture.Create(); var clock = new Clock();
        var bridge = Bridge(f, clock, new EphemeralDataProtectionProvider(), Registry(Input(provider))); var begin = BeginContext(f.Actor);
        var prepared = await bridge.BeginAsync(begin, f.Academy.Id, provider, 0); Assert.Equal(201, prepared.StatusCode);
        var cookie = SetCookieHeaderValue.Parse(begin.Response.Headers.SetCookie.ToString());
        Assert.StartsWith("__Host-", cookie.Name.ToString()); Assert.True(cookie.Secure); Assert.True(cookie.HttpOnly);
        Assert.Equal("/", cookie.Path.ToString()); Assert.Equal(Microsoft.Net.Http.Headers.SameSiteMode.Lax, cookie.SameSite);
        Assert.False(cookie.Domain.HasValue); Assert.Equal(TimeSpan.FromMinutes(5), cookie.MaxAge);
        var callback = Callback(f.Actor, provider, prepared.Material!.State, Cookie(begin));
        var result = await bridge.AcceptAuthenticatedCallbackAsync(callback, f.Academy.Id, provider);
        Assert.Equal(200, result.StatusCode); Assert.Equal("OAuthCallbackClaimedNotConnected", result.Code); Assert.NotNull(result.Material!.Verifier);
        Assert.Equal("synthetic-code", result.AuthorizationCode);
        foreach (var secret in new[] { result.Material.Verifier, result.Material.State, result.AuthorizationCode })
        { Assert.DoesNotContain(secret!, JsonSerializer.Serialize(result)); Assert.DoesNotContain(secret!, result.ToString()); }
        var deleted = SetCookieHeaderValue.Parse(callback.Response.Headers.SetCookie.ToString());
        Assert.Equal(cookie.Name, deleted.Name); Assert.Equal("", deleted.Value.ToString()); Assert.True(deleted.Expires < clock.Now);
        Assert.True(deleted.Secure); Assert.True(deleted.HttpOnly); Assert.Equal("/", deleted.Path.ToString());
        Assert.Equal("no-store", callback.Response.Headers.CacheControl); Assert.Equal("no-referrer", callback.Response.Headers["Referrer-Policy"]);
        Assert.Equal(409, (await bridge.AcceptAuthenticatedCallbackAsync(Callback(f.Actor, provider, prepared.Material.State, Cookie(begin)), f.Academy.Id, provider)).StatusCode);
        Assert.Equal("VerificationRequired", (await f.Db.MeetingProviderBindings.SingleAsync()).Status);
        Assert.Equal(2, await f.Db.AuditLogs.CountAsync()); Assert.Empty(await f.Db.MeetingOperations.ToListAsync()); Assert.Empty(await f.Db.Notifications.ToListAsync());
    }

    [Theory] [InlineData("origin")] [InlineData("http")] [InlineData("host")] [InlineData("method")]
    [InlineData("anonymous")] [InlineData("disabled")] [InlineData("foreign")] [InlineData("version")]
    public async Task Invalid_begin_does_not_create_intents_or_cookies(string mode)
    {
        await using var f = await MeetingReadinessTests.Fixture.Create(); var context = BeginContext(f.Actor);
        var bridge = Bridge(f, new Clock(), new EphemeralDataProtectionProvider(), mode == "disabled" ? MeetingOAuthRegistrationRegistry.Disabled : null);
        if (mode == "origin") context.Request.Headers.Origin = "https://attacker.invalid";
        if (mode == "http") context.Request.Scheme = "http";
        if (mode == "host") context.Request.Host = new("attacker.invalid");
        if (mode == "method") context.Request.Method = "GET";
        if (mode == "anonymous") context.User = new(new ClaimsIdentity());
        var result = await bridge.BeginAsync(context, mode == "foreign" ? Guid.NewGuid() : f.Academy.Id, "Zoom", mode == "version" ? 1 : 0);
        Assert.Contains(result.StatusCode, new[] { 400, 401, 403, 409, 503 });
        Assert.Empty(await f.Db.MeetingOAuthAttempts.ToListAsync()); Assert.Empty(await f.Db.AuditLogs.ToListAsync()); Assert.Equal(0, context.Response.Headers.SetCookie.Count);
    }

    [Theory] [InlineData("missing-cookie")] [InlineData("forged-cookie")] [InlineData("duplicate-cookie")]
    [InlineData("duplicate-state")] [InlineData("duplicate-code")] [InlineData("case-key")]
    [InlineData("mixed-result")] [InlineData("unknown-key")] [InlineData("empty-code")]
    [InlineData("large-code")] [InlineData("control")] [InlineData("path")]
    [InlineData("host")] [InlineData("http")] [InlineData("method")] [InlineData("path-base")]
    [InlineData("foreign")] [InlineData("registration")] [InlineData("keys")]
    [InlineData("expiry")] [InlineData("anonymous")] [InlineData("stamp")] [InlineData("revoked")]
    [InlineData("empty-mixed-code")] [InlineData("empty-mixed-error")]
    public async Task Manipulated_or_unauthenticated_callbacks_do_not_claim(string mode)
    {
        await using var f = await MeetingReadinessTests.Fixture.Create(); var clock = new Clock(); var keys = new EphemeralDataProtectionProvider();
        var bridge = Bridge(f, clock, keys); var begin = BeginContext(f.Actor); var prepared = await bridge.BeginAsync(begin, f.Academy.Id, "Zoom", 0);
        var callback = Callback(f.Actor, "Zoom", prepared.Material!.State, Cookie(begin)); var query = callback.Request.QueryString.Value!;
        if (mode == "missing-cookie") callback.Request.Headers.Remove("Cookie");
        if (mode == "forged-cookie") callback.Request.Headers.Cookie = MeetingOAuthBrowserBridge.CookieName("Zoom", prepared.Material.State) + "=forged";
        if (mode == "duplicate-cookie") callback.Request.Headers.Cookie = Cookie(begin) + "; " + Cookie(begin);
        if (mode == "duplicate-state") callback.Request.QueryString = new(query + "&state=" + prepared.Material.State);
        if (mode == "duplicate-code") callback.Request.QueryString = new(query + "&code=another-code");
        if (mode == "case-key") callback.Request.QueryString = new(query.Replace("state=", "State="));
        if (mode == "mixed-result") callback.Request.QueryString = new(query + "&error=access_denied");
        if (mode == "empty-mixed-code") callback.Request.QueryString = new(query.Replace("synthetic-code", "") + "&error=access_denied");
        if (mode == "empty-mixed-error") callback.Request.QueryString = new(query + "&error=");
        if (mode == "unknown-key") callback.Request.QueryString = new(query + "&returnUrl=https://attacker.invalid");
        if (mode == "empty-code") callback.Request.QueryString = new(query.Replace("synthetic-code", ""));
        if (mode == "large-code") callback.Request.QueryString = new(query.Replace("synthetic-code", new string('x', 4097)));
        if (mode == "control") callback.Request.QueryString = new(query.Replace("synthetic-code", "code%0a"));
        if (mode == "path") callback.Request.Path = "/api/meetings/oauth/Zoom/callback/";
        if (mode == "host") callback.Request.Host = new("attacker.invalid");
        if (mode == "http") callback.Request.Scheme = "http";
        if (mode == "method") callback.Request.Method = "POST";
        if (mode == "path-base") callback.Request.PathBase = "/other";
        if (mode == "registration") bridge = Bridge(f, clock, keys, Registry(Input() with { Revision = 2 }));
        if (mode == "keys") bridge = Bridge(f, clock, new EphemeralDataProtectionProvider());
        if (mode == "expiry") clock.Now = clock.Now.AddMinutes(5);
        if (mode == "anonymous") callback.User = new(new ClaimsIdentity());
        if (mode == "stamp") Assert.True((await f.Users.UpdateSecurityStampAsync(f.User)).Succeeded);
        if (mode == "revoked") Assert.True((await f.Users.RemoveFromRoleAsync(f.User, "Owner")).Succeeded);
        var result = await bridge.AcceptAuthenticatedCallbackAsync(callback, mode == "foreign" ? Guid.NewGuid() : f.Academy.Id, "Zoom");
        Assert.Contains(result.StatusCode, new[] { 400, 401, 403, 409, 410 }); Assert.Null(result.Material); Assert.Null(result.AuthorizationCode);
        Assert.Equal("Pending", (await f.Db.MeetingOAuthAttempts.SingleAsync()).Status); Assert.Single(await f.Db.AuditLogs.ToListAsync());
        Assert.Equal(0, callback.Response.Headers.SetCookie.Count); Assert.Equal("no-store", callback.Response.Headers.CacheControl);
    }

    [Fact] public async Task Provider_denial_is_consumed_without_echoing_untrusted_error_or_code()
    {
        await using var f = await MeetingReadinessTests.Fixture.Create(); var bridge = Bridge(f, new Clock(), new EphemeralDataProtectionProvider());
        var begin = BeginContext(f.Actor); var prepared = await bridge.BeginAsync(begin, f.Academy.Id, "Zoom", 0);
        var callback = Callback(f.Actor, "Zoom", prepared.Material!.State, Cookie(begin));
        callback.Request.QueryString = new("?state=" + prepared.Material.State + "&error=access_denied&error_description=private-provider-text&error_uri=https://attacker.invalid");
        var result = await bridge.AcceptAuthenticatedCallbackAsync(callback, f.Academy.Id, "Zoom");
        Assert.Equal(400, result.StatusCode); Assert.Equal("ProviderAuthorizationDeclined", result.Code); Assert.Null(result.Material); Assert.Null(result.AuthorizationCode);
        Assert.DoesNotContain("private-provider-text", JsonSerializer.Serialize(result)); Assert.Equal("Claimed", (await f.Db.MeetingOAuthAttempts.SingleAsync()).Status);
    }

    [Theory] [InlineData("http")] [InlineData("slash")] [InlineData("userinfo")] [InlineData("query")] [InlineData("fragment")]
    [InlineData("callback-host")] [InlineData("callback-path")] [InlineData("callback-query")] [InlineData("callback-escape")]
    [InlineData("return-host")] [InlineData("return-query")] [InlineData("provider")] [InlineData("revision")] [InlineData("client")]
    [InlineData("duplicate")] [InlineData("empty")] [InlineData("null")]
    public void Bad_configuration_is_disabled_as_a_whole_without_echoing_values(string mode)
    {
        var api = Api; var input = Input(); var inputs = new[] { input };
        if (mode == "http") api = Api.Replace("https", "http");
        if (mode == "slash") api += "/";
        if (mode == "userinfo") api = "https://private-password@api.academy.invalid";
        if (mode == "query") api += "?private-token";
        if (mode == "fragment") api += "#private-token";
        if (mode == "callback-host") input = input with { CallbackUri = input.CallbackUri.Replace("api.academy.invalid", "attacker.invalid") };
        if (mode == "callback-path") input = input with { CallbackUri = input.CallbackUri + "/" };
        if (mode == "callback-query") input = input with { CallbackUri = input.CallbackUri + "?return=evil" };
        if (mode == "callback-escape") input = input with { CallbackUri = input.CallbackUri.Replace("Zoom", "%5Aoom") };
        if (mode == "return-host") input = input with { WorkspaceReturnUri = "https://attacker.invalid/communication-settings" };
        if (mode == "return-query") input = input with { WorkspaceReturnUri = input.WorkspaceReturnUri + "?next=evil" };
        if (mode == "provider") input = input with { Provider = "arbitrary-provider" };
        if (mode == "revision") input = input with { Revision = 0 };
        if (mode == "client") input = input with { ClientId = "id\nprivate-token" };
        inputs = mode == "duplicate" ? new[] { input, input } : mode == "empty" ? Array.Empty<MeetingOAuthRegistrationInput>() : new[] { input };
        var registry = MeetingOAuthRegistrationRegistry.Create(api, Workspace, mode == "null" ? null : inputs);
        Assert.False(registry.IsValid); Assert.Null(registry.Resolve("Zoom")); Assert.DoesNotContain("private", input.ToString());
        Assert.Equal("{}", JsonSerializer.Serialize(input));
    }

    [Fact] public void Three_registrations_have_stable_independent_and_revision_bound_fingerprints()
    {
        var inputs = new[] { Input("GoogleWorkspace"), Input("Microsoft365"), Input() }; var registry = Registry(inputs);
        Assert.True(registry.IsValid); Assert.Equal(3, inputs.Select(i => registry.Resolve(i.Provider)!.Fingerprint).Distinct().Count());
        Assert.Equal(registry.Resolve("Zoom")!.Fingerprint, Registry(inputs.Reverse().ToArray()).Resolve("Zoom")!.Fingerprint);
        Assert.NotEqual(registry.Resolve("Zoom")!.Fingerprint, Registry(Input() with { Revision = 2 }).Resolve("Zoom")!.Fingerprint);
        Assert.NotEqual(registry.Resolve("Zoom")!.Fingerprint, Registry(Input() with { ClientId = "other-client" }).Resolve("Zoom")!.Fingerprint);
        Assert.Equal("{}", JsonSerializer.Serialize(registry.Resolve("Zoom"))); Assert.Null(registry.Resolve("zoom"));
    }
}
