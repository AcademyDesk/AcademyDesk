using System.Text.Json;
using System.Text.Json.Serialization;

namespace AcademyDesk.Api.Intelligence.Penta;

public sealed record PentaConversationCreateRequest(Guid RequestId)
{
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; init; }
}

public sealed record PentaConversationTurnRequest(Guid RequestId, long ExpectedContextVersion,
    string? Text, string? Capability)
{
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; init; }
}

public sealed record PentaConversationSummary(Guid ConversationId, long ContextVersion,
    DateTime ExpiresAtUtc, string Title = "Synthetic conversation contract test", bool Synthetic = true);
public sealed record PentaConversationMessageView(long Sequence, string Role, string Content);
public sealed record PentaConversationHistory(PentaConversationSummary Conversation,
    IReadOnlyList<PentaConversationMessageView> Messages, bool Synthetic = true);
public sealed record PentaConversationTurnReceipt(Guid ConversationId, Guid TurnId,
    Guid RequestId, long ContextVersion, string Capability,
    IReadOnlyList<PentaConversationMessageView> Messages,
    string Status = "Completed", bool Synthetic = true,
    string Effect = "Conversation contract recorded only. No academy data or model was used.");
public sealed record PentaConversationOutcome(int HttpStatus, object? Value = null, string? Error = null);

public static class PentaConversationContract
{
    public const string FirstInput = "synthetic conversation turn";
    public const string FollowupInput = "synthetic conversation follow-up";
    public const string Reply = "Synthetic contract reply only. No academy information was read or changed.";
    public const int MaximumTurns = 50;

    // C1a must not become an accidental customer transcript storage endpoint.
    // Real natural-language content/context needs the separate privacy/runtime slice.
    public static bool Valid(PentaConversationTurnRequest? request) => request is not null &&
        request.AdditionalProperties is not { Count: > 0 } && request.RequestId != Guid.Empty &&
        request.ExpectedContextVersion >= 0 &&
        request.Text is FirstInput or FollowupInput &&
        request.Capability is not null && PentaCapabilities.All.Contains(request.Capability);
}
