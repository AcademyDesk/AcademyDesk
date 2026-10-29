using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Domain.Identity;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Infrastructure.Meetings;

// Server-only material: never serialize or log state, browser binding or verifier.
internal sealed class MeetingOAuthMaterial(string state, string challenge, string? verifier = null,
    Guid? academy = null, Guid? actor = null, Guid? binding = null, int? bindingVersion = null, int? generation = null,
    string? registration = null, DateTime? expires = null, string? securityStampHash = null, string? identityNonce = null)
{
    [JsonIgnore] public string State { get; } = state;
    [JsonIgnore] public string Challenge { get; } = challenge;
    [JsonIgnore] public string? Verifier { get; } = verifier;
    [JsonIgnore] public Guid? Academy { get; } = academy;
    [JsonIgnore] public Guid? Actor { get; } = actor;
    [JsonIgnore] public Guid? Binding { get; } = binding;
    [JsonIgnore] public int? BindingVersion { get; } = bindingVersion;
    [JsonIgnore] public int? Generation { get; } = generation;
    [JsonIgnore] public string? Registration { get; } = registration;
    [JsonIgnore] public DateTime? Expires { get; } = expires;
    [JsonIgnore] public string? SecurityStampHash { get; } = securityStampHash;
    [JsonIgnore] public string? IdentityNonce { get; } = identityNonce;
    private int exchangeTaken;
    internal bool TryTakeExchange() => Interlocked.CompareExchange(ref exchangeTaken, 1, 0) == 0;
    public override string ToString() => "[OAuth material redacted]";
}
internal sealed record MeetingOAuthOutcome(int StatusCode, string Code, MeetingOAuthMaterial? Material = null,
    [property: JsonIgnore] string? AuthorizationCode = null)
{
    public override string ToString() => $"MeetingOAuthOutcome {{ StatusCode = {StatusCode}, Code = {Code} }}";
}

