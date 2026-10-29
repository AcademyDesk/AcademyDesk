using System.Data;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using AcademyDesk.Api.Security;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Intelligence.Penta;

public sealed partial class PentaOnboardingDraftService
{
    // Private leased callback only, never an ordinary browser current() port.
    // Uses a new DI scope: no original request DbContext escapes its lifetime.
    private async Task<JsonElement> ClaimedContextAsync(ClaimsPrincipal principal,Guid academy,Guid sessionId,
        PentaOnboardingConversationInput claim,JsonElement expected,CancellationToken token)
    {
        if(!CorePortsEnabled||!configuration.GetValue<bool>("Penta:OperationalOnboardingGatewayContractsEnabled")||!SqlReady())
            throw new HttpRequestException("Private context unavailable.");
        var original=AcademyIdentityTransaction.Validate(db,identity);
        await using var transaction=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable,token);
        await LockAsync(academy,token);
        await using var linked=await AcademyIdentityTransaction.EnlistAsync(db,identity,transaction,original,token);
        var authority=await AuthorityAsync(principal,academy,token);
        if(authority is null)throw new HttpRequestException("Private context unavailable.");
        var(actor,hash)=authority.Value;
        var session=await db.PentaMiniSessions.SingleOrDefaultAsync(x=>x.Id==sessionId&&x.AcademyId==academy&&x.ActorUserId==actor,token);
        if(session is null||session.ExpiresAtUtc<=Now)throw new HttpRequestException("Private context unavailable.");
        var state=ReadConversationState(session,hash);
        var turn=await db.PentaMiniTurns.SingleOrDefaultAsync(x=>x.SessionId==sessionId&&x.AcademyId==academy&&x.ActorUserId==actor&&x.RequestId==claim.RequestId,token);
        if(state is null||turn is null||state.PendingRequest!=claim.RequestId||session.PendingRequestId!=claim.RequestId||
            state.Version!=claim.ExpectedVersion+1||turn.ExpectedVersion!=claim.ExpectedVersion||turn.Status!="OnboardingPending"||turn.ProtectedReceipt is not null)
            throw new HttpRequestException("Private context unavailable.");
        try {
            if(ConversationProtector(session).Unprotect(turn.InputDigest)!=ConversationDigest(claim))throw new HttpRequestException("Private context unavailable.");
        }catch(CryptographicException){throw new HttpRequestException("Private context unavailable.");}
        var row=await Owned(academy,actor,sessionId,state.Draft).SingleOrDefaultAsync(token);
        if(row is null||row.ContextVersion!=session.Version||row.Revision!=claim.DraftRevision||row.ExpiresAtUtc<=Now||row.Status is "Succeeded" or "Cancelled")
            throw new HttpRequestException("Private context unavailable.");
        var body=Read(row);
        if(body is null||body.Authority!=hash||body.Hash!=claim.PayloadHash||
            row.Status=="Authorized"&&body.CoreApproval is {} proof&&proof.ExpiresAtUtc<=Now)
            throw new HttpRequestException("Private context unavailable.");
        var current=await CoreSnapshotAsync(row,body,Now,token);
        if(current.HttpStatus!=200||current.Snapshot is not {} snapshot||!GatewayContextUnchanged(expected,snapshot))
            throw new HttpRequestException("Private context unavailable.");
        ConversationAudit(session,"PrivateContextRead",state.Version,claim.RequestId);
        await db.SaveChangesAsync(token);await transaction.CommitAsync(token);
        return snapshot;
    }
}
