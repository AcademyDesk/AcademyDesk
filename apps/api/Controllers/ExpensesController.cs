using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Controllers;

[ApiController]
[Route("api/academies/{academyId:guid}/expenses")]
public sealed class ExpensesController(AcademyDeskDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ExpenseSummary>>> List(Guid academyId, CancellationToken cancellationToken) => Ok(await dbContext.Expenses.AsNoTracking().Where(x => x.AcademyId == academyId).OrderByDescending(x => x.ExpenseDate).Select(x => new ExpenseSummary(x.Id, x.Description, x.Amount, x.Currency, x.Category, x.BranchId, x.ExpenseDate, x.Status)).ToListAsync(cancellationToken));

    [HttpPost]
    public async Task<ActionResult<ExpenseSummary>> Create(Guid academyId, CreateExpenseRequest request, CancellationToken cancellationToken)
    {
        if (!await dbContext.Academies.AnyAsync(x => x.Id == academyId, cancellationToken)) return NotFound();
        if (string.IsNullOrWhiteSpace(request.Description) || request.Amount <= 0) return BadRequest(new { message = "Description and a positive amount are required." });
        if (request.BranchId.HasValue && !await dbContext.Branches.AnyAsync(x => x.Id == request.BranchId && x.AcademyId == academyId, cancellationToken)) return BadRequest(new { message = "The selected branch does not belong to this academy." });
        var expense = new Expense { AcademyId = academyId, Description = request.Description.Trim(), Amount = request.Amount, Currency = string.IsNullOrWhiteSpace(request.Currency) ? "INR" : request.Currency.Trim().ToUpperInvariant(), Category = string.IsNullOrWhiteSpace(request.Category) ? "General" : request.Category.Trim(), BranchId = request.BranchId, ExpenseDate = request.ExpenseDate ?? DateOnly.FromDateTime(DateTime.UtcNow) };
        dbContext.Expenses.Add(expense); await dbContext.SaveChangesAsync(cancellationToken);
        return Created($"/api/academies/{academyId}/expenses/{expense.Id}", new ExpenseSummary(expense.Id, expense.Description, expense.Amount, expense.Currency, expense.Category, expense.BranchId, expense.ExpenseDate, expense.Status));
    }
}

public sealed record CreateExpenseRequest(string Description, decimal Amount, string? Currency, string? Category, Guid? BranchId, DateOnly? ExpenseDate);
public sealed record ExpenseSummary(Guid Id, string Description, decimal Amount, string Currency, string Category, Guid? BranchId, DateOnly ExpenseDate, string Status);
