using System.Reflection;
using System.Text.Json;
using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace AcademyDesk.Api.Tests;

public sealed class SubmissionCreationIdentityTests
{
    [Theory]
    [InlineData(-1,true)] [InlineData(0,true)] [InlineData(4000,true)] [InlineData(4001,false)]
    public void Mvc_record_validation_uses_constructor_metadata(int length,bool valid)
    {
        using var services=new ServiceCollection().AddLogging().AddMvc().Services.BuildServiceProvider();
        var context=new ActionContext{HttpContext=new DefaultHttpContext{RequestServices=services}};
        services.GetRequiredService<IObjectModelValidator>().Validate(context,null,"",new CreateAssignmentSubmissionRequest(Guid.NewGuid(),Guid.NewGuid(),length<0?null:new string('x',length)));
        Assert.Equal(valid,context.ModelState.IsValid);Assert.Equal(valid?0:1,context.ModelState.ErrorCount);
    }
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static JsonElement Json(object? value) => JsonSerializer.SerializeToElement(value, JsonOptions);
    // Deserialize into the real declared request type, so the same tests run before/after the DTO repair.
    private static async Task<ActionResult<AssignmentSubmission>> Submit(AssignmentSubmissionsController c, Guid academy, object request)
    {
        var method = typeof(AssignmentSubmissionsController).GetMethod("Submit")!;
        var body = JsonSerializer.Deserialize(Json(request).GetRawText(), method.GetParameters()[1].ParameterType, JsonOptions);
        return await (Task<ActionResult<AssignmentSubmission>>)method.Invoke(c, [academy, body, CancellationToken.None])!;
    }
    public static IEnumerable<object[]> InvalidCases() => new[] { "MissingStudent", "ForeignStudent", "EmptyStudent", "MissingAssignment", "ForeignAssignment", "EmptyAssignment", "Unpublished", "WrongPrivateStudent", "NoEnrollment", "WrongBatch", "ForeignEnrollment", "InactiveEnrollment", "ForeignBatch", "MissingBatch", "TooLong" }.Select(x=>new object[]{x});
    [Theory, MemberData(nameof(InvalidCases))]
    public async Task Invalid_identity_scope_or_input_is_rejected_without_writes(string mode)
    {
        var f=await Fixture(); await using var db=f.Db;
        var studentId=f.Student.Id;var assignmentId=f.Assignment.Id;string? response="Synthetic response";
        switch(mode)
        {
            case "MissingStudent": studentId=Guid.NewGuid();break;
            case "ForeignStudent": f.Student.AcademyId=f.Foreign;break;
            case "EmptyStudent": studentId=Guid.Empty;break;
            case "MissingAssignment": assignmentId=Guid.NewGuid();break;
            case "ForeignAssignment": f.Assignment.AcademyId=f.Foreign;break;
            case "EmptyAssignment": assignmentId=Guid.Empty;break;
            case "Unpublished": f.Assignment.IsPublished=false;break;
            case "WrongPrivateStudent": f.Assignment.StudentId=Guid.NewGuid();break;
            case "NoEnrollment": db.Remove(f.Enrollment);break;
            case "WrongBatch": f.Enrollment.BatchId=Guid.NewGuid();break;
            case "ForeignEnrollment": f.Enrollment.AcademyId=f.Foreign;break;
            case "InactiveEnrollment": f.Enrollment.Status="Withdrawn";break;
            case "ForeignBatch": f.Batch.AcademyId=f.Foreign;break;
            case "MissingBatch": db.Remove(f.Batch);break;
            case "TooLong": response=new string('x',4001);break;
        }
        await db.SaveChangesAsync();var before=await Snapshot(db);
        var result=(await Submit(new(db),f.Academy,new{assignmentId,studentId,responseText=response})).Result;
        Assert.Equal(400,Assert.IsAssignableFrom<Microsoft.AspNetCore.Mvc.Infrastructure.IStatusCodeActionResult>(result).StatusCode);
        Assert.True(JsonElement.DeepEquals(before,await Snapshot(db)));Assert.DoesNotContain(db.ChangeTracker.Entries<AssignmentSubmission>(),x=>x.State==EntityState.Added);
    }
    [Theory]
    [InlineData(false,null)] [InlineData(false,"")] [InlineData(false,"  Synthetic response  ")]
    [InlineData(true,null)] [InlineData(true,"Private work")]
    public async Task Eligible_creation_owns_all_lifecycle_fields_and_preserves_optional_response(bool isPrivate,string? response)
    {
        var f=await Fixture();await using var db=f.Db;if(isPrivate)f.Assignment.StudentId=f.Student.Id;await db.SaveChangesAsync();var before=await Snapshot(db);var start=DateTime.UtcNow;
        var spoofId=Guid.NewGuid();var result=await Submit(new(db),f.Academy,new{id=spoofId,academyId=f.Foreign,assignmentId=f.Assignment.Id,studentId=f.Student.Id,responseText=response,status="Reviewed",teacherFeedback="Forged feedback",submittedAtUtc="2001-01-01T00:00:00Z",createdAtUtc="2001-01-01T00:00:00Z",updatedAtUtc="2001-01-01T00:00:00Z"});
        var saved=Assert.IsType<AssignmentSubmission>(Assert.IsType<OkObjectResult>(result.Result).Value);var end=DateTime.UtcNow;
        Assert.NotEqual(spoofId,saved.Id);Assert.Equal(f.Academy,saved.AcademyId);Assert.Equal(f.Assignment.Id,saved.AssignmentId);Assert.Equal(f.Student.Id,saved.StudentId);Assert.Equal(response,saved.ResponseText);Assert.Equal("Submitted",saved.Status);Assert.Null(saved.TeacherFeedback);Assert.Null(saved.UpdatedAtUtc);Assert.InRange(saved.SubmittedAtUtc,start,end);Assert.InRange(saved.CreatedAtUtc,start,end);
        Assert.True(JsonElement.DeepEquals(Json(saved),Json(await db.AssignmentSubmissions.AsNoTracking().SingleAsync())));
        var after=await Snapshot(db);foreach(var field in before.EnumerateObject().Where(x=>x.Name!="submissions"))Assert.True(JsonElement.DeepEquals(field.Value,after.GetProperty(field.Name)));
    }
    [Theory] [InlineData("Submitted")] [InlineData("Reviewed")]
    public async Task Duplicate_create_never_replaces_existing_work_or_feedback(string status)
    {
        var f=await Fixture();await using var db=f.Db;db.Add(new AssignmentSubmission{AcademyId=f.Academy,AssignmentId=f.Assignment.Id,StudentId=f.Student.Id,Status=status,ResponseText="Original",TeacherFeedback="Original feedback"});await db.SaveChangesAsync();var before=await Snapshot(db);
        Assert.IsType<ConflictObjectResult>((await Submit(new(db),f.Academy,new{assignmentId=f.Assignment.Id,studentId=f.Student.Id,responseText="Replacement"})).Result);Assert.True(JsonElement.DeepEquals(before,await Snapshot(db)));
    }
    [Fact]
    public async Task Maximum_response_and_distinct_student_key_are_accepted()
    {
        var f=await Fixture();await using var db=f.Db;db.Add(new AssignmentSubmission{AcademyId=f.Academy,AssignmentId=f.Assignment.Id,StudentId=Guid.NewGuid(),ResponseText="Other learner"});await db.SaveChangesAsync();
        var result=await Submit(new(db),f.Academy,new{assignmentId=f.Assignment.Id,studentId=f.Student.Id,responseText=new string('x',4000)});var row=Assert.IsType<AssignmentSubmission>(Assert.IsType<OkObjectResult>(result.Result).Value);Assert.Equal(4000,row.ResponseText!.Length);Assert.Equal(2,await db.AssignmentSubmissions.CountAsync());
    }
    [Fact]
    public async Task No_new_inactive_student_batch_or_enrollment_date_policy_is_inferred()
    {
        var f=await Fixture();await using var db=f.Db;f.Student.IsActive=false;f.Batch.IsActive=false;f.Enrollment.StartDate=new DateOnly(2100,1,1);f.Enrollment.EndDate=new DateOnly(2000,1,1);await db.SaveChangesAsync();
        Assert.IsType<OkObjectResult>((await Submit(new(db),f.Academy,new{assignmentId=f.Assignment.Id,studentId=f.Student.Id,responseText=(string?)null})).Result);
    }
    private sealed record Seed(AcademyDeskDbContext Db,Guid Academy,Guid Foreign,Student Student,Batch Batch,Assignment Assignment,Enrollment Enrollment);
    private static async Task<Seed> Fixture()
    {
        var db=new AcademyDeskDbContext(new DbContextOptionsBuilder<AcademyDeskDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);var academy=Guid.NewGuid();var foreign=Guid.NewGuid();
        var student=new Student{AcademyId=academy,FirstName="Synthetic",LastName="Student"};var batch=new Batch{AcademyId=academy,Name="Synthetic batch"};var assignment=new Assignment{AcademyId=academy,BatchId=batch.Id,Title="Published work",IsPublished=true};var enrollment=new Enrollment{AcademyId=academy,StudentId=student.Id,BatchId=batch.Id};db.AddRange(student,batch,assignment,enrollment);await db.SaveChangesAsync();return new(db,academy,foreign,student,batch,assignment,enrollment);
    }
    private static async Task<JsonElement> Snapshot(AcademyDeskDbContext db)=>Json(new{submissions=await db.AssignmentSubmissions.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),students=await db.Students.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),assignments=await db.Assignments.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),batches=await db.Batches.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),enrollments=await db.Enrollments.AsNoTracking().OrderBy(x=>x.Id).ToListAsync()});
}
