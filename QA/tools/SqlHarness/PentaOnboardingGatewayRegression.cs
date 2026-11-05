using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Intelligence.Penta;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

internal static partial class SqlHarnessEntryPoint
{
    private sealed class OnboardingScriptedInterpreter:IPentaOnboardingInterpreter
    {
        public int Calls;public Func<PentaOnboardingInterpretation,CancellationToken,Task<JsonElement>>? Handler;
        public Task<JsonElement> InterpretAsync(PentaOnboardingInterpretation input,CancellationToken token){Interlocked.Increment(ref Calls);return Handler!(input,token);}
    }
    private static async Task VerifyPentaOnboardingGatewayAsync(QaApiFactory baseline,QaRunManifest manifest)
    {
        var flags=new Dictionary<string,string?>{["Penta:OperationalDraftContractsEnabled"]="true",["Penta:OperationalOnboardingHttpContractsEnabled"]="true",
            ["Penta:OperationalOnboardingExecutionContractsEnabled"]="true",["Penta:OperationalOnboardingGatewayContractsEnabled"]="true",
            ["Penta:OperationalOnboardingPrivateBridgeContractsEnabled"]="true"};
        var clock=new OnboardingDraftClock();var planner=new OnboardingScriptedInterpreter();
        var leases=new PentaPrivateContextLeases();
        using var factory=new QaApiFactory(manifest,flags,isolatedClock:clock,isolatedOnboardingInterpreter:planner,isolatedOnboardingLeases:leases);
        Guid academy,foreign;using(var scope=factory.Services.CreateScope()){var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();academy=await db.Academies.Where(x=>x.Name=="Synthetic Academy A").Select(x=>x.Id).SingleAsync();foreign=await db.Academies.Where(x=>x.Name=="Synthetic Academy B").Select(x=>x.Id).SingleAsync();}
        var actor=await CreateAccessActorAsync(factory,"qa-core-gateway@example.invalid","AcademyAdmin",academy);
        var abandonmentActor=await CreateAccessActorAsync(factory,"qa-core-abandonment@example.invalid","AcademyAdmin",academy);
        async Task<HttpClient> Client(string email,QaApiFactory? host=null){var client=(host??factory).CreateClient();client.DefaultRequestHeaders.Authorization=new AuthenticationHeaderValue("Bearer",await LoginAsync(client,email,"Synthetic!39Ab"));return client;}
        using var admin=await Client("qa-core-gateway@example.invalid");using var teacher=await Client("qa-teacher-a@example.invalid");using var other=await Client("qa-admin-a@example.invalid");using var anonymous=factory.CreateClient();
        string Route(Guid session,Guid? tenant=null)=>$"/api/academies/{tenant??academy}/penta/onboarding/conversations/{session}";
        string Draft(Guid session,Guid draft)=>Route(session)+$"/drafts/{draft}";
        async Task<(Guid Session,Guid Draft,JsonObject Claim)> Fixture(HttpClient? caller=null,QaApiFactory? scopedHost=null){
            var fixtureHost=scopedHost??factory;var fixtureClient=caller??admin;var fixtureActor=caller is null?actor:abandonmentActor;
            Guid session;using(var scope=fixtureHost.Services.CreateScope()){var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();var row=new PentaMiniSession{AcademyId=academy,ActorUserId=fixtureActor,Version=0,ExpiresAtUtc=clock.Value.UtcDateTime.AddHours(1),ProtectedState="Synthetic gateway fixture; no native inference"};db.PentaMiniSessions.Add(row);await db.SaveChangesAsync();session=row.Id;}
            var draft=Guid.NewGuid();using var save=await fixtureClient.PutAsJsonAsync(Draft(session,draft),new{expectedRevision=0,contextVersion=0,fields=new{}});
            if((int)save.StatusCode!=201)throw new InvalidOperationException("Gateway fixture save failed: "+(int)save.StatusCode);var saved=await save.Content.ReadFromJsonAsync<JsonElement>();
            using var selected=await fixtureClient.PostAsJsonAsync(Route(session)+"/selection",new{requestId=Guid.NewGuid(),expectedVersion=0,draftId=draft,draftRevision=1,payloadHash=saved.GetProperty("payloadHash").GetString()});
            if((int)selected.StatusCode!=200)throw new InvalidOperationException("Gateway selection failed.");
            return(session,draft,JsonSerializer.SerializeToNode(new{requestId=Guid.NewGuid(),expectedVersion=1,draftRevision=1,payloadHash=saved.GetProperty("payloadHash").GetString(),text="SYNTHETIC_GATEWAY_PROMPT"})!.AsObject());
        }
        JsonElement Decision(PentaOnboardingInterpretation input,string kind="unsupported",bool partial=false)=>JsonSerializer.SerializeToElement(new{
            request_ref=input.RequestId.ToString("D"),scope=input.Snapshot.GetProperty("scope"),base_revision=input.Snapshot.GetProperty("draft").GetProperty("revision").GetInt64(),catalog_revision=1,kind,
            candidates=partial?new object[]{new{tool_key=PentaOnboardingDraftService.Tool,edit=new{mode="replace",set=new Dictionary<string,object>{["StudentFirstName"]=new{kind="text",value="SyntheticGateway"}},remove=Array.Empty<string>()}}}:Array.Empty<object>(),
            questions=kind=="clarify"?new[]{"StudentLastName","DateOfBirth"}:Array.Empty<string>(),invalidates_review=partial});
        planner.Handler=(input,_)=>Task.FromResult(Decision(input));
        async Task<string> Snapshot(bool domainOnly=false){using var scope=factory.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();var identity=scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            return JsonSerializer.Serialize(new{students=await db.Students.AsNoTracking().OrderBy(x=>x.Id).ToArrayAsync(),guardians=await db.Guardians.AsNoTracking().OrderBy(x=>x.Id).ToArrayAsync(),links=await db.StudentGuardians.AsNoTracking().OrderBy(x=>x.Id).ToArrayAsync(),
                drafts=domainOnly?[]:await db.PentaOnboardingDrafts.AsNoTracking().OrderBy(x=>x.Id).ToArrayAsync(),sessions=domainOnly?[]:await db.PentaMiniSessions.AsNoTracking().OrderBy(x=>x.Id).ToArrayAsync(),turns=domainOnly?[]:await db.PentaMiniTurns.AsNoTracking().OrderBy(x=>x.Id).ToArrayAsync(),audits=domainOnly?[]:await db.AuditLogs.AsNoTracking().OrderBy(x=>x.Id).ToArrayAsync(),users=await identity.Users.AsNoTracking().OrderBy(x=>x.Id).Select(x=>new{x.Id,x.UserName}).ToArrayAsync()});}
        var count=0;void Pass(string label){count++;Console.WriteLine("PASS: PENTAONBOARDINGGATEWAY "+label);}
        async Task Deny(string label,int expected,Func<Task<HttpResponseMessage>> call){var before=await Snapshot();var calls=planner.Calls;using var response=await call();if((int)response.StatusCode!=expected||before!=await Snapshot()||calls!=planner.Calls)throw new InvalidOperationException("Gateway denial "+label+" expected="+expected+" actual="+(int)response.StatusCode);Pass(label+" status="+expected+" no planner/state changes");}
        async Task<JsonElement> Send(Guid session,JsonObject claim,string expected,HttpClient? caller=null){using var response=await (caller??admin).PostAsJsonAsync(Route(session)+"/turns",claim);if((int)response.StatusCode!=201)throw new InvalidOperationException("Gateway turn failed: "+(int)response.StatusCode);var value=await response.Content.ReadFromJsonAsync<JsonElement>();
            if(value.GetProperty("status").GetString()!=expected||value.GetProperty("canExecute").GetBoolean()||value.GetRawText().Contains("SYNTHETIC_GATEWAY_PROMPT",StringComparison.Ordinal))throw new InvalidOperationException("Gateway fabricated success or echoed prompt.");return value;}
        var initial=await Fixture();var domain=await Snapshot(true);
        await Deny("anonymous",401,()=>anonymous.PostAsJsonAsync(Route(initial.Session)+"/turns",initial.Claim));
        await Deny("teacher",403,()=>teacher.PostAsJsonAsync(Route(initial.Session)+"/turns",initial.Claim));
        await Deny("foreign tenant",403,()=>admin.PostAsJsonAsync(Route(initial.Session,foreign)+"/turns",initial.Claim));
        await Deny("other actor",404,()=>other.PostAsJsonAsync(Route(initial.Session)+"/turns",initial.Claim));
        using(var disabled=await Client("qa-core-gateway@example.invalid",baseline))await Deny("default gates off",404,()=>disabled.PostAsJsonAsync(Route(initial.Session)+"/turns",initial.Claim));
        var noGateway=new Dictionary<string,string?>(flags){["Penta:OperationalOnboardingGatewayContractsEnabled"]="false"};
        using(var disabled=new QaApiFactory(manifest,noGateway,isolatedClock:clock,isolatedOnboardingInterpreter:planner))using(var client=await Client("qa-core-gateway@example.invalid",disabled))await Deny("separate gateway gate off",404,()=>client.PostAsJsonAsync(Route(initial.Session)+"/turns",initial.Claim));
        using(var absent=new QaApiFactory(manifest,flags,isolatedClock:clock))using(var client=await Client("qa-core-gateway@example.invalid",absent))await Deny("no qualified connection no admission",503,()=>client.PostAsJsonAsync(Route(initial.Session)+"/turns",initial.Claim));
        var bad=initial.Claim.DeepClone().AsObject();bad["role"]="Owner";await Deny("caller cannot choose authority",400,()=>admin.PostAsJsonAsync(Route(initial.Session)+"/turns",bad));
        var unsupported=await Send(initial.Session,initial.Claim,"unsupported");Pass("unsupported releases claim atomically without draft mutation");
        if(unsupported.GetProperty("draftRevision").GetInt64()!=1)throw new InvalidOperationException("No-change decision changed revision.");
        await Deny("same gateway request never reinterprets",409,()=>admin.PostAsJsonAsync(Route(initial.Session)+"/turns",initial.Claim));
        using(var restarted=new QaApiFactory(manifest,flags,isolatedClock:clock))using(var client=await Client("qa-core-gateway@example.invalid",restarted)){
            using var response=await client.GetAsync(Route(initial.Session)+$"/claims/{initial.Claim["requestId"]!.GetValue<Guid>()}/result");if((int)response.StatusCode!=200||!JsonElement.DeepEquals(unsupported,await response.Content.ReadFromJsonAsync<JsonElement>()))throw new InvalidOperationException("Neutral receipt not durable across restart.");}Pass("lost-response neutral result uses exact durable receipt without interpreter");
        var clarification=await Fixture();planner.Handler=(input,_)=>Task.FromResult(Decision(input,"clarify"));await Send(clarification.Session,clarification.Claim,"clarification_required");Pass("nonediting clarification preserves current draft revision");
        var partial=await Fixture();planner.Handler=(input,_)=>Task.FromResult(Decision(input,"clarify",true));await Send(partial.Session,partial.Claim,"draft_updated");Pass("prompt to injected interpretation to existing atomic partial adapter");
        var malformed=await Fixture();planner.Handler=(_,_)=>Task.FromResult(JsonSerializer.SerializeToElement(new{message="SYNTHETIC_SECRET_MODEL_PROSE",approved=true}));var invalid=await Send(malformed.Session,malformed.Claim,"invalid_response");if(invalid.GetRawText().Contains("SYNTHETIC_SECRET",StringComparison.Ordinal))throw new InvalidOperationException("Raw invalid model text escaped.");Pass("malformed model output terminal host error never prose or action");
        var unavailable=await Fixture();planner.Handler=(_,_)=>throw new HttpRequestException("synthetic private exception");await Send(unavailable.Session,unavailable.Claim,"unavailable");Pass("private interpreter outage safely completes no-change no domain writes");
        var timeout=await Fixture();planner.Handler=(_,_)=>throw new OperationCanceledException("synthetic interpreter cancellation");await Send(timeout.Session,timeout.Claim,"unavailable");Pass("provider cancellation without caller cancellation is unavailable not success");
        var drift=await Fixture();Guid branch;string branchName;using(var scope=factory.Services.CreateScope()){var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();var row=await db.Branches.FirstAsync(x=>x.AcademyId==academy&&x.IsActive);branch=row.Id;branchName=row.Name;}
        planner.Handler=async(input,token)=>{using var scope=factory.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();db.Database.SetCommandTimeout(5);await db.Branches.Where(x=>x.Id==branch).ExecuteUpdateAsync(x=>x.SetProperty(y=>y.Name,branchName+" changed"),token);return Decision(input,"clarify",true);};
        using(var response=await admin.PostAsJsonAsync(Route(drift.Session)+"/turns",drift.Claim))if((int)response.StatusCode!=409)throw new InvalidOperationException("In-flight catalog change adopted.");
        using(var scope=factory.Services.CreateScope()){var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();var session=await db.PentaMiniSessions.SingleAsync(x=>x.Id==drift.Session);if(session.PendingRequestId is null||(await db.PentaOnboardingDrafts.SingleAsync(x=>x.Id==drift.Draft)).Revision!=1)throw new InvalidOperationException("Drift committed draft or cleared pending.");await db.Branches.Where(x=>x.Id==branch).ExecuteUpdateAsync(x=>x.SetProperty(y=>y.Name,branchName));}
        Pass("no SQL lock held during planner and catalog drift blocks completion");
        var rollback=await Fixture();planner.Handler=(input,_)=>Task.FromResult(Decision(input));var fault=new OnboardingTurnFault("PentaOnboardingConversationCompleted");
        using(var broken=new QaApiFactory(manifest,flags,isolatedDomainInterceptor:fault,isolatedClock:clock,isolatedOnboardingInterpreter:planner))using(var client=await Client("qa-core-gateway@example.invalid",broken)){
            using var response=await client.PostAsJsonAsync(Route(rollback.Session)+"/turns",rollback.Claim);if((int)response.StatusCode!=503||fault.Faults!=1)throw new InvalidOperationException("Gateway final audit fault missing.");}
        using(var response=await admin.GetAsync(Route(rollback.Session)+$"/claims/{rollback.Claim["requestId"]!.GetValue<Guid>()}/result"))if((int)response.StatusCode!=202)throw new InvalidOperationException("Gateway rollback lost pending claim.");Pass("gateway failure retains durable pending claim for reconciliation");
        await Deny("failed gateway request cannot replan automatically",409,()=>admin.PostAsJsonAsync(Route(rollback.Session)+"/turns",rollback.Claim));
        var capacityA=await Fixture();var capacityB=await Fixture();var capacityC=await Fixture();var entered=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);var release=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);var inflight=0;
        planner.Handler=async(input,token)=>{if(Interlocked.Increment(ref inflight)==2)entered.TrySetResult();await release.Task.WaitAsync(token);return Decision(input);};
        var first=admin.PostAsJsonAsync(Route(capacityA.Session)+"/turns",capacityA.Claim);var second=admin.PostAsJsonAsync(Route(capacityB.Session)+"/turns",capacityB.Claim);
        try{await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));await Deny("bounded interpreter capacity no third admission",503,()=>admin.PostAsJsonAsync(Route(capacityC.Session)+"/turns",capacityC.Claim));}
        finally{release.TrySetResult();}
        var responses=await Task.WhenAll(first,second);try{if(responses.Any(x=>(int)x.StatusCode!=201))throw new InvalidOperationException("Admitted capacity requests did not complete201.");}finally{foreach(var response in responses)response.Dispose();}Pass("two admitted isolated conversations finish without duplicated interpretation");
        // Actual host 30-second timer, not an injected cancellation or shortened
        // test deadline. Hold both providers beyond it despite cancellation.
        // Separate synthetic actor/host for the additive suite. The original
        // actor's legitimate 20-drafts/day SQL quota must not be raised/reset.
        using var abandonmentFactory=new QaApiFactory(manifest,flags,isolatedClock:clock,isolatedOnboardingInterpreter:planner,isolatedOnboardingLeases:leases);
        using var abandonmentAdmin=await Client("qa-core-abandonment@example.invalid",abandonmentFactory);
        var timedA=await Fixture(abandonmentAdmin,abandonmentFactory);var timedB=await Fixture(abandonmentAdmin,abandonmentFactory);var timedC=await Fixture(abandonmentAdmin,abandonmentFactory);
        var timedEntered=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var lateA=new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
        var lateB=new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
        var timedInputs=new System.Collections.Concurrent.ConcurrentDictionary<Guid,PentaOnboardingInterpretation>();
        var timedTokens=new System.Collections.Concurrent.ConcurrentDictionary<Guid,CancellationToken>();
        var requestA=timedA.Claim["requestId"]!.GetValue<Guid>();var requestB=timedB.Claim["requestId"]!.GetValue<Guid>();
        planner.Handler=(input,token)=>{timedInputs[input.RequestId]=input;timedTokens[input.RequestId]=token;
            if(timedInputs.Count==2)timedEntered.TrySetResult();return input.RequestId==requestA?lateA.Task:lateB.Task;};
        var elapsed=System.Diagnostics.Stopwatch.StartNew();
        var waitingA=abandonmentAdmin.PostAsJsonAsync(Route(timedA.Session)+"/turns",timedA.Claim);
        var waitingB=abandonmentAdmin.PostAsJsonAsync(Route(timedB.Session)+"/turns",timedB.Claim);
        var abandonmentCount=0;void AbandonPass(string label){abandonmentCount++;Console.WriteLine("PASS: PENTAABANDONMENT "+label);}
        async Task AbandonDeny(int expected,Func<Task<HttpResponseMessage>> call){var before=await Snapshot();var calls=planner.Calls;
            using var response=await call();if((int)response.StatusCode!=expected||before!=await Snapshot()||calls!=planner.Calls)
                throw new InvalidOperationException("Abandoned provider denial failed or changed state.");}
        var capacity=(SemaphoreSlim)typeof(PentaOnboardingDraftService).GetField("GatewayCapacity",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic)!.GetValue(null)!;
        async Task CapacitySettled(){var until=System.Diagnostics.Stopwatch.StartNew();while(capacity.CurrentCount!=2&&until.Elapsed<TimeSpan.FromSeconds(5))await Task.Delay(10);
            if(capacity.CurrentCount!=2)throw new InvalidOperationException("Gateway capacity did not settle after actual provider exit.");}
        try {
            await timedEntered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Console.WriteLine("PENTAABANDONMENT waiting for unchanged real30-second gateway deadline; two synthetic providers intentionally ignore cancellation.");
            var timedResponses=await Task.WhenAll(waitingA,waitingB).WaitAsync(TimeSpan.FromSeconds(45));
            foreach(var response in timedResponses)using(response){var value=await response.Content.ReadFromJsonAsync<JsonElement>();
                if((int)response.StatusCode!=201||value.GetProperty("status").GetString()!="unavailable"||value.GetProperty("draftRevision").GetInt64()!=1||
                    value.GetProperty("canExecute").GetBoolean())throw new InvalidOperationException("Host deadline fabricated draft/domain success.");}
            if(elapsed.Elapsed<TimeSpan.FromSeconds(29)||timedTokens.Values.Any(x=>!x.IsCancellationRequested))throw new InvalidOperationException("Real gateway deadline was not exercised.");
            AbandonPass("real30-second deadline returns unavailable for both ignored-cancellation providers without draft edits");
            if(lateA.Task.IsCompleted||lateB.Task.IsCompleted||capacity.CurrentCount!=0)throw new InvalidOperationException("Timed-out provider capacity released early.");
            await AbandonDeny(503,()=>abandonmentAdmin.PostAsJsonAsync(Route(timedC.Session)+"/turns",timedC.Claim));
            AbandonPass("two unfinished providers retain capacity and third request has no claim or interpretation");
            foreach(var input in timedInputs.Values){try{await input.Current!(CancellationToken.None);throw new InvalidOperationException("Expired private callback released context.");}catch(OperationCanceledException){}}
            AbandonPass("timed-out private context callbacks deny even when provider ignores its cancellation token");
            var beforeLate=await Snapshot();
            lateA.TrySetResult(Decision(timedInputs[requestA],"clarify",true));lateB.TrySetException(new IOException("Synthetic private late failure"));
            await CapacitySettled();
            if(beforeLate!=await Snapshot())throw new InvalidOperationException("Late decision/fault changed SQL state.");
            AbandonPass("late valid partial and late fault discarded with identical SQL state after true provider exit");
            planner.Handler=(input,_)=>Task.FromResult(Decision(input));await Send(timedC.Session,timedC.Claim,"unsupported",abandonmentAdmin);
            AbandonPass("only a previously unadmitted request can proceed once capacity is actually released");
        }finally{lateA.TrySetCanceled();lateB.TrySetCanceled();await Task.WhenAll(waitingA,waitingB).ContinueWith(_=>{},TaskScheduler.Default);await CapacitySettled();}
        // Exact caller token cancellation at the host service boundary, backed by
        // real Identity/SQL. TestServer client abandonment alone cannot prove the
        // endpoint stopped rather than merely that its HTTP waiter stopped.
        var cancelled=await Fixture(abandonmentAdmin,abandonmentFactory);var cancelledPeer=await Fixture(abandonmentAdmin,abandonmentFactory);var cancelledThird=await Fixture(abandonmentAdmin,abandonmentFactory);
        var cancelEntered=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var cancelledWork=new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
        var peerWork=new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
        var cancelledInputs=new System.Collections.Concurrent.ConcurrentDictionary<Guid,PentaOnboardingInterpretation>();
        var cancelledRequest=cancelled.Claim["requestId"]!.GetValue<Guid>();
        planner.Handler=(input,_)=>{cancelledInputs[input.RequestId]=input;if(cancelledInputs.Count==2)cancelEntered.TrySetResult();
            return input.RequestId==cancelledRequest?cancelledWork.Task:peerWork.Task;};
        using var callerCancellation=new CancellationTokenSource();
        using(var scope=abandonmentFactory.Services.CreateScope()) {
            var service=scope.ServiceProvider.GetRequiredService<PentaOnboardingDraftService>();
            var principal=new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(new[]{
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier,abandonmentActor.ToString("D")),
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role,"AcademyAdmin")},"Synthetic SQL caller"));
            var abandoned=service.CoreGatewayAsync(principal,academy,cancelled.Session,JsonSerializer.SerializeToElement(cancelled.Claim),callerCancellation.Token);
            var peer=abandonmentAdmin.PostAsJsonAsync(Route(cancelledPeer.Session)+"/turns",cancelledPeer.Claim);
            try {
                await cancelEntered.Task.WaitAsync(TimeSpan.FromSeconds(10));callerCancellation.Cancel();
                try{await abandoned.WaitAsync(TimeSpan.FromSeconds(5));throw new InvalidOperationException("Caller abort reported a completed result.");}catch(OperationCanceledException){}
                using(var response=await abandonmentAdmin.GetAsync(Route(cancelled.Session)+$"/claims/{cancelledRequest}/result"))if((int)response.StatusCode!=202)throw new InvalidOperationException("Caller abort lost durable pending claim.");
                AbandonPass("real caller token abort stops host waiter and retains pending claim for reconciliation");
                if(capacity.CurrentCount!=0)throw new InvalidOperationException("Caller abort released unfinished provider slot.");
                await AbandonDeny(503,()=>abandonmentAdmin.PostAsJsonAsync(Route(cancelledThird.Session)+"/turns",cancelledThird.Claim));
                AbandonPass("caller abort does not free provider slot or allow extra admission");
                try{await cancelledInputs[cancelledRequest].Current!(CancellationToken.None);throw new InvalidOperationException("Aborted callback released context.");}catch(OperationCanceledException){}
                AbandonPass("caller-aborted private callback denied without a SQL read or audit");
                var draftBefore=await Snapshot();cancelledWork.TrySetResult(Decision(cancelledInputs[cancelledRequest],"clarify",true));
                // The active peer still owns its slot; await only the abandoned
                // provider's completion continuation, with no operation retries.
                var wait=System.Diagnostics.Stopwatch.StartNew();while(capacity.CurrentCount!=1&&wait.Elapsed<TimeSpan.FromSeconds(5))await Task.Delay(10);
                if(capacity.CurrentCount!=1||draftBefore!=await Snapshot())throw new InvalidOperationException("Late cancelled result mutated SQL or lost peer capacity.");
                AbandonPass("late partial after caller abort cannot save draft or clear pending claim");
            }finally {
                cancelledWork.TrySetCanceled();
                var peerInput=cancelledInputs.Values.SingleOrDefault(x=>x.RequestId!=cancelledRequest);
                if(peerInput is not null)peerWork.TrySetResult(Decision(peerInput));else peerWork.TrySetCanceled();
                using var response=await peer.WaitAsync(TimeSpan.FromSeconds(10));
                if((int)response.StatusCode!=201)throw new InvalidOperationException("Unaffected peer failed completion.");
                await CapacitySettled();
            }
        }
        planner.Handler=(input,_)=>Task.FromResult(Decision(input));
        await AbandonDeny(409,()=>abandonmentAdmin.PostAsJsonAsync(Route(cancelled.Session)+"/turns",cancelled.Claim));
        AbandonPass("aborted request remains non-redispatchable after provider exit");
        if(abandonmentCount!=10)throw new InvalidOperationException("Incomplete abandonment matrix: "+abandonmentCount);
        // Actual Python HTTP factory -> actual private callback controller ->
        // real SQL. Scripted proposals ONLY, no native/model qualification.
        await using(var bridge=await PentaPrivateBridgeFixture.StartAsync(factory,leases,manifest.RunId.ToString("N"))) {
            planner.Handler=bridge.Interpreter.InterpretAsync;
            var connected=await Fixture();await Send(connected.Session,connected.Claim,"unsupported");
            if(bridge.CurrentCalls!=2)throw new InvalidOperationException("Missing before/after SQL context checks.");
            Console.WriteLine("PASS: PENTAPRIVATEBRIDGE actual .NET/Python sockets and two current-authority SQL reads; no model");
            using(var client=factory.CreateClient()) {
                using var request=new HttpRequestMessage(HttpMethod.Get,"/internal/penta/onboarding/current");
                request.Headers.Authorization=admin.DefaultRequestHeaders.Authorization;request.Headers.Add("X-Penta-Request",connected.Claim["requestId"]!.GetValue<Guid>().ToString("D"));
                request.Headers.Add(PentaPrivateContextLeases.VersionHeader,PentaPrivateContextLeases.Version);
                request.Headers.Add(PentaPrivateContextLeases.BodyHeader,new string('a',64));
                using var response=await client.SendAsync(request);if((int)response.StatusCode!=404)throw new InvalidOperationException("User bearer opened private context.");
            }
            Console.WriteLine("PASS: PENTAPRIVATEBRIDGE authenticated user token is not a private context lease");
            using(var client=new HttpClient(new SocketsHttpHandler{UseProxy=false,AllowAutoRedirect=false})) {
                using var response=await client.PostAsJsonAsync(new Uri(bridge.Origin,"/candidate/interpret"),new{role="Owner"});
                if((int)response.StatusCode!=403||bridge.CurrentCalls!=2)throw new InvalidOperationException("Unauthenticated private request reached context.");
            }
            Console.WriteLine("PASS: PENTAPRIVATEBRIDGE unauthenticated socket denied before host context");
            var changed=await Fixture();var secondRead=bridge.CurrentCalls+2;
            bridge.BeforeCurrent=async call=>{if(call!=secondRead)return;using var scope=factory.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();await db.Branches.Where(x=>x.Id==branch).ExecuteUpdateAsync(x=>x.SetProperty(y=>y.Name,branchName+" bridge drift"));};
            using(var response=await admin.PostAsJsonAsync(Route(changed.Session)+"/turns",changed.Claim))if((int)response.StatusCode!=409)throw new InvalidOperationException("Private callback drift accepted.");
            using(var scope=factory.Services.CreateScope()){var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();if((await db.PentaOnboardingDrafts.SingleAsync(x=>x.Id==changed.Draft)).Revision!=1||(await db.PentaMiniSessions.SingleAsync(x=>x.Id==changed.Session)).PendingRequestId is null)throw new InvalidOperationException("Private context drift cleared pending or changed draft.");await db.Branches.Where(x=>x.Id==branch).ExecuteUpdateAsync(x=>x.SetProperty(y=>y.Name,branchName));}
            bridge.BeforeCurrent=null;Console.WriteLine("PASS: PENTAPRIVATEBRIDGE second current SQL catalogue drift withholds plan and retains claim");
            await bridge.StopInterpreterAsync();var outage=await Fixture();await Send(outage.Session,outage.Claim,"unavailable");
            Console.WriteLine("PASS: PENTAPRIVATEBRIDGE owned Python process outage no retry or domain write");
        }
        var revoked=await Fixture();planner.Handler=async(input,token)=>{using var scope=factory.Services.CreateScope();var identity=scope.ServiceProvider.GetRequiredService<IdentityDbContext>();await identity.Users.Where(x=>x.Id==actor).ExecuteUpdateAsync(x=>x.SetProperty(y=>y.ConcurrencyStamp,Guid.NewGuid().ToString("N")),token);return Decision(input,"clarify",true);};
        using(var response=await admin.PostAsJsonAsync(Route(revoked.Session)+"/turns",revoked.Claim))if((int)response.StatusCode!=409)throw new InvalidOperationException("Changed current authority adopted.");Pass("current authority change during private planning blocks mutation");
        if(domain!=await Snapshot(true))throw new InvalidOperationException("Gateway changed domain or account data.");Pass("all gateway cases leave domain and account data unchanged");
        if(count!=23)throw new InvalidOperationException("Incomplete gateway matrix: "+count);
        Console.WriteLine("PASS: PENTAONBOARDINGGATEWAY all "+count+" HTTP/real SQL cases; injected interpreter only, no model/runtime qualification or activation.");
    }
}
