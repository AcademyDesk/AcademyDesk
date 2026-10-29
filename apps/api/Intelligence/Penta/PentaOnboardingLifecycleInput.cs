using System.Text.Json;

namespace AcademyDesk.Api.Intelligence.Penta;

internal sealed record PentaOnboardingLifecycleInput(long BaseRevision, string Kind, JsonElement Scope)
{
    public static bool TryRead(JsonElement root, Guid requestId, out PentaOnboardingLifecycleInput? result)
    {
        result = null;
        try {
            Exact(root, "request_ref", "scope", "base_revision", "catalog_revision", "kind", "candidates", "questions", "invalidates_review");
            if (requestId == Guid.Empty || root.GetRawText().Length > 2048 ||
                root.GetProperty("request_ref").GetString() != requestId.ToString("D") ||
                root.GetProperty("catalog_revision").GetInt64() != 1 || !root.GetProperty("invalidates_review").GetBoolean()) return false;
            var kind = root.GetProperty("kind").GetString();
            var revision = root.GetProperty("base_revision").GetInt64();
            if (kind is not ("request_pause" or "request_resume" or "request_cancel") || revision is < 1 or >= 100) return false;
            foreach (var key in new[] { "candidates", "questions" })
                if (root.GetProperty(key).ValueKind != JsonValueKind.Array || root.GetProperty(key).GetArrayLength() != 0) return false;
            var scope = root.GetProperty("scope");
            Exact(scope, "tenant_id", "actor_id", "connection_id", "conversation_id", "authority_version", "context_version");
            foreach (var key in new[] { "tenant_id", "actor_id", "connection_id", "conversation_id" })
                if (scope.GetProperty(key).ValueKind != JsonValueKind.String || scope.GetProperty(key).GetString()!.Length is < 1 or > 128) return false;
            if (scope.GetProperty("authority_version").GetInt64() != 1 ||
                scope.GetProperty("context_version").GetInt64() is < 1 or > 9007199254740991) return false;
            result = new(revision, kind, scope.Clone()); return true;
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or KeyNotFoundException or JsonException or FormatException) { return false; }
    }

    private static void Exact(JsonElement value, params string[] expected)
    {
        if (value.ValueKind != JsonValueKind.Object) throw new ArgumentException("Invalid lifecycle request.");
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var property in value.EnumerateObject())
            if (!seen.Add(property.Name) || !expected.Contains(property.Name, StringComparer.Ordinal)) throw new ArgumentException("Invalid lifecycle request.");
        if (seen.Count != expected.Length) throw new ArgumentException("Invalid lifecycle request.");
    }
}
