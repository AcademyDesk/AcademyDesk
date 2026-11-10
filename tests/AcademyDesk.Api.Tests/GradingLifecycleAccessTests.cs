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

public sealed class GradingLifecycleAccessTests
{
    [Theory]
    [InlineData("Owner")]
    [InlineData("AcademyAdmin")]
    [InlineData("PlatformOwner")]
    [InlineData("role")]
    [InlineData("permanent")]
    [InlineData("timed")]
    public async Task Authorized_creation_lookup_and_lifecycle_share_academic_authority(string actor)
    {
        using var f = new Fixture(); await f.Seed(actor); await f.Login();
        using var created = await f.Client.PostAsJsonAsync(f.Governance + "/grading-schemes", new { name = "New synthetic policy", passingPercent = 60, bandsJson = "[]" });
        Assert.Equal(HttpStatusCode.OK, created.StatusCode);
        var all = await f.Read(f.Governance + "/grading-schemes"); var target = all.EnumerateArray().Single(x => x.GetProperty("name").GetString() == "New synthetic policy").GetProperty("id").GetGuid();
        var active = await f.Read(f.Lifecycle + "/active"); Assert.Equal(2, active.GetArrayLength()); Assert.DoesNotContain(active.EnumerateArray(), x => x.GetProperty("academyId").GetGuid() == f.Foreign);
        using var off = await f.Client.PatchAsJsonAsync(f.Lifecycle + $"/{target}/status", new { isActive = false }); Assert.Equal(HttpStatusCode.OK, off.StatusCode);
        active = await f.Read(f.Lifecycle + "/active"); Assert.DoesNotContain(active.EnumerateArray(), x => x.GetProperty("id").GetGuid() == target);
        using var on = await f.Client.PatchAsJsonAsync(f.Lifecycle + $"/{target}/status", new { isActive = true }); Assert.Equal(HttpStatusCode.OK, on.StatusCode);
        await using var scope = f.Factory.Services.CreateAsyncScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
        var saved = await db.GradingSchemes.SingleAsync(x => x.Id == target); Assert.True(saved.IsActive); Assert.Equal(60, saved.PassingPercent); Assert.Equal("[]", saved.BandsJson);
        Assert.Equal(3, await db.AuditLogs.CountAsync(x => x.ActorUserId == f.Actor));
    }

    [Theory]
    [InlineData("Teacher")]
    [InlineData("FinanceUser")]
    [InlineData("Operations")]
    [InlineData("Sales")]
    [InlineData("none")]
    [InlineData("expired")]
    [InlineData("revoked")]
    [InlineData("wrong")]
    [InlineData("foreign-grant")]
    public async Task Absent_expired_revoked_and_unrelated_authority_cannot_read_or_write(string actor)
    {
        using var f = new Fixture(); await f.Seed(actor); await f.Login(); var before = await f.Snapshot();
        using var active = await f.Client.GetAsync(f.Lifecycle + "/active"); Assert.Equal(HttpStatusCode.Forbidden, active.StatusCode);
        using var status = await f.Client.PatchAsJsonAsync(f.Lifecycle + $"/{f.Scheme}/status", new { isActive = false }); Assert.Equal(HttpStatusCode.Forbidden, status.StatusCode);
        using var create = await f.Client.PostAsJsonAsync(f.Governance + "/grading-schemes", new { name = "Forbidden", passingPercent = 50 }); Assert.Equal(HttpStatusCode.Forbidden, create.StatusCode);
        Assert.Equal(before, await f.Snapshot());
    }

    [Theory]
    [InlineData("role")]
    [InlineData("AcademyAdmin")]
    public async Task Lifecycle_requires_the_same_subscription_as_creation(string actor)
    {
        using var f = new Fixture(); await f.Seed(actor, module: false); await f.Login(); var before = await f.Snapshot();
        using var active = await f.Client.GetAsync(f.Lifecycle + "/active"); Assert.Equal(HttpStatusCode.Forbidden, active.StatusCode);
        using var status = await f.Client.PatchAsJsonAsync(f.Lifecycle + $"/{f.Scheme}/status", new { isActive = false }); Assert.Equal(HttpStatusCode.Forbidden, status.StatusCode);
        Assert.Equal(before, await f.Snapshot());
    }

