using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Domain.Identity;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

internal static partial class SqlHarnessEntryPoint
{
    private static async Task VerifyTeacherLifecycleAsync(QaApiFactory factory, HttpClient client, QaRunManifest manifest)
    {
        // Diagnostic baseline is explicit, never the default acceptance gate.
        var baseline = Environment.GetEnvironmentVariable("QA_TEACHER_LIFECYCLE_BASELINE") == "1";
        Guid academy, foreign, teacher, actor; Batch own, unassigned, foreignBatch; Student student;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            academy = await db.Academies.Where(x => x.Name == "Synthetic Academy A").Select(x => x.Id).SingleAsync();
            foreign = await db.Academies.Where(x => x.Name == "Synthetic Academy B").Select(x => x.Id).SingleAsync();
            own = await db.Batches.SingleAsync(x => x.AcademyId == academy); teacher = own.TeacherId!.Value;
            student = await db.Students.SingleAsync(x => x.AcademyId == academy);
            unassigned = new() { AcademyId = academy, CourseId = own.CourseId, Name = "Lifecycle unassigned" };
            var foreignCourse = new ProgramCourse { AcademyId = foreign, Name = "Lifecycle foreign course" };
            foreignBatch = new() { AcademyId = foreign, CourseId = foreignCourse.Id, Name = "Lifecycle foreign batch" };
            db.AddRange(unassigned, foreignCourse, foreignBatch,
                new Enrollment { AcademyId = academy, BatchId = own.Id, StudentId = student.Id, Status = "Active" },
                new LearningResource { AcademyId = academy, BatchId = own.Id, Title = "Lifecycle own sentinel", Url = "note://own", IsPublished = true },
                new LearningResource { AcademyId = academy, BatchId = unassigned.Id, Title = "Lifecycle unassigned sentinel", Url = "note://unassigned", IsPublished = true },
                new LearningResource { AcademyId = foreign, BatchId = foreignBatch.Id, Title = "Lifecycle foreign sentinel", Url = "note://foreign", IsPublished = true });
            await db.SaveChangesAsync();
            actor = (await scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>().FindByEmailAsync("qa-teacher-a@example.invalid"))!.Id;
        }
        var token = await LoginAsync(client, "qa-teacher-a@example.invalid", "Synthetic!39Ab");
        var admin = await LoginAsync(client, "qa-admin-a@example.invalid", "Synthetic!39Ab");
        client.DefaultRequestHeaders.Authorization = null;
        var bytes = new byte[] { 1, 2, 3, 4, 5 }; // Synthetic legacy-upload fixture, no personal media.
        var results = new List<object>(); var count = 0;

