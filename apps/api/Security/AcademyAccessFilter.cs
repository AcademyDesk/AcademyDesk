using AcademyDesk.Api.Domain.Identity;
using AcademyDesk.Api.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
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
        "FinanceAdjustmentsController", "FinanceGovernanceController", "FeeRemindersController", "AcademyExportsController",
        "PayrollController"
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
        var method = context.HttpContext.Request.Method;
        if (HttpMethods.IsGet(method) || HttpMethods.IsHead(method) || HttpMethods.IsOptions(method))
        {
            await next();
            return;
        }

        var controllerName = context.Controller.GetType().Name;
        var controller = controllerName.Replace("Controller", string.Empty, StringComparison.Ordinal);
        if (string.Equals(controller, "AuditLogs", StringComparison.Ordinal))
        {
            await next();
            return;
        }

        // Explicit linked updates enlist their existing scoped Identity context;
        // other opted-in actions/finance remain domain-only. Reject mismatched
        // stores before business writes; files/Blob are never covered here.
        var identityConnection = IncludesIdentity(context.ActionDescriptor) ? AcademyIdentityTransaction.Validate(academyDb, identityDb) : null;
        await using var transaction = UsesAtomicBoundary(controllerName, context.ActionDescriptor) && academyDb.Database.IsRelational()
            ? await academyDb.Database.BeginTransactionAsync(context.HttpContext.RequestAborted)
            : null;
        await using var identityTransaction = identityConnection is not null
            ? await AcademyIdentityTransaction.EnlistAsync(academyDb, identityDb, transaction!, identityConnection, context.HttpContext.RequestAborted)
            : null;
        var executed = await next();
        if (!ShouldAudit(executed)) return; // Disposing an uncommitted boundary rolls it back.

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
        if (transaction is not null) await transaction.CommitAsync(context.HttpContext.RequestAborted);
    }

    internal static bool UsesAtomicBoundary(string controllerName, ActionDescriptor descriptor) =>
        FinanceControllers.Contains(controllerName) || descriptor is ControllerActionDescriptor action &&
        action.MethodInfo.IsDefined(typeof(AtomicAcademyMutationAttribute), inherit: false);

    internal static bool IncludesIdentity(ActionDescriptor descriptor) => descriptor is ControllerActionDescriptor action &&
        Attribute.GetCustomAttribute(action.MethodInfo, typeof(AtomicAcademyMutationAttribute), inherit: false) is AtomicAcademyMutationAttribute { IncludeIdentity: true };

    internal static bool ShouldAudit(ActionExecutedContext executed)
    {
        if (executed.Canceled || executed.Exception is not null ||
            executed.HttpContext.Response.StatusCode >= StatusCodes.Status400BadRequest) return false;

        // IActionResult has not executed yet: Response.StatusCode can still be
        // 200 for BadRequest/NotFound/Conflict/etc. Respect the pending result.
        var status = executed.Result switch
        {
            ForbidResult => StatusCodes.Status403Forbidden,
            ChallengeResult => StatusCodes.Status401Unauthorized,
            ObjectResult { StatusCode: null, Value: ProblemDetails { Status: int problemStatus } } => problemStatus,
            IStatusCodeActionResult { StatusCode: int resultStatus } => resultStatus,
            _ => executed.HttpContext.Response.StatusCode
        };
        return status < StatusCodes.Status400BadRequest;
    }
}
