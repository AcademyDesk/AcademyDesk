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
    private static Dictionary<string, object?> CourseBody(ProgramCourse row) => new() {
        ["name"] = row.Name, ["courseCode"] = row.CourseCode, ["academyType"] = row.AcademyType,
        ["subjectArea"] = row.SubjectArea, ["level"] = row.Level, ["description"] = row.Description,
        ["durationMonths"] = row.DurationMonths, ["weeklySessions"] = row.WeeklySessions,
        ["sessionMinutes"] = row.SessionMinutes, ["minimumAge"] = row.MinimumAge, ["maximumAge"] = row.MaximumAge,
        ["deliveryMode"] = row.DeliveryMode, ["prerequisites"] = row.Prerequisites, ["learningOutcomes"] = row.LearningOutcomes,
        ["isPublished"] = row.IsPublished, ["isActive"] = row.IsActive };

    private static async Task VerifyCoursePreservationAsync(QaApiFactory factory, HttpClient client)
    {
        Guid academyA, academyB, foreignId, actorId; var seed = 0; var count = 0;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            academyA = await db.Academies.Where(x => x.Name == "Synthetic Academy A").Select(x => x.Id).SingleAsync();
            academyB = await db.Academies.Where(x => x.Name == "Synthetic Academy B").Select(x => x.Id).SingleAsync();
            var foreign = new ProgramCourse { AcademyId = academyB, Name = "FOREIGN-COURSE-MARKER", CourseCode = "QA-FOREIGN" };
            db.Add(foreign); await db.SaveChangesAsync(); foreignId = foreign.Id;
            actorId = await scope.ServiceProvider.GetRequiredService<IdentityDbContext>().Users.Where(x => x.Email == "qa-admin-a@example.invalid").Select(x => x.Id).SingleAsync();
        }
        var token = await LoginAsync(client, "qa-admin-a@example.invalid", "Synthetic!39Ab");
        var foreignToken = await LoginAsync(client, "qa-admin-b@example.invalid", "Synthetic!39Ab");
        var teacherToken = await LoginAsync(client, "qa-teacher-a@example.invalid", "Synthetic!39Ab");
        var root = $"/api/academies/{academyA}/courses";
        void Passed(string label, HttpStatusCode status) { count++; Console.WriteLine($"COURSEPRESERVE CASE {label} HTTP{(int)status} PASS."); }
        async Task<HttpResponseMessage> Send(string method, string route, object? body = null, string? actor = null, bool anonymous = false)
        {
            await Task.Delay(650); client.DefaultRequestHeaders.Authorization = anonymous ? null : new AuthenticationHeaderValue("Bearer", actor ?? token);
            using var request = new HttpRequestMessage(new HttpMethod(method), route);
            if (body is not null) request.Content = JsonContent.Create(body);
            return await client.SendAsync(request);
        }
        async Task<ProgramCourse> Row(Guid id) { using var scope = factory.Services.CreateScope(); return await scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>().Courses.AsNoTracking().SingleAsync(x => x.Id == id); }
        async Task<string> State(Guid? exclude = null, bool audit = true)
        {
            using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var governance = await GovernanceStateAsync(factory);
            return JsonSerializer.Serialize(new { governance.Stable, governance.Tasks, Audits = audit ? governance.Audits : null,
                Courses = await db.Courses.AsNoTracking().Where(x => exclude == null || x.Id != exclude).OrderBy(x => x.Id).ToListAsync(),
                Prerequisites = await db.CoursePrerequisites.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
                Modules = await db.CourseModules.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
                Batches = await db.Batches.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
                Sessions = await db.ClassSessions.AsNoTracking().OrderBy(x => x.Id).ToListAsync() });
        }
        async Task VerifyAudit(IReadOnlyList<AuditLog> before, string method, string route)
        {
            using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var audits = await db.AuditLogs.AsNoTracking().ToListAsync();
            var added = audits.Where(x => before.All(y => y.Id != x.Id)).ToArray();
            if (audits.Count != before.Count + 1 || added.Length != 1 || added[0].ActorUserId != actorId || added[0].AcademyId != academyA ||
                added[0].Action != method + " Courses" || !added[0].MetadataJson!.Contains(route, StringComparison.Ordinal))
                throw new InvalidOperationException("Course actor/route/audit mismatch.");
        }
        void ProjectionEquals(JsonElement projection, ProgramCourse saved)
        {
            if (projection.GetProperty("id").GetGuid() != saved.Id) throw new InvalidOperationException("Course response ID mismatch.");
            foreach (var pair in CourseBody(saved))
                if (!JsonElement.DeepEquals(projection.GetProperty(pair.Key), JsonSerializer.SerializeToElement(pair.Value)))
                    throw new InvalidOperationException("Course projection mismatch: " + pair.Key);
        }
        async Task<JsonElement> Projection(Guid id)
        {
            var before = await State(); using var response = await Send("GET", root); RequireFinanceStatus(response, HttpStatusCode.OK, "course GET");
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var rows = json.RootElement.EnumerateArray().ToArray();
            if (rows.Any(x => x.GetProperty("id").GetGuid() == foreignId) || json.RootElement.GetRawText().Contains("FOREIGN-COURSE-MARKER", StringComparison.Ordinal))
                throw new InvalidOperationException("Course GET leaked foreign data.");
            var result = rows.Single(x => x.GetProperty("id").GetGuid() == id).Clone(); ProjectionEquals(result, await Row(id));
            if (before != await State()) throw new InvalidOperationException("Course read wrote captured state.");
            return result;
        }
        async Task<Guid> Seed(string type = "Music", bool populated = true, bool active = true, string? optionalMode = null)
        {
            var fixture = new ProgramCourse { AcademyId = academyA, Name = $"Synthetic course {++seed}", AcademyType = type,
                CourseCode = populated ? $"QA-C-{seed}" : null, SubjectArea = populated ? "QA subject" : null,
                Level = populated ? "Level1" : null, Description = populated ? "QA description" : null,
                DurationMonths = populated ? 9 : null, WeeklySessions = populated ? 2 : null, SessionMinutes = populated ? 45 : null,
                MinimumAge = populated ? 0 : null, MaximumAge = populated ? 18 : null, DeliveryMode = populated ? "Hybrid" : null,
                Prerequisites = populated ? "QA prerequisite text" : null, LearningOutcomes = populated ? "QA outcomes" : null, IsPublished = populated };
            var body = CourseBody(fixture); body.Remove("isActive");
            if (optionalMode == "omitted") foreach (var key in body.Keys.Except(new[] { "name", "academyType" }).ToArray()) body.Remove(key);
            if (optionalMode == "blank") foreach (var key in new[] { "courseCode", "subjectArea", "level", "description", "deliveryMode", "prerequisites", "learningOutcomes" }) body[key] = "  ";
            var stable = await State(audit: false); var audits = (await GovernanceStateAsync(factory)).Audits;
            using var response = await Send("POST", root, body); RequireFinanceStatus(response, HttpStatusCode.Created, "course create");
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync()); var id = json.RootElement.GetProperty("id").GetGuid(); var saved = await Row(id);
            fixture.Id = saved.Id; fixture.CreatedAtUtc = saved.CreatedAtUtc;
            if (JsonSerializer.Serialize(fixture) != JsonSerializer.Serialize(saved) || stable != await State(id, false))
                throw new InvalidOperationException("Course create full row/unrelated state mismatch.");
            ProjectionEquals(json.RootElement, saved); await VerifyAudit(audits, "POST", root);
            await Projection(id);
            if (!active) { using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); (await db.Courses.SingleAsync(x => x.Id == id)).IsActive = false; await db.SaveChangesAsync(); }
            return id;
        }
        async Task Update(Guid id, Dictionary<string, object?> body, ProgramCourse expected, string label)
        {
            var stable = await State(id, false); var audits = (await GovernanceStateAsync(factory)).Audits;
            using var response = await Send("PUT", root + "/" + id, body); RequireFinanceStatus(response, HttpStatusCode.OK, label);
            var saved = await Row(id);
            if (JsonSerializer.Serialize(expected) != JsonSerializer.Serialize(saved) || stable != await State(id, false))
                throw new InvalidOperationException("Course update full row/unrelated captured state mismatch: " + label);
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync()); ProjectionEquals(json.RootElement, saved);
            await VerifyAudit(audits, "PUT", root + "/" + id); await Projection(id); Passed(label, HttpStatusCode.OK);
        }
        foreach (var type in new[] { "Music", "Tuition", "Coaching" }) foreach (var populated in new[] { true, false }) foreach (var action in new[] { "edit", "deactivate", "reactivate" })
        {
            var id = await Seed(type, populated, action != "reactivate"); var expected = await Row(id); var projection = await Projection(id);
            using (var scope = factory.Services.CreateScope()) { var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
                var prerequisite = new CoursePrerequisite { AcademyId = academyA, CourseId = id, RequiredCourseId = await db.Courses.Where(x => x.AcademyId == academyA && x.Id != id).Select(x => x.Id).FirstAsync() };
                db.Add(prerequisite); await db.SaveChangesAsync(); }
            var body = CourseBody(expected); foreach (var key in body.Keys.ToArray()) body[key] = projection.GetProperty(key).Clone();
            if (action == "edit") { expected.Name = $"Renamed synthetic {seed}"; body["name"] = expected.Name; }
            else { expected.IsActive = !expected.IsActive; body["isActive"] = expected.IsActive; }
            await Update(id, body, expected, $"preserved/{type}/{(populated ? "populated" : "null")}/{action}");
        }
        foreach (var type in new[] { "Music", "Tuition", "Coaching" })
        {
            var id = await Seed(type); var expected = await Row(id); expected.Level = null;
            await Update(id, CourseBody(expected), expected, "visible-level-clear/" + type);
        }
        var control = await Seed(); var cleared = await Row(control);
        cleared.CourseCode = null; cleared.SubjectArea = null; cleared.Level = null; cleared.Description = null; cleared.DurationMonths = null;
        cleared.WeeklySessions = null; cleared.SessionMinutes = null; cleared.MinimumAge = null; cleared.MaximumAge = null; cleared.DeliveryMode = null;
        cleared.Prerequisites = null; cleared.LearningOutcomes = null; cleared.IsPublished = false;
        await Update(control, CourseBody(cleared), cleared, "explicit-full-null-clear");
        var duplicate = await Seed(); var duplicateCode = (await Row(duplicate)).CourseCode;
        foreach (var invalid in new[] { "name", "type", "weekly-low", "weekly-high", "session-low", "session-high", "min-low", "min-high", "max-low", "max-high", "age-reversed", "duplicate-code" })
        {
            var body = CourseBody(cleared);
            switch (invalid) {
                case "name": body["name"] = "  "; break; case "type": body["academyType"] = "Invalid"; break;
                case "weekly-low": body["weeklySessions"] = 0; break; case "weekly-high": body["weeklySessions"] = 15; break;
                case "session-low": body["sessionMinutes"] = 14; break; case "session-high": body["sessionMinutes"] = 481; break;
                case "min-low": body["minimumAge"] = -1; break; case "min-high": body["minimumAge"] = 121; break;
                case "max-low": body["maximumAge"] = -1; break; case "max-high": body["maximumAge"] = 121; break;
                case "age-reversed": body["minimumAge"] = 19; body["maximumAge"] = 18; break;
                default: body["courseCode"] = " " + duplicateCode + " "; break;
            }
            var before = await State(); using var response = await Send("PUT", root + "/" + control, body); RequireFinanceStatus(response, HttpStatusCode.BadRequest, invalid);
            if (before != await State()) throw new InvalidOperationException("Invalid Course request wrote captured state."); Passed("rejected/" + invalid, HttpStatusCode.BadRequest);
        }
        foreach (var method in new[] { "POST", "PUT" }) foreach (var actor in new[] { "foreign", "teacher", "anonymous" })
        {
            var before = await State(); var route = method == "POST" ? root : root + "/" + control;
            var status = actor == "anonymous" ? HttpStatusCode.Unauthorized : HttpStatusCode.Forbidden;
            using var response = await Send(method, route, CourseBody(cleared), actor == "foreign" ? foreignToken : teacherToken, actor == "anonymous"); RequireFinanceStatus(response, status, actor);
            if (before != await State()) throw new InvalidOperationException("Unauthorized Course request wrote captured state."); Passed(method + "/" + actor, status);
        }
        foreach (var item in new[] { ("missing", root + "/" + Guid.NewGuid(), HttpStatusCode.NotFound),
            ("foreign-id", root + "/" + foreignId, HttpStatusCode.NotFound), ("foreign-route", $"/api/academies/{academyB}/courses/{control}", HttpStatusCode.Forbidden) })
        {
            var before = await State(); using var response = await Send("PUT", item.Item2, CourseBody(cleared)); RequireFinanceStatus(response, item.Item3, item.Item1);
            if (before != await State()) throw new InvalidOperationException("Wrong Course target wrote captured state."); Passed("rejected/" + item.Item1, item.Item3);
        }
        foreach (var mode in new[] { "null", "omitted", "blank" }) { await Seed(populated: false, optionalMode: mode); Passed("create-optionals/" + mode, HttpStatusCode.Created); }
        var legacyId = await Seed(); var lost = await Row(legacyId); var legacy = new Dictionary<string, object?> {
            ["name"] = lost.Name, ["academyType"] = lost.AcademyType, ["level"] = lost.Level, ["description"] = lost.Description, ["durationMonths"] = null, ["isActive"] = lost.IsActive };
        lost.CourseCode = null; lost.SubjectArea = null; lost.DurationMonths = null; lost.WeeklySessions = null; lost.SessionMinutes = null;
        lost.MinimumAge = null; lost.MaximumAge = null; lost.DeliveryMode = null; lost.Prerequisites = null; lost.LearningOutcomes = null; lost.IsPublished = false;
        await Update(legacyId, legacy, lost, "legacy-abbreviated-loss-reproduced");
        Console.WriteLine($"COURSEPRESERVE REGRESSION PASS: {count} cases; real Identity/HTTP/SQL full rows/projections/null controls/intentional clears/rejected no-write/audit; browser/concurrency/audit-fault rollback excluded.");
    }
}
