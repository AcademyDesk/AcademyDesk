using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Nodes;
using AcademyDesk.Api.Intelligence.Penta;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace AcademyDesk.Api.Tests;

public sealed class PentaTeacherCoreTests
{
    private static readonly Guid Request=Guid.NewGuid();
    private static JsonObject Decision()=>JsonSerializer.SerializeToNode(new{request_ref=Request.ToString("D"),
        scope=new{tenant_id=Guid.NewGuid().ToString("D"),actor_id=Guid.NewGuid().ToString("D"),connection_id="academydesk-teacher:test",conversation_id=Guid.NewGuid().ToString("D"),authority_version=1,context_version=1},
        base_revision=1,catalog_revision=1,kind="host_prepare",invalidates_review=true,questions=Array.Empty<string>(),
        candidates=new[]{new{tool_key=PentaTeacherCreationFields.Tool,edit=new{mode="replace",set=new{FirstName=new{kind="text",value=" Synthetic "},LastName=new{kind="text",value=" Teacher "}},remove=Array.Empty<string>()}}}})!.AsObject();
    private static JsonElement Element(JsonNode node)=>JsonSerializer.SerializeToElement(node);
    [Fact]
    public void Strict_decision_keeps_request_scope_revision_and_only_the_teacher_tool()
    {
        var json=Decision();Assert.True(PentaTeacherCoreDecision.TryRead(Element(json),Request,false,out var input));
        Assert.Equal(Request,input!.RequestId);Assert.Equal(1,input.BaseRevision);Assert.Equal("prepare",input.Kind);
        Assert.True(PentaTeacherCreationFields.TryRead(input.Payload,out var request));Assert.Equal(" Synthetic ",request!.FirstName);
        Assert.False(PentaTeacherCoreDecision.TryRead(Element(json),Guid.NewGuid(),false,out _));
    }
    [Theory]
    [InlineData("extra_authority")] [InlineData("unknown_tool")] [InlineData("refine")] [InlineData("role_field")]
    [InlineData("multiple_candidates")] [InlineData("skip_invalidation")] [InlineData("wrong_catalog")]
    [InlineData("approve_kind")] [InlineData("invalid_scope_version")]
    public void Unsafe_or_ambiguous_decisions_fail_before_store_access(string change)
    {
        var json=Decision();var step=json["candidates"]![0]!;
        switch(change){
            case "extra_authority":json["approved"]=true;break;
            case "unknown_tool":step["tool_key"]="AcademyDesk.StudentOnboarding.Create";break;
            case "refine":step["edit"]!["mode"]="refine";break;
            case "role_field":step["edit"]!["set"]!["Role"]=JsonSerializer.SerializeToNode(new{kind="text",value="Owner"});break;
            case "multiple_candidates":json["candidates"]!.AsArray().Add(step.DeepClone());break;
            case "skip_invalidation":json["invalidates_review"]=false;break;
            case "wrong_catalog":json["catalog_revision"]=2;break;
            case "approve_kind":json["kind"]="approve";break;
            case "invalid_scope_version":json["scope"]!["context_version"]=0;break;
        }
        Assert.False(PentaTeacherCoreDecision.TryRead(Element(json),Request,false,out _));
    }
    [Fact]
    public void Partial_details_need_bounded_known_clarification_keys_and_never_authorize_creation()
    {
        var json=Decision();json["kind"]="clarify";json["questions"]=new JsonArray("LastName");
        json["candidates"]![0]!["edit"]!["set"]!.AsObject().Remove("LastName");
        Assert.True(PentaTeacherCoreDecision.TryRead(Element(json),Request,true,out var input));Assert.Equal("partial",input!.Kind);
        Assert.False(PentaTeacherCreationFields.HasRequiredFields(input.Payload));
        json["questions"]=new JsonArray("Password");Assert.False(PentaTeacherCoreDecision.TryRead(Element(json),Request,true,out _));
        json["questions"]=new JsonArray("LastName","LastName");Assert.False(PentaTeacherCoreDecision.TryRead(Element(json),Request,true,out _));
    }
    [Fact]
    public void Canonical_normalization_and_descriptor_retain_manual_trim_semantics_and_required_names()
    {
        var fields=new{FirstName=new{kind="text",value=" Synthetic "},LastName=new{kind="text",value=" Teacher "},Email=new{kind="text",value=" "}};
        var payload=JsonSerializer.Serialize(new{tool_key=PentaTeacherCreationFields.Tool,tool_revision=1,fields});
        var canonical=PentaTeacherCreationFields.NormalizeForCore(payload);Assert.True(PentaTeacherCreationFields.TryRead(canonical,out var request));
        Assert.Equal("Synthetic",request!.FirstName);Assert.Null(request.Email);
        var descriptor=JsonSerializer.SerializeToElement(PentaTeacherCreationFields.Describe([]));
        Assert.Equal(14,descriptor.GetArrayLength());
        Assert.Equal(new[]{"FirstName","LastName"},descriptor.EnumerateArray().Where(x=>x.GetProperty("required").GetBoolean()).Select(x=>x.GetProperty("key").GetString()).ToArray());
        Assert.DoesNotContain(descriptor.EnumerateArray(),x=>x.GetProperty("key").GetString() is "Role" or "CompensationJson" or "Password");
    }
    [Theory]
    [InlineData("Production",true,true)] [InlineData("Development",true,true)] [InlineData("Testing",false,true)] [InlineData("Testing",true,false)]
    public async Task All_Core_ports_require_both_independent_testing_flags_before_store_access(string environment,bool teacher,bool core)
    {
        var config=new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{
            ["Penta:TeacherCreationContractsEnabled"]=teacher.ToString(),["Penta:TeacherCoreContractsEnabled"]=core.ToString()}).Build();
        var service=new PentaTeacherCreationWorkflow(null!,null!,null!,TimeProvider.System,config,new Host(environment),null!);
        var id=Guid.NewGuid();var user=new ClaimsPrincipal();
        Assert.Equal(404,(await service.CoreCurrentAsync(user,id,id,id,default)).HttpStatus);
        Assert.Equal(404,(await service.CorePrepareAsync(user,id,id,id,id,default,false,default)).HttpStatus);
        Assert.Equal(404,(await service.CorePrepareAsync(user,id,id,id,id,default,true,default)).HttpStatus);
        foreach(var action in new[]{"operation","status","execute"})Assert.Equal(404,(await service.CoreOperationAsync(user,id,id,id,action,default,default)).HttpStatus);
    }
    private sealed class Host(string name):IHostEnvironment
    {
        public string EnvironmentName{get;set;}=name;public string ApplicationName{get;set;}="TeacherCoreTest";
        public string ContentRootPath{get;set;}="";public IFileProvider ContentRootFileProvider{get;set;}=new NullFileProvider();
    }
}
