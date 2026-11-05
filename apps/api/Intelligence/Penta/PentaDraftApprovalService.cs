using System.Globalization;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using AcademyDesk.Api.Domain.Identity;

namespace AcademyDesk.Api.Intelligence.Penta;

public sealed record PentaDraftPreviewState(Guid ApprovalId, Guid TaskId, Guid ExecutionId,
    string Title, string Status, DateTime ExpiresAtUtc, string Digest,
    string Effect = "Approval recorded only. No academy work item or message will be created.");

public sealed record PentaDraftApprovalOutcome(int HttpStatus, PentaDraftPreviewState? State,
    string? Error = null);

/// <summary>
/// Local synthetic R1 authority rehearsal. Approval is durable, but there is
/// deliberately no domain mutation or external action adapter.
/// </summary>
public sealed class PentaDraftApprovalService(AcademyDeskDbContext db, PentaPilotPolicy policy,
    UserManager<ApplicationUser> users, TimeProvider clock)
{
    private const string Tool = "drafts.prepare.synthetic.v1";
    private const string PolicyVersion = "penta.synthetic-draft.v1";
    private const int DailyUserLimit = 20;
    private const int DailyAcademyLimit = 100;

    public async Task<PentaDraftApprovalOutcome> PrepareAsync(ClaimsPrincipal principal, Guid academyId,
        string? rawTitle, string? key, CancellationToken token)
    {
        var title = NormalizeTitle(rawTitle);
        if (title is null || !ValidKey(key))
            return new(400, null, "A short title and valid Idempotency-Key are required.");
        if (!await policy.AllowsAsync(principal, academyId, token)) return new(403, null);
        var actor = (await users.GetUserAsync(principal))!;
        if (db.Database.IsRelational() && !db.Database.IsSqlServer()) return new(503, null);
        await using var transaction = db.Database.IsSqlServer()
            ? await db.Database.BeginTransactionAsync(token) : null;
        if (transaction is not null && !await LockAcademyAsync(academyId, token)) return new(403, null);
        if (!await policy.AllowsAsync(principal, academyId, token)) return new(403, null);

        var existing = await FindByKeyAsync(academyId, actor.Id, key!, token);
        if (existing is not null)
            return ValidStored(academyId, actor.Id, existing.Value) &&
                existing.Value.Execution.ProposalPayloadJson == Payload(title)
                ? new(200, ToState(existing.Value))
                : new(409, null, "Idempotency key was already used for different arguments.");
        if (await db.PentaExecutions.AnyAsync(x => x.AcademyId == academyId &&
            x.ActorUserId == actor.Id && x.ToolName == Tool && x.IdempotencyKey == key, token))
            return new(409, null, "The stored preview is incomplete.");

        var today = clock.GetUtcNow().UtcDateTime.Date;
        var proposals = db.PentaExecutions.AsNoTracking().Where(x => x.AcademyId == academyId &&
            x.ToolName == Tool && x.CreatedAtUtc >= today);
        if (await proposals.CountAsync(token) >= DailyAcademyLimit ||
            await proposals.CountAsync(x => x.ActorUserId == actor.Id, token) >= DailyUserLimit)
            return new(429, null, "PENTA draft preview daily limit reached.");

        var now = clock.GetUtcNow().UtcDateTime;
        var task = new PentaTask { AcademyId = academyId, ActorUserId = actor.Id,
            Capability = "executor", Status = "AwaitingApproval" };
        var execution = new PentaExecution { AcademyId = academyId, TaskId = task.Id,
            ActorUserId = actor.Id, ToolName = Tool, IdempotencyKey = key!,
            InputDigest = Hex($"{task.Id:D}:{title}"), ProposalPayloadJson = Payload(title),
            ApprovalMode = "Explicit", Status = "AwaitingApproval" };
        var approval = new PentaApproval { AcademyId = academyId, ExecutionId = execution.Id,
            InitiatorUserId = actor.Id, Status = "AwaitingApproval", PolicyVersion = PolicyVersion,
            ExpiresAtUtc = now.AddMinutes(10), DecisionDigest = string.Empty };
        approval.DecisionDigest = Digest(academyId, actor.Id, execution.Id, approval.Id, title,
            approval.ExpiresAtUtc);
        db.PentaTasks.Add(task);
        db.PentaExecutions.Add(execution);
        db.PentaApprovals.Add(approval);
        db.AuditLogs.Add(new AuditLog { AcademyId = academyId, ActorUserId = actor.Id,
            Action = "PentaSyntheticDraftPrepared", EntityType = "PentaApproval", EntityId = approval.Id,
            MetadataJson = JsonSerializer.Serialize(new { taskId = task.Id, tool = Tool, policy = PolicyVersion }) });
        await db.SaveChangesAsync(token);
        if (transaction is not null) await transaction.CommitAsync(token);
        return new(201, ToState((approval, execution, task)));
    }

    public async Task<PentaDraftApprovalOutcome> GetAsync(ClaimsPrincipal principal, Guid academyId,
        Guid approvalId, CancellationToken token)
    {
        if (!await policy.AllowsAsync(principal, academyId, token)) return new(403, null);
        var actor = (await users.GetUserAsync(principal))!;
        var pair = await FindByApprovalAsync(academyId, actor.Id, approvalId, token);
        return pair is null ? new(404, null) : ValidStored(academyId, actor.Id, pair.Value)
            ? new(200, ToState(pair.Value)) : new(409, null, "The stored preview failed integrity validation.");
    }

    public async Task<PentaDraftApprovalOutcome> ConfirmAsync(ClaimsPrincipal principal, Guid academyId,
        Guid approvalId, string? digest, CancellationToken token)
    {
        if (digest is null || digest.Length != 64 || !digest.All(Uri.IsHexDigit))
            return new(400, null, "A valid preview digest is required.");
        if (!await policy.AllowsAsync(principal, academyId, token)) return new(403, null);
        var actor = (await users.GetUserAsync(principal))!;
        if (db.Database.IsRelational() && !db.Database.IsSqlServer()) return new(503, null);
        await using var transaction = db.Database.IsSqlServer()
            ? await db.Database.BeginTransactionAsync(token) : null;
        if (transaction is not null && !await LockAcademyAsync(academyId, token)) return new(403, null);
        if (!await policy.AllowsAsync(principal, academyId, token)) return new(403, null);

        var pair = await FindByApprovalAsync(academyId, actor.Id, approvalId, token, tracked: true);
        if (pair is null) return new(404, null);
        var (approval, execution, task) = pair.Value;
        if (approval.PolicyVersion != PolicyVersion || execution.ToolName != Tool ||
            execution.ApprovalMode != "Explicit" || execution.TaskId != task.Id ||
            approval.InitiatorUserId != actor.Id ||
            !string.Equals(digest, approval.DecisionDigest, StringComparison.OrdinalIgnoreCase))
            return new(409, null, "The preview no longer matches this approval.");
        if (!ValidStored(academyId, actor.Id, pair.Value))
            return new(409, null, "The stored preview failed integrity validation.");
        if (approval.Status == "Approved") return new(200, ToState(pair.Value));
        if (approval.Status != "AwaitingApproval" || execution.Status != "AwaitingApproval" ||
            task.Status != "AwaitingApproval") return new(409, null, "The preview is no longer awaiting confirmation.");
        if (approval.ExpiresAtUtc <= clock.GetUtcNow().UtcDateTime)
        {
            approval.Status = execution.Status = task.Status = "Expired";
            db.AuditLogs.Add(new AuditLog { AcademyId = academyId, ActorUserId = actor.Id,
                Action = "PentaSyntheticDraftExpired", EntityType = "PentaApproval", EntityId = approval.Id });
            await db.SaveChangesAsync(token);
            if (transaction is not null) await transaction.CommitAsync(token);
            return new(410, ToState(pair.Value), "The preview expired; prepare a new one.");
        }
        approval.Status = execution.Status = task.Status = "Approved";
        approval.ApproverUserId = actor.Id;
        approval.DecidedAtUtc = clock.GetUtcNow().UtcDateTime;
        db.AuditLogs.Add(new AuditLog { AcademyId = academyId, ActorUserId = actor.Id,
            Action = "PentaSyntheticDraftApproved", EntityType = "PentaApproval", EntityId = approval.Id,
            MetadataJson = JsonSerializer.Serialize(new { taskId = task.Id, tool = Tool, policy = PolicyVersion,
                noDomainEffect = true }) });
        await db.SaveChangesAsync(token);
        if (transaction is not null) await transaction.CommitAsync(token);
        return new(200, ToState(pair.Value));
    }

    private async Task<bool> LockAcademyAsync(Guid academyId, CancellationToken token) =>
        await db.Academies.FromSqlInterpolated(
            $"SELECT * FROM [Academies] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = {academyId}")
            .AsNoTracking().AnyAsync(token);

    private async Task<(PentaApproval Approval, PentaExecution Execution, PentaTask Task)?> FindByKeyAsync(
        Guid academyId, Guid actorId, string key, CancellationToken token)
    {
        var execution = await db.PentaExecutions.AsNoTracking().SingleOrDefaultAsync(x =>
            x.AcademyId == academyId && x.ActorUserId == actorId && x.ToolName == Tool &&
            x.IdempotencyKey == key, token);
        return execution is null ? null : await LoadAsync(academyId, execution, token);
    }

    private async Task<(PentaApproval Approval, PentaExecution Execution, PentaTask Task)?> FindByApprovalAsync(
        Guid academyId, Guid actorId, Guid approvalId, CancellationToken token, bool tracked = false)
    {
        var approval = await (tracked ? db.PentaApprovals : db.PentaApprovals.AsNoTracking())
            .SingleOrDefaultAsync(x => x.AcademyId == academyId && x.Id == approvalId &&
                x.InitiatorUserId == actorId, token);
        if (approval is null) return null;
        var execution = await (tracked ? db.PentaExecutions : db.PentaExecutions.AsNoTracking())
            .SingleOrDefaultAsync(x => x.AcademyId == academyId && x.Id == approval.ExecutionId &&
                x.ActorUserId == actorId && x.ToolName == Tool, token);
        return execution is null ? null : await LoadAsync(academyId, execution, token, approval, tracked);
    }

    private async Task<(PentaApproval Approval, PentaExecution Execution, PentaTask Task)?> LoadAsync(
        Guid academyId, PentaExecution execution, CancellationToken token,
        PentaApproval? approval = null, bool tracked = false)
    {
        approval ??= await (tracked ? db.PentaApprovals : db.PentaApprovals.AsNoTracking())
            .SingleOrDefaultAsync(x => x.AcademyId == academyId && x.ExecutionId == execution.Id, token);
        var task = await (tracked ? db.PentaTasks : db.PentaTasks.AsNoTracking())
            .SingleOrDefaultAsync(x => x.AcademyId == academyId && x.Id == execution.TaskId &&
                x.ActorUserId == execution.ActorUserId, token);
        return approval is null || task is null ? null : (approval, execution, task);
    }

    private PentaDraftPreviewState ToState(
        (PentaApproval Approval, PentaExecution Execution, PentaTask Task) pair) =>
        new(pair.Approval.Id, pair.Task.Id, pair.Execution.Id,
            ReadTitle(pair.Execution.ProposalPayloadJson) ?? string.Empty,
            pair.Approval.Status == "AwaitingApproval" &&
                pair.Approval.ExpiresAtUtc <= clock.GetUtcNow().UtcDateTime
                ? "Expired" : pair.Approval.Status,
            pair.Approval.ExpiresAtUtc, pair.Approval.DecisionDigest);

    private static bool ValidStored(Guid academyId, Guid actorId,
        (PentaApproval Approval, PentaExecution Execution, PentaTask Task) pair)
    {
        var title = ReadTitle(pair.Execution.ProposalPayloadJson);
        return title is not null && pair.Approval.PolicyVersion == PolicyVersion &&
            pair.Execution.ToolName == Tool && pair.Execution.ApprovalMode == "Explicit" &&
            pair.Execution.ActorUserId == actorId && pair.Task.ActorUserId == actorId &&
            pair.Approval.InitiatorUserId == actorId && pair.Execution.TaskId == pair.Task.Id &&
            pair.Approval.ExecutionId == pair.Execution.Id &&
            pair.Approval.DecisionDigest == Digest(academyId, actorId, pair.Execution.Id,
                pair.Approval.Id, title, pair.Approval.ExpiresAtUtc);
    }

    private static string? NormalizeTitle(string? value)
    {
        var title = value?.Trim().Normalize(NormalizationForm.FormC);
        // This local authority rehearsal must not persist arbitrary academy text.
        // User-authored drafts require a separate retention and protection policy.
        return title is "Review schedule" or "Review attendance" ? title : null;
    }

    private static bool ValidKey(string? value) => value is { Length: >= 8 and <= 128 } &&
        value.All(ch => ch is >= '!' and <= '~');

    private static string Payload(string title) => JsonSerializer.Serialize(new[] { title });

    private static string? ReadTitle(string? payload)
    {
        try
        {
            var titles = JsonSerializer.Deserialize<string[]>(payload ?? "");
            return titles is { Length: 1 } && NormalizeTitle(titles[0]) == titles[0] ? titles[0] : null;
        }
        catch (JsonException) { return null; }
    }

    private static string Digest(Guid academyId, Guid actorId, Guid executionId,
        Guid approvalId, string title, DateTime expiry) => Hex(JsonSerializer.Serialize(new[]
    {
        Tool, PolicyVersion, academyId.ToString("D"), actorId.ToString("D"),
        executionId.ToString("D"), approvalId.ToString("D"), title,
        expiry.Ticks.ToString(CultureInfo.InvariantCulture), "AwaitingApproval"
    }));

    private static string Hex(string value) => Convert.ToHexString(
        SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
