# Local C6 Manual control integration — 6 November 2026

Status: CLAIMED before edits. Local Codex integrates cloud C6implementation
1ec22e733696fa84e4dbf947da463fbe4a2d123a/report2cd20ee2e6beb67a625356b7bed80dbeff17f812.
All eight affected paths are unchanged locally from cloud's83dc3aabase; preserve
newer C5/native session/pinned frontend results and the original dirty trees.
Exact integration paths:
- `QA/HANDOFF/C6_MANUAL_CONTROLS_2026-11-06.md`
- `QA/ISSUES/BUG-API-0005.md`
- `QA/ISSUES/BUG-FUNC-0005.md`
- `QA/tools/finance-governance.test.cjs`
- `QA/tools/manual-controls-browser.cjs`
- `QA/tools/teacher-publication-contract.test.cjs`
- `apps/web/src/app/finance-governance/page.tsx`
- `apps/web/src/app/teacher/page.tsx`

Own report: QA/REPORTS/LOCAL_C6_MANUAL_CONTROLS_2026-11-06.md.
Import reviewed implementation and issue/report updates; retain original issue
history and OPEN status. No backend/domain/SQL/model/lock or shared cloud rewrite.
If needed the Manual browser runner may gain the same strictly loopback API-origin
option as the local PENTA browser runner, preserving all existing tests/fixtures.

Run current TSX handler contracts, isolated pinned build/types/lint and controlled
browser checks using the already-prepared exact-version D: environment. No new
package/model/image download or private audit submission. These checks do not
close the two issues' live HTTP/SQL/device/critical closure requirements. Existing
C5/read-only verification and native session SQL remain source-scoped, not rerun.
