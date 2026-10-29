using System.Security.Claims;
using System.Text.Json;
using AcademyDesk.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Infrastructure.Meetings;

internal sealed partial class MeetingOAuthAttemptStore
{
    internal async Task<MeetingMicrosoftIdentityOutcome> CheckMicrosoftIdentityAsync(ClaimsPrincipal actor, Guid academy, MeetingOAuthRegistration registration,
        Guid reviewId, MeetingAccountPolicyRegistry policies, MeetingMicrosoftIdentityVerifier verifier, HttpMessageInvoker accountTransport, CancellationToken token = default)
    {
        var authority = await Authorize(actor, academy, token);
        if (authority.Code != 200) return new(authority.Code, "AccessDenied");
        if (registration.Provider != "Microsoft365" || registration.Flow?.IsMicrosoftIdentityProfile != true) return new(503, "MicrosoftIdentityConsentRequired");
        var initial = await CheckAccountPolicyAsync(actor, academy, registration, reviewId, policies, token);
        if (!initial.AccountPolicyMatched) return new(initial.StatusCode, initial.Code);
        var row = await db.MeetingAccountReviews.SingleAsync(x => x.Id == reviewId && x.AcademyId == academy, token); var review = UnsealAccountReview(row);
        if (review is null) return new(409, "AccountReviewSourceChanged");
        var exchange = await db.MeetingOAuthExchanges.SingleAsync(x => x.Id == review.Exchange && x.AcademyId == academy, token);
        await db.Entry(exchange).ReloadAsync(token); var source = UnsealExchange(exchange); var version = exchange.Version;
        var attempt = await db.MeetingOAuthAttempts.AsNoTracking().SingleOrDefaultAsync(x => x.Id == exchange.AttemptId && x.AcademyId == academy && x.ActorUserId == review.Actor && x.Provider == registration.Provider, token);
        var intent = attempt is null ? null : Unseal(attempt);
        if (source?.Tokens?.IdentityToken is null) return new(503, "FreshMicrosoftIdentityEvidenceRequired");
        if (intent is null || intent.Status != "Claimed" || intent.Version != 2 || !ValidFingerprint(intent.ClaimedCodeHash) || intent.Id != source.Attempt || intent.ActorUserId != source.Actor ||
            intent.BindingVersion + 1 != source.BindingVersion || intent.BindingId != source.Binding || intent.Generation != source.Generation || intent.SecurityStampHash != source.SecurityStampHash ||
            intent.RegistrationFingerprint != registration.Fingerprint || source.Status != "TokensQuarantined" || source.Fence != review.Fence || source.Registration != review.Registration ||
            source.Tokens.Expires <= clock.GetUtcNow() || registration.Flow.RequiredTokenScopes.Any(s => !source.Tokens.Scopes.Contains(s, StringComparer.Ordinal))) return new(409, "MicrosoftIdentitySourceChanged");
        var signed = await verifier.VerifyAsync(registration, source.Tokens.IdentityToken, review.Subject, MicrosoftIdentityNonce(intent), token);
        if (!signed.IdentityVerified) return signed;
        // Reuse independent pre/post audited credential-read and authority guards.
        // The Graph access token stays opaque; /me proves its current object ID.
        var observed = await new MeetingOAuthAccountProbe(this, accountTransport, clock).InspectAsync(actor, academy, registration, new(exchange.Id, exchange.Fence), token);
        if (observed.StatusCode != 200 || observed.Observation?.Subject != review.Subject || observed.Observation.Account is not null)
            return new(observed.StatusCode == 200 ? 409 : observed.StatusCode, "MicrosoftDirectoryAccountMismatch");
        var current = await CheckAccountPolicyAsync(actor, academy, registration, reviewId, policies, token);
        if (!current.AccountPolicyMatched || current.PolicyFingerprint != initial.PolicyFingerprint) return new(current.AccountPolicyMatched ? 409 : current.StatusCode, "MicrosoftIdentityContextChanged");
        await db.Entry(exchange).ReloadAsync(token); var final = UnsealExchange(exchange); var now = clock.GetUtcNow();
        if (final?.Tokens is null || exchange.Version != version || final.Status != source.Status || final.Fence != source.Fence || final.Tokens.IdentityToken != source.Tokens.IdentityToken ||
            final.Tokens.Access != source.Tokens.Access || final.Tokens.Expires <= now || signed.Expires is null || signed.Expires <= now) return new(409, "MicrosoftIdentitySourceChanged");
        var binding = await CurrentExchangeBinding(academy, review.Binding, review.Provider, review.Generation, review.BindingVersion, token);
        if (binding is null) return new(409, "ConnectionGenerationChanged");
        db.Entry(exchange).Property(x => x.Version).IsModified = true; db.Entry(row).Property(x => x.Version).IsModified = true; db.Entry(binding).Property(x => x.Version).IsModified = true;
        var audit = new AuditLog { AcademyId = academy, ActorUserId = review.Actor, Action = "MeetingMicrosoftIdentityVerifiedEntitlementRequired", EntityType = "MeetingAccountReview", EntityId = reviewId,
            MetadataJson = JsonSerializer.Serialize(new { contract = "meeting-microsoft-identity/1", policy = current.PolicyFingerprint, registration = registration.Fingerprint,
                credentialVersion = version, tokenFingerprint = Hash(source.Tokens.IdentityToken), identityVerified = true, directoryAccountMatched = true, providerVerified = false }) };
        db.Add(audit);
        try { await db.SaveChangesAsync(token); }
        catch (DbUpdateConcurrencyException) { Detach(exchange, row, binding, audit); return new(409, "ConcurrentMicrosoftIdentityCheck"); }
        // No token roles/groups/email/UPN become host permissions. Directory/object
        // matching proves neither academy ownership nor consent/Teams entitlement.
        return new(503, "MicrosoftConsentAndEntitlementEvidenceRequired", true, true, signed.Expires);
    }
}
