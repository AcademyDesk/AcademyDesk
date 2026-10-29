using System.Text.Json;
using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Tests;

// Product approval 2026-10-12: FP1 interim restriction, FP2 no-cash statements,
// FP3 terminal voids. These checks do not certify accounting or tax compliance.
public sealed class BatchOneFinancialPolicyTests
{
    [Theory]
    [InlineData("Refund")]
    [InlineData("refund")]
    [InlineData("CreditNote")]
    [InlineData("creditnote")]
    public async Task Generic_refunds_and_credit_notes_are_blocked_without_mutation(string type)
    {
        await using var db = Db(); var invoice = Invoice(); db.Add(invoice); await db.SaveChangesAsync();
        var before = await Snapshot(db);
        var result = await new FinanceAdjustmentsController(db).Create(invoice.AcademyId, new(invoice.Id, type, 200m, "Synthetic reason"), default);
        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal(before, await Snapshot(db));
    }

    [Theory]
    [InlineData("Refund", true)]
    [InlineData("Refund", false)]
    [InlineData("CreditNote", true)]
    [InlineData("CreditNote", false)]
    public async Task Legacy_pending_documents_require_review_for_either_decision(string type, bool approve)
    {
        await using var db = Db(); var invoice = Invoice();
        var item = new FinanceAdjustment { AcademyId = invoice.AcademyId, InvoiceId = invoice.Id, Type = type, Amount = 200m, Reason = "Legacy synthetic" };
        db.AddRange(invoice, item); await db.SaveChangesAsync(); var before = await Snapshot(db);
        var controller = new FinanceAdjustmentsController(db);
        Assert.IsType<ConflictObjectResult>((await controller.Decide(invoice.AcademyId, item.Id, new(approve, "Synthetic decision"), default)).Result);
        Assert.Equal(before, await Snapshot(db));
        var list = Assert.IsAssignableFrom<IReadOnlyList<FinanceAdjustmentSummary>>(Assert.IsType<OkObjectResult>((await controller.List(invoice.AcademyId, null, default)).Result).Value);
        var summary = Assert.Single(list); Assert.True(summary.RequiresReviewedDisposition);
        Assert.Equal("LegacyInvoiceReductionNotSettlement", summary.FinancialEffect);
    }

    [Theory]
    [InlineData("Discount")]
    [InlineData("Scholarship")]
    [InlineData("Concession")]
    public async Task Invoice_reductions_still_need_and_accept_explicit_approval(string type)
    {
        await using var db = Db(); var invoice = Invoice(); db.Add(invoice); await db.SaveChangesAsync();
        var controller = new FinanceAdjustmentsController(db);
        var created = Assert.IsType<FinanceAdjustmentSummary>(Assert.IsType<OkObjectResult>((await controller.Create(invoice.AcademyId, new(invoice.Id, type, 200m, "Synthetic reduction"), default)).Result).Value);
        Assert.Equal(0m, invoice.AdjustedAmount); Assert.Equal("PendingApproval", created.Status);
        Assert.IsType<OkObjectResult>((await controller.Decide(invoice.AcademyId, created.Id, new(true, "Approved synthetic"), default)).Result);
        Assert.Equal(200m, invoice.AdjustedAmount); Assert.Empty(await db.Payments.ToArrayAsync());
    }

