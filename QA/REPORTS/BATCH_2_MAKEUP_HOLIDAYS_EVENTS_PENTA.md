# Batch2 — make-up classes, holidays and events

2026-10-14. `D:\AcademyDesk-codex-p0`, `codex/penta-search`, HEAD remains `b1f8cb6f4209da7fbd14f8089fcb6792438684c4`. Existing E3 visual continuation, not enterprise closure or Azure approval.

## Implemented

- Three pages adopt the existing scoped operations CSS/PENTA semantic tokens: bounded two-column desktop workflow/register, stacked phone/tablet layouts, wrapping content and44px actions. Prior enrolment/attendance/leave styles remain scoped and are regression-checked.
- Make-up intake now has persistent student/batch/teacher/method/delivery/location labels and required shared selectors/date validation. Offline retains optional room; Online/Hybrid retain required meeting link; next-scheduled mode still lets the server inherit schedule/location. Existing defaults, UTC conversion, nullable IDs, request names, outcome messages and pending guards preserved. Shared clock now permits exact minutes rather than restricting choices to15-minute increments. Empty method/delivery/status placeholders cannot become invalid requests.
- Holidays retain required name/date, original custom scope/isClosed/notes payload, existing2026 default action and removal handler. Visible shared date now participates in submit validation and disables with the form. No new year/default policy or deletion semantics introduced.
- Events retain default type/time, optional branch/venue, required title/start/end dates, original payload and browser-local conversion. Dates have required-value validation and explicit pending disable; clocks permit exact minutes. Time labels explicitly say local. Empty type placeholder does not replace the existing type.
- All new native field labels persist after typing; optional/required captions reflect the existing handler/domain contract. Successful resets do not resurrect required warnings. API/backend/schema/permissions/finance/Mini/vendor sources unchanged.

## Verification checkpoint

- Production webpack build/TypeScript/86 exported outputs PASS after final source edits (86 is not accepted page count).
-83 focused checks PASS:70 retained make-up location/outcome, holidays and events handler checks plus13 required-picker/select checks. Updated only import mocks for CSS/label/validation components; existing payload/status/date/location/duplicate/pending/error assertions retained.
- Changed three-page ESLint0 errors/0 warnings. Whole-app lint debt remains separate and open.
- Three expanded final-export suites PASS24 each (72 total):320/768/1440 × light/dark × success/failed-refresh/400/500-committed uncertainty. All retain original exact request/date/null payload and no-repeat guidance checks. Added blank-date no-write, native persistent labels, settled44px actions, no overflow and no stale resolved warning assertions.

| Final suite | Local receipt under `QA/EVIDENCE/` |
| --- | --- |
| Make-up24 | `makeup-feedback-browser-1791998457112/result.json` |
| Holidays24 | `holidays-feedback-browser-1791998457121/result.json` |
| Events24 | `events-feedback-browser-1791998457125/result.json` |

Previous operations54-case regression PASS on the final export: `operations-visual-1791998484709/result.json`; its source hashes match current files. **126 final-export synthetic browser cases total**, separate from83 focused checks. No app-source edits followed the final build. Dark phone make-up/light phone Events screenshots inspected: readable wrapped layout, labelled fields and visible confirmation. Six vendor hashes/scoped whitespace checks PASS. A unit command accidentally ran from `apps/web` instead of repository root and failed file lookup; rerun correctly83/83 PASS, no application defect or weakened assertion. An intermediate lint warning exposed a missing holiday validation component insertion; fixed and rebuilt before accepted browser tests. Earlier evidence preserved.

Final raw SHA256:

| Source | SHA256 |
| --- | --- |
| `apps/web/src/app/makeup/page.tsx` | `eb5149499c8171f53045fe67200d00b1a716ff9725bae3fc05dd9685fa5a8f89` |
| `apps/web/src/app/holidays/page.tsx` | `5b34de2e1f2d98086c879d96a2bba1ba54f2b936fdfd6291172d411e20223616` |
| `apps/web/src/app/events/page.tsx` | `2c922953fd2b368a30749988ede048bdb669cbd38381e2341c39c17549c35961` |
| `apps/web/src/components/design-system/operations-workspace.module.css` | `e767421d4af5d03ae5d438f3a997cac0c9dab7bc717d10237d5fec358fa8bd5d` |

## Important retained limitation

Events currently serializes browser-local time to UTC but renders saved events in `Asia/Kolkata`. Existing test intentionally uses Los Angeles:10:00 input becomes17:00 UTC/22:30 IST on2026-10-31. This visual change preserves that accepted test contract, not a declaration that the cross-timezone UX is production-correct. A focused Sol High timezone-policy/domain review is required before accepting Events for production; do not silently copy make-up IST conversion or reinterpret existing saved timestamps. Make-up's explicit IST conversion remains unchanged. Also retain physical-device, screen-reader, full state/live Identity/SQL and all existing release gates; synthetic APIs do not prove them.

No stage/commit/push/main/Azure/production database/container action. Index empty, prior dirty work/local evidence retained; main unchanged `f7af512f884ca5fe8ac3ddb287c852492592e709`.

Next: stay on the existing E3 per-screen queue (finance/academics/communications and remaining portal states) with Medium for settled visual work; High for the explicit Events timezone review and any consequential domain/security/release task. Actual AI UI2 composition/Mini qualification/governed workflows and E9/E10 exact-candidate production gates remain OPEN; no new engine contract handoff required here.
