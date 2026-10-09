# Calendar agenda readability — 2026-10-09

Worktree `D:\AcademyDesk-codex-p0`, branch `codex/penta-search`, starting HEAD
`b8299718c5d2600142508e509ed6f980e2c8702c`. Bounded BUG-UI-0008 UI repair.

## Diagnosis and scope

Baseline exported-DOM test failed: a 274px agenda had 2453px-wide rows. Implicit
grid min-content sizing and nowrap headings/details expanded rows; ancestor
clipping hid the overflow, despite a passing document-width check.
Evidence retained in `QA/EVIDENCE/calendar-readability-1791564564724`.

Only calendar-scoped CSS changed: zero-minimum grid track/rows, wrapping titles,
details and student chips, and a single detail column at <=480px. Existing theme
tokens retained. No TSX/domain/API/authentication/database/AI changes.

## Validation

- Production webpack export: 83 pages PASS; TypeScript and calendar ESLint PASS.
- New unchanged long-name/date/URL fixtures: 10/10 exported-DOM cases PASS at
  320/375/390/768/1440, light/dark. Inner-card/client-scroll geometry, no ellipsis,
  filters/collapse/reload, GET-only loading and cancelled nonactionability checked.
  Evidence `QA/EVIDENCE/calendar-readability-1791564825807`; 320px screenshot
  reviewed after single-column refinement. Synthetic GET responses, not live SQL.
- Existing controlled timezone/cancellation source tests: 332/332 PASS across four
  zones using the previous accepted SQL receipt; no fresh SQL run claimed.
  Evidence `QA/EVIDENCE/calendar-timezone-1791564717160`.
- Existing 32-case browser matrix: 32/32 PASS against the final export across four
  timezones, mobile/desktop, light/dark and month/year boundaries. Evidence
  `QA/EVIDENCE/calendar-timezone-browser-1791564847873`.

An earlier browser matrix was interrupted by export replacement during the second
build (HTTP navigation failure); retained in
`QA/EVIDENCE/calendar-timezone-browser-1791564741802`. No assertions weakened.
The final matrix runs only after the final build finishes.

Previous 1,130 API tests, 19 HTTP/SQL cases and 16 directly live browser cases
remain prior evidence, NOT rerun for CSS-only work. No container created or removed.
Evidence and unrelated Mini/application work remain local and untouched.
Main, Mini, production databases and Azure untouched. BUG-UI-0008 remains OPEN
for physical-device/broader responsive gates; enterprise/release gates unchanged.

NEXT: Schedule success-feedback BUG-FUNC-0003, Sol Medium for bounded UI;
Sol High if domain/access integration is required. Mini remains at the saved
86/94 CONTRACT READY / ENGINE BLOCKED state; existing qualification handoff controls.
