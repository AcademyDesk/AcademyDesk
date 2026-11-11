using System.Data;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Domain.Identity;
using AcademyDesk.Api.Security;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Intelligence.Penta;

internal interface IPentaFinanceReceipt
{
    Guid AuditId { get; }
}

internal sealed record PentaFinanceDraft<TPlan, TReceipt>(Guid RequestId, long Revision, long ContextVersion, string Status,
    DateTime ExpiresAtUtc, TPlan Plan, string PayloadHash, Guid? ApprovalId, TReceipt? Receipt);
internal sealed record PentaFinanceDraftBinding(long Revision, string Hash, Guid? ApprovalId = null);
internal sealed record PentaFinanceReview<TPlan>(TPlan? Plan, int Status = 200, string? Error = null);

internal abstract class PentaFinanceOperation<TRequest, TPlan, TReceipt>(string flag, string purpose,
    string entity, string started, string prepared, string approved, string executed, string changed)
    where TRequest : class where TPlan : class where TReceipt : class, IPentaFinanceReceipt
{
    internal string Flag { get; } = flag;
    internal string Purpose { get; } = purpose;
    internal string Entity { get; } = entity;
    internal string Started { get; } = started;
    internal string Prepared { get; } = prepared;
    internal string Approved { get; } = approved;
    internal string Executed { get; } = executed;
    internal string Changed { get; } = changed;
    internal abstract TRequest? Parse(string payload);
    internal abstract TRequest Restore(TPlan plan);
    internal abstract Task<PentaFinanceReview<TPlan>> ReviewAsync(Guid academy, TRequest request, DateTime now, CancellationToken token);
    internal abstract TReceipt Apply(Guid academy, TPlan plan, DateTime now);
    internal abstract Task<bool> ReceiptMatchesAsync(Guid academy, TPlan plan, TReceipt receipt, CancellationToken token);
}

