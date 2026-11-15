using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Domain.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AcademyDesk.Api.Tests;

public sealed class PentaEnrollmentLifecycleBrowserAccessTests
{
    private static readonly Guid RequestId = Guid.Parse("11111111-1111-4111-8111-111111111111");
    private static string Root(Fixture f) => $"/api/academies/{f.Academy}/penta/enrollments/lifecycle-browser";
    private static string Draft(Fixture f) => Root(f) + $"/conversations/{RequestId}/drafts/{RequestId}";

    [Theory][InlineData("pilot")][InlineData("enrollment")][InlineData("lifecycle")][InlineData("browser")][InlineData("browser-missing")]
    public async Task Every_browser_route_is_default_disabled_independently(string disabled)
    {
        using var f = new Fixture(disabled: disabled); await f.Seed("AcademyAdmin"); await f.Login(); var before = await f.Snapshot();
        foreach (var (method,path,body) in Requests(f))
        { using var request = new HttpRequestMessage(method,path); if (body is not null) request.Content = JsonContent.Create(body, options: new JsonSerializerOptions());
          using var response = await f.Actor.SendAsync(request); Assert.Equal(HttpStatusCode.NotFound,response.StatusCode); }
        Assert.Equal(before,await f.Snapshot());
    }
    [Theory][InlineData("Production")]
    public async Task Browser_contract_cannot_be_activated_outside_Testing(string environment)
    {
        using var f = new Fixture(environment); await f.Seed("AcademyAdmin"); await f.Login();
        foreach (var (method,path,body) in Requests(f))
        { using var request = new HttpRequestMessage(method,path); if (body is not null) request.Content = JsonContent.Create(body, options: new JsonSerializerOptions());
          using var response = await f.Actor.SendAsync(request); Assert.Equal(HttpStatusCode.NotFound,response.StatusCode); }
    }
    [Fact]
    public async Task Development_flag_is_refused_before_any_store_or_identity_dependency()
    {
        var config=new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{
            ["Penta:Enabled"]="true",["Penta:EnrollmentCreationEnabled"]="true",["Penta:EnrollmentLifecycleEnabled"]="true",["Penta:EnrollmentLifecycleBrowserContractsEnabled"]="true"}).Build();
        var workflow=new AcademyDesk.Api.Intelligence.Penta.PentaEnrollmentCreationWorkflow(null!,null!,null!,null!,null!,null!,config,new DevelopmentEnvironment(),TimeProvider.System);
        Assert.False(workflow.LifecycleBrowserEnabled);
        Assert.Equal(404,(await workflow.LifecycleBrowserReadinessAsync(new System.Security.Claims.ClaimsPrincipal(),Guid.NewGuid(),default)).HttpStatus);
        Assert.Equal(404,(await workflow.LifecycleBrowserStartAsync(new System.Security.Claims.ClaimsPrincipal(),Guid.NewGuid(),Guid.NewGuid(),true,default)).HttpStatus);
    }
    private sealed class DevelopmentEnvironment : Microsoft.Extensions.Hosting.IHostEnvironment
    {
        public string EnvironmentName {get;set;}="Development";
        public string ApplicationName {get;set;}="Synthetic enrollment availability";
        public string ContentRootPath {get;set;}=Path.GetTempPath();
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider {get;set;}=new Microsoft.Extensions.FileProviders.NullFileProvider();
    }
    [Fact]
    public async Task Current_pilot_role_revocation_denies_the_same_bearer_before_store_work()
    {
        using var f=new Fixture();await f.Seed("AcademyAdmin");await f.Login();
        using var before=await f.Actor.GetAsync(Root(f)+"/readiness");Assert.Equal(HttpStatusCode.ServiceUnavailable,before.StatusCode);
        await f.ChangeIdentity(db=>db.UserRoles.RemoveRange(db.UserRoles.Where(x=>x.UserId==f.ActorId)));
        var snapshot=await f.Snapshot();
        foreach(var(method,path,body) in Requests(f))
        {using var request=new HttpRequestMessage(method,path);if(body is not null)request.Content=JsonContent.Create(body,options:new JsonSerializerOptions());using var response=await f.Actor.SendAsync(request);Assert.Equal(HttpStatusCode.Forbidden,response.StatusCode);}
        Assert.Equal(snapshot,await f.Snapshot());
    }
    [Theory][InlineData("Teacher")][InlineData("Student")][InlineData("Guardian")][InlineData("Operations")][InlineData("Manager")]
    [InlineData("FrontDesk")][InlineData("FinanceUser")][InlineData("PlatformOwner")][InlineData("delegated")]
    public async Task Unsupported_roles_cannot_start_or_recover_a_browser_conversation(string role)
    {
        using var f = new Fixture(); await f.Seed(role); await f.Login(); var before = await f.Snapshot();
        foreach (var (method,path,body) in Requests(f))
        { using var request = new HttpRequestMessage(method,path); if (body is not null) request.Content = JsonContent.Create(body, options: new JsonSerializerOptions());
          using var response = await f.Actor.SendAsync(request); Assert.Equal(HttpStatusCode.Forbidden,response.StatusCode); }
        Assert.Equal(before,await f.Snapshot());
    }
    [Theory][InlineData("Owner")][InlineData("AcademyAdmin")]
    public async Task A_supported_actor_requires_the_real_SQL_transaction_store(string role)
    {
        using var f = new Fixture(); await f.Seed(role); await f.Login(); var before = await f.Snapshot();
        foreach (var (method,path,body) in Requests(f))
        { using var request = new HttpRequestMessage(method,path); if (body is not null) request.Content = JsonContent.Create(body, options: new JsonSerializerOptions());
          using var response = await f.Actor.SendAsync(request); Assert.Equal(HttpStatusCode.ServiceUnavailable,response.StatusCode);
          Assert.True(response.Headers.CacheControl?.NoStore); }
        Assert.Equal(before,await f.Snapshot());
    }
    [Theory][InlineData("anonymous",401)][InlineData("foreign",403)][InlineData("inactive-user",403)]
    [InlineData("inactive-academy",403)][InlineData("maintenance",503)][InlineData("actual-platform-owner",403)]
    public async Task Current_global_authority_applies_to_new_routes(string scenario,int expected)
    {
        using var f=new Fixture();await f.Seed("AcademyAdmin");await f.Login();
        if(scenario=="anonymous")f.Actor.DefaultRequestHeaders.Authorization=null;
        if(scenario=="inactive-user")await f.ChangeIdentity(db=>db.Users.Single(x=>x.Id==f.ActorId).IsActive=false);
        if(scenario=="inactive-academy")await f.Change(db=>db.Academies.Single(x=>x.Id==f.Academy).IsActive=false);
        if(scenario=="maintenance")await f.SaveMode(true);
        if(scenario=="actual-platform-owner")await f.ChangeIdentity(db=>db.Users.Single(x=>x.Id==f.ActorId).IsPlatformOwner=true);
        var before=await f.Snapshot();
        foreach(var (method,path,body) in Requests(f))
        {using var request=new HttpRequestMessage(method,scenario=="foreign"?path.Replace(f.Academy.ToString(),f.Foreign.ToString()):path);if(body is not null)request.Content=JsonContent.Create(body, options: new JsonSerializerOptions());
         using var response=await f.Actor.SendAsync(request);Assert.Equal(expected,(int)response.StatusCode);}
        Assert.Equal(before,await f.Snapshot());
    }
    [Theory][InlineData("start-extra")][InlineData("start-null")][InlineData("prepare-context")][InlineData("prepare-create")]
    [InlineData("approve-intent")][InlineData("approve-hash")][InlineData("execute-approval")][InlineData("execute-extra")]
    public async Task Browser_command_shapes_are_exact_before_store_work(string scenario)
    {
        using var f=new Fixture();await f.Seed("AcademyAdmin");await f.Login();
        var reference=new{kind="reference",value=RequestId};var payload=new{tool_key="AcademyDesk.Enrollment.Create",tool_revision=1,fields=new{StudentId=reference,BatchId=reference,Status=new{kind="text",value="Active"}}};
        var (path,method,body)=scenario switch
        {
          "start-extra"=>(Root(f)+$"/conversations/{RequestId}",HttpMethod.Put,"{\"role\":\"Owner\"}"),
          "start-null"=>(Root(f)+$"/conversations/{RequestId}",HttpMethod.Put,"null"),
          "prepare-context"=>(Draft(f),HttpMethod.Put,JsonSerializer.Serialize(new{contextVersion=1,payload=ValidPayload(f)})),
          "prepare-create"=>(Draft(f),HttpMethod.Put,JsonSerializer.Serialize(new{contextVersion=0,payload})),
          "approve-intent"=>(Draft(f)+"/approve",HttpMethod.Post,JsonSerializer.Serialize(new{revision=1,payloadHash=new string('a',64),intent="ApproveEnrollmentCreate"})),
          "approve-hash"=>(Draft(f)+"/approve",HttpMethod.Post,JsonSerializer.Serialize(new{revision=1,payloadHash=new string('A',64),intent="ApproveEnrollmentUpdate"})),
          "execute-approval"=>(Draft(f)+"/execute",HttpMethod.Post,JsonSerializer.Serialize(new{revision=1,payloadHash=new string('a',64),approvalId=Guid.Empty})),
          _=>(Draft(f)+"/execute",HttpMethod.Post,JsonSerializer.Serialize(new{revision=1,payloadHash=new string('a',64),approvalId=RequestId,role="Owner"}))
        };
        var before=await f.Snapshot();using var request=new HttpRequestMessage(method,path){Content=new StringContent(body,System.Text.Encoding.UTF8,"application/json")};
        using var response=await f.Actor.SendAsync(request);Assert.Equal(HttpStatusCode.BadRequest,response.StatusCode);Assert.Equal(before,await f.Snapshot());
    }
    [Fact]
    public async Task Semantic_tool_refusal_does_not_replace_current_pilot_authorization()
    {
        using var f=new Fixture();await f.Seed("Operations");await f.Login();
        var payload=new{tool_key="AcademyDesk.Enrollment.Create",tool_revision=1,fields=new{StudentId=new{kind="reference",value=RequestId},BatchId=new{kind="reference",value=RequestId},Status=new{kind="text",value="Active"}}};
        using var request=new HttpRequestMessage(HttpMethod.Put,Draft(f)){Content=JsonContent.Create(new{contextVersion=0,payload},options:new JsonSerializerOptions())};
        using var response=await f.Actor.SendAsync(request);Assert.Equal(HttpStatusCode.Forbidden,response.StatusCode);
    }
    [Fact]
    public async Task An_empty_start_key_never_creates_a_session()
    {using var f=new Fixture();await f.Seed("AcademyAdmin");await f.Login();var before=await f.Snapshot();using var response=await f.Actor.PutAsJsonAsync(Root(f)+$"/conversations/{Guid.Empty}",new{});Assert.Equal(HttpStatusCode.BadRequest,response.StatusCode);Assert.Equal(before,await f.Snapshot());}

    [Theory][InlineData("Owner")][InlineData("AcademyAdmin")]
    public async Task Transfer_prepare_and_intent_are_separate_supported_store_contracts(string role)
    {
        using var f=new Fixture();await f.Seed(role);await f.Login();var before=await f.Snapshot();
        var payload=new{tool_key="AcademyDesk.Enrollment.Transfer",tool_revision=1,fields=new{EnrollmentId=new{kind="reference",value=f.Enrollment},TargetBatchId=new{kind="reference",value=f.Batch},TransferDate=new{kind="date",value="2027-01-02"}}};
        using var prepare=new HttpRequestMessage(HttpMethod.Put,Draft(f)){Content=JsonContent.Create(new{contextVersion=0,payload},options:new JsonSerializerOptions())};
        using var prepared=await f.Actor.SendAsync(prepare);Assert.Equal(HttpStatusCode.ServiceUnavailable,prepared.StatusCode);
        using var approve=new HttpRequestMessage(HttpMethod.Post,Draft(f)+"/approve"){Content=JsonContent.Create(new{revision=1,payloadHash=new string('a',64),intent="ApproveEnrollmentTransfer"},options:new JsonSerializerOptions())};
        using var approved=await f.Actor.SendAsync(approve);Assert.Equal(HttpStatusCode.ServiceUnavailable,approved.StatusCode);Assert.Equal(before,await f.Snapshot());
        using var creation=await f.Actor.GetAsync(Root(f).Replace("/lifecycle-browser","")+"/browser-readiness");Assert.Equal(HttpStatusCode.NotFound,creation.StatusCode);
    }
    [Theory][InlineData("")][InlineData("X")][InlineData("Mi\nra")]
    public async Task Invalid_source_search_is_refused_without_store_writes(string q)
    {
        using var f=new Fixture();await f.Seed("AcademyAdmin");await f.Login();var before=await f.Snapshot();using var response=await f.Actor.GetAsync(Root(f)+"/search?q="+Uri.EscapeDataString(q));Assert.Equal(HttpStatusCode.BadRequest,response.StatusCode);Assert.Equal(before,await f.Snapshot());
    }
    [Fact]
    public async Task A_reason_is_required_for_nonactive_lifecycle_intent()
    {
        using var f=new Fixture();await f.Seed("AcademyAdmin");await f.Login();var payload=new{tool_key="AcademyDesk.Enrollment.Update",tool_revision=1,fields=new{EnrollmentId=new{kind="reference",value=f.Enrollment},Status=new{kind="text",value="Paused"}}};
        using var request=new HttpRequestMessage(HttpMethod.Put,Draft(f)){Content=JsonContent.Create(new{contextVersion=0,payload},options:new JsonSerializerOptions())};using var response=await f.Actor.SendAsync(request);Assert.Equal(HttpStatusCode.BadRequest,response.StatusCode);
    }
    private static object ValidPayload(Fixture f)=>new{tool_key="AcademyDesk.Enrollment.Update",tool_revision=1,fields=new{EnrollmentId=new{kind="reference",value=f.Enrollment},Status=new{kind="text",value="Paused"},EndDate=new{kind="date",value="2027-01-02"},LifecycleReason=new{kind="text",value="Synthetic reviewed reason"}}};
    private static IEnumerable<(HttpMethod Method,string Path,object? Body)> Requests(Fixture f)
    {
        yield return(HttpMethod.Get,Root(f)+"/readiness",null);
        yield return(HttpMethod.Get,Root(f)+"/search?q=Mira",null);
        yield return(HttpMethod.Put,Root(f)+$"/conversations/{RequestId}",new{});
        yield return(HttpMethod.Get,Root(f)+$"/starts/{RequestId}",null);
        yield return(HttpMethod.Put,Draft(f),new{contextVersion=0,payload=ValidPayload(f)});
        yield return(HttpMethod.Get,Draft(f),null);
        yield return(HttpMethod.Post,Draft(f)+"/approve",new{revision=1,payloadHash=new string('a',64),intent="ApproveEnrollmentUpdate"});
        yield return(HttpMethod.Post,Draft(f)+"/execute",new{revision=1,payloadHash=new string('a',64),approvalId=RequestId});
    }
    private sealed class Fixture : IDisposable
    {
        internal HttpFactory Factory; internal HttpClient Owner { get; } internal HttpClient Actor { get; }
        internal Guid Academy = Guid.NewGuid(), Foreign = Guid.NewGuid(), Student = Guid.NewGuid(), Guardian = Guid.NewGuid(), Teacher = Guid.NewGuid(), OwnerId = Guid.NewGuid(), ActorId = Guid.NewGuid();
        internal string RefreshToken = ""; internal Guid Batch = Guid.NewGuid(), Enrollment = Guid.NewGuid(), Course = Guid.NewGuid(), Branch = Guid.NewGuid();
        internal string Leads => $"/api/academies/{Academy}/leads";
        internal Fixture(string environment = "Testing", string? disabled = null) { Factory = new HttpFactory(environment, disabled); Owner = Factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false }); Actor = Factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false }); }
        internal async Task Seed(string actorRole)
        {
            await using var scope = Factory.Services.CreateAsyncScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            db.AddRange(new Academy { Id = Academy, Name = "Synthetic maintenance academy", EnabledModulesJson = "[\"Core\",\"Sales\",\"Finance\",\"AcademicGovernance\"]" }, new Academy { Id = Foreign, Name = "Foreign academy" });
            db.Add(new Student { Id = Student, AcademyId = Academy, FirstName = "Synthetic", LastName = "Student" }); db.Add(new Guardian { Id = Guardian, AcademyId = Academy, FirstName = "Synthetic", LastName = "Parent" }); db.Add(new Teacher { Id = Teacher, AcademyId = Academy, FirstName = "Synthetic", LastName = "Teacher" }); db.Add(new StudentGuardian { AcademyId = Academy, StudentId = Student, GuardianId = Guardian, CanAccessPortal = true }); db.Add(new ProgramCourse { Id = Course, AcademyId = Academy, Name = "Music foundation", AcademyType = "Music" }); db.Add(new Branch { Id = Branch, AcademyId = Academy, Name = "Central", AddressLine1 = "Synthetic private branch address" }); db.Add(new Batch { Id = Batch, AcademyId = Academy, CourseId = Course, TeacherId = Teacher, BranchId = Branch, Name = "Synthetic lifecycle batch", Capacity = 10, WaitlistCapacity = 10 }); db.Add(new Enrollment { Id = Enrollment, AcademyId = Academy, StudentId = Student, BatchId = Batch, StartDate = new(2026,10,1), Status = "Active" }); await db.SaveChangesAsync();
            var roles = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>(); var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            foreach (var role in new[] { "AcademyAdmin", actorRole }.Distinct()) Assert.True((await roles.CreateAsync(new ApplicationRole { Name = role, IsSystemRole = role != "delegated", PermissionsJson = role == "delegated" ? "[\"scheduling.manage\"]" : "[]" })).Succeeded);
            foreach (var owner in new[] { true, false })
            {
                var user = new ApplicationUser { Id = owner ? OwnerId : ActorId, UserName = (owner ? "owner" : "actor") + "@maintenance.example.invalid", Email = (owner ? "owner" : "actor") + "@maintenance.example.invalid", EmailConfirmed = true, DisplayName = "Synthetic maintenance actor", AcademyId = Academy, IsPlatformOwner = owner, StudentId = !owner && actorRole == "Student" ? Student : null, GuardianId = !owner && actorRole == "Guardian" ? Guardian : null, TeacherId = !owner && actorRole == "Teacher" ? Teacher : null };
                Assert.True((await users.CreateAsync(user, "Synthetic!39Ab")).Succeeded); Assert.True((await users.AddToRoleAsync(user, owner ? "AcademyAdmin" : actorRole)).Succeeded);
            }
        }
        internal async Task Grant(string kind) { await using var scope=Factory.Services.CreateAsyncScope(); var db=scope.ServiceProvider.GetRequiredService<IdentityDbContext>();db.Add(new AccessGrant { AcademyId=kind=="foreign-grant"?Foreign:Academy, UserId=ActorId, GrantedByUserId=OwnerId, PermissionsJson=kind=="wrong"?"[\"students.manage\"]":"[\"scheduling.manage\"]", IsPermanent=kind=="permanent", ExpiresAtUtc=DateTimeOffset.UtcNow.AddHours(kind=="expired"?-1:1), RevokedAtUtc=kind=="revoked"?DateTimeOffset.UtcNow:null });await db.SaveChangesAsync(); }
        internal string ReadPath(string role) => role switch { "Guardian" or "Student" => "/api/portal/me", "Teacher" => "/api/teacher/me", "FinanceUser" => $"/api/academies/{Academy}/invoices", "Operations" => $"/api/academies/{Academy}/batches", _ => Leads };
        internal async Task Login() { using var response = await Owner.PostAsJsonAsync("/api/auth/login", new { email = "owner@maintenance.example.invalid", password = "Synthetic!39Ab" }); Assert.Equal(HttpStatusCode.OK, response.StatusCode); using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync()); Owner.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", json.RootElement.GetProperty("accessToken").GetString()); await LoginActor(); }
        internal async Task LoginActor() { Actor.DefaultRequestHeaders.Authorization = null; using var response = await Actor.PostAsJsonAsync("/api/auth/login", new { email = "actor@maintenance.example.invalid", password = "Synthetic!39Ab" }); Assert.Equal(HttpStatusCode.OK, response.StatusCode); using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync()); Actor.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", json.RootElement.GetProperty("accessToken").GetString()); RefreshToken = json.RootElement.GetProperty("refreshToken").GetString()!; }
        internal async Task SaveMode(bool enabled, string? message = null) { using var response = await Owner.PutAsJsonAsync("/api/platform/settings", new { platformName = "Synthetic platform", defaultCurrency = "INR", defaultTrialDays = 30, dataRetentionDays = 2555, maintenanceMode = enabled, statusMessage = message }); Assert.Equal(HttpStatusCode.OK, response.StatusCode); }
        internal async Task Change(Action<AcademyDeskDbContext> action) { await using var scope = Factory.Services.CreateAsyncScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); action(db); await db.SaveChangesAsync(); }
        internal async Task ChangeIdentity(Action<IdentityDbContext> action) { await using var scope = Factory.Services.CreateAsyncScope(); var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>(); action(db); await db.SaveChangesAsync(); }
        internal async Task<string> Snapshot() { await using var scope = Factory.Services.CreateAsyncScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); var identity = scope.ServiceProvider.GetRequiredService<IdentityDbContext>(); return JsonSerializer.Serialize(new { sessions = await db.ClassSessions.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), miniSessions = await db.PentaMiniSessions.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), drafts = await db.PentaOnboardingDrafts.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), settings = await db.PlatformSettings.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), leads = await db.Leads.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), audits = await db.AuditLogs.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), platformAudits = await db.PlatformAuditEntries.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), enrollments = await db.Enrollments.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), courses = await db.Courses.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), teachers = await db.Teachers.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), branches = await db.Branches.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), students = await db.Students.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), batches = await db.Batches.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), grants = await identity.AccessGrants.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), roles = await identity.Roles.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), users = await identity.Users.AsNoTracking().OrderBy(x => x.Id).ToListAsync() }); }
        public void Dispose() { Owner.Dispose(); Actor.Dispose(); Factory.Dispose(); }
    }
    private sealed class HttpFactory(string environment, string? disabled) : WebApplicationFactory<Program>
    {
        private readonly string database = $"platform-maintenance-{Guid.NewGuid():N}";
        internal string WebRoot { get; } = Path.Combine(Path.GetTempPath(), "c40-enrollment-lifecycle-browser-" + Guid.NewGuid().ToString("N"));
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            Directory.CreateDirectory(WebRoot); builder.UseWebRoot(WebRoot); builder.UseEnvironment(environment); builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?> { ["Database:ApplyMigrationsOnStartup"] = "false", ["Bootstrap:PlatformOwnerEmail"] = "", ["Bootstrap:PlatformOwnerPassword"] = "", ["MediaStorage:Enabled"] = "false", ["Penta:Enabled"] = disabled == "pilot" ? "false" : "true", ["Penta:EnrollmentCreationEnabled"] = disabled == "enrollment" ? "false" : "true", ["Penta:EnrollmentBrowserContractsEnabled"] = "false", ["Penta:EnrollmentLifecycleEnabled"] = disabled == "lifecycle" ? "false" : "true", ["Penta:EnrollmentLifecycleBrowserContractsEnabled"] = disabled == "browser-missing" ? null : disabled == "browser" ? "false" : "true" }));
            builder.ConfigureServices(services => { services.RemoveAll<DbContextOptions<AcademyDeskDbContext>>(); services.RemoveAll<DbContextOptions<IdentityDbContext>>(); services.RemoveAll<IDbContextOptionsConfiguration<AcademyDeskDbContext>>(); services.RemoveAll<IDbContextOptionsConfiguration<IdentityDbContext>>(); services.RemoveAll<AcademyDeskDbContext>(); services.RemoveAll<IdentityDbContext>(); services.AddDbContext<AcademyDeskDbContext>(o => o.UseInMemoryDatabase(database + "-domain")); services.AddDbContext<IdentityDbContext>(o => o.UseInMemoryDatabase(database + "-identity")); });
        }
        protected override void Dispose(bool disposing) { base.Dispose(disposing); if (disposing && Directory.Exists(WebRoot)) { if (Directory.GetParent(WebRoot)?.FullName != Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar) || !Path.GetFileName(WebRoot).StartsWith("c40-enrollment-lifecycle-browser-", StringComparison.Ordinal) || new DirectoryInfo(WebRoot).Attributes.HasFlag(FileAttributes.ReparsePoint)) throw new InvalidOperationException("Refused nonowned cleanup."); Directory.Delete(WebRoot, true); } }
    }
}
