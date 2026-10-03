# BUG-DATA-0043 — Practice-log creation accepts unchecked learner and caller-supplied review lifecycle

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | CONFIRMED:6 real HTTP/SQL baseline bypasses; local repair |
| Final verification | PARTIAL: backend/MVC/HTTP-SQL PASS; browser/closure OPEN |
| Severity | Major learner-record integrity |
| Priority | P1 |
| Category | DATA |
| Module | LEARNING |
| Role | See reproduction; same-tenant Admin unless stated otherwise |
| Screen / route | /practice-logs |
| API | POST /api/academies/{academyId}/practice-logs |
| Environment | Source review of local working tree; runtime production state not inferred |
| Device/viewport | NOT RUN; use QA/06_DEVICE_VIEWPORT_MATRIX.md where UI applies |
| Baseline | 20bb6047f9edf733ac8e2a226621cc582ec54b3c + pre-existing student UI diff |
| Discovery test / review ID | LEARNING-PRACTICE-IDENTITY-001 |
| Evidence classification | Static trace; runtime reproduction pending |
| Preconditions | Isolated synthetic fixture from QA/10_TEST_DATA_STRATEGY.md; never customer data |
| Reproduction frequency | Not measured; reproduction instructions are proposed |
| Source | apps/api/Controllers/PracticeLogsController.cs:4 |
| Class/function | Create |
| Related source | INVENTORY/API_ENDPOINTS.md, CONTRACTS.md, FORMS_AND_FIELDS.md and ASYNC_FORM_RISKS.md |
| Root-cause confidence | HIGH |
| Evidence location | Source excerpt below; source fingerprint in INVENTORY/SOURCE_MANIFEST.md |
| Screenshot | Not captured on pinned baseline |
| Console logs | Not captured; required in retest |
| API request | As an authorized user in isolated fixtures submit a valid control plus missing, foreign and inactive StudentId values. Include Status=Reviewed, TeacherFeedback and ReviewedAtUtc in create JSON; read the fresh row and compare the separate Review endpoint. |
| API response | Not captured for this issue; use synthetic request/response in isolated reproduction |
| Database before/after | Not executed; fixture and fresh-context assertions defined below |
| Dependencies | Safe SQL/HTTP/browser harness as applicable; desired policy review where noted |
| Fix commit | Local uncommitted repair; no push/deployment |
| Retest result | Bounded local PASS; see Phase 2B report |
| Regression result | 875 backend (26 new) /37 real HTTP-SQL PASS; not full closure |
| Closure notes | Remain OPEN; follow closure requirements in QA README |

## Exact reproduction

As an authorized user in isolated fixtures submit a valid control plus missing, foreign and inactive StudentId values. Include Status=Reviewed, TeacherFeedback and ReviewedAtUtc in create JSON; read the fresh row and compare the separate Review endpoint.

## Expected

Creation verifies a same-academy eligible learner and owns review state, feedback and reviewed time on the server. Any privileged backfill is an explicit, audited contract.

## Actual / evidence

Create binds PracticeLog directly, checks only MinutesPracticed, replaces only Id and AcademyId, then saves supplied StudentId, Status, TeacherFeedback and ReviewedAtUtc. No learner lookup or relationship constraint is mapped for StudentId. Runtime NOT RUN; this finding does not claim an unauthorized caller bypass.

Source snapshot:

```text
3: [ApiController][Route("api/academies/{academyId:guid}/practice-logs")]
4: public sealed class PracticeLogsController(AcademyDeskDbContext db) : ControllerBase { [HttpGet] public async Task<ActionResult<IReadOnlyList<PracticeLog>>> List(Guid academyId,CancellationToken t)=>Ok(await db.PracticeLogs.AsNoTracking().Where(x=>x.AcademyId==academyId).OrderByDescending(x=>x.PracticeDate).ToListAsync(t)); [HttpPost] public async Task<ActionResult<PracticeLog>> Create(Guid academyId,PracticeLog x,CancellationToken t){if(x.MinutesPracticed<1)return BadRequest(new{message="Minutes must be greater than zero."});x.Id=Guid.NewGuid();x.AcademyId=academyId;db.PracticeLogs.Add(x);await db.SaveChangesAsync(t);return Ok(x);} [HttpPatch("{id:guid}/review")] public async Task<ActionResult<PracticeLog>> Review(Guid academyId,Guid id,ReviewPracticeLogRequest r,CancellationToken t){var x=await db.PracticeLogs.SingleOrDefaultAsync(x=>x.Id==id&&x.AcademyId==academyId,t);if(x is null)return NotFound();x.TeacherFeedback=r.TeacherFeedback?.Trim();x.Status="Reviewed";x.ReviewedAtUtc=DateTime.UtcNow;await db.SaveChangesAsync(t);return Ok(x);} }
5: public sealed record ReviewPracticeLogRequest(string? TeacherFeedback);
6:
```

## Suspected root cause

Domain-entity model binding is used instead of a constrained create request and server-owned lifecycle fields.

## Business impact and blast radius

Practice history, teacher review status and learner-facing progress summaries can contain invalid or prematurely reviewed records.

## Related / required regression

LEARNING-PRACTICE-IDENTITY-001: HTTP/SQL same-tenant/foreign/missing/inactive learner matrix; logged versus reviewed lifecycle overposting, dates/minutes boundaries, repeated day entries and Review-only feedback checks with fresh-row assertions.

Also run all endpoint/form cases pointing to this issue in QA/03_TEST_MATRIX.md and the critical regression suite before closure.

## Phase 2B local repair — 2026-10-01

[Execution report](../REPORTS/PHASE_2B_PRACTICE_IDENTITY_REPAIR.md):6 real HTTP/SQL baseline bypasses;19 controller baseline7 PASS/12 expected FAIL. Narrow create DTO ignores review/entity lifecycle overposting; validates active same-tenant learner,1–1440 minutes,nonfuture UTC date and250/2000 text maxima. Optional fields/omitted-null date default/explicit historic dates and repeated-day entries retained. Server sets Logged/null feedback-review time/current creation time; separate Review remains trimmed nullable feedback/server timestamp.875 backend (26 new including7 MVC) and37 real Identity/global-filter/HTTP-SQL PASS; complete fresh rows,exact review,actor audits,unrelated/foreign/reviewed-day preservation and no-write denials. Owned runs cleaned; frontend/schema/access/dev/Azure/commit/deploy unchanged. Issue OPEN for browser/data-entry feedback/linked/critical/timezone-policy/races/fault/release acceptance.
