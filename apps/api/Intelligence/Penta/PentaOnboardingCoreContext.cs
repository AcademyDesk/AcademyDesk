using System.Data;
using System.Security.Claims;
using System.Text.Json;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Security;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Intelligence.Penta;

public sealed record PentaOnboardingBranchChoice(string id, string label);
public sealed record PentaOnboardingCoreContextOutcome(int HttpStatus, JsonElement? Snapshot = null, string? Error = null);

public sealed partial class PentaOnboardingDraftService
{
    // Candidate.5 current() port only. No new authority ledger, model call or
    // permissive registration. A draft is an immutable authority epoch: changes
    // to its full private authority hash close it; a fresh draft has a new scope.
    public async Task<PentaOnboardingCoreContextOutcome> CoreContextAsync(ClaimsPrincipal principal, Guid academy,
        Guid sessionId, Guid draftId, CancellationToken token)
    {
        if (!ExecutionEnabled || !configuration.GetValue<bool>("Penta:OperationalOnboardingHttpContractsEnabled")) return new(404);
        if (!SqlReady()) return new(503);
        db.ChangeTracker.Clear(); identity.ChangeTracker.Clear();
        var original = AcademyIdentityTransaction.Validate(db, identity);
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, token);
        await LockAsync(academy, token);
        await using var linked = await AcademyIdentityTransaction.EnlistAsync(db, identity, transaction, original, token);
        var authority = await AuthorityAsync(principal, academy, token);
        if (authority is null) return new(403);
        var (actor, authorityHash) = authority.Value;
        var row = await Owned(academy, actor, sessionId, draftId).SingleOrDefaultAsync(token);
        if (row is null) return new(404);
        var body = Read(row);
        if (body is null || body.Authority != authorityHash ||
            await SessionAsync(academy, actor, sessionId, row.ContextVersion, token) is null || row.ContextVersion >= 9007199254740991)
            return new(409, Error: "This draft context changed. Prepare a fresh review.");
        if (row.Status != "Succeeded" && (row.ExpiresAtUtc <= Now ||
            row.Status == "Authorized" && body.CoreApproval is { } approval && approval.ExpiresAtUtc <= Now))
            return new(410, Error: "This draft context expired.");
        var outcome = await CoreSnapshotAsync(row, body, Now, token);
        if (outcome.HttpStatus != 200) return outcome;
        Audit(row, "CoreContextRead");
        await db.SaveChangesAsync(token);
        await transaction.CommitAsync(token);
        return outcome;
    }

    // Called only inside the owning current-authority SQL transaction, including
    // lifecycle transitions. Build the bounded response before committing audit.
    private async Task<PentaOnboardingCoreContextOutcome> CoreSnapshotAsync(PentaOnboardingDraft row, Body body,
        DateTime asOf, CancellationToken token)
    {
        var academy = row.AcademyId;
        PentaOnboardingFields.TryRead(body.Payload, out var request);
        // No unreviewed health/accessibility text is forwarded to inference.
        // Do not silently strip it and imply Core saw the complete request.
        if (!string.IsNullOrWhiteSpace(request!.MedicalOrAccessibilityNotes))
            return new(409, Error: "This draft needs the secure manual workspace before AI context can be shared.");
        var branchRows = await db.Branches.AsNoTracking().Where(x => x.AcademyId == academy && x.IsActive)
            .OrderBy(x => x.Name).ThenBy(x => x.Id).Select(x => new { x.Id, x.Name }).Take(101).ToArrayAsync(token);
        // SQL Server's GUID-to-string translation can use uppercase hex. Format
        // stable protocol identities in the host, consistently with saved refs.
        var branches = branchRows.Select(x => new PentaOnboardingBranchChoice(x.Id.ToString("D"), x.Name)).ToArray();
        // No silent truncation, foreign labels or display-label identity binding.
        if (branches.Length > 100 || branches.Any(x => string.IsNullOrWhiteSpace(x.label) || x.label.Length > 2000))
            return new(503, Error: "The branch context needs a narrower secure selection.");
        if (request.BranchId is { } branch && !branches.Any(x => x.id == branch.ToString("D")))
            return new(409, Error: "The selected branch changed. Review the draft.");
        if (row.Status == "Succeeded" && (body.Receipt is not { } receipt ||
            !await db.Students.AsNoTracking().AnyAsync(x => x.Id == receipt.StudentId && x.AcademyId == academy, token) ||
            !await db.AuditLogs.AsNoTracking().AnyAsync(x => x.Id == receipt.AuditId && x.EntityId == row.Id &&
                x.AcademyId == academy && x.ActorUserId == row.ActorUserId && x.Action == "PentaOnboardingExecuted", token)))
            return new(409, Error: "The recorded result requires reconciliation. Do not repeat creation.");
        var status = row.Status switch { "Authorized" => "approved", "Succeeded" => "succeeded",
            "Prepared" => "awaiting_approval", "Paused" => "paused", "Cancelled" => "cancelled", _ => "collecting" };
        using var canonical = JsonDocument.Parse(body.Payload);
        var tool = new {
            key = Tool, revision = 1, family = "AI-04", capability = new[] { "Executor" },
            description = "Prepare one student and optional guardian without portal accounts. The host validates all fields, duplicates and branches; separate explicit user confirmation is required to create. Secure credentials and medical notes stay in the manual workspace.",
            effect = "write", fields = PentaOnboardingFields.Describe(body.Payload, DateOnly.FromDateTime(Now), branches),
            preparation_required = true, approval_required = true, max_items = 1
        };
        var snapshot = JsonSerializer.SerializeToElement(new {
            version = PentaOperationalPayload.ContractVersion,
            scope = CoreScope(row),
            as_of = new DateTimeOffset(asOf, TimeSpan.Zero).ToString("O"), catalog_revision = 1,
            tools = new[] { tool }, draft = new { @ref = row.Id.ToString("D"), revision = row.Revision, tool_key = Tool,
                fields = canonical.RootElement.GetProperty("fields"), status,
                operation_ref = status is "approved" or "succeeded" or "awaiting_approval" ? CoreOperationRef(row) : null },
            evidence = Array.Empty<object>(), availability = "ready"
        });
        // Contract bounds are narrower than arbitrary API response sizes.
        if (snapshot.GetRawText().Length > 16000) return new(503, Error: "The onboarding context needs a narrower secure selection.");
        return new(200, snapshot);
    }
}
