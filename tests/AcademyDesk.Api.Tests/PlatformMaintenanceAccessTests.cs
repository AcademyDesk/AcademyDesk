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

public sealed class PlatformMaintenanceAccessTests
{
    [Theory]
    [InlineData("Owner")]
    [InlineData("AcademyAdmin")]
    [InlineData("Sales")]
    [InlineData("Marketing")]
    [InlineData("FrontDesk")]
    [InlineData("FinanceUser")]
    [InlineData("Operations")]
    [InlineData("Teacher")]
    [InlineData("Guardian")]
    [InlineData("Student")]
    public async Task Existing_authenticated_business_reads_and_profile_writes_are_blocked_without_changes(string role)
    {
        using var f = new Fixture(); await f.Seed(role); await f.Login();
        using var normal = await f.Actor.GetAsync(f.ReadPath(role)); Assert.Equal(HttpStatusCode.OK, normal.StatusCode);
        await f.SaveMode(true, "Synthetic planned maintenance"); var before = await f.Snapshot();
        using var blocked = await f.Actor.GetAsync(f.ReadPath(role)); await AssertMaintenance(blocked, "Synthetic planned maintenance");
        using var write = await f.Actor.PutAsJsonAsync("/api/auth/session/profile", new { displayName = "Must not change", phoneNumber = "+0000000" }); await AssertMaintenance(write, "Synthetic planned maintenance");
        Assert.Equal(before, await f.Snapshot());
    }

