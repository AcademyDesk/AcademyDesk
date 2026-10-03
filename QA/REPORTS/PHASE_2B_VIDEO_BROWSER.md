# Phase 2B — real portal MP4 upload, playback and download

2026-09-30. **PASS for the bounded synthetic MP4 scenario below.** Continues the accepted QA sequence without repeating Astra source audits. Phase 2B and BUG-SEC-0001 remain OPEN / FIX-IN-PROGRESS / PARTIAL PASS. No application source edits, commit/push, deployment or development/Azure database changes.

## Run and evidence

Run `920e8f8104364ff8b58eb03b0c05c9ba`, UTC start `2026-09-30T18:09:49.1548781Z`, SQL loopback port 59903, guarded harness exit 0 after 249.7 seconds. Real Program/TestServer/Identity/SQL/private Azurite behind the existing loopback bridge. Tenant-isolation startup controls passed. Migration histories 80 application / 7 Identity unchanged. Actual Next source-only copy `.build-check/browser-web-ebb3e11ee22e49acb9a0e5ab0ea8eb2e`, web/API 49001/49002; no `.env` or normal dev-server replacement.

Synthetic test-pattern MP4: four seconds, 320x180, 24 fps, H.264/yuv420p, silent, **17,730 bytes**. SHA-256 `8aa1d0504970101ad67c7cb16ae29aedef574de28d18e403ac719177a7695fa8`. Generated through [the QA-only fixture tool](../tools/create-browser-video-fixture.cjs) using pinned npm-registry `@ffmpeg-installer/win32-x64@4.1.0` installed with scripts disabled under `.build-check/media-tools`; no application package/dependency edits. Generator refuses overwriting an existing fixture. Encoder hash and recipe retained in observations/tool; this is not a fake video header or customer recording.

[HTTP/SQL/cleanup evidence](../EVIDENCE/logs/phase-2b-video-http.log), [browser observations and integrity](../EVIDENCE/logs/phase-2b-video-observations.log), [frontend lifecycle](../EVIDENCE/logs/phase-2b-video-frontend.log), [49-record source snapshot](PHASE_2B_VIDEO_BROWSER_SOURCE_SNAPSHOT.json). All previous 48 records match the resume snapshot; only the new QA generator is added. Historical manifests are unchanged.

## Observed checks

| Check | Observed result |
| --- | --- |
| Teacher selected-file preview | Decoded 320x180, duration 4, readyState 4, no media error; native Play advanced from 0 to completion at 4 seconds |
| Upload/save/reset | Create 201, chunk PUT 200, complete 200; visible `Class material uploaded successfully.`; file/title/comment cleared; history retained title/comment |
| Teacher saved private preview | Authorized HEAD/GET 200; decoded same dimensions/duration; native Play advanced and ended without media error |
| Teacher native download | Browser completed original MP4; independently hashed length/bytes exactly match source |
| Authorized Student Library | Saved title visible; private HEAD/GET 200; decoded same dimensions/duration; native Play advanced and ended without media error |
| SQL completion observation | One completed upload session with expected ID `5faa650c-e390-4974-9626-af26c628ab4d`, length and filename; not an additional fresh-context resource-count assertion |
| Console samples | Teacher and Student error/warning samples `[]`; not exhaustive console certification |

Normal UI login/sign-out and file chooser used. No browser credentials, cookies or storage inspected/injected. Playback started through actual native video controls, not script-injected `play()`. Read-only DOM properties recorded decode and playback progression. Screenshots: [selected preview](../EVIDENCE/screenshots/phase-2b-video-selected.png), [saved Teacher playback](../EVIDENCE/screenshots/phase-2b-video-saved-playback.png), [Student playback](../EVIDENCE/screenshots/phase-2b-video-student-playback.png). Desktop Chromium at approximately 590x656; not actual phone acceptance or approved visual baselines.

## Automation observations and cleanup

Initial platform-package lookup was corrected using npm installer metadata. Initial tab navigation timed out during page compilation, then the same tab recovered. An assumed success-message selector did not match; fresh AX showed the actual success text above. These are retained automation observations, not product failures.

Teacher and Student signed out normally and the temporary tab closed. Exact owned stop marker disposed host, deleted the owned private Blob container, removed disposable database/runtime login and matching labelled SQL/Azurite containers. Verified no run containers, QA listeners or owned host root remain. These disposable contents are not recoverable. Matched Next child/parent PIDs stopped intentionally; launcher exit 1 reflects that termination, not application failure. Source copies, encoder, fixture and verified download remain recoverable on D:; no customer files deleted.

## Limits and next task

SECURITY-FILE-001 receives supplemental positive video-path evidence, not security issue closure. Existing unit/API/protocol/legacy suites were not rerun or recounted. TEACHERPORTAL-UPLOAD-001 failure-recovery case is unchanged: a successful video upload does not prove its original HTTP-rejection/network-failure scenario.

Not tested here: every codec/container, audio in video, camera/microphone recording, guardian video, Student download, >32 MiB preview/download behavior, 2 GB transfer, actual Android/iOS, network loss/expiry/revocation/shared-device cleanup, critical suite or production Blob configuration. The four financial P0 issues and their unresolved policies remain open; release readiness is not implied.

Next bounded task: a real-portal synthetic file above the 32 MiB inline-preview limit, checking clear download fallback and exact native-download integrity. Later acceptance still needs recording, larger-transfer/recovery and actual-device checks. Continue the agreed **Sol High** allocation; use Astra only for focused security/policy exceptions, not repeating accepted audits.
