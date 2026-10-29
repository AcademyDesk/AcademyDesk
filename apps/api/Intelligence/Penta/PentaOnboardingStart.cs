using System.Data;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Security;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Intelligence.Penta;

public sealed partial class PentaOnboardingDraftService
{
    private const string StartPolicy = "academy.onboarding-start.v1";
    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    private sealed record StartReceipt(string Policy, Guid Academy, Guid Actor, Guid Request,
        Guid Session, Guid Draft, string Authority, DateTime Expiry, Guid Audit);
    private IDataProtector StartProtector(PentaMiniSession session, Guid request) => protection.CreateProtector(
        StartPolicy, session.AcademyId.ToString("D"), session.ActorUserId.ToString("D"), session.Id.ToString("D"), request.ToString("D"));

    // Testing-only entry to the existing operational gateway. Interpreter presence
    // is connection availability, NOT model qualification or permission to send/create.
    public Task<PentaOnboardingCoreOutcome> CoreStartReadinessAsync(ClaimsPrincipal user, Guid academy, CancellationToken token) =>
        StartBoundAsync(user, academy, "readiness", Guid.Empty, token);
    public Task<PentaOnboardingCoreOutcome> CoreStartResultAsync(ClaimsPrincipal user, Guid academy, Guid request, CancellationToken token) =>
        request == Guid.Empty ? Task.FromResult(new PentaOnboardingCoreOutcome(400)) : StartBoundAsync(user, academy, "result", request, token);
    public Task<PentaOnboardingCoreOutcome> CoreStartAsync(ClaimsPrincipal user, Guid academy, JsonElement input, CancellationToken token)
    {
        if (!CorePortsEnabled || !configuration.GetValue<bool>("Penta:OperationalOnboardingGatewayContractsEnabled"))
            return Task.FromResult(new PentaOnboardingCoreOutcome(404));
        if (input.ValueKind != JsonValueKind.Object || input.EnumerateObject().Count() != 1 ||
            !input.TryGetProperty("requestId", out var value) || value.ValueKind != JsonValueKind.String ||
            !Guid.TryParseExact(value.GetString(), "D", out var request) || request == Guid.Empty || request.ToString("D") != value.GetString())
            return Task.FromResult(new PentaOnboardingCoreOutcome(400));
        return StartBoundAsync(user, academy, "start", request, token);
    }
    private async Task<PentaOnboardingCoreOutcome> StartBoundAsync(ClaimsPrincipal user, Guid academy,
        string action, Guid request, CancellationToken token)
    {
        if (!CorePortsEnabled || !configuration.GetValue<bool>("Penta:OperationalOnboardingGatewayContractsEnabled")) return new(404);
        if (!SqlReady()) return new(503);
        db.ChangeTracker.Clear(); identity.ChangeTracker.Clear();
        var original = AcademyIdentityTransaction.Validate(db, identity);
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, token);
        await LockAsync(academy, token);
        await using var linked = await AcademyIdentityTransaction.EnlistAsync(db, identity, transaction, original, token);
        var authority = await AuthorityAsync(user, academy, token);
        if (authority is null) return new(403);
        var (actor, hash) = authority.Value;
        if (action == "readiness") {
            db.AuditLogs.Add(new AuditLog { AcademyId = academy, ActorUserId = actor, EntityType = "Academy", EntityId = academy,
                Action = "PentaOnboardingStartReadiness", OccurredAtUtc = Now,
                MetadataJson = JsonSerializer.Serialize(new { policy = StartPolicy, domainEffect = false }) });
            await db.SaveChangesAsync(token); await transaction.CommitAsync(token);
            return new(200, JsonSerializer.SerializeToElement(new { version = "onboarding-start/1", academyId = academy,
                available = interpreter is not null, canStart = interpreter is not null, canExecute = false }));
        }
        // A request ID is an idempotency key, not a session ID or authority claim.
        // Purpose collisions, multiple existing matches and foreign ownership fail closed.
        var matches = await db.PentaMiniTurns.Where(x => x.AcademyId == academy && x.ActorUserId == actor && x.RequestId == request)
            .Take(2).ToArrayAsync(token);
        if (matches.Length > 0) {
            if (matches.Length != 1 || matches[0].Status != "OnboardingStarted" || matches[0].ExpectedVersion != 0) return ConversationConflict();
            var marker = matches[0];
            var session = await db.PentaMiniSessions.SingleOrDefaultAsync(x => x.Id == marker.SessionId && x.AcademyId == academy && x.ActorUserId == actor, token);
            if (session is null) return ConversationConflict();
            StartReceipt? saved;
            try {
                var protector = StartProtector(session, request);
                if (marker.ProtectedReceipt is null || protector.Unprotect(marker.InputDigest) != StartPolicy) return ConversationConflict();
                saved = JsonSerializer.Deserialize<StartReceipt>(protector.Unprotect(marker.ProtectedReceipt));
            } catch (Exception ex) when (ex is CryptographicException or JsonException or ArgumentException) { return ConversationConflict(); }
            if (saved is null || saved.Policy != StartPolicy || saved.Request != request || saved.Academy != academy || saved.Actor != actor ||
                saved.Session != session.Id || saved.Authority != hash || saved.Expiry != session.ExpiresAtUtc || saved.Draft == Guid.Empty ||
                ReadConversationState(session, hash)?.Draft != saved.Draft) return ConversationConflict();
            if (session.ExpiresAtUtc <= Now) return new(410);
            var draft = await Owned(academy, actor, session.Id, saved.Draft).SingleOrDefaultAsync(token);
            var body = draft is null ? null : Read(draft);
            if (draft is null || body is null || body.Authority != hash || draft.ContextVersion != session.Version ||
                !await db.AuditLogs.AnyAsync(x => x.Id == saved.Audit && x.AcademyId == academy && x.ActorUserId == actor &&
                    x.EntityId == session.Id && x.Action == "PentaOnboardingConversationStarted", token)) return ConversationConflict();
            ConversationAudit(session, action == "result" ? "StartRead" : "StartReplayed", 1, request);
            await db.SaveChangesAsync(token); await transaction.CommitAsync(token);
            return new(200, StartView(saved));
        }
        if (action == "result") return new(404);
        if (interpreter is null) return new(503, Error: "Private conversational interpretation is not connected. Use the manual workspace.");
        var day = Now.Date;
        var sessions = db.PentaMiniSessions.Where(x => x.AcademyId == academy && x.CreatedAtUtc >= day);
        var drafts = db.PentaOnboardingDrafts.Where(x => x.AcademyId == academy && x.CreatedAtUtc >= day);
        var turns = db.PentaMiniTurns.Where(x => x.AcademyId == academy && x.CreatedAtUtc >= day);
        if (await sessions.CountAsync(token) >= 100 || await sessions.CountAsync(x => x.ActorUserId == actor, token) >= 20 ||
            await drafts.CountAsync(token) >= 100 || await drafts.CountAsync(x => x.ActorUserId == actor, token) >= 20 ||
            await turns.CountAsync(token) >= 500 || await turns.CountAsync(x => x.ActorUserId == actor, token) >= 100) return new(429);
        var created = new PentaMiniSession { AcademyId = academy, ActorUserId = actor, Version = 0, CreatedAtUtc = Now,
            ExpiresAtUtc = Now.AddMinutes(10), ProtectedState = "" };
        created.ProtectedState = ConversationProtector(created).Protect("Operational-only session; no read-only Mini state.");
        var row = new PentaOnboardingDraft { AcademyId = academy, ActorUserId = actor, SessionId = created.Id,
            ContextVersion = 0, Revision = 1, Status = "AwaitingReview", CreatedAtUtc = Now,
            ExpiresAtUtc = created.ExpiresAtUtc, ProtectedBody = "" };
        var canonical = PentaOperationalPayload.Canonicalize(JsonSerializer.Serialize(new { tool_key = Tool, tool_revision = 1, fields = new { } }));
        Seal(row, hash, canonical);
        var state = new ConversationState(ConversationPolicy, academy, actor, created.Id, 0, hash, 1, row.Id, null);
        created.ProtectedOnboardingState = ConversationProtector(created).Protect(JsonSerializer.Serialize(state));
        db.PentaMiniSessions.Add(created); db.PentaOnboardingDrafts.Add(row);
        Audit(row, "Saved"); var audit = ConversationAudit(created, "Started", 1, request);
        var receipt = new StartReceipt(StartPolicy, academy, actor, request, created.Id, row.Id, hash, created.ExpiresAtUtc, audit.Id);
        var seal = StartProtector(created, request);
        db.PentaMiniTurns.Add(new PentaMiniTurn { AcademyId = academy, ActorUserId = actor, SessionId = created.Id,
            RequestId = request, ExpectedVersion = 0, Status = "OnboardingStarted", CreatedAtUtc = Now,
            InputDigest = seal.Protect(StartPolicy), ProtectedReceipt = seal.Protect(JsonSerializer.Serialize(receipt)) });
        await db.SaveChangesAsync(token); await transaction.CommitAsync(token);
        return new(201, StartView(receipt));
    }
    private static JsonElement StartView(StartReceipt receipt) => JsonSerializer.SerializeToElement(new {
        version = "onboarding-start/1", academyId = receipt.Academy, requestId = receipt.Request,
        conversationId = receipt.Session, draftId = receipt.Draft,
        expiresAtUtc = DateTime.SpecifyKind(receipt.Expiry, DateTimeKind.Utc), status = "draft_ready", canExecute = false, domainEffect = false });
}
