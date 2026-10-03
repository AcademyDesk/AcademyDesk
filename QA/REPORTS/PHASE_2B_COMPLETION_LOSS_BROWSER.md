# Phase 2B — lost completion body, reload and safe confirmation retry

2026-10-01. **PASS for one bounded real Teacher completion-response-loss recovery case.** Application/API-test sources unchanged; only QA infrastructure/tests changed. Phase 2B and both media-related issues remain open with partial evidence. No commit/push, Azure access/deployment or development/Azure database writes. Agreed Sol High allocation; no repeat accepted Astra audit.

## Real host and fault boundary

Final fresh run `c6196bdbe2004f78aed490bd9ea90d24`, UTC start `2026-10-01T03:50:57.5016596Z`, isolated SQL port 56179. Real Program/Identity/SQL/private Azurite behind loopback bridge 49002; actual source-only Next frontend 49001 at `.build-check/browser-web-7c48a875ce42416894035a0c2b6e524c`. Normal development server/database not replaced; no .env copied. Tenant startup controls passed. Runtime inventory 307 routes / 296 controller method/routes / 10 framework Identity entries; migration histories 80 application / 7 Identity.

Explicit `BrowserLoseCompletionOnce`/QA_BROWSER_LOSE_COMPLETION_ONCE only. Missing Browser or combination with stage-failure mode rejected before provisioning. Real completion executes and commits SQL/Blob, then fresh no-tracking assertions verify one session/resource/notification. The bridge forwards real HTTP headers and **the first byte of the actual JSON body**, then aborts the connection. No fabricated success/error body, mock auth, altered product storage or authorization. This simulates an incomplete response after durable persistence, not SQL failure.

QA helper checks the same upload ID, completed session, matching title/comment, exactly one learning resource, one enrolled Student notification and one committed Blob block before response truncation, after retry and at stop. Assertions apply to this synthetic one-recipient/one-chunk case, not global notification concurrency certification.

Fixture: existing genuine silent one-second 8 kHz mono PCM WAV `synthetic-audio.wav`, **16,044 bytes**, SHA-256 `56d4af65701c26df20bd4021eda95b6e830348ce3a746086079fe89285548dc9`. Title “Completion recovery lesson”; comment “Saved before confirmation was lost”.

## Observed sequence

| Check | Result |
| --- | --- |
| Initial transfer | Create 201, status GET 200, chunk 0 PUT 200, real completion 200 after persistence |
| Before truncating completion response | One completed session/resource/notification, one committed Blob block |
| Visible uncertain result | Title/comment locked and retained, filename/draft and audio preview retained (duration 1, readyState 4, no media error), progress Confirming upload 100%, Resume enabled |
| Notice | “The server returned an unexpected result. Keep your file and resume to check its status.” No premature success or history refresh |
| Actual page reload / reopen class | Same draft/title/comment restored; reselect-original-file notice; Resume disabled until file chosen. The already saved history item is now visible |
| Original file reselected / Resume | Create 200 reuses same ID `394dc574-a6d0-42b4-8529-17e6f55da82d`; GET confirmed progress; **no second chunk PUT**; completion 200 |
| Final fresh SQL/Blob assertions | Still one completed session/resource/notification and one committed block; same upload ID, no duplicates |
| Confirmed UI | Success notice, fields/file/preview/draft reset, one refreshed history article |
| Native Teacher download | Exact 16,044-byte original SHA-256; generated download moved without overwrite to `.build-check/browser-media-fixtures/completion-retry-downloaded.wav` |

Real UI/file chooser used; no browser credentials/storage/hidden application state read or injected. Reload necessarily requires reselecting the original file because the browser does not persist its in-memory File object. Saved draft retains metadata/hashes instead. Functional evidence is semantic DOM/media, real HTTP, fresh SQL/private Blob assertions and exact download, not visual/mobile acceptance. Browser log sample returned ordinary DevTools/HMR entries; no exhaustive network-console/error certification. Login was initially attempted before the final backend was ready, gave generic login failure, then succeeded after BROWSER READY; this is test sequencing, not new credential defect evidence.

