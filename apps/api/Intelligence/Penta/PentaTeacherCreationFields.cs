using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using AcademyDesk.Api.Controllers;

namespace AcademyDesk.Api.Intelligence.Penta;

/// <summary>
/// Inactive operational-0.3 teacher field adapter only. Parsing never grants
/// authority, prepares an approval, dispatches a service or proves a duplicate
/// check. No catalog/HTTP/DI/model registration is added by this adapter.
/// Complex availability/certification/compensation and credentials stay Manual
/// until dedicated reviewed workflows exist; never silently drop model fields.
/// </summary>
public static class PentaTeacherCreationFields
{
    public const string Tool = "AcademyDesk.Teacher.Create";
    private static readonly IReadOnlyDictionary<string, (string Kind, int Maximum)> Fields =
        new Dictionary<string, (string, int)>(StringComparer.Ordinal) {
            ["FirstName"] = ("text",120), ["LastName"] = ("text",120), ["Email"] = ("text",320),
            ["Phone"] = ("text",30), ["Specialties"] = ("text",500), ["Qualifications"] = ("text",1000),
            ["EmploymentType"] = ("text",50), ["DateOfBirth"] = ("date",10), ["JoiningDate"] = ("date",10),
            ["AddressLine1"] = ("text",240), ["City"] = ("text",100), ["State"] = ("text",100),
            ["PostalCode"] = ("text",30), ["BranchId"] = ("reference",36)
        };
    public static IReadOnlyList<string> Names => Array.AsReadOnly(Fields.Keys.ToArray());

    internal static string NormalizeForCore(string payload)
    {
        if(!TryRead(payload,out _))throw new ArgumentException("Invalid teacher draft.");
        var root=JsonNode.Parse(payload)!.AsObject();var fields=root["fields"]!.AsObject();
        foreach(var key in fields.Select(x=>x.Key).ToArray()){
            var field=fields[key]!;if(field["kind"]!.GetValue<string>()!="text")continue;
            var value=field["value"]!.GetValue<string>().Trim();
            if(value.Length==0)fields.Remove(key);else field["value"]=value;
        }
        var canonical=PentaOperationalPayload.Canonicalize(root.ToJsonString());
        if(!TryRead(canonical,out _))throw new ArgumentException("Invalid teacher draft.");return canonical;
    }
    internal static object[] Describe(IReadOnlyList<PentaOnboardingBranchChoice> branches)=>Fields.Select(field=>(object)new{
        key=field.Key,kind=field.Value.Kind,label=System.Text.RegularExpressions.Regex.Replace(field.Key,"(?<=[a-z])(?=[A-Z])"," "),
        required=field.Key is "FirstName" or "LastName",clearable=field.Key is not ("FirstName" or "LastName"),
        choices=field.Value.Kind=="reference"?branches:Array.Empty<PentaOnboardingBranchChoice>(),columns=Array.Empty<object>(),max_length=field.Value.Maximum
    }).ToArray();

    public static bool TryRead(string? payload, out CreateTeacherRequest? request)
    {
        request = null;
        if (payload is null || payload.Length > 6000) return false;
        try {
            var canonical = PentaOperationalPayload.Canonicalize(payload);
            if (canonical.Length > 6000) return false;
            using var doc = JsonDocument.Parse(canonical);
            var root = doc.RootElement;
            if (root.GetProperty("tool_key").GetString() != Tool || root.GetProperty("tool_revision").GetInt64() != 1) return false;
            var values = new Dictionary<string,string>(StringComparer.Ordinal);
            foreach (var field in root.GetProperty("fields").EnumerateObject()) {
                if (!Fields.TryGetValue(field.Name,out var definition) || field.Value.GetProperty("kind").GetString() != definition.Kind) return false;
                var value = field.Value.GetProperty("value");
                if (value.ValueKind != JsonValueKind.String) return false;
                var text = value.GetString()!;
                if (text.Length > definition.Maximum || text.Any(char.IsControl)) return false;
                if (definition.Kind == "date" && !DateOnly.TryParseExact(text,"yyyy-MM-dd",CultureInfo.InvariantCulture,DateTimeStyles.None,out _)) return false;
                if (definition.Kind == "reference" && (!Guid.TryParseExact(text,"D",out var id) || id == Guid.Empty || id.ToString("D") != text)) return false;
                values.Add(field.Name,text);
            }
            string? Read(string name) => values.GetValueOrDefault(name);
            DateOnly? Date(string name) => Read(name) is { } value ? DateOnly.ParseExact(value,"yyyy-MM-dd",CultureInfo.InvariantCulture) : null;
            request = new CreateTeacherRequest(Read("FirstName") ?? "",Read("LastName") ?? "",Read("Email"),Read("Phone"),Read("Specialties"),
                Read("BranchId") is { } branch ? Guid.ParseExact(branch,"D") : null,Read("Qualifications"),Read("EmploymentType"),
                Date("DateOfBirth"),Date("JoiningDate"),Read("AddressLine1"),Read("City"),Read("State"),Read("PostalCode"));
            return true;
        } catch(Exception error) when(error is JsonException or ArgumentException or InvalidOperationException or FormatException or KeyNotFoundException) {
            request = null; return false;
        }
    }
    public static bool HasRequiredFields(string payload) => TryRead(payload,out var request) &&
        !string.IsNullOrWhiteSpace(request!.FirstName) && !string.IsNullOrWhiteSpace(request.LastName);
}
