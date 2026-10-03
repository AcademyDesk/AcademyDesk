# Phase 2B — Course controller contract checks

Historical controller checkpoint: [real Course HTTP/SQL checks](PHASE_2B_COURSE_SQL_CHECK.md) now add two fresh47-case SQL runs without changing application sources. Its322 backend/233 frontend results are retained, not rerun. Browser/device/all-linked/critical acceptance remains open.

2026-10-01. Sol High; verifies the remaining controller-level gap for accepted [BUG-DATA-0028](../ISSUES/BUG-DATA-0028.md), not a repeat audit. No application source change or deployment. Issue/Phase 2B remain OPEN.

[45 new tests](../../tests/AcademyDesk.Api.Tests/CoursePreservationTests.cs) execute the unchanged CoursesController against uniquely named EF InMemory fixtures. They cover:

- 18 full-summary/list → camelCase JSON → request → edit/deactivate/reactivate cases: Music/Tuition/Coaching, populated/null optionals. Fresh DbContext reads compare every entity property, including identity/academy/timestamps. Response summaries match expected settings; foreign course and structured prerequisite records remain unchanged.
- 6 create cases, 3 intentional visible-level clears, 1 explicit full optional-setting clear preserving replacement semantics, 2 foreign/missing-target404 no-write checks.
- 12 rejected replacements: blank name, invalid type, weekly/session bounds, age bounds/order and duplicate same-academy code. Fresh reads verify unchanged target/sibling rows.
- 1 foreign-academy duplicate-code control, 1 intentional string-normalization control and 1 legacy abbreviated-body reproduction. The legacy case deliberately confirms loss under the unchanged replacement API; it does not require a server PATCH/default-preservation change.

[Targeted log](../EVIDENCE/logs/phase-2b-course-contract-targeted.log)/[TRX](../EVIDENCE/course-contract/course-contract-targeted.trx):45/45 PASS. [Fresh full backend suite](../EVIDENCE/logs/phase-2b-course-contract-suite.log)/[TRX](../EVIDENCE/course-contract/course-contract-suite.trx):322/322 PASS = prior277 plus45, no failures/skips. No existing assertion was removed or weakened.

Build/test output is isolated in `.build-check/course-contract`; normal backend binaries and source remain unchanged. The suite's existing guarded Testing-host smoke check creates and cleans only its run-owned temporary folders and issues no database request. It does not start a second user-facing development server. New Course tests use no configured connection strings, SQL database, Azure resource, migrations or outbound integrations.

This is direct-controller execution and System.Text.Json Web-options binding, **not HTTP routing/model-validation/filter/role authorization, SQL constraints/transactions/concurrency or browser/device proof**. Foreign target checks prove controller row scoping, not the authorization pipeline. Fresh reads verify InMemory stored rows, not SQL durability. Existing frontend233/233, TypeScript and unchanged Course lint1error/1warning/helper-clean results remain historical and were not rerun because all prior sources are unchanged.

[Snapshot](PHASE_2B_COURSE_CONTRACT_SOURCE_SNAPSHOT.json):157 sources; all155 prior source captures unchanged,2 test/validator additions. Normal API/SQL-harness fingerprints unchanged; isolated API/test binary fingerprints separately retained. [Validator](../tools/validate-course-contract.cjs)/[output](../EVIDENCE/logs/phase-2b-course-contract-validation.log) checks hashes, unchanged HEAD, exact delta, logs plus TRX counts/outcomes, links and OPEN issue status.

HEAD20bb6047f9edf733ac8e2a226621cc582ec54b3c unchanged; broad dirty work preserved. No commit/push, database/Azure writes, normal dev restart or customer-data deletion.

Next: extend the existing isolated HTTP/SQL harness for Course preservation and rejected/foreign-target no-write acceptance; Sol High. Browser/device/all-linked/critical, concurrent replacement and existing lint remain separate open gates. Do not repeat unchanged audit work or use the normal development database.
