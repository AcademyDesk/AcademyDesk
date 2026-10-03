using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Identity;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

internal static partial class SqlHarnessEntryPoint
{
    private static async Task VerifySessionRefreshAsync(QaApiFactory factory, HttpClient client, QaRunManifest manifest, bool concurrency = false)
    {
        client.DefaultRequestHeaders.Authorization = null;
        Guid academyId, studentId;
        int userCount, auditCount, membershipCount;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            academyId = await db.Academies.Where(x => x.Name == "Synthetic Academy A").Select(x => x.Id).SingleAsync();
            studentId = await db.Students.Where(x => x.AcademyId == academyId && x.FirstName == "Isolated-A").Select(x => x.Id).SingleAsync();
            userCount = await scope.ServiceProvider.GetRequiredService<IdentityDbContext>().Users.CountAsync();
            membershipCount = await scope.ServiceProvider.GetRequiredService<IdentityDbContext>().UserRoles.CountAsync();
            auditCount = await db.AuditLogs.CountAsync();
        }
        var sessions = new List<object>();
        var options = factory.Services.GetRequiredService<IOptionsMonitor<BearerTokenOptions>>().Get(IdentityConstants.BearerScheme);
        foreach (var (email, workspace) in new[] { ("qa-admin-a@example.invalid", "AcademyAdmin"), ("qa-teacher-a@example.invalid", "Teacher") })
        {
            using var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "Synthetic!39Ab" });
            if (!login.IsSuccessStatusCode) throw new InvalidOperationException("Synthetic native login failed.");
            using var data = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
            var access = data.RootElement.GetProperty("accessToken").GetString()!;
            var refresh = data.RootElement.GetProperty("refreshToken").GetString()!;
            var accessTicket = options.BearerTokenProtector.Unprotect(access) ?? throw new InvalidOperationException("Native access ticket missing.");
            var refreshTicket = options.RefreshTokenProtector.Unprotect(refresh) ?? throw new InvalidOperationException("Native refresh ticket missing.");
            // QA-only expired signed native tickets. Production clocks/options and claims unchanged.
            accessTicket.Properties.ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(-1);
            refreshTicket.Properties.ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(-1);
            sessions.Add(new { workspace, expiredAccess = options.BearerTokenProtector.Protect(accessTicket), refreshToken = refresh, expiredRefresh = options.RefreshTokenProtector.Protect(refreshTicket) });
        }
        var builder = WebApplication.CreateSlimBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.ConfigureKestrel(o => o.Listen(IPAddress.Loopback, 0));
        await using var bridge = builder.Build();
        bridge.Run(async context =>
        {
            if (context.Connection.RemoteIpAddress is null || !IPAddress.IsLoopback(context.Connection.RemoteIpAddress) || context.Request.Host.Host != "127.0.0.1") { context.Response.StatusCode = 400; return; }
            using var request = new HttpRequestMessage(new HttpMethod(context.Request.Method), context.Request.Path + context.Request.QueryString);
            if (context.Request.ContentLength > 0) request.Content = new StreamContent(context.Request.Body);
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
            await response.Content.CopyToAsync(context.Response.Body, context.RequestAborted);
        });
        await bridge.StartAsync();
        var origin = bridge.Urls.Single();
        Console.WriteLine((concurrency ? "REFRESHFLIGHT BRIDGE " : "SESSIONREFRESH BRIDGE ") + origin);
        using var process = new Process { StartInfo = new ProcessStartInfo("node") { UseShellExecute = false, RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true } };
        process.StartInfo.ArgumentList.Add(concurrency ? "QA/tools/refresh-concurrency-http.cjs" : "QA/tools/session-refresh-http.cjs");
        process.Start();
        var output = process.StandardOutput.ReadToEndAsync();
        var errors = process.StandardError.ReadToEndAsync();
        await process.StandardInput.WriteAsync(JsonSerializer.Serialize(new { origin, academyId, studentId, sessions }));
        process.StandardInput.Close();
        try { using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(45)); await process.WaitForExitAsync(timeout.Token); }
        catch (OperationCanceledException) { process.Kill(); throw new InvalidOperationException("Owned client timed out."); }
        Console.Write(await output); Console.Write(await errors);
        await bridge.StopAsync();
        if (process.ExitCode != 0) throw new InvalidOperationException("Native session-refresh client failed.");
        using var finalScope = factory.Services.CreateScope();
        var identity = finalScope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var finalDb = finalScope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
        var user = await identity.Users.SingleAsync(x => x.Email == "qa-refresh-student@example.invalid");
        var roleId = await identity.Roles.Where(x => x.Name == "Student").Select(x => x.Id).SingleAsync();
        if (user.AcademyId != academyId || user.StudentId != studentId || user.GuardianId is not null || user.TeacherId is not null ||
            !await identity.UserRoles.AnyAsync(x => x.UserId == user.Id && x.RoleId == roleId) ||
            await identity.Users.CountAsync() != userCount + 1 || await identity.UserRoles.CountAsync() != membershipCount + 1 || await finalDb.AuditLogs.CountAsync() != auditCount + 1)
            throw new InvalidOperationException("Refreshed POST did not persist exactly one typed student identity/membership/success audit.");
        Console.WriteLine(concurrency
            ? "REFRESHFLIGHT SQL PASS exactly one new Student identity/membership/typed academy link/success audit;25 helper requests plus two fixture logins;controlled native overlaps/late401 verified,no cross-tab/browser/lifetime stress acceptance."
            : "SESSIONREFRESH SQL PASS exactly one new Student identity/membership/typed academy link/success audit;11 helper requests plus two fixture logins; native expiries200/401 verified, no browser/device/lifetime stress acceptance.");
    }
}
