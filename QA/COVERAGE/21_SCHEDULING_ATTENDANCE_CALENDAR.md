# Batch 21 — Sessions, scheduling, attendance and calendar

2026-09-29. Source specification; runtime NOT RUN. Completes ClassSessionsController (List/Create/Update) and AttendanceController (List/Mark), five actions. Reuse SCHEDULE-API-001, SCHEDULE-API-002, SCHEDULE-API-003, ATTENDANCE-API-001, ATTENDANCE-API-002, E2E-SCHEDULE-001 and E2E-TEACHER-001. Calendar rendering is reviewed here; event/make-up mutations and meeting-link integration remain separate unfinished scope. Previously reviewed TeacherPortal actions are comparison dependencies, not new controller completions.

SCHEDULE-FORM-006 adds seven lexical controls. Four standalone declarations are sessionStatus on schedule, session, attendance-${student.id}, and attendanceNotes on attendance. Calendar uses buttons/links rather than inventoried input fields; its interactions are specified without inflating field totals.

## G02 — Schedule form and session contract

| Field | UI, API and database rules / required cases |
| --- | --- |
| batchId | Required native selector; initially first batch ID is selected without applying displayed teacher/branch defaults. API requires same-academy batch existence but not active/open batch. Test empty/malformed/foreign/missing/inactive and loaded-list changes |
| teacherId | Optional selector sends null; API checks explicit same-tenant teacher, then falls back to batch teacher. Null means inherit, although UI says No teacher assigned; define deliberate unassigned versus inherited behavior. Explicit inactive teacher accepted by existence guard. Test override, null and stale dependency |
| branchId | Optional selector sends null; same-tenant explicit branch required, null inherits batch branch. Same label ambiguity as teacher. Test explicit/inherited/null/foreign/inactive branch |
| startLocal / endLocal | Required native datetime-local; helper appends seconds when length16 then +05:30 and converts toISOString. UI explicitly states IST; test identical output in India/UTC/US browser zones, same/reversed times, midnight/day/month/year rollover, leap date and long duration. Invalid date conversion can throw before request; no handler catch. Server requires end>start but has no date bounds or UTC-kind normalization guard |
| deliveryMode | UI InPerson/Online/Hybrid, default InPerson. Create defaults null/blank to InPerson; Update null defaults InPerson but empty string remains empty. No server allowlist. Test unknown/case/leading/trailing spaces and explicit normalization policy |
| roomName | Optional in-person room, required Online/Hybrid meeting link in UI/API. Backend requirement tests untrimmed delivery mode case-insensitively; padded Online can bypass before being trimmed on storage. EF120 applies to both physical room and full URL. Test omitted/null/empty/whitespace,120/121 and realistic long URLs; no URI validation here. Never open submitted test URLs |

BUG-DATA-0037: changing from assigned batch to one with null teacher/branch retains previous values because applyBatchDefaults writes only truthy IDs. Test fully/partially assigned, unassigned, empty option, first auto-selection and deliberate overrides. Compare visible fields, actual POST and persisted session.

List scopes tenant and optionally filters StartUtc >= fromUtc and StartUtc < toUtc; these are start-time filters, not overlap filters. Test exact bounds, earlier-starting session spanning the window, reversed/empty windows, malformed dates, null filters, tie order and all lifecycle statuses. No pagination or finite default range; performance baseline must include large history. Create returns201 summary and ID Location, but no single-session GET exists. Update changes start/end/delivery/room/status only; batch/teacher/branch remain unchanged.

BUG-DATA-0005 extends: teacher clash validation uses explicit request teacher before inheritance. Room clash uses requested branch before batch branch fallback. Omitted branch can miss a conflict at the inherited branch or falsely conflict against an unassigned-branch session. Updates omit all clash checks. Batch updates also reassign future Scheduled teachers without clash validation. Test exact adjacency (allowed by strict overlap), identical/partial/contained overlap, cancelled exclusion, different teacher/branch/room, inherited values and simultaneous writes. There is no unique/exclusion constraint enforcing nonoverlap; preflight queries alone do not prove concurrency safety.

Session entity EF limits delivery/status30 and room120; index academy/start is nonunique. No availability, leave, holiday, enrollment-date or academic-period guard is present in session handlers. Define which are hard blocks versus warnings before adding rejection expectations. Online links sharing a URL participate in the same room collision logic; explicit policy needed for concurrent provider links.

## Session status and teacher comparison

