using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Domain.Identity;
using AcademyDesk.Api.Intelligence.Penta;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.InMemory;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace AcademyDesk.Api.Tests;

public sealed class PentaFoundationTests
{
    private sealed class LocalEnvironment(string name) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = name;
        public string ApplicationName { get; set; } = "PentaFoundationTests";
        public string ContentRootPath { get; set; } = string.Empty;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private sealed class FakeProvider : IPentaSyntheticProvider
    {
        public int Calls { get; private set; }
        public bool Malformed { get; set; }

        public Task<PentaSyntheticResult> ExecuteAsync(string capability, CancellationToken token)
        {
            Calls++;
            token.ThrowIfCancellationRequested();
            return Task.FromResult(new PentaSyntheticResult(Guid.NewGuid(), capability,
                Malformed ? "arbitrary.tool" : PentaSyntheticDispatcher.ToolName,
                "Synthetic result", true));
        }
    }

    private sealed class Factory(bool enabled, FakeProvider provider) : WebApplicationFactory<Program>
    {
        private readonly string database = $"penta-{Guid.NewGuid():N}";

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Penta:Enabled"] = enabled.ToString(),
                    ["Database:ApplyMigrationsOnStartup"] = "false",
                    ["Bootstrap:PlatformOwnerEmail"] = "",
                    ["Bootstrap:PlatformOwnerPassword"] = ""
                }));
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<AcademyDeskDbContext>>();
                services.RemoveAll<DbContextOptions<IdentityDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<AcademyDeskDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<IdentityDbContext>>();
                services.RemoveAll<AcademyDeskDbContext>();
                services.RemoveAll<IdentityDbContext>();
                services.AddDbContext<AcademyDeskDbContext>(options => options.UseInMemoryDatabase(database + "-domain"));
                services.AddDbContext<IdentityDbContext>(options => options.UseInMemoryDatabase(database + "-identity"));
                services.RemoveAll<IPentaSyntheticProvider>();
                services.AddSingleton<IPentaSyntheticProvider>(provider);
            });
        }
    }

    [Fact]
    public async Task Gateway_enforces_identity_tenant_role_bounds_and_provider_output()
    {
        var provider = new FakeProvider();
        using var factory = new Factory(true, provider);
        using var client = factory.CreateClient(new() { AllowAutoRedirect = false });
        var academyA = Guid.NewGuid();
        var academyB = Guid.NewGuid();
        await SeedAsync(factory, academyA, academyB);
        var pathA = $"/api/academies/{academyA}/penta/turns";
        var pathB = $"/api/academies/{academyB}/penta/turns";
        var valid = new { text = "synthetic", capability = "pulse" };

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync(pathA, valid)).StatusCode);
        Assert.Equal(0, provider.Calls);

        var tokenA = await LoginAsync(client, "admin-a");
        var tokenB = await LoginAsync(client, "admin-b");
        var teacher = await LoginAsync(client, "teacher-a");
        var platform = await LoginAsync(client, "platform");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync(pathB, valid)).StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", teacher);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync(pathA, valid)).StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", platform);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync(pathA, valid)).StatusCode);
        Assert.Equal(0, provider.Calls);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync(pathA,
            new { text = "x", capability = "executor", actorUserId = Guid.NewGuid() })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync(pathA,
            new { text = "x", capability = "executor", tool = "payments.create" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync(pathA,
            new { text = "x", capability = "unknown" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync(pathA,
            new { text = new string('X', 4001), capability = "pulse" })).StatusCode);
        Assert.Equal(0, provider.Calls);

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var academy = await db.Academies.SingleAsync(x => x.Id == academyA);
            academy.IsActive = false;
            await db.SaveChangesAsync();
        }
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync(pathA, valid)).StatusCode);
        Assert.Equal(0, provider.Calls);
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var academy = await db.Academies.SingleAsync(x => x.Id == academyA);
            academy.IsActive = true;
            await db.SaveChangesAsync();
        }

        using (var success = await client.PostAsJsonAsync(pathA, valid))
        {
            Assert.Equal(HttpStatusCode.OK, success.StatusCode);
            var result = await success.Content.ReadFromJsonAsync<PentaSyntheticResult>();
            Assert.NotNull(result);
            Assert.Equal(PentaSyntheticDispatcher.ToolName, result.Tool);
            Assert.Equal("pulse", result.Capability);
            Assert.True(result.Synthetic);
        }
        Assert.Equal(1, provider.Calls);
        provider.Malformed = true;
        Assert.Equal(HttpStatusCode.BadGateway, (await client.PostAsJsonAsync(pathA, valid)).StatusCode);
        Assert.Equal(2, provider.Calls);

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            Assert.Equal(1, await db.AuditLogs.CountAsync(x => x.AcademyId == academyA && x.EntityType == "Penta"));
        }

        provider.Malformed = false;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenB);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync(pathB, valid)).StatusCode);

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = (await users.FindByEmailAsync("penta-admin-b@example.invalid"))!;
            user.IsActive = false;
            Assert.True((await users.UpdateAsync(user)).Succeeded);
        }
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync(pathB, valid)).StatusCode);
        Assert.Equal(3, provider.Calls);
    }

    [Fact]
    public async Task Missing_feature_flag_does_not_dispatch()
    {
        var provider = new FakeProvider();
        using var factory = new Factory(false, provider);
        using var client = factory.CreateClient(new() { AllowAutoRedirect = false });
        var academy = Guid.NewGuid();
        await SeedAsync(factory, academy, Guid.NewGuid());
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await LoginAsync(client, "admin-a"));
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync(
            $"/api/academies/{academy}/penta/turns", new { text = "x", capability = "pulse" })).StatusCode);
        Assert.Equal(0, provider.Calls);
    }

    [Fact]
    public async Task Dispatcher_rejects_a_cancelled_operation()
    {
        var provider = new FakeProvider();
        var dispatcher = new PentaSyntheticDispatcher(provider);
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => dispatcher.RunAsync("pulse", cancelled.Token));
    }

    [Theory]
    [InlineData("Production", true, false)]
    [InlineData("Development", true, true)]
    [InlineData("Testing", true, true)]
    [InlineData("Testing", false, false)]
    public void Synthetic_tool_requires_local_environment_and_explicit_flag(string environment, bool enabled, bool expected)
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Penta:Enabled"] = enabled.ToString()
        }).Build();
        var policy = new PentaPilotPolicy(null!, null!, config, new LocalEnvironment(environment));
        Assert.Equal(expected, policy.IsAvailable);
    }

    private static async Task SeedAsync(Factory factory, Guid academyA, Guid academyB)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
        db.Academies.AddRange(new Academy { Id = academyA, Name = "A" }, new Academy { Id = academyB, Name = "B" });
        await db.SaveChangesAsync();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        Assert.True((await roles.CreateAsync(new ApplicationRole { Name = "AcademyAdmin", IsSystemRole = true })).Succeeded);
        Assert.True((await roles.CreateAsync(new ApplicationRole { Name = "Teacher", IsSystemRole = true })).Succeeded);
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        foreach (var (name, academy, role, isPlatform) in new[]
        {
            ("admin-a", (Guid?)academyA, "AcademyAdmin", false),
            ("admin-b", (Guid?)academyB, "AcademyAdmin", false),
            ("teacher-a", (Guid?)academyA, "Teacher", false),
            ("platform", (Guid?)null, "", true)
        })
        {
            var address = $"penta-{name}@example.invalid";
            var user = new ApplicationUser
            {
                UserName = address, Email = address, EmailConfirmed = true,
                DisplayName = name, AcademyId = academy, IsPlatformOwner = isPlatform
            };
            Assert.True((await users.CreateAsync(user, "Synthetic!39Ab")).Succeeded);
            if (role.Length > 0) Assert.True((await users.AddToRoleAsync(user, role)).Succeeded);
        }
    }

    private static async Task<string> LoginAsync(HttpClient client, string name)
    {
        using var response = await client.PostAsJsonAsync("/api/auth/login",
            new { email = $"penta-{name}@example.invalid", password = "Synthetic!39Ab" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var payload = await System.Text.Json.JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        return payload.RootElement.GetProperty("accessToken").GetString()!;
    }
}
