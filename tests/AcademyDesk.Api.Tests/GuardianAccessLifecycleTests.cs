using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
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

public sealed class GuardianAccessLifecycleTests
{
    [Theory]
    [InlineData("minor")]
    [InlineData("adult")]
    [InlineData("birthday")]
    [InlineData("unknown")]
    public async Task Explicit_revocation_clears_every_flag_at_every_age_and_denies_child_reads(string age)
    {
        using var f = new GuardianAccessFixture(); await f.Seed(age: age); await f.Login("admin");
        using var response = await f.Client.PatchAsJsonAsync(f.AccessPath, new { allowPortalAccess = false }); Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        foreach (var key in new[] { "canAccessPortal", "canViewAcademicProgress", "canViewFinance", "canViewDocuments", "canManageLeave" }) Assert.False(json.RootElement.GetProperty(key).GetBoolean());
        await using (var scope = f.Factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); var link = await db.StudentGuardians.SingleAsync(x => x.StudentId == f.Student && x.GuardianId == f.Guardian);
            Assert.False(link.CanAccessPortal); Assert.Null(link.AccessGrantedAtUtc); Assert.NotNull(link.AccessRevokedAtUtc); Assert.Single(await db.AuditLogs.Where(x => x.ActorUserId == f.Admin).ToListAsync());
        }
        await f.Login("guardian");
        foreach (var path in new[] { f.ChildPath, f.ChildPath + $"/invoices/{f.Invoice}/download", f.ChildPath + "/certificates/C29/download", f.ChildPath + "/leave-requests" })
        { using var denied = await f.Client.GetAsync(path); Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode); }
        using var leave = await f.Client.PostAsJsonAsync(f.ChildPath + "/leave-requests", new { startDate = "2026-12-01", endDate = "2026-12-02", reason = "Synthetic request" }); Assert.Equal(HttpStatusCode.Forbidden, leave.StatusCode);
        using var practice = await f.Client.PostAsJsonAsync(f.ChildPath + "/practice-logs", new { practiceDate = "2026-11-10", minutesPracticed = 5, focusArea = "Synthetic" }); Assert.Equal(HttpStatusCode.Forbidden, practice.StatusCode);
    }

    [Fact]
    public async Task Revocation_is_per_link_and_preserves_other_guardians_siblings_and_student_access()
    {
        using var f = new GuardianAccessFixture(); await f.Seed(); await f.Login("admin"); using var revoke = await f.Client.PatchAsJsonAsync(f.AccessPath, new { allowPortalAccess = false }); Assert.Equal(HttpStatusCode.OK, revoke.StatusCode);
        await f.Login("guardian"); var me = await f.Read("/api/portal/me"); var children = me.GetProperty("children").EnumerateArray().Select(x => x.GetProperty("id").GetGuid()).ToArray(); Assert.DoesNotContain(f.Student, children); Assert.Contains(f.Sibling, children);
        using var sibling = await f.Client.GetAsync($"/api/portal/students/{f.Sibling}"); Assert.Equal(HttpStatusCode.OK, sibling.StatusCode);
        await f.Login("other"); using var other = await f.Client.GetAsync(f.ChildPath); Assert.Equal(HttpStatusCode.OK, other.StatusCode);
        await f.Login("student"); using var self = await f.Client.GetAsync(f.ChildPath); Assert.Equal(HttpStatusCode.OK, self.StatusCode);
    }

    [Theory]
    [InlineData("Owner")]
    [InlineData("AcademyAdmin")]
    [InlineData("FrontDesk")]
    [InlineData("PlatformOwner")]
    public async Task Authorized_revocation_and_explicit_regrant_preserve_subpermission_choices(string role)
    {
        using var f = new GuardianAccessFixture(); await f.Seed(adminRole: role); await f.Login("admin");
        using var revoke = await f.Client.PatchAsJsonAsync(f.AccessPath, new { allowPortalAccess = false }); Assert.Equal(HttpStatusCode.OK, revoke.StatusCode);
        using var grant = await f.Client.PatchAsJsonAsync(f.AccessPath, new { allowPortalAccess = true, allowAcademicProgress = false, allowFinance = true, allowDocuments = false, allowLeave = false }); Assert.Equal(HttpStatusCode.OK, grant.StatusCode);
        await f.Login("guardian"); var details = await f.Read(f.ChildPath); Assert.True(details.GetProperty("canViewFinance").GetBoolean());
        using var invoice = await f.Client.GetAsync(f.ChildPath + $"/invoices/{f.Invoice}/download"); Assert.Equal(HttpStatusCode.OK, invoice.StatusCode);
        using var certificate = await f.Client.GetAsync(f.ChildPath + "/certificates/C29/download"); Assert.Equal(HttpStatusCode.Forbidden, certificate.StatusCode);
        using var leave = await f.Client.GetAsync(f.ChildPath + "/leave-requests"); Assert.Equal(HttpStatusCode.Forbidden, leave.StatusCode);
        using var practice = await f.Client.PostAsJsonAsync(f.ChildPath + "/practice-logs", new { practiceDate = "2026-11-10", minutesPracticed = 5, focusArea = "Synthetic" }); Assert.Equal(HttpStatusCode.Forbidden, practice.StatusCode);
        await using var scope = f.Factory.Services.CreateAsyncScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); Assert.Equal(2, await db.AuditLogs.CountAsync(x => x.ActorUserId == f.Admin));
    }

    [Theory]
    [InlineData("me")]
    [InlineData("children")]
    [InlineData("student")]
    [InlineData("invoice")]
    [InlineData("certificate")]
    [InlineData("leave-list")]
    [InlineData("leave-create")]
    [InlineData("practice")]
    [InlineData("profile")]
    public async Task Inactive_guardian_domain_blocks_an_otherwise_active_identity(string route)
    {
        using var f = new GuardianAccessFixture(); await f.Seed(); await f.Change(db => db.Guardians.Single(x => x.Id == f.Guardian).IsActive = false); await f.Login("guardian"); var before = await f.Snapshot();
        using var response = route switch
        {
            "me" => await f.Client.GetAsync("/api/portal/me"),
            "children" => await f.Client.GetAsync($"/api/portal/guardians/{f.Guardian}/children"),
            "student" => await f.Client.GetAsync(f.ChildPath),
            "invoice" => await f.Client.GetAsync(f.ChildPath + $"/invoices/{f.Invoice}/download"),
            "certificate" => await f.Client.GetAsync(f.ChildPath + "/certificates/C29/download"),
            "leave-list" => await f.Client.GetAsync(f.ChildPath + "/leave-requests"),
            "leave-create" => await f.Client.PostAsJsonAsync(f.ChildPath + "/leave-requests", new { startDate = "2026-12-01", endDate = "2026-12-02", reason = "Synthetic" }),
            "practice" => await f.Client.PostAsJsonAsync(f.ChildPath + "/practice-logs", new { practiceDate = "2026-11-10", minutesPracticed = 5, focusArea = "Synthetic" }),
            _ => await f.Client.PutAsJsonAsync($"/api/portal/guardians/{f.Guardian}/profile", new { email = "changed@example.invalid", phone = "+0000000" })
        };
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode); Assert.Equal(before, await f.Snapshot());
    }

    [Fact]
    public async Task Current_domain_deactivation_and_reactivation_apply_to_the_same_bearer()
    {
        using var f = new GuardianAccessFixture(); await f.Seed(); await f.Login("guardian"); await f.Read(f.ChildPath);
        await f.Change(db => db.Guardians.Single(x => x.Id == f.Guardian).IsActive = false); using var denied = await f.Client.GetAsync(f.ChildPath); Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        await f.Change(db => db.Guardians.Single(x => x.Id == f.Guardian).IsActive = true); using var restored = await f.Client.GetAsync(f.ChildPath); Assert.Equal(HttpStatusCode.OK, restored.StatusCode);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"allowPortalAccess\":null}")]
    [InlineData("{\"allowPortalAccess\":\"false\"}")]
    public async Task Missing_or_malformed_access_intent_cannot_mutate_a_link(string json)
    {
        using var f = new GuardianAccessFixture(); await f.Seed(); await f.Login("admin"); var before = await f.Snapshot();
        using var response = await f.Client.PatchAsync(f.AccessPath, new StringContent(json, System.Text.Encoding.UTF8, "application/json")); Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode); Assert.Equal(before, await f.Snapshot());
    }

    [Theory]
    [InlineData("guardian")]
    [InlineData("student")]
    public async Task Inactive_domain_cannot_be_regranted_but_can_always_be_revoked(string kind)
    {
        using var f = new GuardianAccessFixture(); await f.Seed(); await f.Change(db => { if (kind == "guardian") db.Guardians.Single(x => x.Id == f.Guardian).IsActive = false; else db.Students.Single(x => x.Id == f.Student).IsActive = false; }); await f.Login("admin"); var before = await f.Snapshot();
        using var grant = await f.Client.PatchAsJsonAsync(f.AccessPath, new { allowPortalAccess = true }); Assert.Equal(HttpStatusCode.Conflict, grant.StatusCode); Assert.Equal(before, await f.Snapshot());
        using var revoke = await f.Client.PatchAsJsonAsync(f.AccessPath, new { allowPortalAccess = false }); Assert.Equal(HttpStatusCode.OK, revoke.StatusCode);
    }

    [Theory]
    [InlineData("Teacher")]
    [InlineData("FinanceUser")]
    public async Task Unrelated_staff_cannot_mutate_guardian_access(string role)
    {
        using var f = new GuardianAccessFixture(); await f.Seed(adminRole: role); await f.Login("admin"); var before = await f.Snapshot(); using var response = await f.Client.PatchAsJsonAsync(f.AccessPath, new { allowPortalAccess = false }); Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode); Assert.Equal(before, await f.Snapshot());
    }

    [Fact]
    public async Task Foreign_route_or_guardian_cannot_mutate_the_selected_link()
    {
        using var f = new GuardianAccessFixture(); await f.Seed(); await f.Login("admin"); var before = await f.Snapshot();
        using var route = await f.Client.PatchAsJsonAsync(f.AccessPath.Replace(f.Academy.ToString(), f.Foreign.ToString()), new { allowPortalAccess = false }); Assert.Equal(HttpStatusCode.Forbidden, route.StatusCode);
        using var record = await f.Client.PatchAsJsonAsync(f.AccessPath.Replace(f.Guardian.ToString(), Guid.NewGuid().ToString()), new { allowPortalAccess = false }); Assert.Equal(HttpStatusCode.NotFound, record.StatusCode); Assert.Equal(before, await f.Snapshot());
    }

    [Fact]
    public async Task Initial_minor_link_default_is_preserved_while_later_explicit_revocation_is_effective()
    {
        using var f = new GuardianAccessFixture(); await f.Seed(); await f.Change(db => db.StudentGuardians.Remove(db.StudentGuardians.Single(x => x.StudentId == f.Student && x.GuardianId == f.Guardian))); await f.Login("admin");
        using var created = await f.Client.PostAsJsonAsync(f.AccessPath[..f.AccessPath.LastIndexOf('/')] .Replace("/" + f.Guardian, ""), new { guardianId = f.Guardian, relationship = "Parent", isPrimary = false }); Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        using var initial = JsonDocument.Parse(await created.Content.ReadAsStringAsync()); Assert.True(initial.RootElement.GetProperty("canAccessPortal").GetBoolean());
        using var revoke = await f.Client.PatchAsJsonAsync(f.AccessPath, new { allowPortalAccess = false }); Assert.Equal(HttpStatusCode.OK, revoke.StatusCode); using var j = JsonDocument.Parse(await revoke.Content.ReadAsStringAsync()); Assert.False(j.RootElement.GetProperty("canAccessPortal").GetBoolean());
    }

    [Theory]
    [InlineData("student")]
    [InlineData("guardian")]
    public async Task Legacy_cross_academy_relationships_cannot_expose_children_or_mutate_access(string kind)
    {
        using var f = new GuardianAccessFixture(); await f.Seed();
        await f.Change(db => { if (kind == "student") db.Students.Single(x => x.Id == f.Student).AcademyId = f.Foreign; else db.Guardians.Single(x => x.Id == f.Guardian).AcademyId = f.Foreign; });
        await f.Login("guardian");
        using var child = await f.Client.GetAsync(f.ChildPath); Assert.Equal(HttpStatusCode.Forbidden, child.StatusCode);
        if (kind == "student")
        {
            var me = await f.Read("/api/portal/me"); Assert.DoesNotContain(me.GetProperty("children").EnumerateArray(), x => x.GetProperty("id").GetGuid() == f.Student);
            var children = await f.Read($"/api/portal/guardians/{f.Guardian}/children"); Assert.DoesNotContain(children.EnumerateArray(), x => x.GetProperty("id").GetGuid() == f.Student);
        }
        else
        {
            using var me = await f.Client.GetAsync("/api/portal/me"); Assert.Equal(HttpStatusCode.Forbidden, me.StatusCode);
            using var children = await f.Client.GetAsync($"/api/portal/guardians/{f.Guardian}/children"); Assert.Equal(HttpStatusCode.Forbidden, children.StatusCode);
        }
        await f.Login("admin"); var before = await f.Snapshot(); using var access = await f.Client.PatchAsJsonAsync(f.AccessPath, new { allowPortalAccess = false }); Assert.Equal(HttpStatusCode.NotFound, access.StatusCode); Assert.Equal(before, await f.Snapshot());
    }

    [Fact]
    public async Task Anonymous_callers_cannot_revoke_or_read_child_records()
    {
        using var f = new GuardianAccessFixture(); await f.Seed(); var before = await f.Snapshot();
        using var patch = await f.Client.PatchAsJsonAsync(f.AccessPath, new { allowPortalAccess = false }); Assert.Equal(HttpStatusCode.Unauthorized, patch.StatusCode);
        using var child = await f.Client.GetAsync(f.ChildPath); Assert.Equal(HttpStatusCode.Unauthorized, child.StatusCode); Assert.Equal(before, await f.Snapshot());
    }
}

