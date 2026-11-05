using System.Text.Json;
using AcademyDesk.Api.Intelligence.Penta;
using Microsoft.AspNetCore.Mvc;

namespace AcademyDesk.Api.Controllers;

// Existing independently audited PENTA controller boundary; no new generic
// audit exemption and no model-accessible public tool registration.
public sealed partial class PentaOnboardingController
{
    private const string TeacherDraftRoute="~/api/academies/{academyId:guid}/penta/teachers/conversations/{conversationId:guid}/drafts/{draftId:guid}";
    [HttpPut(TeacherDraftRoute),RequestSizeLimit(16*1024)]
    public Task<ActionResult> TeacherSave(Guid academyId,Guid conversationId,Guid draftId,[FromBody]JsonElement input,
        [FromServices]PentaTeacherCreationWorkflow teachers,CancellationToken token)=>TeacherSafely(teachers,async()=>{
            if(!Keys(input,"expectedRevision","contextVersion","fields") || !Integer(input,"expectedRevision",0,99,out var revision) ||
                !Integer(input,"contextVersion",0,9007199254740991,out var context) || input.GetProperty("fields").ValueKind!=JsonValueKind.Object)return BadInput();
            var payload=JsonSerializer.Serialize(new{tool_key=PentaTeacherCreationFields.Tool,tool_revision=1,fields=input.GetProperty("fields")});
            var result=await teachers.SaveAsync(User,academyId,conversationId,draftId,revision,context,payload,token);
            return ToResult(result.HttpStatus,result.Draft,result.Error);
        });
    [HttpGet(TeacherDraftRoute)]
    public Task<ActionResult> TeacherRead(Guid academyId,Guid conversationId,Guid draftId,
        [FromServices]PentaTeacherCreationWorkflow teachers,CancellationToken token)=>TeacherSafely(teachers,async()=>{
            var result=await teachers.ReadAsync(User,academyId,conversationId,draftId,token);
            return ToResult(result.HttpStatus,result.Draft,result.Error);
        });
    [HttpPost(TeacherDraftRoute+"/review"),RequestSizeLimit(1024)]
    public Task<ActionResult> TeacherReview(Guid academyId,Guid conversationId,Guid draftId,[FromBody]JsonElement input,
        [FromServices]PentaTeacherCreationWorkflow teachers,CancellationToken token)=>TeacherAction(teachers,academyId,conversationId,draftId,"review",input,token);
    [HttpPost(TeacherDraftRoute+"/approve"),RequestSizeLimit(1024)]
    public Task<ActionResult> TeacherApprove(Guid academyId,Guid conversationId,Guid draftId,[FromBody]JsonElement input,
        [FromServices]PentaTeacherCreationWorkflow teachers,CancellationToken token)=>TeacherAction(teachers,academyId,conversationId,draftId,"approve",input,token);
    [HttpPost(TeacherDraftRoute+"/execute"),RequestSizeLimit(1024)]
    public Task<ActionResult> TeacherExecute(Guid academyId,Guid conversationId,Guid draftId,[FromBody]JsonElement input,
        [FromServices]PentaTeacherCreationWorkflow teachers,CancellationToken token)=>TeacherAction(teachers,academyId,conversationId,draftId,"execute",input,token);
    [HttpPost(TeacherDraftRoute+"/cancel"),RequestSizeLimit(1024)]
    public Task<ActionResult> TeacherCancel(Guid academyId,Guid conversationId,Guid draftId,[FromBody]JsonElement input,
        [FromServices]PentaTeacherCreationWorkflow teachers,CancellationToken token)=>TeacherAction(teachers,academyId,conversationId,draftId,"cancel",input,token);
    private Task<ActionResult> TeacherAction(PentaTeacherCreationWorkflow teachers,Guid academy,Guid session,Guid draft,string action,JsonElement input,CancellationToken token)=>TeacherSafely(teachers,async()=>{
        var approval=Guid.Empty;
        var expected=action=="approve"?new[]{"revision","payloadHash","approvalId","intent"}:
            action=="execute"?new[]{"revision","payloadHash","approvalId"}:new[]{"revision","payloadHash"};
        if(!Keys(input,expected) || !Integer(input,"revision",1,100,out var revision) ||
            input.GetProperty("payloadHash").ValueKind!=JsonValueKind.String || action=="approve" && input.GetProperty("intent").ValueKind!=JsonValueKind.String)return BadInput();
        if((action is "approve" or "execute") && (input.GetProperty("approvalId").ValueKind!=JsonValueKind.String ||
            !input.GetProperty("approvalId").TryGetGuid(out approval) || approval==Guid.Empty))return BadInput();
        var binding=new PentaTeacherCreationBinding(revision,input.GetProperty("payloadHash").GetString()!,approval);
        var result=action switch{
            "review"=>await teachers.ReviewAsync(User,academy,session,draft,binding,token),
            "approve"=>await teachers.ApproveAsync(User,academy,session,draft,binding,input.GetProperty("intent").GetString()!,token),
            "execute"=>await teachers.ExecuteAsync(User,academy,session,draft,binding,token),
            _=>await teachers.CancelAsync(User,academy,session,draft,binding,token)};
        return ToResult(result.HttpStatus,result.Draft,result.Error);
    });
    private Task<ActionResult> TeacherSafely(PentaTeacherCreationWorkflow teachers,Func<Task<ActionResult>> operation)=>Safely(async()=>{
        if(!teachers.Enabled)return NotFound();
        return await operation();
    },"teacher creation");
}
