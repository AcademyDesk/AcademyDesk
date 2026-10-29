using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.WebUtilities;

namespace AcademyDesk.Api.Infrastructure.Meetings;

internal sealed record MeetingOAuthBridgeOutcome(int StatusCode, string Code,
    [property: JsonIgnore] MeetingOAuthMaterial? Material = null, [property: JsonIgnore] string? AuthorizationCode = null)
{
    public override string ToString() => $"MeetingOAuthBridgeOutcome {{ StatusCode = {StatusCode}, Code = {Code} }}";
}

/// <summary>
/// Internal authenticated adapter primitive, deliberately NOT registered in DI or
/// exposed by routes. No redirects, credentials, provider calls or token exchange.
/// Anonymous callbacks can only quarantine an encrypted code. Resume requires
/// current authenticated authority plus both protected browser cookies. No token
/// exchange, usable connection UI or provider redirect is enabled by this helper.
/// </summary>
internal sealed class MeetingOAuthBrowserBridge(MeetingOAuthAttemptStore store,
    MeetingOAuthRegistrationRegistry registry, IDataProtectionProvider protection, TimeProvider clock)
{
    private sealed record Correlation(Guid Academy, string Provider, string StateHash, string Registration,
        string Browser, DateTimeOffset Expires);
    private sealed record ResumeTicket(Guid Academy, string Provider, string State, string Registration, DateTimeOffset Expires);
    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    internal static string CookieName(string provider, string state) => "__Host-academydesk-meeting-" + provider + "-" + Hash(state);
    internal static string ResumeCookieName(string provider) => "__Host-academydesk-meeting-resume-" + provider;
    private IDataProtector Protector(string name) => protection.CreateProtector("academy.meeting-oauth-browser.v1", name);
    private static CookieOptions Options() => new() { Secure = true, HttpOnly = true, SameSite = SameSiteMode.Lax, Path = "/", IsEssential = true };
    private static void PrivateResponse(HttpResponse response)
    {
        response.Headers.CacheControl = "no-store"; response.Headers.Pragma = "no-cache";
        response.Headers["Referrer-Policy"] = "no-referrer";
    }
    private static bool ExactHost(HttpRequest request, Uri callback) => request.IsHttps &&
        string.Equals(request.Host.Value, callback.Authority, StringComparison.OrdinalIgnoreCase) && !request.PathBase.HasValue;

    public async Task<MeetingOAuthBridgeOutcome> BeginAsync(HttpContext context, Guid academy, string provider,
        int expectedVersion, CancellationToken token = default)
    {
        PrivateResponse(context.Response);
        if (context.User.Identity?.IsAuthenticated != true) return new(401, "ReauthenticationRequired");
        var registration = registry.Resolve(provider);
        if (registration is null) return new(503, "RegistrationUnavailable");
        if (!HttpMethods.IsPost(context.Request.Method) || !ExactHost(context.Request, registration.Callback) ||
            context.Request.Headers.Origin.Count != 1 || context.Request.Headers.Origin[0] != registration.WorkspaceReturn.GetLeftPart(UriPartial.Authority))
            return new(400, "InvalidConnectionOrigin");
        var browser = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        var result = await store.PrepareAsync(context.User, academy, provider, expectedVersion, browser, registration.Fingerprint, token);
        if (result.StatusCode != 201 || result.Material is null) return new(result.StatusCode, result.Code);
        var name = CookieName(provider, result.Material.State);
        var expires = clock.GetUtcNow().AddMinutes(5);
        var cookie = Protector(name).Protect(JsonSerializer.Serialize(new Correlation(academy, provider, Hash(result.Material.State), registration.Fingerprint, browser, expires)));
        var options = Options(); options.Expires = expires; options.MaxAge = TimeSpan.FromMinutes(5);
        context.Response.Cookies.Append(name, cookie, options);
        return new(201, "OAuthBrowserIntentOnlyNotConnected", result.Material);
    }

    public async Task<MeetingOAuthBridgeOutcome> AcceptAuthenticatedCallbackAsync(HttpContext context, Guid academy,
        string provider, CancellationToken token = default)
    {
        PrivateResponse(context.Response);
        if (context.User.Identity?.IsAuthenticated != true) return new(401, "ReauthenticationRequired");
        var registration = registry.Resolve(provider);
        if (registration is null) return new(503, "RegistrationUnavailable");
        var parsed = ReadCallback(context.Request, registration, academy);
        if (parsed.Error is not null) return parsed.Error;
        var result = await store.ClaimAsync(context.User, academy, provider, parsed.State, parsed.Correlation!.Browser, registration.Fingerprint, token);
        if (result.StatusCode != 200) return new(result.StatusCode, result.Code);
        context.Response.Cookies.Delete(CookieName(provider, parsed.State), Options());
        return parsed.Denied ? new(400, "ProviderAuthorizationDeclined") : new(200, "OAuthCallbackClaimedNotConnected", result.Material, parsed.Code);
    }

    // Future anonymous GET callback adapter calls this, then redirects ONLY to the
    // registration's fixed WorkspaceReturn. Never place codes/tickets in that URL.
    public async Task<MeetingOAuthBridgeOutcome> CaptureCallbackAsync(HttpContext context, string provider, CancellationToken token = default)
    {
        PrivateResponse(context.Response);
        var registration = registry.Resolve(provider);
        if (registration is null) return new(503, "RegistrationUnavailable");
        var parsed = ReadCallback(context.Request, registration, null);
        if (parsed.Error is not null) return parsed.Error;
        var correlation = parsed.Correlation!;
        var result = await store.CaptureAsync(correlation.Academy, provider, parsed.State, correlation.Browser, registration.Fingerprint,
            parsed.Denied ? null : parsed.Code, parsed.Denied, token);
        if (result.StatusCode != 202) return new(result.StatusCode, result.Code);
        var name = ResumeCookieName(provider);
        var ticket = Protector(name).Protect(JsonSerializer.Serialize(new ResumeTicket(correlation.Academy, provider, parsed.State, registration.Fingerprint, correlation.Expires)));
        var options = Options(); options.Expires = correlation.Expires; options.MaxAge = correlation.Expires - clock.GetUtcNow();
        context.Response.Cookies.Append(name, ticket, options);
        return new(202, "CallbackCapturedAuthenticationRequired");
    }

    public async Task<MeetingOAuthBridgeOutcome> ResumeCapturedAsync(HttpContext context, Guid academy, string provider, CancellationToken token = default)
    {
        PrivateResponse(context.Response);
        if (context.User.Identity?.IsAuthenticated != true) return new(401, "ReauthenticationRequired");
        var registration = registry.Resolve(provider);
        if (registration is null) return new(503, "RegistrationUnavailable");
        if (!HttpMethods.IsPost(context.Request.Method) || !ExactHost(context.Request, registration.Callback) || context.Request.QueryString.HasValue ||
            context.Request.Headers.Origin.Count != 1 || context.Request.Headers.Origin[0] != registration.WorkspaceReturn.GetLeftPart(UriPartial.Authority))
            return new(400, "InvalidConnectionOrigin");
        var name = ResumeCookieName(provider);
        if (!ReadCookie(context.Request, name, out var cookie)) return new(409, "ResumeCookieRequired");
        ResumeTicket? ticket;
        try { ticket = JsonSerializer.Deserialize<ResumeTicket>(Protector(name).Unprotect(cookie!)); }
        catch (Exception ex) when (ex is CryptographicException or JsonException or ArgumentException) { return new(409, "ResumeCookieInvalid"); }
        if (ticket is null || ticket.Academy != academy || ticket.Provider != provider || ticket.Registration != registration.Fingerprint ||
            !MeetingOAuthRegistrationRegistry.ValidRandom(ticket.State)) return new(409, "ResumeCookieInvalid");
        if (ticket.Expires <= clock.GetUtcNow()) return new(410, "ResumeExpired");
        var correlation = ReadCorrelation(context.Request, registration, ticket.State, academy);
        if (correlation.Error is not null) return correlation.Error;
        var result = await store.ResumeAsync(context.User, academy, provider, ticket.State, correlation.Correlation!.Browser, registration.Fingerprint, token);
        if (result.StatusCode == 200 || result.Code == "ProviderAuthorizationDeclined")
        {
            context.Response.Cookies.Delete(name, Options()); context.Response.Cookies.Delete(CookieName(provider, ticket.State), Options());
        }
        return new(result.StatusCode, result.StatusCode == 200 ? "OAuthResumedNotConnected" : result.Code, result.Material, result.AuthorizationCode);
    }

    private sealed record CallbackRead(string State = "", string Code = "", bool Denied = false, Correlation? Correlation = null, MeetingOAuthBridgeOutcome? Error = null)
    {
        public override string ToString() => "[Callback input redacted]";
    }
    private CallbackRead ReadCallback(HttpRequest request, MeetingOAuthRegistration registration, Guid? academy)
    {
        if (!HttpMethods.IsGet(request.Method) || !ExactHost(request, registration.Callback) ||
            request.Path.Value != registration.Callback.AbsolutePath || request.QueryString.Value?.Length > 8192)
            return new(Error: new(400, "InvalidConnectionCallback"));
        // Provider metadata is accepted only as bounded ignored input; never used
        // as authority/redirects. ASP.NET groups duplicate/case-folded keys: reject
        // every multi-value or non-exact key rather than picking the first value.
        var allowed = registration.Provider == "GoogleWorkspace"
            ? new[] { "state", "code", "error", "error_description", "error_uri", "scope", "authuser", "prompt", "hd", "iss" }
            : new[] { "state", "code", "error", "error_description", "error_uri", "session_state" };
        var query = request.Query;
        if (query.Any(p => !allowed.Contains(p.Key, StringComparer.Ordinal) || p.Value.Count != 1 ||
            p.Value[0] is not { Length: <= 4096 } value || value.Any(char.IsControl))) return new(Error: new(400, "InvalidConnectionCallback"));
        var state = query["state"].ToString(); var code = query["code"].ToString(); var error = query["error"].ToString();
        if (!MeetingOAuthRegistrationRegistry.ValidRandom(state) || query.ContainsKey("code") == query.ContainsKey("error") ||
            (query.ContainsKey("code") && code.Length == 0) || (query.ContainsKey("error") && error.Length == 0) ||
            code.Any(c => c is < '!' or > '~') || error.Length > 128 || error.Any(c => c is < '!' or > '~')) return new(Error: new(400, "InvalidConnectionCallback"));
        var read = ReadCorrelation(request, registration, state, academy);
        return read.Error is not null ? read : new(state, code, error.Length != 0, read.Correlation);
    }
    private static bool ReadCookie(HttpRequest request, string name, out string? value)
    {
        value = null;
        var count = request.Headers.Cookie.SelectMany(h => (h ?? "").Split(';')).Count(p => p.Trim().StartsWith(name + "=", StringComparison.Ordinal));
        return count == 1 && request.Cookies.TryGetValue(name, out value) && value.Length <= 4096;
    }
    private CallbackRead ReadCorrelation(HttpRequest request, MeetingOAuthRegistration registration, string state, Guid? academy)
    {
        var name = CookieName(registration.Provider, state);
        if (!ReadCookie(request, name, out var protectedCookie)) return new(Error: new(409, "BrowserCorrelationRequired"));
        Correlation? correlation;
        try { correlation = JsonSerializer.Deserialize<Correlation>(Protector(name).Unprotect(protectedCookie!)); }
        catch (Exception ex) when (ex is CryptographicException or JsonException or ArgumentException) { return new(Error: new(409, "BrowserCorrelationInvalid")); }
        if (correlation is null || (academy is not null && correlation.Academy != academy) || correlation.Provider != registration.Provider || correlation.StateHash != Hash(state) ||
            correlation.Registration != registration.Fingerprint || !MeetingOAuthRegistrationRegistry.ValidRandom(correlation.Browser))
            return new(Error: new(409, "BrowserCorrelationInvalid"));
        if (correlation.Expires <= clock.GetUtcNow()) return new(Error: new(410, "BrowserCorrelationExpired"));
        return new(Correlation: correlation);
    }
}
