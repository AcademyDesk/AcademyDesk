using System.Data;
using System.Security.Claims;
using System.Text.Json;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Security;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Intelligence.Penta;

public sealed partial class PentaOnboardingDraftService
{
    // Code-owned actions only, separate from WorkflowDecision/model planning.
    // Future UI must collect an explicit user gesture; possessing this API in a
    // test host is not proof of a human/browser confirmation or live AI access.
    public async Task<PentaOnboardingCoreOutcome> CoreOperationAsync(ClaimsPrincipal principal, Guid academy,
        Guid sessionId, Guid draftId, string action, PentaOnboardingOperationBinding binding, CancellationToken token)
    {
        if (!CorePortsEnabled) return new(404);
        if (action is not ("challenge" or "confirm" or "operation" or "status")) return new(400);
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
            body.PreparationRequestId is null || binding.OperationRef != CoreOperationRef(row) || binding.Revision != row.Revision ||
            binding.Hash != body.Hash || row.Status is not ("Prepared" or "Authorized" or "Succeeded")) return CoreConflict();
        var preparationRequestId = body.PreparationRequestId.Value;
        if (row.Status != "Succeeded" && (row.ExpiresAtUtc <= Now ||
            row.Status == "Authorized" && body.CoreApproval!.ExpiresAtUtc <= Now)) return new(410, Error: "This creation approval expired.");
        if (row.Status == "Succeeded" && !await CoreResultExistsAsync(row, body, token)) return CoreConflict();
        if (action is "operation" or "status") {
            if (row.Status == "Prepared" && body.CoreApproval is { } old && old.ExpiresAtUtc <= Now)
                return new(410, Error: "This review challenge expired. Request a fresh review.");
            var value = action == "operation" ? Prepared(row, body, preparationRequestId) : CoreResult(row, body);
            Audit(row, action == "operation" ? "CoreOperationRead" : "CoreStatusRead");
            await db.SaveChangesAsync(token); await transaction.CommitAsync(token);
            return new(200, value);
        }
        if (row.Status == "Succeeded" || action == "challenge" && row.Status != "Prepared") return CoreConflict();
        if (action == "confirm" && (body.CoreApproval is not { } proof || proof.Id != binding.ApprovalId)) return CoreConflict();
        if (action == "confirm" && body.CoreApproval!.ExpiresAtUtc <= Now) return new(410, Error: "This review challenge expired.");
        var report = await CoreValidationReportAsync(row, body.Payload, preparationRequestId, token);
        if (report.GetProperty("status").GetString() != "valid") return new(400, report);
        if (action == "challenge" && body.CoreApproval is { } current && current.ExpiresAtUtc > Now ||
            action == "confirm" && row.Status == "Authorized")
            return new(200, Prepared(row, body, preparationRequestId)); // no second audit or expiry extension
        CoreApproval approval;
        if (action == "challenge") {
            approval = new(Guid.NewGuid(), row.ExpiresAtUtc < Now.AddMinutes(5) ? row.ExpiresAtUtc : Now.AddMinutes(5), false);
        } else {
            approval = body.CoreApproval! with { Approved = true }; row.Status = "Authorized";
        }
        row.UpdatedAtUtc = Now;
        body = Seal(row, authority.Value.Hash, body.Payload, authorizesCreation: approval.Approved,
            coreRequests: body.CoreRequests, preparationRequestId: preparationRequestId, coreApproval: approval);
        Audit(row, action == "challenge" ? "CoreApprovalIssued" : "CoreApproved");
        await db.SaveChangesAsync(token); await transaction.CommitAsync(token);
        return new(200, Prepared(row, body, preparationRequestId));
    }

    private async Task<bool> CoreResultExistsAsync(PentaOnboardingDraft row, Body body, CancellationToken token) =>
        body.Receipt is { } receipt && receipt.ReceiptId != Guid.Empty &&
        await db.Students.AsNoTracking().AnyAsync(x => x.Id == receipt.StudentId && x.AcademyId == row.AcademyId, token) &&
        await db.AuditLogs.AsNoTracking().AnyAsync(x => x.Id == receipt.AuditId && x.EntityId == row.Id &&
            x.AcademyId == row.AcademyId && x.ActorUserId == row.ActorUserId && x.Action == "PentaOnboardingExecuted", token);

    private static JsonElement CoreResult(PentaOnboardingDraft row, Body body) => JsonSerializer.SerializeToElement(new {
        operation_ref = CoreOperationRef(row), scope = CoreScope(row), payload_hash = body.Hash, draft_revision = row.Revision,
        status = row.Status == "Succeeded" ? "succeeded" : row.Status == "Authorized" ? "approved" : "awaiting_approval",
        receipt_ref = body.Receipt?.ReceiptId.ToString("D"), audit_ref = body.Receipt?.AuditId.ToString("D"),
        source_refs = body.Receipt is { } receipt ? (receipt.GuardianId is { } guardian ? new[] { receipt.StudentId.ToString("D"), guardian.ToString("D") } : [receipt.StudentId.ToString("D")]) : Array.Empty<string>(),
        error_code = "none" });
}
