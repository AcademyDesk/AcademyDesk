using System.Globalization;
using System.Text.Json;
using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Tests;

// Retain approved invoice-reduction safeguards alongside the cloud's separate
// validated payment-restoration policy. SQL races/audit proof live in SqlHarness.
public sealed class FinanceAdjustmentReconciliationTests
{
    [Theory]
    [InlineData("Refund")]
    [InlineData("refund")]
    [InlineData("CreditNote")]
    [InlineData("creditnote")]
    public async Task Generic_settlement_documents_are_refused_without_changes(string type)
    {
        await using var db = Db(); var invoice = Invoice(); db.Add(invoice); await db.SaveChangesAsync();
        var before = await Snapshot(db);
        Assert.IsType<BadRequestObjectResult>((await new FinanceAdjustmentsController(db).Create(invoice.AcademyId, new(invoice.Id, type, 200m, "Synthetic"), default)).Result);
        Assert.Equal(before, await Snapshot(db));
    }

    [Theory]
    [InlineData("Refund", true)]
    [InlineData("Refund", false)]
    [InlineData("CreditNote", true)]
    [InlineData("CreditNote", false)]
    public async Task Legacy_pending_documents_are_retained_for_review_without_automatic_decisions(string type, bool approve)
    {
        await using var db = Db(); var invoice = Invoice(); var item = Adjustment(invoice, 200m); item.Type = type;
        db.AddRange(invoice, item); await db.SaveChangesAsync(); var before = await Snapshot(db);
        var controller = new FinanceAdjustmentsController(db);
        Assert.IsType<ConflictObjectResult>((await controller.Decide(invoice.AcademyId, item.Id, new(approve, "Synthetic"), default)).Result);
        Assert.Equal(before, await Snapshot(db));
        var summaries = Assert.IsAssignableFrom<IReadOnlyList<FinanceAdjustmentSummary>>(Assert.IsType<OkObjectResult>((await controller.List(invoice.AcademyId, null, default)).Result).Value);
        var summary = Assert.Single(summaries); Assert.True(summary.RequiresReviewedDisposition);
        Assert.Equal("LegacyInvoiceReductionNotSettlement", summary.FinancialEffect);
    }

    [Theory]
    [InlineData("0.001")]
    [InlineData("1000.001")]
    [InlineData("10000000000000000")]
    public async Task New_reductions_cannot_be_silently_rounded_or_exceed_SQL_precision(string value)
    {
        await using var db = Db(); var invoice = Invoice(); db.Add(invoice); await db.SaveChangesAsync(); var before = await Snapshot(db);
        var amount = decimal.Parse(value, CultureInfo.InvariantCulture);
        Assert.IsType<BadRequestObjectResult>((await new FinanceAdjustmentsController(db).Create(invoice.AcademyId, new(invoice.Id, "Discount", amount, "Synthetic"), default)).Result);
        Assert.Equal(before, await Snapshot(db));
    }

    [Theory]
    [InlineData("Completed", "400", "Paid")]
    [InlineData("Reconciled", "400", "Paid")]
    [InlineData("Completed", "399.99", "PartiallyPaid")]
    [InlineData("Reconciled", "399.99", "PartiallyPaid")]
    [InlineData("Voided", "1000", "Paid")]
    public async Task Approval_respects_collectible_balance_and_completed_plus_reconciled_payments(string paymentStatus, string amount, string expectedStatus)
    {
        await using var db = Db(); var invoice = Invoice(); var item = Adjustment(invoice, decimal.Parse(amount, CultureInfo.InvariantCulture));
        db.AddRange(invoice, item, new Payment { AcademyId = invoice.AcademyId, InvoiceId = invoice.Id, Amount = 600m, Status = paymentStatus });
        await db.SaveChangesAsync();
        var result = await new FinanceAdjustmentsController(db).Decide(invoice.AcademyId, item.Id, new(true, " Approved "), default);
        var summary = Assert.IsType<FinanceAdjustmentSummary>(Assert.IsType<OkObjectResult>(result.Result).Value);
        Assert.Equal("Approved", summary.Status); Assert.Equal("InvoiceReduction", summary.FinancialEffect); Assert.False(summary.RequiresReviewedDisposition);
        Assert.Equal(item.Amount, invoice.AdjustedAmount); Assert.Equal(expectedStatus, invoice.Status);
        Assert.Equal("Approved", item.ApprovalNotes); Assert.Equal(item.AppliedAtUtc, item.ApprovedAtUtc);
    }

