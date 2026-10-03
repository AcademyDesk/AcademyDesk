using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.RegularExpressions;

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
        return Ok(await query.OrderByDescending(x => x.CreatedAtUtc).Select(x => new NotificationSummary(x.Id, x.RecipientId, x.RecipientType, x.Title, x.Message, x.Channel, x.Status, x.TemplateId, x.VariablesJson, x.FailureReason, x.ScheduledAtUtc, x.SentAtUtc)).ToListAsync(cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<NotificationSummary>> Create(Guid academyId, CreateNotificationRequest request, CancellationToken cancellationToken)
    {
        if (!await dbContext.Academies.AnyAsync(x => x.Id == academyId, cancellationToken)) return NotFound();
        var channel = string.IsNullOrWhiteSpace(request.Channel) ? "InApp" : request.Channel.Trim();
        channel = CanonicalExternalChannel(channel) ?? channel;
        var isAnnouncement = string.Equals(request.RecipientType?.Trim(), "Academy", StringComparison.OrdinalIgnoreCase) && request.IsImportant;
        if (isAnnouncement && !channel.Equals("InApp", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "Portal banner announcements must use the InApp channel." });
        var title = request.Title?.Trim(); var body = request.Message?.Trim();
        var requiresMarketingConsent = false;
        if (request.TemplateId.HasValue)
        {
            var template = await dbContext.CommunicationTemplates.SingleOrDefaultAsync(x => x.Id == request.TemplateId && x.AcademyId == academyId && x.IsActive, cancellationToken);
            // Disabled overrides IsActive, including contradictory legacy rows.
            // Draft/Approved keep the existing local policy, not provider approval.
            if (template is null || string.Equals(template.Status?.Trim(), "Disabled", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { message = "The selected template is unavailable." });
            if ((isAnnouncement || !string.IsNullOrWhiteSpace(request.Channel)) && !template.Channel.Equals(channel, StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { message = "The selected template does not match the requested channel. Choose a matching template or remove it." });
            channel = CanonicalExternalChannel(template.Channel) ?? template.Channel; title ??= template.Name; body ??= template.Body;
            requiresMarketingConsent = IsMarketing(template);
        }
        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(body)) return BadRequest(new { message = "A title and message are required." });
        var variables = request.Variables ?? new Dictionary<string, string>();
        if (isAnnouncement)
        {
            channel = "InApp";
            if (request.DisplayHours is < 1 or > 168) return BadRequest(new { message = "Important announcements must be displayed for 1 to 168 hours." });
            var startsAtUtc = request.ScheduledAtUtc?.ToUniversalTime() ?? DateTime.UtcNow;
            variables["important"] = "true";
            variables["startsAtUtc"] = startsAtUtc.ToString("O");
            variables["expiresAtUtc"] = startsAtUtc.AddHours(request.DisplayHours ?? 1).ToString("O");
        }
        body = Regex.Replace(body, "\\{\\{([a-zA-Z0-9_]+)\\}\\}", match => variables.TryGetValue(match.Groups[1].Value, out var value) ? value : match.Value);
        var status = "Queued"; string? failureReason = null;
        if (channel is "Email" or "WhatsApp")
        {
            failureReason = await ConsentFailure(academyId, request.RecipientId, request.RecipientType, channel, requiresMarketingConsent, cancellationToken);
            if (failureReason is not null) status = "BlockedConsent";
            else if (!await dbContext.CommunicationChannels.AnyAsync(x => x.AcademyId == academyId && x.Channel == channel && x.MessagesEnabled && x.HasSecureConnection, cancellationToken)) { status = "AwaitingConnection"; failureReason = $"{channel} is not securely connected for this academy."; }
        }
        var notification = new Notification { AcademyId = academyId, RecipientId = request.RecipientId, RecipientType = string.IsNullOrWhiteSpace(request.RecipientType) ? "Academy" : request.RecipientType.Trim(), Title = title, Message = body, Channel = channel, Status = status, TemplateId = request.TemplateId, VariablesJson = variables.Count == 0 ? null : JsonSerializer.Serialize(variables), FailureReason = failureReason, ScheduledAtUtc = request.ScheduledAtUtc };
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
        return Created($"/api/academies/{academyId}/notifications/{notification.Id}", new NotificationSummary(notification.Id, notification.RecipientId, notification.RecipientType, notification.Title, notification.Message, notification.Channel, notification.Status, notification.TemplateId, notification.VariablesJson, notification.FailureReason, notification.ScheduledAtUtc, notification.SentAtUtc));
    }
    [HttpPatch("{notificationId:guid}/status")]
    public async Task<ActionResult> UpdateStatus(Guid academyId, Guid notificationId, UpdateNotificationStatusRequest request, CancellationToken token)
    {
        var x = await dbContext.Notifications.SingleOrDefaultAsync(v => v.Id == notificationId && v.AcademyId == academyId, token);
        if (x is null) return NotFound();
        if (!new[] { "Queued", "Cancelled", "RetryRequested" }.Contains(request.Status, StringComparer.OrdinalIgnoreCase)) return BadRequest();
        var externalChannel = CanonicalExternalChannel(x.Channel);
        if (externalChannel is not null && !request.Status.Equals("Cancelled", StringComparison.OrdinalIgnoreCase))
        {
            CommunicationTemplate? template = null;
            if (x.TemplateId.HasValue)
            {
                template = await dbContext.CommunicationTemplates.AsNoTracking().SingleOrDefaultAsync(v => v.Id == x.TemplateId && v.AcademyId == academyId && v.IsActive, token);
                if (template is null || string.Equals(template.Status?.Trim(), "Disabled", StringComparison.OrdinalIgnoreCase))
                    return BadRequest(new { message = "The selected template is unavailable." });
            }
            // A previously queued message is not authority to override a later opt-out.
            var reason = await ConsentFailure(academyId, x.RecipientId, x.RecipientType, externalChannel, IsMarketing(template), token);
            if (reason is not null) return BadRequest(new { message = reason });
        }
        x.Status = request.Status.Trim();
        x.FailureReason = request.Status == "RetryRequested" ? null : x.FailureReason;
        await dbContext.SaveChangesAsync(token);
        return Ok();
    }

    private static string? CanonicalExternalChannel(string? channel) =>
        string.Equals(channel?.Trim(), "Email", StringComparison.OrdinalIgnoreCase) ? "Email" :
        string.Equals(channel?.Trim(), "WhatsApp", StringComparison.OrdinalIgnoreCase) ? "WhatsApp" : null;

    private static bool IsMarketing(CommunicationTemplate? template) =>
        string.Equals(template?.Category?.Trim(), "Marketing", StringComparison.OrdinalIgnoreCase);

    private async Task<string?> ConsentFailure(Guid academyId, Guid? recipientId, string? recipientType, string channel, bool requiresMarketingConsent, CancellationToken token)
    {
        var preference = recipientId.HasValue ? await dbContext.CommunicationPreferences.AsNoTracking().SingleOrDefaultAsync(x => x.AcademyId == academyId && x.RecipientId == recipientId && x.RecipientType == recipientType, token) : null;
        var channelAllowed = channel == "Email" ? preference?.EmailAllowed == true : preference?.WhatsAppAllowed == true;
        if (!channelAllowed) return $"No recorded {channel} consent for this recipient.";
        return requiresMarketingConsent && preference?.MarketingAllowed != true ? "No recorded marketing consent for this recipient." : null;
    }
}

public sealed record CreateNotificationRequest(Guid? RecipientId, string? RecipientType, string? Title, string? Message, string? Channel, DateTime? ScheduledAtUtc, Guid? TemplateId, Dictionary<string, string>? Variables, bool IsImportant = false, int? DisplayHours = null);
public sealed record NotificationSummary(Guid Id, Guid? RecipientId, string RecipientType, string Title, string Message, string Channel, string Status, Guid? TemplateId, string? VariablesJson, string? FailureReason, DateTime? ScheduledAtUtc, DateTime? SentAtUtc);
public sealed record UpdateNotificationStatusRequest(string Status);
