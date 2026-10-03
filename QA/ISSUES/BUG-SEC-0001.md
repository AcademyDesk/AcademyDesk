# BUG-SEC-0001 — Class material URLs are served outside authorization

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | FIX-IN-PROGRESS |
| Final verification | PARTIAL PASS — APIs/client protocol and bounded real portal media smoke; actual devices/wider client/full critical verification pending |
| Severity | Critical confidentiality risk |
| Priority | P0 |
| Category | SEC |
| Module | SHARED |
| Role | See reproduction; same-tenant Admin unless stated otherwise |
| Screen / route | /teacher; /portal |
| API | GET /uploads/teacher-materials/{known-file} |
| Environment | Local isolated ASP.NET TestServer and run-owned Docker SQL; runtime production state not inferred |
| Device/viewport | Desktop Chromium real Teacher/student/guardian media smoke; guardian 390px responsive sample. Actual Android/iOS and full viewport matrix NOT RUN |
| Baseline | 20bb6047f9edf733ac8e2a226621cc582ec54b3c + pre-existing student UI diff |
| Discovery test / review ID | SECURITY-FILE-001 |
| Evidence classification | Runtime reproduction; [Phase 2A private-file report](../REPORTS/PHASE_2A_PRIVATE_FILE_REPRO.md) |
| Preconditions | Isolated synthetic fixture from QA/10_TEST_DATA_STRATEGY.md; never customer data |
| Reproduction frequency | 2 of 2 fresh Teacher-upload fixtures |
| Source | apps/api/Program.cs:77 |
| Class/function | StaticFiles pipeline / TeacherPortalController.UploadClassMaterial |
| Related source | INVENTORY/API_ENDPOINTS.md, CONTRACTS.md, FORMS_AND_FIELDS.md and ASYNC_FORM_RISKS.md |
| Root-cause confidence | HIGH |
| Evidence location | [Phase 2A private-file report](../REPORTS/PHASE_2A_PRIVATE_FILE_REPRO.md); source excerpt below |
| Screenshot | Baseline not captured; local retest [guardian 390px](../EVIDENCE/screenshots/phase-2b-guardian-media-390.png), other screenshots in browser report |
| Console logs | Bounded real portal retest error/warning samples empty; exhaustive events and device logs pending |
| API request | Teacher A upload to `/api/teacher/resources/upload`; direct GET and Range as teacher, anonymous, and tenant B; unpublish then anonymous GET |
| API response | Upload 200; unauthorized ordinary GET 200 and Range 206 with exact synthetic bytes, including anonymous GET after unpublish |
| Database before/after | Fresh SQL context confirmed academy/batch-owned resource; after unpublish, `IsPublished=false` while direct URL still served bytes |
| Dependencies | Safe SQL/HTTP/browser harness as applicable; desired policy review where noted |
| Fix commit | Not committed — first local Phase 2B legacy access gate implemented |
| Retest result | Original case PASS on 2 fresh SQL/HTTP runs; original Phase 2A FAIL evidence retained |
| Regression result | Historical checks retained; final sources pass API 85 / client 22, 75 real HTTP assertions and original leak plus 29 legacy HTTP checks; 68 MiB synthetic desktop browser probe PASS; earlier disk failure retained separately, recovered fresh run PASS; full portal/device/critical acceptance pending |
| Closure notes | Remain OPEN; follow closure requirements in QA README |

## Exact reproduction

In disposable storage upload synthetic material as Teacher A. Copy its returned URL. Fetch without credentials and as tenant B. Repeat after revoking access.

## Expected

Private class material requires authorized tenant/student access on every retrieval.

## Actual / evidence

Upload writes beneath WebRootPath and returns /uploads URL; UseStaticFiles runs before authentication/authorization. A known URL bypasses controller resource checks. Two isolated local runs retrieved the exact synthetic file anonymously and across tenants, including byte-range requests and after unpublishing. No customer file was accessed.

Source snapshot:

```text
76: app.UseHttpsRedirection();
77: app.UseStaticFiles();
78: app.UseCors("WebClient");
79: app.UseRateLimiter();
```

## Suspected root cause

Public static hosting for private learning materials; unpredictable filenames are not authorization.

## Business impact and blast radius

