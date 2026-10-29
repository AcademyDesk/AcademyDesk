using System.Globalization;
using System.Text.Json;

namespace AcademyDesk.Api.Intelligence.Penta;

// Supplied by authenticated host composition/current durable state, NEVER by a model or browser.
public sealed record PentaOperationalScope(string TenantId, string ActorId, string ConnectionId,
    string ConversationId, long AuthorityVersion, long ContextVersion);
public sealed record PentaOperationalExpectation(PentaOperationalScope Scope, string OperationRef,
    string RequestRef, string ToolKey, long ToolRevision, string DraftRef, long DraftRevision, string PayloadHash);
public sealed record PentaBoundPreparation(string Status, string CanonicalPayload, string Preview,
    string? ApprovalRef, DateTimeOffset? ApprovalExpiresAt);
public sealed record PentaBoundReceipt(string Status, string? ReceiptRef, string? AuditRef,
    IReadOnlyList<string> SourceRefs, string ErrorCode);

/// <summary>
/// Inactive operational-0.3 host-boundary parser. Structural/identity binding only:
/// parsed approval/success claims are NOT permission, proof of commit or audit evidence.
/// Current authorization and durable approval/operation/audit rows must be checked separately.
/// </summary>
public static class PentaOperationalBindings
{
    private static readonly string[] States = ["prepared", "awaiting_approval", "approved", "executing", "succeeded", "failed", "cancelled", "unknown"];
    private const long MaxInteger = 9007199254740991;

    public static PentaBoundPreparation ReadPreparation(string json, PentaOperationalExpectation expected)
    {
        ValidateExpectation(expected);
        return Parse(json, root =>
        {
            Object(root, "ref", "request_ref", "scope", "tool_key", "tool_revision", "draft_ref", "draft_revision",
                "payload_hash", "canonical_fields", "preview", "approval_ref", "approval_expires_at", "status");
            CheckScope(root.GetProperty("scope"), expected.Scope);
            Same(Reference(root, "ref"), expected.OperationRef);
            Same(Reference(root, "request_ref"), expected.RequestRef);
            Same(Reference(root, "tool_key"), expected.ToolKey);
            Same(Reference(root, "draft_ref"), expected.DraftRef);
            if (Integer(root, "tool_revision") != expected.ToolRevision || Integer(root, "draft_revision") != expected.DraftRevision) throw Invalid();
            Same(Text(root, "payload_hash", 64), expected.PayloadHash);
            var fields = root.GetProperty("canonical_fields");
            var payload = JsonSerializer.Serialize(new { tool_key = expected.ToolKey, tool_revision = expected.ToolRevision, fields });
            if (!PentaOperationalPayload.MatchesDigest(payload, expected.PayloadHash)) throw Invalid();
            foreach (var field in fields.EnumerateObject()) CheckFieldKey(field.Name);
            var preview = Text(root, "preview", 2000);
            var approval = NullableReference(root, "approval_ref");
            var expiry = root.GetProperty("approval_expires_at");
            DateTimeOffset? expiresAt = expiry.ValueKind == JsonValueKind.Null ? null : Instant(expiry);
            if ((approval is null) != (expiresAt is null)) throw Invalid();
            return new PentaBoundPreparation(State(root), PentaOperationalPayload.Canonicalize(payload), preview, approval, expiresAt);
        });
    }

    public static PentaBoundReceipt ReadReceipt(string json, PentaOperationalExpectation expected)
    {
        ValidateExpectation(expected);
        return Parse(json, root =>
        {
            Object(root, "operation_ref", "scope", "payload_hash", "draft_revision", "status",
                "receipt_ref", "audit_ref", "source_refs", "error_code");
            CheckScope(root.GetProperty("scope"), expected.Scope);
            Same(Reference(root, "operation_ref"), expected.OperationRef);
            Same(Text(root, "payload_hash", 64), expected.PayloadHash);
            if (Integer(root, "draft_revision") != expected.DraftRevision) throw Invalid();
            var state = State(root);
            var receipt = NullableReference(root, "receipt_ref");
            var audit = NullableReference(root, "audit_ref");
            var error = Text(root, "error_code", 32);
            if (error is not ("none" or "denied" or "invalid" or "unavailable" or "conflict" or "unknown") ||
                state == "succeeded" && (receipt is null || audit is null || error != "none") ||
                state == "unknown" && error != "unknown") throw Invalid();
            var sources = root.GetProperty("source_refs");
            if (sources.ValueKind != JsonValueKind.Array || sources.GetArrayLength() > 20) throw Invalid();
            var references = sources.EnumerateArray().Select(ReferenceValue).ToArray();
            return new PentaBoundReceipt(state, receipt, audit, Array.AsReadOnly(references), error);
        });
    }

    // This checks freshness of a parsed claim, not approval authenticity/authority.
    public static bool HasCurrentApprovalClaim(PentaBoundPreparation preparation, DateTimeOffset now) =>
        preparation.Status == "approved" && preparation.ApprovalRef is not null && preparation.ApprovalExpiresAt > now;

