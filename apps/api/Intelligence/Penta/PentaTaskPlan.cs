using System.Collections.ObjectModel;
using System.Text.Json;

namespace AcademyDesk.Api.Intelligence.Penta;

internal sealed record PentaTaskReference(string StepId, string Output);
internal sealed record PentaTaskDraft(string Id, string ToolKey, long Revision,
    IReadOnlyDictionary<string, JsonElement> Fields, IReadOnlyDictionary<string, PentaTaskReference> Bindings,
    IReadOnlyList<string> Dependencies, IReadOnlyList<PentaTaskIssue> Issues)
{
    // No "approved"/"succeeded" status can be supplied by a model proposal.
    internal string Status => Issues.Count > 0 ? "collecting" : Dependencies.Count > 0 ? "waiting-for-dependencies" : "ready-for-host-validation";
}
internal sealed record PentaTaskPlan(PentaOperationalScope Scope, long Revision, IReadOnlyList<PentaTaskDraft> Steps);

// INTERNAL host-only observation. Never deserialize this from model/browser output.
// The eventual adapter must verify a real scoped committed receipt/audit before
// constructing it. This type deliberately does not provide dispatch or approval.
internal sealed record PentaTaskHostResult(PentaOperationalScope Scope, string StepId, string ToolKey,
    long DraftRevision, string ReceiptRef, IReadOnlyDictionary<string, Guid> Identities);

/// <summary>
/// Bounded multi-task draft reducer. Pure planning only: no HTTP route, database
/// writes, model activation, permission grant or automatic domain execution.
/// Existing prepared operations/approvals/receipts remain the execution boundary.
/// </summary>
internal static class PentaTaskPlanning
{
    internal const string Contract = "academy-task-plan.v1";
    internal const int MaximumSteps = 12;
    private const int MaximumCharacters = 32768;

    internal static PentaTaskPlan Create(PentaOperationalScope scope, string proposal,
        IReadOnlySet<string> hostPermittedKeys, DateOnly today) => Safe(() => CreateCore(scope, proposal, hostPermittedKeys, today));

    private static PentaTaskPlan CreateCore(PentaOperationalScope scope, string proposal,
        IReadOnlySet<string> hostPermittedKeys, DateOnly today)
    {
        ValidateScope(scope);
        using var json = Read(proposal);
        var root = json.RootElement;
        Object(root, "contract", "base_revision", "steps");
        if (root.GetProperty("contract").GetString() != Contract || root.GetProperty("base_revision").GetInt64() != 0) throw Invalid();
        var steps = root.GetProperty("steps");
        if (steps.ValueKind != JsonValueKind.Array || steps.GetArrayLength() is < 1 or > MaximumSteps) throw Invalid();
        var drafts = new List<PentaTaskDraft>();
        foreach (var step in steps.EnumerateArray())
        {
            Object(step, "id", "tool_key", "tool_revision", "fields", "bindings", "depends_on");
            var id = Reference(step.GetProperty("id"));
            var tool = Reference(step.GetProperty("tool_key"));
            if (!hostPermittedKeys.Contains(tool) || !PentaTaskCatalog.Keys.Contains(tool) || step.GetProperty("tool_revision").GetInt64() != 1) throw Invalid();
            var fields = Fields(tool, step.GetProperty("fields"));
            var bindings = Bindings(step.GetProperty("bindings"));
            var dependencies = References(step.GetProperty("depends_on"));
            drafts.Add(Draft(id, tool, 1, fields, bindings, dependencies, today));
        }
        return Plan(scope, 1, drafts, today);
    }

    internal static PentaTaskPlan Edit(PentaTaskPlan plan, string stepId, long expectedRevision,
        string editJson, IReadOnlySet<string> hostPermittedKeys, DateOnly today,
        IReadOnlyList<PentaTaskHostResult>? committedResults = null) =>
        Safe(() => EditCore(plan, stepId, expectedRevision, editJson, hostPermittedKeys, today, committedResults));

