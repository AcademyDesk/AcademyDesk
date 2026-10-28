using System.Text.Json;
using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Tests;

// User-approved restoration policy. Controller/InMemory evidence, not SQL concurrency proof.
public sealed class PaymentRestorationTests
{
    [Theory]
    [InlineData(false, 600, 0, 0, "PartiallyPaid")]
    [InlineData(true, 600, 0, 0, "PartiallyPaid")]
    [InlineData(false, 1000, 0, 0, "Paid")]
    [InlineData(true, 1000, 0, 0, "Paid")]
    [InlineData(false, 800, 200, 0, "Paid")]
    [InlineData(true, 800, 200, 0, "Paid")]
    [InlineData(false, 600, 200, 200, "Paid")]
    [InlineData(true, 600, 200, 200, "Paid")]
    [InlineData(false, 300, 200, 100, "PartiallyPaid")]
    [InlineData(true, 300, 200, 100, "PartiallyPaid")]
    public async Task Restoration_recalculates_adjusted_balance_and_preserves_other_rows(
        bool reconcile, decimal amount, decimal adjusted, decimal otherAmount, string expectedStatus)
    {
        await using var db = CreateDb();
        var (invoice, payment) = Fixture(amount, adjusted);
        var originalReference = payment.ReconciliationReference;
        var originalStamp = payment.ReconciledAtUtc;
        db.AddRange(invoice, payment,
            new Payment { AcademyId = invoice.AcademyId, InvoiceId = invoice.Id, Amount = otherAmount, Status = "Reconciled", ReconciliationReference = "OTHER" },
            new Payment { AcademyId = invoice.AcademyId, InvoiceId = invoice.Id, Amount = 9999m, Status = "Voided" });
        await db.SaveChangesAsync();
        Assert.IsType<OkResult>(await Restore(db, invoice.AcademyId, payment.Id, reconcile));
        db.ChangeTracker.Clear();
        var saved = await db.Payments.AsNoTracking().SingleAsync(x => x.Id == payment.Id);
        var savedInvoice = await db.Invoices.AsNoTracking().SingleAsync(x => x.Id == invoice.Id);
        Assert.Equal(expectedStatus, savedInvoice.Status);
        Assert.Equal(1000m, savedInvoice.TotalAmount); Assert.Equal(adjusted, savedInvoice.AdjustedAmount);
        Assert.Equal(amount, saved.Amount); Assert.Equal(reconcile ? "Reconciled" : "Completed", saved.Status);
        Assert.Equal(reconcile ? "RESTORED" : originalReference, saved.ReconciliationReference);
        if (reconcile) Assert.True(saved.ReconciledAtUtc > originalStamp);
        else Assert.Equal(originalStamp, saved.ReconciledAtUtc);
        Assert.Equal(3, await db.Payments.CountAsync());
        Assert.Equal(otherAmount, await db.Payments.Where(x => x.ReconciliationReference == "OTHER").Select(x => x.Amount).SingleAsync());
    }

    [Theory]
    [InlineData(false, 600, 0, 400.01)]
    [InlineData(true, 600, 0, 400.01)]
    [InlineData(false, 800, 200, 0.01)]
    [InlineData(true, 800, 200, 0.01)]
    [InlineData(false, 1, 1000, 0)]
    [InlineData(true, 1, 1000, 0)]
    public async Task Restoration_cannot_overcollect_or_ignore_adjustments(bool reconcile, decimal amount, decimal adjusted, decimal other)
    {
        await using var db = CreateDb();
        var (invoice, payment) = Fixture(amount, adjusted);
        db.AddRange(invoice, payment, new Payment { AcademyId = invoice.AcademyId, InvoiceId = invoice.Id, Amount = other, Status = "Completed" });
        await db.SaveChangesAsync();
        var before = await Snapshot(db);
        Assert.IsType<BadRequestObjectResult>(await Restore(db, invoice.AcademyId, payment.Id, reconcile));
        db.ChangeTracker.Clear(); Assert.Equal(before, await Snapshot(db));
    }

