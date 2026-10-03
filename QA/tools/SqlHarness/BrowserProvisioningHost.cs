using System.Net;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Identity;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

internal static partial class SqlHarnessEntryPoint
{
    // Owned loopback bridge, real auth/SQL/API. No fabricated responses or session injection.
    private static async Task ServeBrowserProvisioningAsync(QaRunManifest manifest)
    {
        if (!int.TryParse(Environment.GetEnvironmentVariable("QA_BROWSER_API_PORT"), out var port) || port is < 1024 or > 65535 ||
            !Uri.TryCreate(Environment.GetEnvironmentVariable("QA_BROWSER_ORIGIN"), UriKind.Absolute, out var origin) ||
            origin.Scheme != "http" || origin.Host != "127.0.0.1" || origin.Port is < 1024 or > 65535 || origin.Port == port ||
            origin.AbsolutePath != "/" || origin.Query != "" || origin.Fragment != "" || origin.UserInfo != "")
            throw new InvalidOperationException("Exact distinct loopback browser API port and origin required.");
        try
        {
            using var factory = new QaApiFactory(manifest, new Dictionary<string, string?> { ["Cors:AllowedOrigins:0"] = origin.GetLeftPart(UriPartial.Authority) });
            using var client = factory.CreateClient(new() { AllowAutoRedirect = false });
            if (!factory.PreflightPassed || (await client.GetAsync("/health")).StatusCode != HttpStatusCode.OK) throw new InvalidOperationException("Real backend preflight failed.");
            RecordRuntimeEndpoints(factory, manifest);
            await VerifyTenantIsolationAsync(factory, client);
            client.DefaultRequestHeaders.Authorization = null;
            using (var scope = factory.Services.CreateScope())
            {
                var roles = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
                var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                if (!(await roles.CreateAsync(new ApplicationRole { Name = "PlatformOwner" })).Succeeded) throw new InvalidOperationException("Fixture role failed.");
                var owner = new ApplicationUser { UserName = "qa-ui-owner@example.invalid", Email = "qa-ui-owner@example.invalid", EmailConfirmed = true, IsActive = true, IsPlatformOwner = true, DisplayName = "Synthetic UI Owner" };
                if (!(await users.CreateAsync(owner, "Synthetic!39Ab")).Succeeded || !(await users.AddToRoleAsync(owner, "PlatformOwner")).Succeeded) throw new InvalidOperationException("Fixture owner failed.");
            }
            var builder = WebApplication.CreateSlimBuilder();
            builder.Logging.ClearProviders();
            builder.WebHost.UseUrls($"http://127.0.0.1:{port}");
            await using var bridge = builder.Build();
            bridge.Run(async context =>
            {
                if (context.Connection.RemoteIpAddress is null || !IPAddress.IsLoopback(context.Connection.RemoteIpAddress) || context.Request.Host.Host != "127.0.0.1" || context.Request.Host.Port != port)
                { context.Response.StatusCode = 400; return; }
                using var request = new HttpRequestMessage(new HttpMethod(context.Request.Method), context.Request.Path + context.Request.QueryString);
                if (context.Request.ContentLength > 0 || context.Request.Headers.ContainsKey("Transfer-Encoding")) request.Content = new StreamContent(context.Request.Body);
                foreach (var header in context.Request.Headers)
                {
                    if (header.Key.Equals("Host", StringComparison.OrdinalIgnoreCase) || header.Key.Equals("Connection", StringComparison.OrdinalIgnoreCase) || header.Key.Equals("Transfer-Encoding", StringComparison.OrdinalIgnoreCase)) continue;
                    if (!request.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray())) request.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
                }
                using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, context.RequestAborted);
                context.Response.StatusCode = (int)response.StatusCode;
                foreach (var header in response.Headers.Concat(response.Content.Headers))
                {
                    if (header.Key.Equals("Transfer-Encoding", StringComparison.OrdinalIgnoreCase) || header.Key.Equals("Connection", StringComparison.OrdinalIgnoreCase)) continue;
                    context.Response.Headers[header.Key] = header.Value.ToArray();
                }
                Console.WriteLine($"PROVISIONUI HTTP {context.Request.Method} {context.Request.Path} {(int)response.StatusCode}");
                await response.Content.CopyToAsync(context.Response.Body, context.RequestAborted);
            });
            await bridge.StartAsync();
            var stop = Path.Combine(manifest.Root, "stop-browser");
            Console.WriteLine($"PROVISIONUI READY run={manifest.RunId:N} api={port} origin={origin.Port} stop={stop}");
            var deadline = DateTime.UtcNow.AddMinutes(25);
            while (!File.Exists(stop) && DateTime.UtcNow < deadline) await Task.Delay(1000);
            await bridge.StopAsync();
            // Durable post-browser acceptance, independent from visible UI assertions.
            using var finalScope = factory.Services.CreateScope();
            var finalUsers = finalScope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var db = finalScope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            foreach (var (email, role) in new[] { ("qa-ui-student@example.invalid", "Student"), ("qa-ui-teacher@example.invalid", "Teacher"), ("qa-ui-admin@example.invalid", "AcademyAdmin") })
            {
                var user = await finalUsers.FindByEmailAsync(email);
                if (user?.AcademyId is null || !await finalUsers.IsInRoleAsync(user, role) || !await db.Academies.AnyAsync(x => x.Id == user.AcademyId)) throw new InvalidOperationException("Browser-created account/role/academy missing.");
                if (role == "Student" && !await db.Students.AnyAsync(x => x.Id == user.StudentId && x.AcademyId == user.AcademyId)) throw new InvalidOperationException("Student typed link missing.");
                if (role == "Teacher" && !await db.Teachers.AnyAsync(x => x.Id == user.TeacherId && x.AcademyId == user.AcademyId)) throw new InvalidOperationException("Teacher typed link missing.");
            }
            if (await db.Academies.CountAsync(x => x.Name == "Synthetic UI Academy") != 1) throw new InvalidOperationException("Browser-created academy is not unique.");
            Console.WriteLine("PROVISIONUI SQL PASS three real browser-created identities/roles/academy associations, student/teacher typed links, one unique new academy; broader lifecycle not claimed.");
        }
        finally
        {
            if (Directory.Exists(manifest.Root)) QaRunGuard.CleanupHostFolders(manifest, root => { Directory.Delete(root, recursive: true); return true; });
        }
    }
}
