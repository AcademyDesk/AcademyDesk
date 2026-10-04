# ADR PENTA-001 — shared authority, context and execution

Date: 2026-10-04. Design baseline: `9265e72` on `codex/enterprise-p0-continuation`, `D:\AcademyDesk-codex-p0`.

Status: architecture selected for the disabled local foundation. Naming is owner-confirmed. AI0-S1, S2a, synthetic-only S2b, internal-only priced-budget/recovery S2c and synthetic approval S3a are locally implemented and tested; a real domain action, provider/privacy selection, broader security acceptance and live enablement are pending. See [AI0-S1 evidence](QA/REPORTS/PENTA_AI0_S1_FOUNDATION.md), [AI0-S2a evidence](QA/REPORTS/PENTA_AI0_S2A_EXECUTION_LEDGER.md), [AI0-S2b evidence](QA/REPORTS/PENTA_AI0_S2B_SYNTHETIC_QUOTA.md), [AI0-S2c evidence](QA/REPORTS/PENTA_AI0_S2C_PRICED_BUDGET_RECOVERY.md) and [AI0-S3a evidence](QA/REPORTS/PENTA_AI0_S3A_SYNTHETIC_DRAFT_APPROVAL.md). This decision is not release certification or approval to disclose academy data to an external model.

Read with [product vision](AI_PRODUCT_VISION.md), [delivery plan](ACADEMY_DESK_AI_IMPLEMENTATION_PLAN.md), [existing enterprise roadmap](ENTERPRISE_TESTING_ROADMAP.md) and [release gates](QA/09_RELEASE_GATES.md). Historical audit results remain evidence; this adds the PENTA boundary and its tests to the existing program.

## 1. Product decision and ownership

**Academy Desk PENTA — Five AI systems working as one.** Canonical capability IDs are `pulse`, `executor`, `navigator`, `twin`, `autopilot`; display names are Pulse, Executor, Navigator, Twin, Autopilot. Operator and Growth in historical AI documents are aliases for Executor and Navigator. Existing subscription plans, business module identifiers and old reports are not renamed.

| Experience | Owns | Receives help from / hands off to |
| --- | --- | --- |
| Pulse | Factual signals, alerts, morning briefing and prioritization of attention | Twin supplies authorized explanations; Navigator suggests a response; Executor offers an action preview. |
| Executor | User-requested search, operational assistance, drafts, previews and approved execution | Other experiences can propose tasks. Executor alone dispatches business tools through the common execution policy. |
| Navigator | Admissions, lead qualification, conversion, capacity opportunities and strategic next steps | Twin supplies scenarios/evidence; Executor prepares an approved operational or communication action. |
| Twin | Scoped academy context, patterns, source-backed explanations and what-if scenarios | Serves the other experiences through authorized facts; simulations never become actual records implicitly. |
| Autopilot | Explicitly enabled recurring workflows, run status, exceptions, pause and escalation | Dispatches the same Executor tool contracts using bounded workflow authority. It cannot invent or expand its mandate. |

All AI features map to these five. Scheduling, learning, finance, payroll and WhatsApp are domains/channels used by PENTA, not additional unrestricted agents. Navigator can answer broader opportunity questions than the old Growth label. Twin is a contextual and analytical view over existing facts, not a replacement operational database.

One user task has one task identity, academy, initiating actor, conversation and cost budget across handoffs. A capability name is a routing/display attribute, never a role, permission or independent identity. One orchestrator normally selects one primary capability and bounded tools; it need not call five models for each question. Users can select a capability or let the server route the intent; the policy decision is unchanged.

## 2. Source findings that determine the design

