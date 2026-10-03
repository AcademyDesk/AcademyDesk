# AcademyDesk model routing and handoff

Recorded 2026-10-03 (Asia/Calcutta), from the user's required model-routing instructions. This is the model-allocation section for the forthcoming AI Readiness Assessment. It does not establish AI readiness, approve new architecture, or certify completion of the Enterprise QA phases.

## Working policy

Before each substantial task, state **Recommended Model**, **Reasoning Level**, **Why**, and **Risk if delegated lower** when relevant. Follow the user's selected workflow. Recommendations do not switch this chat's model or authorize autonomous delegation.

- Astra: consequential architecture, unresolved cross-module security or transaction reasoning, and focused review of risky designs or implementations.
- Sol: primary engineering model; High for complex implementation and tests, Medium or Low for straightforward work.
- Terra: well-defined, low-risk routine engineering. Do not assign security decisions or financial business rules solely to save usage.
- Luna: tiny, easily verified text, naming, styling, or formatting edits. Exclude authorization, tenant isolation, financial and scheduling rules, payroll, migrations, architecture, agent permissions and action execution.

Use accepted Astra decisions and evidence. Revisit them only when new evidence exposes an actual gap. Model identity alone neither invalidates previous work nor proves correctness. Keep acceptance criteria and meaningful verification unchanged when routing work to a lower-cost model.

If a Sol task exposes a consequential unresolved architecture/security issue, pause the affected implementation and recommend a focused Astra review. Resume with Sol after the decision is settled. Move safe repetitive subtasks to Terra when their boundaries are clear. Avoid repeating a whole audit or adding routine Astra reviews.

## Phase-level allocation

These are task-based recommendations, not claims that the named future AI modules already exist or have approved specifications. Design assumptions and dependencies must be established during the readiness assessment. In two-model rows, reasoning levels follow the model order. Review means a focused review when warranted, not a mandatory extra model pass for every edit.

| Phase / Task | Recommended Model | Reasoning Level | Why | Can Delegate Lower? | Review Model |
| --- | --- | --- | --- | --- | --- |
| Remaining Enterprise P0 fixes | Sol; Astra for unresolved design | High | Implement evidenced critical fixes with explicit acceptance and rollback checks | Only mechanical supporting work | Astra for architectural or security uncertainty |
| Remaining Enterprise P1 fixes | Sol | High for permissions/data; Medium for bounded fixes | Repair accepted defects and cover remaining runtime gaps | Terra for isolated low-risk changes | Sol; Astra only for consequential new issues |
| AI architecture review / Academy Brain | Astra | High | Establish boundaries, data ownership, dependencies and action authority | Sol implements accepted decisions | Astra |
| Intelligence Gateway | Astra design → Sol implementation | High → High | Set authentication, tenancy, provider boundaries, budgets and failure behavior | Terra for approved boilerplate only | Astra at boundary/design changes |
| Agent Tool Registry | Sol following approved architecture | High | Encode tool contracts, scopes, validation and execution permissions | Terra for mechanical schemas only | Astra if authorization model changes |
| Approval Engine | Astra design → Sol implementation | High → High | Define approvers, decision binding, expiry, replay and execution races | No delegation of approval semantics | Astra |
| AI Audit System | Sol; Astra for audit guarantees | High | Attribute actions and outcomes without leaking sensitive data | Terra for approved display/mapping work | Astra for integrity/retention design |
| Model Router | Sol following approved policy | High | Implement selection, fallback, budget and quality controls | Terra for static configuration and fixtures | Sol; Astra if routing changes trust boundaries |
| Pulse Signal Engine | Sol | High | Implement approved signal rules, deduplication and tenant scoping | Terra for repetitive approved rules | Sol; Astra for cross-module semantics |
| Operator | Astra action-boundary review → Sol implementation | High → High | Connect agent actions to validated, authorized operations | Only presentation and boilerplate | Astra for consequential action paths |
| Autopilot | Astra design → Sol implementation | High → High | Establish autonomous action limits, approvals, retries and recovery | No delegation of authority/recovery decisions | Astra |
| Growth Engine | Sol after scope approval | High | Implement approved workflows, metrics, consent and action controls | Terra for read-only reporting/UI | Astra if autonomous external actions are introduced |
| Twin Simulation Engine | Astra design → Sol implementation | High → High | Define simulation fidelity, isolation, assumptions and production separation | Terra for fixtures/presentation after contracts settle | Astra |
| AI frontend architecture | Sol; Astra for cross-module authority design | High | Organize workspaces, streaming, state, errors and approval visibility | Terra for defined components | Sol; Astra for consequential new boundaries |
| Five-character UI | Sol | Medium | Implement an approved visual/component system with accessibility | Terra for repeatable components; Luna for copy | Sol |
| Animation/state integration | Sol | Medium | Keep visual states consistent with real operation status and reduced motion | Terra for specified transitions; Luna for tiny styling | Sol |
| Integration testing | Sol | High | Exercise real service boundaries, persistence and failure behavior | Terra for established low-risk cases | Sol |
| Security testing | Astra for novel threat design; Sol for execution | High → High | Test meaningful abuse paths against explicit security requirements | No delegation of security conclusions | Astra for new/high-impact findings |
| Tenant-isolation testing | Sol using accepted threat model | High | Verify cross-tenant denial and data integrity across actual boundaries | Mechanical fixture work only | Astra if isolation architecture is unresolved |
| Financial testing | Sol | High | Verify amounts, ledger invariants, idempotency and rollback | Test data formatting only | Astra for unresolved financial/transaction invariants |
| Scheduling testing | Sol | High | Verify conflicts, time zones, recurrence and delivery-mode rules | Terra only for approved simple cases | Astra for unresolved concurrency/rule conflicts |
| Regression testing | Sol for critical paths; Terra for routine cases | High/Medium → Medium | Preserve accepted behavior and target changed dependencies | Yes, where fixtures and expectations are settled | Sol for failures and closure |
| Routine UI fixes | Terra | Medium | Apply bounded layout, responsive and accessibility corrections | Luna for tiny copy/style changes | Sol if shared state/navigation behavior changes |
| Documentation | Terra | Medium | Record verified decisions, evidence and handoffs | Luna for formatting only | Sol for technical accuracy; Astra only for architectural decisions |

