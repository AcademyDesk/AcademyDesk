# AI-first operational integration — Academy / Mini paired work

## Scope supersession — complete PENTA-side dependency, not students only

Owner explicitly requests **all PENTA AI/Core/Mini work required by Academy Desk**, not a student-only handoff followed by repeated requests for every domain. This section supersedes narrower "next student slice only" wording in the original packet and the prior chat prompt. Student onboarding below is a detailed acceptance example, not a limit on scope or the final stopping point.

The receiving PENTA project should continue its existing program and implement the complete reusable conversational integration capability against the existing Academy action inventory and AI-01–AI-14 families below. Read the current `PENTA_PLATFORM_CONTRACT.md`, frontend delivery/synchronization contracts, governance, engine/schema/evaluation source and latest return before changing anything. Reconcile newer accepted work first; do not redo completed audits or discard existing contracts/fixtures. No separate Academy backend, parallel model engine or new training program is requested.

### Required PENTA-owned deliverables

| ID | Deliverable | Completion evidence required from PENTA |
| --- | --- | --- |
| CORE-01 | Qualified private inference and conversation | Resolve current recorded qualification gaps or document a newer accepted candidate with provenance. Multi-turn intent, follow-ups, corrections, cancellation, ambiguity, optional/cleared fields, mixed requests, unsupported requests and supported-language behaviour measured on frozen and fresh independent cases. Resource/latency/concurrency limits reported; no claim of general ChatGPT parity. |
| CORE-02 | Versioned operational Tool Protocol / connector SDK | Strict schemas and executable validators for server-permitted tool descriptors, bounded arguments, input forms/dialogue state, host observations, evidence references, typed artifacts, drafts/revisions and safe errors. Explicit compatibility/migration from0.1; no arbitrary SQL/code/URL dispatch, automatic downgrade, unknown-field acceptance or authority in model output. |
| CORE-03 | Shared orchestration and workflow state | Bounded multi-step plans, minimal clarification, reference resolution, corrections/review invalidation, cancellation, pause/resume and deterministic state transitions. Host-owned draft/context/version remains authoritative; do not duplicate mutable authoritative state across Core and Academy. Reusable interfaces must support all eligible domain families, not hard-code only learners. |
| CORE-04 | Safe action/approval/status interfaces | Separate proposed/prepared, awaiting host approval, approved, executing, succeeded, failed, cancelled and unknown outcomes. Host-issued operation/revision/approval references are opaque; Core cannot manufacture them. Exact receipt/status reconciliation and no blind write/send retry after timeouts/restarts. Scheduling and bulk operations require explicit bounded host authority. |
| CORE-05 | Evidence grounding, context and Academy Brain interfaces | Source-linked/as-of/coverage-aware answers; observed facts vs hypotheses vs recommendations distinguished. Tenant/actor/connection/purpose/retention-scoped retrieval/history/summary/feedback interfaces, correction and deletion/invalidation hooks. Context is never permission; no secret ingestion, automatic cross-tenant learning or live-chat weight updates. Academy decides and enforces persistence/access/retention. |
| CORE-06 | Five capability implementations | Pulse consumes authorized operational observations for briefings/alerts; Executor prepares and requests permitted operations; Navigator supports admissions/growth/follow-ups; Twin explains authorized evidence and bounded what-if scenarios; Autopilot manages approved routines through host mandates/limits/stop/revoke/escalation. No capability is accepted from its label alone. Every flow uses the same protected tools and host results. |
| CORE-07 | Reusable production-integrable AI frontend | Versioned actual source/components and host-adapter interfaces for prompt-first conversation, composer, capability help, role-appropriate availability, result cards/tables/charts/canvas, source details, clarification, draft correction, exact confirmation, action progress/receipt/recovery, history and feedback. Orbit+Aura/Prism themes and responsive keyboard/focus/overlay behaviour. Remove synthetic bootstrap/identities/history/actions from the production entry; preserve a separately labelled demo. Model selection/attachments/recording exposed only where genuinely supported and qualified. |
| CORE-08 | Runtime integration, qualification and return package | Versioned schemas/SDK/frontend artifacts, hashes, migration/rollback, deployment-independent local startup/health/readiness, cancellation/timeouts/bounds, redacted diagnostics and outage handling. Engine, schema, security/adversarial, state/replay/concurrency, source-grounding, frontend and connector-contract tests with actual counts/limits. Explicit host-dependent SQL/device/provider acceptance remains separate. |

