# BUG-DATA-0004 — Student transaction does not encompass Identity account writes

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | STATIC-FINDING |
| Final verification | NOT RUN |
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
| Evidence classification | Static trace; runtime reproduction pending |
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
| Fix commit | Not implemented (Phase 1) |
| Retest result | NOT RUN |
| Regression result | NOT RUN |
| Closure notes | Remain OPEN; follow closure requirements in QA README |

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
