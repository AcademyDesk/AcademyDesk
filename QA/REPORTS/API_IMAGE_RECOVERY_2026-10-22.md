# Exact-candidate disposable SQL backup/restore — 22 October 2026

## Accepted successor

**PASS47checks** in SQL`95548d27905e4b9ca15e1f2667b0a848`. Window
`d2b9cb39a15c4937ad5b96bd83e66fb3` admitted at4.011GiB, exit0/runtimePassedtrue/
restorationPassedtrue/inventoryPreservedtrue. Eight before/after table counts and
full-row canonical hashes match; all109histories restored, CHECKDB clean. Real
replacement-image login/session/ledger600paid400balance/role+tenant restrictions
and fresh inactive-account login denial passed. Old ephemeral-key bearer correctly
returns401; durable-key/session recovery is not inferred. Synthetic database
recovery segment8633ms is not a production RTO. Exact application source unchanged.
Owned SQL/API/network removed; original PENTA IDs/images restored/API running/
inference healthy/full inventories match/29containers41volumes. No restart,
downloads, main/Mini/app/package/Git/Azure action. Earlier unsuccessful/blocked
attempts below remain historical failures, not relabelled successes.

Local accepted receipt SHA256s:

- SQL:`C3654C2DAE3E344691F76CA85B40319693E094796C08B75B9A8C7731BD5AF0BA`.
- Window:`95BBAC75593384D880F5932B78C1F47B41C72CA7BAA3FDB5E91308C6ACC44E39`.

Next existing gate: certificate-encrypted key recovery across actual replacement
containers, current authority/revocation and environment isolation. Production
storage/ACLs/rotation/full backup/rollback, devices/security/model/release remain OPEN.

## Scope

Existing E9 recovery gate, `D:\AcademyDesk-codex-p0`, branch `codex/penta-search`,
HEAD`b1f8cb6f4209da7fbd14f8089fcb6792438684c4`. Exact accepted API build
`0a5f8d2c708a40a994dd97e9f6d12425`, image
`sha256:afc653e56a4231451fd53713bbf6fbf1188db8df4d4bdd382d8b87a2e4e94a70`.
Cached SQL only, internal Docker network, synthetic data and generated temporary
credentials only. No application/package/migration changes, customer database,
Azure, model/download/training action, Git write, main/Mini edit or Docker restart.

Standing owner-approved bounded service windows apply. Stop only the two original
PENTA services, keep the3GiB guard, run one isolated test, then immediately restore
the exact original containers/images and health, including after failures.
Remove only run-labelled SQL/API/network; preserve all other inventories.

## Acceptance

`api-image-sql.cjs --recovery` retains the original30authenticated SQL/role/tenant
checks, creates student/invoice1000/payment600/reconciliation through real API,
then quiesces only its owned API. `api-image-recovery.cjs` must:

- Record counts and canonical full-row SHA256s for eight relevant tables:
  Academies,Students,Invoices,Payments,AspNetUsers,AspNetUserRoles,
  __EFMigrationsHistory,AuditLogs. Hashes are generated inside SQL; no raw
  identities, credential material or protected data are copied into evidence.
- Require meaningful synthetic finance/identity rows and all109migration histories.
- Create a COPY_ONLY,CHECKSUM,compressed backup inside the owned SQL container.
- Run RESTORE VERIFYONLY WITH CHECKSUM, then actually restore into a separate
  run-owned database using explicit new data/log paths, without REPLACE/source overwrite.
- Run DBCC CHECKDB and compare eight restored counts/hashes exactly; source
  database must also remain unchanged during restore.
- Start the same immutable API image against the restored database and verify
  real login/session, academy scope, reconciled600/balance400, role/tenant denials
  and inactive-identity fresh-login denial. Prior-container bearer rejection is
  separate: fresh ephemeral keys do not prove durable-key recovery. The original
  same-container active-bearer/deactivated-account403 assertion stays unchanged.
  Do not equate SQL-only verification with API recovery.

