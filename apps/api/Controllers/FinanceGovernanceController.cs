using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace AcademyDesk.Api.Controllers;
public sealed record CollectionTaskSummary(Guid Id, string Type, string Title, string? Description, string Priority,
    string Status, DateTime? DueAtUtc, string? EscalationStage, DateOnly? PromisedPaymentDate);
public sealed record CollectionInvoiceSummary(Guid Id, string InvoiceNumber, Guid StudentId, decimal TotalAmount,
    decimal AdjustedAmount, decimal PaidAmount, string Currency, DateOnly DueDate, string Status, int DaysOverdue)
{
    public decimal Balance => Math.Max(0, TotalAmount - AdjustedAmount - PaidAmount);
}
public sealed record FinanceCurrencyTotals(string Currency, decimal GrossBilled, decimal ApprovedAdjustments,
    decimal Collected, decimal Outstanding, decimal Reconciled);
[ApiController]
[Route("api/academies/{academyId:guid}/finance-governance")]
public sealed class FinanceGovernanceController(AcademyDeskDbContext db, IdentityDbContext identity) : ControllerBase
{
 private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

 // Finance can manage its Collections register, not unrelated admin work items.
 // Preserve assignment, entity links, status and completion metadata on edits.
 [HttpGet("collection-tasks")]
 public async Task<ActionResult<IReadOnlyList<CollectionTaskSummary>>> CollectionTasks(Guid academyId, CancellationToken token) =>
     Ok(await db.AdminWorkItems.AsNoTracking().Where(x => x.AcademyId == academyId && x.Type == "Collections")
         .OrderBy(x => x.DueAtUtc).ThenByDescending(x => x.Priority).ThenBy(x => x.Id)
         .Select(x => new CollectionTaskSummary(x.Id, x.Type, x.Title, x.Description, x.Priority, x.Status,
             x.DueAtUtc, x.EscalationStage, x.PromisedPaymentDate)).ToListAsync(token));

