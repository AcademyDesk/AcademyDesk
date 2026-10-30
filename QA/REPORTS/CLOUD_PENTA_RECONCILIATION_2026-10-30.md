# Cloud/PENTA source reconciliation — 30 October 2026

## Source and boundaries

Branch `codex/cloud-penta-reconcile-20261030`, worktree
`D:\AcademyDesk-penta-cloud`, starts at published student checkpoint
`13ed932bad477bf4c040922a2363ebae857a5bfc`. Student/teacher/enrollment contracts,
transactions, finance safeguards and current stored authority are retained.

Reconciled all source changes from the other profile's8commits after10a60ee
througha63e392: payroll print/PDF, attendance and assessment generation fences,
work-item assignee/entity validation, role/session/recovery/export QA and safe
native failure diagnostics.41paths whose current content matched the cloud base
were brought forward exactly;6shared files were reconciled explicitly. Both CI
SQL inventories remain, including strict collection/adjustment races and shared
enrollment/teacher/student modules. No wholesale branch replacement, main merge,
force push/history rewrite, policy rollback, Azure or customer database access.

This is source reconciliation; a63e392 is not made an ancestor by a Git merge.
Do not reapply its8commits blindly on top of this checkpoint. Retain the recorded
cloud provenance and this source/test binding. Root handoff preserves both
profiles' historical records with current acceptance distinguished.

## Actual checks

Full unfiltered API:1645PASS/zero failed/zero skipped, window
`cf5e396a5cb64fecacedd2cd5c98c1ff`. Later harness-only corrections reuse that TRX
only after exact unchanged application/test hashes and counter verification.
Harness rebuild succeeds with0warnings/errors. Seven affected frontend suites
pass93/zero failed/skipped, rerun with exact cached TypeScript5.9.3/React19.2.8
versions checked against the lock. These execute actual page code with synthetic
transport, not browser rendering or physical devices.

Eight real SQL modules PASS:

| Module | Owned SQL run | Window |
| --- | --- | --- |
| WorkItemIntegrity | f7a2a3988f1f4c438d1b1fe5215ad4e7 |438a9ae3e40645b8846325fab0eee51b |
| ExportSecurity |006e60d1fa8b4a6cb41cb0b0a6e4c38e |438a9ae3e40645b8846325fab0eee51b |
| RoleReplacement |3b5116adb5714b17bb88bc94ec668c94 |438a9ae3e40645b8846325fab0eee51b |
| RoleRevocation |dc47d5fae25e4876bc4467108ad2fd95 |438a9ae3e40645b8846325fab0eee51b |
| SessionRevocationRepair |f0d40b38c2864fdebbfce0a7d0f63f39 |438a9ae3e40645b8846325fab0eee51b |
| SessionRefresh |aa30335be47e4fff80e5275e8bd3148d |ef1e4f25b0e544c6b4763d9d2c2a550e |
| RefreshConcurrency |958a2a3f46124621adbb88ca2884e772 |ef1e4f25b0e544c6b4763d9d2c2a550e |
| Recovery |b3bcf4546ea841819d2501f0682ea501 |ef1e4f25b0e544c6b4763d9d2c2a550e |

The retained5plus successor3 use identical application/test/harness source pins
and identical compiled SqlHarness SHA256, verified before combining results.
All8prove84app/7Identity migrations, scoped runtime privileges, ownership-marker
negative cleanup and exact run-owned database/login/container cleanup. Counts
are overlapping module evidence, not an aggregate application percentage.

WorkItem57and Export31checks pass. Role replacement retains its extended56-case
matrix and existing core checks; role revocation runs strict enforcement, not
diagnostic-only acceptance. Native refresh proves11helper HTTP requests and one
actual identity/membership/link/audit mutation. Refresh concurrency proves25HTTP
requests/four controlled overlaps, one refresh per group, late401reuse and no
expired-refresh replay. Recovery performs6guarded full copy-only/checksum backup,
VERIFYONLY/header, post-backup canary, actual RESTORE, full ordered table/schema
fingerprints, restored ownership/migrations/runtime privilege and HTTP checks.
This is disposable SQL recovery, not Azure PITR/media/customer backup acceptance.

Final exact original PENTA service IDs/images/running/healthy,API200,complete
Docker inventory and shared-lease release PASS. No qualified-model inference occurs.

## Retained failures and local dependency correction

First cf5window completed API1645/93frontend and2SQL modules, then a fresh SQL
instance rejected its firstsa login before any QA database was created. Later
owned-instance diagnostic authenticated successfully; marker/container identity,
loopback binding and no mounts were verified. Failure evidence/container retained
stopped. Added bounded sixty-second authenticated startup readiness before any
DB creation: only18456/18401startup errors retry, exact server is checked, no
scenario/mutation retries or weaker assertions. Wrong credentials cannot pass.

438window passed5modules, then the native refresh helper failed to resolve
TypeScript in this new worktree before scenarios. A local ignored require-hook
resolves only version-checked cached TypeScript/React paths. No source dependency,
package/lock, download or credential change. The final3modules rerun with that
same hook inherited by their Node processes.5already-passing SQL modules are
retained only after source and binary equality. Both failed owned containers are
preserved stopped; earlier interrupted student SQL container also stays stopped.
No pruning, Docker restart or deletion of QA/EVIDENCE occurred.

## Remaining gates and next task

No exact-current-lockfile web build is claimed: cached Next is16.4.1; lock requires
16.4.3. No fresh combined GitHub CI, actual browser/PDF, cross-tab, physical device,
Azure backup/PITR or production acceptance. The older cloud's browser reports are
historical proof for their recorded source, not automatically this whole candidate.
Existing P0/critical issues and release protocol remain open.

PENTA host preparation is in separate `D:\AcademyDesk-penta-host` from13ed932:
existing protected session/draft/approval/receipt/recovery source and12table
mappings are being integrated. It is UNVALIDATED and not part of this checkpoint.
Move that work onto this published combined base without losing uncommitted work;
then compile/test, generate and inspect an additive combined migration, verify
safe upgrade/downgrade and retained actual SQL host flows before publication.
No tools/models are activated by source preparation.

Mini/Core source remains c3c61b9; its previously observed full cloud CI is failed,
semantic qualification NOT ACCEPTED, and the owner's stop before candidate
weights loading remains. No model experiment, training, activation, paid compute,
external provider enablement or90%AI claim. Continue Sol High for protected
host/authority/migration integration; use Sol Medium for settled UI/docs.
