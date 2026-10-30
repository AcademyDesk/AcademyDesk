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

## Supplemental consumer and access checks

- Both repositories' clean status and remote refs were checked. PentaMini remains
  at2bf76b54054ba610071e3435c94cf36bedff3a99; its existing virtual environment
  reran49 tests PASS, one inherited Starlette deprecation warning. The initial
  restricted-socket invocation stalled in TestClient; it was interrupted and the
  normal network-permitted run completed in0.38s. No inference readiness claim.
- Actionlint1.7.7 passes all repository workflows after the work-item wiring.
- Actual LibreOfficeDev26.8.0.0.alpha0 build2c87e51eeaa2b413ff4ae097b2705eea1995d8e5
  imported a synthetic corpus produced by the unchanged frontend csv.ts encoder.
  Its raw calibration imported =1+1 as a formula with numeric result2. All18
  non-NUL protected rows imported with zero formula cells, including15-style
  marker/control cases, quoted comma/newline, Unicode and ordinary/blank controls.
  Import settings explicitly enabled formula evaluation and disabled forced text:
  `44,34,76,1,,1033,false,true,true,false,false,0,true`.
- The initial multi-file invocation (including NUL) timed out after45s and
  produced no protected document. The18-row non-NUL retry converted. A fresh
  per-document/profile probe then passed both18 non-NUL and19 NUL-containing
  corpora, with zero formula cells; raw calibration remained numeric2. The
  original timeout is retained; it was not established as a NUL product defect.
  Apostrophes remained visible and control whitespace was normalized by Calc;
  no exact spreadsheet roundtrip/content-preservation guarantee is claimed.
