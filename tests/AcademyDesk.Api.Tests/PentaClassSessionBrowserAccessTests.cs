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

public sealed class PentaClassSessionBrowserAccessTests
{
    private static readonly Guid RequestId = Guid.Parse("a1111111-1111-4111-8111-111111111111");
    private static string Root(Fixture f) => $"/api/academies/{f.Academy}/penta/class-scheduling/browser";
    private static string Draft(Fixture f) => Root(f) + $"/conversations/{RequestId}/drafts/{RequestId}";
    private static object Payload(Fixture f, bool move=false) => new { tool_key=move?"AcademyDesk.ClassSession.Reschedule":"AcademyDesk.ClassSession.Create",tool_revision=1,
        fields=new Dictionary<string,object> { [move?"SessionId":"BatchId"]=new{kind="reference",value=move?RequestId:f.Batch},["StartUtc"]=new{kind="text",value="2027-01-01T10:00:00Z"},["EndUtc"]=new{kind="text",value="2027-01-01T11:00:00Z"},["DeliveryMode"]=new{kind="text",value="InPerson"},["RoomName"]=new{kind="text",value="Synthetic studio"} } };
    private static IEnumerable<(HttpMethod Method,string Path,object? Body)> Requests(Fixture f)
    {
        yield return(HttpMethod.Get,Root(f)+"/readiness",null);
        yield return(HttpMethod.Get,Root(f)+"/batches?q=Music",null);
        yield return(HttpMethod.Get,Root(f)+"/sessions?q=Music",null);
        yield return(HttpMethod.Put,Root(f)+$"/conversations/{RequestId}",new{});
        yield return(HttpMethod.Get,Root(f)+$"/starts/{RequestId}",null);
        yield return(HttpMethod.Put,Draft(f),new{contextVersion=0,payload=Payload(f)});
        yield return(HttpMethod.Get,Draft(f),null);
        yield return(HttpMethod.Post,Draft(f)+"/approve",new{revision=1,payloadHash=new string('a',64),intent="ApproveClassSessionSchedule"});
        yield return(HttpMethod.Post,Draft(f)+"/execute",new{revision=1,payloadHash=new string('a',64),approvalId=RequestId});
    }
    private static async Task Matrix(Fixture f,int code,string? replace=null)
    {
        var before=await f.Snapshot();
        foreach(var(method,path,body) in Requests(f)) { using var request=new HttpRequestMessage(method,replace is null?path:path.Replace(f.Academy.ToString(),replace));if(body is not null)request.Content=JsonContent.Create(body,options:new JsonSerializerOptions());using var response=await f.Actor.SendAsync(request);Assert.Equal(code,(int)response.StatusCode);if(code==503)Assert.True(response.Headers.CacheControl?.NoStore); }
        Assert.Equal(before,await f.Snapshot());
    }
    [Theory][InlineData("pilot")][InlineData("schedule")][InlineData("reschedule")][InlineData("browser")][InlineData("browser-missing")]
    public async Task All_nine_routes_require_each_independent_enablement(string disabled)
    {using var f=new Fixture(disabled:disabled);await f.Seed("AcademyAdmin");await f.Login();await Matrix(f,404);}
    [Theory][InlineData("Production")]
    public async Task Browser_protocol_cannot_be_activated_in_Production(string environment)
    {using var f=new Fixture(environment);await f.Seed("AcademyAdmin");await f.Login();await Matrix(f,404);}
    [Theory][InlineData("Owner")][InlineData("AcademyAdmin")]
    public async Task Supported_actor_fails_closed_without_real_linked_SQL(string role)
    {using var f=new Fixture();await f.Seed(role);await f.Login();await Matrix(f,503);}
    [Theory][InlineData("Teacher")][InlineData("Student")][InlineData("Guardian")][InlineData("Operations")][InlineData("Manager")][InlineData("FrontDesk")][InlineData("FinanceUser")][InlineData("PlatformOwner")][InlineData("delegated")]
    public async Task Other_roles_and_granular_scheduling_grants_do_not_expand_the_pilot(string role)
    {using var f=new Fixture();await f.Seed(role);await f.Login();await Matrix(f,403);}
    [Theory][InlineData("anonymous",401)][InlineData("foreign",403)][InlineData("inactive-user",403)][InlineData("inactive-academy",403)][InlineData("maintenance",503)][InlineData("actual-platform-owner",403)]
    public async Task Current_global_authority_applies_before_host_store_work(string scenario,int code)
    {
        using var f=new Fixture();await f.Seed("AcademyAdmin");await f.Login();
        if(scenario=="anonymous")f.Actor.DefaultRequestHeaders.Authorization=null;
        if(scenario=="inactive-user")await f.ChangeIdentity(db=>db.Users.Single(x=>x.Id==f.ActorId).IsActive=false);
        if(scenario=="inactive-academy")await f.Change(db=>db.Academies.Single(x=>x.Id==f.Academy).IsActive=false);
        if(scenario=="maintenance")await f.SaveMode(true);
        if(scenario=="actual-platform-owner")await f.ChangeIdentity(db=>db.Users.Single(x=>x.Id==f.ActorId).IsPlatformOwner=true);
        await Matrix(f,code,scenario=="foreign"?f.Foreign.ToString():null);
    }
    [Fact]
    public async Task Same_bearer_current_role_removal_denies_all_routes()
    {
        using var f=new Fixture();await f.Seed("AcademyAdmin");await f.Login();using var initial=await f.Actor.GetAsync(Root(f)+"/readiness");Assert.Equal(HttpStatusCode.ServiceUnavailable,initial.StatusCode);
        await f.ChangeIdentity(db=>db.UserRoles.RemoveRange(db.UserRoles.Where(x=>x.UserId==f.ActorId)));await Matrix(f,403);
    }
    [Theory][InlineData("start-extra")][InlineData("start-null")][InlineData("prepare-context")][InlineData("prepare-tool")][InlineData("prepare-null")][InlineData("approve-intent")][InlineData("approve-hash")][InlineData("execute-approval")][InlineData("execute-extra")]
    public async Task Exact_browser_commands_are_refused_before_SQL(string scenario)
    {
        using var f=new Fixture();await f.Seed("AcademyAdmin");await f.Login();
        var(path,method,body)=scenario switch {
          "start-extra"=>(Root(f)+$"/conversations/{RequestId}",HttpMethod.Put,"{\"role\":\"Owner\"}"),
          "start-null"=>(Root(f)+$"/conversations/{RequestId}",HttpMethod.Put,"null"),
          "prepare-context"=>(Draft(f),HttpMethod.Put,JsonSerializer.Serialize(new{contextVersion=1,payload=Payload(f)})),
          "prepare-tool"=>(Draft(f),HttpMethod.Put,JsonSerializer.Serialize(new{contextVersion=0,payload=new{tool_key="AcademyDesk.Enrollment.Create",tool_revision=1,fields=new{}}})),
          "prepare-null"=>(Draft(f),HttpMethod.Put,"{\"contextVersion\":0,\"payload\":null}"),
          "approve-intent"=>(Draft(f)+"/approve",HttpMethod.Post,JsonSerializer.Serialize(new{revision=1,payloadHash=new string('a',64),intent="ApproveEnrollmentCreate"})),
          "approve-hash"=>(Draft(f)+"/approve",HttpMethod.Post,JsonSerializer.Serialize(new{revision=1,payloadHash=new string('A',64),intent="ApproveClassSessionSchedule"})),
          "execute-approval"=>(Draft(f)+"/execute",HttpMethod.Post,JsonSerializer.Serialize(new{revision=1,payloadHash=new string('a',64),approvalId=Guid.Empty})),
          _=>(Draft(f)+"/execute",HttpMethod.Post,JsonSerializer.Serialize(new{revision=1,payloadHash=new string('a',64),approvalId=RequestId,role="Owner"})) };
        var before=await f.Snapshot();using var request=new HttpRequestMessage(method,path){Content=new StringContent(body,System.Text.Encoding.UTF8,"application/json")};using var response=await f.Actor.SendAsync(request);Assert.Equal(HttpStatusCode.BadRequest,response.StatusCode);Assert.Equal(before,await f.Snapshot());
    }
    [Theory][InlineData(false)][InlineData(true)]
    public async Task Creation_and_reschedule_intents_reach_the_required_store(bool move)
    {
        using var f=new Fixture();await f.Seed("AcademyAdmin");await f.Login();var before=await f.Snapshot();
        using var request=new HttpRequestMessage(HttpMethod.Put,Draft(f)){Content=JsonContent.Create(new{contextVersion=0,payload=Payload(f,move)},options:new JsonSerializerOptions())};using var response=await f.Actor.SendAsync(request);Assert.Equal(HttpStatusCode.ServiceUnavailable,response.StatusCode);
        using var approve=await f.Actor.PostAsJsonAsync(Draft(f)+"/approve",new{revision=1,payloadHash=new string('a',64),intent=move?"ApproveClassSessionReschedule":"ApproveClassSessionSchedule"});Assert.Equal(HttpStatusCode.ServiceUnavailable,approve.StatusCode);Assert.Equal(before,await f.Snapshot());
    }
    [Theory][InlineData("")][InlineData("x")][InlineData("mu\nsic")]
    public async Task Both_lookup_queries_are_bounded_before_SQL(string query)
    {using var f=new Fixture();await f.Seed("AcademyAdmin");await f.Login();var before=await f.Snapshot();foreach(var kind in new[]{"batches","sessions"}){using var response=await f.Actor.GetAsync(Root(f)+"/"+kind+"?q="+Uri.EscapeDataString(query));Assert.Equal(HttpStatusCode.BadRequest,response.StatusCode);}Assert.Equal(before,await f.Snapshot());}
    [Fact]
    public async Task Semantic_refusal_does_not_replace_current_pilot_authorization()
    {using var f=new Fixture();await f.Seed("Operations");await f.Login();using var response=await f.Actor.PutAsJsonAsync(Draft(f),new{contextVersion=0,payload=new{tool_key="AcademyDesk.Enrollment.Create",tool_revision=1,fields=new{}}});Assert.Equal(HttpStatusCode.Forbidden,response.StatusCode);}
    [Fact]
    public async Task Empty_start_never_creates_a_session()
    {using var f=new Fixture();await f.Seed("AcademyAdmin");await f.Login();var before=await f.Snapshot();using var response=await f.Actor.PutAsJsonAsync(Root(f)+$"/conversations/{Guid.Empty}",new{});Assert.Equal(HttpStatusCode.BadRequest,response.StatusCode);Assert.Equal(before,await f.Snapshot());}
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
        internal string WebRoot { get; } = Path.Combine(Path.GetTempPath(), "c43-class-session-browser-" + Guid.NewGuid().ToString("N"));
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            Directory.CreateDirectory(WebRoot); builder.UseWebRoot(WebRoot); builder.UseEnvironment(environment); builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?> { ["Database:ApplyMigrationsOnStartup"] = "false", ["Bootstrap:PlatformOwnerEmail"] = "", ["Bootstrap:PlatformOwnerPassword"] = "", ["MediaStorage:Enabled"] = "false", ["Penta:Enabled"] = disabled == "pilot" ? "false" : "true", ["Penta:ClassSessionSchedulingEnabled"] = disabled == "schedule" ? "false" : "true", ["Penta:ClassSessionReschedulingEnabled"] = disabled == "reschedule" ? "false" : "true", ["Penta:ClassSchedulingBrowserContractsEnabled"] = disabled == "browser-missing" ? null : disabled == "browser" ? "false" : "true" }));
            builder.ConfigureServices(services => { services.RemoveAll<DbContextOptions<AcademyDeskDbContext>>(); services.RemoveAll<DbContextOptions<IdentityDbContext>>(); services.RemoveAll<IDbContextOptionsConfiguration<AcademyDeskDbContext>>(); services.RemoveAll<IDbContextOptionsConfiguration<IdentityDbContext>>(); services.RemoveAll<AcademyDeskDbContext>(); services.RemoveAll<IdentityDbContext>(); services.AddDbContext<AcademyDeskDbContext>(o => o.UseInMemoryDatabase(database + "-domain")); services.AddDbContext<IdentityDbContext>(o => o.UseInMemoryDatabase(database + "-identity")); });
        }
        protected override void Dispose(bool disposing) { base.Dispose(disposing); if (disposing && Directory.Exists(WebRoot)) { if (Directory.GetParent(WebRoot)?.FullName != Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar) || !Path.GetFileName(WebRoot).StartsWith("c43-class-session-browser-", StringComparison.Ordinal) || new DirectoryInfo(WebRoot).Attributes.HasFlag(FileAttributes.ReparsePoint)) throw new InvalidOperationException("Refused nonowned cleanup."); Directory.Delete(WebRoot, true); } }
    }
}
