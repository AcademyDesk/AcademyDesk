using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Infrastructure.Media;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

internal static partial class SqlHarnessEntryPoint
{
    // Real provider boundary scheduling only. Never fake policy/HTTP/storage.
    private static async Task VerifyMediaAdmissionReviewAsync(QaBlobFixture fixture, QaRunManifest manifest)
    {
        var gate = new MediaAdmissionGate(fixture.Store);
        using var factory = new QaApiFactory(manifest, isolatedMediaStore: gate);
        using var client = factory.CreateClient(new() { AllowAutoRedirect = false });
        var owner = await LoginAsync(client, "qa-teacher-a@example.invalid", "Synthetic!39Ab");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", owner);
        Guid academy, teacher, batch, student;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            academy = await db.Academies.Where(x => x.Name == "Synthetic Academy A").Select(x => x.Id).SingleAsync();
            teacher = await db.Teachers.Where(x => x.AcademyId == academy).Select(x => x.Id).SingleAsync();
            batch = await db.Batches.Where(x => x.AcademyId == academy).Select(x => x.Id).SingleAsync();
            student = await db.Students.Where(x => x.AcademyId == academy).Select(x => x.Id).SingleAsync();
        }
        async Task<JsonElement> State()
        {
            using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); var blobs = new List<object>();
            await foreach (var blob in fixture.Container.GetBlobsAsync()) blobs.Add(new { blob.Name, etag = blob.Properties.ETag?.ToString(), blob.Properties.ContentLength, blob.Properties.LastModified });
            return JsonSerializer.SerializeToElement(new { protectedState = await LifecycleOverlapState(factory, manifest), uploads = await db.ClassMediaUploadSessions.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(), blobs });
        }
        void Check(bool ok, string label) { if (!ok) throw new InvalidOperationException("Admission review observation mismatch: " + label); }
        async Task Active(string mode, bool value)
        {
            var before = await State(); using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var count = mode switch {
                "teacher" => await db.Teachers.Where(x => x.Id == teacher && x.AcademyId == academy).ExecuteUpdateAsync(s => s.SetProperty(x => x.IsActive, value)),
                "academy" => await db.Academies.Where(x => x.Id == academy).ExecuteUpdateAsync(s => s.SetProperty(x => x.IsActive, value)),
                _ => await db.Batches.Where(x => x.Id == batch && x.AcademyId == academy).ExecuteUpdateAsync(s => s.SetProperty(x => x.IsActive, value)) };
            Check(count == 1, "authority control count");
            var expected = JsonNode.Parse(before.GetRawText())!; var field = mode == "teacher" ? "teachers" : mode == "academy" ? "academies" : "batches"; var id = mode == "teacher" ? teacher : mode == "academy" ? academy : batch;
            expected["protectedState"]![field]!.AsArray().Single(x => x!["Id"]!.GetValue<Guid>() == id)!["IsActive"] = value;
            Check(JsonElement.DeepEquals(JsonSerializer.SerializeToElement(expected), await State()), "authority control changed unrelated data");
        }
        var rows = new List<object>(); var requests = 1; var findings = 0;
        foreach (var mode in new[] { "teacher", "academy", "batch" }) foreach (var operation in new[] { "stage", "commit" })
        {
            var bytes = new byte[] { 71, 72, 73 }; var title = "QA admitted " + mode + " " + operation;
            await Task.Delay(650);
            using var created = await client.PostAsJsonAsync("/api/teacher/media-uploads", new { clientRequestId = Guid.NewGuid(), batchId = batch, studentId = student, fileName = "admitted.bin", length = bytes.Length, title }); requests++;
            Check(created.StatusCode == HttpStatusCode.Created, "fixture create201");
            using var json = JsonDocument.Parse(await created.Content.ReadAsStringAsync()); var id = json.RootElement.GetProperty("id").GetGuid(); var root = $"/api/teacher/media-uploads/{id}";
            var upload = MediaUpload.Create(academy, id, "admitted.bin", bytes.Length, new MediaStorageOptions());
            HttpRequestMessage Stage() => new(HttpMethod.Put, root + "/chunks/0") { Content = new ByteArrayContent(bytes) { Headers = { ContentType = new("application/octet-stream") } } };
            if (operation == "commit") { await Task.Delay(650); using var staged = await client.SendAsync(Stage()); requests++; Check(staged.StatusCode == HttpStatusCode.OK, "pre-stage200"); }
            gate.Arm(id, operation); using var command = operation == "stage" ? Stage() : new HttpRequestMessage(HttpMethod.Post, root + "/complete");
            await Task.Delay(650); var pending = client.SendAsync(command); requests++;
            try
            {
                await gate.Entered.WaitAsync(TimeSpan.FromSeconds(15));
                // Arrival proves CurrentTeacher/AuthorizedSession/domain policy already ran.
                var beforeRevoke = await State(); await Active(mode, false); var revoked = await State();
                await Task.Delay(650); using var newRequest = await client.GetAsync(root); requests++; Check(newRequest.StatusCode == HttpStatusCode.NotFound, "new request must404 after revocation");
                Check(JsonElement.DeepEquals(revoked, await State()), "fresh denied request mutated state");
                var progressBefore = await fixture.Store.ProgressAsync(upload, default); Check(!progressBefore.Committed && progressBefore.UploadedBlocks.SequenceEqual(operation == "commit" ? new[] { 0 } : Array.Empty<int>()), "held provider already changed bytes");
                gate.Release(); using var admitted = await pending.WaitAsync(TimeSpan.FromSeconds(15)); var status = (int)admitted.StatusCode;
                Check(admitted.StatusCode is HttpStatusCode.OK or HttpStatusCode.NotFound, "unexpected in-flight status (including429)");
                var after = await State(); var progressAfter = await fixture.Store.ProgressAsync(upload, default);
                using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
                var resourceCount = await db.LearningResources.CountAsync(x => x.Id == id); var notificationCount = await db.Notifications.CountAsync(x => x.Message == title + " is available in your class history.");
                if (admitted.StatusCode == HttpStatusCode.OK)
                {
                    findings++;
                    if (operation == "stage") { Check(JsonElement.DeepEquals(revoked, after), "stage changed SQL/committed data"); Check(!progressAfter.Committed && progressAfter.UploadedBlocks.SequenceEqual(new[] { 0 }) && resourceCount == 0 && notificationCount == 0, "stage observation differs"); }
                    else
                    {
                        Check(progressAfter.Committed && resourceCount == 1 && notificationCount == 1, "publication observation differs");
                        LifecycleSame(revoked.GetProperty("protectedState"), after.GetProperty("protectedState"), "resources", "notifications");
                        var expected = JsonNode.Parse(revoked.GetProperty("uploads").GetRawText())!.AsArray(); var completed = after.GetProperty("uploads").EnumerateArray().Single(x => x.GetProperty("Id").GetGuid() == id);
                        expected.Single(x => x!["Id"]!.GetValue<Guid>() == id)!["CompletedAtUtc"] = JsonNode.Parse(completed.GetProperty("CompletedAtUtc").GetRawText());
                        Check(completed.GetProperty("CompletedAtUtc").ValueKind == JsonValueKind.String && JsonElement.DeepEquals(JsonSerializer.SerializeToElement(expected), after.GetProperty("uploads")), "publication changed immutable/other upload rows");
                        foreach (var field in new[] { "resources", "notifications" })
                        {
                            var old = revoked.GetProperty("protectedState").GetProperty(field); var next = after.GetProperty("protectedState").GetProperty(field); Check(next.GetArrayLength() == old.GetArrayLength() + 1, "publication delta count");
                            foreach (var value in old.EnumerateArray()) Check(next.EnumerateArray().Any(x => JsonElement.DeepEquals(x, value)), "publication changed previous row");
                        }
                        Check(after.GetProperty("blobs").GetArrayLength() == revoked.GetProperty("blobs").GetArrayLength() + 1, "publication committed Blob count");
                        foreach (var value in revoked.GetProperty("blobs").EnumerateArray()) Check(after.GetProperty("blobs").EnumerateArray().Any(x => JsonElement.DeepEquals(x, value)), "publication replaced prior Blob");
                    }
                }
                else { Check(JsonElement.DeepEquals(revoked, after) && progressAfter.Committed == progressBefore.Committed && progressAfter.UploadedBlocks.SequenceEqual(progressBefore.UploadedBlocks), "denial changed durable state"); }
                await Active(mode, true); LifecycleSame(beforeRevoke.GetProperty("protectedState"), (await State()).GetProperty("protectedState"), operation == "commit" && status == 200 ? new[] { "resources", "notifications" } : Array.Empty<string>());
                rows.Add(new { mode, operation, freshRequest = 404, admittedStatus = status, stagedBlocks = progressAfter.UploadedBlocks, committed = progressAfter.Committed, resourceCount, notificationCount, guarded = status == 404 });
                Console.WriteLine($"BLOB ADMISSION OBSERVED:{mode}/{operation},new-request404,admitted{status},committed={progressAfter.Committed},resources={resourceCount},notifications={notificationCount};{(status == 404 ? "GUARDED" : "NOT GUARDED")}.");
            }
            finally { gate.Release(); await pending.WaitAsync(TimeSpan.FromSeconds(15)); }
        }
        var evidence = Path.Combine(Directory.GetCurrentDirectory(), "QA/EVIDENCE/media-admission", manifest.RunId.ToString("N")); Directory.CreateDirectory(evidence);
        await File.WriteAllTextAsync(Path.Combine(evidence, "receipt.json"), JsonSerializer.Serialize(new { classification = "real SQL/API/private Azurite; provider timing gate only; NOT release acceptance", requests, findings, cases = rows }, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine($"BLOB ADMISSION SUMMARY:6 coordinated after-policy cases/{requests} HTTP/{findings} NOT GUARDED; receipt={evidence}; no429; fixture authority restored.");
        Check(findings == 0, "in-flight writes continued after current persisted authority was revoked; security gate FAILED");
    }
}

