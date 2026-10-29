# Package advisory successor — 24 October 2026

Latest same-day successor: Zod4.6.6patch APPLIED/qualified in development,
fresh working-lock audit `131aecb8db284cdc9517fa5c5a64908d` zero matches. See
[applied candidate](ZOD_SECURITY_CANDIDATE_2026-10-24.md). The oneHIGH observation
below is the preserved prepatch finding; the wider security gate remains OPEN.

Read-only public metadata refresh after the previous report's stated upstream
blocker changed. No package, manifest, lock, application, image or model changes.
Existing checks were not suppressed or widened. No deliberate memory-exhaustion
payload or customer data was used.

## Current result: security gate remains OPEN

Existing scanner `web-package-advisories.cjs` completed against the unchanged
current lock and source-pinned new export `27295f0db57a4656862cbf52186ee9eb`.
Receipt `08f1b97dc13b4ac9a1960672e1ee756b`:444lock entries/418package names,
oneHIGH match, no credentials/project name/academy data sent, no package downloads
or Docker mutations. Lock SHA256 remains
`201ed4fb20dc7bc412828c4308140ed549c0e8a00851159325c48a17cc93a6fa`.
Retrieved `2026-10-24T13:44:16.673Z`; response SHA256
`bf3822b6a6aba46689df2686a2ff98225b3b1f4f25b07432311cf8af1d5a66a3`.
The scanner completed but retains failure exit semantics for the nonzero finding;
the outer PowerShell wrapper's exit0 does not turn this receipt into a clean audit.

| Advisory | Current authoritative status | Current lock outcome |
| --- | --- | --- |
| GHSA-vfj7-8cjw-p6xm / braces | GitHub withdrew it23October2026 as not practically exploitable; historical range<=3.0.3 and no patch retained | No longer returned as a current matching advisory by the fresh registry response |
| GHSA-mrhx-c7xc-m3w5 / zod | HIGH, reviewed23October2026, affects<4.6.6, patched4.6.6 | Current dev dependency4.6.5 matches; registry advisory1243066; OPEN |

Primary sources inspected24October2026:
- [Withdrawn braces advisory](https://github.com/advisories/GHSA-vfj7-8cjw-p6xm)
- [Current Zod advisory and patched version](https://github.com/advisories/GHSA-mrhx-c7xc-m3w5)

This supersedes prior statements that braces remains the sole active upstream
blocker. Historical receipts/reports remain unchanged; this is not a waiver or
claim the installed braces code changed. Do not describe the overall package
gate as clean merely because that advisory was withdrawn.

## Bounded local applicability observations

Current lock marks `node_modules/zod`4.6.5 dev-only. The declared incoming paths
include eslint-plugin-react-hooks and zod-validation-error, both accepting
`^3.25.0 || ^4.0.0`. A read-only semver check over the exact lock confirms both
incoming dependency/peer ranges accept4.6.6; no manifest/lock change was made.
Proposed isolated manifest delta is only an added `"zod": "4.6.6"` override,
retaining the existing `"source-map-js": "1.2.2"`; it is a reviewed candidate
direction, not an applied or qualified patch. No direct zod/braces imports were found in apps/web/src or
apps/web/scripts. The source Dockerfile runtime stage is nginx/static export,
not a Node server. These observations do not prove absence from every bundled
asset, exploitability, complete image/SBOM/license review or safe build tooling.
Do not infer applicability exemption or production approval from devOnly.

## Exact next remediation packet (High)

1. Prepare isolated candidate from current worktree and accepted lock, preserve
   current source/dependencies and all legitimate dirty work. Do not mutate main.
2. Review a narrow exact transitive pin of zod4.6.6 (same4.x major and accepted
   parent ranges); verify official registry provenance/integrity. Do not replace
   source files inside node_modules by hand or invent a lock integrity value.
3. Under a separately authorized dependency acquisition/install window, resolve
   and install only in the candidate on D:, no credential/user npm-config access.
   No acquisition/install or actual patch was performed in this continuation.
4. Validate lock graph/direct manifests, native package hashes, fresh all-lock
   advisories with zero currently matched findings or explicit reviewed blockers,
   typecheck/full lint/ordinary production export and affected regression.
5. Align candidate only after validation, fresh source-pinned export/browser proof
   for the changed dependency artifact, and exact original-service restoration/
   full inventory. Keep previous failed envelopes failed. Review current release
   image/OS/API/permissions/security/privacy/license gates separately.

BUG-SEC-0011 remains OPEN; no added issue/closure or whole enterprise acceptance.
No main/Mini changes, model activation, commit/push/Azure or production data access.

Later owner24October approved the isolated4.6.6download/install/build/test packet.
Candidate now has exact target lock delta and zero fresh advisory matches, but
current app is preserved4.6.5and candidate browser/restoration qualification is
still RUNNING. Follow [candidate report](ZOD_SECURITY_CANDIDATE_2026-10-24.md)
for exact accepted/failed boundaries; this original working-lock receipt is not
relabelled as clean or as an applied fix.
