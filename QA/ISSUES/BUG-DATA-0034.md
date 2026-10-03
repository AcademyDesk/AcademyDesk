# BUG-DATA-0034 — Administrator assessment results accept students outside the assessment batch

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | Accepted static finding; local membership-only repair verified |
| Final verification | PARTIAL PASS; historical-status policy and release acceptance pending |
| Severity | Major assessment membership inconsistency |
| Priority | P1 |
| Category | DATA |
| Module | ACADEMIC |
| Role | See reproduction; same-tenant Admin unless stated otherwise |
| Screen / route | /assessments; direct result API |
| API | POST academies/{academyId}/assessments/{assessmentId}/results |
| Environment | Source review of local working tree; runtime production state not inferred |
| Device/viewport | NOT RUN; use QA/06_DEVICE_VIEWPORT_MATRIX.md where UI applies |
| Baseline | 20bb6047f9edf733ac8e2a226621cc582ec54b3c + pre-existing student UI diff |
| Discovery test / review ID | ACADEMIC-ROSTER-001 |
| Evidence classification | Static trace; runtime reproduction pending |
| Preconditions | Isolated synthetic fixture from QA/10_TEST_DATA_STRATEGY.md; never customer data |
| Reproduction frequency | Not measured; reproduction instructions are proposed |
| Source | apps/api/Controllers/AssessmentsController.cs:42 |
| Class/function | AssessmentResults.Upsert |
| Related source | INVENTORY/API_ENDPOINTS.md, CONTRACTS.md, FORMS_AND_FIELDS.md and ASYNC_FORM_RISKS.md |
| Root-cause confidence | HIGH |
| Evidence location | Source excerpt below; source fingerprint in INVENTORY/SOURCE_MANIFEST.md |
| Screenshot | Not captured on pinned baseline |
| Console logs | Not captured; required in retest |
| API request | Create assessment for batchA and a same-academy student enrolled only in batchB. Submit a valid score for that student through the administrator result API. Compare assigned-teacher result request and UI roster. |
| API response | Not captured for this issue; use synthetic request/response in isolated reproduction |
| Database before/after | Not executed; fixture and fresh-context assertions defined below |
| Dependencies | Safe SQL/HTTP/browser harness as applicable; desired policy review where noted |
| Fix commit | Local uncommitted membership guard; no deployment |
| Retest result | 408 backend tests and final30 real HTTP/SQL cases PASS for bounded membership scope |
| Regression result | Partial: status/historical-policy and full browser/device/critical gates pending |
| Closure notes | Remain OPEN; follow closure requirements in QA README |

## Local repair checkpoint — 2026-10-01

See[partial repair report](../REPORTS/PHASE_2B_ASSESSMENT_ROSTER_REPAIR.md). Administrator Upsert now requires exact academy/batch/student enrollment before any grading or mutation. Wrong-batch/never-enrolled/foreign-membership creates and overwrites are rejected without captured result/enrollment/notification/audit writes; active create/update/readback and real API-saved Student/Guardian visibility pass. Backend408/408 including12 new roster tests; finalHTTP/SQL30/30. No status filter has been added: the historical/new-result policy question is still pending. Keep OPEN; this is not full roster eligibility or deployment certification. Original static evidence below remains historical.

## Original reproduction

Create assessment for batchA and a same-academy student enrolled only in batchB. Submit a valid score for that student through the administrator result API. Compare assigned-teacher result request and UI roster.

## Expected

A result targets an eligible learner of the assessment batch; any historical or administrator exception must be an explicit policy and workflow.

## Actual / evidence

Administrator path checks same-academy student existence only, so unrelated students can receive results. Teacher path requires active enrollment in the assessment batch, and administrator UI also renders active batch learners only. Runtime NOT RUN; historical enrollment exception policy remains to be agreed.

Source snapshot:

```text
41:         if (assessment is null) return NotFound();
42:         if (!await dbContext.Students.AnyAsync(x => x.Id == request.StudentId && x.AcademyId == academyId, cancellationToken)) return BadRequest(new { message = "The student does not belong to this academy." });
43:         if (request.Score < 0 || request.Score > assessment.MaxScore) return BadRequest(new { message = "Score must be within the assessment range." });
44:         var result = await dbContext.AssessmentResults.SingleOrDefaultAsync(x => x.AcademyId == academyId && x.AssessmentId == assessmentId && x.StudentId == request.StudentId, cancellationToken);
```

## Suspected root cause

Enrollment eligibility is enforced in the UI and teacher writer but omitted from administrator result mutation.

## Business impact and blast radius

Same-tenant wrong-batch results and records absent from the intended roster; not a demonstrated cross-tenant authorization bypass.

## Related / required regression

ACADEMIC-ROSTER-001: Real HTTP active/paused/completed/waitlisted/never-enrolled and foreign-tenant matrix, fresh row counts and portal visibility. Define historical correction permission before requiring active-only admission everywhere.

Also run all endpoint/form cases pointing to this issue in QA/03_TEST_MATRIX.md and the critical regression suite before closure.
