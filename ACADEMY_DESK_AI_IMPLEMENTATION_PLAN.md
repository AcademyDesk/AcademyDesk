# Academy Desk PENTA — AI implementation plan

Status: retained PENTA technical plan, 2026-10-04. Planning branch: codex/enterprise-p0-continuation. Current cross-product sequencing and vertical-delivery rule are in [ACADEMY_DESK_PRODUCT_ROADMAP.md](ACADEMY_DESK_PRODUCT_ROADMAP.md). AI1-S1a/b scoped read backends are locally tested, but the user-facing pilot is not complete. This document authorizes no live AI capability, production data disclosure, GitHub merge, or Azure deployment. Validated feature-branch commits may be pushed after scope/secret checks.

Naming is confirmed: Pulse, Executor, Navigator, Twin and Autopilot form Academy Desk PENTA. The focused architecture design is recorded in [PENTA_ARCHITECTURE_DECISION.md](PENTA_ARCHITECTURE_DECISION.md). AI0-S1 is now locally implemented with bounded API and real SQL/HTTP evidence in [its report](QA/REPORTS/PENTA_AI0_S1_FOUNDATION.md); the next engineering task is AI0-S2. Historical Operator/Growth names map to Executor/Navigator without changing existing business or subscription names.

## 1. Product outcome

Academy Desk should help run a music academy, and later other instructor-led academies. Target 80–90% of eligible day-to-day outcomes reached through AI assistance or approved automation, with 10–20% manual control for precision, exception handling, fallback, administration and governance. This is a direction, not a measured current percentage or a target for autonomous financial decisions.

The existing ERP is the system of record and deterministic control layer. One shared intelligence platform becomes the interaction and automation layer. Pulse, Executor, Navigator, Twin and Autopilot are five PENTA experiences, not five independent databases or unrestricted agents. AI can find, interpret, propose, prepare and explain. Only allowlisted operations can execute through the same current business rules and authorization as manual actions. The LLM never receives raw SQL or unrestricted database writes.

The first useful experience should include an admin command/workspace, bounded natural-language search, source-linked answers, a truthful action preview, a task inbox and a daily briefing. Manual ERP remains under My Academy with deep links from AI results. Teacher, Student and Guardian AI surfaces follow their own narrower permission contracts; an admin pilot must not imply they are safe.

## 2. Verified starting point and backend readiness

Evidence: AGENTS.md, AI_HANDOFF.md, AI_PRODUCT_VISION.md, ENTERPRISE_TESTING_ROADMAP.md, QA/REPORTS/AI_READINESS_ASSESSMENT.md, QA/ISSUES/INDEX.md, source inspected in apps/api and apps/web. The linked AI assessment is a dated snapshot; its first successor checkpoint records the local staff-role repair. Existing scoped test passes are retained, not treated as release acceptance.

| Layer | Reuse now | Gap before live AI |
| --- | --- | --- |
| Domain and persistence | ASP.NET Core controllers, EF Core, SQL Server, academy entities, existing finance/scheduling/admissions logic | No universal application-service/tool boundary; some operations have unresolved invariants or ambiguous failure outcomes. |
| Identity and authorization | Identity bearer sessions, live role/grant/module checks, academy-scoped route filter, targeted role-replacement repair | Filter only runs its academy checks for actions with academyId; platform-owner bypass and role-specific portal routes need explicit AI parity checks. Role/lifecycle and token/key deployment acceptance remain open. |
| Audit and transactions | Domain AuditLog and selected finance/Identity atomic boundaries | No correlated AI action, approval, attempt, cost or outcome ledger; shared audit/transaction boundary is not universal. |
| Frontend | Next.js static export, workspace shell, role portals, design system and mobile repairs | No AI workspace, task inbox, execution-state UI, role-aware AI navigation or end-to-end AI trust UX. |
| Media and messaging | Private Blob abstraction, notifications/preferences/templates and status records | Azure Blob enablement and large-file/device acceptance remain open; queued notification is not verified delivery; secure provider connection, consent-at-dispatch and outbox are missing. |
| Testing and release | Existing API tests, disposable SQL/HTTP harness, many scoped repairs and QA reports | Five P0 issues remain formally open, 97 P1 and 13 P2 entries remain in the index; no full release certification. The AI readiness report also tracks one additional distinct issue group. Current branch is ahead of reviewed main; production version was not inspected here. |

Decision: the backend is READY TO REUSE for a disabled, locally tested AI foundation. It is NOT READY for unrestricted live AI reads, any AI mutation, outbound Autopilot or financial AI execution. Fix the exact domain and permission boundary needed by each capability rather than rebuilding the ERP or waiting for every cosmetic issue.

