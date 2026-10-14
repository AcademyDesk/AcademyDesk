# Batch2 — shared Manual controls and popup parity

Date:2026-10-14. Worktree `D:\AcademyDesk-codex-p0`, branch `codex/penta-search`, HEAD unchanged `2e376b0ec47268c9b11d0dda2387620932ce2dd0`. Owner requested continuing the frontend/popup consistency work after the PENTA foundation integration. This is a bounded shared-control slice, not whole-frontend acceptance.

## Implemented

- New host-owned `AnchoredPopup` uses the browser's native popover top layer while retaining the actual Manual/PENTA DOM ancestry and scoped tokens. No `body` portal, arbitrarily large z-index or CSS coordinate workaround. Bounded trigger-relative placement follows captured scroll, resize, visualViewport changes and element resize; flips above when appropriate, closes if the trigger leaves the viewport. Listboxes cap at420px; dialogs can use520px where available; short viewports scroll internally.
- Existing StandardSelectField, StandardDateField and StandardTimeField consume it. Existing option sets, hidden field names/values, optional dates, direct month/year selection, date-only leap-day and 12/24-hour conversion/interval semantics preserved. No payload/domain/auth/approval change. Escape and outside dismissal, select selection/focus return, arrow/Home/End listbox navigation, wrapped selected names and an explicit styled time Done action.
- Shared date/month/year layout no longer clips the year at320px. Time options use readable individual columns rather than tiny nested grids. Options/time/navigation/footer/close controls have44px minimum heights; calendar days intentionally remain38px minimum height in seven columns, not universal44px device acceptance.
- Shared detail modal consumes PENTA surface/border/radius/shadow tokens and refined header/backdrop; existing native dialog/inert background/focus/scroll behavior unchanged. Manual-scoped shared panel/header/action/table-wrap defaults align existing forms with the pinned foundation.45 app source files reference one or more shared select/date/time controls; this is a source-use count, not45 tested routes.
- No edits to the six immutable PENTA vendor sources, dependency installation, npm lockfile, Next config, model runtime or backend. Existing unrelated dirty application/QA work retained.

## Validation and limitations

Final `npm run build -- --webpack` passed: production compilation, TypeScript and all83 exported pages. No application source changed after this build. Intermediate checks caught a legacy36/35px select override, selected-name overflow, cramped year, and a32px Done button; repaired instead of reducing assertions. Failed/intermediate evidence remains local. An initial default Turbopack build refused the existing out-of-root node_modules junction; established webpack build succeeds without config/install changes. One lint command from the repository root had no config; rerun from apps/web succeeds for popup/select/time.

Final-export receipts:

- **36/36 shared-control cases PASS**, `QA/EVIDENCE/manual-controls-1791964889995/result.json`. Student fees, Teacher payments and make-up classes;320/390/768/1024/1440x700 plus768x360, both themes. Native top-layer/theme ancestry, viewport bounds,46 long options,44px option/time targets, full selected-name wrapping, keyboard End/selection/Escape/focus/outside/resize/available actual-scroll anchoring, readable month/year,2000-02-29 exact date-only value,23:45 exact time and Done/focus, reachable44px calendar footer/no horizontal overflow/no mutations or unexpected page errors. Receipt pins source hashes. Six final phone calendar images and two time images retained; light Student-fee date and dark time screenshots inspected after final height/footer refinement. Actual scrolling is checked when the page/ancestor can move; not a universal pinch/soft-keyboard/device claim.
- **32/32 onboarding cases PASS**, `QA/EVIDENCE/onboarding-dates-browser-1791964890039/result.json`: Student/Teacher, blank/valid optional dates, successful/error synthetic responses,320/1440px, both themes.
- **32/32 modal cases PASS**, `QA/EVIDENCE/manual-detail-modal-1791964889960/result.json`: Student/Teacher overview, empty/40 long records,320/390/768/1440px, both themes; native inert background/focus/scroll restoration/Escape/backdrop dismissal,44px contrast-checked close and high-z-index sibling probe. Final320px dark Student image visually inspected.
- **8/8 payroll precision journeys PASS**, `QA/EVIDENCE/payroll-precision-browser-1791964981650/receipt.json`:16 exact intercepted writes, maximum/one-cent profile labels, native sub-cent no-submit and draft retention. No finance arithmetic change.
- **24/24 Admin foundation regression cases PASS**, `QA/EVIDENCE/admin-manual-1791964979265/result.json`: Core/Full module fixtures across five widths plus short landscape/both themes, existing navigation and actual primary-action contrast checks retained. This does not accept every Admin page.
- **9/9 focused onboarding handler tests PASS** via `node --test QA/tools/onboarding-dates.test.cjs`; six pinned vendor hashes, runner syntax and scoped diff/high-confidence credential-pattern checks PASS. The latter is not a whole-repository/history secret audit.

The earlier controls run `manual-controls-1791964728338` finished while later refinement/build work was underway; it is intermediate evidence, not the final source receipt. Earlier onboarding/payroll/modal/Admin passes are also retained, not substituted for final receipts above.

All browser suites serve the actual production export through a loopback static host with synthetic read fixtures. No real API/SQL/tenant/role/security, device/Safari/screen-reader or AI inference acceptance is implied. The controls suite forbids mutations and unexpected page errors; form suites deliberately intercept synthetic writes to verify payload/feedback retention. No live academy data was changed.

Existing date-field lint debt (two errors/one warning) was independently compared to HEAD and is unchanged. New popup/select/time scoped lint passes. Existing whole-app/page lint debt remains OPEN. Full enterprise/API/security/runtime/SQL/physical/operational gates are not repeated or closed by presentation tests. The nested-popup-inside-modal Escape safeguard is implemented but not separately accepted by an actual nested workflow in this slice.

## Next / boundaries

Continue Batch2 per-screen Manual adoption: Admin directories/onboarding, batch/schedule and remaining finance/forms/tables/actions, then remaining Platform/Teacher/Student screen states, using existing inventory. Full visual parity remains unchecked; the previous foundation/checkpoint passes are not whole-portal acceptance. AI UI2 authenticated composition remains separate; do not import prototype demo history/actions or activate locked models/tools.

Use the settled Manual styling route (Sol Medium); consequential host/context/security work stays Sol High. No PENTA engine handoff is required for these host-owned control changes; genuine shared-contract gaps must use the established handoff. Main/Mini unchanged; no stage, commit, push, Azure, credentials, containers, migrations or evidence deletion.

Local preview probing this turn found frontend3000/API5092 unavailable; prior handoff's running-preview claim is not current. No runtime restart or schema upgrade was attempted during this bounded UI verification. Tests use their own disposable loopback servers, not the normal development API.
