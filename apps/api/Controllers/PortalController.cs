using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/portal")]
public sealed class PortalController(UserManager<ApplicationUser> users, AcademyDeskDbContext db) : ControllerBase
{
    [HttpGet("me")]
    public async Task<ActionResult> Me(CancellationToken token)
    {
        var user = await users.GetUserAsync(User);
        if (user?.AcademyId is null) return Forbid();
        if (user.StudentId.HasValue)
        {
            var student = await db.Students.AsNoTracking().SingleOrDefaultAsync(x => x.Id == user.StudentId && x.AcademyId == user.AcademyId, token);
            if (student is null) return Forbid();
            var enrollmentCount = await db.Enrollments.CountAsync(x => x.AcademyId == user.AcademyId && x.StudentId == student.Id && x.Status == "Active", token);
            var invoiceCount = await db.Invoices.CountAsync(x => x.AcademyId == user.AcademyId && x.StudentId == student.Id, token);
            return Ok(new { role = "Student", displayName = $"{student.FirstName} {student.LastName}", studentId = student.Id, enrollmentCount, invoiceCount });
        }
        if (user.GuardianId.HasValue)
        {
            var children = await db.StudentGuardians.AsNoTracking().Where(x => x.AcademyId == user.AcademyId && x.GuardianId == user.GuardianId).Join(db.Students.AsNoTracking(), x => x.StudentId, s => s.Id, (x, s) => new { s.Id, name = s.FirstName + " " + s.LastName }).ToListAsync(token);
            return Ok(new { role = "Guardian", displayName = user.DisplayName, children });
        }
        return Forbid();
    }

    [HttpPost("change-password")]
    public async Task<ActionResult> ChangePassword(PortalChangePasswordRequest request, CancellationToken token)
    {
        var user = await users.GetUserAsync(User);
        if (user?.AcademyId is null || (!user.StudentId.HasValue && !user.GuardianId.HasValue)) return Forbid();
        if (string.IsNullOrWhiteSpace(request.CurrentPassword) || string.IsNullOrWhiteSpace(request.NewPassword))
            return BadRequest(new { message = "Current and new passwords are required." });
        if (request.NewPassword.Length < 8)
            return BadRequest(new { message = "The new password must be at least 8 characters." });
        if (request.CurrentPassword == request.NewPassword)
            return BadRequest(new { message = "The new password must be different from the current password." });

        var result = await users.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
            return BadRequest(new { message = string.Join(" ", result.Errors.Select(x => x.Description)) });
        return Ok(new { message = "Password changed successfully." });
    }

