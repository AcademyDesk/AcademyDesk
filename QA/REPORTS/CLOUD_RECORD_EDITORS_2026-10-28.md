# Guardian, teacher and admission-fee editor isolation — 2026-10-28

MODEL: Session Codex model; no model override or delegation.
TOOL: Codex cloud terminal.
REPOSITORY: AcademyDesk/AcademyDesk.
BRANCH: codex/release-export-security; base 0310dbfabb490867a4af12283f0035b1924a0658.
ENVIRONMENT: Managed Linux cloud; synthetic API responses, no SQL/browser.
TASK: Continue BUG-DATA-0018 for guardian/teacher profiles and admission fees.
MAY MODIFY: Those editors, shared editor helper, relevant QA runtime/tests/docs.
MUST NOT MODIFY: Backend business rules, schema, customer records or deployments.
VALIDATE: Controlled delayed/rejected/reversed reads and writes; TypeScript/lint/build.
STOP WHEN: Bounded cases pass, full-suite limitations recorded, checkpoint pushed.
NEXT ROUTE: Codex for remaining profile/fee workflows and guarded SQL/HTTP gates.

## Reproduction and resulting behavior

Thirty initial controlled actual-TSX cases produced 28 failures/two passes.
Previous values remained visible and saveable under a newly selected guardian,
teacher or admission-fee target. Old loads/saves replaced newer selections,
duplicate clicks emitted duplicate writes, and transport failure could leave an
admission-fee save permanently busy. Raw network error text was also shown by
the profile editors. These were synthetic local reproductions, not SQL evidence.

`useRecordEditor` binds the loaded data and draft to the exact URL and a distinct
load generation. Selection changes reset before commit; failed/pending loads
cannot save. Read and write completions update only their original generation,
including A → B → A, and stop updating after unmount. Failed reads have a retry
action. Duplicate same-generation writes are blocked; inputs lock during saving.
Rejected saves preserve drafts; generic transport recovery avoids raw errors.
An unreadable body after a confirmed successful write retains success and asks
for a reload rather than presenting a failed write.

Profile date-null/availability and admission zero/date-null request behavior is
preserved. Fee amounts and authorization remain governed by the existing API.
Teacher details close on selection. The subject-fee child on the fees page is
keyed by academy/student to prevent drafts carrying between selected students.
Initial academy/list reads now check status and ignore unmounted completions.

## Final validation for this increment

- All 47 focused cases PASS, zero failed/skipped/cancelled. Covers the original
  30, A/B/A loads, overlapping saves, invalid reply shapes, malformed confirmed
  readbacks and exact zero/date/availability payloads.
- Full frontend suite: 45 current files, 1,013 tests; 1,010 PASS / three FAIL,
  zero skipped/cancelled. Only communication-drafts historical SQL assertions
  fail because QA/EVIDENCE/logs/phase-2b-communication-roundtrip-sql.log is absent.
  Nothing was fabricated or skipped. Runner reports that file's actual exit 1.
- TypeScript PASS; full lint PASS with zero errors/63 warnings (previously 65).
  No rule suppression. Production webpack build PASS on Next.js 16.4.3.
- Diff check PASS. Scoped secret-pattern scan runs before commit (heuristic).
- Earlier API 1,073 PASS and package-audit results are retained, not rerun for
  frontend-only changes. PentaMini is unchanged.

Ignored logs are under .build-check/: record-editor-before.log,
record-editor-final.log, record-editor-full-frontend.log, record-editor-lint.log,
record-editor-build.log and frontend-tests/summary.json. They are local execution
artifacts and are not broadly committed as QA evidence.

## Limits and continuation

BUG-DATA-0018 remains OPEN. Student administrative profiles, assessment/attendance,
tenant discovery and other editor paths need separate race coverage and any
needed repairs. Same-record server concurrency/idempotency, browser/device,
actual HTTP/SQL persistence and critical release acceptance remain unverified.
A client pending guard does not establish backend exactly-once behavior.

Runtime policy was reread and still lacks the three saved CDN requirements.
SQL/model blocked downloads were not repeated. No configured application database
was used. No environment configuration changed or was published, no main merge,
and no Azure deployment. Development-branch pushes remain user-authorized.
