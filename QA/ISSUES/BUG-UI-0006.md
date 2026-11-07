# BUG-UI-0006 — Academic date-only values can display a different day outside India

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | RUNTIME-REPRODUCED in controlled TSX/Chromium; C10 partial repair PASS |
| Final verification | C10 controlled regression/browser PASS; full original host/device/critical closure pending |
| Severity | Moderate academic calendar display error |
| Priority | P2 |
| Category | UI |
| Module | ACADEMIC |
| Role | See reproduction; same-tenant Admin unless stated otherwise |
| Screen / route | /academic-periods; /holidays |
| API | GET academic-periods |
| Environment | Source review of local working tree; runtime production state not inferred |
| Device/viewport | NOT RUN; use QA/06_DEVICE_VIEWPORT_MATRIX.md where UI applies |
| Baseline | 20bb6047f9edf733ac8e2a226621cc582ec54b3c + pre-existing student UI diff |
| Discovery test / review ID | ACADEMIC-DATE-001 |
| Evidence classification | Static trace; runtime reproduction pending |
| Preconditions | Isolated synthetic fixture from QA/10_TEST_DATA_STRATEGY.md; never customer data |
| Reproduction frequency | Not measured; reproduction instructions are proposed |
| Source | apps/web/src/app/academic-periods/page.tsx:121 |
| Class/function | formatDate |
| Related source | INVENTORY/API_ENDPOINTS.md, CONTRACTS.md, FORMS_AND_FIELDS.md and ASYNC_FORM_RISKS.md |
| Root-cause confidence | HIGH |
| Evidence location | Source excerpt below; source fingerprint in INVENTORY/SOURCE_MANIFEST.md |
| Screenshot | Not captured on pinned baseline |
| Console logs | Not captured; required in retest |
| API request | Use a stored year/term date2026-09-01 and render the page with browser timezone America/Los_Angeles, then Asia/Kolkata. Compare displayed start/end with the date-only response. |
| API response | Not captured for this issue; use synthetic request/response in isolated reproduction |
| Database before/after | Not executed; fixture and fresh-context assertions defined below |
| Dependencies | Safe SQL/HTTP/browser harness as applicable; desired policy review where noted |
| Fix commit | Not implemented (Phase 1) |
| Retest result | NOT RUN |
| Regression result | NOT RUN |
| Closure notes | Remain OPEN; follow closure requirements in QA README |

## Exact reproduction

Use a stored year/term date2026-09-01 and render the page with browser timezone America/Los_Angeles, then Asia/Kolkata. Compare displayed start/end with the date-only response.

## Expected

A date-only academic boundary displays the stored calendar date regardless of viewer timezone.

## Actual / evidence

Formatter parses noon in browser local timezone then formats in Asia/Kolkata. Noon Los Angeles in September becomes00:30 the next day in India, displaying September2 for September1. Holiday date rendering uses the same local-noon to IST pattern. Source finding; browser NOT RUN.

Source snapshot:

```text
120:       timeZone: "Asia/Kolkata",
121:     }).format(new Date(`${value}T12:00:00`));
122:   return (
123:     <main className="enterprise-settings periods-standard min-h-screen">
```

## Suspected root cause

Date-only value is converted through mismatched implicit local and explicit display timezones.

## Business impact and blast radius

Academic year and term start/end displays for some non-India viewers; holiday displays also affected; stored dates are unchanged.

## Related / required regression

ACADEMIC-DATE-001: Browser timezone matrix India/UTC/Los Angeles/Honolulu and positive offsets at DST and year/leap boundaries; assert exact stored date displayed, not only a valid formatted string.

Also run all endpoint/form cases pointing to this issue in QA/03_TEST_MATRIX.md and the critical regression suite before closure.


## C10 academic batch — 7 November 2026

Academic periods and holiday dates now parse a date-only value at an explicit UTC time and format it in UTC. Civil dates no longer depend on the browser timezone; the API date string and domain dates are unchanged. Honolulu reproduced the one-day drift; India, UTC and Kiritimati remain positive controls.

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
