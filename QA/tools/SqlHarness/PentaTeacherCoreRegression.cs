using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Domain.Identity;
using AcademyDesk.Api.Intelligence.Penta;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

internal static partial class SqlHarnessEntryPoint
{
    private static async Task VerifyPentaTeacherCoreAsync(QaRunManifest manifest)
    {
        var flags=new Dictionary<string,string?>{["Penta:TeacherCreationContractsEnabled"]="true",["Penta:TeacherCoreContractsEnabled"]="true",
            ["Penta:OperationalDraftContractsEnabled"]="true",["Penta:OperationalOnboardingHttpContractsEnabled"]="true"};
        using var factory=new QaApiFactory(manifest,flags);Guid academy,foreign;
        using(var scope=factory.Services.CreateScope()){
            var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();academy=await db.Academies.Where(x=>x.Name=="Synthetic Academy A").Select(x=>x.Id).SingleAsync();
            foreign=await db.Academies.Where(x=>x.Name=="Synthetic Academy B").Select(x=>x.Id).SingleAsync();}
        var actor=await CreateAccessActorAsync(factory,"qa-teacher-core@example.invalid","AcademyAdmin",academy);
        await CreateAccessActorAsync(factory,"qa-teacher-core-other@example.invalid","AcademyAdmin",academy);
        Guid session;using(var scope=factory.Services.CreateScope()){
            var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();var row=new PentaMiniSession{AcademyId=academy,ActorUserId=actor,Version=1,ExpiresAtUtc=DateTime.UtcNow.AddHours(1),ProtectedState="Synthetic teacher Core fixture; no inference"};
            db.PentaMiniSessions.Add(row);await db.SaveChangesAsync();session=row.Id;}
        async Task<HttpClient> Client(string email,QaApiFactory? host=null){var c=(host??factory).CreateClient();var bearer=await LoginAsync(c,email,"Synthetic!39Ab");c.DefaultRequestHeaders.Authorization=new AuthenticationHeaderValue("Bearer",bearer);return c;}
        using var admin=await Client("qa-teacher-core@example.invalid");using var other=await Client("qa-teacher-core-other@example.invalid");using var teacher=await Client("qa-teacher-a@example.invalid");using var anonymous=factory.CreateClient();
        string Route(Guid draft,string suffix="",Guid? tenant=null)=>$"/api/academies/{tenant??academy}/penta/teachers/conversations/{session}/drafts/{draft}{suffix}";
        var count=0;void Pass(string label){count++;Console.WriteLine("PASS: PENTATEACHERCORE "+label);}
        async Task<string> Snapshot(){using var scope=factory.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();var identity=scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            return JsonSerializer.Serialize(new{teachers=await db.Teachers.AsNoTracking().OrderBy(x=>x.Id).ToArrayAsync(),drafts=await db.PentaOnboardingDrafts.AsNoTracking().OrderBy(x=>x.Id).ToArrayAsync(),
                sessions=await db.PentaMiniSessions.AsNoTracking().OrderBy(x=>x.Id).ToArrayAsync(),audits=await db.AuditLogs.AsNoTracking().OrderBy(x=>x.Id).ToArrayAsync(),
                users=await identity.Users.AsNoTracking().Select(x=>new{x.Id,x.AcademyId,x.IsActive,x.IsPlatformOwner,x.TeacherId}).OrderBy(x=>x.Id).ToArrayAsync(),
                roles=await identity.UserRoles.AsNoTracking().OrderBy(x=>x.UserId).ThenBy(x=>x.RoleId).ToArrayAsync(),students=await db.Students.AsNoTracking().OrderBy(x=>x.Id).ToArrayAsync(),payments=await db.Payments.AsNoTracking().OrderBy(x=>x.Id).ToArrayAsync()});}
        async Task Deny(string label,int expected,Func<Task<HttpResponseMessage>> call){var before=await Snapshot();using var response=await call();RequireFinanceStatus(response,(HttpStatusCode)expected,label);
            if(before!=await Snapshot())throw new InvalidOperationException("Teacher Core refusal changed scoped state.");Pass(label);}
        async Task<JsonElement> Value(Task<HttpResponseMessage> call,int expected,string label){using var response=await call;RequireFinanceStatus(response,(HttpStatusCode)expected,label);return await response.Content.ReadFromJsonAsync<JsonElement>();}
        async Task<PentaTeacherCreationView> Save(string last){using var response=await admin.PutAsJsonAsync(Route(Guid.NewGuid()),new{expectedRevision=0,contextVersion=1,
            fields=new Dictionary<string,object>{["FirstName"]=new{kind="text",value="CoreSynthetic"},["LastName"]=new{kind="text",value=last}}});RequireFinanceStatus(response,HttpStatusCode.Created,"seed teacher Core draft");return(await response.Content.ReadFromJsonAsync<PentaTeacherCreationView>())!;}
        var draft=await Save("CoreOne");
        await Deny("anonymous context denied",401,()=>anonymous.GetAsync(Route(draft.Id,"/core-context")));
        await Deny("Teacher cannot discover write catalog",403,()=>teacher.GetAsync(Route(draft.Id,"/core-context")));
        await Deny("foreign tenant cannot discover context",403,()=>admin.GetAsync(Route(draft.Id,"/core-context",foreign)));
        await Deny("other admin cannot adopt context",409,()=>other.GetAsync(Route(draft.Id,"/core-context")));
        var noCoreFlags=new Dictionary<string,string?>(flags){["Penta:TeacherCoreContractsEnabled"]="false"};
        using(var offHost=new QaApiFactory(manifest,noCoreFlags)){using var off=await Client("qa-teacher-core@example.invalid",offHost);await Deny("independent Core gate off",404,()=>off.GetAsync(Route(draft.Id,"/core-context")));}
        var current=await Value(admin.GetAsync(Route(draft.Id,"/core-context")),200,"current teacher context");
        var scopeJson=current.GetProperty("scope");var tools=current.GetProperty("tools");var tool=tools[0];
        if(tools.GetArrayLength()!=1||tool.GetProperty("family").GetString()!="AI-05"||!tool.GetProperty("approval_required").GetBoolean()||!tool.GetProperty("preparation_required").GetBoolean()||
            tool.GetProperty("max_items").GetInt32()!=1||tool.GetProperty("fields").GetArrayLength()!=14||scopeJson.GetProperty("actor_id").GetString()!=actor.ToString("D")||scopeJson.GetProperty("tenant_id").GetString()!=academy.ToString("D"))
            throw new InvalidOperationException("Teacher catalog not bound to current host authority.");Pass("current host scope and bounded fourteen-field AI05 descriptor");
        JsonObject Decision(Guid request,long revision,bool partial=false)=>JsonSerializer.SerializeToNode(new{request_ref=request.ToString("D"),scope=scopeJson,base_revision=revision,catalog_revision=1,
            kind=partial?"clarify":"host_prepare",questions=partial?new[]{"LastName"}:Array.Empty<string>(),invalidates_review=true,
            candidates=new[]{new{tool_key=PentaTeacherCreationFields.Tool,edit=new{mode="replace",set=partial?
                new Dictionary<string,object>{["FirstName"]=new{kind="text",value=" CoreSynthetic "}}:
                new Dictionary<string,object>{["FirstName"]=new{kind="text",value=" CoreSynthetic "},["LastName"]=new{kind="text",value=" CoreOne "}},remove=Array.Empty<string>()}}}})!.AsObject();
        var partialId=Guid.NewGuid();var partialDecision=Decision(partialId,1,true);
        var partial=await Value(admin.PostAsJsonAsync(Route(draft.Id,$"/core/requests/{partialId}/partial"),partialDecision),201,"teacher partial Core save");
        if(partial.GetProperty("draft").GetProperty("revision").GetInt64()!=2||partial.GetProperty("draft").GetProperty("status").GetString()!="collecting")throw new InvalidOperationException("Partial teacher result did not remain collecting.");
        var noChange=await Snapshot();_=await Value(admin.PostAsJsonAsync(Route(draft.Id,$"/core/requests/{partialId}/partial"),partialDecision),200,"partial exact replay");
        if(noChange!=await Snapshot())throw new InvalidOperationException("Partial replay changed state.");Pass("partial field dialogue persistence and identical no-write replay");
        var requestId=Guid.NewGuid();var decision=Decision(requestId,2);var poisoned=decision.DeepClone().AsObject();poisoned["scope"]!["actor_id"]=Guid.NewGuid().ToString("D");
        await Deny("model-supplied actor cannot change scope",409,()=>admin.PostAsJsonAsync(Route(draft.Id,$"/core/requests/{requestId}/prepare"),poisoned));
        var unknown=decision.DeepClone().AsObject();unknown["candidates"]![0]!["edit"]!["set"]!["Role"]=JsonSerializer.SerializeToNode(new{kind="text",value="Owner"});
        await Deny("model-supplied role field rejected",400,()=>admin.PostAsJsonAsync(Route(draft.Id,$"/core/requests/{requestId}/prepare"),unknown));
        var preparedList=await Value(admin.PostAsJsonAsync(Route(draft.Id,$"/core/requests/{requestId}/prepare"),decision),201,"teacher Core preparation");
        if(preparedList.ValueKind!=JsonValueKind.Array||preparedList.GetArrayLength()!=1)throw new InvalidOperationException("ConnectorSDK prepare must return one operation list.");
        var prepared=preparedList[0];var operationRef=prepared.GetProperty("ref").GetString()!;var hash=prepared.GetProperty("payload_hash").GetString()!;var approval=prepared.GetProperty("approval_ref").GetString()!;
        var expectation=new PentaOperationalExpectation(new(academy.ToString("D"),actor.ToString("D"),scopeJson.GetProperty("connection_id").GetString()!,session.ToString("D"),1,2),
            operationRef,requestId.ToString("D"),PentaTeacherCreationFields.Tool,1,draft.Id.ToString("D"),3,hash);
        var parsed=PentaOperationalBindings.ReadPreparation(prepared.GetRawText(),expectation);if(parsed.Status!="awaiting_approval"||PentaOperationalBindings.HasCurrentApprovalClaim(parsed,DateTimeOffset.UtcNow))throw new InvalidOperationException("Model preparation granted approval.");
        Pass("prepared operation matches shared host binding and never grants approval");
        noChange=await Snapshot();_=await Value(admin.PostAsJsonAsync(Route(draft.Id,$"/core/requests/{requestId}/prepare"),decision),200,"preparation replay");if(noChange!=await Snapshot())throw new InvalidOperationException("Preparation replay changed state.");Pass("same prepare request replays without new revision or audit");
        var changed=decision.DeepClone().AsObject();changed["candidates"]![0]!["edit"]!["set"]!["LastName"]!["value"]="Changed";
        await Deny("same request changed payload refused",409,()=>admin.PostAsJsonAsync(Route(draft.Id,$"/core/requests/{requestId}/prepare"),changed));
        object Binding(bool execute=false)=>execute?new{scope=scopeJson,operation_ref=operationRef,revision=3,payload_hash=hash,approval_ref=approval}:new{scope=scopeJson,operation_ref=operationRef,revision=3,payload_hash=hash};
        await Deny("Core cannot execute unapproved preparation",409,()=>admin.PostAsJsonAsync(Route(draft.Id,"/core/execute"),Binding(true)));
        var approvalView=await Value(admin.PostAsJsonAsync(Route(draft.Id,"/approve"),new{revision=3,payloadHash=hash,approvalId=approval,intent=PentaTeacherCreationWorkflow.CreationIntent}),200,"explicit host teacher approval");
        if(approvalView.GetProperty("status").GetString()!="Authorized")throw new InvalidOperationException("Explicit teacher approval missing.");
        var approved=await Value(admin.PostAsJsonAsync(Route(draft.Id,"/core/operation"),Binding()),200,"Core approved operation");
        if(!PentaOperationalBindings.HasCurrentApprovalClaim(PentaOperationalBindings.ReadPreparation(approved.GetRawText(),expectation),DateTimeOffset.UtcNow))throw new InvalidOperationException("Current host approval not represented.");Pass("explicit native host approval projects to shared approved operation");
        var race=await Task.WhenAll(admin.PostAsJsonAsync(Route(draft.Id,"/core/execute"),Binding(true)),admin.PostAsJsonAsync(Route(draft.Id,"/core/execute"),Binding(true)));
        var statuses=race.Select(x=>(int)x.StatusCode).Order().ToArray();if(!statuses.SequenceEqual(new[]{200,201}))throw new InvalidOperationException("Core execution race did not guard one-time domain write.");
        var receipts=new List<JsonElement>();foreach(var response in race){using(response)receipts.Add(await response.Content.ReadFromJsonAsync<JsonElement>());}
        if(!JsonElement.DeepEquals(receipts[0],receipts[1]))throw new InvalidOperationException("Core replay receipt differed.");var receipt=receipts[0];
        var bound=PentaOperationalBindings.ReadReceipt(receipt.GetRawText(),expectation);if(bound.Status!="succeeded"||bound.SourceRefs.Count!=1)throw new InvalidOperationException("Missing real Core receipt evidence.");Pass("Core execute race returns same committed source receipt once");
        var recovered=await Value(admin.PostAsJsonAsync(Route(draft.Id,"/core/status"),Binding()),200,"Core receipt recovery");if(!JsonElement.DeepEquals(receipt,recovered))throw new InvalidOperationException("Core status did not recover exact receipt.");Pass("Core status reads committed receipt without another teacher");
        var succeeded=await Value(admin.GetAsync(Route(draft.Id,"/core-context")),200,"succeeded teacher context");if(succeeded.GetProperty("draft").GetProperty("status").GetString()!="succeeded")throw new InvalidOperationException("Core current missed completed draft.");
        var nativeDraft=await Save("NativeApproval");var nativeReview=await Value(admin.PostAsJsonAsync(Route(nativeDraft.Id,"/review"),new{revision=nativeDraft.Revision,payloadHash=nativeDraft.PayloadHash}),200,"native teacher review");
        _=await Value(admin.PostAsJsonAsync(Route(nativeDraft.Id,"/approve"),new{revision=nativeDraft.Revision,payloadHash=nativeDraft.PayloadHash,
            approvalId=nativeReview.GetProperty("approvalId").GetString(),intent=PentaTeacherCreationWorkflow.CreationIntent}),200,"native teacher approval retained");
        await Deny("native approval cannot fabricate a Core operation identity",409,()=>admin.GetAsync(Route(nativeDraft.Id,"/core-context")));
        var badBinding=JsonSerializer.SerializeToNode(Binding(true))!.AsObject();badBinding["scope"]!["tenant_id"]=foreign.ToString("D");
        await Deny("foreign scope cannot replay completed receipt",409,()=>admin.PostAsJsonAsync(Route(draft.Id,"/core/execute"),badBinding));
        var edited=await Save("EditHistory");var editContext=await Value(admin.GetAsync(Route(edited.Id,"/core-context")),200,"edit-history context");
        var originalScope=scopeJson;scopeJson=editContext.GetProperty("scope");var editRequest=Guid.NewGuid();var editDecision=Decision(editRequest,1,true);
        _=await Value(admin.PostAsJsonAsync(Route(edited.Id,$"/core/requests/{editRequest}/partial"),editDecision),201,"edit-history partial");
        _=await Value(admin.PutAsJsonAsync(Route(edited.Id),new{expectedRevision=2,contextVersion=1,fields=new Dictionary<string,object>{["FirstName"]=new{kind="text",value="CoreSynthetic"},["LastName"]=new{kind="text",value="ManualEdit"}}}),201,"native edit of Core draft");
        await Deny("native edit cannot erase request replay history",409,()=>admin.PostAsJsonAsync(Route(edited.Id,$"/core/requests/{editRequest}/partial"),editDecision));scopeJson=originalScope;
        var faultDraft=await Save("PreparationRollback");var faultContext=await Value(admin.GetAsync(Route(faultDraft.Id,"/core-context")),200,"fault-context");
        scopeJson=faultContext.GetProperty("scope");var faultId=Guid.NewGuid();var faultDecision=Decision(faultId,1);faultDecision["candidates"]![0]!["edit"]!["set"]!["LastName"]!["value"]="PreparationRollback";
        var fault=new TeacherCorePreparationFault();using(var faulty=new QaApiFactory(manifest,flags,isolatedDomainInterceptor:fault)){using var c=await Client("qa-teacher-core@example.invalid",faulty);
            await Deny("Core preparation audit failure rolls protected ledger back",503,()=>c.PostAsJsonAsync(Route(faultDraft.Id,$"/core/requests/{faultId}/prepare"),faultDecision));if(fault.Hits!=1)throw new InvalidOperationException("Core preparation audit fault not reached.");}
        using(var scope=factory.Services.CreateScope()){
            var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();if(await db.Teachers.CountAsync(x=>x.AcademyId==academy&&x.FirstName=="CoreSynthetic"&&x.LastName=="CoreOne")!=1 ||
                await db.AuditLogs.CountAsync(x=>x.EntityId==draft.Id&&x.Action=="PentaTeacherDraftExecuted")!=1)throw new InvalidOperationException("Core final teacher/audit count mismatch.");
            var row=await scope.ServiceProvider.GetRequiredService<IdentityDbContext>().Users.SingleAsync(x=>x.Id==actor);row.IsActive=false;await scope.ServiceProvider.GetRequiredService<IdentityDbContext>().SaveChangesAsync();}
        scopeJson=originalScope;await Deny("inactive actor cannot recover Core receipt",403,()=>admin.PostAsJsonAsync(Route(draft.Id,"/core/status"),Binding()));
        var directory=Path.GetFullPath(Path.Combine("QA","EVIDENCE","penta-teacher-core-sql-"+manifest.RunId.ToString("N")));Directory.CreateDirectory(directory);
        await File.WriteAllTextAsync(Path.Combine(directory,"contract-samples.json"),JsonSerializer.Serialize(new{current,partial,prepared,approved,receipt,succeeded},new JsonSerializerOptions{WriteIndented=true}));
        await File.WriteAllTextAsync(Path.Combine(directory,"result.json"),JsonSerializer.Serialize(new{run=manifest.RunId.ToString("N"),passed=true,checks=count,createdTeachers=1,executionAudits=1,statuses,modelInference=false,toolRegistered=false,productionApproved=false},new JsonSerializerOptions{WriteIndented=true}));
        Console.WriteLine($"PASS: PENTATEACHERCORE all {count} real SQL/HTTP shared-contract cases; no inference or tool activation.");
    }
    private sealed class TeacherCorePreparationFault:SaveChangesInterceptor
    {
        public int Hits{get;private set;}
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData data,InterceptionResult<int> result,CancellationToken token=default){
            if(data.Context is AcademyDeskDbContext db&&db.ChangeTracker.Entries<AuditLog>().Any(x=>x.State==EntityState.Added&&x.Entity.Action=="PentaTeacherDraftCorePrepared")){
                Hits++;throw new InvalidOperationException("Synthetic teacher Core preparation audit failure.");}return base.SavingChangesAsync(data,result,token);
        }
    }
}
