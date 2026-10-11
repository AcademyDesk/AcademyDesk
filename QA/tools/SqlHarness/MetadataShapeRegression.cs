using System.Net;
using System.Net.Http.Headers;
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
    private static async Task VerifyMetadataShapeAsync(QaApiFactory factory, HttpClient client)
    {
        Guid academy, foreign, teacher, student;
        var count = 0;
        var roots = new string?[] { null, "", "  ", "null", "[]", "[{}]", "\"text\"", "12", "true", "false", "{" };
        var active = "\"important\":\"true\",\"expiresAtUtc\":\"2099-10-12T00:00:00Z\"";
        var validAdmin = "{" + active + ",\"audiences\":\"Admin\"}";
        Notification target, localSentinel, foreignSentinel;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            academy = await db.Academies.Where(x => x.Name == "Synthetic Academy A").Select(x => x.Id).SingleAsync();
            foreign = await db.Academies.Where(x => x.Name == "Synthetic Academy B").Select(x => x.Id).SingleAsync();
            teacher = await db.Teachers.Select(x => x.Id).SingleAsync();
            student = await db.Students.Where(x => x.AcademyId == academy).Select(x => x.Id).SingleAsync();
            Notification Row(Guid tenant, string title) => new() { AcademyId = tenant, RecipientType = "Academy", Title = title,
                Message = "Synthetic metadata fixture", Channel = "InApp", Status = "Sent", VariablesJson = validAdmin };
            target = Row(academy, "Metadata target"); localSentinel = Row(academy, "Valid local sentinel"); foreignSentinel = Row(foreign, "Foreign sentinel");
            db.AddRange(target, localSentinel, foreignSentinel); await db.SaveChangesAsync();
        }
        var platformId = await CreateAccessActorAsync(factory, "qa-metadata-platform@example.invalid", "PlatformOwner", academy);
        await CreateAccessActorAsync(factory, "qa-metadata-student@example.invalid", "Student", academy, student);
        using (var scope = factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var row = (await users.FindByIdAsync(platformId.ToString()))!; row.AcademyId = null; row.IsPlatformOwner = true;
            if (!(await users.UpdateAsync(row)).Succeeded) throw new InvalidOperationException("Metadata platform fixture failed.");
        }
        var admin = await LoginAsync(client, "qa-admin-a@example.invalid", "Synthetic!39Ab");
        var other = await LoginAsync(client, "qa-admin-b@example.invalid", "Synthetic!39Ab");
        var teacherToken = await LoginAsync(client, "qa-teacher-a@example.invalid", "Synthetic!39Ab");
        var studentToken = await LoginAsync(client, "qa-metadata-student@example.invalid", "Synthetic!39Ab");
        var platform = await LoginAsync(client, "qa-metadata-platform@example.invalid", "Synthetic!39Ab");
        client.DefaultRequestHeaders.Authorization = null;
        async Task<string> Snapshot()
        {
            using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            return JsonSerializer.Serialize(new { Teachers = await db.Teachers.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(),
                Notifications = await db.Notifications.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(),
                Audits = await db.AuditLogs.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(),
                PlatformAudits = await db.PlatformAuditEntries.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(),
                Receipts = await db.NotificationReadReceipts.AsNoTracking().OrderBy(x => x.NotificationId).ThenBy(x => x.UserId).ToArrayAsync() });
        }
        async Task<JsonDocument?> Read(string path, string? token, HttpStatusCode expected, string label)
        {
            await Task.Delay(650); var before = await Snapshot(); using var request = new HttpRequestMessage(HttpMethod.Get, path);
            if (token is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            using var response = await client.SendAsync(request); RequireFinanceStatus(response, expected, label);
            if (before != await Snapshot()) throw new InvalidOperationException("Metadata GET mutated captured SQL/audit rows: " + label);
            count++; Console.WriteLine("PASS: METADATASHAPE " + label + " status/no-write");
            return expected == HttpStatusCode.OK ? JsonDocument.Parse(await response.Content.ReadAsStringAsync()) : null;
        }
        var compensationPath = $"/api/academies/{academy}/teachers/{teacher}/compensation";
        var compensation = roots.Select(x => (Metadata: x, Model: "", Monthly: (decimal?)null, Hourly: (decimal?)null, Date: (string?)null)).ToList();
        compensation.Add(("{\"Model\":\"Monthly\",\"MonthlySalary\":0,\"EffectiveFrom\":\"2026-10-01\"}", "Monthly", 0m, null, "2026-10-01"));
        compensation.Add(("{\"model\":\"Monthly\",\"baseMonthlySalary\":\"1200.50\"}", "Monthly", 1200.50m, null, null));
        compensation.Add(("{\"MODEL\":\"Hourly\",\"baseHourlyRate\":0,\"beginnerHourlyRate\":10}", "Hourly", null, 0m, null));
        foreach (var item in compensation)
        {
            using (var scope = factory.Services.CreateScope()) { var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
                (await db.Teachers.SingleAsync(x => x.Id == teacher)).CompensationJson = item.Metadata; await db.SaveChangesAsync(); }
            using var json = (await Read(compensationPath, admin, HttpStatusCode.OK, "compensation-" + count))!; var result = json.RootElement;
            decimal? Number(string name) => result.GetProperty(name).ValueKind == JsonValueKind.Null ? null : result.GetProperty(name).GetDecimal();
            if (result.GetProperty("teacherId").GetGuid() != teacher || result.GetProperty("model").GetString() != item.Model ||
                Number("monthlySalary") != item.Monthly || Number("standardHourlyRate") != item.Hourly || result.GetProperty("effectiveFrom").GetString() != item.Date ||
                Number("beginnerHourlyRate") != (item.Model == "Hourly" ? 10m : null) || Number("intermediateHourlyRate") != null || Number("advancedHourlyRate") != null)
                throw new InvalidOperationException("Compensation metadata response differs.");
        }
        var announcements = roots.Select(x => (Metadata: x, Portal: false, Platform: false, Expiry: (string?)null)).ToList();
        foreach (var item in new[] {
            "{\"important\":true,\"expiresAtUtc\":\"2099-10-12T00:00:00Z\"}",
            "{\"important\":[],\"expiresAtUtc\":\"2099-10-12T00:00:00Z\"}",
            "{\"important\":\"true\",\"expiresAtUtc\":12}", "{\"important\":\"true\",\"expiresAtUtc\":{}}",
            "{" + active + ",\"startsAtUtc\":[]}", "{" + active + ",\"startsAtUtc\":true}" }) announcements.Add((item, false, false, null));
        foreach (var value in new[] { "[]", "{}", "12", "true" }) announcements.Add(("{" + active + ",\"audiences\":" + value + "}", false, false, null));
        announcements.Add((validAdmin, true, true, "2099-10-12T00:00:00Z"));
        announcements.Add(("{" + active + "}", true, false, null));
        announcements.Add(("{" + active + ",\"audiences\":\"Teacher\"}", false, false, null));
        announcements.Add(("{\"important\":\"true\",\"audiences\":\"Admin\",\"expiresAtUtc\":12}", false, true, null));
        foreach (var item in announcements)
        {
            using (var scope = factory.Services.CreateScope()) { var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
                (await db.Notifications.SingleAsync(x => x.Id == target.Id)).VariablesJson = item.Metadata; await db.SaveChangesAsync(); }
            using var portalJson = (await Read("/api/portal/announcements", admin, HttpStatusCode.OK, "portal-" + count))!;
            var portalIds = portalJson.RootElement.EnumerateArray().Select(x => x.GetProperty("id").GetGuid()).Order().ToArray();
            var expected = (item.Portal ? new[] { localSentinel.Id, target.Id } : new[] { localSentinel.Id }).Order().ToArray();
            if (!portalIds.SequenceEqual(expected)) throw new InvalidOperationException("Portal metadata visibility differs or foreign sentinel leaked.");
            using var platformJson = (await Read("/api/platform/announcements", platform, HttpStatusCode.OK, "platform-" + count))!;
            var rows = platformJson.RootElement.EnumerateArray().ToArray();
            var expectedPlatform = (item.Platform ? new[] { localSentinel.Id, foreignSentinel.Id, target.Id } : new[] { localSentinel.Id, foreignSentinel.Id }).Order().ToArray();
            if (!rows.Select(x => x.GetProperty("id").GetGuid()).Order().SequenceEqual(expectedPlatform) ||
                (item.Platform && rows.Single(x => x.GetProperty("id").GetGuid() == target.Id).GetProperty("expiresAtUtc").GetString() != item.Expiry))
                throw new InvalidOperationException("Platform metadata visibility/expiry differs.");
        }
        foreach (var item in new (string? Token, HttpStatusCode Status)[] { (null, HttpStatusCode.Unauthorized), (teacherToken, HttpStatusCode.Forbidden),
            (studentToken, HttpStatusCode.Forbidden), (other, HttpStatusCode.Forbidden) })
            using (await Read(compensationPath, item.Token, item.Status, "compensation-denied-" + count)) { }
        using (await Read($"/api/academies/{academy}/teachers/{Guid.NewGuid()}/compensation", admin, HttpStatusCode.NotFound, "compensation-missing")) { }
        using (await Read("/api/platform/announcements", admin, HttpStatusCode.Forbidden, "platform-admin-denied")) { }
        using (await Read("/api/portal/announcements", null, HttpStatusCode.Unauthorized, "portal-anonymous")) { }
        // Foreign portal reads are legitimate within their own tenant, not a caller-supplied tenant.
        using (var json = (await Read("/api/portal/announcements", other, HttpStatusCode.OK, "foreign-portal-scoped"))!)
            if (json.RootElement.GetArrayLength() != 1 || json.RootElement[0].GetProperty("id").GetGuid() != foreignSentinel.Id)
                throw new InvalidOperationException("Foreign portal returned another tenant's metadata.");
        if (count != 72) throw new InvalidOperationException("Incomplete metadata matrix: " + count);
        Console.WriteLine($"PASS: METADATASHAPE all {count} real SQL/Identity/HTTP reads, exact response/sentinel/no-write controls; no 429.");
    }
}
