using System.Text.Json;
using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Tests;

public sealed class ComplianceIdentityTests
{
    private static readonly JsonSerializerOptions JsonOptions=new(JsonSerializerDefaults.Web);
    private static JsonElement Json(object? value)=>JsonSerializer.SerializeToElement(value,JsonOptions);
    public static IEnumerable<object[]> InvalidPeople(){
        foreach(var document in new[]{false,true})foreach(var mode in new[]{"None","BothLocal","BothForeign","StudentMissing","StudentForeign","StudentEmpty","StudentSwapped","GuardianMissing","GuardianForeign","GuardianEmpty","GuardianSwapped","StudentExtraEmpty","GuardianExtraEmpty"})
            yield return [document,mode];
    }
    [Theory,MemberData(nameof(InvalidPeople))]
    public async Task Invalid_or_contradictory_person_references_are_rejected_without_writes(bool document,string mode){
        var f=await Fixture();await using var db=f.Db;Guid? student=null,guardian=null;
        switch(mode){
            case "BothLocal":student=f.Student.Id;guardian=f.Guardian.Id;break;case "BothForeign":student=f.ForeignStudent.Id;guardian=f.ForeignGuardian.Id;break;
            case "StudentMissing":student=Guid.NewGuid();break;case "StudentForeign":student=f.ForeignStudent.Id;break;case "StudentEmpty":student=Guid.Empty;break;case "StudentSwapped":student=f.Guardian.Id;break;
            case "GuardianMissing":guardian=Guid.NewGuid();break;case "GuardianForeign":guardian=f.ForeignGuardian.Id;break;case "GuardianEmpty":guardian=Guid.Empty;break;case "GuardianSwapped":guardian=f.Student.Id;break;
            case "StudentExtraEmpty":student=f.Student.Id;guardian=Guid.Empty;break;case "GuardianExtraEmpty":student=Guid.Empty;guardian=f.Guardian.Id;break;
        }
        var before=await Snapshot(db);var controller=new ComplianceController(db);
        var result=document?await controller.AddDocument(f.Academy,new(student,guardian,"ID","file.pdf",null,null,null),default):await controller.AddConsent(f.Academy,new(student,guardian,"Media",true,null),default);
        Assert.IsType<BadRequestObjectResult>(result);Assert.True(JsonElement.DeepEquals(before,await Snapshot(db)));
        Assert.DoesNotContain(db.ChangeTracker.Entries(),x=>x.State==EntityState.Added);
    }
    public static IEnumerable<object[]> ValidPeople(){
        foreach(var document in new[]{false,true})foreach(var student in new[]{false,true})foreach(var active in new[]{false,true})foreach(var optional in new[]{"Null","Empty","Filled"})
            foreach(var granted in document?new[]{true}:new[]{false,true})yield return [document,student,active,optional,granted];
    }
    [Theory,MemberData(nameof(ValidPeople))]
    public async Task Valid_single_person_preserves_optional_fields_inactive_eligibility_and_server_lifecycle(bool document,bool student,bool active,string optional,bool granted){
        var f=await Fixture();await using var db=f.Db;f.Student.IsActive=active;f.Guardian.IsActive=active;await db.SaveChangesAsync();
        Guid? studentId=student?f.Student.Id:null,guardianId=student?null:f.Guardian.Id;string? reference=optional=="Null"?null:optional=="Empty"?"":"  Synthetic reference  ";
        var expiry=optional=="Filled"?(DateOnly?)new DateOnly(1999,2,28):null;var before=await Snapshot(db);var start=DateTime.UtcNow;var controller=new ComplianceController(db);Guid id;
        if(document){
            var row=Assert.IsType<PersonDocument>(Assert.IsType<OkObjectResult>(await controller.AddDocument(f.Academy,new(studentId,guardianId,"  ID  ","  file.pdf  ",reference,expiry,optional=="Filled"?"  StaffRestricted  ":optional=="Empty"?" ":null),default)).Value);id=row.Id;
            Assert.Equal("ID",row.DocumentType);Assert.Equal("file.pdf",row.FileName);Assert.Equal(reference?.Trim(),row.SecureReference);Assert.Equal(expiry,row.ExpiryDate);Assert.Equal(optional=="Filled"?"StaffRestricted":"AdminOnly",row.Visibility);
            Assert.Equal("PendingReview",row.Status);Assert.Null(row.ReviewedDate);Assert.Equal(studentId,row.StudentId);Assert.Equal(guardianId,row.GuardianId);
            Assert.InRange(row.CreatedAtUtc,start,DateTime.UtcNow);Assert.Null(row.UpdatedAtUtc);Assert.Equal(f.Academy,row.AcademyId);Assert.NotEqual(Guid.Empty,id);
            Assert.True(JsonElement.DeepEquals(Json(row),Json(await db.PersonDocuments.AsNoTracking().SingleAsync(x=>x.Id==id))));
        }else{
            var row=Assert.IsType<ConsentRecord>(Assert.IsType<OkObjectResult>(await controller.AddConsent(f.Academy,new(studentId,guardianId,"  Media  ",granted,reference),default)).Value);id=row.Id;
            Assert.Equal("Media",row.ConsentType);Assert.Equal(reference?.Trim(),row.EvidenceReference);Assert.Equal(granted,row.Granted);Assert.Equal(studentId,row.StudentId);Assert.Equal(guardianId,row.GuardianId);
            Assert.InRange(row.RecordedAtUtc,start,DateTime.UtcNow);if(granted)Assert.Null(row.WithdrawnAtUtc);else Assert.InRange(row.WithdrawnAtUtc!.Value,start,DateTime.UtcNow);
            Assert.InRange(row.CreatedAtUtc,start,DateTime.UtcNow);Assert.Null(row.UpdatedAtUtc);Assert.Equal(f.Academy,row.AcademyId);Assert.NotEqual(Guid.Empty,id);
            Assert.True(JsonElement.DeepEquals(Json(row),Json(await db.ConsentRecords.AsNoTracking().SingleAsync(x=>x.Id==id))));
        }
        var after=await Snapshot(db);Preserve(before,after,document?"documents":"consents",id,true);
    }
    [Theory] [InlineData(null,"file.pdf")] [InlineData(" ","file.pdf")] [InlineData("ID"," ")]
    public async Task Document_required_text_guards_remain_no_write(string? type,string filename){
        var f=await Fixture();await using var db=f.Db;var before=await Snapshot(db);
        Assert.IsType<BadRequestObjectResult>(await new ComplianceController(db).AddDocument(f.Academy,new(f.Student.Id,null,type!,filename,null,null,null),default));Assert.True(JsonElement.DeepEquals(before,await Snapshot(db)));
    }
    [Theory] [InlineData(null)] [InlineData(" ")]
    public async Task Consent_required_type_guard_remains_no_write(string? type){
        var f=await Fixture();await using var db=f.Db;var before=await Snapshot(db);
        Assert.IsType<BadRequestObjectResult>(await new ComplianceController(db).AddConsent(f.Academy,new(f.Student.Id,null,type!,true,null),default));Assert.True(JsonElement.DeepEquals(before,await Snapshot(db)));
    }
    [Fact]
    public async Task Review_is_scoped_and_preserves_all_unrelated_fields_with_canonical_status_and_optional_date(){
        var f=await Fixture();await using var db=f.Db;var controller=new ComplianceController(db);var before=await Snapshot(db);
        Assert.IsType<NotFoundResult>(await controller.ReviewDocument(f.Academy,f.ForeignDocument.Id,new("Approved",null),default));
        Assert.IsType<NotFoundResult>(await controller.ReviewDocument(f.Academy,Guid.NewGuid(),new("Approved",null),default));
        Assert.IsType<BadRequestObjectResult>(await controller.ReviewDocument(f.Academy,f.Document.Id,new("Unsupported",null),default));
        Assert.True(JsonElement.DeepEquals(before,await Snapshot(db)));
        foreach(var status in new[]{"PendingReview","Approved","Rejected","Expired"}){
            before=await Snapshot(db);var date=status=="Approved"?(DateOnly?)new DateOnly(2020,2,29):null;var today=DateOnly.FromDateTime(DateTime.UtcNow);
            var row=Assert.IsType<PersonDocument>(Assert.IsType<OkObjectResult>(await controller.ReviewDocument(f.Academy,f.Document.Id,new(status.ToLowerInvariant(),date),default)).Value);
            Assert.Equal(status,row.Status);Assert.InRange(row.ReviewedDate!.Value,date??today,date??DateOnly.FromDateTime(DateTime.UtcNow));
            var after=await Snapshot(db);Preserve(before,after,"documents",row.Id,false);
            Fields(before.GetProperty("documents").EnumerateArray().Single(x=>x.GetProperty("id").GetGuid()==row.Id),Json(row),"status","reviewedDate");
        }
    }
    [Theory] [InlineData("Null")] [InlineData("Past")] [InlineData("Future")]
    public async Task Review_task_retains_document_and_optional_expiry_behavior(string mode){
        var f=await Fixture();await using var db=f.Db;f.Document.ExpiryDate=mode=="Null"?null:DateOnly.FromDateTime(DateTime.UtcNow).AddDays(mode=="Past"?-10:60);await db.SaveChangesAsync();var before=await Snapshot(db);var controller=new ComplianceController(db);
        Assert.IsType<NotFoundResult>(await controller.CreateReviewTask(f.Academy,f.ForeignDocument.Id,default));
        Assert.IsType<NotFoundResult>(await controller.CreateReviewTask(f.Academy,Guid.NewGuid(),default));Assert.True(JsonElement.DeepEquals(before,await Snapshot(db)));
        Assert.IsType<OkResult>(await controller.CreateReviewTask(f.Academy,f.Document.Id,default));var after=await Snapshot(db);var task=await db.AdminWorkItems.SingleAsync();
        Assert.Equal(f.Academy,task.AcademyId);Assert.Equal("Compliance",task.Type);Assert.Equal("Review "+f.Document.DocumentType,task.Title);Assert.Equal($"Document {f.Document.FileName} requires review or renewal.",task.Description);
        Assert.Equal(mode=="Past"?"High":"Normal",task.Priority);Assert.Equal("PersonDocument",task.EntityType);Assert.Equal(f.Document.Id,task.EntityId);Assert.Equal(f.Document.ExpiryDate?.ToDateTime(TimeOnly.MinValue),task.DueAtUtc);
        foreach(var key in new[]{"students","guardians","documents","consents"})Assert.True(JsonElement.DeepEquals(before.GetProperty(key),after.GetProperty(key)));
    }
    [Theory] [InlineData(true)] [InlineData(false)]
    public async Task Withdraw_is_scoped_and_preserves_recorded_history(bool granted){
        var f=await Fixture();await using var db=f.Db;f.Consent.Granted=granted;f.Consent.WithdrawnAtUtc=granted?null:new DateTime(2000,1,1,0,0,0,DateTimeKind.Utc);await db.SaveChangesAsync();var before=await Snapshot(db);var controller=new ComplianceController(db);var start=DateTime.UtcNow;
        Assert.IsType<NotFoundResult>(await controller.WithdrawConsent(f.Academy,f.ForeignConsent.Id,default));
        Assert.IsType<NotFoundResult>(await controller.WithdrawConsent(f.Academy,Guid.NewGuid(),default));Assert.True(JsonElement.DeepEquals(before,await Snapshot(db)));
        var row=Assert.IsType<ConsentRecord>(Assert.IsType<OkObjectResult>(await controller.WithdrawConsent(f.Academy,f.Consent.Id,default)).Value);Assert.False(row.Granted);Assert.InRange(row.WithdrawnAtUtc!.Value,start,DateTime.UtcNow);
        var after=await Snapshot(db);Preserve(before,after,"consents",row.Id,false);Fields(before.GetProperty("consents").EnumerateArray().Single(x=>x.GetProperty("id").GetGuid()==row.Id),Json(row),"granted","withdrawnAtUtc");
    }
    [Theory] [InlineData(true)] [InlineData(false)]
    public async Task Repeated_creation_preserves_prior_evidence_instead_of_upserting(bool document){
        var f=await Fixture();await using var db=f.Db;var before=await Snapshot(db);var controller=new ComplianceController(db);var ids=new List<Guid>();
        for(var i=0;i<2;i++){var result=document?await controller.AddDocument(f.Academy,new(f.Student.Id,null,"ID","repeat.pdf",null,null,null),default):await controller.AddConsent(f.Academy,new(f.Student.Id,null,"Media",true,null),default);var value=Assert.IsType<OkObjectResult>(result).Value;ids.Add(Json(value).GetProperty("id").GetGuid());}
        Assert.NotEqual(ids[0],ids[1]);var after=await Snapshot(db);
        foreach(var key in new[]{"documents","consents"}){var old=before.GetProperty(key).EnumerateArray().ToArray();var current=after.GetProperty(key).EnumerateArray().ToArray();Assert.Equal(old.Length+((document?key=="documents":key=="consents")?2:0),current.Length);foreach(var row in old)Assert.Contains(current,x=>JsonElement.DeepEquals(row,x));}
    }
    [Fact]
    public async Task Lists_return_complete_scoped_records_in_existing_order_without_writes(){
        var f=await Fixture();await using var db=f.Db;var before=await Snapshot(db);var controller=new ComplianceController(db);
        var docs=Assert.IsType<List<PersonDocument>>(Assert.IsType<OkObjectResult>(await controller.Documents(f.Academy,default)).Value);var consents=Assert.IsType<List<ConsentRecord>>(Assert.IsType<OkObjectResult>(await controller.Consents(f.Academy,default)).Value);
        Assert.True(JsonElement.DeepEquals(Json(new[]{f.Document}),Json(docs)));Assert.True(JsonElement.DeepEquals(Json(new[]{f.Consent}),Json(consents)));Assert.True(JsonElement.DeepEquals(before,await Snapshot(db)));
    }
    private static void Fields(JsonElement old,JsonElement current,params string[] allowed){foreach(var field in old.EnumerateObject())if(!allowed.Contains(field.Name))Assert.True(JsonElement.DeepEquals(field.Value,current.GetProperty(field.Name)),field.Name);}
    private static void Preserve(JsonElement before,JsonElement after,string changed,Guid id,bool added){
        foreach(var key in new[]{"students","guardians","tasks"})Assert.True(JsonElement.DeepEquals(before.GetProperty(key),after.GetProperty(key)));
        foreach(var key in new[]{"documents","consents"}){var old=before.GetProperty(key).EnumerateArray().ToArray();var current=after.GetProperty(key).EnumerateArray().ToArray();Assert.Equal(old.Length+(added&&key==changed?1:0),current.Length);foreach(var row in old.Where(x=>key!=changed||x.GetProperty("id").GetGuid()!=id))Assert.Contains(current,x=>JsonElement.DeepEquals(row,x));}
    }
    private static async Task<JsonElement> Snapshot(AcademyDeskDbContext db)=>Json(new{students=await db.Students.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),guardians=await db.Guardians.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),documents=await db.PersonDocuments.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),consents=await db.ConsentRecords.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),tasks=await db.AdminWorkItems.AsNoTracking().OrderBy(x=>x.Id).ToListAsync()});
    private sealed record Seed(AcademyDeskDbContext Db,Guid Academy,Student Student,Guardian Guardian,Student ForeignStudent,Guardian ForeignGuardian,PersonDocument Document,PersonDocument ForeignDocument,ConsentRecord Consent,ConsentRecord ForeignConsent);
    private static async Task<Seed> Fixture(){
        var db=new AcademyDeskDbContext(new DbContextOptionsBuilder<AcademyDeskDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);var academy=Guid.NewGuid();var foreign=Guid.NewGuid();
        var student=new Student{AcademyId=academy,FirstName="Synthetic",LastName="Student"};var guardian=new Guardian{AcademyId=academy,FirstName="Synthetic",LastName="Guardian"};
        var foreignStudent=new Student{AcademyId=foreign,FirstName="Foreign",LastName="Student"};var foreignGuardian=new Guardian{AcademyId=foreign,FirstName="Foreign",LastName="Guardian"};
        var doc=new PersonDocument{AcademyId=academy,StudentId=student.Id,DocumentType="Prior ID",FileName="prior.pdf",SecureReference="Retain document evidence",Status="Approved",ReviewedDate=new DateOnly(2000,1,1),ExpiryDate=new DateOnly(2000,2,1)};
        var foreignDoc=new PersonDocument{AcademyId=foreign,GuardianId=foreignGuardian.Id,DocumentType="Foreign ID",FileName="foreign.pdf"};
        var consent=new ConsentRecord{AcademyId=academy,GuardianId=guardian.Id,ConsentType="Prior consent",Granted=true,EvidenceReference="Retain consent evidence",RecordedAtUtc=new DateTime(2000,1,1,0,0,0,DateTimeKind.Utc)};
        var foreignConsent=new ConsentRecord{AcademyId=foreign,StudentId=foreignStudent.Id,ConsentType="Foreign consent",Granted=true};
        db.AddRange(student,guardian,foreignStudent,foreignGuardian,doc,foreignDoc,consent,foreignConsent);await db.SaveChangesAsync();return new(db,academy,student,guardian,foreignStudent,foreignGuardian,doc,foreignDoc,consent,foreignConsent);
    }
}
