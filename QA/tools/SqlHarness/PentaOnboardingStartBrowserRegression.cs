using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
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
    private static async Task VerifyPentaOnboardingStartBrowserAsync(QaRunManifest manifest)
    {
        var flags=new Dictionary<string,string?> { ["Penta:OperationalDraftContractsEnabled"]="true",
            ["Penta:OperationalOnboardingHttpContractsEnabled"]="true",["Penta:OperationalOnboardingExecutionContractsEnabled"]="true",
            ["Penta:OperationalOnboardingGatewayContractsEnabled"]="true" };
        var planner=new OnboardingScriptedInterpreter();planner.Handler=(_,_)=>throw new InvalidOperationException("Start browser must not interpret.");
        using var factory=new QaApiFactory(manifest,flags,isolatedOnboardingInterpreter:planner);
        using var offline=new QaApiFactory(manifest,flags);using var client=factory.CreateClient();using var unavailable=offline.CreateClient();
        // Retained regressions deliberately consume Academy A's daily draft
        // allowance. A separate run-owned academy isolates this journey without
        // deleting fixtures, shifting dates or changing/bypassing any quota.
        Guid academy;using(var scope=factory.Services.CreateScope()) {
            var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var row=new Academy{Name="Synthetic Start Browser "+manifest.RunId.ToString("N")};
            db.Academies.Add(row);await db.SaveChangesAsync();academy=row.Id;
        }
        const string email="qa-onboarding-start-browser@example.invalid";var actor=await CreateAccessActorAsync(factory,email,"AcademyAdmin",academy);
        var token=await LoginAsync(client,email,"Synthetic!39Ab");var teacher=await LoginAsync(client,"qa-teacher-a@example.invalid","Synthetic!39Ab");
        async Task<string> Snapshot(){using var scope=factory.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();var identity=scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            return JsonSerializer.Serialize(new{students=await db.Students.AsNoTracking().OrderBy(x=>x.Id).ToArrayAsync(),guardians=await db.Guardians.AsNoTracking().OrderBy(x=>x.Id).ToArrayAsync(),
                links=await db.StudentGuardians.AsNoTracking().OrderBy(x=>x.Id).ToArrayAsync(),invoices=await db.Invoices.AsNoTracking().OrderBy(x=>x.Id).ToArrayAsync(),payments=await db.Payments.AsNoTracking().OrderBy(x=>x.Id).ToArrayAsync(),
                users=await identity.Users.AsNoTracking().OrderBy(x=>x.Id).ToArrayAsync(),roles=await identity.UserRoles.AsNoTracking().OrderBy(x=>x.UserId).ThenBy(x=>x.RoleId).ToArrayAsync(),
                academies=await db.Academies.AsNoTracking().OrderBy(x=>x.Id).ToArrayAsync(),branches=await db.Branches.AsNoTracking().OrderBy(x=>x.Id).ToArrayAsync(),
                sessions=await db.PentaMiniSessions.AsNoTracking().Where(x=>x.ActorUserId!=actor).OrderBy(x=>x.Id).ToArrayAsync(),
                drafts=await db.PentaOnboardingDrafts.AsNoTracking().Where(x=>x.ActorUserId!=actor).OrderBy(x=>x.Id).ToArrayAsync(),turns=await db.PentaMiniTurns.AsNoTracking().Where(x=>x.ActorUserId!=actor).OrderBy(x=>x.Id).ToArrayAsync()});}
        var before=await Snapshot();Guid[] beforeAudits;using(var scope=factory.Services.CreateScope())beforeAudits=await scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>().AuditLogs.Select(x=>x.Id).ToArrayAsync();
        var builder=WebApplication.CreateSlimBuilder();builder.Logging.ClearProviders();builder.WebHost.UseUrls("http://127.0.0.1:0");await using var bridge=builder.Build();
        bridge.Run(async context=>{
            var path=context.Request.Path.Value??"";var offlineRequest=path.StartsWith("/offline/",StringComparison.Ordinal);if(offlineRequest)path=path[8..];
            var prefix=$"/api/academies/{academy}/penta/onboarding";
            var read=context.Request.Method=="GET"&&(path=="/api/auth/session"||path==prefix+"/readiness"||
                System.Text.RegularExpressions.Regex.IsMatch(path,@"^"+prefix+@"/(starts/[0-9a-f-]{36}|conversations/[0-9a-f-]{36}/(current|drafts/[0-9a-f-]{36}))$"));
            var post=context.Request.Method=="POST"&&path==prefix+"/conversations";
            if(context.Connection.RemoteIpAddress is null||!IPAddress.IsLoopback(context.Connection.RemoteIpAddress)||context.Request.Host.Host!="127.0.0.1"||!(read||post)||context.Request.ContentLength>1024){context.Response.StatusCode=405;return;}
            using var request=new HttpRequestMessage(new HttpMethod(context.Request.Method),path);
            if(context.Request.Headers.TryGetValue("Authorization",out var authorization))request.Headers.TryAddWithoutValidation("Authorization",authorization.ToArray());
            if(post){using var buffer=new MemoryStream();await context.Request.Body.CopyToAsync(buffer,context.RequestAborted);if(buffer.Length>1024){context.Response.StatusCode=413;return;}
                request.Content=new ByteArrayContent(buffer.ToArray());request.Content.Headers.ContentType=new MediaTypeHeaderValue("application/json");}
            using var response=await(offlineRequest?unavailable:client).SendAsync(request,context.RequestAborted);
            context.Response.StatusCode=(int)response.StatusCode;context.Response.ContentType=response.Content.Headers.ContentType?.ToString();await response.Content.CopyToAsync(context.Response.Body,context.RequestAborted);
        });
        await bridge.StartAsync();
        var start=new ProcessStartInfo("node"){WorkingDirectory=Directory.GetCurrentDirectory(),UseShellExecute=false,RedirectStandardInput=true,RedirectStandardOutput=true,RedirectStandardError=true,CreateNoWindow=true};
        start.ArgumentList.Add(Path.GetFullPath("QA/tools/penta-onboarding-start-live.cjs"));start.ArgumentList.Add(bridge.Urls.Single());start.ArgumentList.Add(manifest.RunId.ToString("N"));
        using var process=Process.Start(start)??throw new InvalidOperationException("Start browser failed to start.");
        await process.StandardInput.WriteAsync(JsonSerializer.Serialize(new{token,teacher,academy}));process.StandardInput.Close();var stdout=process.StandardOutput.ReadToEndAsync();var stderr=process.StandardError.ReadToEndAsync();
        try{using var timeout=new CancellationTokenSource(TimeSpan.FromMinutes(4));await process.WaitForExitAsync(timeout.Token);}catch{if(!process.HasExited)process.Kill(entireProcessTree:true);throw;}
        Console.Write(await stdout);var error=await stderr;if(process.ExitCode!=0)throw new InvalidOperationException("Start browser failed: "+error);await bridge.StopAsync();
        if(before!=await Snapshot()||planner.Calls!=0)throw new InvalidOperationException("Start browser changed unrelated state or invoked interpreter.");
        using(var scope=factory.Services.CreateScope()){var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var sessions=await db.PentaMiniSessions.AsNoTracking().Where(x=>x.ActorUserId==actor).ToArrayAsync();var drafts=await db.PentaOnboardingDrafts.AsNoTracking().Where(x=>x.ActorUserId==actor).ToArrayAsync();
            var turns=await db.PentaMiniTurns.AsNoTracking().Where(x=>x.ActorUserId==actor).ToArrayAsync();var audits=await db.AuditLogs.AsNoTracking().Where(x=>!beforeAudits.Contains(x.Id)).ToArrayAsync();
            if(sessions.Length!=3||drafts.Length!=3||turns.Length!=3||sessions.Any(x=>x.Version!=0||x.PendingRequestId!=null||x.AcademyId!=academy||x.ProtectedOnboardingState==null)||
                drafts.Any(x=>x.Revision!=1||x.Status!="AwaitingReview"||x.AcademyId!=academy)||turns.Any(x=>x.Status!="OnboardingStarted"||x.AcademyId!=academy)||
                audits.Count(x=>x.Action=="PentaOnboardingConversationStarted")!=3||audits.Count(x=>x.Action=="PentaOnboardingDraftSaved")!=3||
                audits.Any(x=>x.AcademyId!=academy||x.ActorUserId!=actor||x.Action=="PentaOnboardingExecuted"))throw new InvalidOperationException("Start browser exact postconditions failed.");
            var evidence=Path.GetFullPath(Path.Combine("QA","EVIDENCE","penta-onboarding-start-live-"+manifest.RunId.ToString("N")));Directory.CreateDirectory(evidence);
            await File.WriteAllTextAsync(Path.Combine(evidence,"sql-postconditions.json"),JsonSerializer.Serialize(new{run=manifest.RunId.ToString("N"),passed=true,scenarios=5,
                sessions=3,drafts=3,startMarkers=3,startAudits=3,studentsCreated=0,interpreterCalls=0,unrelatedStateUnchanged=true,
                audits=audits.GroupBy(x=>x.Action).OrderBy(x=>x.Key).Select(x=>new{action=x.Key,count=x.Count()}).ToArray(),physicalDevices="NOT RUN"},new JsonSerializerOptions{WriteIndented=true}));}
        Console.WriteLine("PASS: PENTAONBOARDINGSTARTBROWSER 5 actual browser/SQL scenarios;3 explicit sessions/empty drafts;lost response recovered GET-only;offline/Teacher unavailable;no student/inference/429.");
    }
}
