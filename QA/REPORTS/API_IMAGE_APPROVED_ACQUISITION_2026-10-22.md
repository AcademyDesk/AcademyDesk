# Approved API acquisition checkpoint — 22 October 2026

## Scope and result

Continue E9 from the existing API preflight in `D:\AcademyDesk-codex-p0`,
branch `codex/penta-search`, HEAD `b1f8cb6f4209da7fbd14f8089fcb6792438684c4`.
Owner's continuation approved the explicit question about downloading the pinned
.NET bases and required project NuGet packages after checking D: storage. This
does not authorize package upgrades, models, restart/prune, production or Azure.

**CURRENT API IMAGE BUILD: PASS. CORRECTED RUNTIME RETEST: PENDING.**

## Actual acquisition, including retained failure

Docker settings resolve to `D:\DockerDesktopData\DockerDesktopWSL`. Physical
`disk\docker_data.vhdx` and WSL registry base path were checked; D: had about113GiB
free. Linux `/var/lib/docker` alone would not establish the Windows storage drive.

Run `199ff7f57c554faca313d34e2f5f37e8` downloaded the approved public SDK manifest:

`mcr.microsoft.com/dotnet/sdk@sha256:747cbade5bfd918a175ac32f8f1d931edec2c7d0d5397a3de72eaef1ac1f66d2`

Its SDK log confirms pull success; image inspection confirms Linux/amd64 and that
exact RepoDigest. The helper then stopped because it compared a tag-plus-digest
reference with Docker's canonical repository-plus-digest representation. No
success result.json exists for this partial run. No original10.0 tag was created;
the runtime pull was not reached. Preserve `QA/EVIDENCE/api-base-acquisition-199ff7f57c554faca313d34e2f5f37e8/sdk.log`.

`api-image-acquire.cjs` now removes the mutable tag for canonical digest identity.
A regression checks that representation. It still refuses conflicting existing
tags, requires exact platform/digest, uses only the two approved public manifests,
and has unchanged3GiB RAM/10GiB disk bounds.

Corrected invocation `node QA/tools/api-image-acquire.cjs --approved-22oct` exited1
with `3GiB guard: acquisition not started.` Available physical RAM was2.62GiB.
Log: `.build-check/session-resume/api-base-acquisition-final.log`. This refusal
occurs before a pull/evidence directory. Neither original SDK10.0 nor ASP.NET10.0
tag exists. No new build/runtime was started and no service was restarted to gain
memory. The SDK digest download is retained locally, not pruned or disguised.

Memory then recovered naturally to4.29GiB. Corrected acquisition run
`991c3b89a1404525ac645d41b06e30cd` passed both immutable digest/platform checks,
created only the previously absent10.0 tags and recorded the physical D: VHD.
SDK1303848790bytes; ASP.NET340001499bytes. Runtime digest:
`sha256:8af87eba5e7cdafb1514d7388be287365da97c132509e84dfe252ffd552398ca`.
Receipt: `QA/EVIDENCE/api-base-acquisition-991c3b89a1404525ac645d41b06e30cd/result.json`.

## Current Linux image build — PASS

Run `0a5f8d2c708a40a994dd97e9f6d12425`, explicit approved restore networking,
original Dockerfile and482source files/11119924bytes: exit0, not timed out.
Restore9.18s; publish step30.6s. Original source hashes unchanged. Candidate:

`sha256:afc653e56a4231451fd53713bbf6fbf1188db8df4d4bdd382d8b87a2e4e94a70`

Unique local tag `academydesk-local-candidate:qa-0a5f8d2c708a40a994dd97e9f6d12425`.
Build receipt/log under `QA/EVIDENCE/api-image-0a5f8d2c708a40a994dd97e9f6d12425/`.
No application/package/Dockerfile edits, push or deployment. Earlier unsuccessful
attempts remain historical failures; this successor does not relabel them.

## Initial runtime assertion failure and precise diagnosis

Run `9e50f56a4e1d40c2a5cb6061e8484a84` reached all nine HTTP assertions and container
isolation checks, then failed the helper's exact app.deps PKCS-library assertion.
It exits1 and is NOT accepted runtime evidence; the first helper failed before
writing result.json. The log is retained in `.build-check/session-resume/api-image-runtime.log`.
Exact owned container removed; no other containers/images/volumes removed.

Read-only, never-started image inspection `bf4444301c51470383d29718bbf63c4f` copied
only app dependency/runtime manifests and the shared PKCS DLL to local QA evidence.
It confirms .NET and ASP.NET10.0.13, app runtimeconfig requests net10 frameworks,
and the shipped shared-framework PKCS ProductVersion is
`10.0.13+4099f83977f4d29604a9aeb971fb0a7bc1d5f699` (FileVersion10.0.1326.50504).
The equal patched framework asset legitimately has no separate app.deps library.
Inspection container ownership checked and removed without starting it.

The runtime helper now inspects every shipped PKCS binary's actual ProductVersion,
requiring net10.0.13 or newer net10 patch; app.deps presence alone is neither
required nor sufficient. A new regression rejects older/wrong-family/file-version
representations. It also persists completed checks and failure results before
returning, including verified cleanup. **15/15 focused tests and syntax PASS.**
This binary metadata check is still not loaded-assembly runtime acceptance; the
existing loaded-assembly test and broader security gate are unchanged.

