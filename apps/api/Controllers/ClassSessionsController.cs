using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
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
    public async Task<ActionResult<ClassSessionSummary>> Create(Guid academyId, CreateClassSessionRequest request, CancellationToken cancellationToken)
    {
        if (!await dbContext.Batches.AnyAsync(x => x.Id == request.BatchId && x.AcademyId == academyId, cancellationToken)) return BadRequest(new { message = "The selected batch does not belong to this academy." });
        if (request.EndUtc <= request.StartUtc) return BadRequest(new { message = "End time must be after start time." });
        if (request.TeacherId.HasValue && !await dbContext.Teachers.AnyAsync(x => x.Id == request.TeacherId && x.AcademyId == academyId, cancellationToken)) return BadRequest(new { message = "The selected teacher does not belong to this academy." });
        if (request.BranchId.HasValue && !await dbContext.Branches.AnyAsync(x => x.Id == request.BranchId && x.AcademyId == academyId, cancellationToken)) return BadRequest(new { message = "The selected branch does not belong to this academy." });

        var session = new ClassSession { AcademyId = academyId, BatchId = request.BatchId, TeacherId = request.TeacherId, BranchId = request.BranchId, StartUtc = request.StartUtc, EndUtc = request.EndUtc, DeliveryMode = string.IsNullOrWhiteSpace(request.DeliveryMode) ? "InPerson" : request.DeliveryMode.Trim(), RoomName = request.RoomName?.Trim() };
        dbContext.ClassSessions.Add(session); await dbContext.SaveChangesAsync(cancellationToken);
        return Created($"/api/academies/{academyId}/sessions/{session.Id}", new ClassSessionSummary(session.Id, session.BatchId, session.TeacherId, session.BranchId, session.StartUtc, session.EndUtc, session.DeliveryMode, session.RoomName, session.Status));
    }
    [HttpPut("{sessionId:guid}")]
    public async Task<ActionResult<ClassSessionSummary>> Update(Guid academyId, Guid sessionId, UpdateClassSessionRequest request, CancellationToken token)
    {
        var session = await dbContext.ClassSessions.SingleOrDefaultAsync(x => x.Id == sessionId && x.AcademyId == academyId, token);
        if (session is null) return NotFound();
        if (request.EndUtc <= request.StartUtc) return BadRequest(new { message = "End time must be after the start time." });
        if (!new[] { "Scheduled", "Completed", "Cancelled", "NoShow" }.Contains(request.Status, StringComparer.OrdinalIgnoreCase)) return BadRequest(new { message = "Invalid session status." });
        session.StartUtc = request.StartUtc; session.EndUtc = request.EndUtc; session.DeliveryMode = request.DeliveryMode?.Trim() ?? "InPerson"; session.RoomName = request.RoomName?.Trim(); session.Status = request.Status.Trim();
        await dbContext.SaveChangesAsync(token);
        return Ok(new ClassSessionSummary(session.Id, session.BatchId, session.TeacherId, session.BranchId, session.StartUtc, session.EndUtc, session.DeliveryMode, session.RoomName, session.Status));
    }
}

public sealed record CreateClassSessionRequest(Guid BatchId, Guid? TeacherId, Guid? BranchId, DateTime StartUtc, DateTime EndUtc, string? DeliveryMode, string? RoomName);
public sealed record ClassSessionSummary(Guid Id, Guid BatchId, Guid? TeacherId, Guid? BranchId, DateTime StartUtc, DateTime EndUtc, string DeliveryMode, string? RoomName, string Status);
public sealed record UpdateClassSessionRequest(DateTime StartUtc, DateTime EndUtc, string? DeliveryMode, string? RoomName, string Status);
