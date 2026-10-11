# Invoice status consumers and linked browser — bounded verification

2026-10-12 IST (runs started2026-10-11 UTC). Worktree `D:\AcademyDesk-codex-p0`, branch `codex/penta-search`, starting HEAD `c645a585e586ca51bb0999fb79cdf7b35d1862d5`.

## Scope and outcome

Continue [BUG-DATA-0055](../ISSUES/BUG-DATA-0055.md) after the accepted [ledger repair](PHASE_2B_INVOICE_STATUS_LEDGER_REPAIR.md) and [concurrency/audit gate](PHASE_2B_INVOICE_STATUS_CONCURRENCY_AUDIT.md). QA-only: strengthen retained consumer status/document assertions; add opt-in real linked browser coverage. No application/API/domain/schema/RBAC/financial-policy/Mini/Core/frontend source change. No audit restart or new testing plan.

**Bounded PASS:** six retained native consumer stages and four browser journeys/24 page-states;16 real mutations (eight status200, four payment201, four rejected status400),200 non-mutation full finance-snapshot controls, no429. **NOT full consumer/visual/issue/release acceptance:** the display gaps below remain OPEN.

## Native consumer checks

The existing `Consumers` regression remains intact; Admin and Student invoice status now must equal fresh SQL, and downloaded HTML must contain that status as well as the existing exact adjustment/paid/balance amounts.

- Mixed approved adjustment200 plus Reconciled600/Completed100/Voided75: collected700, balance100.
- Exact adjusted settlement: collected800, balance0; fully adjusted1000: collected0, balance0. Existing no-cash semantics characterized, not policy approval.
- PendingApproval and Rejected200 remain unapplied: collected600, balance400.
- Approved200 plus799.99 collection: balance0.01 retained in SQL, Admin/Student API, downloaded document, dashboard and reminder notification.
- Positive-balance reminders queue once; settled/fully adjusted reminders queue none. Financial snapshots unchanged by these reads/reminders. Existing foreign/student reminder and foreign-student document denials retained; tenant preflight checks retained.

These are six named stages, not a claim that every finance/RBAC action was retested. Previous111 targeted API tests,45 status controls and20+9 concurrency/audit controls are **retained, not rerun** in this QA-only batch.

## Real linked browser gate

Fresh production webpack export, headless installed Edge,320/1440px × light/dark. No API response mocks. A loopback-only relay forwards real authentication and API calls to the guarded ASP.NET host backed by run-owned SQL; it permits only GET/OPTIONS, login, the four test invoices' status PATCHes and their exact400 payment POSTs. Pacing respects the unchanged production120/min limiter.

Four synthetic invoices1000 each start PartiallyPaid with a Reconciled600 row and yesterday's due date. Each journey:

1. Real API PATCH ` partiallypaid `200; invalid `Paid`400 with the existing exact ledger error, no changes in Invoices/Payments/FinanceAdjustments/AuditLogs.
2. Real Invoices page: canonical register status, count-based KPIs, selected preview **status**. Preview's gross-money wording is not accepted as remaining-balance presentation.
3. Real Finance Governance: target collection balance400, paid600/adjusted0, one day overdue, visible target card and matching queue KPI.
4. Real Student portal: status/balance400, Outstanding/Paid/All filtering; UI download action produces authoritative HTML with paid600/balance400/statusPartiallyPaid.
5. Real payment POST400201; PATCH ` paid `200. Reload all three pages: statusPaid, matching KPIs, target removed from collections, Student balance0 and paid filter; UI-downloaded HTML paid1,000.00/balance0.00/statusPaid.

Mutations are deliberate **API calls**, not invented UI status/payment controls. Browser navigation/filter/preview/download actions use real pages. Final four invoices each have exactly two payments: one Reconciled600 and one Completed400, collected1000; original reconciliation evidence and positive foreign invoice/payment sentinel retained. Every linked operation compares full canonically ordered four-table persisted snapshots; successes permit only the target status/new payment and exactly one actor/tenant/action/route-correct success audit. Reads/logins leave those four finance tables unchanged (not a claim that Identity or every database table is read-only). No approximate numeric/rounding equality or weakened prior assertions.

