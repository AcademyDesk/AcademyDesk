# BUG-PERF-0001 — Large operational lists have no consistent paging contract

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | STATIC-FINDING |
| Final verification | NOT RUN |
| Severity | Moderate scale risk |
| Priority | P2 |
| Category | PERF |
| Module | TEACHER |
| Role | See reproduction; same-tenant Admin unless stated otherwise |
| Screen / route | /teachers; /students; /payments; /communications |
| API | GET list APIs |
| Environment | Source review of local working tree; runtime production state not inferred |
| Device/viewport | NOT RUN; use QA/06_DEVICE_VIEWPORT_MATRIX.md where UI applies |
| Baseline | 20bb6047f9edf733ac8e2a226621cc582ec54b3c + pre-existing student UI diff |
| Discovery test / review ID | PERF-BASELINE-001 |
| Evidence classification | Static trace; runtime reproduction pending |
| Preconditions | Isolated synthetic fixture from QA/10_TEST_DATA_STRATEGY.md; never customer data |
| Reproduction frequency | Not measured; reproduction instructions are proposed |
| Source | apps/api/Controllers/TeachersController.cs:21 |
| Class/function | List; similar students/payments/notifications endpoints |
| Related source | INVENTORY/API_ENDPOINTS.md, CONTRACTS.md, FORMS_AND_FIELDS.md and ASYNC_FORM_RISKS.md |
| Root-cause confidence | MEDIUM |
| Evidence location | Source excerpt below; source fingerprint in INVENTORY/SOURCE_MANIFEST.md |
| Screenshot | Not captured on pinned baseline |
| Console logs | Not captured; required in retest |
| API request | In performance-only disposable SQL seed progressively larger permitted datasets; measure response bytes, latency, query count and browser rendering. |
| API response | Not captured for this issue; use synthetic request/response in isolated reproduction |
| Database before/after | Not executed; fixture and fresh-context assertions defined below |
| Dependencies | Safe SQL/HTTP/browser harness as applicable; desired policy review where noted |
| Fix commit | Not implemented (Phase 1) |
| Retest result | NOT RUN |
| Regression result | NOT RUN |
| Closure notes | Remain OPEN; follow closure requirements in QA README |

## Exact reproduction

In performance-only disposable SQL seed progressively larger permitted datasets; measure response bytes, latency, query count and browser rendering.

## Expected

Measured bounded resource use and usable large lists; thresholds BASELINE REQUIRED.

## Actual / evidence

Several list actions load all tenant rows. No observed latency failure or numeric threshold claimed.

Source snapshot:

```text
20:             .Select(x => new TeacherSummary(x.Id, x.FirstName, x.LastName, x.Email, x.Phone, x.Specialties, x.BranchId, x.IsActive))
21:             .ToListAsync(cancellationToken);
22:         return Ok(teachers);
23:     }
```

## Suspected root cause

Unbounded list contracts and client-side rendering; performance impact unmeasured.

## Business impact and blast radius

Large tenants and mobile memory/network use.

## Related / required regression

PERF-BASELINE-001: Parameterized scale benchmark with SQL query telemetry; preserve correctness while paging later.

Also run all endpoint/form cases pointing to this issue in QA/03_TEST_MATRIX.md and the critical regression suite before closure.
