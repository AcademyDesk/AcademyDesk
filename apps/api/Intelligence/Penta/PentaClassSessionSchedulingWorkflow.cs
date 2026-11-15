using System.Data;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
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

public sealed record PentaClassSessionReceipt(Guid ReceiptId, Guid AuditId, Guid ClassSessionId, DateTime CompletedAtUtc);
public sealed record PentaClassSessionDraftView(Guid RequestId, long Revision, long ContextVersion, string Status, DateTime ExpiresAtUtc,
    ClassSessionSummary Plan, string PayloadHash, Guid? ApprovalId, PentaClassSessionReceipt? Receipt,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] ClassSessionSummary? Source = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] bool? FollowsBatchTeacher = null);
internal sealed record PentaClassSessionOutcome(int HttpStatus, PentaClassSessionDraftView? Draft = null, string? Error = null);
internal sealed record PentaClassSessionBinding(long Revision, string Hash, Guid? ApprovalId = null);

/// <summary>Testing-only class scheduling and rescheduling with approval bound to the current plan.</summary>
public sealed partial class PentaClassSessionSchedulingWorkflow(AcademyDeskDbContext db, IdentityDbContext identity,
    ClassSessionSchedulingService scheduler, PentaPilotPolicy policy, UserManager<ApplicationUser> users,
    IDataProtectionProvider protection, IConfiguration configuration, IHostEnvironment environment, TimeProvider clock)
{
    internal const string ApprovalIntent = "ApproveClassSessionSchedule";
    internal const string RescheduleIntent = "ApproveClassSessionReschedule";
    private const string Purpose = "academy.class-session-schedule.v1";
    private const string Entity = "PentaClassSessionDraft";
    private const string ExecutionAudit = "PentaClassSessionScheduled";
    private const string RescheduleAudit = "PentaClassSessionRescheduled";
    private enum Action { Prepare, Read, Approve, Execute }
    private sealed record Body(string RequestHash, string SecurityStamp, CreateClassSessionRequest? Request,
        ClassSessionSummary Plan, Guid? ApprovalId = null, PentaClassSessionReceipt? Receipt = null,
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] PentaClassSessionMove? Move = null,
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] ClassSessionSnapshot? Source = null,
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] bool? FollowsBatchTeacher = null);

    public bool Enabled => environment.IsEnvironment("Testing") && policy.IsAvailable && configuration.GetValue<bool>("Penta:ClassSessionSchedulingEnabled");
    private bool ReschedulingEnabled => configuration.GetValue<bool>("Penta:ClassSessionReschedulingEnabled");
    private DateTime Now => clock.GetUtcNow().UtcDateTime;

    internal Task<PentaClassSessionOutcome> PrepareAsync(ClaimsPrincipal user, Guid academy, Guid session, Guid request, long context,
        string payload, CancellationToken token) => RunAsync(Action.Prepare, user, academy, session, request, context, payload, null, token);
    internal Task<PentaClassSessionOutcome> ReadAsync(ClaimsPrincipal user, Guid academy, Guid session, Guid request, CancellationToken token) =>
        RunAsync(Action.Read, user, academy, session, request, null, null, null, token);
    internal Task<PentaClassSessionOutcome> ApproveAsync(ClaimsPrincipal user, Guid academy, Guid session, Guid request,
        PentaClassSessionBinding binding, string intent, CancellationToken token) =>
        RunAsync(Action.Approve, user, academy, session, request, null, null, binding, token, intent);
    internal Task<PentaClassSessionOutcome> ExecuteAsync(ClaimsPrincipal user, Guid academy, Guid session, Guid request,
        PentaClassSessionBinding binding, CancellationToken token) => RunAsync(Action.Execute, user, academy, session, request, null, null, binding, token);

    private async Task<PentaClassSessionOutcome> RunAsync(Action action, ClaimsPrincipal user, Guid academy, Guid session, Guid request,
        long? context, string? payload, PentaClassSessionBinding? binding, CancellationToken token, string? intent = null, bool browser = false)
    {
        if (!Enabled) return new(404);
        if (browser && !BrowserEnabled) return new(404);
        if (session == Guid.Empty || request == Guid.Empty) return new(400);
        if (!Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var actor) ||
            !await policy.AllowsAsync(user, academy, token)) return new(403);
        var stamp = user.FindFirstValue(users.Options.ClaimsIdentity.SecurityStampClaimType);
        if (browser && string.IsNullOrEmpty(stamp)) return new(403);
        if (browser && action == Action.Prepare && (context != 0 || PentaClassSessionFields.Read(payload!) is null)) return new(400);
        var original = AcademyIdentityTransaction.Validate(db, identity);
        identity.ChangeTracker.Clear();
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, token);
        await scheduler.LockAsync(academy, token);
        await using var linked = await AcademyIdentityTransaction.EnlistAsync(db, identity, transaction, original, token);
        var current = await identity.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == actor, token);
        if (string.IsNullOrEmpty(stamp) || current?.SecurityStamp != stamp || !await policy.AllowsAsync(user, academy, token)) return new(403);
        var modules = await db.Academies.AsNoTracking().Where(x => x.Id == academy && x.IsActive).Select(x => x.EnabledModulesJson).SingleAsync(token);
        if (!SubscriptionPlanCatalog.Allows(modules, SubscriptionPlanCatalog.ModuleForController(nameof(ClassSessionsController)))) return new(403);

        if (browser && !BrowserEnabled) return new(404);
        var conversation = await db.PentaMiniSessions.AsNoTracking().SingleOrDefaultAsync(x => x.Id == session && x.AcademyId == academy && x.ActorUserId == actor, token);
        if (conversation is null) return new(404);
        if (browser && (string.IsNullOrEmpty(stamp) || !new PentaClassSessionBrowserSession(protection).Matches(conversation, stamp))) return new(404);
        if (conversation.ExpiresAtUtc <= Now) return new(410);
        var id = new Guid(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new { Purpose, academy, actor, session, request })).AsSpan(0, 16));
        var row = await db.PentaOnboardingDrafts.SingleOrDefaultAsync(x => x.Id == id && x.AcademyId == academy && x.ActorUserId == actor && x.SessionId == session, token);
        if (row is null)
        {
            if (action != Action.Prepare) return new(404);
            if (conversation.Version != context || conversation.PendingRequestId is not null) return Changed();
            var prepared = await PrepareNewAsync(conversation, id, request, stamp, payload!, token);
            if (prepared.Draft is { } draft)
            {
                if (draft.ExpiresAtUtc <= Now) return new(410);
                await transaction.CommitAsync(token);
            }
            return prepared;
        }

        var body = Open(row);
        if (body is null || body.SecurityStamp != stamp) return Changed();
        if (body.Move is not null && !ReschedulingEnabled) return new(404);
        if (action == Action.Approve && intent != (body.Move is null ? ApprovalIntent : RescheduleIntent)) return Changed();
        if (action == Action.Prepare && (!PentaOperationalPayload.MatchesDigest(payload!, body.RequestHash) || context != row.ContextVersion)) return Changed();
        if (binding is not null && (binding.Revision != row.Revision || binding.Hash != Hash(body) ||
            action == Action.Execute && (body.ApprovalId is null || binding.ApprovalId != body.ApprovalId))) return Changed();
        if (row.Status == "Succeeded")
            return await ReceiptExistsAsync(row, body, token) ? View(200, request, row, body) : Changed();
        if (row.ExpiresAtUtc <= Now) return new(410);
        if (conversation.Version != row.ContextVersion || conversation.PendingRequestId is not null) return Changed();
        if (action is Action.Read or Action.Prepare || action == Action.Approve && row.Status == "Authorized") return View(200, request, row, body);

        // Recheck the source and slot under the same lock as Manual scheduling.
        var resolved = await ResolveAsync(academy, new(body.Request, body.Move), token);
        if (resolved.Plan is not { } plan || plan != body.Plan || resolved.Source != body.Source || plan.StartUtc <= Now ||
            body.FollowsBatchTeacher.HasValue && body.FollowsBatchTeacher != resolved.FollowsBatchTeacher) return Changed();
        if (action == Action.Approve)
        {
            row.Status = "Authorized";
            body = body with { ApprovalId = Guid.NewGuid() };
        }
        else
        {
            if (row.Status != "Authorized") return Changed();
            var sessionRow = await scheduler.SaveAsync(academy, plan, token, resolved.FollowsBatchTeacher);
            row.Status = "Succeeded";
            body = body with { Receipt = new(Guid.NewGuid(), Guid.NewGuid(), sessionRow.Id, Now) };
        }
        var audit = action == Action.Approve
            ? (body.Move is null ? "PentaClassSessionApproved" : "PentaClassSessionRescheduleApproved")
            : CompletedAudit(body);
        var result = await SaveAsync(request, row, body, audit,
            action == Action.Approve ? 200 : 201, token);
        if (row.ExpiresAtUtc <= Now || plan.StartUtc <= Now) return new(410);
        await transaction.CommitAsync(token);
        return result;
    }

    private async Task<PentaClassSessionOutcome> PrepareNewAsync(PentaMiniSession session, Guid id, Guid request, string stamp, string payload, CancellationToken token)
    {
        if (PentaClassSessionFields.Read(payload) is not { } input) return new(400, Error: "Enter a valid class schedule with UTC start and end times and the appropriate room or HTTPS meeting link.");
        if (input.Move is not null && !ReschedulingEnabled) return new(404);
        var today = Now.Date;
        var recent = db.PentaOnboardingDrafts.AsNoTracking().Where(x => x.AcademyId == session.AcademyId && x.CreatedAtUtc >= today);
        if (await recent.CountAsync(token) >= 100 || await recent.CountAsync(x => x.ActorUserId == session.ActorUserId, token) >= 20) return new(429);
        var resolved = await ResolveAsync(session.AcademyId, input, token);
        if (resolved.Plan is not { } plan) return new(resolved.Status, Error: resolved.Error);
        var now = Now;
        if (session.ExpiresAtUtc <= now) return new(410);
        if (plan.StartUtc <= now)
            return new(400, Error: "Choose a future class time.");
        var expiry = new[] { session.ExpiresAtUtc, plan.StartUtc, resolved.Source?.Summary.StartUtc ?? plan.StartUtc, now.AddMinutes(10) }.Min();
        var row = new PentaOnboardingDraft { Id = id, AcademyId = session.AcademyId, ActorUserId = session.ActorUserId, SessionId = session.Id,
            ContextVersion = session.Version, Revision = 1, Status = "Prepared", CreatedAtUtc = now, ExpiresAtUtc = expiry, ProtectedBody = "" };
        var body = new Body(PentaOperationalPayload.Digest(payload), stamp, input.Create, plan, Move: input.Move,
            Source: resolved.Source, FollowsBatchTeacher: resolved.FollowsBatchTeacher);
        if (Seal(row, body).Length > 14500) return new(400, Error: "The reviewed scheduling details are too long.");
        db.PentaOnboardingDrafts.Add(row);
        return await SaveAsync(request, row, body, input.Move is null ? "PentaClassSessionPrepared" : "PentaClassSessionReschedulePrepared", 201, token);
    }

    private async Task<ClassSessionPlanOutcome> ResolveAsync(Guid academy, PentaClassSessionInput input, CancellationToken token)
    {
        if (input.Move is not { } move)
            return await scheduler.PlanCreateAsync(academy, input.Create!, token);

        var resolved = await scheduler.PlanUpdateAsync(academy, move.SessionId, move.Request, token);
        if (resolved.Plan is not { } plan || resolved.Source is not { Summary: { } source }) return resolved;
        if (!string.Equals(source.Status, "Scheduled", StringComparison.OrdinalIgnoreCase) || source.StartUtc <= Now)
            return new(409, Error: "Only a future scheduled class can be moved.");
        if (!await db.Batches.AnyAsync(x => x.Id == source.BatchId && x.AcademyId == academy, token) ||
            source.TeacherId.HasValue && !await db.Teachers.AnyAsync(x => x.Id == source.TeacherId && x.AcademyId == academy, token) ||
            source.BranchId.HasValue && !await db.Branches.AnyAsync(x => x.Id == source.BranchId && x.AcademyId == academy, token))
            return new(409, Error: "The current class assignment could not be verified.");

        plan = plan with { Status = source.Status };
        return plan == source ? new(400, Error: "Choose a different time or location.") : resolved with { Plan = plan };
    }

    private async Task<PentaClassSessionOutcome> SaveAsync(Guid request, PentaOnboardingDraft row, Body body, string action, int status, CancellationToken token)
    {
        row.ProtectedBody = Seal(row, body);
        row.UpdatedAtUtc = Now;
        db.AuditLogs.Add(new AuditLog { Id = body.Receipt?.AuditId ?? Guid.NewGuid(), AcademyId = row.AcademyId, ActorUserId = row.ActorUserId,
            EntityType = Entity, EntityId = row.Id, Action = action, OccurredAtUtc = Now });
        await db.SaveChangesAsync(token);
        return View(status, request, row, body);
    }

    private IDataProtector Protector(PentaOnboardingDraft row) => protection.CreateProtector(Purpose, row.Id.ToString("D"),
        row.AcademyId.ToString("D"), row.ActorUserId.ToString("D"), row.SessionId.ToString("D"),
        FormattableString.Invariant($"{row.ContextVersion}:{row.Revision}:{row.Status}:{row.ExpiresAtUtc.Ticks}"));
    private string Seal(PentaOnboardingDraft row, Body body) => Protector(row).Protect(JsonSerializer.Serialize(body));
    private Body? Open(PentaOnboardingDraft row)
    {
        try
        {
            var body = JsonSerializer.Deserialize<Body>(Protector(row).Unprotect(row.ProtectedBody));
            if (body is null || (body.Request is null) == (body.Move is null) ||
                (body.Move is null) != (body.Source is null)) return null;
            return row.Status switch
            {
                "Prepared" when body is { ApprovalId: null, Receipt: null } => body,
                "Authorized" when body is { ApprovalId: not null, Receipt: null } => body,
                "Succeeded" when body is { ApprovalId: not null, Receipt: not null } => body,
                _ => null
            };
        }
        catch (Exception ex) when (ex is CryptographicException or JsonException or FormatException) { return null; }
    }

    private async Task<bool> ReceiptExistsAsync(PentaOnboardingDraft row, Body body, CancellationToken token) =>
        await db.ClassSessions.AsNoTracking().AnyAsync(x => x.AcademyId == row.AcademyId &&
            x.Id == body.Receipt!.ClassSessionId && x.BatchId == body.Plan.BatchId, token) &&
        await db.AuditLogs.AsNoTracking().AnyAsync(x => x.Id == body.Receipt!.AuditId && x.AcademyId == row.AcademyId &&
            x.ActorUserId == row.ActorUserId && x.EntityType == Entity && x.EntityId == row.Id && x.Action == CompletedAudit(body), token);

    private static string CompletedAudit(Body body) => body.Move is null ? ExecutionAudit : RescheduleAudit;
    private static string Hash(Body body)
    {
        var bytes = body.Move is not null
            ? JsonSerializer.SerializeToUtf8Bytes(new { Operation = PentaClassSessionFields.RescheduleTool, Source = body.Source!.Summary, body.Plan })
            : body.FollowsBatchTeacher.HasValue
                ? JsonSerializer.SerializeToUtf8Bytes(new { body.Plan, body.FollowsBatchTeacher })
                : JsonSerializer.SerializeToUtf8Bytes(body.Plan);
        return Convert.ToHexStringLower(SHA256.HashData(bytes));
    }
    private static PentaClassSessionOutcome View(int status, Guid request, PentaOnboardingDraft row, Body body) =>
        new(status, new(request, row.Revision, row.ContextVersion, row.Status, row.ExpiresAtUtc, body.Plan,
            Hash(body), body.ApprovalId, body.Receipt, body.Source?.Summary, body.FollowsBatchTeacher));
    private static PentaClassSessionOutcome Changed() => new(409, Error: "The class scheduling request could not be verified. Read the same draft before retrying.");
}
