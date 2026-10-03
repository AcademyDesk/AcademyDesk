# Phase 2B — Microphone waiting cancellation and saved-file recovery

2026-10-01. **PARTIAL PASS. Actual permission denial and live Cancel click remain unverified.** Local repairs and saved-file fallback verified; no Azure access/deployment, commit/push or normal development database change. Accepted Astra audits are not repeated. Agreed Sol High allocation continues.

## Scope and repairs

User reported no browser permission prompt on fresh loopback 49011. This browser subsequently granted microphone access without a visible prompt. Do not classify that as a denial pass. Two attempts to exercise Cancel raced with access being granted; recordings were stopped and removed, never uploaded. Actual Cancel click is NOT VERIFIED, not an established product failure. No further microphone attempts are required in this environment; carry actual denial/cancel to a browser/device where permission can be declined normally.

Added Cancel microphone request while getUserMedia is pending. It unlocks saved-file actions and keeps the selected file/title/comment. It cannot dismiss or abort the native browser prompt: late grants are ignored and tracks stopped; another recording request remains disabled until the old request settles. Explicit actual-component handler tests, with React-hook/media/storage doubles, verify cancellation, late grant cleanup and late rejection guidance. Before-fix cancellation regression: 9 PASS / 2 expected FAIL; final regression passes. This is not hardware/permission-prompt evidence.

Real saved-file fallback exposed a separate defect: Remove file did not reset the native picker value, so choosing the same original file did not restore the selection. Removal now clears the input; successful recording start also clears it, but pending/denied/cancelled requests retain it. Before-fix picker regressions: 11 PASS / 2 expected FAIL; final regression passes. No API/security-policy or CSS changes in this slice. Application delta versus the previous recording snapshot is only the uploader.

## Executed final-source checks

| Check | Result / limit |
| --- | --- |
| Remove / same-file native reselect | PASS on final refreshed page: filename returned, title/comment retained, Confirm upload enabled |
| Explicit saved-file upload | PASS: silent synthetic WAV only; create 201, chunk 200, completion 200, success message, blank/reset form and refreshed history |
| Fresh read-only SQL | PASS before upload: zero sessions/resources/media notifications; after: exactly one completed session, one resource and one Student notification, original Teacher/batch/title/comment/file, 16,044 bytes |
| Targeted tests | 45/45 PASS: 28 media/client, 4 workspace, 13 actual recording-handler tests with explicit doubles |
| TypeScript / uploader lint | Typecheck exit 0, no diagnostics; lint exit 0, zero errors / one existing no-img-element warning |
| Real denial / real Cancel | NOT VERIFIED; no prompt available and click raced with grant. No substituted unit-test acceptance |

No full build/lint/critical suite rerun, no real Android/iOS, 2 GB, long-duration/recorder size-limit acceptance, universal format guarantee or release certification. Prior recording/media/security runtime results remain historical, not automatically rerun on changed source. Synthetic metadata mentions denial for fixture traceability; it does not establish a denial occurred.

## Reproducibility and evidence

Owned run `e5e9c7f2f0434fc5975911cc10841936`, UTC start `2026-10-01T04:51:03.1231795Z`, SQL loopback port 59907, API 49012, web 49011. Source-only frontend copy `.build-check/browser-web-e1484b72caa74cc080877c4756e241df`; real Identity/API/SQL with private local Azurite, not Azure. Tenant guards PASS; route inventory 307 / controller methods 296 / framework Identity 10. The final uploader was copied and the page reloaded before same-file/upload verification.

[59-record source snapshot](PHASE_2B_MICROPHONE_RECOVERY_SOURCE_SNAPSHOT.json): previous 58 sources differ only in uploader and recording tests; new read-only scoped verifier added. HEAD unchanged at `20bb6047f9edf733ac8e2a226621cc582ec54b3c`. Existing unrelated working-tree changes preserved.

Evidence: [cancellation before-fix](../EVIDENCE/logs/phase-2b-microphone-recovery-tests-before-cancel.log), [picker before-fix](../EVIDENCE/logs/phase-2b-microphone-recovery-tests-before-picker.log), [45 final tests](../EVIDENCE/logs/phase-2b-microphone-recovery-tests.log), [typecheck](../EVIDENCE/logs/phase-2b-microphone-recovery-typecheck.log), [lint](../EVIDENCE/logs/phase-2b-microphone-recovery-lint.log), [SQL](../EVIDENCE/logs/phase-2b-microphone-recovery-sql.log), [actual DOM](../EVIDENCE/logs/phase-2b-microphone-recovery-dom.log), [HTTP/cleanup](../EVIDENCE/logs/phase-2b-microphone-recovery-http-cleanup.log), [validation](../EVIDENCE/logs/phase-2b-microphone-recovery-validation.log), [saved-file success screenshot](../EVIDENCE/screenshots/phase-2b-microphone-fallback.png). Screenshot is functional evidence, not a golden visual/mobile/physical-device baseline. No raw microphone media retained in evidence.

## Cleanup and next

Synthetic sign-out verified at Sign in; owned QA tab closed. Exact run private Blob container, SQL DB/runtime login, labelled SQL/Azurite containers and run root removed; loopback listeners 49011/49012 absent. Backend exit 0, elapsed 543.3 seconds, application migrations 80 / Identity 7. Exact Next child 7348/parent 19168 intentionally stopped; frontend launcher exit 1 is that termination, not an app failure. Uploaded disposable silent fixture cannot be recovered from QA storage; source fixture/evidence remain. No customer data removed. Older interrupted `d6dfd31d64c146789abbf686bccb920a` stopped containers remain outside scope; refused historical cleanup not retried.

Next bounded local slice: recorder memory-limit stop/review/upload recovery using controlled events first; agreed **Sol High**. Actual denial/live cancel are deferred external-browser/device gates, not closed or endlessly retried here. Phase 2B, BUG-SEC-0001, legacy BUG-FUNC-0007 failure evidence, four financial/payroll P0 plus policy, full critical/device/release gates remain open. No issue/phase closure implied by these narrow repairs.
