# C19 academy dashboard monetary readback — 8 November 2026

Cloud owner. Status CLAIMED; implementation/verification pending.
Branch `codex/cloud-c19-dashboard-balances-20261108`.
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
