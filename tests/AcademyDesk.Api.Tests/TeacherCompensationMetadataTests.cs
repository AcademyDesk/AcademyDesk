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
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AcademyDesk.Api.Tests;

public sealed class TeacherCompensationMetadataTests
{
    public static IEnumerable<object?[]> EmptyMetadata()
    {
        foreach (var json in new string?[]
        {
            null, "", "  ", "null", "[]", "[{}]", "\"text\"", "\"\"", "0", "1.25",
            "true", "false", "{", "{\"model\":", "{}"
        }) yield return [json];
    }

    [Theory, MemberData(nameof(EmptyMetadata))]
    public async Task Empty_malformed_and_non_object_metadata_reads_as_unconfigured_without_writes(string? json)
    {
        await using var db = CreateDb();
        var teacher = NewTeacher(Guid.NewGuid(), json);
        db.Teachers.Add(teacher);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var response = await new TeacherCompensationController(db).Get(teacher.AcademyId, teacher.Id, default);

        var summary = Assert.IsType<TeacherCompensationSummary>(Assert.IsType<OkObjectResult>(response.Result).Value);
        Assert.Equal(new TeacherCompensationSummary(teacher.Id, "", null, null, null, null, null, null), summary);
        Assert.Empty(db.ChangeTracker.Entries());
        Assert.Equal(json, (await db.Teachers.AsNoTracking().SingleAsync()).CompensationJson);
        Assert.Empty(await db.AuditLogs.ToListAsync());
    }

    [Theory]
    [InlineData("{\"model\":\"Monthly\",\"monthlySalary\":0,\"effectiveFrom\":\"2026-11-07\"}", "Monthly", 0)]
    [InlineData("{\"MODEL\":\"Monthly\",\"BASEMONTHLYSALARY\":1250.50,\"effectiveFrom\":\"2026-11-07\"}", "Monthly", 1250.50)]
    [InlineData("{\"model\":\"Monthly\",\"baseMonthlySalary\":\"1250\",\"effectiveFrom\":\"2026-11-07\"}", "Monthly", 1250)]
    public async Task Monthly_legacy_aliases_case_and_zero_are_preserved(string json, string model, double salary)
    {
        var result = await Read(json);
        Assert.Equal(model, result.Model);
        Assert.Equal((decimal)salary, result.MonthlySalary);
        Assert.Null(result.StandardHourlyRate);
        Assert.Equal(new DateOnly(2026, 11, 7), result.EffectiveFrom);
    }

    [Theory]
    [InlineData("standardHourlyRate")]
    [InlineData("baseHourlyRate")]
    [InlineData("BASEHOURLYRATE")]
    public async Task Hourly_aliases_keep_each_decimal_zero_null_and_effective_date(string key)
    {
        var result = await Read($$"""{"model":"Hourly","{{key}}":125.50,"beginnerHourlyRate":0,"intermediateHourlyRate":null,"advancedHourlyRate":250.75,"effectiveFrom":"2026-11-07"}""");
        Assert.Equal("Hourly", result.Model);
        Assert.Null(result.MonthlySalary);
        Assert.Equal(125.50m, result.StandardHourlyRate);
        Assert.Equal(0m, result.BeginnerHourlyRate);
        Assert.Null(result.IntermediateHourlyRate);
        Assert.Equal(250.75m, result.AdvancedHourlyRate);
        Assert.Equal(new DateOnly(2026, 11, 7), result.EffectiveFrom);
    }

