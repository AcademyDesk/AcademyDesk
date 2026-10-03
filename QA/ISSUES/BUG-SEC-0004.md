# BUG-SEC-0004 — Standard staff role replacement retains custom-role permissions

## Current repair checkpoint — 2026-10-03

**PARTIALLY FIXED / LOCAL BOUNDED REPAIR VERIFIED.** Standard staff replacement removes obsolete custom roles, discloses actual remaining memberships, preserves protected identities/explicit grants, checks stamp failure and atomically saves Identity plus attributed success audit, including the existing same-academy platform-bypass actor.

44/44 targeted backend tests;37 strict native core observations;56 extended cases plus1 explicit-grant read accepted.21 injected failures roll back and21 retries succeed across Admin/Owner/bypass.46 unchanged fresh SQL snapshots. Owned run/SQL/root/container cleaned,0 listeners;1019 immutable predecessor pins preserved. No frontend/auth rewrite/schema/dev/Azure/deployment change. See [bounded repair report](../REPORTS/PHASE_2B_STAFF_ROLE_REPLACEMENT.md). Concurrent edits, other role-sensitive routes, grant-expiry/module variants/browser recovery and broader identity/session/release remain OPEN.

## Historical runtime checkpoint — 2026-10-02

| Field | Current result |
| --- | --- |
| Status | OPEN — product not fixed |
| Confirmation status | RUNTIME-REPRODUCED |
| Final verification | Two fresh native HTTP/SQL diagnostics;35 controls accepted and2 policy failures per run,not full acceptance |
| Evidence | [Existing-session diagnostic](../REPORTS/PHASE_2B_ROLE_REVOCATION_DIAGNOSTIC.md) |

Actual standard replacement PATCH200 reports Operations only,while fresh SQL retains Operations+QA-Finance+QA-Messages. A fresh normal login reads the real synthetic invoice200,expected403. Old access/refresh correctly401 after stamp rotation;the accepted authentication repair is preserved. Actual custom-role assignment removes custom memberships and the same bearer loses finance403;live grant add/revoke and offboard controls pass. Two30-request runs/37 observations each,18 unchanged snapshots/six attributed success audits;owned resources fully cleaned. QA-only,no product/Azure/deploy change. Next narrow replacement/actual-response/rollback-safe repair,with protected identities and independent grants explicitly covered;broader gates OPEN.

## Historical Phase 1 classification and proposed reproduction

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | STATIC-FINDING |
| Final verification | NOT RUN |
| Severity | Major retained privilege |
| Priority | P1 |
| Category | SEC |
| Module | AUTH |
| Role | See reproduction; same-tenant Admin unless stated otherwise |
| Screen / route | /staff |
| API | PATCH staff/{staffId}/role |
| Environment | Source review of local working tree; runtime production state not inferred |
| Device/viewport | NOT RUN; use QA/06_DEVICE_VIEWPORT_MATRIX.md where UI applies |
| Baseline | 20bb6047f9edf733ac8e2a226621cc582ec54b3c + pre-existing student UI diff |
| Discovery test / review ID | SECURITY-ROLE-004 |
| Evidence classification | Static trace; runtime reproduction pending |
| Preconditions | Isolated synthetic fixture from QA/10_TEST_DATA_STRATEGY.md; never customer data |
| Reproduction frequency | Not measured; reproduction instructions are proposed |
| Source | apps/api/Controllers/StaffController.cs:102 |
| Class/function | UpdateRole |
| Related source | INVENTORY/API_ENDPOINTS.md, CONTRACTS.md, FORMS_AND_FIELDS.md and ASYNC_FORM_RISKS.md |
| Root-cause confidence | HIGH |
| Evidence location | Source excerpt below; source fingerprint in INVENTORY/SOURCE_MANIFEST.md |
| Screenshot | Not captured on pinned baseline |
| Console logs | Not captured; required in retest |
| API request | Create a synthetic custom role with finance.manage and assign it to a staff user. As academy admin change that user to Operations through the staff role endpoint. Read persisted role memberships and call a Finance API as that user, with relevant module enabled and no grants. |
| API response | Not captured for this issue; use synthetic request/response in isolated reproduction |
| Database before/after | Not executed; fixture and fresh-context assertions defined below |
| Dependencies | Safe SQL/HTTP/browser harness as applicable; desired policy review where noted |
| Fix commit | Not implemented (Phase 1) |
| Retest result | NOT RUN |
| Regression result | NOT RUN |
| Closure notes | Remain OPEN; follow closure requirements in QA README |

## Exact reproduction

Create a synthetic custom role with finance.manage and assign it to a staff user. As academy admin change that user to Operations through the staff role endpoint. Read persisted role memberships and call a Finance API as that user, with relevant module enabled and no grants.

## Expected

A replacement represented as Operations-only must remove obsolete custom-role authority or explicitly preserve and display additive roles. Response must accurately disclose resulting membership.

## Actual / evidence

Only the six AllowedRoles are removed. Custom roles remain assigned and AcademyAccessFilter unions their permissions. Response reports only requestedRole, concealing retained membership. Static finding; access probe NOT RUN.

Source snapshot:

```text
101:         var currentRoles = await userManager.GetRolesAsync(staff);
102:         var removableRoles = currentRoles.Where(role => AllowedRoles.Contains(role, StringComparer.OrdinalIgnoreCase)).ToArray();
103:         if (removableRoles.Length > 0)
104:         {
```

## Suspected root cause

Standard-role replacement and effective permission union disagree about retained custom roles.

## Business impact and blast radius

Staff role reduction, finance and other custom permissions; security-stamp update does not remove persisted roles.

## Related / required regression

SECURITY-ROLE-004: Custom finance role to Operations, multiple custom roles, ordinary standard replacement, protected admin roles and explicit grants; compare response, fresh Identity memberships and actual allowed/denied calls.

Also run all endpoint/form cases pointing to this issue in QA/03_TEST_MATRIX.md and the critical regression suite before closure.
