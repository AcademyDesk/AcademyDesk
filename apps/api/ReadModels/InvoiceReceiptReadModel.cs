using AcademyDesk.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.ReadModels;

// Callers supply their already-authorized invoice IDs. Unlike units never offset.
internal static class InvoiceReceiptReadModel
{
    internal static string CurrencyKey(string currency) => currency.Trim().ToUpperInvariant();

    internal static async Task<Dictionary<(Guid InvoiceId, string Currency), decimal>> PaidAsync(
        AcademyDeskDbContext db, Guid academyId, IEnumerable<Guid> invoiceIds, CancellationToken token)
    {
        var ids = invoiceIds.Distinct().ToArray();
        if (ids.Length == 0) return [];
        var rows = await db.Payments.AsNoTracking()
            .Where(p => p.AcademyId == academyId && ids.Contains(p.InvoiceId)
                && (p.Status == "Completed" || p.Status == "Reconciled"))
            .Select(p => new { p.InvoiceId, p.Currency, p.Amount }).ToListAsync(token);
        return rows.GroupBy(p => (p.InvoiceId, Currency: CurrencyKey(p.Currency)))
            .ToDictionary(group => group.Key, group => group.Sum(p => p.Amount));
    }
}
