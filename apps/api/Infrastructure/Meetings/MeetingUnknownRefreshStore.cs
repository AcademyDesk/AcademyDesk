using System.Security.Claims;
using System.Text.Json;
using AcademyDesk.Api.Domain.Entities;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Infrastructure.Meetings;

internal sealed partial class MeetingOAuthAttemptStore
{
    private const string UnknownRefreshManualScope = "unknown-refresh-family-manual/1";

    // Trusted human-confirmation adapter only, not a model approval/tool. Retires
    // an uncertain exchange locally; never retries refresh or claims the original
    // or a possibly rotated refresh-token family was revoked at the provider.
    internal async Task<MeetingRevocationOutcome> DisconnectUnknownRefreshAsync(ClaimsPrincipal actor, Guid academy, MeetingOAuthRegistration registration,
        MeetingOAuthExchangePermit permit, int expectedExchangeVersion, int expectedBindingVersion, bool disconnectConfirmed, CancellationToken token = default)
    {
        var authority = await Authorize(actor, academy, token);
        if (authority.Code != 200 || authority.User is null) return new(authority.Code, "AccessDenied");
        if (!SupportsRefresh(registration)) return new(503, "ProviderRefreshUnqualified");
        if (!disconnectConfirmed || expectedExchangeVersion < 3 || expectedExchangeVersion % 2 != 1 || expectedExchangeVersion == int.MaxValue || expectedBindingVersion < 1)
            return new(400, "UnknownRefreshDisconnectConfirmationRequired");
        var row = await db.MeetingOAuthExchanges.SingleOrDefaultAsync(x => x.Id == permit.Id && x.AcademyId == academy && x.ActorUserId == authority.User.Id &&
            x.Provider == registration.Provider && x.Status == "RefreshUnknown" && x.Version == expectedExchangeVersion, token);
        var source = row is null ? null : UnsealExchange(row);
        if (source?.Tokens is null || source.Fence != permit.Fence || source.Registration != registration.Fingerprint ||
            !await FreshExchangeAuthority(actor, academy, source.Actor, source.SecurityStampHash, token)) return new(409, "UnknownRefreshContextChanged");
        var binding = await CurrentExchangeBinding(academy, source.Binding, source.Provider, source.Generation, source.BindingVersion, token);
        var alreadyDisconnected = false;
        if (binding is null && source.Generation < int.MaxValue && source.BindingVersion < int.MaxValue)
        {
            binding = await db.MeetingProviderBindings.SingleOrDefaultAsync(x => x.Id == source.Binding && x.AcademyId == academy && x.Provider == source.Provider &&
                x.Status == "Disconnected" && x.Generation == source.Generation + 1 && x.Version == source.BindingVersion + 1, token);
            alreadyDisconnected = binding is not null;
        }
        if (binding is null || binding.Version != expectedBindingVersion || (!alreadyDisconnected && (binding.Generation == int.MaxValue || binding.Version == int.MaxValue)) ||
            await db.MeetingCredentialRevocations.AnyAsync(x => x.ExchangeId == source.Id, token))
            return new(409, "UnknownRefreshContextChanged");
        var now = clock.GetUtcNow().UtcDateTime;
        row!.Status = "RefreshAbandoned"; row.Version++; row.UpdatedAtUtc = now;
        row.ProtectedEnvelope = ExchangeProtector(row).Protect(JsonSerializer.Serialize(source with { Status = row.Status, Version = row.Version }));
        if (!alreadyDisconnected) { binding.Status = "Disconnected"; binding.Generation++; binding.Version++; binding.UpdatedAtUtc = now; }
        else db.Entry(binding).Property(x => x.Version).IsModified = true;
        var revocation = new MeetingCredentialRevocation { AcademyId = academy, ActorUserId = source.Actor, ExchangeId = source.Id, BindingId = source.Binding,
            Provider = source.Provider, Generation = binding.Generation, BindingVersion = binding.Version, Fence = Guid.NewGuid(), RegistrationFingerprint = source.Registration,
            DeadlineUtc = now, Status = "ManualRequired", ProtectedEnvelope = "" };
        // A different protected scope from Google's project-grant consent: no
        // provider-side revocation was authorized or attempted by this operation.
        var e = new RevocationEnvelope(revocation.Id, academy, revocation.ActorUserId, source.Id, source.Binding, source.Provider, revocation.Generation,
            revocation.BindingVersion, revocation.Fence, source.Registration, source.SecurityStampHash, revocation.DeadlineUtc, revocation.Status, 0, UnknownRefreshManualScope);
        revocation.ProtectedEnvelope = RevocationProtector(revocation).Protect(JsonSerializer.Serialize(e));
        var abandoned = new AuditLog { AcademyId = academy, ActorUserId = source.Actor, Action = "MeetingUnknownRefreshAbandonedLocally", EntityType = "MeetingOAuthExchange", EntityId = source.Id };
        var disconnected = new AuditLog { AcademyId = academy, ActorUserId = source.Actor, Action = alreadyDisconnected ? "MeetingUnknownRefreshLocalDisconnectRetained" : "MeetingDisconnectedLocallyRevocationRequired", EntityType = "MeetingProviderBinding", EntityId = source.Binding };
        var manual = new AuditLog { AcademyId = academy, ActorUserId = source.Actor, Action = "MeetingUnknownRefreshFamilyManualRequired", EntityType = "MeetingCredentialRevocation", EntityId = revocation.Id };
        db.AddRange(revocation, abandoned, disconnected, manual);
        try { await db.SaveChangesAsync(token); }
        catch (DbUpdateException ex) when (ex is DbUpdateConcurrencyException || ex.InnerException is SqlException { Number: 2601 or 2627 })
        { Detach(row, binding, revocation, abandoned, disconnected, manual); return new(409, "ConcurrentUnknownRefreshDisconnect"); }
        return new(202, "LocallyDisconnectedRefreshFamilyRevocationManualRequired");
    }
}
