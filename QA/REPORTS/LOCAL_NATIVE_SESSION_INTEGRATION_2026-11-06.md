# Local native session integration — 6 November 2026

Status: CLAIMED before edits. Owner local Codex on the existing host feature
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
