using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Domain.Identity;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

internal static partial class SqlHarnessEntryPoint
{
    private static async Task VerifyFinanceLookupsAsync(QaApiFactory factory, HttpClient client)
    {
        Guid academyA, academyB, studentA, studentB;
        string originalModules;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var academy = await db.Academies.SingleAsync(x => x.Name == "Synthetic Academy A");
            academyA = academy.Id; originalModules = academy.EnabledModulesJson;
            academyB = await db.Academies.Where(x => x.Name == "Synthetic Academy B").Select(x => x.Id).SingleAsync();
            studentA = await db.Students.Where(x => x.AcademyId == academyA).Select(x => x.Id).SingleAsync();
            studentB = await db.Students.Where(x => x.AcademyId == academyB).Select(x => x.Id).SingleAsync();
            var own = await db.Students.SingleAsync(x => x.Id == studentA);
            own.Email = "qa-billing@example.invalid"; own.Phone = "QA-BILLING";
            own.AdminNotes = "PRIVATE-ADMIN-MARKER"; own.MedicalOrAccessibilityNotes = "PRIVATE-MEDICAL-MARKER";
            own.DateOfBirth = new DateOnly(2000, 1, 1); own.AddressLine1 = "PRIVATE-ADDRESS-MARKER";
            db.Students.AddRange(new Student { AcademyId = academyA, FirstName = "Zed", LastName = "Alpha", IsActive = false },
                new Student { AcademyId = academyA, FirstName = "Amy", LastName = "Alpha", Email = "qa-amy@example.invalid" });
            await db.SaveChangesAsync();
        }
        var root = $"/api/academies/{academyA}";
        var lookup = root + "/invoices/student-options";
        var count = 0;
        // Each assertion uses actual auth/filter/controller/SQL; GETs and rejections
        // must leave captured people/Identity/finance/notification/audit state unchanged.
        async Task<string> Read(string label, string? token, string url, HttpStatusCode expected, Guid? expectedAcademy = null)
        {
            await Task.Delay(650); // Keep production limiter unchanged.
            client.DefaultRequestHeaders.Authorization = token is null ? null : new AuthenticationHeaderValue("Bearer", token);
            var before = await PeopleStateAsync(factory);
            using var response = await client.GetAsync(url);
            RequireFinanceStatus(response, expected, "lookup " + label);
            var body = await response.Content.ReadAsStringAsync();
            if (before != await PeopleStateAsync(factory)) throw new InvalidOperationException("Lookup/read changed captured state: " + label);
            if (expectedAcademy.HasValue) await VerifyLookupPayloadAsync(factory, body, expectedAcademy.Value);
            Console.WriteLine($"LOOKUP CASE {label} PASS: HTTP={(int)expected}; captured state unchanged{(expectedAcademy.HasValue ? "; exact five-field tenant billing projection/order/nulls" : "")}.");
            count++; return body;
        }
        var tokens = new Dictionary<string, string>();
        (string Role, bool Allowed, string Permissions)[] actors = [
            ("AcademyAdmin", true, "[]"), ("Owner", true, "[]"), ("FinanceUser", true, "[]"),
            ("QA-FinanceOnly", true, "[\"finance.manage\"]"), ("Teacher", false, "[]"),
            ("Student", false, "[]"), ("FrontDesk", false, "[]"), ("Operations", false, "[]"),
            ("QA-StudentsOnly", false, "[\"students.manage\"]"), ("QA-NoFinance", false, "[]")
        ];
        foreach (var actor in actors)
        {
            var email = actor.Role == "AcademyAdmin" ? "qa-admin-a@example.invalid" : actor.Role == "Teacher"
                ? "qa-teacher-a@example.invalid" : "qa-lookup-" + actor.Role.ToLowerInvariant() + "@example.invalid";
            if (actor.Role is not ("AcademyAdmin" or "Teacher"))
                await CreateAccessActorAsync(factory, email, actor.Role, academyA, actor.Role == "Student" ? studentA : null, actor.Permissions);
            var token = await LoginAsync(client, email, "Synthetic!39Ab"); tokens[actor.Role] = token;
            await Read(actor.Role, token, lookup, actor.Allowed ? HttpStatusCode.OK : HttpStatusCode.Forbidden, actor.Allowed ? academyA : null);
            if (actor.Allowed)
                foreach (var dependency in new[] { "invoices", "payments", "fee-plans", "finance-governance/settings", "academies" })
                    await Read(actor.Role + "/" + dependency, token, dependency == "academies" ? "/api/academies" : root + "/" + dependency, HttpStatusCode.OK);
        }
        foreach (var role in new[] { "FinanceUser", "QA-FinanceOnly" })
        {
            await Read(role + "/student-management-still-denied", tokens[role], root + "/students", HttpStatusCode.Forbidden);
            await Read(role + "/governance-work-items-still-open", tokens[role], root + "/admin-work-items?type=Collections", HttpStatusCode.Forbidden);
            await Read(role + "/foreign-route", tokens[role], $"/api/academies/{academyB}/invoices/student-options", HttpStatusCode.Forbidden);
        }
        await Read("anonymous", null, lookup, HttpStatusCode.Unauthorized);
        await Read("Admin/foreign-route", tokens["AcademyAdmin"], $"/api/academies/{academyB}/invoices/student-options", HttpStatusCode.Forbidden);

