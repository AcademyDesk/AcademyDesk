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
    private static async Task VerifyWorkItemIntegrityAsync(QaApiFactory factory, HttpClient client, QaRunManifest manifest)
    {
        Guid academy, foreign, student, invoice, document, otherInvoice, otherDocument, sentinel;
        var count = 0;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            academy = await db.Academies.Where(x => x.Name == "Synthetic Academy A").Select(x => x.Id).SingleAsync();
            foreign = await db.Academies.Where(x => x.Name == "Synthetic Academy B").Select(x => x.Id).SingleAsync();
            student = await db.Students.Where(x => x.AcademyId == academy).Select(x => x.Id).SingleAsync();
            foreach (var a in await db.Academies.ToArrayAsync()) a.EnabledModulesJson = "[\"Finance\",\"FinanceControls\",\"AccessGovernance\"]";
            var local = new Invoice { AcademyId = academy, StudentId = student, InvoiceNumber = "QA-WORK-LOCAL", TotalAmount = 1000 };
            var other = new Invoice { AcademyId = foreign, StudentId = await db.Students.Where(x => x.AcademyId == foreign).Select(x => x.Id).SingleAsync(), InvoiceNumber = "QA-WORK-FOREIGN", TotalAmount = 1000 };
            var ownDoc = new PersonDocument { AcademyId = academy, StudentId = student, DocumentType = "Synthetic", FileName = "qa-local.txt" };
            var otherDoc = new PersonDocument { AcademyId = foreign, DocumentType = "Synthetic", FileName = "qa-foreign.txt" };
            var otherWork = new AdminWorkItem { AcademyId = foreign, Type = "Collections", Title = "Foreign sentinel" };
            db.AddRange(local, other, ownDoc, otherDoc, otherWork); await db.SaveChangesAsync();
            invoice = local.Id; otherInvoice = other.Id; document = ownDoc.Id; otherDocument = otherDoc.Id; sentinel = otherWork.Id;
        }
        var staff = await CreateAccessActorAsync(factory, "qa-work-staff@example.invalid", "Operations", academy);
        var portalStudent = await CreateAccessActorAsync(factory, "qa-work-student@example.invalid", "Student", academy, student);
        await CreateAccessActorAsync(factory, "qa-work-finance@example.invalid", "FinanceUser", academy);
        var admin = await LoginAsync(client, "qa-admin-a@example.invalid", "Synthetic!39Ab");
        var otherAdmin = await LoginAsync(client, "qa-admin-b@example.invalid", "Synthetic!39Ab");
        var teacher = await LoginAsync(client, "qa-teacher-a@example.invalid", "Synthetic!39Ab");
        var studentToken = await LoginAsync(client, "qa-work-student@example.invalid", "Synthetic!39Ab");
        var finance = await LoginAsync(client, "qa-work-finance@example.invalid", "Synthetic!39Ab");
        client.DefaultRequestHeaders.Authorization = null;
        var root = $"/api/academies/{academy}/admin-work-items";
        var follow = $"/api/academies/{academy}/finance-governance/collections/{invoice}/follow-up";
        var valid = new CreateWorkItem("Operations", " Work integrity ", null, null, null, null, null, null);
        async Task<string> Snapshot()
        {
            using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); var identity = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            return JsonSerializer.Serialize(new { Work = await db.AdminWorkItems.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(), Audits = await db.AuditLogs.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(),
                Invoices = await db.Invoices.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(), Payments = await db.Payments.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(), Documents = await db.PersonDocuments.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(),
                Users = await identity.Users.AsNoTracking().OrderBy(x => x.Id).Select(x => new { x.Id, x.AcademyId, x.IsActive, x.IsPlatformOwner, x.LockoutEnd }).ToArrayAsync(),
                Roles = await identity.UserRoles.AsNoTracking().OrderBy(x => x.UserId).ThenBy(x => x.RoleId).ToArrayAsync() });
        }
        async Task<HttpResponseMessage> Send(HttpMethod method, string path, object? body, string? token)
        {
            await Task.Delay(650); using var request = new HttpRequestMessage(method, path);
            if (body is not null) request.Content = JsonContent.Create(body);
            if (token is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return await client.SendAsync(request);
        }
        void Pass(string label) { count++; Console.WriteLine("PASS: WORKITEM " + label); }
        async Task NoWrite(HttpMethod method, string path, object? body, string? token, HttpStatusCode expected, string label)
        {
            var before = await Snapshot(); using var response = await Send(method, path, body, token); RequireFinanceStatus(response, expected, label);
            if (before != await Snapshot()) throw new InvalidOperationException("Work item rejected/read request changed snapshot: " + label);
            Pass(label + " exact status/no-write");
        }
        async Task<Guid> Created(string path, object body, string token, string type, string? entityType, Guid? entityId, Guid? assignee)
        {
            using var before = JsonDocument.Parse(await Snapshot()); using var response = await Send(HttpMethod.Post, path, body, token);
            RequireFinanceStatus(response, HttpStatusCode.OK, "valid work creation"); using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync()); var id = json.RootElement.GetProperty("id").GetGuid();
            using var after = JsonDocument.Parse(await Snapshot());
            foreach (var field in new[] { "Invoices", "Payments", "Documents", "Users", "Roles" })
                if (before.RootElement.GetProperty(field).GetRawText() != after.RootElement.GetProperty(field).GetRawText()) throw new InvalidOperationException("Work creation changed unrelated " + field);
            var oldRows = before.RootElement.GetProperty("Work").EnumerateArray().ToArray(); var rows = after.RootElement.GetProperty("Work").EnumerateArray().ToArray();
            foreach (var row in oldRows) if (rows.Single(x => x.GetProperty("Id").GetGuid() == row.GetProperty("Id").GetGuid()).GetRawText() != row.GetRawText()) throw new InvalidOperationException("Work creation changed old/sentinel row.");
            var added = rows.Single(x => x.GetProperty("Id").GetGuid() == id);
            Guid? NullableId(string field) => added.GetProperty(field).ValueKind == JsonValueKind.Null ? null : added.GetProperty(field).GetGuid();
            if (rows.Length != oldRows.Length + 1 || added.GetProperty("AcademyId").GetGuid() != academy || added.GetProperty("Type").GetString() != type ||
                added.GetProperty("Status").GetString() != "Open" || added.GetProperty("EntityType").GetString() != entityType || NullableId("EntityId") != entityId || NullableId("AssignedUserId") != assignee ||
                added.GetProperty("DueAtUtc").ValueKind != JsonValueKind.Null) throw new InvalidOperationException("Work persisted identity/defaults differ.");
            var auditsBefore = before.RootElement.GetProperty("Audits").EnumerateArray().ToArray();
            var audit = after.RootElement.GetProperty("Audits").EnumerateArray().Where(x => !auditsBefore.Any(old => old.GetProperty("Id").GetGuid() == x.GetProperty("Id").GetGuid())).Single();
            if (audit.GetProperty("AcademyId").GetGuid() != academy || audit.GetProperty("ActorUserId").ValueKind == JsonValueKind.Null || audit.GetProperty("Action").GetString() != (path == root ? "POST AdminWorkItems" : "POST FinanceGovernance")) throw new InvalidOperationException("Work success audit identity differs.");
            Pass("valid " + type + " create/SQL/reference/audit"); return id;
        }
        foreach (var mode in new[] { "foreign", "missing", "empty", "inactive", "platform", "locked", "Teacher", "Student", "Guardian", "mixed" })
        {
            var id = mode == "empty" ? Guid.Empty : mode == "missing" ? Guid.NewGuid() : mode == "Student" ? portalStudent :
                await CreateAccessActorAsync(factory, "qa-work-" + mode.ToLowerInvariant() + "@example.invalid", mode is "mixed" or "Teacher" ? "Teacher" : mode == "Guardian" ? "Guardian" : "QA-Work-" + mode, mode == "foreign" ? foreign : academy);
            if (mode is not ("missing" or "empty" or "Student"))
            {
                using var scope = factory.Services.CreateScope(); var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(); var user = await users.FindByIdAsync(id.ToString()) ?? throw new InvalidOperationException();
                if (mode == "mixed" && !(await users.AddToRoleAsync(user, "Operations")).Succeeded) throw new InvalidOperationException("Mixed fixture failed.");
                user.IsActive = mode != "inactive"; user.IsPlatformOwner = mode == "platform";
                if (mode == "locked") { user.LockoutEnabled = true; user.LockoutEnd = DateTimeOffset.UtcNow.AddDays(1); }
                if (!(await users.UpdateAsync(user)).Succeeded) throw new InvalidOperationException("Work actor fixture failed.");
            }
            await NoWrite(HttpMethod.Post, root, valid with { AssignedUserId = id }, admin, HttpStatusCode.BadRequest, "invalid assignee " + mode);
            await NoWrite(HttpMethod.Post, follow, new CollectionFollowUpRequest(null, null, id, null), finance, HttpStatusCode.BadRequest, "follow-up assignee " + mode);
        }
        foreach (var mode in new[] { "type-only", "id-only", "unknown", "case", "empty", "missing", "foreign", "wrong-table", "collections-document", "compliance-invoice" })
        {
            var r = valid with { EntityType = "Invoice", EntityId = invoice };
            r = mode switch { "type-only" => r with { EntityId = null }, "id-only" => r with { EntityType = null }, "unknown" => r with { EntityType = "Student" }, "case" => r with { EntityType = "invoice" },
                "empty" => r with { EntityId = Guid.Empty }, "missing" => r with { EntityId = Guid.NewGuid() }, "foreign" => r with { EntityId = otherInvoice }, "wrong-table" => r with { EntityId = document },
                "collections-document" => r with { Type = "Collections", EntityType = "PersonDocument", EntityId = document }, "compliance-invoice" => r with { Type = "Compliance" }, _ => throw new InvalidOperationException() };
            await NoWrite(HttpMethod.Post, root, r, admin, HttpStatusCode.BadRequest, "link " + mode);
        }
        foreach (var id in new[] { otherDocument, Guid.NewGuid() }) await NoWrite(HttpMethod.Post, root, valid with { EntityType = "PersonDocument", EntityId = id }, admin, HttpStatusCode.BadRequest, "invalid document link");
        foreach (var r in new[] { valid with { Type = new('x', 61) }, valid with { Title = new('x', 241) }, valid with { Description = new('x', 4001) }, valid with { Priority = "Unknown" } })
            await NoWrite(HttpMethod.Post, root, r, admin, HttpStatusCode.BadRequest, "field/priority guard");
        foreach (var r in new[] { new CollectionFollowUpRequest(null, "Unknown", null, null), new CollectionFollowUpRequest(new('x', 4001), null, null, null) })
            await NoWrite(HttpMethod.Post, follow, r, finance, HttpStatusCode.BadRequest, "follow-up field guard");
        foreach (var actor in new (string? Token, HttpStatusCode Status)[] { (null, HttpStatusCode.Unauthorized), (teacher, HttpStatusCode.Forbidden), (studentToken, HttpStatusCode.Forbidden), (finance, HttpStatusCode.Forbidden), (otherAdmin, HttpStatusCode.Forbidden) })
            foreach (var method in new[] { HttpMethod.Get, HttpMethod.Post, HttpMethod.Patch }) await NoWrite(method, method == HttpMethod.Patch ? root + "/" + sentinel + "/status" : root,
                method == HttpMethod.Get ? null : method == HttpMethod.Post ? valid : new WorkStatus("Completed"), actor.Token, actor.Status, "queue actor " + method);
        foreach (var method in new[] { HttpMethod.Get, HttpMethod.Post, HttpMethod.Patch }) await NoWrite(method, $"/api/academies/{foreign}/admin-work-items" + (method == HttpMethod.Patch ? "/" + sentinel + "/status" : ""),
            method == HttpMethod.Get ? null : method == HttpMethod.Post ? valid : new WorkStatus("Completed"), admin, HttpStatusCode.Forbidden, "foreign route " + method);
        await NoWrite(HttpMethod.Patch, root + "/" + sentinel + "/status", new WorkStatus("Completed"), admin, HttpStatusCode.NotFound, "foreign work status");
        await NoWrite(HttpMethod.Patch, root + "/" + sentinel + "/collections", new CollectionStateRequest("Reminder", null), admin, HttpStatusCode.NotFound, "foreign collection state");
        await NoWrite(HttpMethod.Patch, root + "/" + Guid.NewGuid() + "/status", new WorkStatus("Completed"), admin, HttpStatusCode.NotFound, "missing work status");
        var plain = await Created(root, valid, admin, "Operations", null, null, null);
        await Created(root, valid with { AssignedUserId = staff }, admin, "Operations", null, null, staff);
        var collection = await Created(root, valid with { Type = "Collections", EntityType = "Invoice", EntityId = invoice }, admin, "Collections", "Invoice", invoice, null);
        await Created(root, valid with { Type = "Compliance", EntityType = "PersonDocument", EntityId = document }, admin, "Compliance", "PersonDocument", document, null);
        await Created(follow, new CollectionFollowUpRequest(null, null, staff, null), finance, "Collections", "Invoice", invoice, staff);
        await Created(follow, new CollectionFollowUpRequest(null, null, null, null), finance, "Collections", "Invoice", invoice, null);
        foreach (var query in new[] { "?status=Open", "?type=Collections" })
        {
            var before = await Snapshot(); using var response = await Send(HttpMethod.Get, root + query, null, admin); RequireFinanceStatus(response, HttpStatusCode.OK, "filtered queue");
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync()); var rows = json.RootElement.EnumerateArray().ToArray();
            if (rows.Length == 0 || rows.Any(x => x.GetProperty("academyId").GetGuid() != academy || (query.Contains("status") ? x.GetProperty("status").GetString() != "Open" : x.GetProperty("type").GetString() != "Collections")) || before != await Snapshot()) throw new InvalidOperationException("Queue filter leaked/mutated.");
            Pass("scoped filtered queue read");
        }
        foreach (var status in new[] { "Completed", "Open" })
        {
            using var response = await Send(HttpMethod.Patch, root + "/" + plain + "/status", new WorkStatus(status), admin); RequireFinanceStatus(response, HttpStatusCode.OK, "existing status");
            using var scope = factory.Services.CreateScope(); var row = await scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>().AdminWorkItems.AsNoTracking().SingleAsync(x => x.Id == plain);
            if (row.Status != status || (status == "Completed" ? row.CompletedAtUtc is null : row.CompletedAtUtc is not null) || row.EntityId is not null || row.AssignedUserId is not null) throw new InvalidOperationException("Status/default/reference changed.");
            Pass("existing status/completion semantics");
        }
        await NoWrite(HttpMethod.Patch, root + "/" + plain + "/status", new WorkStatus("Unknown"), admin, HttpStatusCode.BadRequest, "invalid status");
        using (var response = await Send(HttpMethod.Patch, root + "/" + collection + "/collections", new CollectionStateRequest("Reminder", null), admin)) RequireFinanceStatus(response, HttpStatusCode.OK, "collection state");
        Pass("existing collection-state update");
        foreach (var id in new[] { otherInvoice, Guid.NewGuid() }) await NoWrite(HttpMethod.Post, $"/api/academies/{academy}/finance-governance/collections/{id}/follow-up", new CollectionFollowUpRequest(null, null, null, null), finance, HttpStatusCode.NotFound, "foreign/missing invoice follow-up");
        await SetAuditInsertDeniedAsync(manifest, true);
        try { await NoWrite(HttpMethod.Post, root, valid, admin, HttpStatusCode.InternalServerError, "work-create audit rollback");
            await NoWrite(HttpMethod.Post, follow, new CollectionFollowUpRequest(null, null, null, null), finance, HttpStatusCode.InternalServerError, "follow-up audit rollback"); }
        finally { await SetAuditInsertDeniedAsync(manifest, false); }
        await Created(root, valid, admin, "Operations", null, null, null);
        using (var scope = factory.Services.CreateScope()) { var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); (await db.Academies.SingleAsync(x => x.Id == academy)).IsActive = false; await db.SaveChangesAsync(); }
        await NoWrite(HttpMethod.Post, root, valid, admin, HttpStatusCode.Forbidden, "suspended academy");
        using (var scope = factory.Services.CreateScope()) { var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); (await db.Academies.SingleAsync(x => x.Id == academy)).IsActive = true; await db.SaveChangesAsync(); }
        foreach (var token in new[] { teacher, studentToken }) await NoWrite(HttpMethod.Post, follow, new CollectionFollowUpRequest(null, null, null, null), token, HttpStatusCode.Forbidden, "portal finance denial");
        if (count != 79) throw new InvalidOperationException("Work-item matrix incomplete: " + count);
        Console.WriteLine($"PASS: WORKITEM all {count} real SQL/Identity/HTTP cases; exact guards, optional values, tenant/role isolation, status/filter preservation and audit rollback; no 429.");
    }
}
