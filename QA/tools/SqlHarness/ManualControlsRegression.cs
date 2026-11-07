using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Domain.Identity;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

internal static partial class SqlHarnessEntryPoint
{
    private sealed record ManualFixture(Guid Academy, Guid Foreign, Guid Batch, Guid OtherBatch, Guid ForeignBatch, Guid Student, Guid Admin);

    private static async Task<JsonObject> ManualStateAsync(QaApiFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
        var id = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        return JsonSerializer.SerializeToNode(new {
            invoices = await db.Invoices.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(),
            payments = await db.Payments.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(),
            adjustments = await db.FinanceAdjustments.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(),
            assignments = await db.Assignments.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(),
            assessments = await db.Assessments.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(),
            lessons = await db.LessonPlans.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(),
            notifications = await db.Notifications.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(),
            audits = await db.AuditLogs.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(),
            users = await id.Users.AsNoTracking().OrderBy(x => x.Id).Select(x => new { x.Id, x.AcademyId, x.TeacherId, x.StudentId, x.SecurityStamp, x.IsActive, x.IsPlatformOwner }).ToArrayAsync(),
            roles = await id.UserRoles.AsNoTracking().OrderBy(x => x.UserId).ThenBy(x => x.RoleId).ToArrayAsync()
        })!.AsObject();
    }

