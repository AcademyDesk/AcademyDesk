using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
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
    private static async Task VerifyTrialDurationAsync(QaApiFactory factory, HttpClient client)
    {
        Guid academy, ownerId;
        using (var scope = factory.Services.CreateScope()) academy = await scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>().Academies.Where(x => x.Name == "Synthetic Academy A").Select(x => x.Id).SingleAsync();
        ownerId = await CreateAccessActorAsync(factory, "qa-trial-platform@example.invalid", "PlatformOwner", academy);
        await CreateAccessActorAsync(factory, "qa-trial-role-only@example.invalid", "PlatformOwner", academy);
        using (var scope = factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(); var owner = (await users.FindByIdAsync(ownerId.ToString()))!;
            owner.IsPlatformOwner = true; owner.AcademyId = null; owner.DisplayName = "Synthetic trial owner";
            if (!(await users.UpdateAsync(owner)).Succeeded) throw new InvalidOperationException("Trial owner fixture failed");
        }
        var tokens = new Dictionary<string, string>();
        foreach (var (name, email) in new[] { ("platform", "qa-trial-platform@example.invalid"), ("admin", "qa-admin-a@example.invalid"), ("teacher", "qa-teacher-a@example.invalid"), ("role-only", "qa-trial-role-only@example.invalid") }) tokens[name] = await LoginAsync(client, email, "Synthetic!39Ab");
        client.DefaultRequestHeaders.Authorization = null;
        int count = 0, sequence = 0;
        void Check(bool valid, string message) { if (!valid) throw new InvalidOperationException(message); }
        string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
        async Task<string> Snapshot()
        {
            using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); var identity = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            return JsonSerializer.Serialize(new { academies = await db.Academies.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), settings = await db.PlatformSettings.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), audits = await db.PlatformAuditEntries.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), academyAudits = await db.AuditLogs.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), students = await db.Students.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), notifications = await db.Notifications.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), users = await identity.Users.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), roles = await identity.Roles.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), memberships = await identity.UserRoles.AsNoTracking().OrderBy(x => x.UserId).ThenBy(x => x.RoleId).ToListAsync() });
        }
        async Task Request(string label, HttpMethod method, string url, string? actor, string? body, HttpStatusCode status, string mutation = "none", int? days = null)
        {
            await Task.Delay(650); var before = await Snapshot(); var started = DateTime.UtcNow;
            using var request = new HttpRequestMessage(method, url);
            if (actor is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens[actor]);
            if (body is not null) request.Content = new StringContent(body, Encoding.UTF8, "application/json");
            using var response = await client.SendAsync(request); var text = await response.Content.ReadAsStringAsync(); var ended = DateTime.UtcNow; var after = await Snapshot();
            Console.WriteLine("TRIALDURATION HTTP " + JsonSerializer.Serialize(new { label, actor, body, status = (int)response.StatusCode, response = text, beforeDigest = Hash(before), afterDigest = Hash(after) }));
            RequireFinanceStatus(response, status, label);
            using var oldDocument = JsonDocument.Parse(before); using var savedDocument = JsonDocument.Parse(after); var old = oldDocument.RootElement; var saved = savedDocument.RootElement;
            Guid? addedAudit = null, newAcademy = null; DateTime? expires = null;
            if (mutation == "none") Check(before == after, label + ": captured data changed on read/rejection");
            else
            {
                foreach (var field in old.EnumerateObject())
                {
                    var changed = mutation == "onboard" ? new[] { "academies", "users", "memberships", "audits" } : new[] { "settings", "audits" };
                    if (!changed.Contains(field.Name)) Check(field.Value.GetRawText() == saved.GetProperty(field.Name).GetRawText(), label + ": unrelated captured collection changed");
                    else if (field.Name != "settings")
                    {
                        string Key(JsonElement row) => field.Name == "memberships" ? row.GetProperty("UserId").GetString() + ":" + row.GetProperty("RoleId").GetString() : row.GetProperty("Id").GetString()!;
                        var original = field.Value.EnumerateArray().ToDictionary(Key); var now = saved.GetProperty(field.Name).EnumerateArray().ToDictionary(Key);
                        Check(now.Count == original.Count + 1, label + ": addition count mismatch in " + field.Name);
                        foreach (var row in original) Check(now.TryGetValue(row.Key, out var preserved) && preserved.GetRawText() == row.Value.GetRawText(), label + ": original row changed in " + field.Name);
                    }
                }
                using var outputDocument = JsonDocument.Parse(text); var output = outputDocument.RootElement;
                var auditIds = old.GetProperty("audits").EnumerateArray().Select(x => x.GetProperty("Id").GetGuid()).ToHashSet(); var audit = saved.GetProperty("audits").EnumerateArray().Single(x => !auditIds.Contains(x.GetProperty("Id").GetGuid())); addedAudit = audit.GetProperty("Id").GetGuid();
                Check(audit.GetProperty("ActorUserId").GetGuid() == ownerId && audit.GetProperty("ActorName").GetString() == "Synthetic trial owner", label + ": audit actor mismatch");
                if (mutation == "onboard")
                {
                    var id = output.GetProperty("id").GetGuid(); newAcademy = id;
                    var row = saved.GetProperty("academies").EnumerateArray().Single(x => x.GetProperty("Id").GetGuid() == id); expires = row.GetProperty("SubscriptionEndsAtUtc").GetDateTime();
                    Check(expires >= started.AddDays(days!.Value) && expires <= ended.AddDays(days.Value), label + ": expiry outside independent UTC request window");
                    using var inputDocument = JsonDocument.Parse(body!); var input = inputDocument.RootElement;
                    Check(row.GetProperty("Name").GetString() == input.GetProperty("academyName").GetString() && row.GetProperty("SubscriptionPlan").GetString() == "Trial" && row.GetProperty("SubscriptionStatus").GetString() == "Trial" && row.GetProperty("StudentLimit").GetInt32() == 75 && row.GetProperty("StaffLimit").GetInt32() == 15 && row.GetProperty("IsActive").GetBoolean(), label + ": new tenant identity/trial mismatch");
                    Check(row.GetProperty("CountryCode").GetString() == "IN" && row.GetProperty("TimeZone").GetString() == "Asia/Kolkata" && row.GetProperty("LegalName").ValueKind == JsonValueKind.Null, label + ": optional/default tenant fields mismatch");
                    var modules = JsonSerializer.Deserialize<string[]>(row.GetProperty("EnabledModulesJson").GetString()!);
                    Check(modules!.SequenceEqual(new[] { "Core", "Sales", "Engagement", "Finance", "Certificates", "TeacherClassroom", "AcademicGovernance", "MultiBranch", "AccessGovernance", "FinanceControls" }), label + ": Trial modules changed");
                    var user = saved.GetProperty("users").EnumerateArray().Single(x => x.GetProperty("AcademyId").ValueKind != JsonValueKind.Null && x.GetProperty("AcademyId").GetGuid() == id);
                    Check(user.GetProperty("UserName").GetString() == input.GetProperty("adminUserName").GetString() && user.GetProperty("Email").GetString() == input.GetProperty("adminUserName").GetString() && output.GetProperty("userName").GetString() == user.GetProperty("UserName").GetString(), label + ": new administrator identity mismatch");
                    var role = saved.GetProperty("roles").EnumerateArray().Single(x => x.GetProperty("Name").GetString() == "AcademyAdmin");
                    Check(saved.GetProperty("memberships").EnumerateArray().Any(x => x.GetProperty("UserId").GetGuid() == user.GetProperty("Id").GetGuid() && x.GetProperty("RoleId").GetGuid() == role.GetProperty("Id").GetGuid()), label + ": administrator membership missing");
                    Check(audit.GetProperty("Action").GetString() == "Academy onboarded" && audit.GetProperty("EntityType").GetString() == "Academy" && audit.GetProperty("EntityId").GetGuid() == id, label + ": onboarding audit identity mismatch");
                }
                else
                {
                    var rows = saved.GetProperty("settings").EnumerateArray().ToArray(); Check(rows.Length == 1 && rows[0].GetProperty("DefaultTrialDays").GetInt32() == days && output.GetProperty("defaultTrialDays").GetInt32() == days, label + ": saved duration mismatch");
                    using var inputDocument = JsonDocument.Parse(body!); var input = inputDocument.RootElement;
                    foreach (var (persisted, supplied) in new[] { ("PlatformName", "platformName"), ("SupportEmail", "supportEmail"), ("DefaultCurrency", "defaultCurrency"), ("DataRetentionDays", "dataRetentionDays"), ("MaintenanceMode", "maintenanceMode"), ("StatusMessage", "statusMessage") }) Check(rows[0].GetProperty(persisted).GetRawText() == input.GetProperty(supplied).GetRawText(), label + ": setting persistence mismatch: " + persisted);
                    var oldRows = old.GetProperty("settings").EnumerateArray().ToArray(); if (oldRows.Length > 0) foreach (var field in new[] { "Id", "CreatedAtUtc" }) Check(rows[0].GetProperty(field).GetRawText() == oldRows[0].GetProperty(field).GetRawText(), label + ": settings identity changed");
                    Check(audit.GetProperty("Action").GetString() == "Platform settings updated" && audit.GetProperty("EntityType").GetString() == "PlatformSettings" && audit.GetProperty("EntityId").GetGuid() == rows[0].GetProperty("Id").GetGuid(), label + ": settings audit identity mismatch");
                }
            }
            Console.WriteLine("TRIALDURATION EVIDENCE " + JsonSerializer.Serialize(new { label, actor, status = (int)response.StatusCode, response = text, mutation, days, started, ended, expires, newAcademy, addedAudit, beforeDigest = Hash(before), afterDigest = Hash(after), bodyDigest = Hash(text), exactSnapshotVerified = true }));
            count++; Console.WriteLine("TRIALDURATION CASE " + label + " PASS.");
        }
        string Settings(int days) => JsonSerializer.Serialize(new { platformName = "Synthetic QA Platform", supportEmail = (string?)null, defaultCurrency = "INR", defaultTrialDays = days, dataRetentionDays = 30, maintenanceMode = false, statusMessage = (string?)null });
        string Intake(string? email = null) => JsonSerializer.Serialize(new { academyName = "Synthetic Trial " + ++sequence, adminUserName = email ?? "qa-trial-new-" + sequence + "@example.invalid", password = "Synthetic!39Ab" });
        using (var scope = factory.Services.CreateScope()) Check(!await scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>().PlatformSettings.AnyAsync(), "Fresh fixture unexpectedly has settings");
        await Request("absent-settings-fallback30", HttpMethod.Post, "/api/platform/academies", "platform", Intake(), HttpStatusCode.Created, "onboard", 30);
        foreach (var days in new[] { 14, 30, 45, 1 })
        {
            await Request("save-duration-" + days, HttpMethod.Put, "/api/platform/settings", "platform", Settings(days), HttpStatusCode.OK, "settings", days);
            await Request("new-trial-" + days, HttpMethod.Post, "/api/platform/academies", "platform", Intake(), HttpStatusCode.Created, "onboard", days);
        }
        foreach (var days in new[] { 0, -1 }) await Request("reject-setting-" + days, HttpMethod.Put, "/api/platform/settings", "platform", Settings(days), HttpStatusCode.BadRequest);
        await Request("save-overflow-duration", HttpMethod.Put, "/api/platform/settings", "platform", Settings(int.MaxValue), HttpStatusCode.OK, "settings", int.MaxValue);
        await Request("reject-impossible-expiry", HttpMethod.Post, "/api/platform/academies", "platform", Intake(), HttpStatusCode.BadRequest);
        await Request("restore-duration45", HttpMethod.Put, "/api/platform/settings", "platform", Settings(45), HttpStatusCode.OK, "settings", 45);
        await Request("duplicate-admin-rejected", HttpMethod.Post, "/api/platform/academies", "platform", Intake("qa-trial-new-1@example.invalid"), HttpStatusCode.Conflict);
        foreach (var actor in new string?[] { null, "admin", "teacher", "role-only" }) await Request("onboarding-denied-" + (actor ?? "anonymous"), HttpMethod.Post, "/api/platform/academies", actor, Intake(), actor is null ? HttpStatusCode.Unauthorized : HttpStatusCode.Forbidden);
        await Request("setting-denied-admin", HttpMethod.Put, "/api/platform/settings", "admin", Settings(14), HttpStatusCode.Forbidden);
        using (var scope = factory.Services.CreateScope()) { var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); db.PlatformSettings.Add(new PlatformSettings { DefaultTrialDays = 7, CreatedAtUtc = new(2000, 1, 1), UpdatedAtUtc = new(2000, 1, 2) }); await db.SaveChangesAsync(); }
        await Request("latest-setting-not-stale-duplicate", HttpMethod.Post, "/api/platform/academies", "platform", Intake(), HttpStatusCode.Created, "onboard", 45);
        await Request("owner-list-no-write", HttpMethod.Get, "/api/platform/academies", "platform", null, HttpStatusCode.OK);
        Console.WriteLine($"TRIALDURATION REGRESSION PASS: {count} real Identity/HTTP-SQL cases; UTC duration window, exact tenant/admin/membership/audit additions, existing expiry/settings preserved, denied requests unchanged. Accepted source baseline only; not runtime-baseline/browser/full-critical acceptance.");
    }
}
