using AcademyDesk.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Tests.Infrastructure;

public sealed class QaApiFactoryTests
{
    [Fact]
    public void Testing_host_starts_with_isolated_roots_and_both_contexts_on_approved_target()
    {
        var manifest = QaRunManifest.Create();
        try
        {
            using var factory = new QaApiFactory(manifest);
            _ = factory.Server; // Start the actual Program pipeline; do not issue a DB request.

            Assert.True(factory.PreflightPassed);
            var environment = factory.Services.GetRequiredService<IWebHostEnvironment>();
            Assert.Equal("Testing", environment.EnvironmentName);
            Assert.Equal(manifest.ContentRoot, environment.ContentRootPath);
            Assert.Equal(manifest.WebRoot, environment.WebRootPath);
            using var scope = factory.Services.CreateScope();
            var application = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var identity = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            Assert.Equal(manifest.Database, application.Database.GetDbConnection().Database);
            Assert.Equal(manifest.Database, identity.Database.GetDbConnection().Database);
            Assert.Equal(manifest.SqlServer, application.Database.GetDbConnection().DataSource);
            Assert.Equal(manifest.SqlServer, identity.Database.GetDbConnection().DataSource);
            var routes = factory.Services.GetRequiredService<EndpointDataSource>().Endpoints
                .OfType<RouteEndpoint>().Select(endpoint => endpoint.RoutePattern.RawText).ToArray();
            Assert.Contains("/health", routes);
            Assert.Contains("/api/auth/login", routes);
        }
        finally
        {
            if (Directory.Exists(manifest.Root))
                QaRunGuard.CleanupHostFolders(manifest, root => { Directory.Delete(root, recursive: true); return true; });
        }
        Assert.False(Directory.Exists(manifest.Root));
    }

    [Theory]
    [InlineData("ConnectionStrings:DefaultConnection", "Server=example.invalid;Database=AcademyDesk_Dev;Integrated Security=true")]
    [InlineData("QA:RunToken", "wrong-token")]
    [InlineData("QA:StorageRoot", "C:\\unapproved")]
    [InlineData("Database:ApplyMigrationsOnStartup", "true")]
    [InlineData("Bootstrap:PlatformOwnerEmail", "owner@example.invalid")]
    public void Final_configuration_override_is_rejected_before_host_start(string key, string value)
    {
        var manifest = QaRunManifest.Create();
        using var factory = new QaApiFactory(manifest, new Dictionary<string, string?> { [key] = value });

        Assert.ThrowsAny<Exception>(() => _ = factory.Server);
        Assert.False(factory.PreflightPassed);
        Assert.False(Directory.Exists(manifest.Root));
    }
}
