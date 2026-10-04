using System.Text.Json;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Intelligence.Penta;

// Internal-only contract. No endpoint, live provider, or client-supplied price is enabled.
public sealed record PentaPricedPolicy(string Provider, string Model, string PriceVersion,
    decimal InputUsdPerMillion, decimal OutputUsdPerMillion,
    int MaxInputTokens, int MaxOutputTokens, decimal DailyUserUsd, decimal DailyAcademyUsd);

public enum PentaBudgetResult { Reserved, Reconciled, AlreadyReconciled, Exhausted, InvalidPolicy, InvalidUsage, NotFound }

public sealed class PentaBudgetService(AcademyDeskDbContext db)
{
    private const string ModelUnit = "ModelCost";

    public async Task<PentaBudgetResult> ReserveAsync(Guid academyId, Guid actorId, Guid executionId,
        PentaPricedPolicy policy, CancellationToken token)
    {
        if (!Valid(policy) || !db.Database.IsSqlServer()) return PentaBudgetResult.InvalidPolicy;
        var worst = Charge(policy.MaxInputTokens, policy.MaxOutputTokens, policy);
        if (worst <= 0 || worst > policy.DailyUserUsd || worst > policy.DailyAcademyUsd)
            return PentaBudgetResult.Exhausted;

        await using var transaction = await db.Database.BeginTransactionAsync(token);
        if (!await LockAcademyAsync(academyId, token)) return PentaBudgetResult.NotFound;
        var execution = await db.PentaExecutions.AsNoTracking().SingleOrDefaultAsync(
            x => x.AcademyId == academyId && x.Id == executionId && x.ActorUserId == actorId, token);
        if (execution is null || execution.Status != "Executing" ||
            !await db.PentaTasks.AnyAsync(x => x.AcademyId == academyId && x.Id == execution.TaskId &&
                x.Status == "Executing", token)) return PentaBudgetResult.NotFound;
        // A model can never be called on an execution that already owns a reservation.
        if (await db.PentaUsageReservations.AnyAsync(x => x.AcademyId == academyId && x.ExecutionId == executionId, token))
            return PentaBudgetResult.InvalidPolicy;

        var start = DateTime.UtcNow.Date;
        if (await db.PentaUsageReservations.AnyAsync(x => x.AcademyId == academyId &&
            x.WindowStartUtc == start && x.UnitKind == ModelUnit && x.Currency != "USD", token))
            return PentaBudgetResult.InvalidPolicy;
        var rows = db.PentaUsageReservations.AsNoTracking().Where(x => x.AcademyId == academyId &&
            x.WindowStartUtc == start && x.UnitKind == ModelUnit && x.Currency == "USD");
        // Unknown usage continues to consume its entire worst-case reservation.
        var academyUsed = await rows.SumAsync(x => (decimal?)(x.Status == "Reconciled" && x.ActualCost != null
            ? x.ActualCost.Value : x.EstimatedCost), token) ?? 0m;
        var userUsed = await rows.Where(x => x.ActorUserId == actorId)
            .SumAsync(x => (decimal?)(x.Status == "Reconciled" && x.ActualCost != null
                ? x.ActualCost.Value : x.EstimatedCost), token) ?? 0m;
        if (academyUsed + worst > policy.DailyAcademyUsd || userUsed + worst > policy.DailyUserUsd)
            return PentaBudgetResult.Exhausted;

        db.PentaUsageReservations.Add(new PentaUsageReservation
        {
            AcademyId = academyId, ActorUserId = actorId, ExecutionId = executionId,
            WindowStartUtc = start, UnitsReserved = policy.MaxInputTokens + policy.MaxOutputTokens,
            UnitKind = ModelUnit, Status = "Reserved", EstimatedCost = worst,
            Currency = "USD", PriceVersion = policy.PriceVersion,
            ProviderName = policy.Provider, ModelName = policy.Model,
            InputTokensReserved = policy.MaxInputTokens, OutputTokensReserved = policy.MaxOutputTokens,
            InputUsdPerMillion = policy.InputUsdPerMillion,
            OutputUsdPerMillion = policy.OutputUsdPerMillion
        });
        db.AuditLogs.Add(new AuditLog { AcademyId = academyId, ActorUserId = actorId,
            Action = "PentaModelBudgetReserved", EntityType = "PentaExecution", EntityId = executionId,
            MetadataJson = JsonSerializer.Serialize(new { policy.Provider, policy.Model, policy.PriceVersion, worst }) });
        await db.SaveChangesAsync(token);
        await transaction.CommitAsync(token);
        return PentaBudgetResult.Reserved;
    }

