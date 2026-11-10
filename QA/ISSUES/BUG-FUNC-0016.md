# BUG-FUNC-0016 — Enrollment status selector omits the required lifecycle reason

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
| Fix commit | `56555fa5946dedc0b28f166d13070ef141cf4407` C32 client return; no deployment |
| Retest result | 38 controlled TSX and40 sandboxed Chromium cases PASS;20 new actual HTTP/Identity/InMemory contract cases in the full API run;native SQL/device acceptance OPEN |
| Regression result | Full frontend2,416 PASS/3 existing missing-SQL-receipt failures;full API2,451 PASS with Desktop241810e preserved;exact source/limits in C32 handoff |
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

## C32 lifecycle review and recovery — 10 November 2026

Historical evidence above is retained. Choosing a different status now opens a
review of the learner, batch, reviewed and requested statuses. No write is sent
until confirmation; every non-Active target requires a trimmed nonblank reason.
Active retains the existing contract of clearing end date and lifecycle reason;
other targets preserve the existing end date. Cancel, same/invalid status and
stale UI callbacks do not submit. A fresh GET compares the reviewed enrollment
before PUT; a changed/missing record requires a fresh review. The existing API
still owns permissions, tenant and admission/capacity/prerequisite rules. This
client read does not add an atomic optimistic-concurrency contract to the server.

Creation and status changes share an immediate duplicate-intent guard and finally
cleanup. Confirmed receipts and failed readback are distinct from uncertain
transport/5xx/malformed acknowledgements. Drafts survive failure; recovery only
reads, then requires explicit review/new intent after an uncertain result. Failed
or malformed lookups no longer appear as an authoritative empty register. Server
refusals retain useful details, successful notices survive readback, and late
results cannot update an unmounted page.

The original actual-TSX suite recorded0/34 PASS before repair; the final successor
passes38/38. Chromium passes40/40 at320/1440 with synthetic API persistence/faults,
including all6 statuses, cancel/blank reason, stale source, duplicate confirmation,
lost-before/lost-after response, bad receipt, denied/preflight/readback failures
and Active/Waitlisted creation with real calendar interaction. Actual HTTP uses
Identity and InMemory; it does not replace real SQL transaction/race acceptance.

[Source pins, evidence, current combined result and ordered native tasks](../HANDOFF/C32_ENROLLMENT_LIFECYCLE_2026-11-10.md).
Remain OPEN for actual SQL/browser/device, linked critical and release acceptance.
Delegated lookup access in0018 remains a separate task; no permission was expanded.
