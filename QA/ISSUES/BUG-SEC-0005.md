# BUG-SEC-0005 — Guardian download and class-history paths bypass document and finance flags

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | REPRODUCED — certificate, invoice and nested-resource paths locally repaired |
| Final verification | Certificate previous25 and guardian-flags55 bounded HTTP-SQL PASS; broader browser/critical gates OPEN |
| Severity | Major restricted family data exposure |
| Priority | P1 |
| Category | SEC |
| Module | FAMILY |
| Role | See reproduction; same-tenant Admin unless stated otherwise |
| Screen / route | /portal |
| API | GET portal students/{studentId}/invoices/{invoiceId}/download; certificates/{certificateNumber}/download; student details |
| Environment | Source review of local working tree; runtime production state not inferred |
| Device/viewport | NOT RUN; use QA/06_DEVICE_VIEWPORT_MATRIX.md where UI applies |
| Baseline | 20bb6047f9edf733ac8e2a226621cc582ec54b3c + pre-existing student UI diff |
| Discovery test / review ID | SECURITY-GUARDIAN-002 |
| Evidence classification | Static trace; runtime reproduction pending |
| Preconditions | Isolated synthetic fixture from QA/10_TEST_DATA_STRATEGY.md; never customer data |
| Reproduction frequency | Not measured; reproduction instructions are proposed |
| Source | apps/api/Controllers/PortalController.cs:265 |
| Class/function | DownloadInvoice / DownloadCertificate / Student |
| Related source | INVENTORY/API_ENDPOINTS.md, CONTRACTS.md, FORMS_AND_FIELDS.md and ASYNC_FORM_RISKS.md |
| Root-cause confidence | HIGH |
| Evidence location | Source excerpt below; source fingerprint in INVENTORY/SOURCE_MANIFEST.md |
| Screenshot | Not captured on pinned baseline |
| Console logs | Not captured; required in retest |
| API request | Seed an active child with two guardians: portal access allowed but finance/documents denied; academic progress allowed. Request known invoice/certificate identifiers directly and inspect classHistory resource URLs. Repeat with permitted, revoked, foreign-child and inactive-child controls. |
| API response | Not captured for this issue; use synthetic request/response in isolated reproduction |
| Database before/after | Not executed; fixture and fresh-context assertions defined below |
| Dependencies | Safe SQL/HTTP/browser harness as applicable; desired policy review where noted |
| Fix commit | Local uncommitted certificate/document/finance guards; no deployment |
| Retest result | Guardian successor55 HTTP-SQL cases/1019 existing backend rerun PASS; previous certificate25 retained |
| Regression result | Eight finance/document/academic combinations, scoped/revoked/inactive/no-write controls PASS; wider gates OPEN |
| Closure notes | Remain OPEN; follow closure requirements in QA README |

## Exact reproduction

Seed an active child with two guardians: portal access allowed but finance/documents denied; academic progress allowed. Request known invoice/certificate identifiers directly and inspect classHistory resource URLs. Repeat with permitted, revoked, foreign-child and inactive-child controls.

## Expected

Restricted documents and financial data are denied at each endpoint and nested projection, not only hidden in the UI.

## Actual / evidence

Download actions check CanAccessStudent but not CanViewFinance/CanViewDocuments. Student filters top-level resources/certificates yet classHistory resources depend only on academic access. Static trace; runtime NOT RUN.

Source snapshot:

```text
264:     [HttpGet("students/{studentId:guid}/invoices/{invoiceId:guid}/download")]
265:     public async Task<IActionResult> DownloadInvoice(Guid studentId, Guid invoiceId, CancellationToken token)
266:     {
267:         var user = await users.GetUserAsync(User);
```

## Suspected root cause

Projection permissions are not reused by download actions or nested resource lists.

## Business impact and blast radius

Linked guardians with limited access; requires a valid target identifier for direct downloads.

## Related / required regression

SECURITY-GUARDIAN-002: Real HTTP permissions matrix across flags, revoked link, active child and foreign tenant; verify response bytes and nested JSON contain no restricted data.

Also run all endpoint/form cases pointing to this issue in QA/03_TEST_MATRIX.md and the critical regression suite before closure.

## Certificate-path successor — 2026-10-02

[Family download repair](../REPORTS/PHASE_2B_CERTIFICATE_FAMILY_DOWNLOAD_REPAIR.md) retains a real restricted-guardian certificate disclosure baseline and locally enforces live CanViewDocuments on the scoped active guardian link.25 shared certificate cases PASS, not a second25-case suite: permission revoked after login, revoked link, portal-off, foreign/wrong child, inactive child and independent permitted-guardian controls; no certificate HTML on denial and unchanged captured SQL/Identity/audit snapshots.1019 existing backend tests rerun PASS; normal development/Azure untouched and owned synthetic resources cleaned. Original static description remains historical for this certificate branch, not evidence of whole-issue closure. Invoice finance/document checks, nested class-history resources, all-role/dual-link/race/browser/device/linked/critical/release gates remain OPEN. Next remaining paths from this accepted issue; Sol High.

## Invoice/nested-resource successor — 2026-10-02

[Guardian flags repair](../REPORTS/PHASE_2B_GUARDIAN_FLAGS_REPAIR.md) supersedes the previous remaining-path statement. Two real baseline disclosures reproduced. Invoice download now requires live finance permission (not an additional document permission); nested class-history attachments require documents permission while allowed academic metadata stays visible.55 real Identity/HTTP-SQL cases PASS (22 details,25 invoice,8 retained certificate controls), all captured GET snapshots unchanged;1019 existing backend tests rerun PASS. Previous certificate method/evidence and normal development binaries retained; two new owned SQL runs cleaned; Azure/dev database/services untouched. Original static-only fields above are historical, not the current runtime evidence classification. All three known branches are bounded locally repaired; complete role/dual-link/race/full-portal/browser/device/critical/release closure remains OPEN. Next accepted BUG-SEC-0009 announcement audience gap; Sol High.
