using System.Text.Json;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Domain.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AcademyDesk.Api.Tests;

public sealed class ExpenseBranchOptionTests
{
    [Theory] [InlineData("FinanceUser")] [InlineData("Owner")] [InlineData("AcademyAdmin")]
    public async Task Finance_scope_without_MultiBranch_can_read_only_local_id_name_options(string role)
    {
        using var f = new Fixture(); await f.Seed(role); await f.Login(); var before = await f.Snapshot();
        using (var legacy = await f.Client.GetAsync($"/api/academies/{f.Academy}/branches")) Assert.Equal(HttpStatusCode.Forbidden, legacy.StatusCode);
        var rows = (await f.Options()).EnumerateArray().ToArray(); Assert.Equal(new[] { "Alpha", "Alpha", "Zulu" }, rows.Select(x => x.GetProperty("name").GetString()));
        Assert.Equal(new[] { 1, 2, 3 }.Select(Id), rows.Select(x => x.GetProperty("id").GetGuid()));
        foreach (var row in rows) Assert.Equal(new[] { "id", "name" }, row.EnumerateObject().Select(x => x.Name).Order());
        using (var ledger = await f.Client.GetAsync($"/api/academies/{f.Academy}/expenses")) Assert.Equal(HttpStatusCode.OK, ledger.StatusCode);
        Assert.Equal(before, await f.Snapshot());
    }
    [Theory] [InlineData("Teacher")] [InlineData("Student")] [InlineData("Sales")] [InlineData("Operations")]
    public async Task Nonfinancial_roles_remain_denied_without_branch_or_audit_changes(string role)
    { using var f = new Fixture(); await f.Seed(role); await f.Login(); var before = await f.Snapshot(); using var r = await f.Client.GetAsync(f.Path); Assert.Equal(HttpStatusCode.Forbidden, r.StatusCode); Assert.Equal(before, await f.Snapshot()); }
    [Fact] public async Task Anonymous_and_foreign_academy_are_denied_without_mutations()
    {
        using var f = new Fixture(); await f.Seed(); var before = await f.Snapshot(); using (var r = await f.Client.GetAsync(f.Path)) Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
        await f.Login(); using (var r = await f.Client.GetAsync($"/api/academies/{f.Foreign}/expenses/branch-options")) Assert.Equal(HttpStatusCode.Forbidden, r.StatusCode); Assert.Equal(before, await f.Snapshot());
    }
    [Fact] public async Task Missing_Finance_module_does_not_gain_access_through_the_lookup()
    { using var f = new Fixture(); await f.Seed(finance: false); await f.Login(); var before = await f.Snapshot(); using var r = await f.Client.GetAsync(f.Path); Assert.Equal(HttpStatusCode.Forbidden, r.StatusCode); Assert.Equal(before, await f.Snapshot()); }
    [Fact] public async Task Empty_options_do_not_create_a_branch_or_audit_row()
    { using var f = new Fixture(); await f.Seed(branches: false); await f.Login(); var before = await f.Snapshot(); Assert.Empty((await f.Options()).EnumerateArray()); Assert.Equal(before, await f.Snapshot()); }
    [Fact] public async Task Current_platform_owner_flag_preserves_cross_academy_scope_and_revocation()
    {
        using var f = new Fixture(); await f.Seed("PlatformOwner", owner: true); await f.Login(); var before = await f.Snapshot();
        using (var r = await f.Client.GetAsync($"/api/academies/{f.Foreign}/expenses/branch-options")) { Assert.Equal(HttpStatusCode.OK, r.StatusCode); using var json = JsonDocument.Parse(await r.Content.ReadAsStringAsync()); Assert.Equal("Foreign branch", Assert.Single(json.RootElement.EnumerateArray()).GetProperty("name").GetString()); }
        await using (var scope = f.Factory.Services.CreateAsyncScope()) { var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(); var user = (await users.FindByEmailAsync("expense-options@example.invalid"))!; user.IsPlatformOwner = false; Assert.True((await users.UpdateAsync(user)).Succeeded); }
        using (var r = await f.Client.GetAsync($"/api/academies/{f.Foreign}/expenses/branch-options")) Assert.Equal(HttpStatusCode.Forbidden, r.StatusCode); Assert.Equal(before, await f.Snapshot());
    }
    [Fact] public async Task Inactive_academy_remains_denied()
    {
        using var f = new Fixture(); await f.Seed(); await using (var scope = f.Factory.Services.CreateAsyncScope()) { var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); (await db.Academies.SingleAsync(x => x.Id == f.Academy)).IsActive = false; await db.SaveChangesAsync(); }
        await f.Login(); var before = await f.Snapshot(); using var r = await f.Client.GetAsync(f.Path); Assert.Equal(HttpStatusCode.Forbidden, r.StatusCode); Assert.Equal(before, await f.Snapshot());
    }
    [Fact] public async Task Lookup_does_not_grant_branch_management_even_with_MultiBranch_enabled()
    {
        using var f = new Fixture(); await f.Seed(); await using (var scope = f.Factory.Services.CreateAsyncScope()) { var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); (await db.Academies.SingleAsync(x => x.Id == f.Academy)).EnabledModulesJson = "[\"Core\",\"Finance\",\"MultiBranch\"]"; await db.SaveChangesAsync(); }
        await f.Login(); var before = await f.Snapshot(); Assert.Equal(3, (await f.Options()).GetArrayLength());
        using (var r = await f.Client.PostAsJsonAsync($"/api/academies/{f.Academy}/branches", new { name = "Forbidden new branch" })) Assert.Equal(HttpStatusCode.Forbidden, r.StatusCode); Assert.Equal(before, await f.Snapshot());
    }
    private static Guid Id(int n) => Guid.Parse($"00000000-0000-4000-8000-{n:D12}");
    private sealed class Fixture : IDisposable
    {
        public HttpFactory Factory = new(); public HttpClient Client { get; } public Guid Academy = Guid.NewGuid(), Foreign = Guid.NewGuid(); public string Path => $"/api/academies/{Academy}/expenses/branch-options";
        public Fixture() => Client = Factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
        public async Task Seed(string role = "FinanceUser", bool finance = true, bool branches = true, bool owner = false)
        {
            await using var scope = Factory.Services.CreateAsyncScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            db.Add(new Academy { Id = Academy, Name = "Synthetic finance academy", EnabledModulesJson = finance ? "[\"Core\",\"Finance\"]" : "[\"Core\"]" }); db.Add(new Academy { Id = Foreign, Name = "Synthetic foreign academy" });
            if (branches) db.AddRange(new Branch { Id = Id(3), AcademyId = Academy, Name = "Zulu", AddressLine1 = "Private address", City = "Private city" }, new Branch { Id = Id(2), AcademyId = Academy, Name = "Alpha", PostalCode = "PRIVATE", IsActive = false }, new Branch { Id = Id(1), AcademyId = Academy, Name = "Alpha" });
            db.Add(new Branch { Id = Id(4), AcademyId = Foreign, Name = "Foreign branch", AddressLine1 = "Foreign private address" }); await db.SaveChangesAsync();
            var roles = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>(); Assert.True((await roles.CreateAsync(new ApplicationRole { Name = role, IsSystemRole = true })).Succeeded);
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(); var user = new ApplicationUser { UserName = "expense-options@example.invalid", Email = "expense-options@example.invalid", EmailConfirmed = true, DisplayName = "Synthetic finance actor", AcademyId = Academy, IsPlatformOwner = owner };
            Assert.True((await users.CreateAsync(user, "Synthetic!39Ab")).Succeeded); Assert.True((await users.AddToRoleAsync(user, role)).Succeeded);
        }
        public async Task Login() { using var r = await Client.PostAsJsonAsync("/api/auth/login", new { email = "expense-options@example.invalid", password = "Synthetic!39Ab" }); Assert.Equal(HttpStatusCode.OK, r.StatusCode); using var json = JsonDocument.Parse(await r.Content.ReadAsStringAsync()); Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", json.RootElement.GetProperty("accessToken").GetString()); }
        public async Task<JsonElement> Options() { using var r = await Client.GetAsync(Path); Assert.Equal(HttpStatusCode.OK, r.StatusCode); using var json = JsonDocument.Parse(await r.Content.ReadAsStringAsync()); return json.RootElement.Clone(); }
        public async Task<string> Snapshot() { await using var scope = Factory.Services.CreateAsyncScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); return JsonSerializer.Serialize(new { branches = await db.Branches.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), academies = await db.Academies.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), audit = await db.AuditLogs.AsNoTracking().OrderBy(x => x.Id).ToListAsync() }); }
        public void Dispose() { Client.Dispose(); Factory.Dispose(); }
    }
    private sealed class HttpFactory : WebApplicationFactory<Program>
    {
        private readonly string database = $"expense-options-http-{Guid.NewGuid():N}";
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing"); builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
            { ["Database:ApplyMigrationsOnStartup"] = "false", ["Bootstrap:PlatformOwnerEmail"] = "", ["Bootstrap:PlatformOwnerPassword"] = "", ["MediaStorage:Enabled"] = "false", ["Penta:Enabled"] = "false" }));
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<AcademyDeskDbContext>>(); services.RemoveAll<DbContextOptions<IdentityDbContext>>(); services.RemoveAll<IDbContextOptionsConfiguration<AcademyDeskDbContext>>(); services.RemoveAll<IDbContextOptionsConfiguration<IdentityDbContext>>(); services.RemoveAll<AcademyDeskDbContext>(); services.RemoveAll<IdentityDbContext>();
                services.AddDbContext<AcademyDeskDbContext>(o => o.UseInMemoryDatabase(database + "-domain")); services.AddDbContext<IdentityDbContext>(o => o.UseInMemoryDatabase(database + "-identity"));
            });
        }
    }

}
