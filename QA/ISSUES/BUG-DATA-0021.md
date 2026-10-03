# BUG-DATA-0021 — Student update accepts branch references without academy validation

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | STATIC-FINDING |
| Final verification | NOT RUN |
| Severity | Major tenant relationship integrity |
| Priority | P1 |
| Category | DATA |
| Module | STUDENT |
| Role | See reproduction; same-tenant Admin unless stated otherwise |
| Screen / route | Direct student update API |
| API | PUT students/{studentId} |
| Environment | Source review of local working tree; runtime production state not inferred |
| Device/viewport | NOT RUN; use QA/06_DEVICE_VIEWPORT_MATRIX.md where UI applies |
| Baseline | 20bb6047f9edf733ac8e2a226621cc582ec54b3c + pre-existing student UI diff |
| Discovery test / review ID | STUDENT-BRANCH-002 |
| Evidence classification | Static trace; runtime reproduction pending |
| Preconditions | Isolated synthetic fixture from QA/10_TEST_DATA_STRATEGY.md; never customer data |
| Reproduction frequency | Not measured; reproduction instructions are proposed |
| Source | apps/api/Controllers/StudentsController.cs:79 |
| Class/function | Update |
| Related source | INVENTORY/API_ENDPOINTS.md, CONTRACTS.md, FORMS_AND_FIELDS.md and ASYNC_FORM_RISKS.md |
| Root-cause confidence | HIGH |
| Evidence location | Source excerpt below; source fingerprint in INVENTORY/SOURCE_MANIFEST.md |
| Screenshot | Not captured on pinned baseline |
| Console logs | Not captured; required in retest |
| API request | As an authorized academy A administrator update an A student using branch B from academy B. Compare a valid A branch, null branch and nonexistent GUID; read fresh student after each request in disposable fixtures. |
| API response | Not captured for this issue; use synthetic request/response in isolated reproduction |
| Database before/after | Not executed; fixture and fresh-context assertions defined below |
| Dependencies | Safe SQL/HTTP/browser harness as applicable; desired policy review where noted |
| Fix commit | Not implemented (Phase 1) |
| Retest result | NOT RUN |
| Regression result | NOT RUN |
| Closure notes | Remain OPEN; follow closure requirements in QA README |

## Exact reproduction

As an authorized academy A administrator update an A student using branch B from academy B. Compare a valid A branch, null branch and nonexistent GUID; read fresh student after each request in disposable fixtures.

## Expected

Nonnull branch must exist within the route academy, matching Create validation; invalid references rejected before any student or Identity writes.

## Actual / evidence

Create checks branch existence and academy. Update checks only student ownership and required names, then assigns arbitrary BranchId. Student mapping defines no branch ownership constraint. Runtime SQL reproduction pending; no cross-tenant data read is claimed.

Source snapshot:

```text
78:         student.Phone = request.Phone?.Trim();
79:         student.BranchId = request.BranchId;
80:         student.IsActive = request.IsActive;
81:         await dbContext.SaveChangesAsync(token);
```

## Suspected root cause

Reference validation present in Create is missing from Update.

## Business impact and blast radius

Authorized student update callers can corrupt branch relationships of their own academy students; arbitrary GUID binding is not itself authorization to read another academy.

## Related / required regression

STUDENT-BRANCH-002: Real HTTP/SQL two-tenant and nonexistent branch matrix with fresh no-write assertions for student and linked identities; retain valid same-academy reassignment and optional-null behavior.

Also run all endpoint/form cases pointing to this issue in QA/03_TEST_MATRIX.md and the critical regression suite before closure.
