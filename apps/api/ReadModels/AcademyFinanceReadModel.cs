using AcademyDesk.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.ReadModels;

// One tenant-scoped read contract for academy finance and dashboard summaries.
// Never convert units, net overpayments against other invoices, or write ledger rows.
internal static class AcademyFinanceReadModel
{
    internal static async Task<FinanceCurrencyTotals[]> LoadAsync(AcademyDeskDbContext db, Guid academyId, CancellationToken token)
    {
        var invoices = await db.Invoices.AsNoTracking().Where(x => x.AcademyId == academyId)
            .Select(x => new { x.Id, x.Currency, x.TotalAmount, x.AdjustedAmount, x.Status }).ToListAsync(token);
        var payments = await db.Payments.AsNoTracking().Where(x => x.AcademyId == academyId
            && (x.Status == "Completed" || x.Status == "Reconciled"))
            .Select(x => new { x.InvoiceId, x.Currency, x.Amount, x.Status }).ToListAsync(token);
        var paid = payments.GroupBy(x => (x.InvoiceId, Currency: CurrencyKey(x.Currency)))
            .ToDictionary(x => x.Key, x => x.Sum(p => p.Amount));
        var byCurrency = invoices.Select(x => CurrencyKey(x.Currency)).Concat(payments.Select(x => CurrencyKey(x.Currency)))
            .Distinct(StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal).Select(currency =>
            {
                var bills = invoices.Where(x => CurrencyKey(x.Currency) == currency).ToArray();
                var receipts = payments.Where(x => CurrencyKey(x.Currency) == currency).ToArray();
                return new FinanceCurrencyTotals(currency, bills.Sum(x => x.TotalAmount), bills.Sum(x => x.AdjustedAmount),
                    receipts.Sum(x => x.Amount),
                    bills.Where(x => !string.Equals(x.Status, "Cancelled", StringComparison.OrdinalIgnoreCase))
                        .Sum(x => Math.Max(0m, x.TotalAmount - x.AdjustedAmount - paid.GetValueOrDefault((x.Id, currency)))),
                    receipts.Where(x => x.Status == "Reconciled").Sum(x => x.Amount));
            }).ToArray();
        return byCurrency;
    }

    private static string CurrencyKey(string currency) => currency.Trim().ToUpperInvariant();
}

public sealed record FinanceCurrencyTotals(string Currency, decimal GrossBilled, decimal ApprovedAdjustments,
    decimal Collected, decimal Outstanding, decimal Reconciled);
