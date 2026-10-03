# Phase 2B — Teacher media client integration

2026-09-30. Bounded local implementation, **not browser/device certification or release approval**. Base commit `20bb6047f9edf733ac8e2a226621cc582ec54b3c` plus [39 source/preservation hashes](PHASE_2B_MEDIA_CLIENT_SOURCE_SNAPSHOT.json). SECURITY-FILE-001 / BUG-SEC-0001 remain **PARTIAL PASS / OPEN / FIX-IN-PROGRESS**. Accepted Astra audits, original leak evidence and earlier checkpoints are retained; no repeat static audit.

## Implemented scope

- Teacher Classroom attachments now use authenticated persistent Blob upload sessions rather than that form's legacy multipart/disk writer. File selection supports pictures, audio, video and other non-active attachments. Optional title/comment use explicit defaults. The maximum remains 2,000,000,000 bytes with 8,388,608-byte chunks, not unlimited uploads.
- Progress, pause/resume and explicit draft clearing are available. Browser drafts are scoped by API origin, current Identity login, batch, student and class session. Stable request IDs survive lost responses. Metadata is immutable on resume; edge fingerprints identify the selected file and full hashes verify each skipped uploaded chunk to reject mixed-file resumes. Draft corruption/storage refusal fails before transport. Clear warns that unfinished private server sessions may remain.
- File/title/comment clear only after confirmed server completion. Errors retain the selection/draft; successful upload is distinguished from failed history refresh. Scope changes remount and abort the current operation. The existing note submit captures its form before awaiting, avoiding a null `event.currentTarget` reset; this does not certify every portal form.
- Teacher history and student/guardian resource rows now use shared bearer-aware HEAD/download/preview actions. The actual shared API client supplies workspace authentication and refresh retry; tokens are not placed in URLs. Approved private paths refuse redirects and cross-origin credential delivery. External HTTP/HTTPS resources remain ordinary external links.
- Small safe-format previews use bounded bytes and signature-selected MIME, not client extension trust. Unsupported formats offer download rather than active HTML/SVG/document embedding. Preview object URLs and in-flight operations are cleaned up. Signature sniffing is **not malware scanning** or a guarantee that every device decodes every file.
- Where streaming file-save is supported, download writes bounded chunks to the selected file and verifies final length before reporting success. Other browsers use a bounded Blob fallback and report download started, not completed. See the explicit mobile gap below.
- Audio recording negotiates the browser-supported container and uses its actual MIME for the filename extension; microphone tracks stop on stop/error/unmount. Recording pauses/resumes, and the user reviews before upload. Correct MIME/container matching matters for playback; browser support is format-dependent. [MDN MediaRecorder MIME documentation](https://developer.mozilla.org/en-US/docs/Web/API/MediaRecorder/mimeType).
- Labels, controls, progress and action groups have responsive spacing/wrapping and bounded preview sizing. These source changes are not approved visual snapshots.

The Teacher summary adds the current Identity `userId` for draft isolation. Approved-origin CORS exposes `Content-Disposition` for safe original filenames; no allow-all-origin/credentials policy was added. Origin configuration is resolved inside the registered policy callback so final guarded test-host configuration is honoured, rather than captured too early.

## Verification executed

| Check | Observed result / evidence |
| --- | --- |
| Actual TypeScript modules, upload protocol/storage doubles, shared authentication client and React server rendering | **18/18 PASS**: `node --test QA/tools/class-media-client.test.cjs`; [captured output](../EVIDENCE/logs/phase-2b-media-client-protocol.log) |
| Real Program/Identity/HTTP/SQL/private Azurite adapter | **49 assertions PASS on each of two fresh runs**; [captured output](../EVIDENCE/logs/phase-2b-media-client-http-runs.log) |
| API unit suite, final repeat | **71/71 PASS**, zero failed/skipped; [output](../EVIDENCE/logs/phase-2b-media-client-api-tests.log) |
| SqlHarness build | PASS, zero warnings/errors: `dotnet build QA/tools/SqlHarness/SqlHarness.csproj --verbosity quiet` |
| Frontend typecheck | PASS: `npx tsc --noEmit --incremental false` from `apps/web` |
| Four new component/helper files, targeted ESLint | Zero errors, two dynamic-preview image warnings; no lint suppression added |
| Six-file targeted lint including Teacher/family pages | **FAIL: 4 errors / 7 warnings**; [current and baseline comparison](../EVIDENCE/logs/phase-2b-media-client-lint.json) |
| Browser/device end-to-end, full frontend build/global lint, full critical suite | **NOT RUN in this slice**; no inference from protocol/typecheck results |

The 18 tests cover optional defaults; lost PUT response and stable resume; changed middle bytes despite identical file edges; completion failure retaining draft; wrong-file/corrupt-draft refusal; pause; invalid manifest; browser-storage failure; unsafe/empty inputs; owner/scope separation; recording extensions; URL/auth safety; inert unsupported preview formats; byte-size bounds/truncated streams; access denial; Unicode original filenames; actual workspace bearer/refresh retry with a Blob request; and deterministic server rendering. Transport/storage doubles and server rendering are **not a running browser**, real microphone or UI-to-backend end-to-end test.

| Fresh run | UTC start | SQL port | Duration | Result |
| --- | --- | --- | --- | --- |
| `8d6c0dc77c374411a4b2ffdf8d94098e` | 2026-09-30T16:09:44.3849624Z | 51913 | 51.8 s | 49 assertions PASS; exit 0 |
| `3d7fe75b3cc045adb0fff867bfc1e972` | 2026-09-30T16:11:02.2983199Z | 52644 | 54.7 s | 49 assertions PASS; exit 0 |

The prior 47 backend assertions are retained, with two additive assertions: Teacher summary equals the current login ID, and authorized HEAD at the approved web origin exposes the attachment filename header. TestServer checks header correctness, not browser CORS enforcement. The synthetic transfer remains 8,388,645 bytes plus the three-byte recovery payload; **no actual 2 GB transfer was run**. Both migration histories remain 80 application / 7 Identity; runtime routes remain 303 with the previous digest. Intentional SQL-collision HTTP 500 is an expected recovery test, not an unreported passing-suite failure.

All successful owned resources were cleaned up: private Blob container, SQL database/runtime login, exact Docker containers and guarded host folders. An initial run `71d57274461d4de8a5979066dde86efc`, UTC 2026-09-30T16:06:30.7533395Z, port 49676, 83 s, **failed** because the CORS header was absent in the late-configured test host. [Failed output is retained](../EVIDENCE/logs/phase-2b-media-client-cors-failure.log). Moving approved-origin resolution into the policy callback and making the assertion report absent headers safely produced the two passes. Both stopped failed-run containers were subsequently removed after exact name/image/run-label verification. Their disposable synthetic contents are not recoverable; independent SQL cleanup for that failed run is not claimed. No development/customer/Azure resource was removed.

[Final integrity check](../EVIDENCE/logs/phase-2b-media-client-integrity.log) passes all 39 source hashes, retained partial/open issue state, both 49-assertion summaries and absence of the three owned run container pairs. Existing student UI preservation hashes match. All four finance P0 lifecycle states remain OPEN / RUNTIME-REPRODUCED. `git diff --check` exits 0 with existing LF/CRLF warnings. The 18 client/protocol tests also pass on a final repeat.

Targeted Teacher lint improved from baseline 5 errors / 6 warnings to 4 errors / 3 warnings. Remaining effect-state errors and existing page warnings are not waived. Family page has two existing warnings; the four new files have two total image warnings. Historical global lint 15 errors / 8 warnings was not rerun and cannot be quoted as current full-workspace results.

## Explicit remaining gaps

1. **Large downloads on mobile/unsupported browsers are not solved.** Without `showSaveFilePicker`, the current safe fallback limits downloads to 64 MiB; previews limit memory to 32 MiB. Larger files show an explanatory message rather than allocating gigabytes. The picker is invoked during the original user click, before awaiting network requests, but is not universally available. [MDN file-save API requirements/compatibility](https://developer.mozilla.org/en-US/docs/Web/API/Window/showSaveFilePicker). An approved private, streaming browser-download strategy is still needed for iOS/Safari and other unsupported devices. Do not claim the user's cross-device 2 GB download requirement is complete.
2. Real Android/iOS/laptop file selection, microphone permissions/recording formats, preview codecs, screenreader/keyboard/mobile focus/scrolling, cancellation, hydration and success-to-history behavior are NOT RUN. Browser recording is audio only and bounded around 32 MiB (final recorder chunk may exceed that threshold); use device recordings for longer audio or video. The desktop picker currently suggests the resource title, which may lack the original extension when a custom title was used; include that usability check in the next slice.
3. Full 2 GB transfer/performance and automatic retry/backoff are NOT RUN. The production rate limiter is unchanged; many chunks can hit 429. The draft survives for a later manual resume, but automatic pacing/backoff is still a gap.
4. Draft metadata (names/title/comment/hashes, never bearer tokens or file bytes) persists in browser localStorage for resume; shared-device/sign-out retention and unfinished-session cleanup UX need review. Process kill/restart, stale/expired sessions, scanning/quarantine and broader revocation variants remain open gates.
5. Admin/other legacy upload forms and assignment/profile media were not converted in this slice. No existing live files were migrated. BUG-SEC-0001 requires remaining module/critical/browser/release verification, not just these isolated passes.
6. Live Azure Blob networking, managed identity/RBAC/private-container configuration and rollout remain unverified. `MediaStorage.Enabled=false` stays unchanged in production settings. No local development/Azure database migration, commit, push or deployment occurred.

## Next bounded task and phase checkpoint

**Stay on the agreed Sol High allocation:** browser end-to-end verification of this Teacher/family integration on isolated fixtures, plus the private large-file mobile-download gap. Use Astra High only for a focused unresolved security/policy architecture decision, not another accepted-audit pass. Keep the original failures and do not weaken authentication or switch storage to public URLs to make downloads work.

Phase 2B remains IN PROGRESS. Four finance/payroll P0 issues remain OPEN / RUNTIME-REPRODUCED with their original failing evidence and pending policy choices. Browser/full critical suite acceptance is still pending; this checkpoint does not close the private-media issue or approve release. The immediately preceding [backend checkpoint](PHASE_2B_BLOB_UPLOAD_SESSIONS.md) is historical where it describes the Teacher client as unconnected; this report supersedes only that next-task status.
