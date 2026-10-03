# BUG-DATA-0037 — Choosing an unassigned batch retains the previous batch teacher and branch

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | LOCAL REPAIR / CONTROLLED TSX AND REAL HTTP-SQL VERIFIED |
| Final verification | PARTIAL PASS:18 TSX checks and18 HTTP-SQL cases; browser/device/critical pending |
| Severity | Major unintended scheduling assignment |
| Priority | P1 |
| Category | DATA |
| Module | SCHEDULE |
| Role | See reproduction; same-tenant Admin unless stated otherwise |
| Screen / route | /schedule |
| API | POST sessions |
| Environment | Local working tree and disposable loopback QA SQL/HTTP; production state not inferred |
| Device/viewport | NOT RUN; use QA/06_DEVICE_VIEWPORT_MATRIX.md where UI applies |
| Baseline | 20bb6047f9edf733ac8e2a226621cc582ec54b3c + pre-existing student UI diff |
| Discovery test / review ID | SCHEDULE-DEFAULTS-001 |
| Evidence classification | Accepted historical static trace plus current actual TSX handlers/payloads and real Identity-HTTP-fresh-SQL |
| Preconditions | Isolated synthetic fixture from QA/10_TEST_DATA_STRATEGY.md; never customer data |
| Reproduction frequency | Not measured; reproduction instructions are proposed |
| Source | apps/web/src/app/schedule/page.tsx:110 |
| Class/function | applyBatchDefaults |
| Related source | INVENTORY/API_ENDPOINTS.md, CONTRACTS.md, FORMS_AND_FIELDS.md and ASYNC_FORM_RISKS.md |
| Root-cause confidence | HIGH |
| Evidence location | PHASE_2B_SCHEDULE_DEFAULTS_REPAIR.md and bounded source/evidence snapshot; original excerpt retained below |
| Screenshot | Not captured on pinned baseline |
| Console logs | Not captured; required in retest |
| API request | Choose batchA with teacherA/branchA, then choose batchB whose teacherId and branchId are null. Enter valid times and submit without intentionally selecting overrides. |
| API response | Twelve actual TSX payloads verified through real Create response and GET readback; existing optional-null inheritance retained |
| Database before/after | Fresh SQL verifies correct batch/teacher/branch/times, unchanged batch defaults; five rejected writes preserve session/batch snapshots |
| Dependencies | Safe SQL/HTTP/browser harness as applicable; desired policy review where noted |
| Fix commit | Local Schedule working-tree repair; not committed/deployed |
| Retest result | Bounded local PASS; actual browser/device pending |
| Regression result |18 controlled TSX/18 real HTTP-SQL PASS; TypeScript/build/lint exit0 with1 inherited warning; full critical not run |
| Closure notes | Remain OPEN; follow closure requirements in QA README |

## Exact reproduction

The source excerpt and original runtime-NOT-RUN statements below are historical accepted baseline evidence, not current implementation.

## Current local repair checkpoint — 2026-10-01

[Repair report](../REPORTS/PHASE_2B_SCHEDULE_DEFAULTS_REPAIR.md): changing the batch replaces both dependent defaults, clearing missing/null fields and old overrides; initial automatic selection applies both defaults. Deliberate overrides entered after a switch, or retained with the unchanged batch, still submit normally.18 actual controlled TSX checks and18 real Identity/HTTP/fresh SQL cases PASS. TypeScript/build PASS; lint exits0 with1 inherited dependency warning. Existing optional-null selected-batch inheritance and API/security/schema/CSS/prior calendar repairs unchanged. No normal dev/Azure/commit/deployment/restart. Issue OPEN for live browser/device/linked/critical gates.

### Historical reproduction

Choose batchA with teacherA/branchA, then choose batchB whose teacherId and branchId are null. Enter valid times and submit without intentionally selecting overrides.

## Expected

Changing the batch resets teacher/branch defaults to the new batch or clearly requests confirmation before preserving an explicit override.

## Actual / evidence

applyBatchDefaults sets teacher/branch only when new IDs are truthy. Old values survive null defaults and are submitted as explicit assignments to batchB. They pass tenant-existence validation. Runtime NOT RUN.

Source snapshot:

```text
109:     const batch = batches.find((item) => item.id === id);
110:     if (batch?.teacherId) setTeacherId(batch.teacherId);
111:     if (batch?.branchId) setBranchId(batch.branchId);
112:   }
```

## Suspected root cause

Dependent optional state is only populated, never cleared on parent change.

## Business impact and blast radius

Wrong teacher/branch scheduled within the same academy, including after selecting the empty batch option and returning to a batch.

## Related / required regression

SCHEDULE-DEFAULTS-001: Assigned->unassigned, partially assigned, empty selection, initial auto-selected batch and deliberate override cases; verify current UI values, payload and fresh session assignment.

Also run all endpoint/form cases pointing to this issue in QA/03_TEST_MATRIX.md and the critical regression suite before closure.
