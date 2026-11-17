# PENTA integration candidate release retest

17 November 2026, desktop and disposable local SQL.

Backend and SQL sources are pinned to
`a7e0b06e92bb851b7adbc9468353b095fca0c1c4`. Both validation windows
rebuilt the same harness with SHA-256
`d02e02e6d64b0a76756d763c7f57123cfee6463f7ede8b804cd99355c95e6bd0`.
The C46 follow-up changes the learner response validator and its browser and
contract checks. It leaves the API, SQL fixtures and harness unchanged.

## Local HTTP, SQL and recovery

| Module | Run ID | Observed result |
| --- | --- | --- |
| CollectionRace | `926332be8f2f4fa38757d2399b486337` | 5/5 pairs returned one 201 and one 400 with the required balance message. Each retained two payment rows and collected 1000. |
| AdjustmentRace | `258645e3852844c2b73cc83548f4708b` | 5/5 pairs returned one 201 and one 400 with the required balance message. Each retained two payment rows and collected 800. |
| Reconciliation | `994e7aeffcf24af8873c6fc0087a2eca` | 600 reconciled on a 1000 invoice, 400 remaining in the financial readbacks, and a 500 excess payment rejected with no SQL change. |
| Payroll | `2592c1a122a64ea4a6e752f1f7649fdc` | 23 passed, including five duplicate-worker races, two audit rollbacks and guarded downgrade. |
| FinanceIntegrity | `525011a1ab6b4d7285eb717350675d84` | 12 passed, including six deterministic races and three audit rollbacks. |
| SessionRevocationRepair | `f2f2c598165249fa96e1b4aabc1cb9ef` | 36/36 HTTP cases, zero policy gaps and fresh SQL verification of Identity, role and link preservation. |
| ClassMedia | `2613e9b8186645e6828d776138d29f12` | Original private-file regression and 29 Identity/SQL HTTP cases passed. |
| BlobMedia | `ce85bfa8cde64944abda1715a3c67833` | 75 assertions passed through HTTP, Identity, SQL and private Azurite. |
| Transition | `2da408ffa417424185034c838bcba562` | Void and reconciliation transition regressions and 12 exact SQL/readback restoration cases passed. |
| Recovery | `2727d704657b4af6b74df496df71989d` | Six guarded backup/restore cases passed, including 82 table fingerprints and authenticated readback after restore. |

The two race modules recorded zero HTTP 429 responses. The first local wrapper
looked for the wrong reconciliation success marker and exited after that module.
The real module emitted `FINANCE-RULE-001 PASS`, exited zero and removed its
owned resources. The second window ran the remaining modules. Both windows
removed their owned containers and restored the original PENTA containers and
Docker inventory. The restored API health endpoint returned 200.

## C46 learner response hardening

Integrated the validator from Cloud `17e50d878aefadc264447f4419ab0d55707cba51`.
Cloud C45 released its overlapping claim. C47's combined acceptance at
`91132efe94114fa4a586c27ecb246ca75758f3c3` has its own Cloud results and Mini
archive diagnosis. This desktop retest adds live HTTP, SQL and browser evidence.

- 50 focused client tests passed. The Cloud test runner now accepts the same
  isolated-compiler setting as existing Desktop tests.
- 36 focused API tests and 61 native HTTP/SQL checks passed. SQL run
  `578e4233bbbb4aa8b0684748192f3c88` checked Guardian revocation, retained
  sibling access and the finance audit.
- Four live Chromium groups passed on Next.js 16.4.5. A response with an
  unexpected field was rejected, its class result stayed hidden and retry
  displayed the verified result. The desktop and mobile viewports used real
  local Identity and synthetic data. Physical device checks remain pending.

## Frontend dependency security

The exact-lock 16.4.3 browser run stopped at Next.js's security check after
compilation. The candidate upgrades to the published 16.4.5 security patch.
The patched build and static export passed. A separate npm advisory in the
lint toolchain required moving the locked typescript-eslint family from 8.70.0
to 8.73.0 and its nested minimatch from 10.2.6 to 11.0.9. The repair retains
the runtime graph already used for the live browser retest. A fresh install
from the final lock passed the full npm audit with zero findings, the
production build and TypeScript checks, the static export and ESLint with
zero errors and 61 existing warnings.

[Vercel 16.4.5 security release](https://github.com/vercel/next.js/releases/tag/v16.4.5)
[Minimatch advisory](https://github.com/advisories/GHSA-g3hv-75wg-m249)

## Release boundary

The guard tests passed 15/15. The actual gate still refuses release with open
`BUG-DATA-0001`, `BUG-DATA-0002`, `BUG-DATA-0003`, `BUG-DATA-0010`,
`BUG-SEC-0001` and `BUG-AUTH-REVOCATION-001`. The linked critical scenarios,
physical iOS/Android devices, staging migration/restore/rollback and exact
release artifact checks still need acceptance. The local backup drill does
not cover Azure SQL PITR, Blob restore or deployment rollback.

The previous GitHub tip had zero Actions runs. This checkpoint adds the
integration branch to the existing read-only Verify workflow's push filter
so an approved feature push can start hosted CI for the combined source. The
deployment workflow remains manual and main-gated in the candidate. Hosted CI
and its built artifacts need their own observed results.

The guided portal evidence does not yet measure the 90% conversation target.
Mini/Plus/Pro qualification remains open and Ultra is deferred. The owner stop
before loading the prepared checkpoint remains in effect.
