# BUG-FUNC-0032 — Certificate preview can be printed before issuance and diverge from the register

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | CONTROLLED-SOURCE-CONFIRMED; synthetic actual React/DOM observed; PDF/full-portal pending |
| Final verification | NOT RUN |
| Severity | Major credential issuance integrity |
| Priority | P1 |
| Category | FUNC |
| Module | COMPLIANCE |
| Role | See reproduction; same-tenant Admin unless stated otherwise |
| Screen / route | /certificates |
| API | POST /api/academies/{academyId}/certificates; GET certificate register |
| Environment | Source review of local working tree; runtime production state not inferred |
| Device/viewport | In-app desktop 1280x720/mobile 390x844 simulated; physical devices NOT RUN |
| Baseline | 20bb6047f9edf733ac8e2a226621cc582ec54b3c + pre-existing student UI diff |
| Discovery test / review ID | CERTIFICATE-PRINT-ISSUANCE-001 |
| Evidence classification | Original static trace and controlled TSX retained; actual React/DOM with synthetic API/QA shell; no saved/native PDF inspection |
| Preconditions | Isolated synthetic fixture from QA/10_TEST_DATA_STRATEGY.md; never customer data |
| Reproduction frequency | Not measured; reproduction instructions are proposed |
| Source | apps/web/src/app/certificates/page.tsx:369 |
| Class/function | CertificatesPage preview / issue / print |
| Related source | INVENTORY/API_ENDPOINTS.md, CONTRACTS.md, FORMS_AND_FIELDS.md and ASYNC_FORM_RISKS.md |
| Root-cause confidence | HIGH |
| Evidence location | Source excerpt below; source fingerprint in INVENTORY/SOURCE_MANIFEST.md |
| Screenshot | Not captured on pinned baseline |
| Console logs | Not captured; required in retest |
| API request | In an isolated academy, select a learner and edit title, theme and issue date. Press Print / save PDF before Issue certificate, then check the print output and fresh certificate register. Issue once, alter the draft and print again; compare the saved row, certificate number and verification code. Repeat a failed Issue response and revoked record. |
| API response | Not captured for this issue; use synthetic request/response in isolated reproduction |
| Database before/after | Not executed; fixture and fresh-context assertions defined below |
| Dependencies | Safe SQL/HTTP/browser harness as applicable; desired policy review where noted |
| Fix commit | Local certificate page/scoped CSS repair; not committed/deployed |
| Retest result | 50/50 controlled print checks retained; synthetic actual DOM isolation/guards/recovery verified; PDF/full-portal pending |
| Regression result | 143/143 controlled TSX PASS (50 print + 48 retained eligibility + 45 retained compliance); TypeScript PASS; lint 0 errors/2 inherited image warnings |
| Closure notes | Remain OPEN; follow closure requirements in QA README |

## Exact reproduction

In an isolated academy, select a learner and edit title, theme and issue date. Press Print / save PDF before Issue certificate, then check the print output and fresh certificate register. Issue once, alter the draft and print again; compare the saved row, certificate number and verification code. Repeat a failed Issue response and revoked record.

## Expected

An official printable credential is tied to a successfully persisted Issued record, contains its immutable number/verification identity and reflects that saved version. A draft print is visibly watermarked or disabled, and failure or revocation cannot appear as newly issued.

## Actual / evidence

The Print / save PDF button always calls window.print on the live draft preview. Print CSS displays the preview as the full print page. The preview is not bound to any certificate row and contains no certificate number, verification code or status. No Issue call is required, and later draft edits change what prints even after a different record was saved. Static source trace; runtime NOT RUN.

Source snapshot:

```text
368:                 className="enterprise-action-button-secondary"
369:                 onClick={() => window.print()}
370:               >
371:                 Print / save PDF
```

## Suspected root cause

Draft preview and printable credential share one component without issuance state or saved-certificate identity.

## Business impact and blast radius

Administrators can produce apparently issued documents with no register or verification record, or print contents that differ from the registered credential.

## Related / required regression

CERTIFICATE-PRINT-ISSUANCE-001: Browser print/PDF capture plus HTTP/SQL pre-issue, successful issue, rejected issue, post-issue draft edit and revoked/replaced matrices. Verify saved identity/status and printed text match; never use customer records.

Also run all endpoint/form cases pointing to this issue in QA/03_TEST_MATRIX.md and the critical regression suite before closure.

## Local successor — 2026-10-02

[Saved print UI repair](../REPORTS/PHASE_2B_CERTIFICATE_PRINT_UI_REPAIR.md) retains four expected baseline assertion failures and verifies the bounded repair with 50 controlled print checks plus 93 retained regressions. Official print preparation uses an explicitly selected saved Issued record, rechecks scoped saved data with four no-store GETs, renders number/code/status and excludes unsaved draft/branding edits. Non-approved previews carry a watermark; failed/uncertain issuance, missing/revoked/replaced records and failed reads cannot authorize a print. Afterprint/manual close/native-error recovery unlocks the page without claiming a PDF was saved.

