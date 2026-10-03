# Phase 2B — upload pause/resume browser attempt

2026-09-30. **BLOCKED / UI NOT RUN**, not a product failure or functional PASS. Prior accepted Astra audit and [real portal smoke](PHASE_2B_PORTAL_MEDIA_BROWSER_SMOKE.md) remain historical evidence. No application edits, commit/push, Azure deployment or development/Azure database mutation.

## Prepared test infrastructure

- Added opt-in test-bridge pacing: `Run-BlobMedia.ps1 -Browser -BrowserPauseChunkMs 30000` holds the first PUT of chunk index 1 before real API forwarding. One-shot, cancellation-aware, 1000–30000 ms bounds; no fabricated API replies, authentication bypass or production pacing.
- Generated inert `synthetic-resume.bin`, 17 MiB / 17,825,792 bytes / three chunks. SHA-256 `9f4be0e976dfd2921de0b28a22ec87d887100b6d4af3301339ef7a51629ee393`. Existing small fixtures unchanged.
- Harness build passed with zero errors/warnings. Three invalid pacing inputs rejected before provisioning. Argument guards now precede the unchanged 2 GiB C: capacity preflight. These are infrastructure checks, not upload tests.

## Interrupted attempt and cleanup

Owned run `8bdd48e8fa674fe2ba0706d3b955779e`, UTC start `2026-09-30T17:37:21.7985371Z`, SQL loopback port 54526; frontend/API 49001/49002. Real Identity/SQL tenant-isolation controls and guarded fixture startup passed. [Backend/cleanup output](../EVIDENCE/logs/phase-2b-media-resume-attempt-http.log).

The source-only frontend launcher ran successfully using installed dependencies, no `.env` copying and no replacement of the normal dev server. Copy/cache retained at `D:\AcademyDesk\.build-check\browser-web-5ee176b9dd2b4c03bee82f12aa854415`. `/login` compiled and returned 200 after 19.4 seconds. [Frontend output](../EVIDENCE/logs/phase-2b-media-resume-attempt-frontend.log).

Browser navigation/accessibility inspection repeatedly timed out on CDP Page.navigate / Emulation.setFocusEmulationEnabled. The documented DOM alternative and tab close also failed; screenshot attempt timed out and reset the browser-control session. No Teacher sign-in, file selection, pause, resume, reload, pacing request or download was observed. SQL upload-session observation was `[]`. No screenshot proof exists, and no upload PASS is claimed. The unsigned-in test tab could not be closed through browser control; do not inspect or reuse customer sessions to bypass this failure.

The exact owned stop marker ended the host; guarded private Blob container, SQL database/runtime login, host folders and labelled SQL/Azurite containers were removed. Harness exit 0 / 204.6 seconds indicates successful host lifecycle/cleanup, not browser acceptance. Owned Next child/parent processes were stopped only after matching loopback listener, PIDs and command lines; resulting frontend exit 1 is cleanup. Disposable contents are not recoverable; source copies and generated fixtures remain recoverable on D:. No broad Docker prune or personal-file cleanup.

## Continuation preflight and next task

At continuation, C: free space was 1,226,199,040 bytes (about 1.14 GiB), below the 2 GiB minimum. No new server/container provisioned. Exact prior-run containers, QA listeners and host root were confirmed absent. A disk-capacity blocker is additional to the unverified browser recovery; neither is an application defect.

[New source snapshot](PHASE_2B_MEDIA_RESUME_SOURCE_SNAPSHOT.json) retains 48 hashes; only three QA source records differ from the preceding snapshot (bridge, launcher pacing parameters, binary fixture generator). Application/test source records, pre-existing student edits, production configuration and HEAD remain unchanged. Historical snapshots are not rewritten.

Next, restore sufficient C: capacity and working browser control, then run the prepared scenario once: upload first chunk → Pause → reload/reselect same teaching scope → reject a wrong original file → reselect the original → Resume → assert one completed resource/session and exact native-download bytes. Still NOT RUN. Stay with the agreed Sol High allocation. Phase 2B and BUG-SEC-0001 remain OPEN / FIX-IN-PROGRESS / PARTIAL PASS; video/large-file/device and critical/release gates remain pending.
