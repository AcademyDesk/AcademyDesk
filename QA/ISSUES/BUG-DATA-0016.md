# BUG-DATA-0016 — Finance and platform summaries combine currencies and present totals as INR

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | STATIC-FINDING |
| Final verification | NOT RUN |
| Severity | Major financial reporting integrity |
| Priority | P1 |
| Category | DATA |
| Module | FINANCE |
| Role | See reproduction; same-tenant Admin unless stated otherwise |
| Screen / route | /finance; /platform |
| API | GET finance-governance/summary; GET /api/platform/overview |
| Environment | Source review of local working tree; runtime production state not inferred |
| Device/viewport | NOT RUN; use QA/06_DEVICE_VIEWPORT_MATRIX.md where UI applies |
| Baseline | 20bb6047f9edf733ac8e2a226621cc582ec54b3c + pre-existing student UI diff |
| Discovery test / review ID | FINANCE-CURRENCY-001 |
| Evidence classification | Static trace; runtime reproduction pending |
| Preconditions | Isolated synthetic fixture from QA/10_TEST_DATA_STRATEGY.md; never customer data |
| Reproduction frequency | Not measured; reproduction instructions are proposed |
| Source | apps/api/Controllers/FinanceGovernanceController.cs:14 |
| Class/function | FinanceGovernance.Summary; PlatformControl.Overview; finance/platform cards |
| Related source | INVENTORY/API_ENDPOINTS.md, CONTRACTS.md, FORMS_AND_FIELDS.md and ASYNC_FORM_RISKS.md |
| Root-cause confidence | HIGH |
| Evidence location | Source excerpt below; source fingerprint in INVENTORY/SOURCE_MANIFEST.md |
| Screenshot | Not captured on pinned baseline |
| Console logs | Not captured; required in retest |
| API request | In disposable fixtures create same-academy fee plans/invoices of INR 1000 and USD 100, with matching-currency payments. Read summary and finance cards; compare single-currency controls. Repeat with platform billing invoices in INR and USD and compare PlatformControl Overview with platform cards. |
| API response | Not captured for this issue; use synthetic request/response in isolated reproduction |
| Database before/after | Not executed; fixture and fresh-context assertions defined below |
| Dependencies | Safe SQL/HTTP/browser harness as applicable; desired policy review where noted |
| Fix commit | Not implemented (Phase 1) |
| Retest result | NOT RUN |
| Regression result | NOT RUN |
| Closure notes | Remain OPEN; follow closure requirements in QA README |

## Exact reproduction

In disposable fixtures create same-academy fee plans/invoices of INR 1000 and USD 100, with matching-currency payments. Read summary and finance cards; compare single-currency controls. Repeat with platform billing invoices in INR and USD and compare PlatformControl Overview with platform cards.

## Expected

Money is grouped by currency or converted using an explicit supported rate/date policy; unlike currencies are never summed as one INR amount.

## Actual / evidence

Summary sums invoice/payment decimal amounts without grouping or currency metadata. FinancePage calls money(summary values) with default INR. Fee-plan and invoice paths permit distinct currencies within one academy. PlatformControl Overview also sums unlike currencies, and platform money formatter hardcodes INR. Source finding; runtime NOT RUN.

Source snapshot:

```text
13:  [HttpGet("collections")] public async Task<ActionResult> Collections(Guid academyId,CancellationToken t)=>Ok(await db.Invoices.AsNoTracking().Where(x=>x.AcademyId==academyId&&x.Status!="Paid"&&x.Status!="Cancelled"&&x.DueDate<DateOnly.FromDateTime(DateTime.UtcNow)).OrderBy(x=>x.DueDate).Select(x=>new{x.Id,x.InvoiceNumber,x.StudentId,x.TotalAmount,x.Currency,x.DueDate,x.Status,DaysOverdue=DateOnly.FromDateTime(DateTime.UtcNow).DayNumber-x.DueDate.DayNumber}).ToListAsync(t));
14:  [HttpGet("summary")] public async Task<ActionResult> Summary(Guid academyId,CancellationToken t){var invoices=await db.Invoices.Where(x=>x.AcademyId==academyId).ToListAsync(t);var payments=await db.Payments.Where(x=>x.AcademyId==academyId&&x.Status!="Voided").ToListAsync(t);return Ok(new{GrossBilled=invoices.Sum(x=>x.TotalAmount),ApprovedAdjustments=invoices.Sum(x=>x.AdjustedAmount),Collected=payments.Sum(x=>x.Amount),Outstanding=Math.Max(0,invoices.Sum(x=>x.TotalAmount-x.AdjustedAmount)-payments.Sum(x=>x.Amount)),Reconciled=payments.Where(x=>x.Status=="Reconciled").Sum(x=>x.Amount),OverdueInvoices=invoices.Count(x=>x.Status!="Paid"&&x.Status!="Cancelled"&&x.DueDate<DateOnly.FromDateTime(DateTime.UtcNow))});}
15:  [HttpPost("collections/{invoiceId:guid}/follow-up")] public async Task<ActionResult> CreateFollowUp(Guid academyId,Guid invoiceId,CollectionFollowUpRequest r,CancellationToken t){var invoice=await db.Invoices.SingleOrDefaultAsync(x=>x.Id==invoiceId&&x.AcademyId==academyId,t);if(invoice is null)return NotFound();if(invoice.Status is "Paid" or "Cancelled")return Conflict(new{message="This invoice no longer requires collections follow-up."});var item=new AdminWorkItem{AcademyId=academyId,Type="Collections",Title=$"Collect {invoice.InvoiceNumber}",Description=string.IsNullOrWhiteSpace(r.Note)?$"Overdue invoice {invoice.InvoiceNumber}.":r.Note.Trim(),Priority=string.IsNullOrWhiteSpace(r.Priority)?"High":r.Priority.Trim(),AssignedUserId=r.AssignedUserId,EntityType="Invoice",EntityId=invoice.Id,DueAtUtc=r.DueAtUtc};db.AdminWorkItems.Add(item);await db.SaveChangesAsync(t);return Ok(item);}
16: }
```

## Suspected root cause

Currency is discarded during aggregation and a presentation default supplies a misleading unit.

## Business impact and blast radius

Gross billed, collected, outstanding and reconciled finance cards for multi-currency academies and platform billing across academies.

## Related / required regression

FINANCE-CURRENCY-001: Real SQL/HTTP plus browser assertions for INR-only, USD-only and mixed currencies; verify exact decimal totals and displayed units without inventing exchange rates.

Also run all endpoint/form cases pointing to this issue in QA/03_TEST_MATRIX.md and the critical regression suite before closure.
