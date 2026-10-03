# BUG-FUNC-0006 — Finance pages require lookup APIs denied to the FinanceUser role

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | RUNTIME-REPRODUCED / LOCAL-REPAIR (student/Governance/invoice-settings dependencies; browser/critical pending) |
| Final verification | Invoice settings: two fresh runs ×34 PASS, API 222/222, controlled frontend 43/43; earlier student/Governance evidence retained; live browser/device/full critical pending |
| Severity | Major role workflow availability |
| Priority | P1 |
| Category | FUNC |
| Module | FINANCE |
| Role | See reproduction; same-tenant Admin unless stated otherwise |
| Screen / route | /payments; /invoices; /finance-governance |
| API | Current finance lookups: invoices/student-options; invoices/document-settings GET; finance-governance/collection-tasks GET/PATCH. Generic students/admin-work-items and Governance settings restrictions remain intact |
| Environment | Source review of local working tree; runtime production state not inferred |
| Device/viewport | NOT RUN; use QA/06_DEVICE_VIEWPORT_MATRIX.md where UI applies |
| Baseline | 20bb6047f9edf733ac8e2a226621cc582ec54b3c + pre-existing student UI diff |
| Discovery test / review ID | SECURITY-ROLE-003 |
| Evidence classification | Accepted UI trace plus real Identity/HTTP/SQL dependency evidence; not live browser proof |
| Preconditions | Isolated synthetic fixture from QA/10_TEST_DATA_STRATEGY.md; never customer data |
| Reproduction frequency | 2/2 fresh corrected local SQL access runs, 2026-10-01; API dependency portion only |
| Source | apps/web/src/app/payments/page.tsx:26 |
| Class/function | load / InvoicesPage.load / FinanceGovernancePage.load |
| Related source | INVENTORY/API_ENDPOINTS.md, CONTRACTS.md, FORMS_AND_FIELDS.md and ASYNC_FORM_RISKS.md |
| Root-cause confidence | HIGH |
| Evidence location | Phase 2B finance access report below; original source excerpt retained |
| Screenshot | Not captured on pinned baseline |
| Console logs | Not captured; required in retest |
| API request | In active synthetic academy with Finance and FinanceControls modules, sign in as FinanceUser without extra custom grants. Verify direct finance reads allowed, then load payments/invoices/governance and capture all prerequisite responses. Compare same-tenant admin control. |
| API response | FinanceUser direct finance actions 200/201; Students/AdminWorkItems GET 403 in both corrected runs; browser load NOT RUN |
| Database before/after | Rejected lookup captured invoice/payment/adjustment/payroll/notification snapshots unchanged |
| Dependencies | Safe SQL/HTTP/browser harness as applicable; desired policy review where noted |
| Fix commit | Local working-tree student/Governance/invoice-settings dependency repairs; not committed or deployed |
| Retest result | Finance-only invoice-settings defaults/branding/roles/module/tenant/read-only/inactive guards PASS twice on real Identity/HTTP/SQL; browser NOT RUN |
| Regression result | 222 API /43 controlled frontend PASS; two fresh runs ×34 invoice-settings HTTP/SQL checks PASS; earlier lookup/Governance evidence historical, not full critical suite |
| Closure notes | Remain OPEN; follow closure requirements in QA README |

## Local Finance-only invoice-settings repair — 2026-10-01

[Invoice document-settings repair](../REPORTS/PHASE_2B_INVOICE_SETTINGS_REPAIR.md) gives the Invoice page its seven preview fields through a read-only Finance-scoped route. Finance-only prerequisites no longer depend on FinanceControls settings management. Existing Governance/module/write restrictions, generic student/task restrictions, nullable branding and four templates are preserved; no row insertion on GET. Earlier checkpoints below are historical; actual browser/device/full critical acceptance remains pending, so OPEN.

## Local Governance repair checkpoint — 2026-10-01 (historical)

