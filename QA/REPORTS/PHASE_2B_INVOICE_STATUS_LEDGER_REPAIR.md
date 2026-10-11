# Phase2B — invoice status follows the collected ledger

2026-10-11. Worktree `D:\AcademyDesk-codex-p0`, branch `codex/penta-search`, parent `8c025cf573af3a60720bb70a4671d8195fe5cea1`.

## Scope and reproduction

Continue [BUG-DATA-0055](../ISSUES/BUG-DATA-0055.md); no new audit/testing plan. Original endpoint permits arbitrary vocabulary-valid status edits. On the unchanged API, new controlled tests record **17/75 PASS,58 FAIL**. These are failed assertions, not58 distinct defects. Local receipt: `QA/EVIDENCE/invoice-status-20261011/baseline.trx`.

Owned SQL baseline `dc656f4eefe849c0b894b5594ba596b3` proves unpaid invoice1000 → PATCH Paid200, stored Paid/collected0/balance1000; PATCH paid200 stores lowercase paid. Register/shared ledger and fresh SQL agree on the inconsistent source. This run is **reproduction only, not acceptance**, exit0/47.9s and normal exact-owned cleanup. Local log: `QA/EVIDENCE/invoice-status-20261011/sql-baseline-fixed-fixture.txt`. Baseline API SHA256 `EED21DD7FAADBB8EEC5CDD9FA08821D2A64B77A6C46E28D9A663DD26BAC2D469` preserved in both API/harness outputs while rebuilding only the corrected test harness with `--no-dependencies`.

Earlier baseline `3d34f333d75e4f168a3a68bb6092de02` failed before reproduction: synthetic invoice number42 characters exceeded SQL40. Corrected only seed to32-character UUID, no assertion/limit weakening. Failure log retained; exact loopback/name/run-label verified before removing only that failed owned container. This is not a successful normal database/login teardown receipt for the failed run.

## Bounded implementation

- Canonical trimmed status allowlist, safe invalid-status400 message. No missing/foreign invoice disclosure change.
- Existing SQL invoice-first `UPDLOCK,HOLDLOCK` shared with collection/void/reconcile/adjustment approval; finance filter's transaction includes normal success audit. Direct/platform callers own a transaction through read/check/write. No schema, permission/filter or isolation override.
- Completed + Reconciled collection is academy/invoice scoped. Paid requires zero adjusted balance; PartiallyPaid requires positive collection and remaining balance; Issued/Overdue require no collection and positive balance. Contradictions return400 without a status/audit/ledger write. Existing due-date manual distinction is retained; no automatic overdue scheduler or due-date policy introduced.
- Cancellation and financially consistent reopening behavior remain as before, except canonical spelling. Existing fully adjusted/no-cash Paid semantics are retained, not a new approved financial policy. Cancellation/refund/credit-note/zero-cash/legacy disposition gates remain separate. No historic data backfill or claim to repair previously corrupted rows.
- Existing void regression now expects exact canonical Cancelled in SQL/register (formerly lowercase cancelled). Its original payment/evidence/count/preservation assertions remain; no broader acceptance weakened. No frontend/PENTA/Mini/Core/model/finance arithmetic change.

## Verification

- **111 targeted tests PASS**:75 new status matrix/invalid/foreign cases and36 retained invoice document/lookup/adjusted payment/payment transition cases. Local receipt `QA/EVIDENCE/invoice-status-20261011/final.trx`. EF InMemory is not SQL concurrency proof.
- API/test and isolated SQL harness compilation PASS; harness0 warnings/0 errors. Full enterprise suite/frontend build not rerun; prior Music/UI evidence retained, not newly accepted.
- First repaired SQL run `3ec5fc182c614a4e8976710d47250134`:45 strict status controls PASS, then retained transition suite hit the unchanged limiter429 on a void. Full run NOT accepted. Original failure log retained as `sql-final.txt`; exact-owned container cleanup recorded separately. Added an explicit61s pacing interval between formerly separate suites, without changing limits, auth or assertions; no retry-until-green.
- Final paced SQL run `17b128d0bca34d6389728339cabe25e6` **PASS**, exit0/134.8s: **45/45 strict status/invalid/role/tenant controls**, canonical register/shared-ledger parity, fresh whole-database no-write/foreign-sentinel checks and success audit checks. Retained full/partial/adjusted void, required reconciliation evidence, mixed Reconciled evidence/collection preservation, repeat/rejected generic path, future Issued and Cancelled preservation regression PASS. No HTTP429. Local receipt `QA/EVIDENCE/invoice-status-20261011/sql-final-paced.txt`.
- Final run applies88 application +7 Identity migrations, verifies restricted runtime login, refuses mismatched cleanup marker and removes its exact owned database/login/container. Original two running and seven stopped containers remain. Voided restoration remains an explicitly observed policy-pending path, not certified behavior.
- Reproduce: build `QA/tools/SqlHarness/SqlHarness.csproj` with `--artifacts-path .build-check/invoice-status-sql`, then `powershell -NoProfile -File QA/tools/SqlHarness/Run-ReconciledPayment.ps1 -Module InvoiceStatus`. The baseline module needs the frozen API artifact; it is reproduction only.

## Open gates and route

BUG-DATA-0055 remains **OPEN/RETEST**. Next **Sol High**: separately bounded simultaneous status-vs-collection/void/approval and same-action audit-failure rollback/recovery, then collection/Student portal/linked browser and physical-device acceptance. New row lock is not itself measured concurrent acceptance. Current normal status/permission matrix does not qualify every role, Platform Owner direct boundary or stale application session.

**Sol Medium** may continue settled Manual Orbit + Aura dark /Prism light rollout; shared versioned FW1 return, Mini86/94 qualification,90% eligible-AI/10% Manual target and enterprise/pre-Azure gates remain OPEN. No Mini project switch needed for this host-only repair. Publish validated feature scope only; unrelated14 tracked diffs/older32+28 continuity/private evidence preserved. No main edit/merge, production data/credentials, Azure, issue closure or policy approval.
