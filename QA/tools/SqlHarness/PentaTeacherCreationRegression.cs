using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
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
    private static async Task VerifyPentaTeacherCreationAsync(QaApiFactory baseline,QaRunManifest manifest)
    {
        var flags=new Dictionary<string,string?>{["Penta:TeacherCreationContractsEnabled"]="true",
            ["Penta:OperationalDraftContractsEnabled"]="true",["Penta:OperationalOnboardingHttpContractsEnabled"]="true"};
        var clock=new OnboardingDraftClock();using var factory=new QaApiFactory(manifest,flags,isolatedClock:clock);
        Guid academy,foreign;using(var scope=factory.Services.CreateScope()){
            var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            academy=await db.Academies.Where(x=>x.Name=="Synthetic Academy A").Select(x=>x.Id).SingleAsync();
            foreign=await db.Academies.Where(x=>x.Name=="Synthetic Academy B").Select(x=>x.Id).SingleAsync();}
        var actor=await CreateAccessActorAsync(factory,"qa-teacher-workflow@example.invalid","AcademyAdmin",academy);
        await CreateAccessActorAsync(factory,"qa-teacher-workflow-other@example.invalid","AcademyAdmin",academy);
        var platform=await CreateAccessActorAsync(factory,"qa-teacher-workflow-platform@example.invalid","AcademyAdmin",academy);
        Guid session;using(var scope=factory.Services.CreateScope()){
            var identity=scope.ServiceProvider.GetRequiredService<IdentityDbContext>();(await identity.Users.SingleAsync(x=>x.Id==platform)).IsPlatformOwner=true;await identity.SaveChangesAsync();
            var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();var row=new PentaMiniSession{AcademyId=academy,ActorUserId=actor,Version=1,ExpiresAtUtc=clock.Value.UtcDateTime.AddHours(1),ProtectedState="Synthetic teacher workflow session; no inference"};
            db.PentaMiniSessions.Add(row);await db.SaveChangesAsync();session=row.Id;}
        async Task<HttpClient> Client(string email,QaApiFactory? host=null){var c=(host??factory).CreateClient();var token=await LoginAsync(c,email,"Synthetic!39Ab");c.DefaultRequestHeaders.Authorization=new AuthenticationHeaderValue("Bearer",token);return c;}
        using var admin=await Client("qa-teacher-workflow@example.invalid");using var other=await Client("qa-teacher-workflow-other@example.invalid");
        using var teacher=await Client("qa-teacher-a@example.invalid");using var owner=await Client("qa-teacher-workflow-platform@example.invalid");using var anonymous=factory.CreateClient();
        var count=0;void Pass(string label){count++;Console.WriteLine("PASS: PENTATEACHER "+label);}
        string Route(Guid id,string suffix="",Guid? tenant=null)=>$"/api/academies/{tenant??academy}/penta/teachers/conversations/{session}/drafts/{id}{suffix}";
        async Task<string> Snapshot(){using var scope=factory.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();var identity=scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            return JsonSerializer.Serialize(new{teachers=await db.Teachers.AsNoTracking().OrderBy(x=>x.Id).ToArrayAsync(),drafts=await db.PentaOnboardingDrafts.AsNoTracking().OrderBy(x=>x.Id).ToArrayAsync(),
                sessions=await db.PentaMiniSessions.AsNoTracking().OrderBy(x=>x.Id).ToArrayAsync(),audits=await db.AuditLogs.AsNoTracking().OrderBy(x=>x.Id).ToArrayAsync(),
                users=await identity.Users.AsNoTracking().Select(x=>new{x.Id,x.IsActive,x.AcademyId,x.TeacherId,x.IsPlatformOwner}).OrderBy(x=>x.Id).ToArrayAsync(),
                memberships=await identity.UserRoles.AsNoTracking().OrderBy(x=>x.UserId).ThenBy(x=>x.RoleId).ToArrayAsync(),
                students=await db.Students.AsNoTracking().OrderBy(x=>x.Id).ToArrayAsync(),payments=await db.Payments.AsNoTracking().OrderBy(x=>x.Id).ToArrayAsync()});}
        async Task Deny(string label,int expected,Func<Task<HttpResponseMessage>> operation){var before=await Snapshot();using var response=await operation();RequireFinanceStatus(response,(HttpStatusCode)expected,label);
            if(before!=await Snapshot())throw new InvalidOperationException("Teacher workflow refusal changed protected scoped state.");Pass(label);}
        object Fields(string last)=>new Dictionary<string,object>{["FirstName"]=new{kind="text",value="SyntheticWorkflow"},["LastName"]=new{kind="text",value=last}};
        object Binding(PentaTeacherCreationView view,bool approval=false)=>approval?new{revision=view.Revision,payloadHash=view.PayloadHash,approvalId=view.ApprovalId}:new{revision=view.Revision,payloadHash=view.PayloadHash};
        async Task<PentaTeacherCreationView> Value(Task<HttpResponseMessage> operation,int expected,string label){using var response=await operation;RequireFinanceStatus(response,(HttpStatusCode)expected,label);return(await response.Content.ReadFromJsonAsync<PentaTeacherCreationView>())??throw new InvalidOperationException("Missing teacher draft view.");}
        async Task<PentaTeacherCreationView> Save(string last,Guid? id=null)=>await Value(admin.PutAsJsonAsync(Route(id??Guid.NewGuid()),new{expectedRevision=0,contextVersion=1,fields=Fields(last)}),201,"save teacher draft");
        async Task<PentaTeacherCreationView> Review(PentaTeacherCreationView view)=>await Value(admin.PostAsJsonAsync(Route(view.Id,"/review"),Binding(view)),200,"review teacher draft");
        async Task<PentaTeacherCreationView> Approve(PentaTeacherCreationView view)=>await Value(admin.PostAsJsonAsync(Route(view.Id,"/approve"),new{revision=view.Revision,payloadHash=view.PayloadHash,approvalId=view.ApprovalId,intent=PentaTeacherCreationWorkflow.CreationIntent}),200,"approve teacher draft");
        var target=Guid.NewGuid();
        await Deny("anonymous denied",401,()=>anonymous.GetAsync(Route(target)));
        await Deny("Teacher role denied",403,()=>teacher.GetAsync(Route(target)));
        await Deny("foreign academy denied",403,()=>admin.GetAsync(Route(target,tenant:foreign)));
        await Deny("Platform Owner cannot become academy conversation actor",403,()=>owner.GetAsync(Route(target)));
        using(var off=baseline.CreateClient()){
            var token=await LoginAsync(off,"qa-teacher-workflow@example.invalid","Synthetic!39Ab");off.DefaultRequestHeaders.Authorization=new AuthenticationHeaderValue("Bearer",token);
            await Deny("independent feature gate off",404,()=>off.GetAsync(Route(target)));}
        await Deny("sensitive unknown field cannot enter draft",400,()=>admin.PutAsJsonAsync(Route(target),new{expectedRevision=0,contextVersion=1,fields=new Dictionary<string,object>{["Role"]=new{kind="text",value="Owner"}}}));
        var draft=await Save("RaceReceipt",target);
        using(var scope=factory.Services.CreateScope()){
            var row=await scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>().PentaOnboardingDrafts.AsNoTracking().SingleAsync(x=>x.Id==draft.Id);
            if(row.ProtectedBody.Contains(draft.CanonicalPayload,StringComparison.Ordinal)||row.ProtectedBody.Contains("RaceReceipt",StringComparison.Ordinal))throw new InvalidOperationException("Teacher draft payload stored as plaintext.");}
        Pass("draft persisted encrypted without teacher write");
        var saved=await Value(admin.PutAsJsonAsync(Route(draft.Id),new{expectedRevision=0,contextVersion=1,fields=Fields("RaceReceipt")}),200,"identical save replay");
        if(saved.Revision!=1)throw new InvalidOperationException("Identical draft save advanced revision.");Pass("same save replay retains revision");
        await Deny("other administrator cannot read private conversation",409,()=>other.GetAsync(Route(draft.Id)));
        await Deny("execute before review rejected",409,()=>admin.PostAsJsonAsync(Route(draft.Id,"/execute"),new{revision=draft.Revision,payloadHash=draft.PayloadHash,approvalId=Guid.NewGuid()}));
        draft=await Review(draft);if(draft.Status!="Reviewed"||draft.ApprovalId is null||draft.ApprovalExpiresAtUtc is null)throw new InvalidOperationException("Teacher review omitted challenge.");
        await Deny("review alone cannot authorize execution",409,()=>admin.PostAsJsonAsync(Route(draft.Id,"/execute"),Binding(draft,true)));
        await Deny("conversational yes is not creation intent",400,()=>admin.PostAsJsonAsync(Route(draft.Id,"/approve"),new{revision=draft.Revision,payloadHash=draft.PayloadHash,approvalId=draft.ApprovalId,intent="yes"}));
        await Deny("manipulated approval reference rejected",409,()=>admin.PostAsJsonAsync(Route(draft.Id,"/approve"),new{revision=draft.Revision,payloadHash=draft.PayloadHash,approvalId=Guid.NewGuid(),intent=PentaTeacherCreationWorkflow.CreationIntent}));
        await Deny("malformed approval type returns400",400,()=>admin.PostAsJsonAsync(Route(draft.Id,"/approve"),new{revision=draft.Revision,payloadHash=draft.PayloadHash,approvalId=123,intent=PentaTeacherCreationWorkflow.CreationIntent}));
        draft=await Approve(draft);if(draft.Status!="Authorized")throw new InvalidOperationException("Teacher approval not persisted.");
        var responses=await Task.WhenAll(admin.PostAsJsonAsync(Route(draft.Id,"/execute"),Binding(draft,true)),admin.PostAsJsonAsync(Route(draft.Id,"/execute"),Binding(draft,true)));
        var statuses=responses.Select(x=>(int)x.StatusCode).Order().ToArray();if(!statuses.SequenceEqual(new[]{200,201}))throw new InvalidOperationException("Concurrent teacher execution was not one201/one200.");
        var receipts=new List<PentaTeacherCreationView>();foreach(var response in responses){using(response)receipts.Add((await response.Content.ReadFromJsonAsync<PentaTeacherCreationView>())!);}
        if(receipts.Any(x=>x.Status!="Succeeded"||x.Receipt is null)||receipts[0].Receipt!=receipts[1].Receipt)throw new InvalidOperationException("Concurrent execution did not recover the exact same receipt.");
        using(var scope=factory.Services.CreateScope()){
            var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();if(await db.Teachers.CountAsync(x=>x.AcademyId==academy&&x.FirstName=="SyntheticWorkflow"&&x.LastName=="RaceReceipt")!=1 ||
                await db.AuditLogs.CountAsync(x=>x.EntityId==draft.Id&&x.Action=="PentaTeacherDraftExecuted")!=1)throw new InvalidOperationException("Concurrent teacher execution duplicated row or audit.");}
        Pass("simultaneous execution saves exactly one teacher and one receipt/audit");
        var recovered=await Value(admin.GetAsync(Route(draft.Id)),200,"read-only outcome recovery");if(recovered.Receipt!=receipts[0].Receipt)throw new InvalidOperationException("Lost response recovery changed receipt.");Pass("GET recovers authoritative receipt without redispatch");
        await Deny("completed draft cannot be edited",409,()=>admin.PutAsJsonAsync(Route(draft.Id),new{expectedRevision=1,contextVersion=1,fields=Fields("Other")}));

        var a=await Approve(await Review(await Save("DistinctDuplicate")));var b=await Approve(await Review(await Save("DistinctDuplicate")));
        var duplicateResponses=await Task.WhenAll(admin.PostAsJsonAsync(Route(a.Id,"/execute"),Binding(a,true)),admin.PostAsJsonAsync(Route(b.Id,"/execute"),Binding(b,true)));
        var duplicateStatuses=duplicateResponses.Select(x=>(int)x.StatusCode).Order().ToArray();foreach(var response in duplicateResponses)response.Dispose();
        if(!duplicateStatuses.SequenceEqual(new[]{201,409}))throw new InvalidOperationException("Distinct concurrent approved duplicate drafts were not one201/one409.");
        using(var scope=factory.Services.CreateScope())if(await scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>().Teachers.CountAsync(x=>x.AcademyId==academy&&x.FirstName=="SyntheticWorkflow"&&x.LastName=="DistinctDuplicate")!=1)
            throw new InvalidOperationException("Distinct concurrent drafts created duplicate teachers.");
        Pass("distinct simultaneous approved duplicate drafts save one teacher and reject one409");
        var existing=await Save("DistinctDuplicate");await Deny("existing teacher held for manual duplicate review",409,()=>admin.PostAsJsonAsync(Route(existing.Id,"/review"),Binding(existing)));
        var edit=await Approve(await Review(await Save("BeforeEdit")));var edited=await Value(admin.PutAsJsonAsync(Route(edit.Id),new{expectedRevision=1,contextVersion=1,fields=Fields("AfterEdit")}),201,"edit teacher draft");
        if(edited.Revision!=2||edited.Status!="AwaitingReview"||edited.ApprovalId is not null)throw new InvalidOperationException("Edit retained prior approval.");
        await Deny("old approved payload cannot execute after edit",409,()=>admin.PostAsJsonAsync(Route(edit.Id,"/execute"),Binding(edit,true)));
        var cancel=await Approve(await Review(await Save("Cancelled")));_=await Value(admin.PostAsJsonAsync(Route(cancel.Id,"/cancel"),Binding(cancel)),200,"cancel teacher draft");
        await Deny("cancelled teacher draft cannot execute",409,()=>admin.PostAsJsonAsync(Route(cancel.Id,"/execute"),Binding(cancel,true)));
        var faultDraft=await Approve(await Review(await Save("AuditRollback")));var fault=new TeacherDraftAuditFault();
        using(var failureHost=new QaApiFactory(manifest,flags,isolatedDomainInterceptor:fault,isolatedClock:clock)){
            using var failure=await Client("qa-teacher-workflow@example.invalid",failureHost);
            await Deny("required execution audit failure rolls back teacher and receipt",503,()=>failure.PostAsJsonAsync(Route(faultDraft.Id,"/execute"),Binding(faultDraft,true)));
            if(fault.Hits!=1)throw new InvalidOperationException("Teacher draft audit fault not reached.");}
        var reconciled=await Value(admin.GetAsync(Route(faultDraft.Id)),200,"failure reconciliation");if(reconciled.Status!="Authorized"||reconciled.Receipt is not null)throw new InvalidOperationException("Audit failure left success state.");
        _=await Value(admin.PostAsJsonAsync(Route(faultDraft.Id,"/execute"),Binding(reconciled,true)),201,"explicit execution after proven rollback");Pass("failed outcome reconciles before explicit successful retry");
        var partial=await Value(admin.PutAsJsonAsync(Route(Guid.NewGuid()),new{expectedRevision=0,contextVersion=1,fields=new Dictionary<string,object>{["FirstName"]=new{kind="text",value="Partial"}}}),201,"partial teacher draft");
        await Deny("missing required name cannot review",400,()=>admin.PostAsJsonAsync(Route(partial.Id,"/review"),Binding(partial)));
        await Deny("student workflow cannot adopt a teacher draft",409,()=>admin.GetAsync($"/api/academies/{academy}/penta/onboarding/conversations/{session}/drafts/{partial.Id}"));
        Guid studentDraft=Guid.NewGuid();using(var scope=factory.Services.CreateScope()){
            var student=scope.ServiceProvider.GetRequiredService<PentaOnboardingDraftService>();
            var principal=new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity([
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier,actor.ToString())],"synthetic"));
            var payload=JsonSerializer.Serialize(new{tool_key=PentaOnboardingDraftService.Tool,tool_revision=1,fields=new{}});
            if((await student.SaveAsync(principal,academy,session,studentDraft,0,1,payload,default)).HttpStatus!=201)throw new InvalidOperationException("Student-purpose draft fixture failed.");}
        await Deny("teacher workflow cannot adopt a student draft",409,()=>admin.GetAsync(Route(studentDraft)));
        var corrupt=await Save("CorruptCiphertext");using(var scope=factory.Services.CreateScope()){
            var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();(await db.PentaOnboardingDrafts.SingleAsync(x=>x.Id==corrupt.Id)).ProtectedBody="synthetic-corrupt-protected-body";await db.SaveChangesAsync();}
        await Deny("corrupt protected draft fails closed",409,()=>admin.GetAsync(Route(corrupt.Id)));
        var stale=await Approve(await Review(await Save("Expired")));clock.Value=clock.Value.AddMinutes(3);
        await Deny("expired scoped approval cannot execute",410,()=>admin.PostAsJsonAsync(Route(stale.Id,"/execute"),Binding(stale,true)));
        var current=await Review(await Save("ContextChange"));using(var scope=factory.Services.CreateScope()){
            var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();(await db.PentaMiniSessions.SingleAsync(x=>x.Id==session)).Version=2;await db.SaveChangesAsync();}
        await Deny("changed conversation cannot authorize stale draft",409,()=>admin.PostAsJsonAsync(Route(current.Id,"/approve"),new{revision=current.Revision,payloadHash=current.PayloadHash,approvalId=current.ApprovalId,intent=PentaTeacherCreationWorkflow.CreationIntent}));
        using(var scope=factory.Services.CreateScope()){
            var identity=scope.ServiceProvider.GetRequiredService<IdentityDbContext>();identity.UserRoles.RemoveRange(await identity.UserRoles.Where(x=>x.UserId==actor).ToListAsync());await identity.SaveChangesAsync();}
        await Deny("revoked current membership cannot recover committed receipt",403,()=>admin.GetAsync(Route(draft.Id)));
        using(var scope=factory.Services.CreateScope()){
            var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();if(await db.Teachers.CountAsync(x=>x.AcademyId==academy&&x.FirstName=="SyntheticWorkflow")!=3)throw new InvalidOperationException("Teacher workflow final row count mismatch.");
            if(await db.AuditLogs.CountAsync(x=>x.ActorUserId==actor&&x.Action=="PentaTeacherDraftExecuted")!=3)throw new InvalidOperationException("Teacher workflow final execution audit count mismatch.");
            var ids=await db.Teachers.Where(t=>t.FirstName=="SyntheticWorkflow").Select(t=>t.Id).ToArrayAsync();
            if(await scope.ServiceProvider.GetRequiredService<IdentityDbContext>().Users.CountAsync(x=>x.TeacherId!=null && ids.Contains(x.TeacherId.Value))!=0)throw new InvalidOperationException("Teacher workflow created a portal account.");}
        var directory=Path.GetFullPath(Path.Combine("QA","EVIDENCE","penta-teacher-workflow-sql-"+manifest.RunId.ToString("N")));Directory.CreateDirectory(directory);
        await File.WriteAllTextAsync(Path.Combine(directory,"result.json"),JsonSerializer.Serialize(new{run=manifest.RunId.ToString("N"),passed=true,checks=count,createdTeachers=3,simultaneousStatuses=statuses,
            duplicateStatuses,executionAudits=3,accountsCreated=0,modelInference=false,toolRegistered=false,physicalDevices="NOT RUN"},new JsonSerializerOptions{WriteIndented=true}));
        Console.WriteLine($"PASS: PENTATEACHER all {count} real SQL/HTTP workflow cases; one-time teacher/audit/receipt, no model registration.");
    }
    private sealed class TeacherDraftAuditFault:SaveChangesInterceptor
    {
        public int Hits{get;private set;}
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData data,InterceptionResult<int> result,CancellationToken token=default){
            if(data.Context is AcademyDeskDbContext db && db.ChangeTracker.Entries<AuditLog>().Any(x=>x.State==EntityState.Added&&x.Entity.Action=="PentaTeacherDraftExecuted")){
                Hits++;throw new InvalidOperationException("Synthetic mandatory teacher draft audit failure.");}
            return base.SavingChangesAsync(data,result,token);
        }
    }
}
