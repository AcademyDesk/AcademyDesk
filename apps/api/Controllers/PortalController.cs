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

    [HttpGet("notifications")]
    public async Task<ActionResult> Notifications(CancellationToken token)
    {
        var user = await users.GetUserAsync(User);
        if (user?.AcademyId is null) return Forbid();
        var recipientId = user.StudentId ?? user.GuardianId;
        if (!recipientId.HasValue) return Forbid();
        var recipientType = user.StudentId.HasValue ? "Student" : "Guardian";
        var notifications = await db.Notifications.AsNoTracking()
            .Where(x => x.AcademyId == user.AcademyId && x.RecipientId == recipientId && x.RecipientType == recipientType)
            .OrderByDescending(x => x.CreatedAtUtc).Take(100)
            .Select(x => new PortalNotificationSummary(x.Id, x.Title, x.Message, x.Channel, x.Status, x.CreatedAtUtc, x.SentAtUtc))
            .ToListAsync(token);
        return Ok(notifications);
    }

    [HttpPatch("notifications/{notificationId:guid}/read")]
    public async Task<ActionResult> MarkNotificationRead(Guid notificationId, CancellationToken token)
    {
        var user = await users.GetUserAsync(User);
        if (user?.AcademyId is null) return Forbid();
        var recipientId = user.StudentId ?? user.GuardianId;
        var recipientType = user.StudentId.HasValue ? "Student" : "Guardian";
        if (!recipientId.HasValue) return Forbid();
        var notification = await db.Notifications.SingleOrDefaultAsync(x => x.Id == notificationId && x.AcademyId == user.AcademyId && x.RecipientId == recipientId && x.RecipientType == recipientType, token);
        if (notification is null) return NotFound();
        notification.Status = "Read";
        await db.SaveChangesAsync(token);
        return Ok(new { notification.Id, notification.Status });
    }

    [HttpGet("events")]
    public async Task<ActionResult> UpcomingEvents(CancellationToken token)
    {
        var user = await users.GetUserAsync(User);
        if (user?.AcademyId is null || (!user.StudentId.HasValue && !user.GuardianId.HasValue)) return Forbid();
        var now = DateTime.UtcNow;
        var events = await db.AcademyEvents.AsNoTracking()
            .Where(x => x.AcademyId == user.AcademyId && x.Status == "Published" && x.StartUtc >= now)
            .OrderBy(x => x.StartUtc).Take(50)
            .Select(x => new PortalEventSummary(x.Id, x.Title, x.Type, x.StartUtc, x.EndUtc, x.Venue, x.Notes))
            .ToListAsync(token);
        return Ok(events);
    }

    [HttpGet("guardians/{guardianId:guid}/children")]
    public async Task<ActionResult> GuardianChildren(Guid guardianId, CancellationToken token)
    {
        var user = await users.GetUserAsync(User);
        if (user?.AcademyId is null || user.GuardianId != guardianId) return Forbid();
        var children = await db.StudentGuardians.AsNoTracking()
            .Where(x => x.AcademyId == user.AcademyId && x.GuardianId == guardianId)
            .Join(db.Students.AsNoTracking(), x => x.StudentId, s => s.Id, (x, s) => new { s.Id, s.FirstName, s.LastName, s.Email, s.Phone, s.IsActive })
            .Select(x => new PortalChildSummary(x.Id, x.FirstName + " " + x.LastName, x.Email, x.Phone, x.IsActive,
                db.Enrollments.Count(e => e.AcademyId == user.AcademyId && e.StudentId == x.Id && e.Status == "Active")))
            .OrderBy(x => x.Name).ToListAsync(token);
        return Ok(children);
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
        var schedule = await db.ClassSessions.AsNoTracking()
            .Where(x => x.AcademyId == user.AcademyId && batchIds.Contains(x.BatchId) && x.StartUtc >= DateTime.UtcNow.AddDays(-1))
            .OrderBy(x => x.StartUtc).Take(20)
            .Join(db.Batches.AsNoTracking(), session => session.BatchId, batch => batch.Id,
                (session, batch) => new PortalSession(session.Id, batch.Name, session.StartUtc, session.EndUtc, session.DeliveryMode, session.RoomName, session.Status))
            .ToListAsync(token);
        var assignments = await db.Assignments.AsNoTracking().Where(x => x.AcademyId == user.AcademyId && batchIds.Contains(x.BatchId) && x.IsPublished).OrderBy(x => x.DueAtUtc).Take(30).Select(x => new PortalAssignment(x.Id, x.Title, x.Type, x.DueAtUtc)).ToListAsync(token);
        var attendance = await db.AttendanceRecords.AsNoTracking().Where(x => x.AcademyId == user.AcademyId && x.StudentId == studentId).Join(db.ClassSessions.AsNoTracking(), a => a.ClassSessionId, s => s.Id, (a, s) => new PortalAttendance(s.StartUtc, a.Status)).OrderByDescending(x => x.StartUtc).Take(30).ToListAsync(token);
        var music = await db.StudentMusicProgress.AsNoTracking().Where(x => x.AcademyId == user.AcademyId && x.StudentId == studentId).Join(db.MusicPieces.AsNoTracking(), p => p.MusicPieceId, piece => piece.Id, (p, piece) => new PortalMusicProgress(piece.Title, p.Status, p.TargetDate)).OrderBy(x => x.TargetDate).ToListAsync(token);
        var resources = await db.LearningResources.AsNoTracking().Where(x => x.AcademyId == user.AcademyId && x.IsPublished && (!x.BatchId.HasValue || batchIds.Contains(x.BatchId.Value))).OrderByDescending(x => x.CreatedAtUtc).Take(30).Select(x => new PortalResource(x.Title, x.Type, x.Url)).ToListAsync(token);
        var practice = await db.PracticeLogs.AsNoTracking().Where(x => x.AcademyId == user.AcademyId && x.StudentId == studentId).OrderByDescending(x => x.PracticeDate).Take(20).Select(x => new PortalPracticeLog(x.PracticeDate, x.MinutesPracticed, x.FocusArea, x.TeacherFeedback, x.Status)).ToListAsync(token);
        var attendanceSummary = new PortalAttendanceSummary(attendance.Count, attendance.Count(x => x.Status == "Present"), attendance.Count(x => x.Status == "Absent"), attendance.Count(x => x.Status == "Late"), attendance.Count(x => x.Status == "Excused" || x.Status == "Online"));
        var practiceSummary = new PortalPracticeSummary(practice.Count, practice.Sum(x => x.MinutesPracticed));
        var lessonPlans = await db.LessonPlans.AsNoTracking().Where(x => x.AcademyId == user.AcademyId && batchIds.Contains(x.BatchId)).OrderByDescending(x => x.CreatedAtUtc).Take(50).Select(x => new PortalLessonPlan(x.BatchId, x.Title, x.Objectives, x.Status)).ToListAsync(token);
        var courseIds = await db.Batches.AsNoTracking().Where(x => batchIds.Contains(x.Id)).Select(x => x.CourseId).Distinct().ToArrayAsync(token);
        var modules = await db.CourseModules.AsNoTracking().Where(x => x.AcademyId == user.AcademyId && courseIds.Contains(x.CourseId) && x.IsPublished).OrderBy(x => x.Sequence).Select(x => new PortalCourseModule(x.CourseId, x.Title, x.Description, x.Sequence)).ToListAsync(token);
        var certificates = await db.Certificates.AsNoTracking().Where(x => x.AcademyId == user.AcademyId && x.StudentId == studentId && x.Status == "Issued").OrderByDescending(x => x.IssuedDate).Select(x => new PortalCertificate(x.CertificateNumber, x.Title, x.IssuedDate, x.Notes)).ToListAsync(token);
        var invoices = await db.Invoices.AsNoTracking().Where(x => x.AcademyId == user.AcademyId && x.StudentId == studentId).OrderByDescending(x => x.IssuedDate).ToListAsync(token);
        var invoiceIds = invoices.Select(x => x.Id).ToArray();
        var paid = await db.Payments.AsNoTracking().Where(x => invoiceIds.Contains(x.InvoiceId) && x.Status == "Completed").GroupBy(x => x.InvoiceId).Select(x => new { x.Key, Total = x.Sum(p => p.Amount) }).ToDictionaryAsync(x => x.Key, x => x.Total, token);
        var results = await db.AssessmentResults.AsNoTracking()
            .Where(x => x.AcademyId == user.AcademyId && x.StudentId == studentId && x.IsPublished)
            .Join(db.Assessments.AsNoTracking().Where(x => batchIds.Contains(x.BatchId)), x => x.AssessmentId, a => a.Id,
                (x, a) => new PortalAssessmentResult(a.Title, a.Type, a.MaxScore, x.Score, x.Grade, x.Remarks))
            .OrderByDescending(x => x.Title).ToListAsync(token);
        return Ok(new PortalStudentDetails($"{student.FirstName} {student.LastName}", student.Email, student.Phone, batches, schedule, assignments, attendance, attendanceSummary, music, resources, practice, practiceSummary, lessonPlans, modules, certificates, invoices.Select(x => new PortalInvoice(x.InvoiceNumber, x.TotalAmount, x.TotalAmount - paid.GetValueOrDefault(x.Id), x.Currency, x.DueDate, x.Status)).ToList(), results));
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

    [HttpPost("students/{studentId:guid}/practice-logs")]
    public async Task<ActionResult> LogPractice(Guid studentId, PortalPracticeLogRequest request, CancellationToken token)
    {
        var user = await users.GetUserAsync(User);
        if (user?.AcademyId is null || !await CanAccessStudent(user, studentId, token)) return Forbid();
        if (request.MinutesPracticed is < 1 or > 1440)
            return BadRequest(new { message = "Practice time must be between 1 and 1440 minutes." });
        if (request.PracticeDate > DateOnly.FromDateTime(DateTime.UtcNow))
            return BadRequest(new { message = "Practice date cannot be in the future." });
        var log = new AcademyDesk.Api.Domain.Entities.PracticeLog
        {
            AcademyId = user.AcademyId.Value,
            StudentId = studentId,
            PracticeDate = request.PracticeDate,
            MinutesPracticed = request.MinutesPracticed,
            FocusArea = request.FocusArea?.Trim(),
            Notes = request.Notes?.Trim()
        };
        db.PracticeLogs.Add(log);
        await db.SaveChangesAsync(token);
        return Ok(new { log.Id, log.PracticeDate, log.MinutesPracticed, log.FocusArea, log.Notes, log.Status });
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

public sealed record PortalStudentDetails(string Name, string? Email, string? Phone, IReadOnlyList<PortalBatch> Batches, IReadOnlyList<PortalSession> Schedule, IReadOnlyList<PortalAssignment> Assignments, IReadOnlyList<PortalAttendance> Attendance, PortalAttendanceSummary AttendanceSummary, IReadOnlyList<PortalMusicProgress> Music, IReadOnlyList<PortalResource> Resources, IReadOnlyList<PortalPracticeLog> PracticeLogs, PortalPracticeSummary PracticeSummary, IReadOnlyList<PortalLessonPlan> LessonPlans, IReadOnlyList<PortalCourseModule> Modules, IReadOnlyList<PortalCertificate> Certificates, IReadOnlyList<PortalInvoice> Invoices, IReadOnlyList<PortalAssessmentResult> AssessmentResults);
public sealed record PortalBatch(Guid Id, string Name);
public sealed record PortalSession(Guid Id, string BatchName, DateTime StartUtc, DateTime EndUtc, string DeliveryMode, string? RoomName, string Status);
public sealed record PortalAssignment(Guid Id, string Title, string Type, DateTime? DueAtUtc);
public sealed record PortalAttendance(DateTime StartUtc, string Status);
public sealed record PortalAttendanceSummary(int Total, int Present, int Absent, int Late, int Other);
public sealed record PortalMusicProgress(string Title, string Status, DateOnly? TargetDate);
public sealed record PortalResource(string Title, string Type, string Url);
public sealed record PortalPracticeLog(DateOnly PracticeDate, int MinutesPracticed, string? FocusArea, string? TeacherFeedback, string Status);
public sealed record PortalPracticeSummary(int LogCount, int TotalMinutes);
public sealed record PortalInvoice(string InvoiceNumber, decimal TotalAmount, decimal Balance, string Currency, DateOnly DueDate, string Status);
public sealed record PortalAssessmentResult(string Title, string Type, decimal MaxScore, decimal Score, string? Grade, string? Remarks);
public sealed record PortalLessonPlan(Guid BatchId, string Title, string? Objectives, string Status);
public sealed record PortalCourseModule(Guid CourseId, string Title, string? Description, int Sequence);
public sealed record PortalCertificate(string CertificateNumber, string Title, DateOnly IssuedDate, string? Notes);
public sealed record PortalSubmissionRequest(string? ResponseText);
public sealed record PortalStudentProfileRequest(string? Email,string? Phone);
public sealed record PortalGuardianProfileRequest(string? Email, string? Phone);
public sealed record PortalChangePasswordRequest(string CurrentPassword, string NewPassword);
public sealed record PortalLeaveRequest(DateOnly StartDate, DateOnly EndDate, string Reason);
public sealed record PortalLeaveSummary(Guid Id, DateOnly StartDate, DateOnly EndDate, string Reason, string Status, string? DecisionNotes);
public sealed record PortalPracticeLogRequest(DateOnly PracticeDate, int MinutesPracticed, string? FocusArea, string? Notes);
public sealed record PortalNotificationSummary(Guid Id, string Title, string Message, string Channel, string Status, DateTime CreatedAtUtc, DateTime? SentAtUtc);
public sealed record PortalChildSummary(Guid Id, string Name, string? Email, string? Phone, bool IsActive, int ActiveEnrollmentCount);
public sealed record PortalEventSummary(Guid Id, string Title, string Type, DateTime StartUtc, DateTime EndUtc, string? Venue, string? Notes);
