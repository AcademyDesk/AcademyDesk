# Phase 2B — revoked class assignment during upload recovery

2026-10-01. **PASS for one bounded real Teacher assignment-revocation/recovery case.** Clear-draft confirmation was human-assisted and the resulting cleared form verified. Phase 2B/issues remain open. Agreed Sol High allocation; accepted Astra audit not repeated. No application/API-test source edits, commit, push, Azure access/deployment or development/Azure database writes.

## Run and scope

Run `c29c576c16f54a508bbe3786441cfed8`, UTC start `2026-10-01T03:59:30.6905690Z`; isolated SQL loopback port 57784, real Program/Identity/SQL/private Azurite behind bridge 49002. Actual source-only Next frontend 49001, copy `.build-check/browser-web-7db20a63f9384f7f9726fe21b1d0e5d9`. Normal development frontend/backend/database not replaced. Tenant controls passed; runtime inventory 307 routes / 296 controller method/routes / 10 framework Identity entries, migration histories 80 application / 7 Identity.

Explicit QA-only BrowserRevokeAfterChunk mode requires paced Browser mode and disallows other fault modes. After real chunk 0 is durable, an exact synthetic BatchId/AcademyId/originalTeacherId guard removes only that fixture's TeacherId assignment. Fresh contexts and private Blob checks require one unfinished session, no resource/notification and only uncommitted block 0. Real authenticated probes reuse the legitimate request's authorization internally; credentials are not logged or read through the browser. No mocked auth/response or application access-policy change.

Existing inert 17 MiB fixture `synthetic-resume.bin`: 17,825,792 bytes, three chunks (8+8+1 MiB), SHA-256 `9f4be0e976dfd2921de0b28a22ec87d887100b6d4af3301339ef7a51629ee393`. Title “Revoked assignment lesson”; comment “Retain draft after assignment removal”. Inert file has no playable preview; this is not additional audio/video/device recording proof.

## Observed results

| Check | Actual result |
| --- | --- |
| Initial real transfer | Create 201, GET 200, chunk 0 PUT 200; same upload ID `ea5e503b-9996-402c-980b-79e3864645a4` |
| After assignment removed | Real authenticated GET status, PUT chunk 1 and POST complete each 404 |
| Browser Pause | File/title/comment retained; confirmed progress 47%, paused notice |
| Browser Resume | Real POST create 400: “Select an active assigned batch, student and class session where applicable.” File, draft, title and comment retained |
| Delayed chunk 1 | After 30-second pacing, request still forwarded; real API returned 404. Do not claim Pause prevented all forwarding |
| Fresh final SQL/Blob | One unfinished session, zero learning resources, zero new-class-material Student notifications, block 0 only, no committed Blob |
| Clear saved draft | Automation was blocked by native confirmation/transport timeout; user accepted OK. Subsequent actual DOM verified blank unlocked fields, no file/draft, upload disabled, Record audio enabled and “Browser draft cleared. You can start a new upload.” |
| Sign-out | Actual Sign in DOM verified; agent-owned QA tab closed |

Clear is browser-local, not deletion of server session/block. Fresh final state remains unchanged. No server DELETE/write occurred for clearing. This is bounded batch-assignment revocation, not account disablement, role removal, security-stamp expiry or all concurrent in-flight races. No screenshot/visual/mobile acceptance or exhaustive console certification claimed.

## Tests and integrity

Targeted client/protocol/SSR **26/26 PASS**, including new explicit transport-double denial/retry/forget case; double proves local metadata retention and no API mutation on forget, not extra SQL/browser evidence. Final QA build **0 warnings / 0 errors**. Three negative opt-in guards passed before provisioning. Earlier 25-test run is not added to final test count. No full API/legacy/critical suite, frontend typecheck or full lint rerun; prior accepted results remain historical.

[53-record source snapshot](PHASE_2B_UPLOAD_REVOCATION_BROWSER_SOURCE_SNAPSHOT.json): compared with previous 52-record completion-loss snapshot, only QA client test, BrowserMediaHost and Run-BlobMedia changed; BrowserUploadRevocation added. All captured product/API-test sources/config/pre-existing student edits unchanged. HEAD remains `20bb6047f9edf733ac8e2a226621cc582ec54b3c`.

Evidence: [HTTP/fresh SQL/Blob/cleanup](../EVIDENCE/logs/phase-2b-upload-revocation-http.log), [actual DOM and confirmation limitation](../EVIDENCE/logs/phase-2b-upload-revocation-dom.log), [26 client tests/final build](../EVIDENCE/logs/phase-2b-upload-revocation-client.log), [initial build/three guards](../EVIDENCE/logs/phase-2b-upload-revocation-guard.log), [frontend lifecycle](../EVIDENCE/logs/phase-2b-upload-revocation-frontend.log), [source/registry/link/cleanup validation](../EVIDENCE/logs/phase-2b-upload-revocation-validation.log).

## Cleanup and remaining gates

Exact owned stop marker disposed host, exit 0 / 345.2 seconds; matching private container, disposable SQL database/login, owned folders and labelled SQL/Azurite containers removed. Current run root/labels absent; no listeners 49001/49002. Matched QA Next child 3248 / parent 18344 intentionally stopped; launcher exit 1 is termination evidence, not an application failure. Synthetic disposable contents are not recoverable; source copy and inert fixture retained on D:. No customer file removed.

Initial cold login navigation timed out, then the same tab recovered with /login 200; frontend log also includes three-second request retries of unestablished cause. Not classified as a new application defect. Older interrupted `d6dfd31d64c146789abbf686bccb920a` two stopped containers remain; previous cleanup refusal not retried/bypassed. No claim every historical resource is gone.

BUG-SEC-0001 remains OPEN / FIX-IN-PROGRESS / PARTIAL PASS. BUG-FUNC-0007 remains OPEN / STATIC-FINDING; original legacy 400/413/500/network rejection not runtime reproduced. TEACHERPORTAL-UPLOAD-001 and SECURITY-FILE-001 receive bounded partial evidence only. Shared-device account/draft isolation, recording, actual Android/iOS, 2 GB transfer, wider permission/expiry/concurrency cases and full critical/release acceptance remain pending. Four financial P0 issues/policy decisions remain open.

Next bounded task: shared-device sign-out/account-switch draft isolation. Continue agreed **Sol High**. No phase or release closure.
