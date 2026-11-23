# Interactive synthetic academy demo — 23 November 2026

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
