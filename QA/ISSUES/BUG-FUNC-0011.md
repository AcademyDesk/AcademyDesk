# BUG-FUNC-0011 — Critical academy support requests are omitted from owner high-priority view

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | STATIC-FINDING |
| Final verification | NOT RUN |
| Severity | Major support triage omission |
| Priority | P1 |
| Category | FUNC |
| Module | PLATFORM |
| Role | See reproduction; same-tenant Admin unless stated otherwise |
| Screen / route | /platform-services; /platform/control?tab=Support |
| API | POST academy platform-services/support-cases; GET /api/platform/support-cases |
| Environment | Source review of local working tree; runtime production state not inferred |
| Device/viewport | NOT RUN; use QA/06_DEVICE_VIEWPORT_MATRIX.md where UI applies |
| Baseline | 20bb6047f9edf733ac8e2a226621cc582ec54b3c + pre-existing student UI diff |
| Discovery test / review ID | PLATFORM-SUPPORT-001 |
| Evidence classification | Static trace; runtime reproduction pending |
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
| Fix commit | Not implemented (Phase 1) |
| Retest result | NOT RUN |
| Regression result | NOT RUN |
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
