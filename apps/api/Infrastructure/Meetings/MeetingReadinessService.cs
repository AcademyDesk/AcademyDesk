using System.Security.Claims;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Identity;
using AcademyDesk.Api.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Infrastructure.Meetings;

public sealed record MeetingOperations(bool Preview, bool Create, bool Update, bool Cancel, bool History);
public sealed record MeetingProviderCapabilities(string Provider, bool Implemented, bool Available,
    string ReasonCode, MeetingOperations Operations, IReadOnlyList<string> SupportedSettings);
public sealed record MeetingCapabilities(string ContractVersion, DateTimeOffset AsOfUtc,
    IReadOnlyList<MeetingProviderCapabilities> Providers);
public sealed record MeetingReadinessDecision(int StatusCode, MeetingCapabilities? Capabilities);

/// <summary>
/// Current identity/academy authority is independent of UI/model claims. No provider
/// adapter is qualified yet: stored configuration or a connection marker cannot
/// enable execution. This service never reads credentials or makes external calls.
/// </summary>
public sealed class MeetingReadinessService(
    AcademyDeskDbContext db, UserManager<ApplicationUser> users, TimeProvider clock)
{
    public async Task<MeetingReadinessDecision> ReadAsync(ClaimsPrincipal actor, Guid academyId, CancellationToken token)
    {
        if (actor.Identity?.IsAuthenticated != true) return new(401, null);
        if (!Guid.TryParse(users.GetUserId(actor), out var actorId)) return new(401, null);
        // GetUserAsync/IsInRoleAsync can reuse tracked Identity user/membership
        // rows in a long-lived coordinator scope after another scope revokes them.
        var user = await users.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == actorId, token);
        if (user is null) return new(401, null);
        if (!user.IsActive || user.AcademyId != academyId) return new(403, null);
        var roles = await users.GetRolesAsync(user); // Store query, not FindAsync's membership cache.
        if (!roles.Contains("Owner", StringComparer.OrdinalIgnoreCase) && !roles.Contains("AcademyAdmin", StringComparer.OrdinalIgnoreCase))
            return new(403, null);
        var academy = await db.Academies.AsNoTracking().SingleOrDefaultAsync(x => x.Id == academyId, token);
        if (academy is null || !academy.IsActive || !SubscriptionPlanCatalog.Allows(academy.EnabledModulesJson, "Engagement"))
            return new(403, null);

        // Do not infer readiness from CommunicationChannel.HasSecureConnection,
        // model tier, request flags or deployment configuration. Adapter activation
        // requires a separately reviewed connection/execution lifecycle.
        var operations = new MeetingOperations(false, false, false, false, false);
        return new(200, new MeetingCapabilities("meeting-host/0.1", clock.GetUtcNow(),
            new[] { "GoogleWorkspace", "Zoom", "Microsoft365" }
                .Select(provider => new MeetingProviderCapabilities(provider, false, false,
                    "IntegrationUnavailable", operations, Array.Empty<string>())).ToArray()));
    }
}
