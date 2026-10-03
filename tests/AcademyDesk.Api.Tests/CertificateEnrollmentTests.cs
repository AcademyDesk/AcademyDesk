using System.Text.Json;
using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;

namespace AcademyDesk.Api.Tests;

public sealed class CertificateEnrollmentTests
{
    [Theory]
    [InlineData("UnrelatedBatch")]
    [InlineData("OtherStudent")]
    [InlineData("ForeignEnrollment")]
    public async Task Unrelated_association_is_rejected_without_writes(string mode)
    {
        await using var f = await Fixture.Create();
        f.Db.Enrollments.Add(new Enrollment
        {
            AcademyId = mode == "ForeignEnrollment" ? f.ForeignAcademy : f.Academy,
            StudentId = mode == "OtherStudent" ? f.OtherStudent.Id : f.Student.Id,
            BatchId = mode == "UnrelatedBatch" ? f.OtherBatch.Id : f.Batch.Id,
            Status = "Active"
        });
        await f.Db.SaveChangesAsync();
        Assert.False(await f.Db.Enrollments.AnyAsync(x => x.AcademyId == f.Academy && x.StudentId == f.Student.Id && x.BatchId == f.Batch.Id));
        await AssertEnrollmentRejected(f);
    }

    [Theory]
    [InlineData("NoBatch", true)]
    [InlineData("Active", true)]
    [InlineData("Completed", true)]
    [InlineData("Completed", false)]
    public async Task Existing_optional_and_enrolled_controls_are_preserved(string mode, bool activeBatch)
    {
        await using var f = await Fixture.Create();
        f.Batch.IsActive = activeBatch;
        if (mode != "NoBatch")
            f.Db.Enrollments.Add(new Enrollment { AcademyId = f.Academy, StudentId = f.Student.Id, BatchId = f.Batch.Id, Status = mode });
        await f.Db.SaveChangesAsync();
        await AssertCreated(f, mode == "NoBatch" ? null : f.Batch.Id);
    }

    [Theory]
    [InlineData("MissingStudent")]
    [InlineData("ForeignStudent")]
    [InlineData("MissingBatch")]
    [InlineData("ForeignBatch")]
    public async Task Existing_reference_guards_reject_without_writes(string mode)
    {
        await using var f = await Fixture.Create();
        var studentId = mode == "MissingStudent" ? Guid.NewGuid() : mode == "ForeignStudent" ? f.ForeignStudent.Id : f.Student.Id;
        var batchId = mode == "MissingBatch" ? Guid.NewGuid() : mode == "ForeignBatch" ? f.ForeignBatch.Id : f.Batch.Id;
        var before = await SourceSnapshot(f.Db);
        var result = await new CertificatesController(f.Db, new TestEnvironment()).Issue(f.Academy, Request(studentId, batchId), default);
        var error = Assert.IsType<BadRequestObjectResult>(result.Result);
        var expected = mode.EndsWith("Student") ? "The student does not belong to this academy." : "The class or batch does not belong to this academy.";
        Assert.Equal(expected, JsonSerializer.SerializeToElement(error.Value).GetProperty("message").GetString());
        await using var fresh = new AcademyDeskDbContext(f.Options);
        Assert.Empty(await fresh.Certificates.ToListAsync());
        Assert.Empty(await fresh.AuditLogs.ToListAsync());
        Assert.Equal(before, await SourceSnapshot(fresh));
    }


    [Theory]
    [InlineData("Waitlisted")]
    [InlineData("Paused")]
    [InlineData("Withdrawn")]
    [InlineData("Cancelled")]
    [InlineData("Transferred")]
    [InlineData("Unknown")]
    [InlineData("")]
    public async Task Nonqualifying_enrollment_status_is_rejected_without_writes(string? status)
    {
        await using var f = await Fixture.Create();
        f.Db.Enrollments.Add(new Enrollment { AcademyId = f.Academy, StudentId = f.Student.Id, BatchId = f.Batch.Id, Status = status! });
        await f.Db.SaveChangesAsync();
        await AssertEnrollmentRejected(f);
    }

