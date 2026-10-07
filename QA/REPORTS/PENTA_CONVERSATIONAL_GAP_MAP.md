# PENTA conversational requirement and source gap map

Date: 2026-10-04. Branch: `codex/penta-search`. HEAD: `f8389a77ff731ebc5a095f42a1d72d2671f22e18`.

Scope: C0 documentation and focused source inspection against the owner's 31-section conversational directive. **No application implementation or new runtime-test pass is claimed.** See [PENTA-002](../../PENTA_CONVERSATIONAL_ARCHITECTURE.md). Existing prototype/QA work is preserved; `QA/EVIDENCE` remains local/untracked. No third-party design/coding handoff is required.

Status meanings: **Partial** = a relevant foundation exists, not requirement acceptance; **Missing** = the required live contract is not implemented; **Planned** = a later bounded capability, unavailable now; **Recorded** = product direction documented, not application verification.

Successor checkpoint: [C1a](PENTA_C1A_PRIVATE_CONVERSATION_FOUNDATION.md) implements Testing-only private synthetic conversation/turn/message storage, atomic context versions, same-request replay and scoped SQL/audit safeguards. Real conversation interpretation, protected general free text, structured references and rich artifacts remain open. The C0 table is updated where this adds source evidence; its original documentation-only validation remains historical.

## All 31 requirements

| Requirement | Owner requirement | Current evidence / gap | Next packet and architecture section |
| --- | --- | --- | --- |
| PENTA-CONV-01 | Natural continuous multi-turn conversation | Partial: C1a private synthetic history persists; natural-language interpreter and chat UX missing | C1b/C3/C4; sections 1, 3–5 |
| PENTA-CONV-02 | Answer, search, reason, clarify, preview, approve and execute | Partial: minimal reads and synthetic approval only; no real conversational/domain execution | C1–C5; sections 5–6 |
| PENTA-CONV-03 | Pronouns, ordinals, dates and references | Partial: server-owned context version/races tested; referents/date interpretation missing | C1b/C2; section 4 |
| PENTA-CONV-04 | Ask only necessary questions and reuse verified details | Missing: query input is not intent/field clarification | C3/C5; sections 4, 6 |
| PENTA-CONV-05 | Rich answers/cards/tables/schedules/charts/approvals | Partial: existing source result cards; no shared typed conversational artifact contract | C1/C4; sections 3, 9 |
| PENTA-CONV-06 | Bounded recent messages and relevant context | Missing: current gateway has a text cap, not conversation context budgeting | C1/C3; sections 4–5, 11 |
| PENTA-CONV-07 | Short-term memory; database as long-term authority | Partial: deterministic scoped reads exist; no context checkpoint/memory policy | C1; sections 3–4 |
| PENTA-CONV-08 | One assistant across five capabilities | Partial: five identifiers/banner exist; prototype capability switches are not shared orchestration | C1/C3/C4; sections 1, 8 |
| PENTA-CONV-09 | Pulse drilldown, prioritization and dismissal | Planned: no accepted conversational Pulse | Later source-gated vertical; section 8 |
| PENTA-CONV-10 | Executor multi-step operations | Partial: synthetic ledger/approval only; shared real domain adapters missing | C5; sections 6, 8, 12 |
| PENTA-CONV-11 | Navigator leads/conversion/follow-up guidance | Planned: no accepted growth conversation or send approval | Later source/consent-gated vertical; section 8 |
| PENTA-CONV-12 | Twin evidence-backed explanations and scenarios | Planned: no accepted evidence/uncertainty contract at runtime | Later scoped metrics vertical; section 8 |
| PENTA-CONV-13 | Autopilot conversation compiles structured safe automation | Planned: no accepted trigger/condition/action/run contract | Later governed automation vertical; sections 6, 8 |
| PENTA-CONV-14 | Current role/module/action authority | Partial: pilot Owner/Admin restriction; Finance tool entitlement not established by it | C1/C2/C5; sections 6–7, 10 |
| PENTA-CONV-15 | Enforced tenant isolation, not a prompt promise | Partial: scoped reads/ledger and C1a private conversation FKs tested; referents/cursors pending | C1b/C2; sections 3–4, 10 |
| PENTA-CONV-16 | Risk-tier prepare/preview/confirm/execute/report | Partial: synthetic approval is not a durable domain action | C5; section 6 |
| PENTA-CONV-17 | Clarify ambiguous identities | Partial: separate multiple-match lookup exists; no multi-turn resolution | C1/C3/C4; sections 4, 7 |
| PENTA-CONV-18 | Useful failure recovery and changed-state handling | Partial: lookup errors/cancellation; no stale proposal/turn recovery UX | C1/C3/C5; sections 3, 6, 9, 11 |
| PENTA-CONV-19 | Internal task/cost runtime routing, one UI | Partial: priced synthetic budget exists; no approved conversational runtime/router | C3; sections 5, 11 |
| PENTA-CONV-20 | Actual progressive streaming without fake reasoning | Missing: no authenticated conversation event stream/status receipt contract | C1/C4; section 9 |
| PENTA-CONV-21 | Private new/recent/continue/rename/search history and retention | Partial: C1a synthetic create/read/continue and test expiry; general history UX/protection/retention decisions open | C1b plus privacy/UI gates; sections 3, 10 |
| PENTA-CONV-22 | Safe meaningful conversation titles | Missing: no title/history contract | C1/C3; section 10 |
| PENTA-CONV-23 | Verified current page and record context | Partial: existing route links; no server-validated page-context intake | C1/C4; sections 4, 9–10 |
| PENTA-CONV-24 | Global PENTA with optional page context | Partial: shell switch/global page exists; no conversation across navigation | C1/C4; sections 1, 9 |
| PENTA-CONV-25 | Enterprise chat, not blind consumer cloning | Recorded: prompt-first plus context rail and Workspace fallback; implementation pending | C4; sections 1, 9 |
| PENTA-CONV-26 | Pending fees → piano → sort → open Ananya | Missing: existing student/batch search lacks dues/enrollment projection | C1–C4; section 7 |
| PENTA-CONV-27 | First write after reliable read; enrollment transfer example | Planned: real write unavailable; transfer source needs concurrency/duplicate/eligibility proof | C5; sections 6, 8, 12 |
| PENTA-CONV-28 | Concise premium data-rich experience | Partial: responsive lookup scaffolding; true transcript and rich reply UX absent | C4; sections 1, 9 |
| PENTA-CONV-29 | Grounded data/knowledge/calculation; no fake intelligent demo | Partial: read projections grounded; no real conversational interpreter | C2/C3/C4; sections 4–5, 7–8, 12 |
| PENTA-CONV-30 | Complete architecture for all required layers | Recorded: PENTA-002 covers storage/context/orchestration/routing/tools/access/approval/audit/cost/stream/errors/retention/UI/security | C0 complete as documentation only; sections 1–12 |
| PENTA-CONV-31 | Conversational academy operating layer beyond CRUD | Recorded: one shared assistant with domain-by-domain releases, not unrestricted agent | C1 onward; sections 1, 6, 8, 12 |

