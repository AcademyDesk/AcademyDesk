# ADR PENTA-002 — conversational operating layer

Date: 2026-10-04; local Mini addendum 2026-10-07. Status: **FIRST LOCAL READ SLICE IMPLEMENTED; FULL DESIGN / PRODUCTION NOT RELEASE-ACCEPTED.**

Source baseline: `f8389a77ff731ebc5a095f42a1d72d2671f22e18`, `codex/penta-search`, `D:\AcademyDesk-codex-p0`, plus the preserved uncommitted read-workspace prototype. This decision supersedes single-query/domain-selector UX and mandatory external-design checkpoints, not the authority, execution or release boundaries in [PENTA-001](PENTA_ARCHITECTURE_DECISION.md). Read with the [requirement gap map](QA/REPORTS/PENTA_CONVERSATIONAL_GAP_MAP.md), [product roadmap](ACADEMY_DESK_PRODUCT_ROADMAP.md) and [enterprise gates](ENTERPRISE_TESTING_ROADMAP.md).

The owner requires a genuine, grounded, multi-turn assistant beyond CRUD. The 31-section conversational directive is traced individually in the gap map. Product direction is approved; proposed schemas, limits and runtime/privacy choices below are implementation specifications or open decisions, not deployed capabilities.

Implementation addendum: [C1a](QA/REPORTS/PENTA_C1A_PRIVATE_CONVERSATION_FOUNDATION.md) now provides three scoped SQL tables, private synthetic history, atomic context versions and replay under an independent Testing-only flag. Only code-owned synthetic strings are accepted; general messages, record referents, model orchestration and production history remain unimplemented. C1 is not closed; next is C1b verified references, bounded context and typed artifacts. Preserve the design below as the complete target rather than claiming every proposed field/behavior exists.

## PENTA Mini local integration — 2026-10-07

The authoritative engine handoff is `D:\PENTA AI Models\HANDOFF_TO_ACADEMY_DESK.md`, engine Git checkpoint `2bf76b54054ba610071e3435c94cf36bedff3a99`. Its existing `/v1/chat` protocol **0.1**, service **0.1.0**, prompt **planner-0.1**, pinned **Qwen3.5-2B Q4_0** are reused unchanged. No engine source, runtime or weights are copied here. Older provider candidates remain disabled; this adapter does not call Azure/OpenAI/external inference.

The browser sends only a prompt, request ID, expected conversation version and capability label to Academy Desk. `PentaChatController` delegates to `PentaMiniOrchestrator`, central `IPentaProvider` / `PentaMiniProvider`, fixed `PentaMiniTools` registry and thin `AcademyDeskConnector`. Mini receives bounded structured state and a **tool-name array**, not invented tool definitions, unlimited history, SQL or authorization hints. Its prose, IDs, state and risk are proposals, never authority. Only `SearchLearners` and `GetLearner` are dispatched; both are host-classified READ. All other tool names/arguments fail closed.

Current live Identity role/academy and Finance subscription are checked before claim and again after planning. SQL stores private actor/academy-scoped sessions and request claims/receipts; raw prompts are not persisted. State, receipts and input digests use purpose-bound Data Protection, scoped composite FKs, 24-hour access expiry, version checks and mandatory atomic audit. A durable claim is committed before inference; no SQL transaction waits for the model. Unknown/cancelled outcomes remain pending and cannot silently redispatch. A replay returns a saved receipt only for the current version and currently authorized actor. Persistent production key-ring/retention/purge policies remain separate release gates.

`OutstandingFeesService` is shared with manual invoice reads. Outstanding is invoice total minus adjusted amount minus **Completed + Reconciled** own-academy payments; Pending/Voided payments are excluded. Cancelled invoices and inactive students are excluded from search. Course filtering selects active enrolled students, **not course-attributed invoice portions**. Duplicate names keep separate source IDs; duplicate enrollments do not multiply amounts. SQL performs count/order/page/aggregation under a consistent transaction. Ten source rows maximum, stable tie-breaking; mixed-currency ranking, invalid currency and corrupt/overcollected ledger fail closed. Selecting a displayed record updates the trusted displayed set to that single record; ordinal references cannot obtain authority from frontend/model-supplied IDs.

