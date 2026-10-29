using System.Security.Claims;
using System.Text.Json;
using AcademyDesk.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Infrastructure.Meetings;

internal sealed partial class MeetingOAuthAttemptStore
{
    internal async Task<MeetingTeamsLicenseOutcome> CheckTeamsLicenseAsync(ClaimsPrincipal actor, Guid academy, MeetingOAuthRegistration registration, Guid reviewId,
        MeetingAccountPolicyRegistry policies, MeetingMicrosoftIdentityVerifier verifier, HttpMessageInvoker accountTransport, MeetingTeamsLicenseProbe licenseProbe, CancellationToken token = default)
    {
        var identity = await CheckMicrosoftIdentityAsync(actor, academy, registration, reviewId, policies, verifier, accountTransport, token);
        if (!identity.IdentityVerified || !identity.DirectoryAccountMatched || identity.Expires is null || identity.Expires <= clock.GetUtcNow()) return new(identity.StatusCode, identity.Code);
        var row = await db.MeetingAccountReviews.SingleAsync(x => x.Id == reviewId && x.AcademyId == academy, token); var review = UnsealAccountReview(row);
        if (review is null) return new(409, "TeamsLicenseSourceChanged");
        var exchange = await db.MeetingOAuthExchanges.SingleAsync(x => x.Id == review.Exchange && x.AcademyId == academy, token);
        await db.Entry(exchange).ReloadAsync(token); var source = UnsealExchange(exchange); var version = exchange.Version;
        if (source?.Tokens is null || source.Status != "TokensQuarantined" || source.Tokens.IdentityToken is null || source.Fence != review.Fence ||
            source.BindingVersion != review.BindingVersion || source.Generation != review.Generation || source.Registration != registration.Fingerprint ||
            !source.Tokens.Scopes.Contains("User.Read", StringComparer.Ordinal) || !source.Tokens.Scopes.Contains("OnlineMeetings.ReadWrite", StringComparer.Ordinal)) return new(409, "TeamsLicenseSourceChanged");
        var policy = policies.Resolve(academy, registration, clock.GetUtcNow()); if (policy is null) return new(503, "AccountQualificationPolicyUnavailable");
        var read = await ReadQuarantinedTokensAsync(actor, academy, registration, new(exchange.Id, exchange.Fence), token);
        if (read.StatusCode != 200 || read.Tokens?.AccessToken != source.Tokens.Access) return new(read.StatusCode == 200 ? 409 : read.StatusCode, "TeamsLicenseSourceChanged");
        var observed = await licenseProbe.InspectAsync(source.Tokens.Access, token);
        var current = await CheckAccountPolicyAsync(actor, academy, registration, reviewId, policies, token);
        if (!current.AccountPolicyMatched || current.PolicyFingerprint != policy.Fingerprint) return new(current.AccountPolicyMatched ? 409 : current.StatusCode, "TeamsLicenseContextChanged");
        await db.Entry(exchange).ReloadAsync(token); var final = UnsealExchange(exchange); var now = clock.GetUtcNow();
        if (final?.Tokens is null || exchange.Version != version || final.Status != source.Status || final.Fence != source.Fence || final.Tokens.Access != source.Tokens.Access ||
            final.Tokens.IdentityToken != source.Tokens.IdentityToken || final.Tokens.Expires <= now || identity.Expires <= now) return new(409, "TeamsLicenseSourceChanged");
        if (observed.StatusCode != 200 || !observed.LicenseReadAuthorized || !observed.TeamsPlanProvisioned) return observed;
        var binding = await CurrentExchangeBinding(academy, review.Binding, review.Provider, review.Generation, review.BindingVersion, token); if (binding is null) return new(409, "ConnectionGenerationChanged");
        db.Entry(exchange).Property(x => x.Version).IsModified = true; db.Entry(row).Property(x => x.Version).IsModified = true; db.Entry(binding).Property(x => x.Version).IsModified = true;
        var audit = new AuditLog { AcademyId = academy, ActorUserId = review.Actor, Action = "MeetingTeamsPlanObservedExecutionQualificationRequired", EntityType = "MeetingAccountReview", EntityId = reviewId,
            MetadataJson = JsonSerializer.Serialize(new { contract = "meeting-teams-license/1", catalog = MeetingTeamsLicenseProbe.Catalog, policy = current.PolicyFingerprint,
                registration = registration.Fingerprint, credentialVersion = version, tokenFingerprint = Hash(source.Tokens.IdentityToken), teamsPlanProvisioned = true, providerVerified = false }) };
        db.Add(audit);
        try { await db.SaveChangesAsync(token); }
        catch (DbUpdateConcurrencyException) { Detach(exchange, row, binding, audit); return new(409, "ConcurrentTeamsLicenseCheck"); }
        // No external permission/role assignment, meeting write or host approval.
        // Provider plan provisioning does not prove organizer policy/write success.
        return new(503, "TeamsMeetingExecutionQualificationRequired", true, true);
    }
}
