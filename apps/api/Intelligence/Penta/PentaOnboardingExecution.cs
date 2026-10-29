using System.Data;
using System.Security.Claims;
using System.Text.Json;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Infrastructure;
using AcademyDesk.Api.Security;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Intelligence.Penta;

public sealed record PentaOnboardingReceipt(Guid ReceiptId, Guid AuditId, Guid StudentId, Guid? GuardianId,
    DateTime CompletedAtUtc);
public sealed record PentaOnboardingExecutionOutcome(int HttpStatus, PentaOnboardingReceipt? Receipt = null,
    string? Error = null, JsonElement? CoreReceipt = null);

public sealed partial class PentaOnboardingDraftService
{
    public const string CreationIntent = "CreateStudentAndOptionalGuardianWithoutAccounts";
    private bool ExecutionEnabled => Enabled && configuration.GetValue<bool>("Penta:OperationalOnboardingExecutionContractsEnabled");

    // Separate host confirmation operation. Ordinary review confirmations and
    // conversational/model "yes" flags can never authorize creation.
    public Task<PentaOnboardingDraftOutcome> ConfirmCreationAsync(ClaimsPrincipal principal, Guid academy,
        Guid sessionId, Guid draftId, long revision, string hash, string intent, CancellationToken token)
    {
        if (!ExecutionEnabled) return Task.FromResult(new PentaOnboardingDraftOutcome(404));
        if (intent != CreationIntent) return Task.FromResult(new PentaOnboardingDraftOutcome(400, Error: "Explicit student creation confirmation is required."));
        return ReviewAsync(principal, academy, sessionId, draftId, revision, hash, token, authorizeCreation: true);
    }

    // Test-only host candidate: independently gated HTTP, no LLM dispatch installed.
    // No model inference, network, credential input or account creation in this boundary.
    public Task<PentaOnboardingExecutionOutcome> ExecuteAsync(ClaimsPrincipal principal, Guid academy,
        Guid sessionId, Guid draftId, long revision, string hash, CancellationToken token) =>
        ExecuteBoundAsync(principal, academy, sessionId, draftId, revision, hash, token);

    public async Task<PentaOnboardingCoreOutcome> CoreExecuteAsync(ClaimsPrincipal principal, Guid academy,
        Guid sessionId, Guid draftId, PentaOnboardingOperationBinding binding, CancellationToken token)
    {
        if (!CorePortsEnabled) return new(404);
        var outcome = await ExecuteBoundAsync(principal, academy, sessionId, draftId, binding.Revision, binding.Hash, token, binding);
        return new(outcome.HttpStatus, outcome.CoreReceipt, outcome.Error);
    }

