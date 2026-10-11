using System.Text.Json;
using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Tests;

public sealed class ExpenseIntegrityTests
{
    [Fact]
    public void Summary_provides_additive_exact_decimal_for_javascript_readers()
    {
        var summary = new ExpenseSummary(Guid.NewGuid(), "Exact", 9999999999999999.99m, "INR", "General", null, new(2026, 10, 11), "Recorded");
        Assert.Equal("9999999999999999.99", summary.AmountExact);
        var json = JsonSerializer.SerializeToElement(summary);
        Assert.Equal(9999999999999999.99m, json.GetProperty("Amount").GetDecimal());
        Assert.Equal("9999999999999999.99", json.GetProperty("AmountExact").GetString());
    }
    private sealed record Fixture(DbContextOptions<AcademyDeskDbContext> Options, Guid Academy, Guid Branch, Guid ForeignBranch, Guid Expense);
    private static async Task<Fixture> Seed()
    {
        var options = new DbContextOptionsBuilder<AcademyDeskDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var db = new AcademyDeskDbContext(options);
        var academy = new Academy { Name = "Synthetic expense" }; var foreign = new Academy { Name = "Foreign sentinel" };
        var branch = new Branch { AcademyId = academy.Id, Name = "Local" }; var other = new Branch { AcademyId = foreign.Id, Name = "Foreign" };
        var expense = new Expense { AcademyId = academy.Id, Description = "Original", Amount = 10.25m, Currency = "USD", Category = "Rent", BranchId = branch.Id, ExpenseDate = new(2026, 10, 1) };
        db.AddRange(academy, foreign, branch, other, expense); await db.SaveChangesAsync();
        return new(options, academy.Id, branch.Id, other.Id, expense.Id);
    }
    private static UpdateExpenseRequest Valid(Fixture f) => new(" Updated ", 12.50m, " Supplies ", f.Branch, new(2026, 10, 11), "Recorded");
    private static async Task<string> Snapshot(AcademyDeskDbContext db) => JsonSerializer.Serialize(new {
        Expenses = await db.Expenses.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(), Branches = await db.Branches.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(), Audits = await db.AuditLogs.AsNoTracking().ToArrayAsync() });

    [Theory]
    [InlineData("foreign")][InlineData("missing")][InlineData("empty-guid")]
    [InlineData("Paid")][InlineData("Voided")][InlineData("unknown")]
    [InlineData("description")][InlineData("description-long")][InlineData("category-long")]
    [InlineData("zero")][InlineData("negative")][InlineData("precision")][InlineData("overflow")]
    public async Task Invalid_update_is_400_and_preserves_all_captured_rows(string mode)
    {
        var f = await Seed(); await using var db = new AcademyDeskDbContext(f.Options); var before = await Snapshot(db); var request = Valid(f);
        request = mode switch {
            "foreign" => request with { BranchId = f.ForeignBranch }, "missing" => request with { BranchId = Guid.NewGuid() }, "empty-guid" => request with { BranchId = Guid.Empty },
            "Paid" or "Voided" or "unknown" => request with { Status = mode }, "description" => request with { Description = " " },
            "description-long" => request with { Description = new('x', 301) }, "category-long" => request with { Category = new('x', 81) },
            "zero" => request with { Amount = 0 }, "negative" => request with { Amount = -1 }, "precision" => request with { Amount = .001m },
            "overflow" => request with { Amount = 10000000000000000m }, _ => throw new InvalidOperationException() };
        Assert.IsType<BadRequestObjectResult>(await new ExpensesController(db).Update(f.Academy, f.Expense, request, default));
        await using var fresh = new AcademyDeskDbContext(f.Options); Assert.Equal(before, await Snapshot(fresh));
        Assert.DoesNotContain(db.ChangeTracker.Entries(), x => x.State is EntityState.Modified or EntityState.Added or EntityState.Deleted);
    }