The prompt-first `PENTA AI | Workspace` surface renders source-linked typed cards, as-of/scope, progress/errors, clickable `!` help and contextual/manual rail. All five labels share one conversation; only this read slice is active. Switching to Manual keeps the same mounted page/drafts and does not send drafts to Mini. Navigating/reloading starts a new in-memory transcript; production resume/history, five full capabilities, mutations, training, Autopilot and cloud deployment are not delivered here.

Metadata learning hooks mirror `penta_mini.learning.PentaLearningEvent`: tenant-scoped IDs, expected revision, prompt/protocol versions, event/failure classifications, empty private argument maps and `training_eligible=false`. No training or feedback export occurs. The API does not attest runtime model identity; audit marks the handoff revision as expected, not verified per-request. See [integration evidence](QA/REPORTS/PENTA_MINI_FIRST_READ_INTEGRATION.md). All broader architecture acceptance cases remain applicable to later increments; this addendum does not close enterprise or production gates.

## 1. Product and interaction contract

The header switch reads **PENTA AI | Workspace**. One assistant can invoke Pulse, Executor, Navigator, Twin and Autopilot in the same conversation. Capability selection supplies an intent preference, not another thread, another identity or permission to execute. Executor opens the chat/composer immediately. There is no mandatory search-domain selector or rigid step-by-step form.

The five capability labels have accessible, clickable `!` help: a short description on click/touch/keyboard, with focus return and Escape dismissal. Pulse explains events and priorities; Executor prepares permitted operations; Navigator supports admissions and growth; Twin explains academy patterns and scenarios; Autopilot manages explicitly approved automation. These are capabilities, not five separately trained models.

The conversation is the primary canvas. Answers can contain concise text, source-linked record cards, bounded tables, deterministic charts, schedule previews, before/after comparisons and approval cards. A collapsible right rail provides verified academy/page/record context, sources, pending proposals and actual progress. Essential answers and confirmations remain usable in chat when the rail is closed or stacked on mobile.

Manual Workspace remains a first-class, permission-filtered directory of operations and exception handling. Switching modes must preserve unsaved manual work without uploading it to a model. Navigating away needs a draft/unsaved-change policy; the prototype's hide-not-unmount behavior only protects the same mounted page. PENTA failure or budget exhaustion must not disable the ERP.

## 2. Verified starting point — reuse rather than restart

| Existing component | What is available | What it does not establish |
| --- | --- | --- |
| [PentaFoundation](apps/api/Intelligence/Penta/PentaFoundation.cs), [PentaController](apps/api/Controllers/PentaController.cs) | Default-off local pilot, current active Owner/Admin, own active academy; synthetic turn route and narrow reads | Real natural-language reasoning, conversational storage, finance-tool authority or eligibility of other portals |
| [PentaExecution entities](apps/api/Domain/Entities/PentaExecution.cs) | Tasks, executions, approvals, usage reservations/entries and attempts | Conversations/messages; prompts are deliberately not persisted in the existing ledger |
| [Student search](apps/api/Intelligence/Penta/PentaStudentSearchService.cs), [batch search](apps/api/Intelligence/Penta/PentaBatchSearchService.cs) | `student.search.v1`, `batch.search.v1`; audited, active-only, minimal fields, ten rows plus `hasMore` | Financial balances, course filtering, availability, total roster counts or multi-turn referents |
| [Synthetic probe coordinator](apps/api/Intelligence/Penta/PentaProbeCoordinator.cs), [candidate adapter](apps/api/Intelligence/Penta/PentaOpenAiProbeAdapter.cs) | Internal, unrouted, budget-bound synthetic coordination; candidate transport tested with fake responses | User/academy-data model calls, selected live model, provider/privacy acceptance or conversational gateway |
| [Local workspace prototype](apps/web/src/components/penta/penta-workspace.tsx), [shell](apps/web/src/components/enterprise-shell.tsx) | Existing-session read UI, manual fallback, source links, responsive scaffolding | Accepted prompt-first chat, durable history, contextual refinements or enabled five-capability behavior |

