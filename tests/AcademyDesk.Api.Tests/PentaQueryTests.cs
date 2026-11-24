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

public sealed class PentaQueryTests
{
    private sealed class Environment(string name) : IHostEnvironment {
        public string EnvironmentName { get; set; } = name;
        public string ApplicationName { get; set; } = "QA";
        public string ContentRootPath { get; set; } = "";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
    private sealed class Fixture : IDisposable {
        internal ServiceProvider Services;
        internal IdentityDbContext Identity;
        internal AcademyDeskDbContext Db;
        internal UserManager<ApplicationUser> Users;
        internal ApplicationUser Actor;
        internal Academy Academy;
        internal ClaimsPrincipal Principal;
        internal Fixture(string role = "Owner") {
            var services = new ServiceCollection().AddLogging();
            services.AddDbContext<IdentityDbContext>(x => x.UseInMemoryDatabase("query-id-" + Guid.NewGuid()));
            services.AddDbContext<AcademyDeskDbContext>(x => x.UseInMemoryDatabase("query-db-" + Guid.NewGuid()));
            services.AddIdentityCore<ApplicationUser>().AddRoles<ApplicationRole>().AddEntityFrameworkStores<IdentityDbContext>();
            Services = services.BuildServiceProvider(); Identity = Services.GetRequiredService<IdentityDbContext>();
            Db = Services.GetRequiredService<AcademyDeskDbContext>(); Users = Services.GetRequiredService<UserManager<ApplicationUser>>();
            Academy = new Academy { Name = "Synthetic query academy", EnabledModulesJson = "[]" };
            Db.Academies.Add(Academy); Db.SaveChanges();
            Actor = new ApplicationUser { AcademyId = Academy.Id, UserName = "synthetic", SecurityStamp = Guid.NewGuid().ToString("N") };
            var membership = new ApplicationRole { Name = role, NormalizedName = role.ToUpperInvariant() };
            Identity.Users.Add(Actor); Identity.Roles.Add(membership);
            Identity.UserRoles.Add(new IdentityUserRole<Guid> { UserId = Actor.Id, RoleId = membership.Id }); Identity.SaveChanges();
            Principal = new(new ClaimsIdentity([new Claim(Users.Options.ClaimsIdentity.UserIdClaimType, Actor.Id.ToString("D")),
                new Claim(Users.Options.ClaimsIdentity.SecurityStampClaimType, Actor.SecurityStamp!), new Claim(ClaimTypes.Role, "Owner")], "test"));
        }
        internal PentaQueryAccess Access(string environment = "Testing", bool enabled = true) => new(Identity, Db, Users,
            new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> {
                ["Penta:Enabled"] = "true", ["Penta:Queries:Enabled"] = enabled.ToString() }).Build(), new Environment(environment));
        public void Dispose() => Services.Dispose();
    }
    private static PentaQueryPlan Search(string entity, Dictionary<string, string>? filters = null) => new(PentaQueryContract.Version, "search", entity, filters ?? [], null);
    private static HashSet<string> All => PentaQueryContract.Controllers.Keys.SelectMany(e => new[] { "search", "open" }.Select(k => PentaQueryContract.Tool(e, k))).ToHashSet();

