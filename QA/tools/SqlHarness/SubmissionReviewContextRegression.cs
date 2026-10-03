using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

internal static partial class SqlHarnessEntryPoint
{
    private static async Task VerifyReviewContextAsync(QaApiFactory factory, HttpClient client)
    {
        Guid academy, foreign; var json = new JsonSerializerOptions(JsonSerializerDefaults.Web); var count = 0;
        using(var scope=factory.Services.CreateScope())
        {
            var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); academy=await db.Academies.Where(x=>x.Name=="Synthetic Academy A").Select(x=>x.Id).SingleAsync(); foreign=await db.Academies.Where(x=>x.Name=="Synthetic Academy B").Select(x=>x.Id).SingleAsync();
            foreach(var id in new[]{academy,foreign}) (await db.Academies.SingleAsync(x=>x.Id==id)).EnabledModulesJson="[\"Core\",\"AcademicGovernance\"]";
            var batch=await db.Batches.SingleAsync(x=>x.AcademyId==academy); var foreignBatch=new Batch{AcademyId=foreign,Name="Private foreign batch"}; var secondBatch=new Batch{AcademyId=academy,Name="Synthetic second batch"};
            var s1=new Student{AcademyId=academy,FirstName="Synthetic",LastName="One",StudentNumber="QA-1"};var s2=new Student{AcademyId=academy,FirstName="Synthetic",LastName="Two"};var fs=new Student{AcademyId=foreign,FirstName="Private",LastName="Foreign"};
            var a1=new Assignment{AcademyId=academy,BatchId=batch.Id,Title="Work One"};var a2=new Assignment{AcademyId=academy,BatchId=secondBatch.Id,Title="Work Two"};var fa=new Assignment{AcademyId=foreign,BatchId=foreignBatch.Id,Title="Private Foreign Work"};var fb=new Assignment{AcademyId=academy,BatchId=foreignBatch.Id,Title="Legacy foreign batch"};var missing=new Assignment{AcademyId=academy,BatchId=Guid.NewGuid(),Title="Legacy missing batch"};
            db.AddRange(s1,s2,fs,foreignBatch,secondBatch,a1,a2,fa,fb,missing);
            var pairs=new[]{(s1.Id,a1.Id),(s2.Id,a2.Id),(fs.Id,a1.Id),(s1.Id,fa.Id),(Guid.NewGuid(),a1.Id),(s1.Id,Guid.NewGuid()),(s1.Id,fb.Id),(s1.Id,missing.Id)};
            for(var i=0;i<pairs.Length;i++) db.Add(new AssignmentSubmission{AcademyId=academy,StudentId=pairs[i].Item1,AssignmentId=pairs[i].Item2,ResponseText=i%2==0?"Identical":null,SubmittedAtUtc=new DateTime(2026,10,1,9,i,0,DateTimeKind.Utc)});
            db.Add(new AssignmentSubmission{AcademyId=foreign,StudentId=fs.Id,AssignmentId=fa.Id,ResponseText="Foreign sentinel"}); await db.SaveChangesAsync();
        }
        var token=await LoginAsync(client,"qa-admin-a@example.invalid","Synthetic!39Ab");var foreignToken=await LoginAsync(client,"qa-admin-b@example.invalid","Synthetic!39Ab");var teacherToken=await LoginAsync(client,"qa-teacher-a@example.invalid","Synthetic!39Ab");client.DefaultRequestHeaders.Authorization=null;
        string route=$"/api/academies/{academy}/assignment-submissions";
        void Pass(string label){count++;Console.WriteLine($"REVIEWCONTEXT CASE {label} PASS.");}
        async Task<JsonElement> Snapshot(){using var scope=factory.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();return JsonSerializer.SerializeToElement(new{rows=await db.AssignmentSubmissions.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),students=await db.Students.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),assignments=await db.Assignments.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),batches=await db.Batches.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),audits=await db.AuditLogs.AsNoTracking().OrderBy(x=>x.Id).ToListAsync()});}
        async Task<JsonElement?> Send(string label,HttpMethod method,string path,string? auth,HttpStatusCode expected,object? body=null){await Task.Delay(650);var before=await Snapshot();using var request=new HttpRequestMessage(method,path);if(auth is not null)request.Headers.Authorization=new AuthenticationHeaderValue("Bearer",auth);if(body is not null)request.Content=JsonContent.Create(body);using var response=await client.SendAsync(request);RequireFinanceStatus(response,expected,label);if(method==HttpMethod.Get||expected!=HttpStatusCode.OK)if(!JsonElement.DeepEquals(before,await Snapshot()))throw new InvalidOperationException("Read/rejection wrote data "+label);JsonElement? value=null;if(expected==HttpStatusCode.OK)value=await response.Content.ReadFromJsonAsync<JsonElement>(json);Pass(label);return value;}
        async Task Verify(JsonElement item)
        {
            using var scope=factory.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();var row=await db.AssignmentSubmissions.AsNoTracking().SingleAsync(x=>x.Id==item.GetProperty("id").GetGuid());
            if(row.AcademyId!=academy)throw new InvalidOperationException("Foreign submission leaked");foreach(var field in JsonSerializer.SerializeToElement(row,json).EnumerateObject())if(!JsonElement.DeepEquals(field.Value,item.GetProperty(field.Name)))throw new InvalidOperationException("Legacy field changed "+field.Name);
            var student=await db.Students.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==row.StudentId&&x.AcademyId==academy);var assignment=await db.Assignments.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==row.AssignmentId&&x.AcademyId==academy);var batch=assignment is null?null:await db.Batches.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==assignment.BatchId&&x.AcademyId==academy);
            if(item.GetProperty("studentName").GetString()!=(student is null?null:$"{student.FirstName} {student.LastName}".Trim())||item.GetProperty("studentNumber").GetString()!=student?.StudentNumber||item.GetProperty("assignmentTitle").GetString()!=assignment?.Title||item.GetProperty("batchName").GetString()!=batch?.Name||(item.GetProperty("batchId").ValueKind==JsonValueKind.Null?null:(Guid?)item.GetProperty("batchId").GetGuid())!=batch?.Id)throw new InvalidOperationException("Scoped projection mismatch");
        }
        var list=(await Send("owned-read-no-write",HttpMethod.Get,route,token,HttpStatusCode.OK))!.Value; if(list.GetArrayLength()!=8)throw new InvalidOperationException("Wrong scoped count");
        foreach(var item in list.EnumerateArray()){await Verify(item);Pass("scoped-context-"+item.GetProperty("submittedAtUtc").GetDateTime().Minute);}
        var times=list.EnumerateArray().Select(x=>x.GetProperty("submittedAtUtc").GetDateTime()).ToArray();if(!times.SequenceEqual(times.OrderDescending()))throw new InvalidOperationException("Order changed");
        var assignmentId=list[0].GetProperty("assignmentId").GetGuid();var filtered=(await Send("assignment-filter",HttpMethod.Get,route+"?assignmentId="+assignmentId,token,HttpStatusCode.OK))!.Value;if(filtered.GetArrayLength()!=1||filtered[0].GetProperty("assignmentId").GetGuid()!=assignmentId)throw new InvalidOperationException("Filter differs");
        var before=await Snapshot();var target=list.EnumerateArray().Single(x=>x.GetProperty("assignmentTitle").GetString()=="Work Two");var targetId=target.GetProperty("id").GetGuid();var reviewed=(await Send("exact-target-review",HttpMethod.Patch,route+$"/{targetId}/review",token,HttpStatusCode.OK,new ReviewRequest("Synthetic feedback")))!.Value;await Verify(reviewed);
        var after=await Snapshot();foreach(var field in before.EnumerateObject().Where(x=>x.Name!="rows"&&x.Name!="audits"))if(!JsonElement.DeepEquals(field.Value,after.GetProperty(field.Name)))throw new InvalidOperationException("Review changed sources");
        foreach(var row in before.GetProperty("rows").EnumerateArray()){var current=after.GetProperty("rows").EnumerateArray().Single(x=>x.GetProperty("Id").GetGuid()==row.GetProperty("Id").GetGuid());if(row.GetProperty("Id").GetGuid()!=targetId){if(!JsonElement.DeepEquals(row,current))throw new InvalidOperationException("Unrelated submission modified");}else{foreach(var field in row.EnumerateObject().Where(x=>x.Name!="Status"&&x.Name!="TeacherFeedback"))if(!JsonElement.DeepEquals(field.Value,current.GetProperty(field.Name)))throw new InvalidOperationException("Unexpected review field changed");if(current.GetProperty("Status").GetString()!="Reviewed"||current.GetProperty("TeacherFeedback").GetString()!="Synthetic feedback")throw new InvalidOperationException("Feedback not saved");}}
        var oldAudits=before.GetProperty("audits").EnumerateArray().ToArray();var newAudits=after.GetProperty("audits").EnumerateArray().ToArray();if(newAudits.Length!=oldAudits.Length+1||oldAudits.Any(x=>!newAudits.Any(y=>JsonElement.DeepEquals(x,y))))throw new InvalidOperationException("Audit preservation mismatch");var audit=newAudits.Single(x=>!oldAudits.Any(y=>JsonElement.DeepEquals(x,y)));if(audit.GetProperty("AcademyId").GetGuid()!=academy||audit.GetProperty("Action").GetString()!="PATCH AssignmentSubmissions"||audit.GetProperty("ActorUserId").ValueKind==JsonValueKind.Null)throw new InvalidOperationException("Actor audit mismatch");Pass("fresh-SQL-exact-write-and-audit");
        var readback=(await Send("fresh-readback",HttpMethod.Get,route,token,HttpStatusCode.OK))!.Value;await Verify(readback.EnumerateArray().Single(x=>x.GetProperty("id").GetGuid()==targetId));
        foreach(var method in new[]{HttpMethod.Get,HttpMethod.Patch})foreach(var auth in new[]{(label:"anonymous",token:(string?)null,status:HttpStatusCode.Unauthorized),(label:"foreign",token:(string?)foreignToken,status:HttpStatusCode.Forbidden),(label:"teacher",token:(string?)teacherToken,status:HttpStatusCode.Forbidden)})await Send(auth.label+"-"+method,method,method==HttpMethod.Get?route:route+$"/{targetId}/review",auth.token,auth.status,method==HttpMethod.Get?null:new ReviewRequest("Denied"));
        Guid foreignId;using(var scope=factory.Services.CreateScope())foreignId=await scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>().AssignmentSubmissions.Where(x=>x.AcademyId==foreign).Select(x=>x.Id).SingleAsync();
        foreach(var id in new[]{foreignId,Guid.NewGuid()})await Send(id==foreignId?"foreign-row-404":"missing-row-404",HttpMethod.Patch,route+$"/{id}/review",token,HttpStatusCode.NotFound,new ReviewRequest(null));
        Console.WriteLine($"REVIEWCONTEXT REGRESSION PASS:{count} cases; scoped labels, full legacy projection, exact feedback persistence, unchanged sources/foreign rows and real auth gates.");
    }
}
