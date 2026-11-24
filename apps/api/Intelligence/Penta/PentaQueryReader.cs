using AcademyDesk.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Intelligence.Penta;

/// <summary>Typed, tenant-scoped projection over existing domain tables. No
/// credentials/contact details, financial calculations, writes or model SQL.</summary>
public sealed class PentaQueryReader(AcademyDeskDbContext db, TimeProvider clock)
{
    internal async Task<bool> CurrentAsync(Guid academy, PentaQueryContext context, CancellationToken token)
    {
        if (!PentaQueryContract.ValidContext(context)) return false;
        var ids = context.ResultIds;
        if (ids.Length == 0) return true;
        var count = context.Entity switch {
            "teacher" => await db.Teachers.CountAsync(x => x.AcademyId == academy && ids.Contains(x.Id), token),
            "student" => await db.Students.CountAsync(x => x.AcademyId == academy && ids.Contains(x.Id), token),
            "batch" => await db.Batches.CountAsync(x => x.AcademyId == academy && ids.Contains(x.Id), token), _ => -1 };
        return count == ids.Length;
    }

    internal async Task<PentaQueryDecision> ExecuteAsync(Guid academy, PentaQueryPlan? plan,
        PentaQueryContext previous, IReadOnlySet<string> permitted, CancellationToken token)
    {
        if (!PentaQueryContract.ValidPlan(plan)) return new("ERROR", "I could not verify the intended operation. No records were searched.", previous);
        if (plan!.Kind is "clarify" or "unsupported") return new(plan.Kind == "clarify" ? "CLARIFICATION_REQUIRED" : "UNSUPPORTED",
            plan.Kind == "clarify" ? "Which student, teacher or batch do you mean? Tell me the name or choose a displayed result."
                : "That operation is not connected to this read conversation. No records were searched or changed. Your guided workflows and Manual workspace remain available.",
            // Unsupported/topic-switch turns must not silently retain a different entity's IDs.
            plan.Kind == "clarify" && plan.Entity == previous.Entity ? previous : PentaQueryContext.Empty);
        if (!permitted.Contains(PentaQueryContract.Tool(plan.Entity!, plan.Kind)))
            return new("UNSUPPORTED", "That read is not permitted in your current workspace. No other tool was substituted.", PentaQueryContext.Empty);
        Guid? selected = null;
        var filters = plan.Filters;
        if (plan.Kind == "open") {
            if (plan.Entity != previous.Entity || plan.Position > previous.ResultIds.Length)
                return new("CLARIFICATION_REQUIRED", "Search in that domain first, then choose a displayed result.", PentaQueryContext.Empty);
            selected = previous.ResultIds[plan.Position!.Value - 1];
            filters = previous.Filters;
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
