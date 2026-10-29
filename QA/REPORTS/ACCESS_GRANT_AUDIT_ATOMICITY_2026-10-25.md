# Access-grant required-audit atomicity — 25 October 2026

Continuation of existing BUG-API-0002/E1/E9, not a new audit or issue census.
The accepted70HTTP/30permission normal-behavior packet is retained unchanged in
[effective grants](EFFECTIVE_GRANT_AUTHORITY_2026-10-24.md). This packet targets
the missing actual SQL audit-failure branch for manual grant creation/revocation.
It does not expose permission-management tools to PENTA.

## Acceptance

Use the same synthetic Teacher bearer before/after actual Owner grant operations.
Run-owned SQL/container/image/database checks precede temporary AuditLogs INSERT
denial for the API's restricted disposable login. Always revoke only that denied
permission in finally. HTTP500 must not save a grant or revocation, must not add
a success audit, and must leave all11domain/Identity/audit snapshot tables intact.
Teacher remains denied after failed creation and retains its previously granted
access after failed revocation. Restored audit permission permits real create/
revoke with one success audit each. No role/member/user changes,429or secrets.

Baseline mode separately requires and labels unsafe persistence if reproduced;
it can never qualify protection. Guard mode requires full rollback. Both retain
normal owner deactivation/admission refusal controls, exact original PENTA
container IDs/images/healthy restoration/full inventory/owned cleanup/lease.
Unchanged3GiB guard, no downloads/restart/prune/main/Mini/Azure/production changes.

## Current evidence

- Harness negative controls PASS: unsafe persistence, wrong bearer access,
  missing fault removal, success instead of500and erroneous success audit cannot
  satisfy guard proof.
- First baseline SQL `c126a43d1cbd4c8e9b09d8cc2336309e` captured both500writes
  persisting without success audits: created grant allowed the same Teacher200;
  failed revoke changed that same bearer to403. Guard protection NOT accepted.
  Window `18648ceba4c64de5a932db74a9d8492e` failed on a subsequent QA row lookup
  comparing SQL/API GUID letter casing exactly. Both fault cases were observed;
  the incomplete envelope remains FAIL. Corrected case-insensitive GUID lookup
  preserves all assertions. Run owned resources removed; exact restoration is
  completed healthy/full inventory before the corrected window. No application
  change was made before baseline completion.
- Corrected baseline SQL `188634bbb1c842aba92dd0396255f2ee` PASS45ordinary
  HTTP checks plus two actual500fault cases. Both unsafe persistence behaviors
  reproduced; unrelated10tables/user roles unchanged. Three recovery audits and
  zero active final grants. SQL owned cleanup/inventory PASS; parent window
  `a067ff9fc6f8467992551a307a711d8d` PASS/exact originals healthy/full inventory.
  This is a
  successful defect reproduction, explicitly NOT protection acceptance.
- Minimal repair: add existing `AtomicAcademyMutation(IncludeIdentity = true)`
  only to AccessGrantsController.Create/Revoke. The existing host filter validates
  matching configured SQL stores, enlists Identity into the academy transaction
  and commits only after the mandatory audit saves. List/permissions/roles/
  subscription/tool catalogs/platform bypass/business schema remain unchanged.
  Six action-boundary/Identity regression cases added. Full suite2933/2933PASS,
  zero failed/skipped; offline development build `f89044e59990403787d3d09f6f14552f`
  image `sha256:3025382370ea05396e09fa4da54d9911c399d0122d9279e357a9f25f53a204cf`
  produced without downloads. Window `f73e34043d2942afb1dfb404a4b0f57a` PASS,
  exact originals healthy/full inventory preserved with only intended image added.
  First repaired SQL `35d876453a114889b6fb8303cef88058` proved failed Create500
  rolls back all11tables and same Teacher remains403, then stopped at the following
  empty-row readback (no detailed diagnostic in that envelope). Window
  `834cfd34f57f470f85f7076439034077` remains FAIL/exact originals healthy/full
  inventory/owned cleanup PASS. QA now selects one explicit count/list JSON object
  for zero/nonzero rows and rejects inconsistent/malformed counts/types. Safe
  source-line diagnostics added without response/SQL/secret values. Syntax and
  two relevant negative-control tests PASS; no API/image change or suite rerun.
  Corrected SQL `2ff063277dd3477ab2130636a23df11d` PASS44ordinary HTTP checks,
  both500fault rollbacks/all11tables unchanged, two real recovery audits/zero active
  final grants. Same bearer access403after failed creation/200after failed revoke.
  Parent `f6378678cabd48ad995ec7fac33fff63` remains FAIL only for the post-SQL
  unchanged3GiB payload-inspection guard; SQL exit0/source/owned cleanup/inventory
  and exact original restoration PASS. No fault tests are repeated for that guard.
  The successful SQL child is not relabelled as a successful full parent window.

  Normal grant regression window `85fd17f7c242450da218035a9c237c31` inspects the
  same image BEFORE SQL memory allocation under the same3GiB guard. Payload
  `58583112a3164bbb83c78955785eb269` PASS65files/93,735,819bytes and every manifest
  path/byte/hash; inspection container never started and removed. Normal SQL
  `6f49f8c45baf4b57b7ca953e4b03cb46` PASS70HTTP/30permission cases, same bearer
  revoke/stored-expiry/independent scopes and all retained refusals unchanged.
  Window `85fd17f7c242450da218035a9c237c31` PASS/exact originals healthy/full
  inventory/owned cleanup/lease. Independent combined qualification PASS with exact
  known supplemental refusal/full restoration plus this separate successful payload
  and normal regression window; negative controls reject any other failure/drift.
  No broad issue closure
  or deployment. Standard source-Dockerfile acceptance remains separate.

## Final scoped qualification

The current API source differs from the previous484file manifest only by the two
exact action markers in AccessGrantsController; removing precisely those lines
reproduces the old file SHA256. Six added boundary tests and all2933API tests PASS.
Native required-audit guard44ordinary checks plus two actual500controls/recovery,
and normal70HTTP/30permission cases PASS on the same immutable new image. Payload
65files/93,735,819bytes matches every published entry. No grants/actions added to
PENTA. Issue remains OPEN for other mutation paths/platform/concurrency/devices.

Receipt hashes: baseline `b37d9b271418fbed7f1bd43625428ec24a8d3a5c4b1b39a1a598496e87e4d20e`;
guard `520cecb03f3b1089e35d0b81038f412befafd66dee1d107556d9a592fe981fda`;
normal `e25b87692e2aeb1e6e1563099cac31322961762ece40dd7820e5e17ee96ba998`.
Qualification explicitly retains the failed supplemental parent and requires its
successful separate image inspection; no failed receipt was relabelled PASS.

## Limits

Only Create/Revoke ordinary academy Owner calls, same-store Identity/domain SQL.
No platform bypass expansion, custom-role audit qualification, in-flight access
revocation cancellation, natural expiry race, physical device, full security or
enterprise acceptance. BUG-API-0002 remains OPEN. Any necessary repair reuses the
existing explicit action-level Identity/domain save-and-audit transaction.
