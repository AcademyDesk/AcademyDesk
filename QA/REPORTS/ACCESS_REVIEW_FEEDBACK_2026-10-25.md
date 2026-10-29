# Access Review feedback and pending-state repair — 25 October 2026

Existing E2/BUG-FUNC-0001/BUG-FUNC-0003 feedback continuation after qualified grant/custom-role
save-audit and payroll packets. No backend/permission/PENTA tool change. The only
new frontend source delta is `apps/web/src/app/access-review/page.tsx`.

## Reproduction and repair

Actual original TSX source hash:
`862d15f501dd5b6aae5c02b722fabad80af6357e3620bf7a9c635ac2c7bbcde2`.
The original grant handler accesses React `event.currentTarget.reset()` after
awaiting the server. A persisted grant can therefore be followed by a null reset
error. Both grant/revoke success messages are also cleared by their register read.
There is no synchronous mutation lock, uncertain-outcome recovery or caught
network failure. Actual-source controlled event/transport reproduction:
`QA/EVIDENCE/access-review-feedback-baseline-final-20261025.tap`,16tests,
one PASS/15FAIL/no skips/cancellations. This is handler evidence, not native SQL.
The first QA orchestration waited on its own duplicate gate; only its two owned
test processes were stopped and its incomplete output retained. Corrected runner
releases all gates even when assertions fail. A later fixture correction uses the
actual revoke204contract; it does not change backend expectations.

Repair captures the form before await, keeps success after readback, serializes
same-tick/cross-action submissions, disables controls during writes/readback,
preserves refused drafts, distinguishes confirmed-save/readback failure from an
uncertain result, and requires explicit register refresh before another mutation.
No automatic retry or claimed idempotency. Late unmounted results do not reset
forms or publish state. Accessible live feedback and a labelled staff selector.
All16controlled actual-handler checks now PASS.

## Current validation

- [x]16actual-source callback tests PASS.
- [x]Fresh offline frontend build/typecheck/lint window
  `581a895f354346269b6d989b2577f880` PASS. UI packet
  `dbccb1a85df246468467bea82e9a092d`, web build
  `e9e4b05575b6462c8ccc833ec97ed310`,627source-bound exported files;
  exact original services healthy/full inventory/lease release PASS.
  No downloads/install/API rebuild.
- [x]Actual Owner login/grant200/revoke204in390/1440light/dark; exact saved
  identities/permissions/audits, durable feedback/reset, same Operations staff bearer
 403→200→403and screenshots required. No API mocks/token injection/model.
- [x]First journey adds actual required-audit grant/revoke500faults with
 11table unchanged snapshots, draft/grant retention, blocked repeat and explicit
  read recovery. Exact temporary fault revoked in finally; no unauthorized writes.
- [x]Exact original PENTA services/images, full inventory/owned cleanup/lease.
  First native window `c0b17ca0788f4169b15a4db260ea1f3b` FAIL: fixture selected
  a Teacher omitted by StaffController.List. Actual Owner login/read passed but
  the picker timed out before any grant request. SQL
  `8588c54d22b649c08bf4df9d475d17e8`/31checks retained; exact originals healthy,
  complete inventory/owned cleanup/lease PASS. No UI/backend rule was broadened.
  QA now creates one synthetic Operations account through the actual Owner API,
  logs it in normally, and retains that same bearer through all grant/revoke
  checks. Identity census is explicitly six. Corrected native window
  `d3a7946fb9b2430892a6182c1e265cc4` stopped at2.797GiB under the unchanged
 3GiB guard, before payload inspection/SQL creation/API requests. Exact original
  services healthy/full inventory/lease release PASS. No rebuild or guard waiver.
  The corrected fixture has not yet received live browser acceptance.

Further bounded attempts (all retained, no application rebuild):

- Window `899b658b3ff1451fa280915b6ebe41c5` admitted3.037GiB and actual image
  inspection `4eee926a23b84b89966a38973de2182b` PASS65files. The following
  SQL preflight refused before creating SQL/API; parent remains FAIL. Exact
  originals healthy/full inventory/lease PASS. Reuse this independently bound
  payload proof; SQL-only retries still enforce the unchanged3GiB guard.
