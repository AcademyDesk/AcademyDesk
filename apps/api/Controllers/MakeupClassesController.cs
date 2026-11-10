using AcademyDesk.Api.Data;
using AcademyDesk.Api.Infrastructure;
using AcademyDesk.Api.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Controllers;

[ApiController]
[Route("api/academies/{academyId:guid}/makeup-classes")]
public sealed class MakeupClassesController(AcademyDeskDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult> List(Guid academyId, CancellationToken token) => Ok(await db.MakeupClasses.AsNoTracking().Where(x => x.AcademyId == academyId).OrderBy(x => x.StartUtc).Select(x => new MakeupSummary(x.Id, x.StudentId, x.BatchId, x.TeacherId, x.StartUtc, x.EndUtc, x.DeliveryMode, x.Venue, x.MeetingLink, x.UsesNextScheduledClass, x.Status, x.Notes)).ToListAsync(token));

    [HttpPost]
    [AtomicAcademyMutation]
    public async Task<ActionResult> Create(Guid academyId, CreateMakeupRequest request, CancellationToken token)
    {
        var scheduling = new MakeupSchedulingService(db, TimeProvider.System);
        var result = await scheduling.PlanAsync(academyId, request, token);
        return result.Plan is { } plan
            ? Ok(await scheduling.ScheduleAsync(academyId, plan, token))
            : BadRequest(new { message = result.Error });
    }

    [HttpPatch("{id:guid}")]
    public async Task<ActionResult> UpdateStatus(Guid academyId, Guid id, UpdateMakeupRequest request, CancellationToken token)
    { if (request.Status is not ("Scheduled" or "Completed" or "Cancelled")) return BadRequest(); var makeup = await db.MakeupClasses.SingleOrDefaultAsync(x => x.Id == id && x.AcademyId == academyId, token); if (makeup is null) return NotFound(); makeup.Status = request.Status; await db.SaveChangesAsync(token); return Ok(); }

}

public sealed record CreateMakeupRequest(Guid StudentId, Guid BatchId, Guid? TeacherId, DateTime? StartUtc, string? DeliveryMode, string? Venue, string? MeetingLink, bool UseNextScheduledClass, string? Notes);
public sealed record MakeupSummary(Guid Id, Guid StudentId, Guid BatchId, Guid? TeacherId, DateTime StartUtc, DateTime EndUtc, string DeliveryMode, string? Venue, string? MeetingLink, bool UsesNextScheduledClass, string Status, string? Notes);
public sealed record UpdateMakeupRequest(string Status);