Retain the existing bounded tests and historical enterprise audit. A synthetic turn or lookup screen is not a real conversational AI feature. The prototype remains useful scaffolding; it is not the final product interaction.

## 3. Conversation, message and turn model

Proposed entities belong in the existing application SQL database, with explicit migrations and academy-scoped composite foreign keys. Do not introduce another ERP backend or authoritative store.

- **Conversation:** academy ID, initiating actor ID, opaque conversation ID, bounded title, status, created/updated/expiry timestamps, retention-policy version and concurrency version. Private to the initiating actor initially; no academy-wide history sharing by default.
- **Message:** academy/conversation IDs, sequence, author/role, bounded content, creation time, sensitivity classification and links to typed artifacts/tool receipts. Store only permitted content under an approved protection/retention policy. A tool result is data, never a system instruction. Model text cannot declare its own trusted role.
- **ConversationTurn:** academy/conversation/actor IDs, client request ID, expected context version, state, started/completed timestamps, safe error code and existing task/execution references. Enforce a tenant-scoped unique request identity. Same request/content replay returns the prior receipt; different content under the same identity conflicts.
- **Context checkpoint:** versioned server-owned resolved references, query criteria, visible result identities/order, explicit date/currency/timezone assumptions and pending workflow/proposal IDs. A summary is a navigation aid, not authoritative academy data.
- **Artifact:** typed versioned projection, source/tool IDs, as-of time, safe record destinations and result/proposal reference. Fields and destinations are code-owned and permission-filtered; no executable HTML or arbitrary model-produced links.

These are the **target schemas**, not a claim that every field/behavior exists. C1a implements only synthetic conversation/turn/message storage; context checkpoints, protected free text, rich artifacts and execution references remain pending. Do not repurpose diagnostic `InputDigest` or synthetic proposal payloads as hidden conversation storage. One future conversation may reference several existing executions; conversation completion does not imply every operation succeeded.

Serialize accepted context updates per conversation. Two tabs submitting against the same context version must not silently overwrite each other's referents: reject the stale request with a recoverable conflict and offer refresh/retry. Recover leases conservatively; an interrupted turn with uncertain tool effects stays unknown until reconciled, never blindly redispatched.

## 4. Context and memory

Construct each turn from code-owned instructions, current verified actor/academy authority, a bounded recent-message window, validated structured state and only the source projections needed for that request. Do not resend the entire conversation, roster, invoice list or unrelated page form.

“Them”, “that student”, “the second result” and “same teacher” resolve against explicit visible result/proposal references. If two candidates remain, ask one focused question with minimal permitted identifying details. Names alone are not record identity. An ordinal refers to the displayed result order, not a new database query's accidental order.

“Tomorrow” and “next week” use the verified academy timezone, with an explicit date/range in the answer or proposal. If the academy timezone is absent/invalid, clarify rather than infer the laptop timezone. Never silently reuse a stale balance, permission or capacity value.

Retain query criteria plus displayed IDs/order/as-of, not unlimited result dumps. Refinements requery the **whole authorized source population** using those criteria, not just the first ten displayed rows. If an earlier item changed or disappeared, explain and refresh rather than silently substitute a similarly named record. Pagination needs deterministic sorting and a scope-bound continuation token; it is not a claim of a transactionally frozen snapshot.

Short-term context supports continuity. SQL records and versioned approved academy knowledge supply long-term facts. Preferences, durable memory, user corrections and operational records have different retention/authority rules. No inference about a student becomes a permanent fact without an explicit governed domain operation.

## 5. Orchestration and runtime routing

Flow: authenticate → authorize conversation and page context → construct bounded context → select eligible capability/tools → reserve budget → invoke approved runtime if needed → validate typed intent → execute permitted reads or prepare a proposal → render source-backed artifacts → record actual outcome.

The model interprets natural language and may explain tool outputs. Server code chooses authority, validates schemas, resolves records, computes financial/scheduling results and executes operations. A model response cannot introduce SQL, another controller, arbitrary URLs, new permissions, model keys or automation code.

