using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
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
    // Same-currency balances only; real auth/filter/model binding and disposable SQL.
    private static async Task VerifyPlatformBillingAsync(QaApiFactory factory, HttpClient client)
    {
        var baseline = Environment.GetEnvironmentVariable("QA_PLATFORM_BILLING_BASELINE") == "1";
        Guid academy, foreign, ownerId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            academy = await db.Academies.Where(x => x.Name == "Synthetic Academy A").Select(x => x.Id).SingleAsync();
            foreign = await db.Academies.Where(x => x.Name == "Synthetic Academy B").Select(x => x.Id).SingleAsync();
        }
        ownerId = await CreateAccessActorAsync(factory, "qa-platform-billing@example.invalid", "PlatformOwner", academy);
        using (var scope = factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var owner = (await users.FindByIdAsync(ownerId.ToString()))!;
            owner.IsPlatformOwner = true; owner.AcademyId = null; owner.DisplayName = "Synthetic billing owner";
            if (!(await users.UpdateAsync(owner)).Succeeded) throw new InvalidOperationException("Billing owner fixture failed.");
        }
        var actors = new Dictionary<string, string>();
        var actorIds = new Dictionary<string, Guid> { ["platform"] = ownerId };
        actors["platform"] = await LoginAsync(client, "qa-platform-billing@example.invalid", "Synthetic!39Ab");
        foreach (var (name, email) in new[] { ("admin", "qa-admin-a@example.invalid"), ("foreign-admin", "qa-admin-b@example.invalid"), ("teacher", "qa-teacher-a@example.invalid") })
        {
            actors[name] = await LoginAsync(client, email, "Synthetic!39Ab");
            using var scope = factory.Services.CreateScope();
            actorIds[name] = (await scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>().FindByEmailAsync(email))!.Id;
        }
        foreach (var role in new[] { "Owner", "Manager", "FinanceUser", "PlatformOwner" })
        {
            var email = "qa-billing-" + role.ToLowerInvariant() + "@example.invalid";
            actorIds[role] = await CreateAccessActorAsync(factory, email, role, academy);
            actors[role] = await LoginAsync(client, email, "Synthetic!39Ab");
        }
        client.DefaultRequestHeaders.Authorization = null;
        int count = 0;
        string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
        async Task<string> Snapshot()
        {
            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var identity = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            return JsonSerializer.Serialize(new
            {
                invoices = await db.PlatformBillingInvoices.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
                platformAudits = await db.PlatformAuditEntries.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
                academyAudits = await db.AuditLogs.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
                academies = await db.Academies.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
                students = await db.Students.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
                notifications = await db.Notifications.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
                users = await identity.Users.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
                roles = await identity.Roles.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
                memberships = await identity.UserRoles.AsNoTracking().OrderBy(x => x.UserId).ThenBy(x => x.RoleId).ToListAsync()
            });
        }
        void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
        async Task<JsonElement> Request(string label, HttpMethod method, string url, string? actor, string? body,
            HttpStatusCode status, string mutation = "none", Guid? invoiceId = null, string? expectedStatus = null,
            string? expectedReference = null, decimal? billed = null, decimal? collected = null, decimal? outstanding = null, int? overdue = null)
        {
            await Task.Delay(650); // Existing test-host rate policy; no bypass.
            var before = await Snapshot(); var started = DateTime.UtcNow;
            using var request = new HttpRequestMessage(method, url);
            if (actor is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", actors[actor]);
            if (body is not null) request.Content = new StringContent(body, Encoding.UTF8, "application/json");
            using var response = await client.SendAsync(request);
            var text = await response.Content.ReadAsStringAsync(); var ended = DateTime.UtcNow;
            var after = await Snapshot();
            // Print failed attempt evidence before assertions, never count it as a pass.
            Console.WriteLine("PLATFORMBILLING HTTP " + JsonSerializer.Serialize(new { label, method = method.Method, url, actor, requestBody = body, status = (int)response.StatusCode, response = text, beforeDigest = Hash(before), afterDigest = Hash(after) }));
            RequireFinanceStatus(response, status, label);
            JsonElement output = default;
            if (text.Length > 0) { using var document = JsonDocument.Parse(text); output = document.RootElement.Clone(); }
            Guid? addedAudit = null;
            using var oldDocument = JsonDocument.Parse(before); using var newDocument = JsonDocument.Parse(after);
            var old = oldDocument.RootElement; var now = newDocument.RootElement;
            if (mutation == "none") Check(before == after, label + ": read/rejection wrote captured data");
            else
            {
                var id = invoiceId ?? output.GetProperty("id").GetGuid();
                var originalRows = old.GetProperty("invoices").EnumerateArray().ToDictionary(x => x.GetProperty("Id").GetGuid());
                var savedRows = now.GetProperty("invoices").EnumerateArray().ToDictionary(x => x.GetProperty("Id").GetGuid());
                Check(savedRows.Count == originalRows.Count + (mutation == "create" ? 1 : 0), label + ": invoice count changed unexpectedly");
                foreach (var row in originalRows.Where(x => x.Key != id)) Check(savedRows.TryGetValue(row.Key, out var saved) && saved.GetRawText() == row.Value.GetRawText(), label + ": unrelated invoice changed");
                var invoice = savedRows[id];
                Check(invoice.GetProperty("Status").GetString() == expectedStatus, label + ": persisted status mismatch");
                Check(invoice.GetProperty("AcademyId").GetGuid() == academy && invoice.GetProperty("Amount").GetDecimal() == 1000m && invoice.GetProperty("Currency").GetString() == "INR", label + ": financial identity/amount changed");
                if (mutation == "create")
                {
                    Check(!originalRows.ContainsKey(id) && invoice.GetProperty("InvoiceNumber").GetString() == "QA-PLATFORM-LIFECYCLE", label + ": created invoice identity mismatch");
                    Check(invoice.GetProperty("PeriodStart").GetString() == "2001-01-01" && invoice.GetProperty("PeriodEnd").GetString() == "2001-01-31" && invoice.GetProperty("DueDate").GetString() == "2001-02-07", label + ": date persistence mismatch");
                    foreach (var field in new[] { "UpdatedAtUtc", "PaidAtUtc", "PaymentReference", "PaymentSubmittedAtUtc" }) Check(invoice.GetProperty(field).ValueKind == JsonValueKind.Null, label + ": initial optional field was not null");
                    Check(invoice.GetProperty("CreatedAtUtc").GetDateTime() >= started && invoice.GetProperty("CreatedAtUtc").GetDateTime() <= ended, label + ": created timestamp mismatch");
                }
                else
                {
                    var original = originalRows[id];
                    var mutable = mutation == "submit" ? new[] { "Status", "PaymentReference", "PaymentSubmittedAtUtc", "UpdatedAtUtc" } : new[] { "Status", "PaidAtUtc", "UpdatedAtUtc" };
                    foreach (var field in original.EnumerateObject().Where(x => !mutable.Contains(x.Name))) Check(field.Value.GetRawText() == invoice.GetProperty(field.Name).GetRawText(), label + ": preserved invoice field changed: " + field.Name);
                    Check(invoice.GetProperty("UpdatedAtUtc").GetDateTime() >= started && invoice.GetProperty("UpdatedAtUtc").GetDateTime() <= ended, label + ": update timestamp mismatch");
                    if (mutation == "submit")
                    {
                        Check(invoice.GetProperty("PaymentReference").GetString() == expectedReference && output.GetProperty("paymentReference").GetString() == expectedReference, label + ": optional reference mismatch");
                        var submitted = invoice.GetProperty("PaymentSubmittedAtUtc").GetDateTime();
                        Check(submitted >= started && submitted <= ended && output.GetProperty("paymentSubmittedAtUtc").GetDateTime().Ticks == submitted.Ticks, label + ": submission timestamp mismatch");
                    }
                    else if (expectedStatus == "Paid") Check(invoice.GetProperty("PaidAtUtc").GetDateTime() >= started && invoice.GetProperty("PaidAtUtc").GetDateTime() <= ended, label + ": approval timestamp missing");
                    else Check(invoice.GetProperty("PaidAtUtc").ValueKind == JsonValueKind.Null, label + ": nonpaid state retained PaidAtUtc");
                }
                Check(output.GetProperty("id").GetGuid() == id && output.GetProperty("status").GetString() == expectedStatus, label + ": response identity/status mismatch");
                var auditStore = mutation == "submit" ? "academyAudits" : "platformAudits";
                foreach (var store in new[] { "platformAudits", "academyAudits" })
                {
                    var original = old.GetProperty(store).EnumerateArray().ToDictionary(x => x.GetProperty("Id").GetGuid());
                    var saved = now.GetProperty(store).EnumerateArray().ToDictionary(x => x.GetProperty("Id").GetGuid());
                    Check(saved.Count == original.Count + (store == auditStore ? 1 : 0), label + ": success audit count mismatch");
                    foreach (var row in original) Check(saved.TryGetValue(row.Key, out var preserved) && preserved.GetRawText() == row.Value.GetRawText(), label + ": existing audit changed");
                    if (store != auditStore) continue;
                    var audit = saved.Single(x => !original.ContainsKey(x.Key)).Value; addedAudit = audit.GetProperty("Id").GetGuid();
                    Check(audit.GetProperty("ActorUserId").GetGuid() == actorIds[actor!], label + ": audit actor mismatch");
                    if (mutation == "submit")
                    {
                        Check(audit.GetProperty("AcademyId").GetGuid() == academy && audit.GetProperty("Action").GetString() == "POST AcademyPlatformServices" && audit.GetProperty("EntityType").GetString() == "AcademyPlatformServices", label + ": academy audit identity mismatch");
                        using var metadata = JsonDocument.Parse(audit.GetProperty("MetadataJson").GetString()!);
                        Check(metadata.RootElement.GetProperty("Route").GetString() == url, label + ": audit route mismatch");
                    }
                    else
                    {
                        Check(audit.GetProperty("EntityId").GetGuid() == id && audit.GetProperty("EntityType").GetString() == "PlatformBillingInvoice" && audit.GetProperty("ActorName").GetString() == "Synthetic billing owner" && audit.GetProperty("Action").GetString() == (mutation == "create" ? "Platform invoice created" : "Platform invoice status updated"), label + ": platform audit identity mismatch");
                        using var metadata = JsonDocument.Parse(audit.GetProperty("MetadataJson").GetString()!);
                        if (mutation == "create") Check(metadata.RootElement.GetProperty("Amount").GetDecimal() == 1000m && metadata.RootElement.GetProperty("AcademyId").GetGuid() == academy, label + ": create audit metadata mismatch");
                        else Check(metadata.RootElement.GetProperty("Status").GetString() == expectedStatus, label + ": status audit metadata mismatch");
                    }
                }
                foreach (var field in old.EnumerateObject().Where(x => x.Name is not "invoices" and not "platformAudits" and not "academyAudits")) Check(field.Value.GetRawText() == now.GetProperty(field.Name).GetRawText(), label + ": unrelated captured collection changed");
            }
            if (billed.HasValue)
            {
                Check(output.GetProperty("totalBilled").GetDecimal() == billed && output.GetProperty("collectedBilling").GetDecimal() == collected && output.GetProperty("outstandingBilling").GetDecimal() == outstanding && output.GetProperty("overdueInvoices").GetInt32() == overdue, label + ": independent financial expectation mismatch");
                if (!(baseline && label == "baseline-submitted-gap")) Check(billed == collected + outstanding, label + ": billed does not reconcile");
            }
            Console.WriteLine("PLATFORMBILLING EVIDENCE " + JsonSerializer.Serialize(new { label, method = method.Method, url, actor, requestBody = body, status = (int)response.StatusCode, response = text, mutation, beforeDigest = Hash(before), afterDigest = Hash(after), bodyDigest = Hash(text), addedAudit, billed, collected, outstanding, overdue, exactSnapshotVerified = true }));
            if (!baseline) { count++; Console.WriteLine("PLATFORMBILLING CASE " + label + " PASS."); }
            return output;
        }
        Task<JsonElement> Overview(string label, decimal billed, decimal collected, decimal outstanding, int overdue = 0) => Request(label, HttpMethod.Get, "/api/platform/overview", "platform", null, HttpStatusCode.OK, billed: billed, collected: collected, outstanding: outstanding, overdue: overdue);
        string Submission(Guid id, Guid? tenant = null) => $"/api/academies/{tenant ?? academy}/platform-services/billing-invoices/{id}/payment-submission";
        var createBody = JsonSerializer.Serialize(new { academyId = academy, invoiceNumber = "QA-PLATFORM-LIFECYCLE", amount = 1000m, currency = "INR", periodStart = "2001-01-01", periodEnd = "2001-01-31", dueDate = "2001-02-07" });
        if (!baseline) await Overview("empty-overview", 0, 0, 0);
        var created = await Request("create-draft", HttpMethod.Post, "/api/platform/billing-invoices", "platform", createBody, HttpStatusCode.Created, "create", expectedStatus: "Draft");
        var invoiceId = created.GetProperty("id").GetGuid();
        Task<JsonElement> SetStatus(string state) => Request("owner-status-" + state, HttpMethod.Patch, $"/api/platform/billing-invoices/{invoiceId}/status", "platform", JsonSerializer.Serialize(new { status = state }), HttpStatusCode.OK, "status", invoiceId, state);
        if (!baseline) await Overview("draft-excluded", 0, 0, 0);
        await SetStatus("Issued"); await Overview("issued-outstanding", 1000, 0, 1000);
        await Request("submit-null-reference", HttpMethod.Post, Submission(invoiceId), "admin", "{\"reference\":null}", HttpStatusCode.OK, "submit", invoiceId, "Payment submitted");
        if (baseline)
        {
            await Overview("baseline-submitted-gap", 1000, 0, 0);
            Console.WriteLine("PLATFORMBILLING BASELINE: real create/issue/admin submission left billed=1000, collected=0, outstanding=0; unpaid deficit=1000. Exact invoice/audit readback, no passing repair cases.");
            return;
        }
        await Overview("submitted-still-outstanding", 1000, 0, 1000);
        await Request("repeat-submission-reference", HttpMethod.Post, Submission(invoiceId), "admin", "{\"reference\":\"  QA-SYNTHETIC  \"}", HttpStatusCode.OK, "submit", invoiceId, "Payment submitted", "QA-SYNTHETIC");
        await Overview("repeated-claim-not-collected", 1000, 0, 1000);
        await Request("repeat-submission-empty-reference", HttpMethod.Post, Submission(invoiceId), "admin", "{\"reference\":\"   \"}", HttpStatusCode.OK, "submit", invoiceId, "Payment submitted", "");
        await Overview("empty-reference-unpaid", 1000, 0, 1000);
        await SetStatus("Overdue"); await Overview("overdue-outstanding", 1000, 0, 1000, 1);
        await Request("overdue-submit-omitted-reference", HttpMethod.Post, Submission(invoiceId), "Owner", "{}", HttpStatusCode.OK, "submit", invoiceId, "Payment submitted");
        await Overview("overdue-submission-unpaid", 1000, 0, 1000);
        await SetStatus("Paid"); await Overview("owner-approved-collected", 1000, 1000, 0);
        await Request("paid-submission-rejected", HttpMethod.Post, Submission(invoiceId), "admin", "{}", HttpStatusCode.BadRequest);
        await Overview("rejected-paid-claim-unchanged", 1000, 1000, 0);
        await SetStatus("Void"); await Overview("void-excluded", 0, 0, 0);
        await Request("void-submission-rejected", HttpMethod.Post, Submission(invoiceId), "admin", "{}", HttpStatusCode.BadRequest);
        await Overview("rejected-void-claim-unchanged", 0, 0, 0);
        await SetStatus("Draft"); await Overview("reset-draft-excluded", 0, 0, 0);
        // Current API permits submitting a Draft. Preserve/test compatibility, not a new workflow policy.
        await Request("draft-submission-compatibility", HttpMethod.Post, Submission(invoiceId), "admin", "{}", HttpStatusCode.OK, "submit", invoiceId, "Payment submitted");
        await Overview("draft-claim-is-unpaid", 1000, 0, 1000);
        foreach (var actor in new string?[] { null, "admin", "foreign-admin", "teacher", "Owner", "Manager", "FinanceUser", "PlatformOwner" })
            await Request("overview-denied-" + (actor ?? "anonymous"), HttpMethod.Get, "/api/platform/overview", actor, null, actor is null ? HttpStatusCode.Unauthorized : HttpStatusCode.Forbidden);
        await Request("cross-tenant-route", HttpMethod.Post, Submission(invoiceId, foreign), "admin", "{}", HttpStatusCode.Forbidden);
        await Request("cross-tenant-admin", HttpMethod.Post, Submission(invoiceId), "foreign-admin", "{}", HttpStatusCode.Forbidden);
        foreach (var actor in new string?[] { null, "teacher", "Manager", "FinanceUser", "PlatformOwner" })
            await Request("submission-denied-" + (actor ?? "anonymous"), HttpMethod.Post, Submission(invoiceId), actor, "{}", actor is null ? HttpStatusCode.Unauthorized : HttpStatusCode.Forbidden);
        await Request("missing-invoice", HttpMethod.Post, Submission(Guid.NewGuid()), "admin", "{}", HttpStatusCode.NotFound);
        await Request("foreign-invoice-own-route", HttpMethod.Post, Submission(invoiceId, foreign), "foreign-admin", "{}", HttpStatusCode.NotFound);
        foreach (var (label, body) in new[] { ("malformed", "{bad"), ("null-body", "null"), ("empty-body", ""), ("numeric-reference", "{\"reference\":123}") })
            await Request("submission-invalid-" + label, HttpMethod.Post, Submission(invoiceId), "admin", body, HttpStatusCode.BadRequest);
        await Overview("denials-do-not-change-balance", 1000, 0, 1000);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            foreach (var (state, amount) in new[] { ("Draft", 77.77m), ("Issued", 1.11m), ("Overdue", 2.22m), ("Payment submitted", 3.33m), ("Paid", 4.44m), ("Void", 99.99m), ("Payment submitted", 0m) })
                db.PlatformBillingInvoices.Add(new PlatformBillingInvoice { AcademyId = state is "Paid" or "Payment submitted" ? foreign : academy, InvoiceNumber = "QA-BALANCE-" + Guid.NewGuid().ToString("N"), Amount = amount, Currency = "INR", Status = state, PeriodStart = new(2001, 1, 1), PeriodEnd = new(2001, 1, 31), DueDate = new(2001, 2, 7) });
            await db.SaveChangesAsync();
        }
        await Overview("two-tenant-decimal-zero-aggregate", 1011.10m, 4.44m, 1006.66m, 1);
        await Overview("repeat-aggregate-read-no-write", 1011.10m, 4.44m, 1006.66m, 1);
        Console.WriteLine($"PLATFORMBILLING REGRESSION PASS: {count} real Identity/HTTP-SQL cases; independent INR lifecycle/decimal/two-tenant totals, exact invoice/audit writes and unchanged reads/denials. Not browser/currency/race/full-critical acceptance.");
    }
}
