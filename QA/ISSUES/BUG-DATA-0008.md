# BUG-DATA-0008 — Blank optional student numbers collide under the unique index

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | RUNTIME-CONFIRMED; bounded normalization repair tested |
| Final verification | 29 native SQL/Identity/HTTP cases PASS; linked browser/device/critical acceptance OPEN |
| Severity | Major onboarding persistence |
| Priority | P1 |
| Category | DATA |
| Module | STUDENT |
| Role | See reproduction; same-tenant Admin unless stated otherwise |
| Screen / route | /student-onboarding |
| API | POST /api/academies/{academyId}/student-onboarding |
| Environment | Local disposable SQL Server 2022/real HTTP pipeline; production state not inferred |
| Device/viewport | NOT RUN; use QA/06_DEVICE_VIEWPORT_MATRIX.md where UI applies |
| Baseline | 20bb6047f9edf733ac8e2a226621cc582ec54b3c + pre-existing student UI diff |
| Discovery test / review ID | STUDENT-RULE-002 |
| Evidence classification | Historical static trace plus disposable-SQL runtime reproduction and strict regression |
| Preconditions | Isolated synthetic fixture from QA/10_TEST_DATA_STRATEGY.md; never customer data |
| Reproduction frequency | One pinned baseline pair reproduced; final 29-case bounded matrix passed, not a stress frequency claim |
| Source | apps/api/Controllers/StudentOnboardingController.cs:33 |
| Class/function | Create / Student unique index |
| Related source | INVENTORY/API_ENDPOINTS.md, CONTRACTS.md, FORMS_AND_FIELDS.md and ASYNC_FORM_RISKS.md |
| Root-cause confidence | HIGH |
| Evidence location | QA/REPORTS/PHASE_2B_ONBOARDING_NUMBER_REPAIR.md; local run receipt identified there; historical source below |
| Screenshot | Not captured on pinned baseline |
| Console logs | Not captured; required in retest |
| API request | In a fresh isolated academy submit two different adult students with valid dates and studentNumber="" (matching the browser form). Repeat with whitespace; compare omitted/null controls. Query Students after each response. |
| API response | Baseline first empty intake200/second400; fixed blank pairs200/200, real duplicate400 and denied401/403 |
| Database before/after | Baseline fresh owned-SQL query: one synthetic intake with empty number; fixed nulls/exact domain/Identity/audit deltas and rejected unchanged snapshots verified |
| Dependencies | Safe SQL/HTTP/browser harness as applicable; desired policy review where noted |
| Fix commit | Bounded feature commit fix(api): normalize optional student numbers before onboarding persistence; see linked report/git history |
| Retest result | 29 native SQL/Identity/HTTP cases PASS; wider acceptance OPEN |
| Regression result | Recorded in linked report; original account/atomic assertions retained unchanged |
| Closure notes | Remain OPEN; follow closure requirements in QA README |

## Exact reproduction

In a fresh isolated academy submit two different adult students with valid dates and studentNumber="" (matching the browser form). Repeat with whitespace; compare omitted/null controls. Query Students after each response.

## Expected

Multiple students may omit the optional student number; no blank-number collision or unintended partial identity writes.

## Actual / evidence

Historical finding: FormData includes empty optional studentNumber; the former controller trimmed but retained empty string. The unique filtered index covers all non-null (AcademyId, StudentNumber), including empty strings. This is now runtime-confirmed on baseline d33ee60: first intake200, second400; fresh owned SQL contained one synthetic intake with empty number. The bounded fix persists blank/whitespace numbers as NULL and retains explicit-number trimming and the unchanged unique index.

Historical Phase 1 source snapshot, not current code:

```text
32:             await using var transaction = await db.Database.BeginTransactionAsync(token);
33:             var student = new Student { AcademyId = academyId, FirstName = request.StudentFirstName.Trim(), LastName = request.StudentLastName.Trim(), StudentNumber = request.StudentNumber?.Trim(), PreferredName = request.PreferredName?.Trim(), Gender = request.Gender?.Trim(), DateOfBirth = request.DateOfBirth, AdmissionDate = request.AdmissionDate ?? DateOnly.FromDateTime(DateTime.UtcNow), Email = request.StudentEmail?.Trim(), Phone = request.StudentPhone?.Trim(), AddressLine1 = request.StudentAddressLine1?.Trim(), City = request.StudentCity?.Trim(), State = request.StudentState?.Trim(), PostalCode = request.StudentPostalCode?.Trim(), EmergencyContactName = request.EmergencyContactName?.Trim(), EmergencyContactPhone = request.EmergencyContactPhone?.Trim(), MedicalOrAccessibilityNotes = request.MedicalOrAccessibilityNotes?.Trim(), BranchId = request.BranchId };
34:             db.Students.Add(student);
35:             Guardian? parent = null;
```

## Suspected root cause

Optional unique identifier normalized to empty string rather than null before persistence.

## Business impact and blast radius

Successive student onboarding in the same academy when optional student number is left blank; not evidence that every reported Azure failure has this cause.

## Related / required regression

STUDENT-RULE-002: Real SQL: two blank/whitespace-number intakes succeed with null; explicit same-tenant duplicate number rejects; same number in distinct tenants is isolated; inspect full student/guardian/identity chain.

Also run all endpoint/form cases pointing to this issue in QA/03_TEST_MATRIX.md and the critical regression suite before closure.

## Bounded repair — 2026-10-10

[Report](../REPORTS/PHASE_2B_ONBOARDING_NUMBER_REPAIR.md): baseline bdc8940bc4b14c88a18642e976756190, fixed44d44e74c20d4e97aa9e40e9380b2610.29 real SQL/Identity/HTTP cases PASS: repeated omitted/null/empty/space/tab/newline/Unicode whitespace, minor guardian/accounts, explicit trimming and same-tenant duplicates, distinct and cross-academy values, denied no-write, existing length constraint, preserved legacy blank and one concurrent blank pair. Exact fresh row deltas/IDs/roles/flags and complete safe rejected snapshots verified. No uniqueness weakening, migration, historical-row rewrite, new required field, authority change or production operation. Issue remains OPEN for linked browser, physical Android/iOS, broader race/stale/critical release acceptance; synthetic/API proof alone does not close it.