Coverage: **31/31 requirements mapped, not 31 features passed.** No claim that natural-language reasoning or conversational CRUD works today.

## Source evidence and non-equivalences

1. [PentaExecution.cs](../../apps/api/Domain/Entities/PentaExecution.cs): existing task/execution/approval/usage ledger deliberately omits prompts. Add separate protected conversation/message/context contracts, retaining execution accounting.
2. [PentaController.cs](../../apps/api/Controllers/PentaController.cs), [PentaFoundation.cs](../../apps/api/Intelligence/Penta/PentaFoundation.cs): local synthetic turn and current active own-academy Owner/Admin policy. Do not broaden roles or silently promote synthetic endpoints.
3. [PentaStudentSearchService.cs](../../apps/api/Intelligence/Penta/PentaStudentSearchService.cs), [PentaBatchSearchService.cs](../../apps/api/Intelligence/Penta/PentaBatchSearchService.cs): active-only ten-row minimal reads. Student link is a supported 360 destination; batch destination is a list, not detail. Filtering ten displayed results cannot answer the whole-source dues question.
4. [PentaProbeCoordinator.cs](../../apps/api/Intelligence/Penta/PentaProbeCoordinator.cs), [PentaOpenAiProbeAdapter.cs](../../apps/api/Intelligence/Penta/PentaOpenAiProbeAdapter.cs): internal unrouted synthetic coordination and candidate transport. Neither is a selected conversational runtime. No live model evaluated in this checkpoint.
5. [InvoicesController.cs](../../apps/api/Controllers/InvoicesController.cs): unbounded invoice listing calculates paid from non-Voided payments. [PaymentsController.cs](../../apps/api/Controllers/PaymentsController.cs) and [FeeRemindersController.cs](../../apps/api/Controllers/FeeRemindersController.cs) explicitly use Completed/Reconciled. Status-based [AdminIntelligenceController.cs](../../apps/api/Controllers/AdminIntelligenceController.cs) summaries are not interchangeable net balances. This is a static source concern requiring a shared deterministic read contract, not a new reproduced bug or closed finance issue.
6. [Invoice](../../apps/api/Domain/Entities/Invoice.cs), [Enrollment](../../apps/api/Domain/Entities/Enrollment.cs), [Batch](../../apps/api/Domain/Entities/Batch.cs), [ProgramCourse](../../apps/api/Domain/Entities/ProgramCourse.cs): course enrollment can filter students; current invoice linkage does not attribute balances to a particular course. Avoid invoice/enrollment join multiplication and cross-currency sums.
7. [AcademyAccessFilter.cs](../../apps/api/Security/AcademyAccessFilter.cs), [PermissionCatalog.cs](../../apps/api/Security/PermissionCatalog.cs), [SubscriptionPlanCatalog.cs](../../apps/api/Security/SubscriptionPlanCatalog.cs): reuse current tenant/role/module authority. General PENTA pilot eligibility does not replace per-tool Finance entitlement or later action-level permission.
8. [EnrollmentsController.cs](../../apps/api/Controllers/EnrollmentsController.cs): visible transfer reads/checks then saves source/replacement. This does not prove serialized capacity/duplicate/prerequisite guarantees. Require targeted shared-service/concurrency evidence before an AI transfer; no repair or acceptance claimed here.
9. [FeeRemindersController.cs](../../apps/api/Controllers/FeeRemindersController.cs): queues notification rows. An assistant must say queued, not sent/delivered, without separate delivery evidence.
10. [Prototype workspace](../../apps/web/src/components/penta/penta-workspace.tsx), [shell](../../apps/web/src/components/enterprise-shell.tsx): preserve existing draft-aware switch and source UI, but domain-first lookup and isolated capability views are superseded by one prompt-first conversation. No application edit in this checkpoint.

