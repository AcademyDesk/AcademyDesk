# Phase 2B — Attendance-note preservation

2026-10-01, Sol High bounded repair of accepted [BUG-DATA-0035](../ISSUES/BUG-DATA-0035.md). Accepted source diagnosis reused, not a repeat Astra audit. Issue/Phase2B remain OPEN for browser/device, linked cases and critical release acceptance.

## Result

The [attendance page](../../apps/web/src/app/attendance/page.tsx) now uses the same effective note for display and submission: the session/student draft when present, otherwise the saved record note, otherwise empty. Untouched status changes and unchanged Save preserve the visible note. Explicit empty edits send optional null, preserving the existing API's intentional-clear contract. Existing empty strings normalize to null; existing API trimming is retained, not changed to byte-exact whitespace preservation.

Drafts and fetched records are scoped to session; switching away/back retains the correct draft without carrying it to another session. Out-of-order reads for one session cannot replace another session's records. Current-session loading/malformed responses do not expose Save actions. A synchronous pending guard prevents repeated same-tick requests; session/status/note/Save controls are disabled during writes. Note length remains the existing 500-character database contract.

Confirmed success is announced through a live status message. Failed writes/network uncertainty retain drafts and do not claim success; confirmed write plus failed readback has distinct guidance and retains the draft. Only the submitted draft is cleared after successful refresh. Blank/non-string server guidance falls back to a visible message.

No attendance API, permissions, enrollment/status policy, schema/migration or CSS changed. Existing active-enrollment requirement and allowed statuses are retained. No broad hook/lifecycle cleanup in this slice; the prior effect lint findings remain open.

## Verification

- [Actual TSX handler tests](../tools/attendance-notes.test.cjs)/[log](../EVIDENCE/logs/phase-2b-attendance-notes-ui.log):24/24 PASS. Ten emitted request scenarios cover status-only/unchanged saves, existing null/empty, explicit clear, edits/trim, Unicode/multiline, 500 characters, first optional and typed attendance. Additional cases cover session/student draft isolation, delayed response isolation, malformed/unloaded read protection, duplicate/pending controls,400/403/500/network/blank guidance, saved-but-readback-failed recovery and live announcement. Controlled hooks/API are not browser or device evidence.
- [Real Identity/HTTP/SQL regression](../tools/SqlHarness/AttendanceNotesRegression.cs)/[log](../EVIDENCE/logs/phase-2b-attendance-notes-sql.log):16/16 PASS. Ten actual emitted TSX payloads are sent to the unchanged attendance endpoint (only the synthetic student ID is mapped to the SQL fixture GUID); response, fresh SQL and GET readback agree. Existing/new records, null/clear/trim/Unicode/500 characters verified; other session remains untouched. Invalid status, no active enrollment, missing session, cross-tenant and anonymous writes reject with400/404/403/401 and identical attendance snapshots. A separate second-session write leaves the first unchanged.
- [Isolated SQL harness build](../EVIDENCE/logs/phase-2b-attendance-notes-sql-build.log):0 warnings/errors. [TypeScript](../EVIDENCE/logs/phase-2b-attendance-notes-typecheck.log):PASS. [Page lint](../EVIDENCE/logs/phase-2b-attendance-notes-lint.log):FAIL,1 pre-existing error/2 dependency warnings, same categories/count as [before repair](../EVIDENCE/logs/phase-2b-attendance-notes-baseline-lint.log).
- The previous426/426 backend suite is historical, **not rerun or counted as new coverage**. Backend source/schema/security and normal development assemblies remain unchanged; this task's API durability evidence is the16-case SQL run.

## Isolation and handoff

One successful owned loopback SQL run `f99df293c85c4ef0b2ce20847c964911`, port57829;81 domain/7 Identity migrations. Runtime inventory unchanged:312 total routes,301 controller method-routes,10 framework Identity method-routes, digest `74CB2F226EBAFF8881F6CF80D6F648310A78B07D79A0A6D9E04D4ED858544FF3`. Database/login/container/root removed after successful ownership checks. No normal dev services restarted, no normal dev database/customer data/Azure/Blob touched, no commit/push/deployment.

[Source/evidence snapshot](PHASE_2B_ATTENDANCE_NOTES_SOURCE_SNAPSHOT.json) and [validator](../tools/validate-attendance-notes.cjs) pin this bounded repair and preserve the accepted preceding backend/security/schema evidence. Remaining browser/mobile/reload interaction, broader lifecycle/concurrency and critical/release gates are not implicitly closed. Next accepted independent repair: [BUG-DATA-0036](../ISSUES/BUG-DATA-0036.md), calendar session-specific teacher/meeting details; use Sol High and preserve explicit fallback behavior without opening real meetings.
