using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Controllers;

[ApiController]
[Route("api/academies/{academyId:guid}/batches")]
public sealed class BatchesController(AcademyDeskDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<BatchSummary>>> List(Guid academyId, CancellationToken cancellationToken)
    {
        var batches = await dbContext.Batches.AsNoTracking().Where(x => x.AcademyId == academyId && x.IsActive).OrderBy(x => x.Name).Select(x => new BatchSummary(x.Id, x.Name, x.CourseId, x.TeacherId, x.BranchId, x.Capacity, x.StartDate, x.EndDate, x.IsActive)).ToListAsync(cancellationToken);
        return Ok(batches);
    }

    [HttpPost]
    public async Task<ActionResult<BatchSummary>> Create(Guid academyId, CreateBatchRequest request, CancellationToken cancellationToken)
    {
        if (!await dbContext.Academies.AnyAsync(x => x.Id == academyId, cancellationToken)) return NotFound();
        if (string.IsNullOrWhiteSpace(request.Name)) return BadRequest(new { message = "Batch name is required." });
        if (!await dbContext.Courses.AnyAsync(x => x.Id == request.CourseId && x.AcademyId == academyId, cancellationToken)) return BadRequest(new { message = "The selected course does not belong to this academy." });
        if (request.BranchId.HasValue && !await dbContext.Branches.AnyAsync(x => x.Id == request.BranchId && x.AcademyId == academyId, cancellationToken)) return BadRequest(new { message = "The selected branch does not belong to this academy." });
        if (request.TeacherId.HasValue && !await dbContext.Teachers.AnyAsync(x => x.Id == request.TeacherId && x.AcademyId == academyId, cancellationToken)) return BadRequest(new { message = "The selected teacher does not belong to this academy." });
        if (request.Capacity is < 1 or > 1000) return BadRequest(new { message = "Capacity must be between 1 and 1000." });

        var batch = new Batch { AcademyId = academyId, Name = request.Name.Trim(), CourseId = request.CourseId, TeacherId = request.TeacherId, BranchId = request.BranchId, Capacity = request.Capacity, StartDate = request.StartDate, EndDate = request.EndDate };
        dbContext.Batches.Add(batch); await dbContext.SaveChangesAsync(cancellationToken);
        return Created($"/api/academies/{academyId}/batches/{batch.Id}", new BatchSummary(batch.Id, batch.Name, batch.CourseId, batch.TeacherId, batch.BranchId, batch.Capacity, batch.StartDate, batch.EndDate, batch.IsActive));
    }
}

public sealed record CreateBatchRequest(string Name, Guid CourseId, Guid? TeacherId, Guid? BranchId, int Capacity, DateOnly? StartDate, DateOnly? EndDate);
public sealed record BatchSummary(Guid Id, string Name, Guid CourseId, Guid? TeacherId, Guid? BranchId, int Capacity, DateOnly? StartDate, DateOnly? EndDate, bool IsActive);
