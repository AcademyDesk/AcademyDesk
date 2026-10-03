# BUG-DATA-0028 — Course edits and active-state changes clear hidden configuration

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | Client payload loss controlled-reproduced/local repair; legacy loss RUNTIME-REPRODUCED in real HTTP/SQL; browser pending |
| Final verification | Two fresh HTTP/SQL47-case runs PASS; controller322/frontend233/TypeScript retained, not rerun; browser/device/all-linked/critical pending |
| Severity | Major unintended curriculum data loss |
| Priority | P1 |
| Category | DATA |
| Module | CURRICULUM |
| Role | See reproduction; same-tenant Admin unless stated otherwise |
| Screen / route | /courses |
| API | PUT /api/academies/{academyId}/courses/{courseId} |
| Environment | Pinned local source; isolated Testing application with real Identity/HTTP/SQL Server2022; not Azure production |
| Device/viewport | NOT RUN; use QA/06_DEVICE_VIEWPORT_MATRIX.md where UI applies |
| Baseline | 20bb6047f9edf733ac8e2a226621cc582ec54b3c + pre-existing student UI diff |
| Discovery test / review ID | CURRICULUM-PRESERVE-001 |
| Evidence classification | Controlled TSX, controller/InMemory and real Identity/HTTP/SQL fresh-row/no-write/audit evidence; not browser/concurrency proof |
| Preconditions | Isolated synthetic fixture from QA/10_TEST_DATA_STRATEGY.md; never customer data |
| Reproduction frequency | Not measured; reproduction instructions are proposed |
| Source | apps/web/src/app/courses/page.tsx:89 |
| Class/function | saveCourse / toggleActive / Courses.Apply |
| Related source | INVENTORY/API_ENDPOINTS.md, CONTRACTS.md, FORMS_AND_FIELDS.md and ASYNC_FORM_RISKS.md |
| Root-cause confidence | HIGH |
| Evidence location | Source excerpt below; source fingerprint in INVENTORY/SOURCE_MANIFEST.md |
| Screenshot | Not captured on pinned baseline |
| Console logs | Not captured; required in retest |
| API request | Create a synthetic course through the API with code, subject, duration, weekly sessions, session length, age limits, delivery mode, prerequisites, outcomes and IsPublished=true. Edit only its visible name, then independently deactivate/reactivate populated fixtures. Read every persisted property. |
| API response | Real HTTP201/200/400/401/403/404 statuses and full response/list projection assertions recorded in Course SQL logs/harness; not production evidence |
| Database before/after | Fresh SQL full Course row equality and captured unrelated/rejected/read no-write PASS on two owned fixtures; see Course SQL report |
| Dependencies | Safe SQL/HTTP/browser harness as applicable; desired policy review where noted |
| Fix commit | Local frontend complete-replacement helper/edit/status repair; uncommitted/not deployed |
| Retest result | Real HTTP/SQL preservation/null/zero/false/intentional clears/400/401/403/404 no-write PASS; browser/device/all-linked/critical pending |
| Regression result | SQL47 twice and isolated build PASS; prior backend322/frontend233/TypeScript retained; Course lint1error/1warning/browser/concurrency remain open |
| Closure notes | Remain OPEN; follow closure requirements in QA README |

## Real HTTP/SQL checkpoint — 2026-10-01

[Course SQL checks](../REPORTS/PHASE_2B_COURSE_SQL_CHECK.md):47 cases PASS on each of two fresh owned SQL fixtures. Real Identity/MVC/filter/SQL full Course row and response/list projection preservation, optional controls, intentional clears, rejected400/401/403/404 no-write and success actor/route audit verified. Legacy abbreviated body reproduces hidden-setting loss under unchanged replacement semantics. Source/binary fingerprints, route/migration inventory and ownership-checked cleanup pinned; no application change, normal dev database write, commit or Azure deployment. Controller322/frontend233/TypeScript retained, not rerun. Issue remains OPEN for browser/device/all-linked/critical/concurrency gates; next accepted circular prerequisites BUG-DATA-0029, Sol High.

## Controller contract checkpoint — 2026-10-01 (historical)

[Course controller checks](../REPORTS/PHASE_2B_COURSE_CONTRACT_CHECK.md):45 direct-controller/InMemory checks and fresh backend322/322 PASS. Summary/JSON binding/update/fresh entity reads verify all fields, optional controls, explicit clears and rejected no-write behavior; foreign targets are404 and foreign duplicate codes do not conflict. Legacy abbreviated body still clears settings under unchanged replacement semantics, confirming the frontend repair is necessary. All155 preceding captures/normal assemblies unchanged; frontend233/TypeScript/lint retained, not rerun. No SQL durability/HTTP routing/role-filter/browser proof, no application code change or deployment. Issue OPEN; next existing isolated HTTP/SQL harness coverage, Sol High.

## Local frontend checkpoint — 2026-10-01 (historical)

[Course preservation repair](../REPORTS/PHASE_2B_COURSE_PRESERVATION_REPAIR.md): original controlled baseline24 (3 PASS/21 FAIL) confirms abbreviated edit/status PUT payloads. Full summary/helper now retain hidden settings including IsPublished, populated/null/zero/false controls and explicit visible-level clear. New24/Course73/combined233 controlled PASS; TypeScript. All148 unaffected prior captures/backend assemblies unchanged; existing Course lint1error/1warning remains FAIL, helper clean. No HTTP/SQL/browser/permission/device or persisted-row/no-write acceptance. No commit/Azure/deployment; issue remains OPEN. Next isolated controller contract checks, Sol High; no repeated Astra audit. Original static evidence below retained as historical, not current payload.

## Exact reproduction

Create a synthetic course through the API with code, subject, duration, weekly sessions, session length, age limits, delivery mode, prerequisites, outcomes and IsPublished=true. Edit only its visible name, then independently deactivate/reactivate populated fixtures. Read every persisted property.

## Expected

Visible-field edits and active-state changes preserve all unrelated course settings and publication state.

## Actual / evidence

Historical pre-repair UI sends durationMonths:null and omits extended settings including IsPublished; replacement Apply assigns null/default false. The legacy abbreviated-body control now reproduces this loss in real HTTP/SQL on two fixtures. The locally repaired frontend sends the complete summary. Full replacement payloads preserve settings in the current controller and SQL checks; browser/device acceptance remains pending.

Source snapshot:

```text
88:           description: course.description,
89:           durationMonths: null,
90:           isActive: course.isActive,
91:         }),
```

## Suspected root cause

Abbreviated editor payload is sent to a full replacement contract without preserving unrepresented fields.

## Business impact and blast radius

API-configured course metadata, publication state and downstream planning; structured CoursePrerequisite records are separate and are not deleted by this assignment.

## Related / required regression

CURRICULUM-PRESERVE-001: Browser and HTTP/SQL edit/deactivate/reactivate with fully populated and null controls, fresh full-row equality for unrelated properties, explicit intentional clears and rejected-update no-write checks.

Also run all endpoint/form cases pointing to this issue in QA/03_TEST_MATRIX.md and the critical regression suite before closure.