- Consumer checks use simple arithmetic only, no macros, external functions or links,
  fresh workspace-owned LibreOffice profiles and scratch files under
  /workspace/work/csv-spreadsheet. They do not exercise HTTP/SQL/browser downloads.
  The repeatable probe is QA/tools/check-csv-spreadsheet.py (requires Node24,
  Python3 and soffice); it preserves exact encoder bytes, records source/encoder/
  probe hashes and import/version details, owns fresh profiles, and fails on
  timeout/missing documents/failed calibration/formula cells. Its first draft
  normalized line endings during subprocess capture; corrected byte-preserving
  final source was rerun successfully. Exact-byte evidence is under
  /workspace/work/csv-consumer-exact/c513bcbfcc0a49758a9b2da47e492a21.
  The available alpha build is not acceptance of supported stable Excel/Calc.
  BUG-SEC-0002 remains OPEN. Import token meanings were checked against the
  [official filter documentation](https://help.libreoffice.org/latest/en-US/text/shared/guide/csv_params.html).

## Critical acceptance mapping

These are scoped evidence boundaries, not a replacement test program or issue
closure. All five existing critical records remain mandatory under09_RELEASE_GATES.

| Issue / gate | Observed bounded evidence atc7fca5f | Remaining acceptance |
| --- | --- | --- |
| BUG-DATA-0001 reconciled balances | Real reconciliation/consumer reads, exact settlement/excess denial, deterministic collection races and audit rollback | Real Admin/family browser workflow through refreshed SQL-backed UI; complete critical/role/device matrix |
| BUG-DATA-0002 adjustment limits | Three original variants, approved/pending/rejected consumer distinctions, final-cent/overbalance checks, duplicate/concurrent approvals and audit rollback | Broader real browser approval/payment workflow and complete critical matrix |
| BUG-DATA-0003 payroll | Monthly/SessionBlock default/override positive/zero-net boundaries and fresh no-write snapshots; stored-history migration; synthetic browser PDFs | Real authenticated payout/teacher workflow; duplicate/concurrent payout behavior and full critical/device acceptance |
| BUG-DATA-0010 payment transitions | Actual full/partial/adjusted void/evidence paths, approved restoration matrix and deterministic restore/collect/void/reconcile races | Complete browser/permission/lifecycle regression and refreshed UI agreement on final candidate |
| BUG-SEC-0001 private files | Original legacy/direct/range denial and revocation checks;75 private Blob assertions including native tickets, durable resume and completion rollback/recovery | Wider real portal/device/recording/large-transfer acceptance and critical regression; production storage configuration |
| Security/recovery | Strict role/session/native refresh modules and six-stage real owned SQL restore | Cross-tab/browser/device/lifetime/security configuration cases; Azure PITR/retention, private-media restoration and application/schema rollback |

## Export security SQL/HTTP acceptance expansion

The next QA-only increment adds mandatory `ExportSecurity` to the existing owned
runner/CI. It seeds15 synthetic dangerous-text variants in each actual export
path (students, guardians, enrollments, invoices, payments and filtered audit).
Invoice suffixes avoid collation/uniqueness collisions without changing dangerous
leading characters. Actual response bytes are decoded as strict UTF-8 and parsed
by the framework TextFieldParser with quoting enabled and whitespace retained.
The oracle requires exact apostrophe-prefixed field content, expected column
shape, ordinary typed amount values, attachment/content-type headers, no foreign/
filtered sentinel and unchanged full fresh SQL domain/audit snapshots.

The31 grouped HTTP cases include Admin success, anonymous401, foreign403,
Teacher403, existing FinanceUser export permission200 versus audit403 and unknown
resource404. No role grants, production export changes or altered business rules.
SQL harness compilation0warnings/errors and workflow actionlint PASS. Actual
candidate execution remains pending until its source-bound run is observed.

Independent local parser calibration also passed all15 dangerous values through
actual API SpreadsheetCsv output and the framework TextFieldParser, with exact
cell boundaries/content and typed42 amount; no configured database was used.

## Verified work-item candidate CI

Run [40920810451](https://github.com/AcademyDesk/AcademyDesk/actions/runs/40920810451)
completed SUCCESS for exact97f301ebb62aed223f4cf90c4d3b297151a396b6; actual
job123613198709 steps/logs read. WorkItemIntegrity ran its fresh owned fixture,
audit fault enabled/removed, exit0, migrations83+7 and guarded cleanup PASS. The
runner's older log allowlist suppressed WORKITEM case/summary lines; it did not
skip the module or relax failure propagation. The next QA increment explicitly
allows the known WORKITEM/EXPORTSECURITY prefixes and checks57/31 inventories.

API1,217/1,217 and frontend1,152/1,152 passed. Strict role/session/native refresh
modules passed; six-stage Recovery again restored exact70-table/schema hashes,
ownership/migrations/privileges and Identity/HTTP. FinanceIntegrity12, Blob75,
dependency audits, types/lint, build, payroll PDFs/assessment DOM, API image/smoke
and tracked-source cleanliness all passed. These validate the work-item product
source. The subsequent export verification batch has no application changes and
still requires its own actual SQL execution.

## Final assignee role-name normalization correction

Review found a remaining mismatch with Identity's case-insensitive normalized
roles: an active same-academy user with role display name `teacher` passed the
case-sensitive portal exclusion. Added a focused reproduction:12 assignee cases,
11 passed/1 failed; the new lowercase Teacher case was accepted before repair.
The shared exclusion now uses OrdinalIgnoreCase for Student/Guardian/Teacher.
No permissions or valid staff/admin/custom-role eligibility were changed.

The owned WorkItemIntegrity fixture now retains normalized role TEACHER while
changing its display name to teacher, testing the real Identity representation
in both writer paths. Its inventory remains57. Final full API **1,218/1,218 PASS**,
zero failures/skips; SQL harness compilation0warnings/errors and actionlint/diff
checks PASS. This supersedes1217 only for the corrected candidate. The earlier
97f301e successful CI remains historical;256b160's QA-only run is superseded by
this fix. The final candidate must execute its own WorkItemIntegrity, ExportSecurity
and security/recovery modules before its SQL/CI result is accepted.
