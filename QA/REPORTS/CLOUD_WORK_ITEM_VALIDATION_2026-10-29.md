# Work-item validation and security/recovery verification — 29 October 2026 IST

MODEL: Session Codex model; no override or delegation.
TOOL: Codex managed-cloud terminal and GitHub connector.
REPOSITORIES: AcademyDesk/AcademyDesk; AcademyDesk/PentaMini access verified.
BRANCH: codex/release-export-security; base c7fca5fe0c0c2f0513e4e008d99e5634aa656a31.
ENVIRONMENT: Fresh clean older checkout; fetched verified feature-branch source.
TASK: Recover interrupted work, repair work-item/finance follow-up validation,
inspect actual CI security/recovery evidence and continue existing acceptance.
MAY MODIFY: Work-item validation/controllers, affected regressions, SQL runner,
verification workflow and existing QA/handoff records.
MUST NOT MODIFY: Customer/configured DB, payroll/restoration policies, unrelated
UI, main, deployment/environment publication or PentaMini application source.
VALIDATE: Reproduce rejected references, positive controls, full API regression,
SQL compilation and source-bound CI with actual SQL/audit/security/recovery.
NEXT ROUTE: Codex for exact candidate CI and remaining critical acceptance.

## Recovery and implementation

Neither repository had uncommitted changes. AcademyDesk was a clean older
`work` branch at f7af512; fetched remote feature branch was exactly c7fca5f.
No interrupted work-item test or SQL patch was present. Rebuilt after reading
the controllers, current staff directory, issue BUG-DATA-0048 and QA records.
The user's earlier 37-test result remains historical and is not claimed as a
recovered run. No reset, clean, stash or overwrite of existing work occurred.

New pre-fix controller suite: 33 cases, 29 failed/4 passed, zero skipped. Missing,
foreign, empty and ineligible assignees; incomplete/mismatched/unsupported links;
unknown follow-up priorities and oversized text were accepted by the old source.

Both public writers now validate a supplied assignee against an active,
unlocked Identity user in the same academy with scoped staff roles. The existing
staff directory excludes Student/Guardian/Teacher identities; those cannot be
assigned here. Platform owners, roleless users and foreign-academy roles are
also excluded. Existing administrative/staff/custom academy roles remain
eligible; null means unassigned. No role or permission is granted by assignment.

The generic create contract permits the existing application-produced `Invoice`
and `PersonDocument` links, with a complete, same-academy type/ID pair. Unlinked
work remains supported. Unknown types fail closed; this is not a general entity
resolver. Type and title trimming, priority defaults, optional context/due date,
status and existing collections semantics remain. Finance follow-ups validate
priority/note length and share the assignee rules. Existing strict Admin priority
inputs remain strict; finance retains its trimmed/default High behavior.

Admin create/status/collections actions opt into the existing domain/audit SQL
transaction boundary. Finance already uses that boundary. No schema migration
or changed endpoint authorization. Platform-owner audit bypass remains the
existing behavior; it is not broadened by this work. Eligibility is checked at
creation time; concurrent deactivation/role changes and later historical queue
ownership are not new serialization/lifecycle guarantees.

`WorkItemIntegrity` is now mandatory in development CI. It uses the existing
fresh marked GUID database and owned loopback Docker runner. It covers real
Identity + HTTP + fresh SQL: both writers' assignee matrix; supported/foreign/
missing/type-mismatched links; staff/admin/custom/unassigned controls; finance
and teacher role boundaries; filtered list/status behavior; and audit insertion
faults rolling back create, status, collections metadata and finance follow-up.
No configured DB or development seeder is used. Compilation alone is not SQL
acceptance; actual candidate results are recorded below when observed.

## Verified base CI evidence

GitHub connector access works for both repositories with push permissions.
Shell networking requires the tool's network permission; it does not require
changing or publishing the environment. An initial shell proxy connection
failure was resolved by using that permission. NuGet caches were redirected to
`/workspace/work` because the inherited home cache is read-only.

Exact base c7fca5f run [40840527086](https://github.com/AcademyDesk/AcademyDesk/actions/runs/40840527086),
job 123335512505, completed SUCCESS. Job steps and decoded actual logs were read:

- API: 1,179 passed; frontend: all 1,152 passed with genuine generated SQL evidence.
- Role replacement: 37 observations, zero policy gaps; extended 56 cases,
  21 injected failures, protected roles/explicit grant control accepted.
- Strict RoleRevocation: 37 observations, zero gaps, diagnosticOnly=false.
- Session revocation: 36 cases, zero gaps; SessionRefresh: 11 actual native
  helper HTTP requests and fresh SQL assertions passed.
- RefreshConcurrency: 25 native HTTP requests/four controlled overlaps,
  single refresh/replay and expired-refresh behavior passed.
- Recovery: all six stages passed. Seventy table fingerprints; copy-only/checksum
  backup, VERIFYONLY/source header, changed canary, actual RESTORE with exact
  table/schema SHA256 equality, ownership/migrations/unprivileged runtime login,
  and restored real Identity/HTTP healthy200/anonymous401/authorized200.
- FinanceIntegrity: six deterministic SQL races (two waiters observed before
  release), three audit-fault rollbacks, status/cancellation/stale-cache cases.
  Approved zero-net payroll rejection and validated restoration checks passed.
- Private Blob: 75 actual Identity/HTTP/SQL/Azurite assertions and owned cleanup;
  original legacy private-file checks also passed.
- Dependency audits, TypeScript/lint, full build, four real payroll PDFs,
  assessment DOM cases, API image/startup checks and source cleanliness passed.

These complete the scheduled bounded security/recovery checks on c7fca5f. They
are not acceptance of subsequent source. No fabricated local communication log
was created; remote full frontend success does not backfill this fresh machine.

## Current local verification and limits

Focused work/collection tests initially passed 61/61 after repair. Full API then
passed 1,212/1,212. Additional lockout and action-boundary regressions were added; final source
full API passed **1,217/1,217**, zero failures/skips. Candidate SQL outcomes
will be recorded when observed.
SQL harness compiles with zero warnings/errors. Production-gate fixtures pass
14/14, and the real CLI still correctly rejects all five open P0 records.

Local Docker daemon works, but the SQL image pull is denied (`Forbidden` on
image configuration/redirected content). No SQL container or configured database
was used locally. CI is the available actual SQL execution path. No network
policy bypass or environment configuration/publication was attempted.

All five P0 records remain OPEN under the existing closure chain. Physical
devices, broader authenticated browser journeys, full critical regression,
production Azure backup/PITR/retention, private-media restore and application/
schema rollback remain required. This synthetic owned SQL recovery drill cannot
accept those production controls. No merge into main or deployment occurred.
