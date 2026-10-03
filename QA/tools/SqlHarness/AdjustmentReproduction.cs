using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

internal static partial class SqlHarnessEntryPoint
{
    private static async Task VerifyAdjustedBalanceAsync(QaApiFactory factory, HttpClient client, bool enforceRegression = false)
    {
        Guid academyId;
        Guid studentId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            academyId = await db.Academies.AsNoTracking().Where(x => x.Name == "Synthetic Academy A")
                .Select(x => x.Id).SingleAsync();
            studentId = await db.Students.AsNoTracking().Where(x => x.AcademyId == academyId && x.FirstName == "Isolated-A")
                .Select(x => x.Id).SingleAsync();
        }
        var adminToken = await LoginAsync(client, "qa-admin-a@example.invalid", "Synthetic!39Ab");
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);
        var invoicesPath = $"/api/academies/{academyId}/invoices";
        var paymentsPath = $"/api/academies/{academyId}/payments";
        var adjustmentPath = $"/api/academies/{academyId}/finance-adjustments";

        var overpaymentId = await CreateApprovedAdjustmentFixtureAsync(factory, client, academyId, studentId,
            invoicesPath, adjustmentPath, 200m, "overpayment");
        var overpaymentBefore = await CollectAdjustmentBaselineAsync(factory, client, academyId, studentId,
            overpaymentId, invoicesPath, paymentsPath, "overpayment");
        using var extra = await client.PostAsJsonAsync(paymentsPath, new { invoiceId = overpaymentId, amount = 300m });
        RequireCollectionObservation(extra, "adjusted overpayment 300");
        var overpaymentAfter = await ReadAdjustmentSnapshotAsync(factory, academyId, studentId, overpaymentId, "overpayment-300");
        ValidateCollectionPersistence(extra, overpaymentBefore, overpaymentAfter, 300m);
        var overpaymentView = await ReadAdjustedInvoiceViewAsync(client, invoicesPath, overpaymentId);
        if (overpaymentView.Paid != overpaymentAfter.Payments.Sum(x => x.Amount))
            throw new InvalidOperationException("Overpayment admin view does not match its actual ledger.");
        var overpaymentFailed = extra.StatusCode != HttpStatusCode.BadRequest;

        var settlementId = await CreateApprovedAdjustmentFixtureAsync(factory, client, academyId, studentId,
            invoicesPath, adjustmentPath, 200m, "settlement");
        var settlementBefore = await CollectAdjustmentBaselineAsync(factory, client, academyId, studentId,
            settlementId, invoicesPath, paymentsPath, "settlement");
        using var settle = await client.PostAsJsonAsync(paymentsPath, new { invoiceId = settlementId, amount = 200m });
        RequireCollectionObservation(settle, "exact adjusted balance 200");
        var settlementAfter = await ReadAdjustmentSnapshotAsync(factory, academyId, studentId, settlementId, "settlement-200");
        ValidateCollectionPersistence(settle, settlementBefore, settlementAfter, 200m);
        var settlementView = await ReadAdjustedInvoiceViewAsync(client, invoicesPath, settlementId);
        var settlementFailed = settle.StatusCode != HttpStatusCode.Created || settlementAfter.Status != "Paid" ||
            settlementView != (800m, 0m, "Paid");

        var fullId = await CreateApprovedAdjustmentFixtureAsync(factory, client, academyId, studentId,
            invoicesPath, adjustmentPath, 1000m, "full-adjustment");
        var fullBefore = await ReadAdjustmentSnapshotAsync(factory, academyId, studentId, fullId, "full-before-collection");
        var fullViewBefore = await ReadAdjustedInvoiceViewAsync(client, invoicesPath, fullId);
        if (fullBefore.Payments.Count != 0 || fullBefore.Adjusted != 1000m || fullBefore.Status != "Paid" ||
            fullViewBefore != (0m, 0m, "Paid"))
            throw new InvalidOperationException("Full-adjustment fixture did not have zero balance and Paid status.");
        using var fullPayment = await client.PostAsJsonAsync(paymentsPath, new { invoiceId = fullId, amount = 1m });
        RequireCollectionObservation(fullPayment, "payment against fully adjusted invoice");
        var fullAfter = await ReadAdjustmentSnapshotAsync(factory, academyId, studentId, fullId, "full-collection-1");
        ValidateCollectionPersistence(fullPayment, fullBefore, fullAfter, 1m);
        var fullViewAfter = await ReadAdjustedInvoiceViewAsync(client, invoicesPath, fullId);
        var fullFailed = fullPayment.StatusCode != HttpStatusCode.BadRequest || fullAfter.Status != "Paid" ||
            fullViewAfter != (0m, 0m, "Paid");

        Console.WriteLine($"FINANCE-RULE-002 {(overpaymentFailed || settlementFailed || fullFailed ? "FAIL" : "PASS")}: " +
            "independent invoices 1000; approved adjustment 200 + collected 600 => admin balance 200; " +
            $"extra 300 HTTP={(int)extra.StatusCode}/ledger={overpaymentAfter.Payments.Sum(x => x.Amount)}/rows={overpaymentAfter.Payments.Count}/balance={overpaymentView.Balance}; " +
            $"exact 200 HTTP={(int)settle.StatusCode}/ledger={settlementAfter.Payments.Sum(x => x.Amount)}/balance={settlementView.Balance}/SQL status={settlementAfter.Status}/view status={settlementView.Status}; " +
            $"full adjustment 1000 starts Paid/balance=0; extra 1 HTTP={(int)fullPayment.StatusCode}/ledger={fullAfter.Payments.Sum(x => x.Amount)}/SQL status={fullAfter.Status}; " +
            "expected reject 300 unchanged, accept 200 with Paid/zero balance, reject 1 unchanged; pending/invalid/reapproval controls PASS.");
        if (enforceRegression)
        {
            if (overpaymentFailed || settlementFailed || fullFailed)
                throw new InvalidOperationException("FINANCE-RULE-002 repair regression failed.");
            await VerifyAdjustedPaymentRegressionAsync(factory, client, academyId, studentId,
                invoicesPath, paymentsPath, adjustmentPath);
        }
    }

    private static async Task<Guid> CreateApprovedAdjustmentFixtureAsync(QaApiFactory factory, HttpClient client,
        Guid academyId, Guid studentId, string invoicesPath, string adjustmentPath, decimal amount, string scenario)
    {
        // The product invoice number has a seconds-resolution prefix plus a random suffix.
        // Separate fixture creation seconds to avoid unrelated random-number collisions.
        await Task.Delay(1100);
        using var create = await client.PostAsJsonAsync(invoicesPath, new { studentId, amount = 1000m });
        RequireFinanceStatus(create, HttpStatusCode.Created, scenario + " invoice create");
        using var invoiceJson = JsonDocument.Parse(await create.Content.ReadAsStringAsync());
        var invoiceId = invoiceJson.RootElement.GetProperty("id").GetGuid();
        var initial = await ReadAdjustmentSnapshotAsync(factory, academyId, studentId, invoiceId, scenario + "-created");
        if (initial.Adjusted != 0m || initial.Status != "Issued" || initial.Adjustments.Count != 0 || initial.Payments.Count != 0)
            throw new InvalidOperationException("Fresh adjusted-invoice fixture was not empty.");
        using var invalid = await client.PostAsJsonAsync(adjustmentPath,
            new { invoiceId, type = "Discount", amount = 0m, reason = "Synthetic invalid adjustment" });
        RequireFinanceStatus(invalid, HttpStatusCode.BadRequest, scenario + " zero adjustment");
        RequireUnchangedAdjustment(initial,
            await ReadAdjustmentSnapshotAsync(factory, academyId, studentId, invoiceId, scenario + "-invalid-adjustment"));
        using var createAdjustment = await client.PostAsJsonAsync(adjustmentPath,
            new { invoiceId, type = "Discount", amount, reason = "Synthetic QA adjustment" });
        RequireFinanceStatus(createAdjustment, HttpStatusCode.OK, scenario + " adjustment create");
        using var adjustmentJson = JsonDocument.Parse(await createAdjustment.Content.ReadAsStringAsync());
        var adjustmentId = adjustmentJson.RootElement.GetProperty("id").GetGuid();
        var pending = await ReadAdjustmentSnapshotAsync(factory, academyId, studentId, invoiceId, scenario + "-pending");
        if (pending.Adjusted != 0m || pending.Status != "Issued" || pending.Adjustments.Count != 1 ||
            pending.Adjustments[0].Id != adjustmentId || pending.Adjustments[0].Amount != amount ||
            pending.Adjustments[0].Status != "PendingApproval" || pending.Adjustments[0].AppliedAtUtc is not null ||
            pending.Adjustments[0].ApprovedAtUtc is not null)
            throw new InvalidOperationException("Pending adjustment was applied prematurely or did not persist.");
        using var approve = await client.PatchAsJsonAsync($"{adjustmentPath}/{adjustmentId}/approval",
            new { approve = true, notes = "QA-APPROVED" });
        RequireFinanceStatus(approve, HttpStatusCode.OK, scenario + " adjustment approval");
        var approved = await ReadAdjustmentSnapshotAsync(factory, academyId, studentId, invoiceId, scenario + "-approved");
        if (approved.Adjusted != amount || approved.Payments.Count != 0 || approved.Adjustments.Count != 1 ||
            approved.Adjustments[0].Status != "Approved" || approved.Adjustments[0].Amount != amount ||
            approved.Adjustments[0].ApprovedAtUtc is null || approved.Adjustments[0].AppliedAtUtc is null ||
            approved.Adjustments[0].ApprovalNotes != "QA-APPROVED")
            throw new InvalidOperationException("Approved adjustment did not persist exactly once.");
        using var approveAgain = await client.PatchAsJsonAsync($"{adjustmentPath}/{adjustmentId}/approval",
            new { approve = true, notes = "MUST-NOT-APPLY" });
        RequireFinanceStatus(approveAgain, HttpStatusCode.Conflict, scenario + " repeated approval");
        RequireUnchangedAdjustment(approved,
            await ReadAdjustmentSnapshotAsync(factory, academyId, studentId, invoiceId, scenario + "-reapproval"));
        return invoiceId;
    }

    private static async Task<AdjustmentSnapshot> CollectAdjustmentBaselineAsync(QaApiFactory factory,
        HttpClient client, Guid academyId, Guid studentId, Guid invoiceId, string invoicesPath, string paymentsPath, string scenario)
    {
        using var payment = await client.PostAsJsonAsync(paymentsPath, new { invoiceId, amount = 600m });
        RequireFinanceStatus(payment, HttpStatusCode.Created, scenario + " baseline collection 600");
        var snapshot = await ReadAdjustmentSnapshotAsync(factory, academyId, studentId, invoiceId, scenario + "-collected-600");
        var view = await ReadAdjustedInvoiceViewAsync(client, invoicesPath, invoiceId);
        if (snapshot.Adjusted != 200m || snapshot.Status != "PartiallyPaid" || snapshot.Payments.Count != 1 ||
            snapshot.Payments[0].Amount != 600m || snapshot.Payments[0].Status != "Completed" ||
            view != (600m, 200m, "PartiallyPaid"))
            throw new InvalidOperationException("Adjusted balance baseline arithmetic/SQL control failed.");
        return snapshot;
    }

    private static async Task<(decimal Paid, decimal Balance, string Status)> ReadAdjustedInvoiceViewAsync(
        HttpClient client, string invoicesPath, Guid invoiceId)
    {
        using var response = await client.GetAsync(invoicesPath);
        RequireFinanceStatus(response, HttpStatusCode.OK, "adjusted admin invoice view");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var row = json.RootElement.EnumerateArray().Single(x => x.GetProperty("id").GetGuid() == invoiceId);
        return (row.GetProperty("paidAmount").GetDecimal(), row.GetProperty("balance").GetDecimal(),
            row.GetProperty("status").GetString() ?? throw new InvalidOperationException("Invoice view omitted status."));
    }

    private static void RequireCollectionObservation(HttpResponseMessage response, string operation)
    {
        if (response.StatusCode is not (HttpStatusCode.Created or HttpStatusCode.BadRequest))
            throw new InvalidOperationException($"Unexpected {operation} response {(int)response.StatusCode}.");
    }

    private static void ValidateCollectionPersistence(HttpResponseMessage response, AdjustmentSnapshot before,
        AdjustmentSnapshot after, decimal amount)
    {
        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            RequireUnchangedAdjustment(before, after);
            return;
        }
        var priorIds = before.Payments.Select(x => x.Id).ToHashSet();
        var added = after.Payments.Where(x => !priorIds.Contains(x.Id)).ToList();
        if (after.Payments.Count != before.Payments.Count + 1 || added.Count != 1 ||
            added[0].Amount != amount || added[0].Status != "Completed" ||
            !before.Payments.All(x => after.Payments.Contains(x)) || before.Adjusted != after.Adjusted ||
            JsonSerializer.Serialize(before.Adjustments) != JsonSerializer.Serialize(after.Adjustments))
            throw new InvalidOperationException("Accepted collection does not match the fresh persisted ledger.");
    }

    private static async Task<AdjustmentSnapshot> ReadAdjustmentSnapshotAsync(QaApiFactory factory,
        Guid academyId, Guid studentId, Guid invoiceId, string stage)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
        var invoice = await db.Invoices.AsNoTracking().SingleAsync(x => x.Id == invoiceId);
        if (invoice.AcademyId != academyId || invoice.StudentId != studentId || invoice.TotalAmount != 1000m)
            throw new InvalidOperationException("Adjusted-invoice ownership/total mismatch.");
        var payments = await db.Payments.AsNoTracking().Where(x => x.InvoiceId == invoiceId).OrderBy(x => x.Id)
            .Select(x => new FinancePayment(x.Id, x.AcademyId, x.Amount, x.Status, x.ReconciliationReference, x.ReconciledAtUtc)).ToListAsync();
        var adjustments = await db.FinanceAdjustments.AsNoTracking().Where(x => x.InvoiceId == invoiceId).OrderBy(x => x.Id)
            .Select(x => new AdjustmentLedger(x.Id, x.AcademyId, x.Amount, x.Status, x.ApprovedAtUtc, x.AppliedAtUtc, x.ApprovalNotes)).ToListAsync();
        if (payments.Any(x => x.AcademyId != academyId) || adjustments.Any(x => x.AcademyId != academyId))
            throw new InvalidOperationException("Adjustment/payment tenant mismatch.");
        var snapshot = new AdjustmentSnapshot(invoiceId, invoice.TotalAmount, invoice.AdjustedAmount, invoice.Status, payments, adjustments);
        Console.WriteLine($"ADJUSTMENT SQL {stage}: {JsonSerializer.Serialize(snapshot)}");
        return snapshot;
    }

    private static void RequireUnchangedAdjustment(AdjustmentSnapshot before, AdjustmentSnapshot after)
    {
        if (JsonSerializer.Serialize(before) != JsonSerializer.Serialize(after))
            throw new InvalidOperationException("Rejected adjustment/collection changed its persisted snapshot.");
    }

    private sealed record AdjustmentLedger(Guid Id, Guid AcademyId, decimal Amount, string Status,
        DateTime? ApprovedAtUtc, DateTime? AppliedAtUtc, string? ApprovalNotes);
    private sealed record AdjustmentSnapshot(Guid InvoiceId, decimal Total, decimal Adjusted, string Status,
        List<FinancePayment> Payments, List<AdjustmentLedger> Adjustments);
}
