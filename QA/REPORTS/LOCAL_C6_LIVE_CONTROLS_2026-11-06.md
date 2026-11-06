# C6 live finance and Teacher controls — local acceptance

Status: CLAIMED before implementation, 6 November 2026. Base
`fd958a3b847b9869ed1dc8ef2a3fc70b90344fad`; continue the existing C6 handoff.

Scope: one bounded existing-harness module for real finance decision and Teacher
assigned-batch HTTP/SQL acceptance, followed by the current built UI against a
fresh loopback host using real Identity and native responses. Preserve existing
accepted tests, source-bound receipts, finance policies and original dirty trees.

Allowed QA paths: new ManualControls regression/browser host and browser client,
SqlHarness Program/runner registration, this report, related issue records and
shared continuation pointers. Reproduce any actual application defect before a
minimal repair; run relevant API regression after an application change. Do not
rewrite finance, Teacher, auth, AI or frontend systems speculatively.

Verify exact saved values/rows, invoice balance, publication/nullable dates,
assigned/foreign batch authority, canceled decisions sending no mutation, required
rejection notes, success/reset versus denial/unconfirmed retention, audit and
rollback boundaries. Browser interception may deliberately drop an actual server
response for the unconfirmed-outcome case; it must never fabricate a successful
API response. No injected browser session tokens or synthetic backend acceptance.

Keep both original PENTA service identities/images restored, 3 GiB admission guard,
owned SQL cleanup and raw local evidence. No new package/image/model download,
candidate model loading/training/activation, production DB, main or Azure change.
Both C6 issues stay OPEN until their remaining critical/device/release protocols
pass. Route: Sol High for this real authorization/financial integration.
