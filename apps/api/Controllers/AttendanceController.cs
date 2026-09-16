using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Controllers;

[ApiController]
[Route("api/academies/{academyId:guid}/sessions/{sessionId:guid}/attendance")]
public sealed class AttendanceController(AcademyDeskDbContext dbContext) : ControllerBase
{
    private static readonly string[] AllowedStatuses = ["Present", "Absent", "Late", "Excused", "Online"];

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AttendanceSummary>>> List(Guid academyId, Guid sessionId, CancellationToken cancellationToken)
    {
        var records = await dbContext.AttendanceRecords.AsNoTracking().Where(x => x.AcademyId == academyId && x.ClassSessionId == sessionId).OrderBy(x => x.StudentId).Select(x => new AttendanceSummary(x.Id, x.StudentId, x.Status, x.MarkedAtUtc, x.Notes)).ToListAsync(cancellationToken);
        return Ok(records);
    }

    [HttpPost]
    public async Task<ActionResult<AttendanceSummary>> Mark(Guid academyId, Guid sessionId, MarkAttendanceRequest request, CancellationToken cancellationToken)
    {
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