    private static void ValidateExpectation(PentaOperationalExpectation expected)
    {
        if (expected?.Scope is not { } scope ||
            new[] { scope.TenantId, scope.ActorId, scope.ConnectionId, scope.ConversationId }.Any(x =>
                string.IsNullOrWhiteSpace(x) || x.Length > 128) ||
            scope.AuthorityVersion is < 1 or > MaxInteger || scope.ContextVersion is < 1 or > MaxInteger ||
            expected.ToolRevision is < 1 or > MaxInteger || expected.DraftRevision is < 1 or > MaxInteger ||
            new[] { expected.OperationRef, expected.RequestRef, expected.ToolKey, expected.DraftRef }.Any(x => !ValidReference(x)) ||
            expected.PayloadHash is not { Length: 64 } || expected.PayloadHash.Any(c => c is not (>= '0' and <= '9' or >= 'a' and <= 'f')))
            throw Invalid();
    }

    private static T Parse<T>(string json, Func<JsonElement, T> read)
    {
        if (json is null || json.Length > PentaOperationalPayload.MaximumJsonCharacters) throw Invalid();
        try
        {
            using var document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 14 });
            return read(document.RootElement);
        }
        catch (Exception exception) when (exception is JsonException or InvalidOperationException or FormatException or KeyNotFoundException or ArgumentException)
        { throw Invalid(); }
    }

    private static void CheckScope(JsonElement scope, PentaOperationalScope expected)
    {
        Object(scope, "tenant_id", "actor_id", "connection_id", "conversation_id", "authority_version", "context_version");
        Same(Text(scope, "tenant_id", 128), expected.TenantId);
        Same(Text(scope, "actor_id", 128), expected.ActorId);
        Same(Text(scope, "connection_id", 128), expected.ConnectionId);
        Same(Text(scope, "conversation_id", 128), expected.ConversationId);
        if (Integer(scope, "authority_version") != expected.AuthorityVersion || Integer(scope, "context_version") != expected.ContextVersion) throw Invalid();
    }

    private static void Object(JsonElement value, params string[] allowed)
    {
        if (value.ValueKind != JsonValueKind.Object) throw Invalid();
        var keys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in value.EnumerateObject())
            if (!keys.Add(item.Name) || !allowed.Contains(item.Name, StringComparer.Ordinal)) throw Invalid();
        if (keys.Count != allowed.Length) throw Invalid();
    }

    private static string Text(JsonElement root, string key, int maximum)
    {
        var value = root.GetProperty(key);
        if (value.ValueKind != JsonValueKind.String) throw Invalid();
        var text = value.GetString()!;
        if (text.Length == 0 || text.EnumerateRunes().Count() > maximum) throw Invalid();
        return text;
    }

    private static long Integer(JsonElement root, string key)
    {
        var value = root.GetProperty(key);
        if (value.ValueKind != JsonValueKind.Number || !value.TryGetInt64(out var integer) || integer is < 1 or > MaxInteger) throw Invalid();
        return integer;
    }

    private static bool ValidReference(string? value) => value is { Length: >= 1 and <= 128 } &&
        char.IsAsciiLetterOrDigit(value[0]) && value.All(c => char.IsAsciiLetterOrDigit(c) || c is '_' or '.' or ':' or '-');
    private static string ReferenceValue(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.String || !ValidReference(value.GetString())) throw Invalid();
        return value.GetString()!;
    }
    private static string Reference(JsonElement root, string key) => ReferenceValue(root.GetProperty(key));
    private static string? NullableReference(JsonElement root, string key) =>
        root.GetProperty(key).ValueKind == JsonValueKind.Null ? null : Reference(root, key);
    private static string State(JsonElement root)
    {
        var state = Text(root, "status", 32);
        if (!States.Contains(state, StringComparer.Ordinal)) throw Invalid();
        return state;
    }
    private static DateTimeOffset Instant(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.String) throw Invalid();
        var text = value.GetString()!;
        // Explicit timezone only; never interpret a naive model date as device/server local time.
        if (text.Length > 40 || !(text.EndsWith('Z') || text.Length >= 6 && (text[^6] is '+' or '-') && text[^3] == ':') ||
            !DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out var instant)) throw Invalid();
        return instant;
    }
    private static void CheckFieldKey(string key)
    {
        var normalized = new string(key.ToLowerInvariant().Where(char.IsAsciiLetterOrDigit).ToArray());
        var protectedPart = new[] { "password", "secret", "credential", "accesstoken", "refreshtoken", "sql", "script", "shell", "endpoint", "callbackurl" }.Any(normalized.Contains);
        if (protectedPart && normalized != "description" ||
            normalized is "role" or "roles" or "permissions" or "permission" or "tenantid" or "academyid" or "actorid" or "connectionid" or "authorityversion" or "approval" or "approved") throw Invalid();
    }
    private static void Same(string actual, string expected) { if (!string.Equals(actual, expected, StringComparison.Ordinal)) throw Invalid(); }
    private static ArgumentException Invalid() => new("PENTA operation binding could not be verified.");
}
