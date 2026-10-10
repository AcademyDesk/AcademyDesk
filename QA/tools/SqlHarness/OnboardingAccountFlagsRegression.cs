using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Identity;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

internal static partial class SqlHarnessEntryPoint
{
    private static async Task VerifyOnboardingAccountFlagsAsync(QaApiFactory factory, HttpClient client)
    {
        Guid academy, foreign;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            academy = await db.Academies.Where(x => x.Name == "Synthetic Academy A").Select(x => x.Id).SingleAsync();
            foreign = await db.Academies.Where(x => x.Name == "Synthetic Academy B").Select(x => x.Id).SingleAsync();
        }
        var admin = await LoginAsync(client, "qa-admin-a@example.invalid", "Synthetic!39Ab");
        var teacher = await LoginAsync(client, "qa-teacher-a@example.invalid", "Synthetic!39Ab");
        client.DefaultRequestHeaders.Authorization = null;
        var count = 0;
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
            }); // No credentials/hashes captured or emitted.
        }
        async Task Check(string studentShape, string parentShape, bool parent = true, bool minor = false,
            HttpStatusCode expected = HttpStatusCode.OK, string actor = "admin", bool foreignRoute = false)
        {
            await Task.Delay(650); var suffix = Guid.NewGuid().ToString("N");
            var body = new Dictionary<string, object?> {
                ["studentFirstName"] = "Flag", ["studentLastName"] = suffix, ["dateOfBirth"] = minor ? "2015-01-01" : "2000-01-01",
                ["studentEmail"] = "qa-flag-student-" + suffix + "@example.invalid"
            };
            if (parent) { body["parentFirstName"] = "Synthetic"; body["parentLastName"] = "Parent"; body["parentEmail"] = "qa-flag-parent-" + suffix + "@example.invalid"; }
            void Credentials(string prefix, string shape)
            {
                if (shape == "neither") return; // Truly omitted, not empty/null replacement.
                if (shape != "password-only") body[prefix + "UserName"] = shape switch { "null" => null, "empty" => "", "whitespace" => "   ", _ => "qa-flag-" + prefix + "-" + suffix };
                if (shape != "username-only") body[prefix + "TemporaryPassword"] = shape switch { "null" => null, "empty" => "", "whitespace" => "   ", "invalid" => "x", _ => "Synthetic!39Ab" };
            }
            Credentials("student", studentShape); Credentials("parent", parentShape);
            var before = await Snapshot();
            using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/academies/{(foreignRoute ? foreign : academy)}/student-onboarding") { Content = JsonContent.Create(body) };
            if (actor != "anonymous") request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", actor == "teacher" ? teacher : admin);
            using var response = await client.SendAsync(request);
            var label = $"student={studentShape},parent={parentShape},guardian={parent},minor={minor},actor={actor},foreign={foreignRoute}";
            RequireFinanceStatus(response, expected, "account flags " + label);
            if (expected != HttpStatusCode.OK)
            {
                if (before != await Snapshot()) throw new InvalidOperationException("Rejected onboarding changed captured SQL/Identity state: " + label);
                var rejectedBody = await response.Content.ReadAsStringAsync();
                if (!string.IsNullOrWhiteSpace(rejectedBody))
                {
                    using var rejection = JsonDocument.Parse(rejectedBody);
                    if (rejection.RootElement.TryGetProperty("studentAccountCreated", out _) || rejection.RootElement.TryGetProperty("parentAccountCreated", out _)) throw new InvalidOperationException("Rejected onboarding returned successful account flags.");
                }
            }
            else
            {
                using var result = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                var studentAccount = studentShape == "both";
                var parentAccount = parent && parentShape == "both";
                var id = result.RootElement.GetProperty("id").GetGuid();
                var parentId = result.RootElement.GetProperty("parentId").ValueKind == JsonValueKind.Null ? (Guid?)null : result.RootElement.GetProperty("parentId").GetGuid();
                if (parentId.HasValue != parent || result.RootElement.GetProperty("isMinor").GetBoolean() != minor) throw new InvalidOperationException("Guardian/minor response semantics changed.");
                using var afterJson = JsonDocument.Parse(await Snapshot()); using var beforeJson = JsonDocument.Parse(before);
                foreach (var (field, delta) in new[] { ("students", 1), ("guardians", parent ? 1 : 0), ("links", parent ? 1 : 0), ("audits", 1), ("users", (studentAccount ? 1 : 0) + (parentAccount ? 1 : 0)), ("memberships", (studentAccount ? 1 : 0) + (parentAccount ? 1 : 0)) })
                    if (afterJson.RootElement.GetProperty(field).GetArrayLength() - beforeJson.RootElement.GetProperty(field).GetArrayLength() != delta) throw new InvalidOperationException("Unexpected exact SQL delta: " + field + "; " + label);
                using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
                var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                if (!await db.Students.AsNoTracking().AnyAsync(x => x.Id == id && x.AcademyId == academy)) throw new InvalidOperationException("Returned student missing from fresh SQL.");
                if (parent && !await db.StudentGuardians.AsNoTracking().AnyAsync(x => x.StudentId == id && x.GuardianId == parentId && x.CanAccessPortal == minor)) throw new InvalidOperationException("Original adult/minor guardian access default changed.");
                foreach (var (role, created) in new[] { ("Student", studentAccount), ("Guardian", parentAccount) })
                {
                    var account = role == "Student" ? await users.Users.AsNoTracking().SingleOrDefaultAsync(x => x.StudentId == id) : parentId.HasValue ? await users.Users.AsNoTracking().SingleOrDefaultAsync(x => x.GuardianId == parentId) : null;
                    if (created ? account is null || account.AcademyId != academy || !await users.IsInRoleAsync(account, role) : account is not null) throw new InvalidOperationException("Response flag does not match fresh account and role: " + role);
                }
                if (result.RootElement.GetProperty("studentAccountCreated").GetBoolean() != studentAccount || result.RootElement.GetProperty("parentAccountCreated").GetBoolean() != parentAccount) throw new InvalidOperationException("Created flags disagree with freshly verified accounts and roles: " + label);
            }
            count++; Console.WriteLine("PASS: ONBOARDINGFLAGS " + label + " HTTP=" + (int)expected + "; exact flags/domain/Identity/audit verified");
        }
        await Check("neither", "neither"); await Check("username-only", "neither");
        foreach (var student in new[] { "neither", "username-only", "password-only", "both" })
            foreach (var parent in new[] { "neither", "username-only", "password-only", "both" })
                if (parent != "neither" || student is not ("neither" or "username-only")) await Check(student, parent);
        foreach (var shape in new[] { "null", "empty", "whitespace" }) { await Check(shape, "both"); await Check("both", shape); }
        foreach (var parent in new[] { "neither", "username-only", "password-only", "both" }) await Check("both", parent, parent: false);
        await Check("neither", "neither", minor: true); await Check("both", "both", minor: true);
        await Check("both", "both", expected: HttpStatusCode.Unauthorized, actor: "anonymous");
        await Check("both", "both", expected: HttpStatusCode.Forbidden, actor: "teacher");
        await Check("both", "both", expected: HttpStatusCode.Forbidden, foreignRoute: true);
        await Check("invalid", "both", expected: HttpStatusCode.BadRequest);
        await Check("both", "invalid", expected: HttpStatusCode.BadRequest);
        if (count != 33) throw new InvalidOperationException("Incomplete account flag matrix.");
        Console.WriteLine("PASS: ONBOARDINGFLAGS all 33 native SQL/Identity/HTTP cases; partial/absent/blank credentials, real accounts/roles, guardian/minor defaults and rejected no-write controls.");
    }
}
