# Current-image disposable SQL integration — 22 October 2026

## Scope / current status

Continue existing E9, `D:\AcademyDesk-codex-p0`, branch `codex/penta-search`, HEAD
`b1f8cb6f4209da7fbd14f8089fcb6792438684c4`. Actual already-built API candidate:
`sha256:afc653e56a4231451fd53713bbf6fbf1188db8df4d4bdd382d8b87a2e4e94a70`,
build `0a5f8d2c708a40a994dd97e9f6d12425`. No application/Dockerfile/package change,
main/Mini/Git/Azure write, production database or model activation. No additional
PENTA service stop or Docker restart is authorized by this packet.

**CURRENT IMAGE BOUNDED SQL/AUTHENTICATED SUITE: PASS,30/30.**
This is fresh-schema and selected role/tenant/session acceptance, not full release.
Previous9/9 no-database smoke and63file artifact passes remain retained.

## Bounded exact-image helper

`QA/tools/api-image-sql.cjs` verifies successful image receipt, all482source hashes,
image label/identity, cached SQL image identity and unchanged3GiB initial memory
admission. SQL image is already cached; no pull/download. An internal-only fresh
network and uniquely labelled SQL/API containers use no published ports and no
persistent mounts. No inference service joins the network. SQL is2GiB/2CPU,
memory-limit1536MiB; API512MiB/1CPU/read-only/capabilities dropped/no-new-privileges/
bounded tmpfs. Credentials generated per run stay in memory/environment, not logs
or QA files; raw HTTP/tokens/connection strings are never retained or printed.

Migration API uses only the fresh run-owned SQL database and bootstrap creates one
synthetic platform identity. After migration, that API is removed; a second instance
of the exact same image uses a SQL login scoped to db_owner of only that disposable
database, no server-wide grant. This is not production least-privilege qualification.
Migrations/bootstrap are disabled in the runtime instance. Both actual context
migration ID sets must match current source exactly:102application+7Identity.

Planned native HTTP checks: anonymous denial; platform login/session; four synthetic
registrations, two real API academy creations; own-tenant exact lists/payments,
foreign-tenant denial in both directions; actual Teacher/Student roles/tenant
sessions and financial/foreign-academy denial; disabled PENTA404; existing bearer
rejected immediately after authoritative database deactivation. Synthetic financial
registers stay empty; this does not qualify invoice arithmetic/concurrent payments
or a Student's protected-record row access. No qualified inference is activated.

Cleanup finally checks exact run labels and immutable IDs, removes only run-owned
containers and the empty run-owned private network, then verifies complete original
container/volume/network inventories. Synthetic DB/identities/credentials exist
only in the disposable SQL container and disappear with its removal. No prune or
other container/data removal is permitted.

## First attempt — retained helper failure

Run `7909e4ec87c74345a297ace3fa44f2ff` passed initial admission (~3.52GiB). Cached SQL
image `sha256:4402d880dd4c34bfa7d8705e56a86cd6c88da80a1f6bbbe741f999e76264a090`.
The API applied109migration rows. A direct read-only probe of the exact owned SQL
observed109, and direct real `/health` HTTP returned200/Healthy with chunked JSON.

However, the helper's Docker-output wrapper trimmed trailing CRLF, damaging the
chunked HTTP terminator before parsing. It therefore could not accept startup and
hit its unchanged120s startup bound. Recorded exit1, zero accepted checks,
failedPhase `candidate migrations/bootstrap`. **NOT a SQL/auth acceptance PASS**;
no authenticated tenant/role cases ran in this attempt.

Receipt: `QA/EVIDENCE/api-image-sql-7909e4ec87c74345a297ace3fa44f2ff/result.json`.
Both exact owned SQL/migration containers and private network removed;
ownedResourcesRemovedtrue/inventoryPreservedtrue. Final29containers41volumes,
two original PENTA services still running/inference healthy. Only synthetic test
data discarded; existing images, volumes, containers and QA evidence retained.

Corrected Docker wrapper preserves exact response bytes. Parser regressions cover
chunked JSON, header/body status distinction, truncated chunks and empty401 bodies;
migration inventory checks known contexts/unique IDs/nonempty sets.21combined
helper tests PASS before first run; the two parser/inventory tests pass after the
transport fix. No status assertion/startup bound/production code was weakened.

## Corrected HTTP run — verified migrations / partial API evidence

Explicitly approved service window `6ab6250f3fec433f88e6a6fb3a38cb60` reached
3.366GiB under the unchanged3GiB guard. SQL run
`ac946c7dab7a464eb4343377ce048b0b` verified the exact109migration IDs, accepted SQL
health and runtime startup with the separate database-scoped login. Fourteen checks
passed: migration/health, anonymous denial, real platform login/session, four
synthetic registrations and two API academy creations with fresh logins.

It then failed a Docker exec during SQL fixture setup before tenant/role-denial
tests. Result false/exit1, failedPhase `two synthetic tenant fixtures`;
no SQL numeric error was captured by that helper revision, so exact cause is not
proven. Do not call this an application bug or a full SQL/auth acceptance pass.
Exact owned resources removed/full original inventories preserved. The window
restored both original services/same IDs+images/inference healthy, with full
container/volume/network/image inventory equality;29containers41volumes.

