# Historical-schema upgrade validation — 22 October 2026

## Scope and evidence boundary

Existing E9 in `D:\AcademyDesk-codex-p0`, `codex/penta-search`, HEAD
`b1f8cb6f4209da7fbd14f8089fcb6792438684c4`. Current candidate build
`0a5f8d2c708a40a994dd97e9f6d12425`, immutable image
`sha256:afc653e56a4231451fd53713bbf6fbf1188db8df4d4bdd382d8b87a2e4e94a70`.
No app/Dockerfile/schema/migration/package changes, no main/Mini/Git/Azure write,
production DB, downloads, Docker restart or model action. Standing bounded local
service windows apply: unchanged3GiB guard, one isolated test then immediate exact
restoration including failures. No pruning or unrelated data removal.

**Reconstructed historical-schema upgrade PASS: 31 checks.** The current candidate
applied the20pending migrations to89historical histories, producing all109expected
histories with four representative ledger rows unchanged. This does not qualify
the unavailable historical release binary, a production backup, or rollback.

## Source continuity and artifact provenance

Preflight `e05087089b7f4290aac0207a8df2b0e3` verifies baseline commit
`f7af512f884ca5fe8ac3ddb287c852492592e709` has82application+7Identity migrations;
they are the exact ordered prefix of current102+7, leaving20newapplication/0Identity.
No historical migration body/designer changed or deleted; model snapshot changes
are recorded separately. Source continuity PASS is not runtime upgrade proof.

The old report records image `sha256:dd092f9786df2e248f625080fbd000ab7f7f9a1fd773f6f83ab3b44ad6759d9c`,
not currently available. The similarly named cached tag resolves instead to
`sha256:bccd369b5d3a9f677f447c6ac036a4e78d386f6b005b0963f227ee68db60c1e0`,
without revision binding. Do NOT silently call that the documented baseline image.
Full historical-release-binary compatibility remains a separate provenance gate.

Fast safe alternative: reconstruct the baseline schema from unchanged historical
Up migrations in the hash-verified exact candidate assembly. A small QA-only
`MigrationScript` project references existing candidate DLLs, no NuGet packages.
Restore uses an empty local feed/NuGetAuditfalse; build isolated under ignoredD:
artifacts, no version changes/downloads or application startup. Generated SQL and
tool/source/candidate identities are recorded. This is explicitly reconstructed
schema, not an old release binary or customer backup restoration.

Planned acceptance: exact89baseline histories, four representative synthetic
academy/student/invoice/reconciled-payment rows, then original candidate startup
migrations through109. Compare canonical ledger-field hash before/after; run actual
authenticated current-image role/tenant suite afterward. No customer data, rollback,
all-table preservation, old-application schema compatibility or model qualification
is inferred from these four rows.

## Retained unsuccessful attempts

1. SQL run `2d7f2440a5df44e98c7de880ac8284e4`, window
   `341c4d4f336a42f29ca820ce95c85843`: offline generator build0warnings/0errors,
   baseline combined SQL application hit numeric SQL207. Zero accepted checks,
   source untouched; owned cleanup/inventory preserved. Services restored healthy.
   Script generator run `2cdcd447e56e4a8a886bb57c055497f7` retained.
2. SQL run `644de0ebcc974911844d7de52a725b0d`, window
   `0dabac8d0e0e4f75b9bfe7e5c566bdd2`: standalone EF executor in candidate container
   exited139/notOOM before baseline verification. Exact cause not established;
   no claim of database corruption or app defect. Zero accepted checks, owned
   cleanup/inventory preserved, originals restored healthy. Generator run
   `4d61003ace354ccd802b34cb9d4f71ee` retained. No repeated unchanged retry.

