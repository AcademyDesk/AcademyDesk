# Payroll identity preservation — 2026-10-28

Candidate development branch: `codex/release-export-security`.

BUG-DATA-0056 is partially repaired. New payouts persist worker name/type at payment time, and the payout register reads that immutable identity rather than joining mutable profiles. Rename, worker-type/link reassignment, deactivation and missing profiles cannot relabel or hide existing payouts. Foreign profile names are never joined into the register; unknown legacy identity is shown explicitly as `Unavailable worker` / `Unknown`.

The additive `20261028173216_SnapshotPayrollWorkerIdentity` migration introduces two nullable columns and backfills from matching profile ID **and academy ID**. It does not change monetary fields, statuses, periods or profile records. Earlier names overwritten before this upgrade cannot be reconstructed. Missing/foreign profiles remain unknown. Stop old application writers during cutover; old binaries can otherwise create unsnapshotted records after the backfill. Downgrading drops snapshots, so preserve them before any rollback. No migration was applied to the configured application database.

BUG-DATA-0009 is partially repaired: profile updates now apply the same supplied-teacher academy/existence check as creation, before mutation. Required worker links, StaffUserId validation and worker-type/link consistency remain separate open work; this batch does not redefine those policies.

## Observed validation

- Six original history/isolation cases:6 failed before repair,6 passed afterward.
- Two foreign/missing teacher-update cases:2 failed before repair,2 passed afterward.
- Payroll-focused suite before the additional two link cases:25 passed.
- Final full API suite:1,179 passed,0 failed,0 skipped.
- Offline migration scaffolding and generated upgrade SQL:PASS. SQL contains only the two additive columns, academy-scoped backfill and EF history update.
- Prepared SQL harness:owned pre-column upgrade checks all scalar values of three legacy payouts (own, foreign, orphan) and original profile; HTTP create and four history mutations plus two invalid teacher-link no-write checks. Final harness compilation:0 warnings/0 errors. Compiled execution is separate from runtime acceptance.
- Existing receipt migration regression now pins its original migration pair and then upgrades to the current schema, preserving that prior rollback/reapply case when later migrations exist.
- SQL/browser/device acceptance remains pending for this source. Both c25b6b8 run40731742967 and c4a6ffd run40736314010 failed in the guarded SQL step. Diagnostic annotations locate the latter in FinanceIntegrity fixture SaveChanges (FinanceIntegrityRegression.cs:43), before its first race. Its invoice identifier had45 characters against the40-character SQL constraint; corrected to35. Actual race acceptance requires a new successful run. Do not infer latest acceptance from historical52495e2 success.

Evidence logs remain ignored under `.build-check/payroll-history-*` and `.build-check/payroll-link-before.log`. No customer data, configured DB access, real bank disbursement, main merge, environment publication or deployment. Both issues remain OPEN until their full closure protocol and critical/release gates pass. Next route:Codex/Sol High for actual SQL failures and production acceptance; continue remaining financial and tenant integrity work.
