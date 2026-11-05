using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
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
    // Test-controller completion is outside the application/browser channel.
    // Actual Identity/claim/completion/result APIs and SQL; no model or API mocks.
    private static async Task VerifyPentaOnboardingPendingBrowserAsync(QaRunManifest manifest)
    {
        using var factory=new QaApiFactory(manifest,new Dictionary<string,string?>{
            ["Penta:OperationalDraftContractsEnabled"]="true",["Penta:OperationalOnboardingHttpContractsEnabled"]="true",
            ["Penta:OperationalOnboardingExecutionContractsEnabled"]="true"});
        using var client=factory.CreateClient();Guid academy;
        using(var scope=factory.Services.CreateScope()){var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();var row=new Academy{Name="Synthetic Pending Browser "+manifest.RunId.ToString("N")};db.Academies.Add(row);await db.SaveChangesAsync();academy=row.Id;}
        const string email="qa-onboarding-pending-browser@example.invalid";var actor=await CreateAccessActorAsync(factory,email,"AcademyAdmin",academy);
        var bearer=await LoginAsync(client,email,"Synthetic!39Ab");
        client.DefaultRequestHeaders.Authorization=new AuthenticationHeaderValue("Bearer",bearer);
        var fixtures=new List<object>();var completions=new Dictionary<Guid,(Guid Draft,Guid Request,JsonElement Envelope)>();
        foreach(var name in new[]{"light-mobile","dark-desktop"}){
            var session=new PentaMiniSession{AcademyId=academy,ActorUserId=actor,Version=0,ExpiresAtUtc=DateTime.UtcNow.AddHours(1),ProtectedState="Synthetic pending completion browser; no inference"};
            using(var scope=factory.Services.CreateScope()){var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();db.PentaMiniSessions.Add(session);await db.SaveChangesAsync();}
            var draft=Guid.NewGuid();var route=$"/api/academies/{academy}/penta/onboarding/conversations/{session.Id}";
            var fields=new Dictionary<string,object>{["StudentFirstName"]=new{kind="text",value="Synthetic Pending"}};
            using var save=await client.PutAsJsonAsync(route+$"/drafts/{draft}",new{expectedRevision=0,contextVersion=0,fields});
            if((int)save.StatusCode!=201)throw new InvalidOperationException("Pending browser draft setup failed.");
            var saved=await save.Content.ReadFromJsonAsync<JsonElement>();
            using var selected=await client.PostAsJsonAsync(route+"/selection",new{requestId=Guid.NewGuid(),expectedVersion=0,draftId=draft,draftRevision=1,payloadHash=saved.GetProperty("payloadHash").GetString()});
            if((int)selected.StatusCode!=200)throw new InvalidOperationException("Pending browser selection setup failed.");
            var current=await selected.Content.ReadFromJsonAsync<JsonElement>();var requestId=Guid.NewGuid();
            var claim=new{requestId,expectedVersion=1,draftRevision=1,payloadHash=saved.GetProperty("payloadHash").GetString(),text="Synthetic pending claim; no model interpretation"};
            using var admitted=await client.PostAsJsonAsync(route+"/claims",claim);
            if((int)admitted.StatusCode!=202)throw new InvalidOperationException("Pending browser admission failed.");
            var decision=new{request_ref=requestId.ToString("D"),scope=current.GetProperty("snapshot").GetProperty("scope"),base_revision=1,catalog_revision=1,kind="clarify",
                candidates=new[]{new{tool_key=PentaOnboardingDraftService.Tool,edit=new{mode="replace",set=fields,remove=Array.Empty<string>()}}},questions=new[]{"StudentLastName","DateOfBirth"},invalidates_review=true};
            completions.Add(session.Id,(draft,requestId,JsonSerializer.SerializeToElement(new{claim,decision})));
            fixtures.Add(new{name,conversation=session.Id,draft,requestId});
        }
        var sessions=completions.Keys.ToArray();var draftIds=completions.Values.Select(x=>x.Draft).ToArray();var requests=completions.Values.Select(x=>x.Request).ToArray();
        async Task<string> Unrelated(){using var scope=factory.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();var identity=scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            return JsonSerializer.Serialize(new{students=await db.Students.AsNoTracking().OrderBy(x=>x.Id).ToArrayAsync(),guardians=await db.Guardians.AsNoTracking().OrderBy(x=>x.Id).ToArrayAsync(),links=await db.StudentGuardians.AsNoTracking().OrderBy(x=>x.Id).ToArrayAsync(),
                invoices=await db.Invoices.AsNoTracking().OrderBy(x=>x.Id).ToArrayAsync(),payments=await db.Payments.AsNoTracking().OrderBy(x=>x.Id).ToArrayAsync(),users=await identity.Users.AsNoTracking().OrderBy(x=>x.Id).ToArrayAsync(),roles=await identity.UserRoles.AsNoTracking().OrderBy(x=>x.UserId).ThenBy(x=>x.RoleId).ToArrayAsync(),
                academies=await db.Academies.AsNoTracking().OrderBy(x=>x.Id).ToArrayAsync(),branches=await db.Branches.AsNoTracking().OrderBy(x=>x.Id).ToArrayAsync(),
                sessions=await db.PentaMiniSessions.AsNoTracking().Where(x=>!sessions.Contains(x.Id)).OrderBy(x=>x.Id).ToArrayAsync(),drafts=await db.PentaOnboardingDrafts.AsNoTracking().Where(x=>!draftIds.Contains(x.Id)).OrderBy(x=>x.Id).ToArrayAsync(),
                turns=await db.PentaMiniTurns.AsNoTracking().Where(x=>!requests.Contains(x.RequestId)).OrderBy(x=>x.Id).ToArrayAsync()});}
        var before=await Unrelated();Guid[] oldAudits;using(var scope=factory.Services.CreateScope())oldAudits=await scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>().AuditLogs.Select(x=>x.Id).ToArrayAsync();
        var completed=new HashSet<Guid>();var pendingObserved=new HashSet<Guid>();var controllerSecret=Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(32));
        client.DefaultRequestHeaders.Authorization=null;
        var builder=WebApplication.CreateSlimBuilder();builder.Logging.ClearProviders();builder.WebHost.UseUrls("http://127.0.0.1:0");await using var bridge=builder.Build();
        bridge.Run(async context=>{
            if(context.Connection.RemoteIpAddress is null||!IPAddress.IsLoopback(context.Connection.RemoteIpAddress)||context.Request.Host.Host!="127.0.0.1"){context.Response.StatusCode=400;return;}
            var path=context.Request.Path.Value??"";
            if(path.StartsWith("/qa/complete/",StringComparison.Ordinal)){
                if(context.Request.Method!="POST"||context.Request.ContentLength is >0||context.Request.Headers["X-QA-Control"]!=controllerSecret||!Guid.TryParseExact(path[13..],"D",out var session)||!completions.TryGetValue(session,out var item)||!pendingObserved.Contains(session)||!completed.Add(session)){context.Response.StatusCode=403;return;}
                // Recheck the durable pending state BEFORE the single completion.
                using(var scope=factory.Services.CreateScope()){var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();var row=await db.PentaMiniSessions.AsNoTracking().SingleAsync(x=>x.Id==session);var draft=await db.PentaOnboardingDrafts.AsNoTracking().SingleAsync(x=>x.Id==item.Draft);
                    if(row.PendingRequestId!=item.Request||draft.Revision!=1||draft.Status!="AwaitingReview"||await db.AuditLogs.AnyAsync(x=>x.EntityId==session&&x.Action=="PentaOnboardingConversationCompleted"))throw new InvalidOperationException("Pending browser changed before explicit test-controller completion.");}
                using var completion=new HttpRequestMessage(HttpMethod.Post,$"/api/academies/{academy}/penta/onboarding/conversations/{session}/claims/{item.Request}/complete"){Content=JsonContent.Create(item.Envelope)};
                completion.Headers.Authorization=new AuthenticationHeaderValue("Bearer",bearer);using var response=await client.SendAsync(completion,context.RequestAborted);
                if((int)response.StatusCode!=201)throw new InvalidOperationException("Pending browser exact completion failed: "+(int)response.StatusCode);
                context.Response.StatusCode=204;return;
            }
            var prefix=$"/api/academies/{academy}/penta/onboarding/conversations/";
            if(context.Request.Method!="GET"||!(path=="/api/auth/session"||path.StartsWith(prefix,StringComparison.Ordinal)&&System.Text.RegularExpressions.Regex.IsMatch(path,@"/(current|drafts/[0-9a-f-]{36}|claims/[0-9a-f-]{36}/result)$"))){context.Response.StatusCode=405;return;}
            using var request=new HttpRequestMessage(HttpMethod.Get,path);if(context.Request.Headers.TryGetValue("Authorization",out var auth))request.Headers.TryAddWithoutValidation("Authorization",auth.ToArray());
            using var result=await client.SendAsync(request,context.RequestAborted);
            if((int)result.StatusCode==202&&path.EndsWith("/result",StringComparison.Ordinal)){foreach(var entry in completions)if(path==$"{prefix}{entry.Key}/claims/{entry.Value.Request}/result")pendingObserved.Add(entry.Key);}
            context.Response.StatusCode=(int)result.StatusCode;context.Response.ContentType=result.Content.Headers.ContentType?.ToString();await result.Content.CopyToAsync(context.Response.Body,context.RequestAborted);
        });await bridge.StartAsync();
        var start=new ProcessStartInfo("node"){WorkingDirectory=Directory.GetCurrentDirectory(),UseShellExecute=false,RedirectStandardInput=true,RedirectStandardOutput=true,RedirectStandardError=true,CreateNoWindow=true};start.ArgumentList.Add(Path.GetFullPath("QA/tools/penta-onboarding-pending-live.cjs"));start.ArgumentList.Add(bridge.Urls.Single());start.ArgumentList.Add(manifest.RunId.ToString("N"));
        using var process=Process.Start(start)??throw new InvalidOperationException("Pending browser did not start.");await process.StandardInput.WriteAsync(JsonSerializer.Serialize(new{fixtures,token=bearer,controllerSecret}));process.StandardInput.Close();var stdout=process.StandardOutput.ReadToEndAsync();var stderr=process.StandardError.ReadToEndAsync();
        try{using var timeout=new CancellationTokenSource(TimeSpan.FromMinutes(3));await process.WaitForExitAsync(timeout.Token);}catch{if(!process.HasExited)process.Kill(entireProcessTree:true);throw;}
        Console.Write(await stdout);if(process.ExitCode!=0)throw new InvalidOperationException("Pending browser failed: "+await stderr);await bridge.StopAsync();
        if(completed.Count!=2||pendingObserved.Count!=2||before!=await Unrelated())throw new InvalidOperationException("Pending browser did not preserve unrelated data or complete both exact claims.");
        using(var scope=factory.Services.CreateScope()){var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();var rows=await db.PentaMiniSessions.AsNoTracking().Where(x=>sessions.Contains(x.Id)).ToArrayAsync();var drafts=await db.PentaOnboardingDrafts.AsNoTracking().Where(x=>draftIds.Contains(x.Id)).ToArrayAsync();var turns=await db.PentaMiniTurns.AsNoTracking().Where(x=>requests.Contains(x.RequestId)).ToArrayAsync();var audits=await db.AuditLogs.AsNoTracking().Where(x=>!oldAudits.Contains(x.Id)).ToArrayAsync();
            var expected=new Dictionary<string,int>{["PentaOnboardingConversationRead"]=12,["PentaOnboardingConversationResultRead"]=4,["PentaOnboardingDraftRead"]=2,["PentaOnboardingConversationCompleted"]=2,["PentaOnboardingDraftCorePartialSaved"]=2};
            if(rows.Length!=2||rows.Any(x=>x.PendingRequestId!=null||x.Version!=0||x.AcademyId!=academy||x.ActorUserId!=actor)||drafts.Length!=2||drafts.Any(x=>x.Revision!=2||x.Status!="AwaitingReview"||x.AcademyId!=academy||x.ActorUserId!=actor)||turns.Length!=2||turns.Any(x=>x.Status!="OnboardingCompleted"||x.ProtectedReceipt==null)||audits.Length!=22||expected.Any(x=>audits.Count(a=>a.Action==x.Key)!=x.Value)||audits.Any(x=>x.AcademyId!=academy||x.ActorUserId!=actor))throw new InvalidOperationException("Pending browser exact completion/read audit postconditions differ.");
            var directory=Path.GetFullPath(Path.Combine("QA","EVIDENCE","penta-onboarding-pending-live-"+manifest.RunId.ToString("N")));Directory.CreateDirectory(directory);await File.WriteAllTextAsync(Path.Combine(directory,"sql-postconditions.json"),JsonSerializer.Serialize(new{run=manifest.RunId.ToString("N"),passed=true,scenarios=2,pending202Observed=2,completedSameClaims=2,partialRevisions=2,exactAudits=22,studentsCreated=0,browserWrites=0,inference=false,unrelatedDomainAndIdentityUnchanged=true,
                audits=audits.GroupBy(x=>x.Action).OrderBy(x=>x.Key).Select(x=>new{action=x.Key,count=x.Count()}).ToArray(),physicalDevices="NOT RUN"},new JsonSerializerOptions{WriteIndented=true}));}
        Console.WriteLine("PASS: PENTAONBOARDINGPENDING 2 actual browser/SQL pending202 to same-claim completion journeys;22exact audits;browser GET only/no domain writes/inference/429.");
    }
}
