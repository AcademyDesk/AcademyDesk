using System.Text.Json;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Intelligence.Penta;

namespace AcademyDesk.Api.Tests;

public sealed partial class PentaQueryTests
{
    [Theory]
    [InlineData("Show student attendance", "attendance",true)]
    [InlineData("Show student attendance", "student",false)]
    [InlineData("Show class attendance", "session",false)]
    [InlineData("Show class attendance", "attendance",true)]
    [InlineData("Show teacher attendance", "attendance",false)]
    [InlineData("Show student attendance for teacher Nisha's classes", "attendance",true)]
    [InlineData("Show unmarked attendance", "attendance",false)]
    [InlineData("Mark student attendance", "attendance",false)]
    [InlineData("Show attendance percentage", "attendance",false)]
    [InlineData("How many students are absent?", "attendance",false)]
    public void Attendance_domain_does_not_become_a_directory_or_claim_other_outcomes(string text,string entity,bool expected)=>
        Assert.Equal(expected,PentaQueryIntentGuard.Compatible(text,Search(entity)));
    [Theory]
    [InlineData("Present",true)][InlineData("Absent",true)][InlineData("Late",true)][InlineData("Excused",true)][InlineData("Online",true)]
    [InlineData("Unmarked",false)][InlineData("Scheduled",false)][InlineData("Active",false)][InlineData("absent",false)]
    public void Attendance_filter_statuses_are_canonical_and_not_class_or_directory_statuses(string status,bool expected)=>
        Assert.Equal(expected,PentaQueryContract.ValidFilters("attendance",new() { ["status"]=status }));
    [Fact]
    public async Task Attendance_joins_all_tenant_relations_and_uses_class_start_not_mark_time_or_missing_roster() {
        using var f=new Fixture();f.Academy.TimeZone="Asia/Kolkata";var foreign=Guid.NewGuid();
        var teacher=new Teacher { AcademyId=f.Academy.Id,FirstName="Nisha",LastName="Synthetic",Email="private-contact" };
        var batch=new Batch { AcademyId=f.Academy.Id,Name="Synthetic Class",CourseId=Guid.NewGuid() };
        var otherBatch=new Batch { AcademyId=foreign,Name="Foreign Class",CourseId=Guid.NewGuid() };
        var student=new Student { AcademyId=f.Academy.Id,FirstName="Mira",LastName="Synthetic",StudentNumber="QA-ST-1" };
        var otherStudent=new Student { AcademyId=foreign,FirstName="Foreign",LastName="Student" };
        var unmarked=new Student { AcademyId=f.Academy.Id,FirstName="Unmarked",LastName="Roster" };
        f.Db.Teachers.Add(teacher);f.Db.Batches.AddRange(batch,otherBatch);f.Db.Students.AddRange(student,otherStudent,unmarked);
        f.Db.Enrollments.Add(new Enrollment {AcademyId=f.Academy.Id,StudentId=unmarked.Id,BatchId=batch.Id,Status="Active"});
        var start=new DateTime(2026,11,23,18,30,0,DateTimeKind.Utc);
        ClassSession Session(Guid tenant,Guid batchId,DateTime at)=>new() { AcademyId=tenant,BatchId=batchId,TeacherId=teacher.Id,StartUtc=at,EndUtc=at.AddHours(1) };
        var session=Session(f.Academy.Id,batch.Id,start);var otherSession=Session(foreign,otherBatch.Id,start);
        var badBatchSession=Session(f.Academy.Id,otherBatch.Id,start);var nextDay=Session(f.Academy.Id,batch.Id,start.AddDays(1));
        f.Db.ClassSessions.AddRange(session,otherSession,badBatchSession,nextDay);
        AttendanceRecord Mark(Guid tenant,Guid classId,Guid pupil,string status)=>new() { AcademyId=tenant,ClassSessionId=classId,StudentId=pupil,Status=status,
            MarkedAtUtc=start.AddDays(10),Notes="private-medical-note" };
        var own=Mark(f.Academy.Id,session.Id,student.Id,"absent");
        f.Db.AttendanceRecords.AddRange(own,Mark(f.Academy.Id,session.Id,otherStudent.Id,"Absent"),
            Mark(foreign,otherSession.Id,otherStudent.Id,"Absent"),Mark(f.Academy.Id,otherSession.Id,student.Id,"Absent"),
            Mark(f.Academy.Id,badBatchSession.Id,student.Id,"Absent"),Mark(f.Academy.Id,nextDay.Id,student.Id,"Present"));
        await f.Db.SaveChangesAsync();var reader=new PentaQueryReader(f.Db,new CalendarClock());
        var value=await reader.ExecuteAsync(f.Academy.Id,Search("attendance",new() { ["from"]="Today",["status"]="Absent",["teacher"]="Nisha" }),PentaQueryContext.Empty,All,default);
        var row=Assert.Single(value.Result!.Records);Assert.Equal(own.Id,row.Id);Assert.Equal("Mira Synthetic",row.Label);Assert.Equal("Absent",row.Attendance!.Status);
        Assert.Equal(start.AddDays(10),row.Attendance.MarkedAtUtc);Assert.Equal(start,row.Schedule!.StartUtc);
        Assert.Equal(PentaQueryContract.AttendanceScope,value.Result.AttendanceScope);Assert.Equal("2026-11-24",value.Context.Filters["from"]);
        Assert.DoesNotContain("private",JsonSerializer.Serialize(value));Assert.DoesNotContain("Unmarked Roster",JsonSerializer.Serialize(value));
        var implicitDay=await reader.ExecuteAsync(f.Academy.Id,Search("attendance"),PentaQueryContext.Empty,All,default);
        Assert.Equal("2026-11-24",implicitDay.Context.Filters["through"]);Assert.Single(implicitDay.Result!.Records);
        var selected=await reader.ExecuteAsync(f.Academy.Id,new(PentaQueryContract.Version,"open","attendance",[],1),value.Context,All,default);
        Assert.Equal(own.Id,Assert.Single(selected.Result!.Records).Id);
        var wrong=await reader.ExecuteAsync(f.Academy.Id,new(PentaQueryContract.Version,"open","attendance",[],1),new("student",[],[student.Id]),All,default);
        Assert.Equal("CLARIFICATION_REQUIRED",wrong.Kind);
        var denied=await reader.ExecuteAsync(f.Academy.Id,Search("attendance"),PentaQueryContext.Empty,All.Where(x=>!x.Contains("attendance")).ToHashSet(),default);
        Assert.Equal("UNSUPPORTED",denied.Kind);
        otherStudent.IsActive=false;await f.Db.SaveChangesAsync();Assert.True(await reader.CurrentAsync(f.Academy.Id,value.Context,default));
        student.AcademyId=foreign;await f.Db.SaveChangesAsync();Assert.False(await reader.CurrentAsync(f.Academy.Id,value.Context,default));
    }
    [Fact]
    public void Attendance_receipts_require_typed_mark_scope_and_never_add_notes_or_finance_to_old_views() {
        var id=Guid.NewGuid();var conversation=Guid.NewGuid();var request=Guid.NewGuid();
        var time=new DateTime(2026,11,24,1,0,0,DateTimeKind.Utc);var scope=new PentaQueryCalendarScope("Asia/Kolkata","2026-11-24","2026-11-24");
        var row=new PentaQueryRecord(id,"Synthetic Student",null,false,[],"/attendance",Schedule:new(time,time.AddHours(1),"Online","Completed",null),Attendance:new("Late",time.AddDays(2),"Synthetic Class"));
        var receipt=new PentaQueryTurnReceipt(conversation,request,1,"RESULT","Stored marks",new("attendance",new() { ["from"]=scope.From,["through"]=scope.Through },[id]),
            new("attendance",[row],false,DateTime.UtcNow,CalendarScope:scope,AttendanceScope:PentaQueryContract.AttendanceScope),"Show attendance");
        Assert.True(PentaQueryContract.ValidReceipt(receipt,conversation,request,1));
        Assert.False(PentaQueryContract.ValidReceipt(receipt with {Result=receipt.Result! with {AttendanceScope=null}},conversation,request,1));
        Assert.False(PentaQueryContract.ValidReceipt(receipt with {Result=receipt.Result! with {Records=[row with {Attendance=new("Unmarked",time,"Class")}] }},conversation,request,1));
        Assert.False(PentaQueryContract.ValidReceipt(receipt with {Result=receipt.Result! with {Records=[row with {Balances=[new("INR","1.00")]}]}},conversation,request,1));
        Assert.False(PentaQueryContract.ValidFilters("session",new() { ["status"]="Absent" }));
        Assert.False(PentaQueryContract.SameReferences("attendance","student"));
    }
}