    [Fact]
    public async Task Enrollment_status_is_required_by_the_existing_model()
    {
        await using var f = await Fixture.Create();
        Assert.False(f.Db.Model.FindEntityType(typeof(Enrollment))!.FindProperty(nameof(Enrollment.Status))!.IsNullable);
        f.Db.Enrollments.Add(new Enrollment { AcademyId = f.Academy, StudentId = f.Student.Id, BatchId = f.Batch.Id, Status = null! });
        await Assert.ThrowsAsync<DbUpdateException>(() => f.Db.SaveChangesAsync());
        await using var fresh = new AcademyDeskDbContext(f.Options);
        Assert.Empty(await fresh.Enrollments.ToListAsync());
        Assert.Empty(await fresh.Certificates.ToListAsync());
        Assert.Empty(await fresh.AuditLogs.ToListAsync());
    }

    [Theory]
    [InlineData("active")]
    [InlineData("ACTIVE")]
    [InlineData("completed")]
    [InlineData("cOmPlEtEd")]
    public async Task Eligible_status_comparison_is_case_insensitive(string status)
    {
        await using var f = await Fixture.Create();
        f.Db.Enrollments.Add(new Enrollment { AcademyId = f.Academy, StudentId = f.Student.Id, BatchId = f.Batch.Id, Status = status });
        await f.Db.SaveChangesAsync();
        await AssertCreated(f, f.Batch.Id);
    }

