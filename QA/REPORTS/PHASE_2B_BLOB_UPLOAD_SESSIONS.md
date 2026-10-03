# Phase 2B — authenticated private Blob upload sessions

2026-09-30. Local bounded implementation and verification; **not release approval**. Base commit `20bb6047f9edf733ac8e2a226621cc582ec54b3c` plus [29 source/preservation hashes](PHASE_2B_BLOB_UPLOAD_SOURCE_SNAPSHOT.json). SECURITY-FILE-001 / BUG-SEC-0001 remain **PARTIAL PASS / OPEN / FIX-IN-PROGRESS**. Accepted Astra reviews and historical failed captures are preserved, not repeated or replaced.

## Implemented contract

- `POST /api/teacher/media-uploads` creates a durable, server-ID session bound to current academy, login, teacher, assigned active batch and optional active enrolled student/assigned class session. Current Identity role and activity are checked from the database, not merely from old token claims. All progress/chunk/completion operations repeat those checks. Another login with the same teacher entity cannot reuse the owner's session.
- Client request ID is unique per academy/login. Exact metadata retry returns the same ID; changed metadata returns 409. Filename, byte length, teaching scope and title/description/type are immutable. Optional title/description/type/session need not be filled; defaults are explicit. Supplied invalid scope is rejected. Unsafe filenames/active extensions, including a trailing-space executable bypass, are rejected before Blob writes.
- Progress is persisted in SQL plus staged private Blob blocks. Chunk length/index must exactly match the immutable manifest; the server buffers only one bounded 8 MiB chunk. The policy remains a 2,000,000,000-byte maximum, not unlimited storage. Interrupted or repeated chunks can resume without creating another session.
- Completion first commits Blob conditionally, then claims the SQL session via a transactional conditional update. Exactly one resource and notification set are created. A competing Blob commit is accepted only after immutable owner/length/filename verification. A SQL failure rolls back the SQL claim but retains the private committed Blob for retry. No signed public URL, account key, client-selected path or filesystem fallback is provided.
- `GET/HEAD /api/class-media/{resourceId}/content` requires the completed session, exact resource URL binding and the shared current role/tenant/student/guardian access policy. Authorized content is streamed as a no-store, nosniff attachment; single byte ranges and suffix ranges are supported. Invalid/multiple ranges return 416; unsupported If-Range safely falls back to the full response. Client MIME does not enable active inline content. Safe format-specific previews remain a later task.
- Unconfigured private storage returns a retryable 503 without creating a session or writing a public file. No live Azure setting or permission was enabled. The production settings file still has `MediaStorage.Enabled=false`.

SQL migration `20260930125012_AddPrivateClassMediaUploadSessions` creates only the session table and immutable-request unique index. It was scaffolded through the explicitly flagged **offline** design-time factory: no normal Program startup/configuration or database connection. Application migration count is now 80; Identity remains 7. Only disposable owned SQL received the migration. Local development and Azure databases were not migrated.

## Executed verification

Harness build passes with zero warnings/errors. API test suite **71/71 passes**, zero failures/skips: `dotnet test tests/AcademyDesk.Api.Tests/AcademyDesk.Api.Tests.csproj --verbosity quiet`. This includes six upload-policy cases plus two trailing-space filename variants added to the previous 63 tests. Unit policy tests are InMemory and are not substituted for SQL/HTTP evidence.

Run with `./QA/tools/SqlHarness/Run-BlobMedia.ps1` after building the SqlHarness. It creates exactly labelled loopback-only SQL Server 2022 and Azurite targets with synthetic users/bytes and ephemeral nonlogged credentials. Provider injection occurs only after the emulator identity/port guard; QA configuration still refuses live Azure storage. Real Program/TestServer, real Identity, authorization, global filters, SQL constraints/transactions and the actual Blob adapter are exercised. Production rate limits are unchanged; this strict module mode does not silently pass skipped finance tests.

| Fresh run | UTC start | SQL port | Duration | Result |
| --- | --- | --- | --- | --- |
| `30c83a0a8a7a4c418cbebbe66d054d5c` | 2026-09-30T12:56:48.9306555Z | 58522 | 26.4 s | 47 assertions PASS; exit 0 |
| `e2555ecaa4644b3e90e2761267d79a18` | 2026-09-30T12:57:41.9126555Z | 58300 | 25.7 s | 47 assertions PASS; exit 0 |

