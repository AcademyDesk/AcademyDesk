using System.Text.Json;
using System.Text.RegularExpressions;

namespace AcademyDesk.Api.Intelligence.Penta;

internal sealed record PentaTaskDecision(string Kind, PentaTaskPlan? Plan, string Message);

internal static class PentaTaskDialogue
{
    internal const string Contract = "academy-task-dialogue.v1";

    internal static JsonElement Request(Guid requestId, string text, PentaTaskPlan? current,
        IReadOnlySet<string> permitted, DateOnly today) => JsonSerializer.SerializeToElement(new {
            contract = Contract, request_ref = requestId.ToString("D"), text,
            catalog = PentaTaskRuntimeAccess.Describe(permitted, today),
            tasks = current?.Steps.Select(step => new { id = step.Id, tool_key = step.ToolKey,
                revision = step.Revision, fields = step.Fields,
                bindings = step.Bindings.ToDictionary(x => x.Key, x => new { step_id = x.Value.StepId, output = x.Value.Output }),
                depends_on = step.Dependencies }).ToArray() ?? [] }, new JsonSerializerOptions {
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull });

    internal static PentaTaskDecision Apply(PentaOperationalScope scope, Guid requestId, JsonElement reply,
        PentaTaskPlan? current, IReadOnlySet<string> permitted, DateOnly today)
    {
        try
        {
            Require(reply, "contract", "request_ref", "kind", "plan", "edits");
            if (reply.GetProperty("contract").GetString() != Contract || reply.GetProperty("request_ref").GetString() != requestId.ToString("D")) throw Invalid();
            var kind = reply.GetProperty("kind").GetString();
            var proposed = reply.GetProperty("plan"); var edits = reply.GetProperty("edits");
            if (edits.ValueKind != JsonValueKind.Array || edits.GetArrayLength() > PentaTaskPlanning.MaximumSteps) throw Invalid();
            if (kind == "create") {
                if (current is not null || proposed.ValueKind != JsonValueKind.Object || edits.GetArrayLength() != 0) throw Invalid();
                var plan = PentaTaskPlanning.Create(scope, proposed.GetRawText(), permitted, today);
                return new("DRAFT", plan, Present(plan));
            }
            if (proposed.ValueKind != JsonValueKind.Null) throw Invalid();
            if (kind == "edit") {
                if (current is null || edits.GetArrayLength() == 0) throw Invalid();
                var plan = current; var seen = new HashSet<string>(StringComparer.Ordinal);
                foreach (var edit in edits.EnumerateArray()) {
                    Require(edit, "step_id", "set", "remove");
                    var id = edit.GetProperty("step_id").GetString()!;
                    if (!seen.Add(id)) throw Invalid();
                    var fields = JsonSerializer.Serialize(new { set = edit.GetProperty("set"), remove = edit.GetProperty("remove") });
                    plan = PentaTaskPlanning.Edit(plan, id, plan.Revision, fields, permitted, today);
                }
                return new("DRAFT", plan, Present(plan));
            }
            if (edits.GetArrayLength() != 0) throw Invalid();
            return kind switch {
                "clarify" => new("CLARIFICATION_REQUIRED", current, current is null ?
                    "Tell me what you would like to create or update, including any details you already have. You can combine related tasks." : Present(current)),
                "status" => new("STATUS", current, current is null ? "There is no task draft in this conversation yet." : Present(current)),
                // This endpoint ONLY owns drafts. It has no dispatch/approval operation.
                "cancel" => new("CANCELLED", null, "The pending draft has been cancelled. No academy records were changed."),
                "unsupported" => new("UNSUPPORTED", current, "That operation is not connected to this task-planning preview yet. Your manual workspace remains available."),
                _ => throw Invalid() };
        }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException or JsonException or KeyNotFoundException)
        { throw Invalid(); }
    }

    internal static string Present(PentaTaskPlan plan)
    {
        var issues = plan.Steps.SelectMany(step => step.Issues.Select(issue => (step, issue))).Take(3).ToArray();
        var prefix = $"I have kept {plan.Steps.Count} related task{(plan.Steps.Count == 1 ? "" : "s")} in your draft. ";
        if (issues.Length == 0) return prefix + "The draft is ready for backend validation and review. Nothing has been executed.";
        return prefix + "I still need: " + string.Join("; ", issues.Select(row =>
            row.issue.Field is { } field ? $"{row.step.Id}: {Regex.Replace(field, "(?<=[a-z])(?=[A-Z])", " ")} ({row.issue.Code})" :
                $"{row.step.Id}: {row.issue.Message}")) + ". No academy records have been changed.";
    }

    internal static JsonElement View(PentaTaskPlan? plan) => JsonSerializer.SerializeToElement(plan?.Steps.Select(step => new {
        id = step.Id, tool_key = step.ToolKey, revision = step.Revision, fields = step.Fields,
        bindings = step.Bindings.ToDictionary(x => x.Key, x => new { step_id = x.Value.StepId, output = x.Value.Output }),
        depends_on = step.Dependencies, issues = step.Issues.Select(x => new { field = x.Field, code = x.Code, message = x.Message }),
        status = step.Status, execution_enabled = false }).ToArray() ?? []);

    private static void Require(JsonElement value, params string[] expected)
    {
        if (value.ValueKind != JsonValueKind.Object) throw Invalid();
        var names = value.EnumerateObject().Select(x => x.Name).ToArray();
        if (names.Length != expected.Length || names.Distinct(StringComparer.Ordinal).Count() != names.Length || names.Except(expected, StringComparer.Ordinal).Any()) throw Invalid();
    }
    private static ArgumentException Invalid() => new("PENTA could not validate the task interpretation.");
}