- SQL `26169044a148483bb7673aa8f166d183`, window
  `1fd167995c7e4b37a3f597fa7feb6651`:31native checks/synthetic Operations create
  passed, then the strict native transport rejected the login query string before
  dispatch. Parent FAIL, exact owned cleanup/original services/inventory/lease
  PASS. Fixture now uses the existing native `/api/auth/login` token route;
  transport allowlist and application remain unchanged.
- Corrected SQL-only window `15e5404b2c60494792ef0e7fee33a8f0` PASS.
  The final independent qualifier requires a successful current SQL/browser run,
  exact original restoration and separately verified same-image payload; it does
  not turn either failed parent into a pass.

Last lightweight validation:16actual-handler checks and12QA transport/oracle/
SQL-safety checks PASS. No remaining process from the owned frontend build or
failed browser run was found; unowned applications/containers were not stopped.
Host RAM recovered naturally; the final SQL-only window admitted3.439GiB under
the unchanged guard. The backend's2956API suite is retained, not rerun for this
UI-only change.

## Qualified final result

SQL `a0208b1abdfb465fa0d8086a856804d7` PASS53native HTTP checks; the four
actual Owner browser journeys each create one grant200and revoke it204. The first
also has deliberate Create/Revoke500audit faults. Each failure leaves all11SQL
snapshots unchanged, retains draft/current grant, blocks repeat writes until
explicit register refresh, and preserves the existing staff bearer access.
Four grant rows are finally revoked, with exactly eight attributed success audits.
The original Operations bearer proves403→200→403per journey, including retained
403after failed create and retained200after failed revoke. Roles stay unchanged.
No429/unexpected5xx/page errors/horizontal overflow;109migrations and retained
admission/deactivation negative controls PASS. These are desktop browser viewports,
not physical Android/iOS acceptance.

`access-review-browser-qualification.cjs` PASS binds the current API source/image,
the source-verified627file export,16handlers/typecheck/lint/build, exact successful
SQL window restoration and actual65file payload from the earlier failed parent.
That parent remains FAIL for its exact subsequent SQL-memory refusal; the
qualifier requires that literal failure/no-start log and exact restoration rather
than accepting arbitrary failures. Both real browser screenshots per theme and
viewport were inspected. Desktop captures show the current internal scroll
position, not a claim that the whole long form fits one viewport.

SQL receipt SHA256:
`32e33da3bbe9f7671b86fe02780b6e6ee97a487f5201f8545f314e6952d19898`.
Window receipt SHA256:
`8367ebd0f09fa576d717026bced598f64c9ca73a2ecb361f95fb45846915a793`.
PNG SHA256 in390light/dark,1440light/dark order:

- `78cba62447436429911ccfcdc8a7b8bb35dcc942b26ca426b2ca4eb332a3fb31`
- `e9b974e8b885311f47f209d577a563ab968c1c11a33bb320dee3fd0a40ae2f99`
- `608564ff3c568110354b5eff723e209fe2c22b80f909f4b40671001db920bdaa`
- `d51b3612785ca7981ae1c9ec02a06fa00c114baf7653e606f72636bdadda4099`

BUG-FUNC-0001/0003 remain OPEN for wider sites/devices/critical acceptance.
BUG-UI-0003 expired-grant status/count is a separate existing unresolved case;
this permanent-grant feedback packet does not qualify expiry semantics. No
concurrent-server idempotency, whole access-management or release closure.

Use the approved two-service bounded validation windows and unchanged3GiB guard.
No main/Mini/GitHub/Azure/production data/credentials/model changes. Whole
Access Review roles, date/expiry UX, devices, concurrency, AI and release gates
remain OPEN; this scoped feedback repair is not enterprise closure. High for the
live security/fault integration; Medium for settled visual refinement afterward.
