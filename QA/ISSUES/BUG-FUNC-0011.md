# BUG-FUNC-0011 — Critical academy support requests are omitted from owner high-priority view

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | CONTROLLED + CHROMIUM REPRODUCED / BOUNDED REPAIR |
| Final verification | NOT RUN (full issue) |
| Severity | Major support triage omission |
| Priority | P1 |
| Category | FUNC |
| Module | PLATFORM |
| Role | AcademyAdmin intake; PlatformOwner triage |
| Screen / route | /platform-services; /platform/control?tab=Support |
| API | POST academy platform-services/support-cases; GET /api/platform/support-cases |
| Environment | Source review of local working tree; runtime production state not inferred |
| Device/viewport | NOT RUN; use QA/06_DEVICE_VIEWPORT_MATRIX.md where UI applies |
| Baseline | 20bb6047f9edf733ac8e2a226621cc582ec54b3c + pre-existing student UI diff |
| Discovery test / review ID | PLATFORM-SUPPORT-001 |
| Evidence classification | Historical trace retained; controlled and Chromium reproduction/repair verified |
| Preconditions | Isolated synthetic fixture from QA/10_TEST_DATA_STRATEGY.md; never customer data |
| Reproduction frequency | Not measured; reproduction instructions are proposed |
| Source | apps/web/src/app/platform/control/page.tsx:261 |
| Class/function | urgentCases / PlatformServicesPage priority options |
| Related source | INVENTORY/API_ENDPOINTS.md, CONTRACTS.md, FORMS_AND_FIELDS.md and ASYNC_FORM_RISKS.md |
| Root-cause confidence | HIGH |
| Evidence location | Source excerpt below; source fingerprint in INVENTORY/SOURCE_MANIFEST.md |
| Screenshot | Not captured on pinned baseline |
| Console logs | Not captured; required in retest |
| API request | Create an open Critical support request through academy UI in synthetic fixtures. As platform owner compare All cases with High priority count and modal; compare High/Urgent/Normal and closed controls. |
| API response | Not captured for this issue; use synthetic request/response in isolated reproduction |
| Database before/after | Not executed; fixture and fresh-context assertions defined below |
| Dependencies | Safe SQL/HTTP/browser harness as applicable; desired policy review where noted |
| Fix commit | fe0d87af3ba9694ddc3a5c86f9bca9cf54283b47 (C16 triage predicate) |
| Retest result | C16 new/retained54/54 controlled and8/8 native PASS; live SQL/device/critical acceptance pending |
| Regression result | Frontend2,042 PASS /3 genuine SQL-receipt FAIL, zero skips; unchanged C15 API2,084 PASS reused |
| Closure notes | Remain OPEN; follow closure requirements in QA README |

## Exact reproduction

Create an open Critical support request through academy UI in synthetic fixtures. As platform owner compare All cases with High priority count and modal; compare High/Urgent/Normal and closed controls.

## Expected

Highest offered academy priority appears in the owner high-priority triage count and detail view until resolved or closed.

## Actual / evidence

Academy offers Critical and API preserves it. Owner urgentCases accepts only High or Urgent, so Critical requests appear in All cases but not High priority. Runtime NOT RUN.

Source snapshot:

```text
260:   const openCases = cases.filter((item) => !["Resolved", "Closed"].includes(item.status));
261:   const urgentCases = cases.filter((item) => ["High", "Urgent"].includes(item.priority) && !["Resolved", "Closed"].includes(item.status));
262:   const resolvedCases = cases.filter((item) => ["Resolved", "Closed"].includes(item.status));
263:   const caseModalTitle = caseModal === "open" ? "Open support cases" : caseModal === "urgent" ? "High-priority support cases" : caseModal === "resolved" ? "Resolved support cases" : "All support cases";
```

## Suspected root cause

Producer and triage consumer use different priority vocabularies.

## Business impact and blast radius

Owner triage of the highest academy-selected support priority; no evidence that all-case visibility is lost.

## Related / required regression

PLATFORM-SUPPORT-001: HTTP plus browser producer-to-consumer priority matrix for Low/Normal/High/Critical/Urgent, open/in-progress/resolved/closed, exact count and modal membership, and backward-compatible normalization of existing records.

Also run all endpoint/form cases pointing to this issue in QA/03_TEST_MATRIX.md and the critical regression suite before closure.

## C16 bounded triage repair — 7 November 2026

Qualified original baseline:19/26 controlled and6/8 native PASS. Seven
controlled assertions and both native high-priority views omit Critical and
API-preserved priority variants. The predicate now includes Critical alongside
High/Urgent with priority whitespace/case normalization; Resolved/Closed remain
excluded. No priority payload, backend permission or status-write handler changed.

New26 and retained C11 status/navigation28 controlled checks pass54/54, with
zero skips. Actual academy TSX emits Critical and feeds the owner TSX through
a synthetic response; all priority/status combinations and other count/detail
views are checked. Final native/full regression and the exact Desktop SQL/UI
return are in the [C16 report](../HANDOFF/C16_SUPPORT_TRIAGE_2026-11-07.md).
Keep OPEN until real authority/SQL/device/critical closure requirements pass.

Final native8/8 pass on the configured exact-source export with zero runtime/
interceptor errors or API writes. All570 export hashes are unchanged. Normal
sandbox and owned cleanup pass. Full frontend2,042 PASS /3 missing genuine SQL
receipt FAIL, zero skips; build/types/lint pass (zero errors/62 warnings). Two
intermediate timing failures remain retained; the final driver checks the same
views in focused viewport sessions without loosening assertions/timeouts.
