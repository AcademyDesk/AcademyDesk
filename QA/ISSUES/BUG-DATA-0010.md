# BUG-DATA-0010 — Payment status transitions leave invoice state and reconciliation evidence inconsistent

| Field | Value |
| --- | --- |
| Status | OPEN / FIX-IN-PROGRESS |
| Confirmation status | RUNTIME-REPRODUCED; local void/evidence repair retested |
| Final verification | Sequential void/evidence PASS twice; concurrent two-row void baseline FAIL 0/5, repaired two-void and create/void gross/adjusted retests PASS 5/5 each; restoration policy / other interleavings / broader gates pending |
| Severity | Critical financial ledger consistency |
| Priority | P0 |
| Category | DATA |
| Module | FINANCE |
| Role | See reproduction; same-tenant Admin unless stated otherwise |
| Screen / route | /payments; /finance-reconciliation; /finance-governance |
| API | PATCH payments/{paymentId}/status; PATCH payments/{paymentId}/reconcile |
| Environment | Two fresh local run-owned Docker SQL/TestServer fixtures; production state not inferred |
| Device/viewport | NOT RUN; use QA/06_DEVICE_VIEWPORT_MATRIX.md where UI applies |
| Baseline | 20bb6047f9edf733ac8e2a226621cc582ec54b3c + pre-existing student UI diff |
| Discovery test / review ID | FINANCE-RULE-003 |
| Evidence classification | Static trace confirmed by authenticated HTTP, fresh SQL, invoice/payment lists and collections reads |
| Preconditions | Isolated synthetic fixture from QA/10_TEST_DATA_STRATEGY.md; never customer data |
| Reproduction frequency | 2/2 fresh runs, 2026-09-30; independent full/partial/adjusted/direct-transition fixtures |
| Source | apps/api/Controllers/PaymentsController.cs:34 |
| Class/function | UpdateStatus / Reconcile |
| Related source | INVENTORY/API_ENDPOINTS.md, CONTRACTS.md, FORMS_AND_FIELDS.md and ASYNC_FORM_RISKS.md |
| Root-cause confidence | HIGH |
| Evidence location | QA/REPORTS/PHASE_2A_PAYMENT_TRANSITION_REPRO.md; retained Phase 1 source excerpt below |
| Screenshot | Not captured on pinned baseline |
| Console logs | Original FAIL report retained; local repair logs linked in Phase 2B checkpoint below |
| API request | In isolated SQL create invoice 1000 and Completed payment 1000, making invoice Paid. PATCH payment status Voided. Read invoice summary and overdue collections. Separately PATCH a fresh Completed payment to Reconciled via status without reference; try reconcile on a Voided payment. |
| API response | Void 200 leaves stale invoice status; full/adjusted invoices absent from collections; direct Reconciled 200 lacks reference/timestamp |
| Database before/after | Fresh SQL confirms full/adjusted Paid and partial PartiallyPaid after void; direct Reconciled lacks evidence; rejected controls unchanged; restoration policy pending |
| Dependencies | Safe SQL/HTTP/browser harness as applicable; desired policy review where noted |
| Fix commit | Local uncommitted PaymentsController void/evidence repair; no deployment |
| Retest result | Confirmed void/evidence cases PASS on two fresh owned Identity/HTTP/SQL runs; restoration POLICY-PENDING |
| Regression result | API 134/134 and bounded SQL transition module PASS twice; not full critical / concurrent / browser acceptance |
| Closure notes | Remain OPEN; restoration policy, permissions/recomputation and broader closure requirements pending |

## Local repair checkpoint — 2026-10-01

