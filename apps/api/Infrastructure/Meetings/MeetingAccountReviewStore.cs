using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using AcademyDesk.Api.Domain.Entities;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Infrastructure.Meetings;

internal sealed record MeetingAccountReviewState(Guid Id, int Version, string ReviewHash, string Status, DateTime ExpiresAtUtc,
    [property: JsonIgnore] MeetingOAuthAccountObservation Account)
{
    public bool Connected => false;
    public bool CanExecute => false;
    public override string ToString() => "[Account review; identity redacted; not connected]";
}
internal sealed record MeetingAccountReviewOutcome(int StatusCode, string Code, MeetingAccountReviewState? Review = null);

internal sealed partial class MeetingOAuthAttemptStore
{
    private sealed record AccountReviewEnvelope(Guid Id, Guid Academy, Guid Actor, Guid Exchange, Guid Binding, string Provider,
        int Generation, int BindingVersion, string Registration, string ReviewHash, string Stamp, Guid Fence,
        string Subject, string? Account, DateTimeOffset ObservedAt, DateTime ExpiresAtUtc, string Status, int Version, DateTime? ConfirmedAtUtc);
    private IDataProtector AccountReviewProtector(MeetingAccountReview row) => protection.CreateProtector("academy.meeting-account-review.v1",
        row.AcademyId.ToString("D"), row.ActorUserId.ToString("D"), row.Provider, row.ExchangeId.ToString("D"), row.Id.ToString("D"));
    private static string AccountReviewHash(AccountReviewEnvelope e) => Hash(JsonSerializer.Serialize(new object?[] { e.Id, e.Academy, e.Actor, e.Exchange, e.Binding,
        e.Provider, e.Generation, e.BindingVersion, e.Registration, e.Stamp, e.Fence, e.Subject, e.Account, e.ObservedAt, e.ExpiresAtUtc }));
    private AccountReviewEnvelope? UnsealAccountReview(MeetingAccountReview row)
    {
        try
        {
            var e = JsonSerializer.Deserialize<AccountReviewEnvelope>(AccountReviewProtector(row).Unprotect(row.ProtectedEnvelope));
            return e is not null && e.Id == row.Id && e.Academy == row.AcademyId && e.Actor == row.ActorUserId && e.Exchange == row.ExchangeId && e.Binding == row.BindingId &&
                e.Provider == row.Provider && e.Generation == row.Generation && e.BindingVersion == row.BindingVersion && e.Registration == row.RegistrationFingerprint &&
                e.ReviewHash == row.ReviewHash && ValidFingerprint(e.ReviewHash) && EqualHash(AccountReviewHash(e), e.ReviewHash) && e.ExpiresAtUtc == row.ExpiresAtUtc &&
                e.Status == row.Status && e.Version == row.Version && e.ConfirmedAtUtc == row.ConfirmedAtUtc && e.Fence != Guid.Empty && ValidFingerprint(e.Stamp) &&
                ((e.Status == "AwaitingConfirmation" && e.Version == 0 && e.ConfirmedAtUtc is null) || (e.Status == "ConfirmedNotConnected" && e.Version == 1 && e.ConfirmedAtUtc is not null)) ? e : null;
        }
        catch (Exception ex) when (ex is CryptographicException or JsonException or ArgumentException) { return null; }
    }
    private static MeetingAccountReviewState AccountReviewState(MeetingAccountReview row, AccountReviewEnvelope e) => new(row.Id, row.Version,
        row.ReviewHash, row.Status, DateTime.SpecifyKind(row.ExpiresAtUtc, DateTimeKind.Utc), new(e.Subject, e.Account, e.ObservedAt));

    // A refresh is not an account-switch operation. Keep the first protected
    // observation for this exact exchange as the identity-continuity baseline,
    // even if its confirmation expired or was never completed. It is NOT an
    // ownership, directory, entitlement or authorization grant. A different
    // account needs a separately authorized new OAuth exchange and review.
    private async Task<string?> AccountContinuityFailure(ExchangeEnvelope source, string subject, string? account, CancellationToken token)
    {
        var first = await db.MeetingAccountReviews.AsNoTracking().Where(x => x.ExchangeId == source.Id)
            .OrderBy(x => x.BindingVersion).ThenBy(x => x.Id).FirstOrDefaultAsync(token);
        if (first is null) return null;
        var baseline = UnsealAccountReview(first);
        if (baseline is null || baseline.Academy != source.Academy || baseline.Actor != source.Actor || baseline.Exchange != source.Id || baseline.Binding != source.Binding ||
            baseline.Provider != source.Provider || baseline.Generation != source.Generation || baseline.BindingVersion > source.BindingVersion ||
            baseline.Registration != source.Registration || baseline.Stamp != source.SecurityStampHash) return "AccountContinuityHistoryInvalid";
        return string.Equals(baseline.Subject, subject, StringComparison.Ordinal) && string.Equals(baseline.Account, account, StringComparison.Ordinal)
            ? null : "ProviderAccountChangedReauthorizationRequired";
    }

