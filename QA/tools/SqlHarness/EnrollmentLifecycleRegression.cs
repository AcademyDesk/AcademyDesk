using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

internal static partial class SqlHarnessEntryPoint
{
    private static async Task VerifyEnrollmentLifecycleAsync(QaApiFactory factory, HttpClient client)
    {
        Guid academy, foreign, enrollmentId, foreignId; var count = 0;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            academy = await db.Academies.Where(x => x.Name == "Synthetic Academy A").Select(x => x.Id).SingleAsync();
            foreign = await db.Academies.Where(x => x.Name == "Synthetic Academy B").Select(x => x.Id).SingleAsync();
            var student = await db.Students.FirstAsync(x => x.AcademyId == academy);
            var otherStudent = await db.Students.FirstAsync(x => x.AcademyId == foreign);
            var course = new ProgramCourse { AcademyId = academy, Name = "Synthetic lifecycle course" };
            var otherCourse = new ProgramCourse { AcademyId = foreign, Name = "Synthetic foreign lifecycle course" };
            var batch = new Batch { AcademyId = academy, CourseId = course.Id, Name = "Synthetic lifecycle batch" };
            var otherBatch = new Batch { AcademyId = foreign, CourseId = otherCourse.Id, Name = "Synthetic foreign lifecycle batch" };
            db.AddRange(course, otherCourse, batch, otherBatch);
            var row = new Enrollment { AcademyId = academy, StudentId = student.Id, BatchId = batch.Id, StartDate = new(2026, 10, 1), Status = "Active" };
            var other = new Enrollment { AcademyId = foreign, StudentId = otherStudent.Id, BatchId = otherBatch.Id, Status = "Active" };
            db.AddRange(row, other); await db.SaveChangesAsync(); enrollmentId = row.Id; foreignId = other.Id;
        }
        var admin = await LoginAsync(client, "qa-admin-a@example.invalid", "Synthetic!39Ab");
        var otherAdmin = await LoginAsync(client, "qa-admin-b@example.invalid", "Synthetic!39Ab");
        var teacher = await LoginAsync(client, "qa-teacher-a@example.invalid", "Synthetic!39Ab");
        client.DefaultRequestHeaders.Authorization = null;
        async Task<Enrollment[]> Rows()
        {
            using var scope = factory.Services.CreateScope();
            return await scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>().Enrollments.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync();
        }
        async Task Check(string label, string status, string? reason, HttpStatusCode expected,
            string? token = null, bool anonymous = false, Guid? tenant = null, Guid? id = null, bool omitReason = false)
        {
            await Task.Delay(650); var before = await Rows();
            using var request = new HttpRequestMessage(HttpMethod.Put, $"/api/academies/{tenant ?? academy}/enrollments/{id ?? enrollmentId}");
            if (!anonymous) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token ?? admin);
            var body = new Dictionary<string, object?> { ["status"] = status, ["endDate"] = status == "Active" ? null : "2026-10-20" };
            if (!omitReason) body["lifecycleReason"] = reason;
            request.Content = JsonContent.Create(body);
            using var response = await client.SendAsync(request); RequireFinanceStatus(response, expected, label);
            var after = await Rows();
            if (expected != HttpStatusCode.OK)
            {
                if (JsonSerializer.Serialize(before) != JsonSerializer.Serialize(after)) throw new InvalidOperationException("Denied lifecycle write altered rows: " + label);
                if (expected == HttpStatusCode.BadRequest && status != "Unknown")
                {
                    using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                    if (payload.RootElement.GetProperty("message").GetString() != "A lifecycle reason is required when changing an enrolment from Active.") throw new InvalidOperationException("Reason validation message changed.");
                }
            }
            else
            {
                var changed = after.Single(x => x.Id == enrollmentId); var original = before.Single(x => x.Id == enrollmentId);
                if (changed.Status != status || changed.LifecycleReason != (string.IsNullOrWhiteSpace(reason) ? null : reason.Trim()) || changed.EndDate != (status == "Active" ? null : new DateOnly(2026, 10, 20))) throw new InvalidOperationException("Lifecycle projection differs: " + label);
                changed.Status = original.Status; changed.LifecycleReason = original.LifecycleReason; changed.EndDate = original.EndDate; changed.UpdatedAtUtc = original.UpdatedAtUtc;
                if (JsonSerializer.Serialize(before) != JsonSerializer.Serialize(after)) throw new InvalidOperationException("Unrelated lifecycle fields/rows changed: " + label);
                using var read = new HttpRequestMessage(HttpMethod.Get, $"/api/academies/{academy}/enrollments"); read.Headers.Authorization = new AuthenticationHeaderValue("Bearer", admin);
                using var readback = await client.SendAsync(read); RequireFinanceStatus(readback, HttpStatusCode.OK, label + " readback");
                using var result = JsonDocument.Parse(await readback.Content.ReadAsStringAsync());
                if (result.RootElement.EnumerateArray().Single(x => x.GetProperty("id").GetGuid() == enrollmentId).GetProperty("status").GetString() != status) throw new InvalidOperationException("Lifecycle list readback differs.");
            }
            count++; Console.WriteLine($"PASS: ENROLLMENTLIFE {label}");
        }
        foreach (var status in new[] { "Paused", "Completed", "Waitlisted", "Withdrawn", "Cancelled" })
        {
            await Check(status + " omitted", status, null, HttpStatusCode.BadRequest, omitReason: true);
            await Check(status + " null", status, null, HttpStatusCode.BadRequest);
            await Check(status + " whitespace", status, "   ", HttpStatusCode.BadRequest);
            await Check(status + " explained", status, "  Synthetic lifecycle explanation  ", HttpStatusCode.OK);
        }
        await Check("Active without reason", "Active", null, HttpStatusCode.OK);
        await Check("invalid status", "Unknown", "Explanation", HttpStatusCode.BadRequest);
        await Check("anonymous", "Paused", "Explanation", HttpStatusCode.Unauthorized, anonymous: true);
        await Check("teacher", "Paused", "Explanation", HttpStatusCode.Forbidden, token: teacher);
        await Check("foreign actor", "Paused", "Explanation", HttpStatusCode.Forbidden, token: otherAdmin);
        await Check("foreign route", "Paused", "Explanation", HttpStatusCode.Forbidden, tenant: foreign);
        await Check("foreign record ID", "Paused", "Explanation", HttpStatusCode.NotFound, id: foreignId);
        await Check("missing record", "Paused", "Explanation", HttpStatusCode.NotFound, id: Guid.NewGuid());
        if (count != 28) throw new InvalidOperationException("Incomplete lifecycle cases.");
        Console.WriteLine($"PASS: ENROLLMENTLIFE all {count} native SQL/HTTP cases, fresh persistence and protected no-write controls.");
    }
}
