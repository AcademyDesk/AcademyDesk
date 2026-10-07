using System.Text.Json;
using System.Text.Json.Serialization;

namespace AcademyDesk.Api.Intelligence.Penta;

// Wire contract pinned to PentaMini 2bf76b5 / protocol.py, protocol 0.1.
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record MiniState(
    [property: JsonRequired, JsonPropertyName("current_learner_id")] string? CurrentLearnerId,
    [property: JsonRequired, JsonPropertyName("current_result_ids")] string[] CurrentResultIds,
    [property: JsonRequired, JsonPropertyName("filters")] Dictionary<string, string> Filters,
    [property: JsonRequired, JsonPropertyName("sort_by")] string? SortBy)
{
    public static MiniState Empty => new(null, [], [], null);
}
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record MiniToolCall(
    [property: JsonRequired, JsonPropertyName("name")] string Name,
    [property: JsonRequired, JsonPropertyName("arguments")] Dictionary<string, JsonElement> Arguments);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record MiniPlan(
    [property: JsonRequired, JsonPropertyName("protocol_version")] string ProtocolVersion,
    [property: JsonRequired, JsonPropertyName("kind")] string Kind,
    [property: JsonRequired, JsonPropertyName("message")] string Message,
    [property: JsonRequired, JsonPropertyName("tool_call")] MiniToolCall? ToolCall,
    [property: JsonRequired, JsonPropertyName("risk")] string? Risk,
    [property: JsonRequired, JsonPropertyName("state")] MiniState State);
public sealed record MiniPlanRequest(
    [property: JsonPropertyName("conversation_id")] string ConversationId,
    [property: JsonPropertyName("user_message")] string UserMessage,
    [property: JsonPropertyName("state")] MiniState State,
    [property: JsonPropertyName("available_tools")] string[] AvailableTools);
public sealed record MiniProviderOutcome(MiniPlan? Plan, string? Error = null);
public interface IPentaProvider
{
    Task<MiniProviderOutcome> PlanAsync(MiniPlanRequest request, CancellationToken token);
    Task<string> ReadinessAsync(CancellationToken token);
}
public static class PentaMiniTools
{
    public const string ProtocolVersion = "0.1";
    public static readonly string[] Available = ["SearchLearners", "GetLearner"];
    public static readonly HashSet<string> Kinds = ["TEXT_RESPONSE", "TOOL_REQUEST", "CLARIFICATION_REQUIRED", "APPROVAL_REQUIRED", "UNSUPPORTED", "REFUSAL", "ERROR"];
    public static bool ValidState(MiniState? state) => state is not null &&
        state.CurrentResultIds is { Length: <= 10 } && state.CurrentResultIds.All(x => Guid.TryParse(x, out _)) &&
        state.CurrentResultIds.Distinct().Count() == state.CurrentResultIds.Length &&
        (state.CurrentLearnerId is null || state.CurrentResultIds.Contains(state.CurrentLearnerId)) &&
        state.SortBy is null or "outstanding_desc" && state.Filters is { Count: <= 3 } &&
        state.Filters.All(x => x.Key is "name" or "subject" or "balance_status" &&
            x.Value is { Length: > 0 and <= 128 } && !x.Value.Any(char.IsControl)) &&
        (!state.Filters.TryGetValue("balance_status", out var balance) || balance is "Pending" or "Clear");
    public static bool ValidPlan(MiniPlan? plan) => plan is not null && plan.ProtocolVersion == ProtocolVersion &&
        Kinds.Contains(plan.Kind) && plan.Message is { Length: > 0 and <= 500 } && ValidState(plan.State) &&
        (plan.Kind == "TOOL_REQUEST" ? plan.ToolCall is { Name.Length: > 0 and <= 80, Arguments.Count: <= 4 } && plan.Risk == "READ" : plan.ToolCall is null);
    public static bool TrySearch(MiniToolCall call, out MiniState state)
    {
        state = MiniState.Empty;
        if (call.Name != "SearchLearners" || call.Arguments is null || call.Arguments.Count > 4 ||
            call.Arguments.Any(x => x.Key is not ("name" or "subject" or "balance_status" or "sort_by") || x.Value.ValueKind != JsonValueKind.String)) return false;
        var values = call.Arguments.ToDictionary(x => x.Key, x => x.Value.GetString()!);
        var sort = values.Remove("sort_by", out var value) ? value : null;
        state = new(null, [], values, sort);
        return ValidState(state);
    }
}
