using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.ReadModels;

internal static class InvoiceReadModel
{
    internal static IQueryable<InvoiceSummary> Query(AcademyDeskDbContext db, Guid academyId, Guid? invoiceId = null) =>
        db.Invoices.AsNoTracking()
            .Where(invoice => invoice.AcademyId == academyId && (invoiceId == null || invoice.Id == invoiceId))
            .OrderByDescending(invoice => invoice.IssuedDate)
            .Select(invoice => new InvoiceSummary(invoice.Id, invoice.InvoiceNumber, invoice.StudentId, invoice.FeePlanId,
                invoice.TotalAmount, invoice.AdjustedAmount,
                db.Payments.Where(payment => payment.AcademyId == academyId && payment.InvoiceId == invoice.Id &&
                    (payment.Status == "Completed" || payment.Status == "Reconciled") &&
                    payment.Currency.Trim().ToUpper() == invoice.Currency.Trim().ToUpper())
                    .Sum(payment => (decimal?)payment.Amount) ?? 0,
                invoice.Currency, invoice.IssuedDate, invoice.DueDate, invoice.Status));
}