    public async Task<PentaBudgetResult> ReconcileAsync(Guid academyId, Guid executionId,
        string eventKey, int inputTokens, int outputTokens, PentaPricedPolicy policy, CancellationToken token)
    {
        if (!Valid(policy) || !db.Database.IsSqlServer() || string.IsNullOrWhiteSpace(eventKey) || eventKey.Length > 60)
            return PentaBudgetResult.InvalidPolicy;
        await using var transaction = await db.Database.BeginTransactionAsync(token);
        if (!await LockAcademyAsync(academyId, token)) return PentaBudgetResult.NotFound;
        var row = await db.PentaUsageReservations.SingleOrDefaultAsync(x => x.AcademyId == academyId &&
            x.ExecutionId == executionId && x.UnitKind == ModelUnit, token);
        if (row is null) return PentaBudgetResult.NotFound;
        if (row.ProviderName != policy.Provider || row.ModelName != policy.Model ||
            row.PriceVersion != policy.PriceVersion || row.Currency != "USD" ||
            row.InputTokensReserved != policy.MaxInputTokens || row.OutputTokensReserved != policy.MaxOutputTokens)
            return PentaBudgetResult.InvalidPolicy;
        if (row.InputUsdPerMillion != policy.InputUsdPerMillion ||
            row.OutputUsdPerMillion != policy.OutputUsdPerMillion)
            return PentaBudgetResult.InvalidPolicy;
        if (row.Status == "Reconciled") return PentaBudgetResult.AlreadyReconciled;
        if (row.Status is not ("Reserved" or "UsageUnknown")) return PentaBudgetResult.InvalidPolicy;
        if (inputTokens < 0 || outputTokens < 0 || inputTokens > row.InputTokensReserved ||
            outputTokens > row.OutputTokensReserved)
            return PentaBudgetResult.InvalidUsage;
        var actual = Charge(inputTokens, outputTokens, policy);
        if (actual > row.EstimatedCost) return PentaBudgetResult.InvalidUsage;
        row.ActualCost = actual;
        row.InputTokensObserved = inputTokens;
        row.OutputTokensObserved = outputTokens;
        row.Status = "Reconciled";
        db.PentaUsageEntries.Add(new PentaUsageEntry { AcademyId = academyId, ReservationId = row.Id,
            EventKey = eventKey, Outcome = "Observed", UnitsObserved = inputTokens + outputTokens,
            ActualCost = actual, Currency = "USD" });
        db.AuditLogs.Add(new AuditLog { AcademyId = academyId, ActorUserId = row.ActorUserId,
            Action = "PentaModelBudgetReconciled", EntityType = "PentaExecution", EntityId = executionId,
            MetadataJson = JsonSerializer.Serialize(new { eventKey, inputTokens, outputTokens, actual }) });
        await db.SaveChangesAsync(token);
        await transaction.CommitAsync(token);
        return PentaBudgetResult.Reconciled;
    }