    [Theory]
    [InlineData("role")]
    [InlineData("AcademyAdmin")]
    public async Task Suspended_academy_cannot_use_lifecycle(string actor)
    {
        using var f = new Fixture(); await f.Seed(actor, active: false); await f.Login(); var before = await f.Snapshot();
        using var get = await f.Client.GetAsync(f.Lifecycle + "/active"); Assert.Equal(HttpStatusCode.Forbidden, get.StatusCode);
        using var patch = await f.Client.PatchAsJsonAsync(f.Lifecycle + $"/{f.Scheme}/status", new { isActive = false }); Assert.Equal(HttpStatusCode.Forbidden, patch.StatusCode);
        Assert.Equal(before, await f.Snapshot());
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"isActive\":null}")]
    [InlineData("{\"isActive\":\"false\"}")]
    [InlineData("{\"isActive\":0}")]
    public async Task Missing_null_and_malformed_state_never_deactivate_a_scheme(string json)
    {
        using var f = new Fixture(); await f.Seed("AcademyAdmin"); await f.Login(); var before = await f.Snapshot();
        using var response = await f.Client.PatchAsync(f.Lifecycle + $"/{f.Scheme}/status", new StringContent(json, System.Text.Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode); Assert.Equal(before, await f.Snapshot());
    }

    [Theory]
    [InlineData("role")]
    [InlineData("AcademyAdmin")]
    public async Task Foreign_route_and_foreign_record_are_scoped(string actor)
    {
        using var f = new Fixture(); await f.Seed(actor); await f.Login(); var before = await f.Snapshot();
        using var route = await f.Client.GetAsync($"/api/academies/{f.Foreign}/grading-schemes/active"); Assert.Equal(HttpStatusCode.Forbidden, route.StatusCode);
        using var foreign = await f.Client.PatchAsJsonAsync(f.Lifecycle + $"/{f.ForeignScheme}/status", new { isActive = false }); Assert.Equal(HttpStatusCode.NotFound, foreign.StatusCode);
        using var missing = await f.Client.PatchAsJsonAsync(f.Lifecycle + $"/{Guid.NewGuid()}/status", new { isActive = false }); Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        Assert.Equal(before, await f.Snapshot());
    }

    [Fact]
    public async Task Anonymous_requests_cannot_read_or_write()
    {
        using var f = new Fixture(); await f.Seed("role"); var before = await f.Snapshot();
        using var get = await f.Client.GetAsync(f.Lifecycle + "/active"); Assert.Equal(HttpStatusCode.Unauthorized, get.StatusCode);
        using var patch = await f.Client.PatchAsJsonAsync(f.Lifecycle + $"/{f.Scheme}/status", new { isActive = false }); Assert.Equal(HttpStatusCode.Unauthorized, patch.StatusCode); Assert.Equal(before, await f.Snapshot());
    }

    [Fact]
    public async Task Revoking_a_timed_grant_applies_to_the_next_request()
    {
        using var f = new Fixture(); await f.Seed("timed"); await f.Login(); await f.Read(f.Lifecycle + "/active");
        await using (var scope = f.Factory.Services.CreateAsyncScope()) { var identity = scope.ServiceProvider.GetRequiredService<IdentityDbContext>(); (await identity.AccessGrants.SingleAsync()).RevokedAtUtc = DateTimeOffset.UtcNow; await identity.SaveChangesAsync(); }
        var before = await f.Snapshot(); using var patch = await f.Client.PatchAsJsonAsync(f.Lifecycle + $"/{f.Scheme}/status", new { isActive = false }); Assert.Equal(HttpStatusCode.Forbidden, patch.StatusCode); Assert.Equal(before, await f.Snapshot());
    }

    [Fact]
    public async Task Academic_permission_does_not_grant_teacher_management()
    {
        using var f = new Fixture(); await f.Seed("role"); await f.Login(); var before = await f.Snapshot();
        using var response = await f.Client.GetAsync($"/api/academies/{f.Academy}/teachers"); Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode); Assert.Equal(before, await f.Snapshot());
    }

    private sealed class Fixture : IDisposable
    {
        public HttpFactory Factory = new(); public HttpClient Client { get; }
        public Guid Academy = Guid.NewGuid(), Foreign = Guid.NewGuid(), Scheme = Guid.NewGuid(), ForeignScheme = Guid.NewGuid(), Actor = Guid.NewGuid();
        public string Governance => $"/api/academies/{Academy}/academic-governance";
        public string Lifecycle => $"/api/academies/{Academy}/grading-schemes";
        public Fixture() => Client = Factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
        public async Task Seed(string actor, bool module = true, bool active = true)
        {
            await using var scope = Factory.Services.CreateAsyncScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            db.AddRange(new Academy { Id = Academy, Name = "Synthetic academy", IsActive = active, EnabledModulesJson = module ? "[\"Core\",\"AcademicGovernance\"]" : "[\"Core\"]" }, new Academy { Id = Foreign, Name = "Foreign academy" });
            db.AddRange(new GradingScheme { Id = Scheme, AcademyId = Academy, Name = "Owned active", PassingPercent = 50, BandsJson = "[]" }, new GradingScheme { AcademyId = Academy, Name = "Owned inactive", IsActive = false }, new GradingScheme { Id = ForeignScheme, AcademyId = Foreign, Name = "Foreign policy" }); await db.SaveChangesAsync();
            var roles = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
            Assert.True((await roles.CreateAsync(new ApplicationRole { Name = actor, PermissionsJson = actor == "role" ? "[\"academics.manage\"]" : "[]", IsSystemRole = actor is "Owner" or "AcademyAdmin" or "Teacher" or "Sales" or "Operations" or "FinanceUser" })).Succeeded);
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = new ApplicationUser { Id = Actor, UserName = "academic-lifecycle@example.invalid", Email = "academic-lifecycle@example.invalid", EmailConfirmed = true, DisplayName = "Synthetic academic actor", AcademyId = Academy, IsPlatformOwner = actor == "PlatformOwner" };
            Assert.True((await users.CreateAsync(user, "Synthetic!39Ab")).Succeeded); Assert.True((await users.AddToRoleAsync(user, actor)).Succeeded);
            if (new[] { "permanent", "timed", "expired", "revoked", "wrong", "foreign-grant" }.Contains(actor))
            {
                var identity = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
                identity.Add(new AccessGrant { AcademyId = actor == "foreign-grant" ? Foreign : Academy, UserId = Actor, GrantedByUserId = Actor, PermissionsJson = actor == "wrong" ? "[\"sales.manage\"]" : "[\"academics.manage\"]", IsPermanent = actor == "permanent", ExpiresAtUtc = DateTimeOffset.UtcNow.AddHours(actor == "expired" ? -1 : 1), RevokedAtUtc = actor == "revoked" ? DateTimeOffset.UtcNow : null }); await identity.SaveChangesAsync();
            }
        }
        public async Task Login() { using var r = await Client.PostAsJsonAsync("/api/auth/login", new { email = "academic-lifecycle@example.invalid", password = "Synthetic!39Ab" }); Assert.Equal(HttpStatusCode.OK, r.StatusCode); using var json = JsonDocument.Parse(await r.Content.ReadAsStringAsync()); Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", json.RootElement.GetProperty("accessToken").GetString()); }
        public async Task<JsonElement> Read(string path) { using var r = await Client.GetAsync(path); Assert.Equal(HttpStatusCode.OK, r.StatusCode); using var json = JsonDocument.Parse(await r.Content.ReadAsStringAsync()); return json.RootElement.Clone(); }
        public async Task<string> Snapshot() { await using var scope = Factory.Services.CreateAsyncScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); return JsonSerializer.Serialize(new { schemes = await db.GradingSchemes.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), audits = await db.AuditLogs.AsNoTracking().OrderBy(x => x.Id).ToListAsync() }); }
        public void Dispose() { Client.Dispose(); Factory.Dispose(); }
    }
    private sealed class HttpFactory : WebApplicationFactory<Program>
    {
        private readonly string database = $"academic-lifecycle-{Guid.NewGuid():N}";
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing"); builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?> { ["Database:ApplyMigrationsOnStartup"] = "false", ["Bootstrap:PlatformOwnerEmail"] = "", ["Bootstrap:PlatformOwnerPassword"] = "", ["MediaStorage:Enabled"] = "false", ["Penta:Enabled"] = "false" }));
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<AcademyDeskDbContext>>(); services.RemoveAll<DbContextOptions<IdentityDbContext>>(); services.RemoveAll<IDbContextOptionsConfiguration<AcademyDeskDbContext>>(); services.RemoveAll<IDbContextOptionsConfiguration<IdentityDbContext>>(); services.RemoveAll<AcademyDeskDbContext>(); services.RemoveAll<IdentityDbContext>();
                services.AddDbContext<AcademyDeskDbContext>(o => o.UseInMemoryDatabase(database + "-domain")); services.AddDbContext<IdentityDbContext>(o => o.UseInMemoryDatabase(database + "-identity"));
            });
        }
    }
}
