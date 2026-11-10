using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.ReadModels;

internal static class PaymentReadModel
{
    internal static IOrderedQueryable<Payment> Query(AcademyDeskDbContext db, Guid academy, Guid? invoiceId) =>
        db.Payments.AsNoTracking().Where(x => x.AcademyId == academy && (invoiceId == null || x.InvoiceId == invoiceId))
            .OrderByDescending(x => x.PaidAtUtc).ThenBy(x => x.Id);
}
