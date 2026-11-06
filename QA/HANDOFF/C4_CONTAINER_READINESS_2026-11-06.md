# Cloud C4 container/dependency packet — 6 November 2026

Status: SOURCE REPAIR / AVAILABLE VALIDATION COMPLETE. Remote npm audit, image build/runtime and current CI remain blocked. Not production-qualified. Base `a4ce05bee7040a836899c5077433db41bcc74e39` includes C3.2;
shared host still verified at `9dc2ccda5c6aca18d1cc33ec16976df827574533`.

Exact owned paths:

- `apps/web/Dockerfile`
- `apps/web/nginx.static.conf`
- `QA/tools/static-segment-export.test.cjs`
- `apps/web/scripts/prepare-static-segments.mjs`
- `apps/web/package.json` (postbuild script only; locked dependencies unchanged)
- `QA/tools/check-web-container.sh`
- `QA/tools/static-web-runtime.cjs` (existing read-only export/HTTP verifier)
- `QA/HANDOFF/C4_CONTAINER_READINESS_2026-11-06.md`
- `AI_HANDOFF.md` (additive task pointer only; integrator reconciles)

Restore/review existing static-export container source from preservation6887e6e;
verify actual output/routing prerequisites, inspect dependencies and CI read-only.
No parallel backend/API/SQL/migration changes, package/lock upgrades, new package
or image downloads, environment settings, model actions, main merge or Azure.
Only the managed local Docker socket may be inspected; preserve all existing
containers, volumes and images. Missing build images/runtime are prerequisites,
not permission to pull or to substitute a different artifact as production proof.

Validation: installed exact-lock output, meaningful route/config/build regressions,
read-only npm/NuGet audit, real isolated runtime checks only if original cached
capabilities support them. Keep all genuine evidence gaps and test failures.

## Returned source

Implementation `3cf250e3f0769c71d6910ce0ebd2b508ec6511e0` on
`codex/cloud-c4-container-readiness-20261106`. Seven source/tool files changed;
no backend/API/SQL/migration/app-UI source, workflow, dependency version or lockfile
change. Base includes reviewed cloud C3.2 `34f3b46` and handoff `a4ce05b`.
Desktop should review/integrate C3.2 first, then this bounded C4 implementation;
shared handoff documentation is additive and should be reconciled separately.

The existing Dockerfile expected `.next/standalone/server.js` despite Next's
`output: "export"`. Restore the preserved static Nginx path and copy `out/`.
Use webpack, matching the existing verified CI/Azure build invocation. This is
source repair, not an actual image-build pass or a production base-image approval.

A second defect appeared while reviewing preservation6887e6e: its old Nginx
rewrite assumes nested segment payloads. Current Next16.4.3 already emits flat
names, e.g. `/penta/onboarding/__next.penta.onboarding.__PAGE__.txt`; the old rewrite
would send these valid paths to nonexistent nested files. The restored config
now serves TXT directly with404on missing data and retains route.html resolution
without an index.html catch-all.

Restore the existing postbuild hook and preflight all exported segment paths.
Current flat layout is validated without a rewrite; preserved nested layouts get
byte-identical aliases. Root/deeper routes, mixed formats, conflicts, directory/
symlink escapes and changed layouts are covered. No original payload is replaced;
unsupported formats fail for review before writing pending aliases.

`QA/tools/check-web-container.sh` returns an executable handoff for an already-built
local image. It refuses missing images before resource creation, uses `--pull=never`,
an immutable image ID, one owned internal network, loopback-only ephemeral port,
non-root/read-only constrained runtime and ownership/image-checked cleanup. It
never builds/pulls, mounts a customer dataset or contacts an API/model. The
restored read-only HTTP verifier compares every current exported file byte for
byte, checks MIME/route/TXT404behavior, and includes all current flat navigation
payloads. Bash/Node syntax passed; **real image execution remains NOT RUN**. The
non-root runtime is a smoke-launch setting, not proof that the historical image's
default user/security posture is release-qualified.

## Actual validation and evidence boundary

