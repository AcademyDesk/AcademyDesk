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

public sealed partial class PentaOnboardingDraftService
{
    private const string ConversationPolicy = "academy.onboarding-conversation.v1";
    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    private sealed record ConversationState(string Policy, Guid Academy, Guid Actor, Guid Session,
        long Context, string Authority, long Version, Guid Draft, Guid? PendingRequest);
    private IDataProtector ConversationProtector(PentaMiniSession session) => protection.CreateProtector(
        ConversationPolicy, session.AcademyId.ToString("D"), session.ActorUserId.ToString("D"), session.Id.ToString("D"));

    // Selection/admission only. A claim never invokes inference, authorizes a
    // mutation or reports completion. Pending/unknown claims cannot be recycled.
    public async Task<PentaOnboardingCoreOutcome> CoreConversationAsync(ClaimsPrincipal principal, Guid academy,
        Guid sessionId, string action, JsonElement input, CancellationToken token)
    {
        if (!CorePortsEnabled) return new(404);
        if (action is not ("current" or "select" or "claim" or "send-readiness")) return new(400);
        PentaOnboardingConversationInput? request = null;
        if (action is not ("current" or "send-readiness") && !PentaOnboardingConversationInput.TryRead(input, action == "claim", out request))
            return new(400, Error: "Enter the exact current conversation and draft binding.");
        if (!SqlReady()) return new(503);
        db.ChangeTracker.Clear(); identity.ChangeTracker.Clear();
        var original = AcademyIdentityTransaction.Validate(db, identity);
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, token);
        await LockAsync(academy, token);
        await using var linked = await AcademyIdentityTransaction.EnlistAsync(db, identity, transaction, original, token);
        var authority = await AuthorityAsync(principal, academy, token);
        if (authority is null) return new(403);
        var (actor, hash) = authority.Value;
        var session = await db.PentaMiniSessions.SingleOrDefaultAsync(x => x.Id == sessionId && x.AcademyId == academy && x.ActorUserId == actor, token);
        if (session is null) return new(404);
        if (session.ExpiresAtUtc <= Now) return new(410, Error: "This conversation expired.");
        if (session.Version is < 0 or >= 9007199254740991) return ConversationConflict();
        var protector = ConversationProtector(session);
        ConversationState? state = null;
        if (session.ProtectedOnboardingState is not null)
        {
            state = ReadConversationState(session,hash);
            if (state is null) return ConversationConflict();
        }
        else if (session.PendingRequestId is not null) return ConversationConflict();

        var version = state?.Version ?? 0;
        PentaOnboardingDraft? row = null; Body? body = null; JsonElement? snapshot = null;
        var selected = action == "select" ? request!.DraftId : state?.Draft;
        if (selected is { } id)
        {
            row = await Owned(academy, actor, sessionId, id).SingleOrDefaultAsync(token);
            if (row is null) return new(404);
            body = Read(row);
            if (body is null || body.Authority != hash || row.ContextVersion != session.Version) return ConversationConflict();
            if (row.Status != "Succeeded" && (row.ExpiresAtUtc <= Now ||
                row.Status == "Authorized" && body.CoreApproval is { } proof && proof.ExpiresAtUtc <= Now)) return new(410);
            var context = await CoreSnapshotAsync(row, body, Now, token);
            if (context.HttpStatus != 200) return new(context.HttpStatus, Error: context.Error);
            snapshot = context.Snapshot;
        }
        if (action == "send-readiness")
        {
            if (state is null || row is null || body is null || session.PendingRequestId is not null) return ConversationConflict();
            var ready = BrowserContractReady && (row.Status is "AwaitingReview" or "Confirmed") && version < 99 && row.Revision < 99;
            ConversationAudit(session,"SendReadiness",version,null);
            await db.SaveChangesAsync(token); await transaction.CommitAsync(token);
            return new(200,JsonSerializer.SerializeToElement(new { version="onboarding-send/1",academyId=academy,actorId=actor,
                conversationId=session.Id,draftId=row.Id,conversationVersion=version,draftRevision=row.Revision,
                contextVersion=row.ContextVersion+1,authorityVersion=1,payloadHash=body.Hash,canSend=ready,
                runtime=ready?"synthetic_test":"unavailable",qualification="not_qualified",canExecute=false }));
        }
        if (action == "current")
        {
            ConversationAudit(session, "Read", version, null);
            await db.SaveChangesAsync(token); await transaction.CommitAsync(token);
            return new(200, ConversationView(session, state, row, snapshot));
        }

