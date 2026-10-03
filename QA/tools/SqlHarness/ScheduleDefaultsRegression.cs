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
    private static async Task VerifyScheduleDefaultsAsync(QaApiFactory factory,HttpClient client)
    {
        var fixtures=File.ReadLines("QA/EVIDENCE/logs/phase-2b-schedule-defaults-ui.log").Where(x=>x.StartsWith("SCHEDULEDEFAULTS PAYLOAD ",StringComparison.Ordinal))
            .Select(x=>JsonDocument.Parse(x["SCHEDULEDEFAULTS PAYLOAD ".Length..]).RootElement.Clone()).ToArray();
        if(fixtures.Length!=12||fixtures.Select(x=>x.GetProperty("label").GetString()).Distinct().Count()!=12)throw new InvalidOperationException("Run the actual schedule TSX payload tests first");
        Guid academy,foreign,foreignTeacher,foreignBranch,foreignBatch;var count=0;
        var batches=new Dictionary<string,Guid>();var teachers=new Dictionary<string,Guid>();var branches=new Dictionary<string,Guid>();
        using(var scope=factory.Services.CreateScope()) {
            var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            academy=await db.Academies.Where(x=>x.Name=="Synthetic Academy A").Select(x=>x.Id).SingleAsync();
            foreign=await db.Academies.Where(x=>x.Name=="Synthetic Academy B").Select(x=>x.Id).SingleAsync();
            (await db.Academies.SingleAsync(x=>x.Id==academy)).EnabledModulesJson="[\"Core\"]";
            var assigned=await db.Batches.SingleAsync(x=>x.AcademyId==academy);teachers["ta"]=assigned.TeacherId??throw new InvalidOperationException("Missing assigned teacher");batches["a"]=assigned.Id;
            var other=new Teacher {AcademyId=academy,FirstName="Other",LastName="Teacher"};teachers["tb"]=other.Id;db.Add(other);
            foreach(var alias in new[]{"ra","rb"}){var branch=new Branch {AcademyId=academy,Name="Synthetic defaults "+alias};branches[alias]=branch.Id;db.Add(branch);}assigned.BranchId=branches["ra"];
            foreach(var alias in new[]{"b","c","d","e","f"}) {
                var batch=new Batch {AcademyId=academy,CourseId=assigned.CourseId,Name="Synthetic defaults "+alias,TeacherId=alias is "c" or "e"?teachers["tb"]:null,BranchId=alias is "d" or "e"?branches["rb"]:null};batches[alias]=batch.Id;db.Add(batch);
            }
            var foreignT=new Teacher {AcademyId=foreign,FirstName="Foreign",LastName="Teacher"};foreignTeacher=foreignT.Id;
            var foreignR=new Branch {AcademyId=foreign,Name="Foreign defaults branch"};foreignBranch=foreignR.Id;
            var foreignB=new Batch {AcademyId=foreign,CourseId=Guid.NewGuid(),Name="Foreign defaults batch"};foreignBatch=foreignB.Id;db.AddRange(foreignT,foreignR,foreignB);await db.SaveChangesAsync();
        }
        var token=await LoginAsync(client,"qa-admin-a@example.invalid","Synthetic!39Ab");client.DefaultRequestHeaders.Authorization=null;
        Guid? Resolve(JsonElement value,Dictionary<string,Guid> aliases)=>value.ValueKind==JsonValueKind.Null?null:aliases[value.GetString()??throw new InvalidOperationException("Missing fixture alias")];
        async Task<string> BatchSnapshot(){using var scope=factory.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();return JsonSerializer.Serialize(await db.Batches.AsNoTracking().OrderBy(x=>x.Id).ToListAsync());}
        async Task<string> SessionSnapshot(){using var scope=factory.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();return JsonSerializer.Serialize(await db.ClassSessions.AsNoTracking().OrderBy(x=>x.Id).ToListAsync());}
        var initialBatches=await BatchSnapshot();
        async Task<HttpResponseMessage> Send(Guid target,object payload,string? auth){await Task.Delay(550);using var request=new HttpRequestMessage(HttpMethod.Post,$"/api/academies/{target}/sessions");if(auth is not null)request.Headers.Authorization=new AuthenticationHeaderValue("Bearer",auth);request.Content=JsonContent.Create(payload);return await client.SendAsync(request);}
        foreach(var fixture in fixtures) {
            var label=fixture.GetProperty("label").GetString()??throw new InvalidOperationException("Missing fixture label");var payload=fixture.GetProperty("payload");var expected=fixture.GetProperty("expected");
            var batchId=batches[payload.GetProperty("batchId").GetString()!];var teacherId=Resolve(payload.GetProperty("teacherId"),teachers);var branchId=Resolve(payload.GetProperty("branchId"),branches);
            var expectedTeacher=Resolve(expected.GetProperty("teacherId"),teachers);var expectedBranch=Resolve(expected.GetProperty("branchId"),branches);
            var start=payload.GetProperty("startUtc").GetDateTime();var end=payload.GetProperty("endUtc").GetDateTime();
            using var response=await Send(academy,new {batchId,teacherId,branchId,startUtc=start,endUtc=end,deliveryMode=payload.GetProperty("deliveryMode").GetString(),roomName=payload.GetProperty("roomName").GetString()},token);
            RequireFinanceStatus(response,HttpStatusCode.Created,label);using var json=JsonDocument.Parse(await response.Content.ReadAsStringAsync());var id=json.RootElement.GetProperty("id").GetGuid();
            Guid? ReadId(JsonElement value)=>value.ValueKind==JsonValueKind.Null?null:value.GetGuid();
            using(var scope=factory.Services.CreateScope()) {
                var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();var row=await db.ClassSessions.AsNoTracking().SingleAsync(x=>x.Id==id&&x.AcademyId==academy);
                if(row.BatchId!=batchId||row.TeacherId!=expectedTeacher||row.BranchId!=expectedBranch||row.StartUtc!=start||row.EndUtc!=end||row.RoomName!=null||ReadId(json.RootElement.GetProperty("teacherId"))!=row.TeacherId||ReadId(json.RootElement.GetProperty("branchId"))!=row.BranchId)throw new InvalidOperationException("Actual form payload/response/fresh SQL assignment mismatch: "+label);
            }
            var beforeRead=await SessionSnapshot();using var read=new HttpRequestMessage(HttpMethod.Get,$"/api/academies/{academy}/sessions");read.Headers.Authorization=new AuthenticationHeaderValue("Bearer",token);
            using var listed=await client.SendAsync(read);RequireFinanceStatus(listed,HttpStatusCode.OK,"readback-"+label);using var list=JsonDocument.Parse(await listed.Content.ReadAsStringAsync());var returned=list.RootElement.EnumerateArray().Single(x=>x.GetProperty("id").GetGuid()==id);
            if(ReadId(returned.GetProperty("teacherId"))!=expectedTeacher||ReadId(returned.GetProperty("branchId"))!=expectedBranch||beforeRead!=await SessionSnapshot()||initialBatches!=await BatchSnapshot())throw new InvalidOperationException("Readback/default preservation mismatch: "+label);
            count++;Console.WriteLine($"SCHEDULEDEFAULTS CASE {label} PASS.");
        }
        // Preserve the existing null request means inherit current batch default contract.
        var optionalStart=new DateTime(2026,10,25,4,30,0,DateTimeKind.Utc);
        using var inherited=await Send(academy,new {batchId=batches["a"],teacherId=(Guid?)null,branchId=(Guid?)null,startUtc=optionalStart,endUtc=optionalStart.AddHours(1),deliveryMode="InPerson",roomName=(string?)null},token);
        RequireFinanceStatus(inherited,HttpStatusCode.Created,"null-override-inherits");using var inheritedJson=JsonDocument.Parse(await inherited.Content.ReadAsStringAsync());var inheritedId=inheritedJson.RootElement.GetProperty("id").GetGuid();
        using(var scope=factory.Services.CreateScope()){var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();var row=await db.ClassSessions.AsNoTracking().SingleAsync(x=>x.Id==inheritedId);if(row.TeacherId!=teachers["ta"]||row.BranchId!=branches["ra"]||inheritedJson.RootElement.GetProperty("teacherId").GetGuid()!=row.TeacherId||inheritedJson.RootElement.GetProperty("branchId").GetGuid()!=row.BranchId)throw new InvalidOperationException("Null/default inheritance contract changed");}
        count++;Console.WriteLine("SCHEDULEDEFAULTS CASE null-override-inherits-existing-contract PASS.");
        var finalSessions=await SessionSnapshot();
        foreach(var denied in new[]{
            (Label:"foreign-batch",Batch:foreignBatch,Teacher:(Guid?)null,Branch:(Guid?)null,Tenant:academy,Token:(string?)token,Expected:HttpStatusCode.BadRequest),
            (Label:"foreign-teacher",Batch:batches["b"],Teacher:(Guid?)foreignTeacher,Branch:(Guid?)null,Tenant:academy,Token:(string?)token,Expected:HttpStatusCode.BadRequest),
            (Label:"foreign-branch",Batch:batches["b"],Teacher:(Guid?)null,Branch:(Guid?)foreignBranch,Tenant:academy,Token:(string?)token,Expected:HttpStatusCode.BadRequest),
            (Label:"foreign-route",Batch:foreignBatch,Teacher:(Guid?)null,Branch:(Guid?)null,Tenant:foreign,Token:(string?)token,Expected:HttpStatusCode.Forbidden),
            (Label:"anonymous",Batch:batches["b"],Teacher:(Guid?)null,Branch:(Guid?)null,Tenant:academy,Token:(string?)null,Expected:HttpStatusCode.Unauthorized)
        }) {
            using var response=await Send(denied.Tenant,new {batchId=denied.Batch,teacherId=denied.Teacher,branchId=denied.Branch,startUtc=optionalStart.AddDays(1),endUtc=optionalStart.AddDays(1).AddHours(1),deliveryMode="InPerson",roomName=(string?)null},denied.Token);
            RequireFinanceStatus(response,denied.Expected,denied.Label);if(finalSessions!=await SessionSnapshot()||initialBatches!=await BatchSnapshot())throw new InvalidOperationException("Denied request changed session/batch rows: "+denied.Label);
            count++;Console.WriteLine($"SCHEDULEDEFAULTS CASE {denied.Label} PASS.");
        }
        if(count!=18)throw new InvalidOperationException("Unexpected schedule case count: "+count);
        Console.WriteLine("SCHEDULEDEFAULTS REGRESSION PASS:18 cases; twelve actual TSX payloads to real Identity/HTTP/fresh SQL/GET, current teacher/branch defaults and deliberate overrides, unchanged null inheritance and batch rows, five no-write person/batch/route/auth denials; browser/device pending.");
    }
}
