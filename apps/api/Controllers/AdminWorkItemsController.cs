using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Controllers;

[ApiController]
[Route("api/academies/{academyId:guid}/admin-work-items")]
public sealed class AdminWorkItemsController(AcademyDeskDbContext db, IdentityDbContext identity) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult> List(Guid academyId, string? status, string? type, CancellationToken t)
    {
        var q = db.AdminWorkItems.AsNoTracking().Where(x => x.AcademyId == academyId);
        if (!string.IsNullOrWhiteSpace(status)) q = q.Where(x => x.Status == status);
        if (!string.IsNullOrWhiteSpace(type)) q = q.Where(x => x.Type == type);
        return Ok(await q.OrderBy(x => x.DueAtUtc).ThenByDescending(x => x.Priority).ToListAsync(t));
    }

    [HttpPost]
    [AtomicAcademyMutation]
    public async Task<ActionResult> Create(Guid academyId, CreateWorkItem r, CancellationToken t)
    {
        if (string.IsNullOrWhiteSpace(r.Type) || string.IsNullOrWhiteSpace(r.Title)) return BadRequest();
        if (r.Type.Trim().Length > 60 || r.Title.Trim().Length > 240 || r.Description?.Trim().Length > 4000)
            return BadRequest(new { message = "Work item text exceeds its allowed length." });
        if (!WorkItemValidation.ValidPriority(r.Priority)) return BadRequest(new { message = "Invalid priority." });
        var entityType = string.IsNullOrWhiteSpace(r.EntityType) ? null : r.EntityType.Trim();
        if (!await WorkItemValidation.ValidEntity(db, academyId, entityType, r.EntityId, t))
            return BadRequest(new { message = "Choose a valid linked record in this academy." });
        if (!await WorkItemValidation.ValidAssignee(identity, academyId, r.AssignedUserId, t))
            return BadRequest(new { message = "Choose an active staff assignee in this academy." });
        var item = new AdminWorkItem
        {
            AcademyId = academyId, Type = r.Type.Trim(), Title = r.Title.Trim(), Description = r.Description?.Trim(),
            Priority = r.Priority ?? "Normal", EntityType = entityType, EntityId = r.EntityId,
            AssignedUserId = r.AssignedUserId, DueAtUtc = r.DueAtUtc
        };
        db.AdminWorkItems.Add(item); await db.SaveChangesAsync(t); return Ok(item);
    }

    [HttpPatch("{id:guid}/status")]
    [AtomicAcademyMutation]
    public async Task<ActionResult> Status(Guid academyId, Guid id, WorkStatus r, CancellationToken t)
    {
        var item = await db.AdminWorkItems.SingleOrDefaultAsync(x => x.AcademyId == academyId && x.Id == id, t);
        if (item is null) return NotFound();
        if (r.Status is not ("Open" or "InProgress" or "Completed" or "Cancelled")) return BadRequest();
        item.Status = r.Status; item.CompletedAtUtc = r.Status == "Completed" ? DateTime.UtcNow : null;
        await db.SaveChangesAsync(t); return Ok(item);
    }

    [HttpPatch("{id:guid}/collections")]
    [AtomicAcademyMutation]
    public async Task<ActionResult> CollectionState(Guid academyId, Guid id, CollectionStateRequest r, CancellationToken t)
    {
        var item = await db.AdminWorkItems.SingleOrDefaultAsync(x => x.AcademyId == academyId && x.Id == id && x.Type == "Collections", t);
        if (item is null) return NotFound();
        if (r.EscalationStage is not ("Initial" or "Reminder" or "ManagerReview" or "FinalNotice")) return BadRequest();
        item.EscalationStage = r.EscalationStage; item.PromisedPaymentDate = r.PromisedPaymentDate;
        await db.SaveChangesAsync(t); return Ok(item);
    }
}

public sealed record CreateWorkItem(string Type, string Title, string? Description, string? Priority, string? EntityType, Guid? EntityId, Guid? AssignedUserId, DateTime? DueAtUtc);
public sealed record WorkStatus(string Status);
public sealed record CollectionStateRequest(string EscalationStage, DateOnly? PromisedPaymentDate);
