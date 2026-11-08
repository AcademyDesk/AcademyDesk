# C20 invoice readback and Reports currencies — 8 November 2026

Cloud owner. IMPLEMENTED / CLOUD VALIDATED; local acceptance pending.
Branch `codex/cloud-c20-reporting-currencies-20261108`.
Clean base `33e47b2b2a6f760436fb0fbe865b8395d3dd0bce` (C19 return).
At claim Desktop `b7dc134` had C17/C18 integrated,28 HTTP/SQL,51 Platform
billing and2,138 combined API PASS. Final refresh `fe3167b12cbd318369d159bf59a06dacfa2066ae`
already integrates C19:34 combined HTTP/SQL,2,161 API and27 targeted frontend
checks PASS; eight live Payments browser/SQL phases PASS. These are source-bound
local returns, not C20 SQL acceptance. No competing C20 paths found in
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

## Implementation and source compatibility

Only GET Invoice List's payment-sum predicate changes: normalized currency must
match the invoice, alongside the pre-existing academy/invoice/status predicates.
The response preserves original raw currency/status/amount/adjustment metadata,
its computed nonnegative balance DTO, rows and ordering. All content following
the GET List action (every other action and DTO) is byte-identical to the base.
This does not migrate or repair a stored ledger, promote payments, or change
write/status/audit/permission logic. Invoice history still includes cancelled
rows; consumers exclude their receivable rather than hiding history.

Reports uses the same read API balance, actual payment/expense units and
Completed/Reconciled collections. Outstanding fees are summed per invoice,
excluding cancelled debt, without cross-student overpayment offsets or payable
payroll. Existing unpaid payroll records remain a separate, labelled modal
section with their own unit/status. Gross issued totals retain invoice history.
All currency groups retain cents; there is no FX or INR default. Missing units
or malformed/nonfinite monetary values show Currency details unavailable. Empty
successful activity is unitless0.00. Known legacy invalid currency strings use
the labelled formatter fallback rather than crashing.

Expenses retain the original all-recorded-expenses scope: this packet does not
invent expense approval/settlement or payroll policies. The former Net cash
label is now Collected less recorded expenses; differences are calculated only
within each currency, retaining negative expense-only units. Payroll is not
included in that calculation. Numeric strings cannot become apparently valid
amounts through subtraction coercion. No FinanceControls endpoint/module is
added to the existing Reports read dependencies.

Both invoice exports now share the same rows and add Currency beside Amount;
the expense export adds Currency. Existing filenames, gross invoice amount,
due date, operational exports and the actual spreadsheet-escaping helper remain.
Consumers must read the explicit new CSV header/column: schema addition is
intentional, not a currency guessed from the academy's country. Unknown row
currency is labelled unavailable in the CSV. Formula-prefixed names remain
inert cells, without rewriting source data.

Reports commits its ready UI state only after all existing required data and
attendance responses succeed. Loading, empty-academy,401,403,429,503/network
failure no longer exposes zero monetary indicators or empty downloads as if
complete. Sign-in/account-switch and read-only manual retry are available;
late unmounted responses are ignored. Developer host/port instructions were
removed from the product error. Nonfinancial counts, attendance selection,
occupancy and student/batch exports are retained.

## Executed evidence

| Check | Actual result |
| --- | --- |
| Original API22 cases |16 PASS /6 FAIL; zero skips |
| Original UI32 valid-fixture cases |1 PASS /31 FAIL; zero skips |
| Target API |101/101 PASS; zero skips |
| New Reports TSX |32/32 PASS; zero skips |
| Full API |2,177/2,177 PASS; zero skips |
| Full frontend |2,118 PASS /3 FAIL;2,121 tests across79 files; zero skips/cancellations |
| Build/types/lint |PASS; zero lint errors /62 existing warnings |
| Native Chromium |14/14 scenarios;36 dialogs and36 CSV downloads PASS; zero API writes/runtime/interceptor errors |
| Export integrity |570 SHA256 values unchanged after browser |
| Production guard |BLOCKED on five open P0s; required policy preserved |

