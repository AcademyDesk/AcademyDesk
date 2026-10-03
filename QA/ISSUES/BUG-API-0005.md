# BUG-API-0005 — Teacher creation forms serialize publication booleans as strings

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | STATIC-FINDING |
| Final verification | NOT RUN |
| Severity | Major teaching form contract mismatch |
| Priority | P1 |
| Category | API |
| Module | TEACHERPORTAL |
| Role | See reproduction; same-tenant Admin unless stated otherwise |
| Screen / route | /teacher |
| API | POST api/teacher/assignments; POST api/teacher/assessments |
| Environment | Source review of local working tree; runtime production state not inferred |
| Device/viewport | NOT RUN; use QA/06_DEVICE_VIEWPORT_MATRIX.md where UI applies |
| Baseline | 20bb6047f9edf733ac8e2a226621cc582ec54b3c + pre-existing student UI diff |
| Discovery test / review ID | TEACHERPORTAL-CONTRACT-001 |
| Evidence classification | Static trace; runtime reproduction pending |
| Preconditions | Isolated synthetic fixture from QA/10_TEST_DATA_STRATEGY.md; never customer data |
| Reproduction frequency | Not measured; reproduction instructions are proposed |
| Source | apps/web/src/app/teacher/page.tsx:525 |
| Class/function | TeachingTools.create |
| Related source | INVENTORY/API_ENDPOINTS.md, CONTRACTS.md, FORMS_AND_FIELDS.md and ASYNC_FORM_RISKS.md |
| Root-cause confidence | HIGH |
| Evidence location | Source excerpt below; source fingerprint in INVENTORY/SOURCE_MANIFEST.md |
| Screenshot | Not captured on pinned baseline |
| Console logs | Not captured; required in retest |
| API request | Submit homework with a valid batch/title. Submit assessment with valid batch/title/score and filled date. Inspect JSON isPublished types and HTTP model-binding response; repeat with actual JSON booleans as positive controls. |
| API response | Not captured for this issue; use synthetic request/response in isolated reproduction |
| Database before/after | Not executed; fixture and fresh-context assertions defined below |
| Dependencies | Safe SQL/HTTP/browser harness as applicable; desired policy review where noted |
| Fix commit | Not implemented (Phase 1) |
| Retest result | NOT RUN |
| Regression result | NOT RUN |
| Closure notes | Remain OPEN; follow closure requirements in QA README |

## Exact reproduction

Submit homework with a valid batch/title. Submit assessment with valid batch/title/score and filled date. Inspect JSON isPublished types and HTTP model-binding response; repeat with actual JSON booleans as positive controls.

## Expected

UI serializes true/false as JSON booleans matching TeacherCreateAssignmentRequest and TeacherCreateAssessmentRequest.

## Actual / evidence

Hidden inputs produce strings; create converts maxScore and nonblank date only, then JSON.stringify preserves isPublished as a string. DTO expects bool; default AddControllers configuration has no boolean-string converter. Runtime reproduction pending.

Source snapshot:

```text
524:     const f = new FormData(e.currentTarget);
525:     const body = Object.fromEntries(f) as Record<string, unknown>;
526:     if (!body.batchId) {
527:       setM("Select an assigned batch before saving.");
```

## Suspected root cause

FormData string values are not normalized to the API contract.

## Business impact and blast radius

Teacher homework and assessment create flows; blank assessment dates additionally share BUG-API-0001.

## Related / required regression

TEACHERPORTAL-CONTRACT-001: Browser request assertions and real HTTP binding for true/false, string equivalents, blank optional date and valid ISO date; assert exactly one saved row and visible confirmation.

Also run all endpoint/form cases pointing to this issue in QA/03_TEST_MATRIX.md and the critical regression suite before closure.
