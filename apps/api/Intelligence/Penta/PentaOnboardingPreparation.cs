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

public sealed record PentaOnboardingCoreOutcome(int HttpStatus, JsonElement? Value = null, string? Error = null,
    [property: System.Text.Json.Serialization.JsonIgnore] JsonElement? PlanningSnapshot = null);

public sealed partial class PentaOnboardingDraftService
{
    private bool CorePortsEnabled => ExecutionEnabled && configuration.GetValue<bool>("Penta:OperationalOnboardingHttpContractsEnabled");
    private static string CoreOperationRef(PentaOnboardingDraft row) => "onboarding:" + row.Id.ToString("N") + ":" + row.Revision;
    private static object CoreScope(PentaOnboardingDraft row) => new {
        tenant_id = row.AcademyId.ToString("D"), actor_id = row.ActorUserId.ToString("D"),
        connection_id = "academydesk-onboarding:" + row.Id.ToString("N"), conversation_id = row.SessionId.ToString("D"),
        authority_version = 1, context_version = row.ContextVersion + 1 };

    public async Task<PentaOnboardingCoreOutcome> CorePrepareAsync(ClaimsPrincipal principal, Guid academy,
        Guid sessionId, Guid draftId, Guid requestId, JsonElement decision, CancellationToken token)
    {
        if (!CorePortsEnabled) return new(404);
        if (!PentaOnboardingPreparationInput.TryRead(decision, requestId, out var input)) return new(400, Error: "The Core preparation request is invalid.");
        if (!SqlReady()) return new(503);
        db.ChangeTracker.Clear(); identity.ChangeTracker.Clear();
        var original = AcademyIdentityTransaction.Validate(db, identity);
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, token);
        await LockAsync(academy, token);
        await using var linked = await AcademyIdentityTransaction.EnlistAsync(db, identity, transaction, original, token);
        var authority = await AuthorityAsync(principal, academy, token);
        if (authority is null) return new(403);
        var (actor, hash) = authority.Value;
        var row = await Owned(academy, actor, sessionId, draftId).SingleOrDefaultAsync(token);
        if (row is null) return new(404);
        var body = Read(row);
        if (body is null || body.Authority != hash || row.ContextVersion >= 9007199254740991 ||
            await SessionAsync(academy, actor, sessionId, row.ContextVersion, token) is null ||
            !JsonElement.DeepEquals(input!.Scope, JsonSerializer.SerializeToElement(CoreScope(row)))) return CoreConflict();
        if (row.ExpiresAtUtc <= Now) return new(410, Error: "The preparation expired.");
        var result = await ApplyCorePreparationAsync(row,body,hash,requestId,input!,token);
        if (result.HttpStatus is 200 or 201) {
            await db.SaveChangesAsync(token); await transaction.CommitAsync(token);
        }
        return result;
    }

    private async Task<PentaOnboardingCoreOutcome> ApplyCorePreparationAsync(PentaOnboardingDraft row, Body body,
        string authorityHash, Guid requestId, PentaOnboardingPreparationInput input, CancellationToken token)
    {
        var digest = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new {
            requestId, input.BaseRevision, input.Payload }))));
        var history = body.CoreRequests ?? [];
        if (history.SingleOrDefault(x => x.Id == requestId) is { } previous) {
            if (previous.Kind != "prepare" || previous.Digest != digest || previous.Revision != row.Revision || row.Status != "Prepared" ||
                body.PreparationRequestId != requestId || body.CoreApproval is not null) return CoreConflict();
            return new(200, Prepared(row, body, requestId)); // no audit, revision or expiry extension
        }
        if (input.BaseRevision != row.Revision || row.Revision >= 100 || row.Status is "Succeeded" or "Paused" or "Cancelled") return CoreConflict();
        string canonical;
        try { canonical = PentaOnboardingFields.NormalizeForCore(input.Payload, DateOnly.FromDateTime(Now)); }
        catch (ArgumentException) { return new(400, Error: "The complete canonical onboarding draft exceeds the supported bounds."); }
        var report = await CoreValidationReportAsync(row, canonical, requestId, token);
        if (report.GetProperty("status").GetString() != "valid") return new(400, report);
        row.Revision++; row.Status = "Prepared"; row.UpdatedAtUtc = Now;
        var retained = history.Append(new CoreRequest(requestId, digest, input.BaseRevision, row.Revision)).ToArray();
        body = Seal(row, authorityHash, canonical, coreRequests: retained, preparationRequestId: requestId);
        Audit(row, "CorePrepared");
        return new(201, Prepared(row, body, requestId));
    }

    public async Task<PentaOnboardingCoreOutcome> CoreValidationAsync(ClaimsPrincipal principal, Guid academy,
        Guid sessionId, Guid draftId, Guid requestId, CancellationToken token)
    {
        if (!CorePortsEnabled) return new(404);
        if (requestId == Guid.Empty) return new(400);
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
        if (body is null || body.Authority != authority.Value.Hash || row.Status is "Succeeded" or "Paused" or "Cancelled" || row.ContextVersion >= 9007199254740991 ||
            await SessionAsync(academy, row.ActorUserId, sessionId, row.ContextVersion, token) is null) return CoreConflict();
        if (row.ExpiresAtUtc <= Now) return new(410);
        var report = await CoreValidationReportAsync(row, body.Payload, requestId, token);
        Audit(row, "CoreValidated"); await db.SaveChangesAsync(token); await transaction.CommitAsync(token);
        return new(200, report);
    }

    private async Task<JsonElement> CoreValidationReportAsync(PentaOnboardingDraft row, string payload, Guid requestId, CancellationToken token)
    {
        PentaOnboardingFields.TryRead(payload, out var request);
        using var document = JsonDocument.Parse(payload);
        var fields = document.RootElement.GetProperty("fields");
        var issues = new List<object>(); var sources = new List<string>(); var status = "valid";
        void Issue(string? field, string code, object[]? choices = null) => issues.Add(new { field, code, choices = choices ?? [] });
        if (!string.IsNullOrWhiteSpace(request!.MedicalOrAccessibilityNotes)) {
            Issue("MedicalOrAccessibilityNotes", "secure_host_step"); status = "secure_step_required";
        } else {
            foreach (var key in StudentOnboardingRules.RequiredFields(request, DateOnly.FromDateTime(Now)))
                if (!fields.TryGetProperty(key, out var value) || value.GetProperty("value").ValueKind == JsonValueKind.String && string.IsNullOrWhiteSpace(value.GetProperty("value").GetString()))
                    Issue(key, "required");
            if (issues.Count > 0) status = "invalid";
            else if (await StudentOnboardingRules.ValidateAsync(db, row.AcademyId, request, DateOnly.FromDateTime(Now), token) is not null) {
                Issue(null, "business_rule"); status = "invalid";
            } else {
                var first = request.StudentFirstName.Trim().ToUpperInvariant(); var last = request.StudentLastName.Trim().ToUpperInvariant();
                var matches = await db.Students.AsNoTracking().Where(x => x.AcademyId == row.AcademyId && x.DateOfBirth == request.DateOfBirth &&
                    x.FirstName.Trim().ToUpper() == first && x.LastName.Trim().ToUpper() == last)
                    .OrderBy(x => x.Id).Select(x => new { x.Id, x.FirstName, x.LastName }).Take(21).ToArrayAsync(token);
                if (matches.Length > 0) {
                    // Never auto-select, merge, expose foreign records or truncate a
                    // list and pretend it is complete. Large ambiguity goes to Manual.
                    var choices = matches.Length > 20 ? [] : matches.Select(x => (object)new { id = x.Id.ToString("D"), label = x.FirstName + " " + x.LastName }).ToArray();
                    if (matches.Length <= 20) sources.AddRange(matches.Select(x => x.Id.ToString("D")));
                    Issue(null, "duplicate_candidates", choices); status = "ambiguous";
                }
            }
        }
        return JsonSerializer.SerializeToElement(new { request_ref = requestId.ToString("D"), scope = CoreScope(row),
            draft_ref = row.Id.ToString("D"), draft_revision = row.Revision, status, issues, source_refs = sources });
    }
    private static JsonElement Prepared(PentaOnboardingDraft row, Body body, Guid requestId)
    {
        using var document = JsonDocument.Parse(body.Payload);
        return JsonSerializer.SerializeToElement(new { @ref = CoreOperationRef(row), request_ref = requestId.ToString("D"), scope = CoreScope(row),
            tool_key = Tool, tool_revision = 1, draft_ref = row.Id.ToString("D"), draft_revision = row.Revision, payload_hash = body.Hash,
            canonical_fields = document.RootElement.GetProperty("fields"),
            preview = row.Status == "Succeeded"
                ? "Onboarding completed. Reconcile the recorded host receipt and source references; do not repeat creation. No portal accounts were created."
                : row.Status == "Authorized"
                    ? "The host confirmed this exact canonical draft. Creation has not completed; execute only with its current unexpired approval and reconcile the authoritative result. No portal accounts will be created."
                    : "Create one student and optional guardian, without portal accounts. Review every canonical field and guardian-access setting. Guardian flags apply only when the domain permits guardian access. No student has been created; explicit creation confirmation is still required.",
            approval_ref = body.CoreApproval?.Id.ToString("D"),
            approval_expires_at = body.CoreApproval is { } approval ? new DateTimeOffset(approval.ExpiresAtUtc, TimeSpan.Zero).ToString("O") : null,
            status = row.Status switch { "Authorized" => "approved", "Succeeded" => "succeeded", _ => "awaiting_approval" } });
    }
    private static PentaOnboardingCoreOutcome CoreConflict() => new(409, Error: "The request, draft or permissions changed. Reconcile the same request; do not repeat creation.");
}
