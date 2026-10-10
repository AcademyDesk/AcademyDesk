# Academy Desk — delivery progress checklist

Updated:2026-10-10. Worktree `D:\AcademyDesk-codex-p0`, branch `codex/penta-search`. Application goal: **90% eligible operations through conversational PENTA AI;10% manual control/fallback**. This is a target, not measured current coverage.

## How to read and maintain this checklist

- `[x]` means the **named bounded deliverable** is completed at its recorded evidence level. It does not mean the entire feature, issue or release is accepted.
- `[ ]` means pending/in progress. Mark policy approval, implementation, synthetic UI proof, real SQL/linked inference, physical devices and production approval separately.
- Owner request: every future batch updates this file and displays **past completed / current batch / next tasks** in the response. Preserve history and link the new report; do not repeat accepted work or invent a whole-app completion percentage.
- Existing [execution map](ACADEMY_EXECUTION_PATH.md), [E0 queue](QA/REPORTS/EXECUTION_E0_RECONCILIATION.md), [issue register](QA/ISSUES/INDEX.md), [QA checkpoint history](QA/00_QA_README.md) and [handoff](AI_HANDOFF.md) remain authoritative detail. This checklist is their readable delivery view, not a replacement testing plan.

## Past completed checkpoints

- [x] Repository/GitHub baseline and governance established; historical build/API/typecheck/static checks retained in [enterprise roadmap](ENTERPRISE_TESTING_ROADMAP.md). Not final runtime/enterprise acceptance.
- [x] Existing issue/evidence reconciliation and dependency-ordered E0–E10 execution map prepared. No restart of accepted Astra audits.
- [x] Collection transaction/balance protection and existing race repairs — [BUG-DATA-0001](QA/ISSUES/BUG-DATA-0001.md), broader browser/device/release gates OPEN.
- [x] Adjusted-balance and concurrent approval/payment guards — [report](QA/REPORTS/PHASE_2B_APPROVAL_PAYMENT_GUARD.md); refund/credit-note policy OPEN.
- [x] Negative-net payroll guard — [report](QA/REPORTS/PHASE_2B_PAYROLL_NET_REPAIR.md); zero-net semantics and broader acceptance OPEN.
- [x] Payment void/reconciliation consistency and concurrency guards — [report](QA/REPORTS/PHASE_2B_PAYMENT_RECONCILE_VOID_GUARD.md); restoration policy OPEN.
- [x] Teacher lifecycle guard repaired:62 native SQL/HTTP +29 retained private-media checks +77 unit tests — [report](QA/REPORTS/PHASE_2B_TEACHER_LIFECYCLE_REPAIR.md).
- [x] Guardian explicit revocation and SQL child-list repaired:90 native SQL/HTTP +55 retained guardian checks +24 unit tests — [report](QA/REPORTS/PHASE_2B_GUARDIAN_REVOCATION_REPAIR.md).
- [x] Financial policy bundle specified: FP1/FP2/FP3 and18 planned requirements — [proposal](QA/REPORTS/PHASE_2B_FINANCIAL_POLICY_BUNDLE.md). **Not approved or implemented.**
- [x] Earlier28-route feedback queue reconciled;21 routes checkpointed before this batch. Full prior route evidence remains in [BUG-FUNC-0003](QA/ISSUES/BUG-FUNC-0003.md).
- [x] Existing bounded PENTA read/host-security and conversation foundations checkpointed in [handoff](AI_HANDOFF.md); not broad CRUD/action coverage, Mini qualification or90% AI readiness.

## Past batch — Trial Bookings + Curriculum feedback

Scope: success/refresh/error/draft/pending feedback only. Existing payloads/domain/RBAC/tenant rules preserved; no API, database, finance policy or Mini changes.

- [x] Trial Bookings: durable create/status success; optional teacher/notes nulls and local-to-UTC timestamp retained; failed/unrelated drafts retained.
- [x] Curriculum: durable draft/publication success; original course/title/description/sequence conversion retained; failed/unrelated drafts retained. Blank sequence0 remains a separate domain gate.
- [x] Both: confirmed-save/failed-refresh warning distinct from unconfirmed network/5xx; no automatic retry; same-tick duplicate/opposing write guard; checked initial workspace and polite status region.
- [x] Controlled handler verification:52/52 PASS; frozen starting HEAD under the same assertions3 PASS/49 FAIL. Counts are assertions, not49 distinct defects.
- [x] Target lint:0 errors/0 warnings; installed TypeScript validation PASS.
- [x] Production webpack build/export:83/83 pages PASS.
- [x] Desktop/mobile browser matrix:320/1440px × light/dark × both actions × four response scenarios × two pages,64/64 synthetic cases PASS; two mobile screenshots visually reviewed.
- [x] [Final batch report](QA/REPORTS/PHASE_2B_TRIAL_CURRICULUM_FEEDBACK_REPAIR.md), route-count reconciliation and completed-batch handoff.
- [ ] Real SQL/domain/role/tenant acceptance and physical Android/iOS remain outside this feedback-only batch.

