# Batch2 — class/batch, calendar and schedule visual slice

2026-10-14. `D:\AcademyDesk-codex-p0`, `codex/penta-search`, HEAD remains `b1f8cb6f4209da7fbd14f8089fcb6792438684c4`. Continues E3/four delivery groups; not a new audit. **Bounded visual checkpoint PASS, not whole-screen/enterprise/release acceptance.** No staging/commit/push/Azure.

## Delivered

- Batch overview/setup consume scoped `scheduling-workspace.module.css`: consistent PENTA-token surfaces, responsive one-column workflow/directory, two-column desktop intake, wrapping identities/status/actions,44px directory actions and management links. Existing setup fields and inline-edit controls now have persistent labels; required/optional captions match existing native requirements. Original requests/capacity/configuration/lifecycle rules retained.
- `/schedule` uses the shared labelled dropdown for batch, teacher, branch, delivery mode and per-session status. Batch-default application, deliberate overrides, disabled pending states, existing no-batch handler guard, IST→UTC conversion and sensitive outcome messages are unchanged. Empty status-placeholder selection is ignored rather than sent as a status. Date-time inputs have persistent visible labels, and Online/Hybrid still require the existing meeting-link value.
- Schedule gets a semantic title header, grouped create/register surfaces and wrapped session identities/details. Removing its decorative title paragraph initially exposed an old global `h1 + p { display:none }` rule that hid authoritative status messages. Actual POST/readback and state were correct; the status paragraph was CSS-hidden. The new title header puts live status outside that selector. No domain/save algorithm was changed to mask this rendering bug.
- Calendar month is a named, keyboard-focusable horizontal-scroll region. Its bounded720px minimum grid remains readable on phones while the existing full-detail agenda stays available below. Navigation/filter/disclosure/join actions use44px targets, wrapping header controls, persistent IST/scroll guidance and semantic load status. Month/agenda projection, external-link safety, cancelled-class handling and collapse/expand are preserved.
- Shared PENTA vendor source, API, SQL, Mini, authority and business calculations untouched. Host CSS module only, not a global redesign or imported demo.

## Final verification

Final application source unchanged after the successful export.

| Check | Result / local receipt |
| --- | --- |
| `npm run build -- --webpack` | PASS: compilation/TypeScript/86 exported outputs, not86 accepted screens |
| Six focused unit suites | PASS188: batch preservation/times/feedback, schedule defaults/feedback and calendar details |
| `node QA/tools/batch-calendar-visual-browser.cjs` | PASS60; `QA/EVIDENCE/batch-calendar-visual-1791991152564/result.json`; current source hashes included |
| `node QA/tools/schedule-feedback-browser.cjs` | PASS12; `QA/EVIDENCE/schedule-feedback-browser-1791991142203/result.json` |
| `node QA/tools/calendar-readability-browser.cjs` | PASS10; `QA/EVIDENCE/calendar-readability-1791991163119/result.json` |
| Shared vendor integrity/script parsing/scoped whitespace | PASS6 normalized hashes; scripts parse; no whitespace errors (repository LF→CRLF warnings only) |

**82 final-export synthetic browser cases**, separate from188 unit checks. New60 covers320/768/1440 × light/dark:36 Offline/Online/Hybrid batch create/rejection journeys (selected day09:37/request precision, required meeting links, blank optional dates, reset/error retention, inline edit/deactivate preservation of Hybrid link/meeting days/closed enrollment/waitlist),12 overview normal/empty/modal cases and12 calendar empty/unavailable/keyboard-scroll/disclosure cases. Batch overview native details modal still closes on Escape. Action44px assertions kept.

Existing12 schedule cases cover320/1440 × light/dark × normal/refresh-failure/400: create plus status mutation, exact IST→UTC date payloads, nullable optional IDs, retained rejected dates, persistent success, explicit saved-but-refresh-failed warning, one POST/PUT, no JS/hydration errors and viewport width. Existing10 calendar long-content cases cover320/375/390/768/1440 × both themes, non-IST browser timezone, midnight IST boundary, long unbroken identity/subject/meeting details, filtering/collapse/expand/reload, one data request per endpoint, and non-clickable cancelled classes. The script assertions were not reduced.

Final screenshots reviewed: dark batch phone intake/actions, light phone horizontally scrolled month region, dark phone schedule with **visible** Cancelled confirmation. The horizontal month screenshot deliberately shows its far-right columns after the test scroll, not an incorrect default weekday. Fixtures contain only synthetic data. Full-page screenshots do not expand internally scrolling desktop workspaces. Real authentication/API/SQL/device/screen-reader/tenant/financial/release acceptance is NOT provided by this test transport.

## Lint / diagnosis / retained failures

- Scoped ESLint is NOT clean:1 pre-existing batch `react-hooks/set-state-in-effect` error and4 warnings (batch2 effect dependencies + unused destructured value; schedule1 effect dependency). Compared all3 source files against `git show HEAD` using ESLint API; same diagnostic rule/count, no new diagnostics. Calendar is clean. No suppressions or unrelated effect refactor.
- Unit fixtures now mock CSS/native label wrapper/shared select imports and invoke the actual new string callback shape. The stale-handler pending tests still capture/reinvoke stale callbacks; request/default/precision/feedback assertions remain intact.
- Schedule browser fixture retains the original input `aria-label="Room"`; its first locator expectation was corrected to the actual accessible name. Native form validity now asserted before submission, and failed journeys retain synthetic diagnostics/screenshots.
- The hidden schedule confirmation initially failed several browser runs. A single explicitly diagnostic-only intercepted-bundle trace confirmed both save/refresh received the expected success string; this was NOT accepted evidence. Inspection located the legacy CSS sibling rule. Removed tracing from the runner; final12 uses the uninstrumented final production export and unchanged visible-success assertions. No temporary application debug code was introduced. Old failures/evidence kept.
- Earlier60/10 passes preceded the header repair; both suites rerun on the final unchanged export. Do not replace those old receipts or claim an unrelated whole-suite rerun.

## Remaining / next

- This does **not** finish all date/time control parity: batch selected-day inputs remain native required `time`; schedule date-times remain native required `datetime-local`. Their exact-minute/native validation and IST payload contracts are intentionally retained until a shared required date/time replacement is implemented/tested. Existing batch start-date uses the shared year-capable date control.
- Legacy `/schedule` mobile chooser still says Browse workspace rather than a current-section label; route/navigation parity remains to finish. Other batch/calendar/modal/conditional states, large real schedules, physical gestures/screen readers, pending focus/live permission changes and live SQL remain OPEN. The all-page ledger stays unchecked at full-acceptance level.
- Next bounded E3 work: finish shared required date/time and schedule navigation parity, then enrolment/attendance/leave/remaining operational screens. Keep existing 90% eligible conversational AI/10% Manual target, UI2 host composition/qualified Mini/domain tools and E4–E10 gates; no new PENTA contract handoff required for this visual slice.
- Sol Medium for settled visual migration; Sol High when required-field contracts, role/context, domain/security/finance/integration become consequential. No API/schema/credentials/production DB/Mini/vendor/main changes or deployment. Earlier dirty work/evidence preserved.
