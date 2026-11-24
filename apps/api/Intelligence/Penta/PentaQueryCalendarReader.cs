using AcademyDesk.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Intelligence.Penta;

internal static class PentaQueryCalendarReader
{
    internal static async Task<PentaQueryDecision> ReadAsync(AcademyDeskDbContext db, TimeProvider clock, Guid academy,
        Dictionary<string,string> filters, Guid? selected, CancellationToken token) {
        var zone = await db.Academies.AsNoTracking().Where(x => x.Id == academy).Select(x => x.TimeZone).SingleOrDefaultAsync(token);
        if (!PentaQueryCalendar.Window(zone, filters, clock.GetUtcNow(), out var scope, out var fromUtc, out var toUtc))
            return new("CLARIFICATION_REQUIRED", "I could not verify that date range or academy time zone. Use an exact YYYY-MM-DD date or a range of at most 31 days. No classes were changed.", PentaQueryContext.Empty);
        var query = from session in db.ClassSessions.AsNoTracking()
            join batch in db.Batches.AsNoTracking() on session.BatchId equals batch.Id
            where session.AcademyId == academy && batch.AcademyId == academy &&
                session.StartUtc >= fromUtc && session.StartUtc < toUtc &&
                (session.TeacherId == null || db.Teachers.Any(t => t.Id == session.TeacherId && t.AcademyId == academy))
            select new { session, batch };
        if (selected is { } id) query = query.Where(x => x.session.Id == id);
        if (filters.TryGetValue("name", out var name)) query = query.Where(x => x.batch.Name.Contains(name) || x.batch.BatchCode != null && x.batch.BatchCode.Contains(name));
        if (filters.TryGetValue("teacher", out var teacher)) query = query.Where(x => db.Teachers.Any(t =>
            t.AcademyId == academy && t.Id == x.session.TeacherId && (t.FirstName + " " + t.LastName).Contains(teacher)));
        if (filters.TryGetValue("status", out var status)) query = query.Where(x => x.session.Status == status);
        var values = await query.OrderBy(x => x.session.StartUtc).ThenBy(x => x.session.Id).Take(11).Select(x => new {
            x.session.Id, x.batch.Name, x.batch.BatchCode, x.session.StartUtc, x.session.EndUtc, x.session.Status, x.session.DeliveryMode,
            Teacher = db.Teachers.Where(t => t.Id == x.session.TeacherId && t.AcademyId == academy).Select(t => t.FirstName + " " + t.LastName).FirstOrDefault()
        }).ToArrayAsync(token);
        if (selected is not null && values.Length != 1)
            return new("CLARIFICATION_REQUIRED", "That class is no longer available in the displayed date range. Search the calendar again.", PentaQueryContext.Empty);
        var rows = values.Take(10).Select(x => new PentaQueryRecord(x.Id, Clip(x.Name, 240),
            string.IsNullOrWhiteSpace(x.BatchCode) ? null : Clip(x.BatchCode,100), x.Status is "Scheduled" or "InProgress", [], "/schedule",
            Schedule: new(DateTime.SpecifyKind(x.StartUtc, DateTimeKind.Utc), DateTime.SpecifyKind(x.EndUtc, DateTimeKind.Utc), x.DeliveryMode, x.Status,
                string.IsNullOrWhiteSpace(x.Teacher) ? null : Clip(x.Teacher,240)))).ToArray();
        if (rows.Any(x => !PentaQueryCalendar.ValidSchedule(x.Schedule, scope)))
            return new("ERROR", "Some stored class details could not be verified. Open Manual Schedule to review them. No classes were changed.", PentaQueryContext.Empty);
        // Pin relative dates as exact local days before retaining context: a
        // later ordinal/recovery cannot drift when midnight passes.
        var resolved = new Dictionary<string,string>(filters) { ["from"] = scope!.From, ["through"] = scope.Through };
        var context = new PentaQueryContext("session", resolved, rows.Select(x => x.Id).ToArray());
        var prefix = rows.Length == 0 ? "No matching classes start" : values.Length > 10
            ? "Here are the first 10 matching classes; more exist. Refine the batch, teacher or date. These classes start" : "These verified classes start";
        return new("RESULT", $"{prefix} from {scope.From} through {scope.Through} in {scope.TimeZone}.", context,
            new("session", rows, values.Length > 10, clock.GetUtcNow().UtcDateTime, CalendarScope: scope));
    }
    private static string Clip(string value, int max) => value.Length <= max ? value : value[..max];
}
