using System.Net.Http.Headers;
using System.Net.Http.Json;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

internal static partial class SqlHarnessEntryPoint
{
    // Approve a pending discount while a payment for the pre-approval remainder is posted.
    private static async Task VerifyApprovalRaceAsync(QaApiFactory factory, HttpClient client)
    {
        Guid academyId, studentId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            academyId = await db.Academies.AsNoTracking().Where(x => x.Name == "Synthetic Academy A").Select(x => x.Id).SingleAsync();
            studentId = await db.Students.AsNoTracking().Where(x => x.AcademyId == academyId && x.FirstName == "Isolated-A").Select(x => x.Id).SingleAsync();
        }
        var adminToken = await LoginAsync(client, "qa-admin-a@example.invalid", "Synthetic!39Ab");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var invoicesPath = $"/api/academies/{academyId}/invoices";
        var paymentsPath = $"/api/academies/{academyId}/payments";
        var adjustmentsPath = $"/api/academies/{academyId}/finance-adjustments";
        var protectedAttempts = 0;
        for (var attempt = 1; attempt <= 5; attempt++)
        {
            using var create = await client.PostAsJsonAsync(invoicesPath, new { studentId, amount = 1000m });
            RequireFinanceStatus(create, System.Net.HttpStatusCode.Created, "approval-race invoice");
            using var invoiceJson = System.Text.Json.JsonDocument.Parse(await create.Content.ReadAsStringAsync());
            var invoiceId = invoiceJson.RootElement.GetProperty("id").GetGuid();
            using var first = await client.PostAsJsonAsync(paymentsPath, new { invoiceId, amount = 600m });
            RequireFinanceStatus(first, System.Net.HttpStatusCode.Created, "approval-race payment 600");
            using var firstJson = System.Text.Json.JsonDocument.Parse(await first.Content.ReadAsStringAsync());
            var paymentId = firstJson.RootElement.GetProperty("id").GetGuid();
            using var reconcile = await client.PatchAsJsonAsync($"{paymentsPath}/{paymentId}/reconcile", new { reference = "QA-APPROVAL-RACE" });
            RequireFinanceStatus(reconcile, System.Net.HttpStatusCode.OK, "approval-race reconcile");
            using var proposal = await client.PostAsJsonAsync(adjustmentsPath,
                new { invoiceId, type = "Discount", amount = 200m, reason = "QA approval race" });
            RequireFinanceStatus(proposal, System.Net.HttpStatusCode.OK, "approval-race proposal");
            using var proposalJson = System.Text.Json.JsonDocument.Parse(await proposal.Content.ReadAsStringAsync());
            var adjustmentId = proposalJson.RootElement.GetProperty("id").GetGuid();

            using var left = factory.CreateClient(new() { AllowAutoRedirect = false });
            using var right = factory.CreateClient(new() { AllowAutoRedirect = false });
            left.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
            right.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
            var responses = await Task.WhenAll(
                left.PatchAsJsonAsync($"{adjustmentsPath}/{adjustmentId}/approval", new { approve = true, notes = "QA approval race" }),
                right.PostAsJsonAsync(paymentsPath, new { invoiceId, amount = 400m }));
            var approvalCode = (int)responses[0].StatusCode;
            var paymentCode = (int)responses[1].StatusCode;
            var rejection = approvalCode == 400 ? await responses[0].Content.ReadAsStringAsync() :
                paymentCode == 400 ? await responses[1].Content.ReadAsStringAsync() : "";
            foreach (var response in responses) response.Dispose();
            if (approvalCode == 429 || paymentCode == 429)
                throw new InvalidOperationException("Rate limit prevented the approval race.");
            var snapshot = await ReadAdjustmentSnapshotAsync(factory, academyId, studentId, invoiceId, "approval-race-" + attempt);
            var collected = snapshot.Payments.Where(payment => payment.Status is "Completed" or "Reconciled").Sum(payment => payment.Amount);
            var collectible = snapshot.Total - snapshot.Adjusted;
            var view = await ReadAdjustedInvoiceViewAsync(client, invoicesPath, invoiceId);
            var adjustment = snapshot.Adjustments.Single();
            var approvedFirst = approvalCode == 200 && paymentCode == 400 &&
                rejection.Contains("Payment exceeds the invoice balance.", StringComparison.Ordinal) &&
                snapshot.Adjusted == 200m && adjustment.Status == "Approved" &&
                adjustment.ApprovedAtUtc is not null && adjustment.AppliedAtUtc is not null &&
                snapshot.Payments.Count == 1 && collected == 600m && snapshot.Status == "PartiallyPaid" &&
                view == (600m, 200m, "PartiallyPaid");
            var paymentFirst = approvalCode == 400 && paymentCode == 201 &&
                rejection.Contains("Adjustment exceeds the remaining invoice balance.", StringComparison.Ordinal) &&
                snapshot.Adjusted == 0m && adjustment.Status == "PendingApproval" &&
                adjustment.ApprovedAtUtc is null && adjustment.AppliedAtUtc is null &&
                snapshot.Payments.Count == 2 && collected == 1000m && snapshot.Status == "Paid" &&
                view == (1000m, 0m, "Paid");
            var guarded = collected <= collectible && (approvedFirst || paymentFirst);
            if (guarded) protectedAttempts++;
            Console.WriteLine($"FINANCE RACE approval attempt={attempt} statuses={approvalCode},{paymentCode} collected={collected} adjusted={snapshot.Adjusted} status={snapshot.Status} guarded={guarded}");
        }
        if (protectedAttempts != 5)
            throw new InvalidOperationException($"Approval and collection were not guarded on every attempt: {protectedAttempts}/5.");

