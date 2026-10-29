using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Infrastructure.Meetings;

internal sealed class MeetingOAuthTokens(string access, string refresh, DateTimeOffset expires, IReadOnlyList<string> scopes, string? identityToken = null)
{
    [JsonIgnore] public string AccessToken { get; } = access;
    [JsonIgnore] public string RefreshToken { get; } = refresh;
    [JsonIgnore] public DateTimeOffset ExpiresAt { get; } = expires;
    [JsonIgnore] public IReadOnlyList<string> Scopes { get; } = scopes;
    [JsonIgnore] public string? IdentityToken { get; } = identityToken;
    public override string ToString() => "[OAuth tokens redacted; account unverified]";
}
internal sealed record MeetingOAuthExchangeOutcome(int StatusCode, string Code, bool OutcomeUnknown = false,
    [property: JsonIgnore] MeetingOAuthTokens? Tokens = null)
{
    public bool CanRetry => false;
    public override string ToString() => $"MeetingOAuthExchangeOutcome {{ StatusCode = {StatusCode}, Code = {Code}, OutcomeUnknown = {OutcomeUnknown}, CanRetry = false }}";
}

// Not DI registered, no routes or call sites. The lifecycle coordinator must
// authorize, persist exchange intent and handle post-claim crashes before enabling.
internal sealed class MeetingOAuthCodeExchange(AcademyDeskDbContext db, UserManager<ApplicationUser> users,
    MeetingReadinessService readiness, HttpMessageInvoker transport, TimeProvider clock)
{
    internal static SocketsHttpHandler CreateSecureHandler() => new()
    {
        AllowAutoRedirect = false, UseCookies = false, UseProxy = false,
        AutomaticDecompression = DecompressionMethods.None, ConnectTimeout = TimeSpan.FromSeconds(5),
        MaxResponseHeadersLength = 16
    };
    private static bool SecretText(string? value, int max) => value is { Length: > 0 } && value.Length <= max && value.All(c => c is >= '!' and <= '~');
    private static bool BearerToken(string? value) => value is { Length: > 0 and <= 16384 } && value.TrimEnd('=') is { Length: > 0 } core &&
        core.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '.' or '_' or '~' or '+' or '/');
    public async Task<MeetingOAuthExchangeOutcome> ExchangeAsync(ClaimsPrincipal actor, Guid academy,
        MeetingOAuthRegistration registration, MeetingOAuthOutcome claim, string? clientSecret, CancellationToken cancellation = default)
    {
        var authority = await readiness.ReadAsync(actor, academy, cancellation);
        if (authority.StatusCode != 200) return new(authority.StatusCode, "AccessDenied");
        var user = await users.GetUserAsync(actor); var material = claim.Material; var flow = registration.Flow;
        if (flow is null) return new(503, "ProviderProfileUnavailable");
        if (claim.StatusCode != 200 || claim.Code != "OAuthClaimOnlyNotConnected" || material is null || user is null ||
            material.Academy != academy || material.Actor != user.Id || material.Registration != registration.Fingerprint ||
            material.Expires is null || material.Expires <= clock.GetUtcNow().UtcDateTime ||
            !MeetingOAuthRegistrationRegistry.ValidRandom(material.Verifier) || !SecretText(claim.AuthorizationCode, 4096) ||
            (flow.RequiresClientSecret ? !SecretText(clientSecret, 4096) : clientSecret is not null)) return new(409, "InvalidExchangeContext");
        async Task<bool> CurrentAuthority()
        {
            if ((await readiness.ReadAsync(actor, academy, cancellation)).StatusCode != 200) return false;
            var current = await users.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == material.Actor, cancellation);
            var roles = current is null ? Array.Empty<string>() : (await users.GetRolesAsync(current)).ToArray();
            return current is not null && current.IsActive && current.AcademyId == academy && !string.IsNullOrWhiteSpace(current.SecurityStamp) &&
                Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(current.SecurityStamp))) == material.SecurityStampHash &&
                (roles.Contains("Owner", StringComparer.OrdinalIgnoreCase) || roles.Contains("AcademyAdmin", StringComparer.OrdinalIgnoreCase));
        }
        if (!await CurrentAuthority()) return new(403, "ExchangeAuthorityChanged");
        var binding = await db.MeetingProviderBindings.AsNoTracking().SingleOrDefaultAsync(x => x.AcademyId == academy && x.Id == material.Binding && x.Provider == registration.Provider, cancellation);
        if (binding is null || binding.Status != "VerificationRequired" || binding.Version != material.BindingVersion || binding.Generation != material.Generation)
            return new(409, "ConnectionGenerationChanged");
        // The code is single-use even after timeout/malformed response. Never
        // resend after uncertain acceptance. Restart/recovery is separately gated.
        if (!material.TryTakeExchange()) return new(409, "ExchangeAlreadyAttempted");
        var fields = new Dictionary<string, string> { ["grant_type"] = "authorization_code", ["client_id"] = registration.ClientId,
            ["redirect_uri"] = registration.Callback.AbsoluteUri, ["code"] = claim.AuthorizationCode!, ["code_verifier"] = material.Verifier! };
        if (flow.RequiresClientSecret && !flow.IsZoomConfidentialProfile) fields["client_secret"] = clientSecret!;
        if (registration.Provider == "Microsoft365") fields["scope"] = string.Join(' ', flow.RequestedScopes);
        using var request = new HttpRequestMessage(HttpMethod.Post, flow.TokenEndpoint) { Content = new FormUrlEncodedContent(fields) };
        if (flow.IsZoomConfidentialProfile) request.Headers.Authorization = new("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes(registration.ClientId + ":" + clientSecret)));
        request.Headers.Accept.ParseAdd("application/json"); request.Headers.CacheControl = new() { NoStore = true };
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellation); deadline.CancelAfter(TimeSpan.FromSeconds(10));
        try
        {
            using var response = await transport.SendAsync(request, deadline.Token);
            if (response.StatusCode != HttpStatusCode.OK) return new(502, "ProviderExchangeRejected", true);
            if (!string.Equals(response.Content.Headers.ContentType?.MediaType, "application/json", StringComparison.OrdinalIgnoreCase) ||
                response.Content.Headers.ContentEncoding.Count != 0 || response.Content.Headers.ContentLength > 65536) return new(502, "InvalidProviderResponse", true);
            using var stream = await response.Content.ReadAsStreamAsync(deadline.Token);
            var buffer = new byte[65537]; var length = 0;
            while (length < buffer.Length)
            {
                var read = await stream.ReadAsync(buffer.AsMemory(length), deadline.Token); if (read == 0) break; length += read;
            }
            if (length > 65536) return new(502, "InvalidProviderResponse", true);
            var parsed = Parse(buffer.AsMemory(0, length), flow, clock.GetUtcNow());
            if (parsed.StatusCode != 200) return parsed;
            if (!await CurrentAuthority()) return new(403, "ExchangeAuthorityChanged", true);
            var currentBinding = await db.MeetingProviderBindings.AsNoTracking().SingleOrDefaultAsync(x => x.AcademyId == academy && x.Id == material.Binding && x.Provider == registration.Provider, deadline.Token);
            if (currentBinding is null || currentBinding.Version != material.BindingVersion || currentBinding.Generation != material.Generation || currentBinding.Status != "VerificationRequired")
                return new(409, "ConnectionGenerationChanged", true);
            return parsed;
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException or IOException or JsonException)
        { return new(502, "ProviderExchangeOutcomeUnknown", true); }
    }

    internal static MeetingOAuthExchangeOutcome Parse(ReadOnlyMemory<byte> json, MeetingOAuthFlowProfile flow, DateTimeOffset now)
    {
        if (json.Length > 65536) return new(502, "InvalidProviderResponse", true);
        try
        {
            using var document = JsonDocument.Parse(json, new() { MaxDepth = 8 }); var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object) return new(502, "InvalidProviderResponse", true);
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var property in root.EnumerateObject())
                if (!names.Add(property.Name) || property.Name.Equals("error", StringComparison.OrdinalIgnoreCase)) return new(502, "InvalidProviderResponse", true);
            string? Text(string name) => root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
            var access = Text("access_token"); var refresh = Text("refresh_token"); var scope = Text("scope");
            if (!BearerToken(access) || !SecretText(refresh, 16384) || !string.Equals(Text("token_type"), "Bearer", StringComparison.OrdinalIgnoreCase) ||
                !root.TryGetProperty("expires_in", out var expiry) || !expiry.TryGetInt32(out var seconds) || seconds is < 60 or > 86400 ||
                scope is not { Length: > 0 and <= 4096 } || scope.Any(c => c is < ' ' or > '~')) return new(502, "InvalidProviderResponse", true);
            var scopes = scope.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(flow.NormalizeScope).ToArray();
            var allowed = flow.RequestedScopes.Select(flow.NormalizeScope).ToHashSet(StringComparer.Ordinal);
            if (scopes.Distinct(StringComparer.Ordinal).Count() != scopes.Length || scopes.Any(s => !allowed.Contains(s)) ||
                flow.RequiredTokenScopes.Any(s => !scopes.Contains(s, StringComparer.Ordinal))) return new(502, "ProviderScopesNotAccepted", true);
            // Ignore unrecognized non-authoritative fields, but never follow Zoom
            // api_url to an unreviewed origin or decode id_token as host authority.
            if (root.TryGetProperty("api_url", out var apiUrl) && (apiUrl.ValueKind != JsonValueKind.String || apiUrl.GetString() != "https://api.zoom.us" || !flow.IsZoomProfile))
                return new(502, "InvalidProviderResponse", true);
            var identity = Text("id_token");
            if (root.TryGetProperty("id_token", out _) && !ValidIdentityToken(identity)) return new(502, "InvalidProviderResponse", true);
            // Quarantine only; no decoded claim is authority. Other providers' ID
            // tokens are not promoted through the Google-specific verifier.
            return new(200, "TokensReceivedAccountUnverified", Tokens: new(access!, refresh!, now.AddSeconds(seconds), Array.AsReadOnly(scopes),
                flow.IsGoogleIdentityProfile || flow.IsMicrosoftIdentityProfile ? identity : null));
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or FormatException or ArgumentException)
        { return new(502, "InvalidProviderResponse", true); }
    }
    internal static bool ValidIdentityToken(string? value) => value is { Length: > 0 and <= 16384 } && value.Split('.') is { Length: 3 } parts &&
        parts.All(p => p.Length > 0 && p.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_'));
}