2026-10-03 [concurrent void repair](../REPORTS/PHASE_2B_PAYMENT_VOID_RACE_REPAIR.md): two simultaneous voids on a Paid invoice reproduced a stored `PartiallyPaid` status despite zero collected in 5/5 fresh SQL/HTTP baseline pairs. The Voided path now locks the academy invoice row in a transaction before re-reading the payment and recalculating the aggregate. Final five-pair two-void and create/void matrices (including approved adjustments) each PASS 5/5; existing Transition and CollectionRace regressions and 1,042 API tests PASS. Restoration policy and other transition interleavings remain unaccepted; issue stays OPEN.

[Phase 2B repair/evidence](../REPORTS/PHASE_2B_PAYMENT_TRANSITION_REPAIR.md): void updates invoice using remaining Completed/Reconciled amounts and approved adjustment; repeat void is no-write; cancelled status and historic reconciliation evidence preserved. Generic Reconciled changes reject in favor of the dedicated reference-taking action. Before-fix 16 unit cases: 6 PASS/10 FAIL; final API 134/134 and original-case/module SQL PASS twice on fresh owned runs. Resources cleaned; no commit/push/Azure or normal dev database changes.

Voided restoration deliberately unchanged and unaccepted pending human policy: dedicated reconcile still returns 200 with evidence but does not recompute invoice; generic Voided→Completed also remains outside accepted coverage. No concurrency, exhaustive role/tenant/browser/device, cancelled collection-normalization or full critical PASS claimed. Related read-view follow-ups remain queued. The original reproduction below is historical before-fix evidence, not the current confirmed void/evidence behavior.

## Original reproduction (before fix)

## Exact reproduction

In isolated SQL create invoice 1000 and Completed payment 1000, making invoice Paid. PATCH payment status Voided. Read invoice summary and overdue collections. Separately PATCH a fresh Completed payment to Reconciled via status without reference; try reconcile on a Voided payment.

## Expected

Allowed transitions preserve invoice balance/status agreement; voided full payment reopens applicable invoice status; reconciled state has required evidence; invalid transitions reject without writes.

## Actual / evidence

UpdateStatus changes payment.Status only. Two fresh synthetic HTTP/SQL runs confirmed full/adjusted invoices remain Paid after voiding while balances reopen to 1000/800 and collections omit them. A partial invoice remains PartiallyPaid with zero collected and balance 1000. Generic Reconciled status persists without reference/timestamp, visible in payment-list GET. Invalid status, blank dedicated reference and repeated void controls preserved their captured financial snapshots. Dedicated reconciliation adds valid evidence. Reconciling a Voided payment was accepted with evidence, but restoration permission/policy is explicitly PENDING rather than declared invalid without a business decision. No production ledger mutation or product repair was made.

Source snapshot:

```text
33:     [HttpPatch("{paymentId:guid}/status")]
34:     public async Task<ActionResult> UpdateStatus(Guid academyId, Guid paymentId, UpdatePaymentStatusRequest request, CancellationToken token)
35:     { var x=await dbContext.Payments.SingleOrDefaultAsync(v=>v.Id==paymentId&&v.AcademyId==academyId,token); if(x is null)return NotFound(); if(request.Status is not("Completed" or "Reconciled" or "Voided"))return BadRequest(new { message = "Status must be Completed, Reconciled, or Voided." }); x.Status=request.Status; await dbContext.SaveChangesAsync(token); return Ok(); }
36:     [HttpPatch("{paymentId:guid}/reconcile")]
```

## Suspected root cause

Independent generic status and reconciliation paths bypass aggregate recomputation and transition/evidence invariants.

## Business impact and blast radius

Invoice/collection status, payment ledger evidence, amounts counted in read views; distinct from BUG-DATA-0001 Completed-only aggregation.

## Related / required regression

FINANCE-RULE-003: Real SQL/HTTP transition matrix Completed/Reconciled/Voided; full/partial payments, adjustments, repeated transitions and concurrent change; assert invoice state, fresh balances, unchanged rejected rows and reconciliation evidence.

Also run all endpoint/form cases pointing to this issue in QA/03_TEST_MATRIX.md and the critical regression suite before closure.
