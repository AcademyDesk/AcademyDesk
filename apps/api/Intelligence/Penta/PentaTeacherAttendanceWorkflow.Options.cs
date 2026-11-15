using System.Security.Claims;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Intelligence.Penta;

public sealed record PentaTeacherAttendanceReadiness(string Protocol, Guid AcademyId, bool RequiresConfirmation);
public sealed record PentaTeacherAttendanceSession(Guid Id, Guid BatchId, string BatchName, DateTime StartUtc);
public sealed record PentaTeacherAttendanceLearner(Guid Id, string Name);
public sealed record PentaTeacherAttendanceOptions<T>(Guid AcademyId, Guid? SessionId, T[] Rows, bool HasMore);

public sealed partial class PentaTeacherAttendanceWorkflow
{
    private const int OptionLimit = 10;

    internal Task<TeacherAttendanceOutcome<PentaTeacherAttendanceReadiness>> ReadinessAsync(ClaimsPrincipal user, CancellationToken token) =>
        attendance.InTeacherScopeAsync<PentaTeacherAttendanceReadiness>(user, actor =>
            Task.FromResult(new TeacherAttendanceOutcome<PentaTeacherAttendanceReadiness>(200,
                new("teacher-attendance.v1", actor.AcademyId, true))), token);

    internal Task<TeacherAttendanceOutcome<PentaTeacherAttendanceOptions<PentaTeacherAttendanceSession>>> SessionsAsync(
        ClaimsPrincipal user, string? input, CancellationToken token) =>
        attendance.InTeacherScopeAsync<PentaTeacherAttendanceOptions<PentaTeacherAttendanceSession>>(user, async actor =>
        {
            if (!NormalizeOptionQuery(input, out var query)) return new(400, Error: "Enter 2 to 80 characters of a batch name.");
            var rows = await (from session in db.ClassSessions.AsNoTracking()
                join batch in db.Batches.AsNoTracking() on session.BatchId equals batch.Id
                where session.AcademyId == actor.AcademyId && batch.AcademyId == actor.AcademyId &&
                    session.TeacherId == actor.TeacherId && batch.Name.Contains(query)
                orderby session.StartUtc descending, session.Id
                select new PentaTeacherAttendanceSession(session.Id, batch.Id, batch.Name, session.StartUtc))
                .Take(OptionLimit + 1).ToArrayAsync(token);
            await AuditOptionAsync(actor, actor.AcademyId, "Academy", "PentaTeacherAttendanceSessionsSearched", token);
            return new(200, new(actor.AcademyId, null,
                rows.Take(OptionLimit).Select(row => row with { StartUtc = DateTime.SpecifyKind(row.StartUtc, DateTimeKind.Utc) }).ToArray(),
                rows.Length > OptionLimit));
        }, token);

    internal Task<TeacherAttendanceOutcome<PentaTeacherAttendanceOptions<PentaTeacherAttendanceLearner>>> RosterAsync(
        ClaimsPrincipal user, Guid sessionId, string? input, CancellationToken token) =>
        attendance.InAssignedSessionAsync<PentaTeacherAttendanceOptions<PentaTeacherAttendanceLearner>>(user, sessionId, async (actor, session) =>
        {
            if (!NormalizeOptionQuery(input, out var query)) return new(400, Error: "Enter 2 to 80 characters of a learner name.");
            var rows = await db.Students.AsNoTracking().Where(student => student.AcademyId == actor.AcademyId &&
                (student.FirstName + " " + student.LastName).Contains(query) && db.Enrollments.Any(enrollment =>
                    enrollment.AcademyId == actor.AcademyId && enrollment.BatchId == session.BatchId &&
                    enrollment.StudentId == student.Id && enrollment.Status == "Active"))
                .OrderBy(student => student.LastName).ThenBy(student => student.FirstName).ThenBy(student => student.Id)
                .Select(student => new PentaTeacherAttendanceLearner(student.Id, student.FirstName + " " + student.LastName))
                .Take(OptionLimit + 1).ToArrayAsync(token);
            await AuditOptionAsync(actor, session.Id, "ClassSession", "PentaTeacherAttendanceRosterSearched", token);
            return new(200, new(actor.AcademyId, session.Id, rows.Take(OptionLimit).ToArray(), rows.Length > OptionLimit));
        }, token);

    private async Task AuditOptionAsync(TeacherAttendanceActor actor, Guid entityId, string entityType, string action, CancellationToken token)
    {
        db.AuditLogs.Add(new AuditLog { AcademyId = actor.AcademyId, ActorUserId = actor.UserId,
            EntityId = entityId, EntityType = entityType, Action = action, OccurredAtUtc = Now });
        await db.SaveChangesAsync(token);
    }

    private static bool NormalizeOptionQuery(string? input, out string query)
    {
        query = input?.Trim() ?? "";
        return query.Length is >= 2 and <= 80 && !query.Any(char.IsControl);
    }
}
