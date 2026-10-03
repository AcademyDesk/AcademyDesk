# BUG-DATA-0007 — Student onboarding account-created flags can report an account that was not created

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | STATIC-FINDING |
| Final verification | NOT RUN |
| Severity | Major response integrity |
| Priority | P1 |
| Category | DATA |
| Module | STUDENT |
| Role | See reproduction; same-tenant Admin unless stated otherwise |
| Screen / route | /student-onboarding |
| API | POST student-onboarding |
| Environment | Source review of local working tree; runtime production state not inferred |
| Device/viewport | NOT RUN; use QA/06_DEVICE_VIEWPORT_MATRIX.md where UI applies |
| Baseline | 20bb6047f9edf733ac8e2a226621cc582ec54b3c + pre-existing student UI diff |
| Discovery test / review ID | STUDENT-API-001 |
| Evidence classification | Static trace; runtime reproduction pending |
| Preconditions | Isolated synthetic fixture from QA/10_TEST_DATA_STRATEGY.md; never customer data |
| Reproduction frequency | Not measured; reproduction instructions are proposed |
| Source | apps/api/Controllers/StudentOnboardingController.cs:49 |
| Class/function | Create |
| Related source | INVENTORY/API_ENDPOINTS.md, CONTRACTS.md, FORMS_AND_FIELDS.md and ASYNC_FORM_RISKS.md |
| Root-cause confidence | HIGH |
| Evidence location | Source excerpt below; source fingerprint in INVENTORY/SOURCE_MANIFEST.md |
| Screenshot | Not captured on pinned baseline |
| Console logs | Not captured; required in retest |
| API request | Submit an adult synthetic student with username but no temporary password. Read response flags and query Identity store. |
| API response | Not captured for this issue; use synthetic request/response in isolated reproduction |
| Database before/after | Not executed; fixture and fresh-context assertions defined below |
| Dependencies | Safe SQL/HTTP/browser harness as applicable; desired policy review where noted |
| Fix commit | Not implemented (Phase 1) |
| Retest result | NOT RUN |
| Regression result | NOT RUN |
| Closure notes | Remain OPEN; follow closure requirements in QA README |

## Exact reproduction

Submit an adult synthetic student with username but no temporary password. Read response flags and query Identity store.

## Expected

Response truthfully states no account exists, or rejects an incomplete account request with field error.

## Actual / evidence

Account creation requires username AND password; returned flags only check username (and parent presence).

Source snapshot:

```text
48:             await transaction.CommitAsync(token);
49:             return Ok(new { student.Id, student.FirstName, student.LastName, IsMinor = isMinor, ParentId = parent?.Id, StudentAccountCreated = !string.IsNullOrWhiteSpace(request.StudentUserName), ParentAccountCreated = parent is not null && !string.IsNullOrWhiteSpace(request.ParentUserName) });
50:         }
51:         catch (DbUpdateException)
```

## Suspected root cause

Response computes intent instead of confirmed creation result.

## Business impact and blast radius

User thinks portal access is ready although no account exists.

## Related / required regression

STUDENT-API-001: Neither/username-only/password-only/both supplied; assert actual Identity row/role and exact JSON flags.

Also run all endpoint/form cases pointing to this issue in QA/03_TEST_MATRIX.md and the critical regression suite before closure.
