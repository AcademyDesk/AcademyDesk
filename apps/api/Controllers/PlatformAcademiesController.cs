using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Domain.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using AcademyDesk.Api.Security;

namespace AcademyDesk.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/platform/academies")]
public sealed class PlatformAcademiesController(AcademyDeskDbContext db, UserManager<ApplicationUser> users, RoleManager<ApplicationRole> roles, IdentityDbContext identityDb) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult> List(CancellationToken token)
    {
        if (!await IsPlatformOwner()) return Forbid();
        return Ok(await db.Academies.AsNoTracking().OrderBy(x => x.Name).Select(x => new { x.Id, x.Name, x.LegalName, x.CountryCode, x.TimeZone, x.IsActive, x.CreatedAtUtc, x.SubscriptionPlan, x.SubscriptionStatus, x.SubscriptionEndsAtUtc, x.StudentLimit, x.StaffLimit, x.EnabledModulesJson, Branches = db.Branches.Count(b => b.AcademyId == x.Id), Students = db.Students.Count(s => s.AcademyId == x.Id && s.IsActive), Staff = db.Teachers.Count(t => t.AcademyId == x.Id && t.IsActive) }).ToListAsync(token));
    }

    [HttpPost]
    public async Task<ActionResult> Onboard(OnboardAcademyRequest request, CancellationToken token)
    {
        if (!await IsPlatformOwner()) return Forbid();
        if (string.IsNullOrWhiteSpace(request.AcademyName) || string.IsNullOrWhiteSpace(request.AdminUserName) || string.IsNullOrWhiteSpace(request.Password)) return BadRequest(new { message = "Academy name, admin user name, and temporary password are required." });
        var requestedLogin = request.AdminUserName.Trim();
        var requestedEmail = requestedLogin.Contains('@') ? requestedLogin : $"{requestedLogin}@academydesk.local";
        if (await users.FindByEmailAsync(requestedEmail) is not null) return Conflict(new { message = "That academy admin user name already exists." });
        var trialDays = await db.PlatformSettings.AsNoTracking().OrderByDescending(x => x.UpdatedAtUtc ?? x.CreatedAtUtc).Select(x => (int?)x.DefaultTrialDays).FirstOrDefaultAsync(token) ?? 30;
        var trialStartsAtUtc = DateTime.UtcNow;
        if (trialDays < 1 || trialDays > (DateTime.MaxValue - trialStartsAtUtc).TotalDays)
            return BadRequest(new { message = "The configured trial duration is invalid. Update platform settings before onboarding." });
        // This platform route has no academyId, so the academy action filter cannot
        // own its boundary. Keep academy, Identity role/user and success audit atomic.
        var identityConnection = AcademyIdentityTransaction.Validate(db, identityDb);
        await using var transaction = await db.Database.BeginTransactionAsync(token);
        await using var identityTransaction = await AcademyIdentityTransaction.EnlistAsync(db, identityDb, transaction, identityConnection, token);
        if (!await roles.RoleExistsAsync("AcademyAdmin"))
        {
            IdentityResult roleResult;
            try { roleResult = await roles.CreateAsync(new ApplicationRole { Name = "AcademyAdmin" }); }
            catch (Exception exception) when (IdentityProvisioningConflict.RoleName(exception))
            {
                return Conflict(new { message = "Another request is initializing the Academy Admin role. Please retry. No changes were saved." });
            }
            if (!roleResult.Succeeded) return BadRequest(new { message = "The Academy Admin role could not be created. No changes were saved." });
        }
        var trial = SubscriptionPlanCatalog.Get("Trial");
        var academy = new Academy { Name = request.AcademyName.Trim(), LegalName = request.LegalName?.Trim(), CountryCode = string.IsNullOrWhiteSpace(request.CountryCode) ? "IN" : request.CountryCode.Trim().ToUpperInvariant(), TimeZone = "Asia/Kolkata", SubscriptionPlan = trial.Name, SubscriptionStatus = "Trial", SubscriptionEndsAtUtc = trialStartsAtUtc.AddDays(trialDays), StudentLimit = trial.StudentLimit, StaffLimit = trial.StaffLimit, EnabledModulesJson = JsonSerializer.Serialize(trial.Modules) };
        db.Academies.Add(academy); await db.SaveChangesAsync(token);
        var userName = request.AdminUserName.Trim(); var email = userName.Contains('@') ? userName : $"{userName}@academydesk.local";
        var admin = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true, DisplayName = string.IsNullOrWhiteSpace(request.AdminDisplayName) ? userName : request.AdminDisplayName.Trim(), AcademyId = academy.Id };
        IdentityResult result;
        try { result = await users.CreateAsync(admin, request.Password); }
        catch (Exception exception) when (IdentityProvisioningConflict.UserName(exception))
        {
            return Conflict(new { message = "Another account request conflicted with this submission. Please retry, or use a different login if it already exists. No changes were saved." });
        }
        if (!result.Succeeded) return BadRequest(new { message = string.Join(" ", result.Errors.Select(x => x.Description)) + " No changes were saved." });
        var assignment = await users.AddToRoleAsync(admin, "AcademyAdmin");
        if (!assignment.Succeeded) return BadRequest(new { message = "The Academy Admin role could not be assigned. No changes were saved." });
        await Audit("Academy onboarded", "Academy", academy.Id, new { academy.Name, admin.DisplayName }, token);
        await db.SaveChangesAsync(token);
        await transaction.CommitAsync(token);
        return Created($"/api/platform/academies/{academy.Id}", new { academy.Id, academy.Name, admin.UserName, admin.DisplayName });
    }

    [HttpPatch("{academyId:guid}/status")]
    public async Task<ActionResult> SetStatus(Guid academyId, SetPlatformAcademyStatusRequest request, CancellationToken token)
    {
        if (!await IsPlatformOwner()) return Forbid();
        var academy = await db.Academies.SingleOrDefaultAsync(x => x.Id == academyId, token);
        if (academy is null) return NotFound();
        academy.IsActive = request.IsActive;
        await Audit(request.IsActive ? "Academy activated" : "Academy suspended", "Academy", academy.Id, new { academy.Name }, token);
        await db.SaveChangesAsync(token);
        return Ok(new { academy.Id, academy.IsActive });
    }

    [HttpPut("{academyId:guid}/configuration")]
    public async Task<ActionResult> Configure(Guid academyId, TenantConfigurationRequest request, CancellationToken token)
    {
        if (!await IsPlatformOwner()) return Forbid();
        var academy = await db.Academies.SingleOrDefaultAsync(x => x.Id == academyId, token);
        if (academy is null) return NotFound();
        if (string.IsNullOrWhiteSpace(request.SubscriptionPlan) || string.IsNullOrWhiteSpace(request.SubscriptionStatus)) return BadRequest(new { message = "Subscription plan and status are required." });
        if (!SubscriptionPlanCatalog.Plans.TryGetValue(request.SubscriptionPlan, out var plan))
            return BadRequest(new { message = "Choose a valid subscription plan." });
        academy.SubscriptionPlan = plan.Name;
        academy.SubscriptionStatus = request.SubscriptionStatus.Trim();
        academy.SubscriptionEndsAtUtc = request.SubscriptionEndsAtUtc;
        academy.StudentLimit = plan.StudentLimit;
        academy.StaffLimit = plan.StaffLimit;
        academy.EnabledModulesJson = JsonSerializer.Serialize(plan.Modules);
        await Audit("Tenant configuration updated", "Academy", academy.Id, new { academy.SubscriptionPlan, academy.SubscriptionStatus, academy.StudentLimit, academy.StaffLimit }, token);
        await db.SaveChangesAsync(token);
        return Ok(new { academy.Id, academy.SubscriptionPlan, academy.SubscriptionStatus, academy.SubscriptionEndsAtUtc, academy.StudentLimit, academy.StaffLimit, academy.EnabledModulesJson });
    }

    private async Task<bool> IsPlatformOwner() { var user = await users.GetUserAsync(User); return user?.IsPlatformOwner == true; }
    private async Task Audit(string action, string entityType, Guid entityId, object metadata, CancellationToken token) { var user = await users.GetUserAsync(User); db.PlatformAuditEntries.Add(new PlatformAuditEntry { ActorUserId = user?.Id, ActorName = user?.DisplayName ?? "System", Action = action, EntityType = entityType, EntityId = entityId, MetadataJson = JsonSerializer.Serialize(metadata) }); await Task.CompletedTask; }
}

public sealed record OnboardAcademyRequest(string AcademyName, string? LegalName, string? CountryCode, string? TimeZone, string AdminUserName, string? AdminDisplayName, string Password);
public sealed record SetPlatformAcademyStatusRequest(bool IsActive);
public sealed record TenantConfigurationRequest(string SubscriptionPlan, string SubscriptionStatus, DateTime? SubscriptionEndsAtUtc);
