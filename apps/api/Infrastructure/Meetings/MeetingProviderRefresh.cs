using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace AcademyDesk.Api.Infrastructure.Meetings;

// No DI/public route. Future activation must use the secure handler factory and
// approved server-only client-credential resolver, never chat/body secrets.
internal sealed class MeetingProviderRefresh(MeetingOAuthAttemptStore store, HttpMessageInvoker transport, TimeProvider clock)
{
    public async Task<MeetingRefreshOutcome> ExecuteAsync(ClaimsPrincipal actor, Guid academy, MeetingOAuthRegistration registration,
        MeetingOAuthExchangePermit permit, int expectedVersion, string? clientSecret, CancellationToken token = default)
    {
        if (!MeetingOAuthAttemptStore.SupportsRefresh(registration)) return new(503, "ProviderRefreshUnqualified");
        if (clientSecret is not { Length: > 0 and <= 4096 } || clientSecret.Any(c => c is < '!' or > '~')) return new(503, "RefreshClientCredentialUnavailable");
        var start = await store.BeginRefreshAsync(actor, academy, registration, permit, expectedVersion, token);
        return start.StatusCode == 201 && start.Material is not null ? await SendStartedAsync(actor, academy, registration, start.Material, clientSecret, token) : start;
    }
    internal async Task<MeetingRefreshOutcome> SendStartedAsync(ClaimsPrincipal actor, Guid academy, MeetingOAuthRegistration registration,
        MeetingRefreshMaterial material, string clientSecret, CancellationToken token = default)
    {
        if (clientSecret is not { Length: > 0 and <= 4096 } || clientSecret.Any(c => c is < '!' or > '~') ||
            !await store.MaySendRefreshAsync(actor, academy, registration, material, token) || !material.TryTake()) return new(409, "RefreshSendUnavailable");
        var fields = new Dictionary<string, string> { ["grant_type"] = "refresh_token", ["client_id"] = registration.ClientId, ["refresh_token"] = material.RefreshToken };
        if (!registration.Flow!.IsZoomConfidentialProfile) fields["client_secret"] = clientSecret;
        if (registration.Provider == "Microsoft365") fields["scope"] = string.Join(' ', material.Scopes.Select(s => s == "offline_access" ? s : "https://graph.microsoft.com/" + s));
        using var request = new HttpRequestMessage(HttpMethod.Post, registration.Flow!.TokenEndpoint) { Content = new FormUrlEncodedContent(fields) };
        if (registration.Flow.IsZoomConfidentialProfile) request.Headers.Authorization = new("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes(registration.ClientId + ":" + clientSecret)));
        request.Headers.CacheControl = new() { NoStore = true }; request.Headers.Accept.ParseAdd("application/json");
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token); deadline.CancelAfter(TimeSpan.FromSeconds(10));
        var outcome = new MeetingOAuthExchangeOutcome(502, "RefreshOutcomeUnknown", true);
        try
        {
            using var response = await transport.SendAsync(request, deadline.Token);
            if (response.StatusCode == HttpStatusCode.OK && string.Equals(response.Content.Headers.ContentType?.MediaType, "application/json", StringComparison.OrdinalIgnoreCase) &&
                response.Content.Headers.ContentEncoding.Count == 0 && (response.Content.Headers.ContentLength is null || response.Content.Headers.ContentLength <= 65536))
            {
                using var stream = await response.Content.ReadAsStreamAsync(deadline.Token); var buffer = new byte[65537]; var length = 0;
                while (length < buffer.Length) { var n = await stream.ReadAsync(buffer.AsMemory(length), deadline.Token); if (n == 0) break; length += n; }
                outcome = Parse(buffer.AsMemory(0, length), registration, material, clock.GetUtcNow());
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException or IOException or JsonException) { }
        return await store.FinishRefreshAsync(actor, academy, registration, material, outcome, token);
    }
    internal static MeetingOAuthExchangeOutcome Parse(ReadOnlyMemory<byte> json, MeetingOAuthRegistration registration, MeetingRefreshMaterial source, DateTimeOffset now)
    {
        MeetingOAuthExchangeOutcome Invalid() => new(502, "InvalidRefreshResponse", true);
        if (json.Length > 65536 || !MeetingOAuthAttemptStore.SupportsRefresh(registration)) return Invalid();
        try
        {
            using var doc = JsonDocument.Parse(json, new() { MaxDepth = 8 }); var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object) return Invalid();
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase); var fields = new Dictionary<string, object?>();
            foreach (var p in root.EnumerateObject()) { if (!names.Add(p.Name) || p.Name.Equals("error", StringComparison.OrdinalIgnoreCase)) return Invalid(); fields[p.Name] = p.Value.Clone(); }
            // Google normally omits a replacement refresh token. Retain only the
            // exact encrypted source token, never client/model-supplied fallback.
            if (!names.Contains("refresh_token")) { if (registration.Provider != "GoogleWorkspace") return Invalid(); fields["refresh_token"] = source.RefreshToken; }
            // OAuth omitted scope means unchanged grant; inherit only verified
            // stored scopes. Explicit null/empty/invalid scope never falls back.
            if (!names.Contains("scope")) fields["scope"] = string.Join(' ', source.Scopes);
            var parsed = MeetingOAuthCodeExchange.Parse(JsonSerializer.SerializeToUtf8Bytes(fields), registration.Flow!, now);
            if (parsed.Tokens is null || parsed.Tokens.Scopes.Any(s => !source.Scopes.Contains(s, StringComparer.Ordinal)) ||
                ((registration.Provider == "Microsoft365" || registration.Flow!.IsZoomConfidentialProfile) && parsed.Tokens.RefreshToken == source.RefreshToken)) return Invalid();
            return parsed;
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or ArgumentException) { return Invalid(); }
    }
}
