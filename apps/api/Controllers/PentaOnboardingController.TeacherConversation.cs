using System.Text.Json;
using AcademyDesk.Api.Intelligence.Penta;
using Microsoft.AspNetCore.Mvc;

namespace AcademyDesk.Api.Controllers;

public sealed partial class PentaOnboardingController
{
    [HttpPost(TeacherDraftRoute+"/conversation/turns"),RequestSizeLimit(16*1024)]
    public Task<ActionResult> TeacherConversationTurn(Guid academyId,Guid conversationId,Guid draftId,[FromBody]JsonElement input,
        [FromServices]PentaTeacherCreationWorkflow teachers,CancellationToken token)=>TeacherSafely(teachers,async()=>{
            var result=await teachers.ConversationGatewayAsync(User,academyId,conversationId,draftId,input,token);
            return ToResult(result.HttpStatus,result.Value,result.Error);
        });
    [HttpGet(TeacherDraftRoute+"/conversation/turns/{requestId:guid}/result")]
    public Task<ActionResult> TeacherConversationResult(Guid academyId,Guid conversationId,Guid draftId,Guid requestId,
        [FromServices]PentaTeacherCreationWorkflow teachers,CancellationToken token)=>TeacherSafely(teachers,async()=>{
            var result=await teachers.ConversationResultAsync(User,academyId,conversationId,draftId,requestId,token);
            return ToResult(result.HttpStatus,result.Value,result.Error);
        });
}
