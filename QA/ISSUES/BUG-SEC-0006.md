# BUG-SEC-0006 — Minor guardian portal-access revocation is overridden by age rule

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | C29 reproduced portal/link defects; guard-only provisioning checks separately labelled |
| Final verification | C29 scoped Cloud HTTP/controller controls PASS; SQL/audit/races/browser/device and full acceptance pending |
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
| Fix commit | SOURCE_PIN_PENDING (C29 feature return; no deployment) |
| Retest result | 31 actual HTTP/Identity/InMemory portal/link cases plus5 direct provisioning guard checks PASS;59 retained checks PASS |
| Regression result | Final full API2,389/2,389 PASS;unchanged web evidence reused exactly from C28,not rerun |
| Closure notes | Remain OPEN; follow closure requirements in QA README |

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

## C29 guardian privacy source repair — 10 November 2026

Historical source evidence above is retained. Explicit per-link revocation no
longer regrants a minor guardian. Missing intent rejects; initial minor bootstrap
policy is preserved. Current inactive Guardian domain checks block child access
and inactive account provisioning without changing Identity/session authority.

New original33 controls14 PASS/19 FAIL:28 HTTP portal/link cases11 PASS/17 FAIL;
5 direct provisioning guards3 PASS/2 missing-SQL-prerequisite failures. Final36
new controls and59 retained family/media/Identity controls PASS. Provisioning
checks are guard-only, not a successful real HTTP/SQL account-creation proof.
No transaction boundary was bypassed to create a synthetic pass.

[Exact source/result boundary and ordered native SQL/audit/race/browser/device tasks](../HANDOFF/C29_GUARDIAN_ACCESS_2026-11-10.md).

Remain OPEN for genuine provisioning SQL success/refusal/rollback, revocation
atomic audit and concurrency, actual role/browser/device, linked critical and
documented acceptance. Five P0/broader-session/release and model gates remain.
