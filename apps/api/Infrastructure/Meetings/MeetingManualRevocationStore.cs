using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using AcademyDesk.Api.Domain.Entities;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Infrastructure.Meetings;

internal sealed record MeetingManualRevocationReview(Guid Id, int Version, string Provider, string ReviewHash, DateTime ExpiresAtUtc,
    [property: JsonIgnore] string Ticket)
{
    public bool ProviderVerified => false;
    public bool Connected => false;
    public override string ToString() => "[Manual revocation review; provider not verified]";
}
internal sealed record MeetingManualRevocationOutcome(int StatusCode, string Code, MeetingManualRevocationReview? Review = null)
{
    public bool ProviderVerified => false;
    public bool Connected => false;
    public bool CanRetry => false;
}

internal sealed partial class MeetingOAuthAttemptStore
{
    private const string ManualReportPolicy = "manual-revocation-report/1";
    private sealed record ManualReportTicket(string Policy, Guid Id, Guid Academy, Guid Actor, string Registration, string RevocationDigest,
        string SourceDigest, DateTime IssuedAtUtc, DateTime ExpiresAtUtc, string ReviewHash);
    private IDataProtector ManualReportProtector(Guid academy, Guid actor, Guid id) => protection.CreateProtector("academy.meeting-manual-revocation-review.v1",
        academy.ToString("D"), actor.ToString("D"), id.ToString("D"));
    private static string ManualReportHash(ManualReportTicket ticket) => Hash(JsonSerializer.Serialize(ticket with { ReviewHash = "" }));
    private static string ManualReportSourceDigest(ExchangeEnvelope e) => Hash(JsonSerializer.Serialize(new object[] { e.Id, e.Fence, e.Version, e.Status, e.Generation, e.BindingVersion, e.Registration, e.SecurityStampHash }));

    // A human report is NOT independent provider verification or permission to
    // reconnect an unresolved Google project grant. No external call, retry,
    // credential release, free-text evidence/secret upload, route, tool or DI.
    private async Task<(MeetingCredentialRevocation? Row, RevocationEnvelope? Envelope, MeetingProviderBinding? Binding, ExchangeEnvelope? Source)> ManualReportContext(
        ClaimsPrincipal actor, Guid academy, Guid actorId, MeetingOAuthRegistration registration, Guid id, CancellationToken token)
    {
        var row = await db.MeetingCredentialRevocations.SingleOrDefaultAsync(x => x.Id == id && x.AcademyId == academy && x.ActorUserId == actorId && x.Provider == registration.Provider, token);
        var e = row is null ? null : UnsealRevocation(row);
        var unknownFamily = e?.ConsentScope == UnknownRefreshManualScope;
        if (e is null || registration.Flow is null || e.Registration != registration.Fingerprint || (unknownFamily && !SupportsRefresh(registration)) ||
            !(unknownFamily ? e.Status == "ManualRequired" && e.Version == 0 :
                (e.Provider is "Microsoft365" or "Zoom" && e.Status == "ManualRequired" && e.Version == 0) || (e.Provider == "GoogleWorkspace" && e.Status == "OutcomeUnknown" && e.Version == 1)) ||
            !await FreshExchangeAuthority(actor, academy, e.Actor, e.Stamp, token)) return (null, null, null, null);
        var sourceRow = await db.MeetingOAuthExchanges.AsNoTracking().SingleOrDefaultAsync(x => x.Id == e.Exchange && x.AcademyId == academy && x.ActorUserId == actorId && x.Provider == e.Provider, token);
        var source = sourceRow is null ? null : UnsealExchange(sourceRow);
        if (source?.Tokens is null || (unknownFamily ? source.Status != "RefreshAbandoned" || source.Version < 4 || source.Version % 2 != 0 : source.Status != "TokensQuarantined") ||
            source.Binding != e.Binding || source.Registration != e.Registration || source.SecurityStampHash != e.Stamp ||
            source.Generation + 1 != e.Generation || source.BindingVersion + 1 != e.BindingVersion) return (null, null, null, null);
        var binding = await db.MeetingProviderBindings.SingleOrDefaultAsync(x => x.Id == e.Binding && x.AcademyId == academy && x.Provider == e.Provider &&
            x.Status == "Disconnected" && x.Generation == e.Generation && x.Version == e.BindingVersion, token);
        return binding is null ? (null, null, null, null) : (row, e, binding, source);
    }

