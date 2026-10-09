# N2y — conversation reading and concise announcements

2026-10-09. Academy Desk worktree `D:\AcademyDesk-codex-p0`, branch `codex/penta-search`, starting HEAD `8b8889c6de5b9b3e7a3c70b24ea81de7efd1bfb5`.

## Scope and result

Reviewed the current governance, design specification and Mini return before continuing. Mini's handoff is unchanged: CONTRACT READY / ENGINE BLOCKED, saved 86/94, active protocol 0.1. No new model/service/contract activation or repeated inference was performed. Continued bounded independent frontend work, not another enterprise audit or a new domain vertical.

The previous chat applied aria-live to the whole result area and used tail.scrollIntoView on every turn/busy update. Its reading area was not a named keyboard-scrollable region; automatic replies could affect ancestor scrolling and completion always focused the composer.

Now:

- One separately mounted atomic polite status announces preparation and a concise reply number/type/displayed-row count. Result cards, view controls and unsent prompt changes are not live-announced wholesale. Existing errors keep their alerts.
- A named focusable transcript has bounded viewport-relative vertical scrolling; outer-page/manual/composer access remains available. Its local scroll follows replies only while the reader is near the bottom.
- Scrolling more than 64px away pauses following. A held reply preserves that reading position, ancestor/page scroll positions and keyboard focus. Completion does not focus the composer while the reader is away.
- Explicit Jump to latest has a 44px target, resumes local following and returns focus to the transcript. New conversation and logout reset reading state. No reading/display control sends a tool or automatically retries.
- Local scrolling is instant, including reduced-motion mode. No new dependency, global ERP CSS, backend/permission/domain/model/tool/history change or reusable-package extraction.

## Validation and retained baseline

| Check | Result |
| --- | --- |
| Expanded runner against the pre-change N2x export | FAIL as expected: no named keyboard-scrollable transcript, 0 versus 1 |
| TypeScript and targeted chat ESLint | PASS, no diagnostics |
| Final webpack production export | PASS, 83 static pages |
| Unchanged response contract tests | 57/57 PASS |
| Existing exported recovery-browser runner | 16/16 PASS |
| Expanded design/read-position browser runner | 6/6 PASS, 320/768/1440px in light/dark |
| Original design assertions | Retained; source identity/order/currencies, table/choice/no-send, help, contrast, manual/empty/error and overflow checks still pass |
| Previous shared-session regression | N2x 79/79 retained, NOT rerun; shared auth helper is unchanged |
| Real Mini/SQL/full API suite, spoken screen-reader, physical Android/iOS | NOT RUN; gates stay open |

The new exported-browser check holds one synthetic HTTP response while the reader uses the keyboard to reach older messages. It snapshots transcript, document and ancestor scroll positions, releases the reply and asserts exact position and active-element preservation. Keyboard Jump reaches the latest reply and focuses the transcript. Typing and table switching leave the concise status unchanged, and the fixture counts exactly six explicitly submitted turns. New conversation clears the announcement and source results. No old test expectation was loosened.

Local baseline failure: `QA/EVIDENCE/penta-design-1791541904244/failed.json` and screenshot. Initial successful UI run: `penta-design-1791541967701/`; final source rerun after explicit polite-status and logout-reset completion: `penta-design-1791542096031/result.json` and `penta-chat-contract-1791542096031/result.json`. Recovery has zero unexpected errors; deliberate 410/409/network logs remain recorded. Browser servers/contexts are owned and closed by the runners. No SQL container was started/removed or production resource touched.

Visual review inspected mobile light and desktop dark reading screenshots from the initial successful export: bounded result area, unobstructed jump/composer, distinct fields and manual rail. Final small source completion did not change that styling; final screenshot sets are retained. Contrast stays 5.38 light / 8.38 dark. These are synthetic browser, DOM and keyboard checks, not actual screen-reader speech, full WCAG conformance or physical mobile-keyboard acceptance.

## Publication and next route

Publish only chat/CSS, the expanded existing browser runner, design specification, this report and narrow N2y handoff/QA/roadmap entries. Preserve and exclude unrelated local backend/provenance/private-spec work and all QA/EVIDENCE. Normal feature commit/push only, not main merge or Azure. Checks run on the preserved working snapshot and do not qualify excluded work.

Sol Medium is appropriate for settled independent frontend work; Sol High for the existing Mini engine qualification handoff and consequential integration. After a qualified engine return, resume the original prepared real SQL/security/source and 15-turn browser acceptance, then the owner-view checkpoint. No current READY TO VIEW, intelligence/90% claim, enterprise/device/privacy/production acceptance or issue closure. No action/history/new-vertical expansion while that prerequisite remains blocked.
