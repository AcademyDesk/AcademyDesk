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
    private sealed record GovernanceState(string Stable, List<AdminWorkItem> Tasks, List<AuditLog> Audits);
    private static async Task<GovernanceState> GovernanceStateAsync(QaApiFactory factory)
    {
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
        var identity = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var stable = JsonSerializer.Serialize(new {
            Students = await db.Students.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), Teachers = await db.Teachers.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
            Branches = await db.Branches.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), Academies = await db.Academies.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
            Settings = await db.AcademyFinanceSettings.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
            Grants = await identity.AccessGrants.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
            Identity = await PeopleIdentityStateAsync(factory), Finance = await AccessFinancialSnapshotAsync(factory) });
        return new(stable, await db.AdminWorkItems.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), await db.AuditLogs.AsNoTracking().OrderBy(x => x.Id).ToListAsync());
    }

    private static async Task VerifyFinanceGovernanceAsync(QaApiFactory factory, HttpClient client, QaRunManifest manifest)
    {
        Guid academyA, academyB, student, invoiceId, ownTask, completedTask, cancelledTask, foreignTask, otherTask;
        string originalModules;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var academy = await db.Academies.SingleAsync(x => x.Name == "Synthetic Academy A"); academyA = academy.Id; originalModules = academy.EnabledModulesJson;
            academyB = await db.Academies.Where(x => x.Name == "Synthetic Academy B").Select(x => x.Id).SingleAsync();
            student = await db.Students.Where(x => x.AcademyId == academyA).Select(x => x.Id).SingleAsync();
            var ownInvoice = new Invoice { AcademyId = academyA, StudentId = student, InvoiceNumber = "QA-GOV", TotalAmount = 100m, DueDate = new DateOnly(2026, 9, 1) };
            invoiceId = ownInvoice.Id; db.Invoices.Add(ownInvoice);
            var identity = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            var assignee = await identity.Users.Where(x => x.Email == "qa-teacher-a@example.invalid").Select(x => x.Id).SingleAsync();
            AdminWorkItem Fixture(Guid academy, string type, string status) => new() { AcademyId = academy, Type = type, Title = "Synthetic " + status,
                Description = null, Priority = "High", Status = status, AssignedUserId = assignee, EntityType = "Invoice", EntityId = invoiceId,
                DueAtUtc = new DateTime(2026, 10, 4), CompletedAtUtc = status == "Completed" ? new DateTime(2026, 9, 30) : null,
                EscalationStage = "Initial", PromisedPaymentDate = new DateOnly(2026, 10, 1) };
            var open = Fixture(academyA, "Collections", "Open"); ownTask = open.Id;
            var done = Fixture(academyA, "Collections", "Completed"); completedTask = done.Id;
            var cancel = Fixture(academyA, "Collections", "Cancelled"); cancelledTask = cancel.Id;
            var foreign = Fixture(academyB, "Collections", "Open"); foreign.Title = "FOREIGN-GOV-MARKER"; foreignTask = foreign.Id;
            var other = Fixture(academyA, "Operations", "Open"); other.Title = "PRIVATE-OPS-MARKER"; otherTask = other.Id;
            db.AdminWorkItems.AddRange(open, done, cancel, foreign, other); await db.SaveChangesAsync();
        }
        var root = $"/api/academies/{academyA}"; var route = root + "/finance-governance/collection-tasks";
        var count = 0; var actors = new Dictionary<string, (string Token, Guid User)>(); var usersByToken = new Dictionary<string, Guid>();
        async Task<string> Send(string label, string? token, string method, string url, object? body, HttpStatusCode expected,
            Guid? edited = null, string? stage = null, DateOnly? promise = null, bool created = false, bool projection = false)
        {
            await Task.Delay(650); client.DefaultRequestHeaders.Authorization = token is null ? null : new AuthenticationHeaderValue("Bearer", token);
            var before = await GovernanceStateAsync(factory);
            using var request = new HttpRequestMessage(new HttpMethod(method), url);
            if (body is not null) request.Content = JsonContent.Create(body);
            using var response = await client.SendAsync(request); RequireFinanceStatus(response, expected, "governance " + label);
            var json = await response.Content.ReadAsStringAsync(); var after = await GovernanceStateAsync(factory);
            if (edited.HasValue || created)
            {
                if (before.Stable != after.Stable || after.Audits.Count != before.Audits.Count + 1) throw new InvalidOperationException("Governance write crossed field/audit boundary: " + label);
                var addedAudit = after.Audits.Single(x => before.Audits.All(y => y.Id != x.Id));
                if (addedAudit.ActorUserId != usersByToken[token!] || addedAudit.AcademyId != academyA || addedAudit.Action != method + " FinanceGovernance" ||
                    !addedAudit.MetadataJson!.Contains(url, StringComparison.Ordinal)) throw new InvalidOperationException("Governance actor/route/audit mismatch");
                if (edited.HasValue)
                {
                    var item = before.Tasks.Single(x => x.Id == edited); item.EscalationStage = stage; item.PromisedPaymentDate = promise;
                    if (JsonSerializer.Serialize(before.Tasks) != JsonSerializer.Serialize(after.Tasks)) throw new InvalidOperationException("Governance edit changed assignment/link/status/other fields");
                    VerifyCollectionTaskProjection(json, after.Tasks.Single(x => x.Id == edited));
                }
                else
                {
                    using var parsed = JsonDocument.Parse(json); var id = parsed.RootElement.GetProperty("id").GetGuid();
                    var added = after.Tasks.Single(x => x.Id == id);
                    if (after.Tasks.Count != before.Tasks.Count + 1 || added.AcademyId != academyA || added.Type != "Collections" || added.EntityId != invoiceId ||
                        added.EntityType != "Invoice" || added.AssignedUserId is not null || added.Description != "Synthetic follow-up") throw new InvalidOperationException("Governance creation persistence mismatch");
                    if (JsonSerializer.Serialize(before.Tasks) != JsonSerializer.Serialize(after.Tasks.Where(x => x.Id != id))) throw new InvalidOperationException("Governance creation changed another task");
                }
            }
            else if (JsonSerializer.Serialize(before) != JsonSerializer.Serialize(after)) throw new InvalidOperationException("Governance read/rejection/fault changed captured state: " + label);
            if (projection)
            {
                using var parsed = JsonDocument.Parse(json); var rows = parsed.RootElement.EnumerateArray().ToArray();
                // SQL Server uniqueidentifier ordering is not CLR Guid ordering.
                // Read the expected ordered rows through the actual SQL provider.
                using var projectionScope = factory.Services.CreateScope();
                var expectedTasks = await projectionScope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>().AdminWorkItems.AsNoTracking()
                    .Where(x => x.AcademyId == academyA && x.Type == "Collections").OrderBy(x => x.DueAtUtc)
                    .ThenByDescending(x => x.Priority).ThenBy(x => x.Id).ToArrayAsync();
                if (rows.Length != expectedTasks.Length) throw new InvalidOperationException("Governance list scoping mismatch");
                for (var i = 0; i < rows.Length; i++) VerifyCollectionTaskProjection(rows[i].GetRawText(), expectedTasks[i]);
                if (json.Contains("FOREIGN-GOV-MARKER", StringComparison.Ordinal) || json.Contains("PRIVATE-OPS-MARKER", StringComparison.Ordinal)) throw new InvalidOperationException("Governance unrelated task leak");
            }
            Console.WriteLine($"GOVERNANCE CASE {label} PASS: HTTP={(int)expected}; {(edited.HasValue ? "only stage/promise changed; assignment/link/status preserved; one actor/route audit" : created ? "one own invoice-linked task and audit persisted" : "captured state unchanged")}{(projection ? "; exact nine-field own Collections list" : "")}."); count++; return json;
        }
        async Task<string> LoginActor(string role, string permissions = "[]")
        {
            var email = role == "AcademyAdmin" ? "qa-admin-a@example.invalid" : role == "Teacher" ? "qa-teacher-a@example.invalid" : "qa-gov-" + role.ToLowerInvariant() + "@example.invalid";
            if (role is not ("AcademyAdmin" or "Teacher")) await CreateAccessActorAsync(factory, email, role, academyA, role == "Student" ? student : null, permissions);
            var token = await LoginAsync(client, email, "Synthetic!39Ab");
            using var scope = factory.Services.CreateScope(); var id = await scope.ServiceProvider.GetRequiredService<IdentityDbContext>().Users.Where(x => x.Email == email).Select(x => x.Id).SingleAsync();
            actors[role] = (token, id); usersByToken[token] = id; return token;
        }
        var patch = new { escalationStage = "Reminder", promisedPaymentDate = (DateOnly?)null };
        foreach (var actor in new[] { ("AcademyAdmin", true), ("Owner", true), ("FinanceUser", true), ("QA-GovFinance", true),
            ("Teacher", false), ("Student", false), ("FrontDesk", false), ("Operations", false), ("QA-GovStudents", false), ("QA-GovNone", false) })
        {
            var token = await LoginActor(actor.Item1, actor.Item1 == "QA-GovFinance" ? "[\"finance.manage\"]" : actor.Item1 == "QA-GovStudents" ? "[\"students.manage\"]" : "[]");
            await Send(actor.Item1 + "/list", token, "GET", route, null, actor.Item2 ? HttpStatusCode.OK : HttpStatusCode.Forbidden, projection: actor.Item2);
            await Send(actor.Item1 + "/edit", token, "PATCH", route + "/" + ownTask, patch, actor.Item2 ? HttpStatusCode.OK : HttpStatusCode.Forbidden,
                edited: actor.Item2 ? ownTask : null, stage: "Reminder");
        }
        var finance = actors["FinanceUser"].Token; var admin = actors["AcademyAdmin"].Token;
        foreach (var dependency in new[] { "finance-adjustments", "finance-governance/collections", "finance-governance/settings", "academies" })
            await Send("FinanceUser/prerequisite-" + dependency, finance, "GET", dependency == "academies" ? "/api/academies" : root + "/" + dependency, null, HttpStatusCode.OK);
        foreach (var actor in new[] { "FinanceUser", "QA-GovFinance" })
        {
            await Send(actor + "/generic-list-denied", actors[actor].Token, "GET", root + "/admin-work-items?type=Collections", null, HttpStatusCode.Forbidden);
            await Send(actor + "/generic-edit-denied", actors[actor].Token, "PATCH", root + $"/admin-work-items/{ownTask}/collections", patch, HttpStatusCode.Forbidden);
        }
        foreach (var token in new[] { admin, finance }) foreach (var method in new[] { "GET", "PATCH" })
            await Send((token == admin ? "Admin" : "FinanceUser") + "/foreign-route-" + method, token, method, route.Replace(academyA.ToString(), academyB.ToString()) + (method == "PATCH" ? "/" + foreignTask : ""), method == "PATCH" ? patch : null, HttpStatusCode.Forbidden);
        foreach (var target in new[] { ("foreign-task", foreignTask), ("non-collections", otherTask), ("missing-task", Guid.NewGuid()) })
            await Send(target.Item1, finance, "PATCH", route + "/" + target.Item2, patch, HttpStatusCode.NotFound);
        foreach (var stage in new string?[] { null, "", "Unknown", "reminder", "Reminder " })
            await Send("invalid-stage-" + (stage is null ? "null" : stage == "" ? "empty" : stage.Replace(" ", "space")), finance, "PATCH", route + "/" + ownTask, new { escalationStage = stage, promisedPaymentDate = "2026-11-01" }, HttpStatusCode.BadRequest);
        foreach (var stage in new[] { "Initial", "Reminder", "ManagerReview", "FinalNotice" })
            await Send("stage-" + stage, finance, "PATCH", route + "/" + ownTask, new { escalationStage = stage, promisedPaymentDate = "2026-11-01" }, HttpStatusCode.OK, ownTask, stage, new DateOnly(2026, 11, 1));
        await Send("optional-null", finance, "PATCH", route + "/" + ownTask, patch, HttpStatusCode.OK, ownTask, "Reminder");
        await Send("optional-omitted-and-overposting", finance, "PATCH", route + "/" + ownTask,
            new { escalationStage = "FinalNotice", assignedUserId = Guid.NewGuid(), academyId = academyB, type = "Operations", entityId = Guid.NewGuid(), status = "Cancelled", title = "IGNORED" }, HttpStatusCode.OK, ownTask, "FinalNotice");
        await Send("invalid-date", finance, "PATCH", route + "/" + ownTask, new { escalationStage = "Reminder", promisedPaymentDate = "not-a-date" }, HttpStatusCode.BadRequest);
        await Send("missing-stage", finance, "PATCH", route + "/" + ownTask, new { promisedPaymentDate = "2026-11-01" }, HttpStatusCode.BadRequest);
        await Send("malformed-id", finance, "PATCH", route + "/bad-guid", patch, HttpStatusCode.NotFound);
        foreach (var method in new[] { "GET", "PATCH" }) await Send("anonymous-" + method, null, method, route + (method == "PATCH" ? "/" + ownTask : ""), method == "PATCH" ? patch : null, HttpStatusCode.Unauthorized);
        foreach (var target in new[] { ("completed-preserved", completedTask), ("cancelled-preserved", cancelledTask) })
            await Send(target.Item1, finance, "PATCH", route + "/" + target.Item2, patch, HttpStatusCode.OK, target.Item2, "Reminder");

        var grantToken = await LoginActor("QA-GovGrant"); var grantUser = actors["QA-GovGrant"].User;
        await Send("grant/absent-read", grantToken, "GET", route, null, HttpStatusCode.Forbidden);
        await Send("grant/absent-edit", grantToken, "PATCH", route + "/" + ownTask, patch, HttpStatusCode.Forbidden);
        Guid grantId;
        using (var scope = factory.Services.CreateScope())
        {
            var identity = scope.ServiceProvider.GetRequiredService<IdentityDbContext>(); var grant = new AccessGrant { AcademyId = academyA, UserId = grantUser, GrantedByUserId = grantUser,
                PermissionsJson = "[\"finance.manage\"]", ExpiresAtUtc = DateTimeOffset.UtcNow.AddHours(1), Reason = "Synthetic governance grant" };
            identity.AccessGrants.Add(grant); await identity.SaveChangesAsync(); grantId = grant.Id;
        }
        foreach (var state in new[] { "valid", "wrong-academy", "expired", "revoked" })
        {
            using (var scope = factory.Services.CreateScope())
            {
                var identity = scope.ServiceProvider.GetRequiredService<IdentityDbContext>(); var grant = await identity.AccessGrants.SingleAsync(x => x.Id == grantId);
                grant.AcademyId = state == "wrong-academy" ? academyB : academyA; grant.ExpiresAtUtc = state == "expired" ? DateTimeOffset.UtcNow.AddHours(-1) : DateTimeOffset.UtcNow.AddHours(1);
                grant.RevokedAtUtc = state == "revoked" ? DateTimeOffset.UtcNow : null; await identity.SaveChangesAsync();
            }
            var allowed = state == "valid";
            await Send("grant/" + state + "-read", grantToken, "GET", route, null, allowed ? HttpStatusCode.OK : HttpStatusCode.Forbidden, projection: allowed);
            await Send("grant/" + state + "-edit", grantToken, "PATCH", route + "/" + ownTask, patch, allowed ? HttpStatusCode.OK : HttpStatusCode.Forbidden, allowed ? ownTask : null, "Reminder");
        }
        foreach (var enabled in new[] { false, true })
        {
            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); (await db.Academies.SingleAsync(x => x.Id == academyA)).EnabledModulesJson = enabled ? "[\"FinanceControls\"]" : "[\"Finance\"]"; await db.SaveChangesAsync();
            }
            foreach (var actor in new[] { "AcademyAdmin", "FinanceUser" })
            {
                await Send(actor + (enabled ? "/Controls-only-read" : "/Controls-disabled-read"), actors[actor].Token, "GET", route, null, enabled ? HttpStatusCode.OK : HttpStatusCode.Forbidden, projection: enabled);
                await Send(actor + (enabled ? "/Controls-only-edit" : "/Controls-disabled-edit"), actors[actor].Token, "PATCH", route + "/" + ownTask, patch, enabled ? HttpStatusCode.OK : HttpStatusCode.Forbidden, enabled ? ownTask : null, "Reminder");
            }
        }
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); var academy = await db.Academies.SingleAsync(x => x.Id == academyA);
            academy.EnabledModulesJson = originalModules; academy.IsActive = false; await db.SaveChangesAsync();
        }
        await Send("inactive-academy/read", finance, "GET", route, null, HttpStatusCode.Forbidden);
        await Send("inactive-academy/edit", finance, "PATCH", route + "/" + ownTask, patch, HttpStatusCode.Forbidden);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); (await db.Academies.SingleAsync(x => x.Id == academyA)).IsActive = true; await db.SaveChangesAsync();
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(); var user = (await users.FindByIdAsync(actors["FinanceUser"].User.ToString()))!;
            user.IsActive = false; if (!(await users.UpdateAsync(user)).Succeeded) throw new InvalidOperationException("Inactive fixture failed");
        }
        await Send("inactive-user/read", finance, "GET", route, null, HttpStatusCode.Forbidden);
        await Send("inactive-user/edit", finance, "PATCH", route + "/" + ownTask, patch, HttpStatusCode.Forbidden);
        using (var scope = factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(); var user = (await users.FindByIdAsync(actors["FinanceUser"].User.ToString()))!;
            user.IsActive = true; if (!(await users.UpdateAsync(user)).Succeeded) throw new InvalidOperationException("Restore fixture failed");
        }
        await Send("reactivated-user", finance, "GET", route, null, HttpStatusCode.OK, projection: true);
        var createRoute = root + $"/finance-governance/collections/{invoiceId}/follow-up";
        var createBody = new { note = "Synthetic follow-up", priority = "High", assignedUserId = (Guid?)null, dueAtUtc = (DateTime?)null };
        await Send("follow-up-create", finance, "POST", createRoute, createBody, HttpStatusCode.OK, created: true);
        await Send("follow-up-visible", finance, "GET", route, null, HttpStatusCode.OK, projection: true);
        await SetAuditInsertDeniedAsync(manifest, true);
        try
        {
            await Send("audit-fault-edit", finance, "PATCH", route + "/" + ownTask, new { escalationStage = "FinalNotice", promisedPaymentDate = "2026-12-01" }, HttpStatusCode.InternalServerError);
            await Send("audit-fault-create", finance, "POST", createRoute, createBody, HttpStatusCode.InternalServerError);
        }
        finally { await SetAuditInsertDeniedAsync(manifest, false); }
        await Send("audit-recovery-edit", finance, "PATCH", route + "/" + ownTask, patch, HttpStatusCode.OK, ownTask, "Reminder");
        await Send("audit-recovery-create", finance, "POST", createRoute, createBody, HttpStatusCode.OK, created: true);
        Console.WriteLine($"GOVERNANCE REGRESSION PASS: {count} primary cases; finance Collections read/edit access, tenant/type/validation/module/grant boundaries, only stage/promise changed, assignments preserved, audit-fault rollback and recovery. Browser/full critical/other finance policy gaps remain OPEN.");
    }

    private static void VerifyCollectionTaskProjection(string json, AdminWorkItem item)
    {
        using var parsed = JsonDocument.Parse(json);
        var keys = new[] { "description", "dueAtUtc", "escalationStage", "id", "priority", "promisedPaymentDate", "status", "title", "type" };
        var row = parsed.RootElement;
        var summary = row.Deserialize<CollectionTaskSummary>(new JsonSerializerOptions(JsonSerializerDefaults.Web));
        if (!row.EnumerateObject().Select(x => x.Name).OrderBy(x => x, StringComparer.Ordinal).SequenceEqual(keys) ||
            summary != new CollectionTaskSummary(item.Id, item.Type, item.Title, item.Description, item.Priority, item.Status, item.DueAtUtc, item.EscalationStage, item.PromisedPaymentDate))
            throw new InvalidOperationException("Governance field projection mismatch");
    }
}
