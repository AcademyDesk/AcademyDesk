using System.Text.Json;

namespace AcademyDesk.Api.Intelligence.Penta;

internal sealed record PentaOnboardingPreparationInput(long BaseRevision, JsonElement Scope, string Payload, string[]? Questions = null)
{
    public static bool TryRead(JsonElement root, Guid requestId, out PentaOnboardingPreparationInput? result) =>
        TryRead(root, requestId, false, out result);
    public static bool TryReadPartial(JsonElement root, Guid requestId, out PentaOnboardingPreparationInput? result) =>
        TryRead(root, requestId, true, out result);
    private static bool TryRead(JsonElement root, Guid requestId, bool partial, out PentaOnboardingPreparationInput? result)
    {
        result = null;
        try {
            Exact(root, "request_ref", "scope", "base_revision", "catalog_revision", "kind", "candidates", "questions", "invalidates_review");
            if (requestId == Guid.Empty || root.GetRawText().Length > 16000 ||
                root.GetProperty("request_ref").GetString() != requestId.ToString("D") ||
                root.GetProperty("kind").GetString() != (partial ? "clarify" : "host_prepare") ||
                Number(root, "catalog_revision") != 1 || !root.GetProperty("invalidates_review").GetBoolean()) return false;
            var revision = Number(root, "base_revision");
            if (revision is < 1 or >= 100) return false;
            var questions = root.GetProperty("questions");
            if (questions.ValueKind != JsonValueKind.Array || (partial ? questions.GetArrayLength() is < 1 or > 3 : questions.GetArrayLength() != 0)) return false;
            var questionKeys = new List<string>();
            foreach (var question in questions.EnumerateArray()) {
                if (question.ValueKind != JsonValueKind.String || !PentaOnboardingFields.Names.Contains(question.GetString()!, StringComparer.Ordinal) ||
                    questionKeys.Contains(question.GetString()!, StringComparer.Ordinal)) return false;
                questionKeys.Add(question.GetString()!);
            }
            var scope = root.GetProperty("scope");
            Exact(scope, "tenant_id", "actor_id", "connection_id", "conversation_id", "authority_version", "context_version");
            foreach (var key in new[] { "tenant_id", "actor_id", "connection_id", "conversation_id" })
                if (scope.GetProperty(key).ValueKind != JsonValueKind.String || scope.GetProperty(key).GetString()!.Length is < 1 or > 128) return false;
            if (Number(scope, "authority_version") != 1 || Number(scope, "context_version") is < 1 or > 9007199254740991) return false;
            var candidates = root.GetProperty("candidates");
            if (candidates.ValueKind != JsonValueKind.Array || candidates.GetArrayLength() != 1) return false;
            var step = candidates[0]; Exact(step, "tool_key", "edit");
            if (step.GetProperty("tool_key").GetString() != PentaOnboardingDraftService.Tool) return false;
            var edit = step.GetProperty("edit"); Exact(edit, "mode", "set", "remove");
            if (edit.GetProperty("mode").GetString() != "replace" || edit.GetProperty("remove").ValueKind != JsonValueKind.Array ||
                edit.GetProperty("remove").GetArrayLength() != 0) return false;
            var payload = PentaOperationalPayload.Canonicalize(JsonSerializer.Serialize(new {
                tool_key = PentaOnboardingDraftService.Tool, tool_revision = 1, fields = edit.GetProperty("set") }));
            if (!PentaOnboardingFields.TryRead(payload, out _)) return false;
            result = new(revision, scope.Clone(), payload, questionKeys.ToArray()); return true;
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or KeyNotFoundException or JsonException or FormatException) { return false; }
    }
    private static long Number(JsonElement root, string key) => root.GetProperty(key).GetInt64();
    private static void Exact(JsonElement root, params string[] expected)
    {
        if (root.ValueKind != JsonValueKind.Object) throw new ArgumentException("Invalid Core decision.");
        var keys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var property in root.EnumerateObject())
            if (!keys.Add(property.Name) || !expected.Contains(property.Name, StringComparer.Ordinal)) throw new ArgumentException("Invalid Core decision.");
        if (keys.Count != expected.Length) throw new ArgumentException("Invalid Core decision.");
    }
}
