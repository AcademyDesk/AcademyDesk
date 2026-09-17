using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Controllers;

[ApiController]
[Route("api/academies/{academyId:guid}/events")]
public sealed class EventsController(AcademyDeskDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<EventSummary>>> List(Guid academyId, CancellationToken token) => Ok(await dbContext.AcademyEvents.AsNoTracking().Where(x => x.AcademyId == academyId).OrderBy(x => x.StartUtc).Select(x => new EventSummary(x.Id, x.Title, x.Type, x.BranchId, x.StartUtc, x.EndUtc, x.Venue, x.Capacity, x.Status, x.Notes)).ToListAsync(token));
    [HttpPost]
    public async Task<ActionResult<EventSummary>> Create(Guid academyId, CreateEventRequest request, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(request.Title) || request.EndUtc <= request.StartUtc) return BadRequest(new { message = "Title and valid event times are required." });
        if (request.BranchId.HasValue && !await dbContext.Branches.AnyAsync(x => x.Id == request.BranchId && x.AcademyId == academyId, token)) return BadRequest(new { message = "Invalid branch." });
        var item = new AcademyEvent { AcademyId = academyId, Title = request.Title.Trim(), Type = string.IsNullOrWhiteSpace(request.Type) ? "Recital" : request.Type.Trim(), BranchId = request.BranchId, StartUtc = request.StartUtc, EndUtc = request.EndUtc, Venue = request.Venue?.Trim(), Capacity = request.Capacity, Notes = request.Notes?.Trim() };
        dbContext.AcademyEvents.Add(item); await dbContext.SaveChangesAsync(token); return Created($"/api/academies/{academyId}/events/{item.Id}", new EventSummary(item.Id, item.Title, item.Type, item.BranchId, item.StartUtc, item.EndUtc, item.Venue, item.Capacity, item.Status, item.Notes));
    }
}
public sealed record CreateEventRequest(string Title, string? Type, Guid? BranchId, DateTime StartUtc, DateTime EndUtc, string? Venue, int? Capacity, string? Notes);
public sealed record EventSummary(Guid Id, string Title, string Type, Guid? BranchId, DateTime StartUtc, DateTime EndUtc, string? Venue, int? Capacity, string Status, string? Notes);
