# BUG-DATA-0008 — Blank optional student numbers collide under the unique index

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | STATIC-FINDING |
| Final verification | NOT RUN |
| Severity | Major onboarding persistence |
| Priority | P1 |
| Category | DATA |
| Module | STUDENT |
| Role | See reproduction; same-tenant Admin unless stated otherwise |
| Screen / route | /student-onboarding |
| API | POST /api/academies/{academyId}/student-onboarding |
| Environment | Source review of local working tree; runtime production state not inferred |
| Device/viewport | NOT RUN; use QA/06_DEVICE_VIEWPORT_MATRIX.md where UI applies |
| Baseline | 20bb6047f9edf733ac8e2a226621cc582ec54b3c + pre-existing student UI diff |
| Discovery test / review ID | STUDENT-RULE-002 |
| Evidence classification | Static trace; runtime reproduction pending |
| Preconditions | Isolated synthetic fixture from QA/10_TEST_DATA_STRATEGY.md; never customer data |
| Reproduction frequency | Not measured; reproduction instructions are proposed |
| Source | apps/api/Controllers/StudentOnboardingController.cs:33 |
| Class/function | Create / Student unique index |
| Related source | INVENTORY/API_ENDPOINTS.md, CONTRACTS.md, FORMS_AND_FIELDS.md and ASYNC_FORM_RISKS.md |
| Root-cause confidence | HIGH |
| Evidence location | Source excerpt below; source fingerprint in INVENTORY/SOURCE_MANIFEST.md |
| Screenshot | Not captured on pinned baseline |
| Console logs | Not captured; required in retest |
| API request | In a fresh isolated academy submit two different adult students with valid dates and studentNumber="" (matching the browser form). Repeat with whitespace; compare omitted/null controls. Query Students after each response. |
| API response | Not captured for this issue; use synthetic request/response in isolated reproduction |
| Database before/after | Not executed; fixture and fresh-context assertions defined below |
| Dependencies | Safe SQL/HTTP/browser harness as applicable; desired policy review where noted |
| Fix commit | Not implemented (Phase 1) |
| Retest result | NOT RUN |
| Regression result | NOT RUN |
| Closure notes | Remain OPEN; follow closure requirements in QA README |

## Exact reproduction

In a fresh isolated academy submit two different adult students with valid dates and studentNumber="" (matching the browser form). Repeat with whitespace; compare omitted/null controls. Query Students after each response.

## Expected

Multiple students may omit the optional student number; no blank-number collision or unintended partial identity writes.

## Actual / evidence

FormData includes empty optional studentNumber; controller trims but retains empty string. Unique filtered index covers all non-null (AcademyId, StudentNumber), including empty strings. A second blank number is therefore at risk of DbUpdateException. SQL reproduction NOT RUN.

Source snapshot:

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
