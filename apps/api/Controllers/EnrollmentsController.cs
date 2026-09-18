using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Controllers;

[ApiController]
[Route("api/academies/{academyId:guid}/enrollments")]
public sealed class EnrollmentsController(AcademyDeskDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<EnrollmentSummary>>> List(Guid academyId, CancellationToken cancellationToken)
    {
        var enrollments = await dbContext.Enrollments.AsNoTracking().Where(x => x.AcademyId == academyId).OrderByDescending(x => x.StartDate).Select(x => new EnrollmentSummary(x.Id, x.StudentId, x.BatchId, x.StartDate, x.EndDate, x.Status)).ToListAsync(cancellationToken);
        return Ok(enrollments);
    }

    [HttpPost]
    public async Task<ActionResult<EnrollmentSummary>> Create(Guid academyId, CreateEnrollmentRequest request, CancellationToken cancellationToken)
    {
        if (!await dbContext.Students.AnyAsync(x => x.Id == request.StudentId && x.AcademyId == academyId, cancellationToken)) return BadRequest(new { message = "The selected student does not belong to this academy." });
        var batch = await dbContext.Batches.SingleOrDefaultAsync(x => x.Id == request.BatchId && x.AcademyId == academyId, cancellationToken);
        if (batch is null) return BadRequest(new { message = "The selected batch does not belong to this academy." });
        var prerequisites = await dbContext.CoursePrerequisites.Where(x => x.AcademyId == academyId && x.CourseId == batch.CourseId && x.MustBeCompleted).Select(x => x.RequiredCourseId).ToListAsync(cancellationToken);
        if (prerequisites.Count > 0)
        {
            var completedCourseIds = await (from priorEnrollment in dbContext.Enrollments join completedBatch in dbContext.Batches on priorEnrollment.BatchId equals completedBatch.Id where priorEnrollment.AcademyId == academyId && priorEnrollment.StudentId == request.StudentId && priorEnrollment.Status == "Completed" select completedBatch.CourseId).ToListAsync(cancellationToken);
            if (prerequisites.Any(prerequisite => !completedCourseIds.Contains(prerequisite))) return Conflict(new { message = "The learner has not completed all course prerequisites for this batch." });
        }
        if (!batch.IsActive || !string.Equals(batch.EnrollmentStatus, "Open", StringComparison.OrdinalIgnoreCase)) return BadRequest(new { message = "This batch is not open for enrolment." });
        var requestedStatus = string.Equals(request.Status, "Waitlisted", StringComparison.OrdinalIgnoreCase) ? "Waitlisted" : "Active";
        var activeCount = await dbContext.Enrollments.CountAsync(x => x.AcademyId == academyId && x.BatchId == request.BatchId && x.Status == "Active", cancellationToken);
        var waitlistCount = await dbContext.Enrollments.CountAsync(x => x.AcademyId == academyId && x.BatchId == request.BatchId && x.Status == "Waitlisted", cancellationToken);
        if (requestedStatus == "Active" && activeCount >= batch.Capacity) return Conflict(new { message = "This batch has reached its active enrolment capacity. Add the learner to the waitlist or select another batch." });
        if (requestedStatus == "Waitlisted" && (batch.WaitlistCapacity <= 0 || waitlistCount >= batch.WaitlistCapacity)) return Conflict(new { message = "This batch waitlist is full or not enabled." });
        if (await dbContext.Enrollments.AnyAsync(x => x.AcademyId == academyId && x.StudentId == request.StudentId && x.BatchId == request.BatchId && x.Status == "Active", cancellationToken)) return Conflict(new { message = "The student is already enrolled in this batch." });
        var enrollment = new Enrollment { AcademyId = academyId, StudentId = request.StudentId, BatchId = request.BatchId, StartDate = request.StartDate ?? DateOnly.FromDateTime(DateTime.UtcNow), Status = requestedStatus };
        dbContext.Enrollments.Add(enrollment); await dbContext.SaveChangesAsync(cancellationToken);
        return Created($"/api/academies/{academyId}/enrollments/{enrollment.Id}", new EnrollmentSummary(enrollment.Id, enrollment.StudentId, enrollment.BatchId, enrollment.StartDate, enrollment.EndDate, enrollment.Status));
    }
    [HttpPut("{enrollmentId:guid}")]
    public async Task<ActionResult<EnrollmentSummary>> Update(Guid academyId, Guid enrollmentId, UpdateEnrollmentRequest request, CancellationToken token)
    {
        var enrollment = await dbContext.Enrollments.SingleOrDefaultAsync(x => x.Id == enrollmentId && x.AcademyId == academyId, token);
        if (enrollment is null) return NotFound();
        var status = string.IsNullOrWhiteSpace(request.Status) ? enrollment.Status : request.Status.Trim();
        if (!new[] { "Active", "Waitlisted", "Completed", "Withdrawn", "Cancelled", "Paused" }.Contains(status, StringComparer.OrdinalIgnoreCase)) return BadRequest(new { message = "Status must be Active, Waitlisted, Completed, Withdrawn, Cancelled, or Paused." });
        enrollment.Status = status; enrollment.EndDate = request.EndDate;
        await dbContext.SaveChangesAsync(token);
        return Ok(new EnrollmentSummary(enrollment.Id, enrollment.StudentId, enrollment.BatchId, enrollment.StartDate, enrollment.EndDate, enrollment.Status));
    }

    [HttpPost("{enrollmentId:guid}/transfer")]
    public async Task<ActionResult<EnrollmentSummary>> Transfer(Guid academyId, Guid enrollmentId, TransferEnrollmentRequest request, CancellationToken token)
    {
        var source = await dbContext.Enrollments.SingleOrDefaultAsync(x => x.Id == enrollmentId && x.AcademyId == academyId, token);
        var target = await dbContext.Batches.SingleOrDefaultAsync(x => x.Id == request.TargetBatchId && x.AcademyId == academyId, token);
        if (source is null || target is null) return NotFound();
        if (source.Status != "Active" || !target.IsActive || !string.Equals(target.EnrollmentStatus, "Open", StringComparison.OrdinalIgnoreCase)) return Conflict(new { message = "Only active enrolments can transfer to an open batch." });
        if (await dbContext.Enrollments.CountAsync(x => x.AcademyId == academyId && x.BatchId == target.Id && x.Status == "Active", token) >= target.Capacity) return Conflict(new { message = "Target batch is at capacity." });
        source.Status = "Transferred"; source.EndDate = request.TransferDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var replacement = new Enrollment { AcademyId = academyId, StudentId = source.StudentId, BatchId = target.Id, StartDate = source.EndDate.Value, Status = "Active" };
        dbContext.Enrollments.Add(replacement); await dbContext.SaveChangesAsync(token);
        return Ok(new EnrollmentSummary(replacement.Id, replacement.StudentId, replacement.BatchId, replacement.StartDate, replacement.EndDate, replacement.Status));
    }
}

public sealed record CreateEnrollmentRequest(Guid StudentId, Guid BatchId, DateOnly? StartDate, string? Status);
public sealed record EnrollmentSummary(Guid Id, Guid StudentId, Guid BatchId, DateOnly StartDate, DateOnly? EndDate, string Status);
public sealed record UpdateEnrollmentRequest(string Status, DateOnly? EndDate);
public sealed record TransferEnrollmentRequest(Guid TargetBatchId, DateOnly? TransferDate);
