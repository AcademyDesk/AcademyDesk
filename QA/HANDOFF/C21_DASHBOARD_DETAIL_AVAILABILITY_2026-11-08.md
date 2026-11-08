# C21 Dashboard detail availability — 8 November 2026

Cloud owner. IMPLEMENTED / CLOUD VALIDATED; local real-host acceptance pending.
Branch `codex/cloud-c21-dashboard-detail-availability-20261108`.
Implementation `903a9e502eb9cb4b87af7a82638c756fcdff683a`; claim `29479080a1c01eacfe40a7d3c810a6c3e2014dd9`.
Clean base `d22ca1aa2a89e488351102adce99712923114499` (published C20).
Latest Desktop `fe3167b12cbd318369d159bf59a06dacfa2066ae`: C19 is integrated,
34 HTTP/SQL,2,161 API and eight live Payments browser/SQL phases passed in their
recorded scopes. Desktop retains protected financial write/SQL validation and
remaining P0 runtime work. C20 was separate at claim; final Desktop refresh below confirms integration. No current
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

## Concrete legacy-write reproduction requests for Desktop

Static predictions from the unchanged write queries, NOT newly executed results.
Use a future-due USD100 fixture and reset fresh state per case; retain all raw
currency metadata. Compare otherwise identical matching-USD controls.

| Action and seeded receipt | Observed source implication | Required matching-currency review |
| --- | --- | --- |
| PATCH invoice PartiallyPaid; legacy INR50 Completed | Existing sum can accept PartiallyPaid despite zero USD collected | Should remain Issued while USD debt is100; reject status inconsistent with that balance |
| POST payment USD100; legacy INR50 Completed | Existing sum can reject the actual full USD balance as overcollection | Validate that the USD100 settlement is accepted once, with an attributed audit |
| Void USD10 target; other accepted INR100 | Remaining-payment sum can mark invoice Paid using another unit | No USD receipt remains; status and USD100 debt must agree with a future-due invoice |
| Restore Voided USD100 target; accepted INR50 other receipt | Other-payment sum can reject valid restoration | Preserve the approved restore rules, ignore other units for USD capacity, and verify audit/race rollback |
| Approve USD60 Discount; accepted INR50 receipt | Paid sum can reject the adjustment as exceeding remaining balance | Validate USD adjustment60/debt40 and status, with approval/audit/transaction rules intact |

This table does not authorize a data rewrite, currency conversion, payment
promotion, or alternate reconciliation/refund policy. It requests source-bound
reproduction and appropriately claimed source repair by the financial owner.

## Implementation and targeted evidence

Original actual-TSX33 cases:3 PASS /30 FAIL. New tests invoke the actual pure
DetailList renderer as well as the page, not a fixture substitute for empty/
loading/error text. Final target60/60 PASS:33 new cases plus all14 unchanged
C19 currency checks and13 unchanged primary session-recovery cases.

Detail reads distinguish loading, ready and unavailable independently from
empty arrays. Each request catches transport, non-OK, JSON and root-shape
failures without turning them into empty-success states. The primary Dashboard
and auth session must both succeed; then overview indicators become usable
while optional details load. Auth-session500/503 now stays gated. Existing
401/403/429 primary handling and hook order are preserved. Optional module
failure/denial does not change account authority or explicitly clear tokens.

Failed invoice/payroll sections show alerts independently, retaining the other
section's successful rows. Failed student names do not hide invoice IDs/amounts;
the missing names are labelled. Attendance details remain unavailable if student
or session data or any attendance read fails, rather than reporting a percentage
from incomplete data. Primary timetable data is preserved when the optional
session list fails. Real successful empty lists retain genuine empty messages.
Reload details is an explicit GET-only refresh; loading and late unmounted reads
are covered. No mutation retry, new role/module access or API contract change.

C20's2,177 backend passes are REUSED after534 API/project/test source paths and
both compiled assemblies matched the prior source/artifact SHA256 manifest.
There is no newly run API/SQL claim. The existing full API results apply only to
that unchanged Cloud tree, not Desktop's newer support/profile audit source.

## Final validation

| Check | Actual result |
| --- | --- |
| Original TSX |3 PASS /30 FAIL;33 cases; zero skips |
| Target new + retained TSX/session |60/60 PASS; zero skips |
| Full frontend |2,151 PASS /3 FAIL;2,154 tests across80 files; zero skips/cancellations |
| New native Chromium |26/26 PASS;100 detail-dialog checks; zero runtime/interceptor errors or API writes |
| Retained C19 native |14/14 PASS; unchanged driver/assertions |
| Exact-lock build/types/lint |PASS; zero lint errors /62 existing warnings |
| Export integrity |570 SHA256 values unchanged after both native runs |
| Backend |2,177 prior passes REUSED;534 identical API/test paths and two unchanged assemblies verified |
| Production guard |BLOCKED as required by the five still-open P0 records |

The33 new TSX cases cover each optional401/403/503 response, transport failure,
invalid JSON and root shape, independent concurrent financial failures, true
empty lists, missing names with available balances, retained primary schedule,
required auth-session500/503, read-only reload and pending/unmounted late reads.
The retained14 currency and13 session tests were not edited or weakened.

