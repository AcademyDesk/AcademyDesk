# BUG-DATA-0022 — Unknown subscription plan silently replaces tenant configuration with Launch

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | RUNTIME-REPRODUCED / bounded local repair |
| Final verification | 48/48 real Identity/HTTP-SQL and1019/1019 existing backend rerun PASS; browser/critical/concurrency/fault gates OPEN |
| Severity | Major unintended entitlement change |
| Priority | P1 |
| Category | DATA |
| Module | PLATFORM |
| Role | See reproduction; same-tenant Admin unless stated otherwise |
| Screen / route | /platform/control?tab=Tenants; direct configuration API |
| API | PUT /api/platform/academies/{academyId}/configuration |
| Environment | Source review of local working tree; runtime production state not inferred |
| Device/viewport | NOT RUN; use QA/06_DEVICE_VIEWPORT_MATRIX.md where UI applies |
| Baseline | 20bb6047f9edf733ac8e2a226621cc582ec54b3c + pre-existing student UI diff |
| Discovery test / review ID | PLATFORM-PLAN-001 |
| Evidence classification | Accepted static trace plus real owner configuration/Identity/HTTP/fresh SQL baseline and repair evidence |
| Preconditions | Isolated synthetic fixture from QA/10_TEST_DATA_STRATEGY.md; never customer data |
| Reproduction frequency | Three unsafe owned-SQL catalog fallbacks reproduced; one final48-case owned-SQL run |
| Source | apps/api/Controllers/PlatformAcademiesController.cs:66 |
| Class/function | Configure / SubscriptionPlanCatalog.Get |
| Related source | INVENTORY/API_ENDPOINTS.md, CONTRACTS.md, FORMS_AND_FIELDS.md and ASYNC_FORM_RISKS.md |
| Root-cause confidence | HIGH |
| Evidence location | Source excerpt below; source fingerprint in INVENTORY/SOURCE_MANIFEST.md |
| Screenshot | Not captured on pinned baseline |
| Console logs | Not captured; required in retest |
| API request | In isolated fixtures set tenant to Professional. As platform owner submit an unknown nonblank plan or Professional with trailing space and otherwise valid status/date. Read fresh plan, limits and modules. Compare exact and case-variant valid plans. |
| API response | Baseline invalid nonblank plans returned200/Launch75/15; final unknown names400 and supported plans exact catalog values |
| Database before/after | Twelve exact accepted configuration/audit requests;36 unchanged captured reads/rejections, including35 rejected requests |
| Dependencies | Safe SQL/HTTP/browser harness as applicable; desired policy review where noted |
| Fix commit | Local uncommitted Configure-only guard; HEAD unchanged, no push/deployment |
| Retest result | PARTIAL PASS — plan/case/whitespace/null/model-binding/preservation/role/HTTP-SQL; browser/critical OPEN |
| Regression result | 48 HTTP-SQL and1019 existing backend rerun PASS; owned cleanup; broader gates OPEN |
| Closure notes | Remain OPEN; follow closure requirements in QA README |

## Exact reproduction

In isolated fixtures set tenant to Professional. As platform owner submit an unknown nonblank plan or Professional with trailing space and otherwise valid status/date. Read fresh plan, limits and modules. Compare exact and case-variant valid plans.

## Expected

Unknown plan rejects without changing configuration; supported normalization is explicit. An invalid request must not silently reduce entitlements.

## Actual / evidence

Only nonblank validation runs. Catalog lookup is case-insensitive but unknown or untrimmed names fall back to Launch; Configure overwrites plan, limits and modules and returns success. UI offers valid values, so direct API or stale client is required. Runtime NOT RUN.

The above describes the historical Phase1 source snapshot. Local successor2026-10-02 reproduced three such fallbacks against disposable SQL, then guarded Configure only. Production runtime state is not inferred.

Source snapshot:

```text
65:         if (string.IsNullOrWhiteSpace(request.SubscriptionPlan) || string.IsNullOrWhiteSpace(request.SubscriptionStatus)) return BadRequest(new { message = "Subscription plan and status are required." });
66:         var plan = SubscriptionPlanCatalog.Get(request.SubscriptionPlan);
67:         academy.SubscriptionPlan = plan.Name;
68:         academy.SubscriptionStatus = request.SubscriptionStatus.Trim();
```

## Suspected root cause

Catalog fallback intended as a default is used for an explicit administrative mutation.

## Business impact and blast radius

Platform-owner tenant configuration; unintended module/capacity downgrade on invalid plan. No unprivileged access bypass claimed.

## Related / required regression

PLATFORM-PLAN-001: Real HTTP/SQL valid/case/whitespace/unknown/null/empty plan matrix. Rejected input preserves all tenant configuration and creates no success audit; valid upgrades/downgrades match approved catalog exactly.

Also run all endpoint/form cases pointing to this issue in QA/03_TEST_MATRIX.md and the critical regression suite before closure.

## Local successor — 2026-10-02

[Repair/evidence](../REPORTS/PHASE_2B_TENANT_PLAN_REPAIR.md): Configure rejects an unsupported catalog name before assignment/audit. Case-insensitive valid names retain canonical definitions; whitespace-padded values reject, not normalize to Launch. Shared catalog defaults, onboarding, status/expiry semantics, auth/DTO/normal save/audit behavior unchanged.

48/48 real Identity/HTTP-SQL cases PASS: all five plans/case variants, exact independent limits/modules/response/audit metadata, null/present expiry, status trimming, repeat and owner other-tenant/inactive compatibility.35 rejected requests plus one list read have identical full captured snapshots;12 accepted configuration requests have12 distinct success audits and unchanged unrelated tenant fields/collections. Separately1019/1019 existing backend rerun PASS. Builds0 warnings/errors;618 accepted entries pinned. Both owned databases/logins/containers/temp roots/ports removed; no failed build/runtime attempt.

Issue/Phase2B/release remain OPEN for browser/editor feedback, linked critical suite, concurrency/fault rollback and broader status/expiry/downgrade/legacy policy. No recovery of prior customer fallback changes, dev/customer DB mutation, normal services/assemblies, Azure/provider, commit/push/deploy. Next adjacent accepted BUG-FUNC-0009 default trial duration, same Sol High.
