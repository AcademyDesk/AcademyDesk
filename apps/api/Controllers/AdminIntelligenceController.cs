using AcademyDesk.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Controllers;
[ApiController]
[Route("api/academies/{academyId:guid}/admin-intelligence")]
public sealed class AdminIntelligenceController(AcademyDeskDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult> Get(Guid academyId, CancellationToken token)
    {
        var active = await db.Enrollments.Where(x => x.AcademyId == academyId && x.Status == "Active").ToListAsync(token);
        var batches = await db.Batches.Where(x => x.AcademyId == academyId).ToListAsync(token);
        var occupancy = batches.Select(batch => new { batch.Id, batch.Name, ActiveEnrolments = active.Count(x => x.BatchId == batch.Id), batch.Capacity, WaitlistCapacity = batch.WaitlistCapacity, Pressure = batch.Capacity == 0 ? 0 : Math.Round(active.Count(x => x.BatchId == batch.Id) * 100m / batch.Capacity, 1) });
        var invoices = await db.Invoices.Where(x => x.AcademyId == academyId && x.Status != "Paid" && x.DueDate < DateOnly.FromDateTime(DateTime.UtcNow)).Select(x => new { x.InvoiceNumber, x.TotalAmount, x.DueDate }).ToListAsync(token);
        var sessions = await db.ClassSessions.Where(x => x.AcademyId == academyId && x.Status == "Scheduled").ToListAsync(token);
        var workload = sessions.Where(x => x.TeacherId != null).GroupBy(x => x.TeacherId).Select(group => new { TeacherId = group.Key, ScheduledHours = Math.Round(group.Sum(x => (decimal)(x.EndUtc - x.StartUtc).TotalHours), 1), Sessions = group.Count() });
        return Ok(new { occupancy, collections = invoices.Select(x => new { x.InvoiceNumber, x.TotalAmount, x.DueDate, DaysOverdue = DateOnly.FromDateTime(DateTime.UtcNow).DayNumber - x.DueDate.DayNumber }), workload, activeEnrolments = active.Count });
    }
}
