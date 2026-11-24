using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Identity;
using AcademyDesk.Api.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Intelligence.Penta;

internal sealed record PentaQueryAuthority(Guid ActorId, Guid AcademyId, long Version, IReadOnlySet<string> Tools);

public sealed class PentaQueryAccess(IdentityDbContext identity, AcademyDeskDbContext db,
    UserManager<ApplicationUser> users, IConfiguration config, IHostEnvironment environment)
{
    public bool Enabled => (environment.IsDevelopment() || environment.IsEnvironment("Testing")) &&
        config.GetValue<bool>("Penta:Enabled") && config.GetValue<bool>("Penta:Queries:Enabled");

    internal async Task<PentaQueryAuthority?> ResolveAsync(ClaimsPrincipal principal, Guid academy, CancellationToken token)
    {
        if (!Enabled || principal.Identity?.IsAuthenticated != true || academy == Guid.Empty ||
            !Guid.TryParse(users.GetUserId(principal), out var actorId)) return null;
        var stamp = principal.FindFirstValue(users.Options.ClaimsIdentity.SecurityStampClaimType);
        if (string.IsNullOrEmpty(stamp)) return null;
        var actor = await identity.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == actorId, token);
        if (actor is not { IsActive: true, IsPlatformOwner: false } || actor.AcademyId != academy || actor.SecurityStamp != stamp) return null;
        var roles = await (from membership in identity.UserRoles.AsNoTracking()
            join role in identity.Roles.AsNoTracking() on membership.RoleId equals role.Id
            where membership.UserId == actorId select role.Name).ToArrayAsync(token);
        if (!roles.Any(role => role is "Owner" or "AcademyAdmin")) return null;
        var tenant = await db.Academies.AsNoTracking().SingleOrDefaultAsync(x => x.Id == academy && x.IsActive, token);
        if (tenant is null) return null;
        var tools = PentaQueryContract.Controllers.Where(pair => SubscriptionPlanCatalog.Allows(tenant.EnabledModulesJson,
            SubscriptionPlanCatalog.ModuleForController(pair.Value)))
            .SelectMany(pair => new[] { PentaQueryContract.Tool(pair.Key, "search"), PentaQueryContract.Tool(pair.Key, "open") })
            .ToHashSet(StringComparer.Ordinal);
        var fingerprint = SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new {
            actor.SecurityStamp, roles = roles.Order(StringComparer.Ordinal).ToArray(), tenant.EnabledModulesJson })));
        long version = 0; foreach (var b in fingerprint.Take(6)) version = (version << 8) | b;
        return new(actorId, academy, version + 1, tools);
    }

    internal static object[] Describe(IReadOnlySet<string> tools) => PentaQueryContract.Controllers.Keys
        .SelectMany(entity => new[] { "search", "open" }.Select(kind => new {
            key = PentaQueryContract.Tool(entity, kind), entity, kind, readOnly = true,
            fields = entity == "teacher" ? new[] { "name", "subject", "status" } : new[] { "name", "status" } }))
        .Where(tool => tools.Contains(tool.key)).Cast<object>().ToArray();
}
