# Batch2 — Admin directories and profile editors

2026-10-14. Worktree `D:\AcademyDesk-codex-p0`, branch `codex/penta-search`, HEAD unchanged `b1f8cb6f4209da7fbd14f8089fcb6792438684c4`. E3 continuation, not a new enterprise audit. **Bounded local UI checkpoint PASS; whole-page/portal/production acceptance remains OPEN.** No commit/push/Azure.

## Changes

- `/students`: desktop table becomes labelled stacked phone records. Native table/caption/header semantics retained with explicit table/rowgroup/row/cell roles for the responsive presentation; physical assistive-technology acceptance is still needed. Long identity/contact values wrap; actions remain readable and44px high. Assignment is a full-width, persistently labelled panel. Search has a meaningful accessible name. Active/Inactive filter buttons now use correct `aria-pressed` and selected visuals rather than unsupported button `aria-selected`.
- `/teachers`: grouped assignment controls, wrapping record identity/actions, persistently labelled inline edits and semantic status feedback. Exact update/enrollment/batch payloads and availability/role/financial rules remain unchanged.
- `/student-profile` and `/teacher-profile`: scoped PENTA section/grid/toolbar/editor treatment; persistent native field, notes and record-selector labels. Empty-profile registers now explicitly say no records rather than showing an unexplained blank area. Student Management aliases the student profile implementation; its route compiles but was not separately exercised in this new browser matrix.
- Teacher profile availability uses the existing shared anchored clock, preserving blank/full-minute values and disabled unchecked-day controls. Added optional `disabled` to `StandardTimeField`; no caller loses the previous default behavior. No native OS time popup remains on this profile.
- Teacher profile now checks HTTP success and array shape before using academy/teacher lookup responses. Previously a403/503 error object could misleadingly become “create an academy.” Failed responses now show a load failure. This is defensive display handling, not authoritative authorization or a broader security repair.
- Browser tests uncovered disappearing teacher success messages: the existing reload cleared them immediately. Teacher update/status/assignment now confirm after reload; a committed save followed by a failed directory refresh explicitly reports both facts and says not to repeat the save. Domain calls and calculations unchanged; failed writes are not labelled successful.
- Shared vendor snapshot untouched; new `records-workspace.module.css` is host-scoped and reuses PENTA-mapped semantic tokens. Existing native-field wrapper reused. No application backend/schema/Mini/permissions changed.

## Verification

Final application sources were not edited after the final successful build.

| Check | Result |
| --- | --- |
| `npm run build -- --webpack` | PASS: compilation, TypeScript,86 exported outputs, not86 accepted pages |
| Focused unit suites | PASS63: batch-preservation16, student-profile-feedback18, onboarding-dates9, workspace-entry20 |
| `node QA/tools/admin-records-browser.cjs` | PASS100, final `QA/EVIDENCE/admin-records-1791989317468/result.json`; current-source hashes included |
| `node QA/tools/student-profile-feedback-browser.cjs` | PASS44 on final export, `QA/EVIDENCE/student-profile-feedback-browser-1791989327912/result.json` |
| Shared time/onboarding state regression | PASS44, `QA/EVIDENCE/onboarding-states-1791989178945/result.json`; run after the time/profile CSS changes, before final directory-only feedback repair. Relevant source hashes rechecked unchanged, not claimed a fresh final-export run |
| Other shared control regression | PASS36 earlier in this turn, `QA/EVIDENCE/manual-controls-1791988797691/result.json`; before final page-only fixes, retained rather than relabelled final |
| Vendor integrity / scripts / scoped Git whitespace | PASS6 vendor hashes; new script parses; no whitespace errors (Git native LF→CRLF warnings only) |

**144 final-export synthetic browser cases**, separate from63 unit checks and the explicitly retained44/36 regressions. Final100 comprises320/768/1440px × both themes × four routes × ready/empty/403/503, plus4 teacher confirmed-save/failed-refresh cases. Ready journeys check search/Active/Inactive, profile links, assignment selectors closing, student enrollment payload, teacher edit/deactivate/reactivate/assign confirmations and preserved Hybrid meeting configuration, profile saves/date nulls, full-minute time readback/clear and unchecked-day disabled state, native detail dialogs/Escape, field labels, long records and no horizontal page/workspace overflow. Final44 retains existing student draft/pending/double-submit/scope-switch/error/network/malformed-success tests, including stale-load/write isolation, with original assertions intact.

ESLint is **not fully clean**:1 inherited `react-hooks/set-state-in-effect` error in students and2 inherited `react-hooks/exhaustive-deps` warnings in teachers/teacher-profile remain. Compared current six files with `git show HEAD:<path>` via ESLint API: no new diagnostic rule/count;2 old invalid-ARIA warnings removed. Student profile, native editor and shared time have0 diagnostics. Do not report a lint pass or silently disable rules. Broader effect/loading refactors remain separate from this bounded styling slice.

Final screenshots reviewed: Prism student phone cards/actions, Orbit teacher directory/actions, Prism teacher profile/availability; earlier screenshots retained too. Synthetic names/contacts only. Full-page captures do not expand internally scrolling desktop containers; assertions, not one image, establish the recorded bounds. Tests use isolated loopback export plus intercepted synthetic APIs, not live accounts/SQL/AI/financial acceptance or physical iOS/Android/screen-reader proof.

## Failure-driven repairs retained

- Native-field unit fixture needed an explicit wrapper import mock; no existing behavior assertions weakened.
- First matrix exposed Teacher360 error-response handling; repaired the actual page, not the expected state.
- Screenshot review caught a desktop30% action-cell width overriding mobile width. Scoped mobile last-cell width repaired; added legibility bounds.
- Stronger control assertion caught old compact-link specificity leaving Open record at34px. It now uses the standard action class; assignment44px also explicitly retained.
- Teacher success disappeared during reload and caused timing-dependent failures. Fixed actual post-save feedback and added4 refresh-failure cases plus edit/status/assignment journeys. Teacher response fixture now matches the existing server profile projection (`availability` derived from posted JSON), not an incorrectly echoed request shape.
- Initial baseline-lint comparison had a Windows path separator mistake; corrected read-only comparison. Earlier failures/evidence remain local, not deleted.

## Remaining work / next

This is not full profile/directory acceptance: embedded student fee-arrangement forms and linked finances, every modal/conditional state, teacher profile concurrent/stale request behavior, larger registers, physical devices/screen readers and real SQL/auth/tenant-linked mutation gates still require their existing tests. Existing unknown-outcome/security/finance/enterprise gates are not closed by these visuals.

Next E3 bounded group: classes/batches/calendar/schedule surfaces, forms, responsive actions and bespoke dialogs. Keep90% eligible AI /10% Manual target, real UI2 composition, qualified Mini and each governed conversational tool on the existing E4–E10 path. No new PENTA contract dependency in this slice; existing handoff remains, no automatic project switch. Sol Medium for settled visuals; Sol High for consequential authority/context/finance/integration decisions. Other dirty work, local evidence and main preserved; nothing staged/committed/pushed/deployed.