| Inspected source | Consequence for PENTA |
| --- | --- |
| `apps/api/Program.cs` | Reuse current Identity bearer and live account/stamp check. Two EF contexts use the configured SQL store. Rate limiting currently precedes authentication, so the existing global limiter is not proof of an authenticated per-user AI budget. Add PENTA limits after identity validation. Shared production Data Protection remains a deployment prerequisite. |
| `apps/api/Security/AcademyAccessFilter.cs` | Academy checks depend on an `academyId` argument. Platform owners bypass this filter's normal tenant/audit path. Controller permissions do not distinguish every action or field. PENTA therefore needs an explicit service policy even when this filter has passed. |
| `apps/api/Security/PermissionCatalog.cs` and `SubscriptionPlanCatalog.cs` | Reuse actual role/grant/module semantics; do not invent a prompt-only `ai.admin` entitlement or let Core subscription access imply Finance access. Resolve each tool's underlying permission/module explicitly. |
| `apps/api/Controllers/StudentsController.cs` | Current list returns email/phone and is unbounded. First PENTA search needs a separate minimal, paged projection; returning the raw list to a model is not acceptable. |
| `apps/api/Controllers/BatchesController.cs` and `ClassSessionsController.cs` | Batch summaries include admin notes/meeting links. Schedule storage/read facts do not certify availability or conflict-free booking. Project only approved fields; booking requires the existing scheduling repair gates. |
| `apps/api/Security/AcademyIdentityTransaction.cs` | Selected linked mutations can enlist Identity in a same-store domain transaction. This is not a universal boundary; PENTA adapters must prove their own domain/Identity/audit atomicity. |
| `tests/AcademyDesk.Api.Tests/Infrastructure/QaApiFactory.cs` and `QA/tools/SqlHarness` | Extend the guarded fixtures and real HTTP/disposable SQL approach. Do not substitute InMemory results for SQL uniqueness, rollback or concurrency evidence. |

These are current source observations, not a claim of production behavior or new reproductions of every legacy issue. The existing readiness report's old race findings have later repair reports; those repairs are retained.

## 3. Shared architecture and deployment shape

```text
Academy Desk UI / approved communication channel
  -> authenticated PENTA gateway
  -> current actor + academy + capability policy
  -> bounded intent planner / configured model adapter
  -> typed tool proposal and result projection
  -> server validation + preparation + approval if required
  -> protected Executor operation or read projection
  -> current domain rules / SQL / durable outbound message
  -> verified outcome, source links, audit and usage record

Pulse, Navigator and Twin supply scoped facts or proposals.
Autopilot supplies an approved workflow mandate and run identity.
Neither path skips the same policy and execution boundary.
```

Use a modular area of the existing ASP.NET Core API (`apps/api/Intelligence/Penta/`) and the existing application SQL context for PENTA records. Keep user/role authority in Identity. The static-export Next.js frontend calls the API; provider keys and orchestration stay server-side. Start with bounded responses and status polling. Defer streaming, separate services, embeddings/vector storage and new infrastructure until a measured need justifies them.

The gateway may access only code-registered operations. It exposes no arbitrary URL fetch, controller invocation, SQL, filesystem, shell or dynamically loaded tool. The model selects from the authorized registry; server validation remains mandatory even if a provider claims its output matches a schema. PENTA-to-PENTA handoffs are ordinary server task transitions, not a second route to greater authority.

For new read tools, implement scoped projections directly behind an explicit policy. For existing writes, extract a single tested application operation used by both manual API and PENTA when the write is enabled. Do not copy business rules into prompts or invoke a controller instance without its authorization filters. Existing protected HTTP APIs may be used only where the full pipeline, attribution, idempotency and outcome reconciliation are proven; they are not the default shortcut for SQL-atomic writes.

## 4. Actor, academy and permission contract

Create an immutable request-scoped context from authenticated server data: `ActorUserId`, `AcademyId`, current account state, live role/custom-role/grant resolution, enabled modules, relevant linked teacher/student/guardian identity, policy version and correlation ID. The route academy is a requested target, not trusted authority. Resolve and validate it before fetching business data or invoking a model. Body/model-supplied actor, role, scope or approval fields cannot override the context.

