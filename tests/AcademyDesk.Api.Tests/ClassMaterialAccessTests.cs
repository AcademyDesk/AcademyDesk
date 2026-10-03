using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Domain.Identity;
using AcademyDesk.Api.Infrastructure.Media;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Tests;

public sealed class ClassMaterialAccessTests
{
    [Fact]
    public async Task TeacherAndAdminRequireCurrentTenantRoleAndActivity()
    {
        await using var fixture = new Fixture();
        await fixture.Seed();
        Assert.True(await fixture.Read(fixture.Teacher, "Teacher"));
        Assert.True(await fixture.Read(fixture.Admin, "AcademyAdmin"));
        Assert.False(await fixture.Read(fixture.Teacher, "FinanceUser"));
        Assert.False(await fixture.Read(new ApplicationUser { AcademyId = Guid.NewGuid(), IsActive = true }, "AcademyAdmin"));
        fixture.Teacher.IsActive = false;
        Assert.False(await fixture.Read(fixture.Teacher, "Teacher"));
        fixture.Teacher.IsActive = true;
        fixture.Db.Teachers.Single().IsActive = false;
        await fixture.Db.SaveChangesAsync();
        Assert.False(await fixture.Read(fixture.Teacher, "Teacher"));
    }

    [Fact]
    public async Task PublicationAndTargetStudentAreEnforcedForFamilies()
    {
        await using var f = new Fixture(); await f.Seed();
        Assert.True(await f.Read(f.Student, "Student"));
        f.Resource.StudentId = Guid.NewGuid();
        Assert.False(await f.Read(f.Student, "Student"));
        f.Resource.StudentId = null;
        f.Resource.IsPublished = false;
        Assert.False(await f.Read(f.Student, "Student"));
        Assert.True(await f.Read(f.Teacher, "Teacher")); // Staff retains draft management.
    }

    [Fact]
    public async Task EnrollmentBatchTeacherAndAcademyRevocationsApplyImmediately()
    {
        await using var f = new Fixture(); await f.Seed();
        var enrollment = f.Db.Enrollments.Single();
        enrollment.Status = "Withdrawn"; await f.Db.SaveChangesAsync();
        Assert.False(await f.Read(f.Student, "Student"));
        enrollment.Status = "Active";
        var batch = f.Db.Batches.Single(); batch.TeacherId = Guid.NewGuid(); await f.Db.SaveChangesAsync();
        Assert.False(await f.Read(f.Teacher, "Teacher"));
        batch.IsActive = false; await f.Db.SaveChangesAsync();
        Assert.False(await f.Read(f.Student, "Student"));
        Assert.False(await f.Read(f.Admin, "AcademyAdmin"));
        batch.IsActive = true; f.Db.Academies.Single().IsActive = false; await f.Db.SaveChangesAsync();
        Assert.False(await f.Read(f.Student, "Student"));
        Assert.False(await f.Read(f.Admin, "AcademyAdmin"));
    }

    [Fact]
    public async Task GuardianRequiresActiveDocumentGrantAndAnEligibleChild()
    {
        await using var f = new Fixture(); await f.Seed();
        Assert.True(await f.Read(f.Guardian, "Guardian"));
        var link = f.Db.StudentGuardians.Single();
        link.CanViewDocuments = false; await f.Db.SaveChangesAsync();
        Assert.False(await f.Read(f.Guardian, "Guardian"));
        link.CanViewDocuments = true; link.AccessRevokedAtUtc = DateTime.UtcNow; await f.Db.SaveChangesAsync();
        Assert.False(await f.Read(f.Guardian, "Guardian"));
        link.AccessRevokedAtUtc = null; link.CanAccessPortal = false; await f.Db.SaveChangesAsync();
        Assert.False(await f.Read(f.Guardian, "Guardian"));
        link.CanAccessPortal = true; f.Db.Guardians.Single().IsActive = false; await f.Db.SaveChangesAsync();
        Assert.False(await f.Read(f.Guardian, "Guardian"));
    }

    [Fact]
    public async Task OrphanSessionAndCourseMismatchFailClosed()
    {
        await using var f = new Fixture(); await f.Seed();
        f.Resource.ClassSessionId = Guid.NewGuid();
        Assert.False(await f.Read(f.Admin, "AcademyAdmin"));
        f.Resource.ClassSessionId = null; f.Resource.CourseId = Guid.NewGuid();
        Assert.False(await f.Read(f.Student, "Student"));
        Assert.False(await f.Read(f.Admin, "AcademyAdmin"));
    }

    [Fact]
    public async Task CourseOnlyScopeDoesNotAuthorizeAnotherCourse()
    {
        await using var f = new Fixture(); await f.Seed();
        f.Resource.BatchId = null; f.Resource.CourseId = f.Db.Batches.Single().CourseId;
        Assert.True(await f.Read(f.Student, "Student"));
        Assert.True(await f.Read(f.Teacher, "Teacher"));
        f.Resource.CourseId = Guid.NewGuid();
        Assert.False(await f.Read(f.Student, "Student"));
        Assert.False(await f.Read(f.Teacher, "Teacher"));
    }

    private sealed class Fixture : IAsyncDisposable
    {
        public AcademyDeskDbContext Db { get; } = new(new DbContextOptionsBuilder<AcademyDeskDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        private readonly Guid academyId = Guid.NewGuid();
        private readonly Guid batchId = Guid.NewGuid();
        public ApplicationUser Teacher { get; } = new() { TeacherId = Guid.NewGuid() };
        public ApplicationUser Student { get; } = new() { StudentId = Guid.NewGuid() };
        public ApplicationUser Guardian { get; } = new() { GuardianId = Guid.NewGuid() };
        public ApplicationUser Admin { get; } = new();
        public LearningResource Resource { get; } = new() { Title = "Synthetic material", Url = "/uploads/teacher-materials/test.pdf" };

        public async Task Seed()
        {
            foreach (var user in new[] { Teacher, Student, Guardian, Admin }) user.AcademyId = academyId;
            Resource.AcademyId = academyId; Resource.BatchId = batchId;
            Db.Academies.Add(new Academy { Id = academyId, Name = "Synthetic" });
            Db.Teachers.Add(new Teacher { Id = Teacher.TeacherId!.Value, AcademyId = academyId, FirstName = "Synthetic", LastName = "Teacher" });
            Db.Students.Add(new Student { Id = Student.StudentId!.Value, AcademyId = academyId, FirstName = "Synthetic", LastName = "Student" });
            Db.Guardians.Add(new Guardian { Id = Guardian.GuardianId!.Value, AcademyId = academyId, FirstName = "Synthetic", LastName = "Guardian" });
            Db.Batches.Add(new Batch { Id = batchId, AcademyId = academyId, Name = "Synthetic", TeacherId = Teacher.TeacherId, CourseId = Guid.NewGuid() });
            Db.Enrollments.Add(new Enrollment { AcademyId = academyId, BatchId = batchId, StudentId = Student.StudentId.Value });
            Db.StudentGuardians.Add(new StudentGuardian { AcademyId = academyId, StudentId = Student.StudentId.Value,
                GuardianId = Guardian.GuardianId.Value, CanAccessPortal = true, CanViewDocuments = true });
            await Db.SaveChangesAsync();
        }
        public Task<bool> Read(ApplicationUser user, string role) => new ClassMaterialAccess(Db).CanReadAsync(user, [role], Resource, default);
        public ValueTask DisposeAsync() => Db.DisposeAsync();
    }
}
