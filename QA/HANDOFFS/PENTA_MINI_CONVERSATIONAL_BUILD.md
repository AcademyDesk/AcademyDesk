# Inbound development packet — PENTA AI Mini conversational brain

Date: 2026-10-08. Planning checkpoint N2s; next implementation packet N2t.

MODEL: Sol High
TOOL: Codex
ENGINE REPOSITORY: D:\PENTA AI Models
ENGINE BRANCH: codex/academy-language-contract
OBSERVED ENGINE HEAD: a50bbd354ec0e8938064ad2d753c1d4beccfa0fe
APPLICATION WORKTREE: D:\AcademyDesk-codex-p0
APPLICATION BRANCH: codex/penta-search
OBSERVED APPLICATION HEAD: d3c4ccfd6ea8344a73e390e866b43f512cb51e17
ENVIRONMENT: local synthetic development; private Azure GPU option later

## Read before implementation

Read the engine's `HANDOFF_TO_ACADEMY_DESK.md`, protocol, model manifest,
controlled-learning foundation and existing regression evidence. Read Academy
Desk's AGENTS.md, AI_HANDOFF.md, AI_MODEL_ROUTING.md, AI_PRODUCT_VISION.md,
ENTERPRISE_TESTING_ROADMAP.md and
[canonical conversational specification](../../PENTA_PRIVATE_CONVERSATIONAL_SPEC.md).
Verify branch/HEAD/status; prior dirty application/engine work belongs to the
owner and must not be overwritten. Do not edit D:\AcademyDesk main.

## Objective

Develop a private academy-aware conversational service supporting natural
follow-ups, corrections, useful clarifications, source-grounded explanation,
bounded operational proposals and supported languages. PENTA's five capabilities
share one experience. Academy Desk owns memory persistence, actor/tenant access,
source retrieval, policy, execution and audit. Mini has no customer DB authority.

Current evidence is N2r **86/94 exact, NOT ACCEPTED**; 243 installed unit tests
pass. The normal service remains 0.1.13, source 0.1.14/planner-0.12, protocol 0.1.
Do not declare the old read pilot repaired or repeat an unchanged candidate.

## N2t — first deliverable, not full model implementation

1. Convert PC-01 through PC-24 in the canonical spec to immutable synthetic
   multi-turn fixtures with explicit source observations, expected structured
   intent/state, grounded answer claims, permitted/forbidden calls and outcomes.
   Record fixture hashes before any model/prompt changes. Model-only checks
   cannot prove host authorization, history, network or capacity controls;
   designate the correct engine/app/infra test owner for each assertion.
2. Draft a versioned conversational request/decision/explanation schema and
   strict isolated validation tests in both projects. Keep the current 0.1 wire
   DTO/route and serving behavior untouched. No auto-negotiated downgrade or
   permissive optional-field escape hatch. Define evidence-reference ownership,
   unknown-outcome handling and future compatibility migration explicitly.
3. Freeze candidate evaluation settings/bounds and resource measurement plan.
   Document missing production latency/concurrency/language/budget decisions,
   rather than inventing accepted values. Evaluate one model at a time on the
   laptop; no download, model replacement or runtime restart in N2t.
4. Describe reviewed training corpus format, independent splits, privacy and
   licensing labels, versioned base/adapter/config artifacts, and rollback.
   Keep all 94 exposed regression cases outside training. No customer export,
   production feedback ingestion, fine-tune or live learning job.
5. Return a paired handoff: files changed, tests actually run, unresolved
   decisions, next engine task and matching Academy API/UI task. Do not enable
   a feature solely because a fixture or mock passed.

MAY MODIFY: new scoped synthetic fixtures, draft contract/schema tests and
their documentation/continuity pointers after checking file ownership.
MUST NOT MODIFY: serving model/settings, existing protocol/allowlist/domain
authority, frozen regression expectations/runners, unrelated dirty source,
production data/credentials, main, QA/EVIDENCE contents.
VALIDATE: fixture/schema consistency and focused local tests; git diff checks.
Existing enterprise/API suites are retained, not falsely reported rerun.
STOP WHEN: N2t fixtures/contracts are validated and paired handoff is recorded.
NEXT ROUTE: Sol High for private candidate/resource decision and consequential
integration; Sol Medium for clearly bounded UI after contracts settle.

## Later candidate and paired-build conditions

Model acquisition, GPU provisioning, training, serving migration and deployment
each require their own concrete scope/artifact/resource approval. The offer of a
better Azure VM is not purchase or deployment authorization. Use no public web,
external inference/embedding service or hosted fallback during product use.

After a genuine candidate change, retain strict **94/94** existing read checks,
the frozen fresh conversational checks and actual source-linked SQL/browser
acceptance. Preserve every failure; no output edits, weakened tests or repeat
until green. Full history, Pulse/Twin awareness, workflow writes and Autopilot
follow N3–N8 and their separate domain/privacy gates. Frontend/API development
can use labelled synthetic fixtures meanwhile, never as actual-AI acceptance.

No stage, commit, push, merge, Azure deployment, training job or issue closure
is authorized by this packet. Continue existing enterprise remediation.
