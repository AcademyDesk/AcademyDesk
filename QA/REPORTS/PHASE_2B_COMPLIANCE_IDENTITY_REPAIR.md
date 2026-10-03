# Phase 2B — compliance person identity: controller repair

2026-10-01. BUG-DATA-0045 / COMPLIANCE-IDENTITY-001. Bounded server repair following the accepted Astra finding, not a broad re-audit. Continue Sol High. Issue, Phase2B and release remain OPEN.

## Product contract

Only ComplianceController changed in application code. Shared ValidatePerson runs before document/consent creation: exactly one StudentId or GuardianId, and that ID must exist in the selected typed table in the route academy. Reject missing/foreign/empty IDs, swapped student/guardian IDs, both IDs (including an extra empty Guid), and documents with neither subject. Existing consent no-subject and required-type/file-name guard responses remain unchanged. New person guard errors are400 with a message.

The accepted single-subject contract follows the page's Student-or-Parent selector and the issue's expected relationship. It does not invent a dual-subject signed-by/for relationship. Existing inactive-person eligibility is preserved; no active-only or guardian-child enrollment rule is added. Null/empty secure/evidence references, optional expiry date and visibility defaults remain; trimming, intentional historic dates, complete HTTP200 row responses, server IDs/academy/timestamps, PendingReview/default review metadata, consent Granted/withdrawn initialization unchanged.

ReviewDocument/CreateReviewTask/List/Withdraw unchanged; no legacy data rewrite, FK/migration, authorization/module policy, upload/storage/provider or frontend edits. Repeated same-person records still append rather than overwrite evidence. Existing repeated withdrawal refreshes WithdrawnAtUtc; preserving that behavior in a test is not an approved first-withdraw/idempotency policy.

## Executed evidence

- [Baseline](../EVIDENCE/logs/phase-2b-compliance-identity-backend-baseline.log)/[TRX](../EVIDENCE/compliance-identity/compliance-identity-baseline.trx):76 direct-controller/InMemory cases,51 PASS/25 expected FAIL. The13 person-reference modes × document/consent cover neither/both-local/both-foreign, missing/foreign/empty/swapped student or guardian and valid-plus-extra-empty opposite ID. Consent with neither ID was already correctly rejected; other25 cases return OkObjectResult instead of400. No real HTTP/SQL or browser baseline is claimed.
- [Full backend](../EVIDENCE/logs/phase-2b-compliance-identity-suite.log)/[TRX](../EVIDENCE/compliance-identity/compliance-identity-suite.trx):988/988 PASS,76 new; prior912 run as regression, not new cases.
- 26 rejection cases compare complete documents, consents, students, guardians and work items before/after, with no Added entities.36 valid creations cover student/guardian × active/inactive × null/empty/filled optional references; documents preserve optional date/visibility defaults, consents cover Granted true/false and withdrawal initialization. Full returned/stored fields, IDs/academy/UTC clock windows/null update metadata and unchanged prior/foreign evidence/source rows checked.
- Five original text guards, one review sequence across all four canonical statuses with explicit/default review dates + invalid/missing/foreign controls, three review-task expiry/priority/default date controls, two withdrawal-history cases, two repeated-creation cases, one full scoped List check. Those9 lifecycle/list cases together with5 text cases are part of76, not separate additions. One review sequence is one xUnit case with multiple assertions.
- Isolated .build-check/compliance-identity-baseline and .build-check/compliance-identity assemblies only. No normal dev DB/service/restart/migration, SQL host provision, Azure/customer/provider calls, commit/push/deploy. InMemory durability/guard checks are not real SQL, audit/role enforcement, HTTP model binding or frontend proof.

[Snapshot](PHASE_2B_COMPLIANCE_IDENTITY_SOURCE_SNAPSHOT.json)/[validator](../tools/validate-compliance-identity.cjs) preserve predecessor source/evidence/binaries except declared controller/checkpoint/issue successors. Existing resource73 HTTP/SQL evidence retained, not rerun. Baseline before-source hashes and assemblies retained. Historical validators are not rewritten to hide intended successor hashes.

## Pending work and next slice

Next: fix the compliance page's typed Student/Parent options and subject lookup, retaining independent selections for the two forms; then perform real Identity/HTTP/disposable SQL compliance checks. Keep Sol High. The current frontend remains unchanged and still offers the known mixed list; new API guards reject its wrongly typed selections, so browser UX is not resolved yet.

OPEN: typed picker/subject display, controlled-form submit/reset/success/error recovery, browser/mobile/physical devices, real HTTP model-binding/auth/module/SQL/actor audits, all-linked/critical/release acceptance, same-ID collisions across typed tables, existing bad records, concurrent person moves/deactivation/deletion, audit rollback, field-length/visibility/date policy, duplicate/history/repeated-withdraw policy and legally meaningful guardian-for-child/dual-party consent definitions. No issue closure or deployment.
