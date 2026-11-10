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
    // Enrollment entry needs labels, not batch-management privileges or private
    // student details. Inherit this controller's existing permission/academy gate.
    [HttpGet("options")]
    public async Task<ActionResult<EnrollmentOptionsSummary>> Options(Guid academyId, CancellationToken token)
    {
        Response.Headers.CacheControl = "no-store";
        var students = await dbContext.Students.AsNoTracking().Where(x => x.AcademyId == academyId)
            .OrderBy(x => x.LastName).ThenBy(x => x.FirstName).ThenBy(x => x.Id)
            .Select(x => new EnrollmentStudentOption(x.Id, x.FirstName, x.LastName)).ToListAsync(token);
        var batches = await dbContext.Batches.AsNoTracking().Where(x => x.AcademyId == academyId)
            .OrderBy(x => x.Name).ThenBy(x => x.Id)
            .Select(x => new EnrollmentBatchOption(x.Id, x.Name)).ToListAsync(token);
        return Ok(new EnrollmentOptionsSummary(students, batches));
    }

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
public sealed record EnrollmentStudentOption(Guid Id, string FirstName, string LastName);
public sealed record EnrollmentBatchOption(Guid Id, string Name);
public sealed record EnrollmentOptionsSummary(IReadOnlyList<EnrollmentStudentOption> Students, IReadOnlyList<EnrollmentBatchOption> Batches);
