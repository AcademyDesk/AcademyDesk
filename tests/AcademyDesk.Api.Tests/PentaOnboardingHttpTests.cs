using System.Reflection;
using System.Text.Json;
using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Intelligence.Penta;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;

namespace AcademyDesk.Api.Tests;

public sealed class PentaOnboardingHttpTests
{
    [Theory]
    [InlineData("Production", true, true)]
    [InlineData("Development", true, true)]
    [InlineData("Testing", false, true)]
    [InlineData("Testing", true, false)]
    public async Task All_actions_require_both_testing_gates_before_store_access(string environment, bool draft, bool http)
    {
        using var fixture = new Fixture(environment, draft, http);
        var controller = fixture.Controller;
        var id = Guid.NewGuid();
        var invalid = JsonSerializer.SerializeToElement(new { });
        Assert.IsType<NotFoundResult>(await controller.Save(id, id, id, invalid, default));
        Assert.IsType<NotFoundResult>(await controller.Get(id, id, id, default));
        Assert.IsType<NotFoundResult>(await controller.CoreContext(id, id, id, default));
        Assert.IsType<NotFoundResult>(await controller.CorePrepare(id, id, id, id, invalid, default));
        Assert.IsType<NotFoundResult>(await controller.CoreValidation(id, id, id, id, default));
        Assert.IsType<NotFoundResult>(await controller.CorePartial(id, id, id, id, invalid, default));
        Assert.IsType<NotFoundResult>(await controller.CoreTransition(id, id, id, id, invalid, default));
        Assert.IsType<NotFoundResult>(await controller.IssueChallenge(id, id, id, invalid, default));
        Assert.IsType<NotFoundResult>(await controller.ConfirmCore(id, id, id, invalid, default));
        Assert.IsType<NotFoundResult>(await controller.Operation(id, id, id, invalid, default));
        Assert.IsType<NotFoundResult>(await controller.Status(id, id, id, invalid, default));
        Assert.IsType<NotFoundResult>(await controller.ExecuteCore(id, id, id, invalid, default));
        Assert.IsType<NotFoundResult>(await controller.Review(id, id, id, invalid, default));
        Assert.IsType<NotFoundResult>(await controller.AuthorizeCreation(id, id, id, invalid, default));
        Assert.IsType<NotFoundResult>(await controller.Execute(id, id, id, invalid, default));
        Assert.IsType<NotFoundResult>(await controller.CurrentConversation(id, id, default));
        Assert.IsType<NotFoundResult>(await controller.SelectDraft(id, id, invalid, default));
        Assert.IsType<NotFoundResult>(await controller.ClaimTurn(id, id, invalid, default));
        Assert.IsType<NotFoundResult>(await controller.CompleteTurn(id,id,id,invalid,default));
        Assert.IsType<NotFoundResult>(await controller.TurnResult(id,id,id,default));
        Assert.IsType<NotFoundResult>(await controller.GatewayTurn(id,id,invalid,default));
        Assert.IsType<NotFoundResult>(await controller.BrowserTurn(id,id,invalid,default));
        Assert.IsType<NotFoundResult>(await controller.SendReadiness(id,id,default));
        Assert.IsType<NotFoundResult>(await controller.StartReadiness(id,default));
        Assert.IsType<NotFoundResult>(await controller.StartConversation(id,invalid,default));
        Assert.IsType<NotFoundResult>(await controller.StartResult(id,id,default));
        Assert.Empty(fixture.Db.ChangeTracker.Entries());
        Assert.Empty(fixture.Identity.ChangeTracker.Entries());
    }