    // Deliberate operator/recovery invocation only. An in-flight or stale claim is
    // never deemed safe to re-dispatch merely because the original host disappeared.
    public async Task<bool> MarkStaleUnknownAsync(Guid academyId, Guid executionId,
        DateTime olderThanUtc, CancellationToken token)
    {
        if (!db.Database.IsSqlServer() || olderThanUtc.Kind != DateTimeKind.Utc ||
            olderThanUtc > DateTime.UtcNow.AddMinutes(-5)) return false;
        await using var transaction = await db.Database.BeginTransactionAsync(token);
        if (!await LockAcademyAsync(academyId, token)) return false;
        var execution = await db.PentaExecutions.SingleOrDefaultAsync(x => x.AcademyId == academyId &&
            x.Id == executionId && x.Status == "Executing" && x.CreatedAtUtc < olderThanUtc, token);
        if (execution is null) return false;
        var task = await db.PentaTasks.SingleAsync(x => x.AcademyId == academyId && x.Id == execution.TaskId, token);
        if (task.Status != "Executing") return false;
        var reservation = await db.PentaUsageReservations.SingleOrDefaultAsync(x => x.AcademyId == academyId &&
            x.ExecutionId == executionId, token);
        if (reservation is not null && reservation.Status != "Reserved") return false;
        execution.Status = task.Status = "OutcomeUnknown";
        execution.CompletedAtUtc = DateTime.UtcNow;
        if (reservation is not null) reservation.Status = "UsageUnknown";
        db.PentaAttempts.Add(new PentaAttempt { AcademyId = academyId, ExecutionId = executionId,
            Outcome = "OutcomeUnknown" });
        db.AuditLogs.Add(new AuditLog { AcademyId = academyId, ActorUserId = execution.ActorUserId,
            Action = "PentaStaleClaimMarkedUnknown", EntityType = "PentaExecution", EntityId = executionId,
            MetadataJson = JsonSerializer.Serialize(new { taskId = task.Id, reservationHeld = reservation is not null }) });
        await db.SaveChangesAsync(token);
        await transaction.CommitAsync(token);
        return true;
    }

    private async Task<bool> LockAcademyAsync(Guid academyId, CancellationToken token) =>
        await db.Academies.FromSqlInterpolated(
            $"SELECT * FROM [Academies] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = {academyId}")
            .AsNoTracking().AnyAsync(token);

    private static decimal Charge(int inputTokens, int outputTokens, PentaPricedPolicy policy) =>
        decimal.Ceiling(((inputTokens * policy.InputUsdPerMillion + outputTokens * policy.OutputUsdPerMillion)
            / 1_000_000m) * 1_000_000m) / 1_000_000m;

    private static bool Valid(PentaPricedPolicy? p) => p is not null &&
        !string.IsNullOrWhiteSpace(p.Provider) && p.Provider.Length <= 80 &&
        !string.IsNullOrWhiteSpace(p.Model) && p.Model.Length <= 80 &&
        !string.IsNullOrWhiteSpace(p.PriceVersion) && p.PriceVersion.Length <= 40 &&
        p.InputUsdPerMillion > 0 && p.InputUsdPerMillion <= 1000m &&
        p.OutputUsdPerMillion > 0 && p.OutputUsdPerMillion <= 1000m &&
        decimal.Round(p.InputUsdPerMillion, 6) == p.InputUsdPerMillion &&
        decimal.Round(p.OutputUsdPerMillion, 6) == p.OutputUsdPerMillion &&
        p.MaxInputTokens > 0 && p.MaxInputTokens <= 1_000_000 &&
        p.MaxOutputTokens > 0 && p.MaxOutputTokens <= 1_000_000 &&
        (long)p.MaxInputTokens + p.MaxOutputTokens <= int.MaxValue &&
        p.DailyUserUsd > 0 && p.DailyAcademyUsd > 0 &&
        decimal.Round(p.DailyUserUsd, 6) == p.DailyUserUsd &&
        decimal.Round(p.DailyAcademyUsd, 6) == p.DailyAcademyUsd &&
        p.DailyUserUsd <= 1_000_000m && p.DailyAcademyUsd <= 1_000_000m;
}
