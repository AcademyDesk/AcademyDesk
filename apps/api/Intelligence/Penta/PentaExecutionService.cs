using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Intelligence.Penta;

public sealed record PentaTurnState(Guid TaskId, Guid ExecutionId, string Status,
    Guid? CorrelationId, string Capability, string Tool, string? Message, bool Synthetic = true);

public sealed record PentaTurnOutcome(int HttpStatus, PentaTurnState? State, string? Error = null);

/// <summary>Single SQL-backed claim owner for the disabled-by-default synthetic pilot.</summary>
public sealed class PentaExecutionService(AcademyDeskDbContext db, PentaSyntheticDispatcher dispatcher)
{
    public async Task<PentaTurnOutcome> RunAsync(Guid academyId, Guid actorId, string capability,
        string text, string key, CancellationToken token)
    {
        // Delimit fields so different argument tuples cannot share a digest.
        var digest = Convert.ToHexString(SHA256.HashData(
            Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new[] { capability, text }))));
        var existing = await FindAsync(academyId, actorId, key, token);
        if (existing is not null) return Replay(existing.Value.Execution, existing.Value.Task, digest);

        var task = new PentaTask { AcademyId = academyId, ActorUserId = actorId,
            Capability = capability, Status = "Executing" };
        var execution = new PentaExecution { AcademyId = academyId, TaskId = task.Id,
            ActorUserId = actorId, ToolName = PentaSyntheticDispatcher.ToolName,
            IdempotencyKey = key, InputDigest = digest, Status = "Executing" };
        db.PentaTasks.Add(task);
        db.PentaExecutions.Add(execution);
        try
        {
            // SaveChanges is an atomic transaction for both rows on SQL Server.
            await db.SaveChangesAsync(token);
        }
        catch (DbUpdateException exception) when (exception.InnerException is SqlException { Number: 2601 or 2627 })
        {
            db.ChangeTracker.Clear();
            existing = await FindAsync(academyId, actorId, key, token);
            if (existing is null) throw;
            return Replay(existing.Value.Execution, existing.Value.Task, digest);
        }

        PentaSyntheticResult? result = null;
        string status;
        try
        {
            result = await dispatcher.RunAsync(capability, token);
            status = result is null ? "Failed" : "Succeeded";
        }
        catch (Exception) // A claimed key must never silently become executable again.
        {
            status = "OutcomeUnknown";
        }

        task.Status = status;
        execution.Status = status;
        execution.ResultCorrelationId = result?.CorrelationId;
        execution.ResultMessage = result?.Message;
        execution.CompletedAtUtc = DateTime.UtcNow;
        db.PentaAttempts.Add(new PentaAttempt { AcademyId = academyId, ExecutionId = execution.Id, Outcome = status });
        db.AuditLogs.Add(new AuditLog { AcademyId = academyId, ActorUserId = actorId,
            Action = "PentaSyntheticExecution", EntityType = "PentaExecution", EntityId = execution.Id,
            MetadataJson = JsonSerializer.Serialize(new { taskId = task.Id, status, tool = execution.ToolName }) });
        // Outcome, attempt and audit are committed together; never log prompt text.
        await db.SaveChangesAsync(CancellationToken.None);
        return Replay(execution, task, digest);
    }

    public async Task<PentaTurnState?> GetAsync(Guid academyId, Guid actorId, Guid taskId, CancellationToken token)
    {
        var pair = await (from execution in db.PentaExecutions.AsNoTracking()
            join task in db.PentaTasks.AsNoTracking() on new { execution.AcademyId, Id = execution.TaskId }
                equals new { task.AcademyId, task.Id }
            where execution.AcademyId == academyId && execution.ActorUserId == actorId && task.Id == taskId
            select new { execution, task }).SingleOrDefaultAsync(token);
        return pair is null ? null : ToState(pair.execution, pair.task);
    }

    private async Task<(PentaExecution Execution, PentaTask Task)?> FindAsync(
        Guid academyId, Guid actorId, string key, CancellationToken token)
    {
        var pair = await (from execution in db.PentaExecutions.AsNoTracking()
            join task in db.PentaTasks.AsNoTracking() on new { execution.AcademyId, Id = execution.TaskId }
                equals new { task.AcademyId, task.Id }
            where execution.AcademyId == academyId && execution.ActorUserId == actorId &&
                  execution.ToolName == PentaSyntheticDispatcher.ToolName && execution.IdempotencyKey == key
            select new { execution, task }).SingleOrDefaultAsync(token);
        return pair is null ? null : (pair.execution, pair.task);
    }

    private static PentaTurnOutcome Replay(PentaExecution execution, PentaTask task, string digest)
    {
        if (!string.Equals(execution.InputDigest, digest, StringComparison.Ordinal))
            return new(409, null, "Idempotency key was already used for different arguments.");
        var state = ToState(execution, task);
        return execution.Status switch
        {
            "Succeeded" => new(200, state),
            "Executing" => new(202, state),
            _ => new(502, state, "PENTA diagnostic did not complete; use a new request key only after review.")
        };
    }

    private static PentaTurnState ToState(PentaExecution execution, PentaTask task) =>
        new(task.Id, execution.Id, execution.Status, execution.ResultCorrelationId,
            task.Capability, execution.ToolName, execution.ResultMessage);
}
