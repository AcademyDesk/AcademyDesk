using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

internal static partial class SqlHarnessEntryPoint
{
    // Separate opt-in write oracle. Never relax the existing read-only gate.
    private static async Task VerifyRecorderUploadAsync(QaApiFactory factory, HttpClient client, QaBlobFixture fixture, QaRunManifest manifest)
    {
        await SeedBrowserRecipientsAsync(factory); client.DefaultRequestHeaders.Authorization = null;
        var failures = new ConcurrentQueue<string>(); var requests = 0; var unchanged = 0; var creates = 0; var completions = 0; var retries = 0;
        var chunks = new Dictionary<Guid, byte[]>(); var published = new HashSet<Guid>();
        async Task<JsonElement> State()
        {
            using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); var blobs = new List<object>();
            await foreach (var blob in fixture.Container.GetBlobsAsync()) blobs.Add(new { blob.Name, etag = blob.Properties.ETag?.ToString(), blob.Properties.ContentLength, blob.Properties.LastModified });
            return JsonSerializer.SerializeToElement(new { protectedState = await LifecycleOverlapState(factory, manifest), uploads = await db.ClassMediaUploadSessions.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(), blobs });
        }
        void Check(bool ok, string label) { if (!ok) throw new InvalidOperationException("Recorder upload: " + label); }
        void Retained(JsonElement before, JsonElement after, int additions)
        {
            Check(after.GetArrayLength() == before.GetArrayLength() + additions, "array count differs");
            foreach (var row in before.EnumerateArray()) Check(after.EnumerateArray().Any(x => JsonElement.DeepEquals(x, row)), "existing row changed");
        }
        var initial = await State();
        var builder = WebApplication.CreateSlimBuilder(); builder.Logging.ClearProviders(); builder.WebHost.UseUrls("http://127.0.0.1:0");
        await using var bridge = builder.Build(); using var pacing = new SemaphoreSlim(1, 1);
        bridge.Run(async context =>
        {
            if (context.Connection.RemoteIpAddress is null || !IPAddress.IsLoopback(context.Connection.RemoteIpAddress) || context.Request.Host.Host != "127.0.0.1") { context.Response.StatusCode = 400; return; }
            var route = context.Request.Path.ToString(); var method = context.Request.Method; var segments = route.Split('/', StringSplitOptions.RemoveEmptyEntries);
            var create = method == "POST" && route == "/api/teacher/media-uploads";
            var media = segments.Length == 5 && segments[0] == "api" && segments[1] == "teacher" && segments[2] == "media-uploads" && Guid.TryParse(segments[3], out _);
            var complete = method == "POST" && media && segments[4] == "complete";
            var chunk = method == "PUT" && segments.Length == 6 && segments.Take(3).SequenceEqual(new[] { "api", "teacher", "media-uploads" }) && Guid.TryParse(segments[3], out _) && segments[4] == "chunks" && segments[5] == "0";
            if (method is not ("GET" or "HEAD") && !create && !complete && !chunk && !(method == "POST" && route == "/api/auth/login"))
            { failures.Enqueue("Unexpected write"); context.Response.StatusCode = 405; return; }
            await pacing.WaitAsync(context.RequestAborted);
            try
            {
                await Task.Delay(650, context.RequestAborted); var before = await State();
                Check(context.Request.ContentLength.GetValueOrDefault() <= 1024 * 1024, "test body exceeds bounded recording size");
                using var buffer = new MemoryStream(); await context.Request.Body.CopyToAsync(buffer, context.RequestAborted); Check(buffer.Length <= 1024 * 1024, "body exceeded bound"); var body = buffer.ToArray();
                using var request = new HttpRequestMessage(new HttpMethod(method), context.Request.Path + context.Request.QueryString);
                if (body.Length > 0) request.Content = new ByteArrayContent(body);
                foreach (var header in context.Request.Headers)
                {
                    if (header.Key is "Host" or "Connection" or "Transfer-Encoding" or "Content-Length") continue;
                    if (!request.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray()) && request.Content is not null)
                    { request.Content.Headers.Remove(header.Key); request.Content.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray()); }
                }
                using var real = await client.SendAsync(request, context.RequestAborted); var response = await real.Content.ReadAsByteArrayAsync(context.RequestAborted);
                Check(real.StatusCode != HttpStatusCode.TooManyRequests, "HTTP429 cannot pass");
                var after = await State();
                if (create)
                {
                    Check(real.StatusCode == HttpStatusCode.Created, "create must201");
                    using var result = JsonDocument.Parse(response); var id = result.RootElement.GetProperty("id").GetGuid();
                    LifecycleSame(before.GetProperty("protectedState"), after.GetProperty("protectedState")); Retained(before.GetProperty("uploads"), after.GetProperty("uploads"), 1); Check(JsonElement.DeepEquals(before.GetProperty("blobs"), after.GetProperty("blobs")), "create committed bytes");
                    using var data = JsonDocument.Parse(body); var input = data.RootElement;
                    using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
                    var row = await db.ClassMediaUploadSessions.AsNoTracking().SingleAsync(x => x.Id == id);
                    var user = await scope.ServiceProvider.GetRequiredService<IdentityDbContext>().Users.AsNoTracking().SingleAsync(x => x.Email == "qa-teacher-a@example.invalid");
                    Check(row.OwnerUserId == user.Id && row.TeacherId == user.TeacherId && row.AcademyId == user.AcademyId && row.BatchId == input.GetProperty("batchId").GetGuid() && row.StudentId == null &&
                        row.ClassSessionId == input.GetProperty("classSessionId").GetGuid() && row.ClientRequestId == input.GetProperty("clientRequestId").GetGuid() &&
                        row.Length == input.GetProperty("length").GetInt64() && row.FileName == input.GetProperty("fileName").GetString() && row.Title == input.GetProperty("title").GetString() && row.Title.StartsWith("QA recorded ") &&
                        row.Description == "Real synthetic-device audio; explicit confirmation only" && row.Type == input.GetProperty("type").GetString() && row.Type == "Attachment" && row.CompletedAtUtc == null, "create identity/scope/immutable metadata differs");
                    Check(await db.ClassSessions.AnyAsync(x => x.Id == row.ClassSessionId && x.BatchId == row.BatchId && x.TeacherId == row.TeacherId), "wrong session scope"); creates++;
                }
                else if (complete)
                {
                    Check(real.StatusCode == HttpStatusCode.OK, "completion must200"); var id = Guid.Parse(segments[3]); Check(chunks.TryGetValue(id, out var captured), "completion lacks observed native chunk");
                    if (published.Contains(id)) { Check(JsonElement.DeepEquals(before, after), "retry duplicated publication"); unchanged++; retries++; }
                    else
                    {
                        LifecycleSame(before.GetProperty("protectedState"), after.GetProperty("protectedState"), "resources", "notifications");
                        var expected = JsonNode.Parse(before.GetProperty("uploads").GetRawText())!.AsArray(); var completed = after.GetProperty("uploads").EnumerateArray().Single(x => x.GetProperty("Id").GetGuid() == id);
                        Check(completed.GetProperty("CompletedAtUtc").ValueKind == JsonValueKind.String, "completion timestamp absent");
                        expected.Single(x => x!["Id"]!.GetValue<Guid>() == id)!["CompletedAtUtc"] = JsonNode.Parse(completed.GetProperty("CompletedAtUtc").GetRawText());
                        Check(JsonElement.DeepEquals(JsonSerializer.SerializeToElement(expected), after.GetProperty("uploads")), "completion changed immutable upload metadata");
                        Retained(before.GetProperty("blobs"), after.GetProperty("blobs"), 1); Retained(before.GetProperty("protectedState").GetProperty("resources"), after.GetProperty("protectedState").GetProperty("resources"), 1);
                        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); var row = await db.ClassMediaUploadSessions.AsNoTracking().SingleAsync(x => x.Id == id);
                        var recipients = await db.Enrollments.Where(x => x.AcademyId == row.AcademyId && x.BatchId == row.BatchId && x.Status == "Active").Select(x => x.StudentId).Distinct().ToArrayAsync();
                        Retained(before.GetProperty("protectedState").GetProperty("notifications"), after.GetProperty("protectedState").GetProperty("notifications"), recipients.Length);
                        var resource = await db.LearningResources.AsNoTracking().SingleAsync(x => x.Id == id);
                        Check(resource.AcademyId == row.AcademyId && resource.BatchId == row.BatchId && resource.StudentId == row.StudentId && resource.ClassSessionId == row.ClassSessionId && resource.Title == row.Title && resource.Description == row.Description && resource.Type == row.Type && resource.IsPublished && resource.Url == $"/api/class-media/{id}/content", "resource differs from immutable upload");
                        foreach (var recipient in recipients) Check(await db.Notifications.CountAsync(x => x.AcademyId == row.AcademyId && x.RecipientId == recipient && x.RecipientType == "Student" && x.Title == "New class material" && x.Message == row.Title + " is available in your class history." && x.Channel == "InApp" && x.Status == "Queued") == 1, "notification duplicated/misdirected");
                        var info = await fixture.Store.PropertiesAsync(row.AcademyId, id, default); Check(info.Length == captured!.Length && info.FileName == row.FileName, "committed Blob metadata differs");
                        await using var download = await fixture.Store.ReadAsync(row.AcademyId, id, 0, null, default);
                        Check((await SHA256.HashDataAsync(download.Content)).SequenceEqual(SHA256.HashData(captured)), "stored Blob native chunk SHA256 differs");
                        published.Add(id); completions++;
                    }
                }
                else
                {
                    Check(JsonElement.DeepEquals(before, after), "read/login/chunk changed protected state"); unchanged++;
                    if (chunk) { Check(real.StatusCode == HttpStatusCode.OK, "chunk must200"); var id = Guid.Parse(segments[3]); Check(chunks.TryAdd(id, body), "unexpected duplicate chunk"); }
                    else if (route.StartsWith("/api/class-media/") && method == "GET") Check(real.StatusCode is HttpStatusCode.OK or HttpStatusCode.Unauthorized or HttpStatusCode.NotFound, "unexpected protected read status");
                    else Check(real.StatusCode == HttpStatusCode.OK, "unexpected UI/login/HEAD status");
                }
                requests++; context.Response.StatusCode = (int)real.StatusCode;
                foreach (var header in real.Headers.Concat(real.Content.Headers)) { if (header.Key is not ("Transfer-Encoding" or "Connection")) context.Response.Headers[header.Key] = header.Value.ToArray(); }
                if (method != "HEAD") await context.Response.Body.WriteAsync(response, context.RequestAborted);
            }
            catch (Exception e) { failures.Enqueue(e.Message); context.Response.StatusCode = 500; }
            finally { pacing.Release(); }
        });
        await bridge.StartAsync();
        var start = new ProcessStartInfo("node") { WorkingDirectory = Directory.GetCurrentDirectory(), UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true };
        foreach (var arg in new[] { Path.GetFullPath("QA/tools/recorder-browser-live.cjs"), bridge.Urls.Single(), manifest.RunId.ToString("N"), "upload" }) start.ArgumentList.Add(arg);
        using var process = Process.Start(start) ?? throw new InvalidOperationException("Recorder upload browser unavailable"); var output = process.StandardOutput.ReadToEndAsync(); var errors = process.StandardError.ReadToEndAsync();
        try { using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(6)); await process.WaitForExitAsync(timeout.Token); }
        catch { if (!process.HasExited) process.Kill(entireProcessTree: true); throw; }
        finally { await bridge.StopAsync(); }
        Console.Write(await output);
        Check(process.ExitCode == 0 && failures.IsEmpty, "browser failed: " + await errors + string.Join(";", failures));
        Check(creates == 4 && completions == 4 && retries == 4 && chunks.Count == 4 && published.Count == 4 && requests >= 60, "four linked journey counts differ");
        var final = await State(); LifecycleSame(initial.GetProperty("protectedState"), final.GetProperty("protectedState"), "resources", "notifications");
        Retained(initial.GetProperty("uploads"), final.GetProperty("uploads"), 4); Retained(initial.GetProperty("blobs"), final.GetProperty("blobs"), 4);
        Retained(initial.GetProperty("protectedState").GetProperty("resources"), final.GetProperty("protectedState").GetProperty("resources"), 4);
        Console.WriteLine($"BLOB RECORDER UPLOAD SQL PASS:4 native-browser uploads/4 publications/one notification per distinct enrolled recipient/4 no-write completion retries/{requests} HTTP/{unchanged} unchanged snapshots/exact native-chunk Blob hashes/no429. Physical devices/native Download/Student UI NOT RUN.");
    }
}