## 3. Non-negotiable architecture

User/UI → authenticated Intelligence Gateway → intent/model adapter → versioned allowlisted tool → server-derived actor/academy context → current authorization plus tool-specific field/record policy → approval when required → existing protected domain operation → SQL/queue/provider → correlated audit and truthful result.

Start as a modular area in the existing API and existing SQL database. Do not introduce five AI microservices, GPUs, a vector database, Redis or a separate queue without a measured need. Use existing protected HTTP operations or extract one shared application operation when necessary; never invoke a controller directly while bypassing its filters. The frontend is a static export, so the API owns orchestration and execution. Use polling for durable state first; stream only if a measured UX benefit justifies it.

Each tool contract must state version, exact typed input/output, maximum result size, allowed roles, module and tenant scope, sensitive fields, risk class, confirmation rule, audit event, timeout, idempotency key, freshness requirements and failure categories. The server derives actor and academy; model text and retrieved documents are data, not authority. Recheck rights, target state, consent and budgets immediately before execution.

Execution states: Proposed → AwaitingApproval → Approved → Executing → Succeeded / Failed / OutcomeUnknown, with Rejected, Expired and Cancelled branches. Approval binds actor, academy, approver, tool/version, canonical arguments, targets, amount/currency where relevant, entity version and expiry. Stale or changed input requires new approval. Timeout or lost provider response is OutcomeUnknown until reconciled; do not silently retry or show success.

Store safe, redacted action/attempt metadata and usage; do not log raw credentials, minors' private notes, unbounded prompts or whole documents. Model-provider configuration, retention/residency, student-data disclosure and budget policy require explicit product/privacy decisions before live data leaves the API.

## 4. Ordered work packages and exit gates

The work packages below are dependency ordered within a capability, but enterprise remediation can proceed in parallel. Every package ends with a report linking exact tests and unresolved conditions.

### Track E — trusted ERP and deployment boundary (parallel throughout)

E1. Close the five P0 records with their missing policy, browser/device, concurrency, media and full critical acceptance: BUG-DATA-0001, 0002, 0003, 0010 and BUG-SEC-0001. For 0002, separate price reductions, refund liabilities/settlement and GST credit-note documents under the approved IFRS-principles/Indian-GST direction; obtain qualified accounting/tax review. Do not call the current Refund adjustment a payout.

E2. Triage the 97 P1 findings into reproduced blocker, bounded local fix awaiting acceptance, nonblocking accepted deferral, or disproved finding. Prioritize identity/role/guardian security; onboarding/account atomicity; scheduling/admission invariants; financial currency/status/history; consent/provider; audit failure; and core navigation. Preserve historical evidence rather than re-auditing everything.

E3. Verify role-replacement successor repair in the actual intended execution path; complete scope-limited identity/session, module/grant, cross-tenant, portal-route and platform-bypass checks. Persist and validate a shared Data Protection key ring across Azure revisions/replicas before live AI sessions or approvals.

E4. Build/test both Docker images and startup, real migration upgrade/rollback plan for both EF contexts, SQL transactions, relevant HTTP/E2E paths, mobile devices, lint/typecheck, load/fault controls, secure Blob configuration and release rollback/restore. Production deployment remains blocked by QA/09_RELEASE_GATES.md.

### AI-0 — shared intelligence foundation; disabled by default

A0.1. Architecture/threat decision record: identity and tenant derivation, provider/data handling, trusted operation boundary, risk classes, approval delegation, audit retention, budget limits and failure modes. Review original/source-selected role and bypass behavior. Exit: approved explicit trust-boundary and decision matrix.

A0.2. Add feature-flagged gateway with deny-by-default routing, server-derived actor/academy, request size/rate limits, input/output validation, cancellation/timeouts and no callable live tools initially. Exit: unauthorized, foreign-tenant, inactive-academy and disabled-flag tests all deny without model invocation or data disclosure.

A0.3. Add a versioned tool registry and one synthetic/no-data tool; enforce role/module/field scoping outside prompts. Exit: no arbitrary endpoint, SQL, URL, code or tool-name dispatch; parameter tampering tests pass.

A0.4. Add provider abstraction/model router with structured responses, fallback policy, circuit breaker and evaluation fixtures. Keep provider secrets only in approved server-side configuration. Exit: model changes do not alter tool authority or response contract; tests use a fake provider without real data/cost.

A0.5. Add AI execution, approval/attempt and usage/idempotency records with tenant indexes, redaction, retention controls and migrations. Exit: replay/concurrent approval and rollback/unknown-outcome tests pass on disposable SQL; audit attribution matches domain outcome.

