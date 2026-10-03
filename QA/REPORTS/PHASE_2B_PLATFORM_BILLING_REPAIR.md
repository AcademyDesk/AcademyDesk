# Phase 2B — Unverified platform payments remain outstanding

2026-10-02. Accepted BUG-DATA-0024; bounded local Sol High repair. No repeated Astra source audit, commit/push or Azure deployment.

## Reproduction and repair

A real platform-owner HTTP request created a synthetic INR1000 Draft invoice and issued it; a real academy administrator then submitted payment with an optional null reference. Fresh SQL verified the Payment submitted state, unchanged amount/identity and one actor-linked academy audit. The next owner overview returned totalBilled=1000, collectedBilling=0, outstandingBilling=0: an unpaid1000 deficit. These five baseline requests (three writes/two unchanged reads) are reproduction evidence, not passing repair cases.

Only the Overview outstanding predicate changes: include Payment submitted alongside Issued and Overdue. Submission is an unverified claim, not collection. Paid alone remains collected; Draft/Void remain excluded from billed. No DTO, response-property, schema, payment writer, authorization, legacy status transition, audit, platform activity-deletion guard, currency conversion or frontend changes.

## Verification

51/51 real Identity/global-filter/model-binding/HTTP-SQL cases PASS:27x200,1x201,6x400,13x403,2x401,2x404. Separately,1,019/1,019 existing backend tests rerun PASS. No new unit tests or combined inflated count.

- Owner-created Draft → Issued → academy payment submission → Overdue → new submission → owner Paid → Void → Draft/readback. Each independently stated expected total reconciles billed=collected+outstanding for the supported same-currency statuses.
- Null, omitted, trimmed nonempty and whitespace-only optional references, plus repeat submissions: correct persisted/reference response fields and timestamps; every unverified claim remains outstanding and not collected. The existing API permits Draft submission; compatibility is tested, not a new policy approval. The existing repeated submission timestamp/reference/audit behavior is retained.
- Paid and Void submissions reject400 with no writes. Malformed/null/empty request bodies and numeric references reject400. Missing/foreign invoice404 and cross-tenant route/admin403, anonymous401 and non-admin Teacher/Manager/FinanceUser/PlatformOwner-role-without-owner-flag403 are unchanged. Same-academy Owner submission remains permitted.
- Owner overview denied in eight cases: one anonymous plus seven non-owner identities (AcademyAdmin, foreign administrator, Teacher, academy Owner, Manager, FinanceUser and PlatformOwner role without its flag). The authenticated owner is a separate successful identity.
- Empty overview and two-tenant INR fixtures across Draft/Issued/Overdue/Payment submitted/Paid/Void with decimal/zero amounts. Final independent totals:1011.10 billed,4.44 collected,1006.66 outstanding,one Overdue invoice; a repeated read is unchanged. Synthetic fixture inserts are deliberate setup, not endpoint writes.
- Eleven exact permitted writes: one invoice create, five owner status updates, five academy submissions. Exactly11 distinct actor-linked success audits, with preserved full prior audit rows, expected target/type/metadata and no unrelated invoice changes. Fresh snapshots capture platform invoices, both audit stores, academies, students, notifications and Identity users/roles/memberships. Forty reads/rejections have byte-equivalent normalized full snapshots;17 permitted reads and23 rejected requests. This is not a guarantee about every database table.
- Baseline/final isolated builds:0 warnings/0 errors.313 routes unchanged;82 application/7 Identity migrations applied only to owned disposable SQL. Ownership-mismatch cleanup refusal passed. Both owned databases/logins/containers/temp roots/ports removed; source/evidence retained for repeatable setup.

## Evidence and preservation

[Baseline HTTP/SQL](../EVIDENCE/logs/phase-2b-platform-billing-baseline.log), [final HTTP/SQL](../EVIDENCE/logs/phase-2b-platform-billing-final.log), [backend rerun](../EVIDENCE/logs/phase-2b-platform-billing-backend.log), [TRX](../EVIDENCE/platform-billing-test-results/platform-billing.trx), [baseline build](../EVIDENCE/logs/phase-2b-platform-billing-baseline-build.log), [final build](../EVIDENCE/logs/phase-2b-platform-billing-final-build.log), [owned cleanup](../EVIDENCE/platform-billing-cleanup.json).

[Before receipt](../EVIDENCE/platform-billing-before.json) pins594 prior source/evidence/binary entries; six declared product/harness/tracker successors may differ. [Current receipt](../EVIDENCE/platform-billing-source-receipt.json) and [consistency validator](../tools/validate-platform-billing.cjs) verify the one-predicate change, new isolated harness mode, exact case/evidence counts, cleanup and immutable prior sources/evidence/normal binaries. Prior activity-deletion/announcement/guardian/certificate results are retained, not reclassified as browser/critical acceptance. No failed build/runtime attempt occurred in this slice.

## Limits and next

BUG-DATA-0024/Phase2B/release remain OPEN for browser/dashboard tile/readback/feedback, linked critical regression, currency-specific reporting/conversion, unsupported/legacy statuses, broader status-transition policy, concurrent status changes/consistent multi-query overview snapshots and fault/rollback tests. Overview still executes separate aggregate queries; no concurrent-snapshot guarantee is added. Payment submission alone is not proof of actual transferred funds. No customer/dev DB, normal dev-service/assembly, Azure/Blob/provider or push/deploy/restart changes. Disposable synthetic test data was destroyed; retained fixtures can recreate it.

Next remaining accepted platform gap: BUG-DATA-0022, unknown subscription plans silently fall back to Launch and overwrite tenant entitlements. Reproduce and guard the configuration request; continue Sol High.
