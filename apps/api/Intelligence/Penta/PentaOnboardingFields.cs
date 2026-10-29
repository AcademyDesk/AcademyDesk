using System.Text.Json;
using System.Text.Json.Nodes;
using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Infrastructure;

namespace AcademyDesk.Api.Intelligence.Penta;

/// <summary>Complete non-credential onboarding request surface; never a role/security tool.</summary>
public static class PentaOnboardingFields
{
    private static readonly IReadOnlyDictionary<string, (string Kind, int Maximum)> Fields =
        new Dictionary<string, (string, int)>(StringComparer.Ordinal) {
            ["StudentFirstName"]=("text",120), ["StudentLastName"]=("text",120), ["DateOfBirth"]=("date",10),
            ["StudentEmail"]=("text",320), ["StudentPhone"]=("text",30), ["StudentAddressLine1"]=("text",240),
            ["StudentCity"]=("text",100), ["BranchId"]=("reference",36), ["ParentFirstName"]=("text",120),
            ["ParentLastName"]=("text",120), ["ParentEmail"]=("text",320), ["ParentPhone"]=("text",30),
            ["ParentAddressLine1"]=("text",240), ["ParentCity"]=("text",100), ["Relationship"]=("text",50),
            ["StudentNumber"]=("text",50), ["PreferredName"]=("text",120), ["Gender"]=("text",50),
            ["AdmissionDate"]=("date",10), ["StudentState"]=("text",100), ["StudentPostalCode"]=("text",30),
            // Shared protocol bounds text values to2000; Manual retains its4000-character note field.
            ["EmergencyContactName"]=("text",160), ["EmergencyContactPhone"]=("text",30), ["MedicalOrAccessibilityNotes"]=("text",2000),
            ["AllowParentPortalAccess"]=("boolean",0), ["AllowAcademicProgress"]=("boolean",0), ["AllowFinance"]=("boolean",0),
            ["AllowDocuments"]=("boolean",0), ["AllowLeave"]=("boolean",0)
        };
    public static IReadOnlyList<string> Names => Array.AsReadOnly(Fields.Keys.ToArray());
    internal static string NormalizeForCore(string payload, DateOnly today)
    {
        if (!TryRead(payload, out var request)) throw new ArgumentException("Invalid onboarding draft.");
        var root = JsonNode.Parse(payload)!.AsObject(); var fields = root["fields"]!.AsObject();
        foreach (var key in fields.Select(x => x.Key).ToArray()) {
            var value = fields[key]!;
            if (value["kind"]!.GetValue<string>() != "text") continue;
            var text = value["value"]!.GetValue<string>().Trim();
            if (text.Length == 0) fields.Remove(key); else value["value"] = text;
        }
        fields.TryAdd("AdmissionDate", JsonSerializer.SerializeToNode(new { kind = "date", value = today.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture) }));
        var required = StudentOnboardingRules.RequiredFields(request!, today);
        if (required.Contains("ParentFirstName")) fields.TryAdd("Relationship", JsonSerializer.SerializeToNode(new { kind = "text", value = "Parent" }));
        // Surface the same existing guardian configuration defaults before review.
        // Minor portal access is mandatory in the shared domain service.
        var minor = required.Contains("ParentEmail");
        fields["AllowParentPortalAccess"] = JsonSerializer.SerializeToNode(new { kind = "boolean", value = minor || request!.AllowParentPortalAccess });
        foreach (var (key, value) in new[] { ("AllowAcademicProgress", request!.AllowAcademicProgress), ("AllowFinance", request.AllowFinance),
            ("AllowDocuments", request.AllowDocuments), ("AllowLeave", request.AllowLeave) })
            fields[key] = JsonSerializer.SerializeToNode(new { kind = "boolean", value });
        var canonical = PentaOperationalPayload.Canonicalize(root.ToJsonString());
        if (!TryRead(canonical, out _)) throw new ArgumentException("Invalid onboarding draft.");
        return canonical;
    }
    // Shared candidate.5 FieldSpec, derived from the actual host request parser.
    // Descriptors are data; only authenticated composition may publish a catalog.
    internal static object[] Describe(string payload, DateOnly today, IReadOnlyList<PentaOnboardingBranchChoice> branches)
    {
        if (!TryRead(payload, out var request)) throw new ArgumentException("Invalid onboarding draft.");
        var required = StudentOnboardingRules.RequiredFields(request!, today);
        return Fields.Select(field => (object)new {
            key = field.Key, kind = field.Value.Kind,
            label = System.Text.RegularExpressions.Regex.Replace(field.Key, "(?<=[a-z])(?=[A-Z])", " "),
            required = required.Contains(field.Key), clearable = !required.Contains(field.Key),
            choices = field.Value.Kind == "reference" ? branches : Array.Empty<PentaOnboardingBranchChoice>(),
            columns = Array.Empty<object>(), max_length = Math.Max(1, field.Value.Maximum)
        }).ToArray();
    }
    public static bool TryRead(string? payload, out StudentOnboardingRequest? request)
    {
        request = null;
        if (payload is null || payload.Length > 6000) return false;
        try
        {
            var canonical = PentaOperationalPayload.Canonicalize(payload);
            if (canonical.Length > 6000) return false;
            using var doc = JsonDocument.Parse(canonical);
            if (doc.RootElement.GetProperty("tool_key").GetString() != PentaOnboardingDraftService.Tool ||
                doc.RootElement.GetProperty("tool_revision").GetInt64() != 1) return false;
            var values = new Dictionary<string, object?>(StringComparer.Ordinal);
            foreach (var item in doc.RootElement.GetProperty("fields").EnumerateObject())
            {
                if (!Fields.TryGetValue(item.Name, out var definition) || item.Value.GetProperty("kind").GetString() != definition.Kind) return false;
                var value = item.Value.GetProperty("value");
                if (definition.Kind == "boolean") { values[item.Name] = value.GetBoolean(); continue; }
                var text = value.GetString()!;
                if (text.Length > definition.Maximum || text.Any(c => char.IsControl(c) && c is not '\n' and not '\r' and not '\t')) return false;
                if (definition.Kind == "reference" && (!Guid.TryParseExact(text, "D", out var id) || id == Guid.Empty)) return false;
                values[item.Name] = text;
            }
            request = JsonSerializer.Deserialize<StudentOnboardingRequest>(JsonSerializer.Serialize(values));
            return request is not null;
        }
        catch (Exception ex) when (ex is ArgumentException or JsonException or InvalidOperationException or FormatException) { return false; }
    }
    public static bool Ready(string payload, DateOnly today) => TryRead(payload, out var request) &&
        StudentOnboardingRules.Validate(request!, today) is null;
}
