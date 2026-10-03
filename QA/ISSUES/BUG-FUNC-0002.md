# BUG-FUNC-0002 — Granted onboarding permission conflicts with action-level admin check

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | STATIC-FINDING |
| Final verification | NOT RUN |
| Severity | Major workflow |
| Priority | P1 |
| Category | FUNC |
| Module | STUDENT |
| Role | See reproduction; same-tenant Admin unless stated otherwise |
| Screen / route | /student-onboarding |
| API | POST student-onboarding |
| Environment | Source review of local working tree; runtime production state not inferred |
| Device/viewport | NOT RUN; use QA/06_DEVICE_VIEWPORT_MATRIX.md where UI applies |
| Baseline | 20bb6047f9edf733ac8e2a226621cc582ec54b3c + pre-existing student UI diff |
| Discovery test / review ID | SECURITY-ROLE-002 |
| Evidence classification | Static trace; runtime reproduction pending |
| Preconditions | Isolated synthetic fixture from QA/10_TEST_DATA_STRATEGY.md; never customer data |
| Reproduction frequency | Not measured; reproduction instructions are proposed |
| Source | apps/api/Controllers/StudentOnboardingController.cs:20 |
| Class/function | Create plus PermissionCatalog.ForSystemRole |
| Related source | INVENTORY/API_ENDPOINTS.md, CONTRACTS.md, FORMS_AND_FIELDS.md and ASYNC_FORM_RISKS.md |
| Root-cause confidence | HIGH |
| Evidence location | Source excerpt below; source fingerprint in INVENTORY/SOURCE_MANIFEST.md |
| Screenshot | Not captured on pinned baseline |
| Console logs | Not captured; required in retest |
| API request | Seed Sales, Marketing and FrontDesk accounts in active enabled academy. Verify students.onboard permission, then call onboarding with valid body. |
| API response | Not captured for this issue; use synthetic request/response in isolated reproduction |
| Database before/after | Not executed; fixture and fresh-context assertions defined below |
| Dependencies | Safe SQL/HTTP/browser harness as applicable; desired policy review where noted |
| Fix commit | Not implemented (Phase 1) |
| Retest result | NOT RUN |
| Regression result | NOT RUN |
| Closure notes | Remain OPEN; follow closure requirements in QA README |

## Exact reproduction

Seed Sales, Marketing and FrontDesk accounts in active enabled academy. Verify students.onboard permission, then call onboarding with valid body.

## Expected

Documented permission-based workflow is consistent; either role can onboard or permission/UI must explicitly deny it.

## Actual / evidence

Global catalog grants students.onboard; controller separately requires Owner/AcademyAdmin. Those staff roles pass global permission then are forbidden by action.

Source snapshot:

```text
19:         var actor = await users.GetUserAsync(User);
20:         if (actor?.AcademyId != academyId || (!await users.IsInRoleAsync(actor, "Owner") && !await users.IsInRoleAsync(actor, "AcademyAdmin"))) return Forbid();
21:         if (string.IsNullOrWhiteSpace(request.StudentFirstName) || string.IsNullOrWhiteSpace(request.StudentLastName) || request.DateOfBirth is null) return BadRequest(new { message = "Student name and date of birth are required." });
22:         if (request.DateOfBirth > DateOnly.FromDateTime(DateTime.UtcNow)) return BadRequest(new { message = "Date of birth cannot be in the future." });
```

## Suspected root cause

Catalog and endpoint-specific policy disagree.

## Business impact and blast radius

Staff onboarding and other endpoints with stricter admin checks.

## Related / required regression

SECURITY-ROLE-002: Full-pipeline matrix for grant/role combinations; approve desired policy before repair.

Also run all endpoint/form cases pointing to this issue in QA/03_TEST_MATRIX.md and the critical regression suite before closure.
