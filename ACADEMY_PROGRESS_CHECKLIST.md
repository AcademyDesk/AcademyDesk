# Academy Desk — delivery progress checklist

Updated:2026-10-11. Worktree `D:\AcademyDesk-codex-p0`, branch `codex/penta-search`. Application goal: **90% eligible operations through conversational PENTA AI;10% manual control/fallback**. This is a target, not measured current coverage.

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

## Past batch — Enrollment lifecycle-reason workflow

- [x] Inline review: select status, enter required reason for non-Active changes, Save or Cancel; selection alone sends no write.
- [x] Reason/status draft retention on denied/uncertain responses; matching-only reset after confirmation; original creation/end-date semantics and server safeguard retained.
- [x]79 controlled cases PASS:59 retained feedback cases adapted to explicit Save/reason contract plus20 new lifecycle checks.
- [x]44 synthetic responsive browser cases PASS; mobile light/dark screenshots inspected; lint0/0, TypeScript and webpack export83 PASS.
- [x]28 real Identity/HTTP/disposable-SQL cases PASS,88/7 migrations, scoped persistence/readback/no-write controls; owned cleanup verified.
- [x] [Report](QA/REPORTS/PHASE_2B_ENROLLMENT_LIFECYCLE_REASON_REPAIR.md), issue evidence and scoped handoff prepared.
- [ ] Direct live browser-to-SQL, physical Android/iOS, stale-record/concurrent and full critical workflow acceptance; BUG-FUNC-0016 remains OPEN.

## Past batch — Shared subject-fee decimal input

- [x] Native browser reproduced fractional stepMismatch before source repair;72 baseline checks.
- [x] One shared step0.01 attribute fixes both routes; existing minimum1 and backend/payload policy unchanged.
- [x]88 synthetic browser checks PASS across both routes,320/1440px,light/dark; valid exact requests and invalid no-write behavior verified; two mobile screenshots inspected.
- [x] TypeScript and83-page webpack export PASS; [report](QA/REPORTS/PHASE_2B_SUBJECT_FEE_DECIMAL_REPAIR.md) and OPEN issue updated.
- [x] Existing effect lint error/dependency warning cleared in the following bounded loading/feedback batch; previous decimal-only report retains its historical NOT PASS result.
- [ ] Real SQL/HTTP/role/tenant, physical Android/iOS and critical regression acceptance; BUG-FUNC-0008 OPEN.

## Past batch — Shared subject-fee loading/save feedback

- [x] Checked initial read, durable polite notices, denied/uncertain draft retention and confirmed-write/failed-refresh distinction; no automatic write retry.
- [x] Synchronous write/readback guard, disabled controls, matching-only reset and fresh keyed student editor; late unmounted results ignored.
- [x]17 actual-TSX checks and72 synthetic responsive browser cases PASS; retained88 decimal checks PASS; mobile screenshots inspected.
- [x] Target lint0/0, TypeScript and83-page webpack export PASS; [report](QA/REPORTS/PHASE_2B_SUBJECT_FEE_FEEDBACK_REPAIR.md) and scoped handoff prepared.
- [ ] Live SQL/HTTP/role/tenant, physical Android/iOS and critical workflow acceptance remain separate; existing issues OPEN.

## Past batch — Admission-fee loading/save feedback

- [x] Checked initial details/workspace; keyed editor isolates late loads/saves across students; failed reads cannot enable saving.
- [x] Durable polite success, retained values/drafts, denied/uncertain guidance and synchronous duplicate/date/pending guards; optional nulls/zero/precision/PUT/domain unchanged.
- [x]21 new +17 retained handler checks PASS;52 synthetic responsive cases and88 retained decimal cases PASS; mobile screenshots inspected.
- [x] Target lint0/0, TypeScript and83-page webpack export PASS; [report](QA/REPORTS/PHASE_2B_ADMISSION_FEE_FEEDBACK_REPAIR.md), issue and scoped handoff prepared.
- [ ] Real SQL/HTTP/role/tenant/physical-device/critical acceptance; existing issue and enterprise release gates OPEN.

