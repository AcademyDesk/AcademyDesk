using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AcademyDesk.Api.Controllers;
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

public sealed class PortalAnnouncementMetadataTests
{
    private const string Active = "{\"important\":\"true\",\"startsAtUtc\":\"2000-01-01T00:00:00Z\",\"expiresAtUtc\":\"2099-01-01T00:00:00Z\",\"audiences\":\"Both\"}";

    [Theory]
    [InlineData("Student")]
    [InlineData("Teacher")]
    public async Task Every_non_object_and_malformed_root_is_hidden_without_hiding_valid_announcements(string actor)
    {
        await using var f = await Fixture.Create(actor);
        var failures = new List<string>();
        foreach (var json in new string?[] { null, "", " ", "null", "[]", "[{}]", "\"text\"", "\"\"", "0", "1.25", "true", "false", "{", "{\"important\":", "{}" })
            await Check(f, json, false, failures);
        Assert.Empty(failures);
    }

    [Theory]
    [InlineData("Student", "important")]
    [InlineData("Teacher", "important")]
    [InlineData("Student", "startsAtUtc")]
    [InlineData("Teacher", "startsAtUtc")]
    [InlineData("Student", "expiresAtUtc")]
    [InlineData("Teacher", "expiresAtUtc")]
    [InlineData("Student", "audiences")]
    [InlineData("Teacher", "audiences")]
    public async Task Wrong_kind_fields_are_hidden_and_cannot_broaden_an_audience_or_time_window(string actor, string field)
    {
        await using var f = await Fixture.Create(actor);
        var failures = new List<string>();
        foreach (var literal in new[] { "null", "true", "false", "7", "[]", "{}", "\"not-a-date-or-audience\"" })
        {
            var values = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(Active)!;
            values[field] = JsonSerializer.Deserialize<JsonElement>(literal);
            await Check(f, JsonSerializer.Serialize(values), false, failures);
        }
        Assert.Empty(failures);
    }

    [Theory]
    [InlineData("Student")]
    [InlineData("Teacher")]
    [InlineData("AcademyAdmin")]
    [InlineData("Owner")]
    [InlineData("Guardian")]
    public async Task Valid_legacy_audiences_and_scheduled_windows_keep_their_exact_visibility(string actor)
    {
        await using var f = await Fixture.Create(actor);
        var failures = new List<string>();
        var audience = actor is "AcademyAdmin" or "Owner" ? "Admin" : actor;
        var eligible = actor != "Guardian";
        foreach (var (json, visible) in new (string, bool)[]
        {
            (Active, eligible),
            (Active.Replace("\"Both\"", "\"Student\""), audience == "Student"),
            (Active.Replace("\"Both\"", "\"Teacher\""), audience == "Teacher"),
            (Active.Replace("\"Both\"", "\"Admin\""), audience == "Admin"),
            (Active.Replace("\"Both\"", "\" Student, teacher \""), audience is "Student" or "Teacher"),
            (Active.Replace("\"Both\"", "\"bOtH\""), eligible),
            (Active.Replace(",\"audiences\":\"Both\"", ""), eligible),
            (Active.Replace("\"Both\"", "\"\""), false),
            (Active.Replace("\"true\"", "\"TRUE\""), eligible),
            (Active.Replace("\"true\"", "\"false\""), false),
            (Active.Replace("2099-01-01T00:00:00Z", "2001-01-01T00:00:00Z"), false),
            (Active.Replace("2000-01-01T00:00:00Z", "2098-01-01T00:00:00Z"), false),
            (Active.Replace("\"startsAtUtc\":\"2000-01-01T00:00:00Z\",", ""), eligible),
            (Active.Replace("2099-01-01T00:00:00Z", "2099-01-01T05:30:00+05:30"), eligible)
        }) await Check(f, json, visible, failures);
        Assert.Empty(failures);
    }

    [Theory]
    [InlineData("Student")]
    [InlineData("Teacher")]
    [InlineData("AcademyAdmin")]
    public async Task Real_notification_writer_metadata_is_readable_by_its_intended_audience(string actor)
    {
        await using var f = await Fixture.Create(actor);
        // Exercise the actual writer against the owned synthetic persistence.
        await using (var scope = f.Factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var result = await new NotificationsController(db).Create(f.Academy,
                new CreateNotificationRequest(null, "Academy", "Writer title", "Writer message", "InApp", null,
                    null, new Dictionary<string, string> { ["audiences"] = "Student,Teacher" }, true, 4), default);
            Assert.IsType<Microsoft.AspNetCore.Mvc.CreatedResult>(result.Result);
        }
        using var response = await f.Client.GetAsync("/api/portal/announcements");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var rows = await response.Content.ReadFromJsonAsync<PortalAnnouncementSummary[]>();
        Assert.NotNull(rows);
        Assert.Equal(actor is "Student" or "Teacher", rows.Any(x => x.Title == "Writer title"));
        Assert.Contains(rows, x => x.Id == f.Canary);
    }

