using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

internal static partial class SqlHarnessEntryPoint
{
    private static async Task VerifyPaymentTransitionsAsync(QaApiFactory factory, HttpClient client, bool enforceRegression = false)
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
        var token = await LoginAsync(client, "qa-admin-a@example.invalid", "Synthetic!39Ab");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var full = await CreateTransitionFixtureAsync(factory, client, academyId, studentId, 1000m, 0m, "full");
        var fullFailed = await ObserveVoidAsync(factory, client, full, "full");

        var voidBeforeRestore = await ReadAdjustmentSnapshotAsync(factory, academyId, studentId, full.InvoiceId, "voided-before-restore");
        using var restore = await client.PatchAsJsonAsync($"{full.PaymentsPath}/{full.PaymentId}/reconcile",
            new { reference = "QA-RESTORE-OBSERVATION" });
        if (restore.StatusCode is not (HttpStatusCode.OK or HttpStatusCode.BadRequest or HttpStatusCode.Conflict))
            throw new InvalidOperationException("Unexpected voided-payment reconciliation response.");
        var restored = await ReadAdjustmentSnapshotAsync(factory, academyId, studentId, full.InvoiceId, "voided-after-restore");
        if (restore.StatusCode == HttpStatusCode.OK)
            RequireTransitionPayment(restored, full, "Reconciled", "QA-RESTORE-OBSERVATION", true);
        else RequireUnchangedAdjustment(voidBeforeRestore, restored);
        Console.WriteLine($"TRANSITION RESTORE POLICY-PENDING: Voided->reconcile HTTP={(int)restore.StatusCode}/" +
            $"payment={restored.Payments.Single().Status}/evidence={restored.Payments.Single().ReconciliationReference ?? "none"}/" +
            $"timestamp={restored.Payments.Single().ReconciledAtUtc.HasValue}; no approved restoration policy assumed.");

        var partial = await CreateTransitionFixtureAsync(factory, client, academyId, studentId, 600m, 0m, "partial");
        var partialFailed = await ObserveVoidAsync(factory, client, partial, "partial");
        var adjusted = await CreateTransitionFixtureAsync(factory, client, academyId, studentId, 800m, 200m, "adjusted");
        var adjustedFailed = await ObserveVoidAsync(factory, client, adjusted, "adjusted");

