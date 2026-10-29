using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
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
    // Advance each fixture's isolated host clock through a secret-controlled
    // Node test controller. Never tamper with expiry bound into protected bodies.
    private static async Task VerifyPentaOnboardingExpiryBrowserAsync(QaRunManifest manifest)
    {
        var flags = new Dictionary<string,string?> {
            ["Penta:OperationalDraftContractsEnabled"]="true", ["Penta:OperationalOnboardingHttpContractsEnabled"]="true",
            ["Penta:OperationalOnboardingExecutionContractsEnabled"]="true" };
        using var factory = new QaApiFactory(manifest, flags);
        using var client = factory.CreateClient(); Guid academy;
        using (var scope = factory.Services.CreateScope()) {
            var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var row=new Academy {Name="Synthetic Expiry Browser "+manifest.RunId.ToString("N")};
            db.Academies.Add(row); await db.SaveChangesAsync(); academy=row.Id;
        }
        const string email="qa-onboarding-expiry-browser@example.invalid";
        var actor=await CreateAccessActorAsync(factory,email,"AcademyAdmin",academy);
        var bearer=await LoginAsync(client,email,"Synthetic!39Ab");
        client.DefaultRequestHeaders.Authorization=new AuthenticationHeaderValue("Bearer",bearer);
        var fixtures=new List<object>();
        var owned=new Dictionary<Guid,(Guid Draft, bool SessionExpiry, bool Pending, Guid? Request)>();
        foreach (var pending in new[]{false,true}) foreach (var sessionExpiry in new[]{false,true}) {
            var session=new PentaMiniSession {AcademyId=academy,ActorUserId=actor,Version=0,
                ExpiresAtUtc=DateTime.UtcNow.AddHours(1),ProtectedState="Synthetic expiry fixture; no inference"};
            using(var scope=factory.Services.CreateScope()){var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();db.PentaMiniSessions.Add(session);await db.SaveChangesAsync();}
            var draft=Guid.NewGuid(); var route=$"/api/academies/{academy}/penta/onboarding/conversations/{session.Id}";
            using var save=await client.PutAsJsonAsync(route+$"/drafts/{draft}",new{expectedRevision=0,contextVersion=0,
                fields=new Dictionary<string,object>{["StudentFirstName"]=new{kind="text",value="Synthetic Expiry Private"}}});
            if((int)save.StatusCode!=201)throw new InvalidOperationException("Expiry draft setup failed.");
            var value=await save.Content.ReadFromJsonAsync<JsonElement>();
            using var selected=await client.PostAsJsonAsync(route+"/selection",new{requestId=Guid.NewGuid(),expectedVersion=0,
                draftId=draft,draftRevision=1,payloadHash=value.GetProperty("payloadHash").GetString()});
            if((int)selected.StatusCode!=200)throw new InvalidOperationException("Expiry selection setup failed.");
            Guid? requestId=null;
            if(pending){requestId=Guid.NewGuid();using var claim=await client.PostAsJsonAsync(route+"/claims",new{
                requestId,expectedVersion=1,draftRevision=1,payloadHash=value.GetProperty("payloadHash").GetString(),text="Synthetic expiry pending fixture; no model"});
                if((int)claim.StatusCode!=202)throw new InvalidOperationException("Expiry pending admission failed.");}
            var name=(pending?"pending":"selected")+"-"+(sessionExpiry?"session":"draft");
            owned.Add(session.Id,(draft,sessionExpiry,pending,requestId));fixtures.Add(new{name,conversation=session.Id,draft,pending,
                expiresAtUtc=value.GetProperty("expiresAtUtc").GetString()});
        }
        var expired=new HashSet<Guid>();var observed=new HashSet<Guid>();var controllerSecret=Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(32));
        var clocks=owned.Keys.ToDictionary(x=>x,_=>new OnboardingDraftClock());
        var hosts=clocks.ToDictionary(x=>x.Key,x=>new QaApiFactory(manifest,flags,isolatedClock:x.Value));
        try {
        async Task<string> DomainSnapshot(){using var scope=factory.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();var identity=scope.ServiceProvider.GetRequiredService<IdentityDbContext>();return JsonSerializer.Serialize(new{
            students=await db.Students.AsNoTracking().OrderBy(x=>x.Id).ToArrayAsync(),guardians=await db.Guardians.AsNoTracking().OrderBy(x=>x.Id).ToArrayAsync(),links=await db.StudentGuardians.AsNoTracking().OrderBy(x=>x.Id).ToArrayAsync(),
            invoices=await db.Invoices.AsNoTracking().OrderBy(x=>x.Id).ToArrayAsync(),payments=await db.Payments.AsNoTracking().OrderBy(x=>x.Id).ToArrayAsync(),
            users=await identity.Users.AsNoTracking().OrderBy(x=>x.Id).ToArrayAsync(),roles=await identity.UserRoles.AsNoTracking().OrderBy(x=>x.UserId).ThenBy(x=>x.RoleId).ToArrayAsync(),
            academies=await db.Academies.AsNoTracking().OrderBy(x=>x.Id).ToArrayAsync(),branches=await db.Branches.AsNoTracking().OrderBy(x=>x.Id).ToArrayAsync(),
            sessions=await db.PentaMiniSessions.AsNoTracking().OrderBy(x=>x.Id).ToArrayAsync(),
            drafts=await db.PentaOnboardingDrafts.AsNoTracking().OrderBy(x=>x.Id).ToArrayAsync(),
            turns=await db.PentaMiniTurns.AsNoTracking().OrderBy(x=>x.Id).ToArrayAsync()});}
        var before=await DomainSnapshot();Guid[] oldAudits;
        using(var scope=factory.Services.CreateScope())oldAudits=await scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>().AuditLogs.Select(x=>x.Id).ToArrayAsync();
        client.DefaultRequestHeaders.Authorization=null;
        var builder=WebApplication.CreateSlimBuilder();builder.Logging.ClearProviders();builder.WebHost.UseUrls("http://127.0.0.1:0");await using var bridge=builder.Build();
        bridge.Run(async context=>{
            if(context.Connection.RemoteIpAddress is null||!IPAddress.IsLoopback(context.Connection.RemoteIpAddress)||context.Request.Host.Host!="127.0.0.1"){context.Response.StatusCode=400;return;}
            var path=context.Request.Path.Value??"";
            if(path.StartsWith("/qa/expire/",StringComparison.Ordinal)){
                if(context.Request.Method!="POST"||context.Request.ContentLength is >0||context.Request.Headers["X-QA-Control"]!=controllerSecret||
                    !Guid.TryParseExact(path[11..],"D",out var session)||!owned.TryGetValue(session,out var fixture)||!observed.Contains(session)||!expired.Add(session)){context.Response.StatusCode=403;return;}
                using(var scope=factory.Services.CreateScope()){
                    var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
                    var row=await db.PentaMiniSessions.SingleAsync(x=>x.Id==session&&x.AcademyId==academy&&x.ActorUserId==actor);
                    var draft=await db.PentaOnboardingDrafts.SingleAsync(x=>x.Id==fixture.Draft&&x.AcademyId==academy&&x.ActorUserId==actor);
                    if(draft.Revision!=1||draft.Status!="AwaitingReview"||row.PendingRequestId!=fixture.Request)throw new InvalidOperationException("Expiry fixture changed before controlled expiration.");
                    clocks[session].Value=fixture.SessionExpiry?new DateTimeOffset(DateTime.SpecifyKind(row.ExpiresAtUtc,DateTimeKind.Utc)).AddSeconds(1):
                        new DateTimeOffset(DateTime.SpecifyKind(draft.ExpiresAtUtc,DateTimeKind.Utc)).AddSeconds(1);
                    if(!fixture.SessionExpiry&&row.ExpiresAtUtc<=clocks[session].GetUtcNow().UtcDateTime)
                        throw new InvalidOperationException("Draft expiry fixture must retain an unexpired conversation.");
                }
                context.Response.StatusCode=204;return;
            }
            var prefix=$"/api/academies/{academy}/penta/onboarding/conversations/";
            if(context.Request.Method!="GET"||!(path=="/api/auth/session"||path.StartsWith(prefix,StringComparison.Ordinal)&&System.Text.RegularExpressions.Regex.IsMatch(path,@"/(current|drafts/[0-9a-f-]{36}|claims/[0-9a-f-]{36}/result)$"))){context.Response.StatusCode=405;return;}
            using var request=new HttpRequestMessage(HttpMethod.Get,path);if(context.Request.Headers.TryGetValue("Authorization",out var auth))request.Headers.TryAddWithoutValidation("Authorization",auth.ToArray());
            HttpClient selectedClient=client;HttpClient? fixtureClient=null;
            if(path.StartsWith(prefix,StringComparison.Ordinal)&&Guid.TryParseExact(path[prefix.Length..].Split('/')[0],"D",out var selectedSession)&&hosts.TryGetValue(selectedSession,out var selectedHost))selectedClient=fixtureClient=selectedHost.CreateClient();
            try{using var result=await selectedClient.SendAsync(request,context.RequestAborted);
                if((int)result.StatusCode==200&&path.EndsWith("/current",StringComparison.Ordinal)&&Guid.TryParseExact(path[prefix.Length..].Split('/')[0],"D",out var seen)&&owned.ContainsKey(seen))observed.Add(seen);
                context.Response.StatusCode=(int)result.StatusCode;context.Response.ContentType=result.Content.Headers.ContentType?.ToString();await result.Content.CopyToAsync(context.Response.Body,context.RequestAborted);}
            finally{fixtureClient?.Dispose();}
        });await bridge.StartAsync();
        var start=new ProcessStartInfo("node"){WorkingDirectory=Directory.GetCurrentDirectory(),UseShellExecute=false,RedirectStandardInput=true,RedirectStandardOutput=true,RedirectStandardError=true,CreateNoWindow=true};
        start.ArgumentList.Add(Path.GetFullPath("QA/tools/penta-onboarding-expiry-live.cjs"));start.ArgumentList.Add(bridge.Urls.Single());start.ArgumentList.Add(manifest.RunId.ToString("N"));
        using var process=Process.Start(start)??throw new InvalidOperationException("Expiry browser did not start.");
        await process.StandardInput.WriteAsync(JsonSerializer.Serialize(new{fixtures,token=bearer,controllerSecret}));process.StandardInput.Close();var stdout=process.StandardOutput.ReadToEndAsync();var stderr=process.StandardError.ReadToEndAsync();
        try{using var timeout=new CancellationTokenSource(TimeSpan.FromMinutes(3));await process.WaitForExitAsync(timeout.Token);}catch{if(!process.HasExited)process.Kill(entireProcessTree:true);throw;}
        Console.Write(await stdout);if(process.ExitCode!=0)throw new InvalidOperationException("Expiry browser failed: "+await stderr);await bridge.StopAsync();
        if(expired.Count!=4||before!=await DomainSnapshot())throw new InvalidOperationException("Expiry browser changed unrelated/domain/Identity/protected state.");
        foreach(var entry in owned.Where(x=>x.Value.Pending)){
            using var expiredClient=hosts[entry.Key].CreateClient();expiredClient.DefaultRequestHeaders.Authorization=new AuthenticationHeaderValue("Bearer",bearer);
            using var response=await expiredClient.GetAsync($"/api/academies/{academy}/penta/onboarding/conversations/{entry.Key}/claims/{entry.Value.Request}/result");
            if((int)response.StatusCode!=410)throw new InvalidOperationException("Expired pending result remained accessible.");
        }
        using(var scope=factory.Services.CreateScope()){
            var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();var audits=await db.AuditLogs.AsNoTracking().Where(x=>!oldAudits.Contains(x.Id)).ToArrayAsync();
            foreach(var entry in owned){
                var row=await db.PentaMiniSessions.AsNoTracking().SingleAsync(x=>x.Id==entry.Key);var draft=await db.PentaOnboardingDrafts.AsNoTracking().SingleAsync(x=>x.Id==entry.Value.Draft);
                var now=clocks[entry.Key].GetUtcNow().UtcDateTime;
                if(draft.ExpiresAtUtc>now||(row.ExpiresAtUtc<=now)!=entry.Value.SessionExpiry)throw new InvalidOperationException("Per-fixture expiry clock does not match the intended session/draft boundary.");
            }
            var allowed=new[]{"PentaOnboardingConversationRead","PentaOnboardingDraftRead"};
            if(audits.Length!=8||audits.Any(x=>x.AcademyId!=academy||x.ActorUserId!=actor||!allowed.Contains(x.Action)))throw new InvalidOperationException("Expiry browser unexpected audit mutation.");
            var pending=await db.PentaMiniTurns.AsNoTracking().Where(x=>x.AcademyId==academy&&x.Status=="OnboardingPending").ToArrayAsync();
            if(pending.Length!=2||pending.Any(x=>x.ProtectedReceipt!=null))throw new InvalidOperationException("Expiry browser completed or recycled pending claims.");
            var directory=Path.GetFullPath(Path.Combine("QA","EVIDENCE","penta-onboarding-expiry-live-"+manifest.RunId.ToString("N")));Directory.CreateDirectory(directory);
            await File.WriteAllTextAsync(Path.Combine(directory,"sql-postconditions.json"),JsonSerializer.Serialize(new{passed=true,scenarios=4,sessionExpiryCases=2,draftOnlyExpiryCases=2,pendingClaimsRetained=2,expiredResultDenials=2,exactReadAudits=8,
                protectedDomainIdentityStateUnchanged=true,studentsCreated=0,inference=false,expiryControllerOnly=true,physicalDevices="NOT RUN"},new JsonSerializerOptions{WriteIndented=true}));
        }
        Console.WriteLine("PASS: PENTAONBOARDINGEXPIRY 4 actual browser/SQL selected/pending session/draft expiry journeys;pending retained;no domain writes/inference.");
        } finally { foreach(var host in hosts.Values)host.Dispose(); }
    }
}
