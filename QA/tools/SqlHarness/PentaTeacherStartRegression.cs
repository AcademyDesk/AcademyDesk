using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Intelligence.Penta;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

internal static partial class SqlHarnessEntryPoint
{
    private static async Task VerifyPentaTeacherStartAsync(QaRunManifest manifest)
    {
        var flags=new Dictionary<string,string?>{["Penta:TeacherCreationContractsEnabled"]="true",["Penta:TeacherCoreContractsEnabled"]="true",
            ["Penta:TeacherConversationContractsEnabled"]="true",["Penta:TeacherStartContractsEnabled"]="true",
            ["Penta:OperationalDraftContractsEnabled"]="true",["Penta:OperationalOnboardingHttpContractsEnabled"]="true"};
        var planner=new TeacherConversationPlanner();var clock=new TeacherStartClock();using var factory=new QaApiFactory(manifest,flags,isolatedClock:clock,isolatedTeacherInterpreter:planner);
        Guid academy,foreign;using(var scope=factory.Services.CreateScope()){
            var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();academy=await db.Academies.Where(x=>x.Name=="Synthetic Academy A").Select(x=>x.Id).SingleAsync();
            foreign=await db.Academies.Where(x=>x.Name=="Synthetic Academy B").Select(x=>x.Id).SingleAsync();}
        var actor=await CreateAccessActorAsync(factory,"qa-teacher-start@example.invalid","AcademyAdmin",academy);
        await CreateAccessActorAsync(factory,"qa-teacher-start-other@example.invalid","AcademyAdmin",academy);
        async Task<HttpClient> Client(string email,QaApiFactory? host=null){var c=(host??factory).CreateClient();c.DefaultRequestHeaders.Authorization=new AuthenticationHeaderValue("Bearer",await LoginAsync(c,email,"Synthetic!39Ab"));return c;}
        using var admin=await Client("qa-teacher-start@example.invalid");using var other=await Client("qa-teacher-start-other@example.invalid");using var teacher=await Client("qa-teacher-a@example.invalid");using var platform=await Client("qa-teacher-workflow-platform@example.invalid");using var anonymous=factory.CreateClient();
        string Base(Guid? tenant=null)=>$"/api/academies/{tenant??academy}/penta/teachers";
        string Result(Guid request)=>Base()+"/starts/"+request;
        var checks=0;void Pass(string label){checks++;Console.WriteLine("PASS: PENTATEACHERSTART "+label);}
        async Task<string> Snapshot(bool operational=true){using var scope=factory.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();var identity=scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            return JsonSerializer.Serialize(new{teachers=await db.Teachers.AsNoTracking().OrderBy(x=>x.Id).ToArrayAsync(),students=await db.Students.AsNoTracking().OrderBy(x=>x.Id).ToArrayAsync(),
                invoices=await db.Invoices.AsNoTracking().OrderBy(x=>x.Id).ToArrayAsync(),payments=await db.Payments.AsNoTracking().OrderBy(x=>x.Id).ToArrayAsync(),
                users=await identity.Users.AsNoTracking().Select(x=>new{x.Id,x.AcademyId,x.IsActive,x.IsPlatformOwner,x.SecurityStamp,x.ConcurrencyStamp,x.TeacherId}).OrderBy(x=>x.Id).ToArrayAsync(),
                roles=await identity.UserRoles.AsNoTracking().OrderBy(x=>x.UserId).ThenBy(x=>x.RoleId).ToArrayAsync(),
                sessions=operational?await db.PentaMiniSessions.AsNoTracking().OrderBy(x=>x.Id).ToArrayAsync():[],drafts=operational?await db.PentaOnboardingDrafts.AsNoTracking().OrderBy(x=>x.Id).ToArrayAsync():[],
                turns=operational?await db.PentaMiniTurns.AsNoTracking().OrderBy(x=>x.Id).ToArrayAsync():[],audits=operational?await db.AuditLogs.AsNoTracking().OrderBy(x=>x.Id).ToArrayAsync():[]});}
        async Task Deny(string label,int expected,Func<Task<HttpResponseMessage>> call){var before=await Snapshot();var calls=planner.Calls;using var response=await call();RequireFinanceStatus(response,(HttpStatusCode)expected,label);
            if(before!=await Snapshot()||calls!=planner.Calls)throw new InvalidOperationException("Teacher startup refusal changed scoped state or invoked interpretation.");Pass(label);}
        async Task<JsonElement> Value(Task<HttpResponseMessage> call,int expected,string label){using var response=await call;RequireFinanceStatus(response,(HttpStatusCode)expected,label);return await response.Content.ReadFromJsonAsync<JsonElement>();}
        var request=Guid.NewGuid();var domainBefore=await Snapshot(false);int usersBefore;
        using(var scope=factory.Services.CreateScope())usersBefore=await scope.ServiceProvider.GetRequiredService<IdentityDbContext>().Users.CountAsync();
        await Deny("anonymous startup denied",401,()=>anonymous.PostAsJsonAsync(Base()+"/conversations",new{requestId=request}));
        await Deny("Teacher startup denied",403,()=>teacher.PostAsJsonAsync(Base()+"/conversations",new{requestId=request}));
        await Deny("Platform Owner cannot act as academy teacher operator",403,()=>platform.PostAsJsonAsync(Base()+"/conversations",new{requestId=request}));
        await Deny("foreign tenant startup denied",403,()=>admin.PostAsJsonAsync(Base(foreign)+"/conversations",new{requestId=request}));
        using(var disabled=new QaApiFactory(manifest)){using var c=await Client("qa-teacher-start@example.invalid",disabled);await Deny("default startup gate off",404,()=>c.PostAsJsonAsync(Base()+"/conversations",new{requestId=request}));}
        using(var disabled=new QaApiFactory(manifest,new Dictionary<string,string?>(flags){["Penta:TeacherStartContractsEnabled"]="false"},isolatedTeacherInterpreter:planner)){
            using var c=await Client("qa-teacher-start@example.invalid",disabled);await Deny("independent startup gate off",404,()=>c.GetAsync(Base()+"/readiness"));}
        foreach(var json in new[]{"{}","null","[]","{\"requestId\":null}","{\"requestId\":\"00000000-0000-0000-0000-000000000000\"}",
            "{\"requestId\":\"AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAAA\"}","{\"requestId\":\""+request+"\",\"approved\":true}","{\"requestId\":\""+request+"\",\"requestId\":\""+request+"\"}"})
            await Deny("strict startup input rejected",400,()=>admin.PostAsync(Base()+"/conversations",new StringContent(json,System.Text.Encoding.UTF8,"application/json")));
        var readiness=await Value(admin.GetAsync(Base()+"/readiness"),200,"synthetic readiness");
        if(!readiness.GetProperty("available").GetBoolean()||!readiness.GetProperty("canStart").GetBoolean()||!readiness.GetProperty("syntheticInterpreter").GetBoolean()||readiness.GetProperty("canExecute").GetBoolean())throw new InvalidOperationException("Teacher test readiness overstated execution.");Pass("synthetic readiness advertises startup only");
        using(var offline=new QaApiFactory(manifest,flags,isolatedClock:clock)){using var c=await Client("qa-teacher-start@example.invalid",offline);
            var unavailable=await Value(c.GetAsync(Base()+"/readiness"),200,"disconnected readiness");if(unavailable.GetProperty("available").GetBoolean()||unavailable.GetProperty("canStart").GetBoolean())throw new InvalidOperationException("Disconnected provider appeared available.");Pass("disconnected readiness unavailable");
            await Deny("missing interpreter cannot allocate a start",503,()=>c.PostAsJsonAsync(Base()+"/conversations",new{requestId=request}));}
        var unqualified=new TeacherConversationUnqualified();using(var host=new QaApiFactory(manifest,flags,isolatedClock:clock,isolatedTeacherInterpreter:unqualified)){
            using var c=await Client("qa-teacher-start@example.invalid",host);await Deny("real unqualified interpreter cannot allocate",503,()=>c.PostAsJsonAsync(Base()+"/conversations",new{requestId=request}));if(unqualified.Calls!=0)throw new InvalidOperationException("Unqualified provider invoked.");}
        await Deny("unknown startup recovery returns404 without allocation",404,()=>admin.GetAsync(Result(request)));
        var responses=await Task.WhenAll(admin.PostAsJsonAsync(Base()+"/conversations",new{requestId=request}),admin.PostAsJsonAsync(Base()+"/conversations",new{requestId=request}));JsonElement saved;
        using(responses[0])using(responses[1]){
            if(!responses.Select(x=>(int)x.StatusCode).Order().SequenceEqual(new[]{200,201}))throw new InvalidOperationException("Teacher startup race must be201/200.");
            saved=await responses[0].Content.ReadFromJsonAsync<JsonElement>();if(!JsonElement.DeepEquals(saved,await responses[1].Content.ReadFromJsonAsync<JsonElement>()))throw new InvalidOperationException("Teacher startup race receipts differ.");}
        var session=saved.GetProperty("conversationId").GetGuid();var draft=saved.GetProperty("draftId").GetGuid();string Draft(string suffix="")=>Base()+$"/conversations/{session}/drafts/{draft}"+suffix;
        if(saved.GetProperty("status").GetString()!="conversation_started"||saved.GetProperty("canExecute").GetBoolean()||saved.GetProperty("domainEffect").GetBoolean())throw new InvalidOperationException("Startup falsely reported teacher creation.");
        using(var scope=factory.Services.CreateScope()){
            var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();if(await db.PentaMiniSessions.CountAsync(x=>x.Id==session&&x.AcademyId==academy&&x.ActorUserId==actor&&x.Version==1)!=1||
                await db.PentaOnboardingDrafts.CountAsync(x=>x.Id==draft&&x.SessionId==session&&x.Revision==1&&x.ContextVersion==1&&x.Status=="AwaitingReview")!=1||
                await db.PentaMiniTurns.CountAsync(x=>x.SessionId==session&&x.RequestId==request&&x.Status=="TeacherConversationStarted")!=1||
                await db.AuditLogs.CountAsync(x=>x.EntityId==session&&x.Action=="PentaTeacherConversationStarted")!=1)throw new InvalidOperationException("Startup exact SQL counts failed.");}
        Pass("parallel201/200 same receipt one owned session/draft/marker/start audit");
        var context=await Value(admin.GetAsync(Draft("/core-context")),200,"started teacher context");
        if(context.GetProperty("draft").GetProperty("fields").EnumerateObject().Any()||context.GetProperty("draft").GetProperty("revision").GetInt64()!=1||context.GetProperty("draft").GetProperty("status").GetString()!="collecting")throw new InvalidOperationException("Startup draft was not empty and collecting.");Pass("started session directly supplies empty current teacher Core context");
        using(var offline=new QaApiFactory(manifest,flags,isolatedClock:clock)){using var c=await Client("qa-teacher-start@example.invalid",offline);
            if(!JsonElement.DeepEquals(saved,await Value(c.GetAsync(Result(request)),200,"restart recovery"))||!JsonElement.DeepEquals(saved,await Value(c.PostAsJsonAsync(Base()+"/conversations",new{requestId=request}),200,"restart replay")))throw new InvalidOperationException("Disconnected startup recovery reallocated identity.");}
        Pass("fresh host without interpreter recovers original startup IDs");
        await Deny("other actor cannot recover startup IDs",404,()=>other.GetAsync(Result(request)));
        await Deny("foreign tenant cannot recover startup IDs",403,()=>admin.GetAsync(Base(foreign)+"/starts/"+request));
        await Deny("Teacher cannot recover startup IDs",403,()=>teacher.GetAsync(Result(request)));
        var fault=new TeacherConversationAuditFault("PentaTeacherConversationStarted");var failedId=Guid.NewGuid();
        using(var faulty=new QaApiFactory(manifest,flags,isolatedClock:clock,isolatedTeacherInterpreter:planner,isolatedDomainInterceptor:fault)){
            using var c=await Client("qa-teacher-start@example.invalid",faulty);await Deny("mandatory startup audit failure rolls all startup rows back",503,()=>c.PostAsJsonAsync(Base()+"/conversations",new{requestId=failedId}));if(fault.Hits!=1)throw new InvalidOperationException("Startup audit fault not hit.");}
        await Deny("rolled back startup has no phantom receipt",404,()=>admin.GetAsync(Result(failedId)));
        clock.Value=clock.Value.AddMinutes(11);await Deny("expired recovery cannot revive startup",410,()=>admin.GetAsync(Result(request)));
        await Deny("expired POST replay cannot allocate replacement",410,()=>admin.PostAsJsonAsync(Base()+"/conversations",new{requestId=request}));clock.Value=clock.Value.AddMinutes(-11);
        using(var scope=factory.Services.CreateScope()){
            var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();var marker=await db.PentaMiniTurns.SingleAsync(x=>x.SessionId==session&&x.RequestId==request);var original=marker.ProtectedReceipt;
            marker.ProtectedReceipt="invalid synthetic startup receipt";await db.SaveChangesAsync();await Deny("tampered startup receipt rejected",409,()=>admin.GetAsync(Result(request)));marker.ProtectedReceipt=original;await db.SaveChangesAsync();
            var s=await db.PentaMiniSessions.SingleAsync(x=>x.Id==session);var state=s.ProtectedOnboardingState;s.ProtectedOnboardingState="invalid synthetic teacher session";await db.SaveChangesAsync();
            await Deny("tampered startup cursor rejected",409,()=>admin.GetAsync(Result(request)));s.ProtectedOnboardingState=state;await db.SaveChangesAsync();s.Version++;await db.SaveChangesAsync();
            await Deny("changed conversation epoch cannot recover old start",409,()=>admin.GetAsync(Result(request)));s.Version--;await db.SaveChangesAsync();
            var audit=await db.AuditLogs.SingleAsync(x=>x.EntityId==session&&x.Action=="PentaTeacherConversationStarted");db.AuditLogs.Remove(audit);await db.SaveChangesAsync();
            await Deny("missing mandatory startup audit rejected",409,()=>admin.GetAsync(Result(request)));db.AuditLogs.Add(audit);await db.SaveChangesAsync();
            var status=marker.Status;marker.Status="OnboardingStarted";await db.SaveChangesAsync();await Deny("foreign workflow marker cannot be adopted",409,()=>admin.GetAsync(Result(request)));marker.Status=status;await db.SaveChangesAsync();
        }
        if(planner.Calls!=0||domainBefore!=await Snapshot(false))throw new InvalidOperationException("Startup invoked interpretation or changed domain/Identity data.");Pass("startup and recovery use zero interpretation calls and no domain writes");
        // Bounded session quota; exact same-key recovery remains possible at the cap.
        var quotaIds=new List<Guid>();using(var scope=factory.Services.CreateScope()){
            var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();var count=await db.PentaMiniSessions.CountAsync(x=>x.ActorUserId==actor&&x.AcademyId==academy&&x.CreatedAtUtc>=clock.Value.UtcDateTime.Date);
            for(var i=count;i<20;i++){var s=new PentaMiniSession{AcademyId=academy,ActorUserId=actor,Version=0,CreatedAtUtc=clock.Value.UtcDateTime,ExpiresAtUtc=clock.Value.UtcDateTime.AddMinutes(10),ProtectedState="Synthetic quota-only fixture"};quotaIds.Add(s.Id);db.PentaMiniSessions.Add(s);}await db.SaveChangesAsync();}
        try{await Deny("new startup obeys session quota",429,()=>admin.PostAsJsonAsync(Base()+"/conversations",new{requestId=Guid.NewGuid()}));
            if(!JsonElement.DeepEquals(saved,await Value(admin.PostAsJsonAsync(Base()+"/conversations",new{requestId=request}),200,"quota recovery")))throw new InvalidOperationException("Quota blocked exact startup recovery.");Pass("same request recovers at quota without reallocation");}
        finally{using var scope=factory.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();await db.PentaMiniSessions.Where(x=>quotaIds.Contains(x.Id)&&x.ActorUserId==actor&&x.AcademyId==academy).ExecuteDeleteAsync();}
        // Exercise an actual HTTP startup -> partial -> preparation -> explicit
        // approval -> domain create -> persisted receipt, without seeded session IDs.
        var current=(await admin.GetFromJsonAsync<PentaTeacherCreationView>(Draft()))!;var prompt=Guid.NewGuid();
        var entered=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);var release=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        planner.Handler=async(input,token)=>{entered.TrySetResult();await release.Task.WaitAsync(token);return TeacherChatDecision(input,true,"TeacherStartSynthetic");};
        object Prompt(PentaTeacherCreationView d,Guid id)=>new{requestId=id,draftRevision=d.Revision,payloadHash=d.PayloadHash,text="Synthetic teacher onboarding details"};
        var pending=admin.PostAsJsonAsync(Draft("/conversation/turns"),Prompt(current,prompt));
        try{await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            if(!JsonElement.DeepEquals(saved,await Value(admin.GetAsync(Result(request)),200,"pending start GET"))||!JsonElement.DeepEquals(saved,await Value(admin.PostAsJsonAsync(Base()+"/conversations",new{requestId=request}),200,"pending start POST"))||planner.Calls!=1)throw new InvalidOperationException("Pending startup recovery reallocated or redispatched.");
            Pass("startup IDs recover during actual pending interpretation without redispatch");}
        finally{release.TrySetResult();}
        var partial=await Value(pending,200,"startup partial prompt");if(partial.GetProperty("kind").GetString()!="partial")throw new InvalidOperationException("Started partial prompt failed.");Pass("public startup session supports partial conversation without seeding");
        current=(await admin.GetFromJsonAsync<PentaTeacherCreationView>(Draft()))!;planner.Handler=(input,_)=>Task.FromResult(TeacherChatDecision(input,false,"TeacherStartSynthetic","IntegratedFlow"));
        var prepared=await Value(admin.PostAsJsonAsync(Draft("/conversation/turns"),Prompt(current,Guid.NewGuid())),200,"startup prepare prompt");if(prepared.GetProperty("kind").GetString()!="prepared")throw new InvalidOperationException("Started prepare prompt failed.");
        current=(await admin.GetFromJsonAsync<PentaTeacherCreationView>(Draft()))!;await Deny("started prepared operation still needs explicit approval",409,()=>admin.PostAsJsonAsync(Draft("/execute"),new{revision=current.Revision,payloadHash=current.PayloadHash,approvalId=current.ApprovalId}));
        _=await Value(admin.PostAsJsonAsync(Draft("/approve"),new{revision=current.Revision,payloadHash=current.PayloadHash,approvalId=current.ApprovalId,intent=PentaTeacherCreationWorkflow.CreationIntent}),200,"explicit teacher approval");
        var receipt=await Value(admin.PostAsJsonAsync(Draft("/execute"),new{revision=current.Revision,payloadHash=current.PayloadHash,approvalId=current.ApprovalId}),201,"explicit teacher creation");
        if(receipt.GetProperty("receipt").GetProperty("teacherId").GetGuid()==Guid.Empty)throw new InvalidOperationException("Creation lacked committed source receipt.");Pass("started conversation reaches separate explicit approval and one real teacher receipt");
        if(!JsonElement.DeepEquals(saved,await Value(admin.GetAsync(Result(request)),200,"completed start recovery")))throw new InvalidOperationException("Completed startup identity changed.");Pass("completed startup recovers stable IDs without implying fresh creation");
        using(var scope=factory.Services.CreateScope()){
            var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();if(await db.Teachers.CountAsync(x=>x.AcademyId==academy&&x.FirstName=="TeacherStartSynthetic"&&x.LastName=="IntegratedFlow")!=1||
                await db.AuditLogs.CountAsync(x=>x.EntityId==draft&&x.Action=="PentaTeacherDraftExecuted")!=1||await db.PentaMiniSessions.CountAsync(x=>x.AcademyId==academy&&x.ActorUserId==actor)!=1||
                await scope.ServiceProvider.GetRequiredService<IdentityDbContext>().Users.CountAsync()!=usersBefore)throw new InvalidOperationException("Startup flow exact postconditions failed.");}
        using(var scope=factory.Services.CreateScope()){
            var id=scope.ServiceProvider.GetRequiredService<IdentityDbContext>();var user=await id.Users.SingleAsync(x=>x.Id==actor);user.SecurityStamp=Guid.NewGuid().ToString();await id.SaveChangesAsync();}
        await Deny("old bearer cannot recover after stamp revocation",401,()=>admin.GetAsync(Result(request)));
        using var fresh=await Client("qa-teacher-start@example.invalid");await Deny("new bearer cannot adopt old startup authority epoch",409,()=>fresh.GetAsync(Result(request)));
        using(var scope=factory.Services.CreateScope()){
            var id=scope.ServiceProvider.GetRequiredService<IdentityDbContext>();(await id.Users.SingleAsync(x=>x.Id==actor)).IsActive=false;await id.SaveChangesAsync();}
        await Deny("inactive actor cannot recover startup IDs",403,()=>fresh.GetAsync(Result(request)));
        if(planner.Calls!=2)throw new InvalidOperationException("Only two explicit prompts may invoke synthetic interpretation.");Pass("two explicit prompt calls only; all startup/recovery calls avoid interpretation");
        var directory=Path.GetFullPath(Path.Combine("QA","EVIDENCE","penta-teacher-start-sql-"+manifest.RunId.ToString("N")));Directory.CreateDirectory(directory);
        await File.WriteAllTextAsync(Path.Combine(directory,"result.json"),JsonSerializer.Serialize(new{run=manifest.RunId.ToString("N"),passed=true,checks,startSessionsCreated=1,startStatuses=new[]{200,201},
            createdTeachers=1,executionAudits=1,createdAccounts=0,plannerCalls=planner.Calls,syntheticInterpreter=true,modelInference=false,toolRegistered=false,productionApproved=false},new JsonSerializerOptions{WriteIndented=true}));
        Console.WriteLine($"PASS: PENTATEACHERSTART all {checks} real SQL/HTTP startup cases; no startup inference, one explicitly approved teacher flow.");
    }
    private sealed class TeacherStartClock:TimeProvider{
        public DateTimeOffset Value{get;set;}=DateTimeOffset.UtcNow;public override DateTimeOffset GetUtcNow()=>Value;
    }
}
