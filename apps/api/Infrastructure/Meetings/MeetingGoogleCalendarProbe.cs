using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AcademyDesk.Api.Infrastructure.Meetings;

internal sealed record MeetingGoogleCalendarOutcome(int StatusCode, string Code, bool MetadataReadAuthorized = false, bool MeetSupported = false,
    [property: JsonIgnore] string? CalendarFingerprint = null)
{
    public bool ProviderVerified => false;
    public bool Connected => false;
    public bool CanExecute => false;
    public bool CanRetry => false;
    public override string ToString() => $"[Google calendar capability {StatusCode}; not connected]";
}

// Fixed read-only metadata endpoint. No caller-supplied URL/calendar/query, event
// enumeration or write. Not registered in DI/routes/tools. Future activation must
// use MeetingOAuthCodeExchange.CreateSecureHandler (normal TLS, no redirect/proxy).
internal sealed class MeetingGoogleCalendarProbe(HttpMessageInvoker transport)
{
    internal const string Endpoint = "https://www.googleapis.com/calendar/v3/calendars/primary?fields=kind%2Cid%2CconferenceProperties";
    internal async Task<MeetingGoogleCalendarOutcome> InspectAsync(string accessToken, CancellationToken cancellation = default)
    {
        if (accessToken is not { Length: > 0 and <= 16384 } || accessToken.Any(c => c is < '!' or > '~')) return new(409, "GoogleCalendarCredentialUnavailable");
        using var request = new HttpRequestMessage(HttpMethod.Get, Endpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Accept.ParseAdd("application/json"); request.Headers.CacheControl = new() { NoStore = true };
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellation); deadline.CancelAfter(TimeSpan.FromSeconds(10));
        try
        {
            using var response = await transport.SendAsync(request, deadline.Token);
            if (response.StatusCode != HttpStatusCode.OK) return new(502, "GoogleCalendarMetadataUnavailable");
            if (!string.Equals(response.Content.Headers.ContentType?.MediaType, "application/json", StringComparison.OrdinalIgnoreCase) ||
                response.Content.Headers.ContentEncoding.Count != 0 || response.Content.Headers.ContentLength > 32768) return new(502, "InvalidGoogleCalendarMetadata");
            using var stream = await response.Content.ReadAsStreamAsync(deadline.Token); var buffer = new byte[32769]; var length = 0;
            while (length < buffer.Length) { var count = await stream.ReadAsync(buffer.AsMemory(length), deadline.Token); if (count == 0) break; length += count; }
            return Parse(buffer.AsMemory(0, length));
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException or IOException or JsonException or ArgumentException)
        { return new(502, "GoogleCalendarMetadataUnavailable"); } // No raw response/token/error logging.
    }
    internal static MeetingGoogleCalendarOutcome Parse(ReadOnlyMemory<byte> json)
    {
        MeetingGoogleCalendarOutcome Invalid() => new(502, "InvalidGoogleCalendarMetadata");
        bool Unique(JsonElement value) => value.ValueKind == JsonValueKind.Object && value.EnumerateObject().Select(x => x.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count() == value.EnumerateObject().Count();
        string? Text(JsonElement value, string key) => value.TryGetProperty(key, out var p) && p.ValueKind == JsonValueKind.String ? p.GetString() : null;
        if (json.Length > 32768) return Invalid();
        try
        {
            using var document = JsonDocument.Parse(json, new() { MaxDepth = 6 }); var root = document.RootElement;
            if (!Unique(root) || root.TryGetProperty("error", out _) || Text(root, "kind") != "calendar#calendar" ||
                Text(root, "id") is not { Length: > 0 and <= 1024 } calendar || calendar.Any(c => c is < '!' or > '~')) return Invalid();
            if (!root.TryGetProperty("conferenceProperties", out var properties)) return new(409, "GoogleMeetNotAdvertised", true);
            if (!Unique(properties)) return Invalid();
            if (!properties.TryGetProperty("allowedConferenceSolutionTypes", out var solutions)) return new(409, "GoogleMeetNotAdvertised", true);
            if (solutions.ValueKind != JsonValueKind.Array || solutions.GetArrayLength() > 16) return Invalid();
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var solution in solutions.EnumerateArray())
                if (solution.ValueKind != JsonValueKind.String || solution.GetString() is not { Length: > 0 and <= 64 } name ||
                    !name.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_') || !names.Add(name)) return Invalid();
            if (!names.Contains("hangoutsMeet")) return new(409, "GoogleMeetNotAdvertised", true);
            var fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new[] { "google-primary-calendar/1", calendar }))));
            return new(200, "GoogleMeetAdvertisedNotQualified", true, true, fingerprint);
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or ArgumentException) { return Invalid(); }
    }
}
