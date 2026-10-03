# Batch 22 — Leave, make-up classes, holidays and events

2026-09-29. Source-backed specification only; runtime NOT RUN. Completes LeaveRequestsController (List/Create/Decide), MakeupClassesController (List/Create/UpdateStatus), HolidaysController (List/Create/India/Delete), HolidayDeleteController (Delete) and EventsController (List/Create/UpdateStatus):14 declared actions. Duplicate route declarations count as two source actions, not two independently reachable endpoints. Reuse SCHEDULE-API-004 through SCHEDULE-API-017 and E2E-LEAVE-001.

SCHEDULE-FORM-001 events has8 lexical controls; SCHEDULE-FORM-002 holidays2; SCHEDULE-FORM-003 leave5; SCHEDULE-FORM-004 makeup9. One standalone declaration makeup-status-${item.id} is counted once regardless of row count. Meeting-links/provider configuration remains pending. TeacherPortal/Portal and calendar are prior comparison dependencies, not newly completed controllers.

## G02 — Leave form and API

| Field | Contract and required cases |
| --- | --- |
| type | Native Student/Teacher selector; changing it resets person ID. API lacks allowlist/normalization. BUG-DATA-0038: unknown/case-variant type skips both person checks; exact type still copies unchecked opposite ID |
| id | Required person selector from all students or teachers; UI sends opposite ID null. Exact canonical API type requires matching same-academy entity. Test absent/null/empty/malformed/zero/foreign/missing/inactive and both IDs supplied |
| start / end | Required native date controls, DateOnly DTO, end>=start. Test same-day, reversed, missing/default0001, malformed, leap/year boundaries and long spans. No past/future bound, overlap or existing-leave guard is present |
| reason | Required textarea; API rejects blank and trims; EF2000 required. Test whitespace, Unicode,2000/2001, multiline and escaped display |
| decision notes (API only) | UI always sends null; API trims optional Notes, EF1000. Test omitted/null/empty/whitespace/1000/1001; preserve or clear semantics must be explicit |

List is tenant-filtered, newest CreatedAtUtc first, unpaginated; ties need deterministic display expectations. Decide permits exact Approved/Rejected and scopes row to tenant; it does not require Requested current state, decision actor/time, or reason. UI only shows Approve/Reject for Requested, so direct repeated/opposite decisions need tests and an approved reversal policy.

Create and Decide return empty200. No automatic session cancellation, attendance update, make-up linkage or notification occurs in these actions. LeaveRequest has no corresponding source-session/make-up ID. Define leave-to-schedule warning/block and approval history expectations before treating every absent coupling as a defect. Teacher self-service leave creation is previously reviewed and must be compared for ownership and canonical requester values.

Leave UI load requires students, teachers and leave lists all to succeed. Empty academy list is dereferenced and caught as migration/startup guidance, not an accurate empty state. Decision handler does not inspect PATCH response.ok and reloads even after resolved403/500; test no false success and persistent actionable failure. Create/decide lack busy/catch and durable success. List uses Unknown on missing person lookup; show unavailable dependency separately from truly missing person.

## G02 — Make-up scheduling fields

| Field | Contract and required cases |
| --- | --- |
| student / batch | Selectors independently list all loaded rows. Handler requires both; API checks each same-tenant existence, not active enrollment, learner activity, leave approval or eligible historical class. Test wrong batch learner, inactive/paused enrollment, foreign/missing IDs and define supported cross-batch make-up policy |
| teacher | Optional selector promises Use class teacher; null is posted. BUG-FUNC-0022: manual branch does not inherit batch teacher. Next branch uses session teacher then batch teacher and ignores request override. Test optional/default/explicit overrides in both modes; selected ignored values need clear UX |
| scheduling-mode | Manual or NextScheduled. Server selects earliest future Scheduled session in batch, strict StartUtc>now. Test no candidate, exact now, cancelled/past, ties, overlapping sessions and next-session changes during submit |
| makeup-date / makeup-time | Manual date required, time defaults09:00, explicit IST conversion using +05:30;15-minute selector. Time-first change derives date from UTC today, which can be prior IST day before05:30. Test date-first/time-first, midnight/year/leap rollover, invalid Date and UTC/India/US browsers |
| delivery-mode | UI Offline/Online/Hybrid. API trims then validates case-insensitively, but subsequent link requirement and notification location use exact Online/Hybrid. Direct lowercase online without a link needs rejection/normalization test. Next mode overwrites request mode with source session |
| venue | Only Manual+Offline exposes optional room. API trims, EF250. Test null/blank/spaces250/251 and mode switches retaining hidden values |
| meetingLink | Only Manual+Online/Hybrid shows required field; API trims, EF1000, no URI scheme validation. BUG-FUNC-0023: client requirement also runs in NextScheduled, blocking valid inherited online link while manual input is hidden. Use inert URLs, never open untrusted links |
| notes (API only) | UI always null; optional trimmed text, EF2000; test omission/null/empty/length and readback |

