using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Domain.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/teacher")]
public sealed class TeacherPortalController(
    AcademyDeskDbContext dbContext,
    UserManager<ApplicationUser> userManager) : ControllerBase
{
    private static readonly string[] AttendanceStatuses = ["Present", "Absent", "Late", "Excused", "Online"];

    [HttpGet("me")]
    public async Task<ActionResult<TeacherPortalSummary>> Me(CancellationToken cancellationToken)
    {
        var user = await userManager.GetUserAsync(User);
        if (user?.AcademyId is null || user.TeacherId is null) return Forbid();

        var teacher = await dbContext.Teachers.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == user.TeacherId && x.AcademyId == user.AcademyId && x.IsActive, cancellationToken);
        if (teacher is null) return Forbid();

        var batches = await dbContext.Batches.AsNoTracking()
            .Where(x => x.AcademyId == user.AcademyId && x.TeacherId == user.TeacherId && x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new TeacherBatchSummary(x.Id, x.Name, x.Capacity))
            .ToListAsync(cancellationToken);
        var batchIds = batches.Select(x => x.Id).ToArray();
        var sessions = await dbContext.ClassSessions.AsNoTracking()
            .Where(x => x.AcademyId == user.AcademyId && batchIds.Contains(x.BatchId) && x.StartUtc >= DateTime.UtcNow.AddDays(-1))
            .OrderBy(x => x.StartUtc)
            .Take(30)
            .Select(x => new TeacherSessionSummary(x.Id, x.BatchId, x.StartUtc, x.EndUtc, x.DeliveryMode, x.RoomName, x.Status))
            .ToListAsync(cancellationToken);

        return Ok(new TeacherPortalSummary(teacher.FirstName, teacher.LastName, batches, sessions));
    }

    [HttpPut("profile")]
    public async Task<ActionResult> UpdateProfile(TeacherPortalProfileRequest request, CancellationToken cancellationToken)
    {
        var user = await userManager.GetUserAsync(User);
        if (user?.AcademyId is null || user.TeacherId is null) return Forbid();
        var teacher = await dbContext.Teachers.SingleOrDefaultAsync(x =>
            x.Id == user.TeacherId && x.AcademyId == user.AcademyId && x.IsActive, cancellationToken);
        if (teacher is null) return Forbid();
        teacher.Email = request.Email?.Trim();
        teacher.Phone = request.Phone?.Trim();
        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(new { teacher.Id, teacher.Email, teacher.Phone });
    }

    [HttpGet("sessions/{sessionId:guid}/roster")]
    public async Task<ActionResult<IReadOnlyList<TeacherRosterStudent>>> Roster(Guid sessionId, CancellationToken cancellationToken)
    {
        var context = await GetTeacherContext(sessionId, cancellationToken);
        if (context is null) return Forbid();

        var students = await dbContext.Enrollments.AsNoTracking()
            .Where(x => x.AcademyId == context.AcademyId && x.BatchId == context.Session.BatchId && x.Status == "Active")
            .Join(dbContext.Students.AsNoTracking(), enrollment => enrollment.StudentId, student => student.Id,
                (enrollment, student) => new TeacherRosterStudent(student.Id, student.FirstName, student.LastName))
            .OrderBy(x => x.LastName).ThenBy(x => x.FirstName)
            .ToListAsync(cancellationToken);
        return Ok(students);
    }

    [HttpPost("sessions/{sessionId:guid}/attendance")]
    public async Task<ActionResult<TeacherAttendanceSummary>> MarkAttendance(
        Guid sessionId,
        TeacherMarkAttendanceRequest request,
        CancellationToken cancellationToken)
    {
        var context = await GetTeacherContext(sessionId, cancellationToken);
        if (context is null) return Forbid();
        if (!AttendanceStatuses.Contains(request.Status, StringComparer.OrdinalIgnoreCase))
            return BadRequest(new { message = "Invalid attendance status." });

        var enrolled = await dbContext.Enrollments.AnyAsync(x =>
            x.AcademyId == context.AcademyId && x.BatchId == context.Session.BatchId &&
            x.StudentId == request.StudentId && x.Status == "Active", cancellationToken);
        if (!enrolled) return NotFound();

        var record = await dbContext.AttendanceRecords.SingleOrDefaultAsync(x =>
            x.AcademyId == context.AcademyId && x.ClassSessionId == sessionId && x.StudentId == request.StudentId, cancellationToken);
        if (record is null)
        {
            record = new AttendanceRecord
            {
                AcademyId = context.AcademyId,
                ClassSessionId = sessionId,
                StudentId = request.StudentId
            };
            dbContext.AttendanceRecords.Add(record);
        }
        record.Status = request.Status.Trim();
        record.Notes = request.Notes?.Trim();
        record.MarkedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(new TeacherAttendanceSummary(record.StudentId, record.Status, record.Notes));
    }

    [HttpGet("sessions/{sessionId:guid}/attendance")]
    public async Task<ActionResult<IReadOnlyList<TeacherAttendanceSummary>>> Attendance(Guid sessionId, CancellationToken cancellationToken)
    {
        var context = await GetTeacherContext(sessionId, cancellationToken);
        if (context is null) return Forbid();

        var records = await dbContext.AttendanceRecords.AsNoTracking()
            .Where(x => x.AcademyId == context.AcademyId && x.ClassSessionId == sessionId)
            .Select(x => new TeacherAttendanceSummary(x.StudentId, x.Status, x.Notes))
            .ToListAsync(cancellationToken);
        return Ok(records);
    }

    private async Task<TeacherContext?> GetTeacherContext(Guid sessionId, CancellationToken cancellationToken)
    {
        var user = await userManager.GetUserAsync(User);
        if (user?.AcademyId is null || user.TeacherId is null) return null;
        var session = await dbContext.ClassSessions.AsNoTracking().SingleOrDefaultAsync(x =>
            x.Id == sessionId && x.AcademyId == user.AcademyId && x.TeacherId == user.TeacherId, cancellationToken);
        return session is null ? null : new TeacherContext(user.AcademyId.Value, session);
    }

    private sealed record TeacherContext(Guid AcademyId, ClassSession Session);
}

public sealed record TeacherPortalSummary(string FirstName, string LastName, IReadOnlyList<TeacherBatchSummary> Batches, IReadOnlyList<TeacherSessionSummary> Sessions);
public sealed record TeacherBatchSummary(Guid Id, string Name, int Capacity);
public sealed record TeacherSessionSummary(Guid Id, Guid BatchId, DateTime StartUtc, DateTime EndUtc, string DeliveryMode, string? RoomName, string Status);
public sealed record TeacherRosterStudent(Guid Id, string FirstName, string LastName);
public sealed record TeacherMarkAttendanceRequest(Guid StudentId, string Status, string? Notes);
public sealed record TeacherAttendanceSummary(Guid StudentId, string Status, string? Notes);
public sealed record TeacherPortalProfileRequest(string? Email, string? Phone);
