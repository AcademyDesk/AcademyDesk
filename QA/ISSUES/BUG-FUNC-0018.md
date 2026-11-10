# BUG-FUNC-0018 — Batch, scheduling, attendance and progression pages require unrelated lookup permissions

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | STATIC-FINDING |
| Final verification | PARTIAL — C33 enrollment Cloud checks PASS;other workflow/native/device/release acceptance OPEN |
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
| Fix commit | `9e108672b9c1e43f395ac11684b2e8235cb663ff` enrollment subset only;no deployment |
| Retest result | 36 new actual HTTP/Identity/InMemory checks in full API;18 new +38 retained client checks and40 synthetic FrontDesk Chromium cases PASS |
| Regression result | API2,487/2,487 PASS;frontend2,434 PASS/3 inherited missing SQL-receipt failures;other0018 pages not changed |
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

## C33 enrollment subset — 10 November 2026

Historical evidence above is retained. `GET enrollments/options` now provides
only student id/firstName/lastName and batch id/name under the enrollment
controller's existing students.manage, current academy/actor and global filter.
No broader permission or module rule changed. FrontDesk, custom students.manage
roles and current direct grants can load these labels while full batch reads
remain forbidden. Current grant/custom-role revocation, anonymous/unrelated/
foreign/suspended and maintenance refusals remain enforced. A PlatformOwner role
label does not replace the real server owner flag; existing owner policy remains.

The page uses options plus its enrollment register; it no longer needs full
student/batch records. Partial/malformed/duplicate options fail closed with no
fallback. Historical inactive/closed labels remain available for the register;
the API still rechecks admission and target eligibility on writes. Existing C32
reason/review/draft/receipt/recovery behavior and assertions are retained.

Original HTTP matrix3/33 PASS/30 FAIL reflects the absent GET contract, not30
independent defects. Final36 new HTTP cases cover minimized schema/no-store,
no-write/audit, same-tenant and current authority, empty/tied/historical data,
Core-only FrontDesk, create/lifecycle flow and unchanged full batch denial.
18 new client checks and retained38 C32 controls pass. All40 native Chromium
scenarios pass with synthetic FrontDesk profile/API and no full student/batch
request; browser sandbox stays enabled. These are not native SQL or actual-device
acceptance. Existing enrollment action bodies and C32 assertions are byte-exact.

[Exact source/results and ordered native handoff](../HANDOFF/C33_ENROLLMENT_OPTIONS_2026-11-10.md).
Remain OPEN for other0018 batch/setup/scheduling/attendance/promotions/makeup/events
lookup scopes and real SQL/browser/device/linked critical/release acceptance.
