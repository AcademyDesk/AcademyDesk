using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/academies/{academyId:guid}/platform-services")]
public sealed class AcademyPlatformServicesController(AcademyDeskDbContext db) : ControllerBase
{
    [HttpGet("billing-invoices")]
    public async Task<ActionResult> BillingInvoices(Guid academyId, CancellationToken token) => Ok(await db.PlatformBillingInvoices.AsNoTracking()
        .Where(x => x.AcademyId == academyId)
        .OrderByDescending(x => x.DueDate)
        .Select(x => new { x.Id, x.InvoiceNumber, x.Amount, x.Currency, x.Status, x.PeriodStart, x.PeriodEnd, x.DueDate, x.PaidAtUtc, x.PaymentReference, x.PaymentSubmittedAtUtc })
        .ToListAsync(token));

    [HttpPost("billing-invoices/{invoiceId:guid}/payment-submission")]
    public async Task<ActionResult> SubmitPayment(Guid academyId, Guid invoiceId, SubmitPlatformPaymentRequest request, CancellationToken token)
    {
        var invoice = await db.PlatformBillingInvoices.SingleOrDefaultAsync(x => x.Id == invoiceId && x.AcademyId == academyId, token);
        if (invoice is null) return NotFound();
        if (invoice.Status is "Paid" or "Void") return BadRequest(new { message = "This invoice cannot accept a payment submission." });
        invoice.Status = "Payment submitted";
        invoice.PaymentReference = request.Reference?.Trim();
        invoice.PaymentSubmittedAtUtc = DateTime.UtcNow;
        invoice.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(token);
        return Ok(new { invoice.Id, invoice.Status, invoice.PaymentReference, invoice.PaymentSubmittedAtUtc });
    }

    [HttpGet("support-cases")]
    public async Task<ActionResult> SupportCases(Guid academyId, CancellationToken token) => Ok(await db.PlatformSupportCases.AsNoTracking()
        .Where(x => x.AcademyId == academyId)
        .OrderByDescending(x => x.UpdatedAtUtc ?? x.CreatedAtUtc)
        .Select(x => new { x.Id, x.Subject, x.Priority, x.Status, x.Description, x.AcademyResponse, x.CreatedAtUtc, x.UpdatedAtUtc, x.AcademyRespondedAtUtc, x.ResolvedAtUtc })
        .ToListAsync(token));

    [HttpPost("support-cases")]
    public async Task<ActionResult> CreateSupportCase(Guid academyId, CreateAcademySupportCaseRequest request, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(request.Subject) || string.IsNullOrWhiteSpace(request.Description)) return BadRequest(new { message = "A subject and message are required." });
        var item = new PlatformSupportCase { AcademyId = academyId, Subject = request.Subject.Trim(), Priority = string.IsNullOrWhiteSpace(request.Priority) ? "Normal" : request.Priority.Trim(), Description = request.Description.Trim() };
        db.PlatformSupportCases.Add(item);
        await db.SaveChangesAsync(token);
        return Created($"/api/academies/{academyId}/platform-services/support-cases/{item.Id}", item);
    }

    [HttpPost("support-cases/{caseId:guid}/response")]
    public async Task<ActionResult> RespondToSupportCase(Guid academyId, Guid caseId, RespondToSupportCaseRequest request, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(request.Message)) return BadRequest(new { message = "Write a response before sending it." });
        var item = await db.PlatformSupportCases.SingleOrDefaultAsync(x => x.Id == caseId && x.AcademyId == academyId, token);
        if (item is null) return NotFound();
        if (item.Status is "Resolved" or "Closed") return BadRequest(new { message = "This support case is closed." });
        item.AcademyResponse = request.Message.Trim();
        item.AcademyRespondedAtUtc = DateTime.UtcNow;
        item.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(token);
        return Ok(new { item.Id, item.AcademyResponse, item.AcademyRespondedAtUtc });
    }
}

public sealed record SubmitPlatformPaymentRequest(string? Reference);
public sealed record CreateAcademySupportCaseRequest(string Subject, string? Priority, string Description);
public sealed record RespondToSupportCaseRequest(string Message);
