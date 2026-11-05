using System.Text.Json;

namespace AcademyDesk.Api.Intelligence.Penta;

// Host admission contract, not a model plan or an approval contract.
public sealed record PentaOnboardingConversationInput(Guid RequestId, long ExpectedVersion,
    Guid? DraftId, long DraftRevision, string PayloadHash, string? Text)
{
    public static bool TryRead(JsonElement value, bool claim, out PentaOnboardingConversationInput? input)
    {
        input = null;
        string[] keys = claim ? ["requestId", "expectedVersion", "draftRevision", "payloadHash", "text"]
            : ["requestId", "expectedVersion", "draftId", "draftRevision", "payloadHash"];
        if (value.ValueKind != JsonValueKind.Object || value.GetRawText().Length > 12000) return false;
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var field in value.EnumerateObject())
            if (!seen.Add(field.Name) || !keys.Contains(field.Name, StringComparer.Ordinal)) return false;
        if (seen.Count != keys.Length || !Id(value.GetProperty("requestId"), out var request) ||
            !Number(value.GetProperty("expectedVersion"), 0, 99, out var version) ||
            !Number(value.GetProperty("draftRevision"), 1, 100, out var revision) ||
            value.GetProperty("payloadHash").ValueKind != JsonValueKind.String) return false;
        var hash = value.GetProperty("payloadHash").GetString()!;
        if (hash.Length != 64 || !hash.All(c => c is >= '0' and <= '9' or >= 'a' and <= 'f')) return false;
        Guid? draft = null; string? text = null;
        if (claim)
        {
            if (value.GetProperty("text").ValueKind != JsonValueKind.String) return false;
            text = value.GetProperty("text").GetString();
            if (text is not { Length: > 0 and <= 2000 } || string.IsNullOrWhiteSpace(text) ||
                text.Any(c => char.IsControl(c) && c is not '\n' and not '\t')) return false;
        }
        else
        {
            if (!Id(value.GetProperty("draftId"), out var selected)) return false;
            draft = selected;
        }
        input = new(request, version, draft, revision, hash, text);
        return true;
    }

    private static bool Id(JsonElement value, out Guid id)
    {
        id = Guid.Empty;
        return value.ValueKind == JsonValueKind.String && Guid.TryParseExact(value.GetString(), "D", out id) && id != Guid.Empty;
    }
    private static bool Number(JsonElement value, long min, long max, out long number)
    {
        number = 0;
        return value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out number) && number >= min && number <= max;
    }
}
