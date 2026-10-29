using System.Text.Json;
using System.Text.Json.Nodes;
using AcademyDesk.Api.Intelligence.Penta;

namespace AcademyDesk.Api.Tests;

public sealed class PentaOnboardingNeutralTests
{
    private static readonly Guid Request=Guid.Parse("44444444-2222-3333-4444-555555555555");
    private static JsonObject Input(string kind="clarify")=>JsonSerializer.SerializeToNode(new{request_ref=Request.ToString("D"),
        scope=new{tenant_id="synthetic",actor_id="synthetic",connection_id="synthetic",conversation_id="synthetic",authority_version=1,context_version=1},
        base_revision=1,catalog_revision=1,kind,candidates=Array.Empty<object>(),questions=kind=="clarify"?new[]{"StudentFirstName"}:Array.Empty<string>(),invalidates_review=false})!.AsObject();
    [Theory]
    [InlineData("clarify")]
    [InlineData("unsupported")]
    public void Neutral_decision_has_no_candidates_or_invalidation(string kind)
    {
        Assert.True(PentaOnboardingNeutralInput.TryRead(JsonSerializer.SerializeToElement(Input(kind)),Request,out var parsed));Assert.Equal(kind,parsed!.Kind);
        var wrapped=JsonSerializer.SerializeToElement(new{claim=new{requestId=Request,expectedVersion=1,draftRevision=1,payloadHash=new string('a',64),text="synthetic"},decision=Input(kind)});
        Assert.True(PentaOnboardingCompletionInput.TryRead(wrapped,out var completion));Assert.Null(completion!.Preparation);Assert.Null(completion.Lifecycle);
    }
    [Theory]
    [InlineData("approved")]
    [InlineData("unavailable")]
    [InlineData("scope")]
    [InlineData("duplicate-scope")]
    [InlineData("invalidates")]
    [InlineData("candidates")]
    [InlineData("question")]
    [InlineData("duplicates")]
    [InlineData("four")]
    [InlineData("empty")]
    [InlineData("unsupported-question")]
    [InlineData("revision")]
    [InlineData("request")]
    [InlineData("extra")]
    public void Malformed_or_privileged_neutral_output_fails_closed(string mutation)
    {
        var value=Input();switch(mutation){
            case "approved":value["kind"]="approved";break;
            case "unavailable":value["kind"]="unavailable";break;
            case "scope":value["scope"]!["role"]="Owner";break;
            case "duplicate-scope":value["scope"]=JsonNode.Parse("{\"tenant_id\":\"x\"}");break;
            case "invalidates":value["invalidates_review"]=true;break;
            case "candidates":value["candidates"]=new JsonArray(new JsonObject());break;
            case "question":value["questions"]=new JsonArray("GrantRole");break;
            case "duplicates":value["questions"]=new JsonArray("StudentFirstName","StudentFirstName");break;
            case "four":value["questions"]=new JsonArray("StudentFirstName","StudentLastName","DateOfBirth","ParentEmail");break;
            case "empty":value["questions"]=new JsonArray();break;
            case "unsupported-question":value["kind"]="unsupported";break;
            case "revision":value["base_revision"]=0;break;
            case "request":value["request_ref"]=Guid.NewGuid().ToString("D");break;
            case "extra":value["message"]="private model prose";break;
        }
        Assert.False(PentaOnboardingNeutralInput.TryRead(JsonSerializer.SerializeToElement(value),Request,out _));
    }
    [Fact]
    public void Duplicate_keys_are_not_silently_collapsed()
    {
        using var root=JsonDocument.Parse(Input().ToJsonString().Insert(1,"\"kind\":\"unsupported\","));
        Assert.False(PentaOnboardingNeutralInput.TryRead(root.RootElement,Request,out _));
    }
    [Fact]
    public void Private_snapshot_cannot_be_serialized_to_a_client()
    {
        var outcome=new PentaOnboardingCoreOutcome(202,JsonSerializer.SerializeToElement(new{status="pending"}),PlanningSnapshot:JsonSerializer.SerializeToElement(new{secret_marker="synthetic"}));
        Assert.DoesNotContain("secret_marker",JsonSerializer.Serialize(outcome),StringComparison.Ordinal);
    }
}
