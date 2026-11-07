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

public sealed class TeacherAvailabilityMetadataTests
{
    public static IEnumerable<object?[]> Cases()
    {
        foreach (var json in new string?[] { null, "", " ", "null", "[]", "{}", "0", "true", "\"text\"", "{", "[7]", "[{\"day\":7}]", "[{\"day\":\"Mon\",\"from\":42}]", "[{}]", "[{\"day\":\" \"}]", "[null]", "[null,null]" })
            yield return [json, Array.Empty<TeacherAvailabilitySummary>()];
        yield return ["[{\"day\":\"Mon\",\"from\":\"10:00\",\"to\":\"12:00\"}]", new[] { new TeacherAvailabilitySummary("Mon", "10:00", "12:00") }];
        yield return ["[null,{\"day\":\"Mon\",\"from\":\"10:00\",\"to\":\"12:00\"},null,{\"DAY\":\"Tue\",\"FROM\":\"11:00\",\"TO\":null},{\"day\":\" \"},{\"day\":null}]", new[] { new TeacherAvailabilitySummary("Mon", "10:00", "12:00"), new TeacherAvailabilitySummary("Tue", "11:00", null) }];
        yield return ["[{\"day\":\"Thu\",\"other\":true}]", new[] { new TeacherAvailabilitySummary("Thu", null, null) }];
        yield return ["[{\"day\":\"Mon\"},{\"day\":\"Mon\"}]", new[] { new TeacherAvailabilitySummary("Mon", null, null), new TeacherAvailabilitySummary("Mon", null, null) }];
    }

    [Theory, MemberData(nameof(Cases))]
    public async Task Existing_metadata_projects_safely_without_changing_either_teacher(string? json, TeacherAvailabilitySummary[] expected)
    {
        await using var db = CreateDb(); var teacher = NewTeacher(Guid.NewGuid(), json); var other = NewTeacher(Guid.NewGuid(), "[null]");
        db.AddRange(teacher, other); await db.SaveChangesAsync(); var before = await Snapshot(db); db.ChangeTracker.Clear();
        var result = await new ProfilesController(db).Teacher(teacher.AcademyId, teacher.Id, default);
        var profile = Assert.IsType<TeacherProfileSummary>(Assert.IsType<OkObjectResult>(result.Result).Value);
        Assert.Equal(expected, profile.Availability); Assert.Equal(teacher.Id, profile.Id);
        Assert.Empty(db.ChangeTracker.Entries()); Assert.Equal(before, await Snapshot(db)); Assert.Empty(await db.AuditLogs.ToListAsync());
    }