Standalone sessionStatus selector sends a full Update request with existing times/location. Admin allowlist Scheduled/Completed/Cancelled/NoShow differs from teacher Scheduled/InProgress/Completed/Cancelled/Rescheduled. Test a teacher-written InProgress/Rescheduled row through the admin selector and preserve unsupported-current-value clarity. No transition ordering, completion-time or cancellation-reason guard exists; subsequent completed-session edit and reopen policy need explicit approval.

Both APIs accept status case-insensitively but preserve original case. Queries executed by SQL may use case-insensitive collation, whereas in-memory summary predicates compare exact strings. Test canonical/lower/mixed case across schedule, teacher progress, portal history and attendance summaries. Do not assume all database predicates have case-sensitive behavior.

Teacher GetTeacherContext scopes session by academy and explicit assigned teacher; owning batch alone is insufficient. An override teacher can access that session through session endpoints, while teacher Me also filters sessions by batches owned by that teacher, so compare dashboard discovery with direct/calendar access. Calendar API uses UTC month boundaries; browser display-month parity must include the first/last IST hours. Existing TeacherPortal review is reused.

## G02 — Attendance controls and persistence

session selector lists every loaded session and initially selects first API row, which is earliest history rather than today. Display label formats browser-local time without an explicit timezone, while schedule displays IST. Test chronological selection, identical names, missing lookup rows and timezone labeling before approving defaults.

attendance-${student.id} offers Present/Absent/Late/Excused/Online. Choosing status immediately calls mark; separate Save resends current record status and is a no-op for an unmarked row. Verify first-time marking, keyboard choice, cancellation expectations, double taps and per-row saving state. Selected value remains readback-dependent. Shared savingId can be replaced by another concurrent row request; completion of one clears it while another runs.

attendanceNotes is optional text, displayed from typed dictionary value or record.notes. API trims, permits null and caps Notes at500 in EF. Test null/empty/spaces,500/501, Unicode, explicit clear and escaped display.

BUG-DATA-0035: payload uses only notesByStudent[id] || null, ignoring visible record.notes fallback. A status-only update or unchanged Save clears existing persisted notes. New typing and explicit empty clear need separate positive controls.

BUG-DATA-0018 extends: session switching leaves previous records editable during pending/failed GET. Typed notes dictionary is keyed only by student, never cleared on session change, so notes can carry across sessions even after successful new read. Late GET/post-save reload has no selected-session identity check. Test shared learner across sessionA/B with distinct status/notes, reversed responses, failed selection, switching during save and exact target+payload+fresh rows. Notes for a different session must not silently overwrite current records.

API Mark scopes session to tenant, validates status, and requires Active enrollment in its batch. It does not independently query Student existence/activity, session status or enrollment start/end eligibility at class time. Test foreign/missing session, wrong batch/student, inactive/paused/completed enrollment, future/cancelled/completed session and historically eligible learner. Historical correction and cancelled-session marking need a defined policy; do not classify every permissive path as an exploit.

Mark upserts unique academy/session/student, sets MarkedAtUtc on every write, and returns200. List filters academy/session without verifying parent, returns empty for unknown ID and orders by StudentId. Test replay, concurrent first inserts against unique index, competing updates, rejected no-write, precision of timestamp and optional clearing. Student attendance records do not imply teacher attendance or session completion.

Teacher attendance permits Present/Absent only, unlike admin five-state list. Single/bulk teacher writes queue a student notification every time; admin Mark queues none. Test intended parity and repeat-notification behavior without asserting delivery. Bulk validates distinct student IDs but loops original request records; duplicate new students can generate multiple inserts before unique-index rejection, and duplicate existing rows generate repeated notifications. Null record-list/element variants need real HTTP binding tests. Teacher attendance summary and family summary require canonical-status and historical-enrollment fixtures.

## G03 — Permissions and failure behavior

ClassSessions requires Core/scheduling.manage, Attendance Core/attendance.manage through global academyId filter. Test anonymous, deactivated Identity, suspended tenant, missing module, revoked/expired custom grants, foreign academy/entity, allowed admin and authorized delegated controls. Platform-owner bypass is intentional and tested separately.

BUG-FUNC-0018 extends to schedule and attendance lookup dependencies. Schedule needs batches, unmapped teachers/branches, and MultiBranch even for optional no-branch assignment. Operations/Manager have scheduling/batches/attendance permissions but not student/enrollment lookup permission needed by attendance. A custom attendance-only role also lacks sessions/batches lookups. Test each denial independently and specify narrowly scoped read contracts without granting unrelated mutation rights.

