# BUG-DATA-0004 — Student transaction does not encompass Identity account writes

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | RUNTIME-CONFIRMED; bounded repair tested |
| Final verification | 21 native SQL/Identity/HTTP cases PASS; browser/device/full critical acceptance OPEN |
| Severity | Major integrity |
| Priority | P1 |
| Category | DATA |
| Module | STUDENT |
| Role | See reproduction; same-tenant Admin unless stated otherwise |
| Screen / route | /student-onboarding |
| API | POST student-onboarding |
| Environment | Source review of local working tree; runtime production state not inferred |
| Device/viewport | NOT RUN; use QA/06_DEVICE_VIEWPORT_MATRIX.md where UI applies |
| Baseline | 20bb6047f9edf733ac8e2a226621cc582ec54b3c + pre-existing student UI diff |
| Discovery test / review ID | STUDENT-DB-001 |
| Evidence classification | Historical static trace plus native disposable-SQL reproduction and strict regression |
| Preconditions | Isolated synthetic fixture from QA/10_TEST_DATA_STRATEGY.md; never customer data |
| Reproduction frequency | Not measured; reproduction instructions are proposed |
| Source | apps/api/Controllers/StudentOnboardingController.cs:32 |
| Class/function | Create / CreateAccount |
| Related source | INVENTORY/API_ENDPOINTS.md, CONTRACTS.md, FORMS_AND_FIELDS.md and ASYNC_FORM_RISKS.md |
| Root-cause confidence | HIGH |
| Evidence location | Source excerpt below; source fingerprint in INVENTORY/SOURCE_MANIFEST.md |
| Screenshot | Not captured on pinned baseline |
| Console logs | Not captured; required in retest |
| API request | In disposable SQL create onboarding with student account and guardian account; induce failure creating the second account after first account creation. Inspect both DbContexts with new connections. |
| API response | Not captured for this issue; use synthetic request/response in isolated reproduction |
| Database before/after | Not executed; fixture and fresh-context assertions defined below |
| Dependencies | Safe SQL/HTTP/browser harness as applicable; desired policy review where noted |
| Fix commit | Feature commit containing PHASE_2B_ONBOARDING_ATOMIC_REPAIR.md; no main/Azure deployment |
| Retest result | 21 native cases PASS; see latest checkpoint below |
| Regression result | 23 retained optional-date native cases and56 targeted unit tests PASS |
| Closure notes | Remain OPEN; follow closure requirements in QA README |

## Latest bounded checkpoint — 2026-10-10

[Report](../REPORTS/PHASE_2B_ONBOARDING_ATOMIC_REPAIR.md), starting HEAD517a943. Baseline `066c3660351545adbd4f5024c72875d3` reproduced HTTP400 after invalid second-account password with a changed fresh domain/Identity/audit snapshot. No strict case passed before the expected rollback assertion failed. The historical static entries below are not current verification results.

Student Onboarding now opts into the existing same-store SQL domain/Identity/audit boundary; its nested domain-only transaction is removed. Role creation and assignment IdentityResults are checked. Explicit platform-owner transaction/audit opt-in is scoped to this action; original controller authorization and other endpoint bypass behavior remain unchanged.

Final native run `97dde5dfa27849bab8dfbdb2597582d0`:21/21 PASS, including invalid/duplicate parent credentials, six SQL failure stages, failed role/membership results, successful/minimal account options, Owner/platform-owner integrity and anonymous/teacher/foreign-route no-write controls. Fresh contexts verify failed domain/Identity/role/membership/audit snapshots unchanged and successful exact row deltas/role links. Optional-date native retest23/23 and targeted unit56/56 PASS; builds0/0,88 application/7 Identity migrations and exact owned cleanup verified. Evidence remains local.

Issue remains OPEN: linked live browser, physical Android/iOS, concurrent onboarding/cancellation/connection-store mismatch and full critical acceptance are not completed here. BUG-DATA-0007 incomplete-credential created flags and other onboarding field/branch/uniqueness issues remain separate; this transaction fix does not claim them repaired.

## Exact reproduction

In disposable SQL create onboarding with student account and guardian account; induce failure creating the second account after first account creation. Inspect both DbContexts with new connections.

## Expected

All related records commit together, or partial outcomes are explicit and recoverable without orphan logins.

## Actual / evidence

Transaction is on AcademyDeskDbContext; UserManager uses separately registered IdentityDbContext with no shared transaction enlistment shown. Student rollback may leave the first identity committed.

Source snapshot:

```text
31:         {
32:             await using var transaction = await db.Database.BeginTransactionAsync(token);
33:             var student = new Student { AcademyId = academyId, FirstName = request.StudentFirstName.Trim(), LastName = request.StudentLastName.Trim(), StudentNumber = request.StudentNumber?.Trim(), PreferredName = request.PreferredName?.Trim(), Gender = request.Gender?.Trim(), DateOfBirth = request.DateOfBirth, AdmissionDate = request.AdmissionDate ?? DateOnly.FromDateTime(DateTime.UtcNow), Email = request.StudentEmail?.Trim(), Phone = request.StudentPhone?.Trim(), AddressLine1 = request.StudentAddressLine1?.Trim(), City = request.StudentCity?.Trim(), State = request.StudentState?.Trim(), PostalCode = request.StudentPostalCode?.Trim(), EmergencyContactName = request.EmergencyContactName?.Trim(), EmergencyContactPhone = request.EmergencyContactPhone?.Trim(), MedicalOrAccessibilityNotes = request.MedicalOrAccessibilityNotes?.Trim(), BranchId = request.BranchId };
34:             db.Students.Add(student);
```

## Suspected root cause

Cross-context transaction scope not coordinated; role assignment results also not checked.

## Business impact and blast radius

Onboarding and other operations spanning academy and Identity stores.

## Related / required regression

STUDENT-DB-001: Failure injection after each identity/database stage; assert no orphan users or incorrect created-account flags.

Also run all endpoint/form cases pointing to this issue in QA/03_TEST_MATRIX.md and the critical regression suite before closure.
