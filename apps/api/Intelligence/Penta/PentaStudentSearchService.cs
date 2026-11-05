using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Intelligence.Penta;

public sealed record PentaStudentSearchRow(Guid SourceId, string DisplayName,
    bool IsActive, string SourcePath);

public sealed record PentaStudentSearchResult(string Tool, string Capability,
    Guid AcademyId, DateTime AsOfUtc, string TimeZone, string Source,
    int MaxRows, bool HasMore, IReadOnlyList<PentaStudentSearchRow> Rows);

/// <summary>Fixed R0 active-student search. No contact, finance, guardian or notes fields.</summary>
public sealed class PentaStudentSearchService(AcademyDeskDbContext db, TimeProvider clock)
{
    public const string ToolName = "student.search.v1";
    public const int MaximumRows = 10;

    public static bool TryNormalizeQuery(string? input, out string query)
    {
        query = input?.Trim() ?? string.Empty;
        return query.Length is >= 2 and <= 80 && !query.Any(char.IsControl);
    }

    public async Task<PentaStudentSearchResult?> ReadAsync(Guid academyId, Guid actorId,
        string query, CancellationToken token)
    {
        if (!TryNormalizeQuery(query, out var normalized) || normalized != query)
            throw new ArgumentException("A normalized search query is required.", nameof(query));

        var academy = await db.Academies.AsNoTracking()
            .Where(x => x.Id == academyId && x.IsActive)
            .Select(x => new { x.Id, x.TimeZone })
            .SingleOrDefaultAsync(token);
        if (academy is null) return null;

        // Filter and cap in SQL. Never materialize full student records or a full roster.
        var matches = await db.Students.AsNoTracking()
            .Where(x => x.AcademyId == academyId && x.IsActive &&
                (x.FirstName.Contains(normalized) || x.LastName.Contains(normalized) ||
                 (x.FirstName + " " + x.LastName).Contains(normalized) ||
                 x.StudentNumber != null && x.StudentNumber.Contains(normalized)))
            .OrderBy(x => x.LastName).ThenBy(x => x.FirstName).ThenBy(x => x.Id)
            .Select(x => new { x.Id, x.FirstName, x.LastName, x.IsActive })
            .Take(MaximumRows + 1).ToListAsync(token);

        var asOfUtc = clock.GetUtcNow().UtcDateTime;
        // Audit is mandatory, including zero-result searches; no result is returned
        // when the audit store fails. Persist no query or student name in metadata.
        db.AuditLogs.Add(new AuditLog
        {
            AcademyId = academyId,
            ActorUserId = actorId,
            Action = "PentaStudentSearchRead",
            EntityType = "Academy",
            EntityId = academyId,
            OccurredAtUtc = asOfUtc,
            MetadataJson = "{\"tool\":\"student.search.v1\"}"
        });
        await db.SaveChangesAsync(token);

        var rows = matches.Take(MaximumRows)
            .Select(x => new PentaStudentSearchRow(x.Id, $"{x.FirstName} {x.LastName}",
                x.IsActive, $"/student-management?studentId={x.Id:D}"))
            .ToArray();
        return new PentaStudentSearchResult(ToolName, "executor", academy.Id,
            asOfUtc, academy.TimeZone, "Students", MaximumRows,
            matches.Count > MaximumRows, rows);
    }
}
