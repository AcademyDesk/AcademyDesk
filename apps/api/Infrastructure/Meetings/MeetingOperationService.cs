using System.Security.Claims;
using System.Text.Json.Serialization;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Infrastructure.Meetings;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record MeetingAdmissionRequest(Guid ReceiptId, int ExpectedVersion, string? RequestHash, string? SourceVersion);
public sealed record MeetingOperationStatus(Guid Id, string Status, int Version, DateTime UpdatedAtUtc,
    DateTimeOffset AsOfUtc, string ExternalOutcome, bool RequiresReview, bool CanExecute = false, bool CanRetry = false);
public sealed record MeetingOperationOutcome(int StatusCode, string Code, MeetingOperationStatus? Operation = null);

/// <summary>
/// Current host authority and safe status projection. Admission is DENIAL ONLY:
/// no provider/real execution approval is qualified. No queue/store/adapter is
/// injected, and no capability/configuration/model claim can enable a write.
/// </summary>
public sealed class MeetingOperationService(AcademyDeskDbContext db, UserManager<ApplicationUser> users,
    MeetingReadinessService readiness, MeetingReceiptService receipts, TimeProvider clock)
{
    private static bool ValidHash(string? value) => value is { Length: 64 } && value.All(Uri.IsHexDigit);
    public async Task<MeetingOperationOutcome> RequestCreationAsync(ClaimsPrincipal actor, Guid academy,
        MeetingAdmissionRequest request, string? key, CancellationToken token)
    {
        var authority = await readiness.ReadAsync(actor, academy, token);
        if (authority.StatusCode != 200) return new(authority.StatusCode, "AccessDenied");
        if (request.ReceiptId == Guid.Empty || request.ExpectedVersion < 0 || !ValidHash(request.RequestHash) || !ValidHash(request.SourceVersion))
            return new(400, "InvalidMeetingRequest");
        if (key is null || key.Length is < 8 or > 64 || key.Any(c => !char.IsAsciiLetterOrDigit(c) && c is not '-' and not '_'))
            return new(400, "InvalidIdempotencyKey");
        // Authenticated receipt checks include actor, tenant, protected integrity,
        // expiry and current source eligibility. A reviewed draft is NOT approval.
        var result = await receipts.GetAsync(actor, academy, request.ReceiptId, token);
        if (result.StatusCode != 200 || result.Receipt is null) return new(result.StatusCode, result.Code);
        var receipt = result.Receipt;
        if (receipt.Version != request.ExpectedVersion || receipt.RequestHash != request.RequestHash || receipt.SourceVersion != request.SourceVersion)
            return new(409, "StaleMeetingRequest");
        if (receipt.Status != "ReviewedDraft") return new(409, "DraftReviewRequired");
        // Deliberate terminal gate. Real approval/connection/admission must be
        // implemented and qualified together; toggling readiness alone is unsafe.
        return new(503, "MeetingExecutionUnavailable");
    }

    public async Task<MeetingOperationOutcome> GetAsync(ClaimsPrincipal actor, Guid academy, Guid id, CancellationToken token)
    {
        var authority = await readiness.ReadAsync(actor, academy, token);
        if (authority.StatusCode != 200) return new(authority.StatusCode, "AccessDenied");
        var user = await users.GetUserAsync(actor);
        if (user is null) return new(401, "AccessDenied");
        // Initiator-only like draft receipts; another academy admin is not an
        // automatic approver or delegate. Never materialize the private envelope.
        var saved = await (
            from operation in db.MeetingOperations.AsNoTracking()
            where operation.AcademyId == academy && operation.ActorUserId == user.Id && operation.Id == id
            join box in db.MeetingOutbox.AsNoTracking() on new { operation.AcademyId, OperationId = operation.Id }
                equals new { box.AcademyId, box.OperationId } into boxes
            from box in boxes.DefaultIfEmpty()
            select new { operation.Id, operation.Status, operation.Version, operation.CreatedAtUtc, operation.UpdatedAtUtc,
                BoxVersion = box == null ? (int?)null : box.Version, LeaseId = box == null ? (Guid?)null : box.LeaseId,
                LeaseExpires = box == null ? (DateTime?)null : box.LeaseExpiresAtUtc }).SingleOrDefaultAsync(token);
        if (saved is null) return new(404, "OperationNotFound");
        var leased = saved.Status is "Leased" or "Dispatching";
        var known = leased || saved.Status is "Held" or "PendingDispatch" or "AwaitingReview";
        var leaseValid = leased ? saved.LeaseId is not null && saved.LeaseId != Guid.Empty && saved.LeaseExpires is not null
            : saved.LeaseId is null && saved.LeaseExpires is null;
        if (!known || saved.Version < 0 || saved.BoxVersion != saved.Version || !leaseValid) return new(409, "OperationStateUnavailable");
        return new(200, "StoredOperationStatus", new(saved.Id, saved.Status, saved.Version,
            DateTime.SpecifyKind(saved.UpdatedAtUtc ?? saved.CreatedAtUtc, DateTimeKind.Utc), clock.GetUtcNow(),
            saved.Status is "Dispatching" or "AwaitingReview" ? "Unknown" : "NotConfirmed", saved.Status == "AwaitingReview"));
    }
}
