# Private material preview/request scope — 25 October 2026

Existing BUG-SEC-0001 private-media recovery/E1–E3 continuation. This is a client
scope/lifecycle defect, not evidence of bypassing server media authorization.
Only application diff is `apps/web/src/components/private-material-actions.tsx`.
No backend, tenant policy, MIME/size limit, ticket contract or upload behavior change.

## Confirmed reproduction

The component was reused when its url prop changed. Cleanup revoked the object
URL and aborted the request, but state still referenced the previous preview and
notice; returning to the earlier URL could resurrect a revoked preview. A late
HEAD response could continue toward ticket preparation, and late catch/finally
could publish into the reused view or unlock a different pending operation.

Nine actual-TSX hook/lifecycle cases, controlled async boundaries and object URL/
native-target tracking: settled baseline1PASS/8FAIL, repair9/9PASS.
`QA/EVIDENCE/private-material-scope-baseline-settled-20261025.tap` uses the
exact hash-verified pre-repair source copied in accepted build
`caa9cdd48b6a43b7abb96d81f768124e`, preserving all earlier legitimate work.
The earlier initial-baseline file2PASS/7FAIL did not settle the void event
handler's microtasks consistently; retain it as incomplete QA diagnostics, not
the accepted reproduction. Fixed harness waits a scheduler turn for both old and
repaired source, preserving every assertion.

## Repair

Keep the public component API; its internal material view is keyed by URL, so a
different file receives fresh preview/message/busy state. Cleanup aborts and
releases the request reference, revokes exactly the live object URL and removes
native download targets. After HEAD, check cancellation/operation identity before
preparing bytes or a ticket. Catch/finally publish only while they still own the
operation. A cancelled same-scope action still gives its existing understandable
message. Late unmounted responses cannot clear a replacement operation's lock.

Existing server retrieval authorization remains authoritative. This does not add
push invalidation for already-displayed previews when permissions change, certify
all codecs/devices, or make a native handoff a confirmed completed disk download.

## Bounded validation accepted

- [x]Nine focused actual-source cases PASS, including A→B→A, old delayed HEAD,
  replacement operation, explicit cancel, unmount and exact resource cleanup.
- [x]Combined75retained media/safe-preview/image/guardian-selection cases PASS,
  zero failures/skips. `QA/EVIDENCE/private-material-scope-retained-20261025.tap`.
- [x]Window `c1a2bc21a77a4f6c83eb007cea7a9085` PASS: UI
  `f1d14fe30c3f47a19bd84f674bcd0086`, web build
  `d5078c90935248fcb42b462b1831ddbd`;75handlers/typecheck/lint/627files
  and eight actual Student portal presentation journeys320/390/768/1440
  light-dark using synthetic protected-media reads. Preview/close, pending HEAD
  unmount, no late byte request, fresh preview, exact object URL cleanup/no stale
  notice and no write/page error/overflow assertions PASS. Browser
  `270eceb1d9cb45b9a7c4760b5af32660`; all eight PNGs inspected. The visible
  white square is the synthetic one-pixel image, not a customer image/codec test.
- [x]`node QA/tools/material-scope-qualification.cjs` PASS: exact current source/
  export/browser/resources; original service IDs/images restored, inference
  healthy/API health200/full inventory/lease release PASS. Browser SHA256
  `527e9c8a8f7dbd166c096088a12d4aa0c3eb0e1ffc5bf1d67c512d371e68efd7`;
  window SHA256
  `4422e7d0f17d7b58dfe2689fc250ef13719740690c6ea09137dc3afe3f5d2c7f`.

These controlled browser reads/token/bytes do not qualify SQL, real RBAC/tickets,
physical devices or all Teacher/Guardian consumers. Latest native grant/revoke
53HTTP/four journeys/two audit faults/eight audits remains bound to its preceding
shared-control export. No blanket current whole-application acceptance inferred.
BUG-SEC-0001 and E0–E10 remain OPEN; Medium fits this settled lifecycle repair,
High for real security/SQL/model integration. No main/Mini/GitHub/Azure changes.