Target API is22 new actual MVC/Identity HTTP cases plus33 unchanged invoice
ledger/status cases and23 each C17 finance/C19 dashboard tests. New controls
cover AcademyAdmin/Owner/FinanceUser, raw and normalized USD units, accepted
versus Pending/Processing/Failed/Voided receipts, foreign rows, exact decimals,
per-invoice overpayment, cancellation history, empty data, anonymous/foreign/
role/module denials and fresh money/audit snapshots unchanged on reads. These
are explicit isolated InMemory domain/Identity stores; migrations, bootstrap,
media and PENTA are disabled. No configured SQL/laptop/provider endpoint used.

The UI suite tests group amounts, invoice adjustments/balance, Reconciled
receipt details, expense-only negative differences, malformed metadata and
numeric strings, sixteen required-feed/status failures, initial/empty gating,
manual recovery, late responses, both invoice CSV handlers, expense currency,
real formula escaping, operational counts and occupancy CSV. The first31-case
run and early29/31 target are retained: one fixture lacked required due dates,
and an Intl negative value correctly rendered -EUR9.15 where the initial
expectation put the minus after the code. Those harness errors were corrected,
and the shared final32-case suite was rerun against the unchanged original page
and the final implementation. No old assertion, missing receipt or source data
was weakened or made synthetic to obtain a pass.

Full frontend's only failures remain the Email/WhatsApp/Meeting genuine SQL-
receipt checks; they are FAIL, not skips, and raw local receipts are not invented
or published. Exact Next16.4.3/React19.2.8 build uses test-only API loopback49142,
not a production configuration.89 entries,570 exported files and87 unchanged
RSC aliases. No packages, restore, images or external audit requests added.

## Desktop return and release priorities

C17/C18 actual SQL acceptance exists at Desktop `b7dc134`:28 combined
currency/support HTTP/SQL and51 original Platform billing checks,85 application/
7 Identity migrations, owned cleanup and original service restoration. The
combined API2,138 result is bound to that local source. C20 inherits the Cloud
C19 branch, not Desktop's newer support/profile atomic-audit repairs or six
newer tests. C19 is now integrated at Desktop; import only these C20 scoped paths, preserve newer
shared handoffs/issue evidence and support/profile/SQL code, and rerun the full
combined API suite. Cloud2,177 is not a combined-tree or SQL result.

Priority acceptance on an isolated SQL service and real Identity/HTTP/browser:

1. Invoice GET: matching/whitespace/case and wrong-currency receipts, all accepted
   states, adjustments/overpayments/cancelled history, no cross-tenant reads or
   read-side mutation/audit. Confirm SQL translation and precision, Finance role
   and module boundaries, and Dashboard/Finance/Invoice agreement.
2. Reports: actual distinct-currency invoice/payment/expense/payroll rows,
   Reconciled collections, adjusted fees and separate payroll, all three dialogs,
   both invoice CSVs and expense CSV with real headers and formula escaping;
   required-feed denial/loss/retry, lifecycle/session and physical devices.
3. Legacy different-currency receipts still require write-path review: invoice
   manual status, payment collection/void/restore and adjustment approval have
   independent monetary queries. C20 deliberately does not change these locally
   owned write paths; do not treat corrected reads as their acceptance. Preserve
   current transaction/audit/concurrency safeguards and claim any repair first.
4. Keep five P0s and BUG-DATA-0016 OPEN through required original/module/critical/
   device retests. Exact release CI/image/security checks and staging backup/
   restore/rollback remain; Mini/Plus/Pro unqualified, Ultra deferred, no models.

Next route: Sol High for local SQL/legacy monetary writes/authority/integration;
Sol Medium for settled imports/docs. Feature push is durable exchange, not live
D: synchronization. Exact remote CI UNKNOWN: verify.yml does not match this
feature branch's pushes; no workflow dispatched. No main merge, Azure or
model/environment publication, public access or privacy change.

## Native artifacts and reproduction

