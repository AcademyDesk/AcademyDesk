using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Domain.Identity;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Infrastructure.Meetings;

public sealed record MeetingReceiptState(Guid Id, string Status, int Version, DateTime ExpiresAtUtc,
    string RequestHash, string SourceVersion, bool CanExecute = false);
public sealed record MeetingReceiptOutcome(int StatusCode, string Code, MeetingReceiptState? Receipt = null);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ReviewMeetingDraftRequest(int ExpectedVersion, string? RequestHash, string? SourceVersion);

public sealed class MeetingReceiptService(AcademyDeskDbContext db, UserManager<ApplicationUser> users,
    MeetingReadinessService readiness, MeetingPreviewService preview, IDataProtectionProvider protection, TimeProvider clock)
{
    private sealed record Envelope(Guid Id, Guid AcademyId, Guid ActorUserId, string Key,
        string RequestHash, string SourceVersion, DateTime ExpiresAtUtc, string Status, int Version,
        DateTime? ReviewedAtUtc, MeetingPreviewRequest Request);
    private IDataProtector Protector(MeetingPreviewReceipt row) => protection.CreateProtector(
        "academy.meeting-draft-receipt.v1", row.AcademyId.ToString("D"), row.ActorUserId.ToString("D"), row.Id.ToString("D"));
    private static MeetingReceiptState State(MeetingPreviewReceipt row) => new(row.Id, row.Status,
        row.Version, row.ExpiresAtUtc, row.RequestHash, row.SourceVersion);
    private Envelope? Unseal(MeetingPreviewReceipt row)
    {
        try
        {
            var e = JsonSerializer.Deserialize<Envelope>(Protector(row).Unprotect(row.ProtectedEnvelope));
            return e is not null && e.Id == row.Id && e.AcademyId == row.AcademyId && e.ActorUserId == row.ActorUserId &&
                e.Key == row.IdempotencyKey && e.RequestHash == row.RequestHash && e.SourceVersion == row.SourceVersion &&
                e.ExpiresAtUtc == row.ExpiresAtUtc && e.Status == row.Status && e.Version == row.Version && e.ReviewedAtUtc == row.ReviewedAtUtc ? e : null;
        }
        catch (Exception ex) when (ex is CryptographicException or JsonException or ArgumentException) { return null; }
    }
    private Task<MeetingPreviewReceipt?> Find(Guid academy, Guid actor, Guid id, CancellationToken token)
        => db.MeetingPreviewReceipts.SingleOrDefaultAsync(x => x.AcademyId == academy && x.ActorUserId == actor && x.Id == id, token);
    private async Task<(int Status, ApplicationUser? User)> Authorize(ClaimsPrincipal actor, Guid academy, CancellationToken token)
    {
        var result = await readiness.ReadAsync(actor, academy, token);
        return (result.StatusCode, result.StatusCode == 200 ? await users.GetUserAsync(actor) : null);
    }
    public async Task<MeetingReceiptOutcome> PrepareAsync(ClaimsPrincipal actor, Guid academy,
        MeetingPreviewRequest request, string? key, CancellationToken token)
    {
        var authority = await Authorize(actor, academy, token);
        if (authority.Status != 200 || authority.User is null) return new(authority.Status, "AccessDenied");
        if (key is null || key.Length is < 8 or > 64 || key.Any(c => !char.IsAsciiLetterOrDigit(c) && c is not '-' and not '_')) return new(400, "InvalidIdempotencyKey");
        var prepared = await preview.PrepareAsync(actor, academy, request, token);
        if (prepared.Draft is null) return new(prepared.StatusCode, prepared.ReasonCode);
        var draft = prepared.Draft;
        var existing = await db.MeetingPreviewReceipts.SingleOrDefaultAsync(x => x.AcademyId == academy && x.ActorUserId == authority.User.Id && x.IdempotencyKey == key, token);
        if (existing is not null)
        {
            if (Unseal(existing) is null) return new(409, "ReceiptIntegrityFailed");
            if (existing.ExpiresAtUtc <= clock.GetUtcNow().UtcDateTime) return new(410, "ReceiptExpired");
            if (existing.RequestHash != draft.RequestHash || existing.SourceVersion != draft.SourceVersion) return new(409, "IdempotencyConflict");
            return new(200, "ExistingDraftReceipt", State(existing));
        }
        var row = new MeetingPreviewReceipt { AcademyId = academy, ActorUserId = authority.User.Id,
            IdempotencyKey = key, RequestHash = draft.RequestHash, SourceVersion = draft.SourceVersion,
            ExpiresAtUtc = draft.ExpiresAtUtc.UtcDateTime, ProtectedEnvelope = "" };
        var envelope = new Envelope(row.Id, academy, row.ActorUserId, key, row.RequestHash, row.SourceVersion,
            row.ExpiresAtUtc, row.Status, row.Version, null, request);
        row.ProtectedEnvelope = Protector(row).Protect(JsonSerializer.Serialize(envelope));
        var audit = new AuditLog { AcademyId = academy, ActorUserId = row.ActorUserId, Action = "MeetingDraftReceiptPrepared", EntityType = "MeetingPreviewReceipt", EntityId = row.Id };
        db.MeetingPreviewReceipts.Add(row); db.AuditLogs.Add(audit);
        try { await db.SaveChangesAsync(token); }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
        {
            db.Entry(row).State = EntityState.Detached; db.Entry(audit).State = EntityState.Detached;
            return new(409, "ConcurrentReceiptPreparation");
        }
        return new(201, "DraftReceiptPreparedNotExecutable", State(row));
    }
    public async Task<MeetingReceiptOutcome> GetAsync(ClaimsPrincipal actor, Guid academy, Guid id, CancellationToken token)
    {
        var authority = await Authorize(actor, academy, token);
        if (authority.Status != 200 || authority.User is null) return new(authority.Status, "AccessDenied");
        var row = await Find(academy, authority.User.Id, id, token);
        if (row is null) return new(404, "ReceiptNotFound");
        var e = Unseal(row); if (e is null) return new(409, "ReceiptIntegrityFailed");
        if (row.ExpiresAtUtc <= clock.GetUtcNow().UtcDateTime) return new(410, "ReceiptExpired");
        var current = await preview.PrepareAsync(actor, academy, e.Request, token);
        if (current.Draft is null || current.Draft.SourceVersion != row.SourceVersion || current.Draft.RequestHash != row.RequestHash) return new(409, "SourceChanged");
        return new(200, "DraftOnlyNotExecutable", State(row));
    }
    public async Task<MeetingReceiptOutcome> ReviewAsync(ClaimsPrincipal actor, Guid academy, Guid id, ReviewMeetingDraftRequest request, CancellationToken token)
    {
        var authority = await Authorize(actor, academy, token);
        if (authority.Status != 200 || authority.User is null) return new(authority.Status, "AccessDenied");
        var row = await Find(academy, authority.User.Id, id, token); if (row is null) return new(404, "ReceiptNotFound");
        var e = Unseal(row); if (e is null) return new(409, "ReceiptIntegrityFailed");
        if (row.ExpiresAtUtc <= clock.GetUtcNow().UtcDateTime) return new(410, "ReceiptExpired");
        if (request.ExpectedVersion != row.Version || request.RequestHash != row.RequestHash || request.SourceVersion != row.SourceVersion || row.Status != "AwaitingReview") return new(409, "StaleReview");
        var current = await preview.PrepareAsync(actor, academy, e.Request, token);
        if (current.Draft is null || current.Draft.SourceVersion != row.SourceVersion || current.Draft.RequestHash != row.RequestHash) return new(409, "SourceChanged");
        row.Status = "ReviewedDraft"; row.Version++; row.ReviewedAtUtc = clock.GetUtcNow().UtcDateTime;
        row.ProtectedEnvelope = Protector(row).Protect(JsonSerializer.Serialize(e with { Status = row.Status, Version = row.Version, ReviewedAtUtc = row.ReviewedAtUtc }));
        var audit = new AuditLog { AcademyId = academy, ActorUserId = row.ActorUserId, Action = "MeetingDraftReviewedNoExecution", EntityType = "MeetingPreviewReceipt", EntityId = row.Id };
        db.AuditLogs.Add(audit);
        try { await db.SaveChangesAsync(token); }
        catch (DbUpdateConcurrencyException)
        {
            db.Entry(row).State = EntityState.Detached; db.Entry(audit).State = EntityState.Detached;
            return new(409, "ConcurrentReview");
        }
        return new(200, "DraftReviewedNotExecutionApproved", State(row));
    }
}
