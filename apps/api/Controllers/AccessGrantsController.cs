using System.Text.Json;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Identity;
using AcademyDesk.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Controllers;

[ApiController, Authorize]
[Route("api/academies/{academyId:guid}/access-grants")]
public sealed class AccessGrantsController(IdentityDbContext db, UserManager<ApplicationUser> users) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult> List(Guid academyId, CancellationToken token)
    {
        if (!await IsAdmin(academyId)) return Forbid();
        var grants = await db.AccessGrants.Where(x => x.AcademyId == academyId)
            .OrderByDescending(x => x.CreatedAtUtc).ToListAsync(token);
        var userIds = grants.Select(x => x.UserId).Distinct().ToArray();
        var people = await users.Users.Where(x => userIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.DisplayName, token);
        return Ok(grants.Select(x => new
        {
            x.Id, x.UserId, userName = people.GetValueOrDefault(x.UserId, "Former user"),
            permissions = JsonSerializer.Deserialize<string[]>(x.PermissionsJson) ?? [], x.IsPermanent,
            x.ExpiresAtUtc, x.Reason, x.CreatedAtUtc, x.RevokedAtUtc
        }));
    }

    [HttpPost]
    public async Task<ActionResult> Create(Guid academyId, CreateAccessGrantRequest request, CancellationToken token)
    {
        var admin = await CurrentAdmin(academyId);
        if (admin is null) return Forbid();
        var person = await users.FindByIdAsync(request.UserId.ToString());
        if (person?.AcademyId != academyId || !person.IsActive) return NotFound(new { message = "Choose an active academy staff member." });
        var permissions = (request.Permissions ?? []).Distinct().Where(PermissionCatalog.All.Contains).ToArray();
        if (permissions.Length == 0) return BadRequest(new { message = "Choose at least one permitted function." });
        var expiry = request.ExpiresAtUtc;
        if (!request.IsPermanent && expiry is null) return BadRequest(new { message = "Choose an expiry date or select permanent access." });
        if (!request.IsPermanent && expiry <= DateTimeOffset.UtcNow) return BadRequest(new { message = "The expiry date must be in the future." });
        var grant = new AccessGrant { AcademyId = academyId, UserId = person.Id, PermissionsJson = JsonSerializer.Serialize(permissions), IsPermanent = request.IsPermanent, ExpiresAtUtc = request.IsPermanent ? null : expiry, Reason = request.Reason?.Trim(), GrantedByUserId = admin.Id };
        db.AccessGrants.Add(grant);
        await db.SaveChangesAsync(token);
        return Ok(new { grant.Id });
    }

    [HttpPatch("{grantId:guid}/revoke")]
    public async Task<ActionResult> Revoke(Guid academyId, Guid grantId, CancellationToken token)
    {
        var admin = await CurrentAdmin(academyId);
        if (admin is null) return Forbid();
        var grant = await db.AccessGrants.SingleOrDefaultAsync(x => x.Id == grantId && x.AcademyId == academyId, token);
        if (grant is null) return NotFound();
        if (grant.RevokedAtUtc is null) { grant.RevokedAtUtc = DateTimeOffset.UtcNow; grant.RevokedByUserId = admin.Id; await db.SaveChangesAsync(token); }
        return NoContent();
    }

    private async Task<ApplicationUser?> CurrentAdmin(Guid academyId)
    {
        var user = await users.GetUserAsync(User);
        return user?.AcademyId == academyId && (await users.IsInRoleAsync(user, "Owner") || await users.IsInRoleAsync(user, "AcademyAdmin")) ? user : null;
    }
    private async Task<bool> IsAdmin(Guid academyId) => await CurrentAdmin(academyId) is not null;
}

public sealed record CreateAccessGrantRequest(Guid UserId, IReadOnlyList<string>? Permissions, bool IsPermanent, DateTimeOffset? ExpiresAtUtc, string? Reason);
