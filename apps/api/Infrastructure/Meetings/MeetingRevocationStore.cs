using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using AcademyDesk.Api.Domain.Entities;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Infrastructure.Meetings;

internal sealed class MeetingRevocationMaterial(Guid id, Guid fence, string refresh)
{
    [JsonIgnore] public Guid Id { get; } = id;
    [JsonIgnore] public Guid Fence { get; } = fence;
    [JsonIgnore] public string RefreshToken { get; } = refresh;
    private int sent;
    internal bool TryTake() => Interlocked.CompareExchange(ref sent, 1, 0) == 0;
    public override string ToString() => "[Revocation material redacted]";
}
internal sealed record MeetingRevocationOutcome(int StatusCode, string Code, [property: JsonIgnore] MeetingRevocationMaterial? Material = null)
{
    public bool CanRetry => false;
    public bool Connected => false;
    public override string ToString() => $"MeetingRevocationOutcome {{ StatusCode = {StatusCode}, Code = {Code}, CanRetry = false }}";
}

internal sealed partial class MeetingOAuthAttemptStore
{
    private sealed record RevocationEnvelope(Guid Id, Guid Academy, Guid Actor, Guid Exchange, Guid Binding, string Provider,
        int Generation, int BindingVersion, Guid Fence, string Registration, string Stamp, DateTime DeadlineUtc, string Status, int Version, string ConsentScope);
    private static bool ValidRevocationScope(RevocationEnvelope e) =>
        e.ConsentScope == (e.Provider == "GoogleWorkspace" ? "google-project-grant/1" : "manual-provider-revocation/1") ||
        (e.ConsentScope == UnknownRefreshManualScope && e.Provider is "GoogleWorkspace" or "Microsoft365" or "Zoom" &&
            ((e.Status == "ManualRequired" && e.Version == 0) || (e.Status == "ManualReportedUnverified" && e.Version == 1)));
    private IDataProtector RevocationProtector(MeetingCredentialRevocation row) => protection.CreateProtector("academy.meeting-revocation.v1",
        row.AcademyId.ToString("D"), row.ActorUserId.ToString("D"), row.Provider, row.ExchangeId.ToString("D"), row.Id.ToString("D"));
    private RevocationEnvelope? UnsealRevocation(MeetingCredentialRevocation row)
    {
        try
        {
            var e = JsonSerializer.Deserialize<RevocationEnvelope>(RevocationProtector(row).Unprotect(row.ProtectedEnvelope));
            return e is not null && e.Id == row.Id && e.Academy == row.AcademyId && e.Actor == row.ActorUserId && e.Exchange == row.ExchangeId && e.Binding == row.BindingId &&
                e.Provider == row.Provider && e.Generation == row.Generation && e.BindingVersion == row.BindingVersion && e.Fence == row.Fence && e.Fence != Guid.Empty &&
                e.Registration == row.RegistrationFingerprint && ValidFingerprint(e.Stamp) && e.DeadlineUtc == row.DeadlineUtc && e.Status == row.Status && e.Version == row.Version &&
                ValidRevocationScope(e) ? e : null;
        }
        catch (Exception ex) when (ex is CryptographicException or JsonException or ArgumentException) { return null; }
    }

