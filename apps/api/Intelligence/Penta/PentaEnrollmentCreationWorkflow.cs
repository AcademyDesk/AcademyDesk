using System.Data;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Domain.Identity;
using AcademyDesk.Api.Infrastructure;
using AcademyDesk.Api.Security;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Intelligence.Penta;

public sealed record PentaEnrollmentReceipt(Guid ReceiptId, Guid AuditId, EnrollmentSummary Enrollment, DateTime CompletedAtUtc);
public sealed record PentaEnrollmentChangePreview(string Tool, Guid EnrollmentId, Guid StudentId, Guid SourceBatchId,
    string CurrentStatus, string Status, DateOnly? EndDate, string? LifecycleReason, Guid? TargetBatchId, DateOnly? TransferDate);
public sealed record PentaEnrollmentDraftView(Guid RequestId, long Revision, long ContextVersion, string Status,
    DateTime ExpiresAtUtc, EnrollmentCreatePreview? Admission, string PayloadHash, Guid? ApprovalId, PentaEnrollmentReceipt? Receipt,
    [property: System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    PentaEnrollmentChangePreview? Change = null);
internal sealed record PentaEnrollmentDraftOutcome(int HttpStatus, PentaEnrollmentDraftView? Draft = null, string? Error = null);
internal sealed record PentaEnrollmentBinding(long Revision, string PayloadHash, Guid ApprovalId = default);

/// <summary>Testing-only enrollment changes. The host owns authorization, confirmation and the committed receipt.</summary>
public sealed partial class PentaEnrollmentCreationWorkflow(AcademyDeskDbContext db, IdentityDbContext identity,
    EnrollmentMutationService enrollments, PentaPilotPolicy policy, UserManager<ApplicationUser> users,
    IDataProtectionProvider protection, IConfiguration configuration, IHostEnvironment environment, TimeProvider clock)
{
    public const string ApprovalIntent = "ApproveEnrollmentCreate";
    internal const string UpdateIntent = "ApproveEnrollmentUpdate";
    internal const string TransferIntent = "ApproveEnrollmentTransfer";
    private const string Purpose = "academy.enrollment-create.v1";
    private const string ExecutedAction = "PentaEnrollmentExecuted";
    private const string EntityType = "PentaEnrollmentDraft";
    private const string Prepared = "Prepared";
    private const string Authorized = "Authorized";
    private const string Succeeded = "Succeeded";
    private enum Action { Prepare, Read, Approve, Execute }

    private sealed record Body(string RequestHash, string SecurityStamp, EnrollmentCreatePreview? Admission,
        Guid? ApprovalId = null, PentaEnrollmentReceipt? Receipt = null, PentaEnrollmentChangePreview? Change = null, string? SourceHash = null);

    public bool Enabled => environment.IsEnvironment("Testing") && policy.IsAvailable &&
        configuration.GetValue<bool>("Penta:EnrollmentCreationEnabled");
    private bool LifecycleEnabled => configuration.GetValue<bool>("Penta:EnrollmentLifecycleEnabled");
    private DateTime Now => clock.GetUtcNow().UtcDateTime;

    internal Task<PentaEnrollmentDraftOutcome> PrepareAsync(ClaimsPrincipal user, Guid academy, Guid session, Guid request,
        long contextVersion, string payload, CancellationToken token) =>
        RunAsync(Action.Prepare, user, academy, session, request, null, contextVersion, payload, token);

    internal Task<PentaEnrollmentDraftOutcome> ReadAsync(ClaimsPrincipal user, Guid academy, Guid session, Guid request, CancellationToken token) =>
        RunAsync(Action.Read, user, academy, session, request, null, null, null, token);

    internal Task<PentaEnrollmentDraftOutcome> ApproveAsync(ClaimsPrincipal user, Guid academy, Guid session, Guid request,
        PentaEnrollmentBinding binding, string intent, CancellationToken token) =>
        RunAsync(Action.Approve, user, academy, session, request, binding, null, null, token, intent);

    internal Task<PentaEnrollmentDraftOutcome> ExecuteAsync(ClaimsPrincipal user, Guid academy, Guid session, Guid request,
        PentaEnrollmentBinding binding, CancellationToken token) =>
        RunAsync(Action.Execute, user, academy, session, request, binding, null, null, token);

    private async Task<PentaEnrollmentDraftOutcome> RunAsync(Action action, ClaimsPrincipal principal, Guid academy,
        Guid session, Guid request, PentaEnrollmentBinding? binding, long? contextVersion, string? payload, CancellationToken token,
        string? intent = null, bool browser = false, bool lifecycleBrowser = false)
    {
        if (!Enabled) return new(404);
        if (lifecycleBrowser && (!browser || !LifecycleBrowserEnabled)) return new(404);
        if (browser && !lifecycleBrowser && !BrowserEnabled) return new(404);
        if (session == Guid.Empty || request == Guid.Empty) return new(400);
        if (!Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var actor) ||
            !await policy.AllowsAsync(principal, academy, token)) return new(403);
        if (browser && !lifecycleBrowser && action == Action.Prepare && !PentaEnrollmentFields.TryCreate(payload, out _)) return new(400);
        if (lifecycleBrowser && action == Action.Prepare && !LifecyclePayload(payload)) return new(400);
        var stamp = principal.FindFirstValue(users.Options.ClaimsIdentity.SecurityStampClaimType);
        if (browser && string.IsNullOrEmpty(stamp)) return new(403);
        var original = AcademyIdentityTransaction.Validate(db, identity);
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, token);
        await using var linked = await AcademyIdentityTransaction.EnlistAsync(db, identity, transaction, original, token);
        if (await enrollments.CurrentHostRefusalAsync(principal, academy, stamp, token) is { } refusal) return new(refusal);
        if (lifecycleBrowser && !LifecycleBrowserEnabled) return new(404);
        if (browser && !lifecycleBrowser && !BrowserEnabled) return new(404);

        var conversation = await db.PentaMiniSessions.AsNoTracking().SingleOrDefaultAsync(x =>
            x.Id == session && x.AcademyId == academy && x.ActorUserId == actor, token);
        if (conversation is null) return new(404);
        if (browser && !lifecycleBrowser && (string.IsNullOrEmpty(stamp) || !new PentaEnrollmentBrowserSession(protection).Matches(conversation, stamp))) return new(404);
        if (lifecycleBrowser && (string.IsNullOrEmpty(stamp) || !new PentaEnrollmentLifecycleBrowserSession(protection).Matches(conversation, stamp))) return new(404);
        if (conversation.ExpiresAtUtc <= Now) return new(410, Error: "The conversation expired.");
        // Scope the retry key to the actor, conversation and operation, even though the storage ID is global.
        var id = new Guid(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new { Purpose, academy, actor, session, request })).AsSpan(0, 16));
        var row = await db.PentaOnboardingDrafts.SingleOrDefaultAsync(x =>
            x.Id == id && x.AcademyId == academy && x.ActorUserId == actor && x.SessionId == session, token);
        if (row is null)
        {
            if (action != Action.Prepare) return new(404);
            if (conversation.Version != contextVersion || conversation.PendingRequestId is not null) return Changed();
            var prepared = await PrepareNewAsync(principal, conversation, id, request, stamp!, payload!, token);
            if (prepared.HttpStatus == 201) await transaction.CommitAsync(token);
            return prepared;
        }

        var body = Open(row);
        if (body is null || body.SecurityStamp != stamp) return Changed();
        if (browser && !lifecycleBrowser && body.Change is not null) return new(404);
        if (lifecycleBrowser && body.Change is null) return new(404);
        if (body.Change is not null && !LifecycleEnabled) return new(404);
        if (action == Action.Prepare && (!PentaOperationalPayload.MatchesDigest(payload!, body.RequestHash) || contextVersion != row.ContextVersion)) return Changed();
        if (action == Action.Approve && intent != Intent(body.Change)) return Changed();
        if (binding is not null && (binding.Revision != row.Revision || binding.PayloadHash != Digest(body) ||
            action == Action.Execute && binding.ApprovalId != body.ApprovalId)) return Changed();
        if (row.Status == Succeeded)
            return await ReceiptExistsAsync(row, body.Receipt!, token) ? View(200, request, row, body) : Changed();
        if (row.ExpiresAtUtc <= Now) return new(410, Error: "The enrollment draft expired.");
        if (conversation.Version != row.ContextVersion || conversation.PendingRequestId is not null) return Changed();
        if (action is Action.Read or Action.Prepare || action == Action.Approve && row.Status == Authorized)
            return View(200, request, row, body);

        PentaEnrollmentDraftOutcome outcome;
        if (action == Action.Approve)
        {
            row.Status = Authorized;
            body = body with { ApprovalId = Guid.NewGuid() };
            outcome = await SaveAsync(200, request, row, body, "PentaEnrollmentApproved", token);
        }
        else
        {
            if (row.Status != Authorized) return new(409, Error: "Explicit approval is required before enrollment.");
            var saved = await ExecutePlanAsync(principal, academy, body, token);
            if (saved.HttpStatus != (body.Change is null ? 201 : 200) || saved.Result is null)
                return new(saved.HttpStatus, Error: saved.Error);
            row.Status = Succeeded;
            body = body with { Receipt = new(Guid.NewGuid(), Guid.NewGuid(), saved.Result, Now) };
            outcome = await SaveAsync(201, request, row, body, ExecutedAction, token);
        }
        if (row.ExpiresAtUtc <= Now || conversation.ExpiresAtUtc <= Now) return new(410, Error: "The enrollment approval expired before commit.");
        await transaction.CommitAsync(token);
        return outcome;
    }

    private async Task<PentaEnrollmentDraftOutcome> PrepareNewAsync(ClaimsPrincipal principal, PentaMiniSession session,
        Guid id, Guid request, string stamp, string payload, CancellationToken token)
    {
        var today = Now.Date;
        var recent = db.PentaOnboardingDrafts.AsNoTracking().Where(x => x.AcademyId == session.AcademyId && x.CreatedAtUtc >= today);
        if (await recent.CountAsync(token) >= 100 || await recent.CountAsync(x => x.ActorUserId == session.ActorUserId, token) >= 20)
            return new(429, Error: "The daily PENTA draft limit was reached.");
        var plan = await PreparePlanAsync(principal, session.AcademyId, stamp, payload, token);
        if (plan.Body is not { } body) return new(plan.HttpStatus, Error: plan.Error);
        var now = Now;
        if (session.ExpiresAtUtc <= now) return new(410, Error: "The conversation expired.");
        var row = new PentaOnboardingDraft { Id = id, AcademyId = session.AcademyId, ActorUserId = session.ActorUserId,
            SessionId = session.Id, ContextVersion = session.Version, Revision = 1, Status = Prepared, CreatedAtUtc = now,
            ExpiresAtUtc = session.ExpiresAtUtc < now.AddMinutes(10) ? session.ExpiresAtUtc : now.AddMinutes(10), ProtectedBody = "" };
        db.PentaOnboardingDrafts.Add(row);
        return await SaveAsync(201, request, row, body, "PentaEnrollmentPrepared", token);
    }

    private async Task<PentaEnrollmentDraftOutcome> SaveAsync(int status, Guid request, PentaOnboardingDraft row, Body body,
        string action, CancellationToken token)
    {
        row.ProtectedBody = Protector(row).Protect(JsonSerializer.Serialize(body));
        row.UpdatedAtUtc = Now;
        db.AuditLogs.Add(new AuditLog { Id = body.Receipt?.AuditId ?? Guid.NewGuid(), AcademyId = row.AcademyId,
            ActorUserId = row.ActorUserId, EntityType = EntityType, EntityId = row.Id, Action = action, OccurredAtUtc = Now,
            MetadataJson = JsonSerializer.Serialize(new { tool = body.Change?.Tool ?? PentaEnrollmentFields.CreateTool }) });
        await db.SaveChangesAsync(token);
        return View(status, request, row, body);
    }

    private IDataProtector Protector(PentaOnboardingDraft row) => protection.CreateProtector(Purpose, row.Id.ToString("D"),
        row.AcademyId.ToString("D"), row.ActorUserId.ToString("D"), row.SessionId.ToString("D"),
        FormattableString.Invariant($"{row.ContextVersion}:{row.Revision}:{row.Status}:{row.ExpiresAtUtc.Ticks}"));

    private Body? Open(PentaOnboardingDraft row)
    {
        try
        {
            var body = JsonSerializer.Deserialize<Body>(Protector(row).Unprotect(row.ProtectedBody));
            if (body is null || (body.Admission is null) == (body.Change is null) ||
                body.Change is not null && (body.SourceHash is not { Length: 64 } ||
                    body.Change.Tool is not (PentaEnrollmentFields.UpdateTool or PentaEnrollmentFields.TransferTool))) return null;
            return row.Status switch
            {
                Prepared when body is { ApprovalId: null, Receipt: null } => body,
                Authorized when body is { ApprovalId: not null, Receipt: null } => body,
                Succeeded when body is { ApprovalId: not null, Receipt: not null } => body,
                _ => null
            };
        }
        catch (Exception ex) when (ex is CryptographicException or JsonException or FormatException) { return null; }
    }

    private async Task<bool> ReceiptExistsAsync(PentaOnboardingDraft row, PentaEnrollmentReceipt receipt, CancellationToken token) =>
        await db.Enrollments.AsNoTracking().AnyAsync(x => x.AcademyId == row.AcademyId && x.Id == receipt.Enrollment.Id &&
            x.StudentId == receipt.Enrollment.StudentId && x.BatchId == receipt.Enrollment.BatchId, token) &&
        await db.AuditLogs.AsNoTracking().AnyAsync(x => x.Id == receipt.AuditId && x.AcademyId == row.AcademyId &&
            x.ActorUserId == row.ActorUserId && x.EntityType == EntityType && x.EntityId == row.Id && x.Action == ExecutedAction, token);

    // Only reviewed fields contribute to the public digest. The source fingerprint stays protected.
    private static string Digest(Body body) => body.Change is { } change
        ? Hash(new { change.Tool, Revision = 1, Change = change })
        : Hash(new { Tool = PentaEnrollmentFields.CreateTool, Revision = 1, body.Admission });

    private static string Hash<T>(T value) => Convert.ToHexStringLower(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(value)));

    private static PentaEnrollmentDraftOutcome View(int status, Guid request, PentaOnboardingDraft row, Body body) =>
        new(status, new(request, row.Revision, row.ContextVersion, row.Status, row.ExpiresAtUtc, body.Admission,
            Digest(body), body.ApprovalId, body.Receipt, body.Change));

    private static PentaEnrollmentDraftOutcome Changed() => new(409, Error: "The enrollment request could not be verified. Read the same draft before retrying.");
}
