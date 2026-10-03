# BUG-DATA-0017 — Collections queue presents gross invoice amount instead of remaining balance

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | RUNTIME-REPRODUCED / LOCAL-REPAIR-PASS (browser/critical pending) |
| Final verification | Two fresh SQL runs ×30 PASS; API 215/215; controlled TSX 33/33; actual browser/device/critical NOT RUN |
| Severity | Major collection exposure accuracy |
| Priority | P1 |
| Category | DATA |
| Module | FINANCE |
| Role | See reproduction; same-tenant Admin unless stated otherwise |
| Screen / route | /finance-governance; /finance |
| API | GET finance-governance/collections |
| Environment | Local working tree; fresh owned synthetic SQL/Identity/HTTP QA hosts; production state not inferred |
| Device/viewport | NOT RUN; use QA/06_DEVICE_VIEWPORT_MATRIX.md where UI applies |
| Baseline | 20bb6047f9edf733ac8e2a226621cc582ec54b3c + pre-existing student UI diff |
| Discovery test / review ID | FINANCE-COLLECTIONS-001 |
| Evidence classification | Accepted static trace plus real local Identity/HTTP/SQL baseline and regression; controlled TSX, not live browser |
| Preconditions | Isolated synthetic fixture from QA/10_TEST_DATA_STRATEGY.md; never customer data |
| Reproduction frequency | Original mismatch reproduced once; local repair passed twice on fresh SQL/Identity/HTTP runs, 2026-10-01 |
| Source | apps/api/Controllers/FinanceGovernanceController.cs:13 |
| Class/function | Collections; finance-governance overdue queue |
| Related source | INVENTORY/API_ENDPOINTS.md, CONTRACTS.md, FORMS_AND_FIELDS.md and ASYNC_FORM_RISKS.md |
| Root-cause confidence | HIGH |
| Evidence location | Current PHASE_2B_COLLECTIONS_BALANCE_REPAIR.md and source checkpoint; historical static excerpt retained |
| Screenshot | Not captured on pinned baseline |
| Console logs | Not captured; required in retest |
| API request | Seed overdue invoice 1000, non-Voided payment 400 and approved adjustment 100, preserving a collectible status. Compare invoice list balance 500 against collections response and follow-up display. Repeat unpaid and fully settled controls. |
| API response | Baseline gross 1000 with missing collection balance versus canonical 500 reproduced; final queue balance/components, visibility, summary count and follow-up controls PASS twice |
| Database before/after | Fresh-context financial/people/identity/task/audit snapshots unchanged for reads/rejections; successful cent follow-up adds only own linked task and audit |
| Dependencies | Safe SQL/HTTP/browser harness as applicable; desired policy review where noted |
| Fix commit | Local working-tree repair; not committed or deployed |
| Retest result | Original 1000-versus-500 mismatch reproduced; corrected queue/context, cents and zero-balance rejection PASS locally |
| Regression result | 215 API /33 controlled frontend PASS; two fresh SQL runs ×30 PASS; live browser/device/full critical pending |
| Closure notes | Remain OPEN; follow closure requirements in QA README |

## Local repair checkpoint — 2026-10-01

[Remaining Collections balance repair](../REPORTS/PHASE_2B_COLLECTIONS_BALANCE_REPAIR.md) preserves face total and exposes applied adjustment, non-Voided paid amount and remaining balance. Both Finance queues and selected follow-up context show Remaining due with cents. Zero-balance invoices are excluded; current-balance follow-up rejection creates no task/audit. Summary overdue count matches queue; mixed-currency totals remain separate BUG-DATA-0016. Actual baseline mismatch and two fresh SQL runs ×30 PASS, API 215/215 and controlled frontend 33/33; no live browser/critical closure or deployment. Original static excerpt below is historical, not current code.

## Exact reproduction

Seed overdue invoice 1000, non-Voided payment 400 and approved adjustment 100, preserving a collectible status. Compare invoice list balance 500 against collections response and follow-up display. Repeat unpaid and fully settled controls.

## Expected

Collections identifies remaining collectible balance and distinguishes it from original invoice total; follow-up context must not imply already-paid or adjusted amounts are still due.

## Actual / evidence

Collections projects TotalAmount and status/date only, without payments or adjustments. Governance displays that gross amount in the overdue queue and follow-up form. Existing invoice list independently calculates net balance.

Source snapshot:

