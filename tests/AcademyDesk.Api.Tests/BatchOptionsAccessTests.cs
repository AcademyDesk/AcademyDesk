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

public sealed class BatchOptionsAccessTests
{
    private static string Route(Fixture f)=>$"/api/academies/{f.Academy}/batches/options";
    [Theory]
    [InlineData("Operations")][InlineData("Manager")][InlineData("Owner")][InlineData("AcademyAdmin")][InlineData("delegated")][InlineData("temporary")][InlineData("permanent")]
    public async Task Batch_authority_can_read_minimal_options_without_related_management(string role)
    {
        using var f=new Fixture();await f.Seed(role);if(role is "temporary" or "permanent")await f.Grant(role);await f.Login();var before=await f.Snapshot();using var response=await f.Actor.GetAsync(Route(f));Assert.Equal(HttpStatusCode.OK,response.StatusCode);Assert.Contains("no-store",response.Headers.CacheControl?.ToString()??"");Assert.Equal(before,await f.Snapshot());
        if(role is "Operations" or "Manager" or "delegated" or "temporary" or "permanent")foreach(var endpoint in new[]{"courses","teachers","branches"}){using var full=await f.Actor.GetAsync($"/api/academies/{f.Academy}/{endpoint}");Assert.Equal(HttpStatusCode.Forbidden,full.StatusCode);}
    }
    [Theory]
    [InlineData("Sales")][InlineData("Marketing")][InlineData("FrontDesk")][InlineData("FinanceUser")][InlineData("Teacher")][InlineData("Student")][InlineData("Guardian")][InlineData("PlatformOwner")][InlineData("none")][InlineData("expired")][InlineData("revoked")][InlineData("wrong")][InlineData("foreign-grant")]
    public async Task Unrelated_or_invalid_authority_cannot_read_options(string role)
    {
        using var f=new Fixture();await f.Seed(role);if(role is "expired" or "revoked" or "wrong" or "foreign-grant")await f.Grant(role);await f.Login();var before=await f.Snapshot();using var response=await f.Actor.GetAsync(Route(f));Assert.Equal(HttpStatusCode.Forbidden,response.StatusCode);Assert.Equal(before,await f.Snapshot());
    }
    [Theory][InlineData(false)][InlineData(true)]
    public async Task Current_role_or_grant_revocation_applies_to_the_existing_bearer(bool direct)
    {
        using var f=new Fixture();await f.Seed(direct?"temporary":"delegated");if(direct)await f.Grant("temporary");await f.Login();using var before=await f.Actor.GetAsync(Route(f));Assert.Equal(HttpStatusCode.OK,before.StatusCode);await f.ChangeIdentity(db=>{if(direct)db.AccessGrants.Single().RevokedAtUtc=DateTimeOffset.UtcNow;else db.Roles.Single(x=>x.Name=="delegated").PermissionsJson="[]";});var snapshot=await f.Snapshot();using var after=await f.Actor.GetAsync(Route(f));Assert.Equal(HttpStatusCode.Forbidden,after.StatusCode);Assert.Equal(snapshot,await f.Snapshot());
    }
    [Fact]
    public async Task Core_only_Operations_can_load_labels_without_MultiBranch()
    {
        using var f=new Fixture();await f.Seed("Operations");await f.Login();await f.Change(db=>db.Academies.Single(x=>x.Id==f.Academy).EnabledModulesJson="[\"Core\"]");using var options=await f.Actor.GetAsync(Route(f));Assert.Equal(HttpStatusCode.OK,options.StatusCode);using var branches=await f.Actor.GetAsync($"/api/academies/{f.Academy}/branches");Assert.Equal(HttpStatusCode.Forbidden,branches.StatusCode);
    }
    [Fact]
    public async Task Exact_minimal_projection_keeps_history_and_excludes_foreign_and_private_fields()
    {
        using var f=new Fixture();await f.Seed("Operations");await f.Login();var inactive=Guid.NewGuid();await f.Change(db=>{db.Teachers.Single().Email="private@example.invalid";db.Teachers.Single().CompensationJson="Synthetic private compensation";db.AddRange(new ProgramCourse{Id=inactive,AcademyId=f.Academy,Name="Historical course",AcademyType="Arts"},new Teacher{Id=inactive,AcademyId=f.Academy,FirstName="Historical",LastName="Teacher",IsActive=false},new Branch{Id=inactive,AcademyId=f.Academy,Name="Historical branch",IsActive=false},new ProgramCourse{AcademyId=f.Foreign,Name="Foreign excluded"},new Teacher{AcademyId=f.Foreign,FirstName="Foreign",LastName="Excluded"},new Branch{AcademyId=f.Foreign,Name="Foreign excluded"});});var before=await f.Snapshot();using var response=await f.Actor.GetAsync(Route(f));Assert.Equal(HttpStatusCode.OK,response.StatusCode);var body=await response.Content.ReadAsStringAsync();using var json=JsonDocument.Parse(body);var root=json.RootElement;Assert.Equal(new[]{"branches","courses","teachers"},root.EnumerateObject().Select(x=>x.Name).OrderBy(x=>x).ToArray());foreach(var name in new[]{"courses","teachers","branches"}){Assert.Equal(2,root.GetProperty(name).GetArrayLength());Assert.Contains(root.GetProperty(name).EnumerateArray(),x=>x.GetProperty("id").GetGuid()==inactive);}foreach(var row in root.GetProperty("courses").EnumerateArray())Assert.Equal(new[]{"academyType","id","name"},row.EnumerateObject().Select(x=>x.Name).OrderBy(x=>x).ToArray());foreach(var row in root.GetProperty("teachers").EnumerateArray())Assert.Equal(new[]{"firstName","id","lastName"},row.EnumerateObject().Select(x=>x.Name).OrderBy(x=>x).ToArray());foreach(var row in root.GetProperty("branches").EnumerateArray())Assert.Equal(new[]{"id","name"},row.EnumerateObject().Select(x=>x.Name).OrderBy(x=>x).ToArray());Assert.DoesNotContain("private",body,StringComparison.OrdinalIgnoreCase);Assert.DoesNotContain("Foreign",body,StringComparison.OrdinalIgnoreCase);Assert.Equal(before,await f.Snapshot());
    }
    [Fact]
    public async Task Empty_labels_are_a_read_without_default_record_creation()
    {
        using var f=new Fixture();await f.Seed("Operations");await f.Login();await f.Change(db=>{db.Courses.RemoveRange(db.Courses);db.Teachers.RemoveRange(db.Teachers);db.Branches.RemoveRange(db.Branches);});var before=await f.Snapshot();using var response=await f.Actor.GetAsync(Route(f));Assert.Equal(HttpStatusCode.OK,response.StatusCode);using var json=JsonDocument.Parse(await response.Content.ReadAsStringAsync());foreach(var name in new[]{"courses","teachers","branches"})Assert.Empty(json.RootElement.GetProperty(name).EnumerateArray());Assert.Equal(before,await f.Snapshot());
    }
    [Theory][InlineData("anonymous",401)][InlineData("foreign",403)][InlineData("suspended-user",403)][InlineData("suspended-academy",403)][InlineData("maintenance",503)]
    public async Task Global_boundaries_still_apply(string scenario,int expected)
    {
        using var f=new Fixture();await f.Seed("Operations");await f.Login();if(scenario=="anonymous")f.Actor.DefaultRequestHeaders.Authorization=null;if(scenario=="suspended-user")await f.ChangeIdentity(db=>db.Users.Single(x=>x.Id==f.ActorId).IsActive=false);if(scenario=="suspended-academy")await f.Change(db=>db.Academies.Single(x=>x.Id==f.Academy).IsActive=false);if(scenario=="maintenance")await f.SaveMode(true);var before=await f.Snapshot();using var response=await f.Actor.GetAsync(scenario=="foreign"?Route(f).Replace(f.Academy.ToString(),f.Foreign.ToString()):Route(f));Assert.Equal(expected,(int)response.StatusCode);Assert.Equal(before,await f.Snapshot());
    }
    [Fact]
    public async Task Server_platform_owner_retains_current_cross_tenant_route_scope()
    {
        using var f=new Fixture();await f.Seed("Operations");await f.Login();await f.Change(db=>db.Add(new Teacher{AcademyId=f.Foreign,FirstName="Foreign owner",LastName="Visible"}));using var response=await f.Owner.GetAsync(Route(f).Replace(f.Academy.ToString(),f.Foreign.ToString()));Assert.Equal(HttpStatusCode.OK,response.StatusCode);using var json=JsonDocument.Parse(await response.Content.ReadAsStringAsync());Assert.Single(json.RootElement.GetProperty("teachers").EnumerateArray());Assert.Empty(json.RootElement.GetProperty("courses").EnumerateArray());Assert.Empty(json.RootElement.GetProperty("branches").EnumerateArray());
    }
    [Theory][InlineData("POST")][InlineData("PUT")][InlineData("DELETE")]
    public async Task Options_has_no_mutation_method(string method)
    {
        using var f=new Fixture();await f.Seed("Operations");await f.Login();var before=await f.Snapshot();using var response=await f.Actor.SendAsync(new HttpRequestMessage(new HttpMethod(method),Route(f)));Assert.Equal(HttpStatusCode.MethodNotAllowed,response.StatusCode);Assert.Equal(before,await f.Snapshot());
    }
    private sealed class Fixture : IDisposable
    {
        internal HttpFactory Factory = new(); internal HttpClient Owner { get; } internal HttpClient Actor { get; }
        internal Guid Academy = Guid.NewGuid(), Foreign = Guid.NewGuid(), Student = Guid.NewGuid(), Guardian = Guid.NewGuid(), Teacher = Guid.NewGuid(), OwnerId = Guid.NewGuid(), ActorId = Guid.NewGuid();
        internal string RefreshToken = ""; internal Guid Batch = Guid.NewGuid(), Enrollment = Guid.NewGuid(), Course = Guid.NewGuid(), Branch = Guid.NewGuid();
        internal string Leads => $"/api/academies/{Academy}/leads";
        internal Fixture() { Owner = Factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false }); Actor = Factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false }); }
        internal async Task Seed(string actorRole)
        {
            await using var scope = Factory.Services.CreateAsyncScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            db.AddRange(new Academy { Id = Academy, Name = "Synthetic maintenance academy", EnabledModulesJson = "[\"Core\",\"Sales\",\"Finance\",\"AcademicGovernance\"]" }, new Academy { Id = Foreign, Name = "Foreign academy" });
            db.Add(new Student { Id = Student, AcademyId = Academy, FirstName = "Synthetic", LastName = "Student" }); db.Add(new Guardian { Id = Guardian, AcademyId = Academy, FirstName = "Synthetic", LastName = "Parent" }); db.Add(new Teacher { Id = Teacher, AcademyId = Academy, FirstName = "Synthetic", LastName = "Teacher" }); db.Add(new StudentGuardian { AcademyId = Academy, StudentId = Student, GuardianId = Guardian, CanAccessPortal = true }); db.Add(new ProgramCourse { Id = Course, AcademyId = Academy, Name = "Music foundation", AcademyType = "Music" }); db.Add(new Branch { Id = Branch, AcademyId = Academy, Name = "Central", AddressLine1 = "Synthetic private branch address" }); db.Add(new Batch { Id = Batch, AcademyId = Academy, CourseId = Course, TeacherId = Teacher, BranchId = Branch, Name = "Synthetic lifecycle batch", Capacity = 10, WaitlistCapacity = 10 }); db.Add(new Enrollment { Id = Enrollment, AcademyId = Academy, StudentId = Student, BatchId = Batch, StartDate = new(2026,10,1), Status = "Active" }); await db.SaveChangesAsync();
            var roles = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>(); var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            foreach (var role in new[] { "AcademyAdmin", actorRole }.Distinct()) Assert.True((await roles.CreateAsync(new ApplicationRole { Name = role, IsSystemRole = role != "delegated", PermissionsJson = role == "delegated" ? "[\"batches.manage\"]" : "[]" })).Succeeded);
            foreach (var owner in new[] { true, false })
            {
                var user = new ApplicationUser { Id = owner ? OwnerId : ActorId, UserName = (owner ? "owner" : "actor") + "@maintenance.example.invalid", Email = (owner ? "owner" : "actor") + "@maintenance.example.invalid", EmailConfirmed = true, DisplayName = "Synthetic maintenance actor", AcademyId = Academy, IsPlatformOwner = owner, StudentId = !owner && actorRole == "Student" ? Student : null, GuardianId = !owner && actorRole == "Guardian" ? Guardian : null, TeacherId = !owner && actorRole == "Teacher" ? Teacher : null };
                Assert.True((await users.CreateAsync(user, "Synthetic!39Ab")).Succeeded); Assert.True((await users.AddToRoleAsync(user, owner ? "AcademyAdmin" : actorRole)).Succeeded);
            }
        }
        internal async Task Grant(string kind) { await using var scope=Factory.Services.CreateAsyncScope(); var db=scope.ServiceProvider.GetRequiredService<IdentityDbContext>();db.Add(new AccessGrant { AcademyId=kind=="foreign-grant"?Foreign:Academy, UserId=ActorId, GrantedByUserId=OwnerId, PermissionsJson=kind=="wrong"?"[\"students.manage\"]":"[\"batches.manage\"]", IsPermanent=kind=="permanent", ExpiresAtUtc=DateTimeOffset.UtcNow.AddHours(kind=="expired"?-1:1), RevokedAtUtc=kind=="revoked"?DateTimeOffset.UtcNow:null });await db.SaveChangesAsync(); }
        internal string ReadPath(string role) => role switch { "Guardian" or "Student" => "/api/portal/me", "Teacher" => "/api/teacher/me", "FinanceUser" => $"/api/academies/{Academy}/invoices", "Operations" => $"/api/academies/{Academy}/batches", _ => Leads };
        internal async Task Login() { using var response = await Owner.PostAsJsonAsync("/api/auth/login", new { email = "owner@maintenance.example.invalid", password = "Synthetic!39Ab" }); Assert.Equal(HttpStatusCode.OK, response.StatusCode); using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync()); Owner.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", json.RootElement.GetProperty("accessToken").GetString()); await LoginActor(); }
        internal async Task LoginActor() { Actor.DefaultRequestHeaders.Authorization = null; using var response = await Actor.PostAsJsonAsync("/api/auth/login", new { email = "actor@maintenance.example.invalid", password = "Synthetic!39Ab" }); Assert.Equal(HttpStatusCode.OK, response.StatusCode); using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync()); Actor.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", json.RootElement.GetProperty("accessToken").GetString()); RefreshToken = json.RootElement.GetProperty("refreshToken").GetString()!; }
        internal async Task SaveMode(bool enabled, string? message = null) { using var response = await Owner.PutAsJsonAsync("/api/platform/settings", new { platformName = "Synthetic platform", defaultCurrency = "INR", defaultTrialDays = 30, dataRetentionDays = 2555, maintenanceMode = enabled, statusMessage = message }); Assert.Equal(HttpStatusCode.OK, response.StatusCode); }
        internal async Task Change(Action<AcademyDeskDbContext> action) { await using var scope = Factory.Services.CreateAsyncScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); action(db); await db.SaveChangesAsync(); }
        internal async Task ChangeIdentity(Action<IdentityDbContext> action) { await using var scope = Factory.Services.CreateAsyncScope(); var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>(); action(db); await db.SaveChangesAsync(); }
        internal async Task<string> Snapshot() { await using var scope = Factory.Services.CreateAsyncScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); var identity = scope.ServiceProvider.GetRequiredService<IdentityDbContext>(); return JsonSerializer.Serialize(new { settings = await db.PlatformSettings.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), leads = await db.Leads.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), audits = await db.AuditLogs.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), platformAudits = await db.PlatformAuditEntries.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), enrollments = await db.Enrollments.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), courses = await db.Courses.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), teachers = await db.Teachers.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), branches = await db.Branches.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), students = await db.Students.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), batches = await db.Batches.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), grants = await identity.AccessGrants.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), roles = await identity.Roles.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), users = await identity.Users.AsNoTracking().OrderBy(x => x.Id).ToListAsync() }); }
        public void Dispose() { Owner.Dispose(); Actor.Dispose(); Factory.Dispose(); }
    }
    private sealed class HttpFactory : WebApplicationFactory<Program>
    {
        private readonly string database = $"platform-maintenance-{Guid.NewGuid():N}";
        internal string WebRoot { get; } = Path.Combine(Path.GetTempPath(), "c34-batch-options-" + Guid.NewGuid().ToString("N"));
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            Directory.CreateDirectory(WebRoot); builder.UseWebRoot(WebRoot); builder.UseEnvironment("Testing"); builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?> { ["Database:ApplyMigrationsOnStartup"] = "false", ["Bootstrap:PlatformOwnerEmail"] = "", ["Bootstrap:PlatformOwnerPassword"] = "", ["MediaStorage:Enabled"] = "false", ["Penta:Enabled"] = "false" }));
            builder.ConfigureServices(services => { services.RemoveAll<DbContextOptions<AcademyDeskDbContext>>(); services.RemoveAll<DbContextOptions<IdentityDbContext>>(); services.RemoveAll<IDbContextOptionsConfiguration<AcademyDeskDbContext>>(); services.RemoveAll<IDbContextOptionsConfiguration<IdentityDbContext>>(); services.RemoveAll<AcademyDeskDbContext>(); services.RemoveAll<IdentityDbContext>(); services.AddDbContext<AcademyDeskDbContext>(o => o.UseInMemoryDatabase(database + "-domain")); services.AddDbContext<IdentityDbContext>(o => o.UseInMemoryDatabase(database + "-identity")); });
        }
        protected override void Dispose(bool disposing) { base.Dispose(disposing); if (disposing && Directory.Exists(WebRoot)) { if (Directory.GetParent(WebRoot)?.FullName != Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar) || !Path.GetFileName(WebRoot).StartsWith("c34-batch-options-", StringComparison.Ordinal) || new DirectoryInfo(WebRoot).Attributes.HasFlag(FileAttributes.ReparsePoint)) throw new InvalidOperationException("Refused nonowned cleanup."); Directory.Delete(WebRoot, true); } }
    }
}