The backup stays inside ephemeral SQL storage and is discarded with that exact
owned container. It is not exported or committed. The API uses a database-scoped
db_owner test identity, not a qualified production least-privilege configuration.
Disposable container-local keyrings are not durable-key/session recovery proof.
This does not test an Azure/customer backup, production RPO/RTO, media/key backups,
all-table recovery, historical application rollback or deployment recovery.

## Retained first failure

SQL`4dd23eea27ce4ae2aea1b2a87fa526eb`, window
`1d1875e090e04102806cc7f3d15976c0`:34HTTP checks passed before snapshot-source
failed with numeric SQL156. No backup was created and no recovery accepted.
Snapshot aliases were unquoted; corrected helper brackets RowCount/Digest and
adds a focused regression assertion. Do not fabricate a raw SQL diagnostic that
was deliberately not retained. Exact owned cleanup/inventories and immediate
original-service restoration PASS, originals healthy. No app change.

## Historical checkpoint before accepted successor

Second SQL`e8fef64fda5d468aaf0c6ca1648ec568`, window
`a8dde01a37f44075adbc3818e3143fa9`:45checks passed. Backup/VERIFYONLY/separate
restore/CHECKDB/eight matching table snapshots/all109histories and new-container
owner/Teacher/Student logins, ledger600/400 and role/tenant denials passed.
Database recovery segment8625ms is local synthetic timing, not production RTO.
Final assertion expected403 from a pre-replacement token but received401;
entire run remains NOT ACCEPTED, not relabelled as PASS. Fresh container-local
keyrings do not promise bearer continuity. Corrected test independently requires
old active-user bearer401 and fresh inactive-user login401, retaining the original
same-container deactivation403 check. Durable-key recovery remains OPEN.
Owned cleanup/inventories preserved; original services restored immediately.

Corrected retest window`21670941282b48b6b148335df67924e1` was refused by the
unchanged3GiB admission guard:2,858,622,976available bytes (~2.662GiB) after the
bounded temporary stop/recovery wait. No child helper, SQL container/database,
backup or candidate API started. Final window exit1/restorationPassedtrue/
inventoryPreservedtrue. Both exact original containers/images restored, API
running/inference healthy;29containers41volumes and all image/network inventories
preserved. No further automatic unchanged retry or guard reduction.

Local result SHA256s:

- Second partial SQL:`BB9ADF4C64C2DF94C28699E7AFEF6A83EF6F4C112C0D18953761ECD972C6FF46`.
- Final blocked window:`7D8C737E56BE633159C37825065260271AEBB75315DA3A89969013DC8E2DBAB3`.

Independent final comparison of all candidate application-source hashes PASS;
main/Mini HEAD unchanged, main tracked tree clean, staging empty, QA/EVIDENCE
untracked and .build-check ignored. No push/commit/deployment or issue closure.

Recovery is NOT YET ACCEPTED. Final corrected source has31combined tooling tests
PASS, including five recovery regressions and the retained same-container403
assertion. The47-check real SQL/API retest remains unexecuted due to memory;
do not claim it passed from the45partial checks or source-only assertions.
Earlier migration31/31 and finance5/5 are retained independently, not rerun or
superseded by this drill.

Final QA source SHA256s:

- recovery helper:`87FEC0C12F1F3F65189195555F6EDEB4B9493FAEC2EE59F4E5059096CE8E64A8`.
- recovery tests:`286631B38A01C92D1C1D44D0467A75289B41E053501747CE16638902D009DD92`.
- SQL runner:`FFEC77B89894CC9308336E99EC5264D6EA8FD97784A73C0028D14A93717C3662`.
- service window:`29C52E0916C8A2D7E0A2113230B389450F541E9472EA5B92BD054BB5CCB5226C`.

Next: once memory is genuinely available, run one corrected bounded recovery
window under existing standing authority. No Docker restart, downloads, user-app
termination or production/Azure operation. If the guard still refuses, keep the
gate OPEN instead of running a retry loop. Keyring/session recovery and release
rollback stay independent even after this database drill passes.

Existing issues and11broad release packages remain OPEN. Next route: Sol High for
SQL/runtime/key recovery and consequential security; Medium for settled docs/UI.
