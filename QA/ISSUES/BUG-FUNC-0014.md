# BUG-FUNC-0014 — Blank curriculum sequence saves zero despite minimum one

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | RUNTIME-REPRODUCED in controlled TSX/Chromium; C10 partial repair PASS |
| Final verification | C10 controlled regression/browser PASS; full original host/device/critical closure pending |
| Severity | Moderate validation inconsistency |
| Priority | P2 |
| Category | FUNC |
| Module | CURRICULUM |
| Role | See reproduction; same-tenant Admin unless stated otherwise |
| Screen / route | /curriculum |
| API | POST /api/academies/{academyId}/course-modules |
| Environment | Source review of local working tree; runtime production state not inferred |
| Device/viewport | NOT RUN; use QA/06_DEVICE_VIEWPORT_MATRIX.md where UI applies |
| Baseline | 20bb6047f9edf733ac8e2a226621cc582ec54b3c + pre-existing student UI diff |
| Discovery test / review ID | CURRICULUM-SEQUENCE-001 |
| Evidence classification | Static trace; runtime reproduction pending |
| Preconditions | Isolated synthetic fixture from QA/10_TEST_DATA_STRATEGY.md; never customer data |
| Reproduction frequency | Not measured; reproduction instructions are proposed |
| Source | apps/web/src/app/curriculum/page.tsx:68 |
| Class/function | add / CourseModules.Create |
| Related source | INVENTORY/API_ENDPOINTS.md, CONTRACTS.md, FORMS_AND_FIELDS.md and ASYNC_FORM_RISKS.md |
| Root-cause confidence | HIGH |
| Evidence location | Source excerpt below; source fingerprint in INVENTORY/SOURCE_MANIFEST.md |
| Screenshot | Not captured on pinned baseline |
| Console logs | Not captured; required in retest |
| API request | Select a valid synthetic course, enter a title, clear the optional numeric Sequence input and submit. Compare persisted sequence with explicit1 and direct API0/-1 controls. |
| API response | Not captured for this issue; use synthetic request/response in isolated reproduction |
| Database before/after | Not executed; fixture and fresh-context assertions defined below |
| Dependencies | Safe SQL/HTTP/browser harness as applicable; desired policy review where noted |
| Fix commit | Not implemented (Phase 1) |
| Retest result | NOT RUN |
| Regression result | NOT RUN |
| Closure notes | Remain OPEN; follow closure requirements in QA README |

## Exact reproduction

Select a valid synthetic course, enter a title, clear the optional numeric Sequence input and submit. Compare persisted sequence with explicit1 and direct API0/-1 controls.

## Expected

Sequence honors the displayed minimum1: either require it, choose a documented positive default or reject before saving.

## Actual / evidence

Input has min1 but no required attribute. Empty value passes native validity, Number of empty string is0, and API stores Sequence without range validation. Runtime NOT RUN.

Source snapshot:

```text
67:           description: null,
68:           sequence: Number(sequence),
69:         }),
70:       },
```

## Suspected root cause

Blank numeric coercion bypasses the UI minimum and server lacks the matching invariant.

## Business impact and blast radius

Module ordering can start at0 or negative values through direct API; repeated zero may instead hit the unique course/sequence index.

## Related / required regression

CURRICULUM-SEQUENCE-001: Browser clear/0/1/fraction and HTTP null/missing/negative/Int32 boundaries, duplicate and concurrent sequence tests with fresh readback and no success notice on rejection.

Also run all endpoint/form cases pointing to this issue in QA/03_TEST_MATRIX.md and the critical regression suite before closure.


## C10 academic batch — 7 November 2026

The sequence field is required, whole-number step one, minimum one. The actual submit handler also refuses blank, zero and fractional sequences before a POST; a valid sequence two is a positive control and retains the next-sequence behavior. API/domain and module publication rules are unchanged.

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
