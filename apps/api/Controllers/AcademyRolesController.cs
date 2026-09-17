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

    [HttpPut("staff/{staffId:guid}/assignment")]
    public async Task<ActionResult> Assign(Guid academyId, Guid staffId, RoleAssignmentRequest request)
    {
        if (!await IsAdmin(academyId)) return Forbid();
        var staff = await users.FindByIdAsync(staffId.ToString());
        var role = await roles.FindByIdAsync(request.RoleId.ToString());
        if (staff?.AcademyId != academyId || role is null || (role.AcademyId is not null && role.AcademyId != academyId)) return NotFound();
        if (role.Name is null) return BadRequest();
        var current = await users.GetRolesAsync(staff);
        var mutable = current.Where(name => !new[] { "Owner", "AcademyAdmin", "Student", "Guardian" }.Contains(name)).ToArray();
        if (mutable.Length > 0) await users.RemoveFromRolesAsync(staff, mutable);
        var result = await users.AddToRoleAsync(staff, role.Name);
        return result.Succeeded ? Ok(new { staff.Id, role = role.Name }) : BadRequest(result.Errors);
    }

    private async Task<bool> IsAdmin(Guid academyId)
    {
        var user = await users.GetUserAsync(User);
        return user?.AcademyId == academyId && (await users.IsInRoleAsync(user, "Owner") || await users.IsInRoleAsync(user, "AcademyAdmin"));
    }
}
public sealed record CreateAcademyRoleRequest(string Name, IReadOnlyList<string>? Permissions);
public sealed record RoleAssignmentRequest(Guid RoleId);
