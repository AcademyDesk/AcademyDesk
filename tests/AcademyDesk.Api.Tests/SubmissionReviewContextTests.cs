using System.Text.Json;
using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Tests;

public sealed class SubmissionReviewContextTests
{
    private static JsonElement Json(object? value) => JsonSerializer.SerializeToElement(value, new JsonSerializerOptions(JsonSerializerDefaults.Web));
    [Theory]
    [InlineData("Owned", "Owned", "Owned")]
    [InlineData("Foreign", "Owned", "Owned")]
    [InlineData("Missing", "Owned", "Owned")]
    [InlineData("Owned", "Foreign", "Owned")]
    [InlineData("Owned", "Missing", "Owned")]
    [InlineData("Owned", "Owned", "Foreign")]
    [InlineData("Owned", "Owned", "Missing")]
    public async Task Context_is_scoped_and_legacy_fields_preserved(string studentScope, string assignmentScope, string batchScope)
    {
        await using var db = NewDb(); var academy = Guid.NewGuid(); var foreign = Guid.NewGuid();
        var student = new Student { AcademyId = studentScope == "Foreign" ? foreign : academy, FirstName = "Synthetic", LastName = "Learner", StudentNumber = "QA-1" };
        var batch = new Batch { AcademyId = batchScope == "Foreign" ? foreign : academy, Name = "Synthetic batch" };
        var assignment = new Assignment { AcademyId = assignmentScope == "Foreign" ? foreign : academy, BatchId = batch.Id, Title = "Synthetic assignment" };
        var row = new AssignmentSubmission { AcademyId = academy, StudentId = student.Id, AssignmentId = assignment.Id, ResponseText = null, TeacherFeedback = null };
        if(studentScope != "Missing") db.Add(student); if(batchScope != "Missing") db.Add(batch); if(assignmentScope != "Missing") db.Add(assignment); db.Add(row); await db.SaveChangesAsync();
        var result = await new AssignmentSubmissionsController(db).List(academy, null, default);
        var item = Json(Assert.IsType<OkObjectResult>(result.Result).Value)[0];
        foreach(var field in Json(row).EnumerateObject()) Assert.True(JsonElement.DeepEquals(field.Value, item.GetProperty(field.Name)), field.Name);
        Assert.Equal(studentScope == "Owned" ? "Synthetic Learner" : null, item.GetProperty("studentName").GetString());
        Assert.Equal(studentScope == "Owned" ? "QA-1" : null, item.GetProperty("studentNumber").GetString());
        Assert.Equal(assignmentScope == "Owned" ? assignment.Title : null, item.GetProperty("assignmentTitle").GetString());
        Assert.Equal(assignmentScope == "Owned" && batchScope == "Owned" ? batch.Name : null, item.GetProperty("batchName").GetString());
        Assert.Equal(assignmentScope == "Owned" && batchScope == "Owned" ? batch.Id : (Guid?)null, item.GetProperty("batchId").ValueKind == JsonValueKind.Null ? null : item.GetProperty("batchId").GetGuid());
    }
    [Fact]
    public async Task Filter_order_and_exact_review_target_preserve_other_rows()
    {
        await using var db = NewDb(); var academy = Guid.NewGuid(); var foreign = Guid.NewGuid();
        var student = new Student { AcademyId = academy, FirstName = "Same", LastName = "Name" }; var batch = new Batch { AcademyId = academy, Name = "QA batch" };
        var a = new Assignment { AcademyId = academy, BatchId = batch.Id, Title = "Work one" }; var b = new Assignment { AcademyId = academy, BatchId = batch.Id, Title = "Work two" };
        var first = new AssignmentSubmission { AcademyId = academy, StudentId = student.Id, AssignmentId = a.Id, ResponseText = "Identical", SubmittedAtUtc = DateTime.UtcNow.AddMinutes(-1) };
        var second = new AssignmentSubmission { AcademyId = academy, StudentId = student.Id, AssignmentId = b.Id, ResponseText = "Identical" };
        var outsider = new AssignmentSubmission { AcademyId = foreign, StudentId = student.Id, AssignmentId = b.Id, ResponseText = "Identical" };
        db.AddRange(student,batch,a,b,first,second,outsider); await db.SaveChangesAsync(); var original = Json(first); var foreignBefore = Json(outsider); var c = new AssignmentSubmissionsController(db);
        var list = Json(Assert.IsType<OkObjectResult>((await c.List(academy,null,default)).Result).Value); Assert.Equal(2,list.GetArrayLength()); Assert.Equal(second.Id,list[0].GetProperty("id").GetGuid());
        var filtered = Json(Assert.IsType<OkObjectResult>((await c.List(academy,a.Id,default)).Result).Value); Assert.Equal(1,filtered.GetArrayLength()); Assert.Equal(first.Id,filtered[0].GetProperty("id").GetGuid());
        var reviewed = Json(Assert.IsType<OkObjectResult>((await c.Review(academy,second.Id,new("Target feedback"),default)).Result).Value);
        Assert.Equal(second.Id,reviewed.GetProperty("id").GetGuid()); Assert.Equal("Work two",reviewed.GetProperty("assignmentTitle").GetString()); Assert.Equal("Reviewed",reviewed.GetProperty("status").GetString()); Assert.Equal("Target feedback",reviewed.GetProperty("teacherFeedback").GetString());
        Assert.True(JsonElement.DeepEquals(original,Json(await db.AssignmentSubmissions.AsNoTracking().SingleAsync(x=>x.Id==first.Id)))); Assert.True(JsonElement.DeepEquals(foreignBefore,Json(await db.AssignmentSubmissions.AsNoTracking().SingleAsync(x=>x.Id==outsider.Id))));
        Assert.IsType<NotFoundResult>((await c.Review(academy,outsider.Id,new("Forbidden"),default)).Result); Assert.IsType<NotFoundResult>((await c.Review(academy,Guid.NewGuid(),new(null),default)).Result);
    }
    private static AcademyDeskDbContext NewDb() => new(new DbContextOptionsBuilder<AcademyDeskDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
}
