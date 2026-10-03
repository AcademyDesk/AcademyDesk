using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
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
        if (request.Amount <= 0) return BadRequest(new { message = "Payment amount must be positive." });
        // The academy access filter owns the transaction for normal finance requests.
        // Direct/platform callers need their own boundary so the invoice lock lasts
        // through the balance check and the payment insert.
        await using var ownedTransaction = dbContext.Database.IsSqlServer() && dbContext.Database.CurrentTransaction is null
            ? await dbContext.Database.BeginTransactionAsync(cancellationToken) : null;
        var invoiceQuery = dbContext.Database.IsSqlServer()
            ? dbContext.Invoices.FromSqlInterpolated($"SELECT * FROM [Invoices] WITH (UPDLOCK, HOLDLOCK) WHERE [AcademyId] = {academyId} AND [Id] = {request.InvoiceId}")
            : dbContext.Invoices.Where(x => x.Id == request.InvoiceId && x.AcademyId == academyId);
        var invoice = await invoiceQuery.SingleOrDefaultAsync(cancellationToken);
        if (invoice is null) return NotFound();
        var paid = await dbContext.Payments.Where(x => x.InvoiceId == invoice.Id && (x.Status == "Completed" || x.Status == "Reconciled")).SumAsync(x => (decimal?)x.Amount, cancellationToken) ?? 0;
        var collectibleAmount = invoice.TotalAmount - invoice.AdjustedAmount;
        if (paid + request.Amount > collectibleAmount) return BadRequest(new { message = "Payment exceeds the invoice balance." });
        var payment = new Payment { AcademyId = academyId, InvoiceId = invoice.Id, Amount = request.Amount, Currency = invoice.Currency, Method = string.IsNullOrWhiteSpace(request.Method) ? "Offline" : request.Method.Trim(), Reference = request.Reference?.Trim() };
        invoice.Status = paid + request.Amount == collectibleAmount ? "Paid" : "PartiallyPaid";
        dbContext.Payments.Add(payment); await dbContext.SaveChangesAsync(cancellationToken);
        if (ownedTransaction is not null) await ownedTransaction.CommitAsync(cancellationToken);
        return Created($"/api/academies/{academyId}/payments/{payment.Id}", new PaymentSummary(payment.Id, payment.InvoiceId, payment.Amount, payment.Currency, payment.Method, payment.Status, payment.Reference, payment.PaidAtUtc, payment.ReconciliationReference, payment.ReconciledAtUtc));
    }
    [HttpPatch("{paymentId:guid}/status")]
    public async Task<ActionResult> UpdateStatus(Guid academyId, Guid paymentId, UpdatePaymentStatusRequest request, CancellationToken token)
    {
        var payment = await dbContext.Payments.SingleOrDefaultAsync(x => x.Id == paymentId && x.AcademyId == academyId, token);
        if (payment is null) return NotFound();
        if (request.Status is not ("Completed" or "Reconciled" or "Voided"))
            return BadRequest(new { message = "Status must be Completed, Reconciled, or Voided." });
        if (request.Status == "Reconciled")
            return BadRequest(new { message = "Use the reconciliation action and provide a bank/cash reference." });
        if (payment.Status == request.Status) return Ok();
        if (request.Status == "Voided")
        {
            var invoice = await dbContext.Invoices.SingleOrDefaultAsync(x => x.Id == payment.InvoiceId && x.AcademyId == academyId, token);
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
        return Ok();
    }
    [HttpPatch("{paymentId:guid}/reconcile")]
    public async Task<ActionResult> Reconcile(Guid academyId, Guid paymentId, ReconcilePaymentRequest request, CancellationToken token)
    { var payment=await dbContext.Payments.SingleOrDefaultAsync(x=>x.Id==paymentId&&x.AcademyId==academyId,token);if(payment is null)return NotFound();if(string.IsNullOrWhiteSpace(request.Reference))return BadRequest(new{message="A bank/cash reconciliation reference is required."});payment.Status="Reconciled";payment.ReconciledAtUtc=DateTime.UtcNow;payment.ReconciliationReference=request.Reference.Trim();await dbContext.SaveChangesAsync(token);return Ok(); }
}

public sealed record RecordPaymentRequest(Guid InvoiceId, decimal Amount, string? Method, string? Reference);
public sealed record PaymentSummary(Guid Id, Guid InvoiceId, decimal Amount, string Currency, string Method, string Status, string? Reference, DateTime PaidAtUtc, string? ReconciliationReference, DateTime? ReconciledAtUtc);
public sealed record UpdatePaymentStatusRequest(string Status);
public sealed record ReconcilePaymentRequest(string Reference);
