# BUG-DATA-0055 — Direct invoice status edit can mark an unpaid invoice Paid

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | RUNTIME-CONFIRMED; bounded repair verified, RETEST |
| Final verification | Retained111/45/20+9 gates; six native stages/four linked journeys60 page-states/16 mutations/monetary display PASS; wider gates OPEN |
| Severity | Major invoice-ledger status integrity |
| Priority | P1 |
| Category | DATA |
| Module | FEES |
| Role | See reproduction; same-tenant Admin unless stated otherwise |
| Screen / route | /invoices; direct invoice API |
| API | PATCH /api/academies/{academyId}/invoices/{invoiceId}/status |
| Environment | D:\AcademyDesk-codex-p0, codex/penta-search; real Identity/HTTP/disposable SQL; production state not inferred |
| Device/viewport | Headless Edge320/1440px/light-dark linked consumer states; physical Android/iOS NOT RUN |
| Baseline | 20bb6047f9edf733ac8e2a226621cc582ec54b3c + pre-existing student UI diff |
| Discovery test / review ID | FINANCE-INVOICE-STATUS-001 |
| Evidence classification | Runtime reproduction on frozen API plus controlled tests; local evidence retained |
| Preconditions | Isolated synthetic fixture from QA/10_TEST_DATA_STRATEGY.md; never customer data |
| Reproduction frequency | Frozen native baseline reproduced Paid/paid; successor20/20 overlapped pairs PASS, bounded modes only |
| Source | apps/api/Controllers/InvoicesController.cs:30 |
| Class/function | Invoices.UpdateStatus |
| Related source | INVENTORY/API_ENDPOINTS.md, CONTRACTS.md, FORMS_AND_FIELDS.md and ASYNC_FORM_RISKS.md |
| Root-cause confidence | HIGH |
| Evidence location | QA/REPORTS/PHASE_2B_INVOICE_STATUS_LEDGER_REPAIR.md; PHASE_2B_INVOICE_STATUS_CONCURRENCY_AUDIT.md; PHASE_2B_INVOICE_CONSUMERS_LINKED.md; local QA/EVIDENCE/invoice-status-20261011, invoice-integrity-20261011 and invoice-consumers-20261012 |
| Screenshot | Not captured on pinned baseline |
| Console logs | Linked four consumer journeys: zero console/page errors; wider workflows pending |
| API request | In an isolated academy create an invoice for 1000 with no payments. PATCH status to Paid, then read invoice list, finance overview and any collection/reminder projections. Repeat lowercase paid, Cancelled on a paid invoice, malformed status, wrong academy, and a legitimately fully collected control. |
| API response | Frozen unpaid Paid/paid200; repaired contradictions400, canonical accepted status200;45 final strict controls and retained transitions PASS/no429 |
| Database before/after | Frozen Paid with collected0/balance1000 reproduced; final matrix checks fresh full invoice/payment/adjustment/audit snapshots |
| Dependencies | Safe SQL/HTTP/browser harness as applicable; desired policy review where noted |
| Fix commit | ee1e5a123d9683c8074118136f2fbebc326c9b9d; QA-only successor follows this repair |
| Retest result | OPEN/RETEST; bounded concurrency/audit/native+linked consumer and fractional/gross-vs-due display PASS; device/direct/platform/stale/critical/release/policy pending |
| Regression result | Prior111/45/20+9 evidence retained; latest run85dbbb61fb434106b2a1b9ef73b9399d six native/four linked journeys60 states/16 mutations/480 non-mutation snapshots and display PASS,500.7s/no429/exact owned cleanup; wider gates OPEN |
| Closure notes | Remain OPEN; follow closure requirements in QA README |

## Exact reproduction

2026-10-12 monetary-display successor: [report](../REPORTS/PHASE_2B_INVOICE_MONEY_DISPLAY_REPAIR.md) records Student currency minor-unit preservation and four-line server-backed invoice total/adjustment/collected/remaining summary.16 source/render checks (frozen4/16), types/export83/harness0/0 and six retained native/four linked journeys60 page-states/20 UI downloads/16 mutations/480 unchanged finance snapshots/exact audits/foreign sentinel/no429 PASS. Run85dbbb61fb434106b2a1b9ef73b9399d,500.7s/88+7 migrations/exact owned cleanup; five inherited lint warnings and initial build/command failures disclosed. Current bounded fractional/zero and gross-vs-due presentation accepted; older paragraph below preserves discovery history. Mixed-currency aggregate, stale/direct/platform/device/wider visual/critical/release/policy gates remain separate; issue OPEN.

2026-10-12 consumer successor: [report](../REPORTS/PHASE_2B_INVOICE_CONSUMERS_LINKED.md) records six retained native stages strengthened with status/document assertions, four real browser journeys/24 page-states, eight real UI downloads,16 API mutations (12 success/four400 no-write),200 unchanged four-finance-table controls/exact success audit/positive foreign sentinel/no429. Final run1787e8dba86e45c4b7c371e502d704a9,265s/88+7 migrations/exact owned cleanup; two selector/document-grouping harness failures retained/corrected without tolerance or app changes. **OPEN follow-ups:** Student0.01 renders0 due integer currency formatting; invoice preview labels gross amount "Total payable" even after settlement. Current integer status/consumer gate is not acceptance of those monetary displays. Direct/platform/stale/device/critical/release and policy gates remain separate; issue not closed.

2026-10-12 IST QA-only successor: [concurrency/audit report](../REPORTS/PHASE_2B_INVOICE_STATUS_CONCURRENCY_AUDIT.md) records20/20 real SQL-overlapped status-vs-collection/void/approval/restoration pairs and9/9 audit rollback/rejection/recovery controls,111 targeted tests PASS. Four-table full finance snapshots, actor/tenant/action/route audits and invoice-register parity checked; initial harness monitoring/order/decimal-scale failures retained and corrected without weakening ledger assertions. No application or policy change;0055 remains OPEN for wider gates.

2026-10-11 successor: frozen API from parent8c025cf accepts Paid200 with collected0/balance1000 and then stores paid200 noncanonically; owned SQL dc656f4eefe849c0b894b5594ba596b3,47.9s, normal cleanup. Repaired final run17b128d0bca34d6389728339cabe25e6 passes45 strict controls and retained transition regression,134.8s/no429/exact owned cleanup. Original static excerpt below is historical, not current repaired source. [Bounded report](../REPORTS/PHASE_2B_INVOICE_STATUS_LEDGER_REPAIR.md) records failed fixture/rate-limit runs without treating them as full passes. No policy approval or issue closure inferred.

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
