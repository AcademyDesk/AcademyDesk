using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AcademyDesk.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Intelligence.Penta;

public sealed record PentaTeacherCoreOutcome(int HttpStatus,JsonElement? Value=null,string? Error=null);

public sealed partial class PentaTeacherCreationWorkflow
{
    public bool CoreEnabled=>Enabled&&configuration.GetValue<bool>("Penta:TeacherCoreContractsEnabled");
    private sealed record TeacherCoreCall(string Mode,JsonElement? Scope=null,string? OperationRef=null,PentaTeacherCoreDecision? Decision=null);
    public async Task<PentaTeacherCoreOutcome> CoreCurrentAsync(ClaimsPrincipal user,Guid academy,Guid session,Guid draft,CancellationToken token)
    {
        if(!CoreEnabled)return new(404);
        return CoreOutcome(await RunAsync(Action.CoreRead,user,academy,session,draft,null,0,null,null,token,new("current")));
    }
    public async Task<PentaTeacherCoreOutcome> CorePrepareAsync(ClaimsPrincipal user,Guid academy,Guid session,Guid draft,Guid requestId,JsonElement input,bool partial,CancellationToken token)
    {
        if(!CoreEnabled)return new(404);
        if(!PentaTeacherCoreDecision.TryRead(input,requestId,partial,out var decision))return new(400,Error:"The teacher preparation request is invalid.");
        return CoreOutcome(await RunAsync(Action.CorePrepare,user,academy,session,draft,null,0,null,null,token,new(partial?"partial":"prepare",decision!.Scope,Decision:decision)));
    }
    public async Task<PentaTeacherCoreOutcome> CoreOperationAsync(ClaimsPrincipal user,Guid academy,Guid session,Guid draft,string action,JsonElement input,CancellationToken token)
    {
        if(!CoreEnabled)return new(404);
        if(action is not ("operation" or "status" or "execute") || !PentaTeacherCoreDecision.TryBinding(input,action=="execute",out var scope,out var operation,out var binding))return new(400);
        return CoreOutcome(await RunAsync(action=="execute"?Action.Execute:Action.CoreRead,user,academy,session,draft,binding,0,null,null,token,new(action,scope,operation)));
    }
    private static PentaTeacherCoreOutcome CoreOutcome(PentaTeacherCreationOutcome result)=>new(result.HttpStatus,result.CoreValue,result.Error);
    private static string CoreOperationRef(PentaOnboardingDraft row)=>"teacher:"+row.Id.ToString("N")+":"+row.Revision;
    private static object CoreScope(PentaOnboardingDraft row)=>new{tenant_id=row.AcademyId.ToString("D"),actor_id=row.ActorUserId.ToString("D"),
        conversation_id=row.SessionId.ToString("D"),connection_id="academydesk-teacher:"+row.Id.ToString("N"),authority_version=1,context_version=row.ContextVersion+1};
    private async Task<PentaTeacherCreationOutcome> ApplyCoreAsync(PentaOnboardingDraft row,Body body,string authority,TeacherCoreCall call,CancellationToken token)
    {
        var input=call.Decision!;var history=body.CoreRequests??[];
        // Exact request replay: same ID/digest/revision returns the stored result;
        // changed input or replay after a later revision never adopts a new draft.
        var digest=Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(input))));
        var old=history.SingleOrDefault(x=>x.Id==input.RequestId);
        if(old is not null){
            if(old.Digest!=digest || old.Revision!=row.Revision || row.Status!=(call.Mode=="partial"?"AwaitingReview":"Reviewed"))return Conflict();
            if(call.Mode=="prepare"&&body.ApprovalExpiry<=Now)return new(410,Error:"The original teacher preparation approval expired.");
            var replay=call.Mode=="partial"?await CoreSnapshotAsync(row,body,token):new PentaTeacherCoreOutcome(200,Prepared(row,body));
            return new(replay.HttpStatus,Error:replay.Error,CoreValue:replay.Value);
        }
        if(row.Revision!=input.BaseRevision || row.Status is "Succeeded" or "Cancelled" || history.Length>=99)return Conflict();
        var canonical=PentaTeacherCreationFields.NormalizeForCore(input.Payload);
        if(call.Mode=="prepare" && await ValidateAsync(row.AcademyId,canonical,token) is { } invalid)return invalid;
        row.Revision++;row.Status=call.Mode=="partial"?"AwaitingReview":"Reviewed";row.UpdatedAtUtc=Now;
        var expiry=Now.AddMinutes(2);if(expiry>row.ExpiresAtUtc)expiry=row.ExpiresAtUtc;
        body=Seal(row,authority,canonical,call.Mode=="partial"?null:Guid.NewGuid(),call.Mode=="partial"?null:expiry,
            coreRequests:history.Append(new TeacherCoreRequest(input.RequestId,digest,input.BaseRevision,row.Revision,input.Kind)).ToArray(),
            preparationId:call.Mode=="partial"?null:input.RequestId);
        var value=call.Mode=="partial"?await CoreSnapshotAsync(row,body,token):new PentaTeacherCoreOutcome(201,Prepared(row,body));
        if(value.HttpStatus is not (200 or 201))return new(value.HttpStatus,Error:value.Error);
        Audit(row,call.Mode=="partial"?"CorePartialSaved":"CorePrepared");return new(201,CoreValue:value.Value);
    }
    private async Task<PentaTeacherCoreOutcome> CoreSnapshotAsync(PentaOnboardingDraft row,Body body,CancellationToken token)
    {
        if((row.Status is "Authorized" or "Succeeded")&&body.PreparationId is null)
            return new(409,Error:"Review this teacher operation in the manual workspace before starting a new AI operation.");
        PentaTeacherCreationFields.TryRead(body.Payload,out var request);
        // No silent truncation/foreign labels. Preserve a saved inactive branch
        // as a current reference because the existing Manual domain permits it.
        var rows=await db.Branches.AsNoTracking().Where(x=>x.AcademyId==row.AcademyId).OrderBy(x=>x.Name).ThenBy(x=>x.Id)
            .Select(x=>new{x.Id,x.Name,x.IsActive}).Take(101).ToArrayAsync(token);
        if(rows.Length>100 || rows.Any(x=>string.IsNullOrWhiteSpace(x.Name)||x.Name.Length>1900))return new(503,Error:"Use Manual to narrow the teacher branch selection.");
        var choices=rows.Select(x=>new PentaOnboardingBranchChoice(x.Id.ToString("D"),x.Name+(x.IsActive?"":" (inactive)"))).ToArray();
        if(request!.BranchId is { } branch && !choices.Any(x=>x.id==branch.ToString("D")))return new(409,Error:"The teacher branch changed. Review the current draft.");
        using var payload=JsonDocument.Parse(body.Payload);
        var snapshot=JsonSerializer.SerializeToElement(new{version=PentaOperationalPayload.ContractVersion,scope=CoreScope(row),as_of=new DateTimeOffset(Now,TimeSpan.Zero).ToString("O"),catalog_revision=1,
            tools=new[]{new{key=PentaTeacherCreationFields.Tool,revision=1,family="AI-05",capability=new[]{"Executor"},effect="write",
                description="Prepare one teacher without a portal account. Academy Desk independently validates current access, fields, duplicates and branches; separate explicit approval is required to create. Credentials, compensation and complex metadata stay Manual.",
                fields=PentaTeacherCreationFields.Describe(choices),preparation_required=true,approval_required=true,max_items=1}},
            draft=new{@ref=row.Id.ToString("D"),revision=row.Revision,tool_key=PentaTeacherCreationFields.Tool,fields=payload.RootElement.GetProperty("fields"),
                status=row.Status switch{"Authorized"=>"approved","Succeeded"=>"succeeded","Reviewed"=>"awaiting_approval","Cancelled"=>"cancelled",_=>"collecting"},
                operation_ref=body.PreparationId is not null?CoreOperationRef(row):null},evidence=Array.Empty<object>(),availability="ready"});
        if(snapshot.GetRawText().Length>16000)return new(503,Error:"Use Manual to narrow this teacher context.");return new(200,snapshot);
    }
    private static JsonElement Prepared(PentaOnboardingDraft row,Body body)
    {
        using var payload=JsonDocument.Parse(body.Payload);
        return JsonSerializer.SerializeToElement(new{@ref=CoreOperationRef(row),request_ref=body.PreparationId!.Value.ToString("D"),scope=CoreScope(row),tool_key=PentaTeacherCreationFields.Tool,
            tool_revision=1,draft_ref=row.Id.ToString("D"),draft_revision=row.Revision,payload_hash=body.Hash,canonical_fields=payload.RootElement.GetProperty("fields"),
            preview="Create one teacher without a portal account. Review every canonical field; separate explicit host approval is required. Read the recorded receipt after execution; never repeat an unconfirmed creation.",
            approval_ref=body.ApprovalId?.ToString("D"),approval_expires_at=body.ApprovalExpiry is { } expiry?new DateTimeOffset(expiry,TimeSpan.Zero).ToString("O"):null,
            status=row.Status switch{"Authorized"=>"approved","Succeeded"=>"succeeded","Cancelled"=>"cancelled",_=>"awaiting_approval"}});
    }
    private static JsonElement CoreReceipt(PentaOnboardingDraft row,Body body)=>JsonSerializer.SerializeToElement(new{
        operation_ref=CoreOperationRef(row),scope=CoreScope(row),payload_hash=body.Hash,draft_revision=row.Revision,
        status=row.Status switch{"Authorized"=>"approved","Succeeded"=>"succeeded","Cancelled"=>"cancelled",_=>"awaiting_approval"},
        receipt_ref=body.Receipt?.ReceiptId.ToString("D"),audit_ref=body.Receipt?.AuditId.ToString("D"),source_refs=body.Receipt is { } receipt?new[]{receipt.TeacherId.ToString("D")}:Array.Empty<string>(),error_code="none"});
    private static bool ValidCoreHistory(PentaOnboardingDraft row,Body body)
    {
        var history=body.CoreRequests??[];
        if(history.Any(x=>x is null))return false;
        return history.Length<=99 && history.Select(x=>x.Id).Distinct().Count()==history.Length && history.Select(x=>x.Revision).Distinct().Count()==history.Length &&
            history.All(x=>x.Id!=Guid.Empty&&x.BaseRevision is >=1 and <100&&x.Revision==x.BaseRevision+1&&x.Revision<=row.Revision&&
                (x.Kind is "partial" or "prepare")&&x.Digest is {Length:64}&&x.Digest.All(c=>c is >= '0' and <= '9' or >= 'a' and <= 'f')) &&
            (body.PreparationId is not { } id || history.Any(x=>x.Id==id&&x.Kind=="prepare"&&x.Revision==row.Revision));
    }
}
