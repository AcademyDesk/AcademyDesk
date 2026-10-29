using System.Security.Claims;
using System.Text.Json;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Infrastructure.Meetings;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Tests;

public sealed class MeetingAdmissionTests
{
    private sealed class Clock : TimeProvider { public DateTimeOffset Now=DateTimeOffset.UtcNow; public override DateTimeOffset GetUtcNow()=>Now; }
    private static async Task<(MeetingReadinessTests.Fixture F, MeetingReceiptService Receipts, MeetingOperationService Service, MeetingPreviewRequest Request, Clock Clock)> Seed(string role="Owner")
    {
        var f=await MeetingReadinessTests.Fixture.Create(role);var clock=new Clock();
        var teacher=new Teacher{AcademyId=f.Academy.Id,FirstName="Synthetic",LastName="Teacher",Email="teacher@example.invalid"};
        var batch=new Batch{AcademyId=f.Academy.Id,Name="Online",TeacherId=teacher.Id,DeliveryMode="Online"};
        var student=new Student{AcademyId=f.Academy.Id,FirstName="Synthetic",LastName="Student",Email="student@example.invalid"};
        f.Db.AddRange(teacher,batch,student,new Enrollment{AcademyId=f.Academy.Id,StudentId=student.Id,BatchId=batch.Id});await f.Db.SaveChangesAsync();
        var request=new MeetingPreviewRequest("GoogleWorkspace",batch.Id,teacher.Id,"Synthetic private title",new DateTimeOffset(DateTime.SpecifyKind(DateTime.UtcNow.Date.AddDays(2).AddHours(17),DateTimeKind.Unspecified),TimeSpan.FromMinutes(330)),"Asia/Kolkata",60,[student.Id],"Once","InviteesOnly","Disabled","Off");
        var receipts=new MeetingReceiptService(f.Db,f.Users,f.Readiness,new MeetingPreviewService(f.Db,f.Users,f.Readiness,clock),new EphemeralDataProtectionProvider(),clock);
        return(f,receipts,new(f.Db,f.Users,f.Readiness,receipts,clock),request,clock);
    }
    private static MeetingAdmissionRequest Body(MeetingReceiptState receipt)=>new(receipt.Id,receipt.Version,receipt.RequestHash,receipt.SourceVersion);

