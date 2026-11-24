using System.Text.Json;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Intelligence.Penta;
using Xunit;

namespace AcademyDesk.Api.Tests;

public sealed partial class PentaQueryTests
{
    private sealed class CalendarClock : TimeProvider {
        public override DateTimeOffset GetUtcNow() => DateTimeOffset.Parse("2026-11-23T20:00:00Z");
    }
    [Theory]
    [InlineData("Today", "2026-11-24", "2026-11-24")]
    [InlineData("Tomorrow", "2026-11-25", "2026-11-25")]
    [InlineData("Yesterday", "2026-11-23", "2026-11-23")]
    [InlineData("Next7Days", "2026-11-24", "2026-11-30")]
    public void Calendar_relative_days_use_academy_zone_not_UTC_or_model(string expression, string from, string through) {
        Assert.True(PentaQueryCalendar.Window("Asia/Kolkata", new Dictionary<string,string> { ["from"]=expression },
            new CalendarClock().GetUtcNow(), out var scope, out var start, out var end));
        Assert.Equal(from,scope!.From); Assert.Equal(through,scope.Through);
        Assert.Equal(DateTime.Parse(from).AddHours(-5.5),DateTime.SpecifyKind(start,DateTimeKind.Unspecified));
        Assert.Equal(DateTime.Parse(through).AddDays(1).AddHours(-5.5),DateTime.SpecifyKind(end,DateTimeKind.Unspecified));
        Assert.Equal(DateTimeKind.Utc,start.Kind);
    }
    [Theory]
    [InlineData("2026-11-01", "2026-11-30", true)]
    [InlineData("2026-11-01", "2026-12-01", true)]
    [InlineData("2026-11-01", "2026-12-02", false)]
    [InlineData("2026-11-24", "2026-11-23", false)]
    [InlineData("Today", "2026-11-24", false)]
    [InlineData("2026-02-30", "2026-03-01", false)]
    [InlineData("0000-01-01", "0000-01-01", false)]
    public void Calendar_range_is_real_bounded_and_unambiguous(string from,string through,bool expected) =>
        Assert.Equal(expected,PentaQueryContract.ValidFilters("session",new() { ["from"]=from,["through"]=through }));
    [Fact]
    public void Calendar_DST_is_actual_local_day_and_ambiguous_midnight_fails_closed() {
        Assert.True(PentaQueryCalendar.Window("America/New_York",new Dictionary<string,string> { ["from"]="2026-03-08" },
            DateTimeOffset.UnixEpoch,out _,out var from,out var to));
        Assert.Equal(23,(to-from).TotalHours);
        Assert.False(PentaQueryCalendar.Window("America/Havana",new Dictionary<string,string> { ["from"]="2026-03-08" },
            DateTimeOffset.UnixEpoch,out _,out _,out _));
        Assert.False(PentaQueryCalendar.Window("missing-zone",new Dictionary<string,string>(),DateTimeOffset.UnixEpoch,out _,out _,out _));
    }
    [Theory]
    [InlineData("Show teacher Nisha's calendar", "teacher", false)]
    [InlineData("Show teacher Nisha's calendar", "session", true)]
    [InlineData("Show batch classes tomorrow", "batch", false)]
    [InlineData("Show batch classes tomorrow", "session", true)]
    [InlineData("Show class attendance", "session", false)]
    [InlineData("Create a class", "session", false)]
    [InlineData("Cancel the class", "session", false)]
    [InlineData("Schedule a class", "session", false)]
    [InlineData("Change teacher", "session", false)]
    [InlineData("Show student Mira's classes", "session", false)]
    public void Calendar_requests_cannot_be_substituted_with_directory_or_attendance(string text,string entity,bool expected) =>
        Assert.Equal(expected,PentaQueryIntentGuard.Compatible(text,Search(entity)));
    [Fact]
    public async Task Calendar_reader_pins_local_dates_and_excludes_foreign_relationships_and_boundary_starts() {
        using var f=new Fixture(); f.Academy.TimeZone="Asia/Kolkata";
        var teacher=new Teacher { AcademyId=f.Academy.Id,FirstName="Nisha",LastName="Synthetic",Email="private@example.invalid" };
        var foreign=new Teacher { AcademyId=Guid.NewGuid(),FirstName="Private",LastName="Foreign" };
        var batch=new Batch { AcademyId=f.Academy.Id,Name="Synthetic calendar",CourseId=Guid.NewGuid() };
        var foreignBatch=new Batch { AcademyId=foreign.AcademyId,Name="Foreign calendar",CourseId=Guid.NewGuid() };
        f.Db.Teachers.AddRange(teacher,foreign); f.Db.Batches.AddRange(batch,foreignBatch);
        var dayStart=DateTime.Parse("2026-11-23T18:30:00Z").ToUniversalTime();
        ClassSession Session(Guid tenant,Guid batchId,Guid? teacherId,DateTime start) => new() {
            AcademyId=tenant,BatchId=batchId,TeacherId=teacherId,StartUtc=start,EndUtc=start.AddHours(1),DeliveryMode="Hybrid",RoomName="private-room" };
        var first=Session(f.Academy.Id,batch.Id,teacher.Id,dayStart);
        f.Db.ClassSessions.AddRange(first,Session(f.Academy.Id,batch.Id,teacher.Id,dayStart.AddDays(1)),
            Session(f.Academy.Id,batch.Id,teacher.Id,dayStart.AddMinutes(-1)),Session(foreign.AcademyId,foreignBatch.Id,foreign.Id,dayStart),
            Session(f.Academy.Id,foreignBatch.Id,teacher.Id,dayStart),Session(f.Academy.Id,batch.Id,foreign.Id,dayStart));
        await f.Db.SaveChangesAsync(); var reader=new PentaQueryReader(f.Db,new CalendarClock());
        var decision=await reader.ExecuteAsync(f.Academy.Id,Search("session",new() { ["from"]="Today",["teacher"]="Nisha" }),PentaQueryContext.Empty,All,default);
        var row=Assert.Single(decision.Result!.Records); Assert.Equal(first.Id,row.Id); Assert.Equal("Nisha Synthetic",row.Schedule!.Teacher);
        Assert.Equal("2026-11-24",decision.Context.Filters["from"]); Assert.Equal("2026-11-24",decision.Context.Filters["through"]);
        Assert.Null(row.Balances); Assert.DoesNotContain("private",JsonSerializer.Serialize(decision));
        var opened=await reader.ExecuteAsync(f.Academy.Id,new(PentaQueryContract.Version,"open","session",[],1),decision.Context,All,default);
        Assert.Equal(first.Id,Assert.Single(opened.Result!.Records).Id);
        var denied=await reader.ExecuteAsync(f.Academy.Id,new(PentaQueryContract.Version,"open","session",[],1),new("teacher",[],[teacher.Id]),All,default);
        Assert.Equal("CLARIFICATION_REQUIRED",denied.Kind); Assert.Null(denied.Result);
        var noTool=await reader.ExecuteAsync(f.Academy.Id,Search("session"),PentaQueryContext.Empty,new HashSet<string>(),default);
        Assert.Equal("UNSUPPORTED",noTool.Kind);
    }
    [Fact]
    public void Calendar_receipt_requires_its_exact_scope_and_rejects_schedule_fields_in_directories() {
        var id=Guid.NewGuid();var conversation=Guid.NewGuid();var request=Guid.NewGuid();
        var row=new PentaQueryRecord(id,"Synthetic",null,true,[],"/schedule",Schedule:new(DateTime.Parse("2026-11-24T02:00:00Z").ToUniversalTime(),
            DateTime.Parse("2026-11-24T03:00:00Z").ToUniversalTime(),"Online","Scheduled",null));
        var scope=new PentaQueryCalendarScope("Asia/Kolkata","2026-11-24","2026-11-24");
        var receipt=new PentaQueryTurnReceipt(conversation,request,1,"RESULT","Verified",new("session",new() { ["from"]=scope.From,["through"]=scope.Through },[id]),
            new("session",[row],false,DateTime.UtcNow,CalendarScope:scope),"Show classes today");
        Assert.True(PentaQueryContract.ValidReceipt(receipt,conversation,request,1));
        Assert.False(PentaQueryContract.ValidReceipt(receipt with { Result=receipt.Result! with { CalendarScope=null } },conversation,request,1));
        Assert.False(PentaQueryContract.ValidReceipt(receipt with { Result=receipt.Result! with { CalendarScope=scope with { From="2026-11-25" } } },conversation,request,1));
        Assert.False(PentaQueryContract.ValidReceipt(receipt with { Result=receipt.Result! with { Records=[row with { Schedule=row.Schedule! with { Status="Active" } }] } },conversation,request,1));
    }
}
