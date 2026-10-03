# Phase 2B — Course real HTTP/SQL checks

Historical checkpoint: superseded by [the prerequisite repair](PHASE_2B_PREREQUISITE_REPAIR.md). Its controller322 count and next-task recommendation below are retained historical evidence; current backend suite is341. Course-specific browser/device/critical gates remain open.

2026-10-01. Sol High. Closes the bounded HTTP/SQL verification gap for the local [Course preservation repair](PHASE_2B_COURSE_PRESERVATION_REPAIR.md), not the whole issue/phase. No application change or Azure deployment. [BUG-DATA-0028](../ISSUES/BUG-DATA-0028.md) remains OPEN for browser/device/all-linked/critical acceptance.

## Executed scope

[Course harness](../tools/SqlHarness/CoursePreservationRegression.cs) runs through the real Testing application, Identity login, bearer authentication, MVC model binding/filters and SQL Server provider:

- 18 independent edit/deactivate/reactivate cases: Music/Tuition/Coaching x populated/null settings. Full list projections supply the complete PUT body; fresh SQL reads compare every entity field including identity/academy/timestamps. All response/list DTO fields match stored values; foreign rows and synthetic structured prerequisite records remain unchanged.
- 3 intentional visible-level clears and 1 full optional-setting/publication clear preserve the replacement contract. Minimum age0 and true/false publication controls are included.
- 12 invalid PUT400 cases cover name/type, weekly/session bounds, age bounds/order and same-academy duplicate code. 6 POST/PUT role controls cover foreign Admin/Teacher403 and anonymous401. Missing/foreign IDs404 and foreign-route403 add3 controls. Rejections preserve the captured state, including audits.
- 3 create controls with null, omitted or blank optional values return201 and store null/false defaults. Every seed POST additionally verifies its complete SQL row, response/list projection, unrelated state and one actor/route audit; those setup assertions are not separately counted as cases.
- 1 legacy abbreviated-body case reproduces data loss under the unchanged replacement API. This proves the frontend complete-body repair is required; no server PATCH semantics or validation weakening was introduced.

Every checked list GET is tenant-scoped and leaves captured state unchanged. Success adds exactly one own actor/controller/route audit. Captured state comprises Courses, prerequisites, modules, batches, sessions, plus the established Governance people/academy/branch/Identity/finance/task/audit snapshots—not every database table. Audit-fault rollback, concurrent writes, all roles/modules/endpoints and performance are not certified.

## Evidence and isolation

[Run1](../EVIDENCE/logs/phase-2b-course-sql-run1.log) and [Run2](../EVIDENCE/logs/phase-2b-course-sql-run2.log):47 exact cases PASS each on distinct generated fixtures. [Build](../EVIDENCE/logs/phase-2b-course-sql-build.log):0 warnings/errors. Both runs use the same pinned isolated API/harness binaries with no application/harness source edits between them. Historical controller45/backend322, frontend233 and TypeScript results are retained, not rerun. Existing Course lint1error/1warning remains open; no browser or production-build claim.

Existing [wrapper](../tools/SqlHarness/Run-ReconciledPayment.ps1)/dispatcher gain a Course module only; other module invocations remain unchanged. Course output builds/runs from `.build-check/course-sql`, without replacing normal backend/harness binaries. Runs use generated loopback-only SQL Server2022 containers/databases, least-privilege runtime login, isolated Testing roots and synthetic .invalid users. Both resolve domain/Identity contexts to the exact owned target, apply80 domain/7 Identity migrations and keep runtime inventory311 routes/300 controller routes/10 Identity routes with the established digest.

Cleanup first refuses a mismatched ownership marker, then removes only each generated database/login/container/root. Logs remain; disposable synthetic data is regenerable. No development database, frontend/backend service, Azure/Blob resource or customer data was altered. The validator independently checks absence of owned containers/roots/listeners after each run. No normal development stack was duplicated or restarted.

[Snapshot](PHASE_2B_COURSE_SQL_SOURCE_SNAPSHOT.json):159 captures;2 existing QA dispatcher/wrapper files changed,155 prior captures unchanged,2 new QA harness/validator files. Normal API/harness and controller-test binary fingerprints remain unchanged; new isolated runtime binaries separately pinned. [Validator](../tools/validate-course-sql.cjs)/[output](../EVIDENCE/logs/phase-2b-course-sql-validation.log) verifies exact source/binary delta,47 statuses per run, runtime/migration/ownership checks, cleanup, links, OPEN status and unchanged HEAD20bb6047f9edf733ac8e2a226621cc582ec54b3c. Broad dirty work preserved; no commit/push.

## Next

Continue with accepted circular Course prerequisite prevention, BUG-DATA-0029, Sol High. Reuse Astra's finding, implement the missing guard and isolated regressions; do not redo unchanged audit work. Course browser/device/accessibility, existing lint, concurrent replacements, all-linked and critical/release acceptance remain separate open gates.
