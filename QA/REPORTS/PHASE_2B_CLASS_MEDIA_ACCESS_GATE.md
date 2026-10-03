# Phase 2B — legacy class-material access gate

Date: 2026-09-30. Base commit `20bb6047f9edf733ac8e2a226621cc582ec54b3c` plus local edits in [the source snapshot](PHASE_2B_CLASS_MEDIA_SOURCE_SNAPSHOT.json). Issue [BUG-SEC-0001](../ISSUES/BUG-SEC-0001.md) remains **OPEN / FIX-IN-PROGRESS**. This is the first bounded repair slice, not full media delivery or release approval. Phase 2A's original failing evidence and accepted Astra audit are retained unchanged.

## Implemented access contract

- Public static-file lookup excludes `uploads/teacher-materials` and the sibling private `uploads/learning-resources` directory. The filtered provider normalizes case, duplicate separators, dot segments and Windows trailing-dot/short-name aliases. Other static assets retain the original provider.
- Existing material GET and HEAD URLs now go through an authenticated controller. It requires exactly one current same-tenant learning-resource row, validates generated GUID filenames and applies current database authorization before returning any bytes. Unknown, orphaned, ambiguous or unauthorized resource bindings return 404; unauthenticated requests return 401. Inactive identities return 403. No signed public redirect or bearer token in a URL is introduced.
- Every read checks current user and academy activity. Batch-scoped resources require an active same-tenant batch; a supplied session must belong to that batch/academy and combined course/batch scope must agree. Same-tenant current Owner/AcademyAdmin roles can manage materials. Current Teacher role plus an active teacher entity permits only assigned active batch/course scope. There is no generic same-tenant or platform-owner bypass.
- Family reads require publication, active students, recipient match and active scoped enrollment. A Guardian additionally requires an active guardian entity, current non-revoked portal link and `CanViewDocuments`. Draft management remains available to authorized staff, consistent with their existing resource listings. Unpublishing revokes family reads, not staff draft management. Broad custom-role permissions are not granted by this slice.
- Responses are streamed attachments (`application/octet-stream`) with `nosniff` and `private, no-store`; authorized byte ranges and HEAD requests remain supported. Format-specific inline preview is NOT implemented here. Previously downloaded or cached public copies cannot be remotely recalled.

This follows the framework's separation of public static files from authorization-controlled file actions. [Microsoft static-file authorization guidance](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/static-files?view=aspnetcore-10.0#static-file-authorization) describes the public static-file bypass and authenticated action approach; the application-specific role/recipient contract and actual results are from the local source/tests above.

**No existing file was moved, migrated or deleted.** This compatibility gate protects the current disk-backed URLs within the API pipeline while Blob integration is built. Existing upload writers still use their original webroot locations, extension restrictions and 50 MB multipart limit. That is temporary legacy behavior, not a new approved filesystem fallback, a completed Blob integration, or fulfillment of the 2 GB/resumable upload requirement. Independently serving these disk folders through a proxy/static host would bypass this API gate and must not be enabled. Production deployment/configuration was not tested or changed.

## Executed verification

Harness build: PASS, zero warnings/errors. API unit suite: **63/63 PASS**, zero failures/skips, via `dotnet test tests/AcademyDesk.Api.Tests/AcademyDesk.Api.Tests.csproj --artifacts-path .build-check/qa-phase2b-media --verbosity minimal`. This includes 18 new provider/access-policy checks (12 provider variants and six policy tests) plus the 45 existing checks. InMemory policy tests alone are not claimed as HTTP/SQL evidence.

Two fresh media-only runs used the existing guard, real Program/TestServer, real Identity sign-in, global filters and run-owned SQL migration sets (79 application / 7 Identity). No mock authentication or production rate-limit change was used.

| Run | UTC start | Loopback SQL port | Duration including readiness | Result |
| --- | --- | --- | --- | --- |
| `bac852fc19994b1ebca264f498a32bfd` | 2026-09-30T12:28:36.3559634Z | 54781 | 20.5 seconds | Original case PASS + 29 additional HTTP checks PASS; exit 0 |
| `8195bbc87f1e4b2e81f1dbed8a259afd` | 2026-09-30T12:29:26.0525567Z | 50062 | 21.9 seconds | Same outcomes; exit 0 |

