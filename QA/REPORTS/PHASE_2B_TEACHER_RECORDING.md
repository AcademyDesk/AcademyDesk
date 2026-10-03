# Phase 2B — Teacher recording and microphone waiting recovery

2026-10-01. **PARTIAL PASS; real microphone-denial case pending.** Local recording start/pause/resume/stop, selected playback, upload/reset/history and saved authenticated preview passed. No Azure access/deployment, commit/push or development database change. No Phase 2B/security/media/financial/critical/release closure. Continue agreed Sol High allocation; accepted Astra audits are not repeated.

## Reproduced gap and local repair

The actual local page remained on enabled Record audio/file controls without feedback while browser microphone permission was pending. Explicit hook/media doubles also reproduced an enabled Confirm upload for a previously selected file during a pending request (8/9 tests PASS, one expected regression FAIL). That allows an upload to compete with later microphone grant and file replacement. New requestingMicrophone state now disables conflicting picker/remove/clear/submit/record actions, shows live waiting guidance, and resets on request settlement. Ref guards prevent synchronous competing file/submit/clear handlers. Decline keeps the selected file and metadata; late permission grant after unmount still releases tracks. No permission bypass, automatic upload or server auth change.

Initial new test harness had a tree-traversal recursion bug; corrected before valid before-fix results. The first backend launch mistakenly used Windows PowerShell 5, which lacks the static random-byte API; it failed before any container provisioning. Relaunch with installed pwsh succeeded. Initial source-copy command used the wrong working-directory-relative path; corrected absolute paths matched current source SHA. These infrastructure mistakes are not counted as application failures.

## Executed evidence

| Check | Result / boundary |
| --- | --- |
| Real permission waiting UI | PASS: actual live status, disabled Record/picker/submit, synthetic title retained |
| Real microphone recording | PASS: user approved local access; actual recording, pause, resume, stop and reviewable WebM |
| Selected preview | PASS: actual native play/pause; currentTime 8.257724, readyState 4, no media error |
| Confirm upload / blank optional comment | PASS: POST create 201, one chunk 200, completion 200, success notice, fields reset and history item |
| Saved private preview | PASS for authorized HEAD/GET 200, readyState 4, native playback starts, no media error; elapsed saved-media playback not independently sampled |
| SQL fresh read-only assertions | PASS: one completed session, one resource and one Student notification; original Teacher/batch, nonempty under-32-MiB WebM and blank optional comment |
| Real permission denial | PENDING: user was asked to block access; no verified denial reply/UI yet. Not counted as passed |
| Explicit denial/lifecycle cases | PASS with actual component handlers but React-hook/media/storage doubles: denial retaining file/title, retry, MIME/extension, pause/resume/stop, empty recording, unsupported device, constructor/start failure, active unmount, late grant release |

First real sample was stopped and removed before upload because permission waiting/user interruption lengthened it. Only the second bounded local sample was saved in disposable private QA storage. No raw microphone content is preserved in reports/screenshots. Actual elapsed recording duration was not instrumented. WebM magic bytes select the existing video-preview control even for audio-only recording; it decoded and played. Failed audio-selector lookup was corrected to the actual rendered video element, not treated as a product defect or audio/video classification acceptance.

**41/41 targeted tests PASS** (28 media/client + 4 Teacher workspace + 9 recording handlers). TypeScript compiler exit 0, no diagnostics. Uploader lint exit 0: 0 errors / 1 existing no-img-element warning; no full lint/build/critical suite certification. Current narrow dev-console error/warn sample was empty; not exhaustive console certification.

## Reproducibility and evidence

Fresh owned run `909341087be143c8ace7ab41f88ab954`, UTC start `2026-10-01T04:37:28.7943454Z`, SQL port 54721, source-only frontend `.build-check/browser-web-515f340911614ee58454b36e0d6a75e7` on loopback 49001 and real Identity/API/SQL/private Azurite bridge 49002. Exact guarded tenant controls PASS; route inventory 307 / controller methods 296 / framework Identity 10. Normal laptop development app/DB not replaced. QA copy/current uploader hashes match. Browser ID changed across reconnection; existing matching QA tab was rebound, not a new identity/context assertion.

[58-record snapshot](PHASE_2B_TEACHER_RECORDING_SOURCE_SNAPSHOT.json): previous 56 captured sources differ only in uploader; new recording handler test and read-only SQL verifier added. HEAD remains `20bb6047f9edf733ac8e2a226621cc582ec54b3c`. Earlier media/security runtime passes remain historical, not recounted on changed frontend.

Evidence: [before-fix regression](../EVIDENCE/logs/phase-2b-recording-tests-before.log), [41 tests](../EVIDENCE/logs/phase-2b-recording-tests.log), [typecheck](../EVIDENCE/logs/phase-2b-recording-typecheck.log), [lint](../EVIDENCE/logs/phase-2b-recording-lint.log), [SQL assertions](../EVIDENCE/logs/phase-2b-recording-sql.log), [selected actual DOM/media observations](../EVIDENCE/logs/phase-2b-recording-dom.log), [selected HTTP/cleanup log](../EVIDENCE/logs/phase-2b-recording-http.log), [validation](../EVIDENCE/logs/phase-2b-recording-validation.log), [waiting screenshot](../EVIDENCE/screenshots/phase-2b-recording-permission-wait.png), [saved preview screenshot](../EVIDENCE/screenshots/phase-2b-recording-saved-playback.png). Screenshots are functional samples, not approved golden/mobile/physical-device baselines.

## Cleanup and next

Synthetic sign-out verified; owned tab closed. Current owned cleanup PASS: private Blob container (including recorded sample), disposable SQL DB/runtime login, run root and labelled SQL/Azurite containers removed; current QA listeners absent. Backend exit 0 / 659.4 seconds, final SQL completed session length 265101 bytes. Exact Next child 12532/parent 17556 intentionally stopped; launcher exit 1 records that termination, not app failure. Recorded disposable sample is not recoverable; no customer data removed. Source copy/evidence retained. Older interrupted `d6dfd31d64c146789abbf686bccb920a` stopped containers are not targets of this run; refused historical cleanup is not retried.

Next bounded task: complete the real microphone-block/denial and recovery case before marking this recording slice fully passed. Agreed **Sol High**. Actual Android/iOS, other recording containers/devices, 2 GB uploads, wider recorder failure/long-duration limits, full critical suite, financial P0/policy and release gates remain pending. No universal format or unlimited recording guarantee.