    private static PentaTaskPlan EditCore(PentaTaskPlan plan, string stepId, long expectedRevision,
        string editJson, IReadOnlySet<string> hostPermittedKeys, DateOnly today,
        IReadOnlyList<PentaTaskHostResult>? committedResults)
    {
        ValidateScope(plan.Scope);
        if (plan.Revision != expectedRevision || plan.Revision == long.MaxValue ||
            plan.Steps.Any(x => !hostPermittedKeys.Contains(x.ToolKey))) throw Invalid();
        var target = plan.Steps.SingleOrDefault(x => x.Id == stepId) ?? throw Invalid();
        using var json = Read(editJson);
        Object(json.RootElement, "set", "remove");
        var edits = Fields(target.ToolKey, json.RootElement.GetProperty("set"));
        var remove = References(json.RootElement.GetProperty("remove"), 40);
        if (remove.Intersect(edits.Keys, StringComparer.Ordinal).Any() || edits.Keys.Any(target.Bindings.ContainsKey) || remove.Any(target.Bindings.ContainsKey)) throw Invalid();
        var affected = new HashSet<string>(StringComparer.Ordinal) { stepId };
        bool changed;
        do {
            changed = false;
            foreach (var step in plan.Steps)
                if (step.Dependencies.Any(affected.Contains) && affected.Add(step.Id)) changed = true;
        } while (changed);
        // A committed operation is history, not an editable draft. A separate
        // approved domain correction is needed; never silently rewrite its result.
        if (committedResults?.Any(x => x.Scope == plan.Scope && affected.Contains(x.StepId)) == true) throw Invalid();
        var values = target.Fields.ToDictionary(x => x.Key, x => x.Value.Clone(), StringComparer.Ordinal);
        foreach (var key in remove) {
            if (!values.Remove(key)) throw Invalid();
        }
        foreach (var pair in edits) values[pair.Key] = pair.Value;
        var drafts = plan.Steps.Select(step => affected.Contains(step.Id)
            ? Draft(step.Id, step.ToolKey, checked(step.Revision + 1), step.Id == stepId ? values : step.Fields,
                step.Bindings, step.Dependencies, today)
            : step).ToArray();
        return Plan(plan.Scope, checked(plan.Revision + 1), drafts, today);
    }

    // Materialize typed references using only host-verified persisted results.
    // Returning a payload is NOT authorization, preparation, approval or dispatch.
    internal static string PreparePayload(PentaTaskPlan plan, string stepId,
        IReadOnlyList<PentaTaskHostResult> committedResults, IReadOnlySet<string> hostPermittedKeys, DateOnly today) =>
        Safe(() => PreparePayloadCore(plan, stepId, committedResults, hostPermittedKeys, today));

    private static string PreparePayloadCore(PentaTaskPlan plan, string stepId,
        IReadOnlyList<PentaTaskHostResult> committedResults, IReadOnlySet<string> hostPermittedKeys, DateOnly today)
    {
        ValidateScope(plan.Scope);
        var step = plan.Steps.SingleOrDefault(x => x.Id == stepId) ?? throw Invalid();
        if (!hostPermittedKeys.Contains(step.ToolKey) || step.Issues.Count > 0) throw Invalid();
        var results = new Dictionary<string, PentaTaskHostResult>(StringComparer.Ordinal);
        foreach (var result in committedResults)
        {
            var source = plan.Steps.SingleOrDefault(x => x.Id == result.StepId);
            if (source is null || source.Issues.Count > 0 || result.Scope != plan.Scope || source.ToolKey != result.ToolKey || source.Revision != result.DraftRevision ||
                !ValidReference(result.ReceiptRef) || !results.TryAdd(result.StepId, result)) throw Invalid();
            var definition = PentaTaskCatalog.Describe(source.ToolKey, source.Fields, today);
            if (result.Identities.Any(x => !definition.Outputs.ContainsKey(x.Key) || x.Value == Guid.Empty)) throw Invalid();
        }
        if (results.ContainsKey(stepId) || step.Dependencies.Any(id => !results.ContainsKey(id))) throw Invalid();
        var fields = step.Fields.ToDictionary(x => x.Key, x => x.Value.Clone(), StringComparer.Ordinal);
        foreach (var (key, binding) in step.Bindings)
        {
            if (!results.TryGetValue(binding.StepId, out var result) || !result.Identities.TryGetValue(binding.Output, out var id)) throw Invalid();
            fields[key] = JsonSerializer.SerializeToElement(new { kind = "reference", value = id.ToString("D") });
        }
        var definitionAfter = PentaTaskCatalog.Describe(step.ToolKey, fields, today);
        if (PentaTaskCatalog.Inspect(definitionAfter, fields, new HashSet<string>(), today).Count != 0) throw Invalid();
        return PentaTaskCatalog.Payload(step.ToolKey, fields);
    }

    private static PentaTaskDraft Draft(string id, string tool, long revision,
        IReadOnlyDictionary<string, JsonElement> fields, IReadOnlyDictionary<string, PentaTaskReference> bindings,
        IReadOnlyList<string> dependencies, DateOnly today)
    {
        var definition = PentaTaskCatalog.Describe(tool, fields, today);
        var specs = definition.Fields.ToDictionary(x => x.Key, StringComparer.Ordinal);
        if (fields.Keys.Any(key => !specs.ContainsKey(key)) || bindings.Any(pair => fields.ContainsKey(pair.Key) ||
            !specs.TryGetValue(pair.Key, out var field) || field.ReferenceEntity is null)) throw Invalid();
        var issues = PentaTaskCatalog.Inspect(definition, fields, bindings.Keys.ToHashSet(StringComparer.Ordinal), today);
        return new(id, tool, revision,
            new ReadOnlyDictionary<string, JsonElement>(fields.ToDictionary(x => x.Key, x => x.Value.Clone(), StringComparer.Ordinal)),
            new ReadOnlyDictionary<string, PentaTaskReference>(bindings.ToDictionary(x => x.Key, x => x.Value, StringComparer.Ordinal)),
            Array.AsReadOnly(dependencies.Concat(bindings.Values.Select(x => x.StepId)).Distinct(StringComparer.Ordinal).ToArray()), issues);
    }