Manual end is start plus positive batch SessionMinutes or60; no overlap, teacher availability, leave, holiday or enrollment-capacity check. Define warning/block policies and test independently of normal ClassSession clash guards, which are not called here.

BUG-DATA-0039: standard physical sessions use InPerson, but next-class room is copied only for Offline. The copied mode bypasses earlier request allowlist and saved venue becomes null. Verify source class, make-up row, response, directory/calendar and notification all retain intended location.

UsesNextScheduledClass is a boolean snapshot, with copied times/location/teacher but no source ClassSessionId, LeaveRequestId or original missed session ID. No ClassSession or attendance record is created. Test source cancellation/reschedule/reassignment after creation and define snapshot versus linked-event behavior; do not promise automatic propagation or ordinary-class attendance support. Directory replaces actual time with Next scheduled class for that mode; users need an unambiguous scheduled date even when the source advances.

Status standalone makeup-status-${item.id} PATCH allows exact Scheduled/Completed/Cancelled, no transition/reason/time guard. Test repeated, reverse, simultaneous changes, foreign ID and unsupported status. Status change emits no cancellation/completion notice. Calendar currently includes all statuses, so cancelled make-ups must not appear indistinguishable from active appointments (reuse calendar lifecycle specification).

Create queues student InApp notification and linked Parent notifications for CanAccessPortal and non-revoked links in same SaveChanges. No teacher notification; no CanViewAcademicProgress check in this selection. Test recipient-type consistency with portal readers, restricted guardian flags, revoked links, duplicate linkage and exactly one permitted notification per intended recipient. This is queue persistence, not evidence of browser display or external delivery. Existing guardian disclosure findings/policy apply.

Create parses response defensively, has try/finally saving reset, and displays server error. Its positive message is erased by load. A successful POST followed by failed readback is reported as failed scheduling; preserve committed/unknown outcome and prevent blind retry duplicates. Status handler checks HTTP failure but lacks network catch/busy/success. No create idempotency key or natural uniqueness constraint; concurrent replay tests must count both make-ups and notifications.

## G02 — Holidays

Name input and holiday-date are the two visible controls. Hidden state always sends notes empty, isClosed true, scope Custom. No UI controls for Notes, Scope, StateOrUt or open-day override. Native name requirement and truthy guard do not trim whitespace.

Create binds AcademyHoliday domain object, replacing only Id and AcademyId. EF Name200 required, Notes1000, Scope30 required, StateOrUt80; unique academy/date. Test required model binding separately from database validation, omission/null/empty/whitespace, all length boundaries, DateOnly format/default and overposted inherited metadata. Never presume C# required or SQL required rejects whitespace. Same date across different tenants should work; duplicate same-tenant date and concurrent create must fail clearly without ambiguous save. List returns full entity, ordered date, including closure metadata.

India action uses hardcoded2026 list, skips dates already present and does not update a custom holiday occupying the same date. Test first/second invocation, partial existing set, same-date custom name, simultaneous runs and unique-index failure. Static dates are not verified against an authoritative holiday calendar in this batch; do not approve the UI Official wording as provenance, regional applicability or future-year support.

BUG-API-0008: HolidaysController DELETE {id:guid} and HolidayDeleteController DELETE {holidayId:guid} have identical matching route shape, HTTP verb and constraints. Full MVC endpoint selection must be tested; isolated controller tests cannot detect ambiguity. Both action bodies scope lookup to academy and return404 or204. Do not delete customer holidays to reproduce.

BUG-UI-0006 extends to holidays: date-only parsed at browser-local noon then rendered in IST can show next day. Test exact source date across zones. Delete lacks confirmation/busy/network handling; assess accidental repeated activation and focus after row removal. Defaults notice is cleared by load; custom create/delete have no positive notice. No runtime deletion or default insertion was performed.

## G02 — Events

