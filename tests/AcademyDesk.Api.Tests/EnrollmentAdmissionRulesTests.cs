using System.Text.Json;
using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Tests;

public sealed class EnrollmentAdmissionRulesTests
{
    private static AcademyDeskDbContext Store() => new(new DbContextOptionsBuilder<AcademyDeskDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    private sealed record Fixture(Guid Academy, Student Student, Batch Target, Batch Source, Enrollment SourceEnrollment);
    private static async Task<Fixture> Seed(AcademyDeskDbContext db)
    {
        var academy = Guid.NewGuid(); var student = new Student { AcademyId = academy, FirstName = "Synthetic", LastName = "Admission" };
        var target = new Batch { AcademyId = academy, Name = "Target", CourseId = Guid.NewGuid(), Capacity = 8, WaitlistCapacity = 4 };
        var source = new Batch { AcademyId = academy, Name = "Source", CourseId = Guid.NewGuid(), Capacity = 8 };
        var enrollment = new Enrollment { AcademyId = academy, StudentId = student.Id, BatchId = source.Id, Status = "Active" };
        db.AddRange(student, target, source, enrollment); await db.SaveChangesAsync(); return new(academy, student, target, source, enrollment);
    }
    private static async Task<string> State(AcademyDeskDbContext db)
    {
        db.ChangeTracker.Clear();
        return JsonSerializer.Serialize(new {
            Batches = await db.Batches.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
            Enrollments = await db.Enrollments.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
            Promotions = await db.BatchPromotions.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
            Prerequisites = await db.CoursePrerequisites.AsNoTracking().OrderBy(x => x.Id).ToListAsync()
        });
    }
    private static async Task<Func<Task<ActionResult>>> Operation(AcademyDeskDbContext db, Fixture f, string path)
    {
        if (path == "create") return async () => (await new EnrollmentsController(db).Create(f.Academy, new(f.Student.Id, f.Target.Id, null, "Active"), default)).Result!;
        if (path == "activate")
        {
            var waiting = new Enrollment { AcademyId = f.Academy, StudentId = f.Student.Id, BatchId = f.Target.Id, Status = "Waitlisted" };
            db.Add(waiting); await db.SaveChangesAsync();
            return async () => (await new EnrollmentsController(db).Update(f.Academy, waiting.Id, new("Active", null, null), default)).Result!;
        }
        if (path == "transfer") return async () => (await new EnrollmentsController(db).Transfer(f.Academy, f.SourceEnrollment.Id, new(f.Target.Id, null), default)).Result!;
        var promotion = new BatchPromotion { AcademyId = f.Academy, StudentId = f.Student.Id, SourceBatchId = f.Source.Id, TargetBatchId = f.Target.Id, EffectiveDate = new(2026, 10, 23) };
        db.Add(promotion); await db.SaveChangesAsync();
        return () => new BatchPromotionsController(db).Decide(f.Academy, promotion.Id, new("Approved", null), default);
    }
    public static IEnumerable<object[]> PrerequisiteCases() =>
        from path in new[] { "create", "activate", "transfer", "promote" }
        from mode in new[] { "missing", "partial", "wrong-student", "foreign-enrollment", "foreign-batch", "active-only", "all-completed", "optional" }
        select new object[] { path, mode };

    [Theory]
    [MemberData(nameof(PrerequisiteCases))]
    public async Task All_admission_paths_require_own_student_completed_courses_and_preserve_denials(string path, string mode)
    {
        await using var db = Store(); var f = await Seed(db); var foreign = Guid.NewGuid();
        var required = new[] { Guid.NewGuid(), Guid.NewGuid() };
        db.CoursePrerequisites.AddRange(required.Select(id => new CoursePrerequisite { AcademyId = f.Academy, CourseId = f.Target.CourseId, RequiredCourseId = id, MustBeCompleted = mode != "optional" }));
        foreach (var id in required.Take(mode is "missing" or "optional" ? 0 : mode == "partial" ? 1 : 2))
        {
            var batch = new Batch { AcademyId = mode == "foreign-batch" ? foreign : f.Academy, Name = "Prior", CourseId = id, Capacity = 4 };
            db.AddRange(batch, new Enrollment { AcademyId = mode == "foreign-enrollment" ? foreign : f.Academy, StudentId = mode == "wrong-student" ? Guid.NewGuid() : f.Student.Id, BatchId = batch.Id, Status = mode == "active-only" ? "Active" : "Completed" });
        }
        await db.SaveChangesAsync(); var operation = await Operation(db, f, path); var before = await State(db);
        var result = await operation();
        if (mode is "all-completed" or "optional")
        {
            Assert.True(result is OkResult or OkObjectResult or CreatedResult);
            Assert.Single(await db.Enrollments.AsNoTracking().Where(x => x.BatchId == f.Target.Id && x.Status == "Active").ToListAsync());
        }
        else
        {
            var conflict = Assert.IsType<ConflictObjectResult>(result);
            Assert.Contains("prerequisites", JsonSerializer.Serialize(conflict.Value)); Assert.Equal(before, await State(db));
        }
    }

    [Theory]
    [InlineData("Active", "create")]
    [InlineData("Waitlisted", "create")]
    [InlineData("Active", "activate")]
    [InlineData("Waitlisted", "activate")]
    [InlineData("Active", "transfer")]
    [InlineData("Waitlisted", "transfer")]
    [InlineData("Active", "promote")]
    [InlineData("Waitlisted", "promote")]
    public async Task Existing_active_or_waitlisted_row_blocks_a_second_admission_without_touching_history(string status, string path)
    {
        await using var db = Store(); var f = await Seed(db);
        db.Add(new Enrollment { AcademyId = f.Academy, StudentId = f.Student.Id, BatchId = f.Target.Id, Status = status }); await db.SaveChangesAsync();
        var operation = await Operation(db, f, path); var before = await State(db);
        Assert.IsType<ConflictObjectResult>(await operation()); Assert.Equal(before, await State(db));
    }

    [Theory]
    [InlineData("create")]
    [InlineData("activate")]
    [InlineData("transfer")]
    [InlineData("promote")]
    public async Task Completed_historical_enrollment_does_not_block_fresh_admission(string path)
    {
        await using var db = Store(); var f = await Seed(db);
        var history = new Enrollment { AcademyId = f.Academy, StudentId = f.Student.Id, BatchId = f.Target.Id, Status = "Completed", LifecycleReason = "Historical completion" };
        db.Add(history); await db.SaveChangesAsync(); var original = JsonSerializer.Serialize(history); var operation = await Operation(db, f, path);
        Assert.True(await operation() is OkResult or OkObjectResult or CreatedResult);
        db.ChangeTracker.Clear(); Assert.Equal(original, JsonSerializer.Serialize(await db.Enrollments.SingleAsync(x => x.Id == history.Id)));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Promotion_credits_source_completion_atomically_but_requires_every_other_prerequisite(bool missingOther)
    {
        await using var db = Store(); var f = await Seed(db);
        db.Add(new CoursePrerequisite { AcademyId = f.Academy, CourseId = f.Target.CourseId, RequiredCourseId = f.Source.CourseId });
        if (missingOther) db.Add(new CoursePrerequisite { AcademyId = f.Academy, CourseId = f.Target.CourseId, RequiredCourseId = Guid.NewGuid() });
        await db.SaveChangesAsync(); var operation = await Operation(db, f, "promote"); var before = await State(db); var result = await operation();
        if (missingOther) { Assert.IsType<ConflictObjectResult>(result); Assert.Equal(before, await State(db)); }
        else { Assert.IsType<OkResult>(result); db.ChangeTracker.Clear(); Assert.Equal("Completed", (await db.Enrollments.SingleAsync(x => x.Id == f.SourceEnrollment.Id)).Status); }
    }

    [Fact]
    public async Task Entering_waitlist_requires_spare_waitlist_capacity_and_never_creates_a_new_row()
    {
        await using var db = Store(); var f = await Seed(db); f.Source.WaitlistCapacity = 1;
        db.Add(new Enrollment { AcademyId = f.Academy, StudentId = Guid.NewGuid(), BatchId = f.Source.Id, Status = "Waitlisted" }); await db.SaveChangesAsync(); var before = await State(db);
        Assert.IsType<ConflictObjectResult>((await new EnrollmentsController(db).Update(f.Academy, f.SourceEnrollment.Id, new("Waitlisted", null, "Move to waitlist"), default)).Result);
        Assert.Equal(before, await State(db));
    }

    [Fact]
    public async Task Creating_another_waitlist_entry_is_conflict_and_retains_the_existing_row()
    {
        await using var db = Store(); var f = await Seed(db);
        db.Add(new Enrollment { AcademyId = f.Academy, StudentId = f.Student.Id, BatchId = f.Target.Id, Status = "Waitlisted" }); await db.SaveChangesAsync(); var before = await State(db);
        Assert.IsType<ConflictObjectResult>((await new EnrollmentsController(db).Create(f.Academy, new(f.Student.Id, f.Target.Id, null, "Waitlisted"), default)).Result);
        Assert.Equal(before, await State(db));
    }
}
