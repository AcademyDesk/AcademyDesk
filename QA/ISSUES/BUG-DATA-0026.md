# BUG-DATA-0026 — Lead stage selection reports conversion without creating a student

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | STATIC-FINDING |
| Final verification | NOT RUN |
| Severity | Major conversion reporting integrity |
| Priority | P1 |
| Category | DATA |
| Module | SALES |
| Role | See reproduction; same-tenant Admin unless stated otherwise |
| Screen / route | /leads; /sales-marketing?view=conversion |
| API | PATCH leads/{leadId}/stage |
| Environment | Source review of local working tree; runtime production state not inferred |
| Device/viewport | NOT RUN; use QA/06_DEVICE_VIEWPORT_MATRIX.md where UI applies |
| Baseline | 20bb6047f9edf733ac8e2a226621cc582ec54b3c + pre-existing student UI diff |
| Discovery test / review ID | SALES-CONVERSION-001 |
| Evidence classification | Static trace; runtime reproduction pending |
| Preconditions | Isolated synthetic fixture from QA/10_TEST_DATA_STRATEGY.md; never customer data |
| Reproduction frequency | Not measured; reproduction instructions are proposed |
| Source | apps/api/Controllers/LeadsController.cs:41 |
| Class/function | UpdateStage / sales-marketing converted aggregation |
| Related source | INVENTORY/API_ENDPOINTS.md, CONTRACTS.md, FORMS_AND_FIELDS.md and ASYNC_FORM_RISKS.md |
| Root-cause confidence | HIGH |
| Evidence location | Source excerpt below; source fingerprint in INVENTORY/SOURCE_MANIFEST.md |
| Screenshot | Not captured on pinned baseline |
| Console logs | Not captured; required in retest |
| API request | With an unconverted synthetic lead select Converted in the stage dropdown, without using Convert to student. Read lead, student table and conversion dashboard. |
| API response | Not captured for this issue; use synthetic request/response in isolated reproduction |
| Database before/after | Not executed; fixture and fresh-context assertions defined below |
| Dependencies | Safe SQL/HTTP/browser harness as applicable; desired policy review where noted |
| Fix commit | Not implemented (Phase 1) |
| Retest result | NOT RUN |
| Regression result | NOT RUN |
| Closure notes | Remain OPEN; follow closure requirements in QA README |

## Exact reproduction

With an unconverted synthetic lead select Converted in the stage dropdown, without using Convert to student. Read lead, student table and conversion dashboard.

## Expected

Converted implies a linked student created through the conversion workflow; stage-only updates cannot inflate student conversion counts.

## Actual / evidence

Converted is an accepted stage and is saved without creating a student or setting ConvertedStudentId. Dashboard counts either a linked student OR Converted stage as conversion. Runtime NOT RUN.

Source snapshot:

```text
40:         if (lead.ConvertedStudentId.HasValue) return Conflict(new { message = "A converted lead cannot be moved back through the pipeline." });
41:         lead.Stage = Stages.Single(x => x.Equals(request.Stage, StringComparison.OrdinalIgnoreCase));
42:         lead.FollowUpAtUtc = request.FollowUpAtUtc;
43:         await dbContext.SaveChangesAsync(cancellationToken);
```

## Suspected root cause

Conversion lifecycle invariant is absent from general stage mutation and reporting trusts the label.

## Business impact and blast radius

Conversion count/rate and lead status disagree with actual student records; actual conversion remains separately possible.

## Related / required regression

SALES-CONVERSION-001: Browser and real HTTP/SQL all-stage matrix, stage-only Converted rejection or approved atomic conversion, exact student/link counts, and dashboard numerator with legacy inconsistent rows.

Also run all endpoint/form cases pointing to this issue in QA/03_TEST_MATRIX.md and the critical regression suite before closure.