        var direct = await CreateTransitionFixtureAsync(factory, client, academyId, studentId, 600m, 0m, "direct-reconcile");
        var directBefore = await ReadAdjustmentSnapshotAsync(factory, academyId, studentId, direct.InvoiceId, "direct-before");
        using var blank = await client.PatchAsJsonAsync($"{direct.PaymentsPath}/{direct.PaymentId}/reconcile", new { reference = " " });
        RequireFinanceStatus(blank, HttpStatusCode.BadRequest, "blank transition reconciliation reference");
        RequireUnchangedAdjustment(directBefore,
            await ReadAdjustmentSnapshotAsync(factory, academyId, studentId, direct.InvoiceId, "direct-blank-rejected"));
        using var setReconciled = await client.PatchAsJsonAsync($"{direct.PaymentsPath}/{direct.PaymentId}/status", new { status = "Reconciled" });
        if (setReconciled.StatusCode is not (HttpStatusCode.OK or HttpStatusCode.BadRequest or HttpStatusCode.Conflict))
            throw new InvalidOperationException("Unexpected direct Reconciled status response.");
        var directAfter = await ReadAdjustmentSnapshotAsync(factory, academyId, studentId, direct.InvoiceId, "direct-after");
        var missingEvidence = false;
        if (setReconciled.StatusCode == HttpStatusCode.OK)
        {
            var row = directAfter.Payments.Single();
            if (row.Id != direct.PaymentId || row.Amount != 600m || row.Status != "Reconciled")
                throw new InvalidOperationException("Direct transition response did not match the persisted payment.");
            missingEvidence = string.IsNullOrWhiteSpace(row.ReconciliationReference) || row.ReconciledAtUtc is null;
        }
        else RequireUnchangedAdjustment(directBefore, directAfter);
        using var list = await client.GetAsync(direct.PaymentsPath + "?invoiceId=" + direct.InvoiceId);
        RequireFinanceStatus(list, HttpStatusCode.OK, "direct transition payment list");
        using var listJson = JsonDocument.Parse(await list.Content.ReadAsStringAsync());
        var listed = listJson.RootElement.EnumerateArray().Single(x => x.GetProperty("id").GetGuid() == direct.PaymentId);
        if (listed.GetProperty("status").GetString() != directAfter.Payments.Single().Status ||
            listed.GetProperty("reconciliationReference").GetString() != directAfter.Payments.Single().ReconciliationReference ||
            (listed.GetProperty("reconciledAtUtc").ValueKind == JsonValueKind.Null) != (directAfter.Payments.Single().ReconciledAtUtc is null))
            throw new InvalidOperationException("Direct transition API list disagrees with SQL evidence.");
        Console.WriteLine($"TRANSITION DIRECT {(missingEvidence ? "FAIL" : "PASS")}: HTTP={(int)setReconciled.StatusCode}/" +
            $"payment={directAfter.Payments.Single().Status}/reference={directAfter.Payments.Single().ReconciliationReference ?? "none"}/" +
            $"timestamp={directAfter.Payments.Single().ReconciledAtUtc.HasValue}/invoice={direct.InvoiceId}/paymentId={direct.PaymentId}; " +
            "Reconciled requires reference and timestamp; blank-reference control 400/unchanged.");
        using var legitimate = await client.PatchAsJsonAsync($"{direct.PaymentsPath}/{direct.PaymentId}/reconcile",
            new { reference = "QA-VALID-RECONCILE" });
        RequireFinanceStatus(legitimate, HttpStatusCode.OK, "legitimate reconciliation evidence control");
        var legitimatelyReconciled = await ReadAdjustmentSnapshotAsync(factory, academyId, studentId, direct.InvoiceId, "legitimate-reconciled");
        RequireTransitionPayment(legitimatelyReconciled, direct, "Reconciled", "QA-VALID-RECONCILE", true);
        Console.WriteLine("TRANSITION EVIDENCE CONTROL PASS: dedicated reconcile route 200; reference/timestamp persisted on the original payment.");

