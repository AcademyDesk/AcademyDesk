using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.ReadModels;

internal static class ExpenseReadModel
{
    internal static IOrderedQueryable<Expense> Query(AcademyDeskDbContext db, Guid academy) =>
        db.Expenses.AsNoTracking().Where(x => x.AcademyId == academy)
            .OrderByDescending(x => x.ExpenseDate).ThenBy(x => x.Id);
}
