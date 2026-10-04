using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Globalization;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Intelligence.Penta;

public sealed record PentaTurnState(Guid TaskId, Guid ExecutionId, string Status,
    Guid? CorrelationId, string Capability, string Tool, string? Message, bool Synthetic = true);

public sealed record PentaTurnOutcome(int HttpStatus, PentaTurnState? State, string? Error = null);

/// <summary>Single SQL-backed claim owner for the disabled-by-default synthetic pilot.</summary>
public sealed class PentaExecutionService(AcademyDeskDbContext db, PentaSyntheticDispatcher dispatcher,
    IConfiguration configuration)
{
    public async Task<PentaTurnOutcome> RunAsync(Guid academyId, Guid actorId, string capability,
        string text, string key, CancellationToken token)
    {
        // Delimit fields so different argument tuples cannot share a digest.
        var digest = Convert.ToHexString(SHA256.HashData(
            Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new[] { capability, text }))));
        var existing = await FindAsync(academyId, actorId, key, token);
        if (existing is not null) return Replay(existing.Value.Execution, existing.Value.Task, digest);

        var userLimit = PilotLimit("Penta:SyntheticDailyUserLimit", 20, 10000);
        var academyLimit = PilotLimit("Penta:SyntheticDailyAcademyLimit", 100, 100000);
        if (userLimit is null || academyLimit is null)
            return new(503, null, "PENTA diagnostic budget configuration is invalid.");

        if (db.Database.IsRelational() && !db.Database.IsSqlServer())
            return new(503, null, "PENTA diagnostic budget store is unsupported.");
        await using var claimTransaction = db.Database.IsSqlServer()
            ? await db.Database.BeginTransactionAsync(token) : null;
        if (claimTransaction is not null)
        {
            // All pilot claims for this academy lock the same tenant row first.
            // The quota count and insertion must share this SQL transaction.
            var academy = await db.Academies.FromSqlInterpolated(
                $"SELECT * FROM [Academies] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = {academyId}")
                .AsNoTracking().SingleOrDefaultAsync(token);
            if (academy is null || !academy.IsActive) return new(403, null, "Academy is unavailable.");
        }
        // A second request may have claimed this key while we waited for the lock.
        existing = await FindAsync(academyId, actorId, key, token);
        if (existing is not null) return Replay(existing.Value.Execution, existing.Value.Task, digest);

        var windowStart = DateTime.UtcNow.Date;
        var reservations = db.PentaUsageReservations.AsNoTracking()
            .Where(x => x.AcademyId == academyId && x.WindowStartUtc == windowStart &&
                        x.UnitKind == "SyntheticCall");
        var academyUsed = await reservations.SumAsync(x => (int?)x.UnitsReserved, token) ?? 0;
        var actorUsed = await reservations.Where(x => x.ActorUserId == actorId)
            .SumAsync(x => (int?)x.UnitsReserved, token) ?? 0;
        if (academyUsed >= academyLimit.Value || actorUsed >= userLimit.Value)
            return new(429, null, "PENTA diagnostic daily limit reached.");

        var task = new PentaTask { AcademyId = academyId, ActorUserId = actorId,
            Capability = capability, Status = "Executing" };
        var execution = new PentaExecution { AcademyId = academyId, TaskId = task.Id,
            ActorUserId = actorId, ToolName = PentaSyntheticDispatcher.ToolName,
            IdempotencyKey = key, InputDigest = digest, Status = "Executing" };
        var reservation = new PentaUsageReservation
        {
            AcademyId = academyId, ExecutionId = execution.Id, ActorUserId = actorId,
            WindowStartUtc = windowStart, UnitsReserved = 1, UnitKind = "SyntheticCall",
            Status = "Reserved", EstimatedCost = 0m, Currency = "USD",
            PriceVersion = "synthetic.v1"
        };
        db.PentaTasks.Add(task);
        db.PentaExecutions.Add(execution);
        db.PentaUsageReservations.Add(reservation);
        try
        {
            // A failed claim cannot dispatch the provider or consume quota.
            await db.SaveChangesAsync(token);
            if (claimTransaction is not null)
            {
                await claimTransaction.CommitAsync(token);
                await claimTransaction.DisposeAsync();
            }
        }
        catch (DbUpdateException exception) when (exception.InnerException is SqlException { Number: 2601 or 2627 })
        {
            if (claimTransaction is not null)
            {
                await claimTransaction.RollbackAsync(token);
                await claimTransaction.DisposeAsync();
            }
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
        reservation.Status = status == "OutcomeUnknown" ? "UsageUnknown" : "Reconciled";
        if (status != "OutcomeUnknown")
        {
            reservation.ActualCost = 0m;
            db.PentaUsageEntries.Add(new PentaUsageEntry
            {
                AcademyId = academyId, ReservationId = reservation.Id, EventKey = "synthetic-final",
                Outcome = status, UnitsObserved = 1, ActualCost = 0m, Currency = "USD"
            });
        }
        db.PentaAttempts.Add(new PentaAttempt { AcademyId = academyId, ExecutionId = execution.Id, Outcome = status });
        db.AuditLogs.Add(new AuditLog { AcademyId = academyId, ActorUserId = actorId,
            Action = "PentaSyntheticExecution", EntityType = "PentaExecution", EntityId = execution.Id,
            MetadataJson = JsonSerializer.Serialize(new { taskId = task.Id, status, tool = execution.ToolName }) });
        // Outcome, known usage, attempt and audit are committed together; never log prompt text.
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

    private int? PilotLimit(string key, int fallback, int maximum)
    {
        var raw = configuration[key];
        if (raw is null) return fallback;
        return int.TryParse(raw, NumberStyles.None, CultureInfo.InvariantCulture, out var value) &&
               value >= 1 && value <= maximum ? value : null;
    }
}
