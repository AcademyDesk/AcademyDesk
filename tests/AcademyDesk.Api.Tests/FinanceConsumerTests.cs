using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Tests;

public sealed class FinanceConsumerTests
{
    [Theory]
    [InlineData(200, 600, "Completed", 200)]
    [InlineData(200, 600, "Reconciled", 200)]
    [InlineData(200, 800, "Reconciled", 0)]
    [InlineData(1000, 0, "Completed", 0)]
    [InlineData(0, 600, "Completed", 400)]
    [InlineData(200, 600, "Voided", 800)]
    public async Task Reminders_use_applied_adjustment_and_collected_statuses(decimal adjusted,
        decimal amount, string paymentStatus, decimal expectedBalance)
    {
        await using var db = new AcademyDeskDbContext(new DbContextOptionsBuilder<AcademyDeskDbContext>()
            .UseInMemoryDatabase("qa-finance-consumer-" + Guid.NewGuid()).Options);
        var invoice = new Invoice { AcademyId = Guid.NewGuid(), StudentId = Guid.NewGuid(),
            InvoiceNumber = "QA-CONSUMER", TotalAmount = 1000m, AdjustedAmount = adjusted };
        db.Invoices.Add(invoice);
        if (amount > 0m) db.Payments.Add(new Payment { AcademyId = invoice.AcademyId,
            InvoiceId = invoice.Id, Amount = amount, Status = paymentStatus });
        // An unapplied proposal is not money credited against the invoice.
        db.FinanceAdjustments.Add(new FinanceAdjustment { AcademyId = invoice.AcademyId,
            InvoiceId = invoice.Id, Type = "Discount", Amount = 50m, Reason = "QA pending", Status = "PendingApproval" });
        await db.SaveChangesAsync();
        var result = await new FeeRemindersController(db).Queue(invoice.AcademyId, new(invoice.Id, "InApp"), default);
        var response = Assert.IsType<FeeReminderResult>(Assert.IsType<OkObjectResult>(result.Result).Value);
        Assert.Equal(expectedBalance > 0m ? 1 : 0, response.QueuedCount);
        var notifications = await db.Notifications.ToListAsync();
        if (expectedBalance > 0m)
            Assert.Contains($"INR {expectedBalance:0.00}", Assert.Single(notifications).Message);
        else Assert.Empty(notifications);
        Assert.Equal(adjusted, invoice.AdjustedAmount);
        Assert.Equal(amount > 0m ? 1 : 0, await db.Payments.CountAsync());
        Assert.Equal("PendingApproval", (await db.FinanceAdjustments.SingleAsync()).Status);
    }
}
