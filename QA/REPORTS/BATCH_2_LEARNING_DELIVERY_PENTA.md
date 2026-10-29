# Batch2 — assessments, assignments and resources

2026-10-15. Worktree `D:\AcademyDesk-codex-p0`, branch `codex/penta-search`, HEAD `b1f8cb6`. Bounded Manual E3 presentation continuation,not full learning-domain/portal acceptance.

## Implemented

- Three pages consume an isolated PENTA learning stylesheet:responsive panels/light-dark semantic tokens,readable controls and44px actions. Assessment result editors stack on phones rather than crowding scores/grades/remarks into one narrow row.
- Assessment batch/type/optional scheme/selected assessment and grading-mode selectors have permanent labels. Score/automatic-or-manual grade/optional remarks have visible labels after entry,with existing ARIA names retained. Remarks disable with the other controls during pending save. Existing automatic/manual grade interpretation,score limits and ResultRow state/handlers remain unchanged.
- Assignment Batch and Type replace native selects with the existing anchored shared control. Required batch validation retained;title/instructions/type/optional due time/publication checkbox have visible labels. Due time is explicitly local,optional;existing browser-local UTC conversion and blank/null behavior unchanged. No silent timezone/history rewrite.
- Resource upload uses a native associated label. Type/optional subject/optional audience shared selectors no longer nest interactive controls inside native labels. File-versus-link behavior,accepted file types,multipart/link request paths and optional null scope remain unchanged.
- Assessment empty/unavailable selection guidance added. The existing save/load/create/result/domain helpers remain unchanged by10pre-edit function hashes;ResultRow presentation changed deliberately and is covered by existing grade tests and actual browser result saves.

## Verification

- Final webpack build/TypeScript/static export86 PASS. Scoped three-page ESLint0errors/1Resources dependency warning,reproduced from HEAD;no suppression or unrelated effect rewrite. Whole-app lint gate remains OPEN. No app-source edits after final build.
- 71 focused node:test entries PASS:10 new function-preservation entries plus existing assessment grade/options/create-feedback and assignment-feedback cases. Import mocks updated for the new CSS;one secondary grade-test compiler needed the same mock correction. Existing exact-payload/nullable fields/local-UTC/automatic-manual grade/pending/readback assertions retained.
- Final synthetic browser results/receipts below after completion:54 new3page×3width×2theme×normal-empty-denied cases;32 existing assessment-create success/rejected/uncertain/readback-failure cases and16 assignment counterparts. New normal paths perform real DOM manual-grade result save,assignment creation andresource link publish/readback,with exact POST bodies/optional null fields. Resource file selection clears/disables Web link and clearing re-enables it;no actual upload/storage/preview proof claimed. Shared selectors collapse,44px actions,no page overflow/errors checked.
- Final102/102 synthetic browser cases PASS. Receipts:`QA/EVIDENCE/learning-visual-1792047575732/result.json` (54),`QA/EVIDENCE/assessment-create-feedback-browser-1792047586231/result.json` (32),`QA/EVIDENCE/assignments-feedback-browser-1792047596867/result.json` (16). New matrix source hashes match final files;final phone screenshots reviewed,including all entered result labels. Script parsing/scoped whitespace/vendor6 PASS. No app-source edit after final build. Synthetic transport only,not live SQL/auth/device/release acceptance.

## Corrections and limits

The first new resource fixture lacked the existing Certificates entitlement;host preview correctly disabled input. Fixture now includes that module alongside AcademicGovernance;no permission/entitlement change or force clicks. Final screenshots prompted persistent result-field labels,then a fresh build and final reruns. Intermediate receipts remain local.

This presentation checkpoint preserves existing domain behavior,not new readiness/context/uncertainty acceptance. Resources still erase their success notice in the existing loader and lack independent pending/unknown-outcome recovery;these are the next bounded High functional follow-up,not considered fixed by new styling. Existing assignment/assessment transport/context/draft concurrency and local-time policy limits remain in the enterprise queue. Denied resource/assignment reads may still render inherited empty/register/forms;this suite confirms visual feedback,not secure frontend readiness. Backend authorization remains authoritative and unchanged. Do not close existing issues from these synthetic tests.

Next High:resource publish/upload pending,truthful outcome and read recovery safeguards;then Medium communications/remaining academic/portal-state presentation. Existing realSQL/API/RBAC/tenant/physical-device/PENTA UI2/Mini/domain conversation/E9–E10 release gates remain OPEN. Owner sequence unchanged:complete agreed PENTA+Manual app → authorized go-live → Hayansh onboarding → Academy Desk product website after first customer live.

No backend/schema/Mini/vendor/main/staging/commit/push/Azure/production data/container changes. Prior dirty application work and local evidence preserved. No new project switch/engine contract handoff required.
