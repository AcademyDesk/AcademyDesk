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
    // Additive local SQL/private-Blob gate. No mocked authorization, role claims,
    // response status, ticket clock, limiter or storage implementation.
    private static async Task VerifyMediaLifecycleAsync(QaApiFactory factory, HttpClient client, QaBlobFixture fixture, QaRunManifest manifest)
    {
        const string origin = "http://localhost:3000";
        var checks = 0; var requests = 0; var denied = 0; var snapshots = 0;
        void Check(bool ok, string label) { if (!ok) throw new InvalidOperationException("BLOB LIFECYCLE FAIL: " + label); checks++; }
        async Task<JsonElement> State()
        {
            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var blobs = new List<object>();
            await foreach (var blob in fixture.Container.GetBlobsAsync())
                blobs.Add(new { blob.Name, etag = blob.Properties.ETag?.ToString(), blob.Properties.ContentLength, blob.Properties.LastModified });
            return JsonSerializer.SerializeToElement(new {
                protectedState = await LifecycleOverlapState(factory, manifest),
                uploads = await db.ClassMediaUploadSessions.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(),
                blobs
            });
        }
        async Task Same(JsonElement before)
        { Check(JsonElement.DeepEquals(before, await State()), "HTTP operation changed SQL/Identity/files/committed Blob state"); snapshots++; }
        async Task<HttpResponseMessage> Send(HttpRequestMessage command, string? bearer, HttpStatusCode expected, string label, bool noWrite = true)
        {
            using (command)
            {
                var before = noWrite ? await State() : default;
                await Task.Delay(650);
                command.Headers.Authorization = bearer is null ? null : new AuthenticationHeaderValue("Bearer", bearer);
                var result = await client.SendAsync(command); requests++;
                if (result.StatusCode != expected || result.StatusCode == HttpStatusCode.TooManyRequests)
                { var actual = result.StatusCode; result.Dispose(); throw new InvalidOperationException($"BLOB LIFECYCLE FAIL: {label}, expected {(int)expected}, actual {(int)actual}"); }
                checks++; if (expected is HttpStatusCode.BadRequest or HttpStatusCode.NotFound) denied++;
                try { if (noWrite) await Same(before); } catch { result.Dispose(); throw; }
                return result;
            }
        }
        Guid academy, teacher, batch, student, published;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            academy = await db.Academies.Where(x => x.Name == "Synthetic Academy A").Select(x => x.Id).SingleAsync();
            teacher = await db.Teachers.Where(x => x.AcademyId == academy).Select(x => x.Id).SingleAsync();
            batch = await db.Batches.Where(x => x.AcademyId == academy).Select(x => x.Id).SingleAsync();
            student = await db.Students.Where(x => x.AcademyId == academy).Select(x => x.Id).SingleAsync();
            published = await db.ClassMediaUploadSessions.Where(x => x.AcademyId == academy && x.FileName == "retry.ogg" && x.CompletedAtUtc != null).Select(x => x.Id).SingleAsync();
        }
        var beforeLogin = await State();
        var owner = await LoginAsync(client, "qa-teacher-a@example.invalid", "Synthetic!39Ab");
        var family = await LoginAsync(client, "qa-blob-student@example.invalid", "Synthetic!39Ab");
        await Same(beforeLogin);
        var bytes = RandomNumberGenerator.GetBytes(MediaStorageOptions.DefaultChunkBytes + 3);
        var createBody = new { clientRequestId = Guid.NewGuid(), batchId = batch, studentId = student, fileName = "lifecycle-resume.mov", length = bytes.Length, title = "Synthetic lifecycle media", description = "Preserve this private draft" };
        Guid pending;
        using (var result = await Send(new(HttpMethod.Post, "/api/teacher/media-uploads") { Content = JsonContent.Create(createBody) }, owner, HttpStatusCode.Created, "pending create", false))
        { using var json = JsonDocument.Parse(await result.Content.ReadAsStringAsync()); pending = json.RootElement.GetProperty("id").GetGuid(); }
        var root = $"/api/teacher/media-uploads/{pending}"; var content = $"/api/class-media/{published}/content";
        var upload = MediaUpload.Create(academy, pending, createBody.fileName, bytes.Length, new MediaStorageOptions());
        HttpRequestMessage Chunk(int index, byte[] value) => new(HttpMethod.Put, root + "/chunks/" + index) { Content = new ByteArrayContent(value) { Headers = { ContentType = new("application/octet-stream") } } };
        using (await Send(Chunk(0, bytes[..MediaStorageOptions.DefaultChunkBytes]), owner, HttpStatusCode.OK, "initial stage", false)) { }
        async Task Progress()
        { var progress = await fixture.Store.ProgressAsync(upload, default); Check(!progress.Committed && progress.UploadedBlocks.SequenceEqual([0]), "denial changed pending staged blocks"); }
        HttpRequestMessage Mint() { var req = new HttpRequestMessage(HttpMethod.Post, "/api/class-material-downloads/tickets") { Content = JsonContent.Create(new { path = content }) }; req.Headers.Add("Origin", origin); return req; }
        HttpRequestMessage Redeem(string ticket) { var req = new HttpRequestMessage(HttpMethod.Post, content) { Content = new FormUrlEncodedContent([new("ticket", ticket)]) }; req.Headers.Add("Origin", origin); return req; }
        async Task<string> Ticket(string bearer)
        {
            using var result = await Send(Mint(), bearer, HttpStatusCode.OK, "ticket mint");
            using var json = JsonDocument.Parse(await result.Content.ReadAsStringAsync());
            Check(json.RootElement.GetProperty("expiresInSeconds").GetInt32() == 60 && result.Headers.CacheControl?.NoStore == true, "ticket lifetime/cache changed");
            return json.RootElement.GetProperty("ticket").GetString()!;
        }
        async Task Read(HttpRequestMessage command, string? bearer, string label)
        {
            using var result = await Send(command, bearer, HttpStatusCode.OK, label);
            Check((await result.Content.ReadAsByteArrayAsync()).SequenceEqual(new byte[] { 1, 2, 3 }) && result.Content.Headers.ContentDisposition?.DispositionType == "attachment" && result.Headers.CacheControl?.NoStore == true, "restored private attachment bytes/headers differ");
        }
        async Task Active(string mode, bool value)
        {
            var before = await State();
            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
                var affected = mode == "teacher"
                    ? await db.Teachers.Where(x => x.Id == teacher && x.AcademyId == academy).ExecuteUpdateAsync(s => s.SetProperty(x => x.IsActive, value))
                    : await db.Academies.Where(x => x.Id == academy).ExecuteUpdateAsync(s => s.SetProperty(x => x.IsActive, value));
                Check(affected == 1, "authority control did not affect exactly one row");
            }
            var expected = JsonNode.Parse(before.GetRawText())!;
            var field = mode == "teacher" ? "teachers" : "academies"; var id = mode == "teacher" ? teacher : academy;
            var row = expected["protectedState"]![field]!.AsArray().Single(x => x!["Id"]!.GetValue<Guid>() == id)!;
            row["IsActive"] = value;
            Check(JsonElement.DeepEquals(JsonSerializer.SerializeToElement(expected), await State()), "authority control changed unrelated fields");
        }
        var initial = await State();
        foreach (var mode in new[] { "teacher", "academy" })
        for (var attempt = 1; attempt <= 3; attempt++)
        {
            var ownerTicket = await Ticket(owner); var familyTicket = mode == "academy" ? await Ticket(family) : null;
            await Active(mode, false);
            using (var result = await Send(new(HttpMethod.Post, "/api/teacher/media-uploads") { Content = JsonContent.Create(createBody) }, owner, HttpStatusCode.BadRequest, "revoked create retry"))
            { using var json = JsonDocument.Parse(await result.Content.ReadAsStringAsync()); Check(json.RootElement.GetProperty("message").GetString() == "Select an active assigned batch, student and class session where applicable.", "revocation create guidance changed"); }
            using (await Send(new(HttpMethod.Get, root), owner, HttpStatusCode.NotFound, "revoked progress")) { }
            using (await Send(Chunk(0, new byte[MediaStorageOptions.DefaultChunkBytes]), owner, HttpStatusCode.NotFound, "revoked overwrite staged chunk")) { }
            using (await Send(Chunk(1, bytes[^3..]), owner, HttpStatusCode.NotFound, "revoked tail")) { }
            using (await Send(new(HttpMethod.Post, root + "/complete"), owner, HttpStatusCode.NotFound, "revoked completion")) { }
            foreach (var method in new[] { HttpMethod.Get, HttpMethod.Head })
                using (await Send(new(method, content), owner, HttpStatusCode.NotFound, "revoked teacher content")) { }
            using (await Send(Mint(), owner, HttpStatusCode.NotFound, "revoked teacher mint")) { }
            using (await Send(Redeem(ownerTicket), null, HttpStatusCode.NotFound, "pre-revocation teacher ticket")) { }
            if (familyTicket is not null)
            {
                foreach (var method in new[] { HttpMethod.Get, HttpMethod.Head })
                    using (await Send(new(method, content), family, HttpStatusCode.NotFound, "inactive academy student content")) { }
                using (await Send(Mint(), family, HttpStatusCode.NotFound, "inactive academy student mint")) { }
                using (await Send(Redeem(familyTicket), null, HttpStatusCode.NotFound, "pre-revocation student ticket")) { }
            }
            else await Read(new(HttpMethod.Get, content), family, "domain Teacher deactivation does not revoke unrelated enrolled student");
            await Progress(); await Active(mode, true);
            Check(JsonElement.DeepEquals(initial, await State()), "restore did not return exact initial state");
            using (var result = await Send(new(HttpMethod.Post, "/api/teacher/media-uploads") { Content = JsonContent.Create(createBody) }, owner, HttpStatusCode.OK, "restored original session"))
            { using var json = JsonDocument.Parse(await result.Content.ReadAsStringAsync()); Check(json.RootElement.GetProperty("id").GetGuid() == pending, "restore created another session"); }
            using (var result = await Send(new(HttpMethod.Get, root), owner, HttpStatusCode.OK, "restored progress"))
            { using var json = JsonDocument.Parse(await result.Content.ReadAsStringAsync()); Check(json.RootElement.GetProperty("uploadedBlocks").EnumerateArray().Select(x => x.GetInt32()).SequenceEqual([0]), "restored progress differs"); }
            await Read(new(HttpMethod.Get, content), owner, "restored teacher attachment");
            await Read(Redeem(ownerTicket), null, "restored original teacher ticket");
            if (familyTicket is not null) await Read(Redeem(familyTicket), null, "restored original student ticket");
            Console.WriteLine($"BLOB LIFECYCLE case PASS: {mode}-{attempt}, authoritative denial/restoration, same tokens/tickets/session, unchanged SQL/Blob/no429.");
        }
        var beforeFinish = await State();
        using (await Send(Chunk(1, bytes[^3..]), owner, HttpStatusCode.OK, "restored exact tail", false)) { }
        using (await Send(new(HttpMethod.Post, root + "/complete"), owner, HttpStatusCode.OK, "restored publish", false)) { }
        var final = await State();
        LifecycleSame(beforeFinish.GetProperty("protectedState"), final.GetProperty("protectedState"), "resources", "notifications");
        var expectedUploads = JsonNode.Parse(beforeFinish.GetProperty("uploads").GetRawText())!.AsArray();
        var completedUpload = final.GetProperty("uploads").EnumerateArray().Single(x => x.GetProperty("Id").GetGuid() == pending);
        Check(completedUpload.GetProperty("CompletedAtUtc").ValueKind == JsonValueKind.String, "completion timestamp missing");
        expectedUploads.Single(x => x!["Id"]!.GetValue<Guid>() == pending)!["CompletedAtUtc"] = JsonNode.Parse(completedUpload.GetProperty("CompletedAtUtc").GetRawText());
        Check(JsonElement.DeepEquals(JsonSerializer.SerializeToElement(expectedUploads), final.GetProperty("uploads")), "completion altered session metadata or another upload");
        foreach (var field in new[] { "resources", "notifications" })
        foreach (var oldRow in beforeFinish.GetProperty("protectedState").GetProperty(field).EnumerateArray())
            Check(final.GetProperty("protectedState").GetProperty(field).EnumerateArray().Any(x => JsonElement.DeepEquals(x, oldRow)), "completion altered existing " + field);
        foreach (var blob in beforeFinish.GetProperty("blobs").EnumerateArray())
            Check(final.GetProperty("blobs").EnumerateArray().Any(x => JsonElement.DeepEquals(x, blob)), "completion altered another committed Blob");
        Check(final.GetProperty("blobs").GetArrayLength() == beforeFinish.GetProperty("blobs").GetArrayLength() + 1, "completion Blob count differs");
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            Check(await db.ClassMediaUploadSessions.CountAsync() == initial.GetProperty("uploads").GetArrayLength() &&
                await db.ClassMediaUploadSessions.CountAsync(x => x.Id == pending && x.CompletedAtUtc != null) == 1 &&
                await db.LearningResources.CountAsync(x => x.Id == pending) == 1 &&
                await db.LearningResources.CountAsync() == beforeFinish.GetProperty("protectedState").GetProperty("resources").GetArrayLength() + 1 &&
                await db.Notifications.CountAsync() == beforeFinish.GetProperty("protectedState").GetProperty("notifications").GetArrayLength() + 1, "restore published duplicate/missing SQL rows");
            var resource = await db.LearningResources.AsNoTracking().SingleAsync(x => x.Id == pending);
            Check(resource.AcademyId == academy && resource.BatchId == batch && resource.StudentId == student && resource.ClassSessionId == null &&
                resource.Title == createBody.title && resource.Description == createBody.description && resource.Type == "Class material" &&
                resource.IsPublished && resource.Url == $"/api/class-media/{pending}/content", "resumed resource metadata/scope differs");
            Check(await db.Notifications.CountAsync(x => x.AcademyId == academy && x.RecipientId == student && x.RecipientType == "Student" &&
                x.Title == "New class material" && x.Message == createBody.title + " is available in your class history." && x.Channel == "InApp" && x.Status == "Queued") == 1,
                "resumed notification recipient/content differs");
        }
        using (var result = await Send(new(HttpMethod.Get, $"/api/class-media/{pending}/content"), family, HttpStatusCode.OK, "resumed file download"))
            Check((await result.Content.ReadAsByteArrayAsync()).SequenceEqual(bytes), "denial altered original staged bytes or resumed file");
        Console.WriteLine($"BLOB LIFECYCLE summary PASS:6 cases/{requests} HTTP/{denied} exact denials/{snapshots} unchanged snapshots/{checks} checks, same-session exact-byte recovery; not browser/device/2GB/queued-after-admission acceptance.");
    }
}
