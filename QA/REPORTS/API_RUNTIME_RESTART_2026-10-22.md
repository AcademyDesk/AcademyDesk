# Approved one-time restart and runtime admission — 22 October 2026

## Scope

Explicit owner approval: restart Docker once, preserve all containers/volumes/
images, restore the two existing PENTA services, then attempt the guarded API
runtime retest. No pruning, model activation, further restart, Azure, Git write,
main/Mini modification or production database access. Existing E9 program only.

Worktree `D:\AcademyDesk-codex-p0`, branch `codex/penta-search`, HEAD
`b1f8cb6f4209da7fbd14f8089fcb6792438684c4`.

## Restart and restoration — PASS

New bounded QA helper `QA/tools/api-runtime-restart.cjs` requires the explicit
approval flag and refuses if the running-service list differs from the exact two
expected services. It snapshots container IDs, volume names, image IDs and current
service IDs/images before the single restart invocation. No environment values
or credentials are recorded. Uses `docker desktop restart --timeout 180` once,
bounded daemon recovery and service-health waits; never retries the restart.

Run `c62fae59e8cb438cb0ccee3dbdaaa927`, restart exit0, restartInvocations1:

- Original container IDs/images restored for `pentaaimodels-api-1` and
  `pentaaimodels-inference-1`. Both running; inference healthy.
- Exact before/after container, volume and image inventories match.
- Final29containers/41volumes; no other running services, pruning or removals.
- Restored free memory1.998GiB; later check2.03GiB. Memory was briefly3.76GiB during
  restart, but services had not recovered then; no runtime admitted on that value.

Receipt: `QA/EVIDENCE/api-runtime-restart-c62fae59e8cb438cb0ccee3dbdaaa927/result.json`.
SHA256 `6FC3F92173A1F09A26CFE575933BA83C22076D3DF847C02F75A1692E68070300`.
The helper does not inspect/delete/recreate service data, networks or volumes;
restoration uses the same existing container identities.

## API runtime retest — BLOCKED BEFORE START

Exact invocation:

`node QA/tools/api-image-runtime.cjs 0a5f8d2c708a40a994dd97e9f6d12425`

Exit1, `3GiB guard: runtime not started.` Log:
`.build-check/session-resume/api-image-runtime-after-restart.log`.
No candidate runtime container created, no HTTP checks executed in this attempt,
no SQL/inference calls. The accepted current API image build and63file artifact
inspection remain retained; neither is rerun or replaced by stale images.
The earlier nine HTTP checks reached before a helper failure remain NOT ACCEPTED.

Do not restart again or lower the3GiB guard. A new explicit question asks whether
the two services may be temporarily stopped for this isolated API test and then
restored immediately; no such stop is authorized/performed at this checkpoint.
Without that approval, wait for sufficient available memory or user-released RAM.

## Validation and preserved state

19combined existing artifact/build/runtime-helper tests PASS; log
`.build-check/session-resume/api-image-post-restart-tests.log`. Restart helper
Node syntax PASS. No new application/controller/schema/Dockerfile/package edits.
Staging empty; main HEAD `f7af512f884ca5fe8ac3ddb287c852492592e709`/tracked tree
clean; Mini HEAD `2f8806967709a1a7ec29e5176e7c31968adbf6aa` unchanged. QA/EVIDENCE
remains local/untracked, .build-check ignored. No commit/push/deploy.

Next route: Sol High for exact-image runtime and consequential integration/
security gates. Independent Mini review/qualification and all11 broad enterprise
packages remain OPEN. This is successful restoration, not production readiness
or completion of runtime/SQL/security/device acceptance.