        var grantUserId = await CreateAccessActorAsync(factory, "qa-lookup-grant@example.invalid", "QA-LookupGrant", academyA);
        var grantToken = await LoginAsync(client, "qa-lookup-grant@example.invalid", "Synthetic!39Ab");
        await Read("grant/absent", grantToken, lookup, HttpStatusCode.Forbidden);
        Guid grantId;
        using (var scope = factory.Services.CreateScope())
        {
            var identity = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            var grant = new AccessGrant { AcademyId = academyB, UserId = grantUserId, GrantedByUserId = grantUserId,
                PermissionsJson = "[\"finance.manage\"]", ExpiresAtUtc = DateTimeOffset.UtcNow.AddHours(1), Reason = "Synthetic lookup fixture" };
            identity.AccessGrants.Add(grant); await identity.SaveChangesAsync(); grantId = grant.Id;
        }
        await Read("grant/wrong-academy", grantToken, lookup, HttpStatusCode.Forbidden);
        foreach (var state in new[] { "valid", "expired", "revoked", "permanent" })
        {
            using (var scope = factory.Services.CreateScope())
            {
                var identity = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
                var grant = await identity.AccessGrants.SingleAsync(x => x.Id == grantId);
                grant.AcademyId = academyA; grant.IsPermanent = state == "permanent";
                grant.ExpiresAtUtc = state is "expired" or "permanent" ? DateTimeOffset.UtcNow.AddHours(-1) : DateTimeOffset.UtcNow.AddHours(1);
                grant.RevokedAtUtc = state == "revoked" ? DateTimeOffset.UtcNow : null;
                await identity.SaveChangesAsync();
            }
            var allowed = state is "valid" or "permanent";
            await Read("grant/" + state + "-same-token", grantToken, lookup, allowed ? HttpStatusCode.OK : HttpStatusCode.Forbidden, allowed ? academyA : null);
        }
        foreach (var modules in new[] { "[\"FinanceControls\"]", "[\"Finance\"]" })
        {
            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
                (await db.Academies.SingleAsync(x => x.Id == academyA)).EnabledModulesJson = modules; await db.SaveChangesAsync();
            }
            var enabled = modules == "[\"Finance\"]";
            foreach (var role in new[] { "AcademyAdmin", "FinanceUser" })
                await Read(role + (enabled ? "/Finance-only" : "/Finance-disabled"), tokens[role], lookup,
                    enabled ? HttpStatusCode.OK : HttpStatusCode.Forbidden, enabled ? academyA : null);
        }
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var academy = await db.Academies.SingleAsync(x => x.Id == academyA);
            academy.EnabledModulesJson = originalModules; academy.IsActive = false; await db.SaveChangesAsync();
        }
        await Read("inactive-academy", tokens["FinanceUser"], lookup, HttpStatusCode.Forbidden);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            (await db.Academies.SingleAsync(x => x.Id == academyA)).IsActive = true; await db.SaveChangesAsync();
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await users.FindByEmailAsync("qa-lookup-financeuser@example.invalid") ?? throw new InvalidOperationException("Finance fixture missing");
            user.IsActive = false;
            if (!(await users.UpdateAsync(user)).Succeeded) throw new InvalidOperationException("Inactive fixture failed");
        }
        await Read("inactive-user-existing-token", tokens["FinanceUser"], lookup, HttpStatusCode.Forbidden);
        using (var scope = factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await users.FindByEmailAsync("qa-lookup-financeuser@example.invalid") ?? throw new InvalidOperationException("Finance fixture missing");
            user.IsActive = true; if (!(await users.UpdateAsync(user)).Succeeded) throw new InvalidOperationException("Restore fixture failed");
        }
        await Read("reactivated-user-existing-token", tokens["FinanceUser"], lookup, HttpStatusCode.OK, academyA);

        // Confirm finance lookup IDs actually support the existing save contracts.
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens["FinanceUser"]);
        var beforeRejected = await PeopleStateAsync(factory);
        using var forbiddenStudent = await client.PostAsJsonAsync(root + "/students", new { firstName = "Forbidden", lastName = "Synthetic" });
        RequireFinanceStatus(forbiddenStudent, HttpStatusCode.Forbidden, "lookup student mutation denied");
        if (beforeRejected != await PeopleStateAsync(factory)) throw new InvalidOperationException("Denied student write changed state");
        Console.WriteLine("LOOKUP CASE FinanceUser/student-write-denied PASS: HTTP=403; captured state unchanged."); count++;
        using var foreignInvoice = await client.PostAsJsonAsync(root + "/invoices", new { studentId = studentB, amount = 100m });
        RequireFinanceStatus(foreignInvoice, HttpStatusCode.BadRequest, "lookup foreign-student invoice");
        if (beforeRejected != await PeopleStateAsync(factory)) throw new InvalidOperationException("Foreign invoice changed state");
        Console.WriteLine("LOOKUP CASE FinanceUser/foreign-student-invoice PASS: HTTP=400; captured state unchanged."); count++;
        var audits = await AccessAuditCountAsync(factory);
        using var created = await client.PostAsJsonAsync(root + "/invoices", new { studentId = studentA, amount = 100m });
        RequireFinanceStatus(created, HttpStatusCode.Created, "lookup invoice create");
        using var invoiceBody = JsonDocument.Parse(await created.Content.ReadAsStringAsync());
        var invoiceId = invoiceBody.RootElement.GetProperty("id").GetGuid();
        using (var scope = factory.Services.CreateScope())
            if (!await scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>().Invoices.AsNoTracking()
                .AnyAsync(x => x.Id == invoiceId && x.AcademyId == academyA && x.StudentId == studentA && x.TotalAmount == 100m))
                throw new InvalidOperationException("Lookup invoice did not persist");
        if (await AccessAuditCountAsync(factory) != audits + 1) throw new InvalidOperationException("Lookup invoice audit mismatch");
        Console.WriteLine("LOOKUP CASE FinanceUser/invoice-create PASS: HTTP=201; fresh SQL own student/invoice and one success audit."); count++;
        using var paid = await client.PostAsJsonAsync(root + "/payments", new { invoiceId, amount = 100m, method = "UPI" });
        RequireFinanceStatus(paid, HttpStatusCode.Created, "lookup payment create");
        using var paymentBody = JsonDocument.Parse(await paid.Content.ReadAsStringAsync());
        var paymentId = paymentBody.RootElement.GetProperty("id").GetGuid();
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            if (!await db.Payments.AsNoTracking().AnyAsync(x => x.Id == paymentId && x.AcademyId == academyA && x.InvoiceId == invoiceId && x.Amount == 100m && x.Status == "Completed") ||
                !await db.Invoices.AsNoTracking().AnyAsync(x => x.Id == invoiceId && x.Status == "Paid")) throw new InvalidOperationException("Lookup payment did not settle");
        }
        if (await AccessAuditCountAsync(factory) != audits + 2) throw new InvalidOperationException("Lookup payment audit mismatch");
        Console.WriteLine("LOOKUP CASE FinanceUser/payment-create PASS: HTTP=201; fresh SQL completed payment/Paid invoice and one success audit."); count++;
        await Read("FinanceUser/refresh-after-save", tokens["FinanceUser"], lookup, HttpStatusCode.OK, academyA);
        Console.WriteLine($"LOOKUP REGRESSION PASS: {count} primary cases; actual finance dependencies, minimized same-tenant billing lookup, roles/grants/modules/inactive guards, unchanged student management and save/readback. Governance work-items/browser/full critical gates remain OPEN.");
    }

    private static async Task VerifyLookupPayloadAsync(QaApiFactory factory, string body, Guid academy)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
        var expected = await db.Students.AsNoTracking().Where(x => x.AcademyId == academy)
            .OrderBy(x => x.LastName).ThenBy(x => x.FirstName).ThenBy(x => x.Id)
            .Select(x => new InvoiceStudentOption(x.Id, x.FirstName, x.LastName, x.Email, x.Phone)).ToListAsync();
        using var json = JsonDocument.Parse(body);
        var rows = json.RootElement.EnumerateArray().ToArray();
        if (rows.Length != expected.Count) throw new InvalidOperationException("Lookup row count mismatch");
        var keys = new[] { "email", "firstName", "id", "lastName", "phone" };
        for (var i = 0; i < rows.Length; i++)
        {
            if (!rows[i].EnumerateObject().Select(x => x.Name).OrderBy(x => x, StringComparer.Ordinal).SequenceEqual(keys) ||
                rows[i].Deserialize<InvoiceStudentOption>(new JsonSerializerOptions(JsonSerializerDefaults.Web)) != expected[i])
                throw new InvalidOperationException("Lookup field minimization/order/null/contact contract mismatch");
        }
        if (body.Contains("PRIVATE-", StringComparison.Ordinal) || body.Contains("Isolated-B", StringComparison.Ordinal))
            throw new InvalidOperationException("Lookup exposed sensitive or foreign fields");
    }
}
