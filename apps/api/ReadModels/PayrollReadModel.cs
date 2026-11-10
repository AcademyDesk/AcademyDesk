using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.ReadModels;

internal static class PayrollReadModel
{
    internal static IOrderedQueryable<PayrollPayout> Query(AcademyDeskDbContext db, Guid academy) =>
        db.PayrollPayouts.AsNoTracking().Where(x => x.AcademyId == academy)
            .OrderByDescending(x => x.PaidAtUtc).ThenBy(x => x.Id);
}
