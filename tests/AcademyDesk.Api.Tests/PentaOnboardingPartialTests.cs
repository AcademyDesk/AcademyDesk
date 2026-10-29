using System.Text.Json;
using System.Text.Json.Nodes;
using AcademyDesk.Api.Intelligence.Penta;

namespace AcademyDesk.Api.Tests;

public sealed class PentaOnboardingPartialTests
{
    private static readonly Guid Request = Guid.Parse("44444444-2222-3333-4444-555555555555");
    private static JsonObject Decision() => JsonSerializer.SerializeToNode(new {
        request_ref = Request.ToString("D"),scope = new { tenant_id = "synthetic",actor_id = "synthetic",connection_id = "synthetic",
            conversation_id = "synthetic",authority_version = 1,context_version = 1 },base_revision = 1,catalog_revision = 1,kind = "clarify",
        candidates = new[] { new { tool_key = PentaOnboardingDraftService.Tool,edit = new { mode = "replace",set = new { },remove = Array.Empty<string>() } } },
        questions = new[] { "StudentFirstName","StudentLastName","DateOfBirth" },invalidates_review = true })!.AsObject();
    [Fact]
    public void Exact_partial_decision_preserves_questions_without_preparing_or_approving()
    {
        Assert.True(PentaOnboardingPreparationInput.TryReadPartial(JsonSerializer.SerializeToElement(Decision()),Request,out var input));
        Assert.Equal(new[] { "StudentFirstName","StudentLastName","DateOfBirth" },input!.Questions);
        Assert.False(PentaOnboardingPreparationInput.TryRead(JsonSerializer.SerializeToElement(Decision()),Request,out _));
    }
    [Theory]
    [InlineData("empty")]
    [InlineData("duplicate")]
    [InlineData("unknown")]
    [InlineData("four")]
    [InlineData("number")]
    [InlineData("role")]
    [InlineData("approval")]
    [InlineData("prepare")]
    [InlineData("refine")]
    [InlineData("credentials")]
    [InlineData("missing-kind")]
    [InlineData("invalidate")]
    public void Partial_contract_rejects_unsafe_or_ambiguous_decisions(string mutation)
    {
        var value = Decision();
        switch (mutation) {
            case "empty": value["questions"] = new JsonArray(); break;
            case "duplicate": value["questions"] = new JsonArray("StudentFirstName","StudentFirstName"); break;
            case "unknown": value["questions"] = new JsonArray("GrantRole"); break;
            case "four": value["questions"] = new JsonArray("StudentFirstName","StudentLastName","DateOfBirth","ParentEmail"); break;
            case "number": value["questions"] = new JsonArray(JsonValue.Create(1)); break;
            case "role": value["scope"]!["role"] = "Owner"; break;
            case "approval": value["approved"] = true; break;
            case "prepare": value["kind"] = "host_prepare"; break;
            case "refine": value["candidates"]![0]!["edit"]!["mode"] = "refine"; break;
            case "credentials": value["candidates"]![0]!["edit"]!["set"]!["StudentTemporaryPassword"] = JsonSerializer.SerializeToNode(new { kind = "text",value = "synthetic-only" }); break;
            case "missing-kind": value.Remove("kind"); break;
            case "invalidate": value["invalidates_review"] = false; break;
        }
        Assert.False(PentaOnboardingPreparationInput.TryReadPartial(JsonSerializer.SerializeToElement(value),Request,out _));
    }
}
