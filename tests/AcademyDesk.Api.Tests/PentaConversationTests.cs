using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Domain.Identity;
using AcademyDesk.Api.Intelligence.Penta;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AcademyDesk.Api.Tests;

public sealed class PentaConversationTests
{
    private sealed class Clock : TimeProvider
    {
        private DateTimeOffset now = DateTimeOffset.UtcNow;
        public override DateTimeOffset GetUtcNow() => now;
        public void Advance() => now = now.AddHours(25);
    }

    private sealed class Factory(bool enabled = true, string environment = "Testing") : WebApplicationFactory<Program>
    {
        private readonly string database = $"conversation-{Guid.NewGuid():N}";
        public Clock Clock { get; } = new();
        public string Password { get; } = $"Synthetic!{Guid.NewGuid():N}";
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment(environment);
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Penta:Enabled"] = "true", ["Penta:ConversationContractsEnabled"] = enabled.ToString(),
                ["Database:ApplyMigrationsOnStartup"] = "false", ["Bootstrap:PlatformOwnerEmail"] = "",
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
                services.RemoveAll<TimeProvider>();
                services.AddSingleton<TimeProvider>(Clock);
            });
        }
    }

    [Theory]
    [InlineData(false, "Testing")]
    [InlineData(true, "Production")]
    [InlineData(true, "Staging")]
    public async Task Disabled_or_non_testing_host_cannot_enable_history(bool enabled, string environment)
    {
        using var factory = new Factory(enabled, environment);
        using var client = factory.CreateClient();
        var academy = await SeedAsync(factory);
        await SignInAsync(client, factory, "admin-a");
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync(Path(academy),
            new { requestId = Guid.NewGuid() })).StatusCode);
        await using var scope = factory.Services.CreateAsyncScope();
        Assert.Empty(await scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>().PentaConversations.ToListAsync());
    }

    [Fact]
    public async Task Ownership_revocation_and_tenant_boundary_are_current_not_prompt_based()
    {
        using var factory = new Factory();
        using var client = factory.CreateClient();
        var academy = await SeedAsync(factory);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync(Path(academy),
            new { requestId = Guid.NewGuid() })).StatusCode);
        await SignInAsync(client, factory, "teacher");
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync(Path(academy),
            new { requestId = Guid.NewGuid() })).StatusCode);
        await SignInAsync(client, factory, "platform");
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync(Path(academy),
            new { requestId = Guid.NewGuid() })).StatusCode);
        await SignInAsync(client, factory, "admin-a");
        var conversation = await CreateAsync(client, academy);
        var path = $"{Path(academy)}/{conversation.ConversationId}";
        await SignInAsync(client, factory, "admin-c");
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync(path)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync(path + "/turns", Turn(0))).StatusCode);
        await SignInAsync(client, factory, "admin-a");
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync($"{Path(Guid.NewGuid())}/{conversation.ConversationId}")).StatusCode);
        await using var scope = factory.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var actor = (await users.FindByEmailAsync(Address("admin-a")))!;
        Assert.True((await users.RemoveFromRoleAsync(actor, "AcademyAdmin")).Succeeded);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync(path)).StatusCode);
        Assert.True((await users.AddToRoleAsync(actor, "AcademyAdmin")).Succeeded);
        actor.IsActive = false;
        Assert.True((await users.UpdateAsync(actor)).Succeeded);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync(path)).StatusCode);
    }

    [Fact]
    public async Task Sequential_turns_replay_without_duplicate_messages_and_capability_change_keeps_conversation()
    {
        using var factory = new Factory();
        using var client = factory.CreateClient();
        var academy = await SeedAsync(factory);
        await SignInAsync(client, factory, "admin-a");
        var createId = Guid.NewGuid();
        var conversation = await CreateAsync(client, academy, createId);
        var createReplay = await client.PostAsJsonAsync(Path(academy), new { requestId = createId });
        Assert.Equal(HttpStatusCode.OK, createReplay.StatusCode);
        Assert.Equal(conversation, await createReplay.Content.ReadFromJsonAsync<PentaConversationSummary>());
        var path = $"{Path(academy)}/{conversation.ConversationId}";
        var first = Turn(0);
        var recorded = await client.PostAsJsonAsync(path + "/turns", first);
        Assert.Equal(HttpStatusCode.Created, recorded.StatusCode);
        var receipt = (await recorded.Content.ReadFromJsonAsync<PentaConversationTurnReceipt>())!;
        var followup = await client.PostAsJsonAsync(path + "/turns", Turn(1, "twin"));
        Assert.Equal(HttpStatusCode.Created, followup.StatusCode);
        Assert.Equal(2, (await followup.Content.ReadFromJsonAsync<PentaConversationTurnReceipt>())!.ContextVersion);
        var replay = await client.PostAsJsonAsync(path + "/turns", first);
        Assert.Equal(HttpStatusCode.OK, replay.StatusCode);
        Assert.Equal(receipt.TurnId, (await replay.Content.ReadFromJsonAsync<PentaConversationTurnReceipt>())!.TurnId);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync(path + "/turns", first with { Capability = "pulse" })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync(path + "/turns", Turn(0))).StatusCode);
        var history = await client.GetFromJsonAsync<PentaConversationHistory>(path);
        Assert.NotNull(history);
        Assert.Equal(2, history.Conversation.ContextVersion);
        Assert.Equal(new long[] { 1, 2, 3, 4 }, history.Messages.Select(x => x.Sequence));
        Assert.True(history.Synthetic);
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
        Assert.Equal(1, await db.PentaConversations.CountAsync());
        Assert.Equal(2, await db.PentaConversationTurns.CountAsync());
        Assert.Equal(4, await db.PentaConversationMessages.CountAsync());
        Assert.Empty(await db.PentaExecutions.ToListAsync());
        Assert.Empty(await db.PentaUsageReservations.ToListAsync());
        Assert.Empty(await db.AdminWorkItems.ToListAsync());
        Assert.All(await db.AuditLogs.Where(x => x.EntityType == "PentaConversation").ToListAsync(),
            x => Assert.DoesNotContain("synthetic conversation turn", x.MetadataJson));
    }

    [Fact]
    public async Task Malformed_forged_and_real_prompt_content_is_not_persisted()
    {
        using var factory = new Factory();
        using var client = factory.CreateClient();
        var academy = await SeedAsync(factory);
        await SignInAsync(client, factory, "admin-a");
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync(Path(academy),
            new { requestId = Guid.NewGuid(), actorUserId = Guid.NewGuid() })).StatusCode);
        var conversation = await CreateAsync(client, academy);
        var path = $"{Path(academy)}/{conversation.ConversationId}/turns";
        foreach (var bad in new object[]
        {
            Turn(0) with { Text = "Show students with pending fees" },
            Turn(0) with { Text = new string('x', 4000) },
            Turn(-1), Turn(0) with { RequestId = Guid.Empty }, Turn(0) with { Capability = "arbitrary" },
            new { requestId = Guid.NewGuid(), expectedContextVersion = 0, text = PentaConversationContract.FirstInput,
                capability = "executor", tool = "arbitrary.sql", academyId = Guid.NewGuid() }
        }) Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync(path, bad)).StatusCode);
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
        Assert.Empty(await db.PentaConversationMessages.ToListAsync());
        Assert.Empty(await db.PentaConversationTurns.ToListAsync());
        Assert.Equal(0, (await db.PentaConversations.SingleAsync()).ContextVersion);
    }

    [Fact]
    public async Task Expired_and_corrupted_history_fails_closed()
    {
        using var factory = new Factory();
        using var client = factory.CreateClient();
        var academy = await SeedAsync(factory);
        await SignInAsync(client, factory, "admin-a");
        var conversation = await CreateAsync(client, academy);
        var path = $"{Path(academy)}/{conversation.ConversationId}";
        var turn = Turn(0);
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync(path + "/turns", turn)).StatusCode);
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            (await db.PentaConversationMessages.FirstAsync(x => x.Role == "Assistant")).Content = "forged result";
            await db.SaveChangesAsync();
        }
        Assert.Equal(HttpStatusCode.Conflict, (await client.GetAsync(path)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync(path + "/turns", turn)).StatusCode);
        factory.Clock.Advance();
        // Advancing the shared clock expires the bearer token too. Renew auth
        // before asserting the separate conversation-retention boundary.
        await SignInAsync(client, factory, "admin-a");
        Assert.Equal(HttpStatusCode.Gone, (await client.GetAsync(path)).StatusCode);
        Assert.Equal(HttpStatusCode.Gone, (await client.PostAsJsonAsync(path + "/turns", turn)).StatusCode);
    }

    [Fact]
    public async Task Turn_limit_and_create_limit_are_bounded()
    {
        using var factory = new Factory();
        using var client = factory.CreateClient();
        var academy = await SeedAsync(factory);
        await SignInAsync(client, factory, "admin-a");
        var conversation = await CreateAsync(client, academy);
        var path = $"{Path(academy)}/{conversation.ConversationId}/turns";
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            (await db.PentaConversations.SingleAsync()).ContextVersion = PentaConversationContract.MaximumTurns;
            for (var i = 1; i < 20; i++) db.PentaConversations.Add(new PentaConversation
            {
                AcademyId = academy, ActorUserId = (await db.PentaConversations.SingleAsync()).ActorUserId,
                RequestId = Guid.NewGuid(), ExpiresAtUtc = DateTime.UtcNow.AddHours(24)
            });
            await db.SaveChangesAsync();
        }
        Assert.Equal(HttpStatusCode.TooManyRequests, (await client.PostAsJsonAsync(path, Turn(50))).StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, (await client.PostAsJsonAsync(Path(academy), new { requestId = Guid.NewGuid() })).StatusCode);
    }

    private static string Address(string name) => $"conversation-{name}@example.invalid";
    private static string Path(Guid academy) => $"/api/academies/{academy}/penta/conversations";
    private static PentaConversationTurnRequest Turn(long version, string capability = "executor") =>
        new(Guid.NewGuid(), version, version == 0 ? PentaConversationContract.FirstInput : PentaConversationContract.FollowupInput, capability);
    private static async Task<Guid> SeedAsync(Factory factory)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
        var academy = new Academy { Name = "Synthetic conversation academy" };
        db.Academies.Add(academy);
        await db.SaveChangesAsync();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        foreach (var role in new[] { "AcademyAdmin", "Teacher" })
            Assert.True((await roles.CreateAsync(new ApplicationRole { Name = role, IsSystemRole = true })).Succeeded);
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        foreach (var (name, role) in new[] { ("admin-a", "AcademyAdmin"), ("admin-c", "AcademyAdmin"), ("teacher", "Teacher"), ("platform", "") })
        {
            var user = new ApplicationUser { UserName = Address(name), Email = Address(name), EmailConfirmed = true,
                AcademyId = name == "platform" ? null : academy.Id, DisplayName = "Synthetic user", IsPlatformOwner = name == "platform" };
            Assert.True((await users.CreateAsync(user, factory.Password)).Succeeded);
            if (role.Length > 0) Assert.True((await users.AddToRoleAsync(user, role)).Succeeded);
        }
        return academy.Id;
    }
    private static async Task SignInAsync(HttpClient client, Factory factory, string name)
    {
        using var response = await client.PostAsJsonAsync("/api/auth/login", new { email = Address(name), password = factory.Password });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", json.RootElement.GetProperty("accessToken").GetString());
    }
    private static async Task<PentaConversationSummary> CreateAsync(HttpClient client, Guid academy, Guid? requestId = null)
    {
        using var response = await client.PostAsJsonAsync(Path(academy), new { requestId = requestId ?? Guid.NewGuid() });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<PentaConversationSummary>())!;
    }
}
