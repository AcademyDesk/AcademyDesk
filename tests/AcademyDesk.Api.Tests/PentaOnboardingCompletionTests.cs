using System.Text.Json;
using System.Text.Json.Nodes;
using AcademyDesk.Api.Intelligence.Penta;

namespace AcademyDesk.Api.Tests;

public sealed class PentaOnboardingCompletionTests
{
    private static readonly Guid Request=Guid.Parse("44444444-2222-3333-4444-555555555555");
    private static JsonObject Input(string kind="clarify")=>JsonSerializer.SerializeToNode(new {
        claim=new {requestId=Request,expectedVersion=1,draftRevision=1,payloadHash=new string('a',64),text="Synthetic private prompt"},
        decision=new {request_ref=Request.ToString("D"),scope=new {tenant_id="synthetic",actor_id="synthetic",connection_id="synthetic",conversation_id="synthetic",authority_version=1,context_version=1},
            base_revision=1,catalog_revision=1,kind,candidates=kind.StartsWith("request_",StringComparison.Ordinal)?Array.Empty<object>():new object[]{new {tool_key=PentaOnboardingDraftService.Tool,edit=new {mode="replace",set=new {},remove=Array.Empty<string>()}}},
            questions=kind=="clarify"?new[]{"StudentFirstName"}:Array.Empty<string>(),invalidates_review=true} })!.AsObject();
    [Theory]
    [InlineData("clarify")]
    [InlineData("host_prepare")]
    [InlineData("request_pause")]
    [InlineData("request_resume")]
    [InlineData("request_cancel")]
    public void Only_bounded_existing_draft_adapters_are_admitted(string kind)
    {
        Assert.True(PentaOnboardingCompletionInput.TryRead(JsonSerializer.SerializeToElement(Input(kind)),out var parsed));
        Assert.Equal(kind,parsed!.Kind);Assert.Equal(Request,parsed.Claim.RequestId);Assert.Equal(64,parsed.Digest.Length);
    }
    [Theory]
    [InlineData("execute")]
    [InlineData("approve")]
    [InlineData("sql")]
    [InlineData("extra")]
    [InlineData("tenant")]
    [InlineData("request")]
    [InlineData("base")]
    [InlineData("no-claim")]
    [InlineData("credential")]
    [InlineData("approval")]
    [InlineData("kind-type")]
    [InlineData("null")]
    public void Untrusted_completion_cannot_expand_admission(string mutation)
    {
        var value=Input();
        switch(mutation){
            case "execute":value["decision"]!["kind"]="execute";break;
            case "approve":value["decision"]!["kind"]="approve";break;
            case "sql":value["decision"]!["candidates"]![0]!["tool_key"]="ExecuteSQL";break;
            case "extra":value["succeeded"]=true;break;
            case "tenant":value["claim"]!["tenantId"]="untrusted";break;
            case "request":value["decision"]!["request_ref"]=Guid.NewGuid().ToString("D");break;
            case "base":value["decision"]!["base_revision"]=2;break;
            case "no-claim":value.Remove("claim");break;
            case "credential":value["decision"]!["candidates"]![0]!["edit"]!["set"]!["StudentTemporaryPassword"]=JsonSerializer.SerializeToNode(new{kind="text",value="synthetic"});break;
            case "approval":value["decision"]!["approved"]=true;break;
            case "kind-type":value["decision"]!["kind"]=1;break;
            case "null":value["decision"]=null;break;
        }
        Assert.False(PentaOnboardingCompletionInput.TryRead(JsonSerializer.SerializeToElement(value),out _));
    }
    [Fact]
    public void Validated_partial_questions_survive_completion_materialization()
    {
        Assert.True(PentaOnboardingCompletionInput.TryRead(JsonSerializer.SerializeToElement(Input()),out var parsed));
        Assert.Equal(new[]{"StudentFirstName"},parsed!.Questions);
        Assert.Equal(parsed.Preparation!.Questions,parsed.Questions);
    }
    [Fact]
    public void Duplicate_top_level_claim_cannot_be_silently_replaced()
    {
        using var value=JsonDocument.Parse(Input().ToJsonString().Insert(1,"\"claim\":{},"));
        Assert.False(PentaOnboardingCompletionInput.TryRead(value.RootElement,out _));
    }
    [Fact]
    public void Equivalent_field_order_has_same_semantic_decision_digest()
    {
        var first=Input();var second=Input();
        first["decision"]!["candidates"]![0]!["edit"]!["set"]=JsonNode.Parse("{\"StudentFirstName\":{\"kind\":\"text\",\"value\":\"Synthetic\"},\"StudentLastName\":{\"kind\":\"text\",\"value\":\"Person\"}}");
        second["decision"]!["candidates"]![0]!["edit"]!["set"]=JsonNode.Parse("{\"StudentLastName\":{\"value\":\"Person\",\"kind\":\"text\"},\"StudentFirstName\":{\"value\":\"Synthetic\",\"kind\":\"text\"}}");
        Assert.True(PentaOnboardingCompletionInput.TryRead(JsonSerializer.SerializeToElement(first),out var a));
        Assert.True(PentaOnboardingCompletionInput.TryRead(JsonSerializer.SerializeToElement(second),out var b));
        Assert.Equal(a!.Digest,b!.Digest);
    }
}
