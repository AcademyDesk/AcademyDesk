using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace AcademyDesk.Api.Infrastructure.Meetings;

internal sealed record MeetingMicrosoftIdentityOutcome(int StatusCode, string Code, bool IdentityVerified = false, bool DirectoryAccountMatched = false,
    [property: JsonIgnore] DateTimeOffset? Expires = null)
{
    public bool ProviderVerified => false;
    public bool Connected => false;
    public bool CanExecute => false;
    public bool CanRetry => false;
    public override string ToString() => $"[Microsoft identity check {StatusCode}; not connected]";
}

// Inactive private verifier. Keys URL uses only canonical configured tenant, never
// token tid/jku/x5u or runtime model input. Existing secure handler required before
// activation. Key rollout/caching/real-provider qualification remain release gates.
internal sealed class MeetingMicrosoftIdentityVerifier(HttpMessageInvoker transport, TimeProvider clock)
{
    internal const string IssuerTemplate = "https://login.microsoftonline.com/{tenantid}/v2.0";
    internal static string KeysEndpoint(MeetingOAuthRegistration registration) => $"https://login.microsoftonline.com/{registration.Flow!.DirectoryTenant}/discovery/v2.0/keys";
    internal static string Issuer(MeetingOAuthRegistration registration) => $"https://login.microsoftonline.com/{registration.Flow!.DirectoryTenant}/v2.0";
    private static bool Unique(JsonElement value) => value.ValueKind == JsonValueKind.Object && value.EnumerateObject().Select(x => x.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count() == value.EnumerateObject().Count();
    private static string? Text(JsonElement value, string name) => value.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
    private static bool Number(JsonElement value, string name, out long number) { number = 0; return value.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number && v.TryGetInt64(out number); }
    private static bool Id(string? value) => value is { Length: > 0 and <= 255 } && value.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '.' or '~');
    private static bool GuidId(string? value) => Guid.TryParseExact(value, "D", out var id) && id != Guid.Empty && value == id.ToString("D");
    private static byte[] Decode(string value) { var result = WebEncoders.Base64UrlDecode(value); if (WebEncoders.Base64UrlEncode(result) != value) throw new FormatException(); return result; }
    internal async Task<MeetingMicrosoftIdentityOutcome> VerifyAsync(MeetingOAuthRegistration registration, string? jwt, string expectedObject, string expectedNonce, CancellationToken cancellation = default)
    {
        MeetingMicrosoftIdentityOutcome Invalid() => new(502, "InvalidMicrosoftIdentityEvidence");
        if (registration.Provider != "Microsoft365" || registration.Flow?.IsMicrosoftIdentityProfile != true || !GuidId(registration.ClientId) ||
            !GuidId(registration.Flow.DirectoryTenant) || registration.Flow.DirectoryTenant == "9188040d-6c67-4c5b-b112-36a304b66dad") return new(503, "MicrosoftIdentityProfileUnavailable");
        if (!MeetingOAuthCodeExchange.ValidIdentityToken(jwt) || !GuidId(expectedObject) || !MeetingOAuthRegistrationRegistry.ValidRandom(expectedNonce)) return Invalid();
        try
        {
            var parts = jwt!.Split('.'); using var headDoc = JsonDocument.Parse(Decode(parts[0]), new() { MaxDepth = 4 }); using var claimsDoc = JsonDocument.Parse(Decode(parts[1]), new() { MaxDepth = 8 });
            var header = headDoc.RootElement; var claims = claimsDoc.RootElement; var now = clock.GetUtcNow().ToUnixTimeSeconds(); var issuer = Issuer(registration);
            if (!Unique(header) || !Unique(claims) || Text(header, "alg") != "RS256" || Text(header, "typ") != "JWT" || !Id(Text(header, "kid")) ||
                new[] { "jku", "jwk", "x5u", "x5c", "crit", "b64", "zip" }.Any(n => header.TryGetProperty(n, out _)) ||
                Text(claims, "ver") != "2.0" || Text(claims, "iss") != issuer || Text(claims, "tid") != registration.Flow.DirectoryTenant ||
                Text(claims, "aud") != registration.ClientId || (claims.TryGetProperty("azp", out _) && Text(claims, "azp") != registration.ClientId) ||
                Text(claims, "oid") != expectedObject || !Id(Text(claims, "sub")) || Text(claims, "nonce") != expectedNonce ||
                !Number(claims, "iat", out var issued) || !Number(claims, "nbf", out var notBefore) || !Number(claims, "exp", out var expires) ||
                issued < now - 7200 || issued > now + 60 || notBefore > now || expires <= now || expires <= issued || expires - issued > 7200) return Invalid();
            // ID token from /token need not contain at_hash. Do not decode Graph
            // access tokens or infer their audience/permissions; host separately
            // checks this exact encrypted token via Graph /me after this signature.
            using var request = new HttpRequestMessage(HttpMethod.Get, KeysEndpoint(registration)); request.Headers.Accept.ParseAdd("application/json"); request.Headers.CacheControl = new() { NoStore = true };
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellation); deadline.CancelAfter(TimeSpan.FromSeconds(10));
            using var response = await transport.SendAsync(request, deadline.Token);
            if (response.StatusCode != HttpStatusCode.OK || response.Content.Headers.ContentEncoding.Count != 0 || response.Content.Headers.ContentLength > 131072 ||
                response.Content.Headers.ContentType?.MediaType is not ("application/json" or "application/jwk-set+json")) return new(502, "MicrosoftSigningKeysUnavailable");
            using var stream = await response.Content.ReadAsStreamAsync(deadline.Token); var buffer = new byte[131073]; var length = 0;
            while (length < buffer.Length) { var count = await stream.ReadAsync(buffer.AsMemory(length), deadline.Token); if (count == 0) break; length += count; }
            if (length > 131072) return Invalid();
            using var keysDoc = JsonDocument.Parse(buffer.AsMemory(0, length), new() { MaxDepth = 8 }); var root = keysDoc.RootElement;
            if (!Unique(root) || !root.TryGetProperty("keys", out var keys) || keys.ValueKind != JsonValueKind.Array || keys.GetArrayLength() is < 1 or > 32) return Invalid();
            var ids = new HashSet<string>(StringComparer.Ordinal); JsonWebKey? selected = null;
            foreach (var key in keys.EnumerateArray())
            {
                if (!Unique(key) || Text(key, "kty") != "RSA" || Text(key, "use") != "sig" || !Id(Text(key, "kid")) || !ids.Add(Text(key, "kid")!) ||
                    (key.TryGetProperty("alg", out _) && Text(key, "alg") != "RS256") ||
                    new[] { "d", "p", "q", "dp", "dq", "qi", "oth", "k", "jku", "x5u" }.Any(n => key.TryGetProperty(n, out _)) ||
                    (key.TryGetProperty("key_ops", out var operations) && (operations.ValueKind != JsonValueKind.Array || operations.GetArrayLength() != 1 || operations[0].GetString() != "verify"))) return Invalid();
                var modulus = Decode(Text(key, "n") ?? ""); var exponent = Decode(Text(key, "e") ?? "");
                if (modulus.Length is < 256 or > 512 || modulus[0] < 128 || !exponent.SequenceEqual(new byte[] { 1, 0, 1 })) return Invalid();
                if (Text(key, "kid") == Text(header, "kid"))
                {
                    // Ignore provider x5c/x5t metadata; use only vetted n/e public
                    // parameters from the fixed tenant key response. No cert URL.
                    if (Text(key, "issuer") != issuer && Text(key, "issuer") != IssuerTemplate) return Invalid();
                    selected = new JsonWebKey { Kty = "RSA", Use = "sig", Alg = "RS256", Kid = Text(key, "kid"), N = Text(key, "n"), E = Text(key, "e") };
                }
            }
            if (selected is null) return Invalid();
            var validation = new TokenValidationParameters { RequireSignedTokens = true, RequireExpirationTime = true, ValidateIssuerSigningKey = true, IssuerSigningKey = selected,
                ValidAlgorithms = new[] { SecurityAlgorithms.RsaSha256 }, ValidateIssuer = true, ValidIssuer = issuer, ValidateAudience = true, ValidAudience = registration.ClientId,
                ValidateLifetime = true, ClockSkew = TimeSpan.Zero, TryAllIssuerSigningKeys = false,
                LifetimeValidator = (start, end, _, _) => end.HasValue && end.Value > clock.GetUtcNow().UtcDateTime && (!start.HasValue || start.Value <= clock.GetUtcNow().UtcDateTime) };
            var checkedToken = await new JsonWebTokenHandler { MaximumTokenSizeInBytes = 16384 }.ValidateTokenAsync(jwt, validation);
            if (!checkedToken.IsValid || DateTimeOffset.FromUnixTimeSeconds(expires) <= clock.GetUtcNow()) return Invalid();
            return new(200, "MicrosoftSignedIdentityOnlyNotConnected", true, Expires: DateTimeOffset.FromUnixTimeSeconds(expires));
        }
        catch (Exception ex) when (ex is JsonException or FormatException or ArgumentException or InvalidOperationException or CryptographicException or HttpRequestException or IOException or OperationCanceledException)
        { return Invalid(); } // Do not log tokens, raw claims, keys or exceptions.
    }
}
