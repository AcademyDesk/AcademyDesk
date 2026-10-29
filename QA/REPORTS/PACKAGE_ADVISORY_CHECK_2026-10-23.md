# Exact-candidate package advisory check — 23 October 2026

**API NuGet slice: completed, zero matching affected package versions.
Frontend lock slice: completed, FAIL with14advisory/version matches. Release NOT APPROVED.**

Worktree `D:\AcademyDesk-codex-p0`, branch `codex/penta-search`, HEAD
`b1f8cb6f4209da7fbd14f8089fcb6792438684c4`; legitimate dirty work preserved.
No application/package/lockfile change, install, suppression, scanner download,
Docker mutation, credentials, Mini/model, production database, Git push or Azure.
Public package/version metadata only; all evidence on D: in local QA/EVIDENCE.

## API:31published NuGet packages

Run `f1acde9cf84f4523bd7fe6a61d89fa28`, retrieved2026-10-23T03:49:18Z:
build `0a5f8d2c708a40a994dd97e9f6d12425`, current published component inventory
`bcce82c8108942b2a0ddb7664d3063e1`. Input graph and dependency-manifest hash
matched; current14native receipts/API+web source/export pins revalidated.

`Check-ApiPackageAdvisories.ps1` uses the installed NuGet.Versioning7.9.0.0
assembly for actual NuGet range semantics, not a custom semver approximation.
The official HTTPS service index identified VulnerabilityInfo/6.7.0; both listed
pages downloaded and retained with SHA-256/HTTP dates. Base updated19October,
update22October; all package records/ranges validated, additive matches deduplicated.
No matching affected version among31packages. This excludes shared .NET framework,
base OS, frontend and build tooling; it is not absence of all vulnerabilities.
The earlier PKCS remediation remains its own loaded-assembly/runtime evidence.

Repeat: `pwsh -NoProfile -File QA/tools/Check-ApiPackageAdvisories.ps1`.
Offline self-test: same command plus `-SelfTest`; no network in self-test.
Negative controls cover range edges, four-part/prerelease versions, duplicate/
empty index, unsafe hosts/paths/query and invalid severity/range/URL.
Bodies/time bounded, TLS validation unchanged, redirects/cookies/default
credentials disabled. Unknown/missing/malformed feeds fail, not a clean scan.

Incomplete attempts `f48cc81b577c45aa9d4b0dbd939d1184` and
`4de85f99aae4479fa1c11a91939fa524` retained. The reader needed ordinary compressed
HTTP response support and the official index-discovered `/v3-vulnerabilities/`
page prefix in addition to `/v3/vulnerabilities/`. Neither incomplete run is PASS.
No arbitrary host/path fallback, skipped page, TLS bypass or advisory suppression.

## Frontend:438lock entries /418public package names

Run `77a1824c06844b2d8a7629558d35b9ad`,23October03:52:39Z snapshot:
current accepted export `bad444b6c61e45f680ed62f76d35dcf4`/628file hashes verified
before and after. Lock SHA-256:
`a8bc96339893d0f6baa679a54ee018a3fc58eed6ec17a97a8930c9ead889bc08`.
Bulk response SHA-256:
`df230ccb8846812a3d53f33a1bd0b6b5c9afc16c0d9f576bd9eceb01179fd02a`.

`web-package-advisories.cjs` sends only public registry package names and versions
to npm's bulk advisory endpoint. Private root name, source, credentials/.npmrc and
academy/user data are not read/sent. Includes dev and optional-platform packages,
so a matched lock entry is not proof it is executed in production. Uses existing
semver with npm's includePrerelease/loose range options. No `npm audit fix`,
installation, quick-audit fallback, meta-vulnerability propagation or package edits.

**14matches:3critical,5high,5moderate,1low**, over four package versions:

| Package/version | Matching advisories | Current disposition |
| --- | --- | --- |
| next16.3.5 |11:3critical,2high,5moderate,1low | OPEN; Next publisher lists16.4.1 fixes for the newest matched critical/cache/image and Server Action findings. Controlled compatible upgrade/rebuild and regression needed; no automatic exception. |
| sharp0.35.4 |1high | OPEN; publisher lists0.35.5 patched. Determine actual optional/build/runtime paths; no blanket non-applicability claim. |
| source-map-js1.2.1 |1high | OPEN;1.2.2 listed patched. Multiple PostCSS/Tailwind dependency paths, not discarded as solely dev. |
| braces3.0.3 |1high | OPEN; current advisory lists no patched version. Dev micromatch path; requires upstream/removal or separately reviewed constrained-exposure disposition, not an invented3.0.4 fix or silent suppression. |

