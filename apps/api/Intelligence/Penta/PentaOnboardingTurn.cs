using System.Data;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Security;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Intelligence.Penta;

public sealed partial class PentaOnboardingDraftService
{
    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    private sealed record CompletedTurn(string Policy,Guid Academy,Guid Actor,Guid Session,Guid Request,
        long Context,long Version,Guid Draft,long BaseRevision,long Revision,string Authority,string PayloadHash,
        string ClaimDigest,string DecisionDigest,string Kind,Guid AuditId,DateTime CompletedAt,JsonElement Value,bool NoChange=false);

    // Untrusted plan -> existing deterministic draft adapter -> turn/audits/cursor
    // in ONE SQL commit. No inference/approval/domain-create completion port.
    public async Task<PentaOnboardingCoreOutcome> CoreTurnAsync(ClaimsPrincipal principal,Guid academy,
        Guid sessionId,Guid requestId,JsonElement input,bool readOnly,CancellationToken token)=>
        await CoreTurnBoundAsync(principal,academy,sessionId,requestId,input,readOnly,token);

    private async Task<PentaOnboardingCoreOutcome> CoreTurnBoundAsync(ClaimsPrincipal principal,Guid academy,
        Guid sessionId,Guid requestId,JsonElement input,bool readOnly,CancellationToken token,
        JsonElement? expectedSnapshot=null,string? hostFailure=null)
    {
        if(!CorePortsEnabled)return new(404);
        PentaOnboardingCompletionInput? completion=null;
        if(hostFailure is not null) {
            if(hostFailure is not ("unavailable" or "invalid_response")||expectedSnapshot is null||
                !PentaOnboardingConversationInput.TryRead(input,true,out var claim)||claim!.RequestId!=requestId)return new(400);
            completion=new(claim,hostFailure,claim.DraftRevision,expectedSnapshot.Value.GetProperty("scope"),null,null,
                PentaOnboardingCompletionInput.NeutralDigest(hostFailure,claim.DraftRevision,[]),[]);
        }
        if(requestId==Guid.Empty || !readOnly && hostFailure is null && (!PentaOnboardingCompletionInput.TryRead(input,out completion)||completion!.Claim.RequestId!=requestId))
            return new(400,Error:"Enter the exact admitted claim and a supported draft decision.");
        if(!SqlReady())return new(503);
        db.ChangeTracker.Clear();identity.ChangeTracker.Clear();
        var original=AcademyIdentityTransaction.Validate(db,identity);
        await using var transaction=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable,token);
        await LockAsync(academy,token);
        await using var linked=await AcademyIdentityTransaction.EnlistAsync(db,identity,transaction,original,token);
        var authority=await AuthorityAsync(principal,academy,token);
        if(authority is null)return new(403);
        var(actor,hash)=authority.Value;
        var session=await db.PentaMiniSessions.SingleOrDefaultAsync(x=>x.Id==sessionId&&x.AcademyId==academy&&x.ActorUserId==actor,token);
        if(session is null)return new(404);
        if(session.ExpiresAtUtc<=Now)return new(410);
        var state=ReadConversationState(session,hash);
        if(state is null || session.Version is <0 or >=9007199254740991)return ConversationConflict();
        var turn=await db.PentaMiniTurns.SingleOrDefaultAsync(x=>x.AcademyId==academy&&x.ActorUserId==actor&&x.SessionId==sessionId&&x.RequestId==requestId,token);
        if(turn is null)return new(404);
        var row=await Owned(academy,actor,sessionId,state.Draft).SingleOrDefaultAsync(token);
        if(row is null)return new(404);
        var body=Read(row);
        if(body is null||body.Authority!=hash||row.ContextVersion!=session.Version)return ConversationConflict();
        if(row.ExpiresAtUtc<=Now||row.Status=="Authorized"&&body.CoreApproval is {} approval&&approval.ExpiresAtUtc<=Now)return new(410);
        if(expectedSnapshot is {} expected) {
            var current=await CoreSnapshotAsync(row,body,Now,token);
            if(current.HttpStatus!=200)return new(current.HttpStatus,Error:current.Error);
            if(!GatewayContextUnchanged(expected,current.Snapshot!.Value))return ConversationConflict();
        }
        var protector=ConversationProtector(session);
        string claimDigest;
        try{claimDigest=protector.Unprotect(turn.InputDigest);}catch(CryptographicException){return ConversationConflict();}
        if(claimDigest.Length!=64||!claimDigest.All(c=>c is >= '0' and <= '9' or >= 'a' and <= 'f'))return ConversationConflict();
        if(!readOnly && (claimDigest!=ConversationDigest(completion!.Claim)||turn.ExpectedVersion!=completion.Claim.ExpectedVersion||
            !JsonElement.DeepEquals(completion.Scope,JsonSerializer.SerializeToElement(CoreScope(row)))))return ConversationConflict();

