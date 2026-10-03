# Batch 39 — Music, practice and resource form semantics

2026-09-30. Source-backed G01/G02 specification for LEARNING-FORM-003–006. Application/browser/HTTP/SQL runtime **NOT RUN**. The four forms have 4 + 3 + 5 + 6 = 18 inventoried native controls. The progress-status selector and teacher-feedback input are separate, already inventoried standalone controls; they are traced here without increasing counts. Batch 37 supplies effective G03 permissions: MusicPieces, MusicProgress and PracticeLogs map Core; LearningResources maps Certificates; all lack a PermissionCatalog entry, so ordinary delegated users cannot reach these academy actions. Preserve the earlier [resource/API review](03_AUTH_ACCOUNTS_FILES.md) and [Batch 26](26_MUSIC_PRACTICE_RESOURCES.md).

Source anchors: `apps/web/src/app/music/page.tsx:51–307`, `practice-logs/page.tsx:2`, `resources/page.tsx:31–218`; four matching API controllers; `apps/api/Domain/Entities/{MusicPiece,StudentMusicProgress,PracticeLog,LearningResource}.cs`; `AcademyDeskDbContext.cs:424–440,485–493,528`. The exact values below are storage constraints on the pinned source, not proof of friendly request rejection. All referenced Test IDs are permanent and NOT RUN.

## G02 — LEARNING-FORM-003 Add repertoire piece

`music.addPiece` builds JSON from React state for `POST /music-pieces` (LEARNING-API-015). `StandardSelectField` has a difficulty mirror, but this handler does not read FormData. The page hardcodes `genre: null` and `durationMinutes: null`; neither is a rendered control.

| Input / initial state | JSON → `CreateMusicPieceRequest` → stored value | Boundary variants for LEARNING-FORM-003 |
| --- | --- | --- |
| Required title, empty string | `title` → nonnullable string; API blank/whitespace 400, otherwise trim | Required `MusicPiece.Title` max 250; trimmed 249/250/251, Unicode, null/missing API body, whitespace, duplicate same-academy title (index not unique) |
| Optional composer, empty string | `composer: composer || null` → nullable; API trims nonnull | Nullable `Composer` max 200; null/blank/space, trimmed 199/200/201 and Unicode. Whitespace-only is sent, then trimmed to an empty string rather than null |
| Instrument, initial Piano | `instrument` string → nullable DTO; API trims nonnull | Nullable `Instrument` max 100; empty string remains empty, no instrument allowlist. Probe 99/100/101, null direct API and retained value after form success |
| Difficulty select, initial Beginner; UI Beginner/Intermediate/Advanced | `difficulty` string → nullable DTO; API blank/space/null defaults Beginner, otherwise trims | Required `Difficulty` max 40; no server vocabulary check. Probe each option, unknown direct value, 39/40/41, case/space variants |

Genre is nullable max 100; duration is nullable int without explicit range/precision guard. Direct API must cover genre 99/100/101 and duration negative/zero/large/binding overflow even though the page always sends null. `IsActive` is entity default true and List returns only active pieces. SetActive has no page control here; test its scoped API separately. On success, title/composer clear while instrument/difficulty retain current choices; `load()` clears the shared message and no positive notice survives. On non-OK, page displays “Piece title is required.” for any failure status, even when title is valid. Transport and failed post-commit reload are uncaught; distinguish persisted record from UI state before retry.

## G02 — LEARNING-FORM-004 Assign piece to student and inline progress

`music.assign` sends `studentId`, `musicPieceId: pieceId`, `targetDate: targetDate || null`, `notes: null` to `POST /music-progress` (LEARNING-API-018). Hidden mirror names `student`, `piece`, `target` are not JSON keys. The loader auto-selects the first returned student and **active** piece if no choice exists. A previously selected ID may remain stale across reloads, but an inactive piece is not returned by MusicPieces.List. Batch 26 now records the same distinction.

| Input | Request, guards and storage | Boundary/state variants for LEARNING-FORM-004 |
| --- | --- | --- |
| Student selector, initially empty | Required Guid in DTO; API checks existence within route academy, not active/enrolled state | Empty lists keep submit as no-op; missing/foreign/inactive student direct API, stale selection and rapid duplicate submit; no UI `required` attribute on selector |
| Piece selector, initially empty | Required `MusicPieceId` Guid; same-academy existence check, not active state | Missing/foreign/inactive piece; cached/stale ID after deactivation. Unique `(AcademyId,StudentId,MusicPieceId)` index and precheck produce duplicate 409, but concurrent duplicate SQL failure shape remains untested |
| Optional target date | Nullable DateOnly; empty→null; no API range/date-order check | Year/month, leap day, malformed direct API date, late/past target and readback; chooser control name differs from outgoing `targetDate` |