Define a replaceable conversational runtime interface separate from the fixed synthetic probe adapter. Initial real pilot uses one explicitly approved runtime/model. A capability switch does not trigger five model calls. Deterministic navigation/status and already verified calculations need no language-model call where unnecessary. More sophisticated task routing follows measured evaluation, not assumptions that a bigger model always fixes correctness.

Proposed initial engineering ceilings: user message at most 4,000 characters, at most three tool calls and five orchestration steps per turn, finite context/output tokens and a finite turn deadline. Exact token/deadline values depend on runtime evaluation and remain configurable decisions. Existing diagnostic/probe limits are not silently changed. Exceeding a ceiling yields a truthful partial/clarification outcome, not recursive autonomous work.

Model/provider selection, residency, student/minor-data treatment, retention, licensing, cost and failure handling remain open. Evaluate private/self-hosted pretrained models on synthetic academy conversations if the owner wants no hosted model service. Owning PENTA orchestration does not mean owning pretrained weights or having trained a foundation model. No download, provider enablement or customer-data call is authorized by this document. No fallback to another provider/region without approval.

## 6. Tools, shared domain operations and risk

Use a code-owned registry of versioned, typed tools. Each tool declares allowed roles/modules, same-academy projection, purpose/fields, risk, audit, limits and execution semantics. Existing tool IDs remain as implemented; historical candidate names are not proof of installed tools.

CRUD is one part of the tool catalog. Also plan scheduling, attendance, fee analysis, reminders, learning/media, leads, reporting, explanations and workflow automation. Each domain becomes available only after its own source, authority and invariant gates. No UI card should imply an unavailable operation works.

Manual and AI entry points call the same protected domain service. Collect only missing required fields, accept several supplied fields at once, validate corrections and omit optional fields unless useful. Sensitive credentials should use a secure non-chat control and must not enter model context or saved transcripts. Do not turn every operation into a long mandatory questionnaire.

Duplicate policy must distinguish database uniqueness from similarity. Enforce defined unique identifiers transactionally; handle racing creates with the existing safe validation response. Similar names/shared family contacts trigger clarification, not automatic merges or claims of duplicates. Update/delete require current record/version and shared lifecycle rules.

| Risk | Conversation behavior | Release boundary |
| --- | --- | --- |
| R0 permitted read | Execute without an action confirmation; show source/as-of and scope | Current authorization, minimal fields, bounded queries and mandatory read audit |
| R1 low-impact operation | Collect details → show exact proposal → explicit confirmation → execute once | Shared domain adapter, idempotency, revocation, fault/audit tests; synthetic approval is not a real action |
| R2 scheduling/enrollment/bulk/external effect | Show conflicts, scope, affected records and current-state preview; require confirmation | Source-specific concurrency, duplicates, capacity, notification/outbox and recovery gates |
| R3 finance/security/destructive effect | No unrestricted agent execution; specialist deterministic policy and explicit approval | Finance/security P0 dispositions and separately accepted approval/execution contracts |

Approval binds actor, academy, tool/version, normalized arguments, target identities, relevant record versions, policy version, scope and expiry. A typed “yes” can only request confirmation of the single displayed, unexpired proposal; the server still validates it. If there are multiple proposals or ambiguous replies, clarify. Changing details invalidates old approval. Never treat model-generated confirmation text as the user's approval.

Recheck authority and relevant data inside the execution boundary. A domain write and required audit/execution receipt commit atomically where applicable. External effects use the existing accepted outbox/delivery contract when available; “queued” is not “sent”. Unknown outcomes block automatic retries. Cancellation only cancels unstarted work; undo is a separately authorized operation, not a promise to reverse an already committed effect.

## 7. First useful read journey — dues with conversational refinement

Acceptance conversation:

1. “Show students with pending fees.”
2. “Only piano students.”
3. “Sort highest due first.”
4. “Open Ananya.” If more than one relevant Ananya exists, clarify before selecting a Student 360 destination.