        Console.WriteLine($"FINANCE-RULE-003 {(fullFailed || partialFailed || adjustedFailed || missingEvidence ? "FAIL" : "PASS")}: " +
            $"full void inconsistent={fullFailed}; partial void inconsistent={partialFailed}; adjusted void inconsistent={adjustedFailed}; " +
            $"direct Reconciled missing evidence={missingEvidence}; voided restoration POLICY-PENDING HTTP={(int)restore.StatusCode}; " +
            "invalid status/blank reference/repeated void controls verified; independent SQL and read APIs used.");
        if (enforceRegression)
        {
            if (fullFailed || partialFailed || adjustedFailed || missingEvidence)
                throw new InvalidOperationException("FINANCE-RULE-003 void/evidence repair regression failed.");
            await VerifyVoidEvidenceRegressionAsync(factory, client, academyId, studentId);
        }
    }

    private static async Task<TransitionFixture> CreateTransitionFixtureAsync(QaApiFactory factory, HttpClient client,
        Guid academyId, Guid studentId, decimal paymentAmount, decimal adjustment, string scenario)
    {
        await Task.Delay(1100); // Avoid unrelated invoice-number collisions across independent fixtures.
        var invoicesPath = $"/api/academies/{academyId}/invoices";
        var paymentsPath = $"/api/academies/{academyId}/payments";
        var due = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-7);
        using var invoiceResponse = await client.PostAsJsonAsync(invoicesPath, new { studentId, amount = 1000m, dueDate = due });
        RequireFinanceStatus(invoiceResponse, HttpStatusCode.Created, scenario + " transition invoice");
        using var invoiceJson = JsonDocument.Parse(await invoiceResponse.Content.ReadAsStringAsync());
        var invoiceId = invoiceJson.RootElement.GetProperty("id").GetGuid();
        var empty = await ReadAdjustmentSnapshotAsync(factory, academyId, studentId, invoiceId, scenario + "-invoice-created");
        if (empty.Payments.Count != 0 || empty.Adjustments.Count != 0 || empty.Status != "Issued" || empty.Adjusted != 0m)
            throw new InvalidOperationException("Fresh transition fixture was not empty.");
        using var payment = await client.PostAsJsonAsync(paymentsPath, new { invoiceId, amount = paymentAmount });
        RequireFinanceStatus(payment, HttpStatusCode.Created, scenario + " transition payment");
        using var paymentJson = JsonDocument.Parse(await payment.Content.ReadAsStringAsync());
        var paymentId = paymentJson.RootElement.GetProperty("id").GetGuid();
        if (adjustment > 0m)
        {
            // Approve after collection so the known BUG-DATA-0002 does not corrupt the Paid baseline.
            var adjustmentPath = $"/api/academies/{academyId}/finance-adjustments";
            using var create = await client.PostAsJsonAsync(adjustmentPath,
                new { invoiceId, type = "Discount", amount = adjustment, reason = "Synthetic transition discount" });
            RequireFinanceStatus(create, HttpStatusCode.OK, "transition adjustment create");
            using var created = JsonDocument.Parse(await create.Content.ReadAsStringAsync());
            var adjustmentId = created.RootElement.GetProperty("id").GetGuid();
            using var approve = await client.PatchAsJsonAsync($"{adjustmentPath}/{adjustmentId}/approval",
                new { approve = true, notes = "QA-TRANSITION-APPROVED" });
            RequireFinanceStatus(approve, HttpStatusCode.OK, "transition adjustment approval");
        }
        var fixture = new TransitionFixture(academyId, studentId, invoiceId, paymentId, paymentAmount, adjustment,
            invoicesPath, paymentsPath, $"/api/academies/{academyId}/finance-governance/collections");
        var snapshot = await ReadAdjustmentSnapshotAsync(factory, academyId, studentId, invoiceId, scenario + "-baseline");
        RequireTransitionPayment(snapshot, fixture, "Completed", null, false);
        var baselineStatus = paymentAmount + adjustment == 1000m ? "Paid" : "PartiallyPaid";
        var view = await ReadAdjustedInvoiceViewAsync(client, invoicesPath, invoiceId);
        var listedForCollections = await CollectionsIncludesAsync(client, fixture);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            if (await db.Invoices.AsNoTracking().Where(x => x.Id == invoiceId).Select(x => x.DueDate).SingleAsync() != due)
                throw new InvalidOperationException("Transition due-date fixture did not persist.");
        }
        if (snapshot.Status != baselineStatus || view != (paymentAmount, 1000m - adjustment - paymentAmount, baselineStatus) ||
            listedForCollections != (baselineStatus != "Paid"))
            throw new InvalidOperationException("Transition baseline arithmetic/status/collections control failed.");
        return fixture;
    }

    private static async Task<bool> ObserveVoidAsync(QaApiFactory factory, HttpClient client, TransitionFixture fixture, string scenario)
    {
        var before = await ReadAdjustmentSnapshotAsync(factory, fixture.AcademyId, fixture.StudentId, fixture.InvoiceId, scenario + "-before-void");
        using var invalid = await client.PatchAsJsonAsync($"{fixture.PaymentsPath}/{fixture.PaymentId}/status", new { status = "Invalid-QA-Status" });
        RequireFinanceStatus(invalid, HttpStatusCode.BadRequest, "invalid payment transition status");
        RequireUnchangedAdjustment(before, await ReadAdjustmentSnapshotAsync(factory, fixture.AcademyId, fixture.StudentId, fixture.InvoiceId, scenario + "-invalid-status"));
        using var response = await client.PatchAsJsonAsync($"{fixture.PaymentsPath}/{fixture.PaymentId}/status", new { status = "Voided" });
        RequireFinanceStatus(response, HttpStatusCode.OK, "payment void");
        var after = await ReadAdjustmentSnapshotAsync(factory, fixture.AcademyId, fixture.StudentId, fixture.InvoiceId, scenario + "-after-void");
        RequireTransitionPayment(after, fixture, "Voided", null, false);
        if (JsonSerializer.Serialize(before.Adjustments) != JsonSerializer.Serialize(after.Adjustments))
            throw new InvalidOperationException("Voiding changed the approved adjustment ledger.");
        var view = await ReadAdjustedInvoiceViewAsync(client, fixture.InvoicesPath, fixture.InvoiceId);
        var collections = await CollectionsIncludesAsync(client, fixture);
        var failed = after.Status is not ("Issued" or "Overdue") || view.Paid != 0m ||
            view.Balance != 1000m - fixture.Adjustment || view.Status != after.Status || !collections;
        using var repeat = await client.PatchAsJsonAsync($"{fixture.PaymentsPath}/{fixture.PaymentId}/status", new { status = "Voided" });
        if (repeat.StatusCode is not (HttpStatusCode.OK or HttpStatusCode.Conflict))
            throw new InvalidOperationException("Unexpected repeated void response.");
        RequireUnchangedAdjustment(after, await ReadAdjustmentSnapshotAsync(factory, fixture.AcademyId, fixture.StudentId, fixture.InvoiceId, scenario + "-repeat-void"));
        Console.WriteLine($"TRANSITION VOID {scenario} {(failed ? "FAIL" : "PASS")}: HTTP=200/payment=Voided/" +
            $"SQL invoice={after.Status}/admin paid={view.Paid}/balance={view.Balance}/collections includes={collections}/" +
            $"repeat HTTP={(int)repeat.StatusCode}/unchanged=True/invoice={fixture.InvoiceId}/paymentId={fixture.PaymentId}; " +
            "expected reopened Issued or Overdue, net-adjusted balance and collections eligibility.");
        return failed;
    }

    private static async Task<bool> CollectionsIncludesAsync(HttpClient client, TransitionFixture fixture)
    {
        using var response = await client.GetAsync(fixture.CollectionsPath);
        RequireFinanceStatus(response, HttpStatusCode.OK, "transition collections read");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.EnumerateArray().Any(x => x.GetProperty("id").GetGuid() == fixture.InvoiceId);
    }

    private static void RequireTransitionPayment(AdjustmentSnapshot snapshot, TransitionFixture fixture,
        string status, string? reference, bool timestamp)
    {
        if (snapshot.Payments.Count != 1 || snapshot.Payments[0].Id != fixture.PaymentId ||
            snapshot.Payments[0].Amount != fixture.PaymentAmount || snapshot.Payments[0].Status != status ||
            snapshot.Payments[0].ReconciliationReference != reference || snapshot.Payments[0].ReconciledAtUtc.HasValue != timestamp ||
            snapshot.Adjusted != fixture.Adjustment || snapshot.Adjustments.Count != (fixture.Adjustment > 0m ? 1 : 0) ||
            snapshot.Adjustments.Any(x => x.Status != "Approved" || x.Amount != fixture.Adjustment || x.AppliedAtUtc is null))
            throw new InvalidOperationException("Transition payment/evidence/adjustment SQL fixture mismatch.");
    }

    private sealed record TransitionFixture(Guid AcademyId, Guid StudentId, Guid InvoiceId, Guid PaymentId,
        decimal PaymentAmount, decimal Adjustment, string InvoicesPath, string PaymentsPath, string CollectionsPath);
}