| Check | Actual result |
| --- | --- |
| Next16.4.3 exact-installed-lock build | PASS, webpack;89static generation entries; automatic postbuild87segments/0created/87unchanged |
| Export inventory | 570files,88HTML,87flat navigation payloads; no nested payloads in this build |
| Segment regression | **14PASS**, including all nine preserved cases and five current/mixed/escape cases |
| TypeScript / ESLint | PASS / exit0,0errors and63existing warnings |
| Full frontend runner | **1894tests:1891PASS,3FAIL,0skipped/cancelled**,68files |
| Chromium/React/DOM | **24groups PASS**, intercepted synthetic APIs; no live business/model calls |
| Current API Release publish | PASS using `--no-restore /p:UseAppHost=false`, existing SDK10.0.403/runtime10.0.14; no database/Identity acceptance or container claim |
| npm manifest/installed lock consistency | 371installed package versions all match;67uninstalled entries are optional platform packages; root dependencies/devDependencies match lock |
| Package-lock | Byte-exact unchanged from a4ce05b |
| NuGet local graph | Current API/test graphs read with `--no-restore`, no dependency installation |
| NuGet fresh public-feed comparison | **104unique resolved package versions,0matching advisories**, using SDK NuGet.Versioning range semantics; scope below |
| Docker prerequisite / cleanup safety | Four required base images absent; nonexistent-image preflight refuses with exit1; before/after full resource inventories identical; no container created |
| Real Docker builds/Nginx/API image execution | **NOT RUN**: no required cached images and no new download authorization |
| GitHub Actions | Actual feature-branch `gh run list` returned **Forbidden**; current run status/conclusion **UNKNOWN** |
| npm remote vulnerability audit | **NOT EXECUTED**: automatic approval review rejected private dependency metadata disclosure |

The three frontend failures are the same Notice/Announcement/Meeting cases requiring
original `QA/EVIDENCE/logs/phase-2b-communication-roundtrip-sql.log`. No receipt was
fabricated, no failing assertion was removed and no case skipped. The full runner
remains exit1. Fourteen added static-export cases all pass. The real Chromium
runner uses its isolated Node static server and synthetic responses; it is not
Nginx/image/SQL/physical-device acceptance.

NuGet's official `https://api.nuget.org/v3/index.json` advertised
`https://api.nuget.org/v3/vulnerabilities/index.json`. Only public index/base/update
GETs were sent; **no private package names, versions or graph were transmitted**.
Base update time2026-11-04T11:49:31.6454276Z, incremental update
2026-11-05T17:49:40.1221139Z;644package advisory entries. Raw/decoded feed bytes and
an outside-repository comparator are retained. It uses the installed SDK's
NuGet.Versioning.dll to evaluate exact NuGet ranges against locally restored API/
test package graphs. Result0findings is limited to that feed and graph. This is
not a fresh restore/NU1900–NU1904 gate, framework/runtime/OS image scan or blanket
security approval. Existing graph/publish dependency versions remain unchanged.

Automatic review rejected the npm audit command with the reason that it sends
metadata from the potentially private lockfile to an external registry without
specific authorization for that disclosure. No alternate npm endpoint, indirect
upload or package-data workaround was attempted. The owner must explicitly
approve sending package names/versions to `registry.npmjs.org` before that audit
can run; no package install/update is part of the requested action.

Artifacts in `/workspace/c4-container-20261106`: build/lint/test/browser receipts,
export hashes, installed-lock and local NuGet graph inventories, public advisory
feed bytes/comparator/result, API publish output/log, Docker prerequisite/resource
snapshots and CI denial. Original compressed feed bytes and earlier diagnostic
attempts are retained. Detailed frontend logs remain in `.build-check/frontend-tests`.
Raw evidence/dependency binaries, laptop data and credentials are not committed.

## Prioritized remaining work

1. Review the bounded source repair, then authorize/review supported base-image
   versions and immutable digests before any new image download. Historical
   `node:22-alpine`, `nginx:1.27-alpine` and API `sdk:10.0`/`aspnet:10.0` tags remain
   unqualified; the preserved Nginx1.27 tag is not security-approved for release.
   Do not mistake restoration of that historical source for a current-base review.
   Build actual API/web images, scan their OS/runtime components, run existing API
   smoke and the returned static smoke against matching freshly built output.
   No caches/images were substituted for the missing prerequisites here.
2. Obtain explicit approval for npm's read-only dependency metadata request and
   complete fresh vulnerability auditing. No public internet toggle or policy
   change is needed/requested for the permitted npm registry host.
3. Restore permitted GitHub API access to read actual CI evidence. Static review
   shows verify.yml push triggers only main and codex/release-export-security,
   PRs into main, or explicit workflow calls/dispatch; pushing these cloud feature
   branches does not itself establish an automatic CI run. No workflow dispatch,
   main PR/merge or repository-setting change was performed.
4. Desktop retains current-source live role/session/Manual and protected student/
   teacher SQL/HTTP acceptance and genuine communication receipts. C5's two strict
   Linux provenance cases and all existing enterprise/release gates remain open.
   Mini/Plus/Pro are unqualified; Ultra deferred.90%AI outcome coverage is unproven.

Environment configuration, private access/network policy, main, Azure and all
model acquisition/loading/training/inference/activation remain unchanged. Next
route: Sol High for integration/security and source-bound acceptance.
