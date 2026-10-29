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
    // Native browser permission/recorder gate. Fake audio device, no host microphone.
    // The relay only accepts reads and real login; no upload or domain mutation.
    private static async Task VerifyRecorderBrowserAsync(QaApiFactory factory, HttpClient client, QaBlobFixture fixture, QaRunManifest manifest)
    {
        if (Environment.GetEnvironmentVariable("QA_MEDIA_RECORDER_UPLOAD") == "1") { await VerifyRecorderUploadAsync(factory, client, fixture, manifest); return; }
        var recovery = Environment.GetEnvironmentVariable("QA_MEDIA_RECORDER_RECOVERY") == "1";
        await SeedBrowserRecipientsAsync(factory);
        client.DefaultRequestHeaders.Authorization = null;
        async Task<JsonElement> State()
        {
            using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var blobs = new List<object>();
            await foreach (var blob in fixture.Container.GetBlobsAsync())
                blobs.Add(new { blob.Name, etag = blob.Properties.ETag?.ToString(), blob.Properties.ContentLength, blob.Properties.LastModified });
            return JsonSerializer.SerializeToElement(new { protectedState = await LifecycleOverlapState(factory, manifest),
                uploads = await db.ClassMediaUploadSessions.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(), blobs });
        }
        var initial = await State(); var requests = 0; var deniedWrites = 0; var failures = new ConcurrentQueue<string>();
        var builder = WebApplication.CreateSlimBuilder(); builder.Logging.ClearProviders(); builder.WebHost.UseUrls("http://127.0.0.1:0");
        await using var bridge = builder.Build(); using var pacing = new SemaphoreSlim(1, 1);
        bridge.Run(async context =>
        {
            if (context.Connection.RemoteIpAddress is null || !IPAddress.IsLoopback(context.Connection.RemoteIpAddress) || context.Request.Host.Host != "127.0.0.1") { context.Response.StatusCode = 400; return; }
            var route = context.Request.Path.ToString();
            if (context.Request.Method is not ("GET" or "OPTIONS") && !(context.Request.Method == "POST" && route == "/api/auth/login"))
            { Interlocked.Increment(ref deniedWrites); failures.Enqueue("Unexpected domain write"); context.Response.StatusCode = 405; return; }
            await pacing.WaitAsync(context.RequestAborted);
            try
            {
                await Task.Delay(650, context.RequestAborted);
                using var request = new HttpRequestMessage(new HttpMethod(context.Request.Method), context.Request.Path + context.Request.QueryString);
                var body = await new StreamReader(context.Request.Body).ReadToEndAsync(context.RequestAborted);
                if (body.Length > 0) request.Content = new StringContent(body);
                foreach (var header in context.Request.Headers)
                {
                    if (header.Key is "Host" or "Connection" or "Transfer-Encoding" or "Content-Length") continue;
                    if (!request.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray()) && request.Content is not null)
                    { request.Content.Headers.Remove(header.Key); request.Content.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray()); }
                }
                using var real = await client.SendAsync(request, context.RequestAborted);
                if (real.StatusCode != HttpStatusCode.OK) throw new InvalidOperationException("Unexpected API status " + (int)real.StatusCode);
                if (!JsonElement.DeepEquals(initial, await State())) throw new InvalidOperationException("Recorder changed SQL/Identity/files/Blob state");
                Interlocked.Increment(ref requests);
                var bytes = await real.Content.ReadAsByteArrayAsync(context.RequestAborted); context.Response.StatusCode = (int)real.StatusCode;
                foreach (var header in real.Headers.Concat(real.Content.Headers))
                { if (header.Key is not ("Transfer-Encoding" or "Connection")) context.Response.Headers[header.Key] = header.Value.ToArray(); }
                await context.Response.Body.WriteAsync(bytes, context.RequestAborted);
            }
            catch (Exception e) { failures.Enqueue(e.GetType().Name); context.Response.StatusCode = 500; }
            finally { pacing.Release(); }
        });
        await bridge.StartAsync();
        var start = new ProcessStartInfo("node") { WorkingDirectory = Directory.GetCurrentDirectory(), UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true };
        foreach (var arg in new[] { Path.GetFullPath("QA/tools/recorder-browser-live.cjs"), bridge.Urls.Single(), manifest.RunId.ToString("N") }) start.ArgumentList.Add(arg);
        if (recovery) start.ArgumentList.Add("recovery");
        using var process = Process.Start(start) ?? throw new InvalidOperationException("Recorder browser process unavailable");
        var output = process.StandardOutput.ReadToEndAsync(); var errors = process.StandardError.ReadToEndAsync();
        try { using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(6)); await process.WaitForExitAsync(timeout.Token); }
        catch { if (!process.HasExited) process.Kill(entireProcessTree: true); throw; }
        finally { await bridge.StopAsync(); }
        Console.Write(await output);
        if (process.ExitCode != 0 || !failures.IsEmpty || deniedWrites != 0 || requests < 8 || !JsonElement.DeepEquals(initial, await State()))
            throw new InvalidOperationException("Recorder browser gate failed: " + await errors + string.Join(";", failures));
        Console.WriteLine(recovery
            ? $"BLOB RECORDER RECOVERY SQL PASS:4 browser journeys/controlled pending cancellation-error-limit with native fake-device streams/{requests} unchanged full snapshots/no writes/no429. Human prompt/native device failures/long-duration limit NOT RUN."
            : $"BLOB RECORDER SQL PASS:4 browser journeys/denied-granted native permissions/native fake-device playback/unmount/{requests} unchanged full snapshots/no writes/no429. Physical devices and human prompt/cancel NOT RUN.");
    }
}
