# Current API image isolated runtime — 22 October 2026

## Scope / result

Existing E9 program, `D:\AcademyDesk-codex-p0`, `codex/penta-search`, HEAD
`b1f8cb6f4209da7fbd14f8089fcb6792438684c4`. Owner explicitly approved temporarily
stopping only the two original PENTA services, testing if the unchanged3GiB guard
passes, then immediately restoring those exact services. No additional Docker
restart, prune, model change/activation, production DB, Git/main/Mini or Azure write.

**CURRENT API IMAGE ISOLATED NO-DATABASE SMOKE: PASS,9/9.**
This is not authenticated SQL/tenant/financial/device/release acceptance.

## Exact artifact and actual HTTP results

Build `0a5f8d2c708a40a994dd97e9f6d12425` and immutable candidate
`sha256:afc653e56a4231451fd53713bbf6fbf1188db8df4d4bdd382d8b87a2e4e94a70`.
Helper verifies the successful build receipt, exact label/ID and all482 current
API source hashes before running. No stale3October image or rebuild substituted.

Runtime `a1e7f4168fca423aacc03f9055ce6bdf`:

| Actual request | Expected and observed |
| --- | --- |
| Startup `/health` with deliberately unavailable local SQL |503|
| Anonymous `/api/auth/session` |401|
| Anonymous `/api/academies` |401|
| Anonymous scoped payments |401|
| Anonymous private class-media content |401|
| Anonymous scoped PENTA chat health |401|
| Synthetic invalid bearer on auth session |401|
| Production `/openapi/v1.json` |404|
| Repeat unavailable-database `/health` |503 in351ms, under unchanged7s bound|

Requests used real HTTP inside the candidate container, not framework test-host
substitutes or body-code matching. Process remained running/not OOM. Networknone,
read-only root,512MiB/1CPU/pids128/capabilities dropped/no-new-privileges, bounded
tmpfs only and zero persistent mounts. Migrations/bootstrap disabled. Explicit
unreachable loopback SQL and no network prevent customer/development database or
inference access. No published network ports. This test uses the production
image's existing user; it does not qualify non-root production operation.

Actual .NET/ASP.NET shared runtimes10.0.13. Shipped PKCS DLL ProductVersion
`10.0.13+4099f83977f4d29604a9aeb971fb0a7bc1d5f699`, SHA256
`E562A8977D9DD59E4329F1AD0941C99D10DC27C069C15F8B10BE647103C144E4`.
An equal patched framework asset legitimately has no separate app.deps PKCS entry.
Binary metadata is not a loaded-assembly test; loadedPkcsAcceptancefalse retained.

Exact owned runtime container removed after all checks, identity/run label checked;
no images/volumes/other containers removed. Receipt:
`QA/EVIDENCE/api-image-runtime-a1e7f4168fca423aacc03f9055ce6bdf/result.json`.
Earlier assertion failure and memory refusals remain retained, not relabelled PASS.

## Temporary service window and restoration

Wrapper `QA/tools/api-runtime-window.cjs` snapshots full container/volume/image
inventories and exact original service IDs/images, requires the explicit approval
flag, refuses unexpected running services and restores from finally even when
the test fails. No compose recreation, model edit, restart or prune.

Window `e48609bccb50425aa90e868c28b11b0d`: both services stopped, memory naturally
reached4052996096bytes (~3.775GiB), unchanged admission passed. After smoke and
exact test-container cleanup, restoration began immediately in inference→API
order using existing IDs. No extra work runs during the stopped window.

Restoration/inventory final result PASS, wrapper exit0, runtimeExit0,
restorationPassedtrue/inventoryPreservedtrue. Both original services running,
inference healthy, exact same IDs/images; all container/volume/image inventories
match before/after,29containers/41volumes. No second Docker restart.
Final result recorded in
`QA/EVIDENCE/api-runtime-window-e48609bccb50425aa90e868c28b11b0d/result.json`.
Do not infer restoration success from the smoke receipt alone; verify its final
restorationPassed/inventoryPreserved fields and actual service health.

| Receipt | SHA256 |
| --- | --- |
| Runtimea1e7f416 result.json | `B50B1FA46E969BBA8B99A41E3151D946F711A8F508158FE22BA390312D6306B4` |
| Windowe48609bc result.json | `87B6150EA961A1F88630CA41FD1D4E52F0867B5D60CA9F0AE90D228BB3C0A893` |

## Validation / remaining gates

19combined helper tests PASS, restart/window helper syntax PASS. Log:
`.build-check/session-resume/api-image-window-tests.log`. No application code,
controller/schema, Dockerfile/package or model changes. Existing build,63file
image-artifact, frontend and native SQL evidence retained, not a new audit program.
Staging empty; main HEAD `f7af512f884ca5fe8ac3ddb287c852492592e709`/tracked tree
clean, Mini HEAD `2f8806967709a1a7ec29e5176e7c31968adbf6aa` unchanged. Evidence
local/untracked and .build-check ignored. No commit/push/Azure action.

Next: exact-candidate disposable SQL/migrations and authenticated role/tenant/
finance integration, full frontend/API critical E2E and remaining enterprise/
security/device/restore/rollback gates. Independent Mini review/qualification
remains unresolved; all11 broad packages remain OPEN. No production readiness,
90% AI completion, issue closure, Git publication or Azure approval is claimed.
Sol High for consequential runtime/integration/security; Medium for settled docs/UI.
