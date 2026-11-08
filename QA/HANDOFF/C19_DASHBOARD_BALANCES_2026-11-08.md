# C19 academy dashboard monetary readback — 8 November 2026

Cloud owner. Status IMPLEMENTED / CLOUD VALIDATED; local acceptance pending.
Branch `codex/cloud-c19-dashboard-balances-20261108`.
Implementation `e9d256221fa49fa467b9a6de38d07f9e3aa3600a`; claim `ab4c43ac29cb3d381da7dbe068ba52d6f0512394`.
Clean base `cb0656a78f94fd10f398c5d67860a3406f3ec0a1` verified after workspace
startup recovery. Latest Desktop inspected `88fd8e1`; no competing Dashboard
claim found. C17/C18 remain separate required returns. Preserve all Desktop
SQL/profile-audit work, shared handoffs, original repositories and raw evidence.

Exact paths:

- `apps/api/Controllers/DashboardController.cs`
- `apps/api/Controllers/FinanceGovernanceController.cs` — delegate existing C17
  summary computation to the shared read model, retaining its JSON behavior
- `apps/api/ReadModels/AcademyFinanceReadModel.cs`
- `apps/web/src/app/dashboard/page.tsx`
- `apps/web/src/lib/finance-summary.ts` — broaden helper input type to grouped
  metadata only; preserve existing money/derived formatter behavior
- `tests/AcademyDesk.Api.Tests/DashboardCurrencySummaryTests.cs`
- `QA/tools/dashboard-currency-summary.test.cjs`
- `QA/tools/dashboard-currency-summary-browser.cjs`
- `QA/tools/session-recovery-ui.test.cjs` — load the real shared module only;
  preserve every existing session/denial/recovery assertion
- `QA/ISSUES/BUG-DATA-0016.md`
- `QA/HANDOFF/C19_DASHBOARD_BALANCES_2026-11-08.md`

Reproduce mixed units, cancelled/overpaid balance distortion and dashboard
fee/payroll conflation. Reuse C17's financial read algorithm through one small
shared read model; don't introduce a second accounting calculation. Dashboard
money scalars retain single-currency values/empty zero and become null when
mixed, with explicit grouped metadata. Preserve all current authority, student/
teacher/course/batch/attendance/schedule/activity logic, timezone handling,
session gates, write flows and ledger rows.

The existing Outstanding fees tile will show student receivables, with payroll
records separately labelled in its existing detail view. No netting or combined
opposite-direction money total. Detail invoices use the existing authoritative
balance/currency fields, exclude cancelled/nonpositive rows and preserve cents.
Keep existing payroll status selection; don't invent a payout workflow or
interpret unknown payroll states as a new accounting policy.

Validate original/final HTTP/Identity/InMemory fixtures and cross-endpoint
agreement, retained C17 currency and session recovery checks, actual TSX/browser,
build/types/lint and full suites. Real SQL/concurrency/audit/device/staging/CI
remain distinct gates. Reports-page monetary calculations are NOT claimed.
Keep BUG-DATA-0016/P0s OPEN. No new dependencies/images, configured SQL access,
customer/provider sends, model loading, main/Azure/environment/public changes.
Sol High for monetary/security/local SQL; Medium for settled source import/docs.

## Contract and reproduction

Original source at claim `ab4c43ac29cb3d381da7dbe068ba52d6f0512394` failed
18 of 23 new actual HTTP/Identity tests and all14 controlled TSX checks. The
backend failures demonstrate absent currency groups, mixed flat money,
cancelled invoice debt, overpayment netting and different-currency offsets.
The UI failures include ignored adjustments/balance metadata and adding a EUR
payroll amount to a fee tile labelled INR. Original source and unchanged tests,
logs and TRX remain in the excluded task artifact directory.

`GET /api/academies/{academyId}/dashboard` adds `byCurrency` using C17's
five-field monetary groups, plus `currency` and `hasMixedCurrencies`.
`totalInvoiced`, `totalPaid` and `outstandingBalance` remain numeric for one
currency, zero for empty, and become null for mixed units. API/UI must return
together after acceptance; mixed scalar nullability is deliberate. The shared
read model retains tenant/payment-state/currency/cancellation/balance predicates
and reads only the required scalar columns. FinanceGovernance delegates its
existing summary to this read model; all23 C17 checks are retained. It does not
change collections/follow-up writes, authorization, money or audit records.

The fee tile consumes grouped receivables only, with explicit currency and
cents. It does not net payroll payable against receivable fees. Its existing
modal lists student invoice `balance`/`currency`, excludes cancelled and
nonpositive/nonfinite balances, and labels payroll separately as records not
marked Paid. Payroll's existing case-sensitive status selection is preserved;
this is not a new payroll liability/status policy. Missing grouped metadata
shows Currency details unavailable, not guessed INR. Empty activity is unitless
0.00. Missing individual row currency also shows unavailable. Session recovery,
tenant/role gates, counts, attendance, timetable, timezone and activity remain.

## Evidence and exact source boundary

Targeted API46/46 PASS:23 new dashboard HTTP/Identity/InMemory cases plus all23
C17 finance controls. New cases cover both Owner/AcademyAdmin roles, single
INR/USD/EUR and legacy currency spelling, six payment statuses, three cancelled
spellings, overpayment, currency-mismatched receipt, empty data, cross-tenant
and anonymous denials, existing Teacher/Student/FinanceUser dashboard denials,
nonfinancial counts/attendance/timetable/activity and snapshots unchanged on
reads/denials. No role expansion or synthetic authority bypass. All stores
are isolated InMemory; real SQL transaction/concurrency proof is not claimed.

