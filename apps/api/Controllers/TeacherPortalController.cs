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
    UserManager<ApplicationUser> userManager,
    IWebHostEnvironment environment) : ControllerBase
{
    private static readonly string[] AttendanceStatuses = ["Present", "Absent"];

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
            .Select(x => new TeacherBatchSummary(x.Id, x.Name, x.Capacity, x.DeliveryMode, x.MeetingLink, x.RoomName))
            .ToListAsync(cancellationToken);
        var batchIds = batches.Select(x => x.Id).ToArray();
        var sessions = await dbContext.ClassSessions.AsNoTracking()
            .Where(x => x.AcademyId == user.AcademyId && batchIds.Contains(x.BatchId) && x.StartUtc >= DateTime.UtcNow.AddDays(-1))
            .OrderBy(x => x.StartUtc)
            .Take(30)
            .Select(x => new TeacherSessionSummary(x.Id, x.BatchId, x.StartUtc, x.EndUtc, x.DeliveryMode, x.RoomName, x.Status, x.TeacherAttendanceStatus))
            .ToListAsync(cancellationToken);

        return Ok(new TeacherPortalSummary(teacher.FirstName, teacher.LastName, batches, sessions));
    }

    [HttpGet("calendar")]
    public async Task<ActionResult<TeacherCalendarSummary>> Calendar(int year, int month, CancellationToken cancellationToken)
    {
        var user = await userManager.GetUserAsync(User);
        if (user?.AcademyId is null || user.TeacherId is null || month is < 1 or > 12 || year is < 2020 or > 2100) return BadRequest();
        var start = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = start.AddMonths(1);
        var sessions = await dbContext.ClassSessions.AsNoTracking()
            .Where(x => x.AcademyId == user.AcademyId && x.TeacherId == user.TeacherId && x.StartUtc >= start && x.StartUtc < end)
            .OrderBy(x => x.StartUtc)
            .Select(x => new TeacherSessionSummary(x.Id, x.BatchId, x.StartUtc, x.EndUtc, x.DeliveryMode, x.RoomName, x.Status, x.TeacherAttendanceStatus))
            .ToListAsync(cancellationToken);
        var holidays = await dbContext.AcademyHolidays.AsNoTracking()
            .Where(x => x.AcademyId == user.AcademyId && x.HolidayDate >= DateOnly.FromDateTime(start) && x.HolidayDate < DateOnly.FromDateTime(end))
            .OrderBy(x => x.HolidayDate)
            .Select(x => new TeacherHolidaySummary(x.Id, x.Name, x.HolidayDate, x.IsClosed))
            .ToListAsync(cancellationToken);
        return Ok(new TeacherCalendarSummary(sessions, holidays));
    }

    [HttpGet("batch-progress")]
    public async Task<ActionResult<IReadOnlyList<TeacherBatchProgressSummary>>> BatchProgress(CancellationToken cancellationToken)
    {
        var user = await userManager.GetUserAsync(User);
        if (user?.AcademyId is null || user.TeacherId is null) return Forbid();

        var batches = await dbContext.Batches.AsNoTracking()
            .Where(x => x.AcademyId == user.AcademyId && x.TeacherId == user.TeacherId && x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new { x.Id, x.Name, x.SessionMinutes, x.SessionsPerWeek })
            .ToListAsync(cancellationToken);
        var batchIds = batches.Select(x => x.Id).ToArray();
        var sessions = await dbContext.ClassSessions.AsNoTracking()
            .Where(x => x.AcademyId == user.AcademyId && batchIds.Contains(x.BatchId))
            .OrderBy(x => x.StartUtc)
            .Select(x => new { x.BatchId, x.StartUtc, x.EndUtc, x.Status })
            .ToListAsync(cancellationToken);
        var payroll = await dbContext.PayrollProfiles.AsNoTracking()
            .SingleOrDefaultAsync(x => x.AcademyId == user.AcademyId && x.TeacherId == user.TeacherId && x.IsActive, cancellationToken);
        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var monthEnd = monthStart.AddMonths(1);

        var result = batches.Select(batch =>
        {
            var batchSessions = sessions.Where(x => x.BatchId == batch.Id).ToArray();
            var completed = batchSessions.Where(x => x.Status == "Completed").OrderByDescending(x => x.StartUtc).ToArray();
            var upcoming = batchSessions.Where(x => x.StartUtc >= now && x.Status != "Completed").OrderBy(x => x.StartUtc).ToArray();
            var isSessionBlock = payroll?.PaymentModel == "SessionBlock" && payroll.SessionsPerCycle is > 0;
            var cycleTotal = isSessionBlock ? payroll!.SessionsPerCycle!.Value : Math.Max(1, batchSessions.Count(x => x.StartUtc >= monthStart && x.StartUtc < monthEnd));
            var completeForCycle = isSessionBlock
                ? completed.Length == 0 ? 0 : completed.Length % cycleTotal == 0 ? cycleTotal : completed.Length % cycleTotal
                : batchSessions.Count(x => x.Status == "Completed" && x.StartUtc >= monthStart && x.StartUtc < monthEnd);
            return new TeacherBatchProgressSummary(batch.Id, batch.Name, batch.SessionMinutes, batch.SessionsPerWeek, isSessionBlock ? "Session cycle" : "Monthly", cycleTotal, completeForCycle, Math.Max(0, cycleTotal - completeForCycle), completeForCycle >= cycleTotal, completed.Take(4).Select(x => x.StartUtc).ToArray(), upcoming.Take(4).Select(x => new TeacherScheduledClassSummary(x.StartUtc, x.EndUtc)).ToArray());
        }).ToArray();
        return Ok(result);
    }

    [HttpGet("profile")]
    public async Task<ActionResult<TeacherPortalProfileSummary>> Profile(CancellationToken cancellationToken)
    {
        var user = await userManager.GetUserAsync(User);
        if (user?.AcademyId is null || user.TeacherId is null) return Forbid();
        var teacher = await dbContext.Teachers.AsNoTracking().SingleOrDefaultAsync(x =>
            x.Id == user.TeacherId && x.AcademyId == user.AcademyId && x.IsActive, cancellationToken);
        return teacher is null ? Forbid() : Ok(new TeacherPortalProfileSummary(teacher.FirstName, teacher.LastName, teacher.PreferredName, teacher.Email, teacher.Phone, teacher.AddressLine1, teacher.City, teacher.State, teacher.PostalCode, teacher.EmergencyContactName, teacher.EmergencyContactPhone));
    }

    [HttpPut("profile")]
    public async Task<ActionResult> UpdateProfile(TeacherPortalProfileRequest request, CancellationToken cancellationToken)
    {
        var user = await userManager.GetUserAsync(User);
        if (user?.AcademyId is null || user.TeacherId is null) return Forbid();
        var teacher = await dbContext.Teachers.SingleOrDefaultAsync(x =>
            x.Id == user.TeacherId && x.AcademyId == user.AcademyId && x.IsActive, cancellationToken);
        if (teacher is null) return Forbid();
        if (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName))
            return BadRequest(new { message = "First and last name are required." });
        teacher.FirstName = request.FirstName.Trim();
        teacher.LastName = request.LastName.Trim();
        teacher.PreferredName = request.PreferredName?.Trim();
        teacher.Email = request.Email?.Trim();
        teacher.Phone = request.Phone?.Trim();
        teacher.AddressLine1 = request.AddressLine1?.Trim();
        teacher.City = request.City?.Trim();
        teacher.State = request.State?.Trim();
        teacher.PostalCode = request.PostalCode?.Trim();
        teacher.EmergencyContactName = request.EmergencyContactName?.Trim();
        teacher.EmergencyContactPhone = request.EmergencyContactPhone?.Trim();
        user.DisplayName = string.Join(" ", new[] { teacher.FirstName, teacher.LastName }.Where(x => !string.IsNullOrWhiteSpace(x)));
        user.PhoneNumber = teacher.Phone;
        var identityUpdate = await userManager.UpdateAsync(user);
        if (!identityUpdate.Succeeded) return BadRequest(new { message = string.Join(" ", identityUpdate.Errors.Select(x => x.Description)) });
        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(new TeacherPortalProfileSummary(teacher.FirstName, teacher.LastName, teacher.PreferredName, teacher.Email, teacher.Phone, teacher.AddressLine1, teacher.City, teacher.State, teacher.PostalCode, teacher.EmergencyContactName, teacher.EmergencyContactPhone));
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
            .Select(x => new TeacherAssignmentSummary(x.Id, x.BatchId, x.StudentId, x.Title, x.Description, x.DueAtUtc, x.Type, x.IsPublished))
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
        if (request.StudentId.HasValue && !await dbContext.Enrollments.AnyAsync(x => x.AcademyId == user.AcademyId && x.BatchId == request.BatchId && x.StudentId == request.StudentId && x.Status == "Active", cancellationToken))
            return BadRequest(new { message = "The selected student is not active in this batch." });
        var assignment = new Assignment
        {
            AcademyId = user.AcademyId.Value,
            BatchId = request.BatchId,
            StudentId = request.StudentId,
            Title = request.Title.Trim(),
            Description = request.Description?.Trim(),
            DueAtUtc = request.DueAtUtc,
            Type = string.IsNullOrWhiteSpace(request.Type) ? "Homework" : request.Type.Trim(),
            IsPublished = request.IsPublished
        };
        dbContext.Assignments.Add(assignment);
        if (assignment.IsPublished)
            await NotifyStudents(assignment.BatchId, assignment.StudentId, "New homework", $"{assignment.Title} has been assigned. Open Tasks to view and submit your work.", cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(new TeacherAssignmentSummary(assignment.Id, assignment.BatchId, assignment.StudentId, assignment.Title, assignment.Description, assignment.DueAtUtc, assignment.Type, assignment.IsPublished));
    }

    [HttpPatch("assignments/{assignmentId:guid}/publish")]
    public async Task<ActionResult> PublishAssignment(Guid assignmentId, TeacherPublishAssignmentRequest request, CancellationToken cancellationToken)
    {
        var user = await userManager.GetUserAsync(User);
        if (user?.AcademyId is null || user.TeacherId is null) return Forbid();
        var assignment = await dbContext.Assignments.SingleOrDefaultAsync(x => x.Id == assignmentId && x.AcademyId == user.AcademyId, cancellationToken);
        if (assignment is null || !await OwnsBatch(user, assignment.BatchId, cancellationToken)) return Forbid();
        assignment.IsPublished = request.IsPublished;
        if (assignment.IsPublished)
            await NotifyStudents(assignment.BatchId, assignment.StudentId, "New homework", $"{assignment.Title} has been assigned. Open Tasks to view and submit your work.", cancellationToken);
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

    [HttpGet("lesson-plans")]
    public async Task<ActionResult<IReadOnlyList<TeacherLessonPlanSummary>>> LessonPlans(CancellationToken cancellationToken)
    {
        var user = await userManager.GetUserAsync(User);
        if (user?.AcademyId is null || user.TeacherId is null) return Forbid();
        var plans = await dbContext.LessonPlans.AsNoTracking()
            .Where(x => x.AcademyId == user.AcademyId && dbContext.Batches.Any(b => b.Id == x.BatchId && b.TeacherId == user.TeacherId))
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new TeacherLessonPlanSummary(x.Id, x.BatchId, x.CourseModuleId, x.ClassSessionId, x.Title, x.Objectives, x.Status))
            .ToListAsync(cancellationToken);
        return Ok(plans);
    }

    [HttpPost("lesson-plans")]
    public async Task<ActionResult<TeacherLessonPlanSummary>> CreateLessonPlan(TeacherCreateLessonPlanRequest request, CancellationToken cancellationToken)
    {
        var user = await userManager.GetUserAsync(User);
        if (user?.AcademyId is null || user.TeacherId is null) return Forbid();
        if (string.IsNullOrWhiteSpace(request.Title) || !await OwnsBatch(user, request.BatchId, cancellationToken))
            return BadRequest(new { message = "Select a batch you teach and provide a lesson title." });
        var plan = new LessonPlan { AcademyId = user.AcademyId.Value, BatchId = request.BatchId, CourseModuleId = request.CourseModuleId, ClassSessionId = request.ClassSessionId, Title = request.Title.Trim(), Objectives = request.Objectives?.Trim() };
        dbContext.LessonPlans.Add(plan);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(new TeacherLessonPlanSummary(plan.Id, plan.BatchId, plan.CourseModuleId, plan.ClassSessionId, plan.Title, plan.Objectives, plan.Status));
    }

    [HttpPatch("lesson-plans/{lessonPlanId:guid}/status")]
    public async Task<ActionResult> UpdateLessonPlanStatus(Guid lessonPlanId, TeacherLessonPlanStatusRequest request, CancellationToken cancellationToken)
    {
        var user = await userManager.GetUserAsync(User);
        if (user?.AcademyId is null || user.TeacherId is null) return Forbid();
        if (request.Status is not ("Planned" or "Delivered" or "Skipped" or "MakeupNeeded")) return BadRequest(new { message = "Invalid lesson-plan status." });
        var plan = await dbContext.LessonPlans.SingleOrDefaultAsync(x => x.Id == lessonPlanId && x.AcademyId == user.AcademyId, cancellationToken);
        if (plan is null || !await OwnsBatch(user, plan.BatchId, cancellationToken)) return Forbid();
        plan.Status = request.Status;
        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(new { plan.Id, plan.Status });
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

    [HttpGet("assessments/{assessmentId:guid}/results")]
    public async Task<ActionResult<IReadOnlyList<TeacherAssessmentResultSummary>>> AssessmentResults(Guid assessmentId, CancellationToken cancellationToken)
    {
        var user = await userManager.GetUserAsync(User);
        if (user?.AcademyId is null || user.TeacherId is null) return Forbid();
        var assessment = await dbContext.Assessments.AsNoTracking().SingleOrDefaultAsync(x => x.Id == assessmentId && x.AcademyId == user.AcademyId, cancellationToken);
        if (assessment is null || !await OwnsBatch(user, assessment.BatchId, cancellationToken)) return Forbid();
        var results = await dbContext.AssessmentResults.AsNoTracking()
            .Where(x => x.AcademyId == user.AcademyId && x.AssessmentId == assessmentId)
            .Join(dbContext.Students.AsNoTracking(), x => x.StudentId, s => s.Id,
                (x, s) => new TeacherAssessmentResultSummary(x.Id, x.StudentId, s.FirstName + " " + s.LastName, x.Score, x.Grade, x.Remarks, x.IsPublished))
            .ToListAsync(cancellationToken);
        return Ok(results);
    }

    [HttpPost("assessments/{assessmentId:guid}/results")]
    public async Task<ActionResult<TeacherAssessmentResultSummary>> RecordAssessmentResult(Guid assessmentId, TeacherRecordAssessmentResultRequest request, CancellationToken cancellationToken)
    {
        var user = await userManager.GetUserAsync(User);
        if (user?.AcademyId is null || user.TeacherId is null) return Forbid();
        var assessment = await dbContext.Assessments.SingleOrDefaultAsync(x => x.Id == assessmentId && x.AcademyId == user.AcademyId, cancellationToken);
        if (assessment is null || !await OwnsBatch(user, assessment.BatchId, cancellationToken)) return Forbid();
        if (request.Score < 0 || request.Score > assessment.MaxScore || !await dbContext.Enrollments.AnyAsync(x =>
            x.AcademyId == user.AcademyId && x.BatchId == assessment.BatchId && x.StudentId == request.StudentId && x.Status == "Active", cancellationToken))
            return BadRequest(new { message = "Student must be actively enrolled and score must be within the assessment range." });
        var result = await dbContext.AssessmentResults.SingleOrDefaultAsync(x => x.AcademyId == user.AcademyId && x.AssessmentId == assessmentId && x.StudentId == request.StudentId, cancellationToken);
        var isNew = result is null;
        result ??= new AssessmentResult { AcademyId = user.AcademyId.Value, AssessmentId = assessmentId, StudentId = request.StudentId };
        result.Score = request.Score; result.Grade = request.Grade?.Trim(); result.Remarks = request.Remarks?.Trim(); result.IsPublished = request.IsPublished;
        if (isNew) dbContext.AssessmentResults.Add(result);
        await dbContext.SaveChangesAsync(cancellationToken);
        var student = await dbContext.Students.AsNoTracking().SingleAsync(x => x.Id == result.StudentId, cancellationToken);
        return Ok(new TeacherAssessmentResultSummary(result.Id, result.StudentId, student.FirstName + " " + student.LastName, result.Score, result.Grade, result.Remarks, result.IsPublished));
    }

    [HttpGet("resources")]
    public async Task<ActionResult<IReadOnlyList<TeacherResourceSummary>>> Resources(Guid? batchId, CancellationToken cancellationToken)
    {
        var user = await userManager.GetUserAsync(User);
        if (user?.AcademyId is null || user.TeacherId is null) return Forbid();
        var items = await dbContext.LearningResources.AsNoTracking()
            .Where(x => x.AcademyId == user.AcademyId && (!batchId.HasValue || x.BatchId == batchId) && x.BatchId.HasValue && dbContext.Batches.Any(b => b.Id == x.BatchId && b.TeacherId == user.TeacherId))
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new TeacherResourceSummary(x.Id, x.BatchId!.Value, x.StudentId, x.ClassSessionId, x.Title, x.Description, x.Type, x.Url, x.CreatedAtUtc))
            .ToListAsync(cancellationToken);
        return Ok(items);
    }

    [HttpGet("classroom-activity")]
    public async Task<ActionResult<TeacherClassroomActivitySummary>> ClassroomActivity(Guid batchId, Guid? studentId, DateTime? fromUtc, DateTime? toUtc, CancellationToken cancellationToken)
    {
        var user = await userManager.GetUserAsync(User);
        if (user?.AcademyId is null || user.TeacherId is null || !await OwnsBatch(user, batchId, cancellationToken)) return Forbid();
        if (studentId.HasValue && !await dbContext.Enrollments.AnyAsync(x => x.AcademyId == user.AcademyId && x.BatchId == batchId && x.StudentId == studentId && x.Status == "Active", cancellationToken))
            return BadRequest(new { message = "The selected student is not active in this batch." });
        var resources = await dbContext.LearningResources.AsNoTracking()
            .Where(x => x.AcademyId == user.AcademyId && x.BatchId == batchId && (!studentId.HasValue || x.StudentId == null || x.StudentId == studentId) && (!fromUtc.HasValue || x.CreatedAtUtc >= fromUtc) && (!toUtc.HasValue || x.CreatedAtUtc < toUtc.Value.AddDays(1)))
            .OrderByDescending(x => x.CreatedAtUtc).Take(30)
            .Select(x => new TeacherResourceSummary(x.Id, x.BatchId!.Value, x.StudentId, x.ClassSessionId, x.Title, x.Description, x.Type, x.Url, x.CreatedAtUtc)).ToListAsync(cancellationToken);
        var homework = await dbContext.Assignments.AsNoTracking()
            .Where(x => x.AcademyId == user.AcademyId && x.BatchId == batchId && (!studentId.HasValue || x.StudentId == null || x.StudentId == studentId) && (!fromUtc.HasValue || x.CreatedAtUtc >= fromUtc) && (!toUtc.HasValue || x.CreatedAtUtc < toUtc.Value.AddDays(1)))
            .OrderByDescending(x => x.CreatedAtUtc).Take(30)
            .Select(x => new TeacherAssignmentSummary(x.Id, x.BatchId, x.StudentId, x.Title, x.Description, x.DueAtUtc, x.Type, x.IsPublished)).ToListAsync(cancellationToken);
        return Ok(new TeacherClassroomActivitySummary(resources, homework));
    }

    [HttpPost("resources/note")]
    public async Task<ActionResult<TeacherResourceSummary>> CreateClassNote(TeacherCreateResourceNoteRequest request, CancellationToken cancellationToken)
    {
        var user = await userManager.GetUserAsync(User);
        if (user?.AcademyId is null || user.TeacherId is null || string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Notes) || !await OwnsBatch(user, request.BatchId, cancellationToken))
            return BadRequest(new { message = "Select one of your batches and provide a title and note." });
        if (request.StudentId.HasValue && !await dbContext.Enrollments.AnyAsync(x => x.AcademyId == user.AcademyId && x.BatchId == request.BatchId && x.StudentId == request.StudentId && x.Status == "Active", cancellationToken))
            return BadRequest(new { message = "The selected student is not active in this batch." });
        var resource = new LearningResource { AcademyId = user.AcademyId.Value, BatchId = request.BatchId, StudentId = request.StudentId, ClassSessionId = request.ClassSessionId, Title = request.Title.Trim(), Description = request.Notes.Trim(), Type = string.IsNullOrWhiteSpace(request.Type) ? "Class note" : request.Type.Trim(), Url = $"note://{Guid.NewGuid():N}", IsPublished = true };
        dbContext.LearningResources.Add(resource);
        await NotifyStudents(request.BatchId, request.StudentId, "New class note", $"{resource.Title} is available in your class history.", cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(ResourceSummary(resource));
    }

    [HttpPost("resources/upload")]
    [RequestSizeLimit(50_000_000)]
    public async Task<ActionResult<TeacherResourceSummary>> UploadClassMaterial([FromForm] TeacherUploadResourceRequest request, CancellationToken cancellationToken)
    {
        var user = await userManager.GetUserAsync(User);
        if (user?.AcademyId is null || user.TeacherId is null || request.File is null || request.File.Length == 0 || !await OwnsBatch(user, request.BatchId, cancellationToken))
            return BadRequest(new { message = "Select one of your batches and a file to upload." });
        if (request.File.Length > 50_000_000) return BadRequest(new { message = "Files must be 50 MB or smaller." });
        if (request.StudentId.HasValue && !await dbContext.Enrollments.AnyAsync(x => x.AcademyId == user.AcademyId && x.BatchId == request.BatchId && x.StudentId == request.StudentId && x.Status == "Active", cancellationToken))
            return BadRequest(new { message = "The selected student is not active in this batch." });
        var extension = Path.GetExtension(request.File.FileName).ToLowerInvariant();
        var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp", ".pdf", ".mp3", ".m4a", ".wav", ".mp4", ".mov", ".webm", ".doc", ".docx" };
        if (!allowed.Contains(extension)) return BadRequest(new { message = "Upload a photo, PDF, audio, video, or document." });
        var folder = Path.Combine(environment.WebRootPath, "uploads", "teacher-materials");
        Directory.CreateDirectory(folder);
        var fileName = $"{Guid.NewGuid():N}{extension}";
        await using (var stream = System.IO.File.Create(Path.Combine(folder, fileName))) await request.File.CopyToAsync(stream, cancellationToken);
        var resource = new LearningResource { AcademyId = user.AcademyId.Value, BatchId = request.BatchId, StudentId = request.StudentId, ClassSessionId = request.ClassSessionId, Title = string.IsNullOrWhiteSpace(request.Title) ? Path.GetFileNameWithoutExtension(request.File.FileName) : request.Title.Trim(), Description = request.Description?.Trim(), Type = string.IsNullOrWhiteSpace(request.Type) ? "Class material" : request.Type.Trim(), Url = $"/uploads/teacher-materials/{fileName}", IsPublished = true };
        dbContext.LearningResources.Add(resource);
        await NotifyStudents(request.BatchId, request.StudentId, "New class material", $"{resource.Title} is available in your class history.", cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(ResourceSummary(resource));
    }

    [HttpGet("progress")]
    public async Task<ActionResult<TeacherProgressSummary>> Progress(CancellationToken cancellationToken)
    {
        var user = await userManager.GetUserAsync(User);
        if (user?.AcademyId is null || user.TeacherId is null) return Forbid();
        var sessions = dbContext.ClassSessions.AsNoTracking().Where(x => x.AcademyId == user.AcademyId && x.TeacherId == user.TeacherId);
        var completed = await sessions.CountAsync(x => x.Status == "Completed", cancellationToken);
        var upcoming = await sessions.CountAsync(x => (x.Status == "Scheduled" || x.Status == "InProgress") && x.StartUtc >= DateTime.UtcNow, cancellationToken);
        var attendance = await dbContext.AttendanceRecords.AsNoTracking().Where(x => x.AcademyId == user.AcademyId && dbContext.ClassSessions.Any(s => s.Id == x.ClassSessionId && s.TeacherId == user.TeacherId)).ToListAsync(cancellationToken);
        var present = attendance.Count(x => x.Status == "Present");
        return Ok(new TeacherProgressSummary(completed, upcoming, attendance.Count, present));
    }

    [HttpGet("payments")]
    public async Task<ActionResult<TeacherPaymentSummary>> Payments(int? year, int? month, CancellationToken cancellationToken)
    {
        var user = await userManager.GetUserAsync(User);
        if (user?.AcademyId is null || user.TeacherId is null || (year.HasValue != month.HasValue) || (month.HasValue && month is < 1 or > 12) || (year.HasValue && year is < 2020 or > 2100)) return BadRequest();
        var profile = await dbContext.PayrollProfiles.AsNoTracking().FirstOrDefaultAsync(x => x.AcademyId == user.AcademyId && x.TeacherId == user.TeacherId && x.IsActive, cancellationToken);
        var payoutQuery = profile is null ? dbContext.PayrollPayouts.AsNoTracking().Where(x => false) : dbContext.PayrollPayouts.AsNoTracking().Where(x => x.AcademyId == user.AcademyId && x.PayrollProfileId == profile.Id);
        if (year.HasValue && month.HasValue)
            payoutQuery = payoutQuery.Where(x => x.Status == "Paid" && x.PaidAtUtc.Year == year.Value && x.PaidAtUtc.Month == month.Value);
        IReadOnlyList<TeacherPayslipSummary> payouts = await payoutQuery.OrderByDescending(x => x.PaidAtUtc).Take(year.HasValue ? 100 : 12).Select(x => new TeacherPayslipSummary(x.Id, x.PayslipNumber, x.PeriodLabel, x.GrossAmount, x.Deductions, x.NetAmount, x.Currency, x.Status, x.PaymentMethod, x.Reference, x.PaidAtUtc)).ToListAsync(cancellationToken);
        return Ok(new TeacherPaymentSummary(profile?.PaymentModel, profile?.MonthlyAmount, profile?.AmountPerCycle, profile?.SessionsPerCycle, payouts));
    }

    private static TeacherResourceSummary ResourceSummary(LearningResource resource) => new(resource.Id, resource.BatchId!.Value, resource.StudentId, resource.ClassSessionId, resource.Title, resource.Description, resource.Type, resource.Url, resource.CreatedAtUtc);

    private async Task NotifyStudents(Guid batchId, Guid? studentId, string title, string message, CancellationToken token)
    {
        var batch = await dbContext.Batches.AsNoTracking().SingleOrDefaultAsync(x => x.Id == batchId, token);
        if (batch is null) return;
        var recipients = studentId.HasValue
            ? new[] { studentId.Value }
            : await dbContext.Enrollments.AsNoTracking().Where(x => x.AcademyId == batch.AcademyId && x.BatchId == batchId && x.Status == "Active").Select(x => x.StudentId).ToArrayAsync(token);
        foreach (var recipientId in recipients.Distinct())
            dbContext.Notifications.Add(new Notification { AcademyId = batch.AcademyId, RecipientId = recipientId, RecipientType = "Student", Title = title, Message = message, Channel = "InApp", Status = "Queued" });
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
                (enrollment, student) => new { student.Id, student.FirstName, student.LastName })
            .OrderBy(x => x.LastName).ThenBy(x => x.FirstName)
            .Select(x => new TeacherRosterStudent(x.Id, x.FirstName, x.LastName))
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
        dbContext.Notifications.Add(new Notification { AcademyId = context.AcademyId, RecipientId = request.StudentId, RecipientType = "Student", Title = "Attendance updated", Message = $"Your attendance for the class has been marked {record.Status}.", Channel = "InApp", Status = "Queued" });
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

    [HttpPatch("sessions/{sessionId:guid}/status")]
    public async Task<ActionResult> UpdateSessionStatus(Guid sessionId, TeacherSessionStatusRequest request, CancellationToken cancellationToken)
    {
        var context = await GetTeacherContext(sessionId, cancellationToken);
        if (context is null) return Forbid();
        if (!new[] { "Scheduled", "InProgress", "Completed", "Cancelled", "Rescheduled" }.Contains(request.Status, StringComparer.OrdinalIgnoreCase))
            return BadRequest(new { message = "Invalid session status." });
        context.Session.Status = request.Status.Trim();
        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(new { context.Session.Id, context.Session.Status });
    }

    [HttpPost("sessions/{sessionId:guid}/attendance/bulk")]
    public async Task<ActionResult> MarkAttendanceBulk(Guid sessionId, TeacherBulkAttendanceRequest request, CancellationToken cancellationToken)
    {
        var context = await GetTeacherContext(sessionId, cancellationToken);
        if (context is null) return Forbid();
        if (request.Records.Count == 0) return BadRequest(new { message = "At least one attendance record is required." });
        if (request.Records.Any(x => !AttendanceStatuses.Contains(x.Status, StringComparer.OrdinalIgnoreCase)))
            return BadRequest(new { message = "One or more attendance statuses are invalid." });
        var studentIds = request.Records.Select(x => x.StudentId).Distinct().ToArray();
        var enrolled = await dbContext.Enrollments.Where(x => x.AcademyId == context.AcademyId && x.BatchId == context.Session.BatchId && x.Status == "Active" && studentIds.Contains(x.StudentId)).Select(x => x.StudentId).ToListAsync(cancellationToken);
        if (enrolled.Count != studentIds.Length) return BadRequest(new { message = "Every student must be actively enrolled in this batch." });
        var existing = await dbContext.AttendanceRecords.Where(x => x.AcademyId == context.AcademyId && x.ClassSessionId == sessionId && studentIds.Contains(x.StudentId)).ToDictionaryAsync(x => x.StudentId, cancellationToken);
        foreach (var item in request.Records)
        {
            if (!existing.TryGetValue(item.StudentId, out var record))
            {
                record = new AttendanceRecord { AcademyId = context.AcademyId, ClassSessionId = sessionId, StudentId = item.StudentId };
                dbContext.AttendanceRecords.Add(record);
            }
            record.Status = item.Status.Trim(); record.Notes = item.Notes?.Trim(); record.MarkedAtUtc = DateTime.UtcNow;
            dbContext.Notifications.Add(new Notification { AcademyId = context.AcademyId, RecipientId = item.StudentId, RecipientType = "Student", Title = "Attendance updated", Message = $"Your attendance for the class has been marked {record.Status}.", Channel = "InApp", Status = "Queued" });
        }
        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(new { updated = request.Records.Count });
    }

    [HttpPut("sessions/{sessionId:guid}/teacher-attendance")]
    public async Task<ActionResult> MarkTeacherAttendance(Guid sessionId, TeacherSessionAttendanceRequest request, CancellationToken cancellationToken)
    {
        var context = await GetTeacherContext(sessionId, cancellationToken);
        if (context is null) return Forbid();
        if (!AttendanceStatuses.Contains(request.Status, StringComparer.OrdinalIgnoreCase)) return BadRequest(new { message = "Invalid attendance status." });
        context.Session.TeacherAttendanceStatus = request.Status.Trim();
        context.Session.TeacherAttendanceMarkedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(new { context.Session.Id, context.Session.TeacherAttendanceStatus });
    }

    private async Task<TeacherContext?> GetTeacherContext(Guid sessionId, CancellationToken cancellationToken)
    {
        var user = await userManager.GetUserAsync(User);
        if (user?.AcademyId is null || user.TeacherId is null) return null;
        var session = await dbContext.ClassSessions.SingleOrDefaultAsync(x =>
            x.Id == sessionId && x.AcademyId == user.AcademyId && x.TeacherId == user.TeacherId, cancellationToken);
        return session is null ? null : new TeacherContext(user.AcademyId.Value, session);
    }

    private sealed record TeacherContext(Guid AcademyId, ClassSession Session);
}

