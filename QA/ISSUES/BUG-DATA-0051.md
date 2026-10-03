# BUG-DATA-0051 — Teacher update accepts a foreign or nonexistent branch

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | RUNTIME-REPRODUCED |
| Final verification | Local same-tenant Admin branch matrix PASS twice; browser/all-role/concurrency/critical pending |
| Severity | Major teacher branch integrity |
| Priority | P1 |
| Category | DATA |
| Module | TEACHER |
| Role | See reproduction; same-tenant Admin unless stated otherwise |
| Screen / route | /teachers |
| API | PUT /api/academies/{academyId}/teachers/{teacherId} |
| Environment | Fresh guarded local real Identity/MVC/SQL; production state not inferred |
| Device/viewport | NOT RUN; use QA/06_DEVICE_VIEWPORT_MATRIX.md where UI applies |
| Baseline | 20bb6047f9edf733ac8e2a226621cc582ec54b3c + pre-existing student UI diff |
| Discovery test / review ID | TEACHER-UPDATE-BRANCH-001 |
| Evidence classification | Foreign/missing/empty/inactive-foreign assignment reproduced twice; bounded paired student/teacher repair retested |
| Preconditions | Isolated synthetic fixture from QA/10_TEST_DATA_STRATEGY.md; never customer data |
| Reproduction frequency | Four invalid teacher +four paired student assignments reproduced 2/2 fresh baseline runs; repair PASS 2/2 final runs |
| Source | apps/api/Controllers/TeachersController.cs:52 |
| Class/function | Update |
| Related source | INVENTORY/API_ENDPOINTS.md, CONTRACTS.md, FORMS_AND_FIELDS.md and ASYNC_FORM_RISKS.md |
| Root-cause confidence | HIGH |
| Evidence location | PHASE_2B_PEOPLE_BRANCH_REPAIR.md, before/current snapshots and baseline/final SQL logs; historic excerpt retained |
| Screenshot | Not captured on pinned baseline |
| Console logs | Sanitized baseline/final harness logs linked in current report |
| API request | With synthetic academies A/B and an A teacher, submit a B branch GUID, a missing GUID, a valid A branch and null separately. Read the teacher row and linked views in a fresh SQL context. |
| API response | Before invalid GUIDs 200; repaired 400 with exact Create-equivalent branch message; valid/null/omitted controls 200; malformed GUID 400 before/after |
| Database before/after | Before invalid branch +two own accounts +audit saved; repaired refusals preserve all captured people/accounts/finance/audits; successful branch changes verified in fresh SQL and list GET |
| Dependencies | Safe SQL/HTTP/browser harness as applicable; desired policy review where noted |
| Fix commit | Local uncommitted two-line membership guard per Update action; no deployment |
| Retest result | 22 branch +26 linked cases PASS twice; own inactive behavior preserved, policy approval not inferred |
| Regression result | API 184/184; two final SQL runs ×48 cases PASS; broader/browser/critical pending |
| Closure notes | Remain OPEN; follow closure requirements in QA README |

## Exact reproduction

Latest [local repair/evidence](../REPORTS/PHASE_2B_PEOPLE_BRANCH_REPAIR.md) covers this teacher finding and paired Student.Update. Both original controllers return 200 and persist foreign/missing/empty/inactive-foreign branch IDs on two fresh SQL runs. Membership guards matching Create reject these before mutation; local 184 API tests and two final runs ×48 cases PASS. Optional null/omission still clear existing assignment; own inactive branches remain accepted as existing behavior, not new business-policy approval. No FK/schema/frontend change, historical data reconciliation or issue/phase closure. Next already reproduced FinanceUser lookup dependency BUG-FUNC-0006, agreed Sol High.

With synthetic academies A/B and an A teacher, submit a B branch GUID, a missing GUID, a valid A branch and null separately. Read the teacher row and linked views in a fresh SQL context.

## Expected

A nonnull branch must exist in the route academy as in Teachers.Create; foreign or missing IDs are rejected before any teacher or linked-account write.

## Actual / evidence

Original Create validated branch academy ownership while Update assigned BranchId directly after checking route scope/names. Baseline runtime confirms 200 plus persisted invalid GUID and list readback for foreign/missing/empty/inactive-foreign cases. No configured branch FK prevented these writes. Current Update guard rejects them with 400 before mutation; fresh captured rows/accounts/audits remain unchanged. Browser rendering, deletion races, old invalid data and full critical acceptance remain unverified.

Source snapshot:

```text
51:         teacher.Specialties = request.Specialties?.Trim();
52:         teacher.BranchId = request.BranchId;
53:         teacher.IsActive = request.IsActive;
54:         await dbContext.SaveChangesAsync(token);
```

## Suspected root cause

Create-only relationship validation is absent on Update.

## Business impact and blast radius

Administrative teacher edits can persist an invalid branch reference and misstate branch-specific staffing; GUID assignment alone does not imply access to foreign branch data.

## Related / required regression

TEACHER-UPDATE-BRANCH-001: Real HTTP/SQL same-academy/foreign/missing/null branch matrix, fresh no-write assertions on rejection, linked account unchanged, inactive branch policy and valid reassignment controls.

Also run all endpoint/form cases pointing to this issue in QA/03_TEST_MATRIX.md and the critical regression suite before closure.