    [Fact]
    public async Task Owner_save_enable_and_disable_affect_the_same_existing_actor_bearer()
    {
        using var f = new Fixture(); await f.Seed("AcademyAdmin"); await f.Login(); await f.SaveMode(true);
        var before = await f.Snapshot(); using var denied = await f.Actor.PostAsJsonAsync(f.Leads, new { fullName = "Blocked synthetic lead" }); await AssertMaintenance(denied); Assert.Equal(before, await f.Snapshot());
        await f.SaveMode(false); using var created = await f.Actor.PostAsJsonAsync(f.Leads, new { fullName = "Allowed synthetic lead" }); Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        await using var scope = f.Factory.Services.CreateAsyncScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); Assert.Single(await db.Leads.ToListAsync()); Assert.Single(await db.AuditLogs.Where(x => x.ActorUserId == f.ActorId).ToListAsync());
    }

    [Theory]
    [InlineData("PlatformOwner")]
    [InlineData("CustomPlatformAdministrator")]
    public async Task A_role_label_without_the_server_owner_flag_cannot_bypass_maintenance(string label)
    {
        using var f = new Fixture(); await f.Seed(label); await f.Login(); await f.SaveMode(true); var before = await f.Snapshot();
        using var response = await f.Actor.PutAsJsonAsync("/api/auth/session/profile", new { displayName = "Blocked" }); await AssertMaintenance(response); Assert.Equal(before, await f.Snapshot());
    }

    [Fact]
    public async Task Server_owner_retains_settings_and_business_recovery_without_role_name_elevation()
    {
        using var f = new Fixture(); await f.Seed("Teacher"); await f.Login(); await f.SaveMode(true);
        using var settings = await f.Owner.GetAsync("/api/platform/settings"); Assert.Equal(HttpStatusCode.OK, settings.StatusCode);
        using var read = await f.Owner.GetAsync(f.Leads); Assert.Equal(HttpStatusCode.OK, read.StatusCode);
        using var created = await f.Owner.PostAsJsonAsync(f.Leads, new { fullName = "Owner recovery lead" }); Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        await f.SaveMode(false); using var forbidden = await f.Actor.GetAsync(f.Leads); Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
    }

    [Fact]
    public async Task Revoked_server_owner_flag_takes_effect_with_the_existing_bearer()
    {
        using var f = new Fixture(); await f.Seed("AcademyAdmin"); await f.Login(); await f.SaveMode(true);
        await f.ChangeIdentity(db => db.Users.Single(x => x.Id == f.OwnerId).IsPlatformOwner = false);
        using var response = await f.Owner.GetAsync("/api/platform/settings"); await AssertMaintenance(response);
        await f.ChangeIdentity(db => db.Users.Single(x => x.Id == f.OwnerId).IsPlatformOwner = true); await f.SaveMode(false);
    }

    [Fact]
    public async Task Authentication_session_password_validation_refresh_and_health_remain_available()
    {
        using var f = new Fixture(); await f.Seed("Guardian"); await f.Login(); await f.SaveMode(true);
        await f.LoginActor();
        using var session = await f.Actor.GetAsync("/api/auth/session"); Assert.Equal(HttpStatusCode.OK, session.StatusCode);
        using var refresh = await f.Actor.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = f.RefreshToken }); Assert.Equal(HttpStatusCode.OK, refresh.StatusCode);
        using var health = await f.Actor.GetAsync("/health"); Assert.Equal(HttpStatusCode.OK, health.StatusCode);
        using var password = await f.Actor.PostAsJsonAsync("/api/auth/session/change-password", new { currentPassword = "Incorrect!39Ab", newPassword = "SyntheticNew!39Ab" }); Assert.Equal(HttpStatusCode.BadRequest, password.StatusCode);
        using var family = await f.Actor.PostAsJsonAsync("/api/portal/change-password", new { currentPassword = "Incorrect!39Ab", newPassword = "SyntheticNew!39Ab" }); Assert.Equal(HttpStatusCode.BadRequest, family.StatusCode);
        using var business = await f.Actor.GetAsync("/api/portal/me"); await AssertMaintenance(business);
    }

    [Fact]
    public async Task Profile_image_is_blocked_before_file_or_identity_write()
    {
        using var f = new Fixture(); await f.Seed("AcademyAdmin"); await f.Login(); await f.SaveMode(true); var before = await f.Snapshot();
        using var content = new MultipartFormDataContent(); var bytes = new ByteArrayContent(new byte[] { 137, 80, 78, 71 }); bytes.Headers.ContentType = new MediaTypeHeaderValue("image/png"); content.Add(bytes, "image", "synthetic.png");
        using var response = await f.Actor.PostAsync("/api/auth/session/profile-image", content); await AssertMaintenance(response); Assert.Equal(before, await f.Snapshot()); Assert.Empty(Directory.EnumerateFiles(f.Factory.WebRoot, "*", SearchOption.AllDirectories));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Missing_or_blank_public_message_uses_a_clear_default(string? message)
    {
        using var f = new Fixture(); await f.Seed("AcademyAdmin"); await f.Login(); await f.SaveMode(true, message);
        using var response = await f.Actor.GetAsync(f.Leads); await AssertMaintenance(response);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Newest_legacy_settings_record_controls_access_without_gate_cleanup(bool newest)
    {
        using var f = new Fixture(); await f.Seed("AcademyAdmin"); await f.Login(); await f.Change(db => db.AddRange(new PlatformSettings { MaintenanceMode = !newest, CreatedAtUtc = new DateTime(2000, 1, 1) }, new PlatformSettings { MaintenanceMode = newest, CreatedAtUtc = new DateTime(2001, 1, 1), UpdatedAtUtc = new DateTime(2002, 1, 1) })); var before = await f.Snapshot();
        using var response = await f.Actor.GetAsync(f.Leads); if (newest) await AssertMaintenance(response); else Assert.Equal(HttpStatusCode.OK, response.StatusCode); Assert.Equal(before, await f.Snapshot());
    }

    [Fact]
    public async Task Tied_legacy_settings_choose_the_same_record_for_gate_and_owner_settings()
    {
        using var f = new Fixture(); await f.Seed("AcademyAdmin"); await f.Login(); Guid high = Guid.Parse("ffffffff-ffff-4fff-bfff-ffffffffffff"), low = Guid.Parse("00000000-0000-4000-8000-000000000001"); var stamp = new DateTime(2000, 1, 1);
        await f.Change(db => db.AddRange(new PlatformSettings { Id = low, CreatedAtUtc = stamp, MaintenanceMode = false }, new PlatformSettings { Id = high, CreatedAtUtc = stamp, MaintenanceMode = true })); var before = await f.Snapshot();
        using var response = await f.Actor.GetAsync(f.Leads); await AssertMaintenance(response); Assert.Equal(before, await f.Snapshot());
        using var settings = await f.Owner.GetAsync("/api/platform/settings"); Assert.Equal(HttpStatusCode.OK, settings.StatusCode); using var json = JsonDocument.Parse(await settings.Content.ReadAsStringAsync()); Assert.Equal(high, json.RootElement.GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task Missing_settings_are_not_created_by_a_normal_read()
    {
        using var f = new Fixture(); await f.Seed("AcademyAdmin"); await f.Login(); var before = await f.Snapshot(); using var response = await f.Actor.GetAsync(f.Leads); Assert.Equal(HttpStatusCode.OK, response.StatusCode); Assert.Equal(before, await f.Snapshot());
    }

    [Fact]
    public async Task Disabling_maintenance_restores_existing_tenant_and_permission_denials()
    {
        using var f = new Fixture(); await f.Seed("Teacher"); await f.Login(); await f.SaveMode(true);
        using var blocked = await f.Actor.GetAsync(f.Leads); await AssertMaintenance(blocked);
        await f.SaveMode(false); var before = await f.Snapshot(); using var own = await f.Actor.GetAsync(f.Leads); Assert.Equal(HttpStatusCode.Forbidden, own.StatusCode);
        using var foreign = await f.Actor.GetAsync(f.Leads.Replace(f.Academy.ToString(), f.Foreign.ToString())); Assert.Equal(HttpStatusCode.Forbidden, foreign.StatusCode); Assert.Equal(before, await f.Snapshot());
    }

    [Fact]
    public async Task Anonymous_requests_still_require_authentication()
    {
        using var f = new Fixture(); await f.Seed("AcademyAdmin"); await f.Login(); await f.SaveMode(true); f.Actor.DefaultRequestHeaders.Authorization = null;
        using var response = await f.Actor.GetAsync(f.Leads); Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static async Task AssertMaintenance(HttpResponseMessage response, string? expected = null)
    {
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode); Assert.Contains("no-store", response.Headers.CacheControl?.ToString() ?? "");
        using var j = JsonDocument.Parse(await response.Content.ReadAsStringAsync()); Assert.Equal("platform_maintenance", j.RootElement.GetProperty("code").GetString()); Assert.True(j.RootElement.GetProperty("maintenanceMode").GetBoolean());
        var message = j.RootElement.GetProperty("message").GetString(); if (expected is null) Assert.Contains("maintenance", message!, StringComparison.OrdinalIgnoreCase); else Assert.Equal(expected, message);
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
        internal string WebRoot { get; } = Path.Combine(Path.GetTempPath(), "c30-maintenance-" + Guid.NewGuid().ToString("N"));
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            Directory.CreateDirectory(WebRoot); builder.UseWebRoot(WebRoot); builder.UseEnvironment("Testing"); builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?> { ["Database:ApplyMigrationsOnStartup"] = "false", ["Bootstrap:PlatformOwnerEmail"] = "", ["Bootstrap:PlatformOwnerPassword"] = "", ["MediaStorage:Enabled"] = "false", ["Penta:Enabled"] = "false" }));
            builder.ConfigureServices(services => { services.RemoveAll<DbContextOptions<AcademyDeskDbContext>>(); services.RemoveAll<DbContextOptions<IdentityDbContext>>(); services.RemoveAll<IDbContextOptionsConfiguration<AcademyDeskDbContext>>(); services.RemoveAll<IDbContextOptionsConfiguration<IdentityDbContext>>(); services.RemoveAll<AcademyDeskDbContext>(); services.RemoveAll<IdentityDbContext>(); services.AddDbContext<AcademyDeskDbContext>(o => o.UseInMemoryDatabase(database + "-domain")); services.AddDbContext<IdentityDbContext>(o => o.UseInMemoryDatabase(database + "-identity")); });
        }
        protected override void Dispose(bool disposing) { base.Dispose(disposing); if (disposing && Directory.Exists(WebRoot)) { if (Directory.GetParent(WebRoot)?.FullName != Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar) || !Path.GetFileName(WebRoot).StartsWith("c30-maintenance-", StringComparison.Ordinal) || new DirectoryInfo(WebRoot).Attributes.HasFlag(FileAttributes.ReparsePoint)) throw new InvalidOperationException("Refused nonowned cleanup."); Directory.Delete(WebRoot, true); } }
    }
}
