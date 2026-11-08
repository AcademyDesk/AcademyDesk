# C21 Dashboard detail availability — 8 November 2026

Cloud owner. CLAIMED; implementation and verification pending.
Branch `codex/cloud-c21-dashboard-detail-availability-20261108`.
Clean base `d22ca1aa2a89e488351102adce99712923114499` (published C20).
Latest Desktop `fe3167b12cbd318369d159bf59a06dacfa2066ae`: C19 is integrated,
34 HTTP/SQL,2,161 API and eight live Payments browser/SQL phases passed in their
recorded scopes. Desktop retains protected financial write/SQL validation and
remaining P0 runtime work. C20 is a separate required source return. No current
competing Dashboard source claim found; preserve C19 SQL and current handoffs.

Exact paths:

- `apps/web/src/app/dashboard/page.tsx`
- `QA/tools/dashboard-detail-availability.test.cjs`
- `QA/tools/dashboard-detail-availability-browser.cjs`
- `QA/ISSUES/BUG-DATA-0016.md` — preserve all source-bound SQL/native history
- `QA/HANDOFF/C21_DASHBOARD_DETAIL_AVAILABILITY_2026-11-08.md`

Keep authoritative overview data and existing role/session/tenant gating.
Optional invoice/payroll/student/session/attendance failures must not become
empty-success claims or invented zero attendance. Isolate each optional read,
track availability independently from its rows, and show truthful affected
section alerts while retaining successful independent detail data. Reload is
read-only, explicit and does not clear credentials for an optional module denial.
A failed required auth-session response must not expose ready indicators.
Preserve state-hook order used by retained session tests, C19 monetary groups,
invoice balance/currency/status rules, separate payroll and primary schedule.
No backend/write/migration/domain/permissions/model/API-contract change.

Before/after actual TSX, retained C19 monetary and primary session gates, native
Chromium at narrow/wide views with controlled independent read failures/recovery,
exact build/types/lint and full frontend. Existing backend2,177 evidence may be
reused only after proving identical API/test trees; no new SQL/backend pass claim.
Keep BUG-DATA-0016 and five P0s OPEN. No new packages/images, live laptop/customer/
provider calls, main/Azure/model/environment/privacy/public-internet changes.
Sol High for authority/error semantics; Medium for settled imports/docs.

## Protected-write handoff (read-only source review)

Do not edit these locally owned production paths from C21. C20 fixed GET Invoice
List currency matching; the following independent write sums still require
Desktop's original-case reproduction and a scoped repair claim:

- InvoicesController.UpdateStatus: accepted-payment SUM currently has no
  matching-currency predicate. Legacy INR receipts on a USD invoice can influence
  whether a requested status agrees with its balance.
- PaymentsController.Create and UpdateStatus void recalculation: their sums also
  include legacy different-currency receipts. RestoreBalanceAsync validates the
  target payment currency but its other-payment sum is independent.
- FinanceAdjustmentsController.Decide: its paid SUM needs the same legacy review
  before deciding remaining balance and persisted invoice status.

Reproduce only on disposable data: USD100 invoice, no adjustment, accepted INR50
legacy receipt; compare matching USD50 controls. Assert USD receivable stays100
until a matching receipt/approved adjustment applies, other units and foreign/
non-collected rows cannot settle it, exact amounts/statuses remain consistent,
and audit-fault/race paths retain current transactions. Do not infer every
write action's expected disposition from a read fix; preserve explicit
restore/reconciliation/cancellation rules and raw legacy data. This is a static
handoff, not newly executed SQL or a claim those writes were fixed. Use the
existing financial regression and protected SQL harness; don't redo accepted
valid-currency modules from zero or weaken their audits/concurrency controls.
