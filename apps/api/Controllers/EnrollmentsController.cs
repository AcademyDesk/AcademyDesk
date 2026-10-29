using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Infrastructure;
using AcademyDesk.Api.Security;
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
    [AtomicAcademyMutation]
    public async Task<ActionResult<EnrollmentSummary>> Create(Guid academyId, CreateEnrollmentRequest request, CancellationToken cancellationToken) =>
        Respond(academyId, await new EnrollmentMutationOperations(dbContext).CreateAsync(academyId, request, cancellationToken));

    [HttpPut("{enrollmentId:guid}")]
    [AtomicAcademyMutation]
    public async Task<ActionResult<EnrollmentSummary>> Update(Guid academyId, Guid enrollmentId, UpdateEnrollmentRequest request, CancellationToken token) =>
        Respond(academyId, await new EnrollmentMutationOperations(dbContext).UpdateAsync(academyId, enrollmentId, request, token));

    [HttpPost("{enrollmentId:guid}/transfer")]
    [AtomicAcademyMutation]
    public async Task<ActionResult<EnrollmentSummary>> Transfer(Guid academyId, Guid enrollmentId, TransferEnrollmentRequest request, CancellationToken token) =>
        Respond(academyId, await new EnrollmentMutationOperations(dbContext).TransferAsync(academyId, enrollmentId, request, token));

    private ActionResult<EnrollmentSummary> Respond(Guid academyId, EnrollmentMutationOutcome outcome) => outcome.HttpStatus switch {
        201 => Created($"/api/academies/{academyId}/enrollments/{outcome.Result!.Id}", outcome.Result),
        200 => Ok(outcome.Result), 400 => BadRequest(new { message = outcome.Error }),
        409 => Conflict(new { message = outcome.Error }), 404 => NotFound(),
        _ => StatusCode(outcome.HttpStatus, new { message = outcome.Error })
    };
}

public sealed record CreateEnrollmentRequest(Guid StudentId, Guid BatchId, DateOnly? StartDate, string? Status);
public sealed record EnrollmentSummary(Guid Id, Guid StudentId, Guid BatchId, DateOnly StartDate, DateOnly? EndDate, string Status);
public sealed record UpdateEnrollmentRequest(string Status, DateOnly? EndDate, string? LifecycleReason);
public sealed record TransferEnrollmentRequest(Guid TargetBatchId, DateOnly? TransferDate);
