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
    // Scoped diagnostic: a reproduced policy gap is reported, not labelled a pass.
    private static async Task VerifyRoleRevocationAsync(QaApiFactory factory, HttpClient client)
    {
        const string password = "Synthetic!39Ab", staffEmail = "qa-role-staff@example.invalid";
        Guid academyA, academyB, staffId, operationsId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var academy = await db.Academies.SingleAsync(x => x.Name == "Synthetic Academy A");
            academyA = academy.Id;
            academyB = await db.Academies.Where(x => x.Name == "Synthetic Academy B").Select(x => x.Id).SingleAsync();
            academy.EnabledModulesJson = "[\"Core\",\"Finance\",\"FinanceControls\",\"AccessGovernance\"]";
            var student = await db.Students.Where(x => x.AcademyId == academyA).Select(x => x.Id).SingleAsync();
            db.Invoices.Add(new Invoice { AcademyId = academyA, StudentId = student, InvoiceNumber = "QA-ROLE-PRIVATE", TotalAmount = 1234m });
            await db.SaveChangesAsync();
            var roles = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
            foreach (var role in new[] {
                new ApplicationRole { Name = "Operations", IsSystemRole = true },
                new ApplicationRole { Name = "Sales", IsSystemRole = true },
                new ApplicationRole { Name = "QA-Finance", AcademyId = academyA, PermissionsJson = "[\"finance.manage\"]" },
                new ApplicationRole { Name = "QA-Messages", AcademyId = academyA, PermissionsJson = "[\"communications.manage\"]" }
            }) if (!(await roles.CreateAsync(role)).Succeeded) throw new InvalidOperationException("Owned role fixture creation failed.");
            operationsId = (await roles.FindByNameAsync("Operations"))!.Id;
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var staff = new ApplicationUser { UserName = staffEmail, Email = staffEmail, EmailConfirmed = true, DisplayName = "Synthetic Role Staff", AcademyId = academyA, IsActive = true };
            if (!(await users.CreateAsync(staff, password)).Succeeded || !(await users.AddToRolesAsync(staff, ["Sales", "QA-Finance", "QA-Messages"])).Succeeded)
                throw new InvalidOperationException("Owned staff fixture creation failed.");
            staffId = staff.Id;
        }
        var requests = 0; var gaps = 0; var unchanged = 0; var observations = new List<object>();
        void Observe(string id, bool accepted, object detail, bool policyGate = false)
        {
            if (!accepted && !policyGate) throw new InvalidOperationException("Diagnostic control failed: " + id);
            if (!accepted) gaps++;
            var item = new { id, accepted, policyGate, detail };
            observations.Add(item); Console.WriteLine("ROLESESSION CASE " + JsonSerializer.Serialize(item));
        }
        async Task<string> Snapshot()
        {
            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var identity = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            return JsonSerializer.Serialize(new {
                users = await identity.Users.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
                roles = await identity.Roles.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
                memberships = await identity.UserRoles.AsNoTracking().OrderBy(x => x.UserId).ThenBy(x => x.RoleId).ToListAsync(),
                grants = await identity.AccessGrants.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
                invoices = await db.Invoices.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
                audits = await db.AuditLogs.AsNoTracking().OrderBy(x => x.Id).ToListAsync()
            });
        }
        async Task<HttpResponseMessage> Send(string id, string method, string path, int expected, string? bearer = null, object? body = null, bool readOnly = false, bool policyGate = false)
        {
            var before = readOnly ? await Snapshot() : null;
            using var request = new HttpRequestMessage(new HttpMethod(method), path);
            if (bearer is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
            if (body is not null) request.Content = JsonContent.Create(body);
            requests++; var response = await client.SendAsync(request);
            Observe(id, (int)response.StatusCode == expected, new { method, path, actual = (int)response.StatusCode, expected }, policyGate);
            if (readOnly) { if (before != await Snapshot()) throw new InvalidOperationException("Read/denial changed snapshot: " + id); unchanged++; }
            if (!response.IsSuccessStatusCode && (await response.Content.ReadAsStringAsync()).Contains("QA-ROLE-PRIVATE", StringComparison.Ordinal)) throw new InvalidOperationException("Denied response leaked fixture invoice.");
            return response;
        }
        async Task<(string access, string refresh)> Login(string email, string id)
        {
            using var response = await Send(id, "POST", "/api/auth/login", 200, body: new { email, password });
            using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            return (payload.RootElement.GetProperty("accessToken").GetString()!, payload.RootElement.GetProperty("refreshToken").GetString()!);
        }
        async Task<string[]> Memberships()
        {
            using var scope = factory.Services.CreateScope();
            var identity = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            return await (from member in identity.UserRoles.AsNoTracking() join role in identity.Roles.AsNoTracking() on member.RoleId equals role.Id where member.UserId == staffId orderby role.Name select role.Name!).ToArrayAsync();
        }
        async Task<string?> Stamp()
        {
            using var scope = factory.Services.CreateScope();
            return await scope.ServiceProvider.GetRequiredService<IdentityDbContext>().Users.Where(x => x.Id == staffId).Select(x => x.SecurityStamp).SingleAsync();
        }
        async Task RequireInvoice(string id, string token, int expected, bool gate = false)
        {
            using var response = await Send(id, "GET", $"/api/academies/{academyA}/invoices", expected, token, readOnly: true, policyGate: gate);
            if (response.IsSuccessStatusCode)
            {
                using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                if (payload.RootElement.GetArrayLength() != 1 || payload.RootElement[0].GetProperty("invoiceNumber").GetString() != "QA-ROLE-PRIVATE" || payload.RootElement[0].GetProperty("totalAmount").GetDecimal() != 1234m)
                    throw new InvalidOperationException("Positive finance fixture control invalid.");
            }
        }
        var adminA = await Login("qa-admin-a@example.invalid", "admin-a-login");
        var adminB = await Login("qa-admin-b@example.invalid", "admin-b-login");
        var teacher = await Login("qa-teacher-a@example.invalid", "teacher-login");
        var original = await Login(staffEmail, "custom-staff-login");
        var finance = $"/api/academies/{academyA}/invoices";
        var assignment = $"/api/academies/{academyA}/staff/{staffId}/role";
        await RequireInvoice("custom-finance-positive-payload", original.access, 200);
        using (await Send("custom-staff-cannot-change-own-role", "PATCH", assignment, 403, original.access, new { role = "Operations" }, true)) { }
        using (await Send("foreign-admin-cannot-change-role", "PATCH", assignment, 403, adminB.access, new { role = "Operations" }, true)) { }
        using (await Send("teacher-cannot-change-role", "PATCH", assignment, 403, teacher.access, new { role = "Operations" }, true)) { }
        using (await Send("invalid-role-no-write", "PATCH", assignment, 400, adminA.access, new { role = "Unknown" }, true)) { }
        using (await Send("missing-staff-no-write", "PATCH", $"/api/academies/{academyA}/staff/{Guid.NewGuid()}/role", 404, adminA.access, new { role = "Operations" }, true)) { }
        var stampBefore = await Stamp();
        string[] reported;
        using (var replace = await Send("standard-operations-replacement", "PATCH", assignment, 200, adminA.access, new { role = "Operations" }))
        {
            using var body = JsonDocument.Parse(await replace.Content.ReadAsStringAsync());
            reported = body.RootElement.GetProperty("roles").EnumerateArray().Select(x => x.GetString()!).OrderBy(x => x).ToArray();
        }
        var actual = await Memberships();
        Observe("role-response-matches-persisted-memberships", reported.SequenceEqual(actual), new { reported, actual }, true);
        Observe("standard-replacement-stamp-rotates", stampBefore != await Stamp(), new { rotated = stampBefore != await Stamp() });
        await RequireInvoice("old-access-rejected-after-role-stamp", original.access, 401);
        using (await Send("old-refresh-rejected-after-role-stamp", "POST", "/api/auth/refresh", 401, body: new { refreshToken = original.refresh }, readOnly: true)) { }
        var current = await Login(staffEmail, "post-replacement-login");
        await RequireInvoice("operations-only-expected-finance-denial", current.access, 403, gate: true);
        using (await Send("operations-positive-batches", "GET", $"/api/academies/{academyA}/batches", 200, current.access, readOnly: true)) { }
        using (await Send("cross-tenant-finance-denied", "GET", $"/api/academies/{academyB}/invoices", 403, current.access, readOnly: true)) { }
        var assignStamp = await Stamp();
        using (await Send("custom-role-replacement-control", "PUT", $"/api/academies/{academyA}/roles/staff/{staffId}/assignment", 200, adminA.access, new { roleId = operationsId })) { }
        Observe("custom-assignment-exact-operations-membership", (await Memberships()).SequenceEqual(new[] { "Operations" }), new { roles = await Memberships() });
        Observe("custom-assignment-stamp-unchanged-observation", assignStamp == await Stamp(), new { unchanged = assignStamp == await Stamp() });
        await RequireInvoice("existing-access-live-role-removal-denies-finance", current.access, 403);
        using (await Send("existing-access-live-operations-still-allowed", "GET", $"/api/academies/{academyA}/batches", 200, current.access, readOnly: true)) { }
        Guid grantId;
        using (var grant = await Send("grant-finance-permission", "POST", $"/api/academies/{academyA}/access-grants", 200, adminA.access, new { userId = staffId, permissions = new[] { "finance.manage" }, isPermanent = true }))
        {
            using var body = JsonDocument.Parse(await grant.Content.ReadAsStringAsync()); grantId = body.RootElement.GetProperty("id").GetGuid();
        }
        await RequireInvoice("existing-access-live-grant-positive", current.access, 200);
        using (await Send("revoke-finance-grant", "PATCH", $"/api/academies/{academyA}/access-grants/{grantId}/revoke", 204, adminA.access)) { }
        await RequireInvoice("existing-access-live-grant-revocation-denied", current.access, 403);
        using (await Send("revoke-again-idempotent-state", "PATCH", $"/api/academies/{academyA}/access-grants/{grantId}/revoke", 204, adminA.access)) { }
        using (var scope = factory.Services.CreateScope())
        {
            var identity = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            var grant = await identity.AccessGrants.SingleAsync(x => x.Id == grantId);
            var admin = await identity.Users.SingleAsync(x => x.Email == "qa-admin-a@example.invalid");
            Observe("grant-revocation-sql-actor", grant.RevokedAtUtc is not null && grant.RevokedByUserId == admin.Id, new { attributed = grant.RevokedByUserId == admin.Id });
        }
        await RequireInvoice("unrelated-admin-finance-still-allowed", adminA.access, 200);
        using (await Send("offboard-staff", "POST", $"/api/academies/{academyA}/staff/{staffId}/offboard", 200, adminA.access)) { }
        await RequireInvoice("offboard-existing-access-denied", current.access, 403);
        using (await Send("offboard-refresh-denied", "POST", "/api/auth/refresh", 401, body: new { refreshToken = current.refresh }, readOnly: true)) { }
        using (await Send("offboard-login-denied", "POST", "/api/auth/login", 401, body: new { email = staffEmail, password })) { }
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var identity = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            var staff = await identity.Users.SingleAsync(x => x.Id == staffId);
            var audit = await db.AuditLogs.Where(x => x.AcademyId == academyA).OrderBy(x => x.Id).ToListAsync();
            // Role replacement, custom assignment, grant create, revoke+repeat and offboard.
            Observe("six-attributed-success-audits", audit.Count == 6 && audit.All(x => x.ActorUserId == identity.Users.Single(x => x.Email == "qa-admin-a@example.invalid").Id), new { count = audit.Count });
            Observe("final-fixture-state", !staff.IsActive && staff.LockoutEnd == DateTimeOffset.MaxValue && (await Memberships()).SequenceEqual(new[] { "Operations" }) && await db.Invoices.CountAsync() == 1, new { inactive = !staff.IsActive, roles = await Memberships(), invoiceCount = await db.Invoices.CountAsync() });
        }
        Console.WriteLine("ROLESESSION SUMMARY " + JsonSerializer.Serialize(new { requests, observations = observations.Count, acceptedControls = observations.Count - gaps, policyGaps = gaps, unchangedSnapshots = unchanged, diagnosticOnly = true, nativeIdentitySql = true }));
    }
}
