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
    [HttpGet("options")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<ActionResult<ScheduleOptions>> Options(Guid academyId, CancellationToken cancellationToken)
    {
        var batches = await dbContext.Batches.AsNoTracking().Where(x => x.AcademyId == academyId)
            .OrderBy(x => x.Name).ThenBy(x => x.Id)
            .Select(x => new ScheduleBatchOption(x.Id, x.Name,
                dbContext.Teachers.Any(t => t.AcademyId == academyId && t.Id == x.TeacherId) ? x.TeacherId : null,
                dbContext.Branches.Any(b => b.AcademyId == academyId && b.Id == x.BranchId) ? x.BranchId : null))
            .ToListAsync(cancellationToken);
        var teachers = await dbContext.Teachers.AsNoTracking().Where(x => x.AcademyId == academyId)
            .OrderBy(x => x.FirstName).ThenBy(x => x.LastName).ThenBy(x => x.Id)
            .Select(x => new ScheduleTeacherOption(x.Id, x.FirstName, x.LastName)).ToListAsync(cancellationToken);
        var branches = await dbContext.Branches.AsNoTracking().Where(x => x.AcademyId == academyId)
            .OrderBy(x => x.Name).ThenBy(x => x.Id)
            .Select(x => new ScheduleBranchOption(x.Id, x.Name)).ToListAsync(cancellationToken);
        return Ok(new ScheduleOptions(batches, teachers, branches));
    }

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
public sealed record ScheduleBatchOption(Guid Id, string Name, Guid? TeacherId, Guid? BranchId);
public sealed record ScheduleTeacherOption(Guid Id, string FirstName, string LastName);
public sealed record ScheduleBranchOption(Guid Id, string Name);
public sealed record ScheduleOptions(IReadOnlyList<ScheduleBatchOption> Batches, IReadOnlyList<ScheduleTeacherOption> Teachers, IReadOnlyList<ScheduleBranchOption> Branches);
