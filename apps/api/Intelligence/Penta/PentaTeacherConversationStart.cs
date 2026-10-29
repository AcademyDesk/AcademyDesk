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

public sealed partial class PentaTeacherCreationWorkflow
{
    private const string TeacherStartPolicy="academy.teacher-conversation-start.v1";
    private bool TeacherStartEnabled=>ConversationEnabled&&configuration.GetValue<bool>("Penta:TeacherStartContractsEnabled");
    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    private sealed record TeacherStartReceipt(string Policy,Guid Academy,Guid Actor,Guid Request,Guid Session,Guid Draft,
        string Authority,long Context,DateTime Expiry,Guid Audit);
    private IDataProtector TeacherStartProtector(PentaMiniSession session,Guid request)=>protection.CreateProtector(TeacherStartPolicy,
        session.AcademyId.ToString("D"),session.ActorUserId.ToString("D"),session.Id.ToString("D"),request.ToString("D"));
    private IDataProtector TeacherSessionProtector(PentaMiniSession session)=>protection.CreateProtector(TeacherStartPolicy,"session",
        session.AcademyId.ToString("D"),session.ActorUserId.ToString("D"),session.Id.ToString("D"));

    public Task<PentaTeacherConversationOutcome> ConversationStartReadinessAsync(ClaimsPrincipal user,Guid academy,CancellationToken token)=>
        TeacherStartBoundAsync(user,academy,"readiness",Guid.Empty,token);
    public Task<PentaTeacherConversationOutcome> ConversationStartResultAsync(ClaimsPrincipal user,Guid academy,Guid request,CancellationToken token)=>
        !TeacherStartEnabled?Task.FromResult(new PentaTeacherConversationOutcome(404)):
        request==Guid.Empty?Task.FromResult(new PentaTeacherConversationOutcome(400)):TeacherStartBoundAsync(user,academy,"result",request,token);
    public Task<PentaTeacherConversationOutcome> ConversationStartAsync(ClaimsPrincipal user,Guid academy,JsonElement input,CancellationToken token)
    {
        if(!TeacherStartEnabled)return Task.FromResult(new PentaTeacherConversationOutcome(404));
        if(input.ValueKind!=JsonValueKind.Object||input.GetRawText().Length>1000||input.EnumerateObject().Count()!=1||
            !input.TryGetProperty("requestId",out var value)||value.ValueKind!=JsonValueKind.String||
            !Guid.TryParseExact(value.GetString(),"D",out var request)||request==Guid.Empty||value.GetString()!=request.ToString("D"))
            return Task.FromResult(new PentaTeacherConversationOutcome(400));
        return TeacherStartBoundAsync(user,academy,"start",request,token);
    }