A0.6. Add per-tenant/user/feature budgets, usage reservation/reconciliation, latency/error metrics and kill switch. Exit: exhausted budget or provider failure degrades to manual UI without writing or leaking data.

AI-0 is complete only when the disabled foundation has native, real HTTP and SQL security/fault tests and a reviewed schema. It does not equal a live AI pilot.

### AI-1 — first useful, read-only Pulse/Executor pilot

A1.1. Choose one admin-only pilot academy and two or three bounded tools, for example a minimal student search, batch lookup and schedule lookup. Do not expose all student fields or raw controller responses. Repair and retest each source's known integrity/permission issues first.

A1.2. Implement scoped read projections with source IDs, as-of timestamp, timezone, currency, maximum rows and citation/deep-link information. Distinguish recorded facts, deterministic calculations, AI interpretations and unknowns. Do not fabricate availability, fees or progress.

A1.3. Build the initial AI workspace: command input, clarification only for necessary missing fields, result cards, evidence links, My Academy fallback, error/denial states, task inbox placeholder and mobile/keyboard access. Status labels reflect server execution IDs, never animation alone.

A1.4. Add a deterministic first Pulse briefing (for example truly overdue tasks or missing attendance) with rule version, deduplication and dismiss/snooze. Only then use a model to explain selected facts. Do not use the current AdminIntelligence output without fixing its known orphan and amount semantics.

A1.5. Run golden-question evals, prompt-injection and cross-tenant denial suites, role/field visibility, stale data, large result, source-missing, cost, browser and physical-device checks. Enable only for the allowlisted pilot after data disclosure/retention and operator sign-off.

AI-1 exit: a real user can ask a useful question and receive a correct, scoped, source-backed answer, with no unapproved write path.

### AI-2 — controlled Executor actions

A2.1. Start with a low-impact draft/task operation, not payment, payroll, account/role change or bulk send. Implement Prepare → Preview → Confirm → Execute → Verify against one protected operation.

A2.2. Bind approval to exact targets/arguments and current rights; add unique idempotency and stale-version checks. Persist truthful Succeeded/Failed/OutcomeUnknown; show correction path rather than generic “done.”

A2.3. Only after each domain prerequisite passes, add student onboarding/enrollment, scheduling and teacher operations one at a time. Onboarding requires domain/Identity/audit atomicity and truthful account flags; enrollment requires capacity/admission concurrency; scheduling requires effective teacher/branch/time/room conflicts.

AI-2 exit: the selected action passes same-tenant success, denied-role, foreign-tenant, duplicate, concurrent, stale approval, audit failure, interrupted request and manual/AI parity checks.

### AI-3 — communications and WhatsApp

A3.1. Begin with drafts, templated previews and human handoff. Connect an approved provider only after secure credentials, academy channel configuration, opt-in/opt-out and template policies are verified.

A3.2. Add durable outbox, dispatch-time consent/authorization rechecks, provider receipts, uncertain-outcome reconciliation, rate/bulk limits, quiet hours and escalation to staff. Never call a queued row Sent.

A3.3. Introduce short lead qualification using live course/schedule/fee facts, capture only useful missing details and hand over qualified leads. Test consent changes, provider failure, duplicate delivery, invented price and prompt injection.

### AI-4 — Pulse, proactive but evidence-led

A4.1. Define each signal as a versioned deterministic rule over an authorized projection: missed attendance, expiring enrollment, uncollected invoice, idle lead and capacity pressure are candidates, not assumed-valid metrics. Resolve the underlying status/currency/timezone bugs before a signal can appear.

A4.2. Add per-user and per-academy briefing preferences, freshness windows, deduplication, dismissal, snooze and deep links. Separate an owner's cross-academy command centre from an academy admin's tenant-scoped view; never infer wider rights from a dashboard card.

A4.3. Make every recommendation show source, timestamp, rule and next safe action. Measure precision, missed important events, repeated noise and human dismissals. Exit: real-event and counterexample fixtures demonstrate accurate, authorized, non-duplicated briefings on desktop and mobile.

### AI-5 — scheduling intelligence

A5.1. Expose narrow read projections for availability, capacity, branch/room, modality, teacher load, holidays, recurrence and timezone; repair the current scheduling conflict and lifecycle gaps before using them as authority.

A5.2. Generate multiple candidate moves with the affected students/teacher, travel/room constraints, online meeting link or offline room, and the consequences of each. For make-up classes, offline requires room details; online and hybrid require meeting-link handling as already decided.

A5.3. Revalidate at confirmation inside the deterministic scheduling transaction, including concurrent booking and stale proposal tests. Exit: an AI proposal cannot bypass a conflict the manual scheduler would reject, and a rejected proposal clearly explains why.

