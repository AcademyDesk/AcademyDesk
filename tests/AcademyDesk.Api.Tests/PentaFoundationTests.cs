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
    private sealed class AdvancingClock : TimeProvider
    {
        private DateTimeOffset now = DateTimeOffset.UtcNow;
        public override DateTimeOffset GetUtcNow() => now;
        public void Advance(TimeSpan duration) => now = now.Add(duration);
    }
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
        public bool Throw { get; set; }

        public Task<PentaSyntheticResult> ExecuteAsync(string capability, CancellationToken token)
        {
            Calls++;
            token.ThrowIfCancellationRequested();
            if (Throw) throw new InvalidOperationException("Synthetic provider failure.");
            return Task.FromResult(new PentaSyntheticResult(Guid.NewGuid(), capability,
                Malformed ? "arbitrary.tool" : PentaSyntheticDispatcher.ToolName,
                "Synthetic result", true));
        }
    }

    private sealed class Factory(bool enabled, FakeProvider provider, int? userLimit = null,
        int? academyLimit = null, string? userLimitText = null, TimeProvider? clock = null) : WebApplicationFactory<Program>
    {
        private readonly string database = $"penta-{Guid.NewGuid():N}";

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Penta:Enabled"] = enabled.ToString(),
                    ["Penta:SyntheticDailyUserLimit"] = userLimitText ?? userLimit?.ToString(),
                    ["Penta:SyntheticDailyAcademyLimit"] = academyLimit?.ToString(),
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
                if (clock is not null)
                {
                    services.RemoveAll<TimeProvider>();
                    services.AddSingleton(clock);
                }
            });
        }
    }

    [Fact]
    public async Task Academy_context_is_minimal_tenant_scoped_audited_and_model_free()
    {
        var provider = new FakeProvider();
        using var factory = new Factory(true, provider);
        using var client = factory.CreateClient(new() { AllowAutoRedirect = false });
        var academyA = Guid.NewGuid();
        var academyB = Guid.NewGuid();
        await SeedAsync(factory, academyA, academyB);
        var pathA = $"/api/academies/{academyA}/penta/academy-context";
        var pathB = $"/api/academies/{academyB}/penta/academy-context";

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(pathA)).StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await LoginAsync(client, "teacher-a"));
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync(pathA)).StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await LoginAsync(client, "platform"));
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync(pathA)).StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await LoginAsync(client, "admin-b"));
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync(pathA)).StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await LoginAsync(client, "admin-a"));
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync(pathB)).StatusCode);

        using (var response = await client.GetAsync(pathA))
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            using var json = await System.Text.Json.JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
            var fields = json.RootElement.EnumerateObject().Select(x => x.Name).ToArray();
            Assert.Equal(new[] { "tool", "capability", "academyId", "name", "countryCode", "timeZone", "asOfUtc", "source", "sourcePath" }, fields);
            Assert.Equal(PentaAcademyContextService.ToolName, json.RootElement.GetProperty("tool").GetString());
            Assert.Equal("twin", json.RootElement.GetProperty("capability").GetString());
            Assert.Equal(academyA, json.RootElement.GetProperty("academyId").GetGuid());
            Assert.Equal("A", json.RootElement.GetProperty("name").GetString());
            Assert.Equal("Academy", json.RootElement.GetProperty("source").GetString());
        }
        Assert.Equal(0, provider.Calls);

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
        Assert.Equal(1, await db.AuditLogs.CountAsync(x => x.Action == "PentaAcademyContextRead" && x.AcademyId == academyA));
        Assert.Empty(await db.PentaExecutions.ToListAsync());
        Assert.Empty(await db.PentaAttempts.ToListAsync());
        Assert.Empty(await db.AdminWorkItems.ToListAsync());
        Assert.Empty(await db.PentaUsageReservations.ToListAsync());

        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var actor = (await users.FindByEmailAsync("penta-admin-a@example.invalid"))!;
        actor.IsActive = false;
        Assert.True((await users.UpdateAsync(actor)).Succeeded);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync(pathA)).StatusCode);
        Assert.Equal(1, await db.AuditLogs.CountAsync(x => x.Action == "PentaAcademyContextRead"));
    }

    [Fact]
    public async Task Academy_context_denies_inactive_academy_without_audit_or_model_call()
    {
        var provider = new FakeProvider();
        using var factory = new Factory(true, provider);
        using var client = factory.CreateClient(new() { AllowAutoRedirect = false });
        var academy = Guid.NewGuid();
        await SeedAsync(factory, academy, Guid.NewGuid());
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await LoginAsync(client, "admin-a"));
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            (await db.Academies.SingleAsync(x => x.Id == academy)).IsActive = false;
            await db.SaveChangesAsync();
        }
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync(
            $"/api/academies/{academy}/penta/academy-context")).StatusCode);
        await using var verify = factory.Services.CreateAsyncScope();
        Assert.Empty(await verify.ServiceProvider.GetRequiredService<AcademyDeskDbContext>()
            .AuditLogs.Where(x => x.Action == "PentaAcademyContextRead").ToListAsync());
        Assert.Equal(0, provider.Calls);
    }

    [Fact]
    public async Task Student_search_is_bounded_minimal_tenant_scoped_audited_and_model_free()
    {
        var provider = new FakeProvider();
        using var factory = new Factory(true, provider);
        using var client = factory.CreateClient(new() { AllowAutoRedirect = false });
        var academyA = Guid.NewGuid();
        var academyB = Guid.NewGuid();
        await SeedAsync(factory, academyA, academyB);
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            for (var number = 0; number < 12; number++)
                db.Students.Add(new Student { AcademyId = academyA, FirstName = "Meera",
                    LastName = $"Learner{number:00}", Email = "private@example.invalid",
                    MedicalOrAccessibilityNotes = "private medical note", StudentNumber = $"A{number:00}" });
            db.Students.Add(new Student { AcademyId = academyA, FirstName = "Meera",
                LastName = "Inactive", IsActive = false });
            db.Students.Add(new Student { AcademyId = academyB, FirstName = "Meera",
                LastName = "Foreign" });
            await db.SaveChangesAsync();
        }
        var pathA = $"/api/academies/{academyA}/penta/student-search?q=Meera";
        var pathB = $"/api/academies/{academyB}/penta/student-search?q=Meera";
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(pathA)).StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await LoginAsync(client, "teacher-a"));
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync(pathA)).StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await LoginAsync(client, "platform"));
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync(pathA)).StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await LoginAsync(client, "admin-b"));
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync(pathA)).StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await LoginAsync(client, "admin-a"));
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync(pathB)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync(
            $"/api/academies/{academyA}/penta/student-search?q=x")).StatusCode);
        using (var response = await client.GetAsync(pathA))
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadAsStringAsync();
            Assert.DoesNotContain("private@example.invalid", body);
            Assert.DoesNotContain("private medical note", body);
            using var json = System.Text.Json.JsonDocument.Parse(body);
            var root = json.RootElement;
            Assert.Equal(new[] { "tool", "capability", "academyId", "asOfUtc", "timeZone",
                "source", "maxRows", "hasMore", "rows" },
                root.EnumerateObject().Select(x => x.Name));
            Assert.Equal(PentaStudentSearchService.ToolName, root.GetProperty("tool").GetString());
            Assert.Equal("executor", root.GetProperty("capability").GetString());
            Assert.Equal(academyA, root.GetProperty("academyId").GetGuid());
            Assert.Equal("Students", root.GetProperty("source").GetString());
            Assert.Equal(10, root.GetProperty("maxRows").GetInt32());
            Assert.True(root.GetProperty("hasMore").GetBoolean());
            var rows = root.GetProperty("rows").EnumerateArray().ToArray();
            Assert.Equal(10, rows.Length);
            Assert.All(rows, row =>
            {
                Assert.Equal(new[] { "sourceId", "displayName", "isActive", "sourcePath" },
                    row.EnumerateObject().Select(x => x.Name));
                Assert.StartsWith("Meera Learner", row.GetProperty("displayName").GetString());
                Assert.True(row.GetProperty("isActive").GetBoolean());
                Assert.Equal($"/student-management?studentId={row.GetProperty("sourceId").GetGuid():D}",
                    row.GetProperty("sourcePath").GetString());
            });
        }
        Assert.Equal(0, provider.Calls);
        using (var empty = await client.GetAsync(
            $"/api/academies/{academyA}/penta/student-search?q=Absent"))
        {
            Assert.Equal(HttpStatusCode.OK, empty.StatusCode);
            using var json = await System.Text.Json.JsonDocument.ParseAsync(await empty.Content.ReadAsStreamAsync());
            Assert.Empty(json.RootElement.GetProperty("rows").EnumerateArray());
            Assert.False(json.RootElement.GetProperty("hasMore").GetBoolean());
        }
        await using var verify = factory.Services.CreateAsyncScope();
        var audit = verify.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
        Assert.Equal(2, await audit.AuditLogs.CountAsync(x => x.Action == "PentaStudentSearchRead" &&
            x.AcademyId == academyA));
        Assert.Equal(0, await audit.AuditLogs.CountAsync(x => x.Action == "PentaStudentSearchRead" &&
            x.AcademyId == academyB));
        Assert.Empty(await audit.PentaExecutions.ToListAsync());
        Assert.Empty(await audit.PentaUsageReservations.ToListAsync());
        var users = verify.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var actor = (await users.FindByEmailAsync("penta-admin-a@example.invalid"))!;
        actor.IsActive = false;
        Assert.True((await users.UpdateAsync(actor)).Succeeded);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync(pathA)).StatusCode);
        Assert.Equal(2, await audit.AuditLogs.CountAsync(x => x.Action == "PentaStudentSearchRead"));
    }

    [Fact]
    public async Task Draft_preview_requires_current_authority_exact_confirmation_and_has_no_domain_effect()
    {
        var provider = new FakeProvider();
        using var factory = new Factory(true, provider);
        using var client = factory.CreateClient(new() { AllowAutoRedirect = false });
        var academyA = Guid.NewGuid();
        var academyB = Guid.NewGuid();
        await SeedAsync(factory, academyA, academyB);
        var path = $"/api/academies/{academyA}/penta/draft-previews";
        async Task<HttpResponseMessage> Preview(string title, string key)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, path)
            { Content = JsonContent.Create(new { title }) };
            request.Headers.Add("Idempotency-Key", key);
            return await client.SendAsync(request);
        }
        Assert.Equal(HttpStatusCode.Unauthorized, (await Preview("Review schedule", "draft-test-one")).StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await LoginAsync(client, "teacher-a"));
        Assert.Equal(HttpStatusCode.Forbidden, (await Preview("Review schedule", "draft-test-one")).StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await LoginAsync(client, "platform"));
        Assert.Equal(HttpStatusCode.Forbidden, (await Preview("Review schedule", "draft-test-one")).StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await LoginAsync(client, "admin-b"));
        Assert.Equal(HttpStatusCode.Forbidden, (await Preview("Review schedule", "draft-test-one")).StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await LoginAsync(client, "admin-a"));
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync(path,
            new { title = "Review schedule", actorUserId = Guid.NewGuid() })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Preview(new string('X', 121), "draft-test-one")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Preview("Call student about fees", "draft-test-one")).StatusCode);
        using var prepared = await Preview("  Review schedule  ", "draft-test-one");
        Assert.Equal(HttpStatusCode.Created, prepared.StatusCode);
        var state = await prepared.Content.ReadFromJsonAsync<PentaDraftPreviewState>();
        Assert.NotNull(state);
        Assert.Equal("Review schedule", state.Title);
        Assert.Equal("AwaitingApproval", state.Status);
        Assert.Contains("No academy work item", state.Effect);
        using var replay = await Preview("Review schedule", "draft-test-one");
        Assert.Equal(HttpStatusCode.OK, replay.StatusCode);
        Assert.Equal(state.ApprovalId, (await replay.Content.ReadFromJsonAsync<PentaDraftPreviewState>())!.ApprovalId);
        Assert.Equal(HttpStatusCode.Conflict, (await Preview("Review attendance", "draft-test-one")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"{path}/{state.ApprovalId}")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync(
            $"{path}/{state.ApprovalId}/confirm", new { digest = new string('0', 64) })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync(
            $"{path}/{state.ApprovalId}/confirm", new { digest = state.Digest, title = "Changed" })).StatusCode);
        using var confirmed = await client.PostAsJsonAsync($"{path}/{state.ApprovalId}/confirm",
            new { digest = state.Digest });
        Assert.Equal(HttpStatusCode.OK, confirmed.StatusCode);
        Assert.Equal("Approved", (await confirmed.Content.ReadFromJsonAsync<PentaDraftPreviewState>())!.Status);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync(
            $"{path}/{state.ApprovalId}/confirm", new { digest = state.Digest })).StatusCode);
        Assert.Equal(0, provider.Calls);
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
        Assert.Equal(1, await db.PentaApprovals.CountAsync());
        Assert.Equal(1, await db.PentaExecutions.CountAsync(x => x.Status == "Approved"));
        Assert.Equal(2, await db.AuditLogs.CountAsync(x => x.EntityType == "PentaApproval"));
        Assert.Empty(await db.AdminWorkItems.ToListAsync());
        Assert.Empty(await db.PentaAttempts.ToListAsync());
    }

    [Fact]
    public async Task Draft_confirmation_denies_revoked_actor_and_expired_preview()
    {
        var clock = new AdvancingClock();
        using var factory = new Factory(true, new FakeProvider(), clock: clock);
        using var client = factory.CreateClient(new() { AllowAutoRedirect = false });
        var academy = Guid.NewGuid();
        await SeedAsync(factory, academy, Guid.NewGuid());
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await LoginAsync(client, "admin-a"));
        var path = $"/api/academies/{academy}/penta/draft-previews";
        using var request = new HttpRequestMessage(HttpMethod.Post, path)
        { Content = JsonContent.Create(new { title = "Review schedule" }) };
        request.Headers.Add("Idempotency-Key", "draft-expire-one");
        using var prepared = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, prepared.StatusCode);
        var state = (await prepared.Content.ReadFromJsonAsync<PentaDraftPreviewState>())!;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var actor = (await users.FindByEmailAsync("penta-admin-a@example.invalid"))!;
            actor.IsActive = false;
            Assert.True((await users.UpdateAsync(actor)).Succeeded);
        }
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync(
            $"{path}/{state.ApprovalId}/confirm", new { digest = state.Digest })).StatusCode);
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var actor = (await users.FindByEmailAsync("penta-admin-a@example.invalid"))!;
            actor.IsActive = true;
            Assert.True((await users.UpdateAsync(actor)).Succeeded);
        }
        clock.Advance(TimeSpan.FromMinutes(11));
        Assert.Equal(HttpStatusCode.Gone, (await client.PostAsJsonAsync(
            $"{path}/{state.ApprovalId}/confirm", new { digest = state.Digest })).StatusCode);
        await using var verify = factory.Services.CreateAsyncScope();
        var db = verify.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
        Assert.Equal("Expired", (await db.PentaApprovals.SingleAsync()).Status);
        Assert.Empty(await db.AdminWorkItems.ToListAsync());
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
            Assert.Equal(2, await db.AuditLogs.CountAsync(x => x.AcademyId == academyA && x.EntityType == "PentaExecution"));
            Assert.Equal(2, await db.PentaExecutions.CountAsync(x => x.AcademyId == academyA));
            Assert.Equal(2, await db.PentaAttempts.CountAsync(x => x.AcademyId == academyA));
            Assert.Equal(2, await db.PentaUsageReservations.CountAsync(x => x.AcademyId == academyA));
            Assert.Equal(2, await db.PentaUsageEntries.CountAsync(x => x.AcademyId == academyA));
            Assert.Equal(0, await db.PentaApprovals.CountAsync());
            Assert.All(await db.PentaExecutions.ToListAsync(), x => Assert.Equal("None", x.ApprovalMode));
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
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync(
            $"/api/academies/{academy}/penta/draft-previews", new { title = "Review schedule" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync(
            $"/api/academies/{academy}/penta/academy-context")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync(
            $"/api/academies/{academy}/penta/student-search?q=Meera")).StatusCode);
        Assert.Equal(0, provider.Calls);
    }

    [Fact]
    public async Task Repeated_key_replays_result_and_changed_arguments_conflict()
    {
        var provider = new FakeProvider();
        using var factory = new Factory(true, provider);
        using var client = factory.CreateClient(new() { AllowAutoRedirect = false });
        var academy = Guid.NewGuid();
        await SeedAsync(factory, academy, Guid.NewGuid());
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await LoginAsync(client, "admin-a"));
        var path = $"/api/academies/{academy}/penta/turns";
        async Task<HttpResponseMessage> Post(string text)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, path)
            {
                Content = JsonContent.Create(new { text, capability = "pulse" })
            };
            request.Headers.Add("Idempotency-Key", "unit-key-1");
            return await client.SendAsync(request);
        }
        using var first = await Post("synthetic");
        using var repeat = await Post("synthetic");
        using var conflict = await Post("changed");
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, repeat.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
        var firstState = await first.Content.ReadFromJsonAsync<PentaTurnState>();
        var repeatedState = await repeat.Content.ReadFromJsonAsync<PentaTurnState>();
        Assert.Equal(firstState, repeatedState);
        Assert.Equal(1, provider.Calls);
        using var status = await client.GetAsync($"/api/academies/{academy}/penta/tasks/{firstState!.TaskId}");
        Assert.Equal(HttpStatusCode.OK, status.StatusCode);
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
        Assert.Equal(1, await db.PentaTasks.CountAsync());
        Assert.Equal(1, await db.PentaExecutions.CountAsync());
        Assert.Equal(1, await db.PentaAttempts.CountAsync());
        Assert.Equal(1, await db.AuditLogs.CountAsync(x => x.EntityType == "PentaExecution"));
        Assert.Equal(1, await db.PentaUsageReservations.CountAsync());
        Assert.Equal(1, await db.PentaUsageEntries.CountAsync());
    }

    [Fact]
    public async Task Unknown_outcome_holds_quota_and_matching_retry_never_dispatches()
    {
        var provider = new FakeProvider { Throw = true };
        using var factory = new Factory(true, provider, userLimit: 1, academyLimit: 2);
        using var client = factory.CreateClient(new() { AllowAutoRedirect = false });
        var academy = Guid.NewGuid();
        await SeedAsync(factory, academy, Guid.NewGuid());
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await LoginAsync(client, "admin-a"));
        var path = $"/api/academies/{academy}/penta/turns";
        async Task<HttpResponseMessage> Post(string key)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, path)
            {
                Content = JsonContent.Create(new { text = "synthetic", capability = "pulse" })
            };
            request.Headers.Add("Idempotency-Key", key);
            return await client.SendAsync(request);
        }
        using var first = await Post("unknown-one");
        using var replay = await Post("unknown-one");
        using var denied = await Post("unknown-two");
        Assert.Equal(HttpStatusCode.BadGateway, first.StatusCode);
        Assert.Equal(HttpStatusCode.BadGateway, replay.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, denied.StatusCode);
        Assert.Equal(1, provider.Calls);
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
        var reservation = await db.PentaUsageReservations.SingleAsync();
        Assert.Equal("UsageUnknown", reservation.Status);
        Assert.Equal(1, reservation.UnitsReserved);
        Assert.Empty(await db.PentaUsageEntries.ToListAsync());
        Assert.Equal(1, await db.PentaAttempts.CountAsync());
    }

    [Theory]
    [InlineData("0")]
    [InlineData("not-a-number")]
    public async Task Invalid_budget_configuration_denies_new_claim_before_dispatch(string invalidLimit)
    {
        var provider = new FakeProvider();
        using var factory = new Factory(true, provider, userLimitText: invalidLimit);
        using var client = factory.CreateClient(new() { AllowAutoRedirect = false });
        var academy = Guid.NewGuid();
        await SeedAsync(factory, academy, Guid.NewGuid());
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await LoginAsync(client, "admin-a"));
        using var denied = await client.PostAsJsonAsync($"/api/academies/{academy}/penta/turns",
            new { text = "synthetic", capability = "pulse" });
        Assert.Equal(HttpStatusCode.ServiceUnavailable, denied.StatusCode);
        Assert.Equal(0, provider.Calls);
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
        Assert.Empty(await db.PentaExecutions.ToListAsync());
        Assert.Empty(await db.PentaUsageReservations.ToListAsync());
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
