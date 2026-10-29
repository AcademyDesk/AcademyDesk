# API container preflight — 22 October 2026

## Scope / status

Existing E9/runtime program, `D:\AcademyDesk-codex-p0`, `codex/penta-search`, HEAD
`b1f8cb6f4209da7fbd14f8089fcb6792438684c4`. **API IMAGE GATE: NOT PASSED.**
No application/controller/schema/Dockerfile/package change in this batch. One
production-context safety change: ten explicit `.dockerignore` exclusions for
local secrets/settings, NuGet credential configuration, publish profiles and IDE
state. Required public production settings remain eligible. No file deletion,
main/Mini modification, staging/commit/push or Azure deployment.

The older3October API image is not an image of the current dirty source. The two
locally required SDK/runtime base images are not directly inspectable in the
Docker image store; BuildKit layer cache alone does not prove they are available.

## Bounded context / additive checks

`QA/tools/api-image-build.cjs` prepares only the current API source under a fresh
ignored D: context. Source manifest:482files/11119924bytes. It excludes bin/obj,
uploads, environment files, local settings, private credentials/key material and
local outputs, rejects links/unexpected input types, limits file/context size,
hash-checks copies and checks unchanged original source afterward. It copies the
original Dockerfile verbatim. There is no NuGet cache injection, test suppression,
server startup, SQL access or model call.

The no-value heuristic checks nonempty sensitive JSON configuration and suspicious
inline credentials. It is not a certified secret scanner or whole-history audit.
Initial tooling failures retained: normal C# password-property forwarding was a
false positive; BOM-prefixed JSON required supported parsing. Rules now distinguish
string literals from ordinary forwarding and parser failures expose only path/risk,
never input snippets. No real secret was found or printed by these checks.

Final **11focused checks PASS**: locally inspectable SDK+runtime identities,
missing/malformed/dynamic bases, configuration/inline-secret cases, BOM/error-value
suppression, local-artifact filtering, current inputs and Docker context exclusions.
The latter checks explicit patterns, NOT actual image-layer secret certification.
Node syntax and tracked/new-file whitespace checks PASS. Logs in
`.build-check/session-resume/api-image-*`.

## Stopped build — retained, NOT PASS

Run`b7c9d317ed714d6d9919584ab9f82f01` attempted the original Dockerfile with
`--pull=false --network=none` and a unique local candidate tag. The network option
restricts RUN steps, NOT base-image resolution. Contrary to the intended cache-only
boundary, Docker resolved newer tag digests and fetched/extracted base layers:

- SDK10.0:`sha256:747cbade5bfd918a175ac32f8f1d931edec2c7d0d5397a3de72eaef1ac1f66d2`.
- ASP.NET10.0:`sha256:8af87eba5e7cdafb1514d7388be287365da97c132509e84dfe252ffd552398ca`.

The exact owned Docker/buildx client processes26416/26976 were stopped once this
was observed. BuildKit history confirms its exact context ended Error after26.5s,
not an orphaned running build. It had reached network-disabled restore; no successful
restore/publish/image result is claimed. Recorded exit4294967295, imageIdnull; the
candidate tag does not exist. Base-layer cache was NOT pruned. Existing PENTA
services were not stopped/restarted; no production/custom database was touched.
The download is recorded honestly, not hidden by the `forcePull:false` receipt.

## Corrected admission / no retry

The tool now requires **both original FROM references to resolve to real local
image IDs before creating a context or invoking a build**. BuildKit metadata/layers
are not accepted as substitutes. A separate preflight run
`b46f9aa94d494c6f8af8bfa95ccff5c0` correctly exits1 with
`BLOCKED_LOCAL_BASES`: both SDK and runtime missing; contextCreatedfalse,
buildStartedfalse, downloadedfalse. This is verified refusal, NOT a build pass.

Before a real retry, explicitly approve/provision exact required SDK/runtime
artifacts and required NuGet dependencies in the development environment; review
their identity, local storage location and acquisition scope. No silent fallback
to the stale3October application image. Do not loosen the memory/network gates or
call the failed attempt accepted. Full original Dockerfile/current Linux build,
runtime, SQL/migrations/auth/tenant/device and release acceptance are still required.

## Receipts / preservation

| Receipt/source | SHA256 |
| --- | --- |
| `QA/EVIDENCE/api-image-b7c9d317ed714d6d9919584ab9f82f01/result.json` | `3ABB48B040CC2CFCEECB99381B15BC8C14B18E1616663D24A19EC4B172213127` |
| Same directory, `build.log` | `A5237E058B0AA49FF6E06B8B3EF95A376270F86616B0700DD72125D07F4CB97E` |
| `QA/EVIDENCE/api-image-b46f9aa94d494c6f8af8bfa95ccff5c0/result.json` | `14AA7E455AFBFC1BAC8DB8F82531EDCCC2BF61071221C0EDE78FF209ECF92743` |
| `.dockerignore` | `EB8B1B7F541031F81377F77798675C8852F99740321B50B1D4708F7D76A1922B` |
| `QA/tools/api-image-build.cjs` | `796A703905590B847E4A150D6473C5766390F6C32D5BA9D8D210C3FA5EFB5183` |
| `QA/tools/api-image-build.test.cjs` | `69FA6984BB453B8F2A210A17C8DB20ADEE70CEB643F95D4E98467D5CC098A9BB` |

Final29containers/41volumes, only the original two PENTA services running; no
candidate image/container, network/volume removal or pruning. Main and Mini HEADs
unchanged. QA evidence is intentionally local/untracked, fixtures ignored. Previous
default frontend build/25chat/12CSS-order/lint0/0 and native SQL evidence remain
retained, not rerun for a packaging preflight. No enterprise/model/issue closure;
all11broad packages remain open at their outstanding gates.

Next route: High for artifact acquisition/current image and consequential runtime
gates; Medium for settled documentation. Mini independent review/qualification
remains a separate unresolved dependency, not improved by container tooling.