`notes` is always null from this form; nullable progress Notes max 2000. Progress Status defaults Assigned (required max 40), Score starts null (decimal(5,2)). A successful assignment clears only targetDate, retains selected student/piece and calls `load()`, clearing the shared message. Exact 409 gets a duplicate notice; other non-OK gives a generic notice. No transport/reload catch. The composed progress selector is keyed by `progress-${item.id}` and calls `PATCH /music-progress/{id}` (LEARNING-API-019), sending selected status plus existing targetDate, score and notes. API accepts five case-insensitive labels and canonicalizes them; missing row 404; no transition or score range check. Test every status, foreign/missing row, zero/negative/large/fractional score against decimal(5,2), notes 1999/2000/2001, date preservation and concurrent edits. **A stored score of 0 is converted to null by `item.score || null` on an ordinary status change**; this is new BUG-DATA-0054 / LEARNING-PROGRESS-ZERO-001. Notes empty string similarly becomes null; assess intended empty/null policy without counting it as a second issue. On success `load()` clears feedback; on rejection message is set and row remains until a fresh read. Reordered loads can display stale progress.

## G02 — LEARNING-FORM-005 Practice log and composed Review

The legacy page sends its whole `f` object as JSON to `POST /practice-logs` (LEARNING-API-021), including `studentId`, `practiceDate`, `minutesPracticed`, `focusArea`, `notes` **and `status: "Logged"`**. The server binds `PracticeLog` directly, replaces only Id and AcademyId and checks only minutes ≥1. This is existing BUG-DATA-0043; the form's status is a client-supplied lifecycle field, not a hidden server guarantee.

| Input / initial state | Wire and server/storage rule | Boundary variants for LEARNING-FORM-005 |
| --- | --- | --- |
| Required student `<select>`, empty | `f.studentId` string → Guid entity; no server student lookup | Missing/default/foreign/inactive/other-tenant ID and binding errors; browser required is insufficient to protect API. No FK for this relation in mapped model |
| Native `date`, initialized with `new Date().toISOString().slice(0,10)` | `f.practiceDate` → DateOnly; server does not adjust it | UTC initial day can differ from academy-local day. Empty/malformed/leap/end-of-day, local-vs-UTC date and direct API omitted/default behavior |
| Number minutes, initial 30, browser min 1 | `+event.target.value` → int entity; server only `<1` 400 | Empty may become 0; 0/negative/fractional/large/overflow, invalid JSON and no-write. No maximum or EF precision constraint is declared |
| Optional focus area, initial empty | State string → `FocusArea`; no trim on Create | Nullable max 250; null/empty/space, 249/250/251 and Unicode |
| Optional notes, initial empty | State string → `Notes`; no trim on Create | Nullable max 2000; null/empty/space, 1999/2000/2001 and Unicode |

`TeacherFeedback` max 2000, `Status` required max 30 and `ReviewedAtUtc` can be overposted on Create; cover alongside BUG-DATA-0043. The `(AcademyId,StudentId,PracticeDate)` index is not unique; no daily duplicate guard is present. Initial load parses academy and two dependent lists without checking `ok` or catching errors. Save on HTTP OK prepends the returned row but does not clear the draft or display a success notice; non-OK and network failure leave no useful feedback. Repeated save can create duplicates. The composed Review input has local `v` state, outside the native form: PATCH sends `teacherFeedback`, server trims it, sets `Reviewed` and UTC review time on the route-scoped row. Review is shown only when current row status is not Reviewed; there is no API guard against repeat review. Test empty/space/2000/2001, repeated review, stale row, denied/500 and input/focus preservation. A successful Review replaces the matching row but does not show a notice; failure silently retains the draft and row.

## G02 — LEARNING-FORM-006 Resource link versus file upload

`resources.create` branches on selected `File`, not a FormData file input read at submit. Selection clears `url` and disables the link. Clearing the file after selection can leave the URL empty; submit then chooses link mode and server rejects it. A displayed title is browser-required for both modes, even though Upload can derive a title from filename. Type/subject/audience mirrors are state controls, not FormData inputs for link mode. The page has no description or publication toggle. Resource read API returns published **and unpublished** rows; “Published resources” is therefore a misleading list heading for an unpublished record.

