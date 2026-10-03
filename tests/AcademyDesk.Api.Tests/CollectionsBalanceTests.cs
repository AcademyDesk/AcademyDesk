using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace AcademyDesk.Api.Tests;

public sealed class CollectionsBalanceTests
{
    private static AcademyDeskDbContext Context() => new(new DbContextOptionsBuilder<AcademyDeskDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    [Theory]
    [InlineData(0, 0, 0, 0, 1000)]
    [InlineData(100, 400, 0, 75, 500)]
    [InlineData(200, 100, 600, 75, 100)]
    [InlineData(200, 799.99, 0, 0, 0.01)]
    [InlineData(200, 800, 0, 0, 0)]
    [InlineData(1000, 0, 0, 0, 0)]
    [InlineData(200, 900, 0, 0, 0)]
    [InlineData(0, 0, 0, 1000, 1000)]
    public async Task Queue_and_follow_up_use_remaining_balance(decimal adjusted, decimal completed, decimal reconciled, decimal voided, decimal expected)
    {
        using var db = Context(); var academy = Guid.NewGuid();
        var invoice = new Invoice { AcademyId = academy, StudentId = Guid.NewGuid(), InvoiceNumber = "QA-COL", TotalAmount = 1000m,
            AdjustedAmount = adjusted, DueDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1) };
        db.Add(invoice);
        foreach (var payment in new[] { (completed, "Completed"), (reconciled, "Reconciled"), (voided, "Voided") })
            if (payment.Item1 > 0) db.Add(new Payment { AcademyId = academy, InvoiceId = invoice.Id, Amount = payment.Item1, Status = payment.Item2 });
        // Other invoices/tenants cannot affect this invoice's balance.
        db.Add(new Payment { AcademyId = academy, InvoiceId = Guid.NewGuid(), Amount = 5000m });
        db.Add(new Payment { AcademyId = Guid.NewGuid(), InvoiceId = invoice.Id, Amount = 5000m });
        await db.SaveChangesAsync(); db.ChangeTracker.Clear();
        var controller = new FinanceGovernanceController(db);
        var result = await controller.Collections(academy, default);
        var rows = Assert.IsType<List<CollectionInvoiceSummary>>(Assert.IsType<OkObjectResult>(result.Result).Value);
        Assert.Equal(expected > 0 ? 1 : 0, rows.Count); Assert.Empty(db.ChangeTracker.Entries());
        if (expected > 0) { Assert.Equal(expected, rows[0].Balance); Assert.Equal(completed + reconciled, rows[0].PaidAmount); Assert.Equal(1000, rows[0].TotalAmount); Assert.Equal(adjusted, rows[0].AdjustedAmount); }
        var response = await controller.CreateFollowUp(academy, invoice.Id, new(null, null, null, null), default);
        if (expected > 0) Assert.IsType<OkObjectResult>(response); else Assert.IsType<ConflictObjectResult>(response);
        Assert.Equal(expected > 0 ? 1 : 0, await db.AdminWorkItems.CountAsync());
        Assert.Equal("Issued", (await db.Invoices.SingleAsync()).Status);
    }

    [Theory]
    [InlineData("Paid", -1, false)]
    [InlineData("Cancelled", -1, false)]
    [InlineData("Issued", 0, false)]
    [InlineData("Issued", 1, false)]
    [InlineData("Issued", -1, true)]
    public async Task Queue_retains_status_due_and_tenant_boundaries(string status, int days, bool visible)
    {
        using var db = Context(); var academy = Guid.NewGuid(); var today = DateOnly.FromDateTime(DateTime.UtcNow);
        db.AddRange(new Invoice { AcademyId = academy, StudentId = Guid.NewGuid(), InvoiceNumber = "QA-OWN", TotalAmount = 1000, Status = status, DueDate = today.AddDays(days) },
            new Invoice { AcademyId = Guid.NewGuid(), StudentId = Guid.NewGuid(), InvoiceNumber = "QA-FOREIGN", TotalAmount = 1000, DueDate = today.AddDays(-1) });
        await db.SaveChangesAsync(); db.ChangeTracker.Clear();
        var result = await new FinanceGovernanceController(db).Collections(academy, default);
        var rows = Assert.IsType<List<CollectionInvoiceSummary>>(Assert.IsType<OkObjectResult>(result.Result).Value);
        Assert.Equal(visible ? 1 : 0, rows.Count); Assert.Empty(db.ChangeTracker.Entries());
        if (visible) { Assert.Equal("QA-OWN", rows[0].InvoiceNumber); Assert.Equal(1, rows[0].DaysOverdue); }
    }

    [Fact]
    public async Task Summary_overdue_count_matches_positive_balance_queue_without_changing_currency_policy()
    {
        using var db = Context(); var academy = Guid.NewGuid(); var due = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1);
        db.AddRange(new Invoice { AcademyId = academy, StudentId = Guid.NewGuid(), InvoiceNumber = "QA-USD", TotalAmount = 1000, AdjustedAmount = 500, Currency = "USD", DueDate = due },
            new Invoice { AcademyId = academy, StudentId = Guid.NewGuid(), InvoiceNumber = "QA-SETTLED", TotalAmount = 1000, AdjustedAmount = 1000, DueDate = due });
        await db.SaveChangesAsync();
        var controller = new FinanceGovernanceController(db); var summary = Assert.IsType<OkObjectResult>(await controller.Summary(academy, default));
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(summary.Value)); Assert.Equal(1, json.RootElement.GetProperty("OverdueInvoices").GetInt32());
        var list = Assert.IsType<List<CollectionInvoiceSummary>>(Assert.IsType<OkObjectResult>((await controller.Collections(academy, default)).Result).Value);
        Assert.Equal("USD", Assert.Single(list).Currency); Assert.Equal(500m, list[0].Balance);
    }
}
