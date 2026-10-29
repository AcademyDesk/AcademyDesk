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
    private static async Task VerifyMeetingReadinessAsync(QaApiFactory factory, HttpClient client, QaRunManifest manifest)
    {
        Guid academy, foreign;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            academy = await db.Academies.Where(x => x.Name == "Synthetic Academy A").Select(x => x.Id).SingleAsync();
            foreign = await db.Academies.Where(x => x.Name == "Synthetic Academy B").Select(x => x.Id).SingleAsync();
            foreach (var a in await db.Academies.ToListAsync()) a.EnabledModulesJson = "[\"Core\",\"Engagement\"]";
            foreach (var id in new[] { academy, foreign }) db.CommunicationChannels.Add(new CommunicationChannel { AcademyId = id, Channel = "Meeting", Provider = "GoogleWorkspace", Status = "Configured", HasSecureConnection = true, ExternalAccountReference = "synthetic-private-reference", SenderAddress = "private@example.invalid" });
            await db.SaveChangesAsync();
        }
        var count = 0;
        var checks = new List<object>();
        async Task<string> Snapshot()
        {
            using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            return JsonSerializer.Serialize(new { channels = await db.CommunicationChannels.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), audits = await db.AuditLogs.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), notifications = await db.Notifications.AsNoTracking().OrderBy(x => x.Id).ToListAsync() });
        }
        async Task Check(string label, string? token, Guid target, HttpStatusCode status)
        {
            await Task.Delay(700); var before = await Snapshot();
            using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/academies/{target}/meetings/capabilities");
            if (token is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            using var response = await client.SendAsync(request);
            if (response.StatusCode != status) throw new InvalidOperationException($"MEETING {label}: expected {(int)status}, got {(int)response.StatusCode}");
            var body = await response.Content.ReadAsStringAsync();
            if (status == HttpStatusCode.OK)
            {
                using var json = JsonDocument.Parse(body); var root = json.RootElement;
                if (root.GetProperty("contractVersion").GetString() != "meeting-host/0.1" || root.GetProperty("asOfUtc").GetDateTimeOffset() > DateTimeOffset.UtcNow.AddMinutes(1)) throw new InvalidOperationException("Meeting readiness contract/clock mismatch");
                var providers = root.GetProperty("providers").EnumerateArray().ToArray();
                if (!providers.Select(x => x.GetProperty("provider").GetString()).SequenceEqual(new[] { "GoogleWorkspace", "Zoom", "Microsoft365" })) throw new InvalidOperationException("Unexpected provider allowlist");
                foreach (var p in providers) if (p.GetProperty("implemented").GetBoolean() || p.GetProperty("available").GetBoolean() || p.GetProperty("reasonCode").GetString() != "IntegrationUnavailable" || p.GetProperty("supportedSettings").GetArrayLength() != 0 || p.GetProperty("operations").EnumerateObject().Count() != 5 || p.GetProperty("operations").EnumerateObject().Any(x => x.Value.GetBoolean())) throw new InvalidOperationException("Unqualified meeting capability enabled");
                if (response.Headers.CacheControl?.NoStore != true || body.Contains("private", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Readiness cache/privacy mismatch");
            }
            else if (body.Contains("IntegrationUnavailable", StringComparison.Ordinal) || body.Contains("private", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Denied response leaked meeting details");
            if (await Snapshot() != before) throw new InvalidOperationException("Readiness GET changed channel/audit/notification rows");
            checks.Add(new { label, status = (int)status, snapshotsUnchanged = true });
            count++; Console.WriteLine($"MEETING READINESS CASE {label} PASS status={(int)status}; SQL snapshots unchanged.");
        }
        client.DefaultRequestHeaders.Authorization = null;
        var admin = await LoginAsync(client, "qa-admin-a@example.invalid", "Synthetic!39Ab");
        var other = await LoginAsync(client, "qa-admin-b@example.invalid", "Synthetic!39Ab");
        await Check("admin-owned-configured-marker", admin, academy, HttpStatusCode.OK);
        await Check("foreign-route", admin, foreign, HttpStatusCode.Forbidden);
        await Check("foreign-actor", other, academy, HttpStatusCode.Forbidden);
        await Check("anonymous", null, academy, HttpStatusCode.Unauthorized);
        await Check("malformed-token", "invalid-ticket", academy, HttpStatusCode.Unauthorized);
        foreach (var role in new[] { "Owner", "Teacher", "Student", "Guardian", "Manager", "FinanceUser", "QA-MeetingGrant", "PlatformOwner" })
        {
            var email = "qa-meeting-" + role.ToLowerInvariant() + "@example.invalid";
            var userId = await CreateAccessActorAsync(factory, email, role, academy, permissions: role == "QA-MeetingGrant" ? "[\"communications.manage\"]" : "[]");
            if (role == "PlatformOwner") { using var s = factory.Services.CreateScope(); var users = s.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(); var u = (await users.FindByIdAsync(userId.ToString()))!; u.IsPlatformOwner = true; if (!(await users.UpdateAsync(u)).Succeeded) throw new InvalidOperationException("Platform fixture failed"); }
            var token = await LoginAsync(client, email, "Synthetic!39Ab");
            await Check(role + "-owned", token, academy, role == "Owner" ? HttpStatusCode.OK : HttpStatusCode.Forbidden);
            if (role == "PlatformOwner") await Check("platform-foreign-bypass-denied", token, foreign, HttpStatusCode.Forbidden);
            if (role == "Owner")
            {
                using (var s = factory.Services.CreateScope()) { var users = s.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(); var u = (await users.FindByIdAsync(userId.ToString()))!; if (!(await users.RemoveFromRoleAsync(u, "Owner")).Succeeded) throw new InvalidOperationException("Role revoke failed"); }
                await Check("owner-role-revoked-old-ticket", token, academy, HttpStatusCode.Forbidden);
            }
        }
        foreach (var mode in new[] { "module-off", "malformed-modules", "academy-inactive" })
        {
            using (var s = factory.Services.CreateScope()) { var db = s.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); var a = await db.Academies.SingleAsync(x => x.Id == academy); a.EnabledModulesJson = mode == "malformed-modules" ? "invalid-json" : mode == "module-off" ? "[\"Core\"]" : "[\"Engagement\"]"; a.IsActive = mode != "academy-inactive"; await db.SaveChangesAsync(); }
            await Check(mode, admin, academy, HttpStatusCode.Forbidden);
        }
        using (var s = factory.Services.CreateScope()) { var db = s.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); var a = await db.Academies.SingleAsync(x => x.Id == academy); a.EnabledModulesJson = "[\"Engagement\"]"; a.IsActive = true; var c = await db.CommunicationChannels.SingleAsync(x => x.AcademyId == academy); c.Provider = "https://untrusted.invalid"; await db.SaveChangesAsync(); }
        await Check("arbitrary-provider-still-unavailable", admin, academy, HttpStatusCode.OK);
        using (var s = factory.Services.CreateScope()) { var users = s.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(); var u = (await users.FindByEmailAsync("qa-admin-a@example.invalid"))!; u.AcademyId = foreign; if (!(await users.UpdateAsync(u)).Succeeded) throw new InvalidOperationException("Tenant change failed"); }
        await Check("server-tenant-changed-old-ticket", admin, academy, HttpStatusCode.Forbidden);
        using (var s = factory.Services.CreateScope()) { var users = s.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(); var u = (await users.FindByEmailAsync("qa-admin-a@example.invalid"))!; u.AcademyId = academy; u.IsActive = false; if (!(await users.UpdateAsync(u)).Succeeded) throw new InvalidOperationException("Deactivate failed"); }
        await Check("inactive-user-old-ticket", admin, academy, HttpStatusCode.Forbidden);
        if (count != 21) throw new InvalidOperationException($"Meeting readiness case count {count}");
        string database;
        using (var s = factory.Services.CreateScope()) database = s.ServiceProvider.GetRequiredService<AcademyDeskDbContext>().Database.GetDbConnection().Database;
        if (!database.StartsWith("AcademyDesk_QA_", StringComparison.Ordinal) || !Guid.TryParseExact(database["AcademyDesk_QA_".Length..], "N", out var run)) throw new InvalidOperationException("Unexpected meeting receipt target");
        var evidence = Path.Combine("QA", "EVIDENCE", "meeting-readiness-sql-" + run.ToString("N")); Directory.CreateDirectory(evidence);
        var sources = new[] { "apps/api/Controllers/MeetingsController.cs", "apps/api/Infrastructure/Meetings/MeetingReadinessService.cs", "apps/api/Security/SubscriptionPlanCatalog.cs", "apps/api/Program.cs", "QA/tools/SqlHarness/MeetingReadinessRegression.cs" }.ToDictionary(path => path, path => Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(path))));
        await File.WriteAllTextAsync(Path.Combine(evidence, "result.json"), JsonSerializer.Serialize(new { runId = run, database, checks, sources, transport = "Real Identity bearer HTTP and run-owned SQL; not provider/device/release acceptance" }, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine("MEETING READINESS PASS: 21 real HTTP/Identity/SQL cases; no 429, writes, credentials or provider dispatch.");
        await VerifyMeetingDraftPreviewAsync(factory, client);
        await VerifyMeetingReceiptsAsync(factory, client, manifest);
        await VerifyMeetingOperationsAsync(factory, manifest);
        await VerifyMeetingOAuthAttemptsAsync(factory, manifest);
        await VerifyMeetingOAuthBrowserAsync(factory, manifest);
        await VerifyMeetingOAuthResumeAsync(factory, manifest);
        await VerifyMeetingOAuthDurabilityAsync(factory, manifest);
        await VerifyMeetingOAuthAccountsAsync(factory, manifest);
        await VerifyMeetingAccountReviewsAsync(factory, manifest);
        await VerifyMeetingRefreshesAsync(factory, manifest);
        await VerifyMeetingRevocationsAsync(factory, manifest);
        await VerifyMeetingManualRevocationsAsync(factory, manifest);
        await VerifyMeetingUnknownRefreshesAsync(factory, manifest);
        await VerifyMeetingAccountContinuityAsync(factory, manifest);
        await VerifyMeetingAccountPoliciesAsync(factory, manifest);
        await VerifyMeetingGoogleIdentityAsync(factory, manifest);
        await VerifyMeetingGoogleCalendarAsync(factory, manifest);
        await VerifyMeetingMicrosoftIdentityAsync(factory, manifest);
        await VerifyMeetingTeamsLicenseAsync(factory, manifest);
        await VerifyMeetingZoomAccountAsync(factory, manifest);
        await VerifyMeetingZoomRefreshAsync(factory, manifest);
        await VerifyMeetingZoomRecoveryAsync(factory, manifest);
        await VerifyMeetingGoogleRevocationAsync(factory, manifest);
        await VerifyMeetingProtectedKeysAsync(factory, manifest);
        await VerifyMeetingAdmissionAsync(factory, client, manifest);
    }
}
