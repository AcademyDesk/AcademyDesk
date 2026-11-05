using System.Data;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Infrastructure;
using AcademyDesk.Api.Security;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Intelligence.Penta;

public sealed partial class PentaOnboardingDraftService
{
    public async Task<PentaOnboardingCoreOutcome> CorePartialAsync(ClaimsPrincipal principal, Guid academy,
        Guid sessionId, Guid draftId, Guid requestId, JsonElement decision, CancellationToken token)
    {
        if (!CorePortsEnabled) return new(404);
        if (!PentaOnboardingPreparationInput.TryReadPartial(decision,requestId,out var input))
            return new(400,Error:"The partial onboarding request is invalid.");
        if (!SqlReady()) return new(503);
        db.ChangeTracker.Clear(); identity.ChangeTracker.Clear();
        var original = AcademyIdentityTransaction.Validate(db,identity);
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable,token);
        await LockAsync(academy,token);
        await using var linked = await AcademyIdentityTransaction.EnlistAsync(db,identity,transaction,original,token);
        var authority = await AuthorityAsync(principal,academy,token);
        if (authority is null) return new(403);
        var row = await Owned(academy,authority.Value.Actor,sessionId,draftId).SingleOrDefaultAsync(token);
        if (row is null) return new(404);
        var body = Read(row);
        if (body is null || body.Authority != authority.Value.Hash || row.ContextVersion >= 9007199254740991 ||
            await SessionAsync(academy,row.ActorUserId,sessionId,row.ContextVersion,token) is null ||
            !JsonElement.DeepEquals(input!.Scope,JsonSerializer.SerializeToElement(CoreScope(row)))) return CoreConflict();
        if (row.ExpiresAtUtc <= Now) return new(410,Error:"This draft expired. Prepare a new draft.");
        var result = await ApplyCorePartialAsync(row,body,authority.Value.Hash,requestId,input!,token);
        if (result.HttpStatus is 200 or 201) {
            await db.SaveChangesAsync(token); await transaction.CommitAsync(token);
        }
        return result;
    }

    // Caller owns current authorization, scope/session validation and the SQL
    // boundary. This helper never releases a pending claim or commits alone.
    private async Task<PentaOnboardingCoreOutcome> ApplyCorePartialAsync(PentaOnboardingDraft row, Body body,
        string authorityHash, Guid requestId, PentaOnboardingPreparationInput input, CancellationToken token)
    {
        var digest = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new {
            requestId,input.BaseRevision,input.Payload,input.Questions,kind = "partial" }))));
        var history = body.CoreRequests ?? [];
        if (history.SingleOrDefault(x => x.Id == requestId) is { } previous) {
            if (previous.Kind != "partial" || previous.Digest != digest || previous.Revision != row.Revision ||
                row.Status != "AwaitingReview" || body.PreparationRequestId is not null || body.CoreApproval is not null) return CoreConflict();
            var replay = await CoreSnapshotAsync(row,body,row.UpdatedAtUtc ?? row.CreatedAtUtc,token);
            return new(replay.HttpStatus,replay.Snapshot,replay.Error); // no write, audit or TTL extension
        }
        if (input.BaseRevision != row.Revision || row.Revision >= 100 || row.Status is "Paused" or "Cancelled" or "Succeeded") return CoreConflict();
        string canonical;
        try { canonical = PentaOnboardingFields.NormalizeForCore(input.Payload,DateOnly.FromDateTime(Now)); }
        catch (ArgumentException) { return new(400,Error:"The canonical partial draft exceeds the supported bounds."); }
        PentaOnboardingFields.TryRead(canonical,out var request);
        using var fields = JsonDocument.Parse(canonical);
        var values = fields.RootElement.GetProperty("fields");
        // Questions describe actual missing host-required fields, not arbitrary
        // model instructions. Conditional guardian requirements use the new DOB.
        var missing = StudentOnboardingRules.RequiredFields(request!,DateOnly.FromDateTime(Now))
            .Where(key => !values.TryGetProperty(key,out _)).ToHashSet(StringComparer.Ordinal);
        if (missing.Count == 0 || input.Questions!.Any(key => !missing.Contains(key)))
            return new(400,Error:"Ask only for current missing required onboarding details; complete drafts require preparation.");
        row.Revision++; row.Status = "AwaitingReview"; row.UpdatedAtUtc = Now;
        // Full canonical replacement: Core resolves refine/clear/retained fields
        // before calling this host. This port neither merges nor creates records.
        body = Seal(row,authorityHash,canonical,coreRequests:
            history.Append(new CoreRequest(requestId,digest,input.BaseRevision,row.Revision,"partial")).ToArray());
        var outcome = await CoreSnapshotAsync(row,body,row.UpdatedAtUtc.Value,token);
        if (outcome.HttpStatus != 200) return new(outcome.HttpStatus,Error:outcome.Error);
        Audit(row,"CorePartialSaved");
        return new(200,outcome.Snapshot);
    }
}