| UI field / initial value | Link POST `/resources` (LEARNING-API-008) | Multipart POST `/resources/upload` (LEARNING-API-009) / storage |
| --- | --- | --- |
| Required title, empty | `title` JSON → nonblank, trim | `title` form part; blank falls back to filename stem. Required `LearningResource.Title` max 250; trimmed 249/250/251, Unicode/filename fallback |
| Web link, empty, disabled if file | `url` JSON must be absolute via `Uri.TryCreate`; trimmed | Omitted, generated `/uploads/learning-resources/{GUID}{ext}`. Required `Url` max 2000; test 1999/2000/2001, safe scheme policy, malformed/blank URL. API does not restrict absolute URI scheme |
| File input, initially none, accepts documents/image/audio/video wildcard | N/A; no file bytes in JSON | `file` part required and nonempty, request size limit 50,000,000; extension allowlist pdf/doc/docx/jpg/jpeg/png/webp/mp3/wav/mp4. `accept` is broader and not security validation. Test MIME mismatch, disallowed extension, empty, boundary, filename and cancellation |
| Type, default Document, UI Document/Book/SheetMusic/Audio/Video/Link | `type` JSON; blank→Link, otherwise trim | `type` part; blank→Document, otherwise trim. Required max 40; no API vocabulary guard. Test each, unknown, 39/40/41 and null/blank defaults per branch |
| Subject optional, `course` state | `courseId: course || null` | Append `courseId` only if set. API verifies same-academy course; no active check |
| Audience optional, `batch` state | `batchId: batch || null` | Append `batchId` only if set. API verifies same-academy batch; no active or course-match check (BUG-DATA-0044) |

Link JSON also sends `description: null` and `isPublished: true`; description is nullable max 2000. Multipart omits both: `UploadResourceRequest.IsPublished` defaults true and Description remains null. Uploaded `StudentId`/`ClassSessionId` are not exposed by this admin form. Upload writes a file before `SaveChanges`; DB failure can leave an orphan file. Public static delivery remains BUG-SEC-0001. File/title and batch/course combinations require fresh DB and family-reader checks using synthetic storage. The admin page itself only loads resources if **both** batches and courses GET also succeed; optional selectors can block the workspace when another module is denied. Resource creation sets “Publishing resource…”, branches to request, resets title/link/file on OK and calls `load()`, which clears the notice. It retains type/course/batch. Non-OK parses optional JSON and shows server message/fallback, retaining drafts; thrown request or failed refresh is uncaught and can leave a committed row with a stale “Publishing” notice. Confirm readback before retry.

## G01/G03 — screen states and test boundary

| Screen | Conditional and failure states to exercise | Existing scenario anchors |
| --- | --- | --- |
| Music | Academy missing/denied, students/pieces/progress independent failures, zero list and auto-selection, stale chosen IDs, duplicate assignment, update-by-row, failed post-save reload, narrow/zoom select placement and keyboard | LEARNING-FORM-003/004; LEARNING-API-014–019; VISUAL-PAGE-048; UI-DROPDOWN-001 |
| Practice | Empty learner/list, date-year/minute input, page request failure/non-JSON, create/Review pending/success/error and repeated save, 320–430px controls and focus | LEARNING-FORM-005; LEARNING-API-020–022; VISUAL-PAGE-057; UI-TIME-001 |
| Resources | No file/link, each selected mode, scope combinations, size/extension, upload failure, file reset, public/open link, empty/unpublished list and mobile overlay | LEARNING-FORM-006; LEARNING-API-007–010; VISUAL-PAGE-060; SECURITY-FILE-001 |

Test anonymous/foreign/inactive/module-disabled/Owner/Admin/platform-flag variants through the global filter in an isolated HTTP host, with no-write and no-file assertions. No individual learner or teacher is granted these admin endpoints by record ownership alone. A direct-controller 200 does not establish the filter behavior. Neither the visual matrix nor the new source trace constitutes browser PASS.

## Outcome

One distinct OPEN P1 static finding: BUG-DATA-0054, a saved zero progress score lost on status update. Existing BUG-DATA-0043/0044, BUG-SEC-0001, BUG-FUNC-0003 and role/overlay issues cover the other named risks. These four forms and two composed controls are STATIC-SPECIFIED for G01/G02; G03 is specified by Batch 37. Runtime remains NOT RUN. Phase 1 stays **NOT CLOSED** pending two certificate forms, remaining evidence verdicts and non-form UI state grouping.
