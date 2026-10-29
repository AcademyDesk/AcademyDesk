using System.Net.Http.Headers;
using System.Net.Http.Json;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

internal static partial class SqlHarnessEntryPoint
{
    // Two collections of the remaining balance, started together. Sequential rejection is a separate case.
    private static async Task VerifyCollectionRaceAsync(QaApiFactory factory, HttpClient client, bool adjusted = false)
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
        var finalPayment = adjusted ? 200m : 400m;
        var expectedCollected = adjusted ? 800m : 1000m;
        var attempts = 0;
        var protectedAttempts = 0;
        for (var attempt = 1; attempt <= 5; attempt++)
        {
            using var create = await client.PostAsJsonAsync(invoicesPath, new { studentId, amount = 1000m });
            RequireFinanceStatus(create, System.Net.HttpStatusCode.Created, "race invoice");
            using var invoiceJson = System.Text.Json.JsonDocument.Parse(await create.Content.ReadAsStringAsync());
            var invoiceId = invoiceJson.RootElement.GetProperty("id").GetGuid();
            if (adjusted)
            {
                using var proposal = await client.PostAsJsonAsync(adjustmentsPath,
                    new { invoiceId, type = "Discount", amount = 200m, reason = "QA adjusted race" });
                RequireFinanceStatus(proposal, System.Net.HttpStatusCode.OK, "race adjustment proposal");
                using var proposalJson = System.Text.Json.JsonDocument.Parse(await proposal.Content.ReadAsStringAsync());
                var adjustmentId = proposalJson.RootElement.GetProperty("id").GetGuid();
                using var approval = await client.PatchAsJsonAsync($"{adjustmentsPath}/{adjustmentId}/approval",
                    new { approve = true, notes = "QA adjusted race approval" });
                RequireFinanceStatus(approval, System.Net.HttpStatusCode.OK, "race adjustment approval");
            }
            using var first = await client.PostAsJsonAsync(paymentsPath, new { invoiceId, amount = 600m });
            RequireFinanceStatus(first, System.Net.HttpStatusCode.Created, "race payment 600");
            using var firstJson = System.Text.Json.JsonDocument.Parse(await first.Content.ReadAsStringAsync());
            var paymentId = firstJson.RootElement.GetProperty("id").GetGuid();
            using var reconcile = await client.PatchAsJsonAsync($"{paymentsPath}/{paymentId}/reconcile", new { reference = "QA-RACE" });
            RequireFinanceStatus(reconcile, System.Net.HttpStatusCode.OK, "race reconcile");

            using var left = factory.CreateClient(new() { AllowAutoRedirect = false });
            using var right = factory.CreateClient(new() { AllowAutoRedirect = false });
            left.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
            right.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
            var responses = await Task.WhenAll(
                left.PostAsJsonAsync(paymentsPath, new { invoiceId, amount = finalPayment }),
                right.PostAsJsonAsync(paymentsPath, new { invoiceId, amount = finalPayment }));
            var codes = responses.Select(response => (int)response.StatusCode).OrderBy(code => code).ToArray();
            var rejected = responses.SingleOrDefault(response => response.StatusCode == System.Net.HttpStatusCode.BadRequest);
            var rejectionMessageMatches = false;
            if (rejected is not null)
            {
                using var rejectionJson = System.Text.Json.JsonDocument.Parse(await rejected.Content.ReadAsStringAsync());
                rejectionMessageMatches = rejectionJson.RootElement.TryGetProperty("message", out var message) &&
                    message.GetString() == "Payment exceeds the invoice balance.";
            }
            foreach (var response in responses) response.Dispose();
            if (codes.Any(code => code == 429))
                throw new InvalidOperationException("Rate limit prevented the collection race.");
            var adjustedSnapshot = adjusted
                ? await ReadAdjustmentSnapshotAsync(factory, academyId, studentId, invoiceId, "adjusted-race-" + attempt)
                : null;
            var regularSnapshot = adjusted
                ? null
                : await ReadFinanceSnapshotAsync(factory, invoiceId, academyId, studentId, "race-" + attempt);
            var payments = adjustedSnapshot?.Payments ?? regularSnapshot!.Payments;
            var collected = payments.Where(payment => payment.Status is "Completed" or "Reconciled").Sum(payment => payment.Amount);
            var adjustmentIntact = !adjusted || adjustedSnapshot!.Adjustments.Count == 1 &&
                adjustedSnapshot.Adjustments[0].Status == "Approved" && adjustedSnapshot.Adjustments[0].Amount == 200m &&
                adjustedSnapshot.Adjustments[0].AppliedAtUtc is not null;
            var guarded = codes.Count(code => code == 201) == 1 && codes.Count(code => code == 400) == 1 &&
                rejectionMessageMatches && (adjustedSnapshot?.Total ?? regularSnapshot!.Total) == 1000m &&
                (adjustedSnapshot?.Adjusted ?? regularSnapshot!.Adjusted) == (adjusted ? 200m : 0m) &&
                adjustmentIntact && collected == expectedCollected && payments.Count == 2 &&
                (adjustedSnapshot?.Status ?? regularSnapshot!.Status) == "Paid";
            attempts++;
            if (guarded) protectedAttempts++;
            Console.WriteLine($"FINANCE RACE adjusted={adjusted} attempt={attempt} statuses={string.Join(",", codes)} rejectionMessage={rejectionMessageMatches} collected={collected} rows={payments.Count} guarded={guarded}");
        }
        if (protectedAttempts != attempts)
            throw new InvalidOperationException($"Concurrent collection was not guarded on every attempt: {protectedAttempts}/{attempts}.");
        Console.WriteLine($"FINANCE RACE PASS: adjusted={adjusted} {protectedAttempts}/{attempts} pairs accepted one payment and rejected the other with 400; collected total stayed {expectedCollected}.");
    }
}