Native26 scenarios at320/light and1440/dark cover successful, empty, independently
failed invoice/payroll/student/session/attendance reads, combined financial
failure, transport reset, malformed payroll body, pending invoice release,
required auth-session503 and reload recovery. Four modal types are verified
per successful overview, plus pending/reload modal checks (100 in total).
Displayed USD80.40 receivable and primary50% attendance stay unchanged; failures
cannot claim no debt, no payroll or a percentage from partial detail data.
Successful independent rows and primary timetable remain visible. Optional
module failures retain the synthetic session token. All request fixtures are
GET-only; no domain mutation occurs. Existing shared auth refresh behavior is
unchanged, not replaced or independently qualified by this packet.

Both native runners used normal Chromium sandbox and removed only their owned
temporary profiles. Controlled failure screenshots at both widths were captured;
this is not physical-device/real-Identity/SQL qualification. Both new26-case and
unchanged C19 fourteen-case runners passed on the same exact export, without
retries or assertion changes. The only full frontend failures remain genuine
Email/WhatsApp/Meeting SQL receipts absent in Cloud; they remain FAIL, not skips
or fabricated evidence. Next16.4.3/React19.2.8 built89 entries and87 unchanged RSC
aliases. Loopback49142 is test configuration, not a production export setting.

All original/final logs, unchanged tests, full per-file frontend results,
new/retained native receipts and screenshots, export fingerprints, unchanged
backend reuse receipt and source/artifact/publication SHA256 manifest stay
outside git at `/workspace/c21-dashboard-detail-availability-20261108`.

```sh
node --test QA/tools/dashboard-detail-availability.test.cjs QA/tools/dashboard-currency-summary.test.cjs QA/tools/session-recovery-ui.test.cjs
node QA/tools/run-frontend-tests.cjs
# apps/web:
./node_modules/.bin/tsc --noEmit
npm run lint
NEXT_PUBLIC_API_URL=http://127.0.0.1:49142 npm run build -- --webpack
# repository root:
QA_BROWSER_EXECUTABLE=/usr/bin/chromium node QA/tools/dashboard-detail-availability-browser.cjs
QA_BROWSER_EXECUTABLE=/usr/bin/chromium node QA/tools/dashboard-currency-summary-browser.cjs
node QA/tools/check-production-gates.cjs # expected exit1 for five open P0s
git diff --check
```

## Desktop integration and remaining priorities

Apply only these five C21 paths after the C20 return, preserving all newer local
SQL/profile/support/payment-browser work and shared governance. Reconcile issue
history instead of replacing the whole newer record. This frontend-only packet
does not invalidate the byte-matching C19 backend SQL results or claim a new SQL
run. It does require actual Dashboard modal/read-failure/session/tenant/device
acceptance against the combined real-host candidate; preserve existing C19/C20
financial assertions and test optional denials independently from primary
account/session failures. No new permission or fallback bypass is introduced.

Priority remains Desktop's original P0 runtime program, C20 SQL/Reports and the
specific legacy monetary-write reproduction table above; Cloud must not edit
those protected paths without a source-ownership handoff. Current CI remains
UNKNOWN (this feature push is outside verify.yml's trigger filters); no workflow
dispatched. Real provider/device/all-role/security/images/staging restore and
rollback, five P0s and BUG-DATA-0016 remain OPEN. Mini/Plus/Pro unqualified; Ultra
deferred; stop before loading/training/inference/activation. No main merge,
Azure deployment, public internet/privacy or environment publication changes.

## Final coordination refresh

Desktop `88a3a45218ced1e4995d83fdcc1d4574cffb29c0` integrated C20 at `512e785` and explicitly confirms
Cloud owns C21 while Desktop owns the protected legacy-currency write repair.
Its source-bound2,183 API and32 Reports checks passed. Real Reports/browser/SQL
passed five phases plus three authority checks: multi-currency amounts,
Reconciled receipts, separate payroll, interrupted-read retry, three dialogs,
both matching invoice CSVs and expense export, tenant/role/module denials,
and audited void/evidence-based restoration with readback. This is new local
acceptance, not an additional Cloud API/SQL run or complete P0 closure.

The same local fixture reproduced the write gap: USD70.15 displayed as due,
but POST of that amount returned400 because the accepted INR70.15 legacy
receipt remained in the writer's sum; captured SQL stayed unchanged. Desktop
has the concrete reproduction and owns the repair matrix. The static table
above remains a requested additional matrix, not evidence that all its branches
were executed. Preserve the local lease/transaction/audit/race safeguards and
original service restoration record. Do not duplicate its active source edits.

[Exact local Reports, payment transitions and legacy reproduction](https://github.com/AcademyDesk/AcademyDesk/blob/88a3a45218ced1e4995d83fdcc1d4574cffb29c0/QA/REPORTS/LOCAL_C20_REPORTS_PAYMENT_TRANSITIONS_2026-11-08.md).

C21 integration should preserve that newer local evidence, governance and
protected-write work. Current issue register remains OPEN pending required
critical/device and release gates; do not replace newer issue files wholesale.
