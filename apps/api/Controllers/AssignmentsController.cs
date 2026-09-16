using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Controllers;

[ApiController]
[Route("api/academies/{academyId:guid}/assignments")]
public sealed class AssignmentsController(AcademyDeskDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AssignmentSummary>>> List(Guid academyId, Guid? batchId, CancellationToken cancellationToken)
    {
        var query = dbContext.Assignments.AsNoTracking().Where(x => x.AcademyId == academyId);
        if (batchId.HasValue) query = query.Where(x => x.BatchId == batchId.Value);
        var assignments = await query.OrderByDescending(x => x.DueAtUtc).Select(x => new AssignmentSummary(x.Id, x.BatchId, x.Title, x.Description, x.DueAtUtc, x.Type, x.IsPublished)).ToListAsync(cancellationToken);
        return Ok(assignments);
    }

    [HttpPost]
    public async Task<ActionResult<AssignmentSummary>> Create(Guid academyId, CreateAssignmentRequest request, CancellationToken cancellationToken)
    {
        if (!await dbContext.Batches.AnyAsync(x => x.Id == request.BatchId && x.AcademyId == academyId, cancellationToken)) return BadRequest(new { message = "The selected batch does not belong to this academy." });
        if (string.IsNullOrWhiteSpace(request.Title)) return BadRequest(new { message = "Assignment title is required." });
        var assignment = new Assignment { AcademyId = academyId, BatchId = request.BatchId, Title = request.Title.Trim(), Description = request.Description?.Trim(), DueAtUtc = request.DueAtUtc, Type = string.IsNullOrWhiteSpace(request.Type) ? "Homework" : request.Type.Trim(), IsPublished = request.IsPublished };
        dbContext.Assignments.Add(assignment); await dbContext.SaveChangesAsync(cancellationToken);
        return Created($"/api/academies/{academyId}/assignments/{assignment.Id}", new AssignmentSummary(assignment.Id, assignment.BatchId, assignment.Title, assignment.Description, assignment.DueAtUtc, assignment.Type, assignment.IsPublished));
    }
}

public sealed record CreateAssignmentRequest(Guid BatchId, string Title, string? Description, DateTime? DueAtUtc, string? Type, bool IsPublished);
public sealed record AssignmentSummary(Guid Id, Guid BatchId, string Title, string? Description, DateTime? DueAtUtc, string Type, bool IsPublished);
