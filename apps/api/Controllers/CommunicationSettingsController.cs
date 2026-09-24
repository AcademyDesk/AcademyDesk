using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Domain.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace AcademyDesk.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/academies/{academyId:guid}/communication-settings")]
public sealed class CommunicationSettingsController(
    AcademyDeskDbContext dbContext,
    UserManager<ApplicationUser> userManager) : ControllerBase
{
    private static readonly string[] AllowedChannels = ["Email", "WhatsApp", "Meeting"];
    private static readonly string[] AllowedStatuses = ["NotConfigured", "Configured", "Disabled"];

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CommunicationChannelSummary>>> List(Guid academyId, CancellationToken cancellationToken)
    {
        if (!await IsOwner(academyId)) return Forbid();

        var items = await dbContext.CommunicationChannels.AsNoTracking()
            .Where(x => x.AcademyId == academyId)
            .OrderBy(x => x.Channel)
            .ToListAsync(cancellationToken);
        return Ok(items.Select(ToSummary).ToList());
    }

    [HttpPut("{channel}")]
    public async Task<ActionResult<CommunicationChannelSummary>> Save(
        Guid academyId,
        string channel,
        SaveCommunicationChannelRequest request,
        CancellationToken cancellationToken)
    {
        if (!await IsOwner(academyId)) return Forbid();
        var canonicalChannel = AllowedChannels.SingleOrDefault(x => x.Equals(channel, StringComparison.OrdinalIgnoreCase));
        if (canonicalChannel is null) return BadRequest(new { message = "Channel must be Email, WhatsApp, or Meeting." });
        if (string.IsNullOrWhiteSpace(request.Provider)) return BadRequest(new { message = "Select a provider." });
        var status = AllowedStatuses.SingleOrDefault(x => x.Equals(request.Status, StringComparison.OrdinalIgnoreCase));
        if (status is null) return BadRequest(new { message = "Status is invalid." });
        if (canonicalChannel == "Email" && status == "Configured" && string.IsNullOrWhiteSpace(request.SenderAddress))
            return BadRequest(new { message = "A sender email address is required before enabling email." });
        if (canonicalChannel == "WhatsApp" && status == "Configured" && string.IsNullOrWhiteSpace(request.PhoneNumber))
            return BadRequest(new { message = "A dedicated WhatsApp number is required before enabling WhatsApp." });
        if (canonicalChannel == "Meeting" && status == "Configured" && (string.IsNullOrWhiteSpace(request.SenderAddress) || string.IsNullOrWhiteSpace(request.ExternalAccountReference)))
            return BadRequest(new { message = "An organizer email and provider application or tenant reference are required before enabling meetings." });

        var item = await dbContext.CommunicationChannels.SingleOrDefaultAsync(
            x => x.AcademyId == academyId && x.Channel == canonicalChannel, cancellationToken);
        if (item is null)
        {
            item = new CommunicationChannel { AcademyId = academyId, Channel = canonicalChannel, Provider = request.Provider.Trim() };
            dbContext.CommunicationChannels.Add(item);
        }

        item.Provider = request.Provider.Trim();
        item.Status = status;
        item.SenderName = Clean(request.SenderName);
        item.SenderAddress = Clean(request.SenderAddress);
        item.ReplyToAddress = Clean(request.ReplyToAddress);
        item.PhoneNumber = Clean(request.PhoneNumber);
        item.ExternalAccountReference = Clean(request.ExternalAccountReference);
        item.MessagesEnabled = request.MessagesEnabled && status == "Configured";
        item.UpdatedAtUtc = DateTime.UtcNow;

        dbContext.AuditLogs.Add(new AuditLog
        {
            AcademyId = academyId,
            Action = "CommunicationChannelSaved",
            EntityType = "CommunicationChannel",
            EntityId = item.Id,
            MetadataJson = JsonSerializer.Serialize(new { item.Channel, item.Provider, item.Status, item.MessagesEnabled })
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(ToSummary(item));
    }

    private async Task<bool> IsOwner(Guid academyId)
    {
        var user = await userManager.GetUserAsync(User);
        return user?.AcademyId == academyId && (await userManager.IsInRoleAsync(user, "Owner") || await userManager.IsInRoleAsync(user, "AcademyAdmin"));
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static CommunicationChannelSummary ToSummary(CommunicationChannel item) => new(item.Id, item.Channel, item.Provider, item.Status, item.SenderName, item.SenderAddress, item.ReplyToAddress, item.PhoneNumber, item.ExternalAccountReference, item.MessagesEnabled, item.HasSecureConnection);
}

public sealed record SaveCommunicationChannelRequest(string Provider, string Status, string? SenderName, string? SenderAddress, string? ReplyToAddress, string? PhoneNumber, string? ExternalAccountReference, bool MessagesEnabled);
public sealed record CommunicationChannelSummary(Guid Id, string Channel, string Provider, string Status, string? SenderName, string? SenderAddress, string? ReplyToAddress, string? PhoneNumber, string? ExternalAccountReference, bool MessagesEnabled, bool HasSecureConnection);
