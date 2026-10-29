using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Infrastructure;
using AcademyDesk.Api.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace AcademyDesk.Api.Controllers;

[ApiController]
[Route("api/academies/{academyId:guid}/batch-promotions")]
public sealed class BatchPromotionsController(AcademyDeskDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult> List(Guid academyId, CancellationToken t) =>
        Ok(await db.BatchPromotions.AsNoTracking().Where(x => x.AcademyId == academyId).OrderByDescending(x => x.EffectiveDate).ToListAsync(t));

    [HttpPost]
    public async Task<ActionResult> Create(Guid academyId, PromotionRequest r, CancellationToken t)
    {
        if (r.SourceBatchId == r.TargetBatchId ||
            !await db.Enrollments.AnyAsync(x => x.AcademyId == academyId && x.StudentId == r.StudentId && x.BatchId == r.SourceBatchId && x.Status == "Active", t) ||
            !await db.Batches.AnyAsync(x => x.AcademyId == academyId && x.Id == r.TargetBatchId, t)) return BadRequest();
        db.BatchPromotions.Add(new BatchPromotion { AcademyId = academyId, StudentId = r.StudentId, SourceBatchId = r.SourceBatchId, TargetBatchId = r.TargetBatchId, EffectiveDate = r.EffectiveDate, Notes = r.Notes?.Trim() });
        await db.SaveChangesAsync(t);
        return Ok();
    }

    [HttpPatch("{id:guid}/decision")]
    [AtomicAcademyMutation]
    public async Task<ActionResult> Decide(Guid academyId, Guid id, PromotionDecision r, CancellationToken t)
    {
        if (r.Status is not ("Approved" or "Rejected")) return BadRequest();

        // Academy actors reuse the save/audit transaction. Platform-owner bypass
        // still needs an owned atomic decision boundary.
        await using var ownedTransaction = db.Database.IsSqlServer() && db.Database.CurrentTransaction is null
            ? await db.Database.BeginTransactionAsync(t) : null;
        if (!await EnrollmentAdmissionBoundary.TryLockAsync(db, academyId, t))
            return StatusCode(503, new { message = EnrollmentAdmissionBoundary.BusyMessage });
        if (db.Database.IsSqlServer() && !await LockDecisionAsync(academyId, id, t))
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "This promotion is being decided. Please try again." });

        var promotion = await db.BatchPromotions.SingleOrDefaultAsync(x => x.Id == id && x.AcademyId == academyId, t);
        if (promotion is null) return NotFound();
        if (promotion.Status != "Pending")
            return Conflict(new { message = "This promotion already has a final decision and cannot be changed." });

        if (r.Status == "Approved")
        {
            var target = await db.Batches.SingleOrDefaultAsync(x => x.Id == promotion.TargetBatchId && x.AcademyId == academyId, t);
            if (target is null || !target.IsActive || !string.Equals(target.EnrollmentStatus, "Open", StringComparison.OrdinalIgnoreCase))
                return Conflict(new { message = "The target batch is not open for enrolment." });
            if (await db.Enrollments.CountAsync(x => x.AcademyId == academyId && x.BatchId == target.Id && x.Status == "Active", t) >= target.Capacity)
                return Conflict(new { message = "Target batch is at capacity." });
            var source = await db.Enrollments.SingleOrDefaultAsync(e => e.AcademyId == academyId && e.StudentId == promotion.StudentId &&
                e.BatchId == promotion.SourceBatchId && e.Status == "Active", t);
            if (source is null)
                return Conflict(new { message = "The source enrollment is no longer active; this promotion cannot be approved." });

            if (await EnrollmentAdmissionRules.HasOtherAdmissionAsync(db, academyId, promotion.StudentId, target.Id, null, t))
                return Conflict(new { message = EnrollmentAdmissionRules.DuplicateMessage });
            var sourceBatch = await db.Batches.AsNoTracking().SingleOrDefaultAsync(x => x.Id == source.BatchId && x.AcademyId == academyId, t);
            if (sourceBatch is null) return Conflict(new { message = "The source batch does not belong to this academy." });
            if (!await EnrollmentAdmissionRules.HasCompletedPrerequisitesAsync(db, academyId, promotion.StudentId, target.CourseId, t, sourceBatch.CourseId))
                return Conflict(new { message = EnrollmentAdmissionRules.PrerequisiteMessage });

            source.Status = "Completed";
            source.EndDate = promotion.EffectiveDate;
            source.LifecycleReason = "Promoted to target batch";
            db.Enrollments.Add(new Enrollment { AcademyId = academyId, StudentId = promotion.StudentId, BatchId = promotion.TargetBatchId, StartDate = promotion.EffectiveDate, Status = "Active" });
        }

        promotion.Status = r.Status;
        promotion.Notes = r.Notes?.Trim() ?? promotion.Notes;
        await db.SaveChangesAsync(t);
        if (ownedTransaction is not null) await ownedTransaction.CommitAsync(t);
        return Ok();
    }

    private async Task<bool> LockDecisionAsync(Guid academyId, Guid promotionId, CancellationToken t)
    {
        await using var command = db.Database.GetDbConnection().CreateCommand();
        command.Transaction = db.Database.CurrentTransaction!.GetDbTransaction();
        command.CommandText = "DECLARE @result int; EXEC @result = sys.sp_getapplock @Resource = @resource, @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 10000; SELECT @result;";
        var resource = command.CreateParameter(); resource.ParameterName = "@resource";
        resource.Value = $"AcademyDesk:BatchPromotion:{academyId:D}:{promotionId:D}";
        command.Parameters.Add(resource);
        return (int)(await command.ExecuteScalarAsync(t))! >= 0;
    }
}

public sealed record PromotionRequest(Guid StudentId, Guid SourceBatchId, Guid TargetBatchId, DateOnly EffectiveDate, string? Notes);
public sealed record PromotionDecision(string Status, string? Notes);
