# Student360 administrative-profile save feedback

2026-10-10; worktree `D:\AcademyDesk-codex-p0`, branch `codex/penta-search`, starting HEAD `5d415eedcd9fda78b633d6778fe73cc23890f076`.

## Bounded scope

Administrative editor preserves all13 fields, optional string defaults, blank date nulls and exact scoped PUT. Existing domain validation, duplicate student-number409, role/tenant authority and backend transactions unchanged. No API/database/Mini/model-contract changes.

- Keyed academy/student editor resets only on selection change; parent refresh does not erase the same-record draft.
- Durable polite save status; HTTP rejection retains draft, network/5xx is unconfirmed and advises checking before retrying. No automatic retry or extra readback/write.
- Successful HTTP response with malformed profile JSON or failed parent display is explicitly a saved change/display failure, not a failed write inviting repetition. Callback requires a non-array object containing all13 profile keys with string/null values.
- Synchronous save guard, disabled native fieldset (including both note fields), disabled custom gender and remounted date controls; stale field callbacks ignored while pending.
- Parent checks workspace/profile reads, clears the old profile immediately on selection, ignores out-of-order reads and invalidates reads on unmount. Query selection also keys the workspace. This fixes a UI record-handover race, not an authorization change.

## Verification

- 18 actual-TSX controlled tests PASS: exact payload/dates, optional nulls,400/403/409/500/503/network, malformed JSON/shapes/types and callback failure, same-tick duplicate/field guard, keyed scope, late unmount and same-record draft retention.
- 44 synthetic exported-application browser cases PASS:320/1440px × light/dark × success,blank,400,403,409,500,network,malformed,scope-write,scope-load,initial-failure. Held writes confirm fields/notes locked and only one PUT. Selected record clears while held read loads; a subsequent third-student draft survives the older read. New student draft survives the previous student's late write. No page exceptions.
- Target eslint0 errors/0 warnings, TypeScript validation PASS and production webpack export83/83 PASS.
- 12 responsive screenshots captured; mobile light success and dark switched-student screenshots visually reviewed.

Evidence intentionally local: `QA/EVIDENCE/student-profile-feedback-browser-1791650017707/result.json`. Initial browser evidence `student-profile-feedback-browser-1791649979220/failed.json`: locator waited for old Save label after it changed to Saving; corrected to stable header button, retaining assertions. First controlled run11 PASS/7 FAIL because a void click callback was incorrectly awaited; test now waits for the async handler's microtasks, with all18 assertions retained. Review also moved the closing fieldset below internal notes so both note fields are locked.

## Open gates and continuation

Controlled hooks and synthetic browser transport do not prove real Identity/HTTP/SQL, authorization, tenant isolation, physical Android/iOS or full critical workflow acceptance. Existing enterprise testing continues; BUG-FUNC-0003 stays OPEN and original28-route count unchanged. Prior admission/subject-fee/enrollment results retained, not rerun or replaced. No SQL container, production data/credentials or Azure touched.

Next: reconcile and execute the existing remaining live acceptance queue alongside pending versioned FW1/shared Manual visual delivery. Sol High for privacy, authority, SQL/domain integration and release decisions; Sol Medium for bounded UI. Mini qualification and90% conversational coverage remain targets, not accepted delivery. No new testing plan or Mini handoff needed for this host-only UI change. Scoped feature publication only; unrelated Mini/32+28 continuity/evidence preserved, no main edit/merge/push/deploy.