| Actor | First enabled read pilot | Later expansion condition |
| --- | --- | --- |
| Academy Owner / AcademyAdmin, active and bound to this academy | Allow only pilot academy, approved tool and minimal fields | Each mutation needs its own invariant and approval acceptance. |
| Staff/system/custom roles or independent access grants | Deny initially | Explicit tool/action/field permission parity tests; possession of one broad module grant is insufficient for every tool. |
| Teacher | Deny initially | Active teacher/account, academy/module and actual class assignment; no unrelated students, staff payroll or guardian-private notes. |
| Student / Guardian | Deny initially | Current self/relationship and field-level policy, including guardian revocation and learner privacy. |
| Platform owner | Deny academy PENTA initially, even if legacy filter bypasses | Separate platform tools, explicit academy selection/delegation, reason and audit. Cross-academy aggregate tools must be independently approved. |
| Background worker | No interactive-user impersonation | Autopilot mandate described below; no stored human bearer token or universal owner credential. |

This intentionally narrows the first pilot; it does not remove existing manual access. Denied roles see the manual workspace or a clear unavailable capability rather than a permission request loop.

Policy requires all of: feature/capability enabled, eligible actor, active academy, exact academy binding, required current permissions, subscription modules, record relationships, permitted fields and purpose, and budget. Any unrecognized role/tool/version/permission requirement denies. Apply the same policy to conversation history, saved results, approvals, execution status, media and exports, not just the initial tool call. Resource IDs from another academy produce no details or existence clues.

Re-read authorization when preparing, approving, claiming execution, dispatching external work and viewing results. Reads use current authorization at dispatch; cached history is not a permanent access grant. Define revocation honestly: revocation observed before claim/dispatch blocks work, while an already committed or in-flight external effect cannot be retroactively cancelled. Before any sensitive write is enabled, its adapter must specify and test the transaction/locking boundary for authorization versus execution, including simultaneous revocation; unresolved races keep that tool disabled.

## 5. Initial tools and data exposure

| Contract | Minimal input/output | Authority and exclusions |
| --- | --- | --- |
| `diagnostic.synthetic.v1` (foundation only) | Fixed synthetic request/result; no echoed arbitrary content, PII, URLs or domain queries | Development/Testing registration only; eligible local synthetic admin. Never a production/live tool. |
| `students.search.v1` (AI-1 candidate) | Name fragment; at most 20 matches per page; ID, display name, active state and authorized app link | Same-academy admin pilot; `students.manage` semantics and Core. Exclude DOB, phone, email, address, fees, notes and guardian data. Disambiguate before later writes. |
| `batches.lookup.v1` (AI-1 candidate) | Bounded name/ID lookup; name, course, modality and recorded status | `batches.manage` semantics and Core. Exclude internal notes and meeting credentials; do not claim a seat is reservable. |
| `schedule.list.v1` (AI-1 candidate) | Explicit academy timezone/date range, maximum seven days and 20 rows/page; recorded session IDs, start/end, mode/status and safe link | `scheduling.manage` semantics and Core. No assertion of availability, no write, no exposed meeting-link token. |
| `drafts.prepare.v1` (AI-2 candidate) | Prepare a personal draft from authorized facts; preview actual contents and purpose | No student/account change, send, enrollment, fee or payroll effect. Persist only through the approved draft operation; retaining a draft does not authorize its eventual use. |

The listed live tools are candidates with precise limits, not enabled implementations. Underlying lookup joins and link destinations must be checked against current source before registration. Keep their contracts domain-oriented so all five experiences can reuse them; renaming an experience must not invalidate business tools.

Every registry entry declares input/output schema and version, required permissions/modules, projection and sensitivity, risk class, time/result limits, transaction owner, freshness/stale check, idempotency rule, approval rule, allowed execution modes, audit event and safe error mapping. Unknown properties are rejected for command inputs. Tool results include record references, source timestamps, pagination and deterministic units/currency/timezone where applicable. Narrative cannot change those values.