## Past batch — Academic Governance + Academic Periods feedback

- [x] Durable scheme/prerequisite/status and year/term/create/close feedback; captured native forms reset only after confirmed writes.
- [x] Draft retention, confirmed-write/failed-readback warning, uncertain result/no automatic retry and synchronous shared write guard.
- [x] Checked uncached initial lists before enabling forms; readback stays on the captured academy; existing prerequisite/year-closure authority unchanged.
- [x] Actual-handler checks:72/72 PASS; frozen starting commit7 PASS/65 FAIL. Counts are assertions, not65 distinct defects.
- [x] Target lint0/0, TypeScript and production83-page export PASS.
- [x] Responsive light/dark browser matrix:112/112 synthetic cases PASS; three mobile screenshots inspected, horizontal register scrolling retained.
- [x] [Final report](QA/REPORTS/PHASE_2B_GOVERNANCE_PERIODS_FEEDBACK_REPAIR.md), route reconciliation and handoff prepared for scoped feature publication.
- [ ] Real SQL/domain/role/tenant and physical Android/iOS acceptance remain separate.

## Past batch — Enrollments + Batch Promotions feedback

- [x] Durable create/status/decision notices; confirmed-write/readback warning and uncertain-result draft retention without automatic retry.
- [x] Shared synchronous pending guard, disabled forms/row actions, checked initial lists and same-academy uncached readback.
- [x] Exact payloads/optional nulls/draft resets retained; promotion prompt cancellation now performs no decision, rejection reason remains required.
- [x]59 actual-handler cases PASS; frozen starting commit5 PASS/54 FAIL. Target lint0/0, TypeScript and83-page production export PASS.
- [x] Responsive synthetic browser matrix80/80 PASS; two mobile screenshots inspected.
- [x] [Report](QA/REPORTS/PHASE_2B_ENROLLMENT_PROMOTION_FEEDBACK_REPAIR.md) and continuity prepared for scoped feature publication.
- [ ] Enrollment lifecycle-reason input/integration — [BUG-FUNC-0016](QA/ISSUES/BUG-FUNC-0016.md) remains OPEN; synthetic update success is not live domain acceptance.
- [ ] Real SQL/role/tenant and physical Android/iOS/full-workflow acceptance remain separate.

## Past batch — Assessments creation feedback

- [x] Durable creation notice, confirmed-write/readback warning and uncertain-response draft retention; no automatic retry.
- [x] Shared synchronous create/result pending guard; checked uncached initial workspace; exact create payload and matching-only reset preserved.
- [x] Existing grading/options/roster/result-save safeguards retained:29 prior checks plus17 new checks,46/46 PASS; frozen creation baseline3 PASS/14 FAIL.
- [x] Target lint0/0, TypeScript and83-page production export PASS.
- [x] Responsive synthetic browser matrix32/32 PASS; light/dark mobile screenshots inspected.
- [x] [Report](QA/REPORTS/PHASE_2B_ASSESSMENT_CREATE_FEEDBACK_REPAIR.md), route reconciliation and scoped handoff prepared.
- [ ] Real SQL/role/tenant, physical Android/iOS and full-workflow acceptance remain separate; BUG-FUNC-0003 OPEN.

## Current batch — Enrollment lifecycle-reason workflow

- [x] Inline review: select status, enter required reason for non-Active changes, Save or Cancel; selection alone sends no write.
- [x] Reason/status draft retention on denied/uncertain responses; matching-only reset after confirmation; original creation/end-date semantics and server safeguard retained.
- [x]79 controlled cases PASS:59 retained feedback cases adapted to explicit Save/reason contract plus20 new lifecycle checks.
- [x]44 synthetic responsive browser cases PASS; mobile light/dark screenshots inspected; lint0/0, TypeScript and webpack export83 PASS.
- [x]28 real Identity/HTTP/disposable-SQL cases PASS,88/7 migrations, scoped persistence/readback/no-write controls; owned cleanup verified.
- [x] [Report](QA/REPORTS/PHASE_2B_ENROLLMENT_LIFECYCLE_REASON_REPAIR.md), issue evidence and scoped handoff prepared.
- [ ] Direct live browser-to-SQL, physical Android/iOS, stale-record/concurrent and full critical workflow acceptance; BUG-FUNC-0016 remains OPEN.

## Original feedback routes — bounded checkpoint list