 [HttpPatch("collection-tasks/{id:guid}")]
 public async Task<ActionResult<CollectionTaskSummary>> UpdateCollectionTask(Guid academyId, Guid id, CollectionStateRequest request, CancellationToken token)
 {
     var item = await db.AdminWorkItems.SingleOrDefaultAsync(x => x.AcademyId == academyId && x.Id == id && x.Type == "Collections", token);
     if (item is null) return NotFound();
     if (request.EscalationStage is not ("Initial" or "Reminder" or "ManagerReview" or "FinalNotice"))
         return BadRequest(new { message = "Choose a valid collections escalation stage." });
     item.EscalationStage = request.EscalationStage;
     item.PromisedPaymentDate = request.PromisedPaymentDate;
     await db.SaveChangesAsync(token);
     return Ok(new CollectionTaskSummary(item.Id, item.Type, item.Title, item.Description, item.Priority,
         item.Status, item.DueAtUtc, item.EscalationStage, item.PromisedPaymentDate));
 }
 [HttpGet("settings")] public async Task<ActionResult<FinanceSettingsSummary>> Get(Guid academyId,CancellationToken t){var x=await db.AcademyFinanceSettings.AsNoTracking().SingleOrDefaultAsync(x=>x.AcademyId==academyId,t);return Ok(x is null?new FinanceSettingsSummary("","GST",0,7,false,null,null,null,null,"Classic","Standard"):new(x.TaxRegistrationNumber,x.TaxLabel,x.TaxRatePercent,x.DefaultPaymentTermsDays,x.TaxInclusivePricing,x.InvoiceLogoUrl,x.InvoiceAuthorityName,x.InvoiceAuthorityTitle,x.InvoiceSignatureUrl,x.InvoiceTemplateKey,x.PayslipTemplateKey));}
 [HttpPut("settings")] public async Task<ActionResult<FinanceSettingsSummary>> Save(Guid academyId,FinanceSettingsRequest r,CancellationToken t){if(r.TaxRatePercent<0||r.TaxRatePercent>100||r.DefaultPaymentTermsDays<0||r.DefaultPaymentTermsDays>365)return BadRequest(new{message="Check tax rate and payment terms."});if(!new[]{"Classic","Modern","Minimal","Formal"}.Contains(r.InvoiceTemplateKey??"Classic")||!new[]{"Standard","Compact","Professional"}.Contains(r.PayslipTemplateKey??"Standard"))return BadRequest(new{message="Choose a supported document theme."});var x=await db.AcademyFinanceSettings.SingleOrDefaultAsync(x=>x.AcademyId==academyId,t);if(x is null){x=new AcademyFinanceSettings{AcademyId=academyId};db.AcademyFinanceSettings.Add(x);}x.TaxRegistrationNumber=r.TaxRegistrationNumber?.Trim()??"";x.TaxLabel=string.IsNullOrWhiteSpace(r.TaxLabel)?"GST":r.TaxLabel.Trim();x.TaxRatePercent=r.TaxRatePercent;x.DefaultPaymentTermsDays=r.DefaultPaymentTermsDays;x.TaxInclusivePricing=r.TaxInclusivePricing;x.InvoiceLogoUrl=Clean(r.InvoiceLogoUrl);x.InvoiceAuthorityName=Clean(r.InvoiceAuthorityName);x.InvoiceAuthorityTitle=Clean(r.InvoiceAuthorityTitle);x.InvoiceSignatureUrl=Clean(r.InvoiceSignatureUrl);x.InvoiceTemplateKey=r.InvoiceTemplateKey??"Classic";x.PayslipTemplateKey=r.PayslipTemplateKey??"Standard";await db.SaveChangesAsync(t);return Ok(new FinanceSettingsSummary(x.TaxRegistrationNumber,x.TaxLabel,x.TaxRatePercent,x.DefaultPaymentTermsDays,x.TaxInclusivePricing,x.InvoiceLogoUrl,x.InvoiceAuthorityName,x.InvoiceAuthorityTitle,x.InvoiceSignatureUrl,x.InvoiceTemplateKey,x.PayslipTemplateKey));}
 private async Task<List<CollectionInvoiceSummary>> OverdueCollections(Guid academyId, DateOnly today, CancellationToken token)
 {
     // Match the existing invoice balance contract; reconciled money is still paid.
     // Project through SQL first, then evaluate the computed DTO balance once per row.
     var rows = await db.Invoices.AsNoTracking()
         .Where(x => x.AcademyId == academyId && x.Status != "Paid" && x.Status != "Cancelled" && x.DueDate < today)
         .OrderBy(x => x.DueDate).ThenBy(x => x.Id)
         .Select(x => new CollectionInvoiceSummary(x.Id, x.InvoiceNumber, x.StudentId, x.TotalAmount, x.AdjustedAmount,
             db.Payments.Where(p => p.AcademyId == academyId && p.InvoiceId == x.Id && p.Status != "Voided")
                 .Sum(p => (decimal?)p.Amount) ?? 0, x.Currency, x.DueDate, x.Status, today.DayNumber - x.DueDate.DayNumber))
         .ToListAsync(token);
     return rows.Where(x => x.Balance > 0).ToList();
 }
 [HttpGet("collections")]
 public async Task<ActionResult<IReadOnlyList<CollectionInvoiceSummary>>> Collections(Guid academyId, CancellationToken token) =>
     Ok(await OverdueCollections(academyId, DateOnly.FromDateTime(DateTime.UtcNow), token));
 [HttpGet("summary")]
 public async Task<ActionResult> Summary(Guid academyId, CancellationToken token)
 {
     var invoices = await db.Invoices.AsNoTracking().Where(x => x.AcademyId == academyId)
         .Select(x => new { x.Currency, x.TotalAmount, x.AdjustedAmount }).ToListAsync(token);
     var payments = await db.Payments.AsNoTracking().Where(x => x.AcademyId == academyId && x.Status != "Voided")
         .Select(x => new { x.Currency, x.Amount, x.Status }).ToListAsync(token);
     // Match fee-plan currency normalization, preserving unknown stored units rather
     // than assigning INR or inventing an exchange rate. Unlike units never net off.
     static string Unit(string? value) => value?.Trim().ToUpperInvariant() ?? "";
     static bool HasCurrencyCode(string value) => value.Length == 3 && value.All(c => c is >= 'A' and <= 'Z');
     var invoiceGroups = invoices.ToLookup(x => Unit(x.Currency));
     var paymentGroups = payments.ToLookup(x => Unit(x.Currency));
     var totals = invoiceGroups.Select(x => x.Key).Union(paymentGroups.Select(x => x.Key), StringComparer.Ordinal)
         .Order(StringComparer.Ordinal).Select(currency =>
         {
             var gross = invoiceGroups[currency].Sum(x => x.TotalAmount);
             var adjusted = invoiceGroups[currency].Sum(x => x.AdjustedAmount);
             var collected = paymentGroups[currency].Sum(x => x.Amount);
             return new FinanceCurrencyTotals(currency, gross, adjusted, collected,
                 Math.Max(0, gross - adjusted - collected), paymentGroups[currency].Where(x => x.Status == "Reconciled").Sum(x => x.Amount));
         }).ToArray();
     // Retain numeric legacy fields for a single currency and an empty ledger.
     // Mixed/malformed currencies have no meaningful aggregate monetary scalar.
     var single = totals.Length == 1 && HasCurrencyCode(totals[0].Currency) ? totals[0] : null;
     var legacy = totals.Length == 0 ? new FinanceCurrencyTotals("", 0, 0, 0, 0, 0) : single;
     return Ok(new
     {
         GrossBilled = legacy?.GrossBilled, ApprovedAdjustments = legacy?.ApprovedAdjustments,
         Collected = legacy?.Collected, Outstanding = legacy?.Outstanding, Reconciled = legacy?.Reconciled,
         Currency = single?.Currency, HasMixedCurrencies = totals.Length > 1,
         HasInvalidCurrency = totals.Any(x => !HasCurrencyCode(x.Currency)), CurrencyTotals = totals,
         OverdueInvoices = (await OverdueCollections(academyId, DateOnly.FromDateTime(DateTime.UtcNow), token)).Count
     });
 }
 [HttpPost("collections/{invoiceId:guid}/follow-up")]
 public async Task<ActionResult> CreateFollowUp(Guid academyId, Guid invoiceId, CollectionFollowUpRequest r, CancellationToken t)
 {
     var invoice = await db.Invoices.AsNoTracking().SingleOrDefaultAsync(x => x.Id == invoiceId && x.AcademyId == academyId, t);
     if (invoice is null) return NotFound();
     var paid = await db.Payments.Where(p => p.AcademyId == academyId && p.InvoiceId == invoiceId && p.Status != "Voided")
         .SumAsync(p => (decimal?)p.Amount, t) ?? 0;
     if (invoice.Status is "Paid" or "Cancelled" || invoice.TotalAmount - invoice.AdjustedAmount - paid <= 0)
         return Conflict(new { message = "This invoice no longer requires collections follow-up." });
     var priority = string.IsNullOrWhiteSpace(r.Priority) ? "High" : r.Priority.Trim();
     if (!WorkItemValidation.ValidPriority(priority)) return BadRequest(new { message = "Invalid priority." });
     if (r.Note?.Trim().Length > 4000) return BadRequest(new { message = "Follow-up note exceeds its allowed length." });
     if (!await WorkItemValidation.ValidAssignee(identity, academyId, r.AssignedUserId, t))
         return BadRequest(new { message = "Choose an active staff assignee in this academy." });
     var item = new AdminWorkItem { AcademyId = academyId, Type = "Collections", Title = $"Collect {invoice.InvoiceNumber}",
         Description = string.IsNullOrWhiteSpace(r.Note) ? $"Overdue invoice {invoice.InvoiceNumber}." : r.Note.Trim(),
         Priority = priority, AssignedUserId = r.AssignedUserId,
         EntityType = "Invoice", EntityId = invoice.Id, DueAtUtc = r.DueAtUtc };
     db.AdminWorkItems.Add(item); await db.SaveChangesAsync(t); return Ok(item);
 }
}
public sealed record FinanceSettingsRequest(string? TaxRegistrationNumber,string? TaxLabel,decimal TaxRatePercent,int DefaultPaymentTermsDays,bool TaxInclusivePricing,string? InvoiceLogoUrl,string? InvoiceAuthorityName,string? InvoiceAuthorityTitle,string? InvoiceSignatureUrl,string? InvoiceTemplateKey,string? PayslipTemplateKey); public sealed record FinanceSettingsSummary(string TaxRegistrationNumber,string TaxLabel,decimal TaxRatePercent,int DefaultPaymentTermsDays,bool TaxInclusivePricing,string? InvoiceLogoUrl,string? InvoiceAuthorityName,string? InvoiceAuthorityTitle,string? InvoiceSignatureUrl,string InvoiceTemplateKey,string PayslipTemplateKey); public sealed record CollectionFollowUpRequest(string? Note,string? Priority,Guid? AssignedUserId,DateTime? DueAtUtc);
