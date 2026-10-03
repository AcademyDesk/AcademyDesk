# Phase 2B — Saved default trial duration

2026-10-02; local Sol High allocation. Fast bounded successor to accepted BUG-FUNC-0009. Accepted source finding is the baseline: Onboard used UtcNow.AddDays(30) independently of the saved DefaultTrialDays. Source copy/hash retained; no new runtime baseline reproduction or baseline build, and no repeated Astra audit.

## Repair and verification

Onboard reads the most recently updated/created PlatformSettings record without changing it, matching the existing settings-selection order. Missing settings use30 days. A captured UTC start plus the selected duration determines only the new tenant expiry. Nonpositive or DateTime-overflowing saved durations reject400 before role/tenant/admin creation, with a settings-recovery message; shared SaveSettings validation is unchanged. Existing tenant expiry, plan/catalog/configuration guard, status handling, default fields, provisioning, audit and authorization code remain unchanged.

22/22 final real Identity/global-filter/HTTP-SQL cases PASS:6x201,7x200,3x400,1x409,4x403,1x401. Separately1019/1019 existing backend tests rerun PASS. Isolated build0 warnings/errors. No new unit tests or combined inflated count.

- Absent-setting30-day fallback and settings PUT → onboarding for14/30/45/1 days. Six created trial expiries fall inside independently captured UTC request windows plus the expected day count. SQL datetime2 loses DateTimeKind; the consistency validator explicitly interprets these generated UTC timestamps as UTC, not local time.
- Exactly one tenant, administrator, AcademyAdmin membership and actor-linked onboarding audit per successful intake. Existing captured tenants/expiry, identities, roles and audit rows unchanged; new Trial limits/modules and optional/default fields retained. Six settings-save requests verify persisted fields/identity and one owner audit each; total12 distinct success audits. This does not certify provisioning failure rollback or unchecked role-assignment behavior.
- Existing invalid setting saves0/-1 reject; a save of int.MaxValue still succeeds under the unchanged settings contract, but onboarding rejects the unusable expiry with no writes. A normal45-day setting restores the flow. Duplicate admin409, anonymous401, academy admin/Teacher/PlatformOwner-role-without-flag403, and admin settings PUT403 make no writes.
- A synthetic stale duplicate setting7 days is inserted as setup. Onboarding uses latest45 and preserves both settings rows; it does not invoke settings-read duplicate cleanup. Final list GET is unchanged. Twelve permitted requests plus nine rejected requests and one GET make22 cases; ten reads/rejections have identical full captured snapshots.
- Captures: academies, settings, both audit stores, students, notifications, Identity users/roles/memberships. Not every database table.313 routes unchanged,82 application/7 Identity migrations only in owned SQL, ownership-mismatch cleanup refusal PASS. One owned database/login/container/temp root/port removed. No failed build/runtime attempt.

## Evidence and preservation

[Final HTTP/SQL](../EVIDENCE/logs/phase-2b-trial-duration-sql.log), [build](../EVIDENCE/logs/phase-2b-trial-duration-build.log), [backend rerun](../EVIDENCE/logs/phase-2b-trial-duration-backend.log), [TRX](../EVIDENCE/trial-duration-test-results/trial-duration.trx), [cleanup](../EVIDENCE/trial-duration-cleanup.json), [before receipt](../EVIDENCE/trial-duration-before.json), [current receipt](../EVIDENCE/trial-duration-source-receipt.json), [validator](../tools/validate-trial-duration.cjs).

641 accepted source/evidence/binary entries pinned; only six declared product/harness/tracker successors may differ. Previous platform plan/balance/audit and portal repairs retained. No normal dev DB/services/assemblies, customer data, Azure/Blob/provider, commit/push/deploy/restart changes. Disposable synthetic data destroyed; fixtures/evidence retained.

BUG-FUNC-0009/Phase2B/release OPEN for browser feedback/editor flow, linked critical suite, concurrent setting changes/timestamp ties, fault rollback and broader legacy/settings bounds/expiry-enforcement policy. Original runtime failure was not newly reproduced. Next accepted gap: BUG-DATA-0011 account provisioning ignores role-assignment failures; begin with platform onboarding's bounded failure path. Continue Sol High.
