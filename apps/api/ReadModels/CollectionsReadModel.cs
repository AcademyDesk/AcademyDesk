using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.ReadModels;

internal static class CollectionsReadModel
{
    internal static async Task<List<CollectionInvoiceSummary>> LoadAsync(AcademyDeskDbContext db, Guid academy, DateOnly today,
        CancellationToken token, int? limit = null)
    {
        var eligible = db.Invoices.AsNoTracking().Where(x => x.AcademyId == academy &&
            x.Status.ToLower() != "paid" && x.Status.ToLower() != "cancelled" && x.DueDate < today);
        var ordered = InvoiceReadModel.WithCollected(db, eligible)
            .Where(x => x.PaidAmount < x.TotalAmount - x.AdjustedAmount)
            .OrderBy(x => x.DueDate).ThenBy(x => x.Id);
        var rows = await (limit is { } size ? ordered.Take(size) : ordered).ToListAsync(token);
        return rows.Select(x => new CollectionInvoiceSummary(x.Id, x.InvoiceNumber, x.StudentId, x.TotalAmount, x.AdjustedAmount,
            x.PaidAmount, x.Currency, x.DueDate, x.Status, today.DayNumber - x.DueDate.DayNumber)).ToList();
    }
}
