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

public sealed class EnrollmentLifecycleAccessTests
{
    [Theory]
    [InlineData("Active")]
    [InlineData("Waitlisted")]
    [InlineData("Paused")]
    [InlineData("Completed")]
    [InlineData("Withdrawn")]
    [InlineData("Cancelled")]
    public async Task Each_offered_status_has_an_exact_receipt_and_persisted_lifecycle_reason(string status)
    {
        using var f = new Fixture(); await f.Seed("AcademyAdmin"); await f.Login();
        var end = status == "Active" ? (DateOnly?)null : new DateOnly(2026,11,9);
        using var response = await f.Actor.PutAsJsonAsync(PathFor(f), new { status, endDate = end, lifecycleReason = status == "Active" ? null : "  Synthetic reviewed reason  " }); Assert.Equal(HttpStatusCode.OK,response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync()); var row = json.RootElement; Assert.Equal(f.Enrollment,row.GetProperty("id").GetGuid()); Assert.Equal(f.Student,row.GetProperty("studentId").GetGuid()); Assert.Equal(f.Batch,row.GetProperty("batchId").GetGuid()); Assert.Equal("2026-10-01",row.GetProperty("startDate").GetString()); Assert.Equal(status,row.GetProperty("status").GetString()); Assert.Equal(end?.ToString("yyyy-MM-dd"),row.GetProperty("endDate").GetString());
        await using var scope = f.Factory.Services.CreateAsyncScope(); var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); var saved=await db.Enrollments.AsNoTracking().SingleAsync(); Assert.Equal(status,saved.Status); Assert.Equal(end,saved.EndDate); Assert.Equal(status == "Active" ? null : "Synthetic reviewed reason", saved.LifecycleReason); Assert.Single(await db.AuditLogs.Where(x=>x.ActorUserId==f.ActorId).ToListAsync());
    }
    [Theory]
    [InlineData("Waitlisted",null)]
    [InlineData("Paused","")]
    [InlineData("Completed","   ")]
    [InlineData("Withdrawn",null)]
    [InlineData("Cancelled",null)]
    public async Task Missing_or_blank_reason_cannot_write_or_audit(string status,string? reason)
    {
        using var f = new Fixture(); await f.Seed("AcademyAdmin"); await f.Login(); var before=await f.Snapshot(); using var response=await f.Actor.PutAsJsonAsync(PathFor(f),new {status,endDate=(string?)null,lifecycleReason=reason}); Assert.Equal(HttpStatusCode.BadRequest,response.StatusCode); Assert.Contains("lifecycle reason",await response.Content.ReadAsStringAsync()); Assert.Equal(before,await f.Snapshot());
    }
    [Theory]
    [InlineData("Teacher")]
    [InlineData("FinanceUser")]
    public async Task Unrelated_roles_cannot_update_an_enrollment(string role)
    {
        using var f = new Fixture(); await f.Seed(role); await f.Login(); var before=await f.Snapshot(); using var response=await f.Actor.PutAsJsonAsync(PathFor(f),new {status="Paused",lifecycleReason="Synthetic reason"}); Assert.Equal(HttpStatusCode.Forbidden,response.StatusCode); Assert.Equal(before,await f.Snapshot());
    }
    [Fact]
    public async Task Foreign_academy_route_cannot_mutate_a_local_record()
    {
        using var f = new Fixture(); await f.Seed("AcademyAdmin"); await f.Login(); var before=await f.Snapshot(); using var response=await f.Actor.PutAsJsonAsync(PathFor(f).Replace(f.Academy.ToString(),f.Foreign.ToString()),new {status="Paused",lifecycleReason="Synthetic reason"}); Assert.Equal(HttpStatusCode.Forbidden,response.StatusCode); Assert.Equal(before,await f.Snapshot());
    }
    [Fact]
    public async Task Activation_rechecks_capacity_and_preserves_the_waitlisted_row_on_conflict()
    {
        using var f = new Fixture(); await f.Seed("AcademyAdmin"); await f.Login(); await f.Change(db=>{db.Batches.Single().Capacity=0;db.Enrollments.Single().Status="Waitlisted";}); var before=await f.Snapshot(); using var response=await f.Actor.PutAsJsonAsync(PathFor(f),new {status="Active"}); Assert.Equal(HttpStatusCode.Conflict,response.StatusCode); Assert.Equal(before,await f.Snapshot());
    }
    [Theory]
    [InlineData("Active")]
    [InlineData("Waitlisted")]
    public async Task Creation_preserves_both_existing_initial_states_and_receipt_shape(string status)
    {
        using var f = new Fixture(); await f.Seed("AcademyAdmin"); await f.Login(); await f.Change(db=>db.Enrollments.RemoveRange(db.Enrollments));
        using var response=await f.Actor.PostAsJsonAsync($"/api/academies/{f.Academy}/enrollments",new {studentId=f.Student,batchId=f.Batch,startDate="2026-11-10",status});Assert.Equal(HttpStatusCode.Created,response.StatusCode);using var json=JsonDocument.Parse(await response.Content.ReadAsStringAsync());Assert.Equal(status,json.RootElement.GetProperty("status").GetString()); Assert.Equal("2026-11-10",json.RootElement.GetProperty("startDate").GetString());
    }
    private static string PathFor(Fixture f)=>$"/api/academies/{f.Academy}/enrollments/{f.Enrollment}";
    [Fact]
    public async Task Anonymous_update_cannot_write_or_audit()
    {
        using var f = new Fixture(); await f.Seed("AcademyAdmin"); var before=await f.Snapshot(); using var response=await f.Actor.PutAsJsonAsync(PathFor(f),new {status="Paused",lifecycleReason="Synthetic"});Assert.Equal(HttpStatusCode.Unauthorized,response.StatusCode);Assert.Equal(before,await f.Snapshot());
    }
    [Fact]
    public async Task Suspended_actor_is_denied_with_existing_bearer()
    {
        using var f = new Fixture();await f.Seed("AcademyAdmin");await f.Login();await f.ChangeIdentity(db=>db.Users.Single(x=>x.Id==f.ActorId).IsActive=false);var before=await f.Snapshot();using var response=await f.Actor.PutAsJsonAsync(PathFor(f),new {status="Paused",lifecycleReason="Synthetic"});Assert.Equal(HttpStatusCode.Forbidden,response.StatusCode);Assert.Equal(before,await f.Snapshot());
    }
    [Fact]
    public async Task Missing_record_is_not_recreated_by_status_update()
    {
        using var f = new Fixture();await f.Seed("AcademyAdmin");await f.Login();var before=await f.Snapshot();using var response=await f.Actor.PutAsJsonAsync(PathFor(f).Replace(f.Enrollment.ToString(),Guid.NewGuid().ToString()),new {status="Paused",lifecycleReason="Synthetic"});Assert.Equal(HttpStatusCode.NotFound,response.StatusCode);Assert.Equal(before,await f.Snapshot());
    }
    private sealed class Fixture : IDisposable
    {
        internal HttpFactory Factory = new(); internal HttpClient Owner { get; } internal HttpClient Actor { get; }
        internal Guid Academy = Guid.NewGuid(), Foreign = Guid.NewGuid(), Student = Guid.NewGuid(), Guardian = Guid.NewGuid(), Teacher = Guid.NewGuid(), OwnerId = Guid.NewGuid(), ActorId = Guid.NewGuid();
        internal string RefreshToken = ""; internal Guid Batch = Guid.NewGuid(), Enrollment = Guid.NewGuid();
        internal string Leads => $"/api/academies/{Academy}/leads";
        internal Fixture() { Owner = Factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false }); Actor = Factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false }); }
        internal async Task Seed(string actorRole)
        {
            await using var scope = Factory.Services.CreateAsyncScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            db.AddRange(new Academy { Id = Academy, Name = "Synthetic maintenance academy", EnabledModulesJson = "[\"Core\",\"Sales\",\"Finance\",\"AcademicGovernance\"]" }, new Academy { Id = Foreign, Name = "Foreign academy" });
            db.Add(new Student { Id = Student, AcademyId = Academy, FirstName = "Synthetic", LastName = "Student" }); db.Add(new Guardian { Id = Guardian, AcademyId = Academy, FirstName = "Synthetic", LastName = "Parent" }); db.Add(new Teacher { Id = Teacher, AcademyId = Academy, FirstName = "Synthetic", LastName = "Teacher" }); db.Add(new StudentGuardian { AcademyId = Academy, StudentId = Student, GuardianId = Guardian, CanAccessPortal = true }); db.Add(new Batch { Id = Batch, AcademyId = Academy, CourseId = Guid.NewGuid(), Name = "Synthetic lifecycle batch", Capacity = 10, WaitlistCapacity = 10 }); db.Add(new Enrollment { Id = Enrollment, AcademyId = Academy, StudentId = Student, BatchId = Batch, StartDate = new(2026,10,1), Status = "Active" }); await db.SaveChangesAsync();
            var roles = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>(); var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            foreach (var role in new[] { "AcademyAdmin", actorRole }.Distinct()) Assert.True((await roles.CreateAsync(new ApplicationRole { Name = role, IsSystemRole = true })).Succeeded);
            foreach (var owner in new[] { true, false })
            {
                var user = new ApplicationUser { Id = owner ? OwnerId : ActorId, UserName = (owner ? "owner" : "actor") + "@maintenance.example.invalid", Email = (owner ? "owner" : "actor") + "@maintenance.example.invalid", EmailConfirmed = true, DisplayName = "Synthetic maintenance actor", AcademyId = Academy, IsPlatformOwner = owner, StudentId = !owner && actorRole == "Student" ? Student : null, GuardianId = !owner && actorRole == "Guardian" ? Guardian : null, TeacherId = !owner && actorRole == "Teacher" ? Teacher : null };
                Assert.True((await users.CreateAsync(user, "Synthetic!39Ab")).Succeeded); Assert.True((await users.AddToRoleAsync(user, owner ? "AcademyAdmin" : actorRole)).Succeeded);
            }
        }
        internal string ReadPath(string role) => role switch { "Guardian" or "Student" => "/api/portal/me", "Teacher" => "/api/teacher/me", "FinanceUser" => $"/api/academies/{Academy}/invoices", "Operations" => $"/api/academies/{Academy}/batches", _ => Leads };
        internal async Task Login() { using var response = await Owner.PostAsJsonAsync("/api/auth/login", new { email = "owner@maintenance.example.invalid", password = "Synthetic!39Ab" }); Assert.Equal(HttpStatusCode.OK, response.StatusCode); using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync()); Owner.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", json.RootElement.GetProperty("accessToken").GetString()); await LoginActor(); }
        internal async Task LoginActor() { Actor.DefaultRequestHeaders.Authorization = null; using var response = await Actor.PostAsJsonAsync("/api/auth/login", new { email = "actor@maintenance.example.invalid", password = "Synthetic!39Ab" }); Assert.Equal(HttpStatusCode.OK, response.StatusCode); using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync()); Actor.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", json.RootElement.GetProperty("accessToken").GetString()); RefreshToken = json.RootElement.GetProperty("refreshToken").GetString()!; }
        internal async Task SaveMode(bool enabled, string? message = null) { using var response = await Owner.PutAsJsonAsync("/api/platform/settings", new { platformName = "Synthetic platform", defaultCurrency = "INR", defaultTrialDays = 30, dataRetentionDays = 2555, maintenanceMode = enabled, statusMessage = message }); Assert.Equal(HttpStatusCode.OK, response.StatusCode); }
        internal async Task Change(Action<AcademyDeskDbContext> action) { await using var scope = Factory.Services.CreateAsyncScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); action(db); await db.SaveChangesAsync(); }
        internal async Task ChangeIdentity(Action<IdentityDbContext> action) { await using var scope = Factory.Services.CreateAsyncScope(); var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>(); action(db); await db.SaveChangesAsync(); }
        internal async Task<string> Snapshot() { await using var scope = Factory.Services.CreateAsyncScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); var identity = scope.ServiceProvider.GetRequiredService<IdentityDbContext>(); return JsonSerializer.Serialize(new { settings = await db.PlatformSettings.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), leads = await db.Leads.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), audits = await db.AuditLogs.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), platformAudits = await db.PlatformAuditEntries.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), enrollments = await db.Enrollments.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), users = await identity.Users.AsNoTracking().OrderBy(x => x.Id).ToListAsync() }); }
        public void Dispose() { Owner.Dispose(); Actor.Dispose(); Factory.Dispose(); }
    }
    private sealed class HttpFactory : WebApplicationFactory<Program>
    {
        private readonly string database = $"platform-maintenance-{Guid.NewGuid():N}";
        internal string WebRoot { get; } = Path.Combine(Path.GetTempPath(), "c32-enrollment-" + Guid.NewGuid().ToString("N"));
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            Directory.CreateDirectory(WebRoot); builder.UseWebRoot(WebRoot); builder.UseEnvironment("Testing"); builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?> { ["Database:ApplyMigrationsOnStartup"] = "false", ["Bootstrap:PlatformOwnerEmail"] = "", ["Bootstrap:PlatformOwnerPassword"] = "", ["MediaStorage:Enabled"] = "false", ["Penta:Enabled"] = "false" }));
            builder.ConfigureServices(services => { services.RemoveAll<DbContextOptions<AcademyDeskDbContext>>(); services.RemoveAll<DbContextOptions<IdentityDbContext>>(); services.RemoveAll<IDbContextOptionsConfiguration<AcademyDeskDbContext>>(); services.RemoveAll<IDbContextOptionsConfiguration<IdentityDbContext>>(); services.RemoveAll<AcademyDeskDbContext>(); services.RemoveAll<IdentityDbContext>(); services.AddDbContext<AcademyDeskDbContext>(o => o.UseInMemoryDatabase(database + "-domain")); services.AddDbContext<IdentityDbContext>(o => o.UseInMemoryDatabase(database + "-identity")); });
        }
        protected override void Dispose(bool disposing) { base.Dispose(disposing); if (disposing && Directory.Exists(WebRoot)) { if (Directory.GetParent(WebRoot)?.FullName != Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar) || !Path.GetFileName(WebRoot).StartsWith("c32-enrollment-", StringComparison.Ordinal) || new DirectoryInfo(WebRoot).Attributes.HasFlag(FileAttributes.ReparsePoint)) throw new InvalidOperationException("Refused nonowned cleanup."); Directory.Delete(WebRoot, true); } }
    }
}