internal sealed class MediaAdmissionGate(IMediaBlobStore inner) : IMediaBlobStore
{
    private Guid target; private string? operation;
    private TaskCompletionSource entered = new(TaskCreationOptions.RunContinuationsAsynchronously), released = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public Task Entered => entered.Task;
    public bool IsConfigured => inner.IsConfigured;
    public void Arm(Guid id, string kind) { if(operation is not null) throw new InvalidOperationException("Provider gate already armed"); target = id; operation = kind; entered = new(TaskCreationOptions.RunContinuationsAsynchronously); released = new(TaskCreationOptions.RunContinuationsAsynchronously); }
    public void Release() { released.TrySetResult(); }
    private async Task Pause(MediaUpload upload, string kind, CancellationToken token)
    { if(upload.ResourceId == target && operation == kind) { operation = null; entered.TrySetResult(); await released.Task.WaitAsync(token); } }
    public async Task StageAsync(MediaUpload upload, int index, Stream content, CancellationToken token) { await Pause(upload, "stage", token); await inner.StageAsync(upload, index, content, token); }
    public async Task<MediaBlobInfo> CommitAsync(MediaUpload upload, CancellationToken token) { await Pause(upload, "commit", token); return await inner.CommitAsync(upload, token); }
    public Task<MediaUploadProgress> ProgressAsync(MediaUpload upload, CancellationToken token) => inner.ProgressAsync(upload, token);
    public Task<MediaBlobInfo> PropertiesAsync(Guid academyId, Guid id, CancellationToken token) => inner.PropertiesAsync(academyId, id, token);
    public Task<MediaBlobRead> ReadAsync(Guid academyId, Guid id, long offset, long? length, CancellationToken token) => inner.ReadAsync(academyId, id, offset, length, token);
}