    internal async Task<MeetingManualRevocationOutcome> PrepareManualRevocationReviewAsync(ClaimsPrincipal actor, Guid academy,
        MeetingOAuthRegistration registration, Guid revocationId, CancellationToken token = default)
    {
        var authority = await Authorize(actor, academy, token);
        if (authority.Code != 200 || authority.User is null) return new(authority.Code, "AccessDenied");
        var (row, e, binding, source) = await ManualReportContext(actor, academy, authority.User.Id, registration, revocationId, token);
        if (row is null) return new(409, "ManualRevocationContextChanged");
        var now = clock.GetUtcNow().UtcDateTime;
        var ticket = new ManualReportTicket(ManualReportPolicy, row.Id, academy, authority.User.Id, registration.Fingerprint, Hash(JsonSerializer.Serialize(e)), ManualReportSourceDigest(source!), now, now.AddMinutes(5), "");
        ticket = ticket with { ReviewHash = ManualReportHash(ticket) };
        // No-op concurrency fences keep the private review read and audit atomic
        // with respect to a concurrent report/reconnect, without advancing state.
        db.Entry(row).Property(x => x.Version).IsModified = true; db.Entry(binding!).Property(x => x.Version).IsModified = true;
        var audit = new AuditLog { AcademyId = academy, ActorUserId = row.ActorUserId, Action = "MeetingManualRevocationReviewPrepared", EntityType = "MeetingCredentialRevocation", EntityId = row.Id }; db.Add(audit);
        try { await db.SaveChangesAsync(token); }
        catch (DbUpdateConcurrencyException) { Detach(row, binding!, audit); return new(409, "ConcurrentManualRevocationReview"); }
        var opaque = ManualReportProtector(academy, authority.User.Id, row.Id).Protect(JsonSerializer.Serialize(ticket));
        return new(200, "ManualReportRequiresExplicitHumanDeclaration", new(row.Id, row.Version, row.Provider, ticket.ReviewHash, ticket.ExpiresAtUtc, opaque));
    }

    internal async Task<MeetingManualRevocationOutcome> ReportManualRevocationAsync(ClaimsPrincipal actor, Guid academy, MeetingOAuthRegistration registration,
        Guid revocationId, int expectedVersion, string reviewHash, string ticket, bool humanReportedRemoval, CancellationToken token = default)
    {
        var authority = await Authorize(actor, academy, token);
        if (authority.Code != 200 || authority.User is null) return new(authority.Code, "AccessDenied");
        if (!humanReportedRemoval || expectedVersion is < 0 or > 1 || !ValidFingerprint(reviewHash) || ticket is not { Length: > 0 and <= 16384 }) return new(400, "ExplicitManualReportRequired");
        var (row, e, binding, source) = await ManualReportContext(actor, academy, authority.User.Id, registration, revocationId, token);
        if (row is null || row.Version != expectedVersion) return new(409, "ManualRevocationContextChanged");
        ManualReportTicket? t;
        try { t = JsonSerializer.Deserialize<ManualReportTicket>(ManualReportProtector(academy, authority.User.Id, row.Id).Unprotect(ticket)); }
        catch (Exception ex) when (ex is CryptographicException or JsonException or ArgumentException) { return new(409, "InvalidManualReportReview"); }
        var now = clock.GetUtcNow().UtcDateTime;
        if (t is null || t.Policy != ManualReportPolicy || t.Id != row.Id || t.Academy != academy || t.Actor != authority.User.Id || t.Registration != registration.Fingerprint ||
            !ValidFingerprint(t.RevocationDigest) || !ValidFingerprint(t.SourceDigest) || !EqualHash(t.RevocationDigest, Hash(JsonSerializer.Serialize(e))) || !EqualHash(t.SourceDigest, ManualReportSourceDigest(source!)) ||
            !ValidFingerprint(t.ReviewHash) || !EqualHash(t.ReviewHash, ManualReportHash(t)) || !EqualHash(t.ReviewHash, reviewHash) ||
            t.IssuedAtUtc > now || t.ExpiresAtUtc - t.IssuedAtUtc != TimeSpan.FromMinutes(5)) return new(409, "InvalidManualReportReview");
        if (t.ExpiresAtUtc <= now) return new(410, "ManualReportReviewExpired");
        row.Status = "ManualReportedUnverified"; row.Version++; row.UpdatedAtUtc = now;
        row.ProtectedEnvelope = RevocationProtector(row).Protect(JsonSerializer.Serialize(e! with { Status = row.Status, Version = row.Version }));
        db.Entry(binding!).Property(x => x.Version).IsModified = true;
        var audit = new AuditLog { AcademyId = academy, ActorUserId = row.ActorUserId, Action = "MeetingRevocationManuallyReportedUnverified", EntityType = "MeetingCredentialRevocation", EntityId = row.Id }; db.Add(audit);
        try { await db.SaveChangesAsync(token); }
        catch (DbUpdateConcurrencyException) { Detach(row, binding!, audit); return new(409, "ConcurrentManualRevocationReport"); }
        return new(200, "ManualRevocationReportedProviderNotVerified");
    }
}
