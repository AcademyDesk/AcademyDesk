using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Domain.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AcademyDesk.Api.Security;

namespace AcademyDesk.Api.Controllers;

[ApiController]
[Route("api/academies")]
[Authorize]
public sealed class AcademiesController(
    AcademyDeskDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    RoleManager<ApplicationRole> roleManager,
    IdentityDbContext identityDb) : ControllerBase
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
            .Select(x => new AcademySummary(x.Id, x.Name, x.LegalName, x.CountryCode, x.TimeZone, x.IsActive, x.SubscriptionPlan, x.SubscriptionStatus, x.EnabledModulesJson))
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

        // This route has no academyId: own the same-store academy/Identity boundary.
        // The existing user's optimistic concurrency stamp also prevents two
        // overlapping requests from committing separate academies for one account.
        var identityConnection = AcademyIdentityTransaction.Validate(dbContext, identityDb);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await using var identityTransaction = await AcademyIdentityTransaction.EnlistAsync(dbContext, identityDb, transaction, identityConnection, cancellationToken);

        var academy = new Academy
        {
            Name = request.Name.Trim(),
            LegalName = string.IsNullOrWhiteSpace(request.LegalName) ? null : request.LegalName.Trim(),
            CountryCode = string.IsNullOrWhiteSpace(request.CountryCode) ? "IN" : request.CountryCode.Trim().ToUpperInvariant(),
            TimeZone = "Asia/Kolkata"
        };

        dbContext.Academies.Add(academy);
        await dbContext.SaveChangesAsync(cancellationToken);

        const string ownerRole = "Owner";
        if (!await roleManager.RoleExistsAsync(ownerRole))
        {
            IdentityResult roleResult;
            try { roleResult = await roleManager.CreateAsync(new ApplicationRole { Name = ownerRole }); }
            catch (Exception exception) when (IdentityProvisioningConflict.RoleName(exception))
            {
                return Conflict(new { message = "Another request is initializing the Owner role. Please retry. No changes were saved." });
            }
            if (!roleResult.Succeeded) return Problem("The Owner role could not be created. No changes were saved.");
        }

        user.AcademyId = academy.Id;
        user.DisplayName = academy.Name;
        var update = await userManager.UpdateAsync(user);
        if (!update.Succeeded) return BadRequest(new { message = "The academy account could not be updated. No changes were saved." });
        // An unassigned account may already have Owner membership. Retain that
        // valid membership instead of treating Identity's duplicate-role result as failure.
        if (!await userManager.IsInRoleAsync(user, ownerRole))
        {
            var assignment = await userManager.AddToRoleAsync(user, ownerRole);
            if (!assignment.Succeeded) return BadRequest(new { message = "The Owner role could not be assigned. No changes were saved." });
        }

        var response = new AcademySummary(
            academy.Id,
            academy.Name,
            academy.LegalName,
            academy.CountryCode,
            academy.TimeZone,
            academy.IsActive, academy.SubscriptionPlan, academy.SubscriptionStatus, academy.EnabledModulesJson);

        await transaction.CommitAsync(cancellationToken);
        return CreatedAtAction(nameof(List), new { id = academy.Id }, response);
    }
    [HttpPut("{academyId:guid}")]
    public async Task<ActionResult<AcademySummary>> Update(Guid academyId, UpdateAcademyRequest request, CancellationToken token)
    { var user=await userManager.GetUserAsync(User); if(user?.AcademyId!=academyId)return Forbid(); var x=await dbContext.Academies.SingleOrDefaultAsync(v=>v.Id==academyId,token); if(x is null)return NotFound(); if(string.IsNullOrWhiteSpace(request.Name))return BadRequest(new{message="Academy name is required."}); x.Name=request.Name.Trim();x.LegalName=string.IsNullOrWhiteSpace(request.LegalName)?null:request.LegalName.Trim();x.CountryCode=string.IsNullOrWhiteSpace(request.CountryCode)?x.CountryCode:request.CountryCode.Trim().ToUpperInvariant();x.TimeZone=string.IsNullOrWhiteSpace(request.TimeZone)?x.TimeZone:request.TimeZone.Trim(); await dbContext.SaveChangesAsync(token); return Ok(new AcademySummary(x.Id,x.Name,x.LegalName,x.CountryCode,x.TimeZone,x.IsActive,x.SubscriptionPlan,x.SubscriptionStatus,x.EnabledModulesJson)); }
    [HttpPatch("{academyId:guid}/active")]
    public async Task<ActionResult> SetActive(Guid academyId, SetAcademyActiveRequest request, CancellationToken token)
    { var user=await userManager.GetUserAsync(User); if(user?.AcademyId!=academyId)return Forbid(); var x=await dbContext.Academies.SingleOrDefaultAsync(v=>v.Id==academyId,token); if(x is null)return NotFound(); x.IsActive=request.IsActive; await dbContext.SaveChangesAsync(token); return Ok(); }
}

public sealed record CreateAcademyRequest(
    string Name,
    string? LegalName,
    string? CountryCode,
    string? TimeZone);
public sealed record UpdateAcademyRequest(string Name,string? LegalName,string? CountryCode,string? TimeZone);
public sealed record SetAcademyActiveRequest(bool IsActive);

public sealed record AcademySummary(
    Guid Id,
    string Name,
    string? LegalName,
    string CountryCode,
    string TimeZone,
    bool IsActive,
    string SubscriptionPlan,
    string SubscriptionStatus,
    string EnabledModulesJson);
