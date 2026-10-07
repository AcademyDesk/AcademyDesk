using System.Data;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Domain.Identity;
using AcademyDesk.Api.Infrastructure;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Intelligence.Penta;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record MiniTurnInput(Guid RequestId, long ExpectedVersion, string Text, string Capability);
public sealed record MiniSessionView(Guid ConversationId, long Version, DateTime ExpiresAtUtc);
public sealed record MiniReceipt(Guid ConversationId, Guid RequestId, long Version, string Kind, string Message,
    string Capability, OutstandingResult? Result, MiniState Context, string Provider = "PENTA Mini", string Protocol = "0.1");

/// <summary>Durable claim -> private planner -> reauthorize -> atomic read/state/audit.
/// No inference runs inside a SQL transaction; no raw prompt or model prose is persisted.</summary>
public sealed class PentaMiniOrchestrator(AcademyDeskDbContext db, IdentityDbContext identity,
    PentaPilotPolicy pilot, PentaFinancePolicy policy, UserManager<ApplicationUser> users,
    IPentaProvider provider, AcademyDeskConnector connector, IDataProtectionProvider protection,
    IConfiguration config, TimeProvider clock)
{
    public bool Enabled => pilot.IsAvailable && config.GetValue<bool>("Penta:Mini:Enabled");
    private IDataProtector Protector(Guid academy, Guid actor, Guid session) => protection.CreateProtector(
        "PentaMini.local.v1", academy.ToString("D"), actor.ToString("D"), session.ToString("D"));
    private DateTime Now => clock.GetUtcNow().UtcDateTime;
    public async Task<PentaConversationOutcome> HealthAsync(ClaimsPrincipal principal, Guid academy, CancellationToken token)
    {
        if (!Enabled) return new(404);
        if (!await policy.AllowsAsync(principal, academy, token)) return new(403);
        return new(200, new { status = await provider.ReadinessAsync(token), provider = "PENTA Mini", protocol = "0.1", readOnly = true });
    }
    public async Task<PentaConversationOutcome> CreateAsync(ClaimsPrincipal principal, Guid academy, CancellationToken token)
    {
        if (!Enabled) return new(404);
        if (!db.Database.IsSqlServer()) return new(503, Error: "PENTA requires its SQL store.");
        if (!await policy.AllowsAsync(principal, academy, token)) return new(403);
        var actor = (await users.GetUserAsync(principal))!.Id;
        await using var tx = await db.Database.BeginTransactionAsync(token);
        await LockAcademyAsync(academy, token);
        if (!await Reauthorize(principal, academy, token)) return new(403);
        var day = Now.Date;
        if (await db.PentaMiniSessions.CountAsync(x => x.AcademyId == academy && x.CreatedAtUtc >= day, token) >= 100 ||
            await db.PentaMiniSessions.CountAsync(x => x.AcademyId == academy && x.ActorUserId == actor && x.CreatedAtUtc >= day, token) >= 20)
            return new(429, Error: "The local pilot conversation limit has been reached.");
        var session = new PentaMiniSession { AcademyId = academy, ActorUserId = actor, ExpiresAtUtc = Now.AddHours(24), ProtectedState = "" };
        session.ProtectedState = Protector(academy, actor, session.Id).Protect(JsonSerializer.Serialize(MiniState.Empty));
        db.PentaMiniSessions.Add(session);
        Audit(academy, actor, session.Id, Guid.Empty, "Created", null, 0, null);
        await db.SaveChangesAsync(token);
        await tx.CommitAsync(token);
        return new(201, new MiniSessionView(session.Id, 0, session.ExpiresAtUtc));
    }
    public async Task<PentaConversationOutcome> TurnAsync(ClaimsPrincipal principal, Guid academy, Guid sessionId,
        MiniTurnInput? input, CancellationToken token)
    {
        if (!Enabled) return new(404);
        if (!db.Database.IsSqlServer()) return new(503);
        if (!await policy.AllowsAsync(principal, academy, token)) return new(403);
        if (input is null || input.RequestId == Guid.Empty || input.ExpectedVersion < 0 ||
            input.Text is not { Length: > 0 and <= 2000 } || string.IsNullOrWhiteSpace(input.Text) ||
            input.Text.Any(ch => char.IsControl(ch) && ch is not '\n' and not '\t') || !PentaCapabilities.All.Contains(input.Capability))
            return new(400, Error: "Enter a valid prompt and conversation version.");
        var actor = (await users.GetUserAsync(principal))!.Id;
        var protector = Protector(academy, actor, sessionId);
        var digest = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(input))));
        MiniState state;
        // This short academy lock also serializes quotas and concurrent claims.
        await using (var claim = await db.Database.BeginTransactionAsync(token))
        {
            await LockAcademyAsync(academy, token);
            if (!await Reauthorize(principal, academy, token)) return new(403);
            var session = await OwnSession(academy, actor, sessionId, token);
            if (session is null) return new(404);
            if (session.ExpiresAtUtc <= Now) return new(410, Error: "This conversation expired. Start a new one.");
            var replay = await db.PentaMiniTurns.SingleOrDefaultAsync(x => x.AcademyId == academy && x.ActorUserId == actor &&
                x.SessionId == sessionId && x.RequestId == input.RequestId, token);
            if (replay is not null)
            {
                // Even a hash of a predictable prompt is private: compare only
                // after purpose-bound decryption, never persist an offline oracle.
                try { if (protector.Unprotect(replay.InputDigest) != digest) return new(409, Error: "This request ID was already used for another prompt."); }
                catch (CryptographicException) { return new(409, Error: "Stored context is unavailable. Start a new conversation."); }
                if (replay.ProtectedReceipt is null) return new(409, Error: "That request is pending or its outcome is unknown. Do not redispatch it; start a new conversation if needed.");
                // A replay cannot resurrect a stale ordinal result after newer turns.
                if (session.Version != replay.ExpectedVersion + 1) return new(409, Error: "A newer turn exists. Start a new conversation to resynchronize.");
                try
                {
                    var receipt = JsonSerializer.Deserialize<MiniReceipt>(protector.Unprotect(replay.ProtectedReceipt));
                    Audit(academy, actor, sessionId, input.RequestId, "Replayed", null, 0, null, input.Capability);
                    await db.SaveChangesAsync(token); await claim.CommitAsync(token);
                    return new(200, receipt);
                }
                catch (Exception ex) when (ex is CryptographicException or JsonException) { return new(409, Error: "Stored context is unavailable. Start a new conversation."); }
            }
            if (session.Version != input.ExpectedVersion || session.PendingRequestId is not null)
                return new(409, Error: "Another turn is pending or this view is stale. Start a new conversation to resynchronize.");
            var day = Now.Date;
            if (session.Version >= 50 || await db.PentaMiniTurns.CountAsync(x => x.AcademyId == academy && x.CreatedAtUtc >= day, token) >= 500 ||
                await db.PentaMiniTurns.CountAsync(x => x.AcademyId == academy && x.ActorUserId == actor && x.CreatedAtUtc >= day, token) >= 100)
                return new(429, Error: "The local pilot turn limit has been reached.");
            try { state = JsonSerializer.Deserialize<MiniState>(protector.Unprotect(session.ProtectedState))!; }
            catch (Exception ex) when (ex is CryptographicException or JsonException) { return new(409, Error: "Stored context is unavailable. Start a new conversation."); }
            if (!PentaMiniTools.ValidState(state)) return new(409, Error: "Stored context is invalid. Start a new conversation.");
            var ids = state.CurrentResultIds.Select(Guid.Parse).ToArray();
            if (await db.Students.CountAsync(x => x.AcademyId == academy && x.IsActive && ids.Contains(x.Id), token) != ids.Length)
                return new(409, Error: "The displayed records changed. Start a new conversation.");
            session.PendingRequestId = input.RequestId;
            db.PentaMiniTurns.Add(new PentaMiniTurn { AcademyId = academy, ActorUserId = actor, SessionId = sessionId,
                RequestId = input.RequestId, ExpectedVersion = input.ExpectedVersion, InputDigest = protector.Protect(digest) });
            Audit(academy, actor, sessionId, input.RequestId, "Claimed", null, 0, null, input.Capability);
            await db.SaveChangesAsync(token); await claim.CommitAsync(token);
        }
        var watch = System.Diagnostics.Stopwatch.StartNew();
        MiniProviderOutcome planned;
        try { planned = await provider.PlanAsync(new(sessionId.ToString("D"), input.Text.Trim(), state, PentaMiniTools.Available.ToArray()), token); }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException or IOException or JsonException)
        { planned = new(null, "Unavailable"); }
        // Caller cancellation never fabricates success; a durable pending claim blocks blind retry.
        token.ThrowIfCancellationRequested();
        db.ChangeTracker.Clear();
        await using var complete = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, token);
        await LockAcademyAsync(academy, token);
        if (!await Reauthorize(principal, academy, token))
        {
            Audit(academy, actor, sessionId, input.RequestId, "DeniedAfterPlanning", planned.Plan?.ToolCall?.Name, watch.ElapsedMilliseconds, "Authorization", input.Capability);
            await db.SaveChangesAsync(token); await complete.CommitAsync(token); return new(403);
        }
        var owned = await OwnSession(academy, actor, sessionId, token);
        if (owned is null || owned.ExpiresAtUtc <= Now || owned.PendingRequestId != input.RequestId || owned.Version != input.ExpectedVersion) return new(409);
        ConnectorOutcome outcome;
        var valid = PentaMiniTools.ValidPlan(planned.Plan);
        var plan = planned.Plan;
        if (!valid) outcome = new("ERROR", "PENTA Mini could not prepare a verified response. Your manual workspace remains available.", state);
        else if (plan!.Kind == "TOOL_REQUEST") outcome = await connector.ExecuteAsync(principal, academy, plan.ToolCall!, state, token);
        else outcome = new(plan.Kind, SafeMessage(plan.Kind), state);
        // Never adopt the model's returned state, free prose, risk or record facts.
        var receiptValue = new MiniReceipt(sessionId, input.RequestId, owned.Version + 1, outcome.Kind, outcome.Message,
            input.Capability, outcome.Result, outcome.State);
        var serialized = JsonSerializer.Serialize(receiptValue);
        if (serialized.Length > 40000) throw new InvalidOperationException("Result exceeds the private receipt bound.");
        owned.ProtectedState = protector.Protect(JsonSerializer.Serialize(outcome.State));
        owned.Version++; owned.PendingRequestId = null;
        var turn = await db.PentaMiniTurns.SingleAsync(x => x.AcademyId == academy && x.ActorUserId == actor && x.SessionId == sessionId && x.RequestId == input.RequestId, token);
        turn.Status = outcome.Kind; turn.ProtectedReceipt = protector.Protect(serialized);
        Audit(academy, actor, sessionId, input.RequestId, outcome.Kind, valid ? plan!.ToolCall?.Name : null,
            watch.ElapsedMilliseconds, planned.Error ?? (outcome.Kind == "ERROR" ? "ToolValidation" : null), input.Capability, valid ? plan : null);
        await db.SaveChangesAsync(token); await complete.CommitAsync(token);
        return new(201, receiptValue);
    }
    private Task<PentaMiniSession?> OwnSession(Guid academy, Guid actor, Guid session, CancellationToken token) =>
        db.PentaMiniSessions.SingleOrDefaultAsync(x => x.AcademyId == academy && x.ActorUserId == actor && x.Id == session, token);
    private async Task<bool> Reauthorize(ClaimsPrincipal principal, Guid academy, CancellationToken token)
    { identity.ChangeTracker.Clear(); return await policy.AllowsAsync(principal, academy, token); }
    private Task<int> LockAcademyAsync(Guid academy, CancellationToken token) => db.Database.ExecuteSqlInterpolatedAsync(
        $"SELECT [Id] FROM [Academies] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={academy}", token);
    private static string SafeMessage(string kind) => kind switch
    {
        "CLARIFICATION_REQUIRED" => "Please name a student or search for pending fees first, then choose from the displayed results.",
        "REFUSAL" => "That request is not permitted in this read-only pilot.",
        "ERROR" => "PENTA Mini is unavailable or could not interpret this request. Use the manual workspace or rephrase.",
        _ => "This pilot can find students, filter subjects and show verified outstanding fees. No writes, sends or scheduling are enabled."
    };
    private void Audit(Guid academy, Guid actor, Guid session, Guid request, string kind, string? tool, long latency, string? error,
        string? capability = null, MiniPlan? proposal = null)
    {
        db.AuditLogs.Add(new AuditLog { AcademyId = academy, ActorUserId = actor, EntityType = "PentaMiniSession", EntityId = session,
            Action = "PentaMini" + kind, OccurredAtUtc = Now, MetadataJson = JsonSerializer.Serialize(new {
                conversationId = session, requestId = request, capability, provider = "PentaMini", protocol = "0.1", riskClass = "READ",
                serviceVersion = "0.1.0", promptVersion = "planner-0.1", kind, tool = PentaMiniTools.Available.Contains(tool) ? tool : null,
                expectedModelRevision = PentaMiniLearningEvent.ExpectedModelRevision, runtimeModelAttested = false,
                argumentKeys = proposal?.ToolCall?.Arguments.Keys.Where(x => x is "name" or "subject" or "balance_status" or "sort_by" or "learner_id").Order().ToArray() ?? [],
                authorization = kind == "DeniedAfterPlanning" ? "Denied" : "Allowed", latencyMs = latency, error,
                learningEvent = PentaMiniLearningEvent.ForOutcome(academy, request, clock.GetUtcNow(), kind,
                    PentaMiniTools.Available.Contains(tool) ? tool : null, error)
            }) });
    }
}