## Next packet — C1b, Sol High

C1a's private synthetic ownership/storage/version/replay boundary is tested. Next add verified page/record references, bounded server-owned context and code-owned typed artifacts. The rest of the C1 target below remains unaccepted; do not enable arbitrary prompt storage or a live model merely because the synthetic schemas exist.

Implement only the protected conversation foundation in this feature worktree: DTO/state contracts, private academy/actor ownership, scoped conversation/message/turn schema under a local-only gate, monotonic context version, request replay/conflict, bounded references, validated page context and typed artifact contracts. Retain current default-off Development/Testing policy and narrow initial Owner/Admin eligibility. Use synthetic runtime and disposable SQL data; no live model, finance read/write, external send, production transcript storage or role expansion.

Acceptance must include private history denial, forged academy/conversation/record references, revoked user/module/role, stale concurrent tabs, duplicate request/replay, malformed/oversized content, bounded context, safe errors, required audit/fault behavior, restart/unknown outcome and scoped migrations. Reuse existing harness guards; remove only run-owned SQL resources. Do not rewrite or weaken previous regression tests.

C2 adds the deterministic Finance dues projection. C3 selects/evaluates an approved runtime. C4 combines these into the real prompt-first four-turn journey. C5 adds the first gated domain operation. These are packets within existing AI1/AI2 sequencing, not a restarted testing program or a promise that five turns complete all enterprise work.

## Evidence boundary

Existing S1a/b API/SQL and AI0 ledger/security evidence is retained. The interrupted lookup prototype has its own [report](PENTA_AI1_S1C_LIVE_SEARCH_WORKSPACE.md); it is not conversational acceptance. No physical Android/iOS test, live model/golden-answer pass, source-calculation repair, migration, commit/push, P0 closure, production database operation or Azure deployment is performed here.

Documentation verification completed:

- Ten packet documents checked; 323 relative file links resolve, none missing.
- Exactly 31 unique requirement rows cover 01–31; all 12 architecture sections are present.
- All nine preserved prototype application/QA-tool file SHA-256 hashes match the pre-edit snapshot.
- `git diff --check` passed; Git emitted existing LF/CRLF normalization warnings, not whitespace errors.
- No application suite rerun, staging, commit, push or runtime/model enablement. HEAD remains the source baseline above.

These checks validate this documentation packet, not the application or enterprise release. Existing enterprise/model/browser/device/release gates remain open.