### All domain families must be covered, not merely named

Use Academy's existing operation/controller/form inventories and AI-01–AI-14 rows, read-only. For each permitted action, record the reusable PENTA contract/dialogue/tool-observation/artifact support, Academy domain-service dependency, required approval and exact test status. Group related operations under shared contracts; do not create a new model or code path per endpoint.

Coverage must include:

- Platform/tenant administration, allowed provisioning/support/billing observations and operations, without role/security-configuration tools.
- Admissions/leads/campaigns, student/guardian onboarding and profiles, teacher/staff setup and scoped workload.
- Courses/curriculum, enrollment/promotions, batches/classes, calendars/scheduling/conflicts, attendance, leave, events/holidays and online/offline/hybrid make-ups.
- Assignments/assessments/practice/progress/resources/music and certificates with eligibility/source checks; private picture/audio/video/recording assistance through host-approved media interfaces and truthful preview limits.
- Fee plans/arrangements, invoices/collections/payments, adjustments/reconciliation, expenses, payroll and financial explanations. All authoritative calculations, tax/accounting rules and financial mutations stay deterministic and host-owned.
- Templates/preferences/consent, reminders/messages/approved bulk workflows, Google Meet/Teams/Zoom connection and meeting-operation observations. Provider OAuth/secret entry and real external execution remain host/secure provider flows, never chat credentials.
- Work queues/tasks, governance/access-review/sign-off where permitted, audit/report/export and dashboards, with current role/resource minimization.
- Protected conversation/context/history, approvals/artifacts/usage, reviewed feedback/Brain and bounded Autopilot routines.

Platform Owner, Owner/Admin, Teacher, Student and Guardian must have distinct host-supplied catalogs and privacy scopes. A Teacher must not gain academy-wide finance access; a Student must not gain another student's data; Guardian grants are per child. No role-switching demo or model-returned role becomes authority.

### Implementation and return strategy

The assignment is broad; implement it in cohesive tested internal batches, not an uncontrolled all-at-once deployment. Produce the full action/dependency matrix early, then contract/SDK/orchestration, conversational/state qualification, reusable frontend binding and capability workflows. Return independently usable versioned increments so Academy host work can proceed without waiting for every unrelated capability. Do not stop permanently after the student example or claim a domain complete from generic schemas.

Use explicit statuses: NOT STARTED, IMPLEMENTED, CONTRACT-TESTED, ENGINE-QUALIFIED, HOST INTEGRATION PENDING or BLOCKED with precise missing input. These are distinct from Academy end-to-end accepted or production ready. All required PENTA-side deliverables must be implemented/validated or truthfully reported as blocked; do not hide failed or untested work behind "complete."

Before a genuine model download/replacement, training job, new resource/cost, privacy decision or protocol activation requires separate authority, stop that action and request the exact decision; continue independent authorized work where possible. No fabricated readiness, mandatory public inference fallback, secretly narrowed launch, or promise of one-week completion without evidence. No automatic background work after the receiving turn ends.

### Final return to Academy Desk

Return one authoritative handoff plus the versioned artifacts and per-increment notices: source/branch/commit and dirty-artifact hashes, implemented vs qualified vs host-pending action matrix, wire/component versions and exact migration, example requests/observations/receipts and negative cases, tests actually run/failures retained, local runtime instructions/resource limits, security/privacy constraints, and the ordered Academy connector/UI/persistence/approval/domain-service changes still required. Distinguish mock/synthetic evidence from real inference, host SQL, actual providers and physical devices.

PENTA does NOT own Academy's authenticated domain authorization, schema/migrations, business calculations, real approvals, transactions/audit, provider credentials or final release acceptance. "PENTA-side complete" never means the app is already fully integrated. Do not modify Academy source/main, production databases/credentials or release settings from the PENTA project. No commit/push/merge/Azure deployment is authorized by this handoff.

2026-10-16. Owner direction: finish the full conversational AI experience and the backend it needs before resuming remaining Manual visual migration. Keep the existing Manual ERP functional as fallback; reuse its domain services rather than build another backend. This changes delivery priority, not security, enterprise acceptance or deployment authority.

Academy worktree: `D:\AcademyDesk-codex-p0`, branch `codex/penta-search`, HEAD `b1f8cb6f4209da7fbd14f8089fcb6792438684c4` plus preserved work. Mini project: `D:\PENTA AI Models`; verify its current branch/status before any separately authorized work. This packet has NOT been sent to another chat, implemented by Mini, or accepted as a new protocol. No cross-project edits are authorized merely by preparing it.

