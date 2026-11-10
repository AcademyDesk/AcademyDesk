using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Security;

// Identity minimal endpoints and /health have their own pipeline. Only these
// exact MVC security actions bypass maintenance; profile/file writes do not.
internal static class PlatformMaintenancePolicy
{
    internal const string DefaultMessage = "Academy Desk is temporarily unavailable for maintenance. Please try again later.";

    internal static bool IsRecoveryAction(ActionExecutingContext context) =>
        context.ActionDescriptor is ControllerActionDescriptor action &&
        (context.Controller is AuthSessionController &&
            (action.MethodInfo.Name == nameof(AuthSessionController.Current) ||
             action.MethodInfo.Name == nameof(AuthSessionController.ChangePassword)) ||
         context.Controller is PortalController && action.MethodInfo.Name == nameof(PortalController.ChangePassword));

    internal static Task<MaintenanceState?> ReadAsync(AcademyDeskDbContext db, CancellationToken token) =>
        db.PlatformSettings.AsNoTracking()
            .OrderByDescending(x => x.UpdatedAtUtc ?? x.CreatedAtUtc).ThenByDescending(x => x.Id)
            .Select(x => new MaintenanceState(x.MaintenanceMode, x.StatusMessage))
            .FirstOrDefaultAsync(token);
}

internal sealed record MaintenanceState(bool Enabled, string? Message);
