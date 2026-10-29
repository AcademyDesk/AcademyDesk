# Enrollment conflict feedback — 24 October 2026

## Finding and bounded repair

Actual enrollment page mapped EVERY HTTP409 to “already actively enrolled,”
overriding authoritative capacity, prerequisite, waitlist and batch-open reasons.
This was misleading even though the new backend correctly rejected the operation.

One page's feedback handling now displays a nonblank server message for expected
4xx rejections.409without a valid message uses a neutral conflict/rules fallback,
not a fabricated duplicate reason. Draft/status/reason remain retained, no retry,
no success/reset/readback on a refused write.5xx/network uncertainty handling and
existing same-tick/pending guards stay unchanged. No API/domain/authority change.
React renders the notice as text, not injected HTML.

Existing duplicate regression now requires the true shared duplicate/waitlist
message. New tests cover Create/lifecycle409for prerequisites, active capacity,
waitlist capacity and batch-open rules, plus null/missing/nonstring/blank/invalid
JSON fallback. Tests reproduced prior misleading behavior before the page change.
Full affected handler94/94PASS, including14new denial/fallback cases and an updated
duplicate expectation; historical79counts are retained, not reused as arithmetic.
zero failed/skipped. They are synthetic actual-handler tests, not native SQL or
browser proof. Existing native enrollment browser assertion now requires the exact
real duplicate response body to be visible; it was not weakened to ignore notices.

## Current frontend validation

Window `f2d48dd6cade454abba310cd1b1881e2` FAILED inventory validation under the approved bounded
frontend window authority. Unchanged3GiB guard. Planned existing-dependency offline
typecheck/targeted lint/normal npm build with isolated copied inputs on D:, no
install/acquisition/manifest changes or deployment. Exact original two services
must restore healthy/full inventories/lease release even on failure.

Actual UI child `12e4a3664ea247aa97e57c99cf05c86b`:94handlers/typecheck/
targeted ESLint PASS; fresh ordinary Turbopack build
`0e2247266b4b4d8db4e8c2a6e20e0cf4` PASS,627export files hashed/verified,
84static-segment aliases prepared. Both original service IDs/images restored healthy;
all29containers/41volumes/13images retained. Four networks remained, but the default
bridge ID changed. This failed envelope is retained without waiver; idle-Docker
causation is not proven. One controlled retry reuses the existing owned16MiB,
network-none idle safeguard, removed after originals restore. Full inventory
identity and the unchanged3GiB guard remain mandatory.
Inputs copied from existing
physical worktree dependencies, no install/acquisition/manifest edits.
The later browser window will use only this exact new hashed export and require
four normal native lifecycle journeys plus real full-capacity409, disabled-
waitlist409, prerequisite409 and closed-batch400 visible denials. Normal duplicate
notice must now exactly match its real Active/Waitlisted backend response, rather
than the old generic text. New four-denial proof requires unchanged Enrollments/
AuditLogs hashes and unchanged submitted draft. No response mocks/credential
injection or live model invocation.

Controlled retry `a0da1a8fecb8489b9e1c21c28fcd36ea` PASS: new UI child
`d761282fbef34d008befe0d6b34401fc`, ordinary build
`27295f0db57a4656862cbf52186ee9eb`,627hashed export files.94handlers,
typecheck, targeted lint and build all PASS. Owned16MiB network-none witness
verified/removed after exact originals restored healthy; all inventory identities
preserved and lease released. Earlier failed bridge-drift window stays failed.
142focused QA-tool tests PASS,0failed/skipped (separate from94handler checks).
Native browser SQL `da6234f227a6414a998a2413c9362cb7`, window
`00cf5a2368d7479198224424db3a3adf` PASS against only this fresh verified export.
46HTTP checks, four normal create/duplicate/pause/resume journeys and four actual
capacity/waitlist/prerequisite/closed-batch refusal journeys at390/1440 light/dark.
All submitted drafts retained, no rejected write/audit, no429 or uncaught page
errors or horizontal overflow. Exactly five Active enrollment rows: four normal
browser enrollments plus one synthetic capacity occupant. No customer data used.
109migration histories verified; owned disposable SQL/API removed, exact two
original services restored healthy, full inventory identities preserved, exclusive
lease released. Eight screenshots and627export hashes independently verified.
Mobile light normal and mobile dark prerequisite screenshots visually inspected.

Independent acceptance command PASS:
```text
node QA/tools/enrollment-conflict-qualification.cjs d761282fbef34d008befe0d6b34401fc a0da1a8fecb8489b9e1c21c28fcd36ea da6234f227a6414a998a2413c9362cb7 00cf5a2368d7479198224424db3a3adf
```
SQL receipt SHA256 `e9df01037d796ff1a209eb993ef9d563cb4dda36c849be4345e9ed1ddc245a25`.
Successful UI-window SHA256 `9eac0af1062ad3c3d3e76362aee6efb79126e5b6f01654de784d7c42a9276fb2`.
Browser-window SHA256 `0d9d31f08c4e837d0a5b556afa14041679511eb130c2bb47318d5483e1b95bf1`.
First failed bridge-drift envelope remains failed and unchanged. No standard
release-image approval or physical Android/iOS qualification is implied.

The actual Next forms guide was read; existing client-handler/API architecture is
retained rather than introducing Server Actions or bypassing backend permissions.

No full visual migration/production approval. HIGH dependency, existing all11
packages and device/AI/enterprise release gates remain OPEN. Prior API/SQL evidence
stays valid for its exact API binary; earlier frontend receipts do not certify the
changed page/export. No main/Mini/model/push/Azure/production data changes.
