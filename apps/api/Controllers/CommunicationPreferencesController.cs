using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Domain.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/academies/{academyId:guid}/communication-preferences")]
public sealed class CommunicationPreferencesController(
    AcademyDeskDbContext dbContext,
    UserManager<ApplicationUser> userManager) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CommunicationPreferenceSummary>>> List(Guid academyId, string? recipientType, CancellationToken cancellationToken)
    {
        if (!await CanManage(academyId)) return Forbid();
        var query = dbContext.CommunicationPreferences.AsNoTracking().Where(x => x.AcademyId == academyId);
        if (!string.IsNullOrWhiteSpace(recipientType)) query = query.Where(x => x.RecipientType == recipientType);
        return Ok(await query.OrderByDescending(x => x.UpdatedAtUtc ?? x.CreatedAtUtc).Select(x => new CommunicationPreferenceSummary(x.Id, x.RecipientId, x.RecipientType, x.EmailAllowed, x.WhatsAppAllowed, x.MarketingAllowed, x.EmailOptedInAtUtc, x.WhatsAppOptedInAtUtc, x.OptedOutAtUtc, x.Notes)).ToListAsync(cancellationToken));
    }

    [HttpPut("{recipientType}/{recipientId:guid}")]
    public async Task<ActionResult<CommunicationPreferenceSummary>> Save(Guid academyId, string recipientType, Guid recipientId, SaveCommunicationPreferenceRequest request, CancellationToken cancellationToken)
    {
        if (!await CanManage(academyId)) return Forbid();
        var canonicalType = recipientType.Equals("Student", StringComparison.OrdinalIgnoreCase) ? "Student" : recipientType.Equals("Guardian", StringComparison.OrdinalIgnoreCase) ? "Guardian" : null;
        if (canonicalType is null) return BadRequest(new { message = "Preferences currently support Student and Guardian recipients." });
        var exists = canonicalType == "Student" ? await dbContext.Students.AnyAsync(x => x.Id == recipientId && x.AcademyId == academyId, cancellationToken) : await dbContext.Guardians.AnyAsync(x => x.Id == recipientId && x.AcademyId == academyId, cancellationToken);
        if (!exists) return NotFound();
        var preference = await dbContext.CommunicationPreferences.SingleOrDefaultAsync(x => x.AcademyId == academyId && x.RecipientType == canonicalType && x.RecipientId == recipientId, cancellationToken);
        var now = DateTime.UtcNow;
        if (preference is null) { preference = new CommunicationPreference { AcademyId = academyId, RecipientType = canonicalType, RecipientId = recipientId }; dbContext.CommunicationPreferences.Add(preference); }
        preference.EmailAllowed = request.EmailAllowed; preference.WhatsAppAllowed = request.WhatsAppAllowed; preference.MarketingAllowed = request.MarketingAllowed; preference.EmailOptedInAtUtc = request.EmailAllowed ? preference.EmailOptedInAtUtc ?? now : null; preference.WhatsAppOptedInAtUtc = request.WhatsAppAllowed ? preference.WhatsAppOptedInAtUtc ?? now : null; preference.OptedOutAtUtc = !request.EmailAllowed && !request.WhatsAppAllowed ? now : null; preference.Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(); preference.UpdatedAtUtc = now;
        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(ToSummary(preference));
    }

    private async Task<bool> CanManage(Guid academyId) { var user = await userManager.GetUserAsync(User); return user?.AcademyId == academyId && (await userManager.IsInRoleAsync(user, "Owner") || await userManager.IsInRoleAsync(user, "AcademyAdmin") || await userManager.IsInRoleAsync(user, "Manager")); }
    private static CommunicationPreferenceSummary ToSummary(CommunicationPreference x) => new(x.Id, x.RecipientId, x.RecipientType, x.EmailAllowed, x.WhatsAppAllowed, x.MarketingAllowed, x.EmailOptedInAtUtc, x.WhatsAppOptedInAtUtc, x.OptedOutAtUtc, x.Notes);
}

public sealed record SaveCommunicationPreferenceRequest(bool EmailAllowed, bool WhatsAppAllowed, bool MarketingAllowed, string? Notes);
public sealed record CommunicationPreferenceSummary(Guid Id, Guid RecipientId, string RecipientType, bool EmailAllowed, bool WhatsAppAllowed, bool MarketingAllowed, DateTime? EmailOptedInAtUtc, DateTime? WhatsAppOptedInAtUtc, DateTime? OptedOutAtUtc, string? Notes);
