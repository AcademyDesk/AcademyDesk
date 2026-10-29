using System.Data;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Infrastructure.Media;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

internal static partial class SqlHarnessEntryPoint
{
    // Explicit publication boundary. The earlier cancel-every-provider-write
    // oracle and its failed evidence stay unchanged, not relabelled PASS.
    private static async Task VerifyMediaPublicationGuardAsync(QaBlobFixture fixture, QaRunManifest manifest)
    {
        var providerGate = new MediaAdmissionGate(fixture.Store); var sqlGate = new MediaPublicationSqlGate();
        using var factory = new QaApiFactory(manifest, isolatedMediaStore: providerGate, isolatedDomainInterceptor: sqlGate);
        using var client = factory.CreateClient(new() { AllowAutoRedirect = false });
        var owner = await LoginAsync(client, "qa-teacher-a@example.invalid", "Synthetic!39Ab"); var family = await LoginAsync(client, "qa-blob-student@example.invalid", "Synthetic!39Ab");
        client.DefaultRequestHeaders.Authorization = new("Bearer", owner);
        Guid academy, teacher, batch, student, actor, role; string stamp;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); var identity = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            academy = await db.Academies.Where(x => x.Name == "Synthetic Academy A").Select(x => x.Id).SingleAsync(); teacher = await db.Teachers.Where(x => x.AcademyId == academy).Select(x => x.Id).SingleAsync();
            batch = await db.Batches.Where(x => x.AcademyId == academy).Select(x => x.Id).SingleAsync(); student = await db.Students.Where(x => x.AcademyId == academy).Select(x => x.Id).SingleAsync();
            var user = await identity.Users.AsNoTracking().SingleAsync(x => x.Email == "qa-teacher-a@example.invalid"); actor = user.Id; stamp = user.SecurityStamp!; role = await identity.Roles.Where(x => x.Name == "Teacher").Select(x => x.Id).SingleAsync();
        }
        var requests = 2; var restored = 0; var privateStages = 0; var denied = 0; var lockControls = 0; var rows = new List<object>();
        void Check(bool ok, string label) { if (!ok) throw new InvalidOperationException("Publication guard: " + label); }
        async Task<JsonElement> State()
        {
            using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); var identity = scope.ServiceProvider.GetRequiredService<IdentityDbContext>(); var blobs = new List<object>();
            await foreach (var b in fixture.Container.GetBlobsAsync()) blobs.Add(new { b.Name, etag = b.Properties.ETag?.ToString(), b.Properties.ContentLength, b.Properties.LastModified });
            var users = await identity.Users.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync();
            return JsonSerializer.SerializeToElement(new { protectedState = await LifecycleOverlapState(factory, manifest), uploads = await db.ClassMediaUploadSessions.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(), blobs,
                authority = users.Select(x => new { x.Id, x.IsActive, x.AcademyId, x.TeacherId, stampHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(x.SecurityStamp ?? ""))) }) });
        }
        async Task Change(string mode, bool active)
        {
            var before = await State(); using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); var identity = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            if (mode == "account") await identity.Users.Where(x => x.Id == actor).ExecuteUpdateAsync(s => s.SetProperty(x => x.IsActive, active));
            else if (mode == "stamp") await identity.Users.Where(x => x.Id == actor).ExecuteUpdateAsync(s => s.SetProperty(x => x.SecurityStamp, active ? stamp : "Synthetic-revoked-stamp"));
            else if (mode == "role") { if (active) { identity.UserRoles.Add(new() { UserId = actor, RoleId = role }); await identity.SaveChangesAsync(); } else Check(await identity.UserRoles.Where(x => x.UserId == actor && x.RoleId == role).ExecuteDeleteAsync() == 1, "role control"); }
            else
            {
                var count = mode switch { "teacher" => await db.Teachers.Where(x => x.Id == teacher).ExecuteUpdateAsync(s => s.SetProperty(x => x.IsActive, active)), "academy" => await db.Academies.Where(x => x.Id == academy).ExecuteUpdateAsync(s => s.SetProperty(x => x.IsActive, active)), _ => await db.Batches.Where(x => x.Id == batch).ExecuteUpdateAsync(s => s.SetProperty(x => x.IsActive, active)) };
                Check(count == 1, "domain control");
            }
            var after = await State(); LifecycleSame(before.GetProperty("protectedState"), after.GetProperty("protectedState"), mode switch { "teacher" => "teachers", "academy" => "academies", "batch" => "batches", "role" => "roles", _ => "users" });
            Check(JsonElement.DeepEquals(before.GetProperty("uploads"), after.GetProperty("uploads")) && JsonElement.DeepEquals(before.GetProperty("blobs"), after.GetProperty("blobs")), "authority control altered uploads/Blob");
        }
        async Task<HttpResponseMessage> Send(HttpRequestMessage command)
        { await Task.Delay(650); using (command) { var result = await client.SendAsync(command); requests++; Check(result.StatusCode != HttpStatusCode.TooManyRequests, "429 cannot pass"); return result; } }
        void Retained(JsonElement old, JsonElement next, int added) { Check(next.GetArrayLength() == old.GetArrayLength() + added, "delta count"); foreach (var value in old.EnumerateArray()) Check(next.EnumerateArray().Any(x => JsonElement.DeepEquals(x, value)), "previous row/Blob changed"); }
        async Task Publish(Guid id, string title, JsonElement before, byte[] bytes)
        {
            var root = $"/api/teacher/media-uploads/{id}"; using var done = await Send(new(HttpMethod.Post, root + "/complete")); Check(done.StatusCode == HttpStatusCode.OK, "restored completion200");
            var after = await State(); LifecycleSame(before.GetProperty("protectedState"), after.GetProperty("protectedState"), "resources", "notifications"); Check(JsonElement.DeepEquals(before.GetProperty("authority"), after.GetProperty("authority")), "publication altered identity authority");
            var expected = JsonNode.Parse(before.GetProperty("uploads").GetRawText())!.AsArray(); var completed = after.GetProperty("uploads").EnumerateArray().Single(x => x.GetProperty("Id").GetGuid() == id); Check(completed.GetProperty("CompletedAtUtc").ValueKind == JsonValueKind.String, "completion timestamp missing");
            expected.Single(x => x!["Id"]!.GetValue<Guid>() == id)!["CompletedAtUtc"] = JsonNode.Parse(completed.GetProperty("CompletedAtUtc").GetRawText()); Check(JsonElement.DeepEquals(JsonSerializer.SerializeToElement(expected), after.GetProperty("uploads")), "completion altered immutable session");
            foreach (var field in new[] { "resources", "notifications" }) Retained(before.GetProperty("protectedState").GetProperty(field), after.GetProperty("protectedState").GetProperty(field), 1);
            using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); var resource = await db.LearningResources.AsNoTracking().SingleAsync(x => x.Id == id);
            Check(resource.AcademyId == academy && resource.BatchId == batch && resource.StudentId == student && resource.Title == title && resource.IsPublished, "published scope differs");
            Check(await db.Notifications.CountAsync(x => x.AcademyId == academy && x.RecipientId == student && x.RecipientType == "Student" && x.Message == title + " is available in your class history.") == 1, "notification recipient/count");
            // A previously committed private recovery Blob must not be replaced.
            if (before.GetProperty("blobs").GetArrayLength() == after.GetProperty("blobs").GetArrayLength()) Check(JsonElement.DeepEquals(before.GetProperty("blobs"), after.GetProperty("blobs")), "recovery changed committed Blob ETag");
            else Retained(before.GetProperty("blobs"), after.GetProperty("blobs"), 1);
            using var read = new HttpRequestMessage(HttpMethod.Get, $"/api/class-media/{id}/content"); read.Headers.Authorization = new("Bearer", family); using var downloaded = await Send(read); Check(downloaded.StatusCode == HttpStatusCode.OK && (await downloaded.Content.ReadAsByteArrayAsync()).SequenceEqual(bytes), "restored exact Student readback");
            using var retry = await Send(new(HttpMethod.Post, root + "/complete")); Check(retry.StatusCode == HttpStatusCode.OK && JsonElement.DeepEquals(after, await State()), "repeat completion duplicated state"); restored++;
        }
        foreach (var mode in new[] { "teacher", "academy", "batch", "account", "role", "stamp" })
        foreach (var operation in mode is "teacher" or "academy" or "batch" ? new[] { "stage", "commit" } : new[] { "commit" })
        {
            var title = "QA guarded " + mode + " " + operation; var bytes = new byte[] { 81, 82, 83 }; using var created = await Send(new(HttpMethod.Post, "/api/teacher/media-uploads") { Content = JsonContent.Create(new { clientRequestId = Guid.NewGuid(), batchId = batch, studentId = student, fileName = "guarded.bin", length = bytes.Length, title }) }); Check(created.StatusCode == HttpStatusCode.Created, "fixture201");
            using var json = JsonDocument.Parse(await created.Content.ReadAsStringAsync()); var id = json.RootElement.GetProperty("id").GetGuid(); var root = $"/api/teacher/media-uploads/{id}"; var upload = MediaUpload.Create(academy, id, "guarded.bin", bytes.Length, new MediaStorageOptions());
            HttpRequestMessage Stage() => new(HttpMethod.Put, root + "/chunks/0") { Content = new ByteArrayContent(bytes) { Headers = { ContentType = new("application/octet-stream") } } };
            if (operation == "commit") { using var staged = await Send(Stage()); Check(staged.StatusCode == HttpStatusCode.OK, "fixture stage200"); }
            var before = await State(); providerGate.Arm(id, operation); using var command = operation == "stage" ? Stage() : new(HttpMethod.Post, root + "/complete"); await Task.Delay(650); var pending = client.SendAsync(command); requests++;
            try
            {
                await providerGate.Entered.WaitAsync(TimeSpan.FromSeconds(15)); await Change(mode, false); var revoked = await State(); providerGate.Release(); using var result = await pending.WaitAsync(TimeSpan.FromSeconds(15));
                Check(result.StatusCode == (operation == "stage" ? HttpStatusCode.OK : HttpStatusCode.NotFound), $"{mode}/{operation} admitted publication boundary expected{(operation == "stage" ? 200 : 404)},actual{(int)result.StatusCode}"); var after = await State();
                Check(JsonElement.DeepEquals(revoked.GetProperty("protectedState"), after.GetProperty("protectedState")) && JsonElement.DeepEquals(revoked.GetProperty("authority"), after.GetProperty("authority")) && JsonElement.DeepEquals(revoked.GetProperty("uploads"), after.GetProperty("uploads")), "revoked publication changed SQL/Identity");
                var progress = await fixture.Store.ProgressAsync(upload, default); Check(progress.UploadedBlocks.SequenceEqual(new[] { 0 }) && progress.Committed == (operation == "commit"), "private recovery progress");
                if (operation == "commit") { Retained(revoked.GetProperty("blobs"), after.GetProperty("blobs"), 1); denied++; } else { Check(JsonElement.DeepEquals(revoked.GetProperty("blobs"), after.GetProperty("blobs")), "stage committed Blob"); privateStages++; }
                using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/class-media/{id}/content"); request.Headers.Authorization = new("Bearer", family); using var hidden = await Send(request); Check(hidden.StatusCode == HttpStatusCode.NotFound && JsonElement.DeepEquals(after, await State()), "unpublished private Blob visible or changed state");
                // Even the already-admitted stage cannot subsequently publish while revoked.
                using var newCompletion = await Send(new(HttpMethod.Post, root + "/complete")); Check((int)newCompletion.StatusCode == (mode == "account" ? 403 : mode == "stamp" ? 401 : 404) && JsonElement.DeepEquals(after, await State()), "new revoked completion not denied");
                await Change(mode, true); var resumed = await State(); LifecycleSame(before.GetProperty("protectedState"), resumed.GetProperty("protectedState")); Check(JsonElement.DeepEquals(before.GetProperty("authority"), resumed.GetProperty("authority")), "authority restoration incomplete");
                await Publish(id, title, resumed, bytes); rows.Add(new { mode, operation, admittedStatus = (int)result.StatusCode, privateBytesRetained = true, hiddenRead = 404, restoredPublication = 200, repeatedCompletion = 200 });
                Console.WriteLine($"BLOB PUBLICATION case PASS:{mode}/{operation},admitted{(int)result.StatusCode},no revoked resource/notification,private bytes hidden,original session restored/exact Student bytes/idempotent200.");
            }
            finally { providerGate.Release(); await pending.WaitAsync(TimeSpan.FromSeconds(15)); }
        }
        // Pause at SaveChanges after all authority reads, while publication locks
        // are held. Actual SQL LOCK_TIMEOUT1222 proves revocation cannot commit
        // between those reads and publication; no timing-only 'still pending'.
        var lockedTitle = "QA guarded SQL ordering"; var lockedBytes = new byte[] { 91, 92, 93 };
        using var made = await Send(new(HttpMethod.Post, "/api/teacher/media-uploads") { Content = JsonContent.Create(new { clientRequestId = Guid.NewGuid(), batchId = batch, studentId = student, fileName = "ordering.bin", length = 3, title = lockedTitle }) }); Check(made.StatusCode == HttpStatusCode.Created, "ordering fixture201"); using var madeJson = JsonDocument.Parse(await made.Content.ReadAsStringAsync()); var lockedId = madeJson.RootElement.GetProperty("id").GetGuid();
        using (var stage = await Send(new(HttpMethod.Put, $"/api/teacher/media-uploads/{lockedId}/chunks/0") { Content = new ByteArrayContent(lockedBytes) { Headers = { ContentType = new("application/octet-stream") } } })) Check(stage.StatusCode == HttpStatusCode.OK, "ordering fixture stage200");
        sqlGate.Arm(lockedId); await Task.Delay(650); var lockedPending = client.PostAsync($"/api/teacher/media-uploads/{lockedId}/complete", null); requests++;
        try
        {
            await sqlGate.Entered.WaitAsync(TimeSpan.FromSeconds(15));
            var connectionString = Connection(manifest.SqlServer, manifest.Database, manifest.RuntimeLogin, manifest.RuntimePassword);
            foreach (var mode in new[] { "academy", "teacher", "batch", "account", "role", "stamp" })
            {
                await using var connection = new SqlConnection(connectionString); await connection.OpenAsync(); await using var change = connection.CreateCommand(); change.CommandTimeout = 5;
                change.CommandText = mode switch { "academy" => "SET LOCK_TIMEOUT 1000; UPDATE Academies SET IsActive=0 WHERE Id=@id", "teacher" => "SET LOCK_TIMEOUT 1000; UPDATE Teachers SET IsActive=0 WHERE Id=@id", "batch" => "SET LOCK_TIMEOUT 1000; UPDATE Batches SET IsActive=0 WHERE Id=@id", "account" => "SET LOCK_TIMEOUT 1000; UPDATE AspNetUsers SET IsActive=0 WHERE Id=@id", "role" => "SET LOCK_TIMEOUT 1000; DELETE FROM AspNetUserRoles WHERE UserId=@id AND RoleId=@role", _ => "SET LOCK_TIMEOUT 1000; UPDATE AspNetUsers SET SecurityStamp=N'Synthetic-revoked' WHERE Id=@id" };
                change.Parameters.Add(new SqlParameter("@id", SqlDbType.UniqueIdentifier) { Value = mode == "academy" ? academy : mode == "teacher" ? teacher : mode == "batch" ? batch : actor }); if(mode == "role") change.Parameters.Add(new SqlParameter("@role", SqlDbType.UniqueIdentifier) { Value = role });
                var blocked = false; try { await change.ExecuteNonQueryAsync(); } catch(SqlException error) when(error.Number == 1222) { blocked = true; } Check(blocked, "authority write not blocked inside publication: " + mode); lockControls++;
            }
            sqlGate.Release(); using var result = await lockedPending.WaitAsync(TimeSpan.FromSeconds(15)); Check(result.StatusCode == HttpStatusCode.OK, "locked publication200");
            Console.WriteLine("BLOB PUBLICATION SQL ordering PASS:6 actual authority UPDATE/DELETE1222 lock controls; publication200; domain+Identity locks held through saved outcome.");
        }
        finally { sqlGate.Release(); await lockedPending.WaitAsync(TimeSpan.FromSeconds(15)); }
        Check(restored == 9 && denied == 6 && privateStages == 3 && lockControls == 6, "grouped case counts");
        var evidence = Path.Combine(Directory.GetCurrentDirectory(), "QA/EVIDENCE/media-publication", manifest.RunId.ToString("N")); Directory.CreateDirectory(evidence);
        await File.WriteAllTextAsync(Path.Combine(evidence, "receipt.json"), JsonSerializer.Serialize(new { classification = "real SQL/API/private Azurite; private staging may finish; publication reauthorized under domain+Identity transaction", requests, restored, denied, privateStages, lockControls, cases = rows }, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine($"BLOB PUBLICATION SUMMARY PASS:9 revoked/restored cases,6 admitted-completion404,3 private-stage200,9 exact-byte Student recoveries/9 idempotent retries,6 SQL lock controls,{requests} HTTP/no429; no browser/physical/full-load acceptance.");
    }
}

internal sealed class MediaPublicationSqlGate : SaveChangesInterceptor
{
    private Guid target;
    private TaskCompletionSource entered = new(TaskCreationOptions.RunContinuationsAsynchronously), released = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public Task Entered => entered.Task;
    public void Arm(Guid id) { target = id; }
    public void Release() => released.TrySetResult();
    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData data, InterceptionResult<int> result, CancellationToken token = default)
    {
        if(target != Guid.Empty && data.Context is AcademyDeskDbContext db && db.ChangeTracker.Entries<AcademyDesk.Api.Domain.Entities.LearningResource>().Any(x => x.State == EntityState.Added && x.Entity.Id == target))
        { target = Guid.Empty; entered.TrySetResult(); await released.Task.WaitAsync(token); }
        return result;
    }
}