    [Theory] [InlineData("AcademyAdmin")] [InlineData("Owner")]
    public async Task Authenticated_save_then_fresh_get_keeps_valid_slots_and_exact_stored_metadata(string role)
    {
        using var factory = new Factory(); using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
        var teacher = NewTeacher(Guid.NewGuid(), "[]"); await Seed(factory, teacher, role);
        var path = $"/api/academies/{teacher.AcademyId}/teachers/{teacher.Id}/profile";
        using (var denied = await client.GetAsync(path)) Assert.Equal(HttpStatusCode.Unauthorized, denied.StatusCode);
        await Login(client); var failures = new List<string>();
        foreach (var data in Cases())
        {
            var json = (string?)data[0]; var expected = (TeacherAvailabilitySummary[])data[1]!;
            using var saved = await client.PutAsJsonAsync(path, Request(json));
            if (saved.StatusCode != HttpStatusCode.OK) failures.Add($"PUT {json ?? "<missing>"}: {(int)saved.StatusCode}");
            else Assert.Equal(expected, (await saved.Content.ReadFromJsonAsync<TeacherProfileSummary>())!.Availability);
            string snapshot;
            await using (var scope = factory.Services.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); var stored = await db.Teachers.AsNoTracking().SingleAsync();
                Assert.Equal(string.IsNullOrWhiteSpace(json) ? null : json.Trim(), stored.AvailabilityJson);
                Assert.Equal("EMP-UPDATED", stored.EmployeeCode); Assert.Equal("Preferred", stored.PreferredName);
                Assert.Equal("Street", stored.AddressLine1); Assert.Null(stored.DateOfBirth); Assert.Equal(new DateOnly(2026, 11, 1), stored.JoiningDate);
                Assert.Equal(teacher.Email, stored.Email); Assert.Equal(teacher.CompensationJson, stored.CompensationJson);
                Assert.Equal(teacher.CertificationsJson, stored.CertificationsJson); snapshot = await Snapshot(db);
            }
            using var read = await client.GetAsync(path);
            if (read.StatusCode != HttpStatusCode.OK) failures.Add($"GET {json ?? "<missing>"}: {(int)read.StatusCode}");
            else Assert.Equal(expected, (await read.Content.ReadFromJsonAsync<TeacherProfileSummary>())!.Availability);
            await using var verify = factory.Services.CreateAsyncScope(); Assert.Equal(snapshot, await Snapshot(verify.ServiceProvider.GetRequiredService<AcademyDeskDbContext>()));
        }
        using (var foreign = await client.PutAsJsonAsync($"/api/academies/{Guid.NewGuid()}/teachers/{teacher.Id}/profile", Request("[null]"))) Assert.Equal(HttpStatusCode.Forbidden, foreign.StatusCode);
        using (var missing = await client.GetAsync($"/api/academies/{teacher.AcademyId}/teachers/{Guid.NewGuid()}/profile")) Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        Assert.True(failures.Count == 0, string.Join("\n", failures));
    }

    [Fact]
    public async Task Teacher_actor_cannot_write_the_administrative_profile()
    {
        using var factory = new Factory(); using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
        var teacher = NewTeacher(Guid.NewGuid(), "[null]"); await Seed(factory, teacher, "Teacher"); await Login(client);
        string before; await using (var scope = factory.Services.CreateAsyncScope()) before = await Snapshot(scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>());
        using var denied = await client.PutAsJsonAsync($"/api/academies/{teacher.AcademyId}/teachers/{teacher.Id}/profile", Request("[]"));
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        await using var verify = factory.Services.CreateAsyncScope(); var db = verify.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
        Assert.Equal(before, await Snapshot(db)); Assert.Empty(await db.AuditLogs.ToListAsync());
    }

    [Theory] [InlineData(true)] [InlineData(false)]
    public async Task Foreign_or_missing_teacher_remains_404_without_writes(bool foreign)
    {
        await using var db = CreateDb(); var teacher = NewTeacher(Guid.NewGuid(), "[null]"); db.Add(teacher); await db.SaveChangesAsync(); var before = await Snapshot(db);
        var academy = foreign ? Guid.NewGuid() : teacher.AcademyId; var id = foreign ? teacher.Id : Guid.NewGuid(); var controller = new ProfilesController(db);
        Assert.IsType<NotFoundResult>((await controller.Teacher(academy, id, default)).Result);
        Assert.IsType<NotFoundResult>((await controller.UpdateTeacherProfile(academy, id, Request("[]"), default)).Result);
        Assert.Equal(before, await Snapshot(db)); Assert.Empty(await db.AuditLogs.ToListAsync());
    }

    private static UpdateTeacherAdminProfileRequest Request(string? json) => new(EmployeeCode: "EMP-UPDATED", PreferredName: " Preferred ", EmploymentType: "Contract", DateOfBirth: null, JoiningDate: new(2026, 11, 1), Qualifications: "Grade8", AddressLine1: " Street ", City: "City", State: null, PostalCode: "123", EmergencyContactName: " Contact ", EmergencyContactPhone: "100", AdminNotes: " Note ", Specialties: "Piano", AvailabilityJson: json);
    private static Teacher NewTeacher(Guid academy, string? json) => new() { AcademyId = academy, FirstName = "Synthetic", LastName = "Availability", Email = "synthetic-teacher@example.invalid", AvailabilityJson = json, CompensationJson = "{\"model\":\"Hourly\"}", CertificationsJson = "[\"Synthetic\"]" };
    private static AcademyDeskDbContext CreateDb() => new(new DbContextOptionsBuilder<AcademyDeskDbContext>().UseInMemoryDatabase($"teacher-availability-{Guid.NewGuid():N}").Options);
    private static async Task<string> Snapshot(AcademyDeskDbContext db) => JsonSerializer.Serialize(await db.Teachers.AsNoTracking().OrderBy(x => x.Id).ToListAsync());
    private static async Task Login(HttpClient client)
    {
        using var response = await client.PostAsJsonAsync("/api/auth/login", new { email = "availability-actor@example.invalid", password = "Synthetic!39Ab" }); Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var token = JsonDocument.Parse(await response.Content.ReadAsStringAsync()); client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.RootElement.GetProperty("accessToken").GetString());
    }
    private static async Task Seed(Factory factory, Teacher teacher, string role)
    {
        await using var scope = factory.Services.CreateAsyncScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
        db.Add(new Academy { Id = teacher.AcademyId, Name = "Synthetic availability academy" }); db.Add(teacher); await db.SaveChangesAsync();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>(); Assert.True((await roles.CreateAsync(new ApplicationRole { Name = role, IsSystemRole = true })).Succeeded);
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(); var actor = new ApplicationUser { UserName = "availability-actor@example.invalid", Email = "availability-actor@example.invalid", EmailConfirmed = true, DisplayName = "Synthetic availability actor", AcademyId = teacher.AcademyId };
        Assert.True((await users.CreateAsync(actor, "Synthetic!39Ab")).Succeeded); Assert.True((await users.AddToRoleAsync(actor, role)).Succeeded);
    }
    private sealed class Factory : WebApplicationFactory<Program>
    {
        private readonly string database = $"availability-http-{Guid.NewGuid():N}";
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
                services.RemoveAll<DbContextOptions<AcademyDeskDbContext>>(); services.RemoveAll<DbContextOptions<IdentityDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<AcademyDeskDbContext>>(); services.RemoveAll<IDbContextOptionsConfiguration<IdentityDbContext>>();
                services.RemoveAll<AcademyDeskDbContext>(); services.RemoveAll<IdentityDbContext>();
                services.AddDbContext<AcademyDeskDbContext>(options => options.UseInMemoryDatabase(database + "-domain"));
                services.AddDbContext<IdentityDbContext>(options => options.UseInMemoryDatabase(database + "-identity"));
            });
        }
    }
}
