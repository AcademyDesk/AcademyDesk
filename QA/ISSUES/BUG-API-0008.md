# BUG-API-0008 — Two controllers declare the same holiday DELETE route

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | REPRODUCED — actual MVC/TestServer; bounded repair SQL verified |
| Final verification | 4 routing/HTTP +36 retained audit tests and11 native SQL/Identity/HTTP checks PASS |
| Severity | Major holiday deletion unavailable |
| Priority | P1 |
| Category | API |
| Module | SCHEDULE |
| Role | See reproduction; same-tenant Admin unless stated otherwise |
| Screen / route | /holidays |
| API | DELETE /api/academies/{academyId:guid}/holidays/{guid} |
| Environment | Source review of local working tree; runtime production state not inferred |
| Device/viewport | NOT RUN; use QA/06_DEVICE_VIEWPORT_MATRIX.md where UI applies |
| Baseline | 20bb6047f9edf733ac8e2a226621cc582ec54b3c + pre-existing student UI diff |
| Discovery test / review ID | SCHEDULE-HOLIDAY-ROUTE-001 |
| Evidence classification | Original static trace; successor actual MVC/TestServer reproduction and repaired native SQL/Identity acceptance |
| Preconditions | Isolated synthetic fixture from QA/10_TEST_DATA_STRATEGY.md; never customer data |
| Reproduction frequency | Not measured; reproduction instructions are proposed |
| Source | apps/api/Controllers/HolidayDeleteController.cs:11 |
| Class/function | HolidayDelete.Delete / Holidays.Delete |
| Related source | INVENTORY/API_ENDPOINTS.md, CONTRACTS.md, FORMS_AND_FIELDS.md and ASYNC_FORM_RISKS.md |
| Root-cause confidence | HIGH |
| Evidence location | Source excerpt below; source fingerprint in INVENTORY/SOURCE_MANIFEST.md |
| Screenshot | Not captured on pinned baseline |
| Console logs | Not captured; required in retest |
| API request | In an isolated full MVC host enumerate endpoints and DELETE a synthetic local holiday. Compare foreign/missing ID and verify no deletion occurs on routing failure. |
| API response | Successor native run: authorized204; repeated/foreign/missing404; anonymous401; role/tenant/lifecycle/module403; no429 |
| Database before/after | Successor fresh SQL snapshots: denied/repeated requests unchanged; authorized deletion removes only local row and adds actor audit |
| Dependencies | Safe SQL/HTTP/browser harness as applicable; desired policy review where noted |
| Fix commit | NonController repair in scoped feature packet; publication SHA supplied in final Git handoff |
| Retest result | 4/4 TestServer/EF InMemory and11/11 real SQL/Identity/HTTP; run33433a0001e14ce0b93ca29077806214 |
| Regression result | 36 retained AuditOutcome tests PASS; full critical/device/browser pending |
| Closure notes | Remain OPEN; follow closure requirements in QA README |

## Exact reproduction

2026-10-11 successor: [bounded route repair](../REPORTS/PHASE_2B_HOLIDAY_ROUTE_REPAIR.md). Starting application reproduced two candidates and three ambiguous HTTP failures (4/4 tests failed). Legacy class retained but excluded from MVC discovery; canonical Holidays DELETE unchanged.4 repaired routing tests +36 audit controls PASS; harness builds0/0.11 native SQL/Identity/HTTP checks PASS, migrations88+7 and exact owned cleanup verified. Initial Docker/execution-policy blocks retained as history; PowerShell7 succeeded without global policy changes. Issue remains OPEN for browser/device/full-critical acceptance. Original static trace below remains historical.

In an isolated full MVC host enumerate endpoints and DELETE a synthetic local holiday. Compare foreign/missing ID and verify no deletion occurs on routing failure.

## Expected

Exactly one matching DELETE endpoint; authorized local row deletes with204, foreign/missing ID returns404 without changes.

## Actual / evidence

Original trace: HolidayDeleteController declares {holidayId:guid} and HolidaysController declares {id:guid} under identical controller route. Parameter names do not distinguish matching URL/method/constraint, yielding equally specific candidates. Actual starting-source MVC/TestServer ambiguity is now reproduced; native acceptance applies to the repaired source, not a SQL baseline run.

Source snapshot:

```text
10: {
11:     [HttpDelete("{holidayId:guid}")]
12:     public async Task<ActionResult> Delete(Guid academyId, Guid holidayId, CancellationToken token)
13:     {
```

## Suspected root cause

Duplicate deletion implementation is exposed through indistinguishable attribute routes.

## Business impact and blast radius

Holiday Remove button and direct DELETE; controller unit calls would miss this routing defect.

## Related / required regression

SCHEDULE-HOLIDAY-ROUTE-001: Endpoint uniqueness assertion plus real HTTP DELETE authorized/denied/foreign/missing/repeated cases against disposable SQL; assert status and exact row changes.

Also run all endpoint/form cases pointing to this issue in QA/03_TEST_MATRIX.md and the critical regression suite before closure.