[Governance access repair](../REPORTS/PHASE_2B_GOVERNANCE_ACCESS_REPAIR.md) adds minimized same-tenant Collections-only GET/PATCH under finance.manage/FinanceControls, replaces both generic dependencies, preserves all fields except stage/optional promise date and retains refresh-aware success feedback. Generic admin work items remain forbidden to FinanceUser. API 201/201, controlled frontend 22/22, two final fresh SQL runs ×82 primary cases PASS, including role/type/tenant/grant/module/inactive controls, overposting rejection-by-binding and audit-fault rollback/recovery. Current owned resources cleaned; no commit/deployment. Original student-lookup results below retained historical. Live browser/critical/Finance-only Invoice settings coupling and broader policy/device/phase gates pending; OPEN.

## Local student-lookup repair checkpoint — 2026-10-01 (historical)

[Finance lookup repair](../REPORTS/PHASE_2B_FINANCE_LOOKUP_REPAIR.md) adds same-tenant `invoices/student-options` under existing finance permission/module gates and switches Payments/Invoices only. ID/names/email/phone preserve selectors and invoice contact; no full profile/broad management grant. API 187/187, controlled TSX 13/13 and two fresh Identity/HTTP/SQL runs ×56 primary cases PASS. Owned cleanup, no commit/deployment. Generic Students remains 403 for FinanceUser; generic governance work-items remains 403 and OPEN. Live browser, Finance-only invoice-settings module coupling, broader/critical/device/policy acceptance pending. Historical observations/source below retained, not current consumer code.

## Runtime dependency checkpoint — 2026-10-01 (historical before repair)

[Finance access regression](../REPORTS/PHASE_2B_FINANCE_ACCESS_REGRESSION.md) reuses accepted G03 mappings. In each of two fresh real Identity/HTTP/SQL runs FinanceUser succeeds on the mapped finance actions but Students and AdminWorkItems GET return 403; captured financial/notification state unchanged. The accepted page source requires those prerequisites. This confirms the API dependency portion, not a live browser page failure or every prerequisite. No broad students.manage grant, policy change or repair; least-privilege lookup design/browser/full critical gates remain open. Initial compiler and over-length synthetic fixture failures are excluded from these two counted runs. Original proposed reproduction below is retained.

## Exact reproduction

In active synthetic academy with Finance and FinanceControls modules, sign in as FinanceUser without extra custom grants. Verify direct finance reads allowed, then load payments/invoices/governance and capture all prerequisite responses. Compare same-tenant admin control.

## Expected

A supported finance role can use its intended workflows without unrelated broad student-management authority; unsupported scope is explicitly communicated rather than a blanket load failure.

## Actual / evidence

Payments/invoices require successful StudentsController lookup, which needs students.manage not granted to FinanceUser. Governance requires AdminWorkItemsController, unmapped in PermissionCatalog and denied to non-admins. Shared Promise.all success gates fail entire views even though finance APIs are permitted.

Source snapshot:

```text
25:     if (!id) return;
26:     const [studentResponse, invoiceResponse, paymentResponse] = await Promise.all([
27:       academyApi(`/api/academies/${id}/students`, { cache: "no-store" }), academyApi(`/api/academies/${id}/invoices`, { cache: "no-store" }), academyApi(`/api/academies/${id}/payments`, { cache: "no-store" }),
28:     ]);
```

## Suspected root cause

Page prerequisite data permissions are broader/different from primary feature permissions.

## Business impact and blast radius

FinanceUser and finance-only custom roles; granting broad management as a workaround risks overprivilege. No backend permission weakening proposed.

## Related / required regression

SECURITY-ROLE-003: Real HTTP+browser dependency matrix for admin, FinanceUser, finance-only grant and forbidden actors with enabled/disabled modules; verify intended data minimization and usable allowed workflow after policy review.

Also run all endpoint/form cases pointing to this issue in QA/03_TEST_MATRIX.md and the critical regression suite before closure.
