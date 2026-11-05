using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Intelligence.Penta;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

internal static partial class SqlHarnessEntryPoint
{
    private static async Task VerifyPentaOnboardingConversationAsync(QaApiFactory baseline,QaRunManifest manifest)
    {
        var flags=new Dictionary<string,string?> { ["Penta:OperationalDraftContractsEnabled"]="true",
            ["Penta:OperationalOnboardingHttpContractsEnabled"]="true",["Penta:OperationalOnboardingExecutionContractsEnabled"]="true",
            ["Penta:Enabled"]="true",["Penta:Mini:Enabled"]="true" };
        var clock=new OnboardingDraftClock(); var planner=new MiniFaultProvider();
        using var factory=new QaApiFactory(manifest,flags,isolatedClock:clock,isolatedMiniProvider:planner);
        Guid academy,foreign,foreignBranch;
        using(var scope=factory.Services.CreateScope()) {
            var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            academy=await db.Academies.Where(x=>x.Name=="Synthetic Academy A").Select(x=>x.Id).SingleAsync();
            foreign=await db.Academies.Where(x=>x.Name=="Synthetic Academy B").Select(x=>x.Id).SingleAsync();
            foreignBranch=await db.Branches.Where(x=>x.AcademyId==foreign).Select(x=>x.Id).FirstAsync();
        }
        var actor=await CreateAccessActorAsync(factory,"qa-core-conversation@example.invalid","AcademyAdmin",academy);
        async Task<HttpClient> Client(string email,QaApiFactory? host=null) {
            var client=(host??factory).CreateClient();
            client.DefaultRequestHeaders.Authorization=new AuthenticationHeaderValue("Bearer",await LoginAsync(client,email,"Synthetic!39Ab")); return client;
        }
        using var admin=await Client("qa-core-conversation@example.invalid");
        using var teacher=await Client("qa-teacher-a@example.invalid");
        using var other=await Client("qa-admin-a@example.invalid");
        using var anonymous=factory.CreateClient();
        string Route(Guid session,Guid? tenant=null)=>$"/api/academies/{tenant??academy}/penta/onboarding/conversations/{session}";
        string DraftRoute(Guid session,Guid draft)=>Route(session)+$"/drafts/{draft}";
        async Task<(Guid Session,Guid Draft,JsonElement View,string ReadState)> Fixture(object? fields=null,Guid? sessionId=null) {
            Guid session; string readState;
            using(var scope=factory.Services.CreateScope()) {
                var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
                if(sessionId is { } existing) { var row=await db.PentaMiniSessions.SingleAsync(x=>x.Id==existing); session=existing; readState=row.ProtectedState; }
                else {
                    var row=new PentaMiniSession { AcademyId=academy,ActorUserId=actor,Version=0,ExpiresAtUtc=clock.Value.UtcDateTime.AddHours(1),ProtectedState="" };
                    row.ProtectedState=scope.ServiceProvider.GetRequiredService<IDataProtectionProvider>().CreateProtector("PentaMini.local.v1",academy.ToString("D"),actor.ToString("D"),row.Id.ToString("D"))
                        .Protect(JsonSerializer.Serialize(MiniState.Empty));
                    db.PentaMiniSessions.Add(row); await db.SaveChangesAsync(); session=row.Id; readState=row.ProtectedState;
                }
            }
            // Keep contract-owned PascalCase field keys; HttpClient's Web JSON
            // defaults would otherwise camel-case anonymous object properties.
            var exactFields=JsonSerializer.SerializeToElement(fields??new { });
            var draft=Guid.NewGuid(); using var response=await admin.PutAsJsonAsync(DraftRoute(session,draft),new { expectedRevision=0,contextVersion=0,fields=exactFields });
            if((int)response.StatusCode!=201) throw new InvalidOperationException("Conversation fixture save failed: "+(int)response.StatusCode);
            return(session,draft,await response.Content.ReadFromJsonAsync<JsonElement>(),readState);
        }
        JsonObject Select(Guid draft,JsonElement view,long version=0,Guid? request=null)=>JsonSerializer.SerializeToNode(new {
            requestId=request??Guid.NewGuid(),expectedVersion=version,draftId=draft,draftRevision=view.GetProperty("revision").GetInt64(),payloadHash=view.GetProperty("payloadHash").GetString() })!.AsObject();
        JsonObject Claim(JsonElement view,long version,Guid? request=null)=>JsonSerializer.SerializeToNode(new {
            requestId=request??Guid.NewGuid(),expectedVersion=version,draftRevision=view.GetProperty("revision").GetInt64(),payloadHash=view.GetProperty("payloadHash").GetString(),text="SYNTHETIC_PRIVATE_PROMPT_20261018 continue" })!.AsObject();
        async Task<string> Snapshot(bool domainOnly=false) {
            using var scope=factory.Services.CreateScope(); var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); var identity=scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            return JsonSerializer.Serialize(new { students=await db.Students.AsNoTracking().OrderBy(x=>x.Id).ToArrayAsync(),guardians=await db.Guardians.AsNoTracking().OrderBy(x=>x.Id).ToArrayAsync(),
                links=await db.StudentGuardians.AsNoTracking().OrderBy(x=>x.Id).ToArrayAsync(),drafts=domainOnly?[]:await db.PentaOnboardingDrafts.AsNoTracking().OrderBy(x=>x.Id).ToArrayAsync(),
                sessions=domainOnly?[]:await db.PentaMiniSessions.AsNoTracking().OrderBy(x=>x.Id).ToArrayAsync(),turns=domainOnly?[]:await db.PentaMiniTurns.AsNoTracking().OrderBy(x=>x.Id).ToArrayAsync(),
                audits=domainOnly?[]:await db.AuditLogs.AsNoTracking().OrderBy(x=>x.Id).ToArrayAsync(),
                users=await identity.Users.AsNoTracking().OrderBy(x=>x.Id).Select(x=>new {x.Id,x.AcademyId,x.UserName}).ToArrayAsync(),roles=await identity.UserRoles.AsNoTracking().OrderBy(x=>x.UserId).ThenBy(x=>x.RoleId).ToArrayAsync() });
        }
        var count=0; void Pass(string label) {count++;Console.WriteLine("PASS: PENTAONBOARDINGCONVERSATION "+label);}
        async Task Deny(string label,int expected,Func<Task<HttpResponseMessage>> call) {
            var before=await Snapshot();using var response=await call();
            if((int)response.StatusCode!=expected||before!=await Snapshot()) throw new InvalidOperationException("Conversation denial "+label+" expected="+expected+" actual="+(int)response.StatusCode);
            Pass(label+" status="+expected+" no state changes");
        }
        async Task<JsonElement> Get(Guid session,HttpClient? client=null) {
            using var response=await (client??admin).GetAsync(Route(session)+"/current");
            if((int)response.StatusCode!=200) throw new InvalidOperationException("Current cursor failed: "+(int)response.StatusCode);
            if(response.Headers.CacheControl?.NoStore!=true) throw new InvalidOperationException("Cursor may be cached.");
            return await response.Content.ReadFromJsonAsync<JsonElement>();
        }
        var first=await Fixture();var second=await Fixture(sessionId:first.Session);var select=Select(first.Draft,first.View);
        await Deny("anonymous",401,()=>anonymous.PostAsJsonAsync(Route(first.Session)+"/selection",select));
        await Deny("teacher",403,()=>teacher.PostAsJsonAsync(Route(first.Session)+"/selection",select));
        await Deny("foreign tenant",403,()=>admin.PostAsJsonAsync(Route(first.Session,foreign)+"/selection",select));
        await Deny("other actor",404,()=>other.PostAsJsonAsync(Route(first.Session)+"/selection",select));
        using(var disabled=await Client("qa-core-conversation@example.invalid",baseline))
            await Deny("default disabled",404,()=>disabled.PostAsJsonAsync(Route(first.Session)+"/selection",select));
        var empty=await Get(first.Session);
        if(empty.GetProperty("version").GetInt64()!=0||empty.GetProperty("currentDraftId").ValueKind!=JsonValueKind.Null||empty.GetProperty("canExecute").GetBoolean()) throw new InvalidOperationException("Unselected cursor fabricated authority.");
        Pass("empty current cursor audited no execution authority");
        await Deny("unselected cannot claim",409,()=>admin.PostAsJsonAsync(Route(first.Session)+"/claims",Claim(first.View,0)));
        foreach(var kind in new[]{"revision","hash","version","approval","missing"}) {
            var bad=select.DeepClone().AsObject(); if(kind=="revision")bad["draftRevision"]=2;if(kind=="hash")bad["payloadHash"]=new string('a',64);
            if(kind=="version")bad["expectedVersion"]=1;if(kind=="approval")bad["approved"]=true;if(kind=="missing")bad["draftId"]=Guid.NewGuid().ToString("D");
            await Deny("selection "+kind,kind=="approval"?400:kind=="missing"?404:409,()=>admin.PostAsJsonAsync(Route(first.Session)+"/selection",bad));
        }
        var selectOther=Select(second.Draft,second.View);var domain=await Snapshot(true);
        var race=await Task.WhenAll(admin.PostAsJsonAsync(Route(first.Session)+"/selection",select),admin.PostAsJsonAsync(Route(first.Session)+"/selection",selectOther));
        JsonObject winner;JsonElement chosenView;Guid chosenDraft;JsonElement selected;
        try {
            if(!race.Select(x=>(int)x.StatusCode).Order().SequenceEqual(new[]{200,409}))throw new InvalidOperationException("Selection race must be200/409 never429.");
            var index=(int)race[0].StatusCode==200?0:1;winner=index==0?select:selectOther;chosenView=index==0?first.View:second.View;chosenDraft=index==0?first.Draft:second.Draft;
            selected=await race[index].Content.ReadFromJsonAsync<JsonElement>();
            if(selected.GetProperty("version").GetInt64()!=1||selected.GetProperty("currentDraftId").GetGuid()!=chosenDraft||domain!=await Snapshot(true))throw new InvalidOperationException("Selection race effect mismatch.");
        }finally{foreach(var response in race)response.Dispose();}
        Pass("competing selection200/409 one current draft no domain effect");
        var beforeReplay=await Snapshot();using(var replay=await admin.PostAsJsonAsync(Route(first.Session)+"/selection",winner)) {
            if((int)replay.StatusCode!=200||beforeReplay!=await Snapshot())throw new InvalidOperationException("Selection replay rewrote state/audit/expiry.");
        }Pass("exact selection replay no writes");
        var changedSelection=winner.DeepClone().AsObject();changedSelection["draftId"]=Guid.NewGuid().ToString("D");
        await Deny("selection request cannot adopt missing target",404,()=>admin.PostAsJsonAsync(Route(first.Session)+"/selection",changedSelection));
        using(var restarted=new QaApiFactory(manifest,flags,isolatedClock:clock,isolatedMiniProvider:planner))
        using(var reload=await Client("qa-core-conversation@example.invalid",restarted)) {
            var current=await Get(first.Session,reload);if(current.GetProperty("currentDraftId").GetGuid()!=chosenDraft||current.GetProperty("version").GetInt64()!=1)throw new InvalidOperationException("Cursor did not survive host recreation.");
        }Pass("restart restores protected current selection");
        var switched=Select(first.Draft,first.View,1);using(var response=await admin.PostAsJsonAsync(Route(first.Session)+"/selection",switched)) {
            if((int)response.StatusCode!=200)throw new InvalidOperationException("Current selection switch failed.");
        }Pass("explicit CAS selection switch advances only operational version");
        await Deny("old selection replay",409,()=>admin.PostAsJsonAsync(Route(first.Session)+"/selection",winner));
        var reused=Claim(first.View,2,winner["requestId"]!.GetValue<Guid>());
        await Deny("selection nonce cannot claim",409,()=>admin.PostAsJsonAsync(Route(first.Session)+"/claims",reused));
        var prompt=Claim(first.View,2);var promptOther=Claim(first.View,2);promptOther["text"]="SYNTHETIC alternative";
        race=await Task.WhenAll(admin.PostAsJsonAsync(Route(first.Session)+"/claims",prompt),admin.PostAsJsonAsync(Route(first.Session)+"/claims",promptOther));
        JsonObject claimed;
        try {
            if(!race.Select(x=>(int)x.StatusCode).Order().SequenceEqual(new[]{202,409}))throw new InvalidOperationException("Claim race must be202/409 never429.");
            var index=(int)race[0].StatusCode==202?0:1;claimed=index==0?prompt:promptOther;
            var result=await race[index].Content.ReadFromJsonAsync<JsonElement>();if(result.GetProperty("status").GetString()!="pending"||result.GetProperty("snapshot").ValueKind!=JsonValueKind.Null||result.GetProperty("canExecute").GetBoolean())throw new InvalidOperationException("Claim exposed planning or execution authority.");
        }finally{foreach(var response in race)response.Dispose();}
        Pass("competing prompt202/409 durable one winner no dispatch");
        using(var scope=factory.Services.CreateScope()) {
            var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();var row=await db.PentaMiniSessions.SingleAsync(x=>x.Id==first.Session);
            var turns=await db.PentaMiniTurns.Where(x=>x.SessionId==first.Session).ToArrayAsync();
            if(row.Version!=0||row.ProtectedState!=first.ReadState||row.PendingRequestId is null||turns.Count(x=>x.Status=="OnboardingPending")!=1||
                await db.AuditLogs.CountAsync(x=>x.EntityId==first.Session&&x.Action=="PentaOnboardingConversationClaimed")!=1||
                JsonSerializer.Serialize(turns).Contains("SYNTHETIC",StringComparison.Ordinal)||row.ProtectedOnboardingState!.Contains(chosenDraft.ToString("D"),StringComparison.Ordinal))throw new InvalidOperationException("Claim privacy/read-only preservation failed.");
        }Pass("one claim audit encrypted digest no raw prompt read-only bytes/version unchanged");
        await Deny("pending replay never redispatches",409,()=>admin.PostAsJsonAsync(Route(first.Session)+"/claims",claimed));
        var altered=claimed.DeepClone().AsObject();altered["text"]="changed";
        await Deny("changed prompt ID rejected",409,()=>admin.PostAsJsonAsync(Route(first.Session)+"/claims",altered));
        await Deny("new request cannot replace unknown claim",409,()=>admin.PostAsJsonAsync(Route(first.Session)+"/claims",Claim(first.View,3)));
        await Deny("selection cannot steal pending turn",409,()=>admin.PostAsJsonAsync(Route(first.Session)+"/selection",Select(first.Draft,first.View,3)));
        await Deny("legacy save cannot race pending turn",409,()=>admin.PutAsJsonAsync(DraftRoute(first.Session,first.Draft),new {expectedRevision=1,contextVersion=0,fields=new {}}));
        using(var restarted=new QaApiFactory(manifest,flags,isolatedClock:clock,isolatedMiniProvider:planner))
        using(var reload=await Client("qa-core-conversation@example.invalid",restarted)) {
            var pending=await Get(first.Session,reload);if(pending.GetProperty("status").GetString()!="pending"||pending.GetProperty("version").GetInt64()!=3)throw new InvalidOperationException("Pending claim lost across host recreation.");
            await Deny("restart cannot reclaim unknown turn",409,()=>reload.PostAsJsonAsync(Route(first.Session)+"/claims",claimed));
        }
        await Deny("read-only Mini cannot adopt operational conversation",409,()=>admin.PostAsJsonAsync($"/api/academies/{academy}/penta/chat/conversations/{first.Session}/turns",new {requestId=Guid.NewGuid(),expectedVersion=0,text="find students",capability="executor"}));
        if(planner.Calls!=0)throw new InvalidOperationException("Admission dispatched inference.");Pass("no admission or mode collision invokes planner");