    [Theory]
    [InlineData("foreign")][InlineData("missing")][InlineData("description-long")][InlineData("category-long")]
    [InlineData("zero")][InlineData("negative")][InlineData("precision")][InlineData("overflow")]
    [InlineData("currency-long")][InlineData("currency-symbol")]
    public async Task Invalid_create_is_400_and_preserves_all_captured_rows(string mode)
    {
        var f = await Seed(); await using var db = new AcademyDeskDbContext(f.Options); var before = await Snapshot(db);
        var request = new CreateExpenseRequest(" New ", 1.25m, " usd ", " Rent ", f.Branch, new(2026, 10, 11));
        request = mode switch { "foreign" => request with { BranchId = f.ForeignBranch }, "missing" => request with { BranchId = Guid.NewGuid() },
            "description-long" => request with { Description = new('x', 301) }, "category-long" => request with { Category = new('x', 81) },
            "zero" => request with { Amount = 0 }, "negative" => request with { Amount = -1 }, "precision" => request with { Amount = .001m },
            "overflow" => request with { Amount = 10000000000000000m }, "currency-long" => request with { Currency = "USDX" }, "currency-symbol" => request with { Currency = "12$" }, _ => throw new InvalidOperationException() };
        Assert.IsType<BadRequestObjectResult>((await new ExpensesController(db).Create(f.Academy, request, default)).Result);
        await using var fresh = new AcademyDeskDbContext(f.Options); Assert.Equal(before, await Snapshot(fresh));
    }

    [Theory][InlineData(null)][InlineData("")][InlineData(" ")][InlineData(" Recorded ")]
    public async Task Local_update_preserves_currency_and_optional_status(string? status)
    {
        var f = await Seed(); await using var db = new AcademyDeskDbContext(f.Options);
        Assert.IsType<OkResult>(await new ExpensesController(db).Update(f.Academy, f.Expense, Valid(f) with { Status = status, BranchId = null, Category = " " }, default));
        await using var fresh = new AcademyDeskDbContext(f.Options); var row = await fresh.Expenses.SingleAsync();
        Assert.Equal("USD", row.Currency); Assert.Equal("Recorded", row.Status); Assert.Equal("Updated", row.Description);
        Assert.Equal("General", row.Category); Assert.Null(row.BranchId); Assert.Equal(12.50m, row.Amount);
    }

    [Fact]
    public async Task Omitted_status_preserves_legacy_stored_status_without_authorizing_new_status()
    {
        var f = await Seed(); await using var db = new AcademyDeskDbContext(f.Options); var row = await db.Expenses.SingleAsync(); row.Status = "Legacy review"; await db.SaveChangesAsync();
        Assert.IsType<OkResult>(await new ExpensesController(db).Update(f.Academy, f.Expense, Valid(f) with { Status = null }, default));
        Assert.Equal("Legacy review", (await db.Expenses.AsNoTracking().SingleAsync()).Status);
    }

    [Theory][InlineData(false)][InlineData(true)]
    public async Task Foreign_or_missing_expense_is_404_without_write(bool missing)
    {
        var f = await Seed(); await using var db = new AcademyDeskDbContext(f.Options); var before = await Snapshot(db);
        Assert.IsType<NotFoundResult>(await new ExpensesController(db).Update(missing ? f.Academy : Guid.NewGuid(), missing ? Guid.NewGuid() : f.Expense, Valid(f), default));
        await using var fresh = new AcademyDeskDbContext(f.Options); Assert.Equal(before, await Snapshot(fresh));
    }

    [Theory][InlineData(false)][InlineData(true)]
    public async Task Create_keeps_boundary_values_and_optional_defaults(bool localBranch)
    {
        var f = await Seed(); await using var db = new AcademyDeskDbContext(f.Options);
        var request = new CreateExpenseRequest(new('x',300), 9999999999999999.99m, " usd ", new('c',80), localBranch ? f.Branch : null, new(2026,10,11));
        var result = Assert.IsType<CreatedResult>((await new ExpensesController(db).Create(f.Academy,request,default)).Result);
        var summary = Assert.IsType<ExpenseSummary>(result.Value); Assert.Equal("USD",summary.Currency); Assert.Equal("Recorded",summary.Status); Assert.Equal(request.Amount,summary.Amount);
    }
}