    // Inactive server coordinator only. Neither flag can come from a model's
    // approval claim. Google requires distinct project-wide grant consent, not
    // generic local-disconnect confirmation. No public UI/route enables this.
    public async Task<MeetingRevocationOutcome> BeginRevocationAsync(ClaimsPrincipal actor, Guid academy, MeetingOAuthRegistration registration,
        MeetingOAuthExchangePermit exchangePermit, int expectedBindingVersion, bool disconnectConfirmed, bool googleProjectGrantConfirmed, CancellationToken token = default)
    {
        var authority = await Authorize(actor, academy, token);
        if (authority.Code != 200 || authority.User is null) return new(authority.Code, "AccessDenied");
        if (registration.Flow is null) return new(503, "ProviderProfileUnavailable");
        if (!disconnectConfirmed || expectedBindingVersion < 1 || (registration.Provider == "GoogleWorkspace" && !googleProjectGrantConfirmed)) return new(400, "RevocationConfirmationRequired");
        var exchange = await db.MeetingOAuthExchanges.AsNoTracking().SingleOrDefaultAsync(x => x.Id == exchangePermit.Id && x.AcademyId == academy && x.ActorUserId == authority.User.Id && x.Provider == registration.Provider, token);
        var source = exchange is null ? null : UnsealExchange(exchange);
        // Expired access tokens do not imply refresh tokens or grants are revoked.
        if (source?.Tokens is null || source.Status != "TokensQuarantined" || source.Fence != exchangePermit.Fence || source.Registration != registration.Fingerprint ||
            source.BindingVersion != expectedBindingVersion || !await FreshExchangeAuthority(actor, academy, source.Actor, source.SecurityStampHash, token)) return new(409, "RevocationContextChanged");
        var binding = await CurrentExchangeBinding(academy, source.Binding, source.Provider, source.Generation, source.BindingVersion, token);
        if (binding is null || binding.Version == int.MaxValue || binding.Generation == int.MaxValue || await db.MeetingCredentialRevocations.AnyAsync(x => x.ExchangeId == source.Id, token)) return new(409, "RevocationContextChanged");
        binding.Status = "Disconnected"; binding.Generation++; binding.Version++; binding.UpdatedAtUtc = clock.GetUtcNow().UtcDateTime;
        // Microsoft broad session logout is NOT per-app revocation. Zoom's current
        // public-PKCE profile has no qualified confidential revoke credentials.
        var automatic = registration.Provider == "GoogleWorkspace";
        var row = new MeetingCredentialRevocation { AcademyId = academy, ActorUserId = source.Actor, ExchangeId = source.Id, BindingId = source.Binding,
            Provider = source.Provider, Generation = binding.Generation, BindingVersion = binding.Version, Fence = Guid.NewGuid(), RegistrationFingerprint = source.Registration,
            DeadlineUtc = clock.GetUtcNow().UtcDateTime.AddSeconds(30), Status = automatic ? "Started" : "ManualRequired", ProtectedEnvelope = "" };
        var e = new RevocationEnvelope(row.Id, academy, row.ActorUserId, row.ExchangeId, row.BindingId, row.Provider, row.Generation, row.BindingVersion, row.Fence,
            row.RegistrationFingerprint, source.SecurityStampHash, row.DeadlineUtc, row.Status, 0, automatic ? "google-project-grant/1" : "manual-provider-revocation/1");
        row.ProtectedEnvelope = RevocationProtector(row).Protect(JsonSerializer.Serialize(e)); // Metadata only: no duplicate stored token.
        var disconnected = new AuditLog { AcademyId = academy, ActorUserId = row.ActorUserId, Action = "MeetingDisconnectedLocallyRevocationRequired", EntityType = "MeetingProviderBinding", EntityId = row.BindingId };
        var audit = new AuditLog { AcademyId = academy, ActorUserId = row.ActorUserId, Action = automatic ? "MeetingRevocationStarted" : "MeetingRevocationManualRequired", EntityType = "MeetingCredentialRevocation", EntityId = row.Id };
        db.AddRange(row, audit, disconnected);
        try { await db.SaveChangesAsync(token); }
        catch (DbUpdateException ex) when (ex is DbUpdateConcurrencyException || ex.InnerException is SqlException { Number: 2601 or 2627 })
        { Detach(binding, row, audit, disconnected); return new(409, "ConcurrentRevocation"); }
        return automatic ? new(201, "RevocationStartedLocallyDisconnected", new(row.Id, row.Fence, source.Tokens.Refresh)) : new(202, "LocallyDisconnectedProviderRevocationManualRequired");
    }

