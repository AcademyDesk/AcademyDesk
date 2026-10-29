# Courses/Batches read lifecycle —21 October2026

Local `codex/penta-search`,unchangedHEADb1f8cb6. Continues QUALITY-LINT-001 and
existing catalogue preservation work;not a backend or enterprise-audit restart.

## Change boundary

- Each initial discovery owns an AbortController and validates the returned list
  before publishing. No effect-time synchronous clearing or deferred-lint trick.
- Each selected-academy collection read owns cancellation. Aborted responses,
  including JSON finishing late, cannot replace lists or error feedback.
- Switching academies clears the old register/options immediately;Batches also
  clears academy-bound course/branch selections. Selecting the same academy is a
  no-op. Non-academy create draft fields remain intact. Old editing selection is
  closed on an actual academy change;that academy's edited values are not saved.
- Collection shapes are checked before publishing any of the four Batches lists.
  This is a minimum rendering guard,not full DTO/domain/security validation.
- Existing create/edit/toggle payloads,hidden-field preservation,save locks,
  success/uncertain outcomes and post-save refresh remain in place.

No backend,role,tenant,approval,financial,subscription or schema rule changed.
Frontend cancellation is not a server authorization or tenant-isolation boundary.

## Executed evidence

-20new dependency-aware actual-component lifecycle tests PASS:discovery,unmount,
  out-of-order response/JSON,A→B selection,late failure,clear,no-op and malformed
  reads. No writes in these tests.
-139existing course/batch feedback,preservation and batch-time checks retained
  unchanged and rerun PASS. **159combinedPASS**,0fail/skip/cancel.
  `.build-check/session-resume/catalog-final.log`.
- **24/24 actual exported React browser cases PASS**:Courses/Batch setup ×
  320/1440px × light/dark × switch/clear/late-failure. Expected old-academy reads
  are awaited and deliberately delayed;new view remains correct after release.
  No writes,pageerrors or horizontal overflow. Synthetic transport,not SQL/role/
  tenant/physical-device acceptance.
  `QA/EVIDENCE/catalog-read-lifecycle-1792581400551/result.json`.
- Initial incomplete browser fixture retained at `catalog-read-lifecycle-1792581315354`:
  it omitted AcademicGovernance entitlement and correctly encountered the shared
  preview lock. Only the synthetic academy fixture was corrected;application
  entitlement checks and assertions were not bypassed or removed.
- TypeScript and87-route webpack export PASS on the final two-page source.
- Targeted lint:0errors,2existing Batches warnings(loadWorkspace dependency and
  unused binding),not suppressed. Full configured lint now **16errors/38warnings
  across6error-bearingfiles**,still FAIL. Receipt:
  `.build-check/session-resume/lint-after-catalog.json`.

Other loading/empty presentation,post-save unmount/recovery,linked native domain/
role/device acceptance and full catalogue workflow closure are not claimed here.
No issue closed. All model/enterprise/release gates remain OPEN.

No main/stage/commit/push/Azure/model/training change. Existing legitimate dirty
work preserved. Next Medium:remaining bounded UI/quality work;High:consequential
model qualification,host/authorization/domain integration and release decisions.

Final successor combined suite **418/418PASS**,0fail/skip/cancel:
`.build-check/session-resume/continuation-final-418.log`. Includes the259related
session/login/workspace/practice/date/theme/Activity and159catalogue checks;
do not add earlier overlapping counts again. Final `git diff --check` PASS,
staged0,trackedQA/EVIDENCE0,.build-check ignored,unchangedHEAD/branch.
28containers/41volumes retained,only2original PENTA services running.

SHA256:Courses020deafaf337e7b81d49bf0efc8e81e8c18c741d607b2b292f91f76197223882;
Batches387d99d882417acc8839cd37f95cc1160d2ed70411a75cdd829c6a7f2700e228;
browserreceiptf04c05da6b5617483da694377f8a3e296e7549c35db63d7280586bdeb5730602.
