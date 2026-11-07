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
    private static async Task VerifyAcademicAcceptanceAsync(QaApiFactory factory, HttpClient client)
    {
        Guid academy, foreign, batch, teacher, course, student;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var owner = await db.Academies.SingleAsync(x => x.Name == "Synthetic Academy A");
            academy = owner.Id;
            foreign = await db.Academies.Where(x => x.Name == "Synthetic Academy B").Select(x => x.Id).SingleAsync();
            owner.EnabledModulesJson = """["Core","Certificates","AcademicGovernance","Finance","FinanceControls","TeacherClassroom"]""";
            var assignment = await db.Batches.SingleAsync(x => x.AcademyId == academy);
            batch = assignment.Id;
            teacher = assignment.TeacherId ?? throw new InvalidOperationException("Synthetic batch teacher missing.");
            course = assignment.CourseId;
            student = await db.Students.Where(x => x.AcademyId == academy).Select(x => x.Id).SingleAsync();
            await db.SaveChangesAsync();
        }
        var admin = await LoginAsync(client, "qa-admin-a@example.invalid", "Synthetic!39Ab");
        var instructor = await LoginAsync(client, "qa-teacher-a@example.invalid", "Synthetic!39Ab");
        client.DefaultRequestHeaders.Authorization = null;
        var own = $"/api/academies/{academy}";
        var outside = $"/api/academies/{foreign}";
        var fees = $"/students/{student}/fee-arrangements";
        var pass = 0;
        var failures = 0;
        void Observe(string label, bool accepted)
        {
            if (accepted) pass++; else failures++;
            Console.WriteLine($"ACADEMICACCEPT CASE {label} {(accepted ? "PASS" : "FAIL")}");
        }
        async Task<HttpResponseMessage> Send(HttpMethod method, string path, string? token, object? body = null)
        {
            using var request = new HttpRequestMessage(method, path);
            if (token is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            if (body is not null) request.Content = JsonContent.Create(body);
            return await client.SendAsync(request);
        }
        async Task<JsonElement> Ok(HttpMethod method, string path, object? body = null, HttpStatusCode status = HttpStatusCode.OK)
        {
            using var response = await Send(method, own + path, admin, body);
            RequireFinanceStatus(response, status, path);
            return await response.Content.ReadFromJsonAsync<JsonElement>();
        }
        async Task<string> Snapshot()
        {
            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            return JsonSerializer.Serialize(new
            {
                sessions = await db.ClassSessions.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(),
                years = await db.AcademicYears.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(),
                terms = await db.AcademicTerms.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(),
                holidays = await db.AcademyHolidays.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(),
                modules = await db.CourseModules.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(),
                fees = await db.StudentFeeArrangements.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(),
                audits = await db.AuditLogs.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync()
            });
        }
        async Task Refused(string label, HttpMethod method, string path, string? token, HttpStatusCode expected, object? body = null)
        {
            var before = await Snapshot();
            using var response = await Send(method, path, token, body);
            var unchanged = before == await Snapshot();
            Observe($"{label} http={(int)response.StatusCode} noWrite={unchanged}", response.StatusCode == expected && unchanged);
        }

        var startUtc = new DateTime(2026, 12, 31, 18, 30, 0, DateTimeKind.Utc);
        var endUtc = startUtc.AddHours(1);
        const string link = "https://lesson.example.invalid/c10";
        var active = await Ok(HttpMethod.Post, "/sessions", new { batchId = batch, teacherId = teacher, startUtc, endUtc, deliveryMode = "Online", roomName = link }, HttpStatusCode.Created);
        var session = active.GetProperty("id").GetGuid();
        var activeRows = await Ok(HttpMethod.Get, "/sessions");
        var scheduled = activeRows.EnumerateArray().Single(x => x.GetProperty("id").GetGuid() == session);
        Observe("active-session-month-boundary", scheduled.GetProperty("status").GetString() == "Scheduled" &&
            scheduled.GetProperty("startUtc").GetDateTime() == startUtc && scheduled.GetProperty("roomName").GetString() == link);
        await Ok(HttpMethod.Put, $"/sessions/{session}", new { startUtc, endUtc, deliveryMode = "Online", roomName = link, status = "Cancelled" });
        var cancelledRows = await Ok(HttpMethod.Get, "/sessions");
        var cancelled = cancelledRows.EnumerateArray().Single(x => x.GetProperty("id").GetGuid() == session);
        using (var scope = factory.Services.CreateScope())
        {
            var row = await scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>().ClassSessions.AsNoTracking().SingleAsync(x => x.Id == session);
            Observe("cancelled-session-projection", row.Status == "Cancelled" && row.StartUtc == startUtc && row.TeacherId == teacher &&
                row.RoomName == link && cancelled.GetProperty("status").GetString() == row.Status &&
                cancelled.GetProperty("teacherId").GetGuid() == teacher && cancelled.GetProperty("roomName").GetString() == link);
        }
        await Refused("foreign-session-read", HttpMethod.Get, outside + "/sessions", admin, HttpStatusCode.Forbidden);

        var year = await Ok(HttpMethod.Post, "/academic-periods/years", new { name = "C10 year", startDate = "2026-12-31", endDate = "2027-12-31", isCurrent = true });
        var yearId = year.GetProperty("id").GetGuid();
        var term = await Ok(HttpMethod.Post, "/academic-periods/terms", new { academicYearId = yearId, name = "C10 term", startDate = "2027-01-01", endDate = "2027-02-01" });
        var holiday = await Ok(HttpMethod.Post, "/holidays", new { name = "C10 holiday", holidayDate = "2027-01-01" });
        var periods = await Ok(HttpMethod.Get, "/academic-periods");
        var holidays = await Ok(HttpMethod.Get, "/holidays");
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var storedYear = await db.AcademicYears.AsNoTracking().SingleAsync(x => x.Id == yearId);
            var termId = term.GetProperty("id").GetGuid();
            var storedTerm = await db.AcademicTerms.AsNoTracking().SingleAsync(x => x.Id == termId);
            var holidayId = holiday.GetProperty("id").GetGuid();
            var storedHoliday = await db.AcademyHolidays.AsNoTracking().SingleAsync(x => x.Id == holidayId);
            Observe("year-and-term-civil-dates", storedYear.StartDate == new DateOnly(2026, 12, 31) && storedTerm.StartDate == new DateOnly(2027, 1, 1) &&
                storedTerm.AcademicYearId == yearId && periods.GetProperty("years").EnumerateArray().Single(x => x.GetProperty("id").GetGuid() == yearId).GetProperty("startDate").GetString() == "2026-12-31" &&
                periods.GetProperty("terms").EnumerateArray().Single(x => x.GetProperty("id").GetGuid() == termId).GetProperty("startDate").GetString() == "2027-01-01");
            Observe("holiday-civil-date", storedHoliday.AcademyId == academy && storedHoliday.HolidayDate == new DateOnly(2027, 1, 1) &&
                holidays.EnumerateArray().Single(x => x.GetProperty("id").GetGuid() == holidayId).GetProperty("holidayDate").GetString() == "2027-01-01");
        }
        await Refused("foreign-holiday-create", HttpMethod.Post, outside + "/holidays", admin, HttpStatusCode.Forbidden, new { name = "Outside", holidayDate = "2027-01-01" });
        await Refused("teacher-year-create", HttpMethod.Post, own + "/academic-periods/years", instructor, HttpStatusCode.Forbidden, new { name = "Teacher", startDate = "2027-01-01", endDate = "2027-12-31" });

        await Refused("zero-module-sequence", HttpMethod.Post, own + "/course-modules", admin, HttpStatusCode.BadRequest, new { courseId = course, title = "Zero", sequence = 0 });
        await Refused("negative-module-sequence", HttpMethod.Post, own + "/course-modules", admin, HttpStatusCode.BadRequest, new { courseId = course, title = "Negative", sequence = -1 });
        await Refused("missing-module-sequence", HttpMethod.Post, own + "/course-modules", admin, HttpStatusCode.BadRequest, new { courseId = course, title = "Missing" });
        await Refused("fractional-module-sequence", HttpMethod.Post, own + "/course-modules", admin, HttpStatusCode.BadRequest, new { courseId = course, title = "Fraction", sequence = 1.5m });
        await Refused("teacher-module-create", HttpMethod.Post, own + "/course-modules", instructor, HttpStatusCode.Forbidden, new { courseId = course, title = "Teacher", sequence = 3 });
        using (var response = await Send(HttpMethod.Post, own + "/course-modules", admin, new { courseId = course, title = "C10 module", sequence = 2 }))
            RequireFinanceStatus(response, HttpStatusCode.OK, "create ordered module");
        var modules = await Ok(HttpMethod.Get, $"/course-modules?courseId={course}");
        var created = modules.EnumerateArray().Single(x => x.GetProperty("title").GetString() == "C10 module");
        var module = created.GetProperty("id").GetGuid();
        await Ok(HttpMethod.Patch, $"/course-modules/{module}/publication", new { isPublished = true });
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var stored = await db.CourseModules.AsNoTracking().SingleAsync(x => x.Id == module);
            Observe("module-sequence-and-publication", !created.GetProperty("isPublished").GetBoolean() && created.GetProperty("sequence").GetInt32() == 2 &&
                stored.AcademyId == academy && stored.CourseId == course && stored.Sequence == 2 && stored.IsPublished);
        }

        foreach (var (subject, amount) in new[] { ("C10 decimal", 1250.50m), ("C10 one paisa", 0.01m) })
        {
            var createdFee = await Ok(HttpMethod.Post, fees, new { subjectName = subject, amount, frequency = "Monthly", courseId = course, effectiveFrom = "2027-01-01" });
            var id = createdFee.GetProperty("id").GetGuid();
            var listed = (await Ok(HttpMethod.Get, fees)).EnumerateArray().Single(x => x.GetProperty("id").GetGuid() == id);
            using var scope = factory.Services.CreateScope();
            var stored = await scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>().StudentFeeArrangements.AsNoTracking().SingleAsync(x => x.Id == id);
            Observe($"subject-fee-{amount}", stored.Amount == amount && stored.StudentId == student && stored.CourseId == course && stored.AcademyId == academy &&
                listed.GetProperty("amount").GetDecimal() == amount && listed.GetProperty("effectiveFrom").GetString() == "2027-01-01");
        }
        var fee = new { subjectName = "Blocked", amount = 0.01m, frequency = "Monthly" };
        await Refused("teacher-fee-create", HttpMethod.Post, own + fees, instructor, HttpStatusCode.Forbidden, fee);
        await Refused("foreign-fee-create", HttpMethod.Post, outside + fees, admin, HttpStatusCode.Forbidden, fee);
        await Refused("anonymous-fee-create", HttpMethod.Post, own + fees, null, HttpStatusCode.Unauthorized, fee);
        await Refused("zero-fee-create", HttpMethod.Post, own + fees, admin, HttpStatusCode.BadRequest, new { subjectName = "Zero", amount = 0, frequency = "Monthly" });
        Console.WriteLine($"ACADEMICACCEPT SUMMARY pass={pass} fail={failures}");
        if (failures != 0) throw new InvalidOperationException("Academic acceptance failed.");
    }
}
