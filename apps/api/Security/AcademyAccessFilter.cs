using AcademyDesk.Api.Domain.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Identity;

namespace AcademyDesk.Api.Security;

/// <summary>
/// Prevents an authenticated user from reading or changing another academy's data.
/// It applies to every controller action that has an academyId route parameter.
/// </summary>
public sealed class AcademyAccessFilter(UserManager<ApplicationUser> userManager) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        if (!context.ActionArguments.TryGetValue("academyId", out var routeValue) || routeValue is not Guid academyId)
        {
            await next();
            return;
        }

        var user = await userManager.GetUserAsync(context.HttpContext.User);
        if (user is null)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        if (user.AcademyId != academyId)
        {
            context.Result = new ForbidResult();
            return;
        }

        await next();
    }
}
