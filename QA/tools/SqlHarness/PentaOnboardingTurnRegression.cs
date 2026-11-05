using System.Data.Common;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Intelligence.Penta;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

internal static partial class SqlHarnessEntryPoint
{
    private sealed class OnboardingAdvancingClock(DateTimeOffset value):TimeProvider
    {
        private long calls;
        public override DateTimeOffset GetUtcNow()=>value.AddTicks(Interlocked.Increment(ref calls));
    }
    private sealed class OnboardingTurnFault(string action,OnboardingDraftClock? expiry=null):DbCommandInterceptor
    {
        public int Faults{get;private set;}
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,CommandEventData data,
            InterceptionResult<DbDataReader> result,CancellationToken token=default)
        {
            if(Faults==0&&command.CommandText.Contains("INSERT INTO [AuditLogs]",StringComparison.Ordinal)&&
                command.Parameters.Cast<DbParameter>().Any(x=>x.Value is string value&&value==action)) {
                Faults++;
                if(expiry is null)command.CommandText="THROW 51019, 'Synthetic turn audit fault', 1;\n"+command.CommandText;
                else expiry.Value=expiry.Value.AddMinutes(11);
            }
            return base.ReaderExecutingAsync(command,data,result,token);
        }
    }
    private static async Task VerifyPentaOnboardingTurnAsync(QaApiFactory baseline,QaRunManifest manifest)
    {
        var flags=new Dictionary<string,string?>{["Penta:OperationalDraftContractsEnabled"]="true",["Penta:OperationalOnboardingHttpContractsEnabled"]="true",["Penta:OperationalOnboardingExecutionContractsEnabled"]="true"};
        var clock=new OnboardingDraftClock();using var factory=new QaApiFactory(manifest,flags,isolatedClock:clock);
        Guid academy,foreign,foreignBranch;
        using(var scope=factory.Services.CreateScope()) {var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            academy=await db.Academies.Where(x=>x.Name=="Synthetic Academy A").Select(x=>x.Id).SingleAsync();foreign=await db.Academies.Where(x=>x.Name=="Synthetic Academy B").Select(x=>x.Id).SingleAsync();
            foreignBranch=await db.Branches.Where(x=>x.AcademyId==foreign).Select(x=>x.Id).FirstAsync();}
        var actor=await CreateAccessActorAsync(factory,"qa-core-turn@example.invalid","AcademyAdmin",academy);
        async Task<HttpClient> Client(string email,QaApiFactory? host=null){var client=(host??factory).CreateClient();client.DefaultRequestHeaders.Authorization=new AuthenticationHeaderValue("Bearer",await LoginAsync(client,email,"Synthetic!39Ab"));return client;}
        using var admin=await Client("qa-core-turn@example.invalid");using var teacher=await Client("qa-teacher-a@example.invalid");using var other=await Client("qa-admin-a@example.invalid");using var anonymous=factory.CreateClient();
        string Route(Guid session,Guid? tenant=null)=>$"/api/academies/{tenant??academy}/penta/onboarding/conversations/{session}";
        string Draft(Guid session,Guid draft)=>Route(session)+$"/drafts/{draft}";
        string Complete(Guid session,Guid request,Guid? tenant=null)=>Route(session,tenant)+$"/claims/{request}/complete";
        string Result(Guid session,Guid request)=>Route(session)+$"/claims/{request}/result";
        static object Text(string value)=>new{kind="text",value};
        async Task<(Guid Session,Guid Draft,JsonElement Context)> Fixture(){
            Guid session;using(var scope=factory.Services.CreateScope()){var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();var row=new PentaMiniSession{AcademyId=academy,ActorUserId=actor,Version=0,ExpiresAtUtc=clock.Value.UtcDateTime.AddHours(1),ProtectedState="Synthetic turn fixture; no inference"};db.PentaMiniSessions.Add(row);await db.SaveChangesAsync();session=row.Id;}
            var draft=Guid.NewGuid();using var save=await admin.PutAsJsonAsync(Draft(session,draft),new{expectedRevision=0,contextVersion=0,fields=new{}});
            if((int)save.StatusCode!=201)throw new InvalidOperationException("Turn fixture save failed.");var view=await save.Content.ReadFromJsonAsync<JsonElement>();
            using var select=await admin.PostAsJsonAsync(Route(session)+"/selection",new{requestId=Guid.NewGuid(),expectedVersion=0,draftId=draft,draftRevision=1,payloadHash=view.GetProperty("payloadHash").GetString()});
            if((int)select.StatusCode!=200)throw new InvalidOperationException("Turn fixture selection failed.");var cursor=await select.Content.ReadFromJsonAsync<JsonElement>();return(session,draft,cursor.GetProperty("snapshot").Clone());
        }
        JsonObject Claim(JsonElement context,long version)=>JsonSerializer.SerializeToNode(new{requestId=Guid.NewGuid(),expectedVersion=version,draftRevision=context.GetProperty("draft").GetProperty("revision").GetInt64(),
            payloadHash=PentaOperationalPayload.Digest(JsonSerializer.Serialize(new{tool_key=PentaOnboardingDraftService.Tool,tool_revision=1,fields=context.GetProperty("draft").GetProperty("fields")})),text="SYNTHETIC_PRIVATE_TURN_PROMPT"})!.AsObject();
        async Task<JsonObject> Admit(Guid session,JsonElement context,long version){var claim=Claim(context,version);using var response=await admin.PostAsJsonAsync(Route(session)+"/claims",claim);if((int)response.StatusCode!=202)throw new InvalidOperationException("Turn claim failed: "+(int)response.StatusCode);return claim;}
        JsonObject Decision(JsonElement context,JsonObject claim,string kind,object? fields=null)=>JsonSerializer.SerializeToNode(new{
            request_ref=claim["requestId"]!.GetValue<Guid>().ToString("D"),scope=context.GetProperty("scope"),base_revision=context.GetProperty("draft").GetProperty("revision").GetInt64(),catalog_revision=1,kind,
            candidates=kind.StartsWith("request_",StringComparison.Ordinal)?Array.Empty<object>():new object[]{new{tool_key=PentaOnboardingDraftService.Tool,edit=new{mode="replace",set=JsonSerializer.SerializeToElement(fields??new{}),remove=Array.Empty<string>()}}},
            questions=kind=="clarify"?new[]{"StudentLastName","DateOfBirth"}:Array.Empty<string>(),invalidates_review=true})!.AsObject();
        static JsonObject Envelope(JsonObject claim,JsonObject decision)=>new(){["claim"]=claim.DeepClone(),["decision"]=decision.DeepClone()};
        async Task<string> Snapshot(bool domainOnly=false){using var scope=factory.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();var identity=scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            return JsonSerializer.Serialize(new{students=await db.Students.AsNoTracking().OrderBy(x=>x.Id).ToArrayAsync(),guardians=await db.Guardians.AsNoTracking().OrderBy(x=>x.Id).ToArrayAsync(),links=await db.StudentGuardians.AsNoTracking().OrderBy(x=>x.Id).ToArrayAsync(),
                drafts=domainOnly?[]:await db.PentaOnboardingDrafts.AsNoTracking().OrderBy(x=>x.Id).ToArrayAsync(),sessions=domainOnly?[]:await db.PentaMiniSessions.AsNoTracking().OrderBy(x=>x.Id).ToArrayAsync(),turns=domainOnly?[]:await db.PentaMiniTurns.AsNoTracking().OrderBy(x=>x.Id).ToArrayAsync(),audits=domainOnly?[]:await db.AuditLogs.AsNoTracking().OrderBy(x=>x.Id).ToArrayAsync(),
                users=await identity.Users.AsNoTracking().OrderBy(x=>x.Id).Select(x=>new{x.Id,x.AcademyId,x.UserName}).ToArrayAsync(),roles=await identity.UserRoles.AsNoTracking().OrderBy(x=>x.UserId).ThenBy(x=>x.RoleId).ToArrayAsync()});}
        var count=0;var partialJourneys=new List<object>();void Pass(string name){count++;Console.WriteLine("PASS: PENTAONBOARDINGTURN "+name);}
        async Task Deny(string name,int status,Func<Task<HttpResponseMessage>> call){var before=await Snapshot();using var response=await call();if((int)response.StatusCode!=status||before!=await Snapshot())throw new InvalidOperationException("Turn denial "+name+" expected="+status+" actual="+(int)response.StatusCode);Pass(name+" status="+status+" no state changes");}
        async Task<JsonElement> Apply(Guid session,JsonObject claim,JsonObject decision){using var response=await admin.PostAsJsonAsync(Complete(session,claim["requestId"]!.GetValue<Guid>()),Envelope(claim,decision));if((int)response.StatusCode!=201)throw new InvalidOperationException("Turn completion failed: "+(int)response.StatusCode);return await response.Content.ReadFromJsonAsync<JsonElement>();}
        var fixture=await Fixture();var initialDomain=await Snapshot(true);var claim=await Admit(fixture.Session,fixture.Context,1);var nonce=claim["requestId"]!.GetValue<Guid>();var fields=new Dictionary<string,object>{["StudentFirstName"]=Text("SyntheticTurn")};var decision=Decision(fixture.Context,claim,"clarify",fields);var envelope=Envelope(claim,decision);
        await Deny("anonymous completion",401,()=>anonymous.PostAsJsonAsync(Complete(fixture.Session,nonce),envelope));
        await Deny("teacher completion",403,()=>teacher.PostAsJsonAsync(Complete(fixture.Session,nonce),envelope));
        await Deny("foreign tenant completion",403,()=>admin.PostAsJsonAsync(Complete(fixture.Session,nonce,foreign),envelope));
        await Deny("other actor completion",404,()=>other.PostAsJsonAsync(Complete(fixture.Session,nonce),envelope));
        using(var disabled=await Client("qa-core-turn@example.invalid",baseline))await Deny("default disabled",404,()=>disabled.PostAsJsonAsync(Complete(fixture.Session,nonce),envelope));
        await Deny("anonymous result",401,()=>anonymous.GetAsync(Result(fixture.Session,nonce)));
        await Deny("teacher result",403,()=>teacher.GetAsync(Result(fixture.Session,nonce)));
        await Deny("other actor result",404,()=>other.GetAsync(Result(fixture.Session,nonce)));
        foreach(var kind in new[]{"text","hash","version","scope","privilege","SQL","execute","base"}){var bad=envelope.DeepClone().AsObject();
            if(kind=="text")bad["claim"]!["text"]="changed";if(kind=="hash")bad["claim"]!["payloadHash"]=new string('a',64);if(kind=="version")bad["claim"]!["expectedVersion"]=0;
            if(kind=="scope")bad["decision"]!["scope"]!["actor_id"]=Guid.NewGuid().ToString("D");if(kind=="privilege")bad["decision"]!["approved"]=true;if(kind=="SQL")bad["decision"]!["candidates"]![0]!["tool_key"]="ExecuteSQL";
            if(kind=="execute")bad["decision"]!["kind"]="execute";if(kind=="base")bad["decision"]!["base_revision"]=2;
            await Deny("altered "+kind,kind is "text" or "hash" or "version" or "scope"?409:400,()=>admin.PostAsJsonAsync(Complete(fixture.Session,nonce),bad));}
        using(var pending=await admin.GetAsync(Result(fixture.Session,nonce))){var value=await pending.Content.ReadFromJsonAsync<JsonElement>();if((int)pending.StatusCode!=202||value.GetProperty("status").GetString()!="pending"||value.GetProperty("canExecute").GetBoolean())throw new InvalidOperationException("Pending lookup invented completion.");}Pass("pending result is202 not success or permission");
        await Deny("wrong request route",400,()=>admin.PostAsJsonAsync(Complete(fixture.Session,Guid.NewGuid()),envelope));
        var responses=await Task.WhenAll(admin.PostAsJsonAsync(Complete(fixture.Session,nonce),envelope),admin.PostAsJsonAsync(Complete(fixture.Session,nonce),envelope));JsonElement completed;
        try{if(!responses.Select(x=>(int)x.StatusCode).Order().SequenceEqual(new[]{200,201}))throw new InvalidOperationException("Completion race must be201/200 never429.");completed=await responses[0].Content.ReadFromJsonAsync<JsonElement>();if(!JsonElement.DeepEquals(completed,await responses[1].Content.ReadFromJsonAsync<JsonElement>()))throw new InvalidOperationException("Concurrent receipt differs.");}finally{foreach(var response in responses)response.Dispose();}
        if(completed.GetProperty("status").GetString()!="draft_updated"||completed.GetProperty("canExecute").GetBoolean()||initialDomain!=await Snapshot(true))throw new InvalidOperationException("Turn claimed domain success.");
        partialJourneys.Add(new{before=fixture.Context,decision,after=completed.GetProperty("snapshot")});Pass("same completion201/200 exact receipt one draft revision no domain effect");
        using(var scope=factory.Services.CreateScope()){var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();var session=await db.PentaMiniSessions.SingleAsync(x=>x.Id==fixture.Session);var turn=await db.PentaMiniTurns.SingleAsync(x=>x.SessionId==fixture.Session&&x.RequestId==nonce);var row=await db.PentaOnboardingDrafts.SingleAsync(x=>x.Id==fixture.Draft);
            if(session.PendingRequestId is not null||session.Version!=0||row.Revision!=2||row.Status!="AwaitingReview"||turn.Status!="OnboardingCompleted"||turn.ProtectedReceipt!.Contains("SYNTHETIC_PRIVATE",StringComparison.Ordinal)||await db.AuditLogs.CountAsync(x=>x.EntityId==session.Id&&x.Action=="PentaOnboardingConversationCompleted")!=1)throw new InvalidOperationException("Atomic completion state mismatch.");}
        Pass("atomic cursor release protected receipt and mandatory completion audit");
        var changed=envelope.DeepClone().AsObject();changed["decision"]!["candidates"]![0]!["edit"]!["set"]!["StudentFirstName"]!["value"]="Altered";
        await Deny("completed request cannot adopt different decision",409,()=>admin.PostAsJsonAsync(Complete(fixture.Session,nonce),changed));
        var beforeReplay=await Snapshot();using(var restarted=new QaApiFactory(manifest,flags,isolatedClock:clock))using(var reload=await Client("qa-core-turn@example.invalid",restarted)){
            using var replay=await reload.PostAsJsonAsync(Complete(fixture.Session,nonce),envelope);if((int)replay.StatusCode!=200||!JsonElement.DeepEquals(completed,await replay.Content.ReadFromJsonAsync<JsonElement>())||beforeReplay!=await Snapshot())throw new InvalidOperationException("Restart replay redispatched.");
            using var result=await reload.GetAsync(Result(fixture.Session,nonce));if((int)result.StatusCode!=200||!JsonElement.DeepEquals(completed,await result.Content.ReadFromJsonAsync<JsonElement>()))throw new InvalidOperationException("Result lost across restart.");}
        Pass("restart and lost-response lookup return real exact stored receipt");
        AuditLog savedAudit;using(var scope=factory.Services.CreateScope()){var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();savedAudit=await db.AuditLogs.AsNoTracking().SingleAsync(x=>x.EntityId==fixture.Session&&x.Action=="PentaOnboardingConversationCompleted");await db.AuditLogs.Where(x=>x.Id==savedAudit.Id).ExecuteDeleteAsync();}
        await Deny("missing completion audit withholds receipt",409,()=>admin.GetAsync(Result(fixture.Session,nonce)));
        using(var scope=factory.Services.CreateScope()){var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();db.AuditLogs.Add(savedAudit);await db.SaveChangesAsync();}
        var current=completed.GetProperty("snapshot");claim=await Admit(fixture.Session,current,completed.GetProperty("version").GetInt64());var prepareNonce=claim["requestId"]!.GetValue<Guid>();
        await Deny("new admitted turn makes old result stale",409,()=>admin.GetAsync(Result(fixture.Session,nonce)));
        var full=new Dictionary<string,object>{["StudentFirstName"]=Text("SyntheticTurn"),["StudentLastName"]=Text("Person"+fixture.Draft.ToString("N")[..8]),["DateOfBirth"]=new{kind="date",value="2000-01-01"}};
        var prepared=await Apply(fixture.Session,claim,Decision(current,claim,"host_prepare",full));var operation=prepared.GetProperty("operations")[0];
        if(operation.GetProperty("status").GetString()!="awaiting_approval"||operation.GetProperty("approval_ref").ValueKind!=JsonValueKind.Null||prepared.GetProperty("canExecute").GetBoolean())throw new InvalidOperationException("Prepared turn fabricated approval.");Pass("claimed preparation commits preview only never approval or student");
        await Deny("prepared turn cannot use legacy execution",409,()=>admin.PostAsJsonAsync(Draft(fixture.Session,fixture.Draft)+"/execute",new{revision=operation.GetProperty("draft_revision").GetInt64(),payloadHash=operation.GetProperty("payload_hash").GetString()}));
        var approved=await ConfirmPreparedOnboardingAsync(admin,Draft(fixture.Session,fixture.Draft),operation);Pass("explicit existing host challenge confirmation remains separate");
        using(var response=await admin.GetAsync(Draft(fixture.Session,fixture.Draft)+"/core-context")){if((int)response.StatusCode!=200)throw new InvalidOperationException("Approved current failed.");current=await response.Content.ReadFromJsonAsync<JsonElement>();}
        claim=await Admit(fixture.Session,current,prepared.GetProperty("version").GetInt64());decision=Decision(current,claim,"clarify",fields);completed=await Apply(fixture.Session,claim,decision);partialJourneys.Add(new{before=current,decision,after=completed.GetProperty("snapshot")});
        await Deny("partial turn edit invalidates old approval",409,()=>admin.PostAsJsonAsync(Draft(fixture.Session,fixture.Draft)+"/core/execute",CoreOnboardingBinding(approved,true)));
        Pass("claimed partial from approved state clears approval without creating");
        current=completed.GetProperty("snapshot");claim=await Admit(fixture.Session,current,completed.GetProperty("version").GetInt64());var paused=await Apply(fixture.Session,claim,Decision(current,claim,"request_pause"));
        if(paused.GetProperty("snapshot").GetProperty("draft").GetProperty("status").GetString()!="paused")throw new InvalidOperationException("Claimed pause failed.");Pass("claimed pause retains fields and clears review");
        current=paused.GetProperty("snapshot");claim=await Admit(fixture.Session,current,paused.GetProperty("version").GetInt64());
        await Deny("paused claim cannot use partial adapter",409,()=>admin.PostAsJsonAsync(Complete(fixture.Session,claim["requestId"]!.GetValue<Guid>()),Envelope(claim,Decision(current,claim,"clarify",fields))));
        var resumed=await Apply(fixture.Session,claim,Decision(current,claim,"request_resume"));Pass("only explicit resume may complete a paused claim");
        current=resumed.GetProperty("snapshot");claim=await Admit(fixture.Session,current,resumed.GetProperty("version").GetInt64());var cancelled=await Apply(fixture.Session,claim,Decision(current,claim,"request_cancel"));
        if(cancelled.GetProperty("snapshot").GetProperty("draft").GetProperty("status").GetString()!="cancelled")throw new InvalidOperationException("Claimed cancel failed.");Pass("claimed cancellation is draft-only no deletion");
        await Deny("cancelled cannot admit another turn",409,()=>admin.PostAsJsonAsync(Route(fixture.Session)+"/claims",Claim(cancelled.GetProperty("snapshot"),cancelled.GetProperty("version").GetInt64())));

        var rollback=await Fixture();claim=await Admit(rollback.Session,rollback.Context,1);nonce=claim["requestId"]!.GetValue<Guid>();envelope=Envelope(claim,Decision(rollback.Context,claim,"clarify",fields));
        var fault=new OnboardingTurnFault("PentaOnboardingConversationCompleted");using(var broken=new QaApiFactory(manifest,flags,isolatedDomainInterceptor:fault,isolatedClock:clock))using(var client=await Client("qa-core-turn@example.invalid",broken))
            await Deny("final completion audit failure rolls back draft receipt and release",503,()=>client.PostAsJsonAsync(Complete(rollback.Session,nonce),envelope));
        if(fault.Faults!=1)throw new InvalidOperationException("Final audit fault not observed.");
        using(var response=await admin.GetAsync(Result(rollback.Session,nonce)))if((int)response.StatusCode!=202)throw new InvalidOperationException("Audit rollback lost pending claim.");Pass("failed completion stays pending for verified reconciliation");
        using(var advancing=new QaApiFactory(manifest,flags,isolatedClock:new OnboardingAdvancingClock(clock.Value)))
        using(var client=await Client("qa-core-turn@example.invalid",advancing)) {
            using var response=await client.PostAsJsonAsync(Complete(rollback.Session,nonce),envelope);
            if((int)response.StatusCode!=201)throw new InvalidOperationException("Verified rollback completion failed.");
            var saved=await response.Content.ReadFromJsonAsync<JsonElement>();
            using var replay=await client.GetAsync(Result(rollback.Session,nonce));
            if((int)replay.StatusCode!=200||!JsonElement.DeepEquals(saved,await replay.Content.ReadFromJsonAsync<JsonElement>()))throw new InvalidOperationException("Advancing clock receipt/audit instants disagree.");
        }Pass("explicit completion after proven rollback commits once and reconciles with advancing clock");
        clock.Value=clock.Value.AddSeconds(1); // fixed fixture clock resumes after the advancing completion clock
        fault=new OnboardingTurnFault("PentaOnboardingConversationResultRead");using(var broken=new QaApiFactory(manifest,flags,isolatedDomainInterceptor:fault,isolatedClock:clock))using(var client=await Client("qa-core-turn@example.invalid",broken))
            await Deny("result audit failure withholds stored result",503,()=>client.GetAsync(Result(rollback.Session,nonce)));
        if(fault.Faults!=1)throw new InvalidOperationException("Result read fault not observed.");
        var deadline=await Fixture();claim=await Admit(deadline.Session,deadline.Context,1);nonce=claim["requestId"]!.GetValue<Guid>();envelope=Envelope(claim,Decision(deadline.Context,claim,"clarify",fields));var originalTime=clock.Value;
        fault=new OnboardingTurnFault("PentaOnboardingConversationCompleted",clock);using(var expired=new QaApiFactory(manifest,flags,isolatedDomainInterceptor:fault,isolatedClock:clock))using(var client=await Client("qa-core-turn@example.invalid",expired))
            await Deny("expiry during final SQL save rolls back whole completion",410,()=>client.PostAsJsonAsync(Complete(deadline.Session,nonce),envelope));
        if(fault.Faults!=1)throw new InvalidOperationException("Completion expiry fault not observed.");clock.Value=originalTime;
        var privacy=await Fixture();claim=await Admit(privacy.Session,privacy.Context,1);nonce=claim["requestId"]!.GetValue<Guid>();var privateFields=new Dictionary<string,object>(fields){["MedicalOrAccessibilityNotes"]=Text("Synthetic private note")};
        var privacyNonce=nonce;var privacyClaim=claim.DeepClone().AsObject();
        await Deny("medical proposal rollback retains pending claim",409,()=>admin.PostAsJsonAsync(Complete(privacy.Session,nonce),Envelope(claim,Decision(privacy.Context,claim,"clarify",privateFields))));
        privateFields.Remove("MedicalOrAccessibilityNotes");privateFields["BranchId"]=new{kind="reference",value=foreignBranch.ToString("D")};
        await Deny("foreign reference proposal cannot commit cursor or draft",409,()=>admin.PostAsJsonAsync(Complete(privacy.Session,nonce),Envelope(claim,Decision(privacy.Context,claim,"clarify",privateFields))));
        clock.Value=originalTime.AddMinutes(11);await Deny("expired claim cannot complete",410,()=>admin.PostAsJsonAsync(Complete(privacy.Session,nonce),Envelope(claim,Decision(privacy.Context,claim,"clarify",fields))));clock.Value=originalTime;
        var competing=await Fixture();claim=await Admit(competing.Session,competing.Context,1);nonce=claim["requestId"]!.GetValue<Guid>();var a=Envelope(claim,Decision(competing.Context,claim,"clarify",fields));var b=a.DeepClone().AsObject();b["decision"]!["candidates"]![0]!["edit"]!["set"]!["StudentFirstName"]!["value"]="Competing";
        responses=await Task.WhenAll(admin.PostAsJsonAsync(Complete(competing.Session,nonce),a),admin.PostAsJsonAsync(Complete(competing.Session,nonce),b));
        try{if(!responses.Select(x=>(int)x.StatusCode).Order().SequenceEqual(new[]{201,409}))throw new InvalidOperationException("Competing plans must be201/409 never429.");}finally{foreach(var response in responses)response.Dispose();}Pass("competing decisions same claim have one committed winner");
        using(var scope=factory.Services.CreateScope()){var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();var session=await db.PentaMiniSessions.SingleAsync(x=>x.Id==privacy.Session);session.Version=1;await db.SaveChangesAsync();}
        await Deny("changed retained context denies completion",409,()=>admin.PostAsJsonAsync(Complete(privacy.Session,privacyNonce),Envelope(privacyClaim,Decision(privacy.Context,privacyClaim,"clarify",fields))));
        using(var scope=factory.Services.CreateScope()){var identity=scope.ServiceProvider.GetRequiredService<IdentityDbContext>();(await identity.Users.SingleAsync(x=>x.Id==actor)).ConcurrencyStamp=Guid.NewGuid().ToString("N");await identity.SaveChangesAsync();}
        await Deny("changed current authority denies result replay",409,()=>admin.GetAsync(Result(competing.Session,nonce)));
        if(initialDomain!=await Snapshot(true))throw new InvalidOperationException("Draft turn completion changed domain/accounts.");Pass("all claimed adapters retain domain and account snapshots");
        if(count!=46)throw new InvalidOperationException("Incomplete turn-completion matrix: "+count);
        var directory=Path.Combine(Directory.GetCurrentDirectory(),"QA","EVIDENCE","penta-onboarding-turn-"+manifest.RunId.ToString("N"));Directory.CreateDirectory(directory);
        await File.WriteAllTextAsync(Path.Combine(directory,"core-turn-partial.json"),JsonSerializer.Serialize(new{journeys=partialJourneys}));
        using var retained=JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(Directory.GetCurrentDirectory(),"QA","EVIDENCE","penta-onboarding-partial-"+manifest.RunId.ToString("N"),"core-partial.json")));
        var combined=retained.RootElement.GetProperty("journeys").EnumerateArray().Select(x=>x.Clone()).Concat(partialJourneys.Select(x=>JsonSerializer.SerializeToElement(x))).ToArray();
        if(partialJourneys.Count!=2||combined.Length!=7)throw new InvalidOperationException("Incomplete recorded turn compatibility journeys.");
        await File.WriteAllTextAsync(Path.Combine(directory,"core-partial.json"),JsonSerializer.Serialize(new{journeys=combined}));
        Console.WriteLine("PASS: PENTAONBOARDINGTURN all "+count+" HTTP/real SQL cases; atomic draft-only completion, no inference/approval/domain execution activation.");
    }
}
