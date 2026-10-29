using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace AcademyDesk.Api.Infrastructure.Meetings;

internal sealed record MeetingGoogleIdentityOutcome(int StatusCode, string Code, bool IdentityVerified = false,
    [property: JsonIgnore] DateTimeOffset? Expires = null)
{
    public bool ProviderVerified => false;
    public bool Connected => false;
    public bool CanExecute => false;
    public override string ToString() => $"[Google identity check {StatusCode}; not connected]";
}

// Private candidate, not DI/HTTP/model input. Transport must use the existing
// secure handler on activation. Fixed public-key endpoint; NEVER token jku/x5u.
// Existing resolved IdentityModel performs cryptographic signature validation.
internal sealed class MeetingGoogleIdentityVerifier(HttpMessageInvoker transport, TimeProvider clock)
{
    internal const string KeysEndpoint = "https://www.googleapis.com/oauth2/v3/certs";
    private static bool UniqueObject(JsonElement value) => value.ValueKind == JsonValueKind.Object &&
        value.EnumerateObject().Select(p => p.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count() == value.EnumerateObject().Count();
    private static string? Text(JsonElement value, string name) => value.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
    private static bool Integer(JsonElement value, string name, out long number)
    { number = 0; return value.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number && v.TryGetInt64(out number); }
    private static bool Id(string? value) => value is { Length: > 0 and <= 128 } && value.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_');
    private static byte[] Decode(string value)
    {
        var bytes = WebEncoders.Base64UrlDecode(value);
        if (WebEncoders.Base64UrlEncode(bytes) != value) throw new FormatException();
        return bytes;
    }
    internal async Task<MeetingGoogleIdentityOutcome> VerifyAsync(MeetingOAuthRegistration registration, string? identityToken,
        string expectedSubject, string expectedNonce, string accessToken, CancellationToken cancellation = default)
    {
        MeetingGoogleIdentityOutcome Invalid() => new(502, "InvalidGoogleIdentityEvidence");
        if (registration.Provider != "GoogleWorkspace" || registration.Flow?.IsGoogleIdentityProfile != true) return new(503, "GoogleIdentityProfileUnavailable");
        if (!MeetingOAuthCodeExchange.ValidIdentityToken(identityToken) || !MeetingOAuthRegistrationRegistry.ValidRandom(expectedNonce) ||
            string.IsNullOrEmpty(expectedSubject) || expectedSubject.Length > 255 || accessToken is not { Length: > 0 and <= 16384 } || accessToken.Any(c => c is < '!' or > '~')) return Invalid();
        try
        {
            var parts = identityToken!.Split('.');
            using var headerDocument = JsonDocument.Parse(Decode(parts[0]), new() { MaxDepth = 4 });
            using var claimsDocument = JsonDocument.Parse(Decode(parts[1]), new() { MaxDepth = 8 });
            var header = headerDocument.RootElement; var claims = claimsDocument.RootElement;
            if (!UniqueObject(header) || !UniqueObject(claims) || Text(header, "alg") != "RS256" || !Id(Text(header, "kid")) ||
                (header.TryGetProperty("typ", out _) && Text(header, "typ") != "JWT") ||
                new[] { "jku", "jwk", "x5u", "x5c", "crit", "b64", "zip" }.Any(n => header.TryGetProperty(n, out _))) return Invalid();
            var issuer = Text(claims, "iss"); var now = clock.GetUtcNow(); var seconds = now.ToUnixTimeSeconds();
            if (issuer is not ("https://accounts.google.com" or "accounts.google.com") || Text(claims, "aud") != registration.ClientId ||
                (claims.TryGetProperty("azp", out _) && Text(claims, "azp") != registration.ClientId) || Text(claims, "sub") != expectedSubject ||
                Text(claims, "nonce") != expectedNonce || !Integer(claims, "iat", out var issued) || !Integer(claims, "exp", out var expires) ||
                issued < seconds - 7200 || issued > seconds + 60 || expires <= seconds || expires <= issued || expires - issued > 7200 ||
                (claims.TryGetProperty("nbf", out _) && (!Integer(claims, "nbf", out var notBefore) || notBefore > seconds))) return Invalid();
            // Host policy is stricter than a bare login: bind this signed identity
            // to the exact quarantined access token, not merely the same account.
            var accessHash = WebEncoders.Base64UrlEncode(SHA256.HashData(Encoding.ASCII.GetBytes(accessToken)).AsSpan(0, 16).ToArray());
            if (Text(claims, "at_hash") != accessHash) return Invalid();
            using var request = new HttpRequestMessage(HttpMethod.Get, KeysEndpoint);
            request.Headers.Accept.ParseAdd("application/json"); request.Headers.CacheControl = new() { NoStore = true };
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellation); deadline.CancelAfter(TimeSpan.FromSeconds(10));
            using var response = await transport.SendAsync(request, deadline.Token);
            if (response.StatusCode != HttpStatusCode.OK || response.Content.Headers.ContentEncoding.Count != 0 ||
                response.Content.Headers.ContentLength > 65536 || response.Content.Headers.ContentType?.MediaType is not ("application/json" or "application/jwk-set+json"))
                return new(502, "GoogleSigningKeysUnavailable");
            using var stream = await response.Content.ReadAsStreamAsync(deadline.Token);
            var buffer = new byte[65537]; var length = 0;
            while (length < buffer.Length) { var count = await stream.ReadAsync(buffer.AsMemory(length), deadline.Token); if (count == 0) break; length += count; }
            if (length > 65536) return new(502, "GoogleSigningKeysUnavailable");
            using var keysDocument = JsonDocument.Parse(buffer.AsMemory(0, length), new() { MaxDepth = 8 }); var root = keysDocument.RootElement;
            if (!UniqueObject(root) || !root.TryGetProperty("keys", out var keys) || keys.ValueKind != JsonValueKind.Array || keys.GetArrayLength() is < 1 or > 16) return Invalid();
            var ids = new HashSet<string>(StringComparer.Ordinal); JsonWebKey? selected = null;
            foreach (var key in keys.EnumerateArray())
            {
                if (!UniqueObject(key) || Text(key, "kty") != "RSA" || Text(key, "alg") != "RS256" || Text(key, "use") != "sig" ||
                    !Id(Text(key, "kid")) || !ids.Add(Text(key, "kid")!) ||
                    new[] { "d", "p", "q", "dp", "dq", "qi", "oth", "k", "jku", "x5u", "x5c" }.Any(n => key.TryGetProperty(n, out _)) ||
                    (key.TryGetProperty("key_ops", out var operations) && (operations.ValueKind != JsonValueKind.Array || operations.GetArrayLength() != 1 || operations[0].GetString() != "verify"))) return Invalid();
                var modulus = Decode(Text(key, "n") ?? ""); var exponent = Decode(Text(key, "e") ?? "");
                if (modulus.Length is < 256 or > 512 || modulus[0] < 128 || !exponent.SequenceEqual(new byte[] { 1, 0, 1 })) return Invalid();
                if (Text(key, "kid") == Text(header, "kid")) selected = new JsonWebKey(key.GetRawText());
            }
            if (selected is null) return Invalid();
            var parameters = new TokenValidationParameters
            {
                RequireSignedTokens = true, RequireExpirationTime = true, ValidateIssuerSigningKey = true, IssuerSigningKey = selected,
                ValidAlgorithms = new[] { SecurityAlgorithms.RsaSha256 }, ValidateIssuer = true,
                ValidIssuers = new[] { "https://accounts.google.com", "accounts.google.com" }, ValidateAudience = true, ValidAudience = registration.ClientId,
                ValidateLifetime = true, ClockSkew = TimeSpan.Zero, TryAllIssuerSigningKeys = false,
                LifetimeValidator = (start, end, _, _) => end.HasValue && end.Value > clock.GetUtcNow().UtcDateTime && (!start.HasValue || start.Value <= clock.GetUtcNow().UtcDateTime)
            };
            var result = await new JsonWebTokenHandler { MaximumTokenSizeInBytes = 16384 }.ValidateTokenAsync(identityToken, parameters);
            if (!result.IsValid || DateTimeOffset.FromUnixTimeSeconds(expires) <= clock.GetUtcNow()) return Invalid();
            return new(200, "GoogleIdentityVerifiedOnlyNotConnected", true, DateTimeOffset.FromUnixTimeSeconds(expires));
        }
        catch (Exception ex) when (ex is JsonException or FormatException or ArgumentException or InvalidOperationException or CryptographicException or HttpRequestException or IOException or OperationCanceledException)
        { return Invalid(); } // Never log token, claims, keys or validator exception.
    }
}