    private static JsonObject ManualRow(JsonObject state, string table, Guid id) => state[table]!.AsArray().Select(x => x!.AsObject()).Single(x => x["Id"]!.GetValue<Guid>() == id);
    private static void ManualSame(JsonNode before, JsonNode after, string label)
    {
        if (!JsonNode.DeepEquals(before, after)) throw new InvalidOperationException("Captured SQL state changed: " + label);
    }
    private static JsonObject[] ManualAdded(JsonObject before, JsonObject after, string table, int count)
    {
        var old = before[table]!.AsArray().Select(x => x!["Id"]!.GetValue<Guid>()).ToHashSet();
        var added = after[table]!.AsArray().Where(x => !old.Contains(x!["Id"]!.GetValue<Guid>())).Select(x => x!.AsObject()).ToArray();
        if (added.Length != count) throw new InvalidOperationException("Unexpected new row count: " + table);
        var retained = new JsonArray(after[table]!.AsArray().Where(x => old.Contains(x!["Id"]!.GetValue<Guid>())).Select(x => x!.DeepClone()).ToArray());
        ManualSame(before[table]!, retained, "existing " + table);
        after[table] = before[table]!.DeepClone();
        return added;
    }
    private static void ManualTeacherDelta(JsonObject before, JsonObject after, ManualFixture fixture, string path, JsonObject body, Guid saved)
    {
        var table = path.EndsWith("assignments", StringComparison.Ordinal) ? "assignments" : path.EndsWith("assessments", StringComparison.Ordinal) ? "assessments" : "lessons";
        var row = ManualAdded(before, after, table, 1).Single();
        if (row["Id"]!.GetValue<Guid>() != saved || row["AcademyId"]!.GetValue<Guid>() != fixture.Academy || row["BatchId"]!.GetValue<Guid>() != fixture.Batch || row["Title"]!.GetValue<string>() != body["title"]!.GetValue<string>().Trim())
            throw new InvalidOperationException("Teacher response/tenant/batch/title differs from the saved row.");
        if (body.ContainsKey("isPublished") && row["IsPublished"]!.GetValue<bool>() != body["isPublished"]!.GetValue<bool>()) throw new InvalidOperationException("Publication boolean did not persist.");
        if (body.ContainsKey("type") && row["Type"]!.GetValue<string>() != body["type"]!.GetValue<string>().Trim()) throw new InvalidOperationException("Teacher record type changed.");
        if (table == "assignments" && (row["Description"]?.GetValue<string>() != body["description"]?.GetValue<string>()?.Trim() || row["StudentId"] is not null)) throw new InvalidOperationException("Optional assignment values changed.");
        if (table == "lessons" && (row["Objectives"]?.GetValue<string>() != body["objectives"]?.GetValue<string>()?.Trim() || row["CourseModuleId"] is not null || row["ClassSessionId"] is not null || row["Status"]!.GetValue<string>() != "Planned")) throw new InvalidOperationException("Optional lesson values changed.");
        if (table == "assessments")
        {
            if (row["MaxScore"]!.GetValue<decimal>() != body["maxScore"]!.GetValue<decimal>()) throw new InvalidOperationException("Fractional maximum score changed.");
            var expected = body["scheduledAtUtc"] is null ? (DateTime?)null : body["scheduledAtUtc"]!.GetValue<DateTime>().ToUniversalTime();
            if (row["ScheduledAtUtc"]?.GetValue<DateTime>().Ticks != expected?.Ticks) throw new InvalidOperationException("Optional UTC assessment date changed.");
        }
        var notificationCount = table == "assignments" && body["isPublished"]!.GetValue<bool>() ? 1 : 0;
        foreach (var notification in ManualAdded(before, after, "notifications", notificationCount))
            if (notification["AcademyId"]!.GetValue<Guid>() != fixture.Academy || notification["RecipientId"]!.GetValue<Guid>() != fixture.Student || notification["RecipientType"]!.GetValue<string>() != "Student" || notification["Channel"]!.GetValue<string>() != "InApp" || notification["Status"]!.GetValue<string>() != "Queued")
                throw new InvalidOperationException("Published homework notification escaped its intended learner.");
        ManualSame(before, after, "Teacher create unrelated tables, money, audit and Identity");
    }
    private static void ManualFinanceDelta(JsonObject before, JsonObject after, ManualFixture fixture, Guid invoice, Guid adjustment, JsonObject body, Guid? actor = null)
    {
        var approved = body["approve"]!.GetValue<bool>();
        var saved = ManualRow(after, "adjustments", adjustment);
        var notes = body["notes"]?.GetValue<string>();
        if (saved["Status"]!.GetValue<string>() != (approved ? "Approved" : "Rejected") || saved["ApprovalNotes"]?.GetValue<string>() != (string.IsNullOrWhiteSpace(notes) ? null : notes.Trim()) || saved["ApprovedAtUtc"] is null || (saved["AppliedAtUtc"] is not null) != approved)
            throw new InvalidOperationException("Financial decision/notes/timestamps did not persist.");
        var ledger = ManualRow(after, "invoices", invoice);
        if (ledger["TotalAmount"]!.GetValue<decimal>() != 1000m || ledger["AdjustedAmount"]!.GetValue<decimal>() != (approved ? 100m : 0m) || ledger["Status"]!.GetValue<string>() != "PartiallyPaid") throw new InvalidOperationException("Financial decision changed the deterministic invoice balance.");
        foreach (var (table, id, fields) in new[] {
            ("invoices", invoice, new[] { "AdjustedAmount", "Status" }),
            ("adjustments", adjustment, new[] { "Status", "ApprovalNotes", "ApprovedAtUtc", "AppliedAtUtc" }) })
        {
            var old = ManualRow(before, table, id); var changed = ManualRow(after, table, id);
            foreach (var field in fields) changed[field] = old[field]?.DeepClone();
        }
        var audit = ManualAdded(before, after, "audits", 1).Single();
        if (audit["ActorUserId"]!.GetValue<Guid>() != (actor ?? fixture.Admin) || audit["AcademyId"]!.GetValue<Guid>() != fixture.Academy || audit["Action"]!.GetValue<string>() != "PATCH FinanceAdjustments" || !audit["MetadataJson"]!.GetValue<string>().Contains($"/finance-adjustments/{adjustment}/approval", StringComparison.Ordinal)) throw new InvalidOperationException("Financial decision audit lacks its authoritative actor/tenant/route.");
        ManualSame(before, after, "financial decision unrelated fields, reconciled payments, forms and Identity");
    }
    private static async Task<(Guid Invoice, Guid Adjustment)> ManualProposalAsync(QaApiFactory factory, ManualFixture fixture, string reason)
    {
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
        var invoice = new Invoice { AcademyId = fixture.Academy, StudentId = fixture.Student, InvoiceNumber = "C6-" + Guid.NewGuid().ToString("N"), TotalAmount = 1000m, Status = "PartiallyPaid", DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)) };
        var adjustment = new FinanceAdjustment { AcademyId = fixture.Academy, InvoiceId = invoice.Id, Amount = 100m, Type = "Discount", Reason = reason };
        db.AddRange(invoice, adjustment, new Payment { AcademyId = fixture.Academy, InvoiceId = invoice.Id, Amount = 600m, Status = "Reconciled", ReconciledAtUtc = DateTime.UtcNow, ReconciliationReference = "Synthetic-C6" });
        await db.SaveChangesAsync(); return (invoice.Id, adjustment.Id);
    }
    private static async Task<HttpResponseMessage> ManualSendAsync(HttpClient client, string? access, string method, string route, JsonObject? body = null)
    {
        using var request = new HttpRequestMessage(new HttpMethod(method), route);
        if (access is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", access);
        if (body is not null) request.Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json");
        return await client.SendAsync(request);
    }

    private static async Task<ManualFixture> VerifyManualControlsAsync(QaApiFactory factory, HttpClient client, QaRunManifest manifest)
    {
        var input = Environment.GetEnvironmentVariable("QA_MANUAL_BROWSER_RECEIPT") ?? throw new InvalidOperationException("Genuine C6 browser receipt required.");
        var bytes = await File.ReadAllBytesAsync(input); if (bytes.Length > 2_000_000) throw new InvalidOperationException("C6 receipt input limit exceeded.");
        using var receipt = JsonDocument.Parse(bytes);
        if (receipt.RootElement.GetProperty("status").GetString() != "PASS" || receipt.RootElement.GetProperty("results").GetArrayLength() != 15 || receipt.RootElement.GetProperty("runtimeErrors").GetArrayLength() != 0) throw new InvalidOperationException("Genuine C6 controlled receipt is not passing.");
        Guid academy, foreign, batch, otherBatch, foreignBatch, student, admin;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            academy = await db.Academies.Where(x => x.Name == "Synthetic Academy A").Select(x => x.Id).SingleAsync();
            foreign = await db.Academies.Where(x => x.Name == "Synthetic Academy B").Select(x => x.Id).SingleAsync();
            batch = await db.Batches.Where(x => x.AcademyId == academy).Select(x => x.Id).SingleAsync();
            student = await db.Students.Where(x => x.AcademyId == academy).Select(x => x.Id).SingleAsync();
            var course = await db.Courses.Where(x => x.AcademyId == academy).Select(x => x.Id).SingleAsync();
            var other = new Batch { AcademyId = academy, CourseId = course, TeacherId = Guid.NewGuid(), Name = "Synthetic other teacher batch" };
            var outside = new Batch { AcademyId = foreign, CourseId = course, Name = "Synthetic foreign batch" };
            otherBatch = other.Id; foreignBatch = outside.Id;
            db.AddRange(other, outside, new Enrollment { AcademyId = academy, StudentId = student, BatchId = batch, Status = "Active" });
            await db.SaveChangesAsync();
            admin = await scope.ServiceProvider.GetRequiredService<IdentityDbContext>().Users.Where(x => x.Email == "qa-admin-a@example.invalid").Select(x => x.Id).SingleAsync();
        }
        var fixture = new ManualFixture(academy, foreign, batch, otherBatch, foreignBatch, student, admin);
        var platformId = await CreateAccessActorAsync(factory, "qa-c6-platform@example.invalid", "Owner", academy);
        using (var scope = factory.Services.CreateScope()) { var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(); var owner = (await users.FindByIdAsync(platformId.ToString()))!; owner.IsPlatformOwner = true; if (!(await users.UpdateAsync(owner)).Succeeded) throw new InvalidOperationException("Synthetic C6 owner setup failed."); }
        var adminAccess = await LoginAsync(client, "qa-admin-a@example.invalid", "Synthetic!39Ab");
        var teacherAccess = await LoginAsync(client, "qa-teacher-a@example.invalid", "Synthetic!39Ab");
        var platformAccess = await LoginAsync(client, "qa-c6-platform@example.invalid", "Synthetic!39Ab");
        client.DefaultRequestHeaders.Authorization = null;
        var checks = 0; var failures = new List<object>();
        void Pass(string label) { checks++; Console.WriteLine("FINANCEMANUAL CASE " + label + " PASS."); }
        var rows = receipt.RootElement.GetProperty("results").EnumerateArray().Where(row => !row.GetProperty("case").GetString()!.Contains("denied") && !row.GetProperty("case").GetString()!.Contains("unconfirmed"));
        foreach (var row in rows)
        foreach (var capture in row.GetProperty("requests").EnumerateArray())
        {
            var body = JsonNode.Parse(capture.GetProperty("body").GetRawText())!.AsObject(); var path = capture.GetProperty("path").GetString()!;
            if (path.StartsWith("/api/teacher/", StringComparison.Ordinal))
            {
                body["batchId"] = batch;
                var before = await ManualStateAsync(factory);
                using var response = await ManualSendAsync(client, teacherAccess, "POST", path, body); RequireFinanceStatus(response, HttpStatusCode.OK, "captured Teacher body");
                var value = JsonNode.Parse(await response.Content.ReadAsStringAsync())!.AsObject(); var saved = value["id"]!.GetValue<Guid>();
                ManualTeacherDelta(before, await ManualStateAsync(factory), fixture, path, body, saved);
                using var read = await ManualSendAsync(client, teacherAccess, "GET", path); RequireFinanceStatus(read, HttpStatusCode.OK, "Teacher readback");
                var found = JsonNode.Parse(await read.Content.ReadAsStringAsync())!.AsArray().Single(x => x!["id"]!.GetValue<Guid>() == saved)!;
                if (path.EndsWith("assessments", StringComparison.Ordinal) && body["scheduledAtUtc"] is not null)
                {
                    // SQL datetime2 retains UTC clock ticks, not DateTime.Kind.
                    // Compare the UTC-by-contract value without claiming a new
                    // portal-wide serialized timezone-format guarantee.
                    var expectedTicks = body["scheduledAtUtc"]!.GetValue<DateTime>().ToUniversalTime().Ticks;
                    if (found["scheduledAtUtc"]!.GetValue<DateTime>().Ticks != expectedTicks || value["scheduledAtUtc"]!.GetValue<DateTime>().Ticks != expectedTicks) throw new InvalidOperationException("Assessment HTTP readback changed its UTC value.");
                    found["scheduledAtUtc"] = expectedTicks; value["scheduledAtUtc"] = expectedTicks;
                }
                ManualSame(value, found, "Teacher fresh API readback"); Pass(row.GetProperty("case").GetString()! + ": exact one row/readback/recipient");
                foreach (var deniedBatch in new[] { otherBatch, foreignBatch }) { var deniedBody = body.DeepClone().AsObject(); deniedBody["batchId"] = deniedBatch; await Denied(teacherAccess, "POST", path, deniedBody, HttpStatusCode.BadRequest, "Teacher cannot use another assigned/foreign batch"); }
                var invalid = body.DeepClone().AsObject(); if (invalid.ContainsKey("isPublished")) { invalid["isPublished"] = "true"; await Denied(teacherAccess, "POST", path, invalid, HttpStatusCode.BadRequest, "string publication is not a bool"); }
                if (path.EndsWith("assessments", StringComparison.Ordinal)) { invalid = body.DeepClone().AsObject(); invalid["scheduledAtUtc"] = ""; await Denied(teacherAccess, "POST", path, invalid, HttpStatusCode.BadRequest, "empty date string is not nullable date"); }
                await Denied(null, "POST", path, body, HttpStatusCode.Unauthorized, "anonymous Teacher write");
            }
            else
            {
                var proposal = await ManualProposalAsync(factory, fixture, "Native " + row.GetProperty("case").GetString());
                path = $"/api/academies/{academy}/finance-adjustments/{proposal.Adjustment}/approval";
                var before = await ManualStateAsync(factory);
                using var response = await ManualSendAsync(client, adminAccess, "PATCH", path, body); RequireFinanceStatus(response, HttpStatusCode.OK, "captured finance decision");
                ManualFinanceDelta(before, await ManualStateAsync(factory), fixture, proposal.Invoice, proposal.Adjustment, body);
                using var invoiceRead = await ManualSendAsync(client, adminAccess, "GET", $"/api/academies/{academy}/invoices");
                RequireFinanceStatus(invoiceRead, HttpStatusCode.OK, "financial decision invoice readback");
                var invoice = JsonNode.Parse(await invoiceRead.Content.ReadAsStringAsync())!.AsArray().Single(x => x!["id"]!.GetValue<Guid>() == proposal.Invoice)!;
                if (invoice["paidAmount"]!.GetValue<decimal>() != 600m || invoice["balance"]!.GetValue<decimal>() != (body["approve"]!.GetValue<bool>() ? 300m : 400m))
                    throw new InvalidOperationException("Invoice readback lost the reconciled payment or collectible balance.");
                Pass("captured finance decision with correct invoice readback and one attributed audit");
                await Denied(adminAccess, "PATCH", path, body, HttpStatusCode.Conflict, "decided proposal cannot be applied twice");
            }
        }
        async Task Denied(string? access, string method, string path, JsonObject body, HttpStatusCode expected, string label)
        {
            var before = await ManualStateAsync(factory); using var response = await ManualSendAsync(client, access, method, path, body); RequireFinanceStatus(response, expected, label); ManualSame(before, await ManualStateAsync(factory), label); Pass(label);
        }
        var valid = new JsonObject { ["approve"] = true, ["notes"] = "Synthetic decision" };
        var target = await ManualProposalAsync(factory, fixture, "Native authority"); var decisionPath = $"/api/academies/{academy}/finance-adjustments/{target.Adjustment}/approval";
        await Denied(null, "PATCH", decisionPath, valid, HttpStatusCode.Unauthorized, "anonymous finance decision");
        await Denied(teacherAccess, "PATCH", decisionPath, valid, HttpStatusCode.Forbidden, "Teacher finance decision");
        await Denied(adminAccess, "PATCH", $"/api/academies/{foreign}/finance-adjustments/{target.Adjustment}/approval", valid, HttpStatusCode.Forbidden, "foreign academy decision");
        foreach (var note in new string?[] { null, "", "   " })
        {
            var proposal = await ManualProposalAsync(factory, fixture, "Native rejection-note guard"); var before = await ManualStateAsync(factory);
            using var response = await ManualSendAsync(client, adminAccess, "PATCH", $"/api/academies/{academy}/finance-adjustments/{proposal.Adjustment}/approval", new JsonObject { ["approve"] = false, ["notes"] = note });
            var unchanged = JsonNode.DeepEquals(before, await ManualStateAsync(factory));
            if (response.StatusCode != HttpStatusCode.BadRequest || !unchanged) { failures.Add(new { guard = "rejection-note", noteKind = note is null ? "null" : note.Length == 0 ? "empty" : "whitespace", status = (int)response.StatusCode, unchanged }); Console.WriteLine($"FINANCEMANUAL FAILURE rejection-note status={(int)response.StatusCode} unchanged={unchanged}"); } else Pass("required rejection note independently enforced");
        }
        foreach (var actor in new[] { (Name: "Admin", Access: adminAccess), (Name: "PlatformOwner", Access: platformAccess) })
        {
            var proposal = await ManualProposalAsync(factory, fixture, "Native required audit"); var before = await ManualStateAsync(factory); var fault = new ManualFinanceAuditFault();
            using var failing = new QaApiFactory(manifest, isolatedDomainInterceptor: fault); using var failedClient = failing.CreateClient();
            using var response = await ManualSendAsync(failedClient, actor.Access, "PATCH", $"/api/academies/{academy}/finance-adjustments/{proposal.Adjustment}/approval", valid);
            var unchanged = JsonNode.DeepEquals(before, await ManualStateAsync(factory));
            if (response.StatusCode != HttpStatusCode.InternalServerError || fault.Hits != 1 || !unchanged) { failures.Add(new { guard = "required-audit", actor = actor.Name, status = (int)response.StatusCode, fault.Hits, unchanged }); Console.WriteLine($"FINANCEMANUAL FAILURE audit actor={actor.Name} status={(int)response.StatusCode} faultHits={fault.Hits} unchanged={unchanged}"); } else Pass(actor.Name + " required audit failure rolls invoice and decision back");
        }
        if (failures.Count == 0)
        {
            var proposal = await ManualProposalAsync(factory, fixture, "Native authorized Platform Owner decision");
            var before = await ManualStateAsync(factory);
            using var response = await ManualSendAsync(client, platformAccess, "PATCH", $"/api/academies/{academy}/finance-adjustments/{proposal.Adjustment}/approval", valid);
            RequireFinanceStatus(response, HttpStatusCode.OK, "authorized Platform Owner decision");
            ManualFinanceDelta(before, await ManualStateAsync(factory), fixture, proposal.Invoice, proposal.Adjustment, valid, platformId);
            Pass("authorized Platform Owner decision records one attributed audit");
        }
        var folder = Path.Combine(Directory.GetCurrentDirectory(), "QA", "EVIDENCE", "c6-manual-sql-" + manifest.RunId.ToString("N")); Directory.CreateDirectory(folder);
        await File.WriteAllTextAsync(Path.Combine(folder, "native-result.json"), JsonSerializer.Serialize(new { passed = failures.Count == 0, checks, failures, fixture, browserReceiptSha256 = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant(), modelInference = false }, new JsonSerializerOptions { WriteIndented = true }));
        if (failures.Count != 0) throw new InvalidOperationException("C6 live controls failed; native status and fresh SQL evidence retained.");
        Console.WriteLine($"FINANCEMANUAL SUMMARY {checks} native HTTP/SQL controls PASS."); return fixture;
    }
    private sealed class ManualFinanceAuditFault : SaveChangesInterceptor
    {
        public int Hits { get; private set; }
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData data, InterceptionResult<int> result, CancellationToken token = default)
        {
            if (data.Context is AcademyDeskDbContext db && db.ChangeTracker.Entries<AuditLog>().Any(x => x.State == EntityState.Added && x.Entity.Action == "PATCH FinanceAdjustments")) { Hits++; throw new InvalidOperationException("Synthetic C6 required finance audit failure."); }
            return base.SavingChangesAsync(data, result, token);
        }
    }
}
