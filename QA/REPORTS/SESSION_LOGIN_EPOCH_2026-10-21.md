# Explicit-login epoch — bounded session repair

21 October2026. `D:\AcademyDesk-codex-p0`, `codex/penta-search`, unchanged HEAD
`b1f8cb6f4209da7fbd14f8089fcb6792438684c4`. Continues the existing session queue;
does not replace native authentication or authorize training/deployment.

## Implemented boundary

- Verified login/profile now calls `beginPortalSession`: creates an opaque random
  per-workspace local login epoch, then saves that workspace's credentials and
  emits a same-document session-change notification. If the new login supplies no
  refresh token, an old account's role-scoped/legacy Admin refresh is not inherited.
- Normal automatic token renewal still uses the existing save helper and retains
  the epoch. Legacy sessions without an epoch remain usable until explicit new
  login/sign-out. Other workspaces' markers/credentials are unchanged.
- Pending refresh captures the epoch and checks it before adopting/replaying a
  response. A completed replacement login prevents adoption even when the token
  pair matches the former pair and no StorageEvent was delivered to that realm.
- Mounted WorkspaceFrame observes replacement markers through native storage,
  same-document login events, focus/page-show and visible-tab checks. Old private
  content unmounts and offers sign-in; new credentials are not erased or borrowed
  by that recovery screen. Route changes still mount a fresh boundary.
- Marker is UI/provenance fencing, NOT identity, a JWT claim, permission, tenant,
  server logout or revocation proof. It is not sent as authorization. Server rules,
  token lifetimes, roles, claims, backend/schema and domain actions are unchanged.

## Exact limitations

This is **not cross-tab single-flight or atomic inter-process refresh/login locking**.
Another tab can still race a marker check and subsequent localStorage writes;
that synchronization window needs its own protocol/acceptance. This evidence covers
replacement completed before the old response is adopted, not every possible race.
Direct external storage writes that bypass the new login boundary may not change
an epoch. Same-origin script/storage manipulation is not prevented by this marker.
It cannot cancel/undo an already-dispatched server request or revoke a server token.
Storage/crypto failure behavior, native session breadth, real BFCache/frozen tabs,
physical devices and critical/release acceptance remain separately gated.

## Controlled validation

- Added24 helper/frame tests: four existing-code negative controls reproduced
  stale-refresh adoption after the stored marker changed (HTTP200 versus required
 401/no replay);20 additional cases initially failed because the new login API did
  not exist. Baseline22pass/24fail retained, not24independently reproduced defects.
- Added12 resume checks across four workspaces × focus/page-show/visible event.
- **188 distinct related tests PASS**, including36new plus existing refresh,
  same-realm concurrency, delivered logout, resume, login challenge, password and
  dashboard recovery. No original assertion dropped; login fixture's function
  argument renamed to match the real explicit-login API.
- TypeScript/webpack **87-route export PASS**; full configured lint **0errors/
  0warnings**, no suppressions or configuration changes.
- **24 new actual exported-browser cases PASS**: four workspaces ×320/light and
  1440/dark × real-login peer replacement, normal renewal, pending identical-pair
  replacement. Actual login handler and native same-origin StorageEvents; no fake
  storage event is dispatched in these cases. Exact same synthetic credentials are
  intentionally returned by the replacement login, proving marker—not token text—
  distinguishes the new UI session.
- Eight pending-response cases hold the synthetic refresh reply until another
  page completes login; old request returns401 without replay or credential save.
  Other workspaces' credentials/markers remain intact. Old view starts no further
  replacement reads. Normal renewal retains the epoch and mounted UI. No unexpected
  page/console errors; expected synthetic401 resource diagnostics are excluded
  explicitly, not claimed as no console output. Teacher/light320 screenshot inspected;
  tested recovery screens have no horizontal overflow.
- Browser APIs and credentials are synthetic, not native Identity/SQL/server
  revocation proof. `/qa/session-api.js` serves transpiled current helper only in the
  local test server; `/api/qa/session-epoch` is intercepted synthetic transport, not
  an application endpoint. Full viewport/theme Cartesian breadth not claimed.
- Retained **80resume browser cases PASS**, four workspaces ×320/1440 ×light/dark
  ×five lifecycle/renewal scenarios. These lifecycle events are deliberately
  dispatched, unlike the24 new native-storage cases. **104 distinct browser
  contexts total**, not208 by adding predecessor runs. No failures/page errors.
