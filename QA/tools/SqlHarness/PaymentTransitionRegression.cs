using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

internal static partial class SqlHarnessEntryPoint
{
    private static async Task VerifyVoidEvidenceRegressionAsync(QaApiFactory factory, HttpClient client,
        Guid academyId, Guid studentId)
    {
        foreach (var adjusted in new[] { 0m, 200m })
        {
            var fixture = await CreateTransitionFixtureAsync(factory, client, academyId, studentId,
                600m, adjusted, "mixed-evidence-" + adjusted);
            using var extra = await client.PostAsJsonAsync(fixture.PaymentsPath,
                new { invoiceId = fixture.InvoiceId, amount = 400m - adjusted });
            RequireFinanceStatus(extra, HttpStatusCode.Created, "mixed evidence exact settlement");
            using var extraJson = JsonDocument.Parse(await extra.Content.ReadAsStringAsync());
            var otherId = extraJson.RootElement.GetProperty("id").GetGuid();
            foreach (var id in new[] { fixture.PaymentId, otherId })
            {
                using var reconcile = await client.PatchAsJsonAsync($"{fixture.PaymentsPath}/{id}/reconcile",
                    new { reference = "QA-KEEP-" + id.ToString("N") });
                RequireFinanceStatus(reconcile, HttpStatusCode.OK, "mixed evidence dedicated reconciliation");
            }
            var before = await ReadAdjustmentSnapshotAsync(factory, academyId, studentId, fixture.InvoiceId, "mixed-reconciled-before-void");
            if (before.Status != "Paid" || before.Payments.Count != 2 || before.Payments.Any(x => x.Status != "Reconciled" ||
                string.IsNullOrWhiteSpace(x.ReconciliationReference) || x.ReconciledAtUtc is null))
                throw new InvalidOperationException("Mixed evidence fixture not reconciled/Paid.");
            using var response = await client.PatchAsJsonAsync($"{fixture.PaymentsPath}/{fixture.PaymentId}/status", new { status = "Voided" });
            RequireFinanceStatus(response, HttpStatusCode.OK, "void reconciled payment");
            var after = await ReadAdjustmentSnapshotAsync(factory, academyId, studentId, fixture.InvoiceId, "mixed-evidence-after-void");
            var expectedTarget = before.Payments.Single(x => x.Id == fixture.PaymentId) with { Status = "Voided" };
            if (after.Payments.Count != 2 || after.Payments.Single(x => x.Id == fixture.PaymentId) != expectedTarget ||
                after.Payments.Single(x => x.Id == otherId) != before.Payments.Single(x => x.Id == otherId) ||
                JsonSerializer.Serialize(after.Adjustments) != JsonSerializer.Serialize(before.Adjustments) ||
                after.Status != "PartiallyPaid" || after.Adjusted != adjusted ||
                await ReadAdjustedInvoiceViewAsync(client, fixture.InvoicesPath, fixture.InvoiceId) != (400m - adjusted, 600m, "PartiallyPaid") ||
                !await CollectionsIncludesAsync(client, fixture))
                throw new InvalidOperationException("Voiding lost evidence/other collection or changed adjustment/status/balance.");
            using var repeat = await client.PatchAsJsonAsync($"{fixture.PaymentsPath}/{fixture.PaymentId}/status", new { status = "Voided" });
            RequireFinanceStatus(repeat, HttpStatusCode.OK, "repeat reconciled void");
            RequireUnchangedAdjustment(after, await ReadAdjustmentSnapshotAsync(factory, academyId, studentId, fixture.InvoiceId, "mixed-repeat-void"));
            using var shortcut = await client.PatchAsJsonAsync($"{fixture.PaymentsPath}/{fixture.PaymentId}/status", new { status = "Reconciled" });
            RequireFinanceStatus(shortcut, HttpStatusCode.BadRequest, "generic reconcile even with historical evidence");
            RequireUnchangedAdjustment(after, await ReadAdjustmentSnapshotAsync(factory, academyId, studentId, fixture.InvoiceId, "mixed-generic-reconcile-rejected"));
            Console.WriteLine($"TRANSITION MIXED adjusted={adjusted} PASS: original evidence retained on Voided payment; other Reconciled amount={400m-adjusted}/balance=600/PartiallyPaid/collections eligible; repeated void and rejected generic reconciliation financially unchanged.");
        }

        var future = await CreateTransitionFixtureAsync(factory, client, academyId, studentId, 600m, 0m, "future-due-void");
        var futureDue = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(7);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var invoice = await db.Invoices.SingleAsync(x => x.Id == future.InvoiceId && x.AcademyId == academyId);
            invoice.DueDate = futureDue; // Isolated fixture variation, not an API capability claim.
            await db.SaveChangesAsync();
        }
        using var futureVoid = await client.PatchAsJsonAsync($"{future.PaymentsPath}/{future.PaymentId}/status", new { status = "Voided" });
        RequireFinanceStatus(futureVoid, HttpStatusCode.OK, "future due void");
        var futureState = await ReadAdjustmentSnapshotAsync(factory, academyId, studentId, future.InvoiceId, "future-issued-after-void");
        RequireTransitionPayment(futureState, future, "Voided", null, false);
        if (futureState.Status != "Issued" || await ReadAdjustedInvoiceViewAsync(client, future.InvoicesPath, future.InvoiceId) != (0m, 1000m, "Issued"))
            throw new InvalidOperationException("Future due invoice was incorrectly reopened overdue.");
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            if (await db.Invoices.AsNoTracking().Where(x => x.Id == future.InvoiceId).Select(x => x.DueDate).SingleAsync() != futureDue)
                throw new InvalidOperationException("Void changed future due date.");
        }

        var cancelled = await CreateTransitionFixtureAsync(factory, client, academyId, studentId, 600m, 0m, "cancelled-void");
        using var cancel = await client.PatchAsJsonAsync($"{cancelled.InvoicesPath}/{cancelled.InvoiceId}/status", new { status = "cancelled" });
        RequireFinanceStatus(cancel, HttpStatusCode.OK, "cancelled fixture state");
        using var cancelVoid = await client.PatchAsJsonAsync($"{cancelled.PaymentsPath}/{cancelled.PaymentId}/status", new { status = "Voided" });
        RequireFinanceStatus(cancelVoid, HttpStatusCode.OK, "void with cancelled invoice");
        var cancelState = await ReadAdjustmentSnapshotAsync(factory, academyId, studentId, cancelled.InvoiceId, "cancelled-preserved-after-void");
        RequireTransitionPayment(cancelState, cancelled, "Voided", null, false);
        if (cancelState.Status != "cancelled" || await ReadAdjustedInvoiceViewAsync(client, cancelled.InvoicesPath, cancelled.InvoiceId) != (0m, 1000m, "cancelled"))
            throw new InvalidOperationException("Void uncancelled the invoice.");
        Console.WriteLine("TRANSITION REGRESSION PASS: original full/partial/adjusted void and required-evidence cases; mixed Reconciled collections/evidence preservation/repeat/rejected generic path; future due Issued; cancelled state preserved case-insensitively. Voided restoration policy remains pending, not certified.");
    }
}
