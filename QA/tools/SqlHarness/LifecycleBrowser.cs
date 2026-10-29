using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
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
    private static async Task VerifyLifecycleBrowserAsync(QaApiFactory factory, HttpClient client, QaRunManifest manifest)
    {
        Guid academy, teacher, admin;
        var child = new Student { FirstName = "Browser", LastName = "Permitted Minor", DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-12)) };
        var guardian = new Guardian { FirstName = "Browser", LastName = "Guardian" };
        StudentGuardian link;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            academy = await db.Academies.Where(x => x.Name == "Synthetic Academy A").Select(x => x.Id).SingleAsync();
            teacher = await db.Teachers.Where(x => x.AcademyId == academy && x.FirstName == "Synthetic").Select(x => x.Id).SingleAsync();
            admin = (await scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>().FindByEmailAsync("qa-admin-a@example.invalid"))!.Id;
            child.AcademyId = academy; guardian.AcademyId = academy;
            link = new StudentGuardian { AcademyId = academy, StudentId = child.Id, GuardianId = guardian.Id, Relationship = "Parent", IsPrimary = true, CanAccessPortal = true, CanViewAcademicProgress = true, CanViewFinance = true, CanViewDocuments = true, CanManageLeave = true, AccessGrantedAtUtc = DateTime.UtcNow };
            db.AddRange(child, guardian, link); await db.SaveChangesAsync();
        }
        var actor = await CreateAccessActorAsync(factory, "qa-browser-parent@example.invalid", "Guardian", academy);
        using (var scope = factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = (await users.FindByIdAsync(actor.ToString()))!; user.GuardianId = guardian.Id;
            if (!(await users.UpdateAsync(user)).Succeeded) throw new InvalidOperationException("Browser guardian Identity fixture failed.");
        }
        Console.WriteLine("REVOCATION BROWSER waiting unchanged limiter window; retained62 Teacher cases precede linked UI.");
        await Task.Delay(TimeSpan.FromSeconds(60));
        var adminToken = await LoginAsync(client, "qa-admin-a@example.invalid", "Synthetic!39Ab");
        client.DefaultRequestHeaders.Authorization = null;
        var failures = new ConcurrentQueue<string>(); var reads = 0; var controls = 0; var teacherDenials = 0; var guardianDenials = 0;
        var builder = WebApplication.CreateSlimBuilder(); builder.Logging.ClearProviders(); builder.WebHost.UseUrls("http://127.0.0.1:0");
        await using var bridge = builder.Build(); using var pacing = new SemaphoreSlim(1, 1);
        bridge.Run(async context =>
        {
            if (context.Connection.RemoteIpAddress is null || !IPAddress.IsLoopback(context.Connection.RemoteIpAddress) || context.Request.Host.Host != "127.0.0.1") { context.Response.StatusCode = 400; return; }
            var route = context.Request.Path.ToString(); var control = route.StartsWith("/__qa/lifecycle/", StringComparison.Ordinal);
            if (control && (context.Request.Method != "POST" || context.Request.Headers["X-QA-Run"] != manifest.RunId.ToString("N"))) { context.Response.StatusCode = 403; return; }
            if (!control && context.Request.Method is not ("GET" or "OPTIONS") && !(context.Request.Method == "POST" && route == "/api/auth/login")) { context.Response.StatusCode = 405; return; }
            await pacing.WaitAsync(context.RequestAborted);
            try
            {
                await Task.Delay(650, context.RequestAborted);
                var before = await LifecycleOverlapState(factory, manifest);
                if (control)
                {
                    var parts = route.Split('/');
                    if (parts.Length != 5 || parts[3] is not ("teacher" or "guardian") || parts[4] is not ("active" or "revoked")) throw new InvalidOperationException("Unknown lifecycle control.");
                    var active = parts[4] == "active";
                    if (parts[3] == "teacher")
                    {
                        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
                        var row = await db.Teachers.SingleAsync(x => x.Id == teacher); row.IsActive = active; await db.SaveChangesAsync();
                        var after = await LifecycleOverlapState(factory, manifest); LifecycleSame(before, after, "teachers");
                        var expected = before.GetProperty("teachers").EnumerateArray().Select(x =>
                        {
                            if (x.GetProperty("Id").GetGuid() != teacher) return x;
                            var original = JsonNode.Parse(x.GetRawText())!; original["IsActive"] = active;
                            return JsonSerializer.SerializeToElement(original);
                        }).ToArray();
                        if (!JsonElement.DeepEquals(JsonSerializer.SerializeToElement(expected), after.GetProperty("teachers"))) throw new InvalidOperationException("Teacher control changed more than selected authority.");
                    }
                    else
                    {
                        var endpoint = $"/api/academies/{academy}/students/{child.Id}/guardians/{guardian.Id}/portal-access";
                        using var response = await LifecycleHttp(client, new(HttpMethod.Patch, endpoint) { Content = JsonContent.Create(new { allowPortalAccess = active, allowAcademicProgress = active, allowFinance = active, allowDocuments = active, allowLeave = active }) }, adminToken);
                        if (response.StatusCode != HttpStatusCode.OK) throw new InvalidOperationException("Browser guardian permission command failed.");
                        var after = await LifecycleOverlapState(factory, manifest); LifecycleSame(before, after, "links", "audits");
                        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
                        var saved = await db.StudentGuardians.AsNoTracking().SingleAsync(x => x.Id == link.Id);
                        if (saved.CanAccessPortal != active || saved.CanViewAcademicProgress != active || saved.CanViewFinance != active || saved.CanViewDocuments != active || saved.CanManageLeave != active || saved.StudentId != child.Id || saved.GuardianId != guardian.Id || saved.Relationship != "Parent" || !saved.IsPrimary || (active ? saved.AccessRevokedAtUtc is not null || saved.AccessGrantedAtUtc is null : saved.AccessGrantedAtUtc is not null || saved.AccessRevokedAtUtc is null)) throw new InvalidOperationException("Guardian control authority differs from SQL.");
                        var oldLinks = before.GetProperty("links").EnumerateArray().Where(x => x.GetProperty("Id").GetGuid() != link.Id).ToArray();
                        var otherLinks = after.GetProperty("links").EnumerateArray().Where(x => x.GetProperty("Id").GetGuid() != link.Id).ToArray();
                        if (!JsonElement.DeepEquals(JsonSerializer.SerializeToElement(oldLinks), JsonSerializer.SerializeToElement(otherLinks))) throw new InvalidOperationException("Guardian control changed other grants.");
                        var oldAudits = before.GetProperty("audits").EnumerateArray().ToArray(); var audits = after.GetProperty("audits").EnumerateArray().ToArray();
                        if (audits.Length != oldAudits.Length + 1 || oldAudits.Any(x => !audits.Any(y => JsonElement.DeepEquals(x, y)))) throw new InvalidOperationException("Guardian command audit mismatch.");
                        var added = audits.Single(x => !oldAudits.Any(y => JsonElement.DeepEquals(x, y)));
                        if (added.GetProperty("ActorUserId").GetGuid() != admin || added.GetProperty("AcademyId").GetGuid() != academy || added.GetProperty("Action").GetString() != "PATCH StudentGuardians" || JsonDocument.Parse(added.GetProperty("MetadataJson").GetString()!).RootElement.GetProperty("Route").GetString() != endpoint) throw new InvalidOperationException("Guardian command audit attribution differs.");
                    }
                    controls++; context.Response.StatusCode = 200; await context.Response.WriteAsync("{}"); return;
                }
                var body = await new StreamReader(context.Request.Body).ReadToEndAsync(context.RequestAborted);
                using var request = new HttpRequestMessage(new HttpMethod(context.Request.Method), context.Request.Path + context.Request.QueryString);
                if (body.Length > 0) request.Content = new StringContent(body);
                foreach (var header in context.Request.Headers)
                {
                    if (header.Key is "Host" or "Connection" or "Transfer-Encoding" or "Content-Length") continue;
                    if (!request.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray()) && request.Content is not null) { request.Content.Headers.Remove(header.Key); request.Content.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray()); }
                }
                using var real = await client.SendAsync(request, context.RequestAborted); var bytes = await real.Content.ReadAsByteArrayAsync(context.RequestAborted);
                if (real.StatusCode == HttpStatusCode.TooManyRequests) throw new InvalidOperationException("Lifecycle browser cannot pass with429.");
                LifecycleSame(before, await LifecycleOverlapState(factory, manifest)); reads++;
                if (real.StatusCode == HttpStatusCode.Forbidden && route == "/api/teacher/me") teacherDenials++;
                if (real.StatusCode == HttpStatusCode.Forbidden && route == $"/api/portal/students/{child.Id}") guardianDenials++;
                context.Response.StatusCode = (int)real.StatusCode;
                foreach (var header in real.Headers.Concat(real.Content.Headers)) if (header.Key is not ("Transfer-Encoding" or "Connection")) context.Response.Headers[header.Key] = header.Value.ToArray();
                await context.Response.Body.WriteAsync(bytes, context.RequestAborted);
            }
            catch (Exception error) { failures.Enqueue(error.Message); throw; }
            finally { pacing.Release(); }
        });
        await bridge.StartAsync();
        var start = new ProcessStartInfo("node") { WorkingDirectory = Directory.GetCurrentDirectory(), UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true };
        foreach (var arg in new[] { Path.GetFullPath("QA/tools/lifecycle-browser-live.cjs"), bridge.Urls.Single(), manifest.RunId.ToString("N"), child.Id.ToString() }) start.ArgumentList.Add(arg);
        using var process = Process.Start(start) ?? throw new InvalidOperationException("Lifecycle browser process unavailable.");
        var output = process.StandardOutput.ReadToEndAsync(); var errors = process.StandardError.ReadToEndAsync();
        try { using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(8)); await process.WaitForExitAsync(timeout.Token); }
        catch { if (!process.HasExited) process.Kill(entireProcessTree: true); throw; }
        finally { await bridge.StopAsync(); }
        Console.Write(await output);
        // Four revoked UI reloads plus four explicit Teacher probes; guardian
        // Me returns200 with zero children, plus four own-child403 probes.
        if (process.ExitCode != 0 || !failures.IsEmpty || controls != 16 || teacherDenials != 8 || guardianDenials != 4) throw new InvalidOperationException($"Lifecycle browser failed controls={controls}/teacher403={teacherDenials}/guardian403={guardianDenials}: " + await errors + string.Join("; ", failures));
        Console.WriteLine($"REVOCATION BROWSER SQL PASS:8 journeys/24 reload states/16 controls/eight Teacher403/four explicit guardian403/{reads} unchanged15-field reads-login-denials/no429. Physical devices NOT RUN.");
    }
}
