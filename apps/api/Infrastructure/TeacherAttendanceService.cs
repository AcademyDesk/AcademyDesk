using System.Data;
using System.Security.Claims;
using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Domain.Identity;
using AcademyDesk.Api.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Infrastructure;

internal sealed record TeacherAttendanceOutcome<T>(int Status, T? Value = default, string? Error = null);
internal sealed record TeacherAttendanceActor(Guid UserId, Guid AcademyId, Guid TeacherId, string Stamp);
public sealed record TeacherAttendanceChange(Guid StudentId, string Status, string? Notes, string InAppMessage);
internal sealed record TeacherAttendancePlan(TeacherAttendanceChange[] Changes, AttendanceSummary[] Previous);

public sealed class TeacherAttendanceService(AcademyDeskDbContext db, IdentityDbContext identity,
    UserManager<ApplicationUser> users, ClassSessionSchedulingService scheduling, TimeProvider clock)
{
    private static readonly string[] Statuses = ["Present", "Absent"];
    private const string NoticeTitle = "Attendance updated";

    internal async Task<TeacherAttendanceOutcome<T>> InAssignedSessionAsync<T>(ClaimsPrincipal principal, Guid sessionId,
        Func<TeacherAttendanceActor, ClassSession, Task<TeacherAttendanceOutcome<T>>> operation, CancellationToken token)
    {
        var initial = await users.GetUserAsync(principal);
        if (initial is not { IsActive: true, IsPlatformOwner: false, AcademyId: { } academy, TeacherId: not null })
            return new(403);
        var actorId = initial.Id;
        var stamp = principal.FindFirstValue(users.Options.ClaimsIdentity.SecurityStampClaimType);
        var connection = db.Database.IsRelational() ? AcademyIdentityTransaction.Validate(db, identity) : null;
        identity.ChangeTracker.Clear();
        await using var transaction = connection is null ? null : await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, token);
        await scheduling.LockAsync(academy, token);
        await using var linked = connection is null ? null : await AcademyIdentityTransaction.EnlistAsync(db, identity, transaction!, connection, token);

        var actor = await identity.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == actorId, token);
        if (actor is not { IsActive: true, IsPlatformOwner: false, TeacherId: { } teacher } || actor.AcademyId != academy ||
            string.IsNullOrEmpty(stamp) || actor.SecurityStamp != stamp || !await users.IsInRoleAsync(actor, "Teacher") ||
            !await db.Academies.AnyAsync(x => x.Id == academy && x.IsActive, token) ||
            !await db.Teachers.AnyAsync(x => x.Id == teacher && x.AcademyId == academy && x.IsActive, token))
            return new(403);

        var session = await db.ClassSessions.AsNoTracking().SingleOrDefaultAsync(x =>
            x.Id == sessionId && x.AcademyId == academy && x.TeacherId == teacher, token);
        if (session is null) return new(403);
        var result = await operation(new(actorId, academy, teacher, stamp), session);
        if (result.Status < 300 && transaction is not null) await transaction.CommitAsync(token);
        return result;
    }

    internal Task<TeacherAttendanceOutcome<TeacherAttendanceChange[]>> MarkAsync(ClaimsPrincipal principal, Guid sessionId,
        IReadOnlyList<TeacherBulkAttendanceItem> records, bool bulk, CancellationToken token) =>
        InAssignedSessionAsync<TeacherAttendanceChange[]>(principal, sessionId, async (actor, session) =>
        {
            var planned = await PlanAsync(session, records, bulk, token);
            if (planned.Value is null) return new(planned.Status, Error: planned.Error);
            await SaveAsync(actor, session, planned.Value, "TeacherAttendanceMarked", token);
            return new(200, planned.Value.Changes);
        }, token);

    internal Task<TeacherAttendanceOutcome<TeacherAttendanceSummary[]>> ReadAsync(ClaimsPrincipal principal, Guid sessionId, CancellationToken token) =>
        InAssignedSessionAsync<TeacherAttendanceSummary[]>(principal, sessionId, async (actor, session) =>
        {
            var records = await db.AttendanceRecords.AsNoTracking()
                .Where(x => x.AcademyId == actor.AcademyId && x.ClassSessionId == session.Id)
                .Select(x => new TeacherAttendanceSummary(x.StudentId, x.Status, x.Notes)).ToArrayAsync(token);
            return new(200, records);
        }, token);

    internal async Task<TeacherAttendanceOutcome<TeacherAttendancePlan>> PlanAsync(ClassSession session,
        IReadOnlyList<TeacherBulkAttendanceItem> records, bool bulk, CancellationToken token)
    {
        if (records.Count == 0) return new(400, Error: "At least one attendance record is required.");
        if (records.Any(x => x is null || !Statuses.Contains(x.Status, StringComparer.OrdinalIgnoreCase)))
            return new(400, Error: bulk ? "One or more attendance statuses are invalid." : "Invalid attendance status.");
        var ids = records.Select(x => x.StudentId).Distinct().ToArray();
        if (ids.Length != records.Count) return new(400, Error: "Each student may appear only once.");
        if (records.Any(x => x.Notes?.Trim().Length > 500)) return new(400, Error: "Attendance notes may contain up to 500 characters.");

        var enrolled = await (from enrollment in db.Enrollments
            join student in db.Students on enrollment.StudentId equals student.Id
            where enrollment.AcademyId == session.AcademyId && student.AcademyId == session.AcademyId &&
                enrollment.BatchId == session.BatchId && enrollment.Status == "Active" && ids.Contains(student.Id)
            select student.Id).Distinct().CountAsync(token);
        if (enrolled != ids.Length)
            return bulk ? new(400, Error: "Every student must be actively enrolled in this batch.") : new(404);

        var previous = await db.AttendanceRecords.AsNoTracking()
            .Where(x => x.AcademyId == session.AcademyId && x.ClassSessionId == session.Id && ids.Contains(x.StudentId))
            .OrderBy(x => x.StudentId).Select(x => new AttendanceSummary(x.Id, x.StudentId, x.Status, x.MarkedAtUtc, x.Notes)).ToArrayAsync(token);
        var changes = records.Select(x => new TeacherAttendanceChange(x.StudentId, x.Status.Trim(), x.Notes?.Trim(),
            $"Your attendance for the class has been marked {x.Status.Trim()}.")).ToArray();
        return new(200, new(changes, previous));
    }

    internal async Task<(Guid AuditId, Guid[] RecordIds, DateTime MarkedAtUtc)> SaveAsync(TeacherAttendanceActor actor,
        ClassSession session, TeacherAttendancePlan plan, string auditAction, CancellationToken token)
    {
        var ids = plan.Changes.Select(x => x.StudentId).ToArray();
        var existing = await db.AttendanceRecords.Where(x => x.AcademyId == actor.AcademyId && x.ClassSessionId == session.Id &&
            ids.Contains(x.StudentId)).ToDictionaryAsync(x => x.StudentId, token);
        var now = clock.GetUtcNow().UtcDateTime;
        var recordIds = new Guid[ids.Length];
        for (var index = 0; index < plan.Changes.Length; index++)
        {
            var change = plan.Changes[index];
            if (!existing.TryGetValue(change.StudentId, out var record))
            {
                record = new AttendanceRecord { AcademyId = actor.AcademyId, ClassSessionId = session.Id, StudentId = change.StudentId };
                db.AttendanceRecords.Add(record);
            }
            record.Status = change.Status;
            record.Notes = change.Notes;
            record.MarkedAtUtc = now;
            recordIds[index] = record.Id;
            db.Notifications.Add(new Notification { AcademyId = actor.AcademyId, RecipientId = change.StudentId, RecipientType = "Student",
                Title = NoticeTitle, Message = change.InAppMessage, Channel = "InApp", Status = "Queued" });
        }

        var audit = new AuditLog { AcademyId = actor.AcademyId, ActorUserId = actor.UserId, EntityId = session.Id,
            EntityType = "ClassSession", Action = auditAction, OccurredAtUtc = now };
        db.AuditLogs.Add(audit);
        await db.SaveChangesAsync(token);
        return (audit.Id, recordIds, now);
    }
}
