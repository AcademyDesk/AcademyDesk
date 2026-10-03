# BUG-FUNC-0009 — Academy onboarding ignores configured default trial duration

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | ACCEPTED SOURCE FINDING / bounded local repair verified |
| Final verification | 22/22 final real Identity/HTTP-SQL and1019 existing backend rerun PASS; no new runtime baseline; broader gates OPEN |
| Severity | Moderate subscription configuration inconsistency |
| Priority | P2 |
| Category | FUNC |
| Module | PLATFORM |
| Role | See reproduction; same-tenant Admin unless stated otherwise |
| Screen / route | /platform; /platform/control?tab=Tenant%20onboarding |
| API | POST /api/platform/academies |
| Environment | Source review of local working tree; runtime production state not inferred |
| Device/viewport | NOT RUN; use QA/06_DEVICE_VIEWPORT_MATRIX.md where UI applies |
| Baseline | 20bb6047f9edf733ac8e2a226621cc582ec54b3c + pre-existing student UI diff |
| Discovery test / review ID | PLATFORM-TRIAL-001 |
| Evidence classification | Accepted source baseline plus final real settings-save/onboarding/Identity/HTTP/SQL evidence |
| Preconditions | Isolated synthetic fixture from QA/10_TEST_DATA_STRATEGY.md; never customer data |
| Reproduction frequency | Not measured; reproduction instructions are proposed |
| Source | apps/api/Controllers/PlatformAcademiesController.cs:35 |
| Class/function | Onboard |
| Related source | INVENTORY/API_ENDPOINTS.md, CONTRACTS.md, FORMS_AND_FIELDS.md and ASYNC_FORM_RISKS.md |
| Root-cause confidence | HIGH |
| Evidence location | Source excerpt below; source fingerprint in INVENTORY/SOURCE_MANIFEST.md |
| Screenshot | Not captured on pinned baseline |
| Console logs | Not captured; required in retest |
| API request | In isolated fixtures save DefaultTrialDays=14 through platform settings, then onboard an academy. Compare created expiry to captured creation time; repeat with45 and default30. |
| API response | Final settings PUT and onboarding responses captured; durations1/14/30/45 and missing30 checked against UTC request windows |
| Database before/after | Six exact tenant/admin/membership/audit additions; prior captured rows/expiry unchanged; ten identical read/rejection snapshots |
| Dependencies | Safe SQL/HTTP/browser harness as applicable; desired policy review where noted |
| Fix commit | Local uncommitted duration read/guard/expiry repair; no push/deploy |
| Retest result | PARTIAL PASS — final settings/onboarding SQL lifecycle; source baseline only; browser/critical OPEN |
| Regression result | 22 HTTP-SQL and1019 existing backend rerun PASS; owned cleanup; broader gates OPEN |
| Closure notes | Remain OPEN; follow closure requirements in QA README |

## Exact reproduction

In isolated fixtures save DefaultTrialDays=14 through platform settings, then onboard an academy. Compare created expiry to captured creation time; repeat with45 and default30.

## Expected

New trials use the saved default trial duration, without retroactively changing existing trials.

## Actual / evidence

Onboard always sets UTCnow+30 and never reads PlatformSettings.DefaultTrialDays. Setting save persists the chosen value independently. Source finding; runtime NOT RUN.

This describes the accepted historical source baseline. The local2026-10-02 repair and final SQL verification are recorded below; the original runtime failure was not newly reproduced.

Source snapshot:

```text
34:         var trial = SubscriptionPlanCatalog.Get("Trial");
35:         var academy = new Academy { Name = request.AcademyName.Trim(), LegalName = request.LegalName?.Trim(), CountryCode = string.IsNullOrWhiteSpace(request.CountryCode) ? "IN" : request.CountryCode.Trim().ToUpperInvariant(), TimeZone = "Asia/Kolkata", SubscriptionPlan = trial.Name, SubscriptionStatus = "Trial", SubscriptionEndsAtUtc = DateTime.UtcNow.AddDays(30), StudentLimit = trial.StudentLimit, StaffLimit = trial.StaffLimit, EnabledModulesJson = JsonSerializer.Serialize(trial.Modules) };
36:         db.Academies.Add(academy); await db.SaveChangesAsync(token);
37:         var userName = request.AdminUserName.Trim(); var email = userName.Contains('@') ? userName : $"{userName}@academydesk.local";
```

## Suspected root cause

Tenant provisioning hardcodes a duration instead of using the configurable default.

## Business impact and blast radius

New academies created by either owner onboarding UI or direct platform API; does not establish automatic expiry enforcement.

## Related / required regression

PLATFORM-TRIAL-001: Real HTTP/SQL setting-save then onboarding for14/30/45 days with bounded clock tolerance, absent-setting fallback and unchanged existing academy expiry.

Also run all endpoint/form cases pointing to this issue in QA/03_TEST_MATRIX.md and the critical regression suite before closure.

## Local successor — 2026-10-02

[Repair/evidence](../REPORTS/PHASE_2B_TRIAL_DURATION_REPAIR.md): new tenants use the latest saved DefaultTrialDays, or30 when absent. Invalid/nonrepresentable stored duration rejects before provisioning. Existing tenant expiry and all other configuration/provisioning/auth/audit behavior unchanged; no shared settings validation change.

22 final real Identity/HTTP-SQL cases PASS: saved1/14/30/45 days, absent30, latest vs stale duplicate, unusable-expiry no-write rejection, duplicate/role/anonymous denials, exact new tenant/admin/membership/audit additions and prior full-row/expiry preservation.1019 existing backend rerun PASS; build0 warnings/errors;641 accepted entries retained; one owned SQL run/container/root/port removed. No new runtime baseline or failed build/runtime attempt. No customer/dev DB/services/Azure/commit/deploy.

Issue/Phase2B/release OPEN for browser, critical, concurrency/ties, legacy/settings-bounds policy and provisioning fault rollback. Next BUG-DATA-0011 role-assignment failure path, Sol High.
