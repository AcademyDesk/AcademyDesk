using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Domain.Identity;
using AcademyDesk.Api.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Text;
using System.Text.Json;

namespace AcademyDesk.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/portal")]
public sealed class PortalController(UserManager<ApplicationUser> users, AcademyDeskDbContext db, IWebHostEnvironment environment) : ControllerBase
{
    [HttpGet("me")]
    public async Task<ActionResult> Me(CancellationToken token)
    {
        var user = await users.GetUserAsync(User);
        if (user?.AcademyId is null) return Forbid();
        if (user.StudentId.HasValue)
        {
            var student = await db.Students.AsNoTracking().SingleOrDefaultAsync(x => x.Id == user.StudentId && x.AcademyId == user.AcademyId, token);
            if (student is null || !student.IsActive) return Forbid();
            var enrollmentCount = await db.Enrollments.CountAsync(x => x.AcademyId == user.AcademyId && x.StudentId == student.Id && x.Status == "Active", token);
            var invoiceCount = await db.Invoices.CountAsync(x => x.AcademyId == user.AcademyId && x.StudentId == student.Id, token);
            return Ok(new { role = "Student", displayName = $"{student.FirstName} {student.LastName}", studentId = student.Id, enrollmentCount, invoiceCount });
        }
        if (user.GuardianId.HasValue)
        {
            var parent = await db.Guardians.AsNoTracking().SingleOrDefaultAsync(x => x.Id == user.GuardianId && x.AcademyId == user.AcademyId, token);
            if (parent is null) return Forbid();
            var children = await db.StudentGuardians.AsNoTracking().Where(x => x.AcademyId == user.AcademyId && x.GuardianId == user.GuardianId && x.CanAccessPortal && x.AccessRevokedAtUtc == null).Join(db.Students.AsNoTracking().Where(s => s.IsActive), x => x.StudentId, s => s.Id, (x, s) => new { s.Id, name = s.FirstName + " " + s.LastName, x.CanViewAcademicProgress, x.CanViewFinance, x.CanViewDocuments, x.CanManageLeave }).ToListAsync(token);
            return Ok(new { role = "Parent", displayName = user.DisplayName, parentId = user.GuardianId, parentEmail = parent.Email, parentPhone = parent.Phone, children });
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
        var recipientId = user.StudentId ?? user.GuardianId ?? user.TeacherId;
        if (!recipientId.HasValue) return Forbid();
        var recipientTypes = user.StudentId.HasValue ? new[] { "Student" } : user.TeacherId.HasValue ? new[] { "Teacher" } : new[] { "Guardian", "Parent" };
        var now = DateTime.UtcNow;
        var notifications = await db.Notifications.AsNoTracking()
            .Where(x => x.AcademyId == user.AcademyId && x.RecipientId == recipientId && recipientTypes.Contains(x.RecipientType))
            .Where(RecipientNotificationVisibility.At(now))
            .OrderByDescending(x => x.CreatedAtUtc).Take(100)
            .Select(x => new PortalNotificationSummary(x.Id, x.Title, x.Message, x.Channel, x.Status, x.CreatedAtUtc, x.SentAtUtc,
                x.Status == "Read" || db.NotificationReadReceipts.Any(r => r.NotificationId == x.Id && r.AcademyId == user.AcademyId && r.UserId == user.Id),
                db.NotificationReadReceipts.Where(r => r.NotificationId == x.Id && r.AcademyId == user.AcademyId && r.UserId == user.Id).Select(r => (DateTime?)r.ReadAtUtc).SingleOrDefault()))
            .ToListAsync(token);
        return Ok(notifications);
    }

    [HttpGet("announcements")]
    public async Task<ActionResult<IReadOnlyList<PortalAnnouncementSummary>>> Announcements(CancellationToken token)
    {
        var user = await users.GetUserAsync(User);
        if (user?.AcademyId is null) return Forbid();
        string? audience = user.StudentId.HasValue ? "Student" : user.TeacherId.HasValue ? "Teacher" : null;
        if (audience is null && (await users.IsInRoleAsync(user, "Owner") || await users.IsInRoleAsync(user, "AcademyAdmin"))) audience = "Admin";
        if (audience is null) return Ok(Array.Empty<PortalAnnouncementSummary>());
        var candidates = await db.Notifications.AsNoTracking()
            .Where(x => x.AcademyId == user.AcademyId && x.RecipientId == null && x.RecipientType == "Academy" && x.Status != "Cancelled")
            .OrderByDescending(x => x.CreatedAtUtc).Take(10)
            .Select(x => new PortalAnnouncementCandidate(x.Id, x.Title, x.Message, x.CreatedAtUtc, x.VariablesJson))
            .ToListAsync(token);
        var now = DateTime.UtcNow;
        var announcements = candidates.Where(x => IsActiveImportantAnnouncement(x.VariablesJson, now) && IsAnnouncementForAudience(x.VariablesJson, audience))
            .Select(x => new PortalAnnouncementSummary(x.Id, x.Title, x.Message, x.CreatedAtUtc)).ToList();
        return Ok(announcements);
    }

    [HttpPatch("notifications/{notificationId:guid}/read")]
    public async Task<ActionResult> MarkNotificationRead(Guid notificationId, CancellationToken token)
    {
        var user = await users.GetUserAsync(User);
        if (user?.AcademyId is null) return Forbid();
        var recipientId = user.StudentId ?? user.GuardianId ?? user.TeacherId;
        var recipientTypes = user.StudentId.HasValue ? new[] { "Student" } : user.TeacherId.HasValue ? new[] { "Teacher" } : new[] { "Guardian", "Parent" };
        if (!recipientId.HasValue) return Forbid();
        // Serialize acknowledgments with other writes to this notification on SQL
        // Server. Delivery status is never changed; retrying preserves first read.
        await using var transaction = db.Database.IsSqlServer() ? await db.Database.BeginTransactionAsync(token) : null;
        var source = db.Database.IsSqlServer()
            ? db.Notifications.FromSqlInterpolated($"SELECT * FROM [Notifications] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = {notificationId}")
            : db.Notifications;
        var notification = await source.AsNoTracking()
            .Where(x => x.Id == notificationId && x.AcademyId == user.AcademyId && x.RecipientId == recipientId && recipientTypes.Contains(x.RecipientType))
            .Where(RecipientNotificationVisibility.At(DateTime.UtcNow)).SingleOrDefaultAsync(token);
        if (notification is null) return NotFound();
        var receipt = await db.NotificationReadReceipts.SingleOrDefaultAsync(x => x.NotificationId == notification.Id && x.UserId == user.Id && x.AcademyId == user.AcademyId, token);
        if (receipt is null)
        {
            receipt = new NotificationReadReceipt { NotificationId = notification.Id, UserId = user.Id, AcademyId = user.AcademyId.Value, ReadAtUtc = DateTime.UtcNow };
            db.NotificationReadReceipts.Add(receipt);
            await db.SaveChangesAsync(token);
        }
        if (transaction is not null) await transaction.CommitAsync(token);
        return Ok(new { notification.Id, notification.Status, IsRead = true, receipt.ReadAtUtc });
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
            .Where(x => x.AcademyId == user.AcademyId && x.GuardianId == guardianId && x.CanAccessPortal && x.AccessRevokedAtUtc == null)
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
        var parentAccess = user.GuardianId.HasValue ? await ParentAccess(user, studentId, token) : null;
        if (!await CanAccessStudent(user, studentId, token)) return Forbid();
        var canViewAcademicProgress = parentAccess?.CanViewAcademicProgress ?? true;
        var canViewFinance = parentAccess?.CanViewFinance ?? true;
        var canViewDocuments = parentAccess?.CanViewDocuments ?? true;
        var student = await db.Students.AsNoTracking().SingleOrDefaultAsync(x => x.Id == studentId && x.AcademyId == user.AcademyId, token);
        if (student is null) return NotFound();
        var enrollments = await db.Enrollments.AsNoTracking().Where(x => x.AcademyId == user.AcademyId && x.StudentId == studentId && x.Status == "Active").ToListAsync(token);
        var batchIds = enrollments.Select(x => x.BatchId).ToArray();
        var batchDetails = await db.Batches.AsNoTracking().Where(x => batchIds.Contains(x.Id)).ToListAsync(token);
        var batches = batchDetails.Select(x => new PortalBatch(x.Id, x.Name)).ToList();
        var schedule = await db.ClassSessions.AsNoTracking()
            .Where(x => x.AcademyId == user.AcademyId && batchIds.Contains(x.BatchId) && x.StartUtc >= DateTime.UtcNow.AddMonths(-6) && x.StartUtc <= DateTime.UtcNow.AddMonths(6))
            .OrderBy(x => x.StartUtc).Take(250)
            .Join(db.Batches.AsNoTracking(), session => session.BatchId, batch => batch.Id,
                (session, batch) => new PortalSession(session.Id, batch.Name, session.StartUtc, session.EndUtc, session.DeliveryMode, session.RoomName, batch.MeetingLink, session.Status))
            .ToListAsync(token);
        var assignments = await db.Assignments.AsNoTracking()
            .Where(x => x.AcademyId == user.AcademyId && batchIds.Contains(x.BatchId) && x.IsPublished && (!x.StudentId.HasValue || x.StudentId == studentId))
            .OrderBy(x => x.DueAtUtc).Take(30)
            .Select(x => new PortalAssignment(x.Id, x.Title, x.Description, x.Type, x.DueAtUtc)).ToListAsync(token);
        var attendance = await db.AttendanceRecords.AsNoTracking()
            .Where(x => x.AcademyId == user.AcademyId && x.StudentId == studentId)
            .Join(db.ClassSessions.AsNoTracking(), a => a.ClassSessionId, s => s.Id,
                (a, s) => new { s.StartUtc, a.Status })
            .OrderByDescending(x => x.StartUtc).Take(30)
            .Select(x => new PortalAttendance(x.StartUtc, x.Status))
            .ToListAsync(token);
        var music = await db.StudentMusicProgress.AsNoTracking()
            .Where(x => x.AcademyId == user.AcademyId && x.StudentId == studentId)
            .Join(db.MusicPieces.AsNoTracking(), p => p.MusicPieceId, piece => piece.Id,
                (p, piece) => new { piece.Title, p.Status, p.TargetDate })
            .OrderBy(x => x.TargetDate)
            .Select(x => new PortalMusicProgress(x.Title, x.Status, x.TargetDate))
            .ToListAsync(token);
        var courseIds = batchDetails.Select(x => x.CourseId).Distinct().ToArray();
        var resources = await db.LearningResources.AsNoTracking().Where(x => x.AcademyId == user.AcademyId && x.IsPublished && (!x.BatchId.HasValue || batchIds.Contains(x.BatchId.Value)) && (!x.CourseId.HasValue || courseIds.Contains(x.CourseId.Value)) && (!x.StudentId.HasValue || x.StudentId == studentId)).OrderByDescending(x => x.CreatedAtUtc).Take(30).Select(x => new PortalResource(x.Title, x.Type, x.Url)).ToListAsync(token);
        var practice = await db.PracticeLogs.AsNoTracking().Where(x => x.AcademyId == user.AcademyId && x.StudentId == studentId).OrderByDescending(x => x.PracticeDate).Take(20).Select(x => new PortalPracticeLog(x.PracticeDate, x.MinutesPracticed, x.FocusArea, x.TeacherFeedback, x.Status)).ToListAsync(token);
        var attendanceSummary = new PortalAttendanceSummary(attendance.Count, attendance.Count(x => x.Status == "Present"), attendance.Count(x => x.Status == "Absent"), attendance.Count(x => x.Status == "Late"), attendance.Count(x => x.Status == "Excused" || x.Status == "Online"));
        var practiceSummary = new PortalPracticeSummary(practice.Count, practice.Sum(x => x.MinutesPracticed));
        var lessonPlans = await db.LessonPlans.AsNoTracking().Where(x => x.AcademyId == user.AcademyId && batchIds.Contains(x.BatchId)).OrderByDescending(x => x.CreatedAtUtc).Take(50).Select(x => new PortalLessonPlan(x.BatchId, x.Title, x.Objectives, x.Status)).ToListAsync(token);
        var modules = await db.CourseModules.AsNoTracking().Where(x => x.AcademyId == user.AcademyId && courseIds.Contains(x.CourseId) && x.IsPublished).OrderBy(x => x.Sequence).Select(x => new PortalCourseModule(x.CourseId, x.Title, x.Description, x.Sequence)).ToListAsync(token);
        var certificates = await db.Certificates.AsNoTracking().Where(x => x.AcademyId == user.AcademyId && x.StudentId == studentId && x.Status == "Issued").OrderByDescending(x => x.IssuedDate).Select(x => new PortalCertificate(x.CertificateNumber, x.Title, x.IssuedDate, x.Notes)).ToListAsync(token);
        var invoices = await db.Invoices.AsNoTracking().Where(x => x.AcademyId == user.AcademyId && x.StudentId == studentId).OrderByDescending(x => x.IssuedDate).ToListAsync(token);
        var invoiceIds = invoices.Select(x => x.Id).ToArray();
        var paid = await db.Payments.AsNoTracking().Where(x => invoiceIds.Contains(x.InvoiceId) && (x.Status == "Completed" || x.Status == "Reconciled")).GroupBy(x => x.InvoiceId).Select(x => new { x.Key, Total = x.Sum(p => p.Amount) }).ToDictionaryAsync(x => x.Key, x => x.Total, token);
        var results = await db.AssessmentResults.AsNoTracking()
            .Where(x => x.AcademyId == user.AcademyId && x.StudentId == studentId && x.IsPublished)
            .Join(db.Assessments.AsNoTracking().Where(x => batchIds.Contains(x.BatchId)), x => x.AssessmentId, a => a.Id,
                (x, a) => new { a.Title, a.Type, a.MaxScore, x.Score, x.Grade, x.Remarks })
            .OrderByDescending(x => x.Title)
            .Select(x => new PortalAssessmentResult(x.Title, x.Type, x.MaxScore, x.Score, x.Grade, x.Remarks))
            .ToListAsync(token);
        var historySessions = await db.ClassSessions.AsNoTracking()
            .Where(x => x.AcademyId == user.AcademyId && batchIds.Contains(x.BatchId) && x.StartUtc < DateTime.UtcNow)
            .Join(db.Batches.AsNoTracking(), session => session.BatchId, batch => batch.Id,
                (session, batch) => new { Session = session, batch.Name })
            .OrderByDescending(x => x.Session.StartUtc).Take(50).ToListAsync(token);
        var historySessionIds = historySessions.Select(x => x.Session.Id).ToArray();
        var historyAttendance = await db.AttendanceRecords.AsNoTracking()
            .Where(x => x.AcademyId == user.AcademyId && x.StudentId == studentId && historySessionIds.Contains(x.ClassSessionId))
            .ToDictionaryAsync(x => x.ClassSessionId, x => x, token);
        var historyResources = await db.LearningResources.AsNoTracking()
            .Where(x => x.AcademyId == user.AcademyId && x.IsPublished && x.ClassSessionId.HasValue && historySessionIds.Contains(x.ClassSessionId.Value) && (!x.StudentId.HasValue || x.StudentId == studentId))
            .OrderByDescending(x => x.CreatedAtUtc).ToListAsync(token);
        var classHistory = historySessions.Select(x => new PortalClassHistory(
            x.Session.Id, x.Name, x.Session.StartUtc, x.Session.EndUtc, x.Session.DeliveryMode, x.Session.Status,
            historyAttendance.TryGetValue(x.Session.Id, out var mark) ? mark.Status : "Not marked",
            canViewDocuments ? historyResources.Where(resource => resource.ClassSessionId == x.Session.Id)
                .Select(resource => new PortalClassResource(resource.Title, resource.Description, resource.Type, resource.Url)).ToList() : []))
            .ToList();
        var cycleProgress = batchDetails.Select(batch =>
        {
            var cycleTotal = Math.Max(1, batch.SessionsPerWeek * 4);
            var completed = historySessions.Count(x => x.Session.BatchId == batch.Id && x.Session.Status == "Completed");
            var inCycle = completed % cycleTotal;
            if (completed > 0 && inCycle == 0) inCycle = cycleTotal;
            var covered = historySessions.Where(x => x.Session.BatchId == batch.Id && x.Session.Status == "Completed").Select(x => x.Session.StartUtc).OrderByDescending(x => x).Take(cycleTotal).ToList();
            var upcomingForBatch = schedule.Where(x => x.Id != Guid.Empty && batchIds.Contains(batch.Id) && x.BatchName == batch.Name).Select(x => x.StartUtc).Take(cycleTotal).ToList();
            return new PortalCycleProgress(batch.Id, batch.Name, batch.SessionMinutes, cycleTotal, inCycle, Math.Max(0, cycleTotal - inCycle), covered, upcomingForBatch);
        }).ToList();
        return Ok(new PortalStudentDetails($"{student.FirstName} {student.LastName}", student.Email, student.Phone, student.FirstName, student.LastName, student.PreferredName, student.Gender, student.DateOfBirth, student.AddressLine1, student.City, student.State, student.PostalCode, student.EmergencyContactName, student.EmergencyContactPhone,
            canViewAcademicProgress ? batches : [], canViewAcademicProgress ? schedule : [], canViewAcademicProgress ? assignments : [], canViewAcademicProgress ? attendance : [], canViewAcademicProgress ? attendanceSummary : new PortalAttendanceSummary(0, 0, 0, 0, 0),
            canViewAcademicProgress ? music : [], canViewDocuments ? resources : [], canViewAcademicProgress ? practice : [], canViewAcademicProgress ? practiceSummary : new PortalPracticeSummary(0, 0), canViewAcademicProgress ? lessonPlans : [], canViewAcademicProgress ? modules : [], canViewDocuments ? certificates : [],
            canViewFinance ? invoices.Select(x => new PortalInvoice(x.Id, x.InvoiceNumber, x.TotalAmount, Math.Max(0m, x.TotalAmount - x.AdjustedAmount - paid.GetValueOrDefault(x.Id)), x.Currency, x.DueDate, x.Status)).ToList() : [], canViewAcademicProgress ? results : [], canViewAcademicProgress ? classHistory : [], canViewAcademicProgress ? cycleProgress : []));
    }

    [HttpPost("students/{studentId:guid}/assignments/{assignmentId:guid}/submit")]
    [RequestSizeLimit(50_000_000)]
    public async Task<ActionResult> SubmitAssignment(Guid studentId, Guid assignmentId, [FromForm] PortalSubmissionRequest request, CancellationToken token)
    {
        var user = await users.GetUserAsync(User); if (user?.AcademyId is null) return Forbid();
        if (!await CanAccessStudent(user, studentId, token) || (user.GuardianId.HasValue && !(await ParentAccess(user, studentId, token))!.CanViewAcademicProgress)) return Forbid();
        var assignment = await db.Assignments.SingleOrDefaultAsync(x => x.Id == assignmentId && x.AcademyId == user.AcademyId && x.IsPublished, token); if (assignment is null) return NotFound();
        var isAssigned = (!assignment.StudentId.HasValue || assignment.StudentId == studentId)
            && await db.Enrollments.AnyAsync(x => x.AcademyId == user.AcademyId && x.StudentId == studentId && x.BatchId == assignment.BatchId && x.Status == "Active", token);
        if (!isAssigned) return Forbid();
        var item = await db.AssignmentSubmissions.SingleOrDefaultAsync(x => x.AcademyId == user.AcademyId && x.AssignmentId == assignmentId && x.StudentId == studentId, token);
        if (item is null) { item = new AcademyDesk.Api.Domain.Entities.AssignmentSubmission { AcademyId = user.AcademyId.Value, AssignmentId = assignmentId, StudentId = studentId }; db.AssignmentSubmissions.Add(item); }
        if (string.IsNullOrWhiteSpace(request.ResponseText) && request.File is null) return BadRequest(new { message = "Write a response or attach a practice recording, photo, or file." });
        string? attachment = null;
        if (request.File is { Length: > 0 })
        {
            if (request.File.Length > 50_000_000) return BadRequest(new { message = "Files must be 50 MB or smaller." });
            var extension = Path.GetExtension(request.File.FileName).ToLowerInvariant();
            var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp", ".pdf", ".mp3", ".m4a", ".wav", ".mp4", ".mov", ".webm", ".doc", ".docx" };
            if (!allowed.Contains(extension)) return BadRequest(new { message = "Upload a photo, PDF, audio, video, or document." });
            var root = environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot");
            var folder = Path.Combine(root, "uploads", "student-submissions");
            Directory.CreateDirectory(folder);
            var fileName = $"{Guid.NewGuid():N}{extension}";
            await using var stream = System.IO.File.Create(Path.Combine(folder, fileName));
            await request.File.CopyToAsync(stream, token);
            attachment = $"/uploads/student-submissions/{fileName}";
        }
        item.ResponseText = string.Join("\n", new[] { request.ResponseText?.Trim(), attachment is null ? null : $"Attachment: {attachment}" }.Where(x => !string.IsNullOrWhiteSpace(x)));
        item.Status = "Submitted"; item.SubmittedAtUtc = DateTime.UtcNow; item.TeacherFeedback = null;
        var teacherId = await db.Batches.AsNoTracking().Where(x => x.Id == assignment.BatchId).Select(x => x.TeacherId).SingleOrDefaultAsync(token);
        if (teacherId.HasValue)
            db.Notifications.Add(new Notification { AcademyId = user.AcademyId.Value, RecipientId = teacherId, RecipientType = "Teacher", Title = "Homework submitted", Message = $"A student submitted {assignment.Title} for review.", Channel = "InApp", Status = "Queued" });
        await db.SaveChangesAsync(token); return Ok(item);
    }

    [HttpGet("students/{studentId:guid}/invoices/{invoiceId:guid}/download")]
    public async Task<IActionResult> DownloadInvoice(Guid studentId, Guid invoiceId, CancellationToken token)
    {
        var user = await users.GetUserAsync(User);
        if (user?.AcademyId is null || !await CanAccessStudent(user, studentId, token)
            || (user.GuardianId.HasValue && (await ParentAccess(user, studentId, token))?.CanViewFinance != true)) return Forbid();
        var invoice = await db.Invoices.AsNoTracking().SingleOrDefaultAsync(x => x.Id == invoiceId && x.AcademyId == user.AcademyId && x.StudentId == studentId, token);
        if (invoice is null) return NotFound();
        var paid = await db.Payments.AsNoTracking().Where(x => x.InvoiceId == invoice.Id && (x.Status == "Completed" || x.Status == "Reconciled")).SumAsync(x => (decimal?)x.Amount, token) ?? 0m;
        var balance = Math.Max(0m, invoice.TotalAmount - invoice.AdjustedAmount - paid);
        var document = $"<html><body style='font-family:Arial;padding:48px'><h1>AcademyDesk invoice</h1><h2>{WebUtility.HtmlEncode(invoice.InvoiceNumber)}</h2><p>Issued: {invoice.IssuedDate:dd MMM yyyy}</p><p>Due: {invoice.DueDate:dd MMM yyyy}</p><hr/><p>Total: {invoice.Currency} {invoice.TotalAmount:N2}</p><p>Adjustments: {invoice.Currency} {invoice.AdjustedAmount:N2}</p><p>Paid: {invoice.Currency} {paid:N2}</p><p>Balance: {invoice.Currency} {balance:N2}</p><p>Status: {WebUtility.HtmlEncode(invoice.Status)}</p></body></html>";
        return File(Encoding.UTF8.GetBytes(document), "text/html", $"{invoice.InvoiceNumber}.html");
    }

    [HttpGet("students/{studentId:guid}/certificates/{certificateNumber}/download")]
    public async Task<IActionResult> DownloadCertificate(Guid studentId, string certificateNumber, CancellationToken token)
    {
        var user = await users.GetUserAsync(User);
        if (user?.AcademyId is null || !await CanAccessStudent(user, studentId, token)
            || (user.GuardianId.HasValue && (await ParentAccess(user, studentId, token))?.CanViewDocuments != true)) return Forbid();
        var certificate = await db.Certificates.AsNoTracking().SingleOrDefaultAsync(x => x.AcademyId == user.AcademyId && x.StudentId == studentId && x.CertificateNumber == certificateNumber && x.Status == "Issued", token);
        if (certificate is null) return NotFound();
        var student = await db.Students.AsNoTracking().SingleOrDefaultAsync(x => x.AcademyId == user.AcademyId && x.Id == certificate.StudentId, token);
        if (student is null) return NotFound();
        var document = $"<html><body style='font-family:Georgia;text-align:center;padding:80px;border:12px solid #1674c4'><h1>Certificate of achievement</h1><p>This certifies that</p><h2>{WebUtility.HtmlEncode($"{student.FirstName} {student.LastName}")}</h2><p>has completed</p><h2>{WebUtility.HtmlEncode(certificate.Title)}</h2><p>Certificate no. {WebUtility.HtmlEncode(certificate.CertificateNumber)}</p><p>Issued {certificate.IssuedDate:dd MMM yyyy}</p></body></html>";
        return File(Encoding.UTF8.GetBytes(document), "text/html", $"{certificate.CertificateNumber}.html");
    }

    [HttpGet("students/{studentId:guid}/leave-requests")]
    public async Task<ActionResult<IReadOnlyList<PortalLeaveSummary>>> StudentLeaveRequests(Guid studentId, CancellationToken token)
    {
        var user = await users.GetUserAsync(User);
        if (user?.AcademyId is null || !await CanManageLeave(user, studentId, token)) return Forbid();
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
        if (user?.AcademyId is null || !await CanManageLeave(user, studentId, token)) return Forbid();
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
        if (user?.AcademyId is null || !await CanAccessStudent(user, studentId, token) || (user.GuardianId.HasValue && !(await ParentAccess(user, studentId, token))!.CanViewAcademicProgress)) return Forbid();
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

    private async Task<StudentGuardian?> ParentAccess(ApplicationUser user, Guid studentId, CancellationToken token) =>
        user.GuardianId.HasValue ? await db.StudentGuardians.AsNoTracking().SingleOrDefaultAsync(x =>
            x.AcademyId == user.AcademyId && x.GuardianId == user.GuardianId && x.StudentId == studentId && x.CanAccessPortal && x.AccessRevokedAtUtc == null, token) : null;

    private Task<bool> IsActiveStudent(Guid academyId, Guid studentId, CancellationToken token) =>
        db.Students.AsNoTracking().AnyAsync(x => x.Id == studentId && x.AcademyId == academyId && x.IsActive, token);

    private async Task<bool> CanAccessStudent(ApplicationUser user, Guid studentId, CancellationToken token) =>
        user.AcademyId.HasValue
        && await IsActiveStudent(user.AcademyId.Value, studentId, token)
        && (user.StudentId == studentId || await ParentAccess(user, studentId, token) is not null);

    private async Task<bool> CanManageLeave(ApplicationUser user, Guid studentId, CancellationToken token) =>
        user.AcademyId.HasValue
        && await IsActiveStudent(user.AcademyId.Value, studentId, token)
        && (user.StudentId == studentId || (await ParentAccess(user, studentId, token))?.CanManageLeave == true);

    private static bool IsActiveImportantAnnouncement(string? variablesJson, DateTime now)
    {
        if (string.IsNullOrWhiteSpace(variablesJson)) return false;
        try
        {
            using var document = JsonDocument.Parse(variablesJson);
            var root = document.RootElement;
            if (!root.TryGetProperty("important", out var important) || !string.Equals(important.GetString(), "true", StringComparison.OrdinalIgnoreCase)) return false;
            if (root.TryGetProperty("startsAtUtc", out var start) && DateTime.TryParse(start.GetString(), out var startsAtUtc) && startsAtUtc.ToUniversalTime() > now) return false;
            return root.TryGetProperty("expiresAtUtc", out var expiry) && DateTime.TryParse(expiry.GetString(), out var expiresAtUtc) && expiresAtUtc.ToUniversalTime() > now;
        }
        catch (JsonException) { return false; }
    }
    private static bool IsAnnouncementForAudience(string? variablesJson, string audience)
    {
        try { using var document = JsonDocument.Parse(variablesJson ?? "{}"); if (!document.RootElement.TryGetProperty("audiences", out var targets)) return true; return targets.GetString()?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Any(x => string.Equals(x, audience, StringComparison.OrdinalIgnoreCase) || string.Equals(x, "Both", StringComparison.OrdinalIgnoreCase)) == true; }
        catch (JsonException) { return false; }
    }
    [HttpPut("students/{studentId:guid}/profile")]
    public async Task<ActionResult> UpdateProfile(Guid studentId, PortalStudentProfileRequest request, CancellationToken token)
    {
        var user = await users.GetUserAsync(User);
        if (user?.AcademyId is null || user.StudentId != studentId || !await IsActiveStudent(user.AcademyId.Value, studentId, token)) return Forbid();
        var student = await db.Students.SingleOrDefaultAsync(x => x.Id == studentId && x.AcademyId == user.AcademyId, token);
        if (student is null) return NotFound();
        if (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName)) return BadRequest(new { message = "First and last names are required." });
        student.FirstName = request.FirstName.Trim(); student.LastName = request.LastName.Trim(); student.PreferredName = request.PreferredName?.Trim(); student.Gender = request.Gender?.Trim(); student.DateOfBirth = request.DateOfBirth;
        student.Email = request.Email?.Trim(); student.Phone = request.Phone?.Trim(); student.AddressLine1 = request.AddressLine1?.Trim(); student.City = request.City?.Trim(); student.State = request.State?.Trim(); student.PostalCode = request.PostalCode?.Trim(); student.EmergencyContactName = request.EmergencyContactName?.Trim(); student.EmergencyContactPhone = request.EmergencyContactPhone?.Trim();
        user.DisplayName = $"{student.FirstName} {student.LastName}";
        if (!string.IsNullOrWhiteSpace(student.Email)) user.Email = student.Email;
        user.PhoneNumber = student.Phone;
        var identityUpdate = await users.UpdateAsync(user);
        if (!identityUpdate.Succeeded) return BadRequest(new { message = string.Join(" ", identityUpdate.Errors.Select(x => x.Description)) });
        await db.SaveChangesAsync(token);
        return Ok(new { message = "Profile saved." });
    }

    [HttpPut("guardians/{guardianId:guid}/profile")]
    public async Task<ActionResult> UpdateGuardianProfile(Guid guardianId, PortalGuardianProfileRequest request, CancellationToken token)
    {
        var user = await users.GetUserAsync(User);
        if (user?.AcademyId is null || user.GuardianId != guardianId) return Forbid();
        var guardian = await db.Guardians.SingleOrDefaultAsync(x => x.Id == guardianId && x.AcademyId == user.AcademyId, token);
        if (guardian is null) return NotFound();
        guardian.Email = request.Email?.Trim();
        guardian.Phone = request.Phone?.Trim();
        if (!string.IsNullOrWhiteSpace(guardian.Email)) user.Email = guardian.Email;
        user.PhoneNumber = guardian.Phone;
        var identityUpdate = await users.UpdateAsync(user);
        if (!identityUpdate.Succeeded) return BadRequest(new { message = string.Join(" ", identityUpdate.Errors.Select(x => x.Description)) });
        await db.SaveChangesAsync(token);
        return Ok(new { guardian.Id, guardian.Email, guardian.Phone });
    }
}

public sealed record PortalStudentDetails(string Name, string? Email, string? Phone, string FirstName, string LastName, string? PreferredName, string? Gender, DateOnly? DateOfBirth, string? AddressLine1, string? City, string? State, string? PostalCode, string? EmergencyContactName, string? EmergencyContactPhone, IReadOnlyList<PortalBatch> Batches, IReadOnlyList<PortalSession> Schedule, IReadOnlyList<PortalAssignment> Assignments, IReadOnlyList<PortalAttendance> Attendance, PortalAttendanceSummary AttendanceSummary, IReadOnlyList<PortalMusicProgress> Music, IReadOnlyList<PortalResource> Resources, IReadOnlyList<PortalPracticeLog> PracticeLogs, PortalPracticeSummary PracticeSummary, IReadOnlyList<PortalLessonPlan> LessonPlans, IReadOnlyList<PortalCourseModule> Modules, IReadOnlyList<PortalCertificate> Certificates, IReadOnlyList<PortalInvoice> Invoices, IReadOnlyList<PortalAssessmentResult> AssessmentResults, IReadOnlyList<PortalClassHistory> ClassHistory, IReadOnlyList<PortalCycleProgress> CycleProgress);
public sealed record PortalBatch(Guid Id, string Name);
public sealed record PortalSession(Guid Id, string BatchName, DateTime StartUtc, DateTime EndUtc, string DeliveryMode, string? RoomName, string? MeetingLink, string Status);
public sealed record PortalAssignment(Guid Id, string Title, string? Description, string Type, DateTime? DueAtUtc);
public sealed record PortalAttendance(DateTime StartUtc, string Status);
public sealed record PortalAttendanceSummary(int Total, int Present, int Absent, int Late, int Other);
public sealed record PortalMusicProgress(string Title, string Status, DateOnly? TargetDate);
public sealed record PortalResource(string Title, string Type, string Url);
public sealed record PortalPracticeLog(DateOnly PracticeDate, int MinutesPracticed, string? FocusArea, string? TeacherFeedback, string Status);
public sealed record PortalPracticeSummary(int LogCount, int TotalMinutes);
public sealed record PortalInvoice(Guid Id, string InvoiceNumber, decimal TotalAmount, decimal Balance, string Currency, DateOnly DueDate, string Status);
public sealed record PortalAssessmentResult(string Title, string Type, decimal MaxScore, decimal Score, string? Grade, string? Remarks);
public sealed record PortalLessonPlan(Guid BatchId, string Title, string? Objectives, string Status);
public sealed record PortalCourseModule(Guid CourseId, string Title, string? Description, int Sequence);
public sealed record PortalCertificate(string CertificateNumber, string Title, DateOnly IssuedDate, string? Notes);
public sealed record PortalClassResource(string Title, string? Description, string Type, string Url);
public sealed record PortalClassHistory(Guid SessionId, string BatchName, DateTime StartUtc, DateTime EndUtc, string DeliveryMode, string Status, string AttendanceStatus, IReadOnlyList<PortalClassResource> Resources);
public sealed record PortalCycleProgress(Guid BatchId, string BatchName, int SessionMinutes, int CycleTotal, int CompletedInCycle, int RemainingInCycle, IReadOnlyList<DateTime> CoveredDates, IReadOnlyList<DateTime> UpcomingDates);
public sealed class PortalSubmissionRequest { public string? ResponseText { get; set; } public IFormFile? File { get; set; } }
public sealed record PortalStudentProfileRequest(string? FirstName, string? LastName, string? PreferredName, string? Gender, DateOnly? DateOfBirth, string? Email, string? Phone, string? AddressLine1, string? City, string? State, string? PostalCode, string? EmergencyContactName, string? EmergencyContactPhone);
public sealed record PortalGuardianProfileRequest(string? Email, string? Phone);
public sealed record PortalChangePasswordRequest(string CurrentPassword, string NewPassword);
public sealed record PortalLeaveRequest(DateOnly StartDate, DateOnly EndDate, string Reason);
public sealed record PortalLeaveSummary(Guid Id, DateOnly StartDate, DateOnly EndDate, string Reason, string Status, string? DecisionNotes);
public sealed record PortalPracticeLogRequest(DateOnly PracticeDate, int MinutesPracticed, string? FocusArea, string? Notes);
public sealed record PortalNotificationSummary(Guid Id, string Title, string Message, string Channel, string Status, DateTime CreatedAtUtc, DateTime? SentAtUtc, bool IsRead, DateTime? ReadAtUtc);
public sealed record PortalAnnouncementSummary(Guid Id, string Title, string Message, DateTime CreatedAtUtc);
public sealed record PortalAnnouncementCandidate(Guid Id, string Title, string Message, DateTime CreatedAtUtc, string? VariablesJson);
public sealed record PortalChildSummary(Guid Id, string Name, string? Email, string? Phone, bool IsActive, int ActiveEnrollmentCount);
public sealed record PortalEventSummary(Guid Id, string Title, string Type, DateTime StartUtc, DateTime EndUtc, string? Venue, string? Notes);
