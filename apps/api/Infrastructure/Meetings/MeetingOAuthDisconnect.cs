using System.Security.Claims;
using AcademyDesk.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Infrastructure.Meetings;

internal sealed partial class MeetingOAuthAttemptStore
{
    // Local host disconnection only, NOT confirmed provider-side revocation.
    // Keep encrypted evidence for the separately governed retention/revoke flow.
    public async Task<MeetingOAuthPersistenceOutcome> DisconnectLocallyAsync(ClaimsPrincipal actor, Guid academy, string provider,
        int expectedVersion, bool confirmed, CancellationToken token = default)
    {
        var authority = await Authorize(actor, academy, token);
        if (authority.Code != 200 || authority.User is null) return new(authority.Code, "AccessDenied");
        if (!ProviderAllowed(provider) || expectedVersion < 1 || !confirmed) return new(400, "DisconnectConfirmationRequired");
        var binding = await db.MeetingProviderBindings.SingleOrDefaultAsync(x => x.AcademyId == academy && x.Provider == provider, token);
        if (binding is null) return new(404, "ConnectionNotFound");
        if (binding.Version != expectedVersion || binding.Version == int.MaxValue || binding.Generation == int.MaxValue || binding.Generation < 0 ||
            binding.Status is not ("AuthorizationPending" or "VerificationRequired")) return new(409, "ConnectionContextChanged");
        if (!await FreshExchangeAuthority(actor, academy, authority.User.Id, Hash(authority.User.SecurityStamp!), token)) return new(403, "AccessDenied");
        binding.Status = "Disconnected"; binding.Generation++; binding.Version++; binding.UpdatedAtUtc = clock.GetUtcNow().UtcDateTime;
        var audit = new AuditLog { AcademyId = academy, ActorUserId = authority.User.Id, Action = "MeetingDisconnectedLocallyRevocationRequired",
            EntityType = "MeetingProviderBinding", EntityId = binding.Id };
        db.AuditLogs.Add(audit);
        try { await db.SaveChangesAsync(token); }
        catch (DbUpdateConcurrencyException) { Detach(binding, audit); return new(409, "ConcurrentConnectionChange"); }
        return new(200, "DisconnectedLocallyProviderRevocationRequired");
    }
}
