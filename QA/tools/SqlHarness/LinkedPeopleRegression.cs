using System.Net;
using System.Data.Common;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Domain.Identity;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

internal static partial class SqlHarnessEntryPoint
{
    private sealed record LinkedFixture(string Kind, Guid Academy, Guid ForeignAcademy, Guid Person, Guid ForeignAccount, string Login);

    private static async Task VerifyLinkedPeopleAsync(QaApiFactory factory, HttpClient client, QaRunManifest manifest, LinkedIdentitySqlFault fault, bool enforceFixed)
    {
        var adminToken = await LoginAsync(client, "qa-admin-a@example.invalid", "Synthetic!39Ab");
        var teacherToken = await LoginAsync(client, "qa-teacher-a@example.invalid", "Synthetic!39Ab");
        foreach (var kind in new[] { "students", "teachers" })
        {
            // Each injected failure has its own real domain row and two linked accounts.
            var validation = await SeedLinkedAsync(factory, kind);
            using (var scope = factory.Services.CreateScope())
            {
                // Real default UserValidator: email uniqueness is not enabled. Model
                // historical malformed usernames directly in this disposable fixture.
                var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
                var users = await db.Users.Where(x => x.AcademyId == validation.Academy &&
                    (kind == "students" ? x.StudentId == validation.Person : x.TeacherId == validation.Person)).ToListAsync();
                foreach (var user in users) user.UserName = "invalid synthetic username with spaces";
                await db.SaveChangesAsync();
            }
            await CheckLinkedAsync(factory, client, validation, adminToken, LinkedBody("validation"), HttpStatusCode.BadRequest,
                kind + "/identity-validation", enforceFixed, baselineAccounts: 0, baselineDomain: true);

            foreach (var failAt in new[] { 1, 2 })
            {
                var fixture = await SeedLinkedAsync(factory, kind);
                var body = LinkedBody("sql-fault-" + failAt);
                await ArmLinkedFaultAsync(manifest, fault, body, failAt);
                try
                {
                    await CheckLinkedAsync(factory, client, fixture, adminToken, body, HttpStatusCode.InternalServerError,
                        kind + "/account-fault-" + failAt, enforceFixed, baselineAccounts: failAt - 1, baselineDomain: true);
                    if (fault.Attempts != failAt) throw new InvalidOperationException("Linked SQL fault did not reach the required account UPDATE.");
                    Console.WriteLine($"LINKED FAULT attempts={fault.Attempts}; failAt={failAt}; SQL THROW on real account UPDATE, no query-order assumption.");
                }
                finally { fault.Disarm(); Console.WriteLine("LINKED FAULT removed: QA command interception only."); }
            }
            var audit = await SeedLinkedAsync(factory, kind);
            await SetAuditInsertDeniedAsync(manifest, true);
            try
            {
                await CheckLinkedAsync(factory, client, audit, adminToken, LinkedBody("audit-fault"), HttpStatusCode.InternalServerError,
                    kind + "/audit-fault", enforceFixed, baselineAccounts: 2, baselineDomain: true);
            }
            finally { await SetAuditInsertDeniedAsync(manifest, false); }

            var success = await SeedLinkedAsync(factory, kind);
            await CheckLinkedAsync(factory, client, success, adminToken, LinkedBody("full"), HttpStatusCode.OK, kind + "/recovery-populated", enforceFixed);
            // Preserve existing blank/null domain email behavior: linked login emails are retained.
            var optional = await SeedLinkedAsync(factory, kind);
            var nullable = LinkedBody("nullable"); nullable.Remove("email"); nullable.Remove("phone"); nullable.Remove("specialties");
            await CheckLinkedAsync(factory, client, optional, adminToken, nullable, HttpStatusCode.OK, kind + "/optional-omitted", enforceFixed);

            var lifecycle = await SeedLinkedAsync(factory, kind);
            var linkedToken = await LoginAsync(client, lifecycle.Login, "Synthetic!39Ab");
            await LinkedHealthAsync(client, linkedToken, HttpStatusCode.OK);
            var inactive = LinkedBody("inactive"); inactive["isActive"] = false;
            await CheckLinkedAsync(factory, client, lifecycle, adminToken, inactive, HttpStatusCode.OK, kind + "/deactivate", enforceFixed);
            await LinkedHealthAsync(client, linkedToken, HttpStatusCode.Forbidden);
            await CheckLinkedAsync(factory, client, lifecycle, adminToken, LinkedBody("reactivate"), HttpStatusCode.OK, kind + "/reactivate", enforceFixed);
            await LinkedHealthAsync(client, linkedToken, HttpStatusCode.OK);
            Console.WriteLine($"LINKED ACCESS {kind} PASS: previously issued linked token active=200, inactive=403, reactivated=200.");

            var rejected = await SeedLinkedAsync(factory, kind);
            var blank = LinkedBody("blank"); blank["firstName"] = " ";
            await CheckLinkedAsync(factory, client, rejected, adminToken, blank, HttpStatusCode.BadRequest, kind + "/blank-name", enforceFixed);
            await CheckLinkedAsync(factory, client, rejected with { Person = Guid.NewGuid() }, adminToken, LinkedBody("missing"), HttpStatusCode.NotFound, kind + "/missing", enforceFixed);
            await CheckLinkedAsync(factory, client, rejected with { Academy = rejected.ForeignAcademy }, adminToken, LinkedBody("foreign"), HttpStatusCode.Forbidden, kind + "/foreign-route", enforceFixed);
            await CheckLinkedAsync(factory, client, rejected, null, LinkedBody("anonymous"), HttpStatusCode.Unauthorized, kind + "/anonymous", enforceFixed);
            await CheckLinkedAsync(factory, client, rejected, teacherToken, LinkedBody("role"), HttpStatusCode.Forbidden, kind + "/teacher-role", enforceFixed);
        }
        Console.WriteLine($"LINKED {(enforceFixed ? "REGRESSION PASS" : "BASELINE REPRODUCED")}: 26 cases; two people actions, real Identity validation, first/second UPDATE SQL faults, audit faults, two-account synchronization, nullable fields, active-token gates, tenant/role/refusal controls; platform bypass/UI/concurrency/provisioning NOT RUN.");
    }

