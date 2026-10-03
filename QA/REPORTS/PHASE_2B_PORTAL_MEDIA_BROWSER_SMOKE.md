# Phase 2B — real portal media browser smoke

2026-09-30. Bounded follow-on to [native downloads](PHASE_2B_NATIVE_DOWNLOAD_INTEGRATION.md), **not complete portal/device/release acceptance**. Phase 2B and BUG-SEC-0001 remain OPEN / FIX-IN-PROGRESS / PARTIAL PASS. Accepted Astra audits and prior failures are retained. No application code, commit, push, Azure configuration/deployment, or development/Azure database change in this slice.

## Test surface and isolation

Actual copied Next.js `/login`, `/teacher` and `/portal` pages were operated through the browser skill in desktop Codex Chromium. This is not the earlier synthetic API probe: the loopback bridge forwarded browser requests to the application's real Program/TestServer pipeline, real Identity, SQL Server and private Azurite provider. No stub API replies or bypass authentication were injected. The bridge is test infrastructure, not proof of deployed Kestrel/reverse-proxy/Azure behavior.

- Run: `5b8bc0b749d947d4aa8fcc26ff939467`; UTC start `2026-09-30T17:17:15.6813481Z`; elapsed **509.1 seconds**, exit **0**.
- SQL: exact owned `AcademyDesk_QA_5b8bc0b749d947d4aa8fcc26ff939467`, loopback port **54242**, scoped runtime login and ownership guard. Migration histories **80 application / 7 Identity**; no new migration.
- Browser web/API: `http://127.0.0.1:49001` / `http://127.0.0.1:49002`, approved exact web Origin; bridge bound to loopback and rejects wrong Host. Real health control 200; wrong-Host control 400. Same API/web ports rejected before provisioning/copying.
- Source-only web copy: `D:\AcademyDesk\.build-check\browser-web-80ae2969666b40178073c5c67096331a`; no `.env` files copied, no use of localhost:3000 or the developer DB. Installed dependencies reused through a junction; caches confined to the copy. Telemetry disabled for this process.
- Synthetic fixtures only: current Teacher, enrolled student and document-authorized guardian in Academy A; tenant controls confirmed Academy B isolation before browser work. Normal UI sign-in created the sessions; no account tokens inspected or manually injected into browser storage.

[HTTP/SQL/cleanup log](../EVIDENCE/logs/phase-2b-portal-media-http.log), [browser observations](../EVIDENCE/logs/phase-2b-portal-media-browser.log), [current source hashes](PHASE_2B_PORTAL_MEDIA_SOURCE_SNAPSHOT.json). All application/test source hashes match the preceding checkpoint; only QA harness/tools changed. Historical snapshots were not rewritten.

Reusable setup from repository root, in two separate terminals: build `QA/tools/SqlHarness/SqlHarness.csproj`, then `./QA/tools/SqlHarness/Run-BlobMedia.ps1 -Browser`; launch `./QA/tools/Run-BrowserMediaFrontend.ps1` in the other terminal. The backend prints its exact owned stop-marker path. Use only its synthetic fixture accounts and generated attachments. End through that marker or the 30-minute deadline; never reuse dev/Azure SQL, a user's existing browser session or a live file. The frontend launcher leaves its source copy/cache recoverable and refuses an existing listener.

## Observed browser results

| Exercised flow | Actual outcome |
| --- | --- |
| Teacher picture upload with title/comment blank | Create 201, chunk 200, complete 200; success message visible, form cleared, history refreshed, filename used as title |
| Teacher picture preview | Authorized HEAD/GET 200; image decoded, natural width 1 |
| Teacher native picture download | Ticket POST 200, attachment POST 200; browser completed file, 67 bytes and exact fixture SHA-256 |
| Teacher WAV upload and saved preview | Success message/history refresh; audio readyState 4, duration 1 second, no media decoder error. Silent fixture, not listening-quality certification |
| Inert unknown-format upload with custom title `Lesson – practice` | Uploaded successfully; Preview explicitly says it cannot safely preview and offers original Download |
| Unknown-format native download | Saved original filename `synthetic-unsupported.bin`, not the custom title; 34 bytes and exact fixture SHA-256 |
| Student Library | All three published attachments visible; WAV decoded; native WAV download completed, 16,044 bytes and exact fixture SHA-256 |
| Document-authorized guardian Library | Linked child and all three attachments visible; private picture preview decoded; native picture download completed with exact bytes |
| Guardian responsive sample | Actual measured innerWidth **390**, document/body scrollWidth **379**; no horizontal overflow; Library navigation and action buttons usable. Desktop Chromium resized, **not Android/iOS** |
| Console samples | Teacher, student and guardian captured error/warning logs returned `[]` in this tested path; not an exhaustive console/event audit |
| Persistence observation before cleanup | SQL contained exactly the three uploaded sessions, each with non-null CompletedAtUtc and expected filename/length |

