# Live PENTA enrollment pause and transfer — 15 November 2026

Desktop base `fc4e1c1360a42789d336634443d666d748f2f5b4`.
Fourteen enrollment paths match Cloud C41
`1b0b967b8f31d156ce2324abfc8d968e7f72ed83`, which includes C40.
The Desktop workspace frame recognizes the enrollment management route.

In Testing, the enrollment assistant links to the protected status and
transfer page. It searches scoped enrollment records, presents the exact
action, requires separate approval and executes on explicit confirmation.
After a transport interruption, the browser checks the same request by GET.

The first live transfer reached the real batch search and received HTTP 200.
The browser correctly refused the response because it lacked the no-store
header required by the lifecycle client. Batch search now returns this
header while retaining its scope, response shape and audit. The live test
checks it before selecting the target batch.

## Verification

- 306 targeted frontend and role-entry checks passed. The locked Next.js
  build, types, static export and targeted lint passed.
- API and harness builds passed. 181 focused enrollment API tests passed.
- 124 retained native enrollment case groups passed, including 15 create
  browser host groups and the existing Manual/service contention cases.
- Four live groups passed using real Identity and disposable SQL. An
  Academy Admin paused an enrollment at 390 px with the exact date and
  reason. An Owner transferred a learner at 1440 px.
- The transfer request was interrupted before dispatch. Reload recovered
  the authorized draft with GET. An explicit same-key retry committed and
  its response was dropped. Reload recovered the original receipt by GET.
- SQL verified one pause on the original row, one transfer preserving the
  source and creating one Active target, and one audit for each transition.

Owned SQL run `e4c6795a936e466c950ff75ff711ad3f` in window `ad8c8299a6344a9cb642e325dbaf22d4`.
The runner removed its owned SQL resources, restored both local PENTA
services and confirmed HTTP 200 with the original Docker inventory.

The live browser scope covers Paused and Transfer. The other lifecycle
states, broader lifecycle-only native authority and race cases, physical
devices, and production acceptance remain open. The C43 scheduling host
is a separate Cloud return. The release checker still reports six blocked
records, and private model qualification and measurement of the 90% outcome target remain open.
