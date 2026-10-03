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
    private static async Task VerifyBatchTimesAsync(QaApiFactory factory, HttpClient client, bool baseline)
    {
        Guid academy, course, batchId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            academy = await db.Academies.Where(x => x.Name == "Synthetic Academy A").Select(x => x.Id).SingleAsync();
            course = await db.Courses.Where(x => x.AcademyId == academy).Select(x => x.Id).SingleAsync();
            batchId = await db.Batches.Where(x => x.AcademyId == academy).Select(x => x.Id).SingleAsync();
        }
        var token = await LoginAsync(client, "qa-admin-a@example.invalid", "Synthetic!39Ab");
        var foreign = await LoginAsync(client, "qa-admin-b@example.invalid", "Synthetic!39Ab");
        var teacher = await LoginAsync(client, "qa-teacher-a@example.invalid", "Synthetic!39Ab");
        var root = $"/api/academies/{academy}/batches"; var count = 0;
        async Task<string> State()
        {
            using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var governance = await GovernanceStateAsync(factory);
            return JsonSerializer.Serialize(new { governance.Stable, governance.Tasks, governance.Audits,
                Batches = await db.Batches.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
                Sessions = await db.ClassSessions.AsNoTracking().OrderBy(x => x.Id).ToListAsync() });
        }
        async Task<HttpResponseMessage> Send(string method, string route, object? body, string? actor = null, bool anonymous = false)
        {
            await Task.Delay(650); client.DefaultRequestHeaders.Authorization = anonymous ? null : new AuthenticationHeaderValue("Bearer", actor ?? token);
            using var request = new HttpRequestMessage(new HttpMethod(method), route);
            if (body is not null) request.Content = JsonContent.Create(body);
            return await client.SendAsync(request);
        }
        var cases = new (string Label, string? Json, int Kind)[] {
            ("Pascal", "[{\"Day\":\"Mon\",\"StartTime\":\"09:00\"}]", 0),
            ("camel", "[{\"day\":\"Mon\",\"startTime\":\"09:00\"}]", 1),
            ("mixed", "[{\"dAY\":\"Tuesday\",\"sTARTtIME\":\"17:45\"},{\"day\":\"Fri\",\"StartTime\":\"08:05\"}]", 1),
            ("optional-null", null, 0), ("optional-omitted", null, 0), ("optional-blank", "   ", 0),
            ("null-entry", "[null]", 2),
            ("mixed-null-entry", "[{\"Day\":\"Mon\",\"StartTime\":\"09:00\"},null]", 2),
            ("empty-array", "[]", 3), ("JSON-null", "null", 3), ("malformed", "not-json", 3),
            ("object", "{}", 3), ("scalar", "1", 3), ("string", "\"Mon\"", 3),
            ("empty-entry", "[{}]", 3), ("missing-time", "[{\"Day\":\"Mon\"}]", 3),
            ("null-day", "[{\"Day\":null,\"StartTime\":\"09:00\"}]", 3),
            ("null-time", "[{\"Day\":\"Mon\",\"StartTime\":null}]", 3),
            ("number-time", "[{\"Day\":\"Mon\",\"StartTime\":9}]", 3),
            ("invalid-day", "[{\"Day\":\"Funday\",\"StartTime\":\"09:00\"}]", 3),
            ("invalid-time", "[{\"Day\":\"Mon\",\"StartTime\":\"25:61\"}]", 3) };
        foreach (var method in new[] { "POST", "PUT" }) foreach (var item in cases)
        {
            Batch old;
            using (var scope = factory.Services.CreateScope()) old = await scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>().Batches.AsNoTracking().SingleAsync(x => x.Id == batchId);
            var body = BatchBody(old); body["name"] = "Synthetic times " + method + item.Label;
            body["courseId"] = course; body["meetingDaysJson"] = item.Json;
            if (item.Label == "optional-omitted") body.Remove("meetingDaysJson");
            if (method == "POST") { body.Remove("isActive"); body["batchCode"] = null; }
            var before = await State(); var audits = await AccessAuditCountAsync(factory);
            using var response = await Send(method, method == "POST" ? root : root + "/" + batchId, body);
            var expected = item.Kind == 3 ? 400 : baseline && item.Kind == 2 ? 500 : baseline && item.Kind == 1 ? 400 : item.Kind == 2 ? 400 : method == "POST" ? 201 : 200;
            RequireFinanceStatus(response, (HttpStatusCode)expected, method + "/" + item.Label);
            var responseText = await response.Content.ReadAsStringAsync();
            using var json = JsonDocument.Parse(responseText);
            if (expected >= 400)
            {
                if (before != await State()) throw new InvalidOperationException("Rejected teaching time wrote captured state.");
                if (!json.RootElement.TryGetProperty("message", out var message) || message.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(message.GetString()) ||
                    responseText.Contains("Exception", StringComparison.OrdinalIgnoreCase) || responseText.Contains("stackTrace", StringComparison.OrdinalIgnoreCase) || responseText.Contains("connectionString", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Schedule error lacks safe structured message.");
            }
            else
            {
                var id = json.RootElement.GetProperty("id").GetGuid(); Batch saved;
                using (var scope = factory.Services.CreateScope()) saved = await scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>().Batches.AsNoTracking().SingleAsync(x => x.Id == id);
                var days = string.IsNullOrWhiteSpace(item.Json) ? null : item.Json;
                if (saved.MeetingDaysJson != days || json.RootElement.GetProperty("meetingDaysJson").GetString() != days || await AccessAuditCountAsync(factory) != audits + 1)
                    throw new InvalidOperationException("Schedule create/update persistence/summary/audit mismatch.");
                var stable = await State(); using var read = await Send("GET", root, null); RequireFinanceStatus(read, HttpStatusCode.OK, "schedule readback");
                using var list = JsonDocument.Parse(await read.Content.ReadAsStringAsync());
                if (list.RootElement.EnumerateArray().Single(x => x.GetProperty("id").GetGuid() == id).GetProperty("meetingDaysJson").GetString() != days || stable != await State())
                    throw new InvalidOperationException("Schedule GET roundtrip/read no-write mismatch.");
            }
            count++; Console.WriteLine($"BATCHTIMES CASE {method}/{item.Label} HTTP{expected} PASS.");
        }
        foreach (var method in new[] { "POST", "PUT" }) foreach (var actor in new[] { ("foreign", foreign, false, 403), ("teacher", teacher, false, 403), ("anonymous", token, true, 401) })
        {
            var before = await State(); var body = new { name = "Denied times", courseId = course, capacity = 10, deliveryMode = "InPerson", meetingDaysJson = "[null]", isActive = true };
            using var response = await Send(method, method == "POST" ? root : root + "/" + batchId, body, actor.Item2, actor.Item3);
            RequireFinanceStatus(response, (HttpStatusCode)actor.Item4, method + "/" + actor.Item1);
            if (before != await State()) throw new InvalidOperationException("Denied actor changed captured state.");
            count++; Console.WriteLine($"BATCHTIMES CASE {method}/{actor.Item1} HTTP{actor.Item4} PASS.");
        }
        Console.WriteLine($"BATCHTIMES {(baseline ? "BASELINE REPRODUCED" : "REGRESSION PASS")}: {count} cases; nested casing/null-entry, optional and invalid shapes, POST/PUT, fresh SQL roundtrip/rejected no-write, auth controls; no browser/audit-fault rollback claim.");
    }
}