## Past batch — Student360 administrative-profile feedback

- [x] Durable polite success and distinct confirmed-write/display-failure guidance; denied/uncertain drafts retained without automatic retry.
- [x] Synchronous duplicate guard and disabled fields/dates/notes; all13 fields and optional-date-null PUT preserved.
- [x] Keyed student editor preserves same-record draft; switching clears the old profile before loading, and late reads/writes cannot overwrite the new selection.
- [x]18 controlled TSX checks and44 synthetic browser cases PASS (320/1440px, light/dark); mobile screenshots inspected.
- [x] Target lint0/0, TypeScript and83-page webpack export PASS; [report](QA/REPORTS/PHASE_2B_STUDENT_PROFILE_FEEDBACK_REPAIR.md) and scoped handoff prepared.
- [ ] Live SQL/Identity/role/tenant and physical Android/iOS/full-critical acceptance; BUG-FUNC-0003 remains OPEN.

## Past batch — Teacher availability profile runtime repair

- [x] BUG-API-0006 reproduced:4/26 controller failures; SQL stored-null GET500 after15 passing controls.
- [x] One null-slot projection guard; optional storage/JSON fallback and valid slots preserved, no new schedule/permission rule.
- [x]26 new+6 retained unit cases PASS;45 real SQL/Identity/HTTP cases PASS, fresh stored/readback and unauthorized no-write controls.
- [x] Harness build0 warnings/0 errors;88/7 migrations, owned database/login cleanup and both owned-container removals verified; [report](QA/REPORTS/PHASE_2B_TEACHER_AVAILABILITY_REPAIR.md).
- [ ] Teacher onboarding/profile browser, physical Android/iOS and full critical/release acceptance; issue OPEN.

## Past batch — Optional onboarding dates

- [x] BUG-API-0001 serialized-empty-date defect reproduced;9 actual-handler tests baseline5 PASS/4 FAIL, fixed9 PASS.
- [x] Teacher DOB/joining and student admission blanks now null; required student DOB/all other payloads retained. Captured native forms fix related async reset failures.
- [x]32 synthetic responsive browser cases and23 real SQL/Identity/HTTP binding/persistence/no-write cases PASS;88/7 migrations and owned cleanup verified.
- [x] Target lint0/0, TypeScript,83-page webpack export and harness0/0 PASS; [report](QA/REPORTS/PHASE_2B_ONBOARDING_DATE_REPAIR.md).
- [ ] Linked browser-to-SQL, physical devices, optional-all-fields/onboarding Identity rollback/critical acceptance; issue OPEN.

## Past batch — Student onboarding transaction integrity

- [x] Existing BUG-DATA-0004 native invalid-second-password400/changed snapshot reproduced; no repeated accepted audit.
- [x] Shared SQL domain/Identity/audit transaction replaces inner domain-only boundary; role creation/membership results checked, authorization unchanged.
- [x] Explicit platform-owner transaction opt-in protects onboarding only; other endpoint bypass behavior retained.
- [x]21 real SQL/Identity/HTTP failure/success/role/tenant cases and23 unchanged optional-date native regressions PASS;88/7 migrations and exact owned cleanup verified.
- [x]56 targeted unit tests and isolated builds0/0 PASS; [report](QA/REPORTS/PHASE_2B_ONBOARDING_ATOMIC_REPAIR.md), issue and scoped handoff updated.
- [ ] Linked browser/device/concurrent/cancellation/store-mismatch/full-critical acceptance; issue OPEN. Partial optional-credential created flags remain separate BUG-DATA-0007.

## Past batch — Truthful onboarding account-created flags

- [x] Existing BUG-DATA-0007 parent username-only false-positive200 flag reproduced; omitted-account control passed.
- [x] Flags now reflect confirmed CreateAccount/role completion; original optional incomplete credentials and transaction/authority retained.
- [x]33 real SQL/Identity/HTTP matrix cases PASS: both accounts partial/blank/complete, absent guardian/minor defaults, rejected no-write and exact successful row/role proof.
- [x] Unchanged21 native rollback cases and56 targeted unit tests PASS; builds0/0,migrations88+7/owned cleanup; [report](QA/REPORTS/PHASE_2B_ONBOARDING_ACCOUNT_FLAGS_REPAIR.md).
- [x] Issue/checkpoint/E0/handoff updated, source/receipt local; no frontend/Mini/main/Azure or unrelated publication.
- [ ] Linked browser/physical devices/full critical acceptance; issue OPEN. Blank optional student numbers and broader intake pending/unknown-result UX remain separate.

