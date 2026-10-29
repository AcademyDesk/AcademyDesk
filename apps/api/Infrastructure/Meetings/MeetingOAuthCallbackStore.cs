using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using AcademyDesk.Api.Domain.Entities;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Infrastructure.Meetings;

internal sealed partial class MeetingOAuthAttemptStore
{
    private sealed record CallbackEnvelope(Guid Id, Guid Academy, Guid Actor, Guid Attempt, string Provider,
        string StateHash, string BrowserHash, string Registration, DateTime Expires, string Status, int Version,
        string? AuthorizationCode, bool Denied);
    private IDataProtector CallbackProtector(MeetingOAuthCallback row) => protection.CreateProtector("academy.meeting-oauth-callback.v1",
        row.AcademyId.ToString("D"), row.ActorUserId.ToString("D"), row.Provider, row.AttemptId.ToString("D"), row.Id.ToString("D"));
    private CallbackEnvelope? UnsealCallback(MeetingOAuthCallback row)
    {
        try
        {
            var e = JsonSerializer.Deserialize<CallbackEnvelope>(CallbackProtector(row).Unprotect(row.ProtectedEnvelope));
            return e is not null && e.Id == row.Id && e.Academy == row.AcademyId && e.Actor == row.ActorUserId && e.Attempt == row.AttemptId &&
                e.Provider == row.Provider && e.Expires == row.ExpiresAtUtc && e.Status == row.Status && e.Version == row.Version &&
                (e.Status == "Consumed" ? e.AuthorizationCode is null && !e.Denied : e.Denied ? e.AuthorizationCode is null : ValidCode(e.AuthorizationCode)) ? e : null;
        }
        catch (Exception ex) when (ex is CryptographicException or JsonException or ArgumentException) { return null; }
    }
    private static bool ValidCode(string? value) => value is { Length: > 0 and <= 4096 } && value.All(c => c is >= '!' and <= '~');

    // Called only after protected browser/callback correlation. No fabricated
    // authenticated principal: capture is quarantine, NOT authorization/exchange.
    public async Task<MeetingOAuthOutcome> CaptureAsync(Guid academy, string provider, string state, string browserBinding,
        string registrationFingerprint, string? authorizationCode, bool denied, CancellationToken token = default)
    {
        if (!ProviderAllowed(provider) || !ValidRandom(state) || !ValidRandom(browserBinding) || !ValidFingerprint(registrationFingerprint) ||
            (denied ? authorizationCode is not null : !ValidCode(authorizationCode))) return new(400, "InvalidConnectionCallback");
        var hash = Hash(state);
        var attempt = await db.MeetingOAuthAttempts.SingleOrDefaultAsync(x => x.AcademyId == academy && x.Provider == provider && x.StateHash == hash, token);
        if (attempt is null) return new(404, "ConnectionIntentNotFound");
        var e = Unseal(attempt);
        if (e is null) return new(409, "ConnectionIntentIntegrityFailed");
        var now = clock.GetUtcNow().UtcDateTime;
        if (attempt.ExpiresAtUtc <= now) return new(410, "ConnectionIntentExpired");
        if (attempt.Status != "Pending" || attempt.Version != 0 || !EqualHash(e.BrowserHash, Hash(browserBinding)) || !EqualHash(e.RegistrationFingerprint, registrationFingerprint))
            return new(409, "ConnectionContextChanged");
        var binding = await db.MeetingProviderBindings.SingleOrDefaultAsync(x => x.AcademyId == academy && x.Id == attempt.BindingId && x.Provider == provider, token);
        if (binding is null || binding.Generation != attempt.Generation || binding.Version != e.BindingVersion || binding.Status != "AuthorizationPending")
            return new(409, "ConnectionGenerationChanged");
        if (await db.MeetingOAuthCallbacks.AnyAsync(x => x.AttemptId == attempt.Id, token)) return new(409, "CallbackAlreadyCaptured");
        var row = new MeetingOAuthCallback { AcademyId = academy, ActorUserId = attempt.ActorUserId, AttemptId = attempt.Id,
            Provider = provider, ExpiresAtUtc = attempt.ExpiresAtUtc, ProtectedEnvelope = "" };
        var envelope = new CallbackEnvelope(row.Id, academy, row.ActorUserId, row.AttemptId, provider, hash, e.BrowserHash,
            registrationFingerprint, row.ExpiresAtUtc, row.Status, row.Version, authorizationCode, denied);
        row.ProtectedEnvelope = CallbackProtector(row).Protect(JsonSerializer.Serialize(envelope));
        // Versioned capture fences competing callbacks and the direct claim path;
        // the binding's compare-and-set also fences a concurrent reconnect.
        // Capture does not approve/consume/extend the original five-minute intent.
        attempt.Status = "CallbackCaptured"; attempt.Version = 1; attempt.UpdatedAtUtc = now;
        attempt.ProtectedEnvelope = Protector(attempt).Protect(JsonSerializer.Serialize(e with { Status = attempt.Status, Version = attempt.Version }));
        db.Entry(binding).Property(x => x.Version).IsModified = true;
        var audit = new AuditLog { AcademyId = academy, ActorUserId = null, Action = "MeetingOAuthCallbackCapturedAwaitingAuthentication", EntityType = "MeetingOAuthCallback", EntityId = row.Id };
        db.AddRange(row, audit);
        try { await db.SaveChangesAsync(token); }
        catch (DbUpdateException ex) when (ex is DbUpdateConcurrencyException || ex.InnerException is SqlException { Number: 2601 or 2627 })
        { Detach(attempt, binding, row, audit); return new(409, "ConcurrentCallbackCapture"); }
        return new(202, "CallbackCapturedAuthenticationRequired");
    }

    public async Task<MeetingOAuthOutcome> ResumeAsync(ClaimsPrincipal actor, Guid academy, string provider, string state,
        string browserBinding, string registrationFingerprint, CancellationToken token = default)
    {
        var authority = await Authorize(actor, academy, token);
        if (authority.Code != 200 || authority.User is null) return new(authority.Code, "AccessDenied");
        if (!ProviderAllowed(provider) || !ValidRandom(state) || !ValidRandom(browserBinding) || !ValidFingerprint(registrationFingerprint))
            return new(400, "InvalidConnectionResume");
        var hash = Hash(state);
        var callback = await (from c in db.MeetingOAuthCallbacks
            join a in db.MeetingOAuthAttempts on c.AttemptId equals a.Id
            where c.AcademyId == academy && c.ActorUserId == authority.User.Id && c.Provider == provider && a.StateHash == hash
            select c).SingleOrDefaultAsync(token);
        if (callback is null) return new(404, "CapturedCallbackNotFound");
        return await ClaimCoreAsync(actor, academy, provider, state, browserBinding, registrationFingerprint, callback, token);
    }
}
