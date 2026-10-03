# BUG-DATA-0045 — Compliance documents and consents accept unchecked or contradictory person references

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | CONTROLLER-REPRODUCED:25 invalid-person baseline failures; local API repair |
| Final verification | PARTIAL:148 real Identity/HTTP-SQL PASS; prior988 backend/45 controlled TSX/153 adjacent frontend retained; browser/linked/critical OPEN |
| Severity | Major compliance-record integrity |
| Priority | P1 |
| Category | DATA |
| Module | COMPLIANCE |
| Role | See reproduction; same-tenant Admin unless stated otherwise |
| Screen / route | /compliance |
| API | POST /api/academies/{academyId}/compliance/documents; POST /consents |
| Environment | Source review of local working tree; runtime production state not inferred |
| Device/viewport | NOT RUN; use QA/06_DEVICE_VIEWPORT_MATRIX.md where UI applies |
| Baseline | 20bb6047f9edf733ac8e2a226621cc582ec54b3c + pre-existing student UI diff |
| Discovery test / review ID | COMPLIANCE-IDENTITY-001 |
| Evidence classification | Executed direct-controller/InMemory,controlled actual TSX and real Identity/HTTP/fresh SQL; browser/device pending |
| Preconditions | Isolated synthetic fixture from QA/10_TEST_DATA_STRATEGY.md; never customer data |
| Reproduction frequency | Not measured; reproduction instructions are proposed |
| Source | apps/api/Controllers/ComplianceController.cs:28 |
| Class/function | AddDocument / AddConsent |
| Related source | INVENTORY/API_ENDPOINTS.md, CONTRACTS.md, FORMS_AND_FIELDS.md and ASYNC_FORM_RISKS.md |
| Root-cause confidence | HIGH |
| Evidence location | Source excerpt below; source fingerprint in INVENTORY/SOURCE_MANIFEST.md |
| Screenshot | Not captured on pinned baseline |
| Console logs | Not captured; required in retest |
| API request | In isolated fixtures submit local, foreign and missing student/guardian IDs, then both IDs together, for document and consent creation. Read persisted records and compare an eligible single-person control. In the browser, choose Student but select a guardian from the mixed list; repeat Parent with a student. Inspect the typed request member and fresh row, not just the displayed name. |
| API response | 148 synthetic HTTP request/response/status captures in compliance SQL final log; original plain-text400 errors retained |
| Database before/after | Full captured snapshots/fresh entity comparisons,56 exact mutations with actor audits and92 unchanged reads/denials; unrelated tables/fault atomicity not inferred |
| Dependencies | Safe SQL/HTTP/browser harness as applicable; desired policy review where noted |
| Fix commit | Local uncommitted controller guard and typed compliance page; no deployment |
| Retest result | 148 real Identity/HTTP-SQL PASS;76 prior controller/45 controlled TSX retained; browser not run |
| Regression result | Prior988 backend/153 adjacent frontend retained; bounded HTTP-SQL148 PASS; browser/linked/critical/release OPEN |
| Closure notes | Remain OPEN; follow closure requirements in QA README |

## Exact reproduction

In isolated fixtures submit local, foreign and missing student/guardian IDs, then both IDs together, for document and consent creation. Read persisted records and compare an eligible single-person control. In the browser, choose Student but select a guardian from the mixed list; repeat Parent with a student. Inspect the typed request member and fresh row, not just the displayed name.

## Expected

Each record validates its selected same-academy person and supports one documented subject relationship, or explicitly defines an audited dual-subject record.

## Actual / evidence

AddDocument validates only document type/file name and AddConsent only requires one non-null ID. Neither verifies StudentId or GuardianId existence/academy, neither rejects both values, and mapped scalar relationships provide no controller-level protection. The compliance page combines students and guardians into one people array, and both independent PersonPicker instances offer that entire array for either personType. Selecting a guardian under Student sends its GUID as StudentId (and vice versa). The table also resolves subjects against the combined array, so the wrong typed link can still display the expected name. Runtime NOT RUN.

Source snapshot:

```text
27:     [HttpPost("documents")]
28:     public async Task<ActionResult> AddDocument(Guid academyId, DocumentRequest request, CancellationToken cancellationToken)
29:     {
30:         if (string.IsNullOrWhiteSpace(request.DocumentType) || string.IsNullOrWhiteSpace(request.FileName))
```

## Suspected root cause

Person identity and mutually exclusive ownership rules are omitted from create contracts. The UI also discards the person-type distinction in its option list and subject lookup.

## Business impact and blast radius

Compliance documents, consent evidence, review queues and any later family/privacy decision based on these records.

## Related / required regression

COMPLIANCE-IDENTITY-001: HTTP/SQL student/guardian local/foreign/missing/both/null matrix, valid single-subject controls, duplicate/history policy, review/withdraw behavior and fresh-row no-write assertions. Browser record-type/choice-type matrix in both independent forms; changing type clears only its own selection and filters choices to that type. Confirm server rejects swapped typed IDs even if the UI is corrected.

Also run all endpoint/form cases pointing to this issue in QA/03_TEST_MATRIX.md and the critical regression suite before closure.

## Local server repair — 2026-10-01

[Controller repair and evidence](../REPORTS/PHASE_2B_COMPLIANCE_IDENTITY_REPAIR.md): baseline76 cases51 PASS/25 expected FAIL; shared create guard now enforces one existing typed same-academy student/guardian. Full988 backend (76 new) PASS, optional/inactive/lifecycle/history/source/foreign preservation checked via direct controllers/InMemory. Frontend remains mixed and needs the next typed-picker repair; this is not HTTP/SQL/audit/browser/closure proof. No normal dev/Azure/database/service/deployment changes. Issue remains OPEN.

## Local typed UI successor — 2026-10-01

[Frontend repair and evidence](../REPORTS/PHASE_2B_COMPLIANCE_UI_REPAIR.md):4 controlled actual TSX baseline failures; independent typed options, correct typed historic/same-ID subject labels, capture form before await/own-form native and controlled reset, retained other draft, durable success and refresh-only recovery.45 new controlled TSX/153 reused adjacent frontend PASS; TypeScript/target lint PASS. Intermediate mock callback-identity/wrong expected other-ID failures retained and corrected, excluded from product results. Prior988 backend/API/schema/access/shared controls/normal binaries unchanged. No browser/real compliance HTTP-SQL/audit/linked/critical closure; no dev DB/services/Azure/commit/deploy. Next real compliance HTTP/SQL, Sol High. OPEN.

## Real HTTP/SQL successor — 2026-10-01

[HTTP/SQL evidence](../REPORTS/PHASE_2B_COMPLIANCE_SQL_CHECK.md):148 bounded real Identity/HTTP/fresh SQL checks PASS;56 exact mutations with actor audits/92 unchanged captured reads-denials. Typed single subjects,optional/inactive/collision/history,scoped review/task/withdrawal and complete lists verified. QA-only plain-text error parsing assumption caused first retained failure;corrected without product edits,both owned fixtures cleaned. Prior API/frontend/schema/access/normal assemblies/988 backend/45 controlled TSX retained,not rerun. Browser/device/linked/critical/policy/races/audit faults/release OPEN. No normal dev/Azure/commit/deploy. Next accepted certificate enrollment gap;Sol High.
