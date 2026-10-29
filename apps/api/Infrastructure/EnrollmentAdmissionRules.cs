using AcademyDesk.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Infrastructure;

/// <summary>Shared deterministic admission checks; callers hold the academy
/// admission transaction lock until domain changes and required audit commit.</summary>
internal static class EnrollmentAdmissionRules
{
    internal const string PrerequisiteMessage = "The learner has not completed all course prerequisites for this batch.";
    internal const string DuplicateMessage = "The student already has an active or waitlisted enrollment in this batch. Activate the existing waitlist entry instead of creating another enrollment.";

    internal static Task<bool> HasOtherAdmissionAsync(AcademyDeskDbContext db, Guid academyId, Guid studentId, Guid batchId, Guid? exceptEnrollmentId, CancellationToken token) =>
        db.Enrollments.AnyAsync(x => x.AcademyId == academyId && x.StudentId == studentId && x.BatchId == batchId &&
            (exceptEnrollmentId == null || x.Id != exceptEnrollmentId) && (x.Status == "Active" || x.Status == "Waitlisted"), token);

    internal static async Task<bool> HasCompletedPrerequisitesAsync(AcademyDeskDbContext db, Guid academyId, Guid studentId, Guid courseId, CancellationToken token, Guid? completingSourceCourseId = null)
    {
        var required = await db.CoursePrerequisites.AsNoTracking()
            .Where(x => x.AcademyId == academyId && x.CourseId == courseId && x.MustBeCompleted)
            .Select(x => x.RequiredCourseId).Distinct().ToListAsync(token);
        if (required.Count == 0) return true;
        var completed = await (from enrollment in db.Enrollments.AsNoTracking()
            join batch in db.Batches.AsNoTracking() on enrollment.BatchId equals batch.Id
            where enrollment.AcademyId == academyId && batch.AcademyId == academyId && enrollment.StudentId == studentId && enrollment.Status == "Completed"
            select batch.CourseId).Distinct().ToListAsync(token);
        // Only promotion may supply its verified source course. That same owned
        // transaction must persist source completion, target admission and audit.
        return required.All(id => completed.Contains(id) || id == completingSourceCourseId);
    }
}
