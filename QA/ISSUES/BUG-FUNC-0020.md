# BUG-FUNC-0020 — Assessment result permissions and module gate differ from assessment setup

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | Accepted static finding; local result catalogs and scoped page lookup repair verified |
| Final verification | LOCAL API/controlled-form PASS; live browser/device/lint/critical release gates pending |
| Severity | Major academic workflow permission mismatch |
| Priority | P1 |
| Category | FUNC |
| Module | ACADEMIC |
| Role | See reproduction; same-tenant Admin unless stated otherwise |
| Screen / route | /assessments |
| API | GET/POST assessments/{assessmentId}/results |
| Environment | Source review of local working tree; runtime production state not inferred |
| Device/viewport | NOT RUN; use QA/06_DEVICE_VIEWPORT_MATRIX.md where UI applies |
| Baseline | 20bb6047f9edf733ac8e2a226621cc582ec54b3c + pre-existing student UI diff |
| Discovery test / review ID | ACADEMIC-RESULT-ACCESS-001 |
| Evidence classification | Static trace; runtime reproduction pending |
| Preconditions | Isolated synthetic fixture from QA/10_TEST_DATA_STRATEGY.md; never customer data |
| Reproduction frequency | Not measured; reproduction instructions are proposed |
| Source | apps/api/Controllers/AssessmentsController.cs:32 |
| Class/function | AssessmentResultsController / PermissionCatalog / SubscriptionPlanCatalog |
| Related source | INVENTORY/API_ENDPOINTS.md, CONTRACTS.md, FORMS_AND_FIELDS.md and ASYNC_FORM_RISKS.md |
| Root-cause confidence | HIGH |
| Evidence location | Source excerpt below; source fingerprint in INVENTORY/SOURCE_MANIFEST.md |
| Screenshot | Not captured on pinned baseline |
| Console logs | Not captured; required in retest |
| API request | Use an active custom role with academics.manage and Core/AcademicGovernance enabled. Create an assessment through its API, then read/save results. Compare AcademyAdmin. Separately disable AcademicGovernance while retaining Core and test admin result access against a known assessment. |
| API response | Not captured for this issue; use synthetic request/response in isolated reproduction |
| Database before/after | Not executed; fixture and fresh-context assertions defined below |
| Dependencies | Safe SQL/HTTP/browser harness as applicable; desired policy review where noted |
| Fix commit | Local uncommitted result catalogs, read-only academic options and page integration; no deployment |
| Retest result | Latest426 backend,29 controlled form cases and40 realHTTP/SQL option cases PASS; prior77 API mapping cases historical |
| Regression result | Local bounded API/controlled-form PASS; live browser/device, existing lint and critical gates OPEN |
| Closure notes | Remain OPEN; follow closure requirements in QA README |

## Local repair checkpoint — 2026-10-01

Latest[scoped option/page repair](../REPORTS/PHASE_2B_ASSESSMENT_OPTIONS_REPAIR.md) replaces the page's four denied generic lookup dependencies with an academic read-only options contract under the existing setup gates. Minimal owned batch/active-roster/active-scheme projection; generic management remains denied. Backend426/426 (9 new), controlled page/grade29/29 and realHTTP/SQL40/40 PASS, including real returned-ID setup/result workflow. Current route count312 adds one GET. No schema/Teacher/policy change or deployment; normal dev services not restarted. Browser/device/critical and1 existing page lint error/2 warnings remain. The prior paragraph below describes the earlier catalog-only checkpoint, not the current page dependencies.

See[API access repair report](../REPORTS/PHASE_2B_ASSESSMENT_ACCESS_REPAIR.md). Results now use the existing setup `academics.manage`/`AcademicGovernance` policy rather than absent permission/Core fallback. Backend417/417 including9 new tests and realHTTP/SQL77 pass across roles, same-token grant lifecycle, modules, tenant/account activity, setup-to-result workflow and Teacher own-route controls. All four generic page lookups still deny academic-only delegation; no broad student/enrollment/batch-management access was added. Keep OPEN for scoped lookup/page repair and release acceptance. Original accepted evidence below is historical, not current catalog behavior.

## Original reproduction

Use an active custom role with academics.manage and Core/AcademicGovernance enabled. Create an assessment through its API, then read/save results. Compare AcademyAdmin. Separately disable AcademicGovernance while retaining Core and test admin result access against a known assessment.

## Expected

Assessment setup and result management follow an explicit consistent academic permission/module policy; necessary lookups also allow the intended delegated workflow.

## Actual / evidence

AssessmentsController maps academics.manage and AcademicGovernance; AssessmentResultsController is absent from both catalogs, so delegated users are denied and module selection falls back to Core. An administrator with Core can still read/write results even when AcademicGovernance is disabled. Page also depends on batches/students/enrollments and active grading schemes, whose permissions can fail earlier. Runtime NOT RUN.

Source snapshot:

```text
31: [Route("api/academies/{academyId:guid}/assessments/{assessmentId:guid}/results")]
32: public sealed class AssessmentResultsController(AcademyDeskDbContext dbContext) : ControllerBase
33: {
34:     [HttpGet]
```

## Suspected root cause

Separate results controller was omitted from feature permission/module mappings.

## Business impact and blast radius

Delegated academic staff cannot record results and subscription gating differs for results versus setup. Same-tenant filter remains present.

## Related / required regression

ACADEMIC-RESULT-ACCESS-001: Real pipeline role/grant/module matrix, each lookup independently controlled, valid admin and teacher controls, inactive-account/tenant and cross-tenant denial; approve result module policy explicitly.

Also run all endpoint/form cases pointing to this issue in QA/03_TEST_MATRIX.md and the critical regression suite before closure.
