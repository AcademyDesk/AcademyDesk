# BUG-API-0007 — Null batch teaching-time entries escape validation as server errors

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | RUNTIME-REPRODUCED — paired bounded local repair verified; OPEN |
| Final verification | 277 API /116 controlled frontend /two fresh Identity-HTTP-SQL runs x48 PASS; browser/device/all-linked/critical NOT RUN |
| Severity | Major invalid-input server failure |
| Priority | P1 |
| Category | API |
| Module | BATCH |
| Role | See reproduction; same-tenant Admin unless stated otherwise |
| Screen / route | /batch-setup; direct API |
| API | POST/PUT batches |
| Environment | Isolated local owned SQL2022/Testing; real middleware/Identity/model binding; production state not inferred |
| Device/viewport | NOT RUN; use QA/06_DEVICE_VIEWPORT_MATRIX.md where UI applies |
| Baseline | 20bb6047f9edf733ac8e2a226621cc582ec54b3c + pre-existing student UI diff |
| Discovery test / review ID | BATCH-NULL-001 |
| Evidence classification | Accepted static trace + real HTTP/SQL baseline and bounded local repair, not production/browser acceptance |
| Preconditions | Isolated synthetic fixture from QA/10_TEST_DATA_STRATEGY.md; never customer data |
| Reproduction frequency | One fresh baseline run; two fresh final-source SQL runs pass all48 cases each |
| Source | apps/api/Controllers/BatchesController.cs:60 |
| Class/function | Validate |
| Related source | INVENTORY/API_ENDPOINTS.md, CONTRACTS.md, FORMS_AND_FIELDS.md and ASYNC_FORM_RISKS.md |
| Root-cause confidence | HIGH |
| Evidence location | [Batch teaching-time repair](../REPORTS/PHASE_2B_BATCH_TIMES_REPAIR.md), sanitized logs and exact source/assembly snapshots |
| Screenshot | Not captured on pinned baseline |
| Console logs | Not captured; required in retest |
| API request | Send an otherwise valid synthetic batch request with MeetingDaysJson equal to [null], then mixed valid and null entries. Compare malformed JSON, null whole JSON and empty list. |
| API response | Baseline casing400/null-entry500; final valid201/200, invalid400 structured message; role403/anonymous401 |
| Database before/after | Fresh SQL schedule string equality in row/response/GET; rejected requests preserve captured batch/session/governance/task/audit state |
| Dependencies | Safe SQL/HTTP/browser harness as applicable; desired policy review where noted |
| Fix commit | Uncommitted local bounded repair; no push/deployment |
| Retest result | PARTIAL PASS — selected POST/PUT cases twice x48 with real Identity/SQL; actual browser NOT RUN |
| Regression result | 277 API /116 controlled frontend PASS; build/TypeScript PASS; existing page lint FAIL unchanged (2 errors/3 warnings) |
| Closure notes | Remain OPEN; follow closure requirements in QA README |

## Exact reproduction

Send an otherwise valid synthetic batch request with MeetingDaysJson equal to [null], then mixed valid and null entries. Compare malformed JSON, null whole JSON and empty list.

## Expected

Invalid nested entries receive a structured validation response before any domain write; omitted optional JSON follows its documented policy.

## Actual / evidence

2026-10-01 current bounded repair: [report](../REPORTS/PHASE_2B_BATCH_TIMES_REPAIR.md). A fresh real HTTP/SQL baseline reproduces camel/mixed nested keys400 and null/mixed-null entries500 for both POST/PUT, without captured writes. Final sources accept Pascal/camel/mixed keys, reject null/invalid entries with sanitized400, and preserve raw valid JSON through row/response/GET. Optional missing/null/blank JSON remains supported; the form now sends null when no days are selected. Explicit empty arrays remain invalid. Two fresh runs x48 PASS, API277 and controlled frontend116 PASS. No browser/device/critical closure and no claim about the historical Teacher error.

### Historical audit and earlier observation (superseded by the result above)

Deserialization permits a null list entry; predicate dereferences x.Day. Catch handles JsonException only, so NullReferenceException escapes before Apply/SaveChanges. This path does not prove a saved batch or the historical production teacher null-error cause. Runtime NOT RUN.

Source snapshot:

```text
59:                 var validDays = new[] { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" };
60:                 if (sessions is null || sessions.Count == 0 || sessions.Any(x => !validDays.Contains(x.Day, StringComparer.OrdinalIgnoreCase) || !TimeOnly.TryParse(x.StartTime, out _)))
61:                     return "Select a valid class time for every teaching day.";
62:             }
```

## Suspected root cause

Nested element null guard is absent from shape validation.

## Business impact and blast radius

Batch API validation and generic failure feedback; distinct from post-commit audit/reset errors.

## Related / required regression

BATCH-NULL-001: Real HTTP malformed/object/scalar/null/empty/mixed-element matrix and fresh DB no-write assertions, including authorized request control and error sanitization.

Also run all endpoint/form cases pointing to this issue in QA/03_TEST_MATRIX.md and the critical regression suite before closure.
