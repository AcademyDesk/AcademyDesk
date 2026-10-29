using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using AcademyDesk.Api.Domain.Entities;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Infrastructure.Meetings;

internal sealed record MeetingOAuthExchangePermit([property: JsonIgnore] Guid Id, [property: JsonIgnore] Guid Fence)
{
    public override string ToString() => "[OAuth exchange permit redacted]";
}
internal sealed record MeetingOAuthPersistenceOutcome(int StatusCode, string Code,
    [property: JsonIgnore] MeetingOAuthExchangePermit? Permit = null, [property: JsonIgnore] MeetingOAuthTokens? Tokens = null)
{
    public bool CanRetry => false;
    public override string ToString() => $"MeetingOAuthPersistenceOutcome {{ StatusCode = {StatusCode}, Code = {Code}, CanRetry = false }}";
}

// Same scoped DbContext as the intent store. No routes/DI, no provider traffic.
internal sealed partial class MeetingOAuthAttemptStore
{
    private sealed record TokenEnvelope(string Access, string Refresh, DateTimeOffset Expires, string[] Scopes, string? IdentityToken = null);
    private sealed record ExchangeEnvelope(Guid Id, Guid Academy, Guid Actor, Guid Attempt, Guid Binding, string Provider,
        int Generation, int BindingVersion, Guid Fence, string Registration, string SecurityStampHash,
        DateTime DeadlineUtc, string Status, int Version, TokenEnvelope? Tokens);
    private IDataProtector ExchangeProtector(MeetingOAuthExchange row) => protection.CreateProtector("academy.meeting-oauth-exchange.v1",
        row.AcademyId.ToString("D"), row.ActorUserId.ToString("D"), row.Provider, row.AttemptId.ToString("D"), row.Id.ToString("D"));
    private ExchangeEnvelope? UnsealExchange(MeetingOAuthExchange row)
    {
        try
        {
            var e = JsonSerializer.Deserialize<ExchangeEnvelope>(ExchangeProtector(row).Unprotect(row.ProtectedEnvelope));
            return e is not null && e.Id == row.Id && e.Academy == row.AcademyId && e.Actor == row.ActorUserId && e.Attempt == row.AttemptId &&
                e.Binding == row.BindingId && e.Provider == row.Provider && e.Generation == row.Generation && e.BindingVersion == row.BindingVersion &&
                e.Fence == row.Fence && e.Fence != Guid.Empty && e.Registration == row.RegistrationFingerprint && e.DeadlineUtc == row.DeadlineUtc &&
                e.Status == row.Status && e.Version == row.Version && ValidFingerprint(e.SecurityStampHash) &&
                (e.Status is "TokensQuarantined" or "RefreshStarted" or "RefreshUnknown" or "RefreshAbandoned" ? e.Tokens is not null : e.Tokens is null) ? e : null;
        }
        catch (Exception ex) when (ex is CryptographicException or JsonException or ArgumentException) { return null; }
    }
    private async Task<bool> FreshExchangeAuthority(ClaimsPrincipal actor, Guid academy, Guid actorId, string stamp, CancellationToken token)
    {
        if ((await readiness.ReadAsync(actor, academy, token)).StatusCode != 200) return false;
        var user = await users.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == actorId, token);
        var resolved = await users.GetUserAsync(actor);
        var roles = user is null ? Array.Empty<string>() : (await users.GetRolesAsync(user)).ToArray();
        return resolved?.Id == actorId && user is not null && user.IsActive && user.AcademyId == academy && !string.IsNullOrWhiteSpace(user.SecurityStamp) &&
            EqualHash(Hash(user.SecurityStamp), stamp) && (roles.Contains("Owner", StringComparer.OrdinalIgnoreCase) || roles.Contains("AcademyAdmin", StringComparer.OrdinalIgnoreCase));
    }
    private async Task<MeetingProviderBinding?> CurrentExchangeBinding(Guid academy, Guid id, string provider, int generation, int version, CancellationToken token)
    {
        var binding = await db.MeetingProviderBindings.SingleOrDefaultAsync(x => x.AcademyId == academy && x.Id == id && x.Provider == provider &&
            x.Status == "VerificationRequired" && x.Generation == generation && x.Version == version, token);
        return binding is { Status: "VerificationRequired" } && binding.Generation == generation && binding.Version == version ? binding : null;
    }

    public async Task<MeetingOAuthPersistenceOutcome> BeginExchangeAsync(ClaimsPrincipal actor, Guid academy, MeetingOAuthRegistration registration,
        MeetingOAuthOutcome claim, CancellationToken token = default)
    {
        var authority = await Authorize(actor, academy, token);
        if (authority.Code != 200 || authority.User is null) return new(authority.Code, "AccessDenied");
        var m = claim.Material;
        if (registration.Flow is null) return new(503, "ProviderProfileUnavailable");
        if (claim.StatusCode != 200 || claim.Code != "OAuthClaimOnlyNotConnected" || m is null || !ValidRandom(m.State) || !ValidCode(claim.AuthorizationCode) ||
            m.Academy != academy || m.Actor != authority.User.Id || m.Registration != registration.Fingerprint) return new(409, "InvalidExchangeContext");
        var stateHash = Hash(m.State);
        var attempt = await db.MeetingOAuthAttempts.SingleOrDefaultAsync(x => x.AcademyId == academy && x.Provider == registration.Provider && x.ActorUserId == authority.User.Id && x.StateHash == stateHash, token);
        var e = attempt is null ? null : Unseal(attempt); var now = clock.GetUtcNow().UtcDateTime;
        if (e is null || attempt!.Status != "Claimed" || attempt.Version != 2 || e.ExpiresAtUtc <= now || e.ClaimedCodeHash != Hash(claim.AuthorizationCode!) ||
            e.Verifier != m.Verifier || e.RegistrationFingerprint != registration.Fingerprint || e.BindingId != m.Binding || e.Generation != m.Generation || e.BindingVersion + 1 != m.BindingVersion ||
            !await FreshExchangeAuthority(actor, academy, e.ActorUserId, e.SecurityStampHash, token)) return new(409, "InvalidExchangeContext");
        var callback = await db.MeetingOAuthCallbacks.SingleOrDefaultAsync(x => x.AttemptId == attempt.Id, token);
        if (callback is null || UnsealCallback(callback) is null || callback.Status != "Consumed") return new(409, "InvalidExchangeContext");
        var binding = await CurrentExchangeBinding(academy, e.BindingId, registration.Provider, e.Generation, e.BindingVersion + 1, token);
        if (binding is null) return new(409, "ConnectionGenerationChanged");
        if (await db.MeetingOAuthExchanges.AnyAsync(x => x.AttemptId == attempt.Id, token)) return new(409, "ExchangeAlreadyStarted");
        var row = new MeetingOAuthExchange { AcademyId = academy, ActorUserId = e.ActorUserId, AttemptId = attempt.Id, BindingId = e.BindingId,
            Provider = registration.Provider, Generation = e.Generation, BindingVersion = binding.Version, Fence = Guid.NewGuid(),
            RegistrationFingerprint = registration.Fingerprint, DeadlineUtc = now.AddSeconds(30), ProtectedEnvelope = "" };
        var envelope = new ExchangeEnvelope(row.Id, academy, row.ActorUserId, row.AttemptId, row.BindingId, row.Provider, row.Generation, row.BindingVersion,
            row.Fence, row.RegistrationFingerprint, e.SecurityStampHash, row.DeadlineUtc, row.Status, row.Version, null);
        row.ProtectedEnvelope = ExchangeProtector(row).Protect(JsonSerializer.Serialize(envelope));
        db.Entry(binding).Property(x => x.Version).IsModified = true; // reconnect compare-and-set fence, same SQL transaction.
        var audit = new AuditLog { AcademyId = academy, ActorUserId = row.ActorUserId, Action = "MeetingOAuthExchangeStarted", EntityType = "MeetingOAuthExchange", EntityId = row.Id };
        db.AddRange(row, audit);
        try { await db.SaveChangesAsync(token); }
        catch (DbUpdateException ex) when (ex is DbUpdateConcurrencyException || ex.InnerException is SqlException { Number: 2601 or 2627 })
        { Detach(binding, row, audit); return new(409, "ConcurrentExchangeStart"); }
        return new(201, "ExchangeStartedNotConnected", new(row.Id, row.Fence));
    }

    public async Task<MeetingOAuthPersistenceOutcome> FinishExchangeAsync(ClaimsPrincipal actor, Guid academy, MeetingOAuthRegistration registration,
        MeetingOAuthExchangePermit permit, MeetingOAuthExchangeOutcome outcome, CancellationToken token = default)
    {
        var authority = await Authorize(actor, academy, token);
        if (authority.Code != 200 || authority.User is null) return new(authority.Code, "AccessDenied");
        var row = await db.MeetingOAuthExchanges.SingleOrDefaultAsync(x => x.Id == permit.Id && x.AcademyId == academy && x.ActorUserId == authority.User.Id && x.Provider == registration.Provider, token);
        if (row is null) return new(404, "ExchangeNotFound");
        var e = UnsealExchange(row); var now = clock.GetUtcNow().UtcDateTime;
        if (e is null || row.Fence != permit.Fence || row.Status != "Started" || row.Version != 0 || row.DeadlineUtc <= now || e.Registration != registration.Fingerprint ||
            !await FreshExchangeAuthority(actor, academy, e.Actor, e.SecurityStampHash, token)) return new(409, "ExchangeContextChanged");
        var binding = await CurrentExchangeBinding(academy, row.BindingId, row.Provider, row.Generation, row.BindingVersion, token);
        if (binding is null) return new(409, "ConnectionGenerationChanged");
        TokenEnvelope? saved = null;
        if (outcome.StatusCode == 200 && outcome.Code == "TokensReceivedAccountUnverified" && !outcome.OutcomeUnknown && outcome.Tokens is { } tokens && registration.Flow is { } flow)
        {
            var lifetime = (tokens.ExpiresAt - clock.GetUtcNow()).TotalSeconds;
            if (lifetime is < 60 or > 86400) return new(409, "InvalidQuarantinedTokens");
            var seconds = (int)Math.Floor(lifetime);
            var validated = MeetingOAuthCodeExchange.Parse(JsonSerializer.SerializeToUtf8Bytes(new { access_token = tokens.AccessToken,
                refresh_token = tokens.RefreshToken, token_type = "Bearer", expires_in = seconds, scope = string.Join(' ', tokens.Scopes) }), flow, clock.GetUtcNow());
            if (validated.StatusCode != 200) return new(409, "InvalidQuarantinedTokens");
            if (tokens.IdentityToken is not null && (!(flow.IsGoogleIdentityProfile || flow.IsMicrosoftIdentityProfile) || !MeetingOAuthCodeExchange.ValidIdentityToken(tokens.IdentityToken)))
                return new(409, "InvalidQuarantinedTokens");
            saved = new(tokens.AccessToken, tokens.RefreshToken, tokens.ExpiresAt.ToUniversalTime(), tokens.Scopes.ToArray(), tokens.IdentityToken);
        }
        row.Status = saved is null ? "OutcomeUnknown" : "TokensQuarantined"; row.Version = 1; row.UpdatedAtUtc = now;
        row.ProtectedEnvelope = ExchangeProtector(row).Protect(JsonSerializer.Serialize(e with { Status = row.Status, Version = 1, Tokens = saved }));
        db.Entry(binding).Property(x => x.Version).IsModified = true;
        var audit = new AuditLog { AcademyId = academy, ActorUserId = row.ActorUserId, Action = saved is null ? "MeetingOAuthExchangeOutcomeUnknown" : "MeetingOAuthTokensQuarantinedNotConnected", EntityType = "MeetingOAuthExchange", EntityId = row.Id };
        db.AuditLogs.Add(audit);
        try { await db.SaveChangesAsync(token); }
        catch (DbUpdateConcurrencyException) { Detach(row, binding, audit); return new(409, "ConcurrentExchangeFinish"); }
        return saved is null ? new(409, "ExchangeOutcomeUnknown") : new(200, "TokensStoredAccountUnverified");
    }

    public async Task<MeetingOAuthPersistenceOutcome> ReadQuarantinedTokensAsync(ClaimsPrincipal actor, Guid academy, MeetingOAuthRegistration registration,
        MeetingOAuthExchangePermit permit, CancellationToken token = default)
    {
        var authority = await Authorize(actor, academy, token);
        if (authority.Code != 200 || authority.User is null) return new(authority.Code, "AccessDenied");
        var row = await db.MeetingOAuthExchanges.AsNoTracking().SingleOrDefaultAsync(x => x.Id == permit.Id && x.AcademyId == academy && x.ActorUserId == authority.User.Id && x.Provider == registration.Provider, token);
        if (row is null) return new(404, "ExchangeNotFound");
        var e = UnsealExchange(row);
        if (e?.Tokens is not { } tokens || row.Fence != permit.Fence || row.Status != "TokensQuarantined" || row.Version < 1 || row.Version % 2 != 1 || e.Registration != registration.Fingerprint ||
            tokens.Expires <= clock.GetUtcNow() || !await FreshExchangeAuthority(actor, academy, e.Actor, e.SecurityStampHash, token)) return new(409, "QuarantinedTokensUnavailable");
        var binding = await CurrentExchangeBinding(academy, row.BindingId, row.Provider, row.Generation, row.BindingVersion, token);
        if (binding is null) return new(409, "ConnectionGenerationChanged");
        db.Entry(binding).Property(x => x.Version).IsModified = true;
        var audit = new AuditLog { AcademyId = academy, ActorUserId = row.ActorUserId, Action = "MeetingOAuthQuarantinedTokensRead", EntityType = "MeetingOAuthExchange", EntityId = row.Id };
        db.AuditLogs.Add(audit);
        try { await db.SaveChangesAsync(token); }
        catch (DbUpdateConcurrencyException) { Detach(binding!, audit); return new(409, "ConnectionGenerationChanged"); }
        return new(200, "QuarantinedTokensAccountUnverified", Tokens: new(tokens.Access, tokens.Refresh, tokens.Expires, Array.AsReadOnly(tokens.Scopes)));
    }

    // Trusted worker recovery only, no provider call and NEVER requeue/reissue code.
    public async Task<bool> RecoverExpiredExchangeAsync(Guid academy, Guid exchangeId, CancellationToken token = default)
    {
        var row = await db.MeetingOAuthExchanges.SingleOrDefaultAsync(x => x.Id == exchangeId && x.AcademyId == academy, token);
        var e = row is null ? null : UnsealExchange(row);
        if (e is null || row!.Status != "Started" || row.Version != 0 || row.DeadlineUtc > clock.GetUtcNow().UtcDateTime) return false;
        row.Status = "OutcomeUnknown"; row.Version = 1; row.UpdatedAtUtc = clock.GetUtcNow().UtcDateTime;
        row.ProtectedEnvelope = ExchangeProtector(row).Protect(JsonSerializer.Serialize(e with { Status = row.Status, Version = 1 }));
        var audit = new AuditLog { AcademyId = academy, Action = "MeetingOAuthExchangeExpiredOutcomeUnknown", EntityType = "MeetingOAuthExchange", EntityId = row.Id };
        db.AuditLogs.Add(audit);
        try { await db.SaveChangesAsync(token); } catch (DbUpdateConcurrencyException) { Detach(row, audit); return false; }
        return true;
    }
}
