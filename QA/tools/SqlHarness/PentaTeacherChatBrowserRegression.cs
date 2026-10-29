using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
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
    private static async Task VerifyPentaTeacherChatBrowserAsync(QaRunManifest manifest)
    {
        var flags=new Dictionary<string,string?>{["Penta:TeacherCreationContractsEnabled"]="true",["Penta:TeacherCoreContractsEnabled"]="true",
            ["Penta:TeacherConversationContractsEnabled"]="true",["Penta:TeacherStartContractsEnabled"]="true",
            ["Penta:OperationalDraftContractsEnabled"]="true",["Penta:OperationalOnboardingHttpContractsEnabled"]="true"};
        var planner=new TeacherConversationPlanner();planner.Handler=(input,_)=>Task.FromResult(input.Text=="QA first name"?TeacherChatDecision(input,true,"TeacherChatSynthetic"):
            input.Text is "QA last name chat-light" or "QA last name chat-dark" or "QA last name chat-lost"?TeacherChatDecision(input,false,"TeacherChatSynthetic",input.Text[13..]):
            throw new InvalidOperationException("Only invented browser fixture phrases are supported by this QA interpreter."));
        using var factory=new QaApiFactory(manifest,flags,isolatedTeacherInterpreter:planner);using var unavailable=new QaApiFactory(manifest);
        using var client=factory.CreateClient();using var off=unavailable.CreateClient();Guid academy;
        using(var scope=factory.Services.CreateScope()){
            var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();var row=new Academy{Name="Synthetic teacher chat "+manifest.RunId.ToString("N")};db.Academies.Add(row);await db.SaveChangesAsync();academy=row.Id;}
        var actor=await CreateAccessActorAsync(factory,"qa-teacher-chat-ui@example.invalid","AcademyAdmin",academy);
        var token=await LoginAsync(client,"qa-teacher-chat-ui@example.invalid","Synthetic!39Ab");var teacher=await LoginAsync(client,"qa-teacher-a@example.invalid","Synthetic!39Ab");
        async Task<string> Unrelated(){using var scope=factory.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();var id=scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            return JsonSerializer.Serialize(new{users=await id.Users.AsNoTracking().OrderBy(x=>x.Id).ToArrayAsync(),roles=await id.UserRoles.AsNoTracking().OrderBy(x=>x.UserId).ThenBy(x=>x.RoleId).ToArrayAsync(),
                students=await db.Students.AsNoTracking().OrderBy(x=>x.Id).ToArrayAsync(),payments=await db.Payments.AsNoTracking().OrderBy(x=>x.Id).ToArrayAsync(),invoices=await db.Invoices.AsNoTracking().OrderBy(x=>x.Id).ToArrayAsync(),
                teachers=await db.Teachers.AsNoTracking().Where(x=>x.AcademyId!=academy).OrderBy(x=>x.Id).ToArrayAsync(),
                drafts=await db.PentaOnboardingDrafts.AsNoTracking().Where(x=>x.AcademyId!=academy).OrderBy(x=>x.Id).ToArrayAsync()});}
        var before=await Unrelated();var requests=0;client.DefaultRequestHeaders.Authorization=null;off.DefaultRequestHeaders.Authorization=null;
        var builder=WebApplication.CreateBuilder();builder.Logging.ClearProviders();builder.WebHost.UseUrls("http://127.0.0.1:0");await using var bridge=builder.Build();
        bridge.Run(async context=>{
            var path=context.Request.Path.Value??"";var offline=path.StartsWith("/offline/",StringComparison.Ordinal);if(offline)path=path[8..];
            if(!(path=="/api/auth/session"&&context.Request.Method=="GET"||path.StartsWith($"/api/academies/{academy}/penta/teachers/",StringComparison.Ordinal)&&context.Request.Method is "GET" or "POST")){context.Response.StatusCode=404;return;}
            Interlocked.Increment(ref requests);using var request=new HttpRequestMessage(new HttpMethod(context.Request.Method),path);
            if(context.Request.Headers.TryGetValue("Authorization",out var auth))request.Headers.TryAddWithoutValidation("Authorization",auth.ToArray());
            if(context.Request.Method=="POST"){
                using var bytes=new MemoryStream();await context.Request.Body.CopyToAsync(bytes,context.RequestAborted);if(bytes.Length>16384){context.Response.StatusCode=413;return;}
                request.Content=new ByteArrayContent(bytes.ToArray());request.Content.Headers.ContentType=new MediaTypeHeaderValue("application/json");}
            using var response=await(offline?off:client).SendAsync(request,context.RequestAborted);context.Response.StatusCode=(int)response.StatusCode;
            context.Response.ContentType=response.Content.Headers.ContentType?.ToString();await response.Content.CopyToAsync(context.Response.Body,context.RequestAborted);
        });
        await bridge.StartAsync();
        var start=new ProcessStartInfo("node"){WorkingDirectory=Directory.GetCurrentDirectory(),UseShellExecute=false,RedirectStandardInput=true,RedirectStandardOutput=true,RedirectStandardError=true,CreateNoWindow=true};
        start.ArgumentList.Add(Path.GetFullPath("QA/tools/penta-teacher-chat-live.cjs"));start.ArgumentList.Add(bridge.Urls.Single());start.ArgumentList.Add(manifest.RunId.ToString("N"));
        using var process=Process.Start(start)??throw new InvalidOperationException("Teacher chat browser could not start.");
        await process.StandardInput.WriteAsync(JsonSerializer.Serialize(new{academy,token,teacher}));process.StandardInput.Close();var output=process.StandardOutput.ReadToEndAsync();var error=process.StandardError.ReadToEndAsync();
        try{using var timeout=new CancellationTokenSource(TimeSpan.FromMinutes(5));await process.WaitForExitAsync(timeout.Token);}catch{if(!process.HasExited)process.Kill(entireProcessTree:true);throw;}
        Console.Write(await output);if(process.ExitCode!=0)throw new InvalidOperationException("Teacher chat browser failed: "+await error);await bridge.StopAsync();
        if(before!=await Unrelated())throw new InvalidOperationException("Teacher chat changed unrelated protected state.");
        using(var scope=factory.Services.CreateScope()){
            var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();var rows=await db.PentaOnboardingDrafts.AsNoTracking().Where(x=>x.ActorUserId==actor).ToArrayAsync();
            var teachers=await db.Teachers.AsNoTracking().Where(x=>x.AcademyId==academy).ToArrayAsync();var audits=await db.AuditLogs.AsNoTracking().Where(x=>x.ActorUserId==actor&&x.Action=="PentaTeacherDraftExecuted").ToArrayAsync();
            if(teachers.Length!=1||teachers[0].FirstName!="TeacherChatSynthetic"||teachers[0].LastName!="chat-light"||audits.Length!=1||rows.Length!=3||rows.Count(x=>x.Status=="Succeeded")!=1||rows.Count(x=>x.Status=="Reviewed")!=1||rows.Count(x=>x.Status=="Cancelled")!=1||
                await db.PentaMiniSessions.CountAsync(x=>x.ActorUserId==actor)!=3||await db.PentaMiniTurns.CountAsync(x=>x.ActorUserId==actor)!=9||planner.Calls!=6)throw new InvalidOperationException("Teacher chat exact SQL postconditions failed.");
            var dir=Path.GetFullPath(Path.Combine("QA","EVIDENCE","penta-teacher-chat-live-"+manifest.RunId.ToString("N")));
            await File.WriteAllTextAsync(Path.Combine(dir,"sql-postconditions.json"),JsonSerializer.Serialize(new{passed=true,createdTeachers=1,createdAccounts=0,executionAudits=1,startedSessions=3,turnRows=9,plannerCalls=6,
                succeededDrafts=1,reviewedDrafts=1,cancelledDrafts=1,unrelatedStateUnchanged=true,actualHttpRequests=requests,modelInference=false,syntheticInterpreter=true,physicalDevices="NOT RUN"},new JsonSerializerOptions{WriteIndented=true}));
        }
        Console.WriteLine("PASS: PENTATEACHERCHATBROWSER five actual browser/SQL journeys; one explicitly approved teacher; lost prompt GET-only recovery; no actual model inference.");
    }
}