## Past batch — Optional student-number SQL normalization

- [x] Existing BUG-DATA-0008 reproduced: first blank intake200/second400; fresh owned SQL one empty-number row.
- [x] Blank/whitespace numbers now NULL; optional fields, explicit trimming/unique index/length limit and transaction/authority unchanged. No migration or historical-row rewrite.
- [x]29 new native cases PASS, including null/blank repetitions, full minor/account chains, duplicates/tenant/role/length guards, legacy compatibility and one simultaneous blank pair.
- [x] Unchanged33 account-flag and21 transaction native cases plus56 targeted unit tests PASS; builds0/0,88+7 migrations and exact owned cleanup; [report](QA/REPORTS/PHASE_2B_ONBOARDING_NUMBER_REPAIR.md).
- [x] Issue/register/E0/checkpoint/handoff updated; evidence local, unrelated Mini work preserved. Feature publication only, not main/Azure.
- [ ] Linked browser/physical devices/broader race/stale/full critical acceptance; issue OPEN. Queued intake feedback and visual work remain next.

## Past batch — Student/Teacher onboarding feedback

- [x] Checked initial academy readiness, synchronous pending/duplicate guards, native field disabling and accessible status.
- [x] Clear success/rejected/unconfirmed feedback; retained failed drafts, safe confirmed-student link fallback; original payloads/reset/navigation/backend authority retained.
- [x]48 new+9 retained handler checks PASS; frozen baseline1 PASS/47 FAIL, not47 distinct bugs.
- [x]48 new+32 retained synthetic desktop/mobile light/dark browser checks PASS; two mobile screenshots inspected. Lint0/0/types/export83; [report](QA/REPORTS/PHASE_2B_ONBOARDING_FEEDBACK_REPAIR.md).
- [ ] Live browser-to-SQL, physical devices and full critical acceptance remain OPEN; previous native proofs retained, not rerun.

## Past batch — Manual detail-modal foundation

- [x] Current Mini frontend ownership/plan inspected; no returned versioned FW1 release claimed or pilot replaced.
- [x] Shared Manual popup: native top layer/inert background,44px close, Escape/outside dismissal, focus return and restored scroll; existing host tokens/data/permissions retained.
- [x]32 synthetic exported-browser cases PASS across320/390/768/1440/light-dark/empty-long content; mobile screenshots inspected. Lint0/0/types/export83 PASS; [report](QA/REPORTS/E3_MANUAL_DETAIL_MODAL_FOUNDATION.md).
- [ ] All consumers/portals, physical Android/iOS/Safari/screen-reader/live integration and full E3 acceptance remain OPEN.

## Past batch — Platform Owner navigation/header foundation

- [x] Missing Announcements from Overview reproduced and repaired through one shared presentation registry; existing routes/authority/switching preserved.
- [x] Desktop active-page semantics, hidden decorative icons,44px navigation targets and visible focus; phone header overlap reproduced with text geometry and repaired.
- [x]16 final synthetic journeys PASS at320/390/768/1440/light-dark/from both entry points, all11 sections/URL/history/scroll/overflow/header checks; mobile screenshots inspected. Types/export83 PASS; [report](QA/REPORTS/E3_PLATFORM_NAVIGATION_FOUNDATION.md).
- [x]32 unchanged shared-popup regression cases passed during this batch; source unchanged. New registry lint0/0.
- [ ] Existing Platform page lint8 errors/10 warnings unchanged; portal overlays/utility states/remaining forms/live/physical devices and full E3 remain OPEN.

## Past batch — Backend holiday DELETE routing

