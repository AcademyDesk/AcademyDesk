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
    private static async Task VerifyLeaveIdentityAsync(QaApiFactory factory, HttpClient client)
    {
        Guid academy, foreign, student, teacher, inactiveStudent, inactiveTeacher, foreignStudent, foreignTeacher, actor;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            academy = await db.Academies.Where(x => x.Name == "Synthetic Academy A").Select(x => x.Id).SingleAsync();
            foreign = await db.Academies.Where(x => x.Name == "Synthetic Academy B").Select(x => x.Id).SingleAsync();
            student = await db.Students.Where(x => x.AcademyId == academy).Select(x => x.Id).SingleAsync();
            teacher = await db.Teachers.Where(x => x.AcademyId == academy).Select(x => x.Id).SingleAsync();
            foreignStudent = await db.Students.Where(x => x.AcademyId == foreign).Select(x => x.Id).SingleAsync();
            var otherTeacher = new Teacher { AcademyId = foreign, FirstName = "Foreign", LastName = "Leave" };
            var oldStudent = new Student { AcademyId = academy, FirstName = "Inactive", LastName = "Leave", IsActive = false };
            var oldTeacher = new Teacher { AcademyId = academy, FirstName = "Inactive", LastName = "Leave", IsActive = false };
            foreignTeacher = otherTeacher.Id; inactiveStudent = oldStudent.Id; inactiveTeacher = oldTeacher.Id;
            db.AddRange(otherTeacher, oldStudent, oldTeacher);
            db.LeaveRequests.Add(new LeaveRequest { AcademyId = foreign, RequesterType = "Teacher", TeacherId = foreignTeacher,
                StartDate = new DateOnly(2026, 10, 2), EndDate = new DateOnly(2026, 10, 3), Reason = "Preserve foreign approved leave", Status = "Approved", DecisionNotes = "Preserve notes" });
            await db.SaveChangesAsync();
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            actor = (await users.FindByEmailAsync("qa-admin-a@example.invalid"))!.Id;
        }
        var token = await LoginAsync(client, "qa-admin-a@example.invalid", "Synthetic!39Ab");
        var foreignToken = await LoginAsync(client, "qa-admin-b@example.invalid", "Synthetic!39Ab");
        var teacherToken = await LoginAsync(client, "qa-teacher-a@example.invalid", "Synthetic!39Ab");
        await CreateAccessActorAsync(factory, "qa-leave-operations@example.invalid", "Operations", academy, permissions: "[\"scheduling.manage\"]");
        var operationsToken = await LoginAsync(client, "qa-leave-operations@example.invalid", "Synthetic!39Ab");
        client.DefaultRequestHeaders.Authorization = null;
        var count = 0;
        var start = new DateOnly(2026, 10, 20);
        object Payload(string? type, Guid? studentId, Guid? teacherId, string? reason = " Synthetic leave ", DateOnly? end = null) =>
            new { requesterType = type, studentId, teacherId, startDate = start, endDate = end ?? start, reason };
        async Task<string> Snapshot()
        {
            using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            return JsonSerializer.Serialize(new { leaves = await db.LeaveRequests.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), audits = await db.AuditLogs.AsNoTracking().OrderBy(x => x.Id).ToListAsync() });
        }
        async Task<HttpResponseMessage> Send(HttpMethod method, Guid target, object? payload, string? auth)
        {
            // Stay below the real app's request-rate limit; do not bypass middleware.
            await Task.Delay(550);
            using var request = new HttpRequestMessage(method, $"/api/academies/{target}/leave-requests");
            if (auth is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth);
            if (payload is not null) request.Content = JsonContent.Create(payload);
            return await client.SendAsync(request);
        }
        void Pass(string label) { count++; Console.WriteLine($"LEAVEIDENTITY CASE {label} PASS."); }
        async Task Denied(string label, object? payload, Guid? target = null, string? auth = null, HttpStatusCode expected = HttpStatusCode.BadRequest, HttpMethod? method = null)
        {
            var before = await Snapshot();
            using var response = await Send(method ?? HttpMethod.Post, target ?? academy, payload, auth);
            RequireFinanceStatus(response, expected, label);
            if (before != await Snapshot()) throw new InvalidOperationException("Rejected request changed leave/audit rows: " + label);
            Pass(label);
        }
        foreach (var type in new[] { "Student", "student", "STUDENT", " \t Student \n", "Teacher", "teacher", "TEACHER", " \t Teacher \n" })
        foreach (var active in new[] { true, false })
        {
            var canonical = type.Trim().Equals("Student", StringComparison.OrdinalIgnoreCase) ? "Student" : "Teacher";
            var label = $"valid-{canonical}-{count}-{(active ? "active" : "inactive")}";
            Guid? sid = canonical == "Student" ? active ? student : inactiveStudent : null;
            Guid? tid = canonical == "Teacher" ? active ? teacher : inactiveTeacher : null;
            using var before = JsonDocument.Parse(await Snapshot());
            using var response = await Send(HttpMethod.Post, academy, Payload(type, sid, tid, " " + label + " "), token);
            RequireFinanceStatus(response, HttpStatusCode.OK, label);
            if ((await response.Content.ReadAsStringAsync()).Length != 0) throw new InvalidOperationException("Existing empty success body changed");
            using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var leaves = await db.LeaveRequests.AsNoTracking().OrderBy(x => x.Id).ToListAsync();
            var row = leaves.Single(x => x.Reason == label);
            var oldLeaves = before.RootElement.GetProperty("leaves");
            if (leaves.Count != oldLeaves.GetArrayLength() + 1 || JsonSerializer.Serialize(leaves.Where(x => x.Id != row.Id)) != oldLeaves.GetRawText() ||
                row.AcademyId != academy || row.RequesterType != canonical || row.StudentId != sid || row.TeacherId != tid || row.StartDate != start || row.EndDate != start || row.Status != "Requested" || row.DecisionNotes != null)
                throw new InvalidOperationException("Canonical fresh SQL leave/preserved row mismatch: " + label);
            var audits = await db.AuditLogs.AsNoTracking().OrderBy(x => x.Id).ToListAsync();
            var oldAudits = before.RootElement.GetProperty("audits");
            var oldIds = oldAudits.EnumerateArray().Select(x => x.GetProperty("Id").GetGuid()).ToHashSet();
            var audit = audits.Single(x => !oldIds.Contains(x.Id));
            if (audits.Count != oldIds.Count + 1 || JsonSerializer.Serialize(audits.Where(x => oldIds.Contains(x.Id))) != oldAudits.GetRawText() || audit.AcademyId != academy || audit.ActorUserId != actor || audit.Action != "POST LeaveRequests" || audit.EntityType != "LeaveRequests")
                throw new InvalidOperationException("Success audit mismatch: " + label);
            using var metadata = JsonDocument.Parse(audit.MetadataJson!);
            if (metadata.RootElement.GetProperty("Route").GetString() != $"/api/academies/{academy}/leave-requests") throw new InvalidOperationException("Audit route mismatch");
            Pass(label);
        }
        foreach (var type in new string?[] { null, "", "  ", "Guardian", "Staff", "StudentBad", "1" })
        foreach (var both in new[] { false, true })
            await Denied($"unsupported-{count}-{(both ? "both" : "none")}", Payload(type, both ? student : null, both ? teacher : null), auth: token);
        foreach (var isStudent in new[] { true, false })
        {
            var type = isStudent ? "student" : " TEACHER "; var local = isStudent ? student : teacher;
            var other = isStudent ? teacher : student; var foreignMatching = isStudent ? foreignStudent : foreignTeacher; var foreignOther = isStudent ? foreignTeacher : foreignStudent;
            foreach (var scenario in new[] {
                (Label:"missing",Matching:(Guid?)null,Extra:(Guid?)null), (Label:"empty",Matching:(Guid?)Guid.Empty,Extra:(Guid?)null),
                (Label:"opposite-only",Matching:(Guid?)null,Extra:(Guid?)other), (Label:"both-local",Matching:(Guid?)local,Extra:(Guid?)other),
                (Label:"foreign-extra",Matching:(Guid?)local,Extra:(Guid?)foreignOther), (Label:"empty-extra",Matching:(Guid?)local,Extra:(Guid?)Guid.Empty),
                (Label:"foreign-matching",Matching:(Guid?)foreignMatching,Extra:(Guid?)null), (Label:"nonexistent",Matching:(Guid?)Guid.NewGuid(),Extra:(Guid?)null),
                (Label:"wrong-entity",Matching:(Guid?)other,Extra:(Guid?)null) })
                await Denied($"{(isStudent ? "student" : "teacher")}-{scenario.Label}", Payload(type, isStudent ? scenario.Matching : scenario.Extra, isStudent ? scenario.Extra : scenario.Matching), auth: token);
        }
        foreach (var reason in new string?[] { null, "", "  " }) await Denied("reason-" + count, Payload("Student", student, null, reason), auth: token);
        await Denied("reversed-dates", Payload("Student", student, null, end: start.AddDays(-1)), auth: token);
        await Denied("missing-type-binding", new { studentId = student, startDate = start, endDate = start, reason = "Binding" }, auth: token);
        await Denied("numeric-type-binding", new { requesterType = 1, studentId = student, startDate = start, endDate = start, reason = "Binding" }, auth: token);
        await Denied("missing-reason-binding", new { requesterType = "Student", studentId = student, startDate = start, endDate = start }, auth: token);
        await Denied("foreign-route", Payload("Student", foreignStudent, null), foreign, token, HttpStatusCode.Forbidden);
        await Denied("foreign-actor-own-route", Payload("Student", student, null), academy, foreignToken, HttpStatusCode.Forbidden);
        await Denied("anonymous", Payload("Student", student, null), expected: HttpStatusCode.Unauthorized);
        await Denied("teacher-existing-access", Payload("Teacher", null, teacher), auth: teacherToken, expected: HttpStatusCode.Forbidden);
        await Denied("operations-existing-access", Payload("Student", student, null), auth: operationsToken, expected: HttpStatusCode.Forbidden);
        await Denied("foreign-read-route", null, foreign, token, HttpStatusCode.Forbidden, HttpMethod.Get);
        foreach (var target in new[] { (Id: academy, Token: token), (Id: foreign, Token: foreignToken) })
        {
            var before = await Snapshot(); using var response = await Send(HttpMethod.Get, target.Id, null, target.Token);
            RequireFinanceStatus(response, HttpStatusCode.OK, "owned-list"); using var list = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var rows = await db.LeaveRequests.AsNoTracking().Where(x => x.AcademyId == target.Id).ToListAsync();
            if (list.RootElement.GetArrayLength() != rows.Count) throw new InvalidOperationException("List count/scope mismatch");
            foreach (var row in rows)
            {
                var item = list.RootElement.EnumerateArray().Single(x => x.GetProperty("id").GetGuid() == row.Id);
                Guid? Id(string name) => item.GetProperty(name).ValueKind == JsonValueKind.Null ? null : item.GetProperty(name).GetGuid();
                if (item.GetProperty("requesterType").GetString() != row.RequesterType || Id("studentId") != row.StudentId || Id("teacherId") != row.TeacherId || item.GetProperty("reason").GetString() != row.Reason || item.GetProperty("status").GetString() != row.Status || item.GetProperty("decisionNotes").GetString() != row.DecisionNotes || item.GetProperty("startDate").GetString() != row.StartDate.ToString("yyyy-MM-dd") || item.GetProperty("endDate").GetString() != row.EndDate.ToString("yyyy-MM-dd")) throw new InvalidOperationException("GET/fresh SQL leave mismatch");
            }
            if (before != await Snapshot()) throw new InvalidOperationException("GET changed leave/audit rows");
            Pass(target.Id == academy ? "owned-list-canonical-readback" : "foreign-owned-list-preserved-decision");
        }
        if (count != 63) throw new InvalidOperationException("Unexpected leave case count: " + count);
        Console.WriteLine("LEAVEIDENTITY REGRESSION PASS:63 cases; canonical supported types, exactly one owned matching person, preserved inactive-person policy/empty success body, real Identity/model binding/HTTP/fresh SQL/scoped GET, rejection leaves no row or success audit; existing non-admin access unchanged; browser/Decide/audit rollback/release pending.");
    }
}
