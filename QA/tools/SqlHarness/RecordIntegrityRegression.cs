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
    private static async Task VerifyRecordIntegrityAsync(QaApiFactory factory, HttpClient client, QaRunManifest manifest)
    {
        Guid academy, foreign, ownHoliday, foreignHoliday, foreignTeacher, sessionId, ownBranch, foreignBranch;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var a = await db.Academies.SingleAsync(x => x.Name == "Synthetic Academy A"); academy = a.Id;
            foreign = await db.Academies.Where(x => x.Name == "Synthetic Academy B").Select(x => x.Id).SingleAsync();
            var modules = JsonSerializer.Deserialize<List<string>>(a.EnabledModulesJson ?? "[]") ?? [];
            foreach (var name in new[] { "Certificates", "Finance" }) if (!modules.Contains(name)) modules.Add(name);
            a.EnabledModulesJson = JsonSerializer.Serialize(modules);
            var holidays = new[] { new AcademyHoliday { AcademyId = academy, Name = "Owned synthetic holiday", HolidayDate = new(2026, 10, 28) }, new AcademyHoliday { AcademyId = foreign, Name = "Foreign synthetic holiday", HolidayDate = new(2026, 10, 28) } };
            ownHoliday = holidays[0].Id; foreignHoliday = holidays[1].Id; db.AddRange(holidays);
            var course = new ProgramCourse { AcademyId = academy, Name = "Synthetic integrity subject" };
            var batch = new Batch { AcademyId = academy, CourseId = course.Id, Name = "Synthetic integrity class" };
            var teacher = new Teacher { AcademyId = foreign, FirstName = "FOREIGN-INTELLIGENCE-PRIVATE", LastName = "Teacher" }; foreignTeacher = teacher.Id;
            var start = new DateTime(2026, 10, 28, 10, 0, 0, DateTimeKind.Utc);
            var session = new ClassSession { AcademyId = academy, BatchId = batch.Id, TeacherId = teacher.Id, StartUtc = start, EndUtc = start.AddHours(1) }; sessionId = session.Id;
            var branches = new[] { new Branch { AcademyId = academy, Name = "Own synthetic branch" }, new Branch { AcademyId = foreign, Name = "Foreign synthetic branch" } };
            ownBranch = branches[0].Id; foreignBranch = branches[1].Id;
            db.AddRange(course, batch, teacher, session); db.AddRange(branches); await db.SaveChangesAsync();
        }
        await CreateAccessActorAsync(factory, "qa-record-teacher@example.invalid", "Teacher", academy);
        var admin = await LoginAsync(client, "qa-admin-a@example.invalid", "Synthetic!39Ab");
        var other = await LoginAsync(client, "qa-admin-b@example.invalid", "Synthetic!39Ab");
        var teacherToken = await LoginAsync(client, "qa-record-teacher@example.invalid", "Synthetic!39Ab");
        client.DefaultRequestHeaders.Authorization = null;
        var root = $"/api/academies/{academy}";
        var count = 0;
        void Pass(string label) { count++; Console.WriteLine($"RECORDINTEGRITY CASE {label} PASS."); }
        async Task<string> State()
        {
            using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            return JsonSerializer.Serialize(new { Students = await db.Students.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(), Expenses = await db.Expenses.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(), Holidays = await db.AcademyHolidays.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(), Sessions = await db.ClassSessions.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(), Teachers = await db.Teachers.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(), Audits = await db.AuditLogs.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync() });
        }
        async Task<HttpResponseMessage> Send(string method, string path, object? body = null, string? token = null)
        {
            using var request = new HttpRequestMessage(new HttpMethod(method), path);
            if (token is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            if (body is not null) request.Content = JsonContent.Create(body);
            return await client.SendAsync(request);
        }
        async Task NoWrite(string label, string method, string path, object? body, HttpStatusCode status, string? token)
        {
            var before = await State(); using var result = await Send(method, path, body, token); RequireFinanceStatus(result, status, label);
            if (before != await State()) throw new InvalidOperationException("Rejected/read-only record operation changed domain/audit rows: " + label);
            Pass(label);
        }
        var beforeRead = await State();
        using (var result = await Send("GET", root + "/admin-intelligence", token: admin))
        {
            RequireFinanceStatus(result, HttpStatusCode.OK, "foreign teacher dashboard fallback");
            var body = await result.Content.ReadAsStringAsync(); using var json = JsonDocument.Parse(body);
            var row = json.RootElement.GetProperty("workload").EnumerateArray().Single(x => x.GetProperty("teacherId").GetGuid() == foreignTeacher);
            if (row.GetProperty("teacherName").GetString() != "Unavailable teacher" || body.Contains("FOREIGN-INTELLIGENCE-PRIVATE", StringComparison.Ordinal) || beforeRead != await State())
                throw new InvalidOperationException("Dashboard foreign-teacher fallback/read-only scope failed.");
        }
        Pass("foreign-teacher-dashboard-safe-read");
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); var own = new Teacher { AcademyId = academy, FirstName = "Own synthetic", LastName = "Teacher" };
            db.Add(own); (await db.ClassSessions.SingleAsync(x => x.Id == sessionId)).TeacherId = own.Id; await db.SaveChangesAsync();
        }
        beforeRead = await State();
        using (var result = await Send("GET", root + "/admin-intelligence", token: admin))
        {
            RequireFinanceStatus(result, HttpStatusCode.OK, "own teacher dashboard control"); using var json = JsonDocument.Parse(await result.Content.ReadAsStringAsync());
            if (!json.RootElement.GetProperty("workload").EnumerateArray().Any(x => x.GetProperty("teacherName").GetString() == "Own synthetic Teacher") || beforeRead != await State())
                throw new InvalidOperationException("Own teacher dashboard control/read-only failed.");
        }
        Pass("own-teacher-dashboard-control");
        var holidayPath = root + "/holidays/";
        await NoWrite("anonymous-holiday-delete401", "DELETE", holidayPath + ownHoliday, null, HttpStatusCode.Unauthorized, null);
        await NoWrite("teacher-holiday-delete403", "DELETE", holidayPath + ownHoliday, null, HttpStatusCode.Forbidden, teacherToken);
        await NoWrite("foreign-admin-holiday-delete403", "DELETE", holidayPath + ownHoliday, null, HttpStatusCode.Forbidden, other);
        await NoWrite("foreign-holiday-row404", "DELETE", holidayPath + foreignHoliday, null, HttpStatusCode.NotFound, admin);
        await NoWrite("missing-holiday404", "DELETE", holidayPath + Guid.NewGuid(), null, HttpStatusCode.NotFound, admin);
        using (var result = await Send("DELETE", holidayPath + ownHoliday, token: admin)) RequireFinanceStatus(result, HttpStatusCode.NoContent, "own holiday delete");
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            if (await db.AcademyHolidays.AnyAsync(x => x.Id == ownHoliday) || !await db.AcademyHolidays.AnyAsync(x => x.Id == foreignHoliday)) throw new InvalidOperationException("Holiday delete scope failed.");
        }
        Pass("own-holiday-delete204-exact-row");
        await NoWrite("repeated-holiday-delete404", "DELETE", holidayPath + ownHoliday, null, HttpStatusCode.NotFound, admin);
        Guid faultHoliday;
        using (var scope = factory.Services.CreateScope()) { var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); var row = new AcademyHoliday { AcademyId = academy, Name = "Synthetic audit fault", HolidayDate = new(2026, 10, 28) }; faultHoliday = row.Id; db.Add(row); await db.SaveChangesAsync(); }
        await SetAuditInsertDeniedAsync(manifest, true);
        try { await NoWrite("holiday-audit-fault-rolls-back-delete", "DELETE", holidayPath + faultHoliday, null, HttpStatusCode.InternalServerError, admin); }
        finally { await SetAuditInsertDeniedAsync(manifest, false); }
        object ImportRows(params string?[] emails) => new { rows = emails.Select(email => new { firstName = "Synthetic", lastName = "Import", email }).ToArray() };
        var duplicate = ImportRows("Duplicate@example.invalid", " DUPLICATE@example.invalid ");
        beforeRead = await State();
        using (var result = await Send("POST", root + "/imports/students/validate", duplicate, admin))
        {
            RequireFinanceStatus(result, HttpStatusCode.OK, "duplicate validation"); using var json = JsonDocument.Parse(await result.Content.ReadAsStringAsync());
            if (!json.RootElement.GetProperty("errors").EnumerateArray().Any(x => x.GetProperty("row").GetInt32() == 2)) throw new InvalidOperationException("Import duplicate row feedback missing.");
        }
        // Validation is a POST and retains its existing action audit; no student is created.
        Pass("duplicate-import-validation-row2");
        await NoWrite("duplicate-import400-all-or-nothing", "POST", root + "/imports/students", duplicate, HttpStatusCode.BadRequest, admin);
        await NoWrite("null-import-row400", "POST", root + "/imports/students", new { rows = new object?[] { null } }, HttpStatusCode.BadRequest, admin);
        using (var result = await Send("POST", root + "/imports/students", ImportRows("First-record@example.invalid", null), admin))
        {
            RequireFinanceStatus(result, HttpStatusCode.OK, "valid import"); using var json = JsonDocument.Parse(await result.Content.ReadAsStringAsync());
            if (json.RootElement.GetProperty("imported").GetInt32() != 2) throw new InvalidOperationException("Import count changed.");
        }
        using (var scope = factory.Services.CreateScope()) { var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); if (await db.Students.CountAsync(x => x.AcademyId == academy && x.LastName == "Import") != 2) throw new InvalidOperationException("Exact import SQL rows missing."); }
        Pass("valid-import-exact-two-rows");
        await NoWrite("existing-normalized-import409", "POST", root + "/imports/students", ImportRows(" FIRST-RECORD@example.invalid "), HttpStatusCode.Conflict, admin);
        await SetAuditInsertDeniedAsync(manifest, true);
        try { await NoWrite("import-audit-fault-rolls-back-all-rows", "POST", root + "/imports/students", ImportRows("audit-import@example.invalid"), HttpStatusCode.InternalServerError, admin); }
        finally { await SetAuditInsertDeniedAsync(manifest, false); }
        Guid expense;
        using (var result = await Send("POST", root + "/expenses", new { description = "Synthetic original", amount = 12m, currency = "USD", branchId = ownBranch }, admin))
        { RequireFinanceStatus(result, HttpStatusCode.Created, "expense seed"); using var json = JsonDocument.Parse(await result.Content.ReadAsStringAsync()); expense = json.RootElement.GetProperty("id").GetGuid(); }
        object ExpenseBody(Guid? branch, string? status = "Recorded") => new { description = "Changed synthetic", amount = 42m, category = "General", branchId = branch, expenseDate = "2026-10-28", status };
        await NoWrite("foreign-expense-branch400", "PUT", root + "/expenses/" + expense, ExpenseBody(foreignBranch), HttpStatusCode.BadRequest, admin);
        await NoWrite("missing-expense-branch400", "PUT", root + "/expenses/" + expense, ExpenseBody(Guid.NewGuid()), HttpStatusCode.BadRequest, admin);
        await NoWrite("fabricated-expense-Paid400", "PUT", root + "/expenses/" + expense, ExpenseBody(ownBranch, "Paid"), HttpStatusCode.BadRequest, admin);
        using (var result = await Send("PUT", root + "/expenses/" + expense, ExpenseBody(ownBranch, " recorded "), admin)) RequireFinanceStatus(result, HttpStatusCode.OK, "valid expense update");
        using (var scope = factory.Services.CreateScope()) { var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); var row = await db.Expenses.AsNoTracking().SingleAsync(x => x.Id == expense); if (row.BranchId != ownBranch || row.Amount != 42m || row.Currency != "USD" || row.Status != "Recorded") throw new InvalidOperationException("Expense persisted fields mismatch."); }
        Pass("valid-expense-update-keeps-currency-and-status");
        await NoWrite("foreign-admin-expense-update403", "PUT", root + "/expenses/" + expense, ExpenseBody(null), HttpStatusCode.Forbidden, other);
        await SetAuditInsertDeniedAsync(manifest, true);
        try { await NoWrite("expense-audit-fault-rolls-back-update", "PUT", root + "/expenses/" + expense, ExpenseBody(null), HttpStatusCode.InternalServerError, admin); }
        finally { await SetAuditInsertDeniedAsync(manifest, false); }
        if (count != 22) throw new InvalidOperationException("Record integrity case inventory changed: " + count);
        Console.WriteLine("RECORDINTEGRITY REGRESSION PASS:22 guarded SQL/HTTP cases for scoped holiday deletion, safe dashboard serialization, duplicate/null/accepted imports and expense tenant/status invariants, including audit rollback; broader browser/concurrency/release remain pending.");
    }
}
