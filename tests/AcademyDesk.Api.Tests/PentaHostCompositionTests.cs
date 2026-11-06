using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Intelligence.Penta;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AcademyDesk.Api.Tests;

// Exercise Program's real registrations: no QaApiFactory, lease replacement,
// interpreter, database fixture or model is supplied by this factory.
public sealed class PentaHostCompositionTests
{
    private static readonly string[] BridgeFlags =
    [
        "OperationalDraftContractsEnabled", "OperationalOnboardingExecutionContractsEnabled",
        "OperationalOnboardingHttpContractsEnabled", "OperationalOnboardingGatewayContractsEnabled",
        "OperationalOnboardingPrivateBridgeContractsEnabled"
    ];

    private sealed class Factory(string environment = "Testing", string? disabledFlag = null)
        : WebApplicationFactory<Program>
    {
        private readonly string root = Path.Combine(Path.GetTempPath(), "academydesk-host-composition", Guid.NewGuid().ToString("N"));

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            Directory.CreateDirectory(Path.Combine(root, "wwwroot"));
            builder.UseEnvironment(environment).UseContentRoot(root).UseWebRoot(Path.Combine(root, "wwwroot"));
            var values = new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Server=127.0.0.1,1;Database=UnusedComposition;Encrypt=False;Connect Timeout=1",
                ["Database:ApplyMigrationsOnStartup"] = "false",
                ["Bootstrap:PlatformOwnerEmail"] = "", ["Bootstrap:PlatformOwnerPassword"] = "",
                ["MediaStorage:Enabled"] = "false", ["Security:DataProtection:Enabled"] = "false",
                ["Penta:Enabled"] = "false", ["Logging:LogLevel:Default"] = "Warning"
            };
            foreach (var flag in BridgeFlags) values["Penta:" + flag] = (flag != disabledFlag).ToString();
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(values));
            // Avoid user-profile key files; this does not replace a PENTA service.
            builder.ConfigureServices(services => services.AddDataProtection().UseEphemeralDataProtectionProvider());
        }
    }

    private static PentaOnboardingInterpretation Input(Func<CancellationToken, Task<JsonElement>> current) =>
        new(Guid.NewGuid(), "Synthetic composition check", JsonSerializer.SerializeToElement(new { draft = new { revision = 1 } }), current);

    private static HttpRequestMessage Request(PentaOnboardingInterpretation input, string credential)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/internal/penta/onboarding/current");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", credential);
        request.Headers.Add("X-Penta-Request", input.RequestId.ToString("D"));
        request.Headers.Add(PentaPrivateContextLeases.BodyHeader, PentaPrivateContextLeases.RequestHash(input));
        request.Headers.Add(PentaPrivateContextLeases.VersionHeader, PentaPrivateContextLeases.Version);
        return request;
    }

    private static void AssertNoModel(IServiceProvider services)
    {
        Assert.Null(services.GetService<IPentaOnboardingInterpreter>());
        Assert.Null(services.GetService<IPentaTeacherInterpreter>());
        Assert.Null(services.GetService<IPentaModelProvider>());
    }

    [Fact]
    public async Task Default_host_shares_one_lease_store_with_HTTP_and_retains_two_read_bound()
    {
        using var factory = new Factory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
        using var first = factory.Services.CreateScope();
        using var second = factory.Services.CreateScope();
        var leases = first.ServiceProvider.GetRequiredService<PentaPrivateContextLeases>();
        Assert.Same(leases, second.ServiceProvider.GetRequiredService<PentaPrivateContextLeases>());
        AssertNoModel(first.ServiceProvider);
        Assert.Equal("Microsoft.EntityFrameworkCore.SqlServer", first.ServiceProvider.GetRequiredService<AcademyDeskDbContext>().Database.ProviderName);
        var calls = 0;
        var input = Input(_ => { calls++; return Task.FromResult(JsonSerializer.SerializeToElement(new { synthetic = true })); });
        using var owned = leases.Open(input, default, out var credential);
        for (var i = 0; i < 2; i++)
        {
            using var request = Request(input, credential);
            using var response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(response.Headers.CacheControl?.NoStore);
            Assert.Equal(PentaPrivateContextLeases.Version, Assert.Single(response.Headers.GetValues(PentaPrivateContextLeases.VersionHeader)));
            Assert.True((await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("synthetic").GetBoolean());
        }
        using var exhaustedRequest = Request(input, credential);
        using var exhausted = await client.SendAsync(exhaustedRequest);
        Assert.Equal(HttpStatusCode.NotFound, exhausted.StatusCode);
        Assert.Equal(2, calls);
        owned.Dispose();
        Assert.Null(await leases.ReadAsync(credential, input.RequestId, PentaPrivateContextLeases.RequestHash(input), default));
    }

    [Theory]
    [InlineData("Production", null)]
    [InlineData("Staging", null)]
    [InlineData("Testing", "OperationalDraftContractsEnabled")]
    [InlineData("Testing", "OperationalOnboardingExecutionContractsEnabled")]
    [InlineData("Testing", "OperationalOnboardingHttpContractsEnabled")]
    [InlineData("Testing", "OperationalOnboardingGatewayContractsEnabled")]
    [InlineData("Testing", "OperationalOnboardingPrivateBridgeContractsEnabled")]
    public async Task Real_host_denies_non_testing_or_any_disabled_gate_without_reading_context(string environment, string? disabledFlag)
    {
        using var factory = new Factory(environment, disabledFlag);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
        using var scope = factory.Services.CreateScope();
        AssertNoModel(scope.ServiceProvider);
        var leases = scope.ServiceProvider.GetRequiredService<PentaPrivateContextLeases>();
        var calls = 0;
        var input = Input(_ => { calls++; return Task.FromResult(JsonSerializer.SerializeToElement(new { synthetic = true })); });
        using var owned = leases.Open(input, default, out var credential);
        using var request = Request(input, credential);
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.True(response.Headers.CacheControl?.NoStore);
        Assert.Equal(0, calls);
        Assert.False(response.Headers.Contains(PentaPrivateContextLeases.VersionHeader));
    }
}