        async Task<JsonElement> Snapshot()
        {
            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var files = Directory.GetFiles(manifest.WebRoot, "*", SearchOption.AllDirectories)
                .Concat(Directory.GetFiles(manifest.StorageRoot, "*", SearchOption.AllDirectories))
                .OrderBy(x => x, StringComparer.Ordinal).Select(x => new { path = Path.GetRelativePath(manifest.Root, x), hash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(x))) }).ToArray();
            return JsonSerializer.SerializeToElement(new
            {
                resources = await db.LearningResources.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
                notifications = await db.Notifications.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
                audits = await db.AuditLogs.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
                students = await db.Students.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
                batches = await db.Batches.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
                enrollments = await db.Enrollments.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), files
            });
        }
        HttpRequestMessage Request(string action, Guid batch, string label)
        {
            if (action == "resources") return new(HttpMethod.Get, $"/api/teacher/resources?batchId={batch}");
            if (action == "activity") return new(HttpMethod.Get, $"/api/teacher/classroom-activity?batchId={batch}");
            if (action == "note") return new(HttpMethod.Post, "/api/teacher/resources/note")
            { Content = JsonContent.Create(new TeacherCreateResourceNoteRequest(batch, null, null, label, "Synthetic lifecycle note", null)) };
            var form = new MultipartFormDataContent();
            form.Add(new StringContent(batch.ToString()), "BatchId"); form.Add(new StringContent(label), "Title");
            form.Add(new ByteArrayContent(bytes), "File", "lifecycle-fixture.pdf");
            return new(HttpMethod.Post, "/api/teacher/resources/upload") { Content = form };
        }
        static void Preserve(JsonElement before, JsonElement after, string field, int added)
        {
            var old = before.GetProperty(field).EnumerateArray().ToArray(); var current = after.GetProperty(field).EnumerateArray().ToArray();
            if (current.Length != old.Length + added || old.Any(x => !current.Any(y => JsonElement.DeepEquals(x, y))))
                throw new InvalidOperationException("Lifecycle changed prior/foreign " + field);
        }
        async Task Check(string label, HttpRequestMessage request, string? auth, HttpStatusCode expected, string? write = null, bool empty = false)
        {
            using (request)
            {
                await Task.Delay(650); // Keep the real unchanged request limiter.
                var before = await Snapshot();
                if (auth is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth);
                using var response = await client.SendAsync(request);
                RequireFinanceStatus(response, expected, label); // Exact status: a 429/401 is never substituted for a required 403.
                var after = await Snapshot();
                if (write is null)
                {
                    if (!JsonElement.DeepEquals(before, after)) throw new InvalidOperationException("Lifecycle read/denial wrote SQL, notification, audit or file: " + label);
                    if (empty && (await response.Content.ReadAsStringAsync()).Trim() != "[]") throw new InvalidOperationException("Foreign/unassigned resource leakage");
                    if (expected == HttpStatusCode.OK && !empty && request.RequestUri!.OriginalString.StartsWith("/api/teacher/", StringComparison.Ordinal) &&
                        (label.EndsWith("-resources", StringComparison.Ordinal) || label.EndsWith("-activity", StringComparison.Ordinal)))
                    {
                        var body = await response.Content.ReadAsStringAsync();
                        if (!body.Contains("Lifecycle own sentinel", StringComparison.Ordinal) || body.Contains("Lifecycle foreign sentinel", StringComparison.Ordinal) || body.Contains("Lifecycle unassigned sentinel", StringComparison.Ordinal))
                            throw new InvalidOperationException("Lifecycle own read scope differs");
                    }
                }
                else
                {
                    Preserve(before, after, "resources", 1); Preserve(before, after, "notifications", 1);
                    Preserve(before, after, "files", write == "upload" ? 1 : 0);
                    foreach (var field in new[] { "audits", "students", "batches", "enrollments" })
                        if (!JsonElement.DeepEquals(before.GetProperty(field), after.GetProperty(field))) throw new InvalidOperationException("Lifecycle write changed " + field);
                    var stored = after.GetProperty("resources").EnumerateArray().Single(x => x.GetProperty("Title").GetString() == label);
                    if (stored.GetProperty("AcademyId").GetGuid() != academy || stored.GetProperty("BatchId").GetGuid() != own.Id || !stored.GetProperty("IsPublished").GetBoolean()) throw new InvalidOperationException("Lifecycle persisted scope differs");
                    var notification = after.GetProperty("notifications").EnumerateArray().Single(x => !before.GetProperty("notifications").EnumerateArray().Any(y => JsonElement.DeepEquals(x, y)));
                    if (notification.GetProperty("AcademyId").GetGuid() != academy || notification.GetProperty("RecipientId").GetGuid() != student.Id) throw new InvalidOperationException("Lifecycle notification scope differs");
                    using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                    if (body.RootElement.GetProperty("id").GetGuid() != stored.GetProperty("Id").GetGuid()) throw new InvalidOperationException("Lifecycle returned ID differs");
                    if (write == "upload")
                    {
                        var url = stored.GetProperty("Url").GetString()!;
                        var disk = Path.Combine(manifest.WebRoot, "uploads", "teacher-materials", Path.GetFileName(url));
                        var storedBytes = await File.ReadAllBytesAsync(disk);
                        if (!url.StartsWith("/uploads/teacher-materials/", StringComparison.Ordinal) || !bytes.SequenceEqual(storedBytes)) throw new InvalidOperationException("Lifecycle upload bytes differ");
                    }
                }
                results.Add(new { label, status = (int)response.StatusCode, write, empty }); count++;
                Console.WriteLine($"TEACHERLIFE CASE {label} status={(int)response.StatusCode} {(baseline ? "OBSERVED" : "PASS")}.");
            }
        }
        async Task Four(string state, bool allowed, string? auth)
        {
            foreach (var action in new[] { "resources", "activity", "note", "upload" })
                await Check(state + "-" + action, Request(action, own.Id, state + "-" + action), auth,
                    allowed ? HttpStatusCode.OK : auth is null ? HttpStatusCode.Unauthorized : HttpStatusCode.Forbidden,
                    allowed && action is "note" or "upload" ? action : null);
        }
        async Task DomainState(bool academyActive, bool teacherActive)
        {
            using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            (await db.Academies.SingleAsync(x => x.Id == academy)).IsActive = academyActive;
            (await db.Teachers.SingleAsync(x => x.Id == teacher)).IsActive = teacherActive;
            await db.SaveChangesAsync();
            var identity = await scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>().FindByIdAsync(actor.ToString());
            if (identity?.IsActive != true || identity.AcademyId != academy || identity.TeacherId != teacher) throw new InvalidOperationException("Lifecycle fixture did not retain active linked Identity");
        }

        await Four("active", true, token);
        await Four("anonymous", false, null);
        foreach (var other in new[] { ("foreign", foreignBatch.Id), ("unassigned", unassigned.Id) })
            foreach (var action in new[] { "resources", "activity", "note", "upload" })
                await Check(other.Item1 + "-" + action, Request(action, other.Item2, other.Item1 + "-" + action), token,
                    action == "resources" ? HttpStatusCode.OK : action == "activity" ? HttpStatusCode.Forbidden : HttpStatusCode.BadRequest,
                    empty: action == "resources");
        foreach (var state in new[] { "suspended-academy", "inactive-teacher" })
        {
            await DomainState(state != "suspended-academy", state != "inactive-teacher");
            await Four(state, baseline, token);
            if (state == "suspended-academy") await Check("scoped-academy-suspension-control", new(HttpMethod.Get, $"/api/academies/{academy}/resources"), admin, HttpStatusCode.Forbidden);
            if (state == "inactive-teacher") await Check("teacher-me-inactivity-control", new(HttpMethod.Get, "/api/teacher/me"), token, HttpStatusCode.Forbidden);
            await DomainState(true, true);
        }
        if (!baseline)
        {
            async Task Link(Guid? academyId, Guid? teacherId, bool active = true)
            {
                using var scope = factory.Services.CreateScope(); var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                var user = (await users.FindByIdAsync(actor.ToString()))!; user.AcademyId = academyId; user.TeacherId = teacherId; user.IsActive = active;
                if (!(await users.UpdateAsync(user)).Succeeded) throw new InvalidOperationException("Lifecycle Identity fixture update failed");
            }
            foreach (var (label, linkedAcademy, linkedTeacher, active) in new[]
            {
                ("missing-teacher-link", (Guid?)academy, (Guid?)null, true),
                ("foreign-teacher-link", (Guid?)foreign, (Guid?)teacher, true),
                ("missing-domain-teacher", (Guid?)academy, (Guid?)Guid.NewGuid(), true),
                ("inactive-identity", (Guid?)academy, (Guid?)teacher, false)
            })
            {
                await Link(linkedAcademy, linkedTeacher, active); await Four(label, false, token); await Link(academy, teacher);
            }
            // Keep the original signed token and stamp: current role authority must win over stale claims.
            using (var scope = factory.Services.CreateScope())
            {
                var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(); var user = (await users.FindByIdAsync(actor.ToString()))!;
                if (!(await users.RemoveFromRoleAsync(user, "Teacher")).Succeeded) throw new InvalidOperationException("Lifecycle role removal failed");
            }
            await Four("removed-teacher-role-retained-link", false, token);
            using (var scope = factory.Services.CreateScope())
            {
                var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(); var user = (await users.FindByIdAsync(actor.ToString()))!;
                if (!(await users.AddToRoleAsync(user, "Teacher")).Succeeded) throw new InvalidOperationException("Lifecycle role restoration failed");
            }
            await Four("restored", true, token);
            foreach (var route in new[] { "me", "profile", "progress", "calendar" })
            {
                var path = "/api/teacher/" + route + (route == "calendar" ? "?year=2026&month=10" : "");
                await DomainState(false, true); await Check("suspended-extra-" + route, new(HttpMethod.Get, path), token, HttpStatusCode.Forbidden);
                await DomainState(true, false); await Check("inactive-extra-" + route, new(HttpMethod.Get, path), token, HttpStatusCode.Forbidden);
                await DomainState(true, true); await Check("active-extra-" + route, new(HttpMethod.Get, path), token, HttpStatusCode.OK);
            }
        }
        var evidence = Path.Combine(Directory.GetCurrentDirectory(), "QA", "EVIDENCE", "teacher-lifecycle", manifest.RunId.ToString("N"));
        Directory.CreateDirectory(evidence);
        await File.WriteAllTextAsync(Path.Combine(evidence, "results.json"), JsonSerializer.Serialize(new { run = manifest.RunId, baseline, cases = count, results }, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine($"TEACHERLIFE {(baseline ? "BASELINE CLASSIFIED: eight unauthorized operations accepted; NOT security acceptance" : "REGRESSION PASS")} cases={count}; unchanged real Identity, SQL, limiter and storage.");
    }
}
