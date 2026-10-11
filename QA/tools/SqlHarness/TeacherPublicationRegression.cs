using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

internal static partial class SqlHarnessEntryPoint
{
    private static async Task VerifyTeacherPublicationAsync(QaApiFactory factory, HttpClient client)
    {
        Guid academy, batch, foreignBatch;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            academy = await db.Academies.Where(x => x.Name == "Synthetic Academy A").Select(x => x.Id).SingleAsync();
            batch = await db.Batches.Where(x => x.AcademyId == academy).Select(x => x.Id).SingleAsync();
            // A foreign existing batch, not merely a nonexistent UUID.
            var foreign = await db.Academies.Where(x => x.Name == "Synthetic Academy B").Select(x => x.Id).SingleAsync();
            var course = new AcademyDesk.Api.Domain.Entities.ProgramCourse { AcademyId = foreign, Name = "Foreign publication sentinel" };
            var teacher = new AcademyDesk.Api.Domain.Entities.Teacher { AcademyId = foreign, FirstName = "Foreign", LastName = "Sentinel" };
            var other = new AcademyDesk.Api.Domain.Entities.Batch { AcademyId = foreign, CourseId = course.Id, TeacherId = teacher.Id, Name = "Foreign sentinel" };
            db.AddRange(course, teacher, other); await db.SaveChangesAsync(); foreignBatch = other.Id;
        }
        var token = await LoginAsync(client, "qa-teacher-a@example.invalid", "Synthetic!39Ab");
        var admin = await LoginAsync(client, "qa-admin-a@example.invalid", "Synthetic!39Ab");
        client.DefaultRequestHeaders.Authorization = null;
        var count = 0;
        async Task<string> Snapshot()
        {
            using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            return JsonSerializer.Serialize(new {
                Assignments = await db.Assignments.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(),
                Assessments = await db.Assessments.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(),
                Notifications = await db.Notifications.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(),
                Audit = await db.AuditLogs.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync()
            });
        }
        async Task Check(string kind, object published, object? date, HttpStatusCode expected, string? actor, Guid? selectedBatch = null)
        {
            var title = "Synthetic publication " + (++count);
            var body = new Dictionary<string, object?> { ["batchId"] = selectedBatch ?? batch, ["title"] = title, ["isPublished"] = published, ["type"] = kind == "assignments" ? "Homework" : "Performance" };
            if (kind == "assignments") body["description"] = "Synthetic instructions";
            else { body["maxScore"] = 12.5m; body["scheduledAtUtc"] = date; }
            var before = await Snapshot(); await Task.Delay(700);
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/teacher/" + kind) { Content = JsonContent.Create(body) };
            if (actor is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", actor);
            using var response = await client.SendAsync(request);
            RequireFinanceStatus(response, expected, "publication " + kind + " " + count);
            if (expected != HttpStatusCode.OK)
            {
                if (before != await Snapshot()) throw new InvalidOperationException("Rejected publication changed scoped rows.");
            }
            else
            {
                var json = await response.Content.ReadFromJsonAsync<JsonElement>(); var id = json.GetProperty("id").GetGuid();
                if (json.GetProperty("isPublished").GetBoolean() != (bool)published) throw new InvalidOperationException("Response boolean mismatch.");
                using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
                if (kind == "assignments")
                {
                    var row = await db.Assignments.AsNoTracking().SingleAsync(x => x.Id == id);
                    if (row.AcademyId != academy || row.BatchId != batch || row.Title != title || row.IsPublished != (bool)published || row.Description != "Synthetic instructions") throw new InvalidOperationException("Fresh SQL assignment mismatch.");
                    if (await db.Assignments.CountAsync(x => x.Title == title) != 1) throw new InvalidOperationException("Assignment count mismatch.");
                }
                else
                {
                    var row = await db.Assessments.AsNoTracking().SingleAsync(x => x.Id == id);
                    var expectedDate = date is null ? (DateTime?)null : DateTime.Parse((string)date, null, System.Globalization.DateTimeStyles.AdjustToUniversal);
                    if (row.AcademyId != academy || row.BatchId != batch || row.Title != title || row.IsPublished != (bool)published || row.MaxScore != 12.5m || row.ScheduledAtUtc != expectedDate) throw new InvalidOperationException("Fresh SQL assessment mismatch.");
                    if (await db.Assessments.CountAsync(x => x.Title == title) != 1) throw new InvalidOperationException("Assessment count mismatch.");
                }
                await Task.Delay(700); using var read = new HttpRequestMessage(HttpMethod.Get, "/api/teacher/" + kind);
                read.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                using var readResponse = await client.SendAsync(read); RequireFinanceStatus(readResponse, HttpStatusCode.OK, "publication readback");
                var rows = await readResponse.Content.ReadFromJsonAsync<JsonElement>();
                if (rows.EnumerateArray().Count(x => x.GetProperty("id").GetGuid() == id && x.GetProperty("isPublished").GetBoolean() == (bool)published) != 1) throw new InvalidOperationException("Fresh teacher list mismatch.");
            }
            Console.WriteLine("PASS: PUBLICATION " + kind + " case=" + count + " status=" + (int)expected + " scopedPersistenceOrNoWrite=true");
        }
        foreach (var kind in new[] { "assignments", "assessments" })
        {
            foreach (var published in new[] { true, false })
            {
                await Check(kind, published, null, HttpStatusCode.OK, token);
                if (kind == "assessments") await Check(kind, published, "2026-10-11T08:00:00Z", HttpStatusCode.OK, token);
                await Check(kind, published.ToString().ToLowerInvariant(), null, HttpStatusCode.BadRequest, token);
            }
            await Check(kind, true, null, HttpStatusCode.Unauthorized, null);
            await Check(kind, true, null, HttpStatusCode.Forbidden, admin);
            await Check(kind, true, null, HttpStatusCode.BadRequest, token, foreignBatch);
            await Check(kind, true, null, HttpStatusCode.BadRequest, token, Guid.NewGuid());
        }
        await Check("assessments", false, "", HttpStatusCode.BadRequest, token);
        await Check("assessments", false, "not-a-date", HttpStatusCode.BadRequest, token);
        Console.WriteLine($"PUBLICATION REGRESSION PASS: {count} real Identity/HTTP/SQL cases; strict JSON booleans, nullable dates, exact one-row create/readback and denied no-write controls; no browser/device/full-critical acceptance.");
    }
}