320/light and1440/dark browser cases cover mixed, USD-only, empty, missing
currency metadata, expense-only negative differences, unavailable required feed
and successful manual retry. All36 Collected/Outstanding/Expenses dialogs open
and close with their correct rows; all36 real CSV files download and are read
from owned temporary directories. The two invoice buttons produce byte-equal
CSV, each has an explicit Currency column, and formula-prefixed student names
are escaped. The failure case exposes neither ready metrics nor downloads;
retry performs only GET requests. Attendance and active-student indicators stay
correct. No browser retries or weakened assertions; normal Chromium sandbox,
owned temporary profile removal, zero runtime/interceptor errors and all570
export hashes unchanged. Narrow/wide synthetic modal screenshots were inspected;
physical-device/real-host acceptance remains separate.

Original/final/intermediate logs, tests, TRX, frontend per-file results, native
receipts/screenshots/36 CSVs, export fingerprints and source/assembly/artifact/
publication SHA256 manifest remain outside git at
`/workspace/c20-reporting-currencies-20261108`. No raw QA/EVIDENCE published.

```sh
# Repository root; Cloud dotnet executable /workspace/.tools/dotnet/dotnet.
dotnet test tests/AcademyDesk.Api.Tests/AcademyDesk.Api.Tests.csproj --no-restore -p:UseSharedCompilation=false --filter 'FullyQualifiedName~InvoiceCurrencyReadbackTests|FullyQualifiedName~InvoiceLedgerStatusTests|FullyQualifiedName~FinanceCurrencySummaryTests|FullyQualifiedName~DashboardCurrencySummaryTests'
dotnet test tests/AcademyDesk.Api.Tests/AcademyDesk.Api.Tests.csproj --no-restore --no-build
node --test QA/tools/reports-finance.test.cjs
node QA/tools/run-frontend-tests.cjs
# apps/web:
./node_modules/.bin/tsc --noEmit
npm run lint
NEXT_PUBLIC_API_URL=http://127.0.0.1:49142 npm run build -- --webpack
# Repository root:
QA_BROWSER_EXECUTABLE=/usr/bin/chromium node QA/tools/reports-finance-browser.cjs
node QA/tools/check-production-gates.cjs # expected exit1 while P0s remain open
git diff --check
```

All other invoice actions/DTO source bytes were compared to the clean base and
remain unchanged. API full-suite run used the rebuilt101-target binaries,
.NET10.0.403; later changes were documentation only. Shared test adapter adds
only the two actual helper/CSV import resolvers; existing assertions remain.

## Final Desktop refresh

`fe3167b12cbd318369d159bf59a06dacfa2066ae` explicitly retains Cloud ownership of C20 and contains
C19 integration `81b1c65`. Its34/34 real HTTP/SQL controls include six new
Dashboard cases (mixed/single/empty, legacy wrong-currency receipt, payroll
separation, local-day boundaries, denials and unchanged captured rows). API
2,161 and frontend27 pass. The live Payments return separately passes eight
browser/SQL phases, including standard/adjusted one-cent-overage prevention,
exact settlement and retained response-loss recovery. Original resources were
restored. These are separate accepted scopes, not C20 or P0 closure. Do not
repeat accepted SQL modules solely because C20 adds another consumer.

- [C19 local SQL source and acceptance](https://github.com/AcademyDesk/AcademyDesk/blob/fe3167b12cbd318369d159bf59a06dacfa2066ae/QA/REPORTS/LOCAL_C19_DASHBOARD_SQL_ACCEPTANCE_2026-11-08.md)
- [P0 live payment browser scope](https://github.com/AcademyDesk/AcademyDesk/blob/fe3167b12cbd318369d159bf59a06dacfa2066ae/QA/REPORTS/LOCAL_P0_PAYMENT_BROWSER_BALANCES_2026-11-08.md)

The existing P0/critical/device/image/CI/staging gates remain. Prioritize new
C20 SQL readback/Reports and the independent legacy write review, preserve
Desktop's active remaining P0 runtime work, and reconcile issue paragraphs
rather than overwrite the whole newer record.
