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
    private static async Task VerifyWorkItemIntegrityAsync(QaApiFactory factory, HttpClient client, QaRunManifest manifest)
    {
        Guid academy, foreign, invoiceId, foreignInvoiceId, documentId, foreignDocumentId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            academy = await db.Academies.Where(x => x.Name == "Synthetic Academy A").Select(x => x.Id).SingleAsync();
            foreign = await db.Academies.Where(x => x.Name == "Synthetic Academy B").Select(x => x.Id).SingleAsync();
            var academyRow = await db.Academies.SingleAsync(x => x.Id == academy);
            var modules = JsonSerializer.Deserialize<List<string>>(academyRow.EnabledModulesJson ?? "[]") ?? [];
            if (!modules.Contains("FinanceControls")) modules.Add("FinanceControls");
            academyRow.EnabledModulesJson = JsonSerializer.Serialize(modules);
            var student = await db.Students.FirstAsync(x => x.AcademyId == academy);
            var otherStudent = await db.Students.FirstAsync(x => x.AcademyId == foreign);
            var invoice = new Invoice { AcademyId = academy, StudentId = student.Id, InvoiceNumber = "QA-WORK-OWN", TotalAmount = 100, DueDate = new(2026, 1, 1) };
            var otherInvoice = new Invoice { AcademyId = foreign, StudentId = otherStudent.Id, InvoiceNumber = "QA-WORK-FOREIGN", TotalAmount = 100 };
            var doc = new PersonDocument { AcademyId = academy, StudentId = student.Id, DocumentType = "ID", FileName = "synthetic.pdf" };
            var otherDoc = new PersonDocument { AcademyId = foreign, StudentId = otherStudent.Id, DocumentType = "ID", FileName = "synthetic-foreign.pdf" };
            db.AddRange(invoice, otherInvoice, doc, otherDoc); await db.SaveChangesAsync();
            invoiceId = invoice.Id; foreignInvoiceId = otherInvoice.Id; documentId = doc.Id; foreignDocumentId = otherDoc.Id;
        }
        var staff = await CreateAccessActorAsync(factory, "qa-work-staff@example.invalid", "Operations", academy);
        var custom = await CreateAccessActorAsync(factory, "qa-work-custom@example.invalid", "QA-WorkStaff", academy);
        var financeUser = await CreateAccessActorAsync(factory, "qa-work-finance@example.invalid", "FinanceUser", academy);
        var teacher = await CreateAccessActorAsync(factory, "qa-work-teacher@example.invalid", "Teacher", academy);
        var guardian = await CreateAccessActorAsync(factory, "qa-work-guardian@example.invalid", "Guardian", academy);
        var studentUser = await CreateAccessActorAsync(factory, "qa-work-student@example.invalid", "Student", academy);
        var inactive = await CreateAccessActorAsync(factory, "qa-work-inactive@example.invalid", "Operations", academy);
        var locked = await CreateAccessActorAsync(factory, "qa-work-locked@example.invalid", "Operations", academy);
        var platform = await CreateAccessActorAsync(factory, "qa-work-platform@example.invalid", "Operations", academy);
        var roleless = await CreateAccessActorAsync(factory, "qa-work-roleless@example.invalid", "Operations", academy);
        var foreignRole = await CreateAccessActorAsync(factory, "qa-work-foreignrole@example.invalid", "QA-ForeignWorkStaff", academy);
        Guid adminId, foreignAdminId;
        using (var scope = factory.Services.CreateScope())
        {
            var identity = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            adminId = await identity.Users.Where(x => x.Email == "qa-admin-a@example.invalid").Select(x => x.Id).SingleAsync();
            foreignAdminId = await identity.Users.Where(x => x.Email == "qa-admin-b@example.invalid").Select(x => x.Id).SingleAsync();
            (await identity.Users.SingleAsync(x => x.Id == inactive)).IsActive = false;
            var lockedUser = await identity.Users.SingleAsync(x => x.Id == locked); lockedUser.LockoutEnabled = true; lockedUser.LockoutEnd = DateTimeOffset.UtcNow.AddDays(1);
            (await identity.Users.SingleAsync(x => x.Id == platform)).IsPlatformOwner = true;
            identity.UserRoles.RemoveRange(await identity.UserRoles.Where(x => x.UserId == roleless).ToArrayAsync());
            (await identity.Roles.SingleAsync(x => x.Name == "QA-ForeignWorkStaff")).AcademyId = foreign;
            await identity.SaveChangesAsync();
        }
        var adminToken = await LoginAsync(client, "qa-admin-a@example.invalid", "Synthetic!39Ab");
        var foreignToken = await LoginAsync(client, "qa-admin-b@example.invalid", "Synthetic!39Ab");
        var teacherToken = await LoginAsync(client, "qa-work-teacher@example.invalid", "Synthetic!39Ab");
        var financeToken = await LoginAsync(client, "qa-work-finance@example.invalid", "Synthetic!39Ab");
        client.DefaultRequestHeaders.Authorization = null;
        var root = $"/api/academies/{academy}"; var workPath = root + "/admin-work-items";
        var followPath = root + $"/finance-governance/collections/{invoiceId}/follow-up"; var count = 0;
        void Pass(string label) { count++; Console.WriteLine($"WORKITEM CASE {label} PASS."); }
        object Work(Guid? assignee = null, string? entityType = null, Guid? entityId = null) => new { type = "Operations", title = "Synthetic action", description = "Synthetic context", priority = "High", assignedUserId = assignee, entityType, entityId, dueAtUtc = "2026-10-29T10:00:00Z" };
        async Task<string> State()
        {
            using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            return JsonSerializer.Serialize(new { tasks = await db.AdminWorkItems.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(), invoices = await db.Invoices.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(), payments = await db.Payments.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(), documents = await db.PersonDocuments.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(), audits = await db.AuditLogs.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(), notifications = await db.Notifications.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync() });
        }
        async Task<HttpResponseMessage> Send(string method, string path, object? body, string? token)
        {
            using var request = new HttpRequestMessage(new HttpMethod(method), path);
            if (token is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            if (body is not null) request.Content = JsonContent.Create(body);
            return await client.SendAsync(request);
        }
        async Task NoWrite(string label, string method, string path, object? body, HttpStatusCode status, string? token)
        {
            var before = await State(); using var response = await Send(method, path, body, token); RequireFinanceStatus(response, status, label);
            if (before != await State()) throw new InvalidOperationException("Rejected/read-only work item changed domain/audit snapshot: " + label);
            Pass(label);
        }
        async Task<Guid> Accept(string label, string path, object body, Guid? assigned, string? entityType, Guid? entityId, string? token = null)
        {
            using var beforeScope = factory.Services.CreateScope(); var beforeDb = beforeScope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var auditCount = await beforeDb.AuditLogs.CountAsync(); var rowCount = await beforeDb.AdminWorkItems.CountAsync();
            using var response = await Send("POST", path, body, token ?? adminToken); RequireFinanceStatus(response, HttpStatusCode.OK, label);
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync()); var id = json.RootElement.GetProperty("id").GetGuid();
            using var afterScope = factory.Services.CreateScope(); var db = afterScope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var row = await db.AdminWorkItems.AsNoTracking().SingleAsync(x => x.Id == id);
            if (row.AcademyId != academy || row.AssignedUserId != assigned || row.EntityType != entityType || row.EntityId != entityId || row.Status != "Open" || row.CompletedAtUtc is not null || row.Priority != "High" || await db.AdminWorkItems.CountAsync() != rowCount + 1 || await db.AuditLogs.CountAsync() != auditCount + 1)
                throw new InvalidOperationException("Accepted work item SQL/audit mismatch: " + label);
            Pass(label); return id;
        }
        foreach (var (label, id) in new (string, Guid)[] { ("missing", Guid.NewGuid()), ("empty", Guid.Empty), ("foreign", foreignAdminId), ("inactive", inactive), ("locked", locked), ("platform", platform), ("roleless", roleless), ("foreign-role", foreignRole), ("teacher", teacher), ("student", studentUser), ("guardian", guardian) })
        {
            await NoWrite(label + "-work-assignee400", "POST", workPath, Work(id), HttpStatusCode.BadRequest, adminToken);
            await NoWrite(label + "-finance-assignee400", "POST", followPath, new { assignedUserId = id }, HttpStatusCode.BadRequest, financeToken);
        }
        Guid taskId = Guid.Empty, collectionId = Guid.Empty;
        foreach (var (label, id) in new (string, Guid?)[] { ("unassigned", null), ("staff", staff), ("admin", adminId), ("custom", custom) })
        {
            taskId = await Accept(label + "-work-control", workPath, Work(id), id, null, null);
            collectionId = await Accept(label + "-finance-control", followPath, new { assignedUserId = id }, id, "Invoice", invoiceId, financeToken);
        }
        foreach (var (type, own, other, wrong) in new[] { ("Invoice", invoiceId, foreignInvoiceId, documentId), ("PersonDocument", documentId, foreignDocumentId, invoiceId) })
        {
            foreach (var (label, id) in new (string, Guid?)[] { ("foreign", other), ("missing", Guid.NewGuid()), ("empty", Guid.Empty), ("wrong-type", wrong), ("absent", null) })
                await NoWrite(type + "-" + label + "400", "POST", workPath, Work(null, type, id), HttpStatusCode.BadRequest, adminToken);
            await Accept(type + "-linked-control", workPath, Work(staff, " " + type + " ", own), staff, type, own);
        }
        await NoWrite("unsupported-entity400", "POST", workPath, Work(null, "Unknown", invoiceId), HttpStatusCode.BadRequest, adminToken);
        await NoWrite("unpaired-id400", "POST", workPath, Work(null, null, invoiceId), HttpStatusCode.BadRequest, adminToken);
        await NoWrite("anonymous401", "POST", workPath, Work(), HttpStatusCode.Unauthorized, null);
        await NoWrite("teacher403", "POST", workPath, Work(), HttpStatusCode.Forbidden, teacherToken);
        await NoWrite("finance-cannot-create-admin-work403", "POST", workPath, Work(), HttpStatusCode.Forbidden, financeToken);
        await NoWrite("foreign-actor403", "POST", workPath, Work(), HttpStatusCode.Forbidden, foreignToken);
        await NoWrite("foreign-invoice404", "POST", root + $"/finance-governance/collections/{foreignInvoiceId}/follow-up", new { }, HttpStatusCode.NotFound, adminToken);
        await NoWrite("invalid-priority400", "POST", followPath, new { priority = "Urgent" }, HttpStatusCode.BadRequest, financeToken);
        await NoWrite("oversized-note400", "POST", followPath, new { note = new string('x', 4001) }, HttpStatusCode.BadRequest, financeToken);
        using (var response = await Send("PATCH", workPath + $"/{taskId}/status", new { status = "Completed" }, adminToken)) RequireFinanceStatus(response, HttpStatusCode.OK, "complete work item");
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); var row = await db.AdminWorkItems.AsNoTracking().SingleAsync(x => x.Id == taskId);
            if (row.Status != "Completed" || row.CompletedAtUtc is null || row.AssignedUserId != custom) throw new InvalidOperationException("Status update lost ownership or completion metadata.");
        }
        Pass("status-completion-keeps-assignee");
        var beforeList = await State();
        using (var response = await Send("GET", workPath + "?status=Completed&type=Operations", null, adminToken))
        {
            RequireFinanceStatus(response, HttpStatusCode.OK, "filtered list"); using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            if (json.RootElement.GetArrayLength() != 1 || json.RootElement[0].GetProperty("id").GetGuid() != taskId || beforeList != await State())
                throw new InvalidOperationException("Filtered work queue changed or returned a different record.");
        }
        Pass("filtered-list-exact-completed-record");
        await SetAuditInsertDeniedAsync(manifest, true);
        try
        {
            await NoWrite("create-audit-fault-rollback", "POST", workPath, Work(staff), HttpStatusCode.InternalServerError, adminToken);
            await NoWrite("status-audit-fault-rollback", "PATCH", workPath + $"/{taskId}/status", new { status = "Open" }, HttpStatusCode.InternalServerError, adminToken);
            await NoWrite("collection-state-audit-fault-rollback", "PATCH", workPath + $"/{collectionId}/collections", new { escalationStage = "Reminder", promisedPaymentDate = "2026-11-01" }, HttpStatusCode.InternalServerError, adminToken);
            await NoWrite("followup-audit-fault-rollback", "POST", followPath, new { assignedUserId = staff }, HttpStatusCode.InternalServerError, financeToken);
        }
        finally { await SetAuditInsertDeniedAsync(manifest, false); }
        Console.WriteLine($"WORKITEM REGRESSION PASS:{count} real Identity/HTTP/fresh SQL cases; scoped assignees/entity pairs, accepted controls, existing roles, status/list and audit rollback. Browser, concurrent eligibility changes and full release acceptance remain separate.");
    }
}