The corrected invocation then refused at the unchanged3GiB memory guard before
creating a runtime container. Log:
`.build-check/session-resume/api-image-runtime-corrected.log`. Do not repeatedly
rerun it while RAM remains below admission. No app/runtime pass is claimed from
the nine checks reached by the failed predecessor.

## Prepared tooling versus proven runtime

### Published-image payload successor

Current image's actual `/app` payload, not just the source context, was copied
from an exact-owned never-started container to ignored D: storage and screened.
The helper verifies current source/image label, entrypoint/work directory and
nonempty credential-like image environment values; no values are printed.
Inspection containers have no network and read-only roots and are never started.

Initial run `9ff0a6e825d64d349b18b0cf4209635a` correctly retained a failure receipt
and removed its exact owned container, but its configuration heuristic misread
token-named dependency package metadata as a credential. No real secret was found.
The correction exempts only validated package-version dependency maps in the exact
app.deps manifest, not configuration credentials or arbitrary strings; malformed
versions and extra credential properties still fail. Case-insensitive private/
development paths, unsafe outputs, malformed JSON and private-key text are checked.
Binary contents are hash-pinned, not claimed scanned for all embedded secrets.

Corrected run `d98dcbc285f94ec39dbc72f44b9e8e43` **PASS**:
63published files/93085157bytes, no flagged local/private/configuration artifacts,
source unchanged, exact owned never-started container removed. Receipt/manifest:
`QA/EVIDENCE/api-image-artifacts-d98dcbc285f94ec39dbc72f44b9e8e43/result.json`.
Physical extraction remains ignored under `.build-check/api-artifacts/` onD:.
**19combined focused tests PASS** (15retained+4new), including credential-value
suppression and the dependency-metadata distinction. This is a bounded heuristic
artifact check, not a whole-image secret certification, CVE/SBOM/license review,
non-root validation or a substitute for runtime/SQL/security acceptance.

Latest free RAM2.69GiB remained below3GiB; corrected runtime was not retried blindly.
Explicit one-time Docker restart/restoration approval requested; no restart performed
without that new approval. All original containers/volumes/images remain preserved.

- Build helper keeps network disabled by default. Only the explicit
  `--approved-project-restore` flag enables normal networking for restore; other
  flags are rejected. Original Dockerfile/package versions/source are unchanged.
- New candidate smoke helper requires a successful source-matched image receipt,
  unique owned container, no network, read-only root, bounded tmpfs/memory/CPU,
  no persistent mounts, migration/bootstrap disabled and unreachable local SQL.
- Nine planned actual HTTP checks cover unhealthy database health, anonymous
  session/academy/payment/private-media/PENTA denial, invalid bearer denial,
  Production OpenAPI absence and repeat health timing. Controller routes were
  checked against source. The helper removes only its exact owned container.
- Runtime versions and shipped PKCS ProductVersion are checked, with captured
  dependency metadata. Neither is proof of the loaded assembly. No authenticated SQL/tenant/device/model or
  non-root production qualification is inferred.

Corrected smoke has **not yet passed**. Prepared tooling is not acceptance evidence.
Current source secret heuristics/tool tests: **15/15 PASS**; all three Node helper
syntax checks PASS. Prior frontend build/browser/SQL receipts are retained, not
rerun or superseded by this packaging checkpoint.

## Next exact sequence

Acquisition and build are now complete; do not rerun them unnecessarily. Once
available memory meets the unchanged3GiB guard:

1. Run `node QA/tools/api-image-runtime.cjs 0a5f8d2c708a40a994dd97e9f6d12425`.
2. Preserve any failures; continue original authenticated SQL/security/device and
   release gates rather than closing enterprise testing from this smoke test.

No application/controller/schema/Dockerfile/package versions changed in this
successor. No staging/commit/push/main/Mini/Azure write, credentials, production
database, model action or unrelated container removal. Sol High for consequential
build/runtime/security work; Medium for settled documentation. All11 broad
packages and the independent Mini review/qualification remain OPEN.

## Final preservation / receipt integrity

15focused checks PASS, final log `.build-check/session-resume/api-image-final-tests.log`;
three helper syntax checks PASS. Final29containers/41volumes, only original two
PENTA services running. Staging empty; main HEAD
`f7af512f884ca5fe8ac3ddb287c852492592e709` and Mini HEAD
`2f8806967709a1a7ec29e5176e7c31968adbf6aa` unchanged; main tracked tree clean.
QA/EVIDENCE local/untracked; .build-check ignored. No files deleted or image prune.

| Local artifact | SHA256 |
| --- | --- |
| Acquisition991c3b89 result.json | `6A2C1D1B58B62C69005B5FBE597C5DBA5432A722D820D8360DDFA1447D19F1CF` |
| Build0a5f8d2c result.json | `6554E7B5FA90081EEABCB5B338E235E55D58ED5AE6D8E3DDE54BBA44A7CCAA62` |
| Same build.log | `C3A689FF35BA4745CEBABDF3BA0A664C00BC0987052D150244EC68F1F6BEAB84` |
| Inspected shared PKCS DLL | `E562A8977D9DD59E4329F1AD0941C99D10DC27C069C15F8B10BE647103C144E4` |
