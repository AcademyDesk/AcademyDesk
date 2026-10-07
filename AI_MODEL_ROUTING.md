# Academy Desk model and tool routing

Credit conservation is a standing requirement. Choose the smallest capable route. Escalate only when the task needs that capability.

## Current directive — Codex-first (2026-10-04)

Codex owns full product design, frontend, backend, QA and delivery. GPT-5.6 Sol High is the requested primary senior route; Sol Medium handles clear bounded implementation and Terra optional mechanical work. Astra is optional independent review for consequential unresolved decisions. Cursor, v0, Lovable and Framer are optional specialists, never mandatory handoffs or blockers. The historical routing below is subordinate to this rule. Continue complete vertical slices through truthful local viewing or a human merge/Azure checkpoint. Do not imply the running model has been changed by an instruction file.

GitHub is the single source of truth for accepted application changes. Azure is the deployment target. Azure deployment occurs only after the required quality gates in `ENTERPRISE_TESTING_ROADMAP.md`, and only when a task explicitly instructs deployment.

Current PENTA packet routing (2026-10-04): C0 conversational architecture/gap mapping is documentation only. **Stay on Sol High for C1 conversation ownership, tenant isolation, retention boundary, concurrency, context and typed artifacts**, and for C2 finance projections and C3 runtime/authority integration. Sol Medium is appropriate for C4 bounded visual refinements after contracts are settled; Sol High owns consequential integration and C5 domain execution. Terra is optional for strictly mechanical edits, not access/finance/approval decisions. Astra is optional focused review of a genuinely unresolved consequential decision, not a requirement to redo accepted audits. The owner currently requests Codex-only delivery; do not open or hand off to Cursor/v0/Lovable/Framer without a new request. These are engineering recommendations, not PENTA runtime model selection or an automatic change to the model running this chat. See [PENTA-002 packets](PENTA_CONVERSATIONAL_ARCHITECTURE.md).

## Codex / Sol High

Current primary senior engineering owner for Academy Desk enterprise application completion. Keep ownership of high-risk decisions and continuation; use lower-cost tools selectively without handing routine ownership back to Grok 4.7.

Use for:

- difficult implementation
- complex debugging
- Docker and runtime failures
- database and integration failures
- security remediation
- authentication
- authorization
- tenant isolation
- difficult concurrency and transaction issues
- difficult cross-module changes
- difficult failing tests

Do not waste Sol High on:

- simple Git commands
- documentation
- straightforward UI changes
- repetitive refactoring
- routine lint cleanup

## Sol Medium

Use for:

- normal implementation
- moderate debugging
- repository administration
- documentation
- medium-complexity fixes

## Cursor

Cursor is a selective IDE, routine-implementation and browser-validation environment under the current Sol High engineering handoff. Prefer Composer 2.5 for bounded routine work. It does not own the remaining enterprise program.

Use for:

- everyday development
- high-volume implementation
- UI implementation
- responsive fixes
- lint cleanup
- components
- straightforward bugs
- repetitive refactoring
- repository and Git operations
- implementing approved v0 designs
- running routine validations

Cursor should conserve expensive Codex credits whenever practical.

## Astra Medium

Use selectively for:

- important architecture decisions
- independent architecture review
- difficult system-design questions
- a second opinion when Sol remains uncertain
- significant milestone review

## Astra High

Use very rarely because of credit consumption.

Use only for:

- exceptionally difficult architecture or security decisions
- major unresolved issues after Sol investigation
- exceptional high-risk decisions
- a final major production quality gate when genuinely justified

Do not use Astra High for routine coding.

## v0

Use for:

- premium SaaS UI and UX exploration
- dashboard design
- page redesign
- React and Next.js interface concepts
- visual component generation
- interaction concepts

v0 is a design specialist.

v0 must not independently redefine:

- backend architecture
- permissions
- database rules
- financial logic
- business rules
- production security

Approved v0 work should normally be production-integrated by Cursor or Codex.

## Framer

Use primarily for the public Academy Desk marketing website:

- homepage
- features
- AI product presentation
- pricing
- demo and contact
- landing pages
- animations
- public SEO experience

Keep Framer logically separate from the secured SaaS application unless explicitly approved.

## Lovable

Use as the experimental and prototyping laboratory.

Use for:

- experimental AI-native UX
- rapid feature concepts
- new workflow experiments
- separate product concepts
- testing radically different interaction ideas

Lovable must not directly alter production `main`.

A successful Lovable prototype follows:

```text
LOVABLE CONCEPT → REVIEW → approved design/architecture → Cursor/Codex production implementation
```

## GitHub

Single source of truth for accepted application changes.

## Azure

Deployment target. Deployment occurs only after required quality gates, and only when explicitly instructed.

## Which tools need which context

| Tool | Context |
| --- | --- |
| Codex | Read `AGENTS.md`, `AI_HANDOFF.md`, `AI_MODEL_ROUTING.md`, `ENTERPRISE_TESTING_ROADMAP.md`, and `AI_PRODUCT_VISION.md` for substantial engineering work. |
| Cursor | Read all five files for substantial engineering or testing work. |
| v0 | Primarily `AGENTS.md` constraints where relevant, `AI_PRODUCT_VISION.md`, and applicable UI requirements. v0 does not need to execute the enterprise testing plan. |
| Lovable | Primarily `AI_PRODUCT_VISION.md` and the experimental boundaries in this file. Lovable should not receive authority over production `main`. |
| Framer | Primarily product positioning, public-facing capabilities, and brand or design requirements. Framer does not need internal backend, security, or testing implementation details unless a specific task requires them. |

## Branch conventions

- `codex/<task>`
- `cursor/<task>`
- `v0/<task>`
- `experiment/lovable-<task>`

`main` contains reviewed, accepted changes. Cursor, Codex, and v0 must not independently change the same production files simultaneously.

## Standard task routing header

Every substantial coding task should identify:

```text
MODEL:
TOOL:
REPOSITORY:
BRANCH:
ENVIRONMENT:
TASK:
MAY MODIFY:
MUST NOT MODIFY:
VALIDATE:
STOP WHEN:
NEXT ROUTE:
```

Example:

```text
MODEL: Sol High
TOOL: Codex
REPOSITORY: D:\AcademyDesk
BRANCH: codex/auth-hardening
ENVIRONMENT: Local/Docker
TASK: Resolve tenant isolation failure
MAY MODIFY: API security + related tests
MUST NOT MODIFY: unrelated UI/business logic
VALIDATE: affected tests + integration tests
STOP WHEN: tests pass and handoff is updated
NEXT ROUTE: Cursor for routine implementation; Astra Medium only if an
architecture decision remains unresolved.
```
