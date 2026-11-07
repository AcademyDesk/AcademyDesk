# BUG-FUNC-0008 — Subject-fee form rejects fractional amounts accepted by the API

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | RUNTIME-REPRODUCED in controlled TSX/Chromium; C10 partial repair PASS |
| Final verification | C10 controlled regression/browser PASS; full original host/device/critical closure pending |
| Severity | Moderate fee entry limitation |
| Priority | P2 |
| Category | FUNC |
| Module | SHARED |
| Role | See reproduction; same-tenant Admin unless stated otherwise |
| Screen / route | /student-fees; /student-profile |
| API | POST student fee-arrangements |
| Environment | Source review of local working tree; runtime production state not inferred |
| Device/viewport | NOT RUN; use QA/06_DEVICE_VIEWPORT_MATRIX.md where UI applies |
| Baseline | 20bb6047f9edf733ac8e2a226621cc582ec54b3c + pre-existing student UI diff |
| Discovery test / review ID | FEES-AMOUNT-001 |
| Evidence classification | Static trace; runtime reproduction pending |
| Preconditions | Isolated synthetic fixture from QA/10_TEST_DATA_STRATEGY.md; never customer data |
| Reproduction frequency | Not measured; reproduction instructions are proposed |
| Source | apps/web/src/components/student-fee-arrangements.tsx:99 |
| Class/function | StudentFeeArrangements.add amount input |
| Related source | INVENTORY/API_ENDPOINTS.md, CONTRACTS.md, FORMS_AND_FIELDS.md and ASYNC_FORM_RISKS.md |
| Root-cause confidence | HIGH |
| Evidence location | Source excerpt below; source fingerprint in INVENTORY/SOURCE_MANIFEST.md |
| Screenshot | Not captured on pinned baseline |
| Console logs | Not captured; required in retest |
| API request | Enter a valid subject, Monthly frequency and amount 1250.50, then submit via browser. Compare 1250 and an API request for 1250.50; inspect input stepMismatch and whether POST is dispatched. |
| API response | Not captured for this issue; use synthetic request/response in isolated reproduction |
| Database before/after | Not executed; fixture and fresh-context assertions defined below |
| Dependencies | Safe SQL/HTTP/browser harness as applicable; desired policy review where noted |
| Fix commit | Not implemented (Phase 1) |
| Retest result | NOT RUN |
| Regression result | NOT RUN |
| Closure notes | Remain OPEN; follow closure requirements in QA README |

## Exact reproduction

Enter a valid subject, Monthly frequency and amount 1250.50, then submit via browser. Compare 1250 and an API request for 1250.50; inspect input stepMismatch and whether POST is dispatched.

## Expected

Subject fees support the agreed two-decimal money precision, consistent with decimal(18,2) storage and adjacent fee forms.

## Actual / evidence

The number input has min=1 and no step, so standard numeric input step defaults to1. Fractional amounts mismatch that step, while API positive-decimal validation and storage accept them. Runtime browser check pending.

Source snapshot:

```text
98:           type="number"
99:           min="1"
100:           placeholder="Amount"
101:           required
```

## Suspected root cause

Money input omits decimal step configuration.

## Business impact and blast radius

Shared subject-fee add editor in Student Fee Details and Student Profile; admission fee and fee-plan inputs already specify step0.01.

## Related / required regression

FEES-AMOUNT-001: Browser/native validity and HTTP/SQL tests for 1,1.01,1250.50,zero,negative,sub1 and excessive precision; enforce agreed minimum separately from decimal increment.

Also run all endpoint/form cases pointing to this issue in QA/03_TEST_MATRIX.md and the critical regression suite before closure.


## C10 academic batch — 7 November 2026

The shared subject-fee input now permits positive paisa increments with min/step0.01, matching the existing positive-amount API contract. Real browser keyboard/HTML/form submissions accept1.50,0.50and0.01exactly. The component is shared by student-fees/student-profile; the parent student-fees route is browser-tested here, not a claim of all parent/role acceptance.

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