### AI-6 — finance, teacher and student intelligence

A6.1. Build fact-backed finance projections for invoice balance, payment/reconciliation, GST document state, refund liability/settlement and currency. Keep all accounting arithmetic, tax classification, payroll and posting in deterministic reviewed code; the model explains, flags anomalies or prepares a review, but cannot independently move money.

A6.2. Add teacher-facing preparation, attendance and compensation explanations from authorized classes and validated payment rules. Add student/guardian progress and practice summaries only from accessible records; distinguish an observed result from a pedagogical inference, and allow teacher correction.

A6.3. Protect minors' notes, recordings and media with object-level rights, consent, retention and safe previews. Evaluate plausible but false totals, missing records, disputed payments and cross-role questions. Exit: finance numbers reconcile to the system of record and teacher/student/guardian views reveal only their permitted fields.

### AI-7 — Autopilot, bounded operations

A7.1. Build a workflow registry specifying trigger, actor/service identity, scoped tenant, allowlisted tool, maximum frequency/amount/recipients, expiry, escalation owner and kill switch. Start with suggest/prepare or a non-destructive task reminder, not broad autonomous account, payment, payroll or bulk-message changes.

A7.2. Use durable trigger/lease, idempotency, transactional outbox where external dispatch is involved, retry with backoff, dead-letter and reconciliation of unknown outcomes. Recheck policy, rights, consent and source freshness at execution time; human approval is required by risk class.

A7.3. Give owners an automation register showing what is on, last result, next run, spend, exceptions and pause control. Exit: replay, concurrent worker, revoked right, changed consent, provider timeout and rollback tests produce one truthful outcome or an explicitly unresolved incident.

### AI-8 — Navigator and lead conversion

A8.1. Connect lead intake only through consented channels and factual academy/course/schedule/fee projections. Qualify with a short, respectful conversation, avoid repeated questions and allow immediate staff handoff or opt-out.

A8.2. Add human-approved campaigns and follow-ups with template/channel controls, quiet hours, recipient deduplication and provider receipt accounting. A generated persuasive message must never invent availability, discounts or outcomes.

A8.3. Define stage transitions and metrics with exact denominators, period/timezone, attribution assumptions and currency; use an experiment holdout before claiming lift. Exit: sample leads can be traced from consent to outcome, and conversion displays agree with underlying records.

### AI-9 — Twin, simulation and grounded knowledge

A9.1. Create versioned, tenant-scoped snapshots and explicit scenario inputs for capacity, staffing, tuition and churn. Deterministic equations calculate outputs; AI may explain assumptions and trade-offs, never silently alter live academy data.

A9.2. Compare a simulation with historical baselines and show ranges/unknowns, sensitivity to assumptions, source age and a clear “simulation, not forecast or approved change” label. Require a separate Executor action and its applicable approval for any resulting live operation.

A9.3. Add policy/document retrieval only for a demonstrated use case, with document ownership, per-recipient authorization, version/expiry, citation and prompt-injection resistance. Start with indexed approved documents; introduce a vector store only when retrieval evaluation shows it is needed. Exit: foreign/private documents and malicious embedded instructions cannot change answers or tool authority.

These later phases may overlap once their own gates pass; their numbering is a product sequence, not permission to ship unsafe dependencies. Each role-specific rollout—owner, admin, teacher, student and guardian—needs its own field-policy matrix, journey tests and sign-off.

### First delivery slices, in order

1. Architecture decision and acceptance fixtures: one focused authority matrix, data-disclosure decision, risk/approval table, and ten representative user journeys. No implementation until the ambiguous security decisions are resolved.
2. Disabled foundation: gateway, synthetic tool, provider fake, execution ledger, budgets and kill switch; prove denial/failure paths against real HTTP and disposable SQL.
3. Read-only admin pilot: one minimal student/batch/schedule question flow and a deterministic briefing card, with source links and mobile/accessibility verification. Real provider and real academy data are enabled only after privacy and operational approval.
4. First low-risk Executor action: prepare a task or draft, preview/confirm/execute/verify with durable audit and unknown-outcome handling. Extend to a second action only after the first passes its full acceptance matrix.
5. Expand role and domain coverage in priority order driven by measured user outcomes and exact domain readiness; do not start AI-3 through AI-9 as one large build.

## 5. Experience and visual-design work

Design the AI interaction contract early, but anchor production visuals to real backend states. The initial workspace includes a global entry point, accessible full workspace, contextual suggestions, owner briefing and task/approval inbox. A result should explain “what I used,” “what I propose,” “what will change,” and “what actually happened.” Support English first and later language/localization only after verified terminology and accessibility. Preserve responsive iOS/Android/desktop and reduced-motion behavior.

