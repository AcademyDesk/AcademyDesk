# Phase 2B — Controlled recorder memory-limit recovery

2026-10-01. **Controlled checks PASS; real-device limit case NOT RUN.** QA-only change. No application fix needed in this slice; no Azure access, commit/push, normal development database change or duplicate live frontend/backend. Accepted Astra audit is not repeated. Continue agreed Sol High allocation.

## Executed bounded checks

Five new tests exercise the actual uploader handlers with explicit React-hook, recorder, preview and upload/storage doubles. Blobs contain real synthetic bytes at the actual 32-MiB threshold (33,554,432 bytes), not a reduced test threshold. They contain no microphone capture and are not valid codec recordings.

| Check | Observed result |
| --- | --- |
| Threshold minus one byte | Recording stays active, upload disabled, no automatic upload; unmount releases tracks |
| Exact threshold in two events | One automatic stop, tracks released, title retained, limit guidance shown, explicit Confirm upload enabled |
| Deferred stop/final data event | Final four bytes retained, no second stop, upload stays disabled until stop callback; larger-than-preview fallback guidance rendered |
| Unmount during deferred stop | Tracks released, queued data/stop callbacks suppressed, no late state updates or upload |
| Upload failure/retry after limit | Same original File and tail retained, MIME/extension/title/comment kept, Resume enabled; explicit retry succeeds, fields reset and history refresh callback runs once |

**50/50 targeted tests PASS**: 28 media/client + 4 workspace + 18 recorder-handler tests. Command: `node --test QA/tools/teacher-recording.test.cjs QA/tools/teacher-workspace.test.cjs QA/tools/class-media-client.test.cjs`. The fifth new test uses upload-function doubles: it establishes uploader control flow, not real Blob persistence/network/private access. Existing client protocol tests ran separately in the same command, not an end-to-end composition of this synthetic recording.

Initial run was 49 PASS / 1 FAIL because the new fixture awaited the void onSubmit wrapper rather than completion of its asynchronous work. The harness now yields one event-loop turn before checking settlement and shares the Error constructor with the explicit upload double, matching the same-page error realm. Final run passes; initial failure is harness timing, not an application defect. The test scope key was corrected from sessionId to actual classSessionId. Deferred stop models dataavailable followed by stop; it is not native MediaRecorder scheduling evidence.

## Important limits

The existing 32-MiB recorder threshold is a request to stop after accumulated encoded chunks reach the threshold, **not a hard browser-memory ceiling**. A delayed data event can overshoot, and the final chunk is intentionally retained rather than truncated. Actual native scheduling, huge final chunks, codec validity/playback and long-running recording on Android/iOS remain NOT RUN. Files above the preview threshold offer upload/download fallback instead of inline review. The 2-GB saved-file allowance is separate from this recorder threshold; it is not proven by this test.

No browser/microphone/SQL/container run was started. No typecheck, lint, build or full critical/security suite rerun was needed or counted: application sources are identical to the previous checkpoint. Real microphone denial and live Cancel remain unverified as documented in [microphone recovery](PHASE_2B_MICROPHONE_RECOVERY.md). No raw audio/evidence screenshot was generated in this non-UI slice.

## Traceability and next

[59-record snapshot](PHASE_2B_RECORDER_LIMIT_SOURCE_SNAPSHOT.json): previous snapshot differs only in `QA/tools/teacher-recording.test.cjs`; all captured application files unchanged. HEAD remains `20bb6047f9edf733ac8e2a226621cc582ec54b3c`; unrelated dirty changes preserved. Evidence: [initial harness timing result](../EVIDENCE/logs/phase-2b-recorder-limit-initial.log), [50 final tests](../EVIDENCE/logs/phase-2b-recorder-limit-tests.log), [hash/link validation](../EVIDENCE/logs/phase-2b-recorder-limit-validation.log). In-memory synthetic test objects released on test-process exit; no customer data deletion/cleanup.

Next bounded local slice: recorder interruption/error recovery and preserving a clear warning when captured audio is retained; agreed **Sol High**. Actual permission-denial/live-cancel/recording-limit device gates remain open, not endlessly retried in the promptless QA browser. Phase 2B, BUG-SEC-0001, legacy BUG-FUNC-0007 failure evidence, financial/payroll P0/policy, full critical/device/release gates remain open. No phase/issue closure or deployment authorization inferred.
