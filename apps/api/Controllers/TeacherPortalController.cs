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

    [HttpGet("leave-requests")]
    public async Task<ActionResult<IReadOnlyList<TeacherLeaveSummary>>> LeaveRequests(CancellationToken cancellationToken)
    {
        var user = await userManager.GetUserAsync(User);
        if (user?.AcademyId is null || user.TeacherId is null) return Forbid();
        var requests = await dbContext.LeaveRequests.AsNoTracking()
            .Where(x => x.AcademyId == user.AcademyId && x.TeacherId == user.TeacherId && x.RequesterType == "Teacher")
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new TeacherLeaveSummary(x.Id, x.StartDate, x.EndDate, x.Reason, x.Status, x.DecisionNotes))
            .ToListAsync(cancellationToken);
        return Ok(requests);
    }

    [HttpPost("leave-requests")]
    public async Task<ActionResult<TeacherLeaveSummary>> RequestLeave(TeacherLeaveRequest request, CancellationToken cancellationToken)
    {
        var user = await userManager.GetUserAsync(User);
        if (user?.AcademyId is null || user.TeacherId is null) return Forbid();
        if (request.EndDate < request.StartDate || string.IsNullOrWhiteSpace(request.Reason))
            return BadRequest(new { message = "Reason and valid dates are required." });
        var leave = new LeaveRequest
        {
            AcademyId = user.AcademyId.Value,
            RequesterType = "Teacher",
            TeacherId = user.TeacherId.Value,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Reason = request.Reason.Trim()
        };
        dbContext.LeaveRequests.Add(leave);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(new TeacherLeaveSummary(leave.Id, leave.StartDate, leave.EndDate, leave.Reason, leave.Status, leave.DecisionNotes));
    }

    [HttpGet("practice-logs")]
    public async Task<ActionResult<IReadOnlyList<TeacherPracticeLogSummary>>> PracticeLogs(CancellationToken cancellationToken)
    {
        var user = await userManager.GetUserAsync(User);
        if (user?.AcademyId is null || user.TeacherId is null) return Forbid();
        var studentIds = await dbContext.Enrollments.AsNoTracking()
            .Where(e => e.AcademyId == user.AcademyId && e.Status == "Active")
            .Join(dbContext.Batches.AsNoTracking().Where(b => b.TeacherId == user.TeacherId), e => e.BatchId, b => b.Id, (e, _) => e.StudentId)
            .Distinct().ToListAsync(cancellationToken);
        var logs = await dbContext.PracticeLogs.AsNoTracking()
            .Where(x => x.AcademyId == user.AcademyId && studentIds.Contains(x.StudentId))
            .Join(dbContext.Students.AsNoTracking(), x => x.StudentId, s => s.Id,
                (x, s) => new TeacherPracticeLogSummary(x.Id, x.StudentId, s.FirstName + " " + s.LastName, x.PracticeDate, x.MinutesPracticed, x.FocusArea, x.Notes, x.TeacherFeedback, x.Status))
            .OrderByDescending(x => x.PracticeDate).Take(100).ToListAsync(cancellationToken);
        return Ok(logs);
    }

    [HttpPatch("practice-logs/{practiceLogId:guid}/review")]
    public async Task<ActionResult> ReviewPracticeLog(Guid practiceLogId, TeacherPracticeReviewRequest request, CancellationToken cancellationToken)
    {
        var user = await userManager.GetUserAsync(User);
        if (user?.AcademyId is null || user.TeacherId is null) return Forbid();
        var permitted = await dbContext.PracticeLogs.AnyAsync(x => x.Id == practiceLogId && x.AcademyId == user.AcademyId &&
            dbContext.Enrollments.Any(e => e.AcademyId == user.AcademyId && e.StudentId == x.StudentId && e.Status == "Active" &&
                dbContext.Batches.Any(b => b.Id == e.BatchId && b.TeacherId == user.TeacherId)), cancellationToken);
        if (!permitted) return Forbid();
        var log = await dbContext.PracticeLogs.SingleAsync(x => x.Id == practiceLogId, cancellationToken);
        log.TeacherFeedback = request.TeacherFeedback?.Trim();
        log.Status = "Reviewed";
        log.ReviewedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(new { log.Id, log.Status, log.TeacherFeedback, log.ReviewedAtUtc });
    }

    [HttpGet("assignments")]
    public async Task<ActionResult<IReadOnlyList<TeacherAssignmentSummary>>> Assignments(CancellationToken cancellationToken)
    {
        var user = await userManager.GetUserAsync(User);
        if (user?.AcademyId is null || user.TeacherId is null) return Forbid();
        var items = await dbContext.Assignments.AsNoTracking()
            .Where(x => x.AcademyId == user.AcademyId && dbContext.Batches.Any(b => b.Id == x.BatchId && b.TeacherId == user.TeacherId))
            .OrderByDescending(x => x.DueAtUtc)
            .Select(x => new TeacherAssignmentSummary(x.Id, x.BatchId, x.Title, x.Description, x.DueAtUtc, x.Type, x.IsPublished))
            .ToListAsync(cancellationToken);
        return Ok(items);
    }

    [HttpPost("assignments")]
    public async Task<ActionResult<TeacherAssignmentSummary>> CreateAssignment(TeacherCreateAssignmentRequest request, CancellationToken cancellationToken)
    {
        var user = await userManager.GetUserAsync(User);
        if (user?.AcademyId is null || user.TeacherId is null) return Forbid();
        if (string.IsNullOrWhiteSpace(request.Title) || !await OwnsBatch(user, request.BatchId, cancellationToken))
            return BadRequest(new { message = "Select a batch you teach and provide an assignment title." });
        var assignment = new Assignment
        {
            AcademyId = user.AcademyId.Value,
            BatchId = request.BatchId,
            Title = request.Title.Trim(),
            Description = request.Description?.Trim(),
            DueAtUtc = request.DueAtUtc,
            Type = string.IsNullOrWhiteSpace(request.Type) ? "Homework" : request.Type.Trim(),
            IsPublished = request.IsPublished
        };
        dbContext.Assignments.Add(assignment);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(new TeacherAssignmentSummary(assignment.Id, assignment.BatchId, assignment.Title, assignment.Description, assignment.DueAtUtc, assignment.Type, assignment.IsPublished));
    }

    [HttpPatch("assignments/{assignmentId:guid}/publish")]
    public async Task<ActionResult> PublishAssignment(Guid assignmentId, TeacherPublishAssignmentRequest request, CancellationToken cancellationToken)
    {
        var user = await userManager.GetUserAsync(User);
        if (user?.AcademyId is null || user.TeacherId is null) return Forbid();
        var assignment = await dbContext.Assignments.SingleOrDefaultAsync(x => x.Id == assignmentId && x.AcademyId == user.AcademyId, cancellationToken);
        if (assignment is null || !await OwnsBatch(user, assignment.BatchId, cancellationToken)) return Forbid();
        assignment.IsPublished = request.IsPublished;
        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(new { assignment.Id, assignment.IsPublished });
    }

    [HttpGet("assignments/{assignmentId:guid}/submissions")]
    public async Task<ActionResult<IReadOnlyList<TeacherSubmissionSummary>>> Submissions(Guid assignmentId, CancellationToken cancellationToken)
    {
        var user = await userManager.GetUserAsync(User);
        if (user?.AcademyId is null || user.TeacherId is null) return Forbid();
        var assignment = await dbContext.Assignments.AsNoTracking().SingleOrDefaultAsync(x => x.Id == assignmentId && x.AcademyId == user.AcademyId, cancellationToken);
        if (assignment is null || !await OwnsBatch(user, assignment.BatchId, cancellationToken)) return Forbid();
        var submissions = await dbContext.AssignmentSubmissions.AsNoTracking()
            .Where(x => x.AcademyId == user.AcademyId && x.AssignmentId == assignmentId)
            .Join(dbContext.Students.AsNoTracking(), x => x.StudentId, s => s.Id,
                (x, s) => new TeacherSubmissionSummary(x.Id, x.StudentId, s.FirstName + " " + s.LastName, x.ResponseText, x.Status, x.SubmittedAtUtc, x.TeacherFeedback))
            .OrderByDescending(x => x.SubmittedAtUtc).ToListAsync(cancellationToken);
        return Ok(submissions);
    }

    [HttpPatch("submissions/{submissionId:guid}/review")]
    public async Task<ActionResult> ReviewSubmission(Guid submissionId, TeacherSubmissionReviewRequest request, CancellationToken cancellationToken)
    {
        var user = await userManager.GetUserAsync(User);
        if (user?.AcademyId is null || user.TeacherId is null) return Forbid();
        var submission = await dbContext.AssignmentSubmissions.SingleOrDefaultAsync(x => x.Id == submissionId && x.AcademyId == user.AcademyId, cancellationToken);
        if (submission is null) return NotFound();
        var assignment = await dbContext.Assignments.AsNoTracking().SingleOrDefaultAsync(x => x.Id == submission.AssignmentId && x.AcademyId == user.AcademyId, cancellationToken);
        if (assignment is null || !await OwnsBatch(user, assignment.BatchId, cancellationToken)) return Forbid();
        submission.TeacherFeedback = request.Feedback?.Trim();
        submission.Status = "Reviewed";
        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(new { submission.Id, submission.Status, submission.TeacherFeedback });
    }

    [HttpGet("assessments")]
    public async Task<ActionResult<IReadOnlyList<TeacherAssessmentSummary>>> Assessments(CancellationToken cancellationToken)
    {
        var user = await userManager.GetUserAsync(User);
        if (user?.AcademyId is null || user.TeacherId is null) return Forbid();
        var items = await dbContext.Assessments.AsNoTracking()
            .Where(x => x.AcademyId == user.AcademyId && dbContext.Batches.Any(b => b.Id == x.BatchId && b.TeacherId == user.TeacherId))
            .OrderByDescending(x => x.ScheduledAtUtc)
            .Select(x => new TeacherAssessmentSummary(x.Id, x.BatchId, x.Title, x.Type, x.MaxScore, x.ScheduledAtUtc, x.IsPublished))
            .ToListAsync(cancellationToken);
        return Ok(items);
    }

    [HttpPost("assessments")]
    public async Task<ActionResult<TeacherAssessmentSummary>> CreateAssessment(TeacherCreateAssessmentRequest request, CancellationToken cancellationToken)
    {
        var user = await userManager.GetUserAsync(User);
        if (user?.AcademyId is null || user.TeacherId is null) return Forbid();
        if (string.IsNullOrWhiteSpace(request.Title) || request.MaxScore <= 0 || !await OwnsBatch(user, request.BatchId, cancellationToken))
            return BadRequest(new { message = "Select a batch you teach, provide a title, and use a positive maximum score." });
        var assessment = new Assessment
        {
            AcademyId = user.AcademyId.Value,
            BatchId = request.BatchId,
            Title = request.Title.Trim(),
            Type = string.IsNullOrWhiteSpace(request.Type) ? "Assessment" : request.Type.Trim(),
            MaxScore = request.MaxScore,
            ScheduledAtUtc = request.ScheduledAtUtc,
            IsPublished = request.IsPublished
        };
        dbContext.Assessments.Add(assessment);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(new TeacherAssessmentSummary(assessment.Id, assessment.BatchId, assessment.Title, assessment.Type, assessment.MaxScore, assessment.ScheduledAtUtc, assessment.IsPublished));
    }

    [HttpPatch("assessments/{assessmentId:guid}/publish")]
    public async Task<ActionResult> PublishAssessment(Guid assessmentId, TeacherPublishAssessmentRequest request, CancellationToken cancellationToken)
    {
        var user = await userManager.GetUserAsync(User);
        if (user?.AcademyId is null || user.TeacherId is null) return Forbid();
        var assessment = await dbContext.Assessments.SingleOrDefaultAsync(x => x.Id == assessmentId && x.AcademyId == user.AcademyId, cancellationToken);
        if (assessment is null || !await OwnsBatch(user, assessment.BatchId, cancellationToken)) return Forbid();
        assessment.IsPublished = request.IsPublished;
        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(new { assessment.Id, assessment.IsPublished });
    }

    private async Task<bool> OwnsBatch(ApplicationUser user, Guid batchId, CancellationToken token) =>
        await dbContext.Batches.AnyAsync(x => x.Id == batchId && x.AcademyId == user.AcademyId && x.TeacherId == user.TeacherId, token);

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
public sealed record TeacherLeaveRequest(DateOnly StartDate, DateOnly EndDate, string Reason);
public sealed record TeacherLeaveSummary(Guid Id, DateOnly StartDate, DateOnly EndDate, string Reason, string Status, string? DecisionNotes);
public sealed record TeacherPracticeLogSummary(Guid Id, Guid StudentId, string StudentName, DateOnly PracticeDate, int MinutesPracticed, string? FocusArea, string? Notes, string? TeacherFeedback, string Status);
public sealed record TeacherPracticeReviewRequest(string? TeacherFeedback);
public sealed record TeacherAssignmentSummary(Guid Id, Guid BatchId, string Title, string? Description, DateTime? DueAtUtc, string Type, bool IsPublished);
public sealed record TeacherCreateAssignmentRequest(Guid BatchId, string Title, string? Description, DateTime? DueAtUtc, string? Type, bool IsPublished);
public sealed record TeacherPublishAssignmentRequest(bool IsPublished);
public sealed record TeacherSubmissionSummary(Guid Id, Guid StudentId, string StudentName, string? ResponseText, string Status, DateTime SubmittedAtUtc, string? TeacherFeedback);
public sealed record TeacherSubmissionReviewRequest(string? Feedback);
public sealed record TeacherAssessmentSummary(Guid Id, Guid BatchId, string Title, string Type, decimal MaxScore, DateTime? ScheduledAtUtc, bool IsPublished);
public sealed record TeacherCreateAssessmentRequest(Guid BatchId, string Title, string? Type, decimal MaxScore, DateTime? ScheduledAtUtc, bool IsPublished);
public sealed record TeacherPublishAssessmentRequest(bool IsPublished);
