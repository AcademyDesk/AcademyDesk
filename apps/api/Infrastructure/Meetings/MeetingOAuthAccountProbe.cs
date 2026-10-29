using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AcademyDesk.Api.Infrastructure.Meetings;

// Private provider observation, never host identity, academy ownership or a
// Connected/meeting-entitlement proof. No routes or production DI registration.
internal sealed class MeetingOAuthAccountObservation(string subject, string? account, DateTimeOffset observedAt)
{
    [JsonIgnore] public string Subject { get; } = subject;
    [JsonIgnore] public string? Account { get; } = account;
    [JsonIgnore] public DateTimeOffset ObservedAt { get; } = observedAt;
    public override string ToString() => "[Provider account observation redacted; not connected]";
}
internal sealed record MeetingOAuthAccountOutcome(int StatusCode, string Code,
    [property: JsonIgnore] MeetingOAuthAccountObservation? Observation = null)
{
    public bool Connected => false;
    public bool CanExecute => false;
    public override string ToString() => $"MeetingOAuthAccountOutcome {{ StatusCode = {StatusCode}, Code = {Code}, Connected = false }}";
}

internal sealed class MeetingOAuthAccountProbe(MeetingOAuthAttemptStore store, HttpMessageInvoker transport, TimeProvider clock)
{
    public async Task<MeetingAccountReviewOutcome> PrepareReviewAsync(ClaimsPrincipal actor, Guid academy,
        MeetingOAuthRegistration registration, MeetingOAuthExchangePermit permit, CancellationToken cancellation = default)
    {
        var inspected = await InspectAsync(actor, academy, registration, permit, cancellation);
        return inspected.StatusCode == 200 && inspected.Observation is { } observed
            ? await store.PrepareAccountReviewAsync(actor, academy, registration, permit, observed, cancellation)
            : new(inspected.StatusCode, inspected.Code);
    }
    public async Task<MeetingOAuthAccountOutcome> InspectAsync(ClaimsPrincipal actor, Guid academy,
        MeetingOAuthRegistration registration, MeetingOAuthExchangePermit permit, CancellationToken cancellation = default)
    {
        if (registration.Flow is not { } flow) return new(503, "ProviderProfileUnavailable");
        // This commits the required read audit and fences current host authority
        // before any credential can reach the fixed provider endpoint.
        var read = await store.ReadQuarantinedTokensAsync(actor, academy, registration, permit, cancellation);
        if (read.StatusCode != 200 || read.Tokens is not { } tokens) return new(read.StatusCode, read.Code);
        using var request = new HttpRequestMessage(HttpMethod.Get, flow.AccountEndpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
        request.Headers.Accept.ParseAdd("application/json"); request.Headers.CacheControl = new() { NoStore = true };
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellation); deadline.CancelAfter(TimeSpan.FromSeconds(10));
        try
        {
            // Use MeetingOAuthCodeExchange.CreateSecureHandler in any future
            // activation: no redirects/proxy/cookies and normal TLS validation.
            using var response = await transport.SendAsync(request, deadline.Token);
            if (response.StatusCode != HttpStatusCode.OK) return new(502, "ProviderAccountUnavailable");
            if (!string.Equals(response.Content.Headers.ContentType?.MediaType, "application/json", StringComparison.OrdinalIgnoreCase) ||
                response.Content.Headers.ContentEncoding.Count != 0 || response.Content.Headers.ContentLength > 65536) return new(502, "InvalidProviderAccountResponse");
            using var stream = await response.Content.ReadAsStreamAsync(deadline.Token);
            var buffer = new byte[65537]; var length = 0;
            while (length < buffer.Length)
            {
                var count = await stream.ReadAsync(buffer.AsMemory(length), deadline.Token); if (count == 0) break; length += count;
            }
            var parsed = Parse(buffer.AsMemory(0, length), registration, clock.GetUtcNow());
            if (parsed.StatusCode != 200) return parsed;
            // A response arriving after revocation/reconnect/token expiry cannot
            // release provider identity. Audit failure here also releases none.
            var current = await store.ReadQuarantinedTokensAsync(actor, academy, registration, permit, deadline.Token);
            if (current.StatusCode != 200 || current.Tokens is null || current.Tokens.AccessToken != tokens.AccessToken)
                return new(current.StatusCode == 200 ? 409 : current.StatusCode, "AccountContextChanged");
            return parsed;
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException or IOException or JsonException)
        { return new(502, "ProviderAccountUnavailable"); }
    }

    internal static MeetingOAuthAccountOutcome Parse(ReadOnlyMemory<byte> json, MeetingOAuthRegistration registration, DateTimeOffset now)
    {
        MeetingOAuthAccountOutcome Invalid() => new(502, "InvalidProviderAccountResponse");
        if (json.Length > 65536 || registration.Flow is null) return Invalid();
        try
        {
            using var document = JsonDocument.Parse(json, new() { MaxDepth = 8 }); var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object) return Invalid();
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var property in root.EnumerateObject())
                if (!names.Add(property.Name) || property.Name.Equals("error", StringComparison.OrdinalIgnoreCase)) return Invalid();
            string? Text(string name) => root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
            bool Id(string? value, int limit) => value is { Length: > 0 } && value.Length <= limit && value.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '.' or '~');
            string? subject; string? account = null;
            switch (registration.Provider)
            {
                case "GoogleWorkspace":
                    subject = Text("sub");
                    // Never use email/domain or an unvalidated id_token as identity.
                    if (!Id(subject, 255) || !root.TryGetProperty("email_verified", out var verified) || verified.ValueKind != JsonValueKind.True) return Invalid();
                    break;
                case "Microsoft365":
                    subject = Text("id");
                    if (!Guid.TryParseExact(subject, "D", out var id) || id == Guid.Empty || subject != id.ToString("D")) return Invalid();
                    // DirectoryTenant is a configured routing restriction, NOT a
                    // tenant claim from /me. Issuer/directory qualification remains open.
                    break;
                case "Zoom":
                    subject = Text("id"); account = Text("account_id");
                    if (!Id(subject, 255) || !Id(account, 255) || Text("status") != "active") return Invalid();
                    break;
                default: return Invalid();
            }
            return new(200, "ProviderAccountObservedNotConnected", new(subject!, account, now));
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or FormatException or ArgumentException) { return Invalid(); }
    }
}
