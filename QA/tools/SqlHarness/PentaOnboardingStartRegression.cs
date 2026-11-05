using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Intelligence.Penta;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

internal static partial class SqlHarnessEntryPoint
{
    private static async Task VerifyPentaOnboardingStartAsync(QaRunManifest manifest)
    {
        var flags = new Dictionary<string,string?> { ["Penta:OperationalDraftContractsEnabled"]="true",
            ["Penta:OperationalOnboardingHttpContractsEnabled"]="true", ["Penta:OperationalOnboardingExecutionContractsEnabled"]="true",
            ["Penta:OperationalOnboardingGatewayContractsEnabled"]="true" };
        var clock = new OnboardingDraftClock(); var planner = new OnboardingScriptedInterpreter();
        planner.Handler = (_,_) => throw new InvalidOperationException("Start must never invoke interpretation.");
        using var factory = new QaApiFactory(manifest, flags, isolatedClock:clock, isolatedOnboardingInterpreter:planner);
        Guid academy, foreign;
        using(var scope=factory.Services.CreateScope()) { var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            academy=await db.Academies.Where(x=>x.Name=="Synthetic Academy A").Select(x=>x.Id).SingleAsync();
            foreign=await db.Academies.Where(x=>x.Name=="Synthetic Academy B").Select(x=>x.Id).SingleAsync(); }
        const string email="qa-onboarding-start@example.invalid";
        var actor=await CreateAccessActorAsync(factory,email,"AcademyAdmin",academy);
        async Task<HttpClient> Client(string login,QaApiFactory? host=null) {
            var c=(host??factory).CreateClient(); c.DefaultRequestHeaders.Authorization=new AuthenticationHeaderValue("Bearer",await LoginAsync(c,login,"Synthetic!39Ab"));return c; }
        using var admin=await Client(email);using var other=await Client("qa-admin-a@example.invalid");
        using var teacher=await Client("qa-teacher-a@example.invalid");using var anonymous=factory.CreateClient();
        string Base(Guid? tenant=null)=>$"/api/academies/{tenant??academy}/penta/onboarding";
        async Task<string> Snapshot(bool operational=true) {
            using var scope=factory.Services.CreateScope(); var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var identity=scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            return JsonSerializer.Serialize(new {
                students=await db.Students.AsNoTracking().OrderBy(x=>x.Id).ToArrayAsync(), guardians=await db.Guardians.AsNoTracking().OrderBy(x=>x.Id).ToArrayAsync(),
                links=await db.StudentGuardians.AsNoTracking().OrderBy(x=>x.Id).ToArrayAsync(), invoices=await db.Invoices.AsNoTracking().OrderBy(x=>x.Id).ToArrayAsync(),
                payments=await db.Payments.AsNoTracking().OrderBy(x=>x.Id).ToArrayAsync(),users=await identity.Users.AsNoTracking().OrderBy(x=>x.Id).ToArrayAsync(),
                roles=await identity.UserRoles.AsNoTracking().OrderBy(x=>x.UserId).ThenBy(x=>x.RoleId).ToArrayAsync(),
                sessions=operational?await db.PentaMiniSessions.AsNoTracking().OrderBy(x=>x.Id).ToArrayAsync():[],
                drafts=operational?await db.PentaOnboardingDrafts.AsNoTracking().OrderBy(x=>x.Id).ToArrayAsync():[],
                turns=operational?await db.PentaMiniTurns.AsNoTracking().OrderBy(x=>x.Id).ToArrayAsync():[],
                audits=operational?await db.AuditLogs.AsNoTracking().OrderBy(x=>x.Id).ToArrayAsync():[] });
        }
        var count=0;void Pass(string label){count++;Console.WriteLine("PASS: PENTAONBOARDINGSTART "+label);}
        async Task Deny(string label,int status,Func<Task<HttpResponseMessage>> call) {
            var before=await Snapshot();using var response=await call();
            if((int)response.StatusCode!=status||before!=await Snapshot())throw new InvalidOperationException("Start denial failed: "+label+" expected="+status+" actual="+(int)response.StatusCode);
            Pass(label+" status="+status+" no changes"); }
        async Task<JsonElement> Read(HttpClient c,string path) {using var response=await c.GetAsync(path);
            if((int)response.StatusCode!=200)throw new InvalidOperationException("Start read failed HTTP="+(int)response.StatusCode);return await response.Content.ReadFromJsonAsync<JsonElement>();}
        var domain=await Snapshot(false);var request=Guid.NewGuid();
        await Deny("anonymous",401,()=>anonymous.PostAsJsonAsync(Base()+"/conversations",new{requestId=request}));
        await Deny("teacher",403,()=>teacher.PostAsJsonAsync(Base()+"/conversations",new{requestId=request}));
        await Deny("foreign tenant",403,()=>admin.PostAsJsonAsync(Base(foreign)+"/conversations",new{requestId=request}));
        using(var disabled=new QaApiFactory(manifest))using(var c=await Client(email,disabled))
            await Deny("default gate",404,()=>c.PostAsJsonAsync(Base()+"/conversations",new{requestId=request}));
        foreach(var body in new[]{"{}","null","[]","{\"requestId\":\"00000000-0000-0000-0000-000000000000\"}",
            "{\"requestId\":\""+request+"\",\"actorId\":\"untrusted\"}","{\"requestId\":\""+request+"\",\"requestId\":\""+request+"\"}"})
            await Deny("strict input",400,()=>admin.PostAsync(Base()+"/conversations",new StringContent(body,System.Text.Encoding.UTF8,"application/json")));
        using(var offline=new QaApiFactory(manifest,flags,isolatedClock:clock))using(var c=await Client(email,offline)) {
            var ready=await Read(c,Base()+"/readiness");if(ready.GetProperty("available").GetBoolean()||ready.GetProperty("canStart").GetBoolean())throw new InvalidOperationException("Absent interpreter appeared available.");
            Pass("absent interpreter readiness unavailable");
            await Deny("absent interpreter no session",503,()=>c.PostAsJsonAsync(Base()+"/conversations",new{requestId=request})); }
        var readiness=await Read(admin,Base()+"/readiness");
        if(!readiness.GetProperty("available").GetBoolean()||readiness.GetProperty("canExecute").GetBoolean())throw new InvalidOperationException("Connected test interpreter readiness mismatch.");
        Pass("connected test interpreter start readiness never execution");
        await Deny("unstarted result",404,()=>admin.GetAsync(Base()+"/starts/"+request));
        var responses=await Task.WhenAll(admin.PostAsJsonAsync(Base()+"/conversations",new{requestId=request}),admin.PostAsJsonAsync(Base()+"/conversations",new{requestId=request}));
        JsonElement saved;
        using(responses[0])using(responses[1]) {
            if(!responses.Select(x=>(int)x.StatusCode).Order().SequenceEqual(new[]{200,201}))throw new InvalidOperationException("Start race must be 201/200.");
            saved=await responses[0].Content.ReadFromJsonAsync<JsonElement>();
            if(!JsonElement.DeepEquals(saved,await responses[1].Content.ReadFromJsonAsync<JsonElement>()))throw new InvalidOperationException("Start race receipts differ."); }
        var session=saved.GetProperty("conversationId").GetGuid();var draft=saved.GetProperty("draftId").GetGuid();
        using(var scope=factory.Services.CreateScope()) {var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            if(await db.PentaMiniSessions.CountAsync(x=>x.Id==session&&x.ActorUserId==actor&&x.AcademyId==academy)!=1||
                await db.PentaOnboardingDrafts.CountAsync(x=>x.Id==draft&&x.SessionId==session&&x.Revision==1&&x.Status=="AwaitingReview")!=1||
                await db.PentaMiniTurns.CountAsync(x=>x.SessionId==session&&x.Status=="OnboardingStarted")!=1||
                await db.AuditLogs.CountAsync(x=>x.EntityId==session&&x.Action=="PentaOnboardingConversationStarted")!=1)throw new InvalidOperationException("Start race exact SQL counts failed."); }
        Pass("parallel201/200 same receipt one session/draft/marker/start audit");
        var cursor=await Read(admin,Base()+"/conversations/"+session+"/current");
        if(cursor.GetProperty("version").GetInt64()!=1||cursor.GetProperty("currentDraftId").GetGuid()!=draft||cursor.GetProperty("status").GetString()!="selected"||cursor.GetProperty("canExecute").GetBoolean())throw new InvalidOperationException("Start cursor failed.");
        Pass("initial current cursor selected version1 empty non-executable draft");
        using(var restart=new QaApiFactory(manifest,flags,isolatedClock:clock))using(var c=await Client(email,restart)) {
            if(!JsonElement.DeepEquals(saved,await Read(c,Base()+"/starts/"+request)))throw new InvalidOperationException("Start recovery changed receipt.");
            using var replay=await c.PostAsJsonAsync(Base()+"/conversations",new{requestId=request});
            if((int)replay.StatusCode!=200||!JsonElement.DeepEquals(saved,await replay.Content.ReadFromJsonAsync<JsonElement>()))throw new InvalidOperationException("Start replay required interpreter or changed receipt."); }
        Pass("fresh host without interpreter recovers saved start and never allocates again");
        await Deny("other actor result",404,()=>other.GetAsync(Base()+"/starts/"+request));
        await Deny("teacher result",403,()=>teacher.GetAsync(Base()+"/starts/"+request));
        var fault=new OnboardingExecutionFault("audit");var failedRequest=Guid.NewGuid();
        using(var faulty=new QaApiFactory(manifest,flags,isolatedClock:clock,isolatedOnboardingInterpreter:planner,isolatedDomainInterceptor:fault))using(var c=await Client(email,faulty)) {
            await Deny("mandatory audit rollback",503,()=>c.PostAsJsonAsync(Base()+"/conversations",new{requestId=failedRequest}));
            if(fault.Faults!=1)throw new InvalidOperationException("Start audit fault not hit."); }
        await Deny("rolled back start has no phantom receipt",404,()=>admin.GetAsync(Base()+"/starts/"+failedRequest));
        clock.Value=clock.Value.AddMinutes(11);
        await Deny("expired start",410,()=>admin.GetAsync(Base()+"/starts/"+request));clock.Value=clock.Value.AddMinutes(-11);
        using(var scope=factory.Services.CreateScope()) {var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var audit=await db.AuditLogs.AsNoTracking().SingleAsync(x=>x.EntityId==session&&x.Action=="PentaOnboardingConversationStarted");
            await db.AuditLogs.Where(x=>x.Id==audit.Id).ExecuteDeleteAsync();
            await Deny("missing start audit",409,()=>admin.GetAsync(Base()+"/starts/"+request));db.AuditLogs.Add(audit);await db.SaveChangesAsync(); }
        if(domain!=await Snapshot(false))throw new InvalidOperationException("Start changed existing domain or Identity state.");
        Pass("existing domain and Identity byte-identical before deliberate revocation fixture");
        using(var scope=factory.Services.CreateScope()) {var identity=scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            var user=await identity.Users.SingleAsync(x=>x.Id==actor);user.SecurityStamp=Guid.NewGuid().ToString();await identity.SaveChangesAsync(); }
        await Deny("old bearer stamp revoked",401,()=>admin.GetAsync(Base()+"/starts/"+request));
        using var currentAdmin=await Client(email);
        await Deny("fresh bearer cannot revive old authority epoch",409,()=>currentAdmin.GetAsync(Base()+"/starts/"+request));
        using(var scope=factory.Services.CreateScope()) {var identity=scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            var user=await identity.Users.SingleAsync(x=>x.Id==actor);user.IsActive=false;await identity.SaveChangesAsync(); }
        await Deny("revoked actor HTTP",403,()=>currentAdmin.GetAsync(Base()+"/starts/"+request));
        var directBefore=await Snapshot();
        using(var scope=factory.Services.CreateScope()) {
            var principal=new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(
                new[]{new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier,actor.ToString("D"))},"Synthetic authenticated service caller"));
            var denied=await scope.ServiceProvider.GetRequiredService<PentaOnboardingDraftService>().CoreStartResultAsync(principal,academy,request,default);
            if(denied.HttpStatus!=403)throw new InvalidOperationException("Revoked actor direct domain guard failed."); }
        if(directBefore!=await Snapshot())throw new InvalidOperationException("Revoked direct guard wrote state.");
        Pass("revoked actor direct domain403 no state changes");
        if(planner.Calls!=0)throw new InvalidOperationException("Bootstrap invoked interpretation.");
        Pass("zero interpretation calls or student creation");
        var evidence=Path.GetFullPath(Path.Combine("QA","EVIDENCE","penta-onboarding-start-"+manifest.RunId.ToString("N")));Directory.CreateDirectory(evidence);
        await File.WriteAllTextAsync(Path.Combine(evidence,"sql-postconditions.json"),JsonSerializer.Serialize(new{run=manifest.RunId.ToString("N"),passed=true,cases=count,
            startSessionsCreated=1,studentsCreated=0,interpreterCalls=planner.Calls,syntheticInterpreter=true,physicalDevices="NOT RUN"},new JsonSerializerOptions{WriteIndented=true}));
        Console.WriteLine("PASS: PENTAONBOARDINGSTART all "+count+" authenticated SQL cases; atomic session/draft/start receipt, current authority, no interpretation or domain creation.");
    }
}