Any upload saved in public webroot; inspect assignment/profile media separately.

## Related / required regression

SECURITY-FILE-001: Authorized download succeeds; anonymous/cross-tenant/revoked retrieval denied, including direct URL and range request.

Also run all endpoint/form cases pointing to this issue in QA/03_TEST_MATRIX.md and the critical regression suite before closure.

## Phase 2B local repair checkpoint — 2026-09-30

Latest 2026-10-01 [same-browser draft isolation](../REPORTS/PHASE_2B_DRAFT_ISOLATION_BROWSER.md): A sign-out denies direct Teacher requests; B own form empty before/after reload; A original metadata recovers and completes same session, one fresh SQL resource/Student notification, exact 17 MiB download. Client 28/28/build PASS, QA-only edits. Role-scoped sign-out leaves other role sessions and owner-keyed drafts intact; not global revocation/secure erase/shared-OS privacy proof. Misleading signed-out profile notice separately queued. Remain OPEN / FIX-IN-PROGRESS / PARTIAL PASS; wider roles/expiry/concurrency/recording/2 GB/actual-device/critical/release pending.

Latest 2026-10-01 [assignment revocation during recovery](../REPORTS/PHASE_2B_UPLOAD_REVOCATION_BROWSER.md): real authenticated status/chunk/completion denied, Resume denied with local draft retained; one unfinished session, zero resources/notifications, uncommitted block 0 only. Clear confirmation human-assisted, actual cleared DOM verified. Client 26/26/build/guards PASS, product unchanged. OPEN / FIX-IN-PROGRESS / PARTIAL PASS; wider role/stamp/expiry/concurrency, shared-device, recording/devices/2 GB/critical/release gates pending.

Latest 2026-10-01 [lost completion/reload/retry](../REPORTS/PHASE_2B_COMPLETION_LOSS_BROWSER.md): same-session recovery with no second chunk, one SQL resource/notification/private Blob and exact native bytes PASS. Application unchanged, QA-only additions, client 25/25 and build pass. Initial transparent retry retained separately; prior security suites not rerun. Remain OPEN / FIX-IN-PROGRESS / PARTIAL PASS; upload permission-revocation/recording/device/2 GB/critical/release gates pending.

Latest 2026-10-01 [real Teacher 503 recovery](../REPORTS/PHASE_2B_HTTP_FAILURE_BROWSER.md): staged file/preview/metadata retained, retry reused session, one completed SQL resource/private Blob and exact native download PASS. QA-only changes; unchanged application, client 24/24 and QA build pass. Prior security/API suites not rerun. Fresh run cleaned; interrupted prior resources retained after cleanup refusal. OPEN / FIX-IN-PROGRESS / PARTIAL PASS; ambiguous completion/recording/2 GB/actual-device/critical/release gates pending.

Latest [33 MiB portal check](../REPORTS/PHASE_2B_LARGE_BROWSER.md): healthy-network optional-field upload and exact Teacher/Student native downloads passed; oversized Preview fetches no content. Reproduced misleading “streaming Save” guidance; two local frontend edits now name Download and explain selected-file size threshold. Targeted client 23/23/typecheck pass, targeted lint 0 errors/1 warning. Backend/private authorization/limits unchanged; preceding API/security suite results remain historical, not recounted as final-source repeats. OPEN / FIX-IN-PROGRESS / PARTIAL PASS; HTTP-failure/recording/2 GB/actual-device/critical/release gates pending.

Latest checkpoint: [real portal MP4 check](../REPORTS/PHASE_2B_VIDEO_BROWSER.md) passed one tiny synthetic H.264 Teacher selected/saved playback, upload/reset/history, exact native download and authorized Student playback. No application edits; previous suites not rerun. This adds positive video-path evidence only; every codec/recording/large-transfer/actual-phone/broader recovery/critical/release acceptance remains pending. OPEN / FIX-IN-PROGRESS / PARTIAL PASS unchanged. Older video-pending checkpoints below are historical.

Superseding checkpoint: [recovered browser resume](../REPORTS/PHASE_2B_MEDIA_RESUME_BROWSER.md) passed one 17 MiB synthetic pause/reload/wrong-file rejection/original resume flow. Chunk 0 sent once; same upload ID; one completed SQL session/resource; original title/comment and exact native-download bytes. No application edits, critical-suite or actual-phone acceptance. Earlier blocked attempt below is retained historically. Remain OPEN / FIX-IN-PROGRESS / PARTIAL PASS.

