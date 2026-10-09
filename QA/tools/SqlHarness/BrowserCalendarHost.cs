using System.Net;
using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

internal static partial class SqlHarnessEntryPoint
{
    private static Uri ValidateCalendarBrowserOrigin()
    {
        if(!int.TryParse(Environment.GetEnvironmentVariable("QA_BROWSER_API_PORT"),out var port)||port is <1024 or >65535||
           !Uri.TryCreate(Environment.GetEnvironmentVariable("QA_BROWSER_ORIGIN"),UriKind.Absolute,out var origin)||
           origin.Scheme!="http"||origin.Host!="127.0.0.1"||origin.Port is <1024 or >65535||origin.Port==port||
           origin.AbsolutePath!="/"||origin.Query!=""||origin.Fragment!=""||origin.UserInfo!="")
            throw new InvalidOperationException("Calendar browser requires exact distinct loopback API port and origin.");
        return origin;
    }

    private static async Task ServeCalendarBrowserAsync(QaApiFactory factory,HttpClient client,QaRunManifest manifest,Uri origin)
    {
        var port=int.Parse(Environment.GetEnvironmentVariable("QA_BROWSER_API_PORT")!);
        Guid academy,student,teacher,course;var cases=new List<object>();
        var starts=new[]{new DateTime(2026,9,30,19,0,0,DateTimeKind.Utc),new DateTime(2026,12,31,19,0,0,DateTimeKind.Utc)};
        using(var scope=factory.Services.CreateScope()) {
            var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            academy=await db.Academies.Where(x=>x.Name=="Synthetic Academy A").Select(x=>x.Id).SingleAsync();
            student=await db.Students.Where(x=>x.AcademyId==academy).Select(x=>x.Id).SingleAsync();
            teacher=await db.Teachers.Where(x=>x.AcademyId==academy&&x.FirstName=="Session").Select(x=>x.Id).SingleAsync();
            course=await db.Batches.Where(x=>x.AcademyId==academy).Select(x=>x.CourseId).SingleAsync();
            // Disposable fixture needs the real Calendar/Schedule/Dashboard entitlements.
            var fixtureAcademy=await db.Academies.SingleAsync(x=>x.Id==academy);
            var modules=JsonSerializer.Deserialize<List<string>>(fixtureAcademy.EnabledModulesJson!)!;
            foreach(var module in new[]{"Certificates","MultiBranch","Finance"})if(!modules.Contains(module))modules.Add(module);
            fixtureAcademy.EnabledModulesJson=JsonSerializer.Serialize(modules);await db.SaveChangesAsync();
        }
        client.DefaultRequestHeaders.Authorization=new AuthenticationHeaderValue("Bearer",await LoginAsync(client,"qa-admin-a@example.invalid","Synthetic!39Ab"));
        var basePath=$"/api/academies/{academy}";var index=0;
        foreach(var zone in new[]{"UTC","Asia/Kolkata","America/Los_Angeles","Australia/Sydney"})foreach(var width in new[]{320,1440})foreach(var theme in new[]{"light","dark"}) {
            var label=zone.Replace('/','-')+"-"+width+"-"+theme;
            // Each independent browser case uses its own real same-tenant Identity actor.
            // Keep production limiter configuration and middleware ordering unchanged.
            var username="qa-live-"+label.ToLowerInvariant()+"@example.invalid";
            await CreateAccessActorAsync(factory,username,"AcademyAdmin",academy);
            var batch=new Batch {AcademyId=academy,CourseId=course,TeacherId=teacher,Name="Live "+label,Capacity=10};
            using(var scope=factory.Services.CreateScope()) {var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();db.Add(batch);await db.SaveChangesAsync();}
            var startUtc=starts[0].AddDays(index++);var endUtc=startUtc.AddHours(1);var roomName="https://meeting.example.invalid/live/"+label;
            await Task.Delay(550);using var created=await client.PostAsJsonAsync(basePath+"/sessions",new {batchId=batch.Id,teacherId=teacher,startUtc,endUtc,deliveryMode="Online",roomName});
            RequireFinanceStatus(created,HttpStatusCode.Created,"live-calendar-session");using var json=JsonDocument.Parse(await created.Content.ReadAsStringAsync());
            cases.Add(new {label,username,sessionId=json.RootElement.GetProperty("id").GetGuid(),batchId=batch.Id,batchName=batch.Name,startUtc,endUtc,roomName,day=index});
        }
        foreach(var startUtc in starts) {
            await Task.Delay(550);using var createdEvent=await client.PostAsJsonAsync(basePath+"/events",new {title="Live boundary recital "+startUtc.Year+"-"+startUtc.Month,type="Recital",startUtc,endUtc=startUtc.AddHours(1),venue="QA Hall"});
            RequireFinanceStatus(createdEvent,HttpStatusCode.Created,"live-calendar-event");
            using var createdMakeup=await client.PostAsJsonAsync(basePath+"/makeup-classes",new {studentId=student,batchId=((JsonElement)JsonSerializer.SerializeToElement(cases[0])).GetProperty("batchId").GetGuid(),startUtc,deliveryMode="Hybrid",meetingLink="https://meeting.example.invalid/boundary",useNextScheduledClass=false});
            RequireFinanceStatus(createdMakeup,HttpStatusCode.OK,"live-calendar-makeup");
        }
        var readsBefore=await Snapshot();
        foreach(var endpoint in new[]{"events","makeup-classes"}) {
            await Task.Delay(550);using var response=await client.GetAsync(basePath+"/"+endpoint);RequireFinanceStatus(response,HttpStatusCode.OK,"live-calendar-"+endpoint);
            using var json=JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            using var scope=factory.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var expected=endpoint=="events"
                ? await db.AcademyEvents.AsNoTracking().Where(x=>x.AcademyId==academy).Select(x=>new {x.Id,x.StartUtc,x.EndUtc,x.Status}).ToListAsync()
                : await db.MakeupClasses.AsNoTracking().Where(x=>x.AcademyId==academy).Select(x=>new {x.Id,x.StartUtc,x.EndUtc,x.Status}).ToListAsync();
            if(json.RootElement.GetArrayLength()!=2||expected.Count!=2)throw new InvalidOperationException("Boundary source history count mismatch");
            foreach(var row in json.RootElement.EnumerateArray()) {
                var stored=expected.Single(x=>x.Id==row.GetProperty("id").GetGuid());
                foreach(var field in new[]{"startUtc","endUtc"}) {
                    if(!row.GetProperty(field).GetString()!.EndsWith('Z'))throw new InvalidOperationException("Unmarked UTC calendar source: "+endpoint+"/"+field);
                    if(row.GetProperty(field).GetDateTime().Ticks!=(field=="startUtc"?stored.StartUtc:stored.EndUtc).Ticks)throw new InvalidOperationException("Boundary source changed UTC ticks");
                }
                if(row.GetProperty("status").GetString()!=stored.Status)throw new InvalidOperationException("Boundary source lost lifecycle");
            }
            Console.WriteLine("CALENDARDETAILS BROWSER UTC "+endpoint+" PASS: two rows, marked UTC, fresh SQL ticks/status preserved");
        }
        if(readsBefore!=await Snapshot())throw new InvalidOperationException("Boundary GET changed stored rows");
        async Task<string> Snapshot() {
            using var scope=factory.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            return JsonSerializer.Serialize(new {sessions=await db.ClassSessions.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),events=await db.AcademyEvents.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),makeups=await db.MakeupClasses.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),batches=await db.Batches.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),notifications=await db.Notifications.AsNoTracking().OrderBy(x=>x.Id).ToListAsync()});
        }
        var initial=await Snapshot();var accepted=new HashSet<Guid>();var puts=0;var getCounts=new ConcurrentDictionary<string,int>();
        client.DefaultRequestHeaders.Authorization=null;
        var builder=WebApplication.CreateSlimBuilder();builder.Logging.ClearProviders();builder.WebHost.UseUrls($"http://127.0.0.1:{port}");
        await using var bridge=builder.Build();bridge.Run(async context=>{
            if(context.Connection.RemoteIpAddress is null||!IPAddress.IsLoopback(context.Connection.RemoteIpAddress)||context.Request.Host.Host!="127.0.0.1"||context.Request.Host.Port!=port){context.Response.StatusCode=400;return;}
            var allowedPut=context.Request.Method=="PUT"&&cases.Any(x=>context.Request.Path==basePath+"/sessions/"+JsonSerializer.SerializeToElement(x).GetProperty("sessionId").GetGuid());
            if(context.Request.Method is not ("GET" or "OPTIONS")&&!(context.Request.Method=="POST"&&context.Request.Path=="/api/auth/login")&&!allowedPut){context.Response.StatusCode=405;return;}
            var before=await Snapshot();using var request=new HttpRequestMessage(new HttpMethod(context.Request.Method),context.Request.Path+context.Request.QueryString);
            if(context.Request.ContentLength>0)request.Content=new StreamContent(context.Request.Body);
            foreach(var header in context.Request.Headers){if(header.Key.Equals("Host",StringComparison.OrdinalIgnoreCase)||header.Key.Equals("Connection",StringComparison.OrdinalIgnoreCase)||header.Key.Equals("Transfer-Encoding",StringComparison.OrdinalIgnoreCase))continue;if(!request.Headers.TryAddWithoutValidation(header.Key,header.Value.ToArray()))request.Content?.Headers.TryAddWithoutValidation(header.Key,header.Value.ToArray());}
            using var response=await client.SendAsync(request,HttpCompletionOption.ResponseHeadersRead,context.RequestAborted);
            context.Response.StatusCode=(int)response.StatusCode;
            foreach(var header in response.Headers.Concat(response.Content.Headers)){if(header.Key.Equals("Transfer-Encoding",StringComparison.OrdinalIgnoreCase)||header.Key.Equals("Connection",StringComparison.OrdinalIgnoreCase))continue;context.Response.Headers[header.Key]=header.Value.ToArray();}
            if(allowedPut&&response.IsSuccessStatusCode){
                var id=Guid.Parse(context.Request.Path.Value!.Split('/').Last());var expected=cases.Select(x=>JsonSerializer.SerializeToElement(x)).Single(x=>x.GetProperty("sessionId").GetGuid()==id);
                using var scope=factory.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();var row=await db.ClassSessions.AsNoTracking().SingleAsync(x=>x.Id==id&&x.AcademyId==academy);
                if(row.Status!="Cancelled"||row.StartUtc!=expected.GetProperty("startUtc").GetDateTime()||row.EndUtc!=expected.GetProperty("endUtc").GetDateTime()||row.RoomName!=expected.GetProperty("roomName").GetString()||row.TeacherId!=teacher||row.BatchId!=expected.GetProperty("batchId").GetGuid()||!accepted.Add(id))throw new InvalidOperationException("Live browser cancellation fresh SQL mismatch/repeat");
                puts++;Console.WriteLine($"CALENDARDETAILS BROWSER SQL CANCELLED id={id} preserved=True");
            }else if(before!=await Snapshot())throw new InvalidOperationException("Calendar read/rejected request changed domain rows");
            if(context.Request.Method=="GET")getCounts.AddOrUpdate(context.Request.Path.Value!,1,(_,count)=>count+1);
            Console.WriteLine($"CALENDARDETAILS BROWSER HTTP {context.Request.Method} {context.Request.Path} {(int)response.StatusCode}");
            await response.Content.CopyToAsync(context.Response.Body,context.RequestAborted);
        });
        await bridge.StartAsync();var stop=Path.Combine(manifest.Root,"stop-browser");
        Console.WriteLine("CALENDARDETAILS BROWSER READY "+JsonSerializer.Serialize(new {runId=manifest.RunId.ToString("N"),academyId=academy,api=port,origin=origin.Port,cases,stop},new JsonSerializerOptions(JsonSerializerDefaults.Web)));
        var deadline=DateTime.UtcNow.AddMinutes(30);while(!File.Exists(stop)&&DateTime.UtcNow<deadline)await Task.Delay(500);
        await bridge.StopAsync();if(accepted.Count!=16||puts!=16)throw new InvalidOperationException("Live calendar requires exactly sixteen distinct accepted UI cancellations");
        using var baseline=JsonDocument.Parse(initial);using var final=JsonDocument.Parse(await Snapshot());
        foreach(var field in new[]{"events","makeups","batches","notifications"})if(baseline.RootElement.GetProperty(field).GetRawText()!=final.RootElement.GetProperty(field).GetRawText())throw new InvalidOperationException("Calendar changed unrelated "+field);
        var finalRows=final.RootElement.GetProperty("sessions").EnumerateArray().ToArray();
        if(finalRows.Length!=baseline.RootElement.GetProperty("sessions").GetArrayLength())throw new InvalidOperationException("Calendar history count changed");
        foreach(var row in baseline.RootElement.GetProperty("sessions").EnumerateArray()) {
            var stored=finalRows.Single(x=>x.GetProperty("Id").GetGuid()==row.GetProperty("Id").GetGuid());
            foreach(var property in row.EnumerateObject())if(property.Name!="Status"&&stored.GetProperty(property.Name).GetRawText()!=property.Value.GetRawText())throw new InvalidOperationException("Calendar mutation lost original row fields");
            if(!accepted.Contains(row.GetProperty("Id").GetGuid())&&stored.GetRawText()!=row.GetRawText())throw new InvalidOperationException("Unrelated session changed");
        }
        Console.WriteLine("CALENDARDETAILS BROWSER FINAL PASS:16 UI cancellations/fresh SQL; all history/other domain rows preserved; "+JsonSerializer.Serialize(getCounts));
    }
}
