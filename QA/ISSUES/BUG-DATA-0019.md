# BUG-DATA-0019 — Fee reminders can request payment for cancelled invoices

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | RUNTIME-CONFIRMED / BOUNDED REPAIR |
| Final verification | NOT RUN |
| Severity | Major erroneous collection notification |
| Priority | P1 |
| Category | DATA |
| Module | FEES |
| Role | See reproduction; same-tenant Admin unless stated otherwise |
| Screen / route | /fee-reminders |
| API | POST fee-reminders |
| Environment | Source review of local working tree; runtime production state not inferred |
| Device/viewport | NOT RUN; use QA/06_DEVICE_VIEWPORT_MATRIX.md where UI applies |
| Baseline | 20bb6047f9edf733ac8e2a226621cc582ec54b3c + pre-existing student UI diff |
| Discovery test / review ID | FEES-REMINDER-001 |
| Evidence classification | Static trace; runtime reproduction pending |
| Preconditions | Isolated synthetic fixture from QA/10_TEST_DATA_STRATEGY.md; never customer data |
| Reproduction frequency | Not measured; reproduction instructions are proposed |
| Source | apps/api/Controllers/FeeRemindersController.cs:15 |
| Class/function | Queue |
| Related source | INVENTORY/API_ENDPOINTS.md, CONTRACTS.md, FORMS_AND_FIELDS.md and ASYNC_FORM_RISKS.md |
| Root-cause confidence | HIGH |
| Evidence location | Source excerpt below; source fingerprint in INVENTORY/SOURCE_MANIFEST.md |
| Screenshot | Not captured on pinned baseline |
| Console logs | Not captured; required in retest |
| API request | Seed a cancelled overdue invoice with positive gross total and no payment. Queue reminders both with explicit InvoiceId and with null InvoiceId; inspect fresh Notification rows. Compare active overdue and settled controls. |
| API response | Not captured for this issue; use synthetic request/response in isolated reproduction |
| Database before/after | Not executed; fixture and fresh-context assertions defined below |
| Dependencies | Safe SQL/HTTP/browser harness as applicable; desired policy review where noted |
| Fix commit | ce9470114cedb7fd5371b7078fbc41883785d373 (C14 API/screen) |
| Retest result | C14 TSX8/8, API/finance58/58, controlled native browser6/6 PASS; real SQL/HTTP/device closure pending |
| Regression result | API2,058/2,058 PASS; frontend2,016 PASS /3 genuine SQL-evidence FAIL, zero skips |
| Closure notes | Remain OPEN; follow closure requirements in QA README |

## Exact reproduction

Seed a cancelled overdue invoice with positive gross total and no payment. Queue reminders both with explicit InvoiceId and with null InvoiceId; inspect fresh Notification rows. Compare active overdue and settled controls.

## Expected

Cancelled invoices produce no collection reminder or payment demand, regardless of bulk or targeted entry point.

## Actual / evidence

Queue filters tenant and optional ID/due date but never invoice status; any positive computed balance creates a student notification. UI also drops invoice status from its due calculation. Source trace only.

Source snapshot:

```text
14:     {
15:         var invoices = dbContext.Invoices.Where(x => x.AcademyId == academyId);
16:         if (request.InvoiceId.HasValue) invoices = invoices.Where(x => x.Id == request.InvoiceId.Value);
17:         else invoices = invoices.Where(x => x.DueDate <= DateOnly.FromDateTime(DateTime.UtcNow));
```

## Suspected root cause

Reminder eligibility is reduced to amount/date without cancellation-state validation.

## Business impact and blast radius

Student in-app collection notices for cancelled invoices; related incorrect net balance calculations remain BUG-DATA-0001/0002.

## Related / required regression

FEES-REMINDER-001: HTTP/SQL and UI cases for Cancelled/Paid/Issued/PartiallyPaid/Overdue, due date, payment and adjustment combinations; assert zero new notices for cancelled invoices and correct positive controls.

Also run all endpoint/form cases pointing to this issue in QA/03_TEST_MATRIX.md and the critical regression suite before closure.

## C14 bounded repair — 7 November 2026

Original controller3/12 pass,9 fail: targeted/bulk cancelled invoices queued
notices, including lower/mixed case. Original screen1/8 pass,7 fail and all six
original native browser scenarios fail. The API now excludes cancellation
case-insensitively; the screen excludes cancelled/nonpositive/invalid balances
and uses the existing API's authoritative net balance. It also separates
currencies instead of labeling a mixed total INR. Linked BUG-DATA-0001/0002
consumer fixes remain subject to their full P0 acceptance.

Targeted TSX8/8 and API plus retained finance58/58 pass. Full API2,058 pass;
full frontend2,016 pass with three unchanged genuine communication SQL receipt
failures, zero skips. No new SQL or customer/provider send occurred. Keep this
issue OPEN for its original real SQL/HTTP/browser/device/critical closure.
[Exact commands, native return and Desktop handoff](../HANDOFF/C14_FEE_REMINDERS_2026-11-07.md).

Final configured Chromium6/6 pass, zero runtime/interceptor errors, two clearly
synthetic POSTs, sandbox enabled and owned cleanup. The570 export hashes remain
unchanged. Preliminary default-API browser failure is retained; no interception
guard was relaxed. Source-bound build, types and lint pass (zero errors/62warnings).
