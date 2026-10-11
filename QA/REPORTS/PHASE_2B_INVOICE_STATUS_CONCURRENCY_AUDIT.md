# Phase2B — invoice status concurrency and audit rollback

2026-10-12 IST; runs started2026-10-11 UTC. Worktree `D:\AcademyDesk-codex-p0`, branch `codex/penta-search`, parent/accepted repair `ee1e5a123d9683c8074118136f2fbebc326c9b9d`.

## Scope

Continue [BUG-DATA-0055](../ISSUES/BUG-DATA-0055.md) and the [ledger repair](PHASE_2B_INVOICE_STATUS_LEDGER_REPAIR.md). QA-only successor: new `InvoiceStatusIntegrityRegression.cs`, strict `InvoiceIntegrity` harness switch and owned runner mapping. No application/API/frontend/schema/permission/Mini/Core change, historical rewrite, policy approval or new testing plan. Previous111 targeted/45 status controls and retained payment transitions remain recorded separately.

## Measured concurrency

Hold the exact same-academy invoice row with an external owned-SQL `UPDLOCK,HOLDLOCK` transaction. Launch two authenticated HTTP operations; release the blocker only after observing **two actual invoice-lock requests waiting in its SQL blocking chain**. A queued request can block another queued request; direct-child count is not the acceptance condition. Launching tasks alone is not overlap evidence.

Fresh snapshots compare every row/field in **Invoices, Payments, FinanceAdjustments and AuditLogs** across both academies, including a positive foreign payment sentinel. This is a four-table finance snapshot, not every database table. Canonical CLR ID ordering and exact JSON numeric equality avoid SQL GUID-order/decimal-scale presentation differences. Self-check proves400 equals400.00 but not400.01; no rounding, omitted fields or weakened ledger/audit assertion.

Final **20/20 overlapped pairs PASS**, five per mode:

| Simultaneous operations on invoice1000 | Strict final state | Observed status-edit / ledger-write HTTP |
| --- | --- | --- |
| Issued edit + collection600 | PartiallyPaid; exactly one new Completed600 row | three200/201, two400/201 |
| PartiallyPaid edit + void reconciled600 | Issued; existing row Voided, reference/evidence preserved | five200/200 |
| PartiallyPaid edit + discount400 approval with reconciled600 | Paid; adjusted400, exact Approved proposal/timestamps | five200/200 |
| Issued edit + reconciliation of existing Voided600 | PartiallyPaid; existing row Reconciled with replacement evidence | five200/200 |

All20 proofs recorded chainWaiters2/directWaiters1. Status200 is allowed only when ledger-consistent at serialization time; otherwise400 with the exact existing ledger-consistency message. Every ledger write returns expected201/200. Success audit history/count and actor/academy/action/route checked; rejected status edits add no success audit. Authenticated invoice register agrees with fresh SQL collected/adjusted/balance/status. No HTTP429.

Both valid/rejected status outcomes were observed for collection. The run does **not** force both lock-acquisition orders for every mode or prove every scheduler/load profile. Restoration remains an observed existing policy-pending path, not approved financial behavior.

## Same-action audit fault

For each actual status change to Overdue, PartiallyPaid, Paid and Cancelled, deny AuditLogs INSERT only to the exact owned runtime login. Domain execution followed by failed audit persistence returns500 and restores the four-table pre-request snapshot, including foreign rows/prior audits. Remove the denial; explicit retry returns200 with only the intended status change and one authoritative success audit. Invalid Paid during the first denial returns400/no-write without requiring audit INSERT.

**9/9 controls PASS**: four500 rollbacks, four200 recoveries, one400 rejection during denial. Register/SQL parity checked after recovery. This proves the normal Admin finance-filter transaction boundary, not Platform Owner/direct-call audit semantics or every other action.

## Receipts and harness corrections

- **111/111 targeted API/controller tests PASS**, local `QA/EVIDENCE/invoice-integrity-20261011/retained.trx`; not full enterprise suite.
- Final isolated harness compilation0 warnings/0 errors,25.66s. Frontend/full enterprise suite not rerun; prior accepted evidence retained, not presented as fresh.
- Legacy aggregate `node QA/tools/validate.cjs` is **NOT PASS/not runnable** here: missing historical local `QA/EVIDENCE/logs/observed-checks.json` stops it before artifact output. No fabricated/copied evidence or validator change. Current staged diff/link/high-signal secret checks pass independently; not a whole-QA validation claim.
- `dba4ace4a4f847d08616bf45d8315ead`,80.8s, `sql.txt`: direct-child-only monitor failed before any accepted pair. No original DMV trace proves its precise blocking chain. Recursive-chain correction still requires both real waiters; later runs directly observe chain2/direct1.
- `63ca156518b344f4b6aba0dbe23b5867`,36.3s, `sql-chain.txt`: first pair passed, second failed snapshot order. Read-only SQL inspection confirmed three GUID rows ordered differently by SQL/CLR. Canonical ordering corrected with every row/field retained.
- `62ea4faf9f2c4f45a469fb8ecc20b330`,61.6s, `sql-final.txt`: ten collection/void pairs passed, approval failed textual decimal scale. Read-only SQL confirmed adjusted400.00/Paid and proposal400.00/Approved. Exact numeric equality corrected with one-paisa negative self-check. This partial run is not full acceptance.
- All three failures/logs retained locally. Only their exact containers were removed after name/run-label/loopback ownership checks. Ephemeral databases are no longer recoverable; those removals are **not** normal successful database/login teardown receipts. Other containers untouched.
- Final **PASS** run `8e62b5200c00447e800d1e40a1ba6abd`, SQL127.0.0.1:62732, started2026-10-11T18:30:22.8178703Z, exit0/62.3s, `sql-final-numeric.txt`:20 pairs +9 audit controls, real Identity/HTTP/tenant preflight,88 application +7 Identity migrations, restricted runtime login, mismatched-marker cleanup refusal and exact-owned database/login/container teardown.
- Final Docker inventory: original two running/seven stopped containers remain. Evidence local/untracked, `.build-check` ignored. Older32/28 continuity additions and unrelated Mini/Platform work preserved/excluded from feature publication. Main unchanged; no production data/credentials or Azure.

## Reproduce / next

Build `QA/tools/SqlHarness/SqlHarness.csproj` with `--artifacts-path .build-check/invoice-integrity-sql`, then `powershell -NoProfile -File QA/tools/SqlHarness/Run-ReconciledPayment.ps1 -Module InvoiceIntegrity` from this worktree. New marked disposable SQL only; runner refuses mismatched ownership and retains failures, never prunes containers.

BUG-DATA-0055 **OPEN/RETEST**. Next **Sol High**: collection/Student portal consumers and linked browser acceptance; role/tenant/stale-context/direct boundary, physical Android/iOS, critical/release and cancellation/reopening/zero-cash/legacy policy gates remain separate. Then existing BUG-DATA-0056 historical payroll attribution queue. **Sol Medium** for settled Manual Orbit + Aura dark /Prism light rollout; FW1 delivery/Mini86/94 qualification open, no Mini project switch needed.90% eligible-AI/10% Manual is the target, not measured readiness.
