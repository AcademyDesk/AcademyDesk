# BUG-DATA-0014 — Downloaded child certificate names the signed-in guardian

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | REPRODUCED — bounded local repair verified; full closure pending |
| Final verification | 25 real Identity/HTTP-SQL cases PASS; HTML contract verified; browser/device/critical NOT RUN |
| Severity | Major document identity integrity |
| Priority | P1 |
| Category | DATA |
| Module | FAMILY |
| Role | See reproduction; same-tenant Admin unless stated otherwise |
| Screen / route | /portal |
| API | GET portal students/{studentId}/certificates/{certificateNumber}/download |
| Environment | Source review of local working tree; runtime production state not inferred |
| Device/viewport | NOT RUN; use QA/06_DEVICE_VIEWPORT_MATRIX.md where UI applies |
| Baseline | 20bb6047f9edf733ac8e2a226621cc582ec54b3c + pre-existing student UI diff |
| Discovery test / review ID | FAMILY-DOCUMENT-001 |
| Evidence classification | Static trace; runtime reproduction pending |
| Preconditions | Isolated synthetic fixture from QA/10_TEST_DATA_STRATEGY.md; never customer data |
| Reproduction frequency | Not measured; reproduction instructions are proposed |
| Source | apps/api/Controllers/PortalController.cs:283 |
| Class/function | DownloadCertificate |
| Related source | INVENTORY/API_ENDPOINTS.md, CONTRACTS.md, FORMS_AND_FIELDS.md and ASYNC_FORM_RISKS.md |
| Root-cause confidence | HIGH |
| Evidence location | Source excerpt below; source fingerprint in INVENTORY/SOURCE_MANIFEST.md |
| Screenshot | Not captured on pinned baseline |
| Console logs | Not captured; required in retest |
| API request | Use a permitted guardian whose display name differs from the child. Download the child issued certificate and inspect recipient name. Compare child login and second guardian. |
| API response | Not captured for this issue; use synthetic request/response in isolated reproduction |
| Database before/after | Not executed; fixture and fresh-context assertions defined below |
| Dependencies | Safe SQL/HTTP/browser harness as applicable; desired policy review where noted |
| Fix commit | Local uncommitted method-only repair; no deployment |
| Retest result | 25 bounded HTTP-SQL cases and 1019 existing backend tests PASS |
| Regression result | Recipient encoding/ownership/status/permission/no-write controls PASS; wider gates OPEN |
| Closure notes | Remain OPEN; follow closure requirements in QA README |

## Exact reproduction

Use a permitted guardian whose display name differs from the child. Download the child issued certificate and inspect recipient name. Compare child login and second guardian.

## Expected

Certificate names its student subject consistently regardless of who downloads it.

## Actual / evidence

The certificate body uses authenticated user.DisplayName rather than the student represented by certificate.StudentId.

Source snapshot:

```text
282:         if (certificate is null) return NotFound();
283:         var document = $"<html><body style='font-family:Georgia;text-align:center;padding:80px;border:12px solid #1674c4'><h1>Certificate of achievement</h1><p>This certifies that</p><h2>{WebUtility.HtmlEncode(user.DisplayName)}</h2><p>has completed</p><h2>{WebUtility.HtmlEncode(certificate.Title)}</h2><p>Certificate no. {WebUtility.HtmlEncode(certificate.CertificateNumber)}</p><p>Issued {certificate.IssuedDate:dd MMM yyyy}</p></body></html>";
284:         return File(Encoding.UTF8.GetBytes(document), "text/html", $"{certificate.CertificateNumber}.html");
285:     }
```

## Suspected root cause

Document recipient confused with requesting principal.

## Business impact and blast radius

Guardian-downloaded certificates and students whose account display name differs from their student record.

## Related / required regression

FAMILY-DOCUMENT-001: Compare HTML recipient against fresh student identity using child and two guardian sessions; retain encoding and ownership denial assertions.

Also run all endpoint/form cases pointing to this issue in QA/03_TEST_MATRIX.md and the critical regression suite before closure.

## Local successor — 2026-10-02

[Family certificate download repair](../REPORTS/PHASE_2B_CERTIFICATE_FAMILY_DOWNLOAD_REPAIR.md) supersedes the original static trace above only within this bounded path. Real permitted-guardian wrong-name reproduction retained; the method now uses its scoped certificate learner's current first/last name, not the requester account.25 real Identity/HTTP-SQL cases PASS include learner/two guardians, safe HTML encoding, optional null batch/notes, case/status, ownership and live permission/revocation controls with unchanged captured snapshots.1019 existing backend tests rerun PASS. Certificate document permission is fixed alongside BUG-SEC-0005, not its invoice/nested-resource paths. Initial build/launch sequencing failure retained/excluded; owned synthetic resources cleaned. HTML bytes are not browser/PDF/device or immutable original-name evidence. Issue/Phase2B/critical/release stay OPEN; no Azure/commit/deploy. Continue Sol High on remaining guardian permission paths.
