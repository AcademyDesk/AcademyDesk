# Batch 35 — Teacher editing and class/batch assignment semantics

2026-09-30. STATIC-SPECIFIED for this bounded slice. Application/browser/HTTP/SQL runtime NOT RUN. Pinned app baseline and pre-existing student edits retained; no app code, database or Azure change.

## Scope and source anchors

Eight non-form declarations in `apps/web/src/app/teachers/page.tsx:217–285`, already registered by Batch 31: `assignmentteacher`, `assignmentbatch`, `editFirstName`, `editLastName`, `Email`, `Phone`, `Specialties`, `teacherbranch`. They map to `VISUAL-PAGE-077`, `TEACHER-API-005` and `BATCH-API-003` as listed below. No new declaration count. Teacher 360/profile and onboarding forms are separate Batch 01/11 contracts; no editor/modal exists for this inline form. Source: TeachersPage lines 23–189, 200–359; TeachersController lines 14–76; BatchesController lines 15–81; AcademyDeskDbContext lines 198–218 and 236–249; AcademyAccessFilter, PermissionCatalog and SubscriptionPlanCatalog. All test IDs below remain NOT RUN.

The directory uses `GET /api/academies`, then four parallel reads of teachers, branches, batches and payroll. Teachers/batches must return OK; branches/payroll non-OK becomes an empty list. A thrown request or invalid JSON enters initial load catch; post-write `load()` has different error handling described below. This is an administrator-oriented combined screen, not a dedicated batch-assignment endpoint.

## G02 — Field → payload → DTO → validation → persistence

For each string length n test n−1/n/n+1 after trimming, whitespace-only, Unicode/SQL length, null/omitted/empty and bypass of client checks. EF limits are storage boundaries, not proven 400 validation. `UpdateTeacherRequest` has no annotations or separate length/email/phone format guard. The inline inputs have no HTML `maxLength`, `required` or `type=email`; first and last have neither visible labels nor placeholders. `StandardSelectField` supplies a named hidden mirror, but these controls are not in a native `<form>` here: `Save` assembles JSON from React state.

| Declaration / existing IDs | Client state and request | Server rule and stored effect | Boundary and state assertions |
| --- | --- | --- | --- |
| `editFirstName` (252), `editLastName` (257): `TEACHER-API-005`, `VISUAL-PAGE-077` | `beginEdit` copies selected teacher; save blocks trimmed-empty but sends raw strings | API checks whitespace, trims into required `Teacher.FirstName`/`LastName`, each EF L(120); linked accounts receive composed `DisplayName` after teacher save | Empty/whitespace/client bypass, 119/120/121, raw leading/trailing space, name update with multiple linked accounts. Cancel discards draft without write |
| `Email` (262): same IDs | Null teacher value displays `""`; save emits `editEmail || null`, so empty becomes null but whitespace remains nonempty | API nullable L(320), `Trim()`; if nonblank, linked account `Email` is updated; null/blank DOES NOT clear linked account email | 319/320/321, malformed format, null/empty/whitespace and teacher-vs-account readback. Do not assume Identity email validation matches this field's optional contract |
| `Phone` (268): same IDs | Empty maps null; whitespace sent | API nullable L(30), trimmed; linked account `PhoneNumber` set even when null/empty | 29/30/31, invalid formats, null/blank/whitespace, account sync success/failure |
| `Specialties` (274): same IDs | Empty maps null; whitespace sent | API nullable L(500), trimmed; stored only on Teacher | 499/500/501, null/blank/whitespace, comma and Unicode; no catalog validation |
| `teacherbranch` (280): same IDs | Starts at `teacher.branchId ?? ""`; options from branch lookup; empty maps null; direct PUT can supply any Guid | `UpdateTeacherRequest.BranchId` nullable; Update assigns directly, unlike Create's same-academy branch lookup. Teacher mapping has no configured branch FK | Valid local, foreign, nonexistent, null and branch-read failure. New [BUG-DATA-0051](../ISSUES/BUG-DATA-0051.md); do not claim cross-tenant data disclosure from storing a GUID |
| `assignmentteacher` (217): `BATCH-API-003`, `VISUAL-PAGE-077` | Options include only loaded active teachers; value is put into batch `TeacherId`; no standalone assignment row | Batches.Update checks teacher exists in route academy, not `IsActive`; writes batch TeacherId then rewrites future scheduled session TeacherId | Empty, inactive via direct API, foreign/missing, stale teacher deactivation between selection/save, replacement of explicit session teacher; exact session boundary is `StartUtc >= DateTime.UtcNow` |
| `assignmentbatch` (226): same IDs | Options include loaded active batches; selected batch snapshot supplies all other PUT fields; missing snapshot silently returns | Batches.Update scopes batch by academy and validates full replacement payload, course/branch/teacher scope, capacity, dates, delivery/enrollment vocabulary and code uniqueness | Empty selection, stale/deleted/inactive batch, 404, validation error, active enrollment capacity, concurrent batch edits, no-write on rejection |

