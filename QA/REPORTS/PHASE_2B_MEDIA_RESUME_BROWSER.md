# Phase 2B — real portal upload pause/reload/resume

2026-09-30. **PASS for this bounded synthetic desktop-browser recovery scenario only.** This continues the [blocked attempt](PHASE_2B_MEDIA_RESUME_ATTEMPT.md) without repeating accepted Astra audits. Phase 2B and BUG-SEC-0001 remain OPEN / FIX-IN-PROGRESS / PARTIAL PASS. No application edits, commit/push, Azure deployment or development/Azure database changes.

## Actual run and isolation

- Run `008270ae08914a0181003c0d60606576`, UTC start `2026-09-30T17:48:40.7778413Z`; harness lifecycle 743.8 seconds, exit 0. Owned SQL database/runtime login on loopback port 64673, real Identity and private Azurite behind the guarded forwarding bridge; tenant-isolation startup controls passed. Migration histories 80 application / 7 Identity unchanged.
- Copied actual Next Teacher portal at web/API 49001/49002. Source-only frontend copy `.build-check/browser-web-e7723f8761894d67bb2baf75c7511f20`; dependencies reused, no `.env` copy and no replacement of normal development servers.
- Inert synthetic original `synthetic-resume.bin`: 17,825,792 bytes, three chunks; SHA-256 `9f4be0e976dfd2921de0b28a22ec87d887100b6d4af3301339ef7a51629ee393`. Not video, recording or a 2 GB transfer.
- Normal UI login as the seeded disposable Teacher. No browser cookies/local storage/account tokens inspected or injected. The application restored its own draft through the UI.
- A one-shot 30-second hold before forwarding chunk index 1 made interruption reproducible. This is test-only transport pacing, not a real mobile network-loss certification. All API responses/storage/authentication were real.

[HTTP and cleanup output](../EVIDENCE/logs/phase-2b-media-resume-http.log); [observations/integrity](../EVIDENCE/logs/phase-2b-media-resume-observations.log). The 48-record [run source snapshot](PHASE_2B_MEDIA_RESUME_BROWSER_SOURCE_SNAPSHOT.json) matches all current files. Historical snapshots were not rewritten. All 37 application/test records from the prior portal-smoke snapshot remain unchanged.

## Observed results

| Check | Result |
| --- | --- |
| Start upload with title/comment | Create 201, progress 200, first 8 MiB chunk 200; visible progress 47% |
| Pause during held second chunk | UI says upload paused and retains original file/draft; interrupted second request eventually logged 499 |
| Actual page reload and reselect same class | Saved draft recovered; original title `Resume QA lesson` and comment `Synthetic pause/reload recovery` retained, fields locked; Resume disabled until original file reselected |
| Choose different synthetic file and submit | Explicit original-file/reselect message; draft retained; no additional create request for that wrong-file attempt |
| Reselect original and Resume | Create 200 reuses exact upload ID; progress 200; first chunk skipped (only one chunk-0 PUT across entire run), chunks 1 and 2 uploaded 200; one completion 200 |
| Success/reset/history | Success message visible, file/title/comment cleared, no draft controls, history shows original title/comment and one attachment |
| Native download | Browser completed original file; independently hashed 17,825,792 bytes match exact original SHA-256 |
| Fresh read-only SQL assertions | Ownership marker matches exact run; exactly one total upload session; same ID completed with expected length/title/comment; exactly one final LearningResource with that ID |
| Console sample | Error/warning log sample returned `[]`; not exhaustive application-console certification |

SQL assertions were performed only inside the exact run-labelled SQL container against its named disposable database with ownership check, using the container's existing ephemeral credential without printing it. No production/development queries.

Evidence screenshots: [paused at 47%](../EVIDENCE/screenshots/phase-2b-media-paused.png), [draft after reload](../EVIDENCE/screenshots/phase-2b-media-draft-reloaded.png), [resumed success](../EVIDENCE/screenshots/phase-2b-media-resumed-success.png), [native download requested](../EVIDENCE/screenshots/phase-2b-media-resumed-download.png). Screenshots are evidence, not approved visual baselines or actual Android/iOS proof.

## Recovery observations, not hidden failures

C: capacity initially measured ~1.14 GiB, then rose to 2,873,352,192 bytes before the existing 2 GiB guard allowed startup. No personal files were deleted to make room. A diagnostic wrapper had expected refusal; its assumption became stale as capacity recovered, so after successful harness cleanup the wrapper threw `Unexpected provisioning attempt` (exit 1). The actual harness exit was 0; retain the wrapper error separately rather than equating it with a product failure.

Browser control was available for pause/reload, then its connection changed at the next user continuation. The original tab was recovered through the currently provided browser-control interface; draft and teaching scope were preserved. One AX Resume activation produced no observed request and a success-selector wait timed out; fresh UI inspection and direct visible-button locator activation subsequently completed normally. This is retained automation evidence, not a claimed app defect. No fresh account session or fabricated API result was substituted.

## Cleanup and next bounded task

Normal UI sign-out returned to the login page; temporary QA tab closed. Exact owned stop marker disposed the real host; guarded Blob container, SQL database/runtime login, host folders and labelled SQL/Azurite containers removed. Disposable contents are not recoverable. Owned Next child/parent stopped after matching loopback listener/PIDs/command line. Verified no remaining run containers, QA listeners or host root. Source copies and synthetic fixtures/download remain recoverable on D:; download moved there only after hash match, with no overwrite.

Permanent coverage references: SECURITY-FILE-001 and TEACHERPORTAL-UPLOAD-001 receive partial recovery evidence, not full PASS or issue closure. HTTP rejection, arbitrary network loss, session-expiry/revocation during upload, shared-device draft cleanup, ambiguous completion, video, recording, >32 MiB preview/2 GB transfer, actual phones and critical/release gates remain pending. No previous 85 unit / 22 client / 75 HTTP / 29 legacy tests rerun or counted as new checks here.

Next: one bounded real-portal video upload/preview/download check, then larger-file and device-specific acceptance. Continue the agreed Sol High allocation; financial/security issue closure still requires all recorded gates and unresolved policies.
