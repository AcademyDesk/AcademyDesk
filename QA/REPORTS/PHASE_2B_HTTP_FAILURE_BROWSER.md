# Phase 2B — real Teacher HTTP 503 upload recovery

2026-10-01. **PASS for one bounded synthetic one-chunk provider-failure/retry scenario.** Phase 2B remains in progress. BUG-FUNC-0007 remains OPEN / STATIC-FINDING with partial evidence for the new uploader; BUG-SEC-0001 remains OPEN / FIX-IN-PROGRESS / PARTIAL PASS. No application changes, commit/push, Azure access/deployment or development/Azure database writes.

## Test boundary and setup

Agreed Sol High allocation; accepted Astra audits not repeated. Real Program/Identity/SQL/private Azurite backend and actual source-only Next frontend on loopback 49002/49001. Fresh run `84f4c8543bf4442ca9dcdef933e58af0`, UTC start `2026-10-01T03:37:11.3402051Z`, SQL port 57944; frontend copy `.build-check/browser-web-d6cd49c7421e46969080473c9052ce7b`. Tenant startup controls and exact owned-context/login checks passed. Runtime inventory 307 routes / 296 controller method/routes / 10 framework Identity entries. Migration histories 80 application / 7 Identity.

QA-only `BrowserFailureBlobStore` decorates the real emulator store and throws Azure RequestFailedException(503) exactly once on chunk 0 **before storage writes**. The unchanged real controller returns its own HTTP 503; the bridge does not fabricate an API response. Explicit BrowserStorageFailOnce/QA_BROWSER_FAIL_STAGE_ONCE opt-in only; negative guard rejects failure injection outside Browser mode before provisioning. Fresh no-tracking SQL and actual Blob progress asserted immediately after failure and after completion.

Existing valid silent one-second PCM fixture: `synthetic-audio.wav`, **16,044 bytes**, SHA-256 `56d4af65701c26df20bd4021eda95b6e830348ce3a746086079fe89285548dc9`. Not customer media. Teacher entered title “HTTP recovery lesson” and comment “Synthetic storage rejection retry” through the UI/file chooser.

## Actual checks

| Check | Observed result |
| --- | --- |
| First upload | Create 201, status GET 200, chunk 0 PUT **503**, no completion request |
| Failure persistence | One session, zero learning resources, zero staged blocks, not committed |
| Retained form | Filename/draft and entered title/comment remain; title/comment locked to immutable session; audio duration 1, readyState 4, no media error; Resume enabled |
| Failure notice | “Private media storage is unavailable. Keep your file and retry later.” No success/history |
| Retry without file chooser/reselection | Create 200 reuses same ID `b3873272-36fc-4962-9b2a-b67ce2c838fa`; status GET 200, chunk 0 PUT 200, complete 200 |
| Confirmed success | Fields/file/preview/draft cleared, success notice, exactly one refreshed history article with original title/comment |
| Final fresh SQL / Blob | One completed session, one matching resource, one committed block; no duplicate attachment |
| Native Teacher download | 16,044 bytes and SHA-256 exactly match original |
| Targeted client/protocol/SSR suite | **24/24 PASS**, including new controlled 503 transport regression |
| QA harness build | 0 warnings / 0 errors |
| Negative opt-in guard | PASS; flag without Browser refused before provisioning |

Staged file retention is proved behaviorally by successful retry **without reselecting**, decoded preview and exact bytes; the native input's files list was empty, so File object identity is not claimed from DOM. Client unit test uses a transport double, not additional browser/SQL evidence. Browser log sample returned ordinary DevTools/HMR entries, no application error in that sample; not exhaustive console/network certification.

## Evidence and provenance

[Selected sanitized HTTP/SQL/cleanup output](../EVIDENCE/logs/phase-2b-http-failure-http.log), [DOM/media/hash/source/cleanup observations](../EVIDENCE/logs/phase-2b-http-failure-observations.log), [24 client tests and build](../EVIDENCE/logs/phase-2b-http-failure-client.log), [opt-in refusal](../EVIDENCE/logs/phase-2b-http-failure-guard.log).

The two screenshot captures were inspected but show only the top viewport, not the offscreen upload state: [first viewport](../EVIDENCE/screenshots/phase-2b-http-failure-retained.png), [second viewport](../EVIDENCE/screenshots/phase-2b-http-retry-success.png). They are **not** visual proof or layout acceptance. Functional evidence is the observed semantic DOM, decoded media, real HTTP, SQL/Blob assertions and download hash. An initial sign-out selector timed out after the page layout/menu changed; fresh snapshot/retry completed normal sign-out and final Sign in DOM. No actual phone/stable-viewport acceptance claimed.

All prior 50 source records compared against [previous final snapshot](PHASE_2B_LARGE_BROWSER_SOURCE_SNAPSHOT.json). Only three QA files changed: client test, BrowserMediaHost and Run-BlobMedia. New QA decorator makes [final snapshot](PHASE_2B_HTTP_FAILURE_BROWSER_SOURCE_SNAPSHOT.json) **51 records**. Application/API-test sources, production config and pre-existing student onboarding/profile hashes unchanged; HEAD unchanged `20bb6047f9edf733ac8e2a226621cc582ec54b3c`. Prior API 85 / HTTP 75 / legacy 29 passes are historical, not rerun. No need to recount previous frontend typecheck/lint as new results.

## Cleanup and interrupted-run limitation

Fresh run stopped using exact owned marker. Harness exit **0**, 331.5 seconds; exact private Blob container, disposable SQL database/runtime login, host folders and matching labelled SQL/Azurite containers removed. Verified fresh root absent, no fresh labelled containers and no QA listeners. Synthetic sign-out/tab closure completed. Only matched QA Next child/parent stopped; launcher exit 1 records intentional termination, not a product failure. Source copy, original fixture and hash-verified download retained on D:. Disposable fresh contents are not recoverable; no customer files removed.

An earlier Sep 30 run `d6dfd31d64c146789abbf686bccb920a` was interrupted overnight after Teacher/class selection but **before upload/fault injection**. Docker was stopped on continuation and restarted using its supported CLI. Both old owned containers were observed Exited(255). Manual exact-target cleanup was refused by policy and was **not retried/bypassed**; those two old containers remain, and its old disposable root may remain (not verified). Fresh success does not imply all historical QA resources are cleaned.

## Remaining gates / next task

This covers one real **503 before-write** recovery path in the new resumable uploader. It does not reproduce the original legacy 400/413/500/network rejection bug, prove arbitrary network loss, response loss after durable completion, expiry/revocation/shared-device cleanup, recording, 2 GB transfer, every codec, actual Android/iOS or full critical/release acceptance. Permanent TEACHERPORTAL-UPLOAD-001 and SECURITY-FILE-001 gain bounded partial evidence, not closure. Four financial P0 issues and policy decisions remain open.

Next bounded task: lost completion response **after persistence**, then retry/reload safely without duplicate resource. Continue agreed **Sol High**. Astra High only for a new unresolved policy/design decision; do not redo accepted audit work.
