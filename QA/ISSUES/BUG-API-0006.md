# BUG-API-0006 — Null availability entries persist and break teacher profile reads

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | STATIC-FINDING |
| Final verification | NOT RUN |
| Severity | Major profile availability and ambiguous save failure |
| Priority | P1 |
| Category | API |
| Module | PROFILE |
| Role | See reproduction; same-tenant Admin unless stated otherwise |
| Screen / route | /teacher-profile; direct profile API |
| API | PUT/GET teachers/{teacherId}/profile |
| Environment | Source review of local working tree; runtime production state not inferred |
| Device/viewport | NOT RUN; use QA/06_DEVICE_VIEWPORT_MATRIX.md where UI applies |
| Baseline | 20bb6047f9edf733ac8e2a226621cc582ec54b3c + pre-existing student UI diff |
| Discovery test / review ID | TEACHER-AVAILABILITY-001 |
| Evidence classification | Static trace; runtime reproduction pending |
| Preconditions | Isolated synthetic fixture from QA/10_TEST_DATA_STRATEGY.md; never customer data |
| Reproduction frequency | Not measured; reproduction instructions are proposed |
| Source | apps/api/Controllers/ProfilesController.cs:183 |
| Class/function | UpdateTeacherProfile / Teacher / ReadAvailability |
| Related source | INVENTORY/API_ENDPOINTS.md, CONTRACTS.md, FORMS_AND_FIELDS.md and ASYNC_FORM_RISKS.md |
| Root-cause confidence | HIGH |
| Evidence location | Source excerpt below; source fingerprint in INVENTORY/SOURCE_MANIFEST.md |
| Screenshot | Not captured on pinned baseline |
| Console logs | Not captured; required in retest |
| API request | In isolated fixtures as an authorized administrator PUT valid teacher metadata with availabilityJson string [null]. Inspect response and fresh stored row, then GET profile again. Compare null string, empty array and valid day-object controls; never mutate customer data. |
| API response | Not captured for this issue; use synthetic request/response in isolated reproduction |
| Database before/after | Not executed; fixture and fresh-context assertions defined below |
| Dependencies | Safe SQL/HTTP/browser harness as applicable; desired policy review where noted |
| Fix commit | Not implemented (Phase 1) |
| Retest result | NOT RUN |
| Regression result | NOT RUN |
| Closure notes | Remain OPEN; follow closure requirements in QA README |

## Exact reproduction

In isolated fixtures as an authorized administrator PUT valid teacher metadata with availabilityJson string [null]. Inspect response and fresh stored row, then GET profile again. Compare null string, empty array and valid day-object controls; never mutate customer data.

## Expected

Invalid availability entries are rejected before persistence or handled safely; saved optional metadata must not make the profile unreadable.

## Actual / evidence

Update saves AvailabilityJson before returning Teacher projection. Deserialized list can contain null elements; predicate dereferences slot.Day and catches only JsonException. NullReferenceException escapes after save and on later GET. Static finding; runtime reproduction pending. The current day editor emits objects, so a direct API or existing malformed record is needed.

Source snapshot:

```text
182:             return (JsonSerializer.Deserialize<List<TeacherAvailabilitySummary>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [])
183:                 .Where(slot => !string.IsNullOrWhiteSpace(slot.Day))
184:                 .ToList();
185:         }
```

## Suspected root cause

Unchecked nested JSON is stored before a reader that assumes every list item is nonnull.

## Business impact and blast radius

Affected teacher administrative profile read/save, including metadata committed before error; teacher intake also accepts AvailabilityJson strings. This does not establish the cause of historical Azure errors.

## Related / required regression

TEACHER-AVAILABILITY-001: Real HTTP/SQL valid/null/empty/malformed/object/scalar/null-element/mixed-list matrix. Assert safe rejection with no writes or successful safe normalization per approved contract; fresh GET must stay readable, response and DB must agree, and failed saves must not show success.

Also run all endpoint/form cases pointing to this issue in QA/03_TEST_MATRIX.md and the critical regression suite before closure.