    private static Dictionary<string, object?> LinkedBody(string label) => new()
    {
        ["firstName"] = "  QA-Linked-" + label + "  ", ["lastName"] = "  Synthetic  ",
        ["email"] = "  qa-linked-updated@example.invalid  ", ["phone"] = "  1234567890  ",
        ["specialties"] = "  Piano  ", ["branchId"] = null, ["isActive"] = true
    };

    private static async Task<LinkedFixture> SeedLinkedAsync(QaApiFactory factory, string kind)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
        var academy = await db.Academies.Where(x => x.Name == "Synthetic Academy A").Select(x => x.Id).SingleAsync();
        var foreign = await db.Academies.Where(x => x.Name == "Synthetic Academy B").Select(x => x.Id).SingleAsync();
        Guid person;
        if (kind == "students")
        {
            var row = new Student { AcademyId = academy, FirstName = "QA-Original", LastName = "Synthetic", Email = "qa-original@example.invalid", Phone = "old" };
            db.Students.Add(row); await db.SaveChangesAsync(); person = row.Id;
        }
        else
        {
            var row = new Teacher { AcademyId = academy, FirstName = "QA-Original", LastName = "Synthetic", Email = "qa-original@example.invalid", Phone = "old", Specialties = "old" };
            db.Teachers.Add(row); await db.SaveChangesAsync(); person = row.Id;
        }
        var manager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var role = kind == "students" ? "Student" : "Teacher";
        if (!await roles.RoleExistsAsync(role))
        {
            var result = await roles.CreateAsync(new ApplicationRole { Name = role });
            if (!result.Succeeded) throw new InvalidOperationException("Synthetic linked role creation failed.");
        }
        string firstEmail = ""; Guid foreignAccount = default;
        for (var i = 0; i < 3; i++)
        {
            var email = "qa-linked-" + Guid.NewGuid().ToString("N") + "@example.invalid";
            var user = new ApplicationUser { UserName = email, Email = email, DisplayName = "QA-Original Synthetic", PhoneNumber = "old",
                AcademyId = i == 2 ? foreign : academy, StudentId = kind == "students" ? person : null,
                TeacherId = kind == "teachers" ? person : null, IsActive = true };
            if (!(await manager.CreateAsync(user, "Synthetic!39Ab")).Succeeded || !(await manager.AddToRoleAsync(user, role)).Succeeded)
                throw new InvalidOperationException("Synthetic linked account creation failed.");
            if (i == 0) firstEmail = email;
            if (i == 2) foreignAccount = user.Id;
        }
        return new LinkedFixture(kind, academy, foreign, person, foreignAccount, firstEmail);
    }

    private static async Task CheckLinkedAsync(QaApiFactory factory, HttpClient client, LinkedFixture fixture, string? token,
        Dictionary<string, object?> body, HttpStatusCode expected, string label, bool enforceFixed, int baselineAccounts = 0, bool baselineDomain = false)
    {
        await Task.Delay(650);
        client.DefaultRequestHeaders.Authorization = token is null ? null : new AuthenticationHeaderValue("Bearer", token);
        var stateBefore = await PeopleStateAsync(factory);
        var accountsBefore = await LinkedAccountsAsync(factory);
        var financeBefore = await AccessFinancialSnapshotAsync(factory);
        var auditBefore = await AccessAuditCountAsync(factory);
        var path = $"/api/academies/{fixture.Academy}/{fixture.Kind}/{fixture.Person}";
        using var response = await client.PutAsJsonAsync(path, body);
        RequireFinanceStatus(response, expected, "linked " + label);
        if (expected == HttpStatusCode.BadRequest && label.StartsWith("branch/", StringComparison.Ordinal) && !label.EndsWith("/malformed", StringComparison.Ordinal))
        {
            using var rejection = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            if (rejection.RootElement.GetProperty("message").GetString() != "The selected branch does not belong to this academy.")
                throw new InvalidOperationException("Branch ownership rejection body mismatch: " + label);
        }
        var success = expected == HttpStatusCode.OK;
        var reproduce = !enforceFixed && baselineDomain;
        var auditDelta = await AccessAuditCountAsync(factory) - auditBefore;
        var accountsAfter = await LinkedAccountsAsync(factory);
        if (auditDelta != (success ? 1 : 0) || financeBefore != await AccessFinancialSnapshotAsync(factory))
            throw new InvalidOperationException("Linked mutation affected finance/notifications/audit unexpectedly: " + label);
        if (!success && !reproduce)
        {
            if (stateBefore != await PeopleStateAsync(factory) || accountsBefore != accountsAfter)
                throw new InvalidOperationException("Failed linked write left captured persisted changes: " + label);
            if (label.EndsWith("identity-validation", StringComparison.Ordinal) &&
                !(await response.Content.ReadAsStringAsync()).Contains("No changes were saved.", StringComparison.Ordinal))
                throw new InvalidOperationException("Linked validation response was not truthful about rollback.");
        }
        else
        {
            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var row = fixture.Kind == "students" ? JsonSerializer.SerializeToElement(await db.Students.AsNoTracking().SingleAsync(x => x.Id == fixture.Person)) :
                JsonSerializer.SerializeToElement(await db.Teachers.AsNoTracking().SingleAsync(x => x.Id == fixture.Person));
            foreach (var key in new[] { "firstName", "lastName", "email", "phone", "branchId", "isActive" }.Concat(fixture.Kind == "teachers" ? ["specialties"] : Array.Empty<string>()))
            {
                var actual = row.GetProperty(char.ToUpperInvariant(key[0]) + key[1..]);
                var wanted = body.GetValueOrDefault(key)?.ToString()?.Trim();
                if (wanted is null ? actual.ValueKind != JsonValueKind.Null : !string.Equals(actual.ToString(), wanted, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Linked fresh domain field mismatch: " + label + "/" + key);
            }
            using var before = JsonDocument.Parse(accountsBefore);
            using var after = JsonDocument.Parse(accountsAfter);
            var oldRows = before.RootElement.EnumerateArray().ToDictionary(x => x.GetProperty("Id").GetGuid());
            var changed = 0;
            foreach (var account in after.RootElement.EnumerateArray())
            {
                var id = account.GetProperty("Id").GetGuid();
                if (account.GetRawText() == oldRows[id].GetRawText()) continue;
                var linkKey = fixture.Kind == "students" ? "StudentId" : "TeacherId";
                if (account.GetProperty("AcademyId").GetGuid() != fixture.Academy || account.GetProperty(linkKey).GetGuid() != fixture.Person || id == fixture.ForeignAccount)
                    throw new InvalidOperationException("Unrelated/foreign account changed: " + label);
                changed++;
                var email = body.GetValueOrDefault("email")?.ToString()?.Trim();
                if (account.GetProperty("DisplayName").GetString() != body["firstName"]!.ToString()!.Trim() + " " + body["lastName"]!.ToString()!.Trim() ||
                    account.GetProperty("IsActive").GetBoolean() != (bool)body["isActive"]! ||
                    account.GetProperty("PhoneNumber").GetString() != body.GetValueOrDefault("phone")?.ToString()?.Trim() ||
                    account.GetProperty("Email").GetString() != (string.IsNullOrWhiteSpace(email) ? oldRows[id].GetProperty("Email").GetString() : email) ||
                    account.GetProperty("NormalizedEmail").GetString() != (string.IsNullOrWhiteSpace(email) ? oldRows[id].GetProperty("NormalizedEmail").GetString() : email.ToUpperInvariant()))
                    throw new InvalidOperationException("Linked account field mismatch: " + label);
            }
            if (changed != (success ? 2 : baselineAccounts)) throw new InvalidOperationException("Unexpected changed linked account count: " + label);
            if (success)
            {
                using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                if (payload.RootElement.GetProperty("id").GetGuid() != fixture.Person || payload.RootElement.GetProperty("firstName").GetString() != body["firstName"]!.ToString()!.Trim())
                    throw new InvalidOperationException("Linked successful response mismatch.");
                var identity = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
                var actor = await identity.Users.Where(x => x.Email == "qa-admin-a@example.invalid").Select(x => x.Id).SingleAsync();
                var log = await db.AuditLogs.AsNoTracking().OrderByDescending(x => x.OccurredAtUtc).FirstAsync();
                if (log.ActorUserId != actor || log.AcademyId != fixture.Academy || log.Action != "PUT " + (fixture.Kind == "students" ? "Students" : "Teachers") || !log.MetadataJson!.Contains(path))
                    throw new InvalidOperationException("Linked audit attribution mismatch.");
            }
        }
        Console.WriteLine($"LINKED CASE {label} {(reproduce ? "REPRODUCED" : "PASS")}: HTTP={(int)expected}; domain={(success || reproduce ? "saved" : "unchanged")}, linked accounts={(success ? 2 : reproduce ? baselineAccounts : 0)}, audit delta={auditDelta}; fresh captured state checked.");
    }

    private static async Task<string> LinkedAccountsAsync(QaApiFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        // In-memory comparison only. No password hashes/security stamps emitted.
        return JsonSerializer.Serialize(await db.Users.AsNoTracking().OrderBy(x => x.Id).Select(x => new
        { x.Id, x.AcademyId, x.StudentId, x.TeacherId, x.UserName, x.NormalizedUserName, x.DisplayName, x.Email, x.NormalizedEmail, x.PhoneNumber, x.IsActive }).ToListAsync());
    }

    private static async Task LinkedHealthAsync(HttpClient client, string token, HttpStatusCode expected)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await client.GetAsync("/health");
        RequireFinanceStatus(response, expected, "linked active token gate");
    }

    private static async Task<SqlConnection> LinkedOwnedSqlAsync(QaRunManifest manifest)
    {
        var db = new SqlConnection(Connection(manifest.SqlServer, manifest.Database, "sa",
            Environment.GetEnvironmentVariable("QA_SQL_SA_PASSWORD") ?? throw new InvalidOperationException("QA credential absent.")));
        await db.OpenAsync();
        var marker = await ReadMarkerAsync(db, manifest);
        if (marker.RunId != manifest.RunId || marker.TokenDigest != manifest.TokenDigest)
        { await db.DisposeAsync(); throw new InvalidOperationException("Refused linked fault: wrong owned marker."); }
        return db;
    }

    private static async Task ArmLinkedFaultAsync(QaRunManifest manifest, LinkedIdentitySqlFault fault, Dictionary<string, object?> body, int failAt)
    {
        await using var db = await LinkedOwnedSqlAsync(manifest);
        var name = body["firstName"]!.ToString()!.Trim() + " " + body["lastName"]!.ToString()!.Trim();
        fault.Arm(name, failAt);
        Console.WriteLine($"LINKED FAULT enabled: failAt={failAt}; generated target in exact owned database only.");
    }

    private sealed class LinkedIdentitySqlFault : DbCommandInterceptor
    {
        private string? displayName;
        private int failAt;
        public int Attempts { get; private set; }
        public void Arm(string name, int at)
        {
            if (at is not (1 or 2) || displayName is not null) throw new InvalidOperationException("Invalid QA fault state.");
            displayName = name; failAt = at; Attempts = 0;
        }
        public void Disarm() { displayName = null; failAt = 0; }
        private void Inject(DbCommand command)
        {
            if (displayName is null || !command.CommandText.Contains("UPDATE [AspNetUsers]", StringComparison.Ordinal) ||
                !command.Parameters.Cast<DbParameter>().Any(x => Equals(x.Value, displayName))) return;
            Attempts++;
            // Preserve native SQL for the first UPDATE. Selected UPDATE fails at
            // the actual SQL server; no mocked IdentityResult/context/save.
            if (Attempts == failAt) command.CommandText = "THROW 51001, 'Synthetic linked account store fault', 1;\n" + command.CommandText;
        }
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        { Inject(command); return ValueTask.FromResult(result); }
        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        { Inject(command); return ValueTask.FromResult(result); }
    }
}
