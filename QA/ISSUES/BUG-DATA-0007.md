# BUG-DATA-0007 — Student onboarding account-created flags can report an account that was not created

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | RUNTIME-CONFIRMED; bounded response repair tested |
| Final verification | 33 native SQL/Identity/HTTP cases PASS; linked browser/device/full critical acceptance OPEN |
| Severity | Major response integrity |
| Priority | P1 |
| Category | DATA |
| Module | STUDENT |
| Role | See reproduction; same-tenant Admin unless stated otherwise |
| Screen / route | /student-onboarding |
| API | POST student-onboarding |
| Environment | Source review of local working tree; runtime production state not inferred |
| Device/viewport | NOT RUN; use QA/06_DEVICE_VIEWPORT_MATRIX.md where UI applies |
| Baseline | 20bb6047f9edf733ac8e2a226621cc582ec54b3c + pre-existing student UI diff |
| Discovery test / review ID | STUDENT-API-001 |
| Evidence classification | Historical static trace plus disposable-SQL HTTP reproduction and strict regression |
| Preconditions | Isolated synthetic fixture from QA/10_TEST_DATA_STRATEGY.md; never customer data |
| Reproduction frequency | Not measured; reproduction instructions are proposed |
| Source | apps/api/Controllers/StudentOnboardingController.cs:49 |
| Class/function | Create |
| Related source | INVENTORY/API_ENDPOINTS.md, CONTRACTS.md, FORMS_AND_FIELDS.md and ASYNC_FORM_RISKS.md |
| Root-cause confidence | HIGH |
| Evidence location | Source excerpt below; source fingerprint in INVENTORY/SOURCE_MANIFEST.md |
| Screenshot | Not captured on pinned baseline |
| Console logs | Not captured; required in retest |
| API request | Submit an adult synthetic student with username but no temporary password. Read response flags and query Identity store. |
| API response | Not captured for this issue; use synthetic request/response in isolated reproduction |
| Database before/after | Not executed; fixture and fresh-context assertions defined below |
| Dependencies | Safe SQL/HTTP/browser harness as applicable; desired policy review where noted |
| Fix commit | Feature commit containing PHASE_2B_ONBOARDING_ACCOUNT_FLAGS_REPAIR.md; no main/Azure deployment |
| Retest result | 33 native cases PASS; see latest checkpoint |
| Regression result | Unchanged21 native transaction cases and56 targeted unit tests PASS |
| Closure notes | Remain OPEN; follow closure requirements in QA README |

## Latest bounded checkpoint — 2026-10-10

[Report](../REPORTS/PHASE_2B_ONBOARDING_ACCOUNT_FLAGS_REPAIR.md), starting HEAD70f1637. Native baseline `11a905dd8a874305908f6324ce0bdaa4` passed the omitted-credentials control then reproduced the parent username-only false-positive flag on HTTP200. This baseline stopped at the flag assertion before that failed case's fresh account query; it is not claimed as an independently enumerated account-count receipt. The same student and parent source defect is covered by the final matrix.

Flags now start false and become true only after the corresponding CreateAccount call, including checked role assignment, succeeds. Existing optional incomplete-credential behavior is retained, not replaced with mandatory passwords. The previous SQL domain/Identity/audit transaction and authorization remain unchanged.

Final run `5f36b5def8d6444095ced20cf91819c6`:33/33 native cases PASS, including all16 student/parent neither/username-only/password-only/both combinations; null/empty/whitespace, absent guardian, minor defaults, denied and invalid-password rollback controls. Fresh SQL verifies exact domain/account/membership/audit deltas and actual account/role linkage before response flags are accepted. Unchanged transaction retest `9377d527f7f041478684753bf447a00e`21/21 and56 targeted unit tests PASS. Builds0/0,88 application/7 Identity migrations and exact owned cleanup verified; evidence local.

Issue remains OPEN for linked browser, physical Android/iOS and full critical workflow acceptance. Blank optional student-number collisions (BUG-DATA-0008), other intake fields/branch/uniqueness concerns and broader pending/unknown-result UX are separate, not repaired or waived here. Historical entries below retain their original baseline context.

## Exact reproduction

Submit an adult synthetic student with username but no temporary password. Read response flags and query Identity store.

## Expected

Response truthfully states no account exists, or rejects an incomplete account request with field error.

## Actual / evidence

Account creation requires username AND password; returned flags only check username (and parent presence).

Source snapshot:

```text
48:             await transaction.CommitAsync(token);
49:             return Ok(new { student.Id, student.FirstName, student.LastName, IsMinor = isMinor, ParentId = parent?.Id, StudentAccountCreated = !string.IsNullOrWhiteSpace(request.StudentUserName), ParentAccountCreated = parent is not null && !string.IsNullOrWhiteSpace(request.ParentUserName) });
50:         }
51:         catch (DbUpdateException)
```

## Suspected root cause

Response computes intent instead of confirmed creation result.

## Business impact and blast radius

User thinks portal access is ready although no account exists.

## Related / required regression

STUDENT-API-001: Neither/username-only/password-only/both supplied; assert actual Identity row/role and exact JSON flags.

Also run all endpoint/form cases pointing to this issue in QA/03_TEST_MATRIX.md and the critical regression suite before closure.
