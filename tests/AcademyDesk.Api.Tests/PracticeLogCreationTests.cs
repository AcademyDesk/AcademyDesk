using System.Text.Json;
using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace AcademyDesk.Api.Tests;

public sealed class PracticeLogCreationTests
{
    [Theory]
    [InlineData(1,250,2000,true)] [InlineData(1440,0,0,true)] [InlineData(0,0,0,false)]
    [InlineData(1441,0,0,false)] [InlineData(30,251,0,false)] [InlineData(30,0,2001,false)] [InlineData(30,-1,-1,true)]
    public void Mvc_record_validation_handles_optional_and_boundary_fields(int minutes,int focusLength,int notesLength,bool valid)
    {
        using var services=new ServiceCollection().AddLogging().AddMvc().Services.BuildServiceProvider();
        var context=new ActionContext{HttpContext=new DefaultHttpContext{RequestServices=services}};
        services.GetRequiredService<IObjectModelValidator>().Validate(context,null,"",new CreatePracticeLogRequest(Guid.NewGuid(),null,minutes,focusLength<0?null:new string('x',focusLength),notesLength<0?null:new string('x',notesLength)));
        Assert.Equal(valid,context.ModelState.IsValid);Assert.Equal(valid?0:1,context.ModelState.ErrorCount);
    }
    private static readonly JsonSerializerOptions JsonOptions=new(JsonSerializerDefaults.Web);
    private static JsonElement Json(object? value)=>JsonSerializer.SerializeToElement(value,JsonOptions);
    private static async Task<ActionResult<PracticeLog>> Create(PracticeLogsController controller,Guid academy,object body)
    {
        var method=typeof(PracticeLogsController).GetMethod("Create")!;
        var request=JsonSerializer.Deserialize(Json(body).GetRawText(),method.GetParameters()[1].ParameterType,JsonOptions);
        return await (Task<ActionResult<PracticeLog>>)method.Invoke(controller,[academy,request,CancellationToken.None])!;
    }
    [Theory]
    [InlineData("Missing")] [InlineData("Foreign")] [InlineData("Inactive")] [InlineData("Empty")]
    [InlineData("NegativeMinutes")] [InlineData("ZeroMinutes")] [InlineData("TooManyMinutes")]
    [InlineData("FutureDate")] [InlineData("LongFocus")] [InlineData("LongNotes")]
    public async Task Invalid_learner_or_boundary_is_rejected_without_writes(string mode)
    {
        var f=await Fixture();await using var db=f.Db;var studentId=f.Student.Id;var minutes=30;var date=DateOnly.FromDateTime(DateTime.UtcNow);string? focus=null,notes=null;
        switch(mode){case "Missing":studentId=Guid.NewGuid();break;case "Foreign":f.Student.AcademyId=f.Foreign;break;case "Inactive":f.Student.IsActive=false;break;case "Empty":studentId=Guid.Empty;break;case "NegativeMinutes":minutes=-1;break;case "ZeroMinutes":minutes=0;break;case "TooManyMinutes":minutes=1441;break;case "FutureDate":date=date.AddDays(1);break;case "LongFocus":focus=new string('x',251);break;case "LongNotes":notes=new string('x',2001);break;}
        await db.SaveChangesAsync();var before=await Snapshot(db);var result=await Create(new(db),f.Academy,new{studentId,practiceDate=date,minutesPracticed=minutes,focusArea=focus,notes});
        Assert.Equal(400,Assert.IsAssignableFrom<IStatusCodeActionResult>(result.Result).StatusCode);Assert.True(JsonElement.DeepEquals(before,await Snapshot(db)));Assert.DoesNotContain(db.ChangeTracker.Entries<PracticeLog>(),x=>x.State==EntityState.Added);
    }
    [Theory]
    [InlineData(1,"Empty")] [InlineData(1440,"Maximum")] [InlineData(30,"Whitespace")] [InlineData(30,"Null")]
    public async Task Server_lifecycle_ignores_overposting_and_preserves_optional_input(int minutes,string variant)
    {
        var f=await Fixture();await using var db=f.Db;string? focus=variant switch{"Empty"=>"","Maximum"=>new string('x',250),"Whitespace"=>"  Focus  ",_=>null};string? notes=variant switch{"Empty"=>"","Maximum"=>new string('x',2000),"Whitespace"=>"  Notes  ",_=>null};var date=new DateOnly(2020,2,29);var spoof=Guid.NewGuid();var before=await Snapshot(db);var start=DateTime.UtcNow;
        var result=await Create(new(db),f.Academy,new{id=spoof,academyId=f.Foreign,studentId=f.Student.Id,practiceDate=date,minutesPracticed=minutes,focusArea=focus,notes,status="Reviewed",teacherFeedback="Forged",reviewedAtUtc="2001-01-01T00:00:00Z",createdAtUtc="2001-01-01T00:00:00Z",updatedAtUtc="2001-01-01T00:00:00Z"});var row=Assert.IsType<PracticeLog>(Assert.IsType<OkObjectResult>(result.Result).Value);
        Assert.NotEqual(spoof,row.Id);Assert.Equal(f.Academy,row.AcademyId);Assert.Equal(f.Student.Id,row.StudentId);Assert.Equal(date,row.PracticeDate);Assert.Equal(minutes,row.MinutesPracticed);Assert.Equal(focus,row.FocusArea);Assert.Equal(notes,row.Notes);Assert.Equal("Logged",row.Status);Assert.Null(row.TeacherFeedback);Assert.Null(row.ReviewedAtUtc);Assert.Null(row.UpdatedAtUtc);Assert.InRange(row.CreatedAtUtc,start,DateTime.UtcNow);
        Assert.True(JsonElement.DeepEquals(Json(row),Json(await db.PracticeLogs.AsNoTracking().SingleAsync())));Assert.True(JsonElement.DeepEquals(before.GetProperty("students"),(await Snapshot(db)).GetProperty("students")));
    }
    [Fact]
    public async Task Omitted_date_defaults_to_today_without_requiring_optional_fields()
    {
        var f=await Fixture();await using var db=f.Db;var start=DateOnly.FromDateTime(DateTime.UtcNow);var result=await Create(new(db),f.Academy,new{studentId=f.Student.Id,minutesPracticed=30});var row=Assert.IsType<PracticeLog>(Assert.IsType<OkObjectResult>(result.Result).Value);Assert.InRange(row.PracticeDate,start,DateOnly.FromDateTime(DateTime.UtcNow));Assert.Null(row.FocusArea);Assert.Null(row.Notes);
    }
    [Fact]
    public async Task Repeated_day_entries_never_replace_reviewed_work_and_list_is_scoped_ordered()
    {
        var f=await Fixture();await using var db=f.Db;var day=new DateOnly(2020,2,29);var original=new PracticeLog{AcademyId=f.Academy,StudentId=f.Student.Id,PracticeDate=day,MinutesPracticed=5,Status="Reviewed",TeacherFeedback="Preserve",ReviewedAtUtc=DateTime.UtcNow};var outsider=new PracticeLog{AcademyId=f.Foreign,StudentId=Guid.NewGuid(),MinutesPracticed=10};db.AddRange(original,outsider);await db.SaveChangesAsync();var originalBefore=Json(original);var outsiderBefore=Json(outsider);
        var controller=new PracticeLogsController(db);foreach(var date in new[]{day,day,day.AddDays(1)})Assert.IsType<OkObjectResult>((await Create(controller,f.Academy,new{studentId=f.Student.Id,practiceDate=date,minutesPracticed=30})).Result);
        Assert.Equal(5,await db.PracticeLogs.CountAsync());Assert.True(JsonElement.DeepEquals(originalBefore,Json(await db.PracticeLogs.AsNoTracking().SingleAsync(x=>x.Id==original.Id))));Assert.True(JsonElement.DeepEquals(outsiderBefore,Json(await db.PracticeLogs.AsNoTracking().SingleAsync(x=>x.Id==outsider.Id))));
        var list=Assert.IsAssignableFrom<IReadOnlyList<PracticeLog>>(Assert.IsType<OkObjectResult>((await controller.List(f.Academy,default)).Result).Value);Assert.Equal(4,list.Count);Assert.All(list,x=>Assert.Equal(f.Academy,x.AcademyId));Assert.Equal(day.AddDays(1),list[0].PracticeDate);
    }
    [Theory] [InlineData(null)] [InlineData("")] [InlineData("  Actual feedback  ")]
    public async Task Separate_review_sets_only_review_fields_for_the_selected_owned_log(string? feedback)
    {
        var f=await Fixture();await using var db=f.Db;var controller=new PracticeLogsController(db);var row=Assert.IsType<PracticeLog>(Assert.IsType<OkObjectResult>((await Create(controller,f.Academy,new{studentId=f.Student.Id,minutesPracticed=30})).Result).Value);var other=new PracticeLog{AcademyId=f.Academy,StudentId=f.Student.Id,MinutesPracticed=10};var foreign=new PracticeLog{AcademyId=f.Foreign,StudentId=Guid.NewGuid(),MinutesPracticed=10};db.AddRange(other,foreign);await db.SaveChangesAsync();var before=await Snapshot(db);var start=DateTime.UtcNow;
        var reviewed=Assert.IsType<PracticeLog>(Assert.IsType<OkObjectResult>((await controller.Review(f.Academy,row.Id,new(feedback),default)).Result).Value);Assert.Equal("Reviewed",reviewed.Status);Assert.Equal(feedback?.Trim(),reviewed.TeacherFeedback);Assert.InRange(reviewed.ReviewedAtUtc!.Value,start,DateTime.UtcNow);var after=await Snapshot(db);
        foreach(var old in before.GetProperty("logs").EnumerateArray()){var current=after.GetProperty("logs").EnumerateArray().Single(x=>x.GetProperty("id").GetGuid()==old.GetProperty("id").GetGuid());if(old.GetProperty("id").GetGuid()!=row.Id)Assert.True(JsonElement.DeepEquals(old,current));else foreach(var field in old.EnumerateObject().Where(x=>x.Name is not("status" or "teacherFeedback" or "reviewedAtUtc")))Assert.True(JsonElement.DeepEquals(field.Value,current.GetProperty(field.Name)));}
        Assert.IsType<NotFoundResult>((await controller.Review(f.Academy,foreign.Id,new("Denied"),default)).Result);Assert.IsType<NotFoundResult>((await controller.Review(f.Academy,Guid.NewGuid(),new(null),default)).Result);
    }
    private sealed record Seed(AcademyDeskDbContext Db,Guid Academy,Guid Foreign,Student Student);
    private static async Task<Seed> Fixture(){var db=new AcademyDeskDbContext(new DbContextOptionsBuilder<AcademyDeskDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);var academy=Guid.NewGuid();var foreign=Guid.NewGuid();var student=new Student{AcademyId=academy,FirstName="Synthetic",LastName="Practice"};db.Add(student);await db.SaveChangesAsync();return new(db,academy,foreign,student);}
    private static async Task<JsonElement> Snapshot(AcademyDeskDbContext db)=>Json(new{logs=await db.PracticeLogs.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),students=await db.Students.AsNoTracking().OrderBy(x=>x.Id).ToListAsync()});
}
