using AcademyDesk.Api.Data;
using AcademyDesk.Api.Infrastructure;
using AcademyDesk.Api.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Controllers;

[ApiController]
[Route("api/academies/{academyId:guid}/sessions")]
public sealed class ClassSessionsController(AcademyDeskDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ClassSessionSummary>>> List(Guid academyId, DateTime? fromUtc, DateTime? toUtc, CancellationToken cancellationToken)
    {
        var query = dbContext.ClassSessions.AsNoTracking().Where(x => x.AcademyId == academyId);
        if (fromUtc.HasValue) query = query.Where(x => x.StartUtc >= fromUtc.Value);
        if (toUtc.HasValue) query = query.Where(x => x.StartUtc < toUtc.Value);
        var sessions = await query.OrderBy(x => x.StartUtc).Select(x => new ClassSessionSummary(x.Id, x.BatchId, x.TeacherId, x.BranchId, x.StartUtc, x.EndUtc, x.DeliveryMode, x.RoomName, x.Status)).ToListAsync(cancellationToken);
        return Ok(sessions);
    }

    [HttpPost]
    [AtomicAcademyMutation(IncludePlatformOwner = true)]
    public async Task<ActionResult<ClassSessionSummary>> Create(Guid academyId, CreateClassSessionRequest request, CancellationToken cancellationToken)
    {
        var scheduling = new ClassSessionSchedulingService(dbContext);
        await scheduling.LockAsync(academyId, cancellationToken);
        var planned = await scheduling.PlanCreateAsync(academyId, request, cancellationToken);
        if (planned.Plan is null) return StatusCode(planned.Status, new { message = planned.Error });
        var session = await scheduling.SaveAsync(academyId, planned.Plan, cancellationToken, planned.FollowsBatchTeacher);
        return Created($"/api/academies/{academyId}/sessions/{session.Id}", session);
    }

    [HttpPut("{sessionId:guid}")]
    [AtomicAcademyMutation(IncludePlatformOwner = true)]
    public async Task<ActionResult<ClassSessionSummary>> Update(Guid academyId, Guid sessionId, UpdateClassSessionRequest request, CancellationToken token)
    {
        var scheduling = new ClassSessionSchedulingService(dbContext);
        await scheduling.LockAsync(academyId, token);
        var planned = await scheduling.PlanUpdateAsync(academyId, sessionId, request, token);
        if (planned.Status == 404) return NotFound();
        if (planned.Plan is null) return StatusCode(planned.Status, new { message = planned.Error });
        return Ok(await scheduling.SaveAsync(academyId, planned.Plan, token));
    }
}

public sealed record CreateClassSessionRequest(Guid BatchId, Guid? TeacherId, Guid? BranchId, DateTime StartUtc, DateTime EndUtc, string? DeliveryMode, string? RoomName);
public sealed record ClassSessionSummary(Guid Id, Guid BatchId, Guid? TeacherId, Guid? BranchId, DateTime StartUtc, DateTime EndUtc, string DeliveryMode, string? RoomName, string Status);
public sealed record UpdateClassSessionRequest(DateTime StartUtc, DateTime EndUtc, string? DeliveryMode, string? RoomName, string Status);
