using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Tests;

public sealed class AdjustedPaymentTests
{
    [Theory]
    [InlineData(300, false, "PartiallyPaid")]
    [InlineData(200, true, "Paid")]
    [InlineData(199.99, true, "PartiallyPaid")]
    [InlineData(200.01, false, "PartiallyPaid")]
    public async Task Approved_adjustment_bounds_collection_and_settlement(decimal amount, bool accepted, string expectedStatus)
    {
        await using var db = CreateDb();
        var invoice = CreateInvoice(200m, "PartiallyPaid");
        db.AddRange(invoice, new Payment { AcademyId = invoice.AcademyId, InvoiceId = invoice.Id, Amount = 600m });
        await db.SaveChangesAsync();
        var result = await new PaymentsController(db).Create(invoice.AcademyId, new(invoice.Id, amount, null, null), default);
        if (accepted) Assert.IsType<CreatedResult>(result.Result);
        else Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal(expectedStatus, invoice.Status);
        Assert.Equal(accepted ? 2 : 1, await db.Payments.CountAsync());
        Assert.Equal(accepted ? 600m + amount : 600m, await db.Payments.SumAsync(x => x.Amount));
        Assert.Equal(200m, invoice.AdjustedAmount);
    }

    [Fact]
    public async Task Fully_adjusted_invoice_rejects_collection_without_changing_paid_state()
    {
        await using var db = CreateDb();
        var invoice = CreateInvoice(1000m, "Paid");
        db.Invoices.Add(invoice);
        await db.SaveChangesAsync();
        Assert.IsType<BadRequestObjectResult>((await new PaymentsController(db)
            .Create(invoice.AcademyId, new(invoice.Id, 1m, null, null), default)).Result);
        Assert.Equal("Paid", invoice.Status);
        Assert.Empty(await db.Payments.ToListAsync());
        Assert.Equal(1000m, invoice.AdjustedAmount);
    }

    [Fact]
    public async Task Adjusted_settlement_counts_reconciled_but_excludes_voided_and_rejects_one_more_cent()
    {
        await using var db = CreateDb();
        var invoice = CreateInvoice(200m, "PartiallyPaid");
        db.AddRange(invoice,
            new Payment { AcademyId = invoice.AcademyId, InvoiceId = invoice.Id, Amount = 600m, Status = "Reconciled" },
            new Payment { AcademyId = invoice.AcademyId, InvoiceId = invoice.Id, Amount = 75m, Status = "Voided" });
        await db.SaveChangesAsync();
        var controller = new PaymentsController(db);
        Assert.IsType<CreatedResult>((await controller.Create(invoice.AcademyId, new(invoice.Id, 200m, null, null), default)).Result);
        Assert.Equal("Paid", invoice.Status);
        Assert.IsType<BadRequestObjectResult>((await controller.Create(invoice.AcademyId, new(invoice.Id, 0.01m, null, null), default)).Result);
        Assert.Equal(3, await db.Payments.CountAsync());
        Assert.Equal(800m, await db.Payments.Where(x => x.Status != "Voided").SumAsync(x => x.Amount));
    }

    [Theory]
    [InlineData("PendingApproval")]
    [InlineData("Rejected")]
    public async Task Unapplied_adjustment_does_not_reduce_collectible_amount(string adjustmentStatus)
    {
        await using var db = CreateDb();
        var invoice = CreateInvoice(0m, "Issued");
        db.AddRange(invoice, new FinanceAdjustment { AcademyId = invoice.AcademyId, InvoiceId = invoice.Id,
            Type = "Discount", Amount = 200m, Reason = "Synthetic QA", Status = adjustmentStatus });
        await db.SaveChangesAsync();
        Assert.IsType<CreatedResult>((await new PaymentsController(db)
            .Create(invoice.AcademyId, new(invoice.Id, 1000m, null, null), default)).Result);
        Assert.Equal("Paid", invoice.Status);
        Assert.Equal(0m, invoice.AdjustedAmount);
        Assert.Equal(adjustmentStatus, (await db.FinanceAdjustments.SingleAsync()).Status);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Nonpositive_collection_remains_invalid(decimal amount)
    {
        await using var db = CreateDb();
        var invoice = CreateInvoice(200m, "Issued");
        db.Invoices.Add(invoice);
        await db.SaveChangesAsync();
        Assert.IsType<BadRequestObjectResult>((await new PaymentsController(db)
            .Create(invoice.AcademyId, new(invoice.Id, amount, null, null), default)).Result);
        Assert.Empty(await db.Payments.ToListAsync());
        Assert.Equal("Issued", invoice.Status);
    }

    private static Invoice CreateInvoice(decimal adjusted, string status) => new()
    { AcademyId = Guid.NewGuid(), StudentId = Guid.NewGuid(), InvoiceNumber = "QA-ADJUST", TotalAmount = 1000m, AdjustedAmount = adjusted, Status = status };

    private static AcademyDeskDbContext CreateDb() => new(new DbContextOptionsBuilder<AcademyDeskDbContext>()
        .UseInMemoryDatabase("qa-adjusted-" + Guid.NewGuid()).Options);
}
