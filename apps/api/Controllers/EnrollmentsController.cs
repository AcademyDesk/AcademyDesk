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
        if (!await dbContext.Batches.AnyAsync(x => x.Id == request.BatchId && x.AcademyId == academyId, cancellationToken)) return BadRequest(new { message = "The selected batch does not belong to this academy." });
        if (await dbContext.Enrollments.AnyAsync(x => x.AcademyId == academyId && x.StudentId == request.StudentId && x.BatchId == request.BatchId && x.Status == "Active", cancellationToken)) return Conflict(new { message = "The student is already enrolled in this batch." });
        var enrollment = new Enrollment { AcademyId = academyId, StudentId = request.StudentId, BatchId = request.BatchId, StartDate = request.StartDate ?? DateOnly.FromDateTime(DateTime.UtcNow) };
        dbContext.Enrollments.Add(enrollment); await dbContext.SaveChangesAsync(cancellationToken);
        return Created($"/api/academies/{academyId}/enrollments/{enrollment.Id}", new EnrollmentSummary(enrollment.Id, enrollment.StudentId, enrollment.BatchId, enrollment.StartDate, enrollment.EndDate, enrollment.Status));
    }
    [HttpPut("{enrollmentId:guid}")]
    public async Task<ActionResult<EnrollmentSummary>> Update(Guid academyId, Guid enrollmentId, UpdateEnrollmentRequest request, CancellationToken token)
    {
        var enrollment = await dbContext.Enrollments.SingleOrDefaultAsync(x => x.Id == enrollmentId && x.AcademyId == academyId, token);
        if (enrollment is null) return NotFound();
        var status = string.IsNullOrWhiteSpace(request.Status) ? enrollment.Status : request.Status.Trim();
        if (!new[] { "Active", "Completed", "Cancelled", "Paused" }.Contains(status, StringComparer.OrdinalIgnoreCase)) return BadRequest(new { message = "Status must be Active, Completed, Cancelled, or Paused." });
        enrollment.Status = status; enrollment.EndDate = request.EndDate;
        await dbContext.SaveChangesAsync(token);
        return Ok(new EnrollmentSummary(enrollment.Id, enrollment.StudentId, enrollment.BatchId, enrollment.StartDate, enrollment.EndDate, enrollment.Status));
    }
}

public sealed record CreateEnrollmentRequest(Guid StudentId, Guid BatchId, DateOnly? StartDate);
public sealed record EnrollmentSummary(Guid Id, Guid StudentId, Guid BatchId, DateOnly StartDate, DateOnly? EndDate, string Status);
public sealed record UpdateEnrollmentRequest(string Status, DateOnly? EndDate);