    [Theory]
    [InlineData("Completed", "400.01")]
    [InlineData("Reconciled", "400.01")]
    [InlineData("Completed", "1000")]
    [InlineData("Reconciled", "1000")]
    public async Task Approval_cannot_reduce_an_invoice_below_collections(string status, string amount)
    {
        await using var db = Db(); var invoice = Invoice(); var item = Adjustment(invoice, decimal.Parse(amount, CultureInfo.InvariantCulture));
        db.AddRange(invoice, item, new Payment { AcademyId = invoice.AcademyId, InvoiceId = invoice.Id, Amount = 600m, Status = status });
        await db.SaveChangesAsync(); var before = await Snapshot(db);
        var denied = Assert.IsType<BadRequestObjectResult>((await new FinanceAdjustmentsController(db).Decide(invoice.AcademyId, item.Id, new(true, null), default)).Result);
        Assert.Contains("Adjustment exceeds the remaining invoice balance.", JsonSerializer.Serialize(denied.Value));
        Assert.Equal(before, await Snapshot(db)); Assert.Equal("PendingApproval", item.Status); Assert.Null(item.AppliedAtUtc);
    }

    [Theory]
    [InlineData("currency")]
    [InlineData("type")]
    [InlineData("zero")]
    [InlineData("negative")]
    [InlineData("precision")]
    [InlineData("range")]
    public async Task Malformed_stored_proposals_cannot_change_ledger_or_decision(string mode)
    {
        await using var db = Db(); var invoice = Invoice(); var item = Adjustment(invoice, 200m);
        if (mode == "currency") item.Currency = "USD";
        if (mode == "type") item.Type = "Unsupported";
        if (mode == "zero") item.Amount = 0;
        if (mode == "negative") item.Amount = -1;
        if (mode == "precision") item.Amount = 0.001m;
        if (mode == "range") item.Amount = 10000000000000000m;
        db.AddRange(invoice, item); await db.SaveChangesAsync(); var before = await Snapshot(db);
        Assert.IsType<BadRequestObjectResult>((await new FinanceAdjustmentsController(db).Decide(invoice.AcademyId, item.Id, new(true, null), default)).Result);
        Assert.Equal(before, await Snapshot(db));
    }

    [Theory]
    [InlineData("Discount")]
    [InlineData("Scholarship")]
    [InlineData("Concession")]
    public async Task Approved_invoice_reductions_preserve_overdue_state_until_settled(string type)
    {
        await using var db = Db(); var invoice = Invoice(); invoice.DueDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-7); invoice.Status = "Overdue";
        var item = Adjustment(invoice, 200m); item.Type = type; db.AddRange(invoice, item); await db.SaveChangesAsync();
        Assert.IsType<OkObjectResult>((await new FinanceAdjustmentsController(db).Decide(invoice.AcademyId, item.Id, new(true, null), default)).Result);
        Assert.Equal("Overdue", invoice.Status);
    }

    [Fact]
    public async Task Other_academy_payment_with_same_invoice_identifier_does_not_reduce_own_balance()
    {
        await using var db = Db(); var invoice = Invoice(); var item = Adjustment(invoice, 1000m);
        db.AddRange(invoice, item, new Payment { AcademyId = Guid.NewGuid(), InvoiceId = invoice.Id, Amount = 900m }); await db.SaveChangesAsync();
        Assert.IsType<OkObjectResult>((await new FinanceAdjustmentsController(db).Decide(invoice.AcademyId, item.Id, new(true, null), default)).Result);
        Assert.Equal(1000m, invoice.AdjustedAmount);
    }

    [Fact]
    public async Task A_second_proposal_is_rechecked_against_prior_approved_reduction()
    {
        await using var db = Db(); var invoice = Invoice(); invoice.AdjustedAmount = 700m;
        var item = Adjustment(invoice, 300.01m); db.AddRange(invoice, item); await db.SaveChangesAsync(); var before = await Snapshot(db);
        Assert.IsType<BadRequestObjectResult>((await new FinanceAdjustmentsController(db).Decide(invoice.AcademyId, item.Id, new(true, null), default)).Result);
        Assert.Equal(before, await Snapshot(db));
    }

    private static Invoice Invoice() => new() { AcademyId = Guid.NewGuid(), StudentId = Guid.NewGuid(), InvoiceNumber = "QA-REDUCTION", TotalAmount = 1000m };
    private static FinanceAdjustment Adjustment(Invoice invoice, decimal amount) => new() { AcademyId = invoice.AcademyId, InvoiceId = invoice.Id, Amount = amount, Reason = "Synthetic reduction" };
    private static AcademyDeskDbContext Db() => new(new DbContextOptionsBuilder<AcademyDeskDbContext>().UseInMemoryDatabase("qa-reconciled-adjustment-" + Guid.NewGuid()).Options);
    private static async Task<string> Snapshot(AcademyDeskDbContext db) => JsonSerializer.Serialize(new {
        Invoices = await db.Invoices.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(),
        Payments = await db.Payments.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(),
        Adjustments = await db.FinanceAdjustments.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(),
        Audits = await db.AuditLogs.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync()
    });
}
