# BUG-API-0001 — Blank optional dates sent as empty JSON strings

2026-10-10 successor: [repair report](../REPORTS/PHASE_2B_ONBOARDING_DATE_REPAIR.md), teacher optional DOB/joining and student admission date serialized as null when blank; required student DOB unchanged. Captured form references also repair related post-await reset fault. Handler baseline5 PASS/4 FAIL, fixed9 PASS;32 synthetic responsive browser/23 real SQL/Identity/HTTP cases PASS,88/7 migrations/owned cleanup. Remains OPEN for linked browser-to-SQL, physical devices, all-optional-fields/full onboarding and critical acceptance.

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | Handler-reproduced; HTTP binding verified; bounded repair |
| Final verification | 9 handler/32 synthetic browser/23 native SQL HTTP PASS; remaining gates OPEN |
| Severity | Major |
| Priority | P1 |
| Category | API |
| Module | TEACHER |
| Role | See reproduction; same-tenant Admin unless stated otherwise |
| Screen / route | /teacher-onboarding; /student-onboarding |
| API | POST teachers / student-onboarding |
| Environment | Source review of local working tree; runtime production state not inferred |
| Device/viewport | NOT RUN; use QA/06_DEVICE_VIEWPORT_MATRIX.md where UI applies |
| Baseline | 20bb6047f9edf733ac8e2a226621cc582ec54b3c + pre-existing student UI diff |
| Discovery test / review ID | FORM-OPTIONAL-001 |
| Evidence classification | Original static trace plus linked handler/browser and native SQL HTTP evidence |
| Preconditions | Isolated synthetic fixture from QA/10_TEST_DATA_STRATEGY.md; never customer data |
| Reproduction frequency | Not measured; reproduction instructions are proposed |
| Source | apps/web/src/app/teacher-onboarding/page.tsx:55 |
| Class/function | submit |
| Related source | INVENTORY/API_ENDPOINTS.md, CONTRACTS.md, FORMS_AND_FIELDS.md and ASYNC_FORM_RISKS.md |
| Root-cause confidence | HIGH |
| Evidence location | Source excerpt below; source fingerprint in INVENTORY/SOURCE_MANIFEST.md |
| Screenshot | Not captured on pinned baseline |
| Console logs | Not captured; required in retest |
| API request | Leave Date of birth and Joining date blank on teacher intake; submit required details. On student intake supply required DOB but leave Admission date blank. Inspect payload and model-binding response in test host. |
| API response | Not captured for this issue; use synthetic request/response in isolated reproduction |
| Database before/after | Not executed; fixture and fresh-context assertions defined below |
| Dependencies | Safe SQL/HTTP/browser harness as applicable; desired policy review where noted |
| Fix commit | See successor report and feature history; optional-date normalization/form capture |
| Retest result | 32 synthetic responsive browser and23 native SQL HTTP cases PASS |
| Regression result | Required DOB/valid dates/malformed errors and denied no-write controls retained; full critical suite OPEN |
| Closure notes | Remain OPEN; follow closure requirements in QA README |

## Exact reproduction

Leave Date of birth and Joining date blank on teacher intake; submit required details. On student intake supply required DOB but leave Admission date blank. Inspect payload and model-binding response in test host.

## Expected

Optional blank dates remain optional and bind as null/omitted; invalid dates show field-specific errors.

## Actual / evidence

Hidden date inputs yield empty strings. Object.fromEntries forwards them unchanged; server contracts use DateOnly?. Standard JSON model binding is expected to reject empty date strings. Runtime HTTP reproduction pending.

Source snapshot:

```text
54:       .filter((row) => row.available);
55:     const body = Object.fromEntries(form) as Record<string, FormDataEntryValue>;
56:     body.specialties = subjectEntries.map(({ subject }) => subject).join(", ");
57:     body.availabilityJson = JSON.stringify(availability);
```

## Suspected root cause

FormData string serialization lacks nullable-date normalization; the DTO being nullable does not mean empty-string JSON is a date.

## Business impact and blast radius

Both onboarding flows and other unnormalized hidden date controls; scan field/contract catalog.

## Related / required regression

FORM-OPTIONAL-001: Full HTTP tests for omitted/null/empty/valid/malformed DateOnly and browser submit with all optional inputs empty.

Also run all endpoint/form cases pointing to this issue in QA/03_TEST_MATRIX.md and the critical regression suite before closure.