Zero console/page errors and no document-level horizontal overflow in the named pages/states.24 full-page screenshots/eight downloaded HTML documents retained; light/dark mobile screenshots inspected. This is not keyboard/accessibility/all-layout or physical Android/iOS acceptance.

## Open display follow-ups discovered

- `apps/web/src/app/portal/page.tsx` forces `maximumFractionDigits:0` in `cash`, used by home fee balance and invoice rows. Native0.01 remains correct, but linked screenshots render that partially paid invoice as₹0 while its reminder says INR0.01. **OPEN:** preserve currency minor-unit precision; add strict linked positive-fraction/zero/filter/home checks. Current integer400/0 browser assertions do not accept the fractional display.
- `apps/web/src/app/invoices/page.tsx` labels gross `totalAmount` as **Total payable**, including an already settled invoice. **OPEN presentation clarification:** distinguish original invoice total from server-authoritative remaining due/paid/adjustments. No tax/IFRS/GST or cancellation/refund policy inferred. Existing invoice table/preview small-screen internal layout is not final Manual redesign acceptance.

## Builds, evidence and cleanup

- `dotnet build QA/tools/SqlHarness/SqlHarness.csproj --artifacts-path .build-check/invoice-consumers-sql --nologo -v minimal`: PASS0 warnings/0 errors,109.52s.
- `npm run build -- --webpack` in `apps/web`: fresh TypeScript and83/83 export PASS; browser JS syntax/diff checks PASS. Build includes the existing dirty snapshot; unrelated Mini/Platform changes are not accepted/published by this batch.
- Failed run `20abd5aea9774d4789b34ac4efab7bae`, port56854,152.8s: browser selector matched four identical400 cards. Retained `QA/EVIDENCE/invoice-consumers-20261012/sql-final.txt` and `invoice-consumers-live-1791744819667/failure.txt`. Selector scoped to exact invoice, assertions unchanged.
- Failed run `8a3c3347c4994a059056cfabf35021ea`, port54913,170.8s: exact document assertion omitted the correct thousands separator in1,000.00. Retained `sql-final-scoped.txt`/`invoice-consumers-live-1791744994427/failure.txt` and HTML. Correct N2-style expectation, no tolerance or app change.
- Both failed containers individually ownership/name/label/loopback verified then removed; their ephemeral SQL discarded. Failed runs **do not have normal database/login teardown receipts**; failure evidence retained, no unrelated container pruning.
- Final PASS run `1787e8dba86e45c4b7c371e502d704a9`, SQL127.0.0.1:49684, started2026-10-11T18:58:06.1508660Z, exit0/265s. `sql-final-document-format.txt`; linked `QA/EVIDENCE/invoice-consumers-live-1791745178323/result.json` reports4 journeys/24 page-states/16 mutations.200 non-mutation finance controls;88 application/7 Identity migrations, exact restricted runtime target, mismatched-marker cleanup refusal and exact owned database/login/container teardown. Original two running/seven stopped Docker containers preserved.

Reproduce: build/export above; set `QA_INVOICE_CONSUMER_BROWSER=1`, then `powershell -NoProfile -File QA/tools/SqlHarness/Run-ReconciledPayment.ps1 -Module Consumers`. Without that opt-in the existing consumer module remains the bounded native path.

## Continuity and next task

BUG-DATA-0055 remains **OPEN/RETEST**. Next **Sol High**: bounded monetary-display clarification/minor-unit fix and strict linked regressions, then existing0056 historical payroll attribution queue. Direct/platform/stale/physical-device/critical/release and financial-policy gates remain separate. No full enterprise suite rerun, PENTA Mini handoff/contract change, main merge or Azure. Mini86/94 NOT ACCEPTED, versioned FW1 and90% eligible-AI target/Manual Orbit + Aura/Prism program retained.

Scoped feature publication only: preserve the pre-existing14 tracked diffs (including older32/28 continuity additions), unrelated untracked work and local evidence; `.build-check` stays ignored. Legacy aggregate QA validator remains blocked by missing historical `QA/EVIDENCE/logs/observed-checks.json`; no evidence fabricated, aggregate gate not claimed PASS.
