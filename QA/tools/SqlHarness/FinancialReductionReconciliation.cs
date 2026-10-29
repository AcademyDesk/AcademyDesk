using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

internal static partial class SqlHarnessEntryPoint
{
    // Additive to the retained cloud finance-integrity regression. Real SQL,
    // actual HTTP/Identity/audit and explicitly observed lock contention.
    private static async Task VerifyFinancialReductionReconciliationAsync(QaApiFactory factory, HttpClient client, QaRunManifest manifest)
    {
        Guid academy, student;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            academy = await db.Academies.Where(x => x.Name == "Synthetic Academy A").Select(x => x.Id).SingleAsync();
            student = await db.Students.Where(x => x.AcademyId == academy && x.FirstName == "Isolated-A").Select(x => x.Id).SingleAsync();
        }
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await LoginAsync(client, "qa-admin-a@example.invalid", "Synthetic!39Ab"));
        var adjustments = $"/api/academies/{academy}/finance-adjustments";
        var payments = $"/api/academies/{academy}/payments";
        var checks = new List<string>(); var races = new List<object>();
        void Pass(string label) { checks.Add(label); Console.WriteLine("FINANCE-RECONCILE CASE " + label + " PASS."); }
        async Task<(Guid Invoice, Guid[] Adjustments)> Seed(decimal amount = 400m, int proposals = 1, string type = "Discount", bool overdue = false)
        {
            using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var invoice = new Invoice { AcademyId = academy, StudentId = student, InvoiceNumber = "QR-" + Guid.NewGuid().ToString("N"), TotalAmount = 1000m, Status = "PartiallyPaid" };
            if (overdue) { invoice.DueDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-7); invoice.Status = "Overdue"; }
            db.Add(invoice);
            if (!overdue) db.Add(new Payment { AcademyId = academy, InvoiceId = invoice.Id, Amount = 600m, Status = "Reconciled", ReconciliationReference = "QA-RECONCILED", ReconciledAtUtc = DateTime.UtcNow });
            var rows = Enumerable.Range(0, proposals).Select(_ => new FinanceAdjustment { AcademyId = academy, InvoiceId = invoice.Id, Amount = amount, Type = type, Reason = "Synthetic reduction reconciliation" }).ToArray();
            db.AddRange(rows); await db.SaveChangesAsync(); return (invoice.Id, rows.Select(x => x.Id).ToArray());
        }
        async Task<FinanceIntegrityState> Read(Guid id)
        {
            using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            return new(await db.Invoices.AsNoTracking().SingleAsync(x => x.Id == id && x.AcademyId == academy),
                await db.Payments.AsNoTracking().Where(x => x.AcademyId == academy && x.InvoiceId == id).OrderBy(x => x.Id).ToArrayAsync(),
                await db.FinanceAdjustments.AsNoTracking().Where(x => x.AcademyId == academy && x.InvoiceId == id).OrderBy(x => x.Id).ToArrayAsync(),
                await db.AuditLogs.AsNoTracking().Where(x => x.AcademyId == academy).OrderBy(x => x.Id).ToArrayAsync());
        }
        async Task Unchanged(Guid id, FinanceIntegrityState before)
        {
            if (JsonSerializer.Serialize(before) != JsonSerializer.Serialize(await Read(id))) throw new InvalidOperationException("Rejected reduction changed ledger, decision or audit.");
        }
        foreach (var type in new[] { "Refund", "refund", "CreditNote", "creditnote" })
        {
            var f = await Seed(); var before = await Read(f.Invoice);
            using var denied = await client.PostAsJsonAsync(adjustments, new { invoiceId = f.Invoice, type, amount = 200m, reason = "Synthetic" });
            RequireFinanceStatus(denied, HttpStatusCode.BadRequest, "generic settlement document"); await Unchanged(f.Invoice, before);
            Pass("generic-" + type + "-refused-no-write");
        }
        foreach (var type in new[] { "Refund", "CreditNote" }) foreach (var approve in new[] { true, false })
        {
            var f = await Seed(type: type); var before = await Read(f.Invoice);
            using var denied = await client.PatchAsJsonAsync($"{adjustments}/{f.Adjustments[0]}/approval", new { approve });
            RequireFinanceStatus(denied, HttpStatusCode.Conflict, "legacy reviewed disposition"); await Unchanged(f.Invoice, before);
            using var list = await client.GetAsync(adjustments + "?invoiceId=" + f.Invoice); RequireFinanceStatus(list, HttpStatusCode.OK, "legacy disposition display");
            using var json = JsonDocument.Parse(await list.Content.ReadAsStringAsync()); var row = json.RootElement.EnumerateArray().Single();
            if (row.GetProperty("financialEffect").GetString() != "LegacyInvoiceReductionNotSettlement" || !row.GetProperty("requiresReviewedDisposition").GetBoolean()) throw new InvalidOperationException("Legacy financial meaning missing from read contract.");
            Pass("legacy-" + type + "-decision-" + approve + "-review-required");
        }
        foreach (var amount in new[] { 0.001m, 1000.001m, 10000000000000000m })
        {
            var f = await Seed(); var before = await Read(f.Invoice);
            using var denied = await client.PostAsJsonAsync(adjustments, new { invoiceId = f.Invoice, type = "Discount", amount, reason = "Synthetic" });
            RequireFinanceStatus(denied, HttpStatusCode.BadRequest, "precision/range refusal"); await Unchanged(f.Invoice, before); Pass("precision-range-" + checks.Count + "-no-write");
        }
        var excess = await Seed(400.01m); var excessBefore = await Read(excess.Invoice);
        using (var denied = await client.PatchAsJsonAsync($"{adjustments}/{excess.Adjustments[0]}/approval", new { approve = true }))
        {
            RequireFinanceStatus(denied, HttpStatusCode.BadRequest, "reconciled balance cap");
            using var body = JsonDocument.Parse(await denied.Content.ReadAsStringAsync());
            if (body.RootElement.GetProperty("message").GetString() != "Adjustment exceeds the remaining invoice balance.") throw new InvalidOperationException("Balance refusal message differs.");
        }
        await Unchanged(excess.Invoice, excessBefore); Pass("reconciled-600-reduction-40001-refused-no-write");
        var exact = await Seed();
        using (var success = await client.PatchAsJsonAsync($"{adjustments}/{exact.Adjustments[0]}/approval", new { approve = true })) RequireFinanceStatus(success, HttpStatusCode.OK, "exact remaining reduction");
        var settled = await Read(exact.Invoice);
        if (settled.Invoice.AdjustedAmount != 400m || settled.Invoice.Status != "Paid" || settled.Payments.Single().Amount != 600m || settled.Payments.Single().Status != "Reconciled") throw new InvalidOperationException("Exact reduction changed reconciled collection or failed settlement.");
        Pass("reconciled-600-reduction-400-exact-settlement");
        var overdue = await Seed(200m, overdue: true);
        using (var success = await client.PatchAsJsonAsync($"{adjustments}/{overdue.Adjustments[0]}/approval", new { approve = true })) RequireFinanceStatus(success, HttpStatusCode.OK, "overdue reduction");
        if ((await Read(overdue.Invoice)).Invoice.Status != "Overdue") throw new InvalidOperationException("Reduction incorrectly relabelled overdue invoice."); Pass("overdue-reduction-remains-overdue");
        var stale = await Seed(100m);
        using (var staleScope = factory.Services.CreateScope())
        {
            var db = staleScope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            _ = await db.FinanceAdjustments.SingleAsync(x => x.Id == stale.Adjustments[0]);
            using var applied = await client.PatchAsJsonAsync($"{adjustments}/{stale.Adjustments[0]}/approval", new { approve = true }); RequireFinanceStatus(applied, HttpStatusCode.OK, "first cached proposal approval");
            var before = await Read(stale.Invoice);
            var duplicate = await new FinanceAdjustmentsController(db).Decide(academy, stale.Adjustments[0], new(true, null), default);
            if (duplicate.Result is not ConflictObjectResult) throw new InvalidOperationException("Stale tracked proposal was applied twice.");
            await Unchanged(stale.Invoice, before);
        }
        Pass("stale-tracked-proposal-refreshed-under-invoice-lock");
        foreach (var mode in new[] { "currency", "type" })
        {
            var f = await Seed();
            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); var item = await db.FinanceAdjustments.SingleAsync(x => x.Id == f.Adjustments[0]);
                if (mode == "currency") item.Currency = "USD"; else item.Type = "Unsupported"; await db.SaveChangesAsync();
            }
            var before = await Read(f.Invoice);
            using var denied = await client.PatchAsJsonAsync($"{adjustments}/{f.Adjustments[0]}/approval", new { approve = true }); RequireFinanceStatus(denied, HttpStatusCode.BadRequest, "stored proposal validation");
            await Unchanged(f.Invoice, before); Pass("stored-" + mode + "-refused-no-write");
        }
        foreach (var mode in new[] { "collection-reduction", "two-reductions" }) for (var attempt = 1; attempt <= 5; attempt++)
        {
            var f = await Seed(proposals: mode == "two-reductions" ? 2 : 1); var before = await Read(f.Invoice);
            using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            await using var held = await db.Database.BeginTransactionAsync();
            _ = await db.Invoices.FromSqlInterpolated($"SELECT * FROM [Invoices] WITH (UPDLOCK, HOLDLOCK) WHERE [AcademyId] = {academy} AND [Id] = {f.Invoice}").SingleAsync();
            var first = client.PatchAsJsonAsync($"{adjustments}/{f.Adjustments[0]}/approval", new { approve = true });
            var second = mode == "two-reductions" ? client.PatchAsJsonAsync($"{adjustments}/{f.Adjustments[1]}/approval", new { approve = true }) : client.PostAsJsonAsync(payments, new { invoiceId = f.Invoice, amount = 400m });
            await RequireInvoiceWaitersAsync(manifest, 2);
            if (first.IsCompleted || second.IsCompleted) throw new InvalidOperationException("Reduction race completed before held invoice lock was released.");
            await held.CommitAsync(); var responses = await Task.WhenAll(first, second);
            try
            {
                var codes = responses.Select(x => (int)x.StatusCode).Order().ToArray();
                if (codes.Length != 2 || codes[1] != 400 || (mode == "two-reductions" ? codes[0] != 200 : codes[0] is not (200 or 201))) throw new InvalidOperationException("Reduction race must have one exact success and one400;429 never qualifies.");
                var after = await Read(f.Invoice); var paid = after.Payments.Where(x => x.Status is "Completed" or "Reconciled").Sum(x => x.Amount);
                var approved = after.Adjustments.Where(x => x.Status == "Approved").Sum(x => x.Amount);
                if (after.Invoice.TotalAmount != 1000m || after.Invoice.AdjustedAmount != approved || paid + approved != 1000m || after.Invoice.Status != "Paid") throw new InvalidOperationException("Reduction race exceeded adjusted balance or lost settlement.");
                if (mode == "two-reductions" && (paid != 600m || approved != 400m || after.Adjustments.Count(x => x.Status == "Approved") != 1)) throw new InvalidOperationException("Competing proposals both applied.");
                if (mode == "collection-reduction" && (after.Payments.Length != (paid == 1000m ? 2 : 1) || after.Adjustments.Count(x => x.Status == "Approved") != (approved == 400m ? 1 : 0))) throw new InvalidOperationException("Mixed race saved the rejected financial row/decision.");
                var added = after.Audits.Where(x => before.Audits.All(y => y.Id != x.Id)).ToArray();
                if (added.Length != 1 || JsonSerializer.Serialize(before.Audits) != JsonSerializer.Serialize(after.Audits.Where(x => added.All(y => y.Id != x.Id)))) throw new InvalidOperationException("Reduction race audit count/state mismatch.");
                races.Add(new { mode, attempt, statuses = codes, collected = paid, adjusted = approved, paymentRows = after.Payments.Length }); Pass(mode + "-race-" + attempt);
            }
            finally { foreach (var response in responses) response.Dispose(); }
        }
        var audit = await Seed(); var auditBefore = await Read(audit.Invoice); await SetAuditInsertDeniedAsync(manifest, true);
        try
        {
            using var denied = await client.PatchAsJsonAsync($"{adjustments}/{audit.Adjustments[0]}/approval", new { approve = true }); RequireFinanceStatus(denied, HttpStatusCode.InternalServerError, "reduction mandatory audit fault"); await Unchanged(audit.Invoice, auditBefore);
        }
        finally { await SetAuditInsertDeniedAsync(manifest, false); }
        Pass("mandatory-audit-failure-rolls-back-reduction-and-decision");
        if (checks.Count != 28 || races.Count != 10) throw new InvalidOperationException("Financial reduction case inventory differs.");
        var folder = Path.Combine("QA", "EVIDENCE", "finance-reconciliation-sql-" + manifest.RunId.ToString("N")); Directory.CreateDirectory(folder);
        await File.WriteAllTextAsync(Path.Combine(folder, "result.json"), JsonSerializer.Serialize(new { run = manifest.RunId.ToString("N"), passed = true, checks, observedWaiterRaces = races, auditRollbacks = 1, modelInference = false, productionAccepted = false }, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine("FINANCE-RECONCILE PASS:28 additional real SQL/HTTP cases;10 observed two-waiter races; required-audit rollback; legacy review/precision/balance/stale-proposal safeguards. Retained cloud restoration policy unchanged.");
    }
}
