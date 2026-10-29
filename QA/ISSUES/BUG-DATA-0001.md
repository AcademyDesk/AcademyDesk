# BUG-DATA-0001 — Reconciled payments disappear from some balance calculations

2026-10-23 [Native Reports/CSV consumer](../REPORTS/API_IMAGE_REPORT_BROWSER_2026-10-23.md):
47SQL/API checks/four real-login browser journeys PASS on current image/export.
Reconciled600+Completed100 collected700/void50 excluded/approved100/balance200,
exact SQL/UI/details/four actual CSVs; nine domain hashes unchanged/no foreign
sentinel. Exact owned cleanup/original restoration/full inventory PASS. 15current
native receipts qualified. Still OPEN for wider roles/fault/devices/release.

2026-10-23 patched-export successor: native SQL
`5cf31ac559164848b493d9f69330067c`, window
`9a06d7e7c0464cb78458a630ca7c84e3`, PASS 45 checks/four real Payments journeys
against the new 627-file export, not the older frontend below. Exact SQL
1000/two rows/Paid/balance0, 201/400, visible login/picker/reset and original
service/inventory restoration retained. Current 14-receipt qualification is
`9c3525fcd8e54ebe9c965f62b9e6f52d`. Still OPEN for remaining consumer, wider
roles/fault/device/release acceptance. [Current P0 gaps](../REPORTS/CURRENT_P0_ACCEPTANCE_GAPS_2026-10-23.md).

2026-10-23 [Exact-image transition matrix](../REPORTS/API_IMAGE_TRANSITIONS_2026-10-22.md):
158 native SQL/API checks and 25/25 concurrent pairs PASS across adjusted
collection, approval/payment, two voids, create/void and reconcile/void. Exact
ledger rows/balances/evidence/canonical reads, unchanged original regressions,
owned cleanup and original service restoration verified. Additive acceptance;
physical-device, wider lifecycle and release requirements remain **OPEN**.

2026-10-22 [Exact exported Payments/browser successor](../REPORTS/API_IMAGE_PAYMENT_BROWSER_2026-10-22.md):
native run294dd8d8dbe34109a54f80e0f59c2d1c PASS45seed/security checks plus4real UI
submits:390/1440light201success,390/1440dark stale400/exact message. Real visible
login, picker collapse, reset/disabled empty form, exact SQL2rows/1000/Paid/balance0,
no429/page exceptions/horizontal overflow. Accepted immutable export/API source
unchanged; owned cleanup/exact original service restoration/full inventories PASS.
QA relay login-query mismatch corrected without application changes; failed first
attempt retained. Physical devices/broader faults/roles/release remain **OPEN**.

2026-10-22 [Exact current-image finance successor](../REPORTS/API_IMAGE_FINANCE_2026-10-22.md):
native Kestrel/disposable SQL sequential1000/reconciled600/balance400/excess500→400
PASS; five barrier-released400pairs each201+400/exact rejection/collected1000/two
rows/Paid/balance0/no429. Eleven successful-payment audits; original race/runner
unchanged. Owned cleanup/original services/inventories restored. Additive image
proof, not adjusted/void/payroll/full browser/device/release acceptance. **OPEN.**

2026-10-21 [Reports consumer successor](../REPORTS/REPORTS_CANONICAL_LEDGER_2026-10-21.md):
actual TSX before1pass/3fail reproduced excluded Reconciled collections and inaccurate
currency/expense display. Reports now uses Completed+Reconciled, server canonical
balance, per-currency integer-cent presentation and existing exact expense/payroll
fields. Receivables/payroll are separate; NoCashDue not shown unsettled; misleading
Net cash label removed.20Reports/helper checks and combined101frontend checks pass;
types/lint/87-route build pass. No backend/money-policy/SQL/model change in this
read-only slice; actual backend ledger evidence retained. Full-page/physical/device/
critical/release acceptance remains separately evidenced. Issue stays OPEN.

2026-10-15 [fee reminders frontend successor](../REPORTS/BATCH_2_FEE_PLANS_REMINDERS_PENTA.md):old Completed-only/gross projection reproduced1000 versus canonical200 with reconciled600/adjusted200. UI now uses server balance,excludes settled invoices and separates currencies.152 focused entries/13 targeted InMemory backend tests/228 synthetic browser cases PASS,including retained finance regressions. No backend/SQL change;not live SQL/role/device/delivery/release closure. Issue remains OPEN.