Current correction generates SQL via candidate `IMigrationsAssembly`,
`IMigrationsSqlGenerator` and `IHistoryRepository`, putting GO between EF commands
while preserving migration transactions and transaction-suppressed boundaries.
This addresses SQL Server batch compilation of later-added/backfilled columns
without editing published migrations. The old207's specific column cause was not
captured, so the hypothesis is not promoted to proven root cause. Standalone
executor mode is removed from final QA helper; offline generator never connects.
Test window `e75e21c97e6849c2907a6f0f10a0f13a` at3.474GiB built the QA generator
with0warnings/0errors, then offline generation failed before any SQL container:
`InvalidOperationException` requiring model finalization/runtime initialization
before `GetRelationalModel`. Direct offline diagnosis involved no credentials or
database connection. Source-proven correction calls `IModelRuntimeInitializer`
before SQL generation; generation logs now persist locally, not swallowed.
Generator `6194f02813444fbe937fa0da4e9bdb5a`/failure retained. Window restored
original services/inference healthy/full inventory equality, no additional SQL.

Next window `9449f558e5204b3a8cba084ea75ee030` passed first admission at3.0205GiB,
but available RAM dropped below3GiB before offline compiler admission. Correctly
refused with `3GiB guard: migration script compiler not started.` No compiler,
generator, SQL database/container or candidate API started in this attempt.
Immediate original-service restoration began. No guard reduction/restart or
further automatic unchanged retry. Corrected generator build/runtime remains
UNVERIFIED at that checkpoint; do not count prior generator compilation as corrected-source proof.
Final window exit1 with restorationPassedtrue/inventoryPreservedtrue. Both
original services restored/same IDs+images/inference healthy; full inventories
match,29containers41volumes. Available RAM later1.07GiB; no new test started.

## Accepted successor

Window`ea8bcc6c36184470b55bd96740d1f9de` admitted at3.160GiB under unchanged
guards; exit0/runtimePassedtrue/restorationPassedtrue/inventoryPreservedtrue.
Corrected offline generator`18e643a61cdc4d659b0d24f1d213bd53` built with0warnings/
0errors and generated both source-continuous historical migration scripts using
the exact candidate assembly. No packages or images were acquired.

SQL run`95c2d4659dee4fddad2a89b8d24740e5` PASS31checks:89historical histories
upgraded to109, four synthetic academy/student/invoice/reconciled-payment rows
preserved, followed by the30real authenticated role/tenant/security checks.
Canonical ledger-field SHA256 before and after is identical:
`AA42B6CA9CF09DE4B61EA2514B96B664107526C7ADC3C7E7505206ABDA4EE674`.
Final disposable data:3academies/5identities/1payment. Current source unchanged.
Owned SQL/API/network removed;29original containers/41volumes and all original
network/image inventories preserved. Original PENTA container IDs/images restored,
API running and inference healthy. No Docker restart or application modification.

Local receipt SHA256s (evidence remains intentionally untracked):

- SQL result:`E3C71170F709FB80DD2C0396DD931D657C10E19DA9C28284BFF4720FDA5822E9`.
- Window result:`3FDCC0EFFE13D5090C7111623E99A565B5C6EB58A7F60DBA08F2A9E1CCAC0A23`.

Retain every failed attempt above. This successor supersedes their admission/
generator blockers only; it does not relabel their outcomes. The reconstructed
baseline was not created by the unavailable original release binary. Four-row
canonical-field preservation is not all-table/customer-data preservation.

## Remaining bounded work

Continue the existing recovery/rollback/keyring and critical workflow/security/
device gates without rerunning accepted finance or migration checks unnecessarily.
A disposable SQL backup/restore drill is a safe next recovery slice; actual
production-backup provenance, environment restore, shared encrypted key recovery,
RPO/RTO, deployment rollback and independent model qualification remain separate.

26combined tooling tests PASS, original finance race/runner unchanged. Existing
API9/9smoke/role-tenant30/30/finance5/5and artifact receipts retained. All enterprise,
device/security/key recovery/restore/rollback/model/11broad packages remain OPEN.
Sol High for consequential upgrade/security, Medium for settled docs. No issue closure.
