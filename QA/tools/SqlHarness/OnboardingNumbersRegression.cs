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
    private static async Task VerifyOnboardingNumbersAsync(QaApiFactory factory, HttpClient client)
    {
        Guid academy, foreign;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            academy = await db.Academies.Where(x => x.Name == "Synthetic Academy A").Select(x => x.Id).SingleAsync();
            foreign = await db.Academies.Where(x => x.Name == "Synthetic Academy B").Select(x => x.Id).SingleAsync();
        }
        var admin = await LoginAsync(client, "qa-admin-a@example.invalid", "Synthetic!39Ab");
        var otherAdmin = await LoginAsync(client, "qa-admin-b@example.invalid", "Synthetic!39Ab");
        var teacher = await LoginAsync(client, "qa-teacher-a@example.invalid", "Synthetic!39Ab");
        client.DefaultRequestHeaders.Authorization = null;
        var count = 0;
        const string duplicateMessage = "Student onboarding could not be saved. Check that the student number is unique and the entered values are valid.";
        async Task<string> Snapshot()
        {
            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var identity = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            return JsonSerializer.Serialize(new {
                students = await db.Students.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(),
                guardians = await db.Guardians.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(),
                links = await db.StudentGuardians.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(),
                audits = await db.AuditLogs.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(),
                users = await identity.Users.AsNoTracking().OrderBy(x => x.Id).Select(x => new { x.Id, x.UserName, x.AcademyId, x.StudentId, x.GuardianId }).ToArrayAsync(),
                roles = await identity.Roles.AsNoTracking().OrderBy(x => x.Id).Select(x => new { x.Id, x.Name }).ToArrayAsync(),
                memberships = await identity.UserRoles.AsNoTracking().OrderBy(x => x.UserId).ThenBy(x => x.RoleId).ToArrayAsync()
            }); // Safe synthetic state only: never capture credentials or password hashes.
        }
        Dictionary<string, object?> Body(string shape, string? number, bool minor)
        {
            var suffix = Guid.NewGuid().ToString("N");
            var body = new Dictionary<string, object?> {
                ["studentFirstName"] = "Number", ["studentLastName"] = suffix,
                ["dateOfBirth"] = minor ? "2015-01-01" : "2000-01-01",
                ["studentEmail"] = "qa-number-student-" + suffix + "@example.invalid"
            };
            if (shape != "omitted") body["studentNumber"] = number;
            if (minor)
            {
                body["parentFirstName"] = "Synthetic"; body["parentLastName"] = "Parent";
                body["parentEmail"] = "qa-number-parent-" + suffix + "@example.invalid";
                body["studentUserName"] = "qa-number-student-" + suffix;
                body["parentUserName"] = "qa-number-parent-" + suffix;
                body["studentTemporaryPassword"] = "Synthetic!39Ab";
                body["parentTemporaryPassword"] = "Synthetic!39Ab";
            }
            return body;
        }
        async Task<HttpResponseMessage> Send(Dictionary<string, object?> body, Guid route, string? actor)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/academies/{route}/student-onboarding") { Content = JsonContent.Create(body) };
            if (actor is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", actor);
            return await client.SendAsync(request);
        }
        void Deltas(string before, string after, int students, int minors)
        {
            using var a = JsonDocument.Parse(after); using var b = JsonDocument.Parse(before);
            foreach (var (field, delta) in new[] { ("students", students), ("guardians", minors), ("links", minors), ("audits", students), ("users", minors * 2), ("memberships", minors * 2) })
                if (a.RootElement.GetProperty(field).GetArrayLength() - b.RootElement.GetProperty(field).GetArrayLength() != delta)
                    throw new InvalidOperationException("Unexpected exact onboarding-number SQL delta: " + field);
        }
        async Task VerifySaved(HttpResponseMessage response, Guid route, string? expectedNumber, bool minor)
        {
            using var result = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var id = result.RootElement.GetProperty("id").GetGuid();
            var parent = result.RootElement.GetProperty("parentId");
            if (result.RootElement.GetProperty("isMinor").GetBoolean() != minor || (parent.ValueKind != JsonValueKind.Null) != minor ||
                result.RootElement.GetProperty("studentAccountCreated").GetBoolean() != minor || result.RootElement.GetProperty("parentAccountCreated").GetBoolean() != minor)
                throw new InvalidOperationException("Onboarding-number response changed optional-account/minor semantics.");
            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var saved = await db.Students.AsNoTracking().SingleAsync(x => x.Id == id && x.AcademyId == route);
            if (saved.StudentNumber != expectedNumber) throw new InvalidOperationException("Optional student number was not persisted in its canonical SQL form.");
            if (!minor)
            {
                if (await users.Users.AsNoTracking().AnyAsync(x => x.StudentId == id)) throw new InvalidOperationException("Optional absent credentials created an account.");
                return;
            }
            var parentId = parent.GetGuid();
            if (!await db.Guardians.AsNoTracking().AnyAsync(x => x.Id == parentId && x.AcademyId == route) ||
                !await db.StudentGuardians.AsNoTracking().AnyAsync(x => x.AcademyId == route && x.StudentId == id && x.GuardianId == parentId && x.CanAccessPortal))
                throw new InvalidOperationException("Minor guardian chain missing from fresh SQL.");
            foreach (var role in new[] { "Student", "Guardian" })
            {
                var account = role == "Student" ? await users.Users.AsNoTracking().SingleAsync(x => x.StudentId == id) : await users.Users.AsNoTracking().SingleAsync(x => x.GuardianId == parentId);
                if (account.AcademyId != route || !await users.IsInRoleAsync(account, role)) throw new InvalidOperationException("Onboarding-number account role/tenant mismatch.");
            }
        }
        async Task Check(string label, string shape, string? number, string? expectedNumber, bool minor = false,
            HttpStatusCode expected = HttpStatusCode.OK, string actor = "admin", bool foreignRoute = false)
        {
            await Task.Delay(650);
            var before = await Snapshot(); var route = foreignRoute ? foreign : academy;
            using var response = await Send(Body(shape, number, minor), route, actor switch { "anonymous" => null, "teacher" => teacher, "otherAdmin" => otherAdmin, _ => admin });
            RequireFinanceStatus(response, expected, "onboarding number " + label);
            if (expected == HttpStatusCode.OK)
            {
                Deltas(before, await Snapshot(), 1, minor ? 1 : 0);
                await VerifySaved(response, route, expectedNumber, minor);
            }
            else
            {
                if (before != await Snapshot()) throw new InvalidOperationException("Rejected onboarding-number case changed domain/Identity/audit state: " + label);
                if (expected == HttpStatusCode.BadRequest)
                {
                    using var rejection = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                    if (rejection.RootElement.GetProperty("message").GetString() != duplicateMessage) throw new InvalidOperationException("Existing persistence rejection message changed.");
                }
            }
            count++; Console.WriteLine("PASS: ONBOARDINGNUMBERS " + label + " HTTP=" + (int)expected + "; fresh SQL/domain/Identity/audit verified");
        }

        // Reproduce the actual blank form collision before asserting canonical null persistence.
        await Task.Delay(650); var blankBefore = await Snapshot();
        using (var first = await Send(Body("empty", "", false), academy, admin))
        using (var second = await Send(Body("empty", "", false), academy, admin))
        {
            RequireFinanceStatus(first, HttpStatusCode.OK, "first empty-number intake");
            RequireFinanceStatus(second, HttpStatusCode.OK, "second empty-number intake");
            Deltas(blankBefore, await Snapshot(), 2, 0);
            await VerifySaved(first, academy, null, false); await VerifySaved(second, academy, null, false);
            count += 2; Console.WriteLine("PASS: ONBOARDINGNUMBERS two empty-number intakes HTTP=200/200; both SQL NULL, exact deltas");
        }
        foreach (var (shape, value) in new (string, string?)[] { ("omitted", null), ("null", null), ("spaces", "   "), ("tabs-newline", "\t\r\n"), ("unicode-space", "\u00a0\u2003") })
            for (var i = 0; i < 2; i++) await Check(shape + "-" + i, shape, value, null);
        foreach (var (shape, value) in new[] { ("empty", ""), ("spaces", "   ") })
            for (var i = 0; i < 2; i++) await Check("minor-" + shape + "-" + i, shape, value, null, minor: true);
        var explicitNumber = "QA-" + Guid.NewGuid().ToString("N");
        await Check("explicit-trimmed", "explicit", "  " + explicitNumber + "  ", explicitNumber, minor: true);
        await Check("same-tenant-trimmed-duplicate", "explicit", " " + explicitNumber + " ", explicitNumber, minor: true, expected: HttpStatusCode.BadRequest);
        await Check("distinct-explicit", "explicit", explicitNumber + "-2", explicitNumber + "-2");
        await Check("other-academy-same-number", "explicit", explicitNumber, explicitNumber, minor: true, actor: "otherAdmin", foreignRoute: true);
        await Check("other-academy-duplicate", "explicit", explicitNumber, explicitNumber, minor: true, expected: HttpStatusCode.BadRequest, actor: "otherAdmin", foreignRoute: true);
        await Check("anonymous-no-write", "empty", "", null, minor: true, expected: HttpStatusCode.Unauthorized, actor: "anonymous");
        await Check("teacher-no-write", "empty", "", null, minor: true, expected: HttpStatusCode.Forbidden, actor: "teacher");
        await Check("foreign-route-no-write", "empty", "", null, minor: true, expected: HttpStatusCode.Forbidden, foreignRoute: true);
        await Check("existing-length-constraint", "explicit", new string('Q', 51), null, minor: true, expected: HttpStatusCode.BadRequest);

        Guid legacyId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var legacy = new Student { AcademyId = academy, FirstName = "Synthetic", LastName = "LegacyBlank", StudentNumber = "", DateOfBirth = new DateOnly(2000, 1, 1) };
            db.Students.Add(legacy); await db.SaveChangesAsync(); legacyId = legacy.Id;
        } // Owned disposable SQL only: simulate a pre-existing blank without rewriting historical rows.
        await Check("legacy-blank-adult", "empty", "", null);
        await Check("legacy-blank-minor", "spaces", "   ", null, minor: true);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            if ((await db.Students.AsNoTracking().SingleAsync(x => x.Id == legacyId)).StudentNumber != "") throw new InvalidOperationException("Legacy row was unexpectedly rewritten.");
        }
        await Task.Delay(650); var concurrentBefore = await Snapshot();
        var concurrent = await Task.WhenAll(Send(Body("empty", "", true), academy, admin), Send(Body("spaces", "   ", true), academy, admin));
        try
        {
            foreach (var response in concurrent) { RequireFinanceStatus(response, HttpStatusCode.OK, "concurrent optional blank number"); await VerifySaved(response, academy, null, true); }
            Deltas(concurrentBefore, await Snapshot(), 2, 2);
            count += 2; Console.WriteLine("PASS: ONBOARDINGNUMBERS concurrent blank intakes HTTP=200/200; two complete chains, legacy preserved");
        }
        finally { foreach (var response in concurrent) response.Dispose(); }
        if (count != 29) throw new InvalidOperationException("Incomplete optional student-number matrix.");
        Console.WriteLine("PASS: ONBOARDINGNUMBERS all 29 native SQL/Identity/HTTP cases; optional nulls, exact chain/account flags, explicit uniqueness, tenant/role/no-write, length, legacy and concurrency controls.");
    }
}
