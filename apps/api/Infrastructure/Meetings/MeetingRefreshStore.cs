using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using AcademyDesk.Api.Domain.Entities;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Infrastructure.Meetings;

internal sealed class MeetingRefreshMaterial(Guid id, Guid fence, int version, string refresh, IReadOnlyList<string> scopes)
{
    [JsonIgnore] public Guid Id { get; } = id;
    [JsonIgnore] public Guid Fence { get; } = fence;
    [JsonIgnore] public int Version { get; } = version;
    [JsonIgnore] public string RefreshToken { get; } = refresh;
    [JsonIgnore] public IReadOnlyList<string> Scopes { get; } = scopes;
    private int sent;
    internal bool TryTake() => Interlocked.CompareExchange(ref sent, 1, 0) == 0;
    public override string ToString() => "[Refresh material redacted]";
}
internal sealed record MeetingRefreshOutcome(int StatusCode, string Code,
    [property: JsonIgnore] MeetingRefreshMaterial? Material = null, [property: JsonIgnore] MeetingOAuthExchangePermit? Permit = null)
{
    public bool Connected => false;
    public bool CanRetry => false;
    public override string ToString() => $"MeetingRefreshOutcome {{ StatusCode = {StatusCode}, Code = {Code}, Connected = false, CanRetry = false }}";
}
internal sealed partial class MeetingOAuthAttemptStore
{
    internal static bool SupportsRefresh(MeetingOAuthRegistration registration) => registration.Flow?.IsGoogleIdentityProfile == true || registration.Flow?.IsMicrosoftProfile == true || registration.Flow?.IsZoomConfidentialProfile == true;

    public async Task<MeetingRefreshOutcome> BeginRefreshAsync(ClaimsPrincipal actor, Guid academy, MeetingOAuthRegistration registration,
        MeetingOAuthExchangePermit permit, int expectedVersion, CancellationToken token = default)
    {
        var authority = await Authorize(actor, academy, token);
        if (authority.Code != 200 || authority.User is null) return new(authority.Code, "AccessDenied");
        if (!SupportsRefresh(registration)) return new(503, "ProviderRefreshUnqualified");
        if (expectedVersion < 1 || expectedVersion % 2 != 1 || expectedVersion > int.MaxValue - 2) return new(400, "InvalidRefreshVersion");
        var row = await db.MeetingOAuthExchanges.SingleOrDefaultAsync(x => x.Id == permit.Id && x.AcademyId == academy && x.ActorUserId == authority.User.Id && x.Provider == registration.Provider && x.Status == "TokensQuarantined" && x.Version == expectedVersion, token);
        var e = row is null ? null : UnsealExchange(row);
        // Expired access may be refreshed; a lost/revoked refresh grant is never
        // inferred valid from access expiry, and any provider error remains unknown.
        if (e?.Tokens is null || e.Fence != permit.Fence || e.Registration != registration.Fingerprint ||
            !await FreshExchangeAuthority(actor, academy, e.Actor, e.SecurityStampHash, token)) return new(409, "RefreshContextChanged");
        var binding = await CurrentExchangeBinding(academy, e.Binding, e.Provider, e.Generation, e.BindingVersion, token);
        if (binding is null || binding.Version == int.MaxValue || await db.MeetingCredentialRevocations.AnyAsync(x => x.ExchangeId == row!.Id, token)) return new(409, "RefreshContextChanged");
        row!.Status = "RefreshStarted"; row.Version++; row.Fence = Guid.NewGuid(); row.DeadlineUtc = clock.GetUtcNow().UtcDateTime.AddSeconds(30);
        row.BindingVersion = ++binding.Version; row.UpdatedAtUtc = binding.UpdatedAtUtc = clock.GetUtcNow().UtcDateTime;
        row.ProtectedEnvelope = ExchangeProtector(row).Protect(JsonSerializer.Serialize(e with { Status = row.Status, Version = row.Version, Fence = row.Fence, DeadlineUtc = row.DeadlineUtc, BindingVersion = row.BindingVersion }));
        var audit = new AuditLog { AcademyId = academy, ActorUserId = row.ActorUserId, Action = "MeetingRefreshStarted", EntityType = "MeetingOAuthExchange", EntityId = row.Id };
        db.AuditLogs.Add(audit);
        try { await db.SaveChangesAsync(token); }
        catch (DbUpdateConcurrencyException) { Detach(row, binding, audit); return new(409, "ConcurrentRefresh"); }
        return new(201, "RefreshStartedNotConnected", new(row.Id, row.Fence, row.Version, e.Tokens.Refresh, Array.AsReadOnly(e.Tokens.Scopes)));
    }

