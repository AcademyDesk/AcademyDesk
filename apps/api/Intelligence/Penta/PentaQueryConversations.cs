using System.Data;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Infrastructure;
using AcademyDesk.Api.Security;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Intelligence.Penta;

internal sealed record PentaQueryStoredState(string Protocol, long Version, long AuthorityVersion, PentaQueryContext Context);

/// <summary>Existing protected conversation storage and Identity/SQL transaction
/// pattern, with an independent protocol/purpose. The model only proposes a read;
/// current host authority, typed reader and mandatory audit decide the result.</summary>
public sealed class PentaQueryConversations(AcademyDeskDbContext db, IdentityDbContext identity,
    PentaQueryAccess access, IPentaQueryPlanner planner, PentaQueryReader reader,
    IDataProtectionProvider protection, TimeProvider clock)
{
    private const string Marker = "academy-query:v1";
    private DateTime Now => clock.GetUtcNow().UtcDateTime;
    private IDataProtector Protector(Guid academy, Guid actor, Guid session) => protection.CreateProtector(
        "PentaAcademyQuery.v1", academy.ToString("D"), actor.ToString("D"), session.ToString("D"));

    public async Task<PentaConversationOutcome> HealthAsync(ClaimsPrincipal principal, Guid academy, CancellationToken token)
    {
        if (!access.Enabled) return new(404);
        if (!db.Database.IsSqlServer()) return new(503);
        var connection = AcademyIdentityTransaction.Validate(db, identity);
        object[] tools;
        await using (var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, token)) {
            await using var linked = await AcademyIdentityTransaction.EnlistAsync(db, identity, tx, connection, token);
            await Lock(academy, token);
            var authority = await access.ResolveAsync(principal, academy, token);
            if (authority is null) return new(403);
            tools = PentaQueryAccess.Describe(authority.Tools);
            Audit(academy, authority.ActorId, Guid.Empty, Guid.Empty, "Catalog");
            await db.SaveChangesAsync(token); await tx.CommitAsync(token);
        }
        return new(200, new { protocol = PentaQueryContract.Version, readOnly = true, tools,
            status = await planner.ReadyAsync(token) ? "Available" : "Unavailable" });
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
            await db.PentaMiniSessions.CountAsync(x => x.AcademyId == academy && x.ActorUserId == authority.ActorId && x.CreatedAtUtc >= Now.Date, token) >= 20) return new(429);
        var session = new PentaMiniSession { AcademyId = academy, ActorUserId = authority.ActorId,
            ExpiresAtUtc = Now.AddHours(24), ProtectedState = "", ProtectedOnboardingState = Marker };
        session.ProtectedState = Protect(session, new(PentaQueryContract.Version, 0, authority.Version, PentaQueryContext.Empty));
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
            input.Text.Any(c => char.IsControl(c) && c is not '\n' and not '\t')) return new(400);
        var connection = AcademyIdentityTransaction.Validate(db, identity);
        PentaQueryAuthority authority; PentaQueryStoredState state;
        var digest = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(input))));
        await using (var claim = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, token)) {
            await using var linked = await AcademyIdentityTransaction.EnlistAsync(db, identity, claim, connection, token);
            await Lock(academy, token);
            authority = (await access.ResolveAsync(principal, academy, token))!;
            if (authority is null) return new(403);
            var session = await Own(academy, authority.ActorId, conversation, token);
            if (session is null) return new(404);
            if (session.ExpiresAtUtc <= Now) return new(410);
            if (session.ProtectedOnboardingState != Marker) return new(409);
            state = ReadState(session);
            if (state.AuthorityVersion != authority.Version) return new(403);
            if (!await reader.CurrentAsync(academy, state.Context, token)) return new(409);
            var old = await db.PentaMiniTurns.SingleOrDefaultAsync(x => x.AcademyId == academy && x.ActorUserId == authority.ActorId &&
                x.SessionId == conversation && x.RequestId == input.RequestId, token);
            if (old is not null) {
                if (Protector(academy, authority.ActorId, conversation).Unprotect(old.InputDigest) != digest ||
                    old.ProtectedReceipt is null || session.PendingRequestId is not null || session.Version != old.ExpectedVersion + 1) return new(409);
                var receipt = ReadReceipt(session, old);
                if (!await reader.CurrentAsync(academy, receipt.Context, token)) return new(409);
                Audit(academy, authority.ActorId, conversation, input.RequestId, "Replayed");
                await db.SaveChangesAsync(token); await claim.CommitAsync(token); return new(200, receipt);
            }
            if (session.Version != input.ExpectedVersion || session.PendingRequestId is not null) return new(409);
            if (session.Version >= 50 || await db.PentaMiniTurns.CountAsync(x => x.AcademyId == academy && x.CreatedAtUtc >= Now.Date, token) >= 500 ||
                await db.PentaMiniTurns.CountAsync(x => x.AcademyId == academy && x.ActorUserId == authority.ActorId && x.CreatedAtUtc >= Now.Date, token) >= 100) return new(429);
            session.PendingRequestId = input.RequestId;
            db.PentaMiniTurns.Add(new PentaMiniTurn { AcademyId = academy, ActorUserId = authority.ActorId, SessionId = conversation,
                RequestId = input.RequestId, ExpectedVersion = input.ExpectedVersion,
                InputDigest = Protector(academy, authority.ActorId, conversation).Protect(digest) });
            Audit(academy, authority.ActorId, conversation, input.RequestId, "Claimed");
            await db.SaveChangesAsync(token); await claim.CommitAsync(token);
        }
        var request = PentaQueryContract.Request(input.Text.Trim(), state.Context, authority.Tools);
        PentaQueryPlannerOutcome outcome;
        try { outcome = await planner.PlanAsync(request, token); }
        catch (Exception ex) when (ex is HttpRequestException or IOException or OperationCanceledException or JsonException) { outcome = new(null, "Unavailable"); }
        token.ThrowIfCancellationRequested(); // preserve unknown claim; never blindly repeat inference
        db.ChangeTracker.Clear(); identity.ChangeTracker.Clear();
        await using var complete = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, token);
        await using var linkedComplete = await AcademyIdentityTransaction.EnlistAsync(db, identity, complete, connection, token);
        await Lock(academy, token);
        var current = await access.ResolveAsync(principal, academy, token);
        if (current is null || current.Version != authority.Version || !current.Tools.SetEquals(authority.Tools)) {
            Audit(academy, authority.ActorId, conversation, input.RequestId, "DeniedAfterPlanning", outcome);
            await db.SaveChangesAsync(token); await complete.CommitAsync(token); return new(403);
        }
        var owned = await Own(academy, authority.ActorId, conversation, token);
        if (owned is null || owned.ExpiresAtUtc <= Now || owned.PendingRequestId != input.RequestId || owned.Version != input.ExpectedVersion ||
            !await reader.CurrentAsync(academy, state.Context, token)) return new(409);
        var proposal = outcome.Plan;
        if (!PentaQueryIntentGuard.Compatible(input.Text, proposal)) proposal = null;
        var decision = await reader.ExecuteAsync(academy, proposal, state.Context, current.Tools, token);
        var receiptValue = new PentaQueryTurnReceipt(conversation, input.RequestId, owned.Version + 1, decision.Kind,
            decision.Message, decision.Context, decision.Result, input.Text);
        var serialized = JsonSerializer.Serialize(receiptValue);
        if (serialized.Length > 40000 || !PentaQueryContract.ValidReceipt(receiptValue, conversation, input.RequestId, owned.Version + 1))
            throw new InvalidOperationException("Invalid query receipt.");
        var encrypted = Protector(academy, authority.ActorId, conversation).Protect(serialized);
        if (encrypted.Length > 60000) throw new InvalidOperationException("Protected query receipt bound exceeded.");
        owned.ProtectedState = Protect(owned, new(PentaQueryContract.Version, owned.Version + 1, current.Version, decision.Context));
        owned.Version++; owned.PendingRequestId = null;
        var turn = await db.PentaMiniTurns.SingleAsync(x => x.AcademyId == academy && x.ActorUserId == authority.ActorId && x.SessionId == conversation && x.RequestId == input.RequestId, token);
        turn.Status = decision.Kind; turn.ProtectedReceipt = encrypted;
        Audit(academy, authority.ActorId, conversation, input.RequestId, decision.Kind, outcome);
        await db.SaveChangesAsync(token); await complete.CommitAsync(token); return new(201, receiptValue);
    }

    public async Task<PentaConversationOutcome> ReadAsync(ClaimsPrincipal principal, Guid academy, Guid conversation,
        Guid? request, long? beforeVersion, CancellationToken token)
    {
        if (!access.Enabled) return new(404);
        if (!db.Database.IsSqlServer()) return new(503);
        if (conversation == Guid.Empty || request == Guid.Empty || beforeVersion is <= 0) return new(400);
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
        if (state.AuthorityVersion != authority.Version) return new(403);
        if (request is { } id) {
            var turn = await db.PentaMiniTurns.SingleOrDefaultAsync(x => x.AcademyId == academy && x.ActorUserId == authority.ActorId && x.SessionId == conversation && x.RequestId == id, token);
            if (turn?.ProtectedReceipt is null || session.PendingRequestId is not null || session.Version != turn.ExpectedVersion + 1) return new(409);
            var receipt = ReadReceipt(session, turn);
            if (!await reader.CurrentAsync(academy, receipt.Context, token)) return new(409);
            Audit(academy, authority.ActorId, conversation, id, "Recovered");
            await db.SaveChangesAsync(token); await tx.CommitAsync(token); return new(200, receipt);
        }
        if (beforeVersion > session.Version + 1) return new(400);
        var before = beforeVersion ?? session.Version + 1;
        var turns = await db.PentaMiniTurns.AsNoTracking().Where(x => x.AcademyId == academy && x.ActorUserId == authority.ActorId &&
            x.SessionId == conversation && x.ProtectedReceipt != null && x.ExpectedVersion + 1 < before)
            .OrderByDescending(x => x.ExpectedVersion).Take(20).ToArrayAsync(token);
        var values = turns.OrderBy(x => x.ExpectedVersion).Select(x => ReadReceipt(session, x)).ToArray();
        foreach (var value in values) if (!await reader.CurrentAsync(academy, value.Context, token)) return new(409);
        var first = turns.Length == 0 ? 0 : turns.Min(x => x.ExpectedVersion);
        var more = turns.Length > 0 && await db.PentaMiniTurns.AnyAsync(x => x.AcademyId == academy && x.ActorUserId == authority.ActorId &&
            x.SessionId == conversation && x.ProtectedReceipt != null && x.ExpectedVersion < first, token);
        Audit(academy, authority.ActorId, conversation, Guid.Empty, "History");
        await db.SaveChangesAsync(token); await tx.CommitAsync(token);
        return new(200, new PentaQueryHistory(conversation, session.Version, session.ExpiresAtUtc, values, session.PendingRequestId, more));
    }
    private PentaQueryStoredState ReadState(PentaMiniSession session)
    {
        var value = JsonSerializer.Deserialize<PentaQueryStoredState>(Protector(session.AcademyId, session.ActorUserId, session.Id).Unprotect(session.ProtectedState));
        if (value is null || value.Protocol != PentaQueryContract.Version || value.Version != session.Version || value.AuthorityVersion < 1 || !PentaQueryContract.ValidContext(value.Context))
            throw new InvalidOperationException("Invalid protected query context.");
        return value;
    }
    private PentaQueryTurnReceipt ReadReceipt(PentaMiniSession session, PentaMiniTurn turn)
    {
        var value = JsonSerializer.Deserialize<PentaQueryTurnReceipt>(Protector(session.AcademyId, session.ActorUserId, session.Id).Unprotect(turn.ProtectedReceipt!));
        if (!PentaQueryContract.ValidReceipt(value, session.Id, turn.RequestId, turn.ExpectedVersion + 1)) throw new InvalidOperationException("Invalid protected query receipt.");
        return value!;
    }
    private string Protect(PentaMiniSession session, PentaQueryStoredState value)
    {
        var result = Protector(session.AcademyId, session.ActorUserId, session.Id).Protect(JsonSerializer.Serialize(value));
        if (result.Length > 12000) throw new InvalidOperationException("Query context bound exceeded.");
        return result;
    }
    private Task<int> Lock(Guid academy, CancellationToken token) => db.Database.ExecuteSqlInterpolatedAsync(
        $"SELECT [Id] FROM [Academies] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={academy}", token);
    private Task<PentaMiniSession?> Own(Guid academy, Guid actor, Guid conversation, CancellationToken token) =>
        db.PentaMiniSessions.SingleOrDefaultAsync(x => x.Id == conversation && x.AcademyId == academy && x.ActorUserId == actor, token);
    private void Audit(Guid academy, Guid actor, Guid conversation, Guid request, string status, PentaQueryPlannerOutcome? outcome = null) =>
        db.AuditLogs.Add(new AuditLog { AcademyId = academy, ActorUserId = actor, EntityType = "PentaMiniSession", EntityId = conversation,
            Action = "PentaQuery" + status, OccurredAtUtc = Now, MetadataJson = JsonSerializer.Serialize(new {
                protocol = PentaQueryContract.Version, conversationId = conversation, requestId = request, status,
                readOnly = true, error = outcome?.Error, runtime = outcome?.Runtime,
                querySourceSha256 = outcome?.QuerySourceSha256, queryPromptSha256 = outcome?.QueryPromptSha256,
                provenance = "ResponseReportedNotAttested" }) });
}
