using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Tests.Infrastructure;
using AcademyDesk.Api.Domain.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;

internal static partial class SqlHarnessEntryPoint
{
    private static async Task VerifyAssessmentGradeMigrationAsync(AcademyDeskDbContext db)
    {
        // This context uses only the ownership-validated generated QA database.
        await db.GetService<IMigrator>().MigrateAsync("20260930125012_AddPrivateClassMediaUploadSessions");
        var id = Guid.NewGuid(); var academy = Guid.NewGuid(); var assessment = Guid.NewGuid(); var student = Guid.NewGuid();
        await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO [AssessmentResults] ([Id],[AcademyId],[AssessmentId],[StudentId],[Score],[Grade],[Remarks],[IsPublished],[CreatedAtUtc]) VALUES ({id},{academy},{assessment},{student},{20m},{"Legacy distinction"},{"Preserve this note"},{true},SYSUTCDATETIME())");
        await db.Database.MigrateAsync();
        var row = await db.AssessmentResults.AsNoTracking().SingleAsync(x => x.Id == id);
        if (row.Grade != "Legacy distinction" || row.Score != 20 || row.Remarks != "Preserve this note" || !row.IsPublished || row.IsGradeManual is not null)
            throw new InvalidOperationException("Migration changed a legacy assessment result.");
        await db.AssessmentResults.Where(x => x.Id == id).ExecuteDeleteAsync();
        Console.WriteLine("GRADE MIGRATION PASS:81 domain migrations; pre-column legacy grade/score/remarks/publication preserved and provenance stays null.");
    }
    private static async Task VerifyAssessmentGradesAsync(QaApiFactory factory, HttpClient client)
    {
        Guid academy, foreign, batch, student; var count = 0;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var a = await db.Academies.SingleAsync(x => x.Name == "Synthetic Academy A"); academy = a.Id;
            foreign = await db.Academies.Where(x => x.Name == "Synthetic Academy B").Select(x => x.Id).SingleAsync();
            a.EnabledModulesJson = JsonSerializer.Serialize((JsonSerializer.Deserialize<string[]>(a.EnabledModulesJson) ?? []).Append("AcademicGovernance").Distinct());
            batch = await db.Batches.Where(x => x.AcademyId == academy).Select(x => x.Id).SingleAsync();
            student = await db.Students.Where(x => x.AcademyId == academy).Select(x => x.Id).SingleAsync();
            db.Add(new Enrollment { AcademyId = academy, BatchId = batch, StudentId = student }); await db.SaveChangesAsync();
        }
        var admin = await LoginAsync(client, "qa-admin-a@example.invalid", "Synthetic!39Ab");
        var teacher = await LoginAsync(client, "qa-teacher-a@example.invalid", "Synthetic!39Ab");
        var other = await LoginAsync(client, "qa-admin-b@example.invalid", "Synthetic!39Ab"); client.DefaultRequestHeaders.Authorization = null;
        async Task<Assessment> Seed(string mode = "scheme")
        {
            using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var scheme = new GradingScheme { AcademyId = mode == "foreign" ? foreign : academy, Name = "Grade fixture " + Guid.NewGuid().ToString("N"), PassingPercent = 50 };
            var a = new Assessment { AcademyId = academy, BatchId = batch, Title = "Grade fixture " + Guid.NewGuid().ToString("N"), MaxScore = mode == "zero-max" ? 0 : 100, GradingSchemeId = mode == "none" ? null : mode == "missing" ? Guid.NewGuid() : scheme.Id };
            db.AddRange(scheme, a); await db.SaveChangesAsync(); return a;
        }
        async Task<string> Snapshot()
        {
            using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            return JsonSerializer.Serialize(new { Results = await db.AssessmentResults.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), Notifications = await db.Notifications.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), Audits = await db.AuditLogs.AsNoTracking().OrderBy(x => x.Id).ToListAsync() });
        }
        string Route(Assessment a, bool t) => t ? $"/api/teacher/assessments/{a.Id}/results" : $"/api/academies/{academy}/assessments/{a.Id}/results";
        async Task Check(string label, Assessment a, bool t, decimal score, string? grade, bool? manual, HttpStatusCode expected, string? expectedGrade = null, bool? expectedManual = null, string? token = null)
        {
            await Task.Delay(550); var before = await Snapshot();
            using var request = new HttpRequestMessage(HttpMethod.Post, Route(a, t)); request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token ?? (t ? teacher : admin));
            // Null mode is deliberately omitted to exercise backwards-compatible JSON binding.
            request.Content = manual is null ? JsonContent.Create(new { studentId = student, score, grade, remarks = " Saved note ", isPublished = false }) : JsonContent.Create(new { studentId = student, score, grade, isGradeManual = manual, remarks = " Saved note ", isPublished = false });
            using var response = await client.SendAsync(request); RequireFinanceStatus(response, expected, label);
            if (expected != HttpStatusCode.OK) { if (before != await Snapshot()) throw new InvalidOperationException("Rejected grade write changed rows: " + label); }
            else
            {
                using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
                var rows = await db.AssessmentResults.AsNoTracking().Where(x => x.AssessmentId == a.Id && x.StudentId == student).ToListAsync();
                if (rows.Count != 1 || rows[0].Grade != expectedGrade || rows[0].IsGradeManual != expectedManual || rows[0].Score != score || rows[0].Remarks != "Saved note" || rows[0].IsPublished) throw new InvalidOperationException("SQL grade/mode/score projection failed: " + label);
                using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync()); var r = json.RootElement;
                if (r.GetProperty("grade").GetString() != expectedGrade || r.GetProperty("isGradeManual").GetBoolean() != expectedManual || r.GetProperty("score").GetDecimal() != score) throw new InvalidOperationException("Grade response projection failed.");
                using var list = new HttpRequestMessage(HttpMethod.Get, Route(a, t)); list.Headers.Authorization = new AuthenticationHeaderValue("Bearer", t ? teacher : admin);
                using var listed = await client.SendAsync(list); RequireFinanceStatus(listed, HttpStatusCode.OK, "grade-readback"); using var readback = JsonDocument.Parse(await listed.Content.ReadAsStringAsync()); var saved = readback.RootElement.EnumerateArray().Single(x => x.GetProperty("studentId").GetGuid() == student);
                if (saved.GetProperty("grade").GetString() != expectedGrade || saved.GetProperty("isGradeManual").GetBoolean() != expectedManual) throw new InvalidOperationException("Grade readback differs.");
            }
            count++; Console.WriteLine($"GRADE CASE {label} PASS.");
        }
        foreach (var t in new[] { false, true })
        {
            var a = await Seed(); var portal = t ? "teacher" : "admin";
            foreach (var (score, input, manual, grade, mode, label) in new (decimal, string?, bool?, string?, bool, string)[] {
                (80,null,false,"Pass",false,"auto-first"),(20,"Pass",false,"Fail",false,"auto-score-edit"),(50,"Fail",false,"Pass",false,"exact-threshold"),
                (0,"Merit",true,"Merit",true,"manual-zero"),(99,"Merit",true,"Merit",true,"manual-score-edit"),(0,"Merit",false,"Fail",false,"switch-to-auto"),
                (80,"  ",null,"Pass",false,"legacy-whitespace"),(20," Legacy override ",null,"Legacy override",true,"legacy-explicit"),
                (49.99m,"",null,"Fail",false,"legacy-empty"),(100,null,null,"Pass",false,"legacy-null") })
                await Check(portal+"-"+label,a,t,score,input,manual,HttpStatusCode.OK,grade,mode);
            await Check(portal+"-manual-empty400",a,t,50," ",true,HttpStatusCode.BadRequest);
            await Check(portal+"-over-range400",a,t,101,null,false,HttpStatusCode.BadRequest);
            await Check(portal+"-excess-precision400",a,t,49.999m,null,false,HttpStatusCode.BadRequest);
            await Check(portal+"-foreign-token403",a,t,50,null,false,HttpStatusCode.Forbidden,token:other);
            foreach (var broken in new[] { "missing", "foreign", "zero-max" }) await Check(portal+"-"+broken+"400",await Seed(broken),t,0,null,false,HttpStatusCode.BadRequest);
            await Check(portal+"-no-scheme-null",await Seed("none"),t,50,"Stale",false,HttpStatusCode.OK,null,false);
        }
        // Unknown legacy provenance remains null and preserves its grade on read.
        var legacy = await Seed(); using (var scope = factory.Services.CreateScope()) { var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); db.Add(new AssessmentResult { AcademyId = academy, AssessmentId = legacy.Id, StudentId = student, Score = 20, Grade = "Approved legacy override", IsGradeManual = null }); await db.SaveChangesAsync(); }
        using (var request = new HttpRequestMessage(HttpMethod.Get, Route(legacy, false))) { request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", admin); using var response = await client.SendAsync(request); RequireFinanceStatus(response, HttpStatusCode.OK, "legacy-preserved"); using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync()); var r = json.RootElement[0]; if (r.GetProperty("grade").GetString() != "Approved legacy override" || r.GetProperty("isGradeManual").ValueKind != JsonValueKind.Null) throw new InvalidOperationException("Legacy grade/provenance was overwritten."); }
        count++; Console.WriteLine("GRADE CASE legacy-grade-preserved PASS.");
        await CreateAccessActorAsync(factory,"qa-grade-student@example.invalid","Student",academy,student);
        var guardianId = Guid.NewGuid();
        var guardianUser = await CreateAccessActorAsync(factory,"qa-grade-guardian@example.invalid","Guardian",academy);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            db.Add(new Guardian { Id = guardianId, AcademyId = academy, FirstName = "Synthetic", LastName = "Guardian" });
            db.Add(new StudentGuardian { AcademyId = academy, StudentId = student, GuardianId = guardianId, CanAccessPortal = true });
            foreach (var r in await db.AssessmentResults.Where(x => x.AcademyId == academy && x.IsGradeManual != null).ToListAsync()) r.IsPublished = true;
            await db.SaveChangesAsync();
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(); var user = await users.FindByIdAsync(guardianUser.ToString()) ?? throw new InvalidOperationException("Guardian fixture missing"); user.GuardianId = guardianId;
            if (!(await users.UpdateAsync(user)).Succeeded) throw new InvalidOperationException("Guardian linking failed");
        }
        foreach (var portal in new[] { "student", "guardian" })
        {
            var token = await LoginAsync(client,$"qa-grade-{portal}@example.invalid","Synthetic!39Ab"); using var request = new HttpRequestMessage(HttpMethod.Get,$"/api/portal/students/{student}"); request.Headers.Authorization = new AuthenticationHeaderValue("Bearer",token);
            using var response = await client.SendAsync(request); RequireFinanceStatus(response,HttpStatusCode.OK,"portal-grade-read"); using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var results = json.RootElement.GetProperty("assessmentResults").EnumerateArray().ToArray();
            using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var expected = await db.AssessmentResults.AsNoTracking().Where(x => x.AcademyId == academy && x.StudentId == student && x.IsPublished).Join(db.Assessments, x => x.AssessmentId, a => a.Id, (r,a) => new { a.Title, r.Score, r.Grade }).ToListAsync();
            if (results.Length != expected.Count || results.Any(x => !expected.Any(y => y.Title == x.GetProperty("title").GetString() && y.Score == x.GetProperty("score").GetDecimal() && y.Grade == x.GetProperty("grade").GetString()))) throw new InvalidOperationException("Family portal grade projection differs from SQL.");
            count++; Console.WriteLine($"GRADE CASE {portal}-published-result-projection PASS.");
        }
        if (count != 39) throw new InvalidOperationException("Grade case count changed: " + count);
        Console.WriteLine("GRADE REGRESSION PASS:39 cases; real Admin/Teacher JSON/HTTP/SQL grade/mode parity, Student/Guardian projection, threshold/score changes, legacy/manual preservation, invalid-state no-write; browser/device/full critical pending.");
    }
}