    private async Task<(MeetingOAuthExchange? Row, ExchangeEnvelope? Envelope, MeetingProviderBinding? Binding)> CurrentRefresh(
        ClaimsPrincipal actor, Guid academy, MeetingOAuthRegistration registration, MeetingRefreshMaterial material, CancellationToken token)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var row = await db.MeetingOAuthExchanges.SingleOrDefaultAsync(x => x.Id == material.Id && x.AcademyId == academy && x.Provider == registration.Provider && x.Status == "RefreshStarted" && x.Version == material.Version && x.Fence == material.Fence && x.DeadlineUtc > now, token);
        var e = row is null ? null : UnsealExchange(row);
        if (e?.Tokens is null || !SupportsRefresh(registration) || e.Version < 2 || e.Version % 2 != 0 || e.Registration != registration.Fingerprint ||
            !EqualHash(Hash(e.Tokens.Refresh), Hash(material.RefreshToken)) || !e.Tokens.Scopes.SequenceEqual(material.Scopes, StringComparer.Ordinal) ||
            !await FreshExchangeAuthority(actor, academy, e.Actor, e.SecurityStampHash, token)) return (null, null, null);
        var binding = await CurrentExchangeBinding(academy, e.Binding, e.Provider, e.Generation, e.BindingVersion, token);
        return binding is null ? (null, null, null) : (row, e, binding);
    }
    internal async Task<bool> MaySendRefreshAsync(ClaimsPrincipal actor, Guid academy, MeetingOAuthRegistration registration, MeetingRefreshMaterial material, CancellationToken token)
        => (await CurrentRefresh(actor, academy, registration, material, token)).Row is not null;

    public async Task<MeetingRefreshOutcome> FinishRefreshAsync(ClaimsPrincipal actor, Guid academy, MeetingOAuthRegistration registration,
        MeetingRefreshMaterial material, MeetingOAuthExchangeOutcome outcome, CancellationToken token = default)
    {
        var authority = await Authorize(actor, academy, token);
        if (authority.Code != 200) return new(authority.Code, "AccessDenied");
        var (row, e, binding) = await CurrentRefresh(actor, academy, registration, material, token);
        if (row is null) return new(409, "RefreshContextChanged");
        TokenEnvelope? next = null;
        if (outcome.StatusCode == 200 && outcome.Code == "TokensReceivedAccountUnverified" && !outcome.OutcomeUnknown && outcome.Tokens is { } tokens)
        {
            var lifetime = (tokens.ExpiresAt - clock.GetUtcNow()).TotalSeconds;
            if (lifetime is >= 60 and <= 86400 && tokens.Scopes.All(s => e!.Tokens!.Scopes.Contains(s, StringComparer.Ordinal)) &&
                ((registration.Provider != "Microsoft365" && registration.Flow?.IsZoomConfidentialProfile != true) || tokens.RefreshToken != e!.Tokens!.Refresh))
            {
                var valid = MeetingOAuthCodeExchange.Parse(JsonSerializer.SerializeToUtf8Bytes(new { access_token = tokens.AccessToken, refresh_token = tokens.RefreshToken,
                    token_type = "Bearer", expires_in = (int)Math.Floor(lifetime), scope = string.Join(' ', tokens.Scopes) }), registration.Flow!, clock.GetUtcNow());
                if (valid.StatusCode == 200) next = new(tokens.AccessToken, tokens.RefreshToken, tokens.ExpiresAt.ToUniversalTime(), tokens.Scopes.ToArray());
            }
        }
        row.Status = next is null ? "RefreshUnknown" : "TokensQuarantined"; row.Version++; row.UpdatedAtUtc = clock.GetUtcNow().UtcDateTime;
        row.ProtectedEnvelope = ExchangeProtector(row).Protect(JsonSerializer.Serialize(e! with { Status = row.Status, Version = row.Version, Tokens = next ?? e!.Tokens }));
        db.Entry(binding!).Property(x => x.Version).IsModified = true;
        var audit = new AuditLog { AcademyId = academy, ActorUserId = row.ActorUserId, Action = next is null ? "MeetingRefreshOutcomeUnknown" : "MeetingRefreshTokensQuarantined", EntityType = "MeetingOAuthExchange", EntityId = row.Id };
        db.AuditLogs.Add(audit);
        try { await db.SaveChangesAsync(token); }
        catch (DbUpdateConcurrencyException) { Detach(row, binding!, audit); return new(409, "ConcurrentRefresh"); }
        return next is null ? new(409, "RefreshOutcomeUnknown") : new(200, "RefreshedTokensAccountUnverified", Permit: new(row.Id, row.Fence));
    }

    // Trusted recovery only: no send permit, token release or automatic retry.
    public async Task<bool> RecoverRefreshAsync(Guid academy, Guid exchange, CancellationToken token = default)
    {
        var row = await db.MeetingOAuthExchanges.SingleOrDefaultAsync(x => x.Id == exchange && x.AcademyId == academy && x.Status == "RefreshStarted", token);
        var e = row is null ? null : UnsealExchange(row);
        if (e?.Tokens is null || row!.Version < 2 || row.Version % 2 != 0 || row.DeadlineUtc > clock.GetUtcNow().UtcDateTime) return false;
        row.Status = "RefreshUnknown"; row.Version++; row.UpdatedAtUtc = clock.GetUtcNow().UtcDateTime;
        row.ProtectedEnvelope = ExchangeProtector(row).Protect(JsonSerializer.Serialize(e with { Status = row.Status, Version = row.Version }));
        var audit = new AuditLog { AcademyId = academy, Action = "MeetingRefreshRecoveredUnknown", EntityType = "MeetingOAuthExchange", EntityId = row.Id }; db.AuditLogs.Add(audit);
        try { await db.SaveChangesAsync(token); } catch (DbUpdateConcurrencyException) { Detach(row, audit); return false; }
        return true;
    }
}
