using System.Security.Claims;
using System.Text.Json;
using AcademyDesk.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Infrastructure.Meetings;

internal sealed partial class MeetingOAuthAttemptStore
{
    // Private host coordinator only. A policy match is a necessary configuration
    // check, NOT proof of ownership/issuer/directory/consent/license/grant impact.
    // No public DTO/DI/tool/activation/credential release is added by this method.
    internal async Task<MeetingAccountPolicyOutcome> CheckAccountPolicyAsync(ClaimsPrincipal actor, Guid academy, MeetingOAuthRegistration registration,
        Guid reviewId, MeetingAccountPolicyRegistry policies, CancellationToken token = default)
    {
        var authority = await Authorize(actor, academy, token);
        if (authority.Code != 200 || authority.User is null) return new(authority.Code, "AccessDenied");
        var policy = policies.Resolve(academy, registration, clock.GetUtcNow());
        if (policy is null) return new(503, "AccountQualificationPolicyUnavailable");
        var row = await db.MeetingAccountReviews.SingleOrDefaultAsync(x => x.Id == reviewId && x.AcademyId == academy && x.ActorUserId == authority.User.Id && x.Provider == registration.Provider, token);
        if (row is null) return new(404, "AccountReviewNotFound");
        var e = UnsealAccountReview(row); var now = clock.GetUtcNow();
        if (e is null || e.Status != "ConfirmedNotConnected" || e.Version != 1 || e.ConfirmedAtUtc is null || e.Registration != registration.Fingerprint ||
            !await FreshExchangeAuthority(actor, academy, e.Actor, e.Stamp, token)) return new(409, "ConfirmedAccountReviewRequired");
        if (row.ExpiresAtUtc <= now.UtcDateTime) return new(410, "AccountReviewExpired");
        var exchange = await db.MeetingOAuthExchanges.AsNoTracking().SingleOrDefaultAsync(x => x.Id == e.Exchange && x.AcademyId == academy && x.ActorUserId == e.Actor && x.Provider == e.Provider, token);
        var source = exchange is null ? null : UnsealExchange(exchange);
        if (source?.Tokens is null || source.Status != "TokensQuarantined" || source.Fence != e.Fence || source.Registration != e.Registration || source.SecurityStampHash != e.Stamp ||
            source.Binding != e.Binding || source.Generation != e.Generation || source.BindingVersion != e.BindingVersion || source.Tokens.Expires <= now) return new(409, "AccountReviewSourceChanged");
        var binding = await CurrentExchangeBinding(academy, e.Binding, e.Provider, e.Generation, e.BindingVersion, token);
        if (binding is null) return new(409, "ConnectionGenerationChanged");
        var continuity = await AccountContinuityFailure(source, e.Subject, e.Account, token);
        if (continuity is not null) return new(409, continuity);
        if (!string.Equals(e.Subject, policy.Input.Subject, StringComparison.Ordinal) || !string.Equals(e.Account, policy.Input.Account, StringComparison.Ordinal)) return new(409, "AccountQualificationPolicyMismatch");
        if (policies.Resolve(academy, registration, clock.GetUtcNow()) != policy) return new(503, "AccountQualificationPolicyUnavailable");
        db.Entry(row).Property(x => x.Version).IsModified = true; db.Entry(binding).Property(x => x.Version).IsModified = true;
        var audit = new AuditLog { AcademyId = academy, ActorUserId = e.Actor, Action = "MeetingAccountPolicyMatchedEvidenceRequired", EntityType = "MeetingAccountReview", EntityId = row.Id,
            MetadataJson = JsonSerializer.Serialize(new { contract = "meeting-account-policy/1", policy = policy.Fingerprint, revision = policy.Input.Revision, registration = registration.Fingerprint, providerVerified = false }) };
        db.Add(audit);
        try { await db.SaveChangesAsync(token); }
        catch (DbUpdateConcurrencyException) { Detach(row, binding, audit); return new(409, "ConcurrentAccountQualificationCheck"); }
        // Deliberately unavailable even after a match. No evidence verifier or
        // approved provider policy/configuration loader is installed in this slice.
        return new(503, "IndependentProviderEvidenceRequired", true, policy.Fingerprint);
    }
}
