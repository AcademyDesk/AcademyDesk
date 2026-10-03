# BUG-DATA-0052 — Teacher update can save the record and then report failure while linked accounts diverge

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | RUNTIME-REPRODUCED |
| Final verification | Local same-store Admin path PASS twice; platform/UI/concurrency/critical acceptance pending |
| Severity | Major teacher/account consistency |
| Priority | P1 |
| Category | DATA |
| Module | TEACHER |
| Role | See reproduction; same-tenant Admin unless stated otherwise |
| Screen / route | /teachers |
| API | PUT /api/academies/{academyId}/teachers/{teacherId} |
| Environment | Fresh guarded local real Identity/MVC/SQL; production state not inferred |
| Device/viewport | NOT RUN; use QA/06_DEVICE_VIEWPORT_MATRIX.md where UI applies |
| Baseline | 20bb6047f9edf733ac8e2a226621cc582ec54b3c + pre-existing student UI diff |
| Discovery test / review ID | TEACHER-UPDATE-ATOMICITY-001 |
| Evidence classification | Before-fix Identity validation/first/second-store/audit partial persistence reproduced twice; bounded repair retested |
| Preconditions | Isolated synthetic fixture from QA/10_TEST_DATA_STRATEGY.md; never customer data |
| Reproduction frequency | Four teacher and four paired student faults reproduced 2/2 fresh baseline runs; repair PASS 2/2 final runs |
| Source | apps/api/Controllers/TeachersController.cs:56 |
| Class/function | Update / linked Identity synchronization |
| Related source | INVENTORY/API_ENDPOINTS.md, CONTRACTS.md, FORMS_AND_FIELDS.md and ASYNC_FORM_RISKS.md |
| Root-cause confidence | HIGH |
| Evidence location | PHASE_2B_LINKED_PEOPLE_REPAIR.md, before/current snapshots and baseline/final SQL logs; historic excerpt retained |
| Screenshot | Not captured on pinned baseline |
| Console logs | Sanitized baseline/final harness logs linked in current report |
| API request | In an isolated host with a linked teacher portal account, induce Identity.UpdateAsync failure after the teacher SaveChanges using a synthetic conflicting email or controlled store failure. Compare response and fresh teacher/account state; repeat with two linked accounts and failure on the second. |
| API response | Before/final fault 400/500; baseline domain/account partial persistence, repaired boundary no captured writes; normal 200 and truthful validation rollback message |
| Database before/after | Before: person +0/1/2 accounts saved depending on fault; repaired boundary: all captured rows unchanged on fault, normal domain +both accounts +one attributed audit persisted |
| Dependencies | Safe SQL/HTTP/browser harness as applicable; desired policy review where noted |
| Fix commit | Local uncommitted explicit domain/Identity/audit SQL transaction; no deployment |
| Retest result | 26 linked cases PASS twice, plus active-token health controls; platform bypass/provisioning/UI/concurrency/critical pending |
| Regression result | API 178/178; two final SQL runs ×60 linked/create/finance cases PASS |
| Closure notes | Remain OPEN; follow closure requirements in QA README |

## Exact reproduction

Latest [local repair/evidence](../REPORTS/PHASE_2B_LINKED_PEOPLE_REPAIR.md) covers this teacher issue and the paired Student lifecycle finding. Identity validation uses controlled malformed synthetic usernames because default email uniqueness is not configured. Store fault uses owned-target-revalidated QA-only interception to make SQL Server fail the first/second actual Identity UPDATE; audit INSERT denial uses existing guarded fault. Partial domain/earlier-account commits reproduced twice, repaired same-store Admin paths PASS twice. Platform-flag bypass remains excluded and retains its truthful legacy partial-success error; all broader closure gates remain OPEN. Optional omission remains valid. Next BUG-DATA-0051 branch validation, agreed Sol High.

In an isolated host with a linked teacher portal account, induce Identity.UpdateAsync failure after the teacher SaveChanges using a synthetic conflicting email or controlled store failure. Compare response and fresh teacher/account state; repeat with two linked accounts and failure on the second.

## Expected

A failed linked-account update either rolls back the teacher change and all account changes, or returns an explicit partial-success contract with safe reconciliation and idempotent retry behavior.

## Actual / evidence

Original Update saved Teacher before iterating linked accounts. Baseline runtime confirms 400 after the domain commit on Identity validation failure; second SQL account failure returns 500 after person +one account persisted, audit failure after both accounts persisted. Current explicit same-store boundary rolls back these captured changes. Page/browser handling and platform-flag bypass are not certified by this bounded retest.

Source snapshot:

```text
55:
56:         var accounts = await userManager.Users
57:             .Where(user => user.AcademyId == academyId && user.TeacherId == teacherId)
58:             .ToListAsync(token);
```

## Suspected root cause

Teacher and Identity writes are not made atomic and the client treats a partial commit as total failure.

## Business impact and blast radius

Teacher name/contact/active-state records can disagree with portal accounts; retry and support diagnosis become ambiguous.

## Related / required regression

TEACHER-UPDATE-ATOMICITY-001: Real HTTP/SQL forced Identity failure, first/second-account order, exact 400 body, fresh row/account assertions, retry, audit behavior and normal success controls. Do not presume all Identity failures can be induced by email uniqueness alone.

Also run all endpoint/form cases pointing to this issue in QA/03_TEST_MATRIX.md and the critical regression suite before closure.
