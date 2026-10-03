# BUG-DATA-0012 — Academy-scoped role names conflict with global Identity uniqueness

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | STATIC-FINDING |
| Final verification | NOT RUN |
| Severity | Major tenant configuration availability |
| Priority | P1 |
| Category | DATA |
| Module | AUTH |
| Role | See reproduction; same-tenant Admin unless stated otherwise |
| Screen / route | API-only custom roles |
| API | POST /api/academies/{academyId}/roles |
| Environment | Source review of local working tree; runtime production state not inferred |
| Device/viewport | NOT RUN; use QA/06_DEVICE_VIEWPORT_MATRIX.md where UI applies |
| Baseline | 20bb6047f9edf733ac8e2a226621cc582ec54b3c + pre-existing student UI diff |
| Discovery test / review ID | AUTH-ROLE-001 |
| Evidence classification | Static trace; runtime reproduction pending |
| Preconditions | Isolated synthetic fixture from QA/10_TEST_DATA_STRATEGY.md; never customer data |
| Reproduction frequency | Not measured; reproduction instructions are proposed |
| Source | apps/api/Controllers/AcademyRolesController.cs:30 |
| Class/function | Create |
| Related source | INVENTORY/API_ENDPOINTS.md, CONTRACTS.md, FORMS_AND_FIELDS.md and ASYNC_FORM_RISKS.md |
| Root-cause confidence | HIGH |
| Evidence location | Source excerpt below; source fingerprint in INVENTORY/SOURCE_MANIFEST.md |
| Screenshot | Not captured on pinned baseline |
| Console logs | Not captured; required in retest |
| API request | As tenant A admin create custom role TutorCoordinator. As tenant B admin create the same display name, then a case variant. Compare tenant-local duplicate control and fresh role rows in isolated SQL. |
| API response | Not captured for this issue; use synthetic request/response in isolated reproduction |
| Database before/after | Not executed; fixture and fresh-context assertions defined below |
| Dependencies | Safe SQL/HTTP/browser harness as applicable; desired policy review where noted |
| Fix commit | Not implemented (Phase 1) |
| Retest result | NOT RUN |
| Regression result | NOT RUN |
| Closure notes | Remain OPEN; follow closure requirements in QA README |

## Exact reproduction

As tenant A admin create custom role TutorCoordinator. As tenant B admin create the same display name, then a case variant. Compare tenant-local duplicate control and fresh role rows in isolated SQL.

## Expected

Academy-scoped custom role names can be reused by independent academies while duplicate names within one academy reject clearly.

## Actual / evidence

Controller only checks duplicate within academy, but Identity snapshot retains global unique NormalizedName RoleNameIndex in addition to tenant Name index. Global Identity validation/index can reject the other academy role.

Source snapshot:

```text
29:         var name = request.Name.Trim();
30:         if (await roles.Roles.AnyAsync(role => role.AcademyId == academyId && role.Name == name)) return Conflict(new { message = "That academy role already exists." });
31:         var role = new ApplicationRole { Name = name, AcademyId = academyId, IsSystemRole = false, PermissionsJson = JsonSerializer.Serialize(request.Permissions?.Distinct() ?? []) };
32:         var result = await roles.CreateAsync(role);
```

## Suspected root cause

Tenant-local role naming contract uses global Identity name namespace.

## Business impact and blast radius

Custom-role setup across academies and case variants; does not prove cross-tenant permission disclosure.

## Related / required regression

AUTH-ROLE-001: Real Identity/SQL same-name roles across two tenants, same-tenant duplicates, system-role collisions, case and whitespace; preserve unambiguous assignment/authorization after any design change.

Also run all endpoint/form cases pointing to this issue in QA/03_TEST_MATRIX.md and the critical regression suite before closure.
