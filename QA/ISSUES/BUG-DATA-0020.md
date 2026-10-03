# BUG-DATA-0020 — Student active-status toggle silently clears branch assignment

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | STATIC-FINDING |
| Final verification | NOT RUN |
| Severity | Major unintended data loss |
| Priority | P1 |
| Category | DATA |
| Module | STUDENT |
| Role | See reproduction; same-tenant Admin unless stated otherwise |
| Screen / route | /students |
| API | PUT students/{studentId} |
| Environment | Source review of local working tree; runtime production state not inferred |
| Device/viewport | NOT RUN; use QA/06_DEVICE_VIEWPORT_MATRIX.md where UI applies |
| Baseline | 20bb6047f9edf733ac8e2a226621cc582ec54b3c + pre-existing student UI diff |
| Discovery test / review ID | STUDENT-BRANCH-001 |
| Evidence classification | Static trace; runtime reproduction pending |
| Preconditions | Isolated synthetic fixture from QA/10_TEST_DATA_STRATEGY.md; never customer data |
| Reproduction frequency | Not measured; reproduction instructions are proposed |
| Source | apps/web/src/app/students/page.tsx:78 |
| Class/function | toggle / Students.Update |
| Related source | INVENTORY/API_ENDPOINTS.md, CONTRACTS.md, FORMS_AND_FIELDS.md and ASYNC_FORM_RISKS.md |
| Root-cause confidence | HIGH |
| Evidence location | Source excerpt below; source fingerprint in INVENTORY/SOURCE_MANIFEST.md |
| Screenshot | Not captured on pinned baseline |
| Console logs | Not captured; required in retest |
| API request | Seed an active student assigned to a same-academy branch. Use Make inactive, read persisted BranchId, then reactivate and read again. Repeat an initially unassigned control. |
| API response | Not captured for this issue; use synthetic request/response in isolated reproduction |
| Database before/after | Not executed; fixture and fresh-context assertions defined below |
| Dependencies | Safe SQL/HTTP/browser harness as applicable; desired policy review where noted |
| Fix commit | Not implemented (Phase 1) |
| Retest result | NOT RUN |
| Regression result | NOT RUN |
| Closure notes | Remain OPEN; follow closure requirements in QA README |

## Exact reproduction

Seed an active student assigned to a same-academy branch. Use Make inactive, read persisted BranchId, then reactivate and read again. Repeat an initially unassigned control.

## Expected

Changing active status preserves branch and all unrelated student fields.

## Actual / evidence

Client Student shape omits returned BranchId and toggle always submits branchId:null. Students.Update assigns the submitted value before saving, erasing existing branch on either status transition. Runtime NOT RUN.

Source snapshot:

```text
77:             phone: student.phone,
78:             branchId: null,
79:             isActive: !student.isActive,
80:           }),
```

## Suspected root cause

Full replacement endpoint called with fabricated null for an unrelated field rather than preserving the loaded branch.

## Business impact and blast radius

Branch-assigned students deactivated or reactivated from register; affects subsequent branch-specific administration/reporting.

## Related / required regression

STUDENT-BRANCH-001: Browser plus real HTTP/SQL active-to-inactive and reverse tests asserting exact branch/name/contact preservation; no-branch control, failed request, retry and linked account state.

Also run all endpoint/form cases pointing to this issue in QA/03_TEST_MATRIX.md and the critical regression suite before closure.
