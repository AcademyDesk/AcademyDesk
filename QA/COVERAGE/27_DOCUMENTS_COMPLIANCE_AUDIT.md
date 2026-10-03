# Batch 27 — Certificates, compliance records, audit and exports

Certificate continuation: [Batch 40](40_CERTIFICATE_FORM_SEMANTICS.md) supplies exact nine-control field/payload/storage boundaries and conditional G01/G02 states for COMPLIANCE-FORM-001/002. It distinguishes immediate logo upload from Save, and identifies BUG-FUNC-0032: print operates on an unissued mutable draft. The summary below is historical; runtime remains NOT RUN.

Compliance continuation: [Batch 34](34_COMPLIANCE_SEMANTICS.md) supplies exact form/member boundaries, helper-instance behavior, state/N/A tables and seven effective action-permission contracts. The certificate/audit/export remainder of this historical batch is separate.

Permission continuation: [Batch 36](36_LATER_ACTION_PERMISSIONS.md) now supplies per-action G03 outcomes for all six Certificates actions, both AuditLogs actions and AcademyExports.Export. Preserve this batch's G01/G02 detail; runtime remains NOT RUN.

2026-09-29. Source specification; runtime NOT RUN. Completes CertificatesController (List/GetBranding/UpdateBranding/UploadLogo/Issue/UpdateStatus), CertificateVerificationController (Verify), ComplianceController (seven actions), AcademyExportsController (Export) and AuditLogsController (List/ExportCsv):17 actions.

COMPLIANCE-FORM-001 adds3 native branding controls and COMPLIANCE-FORM-002 adds6 native certificate controls. COMPLIANCE-FORM-003 adds3 native document controls and COMPLIANCE-FORM-004 adds2 native consent controls; four composed person/date/visibility selectors are specified below without inventing a native form count. The source inventory contains18 lexical controls across the two pages.

## G02 — Certificates and public verification

Branding accepts a six-digit hexadecimal accent or normalizes it to null; signatory is optional and truncated server-side. Logo upload checks reported ContentType and extension with a 2.5 MB size cap but writes beneath webroot. Existing BUG-SEC-0001 governs direct static-file access; test synthetic logo retrieval with anonymous/foreign/revoked sessions and do not access real assets. Test malformed colors, optional/long names, mismatched extension/content type, size limits, interrupted upload, persisted/reloaded branding and safe success/error feedback.

Certificate Issue validates title, fixed theme, student academy and batch academy membership, then generates certificate number and verification code. It records a generic audit event. BUG-DATA-0046: student and batch are not relationship-checked, so any local batch can be attached to any learner. Test no batch as a positive independent-programme control; when batch is supplied test enrolled/current/completed/not-enrolled/inactive/foreign combinations, title/notes limits, issue date boundaries, duplicate/collision behaviour and status transitions. UI loads all batches and does not filter them by selected student.

The issue page sets a success message then calls load, which clears it; this extends BUG-FUNC-0003. It does not expose a revoke/replacement action despite the API. Print/save PDF is browser print of the preview, not a server-generated certificate artifact. Test print layout, long names/themes, date locale/year control, logo fallback, mobile/desktop table overflow, accessible color/file controls and fresh register/readback. Certificate verification is anonymous by design and returns academy, certificate number, title, issue date and status for a code. Test exact code normalization/missing/malformed, issued/revoked/replaced status policy, enumeration/rate-limit expectations and ensure no learner contact data is disclosed.

## G02 — Compliance documents and consent evidence

Documents list tenant rows ordered expiry/type. AddDocument requires document type and filename, preserves optional secure reference, expiry and visibility, but uploads no file. Review accepts four statuses and caller-supplied reviewed date; review-task creates a work item every request with no deduplication. Test empty/space/length/Unicode fields, expiry boundaries, repeated task creation, status/date transitions, past expiry and audit requirements. Do not infer that a filename or secure reference proves file storage, encryption or download authorization.

BUG-DATA-0045: document and consent creation accept unchecked StudentId/GuardianId values; documents permit neither identity and consents permit both simultaneously. API only tests non-null consent identity. Test local/foreign/missing/both/null subjects with fresh database assertions; consent grants/withdrawals need a defined immutable evidence/history rule. Existing communication preferences are distinct and must not be treated as legal-consent proof.

Compliance page has separate document/consent forms, each with an independent PersonPicker instance and its own person-type/person-ID state (page.tsx:73–81). This corrects the earlier shared-state claim. Test independent selections, switching person type and resetting only that instance's selected ID, stale IDs, empty person lists, optional expiry/reference/evidence/visibility fields, duplicate submit, failed request and form reset only after confirmed write. Current page success/readback behavior extends BUG-FUNC-0003. Review task, document review and consent withdraw lack native form declarations but require their own success/error/retry and authorization cases.

## G02 — Activity records and exports

Audit list/export filters action/entity/from/to, caps output at 200, uses inclusive from and exclusive to boundaries. Activity UI loads an unfiltered first 200 and filters only in browser; it does not request the server filters or export. Test date/timezone/day edges, exact action/entity matching, null/tie ordering, 200+ history, stale data and metadata rendering. Metadata is placed inside a pre tag; test escaping/long content and authorization before exposing IP/actor metadata.

AcademyExports supports only students, guardians, enrollments, invoices and payments. It quotes CSV cells, but existing BUG-SEC-0002 covers spreadsheet-formula neutralization. Test unknown resource, empty scoped set, Unicode/quotes/commas/newlines/formula prefixes, content disposition, partial/failed blob download and least-privilege reports.export behavior. Reports page produces separate client-side CSVs whose columns and lookup failures can differ from server exports; compare authoritative requirements before treating either as a financial record.

## G03 — Access, integrity and visual acceptance

Certificates, Compliance, AuditLogs and CertificateVerification have no direct PermissionCatalog mapping; exports map reports.export. Global access filtering is not evidence of anonymous access or of intended delegated role behavior. Test anonymous, wrong academy, inactive/suspended account, expired/revoked grant, owner/admin/teacher/family and no-write/no-file effects. Public verification is the deliberate anonymous exception and needs its own disclosure/rate-limit policy.

At 320/375/390/430, 768 and 1280/1440 plus 200% zoom, test table overflow, print preview, standard-select/dropdown layering, color picker, file focus, long certificate/compliance/audit data, activity details expansion, screen reader names, keyboard focus and durable save status. No browser, real HTTP/SQL, exported file or visual test was executed.

## Outcome

Two new OPEN P1 source findings: BUG-DATA-0045 (unchecked/contradictory compliance person references) and BUG-DATA-0046 (certificate student/batch mismatch). Existing BUG-FUNC-0003, BUG-SEC-0001 and BUG-SEC-0002 extend. Adds5 controllers/17 actions,4 forms/18 lexical controls. Cumulative78/82 forms,456 lexical+122 standalone/composed=578/649 controls,64/70 complete controllers. Source counters are not runtime pass or percentage completion.

Next: finance operations, teacher compensation, dashboard/intelligence and final residual shared controls. G01/G02/G03 remain open; Phase1 NOT CLOSED and Phase2 not started. No app repairs, commit or Azure deployment.
