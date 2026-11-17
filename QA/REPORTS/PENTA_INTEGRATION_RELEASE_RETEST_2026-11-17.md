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

## Hosted checkout failure

[Actions run 44021358007](https://github.com/AcademyDesk/AcademyDesk/actions/runs/44021358007)
for `84c54ab81e424eda2cb7e9f2f532ad010199101f` passed the API regression
and npm install/audit/type/lint steps. The repo-scoped credential could
not fetch the private paired Core repository. Checkout logged Repository
not found. An authorized read-only GitHub probe confirmed PentaMini is
private and its pinned commit `c3c61b9ef3f239035c8b8f46649c66997e98f2b6`
exists. The repository secret metadata lookup returned 404.

The workflow now runs standard verification separately from the paired
PENTA protected onboarding job. The paired job fails with a specific access message
when `PENTA_SOURCE_READ_TOKEN` is unavailable. It checks the exact private
Core SHA and keeps credentials out of the local Git config. The paired
onboarding and loopback transport checks remain required. The reusable
deployment workflow forwards only the named checkout secret and still
depends on the entire verification result before the production guard.
No repository settings or credential values were changed.

[GitHub checkout scope](https://github.com/actions/checkout#checkout-multiple-repos-private)

## C48 Desktop response and hosted finance access fixture

[Hosted verification for 6590278](https://github.com/AcademyDesk/AcademyDesk/actions/runs/44023259353)
reported 2,493 API tests passing and seven completed finance SQL modules
before the Access module reached the Owner payout case. The case received
409 when expecting 201. Its Academy Admin test had already created a regular
payout for the same teacher and period under a different profile.

Each role's fixture now submits its own synthetic period, and SQL readback
verifies that period. The application payroll policy is unchanged.

| Native check | Run ID | Result |
| --- | --- | --- |
| Finance Access | `c57b7d4950b14aaca266119476084bea` | 129 cases passed, including both Admin and Owner payout creation and foreign-scope denial. |
| Payroll | `b4be88e843b24859ae441e62075bc044` | 23 cases passed, including five duplicate-worker period races, two audit rollbacks and the guarded downgrade. |
| PENTA migration | `0032af72914440069dec521c70125c94` | Four guarded SQL upgrade, downgrade, re-upgrade and preservation checks passed. |

The paired job independently still fails for the absent scoped
`PENTA_SOURCE_READ_TOKEN`. The latest native paired run on the same host
application source passed 23 onboarding gateway SQL checks and five real
.NET/Python transport checks, without using a model. Its SQL run is
`5851999dc1604c0da928140ce6a8c952`.

Cloud's C48 private-material source and test paths at `592939d` remain
outside the Desktop changes. The
[Mini evaluation handoff](../HANDOFF/PENTA_MINI_CONVERSATION_EVALUATION_2026-11-15.md)
records which original inputs and scenario preparation are available locally.

## Hosted retry and local API revision rollback

The independent job for `f1f62158eb692564d50aeb60a36dad3c109b9c45`
completed successfully on attempt 2. It passed 2,493 API tests, 26 native SQL
modules, 75 private Blob assertions, the full frontend suite with the fresh
48-case communication SQL receipt, the production frontend build, the
payroll PDF and assessment browser checks, and the API image/startup smoke.
The first attempt reported SQL error 1223 while EF released its migration lock.
The successful rerun used the same source and SQL image digest, so this is
an observed intermittent failure with no established underlying fix.

The separate protected onboarding/Core job remained blocked at the missing
`PENTA_SOURCE_READ_TOKEN` prerequisite.

[Exact hosted verification](https://github.com/AcademyDesk/AcademyDesk/actions/runs/44025630062/attempts/2)

Run `40dd9809e12147e380bb4186b11b41c3` passed 43 local Production-mode
build and runtime checks using the previous API image from `b6793d4` and
the current API tree at `f1f6215`. The current image was built offline using
the existing hash-verified feed of 53 NuGet packages. The two images ran
in sequence on a private, unexposed Docker network with one disposable SQL
database and an encrypted disposable key ring. The upgrade applied
`20261110112020_TrackClassSessionTeacherInheritance`. Rolling back the API
image with migration execution disabled preserved that record in SQL and
both synthetic tenants. The existing Admin bearer worked and cross-tenant
reads were refused before and after rollback. Roll-forward and SQL outage
recovery passed. The runner removed its own resources and restored PENTA
health to HTTP 200.

This is a local revision rollback rehearsal. Staging deployment, Azure PITR
and private Blob restore remain open. C48 changes the frontend helper and tests.
The API tree used for this rehearsal is identical in the C48 integration.

## C48 protected media integration

Integrated the three C48 return paths at
`67f24f79d1ab6ca3fc5b81b215b4637cdfefa312` on top of `f1f6215`.
All 62 new boundary tests and 28 retained client tests passed.
The exact-lock Next.js 16.4.5 production static export and TypeScript
checks passed with 682 local export files fingerprinted before the browser run.

The live Chromium run `99b34f601c324463ad774c05298d36a3` used real
Identity, SQL run `03adc045637a4577bab1cdaf05752a82` and a private disposable
Azurite account. It recorded a teacher attachment through the browser's
synthetic microphone and verified upload persistence and media decoding.
Student HEAD/GET returned 200, and authenticated Range GET returned 206 with
the correct 16 bytes. Cancelling a held preview GET cleared the preview and
an unmodified retry decoded. Student and linked Guardian native downloads
both reproduced the exact 17,677-byte attachment. Anonymous access returned
401 and the unassigned Teacher got 404. The 59 observed in-browser API
responses included no HTTP 429 or 5xx. The SQL runner removed its database,
login and owned containers, then restored the two original PENTA services.

C48's 68 MiB transfer, malformed-response and incomplete-read Chromium
checks used a separate loopback synthetic fixture. The Desktop live test above
covered a small Blob-backed recording. The combined evidence leaves physical
iOS/Android, larger real storage transfers, broader interruption/revocation and
production restore acceptance open. BUG-SEC-0001 and the other five critical
records stay open.

## C49 preview lifetime and live Guardian revocation

Integrated the three C49 files at
`ed252d3d631b85fc1374fa2e6e19f4983716e3a0` onto `2b28699`.
The independent hosted verification for `2b28699` passed all 2,493 API
tests, 26 SQL modules, 75 private Blob assertions and the frontend,
production export, browser and API image checks. Its paired Core job
remained blocked by the repository secret prerequisite.

[C48 hosted run](https://github.com/AcademyDesk/AcademyDesk/actions/runs/44058511583)

The C49 integration passed 62 private-media boundary and 28 retained media
client tests. All 16 actual component/Chromium scenarios passed at 390 and
1280 pixels, including file changes, abort, close, remount and native target
cleanup. The locked Next.js production build and TypeScript checks passed.

The Desktop live run `d2bc234d32b048ef8f688a7d2b496c04`, disposable SQL run
`1c898532f1f24d879c3a1cb797403b8e`, again passed actual Teacher recording
and upload, Student preview and cancellation, a 206 response with the exact
authorized byte range, and byte-exact Student and Guardian native downloads.
An authorized pre-revocation ticket downloaded the exact recording as a
positive control. The Admin then revoked the Guardian's only linked learner.
The original Guardian bearer got 404 on HEAD, GET and a new ticket request.
A separate ticket issued before revocation was also denied without the file.
This used real Identity, SQL and a private Azurite fixture. The runner
disposed the owned fixture and restored both original PENTA services.

Same-instance component replacement was exercised at the browser fixture,
while the live portal run covered cancellation/retry, link revocation and
download authorization. Physical iOS/Android, larger real-storage
transfers, staging restore and all six critical records are still open.
