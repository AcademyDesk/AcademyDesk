using AcademyDesk.Api.Domain.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AcademyDesk.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/academies/{academyId:guid}/staff")]
public sealed class StaffController(
    UserManager<ApplicationUser> userManager,
    RoleManager<ApplicationRole> roleManager) : ControllerBase
{
    private static readonly string[] AllowedRoles = ["Manager", "Teacher", "FrontDesk"];

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<StaffAccountSummary>>> List(Guid academyId)
    {
        if (!await IsOwner(academyId)) return Forbid();

        var users = userManager.Users
            .Where(x => x.AcademyId == academyId)
            .OrderBy(x => x.DisplayName)
            .ToList();

        var result = new List<StaffAccountSummary>();
        foreach (var user in users)
        {
            var roles = await userManager.GetRolesAsync(user);
            result.Add(new StaffAccountSummary(user.Id, user.DisplayName, user.Email ?? string.Empty, user.TeacherId, roles.ToArray(), user.IsActive));
        }

        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<StaffAccountSummary>> Create(Guid academyId, CreateStaffAccountRequest request)
    {
        if (!await IsOwner(academyId)) return Forbid();
        if (!AllowedRoles.Contains(request.Role, StringComparer.OrdinalIgnoreCase))
            return BadRequest(new { message = "Role must be Manager, Teacher, or FrontDesk." });
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.DisplayName) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { message = "Email, display name, and an initial password are required." });

        var role = AllowedRoles.Single(x => x.Equals(request.Role, StringComparison.OrdinalIgnoreCase));
        if (!await roleManager.RoleExistsAsync(role))
        {
            var roleResult = await roleManager.CreateAsync(new ApplicationRole { Name = role });
            if (!roleResult.Succeeded) return Problem("The staff role could not be created.");
        }

        var user = new ApplicationUser
        {
            UserName = request.Email.Trim(),
            Email = request.Email.Trim(),
            DisplayName = request.DisplayName.Trim(),
            AcademyId = academyId,
            TeacherId = request.TeacherId,
            EmailConfirmed = false
        };
        var createResult = await userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
            return BadRequest(new { message = string.Join(" ", createResult.Errors.Select(x => x.Description)) });

        var addRoleResult = await userManager.AddToRoleAsync(user, role);
        if (!addRoleResult.Succeeded)
        {
            await userManager.DeleteAsync(user);
            return Problem("The staff role could not be assigned.");
        }

        return Created($"/api/academies/{academyId}/staff/{user.Id}",
            new StaffAccountSummary(user.Id, user.DisplayName, user.Email ?? string.Empty, user.TeacherId, [role], user.IsActive));
    }

    [HttpPatch("{staffId:guid}/status")]
    public async Task<ActionResult> UpdateStatus(Guid academyId, Guid staffId, StaffStatusRequest request)
    {
        if (!await IsOwner(academyId)) return Forbid();
        var current = await userManager.GetUserAsync(User);
        if (current?.Id == staffId) return BadRequest(new { message = "The owner account cannot be deactivated here." });
        var staff = await userManager.FindByIdAsync(staffId.ToString());
        if (staff?.AcademyId != academyId) return NotFound();
        staff.IsActive = request.IsActive;
        await userManager.UpdateAsync(staff);
        return Ok(new { staff.Id, staff.IsActive });
    }

    [HttpPatch("{staffId:guid}/password")]
    public async Task<ActionResult> ResetPassword(Guid academyId, Guid staffId, StaffPasswordRequest request)
    {
        if (!await IsOwner(academyId)) return Forbid();
        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 8)
            return BadRequest(new { message = "The new password must be at least 8 characters." });
        var staff = await userManager.FindByIdAsync(staffId.ToString());
        if (staff?.AcademyId != academyId) return NotFound();
        var token = await userManager.GeneratePasswordResetTokenAsync(staff);
        var result = await userManager.ResetPasswordAsync(staff, token, request.NewPassword);
        if (!result.Succeeded) return BadRequest(new { message = string.Join(" ", result.Errors.Select(x => x.Description)) });
        return Ok(new { message = "Staff password reset successfully." });
    }

    private async Task<bool> IsOwner(Guid academyId)
    {
        var currentUser = await userManager.GetUserAsync(User);
        return currentUser?.AcademyId == academyId && (await userManager.IsInRoleAsync(currentUser, "Owner") || await userManager.IsInRoleAsync(currentUser, "AcademyAdmin"));
    }
}

public sealed record CreateStaffAccountRequest(string Email, string DisplayName, string Password, string Role, Guid? TeacherId);
public sealed record StaffAccountSummary(Guid Id, string DisplayName, string Email, Guid? TeacherId, IReadOnlyList<string> Roles, bool IsActive);
public sealed record StaffStatusRequest(bool IsActive);
public sealed record StaffPasswordRequest(string NewPassword);