internal sealed class GuardianAccessFixture : IDisposable
{
    internal GuardianHttpFactory Factory = new(); internal HttpClient Client { get; }
    internal Guid Academy = Guid.NewGuid(), Foreign = Guid.NewGuid(), Student = Guid.NewGuid(), Sibling = Guid.NewGuid(), Guardian = Guid.NewGuid(), OtherGuardian = Guid.NewGuid(), Admin = Guid.NewGuid(), Invoice = Guid.NewGuid();
    internal string AccessPath => $"/api/academies/{Academy}/students/{Student}/guardians/{Guardian}/portal-access";
    internal string ChildPath => $"/api/portal/students/{Student}";
    internal GuardianAccessFixture() => Client = Factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
    internal async Task Seed(string age = "minor", string adminRole = "AcademyAdmin")
    {
        await using var scope = Factory.Services.CreateAsyncScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); var today = DateOnly.FromDateTime(DateTime.UtcNow);
        db.AddRange(new Academy { Id = Academy, Name = "Synthetic guardian academy", EnabledModulesJson = "[\"Core\",\"Finance\",\"AcademicGovernance\"]" }, new Academy { Id = Foreign, Name = "Foreign academy" });
        db.AddRange(new Student { Id = Student, AcademyId = Academy, FirstName = "Synthetic", LastName = "Child", DateOfBirth = age == "unknown" ? null : today.AddYears(age == "minor" ? -10 : age == "birthday" ? -18 : -25) }, new Student { Id = Sibling, AcademyId = Academy, FirstName = "Synthetic", LastName = "Sibling" });
        db.AddRange(new Guardian { Id = Guardian, AcademyId = Academy, FirstName = "Synthetic", LastName = "Parent" }, new Guardian { Id = OtherGuardian, AcademyId = Academy, FirstName = "Other", LastName = "Parent" });
        foreach (var pair in new[] { (Student, Guardian), (Student, OtherGuardian), (Sibling, Guardian) }) db.Add(new StudentGuardian { AcademyId = Academy, StudentId = pair.Item1, GuardianId = pair.Item2, CanAccessPortal = true, CanViewAcademicProgress = true, CanViewFinance = true, CanViewDocuments = true, CanManageLeave = true, AccessGrantedAtUtc = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc) });
        db.Add(new Invoice { Id = Invoice, AcademyId = Academy, StudentId = Student, InvoiceNumber = "C29-INVOICE", Currency = "INR", TotalAmount = 100 }); db.Add(new Certificate { AcademyId = Academy, StudentId = Student, CertificateNumber = "C29", VerificationCode = "synthetic-c29", Title = "Synthetic certificate", Status = "Issued" }); await db.SaveChangesAsync();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>(); var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        foreach (var name in new[] { adminRole, "Guardian", "Student" }.Distinct()) Assert.True((await roles.CreateAsync(new ApplicationRole { Name = name, IsSystemRole = true })).Succeeded);
        foreach (var actor in new[] { "admin", "guardian", "other", "student" })
        {
            var user = new ApplicationUser { Id = actor == "admin" ? Admin : Guid.NewGuid(), UserName = actor + "@example.invalid", Email = actor + "@example.invalid", EmailConfirmed = true, DisplayName = "Synthetic " + actor, AcademyId = Academy, GuardianId = actor == "guardian" ? Guardian : actor == "other" ? OtherGuardian : null, StudentId = actor == "student" ? Student : null, IsPlatformOwner = actor == "admin" && adminRole == "PlatformOwner" };
            Assert.True((await users.CreateAsync(user, "Synthetic!39Ab")).Succeeded); Assert.True((await users.AddToRoleAsync(user, actor == "admin" ? adminRole : actor == "student" ? "Student" : "Guardian")).Succeeded);
        }
    }
    internal async Task Login(string actor) { Client.DefaultRequestHeaders.Authorization = null; using var r = await Client.PostAsJsonAsync("/api/auth/login", new { email = actor + "@example.invalid", password = "Synthetic!39Ab" }); Assert.Equal(HttpStatusCode.OK, r.StatusCode); using var j = JsonDocument.Parse(await r.Content.ReadAsStringAsync()); Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", j.RootElement.GetProperty("accessToken").GetString()); }
    internal async Task Change(Action<AcademyDeskDbContext> change) { await using var scope = Factory.Services.CreateAsyncScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); change(db); await db.SaveChangesAsync(); }
    internal async Task<JsonElement> Read(string path) { using var r = await Client.GetAsync(path); Assert.Equal(HttpStatusCode.OK, r.StatusCode); using var j = JsonDocument.Parse(await r.Content.ReadAsStringAsync()); return j.RootElement.Clone(); }
    internal async Task<string> Snapshot() { await using var scope = Factory.Services.CreateAsyncScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); var identity = scope.ServiceProvider.GetRequiredService<IdentityDbContext>(); return JsonSerializer.Serialize(new { guardians = await db.Guardians.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), links = await db.StudentGuardians.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), students = await db.Students.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), leaves = await db.LeaveRequests.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), practice = await db.PracticeLogs.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), audits = await db.AuditLogs.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), users = await identity.Users.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), roles = await identity.Roles.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), memberships = await identity.UserRoles.AsNoTracking().OrderBy(x => x.UserId).ThenBy(x => x.RoleId).ToListAsync() }); }
    public void Dispose() { Client.Dispose(); Factory.Dispose(); }
}

internal sealed class GuardianHttpFactory : WebApplicationFactory<Program>
{
    private readonly string database = $"guardian-access-{Guid.NewGuid():N}";
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing"); builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?> { ["Database:ApplyMigrationsOnStartup"] = "false", ["Bootstrap:PlatformOwnerEmail"] = "", ["Bootstrap:PlatformOwnerPassword"] = "", ["MediaStorage:Enabled"] = "false", ["Penta:Enabled"] = "false" }));
        builder.ConfigureServices(services => { services.RemoveAll<DbContextOptions<AcademyDeskDbContext>>(); services.RemoveAll<DbContextOptions<IdentityDbContext>>(); services.RemoveAll<IDbContextOptionsConfiguration<AcademyDeskDbContext>>(); services.RemoveAll<IDbContextOptionsConfiguration<IdentityDbContext>>(); services.RemoveAll<AcademyDeskDbContext>(); services.RemoveAll<IdentityDbContext>(); services.AddDbContext<AcademyDeskDbContext>(o => o.UseInMemoryDatabase(database + "-domain")); services.AddDbContext<IdentityDbContext>(o => o.UseInMemoryDatabase(database + "-identity")); });
    }
}
