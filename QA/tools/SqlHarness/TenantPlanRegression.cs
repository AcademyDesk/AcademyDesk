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
    private static async Task VerifyTenantPlanAsync(QaApiFactory factory, HttpClient client)
    {
        var baseline = Environment.GetEnvironmentVariable("QA_TENANT_PLAN_BASELINE") == "1";
        // Independent frozen oracle of the accepted catalog; do not call its fallback.
        var plans = new Dictionary<string, (int Students, int Staff, string[] Modules)>
        {
            ["Trial"] = (75, 15, ["Core", "Sales", "Engagement", "Finance", "Certificates", "TeacherClassroom", "AcademicGovernance", "MultiBranch", "AccessGovernance", "FinanceControls"]),
            ["Launch"] = (75, 15, ["Core", "TeacherClassroom"]),
            ["Growth"] = (250, 40, ["Core", "TeacherClassroom", "Sales", "Engagement", "Finance", "FinanceControls", "Certificates"]),
            ["Professional"] = (750, 120, ["Core", "TeacherClassroom", "Sales", "Engagement", "Finance", "Certificates", "AcademicGovernance", "MultiBranch", "AccessGovernance", "FinanceControls"]),
            ["Enterprise"] = (5000, 1000, ["Core", "TeacherClassroom", "Sales", "Engagement", "Finance", "Certificates", "AcademicGovernance", "MultiBranch", "AccessGovernance", "FinanceControls", "Integrations", "WhiteLabel"])
        };
        Guid academy, foreign, ownerId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            academy = await db.Academies.Where(x => x.Name == "Synthetic Academy A").Select(x => x.Id).SingleAsync();
            foreign = await db.Academies.Where(x => x.Name == "Synthetic Academy B").Select(x => x.Id).SingleAsync();
        }
        ownerId = await CreateAccessActorAsync(factory, "qa-plan-platform@example.invalid", "PlatformOwner", academy);
        using (var scope = factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var owner = (await users.FindByIdAsync(ownerId.ToString()))!;
            owner.IsPlatformOwner = true; owner.AcademyId = null; owner.DisplayName = "Synthetic plan owner";
            if (!(await users.UpdateAsync(owner)).Succeeded) throw new InvalidOperationException("Plan owner fixture failed.");
        }
        var tokens = new Dictionary<string, string> { ["platform"] = await LoginAsync(client, "qa-plan-platform@example.invalid", "Synthetic!39Ab") };
        foreach (var (name, email) in new[] { ("admin", "qa-admin-a@example.invalid"), ("foreign-admin", "qa-admin-b@example.invalid"), ("teacher", "qa-teacher-a@example.invalid") }) tokens[name] = await LoginAsync(client, email, "Synthetic!39Ab");
        foreach (var role in new[] { "Owner", "Manager", "FinanceUser", "PlatformOwner" })
        {
            var email = "qa-plan-" + role.ToLowerInvariant() + "@example.invalid";
            await CreateAccessActorAsync(factory, email, role, academy);
            tokens[role] = await LoginAsync(client, email, "Synthetic!39Ab");
        }
        client.DefaultRequestHeaders.Authorization = null;
        int count = 0, index = 0;
        var expiry = new DateTime(2005, 6, 7, 8, 9, 10, DateTimeKind.Utc);
        void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
        string Digest(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
        async Task Seed(Guid id, bool active = true)
        {
            using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var row = await db.Academies.SingleAsync(x => x.Id == id);
            row.SubscriptionPlan = "Professional"; row.SubscriptionStatus = "Suspended"; row.SubscriptionEndsAtUtc = new(2002, 3, 4, 5, 6, 7, DateTimeKind.Utc);
            row.StudentLimit = 750; row.StaffLimit = 120; row.EnabledModulesJson = JsonSerializer.Serialize(plans["Professional"].Modules);
            row.LegalName = "QA — preserve legal name"; row.CountryCode = "IN"; row.TimeZone = "Asia/Kolkata"; row.IsActive = active;
            row.CertificateLogoUrl = "/QA-preserved-logo.png"; row.CertificateAccentColor = "#012345"; row.CertificateSignatoryName = "Synthetic preserved signatory"; row.UpdatedAtUtc = new(2000, 1, 2, 3, 4, 5, DateTimeKind.Utc);
            await db.SaveChangesAsync();
        }
        async Task<string> Snapshot()
        {
            using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); var identity = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            return JsonSerializer.Serialize(new
            {
                academies = await db.Academies.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
                platformAudits = await db.PlatformAuditEntries.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
                academyAudits = await db.AuditLogs.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
                profiles = await db.TenantOnboardingProfiles.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
                invoices = await db.PlatformBillingInvoices.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
                branches = await db.Branches.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
                students = await db.Students.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
                notifications = await db.Notifications.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
                users = await identity.Users.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
                roles = await identity.Roles.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
                memberships = await identity.UserRoles.AsNoTracking().OrderBy(x => x.UserId).ThenBy(x => x.RoleId).ToListAsync()
            });
        }
        string Body(string? plan, string? status = "  Active  ", DateTime? until = null, bool omitPlan = false, bool omitStatus = false)
        {
            var body = new Dictionary<string, object?> { ["subscriptionEndsAtUtc"] = until };
            if (!omitPlan) body["subscriptionPlan"] = plan;
            if (!omitStatus) body["subscriptionStatus"] = status;
            return JsonSerializer.Serialize(body);
        }
        async Task Request(string label, string? actor, Guid id, string? body, HttpStatusCode expected, string? expectedPlan = null, DateTime? until = null, bool read = false)
        {
            await Task.Delay(650); var before = await Snapshot();
            var url = read ? "/api/platform/academies" : $"/api/platform/academies/{id}/configuration";
            using var request = new HttpRequestMessage(read ? HttpMethod.Get : HttpMethod.Put, url);
            if (actor is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens[actor]);
            if (body is not null) request.Content = new StringContent(body, Encoding.UTF8, "application/json");
            using var response = await client.SendAsync(request); var text = await response.Content.ReadAsStringAsync(); var after = await Snapshot();
            Console.WriteLine("TENANTPLAN HTTP " + JsonSerializer.Serialize(new { label, actor, id, requestBody = body, status = (int)response.StatusCode, response = text, beforeDigest = Digest(before), afterDigest = Digest(after) }));
            RequireFinanceStatus(response, expected, label);
            using var oldDocument = JsonDocument.Parse(before); using var newDocument = JsonDocument.Parse(after);
            var old = oldDocument.RootElement; var now = newDocument.RootElement; Guid? addedAudit = null;
            if (expectedPlan is null) Check(before == after, label + ": rejected/read request changed captured data");
            else
            {
                var expectedDefinition = plans[expectedPlan]; var expectedModules = JsonSerializer.Serialize(expectedDefinition.Modules);
                var oldRows = old.GetProperty("academies").EnumerateArray().ToDictionary(x => x.GetProperty("Id").GetGuid());
                var savedRows = now.GetProperty("academies").EnumerateArray().ToDictionary(x => x.GetProperty("Id").GetGuid());
                Check(oldRows.Count == savedRows.Count, label + ": academy count changed");
                foreach (var row in oldRows.Where(x => x.Key != id)) Check(savedRows.TryGetValue(row.Key, out var saved) && saved.GetRawText() == row.Value.GetRawText(), label + ": other academy changed");
                var savedRow = savedRows[id];
                Check(savedRow.GetProperty("SubscriptionPlan").GetString() == expectedPlan && savedRow.GetProperty("SubscriptionStatus").GetString() == "Active", label + ": plan/status mismatch");
                Check(savedRow.GetProperty("StudentLimit").GetInt32() == expectedDefinition.Students && savedRow.GetProperty("StaffLimit").GetInt32() == expectedDefinition.Staff && savedRow.GetProperty("EnabledModulesJson").GetString() == expectedModules, label + ": limits/modules mismatch");
                Check(until.HasValue ? savedRow.GetProperty("SubscriptionEndsAtUtc").GetDateTime().Ticks == until.Value.Ticks : savedRow.GetProperty("SubscriptionEndsAtUtc").ValueKind == JsonValueKind.Null, label + ": nullable expiry mismatch");
                var mutable = new[] { "SubscriptionPlan", "SubscriptionStatus", "SubscriptionEndsAtUtc", "StudentLimit", "StaffLimit", "EnabledModulesJson" };
                foreach (var field in oldRows[id].EnumerateObject().Where(x => !mutable.Contains(x.Name))) Check(field.Value.GetRawText() == savedRow.GetProperty(field.Name).GetRawText(), label + ": unrelated academy field changed: " + field.Name);
                using var result = JsonDocument.Parse(text); var output = result.RootElement;
                Check(output.EnumerateObject().Count() == 7 && output.GetProperty("id").GetGuid() == id && output.GetProperty("subscriptionPlan").GetString() == expectedPlan && output.GetProperty("subscriptionStatus").GetString() == "Active", label + ": response shape/identity mismatch");
                Check(output.GetProperty("studentLimit").GetInt32() == expectedDefinition.Students && output.GetProperty("staffLimit").GetInt32() == expectedDefinition.Staff && output.GetProperty("enabledModulesJson").GetString() == expectedModules, label + ": response limits/modules mismatch");
                Check(until.HasValue ? output.GetProperty("subscriptionEndsAtUtc").GetDateTime().Ticks == until.Value.Ticks : output.GetProperty("subscriptionEndsAtUtc").ValueKind == JsonValueKind.Null, label + ": response expiry mismatch");
                var oldAudits = old.GetProperty("platformAudits").EnumerateArray().ToDictionary(x => x.GetProperty("Id").GetGuid());
                var savedAudits = now.GetProperty("platformAudits").EnumerateArray().ToDictionary(x => x.GetProperty("Id").GetGuid());
                Check(savedAudits.Count == oldAudits.Count + 1, label + ": success audit count mismatch");
                foreach (var row in oldAudits) Check(savedAudits.TryGetValue(row.Key, out var saved) && saved.GetRawText() == row.Value.GetRawText(), label + ": existing audit changed");
                var audit = savedAudits.Single(x => !oldAudits.ContainsKey(x.Key)).Value; addedAudit = audit.GetProperty("Id").GetGuid();
                Check(audit.GetProperty("ActorUserId").GetGuid() == ownerId && audit.GetProperty("ActorName").GetString() == "Synthetic plan owner" && audit.GetProperty("Action").GetString() == "Tenant configuration updated" && audit.GetProperty("EntityType").GetString() == "Academy" && audit.GetProperty("EntityId").GetGuid() == id, label + ": audit identity mismatch");
                using var metadata = JsonDocument.Parse(audit.GetProperty("MetadataJson").GetString()!); var meta = metadata.RootElement;
                Check(meta.EnumerateObject().Count() == 4 && meta.GetProperty("SubscriptionPlan").GetString() == expectedPlan && meta.GetProperty("SubscriptionStatus").GetString() == "Active" && meta.GetProperty("StudentLimit").GetInt32() == expectedDefinition.Students && meta.GetProperty("StaffLimit").GetInt32() == expectedDefinition.Staff, label + ": audit metadata mismatch");
                foreach (var field in old.EnumerateObject().Where(x => x.Name is not "academies" and not "platformAudits")) Check(field.Value.GetRawText() == now.GetProperty(field.Name).GetRawText(), label + ": unrelated captured collection changed");
            }
            if (read)
            {
                using var result = JsonDocument.Parse(text); var rows = result.RootElement.EnumerateArray().ToDictionary(x => x.GetProperty("id").GetGuid());
                Check(rows.Count == 2, label + ": owner list tenant count mismatch");
                foreach (var row in now.GetProperty("academies").EnumerateArray())
                {
                    var output = rows[row.GetProperty("Id").GetGuid()];
                    Check(output.GetProperty("subscriptionPlan").GetString() == row.GetProperty("SubscriptionPlan").GetString() && output.GetProperty("subscriptionStatus").GetString() == row.GetProperty("SubscriptionStatus").GetString() && output.GetProperty("studentLimit").GetInt32() == row.GetProperty("StudentLimit").GetInt32() && output.GetProperty("staffLimit").GetInt32() == row.GetProperty("StaffLimit").GetInt32() && output.GetProperty("enabledModulesJson").GetString() == row.GetProperty("EnabledModulesJson").GetString(), label + ": list configuration readback mismatch");
                }
            }
            Console.WriteLine("TENANTPLAN EVIDENCE " + JsonSerializer.Serialize(new { label, actor, id, requestBody = body, status = (int)response.StatusCode, response = text, expectedPlan, beforeDigest = Digest(before), afterDigest = Digest(after), bodyDigest = Digest(text), addedAudit, exactSnapshotVerified = true }));
            if (!baseline) { count++; Console.WriteLine("TENANTPLAN CASE " + label + " PASS."); }
        }
        if (baseline)
        {
            foreach (var name in new[] { "Professionl", "Professional ", "NotAPlan" }) { await Seed(academy); await Request("baseline-fallback-" + ++index, "platform", academy, Body(name, until: expiry), HttpStatusCode.OK, "Launch", expiry); }
            Console.WriteLine("TENANTPLAN BASELINE: three invalid nonblank plan requests replaced Professional750/120/full modules with Launch75/15/Core+TeacherClassroom and emitted success audit; exact unchanged unrelated data verified. No passing repair cases."); return;
        }
        foreach (var name in new string?[] { "Professionl", "Professional ", " Professional", " ProFessional ", "Unknown", "LaunchX", "Trial\n", "Growth\t", "Enterprise\u00a0", "Professional,Launch", "", " ", "\t", null })
        { await Seed(academy); await Request("invalid-plan-" + ++index, "platform", academy, Body(name, until: expiry), HttpStatusCode.BadRequest); }
        foreach (var name in plans.Keys)
        {
            await Seed(academy, active: name != "Trial"); await Request("valid-canonical-" + name, "platform", academy, Body(name, until: expiry), HttpStatusCode.OK, name, expiry);
            await Seed(academy); await Request("valid-case-" + name, "platform", academy, Body(name.ToLowerInvariant()), HttpStatusCode.OK, name);
        }
        await Request("repeat-enterprise", "platform", academy, Body("ENTERPRISE"), HttpStatusCode.OK, "Enterprise");
        foreach (var status in new string?[] { null, "", " ", "\t" }) { await Seed(academy); await Request("invalid-status-" + ++index, "platform", academy, Body("Professional", status, expiry), HttpStatusCode.BadRequest); }
        await Seed(academy);
        await Request("omitted-plan", "platform", academy, Body(null, until: expiry, omitPlan: true), HttpStatusCode.BadRequest);
        await Request("omitted-status", "platform", academy, Body("Professional", until: expiry, omitStatus: true), HttpStatusCode.BadRequest);
        foreach (var body in new[] { "{bad", "null", "", "{\"subscriptionPlan\":123,\"subscriptionStatus\":\"Active\"}", "{\"subscriptionPlan\":[],\"subscriptionStatus\":\"Active\"}", "{\"subscriptionPlan\":\"Professional\",\"subscriptionStatus\":\"Active\",\"subscriptionEndsAtUtc\":\"invalid\"}" })
            await Request("invalid-json-" + ++index, "platform", academy, body, HttpStatusCode.BadRequest);
        await Request("missing-academy", "platform", Guid.NewGuid(), Body("Professional"), HttpStatusCode.NotFound);
        foreach (var actor in new string?[] { null, "admin", "foreign-admin", "teacher", "Owner", "Manager", "FinanceUser", "PlatformOwner" })
            await Request("denied-" + (actor ?? "anonymous"), actor, academy, Body("Enterprise"), actor is null ? HttpStatusCode.Unauthorized : HttpStatusCode.Forbidden);
        await Seed(foreign); await Request("owner-other-academy", "platform", foreign, Body("Growth", until: expiry), HttpStatusCode.OK, "Growth", expiry);
        await Request("owner-list-readback", "platform", academy, null, HttpStatusCode.OK, read: true);
        Console.WriteLine($"TENANTPLAN REGRESSION PASS: {count} real Identity/HTTP-SQL cases; independent exact catalog limits/modules, optional expiry, case compatibility, rejection no-writes and actor success audits. Not browser/critical/concurrency/full-policy acceptance.");
    }
}
