# C20 invoice readback and Reports currencies — 8 November 2026

Cloud owner. CLAIMED; implementation and verification pending.
Branch `codex/cloud-c20-reporting-currencies-20261108`.
Clean base `33e47b2b2a6f760436fb0fbe865b8395d3dd0bce` (C19 return).
Latest Desktop `b7dc134`: C17/C18 source integrated;28 HTTP/SQL,51 Platform
billing and2,138 combined API PASS. Source-bound local evidence, not Cloud SQL.
C19 is still a separate integration return. No competing C20 paths found in
current shared claims. Preserve newer support/profile audit fixes and SQL work.

Exact paths:

- `apps/api/Controllers/InvoicesController.cs` — GET List monetary projection only
- `apps/web/src/app/reports/page.tsx`
- `apps/web/src/lib/reports-finance.ts`
- `tests/AcademyDesk.Api.Tests/InvoiceCurrencyReadbackTests.cs`
- `QA/tools/reports-finance.test.cjs`
- `QA/tools/reports-finance-browser.cjs`
- `QA/tools/frontend-page-runtime.cjs` — actual CSV and Reports helper imports only
- `QA/ISSUES/BUG-DATA-0016.md` — preserve history and local SQL acceptance
- `QA/HANDOFF/C20_REPORTING_CURRENCIES_2026-11-08.md`

Invoice GET must count only same-academy, same-invoice, matching-normalized-
currency Completed/Reconciled receipts, consistent with C17/C19 summaries.
Preserve raw currency/status/amount metadata, balance DTO, ordering, every
write/status method and current host authority. No migration or SQL harness edit.

Reports must retain currency/adjusted balance metadata, include Reconciled
collections, exclude cancelled fee debt and never offset invoices by another
student's overpayment or combine payroll payable into receivable totals.
Group recorded expenses and collected-less-expenses by actual unit without FX;
keep existing expense inclusion and payroll row status selection, not a new
accounting lifecycle policy. Use existing invoice/payment endpoints, not a new
FinanceControls module dependency. Both invoice CSV buttons and expense export
must carry explicit currency; keep the real spreadsheet escaping helper.

Required-feed failure/loading/denial must not show a ready zero-money view or
allow misleading empty exports. Keep manual sign-in/retry and cancel late
response writes. Preserve nonmoney operational counts, occupancy/export flows.

Before/after actual HTTP/Identity with isolated InMemory stores, cross-summary
agreement, retained invoice/finance/dashboard checks, actual TSX and native
Chromium/export checks, build/types/lint and full regression. Real SQL/query
translation/legacy-data/write/critical/device acceptance is Desktop-owned.
Keep all five P0s and BUG-DATA-0016 OPEN. No raw evidence publication, new
dependencies/images, customer/provider calls, main/Azure/model/environment/
privacy/public internet changes. Mini/Plus/Pro unqualified; Ultra deferred;
stop before model loading. Sol High for money/authority/SQL; Medium for imports.
