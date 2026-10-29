using System.Diagnostics;
using System.Net;
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
    private static async Task VerifyPentaTeacherBrowserAsync(QaRunManifest manifest)
    {
        var flags=new Dictionary<string,string?>{["Penta:TeacherCreationContractsEnabled"]="true",["Penta:OperationalDraftContractsEnabled"]="true",["Penta:OperationalOnboardingHttpContractsEnabled"]="true"};
        using var factory=new QaApiFactory(manifest,flags);using var unavailable=new QaApiFactory(manifest);using var client=factory.CreateClient();using var off=unavailable.CreateClient();
        Guid academy;using(var scope=factory.Services.CreateScope()){
            var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();var row=new Academy{Name="Synthetic teacher UI "+manifest.RunId.ToString("N")};db.Academies.Add(row);await db.SaveChangesAsync();academy=row.Id;}
        var actor=await CreateAccessActorAsync(factory,"qa-teacher-ui@example.invalid","AcademyAdmin",academy);
        var token=await LoginAsync(client,"qa-teacher-ui@example.invalid","Synthetic!39Ab");var teacher=await LoginAsync(client,"qa-teacher-a@example.invalid","Synthetic!39Ab");
        client.DefaultRequestHeaders.Authorization=new AuthenticationHeaderValue("Bearer",token);
        Guid session;using(var scope=factory.Services.CreateScope()){
            var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();var row=new PentaMiniSession{AcademyId=academy,ActorUserId=actor,Version=1,ExpiresAtUtc=DateTime.UtcNow.AddHours(1),ProtectedState="Synthetic teacher browser session; no inference"};db.PentaMiniSessions.Add(row);await db.SaveChangesAsync();session=row.Id;}
        var drafts=new Dictionary<string,Guid>();foreach(var name in new[]{"light-mobile","dark-desktop","lost-response","cancelled"}){
            var id=Guid.NewGuid();var fields=new Dictionary<string,object>{["FirstName"]=new{kind="text",value="BrowserSynthetic"},["LastName"]=new{kind="text",value=name},
                ["Email"]=new{kind="text",value="qa-teacher-ui-"+name+"@example.invalid"},["Specialties"]=new{kind="text",value="Piano and classroom coordination"},
                ["Qualifications"]=new{kind="text",value="Synthetic QA qualification; no customer information"},["City"]=new{kind="text",value="Synthetic city"}};
            using var response=await client.PutAsJsonAsync($"/api/academies/{academy}/penta/teachers/conversations/{session}/drafts/{id}",new{expectedRevision=0,contextVersion=1,fields});
            RequireFinanceStatus(response,HttpStatusCode.Created,"teacher UI protected seed draft");drafts.Add(name,id);}
        async Task<string> Unrelated(){using var scope=factory.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();var identity=scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            return JsonSerializer.Serialize(new{students=await db.Students.AsNoTracking().OrderBy(x=>x.Id).ToArrayAsync(),guardians=await db.Guardians.AsNoTracking().OrderBy(x=>x.Id).ToArrayAsync(),
                invoices=await db.Invoices.AsNoTracking().OrderBy(x=>x.Id).ToArrayAsync(),payments=await db.Payments.AsNoTracking().OrderBy(x=>x.Id).ToArrayAsync(),
                users=await identity.Users.AsNoTracking().OrderBy(x=>x.Id).Select(x=>new{x.Id,x.AcademyId,x.IsActive,x.TeacherId}).ToArrayAsync(),
                roles=await identity.UserRoles.AsNoTracking().OrderBy(x=>x.UserId).ThenBy(x=>x.RoleId).ToArrayAsync(),
                otherTeachers=await db.Teachers.AsNoTracking().Where(x=>x.AcademyId!=academy).OrderBy(x=>x.Id).ToArrayAsync(),
                otherDrafts=await db.PentaOnboardingDrafts.AsNoTracking().Where(x=>x.ActorUserId!=actor).OrderBy(x=>x.Id).ToArrayAsync(),
                sessions=await db.PentaMiniSessions.AsNoTracking().OrderBy(x=>x.Id).ToArrayAsync()});}
        // Seeding used a real admin bearer. The bridge must forward ONLY the
        // browser's supplied authorization; never inherit that fixture identity.
        client.DefaultRequestHeaders.Authorization=null;
        var before=await Unrelated();var requestCount=0;
        var builder=WebApplication.CreateSlimBuilder();builder.Logging.ClearProviders();builder.WebHost.UseUrls("http://127.0.0.1:0");await using var bridge=builder.Build();
        bridge.Run(async context=>{
            var path=context.Request.Path.Value??"";var offline=path.StartsWith("/offline/",StringComparison.Ordinal);if(offline)path=path[8..];
            var prefix=$"/api/academies/{academy}/penta/teachers/conversations/{session}/drafts/";
            var allowed=context.Request.Method=="GET"&&(path=="/api/auth/session"||path.StartsWith(prefix,StringComparison.Ordinal)) ||
                context.Request.Method=="POST"&&path.StartsWith(prefix,StringComparison.Ordinal)&&new[]{"/review","/approve","/execute","/cancel"}.Any(s=>path.EndsWith(s,StringComparison.Ordinal));
            if(context.Connection.RemoteIpAddress is null||!IPAddress.IsLoopback(context.Connection.RemoteIpAddress)||context.Request.Host.Host!="127.0.0.1"||!allowed||context.Request.ContentLength>2048){context.Response.StatusCode=405;return;}
            Interlocked.Increment(ref requestCount);using var request=new HttpRequestMessage(new HttpMethod(context.Request.Method),path);
            if(context.Request.Headers.TryGetValue("Authorization",out var authorization))request.Headers.TryAddWithoutValidation("Authorization",authorization.ToArray());
            if(context.Request.Method=="POST"){using var bytes=new MemoryStream();await context.Request.Body.CopyToAsync(bytes,context.RequestAborted);if(bytes.Length>2048){context.Response.StatusCode=413;return;}
                request.Content=new ByteArrayContent(bytes.ToArray());request.Content.Headers.ContentType=new MediaTypeHeaderValue("application/json");}
            using var response=await(offline?off:client).SendAsync(request,context.RequestAborted);context.Response.StatusCode=(int)response.StatusCode;
            context.Response.ContentType=response.Content.Headers.ContentType?.ToString();await response.Content.CopyToAsync(context.Response.Body,context.RequestAborted);
        });
        await bridge.StartAsync();
        var start=new ProcessStartInfo("node"){WorkingDirectory=Directory.GetCurrentDirectory(),UseShellExecute=false,RedirectStandardInput=true,RedirectStandardOutput=true,RedirectStandardError=true,CreateNoWindow=true};
        start.ArgumentList.Add(Path.GetFullPath("QA/tools/penta-teacher-live.cjs"));start.ArgumentList.Add(bridge.Urls.Single());start.ArgumentList.Add(manifest.RunId.ToString("N"));
        using var process=Process.Start(start)??throw new InvalidOperationException("Teacher UI browser could not start.");
        await process.StandardInput.WriteAsync(JsonSerializer.Serialize(new{academy,session,token,teacher,drafts}));process.StandardInput.Close();var stdout=process.StandardOutput.ReadToEndAsync();var stderr=process.StandardError.ReadToEndAsync();
        try{using var timeout=new CancellationTokenSource(TimeSpan.FromMinutes(4));await process.WaitForExitAsync(timeout.Token);}catch{if(!process.HasExited)process.Kill(entireProcessTree:true);throw;}
        Console.Write(await stdout);var error=await stderr;if(process.ExitCode!=0)throw new InvalidOperationException("Teacher browser failed: "+error);await bridge.StopAsync();
        if(before!=await Unrelated())throw new InvalidOperationException("Teacher UI changed unrelated protected state.");
        using(var scope=factory.Services.CreateScope()){
            var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();var teachers=await db.Teachers.AsNoTracking().Where(x=>x.AcademyId==academy).ToArrayAsync();
            var rows=await db.PentaOnboardingDrafts.AsNoTracking().Where(x=>x.ActorUserId==actor).ToArrayAsync();var audits=await db.AuditLogs.AsNoTracking().Where(x=>x.ActorUserId==actor&&x.Action=="PentaTeacherDraftExecuted").ToArrayAsync();
            if(teachers.Length!=3||audits.Length!=3||rows.Length!=4||rows.Count(x=>x.Status=="Succeeded")!=3||rows.Count(x=>x.Status=="Cancelled")!=1||
                audits.Any(x=>x.AcademyId!=academy)||teachers.Any(x=>x.FirstName!="BrowserSynthetic"))throw new InvalidOperationException("Teacher UI exact postconditions failed.");
            var evidence=Path.GetFullPath(Path.Combine("QA","EVIDENCE","penta-teacher-live-"+manifest.RunId.ToString("N")));
            await File.WriteAllTextAsync(Path.Combine(evidence,"sql-postconditions.json"),JsonSerializer.Serialize(new{passed=true,createdTeachers=3,executionAudits=3,succeededDrafts=3,cancelledDrafts=1,
                accountChanges=false,unrelatedStateUnchanged=true,actualHttpRequests=requestCount,modelInference=false,physicalDevices="NOT RUN"},new JsonSerializerOptions{WriteIndented=true}));
        }
        Console.WriteLine("PASS: PENTATEACHERBROWSER six actual browser/SQL journeys; three teachers/receipts/audits; lost-response GET recovery; no inference or429.");
    }
}
