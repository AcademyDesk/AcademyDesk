# Staff Manual feedback — 25 October 2026

Existing BUG-FUNC-0003/E2 continuation of the independently qualified
[staff audit repair](STAFF_ACTION_AUDIT_ATOMICITY_2026-10-25.md). Do not repeat
the2971API suite for this UI-only packet. Backend image
`ee9df6365eba476b973a940a45f02b8c` was the initial backend; the actual browser
run below exposed an additional missing supported-role preparation failure.

## Reproduction and scoped repair

Original Staff TSX hash
`31da85401ab865ef56522598da5c206b871037c213e9b932c1ebb34674f7cc3e`.
Actual-source controlled handlers reproduce25failures/one PASS out of26cases:
create has no success message; role/offboard messages are cleared on load; no
synchronous cross-action lock, uncertainty recovery, checked read shape or mount
guard. Baseline TAP retained at
`QA/EVIDENCE/staff-feedback-baseline-20261025.tap`.

Only `apps/web/src/app/staff/page.tsx` changes relative to the accepted Access
Review export. It keeps success after checked readback, clears create fields only
after confirmed201with a typed receipt, preserves rejected/uncertain drafts,
serializes create/role/offboard, disables controls throughout write/readback,
separates saved/readback failure from unknown outcome, blocks blind repetition
until explicit directory refresh, ignores late unmounted replies and keeps the
existing offboard confirmation/cancellation. Roles/payloads/server authority and
password policy unchanged. Accessible polite feedback and a read recovery button;
no “restart the API” implementation guidance in the customer error message.

## Required acceptance

- [x]26actual-source handler checks PASS, including cross-action/pending/readback,
  refusal/500/network uncertainty, unmount and offboard cancellation.
- [x]Fresh offline frontend window `9ccc80fdc73c4359b9ffc394b4d28b0e` PASS:
  UI packet `dfc8f5120bea4eb38a7d9c3e1eb2ca78`, web build
  `dc13a41f7b3d4c0da0c046ec17f34cd2`,26handlers/typecheck/targeted lint/full
  build/source-bound627file export, no downloads. Exact original healthy services,
  complete inventory and lease release PASS.
- [ ]Real Owner login/create201/role200/offboard200in390/1440light-dark with
  reset/durable messages/actual SQL identities,12success audits and four screenshots.
  Target old tokens must expire after real role stamp rotation; new FrontDesk
  token reads enrollment200, then loses access after actual offboard.
- [ ]First journey's three actual audit500faults (create/role/offboard) preserve
  all11tables/draft/account/token, block repeats and recover after explicit read.
  Cancelled real offboard dialogs send no write. No response mocks/token injection.
- [ ]Independent exact source/export/native qualifier; exact original PENTA
  services/images healthy/full inventory/owned cleanup/lease release.
  First native window `1950e61f06e3408e918b3928c92e3859` FAIL; SQL
  `30ac76d5ff1f47beb01b7391df275672`31checks/current65file payload PASS, but
  the normal retry changing Operations to FrontDesk returned500. Create-fault
  rollback/reset/success and the failed-role rollback observations are retained,
  not accepted as a complete journey. Exact original services/inventory/cleanup/
  lease PASS. The role fault's500alone does not prove audit-failure provenance:
  the same missing-role condition also caused the normal retry500.

## Missing supported-role prerequisite found by real browser

Staff creation lazily prepares its supported role. Production bootstrap creates
Manager/FinanceUser/etc., but not FrontDesk/Operations/Sales/Marketing; UpdateRole
assumed the target role already existed and AddToRoleAsync failed when it did not.
This is an actual product failure, not a reason to seed away the missing role in
the fixture. Existing BUG-SEC-0004 role-route continuation, no new privilege set.

The minimal successor prepares only an already-supported canonical staff role
inside the existing Identity/domain transaction, before removing current roles.
Failed Identity role validation returns a checked failure; unsupported Owner/
AcademyAdmin/custom role names fail without a write. Existing role metadata is
reused untouched. Eleven real RoleManager/EF InMemory checks cover all six allowed
missing roles, canonical naming, reuse, unsupported privilege and validator
failure. Fresh full API/image window `d9858a86005d46b5a66947b34ed73ee3` PASS:
2982/2982tests/zero skips/failures, image `52c9676e11d24cf9863a123d178021a1`,
`sha256:8a7690edeba2cb6e8ff442dd2e4402b6f607884f2a3549f8899aa69d1ccae57a`,
484source pins/65published files/93,742,871bytes. Exact original services healthy/
full inventory with only intended owned image added/lease release PASS.
UI source/export remains unchanged, so no repeated frontend build is needed.
Afterward rerun the original four-journey/three-fault browser assertions, with
explicit zero→one FrontDesk role proof and rollback when the first audit fails.
Role creation concurrency/collisions/whole authorization remain OPEN.

Post-repair memory envelope: window `e35bfb9433e143068d80b8375794be13`
verified current image payload `30b5219ebdd54f11b9b2b4c33f96ea14`,65files,
93,742,871bytes. Subsequent SQL preflight refused under unchanged3GiB guard,
so no SQL/API/browser started. Parent remains FAIL; exact original healthy
services/inventory/lease release PASS. SQL-only retry
`22f3e961c0a64aea8c0068e4e14cf805` admitted3.278GiB and is RUNNING with
the same image and source-bound frontend; payload is not copied again. The
independent qualifier binds the separate payload/strict failure and restoration,
and still requires a fully successful native/browser child and parent.

