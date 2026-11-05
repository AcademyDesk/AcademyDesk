using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Intelligence.Penta;

public sealed record PentaBatchSearchRow(Guid SourceId, string Name,
    string? BatchCode, string SourcePath);

public sealed record PentaBatchSearchResult(string Tool, string Capability,
    Guid AcademyId, DateTime AsOfUtc, string TimeZone, string Source,
    int MaxRows, bool HasMore, IReadOnlyList<PentaBatchSearchRow> Rows);

/// <summary>Fixed R0 active-batch search; no schedule, teacher, meeting or capacity claims.</summary>
public sealed class PentaBatchSearchService(AcademyDeskDbContext db, TimeProvider clock)
{
    public const string ToolName = "batch.search.v1";
    public const int MaximumRows = 10;

    public static bool TryNormalizeQuery(string? input, out string query)
    {
        query = input?.Trim() ?? string.Empty;
        return query.Length is >= 2 and <= 80 && !query.Any(char.IsControl);
    }

    public async Task<PentaBatchSearchResult?> ReadAsync(Guid academyId, Guid actorId,
        string query, CancellationToken token)
    {
        if (!TryNormalizeQuery(query, out var normalized) || normalized != query)
            throw new ArgumentException("A normalized search query is required.", nameof(query));

        var academy = await db.Academies.AsNoTracking()
            .Where(x => x.Id == academyId && x.IsActive)
            .Select(x => new { x.Id, x.TimeZone })
            .SingleOrDefaultAsync(token);
        if (academy is null) return null;

        // Both filtering and the 11-row lookahead happen in SQL. No batch entity,
        // schedule JSON, meeting URL or teacher relationship is materialized.
        var matches = await db.Batches.AsNoTracking()
            .Where(x => x.AcademyId == academyId && x.IsActive &&
                (x.Name.Contains(normalized) ||
                 x.BatchCode != null && x.BatchCode.Contains(normalized)))
            .OrderBy(x => x.Name).ThenBy(x => x.Id)
            .Select(x => new { x.Id, x.Name, x.BatchCode })
            .Take(MaximumRows + 1).ToListAsync(token);

        var asOfUtc = clock.GetUtcNow().UtcDateTime;
        // Zero-result reads are audited too. Never persist the search term or name.
        db.AuditLogs.Add(new AuditLog
        {
            AcademyId = academyId,
            ActorUserId = actorId,
            Action = "PentaBatchSearchRead",
            EntityType = "Academy",
            EntityId = academyId,
            OccurredAtUtc = asOfUtc,
            MetadataJson = "{\"tool\":\"batch.search.v1\"}"
        });
        await db.SaveChangesAsync(token);

        var rows = matches.Take(MaximumRows)
            .Select(x => new PentaBatchSearchRow(x.Id, x.Name, x.BatchCode,
                "/batch-setup"))
            .ToArray();
        return new PentaBatchSearchResult(ToolName, "executor", academy.Id,
            asOfUtc, academy.TimeZone, "Batches", MaximumRows,
            matches.Count > MaximumRows, rows);
    }
}
