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
[Route("api/platform/academies")]
public sealed class PlatformAcademiesController(AcademyDeskDbContext db, UserManager<ApplicationUser> users, RoleManager<ApplicationRole> roles) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult> List(CancellationToken token)
    {
        if (!await IsPlatformOwner()) return Forbid();
        return Ok(await db.Academies.AsNoTracking().OrderBy(x => x.Name).Select(x => new { x.Id, x.Name, x.LegalName, x.CountryCode, x.TimeZone, x.IsActive, x.CreatedAtUtc, Branches = db.Branches.Count(b => b.AcademyId == x.Id), Students = db.Students.Count(s => s.AcademyId == x.Id && s.IsActive) }).ToListAsync(token));
    }

    [HttpPost]
    public async Task<ActionResult> Onboard(OnboardAcademyRequest request, CancellationToken token)
    {
        if (!await IsPlatformOwner()) return Forbid();
        if (string.IsNullOrWhiteSpace(request.AcademyName) || string.IsNullOrWhiteSpace(request.AdminUserName) || string.IsNullOrWhiteSpace(request.Password)) return BadRequest(new { message = "Academy name, admin user name, and temporary password are required." });
        var requestedLogin = request.AdminUserName.Trim();
        var requestedEmail = requestedLogin.Contains('@') ? requestedLogin : $"{requestedLogin}@academydesk.local";
        if (await users.FindByEmailAsync(requestedEmail) is not null) return Conflict(new { message = "That academy admin user name already exists." });
        if (!await roles.RoleExistsAsync("AcademyAdmin")) await roles.CreateAsync(new ApplicationRole { Name = "AcademyAdmin" });
        var academy = new Academy { Name = request.AcademyName.Trim(), LegalName = request.LegalName?.Trim(), CountryCode = string.IsNullOrWhiteSpace(request.CountryCode) ? "IN" : request.CountryCode.Trim().ToUpperInvariant(), TimeZone = string.IsNullOrWhiteSpace(request.TimeZone) ? "Asia/Kolkata" : request.TimeZone.Trim() };
        db.Academies.Add(academy); await db.SaveChangesAsync(token);
        var userName = request.AdminUserName.Trim(); var email = userName.Contains('@') ? userName : $"{userName}@academydesk.local";
        var admin = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true, DisplayName = string.IsNullOrWhiteSpace(request.AdminDisplayName) ? userName : request.AdminDisplayName.Trim(), AcademyId = academy.Id };
        var result = await users.CreateAsync(admin, request.Password);
        if (!result.Succeeded) { db.Academies.Remove(academy); await db.SaveChangesAsync(token); return BadRequest(new { message = string.Join(" ", result.Errors.Select(x => x.Description)) }); }
        await users.AddToRoleAsync(admin, "AcademyAdmin");
        return Created($"/api/platform/academies/{academy.Id}", new { academy.Id, academy.Name, admin.UserName, admin.DisplayName });
    }

    [HttpPatch("{academyId:guid}/status")]
    public async Task<ActionResult> SetStatus(Guid academyId, SetPlatformAcademyStatusRequest request, CancellationToken token)
    {
        if (!await IsPlatformOwner()) return Forbid();
        var academy = await db.Academies.SingleOrDefaultAsync(x => x.Id == academyId, token);
        if (academy is null) return NotFound();
        academy.IsActive = request.IsActive;
        await db.SaveChangesAsync(token);
        return Ok(new { academy.Id, academy.IsActive });
    }

    private async Task<bool> IsPlatformOwner() { var user = await users.GetUserAsync(User); return user?.IsPlatformOwner == true; }
}

public sealed record OnboardAcademyRequest(string AcademyName, string? LegalName, string? CountryCode, string? TimeZone, string AdminUserName, string? AdminDisplayName, string Password);
public sealed record SetPlatformAcademyStatusRequest(bool IsActive);
