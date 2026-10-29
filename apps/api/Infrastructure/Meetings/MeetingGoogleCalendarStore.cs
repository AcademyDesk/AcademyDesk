using System.Security.Claims;
using System.Text.Json;
using AcademyDesk.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Infrastructure.Meetings;

internal sealed partial class MeetingOAuthAttemptStore
{
    internal async Task<MeetingGoogleCalendarOutcome> CheckGoogleCalendarAsync(ClaimsPrincipal actor, Guid academy, MeetingOAuthRegistration registration,
        Guid reviewId, MeetingAccountPolicyRegistry policies, MeetingGoogleIdentityVerifier identityVerifier, MeetingGoogleCalendarProbe calendarProbe, CancellationToken token = default)
    {
        var authority = await Authorize(actor, academy, token);
        if (authority.Code != 200) return new(authority.Code, "AccessDenied");
        if (registration.Provider != "GoogleWorkspace" || registration.Flow?.Id != MeetingOAuthFlowProfile.GoogleMetadataProfile)
            return new(503, "GoogleCalendarMetadataConsentRequired");
        var identity = await CheckGoogleIdentityAsync(actor, academy, registration, reviewId, policies, identityVerifier, token);
        if (!identity.IdentityVerified || identity.Expires is null || identity.Expires <= clock.GetUtcNow()) return new(identity.StatusCode, identity.Code);
        var row = await db.MeetingAccountReviews.SingleAsync(x => x.Id == reviewId && x.AcademyId == academy, token); var review = UnsealAccountReview(row);
        if (review is null) return new(409, "GoogleCalendarSourceChanged");
        var exchange = await db.MeetingOAuthExchanges.SingleAsync(x => x.Id == review.Exchange && x.AcademyId == academy, token);
        await db.Entry(exchange).ReloadAsync(token); var source = UnsealExchange(exchange); var version = exchange.Version;
        if (source?.Tokens is null || source.Status != "TokensQuarantined" || source.Tokens.IdentityToken is null ||
            source.Fence != review.Fence || source.BindingVersion != review.BindingVersion || source.Generation != review.Generation ||
            source.Registration != registration.Fingerprint || registration.Flow.RequiredTokenScopes.Any(s => !source.Tokens.Scopes.Contains(s, StringComparer.Ordinal)))
            return new(409, "GoogleCalendarSourceChanged");
        var policy = policies.Resolve(academy, registration, clock.GetUtcNow());
        if (policy is null) return new(503, "AccountQualificationPolicyUnavailable");
        // Existing audited credential read independently checks current initiator,
        // source fence and token expiry before a bearer can leave quarantine.
        var read = await ReadQuarantinedTokensAsync(actor, academy, registration, new(exchange.Id, exchange.Fence), token);
        if (read.StatusCode != 200 || read.Tokens?.AccessToken != source.Tokens.Access) return new(read.StatusCode == 200 ? 409 : read.StatusCode, "GoogleCalendarSourceChanged");
        var metadata = await calendarProbe.InspectAsync(source.Tokens.Access, token);
        // No provider-derived flags (including denial metadata) survive a changed
        // host context, even if the HTTP response itself was valid.
        var current = await CheckAccountPolicyAsync(actor, academy, registration, reviewId, policies, token);
        if (!current.AccountPolicyMatched || current.PolicyFingerprint != policy.Fingerprint) return new(current.AccountPolicyMatched ? 409 : current.StatusCode, "GoogleCalendarContextChanged");
        await db.Entry(exchange).ReloadAsync(token); var final = UnsealExchange(exchange); var now = clock.GetUtcNow();
        if (final?.Tokens is null || exchange.Version != version || final.Status != source.Status || final.Fence != source.Fence ||
            final.Tokens.Access != source.Tokens.Access || final.Tokens.IdentityToken != source.Tokens.IdentityToken || final.Tokens.Expires <= now || identity.Expires <= now)
            return new(409, "GoogleCalendarSourceChanged");
        if (metadata.StatusCode != 200 || !metadata.MeetSupported || !metadata.MetadataReadAuthorized || !ValidFingerprint(metadata.CalendarFingerprint)) return metadata;
        var binding = await CurrentExchangeBinding(academy, review.Binding, review.Provider, review.Generation, review.BindingVersion, token);
        if (binding is null) return new(409, "ConnectionGenerationChanged");
        db.Entry(exchange).Property(x => x.Version).IsModified = true; db.Entry(row).Property(x => x.Version).IsModified = true; db.Entry(binding).Property(x => x.Version).IsModified = true;
        var audit = new AuditLog { AcademyId = academy, ActorUserId = review.Actor, Action = "MeetingGoogleMeetAdvertisedExecutionQualificationRequired", EntityType = "MeetingAccountReview", EntityId = reviewId,
            MetadataJson = JsonSerializer.Serialize(new { contract = "meeting-google-calendar/1", policy = current.PolicyFingerprint, registration = registration.Fingerprint,
                credentialVersion = version, tokenFingerprint = Hash(source.Tokens.IdentityToken), calendar = metadata.CalendarFingerprint,
                metadataReadAuthorized = true, meetSupported = true, providerVerified = false }) };
        db.Add(audit);
        try { await db.SaveChangesAsync(token); }
        catch (DbUpdateConcurrencyException) { Detach(exchange, row, binding, audit); return new(409, "ConcurrentGoogleCalendarCheck"); }
        // Successful metadata GET and declared scope are not a write test, license
        // proof, domain-admin consent, grant-isolation proof or execution approval.
        return new(503, "GoogleMeetingExecutionQualificationRequired", true, true);
    }
}
