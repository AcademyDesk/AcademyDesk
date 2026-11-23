using System.Net;
using System.Security.Claims;
using System.Text.Json;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Domain.Identity;
using AcademyDesk.Api.Intelligence.Penta;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace AcademyDesk.Api.Tests;

public sealed class PentaTaskRuntimeTests
{
    private sealed class Environment(string name) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = name;
        public string ApplicationName { get; set; } = "QA";
        public string ContentRootPath { get; set; } = "";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
    private sealed class Fixture : IDisposable
    {
        internal readonly ServiceProvider Services;
        internal readonly IdentityDbContext Identity;
        internal readonly AcademyDeskDbContext Db;
        internal readonly UserManager<ApplicationUser> Users;
        internal readonly ApplicationUser Actor;
        internal readonly Academy Academy;
        internal readonly ClaimsPrincipal Principal;
        internal Fixture(string role = "Owner", bool finance = true)
        {
            var services = new ServiceCollection().AddLogging();
            services.AddDbContext<IdentityDbContext>(options => options.UseInMemoryDatabase("task-identity-" + Guid.NewGuid()));
            services.AddDbContext<AcademyDeskDbContext>(options => options.UseInMemoryDatabase("task-domain-" + Guid.NewGuid()));
            services.AddIdentityCore<ApplicationUser>().AddRoles<ApplicationRole>().AddEntityFrameworkStores<IdentityDbContext>();
            Services = services.BuildServiceProvider(); Identity = Services.GetRequiredService<IdentityDbContext>();
            Db = Services.GetRequiredService<AcademyDeskDbContext>(); Users = Services.GetRequiredService<UserManager<ApplicationUser>>();
            Academy = new Academy { Name = "Synthetic academy", EnabledModulesJson = finance ? "[\"Finance\"]" : "[]" };
            Db.Academies.Add(Academy); Db.SaveChanges();
            Actor = new ApplicationUser { Id = Guid.NewGuid(), AcademyId = Academy.Id, UserName = "synthetic", SecurityStamp = Guid.NewGuid().ToString("N") };
            var membership = new ApplicationRole { Id = Guid.NewGuid(), Name = role, NormalizedName = role.ToUpperInvariant() };
            Identity.Users.Add(Actor); Identity.Roles.Add(membership);
            Identity.UserRoles.Add(new IdentityUserRole<Guid> { UserId = Actor.Id, RoleId = membership.Id }); Identity.SaveChanges();
            Principal = new(new ClaimsIdentity([
                new Claim(Users.Options.ClaimsIdentity.UserIdClaimType, Actor.Id.ToString("D")),
                new Claim(Users.Options.ClaimsIdentity.SecurityStampClaimType, Actor.SecurityStamp),
                // Deliberately misleading; the policy must query stored membership.
                new Claim(ClaimTypes.Role, "Owner")], "test"));
        }
        internal PentaTaskRuntimeAccess Access(string environment = "Testing", bool enabled = true) =>
            new(Identity, Db, Users, new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> {
                ["Penta:Enabled"] = "true", ["Penta:Tasks:Enabled"] = enabled.ToString() }).Build(), new Environment(environment));
        public void Dispose() => Services.Dispose();
    }

    [Theory]
    [InlineData("Owner")][InlineData("AcademyAdmin")]
    public async Task Runtime_catalog_uses_current_stored_admin_authority_and_subscription(string role)
    {
        using var fixture = new Fixture(role);
        var authority = await fixture.Access().ResolveAsync(fixture.Principal, fixture.Academy.Id, default);
        Assert.NotNull(authority); Assert.Equal(9, authority.Tools.Count);
        Assert.Equal(fixture.Actor.Id, authority.ActorId);
        fixture.Academy.EnabledModulesJson = "[]"; await fixture.Db.SaveChangesAsync();
        var changed = await fixture.Access().ResolveAsync(fixture.Principal, fixture.Academy.Id, default);
        Assert.NotNull(changed); Assert.Equal(8, changed.Tools.Count);
        Assert.DoesNotContain("AcademyDesk.Payment.Record", changed.Tools);
        Assert.Contains(PentaTeacherCreationFields.Tool, changed.Tools);
        Assert.NotEqual(authority.Version, changed.Version);
    }

    [Theory]
    [InlineData("Teacher")][InlineData("Student")][InlineData("Guardian")][InlineData("Staff")][InlineData("PlatformOwner")]
    public async Task A_role_label_or_claim_cannot_grant_admin_task_tools(string storedRole)
    {
        using var fixture = new Fixture(storedRole);
        Assert.Null(await fixture.Access().ResolveAsync(fixture.Principal, fixture.Academy.Id, default));
    }

    [Theory]
    [InlineData("actor")][InlineData("academy")][InlineData("stamp")][InlineData("membership")][InlineData("platform")][InlineData("tenant")]
    public async Task Same_principal_is_denied_after_current_state_revocation_or_scope_change(string change)
    {
        using var fixture = new Fixture(); var access = fixture.Access();
        Assert.NotNull(await access.ResolveAsync(fixture.Principal, fixture.Academy.Id, default));
        switch(change) {
            case "actor": fixture.Actor.IsActive = false; break;
            case "academy": fixture.Academy.IsActive = false; break;
            case "stamp": fixture.Actor.SecurityStamp = Guid.NewGuid().ToString("N"); break;
            case "membership": fixture.Identity.UserRoles.RemoveRange(fixture.Identity.UserRoles); break;
            case "platform": fixture.Actor.IsPlatformOwner = true; break;
            case "tenant": fixture.Actor.AcademyId = Guid.NewGuid(); break;
        }
        await fixture.Identity.SaveChangesAsync(); await fixture.Db.SaveChangesAsync();
        Assert.Null(await access.ResolveAsync(fixture.Principal, fixture.Academy.Id, default));
    }

    [Theory]
    [InlineData("Production",true)][InlineData("Staging",true)][InlineData("Testing",false)]
    public async Task Gate_is_default_off_and_excludes_production(string environment, bool enabled)
    {
        using var fixture = new Fixture(); var access = fixture.Access(environment, enabled);
        Assert.False(access.Enabled); Assert.Null(await access.ResolveAsync(fixture.Principal, fixture.Academy.Id, default));
    }

    [Fact]
    public async Task Foreign_tenant_unauthenticated_or_missing_security_stamp_is_denied()
    {
        using var fixture = new Fixture(); var access = fixture.Access();
        Assert.Null(await access.ResolveAsync(fixture.Principal, Guid.NewGuid(), default));
        Assert.Null(await access.ResolveAsync(new ClaimsPrincipal(new ClaimsIdentity()), fixture.Academy.Id, default));
        var noStamp = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, fixture.Actor.Id.ToString("D"))], "test"));
        Assert.Null(await access.ResolveAsync(noStamp, fixture.Academy.Id, default));
    }

    [Fact]
    public async Task Compact_planner_request_fits_transport_and_omits_authority_and_private_source_paths()
    {
        using var fixture = new Fixture(); var authority = (await fixture.Access().ResolveAsync(fixture.Principal, fixture.Academy.Id, default))!;
        var request = PentaTaskDialogue.Request(Guid.NewGuid(), "Create a student and batch, then enroll the student.", null, authority.Tools, new(2026,11,23));
        var json = request.GetRawText(); Assert.True(System.Text.Encoding.UTF8.GetByteCount(json) <= 16 * 1024);
        Assert.False(request.TryGetProperty("scope", out _));
        Assert.DoesNotContain(fixture.Actor.SecurityStamp!, json); Assert.DoesNotContain(fixture.Actor.Id.ToString("D"), json);
        Assert.DoesNotContain("Infrastructure/", json); Assert.DoesNotContain("TemporaryPassword", json);
        Assert.All(request.GetProperty("catalog").EnumerateArray(), tool => Assert.False(tool.GetProperty("execution_enabled").GetBoolean()));
        // Optional private interop vector for the separate Mini contract runner.
        // Metadata only: the assertions above prohibit actor/stamp/credential data.
        if (System.Environment.GetEnvironmentVariable("QA_TASK_CONTRACT_VECTOR") is { Length: > 0 } vector)
            File.WriteAllText(vector, json);
    }

    private sealed class Transport(string body, bool identity = true, string? source = null, string? prompt = null, HttpStatusCode status = HttpStatusCode.OK) : HttpMessageHandler
    {
        public int Calls; public string? Target;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            Calls++; Target = request.RequestUri!.AbsolutePath;
            var result = new HttpResponseMessage(status) { Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json") };
            if(identity) result.Headers.TryAddWithoutValidation(MiniRuntimeIdentity.HeaderName, JsonSerializer.Serialize(
                new MiniRuntimeIdentity("0.1.14","planner-0.15","0.1",new('a',64),new('b',64),new('c',64),new('d',64),new('e',64),new('f',64))));
            result.Headers.TryAddWithoutValidation("X-Penta-Task-Contract", PentaTaskDialogue.Contract);
            if(source is not null) result.Headers.TryAddWithoutValidation("X-Penta-Task-Source", source);
            if(prompt is not null) result.Headers.TryAddWithoutValidation("X-Penta-Task-Prompt", prompt);
            return Task.FromResult(result);
        }
    }
    private static PentaTaskPlannerProvider Provider(Transport transport, string url = "http://127.0.0.1:8000/", string environment = "Testing") =>
        new(new HttpClient(transport),new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?> {
            ["Penta:Enabled"]="true",["Penta:Tasks:Enabled"]="true",["Penta:Tasks:BaseUrl"]=url }).Build(),new Environment(environment));
    private static JsonElement Request => JsonSerializer.SerializeToElement(new { contract=PentaTaskDialogue.Contract });

    [Fact]
    public async Task Separate_transport_requires_per_response_task_provenance_and_never_dispatches_legacy_chat()
    {
        var transport = new Transport("{}",source:new('1',64),prompt:new('2',64));
        var result = await Provider(transport).PlanAsync(Request,default);
        Assert.NotNull(result.Proposal); Assert.Equal(new string('1',64),result.TaskSourceSha256);
        Assert.Equal("/v1/academy-tasks/plan",transport.Target); Assert.Equal(1,transport.Calls);
    }
    [Theory]
    [InlineData(false,null,null)][InlineData(true,null,null)][InlineData(true,"invalid","invalid")]
    public async Task Missing_task_source_identity_is_not_accepted(bool runtime,string? source,string? prompt)
    {
        var result=await Provider(new Transport("{}",runtime,source,prompt)).PlanAsync(Request,default);
        Assert.Null(result.Proposal); Assert.Equal("RuntimeIdentityFailed",result.Error);
    }
    [Theory]
    [InlineData("https://example.invalid/")][InlineData("http://localhost:8000/")]
    [InlineData("http://127.0.0.1:8000/path")][InlineData("http://127.0.0.1:8000/?query=1")]
    [InlineData("http://user:password@127.0.0.1:8000/")]
    public async Task Public_or_manipulated_endpoint_never_receives_context(string url)
    {
        var transport=new Transport("{}"); Assert.Null((await Provider(transport,url).PlanAsync(Request,default)).Proposal);
        Assert.Equal(0,transport.Calls);
    }
    [Fact]
    public async Task Malformed_duplicate_or_oversized_response_is_not_retried()
    {
        foreach(var body in new[]{"not-json","{\"kind\":1,\"kind\":2}",new string('x',33000)}) {
            var transport=new Transport(body,source:new('1',64),prompt:new('2',64));
            var result=await Provider(transport).PlanAsync(Request,default);
            Assert.Null(result.Proposal); Assert.Equal("SchemaFailed",result.Error); Assert.Equal(1,transport.Calls);
        }
    }
}
