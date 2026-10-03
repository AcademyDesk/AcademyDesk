using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Domain.Identity;
using AcademyDesk.Api.Infrastructure.Media;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.DataProtection;

internal static partial class SqlHarnessEntryPoint
{
    private static async Task VerifyBlobUnavailableAsync(QaApiFactory factory, HttpClient client)
    {
        var teacherToken = await LoginAsync(client, "qa-teacher-a@example.invalid", "Synthetic!39Ab");
        client.DefaultRequestHeaders.Authorization = new("Bearer", teacherToken);
        using var result = await client.PostAsJsonAsync("/api/teacher/media-uploads", new { clientRequestId = Guid.NewGuid(), batchId = Guid.NewGuid(), fileName = "test.mov", length = 1 });
        using var scope = factory.Services.CreateScope();
        if (result.StatusCode != HttpStatusCode.ServiceUnavailable || await scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>().ClassMediaUploadSessions.AnyAsync())
            throw new InvalidOperationException("Disabled storage did not fail closed without SQL writes.");
        Console.WriteLine("BLOB disabled PASS: HTTP 503, no session or local disk fallback.");
    }

    private static async Task VerifyBlobUploadsAsync(QaApiFactory factory, HttpClient client, QaBlobFixture fixture, QaRunManifest manifest)
    {
        var checks = 0;
        void Check(bool passed, string name) { if (!passed) throw new InvalidOperationException("BLOB regression FAIL: " + name); checks++; Console.WriteLine("BLOB regression PASS: " + name); }
        async Task Status(HttpResponseMessage r, HttpStatusCode code, string name)
        {
            using (r) { var body = await r.Content.ReadAsStringAsync(); Check(r.StatusCode == code, name + $" (expected {(int)code}, actual {(int)r.StatusCode})"); }
        }
        async Task Mutate(Func<AcademyDeskDbContext, Task> change)
        { using var scope = factory.Services.CreateScope(); await change(scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>()); }
        Guid academy, batch, student, teacher;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            academy = await db.Academies.Where(x => x.Name == "Synthetic Academy A").Select(x => x.Id).SingleAsync();
            batch = await db.Batches.Where(x => x.AcademyId == academy).Select(x => x.Id).SingleAsync();
            student = await db.Students.Where(x => x.AcademyId == academy).Select(x => x.Id).SingleAsync();
            teacher = await db.Teachers.Where(x => x.AcademyId == academy).Select(x => x.Id).SingleAsync();
            db.Enrollments.Add(new Enrollment { AcademyId = academy, StudentId = student, BatchId = batch }); await db.SaveChangesAsync();
            var roles = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
            if (!(await roles.CreateAsync(new ApplicationRole { Name = "Student", IsSystemRole = true })).Succeeded) throw new InvalidOperationException("Student fixture role failed.");
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            foreach (var (email, role, studentId, teacherId) in new[] {
                ("qa-blob-student@example.invalid", "Student", (Guid?)student, (Guid?)null),
                ("qa-blob-other-owner@example.invalid", "Teacher", (Guid?)null, (Guid?)teacher) })
            {
                var user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true, DisplayName = "Synthetic Blob Fixture", AcademyId = academy, StudentId = studentId, TeacherId = teacherId, IsActive = true };
                if (!(await users.CreateAsync(user, "Synthetic!39Ab")).Succeeded || !(await users.AddToRoleAsync(user, role)).Succeeded) throw new InvalidOperationException("Blob fixture identity failed.");
            }
        }
        var owner = await LoginAsync(client, "qa-teacher-a@example.invalid", "Synthetic!39Ab");
        client.DefaultRequestHeaders.Authorization = new("Bearer", owner);
        using (var me = await client.GetAsync("/api/teacher/me"))
        {
            using var body = JsonDocument.Parse(await me.Content.ReadAsStringAsync());
            using var scope = factory.Services.CreateScope();
            var expectedOwner = await scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>().FindByEmailAsync("qa-teacher-a@example.invalid");
            Check(me.StatusCode == HttpStatusCode.OK && body.RootElement.GetProperty("userId").GetGuid() == expectedOwner!.Id, "teacher summary provides current login ID for browser draft isolation");
        }
        var otherOwner = await LoginAsync(client, "qa-blob-other-owner@example.invalid", "Synthetic!39Ab");
        var tenantB = await LoginAsync(client, "qa-admin-b@example.invalid", "Synthetic!39Ab");
        var family = await LoginAsync(client, "qa-blob-student@example.invalid", "Synthetic!39Ab");
        void Auth(string? token) => client.DefaultRequestHeaders.Authorization = token is null ? null : new("Bearer", token);
        var data = RandomNumberGenerator.GetBytes(MediaStorageOptions.DefaultChunkBytes + 37);
        var request = new { clientRequestId = Guid.NewGuid(), batchId = batch, studentId = student, fileName = "பாடம்-phone.mov", length = data.Length };
        Auth(null); await Status(await client.PostAsJsonAsync("/api/teacher/media-uploads", request), HttpStatusCode.Unauthorized, "anonymous creation denied");
        Auth(owner);
        await Status(await client.PostAsJsonAsync("/api/teacher/media-uploads", new { clientRequestId = Guid.NewGuid(), batchId = batch, fileName = "script.exe ", length = 1 }), HttpStatusCode.BadRequest, "active content with trailing space rejected");
        await Status(await client.PostAsJsonAsync("/api/teacher/media-uploads", new { clientRequestId = Guid.NewGuid(), batchId = batch, classSessionId = Guid.NewGuid(), fileName = "file.mov", length = 1 }), HttpStatusCode.BadRequest, "invalid class scope rejected");
        Guid id;
        using (var create = await client.PostAsJsonAsync("/api/teacher/media-uploads", request))
        {
            Check(create.StatusCode == HttpStatusCode.Created, "optional title/description/session omitted, create 201");
            using var body = JsonDocument.Parse(await create.Content.ReadAsStringAsync()); id = body.RootElement.GetProperty("id").GetGuid();
        }
        var root = $"/api/teacher/media-uploads/{id}"; var content = $"/api/class-media/{id}/content";
        using (var retry = await client.PostAsJsonAsync("/api/teacher/media-uploads", request))
        {
            using var body = JsonDocument.Parse(await retry.Content.ReadAsStringAsync());
            Check(retry.StatusCode == HttpStatusCode.OK && body.RootElement.GetProperty("id").GetGuid() == id, "create retry uses same session ID");
        }
        await Status(await client.PostAsJsonAsync("/api/teacher/media-uploads", new { request.clientRequestId, batchId = batch, studentId = student, fileName = "other.mov", length = data.Length }), HttpStatusCode.Conflict, "request ID cannot change immutable file metadata");
        await Status(await client.GetAsync(content), HttpStatusCode.NotFound, "pending content invisible");
        await Status(await client.PostAsync(root + "/complete", null), HttpStatusCode.Conflict, "missing blocks cannot publish");
        async Task<HttpResponseMessage> Chunk(int index, byte[] bytes)
        {
            using var body = new ByteArrayContent(bytes); body.Headers.ContentType = new("application/octet-stream");
            return await client.PutAsync(root + "/chunks/" + index, body);
        }
        await Status(await Chunk(-1, [1]), HttpStatusCode.BadRequest, "invalid chunk index rejected");
        await Status(await Chunk(0, [1]), HttpStatusCode.BadRequest, "incorrect chunk length rejected");
        var head = data[..MediaStorageOptions.DefaultChunkBytes];
        await Status(await Chunk(0, head), HttpStatusCode.OK, "first full bounded chunk uploaded");
        await Status(await Chunk(0, head), HttpStatusCode.OK, "chunk retry does not create a second block");
        using (var resume = await client.GetAsync(root))
        {
            using var body = JsonDocument.Parse(await resume.Content.ReadAsStringAsync());
            Check(resume.StatusCode == HttpStatusCode.OK && body.RootElement.GetProperty("uploadedBlocks").EnumerateArray().Select(x => x.GetInt32()).SequenceEqual([0]), "durable SQL session resumes staged progress");
        }
        using (var restarted = new QaApiFactory(manifest, isolatedMediaStore: new AzureMediaBlobStore(fixture.Container, new MediaStorageOptions())))
        using (var restartedClient = restarted.CreateClient(new() { AllowAutoRedirect = false }))
        {
            restartedClient.DefaultRequestHeaders.Authorization = new("Bearer", owner);
            using var resume = await restartedClient.GetAsync(root);
            using var body = JsonDocument.Parse(await resume.Content.ReadAsStringAsync());
            Check(resume.StatusCode == HttpStatusCode.OK && body.RootElement.GetProperty("uploadedBlocks").EnumerateArray().Select(x => x.GetInt32()).SequenceEqual([0]), "fresh HTTP host and Blob adapter resume same SQL session");
        }
        foreach (var (token, code, name) in new[] { ((string?)null, HttpStatusCode.Unauthorized, "anonymous"), (tenantB, HttpStatusCode.NotFound, "tenant B"), (otherOwner, HttpStatusCode.NotFound, "another login with same teacher ID") })
        {
            Auth(token); await Status(await client.GetAsync(root), code, name + " cannot read session");
            await Status(await Chunk(1, data[^37..]), code, name + " cannot stage tail");
            await Status(await client.PostAsync(root + "/complete", null), code, name + " cannot complete");
        }
        Auth(owner);
        await Mutate(async db => { await db.Batches.Where(x => x.Id == batch).ExecuteUpdateAsync(s => s.SetProperty(x => x.IsActive, false)); });
        await Status(await client.GetAsync(root), HttpStatusCode.NotFound, "inactive assigned batch denies existing session");
        await Mutate(async db => { await db.Batches.Where(x => x.Id == batch).ExecuteUpdateAsync(s => s.SetProperty(x => x.IsActive, true)); });
        await Status(await Chunk(1, data[^37..]), HttpStatusCode.OK, "exact tail uploaded");
        // Both requests race through real SQL/Blob; a SQL row-update claim makes
        // resource creation and notification insertion one transaction.
        var completes = await Task.WhenAll(client.PostAsync(root + "/complete", null), client.PostAsync(root + "/complete", null));
        foreach (var result in completes) await Status(result, HttpStatusCode.OK, "concurrent completion succeeds idempotently");
        await Mutate(async db => Check(await db.LearningResources.CountAsync(x => x.Id == id) == 1 && await db.Notifications.CountAsync(x => x.AcademyId == academy) == 1 && await db.ClassMediaUploadSessions.CountAsync() == 1, "one session, one resource, one notification after retries"));
        await Status(await Chunk(0, head), HttpStatusCode.Conflict, "completed content immutable");
        Auth(family);
        using (var download = await client.GetAsync(content))
            Check(download.StatusCode == HttpStatusCode.OK && (await download.Content.ReadAsByteArrayAsync()).SequenceEqual(data) && download.Content.Headers.ContentDisposition?.DispositionType == "attachment", "authorized enrolled student gets exact attachment bytes");
        async Task Range(string range, HttpStatusCode code, byte[]? expected)
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, content); req.Headers.TryAddWithoutValidation("Range", range);
            using var result = await client.SendAsync(req);
            Check(result.StatusCode == code && (expected is null || (await result.Content.ReadAsByteArrayAsync()).SequenceEqual(expected)), "range " + range + " returns exact status/bytes");
        }
        await Range("bytes=0-7", HttpStatusCode.PartialContent, data[..8]);
        await Range("bytes=-5", HttpStatusCode.PartialContent, data[^5..]);
        await Range("bytes=99999999-", HttpStatusCode.RequestedRangeNotSatisfiable, null);
        using (var req = new HttpRequestMessage(HttpMethod.Get, content))
        {
            req.Headers.TryAddWithoutValidation("Range", "bytes=0-7"); req.Headers.TryAddWithoutValidation("If-Range", "\"unknown-etag\"");
            using var result = await client.SendAsync(req);
            Check(result.StatusCode == HttpStatusCode.OK && (await result.Content.ReadAsByteArrayAsync()).SequenceEqual(data), "unsupported If-Range safely returns full content");
        }
        using (var req = new HttpRequestMessage(HttpMethod.Head, content))
        using (var result = await client.SendAsync(req)) Check(result.StatusCode == HttpStatusCode.OK && result.Content.Headers.ContentLength == data.Length, "authorized HEAD reports exact full length");
        using (var req = new HttpRequestMessage(HttpMethod.Head, content))
        {
            req.Headers.Add("Origin", "http://localhost:3000");
            using var result = await client.SendAsync(req);
            Check(result.StatusCode == HttpStatusCode.OK && result.Headers.TryGetValues("Access-Control-Allow-Origin", out var origins) && origins.Single() == "http://localhost:3000" &&
                result.Headers.TryGetValues("Access-Control-Expose-Headers", out var exposed) && exposed.Single().Contains("Content-Disposition", StringComparison.OrdinalIgnoreCase), "approved web origin can read private attachment filename via CORS");
        }
        await VerifyNativeDownloadAsync(factory, client, content, data, family, tenantB, id, Check, Status);
        Auth(null); await Status(await client.GetAsync(content), HttpStatusCode.Unauthorized, "anonymous content denied");
        Auth(tenantB); await Status(await client.GetAsync(content), HttpStatusCode.NotFound, "tenant B content denied");
        await Mutate(async db => { await db.LearningResources.Where(x => x.Id == id).ExecuteUpdateAsync(s => s.SetProperty(x => x.IsPublished, false)); });
        Auth(family); await Status(await client.GetAsync(content), HttpStatusCode.NotFound, "unpublished content denied to family");
        using (var anonymousBlob = new HttpClient())
        using (var result = await anonymousBlob.GetAsync(fixture.Container.GetBlobClient($"teacher-media/{academy:N}/{id:N}/content").Uri))
            Check(result.StatusCode != HttpStatusCode.OK && result.StatusCode != HttpStatusCode.PartialContent, "direct anonymous Blob URL cannot bypass API");

        // Force a deterministic SQL insert collision AFTER Blob commit. No fake
        // auth/provider/DbContext: only a known synthetic row in this owned DB.
        Auth(owner); Guid recovery;
        using (var result = await client.PostAsJsonAsync("/api/teacher/media-uploads", new { clientRequestId = Guid.NewGuid(), batchId = batch, studentId = student, fileName = "retry.ogg", length = 3 }))
        { using var body = JsonDocument.Parse(await result.Content.ReadAsStringAsync()); Check(result.StatusCode == HttpStatusCode.Created, "recovery session created"); recovery = body.RootElement.GetProperty("id").GetGuid(); }
        root = $"/api/teacher/media-uploads/{recovery}";
        await Status(await Chunk(0, [1, 2, 3]), HttpStatusCode.OK, "recovery payload staged");
        await Mutate(async db => { db.LearningResources.Add(new LearningResource { Id = recovery, AcademyId = academy, Title = "Synthetic insert collision", Type = "QA", Url = "qa:collision" }); await db.SaveChangesAsync(); });
        await Status(await client.PostAsync(root + "/complete", null), HttpStatusCode.InternalServerError, "SQL collision after Blob commit produces recoverable 500");
        await Mutate(async db => Check((await db.ClassMediaUploadSessions.SingleAsync(x => x.Id == recovery)).CompletedAtUtc is null, "failed SQL completion rolls back session claim"));
        var info = await fixture.Store.PropertiesAsync(academy, recovery, default);
        Check(info.Length == 3, "committed Blob retained for recovery");
        await Mutate(async db => { var collision = await db.LearningResources.SingleAsync(x => x.Id == recovery && x.Url == "qa:collision"); db.Remove(collision); await db.SaveChangesAsync(); });
        await Status(await client.PostAsync(root + "/complete", null), HttpStatusCode.OK, "retry recovers SQL without reuploading bytes");
        Check((await fixture.Store.PropertiesAsync(academy, recovery, default)).ETag == info.ETag, "recovery preserves immutable Blob ETag");
        await Mutate(async db => Check(await db.LearningResources.CountAsync(x => x.Id == recovery) == 1 && await db.Notifications.CountAsync(x => x.AcademyId == academy) == 2, "recovery publishes once and notifies once"));
        Console.WriteLine($"BLOB summary PASS: {checks} assertions; real Identity + HTTP + SQL + private Azurite.");
    }

    private static async Task VerifyNativeDownloadAsync(QaApiFactory factory, HttpClient client, string path, byte[] expected,
        string familyToken, string tenantBToken, Guid resourceId, Action<bool, string> check,
        Func<HttpResponseMessage, HttpStatusCode, string, Task> status)
    {
        const string origin = "http://localhost:3000";
        async Task<HttpResponseMessage> Mint(string target = "", string? source = origin)
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, "/api/class-material-downloads/tickets") { Content = JsonContent.Create(new { path = target == "" ? path : target }) };
            if (source is not null) req.Headers.Add("Origin", source);
            return await client.SendAsync(req);
        }
        async Task<HttpResponseMessage> Redeem(string credential, string? source = origin, string? target = null, bool extraField = false)
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, target ?? path);
            var fields = new List<KeyValuePair<string, string>> { new("ticket", credential) }; if (extraField) fields.Add(new("unexpected", "x"));
            req.Content = new FormUrlEncodedContent(fields); req.Content.Headers.ContentLength = (await req.Content.ReadAsByteArrayAsync()).Length;
            if (source is not null) req.Headers.Add("Origin", source);
            return await client.SendAsync(req);
        }
        client.DefaultRequestHeaders.Authorization = null;
        await status(await Mint(), HttpStatusCode.Unauthorized, "native credential issuance requires account authentication");
        client.DefaultRequestHeaders.Authorization = new("Bearer", tenantBToken);
        await status(await Mint(), HttpStatusCode.NotFound, "foreign tenant cannot mint native credential");
        client.DefaultRequestHeaders.Authorization = new("Bearer", familyToken);
        await status(await Mint(source: "https://foreign.example.invalid"), HttpStatusCode.BadRequest, "unapproved origin cannot mint native credential");
        await status(await Mint(target: "/api/teacher/me"), HttpStatusCode.BadRequest, "native credential cannot target non-file endpoint");
        string ticket;
        using (var result = await Mint())
        {
            using var body = JsonDocument.Parse(await result.Content.ReadAsStringAsync());
            check(result.StatusCode == HttpStatusCode.OK && body.RootElement.GetProperty("expiresInSeconds").GetInt32() == 60 && result.Headers.CacheControl?.NoStore == true, "authorized native credential is short-lived and no-store");
            ticket = body.RootElement.GetProperty("ticket").GetString()!;
        }
        // No normal account bearer on any redemption below: actual named scheme.
        client.DefaultRequestHeaders.Authorization = null;
        using (var result = await Redeem(ticket))
            check(result.StatusCode == HttpStatusCode.OK && result.Content.Headers.ContentDisposition?.DispositionType == "attachment" &&
                (await result.Content.ReadAsByteArrayAsync()).SequenceEqual(expected), "native form POST streams exact private Blob attachment without account header");
        await status(await Redeem("X" + ticket[1..]), HttpStatusCode.Unauthorized, "tampered native credential denied");
        await status(await Redeem(ticket, source: null), HttpStatusCode.Unauthorized, "native credential missing Origin denied");
        await status(await Redeem(ticket, source: "https://foreign.example.invalid"), HttpStatusCode.Unauthorized, "native credential wrong Origin denied");
        await status(await Redeem(ticket, target: path.Replace(resourceId.ToString(), Guid.NewGuid().ToString())), HttpStatusCode.Unauthorized, "native credential wrong file denied");
        await status(await Redeem(ticket, extraField: true), HttpStatusCode.Unauthorized, "extra form fields denied");
        await status(await Redeem(new string('a', 5000)), HttpStatusCode.Unauthorized, "oversize native form denied before credential parsing");
        await status(await client.GetAsync(path + "?ticket=" + ticket), HttpStatusCode.Unauthorized, "native credential cannot authenticate GET or URL query");
        client.DefaultRequestHeaders.Authorization = new("Bearer", ticket);
        await status(await client.GetAsync("/api/teacher/me"), HttpStatusCode.Unauthorized, "native credential cannot act as account bearer on other APIs");
        client.DefaultRequestHeaders.Authorization = new("Bearer", familyToken);
        await status(await Redeem("invalid"), HttpStatusCode.Unauthorized, "ordinary bearer cannot bypass required native POST credential");
        client.DefaultRequestHeaders.Authorization = null;
        using (var scope = factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = (await users.FindByEmailAsync("qa-blob-student@example.invalid"))!;
            var stamp = user.SecurityStamp;
            await users.UpdateSecurityStampAsync(user);
            await status(await Redeem(ticket), HttpStatusCode.Unauthorized, "changed security stamp revokes issued native credential");
            user.SecurityStamp = stamp; await users.UpdateAsync(user);
            await users.RemoveFromRoleAsync(user, "Student");
            await status(await Redeem(ticket), HttpStatusCode.NotFound, "removed role revokes current native file access");
            await users.AddToRoleAsync(user, "Student");
            user.IsActive = false; await users.UpdateAsync(user);
            await status(await Redeem(ticket), HttpStatusCode.Unauthorized, "inactive identity cannot redeem issued native credential");
            user.IsActive = true; await users.UpdateAsync(user);
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            await db.LearningResources.Where(x => x.Id == resourceId).ExecuteUpdateAsync(s => s.SetProperty(x => x.IsPublished, false));
            await status(await Redeem(ticket), HttpStatusCode.NotFound, "unpublishing revokes already issued native credential");
            await db.LearningResources.Where(x => x.Id == resourceId).ExecuteUpdateAsync(s => s.SetProperty(x => x.IsPublished, true));
            // Expired protected payload produced only inside the synthetic host;
            // no clock change, sleep or production lifetime override.
            var protector = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.DataProtection.IDataProtectionProvider>()
                .CreateProtector(ClassMaterialDownloadTickets.Purpose).ToTimeLimitedDataProtector();
            var payload = JsonSerializer.Serialize(new ClassMaterialDownloadTickets.DownloadScope(user.Id, user.AcademyId!.Value, user.SecurityStamp!, path, origin));
            await status(await Redeem(protector.Protect(payload, DateTimeOffset.UtcNow.AddSeconds(-1))), HttpStatusCode.Unauthorized, "expired native credential denied through real authentication pipeline");
        }
        client.DefaultRequestHeaders.Authorization = new("Bearer", familyToken);
        foreach (var folder in new[] { "teacher-materials", "learning-resources" })
        {
            var fileName = Guid.NewGuid().ToString("N") + ".mov";
            var legacy = $"/uploads/{folder}/{fileName}";
            byte[] bytes = [11, 13, 17];
            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
                var source = await db.LearningResources.AsNoTracking().SingleAsync(x => x.Id == resourceId);
                db.LearningResources.Add(new LearningResource { AcademyId = source.AcademyId, BatchId = source.BatchId, StudentId = source.StudentId, Title = "Synthetic native legacy file", Type = "Video", Url = legacy, IsPublished = true });
                await db.SaveChangesAsync();
                var root = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>().WebRootPath;
                var directory = System.IO.Path.Combine(root, "uploads", folder);
                Directory.CreateDirectory(directory); await System.IO.File.WriteAllBytesAsync(System.IO.Path.Combine(directory, fileName), bytes);
            }
            string legacyTicket;
            using (var result = await Mint(target: legacy))
            {
                check(result.StatusCode == HttpStatusCode.OK, "authorized native ticket issued for legacy " + folder);
                using var body = JsonDocument.Parse(await result.Content.ReadAsStringAsync()); legacyTicket = body.RootElement.GetProperty("ticket").GetString()!;
            }
            client.DefaultRequestHeaders.Authorization = null;
            using (var result = await Redeem(legacyTicket, target: legacy))
                check(result.StatusCode == HttpStatusCode.OK && (await result.Content.ReadAsByteArrayAsync()).SequenceEqual(bytes) && result.Content.Headers.ContentDisposition?.DispositionType == "attachment", "native POST retrieves legacy " + folder + " exact bytes");
            await status(await client.GetAsync(legacy), HttpStatusCode.Unauthorized, "legacy " + folder + " anonymous GET remains denied");
            client.DefaultRequestHeaders.Authorization = new("Bearer", familyToken);
        }
    }
}