[Sanitized named run output](../EVIDENCE/logs/phase-2b-blob-upload-runs.log) records prerequisite HTTP/auth/tenant controls; disabled-store no-write; invalid input/scope; optional omitted fields; session/chunk retry and immutable metadata; pending invisibility; progress after a fresh HTTP host and Blob adapter; anonymous/tenant-B/other-owner denial for progress, chunk and complete; batch revocation; concurrent complete; one resource/notification; completed-file immutability; exact enrolled-student bytes; range/suffix/invalid-range/If-Range/HEAD; anonymous/foreign/unpublished denial; direct anonymous Blob denial; and post-commit SQL failure recovery with unchanged Blob ETag. Fresh host recreation is in-process, not a killed operating-system process test.

The transferred synthetic file is 8,388,645 bytes, plus a 3-byte recovery payload. A deterministic synthetic resource-ID collision causes a real SQL insert failure **after** Blob commit: HTTP 500 is the expected fault-injection outcome, not an unanticipated failure of the passing run. A fresh SQL read confirms the completion claim rolled back; removing only that known fixture collision allows retry to return 200 with one resource and one notification. No production fault injector exists.

Current runtime metadata: 303 method/routes (292 controller, 10 framework Identity, one health), digest `F1F7F4D3BE82D4045EA33EFA196A6C03BFAC2DE2A01B770D9BEB343032EE76B8`. Six new routes are authenticated session/content actions; this inventory is not all-route authorization coverage.

The [legacy access gate](PHASE_2B_CLASS_MEDIA_ACCESS_GATE.md) was rechecked on fresh run `fdf51d510d5a453da089473be80c0304`, UTC 2026-09-30T12:58:11.8576682Z, port 55473, 20.7 s. Original leak case plus **29 HTTP regressions PASS**, exit 0, with 80/7 migration histories. [Recheck output](../EVIDENCE/logs/phase-2b-blob-legacy-recheck.log) retains results. Its final browser/Blob NOT RUN line describes that legacy-only harness mode, not the separate Blob executions above.

All successful runs verified ownership, refused a false cleanup marker, removed their private Blob container where applicable, removed/verified absence of their SQL database/runtime login, removed exact Docker containers and guarded host folders. The two pre-existing student UI file hashes match the previous preservation snapshot. No development/customer/Azure data was accessed or removed.

## Failed attempts retained

Three intermediate runs failed the concurrent-completion assertion (expected both 200, observed one 503). [Sanitized diagnostic excerpts](../EVIDENCE/logs/phase-2b-blob-upload-concurrency-failures.log) retain run IDs/timestamps and outcomes. The first diagnosis covered a consumed block-list race but was insufficient. The instrumented third run identified `409/BlobAlreadyExists`; handling that narrow conflict with committed-manifest verification produced the two passing repeats. No broad exception-to-success conversion or storage-auth bypass was added. Failed synthetic containers were retained initially, then stopped and removed only after exact name/image/run-label verification; independent SQL-level cleanup is not claimed for failed attempts. Their synthetic contents cannot be recovered from those removed containers; evidence remains in the repository.

## Remaining gates and next bounded task

**Next: Teacher browser resumable upload integration and bearer-aware material download/viewing — stay on the agreed Sol High allocation.** Existing Teacher UI still calls the legacy multipart/disk writer; this new backend is not yet the browser path. Plain anchors/media tags do not automatically include the API bearer token. Do not tell users production supports the new upload flow until those clients and release configuration are verified.

Still NOT RUN: real Android/iOS/laptop recording and format preview, full 2 GB transfer/performance, cancelled network/body requests, process kill/restart, expired/stale session and orphan cleanup UX, scanning/quarantine, full role/student/guardian variants specifically through Blob, all critical suites, browser console/lint resolution, deployment/migration rollout and live Azure networking/managed identity/RBAC/private-container checks. Previous frontend lint 15 errors/8 warnings is historical and unresolved, not rerun here. Four finance/payroll P0 cases remain OPEN / RUNTIME-REPRODUCED; their previous failing evidence is unchanged. Astra is reserved for focused unresolved policy decisions, not redoing accepted audits.

No commit, GitHub push, Azure configuration/deployment, existing-file migration or application-session/finance change occurred. Phase 2B continues; BUG-SEC-0001 is not closed.
