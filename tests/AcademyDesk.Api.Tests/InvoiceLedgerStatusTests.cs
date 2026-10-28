using System.Text.Json;
using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Tests;

public sealed class InvoiceLedgerStatusTests
{
    [Theory]
    [InlineData(0, 0, "Paid", false, "Paid")]
    [InlineData(0, 0, "paid", false, "Paid")]
    [InlineData(400, 0, "Paid", false, "Paid")]
    [InlineData(1000, 0, "Paid", true, "Paid")]
    [InlineData(800, 200, "paid", true, "Paid")]
    [InlineData(0, 1000, "Paid", true, "Paid")]
    [InlineData(600, 200, "partiallypaid", true, "PartiallyPaid")]
    [InlineData(0, 0, "PartiallyPaid", false, "PartiallyPaid")]
    [InlineData(1000, 0, "PartiallyPaid", false, "PartiallyPaid")]
    [InlineData(0, 0, "Issued", true, "Issued")]
    [InlineData(400, 0, "Issued", false, "Issued")]
    [InlineData(1000, 0, "Issued", false, "Issued")]
    [InlineData(0, 0, "Overdue", false, "Overdue")]
    [InlineData(0, 0, "Cancelled", true, "Cancelled")]
    [InlineData(1000, 0, "cancelled", true, "Cancelled")]
    [InlineData(0, 0, "  cancelled  ", true, "Cancelled")]
    public async Task Manual_status_agrees_with_ledger_or_explicit_cancellation(decimal paid, decimal adjusted,
        string requested, bool accepted, string canonical)
    {
        await using var db = CreateDb(); var invoice = Fixture(adjusted);
        db.AddRange(invoice, new Payment { AcademyId = invoice.AcademyId, InvoiceId = invoice.Id, Amount = paid, Status = "Reconciled" });
        await db.SaveChangesAsync(); var before = await Snapshot(db);
        var result = await new InvoicesController(db).UpdateStatus(invoice.AcademyId, invoice.Id, new(requested), default);
        if (accepted) Assert.IsType<OkResult>(result); else Assert.IsType<BadRequestObjectResult>(result);
        db.ChangeTracker.Clear();
        if (!accepted) Assert.Equal(before, await Snapshot(db));
        else
        {
            var stored = await db.Invoices.SingleAsync(); Assert.Equal(canonical, stored.Status);
            Assert.Equal(adjusted, stored.AdjustedAmount); Assert.Equal(1000m, stored.TotalAmount);
            Assert.Equal(paid, (await db.Payments.SingleAsync()).Amount);
        }
    }

