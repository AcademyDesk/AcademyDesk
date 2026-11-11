using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Infrastructure;
using AcademyDesk.Api.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Controllers;

[ApiController]
[Route("api/academies/{academyId:guid}/sessions/{sessionId:guid}/attendance")]
public sealed class AttendanceController(AcademyDeskDbContext dbContext) : ControllerBase
{
    private static readonly string[] AllowedStatuses = ["Present", "Absent", "Late", "Excused", "Online"];

    // Attendance management needs a roster and session labels, not full student,
    // batch or scheduling access. Keep this controller's existing academy gate.
    [HttpGet("/api/academies/{academyId:guid}/attendance/options")]
    public async Task<ActionResult<AttendanceOptionsSummary>> Options(Guid academyId, CancellationToken token)
    {
        Response.Headers.CacheControl = "no-store";
        var active = dbContext.Enrollments.AsNoTracking().Where(x => x.AcademyId == academyId && x.Status == "Active"
            && dbContext.Students.Any(s => s.Id == x.StudentId && s.AcademyId == academyId)
            && dbContext.Batches.Any(b => b.Id == x.BatchId && b.AcademyId == academyId));
        var students = await dbContext.Students.AsNoTracking().Where(x => x.AcademyId == academyId && active.Any(e => e.StudentId == x.Id))
            .OrderBy(x => x.LastName).ThenBy(x => x.FirstName).ThenBy(x => x.Id)
            .Select(x => new AttendanceStudentOption(x.Id, x.FirstName, x.LastName)).ToListAsync(token);
        var batches = await dbContext.Batches.AsNoTracking().Where(x => x.AcademyId == academyId)
            .OrderBy(x => x.Name).ThenBy(x => x.Id).Select(x => new AttendanceBatchOption(x.Id, x.Name)).ToListAsync(token);
        var enrollments = await active.Select(x => new { x.StudentId, x.BatchId, x.Status }).Distinct()
            .OrderBy(x => x.BatchId).ThenBy(x => x.StudentId)
            .Select(x => new AttendanceEnrollmentOption(x.StudentId, x.BatchId, x.Status)).ToListAsync(token);
        var sessions = await dbContext.ClassSessions.AsNoTracking().Where(x => x.AcademyId == academyId
            && dbContext.Batches.Any(b => b.Id == x.BatchId && b.AcademyId == academyId))
            .OrderBy(x => x.StartUtc).ThenBy(x => x.Id)
            .Select(x => new AttendanceSessionOption(x.Id, x.BatchId, x.StartUtc)).ToListAsync(token);
        return Ok(new AttendanceOptionsSummary(students, batches, enrollments, sessions));
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AttendanceSummary>>> List(Guid academyId, Guid sessionId, CancellationToken cancellationToken)
    {
        var records = await dbContext.AttendanceRecords.AsNoTracking().Where(x => x.AcademyId == academyId && x.ClassSessionId == sessionId).OrderBy(x => x.StudentId).Select(x => new AttendanceSummary(x.Id, x.StudentId, x.Status, x.MarkedAtUtc, x.Notes)).ToListAsync(cancellationToken);
        return Ok(records);
    }

    [HttpPost]
    [AtomicAcademyMutation(IncludePlatformOwner = true)]
    public async Task<ActionResult<AttendanceSummary>> Mark(Guid academyId, Guid sessionId, MarkAttendanceRequest request, CancellationToken cancellationToken)
    {
        await new ClassSessionSchedulingService(dbContext).LockAsync(academyId, cancellationToken);
        var session = await dbContext.ClassSessions.AsNoTracking().SingleOrDefaultAsync(x => x.Id == sessionId && x.AcademyId == academyId, cancellationToken);
        if (session is null) return NotFound();
        if (!AllowedStatuses.Contains(request.Status, StringComparer.OrdinalIgnoreCase)) return BadRequest(new { message = "Status must be Present, Absent, Late, Excused, or Online." });
        var enrolled = await dbContext.Enrollments.AnyAsync(x => x.AcademyId == academyId && x.StudentId == request.StudentId && x.BatchId == session.BatchId && x.Status == "Active", cancellationToken);
        if (!enrolled) return BadRequest(new { message = "The student is not actively enrolled in this session's batch." });

        var record = await dbContext.AttendanceRecords.SingleOrDefaultAsync(x => x.AcademyId == academyId && x.ClassSessionId == sessionId && x.StudentId == request.StudentId, cancellationToken);
        if (record is null) { record = new AttendanceRecord { AcademyId = academyId, ClassSessionId = sessionId, StudentId = request.StudentId }; dbContext.AttendanceRecords.Add(record); }
        record.Status = request.Status.Trim(); record.Notes = request.Notes?.Trim(); record.MarkedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(new AttendanceSummary(record.Id, record.StudentId, record.Status, record.MarkedAtUtc, record.Notes));
    }
}

public sealed record MarkAttendanceRequest(Guid StudentId, string Status, string? Notes);
public sealed record AttendanceSummary(Guid Id, Guid StudentId, string Status, DateTime MarkedAtUtc, string? Notes);
public sealed record AttendanceStudentOption(Guid Id, string FirstName, string LastName);
public sealed record AttendanceBatchOption(Guid Id, string Name);
public sealed record AttendanceEnrollmentOption(Guid StudentId, Guid BatchId, string Status);
public sealed record AttendanceSessionOption(Guid Id, Guid BatchId, DateTime StartUtc);
public sealed record AttendanceOptionsSummary(IReadOnlyList<AttendanceStudentOption> Students, IReadOnlyList<AttendanceBatchOption> Batches, IReadOnlyList<AttendanceEnrollmentOption> Enrollments, IReadOnlyList<AttendanceSessionOption> Sessions);