    private async Task<(MeetingCredentialRevocation? Row, RevocationEnvelope? Envelope, MeetingProviderBinding? Binding)> CurrentRevocation(
        ClaimsPrincipal actor, Guid academy, MeetingOAuthRegistration registration, MeetingRevocationMaterial material, CancellationToken token)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var row = await db.MeetingCredentialRevocations.SingleOrDefaultAsync(x => x.Id == material.Id && x.AcademyId == academy && x.Provider == registration.Provider && x.Status == "Started" && x.Version == 0 && x.DeadlineUtc > now, token);
        var e = row is null ? null : UnsealRevocation(row);
        if (e is null || row!.Status != "Started" || row.Version != 0 || e.Fence != material.Fence || row.DeadlineUtc <= clock.GetUtcNow().UtcDateTime ||
            e.Provider != "GoogleWorkspace" || registration.Flow?.IsGoogleIdentityProfile != true || e.Registration != registration.Fingerprint ||
            !await FreshExchangeAuthority(actor, academy, e.Actor, e.Stamp, token)) return (null, null, null);
        var sourceRow = await db.MeetingOAuthExchanges.AsNoTracking().SingleOrDefaultAsync(x => x.Id == e.Exchange && x.AcademyId == academy && x.ActorUserId == e.Actor && x.Provider == e.Provider, token);
        var source = sourceRow is null ? null : UnsealExchange(sourceRow);
        if (source?.Tokens is null || source.Status != "TokensQuarantined" || source.Binding != e.Binding || source.Registration != e.Registration ||
            source.SecurityStampHash != e.Stamp || source.Generation + 1 != e.Generation || source.BindingVersion + 1 != e.BindingVersion || !EqualHash(Hash(source.Tokens.Refresh), Hash(material.RefreshToken))) return (null, null, null);
        var binding = await db.MeetingProviderBindings.SingleOrDefaultAsync(x => x.Id == e.Binding && x.AcademyId == academy && x.Provider == e.Provider &&
            x.Status == "Disconnected" && x.Generation == e.Generation && x.Version == e.BindingVersion, token);
        return binding is null ? (null, null, null) : (row, e, binding);
    }

    internal async Task<bool> MaySendRevocationAsync(ClaimsPrincipal actor, Guid academy, MeetingOAuthRegistration registration, MeetingRevocationMaterial material, CancellationToken token)
        => (await CurrentRevocation(actor, academy, registration, material, token)).Row is not null;

    public async Task<MeetingRevocationOutcome> FinishRevocationAsync(ClaimsPrincipal actor, Guid academy, MeetingOAuthRegistration registration,
        MeetingRevocationMaterial material, bool providerAcknowledged, CancellationToken token = default)
    {
        var authority = await Authorize(actor, academy, token);
        if (authority.Code != 200) return new(authority.Code, "AccessDenied");
        var (row, e, binding) = await CurrentRevocation(actor, academy, registration, material, token);
        if (row is null) return new(409, "RevocationContextChanged");
        row.Status = providerAcknowledged ? "ProviderAcknowledged" : "OutcomeUnknown"; row.Version = 1; row.UpdatedAtUtc = clock.GetUtcNow().UtcDateTime;
        row.ProtectedEnvelope = RevocationProtector(row).Protect(JsonSerializer.Serialize(e! with { Status = row.Status, Version = 1 }));
        db.Entry(binding!).Property(x => x.Version).IsModified = true;
        var audit = new AuditLog { AcademyId = academy, ActorUserId = row.ActorUserId, Action = providerAcknowledged ? "MeetingRevocationProviderAcknowledged" : "MeetingRevocationOutcomeUnknown", EntityType = "MeetingCredentialRevocation", EntityId = row.Id };
        db.AuditLogs.Add(audit);
        try { await db.SaveChangesAsync(token); }
        catch (DbUpdateConcurrencyException) { Detach(row, binding!, audit); return new(409, "ConcurrentRevocation"); }
        return providerAcknowledged ? new(200, "ProviderAcknowledgedLocallyDisconnected") : new(409, "RevocationOutcomeUnknownLocallyDisconnected");
    }

    // Trusted worker evidence recovery only. Never recovers a send permit/token.
    public async Task<bool> RecoverRevocationAsync(Guid academy, Guid id, CancellationToken token = default)
    {
        var row = await db.MeetingCredentialRevocations.SingleOrDefaultAsync(x => x.Id == id && x.AcademyId == academy, token); var e = row is null ? null : UnsealRevocation(row);
        if (e is null || row!.Status != "Started" || row.Version != 0 || row.DeadlineUtc > clock.GetUtcNow().UtcDateTime) return false;
        row.Status = "OutcomeUnknown"; row.Version = 1; row.UpdatedAtUtc = clock.GetUtcNow().UtcDateTime;
        row.ProtectedEnvelope = RevocationProtector(row).Protect(JsonSerializer.Serialize(e with { Status = row.Status, Version = 1 }));
        var audit = new AuditLog { AcademyId = academy, Action = "MeetingRevocationRecoveredUnknown", EntityType = "MeetingCredentialRevocation", EntityId = row.Id };
        db.AuditLogs.Add(audit);
        try { await db.SaveChangesAsync(token); } catch (DbUpdateConcurrencyException) { Detach(row, audit); return false; }
        return true;
    }
}