Latest continuation: [pause/resume browser attempt](../REPORTS/PHASE_2B_MEDIA_RESUME_ATTEMPT.md) is BLOCKED / UI NOT RUN, not a new functional PASS or product failure. Test-only pacing and a 17 MiB fixture prepared; browser control timed out before Teacher login/upload. Disposable resources cleaned. Continuation C: capacity is below the unchanged 2 GiB preflight; pause/resume/reload acceptance remains pending. No application change or issue closure.

Fourth slice: [native download integration](../REPORTS/PHASE_2B_NATIVE_DOWNLOAD_INTEGRATION.md) implements one-file/origin-bound 60-second POST read credentials, current access/stamp checks and native browser attachment handoff, without account credentials in URLs or public storage. Final sources pass API 85 / client 22, 75 real HTTP assertions and original leak plus 29 legacy checks; real desktop browser 68 MiB synthetic probe matches saved hash (synthetic API, not full portal). An earlier disk-exhaustion attempt remains recorded separately; a fresh final-source run passed after capacity recovered. Pre-hardening runs are not claimed as identical-final-source repeats. Full portal/devices, critical suite and release gates remain pending. No commit, Azure deployment or development/Azure migration. Remain OPEN / FIX-IN-PROGRESS; preceding implementation descriptions are historical.

Fifth slice: [real portal browser smoke](../REPORTS/PHASE_2B_PORTAL_MEDIA_BROWSER_SMOKE.md) uses actual Next pages and the real Program/TestServer/Identity/SQL/private Azurite host behind a loopback-only test bridge. Three Teacher uploads passed optional blank fields, success/reset/history; authorized student/guardian views decoded media and four native downloads matched exact saved bytes. Guardian 390px responsive sample passed, not actual Android/iOS. Only QA infrastructure changed; application hashes unchanged. Historical backend/unit passes were not rerun. Video, recording, large-file/pause/resume/shared-device/wider role/critical/deployment gates still pending; remain OPEN / FIX-IN-PROGRESS / PARTIAL PASS.

Third slice: [Teacher media client integration](../REPORTS/PHASE_2B_MEDIA_CLIENT_INTEGRATION.md) connects resumable Teacher attachments and authenticated Teacher/family file actions. 18 protocol/unit/SSR tests pass (not real browser tests); updated 49 real HTTP/SQL/Azurite assertions pass twice; API 71/71 and typecheck pass. CORS test-host failure output is retained. Browser/device acceptance and large private mobile downloads remain pending; targeted lint still fails. No commit/deployment or development/Azure migration. Remain OPEN / FIX-IN-PROGRESS. Earlier client-status paragraphs below are historical.

Second slice: [authenticated Blob sessions](../REPORTS/PHASE_2B_BLOB_UPLOAD_SESSIONS.md) creates durable immutable ownership-bound sessions, resumable bounded chunks, idempotent completion/recovery and authenticated content/ranges. 47 real HTTP/SQL/Azurite assertions passed on two fresh runs; prior concurrency failures are retained. Legacy read gate passed again. Production storage remains disabled and browser clients still use the legacy upload route. Only owned disposable SQL received the new migration; development/Azure untouched. Remain OPEN / FIX-IN-PROGRESS. The paragraph below describes the first-slice checkpoint historically.

[Legacy access-gate report](../REPORTS/PHASE_2B_CLASS_MEDIA_ACCESS_GATE.md): public static access is excluded for teacher and learning-material folders, and existing GET/HEAD URLs now require current tenant/role/recipient checks before any ordinary or Range bytes. Authorized teacher/student/guardian controls and revocation cases passed locally. Staff retain authorized draft management; unpublishing revokes family access. No live file was migrated or deleted. Disk-backed upload writers, authenticated Blob sessions and bearer-aware browser viewers still need integration; this is not a complete media-delivery release or issue closure. The initial combined run hit HTTP 429 later in finance and is retained as failed evidence. Remain OPEN / FIX-IN-PROGRESS until the required closure chain is satisfied.
