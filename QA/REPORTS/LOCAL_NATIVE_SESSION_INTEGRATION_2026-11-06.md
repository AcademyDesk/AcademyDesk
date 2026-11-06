# Local native session integration — 6 November 2026

Status: BOTH CURRENT-CLIENT SQL MODULES PASSED.
Initial scope below was claimed/published before edits. Owner local Codex on the existing host feature
branch. C3.2 changed the shared auth client; verify its existing native SQL helper
paths rather than repeat the earlier enterprise audit. Exact owned paths:
- QA/tools/session-refresh-http.cjs
- QA/tools/refresh-concurrency-http.cjs
- QA/REPORTS/LOCAL_NATIVE_SESSION_INTEGRATION_2026-11-06.md

Run SessionRefresh and RefreshConcurrency using the existing unchanged SQL runner/
harness, actual current api.ts, real loopback Identity/owned SQL and cached tools.
If old VM browser-global fixtures lack C3.2's event/epoch support, correct only
those fixtures and strengthen epoch preservation/explicit-login assertions; keep
all existing statuses, request counts, no-replay/one-write/SQL identity/audit checks.
No application/SQL harness source, token lifetime, business rule or test expectation
reduction. These controlled native HTTP checks do not establish real browser tabs,
physical devices or whole-app acceptance. Preserve original fixtures/failures.

Existing safe memory windows may pause/restore only the two original PENTA services
while retaining3GiB admission, owned-resource markers and original inventory. No
new package/image download, model activity, main/Azure/customer database action.
Sol High for this source/security integration; settled visuals remain separate.


## Actual current-source results

Only the two native Node client fixtures changed. They now exercise explicit
beginPortalSession with real Node EventTarget/CustomEvent/WebCrypto globals and
opaque login epochs instead of testing solely legacy epoch-less token saves.
Same-login renewal/failed refresh and the other role's epoch must stay unchanged;
exactly two explicit-login notifications are required. No production code or
credential/role/tenant authority changes. All existing request/status, no-replay,
single-flight, typed student identity/membership and audit assertions remain.

| Actual SQL module | Result / source boundary |
| --- | --- |
| SessionRefresh6f1b82d99c8b466b90c5976d4fdd1fdd | PASS,20.1s;11actual helper HTTP requests plus two native fixture logins; Admin/Teacher expired access401→refresh200→replay200, successful Student creation once, expired refresh401/no replay, epochs/role isolation retained |
| RefreshConcurrency299d403ac5e04815b7a350cc03d734c3 | PASS;25actual helper requests/four overlaps; one refresh per group, delayed401receipt reuse, one successful mutation, rejected refresh waiters401/no replay; epochs/role isolation retained |
| SQL preservation | Each independent fixture stores exactly one additional typed Student identity/membership/academy link and success audit; rejected/cross-tenant controls and85app/7Identity migrations retained |
| Resources | Exact owned SQL database/login/container removed after ownership checks; original services/images healthy/API200 and complete Docker inventory preserved; leases released |

First window45d71ca3completed SessionRefresh but refused the second module at the
unchanged3GiBpre-module guard. Its overall result remains FAIL, not reclassified.
Successor5cd77026ran only RefreshConcurrency after safe cache/compaction reclaim;
it reused the first module only after exact api.ts/helper hashes matched. No
repeat of its already-passing SQL mutation, guard reduction or unrelated app kill.
Both windows restored original services even when admission refused work. The
existing unchanged harness build from94cc931cwas reused with its current artifact
hash; no new package, model, image or .NET source change. Raw logs/manifests stay
local. Current application/api/tests/SQL source remains83dc3aa; existing1,984API
and1,893/1,894frontend observations retain their recorded exact scope, not reruns.

These are actual loopback HTTP/Identity/SQL checks of current TypeScript under a
controlled single-realm VM with existing QA-only expired signed tickets. They do
NOT prove real browser login/UI, native cross-tab Web Locks, full8hour lifetime,
physical devices or production. C3.2 browser/role/context/recovery gates remain.
No newly accepted model, training/inference/loading, main merge, Azure/customer DB.

C5 genuine v1metadata acceptance is separately published on Mini a789f1a; no old
verifier/record/pin or model qualification changed. Next Sol High: actual protected
host/browser and exact-lock build/runtime/security integration; Sol Medium for
settled visual work. Continue the existing enterprise program, not a new audit.
