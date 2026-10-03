# BUG-FUNC-0013 — Sales staff trial booking depends on a forbidden teacher lookup

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | STATIC-FINDING |
| Final verification | NOT RUN |
| Severity | Major authorized workflow blocked |
| Priority | P1 |
| Category | FUNC |
| Module | SALES |
| Role | See reproduction; same-tenant Admin unless stated otherwise |
| Screen / route | /trial-bookings |
| API | GET leads; GET teachers; GET sales-marketing/trials |
| Environment | Source review of local working tree; runtime production state not inferred |
| Device/viewport | NOT RUN; use QA/06_DEVICE_VIEWPORT_MATRIX.md where UI applies |
| Baseline | 20bb6047f9edf733ac8e2a226621cc582ec54b3c + pre-existing student UI diff |
| Discovery test / review ID | SALES-ACCESS-001 |
| Evidence classification | Static trace; runtime reproduction pending |
| Preconditions | Isolated synthetic fixture from QA/10_TEST_DATA_STRATEGY.md; never customer data |
| Reproduction frequency | Not measured; reproduction instructions are proposed |
| Source | apps/web/src/app/trial-bookings/page.tsx:47 |
| Class/function | load / AcademyAccessFilter / PermissionCatalog |
| Related source | INVENTORY/API_ENDPOINTS.md, CONTRACTS.md, FORMS_AND_FIELDS.md and ASYNC_FORM_RISKS.md |
| Root-cause confidence | HIGH |
| Evidence location | Source excerpt below; source fingerprint in INVENTORY/SOURCE_MANIFEST.md |
| Screenshot | Not captured on pinned baseline |
| Console logs | Not captured; required in retest |
| API request | Use a same-academy Sales, Marketing or FrontDesk user with active academy and Sales/Core modules. Load trial bookings and inspect the three lookup responses; compare AcademyAdmin. |
| API response | Not captured for this issue; use synthetic request/response in isolated reproduction |
| Database before/after | Not executed; fixture and fresh-context assertions defined below |
| Dependencies | Safe SQL/HTTP/browser harness as applicable; desired policy review where noted |
| Fix commit | Not implemented (Phase 1) |
| Retest result | NOT RUN |
| Regression result | NOT RUN |
| Closure notes | Remain OPEN; follow closure requirements in QA README |

## Exact reproduction

Use a same-academy Sales, Marketing or FrontDesk user with active academy and Sales/Core modules. Load trial bookings and inspect the three lookup responses; compare AcademyAdmin.

## Expected

Authorized sales workflow loads with least-privilege lookup access; optional teacher assignment must not block viewing or booking an unassigned trial.

## Actual / evidence

sales.manage authorizes leads/trials, but TeachersController lacks a PermissionCatalog mapping and is denied for nonadministrators. Page requires all three responses to succeed before populating any list. Runtime NOT RUN.

Source snapshot:

```text
46:     if (
47:       ![leadResponse, teacherResponse, trialResponse].every(
48:         (response) => response.ok,
49:       )
```

## Suspected root cause

Page lookup dependency and endpoint permission contract are inconsistent.

## Business impact and blast radius

Sales/Marketing/FrontDesk and custom sales.manage users cannot load trial UI even though trial APIs authorize them; not an authorization bypass.

## Related / required regression

SALES-ACCESS-001: Full-pipeline all-role two-tenant matrix and browser load/book/readback, optional teacher absent, approved restricted teacher lookup shape and no access expansion to teacher mutation/private records.

Also run all endpoint/form cases pointing to this issue in QA/03_TEST_MATRIX.md and the critical regression suite before closure.
