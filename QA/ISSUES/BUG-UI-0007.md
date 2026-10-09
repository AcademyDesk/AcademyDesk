# BUG-UI-0007 — Calendar groups items by local date while displaying India time

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | CONTROLLED-TSX REPRODUCED; EXPORTED-DOM RETEST PASS |
| Final verification | PARTIAL: all three calendar GET UTC sources + 16 live browser combinations PASS; critical/device OPEN |
| Severity | Major schedule date inconsistency |
| Priority | P1 |
| Category | UI |
| Module | SCHEDULE |
| Role | See reproduction; same-tenant Admin unless stated otherwise |
| Screen / route | /calendar |
| API | GET sessions/events/makeup-classes |
| Environment | Source review of local working tree; runtime production state not inferred |
| Device/viewport | NOT RUN; use QA/06_DEVICE_VIEWPORT_MATRIX.md where UI applies |
| Baseline | 20bb6047f9edf733ac8e2a226621cc582ec54b3c + pre-existing student UI diff |
| Discovery test / review ID | SCHEDULE-TIMEZONE-001 |
| Evidence classification | Static trace; runtime reproduction pending |
| Preconditions | Isolated synthetic fixture from QA/10_TEST_DATA_STRATEGY.md; never customer data |
| Reproduction frequency | Not measured; reproduction instructions are proposed |
| Source | apps/web/src/app/calendar/page.tsx:244 |
| Class/function | byDay / agenda / monthName / time |
| Related source | INVENTORY/API_ENDPOINTS.md, CONTRACTS.md, FORMS_AND_FIELDS.md and ASYNC_FORM_RISKS.md |
| Root-cause confidence | HIGH |
| Evidence location | Source excerpt below; source fingerprint in INVENTORY/SOURCE_MANIFEST.md |
| Screenshot | Not captured on pinned baseline |
| Console logs | Not captured; required in retest |
| API request | In a UTC browser use a session at2026-09-30T19:00:00Z, which is October1 at00:30 IST. Inspect September grid, October agenda and rendered item date/time. Repeat in India and a timezone east of India. |
| API response | Not captured for this issue; use synthetic request/response in isolated reproduction |
| Database before/after | Not executed; fixture and fresh-context assertions defined below |
| Dependencies | Safe SQL/HTTP/browser harness as applicable; desired policy review where noted |
| Fix commit | Scoped feature-branch repair; see 2026-10-09 report and file history |
| Retest result | 200 controlled checks / 16 exported-browser combinations PASS |
| Regression result | All 39 previous calendar projection checks retained per timezone; broader critical suite NOT RUN |
| Closure notes | Remain OPEN; follow closure requirements in QA README |

## Current repair checkpoint — 2026-10-09

[Direct live retest](../REPORTS/PHASE_2B_CALENDAR_LIVE_SQL_BROWSER.md): Events
and Makeup GET projections now mark known SQL UTC values without changing ticks.
Fresh SQL checks both start/end and unchanged row/status/count snapshots. Real
login→Schedule cancellation→Calendar/reload PASS across 16 combinations, all
three sources displaying consistent IST month/year/day/time. 840 forwarded
responses all 200, no console/429; full 1,130 API / 332 controlled / 32 original
synthetic browser / 18 guards PASS and owned cleanup verified. Critical/physical
gates remain OPEN. Narrow visual readability is separately OPEN as BUG-UI-0008.

### Earlier class-only SQL checkpoint

[Class cancellation SQL retest](../REPORTS/PHASE_2B_CALENDAR_CANCELLATION_SQL_RETEST.md)
exposed missing UTC Z after SQL datetime2 readback. Class and Teacher session
projections now preserve stored ticks and explicitly mark UTC. 19 real HTTP/SQL
cases, 4 new serializer / 1,126 API tests, 332 TSX checks and 16 captured-response
DOM timezone/viewport/theme combinations PASS; class time remains 3:30 pm IST
in all four timezones. Other event/make-up timestamp sources, directly live
browser/API, original critical suite and physical devices remain OPEN.

### Earlier frontend-only checkpoint (before SQL retest)

[Repair report](../REPORTS/PHASE_2B_CALENDAR_TIMEZONE_REPAIR.md): India civil-day
grouping, UTC marker arithmetic, initial month, heading, Today and agenda now
agree independently of browser timezone. Month header visibly declares IST.
200 controlled TSX checks and 16 desktop/mobile exported-DOM combinations
PASS; TypeScript/lint/83-page export PASS. Synthetic transport only: no new
API/SQL or physical-device proof. Issue remains OPEN for linked, original
critical-regression and applicable device acceptance. Historical source
snapshot below describes the original defect, not the repaired code.

## Original reproduction

In a UTC browser use a session at2026-09-30T19:00:00Z, which is October1 at00:30 IST. Inspect September grid, October agenda and rendered item date/time. Repeat in India and a timezone east of India.

## Expected

Grid placement, selected month, heading, Today and item date/time use the same declared calendar timezone.

## Actual / evidence

Grid and agenda group using browser-local getMonth/getFullYear/toDateString, but item time/date and month heading format Asia/Kolkata. The example appears in September30 cell at00:30 while its agenda date says October1. A month-start Date in a timezone east of India can also display the previous month heading. Runtime NOT RUN.

Source snapshot:

```text
243:     items
244:       .filter((item) => item.start.toDateString() === date.toDateString())
245:       .sort((a, b) => +a.start - +b.start);
246:   const agenda = items
```

## Suspected root cause

Calendar boundaries and labels use different timezone models.

## Business impact and blast radius

Classes, make-ups and events near midnight/month boundaries for non-India viewers.

## Related / required regression

SCHEDULE-TIMEZONE-001: Browser India/UTC/LosAngeles/Sydney timezone cases at midnight/month/year/DST boundaries; assert same date across grid, agenda, heading, Today and schedule list.

Also run all endpoint/form cases pointing to this issue in QA/03_TEST_MATRIX.md and the critical regression suite before closure.
