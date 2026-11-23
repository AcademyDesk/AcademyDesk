using System.Text.Json;
using AcademyDesk.Api.Intelligence.Penta;

namespace AcademyDesk.Api.Tests;

public sealed class PentaTaskDialogueTests
{
    private static readonly DateOnly Today = new(2026,11,23);
    private static readonly PentaOperationalScope Scope = new("academy", "actor", "connection", "conversation", 1, 1);
    private static readonly IReadOnlySet<string> Keys = PentaTaskCatalog.Keys.ToHashSet();
    private static readonly Guid RequestId = Guid.Parse("a38694a1-f0df-4fc4-bd75-6e9076cfaebd");
    private static JsonElement Reply(string kind, object? plan = null, object[]? edits = null) => JsonSerializer.SerializeToElement(new {
        contract = PentaTaskDialogue.Contract, request_ref = RequestId.ToString("D"), kind, plan, edits = edits ?? [] });
    private static object Plan => new { contract = PentaTaskPlanning.Contract, base_revision = 0, steps = new[] { new {
        id = "student", tool_key = PentaOnboardingFields.Tool, tool_revision = 1,
        fields = new { StudentFirstName = new { kind = "text", value = "Synthetic" } },
        bindings = new Dictionary<string, object>(), depends_on = Array.Empty<string>() } } };
    private static PentaTaskPlan Draft() => PentaTaskDialogue.Apply(Scope, RequestId, Reply("create", Plan), null, Keys, Today).Plan!;

    [Fact]
    public void Initial_missing_details_become_draft_not_false_completion()
    {
        var decision = PentaTaskDialogue.Apply(Scope, RequestId, Reply("create", Plan), null, Keys, Today);
        Assert.Equal("DRAFT", decision.Kind); Assert.Contains("Date Of Birth", decision.Message);
        Assert.Contains("No academy records", decision.Message);
        var view = PentaTaskDialogue.View(decision.Plan);
        Assert.False(view[0].GetProperty("execution_enabled").GetBoolean());
        Assert.False(view[0].TryGetProperty("scope", out _));
    }
    [Fact]
    public void Field_edits_preserve_other_details_and_state_roundtrips_with_version_and_authority()
    {
        var decision = PentaTaskDialogue.Apply(Scope, RequestId, Reply("edit", edits: [new {
            step_id="student", set=new { StudentLastName=new { kind="text", value="Person" } }, remove=Array.Empty<string>() }]), Draft(), Keys, Today);
        Assert.Equal("Synthetic", decision.Plan!.Steps[0].Fields["StudentFirstName"].GetProperty("value").GetString());
        var state = new PentaTaskStoredState(PentaTaskDialogue.Contract, 2, 1, decision.Plan);
        var restored = JsonSerializer.Deserialize<PentaTaskStoredState>(JsonSerializer.Serialize(state))!;
        Assert.Equal(2,restored.SessionVersion); Assert.Equal(Scope,restored.Plan!.Scope);
        Assert.Equal("Person",restored.Plan.Steps[0].Fields["StudentLastName"].GetProperty("value").GetString());
    }
    [Theory]
    [InlineData("execute")][InlineData("approved")][InlineData("succeeded")][InlineData("sql")]
    public void Model_cannot_authorize_or_claim_execution(string kind) => Assert.Throws<ArgumentException>(() =>
        PentaTaskDialogue.Apply(Scope,RequestId,Reply(kind),Draft(),Keys,Today));
    [Theory]
    [InlineData("status")][InlineData("unsupported")][InlineData("clarify")]
    public void Nonmutating_turn_retains_current_draft(string kind)
    {
        var current=Draft(); var next=PentaTaskDialogue.Apply(Scope,RequestId,Reply(kind),current,Keys,Today);
        Assert.Same(current,next.Plan);
    }
    [Fact]
    public void Cancel_only_discards_unexecuted_draft_and_creation_cannot_overwrite_existing_tasks()
    {
        Assert.Null(PentaTaskDialogue.Apply(Scope,RequestId,Reply("cancel"),Draft(),Keys,Today).Plan);
        Assert.Throws<ArgumentException>(()=>PentaTaskDialogue.Apply(Scope,RequestId,Reply("create",Plan),Draft(),Keys,Today));
        Assert.Throws<ArgumentException>(()=>PentaTaskDialogue.Apply(Scope,Guid.NewGuid(),Reply("create",Plan),null,Keys,Today));
    }
}
