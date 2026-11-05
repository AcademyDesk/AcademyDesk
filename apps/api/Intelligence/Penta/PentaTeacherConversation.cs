using System.Data;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Security;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Intelligence.Penta;

public sealed record PentaTeacherInterpretation(Guid RequestId,string Text,JsonElement Snapshot);
/// <summary>Interpretation only. No domain service, approval or execution capability.</summary>
public interface IPentaTeacherInterpreter { Task<JsonElement> InterpretAsync(PentaTeacherInterpretation input,CancellationToken token); }
/// <summary>Disposable QA marker only; no real/private runtime implements this marker.</summary>
public interface IPentaTeacherSyntheticInterpreter:IPentaTeacherInterpreter { }
public sealed record PentaTeacherConversationOutcome(int HttpStatus,JsonElement? Value=null,string? Error=null,
    [property:JsonIgnore] JsonElement? PlanningSnapshot=null);

public sealed partial class PentaTeacherCreationWorkflow
{
    private const string TeacherTurnPolicy="academy.teacher-conversation.v1";
    private static readonly SemaphoreSlim TeacherPlanningCapacity=new(2,2);
    private bool ConversationEnabled=>CoreEnabled&&configuration.GetValue<bool>("Penta:TeacherConversationContractsEnabled");
    private sealed record TeacherTurnContext(PentaOnboardingDraft Row,Body Body,PentaMiniSession Session,Guid Actor,string Authority);
    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    private sealed record TeacherTurnReceipt(string Policy,Guid Academy,Guid Actor,Guid Session,Guid Draft,Guid Request,
        string Authority,string ClaimDigest,long BaseRevision,long CompletedRevision,string PayloadHash,Guid Audit,JsonElement Value);
    private IDataProtector TeacherTurnProtector(TeacherTurnContext c,Guid request)=>protection.CreateProtector(TeacherTurnPolicy,
        c.Row.AcademyId.ToString("D"),c.Actor.ToString("D"),c.Session.Id.ToString("D"),c.Row.Id.ToString("D"),request.ToString("D"));
    private static string ClaimDigest(PentaTeacherConversationInput input)=>Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(input))));

    // No live interpreter is registered. Testing uses an explicit synthetic
    // marker; a flag/provider object cannot silently activate a real model.
    public async Task<PentaTeacherConversationOutcome> ConversationGatewayAsync(ClaimsPrincipal user,Guid academy,Guid session,Guid draft,JsonElement input,CancellationToken token)
    {
        if(!ConversationEnabled)return new(404);
        if(!PentaTeacherConversationInput.TryRead(input,out var claim))return new(400);
        if(teacherInterpreter is not IPentaTeacherSyntheticInterpreter)return new(503,Error:"Private teacher conversation interpretation is not qualified or connected. Use Manual.");
        if(!await TeacherPlanningCapacity.WaitAsync(0,token))return new(503,Error:"PENTA is occupied. Check any pending request before trying again.");
        Task<JsonElement>? planning=null;
        try{
            var admitted=await TeacherConversationStoreAsync(user,academy,session,draft,async c=>{
                var digest=ClaimDigest(claim!);var turn=await TurnAsync(c,claim!.RequestId,token);
                if(turn is not null)return await TeacherTurnResultAsync(c,turn,claim,digest,token);
                if(c.Session.PendingRequestId is not null||c.Row.Revision!=claim.DraftRevision||c.Body.Hash!=claim.PayloadHash||c.Row.Status is not ("AwaitingReview" or "Reviewed"))return TurnConflict();
                var today=Now.Date;var turns=db.PentaMiniTurns.Where(x=>x.AcademyId==academy&&x.CreatedAtUtc>=today);
                if(await turns.CountAsync(token)>=400||await turns.CountAsync(x=>x.ActorUserId==c.Actor,token)>=40)return new(429,Error:"The conversation request limit has been reached.");
                var snapshot=await CoreSnapshotAsync(c.Row,c.Body,token);if(snapshot.HttpStatus!=200)return new(snapshot.HttpStatus,Error:snapshot.Error);
                c.Session.PendingRequestId=claim.RequestId;
                db.PentaMiniTurns.Add(new(){AcademyId=academy,ActorUserId=c.Actor,SessionId=session,RequestId=claim.RequestId,ExpectedVersion=c.Row.Revision,
                    InputDigest=TeacherTurnProtector(c,claim.RequestId).Protect(digest),Status="TeacherConversationPending",CreatedAtUtc=Now});
                Audit(c.Row,"ConversationClaimed");
                return new(202,TurnSummary(c,claim.RequestId,"pending",null),PlanningSnapshot:snapshot.Value);
            },token);
            // A known request is never interpreted again, including after a lost
            // response. Pending returns202 without redispatch; completed returns200.
            if(admitted.HttpStatus!=202||admitted.PlanningSnapshot is not { } snapshot)return admitted;
            using var deadline=CancellationTokenSource.CreateLinkedTokenSource(token);deadline.CancelAfter(TimeSpan.FromSeconds(30));
            JsonElement decision=default;string? failure=null;
            try{planning=teacherInterpreter.InterpretAsync(new(claim!.RequestId,claim.Text,snapshot),deadline.Token);decision=await planning.WaitAsync(deadline.Token);}
            catch(Exception ex)when(ex is HttpRequestException or IOException or OperationCanceledException or JsonException or InvalidOperationException){
                token.ThrowIfCancellationRequested();failure=ex is JsonException or InvalidOperationException?"invalid_response":"unavailable";
            }
            token.ThrowIfCancellationRequested();
            return await TeacherConversationStoreAsync(user,academy,session,draft,c=>CompleteTeacherTurnAsync(c,claim!,snapshot,decision,failure,token),token);
        }
        finally{
            // A non-cooperative timed-out worker keeps its capacity until it
            // really exits. Late plans are observed, discarded, never applied.
            if(planning is {IsCompleted:false})_=planning.ContinueWith(done=>{_=done.Exception;TeacherPlanningCapacity.Release();},CancellationToken.None,TaskContinuationOptions.ExecuteSynchronously,TaskScheduler.Default);
            else{if(planning is {IsFaulted:true})_=planning.Exception;TeacherPlanningCapacity.Release();}
        }
    }

    public Task<PentaTeacherConversationOutcome> ConversationResultAsync(ClaimsPrincipal user,Guid academy,Guid session,Guid draft,Guid request,CancellationToken token)=>
        !ConversationEnabled?Task.FromResult(new PentaTeacherConversationOutcome(404)):request==Guid.Empty?Task.FromResult(new PentaTeacherConversationOutcome(400)):
        TeacherConversationStoreAsync(user,academy,session,draft,async c=>{
            var turn=await TurnAsync(c,request,token);if(turn is null)return new(404);
            var result=await TeacherTurnResultAsync(c,turn,null,null,token);
            if(result.HttpStatus is 200 or 202)Audit(c.Row,"ConversationResultRead");return result;
        },token);

    private async Task<PentaTeacherConversationOutcome> CompleteTeacherTurnAsync(TeacherTurnContext c,PentaTeacherConversationInput claim,
        JsonElement expected,JsonElement decision,string? failure,CancellationToken token)
    {
        var turn=await TurnAsync(c,claim.RequestId,token);if(turn is null)return new(404);
        var digest=ClaimDigest(claim);var known=await TeacherTurnResultAsync(c,turn,claim,digest,token);
        if(known.HttpStatus!=202)return known;
        if(c.Session.PendingRequestId!=claim.RequestId||c.Row.Revision!=claim.DraftRevision||c.Body.Hash!=claim.PayloadHash)return TurnConflict();
        var current=await CoreSnapshotAsync(c.Row,c.Body,token);if(current.HttpStatus!=200)return new(current.HttpStatus,Error:current.Error);
        if(!new[]{"scope","catalog_revision","tools","draft","evidence","availability"}.All(key=>
            JsonElement.DeepEquals(expected.GetProperty(key),current.Value!.Value.GetProperty(key))))return TurnConflict();
        PentaTeacherCoreDecision? parsed=null;var partial=decision.ValueKind==JsonValueKind.Object&&decision.TryGetProperty("kind",out var kind)&&kind.ValueKind==JsonValueKind.String&&kind.GetString()=="clarify";
        if(failure is null&&(!PentaTeacherCoreDecision.TryRead(decision,claim.RequestId,partial,out parsed)||parsed!.BaseRevision!=c.Row.Revision||
            !JsonElement.DeepEquals(parsed.Scope,JsonSerializer.SerializeToElement(CoreScope(c.Row)))))failure="invalid_response";
        var outcomeKind=failure??(partial?"partial":"prepared");JsonElement? prepared=null;
        if(failure is null){
            var applied=await ApplyCoreAsync(c.Row,c.Body,c.Authority,new(partial?"partial":"prepare",parsed!.Scope,Decision:parsed),token);
            if(applied.HttpStatus is not (200 or 201)){
                // Snapshot construction can fail after a partial edit. That is
                // an aborted completion, not a business refusal to persist.
                if(c.Row.Revision!=claim.DraftRevision)return new(applied.HttpStatus,Error:applied.Error);
                // Business/duplicate refusal occurs before ApplyCore mutates.
                // Return a code-owned validation outcome, never model prose.
                outcomeKind="needs_manual_review";
            }else prepared=applied.CoreValue;
        }
        c.Session.PendingRequestId=null;c.Session.UpdatedAtUtc=Now;
        var body=Read(c.Row);if(body is null)return TurnConflict();
        var audit=Audit(c.Row,"ConversationCompleted");var summary=TurnSummary(c with{Body=body},claim.RequestId,outcomeKind,prepared);
        var receipt=new TeacherTurnReceipt(TeacherTurnPolicy,c.Row.AcademyId,c.Actor,c.Session.Id,c.Row.Id,claim.RequestId,c.Authority,digest,
            claim.DraftRevision,c.Row.Revision,body.Hash,audit.Id,summary);
        turn.Status="TeacherConversationCompleted";turn.UpdatedAtUtc=Now;turn.ProtectedReceipt=TeacherTurnProtector(c,claim.RequestId).Protect(JsonSerializer.Serialize(receipt));
        if(turn.ProtectedReceipt.Length>60000)throw new InvalidOperationException("Teacher turn result bound exceeded.");
        return new(200,summary);
    }

    private async Task<PentaTeacherConversationOutcome> TeacherTurnResultAsync(TeacherTurnContext c,PentaMiniTurn turn,
        PentaTeacherConversationInput? input,string? expectedDigest,CancellationToken token)
    {
        if(turn.Status is not ("TeacherConversationPending" or "TeacherConversationCompleted"))return TurnConflict();
        string digest;try{digest=TeacherTurnProtector(c,turn.RequestId).Unprotect(turn.InputDigest);}catch(CryptographicException){return TurnConflict();}
        if(digest.Length!=64||digest.Any(x=>x is not (>= '0' and <= '9' or >= 'a' and <= 'f'))||expectedDigest is not null&&digest!=expectedDigest||
            input is not null&&turn.ExpectedVersion!=input.DraftRevision)return TurnConflict();
        if(turn.Status=="TeacherConversationPending"){
            if(c.Session.PendingRequestId!=turn.RequestId||c.Row.Revision!=turn.ExpectedVersion)return TurnConflict();
            return new(202,TurnSummary(c,turn.RequestId,"pending",null));
        }
        TeacherTurnReceipt? receipt;
        try{if(turn.ProtectedReceipt is null||turn.ProtectedReceipt.Length>60000)return TurnConflict();receipt=JsonSerializer.Deserialize<TeacherTurnReceipt>(TeacherTurnProtector(c,turn.RequestId).Unprotect(turn.ProtectedReceipt));}
        catch(Exception ex)when(ex is CryptographicException or JsonException){return TurnConflict();}
        if(receipt is null||receipt.Policy!=TeacherTurnPolicy||receipt.Academy!=c.Row.AcademyId||receipt.Actor!=c.Actor||receipt.Session!=c.Session.Id||receipt.Draft!=c.Row.Id||
            receipt.Request!=turn.RequestId||receipt.Authority!=c.Authority||receipt.ClaimDigest!=digest||receipt.BaseRevision!=turn.ExpectedVersion||receipt.CompletedRevision!=c.Row.Revision||receipt.PayloadHash!=c.Body.Hash||
            c.Session.PendingRequestId is not null||!await db.AuditLogs.AsNoTracking().AnyAsync(x=>x.Id==receipt.Audit&&x.EntityId==c.Row.Id&&x.AcademyId==c.Row.AcademyId&&x.ActorUserId==c.Actor&&x.Action=="PentaTeacherDraftConversationCompleted",token))return TurnConflict();
        return new(200,receipt.Value.Clone());
    }

    private async Task<PentaTeacherConversationOutcome> TeacherConversationStoreAsync(ClaimsPrincipal principal,Guid academy,Guid session,Guid draft,
        Func<TeacherTurnContext,Task<PentaTeacherConversationOutcome>> operation,CancellationToken token)
    {
        if(!ConversationEnabled)return new(404);
        if(academy==Guid.Empty||session==Guid.Empty||draft==Guid.Empty)return new(400);
        if(db.ChangeTracker.HasChanges()||identity.ChangeTracker.HasChanges())return new(503);
        string original;try{original=AcademyIdentityTransaction.Validate(db,identity);}catch(InvalidOperationException){return new(503);}
        db.ChangeTracker.Clear();identity.ChangeTracker.Clear();
        await using var transaction=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable,token);
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT [Id] FROM [Academies] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={academy}",token);
        await using var linked=await AcademyIdentityTransaction.EnlistAsync(db,identity,transaction,original,token);
        var authority=await AuthorityAsync(principal,academy,token);if(authority is null)return new(403);var(actor,stamp)=authority.Value;
        var conversation=await db.PentaMiniSessions.SingleOrDefaultAsync(x=>x.Id==session&&x.AcademyId==academy&&x.ActorUserId==actor,token);if(conversation is null)return new(404);
        if(conversation.ExpiresAtUtc<=Now)return new(410);
        var row=await db.PentaOnboardingDrafts.SingleOrDefaultAsync(x=>x.Id==draft&&x.SessionId==session&&x.AcademyId==academy&&x.ActorUserId==actor,token);if(row is null)return new(404);
        var body=Read(row);if(body is null||body.Authority!=stamp||row.ContextVersion!=conversation.Version||row.ContextVersion>=9007199254740991)return TurnConflict();
        if(row.ExpiresAtUtc<=Now)return new(410);
        var result=await operation(new(row,body,conversation,actor,stamp));
        if(result.HttpStatus is >=200 and <300){await db.SaveChangesAsync(token);await transaction.CommitAsync(token);}
        return result;
    }
    private Task<PentaMiniTurn?> TurnAsync(TeacherTurnContext c,Guid request,CancellationToken token)=>db.PentaMiniTurns.SingleOrDefaultAsync(x=>
        x.AcademyId==c.Row.AcademyId&&x.ActorUserId==c.Actor&&x.SessionId==c.Session.Id&&x.RequestId==request,token);
    private static JsonElement TurnSummary(TeacherTurnContext c,Guid request,string kind,JsonElement? prepared)=>JsonSerializer.SerializeToElement(new{
        version="teacher-conversation/1",conversationId=c.Session.Id,draftId=c.Row.Id,requestId=request,draftRevision=c.Row.Revision,payloadHash=c.Body.Hash,
        kind,status=kind=="pending"?"pending":"completed",canExecute=false,domainEffect=false,result=prepared});
    private static PentaTeacherConversationOutcome TurnConflict()=>new(409,Error:"The teacher conversation, request or access changed. Check the same request; do not resubmit it.");
}
