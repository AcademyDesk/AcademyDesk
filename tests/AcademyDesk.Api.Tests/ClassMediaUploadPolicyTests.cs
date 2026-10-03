using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Domain.Identity;
using AcademyDesk.Api.Infrastructure.Media;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Tests;

public sealed class ClassMediaUploadPolicyTests
{
    [Fact]
    public async Task OptionalScopeIsAllowedButSuppliedScopeMustBeCurrentAndAssigned()
    {
        await using var db = new AcademyDeskDbContext(new DbContextOptionsBuilder<AcademyDeskDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var academy = new Academy { Name = "QA" }; var teacher = new Teacher { AcademyId = academy.Id, FirstName = "QA", LastName = "Teacher" };
        var batch = new Batch { AcademyId = academy.Id, TeacherId = teacher.Id, CourseId = Guid.NewGuid(), Name = "QA" };
        var student = new Student { AcademyId = academy.Id, FirstName = "QA", LastName = "Student" };
        var enrollment = new Enrollment { AcademyId = academy.Id, StudentId = student.Id, BatchId = batch.Id };
        var session = new ClassSession { AcademyId = academy.Id, BatchId = batch.Id, TeacherId = teacher.Id };
        db.AddRange(academy, teacher, batch, student, enrollment, session); await db.SaveChangesAsync();
        var user = new ApplicationUser { AcademyId = academy.Id, TeacherId = teacher.Id, IsActive = true };
        var policy = new ClassMediaUploadPolicy(db);
        Assert.True(await policy.CanUploadAsync(user, batch.Id, null, null, default));
        Assert.True(await policy.CanUploadAsync(user, batch.Id, student.Id, session.Id, default));
        Assert.False(await policy.CanUploadAsync(user, batch.Id, Guid.NewGuid(), session.Id, default));
        Assert.False(await policy.CanUploadAsync(user, batch.Id, student.Id, Guid.NewGuid(), default));
        enrollment.Status = "Withdrawn"; await db.SaveChangesAsync();
        Assert.False(await policy.CanUploadAsync(user, batch.Id, student.Id, session.Id, default));
        enrollment.Status = "Active"; session.TeacherId = Guid.NewGuid(); await db.SaveChangesAsync();
        Assert.False(await policy.CanUploadAsync(user, batch.Id, student.Id, session.Id, default));
        batch.TeacherId = Guid.NewGuid(); await db.SaveChangesAsync();
        Assert.False(await policy.CanUploadAsync(user, batch.Id, null, null, default));
    }

    [Theory]
    [InlineData("user")]
    [InlineData("academy")]
    [InlineData("teacher")]
    [InlineData("batch")]
    public async Task RevocationDeniesPendingUpload(string revoked)
    {
        await using var db = new AcademyDeskDbContext(new DbContextOptionsBuilder<AcademyDeskDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var academy = new Academy { Name = "QA", IsActive = revoked != "academy" };
        var teacher = new Teacher { AcademyId = academy.Id, FirstName = "QA", LastName = "Teacher", IsActive = revoked != "teacher" };
        var batch = new Batch { AcademyId = academy.Id, TeacherId = teacher.Id, CourseId = Guid.NewGuid(), Name = "QA", IsActive = revoked != "batch" };
        db.AddRange(academy, teacher, batch); await db.SaveChangesAsync();
        var user = new ApplicationUser { AcademyId = academy.Id, TeacherId = teacher.Id, IsActive = revoked != "user" };
        Assert.False(await new ClassMediaUploadPolicy(db).CanUploadAsync(user, batch.Id, null, null, default));
    }

    [Fact]
    public void SessionRetryCannotAlterOwnershipOrFileMetadata()
    {
        var a = new ClassMediaUploadSession { OwnerUserId = Guid.NewGuid(), AcademyId = Guid.NewGuid(), TeacherId = Guid.NewGuid(), BatchId = Guid.NewGuid(), FileName = "file.mov", Title = "QA", Length = 17, ChunkBytes = MediaStorageOptions.DefaultChunkBytes };
        var b = new ClassMediaUploadSession { OwnerUserId = a.OwnerUserId, AcademyId = a.AcademyId, TeacherId = a.TeacherId, BatchId = a.BatchId, FileName = a.FileName, Title = a.Title, Length = a.Length, ChunkBytes = a.ChunkBytes };
        Assert.True(ClassMediaUploadPolicy.Matches(a, b));
        b.Length++; Assert.False(ClassMediaUploadPolicy.Matches(a, b)); b.Length--;
        b.OwnerUserId = Guid.NewGuid(); Assert.False(ClassMediaUploadPolicy.Matches(a, b)); b.OwnerUserId = a.OwnerUserId;
        b.StudentId = Guid.NewGuid(); Assert.False(ClassMediaUploadPolicy.Matches(a, b));
    }
}