Future tools for payments/refunds, payroll, role changes, deletion, bulk export and bulk messaging remain unregistered in AI-0/AI-1. Their UI cannot imply they are available. Existing finance and private-media P0 gates remain prerequisites for the affected later tools.

## 6. Risk and approval policy

| Class | Examples | Initial rule |
| --- | --- | --- |
| R0: scoped read/analysis | Search, recorded schedule, simulation | No repetitive confirmation; authorize and minimize fields on every dispatch. No automatic export or external send. |
| R1: low-impact preparation | Personal draft/task | Preview and explicit authenticated user confirmation before a persistent change in the first action pilot. Same authorized actor may confirm. A changed draft needs a new confirmation. |
| R2: consequential operation | Enrollment, reschedule, external send, bulk operation | Disabled initially. Later: exact impact preview, current domain entitlement and applicable authorized approver; define whether independent approval is required per operation. Never silently default an undefined rule to self-approval. |
| R3: privileged/financial/destructive | Refund settlement, payroll release, role/credential changes, deletion | Execution unavailable in initial PENTA. Later needs domain-specific policy, stronger authentication/approval if required, and completed domain/release gates. An LLM cannot assign a lower class. |

An approval is a database record, not a chat “yes,” unsigned client flag or token held by the model. Bind its server-computed digest to academy, initiating actor, authorized approver, tool and schema version, canonical arguments, target IDs, relevant expected versions/state, proposed effect, policy version and expiry. For financial proposals bind amount and currency too. Use deterministic schema serialization with fixed key order, normalized dates/decimals and explicit null semantics; reject ambiguity before computing the digest.

Choose a conservative 10-minute preview validity for the first R1 pilot, configurable per tool with a server-enforced maximum. It is an engineering default, not an agreed finance threshold. Approval transitions use SQL concurrency control. Any edited arguments, changed relevant state, expired approval or changed policy require a new preview. User confirmation authorizes only the displayed operation. Multi-step workflows display a plan; each consequential step has its own authority and outcome, and later steps stop after a failure unless the approved policy explicitly says otherwise.

Status: `Proposed -> AwaitingApproval -> Approved -> Executing -> Succeeded | Failed | OutcomeUnknown`; also `Rejected`, `Expired`, `Cancelled`. An R0 read may go directly from validated proposal to execution with `ApprovalMode=None` recorded. `Failed` means failure with a known no-effect/rollback result; ambiguous effects are `OutcomeUnknown`. A cancel request during execution is not proof of cancellation or undo.

## 7. Atomic execution, retries and audit ownership

Reserve/claim a unique execution in SQL with optimistic concurrency. Idempotency scope includes academy, initiating actor, tool version and key; reuse with different canonical arguments returns conflict. A matching retry retrieves the existing outcome after current access checks. Business uniqueness is separate—two different keys must not bypass a seat, invoice balance or duplicate-enrollment constraint.

For an internal SQL mutation, use one transaction for final approval/state check, the protected domain change, business audit and execution outcome. Define consistent lock ordering with the existing operation and verify it on real SQL. Do not hold a database transaction or invoice lock while calling an LLM/provider. Domain logic keeps its own concurrency protection. A model call must never be inside a transaction retry delegate.

Audit integration needs explicit ownership: the existing `AcademyAccessFilter` appends a generic audit after successful POSTs and starts transactions for selected controllers. A PENTA gateway must not produce a late audit failure after an independently committed mutation or open nested transactions. Before real writes, provide one tested PENTA application boundary and a narrowly scoped filter integration that retains authorization but delegates its audit/transaction ownership; manual callers of an extracted operation must retain their existing behavior. Generic audit and PENTA ledger are correlated, not competing commit owners.

For external effects, atomically persist the approved intent/outbox record and execution transition, then dispatch after commit. Keep provider idempotency key/reference where supported. A lost acknowledgement becomes `OutcomeUnknown` until status reconciliation; do not claim exactly-once delivery from a network API. Never blindly replay an unknown payment or message. A reconciliation task may record provider evidence but cannot manufacture success.

