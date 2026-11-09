using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Infrastructure;
using AcademyDesk.Api.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Controllers;

[ApiController]
[Route("api/academies/{academyId:guid}/sales-marketing")]
public sealed class SalesMarketingController(AcademyDeskDbContext db) : ControllerBase
{
    private static readonly string[] CampaignStatuses = ["Draft", "Active", "Paused", "Completed"];
    private static readonly string[] TrialStatuses = ["Booked", "Completed", "Cancelled", "NoShow"];

    [HttpGet("teacher-options")]
    public async Task<ActionResult> TeacherOptions(Guid academyId, CancellationToken t) =>
        Ok(await db.Teachers.AsNoTracking().Where(x => x.AcademyId == academyId)
            .OrderBy(x => x.FirstName).ThenBy(x => x.LastName)
            .Select(x => new { x.Id, x.FirstName, x.LastName }).ToListAsync(t));

    [HttpGet("campaigns")]
    public async Task<ActionResult> Campaigns(Guid academyId, CancellationToken t) =>
        Ok(await db.SalesCampaigns.AsNoTracking().Where(x => x.AcademyId == academyId).OrderByDescending(x => x.StartDate).ToListAsync(t));

    [HttpPost("campaigns")]
    public async Task<ActionResult> CreateCampaign(Guid academyId, CampaignRequest r, CancellationToken t)
    {
        if (string.IsNullOrWhiteSpace(r.Name) || r.Budget < 0 || !CampaignStatuses.Contains(r.Status ?? "Draft"))
            return BadRequest(new { message = "Provide a campaign name, valid budget and status." });
        var campaign = new SalesCampaign { AcademyId = academyId, Name = r.Name.Trim(), Channel = r.Channel?.Trim() ?? "WhatsApp", StartDate = r.StartDate, EndDate = r.EndDate, Budget = r.Budget, Status = r.Status ?? "Draft" };
        db.SalesCampaigns.Add(campaign);
        await db.SaveChangesAsync(t);
        return Ok(campaign);
    }

    [HttpPatch("campaigns/{id:guid}")]
    public async Task<ActionResult> UpdateCampaign(Guid academyId, Guid id, CampaignUpdateRequest r, CancellationToken t)
    {
        var campaign = await db.SalesCampaigns.SingleOrDefaultAsync(x => x.Id == id && x.AcademyId == academyId, t);
        if (campaign is null) return NotFound();
        if (!CampaignStatuses.Contains(r.Status) || r.Budget < 0) return BadRequest(new { message = "Invalid campaign update." });
        campaign.Status = r.Status; campaign.Budget = r.Budget; campaign.EndDate = r.EndDate; campaign.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(t);
        return Ok(campaign);
    }

    [HttpGet("trials")]
    public async Task<ActionResult> Trials(Guid academyId, CancellationToken t) =>
        Ok(await db.TrialClassBookings.AsNoTracking().Where(x => x.AcademyId == academyId).OrderBy(x => x.ScheduledAtUtc).ToListAsync(t));

    [HttpPost("trials")]
    [AtomicAcademyMutation(IncludePlatformOwner = true)]
    public async Task<ActionResult> CreateTrial(Guid academyId, TrialRequest r, CancellationToken t)
    {
        if (r.ScheduledAtUtc == default) return BadRequest(new { message = "Select a valid lead and scheduled time." });
        var lead = await LeadMutationBoundary.ReadAsync(db, academyId, r.LeadId, t);
        if (lead is null) return BadRequest(new { message = "Select a valid lead and scheduled time." });
        if (LeadMutationBoundary.IsConverted(lead)) return Conflict(new { message = "A converted lead cannot be booked into the trial pipeline." });
        if (r.BatchId.HasValue && !await db.Batches.AnyAsync(x => x.Id == r.BatchId && x.AcademyId == academyId, t)) return BadRequest(new { message = "Select a valid class or batch." });
        if (r.TeacherId.HasValue && !await db.Teachers.AnyAsync(x => x.Id == r.TeacherId && x.AcademyId == academyId, t)) return BadRequest(new { message = "Select a valid teacher." });
        var trial = new TrialClassBooking { AcademyId = academyId, LeadId = r.LeadId, BatchId = r.BatchId, TeacherId = r.TeacherId, ScheduledAtUtc = r.ScheduledAtUtc.ToUniversalTime(), Notes = r.Notes?.Trim() };
        db.TrialClassBookings.Add(trial);
        lead.Stage = "TrialBooked";
        await db.SaveChangesAsync(t);
        return Ok(trial);
    }

    [HttpPatch("trials/{id:guid}/status")]
    [AtomicAcademyMutation(IncludePlatformOwner = true)]
    public async Task<ActionResult> UpdateTrialStatus(Guid academyId, Guid id, TrialStatusRequest r, CancellationToken t)
    {
        if (!TrialStatuses.Contains(r.Status)) return BadRequest(new { message = "Invalid trial status." });
        var trial = await db.TrialClassBookings.SingleOrDefaultAsync(x => x.Id == id && x.AcademyId == academyId, t);
        if (trial is null) return NotFound();
        trial.Status = r.Status; trial.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(t);
        return Ok(trial);
    }
}

public sealed record CampaignRequest(string Name, string? Channel, DateOnly StartDate, DateOnly? EndDate, decimal Budget, string? Status);
public sealed record CampaignUpdateRequest(string Status, decimal Budget, DateOnly? EndDate);
public sealed record TrialRequest(Guid LeadId, Guid? BatchId, Guid? TeacherId, DateTime ScheduledAtUtc, string? Notes);
public sealed record TrialStatusRequest(string Status);