        if(turn.Status=="OnboardingCompleted")
        {
            var receipt=ReadCompletedTurn(session,state,turn,row,body,claimDigest);
            if(receipt is null || !readOnly && (receipt.DecisionDigest!=completion!.Digest||receipt.BaseRevision!=completion.Claim.DraftRevision))return ConversationConflict();
            var audit=await db.AuditLogs.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==receipt.AuditId&&x.AcademyId==academy&&x.ActorUserId==actor&&
                x.EntityId==sessionId&&x.EntityType=="PentaMiniSession"&&x.Action=="PentaOnboardingConversationCompleted",token);
            if(audit is null||audit.OccurredAtUtc!=receipt.CompletedAt||audit.MetadataJson!=ConversationAuditMetadata(receipt.Version,requestId))return ConversationConflict();
            var current=await CoreSnapshotAsync(row,body,receipt.CompletedAt,token);
            if(current.HttpStatus!=200)return new(current.HttpStatus,Error:current.Error);
            if(!JsonElement.DeepEquals(current.Snapshot!.Value,receipt.Value.GetProperty("snapshot")))return ConversationConflict();
            if(readOnly){ConversationAudit(session,"ResultRead",state.Version,requestId);await db.SaveChangesAsync(token);await transaction.CommitAsync(token);}
            return new(200,receipt.Value); // exact stored receipt; never redispatch
        }
        if(turn.Status!="OnboardingPending"||turn.ProtectedReceipt is not null||state.PendingRequest!=requestId||
            session.PendingRequestId!=requestId||state.Version!=turn.ExpectedVersion+1)return ConversationConflict();
        if(readOnly)
        {
            ConversationAudit(session,"ResultRead",state.Version,requestId);
            await db.SaveChangesAsync(token);await transaction.CommitAsync(token);
            return new(202,JsonSerializer.SerializeToElement(new{conversationId=sessionId,requestId,version=state.Version,status="pending",canExecute=false}));
        }
        if(row.Revision!=completion!.Claim.DraftRevision||body.Hash!=completion.Claim.PayloadHash||row.Status is "Succeeded" or "Cancelled")return ConversationConflict();
        // Helpers are private, caller-owned transaction participants. The pending
        // marker remains set while they validate/change the draft. No HTTP loop,
        // nested transaction or release-before-dispatch race.
        var noChange=completion.Preparation is null&&completion.Lifecycle is null;
        var questions=completion.Questions??[];
        if(noChange&&completion.Kind=="clarify") {
            var report=await CoreValidationReportAsync(row,body.Payload,requestId,token);
            var missing=report.GetProperty("issues").EnumerateArray().Where(x=>x.GetProperty("code").GetString()=="required")
                .Select(x=>x.GetProperty("field").GetString()).ToHashSet(StringComparer.Ordinal);
            if(questions.Any(x=>!missing.Contains(x)))return new(400,Error:"Ask only for current missing required details.");
        }
        var outcome=noChange?new PentaOnboardingCoreOutcome(200):completion.Kind switch {
            "clarify"=>await ApplyCorePartialAsync(row,body,hash,requestId,completion.Preparation!,token),
            "host_prepare"=>await ApplyCorePreparationAsync(row,body,hash,requestId,completion.Preparation!,token),
            _=>await ApplyCoreTransitionAsync(row,body,hash,requestId,completion.Lifecycle!,token) };
        if(outcome.HttpStatus is not (200 or 201))return outcome;
        body=Read(row);
        if(body is null||row.Revision!=completion.BaseRevision+(noChange?0:1)||!noChange&&(body.CoreApproval is not null||body.AuthorizesCreation))return ConversationConflict();
        var completedAt=Now;
        var snapshot=await CoreSnapshotAsync(row,body,completedAt,token);
        if(snapshot.HttpStatus!=200)return new(snapshot.HttpStatus,Error:snapshot.Error);
        var value=JsonSerializer.SerializeToElement(new{conversationId=sessionId,requestId,version=state.Version,draftId=row.Id,draftRevision=row.Revision,
            kind=completion.Kind,status=TurnStatus(completion.Kind,noChange),snapshot=snapshot.Snapshot,
            operations=completion.Kind=="host_prepare"?new[]{outcome.Value!.Value}:Array.Empty<JsonElement>(),questions,canExecute=false});
        var completedAudit=ConversationAudit(session,"Completed",state.Version,requestId);
        // A real clock advances between calls. Persist the exact receipt instant,
        // not a second independently sampled audit time, for deterministic replay.
        completedAudit.OccurredAtUtc=completedAt;
        var stored=new CompletedTurn(ConversationPolicy,academy,actor,sessionId,requestId,session.Version,state.Version,row.Id,
            completion.BaseRevision,row.Revision,hash,body.Hash,claimDigest,completion.Digest,completion.Kind,completedAudit.Id,completedAt,value,noChange);
        var serialized=JsonSerializer.Serialize(stored);
        if(serialized.Length>40000)return new(503,Error:"The verified turn exceeds the supported receipt bound.");
        turn.ProtectedReceipt=protector.Protect(serialized);turn.Status="OnboardingCompleted";turn.UpdatedAtUtc=completedAt;
        session.ProtectedOnboardingState=protector.Protect(JsonSerializer.Serialize(state with{PendingRequest=null}));
        session.PendingRequestId=null;session.UpdatedAtUtc=completedAt;
        // Recheck deadline after adapter work; an expired claim cannot commit.
        if(row.ExpiresAtUtc<=Now||session.ExpiresAtUtc<=Now)return new(410);
        await db.SaveChangesAsync(token);
        if(row.ExpiresAtUtc<=Now||session.ExpiresAtUtc<=Now)return new(410);
        await transaction.CommitAsync(token);
        return new(201,value);
    }

    private CompletedTurn? ReadCompletedTurn(PentaMiniSession session,ConversationState state,PentaMiniTurn turn,
        PentaOnboardingDraft row,Body body,string digest)
    {
        try {
            if(turn.ProtectedReceipt is null||session.PendingRequestId is not null||state.PendingRequest is not null)return null;
            var receipt=JsonSerializer.Deserialize<CompletedTurn>(ConversationProtector(session).Unprotect(turn.ProtectedReceipt));
            if(receipt is null||receipt.Policy!=ConversationPolicy||receipt.Academy!=session.AcademyId||receipt.Actor!=session.ActorUserId||receipt.Session!=session.Id||
                receipt.Request!=turn.RequestId||receipt.Context!=session.Version||receipt.Version!=state.Version||receipt.Version!=turn.ExpectedVersion+1||
                receipt.Draft!=row.Id||receipt.BaseRevision is <1 or >=100||receipt.Revision!=receipt.BaseRevision+(receipt.NoChange?0:1)||receipt.Revision!=row.Revision||
                receipt.Authority!=body.Authority||receipt.PayloadHash!=body.Hash||receipt.ClaimDigest!=digest||receipt.DecisionDigest.Length!=64||
                !receipt.DecisionDigest.All(c=>c is >= '0' and <= '9' or >= 'a' and <= 'f')||
                receipt.AuditId==Guid.Empty||receipt.CompletedAt!=turn.UpdatedAtUtc||receipt.CompletedAt>Now||!receipt.NoChange&&(body.CoreApproval is not null||body.AuthorizesCreation))return null;
            var historyKind=receipt.Kind switch{"clarify"=>"partial","host_prepare"=>"prepare","request_pause" or "request_resume" or "request_cancel"=>receipt.Kind,_=>null};
            var status=receipt.Kind switch{"host_prepare"=>"Prepared","request_pause"=>"Paused","request_cancel"=>"Cancelled",_=>"AwaitingReview"};
            if(receipt.NoChange) {
                if(receipt.Kind is not ("unsupported" or "clarify" or "unavailable" or "invalid_response"))return null;
            }else if(historyKind is null||row.Status!=status||body.CoreRequests?.Any(x=>x.Id==turn.RequestId&&x.Kind==historyKind&&x.BaseRevision==receipt.BaseRevision&&x.Revision==row.Revision)!=true||
                (receipt.Kind=="host_prepare"?body.PreparationRequestId!=turn.RequestId:body.PreparationRequestId is not null))return null;
            var value=receipt.Value;
            var operations=value.GetProperty("operations");
            if(operations.ValueKind!=JsonValueKind.Array || (receipt.Kind=="host_prepare"
                ? operations.GetArrayLength()!=1||!JsonElement.DeepEquals(operations[0],Prepared(row,body,turn.RequestId))
                : operations.GetArrayLength()!=0))return null;
            return value.TryGetProperty("snapshot",out var snapshot)&&snapshot.ValueKind==JsonValueKind.Object&&
                value.GetProperty("conversationId").GetGuid()==session.Id&&value.GetProperty("requestId").GetGuid()==turn.RequestId&&
                value.GetProperty("version").GetInt64()==state.Version&&value.GetProperty("draftId").GetGuid()==row.Id&&value.GetProperty("draftRevision").GetInt64()==row.Revision&&
                value.GetProperty("kind").GetString()==receipt.Kind&&value.GetProperty("status").GetString()==TurnStatus(receipt.Kind,receipt.NoChange)&&!value.GetProperty("canExecute").GetBoolean()?receipt:null;
        }catch(Exception ex)when(ex is CryptographicException or JsonException or ArgumentException or InvalidOperationException or KeyNotFoundException or FormatException){return null;}
    }
    private static string TurnStatus(string kind,bool noChange)=>!noChange?"draft_updated":kind=="clarify"?"clarification_required":kind;
}
