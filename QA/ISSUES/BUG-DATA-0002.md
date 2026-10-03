# BUG-DATA-0002 — Payment guard ignores approved invoice adjustments

| Field | Value |
| --- | --- |
| Status | OPEN / FIX-IN-PROGRESS (local guard/status and consumer repair) |
| Confirmation status | RUNTIME-REPRODUCED |
| Final verification | Local guard and consumer modules PASS twice each; approved-adjustment collection race PASS 5/5; concurrent approval/collection, broader/critical/browser gates pending |
| Severity | Critical financial integrity |
| Priority | P0 |
| Category | DATA |
| Module | FINANCE |
| Role | See reproduction; same-tenant Admin unless stated otherwise |
| Screen / route | /payments; /finance-adjustments |
| API | POST payments |
| Environment | Two fresh local run-owned Docker SQL/TestServer fixtures; production state not inferred |
| Device/viewport | Real browser desktop and emulated 390×844 PASS locally; physical Android/iOS NOT RUN |
| Baseline | 20bb6047f9edf733ac8e2a226621cc582ec54b3c + pre-existing student UI diff |
| Discovery test / review ID | FINANCE-RULE-002 |
| Evidence classification | Static trace confirmed by real authenticated HTTP and fresh SQL snapshots |
| Preconditions | Isolated synthetic fixture from QA/10_TEST_DATA_STRATEGY.md; never customer data |
| Reproduction frequency | 2/2 counted fresh runs, 2026-09-30; initial missing-module fixture failure excluded |
| Source | apps/api/Controllers/PaymentsController.cs:27 |
| Class/function | Create |
| Related source | INVENTORY/API_ENDPOINTS.md, CONTRACTS.md, FORMS_AND_FIELDS.md and ASYNC_FORM_RISKS.md |
| Root-cause confidence | HIGH |
| Evidence location | QA/REPORTS/PHASE_2A_INVOICE_ADJUSTMENT_REPRO.md; retained Phase 1 source excerpt below |
| Screenshot | Not captured on pinned baseline |
| Console logs | Original Phase 2A FAIL retained; local repair/retest evidence in Phase 2B report below |
| API request | Seed invoice 1000, approved adjustment 200 and paid 600. Submit payment 300; inspect fresh invoice/payment state. |
| API response | Extra 300 at balance 200 accepted 201; exact 200 accepted but status PartiallyPaid; extra 1 after full adjustment accepted 201 |
| Database before/after | Fresh SQL confirms overcollection, exact settlement with wrong status and full-adjustment Paid→PartiallyPaid; invalid/repeated approval snapshots unchanged |
| Dependencies | Safe SQL/HTTP/browser harness as applicable; desired policy review where noted |
| Fix commit | Local uncommitted guard and balance-consumer repair, 2026-10-01; no deployment |
| Retest result | Three original variants PASS in two fresh guarded Identity/HTTP/SQL runs |
| Regression result | Latest API 140/140, controlled page 7/7 and consumer HTTP/SQL six stages PASS twice; earlier guard module evidence retained; full critical NOT RUN |
| Closure notes | Remain OPEN; follow closure requirements in QA README |

## Latest consumer repair checkpoint — 2026-10-01

2026-10-03 [Live Payments browser check](../REPORTS/PHASE_2B_PAYMENTS_BROWSER_CHECK.md): an approved 200 discount on gross 1000 and Reconciled 600 displayed a 200 balance with 800 due; a stale 200 browser submit after another client paid returned the exact balance error, refreshed to Paid/0, and final isolated SQL asserted exactly two rows and collected 800. Approval-versus-collection concurrency, physical devices and broader gates remain OPEN.

2026-10-03 [P0 continuation](../REPORTS/PHASE_2B_P0_CONTINUATION.md): a new real SQL/Identity/HTTP five-pair race with the adjustment approved *before* collection passes 5/5. It verifies the existing invoice lock also protects the reduced collectible balance from two simultaneous payments. It does not establish policy or safety for an approval racing a payment; that remains OPEN.

[Consumer repair/evidence](../REPORTS/PHASE_2B_FINANCE_CONSUMER_REPAIR.md): Student invoice/download, dashboard and reminder balances now subtract applied adjustment. Payments page uses canonical API balance and counts Completed/Reconciled collections. New before-fix reminder 1 PASS/5 FAIL and page 1 PASS/6 FAIL; final API 140/140, page 7/7/typecheck PASS, targeted lint 0 errors/1 existing warning. Six consumer stages PASS twice on fresh Identity/HTTP/SQL: mixed/Voided, settled/full adjustment, PendingApproval/Rejected and one-cent balance, expected notification deltas and financial snapshots. Owned cleanup, no commit/deployment. Consumer arithmetic gap is repaired locally, not browser/Guardian permission/concurrency/full critical certification or issue closure. Restoration/zero-net policy separate and pending. Earlier checkpoint below is historical.

## Earlier guard repair checkpoint — 2026-10-01

[Repair and evidence](../REPORTS/PHASE_2B_ADJUSTED_PAYMENT_REPAIR.md): collection guard and settlement status now compare collected payments against TotalAmount minus applied AdjustedAmount. Original excess 300/full-adjustment 1 rejected without financial SQL change; exact 200 Paid/zero admin balance. Two fresh SQL runs plus 102 API tests PASS. Remain OPEN: related Student portal/document, dashboard and reminder formulas still omit adjustments; consumer/critical/browser/concurrency acceptance pending. Original evidence/source excerpt below is retained historical failure, not current guard behavior.

## Original reproduction (before repair)

Seed invoice 1000, approved adjustment 200 and paid 600. Submit payment 300; inspect fresh invoice/payment state.

## Expected

Remaining collectible balance is 200; payment 300 is rejected with no database change.

## Actual / evidence

Guard compares against TotalAmount without AdjustedAmount. InvoiceSummary.Balance subtracts adjustments. Payment status calculation also uses gross TotalAmount.

Two counted isolated HTTP/SQL runs confirmed all three variants: balance 200 still permits payment 300 and persists ledger 900 plus discount 200; exact adjusted settlement collects 800 with balance 0 but remains PartiallyPaid; full adjustment 1000 starts Paid, then accepts payment 1 and becomes PartiallyPaid. Pending, zero-amount and repeated-approval controls passed. The initial missing FinanceControls subscription fixture stopped on 403 and is excluded from reproduction frequency. No product fix or production financial write was made.

Source snapshot:

```text
26:         var paid = await dbContext.Payments.Where(x => x.InvoiceId == invoice.Id && x.Status == "Completed").SumAsync(x => (decimal?)x.Amount, cancellationToken) ?? 0;
27:         if (paid + request.Amount > invoice.TotalAmount) return BadRequest(new { message = "Payment exceeds the invoice balance." });
28:         var payment = new Payment { AcademyId = academyId, InvoiceId = invoice.Id, Amount = request.Amount, Currency = invoice.Currency, Method = string.IsNullOrWhiteSpace(request.Method) ? "Offline" : request.Method.Trim(), Reference = request.Reference?.Trim() };
29:         invoice.Status = paid + request.Amount == invoice.TotalAmount ? "Paid" : "PartiallyPaid";
```

## Suspected root cause

Collection uses gross instead of adjusted invoice balance.

## Business impact and blast radius

Overcollection and inconsistent Paid/PartiallyPaid state.

## Related / required regression

FINANCE-RULE-002: Adjusted invoice with partial/full payments; compare read API balance and persisted status.

Also run all endpoint/form cases pointing to this issue in QA/03_TEST_MATRIX.md and the critical regression suite before closure.