[Sanitized repeated run output](../EVIDENCE/logs/phase-2b-class-media-runs.log) retains the original-case retest and named assertions. Uploaded synthetic PDF bytes and authorized GET/Range payloads were compared exactly; the original payload SHA256 remains `8206251DDFF9FAC3E0A98331D341D11EC0FE62A418A72071A3794F81D08D4CD2`. Anonymous GET/Range now return 401 instead of leaking 200/206; tenant B returns 404 without bytes. Authorized Teacher GET is 200, authorized Range is 206, Admin HEAD is 200. Family/student/guardian authorized full reads are 200.

The additional 29 HTTP cases cover authorized Range/HEAD and response headers; anonymous/tenant-B HEAD and conditional GET; case/separator/Windows path variants; active family controls; inactive student; targeted recipient mismatch; family unpublication and staff draft access; guardian revocation/document-grant removal; enrollment withdrawal; inactive teacher/user/batch/academy; current Teacher role removal using the old issued token; sibling learning-material authorized/unauthorized reads; and on-disk orphan files without resource rows. An unrelated public static file control still returned exact bytes. Mutations/restorations use fresh scoped SQL/Identity contexts; no customer records or tokens are retained in evidence.

Current runtime metadata contains 297 method/routes: 286 controller, 10 framework Identity and one health. The four additions are protected legacy GET/HEAD routes; digest `D042E44F1595758464D4C3C3056E7A7721AED35B6C6C8A0FFC462A91B64CDAD2` was identical in both scoped runs. Metadata is not proof of all-route access coverage.

Both successful runs refused a false ownership marker, removed the owned database/runtime login and verified their absence, then removed the exact labelled SQL container and owned host folders. No development or Azure database/storage was contacted. Synthetic SQL/container data is discarded and not recoverable from those removed containers; cached images remain.

## Combined-run failure retained, not suppressed

An initial expanded combined run `44d0c0aa1ceb4ea8ad300f50f7df70f4`, UTC start `2026-09-30T12:26:24.3107751Z`, SQL port 54356, duration 51 seconds, passed the original security case and the then-24 media variants. It later hit **HTTP 429** in the finance transition fixture, with infrastructure exit `-532462766`. [Original failed combined output](../EVIDENCE/logs/phase-2b-class-media-combined-attempt.log) is retained. This combined attempt is **FAIL / full critical suite NOT ACCEPTED**, not a second successful complete integration run. Earlier finance/payroll defects continued to reproduce before the rate-limit interruption.

The cause is the existing 120-request/minute global limiter: it runs before authentication, so this TestServer traffic shares the anonymous/IP partition. The production policy was not raised or disabled to obtain green tests. A strict `--class-media` harness mode now executes the prerequisite controls and media module only; it does not pretend skipped finance tests passed. Original-case and media regression violations throw and fail the process. Run locally with `./QA/tools/SqlHarness/Run-ClassMedia.ps1`; each invocation creates a new exact labelled loopback SQL target and refuses reuse. Docker and the cached SQL Server image are prerequisites.

The failed run was retained initially, then stopped/removed after an exact name/run-label ownership check once the rate-limit cause was understood. Its synthetic database/login were discarded with that disposable container; independent SQL-level cleanup was not claimed for the failed attempt. Complete critical-suite execution needs separate owned module runs or pacing across rate-limit windows, without changing production behavior.

## Decision and next slice

**Original leak case and bounded legacy access regressions PASS locally; BUG-SEC-0001 remains OPEN / FIX-IN-PROGRESS.** No fix commit exists yet, and browser/device, full critical suite and completed media-storage/viewing integration are not verified. No frontend lint repair occurred; Phase 2A's recorded 15 errors / 8 warnings remain an unresolved gate, not a new lint result from this batch.

Next, under the agreed **Sol High** allocation: connect authenticated durable upload sessions/chunks/completion to the existing private Blob adapter, binding academy/user/batch/student/session on every request. Preserve failed upload progress, handle idempotent retry and absence of configured private storage without public fallback. Then upgrade Teacher/family clients to authenticated attachment/preview access; current plain anchor or media-tag URLs do not automatically carry the API bearer token and must not be presented as a verified working viewer. Verify actual recording MIME/filename and safe preview/download fallback later on Android/iOS/desktop. Astra High is reserved for focused unresolved policy choices; no repeat of accepted static audits is needed.

No product finance/session changes, schema migration, existing student UI edit, live-file migration/deletion, commit, GitHub push, Azure configuration or deployment occurred in this slice.
