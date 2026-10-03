using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Tests;

public sealed class PaymentTransitionTests
{
    [Theory]
    [InlineData("Completed", 1000, 0)]
    [InlineData("Reconciled", 1000, 0)]
    [InlineData("Completed", 600, 0)]
    [InlineData("Reconciled", 600, 0)]
    [InlineData("Completed", 800, 200)]
    [InlineData("Reconciled", 800, 200)]
    public async Task Void_reopens_due_invoice_and_preserves_payment_evidence(string status, decimal amount, decimal adjusted)
    {
        await using var db = CreateDb();
        var (invoice, payment) = Fixture(status, amount, adjusted);
        db.AddRange(invoice, payment);
        await db.SaveChangesAsync();
        var reference = payment.ReconciliationReference;
        var stamp = payment.ReconciledAtUtc;
        Assert.IsType<OkResult>(await new PaymentsController(db).UpdateStatus(invoice.AcademyId, payment.Id, new("Voided"), default));
        Assert.Equal("Voided", payment.Status);
        Assert.Equal("Overdue", invoice.Status);
        Assert.Equal(adjusted, invoice.AdjustedAmount);
        Assert.Equal(reference, payment.ReconciliationReference);
        Assert.Equal(stamp, payment.ReconciledAtUtc);
        Assert.Single(await db.Payments.ToListAsync());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(200)]
    public async Task Void_one_payment_retains_other_reconciled_collection(decimal adjusted)
    {
        await using var db = CreateDb();
        var (invoice, payment) = Fixture("Completed", 600m, adjusted);
        invoice.Status = "Paid";
        var other = new Payment { AcademyId = invoice.AcademyId, InvoiceId = invoice.Id, Amount = 400m - adjusted,
            Status = "Reconciled", ReconciliationReference = "QA-other", ReconciledAtUtc = DateTime.UtcNow };
        db.AddRange(invoice, payment, other);
        await db.SaveChangesAsync();
        Assert.IsType<OkResult>(await new PaymentsController(db).UpdateStatus(invoice.AcademyId, payment.Id, new("Voided"), default));
        Assert.Equal("PartiallyPaid", invoice.Status);
        Assert.Equal("Reconciled", other.Status);
        Assert.Equal("QA-other", other.ReconciliationReference);
        Assert.Equal(2, await db.Payments.CountAsync());
    }

    [Fact]
    public async Task Void_does_not_uncancel_an_invoice()
    {
        await using var db = CreateDb();
        var (invoice, payment) = Fixture("Completed", 600m, 0m);
        invoice.Status = "Cancelled";
        db.AddRange(invoice, payment);
        await db.SaveChangesAsync();
        Assert.IsType<OkResult>(await new PaymentsController(db).UpdateStatus(invoice.AcademyId, payment.Id, new("Voided"), default));
        Assert.Equal("Cancelled", invoice.Status);
    }

    [Fact]
    public async Task Repeated_void_is_idempotent()
    {
        await using var db = CreateDb();
        var (invoice, payment) = Fixture("Voided", 600m, 0m);
        invoice.Status = "Overdue";
        db.AddRange(invoice, payment);
        await db.SaveChangesAsync();
        Assert.IsType<OkResult>(await new PaymentsController(db).UpdateStatus(invoice.AcademyId, payment.Id, new("Voided"), default));
        Assert.Equal("Overdue", invoice.Status);
        Assert.Single(await db.Payments.ToListAsync());
    }

    [Theory]
    [InlineData("Completed")]
    [InlineData("Voided")]
    public async Task Generic_reconciled_status_cannot_bypass_dedicated_evidence(string status)
    {
        await using var db = CreateDb();
        var (invoice, payment) = Fixture(status, 600m, 0m);
        db.AddRange(invoice, payment);
        await db.SaveChangesAsync();
        var invoiceStatus = invoice.Status;
        Assert.IsType<BadRequestObjectResult>(await new PaymentsController(db).UpdateStatus(invoice.AcademyId, payment.Id, new("Reconciled"), default));
        Assert.Equal(status, payment.Status);
        Assert.Equal(invoiceStatus, invoice.Status);
        Assert.Null(payment.ReconciliationReference);
        Assert.Null(payment.ReconciledAtUtc);
    }

    [Fact]
    public async Task Dedicated_reconciliation_keeps_required_evidence()
    {
        await using var db = CreateDb();
        var (invoice, payment) = Fixture("Completed", 600m, 0m);
        db.AddRange(invoice, payment);
        await db.SaveChangesAsync();
        Assert.IsType<OkResult>(await new PaymentsController(db).Reconcile(invoice.AcademyId, payment.Id, new(" QA-ref "), default));
        Assert.Equal("Reconciled", payment.Status);
        Assert.Equal("QA-ref", payment.ReconciliationReference);
        Assert.NotNull(payment.ReconciledAtUtc);
        Assert.Equal("PartiallyPaid", invoice.Status);
    }

    [Fact]
    public async Task Invalid_status_changes_nothing()
    {
        await using var db = CreateDb();
        var (invoice, payment) = Fixture("Completed", 600m, 0m);
        db.AddRange(invoice, payment);
        await db.SaveChangesAsync();
        Assert.IsType<BadRequestObjectResult>(await new PaymentsController(db).UpdateStatus(invoice.AcademyId, payment.Id, new("Invalid"), default));
        Assert.Equal("Completed", payment.Status);
        Assert.Equal("PartiallyPaid", invoice.Status);
    }

    [Fact]
    public async Task Blank_reference_changes_nothing()
    {
        await using var db = CreateDb();
        var (invoice, payment) = Fixture("Completed", 600m, 0m);
        db.AddRange(invoice, payment);
        await db.SaveChangesAsync();
        Assert.IsType<BadRequestObjectResult>(await new PaymentsController(db).Reconcile(invoice.AcademyId, payment.Id, new(" "), default));
        Assert.Equal("Completed", payment.Status);
        Assert.Null(payment.ReconciledAtUtc);
    }

    [Fact]
    public async Task Foreign_payment_is_not_mutated()
    {
        await using var db = CreateDb();
        var (invoice, payment) = Fixture("Completed", 600m, 0m);
        db.AddRange(invoice, payment);
        await db.SaveChangesAsync();
        Assert.IsType<NotFoundResult>(await new PaymentsController(db).UpdateStatus(Guid.NewGuid(), payment.Id, new("Voided"), default));
        Assert.Equal("Completed", payment.Status);
    }

    private static (Invoice, Payment) Fixture(string status, decimal amount, decimal adjusted)
    {
        var invoice = new Invoice { AcademyId = Guid.NewGuid(), StudentId = Guid.NewGuid(), InvoiceNumber = "QA-TRANSITION",
            TotalAmount = 1000m, AdjustedAmount = adjusted, Status = amount + adjusted == 1000m ? "Paid" : "PartiallyPaid",
            DueDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-7) };
        var payment = new Payment { AcademyId = invoice.AcademyId, InvoiceId = invoice.Id, Amount = amount, Status = status,
            ReconciliationReference = status == "Reconciled" ? "QA-original" : null,
            ReconciledAtUtc = status == "Reconciled" ? DateTime.UtcNow : null };
        return (invoice, payment);
    }

    private static AcademyDeskDbContext CreateDb() => new(new DbContextOptionsBuilder<AcademyDeskDbContext>()
        .UseInMemoryDatabase("qa-payment-transition-" + Guid.NewGuid()).Options);
}
