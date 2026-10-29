# Attendance selection/save isolation — 29 October 2026 IST

BUG-DATA-0018 receives a partial attendance repair. A cached session no longer counts as a successful fresh load. Changing/revisiting sessions immediately locks the old editor until a new matching read succeeds; failures provide an explicit retry. Null rows, missing statuses, invalid notes and duplicate student rows cannot unlock entry. Saves belong to one academy/session load generation and active roster member; stale callbacks, late completions/readbacks and A→B→A responses cannot clear newer notes or overwrite notices. Duplicate pending requests and post-unmount writes are guarded. Session-specific unsaved notes remain available when a fresh load succeeds after switching back.

## Observed validation

- Ten new controlled actual-TSX cases:10 FAIL before repair;10 PASS afterward.
- All24 original note payload/clear/null/Unicode/length/selection/duplicate/error/readback cases retained and PASS:34 attendance checks total.
- Two legacy harness assumptions updated:the Save selector now distinguishes the added retry control, and the existing reversed-read case checks that a retired response does not populate its old cache. Its selected-session/no-overwrite expectation is unchanged.
- Full frontend:51files/1,148tests;1,145 PASS/3 FAIL/0 skip/cancel. The same3 historical communication SQL-evidence cases lack their ignored real SQL log locally. No evidence fabricated.
- TypeScript/webpack PASS; changed-page lint0 errors/1 inherited loadWorkspace effect warning; diff check PASS. Prior API1,179 PASS retained, not rerun for UI-only changes.

Separately, `assessment-isolation-browser.cjs` executed8 grouped real Chrome checks at1280x900 and390x844:pending selection hides old data, failed selection/retry, reversed reads, rejected draft retention and confirmed-save/readback failure. Exact synthetic write targets/payloads and unchanged other-assessment data verified; no page errors. CI now runs this after its existing payroll/PDF check and preserves synthetic screenshots/request summaries. This is intercepted synthetic transport, not Identity/SQL acceptance. The earlier controlled assessment and original grading/options checks remain intact.

Reports/evidence: `.build-check/attendance-isolation-before.log`, `attendance-regressions-final.log`, `attendance-frontend-full.log`, `attendance-tsc.log`, `attendance-lint.log`, `attendance-build.log`, `assessment-browser.log`, and ignored `assessment-browser/` screenshots/synthetic write summaries. No customer data or database writes occurred.

All broad issue/server concurrency/real authenticated SQL/device/critical/release gates remain OPEN. No API/schema change, main merge, deployment, environment publication or PentaMini change. Next route:Codex/Sol High inspect the actual expanded role/session/recovery CI run, fix real failures and continue release acceptance.

Final verification after adding4 safe-native-client diagnostic cases:52frontend files/1,152tests;1,149 PASS/3 same missing historical SQL-evidence failures/0 skip/cancel. Attendance product source is unchanged from the34-case/type/build pass above. Native refresh/concurrency plus safe diagnostic suite61 PASS; missing AbortController QA failure reproduced and corrected separately.