## Verified constraint, not a new audit

`apps/api/Intelligence/Penta/PentaMiniProtocol.cs` exposes only `SearchLearners` and `GetLearner`. `ValidPlan` admits tool requests only with READ risk; `AcademyDeskConnector` dispatches only those tools. `PentaMiniOrchestrator` uses actor/tenant-bound protected SQL state, durable request claims, post-planning authorization and atomic result/audit. These are useful existing foundations, not general operational execution.

`PentaDraftApprovalService` is explicitly synthetic: only fixed rehearsal titles, no domain mutation. Do not repurpose it as if an approval already creates a student or sends a message. Existing manual domain controllers are real, but they are not automatically model tools. The current UI2 return is a synthetic interaction prototype, not an authenticated operational product.

N2u's read-language qualification and the recorded N2r 86/94 NOT ACCEPTED remain open unless a newer accepted return with provenance is established. Do not repeat an unchanged benchmark or discard failed receipts. Operational contract work extends the existing program; it does not replace that qualification.

## Concrete acceptance example: student onboarding, not a general write switch

Academy source: `apps/api/Controllers/StudentOnboardingController.cs`, `StudentsController.cs`, `Security/AtomicAcademyMutationAttribute.cs` and its actual filter implementation. Verify exact paths and inspect the existing registered SQL/HTTP onboarding regressions before extracting any shared application service.

Acceptance journey:

1. Owner/Admin asks to onboard a student in ordinary language, supplies details over multiple turns, corrects earlier details and can cancel without writes.
2. Mini proposes bounded field values/clarifications. Academy owns the structured draft, current actor/academy, field schema, validation and reference resolution. Optional absent fields remain absent; never invent names, dates, identifiers or contacts.
3. Reuse current onboarding rules, including required date of birth and applicable minor/guardian requirements. Resolve branch references against current tenant sources. Detect duplicate student number and candidate name/contact matches; do not merge or select a duplicate automatically. Shared family email alone is not proof of duplicate identity.
4. Before execution, show an exact canonical host-generated preview, duplicate-check result and missing/invalid fields. Any edit changes the revision and invalidates earlier approval. Cancellation, expiry and current permission revocation prevent execution.
5. Explicit host-bound confirmation is scoped to actor, academy, operation, target/new-record draft, exact payload/revision, expiry and policy. Model-supplied approval or conversational "yes" alone cannot authorize anything.
6. Execute once through the shared onboarding application service under existing transaction/identity/audit safeguards. A model cannot create roles, change security or handle passwords. Account invitations/credential entry remain a separate secure authorized interface, not chat or tool arguments. Do not silently narrow the existing manual onboarding endpoint.
7. Return authoritative committed result/source link and success only after required persistence/audit. Retries/replay/concurrency must not create duplicate students/guardians/accounts. Pending or unknown outcomes require status/reconciliation, not automatic resend.
8. Prove with real disposable SQL/HTTP and linked real Mini/browser journeys; synthetic plan/UI tests alone do not accept the feature. Preserve existing tests and real failure receipts.

## Mini/Core return required before Academy activates operational chat

Continue in the existing Mini project using its governance and current branch, preserving dirty work. Do not rewrite the engine or change the serving model just to add a tool.

- Define the versioned reusable operational extension needed across all listed domains: partial fields, missing-field questions, corrections/clear vs absent, cancellation, reference ambiguity, proposed preparation and exact host-result observations. Student onboarding below supplies one detailed fixture, not the scope limit. Tool names/identifiers below are not an accepted wire contract.
- Return strict request/decision/state/result schemas, a compatibility/migration decision and bounded synthetic fixtures before activation. Preserve protocol0.1 serving behaviour unless a reviewed paired version migration is explicitly approved. No permissive extra fields, hidden regex/router replacement, automatic downgrade or arbitrary tool dispatch.
- Separate proposal/preparation from host approval and execution. Core receives only server-permitted tool descriptors and bounded privacy-filtered observations. It never supplies authoritative roles, tenants, permissions, prices, audit status or approval.
- Include held-out multi-turn examples for omissions, corrections, optional values, duplicate names, foreign record references, prompt injection, malformed/unknown arguments, interrupted confirmation, stale approvals and unavailable/unknown outcomes. Preserve original qualification expectations and record actual resource/latency limitations.
- Return exact source/artifact versions, hashes, tests actually executed, unresolved engine limitations and paired Academy changes. If a different model/resource/training is needed, provide a separate evidence-backed approval request; no model download, training, external inference or Azure provisioning by implication.

