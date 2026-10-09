using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Domain.Identity;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

internal static partial class SqlHarnessEntryPoint
{
    private static async Task VerifyCalendarDetailsAsync(QaApiFactory factory,HttpClient client)
    {
        Guid academy,foreign,batch,teacherA,teacherB;var count=0;
        const string sessionLink="https://session.example.invalid/meeting?lesson=override#class";
        const string batchLink="https://batch.example.invalid/meeting";
        var start=new DateTime(2026,10,8,10,0,0,DateTimeKind.Utc);
        using(var scope=factory.Services.CreateScope()) {
            var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            academy=await db.Academies.Where(x=>x.Name=="Synthetic Academy A").Select(x=>x.Id).SingleAsync();
            foreign=await db.Academies.Where(x=>x.Name=="Synthetic Academy B").Select(x=>x.Id).SingleAsync();
            (await db.Academies.SingleAsync(x=>x.Id==academy)).EnabledModulesJson="[\"Core\",\"TeacherClassroom\"]";
            var b=await db.Batches.SingleAsync(x=>x.AcademyId==academy);batch=b.Id;teacherA=b.TeacherId??throw new InvalidOperationException("Missing batch teacher");b.MeetingLink=batchLink;
            var substitute=new Teacher {AcademyId=academy,FirstName="Session",LastName="Teacher"};teacherB=substitute.Id;db.Add(substitute);
            // Legacy nullable location/assignment fixture; current Online Create requires a location.
            db.Add(new ClassSession {AcademyId=academy,BatchId=batch,TeacherId=null,StartUtc=start.AddDays(1),EndUtc=start.AddDays(1).AddHours(1),DeliveryMode="Online",RoomName=null});
            db.Add(new ClassSession {AcademyId=academy,BatchId=batch,TeacherId=teacherA,StartUtc=start.AddDays(2),EndUtc=start.AddDays(2).AddHours(1),DeliveryMode="InPerson",RoomName="Room A"});
            await db.SaveChangesAsync();
        }
        await CreateAccessActorAsync(factory,"qa-calendar-substitute@example.invalid","Teacher",academy);
        using(var scope=factory.Services.CreateScope()) {
            var users=scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();var user=await users.FindByEmailAsync("qa-calendar-substitute@example.invalid")??throw new InvalidOperationException("Missing teacher identity");user.TeacherId=teacherB;
            if(!(await users.UpdateAsync(user)).Succeeded)throw new InvalidOperationException("Substitute teacher identity fixture failed");
        }
        var token=await LoginAsync(client,"qa-admin-a@example.invalid","Synthetic!39Ab");
        var substituteToken=await LoginAsync(client,"qa-calendar-substitute@example.invalid","Synthetic!39Ab");
        var batchToken=await LoginAsync(client,"qa-teacher-a@example.invalid","Synthetic!39Ab");client.DefaultRequestHeaders.Authorization=null;
        var route=$"/api/academies/{academy}";
        using var create=new HttpRequestMessage(HttpMethod.Post,route+"/sessions");create.Headers.Authorization=new AuthenticationHeaderValue("Bearer",token);
        create.Content=JsonContent.Create(new {batchId=batch,teacherId=teacherB,branchId=(Guid?)null,startUtc=start,endUtc=start.AddHours(1),deliveryMode="Online",roomName="  "+sessionLink+"  "});
        using var created=await client.SendAsync(create);RequireFinanceStatus(created,HttpStatusCode.Created,"create-session-override");
        using var createdJson=JsonDocument.Parse(await created.Content.ReadAsStringAsync());var sessionId=createdJson.RootElement.GetProperty("id").GetGuid();
        using(var scope=factory.Services.CreateScope()) {
            var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();var row=await db.ClassSessions.AsNoTracking().SingleAsync(x=>x.AcademyId==academy&&x.Id==sessionId);var b=await db.Batches.AsNoTracking().SingleAsync(x=>x.Id==batch);
            if(row.TeacherId!=teacherB||row.RoomName!=sessionLink||row.BatchId!=batch||row.DeliveryMode!="Online"||createdJson.RootElement.GetProperty("teacherId").GetGuid()!=row.TeacherId||createdJson.RootElement.GetProperty("roomName").GetString()!=row.RoomName||b.TeacherId!=teacherA||b.MeetingLink!=batchLink)
                throw new InvalidOperationException("Created session override differs from fresh SQL or changed batch defaults");
        }
        count++;Console.WriteLine("CALENDARDETAILS CASE create-override-response-fresh-SQL PASS.");
        async Task<string> Snapshot() {
            using var scope=factory.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            return JsonSerializer.Serialize(new {Sessions=await db.ClassSessions.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),Batches=await db.Batches.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),Teachers=await db.Teachers.AsNoTracking().OrderBy(x=>x.Id).ToListAsync()});
        }
        async Task<JsonElement?> Read(string label,string path,string? auth,HttpStatusCode expected) {
            await Task.Delay(550);var before=await Snapshot();using var request=new HttpRequestMessage(HttpMethod.Get,path);if(auth is not null)request.Headers.Authorization=new AuthenticationHeaderValue("Bearer",auth);
            using var response=await client.SendAsync(request);RequireFinanceStatus(response,expected,label);if(before!=await Snapshot())throw new InvalidOperationException("Read changed calendar rows: "+label);
            JsonElement? body=null;if(expected==HttpStatusCode.OK){using var json=JsonDocument.Parse(await response.Content.ReadAsStringAsync());body=json.RootElement.Clone();}
            count++;Console.WriteLine($"CALENDARDETAILS CASE {label} PASS.");return body;
        }
        var sessions=(await Read("owned-sessions-read",route+"/sessions",token,HttpStatusCode.OK))!.Value;
        var batches=(await Read("owned-batches-read",route+"/batches",token,HttpStatusCode.OK))!.Value;
        var teachers=(await Read("owned-teachers-read",route+"/teachers",token,HttpStatusCode.OK))!.Value;
        using(var scope=factory.Services.CreateScope()) {
            var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();var rows=await db.ClassSessions.AsNoTracking().Where(x=>x.AcademyId==academy).ToListAsync();
            if(sessions.GetArrayLength()!=rows.Count)throw new InvalidOperationException("Session list count differs from SQL");
            foreach(var row in sessions.EnumerateArray()) {
                var stored=rows.Single(x=>x.Id==row.GetProperty("id").GetGuid());Guid? assigned=row.GetProperty("teacherId").ValueKind==JsonValueKind.Null?null:row.GetProperty("teacherId").GetGuid();
                if(assigned!=stored.TeacherId||row.GetProperty("roomName").GetString()!=stored.RoomName||row.GetProperty("batchId").GetGuid()!=stored.BatchId)throw new InvalidOperationException("Session list details differ from fresh SQL");
                RequireUtc(row,"startUtc",stored.StartUtc);RequireUtc(row,"endUtc",stored.EndUtc);
            }
            var b=batches.EnumerateArray().Single(x=>x.GetProperty("id").GetGuid()==batch);if(b.GetProperty("teacherId").GetGuid()!=teacherA||b.GetProperty("meetingLink").GetString()!=batchLink)throw new InvalidOperationException("Batch defaults changed");
            var t=teachers.EnumerateArray().Single(x=>x.GetProperty("id").GetGuid()==teacherB);if(t.GetProperty("firstName").GetString()!="Session"||t.GetProperty("lastName").GetString()!="Teacher")throw new InvalidOperationException("Teacher lookup differs from SQL");
        }
        count++;Console.WriteLine("CALENDARDETAILS CASE session-batch-teacher-projections-match-fresh-SQL PASS.");
        var substituteCalendar=(await Read("substitute-teacher-calendar","/api/teacher/calendar?year=2026&month=10",substituteToken,HttpStatusCode.OK))!.Value;
        var defaultCalendar=(await Read("default-teacher-calendar","/api/teacher/calendar?year=2026&month=10",batchToken,HttpStatusCode.OK))!.Value;
        var substituteRow=substituteCalendar.GetProperty("sessions").EnumerateArray().Single(x=>x.GetProperty("id").GetGuid()==sessionId);
        RequireUtc(substituteRow,"startUtc",start);RequireUtc(substituteRow,"endUtc",start.AddHours(1));
        if(substituteRow.GetProperty("roomName").GetString()!=sessionLink||defaultCalendar.GetProperty("sessions").EnumerateArray().Any(x=>x.GetProperty("id").GetGuid()==sessionId))throw new InvalidOperationException("Teacher calendar assignment/location mismatch");
        count++;Console.WriteLine("CALENDARDETAILS CASE teacher-calendar-session-ownership-and-location PASS.");
        await Read("foreign-sessions-route",$"/api/academies/{foreign}/sessions",token,HttpStatusCode.Forbidden);
        await Read("foreign-batches-route",$"/api/academies/{foreign}/batches",token,HttpStatusCode.Forbidden);
        await Read("anonymous-sessions",route+"/sessions",null,HttpStatusCode.Unauthorized);
        Console.WriteLine("CALENDARDETAILS FIXTURE "+JsonSerializer.Serialize(new {sessions,batches,teachers,expected=new {sessionId,teacher="Session Teacher",location=sessionLink}},new JsonSerializerOptions(JsonSerializerDefaults.Web)));
        if(count!=11)throw new InvalidOperationException("Unexpected calendar case count: "+count);
        // Keep the original positive fixture intact; cancellation is a subsequent real HTTP transition.
        var scheduled=sessions.EnumerateArray().Single(x=>x.GetProperty("id").GetGuid()==sessionId);
        if(scheduled.GetProperty("status").GetString()!="Scheduled")throw new InvalidOperationException("Cancellation prerequisite is not Scheduled");
        object Update(string status)=>new {startUtc=start,endUtc=start.AddHours(1),deliveryMode="Online",roomName=sessionLink,status};
        async Task RejectWrite(string label,HttpMethod method,string path,string? auth,object payload,HttpStatusCode expected) {
            await Task.Delay(550);var before=await Snapshot();using var request=new HttpRequestMessage(method,path);
            if(auth is not null)request.Headers.Authorization=new AuthenticationHeaderValue("Bearer",auth);
            request.Content=JsonContent.Create(payload);using var response=await client.SendAsync(request);RequireFinanceStatus(response,expected,label);
            if(before!=await Snapshot())throw new InvalidOperationException("Rejected cancellation changed calendar data: "+label);
            count++;Console.WriteLine($"CALENDARDETAILS CASE {label} PASS.");
        }
        await RejectWrite("foreign-cancellation-no-write",HttpMethod.Put,$"/api/academies/{foreign}/sessions/{sessionId}",token,Update("Cancelled"),HttpStatusCode.Forbidden);
        await RejectWrite("anonymous-cancellation-no-write",HttpMethod.Put,route+$"/sessions/{sessionId}",null,Update("Cancelled"),HttpStatusCode.Unauthorized);
        await RejectWrite("unassigned-teacher-cancellation-no-write",HttpMethod.Patch,$"/api/teacher/sessions/{sessionId}/status",batchToken,new {status="Cancelled"},HttpStatusCode.Forbidden);
        await RejectWrite("invalid-cancellation-status-no-write",HttpMethod.Put,route+$"/sessions/{sessionId}",token,Update("Invalid"),HttpStatusCode.BadRequest);
        var beforeCancellation=await Snapshot();await Task.Delay(550);
        using var cancel=new HttpRequestMessage(HttpMethod.Put,route+$"/sessions/{sessionId}");cancel.Headers.Authorization=new AuthenticationHeaderValue("Bearer",token);cancel.Content=JsonContent.Create(Update("Cancelled"));
        using var cancelled=await client.SendAsync(cancel);RequireFinanceStatus(cancelled,HttpStatusCode.OK,"owned-cancellation");
        using var cancelledJson=JsonDocument.Parse(await cancelled.Content.ReadAsStringAsync());
        using(var scope=factory.Services.CreateScope()) {
            var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var row=await db.ClassSessions.AsNoTracking().SingleAsync(x=>x.AcademyId==academy&&x.Id==sessionId);
            if(row.Status!="Cancelled"||row.TeacherId!=teacherB||row.BatchId!=batch||row.BranchId!=scheduled.GetProperty("branchId").Deserialize<Guid?>()||row.StartUtc!=start||row.EndUtc!=start.AddHours(1)||row.RoomName!=sessionLink||row.DeliveryMode!="Online")
                throw new InvalidOperationException("Cancellation fresh SQL lost status or original session details");
            using var before=JsonDocument.Parse(beforeCancellation);using var after=JsonDocument.Parse(await Snapshot());
            // Compare every other session and all defaults, not only the target's selected columns.
            foreach(var entity in new[]{"Sessions","Batches","Teachers"}) {
                var a=before.RootElement.GetProperty(entity).EnumerateArray().Where(x=>entity!="Sessions"||x.GetProperty("Id").GetGuid()!=sessionId).Select(x=>x.GetRawText());
                var b=after.RootElement.GetProperty(entity).EnumerateArray().Where(x=>entity!="Sessions"||x.GetProperty("Id").GetGuid()!=sessionId).Select(x=>x.GetRawText());
                if(!a.SequenceEqual(b))throw new InvalidOperationException("Cancellation changed unrelated rows/defaults: "+entity);
            }
        }
        var cancelledRow=cancelledJson.RootElement.Clone();
        foreach(var property in scheduled.EnumerateObject()) {
            var expected=property.Name=="status"?"\"Cancelled\"":property.Value.GetRawText();
            if(cancelledRow.GetProperty(property.Name).GetRawText()!=expected)throw new InvalidOperationException("Cancellation response changed original fields: "+property.Name);
        }
        count++;Console.WriteLine("CALENDARDETAILS CASE owned-cancellation-response-fresh-SQL-preserved-history PASS.");
        var cancelledSessions=(await Read("cancelled-owned-sessions-read",route+"/sessions",token,HttpStatusCode.OK))!.Value;
        var cancelledTeacherCalendar=(await Read("cancelled-substitute-teacher-calendar","/api/teacher/calendar?year=2026&month=10",substituteToken,HttpStatusCode.OK))!.Value;
        using(var scope=factory.Services.CreateScope()) {
            var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();var rows=await db.ClassSessions.AsNoTracking().Where(x=>x.AcademyId==academy).ToListAsync();
            if(cancelledSessions.GetArrayLength()!=sessions.GetArrayLength()||cancelledSessions.GetArrayLength()!=rows.Count)throw new InvalidOperationException("Cancellation dropped history or added rows");
            foreach(var responseRow in cancelledSessions.EnumerateArray()) {
                var row=rows.Single(x=>x.Id==responseRow.GetProperty("id").GetGuid());
                if(row.Status!=responseRow.GetProperty("status").GetString())throw new InvalidOperationException("Cancelled GET status differs from fresh SQL");
                var prior=sessions.EnumerateArray().Single(x=>x.GetProperty("id").GetGuid()==row.Id);
                foreach(var property in prior.EnumerateObject()) {
                    var expected=row.Id==sessionId&&property.Name=="status"?"\"Cancelled\"":property.Value.GetRawText();
                    if(responseRow.GetProperty(property.Name).GetRawText()!=expected)throw new InvalidOperationException("Cancelled GET changed original details/history");
                }
            }
            var teacherRow=cancelledTeacherCalendar.GetProperty("sessions").EnumerateArray().Single(x=>x.GetProperty("id").GetGuid()==sessionId);
            if(teacherRow.GetProperty("status").GetString()!="Cancelled"||teacherRow.GetProperty("roomName").GetString()!=sessionLink)throw new InvalidOperationException("Teacher cancelled readback mismatch");
        }
        count++;Console.WriteLine("CALENDARDETAILS CASE cancelled-admin-teacher-projections-match-fresh-SQL PASS.");
        Console.WriteLine("CALENDARDETAILS CANCELLATION FIXTURE "+JsonSerializer.Serialize(new {academyId=academy,beforeSessions=sessions,afterSessions=cancelledSessions,batches,teachers,expected=new {sessionId,teacher="Session Teacher",location=sessionLink,beforeStatus="Scheduled",afterStatus="Cancelled",cancelHttpStatus=200,freshSqlVerified=true}},new JsonSerializerOptions(JsonSerializerDefaults.Web)));
        if(count!=19)throw new InvalidOperationException("Unexpected extended calendar case count: "+count);
        Console.WriteLine("CALENDARDETAILS REGRESSION PASS:19 cases; original 11 retained; real Scheduled-to-Cancelled HTTP/fresh SQL/history, read-only admin/teacher projections and four denied no-write transitions; captured HTTP fixtures emitted for TSX/browser replay. Physical device pending.");
        static void RequireUtc(JsonElement row,string field,DateTime expected) {
            var raw=row.GetProperty(field).GetString();
            if(raw is null||!raw.EndsWith('Z')||row.GetProperty(field).GetDateTime().Kind!=DateTimeKind.Utc||row.GetProperty(field).GetDateTime()!=DateTime.SpecifyKind(expected,DateTimeKind.Utc))
                throw new InvalidOperationException("Calendar response must preserve the SQL UTC instant with explicit Z: "+field);
        }
    }
}
