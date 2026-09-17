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
        return Ok(await query.OrderByDescending(x => x.PaidAtUtc).Select(x => new PaymentSummary(x.Id, x.InvoiceId, x.Amount, x.Currency, x.Method, x.Status, x.Reference, x.PaidAtUtc)).ToListAsync(cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<PaymentSummary>> Create(Guid academyId, RecordPaymentRequest request, CancellationToken cancellationToken)
    {
        var invoice = await dbContext.Invoices.SingleOrDefaultAsync(x => x.Id == request.InvoiceId && x.AcademyId == academyId, cancellationToken);
        if (invoice is null) return NotFound();
        if (request.Amount <= 0) return BadRequest(new { message = "Payment amount must be positive." });
        var paid = await dbContext.Payments.Where(x => x.InvoiceId == invoice.Id && x.Status == "Completed").SumAsync(x => (decimal?)x.Amount, cancellationToken) ?? 0;
        if (paid + request.Amount > invoice.TotalAmount) return BadRequest(new { message = "Payment exceeds the invoice balance." });
        var payment = new Payment { AcademyId = academyId, InvoiceId = invoice.Id, Amount = request.Amount, Currency = invoice.Currency, Method = string.IsNullOrWhiteSpace(request.Method) ? "Offline" : request.Method.Trim(), Reference = request.Reference?.Trim() };
        invoice.Status = paid + request.Amount == invoice.TotalAmount ? "Paid" : "PartiallyPaid";
        dbContext.Payments.Add(payment); await dbContext.SaveChangesAsync(cancellationToken);
        return Created($"/api/academies/{academyId}/payments/{payment.Id}", new PaymentSummary(payment.Id, payment.InvoiceId, payment.Amount, payment.Currency, payment.Method, payment.Status, payment.Reference, payment.PaidAtUtc));
    }
    [HttpPatch("{paymentId:guid}/status")]
    public async Task<ActionResult> UpdateStatus(Guid academyId, Guid paymentId, UpdatePaymentStatusRequest request, CancellationToken token)
    { var x=await dbContext.Payments.SingleOrDefaultAsync(v=>v.Id==paymentId&&v.AcademyId==academyId,token); if(x is null)return NotFound(); if(request.Status is not("Completed" or "Reconciled" or "Voided"))return BadRequest(new { message = "Status must be Completed, Reconciled, or Voided." }); x.Status=request.Status; await dbContext.SaveChangesAsync(token); return Ok(); }
}

public sealed record RecordPaymentRequest(Guid InvoiceId, decimal Amount, string? Method, string? Reference);
public sealed record PaymentSummary(Guid Id, Guid InvoiceId, decimal Amount, string Currency, string Method, string Status, string? Reference, DateTime PaidAtUtc);
public sealed record UpdatePaymentStatusRequest(string Status);
