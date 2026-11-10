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

public sealed class MaintenanceAvailabilityTests
{
    [Fact]
    public void Availability_route_is_discovered_by_the_real_application()
    {
        using var f = new Fixture();
        var actions = f.Factory.Services.GetRequiredService<Microsoft.AspNetCore.Mvc.Infrastructure.IActionDescriptorCollectionProvider>().ActionDescriptors.Items;
        Assert.True(typeof(AcademyDesk.Api.Controllers.AuthSessionController).GetMethod("Availability") is not null,
            typeof(AcademyDesk.Api.Controllers.AuthSessionController).Assembly.Location + "\n" + string.Join(",", typeof(AcademyDesk.Api.Controllers.AuthSessionController).GetMethods().Select(x => x.Name)));
        Assert.Contains("api/auth/session/availability", string.Join("\n", actions.Where(x => x.DisplayName?.Contains("AuthSessionController") == true).Select(x => x.AttributeRouteInfo?.Template)));
    }
    private const string Route = "/api/auth/session/availability";
    [Theory]
    [InlineData("AcademyAdmin")]
    [InlineData("Teacher")]
    [InlineData("Guardian")]
    [InlineData("Student")]
    [InlineData("PlatformOwner")]
    public async Task Availability_is_an_authenticated_read_during_maintenance_for_each_role(string role)
    {
        using var f = new Fixture(); await f.Seed(role); await f.Login(); await f.SaveMode(true, "  Synthetic status <script>text only</script>  "); var before = await f.Snapshot();
        using var response = await f.Actor.GetAsync(Route); await AssertStatus(response, true, true, "Synthetic status <script>text only</script>");
        Assert.Equal(before, await f.Snapshot()); using var blocked = await f.Actor.GetAsync(f.ReadPath(role)); Assert.Equal(HttpStatusCode.ServiceUnavailable, blocked.StatusCode);
    }
    [Fact]
    public async Task Anonymous_is_unauthorized_and_status_does_not_create_settings()
    {
        using var f = new Fixture(); await f.Seed("AcademyAdmin"); var before = await f.Snapshot(); using var response = await f.Actor.GetAsync(Route); Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode); Assert.Equal(before, await f.Snapshot());
    }
    [Fact]
    public async Task Missing_settings_are_available_without_writes()
    {
        using var f = new Fixture(); await f.Seed("Teacher"); await f.Login(); var before = await f.Snapshot(); using var response = await f.Actor.GetAsync(Route); await AssertStatus(response, false, false, null); Assert.Equal(before, await f.Snapshot());
    }
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Blank_status_uses_the_same_safe_default(string? message)
    {
        using var f = new Fixture(); await f.Seed("Student"); await f.Login(); await f.SaveMode(true, message); using var response = await f.Actor.GetAsync(Route); await AssertStatus(response, true, true, "Academy Desk is temporarily unavailable for maintenance. Please try again later.");
    }
    [Fact]
    public async Task Existing_bearer_observes_enable_disable_and_current_server_owner_flag()
    {
        using var f = new Fixture(); await f.Seed("Teacher"); await f.Login(); await f.SaveMode(true, "Maintenance");
        using var owner = await f.Owner.GetAsync(Route); await AssertStatus(owner, true, false, "Maintenance");
        await f.ChangeIdentity(db => db.Users.Single(x => x.Id == f.OwnerId).IsPlatformOwner = false);
        using var demoted = await f.Owner.GetAsync(Route); await AssertStatus(demoted, true, true, "Maintenance");
        await f.ChangeIdentity(db => db.Users.Single(x => x.Id == f.OwnerId).IsPlatformOwner = true); await f.SaveMode(false, "Old message");
        using var off = await f.Actor.GetAsync(Route); await AssertStatus(off, false, false, null);
    }
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Newest_settings_record_and_tie_breaker_are_shared_with_access_policy(bool enabled)
    {
        using var f = new Fixture(); await f.Seed("AcademyAdmin"); await f.Login();
        await f.Change(db => db.AddRange(new PlatformSettings { Id = Guid.Parse("11111111-1111-4111-8111-111111111111"), CreatedAtUtc = new DateTime(2001,1,1), MaintenanceMode = !enabled }, new PlatformSettings { Id = Guid.Parse("22222222-2222-4222-8222-222222222222"), CreatedAtUtc = new DateTime(2000,1,1), UpdatedAtUtc = new DateTime(2001,1,1), MaintenanceMode = enabled, StatusMessage = "Canonical" }));
        var before = await f.Snapshot(); using var status = await f.Actor.GetAsync(Route); await AssertStatus(status, enabled, enabled, enabled ? "Canonical" : null); Assert.Equal(before, await f.Snapshot());
        using var read = await f.Actor.GetAsync(f.Leads); Assert.Equal(enabled ? HttpStatusCode.ServiceUnavailable : HttpStatusCode.OK, read.StatusCode);
    }
    [Theory]
    [InlineData("availability/extra")]
    [InlineData("profile")]
    public async Task Availability_does_not_grant_mutation_or_prefix_recovery(string path)
    {
        using var f = new Fixture(); await f.Seed("AcademyAdmin"); await f.Login(); await f.SaveMode(true); var before = await f.Snapshot(); using var response = await f.Actor.PutAsJsonAsync("/api/auth/session/" + path, new { displayName = "Blocked" }); Assert.False(response.IsSuccessStatusCode); Assert.Equal(before, await f.Snapshot());
    }
    private static async Task AssertStatus(HttpResponseMessage response, bool enabled, bool restricted, string? message)
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode); Assert.Contains("no-store", response.Headers.CacheControl?.ToString() ?? "");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync()); var value = json.RootElement; Assert.Equal(new[] { "maintenanceMode", "message", "restricted" }, value.EnumerateObject().Select(x => x.Name).OrderBy(x => x).ToArray()); Assert.Equal(enabled, value.GetProperty("maintenanceMode").GetBoolean()); Assert.Equal(restricted, value.GetProperty("restricted").GetBoolean()); Assert.Equal(message, value.GetProperty("message").GetString());
    }
    private sealed class Fixture : IDisposable
    {
        internal HttpFactory Factory = new(); internal HttpClient Owner { get; } internal HttpClient Actor { get; }
        internal Guid Academy = Guid.NewGuid(), Foreign = Guid.NewGuid(), Student = Guid.NewGuid(), Guardian = Guid.NewGuid(), Teacher = Guid.NewGuid(), OwnerId = Guid.NewGuid(), ActorId = Guid.NewGuid();
        internal string RefreshToken = "";
        internal string Leads => $"/api/academies/{Academy}/leads";
        internal Fixture() { Owner = Factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false }); Actor = Factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false }); }
        internal async Task Seed(string actorRole)
        {
            await using var scope = Factory.Services.CreateAsyncScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            db.AddRange(new Academy { Id = Academy, Name = "Synthetic maintenance academy", EnabledModulesJson = "[\"Core\",\"Sales\",\"Finance\",\"AcademicGovernance\"]" }, new Academy { Id = Foreign, Name = "Foreign academy" });
            db.Add(new Student { Id = Student, AcademyId = Academy, FirstName = "Synthetic", LastName = "Student" }); db.Add(new Guardian { Id = Guardian, AcademyId = Academy, FirstName = "Synthetic", LastName = "Parent" }); db.Add(new Teacher { Id = Teacher, AcademyId = Academy, FirstName = "Synthetic", LastName = "Teacher" }); db.Add(new StudentGuardian { AcademyId = Academy, StudentId = Student, GuardianId = Guardian, CanAccessPortal = true }); await db.SaveChangesAsync();
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
        internal async Task<string> Snapshot() { await using var scope = Factory.Services.CreateAsyncScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); var identity = scope.ServiceProvider.GetRequiredService<IdentityDbContext>(); return JsonSerializer.Serialize(new { settings = await db.PlatformSettings.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), leads = await db.Leads.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), audits = await db.AuditLogs.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), platformAudits = await db.PlatformAuditEntries.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), users = await identity.Users.AsNoTracking().OrderBy(x => x.Id).ToListAsync() }); }
        public void Dispose() { Owner.Dispose(); Actor.Dispose(); Factory.Dispose(); }
    }
    private sealed class HttpFactory : WebApplicationFactory<Program>
    {
        private readonly string database = $"platform-maintenance-{Guid.NewGuid():N}";
        internal string WebRoot { get; } = Path.Combine(Path.GetTempPath(), "c31-availability-" + Guid.NewGuid().ToString("N"));
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            Directory.CreateDirectory(WebRoot); builder.UseWebRoot(WebRoot); builder.UseEnvironment("Testing"); builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?> { ["Database:ApplyMigrationsOnStartup"] = "false", ["Bootstrap:PlatformOwnerEmail"] = "", ["Bootstrap:PlatformOwnerPassword"] = "", ["MediaStorage:Enabled"] = "false", ["Penta:Enabled"] = "false" }));
            builder.ConfigureServices(services => { services.RemoveAll<DbContextOptions<AcademyDeskDbContext>>(); services.RemoveAll<DbContextOptions<IdentityDbContext>>(); services.RemoveAll<IDbContextOptionsConfiguration<AcademyDeskDbContext>>(); services.RemoveAll<IDbContextOptionsConfiguration<IdentityDbContext>>(); services.RemoveAll<AcademyDeskDbContext>(); services.RemoveAll<IdentityDbContext>(); services.AddDbContext<AcademyDeskDbContext>(o => o.UseInMemoryDatabase(database + "-domain")); services.AddDbContext<IdentityDbContext>(o => o.UseInMemoryDatabase(database + "-identity")); });
        }
        protected override void Dispose(bool disposing) { base.Dispose(disposing); if (disposing && Directory.Exists(WebRoot)) { if (Directory.GetParent(WebRoot)?.FullName != Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar) || !Path.GetFileName(WebRoot).StartsWith("c31-availability-", StringComparison.Ordinal) || new DirectoryInfo(WebRoot).Attributes.HasFlag(FileAttributes.ReparsePoint)) throw new InvalidOperationException("Refused nonowned cleanup."); Directory.Delete(WebRoot, true); } }
    }
}