Targeted frontend42/42 PASS:14 new dashboard TSX checks,13 unchanged session
recovery cases and15 retained C17 monetary UI checks. The session test adapter
loads the actual shared formatter, without weakening old session assertions.
TypeScript PASS; lint zero errors /62 existing warnings.

| Final check | Actual result |
| --- | --- |
| Full API |2,155/2,155 PASS; zero skips |
| Full frontend |2,086 PASS /3 FAIL;2,089 tests across78 files; zero skips/cancellations |
| Native Chromium |14/14 PASS; zero runtime/interceptor errors or API writes |
| Exact-lock build/types/lint |PASS; zero lint errors /62 existing warnings |
| Export integrity |570 file SHA256 values unchanged after browser |
| Production guard |BLOCKED as expected on all five still-open P0s |

Full frontend's failures are the three unchanged Email/WhatsApp/Meeting cases
requiring `QA/EVIDENCE/logs/phase-2b-communication-roundtrip-sql.log`. These are
failures, not skipped or replaced with synthetic receipts. Desktop's earlier
SQL acceptance remains separately source-bound; its raw receipt is not here.

Native Chromium at320/light and1440/dark covers mixed, USD-only, empty, legacy,
malformed, missing individual currency, and failed dashboard responses. All12
successful-data dialogs open and close, retain adjusted fee cents/units, exclude
cancelled/overpaid invoices and Paid payroll, and show payroll EUR999.55
separately. Failed dashboard responses gate the entire ready view and offer
manual retry. Attendance50% and active-student count2 are retained. Normal
Chromium sandbox and removal of only its owned temporary profile verified.
No retries, assertion weakening, downloads, production calls or mutations.

Full API uses fresh rebuilt final-source binaries, .NET10.0.403, with explicit
isolated InMemory providers in the new fixtures. Next16.4.3/React19.2.8 build
uses test-only loopback API49142;89 generated entries,570 export files and87
unchanged RSC aliases. This test export is not a production configuration.
All original/final logs, TRX, frontend summary, browser receipts and source/
assembly/export/publication SHA256 manifest remain outside git at
`/workspace/c19-dashboard-balances-20261108`.

```sh
# Repository root; dotnet is /workspace/.tools/dotnet/dotnet in Cloud.
dotnet test tests/AcademyDesk.Api.Tests/AcademyDesk.Api.Tests.csproj --no-restore -p:UseSharedCompilation=false --filter 'FullyQualifiedName~DashboardCurrencySummaryTests|FullyQualifiedName~FinanceCurrencySummaryTests'
dotnet test tests/AcademyDesk.Api.Tests/AcademyDesk.Api.Tests.csproj --no-restore -p:UseSharedCompilation=false
node --test QA/tools/dashboard-currency-summary.test.cjs QA/tools/session-recovery-ui.test.cjs QA/tools/finance-currency-summary.test.cjs
node QA/tools/run-frontend-tests.cjs
# apps/web directory:
NEXT_PUBLIC_API_URL=http://127.0.0.1:49142 npm run build -- --webpack
./node_modules/.bin/tsc --noEmit
npm run lint
# Repository root:
QA_BROWSER_EXECUTABLE=/usr/bin/chromium node QA/tools/dashboard-currency-summary-browser.cjs
node QA/tools/check-production-gates.cjs # expected exit1 while P0s remain open
git diff --check
```

## Desktop handoff and acceptance still required

Reconcile these exact paths after C17/C18, preserving Desktop's newer profile
atomic-audit fix and its AuditOutcome/SQL work. Do not copy this older Cloud
backend wholesale. Run the full combined API suite; Cloud counts are not a
result for a later combined tree. Shared handoffs and raw local SQL receipts
remain Desktop-owned. This report is the task-specific handoff.

On disposable SQL and real Identity/HTTP, compare Dashboard and Finance summary
per-currency groups for mixed/single/empty histories, accepted payment states,
adjustments, cancelled casing, overpayment and legacy mismatched-currency
receipts. Verify schema-valid precision, translation/query plan, tenant/role
checks, unchanged records/audit, nonmoney/timezone behavior and actual browser
modal values. Test genuine fee/payroll records separately. Invoice List's legacy
mismatched-payment-currency projection remains a separate known acceptance risk:
C19 consumes its authoritative balance but does not modify InvoicesController.
The shared summary correctly scopes currency; don't claim that raw legacy
mismatch is repaired across all invoice consumers. Reports-page independent
money/payroll calculations are also still unqualified and not claimed here.

Required critical/physical-device/all-role/provider/image/current-CI/staging
restore and rollback gates remain. BUG-DATA-0016 stays OPEN with all five P0s.
The three genuine communication receipt checks are not replaced by mocks.
Mini/Plus/Pro remain unqualified; Ultra deferred; stop before model loading.
No main merge, Azure/model activation or environment publication. Environment
readiness is restored and restricted policy is observed enforced. Current local
snapshot reports configured VPN and automatic Tailscale access; empty legacy
TCP grants alone no longer establish a laptop blocker. No service-specific
laptop connectivity test was performed by this dashboard packet. No settings,
privacy or public internet policy were changed.

Next route: Sol High for monetary contract/SQL/integration/security acceptance;
Sol Medium for settled source import/reporting. GitHub remains durable exchange;
no live D: drive synchronization is asserted. Exact CI status remains UNKNOWN;
this feature branch is outside verify.yml's push filters, no dispatch made.
