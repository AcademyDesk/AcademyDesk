# BUG-DATA-0035 — Attendance status changes erase existing notes that have not been edited

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | LOCAL REPAIR / CONTROLLED TSX AND HTTP-SQL VERIFIED |
| Final verification | PARTIAL PASS:24 handlers and16 HTTP/SQL cases; browser/device/critical pending |
| Severity | Major unintended attendance data loss |
| Priority | P1 |
| Category | DATA |
| Module | ATTENDANCE |
| Role | See reproduction; same-tenant Admin unless stated otherwise |
| Screen / route | /attendance |
| API | POST sessions/{sessionId}/attendance |
| Environment | Local working tree and disposable loopback QA SQL/HTTP; production state not inferred |
| Device/viewport | NOT RUN; use QA/06_DEVICE_VIEWPORT_MATRIX.md where UI applies |
| Baseline | 20bb6047f9edf733ac8e2a226621cc582ec54b3c + pre-existing student UI diff |
| Discovery test / review ID | ATTENDANCE-NOTES-001 |
| Evidence classification | Accepted historical static trace plus current controlled actual TSX requests and real Identity/HTTP/fresh SQL |
| Preconditions | Isolated synthetic fixture from QA/10_TEST_DATA_STRATEGY.md; never customer data |
| Reproduction frequency | Not measured; reproduction instructions are proposed |
| Source | apps/web/src/app/attendance/page.tsx:65 |
| Class/function | mark / Attendance.Mark |
| Related source | INVENTORY/API_ENDPOINTS.md, CONTRACTS.md, FORMS_AND_FIELDS.md and ASYNC_FORM_RISKS.md |
| Root-cause confidence | HIGH |
| Evidence location | PHASE_2B_ATTENDANCE_NOTES_REPAIR.md and bounded source/evidence snapshot; historical excerpt retained below |
| Screenshot | Not captured on pinned baseline |
| Console logs | Not captured; required in retest |
| API request | Load a synthetic attendance record with notes. Without typing in Notes, change its status or press Save. Inspect request and fresh persisted notes. |
| API response |16 real Identity/HTTP-SQL cases PASS; response and GET match fresh persisted record for ten actual TSX payloads |
| Database before/after | Fresh SQL verifies note/status/record identity and unchanged other session; five rejected writes leave attendance snapshot identical |
| Dependencies | Safe SQL/HTTP/browser harness as applicable; desired policy review where noted |
| Fix commit | Local working-tree frontend repair; not committed/deployed |
| Retest result | Bounded local PASS; actual browser/device pending |
| Regression result |24 controlled handlers/16 real HTTP-SQL PASS; full critical not run; inherited lint FAIL |
| Closure notes | Remain OPEN; follow closure requirements in QA README |

## Exact reproduction

## Current local repair checkpoint — 2026-10-01

[Repair report](../REPORTS/PHASE_2B_ATTENDANCE_NOTES_REPAIR.md): displayed/submitted note source aligned, session-scoped record/draft state, explicit null clearing retained, duplicate guard and durable save/error/readback feedback.24 controlled actual TSX checks and16 real Identity/HTTP/fresh SQL/GET cases PASS. Attendance controller/schema/security unchanged. TypeScript PASS; inherited page lint1 error/2 warnings remains FAIL. Normal dev/Azure untouched; no commit/deployment/restart. Issue remains OPEN for actual browser/device/all-linked/critical acceptance. Original source excerpt and unexecuted original baseline below are historical, not current implementation.

### Historical reproduction

Load a synthetic attendance record with notes. Without typing in Notes, change its status or press Save. Inspect request and fresh persisted notes.

## Expected

Changing status or saving an unchanged record preserves visible notes; clearing notes must be a deliberate user edit.

## Actual / evidence

Input falls back to record.notes, but payload reads only notesByStudent. For a freshly loaded row the dictionary has no entry, so mark sends null and API clears existing notes. Runtime NOT RUN.

Source snapshot:

```text
64:     try {
65:       const response = await academyApi(`/api/academies/${academy.id}/sessions/${sessionId}/attendance`, { method: "POST", headers: apiHeaders(true), body: JSON.stringify({ studentId, status, notes: notesByStudent[studentId] || null }) });
66:       if (!response.ok) throw new Error();
67:       await loadRecords(sessionId);
```

## Suspected root cause

Displayed fallback and submitted state use different sources.

## Business impact and blast radius

Saved attendance comments are lost through ordinary status changes or unchanged Save; new typed notes work differently.

## Related / required regression

ATTENDANCE-NOTES-001: Browser existing/empty notes, status-only update, unchanged Save, explicit clear, reload and fresh SQL equality for untouched notes; preserve optional null support.

Also run all endpoint/form cases pointing to this issue in QA/03_TEST_MATRIX.md and the critical regression suite before closure.
