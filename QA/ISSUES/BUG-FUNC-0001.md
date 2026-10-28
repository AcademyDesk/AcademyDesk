# BUG-FUNC-0001 — Async form reset throws after successful persistence

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | STATIC-FINDING |
| Final verification | NOT RUN |
| Severity | Major |
| Priority | P1 |
| Category | FUNC |
| Module | TEACHER |
| Role | See reproduction; same-tenant Admin unless stated otherwise |
| Screen / route | /teacher-onboarding; /student-onboarding; see ASYNC_FORM_RISKS.md |
| API | POST /api/academies/{academyId}/teachers; other form writes |
| Environment | Source review of local working tree; runtime production state not inferred |
| Device/viewport | NOT RUN; use QA/06_DEVICE_VIEWPORT_MATRIX.md where UI applies |
| Baseline | 20bb6047f9edf733ac8e2a226621cc582ec54b3c + pre-existing student UI diff |
| Discovery test / review ID | TEACHER-FORM-001 |
| Evidence classification | Static trace; runtime reproduction pending |
| Preconditions | Isolated synthetic fixture from QA/10_TEST_DATA_STRATEGY.md; never customer data |
| Reproduction frequency | Not measured; reproduction instructions are proposed |
| Source | apps/web/src/app/teacher-onboarding/page.tsx:68 |
| Class/function | submit (and 20 other source occurrences) |
| Related source | INVENTORY/API_ENDPOINTS.md, CONTRACTS.md, FORMS_AND_FIELDS.md and ASYNC_FORM_RISKS.md |
| Root-cause confidence | HIGH |
| Evidence location | Source excerpt below; source fingerprint in INVENTORY/SOURCE_MANIFEST.md |
| Screenshot | Not captured on pinned baseline |
| Console logs | Not captured; required in retest |
| API request | In an isolated tenant, enter valid teacher names, email, subject and valid optional dates. Submit and allow the API promise to resolve. Observe the notice and console, then reload the teacher directory. Repeat each affected handler. |
| API response | Not captured for this issue; use synthetic request/response in isolated reproduction |
| Database before/after | Not executed; fixture and fresh-context assertions defined below |
| Dependencies | Safe SQL/HTTP/browser harness as applicable; desired policy review where noted |
| Fix commit | Not implemented (Phase 1) |
| Retest result | NOT RUN |
| Regression result | NOT RUN |
| Closure notes | Remain OPEN; follow closure requirements in QA README |

## Exact reproduction

In an isolated tenant, enter valid teacher names, email, subject and valid optional dates. Submit and allow the API promise to resolve. Observe the notice and console, then reload the teacher directory. Repeat each affected handler.

## Expected

Exactly one record persists; a success notice is visible and the form resets/returns without throwing.

## Actual / evidence

Source calls event.currentTarget.reset() after await; currentTarget is scoped to event dispatch. User reports persisted teacher with null error and no success. Browser reproduction on this baseline remains NOT RUN.

Source snapshot:

```text
67:       if (!response.ok) throw new Error(result?.message ?? "Teacher could not be created.");
68:       event.currentTarget.reset();
69:       setEmploymentType("Full-time");
70:       setSubjects([{ subject: "", certification: "" }]);
```

## Suspected root cause

React event currentTarget accessed after asynchronous suspension, before success message. Capturing the element before awaiting is a later repair candidate.

## Business impact and blast radius

21 reset sites across 13 source files; repeat attempts can create duplicates.

## Related / required regression

TEACHER-FORM-001: Use a deferred successful response, real React submit event, assert one request/record, no exception, visible success and reload persistence.

Also run all endpoint/form cases pointing to this issue in QA/03_TEST_MATRIX.md and the critical regression suite before closure.

## Cloud released-event-target slice — 2026-10-28

Controlled actual-TSX submissions reproduced six confirmed-write reset failures
with event.currentTarget released after dispatch. Seven handlers now capture the
form before awaiting: academic periods, access grants, sign-off, work queue,
support requests, teacher onboarding and student onboarding. All 14 accepted/
rejected submit cases pass: one synthetic write, success resets once, rejection
retains input; teacher success guidance and student navigation also pass.

No real HTTP/SQL persistence or real React browser submit event is claimed by
these controlled-hook tests. Other reset sites and success/readback behavior
remain open. See [cloud frontend report](../REPORTS/CLOUD_FRONTEND_QUALITY_2026-10-28.md).

## Remaining form recovery — cloud 2026-10-28

The nine remaining direct event-target reset sites now capture the form during
dispatch. Rejected platform support/invoice writes retain input; confirmed
platform/announcement/payroll writes retain success when their readback fails.
All 23 new controlled handler cases pass, including exact payroll payload checks.
The original 37 initial-load/form checks are retained. Full frontend: 963/966
passed, three failed for absent SQL evidence. Browser/SQL, broader notice behavior,
idempotency and critical gates remain OPEN. See
[form recovery report](../REPORTS/CLOUD_FORM_RECOVERY_2026-10-28.md).
