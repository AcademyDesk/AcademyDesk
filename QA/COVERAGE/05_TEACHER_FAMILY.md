# Batch 05 — Teacher and family self-service

2026-09-28. Source-backed specification, **NOT runtime verification**. Baseline remains the fingerprint in progress.json. No application repair, database mutation, browser test, deployment or approved screenshot. Reuse batch 03 for resource/file handling; this review extends it, not a second count of those forms. Phase 1 remains NOT CLOSED.

## G02 — forms and composed fields

Sources: `apps/web/src/app/teacher/page.tsx` (TeacherBatchDropdown, teaching create handler, TeacherSelfService), `apps/web/src/app/portal/page.tsx` (Assignment, Practice, Leave, Profile, ParentProfile, Action); request records and guards in TeacherPortalController/PortalController; limits below from AcademyDeskDbContext. Limits are storage constraints, not evidence of friendly API validation.

| Stable case / source form | Actual fields, serialization and destination |
| --- | --- |
| TEACHERPORTAL-FORM-003 / teacher form-3 | title required text; description optional textarea; type default Homework; hidden isPublished=true. Composed assigned batch selector contributes batchId GUID. POST teacher/assignments. Title whitespace rejected and trimmed; description trimmed; empty type defaults Homework. Title 250, description 4000, type 40 storage limits. API additionally accepts nullable studentId/dueAtUtc (not exposed here); selected student must be actively enrolled in selected owned batch |
| TEACHERPORTAL-FORM-004 / teacher form-4 | title required text, objectives optional textarea, composed batchId. POST teacher/lesson-plans. Required title trim, owned batch; title 250/objectives 2000. Nullable courseModuleId/classSessionId exist only in API, copied without relation guards (BUG-DATA-0015) |
| TEACHERPORTAL-FORM-005 / teacher form-5 | title required; maxScore required number min=1; type default Performance; optional scheduledAtUtc datetime-local; hidden isPublished=false; composed batchId. POST teacher/assessments. Handler converts score to Number and nonblank local datetime to ISO; leaves blank datetime as empty string and publication as string. API title/owned batch/score>0; title 250, type 40, score decimal(10,2); blank type defaults Assessment. Test UI/API difference for 0.5, decimal precision/overflow, UTC/day boundaries and absent date |
| TEACHERPORTAL-FORM-006 / teacher form-6 | Eleven named profile fields below plus unnamed picture input. PUT teacher/profile; picture separately POST auth/session/profile-image with multipart image (not profile JSON). Controlled values, required first/last names, optional other strings. Picture accept PNG/JPEG/WebP; reuse batch 03 server upload cases |
| TEACHERPORTAL-FORM-007 / teacher form-7 | startDate/endDate required date inputs; reason required textarea. POST teacher/leave-requests, DateOnly/DateOnly/string. API checks end>=start and nonblank trimmed reason; reason storage max 2000. Default/missing dates, past dates and overlap have no explicit business guard here; policy acceptance needed. Returned summary prepended before post-await reset |
| FAMILY-FORM-001 / portal form-1 | Controlled response textarea plus file input; multipart responseText/file. Either text or non-null file required server-side. Nonempty file checks 50 MB/extension allowlist; zero-length attachment edge needs test. ResponseText stored max 4000 including appended attachment URL; feedback max 2000. Allowed UI media wildcard is broader than server extensions; reuse batch 03 private-file cases |
| FAMILY-FORM-002 / portal form-2 | Shared Action wrapper has zero lexical child controls in AST; actually hosts four editors below. Object.fromEntries -> JSON, converting only minutesPracticed to Number. Do not treat this as a zero-field functional form |

Composed Action editors (21 source controls, separately identified by name **and line** because email/phone occur twice):

| Editor / fields | UI → DTO → guard/storage |
| --- | --- |
| Practice: practiceDate, minutesPracticed, focusArea | Required date and number(min=1), optional text; POST student practice-logs. DateOnly, int, nullable string; date cannot exceed UTC today; minutes 1..1440; focus trimmed/max250. API-only notes nullable/max2000. Test 0/1/1440/1441, fractional/nonfinite/tampered JSON, UTC vs local day, omitted/default/invalid dates |
| Leave: startDate, endDate, reason | Required date/date/textarea; POST student leave-requests; DateOnly/DateOnly/string; same date/reason rules and storage limits as teacher leave. CanManageLeave required for guardian. Parent component receives history but passes no refresh to Action: verify successful save appears in history without a second write |
| Student Profile: firstName, lastName, preferredName, gender, dateOfBirth, email, phone, addressLine1, city, state, postalCode, emergencyContactName, emergencyContactPhone | Required first/last; optional select gender (blank/Female/Male/Non-binary/Prefer not to say), date DOB, email-type and other text; PUT own student profile. API names required trimmed, optional strings trimmed, nullable DateOnly DOB. Empty DOB stays empty string (existing BUG-API-0001). Gender max50; no explicit enum or future-DOB guard. Other limits below |
| ParentProfile: email, phone | Optional email-type/text; PUT own guardian profile; nullable strings trimmed, email max320/phone30. Blank email clears domain value but does not clear Identity email. Same asymmetry in teacher/student profile; define intended contact/login behavior and test it |

