# Interactive synthetic academy demo — 23 November 2026

## D57 current demo — 23 November 2026

Current run is `e13e72728f55494ab957e7e04d22182b`. Open
http://127.0.0.1:3000/login. The generated password and six accounts are in
`D:\AcademyDesk-penta-host\.build-check\interactive-demo-e13e72728f55494ab957e7e04d22182b\LOGIN.txt`.
Use owner@demo.example.invalid for the complete academy workspace; never publish
the password or reuse a stopped run's file.

- [x] Current-source six-role native permission packet69/69: finance and tenant
  boundaries, Platform Owner restrictions, linked-family access/downloads,
  fixed Guardian children list and Teacher period validation. No429; unchanged
  ledger/Mini state. Read-only packet, not a browser/device or model test.
- [x] Thirteen real-model/Identity/SQL demo checks passed again: six identities,
  pending fees, contextual Piano follow-up, exact INR2250.00, GET recovery and
  Manual optional-fields-empty save. First local query29.422seconds.
- [x] Eleven Core-outage/recovery/denial/Manual checks passed; original endpoint/
  inference start time/resource inventory preserved. Two invented Manual saves
  leave14learners. Guardian list500 is fixed on this backend.
- [ ] Exact-candidate hosted acceptance, devices/staging/private archives and
  qualified production AI/90%remain open. No universal conversational CRUD claim.

Expiry is24November2026 at06:04IST, or earlier on shutdown/low-RAM safety stop.
Use the same startup/stop instructions below and ACTIVE_DEMO to obtain the latest
private login path after any restart. Do not run heavy tests beside the demo.
Earlier fixtures and failures remain private, including the preceding synthetic
database/key-ring backup. This section supersedes all older run IDs and expiry
statements below.

Read-only portal check (existing synthetic fixture only):

~~~powershell
$env:QA_INTERACTIVE_DEMO_PERMISSIONS = '1'
node QA/tools/interactive-demo-permissions.cjs
Remove-Item Env:QA_INTERACTIVE_DEMO_PERMISSIONS
~~~

[Source-bound D57 acceptance](../REPORTS/D57_PORTAL_AND_PREFLIGHT_ACCEPTANCE_2026-11-23.md)

## D56 live demo — current instructions

Current run: `12994a6ab57b40f2b333f6efc554f2ba`, restored on23November2026.
Open http://127.0.0.1:3000/login on this development laptop.
Use `owner@demo.example.invalid` for the complete academy workspace; the
generated password and all six test accounts are in the private local file
`D:\AcademyDesk-penta-host\.build-check\interactive-demo-12994a6ab57b40f2b333f6efc554f2ba\LOGIN.txt`.
Do not commit it, paste its password into chat, or reuse an older run's file.

- [x] Six real Identity/session roles, nine pending-fee learners, two Piano
  follow-up rows, exact INR2250.00, GET receipt recovery and Manual optional-empty
  save passed. First measured local query26.242seconds.
- [x] Eleven additional recovery/outage assertions passed: owned Core pause,
  authoritative unavailable status, same GET-only receipt, unauthorized actor/
  tenant denials, persisted Manual save, unchanged ledger/turn count and stable
  Core bindings/inference start time/resource inventory after unpause.
- [x] Two synthetic Manual saves leave14learners with four teachers/courses/
  batches, two branches, invoices/payments and scheduled classes.
- [ ] This is not unrestricted conversational CRUD, all-role Mini qualification,
  a fresh physical-device browser test, production configuration or90%coverage.
  Mini's free-prompt read workflow is currently Owner/Admin-only.

### Start, use and stop

The current demo is already running; do not launch a second window. If it has
stopped, first confirm the original two PENTA services are restored and the global
validation lease is free. Use the existing D:-only cached setup and local helper:

~~~powershell
Set-Location D:\AcademyDesk-penta-host
$env:QA_DEMO_REUSE_WEB = '1'
node .build-check/start-interactive-demo.cjs
~~~

That private helper uses already-installed dependencies, the pinned local2B model,
the existing lease and opt-in disposable Identity/SQL fixture. It is a
laptop-specific test launcher, not an Azure production recipe. No new package or
weight download is part of these instructions. Read the new ACTIVE_DEMO pointer
after a restart; credentials and disposable IDs change. Open the private file
locally with Notepad; never expose its contents in a published handoff.

~~~powershell
$demoState = Get-Content .build-check/ACTIVE_DEMO.json | ConvertFrom-Json
notepad $demoState.loginFile
~~~