    private async Task<PentaOnboardingExecutionOutcome> ExecuteBoundAsync(ClaimsPrincipal principal, Guid academy,
        Guid sessionId, Guid draftId, long revision, string hash, CancellationToken token, PentaOnboardingOperationBinding? core = null)
    {
        if (!ExecutionEnabled) return new(404);
        if (onboarding is null || !SqlReady()) return new(503);
        var originalIdentityConnection = AcademyIdentityTransaction.Validate(db, identity);
        db.ChangeTracker.Clear(); identity.ChangeTracker.Clear();
        // Same-store serializable reads retain current role/actor/session checks
        // through the commit. Academy-first ordering matches other host boundaries.
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, token);
        await LockAsync(academy, token);
        await using var linked = await AcademyIdentityTransaction.EnlistAsync(db, identity, transaction, originalIdentityConnection, token);
        var authority = await AuthorityAsync(principal, academy, token);
        if (authority is null) return new(403);
        var (actor, authorityHash) = authority.Value;
        var row = await Owned(academy, actor, sessionId, draftId).SingleOrDefaultAsync(token);
        if (row is null) return new(404);
        var body = Read(row);
        if (body is null || body.Authority != authorityHash || row.Revision != revision || hash != body.Hash ||
            await SessionAsync(academy, actor, sessionId, row.ContextVersion, token) is null)
            return new(409, Error: "The confirmed onboarding request changed. Prepare a new review.");
        if (core is null ? CoreOwned(body) : body.PreparationRequestId is null || core.OperationRef != CoreOperationRef(row) ||
            body.CoreApproval is not { Approved: true } proof || proof.Id != core.ApprovalId)
            return new(409, Error: "The current host-issued creation approval is required.");
        if (row.Status == "Succeeded")
        {
            var receipt = body.Receipt!;
            if (receipt.ReceiptId == Guid.Empty || !await db.Students.AsNoTracking().AnyAsync(
                x => x.Id == receipt.StudentId && x.AcademyId == academy, token) ||
                !await db.AuditLogs.AsNoTracking().AnyAsync(x => x.Id == receipt.AuditId && x.AcademyId == academy &&
                    x.ActorUserId == actor && x.EntityId == draftId && x.Action == "PentaOnboardingExecuted", token))
                return new(409, Error: "The recorded outcome requires manual reconciliation. Do not repeat creation.");
            return new(200, receipt, CoreReceipt: core is null ? null : CoreResult(row, body)); // no redispatch
        }
        if (row.ExpiresAtUtc <= Now || core is not null && body.CoreApproval!.ExpiresAtUtc <= Now)
            return new(410, Error: "The creation confirmation expired.");
        if (row.Status != "Authorized" || !body.AuthorizesCreation)
            return new(409, Error: "Explicit current creation authorization is required.");
        if (!PentaOnboardingFields.TryRead(body.Payload, out var request) || !Ready(body.Payload)) return new(400);
        // Revalidate references/unique identifiers and possible duplicate identity
        // inside the same transaction even when the earlier preview was valid.
        if (await StudentOnboardingRules.ValidateAsync(db, academy, request!, DateOnly.FromDateTime(Now), token) is { } invalid)
            return new(400, Error: invalid);
        var first = request!.StudentFirstName.Trim().ToUpperInvariant();
        var last = request.StudentLastName.Trim().ToUpperInvariant();
        if (await db.Students.AsNoTracking().AnyAsync(x => x.AcademyId == academy && x.DateOfBirth == request.DateOfBirth &&
            x.FirstName.Trim().ToUpper() == first && x.LastName.Trim().ToUpper() == last, token))
            return new(409, Error: "A possible matching student exists. Review existing records before creating another.");
        var outcome = await onboarding.CreateAsync(principal, academy, request, token);
        if (outcome.HttpStatus != 200 || outcome.Result is null)
            return new(outcome.HttpStatus, Error: outcome.Error); // uncommitted linked transaction rolls back
        if (row.ExpiresAtUtc <= Now || core is not null && body.CoreApproval!.ExpiresAtUtc <= Now)
            return new(410, Error: "The creation confirmation expired before completion.");
        var audit = new AuditLog { AcademyId = academy, ActorUserId = actor, EntityType = "PentaOnboardingDraft",
            EntityId = draftId, Action = "PentaOnboardingExecuted", OccurredAtUtc = Now,
            MetadataJson = JsonSerializer.Serialize(new { row.SessionId, row.ContextVersion, row.Revision,
                tool = Tool, policy = Policy, intent = CreationIntent, studentId = outcome.Result.Id,
                guardianId = outcome.Result.ParentId, accountsCreated = false }) };
        db.AuditLogs.Add(audit);
        var saved = new PentaOnboardingReceipt(Guid.NewGuid(), audit.Id, outcome.Result.Id, outcome.Result.ParentId, Now);
        row.Status = "Succeeded"; row.UpdatedAtUtc = Now;
        body = Seal(row, authorityHash, body.Payload, authorizesCreation: true, receipt: saved,
            coreRequests: body.CoreRequests, preparationRequestId: body.PreparationRequestId, coreApproval: body.CoreApproval);
        await db.SaveChangesAsync(token);
        if (row.ExpiresAtUtc <= Now || core is not null && body.CoreApproval!.ExpiresAtUtc <= Now)
            return new(410, Error: "The creation confirmation expired before commit.");
        await transaction.CommitAsync(token);
        return new(201, saved, CoreReceipt: core is null ? null : CoreResult(row, body));
    }
}