public sealed record TeacherPortalSummary(string FirstName, string LastName, IReadOnlyList<TeacherBatchSummary> Batches, IReadOnlyList<TeacherSessionSummary> Sessions);
public sealed record TeacherBatchSummary(Guid Id, string Name, int Capacity, string DeliveryMode, string? MeetingLink, string? RoomName);
public sealed record TeacherSessionSummary(Guid Id, Guid BatchId, DateTime StartUtc, DateTime EndUtc, string DeliveryMode, string? RoomName, string Status, string? TeacherAttendanceStatus);
public sealed record TeacherHolidaySummary(Guid Id, string Name, DateOnly HolidayDate, bool IsClosed);
public sealed record TeacherCalendarSummary(IReadOnlyList<TeacherSessionSummary> Sessions, IReadOnlyList<TeacherHolidaySummary> Holidays);
public sealed record TeacherRosterStudent(Guid Id, string FirstName, string LastName);
public sealed record TeacherMarkAttendanceRequest(Guid StudentId, string Status, string? Notes);
public sealed record TeacherAttendanceSummary(Guid StudentId, string Status, string? Notes);
public sealed record TeacherSessionStatusRequest(string Status);
public sealed record TeacherBulkAttendanceRequest(IReadOnlyList<TeacherBulkAttendanceItem> Records);
public sealed record TeacherBulkAttendanceItem(Guid StudentId, string Status, string? Notes);
public sealed record TeacherSessionAttendanceRequest(string Status);
public sealed record TeacherPortalProfileRequest(string? FirstName, string? LastName, string? PreferredName, string? Email, string? Phone, string? AddressLine1, string? City, string? State, string? PostalCode, string? EmergencyContactName, string? EmergencyContactPhone);
public sealed record TeacherPortalProfileSummary(string FirstName, string LastName, string? PreferredName, string? Email, string? Phone, string? AddressLine1, string? City, string? State, string? PostalCode, string? EmergencyContactName, string? EmergencyContactPhone);
public sealed record TeacherLeaveRequest(DateOnly StartDate, DateOnly EndDate, string Reason);
public sealed record TeacherLeaveSummary(Guid Id, DateOnly StartDate, DateOnly EndDate, string Reason, string Status, string? DecisionNotes);
public sealed record TeacherPracticeLogSummary(Guid Id, Guid StudentId, string StudentName, DateOnly PracticeDate, int MinutesPracticed, string? FocusArea, string? Notes, string? TeacherFeedback, string Status);
public sealed record TeacherPracticeReviewRequest(string? TeacherFeedback);
public sealed record TeacherAssignmentSummary(Guid Id, Guid BatchId, Guid? StudentId, string Title, string? Description, DateTime? DueAtUtc, string Type, bool IsPublished);
public sealed record TeacherCreateAssignmentRequest(Guid BatchId, Guid? StudentId, string Title, string? Description, DateTime? DueAtUtc, string? Type, bool IsPublished);
public sealed record TeacherPublishAssignmentRequest(bool IsPublished);
public sealed record TeacherSubmissionSummary(Guid Id, Guid StudentId, string StudentName, string? ResponseText, string Status, DateTime SubmittedAtUtc, string? TeacherFeedback);
public sealed record TeacherSubmissionReviewRequest(string? Feedback);
public sealed record TeacherLessonPlanSummary(Guid Id, Guid BatchId, Guid? CourseModuleId, Guid? ClassSessionId, string Title, string? Objectives, string Status);
public sealed record TeacherCreateLessonPlanRequest(Guid BatchId, Guid? CourseModuleId, Guid? ClassSessionId, string Title, string? Objectives);
public sealed record TeacherLessonPlanStatusRequest(string Status);
public sealed record TeacherAssessmentSummary(Guid Id, Guid BatchId, string Title, string Type, decimal MaxScore, DateTime? ScheduledAtUtc, bool IsPublished);
public sealed record TeacherCreateAssessmentRequest(Guid BatchId, string Title, string? Type, decimal MaxScore, DateTime? ScheduledAtUtc, bool IsPublished);
public sealed record TeacherPublishAssessmentRequest(bool IsPublished);
public sealed record TeacherAssessmentResultSummary(Guid Id, Guid StudentId, string StudentName, decimal Score, string? Grade, string? Remarks, bool IsPublished);
public sealed record TeacherRecordAssessmentResultRequest(Guid StudentId, decimal Score, string? Grade, string? Remarks, bool IsPublished);
public sealed record TeacherResourceSummary(Guid Id, Guid BatchId, Guid? StudentId, Guid? ClassSessionId, string Title, string? Description, string Type, string Url, DateTime CreatedAtUtc);
public sealed record TeacherClassroomActivitySummary(IReadOnlyList<TeacherResourceSummary> Resources, IReadOnlyList<TeacherAssignmentSummary> Homework);
public sealed record TeacherCreateResourceNoteRequest(Guid BatchId, Guid? StudentId, Guid? ClassSessionId, string Title, string Notes, string? Type);
public sealed class TeacherUploadResourceRequest { public Guid BatchId { get; set; } public Guid? StudentId { get; set; } public Guid? ClassSessionId { get; set; } public string? Title { get; set; } public string? Description { get; set; } public string? Type { get; set; } public IFormFile? File { get; set; } }
public sealed record TeacherProgressSummary(int CompletedClasses, int UpcomingClasses, int AttendanceRecords, int PresentOrOnline);
public sealed record TeacherBatchProgressSummary(Guid BatchId, string BatchName, int SessionMinutes, int SessionsPerWeek, string PaymentCycle, int CycleTotal, int CompletedInCycle, int RemainingInCycle, bool PaymentReady, IReadOnlyList<DateTime> CoveredClassDates, IReadOnlyList<TeacherScheduledClassSummary> UpcomingClasses);
public sealed record TeacherScheduledClassSummary(DateTime StartUtc, DateTime EndUtc);
public sealed record TeacherPaymentSummary(string? PaymentModel, decimal? MonthlyAmount, decimal? AmountPerCycle, int? SessionsPerCycle, IReadOnlyList<TeacherPayslipSummary> Payslips);
public sealed record TeacherPayslipSummary(Guid Id, string PayslipNumber, string PeriodLabel, decimal GrossAmount, decimal Deductions, decimal NetAmount, string Currency, string Status, string PaymentMethod, string? Reference, DateTime PaidAtUtc);
