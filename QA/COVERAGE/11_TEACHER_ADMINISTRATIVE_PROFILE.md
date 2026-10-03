# Batch 11 — Teacher administrative profile

2026-09-28. Source specification; runtime NOT RUN. Application fingerprint and pre-existing student edits retained. Teacher360 /teacher-profile is the scope; compensation, payroll and teacher intake reuse prior mappings and are not newly counted here.

## Scope and traceability

Read teacher-profile/page.tsx, ProfilesController.Teacher/UpdateTeacherProfile/ReadAvailability, Teacher entity, EF mapping and global permission filter. This completes ProfilesController coverage by combining Guardian/UpdateGuardianProfile from batch07, Student/UpdateStudentProfile from batch10 and Teacher/UpdateTeacherProfile here. All six actions are mapped; controller completion means source review only. Stable cases PROFILE-API-003/005 and VISUAL-PAGE-076. The wrong-record save family extends GUARDIAN-PROFILE-001 / BUG-DATA-0018; null availability gets TEACHER-AVAILABILITY-001 / BUG-API-0006.

18 source controls, no native forms: teacher selector,14 ordinary profile controls and three availability templates repeated across seven days. Count source declarations once; runtime must exercise all21 day/time instances. AvailabilityJson is a serialized payload, not an extra visible control.

## G02 — fields, optional values and persistence

| Source control | Payload / boundaries |
| --- | --- |
| teacher-record | Teacher GUID selected from roster. Requested teacherId query parameter resolves only if found, otherwise first teacher. Test empty roster, valid/unknown/deactivated/foreign query ID, duplicate names and switching while loading/saving |
| specialties | Optional text → Specialties Clean; EF max500. Free text describes capability, independent of assigned course subjects; test commas/Unicode/long values |
| availabilityEnabled | Repeated checked Boolean(slot) input. Enabling creates day with blank from/to; disabling removes that day. Toggle off/on currently discards that day's times; verify deliberate behavior and unsaved-change notice |
| availabilityFrom / availabilityTo | Repeated native time inputs, disabled when day unchecked. JSON array day/from/to, not UTC instants. Empty strings allowed in UI; API has no time-order/format/day allowlist validation. Test each Mon–Sun, midnight/noon, equal/end-before-start, blank endpoints, invalid direct payload and overnight policy |
| employeeCode | Optional cleaned string, EF max50; same-academy duplicate excluding this teacher returns409 before mutation; filtered unique academy/code index. Test null/clear/same-code/other-academy reuse, case/collation and concurrent duplicate requests |
| preferredName | Optional Clean string, EF max120 |
| teacher-employment-type | employmentType optional string, EF max50. UI Full-time/Part-time/Contract/Visiting faculty; API no enum restriction. Preserve unknown legacy value and test clearing |
| teacher-date-of-birth | dateOfBirth optional DateOnly; UI blank becomes null and loaded string is sliced to date. Test leap dates, malformed date, future DOB, year navigation |
| teacher-joining-date | joiningDate optional DateOnly; blank becomes null. Test before DOB/future/local-date boundaries; controller has no ordering rule, so policy needs agreement |
| addressLine1 | Optional Clean string, EF max240 |
| city / state | Optional Clean strings, EF max100 each |
| postalCode | Optional Clean string, EF max30; preserve leading zeroes and alphanumeric codes |
| emergencyContactName | Optional Clean string, EF max160 |
| emergencyContactPhone | Optional Clean string, EF max30; preserve plus/spaces/leading zeroes |
| qualifications | Optional Clean string, EF max1000; plain text, distinct from CertificationsJson on entity |
| adminNotes | Optional Clean string, EF max4000; intended internal staff notes. Require access and portal-projection checks; no HTML execution |

Every profile request property is nullable. Clean maps whitespace/blank strings to null. Test absent/null/empty/whitespace and max-1/max/max+1 for all strings, including Unicode and HTML-looking values. UI inputs do not impose maxlength and click Save does not invoke a native form validation cycle. EF limits need SQL tests and friendly HTTP error checks; do not claim explicit API length guards. Blank date conversion is present; optional fields must not need filler text. This editor does not modify primary name/email/phone, BranchId, IsActive, CertificationsJson or CompensationJson, and does not synchronize Identity.

AvailabilityJson is accepted as a string and saved without parsing. ReadAvailability then deserializes case-insensitively to a list and removes entries with blank Day, catching JsonException only. Test blank/null/[], malformed syntax, object/scalar roots, mixed property casing, unknown days, duplicate days, invalid time strings, oversized arrays and null elements. A literal string [null] persists successfully, then slot.Day dereferences null during readback; later GET repeats the failure (BUG-API-0006). The ordinary UI emits objects, so this reproduction uses the authorized API or existing bad data, not a claim that clicking a day produces null.

