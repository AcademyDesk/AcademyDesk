# P0 finance concurrency retest — 7 November 2026

Native validation used `b474042` after the verified four-path C12 import at
`b7c9f84`. The application, API test and SQL harness trees match Cloud.
Cloud's 2,027 passing API tests and the prior source-bound P0 media and
restore receipts were reused. The native harness built with zero warnings
or errors. [C12 verification](../HANDOFF/C12_COMPENSATION_METADATA_2026-11-07.md)

Run `f1e9fdcffe2a4cbba34c85d2a47529bf` passed all 12 FinanceIntegrity
checks. Six real SQL races observed both lock waiters and verified the
final ledger and audit, covering concurrent collection, two restorations,
restoration versus collection, duplicate approval, two approvals, and void
versus reconciliation. Three injected audit failures rolled back. The
remaining controls verified unpaid-status refusal, cancellation protection
and refresh under the invoice lock.

Payroll run `4af0a92a082f4b199dec919ef098cff9` passed the original
positive-net and session-cycle controls and all 23 payroll reconciliation
checks. Each of the five alternate-profile races observed two SQL waiters,
returned 201 and 409, and stored one payout. Two injected audit failures
rolled back. The migration retained legacy records, and the guarded
downgrade refused to remove provenance with a legacy null timestamp.

The modules used one bounded window with cached SQL and disposable synthetic
data. They removed only their owned databases, logins and containers. The
original Docker inventory, PENTA services, API health and lease were restored.

All 14 production guard tests passed. The live guard still exits 1 with five
open P0 records. GitHub returned no open integration PR or PR-triggered
Actions run for `b474042`. The candidate carries the pre-Azure guard.
`origin/main` at `f7af512` still has the older workflow without it. Current
CI includes a remote npm audit. The restriction on sending the dependency
graph to the previously rejected audit endpoint still applies.

Exact-release CI and image evidence, linked critical and physical-device
checks, and staging restore and rollback remain pending.
[Production gates](../09_RELEASE_GATES.md)
