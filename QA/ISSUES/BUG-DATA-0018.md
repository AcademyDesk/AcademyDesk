# BUG-DATA-0018 — Record selection can overwrite another record with stale or unloaded data

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | STATIC-FINDING |
| Final verification | NOT RUN |
| Severity | Major wrong-record overwrite risk |
| Priority | P1 |
| Category | DATA |
| Module | GUARDIAN |
| Role | See reproduction; same-tenant Admin unless stated otherwise |
| Screen / route | /guardian-profile; /student-fees; /student-profile; /student-management; /teacher-profile; /platform/control tenant onboarding; /assessments; /attendance; /communication-preferences |
| API | GET/PUT guardians/{guardianId}/profile; GET/PUT student fee-arrangements/admission-fee; GET/PUT students/{studentId}/profile; GET/PUT teachers/{teacherId}/profile; GET/PUT platform academies/{academyId}/onboarding |
| Environment | Source review of local working tree; runtime production state not inferred |
| Device/viewport | NOT RUN; use QA/06_DEVICE_VIEWPORT_MATRIX.md where UI applies |
| Baseline | 20bb6047f9edf733ac8e2a226621cc582ec54b3c + pre-existing student UI diff |
| Discovery test / review ID | GUARDIAN-PROFILE-001 |
| Evidence classification | Static trace; runtime reproduction pending |
| Preconditions | Isolated synthetic fixture from QA/10_TEST_DATA_STRATEGY.md; never customer data |
| Reproduction frequency | Not measured; reproduction instructions are proposed |
| Source | apps/web/src/app/guardian-profile/page.tsx:80 |
| Class/function | select / load / save |
| Related source | INVENTORY/API_ENDPOINTS.md, CONTRACTS.md, FORMS_AND_FIELDS.md and ASYNC_FORM_RISKS.md |
| Root-cause confidence | HIGH |
| Evidence location | Source excerpt below; source fingerprint in INVENTORY/SOURCE_MANIFEST.md |
| Screenshot | Not captured on pinned baseline |
| Console logs | Not captured; required in retest |
| API request | Load guardian A with distinct address. Select B while delaying or failing B profile GET; save while B is selected. Inspect PUT target/body and fresh A/B rows. Also resolve rapid A/B requests in reverse order and switch during save. |
| API response | Not captured for this issue; use synthetic request/response in isolated reproduction |
| Database before/after | Not executed; fixture and fresh-context assertions defined below |
| Dependencies | Safe SQL/HTTP/browser harness as applicable; desired policy review where noted |
| Fix commit | Not implemented (Phase 1) |
| Retest result | NOT RUN |
| Regression result | NOT RUN |
| Closure notes | Remain OPEN; follow closure requirements in QA README |

## Exact reproduction

Load guardian A with distinct address. Select B while delaying or failing B profile GET; save while B is selected. Inspect PUT target/body and fresh A/B rows. Also resolve rapid A/B requests in reverse order and switch during save.

## Expected

Displayed editable data and save target refer to the same loaded record; pending/failed selection cannot submit prior record data.

## Actual / evidence

select changes guardianId immediately but leaves profile/form from A and Save enabled. Failed GET preserves old fields; save uses current guardianId with old form. Requests have no identity/version guard against late responses. StudentFeesPage has the same pattern: its studentId changes without clearing admissionAmount/admissionDueDate or disabling Save during the new GET. StudentProfilePage likewise changes studentId while retaining profile; StudentAdminProfile resets fields from that old profile on studentId change, and onSaved merges into current profile without target validation. TeacherProfilePage also changes teacherId while retaining profile/form and leaves Save enabled during load; late GET/PUT responses replace current profile unconditionally. Tenant discovery clears profile but allows Save during failed/pending GET; blank forms can overwrite existing details, and late responses can remount another tenant data under current ID. Assessments changes assessmentId without clearing/loading-locking results; ResultRow is keyed only by student.id. Shared learners retain previous assessment scores while a pending/failed result GET leaves old results, and save targets the newly selected assessment. Late result GET or post-save reload also overwrites results without checking selected assessment. Attendance similarly retains records when sessionId changes and allows Save while new records load or fail. notesByStudent is keyed only by student ID and is never reset on session change, so notes can carry between sessions even after successful new record load. Communication preferences reset editable consent only when selected preference object changes. Switching A to B when neither has a stored preference leaves selected undefined in both cases, so unsaved consent and notes from A remain and Save targets B. Runtime reproduction pending.

Source snapshot:

```text
79:   async function select(id: string) {
80:     setGuardianId(id);
81:     try {
82:       await load(id);
```

## Suspected root cause

Selected record ID and loaded editable record are stored separately without pending-state or response-identity protection.

## Business impact and blast radius

Administrative guardian address/preferences edits; student admission-fee amounts/due dates and administrative profile metadata; teacher administrative metadata and availability; platform tenant discovery records; stale writes after failed or slow selection.

## Related / required regression

GUARDIAN-PROFILE-001: Browser delayed/failed/out-of-order GET and PUT tests with distinct synthetic guardian values; assert save disabled or correctly bound until matching load completes, cancel/empty selection behavior and exact persisted target. Repeat with StudentFeesPage admission amount/date, delayed student change and failed GET; verify no wrong-student overwrite. Repeat StudentProfilePage and its student-management alias with studentNumber, address and notes; reverse load/save response order and verify fresh rows. Repeat TeacherProfilePage with distinct employee codes and availability, failed selection and switching during save. Add tenant discovery A/B reversed and failed GET, typing during load, save before load and fresh tenant no-wrong-target/no-blank-overwrite assertions. Add two assessments sharing a learner, distinct scores, delayed/failed/reversed result GET, selection during save and no cross-assessment writes. Add attendance shared-learner session A/B with different notes/status, pending/failed/reversed GET and persisted cross-session no-overwrite assertions. Add contacts A/B with no preference rows, edit A without saving, select B or switch contact type, and assert defaults plus no wrong-person consent write.

Also run all endpoint/form cases pointing to this issue in QA/03_TEST_MATRIX.md and the critical regression suite before closure.