```text
12:  [HttpPut("settings")] public async Task<ActionResult<FinanceSettingsSummary>> Save(Guid academyId,FinanceSettingsRequest r,CancellationToken t){if(r.TaxRatePercent<0||r.TaxRatePercent>100||r.DefaultPaymentTermsDays<0||r.DefaultPaymentTermsDays>365)return BadRequest(new{message="Check tax rate and payment terms."});if(!new[]{"Classic","Modern","Minimal","Formal"}.Contains(r.InvoiceTemplateKey??"Classic")||!new[]{"Standard","Compact","Professional"}.Contains(r.PayslipTemplateKey??"Standard"))return BadRequest(new{message="Choose a supported document theme."});var x=await db.AcademyFinanceSettings.SingleOrDefaultAsync(x=>x.AcademyId==academyId,t);if(x is null){x=new AcademyFinanceSettings{AcademyId=academyId};db.AcademyFinanceSettings.Add(x);}x.TaxRegistrationNumber=r.TaxRegistrationNumber?.Trim()??"";x.TaxLabel=string.IsNullOrWhiteSpace(r.TaxLabel)?"GST":r.TaxLabel.Trim();x.TaxRatePercent=r.TaxRatePercent;x.DefaultPaymentTermsDays=r.DefaultPaymentTermsDays;x.TaxInclusivePricing=r.TaxInclusivePricing;x.InvoiceLogoUrl=Clean(r.InvoiceLogoUrl);x.InvoiceAuthorityName=Clean(r.InvoiceAuthorityName);x.InvoiceAuthorityTitle=Clean(r.InvoiceAuthorityTitle);x.InvoiceSignatureUrl=Clean(r.InvoiceSignatureUrl);x.InvoiceTemplateKey=r.InvoiceTemplateKey??"Classic";x.PayslipTemplateKey=r.PayslipTemplateKey??"Standard";await db.SaveChangesAsync(t);return Ok(new FinanceSettingsSummary(x.TaxRegistrationNumber,x.TaxLabel,x.TaxRatePercent,x.DefaultPaymentTermsDays,x.TaxInclusivePricing,x.InvoiceLogoUrl,x.InvoiceAuthorityName,x.InvoiceAuthorityTitle,x.InvoiceSignatureUrl,x.InvoiceTemplateKey,x.PayslipTemplateKey));}
13:  [HttpGet("collections")] public async Task<ActionResult> Collections(Guid academyId,CancellationToken t)=>Ok(await db.Invoices.AsNoTracking().Where(x=>x.AcademyId==academyId&&x.Status!="Paid"&&x.Status!="Cancelled"&&x.DueDate<DateOnly.FromDateTime(DateTime.UtcNow)).OrderBy(x=>x.DueDate).Select(x=>new{x.Id,x.InvoiceNumber,x.StudentId,x.TotalAmount,x.Currency,x.DueDate,x.Status,DaysOverdue=DateOnly.FromDateTime(DateTime.UtcNow).DayNumber-x.DueDate.DayNumber}).ToListAsync(t));
14:  [HttpGet("summary")] public async Task<ActionResult> Summary(Guid academyId,CancellationToken t){var invoices=await db.Invoices.Where(x=>x.AcademyId==academyId).ToListAsync(t);var payments=await db.Payments.Where(x=>x.AcademyId==academyId&&x.Status!="Voided").ToListAsync(t);return Ok(new{GrossBilled=invoices.Sum(x=>x.TotalAmount),ApprovedAdjustments=invoices.Sum(x=>x.AdjustedAmount),Collected=payments.Sum(x=>x.Amount),Outstanding=Math.Max(0,invoices.Sum(x=>x.TotalAmount-x.AdjustedAmount)-payments.Sum(x=>x.Amount)),Reconciled=payments.Where(x=>x.Status=="Reconciled").Sum(x=>x.Amount),OverdueInvoices=invoices.Count(x=>x.Status!="Paid"&&x.Status!="Cancelled"&&x.DueDate<DateOnly.FromDateTime(DateTime.UtcNow))});}
15:  [HttpPost("collections/{invoiceId:guid}/follow-up")] public async Task<ActionResult> CreateFollowUp(Guid academyId,Guid invoiceId,CollectionFollowUpRequest r,CancellationToken t){var invoice=await db.Invoices.SingleOrDefaultAsync(x=>x.Id==invoiceId&&x.AcademyId==academyId,t);if(invoice is null)return NotFound();if(invoice.Status is "Paid" or "Cancelled")return Conflict(new{message="This invoice no longer requires collections follow-up."});var item=new AdminWorkItem{AcademyId=academyId,Type="Collections",Title=$"Collect {invoice.InvoiceNumber}",Description=string.IsNullOrWhiteSpace(r.Note)?$"Overdue invoice {invoice.InvoiceNumber}.":r.Note.Trim(),Priority=string.IsNullOrWhiteSpace(r.Priority)?"High":r.Priority.Trim(),AssignedUserId=r.AssignedUserId,EntityType="Invoice",EntityId=invoice.Id,DueAtUtc=r.DueAtUtc};db.AdminWorkItems.Add(item);await db.SaveChangesAsync(t);return Ok(item);}
```

## Suspected root cause

Collections uses invoice face value rather than the balance contract already exposed elsewhere.

## Business impact and blast radius

Partial-payment/approved-adjustment collection work and overdue displays. Stale Paid status after void remains separate BUG-DATA-0010.

## Related / required regression

FINANCE-COLLECTIONS-001: Independent balance oracle with Completed/Reconciled/Voided payments, approved/rejected adjustments, no payments, zero balance, due today/overdue and currency; verify queue and follow-up amounts agree with invoice details.

Also run all endpoint/form cases pointing to this issue in QA/03_TEST_MATRIX.md and the critical regression suite before closure.
