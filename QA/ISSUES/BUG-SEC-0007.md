# BUG-SEC-0007 — New portal accounts can restore access to inactive guardian records

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | STATIC-FINDING |
| Final verification | NOT RUN |
| Severity | Major guardian lifecycle bypass |
| Priority | P1 |
| Category | SEC |
| Module | AUTH |
| Role | See reproduction; same-tenant Admin unless stated otherwise |
| Screen / route | /portal-accounts; /portal |
| API | POST portal-accounts; GET portal/me; GET portal/students/{studentId} |
| Environment | Source review of local working tree; runtime production state not inferred |
| Device/viewport | NOT RUN; use QA/06_DEVICE_VIEWPORT_MATRIX.md where UI applies |
| Baseline | 20bb6047f9edf733ac8e2a226621cc582ec54b3c + pre-existing student UI diff |
| Discovery test / review ID | SECURITY-GUARDIAN-004 |
| Evidence classification | Static trace; runtime reproduction pending |
| Preconditions | Isolated synthetic fixture from QA/10_TEST_DATA_STRATEGY.md; never customer data |
| Reproduction frequency | Not measured; reproduction instructions are proposed |
| Source | apps/api/Controllers/PortalAccountsController.cs:23 |
| Class/function | Create; Portal.Me / ParentAccess |
| Related source | INVENTORY/API_ENDPOINTS.md, CONTRACTS.md, FORMS_AND_FIELDS.md and ASYNC_FORM_RISKS.md |
| Root-cause confidence | HIGH |
| Evidence location | Source excerpt below; source fingerprint in INVENTORY/SOURCE_MANIFEST.md |
| Screenshot | Not captured on pinned baseline |
| Console logs | Not captured; required in retest |
| API request | Seed a same-academy inactive guardian retaining an enabled link to an active child. As an academy administrator create a new Guardian account with a unique synthetic email. Sign in and request child details. Compare inactive Identity, active guardian and unlinked controls. |
| API response | Not captured for this issue; use synthetic request/response in isolated reproduction |
| Database before/after | Not executed; fixture and fresh-context assertions defined below |
| Dependencies | Safe SQL/HTTP/browser harness as applicable; desired policy review where noted |
| Fix commit | Not implemented (Phase 1) |
| Retest result | NOT RUN |
| Regression result | NOT RUN |
| Closure notes | Remain OPEN; follow closure requirements in QA README |

## Exact reproduction

Seed a same-academy inactive guardian retaining an enabled link to an active child. As an academy administrator create a new Guardian account with a unique synthetic email. Sign in and request child details. Compare inactive Identity, active guardian and unlinked controls.

## Expected

Inactive guardian cannot gain usable child access through account provisioning without an explicit domain reactivation.

## Actual / evidence

Provisioning checks guardian existence but not IsActive; new ApplicationUser defaults active. Portal.Me and ParentAccess do not check Guardian.IsActive. Ordinary guardian deactivation updates existing accounts, but a newly provisioned active account bypasses that synchronization.

Source snapshot:

```text
22:         if (role == "Student" && (!request.StudentId.HasValue || !await db.Students.AnyAsync(x => x.Id == request.StudentId && x.AcademyId == academyId, token))) return BadRequest(new { message = "Select a valid student." });
23:         if (role == "Guardian" && (!request.GuardianId.HasValue || !await db.Guardians.AnyAsync(x => x.Id == request.GuardianId && x.AcademyId == academyId, token))) return BadRequest(new { message = "Select a valid guardian." });
24:         if (role == "Teacher" && (!request.TeacherId.HasValue || !await db.Teachers.AnyAsync(x => x.Id == request.TeacherId && x.AcademyId == academyId, token))) return BadRequest(new { message = "Select a valid teacher." });
25:         if (!await roles.RoleExistsAsync(role)) await roles.CreateAsync(new ApplicationRole { Name = role });
```

## Suspected root cause

Domain lifecycle enforced by updating existing identities rather than checked consistently at provisioning and read time.

## Business impact and blast radius

Administratively provisioned accounts for deactivated guardians; also relevant if deactivation identity synchronization fails. Not an anonymous or unprivileged provisioning exploit.

## Related / required regression

SECURITY-GUARDIAN-004: Real HTTP/SQL provisioning and portal reads before/after deactivation, new identity and reactivation; fresh rows and no unauthorized child data. Preserve admin authorization and denied inactive-Identity control.

Also run all endpoint/form cases pointing to this issue in QA/03_TEST_MATRIX.md and the critical regression suite before closure.