- [x] Existing BUG-API-0008 confirmed through actual MVC/TestServer: duplicate candidates and ambiguous HTTP; baseline0/4.
- [x] Legacy source retained/excluded from MVC; canonical endpoint, payloads, domain/permission/filter rules preserved.
- [x]4 new routing/HTTP +36 retained audit tests PASS; native SqlHarness build0 errors/0 warnings; [report](QA/REPORTS/PHASE_2B_HOLIDAY_ROUTE_REPAIR.md).
- [x]11 real SQL/Identity/HTTP checks PASS; no429, migrations88+7, exact run-owned database/login/container cleanup verified. Initial Docker/execution-policy blocks resolved without global policy changes; feature publication prepared.
- [ ] Live browser, physical Android/iOS, audit-fault atomicity and full critical acceptance remain separate; BUG-API-0008 OPEN.
- [ ] Interrupted Platform detail-overlay/utility edits remain local and unaccepted; preserve for the shared redesign, not a completed visual phase.
- [ ] Owner design direction: all4 Manual portals match PENTA AI; Orbit + Aura dark /Prism light, shared responsive foundation. AI frontend stays in PENTA AI project; Academy owns Manual/host integration.

## Past batch — Backend metadata shape safety

- [x] Existing BUG-API-0004 reproduced: new35-case starting-source suite10 PASS/25 FAIL; not25 separate defects.
- [x] Narrow compensation/portal/platform JSON-kind guards; valid aliases/zero/time/audience behaviour and authority unchanged, no storage rewrite.
- [x]35 new +41 retained notification-channel +4 retained holiday tests:80/80 PASS; [report](QA/REPORTS/PHASE_2B_METADATA_SHAPE_REPAIR.md).
- [x]72 real SQL/Identity/HTTP metadata cases and47 unchanged native announcement audience cases PASS; no429,88+7 migrations and exact owned database/login/container cleanup verified; scoped feature publication prepared.
- [ ] Browser/physical-device/full-critical/whole-portal acceptance; issue remains OPEN.

## Past batch — Expense integrity and recording workflow

- [x] BUG-DATA-0047 starting controller reproduced:9/32 PASS; foreign/missing/empty branches and fabricated status rejected by repair.
- [x] Existing field/decimal bounds, optional/legacy values and immutable update currency preserved; SQL replacement reads locked through domain/audit commit.
- [x] 33 expense +36 unchanged audit tests69/69 PASS; five valid/invalid and five valid/valid concurrent replacements, audit rollback/recovery and tenant/role matrix covered by66/66 native SQL/API cases.
- [x] Truthful record-form readiness/pending/success/rejection/unconfirmed feedback, draft retention and optional branch403 flow; exact integer-cent totals and additive amountExact contract.
- [x]45 handler/money +54 controlled responsive browser cases PASS; four linked real SQL/browser journeys with five saves/four rejected foreign-branch writes PASS, no429. Types/lint/export83/builds0/0; exact owned SQL/login/container cleanup verified. [Report](QA/REPORTS/PHASE_2B_EXPENSE_INTEGRITY_REPAIR.md).
- [ ] Physical Android/iOS, full critical regressions, cross-tab/server-idempotency and accounting/release gates remain separate. Issue OPEN/RETEST; no complete new CRUD editor, tax/FX certification, main merge or Azure.

## Past batch — Work-item ownership and linked-record integrity

- [x] BUG-DATA-0048 starting controller reproduced:12/36 PASS; invalid assignees, links and oversized fields fail the new guards.
- [x] Shared academy-owned active/non-portal staff guard on general and collections creation; optional null/custom staff retained, no role grant.
- [x] Supported local Invoice/PersonDocument links and paired fields validated; general creation uses existing domain/audit transaction, finance balance/authority preserved.
- [x]100 targeted API tests and79 real SQL/Identity/HTTP cases PASS, no429; migrations88/7 and exact owned database/login/container cleanup verified.
- [x]24 actual-handler checks PASS; inactive choices removed, unavailable historical owners labelled truthfully, safe rejected drafts retained; types/lint and83-page webpack export PASS.
- [x]32 synthetic responsive browser cases PASS,320/1440px light/dark; two mobile screenshots inspected. [Report](QA/REPORTS/PHASE_2B_WORK_ITEM_INTEGRITY_REPAIR.md).
- [ ] Linked browser-to-SQL, physical Android/iOS, concurrent offboarding/deletion, status audit-fault and full critical/release acceptance. Issue remains OPEN/RETEST; no main merge/Azure/Core change.

