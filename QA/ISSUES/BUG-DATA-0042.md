# BUG-DATA-0042 — Administrative submission creation accepts unchecked students and client-supplied review state

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | CONFIRMED:6 real HTTP/SQL baseline bypasses; local repair |
| Final verification | PARTIAL: backend/MVC/HTTP-SQL PASS; browser/closure OPEN |
| Severity | Major academic record integrity |
| Priority | P1 |
| Category | DATA |
| Module | LEARNING |
| Role | See reproduction; same-tenant Admin unless stated otherwise |
| Screen / route | Direct administrative submission API |
| API | POST /api/academies/{academyId}/assignment-submissions |
| Environment | Source review of local working tree; runtime production state not inferred |
| Device/viewport | NOT RUN; use QA/06_DEVICE_VIEWPORT_MATRIX.md where UI applies |
| Baseline | 20bb6047f9edf733ac8e2a226621cc582ec54b3c + pre-existing student UI diff |
| Discovery test / review ID | LEARNING-SUBMISSION-IDENTITY-001 |
| Evidence classification | Static trace; runtime reproduction pending |
| Preconditions | Isolated synthetic fixture from QA/10_TEST_DATA_STRATEGY.md; never customer data |
| Reproduction frequency | Not measured; reproduction instructions are proposed |
| Source | apps/api/Controllers/AssignmentSubmissionsController.cs:4 |
| Class/function | Submit |
| Related source | INVENTORY/API_ENDPOINTS.md, CONTRACTS.md, FORMS_AND_FIELDS.md and ASYNC_FORM_RISKS.md |
| Root-cause confidence | HIGH |
| Evidence location | Source excerpt below; source fingerprint in INVENTORY/SOURCE_MANIFEST.md |
| Screenshot | Not captured on pinned baseline |
| Console logs | Not captured; required in retest |
| API request | As authorized admin in isolated fixtures submit a local assignment with a missing, foreign or wrong-batch StudentId, then a private assignment assigned to another learner. Include Status Reviewed, TeacherFeedback and altered SubmittedAtUtc in request; compare eligible published-assignment control. |
| API response | Not captured for this issue; use synthetic request/response in isolated reproduction |
| Database before/after | Not executed; fixture and fresh-context assertions defined below |
| Dependencies | Safe SQL/HTTP/browser harness as applicable; desired policy review where noted |
| Fix commit | Local uncommitted repair; no push/deployment |
| Retest result | Bounded local PASS; see Phase 2B report |
| Regression result | 849 backend (28 new) /34 real HTTP-SQL PASS; not full closure |
| Closure notes | Remain OPEN; follow closure requirements in QA README |

## Exact reproduction

As authorized admin in isolated fixtures submit a local assignment with a missing, foreign or wrong-batch StudentId, then a private assignment assigned to another learner. Include Status Reviewed, TeacherFeedback and altered SubmittedAtUtc in request; compare eligible published-assignment control.

## Expected

Submission validates same-tenant eligible student and assignment scope, and owns submission/review lifecycle fields server-side. Intentional admin overrides require an explicit audited contract.

## Actual / evidence

Only assignment academy/existence is checked. Domain object is accepted wholesale; only Id and AcademyId are replaced. StudentId, Status, TeacherFeedback and SubmittedAtUtc persist from caller; no student/enrollment/private-target/publication checks exist and mapped schema has no corresponding relationship guard. Runtime NOT RUN; no unauthorized caller bypass or cross-tenant data read is claimed.

Source snapshot:

```text
3: [ApiController][Route("api/academies/{academyId:guid}/assignment-submissions")]
4: public sealed class AssignmentSubmissionsController(AcademyDeskDbContext db):ControllerBase{[HttpGet]public async Task<ActionResult<IReadOnlyList<AssignmentSubmission>>>List(Guid academyId,Guid? assignmentId,CancellationToken t){var q=db.AssignmentSubmissions.AsNoTracking().Where(x=>x.AcademyId==academyId);if(assignmentId.HasValue)q=q.Where(x=>x.AssignmentId==assignmentId);return Ok(await q.OrderByDescending(x=>x.SubmittedAtUtc).ToListAsync(t));}[HttpPost]public async Task<ActionResult<AssignmentSubmission>>Submit(Guid academyId,AssignmentSubmission x,CancellationToken t){if(!await db.Assignments.AnyAsync(a=>a.Id==x.AssignmentId&&a.AcademyId==academyId,t))return BadRequest();x.Id=Guid.NewGuid();x.AcademyId=academyId;db.AssignmentSubmissions.Add(x);await db.SaveChangesAsync(t);return Ok(x);}[HttpPatch("{id:guid}/review")]public async Task<ActionResult<AssignmentSubmission>>Review(Guid academyId,Guid id,ReviewRequest r,CancellationToken t){var x=await db.AssignmentSubmissions.SingleOrDefaultAsync(x=>x.Id==id&&x.AcademyId==academyId,t);if(x is null)return NotFound();x.TeacherFeedback=r.Feedback;x.Status="Reviewed";await db.SaveChangesAsync(t);return Ok(x);}}
5: public sealed record ReviewRequest(string? Feedback);
6:
```

## Suspected root cause

Domain entity binding substitutes for a validated submission request and lifecycle transition.

## Business impact and blast radius

Administrative submission records can be attributed to ineligible/nonexistent learners and marked reviewed at creation; teacher/student endpoints have different guards.

## Related / required regression

LEARNING-SUBMISSION-IDENTITY-001: Full HTTP/SQL local/foreign/missing/wrong-batch/private-target student matrix, unpublished assignments, overposted lifecycle fields and duplicate assignment/student keys. Compare approved administrator override policy and fresh rows; reject invalid requests without writes.

Also run all endpoint/form cases pointing to this issue in QA/03_TEST_MATRIX.md and the critical regression suite before closure.

## Phase 2B local repair — 2026-10-01

[Execution report](../REPORTS/PHASE_2B_SUBMISSION_IDENTITY_REPAIR.md):6 real HTTP/SQL baseline bypasses; normalized24 controller baseline5 PASS/19 expected FAIL. Narrow create DTO ignores overposted entity lifecycle fields; independently scoped student/published assignment/batch, private-target and Active enrollment validated. Server creates Submitted/null feedback/current UTC lifecycle; nullable/empty response preserved,4000 boundary enforced; duplicate409 preserves stored work/audits.849 backend (28 new, including4 actual MVC validation cases) and34 real Identity/global-filter/HTTP-SQL PASS; concurrent pair one200/one409 with one row/audit. Initial MVC property-annotation500 repaired by constructor annotation; later harness415 expectation corrected; failed evidence retained, excluded and exact owned targets cleaned. No new inactive/date-window/backfill policy, schema/access/frontend/review changes or dev/Azure/commit/deploy. OPEN for browser/linked/critical/policy/eligibility races/fault/release acceptance.
