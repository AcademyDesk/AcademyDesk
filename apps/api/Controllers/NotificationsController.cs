using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace AcademyDesk.Api.Controllers;

[ApiController]
[Route("api/academies/{academyId:guid}/notifications")]
public sealed class NotificationsController(AcademyDeskDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<NotificationSummary>>> List(Guid academyId, Guid? recipientId, CancellationToken cancellationToken)
    {
        var query = dbContext.Notifications.AsNoTracking().Where(x => x.AcademyId == academyId);
        if (recipientId.HasValue) query = query.Where(x => x.RecipientId == recipientId.Value);
        return Ok(await query.OrderByDescending(x => x.CreatedAtUtc).Select(x => new NotificationSummary(x.Id, x.RecipientId, x.RecipientType, x.Title, x.Message, x.Channel, x.Status, x.ScheduledAtUtc, x.SentAtUtc)).ToListAsync(cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<NotificationSummary>> Create(Guid academyId, CreateNotificationRequest request, CancellationToken cancellationToken)
    {
        if (!await dbContext.Academies.AnyAsync(x => x.Id == academyId, cancellationToken)) return NotFound();
        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Message)) return BadRequest(new { message = "Title and message are required." });
        var notification = new Notification { AcademyId = academyId, RecipientId = request.RecipientId, RecipientType = string.IsNullOrWhiteSpace(request.RecipientType) ? "Academy" : request.RecipientType.Trim(), Title = request.Title.Trim(), Message = request.Message.Trim(), Channel = string.IsNullOrWhiteSpace(request.Channel) ? "InApp" : request.Channel.Trim(), ScheduledAtUtc = request.ScheduledAtUtc };
        dbContext.Notifications.Add(notification);
        dbContext.AuditLogs.Add(new AuditLog
        {
            AcademyId = academyId,
            Action = "NotificationQueued",
            EntityType = "Notification",
            EntityId = notification.Id,
            MetadataJson = JsonSerializer.Serialize(new { notification.Title, notification.Channel, notification.RecipientType })
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        return Created($"/api/academies/{academyId}/notifications/{notification.Id}", new NotificationSummary(notification.Id, notification.RecipientId, notification.RecipientType, notification.Title, notification.Message, notification.Channel, notification.Status, notification.ScheduledAtUtc, notification.SentAtUtc));
    }
}

public sealed record CreateNotificationRequest(Guid? RecipientId, string? RecipientType, string Title, string Message, string? Channel, DateTime? ScheduledAtUtc);
public sealed record NotificationSummary(Guid Id, Guid? RecipientId, string RecipientType, string Title, string Message, string Channel, string Status, DateTime? ScheduledAtUtc, DateTime? SentAtUtc);