    [Theory]
    [InlineData("[]")]
    [InlineData("null")]
    [InlineData("{}")]
    [InlineData("{\"expectedRevision\":0,\"contextVersion\":1,\"fields\":{},\"tenantId\":\"untrusted\"}")]
    [InlineData("{\"expectedRevision\":0,\"expectedRevision\":1,\"contextVersion\":1,\"fields\":{}}")]
    [InlineData("{\"expectedRevision\":100,\"contextVersion\":1,\"fields\":{}}")]
    [InlineData("{\"expectedRevision\":0,\"contextVersion\":9007199254740992,\"fields\":{}}")]
    [InlineData("{\"expectedRevision\":0,\"contextVersion\":1,\"fields\":[]}")]
    public async Task Save_rejects_unexpected_or_ambiguous_top_level_contract(string json)
    {
        using var fixture = new Fixture();
        using var document = JsonDocument.Parse(json);
        Assert.IsType<BadRequestObjectResult>(await fixture.Controller.Save(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), document.RootElement, default));
        Assert.Empty(fixture.Db.ChangeTracker.Entries());
    }

    [Theory]
    [InlineData("review")]
    [InlineData("authorize")]
    [InlineData("execute")]
    public async Task Bound_actions_reject_wrong_digest_revision_extra_authority_and_duplicate_keys(string action)
    {
        using var fixture = new Fixture();
        var hash = new string('a', 64);
        var fields = new[] {
            "\"revision\":0,\"payloadHash\":\"" + hash + "\"",
            "\"revision\":1,\"payloadHash\":\"" + new string('A',64) + "\"",
            "\"revision\":1,\"payloadHash\":\"" + hash + "\",\"approved\":true",
            "\"revision\":1,\"revision\":2,\"payloadHash\":\"" + hash + "\"" };
        foreach (var value in fields)
        {
            using var document = JsonDocument.Parse("{" + value + (action == "authorize" ? ",\"intent\":\"yes\"" : "") + "}");
            var result = action switch {
                "review" => await fixture.Controller.Review(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), document.RootElement, default),
                "execute" => await fixture.Controller.Execute(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), document.RootElement, default),
                _ => await fixture.Controller.AuthorizeCreation(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), document.RootElement, default) };
            Assert.IsType<BadRequestObjectResult>(result);
        }
        Assert.Empty(fixture.Db.ChangeTracker.Entries());
    }

    [Fact]
    public async Task Valid_shape_does_not_bypass_SQL_guard_and_responses_are_not_cached()
    {
        using var fixture = new Fixture();
        var input = JsonSerializer.SerializeToElement(new { expectedRevision = 0, contextVersion = 1, fields = new { } });
        var result = Assert.IsType<ObjectResult>(await fixture.Controller.Save(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), input, default));
        Assert.Equal(503, result.StatusCode);
        Assert.Equal("no-store", fixture.Controller.Response.Headers.CacheControl);
        Assert.Equal("no-cache", fixture.Controller.Response.Headers.Pragma);
        Assert.Empty(fixture.Db.ChangeTracker.Entries());
    }

    [Fact]
    public void HTTP_contract_is_bearer_only_and_has_explicit_request_limits()
    {
        var type = typeof(PentaOnboardingController);
        Assert.Equal(IdentityConstants.BearerScheme, type.GetCustomAttribute<AuthorizeAttribute>()!.AuthenticationSchemes);
        Assert.Equal(16 * 1024, ((IRequestSizeLimitMetadata)type.GetMethod("Save")!.GetCustomAttribute<RequestSizeLimitAttribute>()!).MaxRequestBodySize);
        foreach (var action in new[] { "Review", "AuthorizeCreation", "Execute", "IssueChallenge", "ConfirmCore", "Operation", "Status", "ExecuteCore" })
            Assert.Equal(1024, ((IRequestSizeLimitMetadata)type.GetMethod(action)!.GetCustomAttribute<RequestSizeLimitAttribute>()!).MaxRequestBodySize);
        Assert.Equal(2048, ((IRequestSizeLimitMetadata)type.GetMethod("CoreTransition")!.GetCustomAttribute<RequestSizeLimitAttribute>()!).MaxRequestBodySize);
        Assert.Equal(16 * 1024, ((IRequestSizeLimitMetadata)type.GetMethod("CorePartial")!.GetCustomAttribute<RequestSizeLimitAttribute>()!).MaxRequestBodySize);
        Assert.Equal(2048, ((IRequestSizeLimitMetadata)type.GetMethod("SelectDraft")!.GetCustomAttribute<RequestSizeLimitAttribute>()!).MaxRequestBodySize);
        Assert.Equal(16 * 1024, ((IRequestSizeLimitMetadata)type.GetMethod("ClaimTurn")!.GetCustomAttribute<RequestSizeLimitAttribute>()!).MaxRequestBodySize);
        Assert.Equal(32 * 1024, ((IRequestSizeLimitMetadata)type.GetMethod("CompleteTurn")!.GetCustomAttribute<RequestSizeLimitAttribute>()!).MaxRequestBodySize);
        Assert.Equal(16 * 1024, ((IRequestSizeLimitMetadata)type.GetMethod("GatewayTurn")!.GetCustomAttribute<RequestSizeLimitAttribute>()!).MaxRequestBodySize);
        Assert.Equal(1024, ((IRequestSizeLimitMetadata)type.GetMethod("StartConversation")!.GetCustomAttribute<RequestSizeLimitAttribute>()!).MaxRequestBodySize);
        Assert.Equal(16*1024, ((IRequestSizeLimitMetadata)type.GetMethod("BrowserTurn")!.GetCustomAttribute<RequestSizeLimitAttribute>()!).MaxRequestBodySize);
    }

    [Theory]
    [InlineData("null")]
    [InlineData("[]")]
    [InlineData("{}")]
    [InlineData("{\"requestId\":1}")]
    [InlineData("{\"requestId\":\"00000000-0000-0000-0000-000000000000\"}")]
    [InlineData("{\"requestId\":\"11111111111141118111111111111111\"}")]
    [InlineData("{\"requestId\":\"AAAAAAAA-AAAA-4AAA-8AAA-AAAAAAAAAAAA\"}")]
    [InlineData("{\"requestId\":\"11111111-1111-4111-8111-111111111111\",\"tenantId\":\"untrusted\"}")]
    [InlineData("{\"requestId\":\"11111111-1111-4111-8111-111111111111\",\"requestId\":\"11111111-1111-4111-8111-111111111111\"}")]
    public async Task Start_rejects_noncanonical_or_additional_input_without_store(string json)
    {
        using var fixture = new Fixture(gateway:true);
        var result = Assert.IsType<ObjectResult>(await fixture.Controller.StartConversation(Guid.NewGuid(), JsonSerializer.Deserialize<JsonElement>(json), default));
        Assert.Equal(400, result.StatusCode); Assert.Empty(fixture.Db.ChangeTracker.Entries());
    }
    [Fact]
    public async Task Start_requires_SQL_for_valid_shape_and_readiness()
    {
        using var fixture = new Fixture(gateway:true);var id=Guid.NewGuid();
        foreach(var result in new[] { await fixture.Controller.StartConversation(id,JsonSerializer.SerializeToElement(new{requestId=id}),default),
            await fixture.Controller.StartReadiness(id,default),await fixture.Controller.StartResult(id,id,default) })
            Assert.Equal(503,Assert.IsType<ObjectResult>(result).StatusCode);
        Assert.Empty(fixture.Db.ChangeTracker.Entries()); Assert.Empty(fixture.Identity.ChangeTracker.Entries());
    }
    [Fact]
    public async Task Send_readiness_requires_SQL_and_no_store()
    {
        using var fixture=new Fixture();var id=Guid.NewGuid();
        Assert.Equal(503,Assert.IsType<ObjectResult>(await fixture.Controller.SendReadiness(id,id,default)).StatusCode);
        Assert.Equal("no-store",fixture.Controller.Response.Headers.CacheControl);
        Assert.Empty(fixture.Db.ChangeTracker.Entries());Assert.Empty(fixture.Identity.ChangeTracker.Entries());
    }
    [Fact]
    public async Task Browser_turn_has_no_default_runtime_or_SQL_admission()
    {
        using var fixture=new Fixture(gateway:true);var id=Guid.NewGuid();
        Assert.Equal(503,Assert.IsType<ObjectResult>(await fixture.Controller.BrowserTurn(id,id,JsonSerializer.SerializeToElement(new{}),default)).StatusCode);
        Assert.Empty(fixture.Db.ChangeTracker.Entries());Assert.Empty(fixture.Identity.ChangeTracker.Entries());
    }
    private sealed class Fixture : IDisposable
    {
        public AcademyDeskDbContext Db { get; } = new(new DbContextOptionsBuilder<AcademyDeskDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        public IdentityDbContext Identity { get; } = new(new DbContextOptionsBuilder<IdentityDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        public PentaOnboardingController Controller { get; }
        public Fixture(string environment = "Testing", bool draft = true, bool http = true, bool gateway = false)
        {
            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?> {
                ["Penta:OperationalDraftContractsEnabled"] = draft.ToString(),
                ["Penta:OperationalOnboardingHttpContractsEnabled"] = http.ToString(),
                ["Penta:OperationalOnboardingExecutionContractsEnabled"] = "true",
                ["Penta:OperationalOnboardingGatewayContractsEnabled"] = gateway.ToString() }).Build();
            var host = new Host(environment);
            var service = new PentaOnboardingDraftService(Db, Identity, new EphemeralDataProtectionProvider(), TimeProvider.System, config, host);
            Controller = new(service, config, host, NullLogger<PentaOnboardingController>.Instance) {
                ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() } };
        }
        public void Dispose() { Db.Dispose(); Identity.Dispose(); }
    }
    private sealed class Host(string environment) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environment;
        public string ApplicationName { get; set; } = "HttpGateTest";
        public string ContentRootPath { get; set; } = "";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