## Initial attempt preserved, not silently substituted

Run `f9593dccee9e4302ad59c0ab83e7672c`, UTC start `2026-10-01T03:48:35.8070416Z`, SQL 64298, initially aborted **before headers/body**. Chromium transparently resent completion: real API responded 200 twice, same upload, one resource/notification; UI showed success directly. This proves bounded backend idempotence under transport retry but did **not** exercise visible uncertain outcome/reload. Initial bridge source SHA-256 `2597ece45d293275e12a763c2502eb55caa67e76bd15c63dbda711c9bb751b26` and [output](../EVIDENCE/logs/phase-2b-completion-loss-preheaders-attempt.log) retained.

Test-only bridge changed to truncate the genuine response body; final rebuilt host used a fresh fixture. Do not count initial attempt as an identical-final-source repeat or visible-recovery PASS. Initial host cleanup exit 0 / 96.4 seconds; its tab closed (normal sign-out not claimed for initial attempt). Same healthy, unchanged-product frontend reused for final run.

## Tests, sources and evidence

Targeted client/protocol/SSR **25/25 PASS**, including new explicit lost-completion transport-double case; rerun also 25/25. Unit double models idempotent persistence, not additional SQL/browser proof. Final QA harness build **0 warnings / 0 errors**. Two negative opt-in checks passed before provisioning. No full API/legacy/critical suite, new frontend typecheck or full lint run; historical API 85 / HTTP 75 / legacy 29 remain historical.

[Final HTTP/SQL/cleanup output](../EVIDENCE/logs/phase-2b-completion-loss-http.log), [actual DOM/media observations](../EVIDENCE/logs/phase-2b-completion-loss-dom.log), [25 tests/initial build/negative guards](../EVIDENCE/logs/phase-2b-completion-loss-client.log), [final build](../EVIDENCE/logs/phase-2b-completion-loss-final-build.log), [frontend lifecycle](../EVIDENCE/logs/phase-2b-completion-loss-frontend.log), [integrity/cleanup validation](../EVIDENCE/logs/phase-2b-completion-loss-validation.log).

[Final source snapshot](PHASE_2B_COMPLETION_LOSS_BROWSER_SOURCE_SNAPSHOT.json) has **52 records**. Compared with prior 51-record [503 snapshot](PHASE_2B_HTTP_FAILURE_BROWSER_SOURCE_SNAPSHOT.json), only three QA records changed: client test, BrowserMediaHost and Run-BlobMedia; new BrowserCompletionLoss added. All product/API-test sources, production config and student onboarding/profile edits unchanged. HEAD unchanged `20bb6047f9edf733ac8e2a226621cc582ec54b3c`; historical snapshots not rewritten.

## Cleanup and limitations

Final normal sign-out verified by Sign in DOM and agent-owned tab closed. Exact owned marker disposed host: exit **0**, 188.2 seconds; matching private container, disposable SQL database/runtime login, host folders and labelled SQL/Azurite containers removed. Both current runs' labels/roots absent and no listeners on 49001/49002. Matched QA Next child 20336 / parent 22812 stopped; launcher exit 1 records intentional termination. Disposable contents not recoverable; copies/fixture/verified download retained on D:. No customer file removed.

Older interrupted `d6dfd31d64c146789abbf686bccb920a` still has its two stopped containers; earlier cleanup refusal not bypassed/retried. No claim that every historical QA resource was removed.

TEACHERPORTAL-UPLOAD-001 and SECURITY-FILE-001 gain bounded partial evidence. Original legacy 400/413/500/network rejection reproduction, arbitrary network conditions, permission/expiry revocation during recovery, shared-device cleanup, recording, actual Android/iOS, 2 GB transfer, every codec and critical/release gates remain pending. Four financial P0 issues and unresolved policy decisions still open; no phase/issue closure or release certification.

Next bounded task: upload permission revoked during recovery—deny further writes while preserving a safe retry/clear-draft experience. Continue agreed **Sol High**; Astra High only for a focused unresolved policy/design decision.