Full14IDs/ranges/public links retained in result.json. Repeat:
`node QA/tools/web-package-advisories.cjs`. Nonzero exit on incomplete scan OR
any matches; `completed=true` does not mean security PASS. Five portable tests
cover payload privacy/nested versions, public provenance, ranges/deduplication,
invalid/unsolicited records and strict bounded JSON. First two incomplete
attempts `62301a8c315049c28b6fc16953af7bc3` and
`dcaf42c252fb4cae81b73222412326f5` retained:official bulk response omitted
Content-Type. The reader now accepts an absent header only with strict bounded
UTF-8/JSON/object and advisory schema; HTML, empty/invalid bodies and declared
wrong media type still fail. It did not treat HTTP200 or missing headers as clean.

## Source-grounded exposure triage — NOT a waiver

`next.config.ts` has `output: "export"`; web Docker runtime is nginx copying only
out/, and current Azure workflow builds/uploads apps/web/out to Static Web Apps.
These source configurations do not establish a deployed production revision.
Source search found no `next/og`/ImageResponse, `use server`, `use cache`,
cacheComponents or dangerouslyAllowSVG declaration. Publisher conditions include
Node ImageResponse SVG input, live Image Optimization and cache/Server Action
server endpoints. Thus the checked static architecture differs from those server
surfaces; **this is an engineering inference, not exploitability or deployed
non-applicability proof**. Development/build processes still use affected packages.
Do not close this issue, waive critical matches or infer all client/build paths safe.

Tracked as [BUG-SEC-0011](../ISSUES/BUG-SEC-0011.md), P1 package finding,
release-blocking pending compatible remediation or independently reviewed
applicability/disposition. No destructive exploit tests or customer requests run.

## Existing rollback evidence reviewed, not rerun

Accepted47-check SQL restore,63-check joint SQL/encrypted-key recovery and31-check
reconstructed historical-schema upgrade remain within their recorded scopes.
They are not proof an old release binary works against the new schema. Original
historical release binary provenance remains unavailable; no down migration,
recovery replay, customer restore or production RTO/RPO claim added here. Continue
the existing compatible rollback gate, not a fresh replacement test plan.

## Primary/public specifications and publisher records

Final engineering verification:86/86focused Node QA/issue tests and offline
NuGet self-test PASS. Seven current/existing documents1256local links verified
with CommonMark angle-wrapped absolute paths handled; initial simplistic link
parser rejected one valid existing absolute link, not a missing report. Repository
whitespace/syntax PASS; staging empty. Issue inventory120IDs/5P0/101P1/14P2,
no missing/orphan IDs. Current14native functional receipts/API+web/export pins
unchanged. Docker inventories still29containers/41volumes/same networks/images;
two original PENTA services healthy. These tool passes do not convert the
frontend's14advisory matches into a security pass. No app/main/Mini/package edits.

- [NuGet vulnerability API](https://learn.microsoft.com/en-us/nuget/api/vulnerability-info)
- [npm bulk advisory protocol](https://docs.npmjs.com/cli/v8/commands/npm-audit/)
- [Next Node ImageResponse advisory](https://github.com/vercel/next.js/security/advisories/GHSA-vcvr-r3jv-pc5j)
- [Next image optimization advisory](https://github.com/vercel/next.js/security/advisories/GHSA-2464-fhr4-w9xw)
- [Next use-cache advisory](https://github.com/vercel/next.js/security/advisories/GHSA-673q-mjm6-r5xr)
- [Next Server Actions advisory](https://github.com/vercel/next.js/security/advisories/GHSA-p3m5-59mp-jv5r)
- [sharp publisher advisory](https://github.com/lovell/sharp/security/advisories/GHSA-wq5f-xc86-pv6w)
- [source-map-js advisory and upstream references](https://github.com/advisories/GHSA-68fv-2mgg-jv7q)
- [braces advisory/no patched version](https://github.com/advisories/GHSA-vfj7-8cjw-p6xm)

Next Sol High: controlled frontend dependency remediation/applicability and fresh
build/source/selected browser regression, preserving historical successful
functional evidence. Medium for settled visual/docs. No deployment or approval
of remaining11broad packages, full AI/Mini/device/privacy/security/operations gates.
