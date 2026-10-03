# BUG-DATA-0046 — Certificate issuance permits a student and unrelated local batch

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | RUNTIME-CONFIRMED / retained controller reproduction;approved repair verified in real HTTP-SQL |
| Final verification | PARTIAL PASS / OPEN;62 certificate HTTP-SQL PASS,controlled UI retained;browser/linked/critical/races/faults/legacy pending |
| Severity | Major credential attribution integrity |
| Priority | P1 |
| Category | DATA |
| Module | COMPLIANCE |
| Role | See reproduction; same-tenant Admin unless stated otherwise |
| Screen / route | /certificates |
| API | POST /api/academies/{academyId}/certificates |
| Environment | Source review of local working tree; runtime production state not inferred |
| Device/viewport | NOT RUN; use QA/06_DEVICE_VIEWPORT_MATRIX.md where UI applies |
| Baseline | 20bb6047f9edf733ac8e2a226621cc582ec54b3c + pre-existing student UI diff |
| Discovery test / review ID | CERTIFICATE-ENROLLMENT-001 |
| Evidence classification | Original static trace plus retained controller persistence reproduction and approved-policy regression |
| Preconditions | Isolated synthetic fixture from QA/10_TEST_DATA_STRATEGY.md; never customer data |
| Reproduction frequency | Not measured; reproduction instructions are proposed |
| Source | apps/api/Controllers/CertificatesController.cs:79 |
| Class/function | Issue |
| Related source | INVENTORY/API_ENDPOINTS.md, CONTRACTS.md, FORMS_AND_FIELDS.md and ASYNC_FORM_RISKS.md |
| Root-cause confidence | HIGH |
| Evidence location | Source excerpt below; source fingerprint in INVENTORY/SOURCE_MANIFEST.md |
| Screenshot | Not captured on pinned baseline |
| Console logs | Not captured; required in retest |
| API request | Create two local batches and enroll a learner only in the first. Issue a certificate for that learner with the second batchId, then inspect the stored certificate and rendered preview/portal output. |
| API response | Not captured for this issue; use synthetic request/response in isolated reproduction |
| Database before/after | Not executed; fixture and fresh-context assertions defined below |
| Dependencies | Safe SQL/HTTP/browser harness as applicable; desired policy review where noted |
| Fix commit | Local uncommitted controller guard; no push/deployment |
| Retest result | Prior31 controller/InMemory checks retained;48 new controlled UI PASS; current HTTP-SQL/browser pending |
| Regression result | 1019/1019 backend PASS, including988 existing cases; not release acceptance |
| Closure notes | Remain OPEN; follow closure requirements in QA README |

## Exact reproduction

Create two local batches and enroll a learner only in the first. Issue a certificate for that learner with the second batchId, then inspect the stored certificate and rendered preview/portal output.

## Expected

A supplied batch is an active or historically eligible enrollment for the selected learner, or the API/UI makes independent-programme certificates explicit without implying class membership.

## Actual / evidence

Issue independently verifies student and batch academy membership but never checks enrollment or batch association before storing both IDs. The UI exposes every batch for every selected student. Runtime NOT RUN.

Source snapshot:

```text
78:         if (!await dbContext.Students.AnyAsync(x => x.Id == request.StudentId && x.AcademyId == academyId, token)) return BadRequest(new { message = "The student does not belong to this academy." });
79:         if (request.BatchId.HasValue && !await dbContext.Batches.AnyAsync(x => x.Id == request.BatchId && x.AcademyId == academyId, token)) return BadRequest(new { message = "The class or batch does not belong to this academy." });
80:
81:         var certificate = new Certificate
```

## Suspected root cause

Related entities are existence-checked independently rather than relationship-checked for credential context.

## Business impact and blast radius

Certificate register, verification and printed credential claims for learners assigned to the wrong class.

## Related / required regression

CERTIFICATE-ENROLLMENT-001: HTTP/SQL student-batch enrolled/current/completed/not-enrolled/foreign/deactivated matrix, omitted batch control, direct API bypass and generated certificate/readback assertions.

Also run all endpoint/form cases pointing to this issue in QA/03_TEST_MATRIX.md and the critical regression suite before closure.

## Phase 2B successor — 2026-10-02