The original evidence above describes the pre-repair source, preserved separately. Controlled tests capture JSX, not browser HTML/PDF. Real React lifecycle, native dialog, manual-print watermark, layout/pagination/logo/font readiness and desktop/mobile/device support remain unverified. Certificate data stores IDs, not original learner/class names or branding snapshots; reprints use current saved references. Separate-read status races and linked/critical/release gates remain OPEN. Prior 1019 backend/62 HTTP-SQL passes are retained, not rerun. No API/schema/security/shared-control/dev DB/service/Azure/commit/deploy change. Next: same certificate browser/PDF verification, Sol High.

## Browser successor — 2026-10-02

[Certificate real-browser check](../REPORTS/PHASE_2B_CERTIFICATE_BROWSER_CHECK.md) adds actual React/DOM evidence using only synthetic loopback data and a QA shell/print probe. Saved selection, draft isolation, revoked/replaced/failed-read/rejected-issue guards, returned-ID selection, independent programme/no-note behavior and recovery were observed. One scoped hidden-logo-input CSS rule fixes horizontal overflow. Final mobile390/desktop1280 dropdown placement and hit-testing pass; 143 controlled regressions PASS. The fact ledger retains one wrong-current-selection QA expectation and its clean-reload correction; observations are not independent scenario counts.

Native print was attempted, cancelled and the page recovered, but no PDF/native-preview content was saved or inspected. Simulated afterprint is explicitly not native event proof. Full portal auth/SQL, physical devices, print compositor/pagination/long content/logos/fonts/themes, races and linked/critical/release gates remain OPEN. Earlier browser-NOT-RUN wording above is historical, superseded only within this bounded synthetic DOM scope. API/schema/shared controls/normal binaries/predecessor evidence retained; owned QA processes/tab cleaned/viewport reset. No dev DB/services/Azure/commit/deploy changes. Next same certificate PDF/layout/device verification, Sol High.

## Logo/theme successor — 2026-10-02

[Autonomous appearance check](../REPORTS/PHASE_2B_CERTIFICATE_APPEARANCE_CHECK.md) retains an actual fresh-logo print race and its repair: wait for load/decode with bounded timeout, fail without printing, and cancel on close/unmount. Synthetic PNG upload/rejection/retry handling and three representative saved themes were exercised. Scoped recital footer/Eid title-warning readability defects were repaired and visually retested.58/58 controlled checks PASS (50 existing print checks rerun for this effect change +8 new image-helper checks); TypeScript exit0; lint0 errors/two inherited image warnings. Prior143/backend/SQL totals are historical, not added to this run's count. No accepted audit was repeated or prior evidence rewritten. Issue/Phase2B/release remain OPEN: PDF/fonts/real devices/remaining themes/full-portal and linked/critical gates are not inferred passed. Next certificate font readiness and linked/critical coverage, same Sol High allocation; no user print-dialog handoff.

## Font readiness successor — 2026-10-02

[Font readiness check](../REPORTS/PHASE_2B_CERTIFICATE_FONT_CHECK.md) reproduces premature printing with valid delayed Geist bytes and repairs ordering: mounted layout/font readiness after logo preparation, bounded ten-second wait, failure recovery and cancellation.66/66 controlled checks PASS (50 original print +8 retained image +8 new font-helper); product TypeScript exit0, lint0 errors/two inherited image warnings. Real-browser delayed font, combined font/logo and cancellation evidence plus loaded-font mobile390/desktop1280 geometry are retained. The first Windows-path fixture failure was excluded and corrected; a clean-reload combined capture supplements the hot-refresh callback that lacked an image field. FontFaceSet readiness can settle with stable fallback, not glyph/embedding proof. Original evidence and normal binaries retained; temporary QA services/tab stopped/reset. Issue/Phase2B/release remain OPEN for native PDF/pagination, physical devices/full Next/font pipeline, remaining themes/long content, status races, full-portal auth/SQL and linked/critical gates. No Azure/commit/deploy change. Next certificate linked/full-portal coverage readiness and remaining critical gaps, same Sol High allocation; no routine user test handoff.

## PDF successor — 2026-10-03

[Certificate PDF check](../REPORTS/PHASE_2B_CERTIFICATE_PDF_CHECK.md) reads a real Chrome PDF from the synthetic certificates page. The official PDF text matches the saved Issued row (`CERT-SYNTHETIC-001`, `VERIFY-SYNTHETIC-001`, Saved Learner, Saved programme, Issued) and does not contain the unsaved draft title. The draft PDF is watermarked and has no certificate number. Selecting the revoked synthetic record leaves Print / save PDF disabled. Font-adapted controlled checks were 66/66. The official PDF is three repeated letter pages, so pagination is not accepted. Linked auth/SQL portal and a physical phone were not run. Issue remains OPEN. No product change.

## Linked portal successor — 2026-10-03

[Linked certificate portal](../REPORTS/PHASE_2B_CERTIFICATE_PORTAL_CHECK.md) signs in through the real certificates page and issues one certificate against disposable SQL. The official PDF matches that saved number, verification code, learner, programme, title, note and date, and excludes the unsaved draft title. The draft PDF is watermarked. The official PDF still repeats on three pages. The CertificateIssued audit points at the same certificate and stores a null actor. Physical device NOT RUN. Issue remains OPEN.
