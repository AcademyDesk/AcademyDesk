using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Domain.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Controllers;

[ApiController]
[Route("api/academies")]
[Authorize]
public sealed class AcademiesController(
    AcademyDeskDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    RoleManager<ApplicationRole> roleManager) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AcademySummary>>> List(CancellationToken cancellationToken)
    {
        var user = await userManager.GetUserAsync(User);
        if (user?.AcademyId is null) return Ok(Array.Empty<AcademySummary>());

        var academies = await dbContext.Academies
            .AsNoTracking()
            .Where(x => x.Id == user.AcademyId)
            .OrderBy(x => x.Name)
            .Select(x => new AcademySummary(x.Id, x.Name, x.LegalName, x.CountryCode, x.TimeZone, x.IsActive))
            .ToListAsync(cancellationToken);

        return Ok(academies);
    }

    [HttpPost]
    public async Task<ActionResult<AcademySummary>> Create(
        CreateAcademyRequest request,
        CancellationToken cancellationToken)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null) return Unauthorized();
        if (user.AcademyId is not null)
            return Conflict(new { message = "This user is already assigned to an academy." });

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { message = "Academy name is required." });
        }

        var academy = new Academy
        {
            Name = request.Name.Trim(),
            LegalName = string.IsNullOrWhiteSpace(request.LegalName) ? null : request.LegalName.Trim(),
            CountryCode = string.IsNullOrWhiteSpace(request.CountryCode) ? "IN" : request.CountryCode.Trim().ToUpperInvariant(),
            TimeZone = string.IsNullOrWhiteSpace(request.TimeZone) ? "Asia/Kolkata" : request.TimeZone.Trim()
        };

        dbContext.Academies.Add(academy);
        await dbContext.SaveChangesAsync(cancellationToken);

        const string ownerRole = "Owner";
        if (!await roleManager.RoleExistsAsync(ownerRole))
        {
            var roleResult = await roleManager.CreateAsync(new ApplicationRole { Name = ownerRole });
            if (!roleResult.Succeeded) return Problem("The Owner role could not be created.");
        }

        user.AcademyId = academy.Id;
        user.DisplayName = academy.Name;
        await userManager.UpdateAsync(user);
        await userManager.AddToRoleAsync(user, ownerRole);

        var response = new AcademySummary(
            academy.Id,
            academy.Name,
            academy.LegalName,
            academy.CountryCode,
            academy.TimeZone,
            academy.IsActive);

        return CreatedAtAction(nameof(List), new { id = academy.Id }, response);
    }
    [HttpPut("{academyId:guid}")]
    public async Task<ActionResult<AcademySummary>> Update(Guid academyId, UpdateAcademyRequest request, CancellationToken token)
    { var user=await userManager.GetUserAsync(User); if(user?.AcademyId!=academyId)return Forbid(); var x=await dbContext.Academies.SingleOrDefaultAsync(v=>v.Id==academyId,token); if(x is null)return NotFound(); if(string.IsNullOrWhiteSpace(request.Name))return BadRequest(new{message="Academy name is required."}); x.Name=request.Name.Trim();x.LegalName=string.IsNullOrWhiteSpace(request.LegalName)?null:request.LegalName.Trim();x.CountryCode=string.IsNullOrWhiteSpace(request.CountryCode)?x.CountryCode:request.CountryCode.Trim().ToUpperInvariant();x.TimeZone=string.IsNullOrWhiteSpace(request.TimeZone)?x.TimeZone:request.TimeZone.Trim(); await dbContext.SaveChangesAsync(token); user.DisplayName=x.Name; await userManager.UpdateAsync(user); return Ok(new AcademySummary(x.Id,x.Name,x.LegalName,x.CountryCode,x.TimeZone,x.IsActive)); }
}

public sealed record CreateAcademyRequest(
    string Name,
    string? LegalName,
    string? CountryCode,
    string? TimeZone);
public sealed record UpdateAcademyRequest(string Name,string? LegalName,string? CountryCode,string? TimeZone);

public sealed record AcademySummary(
    Guid Id,
    string Name,
    string? LegalName,
    string CountryCode,
    string TimeZone,
    bool IsActive);
