using System.Data;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Infrastructure;
using AcademyDesk.Api.Security;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Intelligence.Penta;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record PentaTaskTurnInput([property: JsonRequired] Guid RequestId,
    [property: JsonRequired] long ExpectedVersion, [property: JsonRequired] string Text);
public sealed record PentaTaskTurnReceipt(Guid ConversationId, Guid RequestId, long Version,
    string Kind, string Message, JsonElement Tasks, string InputText,
    string Protocol = "academy-task-dialogue.v1", bool ExecutionEnabled = false);
public sealed record PentaTaskHistory(Guid ConversationId, long Version, DateTime ExpiresAtUtc,
    IReadOnlyList<PentaTaskTurnReceipt> Turns, Guid? PendingRequestId, bool HasMore);
internal sealed record PentaTaskStoredState(string Protocol, long SessionVersion, long AuthorityVersion, PentaTaskPlan? Plan);

/// <summary>
/// Actual protected SQL draft conversations, default-off. Durable claim -> one
/// planner call outside SQL -> current authorization -> encrypted state/receipt
/// and mandatory audit in one transaction. NO domain dispatch/approval endpoint.
/// </summary>
public sealed class PentaTaskConversations(AcademyDeskDbContext db, IdentityDbContext identity,
    PentaTaskRuntimeAccess access, IPentaTaskPlanner planner, IDataProtectionProvider protection, TimeProvider clock)
{
    private const string Marker = "academy-tasks:v1";
    private DateTime Now => clock.GetUtcNow().UtcDateTime;
    private DateOnly Today => DateOnly.FromDateTime(Now);
    private IDataProtector Protector(Guid academy, Guid actor, Guid session) => protection.CreateProtector(
        "PentaAcademyTasks.v1", academy.ToString("D"), actor.ToString("D"), session.ToString("D"));

    public async Task<PentaConversationOutcome> CatalogAsync(ClaimsPrincipal principal, Guid academy, CancellationToken token)
    {
        if (!access.Enabled) return new(404);
        if (!db.Database.IsSqlServer()) return new(503);
        var connection = AcademyIdentityTransaction.Validate(db, identity);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, token);
        await using var linked = await AcademyIdentityTransaction.EnlistAsync(db, identity, tx, connection, token);
        await Lock(academy, token);
        var authority = await access.ResolveAsync(principal, academy, token);
        if (authority is null) return new(403);
        Audit(academy, authority.ActorId, Guid.Empty, Guid.Empty, "Catalog");
        await db.SaveChangesAsync(token); await tx.CommitAsync(token);
        return new(200, new { protocol = PentaTaskDialogue.Contract, planningOnly = true,
            executionEnabled = false, tools = PentaTaskRuntimeAccess.Describe(authority.Tools, Today) });
    }

    public async Task<PentaConversationOutcome> CreateAsync(ClaimsPrincipal principal, Guid academy, CancellationToken token)
    {
        if (!access.Enabled) return new(404);
        if (!db.Database.IsSqlServer()) return new(503);
        var connection = AcademyIdentityTransaction.Validate(db, identity);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, token);
        await using var linked = await AcademyIdentityTransaction.EnlistAsync(db, identity, tx, connection, token);
        await Lock(academy, token);
        var authority = await access.ResolveAsync(principal, academy, token);
        if (authority is null) return new(403);
        if (await db.PentaMiniSessions.CountAsync(x => x.AcademyId == academy && x.CreatedAtUtc >= Now.Date, token) >= 100 ||
            await db.PentaMiniSessions.CountAsync(x => x.AcademyId == academy && x.ActorUserId == authority.ActorId && x.CreatedAtUtc >= Now.Date, token) >= 20)
            return new(429);
        var session = new PentaMiniSession { AcademyId = academy, ActorUserId = authority.ActorId,
            ExpiresAtUtc = Now.AddHours(24), ProtectedState = "", ProtectedOnboardingState = Marker };
        session.ProtectedState = Protect(academy, authority.ActorId, session.Id, new(PentaTaskDialogue.Contract, 0, authority.Version, null));
        db.PentaMiniSessions.Add(session); Audit(academy, authority.ActorId, session.Id, Guid.Empty, "Created");
        await db.SaveChangesAsync(token); await tx.CommitAsync(token);
        return new(201, new MiniSessionView(session.Id, 0, session.ExpiresAtUtc));
    }

    public async Task<PentaConversationOutcome> TurnAsync(ClaimsPrincipal principal, Guid academy, Guid conversation,
        PentaTaskTurnInput? input, CancellationToken token)
    {
        if (!access.Enabled) return new(404);
        if (!db.Database.IsSqlServer()) return new(503);
        if (conversation == Guid.Empty || input is null || input.RequestId == Guid.Empty || input.ExpectedVersion < 0 ||
            input.Text is not { Length: > 0 and <= 2000 } || string.IsNullOrWhiteSpace(input.Text) ||
            input.Text.Any(c => char.IsControl(c) && c is not '\n' and not '\r' and not '\t')) return new(400);
        var connection = AcademyIdentityTransaction.Validate(db, identity);
        PentaTaskAuthority authority; PentaTaskStoredState state; PentaOperationalScope scope;
        var digest = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(input))));
        await using (var claim = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, token))
        {
            await using var linked = await AcademyIdentityTransaction.EnlistAsync(db, identity, claim, connection, token);
            await Lock(academy, token);
            authority = (await access.ResolveAsync(principal, academy, token))!;
            if (authority is null) return new(403);
            var session = await Own(academy, authority.ActorId, conversation, token);
            if (session is null) return new(404);
            if (session.ExpiresAtUtc <= Now) return new(410);
            if (session.ProtectedOnboardingState != Marker) return new(409);
            scope = Scope(authority, conversation);
            state = ReadState(session);
            if (state.AuthorityVersion != authority.Version || state.Plan is { } existing && existing.Scope != scope) return new(403);
            var old = await db.PentaMiniTurns.SingleOrDefaultAsync(x => x.AcademyId == academy && x.ActorUserId == authority.ActorId &&
                x.SessionId == conversation && x.RequestId == input.RequestId, token);
            if (old is not null)
            {
                var protector = Protector(academy, authority.ActorId, conversation);
                if (protector.Unprotect(old.InputDigest) != digest || old.ProtectedReceipt is null ||
                    session.PendingRequestId is not null || session.Version != old.ExpectedVersion + 1) return new(409);
                var receipt = ReadReceipt(session, old);
                Audit(academy, authority.ActorId, conversation, input.RequestId, "Replayed");
                await db.SaveChangesAsync(token); await claim.CommitAsync(token); return new(200, receipt);
            }
            if (session.Version != input.ExpectedVersion || session.PendingRequestId is not null) return new(409);
            if (session.Version >= 50 || await db.PentaMiniTurns.CountAsync(x => x.AcademyId == academy && x.CreatedAtUtc >= Now.Date, token) >= 500 ||
                await db.PentaMiniTurns.CountAsync(x => x.AcademyId == academy && x.ActorUserId == authority.ActorId && x.CreatedAtUtc >= Now.Date, token) >= 100)
                return new(429);
            session.PendingRequestId = input.RequestId;
            db.PentaMiniTurns.Add(new PentaMiniTurn { AcademyId = academy, ActorUserId = authority.ActorId, SessionId = conversation,
                RequestId = input.RequestId, ExpectedVersion = input.ExpectedVersion,
                InputDigest = Protector(academy, authority.ActorId, conversation).Protect(digest) });
            Audit(academy, authority.ActorId, conversation, input.RequestId, "Claimed");
            await db.SaveChangesAsync(token); await claim.CommitAsync(token);
        }
        // The host sends only allowed metadata and the actor's bounded draft,
        // never tenant/role claims, security stamps or an unrestricted transcript.
        PentaTaskPlannerOutcome interpretation;
        try { interpretation = await planner.PlanAsync(PentaTaskDialogue.Request(input.RequestId, input.Text.Trim(), state.Plan, authority.Tools, Today), token); }
        catch (Exception error) when (error is HttpRequestException or IOException or JsonException or OperationCanceledException)
        { interpretation = new(null, "Unavailable"); }
        token.ThrowIfCancellationRequested(); // durable pending claim, no blind redispatch
        db.ChangeTracker.Clear(); identity.ChangeTracker.Clear();
        await using var complete = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, token);
        await using var completeLinked = await AcademyIdentityTransaction.EnlistAsync(db, identity, complete, connection, token);
        await Lock(academy, token);
        var currentAuthority = await access.ResolveAsync(principal, academy, token);
        if (currentAuthority is null || currentAuthority.Version != authority.Version || !currentAuthority.Tools.SetEquals(authority.Tools))
        {
            Audit(academy, authority.ActorId, conversation, input.RequestId, "DeniedAfterPlanning", interpretation);
            await db.SaveChangesAsync(token); await complete.CommitAsync(token); return new(403);
        }
        var owned = await Own(academy, authority.ActorId, conversation, token);
        if (owned is null || owned.ExpiresAtUtc <= Now || owned.PendingRequestId != input.RequestId || owned.Version != input.ExpectedVersion) return new(409);
        PentaTaskDecision decision;
        try { decision = interpretation.Proposal is { } proposal
            ? PentaTaskDialogue.Apply(scope, input.RequestId, proposal, state.Plan, authority.Tools, Today)
            : new("ERROR", state.Plan, "PENTA task planning is unavailable. Your draft is retained and Manual remains available."); }
        catch (ArgumentException) { decision = new("ERROR", state.Plan, "PENTA could not validate that task interpretation. Your previous draft is retained."); }
        string protectedState;
        try { protectedState = Protect(academy, authority.ActorId, conversation,
            new(PentaTaskDialogue.Contract, owned.Version + 1, authority.Version, decision.Plan)); }
        catch (ArgumentException)
        {
            decision = new("ERROR", state.Plan, "This draft exceeds the conversation storage limit. Keep this draft and start the additional tasks separately.");
            protectedState = Protect(academy, authority.ActorId, conversation, state with { SessionVersion = owned.Version + 1 });
        }
        var value = new PentaTaskTurnReceipt(conversation, input.RequestId, owned.Version + 1, decision.Kind,
            decision.Message, PentaTaskDialogue.View(decision.Plan), input.Text);
        var serialized = JsonSerializer.Serialize(value);
        if (serialized.Length > 40000) throw new InvalidOperationException("Task receipt bound exceeded.");
        var encrypted = Protector(academy, authority.ActorId, conversation).Protect(serialized);
        if (encrypted.Length > 60000) throw new InvalidOperationException("Protected task receipt bound exceeded.");
        owned.ProtectedState = protectedState; owned.Version++; owned.PendingRequestId = null;
        var turn = await db.PentaMiniTurns.SingleAsync(x => x.AcademyId == academy && x.ActorUserId == authority.ActorId &&
            x.SessionId == conversation && x.RequestId == input.RequestId, token);
        turn.Status = decision.Kind; turn.ProtectedReceipt = encrypted;
        Audit(academy, authority.ActorId, conversation, input.RequestId, decision.Kind, interpretation);
        await db.SaveChangesAsync(token); await complete.CommitAsync(token); return new(201, value);
    }

    public async Task<PentaConversationOutcome> ReadAsync(ClaimsPrincipal principal, Guid academy, Guid conversation,
        Guid request, CancellationToken token)
    {
        if (!access.Enabled) return new(404);
        if (!db.Database.IsSqlServer()) return new(503);
        if (conversation == Guid.Empty || request == Guid.Empty) return new(400);
        var connection = AcademyIdentityTransaction.Validate(db, identity);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, token);
        await using var linked = await AcademyIdentityTransaction.EnlistAsync(db, identity, tx, connection, token);
        await Lock(academy, token);
        var authority = await access.ResolveAsync(principal, academy, token);
        if (authority is null) return new(403);
        var session = await Own(academy, authority.ActorId, conversation, token);
        if (session is null) return new(404);
        if (session.ExpiresAtUtc <= Now) return new(410);
        if (session.ProtectedOnboardingState != Marker) return new(409);
        var state = ReadState(session);
        if (state.AuthorityVersion != authority.Version || state.Plan is { } existing && existing.Scope != Scope(authority, conversation)) return new(403);
        var turn = await db.PentaMiniTurns.SingleOrDefaultAsync(x => x.AcademyId == academy && x.ActorUserId == authority.ActorId &&
            x.SessionId == conversation && x.RequestId == request, token);
        if (turn?.ProtectedReceipt is null || session.PendingRequestId is not null || session.Version != turn.ExpectedVersion + 1) return new(409);
        var receipt = ReadReceipt(session, turn);
        Audit(academy, authority.ActorId, conversation, request, "Recovered");
        await db.SaveChangesAsync(token); await tx.CommitAsync(token); return new(200, receipt);
    }

    public async Task<PentaConversationOutcome> HistoryAsync(ClaimsPrincipal principal, Guid academy, Guid conversation,
        long? beforeVersion, CancellationToken token)
    {
        if (!access.Enabled) return new(404);
        if (!db.Database.IsSqlServer()) return new(503);
        if (conversation == Guid.Empty) return new(400);
        var connection = AcademyIdentityTransaction.Validate(db, identity);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, token);
        await using var linked = await AcademyIdentityTransaction.EnlistAsync(db, identity, tx, connection, token);
        await Lock(academy, token);
        var authority = await access.ResolveAsync(principal, academy, token);
        if (authority is null) return new(403);
        var session = await Own(academy, authority.ActorId, conversation, token);
        if (session is null) return new(404);
        if (session.ExpiresAtUtc <= Now) return new(410);
        if (session.ProtectedOnboardingState != Marker) return new(409);
        var state = ReadState(session);
        if (state.AuthorityVersion != authority.Version || state.Plan is { } existing && existing.Scope != Scope(authority, conversation)) return new(403);
        if (beforeVersion is <= 0 || beforeVersion > session.Version + 1) return new(400);
        var before = beforeVersion ?? session.Version + 1;
        var rows = await db.PentaMiniTurns.AsNoTracking().Where(x => x.AcademyId == academy && x.ActorUserId == authority.ActorId &&
            x.SessionId == conversation && x.ProtectedReceipt != null && x.ExpectedVersion + 1 < before)
            .OrderByDescending(x => x.ExpectedVersion).Take(20).ToListAsync(token);
        var receipts = rows.OrderBy(x => x.ExpectedVersion).Select(row => ReadReceipt(session, row)).ToArray();
        var first = rows.Count == 0 ? 0 : rows.Min(x => x.ExpectedVersion);
        var more = rows.Count > 0 && await db.PentaMiniTurns.AnyAsync(x => x.AcademyId == academy && x.ActorUserId == authority.ActorId &&
            x.SessionId == conversation && x.ProtectedReceipt != null && x.ExpectedVersion < first, token);
        Audit(academy, authority.ActorId, conversation, Guid.Empty, "History");
        await db.SaveChangesAsync(token); await tx.CommitAsync(token);
        return new(200, new PentaTaskHistory(conversation, session.Version, session.ExpiresAtUtc, receipts, session.PendingRequestId, more));
    }

    private PentaTaskStoredState ReadState(PentaMiniSession session)
    {
        try {
            var value = JsonSerializer.Deserialize<PentaTaskStoredState>(Protector(session.AcademyId, session.ActorUserId, session.Id).Unprotect(session.ProtectedState));
            if (value is null || value.Protocol != PentaTaskDialogue.Contract || value.SessionVersion != session.Version || value.AuthorityVersion < 1)
                throw new InvalidOperationException("Task state changed.");
            return value;
        } catch (JsonException) { throw new InvalidOperationException("Task state could not be verified."); }
    }
    private PentaTaskTurnReceipt ReadReceipt(PentaMiniSession session, PentaMiniTurn turn)
    {
        var value = JsonSerializer.Deserialize<PentaTaskTurnReceipt>(Protector(session.AcademyId, session.ActorUserId, session.Id).Unprotect(turn.ProtectedReceipt!));
        if (value is null || value.ConversationId != session.Id || value.RequestId != turn.RequestId || value.Version != turn.ExpectedVersion + 1 ||
            value.Protocol != PentaTaskDialogue.Contract || value.ExecutionEnabled || value.Tasks.ValueKind != JsonValueKind.Array)
            throw new InvalidOperationException("Invalid protected task receipt.");
        return value;
    }
    private string Protect(Guid academy, Guid actor, Guid conversation, PentaTaskStoredState state)
    {
        var value = Protector(academy, actor, conversation).Protect(JsonSerializer.Serialize(state));
        if (value.Length > 12000) throw new ArgumentException("Task state bound exceeded.");
        return value;
    }
    private static PentaOperationalScope Scope(PentaTaskAuthority authority, Guid conversation) => new(authority.AcademyId.ToString("D"),
        authority.ActorId.ToString("D"), "academydesk", conversation.ToString("D"), authority.Version, 1);
    private Task<int> Lock(Guid academy, CancellationToken token) => db.Database.ExecuteSqlInterpolatedAsync(
        $"SELECT [Id] FROM [Academies] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={academy}", token);
    private Task<PentaMiniSession?> Own(Guid academy, Guid actor, Guid conversation, CancellationToken token) =>
        db.PentaMiniSessions.SingleOrDefaultAsync(x => x.Id == conversation && x.AcademyId == academy && x.ActorUserId == actor, token);
    private void Audit(Guid academy, Guid actor, Guid conversation, Guid request, string status, PentaTaskPlannerOutcome? outcome = null) =>
        db.AuditLogs.Add(new AuditLog { AcademyId = academy, ActorUserId = actor, EntityType = "PentaMiniSession", EntityId = conversation,
            Action = "PentaTask" + status, OccurredAtUtc = Now, MetadataJson = JsonSerializer.Serialize(new {
                protocol = PentaTaskDialogue.Contract, conversationId = conversation, requestId = request, status,
                executionEnabled = false, error = outcome?.Error, runtime = outcome?.Runtime,
                taskSourceSha256 = outcome?.TaskSourceSha256, taskPromptSha256 = outcome?.TaskPromptSha256,
                provenance = "ResponseReportedNotAttested" }) });
}
