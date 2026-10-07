using System.Text.Json.Serialization;

namespace AcademyDesk.Api.Intelligence.Penta;

// Mirrors penta_mini.learning.PentaLearningEvent. Metadata only; never sent to
// a trainer, shared across tenants, or marked eligible for training in this pilot.
public sealed record PentaMiniLearningEvent(
    [property: JsonPropertyName("event_id")] Guid EventId,
    [property: JsonPropertyName("occurred_at")] DateTimeOffset OccurredAt,
    [property: JsonPropertyName("event_type")] string EventType,
    [property: JsonPropertyName("tenant_id")] string TenantId,
    [property: JsonPropertyName("interaction_id")] string InteractionId,
    [property: JsonPropertyName("model_revision")] string ModelRevision,
    [property: JsonPropertyName("prompt_version")] string PromptVersion,
    [property: JsonPropertyName("tool_protocol_version")] string ToolProtocolVersion,
    [property: JsonPropertyName("failure_category")] string? FailureCategory,
    [property: JsonPropertyName("original_tool")] string? OriginalTool,
    [property: JsonPropertyName("corrected_tool")] string? CorrectedTool,
    [property: JsonPropertyName("original_arguments")] Dictionary<string, string> OriginalArguments,
    [property: JsonPropertyName("corrected_arguments")] Dictionary<string, string> CorrectedArguments,
    [property: JsonPropertyName("validated")] bool Validated,
    [property: JsonPropertyName("training_eligible")] bool TrainingEligible)
{
    // Expected artifact from the authoritative handoff, not runtime attestation.
    public const string ExpectedModelRevision = "Qwen3.5-2B-Q4_0@f6d5376be1edb4d416d56da11e5397a961aca8ae";
    public static PentaMiniLearningEvent? ForOutcome(Guid academy, Guid request, DateTimeOffset now,
        string kind, string? tool, string? error)
    {
        var (eventType, category) = kind switch {
            "RESULT" => ("ACTION_SUCCEEDED", (string?)null),
            "CLARIFICATION_REQUIRED" => ("CLARIFICATION_REQUIRED", "AMBIGUITY"),
            "DeniedAfterPlanning" => ("ACTION_REJECTED", "AUTHORIZATION"),
            "ERROR" when error == "SchemaFailed" => ("SCHEMA_FAILED", "SCHEMA"),
            "ERROR" when error == "ToolValidation" => ("TOOL_FAILED", "CONNECTOR"),
            "ERROR" => ("MODEL_ESCALATION_REQUIRED", "UNCLASSIFIED"),
            _ => ((string?)null, (string?)null)
        };
        return eventType is null ? null : new(Guid.NewGuid(), now, eventType, academy.ToString("D"),
            request.ToString("D"), ExpectedModelRevision, "planner-0.1", "0.1", category,
            tool, null, [], [], kind == "RESULT", false);
    }
}
