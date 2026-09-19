using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/academies/{academyId:guid}/portal-accounts")]
public sealed class PortalAccountsController(UserManager<ApplicationUser> users, RoleManager<ApplicationRole> roles, AcademyDeskDbContext db) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult> Create(Guid academyId, CreatePortalAccountRequest request, CancellationToken token)
    {
        var owner = await users.GetUserAsync(User);
        if (owner?.AcademyId != academyId || (!await users.IsInRoleAsync(owner, "Owner") && !await users.IsInRoleAsync(owner, "AcademyAdmin"))) return Forbid();
        var role = request.Role is "Student" or "Guardian" or "Teacher" ? request.Role : null;
        if (role is null || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password)) return BadRequest(new { message = "Student, Parent, or Teacher role, email, and temporary password are required." });
        if (role == "Student" && (!request.StudentId.HasValue || !await db.Students.AnyAsync(x => x.Id == request.StudentId && x.AcademyId == academyId, token))) return BadRequest(new { message = "Select a valid student." });
        if (role == "Guardian" && (!request.GuardianId.HasValue || !await db.Guardians.AnyAsync(x => x.Id == request.GuardianId && x.AcademyId == academyId, token))) return BadRequest(new { message = "Select a valid guardian." });
        if (role == "Teacher" && (!request.TeacherId.HasValue || !await db.Teachers.AnyAsync(x => x.Id == request.TeacherId && x.AcademyId == academyId, token))) return BadRequest(new { message = "Select a valid teacher." });
        if (!await roles.RoleExistsAsync(role)) await roles.CreateAsync(new ApplicationRole { Name = role });
        var user = new ApplicationUser { UserName = request.Email.Trim(), Email = request.Email.Trim(), DisplayName = request.DisplayName?.Trim() ?? request.Email.Trim(), AcademyId = academyId, StudentId = role == "Student" ? request.StudentId : null, GuardianId = role == "Guardian" ? request.GuardianId : null, TeacherId = role == "Teacher" ? request.TeacherId : null };
        var create = await users.CreateAsync(user, request.Password);
        if (!create.Succeeded) return BadRequest(new { message = string.Join(" ", create.Errors.Select(x => x.Description)) });
        await users.AddToRoleAsync(user, role);
        return Ok(new { user.Id, user.Email, role });
    }
}
public sealed record CreatePortalAccountRequest(string Role, string Email, string Password, string? DisplayName, Guid? StudentId, Guid? GuardianId, Guid? TeacherId);
