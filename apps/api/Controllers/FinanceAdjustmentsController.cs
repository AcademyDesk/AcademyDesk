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
        var item = await db.FinanceAdjustments.SingleOrDefaultAsync(x => x.AcademyId == academyId && x.Id == adjustmentId, token); if (item is null) return NotFound();
        if (item.Status != "PendingApproval") return Conflict(new { message = "This adjustment has already been decided." });
        item.Status = request.Approve ? "Approved" : "Rejected"; item.ApprovalNotes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(); item.ApprovedAtUtc = DateTime.UtcNow; await db.SaveChangesAsync(token); return Ok(ToSummary(item));
    }
    private static readonly HashSet<string> AllowedTypes = new(StringComparer.OrdinalIgnoreCase) { "Discount", "Scholarship", "Concession", "Refund", "CreditNote" };
    private static FinanceAdjustmentSummary ToSummary(FinanceAdjustment x) => new(x.Id, x.InvoiceId, x.Type, x.Amount, x.Currency, x.Reason, x.Status, x.ApprovedAtUtc, x.ApprovalNotes);
}
public sealed record CreateFinanceAdjustmentRequest(Guid InvoiceId, string? Type, decimal Amount, string? Reason);
public sealed record FinanceAdjustmentDecisionRequest(bool Approve, string? Notes);
public sealed record FinanceAdjustmentSummary(Guid Id, Guid InvoiceId, string Type, decimal Amount, string Currency, string Reason, string Status, DateTime? ApprovedAtUtc, string? ApprovalNotes);