Calendar requires successful batches/sessions. Other resolved nonOK responses become empty arrays; this permits partial calendar but hides missing data without a partial-data notice. Network rejection of any Promise.all request still fails overall load; JSON parse failures also propagate. Test403/404/500 versus transport rejection/malformed JSON independently. Do not present unavailable student/teacher data as authoritative No active students/Unassigned.

Existing audit BUG-API-0002 applies after session/attendance domain save. Saved response, persisted row, notification and audit outcome need separate assertions. Schedule create has no catch/busy guard; schedule status retains busy after rejected network request (BUG-FUNC-0012). Schedule create and attendance mark have no success; schedule status message is cleared by load (BUG-FUNC-0003). Attendance catch converts POST and readback failures into an enrollment error even after committed write. Preserve entered data and communicate unknown/readback outcome accurately.

## G01 — Calendar source states and interactions

Calendar combines all sessions, make-ups and events; related list projections are dependencies only. Filter All/Class/Make-up/Event, previous/next/Today,42-day Monday-first grid, first3 items plus a noninteractive more count, sorted month agenda, and agenda expand/collapse must be tested at desktop and mobile sizes. Agenda has one-student direct display and multi-student disclosure; switching month/filter and remounting disclosure state needs predictable behavior. More-count items must remain discoverable in agenda; adjacent-month grid items are not included in selected month agenda.

BUG-DATA-0036: agenda uses batch teacher rather than session teacher override; batch meeting link overrides a session-specific roomName URL. Compare calendar with stored session, schedule list and assigned teacher. Link precedence requires explicit policy. Raw batch link is used without the same http(s) test as session roomName fallback; assess safe URL rendering in browser without launching schemes. No new URL execution claim made.

BUG-FUNC-0021: Session projection drops status; cancelled classes remain normal-looking items with Open links. Define retained history style and joinability, then test every status and count parity. Event/make-up status projection also omits flags and needs analogous lifecycle review in their later mutation batch.

BUG-UI-0007: byDay and agenda use browser-local date/month while time, agenda date and month title use Asia/Kolkata. A session2026-09-30T19:00Z is October1 00:30 IST but appears in September30 grid in a UTC browser. In a timezone east of India, local month-start can format as previous month in the heading. Test day/month/year boundaries, DST, Today marker and consistent timezone policy across views.

Calendar class roster uses current active enrollments rather than historical session-time membership. Names are keys in student list, so identical learner names need duplicate-key/identity testing; enrollment duplicates should not inflate count. Batch meetingPattern replaces dated schedule text when present, even for exceptional session timing. Confirm per-session specificity before approving display.

## Visual and accessibility specification

320/360/390/768/1280 widths,200%zoom,light/dark,mobile keyboard and landscape: usable grid/agenda, no hidden actions or page-scroll lock, long names/notes/URLs wrap, consistent field labels and status contrast. Native schedule selectors/datetime-local controls need direct year access and device-specific behavior testing; custom attendance dropdowns reuse anchoring/scroll/focus cases. Optional fields remain optional outside stated conditions.

Test keyboard main-navigation selection, update-and-collapse, correct current label and direct section changes; all calendar filter/navigation/disclosure buttons require visible focus and meaningful accessible state. Agenda collapse aria-controls target is removed while collapsed; verify screen reader behavior and focus preservation. Multi-student disclosure lacks explicit aria-expanded in source; include in accessibility acceptance. Notice text requires durable announced success and correct focus return, not only changed rows.

No browser screenshots, device tests or physical iOS/Android approval were produced. Meeting-links form remains unreviewed in progress despite an initial source look; provider integrations and recurring rules belong in subsequent scope.

## Outcome / next scope

Five new OPEN static P1 findings: BUG-DATA-0035 notes loss, BUG-DATA-0036 wrong session details, BUG-DATA-0037 stale batch defaults, BUG-UI-0007 timezone grouping, BUG-FUNC-0021 cancelled class presentation. Existing clash, selection, permission, feedback and busy-state issues extended. Cumulative59/82 forms,339 lexical plus112 standalone controls =451/649;44/70 complete controllers. NotificationsController remains partial;26 controllers incomplete.

Next: leave, make-up, holidays and events; meeting links with communication-provider dependencies afterward. Phase1 G01/G02/G03 remain open; Phase2 has not started. No application repairs, new runtime tests, customer writes or deployment. Earlier remaining-job estimates are not guaranteed closure dates or turns.