Try “Show students with pending fees”, then “Only piano”; a new conversation can
look up “Show Riya Menon”. Use the Manual switch for Students, Teachers, Batches,
Calendar and Finance. Teacher/Student/Guardian/Platform Owner have real linked
identities; they are not authorized to use the Owner financial Mini conversation.

The new recovery tool needs an existing unexpired Owner receipt. It sends no model
request, pauses only the exact owned Core API and preserves the loaded inference
container. It adds one invented learner to prove Manual save during downtime:

~~~powershell
$env:QA_INTERACTIVE_DEMO_RECOVERY = '1'
node QA/tools/interactive-demo-recovery.cjs
Remove-Item Env:QA_INTERACTIVE_DEMO_RECOVERY
~~~

Do not rerun the original private seed verifier after editing data; it deliberately
expects the fresh12-learner fixture. Use its retained result or the recovery tool.

Current expiry: **24November2026 at03:43IST**, earlier on shutdown or unrecoverable
low RAM. Keep heavy builds/suites out of this window. The3GiBadmission and512MiB
safety guards remain unchanged. For graceful stop:

~~~powershell
$demoState = Get-Content .build-check/ACTIVE_DEMO.json | ConvertFrom-Json
Set-Content -LiteralPath $demoState.stop -Value 'Owner finished synthetic demo'
~~~

Wait for cleanup and restoration of the exact two original services. The final
demo is intentionally still active; its final shutdown/restoration is not yet a
completed check. The earlier dynamic-port restoration failure and stop-file cleanup
race are retained in the D56 report, including a private SQL/key-ring backup of
the affected synthetic fixture. Never manually prune images/volumes or erase
evidence to stop the demo. Evaluation remains default-off outside this setup.

[Source-bound results and blockers](../REPORTS/D56_DESKTOP_RELEASE_READINESS_2026-11-23.md)

## Historical demo sections below

The following run IDs, credentials, expiry times and pending-work statements are
historical. Use the current section and ACTIVE_DEMO above.

## Current restored demo

The active run is 00e3a635dfdc49db936783a668306fc8, restored after the earlier demo's RAM safety stop. Current credentials are in D:\AcademyDesk-penta-host\.build-check\interactive-demo-00e3a635dfdc49db936783a668306fc8\LOGIN.txt; older login files belong to stopped disposable runs. The local URL remains http://127.0.0.1:3000/login. Use owner@demo.example.invalid for the full academy workspace.

All13 live checks passed on the C55 application: six role logins, seeded records, nine pending-fee learners, two-row contextual Piano refinement, GET recovery, the host's exact INR2250.00 string, and a persisted Manual learner save. The first measured conversation took20.9seconds locally. The same fixture contains thirteen learners after verification, four teachers/courses/batches, two branches and the financial/classroom data described below. The UI now presents versioned exact balance strings rather than requiring the older numeric-only fallback.

This run expires automatically at 23 November 2026 at 11:06 pm IST, or earlier on shutdown/unrecoverable low RAM. No more heavy local regression suites run while it is available. Its live-model checks retain planner0.15 at4732116; the reviewed Core/CI reconciliation at2686d90 changes portability/provenance only, preserving all serving code. Current verification SHA256: fea4c21e902a7525ee90549111fc058fa9a8a47a6ad534647278cb746deb924d.

The earlier run details below are historical. These current credentials/status supersede them.


## Open the application

Local URL: http://127.0.0.1:3000/login

Use owner@demo.example.invalid for the complete academy workspace. The generated password and all six logins are in the private local file D:\AcademyDesk-penta-host\.build-check\interactive-demo-591c2d63be174cdcb370a877c8b7adb9\LOGIN.txt. Credentials, bearer tokens and private raw receipts stay out of Git. This loopback URL is available on the current development laptop only.

All six real Identity logins and authoritative session profiles passed. Owner/Admin land in the Academy workspace, Teacher in Teacher, Student/Guardian in Portal, and Platform Owner in Platform. The visible browser tab is on the login screen with the owner user name prepared. Use PENTA AI first, then the Manual switch for normal records and forms. Light/dark controls remain available.

## Data and things to try

The isolated SQL database contains Lotus Learning Academy — Demo, two branches, four teachers, four subjects/courses, four batches, twelve enrolled learners, twelve invoices with paid/partial/unpaid examples, reconciled payments, four scheduled classes, a guardian link, a fee plan and an expense. The Manual verification saved a thirteenth learner named Demo Manual Save with optional fields empty. All identities and records are invented; no customer import occurs.