Neither existing name-search tool supplies this dataset. Add a narrowly projected, same-academy **student-dues read tool** with an explicit Finance entitlement check and approved course/enrollment projection. The PENTA controller's general pilot eligibility is not sufficient financial authority.

Proposed metric contract, requiring shared deterministic implementation and tests:

- Pending means positive outstanding balance on eligible non-cancelled invoices, not only overdue invoices and not an unchecked invoice `Status` label.
- Collected amount uses same-invoice, same-academy **Completed plus Reconciled** payment rows. Net collectible uses `TotalAmount - AdjustedAmount`; do not also subtract adjustment records already reflected in that snapshot. Unexpected statuses/currency mismatches/overcollection are explicit anomalies, not facts normalized away.
- Aggregate once per student and currency; use existence filtering for enrollments so multiple courses/enrollments cannot multiply invoice totals. Do not add INR and another currency or rank them as one comparable amount without an approved conversion basis. Ask for currency when necessary.
- “Piano students” initially means active students with an Active enrollment in a resolved piano course, intersected with the previous dues criteria. Course names/subjects must resolve to verified IDs; ambiguity is clarified. State this active-enrollment scope, and allow an explicitly requested permitted historical scope later.
- Current invoices link to students/fee plans, not courses/batches. The output is **those piano students' total outstanding fees**, not “fees owed for piano”. Course-attributed balances need another separately designed accounting relationship.
- Compute filters/sorts/authorized total count over the source, not a ten-row sample. Return at most ten minimal student/currency/balance records per page, source/policy version, timezone, as-of, scope, deterministic order and `hasMore`; totals/counts only when correctly computed under the same scope. Do not expose guardian/medical/contact/free-text records for this question.
- Every result/empty response needs current authorization and required audit. Missing facts produce a useful explanation, not a fictional zero balance. The final destination is an existing verified Student 360 path; opening is navigation, not mutation.

Source concern to resolve: [InvoicesController](apps/api/Controllers/InvoicesController.cs) currently counts non-Voided payments, while [PaymentsController](apps/api/Controllers/PaymentsController.cs) and [FeeRemindersController](apps/api/Controllers/FeeRemindersController.cs) explicitly use Completed/Reconciled. [AdminIntelligenceController](apps/api/Controllers/AdminIntelligenceController.cs) also uses invoice status/total for some summaries. These are not automatically equivalent authoritative answers. This inspection does not reproduce or close existing finance issues; define and test a shared read calculation before exposing dues in PENTA.

## 8. Five capabilities, one conversation

- **Pulse:** a verified briefing can be drilled into, prioritized or dismissed conversationally; retain the same source/task references. Notifications and due dates are facts, prioritization is labeled guidance.
- **Executor:** moves from a scoped read to a governed multi-step proposal without losing identities/constraints. Example enrollment transfer remains blocked until duplicate, eligibility, capacity and concurrent-transfer invariants are verified in shared services. The current controller's check-then-save flow is not proof of those guarantees.
- **Navigator:** uses permitted lead/conversion/history projections; can explain opportunities and prepare follow-ups. Predicted conversion/priorities are uncertainty-labeled recommendations, never invented historical outcomes or consent to send.
- **Twin:** reasons over permitted evidence and deterministic metrics. Explanations distinguish observed correlation, missing data, hypotheses and scenarios. No claims of causation or guaranteed revenue from an incomplete roster.
- **Autopilot:** conversation prepares a structured trigger/condition/action proposal with recipients, limits, expiry, run window, review/disable controls and approval. Compile only an allowlisted declarative schema; never execute prompt-generated scripts, arbitrary cron/SQL or uncontrolled agent loops. Each future run rechecks authority and captures its own outcome.

Capability handoff changes context purpose and tool eligibility, not conversation ownership. A new purpose may require another minimal projection and permission check; do not reuse sensitive data merely because it appeared earlier in the thread.

## 9. API, streaming and frontend integration