Eight form fields: required Event title, event-type selector, optional branch, required start-date/end-date, start-time/end-time defaults10:00/11:00, optional Venue or meeting link. UI sends Capacity/Notes null; type and branch survive successful reset. Test all visible fields, optional empty combinations and repeated creation without silently reusing unintended branch/type.

API trims nonblank Title, requires end>start and validates explicit same-tenant branch existence. Type blank defaults Recital, otherwise arbitrary trimmed string; no allowlist. Capacity nullable has no nonnegative guard; include null/zero/negative/large boundary tests and define event capacity semantics. Venue/Notes trim optional. EF title250/type50/venue250/status40/notes4000; no request max-length checks. Test invalid/foreign/inactive branch, unknown type, realistic long URL and malicious-looking strings rendered as text.

Browser combines dates/times using browser-local Date then toISOString; list formats IST without input timezone label. Specify a consistent event timezone policy and test India/UTC/US/DST boundary conversion plus calendar date grouping (BUG-UI-0007), not just valid ISO syntax.

List is unpaginated/all dates/all statuses despite Upcoming events heading. Create returns201 with ID Location but no matching GET-by-ID action. Status API allows Planned/Published/Completed/Cancelled case-insensitively but stores original case after trimming; UI has no status editor. No transition sequence or associated attendance/notification writes. Test direct lifecycle control, repeated/reverse updates, case variants and cancelled/past event display in calendar. Define whether public/student visibility requires Published rather than treating all rows as public.

## G03 — Permission, failure and persistence matrix

Global AcademyAccessFilter applies tenant/user/subscription/grant checks; no controller being unmapped means anonymous access. MakeupClasses requires Core/makeup.manage. LeaveRequests is Core but has no named permission mapping; Holidays/HolidayDelete/Events use Certificates and likewise lack named mapping, so ordinary delegated feature grants cannot authorize them through that catalog. Administrator/owner paths remain separate. Verify inactive account, suspended tenant, foreign academy/entity, missing modules, expired/revoked grant, anonymous and intended allowed controls through real HTTP.

BUG-FUNC-0018 extends: Operations/Manager have makeup.manage but lack students.manage and unmapped teacher lookup permission; form requires all dependencies even optional teacher. Events requires branches/MultiBranch even with no branch, despite its Certificates feature. Test individual lookup403/500/transport/parse/empty states and least-privilege read contracts rather than granting broad management rights.

Across four forms test double tap, slow/offline/timeout, unauthorized expired-session refresh, validation and model-binding400, database constraint failure, post-save audit failure BUG-API-0002, successful write/failed reload and route switch during requests. Distinguish no-write, saved and unknown outcome. BUG-FUNC-0003 covers missing/erased success; preserve optional fields and entered values on error. No bulk audit was counted as a runtime success.

## G01 — Visual and accessible state specification

For all four pages cover loading, empty academy/list, dependency failure, full/long data, valid submit, field validation, denied action, saving and readback failure. Test mobile320/375/390/430, tablet768 and desktop1280/1440, plus200% zoom and long Unicode names. Check page and list scrolling, dropdown anchor/clipping/layering, focus return, Escape/outside close, keyboard option selection, native leave dates versus shared date controls, accessible labels for placeholder-only controls, error association and live success announcements.

Make-up mode switching must expose only relevant editable fields, explain inherited teacher/time/location and retain or clear hidden values intentionally. Status dropdown on repeated rows needs unique accessible context. Leave approve/reject needs explicit decision result and sensible focus if actions disappear. Holiday remove must not accidentally trigger while scrolling. Events list needs clear start/end, timezone, status and long venue wrapping. No screenshots or physical iOS/Android checks were run; no visual baseline is approved.

## Outcome and next scope

Five new OPEN source findings: BUG-DATA-0038, BUG-DATA-0039, BUG-FUNC-0022, BUG-FUNC-0023 and BUG-API-0008, all P1. Existing feedback/date-only/lookup issues extended without duplicate IDs.14 API actions,4 forms/24 lexical controls and1 standalone control added. Cumulative63/82 forms,363 lexical+113 standalone=476/649 controls,49/70 complete controllers. These are source coverage counters, not runtime pass or overall percentage.

Next: meeting-link/provider dependencies, then remaining learning, communications, documents and operations. G01/G02/G03 remain open; Phase1 NOT CLOSED; Phase2 not started. No app repair, customer writes, outbound notifications, commit or Azure deployment.
