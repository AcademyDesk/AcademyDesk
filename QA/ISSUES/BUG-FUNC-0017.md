# BUG-FUNC-0017 — Waitlist-only batches reject waitlisted enrollment requests

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | STATIC-FINDING |
| Final verification | NOT RUN |
| Severity | Major enrollment configuration inconsistency |
| Priority | P1 |
| Category | FUNC |
| Module | BATCH |
| Role | See reproduction; same-tenant Admin unless stated otherwise |
| Screen / route | /enrollments; direct API |
| API | POST enrollments |
| Environment | Source review of local working tree; runtime production state not inferred |
| Device/viewport | NOT RUN; use QA/06_DEVICE_VIEWPORT_MATRIX.md where UI applies |
| Baseline | 20bb6047f9edf733ac8e2a226621cc582ec54b3c + pre-existing student UI diff |
| Discovery test / review ID | BATCH-WAITLIST-001 |
| Evidence classification | Static trace; runtime reproduction pending |
| Preconditions | Isolated synthetic fixture from QA/10_TEST_DATA_STRATEGY.md; never customer data |
| Reproduction frequency | Not measured; reproduction instructions are proposed |
| Source | apps/api/Controllers/EnrollmentsController.cs:31 |
| Class/function | Create / Batches.Validate |
| Related source | INVENTORY/API_ENDPOINTS.md, CONTRACTS.md, FORMS_AND_FIELDS.md and ASYNC_FORM_RISKS.md |
| Root-cause confidence | HIGH |
| Evidence location | Source excerpt below; source fingerprint in INVENTORY/SOURCE_MANIFEST.md |
| Screenshot | Not captured on pinned baseline |
| Console logs | Not captured; required in retest |
| API request | Set an active synthetic batch EnrollmentStatus to accepted value Waitlist and positive available WaitlistCapacity. Submit a valid student with requested status Waitlisted and satisfied prerequisites. Compare Open configuration. |
| API response | Not captured for this issue; use synthetic request/response in isolated reproduction |
| Database before/after | Not executed; fixture and fresh-context assertions defined below |
| Dependencies | Safe SQL/HTTP/browser harness as applicable; desired policy review where noted |
| Fix commit | Not implemented (Phase 1) |
| Retest result | NOT RUN |
| Regression result | NOT RUN |
| Closure notes | Remain OPEN; follow closure requirements in QA README |

## Exact reproduction

Set an active synthetic batch EnrollmentStatus to accepted value Waitlist and positive available WaitlistCapacity. Submit a valid student with requested status Waitlisted and satisfied prerequisites. Compare Open configuration.

## Expected

A supported waitlist-only configuration accepts eligible waitlisted requests while denying active enrollment, or is explicitly removed from the supported contract.

## Actual / evidence

Batches accepts Waitlist but Enrollments.Create rejects every batch not Open before branching on requested enrollment status. Waitlist enrollment works only with an Open batch. Runtime NOT RUN.

Source snapshot:

```text
30:         }
31:         if (!batch.IsActive || !string.Equals(batch.EnrollmentStatus, "Open", StringComparison.OrdinalIgnoreCase)) return BadRequest(new { message = "This batch is not open for enrolment." });
32:         var requestedStatus = string.Equals(request.Status, "Waitlisted", StringComparison.OrdinalIgnoreCase) ? "Waitlisted" : "Active";
33:         var activeCount = await dbContext.Enrollments.CountAsync(x => x.AcademyId == academyId && x.BatchId == request.BatchId && x.Status == "Active", cancellationToken);
```

## Suspected root cause

Batch configuration enum and enrollment eligibility checks disagree.

## Business impact and blast radius

API-configured waitlist-only batches; current batch setup does not expose its enrollmentStatus state as a control.

## Related / required regression

BATCH-WAITLIST-001: Open/Waitlist/Closed crossed with active flag, Active/Waitlisted request, capacity0/full/available and prerequisite controls; assert no unintended active admission.

Also run all endpoint/form cases pointing to this issue in QA/03_TEST_MATRIX.md and the critical regression suite before closure.
