using System.Text.Json;
using AcademyDesk.Api.Intelligence.Penta;
using Microsoft.AspNetCore.Mvc;

namespace AcademyDesk.Api.Controllers;

public sealed partial class PentaOnboardingController
{
    private const string TeacherStartRoute="~/api/academies/{academyId:guid}/penta/teachers";
    [HttpGet(TeacherStartRoute+"/readiness")]
    public Task<ActionResult> TeacherStartReadiness(Guid academyId,[FromServices]PentaTeacherCreationWorkflow teachers,CancellationToken token)=>TeacherSafely(teachers,async()=>{
        var result=await teachers.ConversationStartReadinessAsync(User,academyId,token);return ToResult(result.HttpStatus,result.Value,result.Error);
    });
    [HttpPost(TeacherStartRoute+"/conversations"),RequestSizeLimit(1024)]
    public Task<ActionResult> TeacherStart(Guid academyId,[FromBody]JsonElement input,[FromServices]PentaTeacherCreationWorkflow teachers,CancellationToken token)=>TeacherSafely(teachers,async()=>{
        var result=await teachers.ConversationStartAsync(User,academyId,input,token);return ToResult(result.HttpStatus,result.Value,result.Error);
    });
    [HttpGet(TeacherStartRoute+"/starts/{requestId:guid}")]
    public Task<ActionResult> TeacherStartResult(Guid academyId,Guid requestId,[FromServices]PentaTeacherCreationWorkflow teachers,CancellationToken token)=>TeacherSafely(teachers,async()=>{
        var result=await teachers.ConversationStartResultAsync(User,academyId,requestId,token);return ToResult(result.HttpStatus,result.Value,result.Error);
    });
}