    // Trusted account-probe handoff only. Future routes/tools must NEVER accept an
    // observation, identity, tenant, permit or raw provider response from a model/UI.
    internal async Task<MeetingAccountReviewOutcome> PrepareAccountReviewAsync(ClaimsPrincipal actor, Guid academy, MeetingOAuthRegistration registration,
        MeetingOAuthExchangePermit permit, MeetingOAuthAccountObservation observed, CancellationToken token = default)
    {
        var authority = await Authorize(actor, academy, token);
        if (authority.Code != 200 || authority.User is null) return new(authority.Code, "AccessDenied");
        var now = clock.GetUtcNow();
        bool Id(string? value) => value is { Length: > 0 and <= 255 } && value.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '.' or '~');
        if (registration.Flow is null || !Id(observed.Subject) || (observed.Account is not null && !Id(observed.Account)) ||
            (registration.Provider == "Zoom" && observed.Account is null) || observed.ObservedAt > now || now - observed.ObservedAt > TimeSpan.FromSeconds(30)) return new(409, "InvalidAccountObservation");
        var exchange = await db.MeetingOAuthExchanges.AsNoTracking().SingleOrDefaultAsync(x => x.Id == permit.Id && x.AcademyId == academy && x.ActorUserId == authority.User.Id && x.Provider == registration.Provider, token);
        var source = exchange is null ? null : UnsealExchange(exchange);
        if (source?.Tokens is null || source.Status != "TokensQuarantined" || source.Fence != permit.Fence || source.Registration != registration.Fingerprint ||
            source.Tokens.Expires <= now || !await FreshExchangeAuthority(actor, academy, source.Actor, source.SecurityStampHash, token)) return new(409, "AccountContextChanged");
        var binding = await CurrentExchangeBinding(academy, source.Binding, source.Provider, source.Generation, source.BindingVersion, token);
        if (binding is null) return new(409, "ConnectionGenerationChanged");
        var continuity = await AccountContinuityFailure(source, observed.Subject, observed.Account, token);
        if (continuity is not null) return new(409, continuity);
        if (await db.MeetingAccountReviews.AnyAsync(x => x.ExchangeId == source.Id && x.BindingVersion == source.BindingVersion, token)) return new(409, "AccountReviewAlreadyExists");
        var expires = now.AddMinutes(5) < source.Tokens.Expires ? now.AddMinutes(5) : source.Tokens.Expires;
        var row = new MeetingAccountReview { AcademyId = academy, ActorUserId = source.Actor, ExchangeId = source.Id, BindingId = source.Binding, Provider = source.Provider,
            Generation = source.Generation, BindingVersion = source.BindingVersion, RegistrationFingerprint = source.Registration,
            ExpiresAtUtc = expires.UtcDateTime, ReviewHash = "", ProtectedEnvelope = "" };
        var e = new AccountReviewEnvelope(row.Id, academy, row.ActorUserId, row.ExchangeId, row.BindingId, row.Provider, row.Generation, row.BindingVersion,
            row.RegistrationFingerprint, "", source.SecurityStampHash, source.Fence, observed.Subject, observed.Account, observed.ObservedAt, row.ExpiresAtUtc, row.Status, row.Version, null);
        e = e with { ReviewHash = AccountReviewHash(e) }; row.ReviewHash = e.ReviewHash;
        row.ProtectedEnvelope = AccountReviewProtector(row).Protect(JsonSerializer.Serialize(e));
        db.Entry(binding).Property(x => x.Version).IsModified = true;
        var audit = new AuditLog { AcademyId = academy, ActorUserId = row.ActorUserId, Action = "MeetingAccountReviewPreparedNotConnected", EntityType = "MeetingAccountReview", EntityId = row.Id };
        db.AddRange(row, audit);
        try { await db.SaveChangesAsync(token); }
        catch (DbUpdateException ex) when (ex is DbUpdateConcurrencyException || ex.InnerException is SqlException { Number: 2601 or 2627 })
        { Detach(row, binding, audit); return new(409, "ConcurrentAccountReview"); }
        return new(201, "AccountReviewRequiresConfirmation", AccountReviewState(row, e));
    }

    public async Task<MeetingAccountReviewOutcome> ReadAccountReviewAsync(ClaimsPrincipal actor, Guid academy, MeetingOAuthRegistration registration,
        Guid reviewId, CancellationToken token = default) => await AccessAccountReview(actor, academy, registration, reviewId, null, null, false, false, token);

    public async Task<MeetingAccountReviewOutcome> ConfirmAccountReviewAsync(ClaimsPrincipal actor, Guid academy, MeetingOAuthRegistration registration,
        Guid reviewId, int expectedVersion, string reviewHash, bool confirmed, CancellationToken token = default)
        => await AccessAccountReview(actor, academy, registration, reviewId, expectedVersion, reviewHash, confirmed, true, token);

    private async Task<MeetingAccountReviewOutcome> AccessAccountReview(ClaimsPrincipal actor, Guid academy, MeetingOAuthRegistration registration,
        Guid reviewId, int? expectedVersion, string? reviewHash, bool confirmed, bool write, CancellationToken token)
    {
        var authority = await Authorize(actor, academy, token);
        if (authority.Code != 200 || authority.User is null) return new(authority.Code, "AccessDenied");
        if (write && (!confirmed || expectedVersion != 0 || !ValidFingerprint(reviewHash))) return new(400, "AccountConfirmationRequired");
        var row = await db.MeetingAccountReviews.SingleOrDefaultAsync(x => x.Id == reviewId && x.AcademyId == academy && x.ActorUserId == authority.User.Id && x.Provider == registration.Provider, token);
        if (row is null) return new(404, "AccountReviewNotFound");
        var e = UnsealAccountReview(row); var now = clock.GetUtcNow();
        if (e is null || registration.Flow is null || e.Registration != registration.Fingerprint || !await FreshExchangeAuthority(actor, academy, e.Actor, e.Stamp, token)) return new(409, "AccountReviewContextChanged");
        if (row.ExpiresAtUtc <= now.UtcDateTime) return new(410, "AccountReviewExpired");
        if (write && (row.Version != expectedVersion || row.Status != "AwaitingConfirmation" || !EqualHash(row.ReviewHash, reviewHash!))) return new(409, "AccountReviewChanged");
        var exchange = await db.MeetingOAuthExchanges.AsNoTracking().SingleOrDefaultAsync(x => x.Id == e.Exchange && x.AcademyId == academy && x.Provider == e.Provider && x.ActorUserId == e.Actor, token);
        var source = exchange is null ? null : UnsealExchange(exchange);
        if (source?.Tokens is null || source.Status != "TokensQuarantined" || source.Fence != e.Fence || source.Registration != e.Registration || source.SecurityStampHash != e.Stamp ||
            source.Binding != e.Binding || source.Generation != e.Generation || source.BindingVersion != e.BindingVersion || source.Tokens.Expires <= now) return new(409, "AccountReviewSourceChanged");
        var binding = await CurrentExchangeBinding(academy, e.Binding, e.Provider, e.Generation, e.BindingVersion, token);
        if (binding is null) return new(409, "ConnectionGenerationChanged");
        var continuity = await AccountContinuityFailure(source, e.Subject, e.Account, token);
        if (continuity is not null) return new(409, continuity);
        if (write)
        {
            row.Status = "ConfirmedNotConnected"; row.Version = 1; row.ConfirmedAtUtc = now.UtcDateTime; row.UpdatedAtUtc = now.UtcDateTime;
            e = e with { Status = row.Status, Version = 1, ConfirmedAtUtc = row.ConfirmedAtUtc };
            row.ProtectedEnvelope = AccountReviewProtector(row).Protect(JsonSerializer.Serialize(e));
        }
        else db.Entry(row).Property(x => x.Version).IsModified = true;
        db.Entry(binding).Property(x => x.Version).IsModified = true;
        var audit = new AuditLog { AcademyId = academy, ActorUserId = row.ActorUserId, Action = write ? "MeetingAccountConfirmedNotConnected" : "MeetingAccountReviewRead", EntityType = "MeetingAccountReview", EntityId = row.Id };
        db.AuditLogs.Add(audit);
        try { await db.SaveChangesAsync(token); }
        catch (DbUpdateConcurrencyException) { Detach(row, binding, audit); return new(409, "ConcurrentAccountReview"); }
        return new(200, write ? "AccountConfirmedNotConnected" : "AccountReviewNotConnected", AccountReviewState(row, e));
    }
}
