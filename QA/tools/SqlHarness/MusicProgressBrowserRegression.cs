using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Text.Json;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

internal static partial class SqlHarnessEntryPoint
{
    private static async Task VerifyMusicProgressBrowserAsync(QaApiFactory factory, HttpClient client, Guid academy, Guid progressId)
    {
        var failures = new ConcurrentQueue<string>(); var writes = 0;
        var path = $"/api/academies/{academy}/music-progress/{progressId}";
        var builder = WebApplication.CreateSlimBuilder(); builder.Logging.ClearProviders(); builder.WebHost.UseUrls("http://127.0.0.1:0");
        Console.WriteLine("PASS: MUSIC BROWSER waiting one unchanged production limiter window before linked checks.");
        await Task.Delay(TimeSpan.FromSeconds(60));
        using var pacing = new SemaphoreSlim(1, 1); await using var bridge = builder.Build();
        bridge.Run(async context =>
        {
            if (context.Connection.RemoteIpAddress is null || !IPAddress.IsLoopback(context.Connection.RemoteIpAddress) || context.Request.Host.Host != "127.0.0.1") { context.Response.StatusCode = 400; return; }
            var write = context.Request.Method == "PATCH" && context.Request.Path == path;
            var login = context.Request.Method == "POST" && context.Request.Path == "/api/auth/login";
            if (context.Request.Method is not ("GET" or "OPTIONS") && !write && !login) { context.Response.StatusCode = 405; return; }
            await pacing.WaitAsync(context.RequestAborted);
            try { await Task.Delay(750, context.RequestAborted); } finally { pacing.Release(); }
            using var request = new HttpRequestMessage(new HttpMethod(context.Request.Method), context.Request.Path + context.Request.QueryString);
            if (context.Request.ContentLength > 0 || context.Request.Headers.ContainsKey("Transfer-Encoding")) request.Content = new StreamContent(context.Request.Body);
            foreach (var header in context.Request.Headers)
            {
                if (header.Key.Equals("Host", StringComparison.OrdinalIgnoreCase) || header.Key.Equals("Connection", StringComparison.OrdinalIgnoreCase) || header.Key.Equals("Transfer-Encoding", StringComparison.OrdinalIgnoreCase)) continue;
                if (!request.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray())) request.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
            }
            using var response = await client.SendAsync(request, context.RequestAborted); var bytes = await response.Content.ReadAsByteArrayAsync(context.RequestAborted);
            if (response.StatusCode == HttpStatusCode.TooManyRequests) failures.Enqueue("HTTP429 not accepted.");
            if (write)
            {
                Interlocked.Increment(ref writes);
                if (response.StatusCode != HttpStatusCode.OK) failures.Enqueue("Linked status update not successful.");
                else
                {
                    using var json = JsonDocument.Parse(bytes); var result = json.RootElement;
                    using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
                    var row = await db.StudentMusicProgress.AsNoTracking().SingleAsync(x => x.Id == progressId);
                    if (row.AcademyId != academy || row.Score != 0 || row.Status != result.GetProperty("status").GetString() || row.Notes != "Retain synthetic notes" || row.TargetDate != new DateOnly(2026, 10, 20)) failures.Enqueue("Linked fresh SQL fields mismatch.");
                    var foreign = await db.StudentMusicProgress.AsNoTracking().SingleAsync(x => x.AcademyId != academy);
                    if (foreign.Score != 17 || foreign.Notes != "Foreign sentinel" || await db.StudentMusicProgress.CountAsync() != 2) failures.Enqueue("Foreign sentinel/count changed.");
                }
            }
            context.Response.StatusCode = (int)response.StatusCode;
            foreach (var header in response.Headers.Concat(response.Content.Headers)) if (!header.Key.Equals("Transfer-Encoding", StringComparison.OrdinalIgnoreCase) && !header.Key.Equals("Connection", StringComparison.OrdinalIgnoreCase)) context.Response.Headers[header.Key] = header.Value.ToArray();
            Console.WriteLine($"PASS: MUSIC BROWSER HTTP {context.Request.Method} {context.Request.Path} {(int)response.StatusCode}");
            await context.Response.Body.WriteAsync(bytes, context.RequestAborted);
        });
        await bridge.StartAsync();
        var start = new ProcessStartInfo("node") { WorkingDirectory = Directory.GetCurrentDirectory(), UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true };
        start.ArgumentList.Add(Path.GetFullPath("QA/tools/music-progress-live.cjs")); start.ArgumentList.Add(bridge.Urls.Single()); start.ArgumentList.Add(academy.ToString()); start.ArgumentList.Add(progressId.ToString());
        using var process = Process.Start(start) ?? throw new InvalidOperationException("Linked music browser did not start.");
        var output = process.StandardOutput.ReadToEndAsync(); var errors = process.StandardError.ReadToEndAsync();
        try { using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(4)); await process.WaitForExitAsync(timeout.Token); }
        catch { if (!process.HasExited) process.Kill(entireProcessTree: true); throw; }
        Console.Write(await output); var errorText = await errors; await bridge.StopAsync();
        if (process.ExitCode != 0 || !failures.IsEmpty || writes != 8) throw new InvalidOperationException("Linked music browser failed: " + errorText + string.Join("; ", failures));
        Console.WriteLine("PASS: MUSIC BROWSER four linked journeys/eight real PATCH200; fresh SQL zero/date/notes and foreign sentinel preserved; no429; physical devices NOT RUN.");
    }
}
