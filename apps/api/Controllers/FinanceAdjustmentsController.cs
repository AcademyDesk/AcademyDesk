using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Controllers;

[ApiController]
[Route("api/academies/{academyId:guid}/finance-adjustments")]
public sealed class FinanceAdjustmentsController(AcademyDeskDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<FinanceAdjustmentSummary>>> List(Guid academyId, Guid? invoiceId, CancellationToken token)
    {
        var query = db.FinanceAdjustments.AsNoTracking().Where(x => x.AcademyId == academyId);
        if (invoiceId.HasValue) query = query.Where(x => x.InvoiceId == invoiceId);
        return Ok(await query.OrderByDescending(x => x.CreatedAtUtc).Select(x => new FinanceAdjustmentSummary(x.Id, x.InvoiceId, x.Type, x.Amount, x.Currency, x.Reason, x.Status, x.ApprovedAtUtc, x.ApprovalNotes)).ToListAsync(token));
    }
    [HttpPost]
    public async Task<ActionResult<FinanceAdjustmentSummary>> Create(Guid academyId, CreateFinanceAdjustmentRequest request, CancellationToken token)
    {
        var invoice = await db.Invoices.SingleOrDefaultAsync(x => x.AcademyId == academyId && x.Id == request.InvoiceId, token);
        if (invoice is null) return NotFound();
        if (request.Amount <= 0 || string.IsNullOrWhiteSpace(request.Reason) || !AllowedTypes.Contains(request.Type ?? "")) return BadRequest(new { message = "Enter a valid type, positive amount, and reason." });
        if (IsLegacyDocumentType(request.Type)) return BadRequest(new { message = "Refunds and credit notes require dedicated reviewed workflows; submit only invoice reductions here." });
        if (!ValidAmount(request.Amount)) return BadRequest(new { message = "Enter an amount with at most two decimal places within the supported range." });
        var adjustment = new FinanceAdjustment { AcademyId = academyId, InvoiceId = invoice.Id, Type = request.Type!, Amount = request.Amount, Currency = invoice.Currency, Reason = request.Reason.Trim() };
        db.FinanceAdjustments.Add(adjustment); await db.SaveChangesAsync(token); return Ok(ToSummary(adjustment));
    }
    [HttpPatch("{adjustmentId:guid}/approval")]
    public async Task<ActionResult<FinanceAdjustmentSummary>> Decide(Guid academyId, Guid adjustmentId, FinanceAdjustmentDecisionRequest request, CancellationToken token)
    {
        var target = await db.FinanceAdjustments.AsNoTracking().Where(x => x.AcademyId == academyId && x.Id == adjustmentId).Select(x => new { x.InvoiceId }).SingleOrDefaultAsync(token);
        if (target is null) return NotFound();
        await using var write = await InvoiceWriteScope.OpenAsync(db, academyId, target.InvoiceId, token);
        var cached = db.Database.IsSqlServer() ? db.ChangeTracker.Entries<FinanceAdjustment>().FirstOrDefault(x => x.Entity.Id == adjustmentId && x.Entity.AcademyId == academyId) : null;
        if (cached is not null && cached.State != EntityState.Unchanged)
            throw new InvalidOperationException("Enter the invoice write boundary before changing a tracked adjustment.");
        var item = await db.FinanceAdjustments.SingleOrDefaultAsync(x => x.AcademyId == academyId && x.Id == adjustmentId, token); if (item is null) return NotFound();
        // Direct callers may already track the proposal. Refresh only after the
        // invoice lock, so a competing approval cannot be applied a second time.
        if (cached is not null)
        {
            await db.Entry(item).ReloadAsync(token);
            if (db.Entry(item).State == EntityState.Detached) return NotFound();
        }
        if (item.Status != "PendingApproval") return Conflict(new { message = "This adjustment has already been decided." });
        if (IsLegacyDocumentType(item.Type)) return Conflict(new { message = "Legacy refund/credit-note requests require reviewed disposition; no decision has been applied." });
        if (request.Approve && write.Invoice is null) return NotFound();
        var decidedAtUtc = DateTime.UtcNow;
        if (request.Approve)
        {
            var invoice = write.Invoice!;
            if (!AllowedTypes.Contains(item.Type) || !ValidAmount(item.Amount) || !string.Equals(item.Currency, invoice.Currency, StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { message = "The stored adjustment type, amount, and currency must be valid for this invoice." });
            var paid = await db.Payments.Where(x => x.AcademyId == academyId && x.InvoiceId == invoice.Id && (x.Status == "Completed" || x.Status == "Reconciled")).SumAsync(x => (decimal?)x.Amount, token) ?? 0;
            var balance = invoice.TotalAmount - invoice.AdjustedAmount - paid - item.Amount;
            if (balance < 0m) return BadRequest(new { message = "Adjustment exceeds the remaining invoice balance." });
            invoice.AdjustedAmount += item.Amount;
            item.AppliedAtUtc = decidedAtUtc;
            if (!string.Equals(invoice.Status, "Cancelled", StringComparison.OrdinalIgnoreCase))
                invoice.Status = balance == 0m ? "Paid" : paid > 0 ? "PartiallyPaid" : invoice.DueDate < DateOnly.FromDateTime(decidedAtUtc) ? "Overdue" : "Issued";
        }
        item.Status = request.Approve ? "Approved" : "Rejected"; item.ApprovalNotes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(); item.ApprovedAtUtc = decidedAtUtc;
        await db.SaveChangesAsync(token); await write.CommitAsync(token); return Ok(ToSummary(item));
    }
    private static readonly HashSet<string> AllowedTypes = new(StringComparer.OrdinalIgnoreCase) { "Discount", "Scholarship", "Concession", "Refund", "CreditNote" };
    private static bool IsLegacyDocumentType(string? type) => string.Equals(type, "Refund", StringComparison.OrdinalIgnoreCase) || string.Equals(type, "CreditNote", StringComparison.OrdinalIgnoreCase);
    private static bool ValidAmount(decimal amount) => amount > 0m && amount <= 9999999999999999.99m && decimal.Round(amount, 2) == amount;
    private static FinanceAdjustmentSummary ToSummary(FinanceAdjustment x) => new(x.Id, x.InvoiceId, x.Type, x.Amount, x.Currency, x.Reason, x.Status, x.ApprovedAtUtc, x.ApprovalNotes);
}
public sealed record CreateFinanceAdjustmentRequest(Guid InvoiceId, string? Type, decimal Amount, string? Reason);
public sealed record FinanceAdjustmentDecisionRequest(bool Approve, string? Notes);
public sealed record FinanceAdjustmentSummary(Guid Id, Guid InvoiceId, string Type, decimal Amount, string Currency, string Reason, string Status, DateTime? ApprovedAtUtc, string? ApprovalNotes)
{
    public string FinancialEffect => string.Equals(Type, "Refund", StringComparison.OrdinalIgnoreCase) || string.Equals(Type, "CreditNote", StringComparison.OrdinalIgnoreCase) ? "LegacyInvoiceReductionNotSettlement" : "InvoiceReduction";
    public bool RequiresReviewedDisposition => FinancialEffect == "LegacyInvoiceReductionNotSettlement" && Status == "PendingApproval";
}
