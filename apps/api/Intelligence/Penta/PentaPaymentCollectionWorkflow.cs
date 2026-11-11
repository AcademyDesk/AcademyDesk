using System.Data;
using System.Globalization;
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

internal sealed record PentaPaymentReceipt(Guid ReceiptId, Guid AuditId, Guid PaymentId, Guid InvoiceId,
    decimal Amount, string Currency, decimal RemainingBalance, DateTime RecordedAtUtc)
{
    public string AmountExact => Amount.ToString("0.00", CultureInfo.InvariantCulture);
    public string RemainingBalanceExact => RemainingBalance.ToString("0.00", CultureInfo.InvariantCulture);
}
internal sealed record PentaPaymentDraft(Guid RequestId, long Revision, long ContextVersion, string Status,
    DateTime ExpiresAtUtc, PaymentCollectionPlan Plan, string PayloadHash, Guid? ApprovalId, PentaPaymentReceipt? Receipt);
internal sealed record PentaPaymentBinding(long Revision, string Hash, Guid? ApprovalId = null);

/// <summary>Testing-only approval and recording of a payment already received.</summary>
public sealed class PentaPaymentCollectionWorkflow(AcademyDeskDbContext db, IdentityDbContext identity,
    PentaFinancePolicy policy, PentaPilotPolicy pilot, UserManager<ApplicationUser> users,
    IDataProtectionProvider protection, IConfiguration configuration, IHostEnvironment environment, TimeProvider clock)
{
    internal const string ApprovalIntent = "ApproveRecordReceivedPayment";
    private const string Purpose = "academy.payment-collection.v1";
    private const string Entity = "PentaPaymentCollectionDraft";
    private const string ExecutionAudit = "PentaPaymentRecorded";
    private enum Action { Start, Prepare, Read, Approve, Execute }
    private sealed record DraftBody(string RequestHash, PaymentCollectionPlan Plan, Guid? ApprovalId = null, PentaPaymentReceipt? Receipt = null);

    public bool Enabled => environment.IsEnvironment("Testing") && pilot.IsAvailable && configuration.GetValue<bool>("Penta:PaymentCollectionEnabled");
    private DateTime Now => clock.GetUtcNow().UtcDateTime;

    internal Task<PentaConversationOutcome> StartAsync(ClaimsPrincipal user, Guid academy, Guid start, CancellationToken token) =>
        RunAsync(Action.Start, user, academy, Guid.Empty, start, null, null, null, token);
    internal Task<PentaConversationOutcome> PrepareAsync(ClaimsPrincipal user, Guid academy, Guid session, Guid request,
        long context, string payload, CancellationToken token) =>
        RunAsync(Action.Prepare, user, academy, session, request, context, payload, null, token);
    internal Task<PentaConversationOutcome> ReadAsync(ClaimsPrincipal user, Guid academy, Guid session, Guid request, CancellationToken token) =>
        RunAsync(Action.Read, user, academy, session, request, null, null, null, token);
    internal Task<PentaConversationOutcome> ApproveAsync(ClaimsPrincipal user, Guid academy, Guid session, Guid request,
        PentaPaymentBinding binding, CancellationToken token) =>
        RunAsync(Action.Approve, user, academy, session, request, null, null, binding, token);
    internal Task<PentaConversationOutcome> ExecuteAsync(ClaimsPrincipal user, Guid academy, Guid session, Guid request,
        PentaPaymentBinding binding, CancellationToken token) =>
        RunAsync(Action.Execute, user, academy, session, request, null, null, binding, token);

    private async Task<PentaConversationOutcome> RunAsync(Action action, ClaimsPrincipal user, Guid academy, Guid sessionId,
        Guid request, long? context, string? payload, PentaPaymentBinding? binding, CancellationToken token)
    {
        if (!Enabled) return new(404);
        if (request == Guid.Empty || action != Action.Start && sessionId == Guid.Empty) return new(400);
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

        var outcome = action == Action.Start
            ? await StartLockedAsync(academy, actor, request, stamp, token)
            : await DraftLockedAsync(action, academy, actor, sessionId, request, stamp, context, payload, binding, token);
        if (outcome.HttpStatus is 200 or 201) await transaction.CommitAsync(token);
        return outcome;
    }

    private async Task<PentaConversationOutcome> StartLockedAsync(Guid academy, Guid actor, Guid request, string stamp, CancellationToken token)
    {
        var id = ScopedId("conversation", academy, actor, Guid.Empty, request);
        var session = await OwnSession(academy, actor, id, token);
        if (session is not null)
        {
            if (!MatchesSession(session, stamp)) return Changed();
            return session.ExpiresAtUtc > Now ? SessionView(200, session) : new(410);
        }

        var now = Now;
        var recent = db.PentaMiniSessions.Where(x => x.AcademyId == academy && x.CreatedAtUtc >= now.Date);
        if (await recent.CountAsync(token) >= 100 || await recent.CountAsync(x => x.ActorUserId == actor, token) >= 20) return new(429);
        session = new PentaMiniSession { Id = id, AcademyId = academy, ActorUserId = actor, CreatedAtUtc = now,
            ExpiresAtUtc = now.AddHours(24), ProtectedState = "" };
        session.ProtectedOnboardingState = SessionProtector(session).Protect(stamp);
        db.PentaMiniSessions.Add(session);
        AddAudit(academy, actor, "PentaPaymentCollectionConversation", session.Id, "PentaPaymentCollectionStarted");
        await db.SaveChangesAsync(token);
        return SessionView(201, session);
    }

    private async Task<PentaConversationOutcome> DraftLockedAsync(Action action, Guid academy, Guid actor, Guid sessionId,
        Guid request, string stamp, long? context, string? payload, PentaPaymentBinding? binding, CancellationToken token)
    {
        var session = await OwnSession(academy, actor, sessionId, token);
        if (session is null || !MatchesSession(session, stamp)) return new(404);
        if (session.ExpiresAtUtc <= Now) return new(410);
        var id = ScopedId("draft", academy, actor, sessionId, request);
        var row = await db.PentaOnboardingDrafts.SingleOrDefaultAsync(x => x.Id == id && x.AcademyId == academy &&
            x.ActorUserId == actor && x.SessionId == sessionId, token);
        if (row is null)
        {
            if (action != Action.Prepare) return new(404);
            if (session.Version != context || session.PendingRequestId is not null) return Changed();
            return await PrepareNewAsync(session, id, request, payload!, token);
        }

        var body = Open(row);
        if (body is null) return Changed();
        if (action == Action.Prepare &&
            (context != row.ContextVersion || !PentaOperationalPayload.MatchesDigest(payload!, body.RequestHash))) return Changed();
        if (binding is not null && (binding.Revision != row.Revision || binding.Hash != Hash(body.Plan) ||
            action == Action.Execute && (body.ApprovalId is null || binding.ApprovalId != body.ApprovalId))) return Changed();
        if (row.Status == "Succeeded")
            return await ReceiptExistsAsync(row, body, token) ? View(200, request, row, body) : Changed();
        if (row.ExpiresAtUtc <= Now) return new(410);
        if (session.Version != row.ContextVersion || session.PendingRequestId is not null) return Changed();
        if (action is Action.Read or Action.Prepare) return View(200, request, row, body);

        await using var write = await InvoiceWriteScope.OpenAsync(db, academy, body.Plan.InvoiceId, token);
        var invoice = write.Invoice;
        if (invoice is null) return InvoiceChanged();
        var paymentRequest = new RecordPaymentRequest(invoice.Id, body.Plan.Amount, body.Plan.Method, null);
        var (plan, _) = await InvoicePaymentCollection.ReviewAsync(db, invoice, paymentRequest, token);
        if (plan is null || Hash(plan) != Hash(body.Plan)) return InvoiceChanged();

        if (action == Action.Approve)
        {
            if (row.Status == "Authorized") return View(200, request, row, body);
            row.Status = "Authorized";
            body = body with { ApprovalId = Guid.NewGuid() };
        }
        else
        {
            if (row.Status != "Authorized") return Changed();
            var payment = InvoicePaymentCollection.Record(db, invoice, plan);
            row.Status = "Succeeded";
            body = body with { Receipt = new(Guid.NewGuid(), Guid.NewGuid(), payment.Id, invoice.Id, payment.Amount,
                payment.Currency, plan.BalanceAfter, payment.PaidAtUtc) };
        }
        await SaveAsync(row, body, action == Action.Approve ? "PentaPaymentApproved" : ExecutionAudit, token);
        if (row.ExpiresAtUtc <= Now) return new(410);
        return View(action == Action.Approve ? 200 : 201, request, row, body);
    }

    private async Task<PentaConversationOutcome> PrepareNewAsync(PentaMiniSession session, Guid id, Guid request,
        string payload, CancellationToken token)
    {
        if (PentaPaymentFields.Read(payload) is not { } input)
            return new(400, Error: "Enter an invoice, an exact amount and a supported payment method.");
        var now = Now;
        var recent = db.PentaOnboardingDrafts.Where(x => x.AcademyId == session.AcademyId && x.CreatedAtUtc >= now.Date);
        if (await recent.CountAsync(token) >= 100 || await recent.CountAsync(x => x.ActorUserId == session.ActorUserId, token) >= 20) return new(429);
        await using var write = await InvoiceWriteScope.OpenAsync(db, session.AcademyId, input.InvoiceId, token);
        if (write.Invoice is not { } invoice) return new(404);
        var (plan, error) = await InvoicePaymentCollection.ReviewAsync(db, invoice, input, token);
        if (plan is null) return new(400, Error: error);
        if (session.ExpiresAtUtc <= Now) return new(410);
        var row = new PentaOnboardingDraft { Id = id, AcademyId = session.AcademyId, ActorUserId = session.ActorUserId, SessionId = session.Id,
            ContextVersion = session.Version, Revision = 1, Status = "Prepared", CreatedAtUtc = now,
            ExpiresAtUtc = session.ExpiresAtUtc < now.AddMinutes(10) ? session.ExpiresAtUtc : now.AddMinutes(10), ProtectedBody = "" };
        var body = new DraftBody(PentaOperationalPayload.Digest(payload), plan);
        db.PentaOnboardingDrafts.Add(row);
        await SaveAsync(row, body, "PentaPaymentPrepared", token);
        return row.ExpiresAtUtc > Now ? View(201, request, row, body) : new(410);
    }

    private Task<PentaMiniSession?> OwnSession(Guid academy, Guid actor, Guid id, CancellationToken token) =>
        db.PentaMiniSessions.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id && x.AcademyId == academy && x.ActorUserId == actor, token);

    private IDataProtector SessionProtector(PentaMiniSession session) => protection.CreateProtector(Purpose, "conversation",
        session.Id.ToString("D"), session.AcademyId.ToString("D"), session.ActorUserId.ToString("D"),
        FormattableString.Invariant($"{session.Version}:{session.ExpiresAtUtc.Ticks}"));
    private bool MatchesSession(PentaMiniSession session, string stamp)
    {
        if (session.ProtectedOnboardingState is not { } cursor) return false;
        try { return SessionProtector(session).Unprotect(cursor) == stamp; }
        catch (Exception ex) when (ex is CryptographicException or FormatException) { return false; }
    }

    private async Task SaveAsync(PentaOnboardingDraft row, DraftBody body, string action, CancellationToken token)
    {
        row.ProtectedBody = DraftProtector(row).Protect(JsonSerializer.Serialize(body));
        row.UpdatedAtUtc = Now;
        AddAudit(row.AcademyId, row.ActorUserId, Entity, row.Id, action, body.Receipt?.AuditId);
        await db.SaveChangesAsync(token);
    }

    private void AddAudit(Guid academy, Guid actor, string entity, Guid entityId, string action, Guid? auditId = null) =>
        db.AuditLogs.Add(new AuditLog { Id = auditId ?? Guid.NewGuid(), AcademyId = academy, ActorUserId = actor,
            EntityType = entity, EntityId = entityId, Action = action, OccurredAtUtc = Now });

    private IDataProtector DraftProtector(PentaOnboardingDraft row) => protection.CreateProtector(Purpose, "draft",
        row.Id.ToString("D"), row.AcademyId.ToString("D"), row.ActorUserId.ToString("D"), row.SessionId.ToString("D"),
        FormattableString.Invariant($"{row.ContextVersion}:{row.Revision}:{row.Status}:{row.ExpiresAtUtc.Ticks}"));
    private DraftBody? Open(PentaOnboardingDraft row)
    {
        try
        {
            var body = JsonSerializer.Deserialize<DraftBody>(DraftProtector(row).Unprotect(row.ProtectedBody));
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

    private async Task<bool> ReceiptExistsAsync(PentaOnboardingDraft row, DraftBody body, CancellationToken token)
    {
        var receipt = body.Receipt!;
        return await db.Payments.AsNoTracking().AnyAsync(x => x.AcademyId == row.AcademyId && x.Id == receipt.PaymentId &&
            x.InvoiceId == receipt.InvoiceId && x.Amount == receipt.Amount && x.Currency == receipt.Currency &&
            x.Method == body.Plan.Method && x.Reference == null && x.PaidAtUtc == receipt.RecordedAtUtc, token) &&
            await db.AuditLogs.AsNoTracking().AnyAsync(x => x.Id == receipt.AuditId && x.AcademyId == row.AcademyId &&
                x.ActorUserId == row.ActorUserId && x.EntityType == Entity && x.EntityId == row.Id && x.Action == ExecutionAudit, token);
    }

    private static Guid ScopedId(string kind, Guid academy, Guid actor, Guid session, Guid request) =>
        new(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new { Purpose, kind, academy, actor, session, request })).AsSpan(0, 16));
    private static string Hash(PaymentCollectionPlan plan) =>
        Convert.ToHexStringLower(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(plan)));
    private static PentaConversationOutcome SessionView(int status, PentaMiniSession session) =>
        new(status, new MiniSessionView(session.Id, session.Version, DateTime.SpecifyKind(session.ExpiresAtUtc, DateTimeKind.Utc)));
    private static PentaConversationOutcome View(int status, Guid request, PentaOnboardingDraft row, DraftBody body) =>
        new(status, new PentaPaymentDraft(request, row.Revision, row.ContextVersion, row.Status,
            DateTime.SpecifyKind(row.ExpiresAtUtc, DateTimeKind.Utc), body.Plan, Hash(body.Plan), body.ApprovalId, body.Receipt));
    private static PentaConversationOutcome Changed() => new(409, Error: "Read the same payment draft to verify its current outcome.");
    private static PentaConversationOutcome InvoiceChanged() => new(409, Error: "The invoice changed after review. Open a fresh payment draft.");
}
