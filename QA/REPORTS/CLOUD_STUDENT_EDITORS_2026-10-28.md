# Student administrative profiles and subject-fee isolation — 2026-10-28

MODEL: Session Codex model; no override or delegation.
TOOL: Codex cloud terminal.
REPOSITORY: AcademyDesk/AcademyDesk.
BRANCH: codex/release-export-security; base bf109723383c481b1003028516f6690f21363df1.
ENVIRONMENT: Managed Linux; controlled TSX and synthetic responses, no SQL/browser.
TASK: Continue BUG-DATA-0018 for student administrative profiles and subject fees.
MAY MODIFY: Student profile page/component, subject-fee component, editor helper,
and focused QA records/tests.
MUST NOT MODIFY: Financial arithmetic, API/RBAC/schema, customer records/deployment.
VALIDATE: Selection/load/save/readback races and exact payloads; frontend checks.
STOP WHEN: Bounded repairs validated and pushed with limitations preserved.
NEXT ROUTE: Codex for remaining tracked data-integrity and real HTTP/SQL gates.

## Reproduction and repairs

The initial 19 controlled actual-TSX cases had 18 failures/one pass. The student
page retained the previous profile during a pending or rejected selection and
accepted old child-save callbacks. The administrative component allowed unloaded
and duplicate saves and accepted completions after its source changed. Subject
fee drafts/lists remained under a new student's ID; a late successful POST could
clear the new student's input and replace their list. Rejected/transport saves
and failed post-save refreshes lacked complete recovery.

The student page now uses the URL/load-generation helper from the previous
checkpoint; its child-save callback accepts only its original generation. Both
student routes share this implementation (`student-management` remains an alias).
Its administrative and fee children are keyed by academy/student. Detail-load
failure locks the editor with an explicit retry action.

The student administrative editor separately scopes pending completions to its
academy/student/profile source and ignores unmounted completions. Missing profiles
cannot save; pending writes are guarded and controls disabled. Server validation
keeps the draft. A confirmed write with an unreadable body retains success and
asks for reload, without invoking the parent with null. Own-save success survives
the parent's new profile props.

Subject fees now reset drafts, frequency, list and notices before a changed
academy/student commits. Reads, POSTs and post-save reloads are lifecycle-bound.
Failed/pending initial loads cannot add; failed reads offer retry. Duplicate
pending POSTs are guarded. A confirmed POST stays successful if the separate list
reload fails, with refresh guidance. Transport failures preserve input and release
busy state. Exact target/amount/frequency/date payload behavior is retained.

## Validation

- All 34 expanded focused cases PASS, zero failed/skipped/cancelled. Covers the
  original 19 plus parent/child success, unmounts, old post-save reloads, rejected
  old writes, overlapping busy states, academy change, exact payloads, malformed
  lists and A/B/A load generations.
- Full frontend: 46 current files; 1,047 tests, 1,044 PASS / three FAIL,
  zero skipped/cancelled. Runner exit 1 is retained; only the three historical
  communication-drafts assertions fail for the missing original SQL log.
- TypeScript PASS. Targeted and full lint PASS (zero errors/63 warnings).
  Production webpack build and diff checks PASS. No dependency/backend changes;
  previous API 1,073-pass/audit results retained, not rerun.
- Ignored local logs: .build-check/student-editor-before.log,
  student-editor-final.log, student-editor-full-frontend.log,
  student-editor-lint.log, student-editor-build.log and frontend-tests/summary.json.

BUG-DATA-0018 stays OPEN for remaining tenant/attendance/assessment and other
editors, browser/device, actual HTTP/SQL persistence, same-record server races,
idempotency and critical regression. These controlled checks do not establish
exactly-once billing or SQL semantics. No real financial/customer action occurred.
Runtime CDN requirements remain unapplied; no environment change/publication,
main merge, SQL migration/seeder, Azure deployment or PentaMini change.
