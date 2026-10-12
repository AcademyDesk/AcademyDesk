# Phase 2B — Payroll linked browser, save feedback and UTC readback

Date:2026-10-12 IST. Repository `D:\AcademyDesk-codex-p0`, branch `codex/penta-search`, starting HEAD `4de3b9c2a710f0d818624f0fa4de77bec362e5c3`. Continuation of existing BUG-DATA-0056 and original async/feedback findings, not a new audit or test plan. Issues remain **OPEN / RETEST**.

## Scope and reproduced defects

The prior [SQL overlap batch](PHASE_2B_PAYROLL_HISTORY_CONCURRENCY_AUDIT.md) remains accepted at its bounded evidence level. Do not replace its24 measured pairs with a browser run or imply full issue closure.

Both actual Payroll TSX submit handlers accessed `event.currentTarget.reset()` after awaiting POST. The frozen starting source under two focused controlled React-event-lifetime tests failed2/2 with `Cannot read properties of null (reading 'reset')` after a simulated confirmed write. Controlled tests reproduce event lifetime, not database persistence. First linked browser run reached real profile201 and verified stored SQL/audit; the repaired handler showed durable confirmation.

Bounded UI repair captures the native form before await, serializes the unchanged payload before disabling fields, and maintains one synchronous pending guard across both forms and authoritative readback. Checked uncached initial lists are required before writes. Native fieldsets disable controls while pending or unresolved. Success remains after readback; rejected400/403 retains draft and uses backend guidance/safe fallback. Network/5xx does not invite a blind retry: retains draft, blocks both forms, and requires explicit register refresh and inspection. Confirmed write/readback failure is explicitly distinct; no automatic write retry or client-side claim of idempotency. Late unmounted results do not reset forms or update visible state.

Second linked run detected a genuine existing response inconsistency, not a monetary mismatch: creation serialized `paidAtUtc` with `Z`, SQL materialization/readback omitted it. Raw POST/GET JSON equality remained strict; it was not normalized away in the browser test. Two new targeted tests using SQL-like Unspecified DateTime failed2/2 before repair for snapshot and legacy payouts. GET now specifies UTC Kind in its DTO projection, like existing identity-recording provenance. Stored datetime ticks, identity labels, financial values, schema, domain validations and permissions are unchanged. Final linked proof must retain exact raw JSON equality.

No Mini/Core/tool/authority, payroll calculations, zero-net policy, identity backfill, duplicate-period policy or migration change. Source printing still calls whole-page `window.print()` and is **not** proof of a row-specific payslip PDF. Existing BUG-FUNC-0033 remains separate.

## Verified local checks

- 27/27 actual TSX controlled checks PASS: both action types, confirmed/reset/accessibility, failed readback,400/403/malformed/network/500, same-tick duplicate/opposing form/refresh during write and readback, unmount, initial incomplete workspace, exact monthly/SessionBlock/staff payloads, stable loading.
- 68/68 freshly built targeted PayrollHistory/PayrollNet/AuditOutcome tests PASS:66 retained +two UTC cases,0 failures/skips. Initial unchanged-artifact66 also passed, but final68 was rebuilt after the API repair.
- TypeScript PASS; target Payroll lint0 errors/0 warnings. Production webpack build/export83/83 pages PASS.
- SqlHarness final rebuild0 warnings/0 errors,8.28s. Initial QA build failed on a multiple-declarator `var`; corrected harness syntax and rebuilt, no application assertion weakened.
- Native history/migration/monthly/SessionBlock retained stages run in every linked attempt. Zero-net remains POLICY-PENDING, never silently accepted from those runs.

Builds use the preserved local working snapshot, including unrelated uncommitted Mini/platform work; this is **not** clean exact-release-candidate validation. Scoped publication excludes those earlier diffs. Full candidate release gates remain required.

## Linked test design

Opt-in `QA_PAYROLL_BROWSER=1` requires Payroll module and guarded history before SQL/container creation. Existing runner, marker validation and exact owned cleanup remain in force; do not use baseline diagnostic mode for acceptance.

Production static export is served read-only on dynamic loopback. Headless installed Edge executes actual React controls through Playwright. Requests are relayed, not mocked, through unchanged real Identity/API middleware to the restricted runtime login in this run's disposable SQL. A loopback-only random test nonce controls audit INSERT denial on marker-validated owned SQL; it is not an application endpoint. Semaphore/pacing and a60-second limiter-window wait apply only to QA transport; production limiter remains120/minute, not bypassed.

Four journeys:320/1440px × light/dark; monthly teacher and session-based staff profile/payout creation. Each checks six register stages: initial, saved, reloaded, rejected/recovered, renamed/relinked, inactive. Edits/deactivation use allowlisted real APIs because this Payroll page has no profile-edit UI; those are not invented UI-editor proof. Creation and payout recording use actual visible form controls, including dropdown collapse and calendar Today. Forced extra submit events during a delayed real save prove no additional POST from either form.

For every request, fresh canonical all-row/field snapshots cover PayrollProfiles, PayrollPayouts, AuditLogs, Invoices and Payments. Reads and failed mutations must be exact no-write. Success permits only the intended profile/payout and exactly one audit with actor/academy/action/route; all other history, foreign sentinels and finance rows remain exact. New payout stored name/type/linked IDs/amounts/sessions/metadata must agree with the profile and authoritative response. Browser readback asserts raw historical DTO equality through rename/type/link/deactivation, all four explicit legacy labels, foreign exclusion and register counts. These five tables are not a claim of all-table/Identity side-effect coverage.

