# BUG-FUNC-0018 — Batch, scheduling, attendance and progression pages require unrelated lookup permissions

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | STATIC-FINDING |
| Final verification | PARTIAL — C33 enrollment,C34 batch/setup,C35 attendance Cloud checks PASS;remaining workflow/native/device/release acceptance OPEN |
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
| Fix commit | C33 enrollment `9e10867`;C34 batch/setup `e93ed8d`;C35 attendance `7485173642d62abf7742acebf9d01c48bb3bb94c`;exact pins in each handoff;no deployment |
| Retest result | C35 38 actual HTTP/Identity/InMemory cases in full API;19 new +34 retained note/isolation controls;32 synthetic Operations/Manager Chromium cases PASS;earlier scope evidence retained below |
| Regression result | C35 full frontend2,474 PASS/3 inherited missing SQL-receipt failures;combined API2,559 PASS with Desktop f708879 preserved;exact source/limits in C35 handoff |
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

## C34 batch and setup subset — 11 November 2026

The recovered Cloud checkout preserved C33. New `GET batches/options` inherits
the existing batches.manage and current academy/tenant/actor gates. It returns
only course id/name/academyType,teacher id/firstName/lastName and branch id/name.
Operations/Manager/custom or current direct grants can read these labels without
academics/workforce/MultiBranch management calls. Existing full related APIs
remain forbidden. Core-only batch operation is covered;no permissions or module
policy changed. Historical labels do not grant eligibility or create authority.

Both /batches and its /batch-setup alias use scoped options plus the register.
Read readiness is invalidated during refresh,failed/invalid reads and academy
changes. Obsolete/unmounted read results are discarded. Unavailable data does not
produce authoritative zero metrics or an empty directory;direct and stale starts
cannot write. Drafts survive refresh and complete replacement payloads preserve
hidden settings. Existing server action bodies,admission/clash/teacher-inheritance
and transaction rules remain unchanged. Broader unknown-write recovery is not
added by this lookup packet.

Original HTTP3/34 PASS/31 FAIL reflects the missing GET contract;original page
0/21 controls passed,including absent new readiness/reload behavior. Final34 new
HTTP cases,106 focused API checks,21 scoped page controls and66 retained payload/
feedback/scheduling controls passed.32 Chromium cases cover Operations/Manager,
both routes,320/1440 and loaded/denied/malformed/empty reads. Loaded setup cases
also create,edit and deactivate with retained payloads. Synthetic API/browser and
InMemory HTTP are not native SQL/device acceptance.

[Source pins,latest combined result,fixture evolution and native tasks](../HANDOFF/C34_BATCH_OPTIONS_2026-11-11.md).
Remain OPEN for scheduling/attendance/promotions/makeup/events lookup scopes and
real SQL/browser/device/critical/release acceptance. Model loading remains stopped.

## C35 attendance subset — 11 November 2026

`GET attendance/options` now inherits the attendance controller's existing
attendance.manage and current global academy/tenant/actor/module/maintenance
policy. It returns only local batch/session labels and active enrollment roster
references with learner names. Every enrollment relation is scoped to local
student and batch records;duplicate active pairs collapse. Nonactive enrollments,
foreign/corrupt references and private details are omitted. Inactive learners
with an Active enrollment retain the existing Mark contract;no eligibility rule
changes. Existing attendance List/Mark/DTO bodies are byte-exact.

The page uses this workspace and the selected-session attendance GET,without full
people/enrollment/batch/scheduling fallback. Workspace readiness and generation
checks reject malformed/partial/foreign/duplicate records,retire old callbacks,
and prevent an unavailable workspace from appearing as an empty schedule/roster.
GET-only reload preserves note drafts and the existing session/record safeguards.
No write retry/replay or new attendance policy is introduced.

38 HTTP/Identity/InMemory controls in the full API include current role/grant
authorization,revocation,Core-only access,minimal/deduplicated active roster,
no-write/foreign/inactive/maintenance/owner boundaries,real read→mark flows for
Operations/Manager/custom authority,and refusal after enrollment is paused.
All19 scoped page and34 retained note/isolation assertions pass.32 sandboxed
Chromium cases cover both roles at320/1440,marks/notes,empty/denied/malformed
workspace,record retry,retained draft reload,session switching and delayed reads.
The browser API is synthetic;InMemory does not replace native SQL/race/device
acceptance. Original HTTP1/34 and page1/19 passes were preserved as baseline
failures,not interpreted as independent defect counts.

[Exact source,combined result and prioritized native tasks](../HANDOFF/C35_ATTENDANCE_OPTIONS_2026-11-11.md).
Remain OPEN for scheduling/promotions/makeup/events lookup scopes and broader
native SQL/browser/device/critical/release acceptance. Model loading stays stopped.
