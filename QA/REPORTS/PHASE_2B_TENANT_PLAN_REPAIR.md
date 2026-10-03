# Phase 2B — Reject invalid tenant plans before configuration writes

2026-10-02. Accepted BUG-DATA-0022; bounded local Sol High repair. No repeated Astra source audit, commit/push or Azure deployment.

## Baseline and repair

Three real platform-owner configuration requests (`Professionl`, `Professional ` and `NotAPlan`) replaced a synthetic Professional tenant's750 student/120 staff limits and full module list with Launch75/15 and Core/TeacherClassroom. Each returned200 and produced a tenant-configuration success audit. Fresh full snapshots verified target changes, untouched other academy and unrelated captured data. These three reproductions are baseline defects, not passing repair cases.

Configure now uses the existing catalog dictionary's TryGetValue and rejects an unsupported name400 with `Choose a valid subscription plan.` before assigning any tenant fields or adding a success audit. The existing nonblank plan/status guard remains. Valid names retain the catalog's case-insensitive comparison and canonical name; whitespace-padded names reject instead of being trimmed or downgraded. No shared catalog Get/ModulesFor default changes: other callers retain their existing fallback. Onboarding, active-state endpoint, authorization, DTO, status trimming, nullable expiry replacement, configured limits/modules, normal audit and save behavior remain unchanged.

## Verification

48/48 real Identity/global-filter/model-binding/HTTP-SQL cases PASS:13x200,26x400,7x403,1x401,1x404. Separately,1,019/1,019 existing backend tests rerun PASS. No new unit tests or combined inflated count.

- Fourteen invalid plans: typo, unknown name/suffix, leading/trailing/both whitespace, newline/tab/nonbreaking-space suffixes, comma-combined names, empty/space/tab-only and null. Ten nonblank unsupported names use the specific new message; blank/null and omitted properties retain existing validation/model-binding behavior. All reject without captured writes or success audit.
- Every supported plan (Trial/Launch/Growth/Professional/Enterprise) in canonical and lowercase form. An independent frozen oracle checks exact limits and ordered module JSON in SQL, response and audit metadata; no oracle call to the catalog fallback. Starting Professional fixtures exercise lower/same/higher capacity choices. A repeated Enterprise request preserves configuration values while retaining the existing fresh success audit.
- Present and null expiry dates plus existing status whitespace trimming. A canonical Trial request against an inactive academy preserves its inactive flag and succeeds only with actual platform-owner authority, matching existing behavior. This is not new trial-duration, expiry enforcement or downgrade-capacity policy.
- Missing/null/empty/blank status, omitted plan/status, malformed/null/empty body, numeric/array plan and invalid date400. Unknown academy404. Anonymous401; own/foreign AcademyAdmin, Teacher, academy Owner, Manager, FinanceUser and PlatformOwner role without owner flag403. Owner can configure the other synthetic academy; final owner list readback matches persisted plans/limits/modules.
- Twelve accepted configuration requests, each with exactly one distinct actor-linked platform success audit. Full prior audit rows and the other tenant are preserved. All six mutable configuration fields are verified; unrelated name/legal/country/time-zone/active/branding/created/updated fields stay unchanged. Deliberate fixture reset writes between requests are not endpoint writes. A repeat may change no tenant values; the success audit is still retained.
- Thirty-six requests have identical fresh full snapshots:35 rejections plus one list read. Captures include academies, both audit stores, onboarding profiles, platform invoices, branches, students, notifications and Identity users/roles/memberships. This is not proof about every database table.
- Baseline/final builds0 warnings/errors;313 routes unchanged;82 application/7 Identity migrations applied only to owned SQL. Ownership-mismatch cleanup refusal passed. Both run-owned databases/logins/containers/temp roots/ports removed. No failed build/runtime attempt in this slice.

## Evidence and preservation

[Baseline HTTP/SQL](../EVIDENCE/logs/phase-2b-tenant-plan-baseline.log), [final HTTP/SQL](../EVIDENCE/logs/phase-2b-tenant-plan-final.log), [backend rerun](../EVIDENCE/logs/phase-2b-tenant-plan-backend.log), [TRX](../EVIDENCE/tenant-plan-test-results/tenant-plan.trx), [baseline build](../EVIDENCE/logs/phase-2b-tenant-plan-baseline-build.log), [final build](../EVIDENCE/logs/phase-2b-tenant-plan-final-build.log), [owned cleanup](../EVIDENCE/tenant-plan-cleanup.json).

[Before receipt](../EVIDENCE/tenant-plan-before.json) pins618 prior source/evidence/binary entries; six declared product/harness/tracker successors may differ. [Current receipt](../EVIDENCE/tenant-plan-source-receipt.json) and [consistency validator](../tools/validate-tenant-plan.cjs) verify the Configure-only guard, new isolated harness mode, exact evidence/case counts, unchanged catalog, immutable prior platform/portal/source/evidence/binaries and cleanup. Earlier platform billing/activity/announcement/guardian/certificate results are retained, not reclassified as browser/critical acceptance.

## Limits and next

BUG-DATA-0022/Phase2B/release remain OPEN for actual browser/editor/feedback, linked critical suite, concurrent configuration changes and fault/rollback. Broader status/expiry/over-capacity downgrade/legacy-plan policies are unchanged; no customer-data recovery from past fallbacks is attempted. No normal dev-service/assembly, dev/customer DB, Azure/Blob/provider, commit/push/deploy/restart changes. Disposable synthetic test data was destroyed; source/evidence retained for repeatable fixtures.

Next adjacent accepted gap: BUG-FUNC-0009, academy onboarding ignores the saved default trial duration. Reproduce settings-save → onboarding and preserve existing tenant expiry; continue Sol High.
