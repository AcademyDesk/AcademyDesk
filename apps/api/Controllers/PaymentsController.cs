using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Controllers;

[ApiController]
[Route("api/academies/{academyId:guid}/payments")]
public sealed class PaymentsController(AcademyDeskDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PaymentSummary>>> List(Guid academyId, Guid? invoiceId, CancellationToken cancellationToken)
    {
        var query = dbContext.Payments.AsNoTracking().Where(x => x.AcademyId == academyId);
        if (invoiceId.HasValue) query = query.Where(x => x.InvoiceId == invoiceId.Value);
        return Ok(await query.OrderByDescending(x => x.PaidAtUtc).Select(x => new PaymentSummary(x.Id, x.InvoiceId, x.Amount, x.Currency, x.Method, x.Status, x.Reference, x.PaidAtUtc, x.ReconciliationReference, x.ReconciledAtUtc)).ToListAsync(cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<PaymentSummary>> Create(Guid academyId, RecordPaymentRequest request, CancellationToken cancellationToken)
    {
        await using var write = await InvoiceWriteScope.OpenAsync(dbContext, academyId, request.InvoiceId, cancellationToken);
        var invoice = write.Invoice;
        if (invoice is null) return NotFound();
        if (string.Equals(invoice.Status, "Cancelled", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "A payment cannot be recorded on a cancelled invoice." });
        if (request.Amount <= 0) return BadRequest(new { message = "Payment amount must be positive." });
        var paid = await dbContext.Payments.Where(x => x.AcademyId == academyId && x.InvoiceId == invoice.Id && (x.Status == "Completed" || x.Status == "Reconciled")).SumAsync(x => (decimal?)x.Amount, cancellationToken) ?? 0;
        var collectibleAmount = invoice.TotalAmount - invoice.AdjustedAmount;
        if (paid + request.Amount > collectibleAmount) return BadRequest(new { message = "Payment exceeds the invoice balance." });
        var payment = new Payment { AcademyId = academyId, InvoiceId = invoice.Id, Amount = request.Amount, Currency = invoice.Currency, Method = string.IsNullOrWhiteSpace(request.Method) ? "Offline" : request.Method.Trim(), Reference = request.Reference?.Trim() };
        invoice.Status = paid + request.Amount == collectibleAmount ? "Paid" : "PartiallyPaid";
        dbContext.Payments.Add(payment); await dbContext.SaveChangesAsync(cancellationToken);
        await write.CommitAsync(cancellationToken);
        return Created($"/api/academies/{academyId}/payments/{payment.Id}", new PaymentSummary(payment.Id, payment.InvoiceId, payment.Amount, payment.Currency, payment.Method, payment.Status, payment.Reference, payment.PaidAtUtc, payment.ReconciliationReference, payment.ReconciledAtUtc));
    }
    [HttpPatch("{paymentId:guid}/status")]
    public async Task<ActionResult> UpdateStatus(Guid academyId, Guid paymentId, UpdatePaymentStatusRequest request, CancellationToken token)
    {
        var target = await dbContext.Payments.AsNoTracking().Where(x => x.Id == paymentId && x.AcademyId == academyId).Select(x => new { x.InvoiceId }).SingleOrDefaultAsync(token);
        if (target is null) return NotFound();
        await using var write = await InvoiceWriteScope.OpenAsync(dbContext, academyId, target.InvoiceId, token);
        var payment = await dbContext.Payments.SingleOrDefaultAsync(x => x.Id == paymentId && x.AcademyId == academyId, token);
        if (payment is null) return NotFound();
        if (request.Status is not ("Completed" or "Reconciled" or "Voided"))
            return BadRequest(new { message = "Status must be Completed, Reconciled, or Voided." });
        if (request.Status == "Reconciled")
            return BadRequest(new { message = "Use the reconciliation action and provide a bank/cash reference." });
        if (payment.Status == request.Status) return Ok();
        if (payment.Status == "Voided" && request.Status == "Completed")
        {
            var rejected = await RestoreBalanceAsync(academyId, payment, write.Invoice, token);
            if (rejected is not null) return rejected;
        }
        if (request.Status == "Voided")
        {
            var invoice = write.Invoice;
            if (invoice is null) return NotFound();
            if (!string.Equals(invoice.Status, "Cancelled", StringComparison.OrdinalIgnoreCase))
            {
                // Exclude the changed row: SQL still contains its previous status until SaveChanges.
                var paid = await dbContext.Payments.Where(x => x.AcademyId == academyId && x.InvoiceId == invoice.Id &&
                    x.Id != payment.Id && (x.Status == "Completed" || x.Status == "Reconciled"))
                    .SumAsync(x => (decimal?)x.Amount, token) ?? 0m;
                var balance = invoice.TotalAmount - invoice.AdjustedAmount - paid;
                invoice.Status = balance <= 0m ? "Paid" : paid > 0m ? "PartiallyPaid" :
                    invoice.DueDate < DateOnly.FromDateTime(DateTime.UtcNow) ? "Overdue" : "Issued";
            }
        }
        payment.Status = request.Status;
        await dbContext.SaveChangesAsync(token);
        await write.CommitAsync(token);
        return Ok();
    }
    [HttpPatch("{paymentId:guid}/reconcile")]
    public async Task<ActionResult> Reconcile(Guid academyId, Guid paymentId, ReconcilePaymentRequest request, CancellationToken token)
    {
        var target = await dbContext.Payments.AsNoTracking().Where(x => x.Id == paymentId && x.AcademyId == academyId).Select(x => new { x.InvoiceId }).SingleOrDefaultAsync(token);
        if (target is null) return NotFound();
        await using var write = await InvoiceWriteScope.OpenAsync(dbContext, academyId, target.InvoiceId, token);
        var payment = await dbContext.Payments.SingleOrDefaultAsync(x => x.Id == paymentId && x.AcademyId == academyId, token);
        if (payment is null) return NotFound();
        if (string.IsNullOrWhiteSpace(request.Reference))
            return BadRequest(new { message = "A bank/cash reconciliation reference is required." });
        if (payment.Status == "Voided")
        {
            var rejected = await RestoreBalanceAsync(academyId, payment, write.Invoice, token);
            if (rejected is not null) return rejected;
        }
        payment.Status = "Reconciled";
        payment.ReconciledAtUtc = DateTime.UtcNow;
        payment.ReconciliationReference = request.Reference.Trim();
        await dbContext.SaveChangesAsync(token);
        await write.CommitAsync(token);
        return Ok();
    }

    private async Task<ActionResult?> RestoreBalanceAsync(Guid academyId, Payment payment, Invoice? invoice, CancellationToken token)
    {
        if (invoice is null) return NotFound();
        if (string.Equals(invoice.Status, "Cancelled", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "A payment cannot be restored on a cancelled invoice." });
        if (payment.Amount <= 0 || !string.Equals(payment.Currency, invoice.Currency, StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "The stored payment amount and currency must be valid for this invoice." });
        var otherPaid = await dbContext.Payments.Where(x => x.AcademyId == academyId && x.InvoiceId == invoice.Id &&
                x.Id != payment.Id && (x.Status == "Completed" || x.Status == "Reconciled"))
            .SumAsync(x => (decimal?)x.Amount, token) ?? 0m;
        var collectible = invoice.TotalAmount - invoice.AdjustedAmount;
        if (otherPaid + payment.Amount > collectible)
            return BadRequest(new { message = "Restoring this payment would exceed the adjusted invoice balance." });
        invoice.Status = otherPaid + payment.Amount == collectible ? "Paid" : "PartiallyPaid";
        return null;
    }
}

public sealed record RecordPaymentRequest(Guid InvoiceId, decimal Amount, string? Method, string? Reference);
public sealed record PaymentSummary(Guid Id, Guid InvoiceId, decimal Amount, string Currency, string Method, string Status, string? Reference, DateTime PaidAtUtc, string? ReconciliationReference, DateTime? ReconciledAtUtc);
public sealed record UpdatePaymentStatusRequest(string Status);
public sealed record ReconcilePaymentRequest(string Reference);
