using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Intelligence.Penta;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

internal static partial class SqlHarnessEntryPoint
{
    // These deterministic responses are deliberately QA source, never a real
    // model adapter, prompt router or production interpreter registration.
    private sealed class DialogueSyntheticInterpreter : IPentaOnboardingSyntheticInterpreter
    {
        public int Calls;
        public Task<JsonElement> InterpretAsync(PentaOnboardingInterpretation input,CancellationToken token)
        {
            token.ThrowIfCancellationRequested();Interlocked.Increment(ref Calls);
            var full=input.Text=="SYNTHETIC complete student details";
            if(!full&&input.Text!="SYNTHETIC provide first name")throw new InvalidOperationException("Unexpected synthetic test prompt.");
            var fields=new Dictionary<string,object>{["StudentFirstName"]=new{kind="text",value="Synthetic"}};
            if(full){fields["StudentLastName"]=new{kind="text",value="Dialogue "+input.Snapshot.GetProperty("draft").GetProperty("ref").GetString()};fields["DateOfBirth"]=new{kind="date",value="2000-01-01"};}
            return Task.FromResult(JsonSerializer.SerializeToElement(new{request_ref=input.RequestId.ToString("D"),scope=input.Snapshot.GetProperty("scope"),
                base_revision=input.Snapshot.GetProperty("draft").GetProperty("revision").GetInt64(),catalog_revision=1,kind=full?"host_prepare":"clarify",
                candidates=new[]{new{tool_key=PentaOnboardingDraftService.Tool,edit=new{mode="replace",set=fields,remove=Array.Empty<string>()}}},
                questions=full?Array.Empty<string>():new[]{"StudentLastName","DateOfBirth"},invalidates_review=true}));
        }
    }
    private static async Task VerifyPentaOnboardingDialogueBrowserAsync(QaRunManifest manifest)
    {
        var flags=new Dictionary<string,string?>{["Penta:OperationalDraftContractsEnabled"]="true",["Penta:OperationalOnboardingHttpContractsEnabled"]="true",
            ["Penta:OperationalOnboardingExecutionContractsEnabled"]="true",["Penta:OperationalOnboardingGatewayContractsEnabled"]="true",["Penta:OperationalOnboardingBrowserContractsEnabled"]="true"};
        var planner=new DialogueSyntheticInterpreter();var unmarked=new OnboardingScriptedInterpreter{Handler=(_,_)=>throw new InvalidOperationException("Unmarked interpreter must not run.")};
        using var factory=new QaApiFactory(manifest,flags,isolatedOnboardingInterpreter:planner);
        using var offline=new QaApiFactory(manifest,flags,isolatedOnboardingInterpreter:unmarked);
        using var disabled=new QaApiFactory(manifest,new Dictionary<string,string?>(flags){["Penta:OperationalOnboardingBrowserContractsEnabled"]="false"},isolatedOnboardingInterpreter:planner);
        using var client=factory.CreateClient();using var unavailable=offline.CreateClient();using var off=disabled.CreateClient();
        Guid academy;using(var scope=factory.Services.CreateScope()){var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();var row=new Academy{Name="Synthetic Dialogue Browser "+manifest.RunId.ToString("N")};db.Academies.Add(row);await db.SaveChangesAsync();academy=row.Id;}
        const string email="qa-onboarding-dialogue-browser@example.invalid";var actor=await CreateAccessActorAsync(factory,email,"AcademyAdmin",academy);
        await CreateAccessActorAsync(factory,"qa-onboarding-dialogue-teacher@example.invalid","Teacher",academy);
        await CreateAccessActorAsync(factory,"qa-onboarding-dialogue-other@example.invalid","AcademyAdmin",academy);
        var token=await LoginAsync(client,email,"Synthetic!39Ab");
        var teacher=await LoginAsync(client,"qa-onboarding-dialogue-teacher@example.invalid","Synthetic!39Ab");
        var other=await LoginAsync(client,"qa-onboarding-dialogue-other@example.invalid","Synthetic!39Ab");
        var foreign=await LoginAsync(client,"qa-admin-a@example.invalid","Synthetic!39Ab");
        Guid[] oldStudents,beforeAudits;using(var scope=factory.Services.CreateScope()){var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();oldStudents=await db.Students.Select(x=>x.Id).ToArrayAsync();beforeAudits=await db.AuditLogs.Select(x=>x.Id).ToArrayAsync();}
        async Task<string> Snapshot(){using var scope=factory.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();var identity=scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            return JsonSerializer.Serialize(new{students=await db.Students.AsNoTracking().Where(x=>oldStudents.Contains(x.Id)).OrderBy(x=>x.Id).ToArrayAsync(),
                guardians=await db.Guardians.AsNoTracking().OrderBy(x=>x.Id).ToArrayAsync(),links=await db.StudentGuardians.AsNoTracking().OrderBy(x=>x.Id).ToArrayAsync(),
                invoices=await db.Invoices.AsNoTracking().OrderBy(x=>x.Id).ToArrayAsync(),payments=await db.Payments.AsNoTracking().OrderBy(x=>x.Id).ToArrayAsync(),
                users=await identity.Users.AsNoTracking().OrderBy(x=>x.Id).ToArrayAsync(),roles=await identity.UserRoles.AsNoTracking().OrderBy(x=>x.UserId).ThenBy(x=>x.RoleId).ToArrayAsync(),
                academies=await db.Academies.AsNoTracking().OrderBy(x=>x.Id).ToArrayAsync(),branches=await db.Branches.AsNoTracking().OrderBy(x=>x.Id).ToArrayAsync(),
                sessions=await db.PentaMiniSessions.AsNoTracking().Where(x=>x.ActorUserId!=actor).OrderBy(x=>x.Id).ToArrayAsync(),drafts=await db.PentaOnboardingDrafts.AsNoTracking().Where(x=>x.ActorUserId!=actor).OrderBy(x=>x.Id).ToArrayAsync(),
                turns=await db.PentaMiniTurns.AsNoTracking().Where(x=>x.ActorUserId!=actor).OrderBy(x=>x.Id).ToArrayAsync()});}
        var before=await Snapshot();
        var builder=WebApplication.CreateSlimBuilder();builder.Logging.ClearProviders();builder.WebHost.UseUrls("http://127.0.0.1:0");await using var bridge=builder.Build();
        bridge.Run(async context=>{
            var path=context.Request.Path.Value??"";var offlineRequest=path.StartsWith("/offline/",StringComparison.Ordinal);if(offlineRequest)path=path[8..];
            var prefix=$"/api/academies/{academy}/penta/onboarding";
            var scoped=System.Text.RegularExpressions.Regex.IsMatch(path,@"^"+prefix+@"/conversations/[0-9a-f-]{36}/");
            var read=context.Request.Method=="GET"&&(path=="/api/auth/session"||path==prefix+"/readiness"||scoped&&System.Text.RegularExpressions.Regex.IsMatch(path,@"/(current|send-readiness|drafts/[0-9a-f-]{36}|claims/[0-9a-f-]{36}/result)$"));
            var post=context.Request.Method=="POST"&&(path==prefix+"/conversations"||scoped&&System.Text.RegularExpressions.Regex.IsMatch(path,@"/(browser-turns|drafts/[0-9a-f-]{36}/core/(challenge|confirm|execute|operation|status))$"));
            if(context.Connection.RemoteIpAddress is null||!IPAddress.IsLoopback(context.Connection.RemoteIpAddress)||context.Request.Host.Host!="127.0.0.1"||!(read||post)||context.Request.ContentLength>16384){context.Response.StatusCode=405;return;}
            using var request=new HttpRequestMessage(new HttpMethod(context.Request.Method),path);if(context.Request.Headers.TryGetValue("Authorization",out var authorization))request.Headers.TryAddWithoutValidation("Authorization",authorization.ToArray());
            if(post){using var buffer=new MemoryStream();await context.Request.Body.CopyToAsync(buffer,context.RequestAborted);if(buffer.Length>16384){context.Response.StatusCode=413;return;}request.Content=new ByteArrayContent(buffer.ToArray());request.Content.Headers.ContentType=new MediaTypeHeaderValue("application/json");}
            using var response=await(offlineRequest?unavailable:client).SendAsync(request,context.RequestAborted);context.Response.StatusCode=(int)response.StatusCode;context.Response.ContentType=response.Content.Headers.ContentType?.ToString();await response.Content.CopyToAsync(context.Response.Body,context.RequestAborted);
        });await bridge.StartAsync();
        var start=new ProcessStartInfo("node"){WorkingDirectory=Directory.GetCurrentDirectory(),UseShellExecute=false,RedirectStandardInput=true,RedirectStandardOutput=true,RedirectStandardError=true,CreateNoWindow=true};
        start.ArgumentList.Add(Path.GetFullPath("QA/tools/penta-onboarding-dialogue-live.cjs"));start.ArgumentList.Add(bridge.Urls.Single());start.ArgumentList.Add(manifest.RunId.ToString("N"));
        using var process=Process.Start(start)??throw new InvalidOperationException("Dialogue browser failed to start.");await process.StandardInput.WriteAsync(JsonSerializer.Serialize(new{token}));process.StandardInput.Close();var stdout=process.StandardOutput.ReadToEndAsync();var stderr=process.StandardError.ReadToEndAsync();
        try{using var timeout=new CancellationTokenSource(TimeSpan.FromMinutes(4));await process.WaitForExitAsync(timeout.Token);}catch{if(!process.HasExited)process.Kill(entireProcessTree:true);throw;}
        Console.Write(await stdout);if(process.ExitCode!=0)throw new InvalidOperationException("Dialogue browser failed: "+await stderr);await bridge.StopAsync();
        if(before!=await Snapshot()||planner.Calls!=4||unmarked.Calls!=0)throw new InvalidOperationException("Dialogue browser changed unrelated state or interpretation count.");
        // Independent HTTP denials use the actual saved session, never browser
        // permission flags. Every denial must leave all owned state/audits exact.
        Guid session;using(var scope=factory.Services.CreateScope())session=await scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>().PentaMiniSessions.Where(x=>x.ActorUserId==actor).OrderBy(x=>x.CreatedAtUtc).Select(x=>x.Id).FirstAsync();
        async Task<string> Owned(){using var scope=factory.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();return JsonSerializer.Serialize(new{sessions=await db.PentaMiniSessions.AsNoTracking().Where(x=>x.ActorUserId==actor).OrderBy(x=>x.Id).ToArrayAsync(),drafts=await db.PentaOnboardingDrafts.AsNoTracking().Where(x=>x.ActorUserId==actor).OrderBy(x=>x.Id).ToArrayAsync(),turns=await db.PentaMiniTurns.AsNoTracking().Where(x=>x.ActorUserId==actor).OrderBy(x=>x.Id).ToArrayAsync(),audits=await db.AuditLogs.AsNoTracking().OrderBy(x=>x.Id).ToArrayAsync()});}
        var guards=0;var route=$"/api/academies/{academy}/penta/onboarding/conversations/{session}";
        foreach(var(entry,expected)in new(string?,int)[]{(null,401),(teacher,403),(other,404),(foreign,403)}){
            using var caller=factory.CreateClient();if(entry is not null)caller.DefaultRequestHeaders.Authorization=new AuthenticationHeaderValue("Bearer",entry);
            foreach(var action in new[]{"send-readiness","browser-turns"}){var saved=await Owned();using var response=action=="send-readiness"?await caller.GetAsync(route+"/"+action):await caller.PostAsJsonAsync(route+"/"+action,new{requestId=Guid.NewGuid(),expectedVersion=1,draftRevision=1,payloadHash=new string('a',64),text="Synthetic denied prompt"});
                if((int)response.StatusCode!=expected||saved!=await Owned()||planner.Calls!=4)throw new InvalidOperationException("Dialogue independent permission denial failed: "+expected);guards++;}
        }
        foreach(var caller in new[]{unavailable,off}){caller.DefaultRequestHeaders.Authorization=new AuthenticationHeaderValue("Bearer",token);var saved=await Owned();using var response=await caller.PostAsJsonAsync(route+"/browser-turns",new{});if((int)response.StatusCode!=503||saved!=await Owned()||planner.Calls!=4||unmarked.Calls!=0)throw new InvalidOperationException("Unqualified/default browser admission bypass.");guards++;}
        using(var scope=factory.Services.CreateScope()){var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();var students=await db.Students.AsNoTracking().Where(x=>!oldStudents.Contains(x.Id)).ToArrayAsync();
            var sessions=await db.PentaMiniSessions.AsNoTracking().Where(x=>x.ActorUserId==actor).ToArrayAsync();var drafts=await db.PentaOnboardingDrafts.AsNoTracking().Where(x=>x.ActorUserId==actor).ToArrayAsync();var turns=await db.PentaMiniTurns.AsNoTracking().Where(x=>x.ActorUserId==actor).ToArrayAsync();var audits=await db.AuditLogs.AsNoTracking().Where(x=>!beforeAudits.Contains(x.Id)).ToArrayAsync();
            if(guards!=10||students.Length!=1||students[0].AcademyId!=academy||students[0].FirstName!="Synthetic"||sessions.Length!=3||sessions.Any(x=>x.AcademyId!=academy||x.PendingRequestId!=null)||drafts.Length!=3||drafts.Count(x=>x.Status=="Succeeded"&&x.Revision==3)!=1||drafts.Count(x=>x.Status=="Prepared"&&x.Revision==3)!=1||drafts.Count(x=>x.Status=="AwaitingReview"&&x.Revision==1)!=1||
                turns.Length!=7||turns.Count(x=>x.Status=="OnboardingStarted")!=3||audits.Any(x=>x.AcademyId!=academy||x.ActorUserId!=actor)||audits.Count(x=>x.Action=="PentaOnboardingExecuted")!=1||audits.Count(x=>x.Action=="PentaOnboardingConversationClaimed")!=4||audits.Count(x=>x.Action=="PentaOnboardingConversationCompleted")!=4||audits.Count(x=>x.Action=="PentaOnboardingDraftCorePartialSaved")!=2||audits.Count(x=>x.Action=="PentaOnboardingDraftCorePrepared")!=2||audits.Count(x=>x.Action=="PentaOnboardingConversationResultRead")!=1||audits.Count(x=>x.Action=="PentaOnboardingConversationSendReadiness")!=9)
                throw new InvalidOperationException("Dialogue exact host SQL postconditions differ.");
            var evidence=Path.GetFullPath(Path.Combine("QA","EVIDENCE","penta-onboarding-dialogue-live-"+manifest.RunId.ToString("N")));Directory.CreateDirectory(evidence);
            await File.WriteAllTextAsync(Path.Combine(evidence,"sql-postconditions.json"),JsonSerializer.Serialize(new{run=manifest.RunId.ToString("N"),passed=true,scenarios=3,independentHttpGuards=guards,studentsCreated=1,executionAudits=1,
                sessions=3,drafts=3,turns=7,scriptedInterpretations=4,unmarkedInterpretations=0,requiredPartialAndPreparationAudits=4,realModelQualified=false,unrelatedStateUnchanged=true,
                audits=audits.GroupBy(x=>x.Action).OrderBy(x=>x.Key).Select(x=>new{action=x.Key,count=x.Count()}).ToArray(),physicalDevices="NOT RUN"},new JsonSerializerOptions{WriteIndented=true}));}
        Console.WriteLine("PASS: PENTAONBOARDINGDIALOGUE 3 actual browser/SQL scenarios;10 independent HTTP guards;4 scripted interpretations/one explicitly approved student; completed message GET recovery; unmarked runtime denied; no real model qualification/429.");
    }
}
