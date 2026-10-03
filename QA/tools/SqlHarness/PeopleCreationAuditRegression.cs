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
    private static async Task VerifyPeopleCreationAuditAsync(QaApiFactory factory, HttpClient client, QaRunManifest manifest, bool enforceFixed)
    {
        Guid academyA, academyB, ownBranch, foreignBranch;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            academyA = await db.Academies.Where(x => x.Name == "Synthetic Academy A").Select(x => x.Id).SingleAsync();
            academyB = await db.Academies.Where(x => x.Name == "Synthetic Academy B").Select(x => x.Id).SingleAsync();
            var own = new Branch { AcademyId = academyA, Name = "Synthetic People Own Branch" };
            var foreign = new Branch { AcademyId = academyB, Name = "Synthetic People Foreign Branch" };
            db.AddRange(own, foreign, new Teacher { AcademyId = academyB, FirstName = "QA-Foreign-Teacher", LastName = "Synthetic" });
            await db.SaveChangesAsync(); ownBranch = own.Id; foreignBranch = foreign.Id;
        }
        const string admin = "qa-admin-a@example.invalid";
        var adminToken = await LoginAsync(client, admin, "Synthetic!39Ab");
        foreach (var kind in new[] { "students", "teachers" })
        {
            var path = $"/api/academies/{academyA}/{kind}";
            await SetAuditInsertDeniedAsync(manifest, true);
            try
            {
                await CheckPeopleCreationAsync(factory, client, adminToken, admin, kind, path,
                    PeopleCreateBody(kind, "fault", ownBranch, full: kind == "teachers"), HttpStatusCode.InternalServerError,
                    allowCommittedFault: !enforceFixed, label: kind + "/fault");
            }
            finally { await SetAuditInsertDeniedAsync(manifest, false); }

            await CheckPeopleCreationAsync(factory, client, adminToken, admin, kind, path, PeopleCreateBody(kind, "minimal"), HttpStatusCode.Created, label: kind + "/minimal-optional-omitted");
            await CheckPeopleCreationAsync(factory, client, adminToken, admin, kind, path, PeopleCreateBody(kind, "full", ownBranch, full: true), HttpStatusCode.Created, label: kind + "/optional-populated");
            var blank = PeopleCreateBody(kind, "blank"); blank["firstName"] = " ";
            await CheckPeopleCreationAsync(factory, client, adminToken, admin, kind, path, blank, HttpStatusCode.BadRequest, label: kind + "/blank-name");
            await CheckPeopleCreationAsync(factory, client, adminToken, admin, kind, path, PeopleCreateBody(kind, "foreign", foreignBranch, full: true), HttpStatusCode.BadRequest, label: kind + "/foreign-branch");
            await CheckPeopleCreationAsync(factory, client, adminToken, admin, kind, $"/api/academies/{academyB}/{kind}", PeopleCreateBody(kind, "cross"), HttpStatusCode.Forbidden, label: kind + "/foreign-route");
            await CheckPeopleCreationAsync(factory, client, null, null, kind, path, PeopleCreateBody(kind, "anonymous"), HttpStatusCode.Unauthorized, label: kind + "/anonymous");

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
            var before = await PeopleStateAsync(factory);
            using var list = await client.GetAsync(path);
            RequireFinanceStatus(list, HttpStatusCode.OK, "people read");
            var body = await list.Content.ReadAsStringAsync();
            var foreignMarker = kind == "students" ? "Isolated-B" : "QA-Foreign-Teacher";
            if (before != await PeopleStateAsync(factory) || body.Contains(foreignMarker, StringComparison.Ordinal))
                throw new InvalidOperationException("People read mutated captured state or leaked known foreign student.");
            Console.WriteLine($"PEOPLE CASE {kind}/read PASS: HTTP=200; captured people/Identity/finance/notifications/audits unchanged; known foreign person not returned.");
        }

        (string Name, string Email, bool Student, bool Teacher)[] actors = [
            ("AcademyAdmin", admin, true, true), ("Owner", "qa-people-owner@example.invalid", true, true),
            ("FrontDesk", "qa-people-frontdesk@example.invalid", true, false),
            ("FinanceUser", "qa-people-finance@example.invalid", false, false),
            ("Teacher", "qa-teacher-a@example.invalid", false, false),
            ("QA-StudentManage", "qa-people-custom@example.invalid", true, false)
        ];
        foreach (var actor in actors)
        {
            if (actor.Name is not ("AcademyAdmin" or "Teacher"))
                await CreateAccessActorAsync(factory, actor.Email, actor.Name, academyA,
                    permissions: actor.Name == "QA-StudentManage" ? "[\"students.manage\"]" : "[]");
            var token = await LoginAsync(client, actor.Email, "Synthetic!39Ab");
            foreach (var kind in new[] { "students", "teachers" })
                await CheckPeopleCreationAsync(factory, client, token, actor.Email, kind, $"/api/academies/{academyA}/{kind}",
                    PeopleCreateBody(kind, actor.Name), (kind == "students" ? actor.Student : actor.Teacher) ? HttpStatusCode.Created : HttpStatusCode.Forbidden,
                    label: actor.Name + "/" + kind);
        }
        Console.WriteLine($"PEOPLE {(enforceFixed ? "REGRESSION PASS" : "BASELINE REPRODUCED")}: 28 cases; two audited create actions only, minimal/full optional fields, own/foreign branches/routes, six current actor classes; Identity updates/provisioning/platform bypass/critical NOT RUN.");
    }

    private static Dictionary<string, object?> PeopleCreateBody(string kind, string label, Guid? branch = null, bool full = false)
    {
        var body = new Dictionary<string, object?> { ["firstName"] = "  QA-People-" + label + "-" + Guid.NewGuid().ToString("N")[..8] + "  ", ["lastName"] = "  Synthetic  " };
        if (!full) return body; // All optional fields omitted, not made artificially required.
        body["email"] = "  qa-person@example.invalid  "; body["phone"] = "  1234567890  ";
        body["branchId"] = branch; body["dateOfBirth"] = "2000-01-02";
        if (kind == "teachers")
        {
            body["specialties"] = "  Piano  "; body["qualifications"] = "  Synthetic Diploma  ";
            body["employmentType"] = "  PartTime  "; body["joiningDate"] = "2026-01-02";
            body["addressLine1"] = "  Synthetic Street  "; body["city"] = "  QA City  ";
            body["state"] = "  QA State  "; body["postalCode"] = "  123456  ";
            body["certificationsJson"] = "[]"; body["availabilityJson"] = "{}"; body["compensationJson"] = "{}";
        }
        return body;
    }

    private static async Task CheckPeopleCreationAsync(QaApiFactory factory, HttpClient client, string? token, string? email,
        string kind, string path, Dictionary<string, object?> body, HttpStatusCode expected, bool allowCommittedFault = false, string label = "")
    {
        await Task.Delay(650); // Preserve the real rate limiter.
        client.DefaultRequestHeaders.Authorization = token is null ? null : new AuthenticationHeaderValue("Bearer", token);
        var before = await PeopleStateAsync(factory);
        var identityBefore = await PeopleIdentityStateAsync(factory);
        var financeBefore = await AccessFinancialSnapshotAsync(factory);
        var countBefore = await PeopleCountAsync(factory, kind);
        var auditBefore = await AccessAuditCountAsync(factory);
        using var response = await client.PostAsJsonAsync(path, body);
        RequireFinanceStatus(response, expected, "people " + label);
        var delta = await PeopleCountAsync(factory, kind) - countBefore;
        var auditDelta = await AccessAuditCountAsync(factory) - auditBefore;
        var saved = expected == HttpStatusCode.Created || allowCommittedFault;
        if (delta != (saved ? 1 : 0) || auditDelta != (expected == HttpStatusCode.Created ? 1 : 0) ||
            identityBefore != await PeopleIdentityStateAsync(factory) || financeBefore != await AccessFinancialSnapshotAsync(factory) ||
            !saved && before != await PeopleStateAsync(factory))
            throw new InvalidOperationException("People create unexpected persisted state: " + label);
        if (saved)
        {
            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var name = (string)body["firstName"]!;
            var row = kind == "students" ? JsonSerializer.SerializeToElement(await db.Students.AsNoTracking().SingleAsync(x => x.FirstName == name.Trim())) :
                JsonSerializer.SerializeToElement(await db.Teachers.AsNoTracking().SingleAsync(x => x.FirstName == name.Trim()));
            foreach (var pair in body)
            {
                var key = char.ToUpperInvariant(pair.Key[0]) + pair.Key[1..];
                var value = row.GetProperty(key);
                var text = pair.Value?.ToString()?.Trim();
                if (text is null ? value.ValueKind != JsonValueKind.Null : value.ToString() != text)
                    throw new InvalidOperationException("People optional/trimmed field mismatch: " + pair.Key);
            }
            if (!body.ContainsKey("email"))
                foreach (var key in new[] { "Email", "Phone", "BranchId", "DateOfBirth" })
                    if (row.GetProperty(key).ValueKind != JsonValueKind.Null) throw new InvalidOperationException("People omitted nullable field was not null: " + key);
            if (!row.GetProperty("IsActive").GetBoolean()) throw new InvalidOperationException("People active default incorrect.");
            if (expected == HttpStatusCode.Created)
            {
                using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                var id = row.GetProperty("Id").GetGuid();
                if (payload.RootElement.GetProperty("id").GetGuid() != id || response.Headers.Location?.ToString() != path + "/" + id ||
                    payload.RootElement.GetProperty("firstName").GetString() != name.Trim())
                    throw new InvalidOperationException("People response ID/Location mismatch.");
                var identity = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
                var actorId = await identity.Users.Where(x => x.Email == email).Select(x => x.Id).SingleAsync();
                var log = await db.AuditLogs.AsNoTracking().OrderByDescending(x => x.OccurredAtUtc).FirstAsync();
                var controller = kind == "students" ? "Students" : "Teachers";
                if (log.ActorUserId != actorId || log.Action != "POST " + controller || log.AcademyId != row.GetProperty("AcademyId").GetGuid() || !log.MetadataJson!.Contains(path))
                    throw new InvalidOperationException("People success audit attribution mismatch.");
            }
        }
        Console.WriteLine($"PEOPLE CASE {label} {(allowCommittedFault ? "REPRODUCED" : "PASS")}: HTTP={(int)expected}, people delta={delta}, audit delta={auditDelta}; captured Identity/finance/notifications unchanged; {(saved ? "fresh row/optional fields verified" : "all captured state unchanged")}.");
    }

    private static async Task<int> PeopleCountAsync(QaApiFactory factory, string kind)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
        return kind == "students" ? await db.Students.CountAsync() : await db.Teachers.CountAsync();
    }

    private static async Task<string> PeopleIdentityStateAsync(QaApiFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        // In-memory comparison only; never emit credentials or password hashes.
        return JsonSerializer.Serialize(new { Users = await db.Users.AsNoTracking().OrderBy(x => x.Id)
            .Select(x => new { x.Id, x.AcademyId, x.StudentId, x.TeacherId, x.IsActive, x.DisplayName, x.Email }).ToListAsync(),
            Roles = await db.Roles.AsNoTracking().OrderBy(x => x.Id).Select(x => new { x.Id, x.Name, x.AcademyId, x.PermissionsJson }).ToListAsync(),
            Links = await db.UserRoles.AsNoTracking().OrderBy(x => x.UserId).ThenBy(x => x.RoleId).ToListAsync() });
    }

    private static async Task<string> PeopleStateAsync(QaApiFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
        return JsonSerializer.Serialize(new { Students = await db.Students.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
            Teachers = await db.Teachers.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
            Branches = await db.Branches.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
            Audits = await db.AuditLogs.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
            Identity = await PeopleIdentityStateAsync(factory), Finance = await AccessFinancialSnapshotAsync(factory) });
    }
}