    [HttpGet("students/{studentId:guid}")]
    public async Task<ActionResult<PortalStudentDetails>> Student(Guid studentId, CancellationToken token)
    {
        var user = await users.GetUserAsync(User);
        if (user?.AcademyId is null) return Forbid();
        var permitted = user.StudentId == studentId ||
            (user.GuardianId.HasValue && await db.StudentGuardians.AnyAsync(x => x.AcademyId == user.AcademyId && x.GuardianId == user.GuardianId && x.StudentId == studentId, token));
        if (!permitted) return Forbid();
        var student = await db.Students.AsNoTracking().SingleOrDefaultAsync(x => x.Id == studentId && x.AcademyId == user.AcademyId, token);
        if (student is null) return NotFound();
        var enrollments = await db.Enrollments.AsNoTracking().Where(x => x.AcademyId == user.AcademyId && x.StudentId == studentId && x.Status == "Active").ToListAsync(token);
        var batchIds = enrollments.Select(x => x.BatchId).ToArray();
        var batches = await db.Batches.AsNoTracking().Where(x => batchIds.Contains(x.Id)).Select(x => new PortalBatch(x.Id, x.Name)).ToListAsync(token);
        var assignments = await db.Assignments.AsNoTracking().Where(x => x.AcademyId == user.AcademyId && batchIds.Contains(x.BatchId) && x.IsPublished).OrderBy(x => x.DueAtUtc).Take(30).Select(x => new PortalAssignment(x.Id, x.Title, x.Type, x.DueAtUtc)).ToListAsync(token);
        var attendance = await db.AttendanceRecords.AsNoTracking().Where(x => x.AcademyId == user.AcademyId && x.StudentId == studentId).Join(db.ClassSessions.AsNoTracking(), a => a.ClassSessionId, s => s.Id, (a, s) => new PortalAttendance(s.StartUtc, a.Status)).OrderByDescending(x => x.StartUtc).Take(30).ToListAsync(token);
        var music = await db.StudentMusicProgress.AsNoTracking().Where(x => x.AcademyId == user.AcademyId && x.StudentId == studentId).Join(db.MusicPieces.AsNoTracking(), p => p.MusicPieceId, piece => piece.Id, (p, piece) => new PortalMusicProgress(piece.Title, p.Status, p.TargetDate)).OrderBy(x => x.TargetDate).ToListAsync(token);
        var resources = await db.LearningResources.AsNoTracking().Where(x => x.AcademyId == user.AcademyId && x.IsPublished && (!x.BatchId.HasValue || batchIds.Contains(x.BatchId.Value))).OrderByDescending(x => x.CreatedAtUtc).Take(30).Select(x => new PortalResource(x.Title, x.Type, x.Url)).ToListAsync(token);
        var practice = await db.PracticeLogs.AsNoTracking().Where(x => x.AcademyId == user.AcademyId && x.StudentId == studentId).OrderByDescending(x => x.PracticeDate).Take(20).Select(x => new PortalPracticeLog(x.PracticeDate, x.MinutesPracticed, x.FocusArea, x.TeacherFeedback, x.Status)).ToListAsync(token);
        var invoices = await db.Invoices.AsNoTracking().Where(x => x.AcademyId == user.AcademyId && x.StudentId == studentId).OrderByDescending(x => x.IssuedDate).ToListAsync(token);
        var invoiceIds = invoices.Select(x => x.Id).ToArray();
        var paid = await db.Payments.AsNoTracking().Where(x => invoiceIds.Contains(x.InvoiceId) && x.Status == "Completed").GroupBy(x => x.InvoiceId).Select(x => new { x.Key, Total = x.Sum(p => p.Amount) }).ToDictionaryAsync(x => x.Key, x => x.Total, token);
        return Ok(new PortalStudentDetails($"{student.FirstName} {student.LastName}", batches, assignments, attendance, music, resources, practice, invoices.Select(x => new PortalInvoice(x.InvoiceNumber, x.TotalAmount, x.TotalAmount - paid.GetValueOrDefault(x.Id), x.Currency, x.DueDate, x.Status)).ToList()));
    }

    [HttpPost("students/{studentId:guid}/assignments/{assignmentId:guid}/submit")]
    public async Task<ActionResult> SubmitAssignment(Guid studentId, Guid assignmentId, PortalSubmissionRequest request, CancellationToken token)
    {
        var user = await users.GetUserAsync(User); if (user?.AcademyId is null) return Forbid();
        var permitted = user.StudentId == studentId || (user.GuardianId.HasValue && await db.StudentGuardians.AnyAsync(x => x.AcademyId == user.AcademyId && x.GuardianId == user.GuardianId && x.StudentId == studentId, token)); if (!permitted) return Forbid();
        var assignment = await db.Assignments.SingleOrDefaultAsync(x => x.Id == assignmentId && x.AcademyId == user.AcademyId && x.IsPublished, token); if (assignment is null) return NotFound();
        var item = await db.AssignmentSubmissions.SingleOrDefaultAsync(x => x.AcademyId == user.AcademyId && x.AssignmentId == assignmentId && x.StudentId == studentId, token);
        if (item is null) { item = new AcademyDesk.Api.Domain.Entities.AssignmentSubmission { AcademyId = user.AcademyId.Value, AssignmentId = assignmentId, StudentId = studentId }; db.AssignmentSubmissions.Add(item); }
        item.ResponseText = request.ResponseText?.Trim(); item.Status = "Submitted"; item.SubmittedAtUtc = DateTime.UtcNow; item.TeacherFeedback = null; await db.SaveChangesAsync(token); return Ok(item);
    }

