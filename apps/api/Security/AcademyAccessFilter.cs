using AcademyDesk.Api.Domain.Identity;
using AcademyDesk.Api.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Identity;
using AcademyDesk.Api.Domain.Entities;

namespace AcademyDesk.Api.Security;

/// <summary>
/// Prevents an authenticated user from reading or changing another academy's data.
/// It applies to every controller action that has an academyId route parameter.
/// </summary>
public sealed class AcademyAccessFilter(UserManager<ApplicationUser> userManager, IdentityDbContext identityDb, AcademyDeskDbContext academyDb) : IAsyncActionFilter
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

        // Platform owners manage academies that are not their own tenant. Their
        // individual platform endpoints still perform their own owner check.
        if (user.IsPlatformOwner)
        {
            await next();
            return;
        }

        if (user.AcademyId != academyId)
        {
            context.Result = new ForbidResult();
            return;
        }

        var controller = context.Controller.GetType().Name;
        var requiredModule = SubscriptionPlanCatalog.ModuleForController(controller);
        var academy = await academyDb.Academies.AsNoTracking().SingleOrDefaultAsync(x => x.Id == academyId, context.HttpContext.RequestAborted);
        if (academy is null || !academy.IsActive || !SubscriptionPlanCatalog.Allows(academy.EnabledModulesJson, requiredModule))
        {
            context.Result = new ObjectResult(new { message = "This feature is not included in your academy subscription." }) { StatusCode = StatusCodes.Status403Forbidden };
            return;
        }

        var isAdministrator = await userManager.IsInRoleAsync(user, "Owner") ||
            await userManager.IsInRoleAsync(user, "AcademyAdmin");
        if (isAdministrator)
        {
            await ExecuteAndAuditAsync(context, next, user, academyId);
            return;
        }

        var required = PermissionCatalog.RequiredFor(controller);
        if (required is null)
        {
            context.Result = new ForbidResult();
            return;
        }
        var roles = await userManager.GetRolesAsync(user);
        var granted = roles.SelectMany(PermissionCatalog.ForSystemRole).ToHashSet(StringComparer.Ordinal);
        var customPermissions = await identityDb.Roles.Where(role => roles.Contains(role.Name!))
            .Select(role => role.PermissionsJson).ToListAsync(context.HttpContext.RequestAborted);
        foreach (var json in customPermissions) foreach (var permission in JsonSerializer.Deserialize<string[]>(json) ?? []) granted.Add(permission);
        var grants = await identityDb.AccessGrants.Where(grant => grant.AcademyId == academyId && grant.UserId == user.Id && grant.RevokedAtUtc == null && (grant.IsPermanent || grant.ExpiresAtUtc > DateTimeOffset.UtcNow))
            .Select(grant => grant.PermissionsJson).ToListAsync(context.HttpContext.RequestAborted);
        foreach (var json in grants) foreach (var permission in JsonSerializer.Deserialize<string[]>(json) ?? []) granted.Add(permission);
        if (required.All(granted.Contains))
        {
            await ExecuteAndAuditAsync(context, next, user, academyId);
            return;
        }

        context.Result = new ForbidResult();
    }

    private async Task ExecuteAndAuditAsync(ActionExecutingContext context, ActionExecutionDelegate next, ApplicationUser user, Guid academyId)
    {
        var executed = await next();
        if (executed.Canceled || executed.Exception is not null || context.HttpContext.Response.StatusCode >= StatusCodes.Status400BadRequest) return;

        var method = context.HttpContext.Request.Method;
        if (HttpMethods.IsGet(method) || HttpMethods.IsHead(method) || HttpMethods.IsOptions(method)) return;

        var controller = context.Controller.GetType().Name.Replace("Controller", string.Empty, StringComparison.Ordinal);
        if (string.Equals(controller, "AuditLogs", StringComparison.Ordinal)) return;

        academyDb.AuditLogs.Add(new AuditLog
        {
            AcademyId = academyId,
            ActorUserId = user.Id,
            Action = $"{method} {controller}",
            EntityType = controller,
            MetadataJson = JsonSerializer.Serialize(new { Route = context.HttpContext.Request.Path.Value, Action = context.ActionDescriptor.DisplayName }),
            IpAddress = context.HttpContext.Connection.RemoteIpAddress?.ToString()
        });
        await academyDb.SaveChangesAsync(context.HttpContext.RequestAborted);
    }
}
