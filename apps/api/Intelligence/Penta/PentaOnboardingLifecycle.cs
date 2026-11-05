using System.Data;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Security;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Intelligence.Penta;

public sealed partial class PentaOnboardingDraftService
{
    public async Task<PentaOnboardingCoreOutcome> CoreTransitionAsync(ClaimsPrincipal principal, Guid academy,
        Guid sessionId, Guid draftId, Guid requestId, JsonElement decision, CancellationToken token)
    {
        if (!CorePortsEnabled) return new(404);
        if (!PentaOnboardingLifecycleInput.TryRead(decision, requestId, out var input))
            return new(400, Error: "The draft lifecycle request is invalid.");
        if (!SqlReady()) return new(503);
        db.ChangeTracker.Clear(); identity.ChangeTracker.Clear();
        var original = AcademyIdentityTransaction.Validate(db, identity);
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, token);
        await LockAsync(academy, token);
        await using var linked = await AcademyIdentityTransaction.EnlistAsync(db, identity, transaction, original, token);
        var authority = await AuthorityAsync(principal, academy, token);
        if (authority is null) return new(403);
        var row = await Owned(academy, authority.Value.Actor, sessionId, draftId).SingleOrDefaultAsync(token);
        if (row is null) return new(404);
        var body = Read(row);
        if (body is null || body.Authority != authority.Value.Hash || row.ContextVersion >= 9007199254740991 ||
            await SessionAsync(academy, row.ActorUserId, sessionId, row.ContextVersion, token) is null ||
            !JsonElement.DeepEquals(input!.Scope, JsonSerializer.SerializeToElement(CoreScope(row)))) return CoreConflict();
        if (row.ExpiresAtUtc <= Now) return new(410, Error: "This draft expired. Prepare a new draft.");
        var result = await ApplyCoreTransitionAsync(row,body,authority.Value.Hash,requestId,input!,token);
        if (result.HttpStatus is 200 or 201) {
            await db.SaveChangesAsync(token); await transaction.CommitAsync(token);
        }
        return result;
    }

    private async Task<PentaOnboardingCoreOutcome> ApplyCoreTransitionAsync(PentaOnboardingDraft row, Body body,
        string authorityHash, Guid requestId, PentaOnboardingLifecycleInput input, CancellationToken token)
    {
        // Digest only server-validated semantic inputs. Scope is independently
        // bound to the protected draft; key ordering never changes request identity.
        var digest = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new {
            requestId, input.BaseRevision, input.Kind }))));
        var history = body.CoreRequests ?? [];
        var target = input.Kind switch { "request_pause" => "Paused", "request_cancel" => "Cancelled", _ => "AwaitingReview" };
        if (history.SingleOrDefault(x => x.Id == requestId) is { } previous) {
            if (previous.Kind != input.Kind || previous.Digest != digest || previous.Revision != row.Revision || row.Status != target)
                return CoreConflict();
            var replay = await CoreSnapshotAsync(row, body, row.UpdatedAtUtc ?? row.CreatedAtUtc, token);
            return new(replay.HttpStatus, replay.Snapshot, replay.Error); // no write, audit or TTL extension
        }
        if (input.BaseRevision != row.Revision || row.Revision >= 100 || row.Status is "Succeeded" or "Cancelled" ||
            (input.Kind == "request_resume" ? row.Status != "Paused" : input.Kind == "request_pause" && row.Status == "Paused"))
            return CoreConflict();
        row.Revision++; row.Status = target; row.UpdatedAtUtc = Now;
        // Cancellation is draft-only, never a student delete. Every transition
        // consumes a revision and removes preparation and creation authorization.
        body = Seal(row, authorityHash, body.Payload, coreRequests:
            history.Append(new CoreRequest(requestId, digest, input.BaseRevision, row.Revision, input.Kind)).ToArray());
        var outcome = await CoreSnapshotAsync(row, body, row.UpdatedAtUtc.Value, token);
        if (outcome.HttpStatus != 200) return new(outcome.HttpStatus, Error: outcome.Error);
        Audit(row, "Core" + (input.Kind == "request_resume" ? "Resumed" : target));
        return new(200, outcome.Snapshot);
    }
}
