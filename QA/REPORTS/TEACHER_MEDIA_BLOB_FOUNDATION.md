# Teacher media — private Blob storage foundation

Date: 2026-09-30. Approved destination: storage account `academydeskmedia`, private container `academydesk-media`, resource group `academydesk-prod`. The user requested device picture/audio/video capture, broad attachment formats, large reliable uploads, and playback/download fallback. This bounded implementation establishes the storage adapter; it does not yet replace the Teacher screen or its routes.

## Implemented

- Added Azure SDK dependencies `Azure.Storage.Blobs 12.29.2` and `Azure.Identity 1.21.0`, checked against the stable package feed.
- Added `MediaStorageOptions`, strict credential-free Azure HTTPS endpoint validation, and registration using system-assigned managed identity in production; a user-assigned client ID is optional. Development can use an existing Azure developer credential. No account key/connection-string setting was introduced.
- Recorded non-secret destination configuration in `appsettings.Production.json`, **disabled** until authenticated routes and deployment checks are complete. Merely starting the app creates no container and writes no Azure data.
- Added a storage adapter supporting deterministic blocks, retry/resume inspection, exact tail and total length, conditional/idempotent commit, immutable completed-file protection, streamed reads and Range reads. Files use generated academy/resource identifiers, not client-controlled blob paths.
- Set a 2,000,000,000-byte per-file policy with 8,388,608-byte chunks (239 chunks at the maximum). This is a policy limit, not an executed full 2 GB transfer.
- The filename policy accepts ordinary picture/audio/video formats (including HEIC, MOV, OGG/FLAC) and other non-active attachments without the old extension allowlist. Executable scripts and active HTML/SVG are rejected. Original bytes are retained as `application/octet-stream` attachments; this does **not** certify rendering or codec playback. A future viewer must choose verified safe inline types and a download fallback.
- Every storage operation verifies that the destination container is private. There is no public-filesystem fallback.
- The isolated SQL host explicitly disables Blob storage and rejects an override enabling it, preserving local QA isolation.

## Actual verification

`dotnet test tests/AcademyDesk.Api.Tests/AcademyDesk.Api.Tests.csproj --verbosity quiet`: **45 passed, 0 failed, 0 skipped** (19 prior checks plus 26 new media configuration/manifest checks).

`dotnet build QA/tools/BlobHarness/BlobHarness.csproj --verbosity quiet`: **0 warnings, 0 errors**.

`QA/tools/BlobHarness` executed **16 checks, all passed**, against a new run-labelled Docker Azurite emulator bound only to `127.0.0.1:52848`, run ID `8ed53e63157f48e4908363751702fd46`. The harness checks Docker name, image, run label and exact loopback port before using generated emulator credentials. It refuses existing test containers and cleans only containers with the matching ownership marker. No real Azure account or SQL database was contacted.

Observed checks: fresh state; wrong-length refusal with no staged data; block retry and resume from a new adapter instance; incomplete-commit refusal with no visible committed object; exact length and Unicode filename; durable completion; idempotent completion; completed-file overwrite refusal; exact full content; exact 42-byte Range response; anonymous private-content denial; a different academy key cannot resolve the object; public-container refusal with no bytes written. Synthetic payload: 8 MB plus a 37-byte tail, SHA-256 `B837C3D1D6777FE41DAB0A0B50AB33573F58066B15BE0E042435FD624C3F6704`.

Both run-owned emulator Blob containers were removed after marker checks, then the exact Docker emulator was stopped and removed. The cached emulator image remains available. `git diff --check` reports no whitespace errors.

## Next slice and limitations

Connect the adapter to authenticated Teacher upload sessions with user/academy/batch/student/session binding on every chunk and completion. Enforce teacher/academy activity, publication and recipient scope when opening media. Add upload progress/pause/retry and retain the file on failure. Then connect Teacher and family viewers with safe streaming/Range authorization and format fallback, select recording filenames from the actual MediaRecorder MIME type, and validate on physical Android/iOS/desktop devices.

Authentication, endpoint-level tenant isolation, expiring viewing permissions, storage quota, malware inspection, device codec compatibility, true 2 GB transfer, Azure role/CORS/network configuration, and retention of existing files are **not** established by this adapter check. The storage adapter's academy-key check is not a substitute for authenticated endpoint authorization. Existing Teacher upload code still writes to public `wwwroot`; [BUG-SEC-0001](../ISSUES/BUG-SEC-0001.md) remains OPEN/RUNTIME-REPRODUCED. No UI changes, schema migration, commit, GitHub push or Azure deployment were made in this slice.
