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
    private static async Task VerifyCollectionsBalanceAsync(QaApiFactory factory, HttpClient client, bool baseline)
    {
        Guid academyA, academyB; var fixtures = new Dictionary<string, (Guid Id, decimal Balance, bool Visible)>();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            academyA = await db.Academies.Where(x => x.Name == "Synthetic Academy A").Select(x => x.Id).SingleAsync();
            academyB = await db.Academies.Where(x => x.Name == "Synthetic Academy B").Select(x => x.Id).SingleAsync();
            var student = await db.Students.Where(x => x.AcademyId == academyA).Select(x => x.Id).SingleAsync();
            void Fixture(string name, decimal adjusted, decimal completed, decimal reconciled, decimal voided, decimal balance,
                bool visible = true, string status = "Issued", int dueDays = -1, string currency = "INR", Guid? academy = null)
            {
                var invoice = new Invoice { AcademyId = academy ?? academyA, StudentId = student, InvoiceNumber = "QA-COL-" + name,
                    TotalAmount = 1000m, AdjustedAmount = adjusted, Currency = currency, Status = status, DueDate = today.AddDays(dueDays) };
                db.Invoices.Add(invoice); fixtures.Add(name, (invoice.Id, balance, visible));
                foreach (var payment in new[] { (completed, "Completed"), (reconciled, "Reconciled"), (voided, "Voided") })
                    if (payment.Item1 > 0) db.Payments.Add(new Payment { AcademyId = invoice.AcademyId, InvoiceId = invoice.Id,
                        Amount = payment.Item1, Status = payment.Item2, Currency = currency });
            }
            // Independent expected constants; no production balance helper as the oracle.
            Fixture("original", 100m, 400m, 0m, 75m, 500m);
            Fixture("mixed", 200m, 100m, 600m, 75m, 100m);
            Fixture("unpaid", 0m, 0m, 0m, 0m, 1000m, dueDays: -30);
            Fixture("void-only", 0m, 0m, 0m, 1000m, 1000m);
            Fixture("cent", 200m, 799.99m, 0m, 0m, 0.01m);
            Fixture("settled-issued", 200m, 800m, 0m, 0m, 0m, false);
            Fixture("fully-adjusted-issued", 1000m, 0m, 0m, 0m, 0m, false);
            Fixture("overpaid-issued", 200m, 900m, 0m, 0m, 0m, false);
            Fixture("paid", 0m, 1000m, 0m, 0m, 0m, false, "Paid");
            Fixture("cancelled", 0m, 0m, 0m, 0m, 1000m, false, "Cancelled");
            Fixture("today", 0m, 0m, 0m, 0m, 1000m, false, dueDays: 0);
            Fixture("future", 0m, 0m, 0m, 0m, 1000m, false, dueDays: 1);
            Fixture("USD", 100m, 300m, 200m, 0m, 400m, currency: "USD");
            Fixture("pending-adjustment", 0m, 400m, 0m, 0m, 600m);
            Fixture("rejected-adjustment", 0m, 400m, 0m, 0m, 600m);
            Fixture("foreign", 0m, 0m, 0m, 0m, 1000m, false, academy: academyB);
            foreach (var adjustment in new[] { ("original", "Approved"), ("pending-adjustment", "PendingApproval"), ("rejected-adjustment", "Rejected") })
                db.FinanceAdjustments.Add(new FinanceAdjustment { AcademyId = academyA, InvoiceId = fixtures[adjustment.Item1].Id,
                    Amount = 100m, Reason = "Synthetic collections fixture", Status = adjustment.Item2,
                    AppliedAtUtc = adjustment.Item2 == "Approved" ? DateTime.UtcNow : null });
            await db.SaveChangesAsync();
        }
        var root = $"/api/academies/{academyA}"; var count = 0;
        var admin = await LoginAsync(client, "qa-admin-a@example.invalid", "Synthetic!39Ab");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", admin);
        async Task<JsonDocument> Read(string label, string url, HttpStatusCode expected = HttpStatusCode.OK)
        {
            await Task.Delay(650); var before = await GovernanceStateAsync(factory);
            using var response = await client.GetAsync(url); RequireFinanceStatus(response, expected, label);
            if (JsonSerializer.Serialize(before) != JsonSerializer.Serialize(await GovernanceStateAsync(factory))) throw new InvalidOperationException("Collections read changed state: " + label);
            count++; Console.WriteLine($"COLLECTIONS CASE {label} PASS: HTTP={(int)expected}; captured state unchanged.");
            return JsonDocument.Parse(expected == HttpStatusCode.OK ? await response.Content.ReadAsStringAsync() : "{}");
        }
        using var invoiceRows = await Read("canonical-invoices", root + "/invoices");
        using var queue = await Read("queue", root + "/finance-governance/collections");
        var rows = queue.RootElement.EnumerateArray().ToDictionary(x => x.GetProperty("id").GetGuid());
        if (baseline)
        {
            var original = rows[fixtures["original"].Id];
            var canonical = invoiceRows.RootElement.EnumerateArray().Single(x => x.GetProperty("id").GetGuid() == fixtures["original"].Id);
            if (original.GetProperty("totalAmount").GetDecimal() != 1000m || canonical.GetProperty("balance").GetDecimal() != 500m || original.TryGetProperty("balance", out _))
                throw new InvalidOperationException("Expected original collection mismatch was not reproduced.");
            Console.WriteLine("COLLECTIONS BASELINE REPRODUCED: gross=1000; canonical remaining=500; collection balance missing; zero-balance Issued invoice included=" + rows.ContainsKey(fixtures["settled-issued"].Id));
            return;
        }
        if (rows.Count != fixtures.Count(x => x.Value.Visible)) throw new InvalidOperationException("Queue count mismatch");
        foreach (var fixture in fixtures)
        {
            if (rows.ContainsKey(fixture.Value.Id) != fixture.Value.Visible) throw new InvalidOperationException("Queue visibility mismatch: " + fixture.Key);
            if (fixture.Value.Visible)
            {
                var row = rows[fixture.Value.Id];
                var canonical = invoiceRows.RootElement.EnumerateArray().Single(x => x.GetProperty("id").GetGuid() == fixture.Value.Id);
                if (row.GetProperty("balance").GetDecimal() != fixture.Value.Balance || canonical.GetProperty("balance").GetDecimal() != fixture.Value.Balance ||
                    row.GetProperty("totalAmount").GetDecimal() != 1000m || row.GetProperty("paidAmount").GetDecimal() != canonical.GetProperty("paidAmount").GetDecimal() ||
                    row.GetProperty("adjustedAmount").GetDecimal() != canonical.GetProperty("adjustedAmount").GetDecimal() ||
                    row.GetProperty("currency").GetString() != (fixture.Key == "USD" ? "USD" : "INR")) throw new InvalidOperationException("Balance/contract mismatch: " + fixture.Key);
                if (row.GetProperty("daysOverdue").GetInt32() != (fixture.Key == "unpaid" ? 30 : 1)) throw new InvalidOperationException("Overdue days mismatch");
            }
            count++; Console.WriteLine($"COLLECTIONS CASE fixture/{fixture.Key} PASS: visible={fixture.Value.Visible}; independent balance={fixture.Value.Balance}.");
        }
        using var summary = await Read("summary-overdue-count", root + "/finance-governance/summary");
        if (summary.RootElement.GetProperty("overdueInvoices").GetInt32() != rows.Count) throw new InvalidOperationException("Summary overdue count disagrees with queue");
        // A stale selection must not create collection work after exact settlement.
        foreach (var target in new[] { "settled-issued", "fully-adjusted-issued", "overpaid-issued", "paid", "cancelled", "foreign", "missing" })
        {
            await Task.Delay(650); var before = await GovernanceStateAsync(factory);
            var id = target == "missing" ? Guid.NewGuid() : fixtures[target].Id;
            using var response = await client.PostAsJsonAsync(root + $"/finance-governance/collections/{id}/follow-up", new { note = "Synthetic", priority = "High" });
            RequireFinanceStatus(response, target is "foreign" or "missing" ? HttpStatusCode.NotFound : HttpStatusCode.Conflict, target);
            if (JsonSerializer.Serialize(before) != JsonSerializer.Serialize(await GovernanceStateAsync(factory))) throw new InvalidOperationException("Rejected follow-up changed state");
            count++; Console.WriteLine($"COLLECTIONS CASE follow-up/{target} PASS: rejected; captured state unchanged.");
        }
        await Task.Delay(650); var beforeCreate = await GovernanceStateAsync(factory);
        using var create = await client.PostAsJsonAsync(root + $"/finance-governance/collections/{fixtures["cent"].Id}/follow-up", new { note = "Synthetic cent follow-up", priority = "High" });
        RequireFinanceStatus(create, HttpStatusCode.OK, "cent follow-up");
        using var created = JsonDocument.Parse(await create.Content.ReadAsStringAsync()); var taskId = created.RootElement.GetProperty("id").GetGuid();
        var afterCreate = await GovernanceStateAsync(factory);
        if (beforeCreate.Stable != afterCreate.Stable || afterCreate.Tasks.Count != beforeCreate.Tasks.Count + 1 || afterCreate.Audits.Count != beforeCreate.Audits.Count + 1 ||
            afterCreate.Tasks.Single(x => x.Id == taskId).EntityId != fixtures["cent"].Id ||
            JsonSerializer.Serialize(beforeCreate.Tasks) != JsonSerializer.Serialize(afterCreate.Tasks.Where(x => x.Id != taskId))) throw new InvalidOperationException("Follow-up write crossed boundary");
        count++; Console.WriteLine("COLLECTIONS CASE follow-up/cent PASS: one own linked task/audit; financial state unchanged.");
        await CreateAccessActorAsync(factory, "qa-collections-finance@example.invalid", "FinanceUser", academyA);
        var financeToken = await LoginAsync(client, "qa-collections-finance@example.invalid", "Synthetic!39Ab");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", financeToken);
        using var financeQueue = await Read("FinanceUser/queue", root + "/finance-governance/collections");
        if (financeQueue.RootElement.GetRawText() != queue.RootElement.GetRawText()) throw new InvalidOperationException("FinanceUser queue mismatch");
        using var foreignRead = await Read("FinanceUser/foreign", $"/api/academies/{academyB}/finance-governance/collections", HttpStatusCode.Forbidden);
        client.DefaultRequestHeaders.Authorization = null;
        using var anonymous = await Read("anonymous", root + "/finance-governance/collections", HttpStatusCode.Unauthorized);
        Console.WriteLine($"COLLECTIONS REGRESSION PASS: {count} bounded cases; independent balances, currencies, eligibility, follow-up rejection and scoped access; no finance writes by reads.");
    }
}