    [Fact] public async Task Review_is_not_execution_approval_and_never_enqueues()
    {
        var(f,receipts,service,request,_)=await Seed();await using var owned=f;
        var receipt=(await receipts.PrepareAsync(f.Actor,f.Academy.Id,request,"admission-draft",default)).Receipt!;
        Assert.Equal("DraftReviewRequired",(await service.RequestCreationAsync(f.Actor,f.Academy.Id,Body(receipt),"creation-key",default)).Code);
        receipt=(await receipts.ReviewAsync(f.Actor,f.Academy.Id,receipt.Id,new(0,receipt.RequestHash,receipt.SourceVersion),default)).Receipt!;
        var first=await service.RequestCreationAsync(f.Actor,f.Academy.Id,Body(receipt),"creation-key",default);
        Assert.Equal(503,first.StatusCode);Assert.Equal("MeetingExecutionUnavailable",first.Code);
        Assert.Equal(503,(await service.RequestCreationAsync(f.Actor,f.Academy.Id,Body(receipt),"creation-key",default)).StatusCode);
        Assert.Empty(await f.Db.MeetingOperations.ToListAsync());Assert.Empty(await f.Db.MeetingOutbox.ToListAsync());
        Assert.Empty(await f.Db.Notifications.ToListAsync());Assert.Equal(2,await f.Db.AuditLogs.CountAsync());
    }
    [Theory] [InlineData("version")] [InlineData("hash")] [InlineData("source")]
    public async Task Admission_requires_exact_displayed_receipt(string field)
    {
        var(f,receipts,service,request,_)=await Seed();await using var owned=f;
        var receipt=(await receipts.PrepareAsync(f.Actor,f.Academy.Id,request,"admission-draft",default)).Receipt!;
        var body=Body(receipt);body=field switch{"version"=>body with{ExpectedVersion=9},"hash"=>body with{RequestHash=new string('C',64)},_=>body with{SourceVersion=new string('D',64)}};
        Assert.Equal("StaleMeetingRequest",(await service.RequestCreationAsync(f.Actor,f.Academy.Id,body,"creation-key",default)).Code);
    }
    [Theory] [InlineData("expiry")] [InlineData("payload")] [InlineData("source")]
    public async Task Invalid_current_receipt_fails_before_unavailable_gate(string changed)
    {
        var(f,receipts,service,request,clock)=await Seed();await using var owned=f;
        var receipt=(await receipts.PrepareAsync(f.Actor,f.Academy.Id,request,"admission-draft",default)).Receipt!;
        if(changed=="expiry")clock.Now=clock.Now.AddMinutes(6);
        if(changed=="payload")(await f.Db.MeetingPreviewReceipts.SingleAsync()).ProtectedEnvelope="forged";
        if(changed=="source")(await f.Db.Students.SingleAsync()).Email="changed@example.invalid";
        await f.Db.SaveChangesAsync();
        Assert.Equal(changed=="expiry"?410:409,(await service.RequestCreationAsync(f.Actor,f.Academy.Id,Body(receipt),"creation-key",default)).StatusCode);
        Assert.Empty(await f.Db.MeetingOperations.ToListAsync());
    }
    [Theory] [InlineData(null)] [InlineData("short")] [InlineData("bad key")]
    public async Task Invalid_key_is_rejected(string? key)
    {
        var(f,_,service,_,_)=await Seed();await using var owned=f;
        Assert.Equal("InvalidIdempotencyKey",(await service.RequestCreationAsync(f.Actor,f.Academy.Id,new(Guid.NewGuid(),0,new string('A',64),new string('B',64)),key,default)).Code);
    }
    [Theory] [InlineData("Teacher")] [InlineData("Student")] [InlineData("Manager")]
    public async Task Current_role_is_authoritative_for_both_routes(string role)
    {
        var(f,_,service,_,_)=await Seed(role);await using var owned=f;
        Assert.Equal(403,(await service.GetAsync(f.Actor,f.Academy.Id,Guid.NewGuid(),default)).StatusCode);
        Assert.Equal(403,(await service.RequestCreationAsync(f.Actor,f.Academy.Id,new(Guid.Empty,0,null,null),null,default)).StatusCode);
    }
    [Theory] [InlineData("Held")] [InlineData("PendingDispatch")] [InlineData("Leased")] [InlineData("Dispatching")] [InlineData("AwaitingReview")]
    public async Task Owned_status_is_read_only_non_executable_and_private(string status)
    {
        var(f,_,service,_,_)=await Seed();await using var owned=f;
        var row=new MeetingOperation{AcademyId=f.Academy.Id,ActorUserId=f.User.Id,ReceiptId=Guid.NewGuid(),Provider="GoogleWorkspace",IdempotencyKey="private-key",RequestHash=new string('A',64),SourceVersion=new string('B',64),ProtectedEnvelope="private-title-secret-email",Status=status};
        row.CreatedAtUtc=DateTime.SpecifyKind(DateTime.UtcNow,DateTimeKind.Unspecified); // Mimic SQL datetime2 materialization.
        var leased=status is "Leased" or "Dispatching";
        f.Db.AddRange(row,new MeetingOutbox{AcademyId=f.Academy.Id,OperationId=row.Id,LeaseId=leased?Guid.NewGuid():null,LeaseExpiresAtUtc=leased?DateTime.UtcNow.AddMinutes(1):null});await f.Db.SaveChangesAsync();
        var outcome=await service.GetAsync(f.Actor,f.Academy.Id,row.Id,default);Assert.Equal(200,outcome.StatusCode);
        var result=outcome.Operation!;Assert.Equal(status,result.Status);Assert.False(result.CanExecute);Assert.False(result.CanRetry);
        Assert.Equal(DateTimeKind.Utc,result.UpdatedAtUtc.Kind);
        Assert.Equal(status=="AwaitingReview",result.RequiresReview);Assert.Equal(status is "Dispatching" or "AwaitingReview"?"Unknown":"NotConfirmed",result.ExternalOutcome);
        Assert.DoesNotContain("private",JsonSerializer.Serialize(result));Assert.Empty(await f.Db.AuditLogs.ToListAsync());
        row.ActorUserId=Guid.NewGuid();await f.Db.SaveChangesAsync();
        Assert.Equal(404,(await service.GetAsync(f.Actor,f.Academy.Id,row.Id,default)).StatusCode);
    }
    [Theory] [InlineData("missing")] [InlineData("version")] [InlineData("lease")] [InlineData("status")]
    public async Task Inconsistent_operation_state_is_not_reported_as_success(string mode)
    {
        var(f,_,service,_,_)=await Seed();await using var owned=f;
        var row=new MeetingOperation{AcademyId=f.Academy.Id,ActorUserId=f.User.Id,ReceiptId=Guid.NewGuid(),Provider="GoogleWorkspace",IdempotencyKey="private-key",RequestHash=new string('A',64),SourceVersion=new string('B',64),ProtectedEnvelope="private",Status=mode=="status"?"Created":"Held"};f.Db.Add(row);
        if(mode!="missing")f.Db.Add(new MeetingOutbox{AcademyId=f.Academy.Id,OperationId=row.Id,Version=mode=="version"?1:0,LeaseId=mode=="lease"?Guid.NewGuid():null});await f.Db.SaveChangesAsync();
        Assert.Equal(409,(await service.GetAsync(f.Actor,f.Academy.Id,row.Id,default)).StatusCode);
    }
    [Fact] public async Task Anonymous_and_foreign_routes_deny_before_lookup()
    {
        var(f,_,service,_,_)=await Seed();await using var owned=f;
        Assert.Equal(401,(await service.GetAsync(new ClaimsPrincipal(new ClaimsIdentity()),f.Academy.Id,Guid.NewGuid(),default)).StatusCode);
        Assert.Equal(403,(await service.GetAsync(f.Actor,Guid.NewGuid(),Guid.NewGuid(),default)).StatusCode);
        Assert.Equal(404,(await service.GetAsync(f.Actor,f.Academy.Id,Guid.NewGuid(),default)).StatusCode);
    }
    [Fact] public void Approval_and_authority_claims_are_not_accepted_fields()
    {
        foreach(var field in new[]{"approved","approvalId","actorUserId","academyId","role","provider","sql"})
            Assert.Throws<JsonException>(()=>JsonSerializer.Deserialize<MeetingAdmissionRequest>("{\""+field+"\":true}",new JsonSerializerOptions(JsonSerializerDefaults.Web)));
    }
}
