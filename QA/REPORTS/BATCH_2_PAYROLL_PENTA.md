# Batch2 — payroll Manual presentation

2026-10-15. Worktree `D:\AcademyDesk-codex-p0`, branch `codex/penta-search`, HEAD `b1f8cb6`. Bounded presentation checkpoint continuing existing E3;not full payroll/portal/release acceptance.

## Implemented

- Scoped payroll stylesheet aligns existing PENTA semantic tokens,18px surfaces,readable spacing,wrapping titles/large amounts and responsive forms across Prism light/Orbit dark. No global CSS/vendor changes.
- Worker type,worker,payment cycle,payout profile and payment method use persistent shared-control labels rather than interactive buttons nested in native labels. Explicit `disabled={blocked}` supplements the existing fieldsets;FormData names/options/value callbacks remain unchanged.
- All visible payroll actions,including Preview/Print and payslip Print/Close,meet44px minimum height. The refresh action no longer shifts on hover. The payout register is a named focusable horizontal-scroll region;wide data remains inside the register instead of overflowing the document.
- Before a successful load,KPIs show unavailable markers rather than zero/false empty work. Failed/initial loading does not claim no payouts. Recorded net value is labelled as recorded amounts,not proof of bank settlement. Existing exact totals/currency calculation unchanged.
- Payslip component/content,UTC display,NoCashDue earnings-statement wording,legacy identity/zero-net warnings,precision and print isolation remain intact. Only its action-button CSS changed.

## Verification

- Final webpack build/TypeScript/static export86 PASS. Payroll page ESLint0errors/0warnings. Six pinned vendor hashes unchanged. Scripts parse/scoped whitespace PASS.
- 77 focused node:test entries PASS,including13 new pre-edit working-source preservation checks:11 named save/load/selection/guard/total declarations and whole-file payroll-money/payslip hashes. Existing feedback/reset/pending/unmount/no-cash/null/raw-decimal validation tests retained. No backend calculation,approval,transaction or authorization contract altered.
- Existing precision browser8journeys/16 exact raw writes PASS on final export:Monthly/SessionBlock×320/1440×light/dark;maximum cents,sub-cent no-submit/draft retention,exact profile labels and payout readback. Receipt `QA/EVIDENCE/payroll-precision-browser-1792044297190/receipt.json`.
- New42-case synthetic browser matrix covers320/768/1440×light/dark×normal/empty/loading/denied/pending/rejected/unknown. Normal cases inspect4documents each:Paid,NoCashDue,legacy andmaximum exact amount;native top-layer dialog,amounts/warnings/scroll/Close/Escape/focus return. Desktop print-media checks assert document visible/actions hidden;this is not physical printing or PDF pagination acceptance. Exact pending payout payload/no-repeat/retained error draft/shared selector collapse/year-picker/44px actions/no page overflow/no page errors checked. Final receipt/source hashes recorded after completion below.
- Final new-browser receipt:`QA/EVIDENCE/payroll-visual-1792044349179/result.json`;42/42 PASS,all5 recorded source hashes match final files. Combined with8 retained precision journeys:50 final-export synthetic browser cases. Final phone light page/dark earnings-statement screenshot inspected,including scrolling to accessible action footer. No app-source changes after final build.

## Corrections and limits

Initial new-browser assertion used Playwright `isDisabled()` on the fieldset itself;the test now checks the native fieldset disabled property while retaining actual action/input assertions. Browser then exposed undersized payslip action buttons;local action CSS repaired and final export rebuilt. Intermediate failures remain local,not counted as final acceptance.

This presentation slice deliberately freezes existing payroll save/recovery logic,not a claim that every transport/stale-context/cross-tab case is complete. It does not add durable idempotency or change finance-policy rules. The existing handler's broader unknown-outcome review (including408),captured-draft/reset behavior and current-profile validation remain consequential High follow-up if required before full payroll acceptance;do not interpret preserved code as newly verified for untested cases. Prior real SQL/Identity/API/document evidence is retained,not rerun here. No liveSQL/auth/physical Android/iOS or statutory-payroll acceptance,issue closure or enterprise release closure.

Next High:bounded payroll action-recovery follow-up for the expressly retained408/current-profile/captured-reset limits,then Medium remaining academics/communications and portal-state presentation. Existing UI2/Mini/domain-tool qualification and E9/E10 exact-release gates remain OPEN. Owner sequence unchanged:complete agreed PENTA+Manual app → authorized go-live → Hayansh onboarding → public Academy Desk product website after first customer live.

No backend/schema/Mini/main/staging/commit/push/Azure/production data/container changes;prior dirty work/local evidence preserved. No project switch/new engine handoff required.
