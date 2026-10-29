using System.Buffers.Binary;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Infrastructure.Media;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

internal static partial class SqlHarnessEntryPoint
{
    // Actual 2,000,000,000-byte transport. Only bounded payload/read buffers;
    // never construct a maximum-size byte array or write a source file to disk.
    private static async Task VerifyMediaLargeTransferAsync(QaApiFactory factory, HttpClient client, QaBlobFixture fixture, QaRunManifest manifest)
    {
        const long length = 2_000_000_000;
        const int chunkBytes = MediaStorageOptions.DefaultChunkBytes;
        var checks = 0; var requests = 0; var unchanged = 0;
        void Check(bool passed, string label) { if (!passed) throw new InvalidOperationException("BLOB LARGE FAIL: " + label); checks++; }
        Check(MediaStorageOptions.MaximumFileBytes == length && new MediaStorageOptions().MaxFileBytes == length, "accepted maximum changed");
        async Task<JsonElement> State()
        {
            using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var blobs = new List<object>();
            await foreach (var blob in fixture.Container.GetBlobsAsync())
                blobs.Add(new { blob.Name, etag = blob.Properties.ETag?.ToString(), blob.Properties.ContentLength, blob.Properties.LastModified });
            return JsonSerializer.SerializeToElement(new { protectedState = await LifecycleOverlapState(factory, manifest),
                uploads = await db.ClassMediaUploadSessions.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(), blobs });
        }
        async Task Same(JsonElement before)
        { Check(JsonElement.DeepEquals(before, await State()), "unexpected SQL/Identity/files/committed Blob change"); unchanged++; }
        Guid academy, batch, student;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            academy = await db.Academies.Where(x => x.Name == "Synthetic Academy A").Select(x => x.Id).SingleAsync();
            batch = await db.Batches.Where(x => x.AcademyId == academy).Select(x => x.Id).SingleAsync();
            student = await db.Students.Where(x => x.AcademyId == academy).Select(x => x.Id).SingleAsync();
        }
        var beforeLogin = await State();
        var owner = await LoginAsync(client, "qa-teacher-a@example.invalid", "Synthetic!39Ab");
        var family = await LoginAsync(client, "qa-blob-student@example.invalid", "Synthetic!39Ab");
        var foreign = await LoginAsync(client, "qa-admin-b@example.invalid", "Synthetic!39Ab");
        await Same(beforeLogin);
        var current = client;
        async Task<HttpResponseMessage> Send(HttpRequestMessage command, string? bearer, HttpStatusCode expected, string label,
            bool noWrite = true, CancellationToken token = default)
        {
            using (command)
            {
                var before = noWrite ? await State() : default;
                await Task.Delay(650, token);
                command.Headers.Authorization = bearer is null ? null : new AuthenticationHeaderValue("Bearer", bearer);
                var result = await current.SendAsync(command, HttpCompletionOption.ResponseHeadersRead, token); requests++;
                if (result.StatusCode != expected || result.StatusCode == HttpStatusCode.TooManyRequests)
                { var actual = result.StatusCode; result.Dispose(); throw new InvalidOperationException($"BLOB LARGE FAIL: {label}, expected {(int)expected}, actual {(int)actual}"); }
                checks++;
                try { if (noWrite) await Same(before); } catch { result.Dispose(); throw; }
                return result;
            }
        }
        foreach (var invalid in new[] { -1L, 0L, length + 1, long.MaxValue })
        {
            using var result = await Send(new(HttpMethod.Post, "/api/teacher/media-uploads") { Content = JsonContent.Create(new {
                clientRequestId = Guid.NewGuid(), batchId = batch, studentId = student, fileName = "maximum-transport.mov", length = invalid }) },
                owner, HttpStatusCode.BadRequest, "size boundary rejection");
            using var body = JsonDocument.Parse(await result.Content.ReadAsStringAsync());
            Check(body.RootElement.GetProperty("message").GetString() == "Choose a nonempty file no larger than 2 GB.", "size rejection guidance differs");
        }
        var beforeCreate = await State();
        var request = new { clientRequestId = Guid.NewGuid(), batchId = batch, studentId = student,
            fileName = "maximum-transport.mov", length, title = "Synthetic maximum transport", description = "Native transport only, not codec preview" };
        Guid id;
        using (var result = await Send(new(HttpMethod.Post, "/api/teacher/media-uploads") { Content = JsonContent.Create(request) }, owner, HttpStatusCode.Created, "maximum create", false))
        {
            using var body = JsonDocument.Parse(await result.Content.ReadAsStringAsync()); id = body.RootElement.GetProperty("id").GetGuid();
            Check(body.RootElement.GetProperty("length").GetInt64() == length && body.RootElement.GetProperty("chunkBytes").GetInt32() == chunkBytes &&
                body.RootElement.GetProperty("blockCount").GetInt32() == 239, "maximum manifest bounds differ");
        }
        var root = $"/api/teacher/media-uploads/{id}"; var content = $"/api/class-media/{id}/content";
        var upload = MediaUpload.Create(academy, id, request.fileName, length, new MediaStorageOptions());
        var staged = await State();
        LifecycleSame(beforeCreate.GetProperty("protectedState"), staged.GetProperty("protectedState"));
        Check(staged.GetProperty("uploads").GetArrayLength() == beforeCreate.GetProperty("uploads").GetArrayLength() + 1 &&
            JsonElement.DeepEquals(beforeCreate.GetProperty("blobs"), staged.GetProperty("blobs")), "creation stored extra session or committed bytes");
        using (await Send(new(HttpMethod.Post, root + "/complete"), owner, HttpStatusCode.Conflict, "empty upload cannot publish")) { }
        var template = RandomNumberGenerator.GetBytes(chunkBytes); var payload = new byte[chunkBytes];
        void Fill(int index)
        {
            var count = checked((int)upload.BlockLength(index)); template.AsSpan(0, count).CopyTo(payload);
            BinaryPrimitives.WriteInt64LittleEndian(payload.AsSpan(0, 8), index);
            BinaryPrimitives.WriteInt64LittleEndian(payload.AsSpan(count - 8, 8), ~((long)index));
        }
        byte ExpectedByte(long position)
        {
            var index = checked((int)(position / chunkBytes)); var offset = checked((int)(position % chunkBytes));
            var count = checked((int)upload.BlockLength(index));
            if (offset < 8) return (byte)(((long)index >> (offset * 8)) & 255);
            if (offset >= count - 8) return (byte)((~((long)index) >> ((offset - (count - 8)) * 8)) & 255);
            return template[offset];
        }
        HttpRequestMessage Chunk(int index, int count) => new(HttpMethod.Put, root + "/chunks/" + index) {
            Content = new ByteArrayContent(payload, 0, count) { Headers = { ContentType = new("application/octet-stream") } } };
        using (await Send(Chunk(239, 1), owner, HttpStatusCode.BadRequest, "out-of-range chunk")) { }
        using (await Send(Chunk(238, 1), owner, HttpStatusCode.BadRequest, "incorrect maximum-file tail length")) { }
        async Task Progress(int count)
        {
            using var result = await Send(new(HttpMethod.Get, root), owner, HttpStatusCode.OK, "exact staged progress");
            using var body = JsonDocument.Parse(await result.Content.ReadAsStringAsync());
            Check(!body.RootElement.GetProperty("blobCommitted").GetBoolean() &&
                body.RootElement.GetProperty("uploadedBlocks").EnumerateArray().Select(x => x.GetInt32()).SequenceEqual(Enumerable.Range(0, count)), "staged progress missing/duplicated/reordered");
        }
        using var expectedHash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        QaApiFactory? resumedFactory = null; HttpClient? resumedClient = null;
        try
        {
            for (var index = 0; index < upload.BlockCount; index++)
            {
                Fill(index); var count = checked((int)upload.BlockLength(index));
                using (await Send(Chunk(index, count), owner, HttpStatusCode.OK, "stage " + index)) { }
                expectedHash.AppendData(payload, 0, count);
                if (index == 0) { using (await Send(Chunk(index, count), owner, HttpStatusCode.OK, "idempotent chunk retry")) { } }
                if (index == 119)
                {
                    await Progress(120); await Same(staged);
                    resumedFactory = new QaApiFactory(manifest, new Dictionary<string, string?> { ["Cors:AllowedOrigins:0"] = "http://localhost:3000" },
                        isolatedMediaStore: new AzureMediaBlobStore(fixture.Container, new MediaStorageOptions()));
                    resumedClient = resumedFactory.CreateClient(new() { AllowAutoRedirect = false }); current = resumedClient;
                    await Progress(120);
                    using (var retry = await Send(new(HttpMethod.Post, "/api/teacher/media-uploads") { Content = JsonContent.Create(request) }, owner, HttpStatusCode.OK, "fresh host original request"))
                    { using var body = JsonDocument.Parse(await retry.Content.ReadAsStringAsync()); Check(body.RootElement.GetProperty("id").GetGuid() == id, "fresh host duplicated session"); }
                    Console.WriteLine("BLOB LARGE recovery PASS:120 durable chunks, fresh DI host/Blob adapter, same original token/request/session; no new login or bytes replayed.");
                }
                if ((index + 1) % 32 == 0 || index == upload.BlockCount - 1)
                { await Progress(index + 1); Console.WriteLine($"BLOB LARGE progress:{index + 1}/{upload.BlockCount} staged; bytes={Math.Min((long)(index + 1) * chunkBytes, length)}; no429."); }
            }
            await Same(staged);
            using (await Send(new(HttpMethod.Post, root + "/complete"), owner, HttpStatusCode.OK, "maximum completion", false)) { }
            var final = await State();
            LifecycleSame(staged.GetProperty("protectedState"), final.GetProperty("protectedState"), "resources", "notifications");
            var expectedUploads = JsonNode.Parse(staged.GetProperty("uploads").GetRawText())!.AsArray();
            var completed = final.GetProperty("uploads").EnumerateArray().Single(x => x.GetProperty("Id").GetGuid() == id);
            Check(completed.GetProperty("CompletedAtUtc").ValueKind == JsonValueKind.String, "maximum completion timestamp absent");
            expectedUploads.Single(x => x!["Id"]!.GetValue<Guid>() == id)!["CompletedAtUtc"] = JsonNode.Parse(completed.GetProperty("CompletedAtUtc").GetRawText());
            Check(JsonElement.DeepEquals(JsonSerializer.SerializeToElement(expectedUploads), final.GetProperty("uploads")), "maximum completion changed upload metadata/other sessions");
            foreach (var field in new[] { "resources", "notifications" })
            {
                Check(final.GetProperty("protectedState").GetProperty(field).GetArrayLength() == staged.GetProperty("protectedState").GetProperty(field).GetArrayLength() + 1, "completion changed " + field + " count");
                foreach (var row in staged.GetProperty("protectedState").GetProperty(field).EnumerateArray())
                    Check(final.GetProperty("protectedState").GetProperty(field).EnumerateArray().Any(x => JsonElement.DeepEquals(x, row)), "completion changed existing " + field);
            }
            foreach (var blob in staged.GetProperty("blobs").EnumerateArray())
                Check(final.GetProperty("blobs").EnumerateArray().Any(x => JsonElement.DeepEquals(x, blob)), "maximum completion replaced prior Blob");
            Check(final.GetProperty("blobs").GetArrayLength() == staged.GetProperty("blobs").GetArrayLength() + 1, "maximum Blob count differs");
            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); var resource = await db.LearningResources.AsNoTracking().SingleAsync(x => x.Id == id);
                Check(resource.AcademyId == academy && resource.BatchId == batch && resource.StudentId == student && resource.ClassSessionId == null &&
                    resource.Title == request.title && resource.Description == request.description && resource.Type == "Class material" && resource.IsPublished && resource.Url == content,
                    "maximum published resource scope/metadata differs");
                Check(await db.Notifications.CountAsync(x => x.AcademyId == academy && x.RecipientId == student && x.RecipientType == "Student" && x.Title == "New class material" &&
                    x.Message == request.title + " is available in your class history." && x.Channel == "InApp" && x.Status == "Queued") == 1, "maximum notification recipient/content differs");
            }
            var info = await fixture.Store.PropertiesAsync(academy, id, default);
            Check(info.Length == length && info.FileName == request.fileName, "committed maximum length/filename differs");
            using (await Send(new(HttpMethod.Post, root + "/complete"), owner, HttpStatusCode.OK, "maximum idempotent completion")) { }
            using (await Send(Chunk(0, chunkBytes), owner, HttpStatusCode.Conflict, "completed maximum overwrite refused")) { }
            using (await Send(new(HttpMethod.Get, content), null, HttpStatusCode.Unauthorized, "anonymous maximum download")) { }
            using (await Send(new(HttpMethod.Get, content), foreign, HttpStatusCode.NotFound, "foreign maximum download")) { }
            using (var head = await Send(new(HttpMethod.Head, content), family, HttpStatusCode.OK, "maximum HEAD"))
            {
                await using var headStream = await head.Content.ReadAsStreamAsync();
                Check(head.Content.Headers.ContentLength == length && await headStream.ReadAsync(new byte[1]) == 0, "maximum HEAD length/body differs");
            }
            async Task Range(string value, HttpStatusCode status, long start = 0, int count = 0)
            {
                var req = new HttpRequestMessage(HttpMethod.Get, content); req.Headers.TryAddWithoutValidation("Range", value);
                using var result = await Send(req, family, status, "maximum range");
                if (status == HttpStatusCode.PartialContent)
                    Check(result.Content.Headers.ContentRange?.ToString() == $"bytes {start}-{start + count - 1}/{length}" && result.Content.Headers.ContentLength == count &&
                        (await result.Content.ReadAsByteArrayAsync()).SequenceEqual(Enumerable.Range(0, count).Select(x => ExpectedByte(start + x))), "maximum boundary range bytes/headers differ");
                else Check(result.Content.Headers.ContentRange?.ToString() == $"bytes */{length}", "invalid maximum range header differs");
            }
            await Range($"bytes={chunkBytes - 17}-{chunkBytes + 24}", HttpStatusCode.PartialContent, chunkBytes - 17, 42);
            await Range("bytes=-37", HttpStatusCode.PartialContent, length - 37, 37);
            await Range($"bytes={length - 1}-{length + 100}", HttpStatusCode.PartialContent, length - 1, 1);
            await Range($"bytes={length}-", HttpStatusCode.RequestedRangeNotSatisfiable);
            await Range("bytes=0-1,4-7", HttpStatusCode.RequestedRangeNotSatisfiable);
            var readBuffer = new byte[65536];
            using (var abort = new CancellationTokenSource(TimeSpan.FromMinutes(5)))
            using (var interrupted = await Send(new(HttpMethod.Get, content), family, HttpStatusCode.OK, "interrupt maximum download", token: abort.Token))
            {
                await using var stream = await interrupted.Content.ReadAsStreamAsync(abort.Token);
                Check(await stream.ReadAsync(readBuffer, abort.Token) > 0, "interrupted download yielded no bytes"); abort.Cancel();
            }
            await Same(final);
            using var readDeadline = new CancellationTokenSource(TimeSpan.FromMinutes(5));
            using var download = await Send(new(HttpMethod.Get, content), family, HttpStatusCode.OK, "stream complete maximum", token: readDeadline.Token);
            Check(download.Content.Headers.ContentLength == length && download.Content.Headers.ContentDisposition?.DispositionType == "attachment" &&
                download.Headers.CacheControl?.NoStore == true && download.Headers.TryGetValues("X-Content-Type-Options", out var sniff) && sniff.Single() == "nosniff", "maximum attachment safety/length headers differ");
            using var actualHash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            await using var input = await download.Content.ReadAsStreamAsync(readDeadline.Token); long received = 0;
            while (true)
            {
                var count = await input.ReadAsync(readBuffer, readDeadline.Token); if (count == 0) break;
                received += count;
                if (received > length) throw new InvalidOperationException("BLOB LARGE FAIL: maximum stream exceeds promised length");
                actualHash.AppendData(readBuffer, 0, count);
            }
            var expected = expectedHash.GetHashAndReset();
            Check(received == length && actualHash.GetHashAndReset().SequenceEqual(expected), "maximum full-stream byte count/SHA256 differs");
            await Same(final);
            Console.WriteLine($"BLOB LARGE summary PASS:actualBytes={received},239 chunks+one retry,120-chunk fresh-host recovery,exactSHA256={Convert.ToHexString(expected)},{requests} HTTP/{unchanged} unchanged snapshots/{checks} checks,no429; five ranges/HEAD/download interruption/idempotent publication. Native local transport only; browser/codec/devices/Azure NOT RUN.");
        }
        finally { resumedClient?.Dispose(); resumedFactory?.Dispose(); }
    }
}