## Past batch — Student import identity integrity

- [x] BUG-DATA-0049 baseline4/17 controller PASS; disposable SQL confirmed validation accepted and import stored duplicate normalized emails.
- [x] Every duplicate file row identified after trim/case comparison; existing local matches rejected, optional blank email/number/phone stored NULL, null/oversized rows rejected.
- [x] Academy-locked import and domain/audit transaction;57 native SQL/Identity/HTTP checks PASS, including five guarded concurrent pairs and audit-fault rollback, no429; exact owned cleanup.
- [x]89 targeted API and27 actual-handler checks PASS; validation gating, retained failed/uncertain drafts, truthful success and pending guards; types/lint/export83 PASS.
- [x]36 synthetic desktop/mobile light/dark browser cases PASS; two mobile screenshots reviewed. [Report](QA/REPORTS/PHASE_2B_STUDENT_IMPORT_INTEGRITY_REPAIR.md).
- [ ] Linked browser-SQL, physical Android/iOS, other student create/update writers, full CSV grammar/idempotency and critical/release gates. Issue OPEN/RETEST, no main/Azure/Core changes.

## Current batch — Teacher compensation zero-rate round-trip

- [x] BUG-DATA-0050 actual handler baseline22/39 PASS;17 failures confirm loaded/response-hydrated numeric zero becomesNULL.
- [x] One absence-aware converter preserves all five numeric/typed zero rates, optionalNULL, model exclusions and dates; no API/payroll/accounting policy change.
- [x]39 handler checks,35 retained metadata API and39 native SQL/Identity/HTTP checks PASS; fresh SQL/readback/resave/foreign sentinel and denied/no-write guards, no429/exact owned cleanup.
- [x] TypeScript and83-page production export PASS; target lint0 errors/1 inherited effect warning, independently unchanged against HEAD.
- [x]40 synthetic desktop/mobile light/dark browser cases PASS, including48 valid resave requests and16 native-invalid no-write cases; two mobile screenshots reviewed. [Report](QA/REPORTS/PHASE_2B_TEACHER_COMPENSATION_ZERO_REPAIR.md).
- [ ] Linked browser-SQL, physical devices, editor stale-selection/pending/recovery, full critical/release acceptance; issue OPEN/RETEST.

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

## Current batch — Finance Policy payslip-theme preservation

- [x] BUG-DATA-0053 actual-handler reproduction:0/6 baseline,6/6 final PASS; existing three theme values preserved on ordinary visible-policy saves.
- [x] TypeScript/target lint and83-page export PASS; [local report](QA/REPORTS/PHASE_2B_FINANCE_POLICY_THEME_PRESERVATION.md) records exact scope/limits.
- [x] Native HTTP/SQL:15 ordinary/reset/rejection/authority controls plus3 explicit stale-overwrite requests checked, no429,88/7 migrations/exact owned cleanup. Not a concurrency protection pass.
- [x]16 synthetic mobile/desktop light/dark browser cases PASS; two screenshots inspected; explicit API reset contract retained. [Verification report](QA/REPORTS/PHASE_2B_FINANCE_POLICY_THEME_PRESERVATION.md) and scoped feature publication packet prepared.
- [ ] Stale-editor/concurrent document change remains **unfixed**, plus linked/device/critical gates; Sol High for consequential concurrency/API decision.

## Past batch — Teacher publication form contract

