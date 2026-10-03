# BUG-DATA-0036 — Calendar substitutes batch teacher and meeting link for session-specific details

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | LOCAL REPAIR / CONTROLLED TSX AND REAL HTTP-SQL VERIFIED |
| Final verification | PARTIAL PASS:31 TSX checks and11 HTTP-SQL cases; browser/device/critical pending |
| Severity | Major misleading class details |
| Priority | P1 |
| Category | DATA |
| Module | SCHEDULE |
| Role | See reproduction; same-tenant Admin unless stated otherwise |
| Screen / route | /calendar; /schedule |
| API | GET sessions; GET batches |
| Environment | Local working tree and disposable loopback QA SQL/HTTP; production state not inferred |
| Device/viewport | NOT RUN; use QA/06_DEVICE_VIEWPORT_MATRIX.md where UI applies |
| Baseline | 20bb6047f9edf733ac8e2a226621cc582ec54b3c + pre-existing student UI diff |
| Discovery test / review ID | SCHEDULE-CALENDAR-DETAILS-001 |
| Evidence classification | Accepted historical static trace plus controlled actual calendar rendering and real API/fresh SQL fixture |
| Preconditions | Isolated synthetic fixture from QA/10_TEST_DATA_STRATEGY.md; never customer data |
| Reproduction frequency | Not measured; reproduction instructions are proposed |
| Source | apps/web/src/app/calendar/page.tsx:207 |
| Class/function | CalendarPage items projection |
| Related source | INVENTORY/API_ENDPOINTS.md, CONTRACTS.md, FORMS_AND_FIELDS.md and ASYNC_FORM_RISKS.md |
| Root-cause confidence | HIGH |
| Evidence location | PHASE_2B_CALENDAR_DETAILS_REPAIR.md and bounded source/evidence snapshot; original excerpt retained below |
| Screenshot | Not captured on pinned baseline |
| Console logs | Not captured; required in retest |
| API request | Use a synthetic batch with teacherA and batch linkA. Create an Online session with explicit teacherB and roomName containing linkB. Compare schedule list, teacher calendar, calendar agenda and its Open target. |
| API response | Real session Create/list, batch/teacher lookups and two teacher calendars verified; captured synthetic JSON feeds actual TSX rendering |
| Database before/after | Fresh SQL matches session teacher/location and unchanged batch defaults; GET/denial row snapshots unchanged |
| Dependencies | Safe SQL/HTTP/browser harness as applicable; desired policy review where noted |
| Fix commit | Local calendar working-tree repair; not committed/deployed |
| Retest result | Bounded local PASS; actual browser/device pending |
| Regression result |31 controlled TSX/11 real HTTP-SQL PASS; TypeScript/calendar lint/build PASS; full critical not run |
| Closure notes | Remain OPEN; follow closure requirements in QA README |

## Exact reproduction

The source excerpt and original runtime-NOT-RUN statements below are historical accepted baseline evidence, not the current implementation.

## Current local repair checkpoint — 2026-10-01

[Repair report](../REPORTS/PHASE_2B_CALENDAR_DETAILS_REPAIR.md): stored session teacher is authoritative (null/unresolved displays Unassigned, matching Schedule). Explicit session location takes precedence; missing location alone allows the legacy batch link. Unsafe links are not opened. Grid and agenda agree; Delivery/Location shown with wrapping.31 actual controlled TSX checks, including captured real API fixture rendering, and11 real Identity/HTTP/fresh SQL cases PASS. TypeScript/calendar lint/build PASS. API/Schedule/Teacher/security/schema/CSS unchanged; no dev/Azure/commit/deployment/restart. Issue OPEN for live browser/device/linked/critical gates. No real meeting opened.

### Historical reproduction

Use a synthetic batch with teacherA and batch linkA. Create an Online session with explicit teacherB and roomName containing linkB. Compare schedule list, teacher calendar, calendar agenda and its Open target.

## Expected

Calendar describes and opens the particular session using its effective assigned teacher and session meeting location, with an explicit fallback policy for missing values.

## Actual / evidence

Calendar Session type does not expose teacherId and agenda always reads batch.teacherId. For Online/Hybrid it prioritizes batch.meetingLink over the session roomName URL. Session teacher overrides and per-session links are obscured. Runtime NOT RUN.

Source snapshot:

```text
206:         subject: assignedBatch ? courseName(assignedBatch.courseId) : "Not assigned",
207:         teacher: teacherName(assignedBatch?.teacherId),
208:         students: classStudents(row.batchId),
209:         meetingPattern: assignedBatch?.meetingPattern ?? undefined,
```

## Suspected root cause

Calendar derives delivery details from batch defaults instead of the session record.

## Business impact and blast radius

Substitute-teacher display and meeting navigation can disagree with the stored session and teacher ownership; verify intended link-precedence policy.

## Related / required regression

SCHEDULE-CALENDAR-DETAILS-001: Browser session override/default/null matrices with distinct teacher names and inert synthetic URLs; assert agenda text and exact href without opening real meetings.

Also run all endpoint/form cases pointing to this issue in QA/03_TEST_MATRIX.md and the critical regression suite before closure.
