using AcademyDesk.Api.Data;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace AcademyDesk.Api.Infrastructure;

public sealed record InvoiceLedger
{
    public Guid Id { get; init; }
    public Guid StudentId { get; init; }
    public decimal Total { get; init; }
    public decimal Adjusted { get; init; }
    public decimal Collected { get; init; }
    public required string Currency { get; init; }
    public required string Status { get; init; }
    public decimal Balance => Total - Adjusted - Collected;
}
public sealed record LearnerBalance(string Currency, decimal Outstanding);
public sealed record OutstandingLearner(Guid SourceId, string DisplayName, string[] Subjects,
    LearnerBalance[] Balances, string SourcePath, string? RecordCode = null);
public sealed record OutstandingResult(int Count, bool HasMore, OutstandingLearner[] Rows, DateTime AsOfUtc,
    string Source = "Invoices / Completed + Reconciled payments / Active enrollments",
    string BalanceScope = "All outstanding fees for each student; subject filters select students, not course-specific invoices.");
public sealed class OutstandingFeesException(string message) : Exception(message);

/// <summary>Shared deterministic ledger read. No model arithmetic or connector rules.</summary>
public sealed class OutstandingFeesService(AcademyDeskDbContext db, TimeProvider clock)
{
    public IQueryable<InvoiceLedger> Ledger(Guid academyId) => db.Invoices.AsNoTracking()
        .Where(i => i.AcademyId == academyId)
        .Select(i => new InvoiceLedger {
            Id = i.Id, StudentId = i.StudentId, Total = i.TotalAmount, Adjusted = i.AdjustedAmount,
            Collected = db.Payments.Where(p => p.AcademyId == academyId && p.InvoiceId == i.Id &&
                (p.Status == "Completed" || p.Status == "Reconciled")).Sum(p => (decimal?)p.Amount) ?? 0,
            Currency = i.Currency, Status = i.Status });

    public async Task<OutstandingResult> ReadAsync(Guid academyId, Dictionary<string, string> filters,
        string? sort, Guid? selected, CancellationToken token)
    {
        // Count, ordered page and balances are one consistent read even when this
        // shared service is called without the orchestrator's transaction.
        if (db.Database.IsSqlServer() && db.Database.CurrentTransaction is null)
        {
            await using var read = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, token);
            var result = await ReadCoreAsync(academyId, filters, sort, selected, token);
            await read.CommitAsync(token);
            return result;
        }
        return await ReadCoreAsync(academyId, filters, sort, selected, token);
    }
    private async Task<OutstandingResult> ReadCoreAsync(Guid academyId, Dictionary<string, string> filters,
        string? sort, Guid? selected, CancellationToken token)
    {
        var students = db.Students.AsNoTracking().Where(s => s.AcademyId == academyId && s.IsActive);
        if (selected is { } id) students = students.Where(s => s.Id == id);
        // The existing generic name argument can identify a learner by their academy
        // record code too. Codes are not assumed unique and never confer authority.
        if (filters.TryGetValue("name", out var name)) students = students.Where(s =>
            (s.FirstName + " " + s.LastName).Contains(name) || s.StudentNumber != null && s.StudentNumber.Contains(name));
        if (filters.TryGetValue("subject", out var subject)) students = students.Where(s =>
            (from e in db.Enrollments
             join b in db.Batches on e.BatchId equals b.Id
             join c in db.Courses on b.CourseId equals c.Id
             where e.AcademyId == academyId && b.AcademyId == academyId && c.AcademyId == academyId &&
                e.StudentId == s.Id && e.Status == "Active" && b.IsActive && c.IsActive &&
                (c.Name.Contains(subject) || c.SubjectArea != null && c.SubjectArea.Contains(subject))
             select e.Id).Any());
        // Project again: EF cannot translate computed CLR Balance in SQL.
        var candidates = students;
        var ledger = Ledger(academyId).Where(i => i.Status != "Cancelled" && candidates.Any(s => s.Id == i.StudentId));
        if (await ledger.AnyAsync(i => i.Total < 0 || i.Adjusted < 0 || i.Adjusted > i.Total || i.Collected < 0 ||
            i.Collected > i.Total - i.Adjusted || i.Currency.Length != 3 ||
            !EF.Functions.Like(EF.Functions.Collate(i.Currency, "Latin1_General_100_BIN2"), "[A-Z][A-Z][A-Z]") ||
            db.Payments.Any(p => p.AcademyId == academyId && p.InvoiceId == i.Id &&
                (p.Status == "Completed" || p.Status == "Reconciled") && (p.Currency != i.Currency || p.Amount < 0)), token))
            throw new OutstandingFeesException("The source ledger needs review. No estimated balance is shown.");
        var grouped = ledger.GroupBy(i => new { i.StudentId, i.Currency }).Select(g => new
        { g.Key.StudentId, g.Key.Currency, Outstanding = g.Sum(i => i.Total - i.Adjusted - i.Collected) });
        var pending = filters.TryGetValue("balance_status", out var status) ? status : null;
        if (pending == "Pending") students = students.Where(s => grouped.Any(g => g.StudentId == s.Id && g.Outstanding > 0));
        if (pending == "Clear") students = students.Where(s => !grouped.Any(g => g.StudentId == s.Id && g.Outstanding > 0));
        // Never rank incomparable currencies or net a credit against another invoice.
        var matchingStudents = students;
        if (sort == "outstanding_desc" && await grouped.Where(g => matchingStudents.Any(s => s.Id == g.StudentId) && g.Outstanding > 0)
                .Select(g => g.Currency).Distinct().Take(2).CountAsync(token) > 1)
            throw new OutstandingFeesException("These results contain different currencies. Highest-first sorting is unavailable; use the manual finance workspace.");
        var ordered = sort == "outstanding_desc"
            ? students.OrderByDescending(s => grouped.Where(g => g.StudentId == s.Id).Sum(g => (decimal?)g.Outstanding) ?? 0).ThenBy(s => s.LastName).ThenBy(s => s.FirstName).ThenBy(s => s.Id)
            : students.OrderBy(s => s.LastName).ThenBy(s => s.FirstName).ThenBy(s => s.Id);
        var count = await students.CountAsync(token);
        var page = await ordered.Select(s => new { s.Id, s.FirstName, s.LastName, s.StudentNumber }).Take(10).ToArrayAsync(token);
        var ids = page.Select(s => s.Id).ToArray();
        var balances = await grouped.Where(g => ids.Contains(g.StudentId)).ToArrayAsync(token);
        var subjects = await (from e in db.Enrollments.AsNoTracking()
            join b in db.Batches on e.BatchId equals b.Id
            join c in db.Courses on b.CourseId equals c.Id
            where e.AcademyId == academyId && b.AcademyId == academyId && c.AcademyId == academyId &&
                ids.Contains(e.StudentId) && e.Status == "Active" && b.IsActive && c.IsActive
            select new { e.StudentId, c.Name }).Distinct().ToArrayAsync(token);
        return new(count, count > page.Length, page.Select(s => new OutstandingLearner(s.Id,
            $"{s.FirstName} {s.LastName}", subjects.Where(c => c.StudentId == s.Id).Select(c => c.Name).Order().Take(5).ToArray(),
            balances.Where(g => g.StudentId == s.Id).OrderBy(g => g.Currency).Select(g => new LearnerBalance(g.Currency, g.Outstanding)).ToArray(),
            $"/student-management?studentId={s.Id:D}", s.StudentNumber)).ToArray(), clock.GetUtcNow().UtcDateTime);
    }
}
