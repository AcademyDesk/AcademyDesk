# Original success-feedback queue reconciliation — 2026-10-10

BUG-FUNC-0003 contains 28 original routes. Reconcile existing source and accepted checkpoints, not a new broad audit or a rerun of accepted evidence. Starting feature HEAD `2f57b1a3675b059c6f57e78000b70d358ddb9c75`, `codex/penta-search`, `D:\AcademyDesk-codex-p0`.

Current progress after [Access Review Sign-off](PHASE_2B_SIGN_OFF_FEEDBACK_REPAIR.md), [Platform Services](PHASE_2B_PLATFORM_SERVICES_FEEDBACK_REPAIR.md) and [Leads](PHASE_2B_LEADS_FEEDBACK_REPAIR.md): **20 route-level feedback checkpoints /8 routes with remaining gaps**. Next `/sales-campaigns`, Sol Medium. The17/11 tables below are the preserved starting reconciliation snapshot, not the current count; remove `/access-review/sign-off`, `/platform-services` and `/leads` from its historical pending list. No accepted prior repair was repeated; enterprise/live/device/release gates stay open.

## Retained checkpoints

| Original routes | Existing evidence / decision |
| --- | --- |
| `/branches`, `/courses`, `/batch-setup` | [Branches](PHASE_2B_BRANCH_FEEDBACK_REPAIR.md), [Courses](PHASE_2B_COURSE_FEEDBACK_REPAIR.md), [Batch](PHASE_2B_BATCH_FEEDBACK_REPAIR.md): accepted feedback repairs retained; separate data/configuration/browser/device gates are not waived. |
| `/compliance` | [Typed subjects and independent feedback](PHASE_2B_COMPLIANCE_UI_REPAIR.md) already covers durable saves, separate refresh warning, draft retention and page-local guards. Do not repeat its repair. |
| `/schedule`, `/attendance`, `/makeup`, `/leave`, `/holidays`, `/events` | Existing [issue checkpoint sections](../ISSUES/BUG-FUNC-0003.md) and their linked reports retained, including subsequent effect-lint follow-ups. |
| `/communication-settings`, `/communications`, `/communication-preferences` | Existing [issue checkpoints](../ISSUES/BUG-FUNC-0003.md) retain provider/channel, queue/banner and consent-feedback evidence. |
| `/assignments`, `/lesson-plans`, `/submission-review` | Existing [issue checkpoints](../ISSUES/BUG-FUNC-0003.md) retained; last Submission Review packet only removed stale pending notice, not another identity rewrite. |
| `/work-queue` | Newly repaired in [this bounded packet](PHASE_2B_WORK_QUEUE_FEEDBACK_REPAIR.md). |

These are **17 route-level feedback checkpoints**, not 17 fully accepted enterprise workflows or a production readiness percentage. Their existing limits remain in force.

## Historical remaining 11 route-level gaps at reconciliation

| Route | Source-confirmed feedback gap / avoid duplicating earlier work |
| --- | --- |
| `/access-review/sign-off` | Successful sign-off calls `load`, which clears `m`; native form element is accessed after await. Next bounded feedback packet. No access-policy change. |
| `/platform-services` | Payment submission, support create and response notices are cleared by `load`; distinguish confirmed save from failed refresh. |
| `/leads` | Create/stage lack durable confirmation; conversion notice cleared by `load`. Keep existing conversion/admissions business rules. |
| `/sales-campaigns` | Create/status refresh without positive confirmation. |
| `/trial-bookings` | Create/status lack durable confirmation. Existing duration repair retained; Sales lookup-permission issue separate. |
| `/curriculum` | Add/publication notice cleared by `load`. Blank sequence contract remains a separate issue. |
| `/academic-governance` | Create/status notice cleared by `load`. Prior finance **Governance** feedback is not this route; prerequisite/domain repairs retained. |
| `/academic-periods` | Create/close messages cleared by `load`. Accepted year-closure transaction repair is not this UI feedback repair. |
| `/enrollments` | Create lacks success; status notice cleared by `load`. Required lifecycle reason is a separate domain-contract task. |
| `/batch-promotions` | Create/decision notice cleared by `load`. Accepted terminal SQL decision/race repair retained. |
| `/assessments` | **Partial**: result save already has durable success, refresh warning and retained error/draft from [grade repair](PHASE_2B_ASSESSMENT_GRADE_REPAIR.md). Creation still sets an empty notice before `load`; do not redo result grading/options/roster fixes. |

Count is routes, not forms/actions/tests or whole-release work. BUG-FUNC-0003 stays OPEN. Historical issue header/commit/lint metadata is not current acceptance; per-packet reports and checkpoint sections govern.

## Routing

Continue **Sol Medium**, Academy Desk, next Sales Campaigns feedback only. No new testing plan, Mini/runtime training, project switch or Azure deployment. Escalate to **Sol High** only if a real authorization, tenant, domain, transaction, privacy or persistence decision is required. Existing first real PENTA read, premium integrated frontend, Mini qualification, security and enterprise release gates continue in parallel; manual ERP improvements are not deferred until the entire compliance program finishes.
