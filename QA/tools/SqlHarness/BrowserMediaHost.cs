using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Domain.Identity;
using AcademyDesk.Api.Infrastructure.Media;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

internal static partial class SqlHarnessEntryPoint
{
    // Test-only loopback bridge to the real Program/TestServer/Identity/SQL graph.
    // No synthetic API replies, account tokens in URLs, remote target, or test
    // management routes. It ends through an exact owned disk marker or timeout.
    private static async Task ServeBrowserMediaAsync(QaRunManifest manifest, QaBlobFixture fixture)
    {
        if (!int.TryParse(Environment.GetEnvironmentVariable("QA_BROWSER_API_PORT"), out var port) || port is < 1024 or > 65535 ||
            !Uri.TryCreate(Environment.GetEnvironmentVariable("QA_BROWSER_ORIGIN"), UriKind.Absolute, out var origin) ||
            origin.Scheme != "http" || origin.Host != "127.0.0.1" || origin.Port is < 1024 or > 65535 || origin.Port == port ||
            origin.AbsolutePath != "/" || origin.Query != "" || origin.Fragment != "" || origin.UserInfo != "")
            throw new InvalidOperationException("Exact distinct loopback browser API port and web origin required.");
        var stopPath = Path.Combine(manifest.Root, "stop-browser");
        var pauseSetting = Environment.GetEnvironmentVariable("QA_BROWSER_PAUSE_CHUNK_MS");
        var pauseMilliseconds = 0;
        if (pauseSetting is not null && (!int.TryParse(pauseSetting, out pauseMilliseconds) || pauseMilliseconds is < 1000 or > 30000))
            throw new InvalidOperationException("Browser chunk pacing must be 1000–30000 ms when enabled.");
        var pauseUsed = 0;
        var failureSetting = Environment.GetEnvironmentVariable("QA_BROWSER_FAIL_STAGE_ONCE");
        if (failureSetting is not null && failureSetting != "1") throw new InvalidOperationException("Browser provider failure must be explicitly enabled with 1.");
        var failureStore = failureSetting == "1" ? new BrowserFailureBlobStore(fixture.Store) : null;
        var completionLossSetting = Environment.GetEnvironmentVariable("QA_BROWSER_LOSE_COMPLETION_ONCE");
        if (completionLossSetting is not null && completionLossSetting != "1") throw new InvalidOperationException("Completion loss must be explicitly enabled with 1.");
        if (completionLossSetting is not null && failureStore is not null) throw new InvalidOperationException("Browser fault modes must be isolated.");
        var completionLoss = completionLossSetting == "1" ? new BrowserCompletionLoss() : null;
        var revocationSetting = Environment.GetEnvironmentVariable("QA_BROWSER_REVOKE_AFTER_CHUNK");
        if (revocationSetting is not null && (revocationSetting != "1" || pauseMilliseconds == 0 || failureStore is not null || completionLoss is not null))
            throw new InvalidOperationException("Revocation requires explicit isolated paced browser mode.");
        var revocation = revocationSetting == "1" ? new BrowserUploadRevocation() : null;
        try
        {
            using var factory = new QaApiFactory(manifest,
                new Dictionary<string, string?> { ["Cors:AllowedOrigins:0"] = origin.GetLeftPart(UriPartial.Authority) }, (IMediaBlobStore?)failureStore ?? fixture.Store);
            using var client = factory.CreateClient(new() { AllowAutoRedirect = false });
            if (!factory.PreflightPassed || (await client.GetAsync("/health")).StatusCode != HttpStatusCode.OK)
                throw new InvalidOperationException("Browser real backend health/preflight failed.");
            RecordRuntimeEndpoints(factory, manifest);
            await VerifyTenantIsolationAsync(factory, client);
            client.DefaultRequestHeaders.Authorization = null;
            await SeedBrowserRecipientsAsync(factory);

            var builder = WebApplication.CreateSlimBuilder();
            builder.Logging.ClearProviders(); // No credentials/request bodies in bridge logs.
            builder.WebHost.UseUrls($"http://127.0.0.1:{port}");
            builder.Services.Configure<Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerOptions>(o => o.Limits.MaxRequestBodySize = 9 * 1024 * 1024);
            await using var bridge = builder.Build();
            bridge.Run(async context =>
            {
                if (context.Connection.RemoteIpAddress is null || !IPAddress.IsLoopback(context.Connection.RemoteIpAddress) ||
                    context.Request.Host.Host != "127.0.0.1" || context.Request.Host.Port != port)
                { context.Response.StatusCode = 400; return; }
                // Deterministic test transport pacing only: hold the first second
                // chunk before forwarding. All data/auth still uses the real API.
                if (pauseMilliseconds > 0 && HttpMethods.IsPut(context.Request.Method) &&
                    context.Request.Path.StartsWithSegments("/api/teacher/media-uploads") &&
                    context.Request.Path.Value!.EndsWith("/chunks/1", StringComparison.Ordinal) &&
                    Interlocked.CompareExchange(ref pauseUsed, 1, 0) == 0)
                {
                    Console.WriteLine($"BROWSER PACING second chunk held {pauseMilliseconds} ms before real API forwarding.");
                    try { await Task.Delay(pauseMilliseconds, context.RequestAborted); }
                    catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
                    { Console.WriteLine("BROWSER PACING request aborted before forwarding; no simulated API response."); return; }
                }
                using var request = new HttpRequestMessage(new HttpMethod(context.Request.Method), context.Request.Path + context.Request.QueryString);
                if (context.Request.ContentLength > 0 || context.Request.Headers.ContainsKey("Transfer-Encoding"))
                    request.Content = new StreamContent(context.Request.Body);
                foreach (var header in context.Request.Headers)
                {
                    if (header.Key.Equals("Host", StringComparison.OrdinalIgnoreCase) || header.Key.Equals("Connection", StringComparison.OrdinalIgnoreCase) ||
                        header.Key.Equals("Transfer-Encoding", StringComparison.OrdinalIgnoreCase)) continue;
                    if (!request.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray()))
                        request.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
                }
                using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, context.RequestAborted);
                if (revocation is not null && response.StatusCode == HttpStatusCode.OK && HttpMethods.IsPut(context.Request.Method) &&
                    context.Request.Path.StartsWithSegments("/api/teacher/media-uploads") && context.Request.Path.Value!.EndsWith("/chunks/0", StringComparison.Ordinal))
                {
                    if (!Guid.TryParse(context.Request.Path.Value.Split('/')[^3], out var stagedId)) throw new InvalidOperationException("Unexpected chunk path.");
                    using var revokeScope = factory.Services.CreateScope();
                    await revocation.RevokeAndProbeAsync(stagedId, revokeScope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(), client,
                        request.Headers.Authorization, fixture.Store);
                }
                if (revocation is not null && response.StatusCode == HttpStatusCode.BadRequest && HttpMethods.IsPost(context.Request.Method) &&
                    context.Request.Path == "/api/teacher/media-uploads")
                {
                    using var deniedScope = factory.Services.CreateScope();
                    await revocation.ObserveDeniedCreateAsync(deniedScope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(), fixture.Store);
                }
                var dropCompletion = false;
                if (completionLoss is not null && response.StatusCode == HttpStatusCode.OK && HttpMethods.IsPost(context.Request.Method) &&
                    context.Request.Path.StartsWithSegments("/api/teacher/media-uploads") &&
                    context.Request.Path.Value!.EndsWith("/complete", StringComparison.Ordinal))
                {
                    var segments = context.Request.Path.Value.Split('/');
                    if (!Guid.TryParse(segments[^2], out var completedId)) throw new InvalidOperationException("Unexpected completion path.");
                    using var completedScope = factory.Services.CreateScope();
                    dropCompletion = await completionLoss.ObserveAsync(completedId,
                        completedScope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(), fixture.Store);
                    Console.WriteLine($"BROWSER HTTP backend POST {context.Request.Path} 200 durable=True");
                }
                context.Response.StatusCode = (int)response.StatusCode;
                foreach (var header in response.Headers.Concat(response.Content.Headers))
                {
                    if (header.Key.Equals("Transfer-Encoding", StringComparison.OrdinalIgnoreCase) || header.Key.Equals("Connection", StringComparison.OrdinalIgnoreCase)) continue;
                    context.Response.Headers[header.Key] = header.Value.ToArray();
                }
                // Log only method, path and status; never query, body, identity or credentials.
                Console.WriteLine($"BROWSER HTTP {context.Request.Method} {context.Request.Path} {(int)response.StatusCode}");
                if (dropCompletion)
                {
                    // Forward the real response headers and first body byte, then
                    // abort. A before-headers abort can be transparently retried by
                    // Chromium and does not exercise the visible uncertain state.
                    using var body = await response.Content.ReadAsStreamAsync(context.RequestAborted);
                    var first = new byte[1];
                    if (await body.ReadAsync(first, context.RequestAborted) != 1) throw new InvalidOperationException("Completion response unexpectedly empty.");
                    await context.Response.Body.WriteAsync(first, context.RequestAborted);
                    await context.Response.Body.FlushAsync(context.RequestAborted);
                    await Task.Delay(100, context.RequestAborted);
                    Console.WriteLine($"BROWSER FAULT completion body truncated after first real byte; upload={completionLoss!.UploadId}");
                    context.Abort();
                    return;
                }
                if (failureStore is not null && response.StatusCode == HttpStatusCode.ServiceUnavailable &&
                    HttpMethods.IsPut(context.Request.Method) && context.Request.Path.Value!.EndsWith("/chunks/0", StringComparison.Ordinal))
                {
                    using var failureScope = factory.Services.CreateScope();
                    await failureStore.AssertStateAsync(failureScope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(), completed: false);
                }
                await response.Content.CopyToAsync(context.Response.Body, context.RequestAborted);
            });
            await bridge.StartAsync();
            Console.WriteLine($"BROWSER READY run={manifest.RunId:N} api=http://127.0.0.1:{port} origin={origin.GetLeftPart(UriPartial.Authority)} stop={stopPath}");
            var deadline = DateTime.UtcNow.AddMinutes(30);
            while (!File.Exists(stopPath) && DateTime.UtcNow < deadline) await Task.Delay(1000);
            await bridge.StopAsync();
            // Post-browser observations, not implicit browser assertions.
            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            if (failureStore is not null) await failureStore.AssertStateAsync(db, completed: true);
            if (completionLoss is not null) await completionLoss.AssertStateAsync(db, fixture.Store, expectedResponses: 2);
            if (revocation is not null) await revocation.AssertStateAsync(db, fixture.Store, requireBrowserRetry: true);
            Console.WriteLine("BROWSER SQL " + JsonSerializer.Serialize(await db.ClassMediaUploadSessions.AsNoTracking()
                .Select(x => new { x.Id, x.CompletedAtUtc, x.Length, x.FileName }).ToArrayAsync()));
            Console.WriteLine($"BROWSER STOP {(File.Exists(stopPath) ? "owned marker" : "30-minute timeout")}; real host disposed before cleanup.");
        }
        finally
        {
            if (Directory.Exists(manifest.Root))
                QaRunGuard.CleanupHostFolders(manifest, root => { Directory.Delete(root, recursive: true); return true; });
        }
    }

    private static async Task SeedBrowserRecipientsAsync(QaApiFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
        var academy = await db.Academies.Where(x => x.Name == "Synthetic Academy A").Select(x => x.Id).SingleAsync();
        var batch = await db.Batches.Where(x => x.AcademyId == academy).SingleAsync();
        var student = await db.Students.Where(x => x.AcademyId == academy && x.FirstName == "Isolated-A").SingleAsync();
        var guardianId = Guid.NewGuid();
        db.Enrollments.Add(new Enrollment { AcademyId = academy, BatchId = batch.Id, StudentId = student.Id });
        db.ClassSessions.Add(new ClassSession { AcademyId = academy, BatchId = batch.Id, TeacherId = batch.TeacherId,
            StartUtc = DateTime.UtcNow.AddMinutes(5), EndUtc = DateTime.UtcNow.AddMinutes(65), DeliveryMode = "Online" });
        db.Guardians.Add(new Guardian { Id = guardianId, AcademyId = academy, FirstName = "Synthetic", LastName = "Guardian" });
        db.StudentGuardians.Add(new StudentGuardian { AcademyId = academy, StudentId = student.Id, GuardianId = guardianId, CanAccessPortal = true });
        await db.SaveChangesAsync();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        // Second same-tenant Teacher for account-switch UI checks. Separate
        // assigned batch/session; no broadening of Teacher A's authorization.
        var secondTeacher = new Teacher { AcademyId = academy, FirstName = "Synthetic", LastName = "Teacher B" };
        var secondBatch = new Batch { AcademyId = academy, CourseId = batch.CourseId,
            TeacherId = secondTeacher.Id, Name = "Synthetic B Batch" };
        db.Teachers.Add(secondTeacher);
        db.Batches.Add(secondBatch);
        db.ClassSessions.Add(new ClassSession { AcademyId = academy, BatchId = secondBatch.Id, TeacherId = secondTeacher.Id,
            StartUtc = DateTime.UtcNow.AddMinutes(5), EndUtc = DateTime.UtcNow.AddMinutes(65), DeliveryMode = "Online" });
        await db.SaveChangesAsync();
        var secondUser = new ApplicationUser { UserName = "qa-browser-teacher-b@example.invalid", Email = "qa-browser-teacher-b@example.invalid",
            EmailConfirmed = true, IsActive = true, DisplayName = "Synthetic Teacher B", AcademyId = academy, TeacherId = secondTeacher.Id };
        if (!(await users.CreateAsync(secondUser, "Synthetic!39Ab")).Succeeded || !(await users.AddToRoleAsync(secondUser, "Teacher")).Succeeded)
            throw new InvalidOperationException("Second browser Teacher fixture failed.");
        // Authenticated Teacher role without a domain profile: real /teacher/me
        // must return 403, distinguishable from anonymous/expired-session 401.
        var unlinkedUser = new ApplicationUser { UserName = "qa-browser-unlinked-teacher@example.invalid", Email = "qa-browser-unlinked-teacher@example.invalid",
            EmailConfirmed = true, IsActive = true, DisplayName = "Synthetic unlinked Teacher", AcademyId = academy };
        if (!(await users.CreateAsync(unlinkedUser, "Synthetic!39Ab")).Succeeded || !(await users.AddToRoleAsync(unlinkedUser, "Teacher")).Succeeded)
            throw new InvalidOperationException("Unlinked browser Teacher fixture failed.");
        foreach (var item in new[] { (Email: "qa-browser-student@example.invalid", Role: "Student", Student: (Guid?)student.Id, Guardian: (Guid?)null),
            (Email: "qa-browser-guardian@example.invalid", Role: "Guardian", Student: (Guid?)null, Guardian: (Guid?)guardianId) })
        {
            if (!await roles.RoleExistsAsync(item.Role) && !(await roles.CreateAsync(new ApplicationRole { Name = item.Role })).Succeeded)
                throw new InvalidOperationException("Browser fixture role failed.");
            var user = new ApplicationUser { UserName = item.Email, Email = item.Email, EmailConfirmed = true, IsActive = true,
                DisplayName = "Synthetic browser recipient", AcademyId = academy, StudentId = item.Student, GuardianId = item.Guardian };
            if (!(await users.CreateAsync(user, "Synthetic!39Ab")).Succeeded || !(await users.AddToRoleAsync(user, item.Role)).Succeeded)
                throw new InvalidOperationException("Browser fixture Identity failed.");
        }
        Console.WriteLine("BROWSER fixture PASS: two same-tenant Teachers with separate assigned batches, active enrolled student, document-authorized guardian; synthetic only.");
    }
}
