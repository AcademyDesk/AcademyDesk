using System.Text.Json;
using AcademyDesk.Api.Domain.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Controllers;

[ApiController, Authorize]
[Route("api/academies/{academyId:guid}/roles")]
public sealed class AcademyRolesController(UserManager<ApplicationUser> users, RoleManager<ApplicationRole> roles) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult> List(Guid academyId)
    {
        if (!await IsAdmin(academyId)) return Forbid();
        var result = roles.Roles.Where(role => role.AcademyId == null || role.AcademyId == academyId)
            .OrderBy(role => role.IsSystemRole).ThenBy(role => role.Name)
            .Select(role => new { role.Id, role.Name, role.IsSystemRole, role.AcademyId, role.PermissionsJson }).ToList();
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult> Create(Guid academyId, CreateAcademyRoleRequest request)
    {
        if (!await IsAdmin(academyId)) return Forbid();
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Length > 80) return BadRequest(new { message = "Enter a role name of up to 80 characters." });
        var name = request.Name.Trim();
        if (await roles.Roles.AnyAsync(role => role.AcademyId == academyId && role.Name == name)) return Conflict(new { message = "That academy role already exists." });
        var role = new ApplicationRole { Name = name, AcademyId = academyId, IsSystemRole = false, PermissionsJson = JsonSerializer.Serialize(request.Permissions?.Distinct() ?? []) };
        var result = await roles.CreateAsync(role);
        return result.Succeeded ? Ok(new { role.Id, role.Name, role.PermissionsJson }) : BadRequest(result.Errors);
    }

    private async Task<bool> IsAdmin(Guid academyId)
    {
        var user = await users.GetUserAsync(User);
        return user?.AcademyId == academyId && (await users.IsInRoleAsync(user, "Owner") || await users.IsInRoleAsync(user, "AcademyAdmin"));
    }
}
public sealed record CreateAcademyRoleRequest(string Name, IReadOnlyList<string>? Permissions);
