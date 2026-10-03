using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
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
        var adjustment = new FinanceAdjustment { AcademyId = academyId, InvoiceId = invoice.Id, Type = request.Type!, Amount = request.Amount, Currency = invoice.Currency, Reason = request.Reason.Trim() };
        db.FinanceAdjustments.Add(adjustment); await db.SaveChangesAsync(token); return Ok(ToSummary(adjustment));
    }
    [HttpPatch("{adjustmentId:guid}/approval")]
    public async Task<ActionResult<FinanceAdjustmentSummary>> Decide(Guid academyId, Guid adjustmentId, FinanceAdjustmentDecisionRequest request, CancellationToken token)
    {
        var invoiceId = await db.FinanceAdjustments.AsNoTracking()
            .Where(x => x.AcademyId == academyId && x.Id == adjustmentId)
            .Select(x => (Guid?)x.InvoiceId).SingleOrDefaultAsync(token);
        if (invoiceId is null) return NotFound();

        // Match payment creation's invoice-first lock order. The finance access
        // filter normally owns the transaction; direct callers need their own.
        await using var ownedTransaction = db.Database.IsSqlServer() && db.Database.CurrentTransaction is null
            ? await db.Database.BeginTransactionAsync(token) : null;
        var invoiceQuery = db.Database.IsSqlServer()
            ? db.Invoices.FromSqlInterpolated($"SELECT * FROM [Invoices] WITH (UPDLOCK, HOLDLOCK) WHERE [AcademyId] = {academyId} AND [Id] = {invoiceId.Value}")
            : db.Invoices.Where(x => x.AcademyId == academyId && x.Id == invoiceId.Value);
        var invoice = await invoiceQuery.SingleOrDefaultAsync(token);
        if (invoice is null) return NotFound();
        // A competing decision may have completed while the invoice lock waited.
        var item = await db.FinanceAdjustments.SingleOrDefaultAsync(x => x.AcademyId == academyId && x.Id == adjustmentId, token);
        if (item is null) return NotFound();
        if (item.Status != "PendingApproval") return Conflict(new { message = "This adjustment has already been decided." });
        var decidedAtUtc = DateTime.UtcNow;
        if (request.Approve)
        {
            var paid = await db.Payments.Where(x => x.AcademyId == academyId && x.InvoiceId == invoice.Id &&
                (x.Status == "Completed" || x.Status == "Reconciled"))
                .SumAsync(x => (decimal?)x.Amount, token) ?? 0m;
            var remainingAfterApproval = invoice.TotalAmount - invoice.AdjustedAmount - item.Amount - paid;
            if (remainingAfterApproval < 0m)
                return BadRequest(new { message = "Adjustment exceeds the remaining invoice balance." });
            invoice.AdjustedAmount += item.Amount;
            item.AppliedAtUtc = decidedAtUtc;
            var balance = remainingAfterApproval;
            invoice.Status = balance <= 0 ? "Paid" : paid > 0 ? "PartiallyPaid" : "Issued";
        }
        item.Status = request.Approve ? "Approved" : "Rejected";
        item.ApprovalNotes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();
        item.ApprovedAtUtc = decidedAtUtc;
        await db.SaveChangesAsync(token);
        if (ownedTransaction is not null) await ownedTransaction.CommitAsync(token);
        return Ok(ToSummary(item));
    }
    private static readonly HashSet<string> AllowedTypes = new(StringComparer.OrdinalIgnoreCase) { "Discount", "Scholarship", "Concession", "Refund", "CreditNote" };
    private static FinanceAdjustmentSummary ToSummary(FinanceAdjustment x) => new(x.Id, x.InvoiceId, x.Type, x.Amount, x.Currency, x.Reason, x.Status, x.ApprovedAtUtc, x.ApprovalNotes);
}
public sealed record CreateFinanceAdjustmentRequest(Guid InvoiceId, string? Type, decimal Amount, string? Reason);
public sealed record FinanceAdjustmentDecisionRequest(bool Approve, string? Notes);
public sealed record FinanceAdjustmentSummary(Guid Id, Guid InvoiceId, string Type, decimal Amount, string Currency, string Reason, string Status, DateTime? ApprovedAtUtc, string? ApprovalNotes);