        var rollback=await Fixture();var rollbackSelection=Select(rollback.Draft,rollback.View);var fault=new OnboardingExecutionFault("audit");
        using(var broken=new QaApiFactory(manifest,flags,isolatedDomainInterceptor:fault,isolatedClock:clock))
        using(var client=await Client("qa-core-conversation@example.invalid",broken))
            await Deny("selection audit failure rolls back pointer and turn",503,()=>client.PostAsJsonAsync(Route(rollback.Session)+"/selection",rollbackSelection));
        if(fault.Faults!=1)throw new InvalidOperationException("Selection audit fault not observed.");
        using(var response=await admin.PostAsJsonAsync(Route(rollback.Session)+"/selection",rollbackSelection))if((int)response.StatusCode!=200)throw new InvalidOperationException("Verified rollback selection failed.");
        Pass("verified rolled-back selection can be explicitly submitted");
        var rollbackClaim=Claim(rollback.View,1);fault=new OnboardingExecutionFault("audit");
        using(var broken=new QaApiFactory(manifest,flags,isolatedDomainInterceptor:fault,isolatedClock:clock))
        using(var client=await Client("qa-core-conversation@example.invalid",broken))
            await Deny("claim audit failure rolls back pending and digest",503,()=>client.PostAsJsonAsync(Route(rollback.Session)+"/claims",rollbackClaim));
        if(fault.Faults!=1)throw new InvalidOperationException("Claim audit fault not observed.");
        using(var response=await admin.PostAsJsonAsync(Route(rollback.Session)+"/claims",rollbackClaim))if((int)response.StatusCode!=202)throw new InvalidOperationException("Verified rollback claim failed.");
        Pass("verified rollback leaves no phantom pending claim");

