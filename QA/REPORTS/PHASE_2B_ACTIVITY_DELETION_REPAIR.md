# Phase 2B — Platform activity-deletion scope safety

2026-10-02. Accepted BUG-DATA-0023; local Sol High repair, no repeat of Astra inventory/audit. No real logs targeted.

## Baseline and narrow repair

Three real Identity/HTTP-SQL baseline requests with `AcademyAdmn`, an empty string and whitespace-wrapped `PlatformOwner` each deleted three synthetic platform plus three synthetic academy audit rows. Exact selected IDs, full surviving rows, response count and the new success audit were checked. These destructive reproductions occurred only inside a verified run-owned disposable database; they are not passing repair cases.

DeleteActivityLogs now allowlists nonnull scopes before querying/deleting/auditing. Only PlatformOwner and AcademyAdmin, with the existing case-insensitive comparison, are supported named values. Unknown, blank, whitespace-only and whitespace-wrapped strings return400 with a clear message and no writes. No trimming to null or broadest-scope fallback.

The existing frontend sends JSON null for All; that contract is preserved. The unchanged nullable DTO also maps omitted scope to null/All; one explicit compatibility case verifies this. Requiring explicit property presence or changing null semantics would be a separate contract change, not an inferred policy in this fix. Existing owner-flag authority, date ordering, inclusive FromUtc/exclusive ToUtc interval, deletion/audit SaveChanges, list-query semantics and frontend confirmation remain unchanged.

## Verification

42/42 real Identity/global-filter/model-binding/HTTP-SQL cases PASS (12x200,19x400,10x403,1x401). Separately,1,019/1,019 existing backend tests rerun PASS; no new unit tests or combined inflated count.

- Eleven invalid scope strings: typo, empty, space/tab, unsupported All/Both/Owner/Admin, leading/trailing whitespace and comma-combined groups. All rejected with the specific scope message and identical fresh captured snapshots; no deletion or success audit.
- Canonical/mixed-case named scopes and null/All; exactly the selected store/interval removed. Domain audit rows from both synthetic academies are considered only for permitted platform-wide owner scope. Repeated accepted deletions return zero and retain existing rows; their normal success auditing remains unchanged.
- Omitted-scope compatibility and an empty interval retained. Twelve accepted requests produced exactly12 unique actor-linked platform success audits and24 selected synthetic row deletions (12 per store). Response counts and audit Scope/FromUtc/ToUtc/PlatformLogs/AdminLogs match the actual rows.
- Fixtures place rows one tick before From, exactly From, inside, one tick before To, exactly To and one tick after To. Every unselected row remains byte-equivalent in normalized full-row JSON, including other stores and older deletion audits. Exact removed IDs are retained in HTTP evidence.
- Equal/reversed dates; malformed/null/empty JSON body, invalid date and numeric/array scope types reject without writes. Nullable historical actor/metadata fields remain valid fixture data.
- Anonymous401 and ten non-owner identities403: same/foreign AcademyAdmin, Teacher, academy Owner, Manager, FinanceUser, Guardian/Student roles, custom delegated permission, PlatformOwner role without the required owner flag. Existing authorization was not changed or broadened.
- Fresh before/after snapshots cover both audit stores, academies, students, notifications, Identity users/roles/memberships. Accepted requests change only selected audit rows and the one success audit; all other captured collections unchanged. This is not proof about every database table.
- Baseline/final isolated builds passed, zero warnings/errors;313 routes unchanged,82 application/7 Identity migrations applied only to owned SQL. Ownership-mismatch cleanup refusal passed. Both run-owned databases/logins/containers/temp roots/ports removed. No customer/dev database, normal dev-service/assembly, Azure/Blob/provider, commit/push/deploy or restart changes.

## Evidence and preservation

[Baseline HTTP](../EVIDENCE/logs/phase-2b-activity-deletion-baseline.log), [final HTTP](../EVIDENCE/logs/phase-2b-activity-deletion-final.log), [backend rerun](../EVIDENCE/logs/phase-2b-activity-deletion-backend.log), [TRX](../EVIDENCE/activity-deletion-test-results/activity-deletion.trx), [baseline build](../EVIDENCE/logs/phase-2b-activity-deletion-baseline-build.log), [final build](../EVIDENCE/logs/phase-2b-activity-deletion-final-build.log), [owned cleanup](../EVIDENCE/activity-deletion-cleanup.json).

[Before receipt](../EVIDENCE/activity-deletion-before.json) pins570 accepted source/evidence/binary entries, including the unchanged frontend null/All request contract. Only six declared product/harness/tracker successors may differ. [Current receipt](../EVIDENCE/activity-deletion-source-receipt.json) and [consistency validator](../tools/validate-activity-deletion.cjs) verify the guard-only product change, new harness mode and immutable prior sources/evidence/assemblies. Prior announcement/guardian/certificate results are retained, not rerun as browser/critical acceptance.

## Remaining gates and next

BUG-DATA-0023/Phase2B/release remain OPEN for actual browser confirmation/cancel/no-request, mobile/time-zone/end-to-end feedback, linked critical regression, concurrent deletion/retention/permission changes and fault rollback. No all-metadata/date-default/legacy-contract policy hardening is claimed; DTO and source list-query behavior remain unchanged. Synthetic test storage was destroyed after verification; fixture code and evidence are retained to reproduce it.

Next accepted gap: BUG-DATA-0024, unverified platform payment submissions disappear from outstanding totals. Reproduce scoped financial readback and fix the accepted pending/outstanding discrepancy; continue Sol High.
