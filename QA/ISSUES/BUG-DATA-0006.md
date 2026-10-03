# BUG-DATA-0006 — Enrollment activation, transfer and promotion bypass admission invariants

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | STATIC-FINDING |
| Final verification | NOT RUN |
| Severity | Major enrollment integrity |
| Priority | P1 |
| Category | DATA |
| Module | BATCH |
| Role | See reproduction; same-tenant Admin unless stated otherwise |
| Screen / route | /enrollments; /batch-promotions |
| API | PUT enrollments/{id}; POST enrollments/{id}/transfer; PATCH batch-promotions/{id}/decision |
| Environment | Source review of local working tree; runtime production state not inferred |
| Device/viewport | NOT RUN; use QA/06_DEVICE_VIEWPORT_MATRIX.md where UI applies |
| Baseline | 20bb6047f9edf733ac8e2a226621cc582ec54b3c + pre-existing student UI diff |
| Discovery test / review ID | BATCH-RULE-001 |
| Evidence classification | Static trace; runtime reproduction pending |
| Preconditions | Isolated synthetic fixture from QA/10_TEST_DATA_STRATEGY.md; never customer data |
| Reproduction frequency | Not measured; reproduction instructions are proposed |
| Source | apps/api/Controllers/EnrollmentsController.cs:50 |
| Class/function | Enrollments.Update/Transfer / BatchPromotions.Decide |
| Related source | INVENTORY/API_ENDPOINTS.md, CONTRACTS.md, FORMS_AND_FIELDS.md and ASYNC_FORM_RISKS.md |
| Root-cause confidence | HIGH |
| Evidence location | Source excerpt below; source fingerprint in INVENTORY/SOURCE_MANIFEST.md |
| Screenshot | Not captured on pinned baseline |
| Console logs | Not captured; required in retest |
| API request | Activate a waitlisted enrollment in a full batch via Update. Transfer to a batch whose prerequisites are unmet or where the student already has enrollment. |
| API response | Not captured for this issue; use synthetic request/response in isolated reproduction |
| Database before/after | Not executed; fixture and fresh-context assertions defined below |
| Dependencies | Safe SQL/HTTP/browser harness as applicable; desired policy review where noted |
| Fix commit | Not implemented (Phase 1) |
| Retest result | NOT RUN |
| Regression result | NOT RUN |
| Closure notes | Remain OPEN; follow closure requirements in QA README |

## Exact reproduction

Activate a waitlisted enrollment in a full batch via Update. Transfer to a batch whose prerequisites are unmet or where the student already has enrollment.

## Expected

All paths into active enrollment enforce agreed capacity, duplicate and prerequisite rules.

## Actual / evidence

Create checks those rules; Update only validates status/reason; Transfer checks capacity/open state but not prerequisite/duplicate parity. Promotion approval inserts Active target enrollment without checking target active/open state, capacity, prerequisites or existing target enrollment. Creation itself also permits repeated Waitlisted rows because its duplicate check only checks Active.

Source snapshot:

```text
49:         if (!string.Equals(status, "Active", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(request.LifecycleReason)) return BadRequest(new { message = "A lifecycle reason is required when changing an enrolment from Active." });
50:         enrollment.Status = status; enrollment.EndDate = request.EndDate; enrollment.LifecycleReason = string.IsNullOrWhiteSpace(request.LifecycleReason) ? null : request.LifecycleReason.Trim();
51:         await dbContext.SaveChangesAsync(token);
52:         return Ok(new EnrollmentSummary(enrollment.Id, enrollment.StudentId, enrollment.BatchId, enrollment.StartDate, enrollment.EndDate, enrollment.Status));
```

## Suspected root cause

Invariants implemented only in create path.

## Business impact and blast radius

Roster capacity, learning prerequisites, duplicated billing/session participation.

## Related / required regression

BATCH-RULE-001: Transition matrix plus concurrent creation/activation/transfer/promotion SQL tests; no duplicate active/waitlisted enrollment and all approved admission guards on every path.

Also run all endpoint/form cases pointing to this issue in QA/03_TEST_MATRIX.md and the critical regression suite before closure.
