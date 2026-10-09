# BUG-FUNC-0021 — Calendar presents cancelled sessions as ordinary joinable classes

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | CONTROLLED-TSX REPRODUCED; EXPORTED-DOM RETEST PASS |
| Final verification | PARTIAL: controlled/synthetic browser PASS; linked SQL/critical/device OPEN |
| Severity | Major misleading schedule state |
| Priority | P1 |
| Category | FUNC |
| Module | SCHEDULE |
| Role | See reproduction; same-tenant Admin unless stated otherwise |
| Screen / route | /calendar |
| API | GET sessions |
| Environment | Source review of local working tree; runtime production state not inferred |
| Device/viewport | NOT RUN; use QA/06_DEVICE_VIEWPORT_MATRIX.md where UI applies |
| Baseline | 20bb6047f9edf733ac8e2a226621cc582ec54b3c + pre-existing student UI diff |
| Discovery test / review ID | SCHEDULE-CANCELLED-001 |
| Evidence classification | Static trace; runtime reproduction pending |
| Preconditions | Isolated synthetic fixture from QA/10_TEST_DATA_STRATEGY.md; never customer data |
| Reproduction frequency | Not measured; reproduction instructions are proposed |
| Source | apps/web/src/app/calendar/page.tsx:191 |
| Class/function | items projection / AgendaItem |
| Related source | INVENTORY/API_ENDPOINTS.md, CONTRACTS.md, FORMS_AND_FIELDS.md and ASYNC_FORM_RISKS.md |
| Root-cause confidence | HIGH |
| Evidence location | Source excerpt below; source fingerprint in INVENTORY/SOURCE_MANIFEST.md |
| Screenshot | Not captured on pinned baseline |
| Console logs | Not captured; required in retest |
| API request | Create a synthetic Online session with a meeting URL, cancel it through authorized session status update, then reload calendar. Compare stored/API status and the grid/agenda class entry. |
| API response | Not captured for this issue; use synthetic request/response in isolated reproduction |
| Database before/after | Not executed; fixture and fresh-context assertions defined below |
| Dependencies | Safe SQL/HTTP/browser harness as applicable; desired policy review where noted |
| Fix commit | Scoped feature-branch repair; see 2026-10-09 report and file history |
| Retest result | 324 controlled checks / 32 exported-browser viewport-timezone-theme combinations PASS |
| Regression result | All 50 preceding projection/timezone cases retained per zone; critical/physical-device NOT RUN |
| Closure notes | Remain OPEN; follow closure requirements in QA README |

## Current repair checkpoint — 2026-10-09

[Repair report](../REPORTS/PHASE_2B_CALENDAR_CANCELLATION_REPAIR.md): returned
session lifecycle is now retained and displayed in grid/agenda. Cancelled
records remain in history, with explicit cancellation text and no meeting
action, including legacy batch-link fallback and case variants. Other actions,
source timestamps and location/teacher precedence remain unchanged. 324
controlled tests and 32 synthetic exported-DOM desktop/mobile/light/dark
combinations PASS; TypeScript/calendar lint/83-page build PASS. Real cancellation
HTTP/SQL transition, linked browser/critical and device retests remain OPEN.
Historical source/evidence below describes the original defect, not current code.

## Original reproduction

Create a synthetic Online session with a meeting URL, cancel it through authorized session status update, then reload calendar. Compare stored/API status and the grid/agenda class entry.

## Expected

Cancelled sessions are either excluded from actionable upcoming items or clearly marked cancelled with joining disabled, according to the approved history policy.

## Actual / evidence

Sessions list includes all statuses. Calendar Session type omits status and every row becomes an ordinary Class item, with an Open meeting link when available. Neither grid nor agenda marks cancellation. Runtime NOT RUN.

Source snapshot:

```text
190:     return [
191:       ...sessions.map((row) => {
192:         const assignedBatch = batch(row.batchId);
193:         const isVirtualClass = ["Online", "Hybrid"].includes(row.deliveryMode);
```

## Suspected root cause

Calendar drops lifecycle status during projection.

## Business impact and blast radius

Users can mistake cancelled classes for scheduled sessions; no claim that the external meeting provider itself is cancelled.

## Related / required regression

SCHEDULE-CANCELLED-001: Scheduled/Completed/Cancelled/NoShow/InProgress/Rescheduled matrix, explicit history policy, count/filter parity and nonactionable cancelled links on desktop/mobile.

Also run all endpoint/form cases pointing to this issue in QA/03_TEST_MATRIX.md and the critical regression suite before closure.