If audit or durable execution persistence is unavailable, a new write does not start. After process restart, inspect claimed executions and receipts; do not treat an expired worker lease alone as proof that replay is safe. Successful output includes authoritative record IDs, final status and a link to the actual record; the frontend renders that structured result independently of model wording.

## 8. Storage, conversation context and privacy

Proposed tables in the application context (final migrations are implemented in AI0-S2):

| Record | Minimum purpose and constraints |
| --- | --- |
| `PentaTask` | Academy, initiating user, primary capability, timestamps, status and correlation; owned/scoped reads, never a client-chosen owner. |
| `PentaExecution` | Task/tool/version, canonical payload reference/digest, idempotency key, expected state, outcome reference, concurrency token; unique scoped idempotency index. |
| `PentaApproval` | Execution/digest, approver, current decision, policy, expiry and concurrency token; cannot be transferred to another execution. |
| `PentaAttempt` / audit events | Append-only application events, attempt number, actor, safe failure class, provider receipt reference and timestamps. No prompt, bearer token or raw provider response in metadata. |
| `PentaUsageReservation` / `PentaUsageEntry` | Tenant/user/task budgets, reservation state, provider/model tier, observed units and versioned price estimate; unique reconciliation identifier. |
| Workflow mandate/outbox | Added only when background/external execution is delivered; not an unused general scheduler in AI0-S1. |

All child relationships must carry/verify academy scope; use composite tenant keys or equivalent enforced relationships so a task in academy A cannot reference an approval in academy B. Index tenant/owner/status lookups. Execution payloads needed for resumption are bounded, encrypted/protected at rest under an explicit retention policy, and never exposed via generic logs or exports. Hashes alone cannot reconstruct a proposal; persist the exact approved payload when execution needs it. Do not persist student media or unrestricted raw chats in the ledger.

Initial context is request-scoped with bounded recent turns, not unlimited long-term memory. Facts come from current authorized tools. User preferences and later academy knowledge have owners, visibility, provenance, expiry and edit/delete paths. Conversation IDs, summaries and caches are partitioned by academy and actor; role/grant changes invalidate or reauthorize stored material before reuse. A new academy selection starts a new scoped task; data from the previous academy does not follow it.

Use only a fake provider and synthetic data by default. Before real-provider enablement, record the chosen provider/account/region, actual retention/training and subprocessors terms, permitted data classes, deletion/retention policy and budget. These choices cannot be inferred from current Azure hosting or from a developer's chat subscription. Until recorded, `LiveProviderEnabled=false`; real customer data cannot leave the API. A live-provider outage may fall back only to a provider already approved for that data class, otherwise use a clear unavailable result and manual workflow.

## 9. Model, cost and reliability contract

Application model routing is independent of which model develops the code. Prefer deterministic calculation and policy without an LLM; use a configured fast tier for bounded intent/extraction and a stronger tier only for evaluated difficult reasoning. The five PENTA experiences do not each require their own vendor/model. Keep runtime model IDs server-configured and restricted; users/model outputs cannot request arbitrary providers, context sizes or spend.

Starting limits for the scaffold: maximum 4,000 prompt characters, two planner rounds, three total tool calls, 20 result rows/page and a 30-second overall request budget with shorter operation limits. These are configurable engineering defaults tested at boundaries. No unbounded recursive handoffs. Later paged exploration must remain under the same task budget.

Before an external model call, atomically reserve its worst permitted configured cost/tokens against tenant and user budgets. Reconcile with observed usage; unknown usage keeps a conservative reservation until reconciled. Use decimal money, price version/currency and separate actual versus estimated cost. Unknown pricing or exhausted budget prevents a new live call. Per-user concurrency and tenant-wide limits supplement the existing IP/global limiter; counters must be shared before scale-out.

Trace task/execution/tool/policy/model version, latency, cost, denial and outcome counts using redacted metadata. Add global, academy, capability, provider and tool kill switches. Disabling stops new work and pending dispatch; the UI must accurately report work already in flight. Manual pages remain usable when PENTA is disabled or unavailable.

