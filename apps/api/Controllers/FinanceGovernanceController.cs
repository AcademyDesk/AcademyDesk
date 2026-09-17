using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace AcademyDesk.Api.Controllers;
[ApiController]
[Route("api/academies/{academyId:guid}/finance-governance")]
public sealed class FinanceGovernanceController(AcademyDeskDbContext db) : ControllerBase
{
 [HttpGet("settings")] public async Task<ActionResult<FinanceSettingsSummary>> Get(Guid academyId,CancellationToken t){var x=await db.AcademyFinanceSettings.AsNoTracking().SingleOrDefaultAsync(x=>x.AcademyId==academyId,t);return Ok(x is null?new FinanceSettingsSummary("","GST",0,7,false):new(x.TaxRegistrationNumber,x.TaxLabel,x.TaxRatePercent,x.DefaultPaymentTermsDays,x.TaxInclusivePricing));}
 [HttpPut("settings")] public async Task<ActionResult<FinanceSettingsSummary>> Save(Guid academyId,FinanceSettingsRequest r,CancellationToken t){if(r.TaxRatePercent<0||r.TaxRatePercent>100||r.DefaultPaymentTermsDays<0||r.DefaultPaymentTermsDays>365)return BadRequest(new{message="Check tax rate and payment terms."});var x=await db.AcademyFinanceSettings.SingleOrDefaultAsync(x=>x.AcademyId==academyId,t);if(x is null){x=new AcademyFinanceSettings{AcademyId=academyId};db.AcademyFinanceSettings.Add(x);}x.TaxRegistrationNumber=r.TaxRegistrationNumber?.Trim()??"";x.TaxLabel=string.IsNullOrWhiteSpace(r.TaxLabel)?"GST":r.TaxLabel.Trim();x.TaxRatePercent=r.TaxRatePercent;x.DefaultPaymentTermsDays=r.DefaultPaymentTermsDays;x.TaxInclusivePricing=r.TaxInclusivePricing;await db.SaveChangesAsync(t);return Ok(new FinanceSettingsSummary(x.TaxRegistrationNumber,x.TaxLabel,x.TaxRatePercent,x.DefaultPaymentTermsDays,x.TaxInclusivePricing));}
 [HttpGet("collections")] public async Task<ActionResult> Collections(Guid academyId,CancellationToken t)=>Ok(await db.Invoices.AsNoTracking().Where(x=>x.AcademyId==academyId&&x.Status!="Paid"&&x.Status!="Cancelled"&&x.DueDate<DateOnly.FromDateTime(DateTime.UtcNow)).OrderBy(x=>x.DueDate).Select(x=>new{x.Id,x.InvoiceNumber,x.StudentId,x.TotalAmount,x.Currency,x.DueDate,x.Status,DaysOverdue=DateOnly.FromDateTime(DateTime.UtcNow).DayNumber-x.DueDate.DayNumber}).ToListAsync(t));
}
public sealed record FinanceSettingsRequest(string? TaxRegistrationNumber,string? TaxLabel,decimal TaxRatePercent,int DefaultPaymentTermsDays,bool TaxInclusivePricing); public sealed record FinanceSettingsSummary(string TaxRegistrationNumber,string TaxLabel,decimal TaxRatePercent,int DefaultPaymentTermsDays,bool TaxInclusivePricing);