- Native **RefreshConcurrency PASS**: run `5009f7540dbb4e61b650976f8b972d0a`,
  exit0/36.4s. **25 native HTTP requests/four overlap groups**, with one refresh per
  group: Admin late401, Teacher overlap, Admin read/create and expired refresh.
  Successful groups have three401s/one refresh200/three replay200s; denied group
  has three401s/one refresh401/no replay. Each explicit login epoch survives both
  successful and rejected native renewal. C# SQL postconditions still require
  exactly one Student user/membership/typed academy link/success audit.
- Native pipeline controls, runtime inventory360/exact target/login,102application+
  7Identity migrations, mismatched cleanup rejection and exact owned teardown PASS.
  Successful owned container absent. Final29containers/41volumes; only two PENTA
  services running. No pruning, broad shutdown or resource restart.
- Isolated harness first lacked its assets file; cached-only restore from the
  existing local NuGet directory succeeded, no package downloads/new versions.
  Final build0warnings/0errors in78.98s. The failed build created no SQL target.
  Harness/runner/backend assertions were not changed. Only the native JS fixture
  now starts explicit marker-bound logins and adds marker-preservation assertions.

Logs under `.build-check/session-resume/`: `session-epoch-red-final.log`,
`session-epoch-tests-final.log`, `session-epoch-build.log`, `session-epoch-lint.json`,
`session-epoch-browser.log`. New browser receipt:
`QA/EVIDENCE/session-epoch-1792605156049/result.json`, SHA256
`5007321E2A8A5C93647A877A1256C3B29B6076DA64960E1AE0A0C5F6C57CD144`.
Retained resume successor receipt:
`QA/EVIDENCE/session-resume-1792605248370/result.json`, SHA256
`115FD7070C0A407678D2DB59F09B550CEF47754C2A9971B989DEE09AB1B76A59`.
Additional logs: `session-epoch-resume-browser.log`, `session-epoch-sql-build.log`
(failed), `session-epoch-sql-restore.log`, `session-epoch-sql-build-final.log`,
`session-epoch-sql.log`.

## Current source pins (SHA256)

| Source | SHA256 |
| --- | --- |
| `apps/web/src/lib/api.ts` | `C4514EAA55541B54A89D6FACD4FE2A1215F99AA5F391D412721F40A6EF17E20E` |
| `apps/web/src/components/workspace-frame.tsx` | `2575311BC1647FB09EF31144FC97992895F18741AA1150EEEAB2D2B4268A665E` |
| `apps/web/src/app/login/page.tsx` | `01429CCAF8B075D1130BFF80477740C5A09CA5C49105487D29F889D726944C89` |
| `QA/tools/session-epoch-browser.cjs` | `6F7535032E298A72C2877BB449E46FD63D5360673CAF68DCF8D069C1DB899A4E` |
| `QA/tools/session-logout.test.cjs` | `BE02ADBFD23532AA160801300AEBD8A034FBD8F2DA10ED98E16B4526F2E41D10` |
| `QA/tools/session-resume.test.cjs` | `FA3809EAA1D45B2CD94DB2FED747AD553C6ACD1B7610E171CBF82BCB94936D46` |
| `QA/tools/refresh-concurrency-http.cjs` | `8946894C33FCC6167243650063E1543A35402C7EA582D8C82C0BE55B981A77A1` |

## Remaining / next

BUG-AUTH-REFRESH-001 and BUG-AUTH-REVOCATION-001 remain OPEN at broader acceptance;
SECURITY-SESSION-001 remains a release gate. Model full-plan fresh6/12/exposed7/12
NOT ACCEPTED,76pending data candidates/zero eligible: no training authority inferred
from “next.” Independent author/reviewer designation/review and scoped experiment
approval remain missing. Model/history/domain/device/enterprise/release work and
BUG-DATA-0001 remain OPEN; all11broad packages retain their outstanding gates.

No Mini/model/weights/backend/schema/main changes, commit/push/merge/Azure,
customer data, acquisition/install/training/inference/activation/restart/prune.
Next High for remaining auth synchronization/host/model authority; Medium for
settled UI/docs. Continue accepted tests, not a new testing program.
