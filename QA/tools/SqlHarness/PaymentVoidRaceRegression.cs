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
    // Two distinct collected rows on one paid invoice are voided simultaneously.
    // The final invoice must agree with the persisted ledger, not with either
    // request's intermediate view of the other payment.
    private static async Task VerifyPaymentVoidRaceAsync(QaApiFactory factory, HttpClient client)
    {
        Guid academyId, studentId;
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
        var guarded = 0;
        for (var attempt = 1; attempt <= 5; attempt++)
        {
            var adjustment = attempt % 2 == 0 ? 200m : 0m;
            var finalPayment = 400m - adjustment;
            var fixture = await CreateTransitionFixtureAsync(factory, client, academyId, studentId, 600m, adjustment,
                "void-race-" + attempt);
            using var second = await client.PostAsJsonAsync(fixture.PaymentsPath,
                new { invoiceId = fixture.InvoiceId, amount = finalPayment });
            RequireFinanceStatus(second, HttpStatusCode.Created, "void-race second payment");
            using var secondJson = JsonDocument.Parse(await second.Content.ReadAsStringAsync());
            var secondId = secondJson.RootElement.GetProperty("id").GetGuid();
            using var reconcile = await client.PatchAsJsonAsync($"{fixture.PaymentsPath}/{fixture.PaymentId}/reconcile",
                new { reference = "QA-VOID-RACE" });
            RequireFinanceStatus(reconcile, HttpStatusCode.OK, "void-race reconcile");
            var before = await ReadAdjustmentSnapshotAsync(factory, academyId, studentId, fixture.InvoiceId,
                "void-race-before-" + attempt);
            if (before.Status != "Paid" || before.Payments.Count != 2 ||
                before.Payments.Single(x => x.Id == fixture.PaymentId).Status != "Reconciled" ||
                before.Payments.Single(x => x.Id == secondId).Status != "Completed")
                throw new InvalidOperationException("Concurrent void fixture was not fully collected.");

            using var left = factory.CreateClient(new() { AllowAutoRedirect = false });
            using var right = factory.CreateClient(new() { AllowAutoRedirect = false });
            left.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            right.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var responses = await Task.WhenAll(
                left.PatchAsJsonAsync($"{fixture.PaymentsPath}/{fixture.PaymentId}/status", new { status = "Voided" }),
                right.PatchAsJsonAsync($"{fixture.PaymentsPath}/{secondId}/status", new { status = "Voided" }));
            var codes = responses.Select(response => (int)response.StatusCode).Order().ToArray();
            foreach (var response in responses) response.Dispose();
            if (codes.Contains(429)) throw new InvalidOperationException("Rate limit prevented the void race.");
            var after = await ReadAdjustmentSnapshotAsync(factory, academyId, studentId, fixture.InvoiceId,
                "void-race-after-" + attempt);
            var view = await ReadAdjustedInvoiceViewAsync(client, fixture.InvoicesPath, fixture.InvoiceId);
            var listed = await CollectionsIncludesAsync(client, fixture);
            var preservedEvidence = after.Payments.Single(x => x.Id == fixture.PaymentId) is var original &&
                original.ReconciliationReference == "QA-VOID-RACE" && original.ReconciledAtUtc is not null;
            var consistent = codes.SequenceEqual(new[] { 200, 200 }) && after.Payments.Count == 2 &&
                after.Payments.All(x => x.Status == "Voided") && preservedEvidence &&
                after.Adjusted == adjustment && after.Adjustments.Count == (adjustment > 0m ? 1 : 0) &&
                after.Adjustments.All(x => x.Status == "Approved" && x.Amount == adjustment) &&
                after.Status == "Overdue" && view == (0m, 1000m - adjustment, "Overdue") && listed;
            if (consistent) guarded++;
            Console.WriteLine($"TRANSITION RACE attempt={attempt} adjusted={adjustment} statuses={string.Join(',', codes)} " +
                $"voided={after.Payments.Count(x => x.Status == "Voided")}/2 invoice={after.Status} " +
                $"viewPaid={view.Paid} balance={view.Balance} collections={listed} guarded={consistent}");
        }
        if (guarded != 5) throw new InvalidOperationException($"Concurrent void ledger/status mismatch: {guarded}/5 guarded.");
        Console.WriteLine("TRANSITION RACE PASS: 5/5 concurrent two-row void pairs, including approved adjustments, left zero collected, Overdue invoice and preserved reconciliation evidence.");
    }
}
