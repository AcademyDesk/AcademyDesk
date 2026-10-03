using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

internal static partial class SqlHarnessEntryPoint
{
    private static async Task VerifyReconciliationConsumersAsync(QaApiFactory factory, HttpClient client,
        Guid academyId, Guid studentId, Guid invoiceId, string invoicesPath, string paymentsPath,
        string adminToken, string studentToken)
    {
        // Voided row is fixture data, not a test/approval of restoration policy.
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            db.Payments.Add(new Payment { AcademyId = academyId, InvoiceId = invoiceId, Amount = 75m, Status = "Voided" });
            await db.SaveChangesAsync();
        }
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        using var mixedPayment = await client.PostAsJsonAsync(paymentsPath, new { invoiceId, amount = 100m });
        RequireFinanceStatus(mixedPayment, HttpStatusCode.Created, "mixed-status payment");
        if (await ReadFinanceViewsAsync(client, invoicesPath, studentId, invoiceId, adminToken, studentToken) != (700m, 300m, 300m))
            throw new InvalidOperationException("Mixed collected/Voided admin/student balances differ.");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", studentToken);
        using var document = await client.GetAsync($"/api/portal/students/{studentId}/invoices/{invoiceId}/download");
        RequireFinanceStatus(document, HttpStatusCode.OK, "student invoice document");
        var html = await document.Content.ReadAsStringAsync();
        if (!html.Contains("Paid: INR 700.00") || !html.Contains("Balance: INR 300.00"))
            throw new InvalidOperationException("Invoice document omits reconciled collection or counts Voided money.");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        using var dashboard = await client.GetAsync($"/api/academies/{academyId}/dashboard");
        RequireFinanceStatus(dashboard, HttpStatusCode.OK, "dashboard");
        using var dashboardJson = JsonDocument.Parse(await dashboard.Content.ReadAsStringAsync());
        if (dashboardJson.RootElement.GetProperty("totalPaid").GetDecimal() != 700m ||
            dashboardJson.RootElement.GetProperty("outstandingBalance").GetDecimal() != 300m)
            throw new InvalidOperationException("Dashboard collected totals differ.");

        var reminders = $"/api/academies/{academyId}/fee-reminders";
        using var reminder = await client.PostAsJsonAsync(reminders, new { invoiceId, channel = "InApp" });
        RequireFinanceStatus(reminder, HttpStatusCode.OK, "partial invoice reminder");
        using var reminderJson = JsonDocument.Parse(await reminder.Content.ReadAsStringAsync());
        if (reminderJson.RootElement.GetProperty("queuedCount").GetInt32() != 1)
            throw new InvalidOperationException("Partial invoice reminder count mismatch.");
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var message = await db.Notifications.AsNoTracking().Where(x => x.AcademyId == academyId && x.RecipientId == studentId && x.Title == "Fee payment reminder").Select(x => x.Message).SingleAsync();
            if (!message.Contains("INR 300.00")) throw new InvalidOperationException("Reminder requests already collected money.");
        }
        using var settlement = await client.PostAsJsonAsync(paymentsPath, new { invoiceId, amount = 300m });
        RequireFinanceStatus(settlement, HttpStatusCode.Created, "exact mixed-status settlement");
        var settled = await ReadFinanceSnapshotAsync(factory, invoiceId, academyId, studentId, "regression-settled");
        if (settled.Status != "Paid" || settled.Payments.Count != 4 ||
            settled.Payments.Where(x => x.Status == "Completed" || x.Status == "Reconciled").Sum(x => x.Amount) != 1000m)
            throw new InvalidOperationException("Exact settlement status/ledger mismatch.");
        if (await ReadFinanceViewsAsync(client, invoicesPath, studentId, invoiceId, adminToken, studentToken) != (1000m, 0m, 0m))
            throw new InvalidOperationException("Settled admin/student views differ.");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        using var excess = await client.PostAsJsonAsync(paymentsPath, new { invoiceId, amount = 0.01m });
        RequireFinanceStatus(excess, HttpStatusCode.BadRequest, "one cent over settled balance");
        RequireUnchangedFinance(settled, await ReadFinanceSnapshotAsync(factory, invoiceId, academyId, studentId, "regression-rejected-cent"));
        using var settledReminder = await client.PostAsJsonAsync(reminders, new { invoiceId, channel = "InApp" });
        RequireFinanceStatus(settledReminder, HttpStatusCode.OK, "settled invoice reminder");
        using var settledReminderJson = JsonDocument.Parse(await settledReminder.Content.ReadAsStringAsync());
        if (settledReminderJson.RootElement.GetProperty("queuedCount").GetInt32() != 0)
            throw new InvalidOperationException("Settled invoice still requests collected money.");
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            if (await db.Notifications.CountAsync(x => x.AcademyId == academyId && x.Title == "Fee payment reminder") != 1)
                throw new InvalidOperationException("Settled reminder unexpectedly created another notification.");
        }
        Console.WriteLine("FINANCE REGRESSION PASS: original reconciliation case, mixed Completed/Reconciled/Voided balances, invoice document, dashboard, reminder amount, exact settlement/Paid SQL, one-cent rejection/no SQL change, settled reminder zero/new-notification absent.");
    }
}
