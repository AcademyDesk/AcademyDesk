using AcademyDesk.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Security;

internal static class WorkItemValidation
{
    private static readonly HashSet<string> PortalRoles = new(StringComparer.OrdinalIgnoreCase) { "Student", "Guardian", "Teacher" };
    internal static bool ValidPriority(string? priority) => priority is null or "Low" or "Normal" or "High" or "Critical";

    internal static async Task<bool> ValidAssignee(IdentityDbContext identity, Guid academyId, Guid? userId, CancellationToken token)
    {
        if (userId is null) return true;
        if (userId == Guid.Empty) return false;
        var user = await identity.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == userId && x.AcademyId == academyId, token);
        if (user is null || !user.IsActive || user.IsPlatformOwner || user.LockoutEnabled && user.LockoutEnd > DateTimeOffset.UtcNow) return false;

        // Match the staff directory's exclusion of portal identities. Custom
        // academy staff roles remain eligible; a role from another academy does not.
        var roles = await (from membership in identity.UserRoles.AsNoTracking()
                           join role in identity.Roles.AsNoTracking() on membership.RoleId equals role.Id
                           where membership.UserId == userId
                           select new { role.Name, role.AcademyId }).ToListAsync(token);
        return roles.Count > 0 && roles.All(x => !string.IsNullOrWhiteSpace(x.Name) &&
            (x.AcademyId is null || x.AcademyId == academyId) && !PortalRoles.Contains(x.Name));
    }

    internal static async Task<bool> ValidEntity(AcademyDeskDbContext db, Guid academyId, string? type, Guid? id, CancellationToken token)
    {
        if (type is null && id is null) return true;
        if (id is null || id == Guid.Empty) return false;
        // These are the entity links currently produced by the application.
        return type switch
        {
            "Invoice" => await db.Invoices.AnyAsync(x => x.AcademyId == academyId && x.Id == id, token),
            "PersonDocument" => await db.PersonDocuments.AnyAsync(x => x.AcademyId == academyId && x.Id == id, token),
            _ => false
        };
    }
}