Shared profile storage boundaries: firstName/lastName/preferredName 120 each; email320; phone30; addressLine1 240; city/state100; postalCode30; emergencyContactName160; emergencyContactPhone30. Teacher has all except gender/DOB; guardian editor only email/phone. No UI maxlength or explicit action length guard in these editors. Test each nullable field absent/null/blank/whitespace; required names blank/whitespace; every bounded field at L-1/L/L+1, Unicode and encoded markup. SQL truncation must become actionable field errors without partial Identity updates. Profile handlers update Identity first and domain context second; inject second-save failure and compare fresh contexts (existing transaction-risk family). Changing contact email does not change UserName. Student Action resets to old defaults and does not refresh the returned profile; readback must reflect saved values, not stale defaults.

The three batch selectors share TeacherBatchDropdown/TeacherDropdown hidden-control mechanics; no extra lexical fields invented in the 649-control denominator. Empty selection is rejected by create handler; API checks tenant/teacher ownership. Test selection persisted/reset, no active batches, long names, keyboard/touch and duplicate names with distinct IDs. Native form reset does not itself reset controlled selector state.

## G03 — effective action access

Both controllers have Authorize. Program rejects inactive Identity users; AcademyAccessFilter skips these actions because there is no academyId action argument. No role-name/academy-active/module gate should be assumed from an inaccessible navigation item. Batch 03 lifecycle finding BUG-SEC-0003 applies as a test family; broaden its probes without claiming runtime exploitation.

TeacherPortalController — all 34 action guard paths mapped, including four resource actions from batch 03:

| Actions | Implemented effective gates / important variants |
| --- | --- |
| Me, Profile, UpdateProfile | Identity AcademyId+TeacherId, same-academy active Teacher; UpdateProfile checks names and Identity result. Me returns active owned batches and explicitly assigned recent sessions |
| Calendar | Linked AcademyId/TeacherId; year2020..2100, month1..12; explicit teacher sessions and academy holidays |
| BatchProgress, Progress, Payments | Linked IDs; owned batch/session/payroll queries. Payments requires both year/month or neither, range checks; filtered payouts only Paid, unfiltered latest12 can include other statuses. BatchProgress active batches; test cycle boundaries, multiple active payroll profiles, cancelled upcoming classes; Progress compares status text case-sensitively |
| LeaveRequests, RequestLeave | Linked IDs, own teacher requester; date/reason checks on create. Active Teacher not rechecked |
| PracticeLogs, ReviewPracticeLog | Linked IDs, active enrollment in a batch belonging to teacher; review log same academy. Read student-level logs across that student's subjects, not just owned-batch-specific logs; policy test required. Feedback nullable trim/max2000, Reviewed status/time |
| Assignments, LessonPlans, Assessments | Linked IDs plus owning batch relationship in queries; unpublished rows visible to owning teacher |
| CreateAssignment, CreateLessonPlan, CreateAssessment | OwnsBatch checks same academy and TeacherId, not batch/teacher active. Title and optional target enrollment/score guards as above |
| PublishAssignment, Submissions, ReviewSubmission | Same-academy assignment/submission plus OwnsBatch. Publish true queues notifications on every call; review trims feedback and sets Reviewed. Repeat/retry notification behavior and unauthorized ID substitution tests |
| UpdateLessonPlanStatus | Same-academy plan+OwnsBatch; exact Planned/Delivered/Skipped/MakeupNeeded allowlist |
| PublishAssessment, AssessmentResults, RecordAssessmentResult | Same-academy assessment+OwnsBatch; result student actively enrolled, score 0..max inclusive; upsert per student/assessment. Grade30/remarks1000, decimal(10,2), published result queues notification. Test repeat/concurrent writes and unpublished assessment with published result |
| Resources, ClassroomActivity, CreateClassNote, UploadClassMaterial | Reuse batch 03 action-specific ownership, resource projection and file matrix, including optional student/session scope. No double-counting reviewed forms |
| Roster, Attendance, MarkAttendance, MarkAttendanceBulk, UpdateSessionStatus, MarkTeacherAttendance | GetTeacherContext requires same-academy session explicitly assigned to linked TeacherId; batch ownership alone insufficient, null/inherited teacher session differs. Student marking requires active enrollment. Status vocabulary validated ignoring case, then original casing retained: test normalization against exact-case summaries. Bulk uses distinct IDs for enrollment check but iterates original records; test repeated student IDs, all-or-nothing persistence and unique-index conflicts. Session status allowlist Scheduled/InProgress/Completed/Cancelled/Rescheduled; no transition ordering/time/closed-cycle guard in this action |

PortalController — all 16 action guard paths mapped:

