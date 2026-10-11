using AcademyDesk.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Security;

public static class WorkItemReferences
{
    public static async Task<bool> CanAssignAsync(IdentityDbContext identity, Guid academy, Guid? userId, CancellationToken token)
    {
        if (userId is null) return true;
        var now = DateTimeOffset.UtcNow;
        if (!await identity.Users.AsNoTracking().AnyAsync(x => x.Id == userId && x.AcademyId == academy && x.IsActive && !x.IsPlatformOwner &&
            (!x.LockoutEnabled || x.LockoutEnd == null || x.LockoutEnd <= now), token)) return false;
        // Match Staff.List's exclusion of separate portal identities. Assignment
        // is routing metadata, never a role/permission grant; custom staff remain valid.
        return !await identity.UserRoles.Where(x => x.UserId == userId).Join(identity.Roles, x => x.RoleId, x => x.Id, (_, role) => role.Name)
            .AnyAsync(name => name == "Teacher" || name == "Student" || name == "Guardian", token);
    }

    public static async Task<bool> HasValidLinkAsync(AcademyDeskDbContext db, Guid academy, string workType, string? entityType, Guid? entityId, CancellationToken token)
    {
        if (entityType is null && entityId is null) return true;
        if (entityType is null || entityId is null || entityId == Guid.Empty) return false;
        // These are the two existing application-generated work links. Do not
        // dynamically resolve arbitrary table names or invent new link vocabularies.
        if (workType == "Collections" && entityType != "Invoice" || workType == "Compliance" && entityType != "PersonDocument") return false;
        return entityType switch
        {
            "Invoice" => await db.Invoices.AsNoTracking().AnyAsync(x => x.Id == entityId && x.AcademyId == academy, token),
            "PersonDocument" => await db.PersonDocuments.AsNoTracking().AnyAsync(x => x.Id == entityId && x.AcademyId == academy, token),
            _ => false
        };
    }
}
