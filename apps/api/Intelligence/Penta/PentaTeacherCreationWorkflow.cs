using System.Data;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Infrastructure;
using AcademyDesk.Api.Security;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Intelligence.Penta;

public sealed record PentaTeacherCreationBinding(long Revision, string PayloadHash, Guid ApprovalId = default);
public sealed record PentaTeacherCreationReceipt(Guid ReceiptId, Guid TeacherId, Guid AuditId, DateTime CompletedAtUtc);
public sealed record PentaTeacherCreationView(Guid Id, Guid SessionId, long ContextVersion, long Revision,
    string Status, DateTime ExpiresAtUtc, string CanonicalPayload, string PayloadHash,
    Guid? ApprovalId, DateTime? ApprovalExpiresAtUtc, PentaTeacherCreationReceipt? Receipt);
public sealed record PentaTeacherCreationOutcome(int HttpStatus, PentaTeacherCreationView? Draft = null, string? Error = null,
    [property:JsonIgnore] JsonElement? CoreValue = null);

/// <summary>
/// Independently gated host workflow candidate. No model, catalog, inference or
/// automatic execution registration. Reuses the existing encrypted draft/session
/// store with a distinct protection purpose; never adopts a student draft.
/// </summary>
public sealed partial class PentaTeacherCreationWorkflow(AcademyDeskDbContext db, IdentityDbContext identity,
    IDataProtectionProvider protection, TimeProvider clock, IConfiguration configuration,
    IHostEnvironment environment, TeacherCreationService creation, IPentaTeacherInterpreter? teacherInterpreter = null)
{
    public const string CreationIntent = "CreateTeacherWithoutAccount";
    private const string Policy = "academy.teacher-creation.v1";
    public bool Enabled => environment.IsEnvironment("Testing") &&
        configuration.GetValue<bool>("Penta:TeacherCreationContractsEnabled");
    private DateTime Now => clock.GetUtcNow().UtcDateTime;
    private enum Action { Save, Read, Review, Approve, Execute, Cancel, CoreRead, CorePrepare }
    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    private sealed record Body(string Policy, Guid Id, Guid Academy, Guid Actor, Guid Session,
        long Context, long Revision, string Status, DateTime Expiry, string Authority,
        string Payload, string Hash, Guid? ApprovalId = null, DateTime? ApprovalExpiry = null,
        PentaTeacherCreationReceipt? Receipt = null, TeacherCoreRequest[]? CoreRequests = null, Guid? PreparationId = null);
    private sealed record TeacherCoreRequest(Guid Id,string Digest,long BaseRevision,long Revision,string Kind);

    public Task<PentaTeacherCreationOutcome> SaveAsync(ClaimsPrincipal user, Guid academy, Guid session,
        Guid draft, long expectedRevision, long context, string payload, CancellationToken token) =>
        RunAsync(Action.Save,user,academy,session,draft,new(expectedRevision,""),context,payload,null,token);
    public Task<PentaTeacherCreationOutcome> ReadAsync(ClaimsPrincipal user, Guid academy, Guid session,
        Guid draft, CancellationToken token) => RunAsync(Action.Read,user,academy,session,draft,null,0,null,null,token);
    public Task<PentaTeacherCreationOutcome> ReviewAsync(ClaimsPrincipal user, Guid academy, Guid session,
        Guid draft, PentaTeacherCreationBinding binding, CancellationToken token) =>
        RunAsync(Action.Review,user,academy,session,draft,binding,0,null,null,token);
    public Task<PentaTeacherCreationOutcome> ApproveAsync(ClaimsPrincipal user, Guid academy, Guid session,
        Guid draft, PentaTeacherCreationBinding binding, string intent, CancellationToken token) =>
        RunAsync(Action.Approve,user,academy,session,draft,binding,0,null,intent,token);
    public Task<PentaTeacherCreationOutcome> ExecuteAsync(ClaimsPrincipal user, Guid academy, Guid session,
        Guid draft, PentaTeacherCreationBinding binding, CancellationToken token) =>
        RunAsync(Action.Execute,user,academy,session,draft,binding,0,null,null,token);
    public Task<PentaTeacherCreationOutcome> CancelAsync(ClaimsPrincipal user, Guid academy, Guid session,
        Guid draft, PentaTeacherCreationBinding binding, CancellationToken token) =>
        RunAsync(Action.Cancel,user,academy,session,draft,binding,0,null,null,token);

    private async Task<PentaTeacherCreationOutcome> RunAsync(Action action, ClaimsPrincipal principal,
        Guid academy, Guid session, Guid draft, PentaTeacherCreationBinding? binding, long context,
        string? payload, string? intent, CancellationToken token, TeacherCoreCall? core = null)
    {
        if (!Enabled) return new(404);
        if (core is not null && !CoreEnabled) return new(404);
        if (academy==Guid.Empty || session==Guid.Empty || draft==Guid.Empty) return new(400);
        if (action==Action.Save && (binding!.Revision is <0 or >=100 || context is <0 or >9007199254740991 ||
            !PentaTeacherCreationFields.TryRead(payload,out _))) return new(400,Error:"Enter valid teacher fields and draft revision.");
        if (action is not (Action.Save or Action.Read or Action.CoreRead or Action.CorePrepare) && (binding is null || binding.Revision is <1 or >100 ||
            binding.PayloadHash is not { Length:64 } || binding.PayloadHash.Any(c=>c is not (>= '0' and <= '9' or >= 'a' and <= 'f')))) return new(400);
        if (action==Action.Approve && intent!=CreationIntent) return new(400,Error:"Explicit teacher creation confirmation is required.");
        if (db.ChangeTracker.HasChanges() || identity.ChangeTracker.HasChanges()) return new(503);
        string original;
        try { original=AcademyIdentityTransaction.Validate(db,identity); }
        catch(InvalidOperationException) { return new(503); }
        db.ChangeTracker.Clear(); identity.ChangeTracker.Clear();
        await using var transaction=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable,token);
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT [Id] FROM [Academies] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={academy}",token);
        await using var linked=await AcademyIdentityTransaction.EnlistAsync(db,identity,transaction,original,token);
        var authority=await AuthorityAsync(principal,academy,token);
        if(authority is null)return new(403);
        var (actor,stamp)=authority.Value;
        var conversation=await db.PentaMiniSessions.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==session &&
            x.AcademyId==academy && x.ActorUserId==actor && x.PendingRequestId==null && x.ExpiresAtUtc>Now,token);
        if(conversation is null || action==Action.Save && conversation.Version!=context)return Conflict();
        var row=await db.PentaOnboardingDrafts.SingleOrDefaultAsync(x=>x.Id==draft && x.AcademyId==academy &&
            x.ActorUserId==actor && x.SessionId==session,token);
        Body? body=row is null?null:Read(row);
        if(row is not null && (body is null || body.Authority!=stamp || row.ContextVersion!=conversation.Version))return Conflict();
        if(row is not null && row.Status!="Succeeded" && row.ExpiresAtUtc<=Now)return new(410,Error:"This teacher draft expired. Prepare a new review.");
        if(core is not null){
            if(row is null)return new(404);
            if(row.ContextVersion>=9007199254740991 || core.Scope is { } scope && !JsonElement.DeepEquals(scope,JsonSerializer.SerializeToElement(CoreScope(row))))return Conflict();
            if((core.Mode is "operation" or "status" or "execute") && (body!.PreparationId is null || core.OperationRef!=CoreOperationRef(row) ||
                binding is null || binding.Revision!=row.Revision || binding.PayloadHash!=body.Hash))return Conflict();
        }
        if(action==Action.CorePrepare){
            var prepared=await ApplyCoreAsync(row!,body!,stamp,core!,token);
            if(prepared.HttpStatus is not (200 or 201))return prepared;
            await db.SaveChangesAsync(token);await transaction.CommitAsync(token);return prepared;
        }
        if(action==Action.CoreRead){
            if(row!.Status=="Succeeded" && !await ReceiptValidAsync(row,body!,token))return Conflict();
            if((row.Status is "Reviewed" or "Authorized") && body!.ApprovalExpiry<=Now)return new(410,Error:"The teacher review approval expired.");
            var value=core!.Mode=="current"?await CoreSnapshotAsync(row,body!,token):
                core.Mode=="operation"?new PentaTeacherCoreOutcome(200,Prepared(row,body!)):new PentaTeacherCoreOutcome(200,CoreReceipt(row,body!));
            if(value.HttpStatus!=200)return new(value.HttpStatus,Error:value.Error);
            Audit(row,"CoreRead");await db.SaveChangesAsync(token);await transaction.CommitAsync(token);return new(200,CoreValue:value.Value);
        }
        if(action==Action.Save)
        {
            var canonical=PentaOperationalPayload.Canonicalize(payload!);
            if(row is null){
                if(binding!.Revision!=0 || await db.PentaOnboardingDrafts.AnyAsync(x=>x.Id==draft,token))return Conflict();
                var today=Now.Date;var daily=db.PentaOnboardingDrafts.Where(x=>x.AcademyId==academy && x.CreatedAtUtc>=today);
                if(await daily.CountAsync(token)>=100 || await daily.CountAsync(x=>x.ActorUserId==actor,token)>=20)return new(429,Error:"The conversation draft limit has been reached.");
                row=new(){Id=draft,AcademyId=academy,ActorUserId=actor,SessionId=session,ContextVersion=context,
                    Revision=1,Status="AwaitingReview",CreatedAtUtc=Now,ExpiresAtUtc=conversation.ExpiresAtUtc<Now.AddMinutes(10)?conversation.ExpiresAtUtc:Now.AddMinutes(10),ProtectedBody=""};
                db.PentaOnboardingDrafts.Add(row);
            }else{
                if(row.Status is "Succeeded" or "Cancelled")return Conflict();
                if(row.Revision==binding!.Revision+1 && body!.Payload==canonical)return new(200,View(row,body));
                if(row.Revision!=binding.Revision)return Conflict();
                row.Revision++;row.Status="AwaitingReview";row.UpdatedAtUtc=Now;
            }
            body=Seal(row,stamp,canonical,coreRequests:body?.CoreRequests);Audit(row,"Saved");
        }
        else
        {
            if(row is null)return new(404);
            if(action!=Action.Read && (binding!.Revision!=row.Revision || binding.PayloadHash!=body!.Hash))return Conflict();
            if(row.Status=="Succeeded"){
                if(action is not (Action.Read or Action.Execute) || action==Action.Execute && binding!.ApprovalId!=body!.ApprovalId)return Conflict();
                if(!await ReceiptValidAsync(row,body!,token))return new(409,Error:"The recorded outcome needs manual reconciliation. Do not repeat creation.");
                if(action==Action.Execute)return new(200,View(row,body!),CoreValue:core is null?null:CoreReceipt(row,body!)); // no redispatch
            }
            if(action==Action.Read)Audit(row,"Read");
            else if(action==Action.Cancel){
                if(row.Status=="Cancelled")return new(200,View(row,body!));
                row.Status="Cancelled";row.UpdatedAtUtc=Now;body=Seal(row,stamp,body!.Payload,coreRequests:body.CoreRequests,preparationId:body.PreparationId);Audit(row,"Cancelled");
            }
            else if(action==Action.Review){
                if(row.Status=="Reviewed" && body!.ApprovalExpiry>Now)return new(200,View(row,body));
                if(row.Status is not ("AwaitingReview" or "Reviewed"))return Conflict();
                if(await ValidateAsync(academy,body!.Payload,token) is { } invalid)return invalid;
                row.Status="Reviewed";row.UpdatedAtUtc=Now;
                var expiry=Now.AddMinutes(2);if(expiry>row.ExpiresAtUtc)expiry=row.ExpiresAtUtc;
                body=Seal(row,stamp,body.Payload,Guid.NewGuid(),expiry,coreRequests:body.CoreRequests,preparationId:body.PreparationId);Audit(row,"Reviewed");
            }
            else if(action is Action.Approve or Action.Execute){
                if(row.Status is not ("Reviewed" or "Authorized") || binding!.ApprovalId==Guid.Empty ||
                    binding.ApprovalId!=body!.ApprovalId)return Conflict();
                if(body.ApprovalExpiry<=Now)return new(410,Error:"The teacher creation approval expired.");
                if(await ValidateAsync(academy,body.Payload,token) is { } invalid)return invalid;
                if(action==Action.Approve){
                    if(row.Status=="Authorized")return new(200,View(row,body));
                    row.Status="Authorized";row.UpdatedAtUtc=Now;
                    body=Seal(row,stamp,body.Payload,body.ApprovalId,body.ApprovalExpiry,coreRequests:body.CoreRequests,preparationId:body.PreparationId);Audit(row,"Authorized");
                }else{
                    if(row.Status!="Authorized")return Conflict();
                    PentaTeacherCreationFields.TryRead(body.Payload,out var request);
                    var outcome=await creation.CreateAsync(principal,academy,request!,token);
                    if(outcome.HttpStatus!=201 || outcome.Result is null)return new(outcome.HttpStatus,Error:outcome.Error);
                    if(body.ApprovalExpiry<=Now || row.ExpiresAtUtc<=Now)return new(410,Error:"The teacher creation approval expired before completion.");
                    var audit=Audit(row,"Executed",outcome.Result.Id);
                    var receipt=new PentaTeacherCreationReceipt(Guid.NewGuid(),outcome.Result.Id,audit.Id,Now);
                    row.Status="Succeeded";row.UpdatedAtUtc=Now;
                    body=Seal(row,stamp,body.Payload,body.ApprovalId,body.ApprovalExpiry,receipt,body.CoreRequests,body.PreparationId);
                }
            }
            else return Conflict();
        }
        await db.SaveChangesAsync(token);
        if(action==Action.Execute && (body!.ApprovalExpiry<=Now || row!.ExpiresAtUtc<=Now))return new(410,Error:"The teacher creation approval expired before commit.");
        await transaction.CommitAsync(token);
        return new(action is Action.Save or Action.Execute?201:200,View(row!,body!),CoreValue:core is null?null:CoreReceipt(row!,body!));
    }

    private async Task<PentaTeacherCreationOutcome?> ValidateAsync(Guid academy,string payload,CancellationToken token)
    {
        if(!PentaTeacherCreationFields.TryRead(payload,out var request) || !PentaTeacherCreationFields.HasRequiredFields(payload))
            return new(400,Error:"Complete the teacher first and last name before review.");
        if(request!.BranchId is { } branch && !await db.Branches.AsNoTracking().AnyAsync(x=>x.Id==branch && x.AcademyId==academy,token))
            return new(400,Error:"The selected branch does not belong to this academy.");
        // Conservative conversational review hold, NOT a new Manual uniqueness rule.
        // Includes inactive records. No foreign-tenant result or automatic merge.
        var first=request.FirstName.Trim().ToUpperInvariant();var last=request.LastName.Trim().ToUpperInvariant();var email=request.Email?.Trim().ToUpperInvariant();
        if(await db.Teachers.AsNoTracking().AnyAsync(x=>x.AcademyId==academy &&
            (x.FirstName.Trim().ToUpper()==first && x.LastName.Trim().ToUpper()==last ||
             email!=null && email!="" && x.Email!=null && x.Email.Trim().ToUpper()==email),token))
            return new(409,Error:"A possible matching teacher already exists. Review existing records in the manual workspace before creating another.");
        return null;
    }
    private async Task<(Guid Actor,string Stamp)?> AuthorityAsync(ClaimsPrincipal principal,Guid academy,CancellationToken token)
    {
        if(principal.Identity?.IsAuthenticated!=true || !Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier),out var actor))return null;
        var user=await identity.Users.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==actor,token);
        if(user is null || !user.IsActive || user.IsPlatformOwner || user.AcademyId!=academy)return null;
        var roles=await (from membership in identity.UserRoles.AsNoTracking() join role in identity.Roles.AsNoTracking() on membership.RoleId equals role.Id
            where membership.UserId==actor select role.Name).ToArrayAsync(token);
        if(!roles.Contains("Owner") && !roles.Contains("AcademyAdmin"))return null;
        var tenant=await db.Academies.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==academy && x.IsActive,token);
        if(tenant is null)return null;
        var stamp=Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new{
            Policy,actor,academy,user.SecurityStamp,user.ConcurrencyStamp,roles=roles.Order(StringComparer.Ordinal),
            tenant.EnabledModulesJson,tenant.SubscriptionPlan,tenant.SubscriptionStatus,tenant.SubscriptionEndsAtUtc,tenant.UpdatedAtUtc}))));
        return(actor,stamp);
    }
    private IDataProtector Protector(PentaOnboardingDraft row)=>protection.CreateProtector(Policy,
        row.AcademyId.ToString("D"),row.ActorUserId.ToString("D"),row.SessionId.ToString("D"),row.Id.ToString("D"));
    private Body Seal(PentaOnboardingDraft row,string authority,string payload,Guid? approval=null,DateTime? expiry=null,PentaTeacherCreationReceipt? receipt=null,
        TeacherCoreRequest[]? coreRequests=null,Guid? preparationId=null)
    {
        var body=new Body(Policy,row.Id,row.AcademyId,row.ActorUserId,row.SessionId,row.ContextVersion,row.Revision,
            row.Status,row.ExpiresAtUtc,authority,payload,PentaOperationalPayload.Digest(payload),approval,expiry,receipt,coreRequests,preparationId);
        row.ProtectedBody=Protector(row).Protect(JsonSerializer.Serialize(body));return body;
    }
    private Body? Read(PentaOnboardingDraft row)
    {
        if(row.ProtectedBody.Length>70000)return null; // bounded encrypted request history, at most99entries
        try{
            var body=JsonSerializer.Deserialize<Body>(Protector(row).Unprotect(row.ProtectedBody));
            return body is not null && body.Policy==Policy && body.Id==row.Id && body.Academy==row.AcademyId && body.Actor==row.ActorUserId &&
                body.Session==row.SessionId && body.Context==row.ContextVersion && body.Revision==row.Revision && body.Status==row.Status &&
                body.Expiry==row.ExpiresAtUtc && PentaTeacherCreationFields.TryRead(body.Payload,out _) &&
                PentaOperationalPayload.MatchesDigest(body.Payload,body.Hash) && ValidCoreHistory(row,body) &&
                row.Status is "AwaitingReview" or "Reviewed" or "Authorized" or "Succeeded" or "Cancelled" &&
                (body.Receipt is not null)==(row.Status=="Succeeded") &&
                (row.Status is "Reviewed" or "Authorized" or "Succeeded"
                    ? body.ApprovalId is { } id && id!=Guid.Empty && body.ApprovalExpiry>row.CreatedAtUtc && body.ApprovalExpiry<=row.ExpiresAtUtc
                    : body.ApprovalId is null && body.ApprovalExpiry is null) ? body:null;
        }catch(Exception ex)when(ex is CryptographicException or JsonException or ArgumentException){return null;}
    }
    private async Task<bool> ReceiptValidAsync(PentaOnboardingDraft row,Body body,CancellationToken token)
    {
        var receipt=body.Receipt;
        return receipt is not null && receipt.ReceiptId!=Guid.Empty &&
            await db.Teachers.AsNoTracking().AnyAsync(x=>x.Id==receipt.TeacherId && x.AcademyId==row.AcademyId,token) &&
            await db.AuditLogs.AsNoTracking().AnyAsync(x=>x.Id==receipt.AuditId && x.AcademyId==row.AcademyId &&
                x.ActorUserId==row.ActorUserId && x.EntityId==row.Id && x.Action=="PentaTeacherDraftExecuted",token);
    }
    private AuditLog Audit(PentaOnboardingDraft row,string outcome,Guid? teacher=null)
    {
        var audit=new AuditLog{AcademyId=row.AcademyId,ActorUserId=row.ActorUserId,EntityType="PentaTeacherDraft",EntityId=row.Id,
            Action="PentaTeacherDraft"+outcome,OccurredAtUtc=Now,MetadataJson=JsonSerializer.Serialize(new{
                row.SessionId,row.ContextVersion,row.Revision,tool=PentaTeacherCreationFields.Tool,policy=Policy,teacherId=teacher,accountsCreated=false})};
        db.AuditLogs.Add(audit);return audit;
    }
    private static PentaTeacherCreationView View(PentaOnboardingDraft row,Body body)=>new(row.Id,row.SessionId,row.ContextVersion,row.Revision,row.Status,
        DateTime.SpecifyKind(row.ExpiresAtUtc,DateTimeKind.Utc),body.Payload,body.Hash,body.ApprovalId,
        body.ApprovalExpiry is { } expiry?DateTime.SpecifyKind(expiry,DateTimeKind.Utc):null,
        body.Receipt is { } receipt?receipt with{CompletedAtUtc=DateTime.SpecifyKind(receipt.CompletedAtUtc,DateTimeKind.Utc)}:null);
    private static PentaTeacherCreationOutcome Conflict()=>new(409,Error:"The teacher draft, conversation or permissions changed. Review the current draft; do not repeat creation.");
}