/// <summary>Shared protected draft, exact approval and receipt transaction for finance operations.</summary>
public sealed class PentaFinanceDraftWorkflow(AcademyDeskDbContext db, IdentityDbContext identity,
    PentaFinancePolicy policy, PentaPilotPolicy pilot, UserManager<ApplicationUser> users,
    IDataProtectionProvider protection, IConfiguration configuration, IHostEnvironment environment, TimeProvider clock)
{
    internal enum Action { Start, Prepare, Read, Approve, Execute }
    private sealed record DraftBody<TPlan, TReceipt>(string RequestHash, TPlan Plan, Guid? ApprovalId = null, TReceipt? Receipt = default);
    private DateTime Now => clock.GetUtcNow().UtcDateTime;

    internal bool IsEnabled(string flag) => environment.IsEnvironment("Testing") && pilot.IsAvailable && configuration.GetValue<bool>(flag);

    internal Task<PentaConversationOutcome> RunAsync<TRequest, TPlan, TReceipt>(
        PentaFinanceOperation<TRequest, TPlan, TReceipt> operation, Action action, ClaimsPrincipal user, Guid academy,
        Guid sessionId, Guid request, long? context, string? payload, PentaFinanceDraftBinding? binding, CancellationToken token)
        where TRequest : class where TPlan : class where TReceipt : class, IPentaFinanceReceipt
    {
        if (!IsEnabled(operation.Flag)) return Task.FromResult(new PentaConversationOutcome(404));
        if (request == Guid.Empty || action != Action.Start && sessionId == Guid.Empty)
            return Task.FromResult(new PentaConversationOutcome(400));
        return WithAuthorityAsync(operation.Flag, user, academy, (actor, stamp) => action == Action.Start
            ? StartLockedAsync(operation.Purpose, operation.Started, academy, actor, request, stamp, token)
            : DraftLockedAsync(operation, action, academy, actor, sessionId, request, stamp, context, payload, binding, token), token);
    }

    internal async Task<PentaConversationOutcome> WithAuthorityAsync(string flag, ClaimsPrincipal user, Guid academy,
        Func<Guid, string, Task<PentaConversationOutcome>> run, CancellationToken token)
    {
        if (!IsEnabled(flag)) return new(404);
        var stamp = user.FindFirstValue(users.Options.ClaimsIdentity.SecurityStampClaimType);
        if (string.IsNullOrEmpty(stamp) || !Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var actor) ||
            !await policy.AllowsAsync(user, academy, token)) return new(403);

        var original = AcademyIdentityTransaction.Validate(db, identity);
        identity.ChangeTracker.Clear();
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, token);
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT [Id] FROM [Academies] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={academy}", token);
        await using var linked = await AcademyIdentityTransaction.EnlistAsync(db, identity, transaction, original, token);
        var current = await identity.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == actor, token);
        if (current?.SecurityStamp != stamp || !await policy.AllowsAsync(user, academy, token)) return new(403);

        var outcome = await run(actor, stamp);
        if (outcome.HttpStatus is 200 or 201) await transaction.CommitAsync(token);
        return outcome;
    }

    private async Task<PentaConversationOutcome> StartLockedAsync(string purpose, string audit, Guid academy, Guid actor,
        Guid request, string stamp, CancellationToken token)
    {
        var id = ScopedId(purpose, "conversation", academy, actor, Guid.Empty, request);
        var session = await OwnSession(academy, actor, id, token);
        if (session is not null)
        {
            if (!MatchesSession(purpose, session, stamp)) return Changed();
            return session.ExpiresAtUtc > Now ? SessionView(200, session) : new(410);
        }

        var now = Now;
        var recent = db.PentaMiniSessions.Where(x => x.AcademyId == academy && x.CreatedAtUtc >= now.Date);
        if (await recent.CountAsync(token) >= 100 || await recent.CountAsync(x => x.ActorUserId == actor, token) >= 20) return new(429);
        session = new PentaMiniSession { Id = id, AcademyId = academy, ActorUserId = actor, CreatedAtUtc = now,
            ExpiresAtUtc = now.AddHours(24), ProtectedState = "" };
        session.ProtectedOnboardingState = SessionProtector(purpose, session).Protect(stamp);
        db.PentaMiniSessions.Add(session);
        AddAudit(academy, actor, audit.Replace("Started", "Conversation", StringComparison.Ordinal), session.Id, audit);
        await db.SaveChangesAsync(token);
        return SessionView(201, session);
    }

    private async Task<PentaConversationOutcome> DraftLockedAsync<TRequest, TPlan, TReceipt>(
        PentaFinanceOperation<TRequest, TPlan, TReceipt> operation, Action action, Guid academy, Guid actor, Guid sessionId,
        Guid request, string stamp, long? context, string? payload, PentaFinanceDraftBinding? binding, CancellationToken token)
        where TRequest : class where TPlan : class where TReceipt : class, IPentaFinanceReceipt
    {
        var session = await OwnSession(academy, actor, sessionId, token);
        if (session is null || !MatchesSession(operation.Purpose, session, stamp)) return new(404);
        if (session.ExpiresAtUtc <= Now) return new(410);
        var id = ScopedId(operation.Purpose, "draft", academy, actor, sessionId, request);
        var row = await db.PentaOnboardingDrafts.SingleOrDefaultAsync(x => x.Id == id && x.AcademyId == academy &&
            x.ActorUserId == actor && x.SessionId == sessionId, token);
        if (row is null)
        {
            if (action != Action.Prepare) return new(404);
            if (session.Version != context || session.PendingRequestId is not null) return Changed();
            return await PrepareNewAsync(operation, session, id, request, payload!, token);
        }

        var body = Open<TPlan, TReceipt>(operation.Purpose, row);
        if (body is null) return Changed();
        if (action == Action.Prepare &&
            (context != row.ContextVersion || !PentaOperationalPayload.MatchesDigest(payload!, body.RequestHash))) return Changed();
        if (binding is not null && (binding.Revision != row.Revision || binding.Hash != Hash(body.Plan) ||
            action == Action.Execute && (body.ApprovalId is null || binding.ApprovalId != body.ApprovalId))) return Changed();
        if (row.Status == "Succeeded")
            return await ReceiptExistsAsync(operation, row, body, token) ? View(200, request, row, body) : Changed();
        if (row.ExpiresAtUtc <= Now) return new(410);
        if (session.Version != row.ContextVersion || session.PendingRequestId is not null) return Changed();
        if (action is Action.Read or Action.Prepare) return View(200, request, row, body);

        var reviewed = await operation.ReviewAsync(academy, operation.Restore(body.Plan), Now, token);
        if (reviewed.Plan is not { } plan || Hash(plan) != Hash(body.Plan))
            return new(409, Error: reviewed.Status == 409 ? reviewed.Error : operation.Changed);

        if (action == Action.Approve)
        {
            if (row.Status == "Authorized") return View(200, request, row, body);
            row.Status = "Authorized";
            body = body with { ApprovalId = Guid.NewGuid() };
        }
        else
        {
            if (row.Status != "Authorized") return Changed();
            row.Status = "Succeeded";
            body = body with { Receipt = operation.Apply(academy, plan, Now) };
        }
        await SaveAsync(operation.Purpose, operation.Entity, row, body,
            action == Action.Approve ? operation.Approved : operation.Executed, token);
        if (row.ExpiresAtUtc <= Now) return new(410);
        return View(action == Action.Approve ? 200 : 201, request, row, body);
    }

    private async Task<PentaConversationOutcome> PrepareNewAsync<TRequest, TPlan, TReceipt>(
        PentaFinanceOperation<TRequest, TPlan, TReceipt> operation, PentaMiniSession session, Guid id, Guid request,
        string payload, CancellationToken token)
        where TRequest : class where TPlan : class where TReceipt : class, IPentaFinanceReceipt
    {
        if (operation.Parse(payload) is not { } input)
            return new(400, Error: "Enter the expected typed finance draft fields.");
        var now = Now;
        var recent = db.PentaOnboardingDrafts.Where(x => x.AcademyId == session.AcademyId && x.CreatedAtUtc >= now.Date);
        if (await recent.CountAsync(token) >= 100 || await recent.CountAsync(x => x.ActorUserId == session.ActorUserId, token) >= 20) return new(429);
        var reviewed = await operation.ReviewAsync(session.AcademyId, input, now, token);
        if (reviewed.Plan is not { } plan) return new(reviewed.Status, Error: reviewed.Error);
        if (session.ExpiresAtUtc <= Now) return new(410);
        var row = new PentaOnboardingDraft { Id = id, AcademyId = session.AcademyId, ActorUserId = session.ActorUserId, SessionId = session.Id,
            ContextVersion = session.Version, Revision = 1, Status = "Prepared", CreatedAtUtc = now,
            ExpiresAtUtc = session.ExpiresAtUtc < now.AddMinutes(10) ? session.ExpiresAtUtc : now.AddMinutes(10), ProtectedBody = "" };
        var body = new DraftBody<TPlan, TReceipt>(PentaOperationalPayload.Digest(payload), plan);
        db.PentaOnboardingDrafts.Add(row);
        await SaveAsync(operation.Purpose, operation.Entity, row, body, operation.Prepared, token);
        return row.ExpiresAtUtc > Now ? View(201, request, row, body) : new(410);
    }

    private Task<PentaMiniSession?> OwnSession(Guid academy, Guid actor, Guid id, CancellationToken token) =>
        db.PentaMiniSessions.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id && x.AcademyId == academy && x.ActorUserId == actor, token);

    private IDataProtector SessionProtector(string purpose, PentaMiniSession session) => protection.CreateProtector(purpose, "conversation",
        session.Id.ToString("D"), session.AcademyId.ToString("D"), session.ActorUserId.ToString("D"),
        FormattableString.Invariant($"{session.Version}:{session.ExpiresAtUtc.Ticks}"));
    private bool MatchesSession(string purpose, PentaMiniSession session, string stamp)
    {
        if (session.ProtectedOnboardingState is not { } cursor) return false;
        try { return SessionProtector(purpose, session).Unprotect(cursor) == stamp; }
        catch (Exception ex) when (ex is CryptographicException or FormatException) { return false; }
    }

    private async Task SaveAsync<TPlan, TReceipt>(string purpose, string entity, PentaOnboardingDraft row,
        DraftBody<TPlan, TReceipt> body, string action, CancellationToken token) where TReceipt : class, IPentaFinanceReceipt
    {
        row.ProtectedBody = DraftProtector(purpose, row).Protect(JsonSerializer.Serialize(body));
        row.UpdatedAtUtc = Now;
        AddAudit(row.AcademyId, row.ActorUserId, entity, row.Id, action, body.Receipt?.AuditId);
        await db.SaveChangesAsync(token);
    }

    private void AddAudit(Guid academy, Guid actor, string entity, Guid entityId, string action, Guid? auditId = null) =>
        db.AuditLogs.Add(new AuditLog { Id = auditId ?? Guid.NewGuid(), AcademyId = academy, ActorUserId = actor,
            EntityType = entity, EntityId = entityId, Action = action, OccurredAtUtc = Now });

    private IDataProtector DraftProtector(string purpose, PentaOnboardingDraft row) => protection.CreateProtector(purpose, "draft",
        row.Id.ToString("D"), row.AcademyId.ToString("D"), row.ActorUserId.ToString("D"), row.SessionId.ToString("D"),
        FormattableString.Invariant($"{row.ContextVersion}:{row.Revision}:{row.Status}:{row.ExpiresAtUtc.Ticks}"));
    private DraftBody<TPlan, TReceipt>? Open<TPlan, TReceipt>(string purpose, PentaOnboardingDraft row)
        where TPlan : class where TReceipt : class
    {
        try
        {
            var body = JsonSerializer.Deserialize<DraftBody<TPlan, TReceipt>>(DraftProtector(purpose, row).Unprotect(row.ProtectedBody));
            return row.Status switch
            {
                "Prepared" when body is { Plan: not null, ApprovalId: null, Receipt: null } => body,
                "Authorized" when body is { Plan: not null, ApprovalId: not null, Receipt: null } => body,
                "Succeeded" when body is { Plan: not null, ApprovalId: not null, Receipt: not null } => body,
                _ => null
            };
        }
        catch (Exception ex) when (ex is CryptographicException or JsonException or FormatException) { return null; }
    }

    private async Task<bool> ReceiptExistsAsync<TRequest, TPlan, TReceipt>(PentaFinanceOperation<TRequest, TPlan, TReceipt> operation,
        PentaOnboardingDraft row, DraftBody<TPlan, TReceipt> body, CancellationToken token)
        where TRequest : class where TPlan : class where TReceipt : class, IPentaFinanceReceipt =>
        await operation.ReceiptMatchesAsync(row.AcademyId, body.Plan, body.Receipt!, token) &&
        await db.AuditLogs.AsNoTracking().AnyAsync(x => x.Id == body.Receipt!.AuditId && x.AcademyId == row.AcademyId &&
            x.ActorUserId == row.ActorUserId && x.EntityType == operation.Entity && x.EntityId == row.Id && x.Action == operation.Executed, token);

    private static Guid ScopedId(string purpose, string kind, Guid academy, Guid actor, Guid session, Guid request) =>
        new(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new { Purpose = purpose, kind, academy, actor, session, request })).AsSpan(0, 16));
    private static string Hash<TPlan>(TPlan plan) => Convert.ToHexStringLower(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(plan)));
    private static PentaConversationOutcome SessionView(int status, PentaMiniSession session) =>
        new(status, new MiniSessionView(session.Id, session.Version, DateTime.SpecifyKind(session.ExpiresAtUtc, DateTimeKind.Utc)));
    private static PentaConversationOutcome View<TPlan, TReceipt>(int status, Guid request, PentaOnboardingDraft row, DraftBody<TPlan, TReceipt> body) =>
        new(status, new PentaFinanceDraft<TPlan, TReceipt>(request, row.Revision, row.ContextVersion, row.Status,
            DateTime.SpecifyKind(row.ExpiresAtUtc, DateTimeKind.Utc), body.Plan, Hash(body.Plan), body.ApprovalId, body.Receipt));
    private static PentaConversationOutcome Changed() => new(409, Error: "Read the same finance draft to verify its current outcome.");
}
