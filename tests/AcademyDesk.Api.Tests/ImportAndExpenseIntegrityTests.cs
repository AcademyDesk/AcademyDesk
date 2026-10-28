using System.Text.Json;
using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Tests;

public sealed class ImportAndExpenseIntegrityTests
{
    [Theory]
    [InlineData("same@example.invalid", "same@example.invalid")]
    [InlineData("Same@example.invalid", " SAME@example.invalid ")]
    [InlineData("I@example.invalid", "i@example.invalid")]
    public async Task Duplicate_import_emails_are_row_specific_and_all_or_nothing(string first, string second)
    {
        await using var db = CreateDb(); var academy = Guid.NewGuid(); var controller = new StudentImportsController(db);
        var rows = new[] { Row(first), Row(second), Row("distinct@example.invalid") };
        var request = new StudentImportRequest(rows);
        var validation = Assert.IsType<StudentImportValidation>(Assert.IsType<OkObjectResult>(controller.Validate(academy, request)).Value);
        Assert.Equal(3, validation.RowCount); Assert.Contains(validation.Errors, x => JsonSerializer.SerializeToElement(x).GetProperty("row").GetInt32() == 2);
        Assert.IsType<BadRequestObjectResult>(await controller.Import(academy, request, default));
        Assert.Empty(await db.Students.ToListAsync());
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", " ")]
    [InlineData("first@example.invalid", "second@example.invalid")]
    public async Task Optional_or_distinct_import_emails_preserve_rows(string? first, string? second)
    {
        await using var db = CreateDb(); var academy = Guid.NewGuid();
        Assert.IsType<OkObjectResult>(await new StudentImportsController(db).Import(academy, new(new[] { Row(first), Row(second) }), default));
        var rows = await db.Students.ToListAsync(); Assert.Equal(2, rows.Count); Assert.All(rows, x => Assert.Equal(academy, x.AcademyId));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Existing_normalized_identity_blocks_same_academy_only(bool foreign)
    {
        await using var db = CreateDb(); var academy = Guid.NewGuid();
        db.Students.Add(new Student { AcademyId = foreign ? Guid.NewGuid() : academy, FirstName = "Existing", LastName = "Synthetic", Email = " Existing@example.invalid " });
        await db.SaveChangesAsync();
        var result = await new StudentImportsController(db).Import(academy, new(new[] { Row("EXISTING@example.invalid") }), default);
        if (foreign) Assert.IsType<OkObjectResult>(result); else Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal(foreign ? 2 : 1, await db.Students.CountAsync());
    }

    [Fact]
    public async Task Null_import_row_is_validation_error_without_partial_write()
    {
        await using var db = CreateDb(); var controller = new StudentImportsController(db); var academy = Guid.NewGuid();
        var rows = new StudentImportRow[] { Row("valid@example.invalid"), null! };
        var validation = Assert.IsType<StudentImportValidation>(Assert.IsType<OkObjectResult>(controller.Validate(academy, new(rows))).Value);
        Assert.Contains(validation.Errors, x => JsonSerializer.SerializeToElement(x).GetProperty("row").GetInt32() == 2);
        Assert.IsType<BadRequestObjectResult>(await controller.Import(academy, new(rows), default)); Assert.Empty(await db.Students.ToListAsync());
    }

    [Theory]
    [InlineData("foreign")]
    [InlineData("missing")]
    public async Task Expense_foreign_or_missing_branch_is_rejected_without_write(string kind)
    {
        await using var db = CreateDb(); var academy = Guid.NewGuid(); var item = Expense(academy); var branch = new Branch { AcademyId = Guid.NewGuid(), Name = "Foreign synthetic" };
        db.AddRange(item, branch); await db.SaveChangesAsync(); var before = JsonSerializer.Serialize(await db.Expenses.AsNoTracking().ToListAsync());
        var result = await new ExpensesController(db).Update(academy, item.Id, new("Changed", 42m, "Changed", kind == "foreign" ? branch.Id : Guid.NewGuid(), item.ExpenseDate, "Recorded"), default);
        Assert.IsType<BadRequestObjectResult>(result); db.ChangeTracker.Clear(); Assert.Equal(before, JsonSerializer.Serialize(await db.Expenses.AsNoTracking().ToListAsync()));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("Recorded")]
    [InlineData(" recorded ")]
    public async Task Valid_expense_update_keeps_currency_and_known_status(string? status)
    {
        await using var db = CreateDb(); var academy = Guid.NewGuid(); var item = Expense(academy); var branch = new Branch { AcademyId = academy, Name = "Local synthetic", IsActive = false };
        db.AddRange(item, branch); await db.SaveChangesAsync();
        Assert.IsType<OkResult>(await new ExpensesController(db).Update(academy, item.Id, new(" Updated ", 42m, " Updated category ", branch.Id, new(2026, 10, 28), status), default));
        db.ChangeTracker.Clear(); var saved = await db.Expenses.SingleAsync();
        Assert.Equal(branch.Id, saved.BranchId); Assert.Equal("USD", saved.Currency); Assert.Equal("Recorded", saved.Status);
        Assert.Equal("Updated", saved.Description); Assert.Equal(42m, saved.Amount); Assert.Equal("Updated category", saved.Category);
    }

    [Theory]
    [InlineData("Paid")]
    [InlineData("Approved")]
    [InlineData("MadeUpStatus")]
    public async Task Unsupported_expense_status_does_not_fabricate_lifecycle(string status)
    {
        await using var db = CreateDb(); var item = Expense(Guid.NewGuid()); db.Add(item); await db.SaveChangesAsync();
        var before = JsonSerializer.Serialize(await db.Expenses.AsNoTracking().ToListAsync());
        Assert.IsType<BadRequestObjectResult>(await new ExpensesController(db).Update(item.AcademyId, item.Id, new("Changed", 99m, null, null, item.ExpenseDate, status), default));
        db.ChangeTracker.Clear(); Assert.Equal(before, JsonSerializer.Serialize(await db.Expenses.AsNoTracking().ToListAsync()));
    }

    [Fact]
    public async Task Null_expense_branch_clears_assignment_and_foreign_expense_remains_untouched()
    {
        await using var db = CreateDb(); var item = Expense(Guid.NewGuid()); item.BranchId = Guid.NewGuid(); db.Add(item); await db.SaveChangesAsync();
        var controller = new ExpensesController(db); var request = new UpdateExpenseRequest("Changed", 99m, null, null, item.ExpenseDate, null);
        Assert.IsType<NotFoundResult>(await controller.Update(Guid.NewGuid(), item.Id, request, default));
        Assert.IsType<OkResult>(await controller.Update(item.AcademyId, item.Id, request, default)); db.ChangeTracker.Clear();
        Assert.Null((await db.Expenses.SingleAsync()).BranchId);
    }

    private static StudentImportRow Row(string? email) => new("Synthetic", "Learner", null, email, null, null);
    private static Expense Expense(Guid academy) => new() { AcademyId = academy, Description = "Synthetic original", Amount = 12m, Currency = "USD" };
    private static AcademyDeskDbContext CreateDb() => new(new DbContextOptionsBuilder<AcademyDeskDbContext>().UseInMemoryDatabase("qa-import-expense-" + Guid.NewGuid()).Options);
}