The original source excerpt/NOT RUN statements above describe the Phase1 discovery baseline, not the latest bounded run. See [certificate enrollment repair](../REPORTS/PHASE_2B_CERTIFICATE_ENROLLMENT_REPAIR.md).

User explicitly approved Active or Completed enrollments only, including Completed history in an inactive batch; no-batch certificates remain permitted. No extra date/active-person policy was added. The controller now requires an exact same-academy/student/batch qualifying enrollment before saving a linked certificate. Case-insensitive statuses match the existing enrollment editor contract. Waitlisted/Paused/Withdrawn/Cancelled/Transferred/unknown/empty do not qualify.

Baseline3 unrelated-association cases each returned201 and persisted the wrong link plus CertificateIssued domain audit. Approved-policy baseline31 tests:20 PASS,10 product failures,1 QA-only null-status fixture failure (required Status prevented fixture persistence, not an issuance bypass). The QA assertion was corrected to verify the existing required model; first post-repair run1018 PASS/1 fixture failure retained, then1019/1019 PASS (31 certificate/988 existing). No success/audit writes on rejected cases and exact optional/default/summary/domain-audit/source preservation checked in fresh InMemory contexts, not SQL.

OPEN: matching picker filtering/stale choice/error recovery, current Identity/HTTP/SQL translation/access/serialization/readback/actor audits, physical/browser/mobile, all-linked/critical acceptance, legacy records, enrollment races/audit-fault atomicity and separate BUG-FUNC-0032 preview-before-issuance. No schema/auth/dev DB/services/Azure/commit/deploy changes.

## Phase 2B frontend successor — 2026-10-02

The earlier UI-pending checkpoint above is superseded in controlled-source scope by [certificate eligibility UI repair](../REPORTS/PHASE_2B_CERTIFICATE_UI_REPAIR.md). Only the certificate page changes:scoped enrollment-based options,learner-change/stale refresh reset,optional no-class,durable issue notices,exact API rejection feedback,draft retention and explicit refresh recovery/duplicate guards.4 original TSX failures reproduced;48 certificate checks +45 reused compliance PASS,TypeScript PASS,target lint0 errors/2 existing image warnings. No browser/real HTTP/SQL/mobile/DOM proof. Prior1019 backend results and all API/schema/shared controls/CSS/normal assemblies retained,not rerun. Current HTTP-SQL/browser/all-linked/critical/races/faults/legacy/preview-print/release gates remain OPEN;no dev services/Azure/commit/deploy.

## Phase 2B SQL preparation successor — 2026-10-02

[Certificate SQL preparation](../REPORTS/PHASE_2B_CERTIFICATE_SQL_PREPARATION.md):QA-only guarded62-case harness prepared and isolated build PASS,0 warnings/0 errors. Execution blocked before new container creation by Docker Linux engine unavailable;current certificate HTTP/SQL NOT RUN,zero new runtime passes/SQL writes. Prior product/UI tests and source remain unchanged,not rerun. Planned17 issuances/45 no-write cases are not achieved counts. Open Docker Desktop,wait for Engine running,and resume this same slice with Sol High. No dev DB/services/Azure/commit/deploy;issue/browser/linked/critical/races/faults/legacy/preview-print/release remain OPEN.

## Phase 2B real HTTP/SQL successor — 2026-10-02

[Certificate real HTTP/SQL](../REPORTS/PHASE_2B_CERTIFICATE_SQL_CHECK.md):62/62 PASS,17 exact persisted issuances/45 no-write reads-denials,full fresh SQL summary/field/source/Identity/prior/foreign preservation and domain+actor audit checks. Active/Completed case variants,exact academy/student/batch matching,optional independent certificates,inactive/history behavior,selected JSON/input/access/module/academy guards and scoped register/enrollment inputs passed. Earlier Docker blocker is superseded. One synthetic batch-name SQL collation fixture failure occurred before certificate requests,retained/excluded and corrected only in QA. Final DB/login removal verified,both owned containers/roots/ports cleaned. No product/UI/schema/security/shared-control changes this slice;prior1019 backend/48 certificate+45 compliance controlled results preserved,not rerun. Browser/all-linked/critical/races/faults/legacy/preview-print/release remain OPEN. Next separate BUG-FUNC-0032 certificate print/preview integrity,Sol High. No dev DB/services/Azure/commit/deploy.