    [Theory]
    [InlineData("Monthly")]
    [InlineData("SessionBlock")]
    public async Task Zero_net_is_a_statement_not_a_bank_payment(string model)
    {
        await using var db = Db(); var profile = Profile(model); db.Add(profile); await db.SaveChangesAsync();
        var controller = PayrollFixture.Controller(db);
        var result = await controller.Pay(profile.AcademyId, new(profile.Id, "Synthetic period", null, null, 1000m, "BankTransfer", null, null), default);
        var summary = Assert.IsType<PayrollPayoutSummary>(Assert.IsType<CreatedResult>(result.Result).Value);
        var row = Assert.Single(await db.PayrollPayouts.ToArrayAsync());
        Assert.Equal("NoCashDue", row.Status); Assert.Equal("NoCashDue", row.PaymentMethod);
        Assert.Null(row.PaidAtUtc); Assert.Null(row.Reference); Assert.Equal(0m, row.NetAmount);
        Assert.Equal(1000m, row.GrossAmount); Assert.Equal(1000m, row.Deductions);
        Assert.Equal(profile.WorkerName, row.WorkerNameSnapshot); Assert.NotNull(row.IdentityRecordedAtUtc);
        Assert.Equal("NoCashDue", summary.SettlementKind); Assert.Equal("0.00", summary.NetAmountExact);
        var rows = Assert.IsAssignableFrom<IReadOnlyList<PayrollPayoutSummary>>(Assert.IsType<OkObjectResult>((await controller.Payouts(profile.AcademyId, default)).Result).Value);
        Assert.Equal(summary, Assert.Single(rows));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Zero_net_cannot_claim_a_paid_date_or_reference(bool paidDate)
    {
        await using var db = Db(); var profile = Profile("Monthly"); db.Add(profile); await db.SaveChangesAsync();
        var controller = PayrollFixture.Controller(db); var before = await Snapshot(db);
        Assert.IsType<BadRequestObjectResult>((await controller.Pay(profile.AcademyId,
            new(profile.Id, "Synthetic period", null, null, 1000m, "BankTransfer", paidDate ? null : "Synthetic reference", paidDate ? DateTime.UtcNow : null), default)).Result);
        Assert.Equal(before, await Snapshot(db));
    }

    [Theory]
    [InlineData("Completed")]
    [InlineData("Reconciled")]
    public async Task Voided_payments_cannot_be_restored_by_generic_status(string target)
    {
        await using var db = Db(); var invoice = Invoice();
        var payment = new Payment { AcademyId = invoice.AcademyId, InvoiceId = invoice.Id, Amount = 600m, Status = "Voided", ReconciliationReference = "Retained evidence", ReconciledAtUtc = DateTime.UtcNow };
        db.AddRange(invoice, payment); await db.SaveChangesAsync(); var before = await Snapshot(db);
        var denied = Assert.IsType<BadRequestObjectResult>(await new PaymentsController(db).UpdateStatus(invoice.AcademyId, payment.Id, new(target), default));
        Assert.Contains("Voided payments cannot be restored.", JsonSerializer.Serialize(denied.Value));
        Assert.Equal(before, await Snapshot(db));
    }

    [Fact]
    public async Task Voided_payments_cannot_be_restored_by_dedicated_reconciliation()
    {
        await using var db = Db(); var invoice = Invoice();
        var payment = new Payment { AcademyId = invoice.AcademyId, InvoiceId = invoice.Id, Amount = 600m, Status = "Voided" };
        db.AddRange(invoice, payment); await db.SaveChangesAsync(); var before = await Snapshot(db);
        Assert.IsType<BadRequestObjectResult>(await new PaymentsController(db).Reconcile(invoice.AcademyId, payment.Id, new("Synthetic reference"), default));
        Assert.Equal(before, await Snapshot(db));
    }

    [Fact]
    public void Exact_money_and_legacy_zero_net_are_explicit_in_response_contract()
    {
        var row = new PayrollPayoutSummary(Guid.NewGuid(), Guid.NewGuid(), "Legacy", "Unknown", "Synthetic", "Period", null,
            9999999999999999.99m, 9999999999999999.99m, 0m, "INR", "Paid", "BankTransfer", null, DateTime.UtcNow);
        Assert.Equal("9999999999999999.99", row.GrossAmountExact);
        Assert.Equal("LegacyZeroNet", row.SettlementKind);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(7)]
    public void Teacher_document_contract_preserves_recorded_cycle_and_utc_identity(int? sessions)
    {
        var identityAt = new DateTime(2026, 10, 12, 12, 34, 56, DateTimeKind.Utc);
        var row = new TeacherPayslipSummary(Guid.NewGuid(), "Synthetic document", "Synthetic period", 250m, 249.99m, 0.01m,
            "INR", "Paid", "BankTransfer", null, identityAt, Guid.NewGuid(), "Recorded worker", identityAt, sessions);
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(row, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
        var cycle = json.RootElement.GetProperty("sessionsCovered");
        if (sessions is null) Assert.Equal(JsonValueKind.Null, cycle.ValueKind);
        else Assert.Equal(sessions.Value, cycle.GetInt32());
        Assert.Equal("2026-10-12T12:34:56Z", json.RootElement.GetProperty("identityRecordedAtUtc").GetString());
        Assert.Equal("250.00", row.GrossAmountExact); Assert.Equal("249.99", row.DeductionsExact); Assert.Equal("0.01", row.NetAmountExact);
        Assert.Equal("PayoutSnapshot", row.IdentitySource); Assert.Equal("Teacher", row.WorkerType);
    }

    private static PayrollProfile Profile(string model) => new() { AcademyId = Guid.NewGuid(), WorkerType = "Staff", StaffUserId = Guid.NewGuid().ToString(), WorkerName = "Synthetic staff", PaymentModel = model, MonthlyAmount = model == "Monthly" ? 1000m : null, AmountPerCycle = model == "SessionBlock" ? 1000m : null, SessionsPerCycle = model == "SessionBlock" ? 4 : null };
    private static Invoice Invoice() => new() { AcademyId = Guid.NewGuid(), StudentId = Guid.NewGuid(), InvoiceNumber = "Synthetic policy", TotalAmount = 1000m };
    private static AcademyDeskDbContext Db() => new(new DbContextOptionsBuilder<AcademyDeskDbContext>().UseInMemoryDatabase("qa-policy-" + Guid.NewGuid()).Options);
    private static async Task<string> Snapshot(AcademyDeskDbContext db) => JsonSerializer.Serialize(new { Profiles = await db.PayrollProfiles.AsNoTracking().ToArrayAsync(), Payouts = await db.PayrollPayouts.AsNoTracking().ToArrayAsync(), Invoices = await db.Invoices.AsNoTracking().ToArrayAsync(), Payments = await db.Payments.AsNoTracking().ToArrayAsync(), Adjustments = await db.FinanceAdjustments.AsNoTracking().ToArrayAsync(), Audits = await db.AuditLogs.AsNoTracking().ToArrayAsync() });
}
