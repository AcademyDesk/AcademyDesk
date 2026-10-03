using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Tests;

public sealed class FinanceWorkflowTests
{
    [Fact]
    public async Task Reconcile_records_evidence_and_marks_payment_reconciled()
    {
        await using var db = CreateDb();
        var academyId = Guid.NewGuid();
        var invoice = new Invoice { AcademyId = academyId, InvoiceNumber = "QA-RECONCILE", StudentId = Guid.NewGuid(),
            TotalAmount = 4500m, Status = "Paid" };
        var payment = new Payment { AcademyId = academyId, InvoiceId = invoice.Id, Amount = 4500m, Method = "UPI", Reference = "UTR-INITIAL" };
        db.AddRange(invoice, payment);
        await db.SaveChangesAsync();

        var result = await new PaymentsController(db).Reconcile(academyId, payment.Id, new ReconcilePaymentRequest("HDFC-20260918-001"), CancellationToken.None);

        Assert.IsType<OkResult>(result);
        var stored = await db.Payments.SingleAsync(x => x.Id == payment.Id);
        Assert.Equal("Reconciled", stored.Status);
        Assert.Equal("HDFC-20260918-001", stored.ReconciliationReference);
        Assert.NotNull(stored.ReconciledAtUtc);
    }

    [Fact]
    public async Task Approving_full_adjustment_marks_invoice_paid_and_records_decision()
    {
        await using var db = CreateDb();
        var academyId = Guid.NewGuid();
        var invoice = new Invoice { AcademyId = academyId, InvoiceNumber = "INV-1001", StudentId = Guid.NewGuid(), TotalAmount = 1000m };
        var adjustment = new FinanceAdjustment { AcademyId = academyId, InvoiceId = invoice.Id, Amount = 1000m, Type = "Scholarship", Reason = "Approved scholarship policy" };
        db.AddRange(invoice, adjustment);
        await db.SaveChangesAsync();

        var result = await new FinanceAdjustmentsController(db).Decide(academyId, adjustment.Id, new FinanceAdjustmentDecisionRequest(true, "Approved by finance"), CancellationToken.None);

        Assert.IsType<OkObjectResult>(result.Result);
        var storedInvoice = await db.Invoices.SingleAsync(x => x.Id == invoice.Id);
        var storedAdjustment = await db.FinanceAdjustments.SingleAsync(x => x.Id == adjustment.Id);
        Assert.Equal(1000m, storedInvoice.AdjustedAmount);
        Assert.Equal("Paid", storedInvoice.Status);
        Assert.Equal("Approved", storedAdjustment.Status);
        Assert.Equal("Approved by finance", storedAdjustment.ApprovalNotes);
        Assert.NotNull(storedAdjustment.AppliedAtUtc);
    }

    [Fact]
    public async Task Rejecting_adjustment_preserves_invoice_balance()
    {
        await using var db = CreateDb();
        var academyId = Guid.NewGuid();
        var invoice = new Invoice { AcademyId = academyId, InvoiceNumber = "INV-1002", StudentId = Guid.NewGuid(), TotalAmount = 1500m };
        var adjustment = new FinanceAdjustment { AcademyId = academyId, InvoiceId = invoice.Id, Amount = 250m, Type = "Discount", Reason = "Insufficient evidence" };
        db.AddRange(invoice, adjustment);
        await db.SaveChangesAsync();

        await new FinanceAdjustmentsController(db).Decide(academyId, adjustment.Id, new FinanceAdjustmentDecisionRequest(false, "Evidence not supplied"), CancellationToken.None);

        var storedInvoice = await db.Invoices.SingleAsync(x => x.Id == invoice.Id);
        var storedAdjustment = await db.FinanceAdjustments.SingleAsync(x => x.Id == adjustment.Id);
        Assert.Equal(0m, storedInvoice.AdjustedAmount);
        Assert.Equal("Issued", storedInvoice.Status);
        Assert.Equal("Rejected", storedAdjustment.Status);
        Assert.Equal("Evidence not supplied", storedAdjustment.ApprovalNotes);
    }

    [Fact]
    public async Task Approval_cannot_reduce_collectible_below_existing_collections()
    {
        await using var db = CreateDb();
        var academyId = Guid.NewGuid();
        var invoice = new Invoice { AcademyId = academyId, InvoiceNumber = "QA-OVER-ADJUST", StudentId = Guid.NewGuid(),
            TotalAmount = 1000m, Status = "Paid" };
        var payment = new Payment { AcademyId = academyId, InvoiceId = invoice.Id, Amount = 1000m,
            Status = "Reconciled", ReconciliationReference = "QA-EXISTING", ReconciledAtUtc = DateTime.UtcNow };
        var adjustment = new FinanceAdjustment { AcademyId = academyId, InvoiceId = invoice.Id, Amount = 200m,
            Type = "Discount", Reason = "QA pending discount" };
        db.AddRange(invoice, payment, adjustment);
        await db.SaveChangesAsync();

        var result = await new FinanceAdjustmentsController(db).Decide(academyId, adjustment.Id,
            new FinanceAdjustmentDecisionRequest(true, "QA approval"), CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("PendingApproval", adjustment.Status);
        Assert.Null(adjustment.ApprovedAtUtc);
        Assert.Null(adjustment.AppliedAtUtc);
        Assert.Equal(0m, invoice.AdjustedAmount);
        Assert.Equal("Paid", invoice.Status);
    }

    [Fact]
    public async Task Approval_at_exact_remaining_balance_preserves_paid_status()
    {
        await using var db = CreateDb();
        var academyId = Guid.NewGuid();
        var invoice = new Invoice { AcademyId = academyId, InvoiceNumber = "QA-EXACT-ADJUST", StudentId = Guid.NewGuid(),
            TotalAmount = 1000m, Status = "PartiallyPaid" };
        var payment = new Payment { AcademyId = academyId, InvoiceId = invoice.Id, Amount = 800m, Status = "Reconciled" };
        var adjustment = new FinanceAdjustment { AcademyId = academyId, InvoiceId = invoice.Id, Amount = 200m,
            Type = "Discount", Reason = "QA exact discount" };
        db.AddRange(invoice, payment, adjustment);
        await db.SaveChangesAsync();

        var result = await new FinanceAdjustmentsController(db).Decide(academyId, adjustment.Id,
            new FinanceAdjustmentDecisionRequest(true, "QA approval"), CancellationToken.None);

        Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal("Approved", adjustment.Status);
        Assert.Equal(200m, invoice.AdjustedAmount);
        Assert.Equal("Paid", invoice.Status);
    }

    private static AcademyDeskDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AcademyDeskDbContext>()
            .UseInMemoryDatabase($"academy-desk-tests-{Guid.NewGuid()}")
            .Options;
        return new AcademyDeskDbContext(options);
    }
}