        var tamper=await Fixture();using(var scope=factory.Services.CreateScope()) {
            var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();var row=await db.PentaMiniSessions.SingleAsync(x=>x.Id==tamper.Session);
            row.ProtectedOnboardingState=await db.PentaMiniSessions.Where(x=>x.Id==first.Session).Select(x=>x.ProtectedOnboardingState).SingleAsync(); await db.SaveChangesAsync();
        }
        await Deny("copied ciphertext cannot select another conversation",409,()=>admin.PostAsJsonAsync(Route(tamper.Session)+"/selection",Select(tamper.Draft,tamper.View)));
        await Deny("corrupt cursor read fails closed",409,()=>admin.GetAsync(Route(tamper.Session)+"/current"));
        var stale=await Fixture();using(var response=await admin.PostAsJsonAsync(Route(stale.Session)+"/selection",Select(stale.Draft,stale.View)))if((int)response.StatusCode!=200)throw new InvalidOperationException("Stale fixture selection failed.");
        using(var scope=factory.Services.CreateScope()) {var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();(await db.PentaMiniSessions.SingleAsync(x=>x.Id==stale.Session)).Version=1;await db.SaveChangesAsync();}
        await Deny("read context change invalidates operational cursor",409,()=>admin.PostAsJsonAsync(Route(stale.Session)+"/claims",Claim(stale.View,1)));
        var secure=await Fixture(new {MedicalOrAccessibilityNotes=new {kind="text",value="SYNTHETIC_SECURE_NOTE"}});
        await Deny("medical context selection cannot disclose or commit cursor",409,()=>admin.PostAsJsonAsync(Route(secure.Session)+"/selection",Select(secure.Draft,secure.View)));
        var foreignRef=await Fixture(new {BranchId=new {kind="reference",value=foreignBranch.ToString("D")}});
        await Deny("foreign reference selection fails before cursor commit",409,()=>admin.PostAsJsonAsync(Route(foreignRef.Session)+"/selection",Select(foreignRef.Draft,foreignRef.View)));
        var expiry=await Fixture();var savedTime=clock.Value;clock.Value=clock.Value.AddMinutes(11);
        await Deny("expired draft cannot be selected",410,()=>admin.PostAsJsonAsync(Route(expiry.Session)+"/selection",Select(expiry.Draft,expiry.View)));
        clock.Value=savedTime.AddHours(2);await Deny("expired session current fails",410,()=>admin.GetAsync(Route(expiry.Session)+"/current"));clock.Value=savedTime;
        var readOnly=await Fixture();
        using(var response=await admin.PostAsJsonAsync($"/api/academies/{academy}/penta/chat/conversations/{readOnly.Session}/turns",new {requestId=Guid.NewGuid(),expectedVersion=0,text="find students",capability="executor"})) {
            if((int)response.StatusCode!=201||planner.Calls!=1)throw new InvalidOperationException("Separate read-only Mini conversation regressed: "+(int)response.StatusCode);
        }
        using(var scope=factory.Services.CreateScope()) {
            var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();var row=await db.PentaMiniSessions.SingleAsync(x=>x.Id==readOnly.Session);
            if(row.ProtectedOnboardingState is not null||row.Version!=1||row.PendingRequestId is not null)throw new InvalidOperationException("Read-only Mini adopted operational state.");
        }Pass("separate read-only Mini request retains its existing flow with synthetic unavailable planner");
        using(var scope=factory.Services.CreateScope()) {var identity=scope.ServiceProvider.GetRequiredService<IdentityDbContext>();(await identity.Users.SingleAsync(x=>x.Id==actor)).ConcurrencyStamp=Guid.NewGuid().ToString("N");await identity.SaveChangesAsync();}
        await Deny("current authority change denies selected cursor",409,()=>admin.GetAsync(Route(rollback.Session)+"/current"));
        if(domain!=await Snapshot(true))throw new InvalidOperationException("Conversation admissions changed domain/accounts.");
        Pass("all admissions leave domain and accounts unchanged");
        if(count!=43)throw new InvalidOperationException("Incomplete conversation matrix: "+count);
        Console.WriteLine("PASS: PENTAONBOARDINGCONVERSATION all "+count+" HTTP/real SQL cases; durable admission only, no inference/completion activation.");
    }
}
