# BUG-FUNC-0016 — Enrollment status selector omits the required lifecycle reason

2026-10-10 feedback-only successor: [Enrollment/Promotion report](../REPORTS/PHASE_2B_ENROLLMENT_PROMOTION_FEEDBACK_REPAIR.md) now displays backend validation messages, retains draft and guards duplicate writes. The status/endDate payload deliberately remains unchanged; LifecycleReason input/integration is still missing. This issue remains OPEN and is not closed by synthetic successful status-response feedback cases. Next bounded domain-linked UI reason/confirmation and real SQL/browser verification; do not relax the existing server requirement.

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | STATIC-FINDING |
| Final verification | NOT RUN |
| Severity | Major workflow blocked |
| Priority | P1 |
| Category | FUNC |
| Module | BATCH |
| Role | See reproduction; same-tenant Admin unless stated otherwise |
| Screen / route | /enrollments |
| API | PUT enrollments/{enrollmentId} |
| Environment | Source review of local working tree; runtime production state not inferred |
| Device/viewport | NOT RUN; use QA/06_DEVICE_VIEWPORT_MATRIX.md where UI applies |
| Baseline | 20bb6047f9edf733ac8e2a226621cc582ec54b3c + pre-existing student UI diff |
| Discovery test / review ID | BATCH-LIFECYCLE-001 |
| Evidence classification | Static trace; runtime reproduction pending |
| Preconditions | Isolated synthetic fixture from QA/10_TEST_DATA_STRATEGY.md; never customer data |
| Reproduction frequency | Not measured; reproduction instructions are proposed |
| Source | apps/web/src/app/enrollments/page.tsx:51 |
| Class/function | updateStatus / Enrollments.Update |
| Related source | INVENTORY/API_ENDPOINTS.md, CONTRACTS.md, FORMS_AND_FIELDS.md and ASYNC_FORM_RISKS.md |
| Root-cause confidence | HIGH |
| Evidence location | Source excerpt below; source fingerprint in INVENTORY/SOURCE_MANIFEST.md |
| Screenshot | Not captured on pinned baseline |
| Console logs | Not captured; required in retest |
| API request | Select Paused, Completed, Waitlisted, Withdrawn or Cancelled for an active synthetic enrollment using the current status dropdown. |
| API response | Not captured for this issue; use synthetic request/response in isolated reproduction |
| Database before/after | Not executed; fixture and fresh-context assertions defined below |
| Dependencies | Safe SQL/HTTP/browser harness as applicable; desired policy review where noted |
| Fix commit | Not implemented (Phase 1) |
| Retest result | NOT RUN |
| Regression result | NOT RUN |
| Closure notes | Remain OPEN; follow closure requirements in QA README |

## Exact reproduction

Select Paused, Completed, Waitlisted, Withdrawn or Cancelled for an active synthetic enrollment using the current status dropdown.

## Expected

UI collects required reason and successfully submits an authorized valid transition, retaining data and explaining rejection otherwise.

## Actual / evidence

UI sends status/endDate only, with no reason input. API requires LifecycleReason for every requested non-Active status, so these choices return400. UI replaces the useful reason with a generic failure. Runtime NOT RUN.

Source snapshot:

```text
50:   }
51:   async function updateStatus(enrollment: Enrollment, status: string) { if (!academy) return; setSavingId(enrollment.id); const response = await academyApi(`/api/academies/${academy.id}/enrollments/${enrollment.id}`, { method: "PUT", headers: apiHeaders(true), body: JSON.stringify({ status, endDate: status === "Active" ? null : enrollment.endDate }) }); setSavingId(null); if (!response.ok) return setMessage("The enrolment status could not be updated."); setMessage("Enrolment updated."); await load(); }
52:
53:   const studentName = (id: string) => { const student = students.find((item) => item.id === id); return student ? `${student.firstName} ${student.lastName}` : "Unknown student"; };
```

## Suspected root cause

Frontend status-change contract was not updated alongside backend lifecycle validation.

## Business impact and blast radius

All non-Active enrollment updates from this page; direct API with a reason is a control, not a UI fix.

## Related / required regression

BATCH-LIFECYCLE-001: Browser each offered status, required/whitespace reason, cancel, stale record, server denial, saved readback and durable feedback; real HTTP no-write for missing reason.

Also run all endpoint/form cases pointing to this issue in QA/03_TEST_MATRIX.md and the critical regression suite before closure.
