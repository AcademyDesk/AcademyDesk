using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Controllers;

[ApiController]
[Route("api/academies/{academyId:guid}/invoices")]
public sealed class InvoicesController(AcademyDeskDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<InvoiceSummary>>> List(Guid academyId, CancellationToken cancellationToken) => Ok(await dbContext.Invoices.AsNoTracking().Where(x => x.AcademyId == academyId).OrderByDescending(x => x.IssuedDate).Select(x => new InvoiceSummary(x.Id, x.InvoiceNumber, x.StudentId, x.FeePlanId, x.TotalAmount, x.AdjustedAmount, dbContext.Payments.Where(payment => payment.InvoiceId == x.Id && payment.Status != "Voided").Sum(payment => (decimal?)payment.Amount) ?? 0, x.Currency, x.IssuedDate, x.DueDate, x.Status)).ToListAsync(cancellationToken));

    // Finance screens need billing identities, not student-management/profile access.
    // This action inherits the existing Invoices finance permission and module gate.
    [HttpGet("student-options")]
    public async Task<ActionResult<IReadOnlyList<InvoiceStudentOption>>> StudentOptions(Guid academyId, CancellationToken cancellationToken) =>
        Ok(await dbContext.Students.AsNoTracking().Where(x => x.AcademyId == academyId)
            .OrderBy(x => x.LastName).ThenBy(x => x.FirstName).ThenBy(x => x.Id)
            .Select(x => new InvoiceStudentOption(x.Id, x.FirstName, x.LastName, x.Email, x.Phone))
            .ToListAsync(cancellationToken));

    // Invoice preview belongs to Finance, not FinanceControls settings management.
    // Read only the document fields; do not expose payroll/tax configuration or add a row on GET.
    [HttpGet("document-settings")]
    public async Task<ActionResult<InvoiceDocumentSettingsSummary>> DocumentSettings(Guid academyId, CancellationToken cancellationToken)
    {
        var settings = await dbContext.AcademyFinanceSettings.AsNoTracking().Where(x => x.AcademyId == academyId)
            .Select(x => new InvoiceDocumentSettingsSummary(x.TaxRegistrationNumber, x.TaxLabel, x.InvoiceLogoUrl,
                x.InvoiceAuthorityName, x.InvoiceAuthorityTitle, x.InvoiceSignatureUrl, x.InvoiceTemplateKey))
            .SingleOrDefaultAsync(cancellationToken);
        return Ok(settings ?? new InvoiceDocumentSettingsSummary("", "GST", null, null, null, null, "Classic"));
    }

    [HttpPost]
    public async Task<ActionResult<InvoiceSummary>> Create(Guid academyId, CreateInvoiceRequest request, CancellationToken cancellationToken)
    {
        if (!await dbContext.Students.AnyAsync(x => x.Id == request.StudentId && x.AcademyId == academyId, cancellationToken)) return BadRequest(new { message = "The student does not belong to this academy." });
        FeePlan? plan = null;
        if (request.FeePlanId.HasValue) plan = await dbContext.FeePlans.SingleOrDefaultAsync(x => x.Id == request.FeePlanId && x.AcademyId == academyId && x.IsActive, cancellationToken);
        if (request.FeePlanId.HasValue && plan is null) return BadRequest(new { message = "The fee plan does not belong to this academy." });
        var amount = request.Amount ?? plan?.Amount ?? 0;
        if (amount <= 0) return BadRequest(new { message = "A positive invoice amount is required." });
        var number = $"INV-{DateTime.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(100, 999)}";
        var invoice = new Invoice { AcademyId = academyId, InvoiceNumber = number, StudentId = request.StudentId, FeePlanId = request.FeePlanId, TotalAmount = amount, Currency = plan?.Currency ?? "INR", DueDate = request.DueDate ?? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)) };
        dbContext.Invoices.Add(invoice); await dbContext.SaveChangesAsync(cancellationToken);
        return Created($"/api/academies/{academyId}/invoices/{invoice.Id}", new InvoiceSummary(invoice.Id, invoice.InvoiceNumber, invoice.StudentId, invoice.FeePlanId, invoice.TotalAmount, invoice.AdjustedAmount, 0, invoice.Currency, invoice.IssuedDate, invoice.DueDate, invoice.Status));
    }
    [HttpPatch("{invoiceId:guid}/status")]
    public async Task<ActionResult> UpdateStatus(Guid academyId, Guid invoiceId, UpdateInvoiceStatusRequest request, CancellationToken token)
    { var x=await dbContext.Invoices.SingleOrDefaultAsync(v=>v.Id==invoiceId&&v.AcademyId==academyId,token); if(x is null)return NotFound(); if(!new[]{"Issued","PartiallyPaid","Paid","Overdue","Cancelled"}.Contains(request.Status,StringComparer.OrdinalIgnoreCase))return BadRequest(); x.Status=request.Status.Trim(); await dbContext.SaveChangesAsync(token); return Ok(); }
}

public sealed record CreateInvoiceRequest(Guid StudentId, Guid? FeePlanId, decimal? Amount, DateOnly? DueDate);
public sealed record InvoiceSummary(Guid Id, string InvoiceNumber, Guid StudentId, Guid? FeePlanId, decimal TotalAmount, decimal AdjustedAmount, decimal PaidAmount, string Currency, DateOnly IssuedDate, DateOnly DueDate, string Status){public decimal Balance => Math.Max(0, TotalAmount-AdjustedAmount-PaidAmount);}
public sealed record UpdateInvoiceStatusRequest(string Status);
public sealed record InvoiceStudentOption(Guid Id, string FirstName, string LastName, string? Email, string? Phone);
public sealed record InvoiceDocumentSettingsSummary(string TaxRegistrationNumber, string TaxLabel, string? InvoiceLogoUrl,
    string? InvoiceAuthorityName, string? InvoiceAuthorityTitle, string? InvoiceSignatureUrl, string InvoiceTemplateKey);
