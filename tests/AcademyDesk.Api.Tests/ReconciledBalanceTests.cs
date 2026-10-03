using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Tests;

public sealed class ReconciledBalanceTests
{
    [Theory]
    [InlineData("Completed", true)]
    [InlineData("Reconciled", true)]
    [InlineData("Voided", false)]
    public async Task Collection_counts_completed_and_reconciled_but_not_voided(string status, bool reject)
    {
        await using var db = CreateDb();
        var invoice = new Invoice { AcademyId = Guid.NewGuid(), StudentId = Guid.NewGuid(), InvoiceNumber = "QA-RECON", TotalAmount = 1000m };
        db.AddRange(invoice, new Payment { AcademyId = invoice.AcademyId, InvoiceId = invoice.Id, Amount = 600m, Status = status });
        await db.SaveChangesAsync();
        var result = await new PaymentsController(db).Create(invoice.AcademyId, new(invoice.Id, 500m, null, null), default);
        if (reject)
        {
            Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Single(await db.Payments.ToListAsync());
            Assert.Equal("Issued", invoice.Status);
        }
        else Assert.IsType<CreatedResult>(result.Result);
    }

    [Fact]
    public async Task Mixed_collected_statuses_allow_exact_settlement_and_reject_one_more()
    {
        await using var db = CreateDb();
        var invoice = new Invoice { AcademyId = Guid.NewGuid(), StudentId = Guid.NewGuid(), InvoiceNumber = "QA-MIXED", TotalAmount = 1000m };
        db.Invoices.Add(invoice);
        foreach (var (amount, status) in new[] { (400m, "Reconciled"), (200m, "Completed"), (100m, "Voided") })
            db.Payments.Add(new Payment { AcademyId = invoice.AcademyId, InvoiceId = invoice.Id, Amount = amount, Status = status });
        await db.SaveChangesAsync();
        var controller = new PaymentsController(db);
        Assert.IsType<CreatedResult>((await controller.Create(invoice.AcademyId, new(invoice.Id, 400m, null, null), default)).Result);
        Assert.Equal("Paid", invoice.Status);
        Assert.IsType<BadRequestObjectResult>((await controller.Create(invoice.AcademyId, new(invoice.Id, 1m, null, null), default)).Result);
        Assert.Equal(4, await db.Payments.CountAsync());
    }

    [Theory]
    [InlineData("Completed", 0)]
    [InlineData("Reconciled", 0)]
    [InlineData("Voided", 1)]
    public async Task Reminders_do_not_request_money_already_collected(string status, int expected)
    {
        await using var db = CreateDb();
        var invoice = new Invoice { AcademyId = Guid.NewGuid(), StudentId = Guid.NewGuid(), InvoiceNumber = "QA-REMINDER", TotalAmount = 1000m };
        db.AddRange(invoice, new Payment { AcademyId = invoice.AcademyId, InvoiceId = invoice.Id, Amount = 1000m, Status = status });
        await db.SaveChangesAsync();
        var result = await new FeeRemindersController(db).Queue(invoice.AcademyId, new(invoice.Id, "InApp"), default);
        var body = Assert.IsType<FeeReminderResult>(Assert.IsType<OkObjectResult>(result.Result).Value);
        Assert.Equal(expected, body.QueuedCount);
        Assert.Equal(expected, await db.Notifications.CountAsync());
    }

    private static AcademyDeskDbContext CreateDb() => new(new DbContextOptionsBuilder<AcademyDeskDbContext>()
        .UseInMemoryDatabase("qa-reconciled-" + Guid.NewGuid()).Options);
}