Proposed routes under the existing authenticated `/api/academies/{academyId}/penta` boundary: create/list private conversations; read a conversation's permitted messages; submit a turn with request ID/context version; retrieve turn status/artifacts; consume status events; rename/delete history under policy. Define DTOs and denial behavior before adding routes. Existing synthetic `/turns` and draft-confirm routes are not silently promoted into production conversational APIs.

Accepted submission may return a turn receipt with bounded polling initially. Streaming later uses authenticated fetch with the current bearer-session renewal/cancellation pattern, not tokens in URLs or unauthenticated EventSource. The static-export Next.js application does not require another Node backend to support this; the current API can own the bounded stream.

Emit real states: received, resolving context, reading a named permitted source, awaiting clarification/approval, completed, failed, outcome unknown. Do not display fake progress, expose hidden reasoning, or show success while a tool is still pending. Streamed narrative is provisional until supported by the final source/artifact receipt; approval/execution results come from authoritative stored outcomes.

Events have scoped monotonic IDs and safe payloads. Reconnect/poll requires authorization and a bounded cursor; a replay never re-executes an action. Stop streaming on logout/revocation/expired scope. Partial responses, cancelled requests, network loss and stale tabs need visible retry/resume controls with idempotency preserved.

Frontend shell: conversation list/new conversation; continuous transcript; prompt composer; five capability controls/help; optional context rail; private-history actions; Workspace fallback. Use existing theme, session, layout and UI primitives. Retain useful prototype source cards but remove mandatory domain-first search. Escape text; allowlist rich artifact renderers and route destinations. No model-provided HTML or unsafe Markdown URLs.

Validate keyboard/focus, screen-reader status announcements without token-by-token noise, reduced motion, contrast, light/dark, virtual keyboard/safe areas and 320px+ responsive layouts. Desktop rail collapses; mobile context opens as an accessible sheet. No device certification from viewport emulation alone.

## 10. Privacy, history, security and audit

History is intentional and private: New, Recent, Continue, Rename and Search only within current authorized ownership. Search is permission-scoped; safe title generation avoids private contact/medical/financial detail and arbitrary academy text. History deletion is separate from operational audit retention and domain deletion; explain the distinction.

Production retention duration, legal basis/notice, sensitive-data exclusions, encryption/key lifecycle, deletion/backup policy and access-revocation behavior are **OPEN decisions**. No production transcript persistence until these are settled. C1 uses disposable synthetic data only, with explicit test cleanup; no auto-enable of browser localStorage transcripts, secrets, unrestricted embeddings or provider history.

Role/module/tenant revocation invalidates conversation/context/artifact access immediately. Old free text may contain formerly permitted data: do not merely remove a card while replaying sensitive text. Deny affected history conservatively until an accepted masking/redaction policy exists. Revalidate record deep links and context after navigation or switching academies. Initial conversations are not shareable across users.

Prompt injection defenses are architectural: untrusted prompts, record names/notes, uploaded documents and tool results cannot override code-owned tool schemas, tenant scope, grants or approval requirements. Unknown properties/tool names, invalid links, forged sources and excessive outputs fail closed. Uploaded private-media access remains separately scoped; no transcript attachment automatically becomes public/model-visible.

Audit stores bounded identifiers, tool/policy version, source scope, outcome and correlation—not raw prompts, responses, credentials or unnecessary student names. Required read audit must succeed before exposing the read. Consequential action audit must not claim success when the transaction rolls back. Protect transcript payloads separately; hashes alone are not anonymization for predictable personal inputs.

Use existing auth/tenant middleware and active-user checks on every route, then explicit action/module permission inside each tool. Current local pilot excludes Platform Owner, Teacher, Student and Guardian; expansion needs its own role contracts. An Owner/Admin pilot check cannot grant access to disabled Finance or other modules. Academy ID from text/client context is never trusted scope.

## 11. Cost and error recovery

Reuse budget reservations/entries/execution recovery rather than inventing an untracked chat quota. Reserve a versioned worst-case allowance before model dispatch, enforce user/academy caps, reconcile observed usage once, retain conservative reservations on uncertain usage and record actual tool/model outcomes. Tool/database work also needs time/row/concurrency limits. History search and summarization must not bypass caps.

