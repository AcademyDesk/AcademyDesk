using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Security;

/// <summary>
/// Teacher self-service routes have no academyId argument. Resolve their current
/// authority from Identity and domain records, not token roles or caller input.
/// Batch/student restrictions remain the responsibility of each action.
/// </summary>
public sealed class TeacherPortalAccessFilter(
    UserManager<ApplicationUser> userManager,
    AcademyDeskDbContext academyDb) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var user = await userManager.GetUserAsync(context.HttpContext.User);
        if (user is null)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        if (!user.IsActive || user.AcademyId is null || user.TeacherId is null ||
            !await userManager.IsInRoleAsync(user, "Teacher") ||
            !await academyDb.Teachers.AsNoTracking().AnyAsync(teacher =>
                teacher.Id == user.TeacherId && teacher.AcademyId == user.AcademyId && teacher.IsActive &&
                academyDb.Academies.Any(academy => academy.Id == teacher.AcademyId && academy.IsActive),
                context.HttpContext.RequestAborted))
        {
            context.Result = new ObjectResult(new { message = "Teacher portal access is unavailable for this account." })
            { StatusCode = StatusCodes.Status403Forbidden };
            return;
        }

        await next();
    }
}
