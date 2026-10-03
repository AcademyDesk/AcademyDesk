# BUG-FUNC-0028 — Manager consent-management permission is blocked by the global access filter

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | REPRODUCED — bounded local permission repair verified |
| Final verification | Catalog/controlled page/real global-filter HTTP-SQL PASS; browser/lint/critical OPEN |
| Severity | Major authorized-role workflow unavailable |
| Priority | P1 |
| Category | FUNC |
| Module | COMMUNICATION |
| Role | See reproduction; same-tenant Admin unless stated otherwise |
| Screen / route | /communication-preferences |
| API | GET/PUT communication-preferences |
| Environment | Source review of local working tree; runtime production state not inferred |
| Device/viewport | NOT RUN; use QA/06_DEVICE_VIEWPORT_MATRIX.md where UI applies |
| Baseline | 20bb6047f9edf733ac8e2a226621cc582ec54b3c + pre-existing student UI diff |
| Discovery test / review ID | COMMUNICATION-PREFERENCE-ACCESS-001 |
| Evidence classification | Static trace; runtime reproduction pending |
| Preconditions | Isolated synthetic fixture from QA/10_TEST_DATA_STRATEGY.md; never customer data |
| Reproduction frequency | Not measured; reproduction instructions are proposed |
| Source | apps/api/Controllers/CommunicationPreferencesController.cs:43 |
| Class/function | CanManage / AcademyAccessFilter / PermissionCatalog |
| Related source | INVENTORY/API_ENDPOINTS.md, CONTRACTS.md, FORMS_AND_FIELDS.md and ASYNC_FORM_RISKS.md |
| Root-cause confidence | HIGH |
| Evidence location | Source excerpt below; source fingerprint in INVENTORY/SOURCE_MANIFEST.md |
| Screenshot | Not captured on pinned baseline |
| Console logs | Not captured; required in retest |
| API request | Use a same-tenant active Manager with Engagement enabled in a full MVC test host. Request preference list and update for local student; compare AcademyAdmin and direct CanManage intent. |
| API response | Not captured for this issue; use synthetic request/response in isolated reproduction |
| Database before/after | Not executed; fixture and fresh-context assertions defined below |
| Dependencies | Safe SQL/HTTP/browser harness as applicable; desired policy review where noted |
| Fix commit | Local uncommitted catalog/controller/narrow-lookup/page repair; no deployment |
| Retest result | 609 backend (16 new),10 controlled TSX,74 real Identity/global-filter/HTTP-SQL cases PASS |
| Regression result | Owner/Admin/Manager/delegated/denied/scope/suspended/module/audit/no-write boundaries verified; browser/critical NOT RUN |
| Closure notes | Remain OPEN; follow closure requirements in QA README |

## Local repair checkpoint — 2026-10-01

[Repair/evidence/limits](../REPORTS/PHASE_2B_PREFERENCE_ACCESS_REPAIR.md). Four blocked Manager HTTP paths reproduced, plus2 catalog and3 controlled-page baseline failures. The existing communications.manage catalog now reaches preference actions, with Manager grant and current custom-role/individual grants handled by the unchanged global filter. Active-account/same-tenant guard and platform-only boundary retained. New minimally scoped recipient lookup replaces forbidden full people records. Final609 backend/10 controlled TSX/74 real HTTP-SQL cases PASS. Other communications and full people management remain denied to Manager. Schema/dev services/Azure unchanged; inherited lint1 error/1 warning, browser/async feedback/races/audit rollback/critical/release remain OPEN. Static evidence below is historical.

## Exact reproduction (historical baseline)

Use a same-tenant active Manager with Engagement enabled in a full MVC test host. Request preference list and update for local student; compare AcademyAdmin and direct CanManage intent.

## Expected

The Manager role explicitly allowed by the action can reach intended consent management with narrowly scoped supporting lookups, or role policy is consistently documented and enforced as admin-only.

## Actual / evidence

CanManage includes Manager but global filter denies nonadministrators when controller lacks a permission mapping. CommunicationPreferencesController is unmapped, so Manager never reaches this check. Page also needs students/guardians lookups that Manager lacks. Runtime NOT RUN.

Source snapshot:

```text
42:
43:     private async Task<bool> CanManage(Guid academyId) { var user = await userManager.GetUserAsync(User); return user?.AcademyId == academyId && (await userManager.IsInRoleAsync(user, "Owner") || await userManager.IsInRoleAsync(user, "AcademyAdmin") || await userManager.IsInRoleAsync(user, "Manager")); }
44:     private static CommunicationPreferenceSummary ToSummary(CommunicationPreference x) => new(x.Id, x.RecipientId, x.RecipientType, x.EmailAllowed, x.WhatsAppAllowed, x.MarketingAllowed, x.EmailOptedInAtUtc, x.WhatsAppOptedInAtUtc, x.OptedOutAtUtc, x.Notes);
45: }
```

## Suspected root cause

Action role policy conflicts with global controller catalog and lookup dependencies.

## Business impact and blast radius

Manager consent management despite action-level allowance; no privilege bypass claimed.

## Related / required regression

COMMUNICATION-PREFERENCE-ACCESS-001: Full pipeline Manager/Owner/AcademyAdmin/delegated/denied/foreign/suspended/module-off matrix and browser least-privilege lookup failures; never bypass the global filter to make tests pass.

Also run all endpoint/form cases pointing to this issue in QA/03_TEST_MATRIX.md and the critical regression suite before closure.