Malformed JSON that throws JsonException is instead returned as empty availability; prepare serializes the returned empty array and a subsequent unrelated profile save can overwrite the stored malformed value. Test visible validation/data-recovery behavior. Unknown days survive the reader but are not shown by the seven-day UI; duplicate same-day entries display only the first and are collapsed when that day is edited. Do not silently treat this lossy read/edit round trip as validated availability. No explicit time zone is stored with these recurring slots; scheduling enforcement is outside this batch.

## G03 — authorization, reads and mutation boundaries

ProfilesController has no PermissionCatalog mapping. Global academy filter allows same-academy Owner/AcademyAdmin after active-academy/module checks; other roles/custom grants hit the unmapped fallback403. Flagged IsPlatformOwner bypass differs from role name. Core is the default module. Teacher roster is also an unmapped administrative controller. Test all12 roles, anonymous/inactive Identity, foreign academy and person ID, disabled academy/module, stale grants and platform-flag controls. Teacher self-service is a separate API; an administrative profile grant must not be inferred from teacher assignment or workforce.manage.

Teacher GET scopes the teacher by academy and ID, allows inactive teachers and returns404 if absent. Assigned batches join course and match academy/TeacherId, including inactive batches. Class sessions require both teacher and current assigned batch IDs, descending StartUtc; reassigning a batch can remove an old teacher's class history from this projection. Test reassignment against agreed historical-summary semantics; do not assume this is a complete employment history.

Batch completed counts use exact Completed; pending excludes exact Completed and Cancelled. Current cycle is UTC calendar month [start,nextMonth), not a compensation/session-block cycle. Classes response is truncated to12; leave requests to8. UI tile counts count these returned arrays, so test13+ sessions and9+ leaves and require clear scope labels if totals are intended. Subjects are distinct names from all assigned batches including inactive. Students joins Active enrollments for teacher-assigned batches, with no student/batch-active filter or per-person distinct; a student in two batches yields two rows. Test active/inactive records, multiple subjects/batches, missing joins, duplicate batch names and modal React keys. No runtime summary correctness claim.

UpdateTeacherProfile checks scoped teacher and employee-code collision, assigns all nullable properties, saves, then invokes Teacher GET. Stored availability may fail during that post-save projection; the request can report failure after committing. Global audit save is a separate existing failure path. Test response plus fresh rows, not just HTTP success. No transaction/rollback promise and no Identity write here. Profile retries are replacement writes; ensure stale or omitted fields cannot accidentally erase independent edits.

## G01 — selection, feedback and visual states

Teacher selection changes teacherId before load but retains profile/form. Save remains enabled during selection load; failed load retains old fields. A delayed A response or save response can replace B's display without checking the response ID. Extend BUG-DATA-0018 with A/B distinct employeeCode, address and availability. Test switching while GET/PUT is pending, failures, reverse-order responses, clearing selection, fresh persisted target/body and dirty-navigation handling. Empty selection returns early from load yet retains profile; Save returns without feedback because teacherId is empty. Detail state is not reset on selection; test open modal showing old/new teacher content clearly.

Initial academy/roster loads parse JSON without checking response.ok consistently; profile load does check it. Test401/403/404/500, null/non-JSON responses, no academy, no teacher, retry and expired session. Save normalizes dates to null, displays returned error message and prepares successful response before setting confirmation. A null successful body makes prepare throw; old message is not cleared at save start. Test persistent accurate success/error notices, progress announcement, disabled double submit, retained values on validation failure, retry after ambiguous persistence and notice placement when scrolled down. Exact server exception output must not leak private notes.

Visual test specifications:320/360/390/768/1280 widths,200% zoom, both themes, long multilingual names/notes, mobile keyboard and landscape. Check persistent accessible labels, placeholders after data entry, optional labels, notice contrast, tab order and field-error associations. Dropdowns must track trigger at top/mid/bottom scroll, choose-and-close, Escape/focus restore and remain above panels. Date picker requires direct year selection. Availability currently uses native time inputs; compare Android/iOS/desktop behavior and the agreed control design rather than claiming historical clock complaint fixed. Seven-day layout must not overflow horizontally or hide disabled-state meaning.

Three interactive tiles open batch/session/student detail modals. Verify heading/record identity, empty/loading states, readable date/time (UI Asia/Kolkata versus UTC cycle boundaries), long rows, scroll containment, focus trap/return, Escape and touch target sizes. Leave count has no detail action; define expectation without inventing one. Browser, physical device and visual baseline checks are NOT RUN.

## Outcome and next review

Added18 source controls; cumulative32/82 native forms,228/649 controls and24/70 complete controller mappings. ProfilesController complete through batches07/10/11. One new P1 static finding and one existing selection-race issue extended; application changes and runtime execution remain pending. Next source review: platform/tenant lifecycle and academy configuration, preserving prior role/onboarding mappings. Phase1 remains NOT CLOSED. The previously stated four remaining stages are broad work groups, not four guaranteed audit turns or a completion percentage.