        var digest = ConversationDigest(request!);
        var replay = await db.PentaMiniTurns.SingleOrDefaultAsync(x => x.AcademyId == academy && x.ActorUserId == actor &&
            x.SessionId == sessionId && x.RequestId == request!.RequestId, token);
        if (replay is not null)
        {
            // Different purpose from read-only turns, so IDs cannot cross modes.
            try { if (protector.Unprotect(replay.InputDigest) != digest) return ConversationConflict(); }
            catch (CryptographicException) { return ConversationConflict(); }
            if (action != "select" || replay.Status != "OnboardingSelected" || replay.ProtectedReceipt is null ||
                session.PendingRequestId is not null || state is null || state.Version != request!.ExpectedVersion + 1 ||
                replay.ExpectedVersion != request.ExpectedVersion || state.Draft != request.DraftId || row!.Revision != request.DraftRevision || body!.Hash != request.PayloadHash)
                return ConversationConflict(); // pending/unknown never redispatches
            try
            {
                if (protector.Unprotect(replay.ProtectedReceipt) != JsonSerializer.Serialize(state)) return ConversationConflict();
            }
            catch (CryptographicException) { return ConversationConflict(); }
            return new(200, ConversationView(session, state, row, snapshot));
        }
        if (request!.ExpectedVersion != version || version >= 100 || session.PendingRequestId is not null ||
            row is null || row.Revision != request.DraftRevision || body!.Hash != request.PayloadHash) return ConversationConflict();
        // A paused workflow may admit a turn, but only the existing explicit
        // resume transition can change it. Partial/prepare still reject Paused.
        if (action == "claim" && row.Status is "Cancelled" or "Succeeded") return ConversationConflict();
        var day = Now.Date;
        if (await db.PentaMiniTurns.CountAsync(x => x.AcademyId == academy && x.CreatedAtUtc >= day, token) >= 500 ||
            await db.PentaMiniTurns.CountAsync(x => x.AcademyId == academy && x.ActorUserId == actor && x.CreatedAtUtc >= day, token) >= 100)
            return new(429, Error: "The conversation request limit has been reached.");
        state = new(ConversationPolicy, academy, actor, sessionId, session.Version, hash, version + 1, row.Id,
            action == "claim" ? request.RequestId : null);
        session.ProtectedOnboardingState = protector.Protect(JsonSerializer.Serialize(state));
        session.PendingRequestId = state.PendingRequest;
        session.UpdatedAtUtc = Now;
        db.PentaMiniTurns.Add(new PentaMiniTurn { AcademyId = academy, ActorUserId = actor, SessionId = sessionId,
            RequestId = request.RequestId, ExpectedVersion = version, CreatedAtUtc = Now, Status = action == "claim" ? "OnboardingPending" : "OnboardingSelected",
            InputDigest = protector.Protect(digest), ProtectedReceipt = action == "select" ? protector.Protect(JsonSerializer.Serialize(state)) : null });
        ConversationAudit(session, action == "claim" ? "Claimed" : "Selected", state.Version, request.RequestId);
        await db.SaveChangesAsync(token); await transaction.CommitAsync(token);
        // Private gateway gets the exact snapshot checked under the claim lock.
        // HTTP serializes Value only, never this internal planning projection.
        return new(action == "claim" ? 202 : 200, ConversationView(session, state, row, snapshot),
            PlanningSnapshot:action=="claim"?snapshot:null);
    }

    private static PentaOnboardingCoreOutcome ConversationConflict() => new(409,
        Error: "The conversation changed or a request is pending or unknown. Reconcile it; do not redispatch the operation.");
    private static JsonElement ConversationView(PentaMiniSession session, ConversationState? state, PentaOnboardingDraft? row, JsonElement? snapshot) =>
        JsonSerializer.SerializeToElement(new { conversationId = session.Id, version = state?.Version ?? 0,
            currentDraftId = state?.Draft, draftRevision = row?.Revision, pendingRequestId = session.PendingRequestId,
            status = session.PendingRequestId is not null ? "pending" : state is null ? "unselected" : "selected",
            // No usable planning context while a durable request is pending.
            snapshot = session.PendingRequestId is null ? snapshot : null, canExecute = false });
    private static string ConversationDigest(PentaOnboardingConversationInput request) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(request))));
    private ConversationState? ReadConversationState(PentaMiniSession session,string authority)
    {
        try {
            if (session.ProtectedOnboardingState is null) return null;
            var state=JsonSerializer.Deserialize<ConversationState>(ConversationProtector(session).Unprotect(session.ProtectedOnboardingState));
            return state is not null && state.Policy==ConversationPolicy && state.Academy==session.AcademyId &&
                state.Actor==session.ActorUserId && state.Session==session.Id && state.Context==session.Version &&
                state.Authority==authority && state.Version is >=1 and <=100 && state.Draft!=Guid.Empty &&
                state.PendingRequest==session.PendingRequestId ? state : null;
        } catch(Exception ex) when(ex is CryptographicException or JsonException or ArgumentException) {return null;}
    }
    private AuditLog ConversationAudit(PentaMiniSession session, string outcome, long version, Guid? request)
    {
        var row=new AuditLog { AcademyId=session.AcademyId,ActorUserId=session.ActorUserId,EntityType="PentaMiniSession",EntityId=session.Id,
            Action="PentaOnboardingConversation"+outcome,OccurredAtUtc=Now,
            MetadataJson=ConversationAuditMetadata(version,request) };
        db.AuditLogs.Add(row); return row;
    }
    private static string ConversationAuditMetadata(long version,Guid? request)=>
        JsonSerializer.Serialize(new {version,requestId=request,policy=ConversationPolicy,domainEffect=false});
}