No video, microphone permission, MediaRecorder, pause/resume, network-loss, >32 MiB preview or 2 GB transfer was exercised. The earlier 85 unit / 22 client / 75 backend / 29 legacy passes remain historical evidence; they were not rerun or counted as new tests here. Full global lint/build, all-role permissions and critical suite are not implicitly passed.

## Evidence and fixture integrity

| Original synthetic file | Bytes | SHA-256 |
| --- | --- | --- |
| `synthetic-picture.png` | 67 | `2142f54f2bd88700b1fdf7eb233fb97568eaba7a4e1d5faa03a8993eaa320b5a` |
| `synthetic-audio.wav` | 16044 | `56d4af65701c26df20bd4021eda95b6e830348ce3a746086079fe89285548dc9` |
| `synthetic-unsupported.bin` | 34 | `7a56ff0c2888f3aee7326a0c1f6f914fc26423c0bee805d012fe3c4534f20ed0` |

Four browser-completed downloads were independently hashed: Teacher picture/fallback, student WAV and guardian picture. They were moved only after matching the recorded synthetic bytes to `.build-check/browser-media-fixtures/downloads/` with role-specific names; recoverable on D:, not deleted or committed.

- [Teacher upload success/history](../EVIDENCE/screenshots/phase-2b-teacher-media-success.png)
- [Student Library](../EVIDENCE/screenshots/phase-2b-student-media-library.png)
- [Guardian Library at 390px](../EVIDENCE/screenshots/phase-2b-guardian-media-390.png)
- [Teacher audio preview at the actual default viewport](../EVIDENCE/screenshots/phase-2b-teacher-audio-default.png)

The Teacher tab measured 681px while the guardian tab measured 390px. Applying the browser viewport override did not resize the already-open Teacher tab; its screenshot was relabelled to the actual default viewport, not claimed as a 390px result. UI actions were re-derived after the responsive layout changed. One initial navigation timed out during Next compilation; the page subsequently loaded 200. One premature selector wait timed out before the student page finished rendering; visible state subsequently confirmed the normal student route. These are retained observation limits, not hidden product failures.

An incidental guardian profile-menu label says “Student portal” even though the page identifies “Parent portal.” This is a copy/accessibility issue deferred to the existing portal UX work, not evidence of an authorization failure. No broad visual certification is claimed.

## Harness changes and cleanup

Added `--browser-media` to the guarded SQL harness, a streamed loopback forwarding host with no management routes, synthetic recipient/session seeding, and an owned stop-marker/30-minute deadline. Request logs contain method/path/status only, not query/body/account credentials. Added binary fixture generation and a reusable source-only frontend launcher. Initial harness build had four compile errors (missing extension namespace/incorrect observation property names); corrected before provisioning, final build zero errors/warnings. New frontend launcher was syntax/negative-guard checked; the equivalent manual copy/start sequence was the actual browser run.

Normal UI sign-out cleared the synthetic Teacher and Portal sessions; agent-created tabs closed and viewport override reset. The exact owned stop marker disposed the bridge/TestServer before guarded host-folder cleanup. Exact private Blob container, SQL database/runtime login and labelled SQL/Azurite containers were removed successfully. Their disposable contents are not recoverable. Owned Next listener/launcher were stopped after matching loopback port and recorded process IDs/command. No other server, Docker prune or personal data was touched; no remaining QA listeners or Docker containers observed. The source copy/cache and generated fixtures remain recoverable on D:.

[Final integrity check](../EVIDENCE/logs/phase-2b-portal-media-integrity.log): 48 source hashes match; original student edits and production configuration preserved; test cases parse; HEAD unchanged. The Next terminal's nonzero exit after explicitly stopping the owned process is cleanup, not a failed application test.

## Next bounded task — agreed Sol High

Exercise upload pause/reselect/resume and reload recovery, then video and large-file pacing using the same isolated host. Obtain actual Android/iOS evidence separately; resized desktop Chromium is insufficient. Recording needs device/microphone consent and format-specific verification. Keep BUG-SEC-0001 open until wider role/revocation/client cleanup, shared-device draft, quarantine/scanning, orphan cleanup, critical regression and deployment gates are satisfied. Shared Data Protection keys, HTTPS/CORS/origin and request-body log redaction remain Azure rollout checks. Production `MediaStorage.Enabled=false` is unchanged. Four finance/payroll P0 issues remain OPEN / RUNTIME-REPRODUCED and their policies are not waived.
