# BUG-FUNC-0021 — Calendar presents cancelled sessions as ordinary joinable classes

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | RUNTIME-REPRODUCED in controlled TSX/Chromium; C10 partial repair PASS |
| Final verification | C10 controlled regression/browser PASS; full original host/device/critical closure pending |
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
| Fix commit | Not implemented (Phase 1) |
| Retest result | NOT RUN |
| Regression result | NOT RUN |
| Closure notes | Remain OPEN; follow closure requirements in QA README |

## Exact reproduction

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


## C10 academic batch — 7 November 2026

Cancelled class sessions remain visible with an explicit Cancelled title and retain their teacher/location details, but have no meeting action in either grid or agenda. Case/whitespace variants are handled. Scheduled classes, safe session-specific links, in-person locations, events and make-ups retain their existing behavior. No server cancellation or join authorization policy changed.

Implementation `acaab4c10113036502858cbca3e08ca1ee64a98b`; final browser fixture
`efec7bd2651dbd5009e9be002de2cbef54bc96f7`. Combined **58 targeted checks PASS**
(39 retained calendar assertions plus19 new cases) and **30 native Chromium
groups PASS**, zero runtime/console errors. Native corrected baseline12PASS/18FAIL;
only eight intercepted synthetic POSTs in the final run, no real host/SQL writes.
Full frontend **1,980PASS / three missing genuine SQL receipts FAIL / zero skips**;
exact-lock build/types/lint PASS (zero errors / existing63warnings). Counts are
shared batch evidence, not additional passes per issue.

Remain **OPEN** for the issue-specific original role/host/SQL/critical/device and
release closure requirements. See [C10 source-bound report](../HANDOFF/C10_ACADEMIC_BATCH_2026-11-07.md).
