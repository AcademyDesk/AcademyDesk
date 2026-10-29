using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AcademyDesk.Api.Intelligence.Penta;

/// <summary>
/// Inactive operational-draft-0.3.0 payload adapter. Matches Python's
/// sort_keys/ensure_ascii canonical JSON, not the default .NET JSON escaping.
/// A digest binds bytes only: it is NOT authorization, approval or a tool catalog.
/// No route, DI registration or domain dispatch is provided here.
/// </summary>
public static class PentaOperationalPayload
{
    public const string ContractVersion = "operational-draft-0.3.0";
    public const int MaximumJsonCharacters = 262144;
    private const long MaximumSafeInteger = 9007199254740991;

    public static string Canonicalize(string json)
    {
        if (json is null || json.Length > MaximumJsonCharacters)
            throw Invalid();
        try
        {
            using var document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 12 });
            var root = document.RootElement;
            RequireObject(root, "tool_key", "tool_revision", "fields");
            var tool = root.GetProperty("tool_key");
            if (tool.ValueKind != JsonValueKind.String || !Reference(tool.GetString()!)) throw Invalid();
            var revision = root.GetProperty("tool_revision");
            // Host catalog revisions deliberately stay within interoperable integer bounds.
            if (!revision.TryGetInt64(out var number) || number is < 1 or > MaximumSafeInteger) throw Invalid();
            ValidateFields(root.GetProperty("fields"), 40, allowRows: true);
            var builder = new StringBuilder();
            Write(root, builder);
            return builder.ToString();
        }
        catch (Exception exception) when (exception is JsonException or InvalidOperationException or FormatException)
        {
            // Never echo the input or a parser error that might contain private fields.
            throw Invalid();
        }
    }

    public static string Digest(string json) => Convert.ToHexStringLower(
        SHA256.HashData(Encoding.UTF8.GetBytes(Canonicalize(json))));

    public static bool MatchesDigest(string json, string? expected)
    {
        if (expected is not { Length: 64 } || expected.Any(c => c is not (>= '0' and <= '9' or >= 'a' and <= 'f')))
            return false;
        try
        {
            return CryptographicOperations.FixedTimeEquals(
                Encoding.ASCII.GetBytes(Digest(json)), Encoding.ASCII.GetBytes(expected));
        }
        catch (ArgumentException) { return false; }
    }

    private static ArgumentException Invalid() => new("Invalid PENTA operational payload.");

    private static void RequireObject(JsonElement value, params string[] expected)
    {
        if (value.ValueKind != JsonValueKind.Object) throw Invalid();
        var keys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var property in value.EnumerateObject())
            if (!keys.Add(property.Name) || !expected.Contains(property.Name, StringComparer.Ordinal)) throw Invalid();
        if (keys.Count != expected.Length) throw Invalid();
    }

    private static bool Reference(string value) => value.Length is >= 1 and <= 128 &&
        Regex.IsMatch(value, @"\A[A-Za-z0-9][A-Za-z0-9_.:-]*\z", RegexOptions.CultureInvariant);

    private static void ValidateFields(JsonElement fields, int maximum, bool allowRows)
    {
        if (fields.ValueKind != JsonValueKind.Object) throw Invalid();
        var keys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var property in fields.EnumerateObject())
        {
            if (!keys.Add(property.Name) || keys.Count > maximum || property.Name.Length is < 1 or > 64 ||
                !Regex.IsMatch(property.Name, @"\A[A-Za-z][A-Za-z0-9_]*\z", RegexOptions.CultureInvariant)) throw Invalid();
            ValidateValue(property.Value, allowRows);
        }
    }

    private static void ValidateValue(JsonElement typed, bool allowRows)
    {
        RequireObject(typed, "kind", "value");
        var value = typed.GetProperty("value");
        switch (typed.GetProperty("kind").GetString())
        {
            case "text":
                if (value.ValueKind != JsonValueKind.String || value.GetString()!.EnumerateRunes().Count() > 2000) throw Invalid();
                break;
            case "integer":
                if (value.ValueKind != JsonValueKind.Number || !value.TryGetInt64(out var integer) ||
                    integer is < -MaximumSafeInteger or > MaximumSafeInteger) throw Invalid();
                break;
            case "decimal":
                if (value.ValueKind != JsonValueKind.String || value.GetString()!.Length > 24 ||
                    !Regex.IsMatch(value.GetString()!, @"\A-?(0|[1-9][0-9]{0,15})(\.[0-9]{1,6})?\z", RegexOptions.CultureInvariant)) throw Invalid();
                break;
            case "boolean":
                if (value.ValueKind is not (JsonValueKind.True or JsonValueKind.False)) throw Invalid();
                break;
            case "date":
                if (value.ValueKind != JsonValueKind.String ||
                    !DateOnly.TryParseExact(value.GetString(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _)) throw Invalid();
                break;
            case "reference":
                if (value.ValueKind != JsonValueKind.String || !Reference(value.GetString()!)) throw Invalid();
                break;
            case "rows":
                if (!allowRows || value.ValueKind != JsonValueKind.Array || value.GetArrayLength() > 100) throw Invalid();
                foreach (var row in value.EnumerateArray()) ValidateFields(row, 16, allowRows: false);
                break;
            default: throw Invalid();
        }
    }

    private static void Write(JsonElement value, StringBuilder output)
    {
        switch (value.ValueKind)
        {
            case JsonValueKind.Object:
                output.Append('{');
                var first = true;
                // All object keys are contract-owned ASCII or validated ASCII field keys.
                foreach (var property in value.EnumerateObject().OrderBy(x => x.Name, StringComparer.Ordinal))
                {
                    if (!first) output.Append(',');
                    first = false;
                    WriteString(property.Name, output); output.Append(':'); Write(property.Value, output);
                }
                output.Append('}');
                break;
            case JsonValueKind.Array:
                output.Append('[');
                for (var index = 0; index < value.GetArrayLength(); index++)
                { if (index != 0) output.Append(','); Write(value[index], output); }
                output.Append(']');
                break;
            case JsonValueKind.String: WriteString(value.GetString()!, output); break;
            case JsonValueKind.True: output.Append("true"); break;
            case JsonValueKind.False: output.Append("false"); break;
            case JsonValueKind.Number: output.Append(value.GetInt64().ToString(CultureInfo.InvariantCulture)); break;
            default: throw Invalid();
        }
    }

    private static void WriteString(string value, StringBuilder output)
    {
        output.Append('"');
        foreach (var character in value)
        {
            switch (character)
            {
                case '"': output.Append("\\\""); break;
                case '\\': output.Append("\\\\"); break;
                case '\b': output.Append("\\b"); break;
                case '\f': output.Append("\\f"); break;
                case '\n': output.Append("\\n"); break;
                case '\r': output.Append("\\r"); break;
                case '\t': output.Append("\\t"); break;
                default:
                    if (character < 0x20 || character > 0x7e)
                        output.Append("\\u").Append(((int)character).ToString("x4", CultureInfo.InvariantCulture));
                    else output.Append(character);
                    break;
            }
        }
        output.Append('"');
    }
}
