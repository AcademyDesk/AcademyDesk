using System.Text.Json;
using AcademyDesk.Api.Intelligence.Penta;
using Microsoft.AspNetCore.Mvc;

namespace AcademyDesk.Api.Controllers;

public sealed partial class PentaOnboardingController
{
    [HttpGet(TeacherDraftRoute+"/core-context")]
    public Task<ActionResult> TeacherCoreCurrent(Guid academyId,Guid conversationId,Guid draftId,
        [FromServices]PentaTeacherCreationWorkflow teachers,CancellationToken token)=>TeacherSafely(teachers,async()=>{
            var result=await teachers.CoreCurrentAsync(User,academyId,conversationId,draftId,token);
            return ToResult(result.HttpStatus,result.Value,result.Error);
        });
    [HttpPost(TeacherDraftRoute+"/core/requests/{requestId:guid}/prepare"),RequestSizeLimit(16*1024)]
    public Task<ActionResult> TeacherCorePrepare(Guid academyId,Guid conversationId,Guid draftId,Guid requestId,[FromBody]JsonElement input,
        [FromServices]PentaTeacherCreationWorkflow teachers,CancellationToken token)=>TeacherCorePreparation(teachers,academyId,conversationId,draftId,requestId,input,false,token);
    [HttpPost(TeacherDraftRoute+"/core/requests/{requestId:guid}/partial"),RequestSizeLimit(16*1024)]
    public Task<ActionResult> TeacherCorePartial(Guid academyId,Guid conversationId,Guid draftId,Guid requestId,[FromBody]JsonElement input,
        [FromServices]PentaTeacherCreationWorkflow teachers,CancellationToken token)=>TeacherCorePreparation(teachers,academyId,conversationId,draftId,requestId,input,true,token);
    private Task<ActionResult> TeacherCorePreparation(PentaTeacherCreationWorkflow teachers,Guid academy,Guid session,Guid draft,Guid request,JsonElement input,bool partial,CancellationToken token)=>TeacherSafely(teachers,async()=>{
        var result=await teachers.CorePrepareAsync(User,academy,session,draft,request,input,partial,token);
        return ToResult(result.HttpStatus,!partial&&(result.HttpStatus is 200 or 201)&&result.Value is { } value?new[]{value}:result.Value,result.Error);
    });
    [HttpPost(TeacherDraftRoute+"/core/operation"),RequestSizeLimit(2048)]
    public Task<ActionResult> TeacherCoreOperation(Guid academyId,Guid conversationId,Guid draftId,[FromBody]JsonElement input,
        [FromServices]PentaTeacherCreationWorkflow teachers,CancellationToken token)=>TeacherCoreBound(teachers,academyId,conversationId,draftId,"operation",input,token);
    [HttpPost(TeacherDraftRoute+"/core/status"),RequestSizeLimit(2048)]
    public Task<ActionResult> TeacherCoreStatus(Guid academyId,Guid conversationId,Guid draftId,[FromBody]JsonElement input,
        [FromServices]PentaTeacherCreationWorkflow teachers,CancellationToken token)=>TeacherCoreBound(teachers,academyId,conversationId,draftId,"status",input,token);
    [HttpPost(TeacherDraftRoute+"/core/execute"),RequestSizeLimit(2048)]
    public Task<ActionResult> TeacherCoreExecute(Guid academyId,Guid conversationId,Guid draftId,[FromBody]JsonElement input,
        [FromServices]PentaTeacherCreationWorkflow teachers,CancellationToken token)=>TeacherCoreBound(teachers,academyId,conversationId,draftId,"execute",input,token);
    private Task<ActionResult> TeacherCoreBound(PentaTeacherCreationWorkflow teachers,Guid academy,Guid session,Guid draft,string action,JsonElement input,CancellationToken token)=>TeacherSafely(teachers,async()=>{
        var result=await teachers.CoreOperationAsync(User,academy,session,draft,action,input,token);
        return ToResult(result.HttpStatus,result.Value,result.Error);
    });
}
