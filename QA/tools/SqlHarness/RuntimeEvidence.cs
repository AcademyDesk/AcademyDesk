using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

internal static partial class SqlHarnessEntryPoint
{
    private static void RecordRuntimeEndpoints(QaApiFactory factory, QaRunManifest manifest)
    {
        using var scope = factory.Services.CreateScope();
        foreach (var connection in new[]
        {
            scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>().Database.GetDbConnection(),
            scope.ServiceProvider.GetRequiredService<IdentityDbContext>().Database.GetDbConnection()
        })
        {
            var actual = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connection.ConnectionString);
            if (actual.DataSource != manifest.SqlServer || actual.InitialCatalog != manifest.Database ||
                actual.UserID != manifest.RuntimeLogin || actual.Password != manifest.RuntimePassword || actual.IntegratedSecurity)
                throw new InvalidOperationException("A resolved DbContext escaped the run-owned SQL configuration.");
        }
        var source = factory.Services.GetRequiredService<EndpointDataSource>();
        var endpoints = source.Endpoints.OfType<RouteEndpoint>().SelectMany(endpoint =>
        {
            var action = endpoint.Metadata.GetMetadata<ControllerActionDescriptor>();
            var methods = endpoint.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods ?? ["ANY"];
            return methods.Select(method => new RuntimeRoute(method, "/" + (endpoint.RoutePattern.RawText ?? "").TrimStart('/'),
                action?.ControllerName, action?.ActionName,
                endpoint.Metadata.GetMetadata<IAllowAnonymous>() is not null,
                endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>().Select(x =>
                    new RuntimeAuthorization(x.Policy, x.Roles, x.AuthenticationSchemes)).ToArray()));
        }).OrderBy(x => x.Route, StringComparer.Ordinal).ThenBy(x => x.Method, StringComparer.Ordinal).ToArray();
        var identity = new[]
        {
            "POST /api/auth/register", "POST /api/auth/login", "POST /api/auth/refresh",
            "GET /api/auth/confirmEmail", "POST /api/auth/resendConfirmationEmail",
            "POST /api/auth/forgotPassword", "POST /api/auth/resetPassword",
            "POST /api/auth/manage/2fa", "GET /api/auth/manage/info", "POST /api/auth/manage/info"
        };
        var keys = endpoints.Select(x => x.Method + " " + x.Route).ToHashSet(StringComparer.Ordinal);
        if (endpoints.Length == 0 || !keys.Contains("GET /health") || identity.Any(x => !keys.Contains(x)) ||
            endpoints.Count(x => x.Controller is not null) == 0)
            throw new InvalidOperationException("Runtime endpoint evidence is incomplete, including framework Identity routes.");
        var serialized = JsonSerializer.Serialize(endpoints);
        var digest = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(serialized)));
        Console.WriteLine("QA ENDPOINTS " + serialized);
        Console.WriteLine($"Runtime inventory PASS: routes={endpoints.Length}, controller method/routes={endpoints.Count(x => x.Controller is not null)}, " +
            $"framework Identity method/routes={endpoints.Count(x => x.Controller is null && x.Route.StartsWith("/api/auth/", StringComparison.Ordinal))}, " +
            $"SHA256={digest}; both resolved DbContexts match exact owned target and runtime login.");
    }

    private sealed record RuntimeRoute(string Method, string Route, string? Controller, string? Action,
        bool AllowAnonymousMetadata, RuntimeAuthorization[] AuthorizationMetadata);
    private sealed record RuntimeAuthorization(string? Policy, string? Roles, string? Schemes);
}
