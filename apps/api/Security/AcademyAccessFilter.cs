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
    private static readonly HashSet<string> FinanceControllers = new(StringComparer.Ordinal)
    {
        "FeePlansController", "InvoicesController", "PaymentsController", "ExpensesController",
        "FinanceAdjustmentsController", "FinanceGovernanceController", "FeeRemindersController", "AcademyExportsController"
    };

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

        var isAdministrator = await userManager.IsInRoleAsync(user, "Owner") ||
            await userManager.IsInRoleAsync(user, "AcademyAdmin") ||
            await userManager.IsInRoleAsync(user, "Manager");
        if (isAdministrator)
        {
            await next();
            return;
        }

        var controller = context.Controller.GetType().Name;
        var collectionTaskRequest = controller == "AdminWorkItemsController" &&
            (string.Equals(context.HttpContext.Request.Query["type"], "Collections", StringComparison.OrdinalIgnoreCase) ||
             context.HttpContext.Request.Path.Value?.EndsWith("/collections", StringComparison.OrdinalIgnoreCase) == true);
        if (await userManager.IsInRoleAsync(user, "FinanceUser") && (FinanceControllers.Contains(controller) || collectionTaskRequest))
        {
            await next();
            return;
        }

        context.Result = new ForbidResult();
    }
}
