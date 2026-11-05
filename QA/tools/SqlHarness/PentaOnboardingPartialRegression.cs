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
    private static async Task VerifyPentaOnboardingPartialAsync(QaApiFactory baseline, QaRunManifest manifest)
    {
        var flags = new Dictionary<string,string?> { ["Penta:OperationalDraftContractsEnabled"]="true",
            ["Penta:OperationalOnboardingHttpContractsEnabled"]="true", ["Penta:OperationalOnboardingExecutionContractsEnabled"]="true" };
        var clock = new OnboardingDraftClock();
        using var factory = new QaApiFactory(manifest,flags,isolatedClock:clock);
        Guid academy, foreign, session, foreignBranch;
        using (var scope = factory.Services.CreateScope()) {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            academy = await db.Academies.Where(x => x.Name=="Synthetic Academy A").Select(x=>x.Id).SingleAsync();
            foreign = await db.Academies.Where(x => x.Name=="Synthetic Academy B").Select(x=>x.Id).SingleAsync();
            foreignBranch = await db.Branches.Where(x=>x.AcademyId==foreign).Select(x=>x.Id).FirstAsync();
        }
        var actor = await CreateAccessActorAsync(factory,"qa-core-partial@example.invalid","AcademyAdmin",academy);
        using (var scope = factory.Services.CreateScope()) {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var row = new PentaMiniSession { AcademyId=academy,ActorUserId=actor,Version=0,ExpiresAtUtc=clock.Value.UtcDateTime.AddHours(1),ProtectedState="Synthetic partial fixture; no inference" };
            db.PentaMiniSessions.Add(row); await db.SaveChangesAsync(); session=row.Id;
        }
        async Task<HttpClient> Client(string email,QaApiFactory? host=null) {
            var client=(host??factory).CreateClient();
            client.DefaultRequestHeaders.Authorization=new AuthenticationHeaderValue("Bearer",await LoginAsync(client,email,"Synthetic!39Ab")); return client;
        }
        using var admin=await Client("qa-core-partial@example.invalid");
        using var teacher=await Client("qa-teacher-a@example.invalid");
        using var other=await Client("qa-admin-a@example.invalid");
        using var anonymous=factory.CreateClient();
        string Route(Guid id,Guid? tenant=null)=>$"/api/academies/{tenant??academy}/penta/onboarding/conversations/{session}/drafts/{id}";
        string PartialRoute(Guid id,Guid request,Guid? tenant=null)=>Route(id,tenant)+$"/core/requests/{request}/partial";
        async Task<Guid> Save() {
            var id=Guid.NewGuid(); using var response=await admin.PutAsJsonAsync(Route(id),new { expectedRevision=0,contextVersion=0,fields=new { } });
            if ((int)response.StatusCode!=201) throw new InvalidOperationException("Partial fixture save failed."); return id;
        }
        async Task<JsonElement> Current(Guid id) {
            using var response=await admin.GetAsync(Route(id)+"/core-context");
            if ((int)response.StatusCode!=200) throw new InvalidOperationException("Partial current failed: "+(int)response.StatusCode);
            return await response.Content.ReadFromJsonAsync<JsonElement>();
        }
        JsonObject Decision(JsonElement current,Guid request,object fields,string[]? questions=null)=>JsonSerializer.SerializeToNode(new {
            request_ref=request.ToString("D"),scope=current.GetProperty("scope"),base_revision=current.GetProperty("draft").GetProperty("revision").GetInt64(),catalog_revision=1,kind="clarify",
            candidates=new[] { new { tool_key=PentaOnboardingDraftService.Tool,edit=new { mode="replace",set=fields,remove=Array.Empty<string>() } } },
            questions=questions??["StudentLastName","DateOfBirth"],invalidates_review=true })!.AsObject();
        object First(string name="Synthetic")=>new Dictionary<string,object> { ["StudentFirstName"]=new { kind="text",value=name },["AllowFinance"]=new { kind="boolean",value=false } };
        async Task<string> Snapshot(bool includeDrafts=true) {
            using var scope=factory.Services.CreateScope(); var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); var identity=scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            return JsonSerializer.Serialize(new { students=await db.Students.AsNoTracking().OrderBy(x=>x.Id).ToArrayAsync(),guardians=await db.Guardians.AsNoTracking().OrderBy(x=>x.Id).ToArrayAsync(),
                links=await db.StudentGuardians.AsNoTracking().OrderBy(x=>x.Id).ToArrayAsync(),drafts=includeDrafts?await db.PentaOnboardingDrafts.AsNoTracking().OrderBy(x=>x.Id).ToArrayAsync():[],
                audits=includeDrafts?await db.AuditLogs.AsNoTracking().OrderBy(x=>x.Id).ToArrayAsync():[],
                users=await identity.Users.AsNoTracking().OrderBy(x=>x.Id).Select(x=>new { x.Id,x.AcademyId,x.UserName }).ToArrayAsync(),roles=await identity.UserRoles.AsNoTracking().OrderBy(x=>x.UserId).ThenBy(x=>x.RoleId).ToArrayAsync() });
        }
        var count=0; var journeys=new List<object>();
        void Pass(string label) { count++; Console.WriteLine("PASS: PENTAONBOARDINGPARTIAL "+label); }
        async Task Deny(string label,int expected,Func<Task<HttpResponseMessage>> call) {
            var before=await Snapshot(); using var response=await call();
            if ((int)response.StatusCode!=expected || before!=await Snapshot()) throw new InvalidOperationException("Partial denial "+label+" expected="+expected+" actual="+(int)response.StatusCode);
            Pass(label+" status="+expected+" no state changes");
        }
        async Task<JsonElement> Apply(Guid id,JsonElement before,object fields,string[] questions) {
            var nonce=Guid.NewGuid(); var decision=Decision(before,nonce,fields,questions); var domain=await Snapshot(false);
            using var response=await admin.PostAsJsonAsync(PartialRoute(id,nonce),decision);
            if ((int)response.StatusCode!=200) throw new InvalidOperationException("Partial apply failed: "+(int)response.StatusCode);
            var after=await response.Content.ReadFromJsonAsync<JsonElement>();
            if (domain!=await Snapshot(false) || after.GetProperty("draft").GetProperty("status").GetString()!="collecting" ||
                after.GetProperty("draft").GetProperty("operation_ref").ValueKind!=JsonValueKind.Null ||
                after.GetProperty("draft").GetProperty("revision").GetInt64()!=before.GetProperty("draft").GetProperty("revision").GetInt64()+1)
                throw new InvalidOperationException("Partial mutated domain or retained approval.");
            journeys.Add(new { before,decision,after }); return after;
        }
        var id=await Save(); var current=await Current(id); var nonce=Guid.NewGuid(); var decision=Decision(current,nonce,First());
        await Deny("anonymous",401,()=>anonymous.PostAsJsonAsync(PartialRoute(id,nonce),decision));
        await Deny("teacher",403,()=>teacher.PostAsJsonAsync(PartialRoute(id,nonce),decision));
        await Deny("foreign tenant",403,()=>admin.PostAsJsonAsync(PartialRoute(id,nonce,foreign),decision));
        await Deny("other actor",404,()=>other.PostAsJsonAsync(PartialRoute(id,nonce),decision));
        using (var disabled=await Client("qa-core-partial@example.invalid",baseline))
            await Deny("default disabled",404,()=>disabled.PostAsJsonAsync(PartialRoute(id,nonce),decision));
        foreach (var change in new[] { "scope","base","catalog","approval","questions","present-question","credentials","tool" }) {
            var bad=decision.DeepClone().AsObject();
            if (change=="scope") bad["scope"]!["actor_id"]=Guid.NewGuid().ToString("D");
            if (change=="base") bad["base_revision"]=2;
            if (change=="catalog") bad["catalog_revision"]=2;
            if (change=="approval") bad["approved"]=true;
            if (change=="questions") bad["questions"]=new JsonArray("GrantRole");
            if (change=="present-question") bad["questions"]=new JsonArray("StudentFirstName");
            if (change=="credentials") bad["candidates"]![0]!["edit"]!["set"]!["StudentTemporaryPassword"]=JsonSerializer.SerializeToNode(new { kind="text",value="synthetic-only" });
            if (change=="tool") bad["candidates"]![0]!["tool_key"]="ExecuteSQL";
            await Deny("untrusted "+change,change is "scope" or "base"?409:400,()=>admin.PostAsJsonAsync(PartialRoute(id,nonce),bad));
        }
        var domainBefore=await Snapshot(false); var responses=await Task.WhenAll(admin.PostAsJsonAsync(PartialRoute(id,nonce),decision),admin.PostAsJsonAsync(PartialRoute(id,nonce),decision));
        JsonElement partial;
        try {
            if (responses.Any(x=>(int)x.StatusCode!=200)) throw new InvalidOperationException("Parallel partial must be200/200 never429.");
            partial=await responses[0].Content.ReadFromJsonAsync<JsonElement>();
            if (!JsonElement.DeepEquals(partial,await responses[1].Content.ReadFromJsonAsync<JsonElement>()) || domainBefore!=await Snapshot(false)) throw new InvalidOperationException("Partial replay/domain mismatch.");
        } finally { foreach (var response in responses) response.Dispose(); }
        using (var scope=factory.Services.CreateScope()) {
            var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); var row=await db.PentaOnboardingDrafts.SingleAsync(x=>x.Id==id);
            if (row.Revision!=2 || row.Status!="AwaitingReview" || await db.AuditLogs.CountAsync(x=>x.EntityId==id && x.Action=="PentaOnboardingDraftCorePartialSaved")!=1)
                throw new InvalidOperationException("Partial CAS/audit duplicated.");
        }
        journeys.Add(new { before=current,decision,after=partial });
        Pass("parallel same request exact snapshot, one revision/audit, no domain or Identity writes");
        var stable=await Snapshot(); clock.Value=clock.Value.AddSeconds(1);
        using (var restarted=new QaApiFactory(manifest,flags,isolatedClock:clock))
        using (var client=await Client("qa-core-partial@example.invalid",restarted))
        using (var response=await client.PostAsJsonAsync(PartialRoute(id,nonce),decision))
            if ((int)response.StatusCode!=200 || !JsonElement.DeepEquals(partial,await response.Content.ReadFromJsonAsync<JsonElement>()) || stable!=await Snapshot()) throw new InvalidOperationException("Partial restart replay wrote state or extended TTL.");
        Pass("fresh-host replay identical, no write/audit/TTL extension");
        var changed=decision.DeepClone().AsObject(); changed["candidates"]![0]!["edit"]!["set"]!["StudentFirstName"]!["value"]="Changed";
        await Deny("same nonce changed fields",409,()=>admin.PostAsJsonAsync(PartialRoute(id,nonce),changed));
        changed=decision.DeepClone().AsObject(); changed["questions"]=new JsonArray("DateOfBirth");
        await Deny("same nonce changed questions",409,()=>admin.PostAsJsonAsync(PartialRoute(id,nonce),changed));
        var retained=new Dictionary<string,object> { ["StudentFirstName"]=new { kind="text",value="Synthetic" },["StudentLastName"]=new { kind="text",value="Partial final student" },["StudentEmail"]=new { kind="text",value="optional@example.invalid" },["AllowFinance"]=new { kind="boolean",value=false } };
        var next=await Apply(id,partial,retained,["DateOfBirth"]);
        if (next.GetProperty("draft").GetProperty("fields").GetProperty("StudentFirstName").GetProperty("value").GetString()!="Synthetic" || next.GetProperty("draft").GetProperty("fields").GetProperty("AllowFinance").GetProperty("value").GetBoolean()) throw new InvalidOperationException("Partial retained fields/explicit false lost.");
        Pass("next turn retains names and explicit false while asking only DOB");
        await Deny("old request after later turn",409,()=>admin.PostAsJsonAsync(PartialRoute(id,nonce),decision));
        var reused=Decision(next,nonce,retained,["DateOfBirth"]);
        await Deny("nonce cannot be reused at later revision",409,()=>admin.PostAsJsonAsync(PartialRoute(id,nonce),reused));
        var crossKind=Decision(next,nonce,new { }); crossKind["kind"]="request_cancel"; crossKind["candidates"]=new JsonArray(); crossKind["questions"]=new JsonArray();
        await Deny("partial nonce cannot become lifecycle command",409,()=>admin.PostAsJsonAsync(Route(id)+$"/core/requests/{nonce}/transition",crossKind));
        retained.Remove("StudentEmail"); var cleared=await Apply(id,next,retained,["DateOfBirth"]);
        if (cleared.GetProperty("draft").GetProperty("fields").TryGetProperty("StudentEmail",out _)) throw new InvalidOperationException("Full replacement did not clear omitted optional field.");
        Pass("explicit full replacement clears omitted optional field without hidden merge");
        var complete=JsonNode.Parse(cleared.GetProperty("draft").GetProperty("fields").GetRawText())!.AsObject(); complete["DateOfBirth"]=JsonSerializer.SerializeToNode(new { kind="date",value="2000-01-01" });
        var completeNonce=Guid.NewGuid();
        await Deny("complete payload must use preparation",400,()=>admin.PostAsJsonAsync(PartialRoute(id,completeNonce),Decision(cleared,completeNonce,complete,["DateOfBirth"])));
        var prepareNonce=Guid.NewGuid(); var prepare=Decision(cleared,prepareNonce,complete); prepare["kind"]="host_prepare"; prepare["questions"]=new JsonArray();
        var reusedPreparation=prepare.DeepClone().AsObject(); reusedPreparation["request_ref"]=nonce.ToString("D");
        await Deny("partial nonce cannot become preparation",409,()=>admin.PostAsJsonAsync(Route(id)+$"/core/requests/{nonce}/prepare",reusedPreparation));
        JsonElement operation;
        using (var response=await admin.PostAsJsonAsync(Route(id)+$"/core/requests/{prepareNonce}/prepare",prepare)) {
            if ((int)response.StatusCode!=201) throw new InvalidOperationException("Partial-to-preparation journey failed."); operation=(await response.Content.ReadFromJsonAsync<JsonElement>())[0].Clone();
        }
        var approved=await ConfirmPreparedOnboardingAsync(admin,Route(id),operation); var approvedCurrent=await Current(id);
        var revised=await Apply(id,approvedCurrent,retained,["DateOfBirth"]);
        await Deny("partial edit invalidates issued creation approval",409,()=>admin.PostAsJsonAsync(Route(id)+"/core/execute",CoreOnboardingBinding(approved,true)));
        Pass("approved draft can return to collecting only by revisioned partial replacement, no domain writes");
        var revisedPayload=JsonSerializer.Serialize(new { tool_key=PentaOnboardingDraftService.Tool,tool_revision=1,fields=revised.GetProperty("draft").GetProperty("fields") });
        await Deny("partial-origin cannot use legacy authorization",409,()=>admin.PostAsJsonAsync(Route(id)+"/authorize-creation",new { revision=revised.GetProperty("draft").GetProperty("revision").GetInt64(),payloadHash=PentaOperationalPayload.Digest(revisedPayload),intent=PentaOnboardingDraftService.CreationIntent }));
        var minorId=await Save(); var minorCurrent=await Current(minorId);
        var minorFields=new Dictionary<string,object> { ["StudentFirstName"]=new { kind="text",value="Synthetic" },["StudentLastName"]=new { kind="text",value="Partial minor" },["DateOfBirth"]=new { kind="date",value="2015-01-01" } };
        var minor=await Apply(minorId,minorCurrent,minorFields,["ParentFirstName","ParentLastName","ParentEmail"]);
        if (!minor.GetProperty("draft").GetProperty("fields").GetProperty("AllowParentPortalAccess").GetProperty("value").GetBoolean()) throw new InvalidOperationException("Shared minor default absent.");
        Pass("minor DOB activates shared required guardian fields and domain portal-access default");
        var medicalId=await Save(); var medicalCurrent=await Current(medicalId); var medicalNonce=Guid.NewGuid();
        var medical=new Dictionary<string,object> { ["StudentFirstName"]=new { kind="text",value="Synthetic" },["MedicalOrAccessibilityNotes"]=new { kind="text",value="Synthetic private note" } };
        await Deny("medical text requires secure Manual step and no partial reservation",409,()=>admin.PostAsJsonAsync(PartialRoute(medicalId,medicalNonce),Decision(medicalCurrent,medicalNonce,medical)));
        medical.Remove("MedicalOrAccessibilityNotes"); medical["BranchId"]=new { kind="reference",value=foreignBranch.ToString("D") };
        await Deny("foreign reference cannot be saved or projected",409,()=>admin.PostAsJsonAsync(PartialRoute(medicalId,medicalNonce),Decision(medicalCurrent,medicalNonce,medical)));
        var rollbackId=await Save(); var rollbackCurrent=await Current(rollbackId); var rollbackNonce=Guid.NewGuid(); var rollback=Decision(rollbackCurrent,rollbackNonce,First());
        var fault=new OnboardingExecutionFault("audit");
        using (var faulty=new QaApiFactory(manifest,flags,isolatedDomainInterceptor:fault,isolatedClock:clock))
        using (var client=await Client("qa-core-partial@example.invalid",faulty)) {
            await Deny("required partial audit full rollback",503,()=>client.PostAsJsonAsync(PartialRoute(rollbackId,rollbackNonce),rollback));
            if (fault.Faults!=1) throw new InvalidOperationException("Partial audit failure not reached.");
        }
        using (var response=await admin.PostAsJsonAsync(PartialRoute(rollbackId,rollbackNonce),rollback)) if ((int)response.StatusCode!=200) throw new InvalidOperationException("Verified partial rollback explicit retry failed.");
        Pass("explicit retry after independently proven rollback saves once");
        var raceId=await Save(); var raceCurrent=await Current(raceId); var raceA=Guid.NewGuid(); var raceB=Guid.NewGuid();
        responses=await Task.WhenAll(admin.PostAsJsonAsync(PartialRoute(raceId,raceA),Decision(raceCurrent,raceA,First("Race A"))),admin.PostAsJsonAsync(PartialRoute(raceId,raceB),Decision(raceCurrent,raceB,First("Race B"))));
        try { if (!responses.Select(x=>(int)x.StatusCode).Order().SequenceEqual(new[] { 200,409 })) throw new InvalidOperationException("Different partial requests must be200/409 never429."); } finally { foreach(var response in responses) response.Dispose(); }
        Pass("competing partial CAS exactly one200 one409");
        var pausedId=await Save(); var pausedCurrent=await Current(pausedId); var pauseNonce=Guid.NewGuid(); var pause=Decision(pausedCurrent,pauseNonce,new { }); pause["kind"]="request_pause"; pause["candidates"]=new JsonArray(); pause["questions"]=new JsonArray();
        using (var response=await admin.PostAsJsonAsync(Route(pausedId)+$"/core/requests/{pauseNonce}/transition",pause)) if ((int)response.StatusCode!=200) throw new InvalidOperationException("Partial paused fixture failed.");
        var paused=await Current(pausedId); var pausedNonce=Guid.NewGuid();
        await Deny("paused partial blocked",409,()=>admin.PostAsJsonAsync(PartialRoute(pausedId,pausedNonce),Decision(paused,pausedNonce,First())));
        var cancelNonce=Guid.NewGuid(); var cancel=Decision(paused,cancelNonce,new { }); cancel["kind"]="request_cancel"; cancel["candidates"]=new JsonArray(); cancel["questions"]=new JsonArray();
        using (var response=await admin.PostAsJsonAsync(Route(pausedId)+$"/core/requests/{cancelNonce}/transition",cancel)) if ((int)response.StatusCode!=200) throw new InvalidOperationException("Partial cancelled fixture failed.");
        var cancelled=await Current(pausedId); var cancelledNonce=Guid.NewGuid();
        await Deny("cancelled partial blocked",409,()=>admin.PostAsJsonAsync(PartialRoute(pausedId,cancelledNonce),Decision(cancelled,cancelledNonce,First())));
        var expiredId=await Save(); var expired=await Current(expiredId); var expiredNonce=Guid.NewGuid(); var savedClock=clock.Value; clock.Value=clock.Value.AddMinutes(11);
        await Deny("expired partial cannot extend TTL",410,()=>admin.PostAsJsonAsync(PartialRoute(expiredId,expiredNonce),Decision(expired,expiredNonce,First()))); clock.Value=savedClock;
        using (var scope=factory.Services.CreateScope()) { var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); (await db.PentaMiniSessions.SingleAsync(x=>x.Id==session)).Version=1; await db.SaveChangesAsync(); }
        var staleNonce=Guid.NewGuid(); await Deny("stale conversation partial",409,()=>admin.PostAsJsonAsync(PartialRoute(rollbackId,staleNonce),Decision(rollbackCurrent,staleNonce,First())));
        using (var scope=factory.Services.CreateScope()) { var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); (await db.PentaMiniSessions.SingleAsync(x=>x.Id==session)).Version=0; await db.SaveChangesAsync(); }
        using (var scope=factory.Services.CreateScope()) { var identity=scope.ServiceProvider.GetRequiredService<IdentityDbContext>(); (await identity.Users.SingleAsync(x=>x.Id==actor)).ConcurrencyStamp=Guid.NewGuid().ToString("N"); await identity.SaveChangesAsync(); }
        await Deny("changed current authority denies replay",409,()=>admin.PostAsJsonAsync(PartialRoute(rollbackId,rollbackNonce),rollback));
        var directory=Path.Combine(Directory.GetCurrentDirectory(),"QA","EVIDENCE","penta-onboarding-partial-"+manifest.RunId.ToString("N"));
        Directory.CreateDirectory(directory); await File.WriteAllTextAsync(Path.Combine(directory,"core-partial.json"),JsonSerializer.Serialize(new { journeys }));
        if (count!=38) throw new InvalidOperationException("Incomplete partial matrix: "+count);
        Console.WriteLine("PASS: PENTAONBOARDINGPARTIAL all "+count+" HTTP/real SQL cases; no model or frontend activation.");
    }
}
