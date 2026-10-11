using System.Text.Json;
using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Tests;

public sealed class InvoiceStatusTests
{
    public static IEnumerable<object[]> Matrix()
    {
        foreach (var ledger in new[] { (0m, 0m, "Completed"), (600m, 0m, "Completed"), (1000m, 0m, "Completed"),
            (600m, 0m, "Reconciled"), (1000m, 0m, "Voided"), (800m, 200m, "Reconciled"), (0m, 1000m, "Completed") })
        foreach (var status in new[] { "Issued", "PartiallyPaid", "Paid", "Overdue", "Cancelled" })
        {
            var collected = ledger.Item3 == "Voided" ? 0m : ledger.Item1;
            var balance = 1000m - ledger.Item2 - collected;
            var allowed = status == "Cancelled" || (balance == 0m ? status == "Paid" : collected > 0m
                ? status == "PartiallyPaid" : status is "Issued" or "Overdue");
            yield return [ledger.Item1, ledger.Item2, ledger.Item3, status, allowed];
            yield return [ledger.Item1, ledger.Item2, ledger.Item3, " " + status.ToLowerInvariant() + " ", allowed];
        }
    }

    [Theory]
    [MemberData(nameof(Matrix))]
    public async Task Status_must_match_collected_ledger_and_is_canonical(decimal amount, decimal adjusted,
        string paymentStatus, string requested, bool allowed)
    {
        await using var db = Db();
        var invoice = new Invoice { AcademyId = Guid.NewGuid(), StudentId = Guid.NewGuid(), InvoiceNumber = "QA-STATUS",
            TotalAmount = 1000m, AdjustedAmount = adjusted, Status = "Issued" };
        db.Invoices.Add(invoice);
        if (amount > 0m) db.Payments.Add(new Payment { AcademyId = invoice.AcademyId, InvoiceId = invoice.Id,
            Amount = amount, Status = paymentStatus, Reference = "Keep evidence" });
        await db.SaveChangesAsync();
        var original = JsonSerializer.Serialize(invoice);
        var payments = JsonSerializer.Serialize(await db.Payments.AsNoTracking().ToArrayAsync());
        var response = await new InvoicesController(db).UpdateStatus(invoice.AcademyId, invoice.Id, new(requested), default);
        if (allowed)
        {
            Assert.IsType<OkResult>(response);
            var canonical = new[] { "Issued", "PartiallyPaid", "Paid", "Overdue", "Cancelled" }
                .Single(s => s.Equals(requested.Trim(), StringComparison.OrdinalIgnoreCase));
            Assert.Equal(canonical, invoice.Status);
            invoice.Status = "Issued";
        }
        else Assert.IsType<BadRequestObjectResult>(response);
        Assert.Equal(original, JsonSerializer.Serialize(invoice));
        Assert.Equal(payments, JsonSerializer.Serialize(await db.Payments.AsNoTracking().ToArrayAsync()));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("unknown")]
    public async Task Invalid_status_has_no_write(string? status)
    {
        await using var db = Db();
        var invoice = new Invoice { AcademyId = Guid.NewGuid(), StudentId = Guid.NewGuid(), InvoiceNumber = "QA-INVALID", TotalAmount = 1000m };
        db.Add(invoice); await db.SaveChangesAsync();
        Assert.IsType<BadRequestObjectResult>(await new InvoicesController(db).UpdateStatus(invoice.AcademyId, invoice.Id, new(status!), default));
        Assert.Equal("Issued", invoice.Status);
    }

    [Fact]
    public async Task Foreign_invoice_is_not_found_or_mutated()
    {
        await using var db = Db();
        var invoice = new Invoice { AcademyId = Guid.NewGuid(), StudentId = Guid.NewGuid(), InvoiceNumber = "QA-FOREIGN", TotalAmount = 1000m };
        db.Add(invoice); await db.SaveChangesAsync();
        Assert.IsType<NotFoundResult>(await new InvoicesController(db).UpdateStatus(Guid.NewGuid(), invoice.Id, new("Paid"), default));
        Assert.Equal("Issued", invoice.Status);
    }

    private static AcademyDeskDbContext Db() => new(new DbContextOptionsBuilder<AcademyDeskDbContext>()
        .UseInMemoryDatabase("qa-invoice-status-" + Guid.NewGuid()).Options);
}
