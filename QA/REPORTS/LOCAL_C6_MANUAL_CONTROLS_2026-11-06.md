# Local C6 Manual control integration — 6 November 2026

Status: implementation integrated; local validation recorded below. Claimed before edits. Local Codex integrates cloud C6implementation
1ec22e733696fa84e4dbf947da463fbe4a2d123a/report2cd20ee2e6beb67a625356b7bed80dbeff17f812.
All eight affected paths are unchanged locally from cloud's83dc3aabase; preserve
newer C5/native session/pinned frontend results and the original dirty trees.
Exact integration paths:
- `QA/HANDOFF/C6_MANUAL_CONTROLS_2026-11-06.md`
- `QA/ISSUES/BUG-API-0005.md`
- `QA/ISSUES/BUG-FUNC-0005.md`
- `QA/tools/finance-governance.test.cjs`
- `QA/tools/manual-controls-browser.cjs`
- `QA/tools/teacher-publication-contract.test.cjs`
- `apps/web/src/app/finance-governance/page.tsx`
- `apps/web/src/app/teacher/page.tsx`

Own report: QA/REPORTS/LOCAL_C6_MANUAL_CONTROLS_2026-11-06.md.
Import reviewed implementation and issue/report updates; retain original issue
history and OPEN status. No backend/domain/SQL/model/lock or shared cloud rewrite.
If needed the Manual browser runner may gain the same strictly loopback API-origin
option as the local PENTA browser runner, preserving all existing tests/fixtures.

Run current TSX handler contracts, isolated pinned build/types/lint and controlled
browser checks using the already-prepared exact-version D: environment. No new
package/model/image download or private audit submission. These checks do not
close the two issues' live HTTP/SQL/device/critical closure requirements. Existing
C5/read-only verification and native session SQL remain source-scoped, not rerun.


## Integrated behavior and source boundary

The two product pages and the returned handler tests/issue records match cloud
implementation `1ec22e733696fa84e4dbf947da463fbe4a2d123a`. The Manual browser QA
runner alone additionally accepts a strictly validated canonical loopback API
origin, retains the Chromium sandbox, hides its process window and verifies its
owned temporary-profile path before cleanup. Six invalid-origin preflights fail
before changing the existing evidence receipt. There is no dependency, backend,
DTO, domain, authorization, SQL harness or model change.

- Finance prompt dismissal returns before any mutation or list reload. Explicit
  empty approval notes remain allowed; rejection still requires a reason.
- Teacher native FormData publication values become JSON booleans; invalid choices
  send nothing. Optional blank assessment dates become null, valid dates use UTC,
  invalid dates send nothing. Fractional maximum scores remain supported.
- A confirmed save displays success and resets the submitted form; denied or
  interrupted saves retain it, with no automatic retry or false success receipt.

## Local evidence

Existing exact-version D: candidate: Next 16.4.3, Zod 4.6.7; all 358 installed
package versions match the committed lock. This remains a cache-derived isolated
environment, not fresh npm ci, a dependency audit or a production-configured build.
No additional downloads were needed in this batch. Original caches stay unchanged.

Run `f16c36c9d7c643d69943b1e6eeee7a08`:

- Targeted actual TSX handler contracts: **36 PASS** (16 finance, 20 Teacher).
- Actual webpack build, static postbuild, TypeScript and ESLint: **PASS**; ESLint
  reports zero errors and 63 existing warnings.
- Actual sandboxed Edge Manual browser: **15 groups PASS**, zero runtime errors.
- Actual sandboxed Edge PENTA browser: **24 groups PASS**, zero runtime errors.
- Browser requests are intercepted synthetic responses. Native dialogs, DOM,
  FormData and UI recovery run in the real browser; Identity/SQL writes do not.
- Initial full-suite environment omitted semantic audit support and the deployment
  workflow. Preserve that failed receipt, including its symlink failure. The
  missing support files were copied byte-exact from the claimed Git source;
  neither tests nor application code were changed to make them pass.

Successor `e8f3be44a10e4a629f2f7f7f532ab290` reruns only the full frontend after
restoring those unchanged test inputs: **1,920 PASS, one FAIL, zero skipped or
cancelled**, across 69 current test files / 1,921 tests. The remaining failure is
Windows EPERM creating the deliberately dangling file symlink in
`static-segment-export.test.cjs`. No privilege setting, test assertion or skip was
changed. The full suite and the aggregate validation window are therefore **not
green**. The previously passing build/browser phases were not needlessly rerun.

Raw run logs, captured synthetic requests, before-images, supporting-source hashes
and browser receipts remain excluded under D: `.build-check`/private operations.
Both runs restored the exact original service IDs/images, API health 200, full
Docker inventory and released the validation lease. Those checks PASS in the final
source-bound proof; no container, volume or image was pruned.

## Acceptance and next work

Both BUG-API-0005 and BUG-FUNC-0005 remain **OPEN**. Current live HTTP/Identity/SQL,
assigned-batch/tenant denial, exact-one-row/attributed-audit checks, physical-device
behavior and critical regression closure remain required. This batch does not
close the wider optional-date bug or qualify any model.

Next local packet: current built UI coupled to the existing real disposable
Identity/SQL browser harness, retaining native session revocation/password checks.
Keep model loading/training/activation stopped, Manual available, release gates
open, raw evidence local, and main/Azure unchanged. Cloud can continue disjoint
role/workflow work from this feature checkpoint; do not overwrite active files.
Model route: **Sol High** for live authorization/session/SQL acceptance; **Sol
Medium** for settled documentation and visual work.
