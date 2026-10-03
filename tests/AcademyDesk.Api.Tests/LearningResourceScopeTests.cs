using System.Text.Json;
using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;

namespace AcademyDesk.Api.Tests;

public sealed class LearningResourceScopeTests
{
    public static IEnumerable<object[]> ScopeCases()
    {
        foreach(var upload in new[]{false,true})
        foreach(var mode in new[]{"Match","Mismatch","BatchOnly","SubjectOnly","AcademyWide","MissingBatch","ForeignBatch","EmptyBatch","MissingCourse","ForeignCourse","EmptyCourse","InactiveMatch","InactiveBatchOnly","InactiveSubjectOnly","InactiveMismatch"})
            yield return [upload,mode];
    }

    [Theory,MemberData(nameof(ScopeCases))]
    public async Task Scope_matrix_preserves_supported_audiences_and_rejects_invalid_pairs_before_writes(bool upload,string mode)
    {
        var f=await Fixture();await using var db=f.Db;using var environment=new TestEnvironment();
        Guid? batch=f.Batch.Id,course=f.Course.Id;
        if(mode.StartsWith("Inactive")){f.Batch.IsActive=false;f.Course.IsActive=false;f.OtherCourse.IsActive=false;await db.SaveChangesAsync();}
        switch(mode){
            case "Mismatch":case "InactiveMismatch":course=f.OtherCourse.Id;break;
            case "BatchOnly":case "InactiveBatchOnly":course=null;break;
            case "SubjectOnly":case "InactiveSubjectOnly":batch=null;break;
            case "AcademyWide":batch=null;course=null;break;
            case "MissingBatch":batch=Guid.NewGuid();break;case "ForeignBatch":batch=f.ForeignBatch.Id;break;case "EmptyBatch":batch=Guid.Empty;break;
            case "MissingCourse":course=Guid.NewGuid();break;case "ForeignCourse":course=f.ForeignCourse.Id;break;case "EmptyCourse":course=Guid.Empty;break;
        }
        var before=await Snapshot(db);var start=DateTime.UtcNow;var controller=new LearningResourcesController(db);
        var result=upload
            ? await controller.Upload(f.Academy,new(){Title="  Synthetic resource  ",Description=null,Type=null,BatchId=batch,CourseId=course,IsPublished=false,File=File("fixture.pdf")},environment,default)
            : await controller.Create(f.Academy,new("  Synthetic resource  ",null,null,"https://example.invalid/resource",batch,course,false),default);
        var invalid=mode.Contains("Mismatch")||mode.StartsWith("Missing")||mode.StartsWith("Foreign")||mode.StartsWith("Empty");
        if(invalid){
            var error=Assert.IsType<BadRequestObjectResult>(result);
            var expected=mode.Contains("Mismatch")?"The selected subject does not belong to the selected batch.":mode.EndsWith("Batch")?"Invalid batch.":"Invalid subject.";
            Assert.Equal(expected,Json(error.Value).GetProperty("message").GetString());
            Assert.True(JsonElement.DeepEquals(before,await Snapshot(db)));Assert.Empty(environment.Files());
            Assert.DoesNotContain(db.ChangeTracker.Entries<LearningResource>(),x=>x.State==EntityState.Added);
            return;
        }
        Assert.IsType<OkResult>(result);var row=await db.LearningResources.AsNoTracking().SingleAsync(x=>x.Title=="Synthetic resource");
        Assert.Equal(f.Academy,row.AcademyId);Assert.NotEqual(Guid.Empty,row.Id);Assert.Equal(batch,row.BatchId);Assert.Equal(course,row.CourseId);
        Assert.Null(row.StudentId);Assert.Null(row.ClassSessionId);Assert.Null(row.Description);Assert.Null(row.UpdatedAtUtc);Assert.False(row.IsPublished);Assert.InRange(row.CreatedAtUtc,start,DateTime.UtcNow);
        Assert.Equal(upload?"Document":"Link",row.Type);
        if(upload){var saved=Assert.Single(environment.Files());Assert.Equal(new byte[]{1,2,3,4},await System.IO.File.ReadAllBytesAsync(saved));Assert.Equal("/uploads/learning-resources/"+Path.GetFileName(saved),row.Url);}
        else {Assert.Equal("https://example.invalid/resource",row.Url);Assert.Empty(environment.Files());}
        var after=await Snapshot(db);
        foreach(var key in new[]{"courses","batches"})Assert.True(JsonElement.DeepEquals(before.GetProperty(key),after.GetProperty(key)));
        foreach(var old in before.GetProperty("resources").EnumerateArray())Assert.True(JsonElement.DeepEquals(old,after.GetProperty("resources").EnumerateArray().Single(x=>x.GetProperty("id").GetGuid()==old.GetProperty("id").GetGuid())));
        Assert.Equal(before.GetProperty("resources").GetArrayLength()+1,after.GetProperty("resources").GetArrayLength());
    }