    [Theory][InlineData("Owner")][InlineData("AcademyAdmin")]
    public async Task Catalog_uses_stored_admin_identity_and_does_not_require_finance(string role) {
        using var f = new Fixture(role);
        var value = await f.Access().ResolveAsync(f.Principal, f.Academy.Id, default);
        Assert.NotNull(value); Assert.Equal(6, value.Tools.Count); Assert.All(value.Tools, tool => Assert.DoesNotContain("Payment", tool));
        Assert.Equal(f.Actor.Id, value.ActorId);
    }
    [Theory][InlineData("Teacher")][InlineData("Student")][InlineData("Guardian")][InlineData("PlatformOwner")][InlineData("Staff")]
    public async Task Spoofed_owner_claim_does_not_grant_directory_reads(string role) {
        using var f = new Fixture(role); Assert.Null(await f.Access().ResolveAsync(f.Principal, f.Academy.Id, default));
    }
    [Theory][InlineData("actor")][InlineData("academy")][InlineData("stamp")][InlineData("membership")][InlineData("tenant")][InlineData("platform")]
    public async Task Current_revocation_is_independently_enforced(string change) {
        using var f = new Fixture(); Assert.NotNull(await f.Access().ResolveAsync(f.Principal, f.Academy.Id, default));
        switch(change) {
            case "actor": f.Actor.IsActive = false; break;
            case "academy": f.Academy.IsActive = false; break;
            case "stamp": f.Actor.SecurityStamp = Guid.NewGuid().ToString(); break;
            case "membership": f.Identity.UserRoles.RemoveRange(f.Identity.UserRoles); break;
            case "tenant": f.Actor.AcademyId = Guid.NewGuid(); break;
            case "platform": f.Actor.IsPlatformOwner = true; break;
        }
        await f.Identity.SaveChangesAsync(); await f.Db.SaveChangesAsync();
        Assert.Null(await f.Access().ResolveAsync(f.Principal, f.Academy.Id, default));
    }
    [Theory][InlineData("Production", true)][InlineData("Staging", true)][InlineData("Testing", false)]
    public void Query_gate_excludes_unqualified_release_environments(string environment, bool enabled) {
        using var f = new Fixture(); Assert.False(f.Access(environment, enabled).Enabled);
    }
    [Fact]
    public async Task Teachers_use_teacher_table_and_subjects_not_student_financial_data() {
        using var f = new Fixture(); var foreign = Guid.NewGuid();
        var teacher = new Teacher { AcademyId = f.Academy.Id, FirstName = "Nisha", LastName = "Teacher", EmployeeCode = "EMP-1", Specialties = "Piano", Email = "private@example.invalid", Phone = "private", CompensationJson = "private" };
        f.Db.Teachers.AddRange(teacher, new Teacher { AcademyId = foreign, FirstName = "Foreign", LastName = "Teacher", Specialties = "Piano" });
        f.Db.Students.Add(new Student { AcademyId = f.Academy.Id, FirstName = "Nisha", LastName = "Student" }); await f.Db.SaveChangesAsync();
        var reader = new PentaQueryReader(f.Db, TimeProvider.System);
        var value = await reader.ExecuteAsync(f.Academy.Id, Search("teacher", new() { ["subject"] = "Piano" }), PentaQueryContext.Empty, All, default);
        Assert.Equal("RESULT", value.Kind); var row = Assert.Single(value.Result!.Records);
        Assert.Equal(teacher.Id, row.Id); Assert.Equal("EMP-1", row.Code); Assert.Equal("Piano", Assert.Single(row.Subjects));
        Assert.Equal($"/teacher-profile?teacherId={teacher.Id:D}", row.SourcePath);
        var json = JsonSerializer.Serialize(value); Assert.DoesNotContain("private", json); Assert.DoesNotContain("Compensation", json); Assert.DoesNotContain("Student", json);
    }
    [Theory][InlineData("teacher")][InlineData("student")][InlineData("batch")]
    public async Task Every_reader_is_scoped_bounded_stably_ordered_and_ordinal_bound(string entity) {
        using var f = new Fixture(); var foreign = Guid.NewGuid();
        for (var i = 0; i < 13; i++) {
            var academy = i < 12 ? f.Academy.Id : foreign;
            if (entity == "teacher") f.Db.Teachers.Add(new Teacher { AcademyId = academy, FirstName = "Synthetic", LastName = "Record" + i.ToString("D2") });
            if (entity == "student") f.Db.Students.Add(new Student { AcademyId = academy, FirstName = "Synthetic", LastName = "Record" + i.ToString("D2") });
            if (entity == "batch") f.Db.Batches.Add(new Batch { AcademyId = academy, Name = "Record" + i.ToString("D2"), CourseId = Guid.NewGuid() });
        }
        await f.Db.SaveChangesAsync(); var reader = new PentaQueryReader(f.Db, TimeProvider.System);
        var found = await reader.ExecuteAsync(f.Academy.Id, Search(entity), PentaQueryContext.Empty, All, default);
        Assert.True(found.Result!.HasMore); Assert.Equal(10, found.Result.Records.Length);
        var opened = await reader.ExecuteAsync(f.Academy.Id, new(PentaQueryContract.Version, "open", entity, [], 2), found.Context, All, default);
        Assert.Equal(found.Result.Records[1].Id, Assert.Single(opened.Result!.Records).Id);
        Assert.False(await reader.CurrentAsync(foreign, found.Context, default));
        var mismatch = await reader.ExecuteAsync(f.Academy.Id, new(PentaQueryContract.Version, "open", entity == "teacher" ? "student" : "teacher", [], 1), found.Context, All, default);
        Assert.Equal("CLARIFICATION_REQUIRED", mismatch.Kind); Assert.Null(mismatch.Result);
    }
    [Fact]
    public async Task Missing_tool_never_substitutes_an_available_domain() {
        using var f = new Fixture(); var reader = new PentaQueryReader(f.Db, TimeProvider.System);
        var outcome = await reader.ExecuteAsync(f.Academy.Id, Search("teacher"), PentaQueryContext.Empty, new HashSet<string> { "AcademyDesk.student.search" }, default);
        Assert.Equal("UNSUPPORTED", outcome.Kind); Assert.Null(outcome.Result);
        var invalid = await reader.ExecuteAsync(f.Academy.Id, Search("teacher", new() { ["salary"] = "all" }), PentaQueryContext.Empty, All, default);
        Assert.Equal("ERROR", invalid.Kind); Assert.Null(invalid.Result);
    }
    [Theory][InlineData("I said teachers", "student")][InlineData("Show students", "teacher")][InlineData("Find batch Piano", "student")]
    public void Structurally_valid_wrong_entity_is_rejected(string text, string wrong) => Assert.False(PentaQueryIntentGuard.Compatible(text, Search(wrong)));
    [Theory][InlineData("Create student and batch")][InlineData("Show teacher attendance")][InlineData("Show student balances")][InlineData("Export students")]
    public void Unavailable_operation_cannot_become_a_successful_directory_read(string text) => Assert.False(PentaQueryIntentGuard.Compatible(text, Search("student")));
    [Theory][InlineData("I said teachers")][InlineData("Show teacher attendance")][InlineData("Create student and batch")]
    public void Legacy_pilot_refuses_wrong_domain_instead_of_executing_a_learner_tool(string text) => Assert.True(PentaLegacyReadGuard.Unsupported(text));

