using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Identity;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

internal static partial class SqlHarnessEntryPoint
{
    // Phase 2A evidence only: observe the real endpoints, never repair their rules here.
    private static async Task VerifyReconciledBalanceAsync(QaApiFactory factory, HttpClient client, bool enforceRegression = false)
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
            var roles = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
            if (!await roles.RoleExistsAsync("Student"))
            {
                var role = await roles.CreateAsync(new ApplicationRole { Name = "Student", IsSystemRole = true });
                if (!role.Succeeded) throw new InvalidOperationException("Synthetic Student role setup failed.");
            }
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var student = new ApplicationUser
            {
                UserName = "qa-student-a@example.invalid", Email = "qa-student-a@example.invalid",
                EmailConfirmed = true, DisplayName = "Synthetic Finance Student",
                AcademyId = academyId, StudentId = studentId, IsActive = true
            };
            var created = await users.CreateAsync(student, "Synthetic!39Ab");
            if (!created.Succeeded || !(await users.AddToRoleAsync(student, "Student")).Succeeded)
                throw new InvalidOperationException("Synthetic Student Identity setup failed.");
        }
        var adminToken = await LoginAsync(client, "qa-admin-a@example.invalid", "Synthetic!39Ab");
        var studentToken = await LoginAsync(client, "qa-student-a@example.invalid", "Synthetic!39Ab");
        var invoicesPath = $"/api/academies/{academyId}/invoices";
        var paymentsPath = $"/api/academies/{academyId}/payments";
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        using var create = await client.PostAsJsonAsync(invoicesPath, new { studentId, amount = 1000m });
        RequireFinanceStatus(create, HttpStatusCode.Created, "invoice create");
        using var invoiceJson = JsonDocument.Parse(await create.Content.ReadAsStringAsync());
        var invoiceId = invoiceJson.RootElement.GetProperty("id").GetGuid();
        var initial = await ReadFinanceSnapshotAsync(factory, invoiceId, academyId, studentId, "invoice-created");
        if (initial.Payments.Count != 0 || initial.Status != "Issued")
            throw new InvalidOperationException("Fresh invoice fixture was not empty and Issued.");

        using var payment = await client.PostAsJsonAsync(paymentsPath, new { invoiceId, amount = 600m });
        RequireFinanceStatus(payment, HttpStatusCode.Created, "payment 600");
        using var paymentJson = JsonDocument.Parse(await payment.Content.ReadAsStringAsync());
        var paymentId = paymentJson.RootElement.GetProperty("id").GetGuid();
        var collected = await ReadFinanceSnapshotAsync(factory, invoiceId, academyId, studentId, "payment-600");
        if (collected.Payments.Count != 1 || collected.Payments[0].Id != paymentId ||
            collected.Payments[0].Amount != 600m || collected.Payments[0].Status != "Completed" ||
            collected.Status != "PartiallyPaid")
            throw new InvalidOperationException("Completed-payment fixture did not persist correctly.");
        var before = await ReadFinanceViewsAsync(client, invoicesPath, studentId, invoiceId, adminToken, studentToken);
        if (before != (600m, 400m, 400m))
            throw new InvalidOperationException("Initial admin/student arithmetic control failed.");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        using var excessBefore = await client.PostAsJsonAsync(paymentsPath, new { invoiceId, amount = 500m });
        RequireFinanceStatus(excessBefore, HttpStatusCode.BadRequest, "excess payment before reconciliation");
        var rejected = await ReadFinanceSnapshotAsync(factory, invoiceId, academyId, studentId, "rejected-500-before");
        RequireUnchangedFinance(collected, rejected);
        using var blankReference = await client.PatchAsJsonAsync($"{paymentsPath}/{paymentId}/reconcile", new { reference = " " });
        RequireFinanceStatus(blankReference, HttpStatusCode.BadRequest, "blank reconciliation reference");
        var invalid = await ReadFinanceSnapshotAsync(factory, invoiceId, academyId, studentId, "rejected-blank-reference");
        RequireUnchangedFinance(collected, invalid);

        using var reconcile = await client.PatchAsJsonAsync($"{paymentsPath}/{paymentId}/reconcile", new { reference = "QA-RECONCILE" });
        RequireFinanceStatus(reconcile, HttpStatusCode.OK, "valid reconciliation");
        var reconciled = await ReadFinanceSnapshotAsync(factory, invoiceId, academyId, studentId, "reconciled-600");
        if (reconciled.Payments.Count != 1 || reconciled.Payments[0].Id != paymentId ||
            reconciled.Payments[0].Amount != 600m || reconciled.Payments[0].Status != "Reconciled" ||
            reconciled.Payments[0].ReconciliationReference != "QA-RECONCILE" ||
            reconciled.Payments[0].ReconciledAtUtc is null || reconciled.Status != "PartiallyPaid")
            throw new InvalidOperationException("Reconciliation fixture did not persist correctly.");
        var after = await ReadFinanceViewsAsync(client, invoicesPath, studentId, invoiceId, adminToken, studentToken);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        using var excessAfter = await client.PostAsJsonAsync(paymentsPath, new { invoiceId, amount = 500m });
        if (excessAfter.StatusCode is not (HttpStatusCode.Created or HttpStatusCode.BadRequest))
            throw new InvalidOperationException($"Unexpected post-reconcile collection status {(int)excessAfter.StatusCode}.");
        var final = await ReadFinanceSnapshotAsync(factory, invoiceId, academyId, studentId, "attempted-500-after");
        if (excessAfter.StatusCode == HttpStatusCode.BadRequest) RequireUnchangedFinance(reconciled, final);
        else if (final.Payments.Count != 2 || final.Payments.Sum(x => x.Amount) != 1100m ||
                 !final.Payments.Any(x => x.Amount == 500m && x.Status == "Completed") ||
                 !final.Payments.Any(x => x.Id == paymentId && x.Status == "Reconciled"))
            throw new InvalidOperationException("Accepted excess-payment response did not match its SQL ledger.");
        var failed = after != (600m, 400m, 400m) || excessAfter.StatusCode != HttpStatusCode.BadRequest;
        Console.WriteLine($"FINANCE-RULE-001 {(failed ? "FAIL" : "PASS")}: invoice create=201/1000; payment=201/600; " +
            "pre-reconcile admin paid=600/balance=400/student balance=400; excess 500=400/no SQL change; " +
            "blank reference=400/no SQL change; reconcile=200; " +
            $"post-reconcile admin paid={after.Paid}/balance={after.AdminBalance}/student balance={after.StudentBalance}; " +
            $"extra 500 HTTP={(int)excessAfter.StatusCode}/SQL rows={final.Payments.Count}/ledger={final.Payments.Sum(x => x.Amount)}; " +
            "expected paid=600/balance=400/reject 500 with unchanged ledger.");
        if (enforceRegression)
        {
            if (failed) throw new InvalidOperationException("FINANCE-RULE-001 repair regression failed.");
            await VerifyReconciliationConsumersAsync(factory, client, academyId, studentId, invoiceId, invoicesPath, paymentsPath, adminToken, studentToken);
        }
    }

    private static void RequireFinanceStatus(HttpResponseMessage response, HttpStatusCode expected, string operation)
    {
        if (response.StatusCode != expected)
            throw new InvalidOperationException($"Finance fixture {operation}: expected {(int)expected}, actual {(int)response.StatusCode}.");
    }

    private static async Task<(decimal Paid, decimal AdminBalance, decimal StudentBalance)> ReadFinanceViewsAsync(
        HttpClient client, string invoicesPath, Guid studentId, Guid invoiceId, string adminToken, string studentToken)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        using var admin = await client.GetAsync(invoicesPath);
        RequireFinanceStatus(admin, HttpStatusCode.OK, "admin invoice list");
        using var adminJson = JsonDocument.Parse(await admin.Content.ReadAsStringAsync());
        var invoice = adminJson.RootElement.EnumerateArray().Single(x => x.GetProperty("id").GetGuid() == invoiceId);
        var paid = invoice.GetProperty("paidAmount").GetDecimal();
        var adminBalance = invoice.GetProperty("balance").GetDecimal();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", studentToken);
        using var student = await client.GetAsync($"/api/portal/students/{studentId}");
        RequireFinanceStatus(student, HttpStatusCode.OK, "student portal invoice read");
        using var studentJson = JsonDocument.Parse(await student.Content.ReadAsStringAsync());
        var portalInvoice = studentJson.RootElement.GetProperty("invoices").EnumerateArray()
            .Single(x => x.GetProperty("id").GetGuid() == invoiceId);
        if (invoice.GetProperty("totalAmount").GetDecimal() != 1000m ||
            portalInvoice.GetProperty("totalAmount").GetDecimal() != 1000m)
            throw new InvalidOperationException("Invoice views returned the wrong synthetic total.");
        return (paid, adminBalance, portalInvoice.GetProperty("balance").GetDecimal());
    }

    private static async Task<FinanceSnapshot> ReadFinanceSnapshotAsync(
        QaApiFactory factory, Guid invoiceId, Guid academyId, Guid studentId, string stage)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
        var invoice = await db.Invoices.AsNoTracking().SingleAsync(x => x.Id == invoiceId);
        if (invoice.AcademyId != academyId || invoice.StudentId != studentId ||
            invoice.TotalAmount != 1000m || invoice.AdjustedAmount != 0m)
            throw new InvalidOperationException("Invoice SQL ownership/amount fixture changed unexpectedly.");
        var rows = await db.Payments.AsNoTracking().Where(x => x.InvoiceId == invoiceId).OrderBy(x => x.Id)
            .Select(x => new FinancePayment(x.Id, x.AcademyId, x.Amount, x.Status, x.ReconciliationReference, x.ReconciledAtUtc))
            .ToListAsync();
        if (rows.Any(x => x.AcademyId != academyId))
            throw new InvalidOperationException("Finance payment SQL ownership mismatch.");
        var snapshot = new FinanceSnapshot(invoice.TotalAmount, invoice.AdjustedAmount, invoice.Status, rows);
        Console.WriteLine($"FINANCE SQL {stage}: {JsonSerializer.Serialize(snapshot)}");
        return snapshot;
    }

    private static void RequireUnchangedFinance(FinanceSnapshot before, FinanceSnapshot after)
    {
        if (JsonSerializer.Serialize(before) != JsonSerializer.Serialize(after))
            throw new InvalidOperationException("Rejected finance request changed the persisted invoice/payment snapshot.");
    }

    private sealed record FinancePayment(Guid Id, Guid AcademyId, decimal Amount, string Status,
        string? ReconciliationReference, DateTime? ReconciledAtUtc);
    private sealed record FinanceSnapshot(decimal Total, decimal Adjusted, string Status, List<FinancePayment> Payments);
}
