using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Infrastructure;

public sealed record EnrollmentMutationOutcome(int HttpStatus, EnrollmentSummary? Result = null, string? Error = null);

/// <summary>Existing deterministic Manual admission operations, shared with the
/// guarded PENTA service. This internal class grants no authority. The caller
/// retains authorization, required audit and outer transaction ownership.</summary>
internal sealed class EnrollmentMutationOperations(AcademyDeskDbContext dbContext)
{
    internal async Task<EnrollmentMutationOutcome> CreateAsync(Guid academyId, CreateEnrollmentRequest request, CancellationToken cancellationToken)
    {
        await using var ownedTransaction = await EnrollmentAdmissionBoundary.BeginOwnedAsync(dbContext, cancellationToken);
        if (!await EnrollmentAdmissionBoundary.TryLockAsync(dbContext, academyId, cancellationToken))
            return new(503, Error: EnrollmentAdmissionBoundary.BusyMessage);
        if (!await dbContext.Students.AnyAsync(x => x.Id == request.StudentId && x.AcademyId == academyId, cancellationToken)) return new(400, Error: "The selected student does not belong to this academy.");
        var batch = await dbContext.Batches.SingleOrDefaultAsync(x => x.Id == request.BatchId && x.AcademyId == academyId, cancellationToken);
        if (batch is null) return new(400, Error: "The selected batch does not belong to this academy.");
        if (!await EnrollmentAdmissionRules.HasCompletedPrerequisitesAsync(dbContext, academyId, request.StudentId, batch.CourseId, cancellationToken))
            return new(409, Error: EnrollmentAdmissionRules.PrerequisiteMessage);
        if (!batch.IsActive || !string.Equals(batch.EnrollmentStatus, "Open", StringComparison.OrdinalIgnoreCase)) return new(400, Error: "This batch is not open for enrolment.");
        var requestedStatus = string.Equals(request.Status, "Waitlisted", StringComparison.OrdinalIgnoreCase) ? "Waitlisted" : "Active";
        var activeCount = await dbContext.Enrollments.CountAsync(x => x.AcademyId == academyId && x.BatchId == request.BatchId && x.Status == "Active", cancellationToken);
        var waitlistCount = await dbContext.Enrollments.CountAsync(x => x.AcademyId == academyId && x.BatchId == request.BatchId && x.Status == "Waitlisted", cancellationToken);
        if (requestedStatus == "Active" && activeCount >= batch.Capacity) return new(409, Error: "This batch has reached its active enrolment capacity. Add the learner to the waitlist or select another batch.");
        if (requestedStatus == "Waitlisted" && (batch.WaitlistCapacity <= 0 || waitlistCount >= batch.WaitlistCapacity)) return new(409, Error: "This batch waitlist is full or not enabled.");
        if (await EnrollmentAdmissionRules.HasOtherAdmissionAsync(dbContext, academyId, request.StudentId, batch.Id, null, cancellationToken))
            return new(409, Error: EnrollmentAdmissionRules.DuplicateMessage);
        var enrollment = new Enrollment { AcademyId = academyId, StudentId = request.StudentId, BatchId = request.BatchId, StartDate = request.StartDate ?? DateOnly.FromDateTime(DateTime.UtcNow), Status = requestedStatus };
        dbContext.Enrollments.Add(enrollment); await dbContext.SaveChangesAsync(cancellationToken);
        if (ownedTransaction is not null) await ownedTransaction.CommitAsync(cancellationToken);
        return new(201, new EnrollmentSummary(enrollment.Id, enrollment.StudentId, enrollment.BatchId, enrollment.StartDate, enrollment.EndDate, enrollment.Status));
    }
    internal async Task<EnrollmentMutationOutcome> UpdateAsync(Guid academyId, Guid enrollmentId, UpdateEnrollmentRequest request, CancellationToken token)
    {
        await using var ownedTransaction = await EnrollmentAdmissionBoundary.BeginOwnedAsync(dbContext, token);
        if (!await EnrollmentAdmissionBoundary.TryLockAsync(dbContext, academyId, token))
            return new(503, Error: EnrollmentAdmissionBoundary.BusyMessage);
        var enrollment = await dbContext.Enrollments.SingleOrDefaultAsync(x => x.Id == enrollmentId && x.AcademyId == academyId, token);
        if (enrollment is null) return new(404);
        var requested = string.IsNullOrWhiteSpace(request.Status) ? enrollment.Status : request.Status.Trim();
        var status = new[] { "Active", "Waitlisted", "Completed", "Withdrawn", "Cancelled", "Paused" }.FirstOrDefault(value => string.Equals(value, requested, StringComparison.OrdinalIgnoreCase));
        if (status is null) return new(400, Error: "Status must be Active, Waitlisted, Completed, Withdrawn, Cancelled, or Paused.");
        if (!string.Equals(status, "Active", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(request.LifecycleReason)) return new(400, Error: "A lifecycle reason is required when changing an enrolment from Active.");
        if ((status is "Active" or "Waitlisted") && !string.Equals(enrollment.Status, status, StringComparison.OrdinalIgnoreCase))
        {
            var batch = await dbContext.Batches.SingleOrDefaultAsync(x => x.Id == enrollment.BatchId && x.AcademyId == academyId, token);
            if (batch is null || !batch.IsActive || !string.Equals(batch.EnrollmentStatus, "Open", StringComparison.OrdinalIgnoreCase))
                return new(409, Error: "This batch is not open for enrolment.");
            if (status == "Active" && await dbContext.Enrollments.CountAsync(x => x.AcademyId == academyId && x.BatchId == batch.Id && x.Status == "Active", token) >= batch.Capacity)
                return new(409, Error: "This batch has reached its active enrolment capacity. Add the learner to the waitlist or select another batch.");
            if (status == "Waitlisted" && (batch.WaitlistCapacity <= 0 || await dbContext.Enrollments.CountAsync(x => x.AcademyId == academyId && x.BatchId == batch.Id && x.Status == "Waitlisted", token) >= batch.WaitlistCapacity))
                return new(409, Error: "This batch waitlist is full or not enabled.");
            if (await EnrollmentAdmissionRules.HasOtherAdmissionAsync(dbContext, academyId, enrollment.StudentId, batch.Id, enrollment.Id, token))
                return new(409, Error: EnrollmentAdmissionRules.DuplicateMessage);
            if (!await EnrollmentAdmissionRules.HasCompletedPrerequisitesAsync(dbContext, academyId, enrollment.StudentId, batch.CourseId, token))
                return new(409, Error: EnrollmentAdmissionRules.PrerequisiteMessage);
        }
        enrollment.Status = status; enrollment.EndDate = request.EndDate; enrollment.LifecycleReason = string.IsNullOrWhiteSpace(request.LifecycleReason) ? null : request.LifecycleReason.Trim();
        await dbContext.SaveChangesAsync(token);
        if (ownedTransaction is not null) await ownedTransaction.CommitAsync(token);
        return new(200, new EnrollmentSummary(enrollment.Id, enrollment.StudentId, enrollment.BatchId, enrollment.StartDate, enrollment.EndDate, enrollment.Status));
    }

    internal async Task<EnrollmentMutationOutcome> TransferAsync(Guid academyId, Guid enrollmentId, TransferEnrollmentRequest request, CancellationToken token)
    {
        await using var ownedTransaction = await EnrollmentAdmissionBoundary.BeginOwnedAsync(dbContext, token);
        if (!await EnrollmentAdmissionBoundary.TryLockAsync(dbContext, academyId, token))
            return new(503, Error: EnrollmentAdmissionBoundary.BusyMessage);
        var source = await dbContext.Enrollments.SingleOrDefaultAsync(x => x.Id == enrollmentId && x.AcademyId == academyId, token);
        var target = await dbContext.Batches.SingleOrDefaultAsync(x => x.Id == request.TargetBatchId && x.AcademyId == academyId, token);
        if (source is null || target is null) return new(404);
        if (source.Status != "Active" || !target.IsActive || !string.Equals(target.EnrollmentStatus, "Open", StringComparison.OrdinalIgnoreCase)) return new(409, Error: "Only active enrolments can transfer to an open batch.");
        if (await dbContext.Enrollments.CountAsync(x => x.AcademyId == academyId && x.BatchId == target.Id && x.Status == "Active", token) >= target.Capacity) return new(409, Error: "Target batch is at capacity.");
        if (await EnrollmentAdmissionRules.HasOtherAdmissionAsync(dbContext, academyId, source.StudentId, target.Id, null, token))
            return new(409, Error: EnrollmentAdmissionRules.DuplicateMessage);
        if (!await EnrollmentAdmissionRules.HasCompletedPrerequisitesAsync(dbContext, academyId, source.StudentId, target.CourseId, token))
            return new(409, Error: EnrollmentAdmissionRules.PrerequisiteMessage);
        source.Status = "Transferred"; source.EndDate = request.TransferDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var replacement = new Enrollment { AcademyId = academyId, StudentId = source.StudentId, BatchId = target.Id, StartDate = source.EndDate.Value, Status = "Active" };
        dbContext.Enrollments.Add(replacement); await dbContext.SaveChangesAsync(token);
        if (ownedTransaction is not null) await ownedTransaction.CommitAsync(token);
        return new(200, new EnrollmentSummary(replacement.Id, replacement.StudentId, replacement.BatchId, replacement.StartDate, replacement.EndDate, replacement.Status));
    }
}