## 10. Autopilot authority and controlled expansion

Do not store a user's access/refresh token for recurring work. A later workflow mandate records academy, approving accountable user, permitted tool/argument constraints, trigger/window, recipients, budget, expiry, escalation owner and revocation state. Its effective authority is the intersection of the mandate and current permitted authority; ownership changes require explicit reassignment/reapproval. No job silently continues after the accountable actor loses the required right.

A worker claims one run with a bounded lease, revalidates mandate/current policy/consent, then calls the same execution service. Trigger duplication, concurrent workers and retry use run plus execution idempotency. New objects discovered by the workflow must fit its recipient/record constraints; they are not implicitly approved. Financial, account and bulk actions stay disabled until their explicit later gates.

Pulse recurring computations can be deterministic and need not invoke a model. A briefing notification still requires channel/recipient rules. Navigator inbound WhatsApp has a separate verified-channel and contact identity boundary; matching a phone number alone is not authority to reveal a student record. Authenticate appropriately or provide public approved information and staff handoff.

## 11. Threats and required acceptance cases

| Threat / defect | Required control and evidence |
| --- | --- |
| Prompt or document says to change academy, role, tool or recipient | Treat content as untrusted data; forged schemas/IDs and malicious tool-result text cannot expand server context or registry. |
| Platform bypass or inherited staff permission leaks data | Explicit PENTA policy runs for all actors; full deny tests include platform owner, custom role and independent grant cases. |
| Cached history leaks after relationship/role change | Reauthorize saved content and source records, invalidate scoped cache, and test revoked guardian/staff access. |
| Double click, retry, worker crash or concurrent approval | SQL unique keys/concurrency/transaction tests prove one business effect or a preserved unknown outcome. |
| Manual action races an AI action | Both call the protected invariant; real SQL tests preserve balances/capacity and reject stale proposals. |
| Model claims “done” before a write commits | UI status comes from structured execution ledger only; injected success text cannot set Succeeded. |
| Partial write or audit failure | Fault injection proves rollback of internal work; external unknown state is reconciled without blind replay. |
| Cost amplification or looping | Shared atomic budget reservations, bounded calls/context, concurrency/timeout tests and kill switches. |
| Wrong arithmetic/time/availability | Deterministic source calculations with currency/timezone, source links and explicit stale/unknown labels. |
| Cross-provider fallback or media leaks | Approved provider/data-class matrix, no arbitrary outbound URLs, private object checks and redacted telemetry. |

Initial golden journeys and expected outcomes (fixtures to implement, not tests already passed):

| ID | Journey | Expected behavior |
| --- | --- | --- |
| PENTA-01 | Eligible admin asks for a named student | Bounded matches, minimal fields, safe source link; no write. |
| PENTA-02 | Two students share a name | Present choices; never choose a mutation target by guess. |
| PENTA-03 | Staff/user substitutes another academy or record ID | Deny without data, existence detail or provider call. |
| PENTA-04 | Platform owner invokes tenant tools without the later delegation feature | Deny despite the legacy owner bypass. |
| PENTA-05 | Admin asks for next week's recorded schedule | Explicit academy timezone and bounded recorded results; no claim of vacancy or booking. |
| PENTA-06 | Navigator recommends follow-up and hands to Executor | Same scope/task/budget; draft shown, nothing sent by recommendation alone. |
| PENTA-07 | A confirmed draft is edited, expires or actor loses access | Reject old approval and require current authorization plus a fresh preview. |
| PENTA-08 | Two clients confirm the same operation | One claim/effect, matching retry gets same result; changed payload conflicts. |
| PENTA-09 | Pulse/Twin see incomplete or stale finance inputs | Explain the missing/stale evidence; never invent a balance or post a payment. |
| PENTA-10 | Budget/provider fails, or an external send times out | Manual fallback; known failure or OutcomeUnknown as appropriate; no false success or blind resend. |

