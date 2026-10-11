using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AcademyDesk.Api.Security;
namespace AcademyDesk.Api.Controllers;
public sealed record CollectionTaskSummary(Guid Id, string Type, string Title, string? Description, string Priority,
    string Status, DateTime? DueAtUtc, string? EscalationStage, DateOnly? PromisedPaymentDate);
public sealed record CollectionInvoiceSummary(Guid Id, string InvoiceNumber, Guid StudentId, decimal TotalAmount,
    decimal AdjustedAmount, decimal PaidAmount, string Currency, DateOnly DueDate, string Status, int DaysOverdue)
{
    public decimal Balance => Math.Max(0, TotalAmount - AdjustedAmount - PaidAmount);
}
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
[HttpGet("settings")]
 public async Task<ActionResult<FinanceSettingsSummary>> Get(Guid academyId, CancellationToken token)
 {
     var row = await db.AcademyFinanceSettings.AsNoTracking().SingleOrDefaultAsync(x => x.AcademyId == academyId, token);
     Response.Headers.CacheControl = "no-store";
     return Ok(SettingsSnapshot(academyId, row));
 }
 [HttpPut("settings")]
 [AtomicAcademyMutation(IncludePlatformOwner = true)]
 public async Task<ActionResult<FinanceSettingsSummary>> Save(Guid academyId, FinanceSettingsRequest r, CancellationToken token)
 {
     if (r.TaxRatePercent < 0 || r.TaxRatePercent > 100 || r.DefaultPaymentTermsDays < 0 || r.DefaultPaymentTermsDays > 365)
         return BadRequest(new { message = "Check tax rate and payment terms." });
     if (!new[] { "Classic", "Modern", "Minimal", "Formal" }.Contains(r.InvoiceTemplateKey ?? "Classic") ||
         !new[] { "Standard", "Compact", "Professional" }.Contains(r.PayslipTemplateKey ?? "Standard"))
         return BadRequest(new { message = "Choose a supported document theme." });
     if (string.IsNullOrWhiteSpace(r.SettingsVersion))
         return StatusCode(StatusCodes.Status428PreconditionRequired, new { message = "Refresh settings before saving. Your draft has not been saved." });
     // Lock the stable academy row even when no settings row exists. The host
     // boundary holds this lock through the settings write and success audit.
     await using var ownedTransaction = db.Database.IsSqlServer() && db.Database.CurrentTransaction is null
         ? await db.Database.BeginTransactionAsync(token) : null;
     var academyQuery = db.Database.IsSqlServer()
         ? db.Academies.FromSqlInterpolated($"SELECT * FROM [Academies] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = {academyId}")
         : db.Academies.Where(x => x.Id == academyId);
     if (await academyQuery.AsNoTracking().SingleOrDefaultAsync(token) is null) return NotFound();
     var row = await db.AcademyFinanceSettings.SingleOrDefaultAsync(x => x.AcademyId == academyId, token);
     if (!string.Equals(r.SettingsVersion, SettingsSnapshot(academyId, row).SettingsVersion, StringComparison.Ordinal))
         return Conflict(new { message = "Settings changed in another window. Your draft has not been saved. Refresh this page and review the latest settings before saving again." });
     if (row is null) { row = new AcademyFinanceSettings { AcademyId = academyId }; db.AcademyFinanceSettings.Add(row); }
     row.TaxRegistrationNumber = r.TaxRegistrationNumber?.Trim() ?? "";
     row.TaxLabel = string.IsNullOrWhiteSpace(r.TaxLabel) ? "GST" : r.TaxLabel.Trim();
     row.TaxRatePercent = r.TaxRatePercent;
     row.DefaultPaymentTermsDays = r.DefaultPaymentTermsDays;
     row.TaxInclusivePricing = r.TaxInclusivePricing;
     row.InvoiceLogoUrl = Clean(r.InvoiceLogoUrl);
     row.InvoiceAuthorityName = Clean(r.InvoiceAuthorityName);
     row.InvoiceAuthorityTitle = Clean(r.InvoiceAuthorityTitle);
     row.InvoiceSignatureUrl = Clean(r.InvoiceSignatureUrl);
     row.InvoiceTemplateKey = r.InvoiceTemplateKey ?? "Classic";
     row.PayslipTemplateKey = r.PayslipTemplateKey ?? "Standard";
     // Advance even for an identical replacement. Include metadata in the token
     // so returning to old field values cannot revive an old editor snapshot.
     row.UpdatedAtUtc = new DateTime(Math.Max(DateTime.UtcNow.Ticks, (row.UpdatedAtUtc?.Ticks ?? 0) + 1), DateTimeKind.Utc);
     await db.SaveChangesAsync(token);
     if (db.Database.IsRelational()) await db.Entry(row).ReloadAsync(token);
     var result = SettingsSnapshot(academyId, row);
     if (ownedTransaction is not null) await ownedTransaction.CommitAsync(token);
     return Ok(result);
 }
 private static FinanceSettingsSummary SettingsSnapshot(Guid academyId, AcademyFinanceSettings? row)
 {
     var summary = row is null ? new FinanceSettingsSummary("", "GST", 0, 7, false, null, null, null, null, "Classic", "Standard") :
         new(row.TaxRegistrationNumber, row.TaxLabel, row.TaxRatePercent, row.DefaultPaymentTermsDays, row.TaxInclusivePricing,
             row.InvoiceLogoUrl, row.InvoiceAuthorityName, row.InvoiceAuthorityTitle, row.InvoiceSignatureUrl, row.InvoiceTemplateKey, row.PayslipTemplateKey);
     // State token, NOT a credential or permission. Canonical decimal formatting
     // avoids different SQL/JSON scale producing different versions.
     var state = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(new { Format = 1, AcademyId = academyId,
         RowId = row?.Id, CreatedTicks = row?.CreatedAtUtc.Ticks, UpdatedTicks = row?.UpdatedAtUtc?.Ticks,
         summary.TaxRegistrationNumber, summary.TaxLabel, Rate = summary.TaxRatePercent.ToString("G29", System.Globalization.CultureInfo.InvariantCulture),
         summary.DefaultPaymentTermsDays, summary.TaxInclusivePricing, summary.InvoiceLogoUrl, summary.InvoiceAuthorityName,
         summary.InvoiceAuthorityTitle, summary.InvoiceSignatureUrl, summary.InvoiceTemplateKey, summary.PayslipTemplateKey });
     return summary with { SettingsVersion = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(state)) };
 }
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
 [HttpGet("summary")] public async Task<ActionResult> Summary(Guid academyId,CancellationToken t){var invoices=await db.Invoices.Where(x=>x.AcademyId==academyId).ToListAsync(t);var payments=await db.Payments.Where(x=>x.AcademyId==academyId&&x.Status!="Voided").ToListAsync(t);return Ok(new{GrossBilled=invoices.Sum(x=>x.TotalAmount),ApprovedAdjustments=invoices.Sum(x=>x.AdjustedAmount),Collected=payments.Sum(x=>x.Amount),Outstanding=Math.Max(0,invoices.Sum(x=>x.TotalAmount-x.AdjustedAmount)-payments.Sum(x=>x.Amount)),Reconciled=payments.Where(x=>x.Status=="Reconciled").Sum(x=>x.Amount),OverdueInvoices=(await OverdueCollections(academyId, DateOnly.FromDateTime(DateTime.UtcNow), t)).Count});}
 [HttpPost("collections/{invoiceId:guid}/follow-up")]
 public async Task<ActionResult> CreateFollowUp(Guid academyId, Guid invoiceId, CollectionFollowUpRequest r, CancellationToken t)
 {
     var invoice = await db.Invoices.AsNoTracking().SingleOrDefaultAsync(x => x.Id == invoiceId && x.AcademyId == academyId, t);
     if (invoice is null) return NotFound();
     var paid = await db.Payments.Where(p => p.AcademyId == academyId && p.InvoiceId == invoiceId && p.Status != "Voided")
         .SumAsync(p => (decimal?)p.Amount, t) ?? 0;
     if (invoice.Status is "Paid" or "Cancelled" || invoice.TotalAmount - invoice.AdjustedAmount - paid <= 0)
         return Conflict(new { message = "This invoice no longer requires collections follow-up." });
     if (!await WorkItemReferences.CanAssignAsync(identity, academyId, r.AssignedUserId, t))
         return BadRequest(new { message = "Choose an active staff member in this academy, or leave the work item unassigned." });
     if (!string.IsNullOrWhiteSpace(r.Priority) && r.Priority.Trim() is not ("Low" or "Normal" or "High" or "Critical"))
         return BadRequest(new { message = "Invalid priority." });
     if (r.Note?.Trim().Length > 4000)
         return BadRequest(new { message = "Follow-up note must not exceed 4000 characters." });
     var item = new AdminWorkItem { AcademyId = academyId, Type = "Collections", Title = $"Collect {invoice.InvoiceNumber}",
         Description = string.IsNullOrWhiteSpace(r.Note) ? $"Overdue invoice {invoice.InvoiceNumber}." : r.Note.Trim(),
         Priority = string.IsNullOrWhiteSpace(r.Priority) ? "High" : r.Priority.Trim(), AssignedUserId = r.AssignedUserId,
         EntityType = "Invoice", EntityId = invoice.Id, DueAtUtc = r.DueAtUtc };
     db.AdminWorkItems.Add(item); await db.SaveChangesAsync(t); return Ok(item);
 }
}
public sealed record FinanceSettingsRequest(string? TaxRegistrationNumber,string? TaxLabel,decimal TaxRatePercent,int DefaultPaymentTermsDays,bool TaxInclusivePricing,string? InvoiceLogoUrl,string? InvoiceAuthorityName,string? InvoiceAuthorityTitle,string? InvoiceSignatureUrl,string? InvoiceTemplateKey,string? PayslipTemplateKey,string? SettingsVersion = null); public sealed record FinanceSettingsSummary(string TaxRegistrationNumber,string TaxLabel,decimal TaxRatePercent,int DefaultPaymentTermsDays,bool TaxInclusivePricing,string? InvoiceLogoUrl,string? InvoiceAuthorityName,string? InvoiceAuthorityTitle,string? InvoiceSignatureUrl,string InvoiceTemplateKey,string PayslipTemplateKey,string? SettingsVersion = null); public sealed record CollectionFollowUpRequest(string? Note,string? Priority,Guid? AssignedUserId,DateTime? DueAtUtc);
