# BUG-FUNC-0007 — Failed teacher upload discards the staged attachment

## Latest bounded evidence — account/draft isolation, 2026-10-01

[Same-browser account switch](../REPORTS/PHASE_2B_DRAFT_ISOLATION_BROWSER.md) passed A pause/sign-out → B empty form/reload → A original metadata recovery/reselect/same-session completion, one resource/notification and exact download. Client 28/28/build PASS, QA-only edits. Original legacy failure remains unexecuted; OPEN / STATIC-FINDING retained. Draft stays owner-scoped on local sign-out, not erased from browser storage; shared-OS/all-role cleanup is not certified.

## Latest bounded evidence — assignment revocation, 2026-10-01

[Revoked assignment recovery](../REPORTS/PHASE_2B_UPLOAD_REVOCATION_BROWSER.md) passed denied Resume with file/draft/title/comment retained; real status/chunk/completion denied, zero resources/notifications, only uncommitted block 0. Human-assisted clear confirmation followed by actual cleared DOM PASS; server state unchanged. Client 26/26/build/guards PASS; QA-only edits. Original legacy failure reproduction remains pending, OPEN / STATIC-FINDING retained; shared-device/wider failure/critical closure gates not satisfied.

## Latest bounded evidence — completion loss, 2026-10-01

[Completion-response-loss recovery](../REPORTS/PHASE_2B_COMPLETION_LOSS_BROWSER.md) passed saved-response truncation → retained draft/preview/title/comment → actual reload/original reselect → same-session completion, no second chunk, one SQL resource/notification/private Blob and exact download. Client 25/25, QA-only changes; original legacy failure reproduction unchanged. Initial before-headers abort transparently retried, retained separately. Remain OPEN / STATIC-FINDING with partial new-uploader evidence; revocation/wider failure/critical gates pending.

## Latest bounded evidence — 2026-10-01

[Real Teacher HTTP 503 recovery](../REPORTS/PHASE_2B_HTTP_FAILURE_BROWSER.md) passed one new-uploader storage rejection before writes: filename/preview/title/comment retained, retry without reselecting reused the same session, then success/reset/history, one completed SQL session/resource and exact native-download bytes. Targeted client 24/24 and QA build pass; application sources unchanged. This is not reproduction of the original legacy 400/413/500/network-error defect. OPEN / STATIC-FINDING retained; ambiguous completion, wider failure and critical closure gates remain pending.

## Bounded follow-on evidence — 2026-09-30

[Real portal media recovery](../REPORTS/PHASE_2B_MEDIA_RESUME_BROWSER.md) passed one synthetic 17 MiB pause → actual reload → original-file reselect/resume case; wrong file rejected, metadata retained, successful form reset/history, one completed SQL session/resource and exact native-download bytes. This tests the new resumable uploader, not every original legacy HTTP-rejection/network-error condition. No application edits in this slice. Original issue remains OPEN; full reproduction, failure variants and closure gates are not satisfied.

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | STATIC-FINDING |
| Final verification | PARTIAL new uploader pause/reload/resume, 503-before-write and lost-completion-body/reload/retry PASS; original legacy failure variants/network-error reproduction NOT RUN |
| Severity | Major data-entry recovery |
| Priority | P1 |
| Category | FUNC |
| Module | TEACHERPORTAL |
| Role | See reproduction; same-tenant Admin unless stated otherwise |
| Screen / route | /teacher |
| API | POST /api/teacher/resources/upload |
| Environment | Source review of local working tree; runtime production state not inferred |
| Device/viewport | NOT RUN; use QA/06_DEVICE_VIEWPORT_MATRIX.md where UI applies |
| Baseline | 20bb6047f9edf733ac8e2a226621cc582ec54b3c + pre-existing student UI diff |
| Discovery test / review ID | TEACHERPORTAL-UPLOAD-001 |
| Evidence classification | Static trace; runtime reproduction pending |
| Preconditions | Isolated synthetic fixture from QA/10_TEST_DATA_STRATEGY.md; never customer data |
| Reproduction frequency | Not measured; reproduction instructions are proposed |
| Source | apps/web/src/app/teacher/page.tsx:450 |
| Class/function | TeacherClassroom.addFile / upload |
| Related source | INVENTORY/API_ENDPOINTS.md, CONTRACTS.md, FORMS_AND_FIELDS.md and ASYNC_FORM_RISKS.md |
| Root-cause confidence | HIGH |
| Evidence location | Source excerpt below; source fingerprint in INVENTORY/SOURCE_MANIFEST.md |
| Screenshot | Not captured on pinned baseline |
| Console logs | Not captured; required in retest |
| API request | Stage a synthetic file or recorded audio, then return a controlled HTTP 400/413/500 upload response. Observe preview and retry state. Compare successful upload and network rejection. |
| API response | Not captured for this issue; use synthetic request/response in isolated reproduction |
| Database before/after | Not executed; fixture and fresh-context assertions defined below |
| Dependencies | Safe SQL/HTTP/browser harness as applicable; desired policy review where noted |
| Fix commit | Not implemented (Phase 1) |
| Retest result | NOT RUN |
| Regression result | NOT RUN |
| Closure notes | Remain OPEN; follow closure requirements in QA README |

## Exact reproduction

Stage a synthetic file or recorded audio, then return a controlled HTTP 400/413/500 upload response. Observe preview and retry state. Compare successful upload and network rejection.

## Expected

Rejected upload retains the staged file/recording and text for retry; only confirmed persistence clears it. No post-await event-null exception.

## Actual / evidence

upload sets an error notice on non-ok but returns normally. addFile then clears pendingFile and revokes its preview URL irrespective of response; it also calls event.currentTarget.reset after await (existing BUG-FUNC-0001 family).

Source snapshot:

```text
449:   }
450:   async function addFile(event: React.FormEvent<HTMLFormElement>) { event.preventDefault(); const form = new FormData(event.currentTarget); if (pendingFile) { await upload(pendingFile, String(form.get("title") || pendingFile.name), String(form.get("description") || ""), "Attachment"); stageFile(null); event.currentTarget.reset(); } else setMessage("Choose a file or record audio before uploading."); }
451:   async function toggleRecording() {
452:     if (recording) { recorder.current?.stop(); return; }
```

## Suspected root cause

Upload helper does not return a success result for the caller to gate clearing.

## Business impact and blast radius

Teacher material upload and locally recorded audio; distinct from the existing shared async-reset defect.

## Related / required regression

TEACHERPORTAL-UPLOAD-001: Browser HTTP rejection vs thrown network error vs success; retain same File and preview on failure, one successful clear, durable notice, no console exception, no duplicate resource on retry.

Also run all endpoint/form cases pointing to this issue in QA/03_TEST_MATRIX.md and the critical regression suite before closure.
