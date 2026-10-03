# Phase 2B — correct Teacher sign-in and profile-denial guidance

2026-10-01. **DRAFT-ISOLATION-UX-001 locally fixed and bounded retest PASS.** Signed-out/expired authentication is no longer described as a missing teacher profile. No authorization/storage policy change, commit, push, Azure access/deployment or development/Azure database writes. Phase 2B/media/security/financial/critical/device gates remain open. Continue agreed Sol High allocation; no repeat accepted Astra audit.

## Change and executed checks

Teacher page preserves the final real /api/teacher/me response status after the normal shared refresh attempt. New teacher-workspace helper maps 401 to sign-in guidance with a fixed /login link; 403 to linked-profile/Academy Admin guidance; other HTTP, network, parsing and downstream roster errors to a temporary-load message. No token clearing, silent draft deletion, bypass or automatic role changes. Pre-existing Teacher page edits preserved.

| Case | Actual evidence |
| --- | --- |
| Anonymous /teacher, backend ready | Real teacher/auth 401; “Please sign in to open your Teacher workspace. Your session may have expired.” Sign in link visible, old unlinked-profile message absent |
| Sign in action | Actual link click opens /login and its normal fields |
| Authenticated Teacher role without domain TeacherId | Real login/session 200, /teacher/me 403; “This account is not linked to an active teacher profile. Please contact your Academy Admin.” Sign-in-expiry message absent |
| Normal linked Teacher A | Real /teacher/me/roster/attendance 200; normal Welcome, Synthetic / assigned timetable loads, no denial message |
| Before backend ready | Initial local navigation displayed temporary-load guidance, not profile denial; reload after READY established the 401 case. This is startup sequencing, not injected 500 or broad outage certification |

Server 404/429/500, transport exceptions and invalid-response cases use actual helper with explicit errors in unit tests, not runtime HTTP reproduction. Real expired-token/refresh-failure flow, suspended academy and wider role states NOT RUN. Ordinary anonymous 401 alone does not prove all expiry/security cases. Announcements/notification access replies in unlinked fixture are preserved in HTTP log, not interpreted as data-access certification.

Frontend lifecycle log retains a Fast Refresh full-reload warning. Exhaustive console cleanliness is not certified by this bounded check.

## Tests and reproducibility

**32/32 targeted tests PASS**: 28 existing media/client/SSR checks plus four new classification/wiring checks. Frontend `tsc --noEmit --incremental false` PASS. QA harness build 0 warnings / 0 errors. Targeted lint remains FAIL: Teacher page 4 errors / 3 warnings, identical rule/severity counts in SHA-verified pre-change source copy; only line numbers shifted. New helper 0 errors / 0 warnings. No clean full-lint/build/critical/regression certification claimed.

Fresh synthetic run `ddc5138955944578a2420800d6bb84dc`, UTC start `2026-10-01T04:28:01.9065923Z`, SQL loopback port 52439, real Program/Identity/SQL/private Azurite bridge 49002, source-only Next frontend 49001 at `.build-check/browser-web-6f3bef8742b14281bce748f8d232a264`. Added only an isolated active Teacher-role user with no TeacherId to existing QA fixture. No fake auth/responses or browser session-store inspection. Normal development app/database not replaced. Startup tenant controls PASS; runtime inventory 307 routes / 296 controller method/routes / 10 framework Identity entries; migration histories 80 / 7.

[56-record source snapshot](PHASE_2B_TEACHER_SIGNIN_UX_SOURCE_SNAPSHOT.json) compares previous 54: Teacher page and QA BrowserMediaHost changed; new product helper and QA tests added. Other captured product/API-test/config hashes unchanged. HEAD remains `20bb6047f9edf733ac8e2a226621cc582ec54b3c`. Earlier media browser/security/SQL tests remain historical, not rerun on this changed Teacher page.

[HTTP/cleanup](../EVIDENCE/logs/phase-2b-teacher-signin-http.log), [32 tests/build](../EVIDENCE/logs/phase-2b-teacher-signin-client.log), [verified baseline/final lint comparison](../EVIDENCE/logs/phase-2b-teacher-signin-lint.log), [typecheck](../EVIDENCE/logs/phase-2b-teacher-signin-typecheck.log), [actual DOM excerpts](../EVIDENCE/logs/phase-2b-teacher-signin-dom.log), [frontend lifecycle](../EVIDENCE/logs/phase-2b-teacher-signin-frontend.log), [integrity/cleanup validation](../EVIDENCE/logs/phase-2b-teacher-signin-validation.log), [sign-in guidance screenshot](../EVIDENCE/screenshots/phase-2b-teacher-signin-guidance.png). Screenshot is a desktop functional sample, not full mobile/visual acceptance.

## Cleanup and next

Normal synthetic account sign-out verified, owned tab closed. Exact owned stop marker: host exit 0 / 237.4 seconds; private container, disposable SQL database/login, run folders and labelled SQL/Azurite containers removed. No upload sessions created (final fresh SQL []). Current owned root/containers/listeners absent. Matched Next child 18224 / parent 15380 intentionally stopped; launcher exit 1 records termination, not application failure. Disposable synthetic data not recoverable; source copy/evidence retained. No customer file removed. Older interrupted `d6dfd31d64c146789abbf686bccb920a` stopped containers remain; refused cleanup not retried.

Next bounded task: Teacher audio-recording start/stop and microphone-denial recovery. Continue agreed **Sol High**. Actual-device/2 GB/full critical/release and four financial P0/policy gates still pending; no phase or media issue closure.