Try these in the Private Mini conversation on the Owner/Admin PENTA screen:

1. Start a private conversation and enter “Show students with pending fees.”
2. Enter “Only piano.” as a follow-up.
3. Start a new conversation for “Show Riya Menon.”
4. Ask “Who has the highest outstanding balance?” and open the authoritative Manual learner/Finance links.

The actual demo run returned nine pending-fee learners and then the two eligible Piano learners. GET recovered the exact stored response. One real-model turn took 34.3 seconds on the bounded local CPU profile; this is a measured local result, not a production latency guarantee. Twelve API checks passed overall, including all six role logins, seeded records, model reads/recovery and a persisted Manual student creation. The synthetic browser matrix separately passed118 enabled and4 default-off cases.

Manual exploration includes Students, Teachers, Batches, Calendar and Finance. Student/Guardian accounts are linked to the first learner and the Teacher to the Piano batch. The Platform Owner can inspect the demo academy. Manual domain authorization, validation, transactions and audit still run through the actual backend.

The free-prompt Mini composer currently supports read-only learner/fee lookup and follow-up. Protected assistants expose the existing enrolled/scheduling/finance flows where available. This demo does not establish universal free-form CRUD, every portal capability, live communications/private Azure media, model production qualification or the90%target. Those gaps remain visible; unavailable actions must not claim success.

## Lifecycle and safeguards

The current run is 591c2d63be174cdcb370a877c8b7adb9, using SQL run ffce7c4b-1bb4-416f-ab73-ce643b8e0af0. Mini source is 4732116bc9a8c4eb9904ee7c4fc09100866816f6, planner0.15 with the already cached pinned Qwen3.5-2B Q4_0 and protocol0.1. Inference is on an internal Docker network; the host/provider listener, SQL and application bind loopback. No weight/package download, training, model promotion, external paid compute or Azure use.

Only three demo containers run: owned SQL, private inference and its Core API. The two original PENTA containers are preserved and paused during the demo, then restored on shutdown. Thirty-two old stopped labelled unmounted QA SQL containers were removed, plus the owned failed-start demo SQL instance. The older media container and its named volume remain preserved. No image/volume/global prune occurred.

The original3GiBadmission and512MiBsafety guards remain. Early runs exposed a file-creation logging race and low RAM. The supervisor now tolerates log creation, releases WSL file cache before a low-memory shutdown, and the demo avoids constructing a duplicate API host. Eight orphaned processes belonging to the failed, precisely identified QA Edge profile were closed. Every earlier failure receipt remains local.

The current demo automatically expires at 23 November 2026 at 10:14 pm IST, or earlier if the laptop shuts down or the unchanged safety guard cannot recover. All demo changes are disposable. To stop gracefully, create the stop file named in .build-check/ACTIVE_DEMO.json. The supervisor shuts down the API/frontend, lets the guarded runner remove its exact database/login/container, removes its two owned model containers/network, and restores the original two services. Preserve raw evidence.

The current local launcher is .build-check/start-interactive-demo.cjs, using the protected helper in D:/AcademyDesk-codex-p0/QA/tools/local-validation-lease.cjs and the already installed export dependencies under D:/PENTA-Models/operations/penta-next-audit-clean-20261117. Run one validation/demo window at a time. Do not treat this laptop-specific launcher as a production deployment recipe. QA/tools/SqlHarness/InteractiveDemo.cs and the opt-in QA_INTERACTIVE_DEMO hook contain the portable application fixture/bridge source. Never set that switch on a production host.

Verification receipt SHA256: 883a31801c4d81e1a3fae2a775f0f0186c1dec9e1cd2a793b0b20acd09b2d046.

## Remaining launch path

- [x] C54 code/native security/browser integration at69a5794 is published and verified on the feature branch.
- [x] Real local Mini conversation, all six logins and a persisted Manual save now work in the interactive demo.
- [ ] Continue C55 exact-money contract with its Cloud owner; the Cloud branch currently contains only its claim.
- [ ] Expand and measure real conversation outcomes across eligible operations and roles;90%remains unmeasured.
- [ ] Resolve private Core Actions access, complete exact-release CI/dependency audit, Mini archive/platform/hash full-suite failures.
- [ ] Finish the six existing critical acceptance records, physical devices and staging restore/private Blob/rollback before Azure.

No main merge or Azure deployment. Next engineering route is Sol High for model/host capability coverage and release blockers. Sol Medium can maintain demo/help and routine implementation after the security boundaries are agreed.