One payout audit INSERT fault must return500 with full rollback, retain browser draft/block repeat, restore permission in finally, explicitly refresh, then successfully save the same captured profile/pay details. Each journey also rejects deductions2000 against gross1000 with the existing400 message/no write, retains values, then records corrected200 deductions/net800. Four final linked profiles, eight payouts, two per profile; all final profiles inactive, old payouts unchanged. No429 accepted. Device emulation is not physical Android/iOS.

## Attempt receipts and limitations

Private receipts remain untracked in `QA/EVIDENCE/payroll-browser-20261012/`; generated artifacts remain ignored. Early full controlled-baseline diagnostics had asynchronous follow-on failures when the old source lacked recovery actions; the focused frozen2-case receipt is the claimed null-reset reproduction, not a distinct-defect count.

Attempt40479e9acda547df93684bfb0a2b58c0 (port63010,141.9s) stopped on an incorrect Playwright fieldset `isDisabled` expectation after confirmed owned audit500 rollback. Corrected to the DOM fieldset.disabled property **and** inherited native-input disabled state; repeated-POST prevention remained required. Attempt9f7abcbb39ec4605ab178e8c22732d74 (port60173,147.4s) then stopped on the real raw UTC serialization mismatch after save; the API repair above fixes it, no equality relaxation. Each failed-run container was inspected for exact name/run label/loopback binding then only that owned SQL container was stopped/removed, including its disposable DB/login. No other containers pruned. Both failed receipts retained; failed-run final harness DB/login cleanup did not execute, so do not claim that guard passed in failed attempts.

Final run `921728d928a943e3a118b78d6c14f672`, started `2026-10-12T13:27:57.8161256Z`, loopback SQL port52205, elapsed308.8s, exit0: **four journeys /24 register states PASS,20 successful writes/four400/one audit500 rollback, two audit controls,221 unchanged non-mutation five-table snapshots, four profiles/eight payouts/exact audits/no429**. Raw POST/GET timestamps now match without normalization.34 retained history controls, legacy-preservation/model migration proof,14 original payroll rules,89 application/7 Identity migrations, exact runtime ownership and mismatched-marker cleanup refusal PASS. Final owned DB/login/container removed; all nine initial container IDs/names remain. No other container restarted/pruned or production DB touched.

Screenshots/receipt are local in `QA/EVIDENCE/payroll-live-1791811796222/`,24 stage images and receipt.json. Final-run320px/light and320px/dark saved-state screenshots visually reviewed; form separation/accessible notices and horizontal register scrolling retained. No whole-portal redesign/accessibility/device acceptance claimed. Browser exceptions and unexpected console errors zero; the four intended400 and one intended500 resource-console errors are classified/checked, not hidden or counted as unexpected errors.

The browser also **reproduced two stored0.01 payouts rendering₹0 in each of the four journeys**. This is explicitly a NOT ACCEPTED monetary display observation and next repair, not a reason to relax current money/SQL assertions. The aggregate and multi-currency display likewise need separate qualification. The current batch preserves the old formatter, not accepts its precision.

Existing24 measured concurrency pairs retained, not rerun by the linked browser flag. Physical Android/iOS, direct/platform/stale/critical, representative existing-database backup/restore/rollback and release gates remain open. Payroll worker-link validation BUG-DATA-0009, duplicate/idempotency/sub-cent/legacy provenance and accountable zero-net/disbursement policy remain separate. This is not production acceptance or a real bank transfer.

## Commands

```powershell
node --test QA/tools/payroll-feedback.test.cjs
node apps/web/node_modules/typescript/bin/tsc --noEmit -p apps/web/tsconfig.json
# From apps/web: node node_modules/eslint/bin/eslint.js src/app/payroll/page.tsx
# From apps/web: node node_modules/next/dist/bin/next build --webpack
dotnet test tests/AcademyDesk.Api.Tests/AcademyDesk.Api.Tests.csproj --artifacts-path .build-check/payroll-history-tests --no-restore --filter 'FullyQualifiedName~PayrollHistoryTests|FullyQualifiedName~PayrollNetTests|FullyQualifiedName~AuditOutcomeTests' --nologo -v minimal
dotnet build QA/tools/SqlHarness/SqlHarness.csproj --artifacts-path .build-check/payroll-history-sql --no-restore --nologo -v minimal
$env:QA_PAYROLL_HISTORY='guarded'
$env:QA_PAYROLL_BROWSER='1'
powershell -NoProfile -File QA/tools/SqlHarness/Run-ReconciledPayment.ps1 -Module Payroll
```

Actual runner used the bundled pwsh executable. Synthetic invalid-domain fixtures only; no real credentials printed or committed.

## Next route and continuity

Stay **Sol High** for bounded Payroll monetary display and selected-payslip BUG-FUNC-0033 reproduction/repair/linked proof, then existing worker-link and broader security/finance acceptance queue. Physical/wider release gates remain separate. Use **Sol Medium** for settled Manual visual rollout, not a finance/authority shortcut. Preserve all four Manual portal Orbit + Aura dark/Prism light work and current interrupted UI diffs. Mini-owned FW1 UI delivery/qualification remain pending;86/94 is NOT ACCEPTED and90% eligible AI is a product target, not readiness. No project switch or Mini shared-contract handoff required for these host-only repairs. No main merge, Azure deployment, production DB, new testing plan or prior-audit restart.
