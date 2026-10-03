# Phase 2B — learning-resource audience scope: controller repair

2026-10-01. BUG-DATA-0044 / LEARNING-RESOURCE-SCOPE-001. Bounded controller repair following the accepted Astra finding; no repeat broad audit. Continue Sol High. Issue, Phase2B and release remain OPEN.

## Contract and implementation

Only application change: shared ValidateScope in LearningResourcesController. Both link Create and legacy administrator Upload already call this guard before creating a row or upload directory/file. Load the exact same-academy batch's CourseId and, when both optional scope IDs are present, require the selected subject ID to equal that CourseId. A mismatch returns400 with "The selected subject does not belong to the selected batch." Missing/foreign/empty batch and subject IDs retain their existing invalid-batch/invalid-subject responses.

Matching pairs, batch-only, subject-only and academy-wide requests remain supported. Existing inactive batch/subject behavior is preserved, not redefined as a new active-only policy. No inferred subject is persisted for batch-only requests. No new enrollment, visibility, publication, upload-format or size policy. Create/Upload retain intentional empty HTTP200 responses; trimming/defaults, optional fields, filenames, List projection and Publish behavior are unchanged. Existing stored contradictory scopes are not rewritten. Frontend, family query, permission/module filters, private storage/provider, schema and migrations are untouched.

## Executed evidence

- [Baseline log](../EVIDENCE/logs/phase-2b-resource-scope-backend-baseline.log)/[TRX](../EVIDENCE/resource-scope/resource-scope-baseline.trx):37 direct-controller cases,33 PASS and4 expected FAIL. Link and upload both accept mismatched local subject/batch pairs, with active and inactive fixtures. Actual result200 instead of400 confirms the guard defect; this is EF InMemory/controller evidence, not a real HTTP/SQL audience proof.
- [Full backend log](../EVIDENCE/logs/phase-2b-resource-scope-suite.log)/[TRX](../EVIDENCE/resource-scope/resource-scope-suite.trx):912/912 PASS,37 new. Thirty cases cover15 audience variants through both endpoints: matching/mismatching pair, batch-only, subject-only, academy-wide; missing/foreign/empty IDs for each dimension; inactive matching/batch-only/subject-only/mismatching controls. Rejections compare complete resources/courses/batches before/after and assert no Added resource and no upload file. Accepted creates compare scope, defaults/nulls/publication/server identity/time; preserve source and unrelated/foreign rows; upload bytes match exact synthetic content.
- Seven additional checks retain missing/empty/disallowed upload guards, blank link title/invalid URL guards, selected scoped Publish/List projection and no-write controls, filename-title fallback and trimmed optional upload text.
- Tests write only tiny synthetic files under per-test GUID temp roots; each exact root is validated and cleaned on disposal. The fixture PDF/audio bytes are arbitrary synthetic data for storage checks, not playable-media evidence.
- Isolated .build-check/resource-scope-baseline and .build-check/resource-scope assemblies only. Normal dev assemblies/services/database untouched. No SQL harness invocation, schema migration, real Identity request, live browser/physical device, Azure/provider request, commit/push/deploy this slice. Prior875 tests run as regression in the912 suite; those875 are not new test cases.

[Source snapshot](PHASE_2B_RESOURCE_SCOPE_SOURCE_SNAPSHOT.json)/[validator](../tools/validate-resource-scope.cjs) preserve all accepted predecessor sources/evidence/binaries except declared successor controller/checkpoint/issue updates. Before-source hashes and separate baseline assemblies retained. Historical validators are not rewritten to mask intentional successor hashes.

## Pending verification and limits

Next bounded task: real Identity/global-filter HTTP + disposable SQL matrix for link and multipart upload, exact fresh SQL/file/audit no-write on denial, accepted body/row/file agreement, scoped List/Publish and family audience visibility for synthetic enrolled learners. Stay Sol High; do not move to the next issue or claim this real-SQL step passed from InMemory evidence.

Still OPEN: actual model binding/auth/module/tenant enforcement, family audience readback, browser/device success/error flow and selector behavior, all-linked/critical acceptance, legacy inconsistent records, concurrent batch-subject reassignment between lookup/save, audit/upload-file rollback failures and explicit inactive/archive scope policy. Schema-source hashes being unchanged is not a new migration execution pass. Broader upload/private Blob/media portability/size/preview gates remain separate and are not resolved by this relationship check.
