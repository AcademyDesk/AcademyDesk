# Batch2 — Teacher Manual foundation

Date:2026-10-13. Worktree `D:\AcademyDesk-codex-p0`, branch `codex/penta-search`, unchanged HEAD `2e376b0ec47268c9b11d0dda2387620932ce2dd0`. Presentation-only bounded checkpoint, not full Teacher redesign or enterprise acceptance.

## Delivered

Teacher adopts the same scoped Manual shell/semantic-token surfaces used for Platform/Admin. The header now identifies the Teacher workspace and wraps long names instead of clipping them. Profile menu is header-anchored, width/height bounded and scrollable. Cards, fields and actions adopt shared radii,44px target-height foundations and mobile16px field text. Long class-history and payout descriptions wrap; payout actions can wrap below their text on narrow screens.

The old fixed bottom bar has seven sections but a five-column layout; it wrapped and obscured the end of content. Below900px it is replaced with an in-page Workspace disclosure sharing the Platform chooser's visual styles. All seven sections are retained, with current-section label, selection-to-collapse, Escape/focus-return and outside dismissal. Desktop sidebar remains, with explicit current-section semantics/decorative icons. Mobile selection keeps its original `setT` behaviour; desktop Classroom context clearing is unchanged. No teaching/media/payroll handlers, payloads, permissions or financial rules were changed by this slice.

Existing dirty Teacher payroll work, including exact-money formatting/shared payslip preview/loading state, was preserved. Its changes in the overall Git diff are not new changes made for this visual slice. Shared chooser CSS now supports buttons as well as Platform links; existing Platform destinations remain unchanged.

## Verification

- Next16.3.5 production webpack build, TypeScript and83/83-page export PASS.
- **36/36 Teacher synthetic production-export journeys PASS**, `QA/EVIDENCE/teacher-manual-1791908394116/result.json`, with four source hashes.320/390/768/1024/1440×700 plus768×360, light/dark, populated/empty/profile-denied states.
- All seven sections reachable; mobile current label/collapse/Escape focus-return/outside dismissal; header44px targets/full identity wrapping/no horizontal overflow; profile44px/mobile16px fields.25 long history rows and20 payslips retained; last history row hit-testing confirms no navigation overlay, last payout fits. No unexpected page errors or API mutations permitted.
- Profile denial renders its existing guidance without protected forms/history. This is controlled UI-response proof, not real host RBAC/tenant isolation.
- **16/16 existing Teacher publication browser cases PASS**, `QA/EVIDENCE/teacher-publication-browser-1791907960157/result.json`. Original assignment/lesson-plan/assessment payload, nullable scheduling, save/reset and no-error assertions retained. Only open the new mobile disclosure before the existing Homework click.
- **56/56 existing focused Teacher workspace/recording/private-media client tests PASS** (`teacher-workspace.test.cjs`, `teacher-recording.test.cjs`, `class-media-client.test.cjs`). No recorder/media behaviour change or physical device proof claimed.
- **24/24 existing Platform navigation/header journeys PASS on the same export**, `QA/EVIDENCE/platform-navigation-1791908444977/result.json`; original264 transitions/history/scroll/collapse/viewport/contrast expectations retained after shared chooser CSS gained button support.
- New `manual-section-navigation.tsx` lint0 errors/0 warnings. Teacher page current lint **5 errors/3 warnings**, independently compared with HEAD **4 errors/3 warnings**. The additional finding is the already-present dirty payroll `setViewing(null)` effect, outside this visual slice. Other existing effect findings remain; no clean page-lint claim, suppression or unrelated payroll refactor.
- New browser runner, publication runner and linked payroll-document runner syntax checked; Git diff whitespace check PASS. Payroll-document runner now opens the disclosure before mobile Payslip selection on both entry and reload; all existing financial/document assertions retained. Its real SQL/browser/PDF integration was **not rerun** for this presentation-only slice; earlier receipts remain historical, not fresh final acceptance.
- Two final320px screenshots visually inspected: light Today/full long identity, dark end of20-payslip list. Both show bounded wrapping and bottom-content clearance. Physical Android/iOS/Safari, spoken screen readers, all profile-menu interactions, every overlay/draft/state combination and real SQL/role/device acceptance NOT RUN.

First new runner failed at a nonexistent accessible name `Select class / batch`; `QA/EVIDENCE/teacher-manual-1791907948490/failed.json`/screenshot retained. Source uses the existing accessible name `Class`; corrected selector retains the original long-list/hit-testing assertions. The failure is not claimed to be an application defect.

## Remaining / next route

Batch2 remains IN PROGRESS. Next **Sol Medium: Student Manual foundation**, then finish existing per-screen/shared-component/state migration for all four portals. These foundations are not whole-portal visual acceptance. Confirm actual versioned PENTA token/component delivery and Orbit+Aura dark/Prism light parity; retain Manual ERP workflows and independent availability, not a chat canvas.

Inherited lint, real host/domain/role/tenant tests, full draft/overlay/state continuity and physical-device acceptance remain OPEN in the existing queue. High only for consequential authorization/session/domain/cross-project contracts. Existing enterprise testing and Batch1 release gates are retained; no issue closure, new testing plan, Mini-project edit, API/schema change, container action, staging, commit, push, main merge or Azure deployment.
