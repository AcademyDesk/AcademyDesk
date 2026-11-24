using AcademyDesk.Api.Data;
using AcademyDesk.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Intelligence.Penta;

/// <summary>Typed, tenant-scoped projection over existing domain tables. No
/// credentials/contact details, financial calculations, writes or model SQL.</summary>
public sealed class PentaQueryReader(AcademyDeskDbContext db, TimeProvider clock, OutstandingFeesService? fees = null)
{
    internal async Task<bool> CurrentAsync(Guid academy, PentaQueryContext context, CancellationToken token)
    {
        if (!PentaQueryContract.ValidContext(context)) return false;
        var ids = context.ResultIds;
        if (ids.Length == 0) return true;
        var count = context.Entity switch {
            "attendance" => await db.AttendanceRecords.CountAsync(x=>x.AcademyId==academy && ids.Contains(x.Id) &&
                db.Students.Any(s=>s.Id==x.StudentId && s.AcademyId==academy) &&
                db.ClassSessions.Any(c=>c.Id==x.ClassSessionId && c.AcademyId==academy &&
                    db.Batches.Any(b=>b.Id==c.BatchId && b.AcademyId==academy) &&
                    (c.TeacherId==null || db.Teachers.Any(t=>t.Id==c.TeacherId && t.AcademyId==academy))),token),
            "teacher" => await db.Teachers.CountAsync(x => x.AcademyId == academy && ids.Contains(x.Id), token),
            "student" => await db.Students.CountAsync(x => x.AcademyId == academy && ids.Contains(x.Id), token),
            "student-fees" => await db.Students.CountAsync(x => x.AcademyId == academy && x.IsActive && ids.Contains(x.Id), token),
            "batch" => await db.Batches.CountAsync(x => x.AcademyId == academy && ids.Contains(x.Id), token),
            "session" => await db.ClassSessions.CountAsync(x => x.AcademyId == academy && ids.Contains(x.Id) &&
                db.Batches.Any(b => b.Id == x.BatchId && b.AcademyId == academy) &&
                (x.TeacherId == null || db.Teachers.Any(t => t.Id == x.TeacherId && t.AcademyId == academy)), token), _ => -1 };
        return count == ids.Length;
    }