Academy can prepare shared host services/contracts/tests while this return is pending. It cannot claim full conversational writes or enable the READ-only allowlist merely because those services exist. Do not transplant synthetic frontend history/actions as working features.

## Full AI-first task tags — maps to existing E4–E9, not a new testing plan

Each family needs its existing action-level inventory reconciled to: permitted role, current domain service, tool contract, source/validation, dialogue, approval, execution/audit, browser/SQL evidence and release status. Family rows are NOT individual tasks or completion percentages.

| Tag | Existing package | Required AI/backend delivery | Current disposition |
| --- | --- | --- | --- |
| AI-01 | E4 | Accepted pinned Mini: current read qualification plus operational language/context/security evaluation | Pending acceptance; existing N2u dependency retained |
| AI-02 | E5 | Authenticated PENTA frontend, real messages/artifacts/context/status, private history/retention, safe recovery | Partial read pilot; full UI2/history pending |
| AI-03 | E5/E6 | Host-owned permitted-tool discovery, strict operation contracts, draft revisions, approvals, idempotent execution/status/audit | Read-only foundations exist; real domain actions pending |
| AI-04 | E6 | Admissions, leads, student/guardian onboarding and profile corrections/duplicate resolution | Existing ERP reused; student onboarding is one concrete acceptance example |
| AI-05 | E6 | Teacher/staff setup, workload and scoped profiles; secure account setup outside chat | Conversational adapters pending |
| AI-06 | E6 | Courses, enrollment/promotion, batches, calendar, scheduling, attendance, leave, make-ups and conflicts | Domain services retained; operational chat pending |
| AI-07 | E6 | Assignments, assessments, practice/progress, learning resources, certificates and private-media assistance | Scoped conversational integration pending; existing media/issuance safeguards retained |
| AI-08 | E7 | Fee plans, invoices, collections, adjustments/reconciliation, expenses and payroll | Fee facts read pilot exists; approved operational chat pending; deterministic calculations retained |
| AI-09 | E7 | Templates, recipient selection, communications/reminders, approved bulk sends and meeting operations | Existing preparation/lifecycle work retained; real tools/delivery/provider qualification pending |
| AI-10 | E5/E6 | Platform, Admin, Teacher, Student and Guardian own/assigned-record catalogs and appropriate AI entry | Entry exists; Admin read pilot only; other role tools pending |
| AI-11 | E8 | Source-backed Pulse briefings, task inbox and operational alerts | Pending real source/coverage/authority acceptance |
| AI-12 | E8 | Navigator admissions/growth and Twin explanations/what-if analysis | Pending grounded workflows; recommendations never execution authority |
| AI-13 | E8 | Academy Brain privacy-scoped context/feedback and bounded Autopilot mandates/stop/revoke/reconcile | Pending; no live-chat training or memory-based permissions |
| AI-14 | E9 | Exact-candidate linked inference/SQL/API/browser/device/runtime/security/operations acceptance and coverage measurement | Existing enterprise program continues; no whole-app acceptance |

The 90% objective means measured eligible operations successfully completed conversationally with required human oversight, not 90% of endpoints or five visible capability buttons. Denied actions, false successes and Manual-only detours do not count as completed AI journeys. Preserve operation-level results rather than invent a whole-app percentage.

## Ownership and stop boundaries

Academy owns authenticated frontend adapters, domain services, SQL/privacy, approvals/execution and all authoritative policies. Mini owns interpretation/Core, canonical shared schemas and model qualification. Remaining Manual visual polish follows the AI completion priority; Manual correctness, security and fallback remain mandatory throughout.

Use Sol High for paired protocol/domain/approval/security decisions and integration; Medium for settled component implementation. This is routing guidance, not an automatic model/project change. No subagents or external tool messages were started.

Required next checkpoint: execute the complete PENTA-side dependency scope above in the existing Mini project, returning versioned tested increments and the final explicit coverage/gap handoff. Academy prepares and integrates corresponding shared-domain workflows without duplicating Core. Do not close BUG-DATA or other enterprise issues, stage/commit/push/merge/deploy, touch production data/credentials, or modify `D:\AcademyDesk` main. Continue established release gates; a one-week target never authorizes unsafe activation.
