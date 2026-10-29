using System.Security.Claims;
using System.Text.Json;
using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Domain.Identity;
using AcademyDesk.Api.Infrastructure.Meetings;
using AcademyDesk.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.DataProtection;
using System.Net;
using System.Net.Http.Headers;
using AcademyDesk.Api.Tests.Infrastructure;

namespace AcademyDesk.Api.Tests;

// Real Identity stores/role resolution over disposable InMemory contexts.
// Not actual bearer expiry, HTTP pipeline, SQL or provider acceptance.
public sealed class MeetingReadinessTests
{
    internal sealed class Fixture : IAsyncDisposable
    {
        public ServiceProvider Services { get; }
        public AcademyDeskDbContext Db { get; }
        public UserManager<ApplicationUser> Users { get; }
        public Academy Academy { get; } = new() { Name = "Synthetic meeting academy", EnabledModulesJson = "[\"Core\",\"Engagement\"]" };
        public ApplicationUser User { get; private set; } = null!;
        public ClaimsPrincipal Actor { get; private set; } = null!;
        public MeetingReadinessService Readiness { get; }
        public Fixture()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddDbContext<IdentityDbContext>(o => o.UseInMemoryDatabase("meeting-identity-" + Guid.NewGuid()));
            services.AddIdentityCore<ApplicationUser>().AddRoles<ApplicationRole>().AddEntityFrameworkStores<IdentityDbContext>();
            Services = services.BuildServiceProvider();
            Users = Services.GetRequiredService<UserManager<ApplicationUser>>();
            Db = new(new DbContextOptionsBuilder<AcademyDeskDbContext>().UseInMemoryDatabase("meeting-domain-" + Guid.NewGuid()).Options);
            Readiness = new(Db, Users, TimeProvider.System);
        }
        public static async Task<Fixture> Create(string role = "Owner")
        {
            var f = new Fixture();
            f.Db.Academies.Add(f.Academy); await f.Db.SaveChangesAsync();
            var roles = f.Services.GetRequiredService<RoleManager<ApplicationRole>>();
            Assert.True((await roles.CreateAsync(new ApplicationRole { Name = role })).Succeeded);
            f.User = new() { Id = Guid.NewGuid(), AcademyId = f.Academy.Id, UserName = "synthetic-" + Guid.NewGuid().ToString("N"), DisplayName = "Synthetic actor" };
            Assert.True((await f.Users.CreateAsync(f.User)).Succeeded);
            Assert.True((await f.Users.AddToRoleAsync(f.User, role)).Succeeded);
            f.Actor = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, f.User.Id.ToString()), new Claim(ClaimTypes.Role, "Owner"), new Claim("academyId", Guid.NewGuid().ToString()) }, "test-identity"));
            return f;
        }
        public Task<MeetingReadinessDecision> Read(Guid? academy = null) => Readiness.ReadAsync(Actor, academy ?? Academy.Id, default);
        public async ValueTask DisposeAsync() { await Db.DisposeAsync(); await Services.DisposeAsync(); }
    }

    [Theory] [InlineData("Owner")] [InlineData("AcademyAdmin")]
    public async Task Authorized_current_owner_or_admin_gets_only_unavailable_capabilities(string role)
    {
        await using var f = await Fixture.Create(role); var before = DateTimeOffset.UtcNow;
        var decision = await f.Read(); Assert.Equal(200, decision.StatusCode);
        var result = Assert.IsType<MeetingCapabilities>(decision.Capabilities);
        Assert.Equal("meeting-host/0.1", result.ContractVersion); Assert.InRange(result.AsOfUtc, before, DateTimeOffset.UtcNow);
        Assert.Equal(new[] { "GoogleWorkspace", "Zoom", "Microsoft365" }, result.Providers.Select(x => x.Provider));
        foreach (var p in result.Providers) { Assert.False(p.Implemented); Assert.False(p.Available); Assert.Equal("IntegrationUnavailable", p.ReasonCode); Assert.Equal(new MeetingOperations(false, false, false, false, false), p.Operations); Assert.Empty(p.SupportedSettings); }
        Assert.DoesNotContain(f.Db.ChangeTracker.Entries(), x => x.State != EntityState.Unchanged);
        Assert.Empty(await f.Db.AuditLogs.ToListAsync());
    }

    [Theory] [InlineData("Teacher")] [InlineData("Student")] [InlineData("Guardian")]
    [InlineData("Manager")] [InlineData("FinanceUser")] [InlineData("Operations")]
    [InlineData("PlatformOwner")]
    public async Task Claimed_owner_role_never_overrides_server_role(string role)
    { await using var f = await Fixture.Create(role); var r = await f.Read(); Assert.Equal(403, r.StatusCode); Assert.Null(r.Capabilities); }

    [Fact] public async Task Foreign_route_is_denied_without_foreign_configuration()
    { await using var f = await Fixture.Create(); var foreign = new Academy { Name = "Private foreign academy", EnabledModulesJson = "[\"Engagement\"]" }; f.Db.Add(foreign); await f.Db.SaveChangesAsync(); var r = await f.Read(foreign.Id); Assert.Equal(403, r.StatusCode); Assert.Null(r.Capabilities); }

    [Fact] public async Task Platform_owner_flag_does_not_bypass_foreign_tenant()
    { await using var f = await Fixture.Create(); f.User.IsPlatformOwner = true; await f.Users.UpdateAsync(f.User); Assert.Equal(403, (await f.Read(Guid.NewGuid())).StatusCode); }

    [Fact] public async Task Revoked_role_takes_effect_despite_stale_role_claim()
    { await using var f = await Fixture.Create(); Assert.Equal(200, (await f.Read()).StatusCode); Assert.True((await f.Users.RemoveFromRoleAsync(f.User, "Owner")).Succeeded); Assert.Equal(403, (await f.Read()).StatusCode); }

    [Theory] [InlineData("role")] [InlineData("inactive")] [InlineData("tenant")] [InlineData("deleted")]
    public async Task Separate_identity_context_changes_cannot_survive_tracked_user_or_role_rows(string mode)
    {
        await using var f = await Fixture.Create(); Assert.Equal(200, (await f.Read()).StatusCode);
        Assert.True(await f.Users.IsInRoleAsync(f.User, "Owner")); // Prime FindAsync membership tracking too.
        await using (var other = new IdentityDbContext(f.Services.GetRequiredService<DbContextOptions<IdentityDbContext>>()))
        {
            var user = await other.Users.SingleAsync(x => x.Id == f.User.Id);
            if (mode == "role") other.Remove(await other.UserRoles.SingleAsync(x => x.UserId == user.Id));
            if (mode == "inactive") user.IsActive = false;
            if (mode == "tenant") user.AcademyId = Guid.NewGuid();
            if (mode == "deleted") other.Remove(user);
            await other.SaveChangesAsync();
        }
        if (mode == "role") Assert.True(await f.Users.IsInRoleAsync(f.User, "Owner")); // Demonstrate the store's tracked membership cache.
        Assert.Equal(mode == "deleted" ? 401 : 403, (await f.Read()).StatusCode);
    }

    [Fact] public async Task Changed_server_academy_takes_effect_despite_old_context()
    { await using var f = await Fixture.Create(); f.User.AcademyId = Guid.NewGuid(); await f.Users.UpdateAsync(f.User); Assert.Equal(403, (await f.Read()).StatusCode); }

    [Fact] public async Task Inactive_user_is_denied()
    { await using var f = await Fixture.Create(); f.User.IsActive = false; await f.Users.UpdateAsync(f.User); Assert.Equal(403, (await f.Read()).StatusCode); }

    [Theory] [InlineData("[]")] [InlineData("[\"Core\"]")] [InlineData("not-json")] [InlineData("null")]
    public async Task Missing_or_invalid_entitlement_fails_closed(string modules)
    { await using var f = await Fixture.Create(); f.Academy.EnabledModulesJson = modules; await f.Db.SaveChangesAsync(); Assert.Equal(403, (await f.Read()).StatusCode); }

    [Fact] public async Task Inactive_academy_is_denied()
    { await using var f = await Fixture.Create(); f.Academy.IsActive = false; await f.Db.SaveChangesAsync(); Assert.Equal(403, (await f.Read()).StatusCode); }

    [Fact] public async Task Missing_academy_is_denied()
    { await using var f = await Fixture.Create(); f.Db.Remove(f.Academy); await f.Db.SaveChangesAsync(); Assert.Equal(403, (await f.Read()).StatusCode); }

    [Fact] public async Task Anonymous_identifier_claim_is_not_authenticated()
    { await using var f = await Fixture.Create(); var actor = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, f.User.Id.ToString()) })); var r = await f.Readiness.ReadAsync(actor, f.Academy.Id, default); Assert.Equal(401, r.StatusCode); Assert.Null(r.Capabilities); }

    [Fact] public async Task Deleted_identity_is_not_accepted_from_old_claims()
    { await using var f = await Fixture.Create(); await f.Users.DeleteAsync(f.User); Assert.Equal(401, (await f.Read()).StatusCode); }

    [Theory] [InlineData("Configured", true)] [InlineData("NotConfigured", false)] [InlineData("Disabled", true)]
    public async Task Stored_marker_or_provider_text_cannot_enable_operations_or_leak_references(string status, bool marker)
    {
        await using var f = await Fixture.Create();
        f.Db.CommunicationChannels.Add(new CommunicationChannel { AcademyId = f.Academy.Id, Channel = "Meeting", Provider = "https://untrusted.invalid/provider", Status = status, HasSecureConnection = marker, ExternalAccountReference = "private-synthetic-reference", SenderAddress = "private@example.invalid", MessagesEnabled = true });
        f.Db.CommunicationChannels.Add(new CommunicationChannel { AcademyId = Guid.NewGuid(), Channel = "Meeting", Provider = "GoogleWorkspace", Status = "Configured", HasSecureConnection = true, ExternalAccountReference = "private-foreign-reference" });
        await f.Db.SaveChangesAsync(); var r = await f.Read(); Assert.Equal(200, r.StatusCode);
        Assert.All(r.Capabilities!.Providers, p => { Assert.False(p.Available); Assert.False(p.Operations.Create); Assert.False(p.Operations.History); });
        var json = JsonSerializer.Serialize(r.Capabilities); Assert.DoesNotContain("private", json); Assert.DoesNotContain("untrusted", json); Assert.DoesNotContain("hasSecureConnection", json, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(2, await f.Db.CommunicationChannels.CountAsync()); Assert.Empty(await f.Db.AuditLogs.ToListAsync());
    }

    [Fact] public async Task Controller_maps_safe_authorized_result_and_denial()
    {
        await using var f = await Fixture.Create(); var preview = new MeetingPreviewService(f.Db, f.Users, f.Readiness, TimeProvider.System);
        var receipts = new MeetingReceiptService(f.Db, f.Users, f.Readiness, preview, new EphemeralDataProtectionProvider(), TimeProvider.System);
        var operations = new MeetingOperationService(f.Db, f.Users, f.Readiness, receipts, TimeProvider.System);
        var controller = new MeetingsController(f.Readiness, preview, receipts, operations) { ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = f.Actor } } };
        Assert.IsType<OkObjectResult>((await controller.Capabilities(f.Academy.Id, default)).Result);
        Assert.IsType<ForbidResult>((await controller.Capabilities(Guid.NewGuid(), default)).Result);
        controller.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity()); Assert.IsType<UnauthorizedResult>((await controller.Capabilities(f.Academy.Id, default)).Result);
    }

    [Fact] public void Route_is_authenticated_read_only_uncached_and_Engagement_scoped()
    {
        Assert.NotEmpty(typeof(MeetingsController).GetCustomAttributes(typeof(AuthorizeAttribute), true));
        Assert.Empty(typeof(MeetingsController).GetCustomAttributes(typeof(AllowAnonymousAttribute), true));
        Assert.Equal("Engagement", SubscriptionPlanCatalog.ModuleForController(nameof(MeetingsController)));
        Assert.Null(PermissionCatalog.RequiredFor(nameof(MeetingsController))); // Explicit server Owner/Admin check; no new grant.
        var action = typeof(MeetingsController).GetMethod(nameof(MeetingsController.Capabilities))!;
        var method = Assert.Single(action.GetCustomAttributes(typeof(HttpMethodAttribute), true).Cast<HttpMethodAttribute>()); Assert.Equal(new[] { "GET" }, method.HttpMethods);
        var cache = Assert.Single(action.GetCustomAttributes(typeof(ResponseCacheAttribute), true).Cast<ResponseCacheAttribute>()); Assert.True(cache.NoStore); Assert.Equal(ResponseCacheLocation.None, cache.Location);
        var actions = typeof(MeetingsController).GetMethods().Where(m => m.DeclaringType == typeof(MeetingsController)).ToArray();
        Assert.Equal(new[] { "Capabilities", "DraftPreview", "GetOperation", "GetReceipt", "PrepareReceipt", "RequestCreation", "ReviewReceipt" }, actions.Select(m => m.Name).Order());
        var draft = actions.Single(m => m.Name == "DraftPreview");
        var draftMethod = Assert.Single(draft.GetCustomAttributes(typeof(HttpMethodAttribute), true).Cast<HttpMethodAttribute>());
        Assert.Equal("draft-preview", draftMethod.Template); Assert.Equal(new[] { "POST" }, draftMethod.HttpMethods);
        foreach (var name in new[] { "RequestCreation", "GetOperation" })
        {
            var endpoint = actions.Single(m => m.Name == name);
            Assert.True(Assert.Single(endpoint.GetCustomAttributes(typeof(ResponseCacheAttribute), true).Cast<ResponseCacheAttribute>()).NoStore);
            var verb = Assert.Single(endpoint.GetCustomAttributes(typeof(HttpMethodAttribute), true).Cast<HttpMethodAttribute>());
            Assert.Equal(new[] { name == "RequestCreation" ? "POST" : "GET" }, verb.HttpMethods);
            Assert.Equal(name == "RequestCreation" ? null : "operations/{operationId:guid}", verb.Template);
            if (name == "RequestCreation") Assert.Equal(4096L, Assert.Single(endpoint.GetCustomAttributesData(), a => a.AttributeType == typeof(RequestSizeLimitAttribute)).ConstructorArguments.Single().Value);
        }
    }

    [Theory] [InlineData("anonymous")] [InlineData("malformed")] [InlineData("expired")]
    public async Task Actual_testing_host_rejects_unauthenticated_tickets_before_database_access(string mode)
    {
        var manifest = QaRunManifest.Create();
        try
        {
            using var factory = new QaApiFactory(manifest);
            using var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
            Assert.True(factory.PreflightPassed);
            using (var scope = factory.Services.CreateScope())
            {
                Assert.NotNull(scope.ServiceProvider.GetRequiredService<MeetingReadinessService>());
                Assert.Null(scope.ServiceProvider.GetService<MeetingOperationStore>()); // Persistence foundation is not an enabled dispatcher.
                Assert.Null(scope.ServiceProvider.GetService<MeetingOAuthAttemptStore>()); // No public OAuth initiation/callback activation yet.
                Assert.Null(scope.ServiceProvider.GetService<MeetingOAuthBrowserBridge>());
                Assert.Null(scope.ServiceProvider.GetService<MeetingOAuthRegistrationRegistry>());
                Assert.Null(scope.ServiceProvider.GetService<MeetingOAuthCodeExchange>());
                Assert.Null(scope.ServiceProvider.GetService<MeetingOAuthExchangeCoordinator>());
                Assert.Null(scope.ServiceProvider.GetService<MeetingOAuthAccountProbe>());
                Assert.Null(scope.ServiceProvider.GetService<MeetingProviderRevocation>());
                Assert.Null(scope.ServiceProvider.GetService<MeetingProviderRefresh>());
                Assert.Null(scope.ServiceProvider.GetService<MeetingGoogleIdentityVerifier>());
                Assert.Null(scope.ServiceProvider.GetService<MeetingGoogleCalendarProbe>());
                Assert.Null(scope.ServiceProvider.GetService<MeetingMicrosoftIdentityVerifier>());
                Assert.Null(scope.ServiceProvider.GetService<MeetingTeamsLicenseProbe>());
                Assert.Null(scope.ServiceProvider.GetService<MeetingZoomAccountProbe>());
            }
            if (mode == "malformed") client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "not-a-valid-ticket");
            if (mode == "expired")
            {
                var options = factory.Services.GetRequiredService<IOptionsMonitor<BearerTokenOptions>>().Get(IdentityConstants.BearerScheme);
                var ticket = new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()), new Claim(ClaimTypes.Role, "Owner") }, IdentityConstants.BearerScheme)),
                    new AuthenticationProperties { IssuedUtc = DateTimeOffset.UtcNow.AddHours(-2), ExpiresUtc = DateTimeOffset.UtcNow.AddHours(-1) }, IdentityConstants.BearerScheme);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", options.BearerTokenProtector.Protect(ticket));
            }
            using var response = await client.GetAsync($"/api/academies/{Guid.NewGuid()}/meetings/capabilities");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.DoesNotContain("IntegrationUnavailable", await response.Content.ReadAsStringAsync());
        }
        finally
        {
            if (Directory.Exists(manifest.Root)) QaRunGuard.CleanupHostFolders(manifest, root => { Directory.Delete(root, recursive: true); return true; });
        }
    }
}
