# Batch configuration/admission serialization — 24 October 2026

QA-only missing-case continuation of BUG-DATA-0006/E1/E9. Existing immutable
development API build `f42f065951044f48bf7e55b75bc9727b` reused. No application,
product-policy, model, permission, dependency or migration changes are planned.

## Acceptance scope

Three pending configuration edits: close batch, disable waitlist, change course
to one with an unmet prerequisite. The owned disposable SQL row gate holds an
X KEY lock on the synthetic batch. A real HTTP config writer must block on that
row while already owning the shared admission application lock. The admission
request must then be observed in SQL waiting on an APPLICATION lock held by the
writer, not merely inferred from timing or a blocked batch row. After release,
config200 then admission409/exact message, unchanged enrollment hashes and exactly
one audit are mandatory. Each configuration also receives a required-audit DENY
INSERT failure500/exact unchanged-state rollback, REVOKE/recovery200 and subsequent
admission409/no rejected write or audit. No new retroactive policy semantics.

The gate is restricted to the exact run-labelled disposable container/database,
GUID-validated batch/academy, no public ports, bounded SQL transaction/lock wait;
release is in finally. Existing gate behavior is retained for old policy proof.
No customer/production data or genuine credentials. Services stop only in the
approved exclusive bounded window, unchanged3GiB guard and exact restoration/
inventory/lease finally. No restart, downloads, pruning or deployment.

## Current state

SQL `4b7774c326494863878b51b9fc460afb`, window
`5f7a1a7af31447c6b815a0156cdf8bc6` PASS.66ordinary setup/security HTTP checks,
three coupled blocking cases, three required-audit rollback/recovery controls.
Each pending writer was actually blocked on the owned X-locked batch row;
each admission actually waited on the writer's APPLICATION lock, with neither
request completed before gate release. All three config200/admission409 outcomes
had the exact current rule message, unchanged enrollments and exactly one audit.
All three denied audits returned500/exact unchanged state; recovery200 then
admission409/no rejected write/audit. No429. All109migrations verified, source
unchanged/owned SQL/API cleanup/full inventory/exact original restoration and
lease release PASS.147focused QA-tool tests PASS,0failed/skipped.

Independent qualifier PASS:
```text
node QA/tools/admission-configuration-qualification.cjs 4b7774c326494863878b51b9fc460afb 5f7a1a7af31447c6b815a0156cdf8bc6
```
SQL SHA256 `6ebae8fb5b477fc2e749170007d7cab813228c3cfeb44855648ed34ef928e78b`.
Window SHA256 `b043f85dc7f6ca010a51df5b3666bd04996a70e194f1a8284c2e7ed0ff9afdc5`.
Test-only native transport adds only exact GUID batch PUT alongside enrollment
PUT; arbitrary/security dispatch/header injection remain rejected. Row-gate
foreign-container/database/unsafe-ID and actual-app-wait negative controls PASS.
No asynchronous client rejection can bypass this new helper's gate-release finally.
Existing completed lifecycle/browser proof
is retained; this packet exercises only the uncovered pending-configuration case.
Issue remains OPEN, physical devices/full release and enterprise gates remain OPEN.
