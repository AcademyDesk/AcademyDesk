using System.Text.Json;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Domain.Identity;
using AcademyDesk.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Controllers;

[ApiController, Authorize]
[Route("api/academies/{academyId:guid}/access-reviews")]
public sealed class AccessReviewsController(AcademyDeskDbContext db, UserManager<ApplicationUser> users) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult> List(Guid academyId, CancellationToken t) =>
        Ok(await db.AccessReviews.AsNoTracking().Where(x => x.AcademyId == academyId).OrderByDescending(x => x.ReviewedAtUtc).ToListAsync(t));

    [HttpPost]
    [AtomicAcademyMutation(IncludePlatformOwner = true)]
    public async Task<ActionResult> SignOff(Guid academyId, AccessReviewRequest r, CancellationToken t)
    {
        // The global academy filter retains role/module authorization. Resolve
        // provenance from the existing Identity service, never a request actor.
        if (!Guid.TryParse(users.GetUserId(User), out _)) return Unauthorized();
        var reviewer = await users.GetUserAsync(User);
        if (reviewer is null) return Unauthorized();
        if (!reviewer.IsActive || reviewer.LockoutEnabled && reviewer.LockoutEnd > DateTimeOffset.UtcNow ||
            reviewer.AcademyId != academyId && !reviewer.IsPlatformOwner) return Forbid();

        var notes = r.Notes?.Trim();
        if (r.CreateFollowUp && notes?.Length > 4000)
            return BadRequest(new { message = "Follow-up review notes must be 4,000 characters or fewer." });

        var review = new AccessReview { AcademyId = academyId, Notes = notes, ReviewerUserId = reviewer.Id };
        db.AccessReviews.Add(review);
        AdminWorkItem? followUp = null;
        if (r.CreateFollowUp)
        {
            followUp = new AdminWorkItem
            {
                AcademyId = academyId, Type = "AccessReview", Title = "Resolve access review actions",
                Description = notes, Priority = "High", EntityType = "AccessReview", EntityId = review.Id
            };
            db.AdminWorkItems.Add(followUp);
        }
        db.AuditLogs.Add(new AuditLog
        {
            AcademyId = academyId, ActorUserId = reviewer.Id, Action = "AccessReviewSignedOff",
            EntityType = "AccessReview", EntityId = review.Id,
            MetadataJson = JsonSerializer.Serialize(new { ReviewId = review.Id, FollowUpId = followUp?.Id }),
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
        });
        // The action-level boundary also includes the filter's subsequent
        // generic audit. Failure there rolls back this review, task and event.
        await db.SaveChangesAsync(t);
        return Ok(new { review.Id, review.ReviewedAtUtc, review.ReviewerUserId, FollowUpId = followUp?.Id });
    }
}

public sealed record AccessReviewRequest(string? Notes, bool CreateFollowUp);