    internal async Task<PentaQueryDecision> ExecuteAsync(Guid academy, PentaQueryPlan? plan,
        PentaQueryContext previous, IReadOnlySet<string> permitted, CancellationToken token)
    {
        if (!PentaQueryContract.ValidPlan(plan)) return new("ERROR", "I could not verify the intended operation. No records were searched.", previous);
        if (plan!.Kind is "clarify" or "unsupported") return new(plan.Kind == "clarify" ? "CLARIFICATION_REQUIRED" : "UNSUPPORTED",
            plan.Kind == "clarify" ? "Which student, teacher, batch or class do you mean? Give a name, an exact date, or choose a displayed result."
                : "That operation is not connected to this read conversation. No records were searched or changed. Your guided workflows and Manual workspace remain available.",
            // Unsupported/topic-switch turns must not silently retain a different entity's IDs.
            plan.Kind == "clarify" && plan.Entity == previous.Entity ? previous : PentaQueryContext.Empty);
        if (!permitted.Contains(PentaQueryContract.Tool(plan.Entity!, plan.Kind)))
            return new("UNSUPPORTED", "That read is not permitted in your current workspace. No other tool was substituted.", PentaQueryContext.Empty);
        Guid? selected = null;
        var filters = plan.Filters;
        if (plan.Kind == "open") {
            if (!PentaQueryContract.SameReferences(plan.Entity, previous.Entity) || plan.Position > previous.ResultIds.Length)
                return new("CLARIFICATION_REQUIRED", "Search in that domain first, then choose a displayed result.", PentaQueryContext.Empty);
            selected = previous.ResultIds[plan.Position!.Value - 1];
            filters = previous.Filters.Where(pair => PentaQueryContract.Fields(plan.Entity!).Contains(pair.Key)).ToDictionary(pair => pair.Key, pair => pair.Value);
        }
        if (plan.Entity == "attendance") return await PentaQueryAttendanceReader.ReadAsync(db,clock,academy,filters,selected,token);
        if (plan.Entity == "session") return await PentaQueryCalendarReader.ReadAsync(db, clock, academy, filters, selected, token);
        if (plan.Entity == "student-fees") {
            // The Finance module is required by the current permitted tool set.
            // QueryConversation reauthorizes under the shared Identity/SQL lock.
            var snapshot = await (fees ?? new OutstandingFeesService(db, clock)).ReadAsync(academy, filters, null, selected, token);
            var money = PentaMiniMoney.FromResult(snapshot);
            if (money is null) return new("ERROR", "The stored balance precision or currency could not be verified. Use Manual Finance; no amounts were changed.", previous);
            if (selected is not null && snapshot.Rows.Length != 1)
                return new("CLARIFICATION_REQUIRED", "That student is no longer available for this fee read. Search again.", PentaQueryContext.Empty);
            var records = snapshot.Rows.Select((row, index) => new PentaQueryRecord(row.SourceId, Clip(row.DisplayName, 240),
                string.IsNullOrWhiteSpace(row.RecordCode) ? null : Clip(row.RecordCode, 100), true,
                row.Subjects.Take(5).Select(x => Clip(x, 200)).ToArray(), PentaQueryContract.Path("student-fees", row.SourceId), money.Rows[index].Balances)).ToArray();
            var feeContext = new PentaQueryContext("student-fees", new(filters), records.Select(x => x.Id).ToArray());
            var feeMessage = records.Length == 0 ? "No students match this fee search in your academy."
                : snapshot.HasMore ? "Here are the first 10 matching students' verified balances, separately by currency. More matches exist; refine the student name or subject."
                : "Here are the verified outstanding balances, shown separately for each currency.";
            return new("RESULT", feeMessage, feeContext, new("student-fees", records, snapshot.HasMore, snapshot.AsOfUtc, PentaQueryContract.BalanceScope));
        }
        filters.TryGetValue("name", out var name); filters.TryGetValue("status", out var status);
        filters.TryGetValue("subject", out var subject);
        PentaQueryRecord[] rows;
        if (plan.Entity == "teacher") {
            var query = db.Teachers.AsNoTracking().Where(x => x.AcademyId == academy);
            if (selected is { } id) query = query.Where(x => x.Id == id);
            if (name is not null) query = query.Where(x => (x.FirstName + " " + x.LastName).Contains(name) || x.EmployeeCode != null && x.EmployeeCode.Contains(name));
            if (subject is not null) query = query.Where(x => x.Specialties != null && x.Specialties.Contains(subject));
            if (status is not null) query = query.Where(x => x.IsActive == (status == "Active"));
            var values = await query.OrderBy(x => x.LastName).ThenBy(x => x.FirstName).ThenBy(x => x.Id).Take(11)
                .Select(x => new { x.Id, x.FirstName, x.LastName, x.EmployeeCode, x.IsActive, x.Specialties }).ToArrayAsync(token);
            rows = values.Select(x => new PentaQueryRecord(x.Id, Clip(x.FirstName + " " + x.LastName, 240), string.IsNullOrWhiteSpace(x.EmployeeCode) ? null : Clip(x.EmployeeCode, 100), x.IsActive,
                string.IsNullOrWhiteSpace(x.Specialties) ? [] : [Clip(x.Specialties, 200)], PentaQueryContract.Path("teacher", x.Id))).ToArray();
        } else if (plan.Entity == "student") {
            var query = db.Students.AsNoTracking().Where(x => x.AcademyId == academy);
            if (selected is { } id) query = query.Where(x => x.Id == id);
            if (name is not null) query = query.Where(x => (x.FirstName + " " + x.LastName).Contains(name) || x.StudentNumber != null && x.StudentNumber.Contains(name));
            if (status is not null) query = query.Where(x => x.IsActive == (status == "Active"));
            var values = await query.OrderBy(x => x.LastName).ThenBy(x => x.FirstName).ThenBy(x => x.Id).Take(11)
                .Select(x => new { x.Id, x.FirstName, x.LastName, x.StudentNumber, x.IsActive }).ToArrayAsync(token);
            rows = values.Select(x => new PentaQueryRecord(x.Id, Clip(x.FirstName + " " + x.LastName, 240),
                string.IsNullOrWhiteSpace(x.StudentNumber) ? null : Clip(x.StudentNumber, 100), x.IsActive, [], PentaQueryContract.Path("student", x.Id))).ToArray();
        } else {
            var query = db.Batches.AsNoTracking().Where(x => x.AcademyId == academy);
            if (selected is { } id) query = query.Where(x => x.Id == id);
            if (name is not null) query = query.Where(x => x.Name.Contains(name) || x.BatchCode != null && x.BatchCode.Contains(name));
            if (status is not null) query = query.Where(x => x.IsActive == (status == "Active"));
            var values = await query.OrderBy(x => x.Name).ThenBy(x => x.Id).Take(11)
                .Select(x => new { x.Id, x.Name, x.BatchCode, x.IsActive }).ToArrayAsync(token);
            rows = values.Select(x => new PentaQueryRecord(x.Id, Clip(x.Name, 240), string.IsNullOrWhiteSpace(x.BatchCode) ? null : Clip(x.BatchCode, 100),
                x.IsActive, [], PentaQueryContract.Path("batch", x.Id))).ToArray();
        }
        if (selected is not null && rows.Length != 1)
            return new("CLARIFICATION_REQUIRED", "That displayed record has changed. Search again before selecting it.", PentaQueryContext.Empty);
        var shown = rows.Take(10).ToArray();
        var context = new PentaQueryContext(plan.Entity, new(filters, StringComparer.Ordinal), shown.Select(x => x.Id).ToArray());
        var plural = plan.Entity == "batch" ? "batches" : plan.Entity + "s";
        var message = shown.Length == 0 ? $"No {plural} match this search in your academy."
            : selected is not null ? $"Here is the {plan.Entity} you selected, from your academy's records."
            : rows.Length > 10 ? $"Here are the first 10 matching {plural}. More matches exist; give me a name or refine the search."
            : $"I found {shown.Length} matching {(shown.Length == 1 ? plan.Entity : plural)} in your academy.";
        return new("RESULT", message, context, new(plan.Entity!, shown, rows.Length > 10, clock.GetUtcNow().UtcDateTime));
    }
    private static string Clip(string value, int length) => value.Length > length ? value[..length] : value;
}
