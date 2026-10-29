# BUG-SEC-0011 — Frontend lockfile contains known advisory-matched package versions

## Current successor — 24 October 2026

Latest: qualified exact4.6.6candidate is now APPLIED to development only. Fresh
working-lock audit `131aecb8db284cdc9517fa5c5a64908d` completed444entries/
418names with ZERO current matches; both prior nonzero receipts remain historical.
155tool tests,94handlers,216synthetic browser checks and current exact-export
46native HTTP checks/eight journeys PASS. Exact21517dependency files independently
verified, previous physical directory retained, main untouched, original services
healthy/full inventory/witness cleanup/lease PASS. This removes the current
package-version match, not the wider image/SBOM/license/runtime/security/release
gate. Issue remains OPEN. See [applied report](../REPORTS/ZOD_SECURITY_CANDIDATE_2026-10-24.md).

The earlier same-day finding and candidate-only stages below are retained history,
superseded by the applied result above wherever they describe the current lock.

[Fresh status/remediation packet](../REPORTS/PACKAGE_ADVISORY_SUCCESSOR_2026-10-24.md):
braces advisory GHSA-vfj7-8cjw-p6xm was withdrawn upstream23October2026 and is
absent from fresh registry matches. Existing unchanged-lock scanner receipt
`08f1b97dc13b4ac9a1960672e1ee756b` completed444entries/418names with **oneHIGH
Zod4.6.5** match, GHSA-mrhx-c7xc-m3w5 (<4.6.6, patched4.6.6). Nonzero finding
still fails the advisory gate; no suppression, fabricated patch or clean result.
No dependency/application changes, install/download or Docker mutation performed.
Prior braces-only snapshots below are historical, not the current active finding.
Issue remains OPEN for narrow candidate remediation and broader security gates.

Owner-approved isolated4.6.6candidate now qualified:94handlers/full lint/typecheck/
ordinary build+postbuild/627export hashes/216synthetic role-session browser checks/
zero current advisory matches. Exact originals/inventory/owned-witness removal/
lease PASS; two earlier runner-failure envelopes remain failed. Candidate report
[Zod successor](../REPORTS/ZOD_SECURITY_CANDIDATE_2026-10-24.md) contains receipt
hashes and the next native enrollment window. Working app still4.6.5; isolated
candidate acceptance is not an applied patch or issue/security/release closure.

| Field | Value |
| --- | --- |
| Status | OPEN / DEPENDENCY REVIEW REQUIRED |
| Confirmation status | Registry advisory/version matches reproduced; application exploitability NOT confirmed |
| Final verification |24October scoped Zod4.6.6 patch APPLIED/155tool tests/94handlers/216synthetic browser+46native checks/eight journeys PASS; fresh working lock zero matches; full-image/security/release OPEN |
| Severity | Initially3critical/5high/5moderate/1low matches; current lock1high; runtime applicability requires review |
| Priority | P1 |
| Category | SEC |
| Module | WEB / BUILD AND RUNTIME DEPENDENCIES |
| Environment | Local pinned candidate/static export; deployed production state not inferred |
| Discovery test / review ID | SECURITY-PACKAGE-001 |
| Evidence location | Current QA/EVIDENCE/web-package-advisories-131aecb8db284cdc9517fa5c5a64908d/result.json; prior08f1b97dc13b4ac9a1960672e1ee756b/original77a1824c06844b2d8a7629558d35b9ad retained |
| Source | apps/web/package.json; apps/web/package-lock.json |
| Root-cause confidence | HIGH for affected versions; runtime reachability not established |

## Remediation successor — 23 October 2026

New bounded runtime successor: 627 exact nginx files, 84 Flight aliases and 25
synthetic chat cases PASS, exact original services restored/full inventories
preserved. Exact-export safety heuristic found no probable private artifacts or
credential literals across all 627 files; not complete security certification.
118 focused tool tests PASS. This does **not** fix/waive braces, prove absence
from the full image, or qualify the updated production image/Azure deployment.
Issue remains OPEN. [Report](../REPORTS/WEB_EXPORT_RUNTIME_2026-10-23.md).

Final successor:alignment PASS and **both** fresh native retests PASS—Payments
45checks+4journeys(SQL`5cf31ac559164848b493d9f69330067c`,window
`9a06d7e7c0464cb78458a630ca7c84e3`),four-role entry32checks+16journeys(SQL
`cd76de157ec744dfa81d72f045bfffdb`,window`2e0dc820a98e4fab800ca3f08790044c`).
Owned cleanup/exact original services healthy/full inventories preserved.
Current native qualification`9c3525fcd8e54ebe9c965f62b9e6f52d` verifies14receipts,
627export files/217source pins/36PNGhashes.112QA-tool tests+NuGet self-testPASS.
**Issue remains OPEN:**oneHIGH braces/original failed frontend resource envelope/
broader security acceptance. No suppression,waiver,Azure or issue closure.
This supersedes running/blocked alignment statements below;history retained.