Use v0 for premium PENTA command centre/Pulse/Executor/task-inbox visual concepts after the first backend contracts and states are testable; integrate approved code through Codex/Cursor with real permissions and accessibility checks. Use Lovable only for an isolated radical interaction experiment. Keep Framer for the public marketing site after product readiness. Five visual characters are optional expression on one shared platform, not five independent backends; real status drives any animation.

## 6. Quality, safety and measurement

For every capability: requirement examples → deterministic source fixture → allowed and denied role/tenant tests → real HTTP/SQL tests → prompt/tool injection, malformed output and privacy tests → concurrency/fault recovery → frontend/browser/mobile check → cost/latency observation → human acceptance. Maintain versioned golden conversations and evaluate task completion, factual grounding, permission denials, false success, human correction, cost and latency. Never grade success solely by pleasant model prose.

Measure the 80–90% direction using eligible completed business outcomes as the denominator. Report separately: manual, AI-assisted, human-approved AI execution, policy-approved automation, failed/unknown and cancelled. Count one outcome once, not every model call or retry. Preserve manual fallback and ability to switch off a capability.

Deployment of this local branch still requires reviewed GitHub integration, full developer/staging/production gates on the exact release commit, migration and restore evidence, then explicit user authorization. A feature flag can keep AI disabled in a deployed binary, but a flag does not waive existing P0/security release gates.

## 7. Decisions and model/tool routing

Product decisions still needed before the affected capability goes live: AI-provider data retention/residency and student/guardian disclosure; role/field scopes for the first read tools; approval thresholds and separation of duties; background actor expiry/revocation; notification provider/consent/quiet hours; zero-net payroll and void-restoration policy; qualified accountant/tax review for refunds and GST credit notes; simulation assumptions. The existing makeup-mode, certificate eligibility and private 2 GB Blob decisions remain settled.

Current official OpenAI model-selection guidance describes Astra for ambiguous demanding work, GPT-6.1 Sol for complex technical work balancing cost, and Luna for scoped frequent tasks: https://developers.openai.com/api/docs/guides/model-selection . Repository AI_MODEL_ROUTING.md remains the project-specific tool policy. Recommendations below are for the **engineering work**, not hard-coded runtime models for Academy Desk customers.

| Next work | Recommended engineering route | Why / stop condition |
| --- | --- | --- |
| Focused authority/threat/approval architecture design | Astra High; design recorded in PENTA_ARCHITECTURE_DECISION.md | This design step is complete; implementation proof remains pending. Reopen only for a consequential new decision. |
| Completed local slice: AI0-S1 disabled synthetic gateway | Codex GPT-6.1 Sol, High reasoning, Default mode | Bounded API and SQL/HTTP evidence recorded; no live data or provider. |
| Immediate next task: AI0-S2 durable task/execution/approval/usage boundary | Codex GPT-6.1 Sol, High reasoning, Default mode | Add schema and concurrency/fault proof before registering any business write. |
| Disabled gateway, tool registry, SQL ledger, budget controls, first read adapters | Codex GPT-6.1 Sol, High reasoning, Default mode | Difficult security/integration code and tests. Use scoped commits; stop when AI-0 gate passes. |
| Bounded API/UI contracts, browser checks, documentation and repetitive approved components | GPT-6.1 Sol Medium or Cursor Composer 2.5; Luna Medium for tiny mechanical tasks | Preserve expensive reasoning for new authority rules or failing integration tests. |
| Premium AI visual concept after real API states exist | v0 design, then Cursor/Codex production integration | Design is not permission/security implementation. |
| Architecture/security exception or major milestone review | Astra High only when evidence leaves a consequential unresolved decision | Do not repeat the old audit or use Astra for routine QA. |

Recommended sequence from this checkpoint: (1) Sol High AI0-S2 durable state and remaining AI-0 slices alongside Track E; (2) first admin read-only pilot; (3) one approved low-risk Executor action. The focused PENTA architecture decision and local AI0-S1 scaffold are recorded. Re-evaluate models and effort against representative tasks rather than assuming the most expensive model is always best.

## 8. Truthful completion statements

Planning complete means this document is reviewed. Foundation complete means disabled architecture and tests pass. Read-only pilot complete means one real role-scoped user journey passes. Mutation ready is operation-specific. Autopilot ready and financial-AI ready have their own evidence gates. “AI-first product complete” requires outcomes, trust and adoption evidence; it cannot be inferred from a chatbot page, passing unit tests or an Azure deployment.