Direct fixture sessions now explicitly set the SQL options required for filtered
indexes, including QUOTED_IDENTIFIER and ANSI settings, without disabling indexes
or constraints. Existing Identity migrations contain filtered indexes. This is a
source-supported correction, not proof that the original exec failed with1934.
Safe exec diagnostics now retain only exit/signal/numeric SQL error classification,
not SQL/credentials/response values; fixture subphases are distinct. Two new tests
lock these controls. **23combined tooling tests PASS**; original assertions and
startup/memory bounds unchanged. Root cause/runtime acceptance still needs a retest.

Owner subsequently approved bounded local validation windows for remaining SQL/API
work: stop only the two PENTA services, one guarded test at a time, restore exact
services immediately afterward including failures. No restart/download/prune/model/
production DB/Azure authority. That authority does not permit arbitrary PENTA
interruptions, additional Docker restarts or bypassing memory/acceptance guards.

| Partial-run receipt | SHA256 |
| --- | --- |
| SQLac946c7d result.json | `E7FD543D7D1F64408BB39D0C86DDAF121156B6CD19D9EB1ABB65DB35EC6422C1` |
| Window6ab6250f result.json | `24D49B306486EE93CC83AF15943F9ADA30D02A9B9382CDDECCCCDAFC4581B578` |

## Corrected fixture session / full bounded suite — PASS

Window `183b84e66ffb466896995345acd737dc` passed unchanged3GiB admission at
3.341GiB. New independent SQL run `b9bc968afc9a489996b7ea6e609f7ebf` completed
**30/30 checks**, exit0, without rebuilding/changing the candidate/API source or
relaxing any original assertion. Explicit fixture session options allowed setup
and later reads/denials to complete. The previous exec's numeric cause remains
unproven, because that older helper did not capture it; do not invent1934 evidence.

| Proven on actual candidate and disposable SQL | Outcome |
| --- | --- |
| Both context migration sets, exact current source IDs |102application+7Identity|
| Runtime startup, migrations/bootstrap disabled, database-scoped login |health200|
| Anonymous academy list |401|
| Platform bootstrap login/session, four synthetic registrations, two API academy creations and fresh logins |Expected200/201|
| Two owners' academy lists |Exactly their own academy only|
| Two owners' own-tenant financial registers |200/exact empty arrays|
| Each owner requests the other academy's payments |403 in both directions|
| Teacher/Student real login/session |Expected role and academy binding|
| Teacher/Student own financial and foreign-academy payments |403 in all four cases|
| Disabled PENTA chat health |404/no inference enabled|
| Existing bearer after current database user deactivation |403|
| Final synthetic counts |2academies/5identities/0payments|

All482candidate source hashes unchanged. No HTTP429 observed in the accepted
checks. No financial write/race, student protected-record row scope, refresh-token
family, model semantics or frontend/device completion is inferred. Login success
does not qualify production durable key rings; candidate containers use temporary
keys and this suite does not test cross-replica login persistence.

Exact owned SQL/API containers and private network removed; only disposable
synthetic data discarded. Wrapper restored original services immediately afterward,
same IDs/images, both running and inference healthy. Window exit0,
restorationPassedtrue/inventoryPreservedtrue; full original container/volume/network/
image inventory equality,29containers41volumes. No Docker restart or downloads.

Final23combined tool tests and helper syntax PASS; log
`.build-check/session-resume/api-image-sql-accepted-tests.log`. Main HEAD
`f7af512f884ca5fe8ac3ddb287c852492592e709`/tracked tree clean, Mini HEAD
`2f8806967709a1a7ec29e5176e7c31968adbf6aa` unchanged, staging empty. QA/EVIDENCE
local/untracked; .build-check ignored. No commit/push/deploy or issue closure.

| Accepted receipt | SHA256 |
| --- | --- |
| SQLb9bc968a result.json | `C9CB5AD4DB32DE234204A5147AFEE65E9435C8067833C835FB725DA1EF43068B` |
| Window183b84e6 result.json | `EC154F53A9B2B5BAC5EADBB94C857138CA317067837FB62C71D945DA4921E084` |

## Current next gate

Do not rerun passed acquisition/build/basic smoke or restart completed audits.
Continue exact-candidate finance/transaction and migration-upgrade/data-preservation
evidence, durable-key/recovery/rollback, full critical E2E/device/security/release
gates and independent Mini review/qualification. Existing financial and auth issues
remain OPEN at their unexecuted breadth. All11 broad packages remain OPEN.
Standing owner permission allows bounded temporary two-service windows for local
SQL/API validation, one guarded test at a time with immediate exact restoration
including failures. No restart/download/prune/model/production/Azure authority.

## Original next checkpoint (historical)

Corrected retry requires unchanged3GiB memory admission; after cleanup RAM was1.91GiB.
Do not blindly rerun/restart/stop PENTA or lower guards. When memory naturally
recovers, run `node QA/tools/api-image-sql.cjs 0a5f8d2c708a40a994dd97e9f6d12425`.
Retain failure and pin new receipt separately. Then continue the existing exact-
candidate migration-upgrade/auth/tenant/financial/E2E/security/device/restore/
rollback/model release gates. Fresh schema success alone is not upgrade acceptance.
All11 broad packages and independent Mini qualification remain OPEN.
Sol High for consequential integration/security; Medium for settled documentation.
