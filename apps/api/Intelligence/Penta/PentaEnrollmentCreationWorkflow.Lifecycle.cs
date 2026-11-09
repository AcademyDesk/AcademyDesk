using System.Security.Claims;
using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Intelligence.Penta;

public sealed partial class PentaEnrollmentCreationWorkflow
{
    private sealed record PlanOutcome(int HttpStatus, Body? Body = null, string? Error = null);

    private async Task<PlanOutcome> PreparePlanAsync(ClaimsPrincipal principal, Guid academy, string stamp,
        string payload, CancellationToken token)
    {
        if (PentaEnrollmentFields.TryCreate(payload, out var create))
        {
            var result = await enrollments.PreviewCreateAsync(principal, academy, create!, stamp, token);
            return result.Preview is { } admission
                ? new(200, new(PentaOperationalPayload.Digest(payload), stamp, admission))
                : new(result.HttpStatus, Error: result.Error);
        }

        var update = PentaEnrollmentFields.TryUpdate(payload, out var enrollmentId, out var statusRequest);
        TransferEnrollmentRequest? transferRequest = null;
        if (!update && !PentaEnrollmentFields.TryTransfer(payload, out enrollmentId, out transferRequest))
            return new(400, Error: "Enter valid typed enrollment fields.");
        if (!LifecycleEnabled) return new(404);
        if (!await EnrollmentAdmissionBoundary.TryLockAsync(db, academy, token))
            return new(503, Error: EnrollmentAdmissionBoundary.BusyMessage);
        var operations = new EnrollmentMutationOperations(db);
        var assessed = update
            ? await operations.AssessUpdateAsync(academy, enrollmentId, statusRequest!, token)
            : await operations.AssessTransferAsync(academy, enrollmentId, transferRequest!, token);
        if (assessed.Source is not { } source) return new(assessed.HttpStatus, Error: assessed.Error);

        var transferDate = transferRequest is null ? (DateOnly?)null : transferRequest.TransferDate ?? DateOnly.FromDateTime(Now);
        var reason = statusRequest?.LifecycleReason;
        var change = new PentaEnrollmentChangePreview(update ? PentaEnrollmentFields.UpdateTool : PentaEnrollmentFields.TransferTool,
            source.Id, source.StudentId, source.BatchId, source.Status, assessed.Status!, statusRequest?.EndDate,
            string.IsNullOrEmpty(reason) ? null : reason, transferRequest?.TargetBatchId, transferDate);
        return new(200, new(PentaOperationalPayload.Digest(payload), stamp, null, Change: change, SourceHash: SourceDigest(source)));
    }

    private async Task<EnrollmentMutationOutcome> ExecutePlanAsync(ClaimsPrincipal principal, Guid academy, Body body, CancellationToken token)
    {
        if (body.Admission is { } admission)
            return await enrollments.CreateAsync(principal, academy,
                new(admission.StudentId, admission.BatchId, admission.StartDate, admission.Status), token);

        var change = body.Change!;
        if (!await EnrollmentAdmissionBoundary.TryLockAsync(db, academy, token))
            return new(503, Error: EnrollmentAdmissionBoundary.BusyMessage);
        var source = await db.Enrollments.AsNoTracking().SingleOrDefaultAsync(x => x.Id == change.EnrollmentId && x.AcademyId == academy, token);
        if (source is null || SourceDigest(source) != body.SourceHash)
            return new(409, Error: "The enrollment changed since review. Prepare a new request before changing it.");
        return change.Tool == PentaEnrollmentFields.UpdateTool
            ? await enrollments.UpdateAsync(principal, academy, change.EnrollmentId,
                new(change.Status, change.EndDate, change.LifecycleReason), token)
            : await enrollments.TransferAsync(principal, academy, change.EnrollmentId,
                new(change.TargetBatchId!.Value, change.TransferDate), token);
    }

    private static string Intent(PentaEnrollmentChangePreview? change) => change?.Tool switch
    {
        PentaEnrollmentFields.UpdateTool => UpdateIntent,
        PentaEnrollmentFields.TransferTool => TransferIntent,
        _ => ApprovalIntent
    };

    private static string SourceDigest(Enrollment source) => Hash(new
    {
        source.Id, source.StudentId, source.BatchId, source.Status, source.StartDate, source.EndDate, source.LifecycleReason
    });
}