    [Fact]
    public async Task Anonymous_and_authenticated_account_without_academy_are_denied()
    {
        await using var f = await Fixture.Create("NoAcademy");
        using var denied = await f.Client.GetAsync("/api/portal/announcements");
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
    }

    private static async Task Check(Fixture f, string? json, bool visible, List<string> failures)
    {
        string before;
        await using (var scope = f.Factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            (await db.Notifications.SingleAsync(x => x.Id == f.Subject)).VariablesJson = json;
            await db.SaveChangesAsync();
            before = await Snapshot(db);
        }
        using var response = await f.Client.GetAsync("/api/portal/announcements");
        if (response.StatusCode != HttpStatusCode.OK) failures.Add($"Metadata {json ?? "<missing>"}: expected200, actual{(int)response.StatusCode}");
        else
        {
            var rows = await response.Content.ReadFromJsonAsync<PortalAnnouncementSummary[]>();
            Assert.NotNull(rows);
            var expected = f.Actor == "Guardian" ? Array.Empty<Guid>() : visible ? new[] { f.Subject, f.Canary } : new[] { f.Canary };
            if (!expected.Order().SequenceEqual(rows.Select(x => x.Id).Order())) failures.Add($"Unexpected audience/time visibility for {json ?? "<missing>"}");
        }
        await using var verify = f.Factory.Services.CreateAsyncScope();
        var fresh = verify.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
        Assert.Equal(before, await Snapshot(fresh));
        Assert.Empty(await fresh.AuditLogs.ToListAsync());
        Assert.Empty(await fresh.NotificationReadReceipts.ToListAsync());
    }

    private static async Task<string> Snapshot(AcademyDeskDbContext db) => JsonSerializer.Serialize(await db.Notifications.AsNoTracking().OrderBy(x => x.Id).ToListAsync());

    private sealed class Fixture : IAsyncDisposable
    {
        public Factory Factory = new();
        public HttpClient Client = null!;
        public Guid Academy = Guid.NewGuid(), Subject = Guid.NewGuid(), Canary = Guid.NewGuid();
        public string Actor = "";
        public static async Task<Fixture> Create(string actor)
        {
            var f = new Fixture { Actor = actor };
            try
            {
                f.Client = f.Factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
                using (var anonymous = await f.Client.GetAsync("/api/portal/announcements")) Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
                await using (var scope = f.Factory.Services.CreateAsyncScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
                    db.Add(new Academy { Id = f.Academy, Name = "Synthetic announcements", EnabledModulesJson = "[\"Engagement\"]" });
                    Notification Row(Guid id, Guid academy, string status = "Queued", Guid? recipient = null) => new()
                    { Id = id, AcademyId = academy, RecipientType = "Academy", RecipientId = recipient, Title = "Synthetic announcement", Message = "Synthetic message", VariablesJson = Active, Status = status };
                    db.AddRange(Row(f.Subject, f.Academy), Row(f.Canary, f.Academy), Row(Guid.NewGuid(), Guid.NewGuid()),
                        Row(Guid.NewGuid(), f.Academy, "Cancelled"), Row(Guid.NewGuid(), f.Academy, recipient: Guid.NewGuid()));
                    await db.SaveChangesAsync();
                    var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                    var user = new ApplicationUser { UserName = "portal-metadata@example.invalid", Email = "portal-metadata@example.invalid",
                        EmailConfirmed = true, DisplayName = "Synthetic portal metadata", AcademyId = actor == "NoAcademy" ? null : f.Academy,
                        StudentId = actor == "Student" ? Guid.NewGuid() : null, TeacherId = actor == "Teacher" ? Guid.NewGuid() : null,
                        GuardianId = actor == "Guardian" ? Guid.NewGuid() : null };
                    Assert.True((await users.CreateAsync(user, "Synthetic!39Ab")).Succeeded);
                    if (actor is "AcademyAdmin" or "Owner")
                    {
                        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
                        Assert.True((await roles.CreateAsync(new ApplicationRole { Name = actor, IsSystemRole = true })).Succeeded);
                        Assert.True((await users.AddToRoleAsync(user, actor)).Succeeded);
                    }
                }
                using var login = await f.Client.PostAsJsonAsync("/api/auth/login", new { email = "portal-metadata@example.invalid", password = "Synthetic!39Ab" });
                Assert.Equal(HttpStatusCode.OK, login.StatusCode);
                using var token = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
                f.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.RootElement.GetProperty("accessToken").GetString());
                return f;
            }
            catch { await f.DisposeAsync(); throw; }
        }
        public async ValueTask DisposeAsync() { Client?.Dispose(); await Factory.DisposeAsync(); }
    }

    private sealed class Factory : WebApplicationFactory<Program>
    {
        private readonly string database = $"portal-metadata-{Guid.NewGuid():N}";
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:ApplyMigrationsOnStartup"] = "false", ["Bootstrap:PlatformOwnerEmail"] = "",
                ["Bootstrap:PlatformOwnerPassword"] = "", ["MediaStorage:Enabled"] = "false", ["Penta:Enabled"] = "false"
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
            });
        }
    }
}