Native SQL `3d4c00f060ad4466909be7852f3a7dc4` passes52ordinary HTTP
checks/four actual journeys/three actual audit faults/twelve success audits and
FrontDesk zero→one preparation. Functional result is not visual acceptance:
manual inspection of390light/dark PNGs found long synthetic staff email addresses
expanding the directory's mobile grid and clipping its fields/content. The old
root-document scrollWidth check returned no overflow because a parent clips the
content; it did not prove child geometry. Retain that exact receipt. Next scoped
CSS sizing repair/stricter visible control bounds and fresh export required, with
no new backend rebuild or modified role/feedback semantics.

The functional parent `22f3e961c0a64aea8c0068e4e14cf805` completed PASS/exact
original healthy restoration/full inventory/cleanup/lease. Its original
source-bound qualifier passed before the CSS change, SQL SHA256
`d5501bf1b5f6ab83bacaa704eb652d0de1253b7065d463fac2235a0fe59fe1bc`,
parent SHA256
`761e24ef9376d5747faecfa6bd3e8c7d9055166b3ea3447a68531ed625ba5fc3`.
Those receipts remain functional-only in view of the screenshot finding.

## Scoped mobile containment successor

Only Staff CSS selectors now use zero-minimum grid tracks below860/760px,
bound nested directory rows/metadata/labels to their panel width and keep the
title icon from shrinking. Existing text ellipsis/values remain unchanged;
no global overflow hiding or truncating away data was added.
The added browser oracle measures at least20real nested Staff control boxes
against viewport and panel bounds, rejecting hidden parent-clipped overflow.
Synthetic oracle negative controls PASS; existing26handler checks retained.
Fresh CSS-only frontend window `f4a835d0f6df480faec47ee760dc5639` PASS:
UI packet `5ce03dcd687040d584db0ffde6a9290c`, build
`b35764ec88cc4fd09eb93523f8e22f99`,26handlers/typecheck/lint/627files,
exact original healthy services/full inventory/lease release. No download/install.
Backend/source-image evidence is reused unchanged; fresh native four-journey/
three-fault/geometry screenshot acceptance remains required.

## Final qualified containment result

SQL `1f5446c9dd944ea5b00f046246f1ccf0`, parent
`d9df857c0cea46cbbcfc471ead87df25` PASS52native HTTP/four actual
390/1440light-dark create-role-offboard journeys, three audit500rollback/recovery
controls and twelve success audits. Each viewport/theme now passes actual nested
viewport/panel geometry:24/30/36/42measured controls respectively. All four
PNGs inspected; long email ellipsis remains inside directory rows and role
selectors/buttons are contained. No page errors/429/unexpected5xx. Cancelled
offboard dialogs send no write. Successful role change invalidates the old bearer;
normal FrontDesk login grants the expected enrolled-record read and actual
offboard rejects that new bearer. FrontDesk is absent initially, is not persisted
by the failed role/audit transaction, then exists once after successful assignment.

Independent `staff-layout-browser-qualification.cjs` PASS binds2982API TRX/current
484source pins/new role-preparation image,26handlers/typecheck/lint/CSS-only
627file export, earlier separately verified65actual image files and exact failed
memory-parent restoration, plus this successful native parent. Failed original
missing-role and RAM receipts stay FAIL. Exact original PENTA IDs/images healthy,
full pre/post Docker inventory identical, only owned SQL/API/network removed and
lease released. This is the accepted successor; earlier functional-only clipped
screenshots remain unaccepted for visual layout.

SQL SHA256 `e05ac126989b8bfeb753042e31ae2c28bce337f9b5f19e85784e120a36e92dce`.
Parent SHA256 `b5edd33cc82bbdc26f0addf6820ab0428a82420dcc169d5aa7088e4b0dbbda4e`.
PNG hashes390light/dark,1440light/dark:

- `14e3ea752156bcb318cc07caa8016491cff70733e01bbf58623d2bc5f7553a2b`
- `425c8e8924846cf45831167cae5abe610f55394d2f2b333e634c5b99f1c33e7a`
- `7237a4aa46c9d62742567013d4ee35c21d823385c6cb153003ebefb6b00a8ef8`
- `74765d72e0ffe3e9a9c7e12e80f7098a4972bf72a5d89966e4a19f4134496018`

BUG-FUNC-0003/BUG-SEC-0004/BUG-API-0002 remain OPEN for their wider scopes,
including arbitrary IdentityResult failure, role-creation/replacement races,
protected-role policy, all portal/device/operational release acceptance. No
accounting/privacy/security independent sign-off, standard source-image release
or production approval follows from this bounded Staff packet.

Stay within approved two-service validation windows and unchanged3GiB guard.
No main/Mini/GitHub/Azure/customer data/model changes. These desktop browser
viewports do not qualify physical devices, whole staff security or release.
BUG-FUNC-0003/BUG-API-0002 wider scopes remain OPEN. High for linked identity/
audit/session work; Medium for settled visual refinements.
