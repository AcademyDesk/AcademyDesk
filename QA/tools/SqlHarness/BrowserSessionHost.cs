using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Identity;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

internal static partial class SqlHarnessEntryPoint
{
    // Browser uses normal UI login. Native controls keep their own tokens in memory.
    private static async Task ServeBrowserSessionAsync(QaRunManifest manifest)
    {
        if (!int.TryParse(Environment.GetEnvironmentVariable("QA_BROWSER_API_PORT"), out var port) || port is < 1024 or > 65535 ||
            !Uri.TryCreate(Environment.GetEnvironmentVariable("QA_BROWSER_ORIGIN"), UriKind.Absolute, out var origin) ||
            origin.Scheme != "http" || origin.Host != "127.0.0.1" || origin.Port is < 1024 or > 65535 || origin.Port == port ||
            origin.AbsolutePath != "/" || origin.Query != "" || origin.Fragment != "" || origin.UserInfo != "")
            throw new InvalidOperationException("Exact distinct loopback browser API port and origin required.");
        try
        {
            using var factory = new QaApiFactory(manifest, new Dictionary<string, string?> { ["Cors:AllowedOrigins:0"] = origin.GetLeftPart(UriPartial.Authority) });
            using var client = factory.CreateClient(new() { AllowAutoRedirect = false, HandleCookies = false });
            if (!factory.PreflightPassed || (await client.GetAsync("/health")).StatusCode != HttpStatusCode.OK) throw new InvalidOperationException("Backend preflight failed.");
            RecordRuntimeEndpoints(factory, manifest);
            await VerifyTenantIsolationAsync(factory, client);
            client.DefaultRequestHeaders.Authorization = null;
            Guid ownerId, adminId;
            string ownerStamp;
            int initialUsers, initialRoles;
            using (var scope = factory.Services.CreateScope())
            {
                var roles = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
                var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                if (!(await roles.CreateAsync(new ApplicationRole { Name = "PlatformOwner" })).Succeeded) throw new InvalidOperationException("Fixture role failed.");
                var owner = new ApplicationUser { UserName = "qa-session-owner@example.invalid", Email = "qa-session-owner@example.invalid", EmailConfirmed = true, IsActive = true, IsPlatformOwner = true, DisplayName = "Synthetic Session Owner" };
                if (!(await users.CreateAsync(owner, "Synthetic!39Ab")).Succeeded || !(await users.AddToRoleAsync(owner, "PlatformOwner")).Succeeded) throw new InvalidOperationException("Fixture owner failed.");
                ownerId = owner.Id; ownerStamp = owner.SecurityStamp!;
                adminId = (await users.FindByEmailAsync("qa-admin-a@example.invalid"))!.Id;
                var id = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
                initialUsers = await id.Users.CountAsync(); initialRoles = await id.UserRoles.CountAsync();
            }
            async Task<(string access, string refresh)> Pair(string email)
            {
                using var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "Synthetic!39Ab" });
                if (response.StatusCode != HttpStatusCode.OK) throw new InvalidOperationException("Native fixture login failed.");
                using var data = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                return (data.RootElement.GetProperty("accessToken").GetString()!, data.RootElement.GetProperty("refreshToken").GetString()!);
            }
            var adminPair = await Pair("qa-admin-a@example.invalid");
            var ownerPair = await Pair("qa-session-owner@example.invalid");
            async Task Expect(string method, string path, int status, string? access = null, object? body = null)
            {
                using var request = new HttpRequestMessage(new HttpMethod(method), path);
                if (access is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", access);
                if (body is not null) request.Content = JsonContent.Create(body);
                using var response = await client.SendAsync(request);
                if ((int)response.StatusCode != status) throw new InvalidOperationException($"Native control {method} {path} expected {status}, got {(int)response.StatusCode}.");
                Console.WriteLine($"SESSIONBROWSER NATIVE {method} {path} {status}");
            }
            bool disabledVerified = false, passwordVerified = false;
            var builder = WebApplication.CreateSlimBuilder(); builder.Logging.ClearProviders(); builder.WebHost.UseUrls($"http://127.0.0.1:{port}");
            await using var bridge = builder.Build();
            bridge.Run(async context =>
            {
                if (context.Connection.RemoteIpAddress is null || !IPAddress.IsLoopback(context.Connection.RemoteIpAddress) || context.Request.Host.Host != "127.0.0.1" || context.Request.Host.Port != port) { context.Response.StatusCode = 400; return; }
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
                Console.WriteLine($"SESSIONBROWSER HTTP {context.Request.Method} {context.Request.Path} {(int)response.StatusCode}");
                if (context.Request.Method == "PATCH" && context.Request.Path == $"/api/platform/admins/{adminId}/active" && response.IsSuccessStatusCode)
                {
                    using var scope = factory.Services.CreateScope();
                    var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                    var admin = (await users.FindByIdAsync(adminId.ToString()))!;
                    var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
                    if (admin.IsActive || await db.PlatformAuditEntries.CountAsync(x => x.EntityId == adminId && x.Action == "Academy admin deactivated" && x.ActorUserId == ownerId) != 1) throw new InvalidOperationException("Browser disable persistence/audit failed.");
                    await Expect("GET", "/api/auth/session", 403, adminPair.access);
                    await Expect("POST", "/api/auth/login", 401, body: new { email = "qa-admin-a@example.invalid", password = "Synthetic!39Ab" });
                    await Expect("POST", "/api/auth/refresh", 401, body: new { refreshToken = adminPair.refresh });
                    await Expect("GET", "/api/auth/session", 200, ownerPair.access);
                    disabledVerified = true;
                    Console.WriteLine("SESSIONBROWSER DISABLE SQL PASS inactive identity and exactly one attributed platform audit; old access403, login/refresh401, unrelated Owner200.");
                }
                if (context.Request.Method == "POST" && context.Request.Path == "/api/auth/session/change-password" && response.IsSuccessStatusCode)
                {
                    using var scope = factory.Services.CreateScope();
                    var owner = await scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>().FindByIdAsync(ownerId.ToString());
                    if (owner is null || owner.SecurityStamp == ownerStamp) throw new InvalidOperationException("Password change stamp did not rotate.");
                    await Expect("GET", "/api/auth/session", 401, ownerPair.access);
                    await Expect("POST", "/api/auth/refresh", 401, body: new { refreshToken = ownerPair.refresh });
                    await Expect("POST", "/api/auth/login", 401, body: new { email = "qa-session-owner@example.invalid", password = "Synthetic!39Ab" });
                    passwordVerified = true;
                    Console.WriteLine("SESSIONBROWSER PASSWORD SQL PASS owner stamp rotated; pre-change access/refresh/login401; native controls contain no browser session injection.");
                }
                await response.Content.CopyToAsync(context.Response.Body, context.RequestAborted);
            });
            await bridge.StartAsync();
            var stop = Path.Combine(manifest.Root, "stop-browser");
            Console.WriteLine($"SESSIONBROWSER READY run={manifest.RunId:N} api={port} origin={origin.Port} stop={stop}");
            var deadline = DateTime.UtcNow.AddMinutes(30);
            while (!File.Exists(stop) && DateTime.UtcNow < deadline) await Task.Delay(1000);
            await bridge.StopAsync();
            using var finalScope = factory.Services.CreateScope();
            var finalIdentity = finalScope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            if (await finalIdentity.Users.CountAsync() != initialUsers || await finalIdentity.UserRoles.CountAsync() != initialRoles) throw new InvalidOperationException("Browser session fixture membership/count preservation failed.");
            if (!disabledVerified) throw new InvalidOperationException("Browser disable action not accepted.");
            Console.WriteLine($"SESSIONBROWSER FINAL disableVerified={disabledVerified} passwordNativeVerified={passwordVerified}; original user/role counts preserved; browser observation acceptance recorded separately.");
        }
        finally
        {
            if (Directory.Exists(manifest.Root)) QaRunGuard.CleanupHostFolders(manifest, root => { Directory.Delete(root, recursive: true); return true; });
        }
    }
}
