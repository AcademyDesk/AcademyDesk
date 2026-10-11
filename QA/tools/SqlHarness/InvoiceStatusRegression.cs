using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Infrastructure;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

internal static partial class SqlHarnessEntryPoint
{
    private static async Task VerifyInvoiceStatusAsync(QaApiFactory factory, HttpClient client)
    {
        var baseline = Environment.GetEnvironmentVariable("QA_INVOICE_STATUS_BASELINE") == "1";
        Guid academy, foreign, student, foreignStudent;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            academy = await db.Academies.Where(x => x.Name == "Synthetic Academy A").Select(x => x.Id).SingleAsync();
            foreign = await db.Academies.Where(x => x.Name == "Synthetic Academy B").Select(x => x.Id).SingleAsync();
            student = await db.Students.Where(x => x.AcademyId == academy).Select(x => x.Id).SingleAsync();
            foreignStudent = await db.Students.Where(x => x.AcademyId == foreign).Select(x => x.Id).SingleAsync();
            foreach (var id in new[] { academy, foreign }) (await db.Academies.SingleAsync(x => x.Id == id)).EnabledModulesJson = "[\"Core\",\"Finance\",\"FinanceControls\"]";
            await db.SaveChangesAsync();
        }
        var admin = await LoginAsync(client, "qa-admin-a@example.invalid", "Synthetic!39Ab");
        var teacher = await LoginAsync(client, "qa-teacher-a@example.invalid", "Synthetic!39Ab");
        var otherAdmin = await LoginAsync(client, "qa-admin-b@example.invalid", "Synthetic!39Ab");
        client.DefaultRequestHeaders.Authorization = null;
        var checks = 0;
        async Task<Invoice> Seed(decimal paid, decimal adjusted, string paymentStatus, bool other = false)
        {
            using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var collected = paymentStatus == "Voided" ? 0m : paid;
            var invoice = new Invoice { AcademyId = other ? foreign : academy, StudentId = other ? foreignStudent : student,
                InvoiceNumber = Guid.NewGuid().ToString("N"), TotalAmount = 1000m, AdjustedAmount = adjusted,
                Status = 1000m - adjusted - collected == 0m ? "Paid" : collected > 0m ? "PartiallyPaid" : "Issued" };
            db.Add(invoice);
            if (paid > 0m) db.Add(new Payment { AcademyId = invoice.AcademyId, InvoiceId = invoice.Id, Amount = paid,
                Status = paymentStatus, Reference = "Keep original reference", ReconciliationReference = paymentStatus == "Reconciled" ? "QA-KEEP" : null,
                ReconciledAtUtc = paymentStatus == "Reconciled" ? DateTime.UtcNow : null });
            if (adjusted > 0m) db.Add(new FinanceAdjustment { AcademyId = invoice.AcademyId, InvoiceId = invoice.Id,
                Amount = adjusted, Reason = "Synthetic approved fixture", Status = "Approved", AppliedAtUtc = DateTime.UtcNow });
            db.Add(new FinanceAdjustment { AcademyId = invoice.AcademyId, InvoiceId = invoice.Id,
                Amount = 777m, Reason = "Pending must not reduce collectible", Status = "PendingApproval" });
            await db.SaveChangesAsync(); return invoice;
        }
        async Task<string> Snapshot(bool audits = true)
        {
            using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            return JsonSerializer.Serialize(new { Invoices = await db.Invoices.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(),
                Payments = await db.Payments.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(),
                Adjustments = await db.FinanceAdjustments.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(),
                Audits = audits ? await db.AuditLogs.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync() : [] });
        }
        async Task Check(Invoice invoice, object body, HttpStatusCode expected, string? actor, string? canonical = null, string? path = null)
        {
            var before = await Snapshot(); await Task.Delay(750);
            using var request = new HttpRequestMessage(HttpMethod.Patch, path ?? $"/api/academies/{academy}/invoices/{invoice.Id}/status") { Content = JsonContent.Create(body) };
            if (actor is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", actor);
            using var response = await client.SendAsync(request); RequireFinanceStatus(response, expected, "invoice status");
            if (!response.IsSuccessStatusCode)
            {
                if (before != await Snapshot()) throw new InvalidOperationException("Rejected status changed ledger or audit.");
            }
            else
            {
                using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
                var stored = await db.Invoices.AsNoTracking().SingleAsync(x => x.Id == invoice.Id);
                if (stored.Status != canonical) throw new InvalidOperationException("Canonical status mismatch.");
                // Whole database snapshot permits only this invoice's status and one success audit.
                var old = JsonSerializer.Deserialize<JsonElement>(before);
                var expectedInvoices = old.GetProperty("Invoices").EnumerateArray().Select(x => JsonSerializer.Deserialize<Invoice>(x.GetRawText())!).ToArray();
                expectedInvoices.Single(x => x.Id == invoice.Id).Status = canonical!;
                var after = JsonSerializer.Deserialize<JsonElement>(await Snapshot());
                if (JsonSerializer.Serialize(expectedInvoices) != after.GetProperty("Invoices").GetRawText() ||
                    old.GetProperty("Payments").GetRawText() != after.GetProperty("Payments").GetRawText() ||
                    old.GetProperty("Adjustments").GetRawText() != after.GetProperty("Adjustments").GetRawText() ||
                    after.GetProperty("Audits").GetArrayLength() != old.GetProperty("Audits").GetArrayLength() + 1)
                    throw new InvalidOperationException("Status action changed unrelated ledger fields/rows or success audit count.");
                var beforeAudits = old.GetProperty("Audits").EnumerateArray().ToArray();
                var afterAudits = after.GetProperty("Audits").EnumerateArray().ToArray();
                foreach (var previous in beforeAudits)
                    if (afterAudits.Single(x => x.GetProperty("Id").GetGuid() == previous.GetProperty("Id").GetGuid()).GetRawText() != previous.GetRawText())
                        throw new InvalidOperationException("Existing audit was changed.");
                var added = afterAudits.Single(x => !beforeAudits.Any(v => v.GetProperty("Id").GetGuid() == x.GetProperty("Id").GetGuid()));
                if (added.GetProperty("AcademyId").GetGuid() != academy || added.GetProperty("Action").GetString() != "PATCH Invoices" ||
                    string.IsNullOrWhiteSpace(added.GetProperty("ActorUserId").GetString()))
                    throw new InvalidOperationException("Success audit authority mismatch.");
                var ledger = await new OutstandingFeesService(db, TimeProvider.System).Ledger(academy).SingleAsync(x => x.Id == invoice.Id);
                using var get = new HttpRequestMessage(HttpMethod.Get, $"/api/academies/{academy}/invoices"); get.Headers.Authorization = new("Bearer", admin);
                using var read = await client.SendAsync(get); RequireFinanceStatus(read, HttpStatusCode.OK, "invoice register");
                var summary = (await read.Content.ReadFromJsonAsync<InvoiceSummary[]>())!.Single(x => x.Id == invoice.Id);
                if (summary.Status != canonical || summary.PaidAmount != ledger.Collected || summary.AdjustedAmount != ledger.Adjusted ||
                    summary.Balance != Math.Max(0m, ledger.Balance)) throw new InvalidOperationException("Register/shared ledger parity mismatch.");
            }
            Console.WriteLine($"PASS: INVOICESTATUS case={++checks} HTTP={(int)response.StatusCode} freshSql=true no429=true");
        }
        await Seed(123m, 0m, "Reconciled", true); // Non-zero foreign sentinel included in every snapshot.
        if (baseline)
        {
            var unpaid = await Seed(0m, 0m, "Completed");
            await Check(unpaid, new { status = "Paid" }, HttpStatusCode.OK, admin, "Paid");
            using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var ledger = await new OutstandingFeesService(db, TimeProvider.System).Ledger(academy).SingleAsync(x => x.Id == unpaid.Id);
            if (ledger.Status != "Paid" || ledger.Collected != 0m || ledger.Balance != 1000m) throw new InvalidOperationException("Expected baseline defect not reproduced.");
            await Check(unpaid, new { status = "paid" }, HttpStatusCode.OK, admin, "paid");
            Console.WriteLine("PASS: INVOICESTATUS BASELINE REPRODUCED ONLY: unpaid Paid/1000 balance and noncanonical paid; NOT acceptance."); return;
        }
        foreach (var row in new[] { (0m, 0m, "Completed"), (600m, 0m, "Completed"), (1000m, 0m, "Completed"),
            (600m, 0m, "Reconciled"), (1000m, 0m, "Voided"), (800m, 200m, "Reconciled"), (0m, 1000m, "Completed") })
        {
            var invoice = await Seed(row.Item1, row.Item2, row.Item3);
            var collected = row.Item3 == "Voided" ? 0m : row.Item1; var balance = 1000m - row.Item2 - collected;
            foreach (var status in new[] { "Issued", "PartiallyPaid", "Paid", "Overdue", "Cancelled" })
            {
                var allowed = status == "Cancelled" || (balance == 0m ? status == "Paid" : collected > 0m ? status == "PartiallyPaid" : status is "Issued" or "Overdue");
                await Check(invoice, new { status = status.ToLowerInvariant() }, allowed ? HttpStatusCode.OK : HttpStatusCode.BadRequest, admin, allowed ? status : null);
            }
        }
        var control = await Seed(0m, 0m, "Completed");
        foreach (var invalid in new string?[] { null, "", " ", "invalid" }) await Check(control, new { status = invalid }, HttpStatusCode.BadRequest, admin);
        await Check(control, new { status = 42 }, HttpStatusCode.BadRequest, admin);
        await Check(control, new { status = "Issued" }, HttpStatusCode.Unauthorized, null);
        await Check(control, new { status = "Issued" }, HttpStatusCode.Forbidden, teacher);
        await Check(control, new { status = "Issued" }, HttpStatusCode.Forbidden, otherAdmin);
        await Check(control, new { status = "Issued" }, HttpStatusCode.Forbidden, admin, path: $"/api/academies/{foreign}/invoices/{control.Id}/status");
        await Check(control, new { status = "Issued" }, HttpStatusCode.NotFound, admin, path: $"/api/academies/{academy}/invoices/{Guid.NewGuid()}/status");
        Console.WriteLine($"PASS: INVOICESTATUS {checks} strict real Identity/HTTP/SQL controls; ledger fields/audit/foreign snapshot and canonical register parity. Cancellation policy, concurrent/audit fault/browser/device/release gates remain OPEN.");
        // These two suites formerly ran in separate hosts. Respect the unchanged
        // fixed-window limiter when combining them; never override it for QA.
        Console.WriteLine("PASS: INVOICESTATUS pacing61s before retained transition regression; rate limit unchanged.");
        await Task.Delay(TimeSpan.FromSeconds(61));
        await VerifyPaymentTransitionsAsync(factory, client, enforceRegression: true);
    }
}
