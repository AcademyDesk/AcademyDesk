using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Infrastructure.Meetings;

/// <summary>
/// Internal persistence/fencing, NOT authorization or a provider adapter. No DI,
/// route, admission method or worker exposes it. A future dispatcher must recheck
/// actor/source/approval/connection. ReviewedDraft cannot become PendingDispatch here.
/// </summary>
internal sealed class MeetingOperationStore(AcademyDeskDbContext db, TimeProvider clock)
{
    public Task<bool> TryClaimAsync(Guid academy, Guid operation, int expectedVersion,
        Guid lease, TimeSpan duration, CancellationToken token = default)
    {
        if (lease == Guid.Empty || duration < TimeSpan.FromSeconds(1) || duration > TimeSpan.FromMinutes(2)) return Task.FromResult(false);
        return Change(academy, operation, expectedVersion, "MeetingOperationLeased", (row, box, now) =>
        {
            if (row.Status != "PendingDispatch" || box.LeaseId is not null || box.LeaseExpiresAtUtc is not null) return false;
            row.Status = "Leased"; box.LeaseId = lease; box.LeaseExpiresAtUtc = now + duration;
            return true;
        }, token);
    }

    // Commit this marker BEFORE any future network call. Unknown outcomes never retry.
    public Task<bool> TryBeginDispatchAsync(Guid academy, Guid operation, int expectedVersion,
        Guid lease, CancellationToken token = default)
        => Change(academy, operation, expectedVersion, "MeetingOperationDispatchStarted", (row, box, now) =>
        {
            if (row.Status != "Leased" || !OwnsLiveLease(box, lease, now)) return false;
            row.Status = "Dispatching"; return true;
        }, token);

    public Task<bool> TryRecordUncertainAsync(Guid academy, Guid operation, int expectedVersion,
        Guid lease, CancellationToken token = default)
        => Change(academy, operation, expectedVersion, "MeetingOperationOutcomeUnknown", (row, box, now) =>
        {
            if (row.Status != "Dispatching" || !OwnsLiveLease(box, lease, now)) return false;
            row.Status = "AwaitingReview"; ClearLease(box); return true;
        }, token);

    public Task<bool> TryRecoverExpiredAsync(Guid academy, Guid operation, int expectedVersion,
        CancellationToken token = default)
        => Change(academy, operation, expectedVersion, "MeetingOperationLeaseRecovered", (row, box, now) =>
        {
            if (box.LeaseId is null || box.LeaseExpiresAtUtc is null || box.LeaseExpiresAtUtc > now) return false;
            if (row.Status == "Leased") row.Status = "PendingDispatch"; // No dispatch marker committed.
            else if (row.Status == "Dispatching") row.Status = "AwaitingReview";
            else return false;
            ClearLease(box); return true;
        }, token);

    private static bool OwnsLiveLease(MeetingOutbox box, Guid lease, DateTime now)
        => lease != Guid.Empty && box.LeaseId == lease && box.LeaseExpiresAtUtc > now;
    private static void ClearLease(MeetingOutbox box) { box.LeaseId = null; box.LeaseExpiresAtUtc = null; }

    private async Task<bool> Change(Guid academy, Guid operation, int expectedVersion, string action,
        Func<MeetingOperation, MeetingOutbox, DateTime, bool> transition, CancellationToken token)
    {
        if (academy == Guid.Empty || operation == Guid.Empty || expectedVersion < 0 || expectedVersion == int.MaxValue) return false;
        var row = await db.MeetingOperations.SingleOrDefaultAsync(x => x.AcademyId == academy && x.Id == operation, token);
        if (row is null || row.Version != expectedVersion) return false;
        var box = await db.MeetingOutbox.SingleOrDefaultAsync(x => x.AcademyId == academy && x.OperationId == operation, token);
        if (box is null || box.Version != expectedVersion) return false;
        var now = clock.GetUtcNow().UtcDateTime;
        if (!transition(row, box, now)) return false;
        row.Version++; box.Version++;
        row.UpdatedAtUtc = box.UpdatedAtUtc = now;
        var audit = new AuditLog { AcademyId = academy, ActorUserId = row.ActorUserId, Action = action,
            EntityType = "MeetingOperation", EntityId = operation };
        db.AuditLogs.Add(audit);
        try { await db.SaveChangesAsync(token); } // SQL commits both fenced rows AND required audit atomically.
        catch (DbUpdateConcurrencyException)
        {
            db.Entry(row).State = EntityState.Detached; db.Entry(box).State = EntityState.Detached;
            db.Entry(audit).State = EntityState.Detached; return false;
        }
        return true;
    }
}
