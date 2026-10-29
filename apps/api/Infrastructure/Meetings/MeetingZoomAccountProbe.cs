using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace AcademyDesk.Api.Infrastructure.Meetings;

internal sealed record MeetingZoomAccountOutcome(int StatusCode, string Code, bool AccountMatched = false, bool MeetingPlanObserved = false)
{
    public bool ProviderVerified => false;
    public bool Connected => false;
    public bool CanExecute => false;
    public bool CanRetry => false;
}

// Private read-only candidate, not registered in DI or exposed through a route.
// Only the current OAuth user, never arbitrary users/accounts or account-wide
// settings. Expected IDs come from reviewed host policy, never chat/client data.
internal sealed class MeetingZoomAccountProbe(HttpMessageInvoker transport)
{
    internal const string Endpoint = "https://api.zoom.us/v2/users/me";
    private static bool Id(string? value) => value is { Length: > 0 and <= 255 } && value.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '.' or '~');
    internal async Task<MeetingZoomAccountOutcome> InspectAsync(string access, string subject, string account, CancellationToken cancellation = default)
    {
        if (!Id(subject) || !Id(account) || access is not { Length: > 0 and <= 16384 } || access.Any(c => c is < '!' or > '~')) return new(409, "ZoomAccountContextUnavailable");
        using var request = new HttpRequestMessage(HttpMethod.Get, Endpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", access); request.Headers.Accept.ParseAdd("application/json"); request.Headers.CacheControl = new() { NoStore = true };
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellation); deadline.CancelAfter(TimeSpan.FromSeconds(10));
        try
        {
            // Future activation must use the existing secure handler: normal TLS,
            // no redirects/proxy/cookies. Never retry, follow links or expand scope.
            using var response = await transport.SendAsync(request, deadline.Token);
            if (response.StatusCode != HttpStatusCode.OK) return new(502, "ZoomAccountEvidenceUnavailable");
            if (!string.Equals(response.Content.Headers.ContentType?.MediaType, "application/json", StringComparison.OrdinalIgnoreCase) ||
                response.Content.Headers.ContentEncoding.Count != 0 || response.Content.Headers.ContentLength > 65536) return new(502, "InvalidZoomAccountEvidence");
            using var stream = await response.Content.ReadAsStreamAsync(deadline.Token); var buffer = new byte[65537]; var length = 0;
            while (length < buffer.Length) { var count = await stream.ReadAsync(buffer.AsMemory(length), deadline.Token); if (count == 0) break; length += count; }
            return Parse(buffer.AsMemory(0, length), subject, account);
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException or IOException or JsonException or ArgumentException)
        { return new(502, "ZoomAccountEvidenceUnavailable"); }
    }
    internal static MeetingZoomAccountOutcome Parse(ReadOnlyMemory<byte> json, string subject, string account)
    {
        MeetingZoomAccountOutcome Invalid() => new(502, "InvalidZoomAccountEvidence");
        bool Unique(JsonElement value) => value.ValueKind switch {
            JsonValueKind.Object => value.EnumerateObject().Select(p => p.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count() == value.EnumerateObject().Count() && value.EnumerateObject().All(p => Unique(p.Value)),
            JsonValueKind.Array => value.EnumerateArray().All(Unique), _ => true };
        if (json.Length > 65536 || !Id(subject) || !Id(account)) return Invalid();
        try
        {
            using var document = JsonDocument.Parse(json, new() { MaxDepth = 8 }); var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object || !Unique(root) || root.EnumerateObject().Any(p => p.Name.Equals("error", StringComparison.OrdinalIgnoreCase) ||
                p.Name.Equals("code", StringComparison.OrdinalIgnoreCase) || p.Name.Equals("message", StringComparison.OrdinalIgnoreCase) ||
                p.Name.Equals("next_page_token", StringComparison.OrdinalIgnoreCase) || p.Name.Equals("next", StringComparison.OrdinalIgnoreCase) || p.Name.Equals("@odata.nextLink", StringComparison.OrdinalIgnoreCase))) return Invalid();
            string? Text(string name) => root.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
            var userId = Text("id"); var accountId = Text("account_id"); var status = Text("status");
            if (!Id(userId) || !Id(accountId) || status is not ("active" or "inactive" or "pending") || !root.TryGetProperty("type", out var type) ||
                type.ValueKind != JsonValueKind.Number || !type.TryGetInt32(out var plan) || plan is not (1 or 2 or 4 or 99)) return Invalid();
            if (userId != subject || accountId != account) return new(409, "ZoomAccountPolicyMismatch");
            if (status != "active") return new(409, "ZoomAccountNotActive");
            // Basic (1) is not incorrectly treated as unlicensed/unusable. Both
            // Basic and Licensed (2) still need real meeting-specific qualification;
            // do not infer duration, capacity, consent or host/admin rights here.
            return plan is 1 or 2 ? new(200, "ZoomMeetingPlanObservedNotQualified", true, true) : new(409, "ZoomMeetingPlanNotObserved", true);
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or ArgumentException) { return Invalid(); }
    }
}