    [Theory]
    [InlineData("Active", false, true)]
    [InlineData("Completed", false, true)]
    [InlineData("Active", true, false)]
    [InlineData("Completed", true, false)]
    public async Task Existing_inactive_person_and_batch_behavior_is_preserved(string status, bool studentActive, bool batchActive)
    {
        await using var f = await Fixture.Create();
        f.Student.IsActive = studentActive;
        f.Batch.IsActive = batchActive;
        f.Db.Enrollments.Add(new Enrollment { AcademyId = f.Academy, StudentId = f.Student.Id, BatchId = f.Batch.Id, Status = status });
        await f.Db.SaveChangesAsync();
        await AssertCreated(f, f.Batch.Id);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Any_eligible_enrollment_is_sufficient_regardless_of_row_order(bool eligibleFirst)
    {
        await using var f = await Fixture.Create();
        foreach (var status in eligibleFirst ? new[] { "Completed", "Waitlisted" } : new[] { "Waitlisted", "Completed" })
            f.Db.Enrollments.Add(new Enrollment { AcademyId = f.Academy, StudentId = f.Student.Id, BatchId = f.Batch.Id, Status = status });
        await f.Db.SaveChangesAsync();
        await AssertCreated(f, f.Batch.Id);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Enrollment_dates_do_not_introduce_an_unapproved_eligibility_rule(bool futureStart)
    {
        await using var f = await Fixture.Create();
        f.Db.Enrollments.Add(new Enrollment {
            AcademyId = f.Academy, StudentId = f.Student.Id, BatchId = f.Batch.Id, Status = "Active",
            StartDate = futureStart ? new DateOnly(2099, 1, 1) : new DateOnly(1999, 1, 1),
            EndDate = futureStart ? null : new DateOnly(2000, 1, 1)
        });
        await f.Db.SaveChangesAsync();
        await AssertCreated(f, f.Batch.Id);
    }

    private static async Task AssertEnrollmentRejected(Fixture f)
    {
        var before = await SourceSnapshot(f.Db);
        var result = await new CertificatesController(f.Db, new TestEnvironment()).Issue(f.Academy, Request(f.Student.Id, f.Batch.Id), default);
        var error = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Select a class or batch where this student has an Active or Completed enrollment.", JsonSerializer.SerializeToElement(error.Value).GetProperty("message").GetString());
        await using var fresh = new AcademyDeskDbContext(f.Options);
        Assert.Empty(await fresh.Certificates.ToListAsync());
        Assert.Empty(await fresh.AuditLogs.ToListAsync());
        Assert.Equal(before, await SourceSnapshot(fresh));
        Assert.DoesNotContain(f.Db.ChangeTracker.Entries(), x => x.State == EntityState.Added);
    }

    private static IssueCertificateRequest Request(Guid student, Guid? batch) =>
        new(student, batch, "  Synthetic recital  ", null, null, null, null, null, new DateOnly(2020, 6, 1), null);

    private static async Task AssertCreated(Fixture f, Guid? batch)
    {
        var before = await SourceSnapshot(f.Db);
        var result = await new CertificatesController(f.Db, new TestEnvironment()).Issue(f.Academy, Request(f.Student.Id, batch), default);
        var created = Assert.IsType<CreatedResult>(result.Result);
        Assert.Equal(201, created.StatusCode);
        var returned = Assert.IsType<CertificateSummary>(created.Value);
        await using var fresh = new AcademyDeskDbContext(f.Options);
        var row = Assert.Single(await fresh.Certificates.ToListAsync());
        Assert.Equal(f.Academy, row.AcademyId);
        Assert.Equal(f.Student.Id, row.StudentId);
        Assert.Equal(batch, row.BatchId);
        Assert.Equal("Synthetic recital", row.Title);
        Assert.Equal("music-recital", row.TemplateKey);
        Assert.Equal("none", row.DesignKey);
        Assert.Equal((50, 30, 72), (row.ArtworkX, row.ArtworkY, row.ArtworkSize));
        Assert.Equal(new DateOnly(2020, 6, 1), row.IssuedDate);
        Assert.Equal("Issued", row.Status);
        Assert.Null(row.Notes);
        Assert.Matches("^CERT-[0-9]{14}-[0-9]{3}$", row.CertificateNumber);
        Assert.Matches("^[A-F0-9]{16}$", row.VerificationCode);
        Assert.Equal(new CertificateSummary(row.Id, row.CertificateNumber, row.StudentId, row.BatchId, row.Title, row.TemplateKey, row.DesignKey, row.ArtworkX, row.ArtworkY, row.ArtworkSize, row.VerificationCode, row.IssuedDate, row.Status, row.Notes), returned);
        Assert.Equal($"/api/academies/{f.Academy}/certificates/{row.Id}", created.Location);
        var audit = Assert.Single(await fresh.AuditLogs.ToListAsync());
        Assert.Equal(f.Academy, audit.AcademyId);
        Assert.Equal("CertificateIssued", audit.Action);
        Assert.Equal("Certificate", audit.EntityType);
        Assert.Equal(row.Id, audit.EntityId);
        Assert.Equal(before, await SourceSnapshot(fresh));
    }

    private static async Task<string> SourceSnapshot(AcademyDeskDbContext db) => JsonSerializer.Serialize(new
    {
        students = await db.Students.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
        batches = await db.Batches.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
        enrollments = await db.Enrollments.AsNoTracking().OrderBy(x => x.Id).ToListAsync()
    });

    private sealed class Fixture : IAsyncDisposable
    {
        public Guid Academy { get; } = Guid.NewGuid();
        public Guid ForeignAcademy { get; } = Guid.NewGuid();
        public DbContextOptions<AcademyDeskDbContext> Options { get; } = new DbContextOptionsBuilder<AcademyDeskDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        public AcademyDeskDbContext Db { get; }
        public Student Student { get; private set; } = null!;
        public Student OtherStudent { get; private set; } = null!;
        public Student ForeignStudent { get; private set; } = null!;
        public Batch Batch { get; private set; } = null!;
        public Batch OtherBatch { get; private set; } = null!;
        public Batch ForeignBatch { get; private set; } = null!;
        private Fixture() => Db = new(Options);
        public static async Task<Fixture> Create()
        {
            var f = new Fixture();
            f.Student = new() { AcademyId = f.Academy, FirstName = "Synthetic", LastName = "Learner" };
            f.OtherStudent = new() { AcademyId = f.Academy, FirstName = "Other", LastName = "Learner" };
            f.ForeignStudent = new() { AcademyId = f.ForeignAcademy, FirstName = "Foreign", LastName = "Learner" };
            f.Batch = new() { AcademyId = f.Academy, Name = "Requested recital" };
            f.OtherBatch = new() { AcademyId = f.Academy, Name = "Actual enrollment" };
            f.ForeignBatch = new() { AcademyId = f.ForeignAcademy, Name = "Foreign batch" };
            f.Db.AddRange(f.Student, f.OtherStudent, f.ForeignStudent, f.Batch, f.OtherBatch, f.ForeignBatch);
            await f.Db.SaveChangesAsync();
            return f;
        }
        public ValueTask DisposeAsync() => Db.DisposeAsync();
    }

    private sealed class TestEnvironment : IWebHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Testing";
        public string ApplicationName { get; set; } = "Certificate enrollment baseline";
        public string WebRootPath { get; set; } = "";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string ContentRootPath { get; set; } = "";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
