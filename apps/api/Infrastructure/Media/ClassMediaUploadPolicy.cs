using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Domain.Identity;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Infrastructure.Media;

public sealed class ClassMediaUploadPolicy(AcademyDeskDbContext db)
{
    public async Task<bool> CanUploadAsync(ApplicationUser user, Guid batchId, Guid? studentId, Guid? sessionId, CancellationToken token)
    {
        if (!user.IsActive || !user.AcademyId.HasValue || !user.TeacherId.HasValue ||
            !await db.Academies.AnyAsync(x => x.Id == user.AcademyId && x.IsActive, token) ||
            !await db.Teachers.AnyAsync(x => x.Id == user.TeacherId && x.AcademyId == user.AcademyId && x.IsActive, token) ||
            !await db.Batches.AnyAsync(x => x.Id == batchId && x.AcademyId == user.AcademyId && x.TeacherId == user.TeacherId && x.IsActive, token)) return false;
        if (studentId.HasValue && (!await db.Students.AnyAsync(x => x.Id == studentId && x.AcademyId == user.AcademyId && x.IsActive, token) ||
            !await db.Enrollments.AnyAsync(x => x.AcademyId == user.AcademyId && x.BatchId == batchId && x.StudentId == studentId && x.Status == "Active", token))) return false;
        return !sessionId.HasValue || await db.ClassSessions.AnyAsync(x => x.Id == sessionId && x.AcademyId == user.AcademyId &&
            x.BatchId == batchId && x.TeacherId == user.TeacherId, token);
    }

    public static bool Matches(ClassMediaUploadSession a, ClassMediaUploadSession b) =>
        a.AcademyId == b.AcademyId && a.OwnerUserId == b.OwnerUserId && a.TeacherId == b.TeacherId &&
        a.BatchId == b.BatchId && a.StudentId == b.StudentId && a.ClassSessionId == b.ClassSessionId &&
        a.FileName == b.FileName && a.Length == b.Length && a.ChunkBytes == b.ChunkBytes && a.Title == b.Title &&
        a.Description == b.Description && a.Type == b.Type;
}