/// <summary>
/// Internal intent/claim persistence only. Not registered in DI, no HTTP routes,
/// redirects, code/token exchange, credential access or connected-account claims.
/// Future HTTP adapter must generate a secure browser correlation cookie, resolve
/// an approved registration fingerprint server-side and reauthenticate the actor.
/// </summary>
internal sealed partial class MeetingOAuthAttemptStore(AcademyDeskDbContext db, UserManager<ApplicationUser> users,
    MeetingReadinessService readiness, IDataProtectionProvider protection, TimeProvider clock)
{
    private sealed record Envelope(Guid Id, Guid AcademyId, Guid BindingId, Guid ActorUserId,
        string Provider, int Generation, int BindingVersion, string StateHash, string BrowserHash,
        string RegistrationFingerprint, string SecurityStampHash, string Verifier, DateTime ExpiresAtUtc,
        string Status, int Version, DateTime? ClaimedAtUtc, string? ClaimedCodeHash = null);

    private static bool ProviderAllowed(string provider) => provider is "GoogleWorkspace" or "Microsoft365" or "Zoom";
    private static bool ValidRandom(string? value) => value is { Length: 43 } && value.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_');
    private static bool ValidFingerprint(string? value) => value is { Length: 64 } && value.All(Uri.IsHexDigit);
    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    // Domain-separated, unique per protected attempt; never accept a client nonce.
    private static string GoogleIdentityNonce(Envelope e) => WebEncoders.Base64UrlEncode(SHA256.HashData(Encoding.UTF8.GetBytes(
        JsonSerializer.Serialize(new[] { "meeting-google-identity-nonce/1", e.Verifier, e.StateHash, e.RegistrationFingerprint }))));
    private static string MicrosoftIdentityNonce(Envelope e) => WebEncoders.Base64UrlEncode(SHA256.HashData(Encoding.UTF8.GetBytes(
        JsonSerializer.Serialize(new[] { "meeting-microsoft-identity-nonce/1", e.Verifier, e.StateHash, e.RegistrationFingerprint }))));
    private static string? IdentityNonce(Envelope e) => e.Provider switch { "GoogleWorkspace" => GoogleIdentityNonce(e), "Microsoft365" => MicrosoftIdentityNonce(e), _ => null };
    private static bool EqualHash(string a, string b) => a.Length == b.Length && CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(a), Encoding.UTF8.GetBytes(b));
    private IDataProtector Protector(MeetingOAuthAttempt row) => protection.CreateProtector("academy.meeting-oauth-attempt.v1",
        row.AcademyId.ToString("D"), row.ActorUserId.ToString("D"), row.Provider, row.Id.ToString("D"));
    private Envelope? Unseal(MeetingOAuthAttempt row)
    {
        try
        {
            var e = JsonSerializer.Deserialize<Envelope>(Protector(row).Unprotect(row.ProtectedEnvelope));
            return e is not null && e.Id == row.Id && e.AcademyId == row.AcademyId && e.BindingId == row.BindingId &&
                e.ActorUserId == row.ActorUserId && e.Provider == row.Provider && e.Generation == row.Generation &&
                e.StateHash == row.StateHash && e.ExpiresAtUtc == row.ExpiresAtUtc && e.Status == row.Status &&
                e.Version == row.Version && e.ClaimedAtUtc == row.ClaimedAtUtc && ValidRandom(e.Verifier) ? e : null;
        }
        catch (Exception ex) when (ex is CryptographicException or JsonException or ArgumentException) { return null; }
    }
    private async Task<(int Code, ApplicationUser? User)> Authorize(ClaimsPrincipal actor, Guid academy, CancellationToken token)
    {
        var result = await readiness.ReadAsync(actor, academy, token);
        var user = result.StatusCode == 200 && Guid.TryParse(users.GetUserId(actor), out var actorId)
            ? await users.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == actorId, token) : null;
        if (user is not null && string.IsNullOrWhiteSpace(user.SecurityStamp)) return (403, null);
        return user is null && result.StatusCode == 200 ? (401, null) : (result.StatusCode, user);
    }

    // Fingerprint is the future approved server registration (client/callback/scopes/version),
    // NOT an arbitrary browser/model configuration. Browser binding must be server-generated.
    public async Task<MeetingOAuthOutcome> PrepareAsync(ClaimsPrincipal actor, Guid academy, string provider,
        int expectedBindingVersion, string browserBinding, string registrationFingerprint, CancellationToken token = default)
    {
        var authority = await Authorize(actor, academy, token);
        if (authority.Code != 200 || authority.User is null) return new(authority.Code, "AccessDenied");
        if (!ProviderAllowed(provider) || expectedBindingVersion < 0 || !ValidRandom(browserBinding) || !ValidFingerprint(registrationFingerprint))
            return new(400, "InvalidConnectionIntent");
        // A Google project-wide revoke may invalidate a freshly connected grant.
        // Even a successful acknowledgement can precede full remote effect.
        // No current status proves completed project reconciliation: do not
        // reauthorize using elapsed time, a human report or a new fingerprint.
        if (provider == "GoogleWorkspace" && await db.MeetingCredentialRevocations.AnyAsync(x => x.AcademyId == academy && x.Provider == provider &&
            (x.Status == "Started" || x.Status == "OutcomeUnknown" || x.Status == "ManualRequired" || x.Status == "ManualReportedUnverified" || x.Status == "ProviderAcknowledged"), token)) return new(409, "ProviderRevocationNeedsReview");
        var binding = await db.MeetingProviderBindings.SingleOrDefaultAsync(x => x.AcademyId == academy && x.Provider == provider, token);
        if (binding is null && expectedBindingVersion != 0) return new(409, "StaleConnection");
        if (binding is not null && (binding.Version != expectedBindingVersion || binding.Version == int.MaxValue || binding.Generation < 0 || binding.Generation == int.MaxValue ||
            binding.Status is not ("NotConnected" or "AuthorizationPending" or "VerificationRequired" or "Disconnected"))) return new(409, "StaleConnection");
        binding ??= new MeetingProviderBinding { AcademyId = academy, Provider = provider };
        if (db.Entry(binding).State == EntityState.Detached) db.MeetingProviderBindings.Add(binding);
        var state = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        var verifier = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        var now = clock.GetUtcNow().UtcDateTime;
        binding.Generation++; binding.Version++; binding.Status = "AuthorizationPending"; binding.UpdatedAtUtc = now;
        var row = new MeetingOAuthAttempt { AcademyId = academy, BindingId = binding.Id, ActorUserId = authority.User.Id,
            Provider = provider, Generation = binding.Generation, StateHash = Hash(state), ProtectedEnvelope = "", ExpiresAtUtc = now.AddMinutes(5) };
        var envelope = new Envelope(row.Id, academy, row.BindingId, row.ActorUserId, provider, row.Generation,
            binding.Version, row.StateHash, Hash(browserBinding), registrationFingerprint, Hash(authority.User.SecurityStamp ?? ""),
            verifier, row.ExpiresAtUtc, row.Status, row.Version, null);
        row.ProtectedEnvelope = Protector(row).Protect(JsonSerializer.Serialize(envelope));
        var audit = new AuditLog { AcademyId = academy, ActorUserId = row.ActorUserId, Action = "MeetingOAuthIntentPrepared", EntityType = "MeetingOAuthAttempt", EntityId = row.Id };
        db.AddRange(row, audit);
        try { await db.SaveChangesAsync(token); }
        catch (DbUpdateException ex) when (ex is DbUpdateConcurrencyException || ex.InnerException is SqlException { Number: 2601 or 2627 })
        {
            Detach(binding, row, audit); return new(409, "ConcurrentConnectionIntent");
        }
        return new(201, "OAuthIntentOnlyNotConnected", new(state, WebEncoders.Base64UrlEncode(SHA256.HashData(Encoding.ASCII.GetBytes(verifier))),
            identityNonce: IdentityNonce(envelope)));
    }

    public async Task<MeetingOAuthOutcome> ClaimAsync(ClaimsPrincipal actor, Guid academy, string provider, string state,
        string browserBinding, string registrationFingerprint, CancellationToken token = default)
        => await ClaimCoreAsync(actor, academy, provider, state, browserBinding, registrationFingerprint, null, token);

    private async Task<MeetingOAuthOutcome> ClaimCoreAsync(ClaimsPrincipal actor, Guid academy, string provider, string state,
        string browserBinding, string registrationFingerprint, MeetingOAuthCallback? callback, CancellationToken token)
    {
        var authority = await Authorize(actor, academy, token);
        if (authority.Code != 200 || authority.User is null) return new(authority.Code, "AccessDenied");
        if (!ProviderAllowed(provider) || !ValidRandom(state) || !ValidRandom(browserBinding) || !ValidFingerprint(registrationFingerprint))
            return new(400, "InvalidConnectionCallback");
        var stateHash = Hash(state);
        var row = await db.MeetingOAuthAttempts.SingleOrDefaultAsync(x => x.AcademyId == academy && x.ActorUserId == authority.User.Id && x.Provider == provider && x.StateHash == stateHash, token);
        if (row is null) return new(404, "ConnectionIntentNotFound");
        var e = Unseal(row);
        if (e is null) return new(409, "ConnectionIntentIntegrityFailed");
        var now = clock.GetUtcNow().UtcDateTime;
        if (row.ExpiresAtUtc <= now) return new(410, "ConnectionIntentExpired");
        if (row.Status != (callback is null ? "Pending" : "CallbackCaptured") || row.Version != (callback is null ? 0 : 1) || row.ClaimedAtUtc is not null)
            return new(409, "ConnectionIntentAlreadyClaimed");
        if (callback is null && await db.MeetingOAuthCallbacks.AnyAsync(x => x.AttemptId == row.Id, token))
            return new(409, "CapturedCallbackRequiresResume");
        if (!EqualHash(e.BrowserHash, Hash(browserBinding)) || !EqualHash(e.RegistrationFingerprint, registrationFingerprint) ||
            !EqualHash(e.SecurityStampHash, Hash(authority.User.SecurityStamp ?? ""))) return new(409, "ConnectionContextChanged");
        var binding = await db.MeetingProviderBindings.SingleOrDefaultAsync(x => x.AcademyId == academy && x.Id == row.BindingId && x.Provider == provider, token);
        if (binding is null || binding.Generation != row.Generation || binding.Version != e.BindingVersion || binding.Version == int.MaxValue || binding.Status != "AuthorizationPending")
            return new(409, "ConnectionGenerationChanged");
        var captured = callback is null ? null : UnsealCallback(callback);
        if (callback is not null && (captured is null || callback.AttemptId != row.Id || callback.Status != "Captured" || callback.Version != 0 ||
            callback.ExpiresAtUtc != row.ExpiresAtUtc || captured.StateHash != row.StateHash || captured.BrowserHash != e.BrowserHash || captured.Registration != registrationFingerprint))
            return new(409, "CapturedCallbackInvalid");
        row.Status = "Claimed"; row.Version++; row.ClaimedAtUtc = now; row.UpdatedAtUtc = now;
        row.ProtectedEnvelope = Protector(row).Protect(JsonSerializer.Serialize(e with { Status = row.Status, Version = row.Version, ClaimedAtUtc = now,
            ClaimedCodeHash = captured?.AuthorizationCode is { } claimedCode ? Hash(claimedCode) : null }));
        binding.Status = "VerificationRequired"; binding.Version++; binding.UpdatedAtUtc = now;
        var audit = new AuditLog { AcademyId = academy, ActorUserId = row.ActorUserId, Action = "MeetingOAuthIntentClaimedNotConnected", EntityType = "MeetingOAuthAttempt", EntityId = row.Id };
        db.AuditLogs.Add(audit);
        AuditLog? consumedAudit = null;
        if (callback is not null)
        {
            callback.Status = "Consumed"; callback.Version = 1; callback.UpdatedAtUtc = now;
            callback.ProtectedEnvelope = CallbackProtector(callback).Protect(JsonSerializer.Serialize(captured! with { Status = "Consumed", Version = 1, AuthorizationCode = null, Denied = false }));
            consumedAudit = new AuditLog { AcademyId = academy, ActorUserId = row.ActorUserId, Action = "MeetingOAuthCallbackConsumedNotConnected", EntityType = "MeetingOAuthCallback", EntityId = callback.Id };
            db.AuditLogs.Add(consumedAudit);
        }
        try { await db.SaveChangesAsync(token); }
        catch (DbUpdateConcurrencyException)
        {
            Detach(binding, row, audit);
            if (callback is not null) Detach(callback, consumedAudit!);
            return new(409, "ConcurrentConnectionClaim");
        }
        return captured?.Denied == true ? new(400, "ProviderAuthorizationDeclined") :
            new(200, "OAuthClaimOnlyNotConnected", new(state, WebEncoders.Base64UrlEncode(SHA256.HashData(Encoding.ASCII.GetBytes(e.Verifier))), e.Verifier,
                academy, row.ActorUserId, binding.Id, binding.Version, binding.Generation, registrationFingerprint, row.ExpiresAtUtc, e.SecurityStampHash,
                IdentityNonce(e)), captured?.AuthorizationCode);
    }

    private void Detach(params object[] rows) { foreach (var row in rows) db.Entry(row).State = EntityState.Detached; }
}
