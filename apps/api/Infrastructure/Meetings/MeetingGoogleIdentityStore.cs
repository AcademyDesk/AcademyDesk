using System.Security.Claims;
using System.Text.Json;
using AcademyDesk.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Infrastructure.Meetings;

internal sealed partial class MeetingOAuthAttemptStore
{
    internal async Task<MeetingGoogleIdentityOutcome> CheckGoogleIdentityAsync(ClaimsPrincipal actor, Guid academy, MeetingOAuthRegistration registration,
        Guid reviewId, MeetingAccountPolicyRegistry policies, MeetingGoogleIdentityVerifier verifier, CancellationToken token = default)
    {
        if (registration.Provider != "GoogleWorkspace") return new(503, "GoogleIdentityProfileUnavailable");
        var policyCheck = await CheckAccountPolicyAsync(actor, academy, registration, reviewId, policies, token);
        if (!policyCheck.AccountPolicyMatched) return new(policyCheck.StatusCode, policyCheck.Code);
        var row = await db.MeetingAccountReviews.SingleAsync(x => x.Id == reviewId, token); var review = UnsealAccountReview(row);
        if (review is null) return new(409, "AccountReviewSourceChanged");
        var exchange = await db.MeetingOAuthExchanges.SingleAsync(x => x.Id == review.Exchange && x.AcademyId == academy, token);
        await db.Entry(exchange).ReloadAsync(token); var source = UnsealExchange(exchange); var version = exchange.Version;
        var attempt = await db.MeetingOAuthAttempts.AsNoTracking().SingleOrDefaultAsync(x => x.Id == exchange.AttemptId && x.AcademyId == academy && x.ActorUserId == review.Actor && x.Provider == registration.Provider, token);
        var intent = attempt is null ? null : Unseal(attempt);
        if (source?.Tokens?.IdentityToken is null) return new(503, "FreshGoogleIdentityEvidenceRequired");
        if (intent is null || intent.Status != "Claimed" || intent.Version != 2 || !ValidFingerprint(intent.ClaimedCodeHash) ||
            intent.Id != source.Attempt || intent.ActorUserId != source.Actor || intent.BindingVersion + 1 != source.BindingVersion || intent.RegistrationFingerprint != registration.Fingerprint ||
            intent.BindingId != source.Binding || intent.Generation != source.Generation || intent.SecurityStampHash != source.SecurityStampHash ||
            source.Status != "TokensQuarantined" || source.Registration != review.Registration || source.Fence != review.Fence || source.Tokens.Expires <= clock.GetUtcNow())
            return new(409, "GoogleIdentitySourceChanged");
        var identity = await verifier.VerifyAsync(registration, source.Tokens.IdentityToken, review.Subject, GoogleIdentityNonce(intent), source.Tokens.Access, token);
        if (!identity.IdentityVerified) return identity;
        // Network/signature work cannot preserve old authority or stale source.
        var current = await CheckAccountPolicyAsync(actor, academy, registration, reviewId, policies, token);
        if (!current.AccountPolicyMatched || current.PolicyFingerprint != policyCheck.PolicyFingerprint) return new(current.AccountPolicyMatched ? 409 : current.StatusCode, "GoogleIdentityContextChanged");
        await db.Entry(exchange).ReloadAsync(token); var final = UnsealExchange(exchange);
        if (final?.Tokens is null || exchange.Version != version || final.Fence != source.Fence || final.Status != source.Status ||
            final.Tokens.IdentityToken != source.Tokens.IdentityToken || final.Tokens.Access != source.Tokens.Access ||
            identity.Expires <= clock.GetUtcNow()) return new(409, "GoogleIdentitySourceChanged");
        var binding = await CurrentExchangeBinding(academy, review.Binding, review.Provider, review.Generation, review.BindingVersion, token);
        if (binding is null) return new(409, "ConnectionGenerationChanged");
        db.Entry(exchange).Property(x => x.Version).IsModified = true;
        db.Entry(row).Property(x => x.Version).IsModified = true;
        db.Entry(binding).Property(x => x.Version).IsModified = true;
        var audit = new AuditLog { AcademyId = academy, ActorUserId = review.Actor, EntityType = "MeetingAccountReview", EntityId = reviewId,
            Action = "MeetingGoogleIdentityVerifiedEntitlementRequired", MetadataJson = JsonSerializer.Serialize(new { contract = "meeting-google-identity/1",
                policy = current.PolicyFingerprint, registration = registration.Fingerprint, credentialVersion = version,
                tokenFingerprint = Hash(source.Tokens.IdentityToken), identityVerified = true, providerVerified = false }) };
        db.Add(audit);
        try { await db.SaveChangesAsync(token); }
        catch (DbUpdateConcurrencyException) { Detach(row, exchange, binding, audit); return new(409, "ConcurrentGoogleIdentityCheck"); }
        // Identity is not domain ownership, provider consent or meeting entitlement.
        return new(503, "GoogleConsentAndEntitlementEvidenceRequired", true, identity.Expires);
    }
}
