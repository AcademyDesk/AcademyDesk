# BUG-DATA-0055 — Direct invoice status edit can mark an unpaid invoice Paid

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | STATIC-FINDING |
| Final verification | NOT RUN |
| Severity | Major invoice-ledger status integrity |
| Priority | P1 |
| Category | DATA |
| Module | FEES |
| Role | See reproduction; same-tenant Admin unless stated otherwise |
| Screen / route | /invoices; direct invoice API |
| API | PATCH /api/academies/{academyId}/invoices/{invoiceId}/status |
| Environment | Source review of local working tree; runtime production state not inferred |
| Device/viewport | NOT RUN; use QA/06_DEVICE_VIEWPORT_MATRIX.md where UI applies |
| Baseline | 20bb6047f9edf733ac8e2a226621cc582ec54b3c + pre-existing student UI diff |
| Discovery test / review ID | FINANCE-INVOICE-STATUS-001 |
| Evidence classification | Static trace; runtime reproduction pending |
| Preconditions | Isolated synthetic fixture from QA/10_TEST_DATA_STRATEGY.md; never customer data |
| Reproduction frequency | Not measured; reproduction instructions are proposed |
| Source | apps/api/Controllers/InvoicesController.cs:30 |
| Class/function | Invoices.UpdateStatus |
| Related source | INVENTORY/API_ENDPOINTS.md, CONTRACTS.md, FORMS_AND_FIELDS.md and ASYNC_FORM_RISKS.md |
| Root-cause confidence | HIGH |
| Evidence location | Source excerpt below; source fingerprint in INVENTORY/SOURCE_MANIFEST.md |
| Screenshot | Not captured on pinned baseline |
| Console logs | Not captured; required in retest |
| API request | In an isolated academy create an invoice for 1000 with no payments. PATCH status to Paid, then read invoice list, finance overview and any collection/reminder projections. Repeat lowercase paid, Cancelled on a paid invoice, malformed status, wrong academy, and a legitimately fully collected control. |
| API response | Not captured for this issue; use synthetic request/response in isolated reproduction |
| Database before/after | Not executed; fixture and fresh-context assertions defined below |
| Dependencies | Safe SQL/HTTP/browser harness as applicable; desired policy review where noted |
| Fix commit | Not implemented (Phase 1) |
| Retest result | NOT RUN |
| Regression result | NOT RUN |
| Closure notes | Remain OPEN; follow closure requirements in QA README |

## Exact reproduction

In an isolated academy create an invoice for 1000 with no payments. PATCH status to Paid, then read invoice list, finance overview and any collection/reminder projections. Repeat lowercase paid, Cancelled on a paid invoice, malformed status, wrong academy, and a legitimately fully collected control.

## Expected

Invoice status reflects the authoritative ledger or an explicit audited override policy; an unpaid invoice cannot be reported as Paid solely through an unconstrained status label. Invalid or contradictory transitions leave invoice and related rows unchanged.

## Actual / evidence

UpdateStatus only checks a case-insensitive status allowlist, then stores the submitted trimmed string. It does not inspect payments, approved adjustments or existing status. A direct caller with finance.manage can mark an unpaid invoice Paid; lowercase paid is stored noncanonically and can evade case-sensitive downstream comparisons. Static source trace; runtime NOT RUN.

Source snapshot:

```text
29:     [HttpPatch("{invoiceId:guid}/status")]
30:     public async Task<ActionResult> UpdateStatus(Guid academyId, Guid invoiceId, UpdateInvoiceStatusRequest request, CancellationToken token)
31:     { var x=await dbContext.Invoices.SingleOrDefaultAsync(v=>v.Id==invoiceId&&v.AcademyId==academyId,token); if(x is null)return NotFound(); if(!new[]{"Issued","PartiallyPaid","Paid","Overdue","Cancelled"}.Contains(request.Status,StringComparer.OrdinalIgnoreCase))return BadRequest(); x.Status=request.Status.Trim(); await dbContext.SaveChangesAsync(token); return Ok(); }
32: }
```

## Suspected root cause

Standalone status endpoint applies vocabulary validation without ledger invariants or canonicalization.

## Business impact and blast radius

Invoice register, collections and reminder eligibility, portal-facing status and financial reporting for direct API users. Distinct from payment-status transition inconsistency BUG-DATA-0010.

## Related / required regression

FINANCE-INVOICE-STATUS-001: Full HTTP/SQL status-by-balance matrix with no/partial/full payments, reconciled/voided payments and adjustments, case variants, cancellation and role/tenant denials. Fresh read must agree across invoice, collection and portal projections.

Also run all endpoint/form cases pointing to this issue in QA/03_TEST_MATRIX.md and the critical regression suite before closure.
