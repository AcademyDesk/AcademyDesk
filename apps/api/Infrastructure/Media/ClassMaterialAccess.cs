using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Domain.Identity;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Infrastructure.Media;

public sealed class ClassMaterialAccess(AcademyDeskDbContext db)
{
    public async Task<bool> CanReadAsync(ApplicationUser user, IReadOnlyCollection<string> currentRoles,
        LearningResource resource, CancellationToken token)
    {
        if (!user.IsActive || user.AcademyId != resource.AcademyId ||
            !await db.Academies.AsNoTracking().AnyAsync(x => x.Id == resource.AcademyId && x.IsActive, token)) return false;

        Batch? batch = null;
        if (resource.BatchId.HasValue)
        {
            batch = await db.Batches.AsNoTracking().SingleOrDefaultAsync(x => x.Id == resource.BatchId &&
                x.AcademyId == resource.AcademyId && x.IsActive, token);
            if (batch is null || (resource.CourseId.HasValue && batch.CourseId != resource.CourseId)) return false;
        }
        if (resource.ClassSessionId.HasValue && (batch is null || !await db.ClassSessions.AsNoTracking().AnyAsync(x =>
            x.Id == resource.ClassSessionId && x.AcademyId == resource.AcademyId && x.BatchId == batch.Id, token))) return false;

        // Current database roles, never the potentially stale roles in a bearer ticket.
        // No platform-owner or generic same-tenant bypass is granted for private files.
        if (currentRoles.Contains("AcademyAdmin") || currentRoles.Contains("Owner")) return true;
        if (currentRoles.Contains("Teacher") && user.TeacherId.HasValue &&
            await db.Teachers.AsNoTracking().AnyAsync(x => x.Id == user.TeacherId && x.AcademyId == resource.AcademyId && x.IsActive, token))
        {
            return batch is not null ? batch.TeacherId == user.TeacherId : resource.CourseId.HasValue &&
                await db.Batches.AsNoTracking().AnyAsync(x => x.AcademyId == resource.AcademyId && x.IsActive &&
                    x.TeacherId == user.TeacherId && x.CourseId == resource.CourseId, token);
        }

        // Staff can manage unpublished resources; family access is publication-gated.
        if (!resource.IsPublished) return false;
        var students = db.Students.AsNoTracking().Where(x => x.AcademyId == resource.AcademyId && x.IsActive &&
            (!resource.StudentId.HasValue || x.Id == resource.StudentId));
        if (currentRoles.Contains("Student") && user.StudentId.HasValue)
            students = students.Where(x => x.Id == user.StudentId);
        else if (currentRoles.Contains("Guardian") && user.GuardianId.HasValue &&
            await db.Guardians.AsNoTracking().AnyAsync(x => x.Id == user.GuardianId && x.AcademyId == resource.AcademyId && x.IsActive, token))
            students = students.Where(x => db.StudentGuardians.Any(link => link.AcademyId == resource.AcademyId &&
                link.StudentId == x.Id && link.GuardianId == user.GuardianId && link.CanAccessPortal &&
                link.CanViewDocuments && link.AccessRevokedAtUtc == null));
        else return false;

        if (resource.BatchId.HasValue || resource.CourseId.HasValue)
            students = students.Where(student => db.Enrollments.Any(enrollment => enrollment.AcademyId == resource.AcademyId &&
                enrollment.StudentId == student.Id && enrollment.Status == "Active" &&
                (!resource.BatchId.HasValue || enrollment.BatchId == resource.BatchId) &&
                db.Batches.Any(b => b.Id == enrollment.BatchId && b.AcademyId == resource.AcademyId && b.IsActive &&
                    (!resource.CourseId.HasValue || b.CourseId == resource.CourseId))));
        return await students.AnyAsync(token);
    }
}
