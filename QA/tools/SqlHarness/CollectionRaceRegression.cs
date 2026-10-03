using System.Net.Http.Headers;
using System.Net.Http.Json;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

internal static partial class SqlHarnessEntryPoint
{
    // Two collections of the remaining balance, started together. Sequential rejection is a separate case.
    private static async Task VerifyCollectionRaceAsync(QaApiFactory factory, HttpClient client)
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
        var attempts = 0;
        var protectedAttempts = 0;
        for (var attempt = 1; attempt <= 5; attempt++)
        {
            using var create = await client.PostAsJsonAsync(invoicesPath, new { studentId, amount = 1000m });
            RequireFinanceStatus(create, System.Net.HttpStatusCode.Created, "race invoice");
            using var invoiceJson = System.Text.Json.JsonDocument.Parse(await create.Content.ReadAsStringAsync());
            var invoiceId = invoiceJson.RootElement.GetProperty("id").GetGuid();
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
                left.PostAsJsonAsync(paymentsPath, new { invoiceId, amount = 400m }),
                right.PostAsJsonAsync(paymentsPath, new { invoiceId, amount = 400m }));
            var codes = responses.Select(response => (int)response.StatusCode).OrderBy(code => code).ToArray();
            foreach (var response in responses) response.Dispose();
            if (codes.Any(code => code == 429))
                throw new InvalidOperationException("Rate limit prevented the collection race.");
            var snapshot = await ReadFinanceSnapshotAsync(factory, invoiceId, academyId, studentId, "race-" + attempt);
            var collected = snapshot.Payments.Where(payment => payment.Status is "Completed" or "Reconciled").Sum(payment => payment.Amount);
            var guarded = codes.Count(code => code == 201) == 1 && codes.Count(code => code == 400) == 1 && collected == 1000m && snapshot.Payments.Count == 2;
            attempts++;
            if (guarded) protectedAttempts++;
            Console.WriteLine($"FINANCE RACE attempt={attempt} statuses={string.Join(",", codes)} collected={collected} rows={snapshot.Payments.Count} guarded={guarded}");
        }
        if (protectedAttempts != attempts)
            throw new InvalidOperationException($"Concurrent collection was not guarded on every attempt: {protectedAttempts}/{attempts}.");
        Console.WriteLine($"FINANCE RACE PASS: {protectedAttempts}/{attempts} pairs accepted one 400 payment and rejected the other; collected total stayed 1000.");
    }
}
