# Invoice monetary display — bounded repair

2026-10-12 IST. Worktree `D:\AcademyDesk-codex-p0`, branch `codex/penta-search`, starting HEAD `ca26cc4e417fac1eabb10db22b4fe65402e71a8b`.

## Scope

Continue the two display gaps from the [linked consumer report](PHASE_2B_INVOICE_CONSUMERS_LINKED.md), associated with [BUG-DATA-0055](../ISSUES/BUG-DATA-0055.md). No API/domain/schema/permission/transaction/financial-policy/Mini/Core changes. Do not restart accepted audits or close the enterprise program.

- Student `cash` no longer forces whole currency units. `Intl`'s currency minor-unit precision preserves INR/USD fractions, JPY whole units and KWD three digits. Both existing home and invoice-row uses remain intact.
- Invoice preview now uses a named shared `InvoiceLedgerSummary`: **Invoice total**, **Approved adjustments**, **Collected payments**, **Remaining due**. Values come directly from the server's existing total/adjusted/paid/balance projection; status does not imply a collected amount. Original invoice amount in the register/detail table, currency, branding, signature, print/close actions remain unchanged.
- The existing invoice summary contract already returns these values, including `balance`. The frontend type now reflects that contract rather than substituting gross amount or a fallback zero.

This is presentation correctness, not IFRS/GST certification, credit-note/refund/cancellation policy approval or a broad Manual visual redesign. Existing fully adjusted/no-cash `Paid` semantics are retained, not endorsed. Existing mixed-currency aggregate/home semantics, stale selected previews and all document themes/devices remain outside this bounded acceptance.

## Source/build checks

`node QA/tools/invoice-money-display.test.cjs <baseline-SHA>` reads the actual frozen source through `git show`, without checking out/resetting files. The same assertions render the actual old preview expression versus the new React component. Baseline **4 PASS / 12 FAIL**; final **16/16 PASS**: two TSX syntax controls, seven actual Student currency values, shared home/row usage, five preview states (partial, paid, fraction, adjusted settlement, no-cash), and authoritative-balance integration. These are controlled source/render assertions, not native SQL or physical-device tests.

- Production `npm run build -- --webpack`: TypeScript and **83/83** exported pages PASS.
- `dotnet build QA/tools/SqlHarness/SqlHarness.csproj --artifacts-path .build-check/invoice-consumers-sql --nologo -v minimal`: **0 warnings / 0 errors**, 44.04 seconds.
- Target lint: **0 errors / 5 inherited page warnings**. Same three Invoices/two Student warnings independently confirmed against HEAD using ESLint stdin. New component has no lint diagnostics. Warnings not suppressed or claimed clean.
- JS syntax/diff checks PASS. First edited-page build detected an accidentally omitted closing brace; restored before the successful build. An initial QA command used the web working directory and failed to find root test files; corrected root commands passed. First quoted PowerShell launch failed parsing **before SQL creation**; correct process-scoped environment/runner launch follows. No global execution-policy change.

## Linked SQL/browser gate

**PASS:** run `85dbbb61fb434106b2a1b9ef73b9399d`, SQL loopback port53110, started2026-10-11T19:19:00.5242737Z; exit0,500.7 seconds. Six retained native stages and four linked browser journeys/**60 page-states** (24 retained +36 new),16 mutations (12 successes/four400 no-write),**480 non-mutation full finance-snapshot controls** passed. Final four mutable target invoices each retain exactly two payment rows, collected1000 and balance0; no429/console errors.88 application/7 Identity migrations, exact scoped runtime login, negative mismatched-marker cleanup refusal and owned database/login/container teardown verified. Original two running/seven stopped Docker containers preserved; no pruning.

Existing four target invoices, 16 exact mutations, rejection message, full four-finance-table snapshots, exact success audits, original reconciliation evidence and foreign sentinel remain checked. Add three read-only seeded ledger projections: gross1000/adjusted200/collected799.99/balance0.01; fully adjusted1000/collected0/balance0; already collected1000/balance0. These seeds characterize existing projection/zero-cash behavior, not three newly approved domain operations. The retained native stages exercise actual adjustment approval separately.

Each 320/1440px × light/dark journey retains the two original states and adds the three display states across real Invoices, Collections and Student pages. Checks include all four preview values; Student home aggregate retains fractional precision; exact invoice row amounts, outstanding/paid/all filters and real downloaded HTML values/status. No API mocks, tolerances, fallback fabricated responses or production limiter override. Mutations remain real API calls, not invented page controls.

## Evidence and boundaries

Local evidence: `QA/EVIDENCE/invoice-money-20261012/source-baseline.txt`, `source-final.txt`, `sql-final.txt`; browser directory `QA/EVIDENCE/invoice-consumers-live-1791746439092/`, independently parsed `result.json`,60 full-page screenshots and20 real UI-downloaded HTML documents. Mobile light partial/cent Student and invoice screenshots and dark fully adjusted preview inspected. No document-level horizontal overflow in the named page states. Existing internally scrollable invoice table/print preview and broader accessibility/device/theme acceptance are not closed by these screenshots. No failed SQL/browser run in this batch; earlier build/command failures are recorded above.

Prior111 unit/45 status/20 overlap +9 audit controls are retained, not rerun here. Full enterprise/critical, direct/platform/stale/concurrent context, physical Android/iOS, legal/policy and pre-Azure gates remain OPEN. BUG-DATA-0055 stays **OPEN/RETEST**. Mini86/94 NOT ACCEPTED, FW1 shared frontend dependency, matching Manual Orbit + Aura/Prism and90% eligible-AI target remain unchanged.

Next **Sol High**: existing [BUG-DATA-0056](../ISSUES/BUG-DATA-0056.md) payroll history attribution — reproduce the static finding in disposable SQL before deciding on a repair. Sol Medium remains appropriate for settled independent Manual styling. No PENTA Mini contract change/project switch required by this batch.

Publication is scoped feature-branch only after final gate/secret/preservation checks; no main merge or Azure. Preserve14 pre-existing tracked diffs, older32/28 continuity additions, unrelated untracked work and local evidence. `.build-check` remains ignored. Legacy aggregate validator still lacks historical `QA/EVIDENCE/logs/observed-checks.json`; no aggregate PASS or fabricated receipt.