`TeacherSummary` returns only the seven editable values plus ID/active flag; Update mutates those only. Other teacher profile/compensation columns are not directly overwritten by this action. `toggleActive` sends loaded summary fields and inverse `isActive`, not the unsaved inline draft; verify whether a concurrent edit is overwritten. The backend updates matching linked Identity accounts, but changes to absent/cleared email are asymmetric as above.

### Assignment is a full batch replacement, not a narrow relationship write

`assignBatch` sends name, code, course, selected teacher, branch, capacity, waitlist, delivery mode, meeting pattern, room, enrollment status, admin notes, start/end dates and isActive from the loaded `Batch` snapshot. The frontend type lacks `ClassType`, `SessionMinutes`, `SessionsPerWeek`, `MeetingDaysJson` and `MeetingLink`. The API `BatchSummary` includes `MeetingLink` but not the first four; this page still discards the link. `UpdateBatchRequest` supplies omitted extended values as null to `BatchRequest`; `Apply` resets to Group, 60, 1, null and null. Online/Hybrid payloads with no meeting link are rejected by `Validate` before applying; InPerson assignment can commit and erase extended settings. This extends existing [BUG-DATA-0031](../ISSUES/BUG-DATA-0031.md), not a duplicate finding. Use complete fresh-row comparisons, not the incomplete list response.

After a valid PUT, `Batches.Update` also updates **all** route-academy/class-batch sessions with Status `Scheduled` and StartUtc at or after current UTC to the batch teacher, even if a session held a deliberate substitute. Past or other-status sessions are untouched. Reuse [BUG-DATA-0005](../ISSUES/BUG-DATA-0005.md) for resulting clash/override risks. Test no-session, future/past, explicit substitute, boundary, and conflicting-teacher scenarios; do not infer historical session changes.

## G01 — Conditional UI and response states

| State | Source behavior / test expectation |
| --- | --- |
| Initial / no academy / empty list | `Loading teachers…` precedes lookup; 401 academy response yields sign-in message, no academy yields create-academy message. Empty teacher list displays no-teachers state. Academy list is not role-filtered here; platform-flag caller without an academy list entry may not reach assignment despite direct API allowance |
| Lookup failure / partial | Teacher or batch non-OK fails entire load with session-expired only for 401; branch/payroll non-OK degrades to empty arrays. A rejected Promise or malformed JSON errors initial load. Post-write `load()` lacks the same catch in save/toggle. Verify no false success or stale row data after each dependency fails and on retry |
| Enter/cancel edit | `beginEdit` initializes one global six-field draft and switches `editingId`. Edit on another row replaces unsaved draft; Cancel only clears editingId. Reopen from loaded teacher. `Open record` navigates separately. No unsaved-change warning, native form submit or modal to test (N/A) |
| Client validation / pending | Names must trim nonempty; all other fields optional. Save disabled for `savingId === teacher.id`, but input, Cancel, row switches and unrelated assignment remain available while request is in flight. Toggle has same per-row lock, not global lock. Test double-click, edit during save, switching row, and response order |
| Teacher success/failure | `saveTeacher` and `toggleActive` await PUT without try/finally. Rejected network call can leave `savingId` stuck; non-OK gives generic message and discards server's partial-save explanation. Success sets message then calls `load()`, which clears it: reuse BUG-FUNC-0003. If PUT committed but reload rejects, the UI may show stale row and an uncaught promise. Assert fresh teacher/account state separately from visible feedback |
| Assignment success/failure | Save disabled without both choices or while `saving`. Missing selected batch snapshot returns silently. PUT error parses optional JSON message and catches network errors; `finally` releases saving. On success selections clear and notice appears, then `load()` clears notice. If reload fails after commit, catch replaces success with an error although batch/session writes happened. Test delayed selection changes, repeat taps and ambiguous committed-write/lost-response |
| Active/inactive choice | UI offers only active teachers and active batches. Backend checks academy relationship but not `IsActive` for selected teacher; direct API can assign an inactive teacher. A teacher deactivate does not automatically unassign batches or scheduled sessions here. Test displayed choices separately from server contract |
| Accessibility / viewport | First/last lack accessible labels; email/phone/specialties rely on placeholders, branch/assignment hidden-input names do not by themselves establish accessible labeling. Test keyboard, screen-reader names, focus after save/cancel/error, dropdown overlay and 320/375/390/430, 768, 1280/1440 widths with 200% zoom in both themes. No screenshot is approved by this static review |

