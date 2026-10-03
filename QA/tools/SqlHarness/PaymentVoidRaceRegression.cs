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
        await VerifyCreateVoidRaceAsync(factory, client, token, academyId, studentId);
    }

    private static async Task VerifyCreateVoidRaceAsync(QaApiFactory factory, HttpClient client, string token,
        Guid academyId, Guid studentId)
    {
        var guarded = 0;
        for (var attempt = 1; attempt <= 5; attempt++)
        {
            var adjustment = attempt % 2 == 0 ? 200m : 0m;
            var newAmount = 400m - adjustment;
            var fixture = await CreateTransitionFixtureAsync(factory, client, academyId, studentId, 600m, adjustment,
                "create-void-race-" + attempt);
            using var reconcile = await client.PatchAsJsonAsync($"{fixture.PaymentsPath}/{fixture.PaymentId}/reconcile",
                new { reference = "QA-CREATE-VOID-RACE" });
            RequireFinanceStatus(reconcile, HttpStatusCode.OK, "create-void race reconcile");

            using var voidClient = factory.CreateClient(new() { AllowAutoRedirect = false });
            using var createClient = factory.CreateClient(new() { AllowAutoRedirect = false });
            voidClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            createClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var voidTask = voidClient.PatchAsJsonAsync($"{fixture.PaymentsPath}/{fixture.PaymentId}/status",
                new { status = "Voided" });
            var createTask = createClient.PostAsJsonAsync(fixture.PaymentsPath,
                new { invoiceId = fixture.InvoiceId, amount = newAmount });
            await Task.WhenAll(voidTask, createTask);
            using var voidResponse = await voidTask;
            using var createResponse = await createTask;
            var codes = new[] { (int)voidResponse.StatusCode, (int)createResponse.StatusCode };
            if (codes.Contains(429)) throw new InvalidOperationException("Rate limit prevented the create/void race.");
            var snapshot = await ReadAdjustmentSnapshotAsync(factory, academyId, studentId, fixture.InvoiceId,
                "create-void-race-after-" + attempt);
            var view = await ReadAdjustedInvoiceViewAsync(client, fixture.InvoicesPath, fixture.InvoiceId);
            var listed = await CollectionsIncludesAsync(client, fixture);
            var oldPayment = snapshot.Payments.SingleOrDefault(x => x.Id == fixture.PaymentId);
            var surviving = snapshot.Payments.SingleOrDefault(x => x.Id != fixture.PaymentId);
            var consistent = codes.SequenceEqual(new[] { 200, 201 }) && snapshot.Payments.Count == 2 &&
                oldPayment is { Status: "Voided", ReconciliationReference: "QA-CREATE-VOID-RACE" } &&
                oldPayment.ReconciledAtUtc is not null && surviving is { Status: "Completed" } &&
                surviving.Amount == newAmount && snapshot.Adjusted == adjustment &&
                snapshot.Status == "PartiallyPaid" && view == (newAmount, 600m, "PartiallyPaid") && listed;
            if (consistent) guarded++;
            Console.WriteLine($"TRANSITION CREATE/VOID attempt={attempt} adjusted={adjustment} " +
                $"statuses={string.Join(',', codes)} collected={snapshot.Payments.Where(x => x.Status is "Completed" or "Reconciled").Sum(x => x.Amount)} " +
                $"invoice={snapshot.Status} viewBalance={view.Balance} guarded={consistent}");
        }
        if (guarded != 5) throw new InvalidOperationException($"Concurrent create/void ledger/status mismatch: {guarded}/5 guarded.");
        Console.WriteLine("TRANSITION CREATE/VOID PASS: 5/5 gross/adjusted pairs retained only the new collection and a PartiallyPaid invoice.");
    }

    private static async Task VerifyReconcileVoidRaceAsync(QaApiFactory factory, HttpClient client)
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
            var fixture = await CreateTransitionFixtureAsync(factory, client, academyId, studentId,
                1000m - adjustment, adjustment, "reconcile-void-race-" + attempt);
            using var reconcileClient = factory.CreateClient(new() { AllowAutoRedirect = false });
            using var voidClient = factory.CreateClient(new() { AllowAutoRedirect = false });
            reconcileClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            voidClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var responses = await Task.WhenAll(
                reconcileClient.PatchAsJsonAsync($"{fixture.PaymentsPath}/{fixture.PaymentId}/reconcile",
                    new { reference = "QA-RECONCILE-VOID-RACE" }),
                voidClient.PatchAsJsonAsync($"{fixture.PaymentsPath}/{fixture.PaymentId}/status",
                    new { status = "Voided" }));
            var codes = responses.Select(response => (int)response.StatusCode).ToArray();
            foreach (var response in responses) response.Dispose();
            if (codes.Contains(429)) throw new InvalidOperationException("Rate limit prevented the reconcile/void race.");
            var after = await ReadAdjustmentSnapshotAsync(factory, academyId, studentId, fixture.InvoiceId,
                "reconcile-void-race-after-" + attempt);
            var view = await ReadAdjustedInvoiceViewAsync(client, fixture.InvoicesPath, fixture.InvoiceId);
            var listed = await CollectionsIncludesAsync(client, fixture);
            var row = after.Payments.Single();
            var collected = row.Status == "Reconciled";
            var consistent = codes.SequenceEqual(new[] { 200, 200 }) && after.Payments.Count == 1 &&
                row.Id == fixture.PaymentId && row.Amount == 1000m - adjustment &&
                row.ReconciliationReference == "QA-RECONCILE-VOID-RACE" && row.ReconciledAtUtc is not null &&
                after.Adjusted == adjustment && after.Adjustments.Count == (adjustment > 0m ? 1 : 0) &&
                (collected && after.Status == "Paid" && view == (1000m - adjustment, 0m, "Paid") && !listed ||
                 !collected && row.Status == "Voided" && after.Status == "Overdue" &&
                 view == (0m, 1000m - adjustment, "Overdue") && listed);
            if (consistent) guarded++;
            Console.WriteLine($"TRANSITION RECONCILE/VOID attempt={attempt} adjusted={adjustment} " +
                $"statuses={string.Join(',', codes)} payment={row.Status} invoice={after.Status} " +
                $"viewPaid={view.Paid} balance={view.Balance} guarded={consistent}");
        }
        if (guarded != 5) throw new InvalidOperationException($"Concurrent reconcile/void ledger/status mismatch: {guarded}/5 guarded.");

        // Existing explicit restoration remains available, but it must restore
        // the invoice atomically and cannot revive an already-replaced amount.
        var restore = await CreateTransitionFixtureAsync(factory, client, academyId, studentId, 1000m, 0m,
            "reconcile-after-void");
        using (var voidResponse = await client.PatchAsJsonAsync($"{restore.PaymentsPath}/{restore.PaymentId}/status",
            new { status = "Voided" }))
            RequireFinanceStatus(voidResponse, HttpStatusCode.OK, "void before explicit reconciliation");
        using (var reconcileResponse = await client.PatchAsJsonAsync($"{restore.PaymentsPath}/{restore.PaymentId}/reconcile",
            new { reference = "QA-EXPLICIT-RESTORE" }))
            RequireFinanceStatus(reconcileResponse, HttpStatusCode.OK, "explicit reconciliation after void");
        var restored = await ReadAdjustmentSnapshotAsync(factory, academyId, studentId, restore.InvoiceId,
            "reconcile-after-void-restored");
        if (restored.Status != "Paid" || restored.Payments.Single().Status != "Reconciled" ||
            await ReadAdjustedInvoiceViewAsync(client, restore.InvoicesPath, restore.InvoiceId) != (1000m, 0m, "Paid"))
            throw new InvalidOperationException("Explicit reconciliation restored a payment without restoring the invoice.");

        var replacement = await CreateTransitionFixtureAsync(factory, client, academyId, studentId, 1000m, 0m,
            "reconcile-replaced-void");
        using (var voidResponse = await client.PatchAsJsonAsync($"{replacement.PaymentsPath}/{replacement.PaymentId}/status",
            new { status = "Voided" }))
            RequireFinanceStatus(voidResponse, HttpStatusCode.OK, "void before replacement payment");
        using (var newPayment = await client.PostAsJsonAsync(replacement.PaymentsPath,
            new { invoiceId = replacement.InvoiceId, amount = 1000m }))
            RequireFinanceStatus(newPayment, HttpStatusCode.Created, "replacement payment");
        using (var rejected = await client.PatchAsJsonAsync($"{replacement.PaymentsPath}/{replacement.PaymentId}/reconcile",
            new { reference = "QA-OVERCOLLECTION-REJECT" }))
        {
            RequireFinanceStatus(rejected, HttpStatusCode.BadRequest, "overcollecting reconciliation");
            using var rejection = JsonDocument.Parse(await rejected.Content.ReadAsStringAsync());
            if (rejection.RootElement.GetProperty("message").GetString() != "Payment exceeds the invoice balance.")
                throw new InvalidOperationException("Overcollecting reconciliation returned the wrong balance message.");
        }
        var unchanged = await ReadAdjustmentSnapshotAsync(factory, academyId, studentId, replacement.InvoiceId,
            "reconcile-replaced-unchanged");
        if (unchanged.Status != "Paid" || unchanged.Payments.Count != 2 ||
            unchanged.Payments.Single(x => x.Id == replacement.PaymentId) is not { Status: "Voided", ReconciliationReference: null, ReconciledAtUtc: null } ||
            unchanged.Payments.Where(x => x.Status is "Completed" or "Reconciled").Sum(x => x.Amount) != 1000m)
            throw new InvalidOperationException("Rejected restoration changed the replacement ledger.");
        Console.WriteLine("TRANSITION RECONCILE/VOID PASS: 5/5 concurrent pairs, explicit restoration updates invoice, replaced amount rejected without writes. Restoration policy remains pending.");
    }
}