    [Fact]
    public void Actual_host_request_is_versioned_minimized_and_compatible_with_the_query_service() {
        var privateId = Guid.NewGuid();
        var value = PentaQueryContract.Request("I said teachers", new("student", new() { ["name"] = "Synthetic" }, [privateId]), All);
        var json = value.GetRawText();
        Assert.DoesNotContain(privateId.ToString("D"), json); Assert.DoesNotContain("actor", json); Assert.DoesNotContain("salary", json);
        Assert.Equal(6, value.GetProperty("tools").GetArrayLength());
        Assert.Equal("Result 1", value.GetProperty("context").GetProperty("result_labels")[0].GetString());
        if (System.Environment.GetEnvironmentVariable("QA_QUERY_CONTRACT_VECTOR") is { Length: > 0 } destination) File.WriteAllText(destination, json);
    }

    private sealed class Transport(string body, bool provenance = true) : HttpMessageHandler {
        internal int Calls; internal string? Target;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token) {
            Calls++; Target = request.RequestUri!.AbsolutePath;
            var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK) { Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json") };
            if (provenance) {
                response.Headers.TryAddWithoutValidation(MiniRuntimeIdentity.HeaderName, JsonSerializer.Serialize(new MiniRuntimeIdentity(
                    "0.1.14", "planner-0.15", "0.1", new('a',64), new('b',64), new('c',64), new('d',64), new('e',64), new('f',64))));
                response.Headers.TryAddWithoutValidation("X-Penta-Query-Contract", PentaQueryContract.Version);
                response.Headers.TryAddWithoutValidation("X-Penta-Query-Source", new string('1',64));
                response.Headers.TryAddWithoutValidation("X-Penta-Query-Prompt", new string('2',64));
            }
            return Task.FromResult(response);
        }
    }
    private static PentaQueryPlanner Provider(Transport handler, string url = "http://127.0.0.1:8000/", string environment = "Testing") => new(new HttpClient(handler),
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?> { ["Penta:Enabled"]="true", ["Penta:Queries:Enabled"]="true", ["Penta:Queries:BaseUrl"]=url }).Build(), new Environment(environment));
    [Fact]
    public async Task Real_transport_targets_query_protocol_and_retains_provenance_without_legacy_fallback() {
        var transport = new Transport(JsonSerializer.Serialize(Search("teacher")));
        var result = await Provider(transport).PlanAsync(JsonSerializer.SerializeToElement(new { contract=PentaQueryContract.Version }), default);
        Assert.NotNull(result.Plan); Assert.Equal("teacher", result.Plan.Entity); Assert.Equal(1, transport.Calls);
        Assert.Equal("/v1/academy-query/plan", transport.Target); Assert.Equal(new string('1',64),result.QuerySourceSha256); Assert.Equal(new string('2',64),result.QueryPromptSha256);
    }
    [Theory]
    [InlineData("http://example.invalid/")][InlineData("http://localhost:8000/")][InlineData("https://127.0.0.1/")]
    [InlineData("http://127.0.0.1:8000/path")][InlineData("http://user:password@127.0.0.1/")][InlineData("http://127.0.0.1/?q=1")]
    public async Task External_or_manipulated_endpoint_never_receives_context(string url) {
        var transport = new Transport("{}");
        Assert.Null((await Provider(transport,url).PlanAsync(JsonSerializer.SerializeToElement(new { contract=PentaQueryContract.Version }),default)).Plan);
        Assert.Equal(0,transport.Calls);
    }
    [Fact]
    public async Task Missing_query_provenance_fails_closed_without_retry() {
        var transport = new Transport(JsonSerializer.Serialize(Search("teacher")), false);
        var result = await Provider(transport).PlanAsync(JsonSerializer.SerializeToElement(new { contract=PentaQueryContract.Version }),default);
        Assert.Null(result.Plan); Assert.Equal("RuntimeIdentityFailed",result.Error); Assert.Equal(1,transport.Calls);
    }
    [Theory]
    [InlineData("{\"contract\":\"academy-query.v1\",\"kind\":\"search\",\"entity\":\"teacher\",\"filters\":{},\"position\":null,\"sql\":\"SELECT\"}")]
    [InlineData("{\"contract\":\"academy-query.v1\",\"kind\":\"search\",\"entity\":\"teacher\",\"entity\":\"student\",\"filters\":{},\"position\":null}")]
    [InlineData("{\"contract\":\"academy-query.v1\",\"kind\":\"search\",\"entity\":\"teacher\",\"Entity\":\"student\",\"filters\":{},\"position\":null}")]
    [InlineData("{\"contract\":\"academy-query.v1\",\"kind\":\"search\",\"entity\":\"teacher\",\"filters\":{},\"position\":null,\"padding\":\"x\"}")]
    public async Task Malformed_or_ambiguous_model_JSON_cannot_produce_a_tool_request(string json) {
        var transport = new Transport(json);
        Assert.Null((await Provider(transport).PlanAsync(JsonSerializer.SerializeToElement(new { contract=PentaQueryContract.Version }),default)).Plan);
        Assert.Equal(1,transport.Calls);
    }
}
