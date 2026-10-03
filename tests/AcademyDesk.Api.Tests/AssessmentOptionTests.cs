using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Tests;

public sealed class AssessmentOptionTests
{
    private static AcademyDeskDbContext Database() => new(new DbContextOptionsBuilder<AcademyDeskDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    private static async Task<AssessmentOptions> Read(AcademyDeskDbContext db, Guid academy) => Assert.IsType<AssessmentOptions>(Assert.IsType<OkObjectResult>((await new AssessmentsController(db).Options(academy, default)).Result).Value);

    [Fact]
    public async Task Options_are_owned_minimal_ordered_and_deduplicate_active_roster()
    {
        await using var db=Database(); var academy=Guid.NewGuid(); var foreign=Guid.NewGuid();
        var alpha=new Batch {AcademyId=academy,Name="Alpha",CourseId=Guid.NewGuid()}; var zeta=new Batch {AcademyId=academy,Name="Zeta",CourseId=Guid.NewGuid()};
        var foreignBatch=new Batch {AcademyId=foreign,Name="Foreign",CourseId=Guid.NewGuid()};
        var amy=new Student {AcademyId=academy,FirstName="Amy",LastName="Alpha",Email="private@example.invalid"};
        var zed=new Student {AcademyId=academy,FirstName="Zed",LastName="Alpha",IsActive=false};
        var foreignStudent=new Student {AcademyId=foreign,FirstName="Foreign",LastName="Student"};
        var unrelated=new Student {AcademyId=academy,FirstName="Unrelated",LastName="Student"};
        db.AddRange(alpha,zeta,foreignBatch,amy,zed,foreignStudent,unrelated);
        foreach(var s in new[]{amy,zed})db.Add(new Enrollment {AcademyId=academy,BatchId=alpha.Id,StudentId=s.Id});
        db.Add(new Enrollment {AcademyId=academy,BatchId=alpha.Id,StudentId=amy.Id});
        db.Add(new Enrollment {AcademyId=academy,BatchId=zeta.Id,StudentId=amy.Id});
        db.Add(new Enrollment {AcademyId=academy,BatchId=alpha.Id,StudentId=foreignStudent.Id});
        db.Add(new Enrollment {AcademyId=academy,BatchId=foreignBatch.Id,StudentId=unrelated.Id});
        db.Add(new Enrollment {AcademyId=foreign,BatchId=alpha.Id,StudentId=unrelated.Id});
        db.AddRange(new GradingScheme {AcademyId=academy,Name="Zeta",PassingPercent=75},new GradingScheme {AcademyId=academy,Name="Alpha",PassingPercent=50},
            new GradingScheme {AcademyId=academy,Name="Inactive",IsActive=false},new GradingScheme {AcademyId=foreign,Name="Foreign"});
        await db.SaveChangesAsync(); db.ChangeTracker.Clear(); var options=await Read(db,academy);
        Assert.Equal(new[]{"Alpha","Zeta"},options.Batches.Select(x=>x.Name)); Assert.Equal(new[]{amy.Id,zed.Id},options.Students.Select(x=>x.Id));
        Assert.Equal(3,options.Enrollments.Count); Assert.All(options.Enrollments,x=>Assert.Equal("Active",x.Status));
        Assert.Equal(new[]{"Alpha","Zeta"},options.GradingSchemes.Select(x=>x.Name)); Assert.Equal(50,options.GradingSchemes[0].PassingPercent);
        Assert.Empty(db.ChangeTracker.Entries()); Assert.Empty(await db.AuditLogs.ToListAsync()); Assert.Equal(7,await db.Enrollments.CountAsync());
    }

    [Theory]
    [InlineData("Paused")]
    [InlineData("Completed")]
    [InlineData("Waitlisted")]
    [InlineData("Withdrawn")]
    [InlineData("Cancelled")]
    [InlineData("Transferred")]
    public async Task Nonactive_membership_is_not_in_the_existing_active_editor_roster(string status)
    {
        await using var db=Database(); var academy=Guid.NewGuid(); var batch=new Batch {AcademyId=academy,Name="Roster",CourseId=Guid.NewGuid()};
        var student=new Student {AcademyId=academy,FirstName="Synthetic",LastName="Learner"};
        db.AddRange(batch,student,new Enrollment {AcademyId=academy,BatchId=batch.Id,StudentId=student.Id,Status=status}); await db.SaveChangesAsync();
        var options=await Read(db,academy); Assert.Single(options.Batches); Assert.Empty(options.Students); Assert.Empty(options.Enrollments);
        Assert.Equal(status,(await db.Enrollments.SingleAsync()).Status);
    }

    [Fact]
    public async Task Empty_tenant_options_are_empty_arrays_not_foreign_data()
    {
        await using var db=Database(); db.Add(new GradingScheme {AcademyId=Guid.NewGuid(),Name="Foreign"}); await db.SaveChangesAsync();
        var options=await Read(db,Guid.NewGuid()); Assert.Empty(options.Batches);Assert.Empty(options.Students);Assert.Empty(options.Enrollments);Assert.Empty(options.GradingSchemes);
    }

    [Fact]
    public void Contract_exposes_only_selector_fields_under_existing_academic_gate()
    {
        static string[] Names(Type type)=>type.GetProperties().Select(x=>x.Name).OrderBy(x=>x,StringComparer.Ordinal).ToArray();
        Assert.Equal(new[]{"Id","Name"},Names(typeof(AssessmentBatchOption)));
        Assert.Equal(new[]{"FirstName","Id","LastName"},Names(typeof(AssessmentStudentOption)));
        Assert.Equal(new[]{"BatchId","Status","StudentId"},Names(typeof(AssessmentEnrollmentOption)));
        Assert.Equal(new[]{"Id","Name","PassingPercent"},Names(typeof(AssessmentSchemeOption)));
        Assert.Equal(new[]{"academics.manage"},PermissionCatalog.RequiredFor(nameof(AssessmentsController)));
        Assert.Equal("AcademicGovernance",SubscriptionPlanCatalog.ModuleForController(nameof(AssessmentsController)));
    }
}