    [Theory] [InlineData("Missing")] [InlineData("Empty")] [InlineData("Extension")]
    public async Task Upload_file_guards_remain_no_write(string mode)
    {
        var f=await Fixture();await using var db=f.Db;using var environment=new TestEnvironment();var before=await Snapshot(db);
        var result=await new LearningResourcesController(db).Upload(f.Academy,new(){BatchId=f.Batch.Id,CourseId=f.Course.Id,File=mode=="Missing"?null:File(mode=="Extension"?"fixture.exe":"fixture.pdf",mode=="Empty"?[]:null)},environment,default);
        Assert.IsType<BadRequestObjectResult>(result);Assert.True(JsonElement.DeepEquals(before,await Snapshot(db)));Assert.Empty(environment.Files());
    }

    [Theory] [InlineData(" ", "https://example.invalid")] [InlineData("Title","not-a-url")]
    public async Task Link_title_and_URL_guards_remain_no_write(string title,string url)
    {
        var f=await Fixture();await using var db=f.Db;var before=await Snapshot(db);
        Assert.IsType<BadRequestObjectResult>(await new LearningResourcesController(db).Create(f.Academy,new(title,null,null,url,f.Batch.Id,f.Course.Id,true),default));
        Assert.True(JsonElement.DeepEquals(before,await Snapshot(db)));
    }

    [Fact]
    public async Task Publish_changes_only_selected_owned_publication_and_list_retains_scope_and_fields()
    {
        var f=await Fixture();await using var db=f.Db;var before=await Snapshot(db);var controller=new LearningResourcesController(db);
        Assert.IsType<NotFoundResult>(await controller.Publish(f.Academy,f.ForeignResource.Id,new(true),default));
        Assert.IsType<NotFoundResult>(await controller.Publish(f.Academy,Guid.NewGuid(),new(true),default));
        Assert.True(JsonElement.DeepEquals(before,await Snapshot(db)));
        Assert.IsType<OkResult>(await controller.Publish(f.Academy,f.Resource.Id,new(true),default));
        var after=await Snapshot(db);
        foreach(var old in before.GetProperty("resources").EnumerateArray()){
            var current=after.GetProperty("resources").EnumerateArray().Single(x=>x.GetProperty("id").GetGuid()==old.GetProperty("id").GetGuid());
            foreach(var property in old.EnumerateObject())if(property.Name!="isPublished"||old.GetProperty("id").GetGuid()!=f.Resource.Id)Assert.True(JsonElement.DeepEquals(property.Value,current.GetProperty(property.Name)));
        }
        var rows=Assert.IsType<List<ResourceSummary>>(Assert.IsType<OkObjectResult>(await controller.List(f.Academy,default)).Value);var row=Assert.Single(rows);
        Assert.Equal(new ResourceSummary(f.Resource.Id,f.Resource.Title,f.Resource.Description,f.Resource.Type,f.Resource.Url,f.Resource.BatchId,f.Resource.CourseId,true),row);
        Assert.True(JsonElement.DeepEquals(after,await Snapshot(db)));
    }