    private static PentaTaskPlan Plan(PentaOperationalScope scope, long revision, IEnumerable<PentaTaskDraft> input, DateOnly today)
    {
        var steps = input.ToArray();
        if (steps.Select(x => x.Id).Distinct(StringComparer.Ordinal).Count() != steps.Length) throw Invalid();
        var byId = steps.ToDictionary(x => x.Id, StringComparer.Ordinal);
        foreach (var step in steps)
        {
            if (step.Dependencies.Any(id => id == step.Id || !byId.ContainsKey(id))) throw Invalid();
            var fields = PentaTaskCatalog.Describe(step.ToolKey, step.Fields, today).Fields.ToDictionary(x => x.Key, StringComparer.Ordinal);
            foreach (var (key, reference) in step.Bindings)
            {
                var source = byId[reference.StepId];
                var outputs = PentaTaskCatalog.Describe(source.ToolKey, source.Fields, today).Outputs;
                if (!outputs.TryGetValue(reference.Output, out var entity) || fields[key].ReferenceEntity != entity) throw Invalid();
            }
        }
        var pending = steps.ToList();
        var ordered = new List<PentaTaskDraft>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        while (pending.Count > 0)
        {
            var ready = pending.Where(step => step.Dependencies.All(seen.Contains)).ToArray();
            if (ready.Length == 0) throw Invalid();
            foreach (var step in ready) { ordered.Add(step); seen.Add(step.Id); pending.Remove(step); }
        }
        return new(scope, revision, ordered.AsReadOnly());
    }

    private static Dictionary<string, JsonElement> Fields(string tool, JsonElement input)
    {
        var canonical = PentaOperationalPayload.Canonicalize(JsonSerializer.Serialize(new { tool_key = tool, tool_revision = 1, fields = input }));
        using var json = JsonDocument.Parse(canonical);
        return json.RootElement.GetProperty("fields").EnumerateObject().ToDictionary(x => x.Name, x => x.Value.Clone(), StringComparer.Ordinal);
    }
    private static Dictionary<string, PentaTaskReference> Bindings(JsonElement input)
    {
        if (input.ValueKind != JsonValueKind.Object || input.EnumerateObject().Count() > 40) throw Invalid();
        var result = new Dictionary<string, PentaTaskReference>(StringComparer.Ordinal);
        foreach (var field in input.EnumerateObject()) {
            Object(field.Value, "step_id", "output");
            if (!ValidReference(field.Name) || !result.TryAdd(field.Name, new(Reference(field.Value.GetProperty("step_id")), Reference(field.Value.GetProperty("output"))))) throw Invalid();
        }
        return result;
    }
    private static string[] References(JsonElement input, int maximum = MaximumSteps)
    {
        if (input.ValueKind != JsonValueKind.Array || input.GetArrayLength() > maximum) throw Invalid();
        var values = input.EnumerateArray().Select(Reference).ToArray();
        if (values.Distinct(StringComparer.Ordinal).Count() != values.Length) throw Invalid();
        return values;
    }
    private static void Object(JsonElement input, params string[] keys)
    {
        if (input.ValueKind != JsonValueKind.Object) throw Invalid();
        var actual = input.EnumerateObject().Select(x => x.Name).ToArray();
        if (actual.Length != keys.Length || actual.Distinct(StringComparer.Ordinal).Count() != keys.Length || actual.Except(keys, StringComparer.Ordinal).Any()) throw Invalid();
    }
    private static JsonDocument Read(string input)
    {
        if (string.IsNullOrEmpty(input) || input.Length > MaximumCharacters) throw Invalid();
        try { return JsonDocument.Parse(input, new JsonDocumentOptions { MaxDepth = 14 }); }
        catch (JsonException) { throw Invalid(); }
    }
    private static bool ValidReference(string? value) => value is { Length: > 0 and <= 128 } && char.IsAsciiLetterOrDigit(value[0]) &&
        value.All(c => char.IsAsciiLetterOrDigit(c) || c is '_' or '.' or ':' or '-');
    private static string Reference(JsonElement value) => value.ValueKind == JsonValueKind.String && ValidReference(value.GetString()) ? value.GetString()! : throw Invalid();
    private static void ValidateScope(PentaOperationalScope scope)
    {
        if (scope is null || new[] { scope.ActorId, scope.TenantId, scope.ConversationId, scope.ConnectionId }.Any(x => !ValidReference(x)) ||
            scope.AuthorityVersion < 1 || scope.ContextVersion < 1) throw Invalid();
    }
    private static ArgumentException Invalid() => PentaTaskCatalog.Invalid();
    private static T Safe<T>(Func<T> action)
    {
        try { return action(); }
        catch (Exception error) when (error is ArgumentException or JsonException or InvalidOperationException or
            FormatException or KeyNotFoundException or OverflowException) { throw Invalid(); }
    }
}
