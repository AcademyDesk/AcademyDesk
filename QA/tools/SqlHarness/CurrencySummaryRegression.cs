using System.Net;
using System.Net.Http.Headers;
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
    private static async Task VerifyCurrencySummariesAsync(QaApiFactory factory, HttpClient client)
    {
        Guid academy, student;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            academy = await db.Academies.Where(x => x.Name == "Synthetic Academy A").Select(x => x.Id).SingleAsync();
            student = await db.Students.Where(x => x.AcademyId == academy).Select(x => x.Id).SingleAsync();
            foreach (var item in new[] { (Currency: "INR", Gross: 1000.25m, Adjusted: 100.10m, Paid: 600.05m), (Currency: "USD", Gross: 100.50m, Adjusted: 10.20m, Paid: 20.10m) })
            {
                var invoice = new Invoice { AcademyId = academy, StudentId = student, InvoiceNumber = "QA-CURRENCY-" + item.Currency,
                    Currency = item.Currency, TotalAmount = item.Gross, AdjustedAmount = item.Adjusted, Status = "PartiallyPaid", DueDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1) };
                db.Invoices.Add(invoice);
                db.Payments.AddRange(new Payment { AcademyId = academy, InvoiceId = invoice.Id, Currency = item.Currency, Amount = item.Paid, Status = "Reconciled" },
                    new Payment { AcademyId = academy, InvoiceId = invoice.Id, Currency = item.Currency, Amount = 9999m, Status = "Voided" });
            }
            foreach (var item in new[] { (Currency: "INR", Amount: 600.05m, Status: "Paid"), (Currency: "INR", Amount: 400.20m, Status: "Issued"),
                (Currency: "USD", Amount: 20.10m, Status: "Paid"), (Currency: "USD", Amount: 80.40m, Status: "Overdue"),
                (Currency: "EUR", Amount: 99999m, Status: "Draft"), (Currency: "EUR", Amount: 99999m, Status: "Void") })
                db.PlatformBillingInvoices.Add(new PlatformBillingInvoice { AcademyId = academy, InvoiceNumber = "QA-PLATFORM-CURRENCY-" + Guid.NewGuid().ToString("N"), Currency = item.Currency, Amount = item.Amount, Status = item.Status });
            await db.SaveChangesAsync();
        }
        var ownerId = await CreateAccessActorAsync(factory, "qa-currency-owner@example.invalid", "PlatformOwner", academy);
        using (var scope = factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var owner = (await users.FindByIdAsync(ownerId.ToString()))!; owner.IsPlatformOwner = true; owner.AcademyId = null;
            if (!(await users.UpdateAsync(owner)).Succeeded) throw new InvalidOperationException("Currency owner fixture failed.");
        }
        async Task<string> Snapshot()
        {
            using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            return JsonSerializer.Serialize(new {
                Invoices = await db.Invoices.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(), Payments = await db.Payments.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(),
                Billing = await db.PlatformBillingInvoices.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(), Audits = await db.AuditLogs.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(),
                PlatformAudits = await db.PlatformAuditEntries.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync() });
        }
        async Task<JsonElement> Read(string email, string path, HttpStatusCode status)
        {
            var access = await LoginAsync(client, email, "Synthetic!39Ab"); client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", access);
            var before = await Snapshot(); using var response = await client.GetAsync(path); RequireFinanceStatus(response, status, "currency summary");
            if (before != await Snapshot()) throw new InvalidOperationException("Currency read changed ledger/audit.");
            if (!response.IsSuccessStatusCode) return default;
            using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            return payload.RootElement.Clone();
        }
        static void Amount(JsonElement row, string field, decimal expected) { if (row.GetProperty(field).GetDecimal() != expected) throw new InvalidOperationException("Currency exact decimal mismatch: " + field); }
        static JsonElement[] Groups(JsonElement result, params string[] scalarFields)
        {
            if (!result.GetProperty("hasMixedCurrencies").GetBoolean() || result.GetProperty("currency").ValueKind != JsonValueKind.Null || scalarFields.Any(field => result.GetProperty(field).ValueKind != JsonValueKind.Null))
                throw new InvalidOperationException("Mixed currencies emitted a scalar monetary total.");
            var groups = result.GetProperty("currencyTotals").EnumerateArray().ToArray();
            if (groups.Length != 2 || groups[0].GetProperty("currency").GetString() != "INR" || groups[1].GetProperty("currency").GetString() != "USD")
                throw new InvalidOperationException("Currency groups included excluded units or lost explicit currency.");
            return groups;
        }
        var summary = await Read("qa-admin-a@example.invalid", $"/api/academies/{academy}/finance-governance/summary", HttpStatusCode.OK);
        var finance = Groups(summary, "grossBilled", "approvedAdjustments", "collected", "outstanding", "reconciled");
        Amount(finance[0], "grossBilled", 1000.25m); Amount(finance[0], "approvedAdjustments", 100.10m); Amount(finance[0], "collected", 600.05m); Amount(finance[0], "outstanding", 300.10m); Amount(finance[0], "reconciled", 600.05m);
        Amount(finance[1], "grossBilled", 100.50m); Amount(finance[1], "approvedAdjustments", 10.20m); Amount(finance[1], "collected", 20.10m); Amount(finance[1], "outstanding", 70.20m); Amount(finance[1], "reconciled", 20.10m);
        await Read("qa-admin-b@example.invalid", $"/api/academies/{academy}/finance-governance/summary", HttpStatusCode.Forbidden);
        await Read("qa-teacher-a@example.invalid", $"/api/academies/{academy}/finance-governance/summary", HttpStatusCode.Forbidden);
        var overview = await Read("qa-currency-owner@example.invalid", "/api/platform/overview", HttpStatusCode.OK);
        var platform = Groups(overview, "totalBilled", "collectedBilling", "outstandingBilling");
        Amount(platform[0], "totalBilled", 1000.25m); Amount(platform[0], "collectedBilling", 600.05m); Amount(platform[0], "outstandingBilling", 400.20m);
        Amount(platform[1], "totalBilled", 100.50m); Amount(platform[1], "collectedBilling", 20.10m); Amount(platform[1], "outstandingBilling", 80.40m);
        await Read("qa-admin-a@example.invalid", "/api/platform/overview", HttpStatusCode.Forbidden);
        Console.WriteLine("FINANCECURRENCY SQL PASS real finance/platform HTTP mixed INR/USD exact decimal groups; no combined scalar/FX; voided/draft/void excluded; cross-tenant/role denial and unchanged SQL/audit snapshots.");
    }
}
