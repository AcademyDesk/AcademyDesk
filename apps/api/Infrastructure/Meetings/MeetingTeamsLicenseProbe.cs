using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace AcademyDesk.Api.Infrastructure.Meetings;

internal sealed record MeetingTeamsLicenseOutcome(int StatusCode, string Code, bool LicenseReadAuthorized = false, bool TeamsPlanProvisioned = false)
{
    public bool ProviderVerified => false;
    public bool Connected => false;
    public bool CanExecute => false;
    public bool CanRetry => false;
}

// Optional inactive observation with EXISTING delegated User.Read only. Graph
// additionally requires a supported directory role. A denial is unavailable
// evidence, never an instruction to grant a role/scope or bypass provider policy.
internal sealed class MeetingTeamsLicenseProbe(HttpMessageInvoker transport)
{
    internal const string Endpoint = "https://graph.microsoft.com/v1.0/me/licenseDetails?$select=servicePlans";
    internal const string Catalog = "teams-service-plan/1";
    // Reviewed Microsoft service-plan reference, 2026-10-16. Narrow recognition,
    // NOT an exhaustive Teams license list or an organizer/write entitlement.
    internal const string TeamsPlan = "57ff2da0-773e-42df-b2af-ffb7a2317929";
    internal async Task<MeetingTeamsLicenseOutcome> InspectAsync(string access, CancellationToken cancellation = default)
    {
        if (access is not { Length: > 0 and <= 16384 } || access.Any(c => c is < '!' or > '~')) return new(409, "TeamsLicenseCredentialUnavailable");
        using var request = new HttpRequestMessage(HttpMethod.Get, Endpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", access); request.Headers.Accept.ParseAdd("application/json"); request.Headers.CacheControl = new() { NoStore = true };
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellation); deadline.CancelAfter(TimeSpan.FromSeconds(10));
        try
        {
            using var response = await transport.SendAsync(request, deadline.Token);
            if (response.StatusCode != HttpStatusCode.OK) return new(502, "TeamsLicenseEvidenceUnavailable");
            if (!string.Equals(response.Content.Headers.ContentType?.MediaType, "application/json", StringComparison.OrdinalIgnoreCase) ||
                response.Content.Headers.ContentEncoding.Count != 0 || response.Content.Headers.ContentLength > 131072) return new(502, "InvalidTeamsLicenseEvidence");
            using var stream = await response.Content.ReadAsStreamAsync(deadline.Token); var buffer = new byte[131073]; var length = 0;
            while (length < buffer.Length) { var count = await stream.ReadAsync(buffer.AsMemory(length), deadline.Token); if (count == 0) break; length += count; }
            return Parse(buffer.AsMemory(0, length));
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException or IOException or JsonException or ArgumentException)
        { return new(502, "TeamsLicenseEvidenceUnavailable"); } // Never expose provider errors/token/SKUs.
    }
    internal static MeetingTeamsLicenseOutcome Parse(ReadOnlyMemory<byte> json)
    {
        MeetingTeamsLicenseOutcome Invalid() => new(502, "InvalidTeamsLicenseEvidence");
        bool Unique(JsonElement value) => value.ValueKind == JsonValueKind.Object && value.EnumerateObject().Select(p => p.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count() == value.EnumerateObject().Count();
        string? Text(JsonElement value, string name) => value.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.String ? p.GetString() : null;
        if (json.Length > 131072) return Invalid();
        try
        {
            using var document = JsonDocument.Parse(json, new() { MaxDepth = 8 }); var root = document.RootElement;
            if (!Unique(root) || root.EnumerateObject().Any(p => p.Name.Equals("error", StringComparison.OrdinalIgnoreCase) || p.Name.Equals("@odata.nextLink", StringComparison.OrdinalIgnoreCase) || p.Name.Equals("@odata.deltaLink", StringComparison.OrdinalIgnoreCase)) ||
                !root.TryGetProperty("value", out var licenses) || licenses.ValueKind != JsonValueKind.Array || licenses.GetArrayLength() > 128) return Invalid();
            var provisioned = false; var total = 0;
            foreach (var license in licenses.EnumerateArray())
            {
                if (!Unique(license) || !license.TryGetProperty("servicePlans", out var plans) || plans.ValueKind != JsonValueKind.Array || plans.GetArrayLength() > 256 || (total += plans.GetArrayLength()) > 1024) return Invalid();
                var seen = new HashSet<Guid>();
                foreach (var plan in plans.EnumerateArray())
                {
                    if (!Unique(plan) || !Guid.TryParseExact(Text(plan, "servicePlanId"), "D", out var id) || id == Guid.Empty || !seen.Add(id) ||
                        Text(plan, "servicePlanId") != id.ToString("D") || Text(plan, "servicePlanName") is not { Length: > 0 and <= 128 } name ||
                        !name.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_') || Text(plan, "appliesTo") is not ("User" or "Company") ||
                        Text(plan, "provisioningStatus") is not ("Success" or "Disabled" or "Error" or "PendingInput" or "PendingActivation" or "PendingProvisioning")) return Invalid();
                    if (id.ToString("D") == TeamsPlan)
                    {
                        if (name != "TEAMS1") return Invalid();
                        provisioned |= Text(plan, "appliesTo") == "User" && Text(plan, "provisioningStatus") == "Success";
                    }
                }
            }
            // Validate the WHOLE response before returning; never accept a known
            // plan from a partial/paginated/malformed document. Unknown IDs never
            // match just because a display name contains 'Teams'.
            return provisioned ? new(200, "TeamsPlanObservedNotQualified", true, true) : new(409, "RecognizedTeamsPlanNotObserved", true);
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or ArgumentException) { return Invalid(); }
    }
}