`apiHeaders(true)` and the generic mutation audit bring existing AUTH-002 and API-RELIABILITY-001/002 obligations. A 400 from a failed linked Identity update is not equivalent to no write; audit filter may skip logging such a partial outcome. Do not automatically retry until fresh read establishes the committed state.

## G03 — Effective action permissions

Applied separately to `Teachers.Update` (`TEACHER-API-005`) and `Batches.Update` (`BATCH-API-003`), assuming valid route/body and no infrastructure failure. Both controllers map to Core module. The shared gate checks user, tenant, active academy and module before role permission, except the platform **flag** bypass. Global Program middleware blocks an inactive authenticated Identity first.

| Caller/context | Teachers.Update | Batches.Update |
| --- | --- | --- |
| Anonymous / unresolved | 401 | 401 |
| Authenticated Identity `IsActive=false`, including platform flag | 403 before controller filter | 403 before filter |
| Active `IsPlatformOwner=true` | Filter bypass; action still scopes teacher lookup to route academy | Filter bypass; action still scopes batch/related IDs to route academy |
| Same-tenant Owner / AcademyAdmin, active academy | Allowed to action validation, including in-place teacher and linked-account writes | Allowed to action validation and scheduled-session propagation |
| Same-tenant ordinary role with `workforce.manage`, custom role or grant | 403: TeachersController has no permission-catalog entry, even with all grants | Allowed only if it has `batches.manage`; workforce alone is insufficient |
| Same-tenant Manager/Operations or granted `batches.manage` | 403 teacher update/list; page cannot complete mandatory teacher lookup | Can call batch API directly; page cannot complete teacher lookup, so this combined UI is not a valid delegated batch-assignment path. Reuse BUG-FUNC-0018 |
| Same-tenant ordinary Teacher/Student/Guardian or PlatformOwner role name without flag | 403 unless separately assigned applicable administrator role for these endpoints | 403 unless separately granted `batches.manage` or administrator role |
| Foreign tenant or missing/inactive academy for non-platform caller | 403 gate, no action write | 403 gate, no action write |

Both actions look up their target row with route academy. `Teachers.Update` lacks local branch scope validation ([BUG-DATA-0051](../ISSUES/BUG-DATA-0051.md)); `Batches.Update` validates supplied course/branch/teacher IDs in the route academy. `Batches.Update` does not check teacher active status or explicit substitute-session ownership. A platform-flag caller bypasses the shared audit in the filter; non-platform successful writes normally enter it. Full-pipeline tests must check denied no-write and audit behavior, not just direct-controller return values.

## Distinct findings and acceptance boundary

- Extend BUG-DATA-0031 for assignment's omitted delivery fields. Existing BATCH-PRESERVE-001 must cover the `/teachers` action as a third trigger.
- Add [BUG-DATA-0051](../ISSUES/BUG-DATA-0051.md) / TEACHER-UPDATE-BRANCH-001 for the missing update branch ownership check.
- Add [BUG-DATA-0052](../ISSUES/BUG-DATA-0052.md) / TEACHER-UPDATE-ATOMICITY-001 for teacher-first commit followed by linked-account failure and a misleading generic client failure.
- Reuse BUG-FUNC-0003 for disappearing success, BUG-FUNC-0018 for delegated lookup blockage, BUG-DATA-0005 for future-session reassignment/clash, and API-RELIABILITY-002 for ambiguous response/retry. No runtime issue is confirmed.

G01/G02/G03 are source-specified for this bounded screen/action slice. That does **not** close Phase 1: later permissions and final inventory-keyed evidence reconciliation remain open. Do not upgrade any scenario from NOT RUN without browser/full-pipeline HTTP/SQL evidence, and do not infer Azure behavior from this baseline.
