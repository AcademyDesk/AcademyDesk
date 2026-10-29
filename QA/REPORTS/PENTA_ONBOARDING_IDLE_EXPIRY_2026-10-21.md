# Mounted onboarding expiry repair — 21 October 2026

Worktree `D:\AcademyDesk-codex-p0`, branch `codex/penta-search`, unchanged HEAD
`b1f8cb6f4209da7fbd14f8089fcb6792438684c4`. Successor to the accepted
[recheck/reload expiry proof](PENTA_ONBOARDING_EXPIRY_2026-10-21.md), not a restarted
enterprise audit, new model feature or permission expansion.

## Gap and repair

The prior proof establishes host denial and private-view removal after an explicit
recheck/reload. Inspection found no mounted lifetime watcher: a previously loaded
draft could remain on screen while idle. The adapter also did not reject an already
verified object at its later expiry, or recheck the timestamp after the final async
HTTP/hash fence. Server authorization still denied expired operations; this was not
evidence of a backend authorization bypass.

- Keep the exact server draft deadline in the existing ephemeral provenance WeakMap.
  A clone, changed projection, UI label or model response cannot supply this value.
- Binding access and dialogue presentation reject at the exact expiry boundary.
  Presentation copies the original deadline without renewal. The final read-adoption
  fence rechecks expiry after async work. No shared contract/vendor file changed.
- A mounted presentation watcher hides the view at the deadline, and on focus or
  visibility events if a background timer was suspended. It clears listeners/timers
  on disposal and cannot invalidate a newer view through a queued stale callback.
  Long timer delays are capped; invalid local clocks fail closed.
- Expiry unmounts the child workspace, clears the recovery handle and shows the
  existing blocked explanation and Manual link. Child disposal retains its existing
  abort/dispose behavior. There is no automatic HTTP poll, refresh, retry, claim,
  approval or domain action. The host remains authoritative if client time/code is
  manipulated; this watcher is not a new security authority or remote-revocation feed.
- Browser QA advances the actual page clock for the two selected cases after the
  separate isolated host clock is expired. Private details must disappear before any
  Recheck click and with no extra request. Subsequent real HTTP410/reload and exact
  SQL postconditions remain mandatory. Pending cases still withhold private details
  and verify expired host reads; no new pending lifetime is inferred by the browser.

## Validation

- Before repair: the new file recorded 1 pass/11 failures. Two existing-behavior
  assertions reproduced missing binding rejection and missing final-read rejection;
  other failures requested the not-yet-implemented watcher API, not additional
  independently reproduced application bugs. Red log retained.
- After repair: **12 new checks PASS**, plus review48, expiry10, recovery42,
  dialogue36, terminal33 and operation44: **225 distinct focused checks PASS**.
  Controlled transports/clocks, not SQL/model/physical-device acceptance.
- TypeScript and targeted configured ESLint PASS, no reported errors/warnings.
- Production webpack build/export **87/87 routes PASS**, including TypeScript.
- Full configured ESLint **0 errors /0 warnings**, exit0; no suppressions/config changes.
- Isolated SqlHarness build: **zero warnings/errors**, 51.42s.
- Updated full runner: **PASS**, run `8766bdab56a34b7eace88d5d6144bb39`,
  **exit0 /188.4s**, `QA_ONBOARDING_REVIEW_BROWSER=1`, module `OnboardingAtomic`.
  **461 retained SQL/API checks and 33 actual browser contexts PASS**: review15,
  operation4, Start5, dialogue3, pending2 and expiry4. Three terminal reloads are
  nested in operation contexts, not three extra contexts. Counts overlap earlier
  checkpoints; do not add all evidence layers into one completion total.
- Both selected contexts explicitly prove **idleExpiryVerified=true** before
  recheck, without an extra browser request. The four expiry contexts still make
  28 GETs total/eight current-state HTTP410 responses/zero browser writes, with
  eight exact actor/academy read audits, two expired-result API410s and two unchanged
  pending claims/no completion receipts. Selected SQL snapshots remain unchanged;
  zero new student or inference in expiry cases. Expected HTTP410 resource console
  diagnostics are recorded separately; no unexpected page/console errors or429.
