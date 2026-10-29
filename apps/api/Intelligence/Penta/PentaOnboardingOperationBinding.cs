using System.Text.Json;

namespace AcademyDesk.Api.Intelligence.Penta;

public sealed record PentaOnboardingOperationBinding(string OperationRef, long Revision, string Hash, Guid? ApprovalId = null)
{
    public static bool TryRead(JsonElement root, bool approval, out PentaOnboardingOperationBinding? result)
    {
        result = null;
        if (root.ValueKind != JsonValueKind.Object || root.GetRawText().Length > 1024) return false;
        var expected = approval ? new[] { "operation_ref", "revision", "payload_hash", "approval_ref" } : ["operation_ref", "revision", "payload_hash"];
        var keys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var property in root.EnumerateObject())
            if (!keys.Add(property.Name) || !expected.Contains(property.Name, StringComparer.Ordinal)) return false;
        if (keys.Count != expected.Length || root.GetProperty("operation_ref").ValueKind != JsonValueKind.String ||
            root.GetProperty("payload_hash").ValueKind != JsonValueKind.String || root.GetProperty("revision").ValueKind != JsonValueKind.Number ||
            !root.GetProperty("revision").TryGetInt64(out var revision) || revision is < 1 or > 100) return false;
        var op = root.GetProperty("operation_ref").GetString()!; var hash = root.GetProperty("payload_hash").GetString()!;
        if (op.Length is < 1 or > 128 || op.Any(c => !char.IsAsciiLetterOrDigit(c) && c is not ('_' or '.' or ':' or '-')) ||
            hash.Length != 64 || hash.Any(c => c is not (>= 'a' and <= 'f' or >= '0' and <= '9'))) return false;
        Guid? id = null;
        if (approval) {
            var value = root.GetProperty("approval_ref");
            if (value.ValueKind != JsonValueKind.String || !Guid.TryParseExact(value.GetString(), "D", out var parsed) || parsed == Guid.Empty) return false;
            id = parsed;
        }
        result = new(op, revision, hash, id); return true;
    }
}