Additional gate coverage: disabled feature (zero provider calls), anonymous/expired/revoked identity, inactive academy, missing module, input/response bounds, arbitrary tool and field tampering, malformed model result, injected instructions, grant expiry during approval, cancellation, restart, audit/store failure, two-tenant execution/history access, mobile/keyboard/manual fallback. Add these cases to the existing API/SQL/browser program at the slice where the behavior is implemented; no wholesale audit rerun is required.

## 12. Product UI contract

Use one PENTA entry point and workspace with the five named capabilities, a task/approval inbox and contextual entry from My Academy. A capability handoff should feel like one task continuing. Do not force the user to choose an agent before every question. Pulse is a briefing, Executor is an action workspace, Navigator presents options and next steps, Twin presents evidence/scenarios, and Autopilot shows enabled workflows and exceptions.

Every result can show its sources/as-of time, clear next action and actual status. Preview cards show affected records and changes; final cards link to the saved record. Unknown outcomes say that verification is pending. Keep technical model IDs, token counts and internal policy details in authorized administration/diagnostics rather than ordinary user flows. No unimplemented capability is advertised as working. Preserve responsive design, visible form labels, keyboard navigation, screen-reader status and manual fallback.

Premium visual exploration can begin once these states and the first API contracts exist. v0 may design the PENTA workspace, but all permissions and real execution states come from this architecture. No design tool is opened by this decision.

## 13. Implementation slices and exact next task

| Slice | Deliverable | Exit evidence / route |
| --- | --- | --- |
| AI0-S1 — LOCAL PASS | Disabled gateway, explicit current actor policy, five capability IDs, code-only registry, bounded dispatcher and fake provider/synthetic tool | API and real Identity/HTTP/two-academy SQL checks passed; evidence in AI0-S1 report. Release remains open. |
| AI0-S2a — LOCAL PASS | Synthetic-only task/execution/attempt ledger, idempotency claim/replay and PENTA-owned audit | Migration, targeted/full API and guarded SQL concurrency evidence in S2a report; no domain mutation tool. |
| AI0-S2b — LOCAL PASS | Tenant-linked approval/usage schema and finite, atomic synthetic work quota | Full API and run-owned SQL tenant/user concurrency, scoped FK, unknown outcome and restart checks. This is not priced model usage or an actionable approval. |
| AI0-S2c — LOCAL PASS | Internal-only priced model budget and conservative crash/audit fault recovery contract | Run-owned SQL concurrency, fault, restart and usage proof; no live provider, production price, automatic recovery or external disclosure. |
| AI0-S3a — LOCAL PASS | Synthetic R1 draft prepare/read/confirm contract with current authority and exact argument binding | Real SQL concurrency, actor/tenant denial, digest, expiry and audit rollback proof; approval has no domain effect. |
| AI0-S3b — NEXT | First authorized R0 read projection or approved R1 domain adapter | Preserve current actor/tenant policy, privacy minimization and no unapproved live provider; Sol High for authority boundary. |
| AI0-S4 | Real provider adapter behind independent disabled flag, redaction/limits/error mapping and evaluation harness | Fake/fault tests; synthetic external calls only after approved provider configuration. Sol High. |
| AI1-S1 | First narrow search/read projection and source-linked PENTA result UI | Per-tool role/field/tenant checks, browser/mobile and golden-answer evidence; privacy decisions before customer data/model enablement. Sol High for boundary, Sol Medium/Cursor for bounded UI. |
| AI1-S2 | Recorded schedule/batch reads and first deterministic Pulse card | Known source defects excluded/fixed, factual metrics and no unintended writes; existing domain tests retained. |
| AI2-S1 | One low-impact persistent draft with prepare/confirm/execute/verify | Approval/idempotency/audit failure matrix, same manual and PENTA invariant, SQL plus browser proof. Sol High. |

