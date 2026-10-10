# Teacher availability — native runtime reproduction and bounded repair

2026-10-10; `D:\AcademyDesk-codex-p0`, `codex/penta-search`, starting HEAD `a8e9f07ca64899e67c00634bf3580e419f70e454`. Continuation of BUG-API-0006 / TEACHER-AVAILABILITY-001, not a new enterprise plan.

## Reproduced defect and repair

Original controller matrix:22 PASS/4 FAIL, null-only and mixed-list GET/PUT projections throw after reading or saving. Original native run `6f0e08456fce46ad821fe45a07faf270`:15 cases PASS, then stored `[null]` GET returned500 instead of200. This establishes the local defect, not the cause of historical Azure messages.

One source guard in `ProfilesController.ReadAvailability` skips null slots before reading Day. Valid slots in mixed lists survive. Existing blank/null metadata and malformed JSON/object/scalar fallback remain empty availability; raw storage/trim behavior is unchanged. This is safe projection, **not new availability validation or scheduling policy**. Existing teacher intake can still store optional JSON; its later profile reads use the repaired projection. No migration, role/tenant/approval/tool/model/frontend changes.

## Verification and exact bounds

- 26 new controller/EF InMemory cases PASS:13 shapes for stored read and administrative save, including optional null/empty/whitespace, JSON null, empty/null/mixed lists, case-insensitive valid slots, empty object entry, object/scalar roots, invalid syntax/day type. Fresh tracker checks preserve storage and optional null dates. InMemory is not SQL proof.
- 6 retained PeopleBranchValidationTests PASS; total32 targeted tests. Full API suite and unchanged frontend build/browser suites were not rerun for this one-line API projection change.
- New additive `TeacherAvailability` module in the existing owned SQL harness/runner; no existing assertion or module removed/weakened. Harness build0 warnings/0 errors.
- Fixed native run `9df9a40937fa4e4891b366c49ea00456`:45/45 SQL/Identity/HTTP cases PASS.13 shapes × stored GET + profile PUT/fresh SQL + fresh GET =39;6 denied/missing controls (anonymous, Teacher write, foreign actor/route/record, missing record) assert whole teacher-table no-write. Foreign sentinel preserved. Existing base health/Identity/two-tenant controls PASS. No HTTP429; deliberate650ms pacing retained.
- 88 application/7 Identity migrations, exact runtime login/DbContext target verification and negative cleanup ownership control PASS. Success run removed its database/login then exact container. Failure run container removed separately only after exact name/run-label/loopback-binding check. Mini containers were neither removed nor pruned.
- Source SHA256 for repaired controller: `0F772BD066EAE6A2D6434AAE3A32E90DDF22EB581A7073C0E12BB90CC6DC8DF9`. SQL ran the preserved dirty snapshot; unrelated Mini implementation is not thereby accepted or included in this commit.

Local receipt: `QA/EVIDENCE/teacher-availability-9df9a40937fa4e4891b366c49ea00456/result.json` includes baseline/final counts and filtered final console. SQL was initially unavailable; local per-user Docker Desktop was started and engine became available. No system policy, production credentials/database or Azure changed.

The historical `node QA/tools/validate.cjs` consistency validator could not run because its required local `QA/EVIDENCE/logs/observed-checks.json` is absent in this worktree (ENOENT). NOT PASS; no evidence invented/copied and no validation assertion removed. This does not invalidate the captured targeted unit/SQL results, but broad QA consistency acceptance is not claimed.

## Open gates and next route

BUG-API-0006 remains OPEN for teacher onboarding/profile browser feedback, physical Android/iOS, full critical regression and exact-release acceptance. This repair does not add a failed-input400 policy, prove arbitrary availability scheduling correctness or close unrelated privacy/financial/AI gates. Earlier accepted feedback/SQL/security evidence remains retained, not re-audited.

Next bounded original item: BUG-API-0001 optional onboarding-date payload case, inspect current source/latest evidence and reproduce only remaining case. Sol Medium for settled form serialization; Sol High for Identity/SQL/domain/privacy/release decisions. Shared FW1, Mini qualification, conversational actions and enterprise E1–E10 gates continue independently. No Mini handoff/project switch needed for this host projection guard; no main edits/merge/push or Azure deployment. Scoped feature publication only, preserving unrelated32/28 continuity and local QA evidence.
