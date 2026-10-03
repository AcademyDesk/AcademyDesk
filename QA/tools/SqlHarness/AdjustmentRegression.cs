using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

internal static partial class SqlHarnessEntryPoint
{
    private static async Task VerifyAdjustedPaymentRegressionAsync(QaApiFactory factory, HttpClient client,
        Guid academyId, Guid studentId, string invoicesPath, string paymentsPath, string adjustmentPath)
    {
        var mixedId = await CreateApprovedAdjustmentFixtureAsync(factory, client, academyId, studentId,
            invoicesPath, adjustmentPath, 200m, "adjusted-mixed");
        var baseline = await CollectAdjustmentBaselineAsync(factory, client, academyId, studentId,
            mixedId, invoicesPath, paymentsPath, "adjusted-mixed");
        using var reconcile = await client.PatchAsJsonAsync($"{paymentsPath}/{baseline.Payments.Single().Id}/reconcile",
            new { reference = "QA-ADJUSTED-RECON" });
        RequireFinanceStatus(reconcile, HttpStatusCode.OK, "adjusted payment reconcile");
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            // Fixture only: not a void/restoration policy transition.
            db.Payments.Add(new Payment { AcademyId = academyId, InvoiceId = mixedId, Amount = 75m, Status = "Voided" });
            await db.SaveChangesAsync();
        }
        var mixed = await ReadAdjustmentSnapshotAsync(factory, academyId, studentId, mixedId, "mixed-before-cent");
        if (mixed.Payments.Count != 2 || mixed.Payments.Single(x => x.Status == "Reconciled").Amount != 600m ||
            await ReadAdjustedInvoiceViewAsync(client, invoicesPath, mixedId) != (600m, 200m, "PartiallyPaid"))
            throw new InvalidOperationException("Adjusted reconciled/Voided fixture differs.");
        using var tooMuch = await client.PostAsJsonAsync(paymentsPath, new { invoiceId = mixedId, amount = 200.01m });
        RequireFinanceStatus(tooMuch, HttpStatusCode.BadRequest, "cent above adjusted balance");
        RequireUnchangedAdjustment(mixed, await ReadAdjustmentSnapshotAsync(factory, academyId, studentId, mixedId, "mixed-rejected-cent"));
        using var partial = await client.PostAsJsonAsync(paymentsPath, new { invoiceId = mixedId, amount = 199.99m });
        RequireFinanceStatus(partial, HttpStatusCode.Created, "one cent below adjusted settlement");
        var partialState = await ReadAdjustmentSnapshotAsync(factory, academyId, studentId, mixedId, "mixed-partial");
        ValidateCollectionPersistence(partial, mixed, partialState, 199.99m);
        if (partialState.Status != "PartiallyPaid" || await ReadAdjustedInvoiceViewAsync(client, invoicesPath, mixedId) != (799.99m, 0.01m, "PartiallyPaid"))
            throw new InvalidOperationException("Adjusted partial decimal boundary differs.");
        using var lastCent = await client.PostAsJsonAsync(paymentsPath, new { invoiceId = mixedId, amount = 0.01m });
        RequireFinanceStatus(lastCent, HttpStatusCode.Created, "exact adjusted mixed settlement");
        var settled = await ReadAdjustmentSnapshotAsync(factory, academyId, studentId, mixedId, "mixed-settled");
        ValidateCollectionPersistence(lastCent, partialState, settled, 0.01m);
        if (settled.Status != "Paid" || settled.Payments.Count != 4 ||
            settled.Payments.Where(x => x.Status != "Voided").Sum(x => x.Amount) != 800m ||
            await ReadAdjustedInvoiceViewAsync(client, invoicesPath, mixedId) != (800m, 0m, "Paid"))
            throw new InvalidOperationException("Adjusted mixed settlement SQL/admin state differs.");
        using var afterSettlement = await client.PostAsJsonAsync(paymentsPath, new { invoiceId = mixedId, amount = 0.01m });
        RequireFinanceStatus(afterSettlement, HttpStatusCode.BadRequest, "collection after adjusted settlement");
        RequireUnchangedAdjustment(settled, await ReadAdjustmentSnapshotAsync(factory, academyId, studentId, mixedId, "settled-rejected-cent"));

        await Task.Delay(1100);
        using var create = await client.PostAsJsonAsync(invoicesPath, new { studentId, amount = 1000m });
        RequireFinanceStatus(create, HttpStatusCode.Created, "unapplied adjustment invoice");
        using var invoiceJson = JsonDocument.Parse(await create.Content.ReadAsStringAsync());
        var pendingId = invoiceJson.RootElement.GetProperty("id").GetGuid();
        using var proposal = await client.PostAsJsonAsync(adjustmentPath,
            new { invoiceId = pendingId, type = "Discount", amount = 200m, reason = "QA unapplied control" });
        RequireFinanceStatus(proposal, HttpStatusCode.OK, "pending adjustment control");
        using var proposalJson = JsonDocument.Parse(await proposal.Content.ReadAsStringAsync());
        var adjustmentId = proposalJson.RootElement.GetProperty("id").GetGuid();
        using var initialPayment = await client.PostAsJsonAsync(paymentsPath, new { invoiceId = pendingId, amount = 600m });
        RequireFinanceStatus(initialPayment, HttpStatusCode.Created, "collect while adjustment pending");
        var pending = await ReadAdjustmentSnapshotAsync(factory, academyId, studentId, pendingId, "unapplied-pending");
        if (pending.Adjusted != 0m || pending.Adjustments.Single().Status != "PendingApproval" ||
            await ReadAdjustedInvoiceViewAsync(client, invoicesPath, pendingId) != (600m, 400m, "PartiallyPaid"))
            throw new InvalidOperationException("Pending proposal changed collectible balance.");
        using var decline = await client.PatchAsJsonAsync($"{adjustmentPath}/{adjustmentId}/approval",
            new { approve = false, notes = "QA rejection control" });
        RequireFinanceStatus(decline, HttpStatusCode.OK, "reject adjustment proposal");
        var rejected = await ReadAdjustmentSnapshotAsync(factory, academyId, studentId, pendingId, "unapplied-rejected");
        if (rejected.Adjusted != 0m || rejected.Status != "PartiallyPaid" ||
            rejected.Adjustments.Single().Status != "Rejected" || rejected.Adjustments.Single().AppliedAtUtc is not null ||
            JsonSerializer.Serialize(pending.Payments) != JsonSerializer.Serialize(rejected.Payments))
            throw new InvalidOperationException("Rejected proposal altered applied amount/payment ledger.");
        using var finish = await client.PostAsJsonAsync(paymentsPath, new { invoiceId = pendingId, amount = 400m });
        RequireFinanceStatus(finish, HttpStatusCode.Created, "unapplied gross settlement");
        var final = await ReadAdjustmentSnapshotAsync(factory, academyId, studentId, pendingId, "unapplied-settled");
        ValidateCollectionPersistence(finish, rejected, final, 400m);
        if (final.Status != "Paid" || await ReadAdjustedInvoiceViewAsync(client, invoicesPath, pendingId) != (1000m, 0m, "Paid"))
            throw new InvalidOperationException("Rejected proposal reduced collection or changed settlement status.");
        Console.WriteLine("ADJUSTMENT REGRESSION PASS: original three adjustment cases; mixed Reconciled/Completed/Voided; cent-over rejection/no financial SQL change; partial 199.99 and exact final 0.01/Paid; post-settlement rejection; PendingApproval/Rejected proposal remains unapplied and gross settlement succeeds.");
    }
}
