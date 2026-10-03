# Phase 2B — Assessment-result API access alignment

2026-10-01. Sol High task allocation. Accepted[BUG-FUNC-0020](../ISSUES/BUG-FUNC-0020.md) findings reused; no repeat Phase1 audit. Local-only bounded repair; issue and Phase2B remain OPEN.

## Result

Two catalog entries were added. [PermissionCatalog](../../apps/api/Security/PermissionCatalog.cs) now maps `AssessmentResultsController` to `academics.manage`; [SubscriptionPlanCatalog](../../apps/api/Security/SubscriptionPlanCatalog.cs) maps it to `AcademicGovernance`. These match the existing `AssessmentsController` setup policy. The result API no longer denies a properly delegated academic permission solely because of a missing catalog entry, or falls back to always-enabled Core when the academic module is disabled.

Admin/Owner result access now requires the same academic module as setup; delegated custom roles and current per-user grants follow the existing permission-filter rules. Generic student, enrollment and batch-management permissions were not broadened. The Teacher-specific owned-assessment routes and pre-existing platform-owner filter bypass are unchanged; this is not a platform bypass repair or new subscription policy. No enrollment-status policy is inferred: the [prior partial roster repair](PHASE_2B_ASSESSMENT_ROSTER_REPAIR.md) historical-correction question remains open.

## Verification

- [Backend suite](../EVIDENCE/logs/phase-2b-assessment-access-suite.log)/[TRX](../EVIDENCE/assessment-access/assessment-access-suite.trx):417/417 PASS, including[9 new tests](../../tests/AcademyDesk.Api.Tests/AssessmentAccessTests.cs). Setup/result permission and module parity, case-insensitive module recognition, absent/malformed module denial and unchanged broader management grants checked.
- [Real HTTP/SQL harness](../tools/SqlHarness/AssessmentAccessRegression.cs)/[log](../EVIDENCE/logs/phase-2b-assessment-access-sql.log):77 cases PASS. Eight actor roles × read/save; anonymous/foreign-route controls; absent, foreign-tenant, current, expired, revoked, permanent, missing-expiry and revoked-permanent per-user grants using the same issued token. Core-only, academic-only, empty and malformed module configurations checked for Admin, Owner and delegated academics. Inactive academy and inactive/reactivated account checked against existing tokens. Assigned Teacher own-route controls pass.
- An academic-only custom role creates an assessment through the real API, saves its active student's result, and reads it back. Response identities/scores and fresh SQL are compared. Rejected requests and successful reads leave captured assessment/result/enrollment/notification/audit rows unchanged. Publication/notification, audit-fault rollback, concurrency and all-role browser workflows are separate gates.
- Four page prerequisites independently return403 for the delegated academic-only actor: `/batches`, `/students`, `/enrollments`, `/grading-schemes/active`. These expected-denial observations demonstrate the remaining workflow gap, not completed UI acceptance. The existing page still uses those generic endpoints and cannot yet load for this actor.
- [Isolated SQL build](../EVIDENCE/logs/phase-2b-assessment-access-sql-build.log):0 warnings/errors.81 domain/7 Identity migrations, unchanged311-route inventory/digest, ownership marker and database-scoped runtime login checks. One successful final77-case SQL run; no duplicated final or original-defect runtime baseline claimed.

## Scope and next task

No frontend, result controller/Teacher writer, DTO, schema or migration changes in this slice. Prior grading/roster writer hashes, assessment frontend/CSS hashes and normal development binaries are checked unchanged by the[validator](../tools/validate-assessment-access.cjs); current bounded[evidence snapshot](PHASE_2B_ASSESSMENT_ACCESS_SOURCE_SNAPSHOT.json) supersedes current catalog/harness fingerprints, not previous historical evidence. Generated SQL database/login/container/local root removed after successful checks. Normal laptop database/services, Azure/Blob and customer data untouched. No commit/push/deployment.

Next: academic-scoped read-only assessment lookup contract and page integration, without adding academics to generic student/batch management. Keep Sol High. Historical roster-status policy/matrix, actual browser/device experience, platform bypass, broader transactional/concurrency/critical/release acceptance remain open. BUG-FUNC-0020 is only partially repaired until the necessary page dependencies and release criteria are satisfied.
