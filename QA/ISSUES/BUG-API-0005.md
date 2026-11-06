# BUG-API-0005 — Teacher creation forms serialize publication booleans as strings

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | CONFIRMED in actual handlers and controlled Chromium; live acceptance pending |
| Final verification | PARTIAL — controlled frontend PASS; original live closure gates OPEN |
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
| Fix commit | 1ec22e733696fa84e4dbf947da463fbe4a2d123a (bounded frontend source) |
| Retest result | Controlled handler/browser/contract PASS; see 6 November return below |
| Regression result | Current cloud frontend1918PASS/3genuine evidence-prerequisite FAIL; no skips |
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

## Cloud C6 bounded return — 6 November 2026

Source1ec22e7 restores the preserved boolean/null-date conversion without
wholesale importing the older Teacher page. Literal true/false FormData values
become JSON booleans; unknown publication text or unreadable assessment dates
send nothing. Blank optional assessment date becomes null; a filled local date
uses the existing UTC conversion. Fractional maximum scores now pass native HTML
validation via step=any, matching the existing decimal API contract. This does
not change score/tenant/batch/publication rules on the server.

Success resets only the captured submitted form. A denied response keeps it;
interrupted transport reports an unconfirmed outcome and retains it without
automatic replay or a false success claim. The original twelve preserved native
FormData checks plus eight new guards/resilience cases all pass20/20. Pre-fix
payload checks7PASS/5FAIL and native browser requests reproduced string booleans
and the old empty date; native fractional-score submission was also blocked by
the old implicit integer step. The final browser covers both booleans, blank/
filled dates, fractional score, lesson-plan payload retention, denied response and
lost response. Actual compiled current API request DTOs accepted seven captured
payloads and rejected nine string/date variants. That is JSON compatibility only,
not real HTTP model-state/domain/Identity/SQL persistence. Current assigned-batch/
tenant/privacy, exact-one-row and visible live result acceptance remain open.
This assessment-date correction does not close BUG-API-0001's broader intake scope.

Original static/source history above is retained. Status remains OPEN: no synthetic
transport or DTO check is substituted for required current live HTTP/SQL/role/
critical/device evidence. Source, baseline/final receipts and Desktop next checks:
[Cloud C6 handoff](../HANDOFF/C6_MANUAL_CONTROLS_2026-11-06.md).
