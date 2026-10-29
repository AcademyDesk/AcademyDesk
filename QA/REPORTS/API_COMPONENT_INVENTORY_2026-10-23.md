# Published API component inventory — 23 October 2026

Status: **Offline application-component inventory complete; full SBOM,
licence, vulnerability and production approval gates remain OPEN.**

Run `bcce82c8108942b2a0ddb7664d3063e1` reads the already accepted artifact
inspection `d98dcbc285f94ec39dbc72f44b9e8e43`, API build
`0a5f8d2c708a40a994dd97e9f6d12425` and immutable image
`sha256:afc653e56a4231451fd53713bbf6fbf1188db8df4d4bdd382d8b87a2e4e94a70`.

- [x] All63published files rehashed against the accepted inspection receipt.
- [x] Current API source pins match the accepted build.
- [x]32declared runtime components:one application project and31NuGet packages.
- [x] Every dependency resolves to an exact declared component/version; all
  components are reachable from AcademyDesk.Api, no unresolved/dropped graph nodes.
- [x]55declared runtime/resource/RID assets mapped to shipped file sizes and
  SHA-256 hashes. Declared NuGet SHA-512 integrity recorded separately, explicitly
  NOT a claim of verifying package archives or of loading every RID-specific asset.
- [x] No network, container mutation, package acquisition, application edit or
  credentials read. Output remains in local untracked QA/EVIDENCE.
- [x] Three portable tests cover valid graph/assets, missing/duplicate files,
  unresolved dependencies, malformed hashes, extra targets, unreachable nodes,
  invalid locales and path traversal/Windows drive/UNC escapes.

Repeatable command: `node QA/tools/api-component-inventory.cjs`.

Output schema `academy-published-component-inventory/1` is an internal evidence
format, **not a claimed SPDX/CycloneDX-conformant or whole-image SBOM**. Base OS,
shared .NET/ASP.NET framework, build SDK/tooling and frontend dependencies are
outside this manifest. `licenseReviewComplete=false`,
`vulnerabilityScanComplete=false`, `wholeImageSbomComplete=false` and
`productionApproved=false` are explicit. A resolved dependency/version or a hash
does not demonstrate safe licensing or absence of vulnerabilities.

Continue the existing exact-release security/operations gate: acquire complete
runtime/base/frontend component coverage, perform current advisory and licence
reviews, evaluate findings and remediation, then retain independently reviewed
release evidence. Do not close that gate or upgrade packages automatically from
this inventory. No new testing strategy, Git push, Mini change or Azure operation.
