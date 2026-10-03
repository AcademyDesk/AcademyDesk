# BUG-FUNC-0018 — Batch, scheduling, attendance and progression pages require unrelated lookup permissions

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | STATIC-FINDING |
| Final verification | NOT RUN |
| Severity | Major authorized workflow blocked |
| Priority | P1 |
| Category | FUNC |
| Module | BATCH |
| Role | See reproduction; same-tenant Admin unless stated otherwise |
| Screen / route | /batches; /batch-setup; /enrollments; /batch-promotions; /schedule; /attendance; /makeup; /events |
| API | GET courses/teachers/branches/batches/students/enrollments/batch-promotions |
| Environment | Source review of local working tree; runtime production state not inferred |
| Device/viewport | NOT RUN; use QA/06_DEVICE_VIEWPORT_MATRIX.md where UI applies |
| Baseline | 20bb6047f9edf733ac8e2a226621cc582ec54b3c + pre-existing student UI diff |
| Discovery test / review ID | BATCH-ACCESS-001 |
| Evidence classification | Static trace; runtime reproduction pending |
| Preconditions | Isolated synthetic fixture from QA/10_TEST_DATA_STRATEGY.md; never customer data |
| Reproduction frequency | Not measured; reproduction instructions are proposed |
| Source | apps/web/src/app/batches/page.tsx:94 |
| Class/function | loadWorkspace / enrollments.load / promotions.load / PermissionCatalog |
| Related source | INVENTORY/API_ENDPOINTS.md, CONTRACTS.md, FORMS_AND_FIELDS.md and ASYNC_FORM_RISKS.md |
| Root-cause confidence | HIGH |
| Evidence location | Source excerpt below; source fingerprint in INVENTORY/SOURCE_MANIFEST.md |
| Screenshot | Not captured on pinned baseline |
| Console logs | Not captured; required in retest |
| API request | Load batch setup as Operations or Manager, enrollment as FrontDesk, and promotions as an academics.manage-only custom role. Use enabled modules and same-tenant fixtures; compare administrator. |
| API response | Not captured for this issue; use synthetic request/response in isolated reproduction |
| Database before/after | Not executed; fixture and fresh-context assertions defined below |
| Dependencies | Safe SQL/HTTP/browser harness as applicable; desired policy review where noted |
| Fix commit | Not implemented (Phase 1) |
| Retest result | NOT RUN |
| Regression result | NOT RUN |
| Closure notes | Remain OPEN; follow closure requirements in QA README |

## Exact reproduction

Load batch setup as Operations or Manager, enrollment as FrontDesk, and promotions as an academics.manage-only custom role. Use enabled modules and same-tenant fixtures; compare administrator.

## Expected

Feature roles can perform their permitted workflow using narrowly scoped lookups without unrelated management privileges.

## Actual / evidence

Batch loading needs courses academics.manage, unmapped teachers/branches and MultiBranch module; Operations/Manager have batches.manage but not those permissions. FrontDesk enrollment needs batches.manage lookup absent from that role. Academic-only promotion needs students.manage and batches.manage. Every page gates all data on all lookups succeeding. Runtime NOT RUN. Schedule also requires unmapped teachers/branches and MultiBranch even for optional assignment; attendance requires students/enrollments lookups denied to Operations/Manager despite their attendance.manage grant. Make-up page requires students.manage and unmapped teacher lookup even though Operations/Manager have makeup.manage. Events requires branches/MultiBranch even for no-branch events.

Source snapshot:

```text
93:     const [courseResponse, teacherResponse, branchResponse, batchResponse] =
94:       await Promise.all([
95:         academyApi(`/api/academies/${id}/courses`, { cache: "no-store" }),
96:         academyApi(`/api/academies/${id}/teachers`, { cache: "no-store" }),
```

## Suspected root cause

Primary action authorization is inconsistent with whole-page prerequisite lookup requirements.

## Business impact and blast radius

Supported operational/front-desk/delegated academic roles and Core-only batch administrators; no cross-tenant bypass claimed.

## Related / required regression

BATCH-ACCESS-001: Full-pipeline role/module/custom-grant matrix and browser lookup failures with least-privilege data minimization; do not grant broad mutation access merely to enable selectors. Include Operations/Manager/custom scheduling.manage and attendance.manage, same-tenant admin without MultiBranch, and each optional lookup failure separately. Include makeup.manage-only role and Certificates-enabled academy without MultiBranch.

Also run all endpoint/form cases pointing to this issue in QA/03_TEST_MATRIX.md and the critical regression suite before closure.