    [Theory]
    [InlineData("null")]
    [InlineData("true")]
    [InlineData("[]")]
    [InlineData("{}")]
    [InlineData("\"not-a-rate-or-date\"")]
    [InlineData("1e100")]
    public async Task Invalid_optional_values_do_not_hide_valid_sibling_fields(string value)
    {
        var result = await Read($$"""{"model":"Hourly","standardHourlyRate":{{value}},"beginnerHourlyRate":0,"advancedHourlyRate":45.25,"effectiveFrom":{{value}}}""");
        Assert.Equal("Hourly", result.Model);
        Assert.Null(result.StandardHourlyRate);
        Assert.Equal(0m, result.BeginnerHourlyRate);
        Assert.Equal(45.25m, result.AdvancedHourlyRate);
        Assert.Null(result.EffectiveFrom);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Foreign_or_missing_teacher_still_returns_404_without_exposing_metadata(bool foreign)
    {
        await using var db = CreateDb();
        var teacher = NewTeacher(Guid.NewGuid(), "{\"model\":\"Monthly\",\"monthlySalary\":9000}");
        db.Teachers.Add(teacher);
        await db.SaveChangesAsync();
        var response = await new TeacherCompensationController(db).Get(
            foreign ? Guid.NewGuid() : teacher.AcademyId, foreign ? teacher.Id : Guid.NewGuid(), default);
        Assert.IsType<NotFoundResult>(response.Result);
        Assert.Equal(teacher.CompensationJson, (await db.Teachers.AsNoTracking().SingleAsync()).CompensationJson);
        Assert.Empty(await db.AuditLogs.ToListAsync());
    }

    [Theory]
    [InlineData("null")]
    [InlineData("[]")]
    [InlineData("\"text\"")]
    [InlineData("false")]
    [InlineData("125.5")]
    [InlineData("{")]
    [InlineData("{\"model\":\"Monthly\",\"monthlySalary\":0}")]
    public async Task Authenticated_http_get_serializes_fallback_and_preserves_current_tenant_boundary(string json)
    {
        // Real MVC, authentication and access filter; only persistence is synthetic InMemory.
        using var factory = new Factory();
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
        var academy = Guid.NewGuid();
        var teacher = NewTeacher(academy, json);
        await Seed(factory, teacher);
        var path = $"/api/academies/{academy}/teachers/{teacher.Id}/compensation";
        using (var anonymous = await client.GetAsync(path)) Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
        using (var login = await client.PostAsJsonAsync("/api/auth/login", new { email = "metadata-admin@example.invalid", password = "Synthetic!39Ab" }))
        {
            Assert.Equal(HttpStatusCode.OK, login.StatusCode);
            using var token = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.RootElement.GetProperty("accessToken").GetString());
        }
        using (var response = await client.GetAsync(path))
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var summary = await response.Content.ReadFromJsonAsync<TeacherCompensationSummary>();
            Assert.NotNull(summary);
            Assert.Equal(new TeacherCompensationSummary(teacher.Id, json.Contains("Monthly") ? "Monthly" : "",
                json.Contains("Monthly") ? 0m : null, null, null, null, null, null), summary);
        }
        using (var denied = await client.GetAsync($"/api/academies/{Guid.NewGuid()}/teachers/{teacher.Id}/compensation"))
            Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        using (var missing = await client.GetAsync($"/api/academies/{academy}/teachers/{Guid.NewGuid()}/compensation"))
            Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);

        await using var verify = factory.Services.CreateAsyncScope();
        var db = verify.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
        Assert.Equal(json, (await db.Teachers.AsNoTracking().SingleAsync()).CompensationJson);
        Assert.Empty(await db.AuditLogs.ToListAsync());
    }

    private static Teacher NewTeacher(Guid academy, string? json) => new()
    { AcademyId = academy, FirstName = "Synthetic", LastName = "Metadata", CompensationJson = json };
    private static AcademyDeskDbContext CreateDb() => new(new DbContextOptionsBuilder<AcademyDeskDbContext>()
        .UseInMemoryDatabase($"compensation-metadata-{Guid.NewGuid():N}").Options);
    private static async Task<TeacherCompensationSummary> Read(string json)
    {
        await using var db = CreateDb();
        var teacher = NewTeacher(Guid.NewGuid(), json);
        db.Add(teacher);
        await db.SaveChangesAsync();
        var result = await new TeacherCompensationController(db).Get(teacher.AcademyId, teacher.Id, default);
        return Assert.IsType<TeacherCompensationSummary>(Assert.IsType<OkObjectResult>(result.Result).Value);
    }
    private static async Task Seed(Factory factory, Teacher teacher)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
        db.Add(new Academy { Id = teacher.AcademyId, Name = "Synthetic metadata academy", EnabledModulesJson = "[\"Finance\"]" });
        db.Add(teacher);
        await db.SaveChangesAsync();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        Assert.True((await roles.CreateAsync(new ApplicationRole { Name = "AcademyAdmin", IsSystemRole = true })).Succeeded);
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = new ApplicationUser { UserName = "metadata-admin@example.invalid", Email = "metadata-admin@example.invalid",
            EmailConfirmed = true, DisplayName = "Synthetic metadata admin", AcademyId = teacher.AcademyId };
        Assert.True((await users.CreateAsync(user, "Synthetic!39Ab")).Succeeded);
        Assert.True((await users.AddToRoleAsync(user, "AcademyAdmin")).Succeeded);
    }
    private sealed class Factory : WebApplicationFactory<Program>
    {
        private readonly string database = $"compensation-http-{Guid.NewGuid():N}";
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