    private async Task<PentaTeacherConversationOutcome> TeacherStartBoundAsync(ClaimsPrincipal user,Guid academy,string action,Guid request,CancellationToken token)
    {
        if(!TeacherStartEnabled)return new(404);
        if(academy==Guid.Empty)return new(400);
        if(db.ChangeTracker.HasChanges()||identity.ChangeTracker.HasChanges())return new(503);
        string original;try{original=AcademyIdentityTransaction.Validate(db,identity);}catch(InvalidOperationException){return new(503);}
        db.ChangeTracker.Clear();identity.ChangeTracker.Clear();
        await using var transaction=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable,token);
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT [Id] FROM [Academies] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={academy}",token);
        await using var linked=await AcademyIdentityTransaction.EnlistAsync(db,identity,transaction,original,token);
        var authority=await AuthorityAsync(user,academy,token);if(authority is null)return new(403);var(actor,stamp)=authority.Value;
        if(action=="readiness"){
            var (candidate,body)=TeacherStartDraft(academy,actor,stamp,Guid.NewGuid());
            var context=await CoreSnapshotAsync(candidate,body,token);
            var available=teacherInterpreter is IPentaTeacherSyntheticInterpreter&&context.HttpStatus==200;
            db.AuditLogs.Add(new(){AcademyId=academy,ActorUserId=actor,EntityType="Academy",EntityId=academy,
                Action="PentaTeacherStartReadiness",OccurredAtUtc=Now,MetadataJson=JsonSerializer.Serialize(new{policy=TeacherStartPolicy,domainEffect=false})});
            await db.SaveChangesAsync(token);await transaction.CommitAsync(token);
            return new(200,JsonSerializer.SerializeToElement(new{version="teacher-start/1",academyId=academy,available,canStart=available,canExecute=false,syntheticInterpreter=teacherInterpreter is IPentaTeacherSyntheticInterpreter}));
        }
        // The actor/academy-scoped request is an idempotency key, never authority.
        // Find existing receipts before checking provider availability or quota.
        var markers=await db.PentaMiniTurns.Where(x=>x.AcademyId==academy&&x.ActorUserId==actor&&x.RequestId==request).Take(2).ToArrayAsync(token);
        if(markers.Length!=0){
            if(markers.Length!=1||markers[0].Status!="TeacherConversationStarted"||markers[0].ExpectedVersion!=0)return TurnConflict();
            var marker=markers[0];var session=await db.PentaMiniSessions.SingleOrDefaultAsync(x=>x.Id==marker.SessionId&&x.AcademyId==academy&&x.ActorUserId==actor,token);
            if(session is null)return TurnConflict();TeacherStartReceipt? receipt,state;
            try{
                var seal=TeacherStartProtector(session,request);
                if(marker.ProtectedReceipt is null||marker.ProtectedReceipt.Length>6000||session.ProtectedOnboardingState is null||session.ProtectedOnboardingState.Length>6000||
                    seal.Unprotect(marker.InputDigest)!=TeacherStartPolicy||TeacherSessionProtector(session).Unprotect(session.ProtectedState)!=TeacherStartPolicy)return TurnConflict();
                receipt=JsonSerializer.Deserialize<TeacherStartReceipt>(seal.Unprotect(marker.ProtectedReceipt));
                state=JsonSerializer.Deserialize<TeacherStartReceipt>(TeacherSessionProtector(session).Unprotect(session.ProtectedOnboardingState));
            }catch(Exception ex)when(ex is CryptographicException or JsonException or ArgumentException){return TurnConflict();}
            if(receipt is null||state!=receipt||receipt.Policy!=TeacherStartPolicy||receipt.Academy!=academy||receipt.Actor!=actor||receipt.Request!=request||
                receipt.Session!=session.Id||receipt.Draft==Guid.Empty||receipt.Audit==Guid.Empty||receipt.Authority!=stamp||receipt.Context!=1||
                session.Version!=receipt.Context||session.ExpiresAtUtc!=receipt.Expiry)return TurnConflict();
            if(session.ExpiresAtUtc<=Now)return new(410);
            var row=await db.PentaOnboardingDrafts.SingleOrDefaultAsync(x=>x.Id==receipt.Draft&&x.AcademyId==academy&&x.ActorUserId==actor&&x.SessionId==session.Id,token);
            var body=row is null?null:Read(row);
            if(row is null||body is null||body.Authority!=stamp||row.ContextVersion!=session.Version||row.ExpiresAtUtc!=receipt.Expiry||
                !await db.AuditLogs.AsNoTracking().AnyAsync(x=>x.Id==receipt.Audit&&x.AcademyId==academy&&x.ActorUserId==actor&&x.EntityId==session.Id&&x.Action=="PentaTeacherConversationStarted",token))return TurnConflict();
            if(row.Status=="Succeeded"&&!await ReceiptValidAsync(row,body,token))return TurnConflict();
            // Startup recovers the immutable identity, not stale approval/field data.
            // It is valid during pending interpretation, with no dispatch or rearm.
            TeacherStartAudit(session,action=="result"?"StartRead":"StartReplayed",request);
            await db.SaveChangesAsync(token);await transaction.CommitAsync(token);return new(200,TeacherStartView(receipt));
        }
        if(action=="result")return new(404);
        if(teacherInterpreter is not IPentaTeacherSyntheticInterpreter)return new(503,Error:"Private teacher conversation interpretation is not qualified or connected. Use Manual.");
        var today=Now.Date;var sessions=db.PentaMiniSessions.Where(x=>x.AcademyId==academy&&x.CreatedAtUtc>=today);
        var drafts=db.PentaOnboardingDrafts.Where(x=>x.AcademyId==academy&&x.CreatedAtUtc>=today);var turns=db.PentaMiniTurns.Where(x=>x.AcademyId==academy&&x.CreatedAtUtc>=today);
        if(await sessions.CountAsync(token)>=100||await sessions.CountAsync(x=>x.ActorUserId==actor,token)>=20||
            await drafts.CountAsync(token)>=100||await drafts.CountAsync(x=>x.ActorUserId==actor,token)>=20||
            await turns.CountAsync(token)>=400||await turns.CountAsync(x=>x.ActorUserId==actor,token)>=40)return new(429);
        var created=new PentaMiniSession{AcademyId=academy,ActorUserId=actor,Version=1,CreatedAtUtc=Now,ExpiresAtUtc=Now.AddMinutes(10),ProtectedState=""};
        var (draft,initial)=TeacherStartDraft(academy,actor,stamp,created.Id);draft.ExpiresAtUtc=created.ExpiresAtUtc;initial=Seal(draft,stamp,initial.Payload);
        var snapshot=await CoreSnapshotAsync(draft,initial,token);if(snapshot.HttpStatus!=200)return new(snapshot.HttpStatus,Error:snapshot.Error);
        db.PentaMiniSessions.Add(created);db.PentaOnboardingDrafts.Add(draft);Audit(draft,"Saved");
        var audit=TeacherStartAudit(created,"Started",request);
        var saved=new TeacherStartReceipt(TeacherStartPolicy,academy,actor,request,created.Id,draft.Id,stamp,created.Version,created.ExpiresAtUtc,audit.Id);
        var protector=TeacherStartProtector(created,request);created.ProtectedState=TeacherSessionProtector(created).Protect(TeacherStartPolicy);
        created.ProtectedOnboardingState=TeacherSessionProtector(created).Protect(JsonSerializer.Serialize(saved));
        db.PentaMiniTurns.Add(new(){AcademyId=academy,ActorUserId=actor,SessionId=created.Id,RequestId=request,ExpectedVersion=0,
            Status="TeacherConversationStarted",CreatedAtUtc=Now,InputDigest=protector.Protect(TeacherStartPolicy),ProtectedReceipt=protector.Protect(JsonSerializer.Serialize(saved))});
        await db.SaveChangesAsync(token);if(created.ExpiresAtUtc<=Now)return new(410);await transaction.CommitAsync(token);
        return new(201,TeacherStartView(saved));
    }
    private (PentaOnboardingDraft Row,Body Body) TeacherStartDraft(Guid academy,Guid actor,string authority,Guid session)
    {
        var row=new PentaOnboardingDraft{AcademyId=academy,ActorUserId=actor,SessionId=session,ContextVersion=1,Revision=1,Status="AwaitingReview",
            CreatedAtUtc=Now,ExpiresAtUtc=Now.AddMinutes(10),ProtectedBody=""};
        var payload=PentaOperationalPayload.Canonicalize(JsonSerializer.Serialize(new{tool_key=PentaTeacherCreationFields.Tool,tool_revision=1,fields=new{}}));
        return(row,Seal(row,authority,payload));
    }
    private AuditLog TeacherStartAudit(PentaMiniSession session,string action,Guid request)
    {
        var audit=new AuditLog{AcademyId=session.AcademyId,ActorUserId=session.ActorUserId,EntityType="PentaTeacherConversation",EntityId=session.Id,
            Action="PentaTeacherConversation"+action,OccurredAtUtc=Now,MetadataJson=JsonSerializer.Serialize(new{policy=TeacherStartPolicy,requestId=request,domainEffect=false})};
        db.AuditLogs.Add(audit);return audit;
    }
    private static JsonElement TeacherStartView(TeacherStartReceipt receipt)=>JsonSerializer.SerializeToElement(new{
        version="teacher-start/1",academyId=receipt.Academy,requestId=receipt.Request,conversationId=receipt.Session,draftId=receipt.Draft,
        expiresAtUtc=DateTime.SpecifyKind(receipt.Expiry,DateTimeKind.Utc),status="conversation_started",canExecute=false,domainEffect=false});
}
