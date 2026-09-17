using AcademyDesk.Api.Domain.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AcademyDesk.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/auth/session")]
public sealed class AuthSessionController(UserManager<ApplicationUser> users) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<SessionSummary>> Current(CancellationToken token)
    {
        var user = await users.GetUserAsync(User);
        if (user is null) return Unauthorized();

        var roles = await users.GetRolesAsync(user);
        return Ok(new SessionSummary(user.DisplayName, roles.ToArray(), user.AcademyId));
    }
}

public sealed record SessionSummary(string DisplayName, IReadOnlyList<string> Roles, Guid? AcademyId);
