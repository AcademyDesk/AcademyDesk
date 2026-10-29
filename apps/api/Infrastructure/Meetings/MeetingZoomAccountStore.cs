using System.Security.Claims;
using System.Text.Json;
using AcademyDesk.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Infrastructure.Meetings;

internal sealed partial class MeetingOAuthAttemptStore
{
    internal async Task<MeetingZoomAccountOutcome> CheckZoomAccountAsync(ClaimsPrincipal actor, Guid academy, MeetingOAuthRegistration registration,
        Guid reviewId, MeetingAccountPolicyRegistry policies, MeetingZoomAccountProbe probe, CancellationToken token = default)
    {
        var authority = await Authorize(actor, academy, token);
        if (authority.Code != 200) return new(authority.Code, "AccessDenied");
        if (registration.Provider != "Zoom" || registration.Flow?.IsZoomProfile != true) return new(503, "ZoomAccountProfileUnavailable");
        var policy = policies.Resolve(academy, registration, clock.GetUtcNow());
        if (policy is null) return new(503, "AccountQualificationPolicyUnavailable");
        var authorized = await CheckAccountPolicyAsync(actor, academy, registration, reviewId, policies, token);
        if (!authorized.AccountPolicyMatched || authorized.PolicyFingerprint != policy.Fingerprint) return new(authorized.AccountPolicyMatched ? 409 : authorized.StatusCode, authorized.Code);
        var row = await db.MeetingAccountReviews.SingleAsync(x => x.Id == reviewId && x.AcademyId == academy, token); var review = UnsealAccountReview(row);
        if (review is null) return new(409, "ZoomAccountSourceChanged");
        var exchange = await db.MeetingOAuthExchanges.SingleAsync(x => x.Id == review.Exchange && x.AcademyId == academy, token);
        await db.Entry(exchange).ReloadAsync(token); var source = UnsealExchange(exchange); var version = exchange.Version;
        if (source?.Tokens is null || source.Status != "TokensQuarantined" || source.Fence != review.Fence || source.BindingVersion != review.BindingVersion ||
            source.Generation != review.Generation || source.Registration != registration.Fingerprint || registration.Flow.RequiredTokenScopes.Any(s => !source.Tokens.Scopes.Contains(s, StringComparer.Ordinal))) return new(409, "ZoomAccountSourceChanged");
        var read = await ReadQuarantinedTokensAsync(actor, academy, registration, new(exchange.Id, exchange.Fence), token);
        if (read.StatusCode != 200 || read.Tokens?.AccessToken != source.Tokens.Access) return new(read.StatusCode == 200 ? 409 : read.StatusCode, "ZoomAccountSourceChanged");
        var observed = await probe.InspectAsync(source.Tokens.Access, policy.Input.Subject, policy.Input.Account!, token);
        // Even denial/plan metadata cannot be released into a changed host context.
        var current = await CheckAccountPolicyAsync(actor, academy, registration, reviewId, policies, token);
        if (!current.AccountPolicyMatched || current.PolicyFingerprint != policy.Fingerprint) return new(current.AccountPolicyMatched ? 409 : current.StatusCode, "ZoomAccountContextChanged");
        await db.Entry(exchange).ReloadAsync(token); var final = UnsealExchange(exchange);
        if (final?.Tokens is null || exchange.Version != version || final.Status != source.Status || final.Fence != source.Fence ||
            final.Tokens.Access != source.Tokens.Access || final.Tokens.Expires <= clock.GetUtcNow()) return new(409, "ZoomAccountSourceChanged");
        if (observed.StatusCode != 200 || !observed.AccountMatched || !observed.MeetingPlanObserved) return observed;
        var binding = await CurrentExchangeBinding(academy, review.Binding, review.Provider, review.Generation, review.BindingVersion, token);
        if (binding is null) return new(409, "ConnectionGenerationChanged");
        db.Entry(exchange).Property(x => x.Version).IsModified = true; db.Entry(row).Property(x => x.Version).IsModified = true; db.Entry(binding).Property(x => x.Version).IsModified = true;
        var audit = new AuditLog { AcademyId = academy, ActorUserId = review.Actor, Action = "MeetingZoomPlanObservedExecutionQualificationRequired", EntityType = "MeetingAccountReview", EntityId = reviewId,
            MetadataJson = JsonSerializer.Serialize(new { contract = "meeting-zoom-account/1", policy = current.PolicyFingerprint, registration = registration.Fingerprint,
                credentialVersion = version, accountMatched = true, meetingPlanObserved = true, providerVerified = false }) };
        db.Add(audit);
        try { await db.SaveChangesAsync(token); }
        catch (DbUpdateConcurrencyException) { Detach(exchange, row, binding, audit); return new(409, "ConcurrentZoomAccountCheck"); }
        // Opaque bearer / provider plan and role fields never authorize host work.
        // No durable Connected state, reusable execution permit or external write.
        return new(503, "ZoomMeetingExecutionQualificationRequired", true, true);
    }
}
