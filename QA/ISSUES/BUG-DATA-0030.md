# BUG-DATA-0030 — New open terms can be added to closed academic years

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | RUNTIME-REPRODUCED / LOCAL REPAIR; broader OPEN |
| Final verification | Bounded backend/HTTP/SQL/race/rollback PASS; browser/device/all-linked/critical NOT RUN |
| Severity | Major academic closure integrity |
| Priority | P1 |
| Category | DATA |
| Module | ACADEMIC |
| Role | See reproduction; same-tenant Admin unless stated otherwise |
| Screen / route | /academic-periods; direct API or stale open form |
| API | POST academic-periods/terms |
| Environment | Isolated Testing Identity/HTTP/SQL Server2022; normal dev/Azure data untouched |
| Device/viewport | NOT RUN; use QA/06_DEVICE_VIEWPORT_MATRIX.md where UI applies |
| Baseline | 20bb6047f9edf733ac8e2a226621cc582ec54b3c + pre-existing student UI diff |
| Discovery test / review ID | ACADEMIC-CLOSURE-001 |
| Evidence classification | Three baseline product failures; final363 backend + two fresh SQL38-case PASS |
| Preconditions | Isolated synthetic fixture from QA/10_TEST_DATA_STRATEGY.md; never customer data |
| Reproduction frequency | Three baseline closed/stale failures; two independent final SQL runs |
| Source | apps/api/Controllers/AcademicPeriodsController.cs:32 CreateTerm /49 CloseYear; original excerpt below historical |
| Class/function | CreateTerm / CloseYear |
| Related source | INVENTORY/API_ENDPOINTS.md, CONTRACTS.md, FORMS_AND_FIELDS.md and ASYNC_FORM_RISKS.md |
| Root-cause confidence | HIGH |
| Evidence location | [Year closure repair report](../REPORTS/PHASE_2B_YEAR_CLOSURE_REPAIR.md), logs/TRX/snapshot linked there |
| Screenshot | Not captured on pinned baseline |
| Console logs | Runtime harness logs linked in repair report; browser console NOT RUN |
| API request | Synthetic open/closed/current/date/name/foreign/missing parent, close child states, stale form, role/tenant, paired concurrent close/create and audit fault controls |
| API response | Term409 for closed parent; existing200/400/401/403/404/409 contracts preserved; deliberate audit500 fully rolled back |
| Database before/after | Fresh SQL full year/term/audit rows and unrelated snapshots; current/closed flags preserved; two blocked launch orders satisfy no closed-year/open-child invariant |
| Dependencies | Safe SQL/HTTP/browser harness as applicable; desired policy review where noted |
| Fix commit | Local uncommitted closed-parent guard and coupled SQL parent lock/domain audit boundary; no deployment |
| Retest result | PASS two fresh real Identity/HTTP/SQL runs x38; two harness-only interrupted attempts retained/excluded/cleaned |
| Regression result | PASS full backend363/363 (22 new); both race winners, audit rollback and platform transaction fallback; frontend/browser/critical not rerun |
| Closure notes | Remain OPEN; browser/device/all-linked/current-year and other lifecycle/critical/release gates pending |

## Local repair checkpoint — 2026-10-01

[Repair/evidence](../REPORTS/PHASE_2B_YEAR_CLOSURE_REPAIR.md): closed parent rejects term409. CreateTerm/CloseYear share a parameterized SQL parent lock and domain save/audit boundary; platform bypass uses an owned transaction without a new audit policy. Three product baseline failures plus two future boundary assertions retained. Backend363/363 and two fresh real HTTP/SQL runs x38 PASS, including both queued close/create race winners, captured no-write/flags, audit rollback and fallback. Two harness-only observability/type failures excluded and owned containers removed. No schema/frontend/dev DB/Azure/commit/push. Browser/device/all-linked/other lifecycle/critical remain pending. Next promotion terminal-state/replay integrity BUG-DATA-0032, Sol High.

## Exact reproduction

Create a synthetic year, close all its terms and close the year. POST a new correctly dated term using that year ID; also retain an open term form while another actor closes its year.

## Expected

A closed year cannot acquire an open child term without an explicit authorized reopening workflow.

## Historical Actual / evidence (before repair)

UI filters closed years from options but API CreateTerm checks only year existence and date bounds, then creates an open term. CloseYear requires all terms closed, so new writes invalidate its condition. Runtime NOT RUN.

Source snapshot:

```text
34:         if (string.IsNullOrWhiteSpace(request.Name) || request.EndDate < request.StartDate || request.StartDate < year.StartDate || request.EndDate > year.EndDate) return BadRequest(new { message = "Term dates must be within the academic year." });
35:         var term = new AcademicTerm { AcademyId = academyId, AcademicYearId = year.Id, Name = request.Name.Trim(), StartDate = request.StartDate, EndDate = request.EndDate }; db.AcademicTerms.Add(term); await db.SaveChangesAsync(token); return Ok(new AcademicTermSummary(term.Id, term.AcademicYearId, term.Name, term.StartDate, term.EndDate, term.IsClosed));
36:     }
37:
```

## Suspected root cause

Parent lifecycle guard is missing from child creation and is enforced only by picker filtering.

## Business impact and blast radius

Closed-year registers and downstream academic lifecycle can contain open terms; fresh picker alone does not protect stale/direct requests.

## Related / required regression

ACADEMIC-CLOSURE-001: HTTP/SQL open/closed/missing/foreign-year matrix, concurrent close-versus-create, boundary dates, no-write on rejection and current/closed flags preserved.

Also run all endpoint/form cases pointing to this issue in QA/03_TEST_MATRIX.md and the critical regression suite before closure.