2026-10-15 [invoice/payment UI outcome recovery](../REPORTS/INVOICE_PAYMENT_SUBMIT_RECOVERY.md):page-lifetime same-tick/pending/ready/review guards and explicit unknown-outcome read recovery;authoritative Completed+Reconciled/canonical balance semantics retained.97 focused test entries/138 synthetic browser cases/build-types PASS;no backend/SQL change or SQL rerun. This is not durable server idempotency or physical-device/critical/full-release proof. Issue remains OPEN;prior SQL race/audit evidence retained.

| Field | Value |
| --- | --- |
| Status | OPEN / FIX-IN-PROGRESS (local repair) |
| Confirmation status | RUNTIME-REPRODUCED |
| Final verification | Sequential case reconfirmed 2026-10-03; unadjusted and pre-approved adjustment collection races PASS 5/5 each against disposable SQL; browser/device/critical gates pending |
| Severity | Critical financial integrity |
| Priority | P0 |
| Category | DATA |
| Module | FINANCE |
| Role | See reproduction; same-tenant Admin unless stated otherwise |
| Screen / route | /payments; /finance-reconciliation; /portal |
| API | POST payments; PATCH payments/{id}/reconcile; GET portal/students/{id} |
| Environment | Two fresh local run-owned Docker SQL/TestServer fixtures; production state not inferred |
| Device/viewport | Real browser desktop and emulated 390×844 PASS locally; physical Android/iOS NOT RUN |
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
| Fix commit | Local repair on `cursor/certificate-validation`; commit recorded in handoff; no merge or deployment |
| Retest result | Original case PASS; concurrent repair PASS 5/5 guarded with exact 400 message in fresh Identity/HTTP/SQL |
| Regression result | Latest API 140/140 and controlled Payments page 7/7; adjusted/collected consumer HTTP/SQL six stages PASS twice; earlier original-case evidence retained; full critical NOT RUN |
| Closure notes | Remain OPEN; follow closure requirements in QA README |

## Concurrent collection — 2026-10-03

[Live Payments browser check](../REPORTS/PHASE_2B_PAYMENTS_BROWSER_CHECK.md): signed-in `/payments` against isolated synthetic SQL showed Reconciled 600/balance 400, accepted exact 400 with success and refreshed Paid/0, then showed the exact balance error and no duplicate when a selected invoice became paid in another client. Direct route/refresh, mobile emulation and final two-row SQL invariant passed. This does not replace physical device, all role/fault and release gates. Issue remains OPEN.

[Additional adjusted-race coverage](../REPORTS/PHASE_2B_P0_CONTINUATION.md): on a 1000 invoice with an approved 200 discount and Reconciled 600, simultaneous 200+200 payment requests produced one 201 and one 400 with the exact balance message in all five fresh SQL/HTTP pairs. Collected stayed 800, with two payment rows, Paid status and one intact approved adjustment. The original unadjusted five-pair case reran 5/5. This does not exercise concurrent adjustment *approval* versus payment or replace live browser/device checks; issue remains OPEN.

[Concurrent collection](../REPORTS/PHASE_2B_COLLECTION_RACE.md): five invoices of 1000, each with a reconciled payment of 600, then two simultaneous payments of 400. Attempts 2, 4, and 5 returned 201 and 201 and stored 1400. Attempts 1 and 3 returned 201 and 400 and stored 1000. Run `45e986ef6c4b4a72a840fcf09b604e4a` exit 1. Create reads the collected sum and inserts the payment without a transaction or invoice-row lock. Issue remains OPEN. The sequential confirmation below is unchanged.

[Concurrent collection repair](../REPORTS/PHASE_2B_COLLECTION_RACE_REPAIR.md): invoice-row `UPDLOCK, HOLDLOCK` now spans the balance read and insert in a SQL transaction. Fresh run `7f5f02d9f7674c6aa7fb31aa3c6dfdb4` PASS 5/5: each pair returned one 201 and one 400 with the existing balance message, no 429, collected 1000, two rows. Separate sequential run `d107470396c54192acbd73def3e6d59e` PASS; API suite 1,042/1,042 PASS. Browser and physical device remain pending; issue stays OPEN.

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