Use bounded context, minimal projections, cached permitted non-sensitive metadata only when freshness allows, and deterministic calculations. Do not cache private artifacts globally or across tenants. Record measured latency, call counts, tokens/cost and useful-outcome rate with no prompt-content telemetry.

Recoverable errors explain the actual next action: clarify identity, refresh changed capacity, revise invalid fields, renew session, wait for quota, retry a read, or inspect a pending receipt. A safe correlation reference replaces stack traces and secret-bearing exceptions. Never report a successful create/update/send from model prose without a durable domain receipt. Preserve entered conversation details when a validation error is recoverable and privacy permits.

## 12. Implementation packets and acceptance

These packets extend existing AI0/AI1 work; they do not restart Phase 1 or replace the enterprise remediation queue.

| Packet | Deliverable | Model recommendation | Exit |
| --- | --- | --- | --- |
| C0 — this checkpoint | Architecture, 31-requirement gap map, coherent handoff/roadmap | Sol High | Documentation consistency and source linkage only |
| C1 — next | Protected conversation/turn/context contracts, private ownership, versioned references, typed artifacts; synthetic runtime tests only | **Sol High**: authority, storage, concurrency | Targeted DTO/state/role/tenant/replay/revocation/audit/fault tests plus guarded disposable SQL; no real model/domain write |
| C2 | Shared deterministic dues projection and finance/module boundary, course-resolution/filter/sort/paging tools | Sol High | Calculations/status/currency/duplicate-join/source parity and real SQL; no financial write or P0 closure |
| C3 | Approved runtime choice and bounded conversational adapter/orchestration | Sol High; optional Astra focused review only if unresolved | Synthetic golden conversations, malformed/injection/ambiguity/cost/unknown-outcome checks; explicit privacy/runtime enablement decision |
| C4 | Prompt-first PENTA AI shell and the full four-turn read journey using C1–C3 | Sol High for integration; Sol Medium for bounded styling after contracts settle | Actual source-backed conversation, manual fallback, browser/SQL/accessibility/light/dark/mobile; physical-device checks remain explicit |
| C5 | First accepted low-impact domain action and then enrollment/scheduling workflows | Sol High | Shared manual/AI rules, preview/approval/current-state/idempotency/transaction and recovery proof; R2/R3 gates separately |
| C6 onward | Pulse, Navigator, Twin and bounded Autopilot conversational journeys | Sol High for invariants; Sol Medium for defined UI work | Domain-by-domain truthful vertical acceptance, not enabling all five from one flag |

C1 can proceed locally without choosing/installing a live runtime or settling production transcript retention; it must not enable production storage. C2/C4 can use deterministic synthetic fixtures for development, clearly labeled. **End-to-end natural-language feature acceptance requires the actual approved runtime**, not a canned response mistaken for intelligence. Owner runtime/privacy decisions gate C3 live enablement, not the entire local program.

Minimum read acceptance cases: exact four-turn journey; duplicate Ananya clarification; ordinal reference after sorting; query refinement over more than ten records; changed/deleted record; mixed currency; cancelled/adjusted/reconciled invoice; cross-tenant record/proposal/cursor; role/module revoked during turn and history replay; prompt injection in request/source; audit failure; duplicate/stale concurrent turn; provider/network/budget failure; desktop/mobile/manual draft preservation. Preserve all historical evidence; add focused tests, not another full declaration audit.

First enrollment transfer must additionally prove capacity and duplicate races, inactive/foreign source/target denial, prerequisites, lifecycle history, stale proposal expiry, audit rollback and exact-once outcome. Existing [EnrollmentsController](apps/api/Controllers/EnrollmentsController.cs) does not on its own prove these gates. [FeeRemindersController](apps/api/Controllers/FeeRemindersController.cs) queues notifications; delivery requires separate provider/outbox evidence.

No P0 issue is closed by this architecture. No application change, migration, model installation/call, feature commit/push, main merge, production database access or Azure deployment occurs in C0. The existing enterprise release program and human merge/Azure checkpoints still apply.