Tick marks below mean a feedback checkpoint, **not full enterprise workflow acceptance**. See original issue/report evidence for each route.

- [x] Branches
- [x] Courses
- [x] Batch Setup
- [x] Compliance
- [x] Schedule
- [x] Attendance
- [x] Make-up Classes
- [x] Leave
- [x] Holidays
- [x] Events
- [x] Communication Settings
- [x] Communications
- [x] Communication Preferences
- [x] Assignments
- [x] Lesson Plans
- [x] Submission Review
- [x] Work Queue
- [x] Access Review Sign-off
- [x] Platform Services
- [x] Leads
- [x] Sales Campaigns
- [x] Trial Bookings — controlled/build/synthetic browser checkpoint
- [x] Curriculum — controlled/build/synthetic browser checkpoint
- [x] Academic Governance — controlled/build/synthetic browser checkpoint
- [x] Academic Periods — controlled/build/synthetic browser checkpoint
- [x] Enrollments — feedback and lifecycle-reason UI/native SQL checkpoint; broader workflow acceptance OPEN
- [x] Batch Promotions — controlled/build/synthetic browser feedback checkpoint
- [x] Assessments — creation feedback checkpoint; prior result-save repair retained

Current bounded feedback count: **28/28 checkpointed**, after the Assessments creation report. Live/domain/device acceptance remains open. Never interpret this as whole-app readiness.

## Next implementation batches

- [x] Academic Governance + Academic Periods feedback — bounded checks completed; prerequisite and year-closure safeguards retained.
- [x] Enrollments + Batch Promotions feedback — bounded checkpoints completed; broader lifecycle/workflow acceptance remains open.
- [x] Assessments creation feedback — existing grade/result/option/roster repairs retained.
- [x] Enrollment required lifecycle-reason UI/native SQL integration (BUG-FUNC-0016) — server safeguard intact; direct browser/device/critical gates remain OPEN.
- [ ] **Next: shared subject-fee fractional-input gap (BUG-FUNC-0008)** — Sol Medium, retain existing minimum/decimal storage/domain policy; reproduce native validity before bounded repair.
- [ ] Shared PENTA/Manual visual foundation: consume versioned FW1 delivery when available, continue independent Manual controls without claiming the shared AI frontend is delivered.
- [ ] FP1/FP2/FP3 explicit owner decisions and legacy disposition — pending, not inferred from “continue.”
- [ ] Approved financial implementation — Sol High; accounting/tax/payroll review before applicable release. Policy-pending finance need not block safe feedback work.

## Upcoming application-wide delivery gates

- [ ] E1: finish remaining critical security/finance/media dispositions and missing fault/browser/device/SQL acceptance. Existing119 issue records/118 distinct groups are not119 untouched fixes; issues can have bounded repairs and remain OPEN.
- [x] E2 bounded portion: all28 original feedback-route checkpoints.
- [ ] E2 remaining live/role/device/domain workflow acceptance; issue closure not inferred from synthetic checkpoints.
- [ ] E3: shared PENTA prototype/component/token/version handoff; premium Manual visual foundation and Platform Owner → Admin → Teacher → Student rollout. Mini owns reusable AI UI; Academy owns Manual/host integration.
- [ ] E4: Mini qualification; latest recorded86/94 is NOT accepted. Resolve failed language/context/provenance/capacity cases, then Academy linked inference/SQL/browser gate. No unchanged benchmark loops.
- [ ] E5: complete conversation/history/context/artifact/privacy/recovery UX and versioned gateway contract.
- [ ] E6: release approved low-risk conversational Executor workflows, one end-to-end manual/AI domain slice at a time.
- [ ] E7: governed financial and communication AI actions with deterministic calculations, approvals, transactions, consent and truthful delivery status.
- [ ] E8: source-backed Pulse/Navigator/Twin, reviewed Academy Brain feedback and bounded Autopilot mandates. No learning-derived permissions.
- [ ] E9: exact-candidate Docker/runtime/database/migration/integration/security/recovery/load/accessibility/physical-device enterprise release gates.
- [ ] E10: reviewed integration and explicit staging/Azure authorization, deployment, smoke and rollback proof. No Azure deployment now.

## Project/model handoff status

- [x] Existing [FW1 Mini frontend handoff](PENTA_HANDOFF_TO_MINI.md) prepared.
- [ ] FW1 delivery and versioned coded frontend returned — not claimed delivered or ready.
- [ ] Switch project only for a genuine Mini-owned dependency; name the exact handoff and return gate first.
- [x] Current feedback work fits **Sol Medium**. Stay on **Sol High** for finance/security/tenant/transaction/privacy/cross-project/release decisions; no automatic chat-model change.