| Actions | Implemented effective gates / expected denial probes |
| --- | --- |
| Me | AcademyId + own active student, or existing own guardian. Children require portal-enabled unrevoked link and active student. No child -> UI initialization error, not proof invalid credentials |
| ChangePassword | AcademyId and StudentId/GuardianId present, old/new nonblank, new>=8 and changed; Identity verifies current password/policy. No active-child requirement. Verify no password logging and session policy separately |
| Notifications, MarkNotificationRead | AcademyId + linked recipient; recipient ID/type and academy scoped, read mark own notification only. Multi-linked identities select recipient ID and recipient type with differing precedence: include fixture, do not assume a supported normal account shape |
| Announcements | AcademyId; audience Student else Teacher else Admin. Guardian falls into Admin. Include Guardian-specific audience acceptance and top10-before-filter starvation; existing JSON shape issue applies |
| UpcomingEvents | AcademyId and student/guardian link, published future events, max50; no active-child check |
| GuardianChildren | Own guardian route ID, enabled/unrevoked links; unlike Me includes inactive child summary. Test scope plus consistent inactive-child UX |
| Student | CanAccessStudent = active same-academy student and own StudentId or enabled/unrevoked guardian link. Academic/Finance/Documents flags shape response; nested classHistory resource flag gap is BUG-SEC-0005. Cycle upcoming list uses batch name and includes past schedule range; duplicate names/date/history-limit cases require arithmetic assertions |
| SubmitAssignment | CanAccessStudent plus guardian academic flag; published assignment, matching optional student target and active enrollment. Unique assignment/student submission upsert, resets feedback, notifies teacher; concurrent/retry and due-date policy tests |
| DownloadInvoice, DownloadCertificate | CanAccessStudent and same-student/scoped record; certificate must be Issued. Missing Finance/Documents flag checks BUG-SEC-0005; certificate identity BUG-DATA-0014. Existing completed-only payment/adjustment bugs also affect invoice output |
| StudentLeaveRequests, RequestStudentLeave | Active same-academy student and own student or guardian CanManageLeave=true |
| LogPractice | CanAccessStudent + guardian academic flag and date/minute guards |
| UpdateProfile, UpdateGuardianProfile | Exact own student/guardian route identity; student additionally active. Guardian cannot edit child via student profile endpoint, even with other flags enabled |

For each group, run anonymous, inactive Identity, missing link, suspended academy, removed role retaining link, disabled module, same-tenant stranger and foreign-tenant IDs. Define module/lifecycle desired policy explicitly instead of treating absence of checks as intentional permission. Denied writes must persist no row/file/notification. Separate HTTP 400/403/404 outcomes from correct absence of unauthorized effects. No browser hide/show substitutes for these endpoint assertions.

## G01 — interaction, visual and outcome states

Teacher creation converts score/date but not booleans (BUG-API-0005); optional empty datetime shares BUG-API-0001. All three create forms share feedback rendered under assessment, so homework/lesson notices can be remote from initiating controls. Self-service profile feedback is rendered inside Leave rather than profile. Existing post-await currentTarget reset defect affects teaching create, teacher leave and shared family Action (BUG-FUNC-0001); do not file duplicate issues. Assignment shows Submitted for review and refreshes; no busy guard, catch or DOM file-input reset. Action lacks busy/catch, shows generic error, and reset can fail before refresh. Failed requests must retain values/file; ambiguous successful-write/network-failure requires readback before retry.

Family assignment and Action messages are small text, not live status regions. Most fields use placeholder-only labels, which disappear after input; test accessible names, visible persistent labels, validation associations and focus. Profile uses uncontrolled defaults while Teacher uses controlled state; test initial async load, switched child and profile save/readback to prevent stale values. Guardian child switching must not flash previous child's restricted information. Source review does not establish a browser race reproduction.

Required rendered matrix: desktop 1440, tablet 768, mobile 360/390/430, adjacent layout breakpoints, portrait/landscape and 200% zoom; light/dark, empty/long/many records, forbidden/load-error/offline/retry, submitting/success/error. Check readable field surfaces, logical headings, notice placement, touch targets and single-column form wrapping. Assigned-batch/child/dropdowns must remain anchored above cards at scroll 0/25/50/75/bottom, including short pages; open/close/selection/Escape and focus restoration. Date/year access, local datetime vs UTC, soft keyboard viewport, sticky navigation, document download loading/failure, and page scroll are required. iOS/Android physical-device and screenshot approval remain NOT RUN. API-only lesson references/results/status actions get API tests; do not invent UI dialogs for them.

## Findings and next work

Four new OPEN static P1 findings: BUG-SEC-0005, BUG-DATA-0014, BUG-API-0005, BUG-DATA-0015. Reproduction IDs: SECURITY-GUARDIAN-002, FAMILY-DOCUMENT-001, TEACHERPORTAL-CONTRACT-001, TEACHERPORTAL-LINK-001. Existing reset, optional-date, private-file, lifecycle and finance findings reused.

Added seven native form mappings (28 lexical controls), 21 composed field declarations and two controller guard mappings (50 actions). Cumulative: 23/82 forms, 151/649 controls, 15/70 controllers. These are source coverage counters, not completion percentage or test PASS. Other standalone portal editors, guardian administration/link forms and remaining app forms still need their own review; mapping these two controllers does not close all family workflows.

Next bounded batch: remaining Finance Governance settings/collections forms and permissions, then continue uncovered modules from progress.json. Keep the agreed Astra High review stage; later implementation/model handoff remains conditional on audit acceptance and user approval.