        // Force the opposite order too: an accepted adjustment must make the
        // pre-approval 400 collection stale even if it is submitted later.
        using var orderedInvoice = await client.PostAsJsonAsync(invoicesPath, new { studentId, amount = 1000m });
        RequireFinanceStatus(orderedInvoice, System.Net.HttpStatusCode.Created, "approval-first invoice");
        using var orderedInvoiceJson = System.Text.Json.JsonDocument.Parse(await orderedInvoice.Content.ReadAsStringAsync());
        var orderedInvoiceId = orderedInvoiceJson.RootElement.GetProperty("id").GetGuid();
        using var orderedPayment = await client.PostAsJsonAsync(paymentsPath,
            new { invoiceId = orderedInvoiceId, amount = 600m });
        RequireFinanceStatus(orderedPayment, System.Net.HttpStatusCode.Created, "approval-first payment 600");
        using var orderedPaymentJson = System.Text.Json.JsonDocument.Parse(await orderedPayment.Content.ReadAsStringAsync());
        var orderedPaymentId = orderedPaymentJson.RootElement.GetProperty("id").GetGuid();
        using var orderedReconcile = await client.PatchAsJsonAsync($"{paymentsPath}/{orderedPaymentId}/reconcile",
            new { reference = "QA-APPROVAL-FIRST" });
        RequireFinanceStatus(orderedReconcile, System.Net.HttpStatusCode.OK, "approval-first reconcile");
        using var orderedProposal = await client.PostAsJsonAsync(adjustmentsPath,
            new { invoiceId = orderedInvoiceId, type = "Discount", amount = 200m, reason = "QA approval first" });
        RequireFinanceStatus(orderedProposal, System.Net.HttpStatusCode.OK, "approval-first proposal");
        using var orderedProposalJson = System.Text.Json.JsonDocument.Parse(await orderedProposal.Content.ReadAsStringAsync());
        var orderedAdjustmentId = orderedProposalJson.RootElement.GetProperty("id").GetGuid();
        using var orderedApproval = await client.PatchAsJsonAsync($"{adjustmentsPath}/{orderedAdjustmentId}/approval",
            new { approve = true, notes = "QA approval first" });
        RequireFinanceStatus(orderedApproval, System.Net.HttpStatusCode.OK, "approval-first decision");
        using var orderedStalePayment = await client.PostAsJsonAsync(paymentsPath,
            new { invoiceId = orderedInvoiceId, amount = 400m });
        RequireFinanceStatus(orderedStalePayment, System.Net.HttpStatusCode.BadRequest, "approval-first stale payment");
        if (!(await orderedStalePayment.Content.ReadAsStringAsync()).Contains("Payment exceeds the invoice balance.", StringComparison.Ordinal))
            throw new InvalidOperationException("Approval-first rejection did not use the established balance message.");
        var ordered = await ReadAdjustmentSnapshotAsync(factory, academyId, studentId, orderedInvoiceId, "approval-first-final");
        if (ordered.Adjusted != 200m || ordered.Payments.Count != 1 || ordered.Payments.Single().Status != "Reconciled" ||
            ordered.Payments.Single().Amount != 600m || ordered.Adjustments.Single().Status != "Approved" ||
            ordered.Status != "PartiallyPaid" ||
            await ReadAdjustedInvoiceViewAsync(client, invoicesPath, orderedInvoiceId) != (600m, 200m, "PartiallyPaid"))
            throw new InvalidOperationException("Approval-first control changed the ledger or invoice incorrectly.");
        Console.WriteLine($"FINANCE RACE PASS: {protectedAttempts}/5 approval/payment pairs serialized; approval-first stale payment rejected with exact ledger and Admin view.");
    }
}
