# BUG-DATA-0001 — Reconciled payments disappear from some balance calculations

| Field | Value |
| --- | --- |
| Status | OPEN / FIX-IN-PROGRESS (local repair) |
| Confirmation status | RUNTIME-REPRODUCED |
| Final verification | Sequential case reconfirmed 2026-10-03; concurrent double-collect FAIL 3/5; browser and device pending |
| Severity | Critical financial integrity |
| Priority | P0 |
| Category | DATA |
| Module | FINANCE |
| Role | See reproduction; same-tenant Admin unless stated otherwise |
| Screen / route | /payments; /finance-reconciliation; /portal |
| API | POST payments; PATCH payments/{id}/reconcile; GET portal/students/{id} |
| Environment | Two fresh local run-owned Docker SQL/TestServer fixtures; production state not inferred |
| Device/viewport | NOT RUN; use QA/06_DEVICE_VIEWPORT_MATRIX.md where UI applies |
| Baseline | 20bb6047f9edf733ac8e2a226621cc582ec54b3c + pre-existing student UI diff |
| Discovery test / review ID | FINANCE-RULE-001 |
| Evidence classification | Static trace confirmed by real authenticated HTTP and fresh SQL snapshots |
| Preconditions | Isolated synthetic fixture from QA/10_TEST_DATA_STRATEGY.md; never customer data |
| Reproduction frequency | 2/2 fresh runs, 2026-09-30 |
| Source | apps/api/Controllers/PaymentsController.cs:26 |
| Class/function | Create / Reconcile; PortalController.Student |
| Related source | INVENTORY/API_ENDPOINTS.md, CONTRACTS.md, FORMS_AND_FIELDS.md and ASYNC_FORM_RISKS.md |
| Root-cause confidence | HIGH |
| Evidence location | QA/REPORTS/PHASE_2A_RECONCILED_PAYMENT_REPRO.md; retained Phase 1 source excerpt below |
| Screenshot | Not captured on pinned baseline |
| Console logs | Original FAIL retained; local repair/retest logs in linked Phase 2B report below |
| API request | Seed invoice 1000 and Completed payment 600. Reconcile that payment. Read admin and family invoice balances, then attempt payment 500. |
| API response | Before reconciliation extra 500 rejected 400; after reconciliation Student balance 1000 and extra 500 accepted 201 |
| Database before/after | Fresh SQL: original 600 becomes Reconciled; extra 500 persists as second Completed row, ledger 1100; invalid controls unchanged |
| Dependencies | Safe SQL/HTTP/browser harness as applicable; desired policy review where noted |
| Fix commit | Local uncommitted controller repair, 2026-10-01; no deployment |
| Retest result | Original case PASS in two fresh guarded Identity/HTTP/SQL runs |
| Regression result | Latest API 140/140 and controlled Payments page 7/7; adjusted/collected consumer HTTP/SQL six stages PASS twice; earlier original-case evidence retained; full critical NOT RUN |
| Closure notes | Remain OPEN; follow closure requirements in QA README |

## Concurrent collection — 2026-10-03

[Concurrent collection](../REPORTS/PHASE_2B_COLLECTION_RACE.md): five invoices of 1000, each with a reconciled payment of 600, then two simultaneous payments of 400. Attempts 2, 4, and 5 returned 201 and 201 and stored 1400. Attempts 1 and 3 returned 201 and 400 and stored 1000. Run `45e986ef6c4b4a72a840fcf09b604e4a` exit 1. Create reads the collected sum and inserts the payment without a transaction or invoice-row lock. Issue remains OPEN. The sequential confirmation below is unchanged.

## Current-source confirmation — 2026-10-03

[Current-source confirmation](../REPORTS/PHASE_2B_RECONCILED_PAYMENT_CONFIRMATION.md): fresh disposable SQL rerun of the original case PASS. Reconciled 600 still leaves admin and student balance 400, and excess 500 is rejected with the ledger unchanged. Issue remains OPEN for concurrent collection, live browser, and device checks.

[Consumer repair/evidence](../REPORTS/PHASE_2B_FINANCE_CONSUMER_REPAIR.md): Payments page Collected now includes Reconciled and Completed, excludes Voided; options/directory/detail/amount prefill/max/open/outstanding use canonical invoice API balance. Actual page with controlled hook state passes 7/7 after before-fix 1 PASS/6 FAIL; typecheck PASS, lint 0 errors/1 pre-existing warning. Related adjustment consumers pass six real Identity/HTTP/SQL stages twice, API suite 140/140. Not a live browser/React lifecycle or full critical/role/concurrency PASS; remains OPEN. No deployment, current owned QA resources cleaned. Earlier checkpoint below is historical.

## Earlier backend repair checkpoint — 2026-10-01

[Repair and evidence](../REPORTS/PHASE_2B_RECONCILED_PAYMENT_REPAIR.md): Completed/Reconciled collections now counted by payment guard/status, family balances/document, dashboard and reminders. Two fresh runs retain paid 600/balance 400 and reject excess 500 without SQL change; mixed/Voided, exact settlement, one-cent rejection and reminder controls pass. Original FAIL evidence and source excerpt below are historical and retained. Remain OPEN until required broader regression/critical acceptance; concurrent writes and separate adjusted-balance BUG-DATA-0002 not certified.

## Original reproduction (before repair)

Seed invoice 1000 and Completed payment 600. Reconcile that payment. Read admin and family invoice balances, then attempt payment 500.

## Expected

Reconciliation does not change paid total; remaining balance remains 400; excess collection is rejected.

## Actual / evidence

Payment creation and family aggregation count only Completed; Reconcile changes status to Reconciled. Invoice list counts non-Voided, so views differ and the create guard can ignore prior money. Two fresh isolated HTTP/SQL runs confirmed admin balance 400 versus Student portal balance 1000, followed by an accepted extra payment 500 and a persisted ledger of 1100. Rejected overpayment before reconciliation and blank-reference controls left their SQL snapshots unchanged. These are synthetic local writes, not production observations.

Source snapshot:

```text
25:         if (request.Amount <= 0) return BadRequest(new { message = "Payment amount must be positive." });
26:         var paid = await dbContext.Payments.Where(x => x.InvoiceId == invoice.Id && x.Status == "Completed").SumAsync(x => (decimal?)x.Amount, cancellationToken) ?? 0;
27:         if (paid + request.Amount > invoice.TotalAmount) return BadRequest(new { message = "Payment exceeds the invoice balance." });
28:         var payment = new Payment { AcademyId = academyId, InvoiceId = invoice.Id, Amount = request.Amount, Currency = invoice.Currency, Method = string.IsNullOrWhiteSpace(request.Method) ? "Offline" : request.Method.Trim(), Reference = request.Reference?.Trim() };
```

## Suspected root cause

Different definitions of a collected payment across payment, invoice and portal controllers.

## Business impact and blast radius

Collections, family balances, reminders and invoice status; financial release blocker reproduced, locally repaired, broader acceptance pending.

## Related / required regression

FINANCE-RULE-001: SQL sequence create→reconcile→read both portals→excess-pay rejection; include void and adjustment variants.

Also run all endpoint/form cases pointing to this issue in QA/03_TEST_MATRIX.md and the critical regression suite before closure.
