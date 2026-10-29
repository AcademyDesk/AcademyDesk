# Batch1 — controlled recorder cancellation/interruption/limit composition

Date:2026-10-13. Worktree `D:\AcademyDesk-codex-p0`, branch `codex/penta-search`, HEAD `2e376b0ec47268c9b11d0dda2387620932ce2dd0`. QA-only continuation, no application repair required. Existing native permission/playback report remains independent and unchanged. Batch1/BUG-SEC-0001/BUG-FUNC-0007 remain OPEN.

## Accepted run

`9b16ad2cb0cf4a3eb1d6a0ab842edb8e`,72.1s,exit0; exact loopback SQL port60255. Retained75 original Blob assertions, four exported Teacher journeys320/1440px light/dark, four controlled cases per journey =16 cases. Real Identity/login/API + real disposable SQL/private Azurite. **This is controlled browser recovery evidence, not native prompt/failure/long-duration certification.**

Cases in each journey:

- Hold a granted native fake-device getUserMedia stream at a test-only Promise boundary; real UI Cancel retains exact original saved-file bytes/title/comment, unlocks saved-file upload and keeps new recording disabled until settlement. Release the late grant: application stops its tracks, creates no recorder, preserves cancellation message, and allows recording again.
- Repeat Cancel with a test-generated rejection. The test releases its held stream before rejecting: this is not proof that an OS-denied request returns a stream or that application code releases a stream it never receives. No recorder/automatic upload; original bytes/details retained.
- Start an actual native MediaRecorder on a synthetic device, inject a controlled error event after1.25s, and observe native final data/stop. Exactly one stop call/event, tracks ended, captured file12533 bytes equals the sum of data chunks, warning survives stop, details/explicit confirmation retained.
- Start another native recorder and inject exactly33554432 bytes minus any preceding chunks using a controlled BlobEvent. Native stop preserves final tail; exact generated File length/MIME/name and SHA256 equal concatenated nonempty chunks. Exactly one stop call/event. Remove file unlocks recording and disables upload. No double stop or implicit publication.

| View | Boundary retained bytes | Native final tail | Exact File/chunk hash |
| --- | ---: | ---: | --- |
|320 light|33554607|175|PASS|
|320 dark|33554607|175|PASS|
|1440 light|33554607|175|PASS|
|1440 dark|33554432|0|PASS|

The injected bytes are inert and zero-headed, not decodable audio. Over32MiB the UI intentionally omits preview; at exactly32MiB its existing signature sniffing also correctly rejects these inert bytes. Saved-file fallback/confirmation remain usable. The oracle independently checks the actual captured File/hash, not merely the event size or rounded “32.0 MB” display. Real WebM playback is established by the separate native-recorder report, not these limit fixtures.

## Integrity and scope

All41 real requests (login +10 reads per journey) compare unchanged protected-state/Identity safe projections/role assignments, resources/notifications/audit/files, complete upload sessions and committed Blob inventories/ETags; final comparison also passes. No domain writes/no auto-upload/no429. This does not compare all Identity authentication metadata or prove cancellation after authorization admission. Original75 Blob assertions are unchanged.

90 application +7 Identity migrations/non-admin runtime/root HTTP/two-tenant controls/mismatched cleanup marker refusal/exact private Blob/database/login cleanup PASS. Runner removed only its two exact name+label+single loopback-bound containers. All nine original containers retained. No host microphone or personal audio accessed; Edge used `--use-fake-device-for-media-stream`, native permission configured via isolated CDP.

## QA changes and validation

Opt-in `-RecorderRecovery` reuses the guarded read-only recorder relay/dedicated ignored `.build-check/media-recorder-sql` harness. Default `-RecorderBrowser` retains its original native denial/playback/unmount oracle. New `recorder-recovery-cases.cjs` transparently observes native streams/recorders/generated Files and explicitly labels injected scheduling/error/data signals. `Program.cs` rejects orphan/invalid recovery mode; runner rejects inherited/mixed modes before provisioning and clears its flag in finally.

Fresh harness build28.86s/zero warnings-errors; both JS syntax checks/PowerShell parser/four mixed-mode refusals/orphan recovery pre-provision refusal/scoped high-confidence secret-pattern scan/git diff checks PASS. No staged files; evidence untracked/build ignored; feature/main SHAs unchanged. Prior56 focused handler/transport checks,1484 API tests and83-page export retained, not rerun for QA-only additions. Teacher export SHA256 `597514e6096780c26d2188a9d07d2f382e757f986c22819d9e59a07120f43ca6`; uploader raw SHA256 `e5206f80573b0f289f3c418436efc071cb42d05ae68c20647c4997e8c3281b0f`, both match preceding native run. No app/limits/permissions/schema/Mini/Core changes.

Failed receipts retained: `24cc9c82950948a3a06a98e44a5b9cc3`56.2s (first mobile journey passed, then null preview assumption) and `0a6f70b44a0548fc816ffcdd1b204c5e`74s (waited for nonexistent audio preview). Initial timing inference was corrected after inspecting source: the inert fixture fails signature sniffing, so preview absence is expected. Final oracle requires correct signature/size-dependent behavior, retains exact File/hash/stop assertions, and does not claim inert bytes play. Only each failed run's exact owned SQL/Blob containers removed after ownership/loopback checks; evidence preserved.

Receipts: `QA/EVIDENCE/batch1-20261012/sql-recorder-recovery.txt`, `sql-recorder-recovery-final.txt`, `sql-recorder-recovery-verified.txt`; final JSON/eight screenshots at `QA/EVIDENCE/recorder-recovery/9b16ad2cb0cf4a3eb1d6a0ab842edb8e`. Inspected final320-dark-limit and earlier failed320-light screenshots: correct warning/file retention; existing left/header/list clipping remains Batch2, not visual acceptance. Raw captured/injected bytes are not saved to evidence or uploaded.

## Next

Next Sol High: compose real recorded audio → explicit upload → authoritative history/authorized exact-byte readback; then remaining scoped security/operational gates. Human pending permission UI, genuine hardware failure/long capture, Android/iOS/physical devices, wider codecs, after-admission authorization timing and release/qualified reviews remain separate. No issue or Batch1 closure. Batch2 shared Manual Orbit+Aura dark/Prism light four-portal visuals follow necessary gates and actual versioned PENTA design handoff; PENTA AI frontend belongs to its project. Medium for settled visuals. No new Core contract/project switch/handoff, stage/commit/push/main/Azure.
