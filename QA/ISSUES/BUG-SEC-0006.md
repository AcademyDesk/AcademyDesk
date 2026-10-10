# BUG-SEC-0006 — Minor guardian portal-access revocation is overridden by age rule

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | RUNTIME-CONFIRMED; bounded repair verified 2026-10-10 |
| Final verification | PARTIAL:90 native SQL/HTTP cases PASS; browser/device/concurrent/broader critical pending |
| Severity | Major access revocation failure |
| Priority | P1 |
| Category | SEC |
| Module | GUARDIAN |
| Role | See reproduction; same-tenant Admin unless stated otherwise |
| Screen / route | Direct guardian portal-access API |
| API | PATCH students/{studentId}/guardians/{guardianId}/portal-access |
| Environment | Source review of local working tree; runtime production state not inferred |
| Device/viewport | NOT RUN; use QA/06_DEVICE_VIEWPORT_MATRIX.md where UI applies |
| Baseline | 20bb6047f9edf733ac8e2a226621cc582ec54b3c + pre-existing student UI diff |
| Discovery test / review ID | SECURITY-GUARDIAN-003 |
| Evidence classification | Static trace; runtime reproduction pending |
| Preconditions | Isolated synthetic fixture from QA/10_TEST_DATA_STRATEGY.md; never customer data |
| Reproduction frequency | Not measured; reproduction instructions are proposed |
| Source | apps/api/Controllers/StudentGuardiansController.cs:54 |
| Class/function | SetPortalAccess |
| Related source | INVENTORY/API_ENDPOINTS.md, CONTRACTS.md, FORMS_AND_FIELDS.md and ASYNC_FORM_RISKS.md |
| Root-cause confidence | HIGH |
| Evidence location | Source excerpt below; source fingerprint in INVENTORY/SOURCE_MANIFEST.md |
| Screenshot | Not captured on pinned baseline |
| Console logs | Not captured; required in retest |
| API request | Seed an active minor with a linked guardian and portal account. As an authorized administrator PATCH allowPortalAccess=false with all four subpermissions explicitly false. Inspect response, fresh link and guardian child/detail access. Repeat adult and unlinked controls; also test omitted subpermissions. |
| API response | Not captured for this issue; use synthetic request/response in isolated reproduction |
| Database before/after | Not executed; fixture and fresh-context assertions defined below |
| Dependencies | Safe SQL/HTTP/browser harness as applicable; desired policy review where noted |
| Fix commit | SetPortalAccess explicit authority repair; see scoped report/history |
| Retest result | 90/90 strict cases; run f717a944a28847deaa1703f522c895c8 |
| Regression result | Existing GuardianFlags native gate and targeted unit tests; results in report |
| Closure notes | Remain OPEN; follow closure requirements in QA README |

## Current bounded repair — 2026-10-10

[Report](../REPORTS/PHASE_2B_GUARDIAN_REVOCATION_REPAIR.md): baseline `bce44b1494ef485f83b7b620faf06dba` confirmed explicitfalse and omitted-subpermission revocations silently retained the minor's grant and child-detail/Me access with the same guardian token. SetPortalAccess now respects the explicit AllowPortalAccess independently of age; initial Link minor defaults and grant subpermission defaults stay unchanged. Final native SQL/HTTP90 cases PASS, checking minor/adult/18th-birthday/unknown-DOB, explicit/omitted revoke, restricted/default regrant, unlink, current-token read visibility, authorization/tenant controls and captured SQL/file preservation. Discovered and separately tracked [BUG-API-0009](BUG-API-0009.md) child-list500 query failure repaired in the same bounded flow. **OPEN** for browser/device, concurrent revocation, broader lifecycle/role and release gates. Historical static evidence below retained, not the current verdict.

## Exact reproduction

Seed an active minor with a linked guardian and portal account. As an authorized administrator PATCH allowPortalAccess=false with all four subpermissions explicitly false. Inspect response, fresh link and guardian child/detail access. Repeat adult and unlinked controls; also test omitted subpermissions.

## Expected

An explicit revocation is respected, or rejected clearly with an actionable policy explanation; it must not silently regrant access. Requirements for a minor to have a guardian must not make each individual guardian irrevocable.

## Actual / evidence

isMinor OR request.AllowPortalAccess forces CanAccessPortal=true, resets AccessRevokedAtUtc to null and updates grant time. Omitted subpermissions default true. Child identity/contact access remains available with all subpermissions false. Deleting the link is a separate available operation, not revocation through this endpoint.

Source snapshot:

```text
53:         var isMinor = link.Student.DateOfBirth.HasValue && link.Student.DateOfBirth.Value.AddYears(18) > DateOnly.FromDateTime(DateTime.UtcNow);
54:         var access = isMinor || request.AllowPortalAccess;
55:         link.CanAccessPortal = access;
56:         link.CanViewAcademicProgress = access && request.AllowAcademicProgress;
```

## Suspected root cause

Default minor-access policy overrides an explicit per-guardian revocation command.

## Business impact and blast radius

Minor guardian links using the portal-access endpoint; current guardian page does not expose this endpoint.

## Related / required regression

SECURITY-GUARDIAN-003: Real HTTP/SQL matrix for minor/adult/18th-birthday/unknown DOB, explicit revoke, omitted vs false flags, regrant and unlink; verify both response and new guardian reads deny after revocation.

Also run all endpoint/form cases pointing to this issue in QA/03_TEST_MATRIX.md and the critical regression suite before closure.