    [Theory]
    [InlineData(false, "Cancelled")]
    [InlineData(true, "Cancelled")]
    [InlineData(false, "cancelled")]
    [InlineData(true, "cancelled")]
    public async Task Restoration_cannot_uncancel_invoice(bool reconcile, string status)
    {
        await using var db = CreateDb(); var (invoice, payment) = Fixture(600m, 0m); invoice.Status = status;
        db.AddRange(invoice, payment); await db.SaveChangesAsync(); var before = await Snapshot(db);
        Assert.IsType<BadRequestObjectResult>(await Restore(db, invoice.AcademyId, payment.Id, reconcile));
        db.ChangeTracker.Clear(); Assert.Equal(before, await Snapshot(db));
    }

    [Theory]
    [InlineData(false, 0, "INR")]
    [InlineData(true, 0, "INR")]
    [InlineData(false, -1, "INR")]
    [InlineData(true, -1, "INR")]
    [InlineData(false, 600, "USD")]
    [InlineData(true, 600, "USD")]
    public async Task Invalid_stored_amount_or_currency_cannot_be_restored(bool reconcile, decimal amount, string currency)
    {
        await using var db = CreateDb(); var (invoice, payment) = Fixture(amount, 0m); payment.Currency = currency;
        db.AddRange(invoice, payment); await db.SaveChangesAsync(); var before = await Snapshot(db);
        Assert.IsType<BadRequestObjectResult>(await Restore(db, invoice.AcademyId, payment.Id, reconcile));
        db.ChangeTracker.Clear(); Assert.Equal(before, await Snapshot(db));
    }

    [Theory]
    [InlineData(false, "missing")]
    [InlineData(true, "missing")]
    [InlineData(false, "foreign-invoice")]
    [InlineData(true, "foreign-invoice")]
    [InlineData(false, "foreign-payment")]
    [InlineData(true, "foreign-payment")]
    public async Task Missing_or_foreign_targets_do_not_change_rows(bool reconcile, string kind)
    {
        await using var db = CreateDb(); var (invoice, payment) = Fixture(600m, 0m);
        var academyId = invoice.AcademyId;
        if (kind == "foreign-invoice") invoice.AcademyId = Guid.NewGuid();
        if (kind == "foreign-payment") academyId = Guid.NewGuid();
        db.Payments.Add(payment); if (kind != "missing") db.Invoices.Add(invoice);
        await db.SaveChangesAsync(); var before = await Snapshot(db);
        Assert.IsType<NotFoundResult>(await Restore(db, academyId, payment.Id, reconcile));
        db.ChangeTracker.Clear(); Assert.Equal(before, await Snapshot(db));
    }

    private static Task<ActionResult> Restore(AcademyDeskDbContext db, Guid academyId, Guid paymentId, bool reconcile) => reconcile
        ? new PaymentsController(db).Reconcile(academyId, paymentId, new(" RESTORED "), default)
        : new PaymentsController(db).UpdateStatus(academyId, paymentId, new("Completed"), default);
    private static async Task<string> Snapshot(AcademyDeskDbContext db) => JsonSerializer.Serialize(new
    {
        Invoices = await db.Invoices.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
        Payments = await db.Payments.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
    });
    private static (Invoice, Payment) Fixture(decimal amount, decimal adjusted)
    {
        var invoice = new Invoice { AcademyId = Guid.NewGuid(), StudentId = Guid.NewGuid(), InvoiceNumber = "QA-RESTORE",
            Currency = "INR", TotalAmount = 1000m, AdjustedAmount = adjusted, Status = "Overdue", DueDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-7) };
        var payment = new Payment { AcademyId = invoice.AcademyId, InvoiceId = invoice.Id, Amount = amount, Currency = "INR", Status = "Voided",
            ReconciliationReference = "ORIGINAL", ReconciledAtUtc = DateTime.UtcNow.AddDays(-1) };
        return (invoice, payment);
    }
    private static AcademyDeskDbContext CreateDb() => new(new DbContextOptionsBuilder<AcademyDeskDbContext>()
        .UseInMemoryDatabase("qa-restoration-" + Guid.NewGuid()).Options);
}