    [Theory]
    [InlineData(0, "Overdue", true)]
    [InlineData(0, "Issued", false)]
    [InlineData(400, "Overdue", false)]
    [InlineData(400, "PartiallyPaid", true)]
    [InlineData(1000, "Overdue", false)]
    [InlineData(1000, "Paid", true)]
    public async Task Past_due_status_still_respects_payment_balance(decimal paid, string requested, bool accepted)
    {
        await using var db = CreateDb(); var invoice = Fixture(0m); invoice.DueDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-7);
        db.AddRange(invoice, new Payment { AcademyId = invoice.AcademyId, InvoiceId = invoice.Id, Amount = paid });
        await db.SaveChangesAsync(); var before = await Snapshot(db);
        var result = await new InvoicesController(db).UpdateStatus(invoice.AcademyId, invoice.Id, new(requested), default);
        if (accepted) Assert.IsType<OkResult>(result); else Assert.IsType<BadRequestObjectResult>(result);
        db.ChangeTracker.Clear(); if (!accepted) Assert.Equal(before, await Snapshot(db));
    }

    [Theory]
    [InlineData("Voided", false)]
    [InlineData("Pending", false)]
    [InlineData("Completed", true)]
    [InlineData("Reconciled", true)]
    public async Task Only_collected_payment_states_count_in_status_and_invoice_list(string status, bool collected)
    {
        await using var db = CreateDb(); var invoice = Fixture(0m);
        db.AddRange(invoice, new Payment { AcademyId = invoice.AcademyId, InvoiceId = invoice.Id, Amount = 1000m, Status = status });
        await db.SaveChangesAsync();
        var controller = new InvoicesController(db);
        var rows = Assert.IsAssignableFrom<IReadOnlyList<InvoiceSummary>>(Assert.IsType<OkObjectResult>((await controller.List(invoice.AcademyId, default)).Result).Value);
        Assert.Equal(collected ? 1000m : 0m, Assert.Single(rows).PaidAmount);
        var result = await controller.UpdateStatus(invoice.AcademyId, invoice.Id, new("Paid"), default);
        if (collected) Assert.IsType<OkResult>(result); else Assert.IsType<BadRequestObjectResult>(result);
    }

    [Theory]
    [InlineData("Cancelled")]
    [InlineData("cancelled")]
    public async Task Cancelled_invoice_cannot_accept_new_collection(string status)
    {
        await using var db = CreateDb(); var invoice = Fixture(0m); invoice.Status = status;
        db.Invoices.Add(invoice); await db.SaveChangesAsync(); var before = await Snapshot(db);
        Assert.IsType<BadRequestObjectResult>((await new PaymentsController(db).Create(invoice.AcademyId, new(invoice.Id, 1m, null, null), default)).Result);
        db.ChangeTracker.Clear(); Assert.Equal(before, await Snapshot(db));
    }

    [Theory]
    [InlineData("")]
    [InlineData("NotAStatus")]
    public async Task Invalid_status_does_not_change_invoice(string status)
    {
        await using var db = CreateDb(); var invoice = Fixture(0m); db.Invoices.Add(invoice); await db.SaveChangesAsync(); var before = await Snapshot(db);
        Assert.Equal(400, Assert.IsAssignableFrom<IStatusCodeActionResult>(await new InvoicesController(db).UpdateStatus(invoice.AcademyId, invoice.Id, new(status), default)).StatusCode);
        db.ChangeTracker.Clear(); Assert.Equal(before, await Snapshot(db));
    }

    [Fact]
    public async Task Foreign_invoice_and_payment_cannot_settle_local_invoice()
    {
        await using var db = CreateDb(); var invoice = Fixture(0m);
        db.AddRange(invoice, new Payment { AcademyId = Guid.NewGuid(), InvoiceId = invoice.Id, Amount = 1000m }); await db.SaveChangesAsync();
        var controller = new InvoicesController(db); var before = await Snapshot(db);
        Assert.IsType<NotFoundResult>(await controller.UpdateStatus(Guid.NewGuid(), invoice.Id, new("Paid"), default));
        Assert.IsType<BadRequestObjectResult>(await controller.UpdateStatus(invoice.AcademyId, invoice.Id, new("Paid"), default));
        db.ChangeTracker.Clear(); Assert.Equal(before, await Snapshot(db));
    }

    [Theory]
    [InlineData("Cancelled")]
    [InlineData("cancelled")]
    public async Task Adjustment_approval_does_not_reopen_cancelled_invoice(string status)
    {
        await using var db = CreateDb(); var invoice = Fixture(0m); invoice.Status = status;
        var adjustment = new FinanceAdjustment { AcademyId = invoice.AcademyId, InvoiceId = invoice.Id, Type = "Discount", Amount = 100m, Reason = "Synthetic fixture" };
        db.AddRange(invoice, adjustment); await db.SaveChangesAsync();
        Assert.IsType<OkObjectResult>((await new FinanceAdjustmentsController(db).Decide(invoice.AcademyId, adjustment.Id, new(true, null), default)).Result);
        db.ChangeTracker.Clear(); Assert.Equal(status, (await db.Invoices.SingleAsync()).Status);
        Assert.Equal(100m, (await db.Invoices.SingleAsync()).AdjustedAmount);
    }

    private static Invoice Fixture(decimal adjusted) => new() { AcademyId = Guid.NewGuid(), StudentId = Guid.NewGuid(), InvoiceNumber = "QA-LEDGER-STATUS",
        TotalAmount = 1000m, AdjustedAmount = adjusted, DueDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(7) };
    private static async Task<string> Snapshot(AcademyDeskDbContext db) => JsonSerializer.Serialize(new { Invoices = await db.Invoices.AsNoTracking().ToListAsync(), Payments = await db.Payments.AsNoTracking().ToListAsync() });
    private static AcademyDeskDbContext CreateDb() => new(new DbContextOptionsBuilder<AcademyDeskDbContext>().UseInMemoryDatabase("qa-invoice-status-" + Guid.NewGuid()).Options);
}
