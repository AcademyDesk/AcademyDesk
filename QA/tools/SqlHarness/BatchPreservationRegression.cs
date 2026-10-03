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
    private static Dictionary<string, object?> BatchBody(Batch row) => new() {
        ["name"] = row.Name, ["batchCode"] = row.BatchCode, ["courseId"] = row.CourseId, ["teacherId"] = row.TeacherId,
        ["branchId"] = row.BranchId, ["capacity"] = row.Capacity, ["waitlistCapacity"] = row.WaitlistCapacity,
        ["deliveryMode"] = row.DeliveryMode, ["classType"] = row.ClassType, ["sessionMinutes"] = row.SessionMinutes,
        ["sessionsPerWeek"] = row.SessionsPerWeek, ["meetingDaysJson"] = row.MeetingDaysJson, ["meetingLink"] = row.MeetingLink,
        ["meetingPattern"] = row.MeetingPattern, ["roomName"] = row.RoomName, ["enrollmentStatus"] = row.EnrollmentStatus,
        ["adminNotes"] = row.AdminNotes, ["startDate"] = row.StartDate, ["endDate"] = row.EndDate, ["isActive"] = row.IsActive };

    private static async Task VerifyBatchPreservationAsync(QaApiFactory factory, HttpClient client, bool baseline)
    {
        Guid academyA, academyB, course, teacher, secondTeacher, branch;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            academyA = await db.Academies.Where(x => x.Name == "Synthetic Academy A").Select(x => x.Id).SingleAsync();
            academyB = await db.Academies.Where(x => x.Name == "Synthetic Academy B").Select(x => x.Id).SingleAsync();
            course = await db.Courses.Where(x => x.AcademyId == academyA).Select(x => x.Id).SingleAsync();
            teacher = await db.Teachers.Where(x => x.AcademyId == academyA).Select(x => x.Id).SingleAsync();
            var next = new Teacher { AcademyId = academyA, FirstName = "Second", LastName = "Synthetic" };
            var location = new Branch { AcademyId = academyA, Name = "Synthetic batch location" };
            db.AddRange(next, location); await db.SaveChangesAsync(); secondTeacher = next.Id; branch = location.Id;
        }
        var token = await LoginAsync(client, "qa-admin-a@example.invalid", "Synthetic!39Ab");
        var foreignToken = await LoginAsync(client, "qa-admin-b@example.invalid", "Synthetic!39Ab");
        var teacherToken = await LoginAsync(client, "qa-teacher-a@example.invalid", "Synthetic!39Ab");
        var root = $"/api/academies/{academyA}/batches"; var count = 0; var seed = 0;
        void Passed(string label) { count++; Console.WriteLine($"BATCHPRESERVE CASE {label} PASS."); }
        async Task<HttpResponseMessage> Send(string method, string route, object? body = null, string? actor = null, bool anonymous = false)
        {
            await Task.Delay(650); client.DefaultRequestHeaders.Authorization = anonymous ? null : new AuthenticationHeaderValue("Bearer", actor ?? token);
            using var request = new HttpRequestMessage(new HttpMethod(method), route);
            if (body is not null) request.Content = JsonContent.Create(body);
            return await client.SendAsync(request);
        }
        async Task<Batch> Row(Guid id) { using var scope = factory.Services.CreateScope(); return await scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>().Batches.AsNoTracking().SingleAsync(x => x.Id == id); }
        async Task<List<ClassSession>> Sessions(Guid id) { using var scope = factory.Services.CreateScope(); return await scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>().ClassSessions.AsNoTracking().Where(x => x.BatchId == id).OrderBy(x => x.Id).ToListAsync(); }
        async Task<string> State(Guid? exclude = null, bool includeAudits = true)
        {
            using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var governance = await GovernanceStateAsync(factory);
            return JsonSerializer.Serialize(new { governance.Stable, governance.Tasks,
                Audits = includeAudits ? governance.Audits : null,
                Batches = await db.Batches.AsNoTracking().Where(x => exclude == null || x.Id != exclude).OrderBy(x => x.Id).ToListAsync(),
                Sessions = await db.ClassSessions.AsNoTracking().Where(x => exclude == null || x.BatchId != exclude).OrderBy(x => x.Id).ToListAsync() });
        }
        async Task<JsonElement> Projection(Guid id)
        {
            var before = await State(); using var response = await Send("GET", root); RequireFinanceStatus(response, HttpStatusCode.OK, "batch GET");
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var result = json.RootElement.EnumerateArray().Single(x => x.GetProperty("id").GetGuid() == id).Clone();
            if (before != await State()) throw new InvalidOperationException("Batch read wrote captured state.");
            return result;
        }
        async Task<Guid> Seed(string mode, bool active = true, bool empty = false)
        {
            var date = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1);
            var row = new Batch { AcademyId = academyA, Name = $"Synthetic preservation {++seed}", BatchCode = empty ? null : $"QA-P-{seed}",
                CourseId = course, TeacherId = teacher, BranchId = empty ? null : branch, Capacity = 12, WaitlistCapacity = 7,
                ClassType = "OneToOne", SessionMinutes = 45, SessionsPerWeek = 2,
                MeetingDaysJson = empty ? null : "[{\"Day\":\"Mon\",\"StartTime\":\"10:30\"}]",
                MeetingLink = empty ? null : "https://example.invalid/qa-meeting", DeliveryMode = mode,
                MeetingPattern = empty ? null : "Mon 10:30", RoomName = empty ? null : "Synthetic room", EnrollmentStatus = "Closed",
                AdminNotes = empty ? null : "Synthetic notes", StartDate = empty ? null : date, EndDate = empty ? null : date.AddDays(90) };
            var body = BatchBody(row); body.Remove("isActive");
            using var response = await Send("POST", root, body);
            if (response.StatusCode != HttpStatusCode.Created)
                Console.WriteLine("BATCHPRESERVE SEEDREJECTION: " + await response.Content.ReadAsStringAsync());
            RequireFinanceStatus(response, HttpStatusCode.Created, "batch seed");
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync()); var id = json.RootElement.GetProperty("id").GetGuid();
            var saved = await Row(id);
            if (saved.ClassType != row.ClassType || saved.SessionMinutes != 45 || saved.SessionsPerWeek != 2 || saved.MeetingDaysJson != row.MeetingDaysJson) throw new InvalidOperationException("Batch seed fields not persisted.");
            using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            (await db.Batches.SingleAsync(x => x.Id == id)).IsActive = active;
            var time = DateTime.UtcNow;
            foreach (var (offset, status) in new[] { (7, "Scheduled"), (-7, "Scheduled"), (8, "Cancelled") })
                db.ClassSessions.Add(new ClassSession { AcademyId = academyA, BatchId = id, TeacherId = teacher, BranchId = branch,
                    StartUtc = time.AddDays(offset), EndUtc = time.AddDays(offset).AddMinutes(45), DeliveryMode = mode, RoomName = "Session room marker", Status = status });
            await db.SaveChangesAsync(); return id;
        }
        async Task CheckUpdate(Guid id, Dictionary<string, object?> body, Batch expected, string label, bool reject = false)
        {
            var stable = await State(id, false); var full = await State(); var sessions = await Sessions(id); var audits = await AccessAuditCountAsync(factory);
            using var response = await Send("PUT", root + "/" + id, body); RequireFinanceStatus(response, reject ? HttpStatusCode.BadRequest : HttpStatusCode.OK, label);
            if (reject) { if (full != await State()) throw new InvalidOperationException("Rejected preservation request wrote captured state."); }
            else
            {
                var actual = await Row(id);
                if (JsonSerializer.Serialize(expected) != JsonSerializer.Serialize(actual) || stable != await State(id, false) || await AccessAuditCountAsync(factory) != audits + 1)
                    throw new InvalidOperationException("Batch full row/unrelated state/audit mismatch: " + label);
                foreach (var session in sessions) if (session.Status == "Scheduled" && session.StartUtc > DateTime.UtcNow) session.TeacherId = expected.TeacherId;
                if (JsonSerializer.Serialize(sessions) != JsonSerializer.Serialize(await Sessions(id))) throw new InvalidOperationException("Existing future teacher synchronization or unrelated session fields changed.");
                var projection = await Projection(id);
                foreach (var pair in body)
                {
                    // New extended projection is tested only after repair; inherited request binding is tested against both versions.
                    if (baseline && new[] { "classType", "sessionMinutes", "sessionsPerWeek", "meetingDaysJson" }.Contains(pair.Key)) continue;
                    var actualValue = projection.GetProperty(pair.Key);
                    var expectedValue = JsonSerializer.SerializeToElement(BatchBody(actual)[pair.Key]);
                    if (!JsonElement.DeepEquals(actualValue, expectedValue)) throw new InvalidOperationException("Batch summary field mismatch: " + pair.Key);
                }
            }
            Passed(label);
        }
        // Real inherited record properties bind from JSON even though they are absent from the derived constructor.
        var bindingId = await Seed("Hybrid"); var binding = await Row(bindingId);
        await CheckUpdate(bindingId, BatchBody(binding), binding, "inherited-request-properties/explicit-full-roundtrip");
        foreach (var mode in baseline ? new[] { "InPerson", "Online", "Hybrid" } : new[] { "InPerson", "Online", "Hybrid", "null-optionals" })
        foreach (var action in new[] { "edit", "deactivate", "reactivate", "assign" })
        {
            var id = await Seed(mode == "null-optionals" ? "InPerson" : mode, action != "reactivate", mode == "null-optionals");
            var expected = await Row(id); var projection = await Projection(id);
            if (baseline && projection.TryGetProperty("classType", out _)) throw new InvalidOperationException("Baseline already exposes extended class fields.");
            var body = BatchBody(expected);
            if (!baseline) foreach (var key in body.Keys.ToArray()) body[key] = projection.GetProperty(key).Clone();
            if (action == "edit") { expected.Name = $"Renamed synthetic {seed}"; body["name"] = expected.Name; }
            if (action is "deactivate" or "reactivate") { expected.IsActive = !expected.IsActive; body["isActive"] = expected.IsActive; }
            if (action == "assign") { expected.TeacherId = secondTeacher; body["teacherId"] = secondTeacher; }
            var reject = baseline && action == "assign" && mode is "Online" or "Hybrid";
            if (baseline)
            {
                var lost = new[] { "classType", "sessionMinutes", "sessionsPerWeek", "meetingDaysJson", "meetingLink" };
                foreach (var key in lost) body.Remove(key);
                if (action != "assign")
                {
                    foreach (var key in new[] { "batchCode", "waitlistCapacity", "deliveryMode", "meetingPattern", "roomName", "enrollmentStatus", "adminNotes" }) body.Remove(key);
                    expected.BatchCode = null; expected.WaitlistCapacity = 0; expected.DeliveryMode = "InPerson";
                    expected.MeetingPattern = null; expected.RoomName = null; expected.EnrollmentStatus = "Open"; expected.AdminNotes = null;
                    if (action == "edit") { expected.EndDate = null; body["endDate"] = null; }
                }
                expected.ClassType = "Group"; expected.SessionMinutes = 60; expected.SessionsPerWeek = 1; expected.MeetingDaysJson = null; expected.MeetingLink = null;
            }
            await CheckUpdate(id, body, expected, (baseline ? reject ? "baseline-rejected/" : "baseline-loss/" : "preserved/") + mode + "/" + action, reject);
        }
        if (baseline) { Console.WriteLine($"BATCHPRESERVE BASELINE REPRODUCED: {count} cases; inherited fields bind; summary/legacy payload omissions cause data loss and Online/Hybrid assignment rejection."); return; }
        var clearId = await Seed("InPerson"); var cleared = await Row(clearId);
        cleared.BatchCode = null; cleared.BranchId = null; cleared.TeacherId = null; cleared.MeetingDaysJson = null; cleared.MeetingLink = null;
        cleared.MeetingPattern = null; cleared.RoomName = null; cleared.AdminNotes = null; cleared.StartDate = null; cleared.EndDate = null;
        await CheckUpdate(clearId, BatchBody(cleared), cleared, "explicit-null-clear/current-replacement-contract");
        foreach (var rejection in new[] {
            ("missing-meeting-link/Online", "Online", "link", token, false, HttpStatusCode.BadRequest),
            ("missing-meeting-link/Hybrid", "Hybrid", "link", token, false, HttpStatusCode.BadRequest),
            ("invalid-meeting-json", "InPerson", "json", token, false, HttpStatusCode.BadRequest),
            ("foreign-course", "InPerson", "course", token, false, HttpStatusCode.BadRequest),
            ("foreign-actor", "InPerson", "none", foreignToken, false, HttpStatusCode.Forbidden),
            ("teacher", "InPerson", "none", teacherToken, false, HttpStatusCode.Forbidden),
            ("anonymous", "InPerson", "none", token, true, HttpStatusCode.Unauthorized) })
        {
            var before = await State(); var invalid = BatchBody(cleared); invalid["deliveryMode"] = rejection.Item2;
            if (rejection.Item3 == "link") invalid["meetingLink"] = null;
            if (rejection.Item3 == "json") invalid["meetingDaysJson"] = "not-json";
            if (rejection.Item3 == "course") invalid["courseId"] = Guid.NewGuid();
            using var response = await Send("PUT", root + "/" + clearId, invalid, rejection.Item4, rejection.Item5);
            RequireFinanceStatus(response, rejection.Item6, rejection.Item1);
            if (before != await State()) throw new InvalidOperationException("Rejected batch request wrote captured data.");
            Passed("rejected-no-write/" + rejection.Item1);
        }
        foreach (var route in new[] { root + "/" + Guid.NewGuid(), $"/api/academies/{academyB}/batches/{clearId}" })
        {
            var before = await State(); using var response = await Send("PUT", route, BatchBody(cleared));
            RequireFinanceStatus(response, route.StartsWith(root, StringComparison.Ordinal) ? HttpStatusCode.NotFound : HttpStatusCode.Forbidden, "missing/foreign route");
            if (before != await State()) throw new InvalidOperationException("Wrong batch route changed captured data.");
            Passed(route.StartsWith(root, StringComparison.Ordinal) ? "rejected-no-write/missing-batch" : "rejected-no-write/foreign-route");
        }
        Console.WriteLine($"BATCHPRESERVE REGRESSION PASS: {count} cases; full rows/projections/inherited JSON binding, future-only teacher synchronization, explicit nullable clear and rejected no-write; browser/concurrency/audit-fault rollback not covered.");
    }
}