- [x] BUG-API-0005 reproduced against frozen actual handler:6/12 PASS,6 failures.
- [x] Boolean publication, optional blank assessment-date NULL and captured form-reset repair:12/12 handler checks PASS.
- [x] TypeScript and83-page production export PASS; [local report](QA/REPORTS/PHASE_2B_TEACHER_PUBLICATION_CONTRACT_REPAIR.md) recorded. Inherited teacher lint4 errors/3 warnings unchanged, NOT clean.
- [x]20 real Identity/HTTP/SQL cases: strict bool/date binding, exact one-row create/readback, malformed/role/foreign-batch no-write controls; no429,88/7 migrations/exact owned cleanup.
- [x]16 synthetic exported-browser cases:320/1440px/light-dark, exact POST/confirmation/reset/no console/overflow; two mobile screenshots inspected. Scoped feature publication after checks; not linked browser-SQL or final visual acceptance.
- [ ] Physical devices/full critical regression and wider teacher form pending/network-recovery acceptance; issue OPEN.

## Next implementation batches

- [x] Academic Governance + Academic Periods feedback — bounded checks completed; prerequisite and year-closure safeguards retained.
- [x] Enrollments + Batch Promotions feedback — bounded checkpoints completed; broader lifecycle/workflow acceptance remains open.
- [x] Assessments creation feedback — existing grade/result/option/roster repairs retained.
- [x] Enrollment required lifecycle-reason UI/native SQL integration (BUG-FUNC-0016) — server safeguard intact; direct browser/device/critical gates remain OPEN.
- [x] Shared subject-fee fractional-input repair (BUG-FUNC-0008) — bounded browser proof; broader issue acceptance OPEN.
- [x] Shared fee editor loading/feedback lint gap — bounded proof complete, broader acceptance OPEN.
- [x] Admission-fee loading/save feedback in Student Fee Details — bounded proof complete; broader acceptance OPEN.
- [x] Student360 administrative-profile save feedback/pending/late-response gap — bounded UI proof; live privacy/role/SQL acceptance remains OPEN.
- [x] Existing queue continuation: BUG-API-0006 teacher availability runtime reproduction/repair/native SQL acceptance; broader issue OPEN.
- [x] BUG-API-0001 optional onboarding dates — bounded serialization/browser/native SQL proof; broader acceptance OPEN.
- [x] BUG-DATA-0004 onboarding domain/Identity transaction fault gate — bounded native repair/proof complete; wider acceptance OPEN.
- [x] BUG-DATA-0007 incomplete optional-credential created-account flags — bounded native proof complete; broader acceptance OPEN.
- [x] BUG-DATA-0008 optional-number collision — bounded29-case native normalization/uniqueness/chain proof; wider acceptance OPEN.
- [x] Student/Teacher onboarding feedback — bounded handler/synthetic browser proof; live/device acceptance OPEN.
- [x] Independent Manual/shared visual foundation started: current ownership/tokens checked and shared detail-popup checkpoint completed; not all shared controls or E3 completion.
- [x] Platform Owner Manual navigation/header checkpoint —16 synthetic journeys, not full portal acceptance.
- [x] BUG-DATA-0047 expense integrity and recording workflow — bounded API/native/linked browser proof complete; wider issue OPEN/RETEST.
- [x] BUG-DATA-0048 work-item assignee/entity ownership — bounded repair/native proof; broader acceptance OPEN/RETEST.
- [x] BUG-DATA-0049 student import duplicate identities — bounded repair/API/native proof, broader acceptance OPEN/RETEST.
- [x] BUG-DATA-0050 teacher compensation zero-rate round-trip — bounded conversion/native proof; broader acceptance OPEN/RETEST.
- [x] BUG-API-0005 bounded handler/native/synthetic browser proof completed; broader issue OPEN/RETEST.
- [ ] **Next: BUG-DATA-0053 stale-settings protection across both editors**, newer Professional overwritten by old Compact with200 reproduced in real SQL. Ordinary-save repair/native/synthetic browser proof completed, do not redo it. Sol High consequential concurrency/API/SQL contract; Medium settled UI, no accepted-audit restart.
- [ ] Manual shared redesign foundation and Platform → Admin → Teacher → Student rollout: Orbit + Aura dark /Prism light, reusable responsive tokens/shell/controls; preserve interrupted overlay work. Sol High foundation/host contracts, Medium settled rollout. Inherited lint remains open.
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