    [Fact]
    public async Task Upload_blank_title_uses_filename_and_retains_trimmed_optional_text()
    {
        var f=await Fixture();await using var db=f.Db;using var environment=new TestEnvironment();
        Assert.IsType<OkResult>(await new LearningResourcesController(db).Upload(f.Academy,new(){Title=" ",Description="  Optional  ",Type="  Audio  ",File=File("recording.mp3")},environment,default));
        var row=await db.LearningResources.SingleAsync(x=>x.Title=="recording");Assert.Equal("Optional",row.Description);Assert.Equal("Audio",row.Type);Assert.True(row.IsPublished);Assert.Null(row.BatchId);Assert.Null(row.CourseId);Assert.Single(environment.Files());
    }

    private static FormFile File(string name,byte[]? bytes=null){var data=bytes??[1,2,3,4];return new(new MemoryStream(data),0,data.Length,"File",name);}
    private static JsonElement Json(object? value)=>JsonSerializer.SerializeToElement(value,new JsonSerializerOptions(JsonSerializerDefaults.Web));
    private static async Task<JsonElement> Snapshot(AcademyDeskDbContext db)=>Json(new{resources=await db.LearningResources.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),courses=await db.Courses.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),batches=await db.Batches.AsNoTracking().OrderBy(x=>x.Id).ToListAsync()});
    private sealed record Seed(AcademyDeskDbContext Db,Guid Academy,ProgramCourse Course,ProgramCourse OtherCourse,ProgramCourse ForeignCourse,Batch Batch,Batch ForeignBatch,LearningResource Resource,LearningResource ForeignResource);
    private static async Task<Seed> Fixture(){
        var db=new AcademyDeskDbContext(new DbContextOptionsBuilder<AcademyDeskDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);var academy=Guid.NewGuid();var foreign=Guid.NewGuid();
        var course=new ProgramCourse{AcademyId=academy,Name="Subject A"};var other=new ProgramCourse{AcademyId=academy,Name="Subject B"};var outsider=new ProgramCourse{AcademyId=foreign,Name="Foreign subject"};
        var batch=new Batch{AcademyId=academy,Name="Batch A",CourseId=course.Id};var foreignBatch=new Batch{AcademyId=foreign,Name="Foreign batch",CourseId=outsider.Id};
        var resource=new LearningResource{AcademyId=academy,Title="Preserve prior",Url="https://example.invalid/prior",BatchId=batch.Id,CourseId=course.Id,IsPublished=false,Description="Preserve description"};
        var foreignResource=new LearningResource{AcademyId=foreign,Title="Preserve foreign",Url="https://example.invalid/foreign",BatchId=foreignBatch.Id,CourseId=outsider.Id,IsPublished=false};
        db.AddRange(course,other,outsider,batch,foreignBatch,resource,foreignResource);await db.SaveChangesAsync();return new(db,academy,course,other,outsider,batch,foreignBatch,resource,foreignResource);
    }
    private sealed class TestEnvironment:IWebHostEnvironment,IDisposable
    {
        private readonly string root=Path.Combine(Path.GetTempPath(),"AcademyDesk-ResourceScope-"+Guid.NewGuid().ToString("N"));
        public TestEnvironment(){WebRootPath=Path.Combine(root,"wwwroot");ContentRootPath=root;}
        public string ApplicationName{get;set;}="ResourceScopeTests";public string EnvironmentName{get;set;}="Testing";
        public string WebRootPath{get;set;}public string ContentRootPath{get;set;}
        public IFileProvider WebRootFileProvider{get;set;}=new NullFileProvider();public IFileProvider ContentRootFileProvider{get;set;}=new NullFileProvider();
        public string[] Files()=>Directory.Exists(root)?Directory.GetFiles(root,"*",SearchOption.AllDirectories):[];
        public void Dispose(){
            // Only the exact per-test GUID root allocated above can be removed.
            var resolved=Path.GetFullPath(root);if(Path.GetDirectoryName(resolved)!=Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar)||!Path.GetFileName(resolved).StartsWith("AcademyDesk-ResourceScope-",StringComparison.Ordinal))throw new InvalidOperationException("Unsafe test cleanup");
            if(Directory.Exists(resolved))Directory.Delete(resolved,true);
        }
    }
}
