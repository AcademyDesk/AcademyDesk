using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

internal static partial class SqlHarnessEntryPoint
{
    private static async Task VerifyAttendanceNotesAsync(QaApiFactory factory, HttpClient client)
    {
        // Consume payloads emitted by actual TSX handlers, not independently reconstructed UI requests.
        var payloads=File.ReadLines("QA/EVIDENCE/logs/phase-2b-attendance-notes-ui.log")
            .Where(x=>x.StartsWith("ATTENDANCE PAYLOAD ",StringComparison.Ordinal))
            .Select(x=>JsonDocument.Parse(x["ATTENDANCE PAYLOAD ".Length..]).RootElement.Clone()).ToArray();
        if(payloads.Length!=10||payloads.Select(x=>x.GetProperty("label").GetString()).Distinct().Count()!=10)
            throw new InvalidOperationException("Run the pinned attendance TSX tests first: expected ten unique payloads.");
        Guid academy,foreign,student,batch,one,two;var count=0;
        using(var scope=factory.Services.CreateScope()) {
            var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            academy=await db.Academies.Where(x=>x.Name=="Synthetic Academy A").Select(x=>x.Id).SingleAsync();
            foreign=await db.Academies.Where(x=>x.Name=="Synthetic Academy B").Select(x=>x.Id).SingleAsync();
            student=await db.Students.Where(x=>x.AcademyId==academy).Select(x=>x.Id).SingleAsync();
            batch=await db.Batches.Where(x=>x.AcademyId==academy).Select(x=>x.Id).SingleAsync();
            var first=new ClassSession {AcademyId=academy,BatchId=batch,StartUtc=DateTime.UtcNow,EndUtc=DateTime.UtcNow.AddHours(1)};
            var second=new ClassSession {AcademyId=academy,BatchId=batch,StartUtc=DateTime.UtcNow.AddDays(1),EndUtc=DateTime.UtcNow.AddDays(1).AddHours(1)};
            one=first.Id;two=second.Id;db.AddRange(first,second,new Enrollment {AcademyId=academy,BatchId=batch,StudentId=student});
            db.Add(new AttendanceRecord {AcademyId=academy,ClassSessionId=two,StudentId=student,Status="Late",Notes="Session two note"});await db.SaveChangesAsync();
        }
        var token=await LoginAsync(client,"qa-admin-a@example.invalid","Synthetic!39Ab");client.DefaultRequestHeaders.Authorization=null;
        string Route(Guid session,Guid? tenant=null)=>$"/api/academies/{tenant??academy}/sessions/{session}/attendance";
        async Task<string> Snapshot() {
            using var scope=factory.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            return JsonSerializer.Serialize(await db.AttendanceRecords.AsNoTracking().OrderBy(x=>x.Id).ToListAsync());
        }
        async Task<HttpResponseMessage> Send(Guid session,object payload,string? auth,Guid? tenant=null) {
            await Task.Delay(550);using var request=new HttpRequestMessage(HttpMethod.Post,Route(session,tenant));
            if(auth is not null)request.Headers.Authorization=new AuthenticationHeaderValue("Bearer",auth);
            request.Content=JsonContent.Create(payload);return await client.SendAsync(request);
        }
        foreach(var fixture in payloads) {
            var label=fixture.GetProperty("label").GetString();var body=fixture.GetProperty("payload");
            var notes=body.GetProperty("notes").GetString();var status=body.GetProperty("status").GetString();var expected=fixture.GetProperty("expected").GetString();
            if(body.GetProperty("studentId").GetString()!="s")throw new InvalidOperationException("Unexpected TSX synthetic student ID");
            using(var scope=factory.Services.CreateScope()) {
                var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
                db.RemoveRange(await db.AttendanceRecords.Where(x=>x.AcademyId==academy&&x.ClassSessionId==one).ToListAsync());
                if(!fixture.GetProperty("newRecord").GetBoolean())db.Add(new AttendanceRecord {AcademyId=academy,ClassSessionId=one,StudentId=student,Status="Present",Notes=fixture.GetProperty("before").GetString()});
                await db.SaveChangesAsync();
            }
            using var saved=await Send(one,new {studentId=student,status,notes},token);RequireFinanceStatus(saved,HttpStatusCode.OK,label??"payload");
            using var json=JsonDocument.Parse(await saved.Content.ReadAsStringAsync());var resultId=json.RootElement.GetProperty("id").GetGuid();
            using(var scope=factory.Services.CreateScope()) {
                var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
                var row=await db.AttendanceRecords.AsNoTracking().SingleAsync(x=>x.AcademyId==academy&&x.ClassSessionId==one&&x.StudentId==student);
                if(row.Id!=resultId||row.Status!=status||row.Notes!=expected||json.RootElement.GetProperty("notes").GetString()!=row.Notes||json.RootElement.GetProperty("status").GetString()!=row.Status)
                    throw new InvalidOperationException("TSX payload/HTTP/fresh SQL mismatch: "+label);
                var other=await db.AttendanceRecords.AsNoTracking().SingleAsync(x=>x.AcademyId==academy&&x.ClassSessionId==two);
                if(other.Notes!="Session two note"||other.Status!="Late")throw new InvalidOperationException("Other session changed: "+label);
            }
            using var read=new HttpRequestMessage(HttpMethod.Get,Route(one));read.Headers.Authorization=new AuthenticationHeaderValue("Bearer",token);
            using var listed=await client.SendAsync(read);RequireFinanceStatus(listed,HttpStatusCode.OK,"readback-"+label);
            using var list=JsonDocument.Parse(await listed.Content.ReadAsStringAsync());
            if(list.RootElement.GetArrayLength()!=1||list.RootElement[0].GetProperty("notes").GetString()!=expected||list.RootElement[0].GetProperty("id").GetGuid()!=resultId)
                throw new InvalidOperationException("Fresh GET differs from SQL: "+label);
            count++;Console.WriteLine($"ATTENDANCENOTES CASE {label} PASS.");
        }
        var original=await Snapshot();
        foreach(var denied in new[] {
            (Label:"invalid-status",Session:one,Student:student,Status:"Invalid",Tenant:academy,Token:(string?)token,Expected:HttpStatusCode.BadRequest),
            (Label:"no-active-enrollment",Session:one,Student:Guid.NewGuid(),Status:"Present",Tenant:academy,Token:(string?)token,Expected:HttpStatusCode.BadRequest),
            (Label:"missing-session",Session:Guid.NewGuid(),Student:student,Status:"Present",Tenant:academy,Token:(string?)token,Expected:HttpStatusCode.NotFound),
            (Label:"foreign-route",Session:one,Student:student,Status:"Present",Tenant:foreign,Token:(string?)token,Expected:HttpStatusCode.Forbidden),
            (Label:"anonymous",Session:one,Student:student,Status:"Present",Tenant:academy,Token:(string?)null,Expected:HttpStatusCode.Unauthorized)
        }) {
            using var response=await Send(denied.Session,new {studentId=denied.Student,status=denied.Status,notes="Must not be written"},denied.Token,denied.Tenant);
            RequireFinanceStatus(response,denied.Expected,denied.Label);if(await Snapshot()!=original)throw new InvalidOperationException("Denied request mutated attendance: "+denied.Label);
            count++;Console.WriteLine($"ATTENDANCENOTES CASE {denied.Label} PASS.");
        }
        using var secondSave=await Send(two,new {studentId=student,status="Online",notes="New session two note"},token);RequireFinanceStatus(secondSave,HttpStatusCode.OK,"second-session");
        using(var scope=factory.Services.CreateScope()) {
            var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var first=await db.AttendanceRecords.AsNoTracking().SingleAsync(x=>x.ClassSessionId==one&&x.StudentId==student);
            var second=await db.AttendanceRecords.AsNoTracking().SingleAsync(x=>x.ClassSessionId==two&&x.StudentId==student);
            if(first.Notes!="New attendance"||first.Status!="Excused"||second.Notes!="New session two note"||second.Status!="Online")throw new InvalidOperationException("Two-session independence failed");
        }
        count++;Console.WriteLine("ATTENDANCENOTES CASE second-session-independence PASS.");
        if(count!=16)throw new InvalidOperationException("Unexpected attendance case count: "+count);
        Console.WriteLine("ATTENDANCENOTES REGRESSION PASS:16 cases; ten actual TSX payloads to real Identity/HTTP/fresh SQL/GET, optional null/clear/trim/unicode/500 characters, session independence and five no-write denials. Browser/device acceptance pending.");
    }
}
