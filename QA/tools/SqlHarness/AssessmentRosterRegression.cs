using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Domain.Identity;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

internal static partial class SqlHarnessEntryPoint
{
    private static async Task VerifyAssessmentRosterAsync(QaApiFactory factory, HttpClient client)
    {
        Guid academy, foreign, batch; var count = 0;
        using (var scope = factory.Services.CreateScope()) {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var a = await db.Academies.SingleAsync(x => x.Name == "Synthetic Academy A"); academy = a.Id;
            foreign = await db.Academies.Where(x => x.Name == "Synthetic Academy B").Select(x => x.Id).SingleAsync();
            a.EnabledModulesJson = JsonSerializer.Serialize((JsonSerializer.Deserialize<string[]>(a.EnabledModulesJson) ?? []).Append("AcademicGovernance").Distinct());
            batch = await db.Batches.Where(x => x.AcademyId == academy).Select(x => x.Id).SingleAsync(); await db.SaveChangesAsync();
        }
        var admin = await LoginAsync(client, "qa-admin-a@example.invalid", "Synthetic!39Ab");
        var teacher = await LoginAsync(client, "qa-teacher-a@example.invalid", "Synthetic!39Ab");
        var other = await LoginAsync(client, "qa-admin-b@example.invalid", "Synthetic!39Ab"); client.DefaultRequestHeaders.Authorization = null;
        async Task<(Assessment Assessment, Student Student)> Seed(string scenario, bool existing = false) {
            using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var student = new Student { AcademyId = scenario == "foreign-student" ? foreign : academy, StudentNumber = Guid.NewGuid().ToString("N"), FirstName = "Synthetic", LastName = "Roster" };
            var assessment = new Assessment { AcademyId = scenario == "foreign-assessment" ? foreign : academy, BatchId = batch, Title = "Roster fixture " + Guid.NewGuid().ToString("N"), MaxScore = 100, IsPublished = true };
            db.AddRange(student, assessment);
            if (scenario != "never") {
                var enrollmentBatch = batch;
                if (scenario == "wrong-batch") {
                    var wrong = new Batch { AcademyId = academy, Name = "Other roster batch " + Guid.NewGuid().ToString("N"), CourseId = Guid.NewGuid() }; db.Add(wrong); enrollmentBatch = wrong.Id;
                }
                db.Add(new Enrollment { AcademyId = scenario is "foreign-enrollment" or "foreign-student" ? foreign : academy, BatchId = enrollmentBatch,
                    StudentId = scenario == "wrong-student" ? Guid.NewGuid() : student.Id });
            }
            if (existing) db.Add(new AssessmentResult { AcademyId = academy, AssessmentId = assessment.Id, StudentId = student.Id, Score = 25, Grade = "Original", Remarks = "Preserve", IsPublished = true });
            await db.SaveChangesAsync(); return (assessment, student);
        }
        async Task<string> Snapshot() {
            using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            return JsonSerializer.Serialize(new { Results = await db.AssessmentResults.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), Enrollments = await db.Enrollments.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), Notifications = await db.Notifications.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), Audits = await db.AuditLogs.AsNoTracking().OrderBy(x => x.Id).ToListAsync() });
        }
        string Route(Guid id, bool t) => t ? $"/api/teacher/assessments/{id}/results" : $"/api/academies/{academy}/assessments/{id}/results";
        async Task Check(string label, Assessment a, Student s, bool t, HttpStatusCode expected, decimal score = 80, string? token = null, Guid? routeId = null) {
            await Task.Delay(550); var before = await Snapshot();
            using var request = new HttpRequestMessage(HttpMethod.Post, Route(routeId ?? a.Id, t)); request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token ?? (t ? teacher : admin));
            request.Content = JsonContent.Create(new { studentId = s.Id, score, grade = (string?)null, isGradeManual = false, remarks = "Confirmed roster", isPublished = true });
            using var response = await client.SendAsync(request); RequireFinanceStatus(response, expected, label);
            if (expected != HttpStatusCode.OK) {
                if (before != await Snapshot()) throw new InvalidOperationException("Rejected roster request mutated captured rows: " + label);
                if (expected == HttpStatusCode.BadRequest && !(await response.Content.ReadAsStringAsync()).Contains("message")) throw new InvalidOperationException("Missing roster guidance");
            } else {
                using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
                var saved = await db.AssessmentResults.AsNoTracking().SingleAsync(x => x.AcademyId == academy && x.AssessmentId == a.Id && x.StudentId == s.Id);
                if (saved.Score != score || saved.Grade is not null || saved.IsGradeManual != false || saved.Remarks != "Confirmed roster" || !saved.IsPublished) throw new InvalidOperationException("Saved roster projection differs");
                using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                if (body.RootElement.GetProperty("studentId").GetGuid() != s.Id || body.RootElement.GetProperty("score").GetDecimal() != saved.Score) throw new InvalidOperationException("Roster response differs from SQL");
                using var read = new HttpRequestMessage(HttpMethod.Get, Route(a.Id, t)); read.Headers.Authorization = new AuthenticationHeaderValue("Bearer", t ? teacher : admin);
                using var listed = await client.SendAsync(read); RequireFinanceStatus(listed, HttpStatusCode.OK, "roster-readback"); using var json = JsonDocument.Parse(await listed.Content.ReadAsStringAsync());
                var row = json.RootElement.EnumerateArray().Single(x => x.GetProperty("studentId").GetGuid() == s.Id);
                if (row.GetProperty("score").GetDecimal() != saved.Score) throw new InvalidOperationException("Fresh API roster readback differs");
            }
            count++; Console.WriteLine($"ROSTER CASE {label} PASS.");
        }
        (Assessment Assessment, Student Student)? visibleResult = null;
        foreach (var t in new[] { false, true }) {
            var portal = t ? "teacher" : "admin";
            foreach (var scenario in new[] { "never", "wrong-batch", "foreign-enrollment", "wrong-student" }) foreach (var existing in new[] { false, true }) {
                var f = await Seed(scenario, existing); await Check($"{portal}-{scenario}-{(existing ? "overwrite" : "create")}", f.Assessment, f.Student, t, HttpStatusCode.BadRequest);
            }
            var foreignStudent = await Seed("foreign-student"); await Check(portal + "-foreign-student", foreignStudent.Assessment, foreignStudent.Student, t, HttpStatusCode.BadRequest);
            var active = await Seed("active");
            await Check(portal + "-active-create", active.Assessment, active.Student, t, HttpStatusCode.OK, 0);
            await Check(portal + "-active-update", active.Assessment, active.Student, t, HttpStatusCode.OK);
            if (!t) visibleResult = active;
            await Check(portal + "-foreign-token", active.Assessment, active.Student, t, HttpStatusCode.Forbidden, token: other);
            await Check(portal + "-missing-assessment", active.Assessment, active.Student, t, t ? HttpStatusCode.Forbidden : HttpStatusCode.NotFound, routeId: Guid.NewGuid());
            var foreignAssessment = await Seed("foreign-assessment"); await Check(portal + "-foreign-assessment", foreignAssessment.Assessment, foreignAssessment.Student, t, t ? HttpStatusCode.Forbidden : HttpStatusCode.NotFound);
        }
        var visible = visibleResult ?? throw new InvalidOperationException("Missing API-saved active result");
        await CreateAccessActorAsync(factory, "qa-roster-student@example.invalid", "Student", academy, visible.Student.Id);
        var guardianUser = await CreateAccessActorAsync(factory, "qa-roster-guardian@example.invalid", "Guardian", academy); var guardianId = Guid.NewGuid();
        using (var scope = factory.Services.CreateScope()) {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); db.Add(new Guardian { Id = guardianId, AcademyId = academy, FirstName = "Synthetic", LastName = "Guardian" });
            db.Add(new StudentGuardian { AcademyId = academy, StudentId = visible.Student.Id, GuardianId = guardianId, CanAccessPortal = true }); await db.SaveChangesAsync();
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(); var user = await users.FindByIdAsync(guardianUser.ToString()) ?? throw new InvalidOperationException("Missing guardian"); user.GuardianId = guardianId;
            if (!(await users.UpdateAsync(user)).Succeeded) throw new InvalidOperationException("Guardian link failed");
        }
        foreach (var portal in new[] { "student", "guardian" }) {
            var token = await LoginAsync(client, $"qa-roster-{portal}@example.invalid", "Synthetic!39Ab");
            using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/portal/students/{visible.Student.Id}"); request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            using var response = await client.SendAsync(request); RequireFinanceStatus(response, HttpStatusCode.OK, "roster-portal"); using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var results = json.RootElement.GetProperty("assessmentResults").EnumerateArray().ToArray();
            if (results.Length != 1 || results[0].GetProperty("title").GetString() != visible.Assessment.Title || results[0].GetProperty("score").GetDecimal() != 80) throw new InvalidOperationException("Family roster projection differs");
            count++; Console.WriteLine($"ROSTER CASE {portal}-active-result-visible PASS.");
        }
        if (count != 30) throw new InvalidOperationException("Roster case count changed: " + count);
        Console.WriteLine("ROSTER REGRESSION PASS:30 cases; real Admin/Teacher HTTP/SQL exact membership, rejected create/overwrite no-write, active readback, family active visibility; historical-status policy remains pending.");
    }
}