The broader AI-3 through AI-9 tasks stay in the [delivery plan](ACADEMY_DESK_AI_IMPLEMENTATION_PLAN.md). Enterprise P0/security/remediation continues concurrently; one slice passing does not close an issue's remaining physical-device or release conditions.

### Retained task packet: AI0-S1

```text
MODEL: GPT-6.1 Sol, High reasoning
TOOL: Codex, Default mode
REPOSITORY: D:\AcademyDesk-codex-p0
BRANCH: codex/enterprise-p0-continuation (recheck status before edits)
ENVIRONMENT: Local development / guarded disposable QA only
TASK: Implement the disabled PENTA foundation scaffold from ADR PENTA-001.
MAY MODIFY: apps/api/Intelligence/Penta; a PENTA controller; minimal Program.cs
registration/configuration; targeted API tests and existing guarded HTTP/SQL
harness integration where needed; PENTA handoff/report documentation.
MUST PRESERVE: unrelated work, main, QA/EVIDENCE, domain behavior, test guards.
VALIDATE: build, relevant API tests, real Identity/HTTP denial and two-academy
cases using existing QA isolation; diff/secret checks. No full audit restart.
STOP WHEN: S1 acceptance below passes and handoff records evidence/limits.
NEXT ROUTE: Sol High for AI0-S2; Sol Medium/Cursor for bounded follow-up work.
```

Concrete S1 acceptance:

1. New PENTA endpoints explicitly require authentication; missing/false `Penta:Enabled` exposes no usable capability and invokes no model/tool. Otherwise-authorized callers receive a stable unavailable response (404 when disabled); existing authentication/authorization denials remain 401/403 as appropriate.
2. When explicitly enabled in a guarded local fixture, only active Owner/AcademyAdmin users in the selected academy may run the synthetic scaffold. Platform owners and all other roles deny even if an existing filter permits broader access. Academy and capability allowlists are server-owned.
3. Only `diagnostic.synthetic.v1` can be registered in the scaffold, and only in Development/Testing. The fake provider has no network implementation; user prompt cannot cause a domain query, arbitrary tool, URL request or business mutation. Normal production configuration cannot register the synthetic tool.
4. `POST /api/academies/{academyId}/penta/turns` accepts a bounded text plus optional validated capability ID and returns only a clearly synthetic structured result/correlation ID in S1. No saved task, real answer, live provider or completed business action is claimed. Later durable task IDs replace this provisional scaffold response before AI-1.
5. Preserve the current filter's authorization. Its metadata-only generic POST audit may run for a successful synthetic request; test and document that effect. A denial must not be audited as a successful business action. Solve dedicated audit ownership in S2 before any real write tool is enabled.
6. Tests prove deny-before-provider/dispatcher with counters, feature/environment limits, missing identity, wrong role, foreign academy, inactive academy, tampered actor/extra command properties, invalid capability/tool, oversized input, malformed fake output and bounded cancellation. Seed only synthetic identities through existing QA guards; use no normal dev/prod database.
7. No real API key, live tool, workflow worker, approval UI, domain write, migration or broad frontend redesign is part of S1. Record its exact test results, source commit when committed, remaining gates and next slice.

## 14. Decisions resolved and remaining enablement choices

Resolved here: PENTA names/ownership; one shared modular platform; tenant and actor derivation; deny-default registry; first admin-only scope with platform-owner denial; minimal candidate reads; typed approvals and state model; SQL/outbox outcome design; finite budgets; synthetic scaffold and next task.

Outstanding before their affected live feature: provider/data-processing choice and retention policy; live academy allowlist and financial budget; staff/teacher/family tool visibility; high-impact approval thresholds and separation of duties; background mandate policies; qualified finance treatment/acceptance; messaging provider/consent and channel identity; physical-device and full deployment gates. The safe default for each unresolved live capability is disabled. These decisions do not block local fake-provider S1 development.

The architecture design itself was source-linked review. AI0-S1 now has the bounded implementation evidence linked above; this does not close any enterprise issue, establish production readiness or provide a real AI capability.
