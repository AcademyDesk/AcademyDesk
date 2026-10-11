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
        var error = ValidateFields(request.Description, request.Amount, request.Category);
        if (error is not null) return BadRequest(new { message = error });
        var currency = string.IsNullOrWhiteSpace(request.Currency) ? "INR" : request.Currency.Trim().ToUpperInvariant();
        if (currency.Length != 3 || currency.Any(c => c < 'A' || c > 'Z'))
            return BadRequest(new { message = "Currency must be a three-letter currency code." });
        if (request.BranchId.HasValue && !await dbContext.Branches.AnyAsync(x => x.Id == request.BranchId && x.AcademyId == academyId, cancellationToken)) return BadRequest(new { message = "The selected branch does not belong to this academy." });
        var expense = new Expense { AcademyId = academyId, Description = request.Description.Trim(), Amount = request.Amount, Currency = currency, Category = NormalizeCategory(request.Category), BranchId = request.BranchId, ExpenseDate = request.ExpenseDate ?? DateOnly.FromDateTime(DateTime.UtcNow) };
        dbContext.Expenses.Add(expense); await dbContext.SaveChangesAsync(cancellationToken);
        return Created($"/api/academies/{academyId}/expenses/{expense.Id}", new ExpenseSummary(expense.Id, expense.Description, expense.Amount, expense.Currency, expense.Category, expense.BranchId, expense.ExpenseDate, expense.Status));
    }
    [HttpPut("{expenseId:guid}")]
    public async Task<ActionResult> Update(Guid academyId, Guid expenseId, UpdateExpenseRequest request, CancellationToken token)
    {
        // Serialize replacement reads so EF's changed-column updates cannot combine
        // fields from two concurrent requests. Keep the lock through the host audit.
        await using var ownedTransaction = dbContext.Database.IsSqlServer() && dbContext.Database.CurrentTransaction is null
            ? await dbContext.Database.BeginTransactionAsync(token) : null;
        var query = dbContext.Database.IsSqlServer()
            ? dbContext.Expenses.FromSqlInterpolated($"SELECT * FROM [Expenses] WITH (UPDLOCK, HOLDLOCK) WHERE [AcademyId] = {academyId} AND [Id] = {expenseId}")
            : dbContext.Expenses.Where(x => x.Id == expenseId && x.AcademyId == academyId);
        var expense = await query.SingleOrDefaultAsync(token);
        if (expense is null) return NotFound();
        var error = ValidateFields(request.Description, request.Amount, request.Category);
        if (error is not null) return BadRequest(new { message = error });
        if (request.BranchId.HasValue && !await dbContext.Branches.AnyAsync(x => x.Id == request.BranchId && x.AcademyId == academyId, token))
            return BadRequest(new { message = "The selected branch does not belong to this academy." });
        // Recorded is the existing expense lifecycle. Omission preserves historical data;
        // callers cannot invent payment/void transitions through this replacement endpoint.
        if (!string.IsNullOrWhiteSpace(request.Status) && request.Status.Trim() != "Recorded")
            return BadRequest(new { message = "Expense status must be Recorded." });

        expense.Description = request.Description.Trim();
        expense.Amount = request.Amount;
        expense.Category = NormalizeCategory(request.Category);
        expense.BranchId = request.BranchId;
        expense.ExpenseDate = request.ExpenseDate;
        if (!string.IsNullOrWhiteSpace(request.Status)) expense.Status = request.Status.Trim();
        // Currency is intentionally immutable in the existing update contract.
        await dbContext.SaveChangesAsync(token);
        if (ownedTransaction is not null) await ownedTransaction.CommitAsync(token);
        return Ok();
    }

    private static string NormalizeCategory(string? category) => string.IsNullOrWhiteSpace(category) ? "General" : category.Trim();

    private static string? ValidateFields(string? description, decimal amount, string? category)
    {
        if (string.IsNullOrWhiteSpace(description) || amount <= 0)
            return "Description and a positive amount are required.";
        if (description.Trim().Length > 300) return "Description must not exceed 300 characters.";
        if (NormalizeCategory(category).Length > 80) return "Category must not exceed 80 characters.";
        if (amount > 9999999999999999.99m || decimal.Round(amount, 2) != amount)
            return "Amount must fit within 16 whole digits and two decimal places.";
        return null;
    }
}

public sealed record CreateExpenseRequest(string Description, decimal Amount, string? Currency, string? Category, Guid? BranchId, DateOnly? ExpenseDate);
public sealed record ExpenseSummary(Guid Id, string Description, decimal Amount, string Currency, string Category, Guid? BranchId, DateOnly ExpenseDate, string Status)
{
    // Additive read field preserves the exact decimal for JavaScript consumers.
    public string AmountExact => Amount.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
}
public sealed record UpdateExpenseRequest(string Description, decimal Amount, string? Category, Guid? BranchId, DateOnly ExpenseDate, string? Status);