    [HttpGet("students/{studentId:guid}/leave-requests")]
    public async Task<ActionResult<IReadOnlyList<PortalLeaveSummary>>> StudentLeaveRequests(Guid studentId, CancellationToken token)
    {
        var user = await users.GetUserAsync(User);
        if (user?.AcademyId is null || !await CanAccessStudent(user, studentId, token)) return Forbid();
        var requests = await db.LeaveRequests.AsNoTracking()
            .Where(x => x.AcademyId == user.AcademyId && x.StudentId == studentId && x.RequesterType == "Student")
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new PortalLeaveSummary(x.Id, x.StartDate, x.EndDate, x.Reason, x.Status, x.DecisionNotes))
            .ToListAsync(token);
        return Ok(requests);
    }

    [HttpPost("students/{studentId:guid}/leave-requests")]
    public async Task<ActionResult<PortalLeaveSummary>> RequestStudentLeave(Guid studentId, PortalLeaveRequest request, CancellationToken token)
    {
        var user = await users.GetUserAsync(User);
        if (user?.AcademyId is null || !await CanAccessStudent(user, studentId, token)) return Forbid();
        if (request.EndDate < request.StartDate || string.IsNullOrWhiteSpace(request.Reason))
            return BadRequest(new { message = "Reason and valid dates are required." });
        var leave = new AcademyDesk.Api.Domain.Entities.LeaveRequest
        {
            AcademyId = user.AcademyId.Value,
            RequesterType = "Student",
            StudentId = studentId,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Reason = request.Reason.Trim()
        };
        db.LeaveRequests.Add(leave);
        await db.SaveChangesAsync(token);
        return Ok(new PortalLeaveSummary(leave.Id, leave.StartDate, leave.EndDate, leave.Reason, leave.Status, leave.DecisionNotes));
    }

    private async Task<bool> CanAccessStudent(ApplicationUser user, Guid studentId, CancellationToken token) =>
        user.StudentId == studentId || (user.GuardianId.HasValue && await db.StudentGuardians.AnyAsync(x =>
            x.AcademyId == user.AcademyId && x.GuardianId == user.GuardianId && x.StudentId == studentId, token));
    [HttpPut("students/{studentId:guid}/profile")]
    public async Task<ActionResult> UpdateProfile(Guid studentId, PortalStudentProfileRequest request, CancellationToken token)
    { var user=await users.GetUserAsync(User); if(user?.AcademyId is null||user.StudentId!=studentId)return Forbid(); var student=await db.Students.SingleOrDefaultAsync(x=>x.Id==studentId&&x.AcademyId==user.AcademyId,token); if(student is null)return NotFound(); student.Email=request.Email?.Trim(); student.Phone=request.Phone?.Trim(); await db.SaveChangesAsync(token); return Ok(); }

    [HttpPut("guardians/{guardianId:guid}/profile")]
    public async Task<ActionResult> UpdateGuardianProfile(Guid guardianId, PortalGuardianProfileRequest request, CancellationToken token)
    {
        var user = await users.GetUserAsync(User);
        if (user?.AcademyId is null || user.GuardianId != guardianId) return Forbid();
        var guardian = await db.Guardians.SingleOrDefaultAsync(x => x.Id == guardianId && x.AcademyId == user.AcademyId, token);
        if (guardian is null) return NotFound();
        guardian.Email = request.Email?.Trim();
        guardian.Phone = request.Phone?.Trim();
        await db.SaveChangesAsync(token);
        return Ok(new { guardian.Id, guardian.Email, guardian.Phone });
    }
}

public sealed record PortalStudentDetails(string Name, IReadOnlyList<PortalBatch> Batches, IReadOnlyList<PortalAssignment> Assignments, IReadOnlyList<PortalAttendance> Attendance, IReadOnlyList<PortalMusicProgress> Music, IReadOnlyList<PortalResource> Resources, IReadOnlyList<PortalPracticeLog> PracticeLogs, IReadOnlyList<PortalInvoice> Invoices);
public sealed record PortalBatch(Guid Id, string Name);
public sealed record PortalAssignment(Guid Id, string Title, string Type, DateTime? DueAtUtc);
public sealed record PortalAttendance(DateTime StartUtc, string Status);
public sealed record PortalMusicProgress(string Title, string Status, DateOnly? TargetDate);
public sealed record PortalResource(string Title, string Type, string Url);
public sealed record PortalPracticeLog(DateOnly PracticeDate, int MinutesPracticed, string? FocusArea, string? TeacherFeedback, string Status);
public sealed record PortalInvoice(string InvoiceNumber, decimal TotalAmount, decimal Balance, string Currency, DateOnly DueDate, string Status);
public sealed record PortalSubmissionRequest(string? ResponseText);
public sealed record PortalStudentProfileRequest(string? Email,string? Phone);
public sealed record PortalGuardianProfileRequest(string? Email, string? Phone);
public sealed record PortalChangePasswordRequest(string CurrentPassword, string NewPassword);
public sealed record PortalLeaveRequest(DateOnly StartDate, DateOnly EndDate, string Reason);
public sealed record PortalLeaveSummary(Guid Id, DateOnly StartDate, DateOnly EndDate, string Reason, string Status, string? DecisionNotes);