## Immediate continuation

**Recommended Model:** Sol<br>
**Reasoning Level:** High<br>
**Why:** Complete the bounded role-revocation evidence handoff, then repair an already reproduced staff-role replacement bug using the accepted architecture.<br>
**Risk if delegated lower:** Incorrect membership changes could preserve obsolete finance access, remove protected identities, or leave partial state after a failure.

The [current diagnostic](PHASE_2B_ROLE_REVOCATION_DIAGNOSTIC.md) records two fresh native SQL runs. Each has 35 accepted controls and two failing policy observations in the same existing BUG-SEC-0004. Standard replacement reports Operations-only while obsolete custom roles remain; a fresh login retains finance access. Existing-token invalidation and live grant revocation passed in this bounded diagnostic.

At this handoff, the report's referenced source receipt and consistency-validator files are absent. Finish the pending matrix/evidence consistency bookkeeping and verify its claims before treating the diagnostic package as complete. Preserve the already executed runs; repeat them only if source/evidence inconsistencies require it.

Then implement the narrow role-replacement repair: remove obsolete mutable/custom memberships, return actual memberships, preserve protected identities and independent explicit grants, and handle membership/stamp/audit failures atomically. Verify the reproduced failure plus protected-role, grant, rollback and denial cases. Escalate only an unresolved policy or transaction-design question to Astra. This plan does not mark the defect fixed or Phase 2B closed.

The laptop remains the development environment. Follow the established separate release workflow for GitHub/Azure. No deployment is performed by recording this plan.

## Basis and version handling

The allocations above implement the user's AcademyDesk policy and engineering judgment. Official OpenAI guidance describes Astra for deep/ambiguous analysis, Sol for complex work balancing time and cost, and Luna for scoped work; it advises keeping the lightest setting that meets the quality bar. See [official model-selection guidance](https://developers.openai.com/api/docs/guides/model-selection), consulted 2026-10-03 through the OpenAI Docs skill.

Use the exact version available in the user's model selector. The host tool metadata currently lists Astra, Sol, Terra and Luna variants, including legacy Terra. That does not establish which version is running in this chat or guarantee unchanged account availability. The family names in this plan follow the user's labels; do not silently migrate versions, promise fixed savings, or treat API defaults as Codex settings.
