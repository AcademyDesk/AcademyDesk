# BUG-DATA-0015 — Lesson creation accepts unchecked module and session references

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | STATIC-FINDING |
| Final verification | NOT RUN |
| Severity | Major academic relationship integrity |
| Priority | P1 |
| Category | DATA |
| Module | TEACHERPORTAL |
| Role | See reproduction; same-tenant Admin unless stated otherwise |
| Screen / route | /teacher; direct API |
| API | POST api/teacher/lesson-plans |
| Environment | Source review of local working tree; runtime production state not inferred |
| Device/viewport | NOT RUN; use QA/06_DEVICE_VIEWPORT_MATRIX.md where UI applies |
| Baseline | 20bb6047f9edf733ac8e2a226621cc582ec54b3c + pre-existing student UI diff |
| Discovery test / review ID | TEACHERPORTAL-LINK-001 |
| Evidence classification | Static trace; runtime reproduction pending |
| Preconditions | Isolated synthetic fixture from QA/10_TEST_DATA_STRATEGY.md; never customer data |
| Reproduction frequency | Not measured; reproduction instructions are proposed |
| Source | apps/api/Controllers/TeacherPortalController.cs:317 |
| Class/function | CreateLessonPlan |
| Related source | INVENTORY/API_ENDPOINTS.md, CONTRACTS.md, FORMS_AND_FIELDS.md and ASYNC_FORM_RISKS.md |
| Root-cause confidence | HIGH |
| Evidence location | Source excerpt below; source fingerprint in INVENTORY/SOURCE_MANIFEST.md |
| Screenshot | Not captured on pinned baseline |
| Console logs | Not captured; required in retest |
| API request | Create a plan for an owned batch while supplying a module from another course/tenant or a session from another batch/tenant. Also supply nonexistent IDs and omitted optional IDs. |
| API response | Not captured for this issue; use synthetic request/response in isolated reproduction |
| Database before/after | Not executed; fixture and fresh-context assertions defined below |
| Dependencies | Safe SQL/HTTP/browser harness as applicable; desired policy review where noted |
| Fix commit | Not implemented (Phase 1) |
| Retest result | NOT RUN |
| Regression result | NOT RUN |
| Closure notes | Remain OPEN; follow closure requirements in QA README |

## Exact reproduction

Create a plan for an owned batch while supplying a module from another course/tenant or a session from another batch/tenant. Also supply nonexistent IDs and omitted optional IDs.

## Expected

Optional references may be omitted; supplied references must exist within the academy and match the selected course/batch.

## Actual / evidence

Only title and OwnsBatch are checked; supplied CourseModuleId and ClassSessionId are copied directly into the entity. No relation validation in this action. LessonPlansController.Create likewise checks only title and same-tenant batch before copying optional module/session IDs; its UI currently sends both null. Persistence outcomes require real SQL confirmation.

Source snapshot:

```text
316:             return BadRequest(new { message = "Select a batch you teach and provide a lesson title." });
317:         var plan = new LessonPlan { AcademyId = user.AcademyId.Value, BatchId = request.BatchId, CourseModuleId = request.CourseModuleId, ClassSessionId = request.ClassSessionId, Title = request.Title.Trim(), Objectives = request.Objectives?.Trim() };
318:         dbContext.LessonPlans.Add(plan);
319:         await dbContext.SaveChangesAsync(cancellationToken);
```

## Suspected root cause

Ownership of the primary batch is treated as sufficient validation of secondary identifiers.

## Business impact and blast radius

Direct teacher lesson-plan requests; both teacher and admin UIs omit these optional identifiers; direct API supplies them.

## Related / required regression

TEACHERPORTAL-LINK-001: Real HTTP/SQL tests for null, valid, wrong course/batch, foreign tenant and nonexistent references; denied requests persist no lesson plan.

Also run all endpoint/form cases pointing to this issue in QA/03_TEST_MATRIX.md and the critical regression suite before closure.
