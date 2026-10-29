using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace AcademyDesk.Api.Intelligence.Penta;

// Response-reported source identity, NOT signed attestation, model provenance,
// deployment approval, authorization or a permission to execute any tool.
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record MiniRuntimeIdentity(
    [property: JsonRequired, JsonPropertyName("service_version")] string ServiceVersion,
    [property: JsonRequired, JsonPropertyName("prompt_version")] string PromptVersion,
    [property: JsonRequired, JsonPropertyName("protocol_version")] string ProtocolVersion,
    [property: JsonRequired, JsonPropertyName("api_sha256")] string ApiSha256,
    [property: JsonRequired, JsonPropertyName("version_sha256")] string VersionSha256,
    [property: JsonRequired, JsonPropertyName("orchestrator_sha256")] string OrchestratorSha256,
    [property: JsonRequired, JsonPropertyName("policy_sha256")] string PolicySha256,
    [property: JsonRequired, JsonPropertyName("provider_sha256")] string ProviderSha256,
    [property: JsonRequired, JsonPropertyName("gate_sha256")] string GateSha256)
{
    public const string HeaderName = "X-Penta-Mini-Runtime";
    public static MiniRuntimeIdentity? FromHeaders(HttpResponseHeaders headers)
    {
        if (!headers.TryGetValues(HeaderName, out var values)) return null;
        var items = values.Take(2).ToArray();
        if (items.Length != 1 || items[0].Length is 0 or > 1536 || items[0].Any(c => c < 32 || c > 126)) return null;
        try
        {
            using var doc = JsonDocument.Parse(items[0], new JsonDocumentOptions { MaxDepth = 2 });
            if (doc.RootElement.ValueKind != JsonValueKind.Object) return null;
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in doc.RootElement.EnumerateObject())
                if (!names.Add(property.Name) || property.Value.ValueKind != JsonValueKind.String) return null;
            var identity = doc.RootElement.Deserialize<MiniRuntimeIdentity>();
            return Valid(identity) ? identity : null;
        }
        catch (JsonException) { return null; }
    }
    public static bool Valid(MiniRuntimeIdentity? identity) => identity is not null &&
        Match(identity.ServiceVersion, @"\A[0-9]{1,4}\.[0-9]{1,4}\.[0-9]{1,4}\z") &&
        Match(identity.PromptVersion, @"\Aplanner-[0-9]{1,4}\.[0-9]{1,4}\z") &&
        identity.ProtocolVersion == PentaMiniTools.ProtocolVersion &&
        new[] { identity.ApiSha256, identity.VersionSha256, identity.OrchestratorSha256,
            identity.PolicySha256, identity.ProviderSha256, identity.GateSha256 }.All(hash => Match(hash, @"\A[0-9a-f]{64}\z"));
    private static bool Match(string? value, string pattern) => value is not null &&
        Regex.IsMatch(value, pattern, RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));
}
