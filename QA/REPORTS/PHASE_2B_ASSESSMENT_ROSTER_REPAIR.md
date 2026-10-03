# Phase 2B — Assessment roster membership repair (partial)

2026-10-01. Sol High task allocation. Accepted [BUG-DATA-0034](../ISSUES/BUG-DATA-0034.md) source evidence reused; no repeat Phase1 audit. Local only; issue and Phase2B remain OPEN.

## Repair and policy boundary

The [administrator result writer](../../apps/api/Controllers/AssessmentsController.cs) now requires an enrollment matching all three keys: academy, assessment batch and student. Same-tenant students belonging only to another batch, students never enrolled, and enrollment rows with the wrong academy or student cannot create or overwrite results. A400 response provides guidance before grading or any mutation. Existing unrelated results are not silently deleted or reassigned. Active exact members retain zero-score creation and updates without duplicate results.

This is deliberately a membership-only repair. Administrator enrollment-status behavior is not changed yet: Paused, Completed, Waitlisted, Withdrawn, Cancelled and Transferred membership are not filtered out by this guard. Teacher saves retain their pre-existing active-only rule. New-result eligibility and historical corrections need the policy choice recorded in the accepted issue; an asynchronous question has been sent to the user. Proposed choices are active-only new results plus correction of an existing historical result, or active-only for all saves. Neither has been assumed approved. This partial fix must not be presented as full roster-policy closure.

No frontend, DTO, schema/migration, publication, delegated permission/module, Teacher writer or historic transcript-visibility changes. [BUG-FUNC-0020](../ISSUES/BUG-FUNC-0020.md) action permission/module mapping remains a separate next academic gap. Previous grade-repair evidence remains historical evidence for its captured source; this checkpoint supersedes its current-source fingerprint for the shared controller and test fixture.

## Verification

- [Backend suite](../EVIDENCE/logs/phase-2b-assessment-roster-suite.log)/[TRX](../EVIDENCE/assessment-roster/assessment-roster-suite.trx):408/408 PASS, including[12 roster tests](../../tests/AcademyDesk.Api.Tests/AssessmentRosterTests.cs). Negative create/overwrite attempts preserve existing score, grade, notes and publication in a fresh context. The prior grading roundtrip fixture now includes the required active enrollment; its assertions are unchanged.
- [Real HTTP/SQL harness](../tools/SqlHarness/AssessmentRosterRegression.cs)/[final log](../EVIDENCE/logs/phase-2b-assessment-roster-sql-final.log):30 cases PASS, real Identity/Admin/assigned Teacher/Student/Guardian. Never-enrolled, wrong-batch, wrong enrollment academy/student, foreign student/token/assessment and missing assessment tested. Rejected requests leave captured result/enrollment/notification/audit rows unchanged. Active creation/update response and fresh GET are checked against SQL. Family projections use the actual Admin API-saved published result. Teacher missing/foreign assessment keeps its403 contract; Admin keeps404.
- The preceding[30-case development run](../EVIDENCE/logs/phase-2b-assessment-roster-sql-development.log) used a directly seeded family-visible result. It is not claimed as an identical final run. The initial[aborted run](../EVIDENCE/logs/phase-2b-assessment-roster-sql.log) stopped after three passing cases because a repeated synthetic batch name violated the existing unique index. This was a fixture failure, not an application regression. The fixture names were made unique and that exact label/loopback-validated container was removed, including its disposable database/login; its local root was already absent.
- [SQL build](../EVIDENCE/logs/phase-2b-assessment-roster-sql-build.log):0 warnings/errors. Isolated build outputs keep both normal development assemblies unchanged.81 domain and7 Identity migrations; unchanged311-route inventory and digest. No additional migration is needed for this membership guard.

## Remaining and handoff

Historical-status policy/matrix, actual browser/device form guidance and visibility, concurrency, delegated academic access and full critical/release acceptance remain pending. No original-defect runtime baseline was repeated. Owned generated SQL resources/root folders are cleaned; normal laptop database/services, Azure/Blob and customer data remain untouched. No commit/push/deployment.

[Bounded evidence snapshot](PHASE_2B_ASSESSMENT_ROSTER_SOURCE_SNAPSHOT.json) and[validator](../tools/validate-assessment-roster.cjs) verify this slice, not the whole application. Next: resolve historical-correction policy, complete the status matrix, then the separate assessment-result permission/module repair. Keep Sol High.