- Retained operation cases still create three synthetic students and dialogue
  creates one after separate approval. Those intentional fixtures are not included
  in the new expiry cases' zero-creation assertion.
- Runtime inventory360/exact DbContext and scoped-login target/102app+7Identity
  migrations verified. Negative cleanup marker refused; exact owned database/login
  teardown confirmed and successful container absent after runner cleanup. Earlier
  failed container remains stopped; final29containers/41volumes, only two PENTA
  services running. No prune or resource restart.
- Actual idle-before-recheck screenshots inspected for light320 and dark1440;
  no horizontal overflow. Not full viewport/theme Cartesian coverage or physical
  Android/iOS acceptance. Focus/visibility suspension is additionally tested with
  controlled events, not claimed as physical-device/background-OS proof.

Logs under `.build-check/session-resume/`: `onboarding-idle-expiry-red.log`,
`onboarding-idle-expiry-green.log`, `onboarding-idle-review.log`,
`onboarding-idle-contract.log`, `onboarding-idle-operation.log`,
`onboarding-idle-web-build.log`, `onboarding-idle-full-lint.json`,
`onboarding-idle-harness-build.log`, `onboarding-idle-sql.log`.

Local evidence:
`QA/EVIDENCE/penta-onboarding-expiry-live-8766bdab56a34b7eace88d5d6144bb39/`.
Contains `result.json`, independent `sql-postconditions.json`, four post-recheck
screenshots and two idle-expiry screenshots. Same-run retained browser directories
contain their independent receipts. SHA256: result
`E3D40A11038863DD2E63E00336DE82183C0E9F7ABCA97959C20E84F4ED163210`;
SQL postconditions
`BB9745FBA3CED6A3287DAB5CDD693D2C600ADB9AAF30EBA6B084674A6B63D557`.

## Source identities (SHA256)

| Source | SHA256 |
| --- | --- |
| `apps/web/src/lib/penta-onboarding-review.ts` | `06FBB773FDA55534167B4809B23C30EF337A87236F500832B1542C640F5F4F03` |
| `apps/web/src/components/penta/onboarding-review-workspace.tsx` | `D1FA67F63D657BFF7F0791714278A5E0E4781F53F25392069D3359DB6E5F0D16` |
| `QA/tools/penta-onboarding-idle-expiry.test.cjs` | `2294E2B6EBE95B85D9A0B9C8D60E247E5ACF466FBBAB78AA0565F45B6FE4699B` |
| `QA/tools/penta-onboarding-review.test.cjs` | `A037CA48BC7A6608F950CD17C048CF5F8827A9B9D1A68E6D6CBEEECF364BB787` |
| `QA/tools/penta-onboarding-expiry-live.cjs` | `2C4F0C280F9EF53C6197830B1DFE4F6AD39CFF81DD7CD6888ECDA5D15ECA0CA6` |
| `QA/tools/SqlHarness/PentaOnboardingExpiryBrowserRegression.cs` | `AEA01057D51DA9DF57518265D7E93DC11D8046BDF167F827563095DB238C6DB6` |

## Boundaries and remaining work

No backend/schema/business-policy/model/default/weights change. No vendor snapshot,
runner, quota, assertion or timeout weakened. No main/Mini edit, commit, push, Azure,
training, acquisition, package install, model activation or resource restart.
Prior failed evidence is retained. QA/EVIDENCE stays local; .build-check ignored.

Model full-plan fresh6/12/exposed7/12 remains NOT ACCEPTED. The 56 review examples
remain pending/zero eligible; approved independent data review and a scoped resource
experiment remain separate requirements. Durable history, wider workflows/roles,
linked model/domain/approval/security/device/enterprise/release acceptance and
BUG-DATA-0001 remain OPEN. No production readiness, 90% delivery or package closure.

Stay Sol High for consequential integration/qualification; Medium for settled UI/docs.
All 11 broad E0–E10 packages retain their outstanding gates. No project switch needed
for independent Academy work, and no unchanged failed-model rerun is justified.