Newest:actual physical dependency alignment PASS(helper
`eed810b557c4497f9617d1def91f6445`,window`45a1a3a273e64072993a8381333bad7a`),
all21517files/436238319bytes verified,main untouched,old junction retained.
Fresh candidate native Payments45checks+4browser journeys PASS(SQL
`5cf31ac559164848b493d9f69330067c`,window`9a06d7e7c0464cb78458a630ca7c84e3`),
exact original services restored/full inventoryPASS. Four-role successor running,
not yet accepted. OneHIGH braces/original failed resource envelope/broader
security/release gates remain OPEN. Older blocked alignment statement below is history.

Worktree alignment implementation now prepared with two focused guard tests;
actual window`ca22ed9ba634410bbe283a0895325731` blocked at1.397GiB before any
copy/swap. Exact originals restored healthy/inventoryPASS. Environment and native
retest gates remain OPEN;no restart/guard weakening/main install. See report.

- Narrow worktree package/lock update applied:Next/eslint-config-next16.4.1,
  sharp0.35.5 and source-map-js1.2.2. React19.2.8 and all application logic
  unchanged. Applied217source pins equal the built isolated candidate
  `293e5beba439453794e6c1e68adc2b71`; generated lockSHA256
  `201ed4fb20dc7bc412828c4308140ed549c0e8a00851159325c48a17cc93a6fa`.
- Fresh isolated npm ci,Next typegen,TypeScript,full lint,normal Turbopack
  production build/postbuild PASS;87static pages/627hashed export files.
  Login28/session80/Admin24/Teacher36/Student48=216synthetic browser checks PASS.
  These do not prove native Identity/SQL/device or live AI operation.
- Applied-lock audit`ff538b776e8b4d9baaab499d8d1a1170`:444entries/418names,
  completedtrue,oneHIGH `braces3.0.3` match,exit1. All13other original matches,
  including threecritical-rated, absent from this exact lock. **Not issue closure.**
- Frontend window`a1e44706bc36496bb19f4c2b0d1f5f25` validationComplete=true,
  exact original services restored healthy,but overallFAIL:one default bridge
  network ID changed.29containers/41volumes/10images and actual PENTA network
  unchanged. Cause unproven; no restart was invoked. Failed receipt retained.
- Candidate native Payments retest window`06ad2581c0584c0d8f1cd1c6e4704c5b`
  BLOCKED by unchanged3GiB RAM guard; no SQL/test child started. Exact services
  restored,full inventories preserved. Native Payments and four-role candidate
  retests remain OPEN. Do not re-label original build receipts as new-build proof.
- Main dependency junction unchanged and still contains old dependencies. The
  patched physical candidate is the validated environment; ordinary worktree
  dev/build through that junction is NOT upgraded. Environment alignment remains.

Acceptance still requires remaining-advisory disposition/remediation,fresh native
browser evidence and environment/inventory reconciliation. No suppression,fake
braces patch,full security acceptance,commit,push or Azure deployment.

[Detailed candidate outcome](../REPORTS/WEB_DEPENDENCY_CANDIDATE_2026-10-23.md).

## Original discovery — retained

23October2026. Existing `next16.3.5`, `sharp0.35.4`, `source-map-js1.2.1` and
dev `braces3.0.3` match14official npm advisory ranges (3critical/5high/5moderate/
1low). Registry response and lock hashes retained; no package changes at discovery.
This record is not14separate proven application exploits or a new testing plan.

Source static-export/nginx/Azure Static Web Apps configuration and absence of
searched server-specific feature declarations may narrow runtime exposure, but
are not deployed non-applicability or full build/client-path security proof.

Next:review compatible Next16.4.1/sharp0.35.5/source-map-js1.2.2 remediation and
upstream/constrained-exposure options for braces (advisory currently lists no
patched version). Fresh lock/install/build/typecheck/lint/selected role/browser
regressions under existing resource/provenance gates; keep failed advisory result
and historical functional receipts. Do not close/waive from static configuration
or silently override/suppress. No customer exploit test or Azure deployment.

[Full audit, publisher links and evidence boundaries](../REPORTS/PACKAGE_ADVISORY_CHECK_2026-10-23.md).
