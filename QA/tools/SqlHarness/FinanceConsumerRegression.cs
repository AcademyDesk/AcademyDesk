using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Domain.Identity;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

internal static partial class SqlHarnessEntryPoint
{
    private static async Task VerifyFinanceConsumersAsync(QaApiFactory factory, HttpClient client)
    {
        Guid academyId, studentId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            academyId = await db.Academies.Where(x => x.Name == "Synthetic Academy A").Select(x => x.Id).SingleAsync();
            studentId = await db.Students.Where(x => x.AcademyId == academyId && x.FirstName == "Isolated-A").Select(x => x.Id).SingleAsync();
            var roles = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
            if (!await roles.RoleExistsAsync("Student") && !(await roles.CreateAsync(new ApplicationRole { Name = "Student", IsSystemRole = true })).Succeeded)
                throw new InvalidOperationException("Consumer Student role setup failed.");
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = new ApplicationUser { UserName = "qa-consumer-student@example.invalid", Email = "qa-consumer-student@example.invalid",
                DisplayName = "Synthetic Consumer Student", EmailConfirmed = true, AcademyId = academyId, StudentId = studentId, IsActive = true };
            if (!(await users.CreateAsync(user, "Synthetic!39Ab")).Succeeded || !(await users.AddToRoleAsync(user, "Student")).Succeeded)
                throw new InvalidOperationException("Consumer Student identity setup failed.");
        }
        var adminToken = await LoginAsync(client, "qa-admin-a@example.invalid", "Synthetic!39Ab");
        var studentToken = await LoginAsync(client, "qa-consumer-student@example.invalid", "Synthetic!39Ab");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var invoicesPath = $"/api/academies/{academyId}/invoices";
        var paymentsPath = $"/api/academies/{academyId}/payments";
        var adjustmentsPath = $"/api/academies/{academyId}/finance-adjustments";
        var mixedId = await CreateApprovedAdjustmentFixtureAsync(factory, client, academyId, studentId,
            invoicesPath, adjustmentsPath, 200m, "consumer-mixed");
        using var first = await client.PostAsJsonAsync(paymentsPath, new { invoiceId = mixedId, amount = 600m });
        RequireFinanceStatus(first, HttpStatusCode.Created, "consumer payment 600");
        using var firstJson = JsonDocument.Parse(await first.Content.ReadAsStringAsync());
        using var reconcile = await client.PatchAsJsonAsync($"{paymentsPath}/{firstJson.RootElement.GetProperty("id").GetGuid()}/reconcile", new { reference = "QA-CONSUMER-RECON" });
        RequireFinanceStatus(reconcile, HttpStatusCode.OK, "consumer reconciliation");
        using var second = await client.PostAsJsonAsync(paymentsPath, new { invoiceId = mixedId, amount = 100m });
        RequireFinanceStatus(second, HttpStatusCode.Created, "consumer payment 100");
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            // Fixture only; no void/restoration policy action inferred.
            db.Payments.Add(new Payment { AcademyId = academyId, InvoiceId = mixedId, Amount = 75m, Status = "Voided" });
            await db.SaveChangesAsync();
        }
        await VerifyConsumerViewsAsync(factory, client, academyId, studentId, mixedId, adminToken, studentToken, 700m, 100m, "mixed-adjusted");
        using var settle = await client.PostAsJsonAsync(paymentsPath, new { invoiceId = mixedId, amount = 100m });
        RequireFinanceStatus(settle, HttpStatusCode.Created, "consumer exact settlement");
        await VerifyConsumerViewsAsync(factory, client, academyId, studentId, mixedId, adminToken, studentToken, 800m, 0m, "adjusted-settled");

        var fullId = await CreateApprovedAdjustmentFixtureAsync(factory, client, academyId, studentId,
            invoicesPath, adjustmentsPath, 1000m, "consumer-full-adjustment");
        await VerifyConsumerViewsAsync(factory, client, academyId, studentId, fullId, adminToken, studentToken, 0m, 0m, "fully-adjusted");

        await Task.Delay(1100); // Avoid unrelated seconds/random invoice-number collisions.
        using var pendingInvoice = await client.PostAsJsonAsync(invoicesPath, new { studentId, amount = 1000m });
        RequireFinanceStatus(pendingInvoice, HttpStatusCode.Created, "consumer pending invoice");
        using var pendingInvoiceJson = JsonDocument.Parse(await pendingInvoice.Content.ReadAsStringAsync());
        var pendingId = pendingInvoiceJson.RootElement.GetProperty("id").GetGuid();
        using var proposal = await client.PostAsJsonAsync(adjustmentsPath, new { invoiceId = pendingId, type = "Discount", amount = 200m, reason = "QA unapplied consumer" });
        RequireFinanceStatus(proposal, HttpStatusCode.OK, "consumer proposal");
        using var proposalJson = JsonDocument.Parse(await proposal.Content.ReadAsStringAsync());
        using var partial = await client.PostAsJsonAsync(paymentsPath, new { invoiceId = pendingId, amount = 600m });
        RequireFinanceStatus(partial, HttpStatusCode.Created, "consumer pending payment");
        await VerifyConsumerViewsAsync(factory, client, academyId, studentId, pendingId, adminToken, studentToken, 600m, 400m, "pending-unapplied");
        using var reject = await client.PatchAsJsonAsync($"{adjustmentsPath}/{proposalJson.RootElement.GetProperty("id").GetGuid()}/approval", new { approve = false, notes = "QA consumer reject" });
        RequireFinanceStatus(reject, HttpStatusCode.OK, "consumer proposal rejection");
        await VerifyConsumerViewsAsync(factory, client, academyId, studentId, pendingId, adminToken, studentToken, 600m, 400m, "rejected-unapplied");

        var centId = await CreateApprovedAdjustmentFixtureAsync(factory, client, academyId, studentId,
            invoicesPath, adjustmentsPath, 200m, "consumer-cent");
        using var centPayment = await client.PostAsJsonAsync(paymentsPath, new { invoiceId = centId, amount = 799.99m });
        RequireFinanceStatus(centPayment, HttpStatusCode.Created, "consumer cent payment");
        await VerifyConsumerViewsAsync(factory, client, academyId, studentId, centId, adminToken, studentToken, 799.99m, 0.01m, "one-cent-outstanding");

        var otherToken = await LoginAsync(client, "qa-admin-b@example.invalid", "Synthetic!39Ab");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", otherToken);
        using var foreign = await client.PostAsJsonAsync($"/api/academies/{academyId}/fee-reminders", new { invoiceId = mixedId });
        RequireFinanceStatus(foreign, HttpStatusCode.Forbidden, "foreign consumer reminder");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", studentToken);
        using var studentReminder = await client.PostAsJsonAsync($"/api/academies/{academyId}/fee-reminders", new { invoiceId = mixedId });
        RequireFinanceStatus(studentReminder, HttpStatusCode.Forbidden, "student cannot queue reminder");
        using var foreignDocument = await client.GetAsync($"/api/portal/students/{Guid.NewGuid()}/invoices/{mixedId}/download");
        RequireFinanceStatus(foreignDocument, HttpStatusCode.Forbidden, "foreign student invoice document");
        Console.WriteLine("CONSUMER REGRESSION PASS: six stages agree across fresh SQL/Admin/Student/document/dashboard/reminder; approved versus PendingApproval/Rejected, mixed Completed/Reconciled/Voided, full adjustment, settlement and cent balance; read/reminder financial snapshots unchanged; foreign/student reminder and foreign document denied. No restoration or full critical acceptance.");
    }

    private static async Task VerifyConsumerViewsAsync(QaApiFactory factory, HttpClient client, Guid academyId,
        Guid studentId, Guid invoiceId, string adminToken, string studentToken, decimal expectedPaid, decimal expectedBalance, string stage)
    {
        var before = await ReadAdjustmentSnapshotAsync(factory, academyId, studentId, invoiceId, "consumer-" + stage);
        if (before.Payments.Where(x => x.Status is "Completed" or "Reconciled").Sum(x => x.Amount) != expectedPaid ||
            Math.Max(0m, before.Total - before.Adjusted - expectedPaid) != expectedBalance)
            throw new InvalidOperationException("Consumer SQL fixture arithmetic differs.");
        var views = await ReadFinanceViewsAsync(client, $"/api/academies/{academyId}/invoices", studentId, invoiceId, adminToken, studentToken);
        if (views != (expectedPaid, expectedBalance, expectedBalance)) throw new InvalidOperationException("Consumer Admin/Student balances differ.");
        using var document = await client.GetAsync($"/api/portal/students/{studentId}/invoices/{invoiceId}/download");
        RequireFinanceStatus(document, HttpStatusCode.OK, "consumer invoice document");
        var html = await document.Content.ReadAsStringAsync();
        if (!html.Contains($"Adjustments: INR {before.Adjusted:N2}") || !html.Contains($"Paid: INR {expectedPaid:N2}") || !html.Contains($"Balance: INR {expectedBalance:N2}"))
            throw new InvalidOperationException("Consumer document balance/evidence differs.");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        decimal gross, applied, collected; int notificationsBefore;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            gross = await db.Invoices.Where(x => x.AcademyId == academyId).SumAsync(x => x.TotalAmount);
            applied = await db.Invoices.Where(x => x.AcademyId == academyId).SumAsync(x => x.AdjustedAmount);
            collected = await db.Payments.Where(x => x.AcademyId == academyId && (x.Status == "Completed" || x.Status == "Reconciled")).SumAsync(x => x.Amount);
            notificationsBefore = await db.Notifications.CountAsync(x => x.AcademyId == academyId && x.RecipientId == studentId && x.Title == "Fee payment reminder");
        }
        using var dashboard = await client.GetAsync($"/api/academies/{academyId}/dashboard");
        RequireFinanceStatus(dashboard, HttpStatusCode.OK, "consumer dashboard");
        using var dashboardJson = JsonDocument.Parse(await dashboard.Content.ReadAsStringAsync());
        if (dashboardJson.RootElement.GetProperty("totalInvoiced").GetDecimal() != gross ||
            dashboardJson.RootElement.GetProperty("totalPaid").GetDecimal() != collected ||
            dashboardJson.RootElement.GetProperty("outstandingBalance").GetDecimal() != gross - applied - collected)
            throw new InvalidOperationException("Consumer dashboard gross/applied/collected arithmetic differs.");
        using var reminder = await client.PostAsJsonAsync($"/api/academies/{academyId}/fee-reminders", new { invoiceId, channel = "InApp" });
        RequireFinanceStatus(reminder, HttpStatusCode.OK, "consumer reminder");
        using var reminderJson = JsonDocument.Parse(await reminder.Content.ReadAsStringAsync());
        var expectedCount = expectedBalance > 0m ? 1 : 0;
        if (reminderJson.RootElement.GetProperty("queuedCount").GetInt32() != expectedCount)
            throw new InvalidOperationException("Consumer reminder queued money not owed.");
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var notifications = db.Notifications.AsNoTracking().Where(x => x.AcademyId == academyId && x.RecipientId == studentId && x.Title == "Fee payment reminder");
            if (await notifications.CountAsync() != notificationsBefore + expectedCount) throw new InvalidOperationException("Consumer reminder notification delta differs.");
            if (expectedCount == 1 && !(await notifications.OrderByDescending(x => x.CreatedAtUtc).Select(x => x.Message).FirstAsync()).Contains($"INR {expectedBalance:0.00}"))
                throw new InvalidOperationException("Consumer reminder amount differs.");
        }
        RequireUnchangedAdjustment(before, await ReadAdjustmentSnapshotAsync(factory, academyId, studentId, invoiceId, "consumer-" + stage + "-after-reads"));
        Console.WriteLine($"CONSUMER STAGE {stage} PASS: SQL paid={expectedPaid}/adjusted={before.Adjusted}/balance={expectedBalance}; Admin/Student/document/dashboard agree; reminder queued={expectedCount}, financial snapshot unchanged.");
    }
}
