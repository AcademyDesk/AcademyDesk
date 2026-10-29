# N2m — sparse query contract implementation and acceptance

2026-10-08. **Candidate NOT ACCEPTED: 60/86 actual-model exact.** The single prescribed language pass and independent regrading are complete. No retry or linked SQL/browser acceptance follows this failed gate.

## Scope

Implement [N2l's bounded decision](PENTA_MINI_N2L_CONTRACT_REVIEW.md) in the existing private Mini only. Academy worktree `D:\AcademyDesk-codex-p0`, branch `codex/penta-search`, HEAD `d3c4ccfd6ea8344a73e390e866b43f512cb51e17`; Mini `D:\PENTA AI Models`, `codex/academy-language-contract`, HEAD `a50bbd354ec0e8938064ad2d753c1d4beccfa0fe`. No stage, commit, push, merge, main, Azure, production database or credential change. Earlier dirty work, evidence and stopped failed SQL are preserved.

Mini package/service **0.1.12/planner-0.11**, external protocol 0.1. The existing API alone was rebuilt/replaced after inference-idle verification. Inference container remains `c06a0392290ce1b9198ff49e2bf6d1e9d4c2891b88e115b40e0329dda580aca2`, pinned image `sha256:559ac229adefe0f7e2d4e32f5222f26927b6bb8ba44b9db08e2544afebf41984`, existing Qwen3.5-2B Q4_0, context 2048, four threads/slots, reasoning off, temperature 0, max 256 tokens. No model/settings/deadline/admission changes or inference restart. Dependency constraints are unchanged, but the API build resolves the existing ranges again: this is not an immutable dependency lock or model-weight attestation.

## Implemented contract

- Known intent remains independent of executable availability. The four known catalog tools are not four new authorized tools; Academy still permits its existing two reads only.
- Search internal arguments are exactly `mode`, `set`, `remove`; mode replace starts empty, refine inherits unchanged fields from the entry snapshot. Sparse set uses existing typed query fields; removals are a bounded unique list of those names.
- Runtime rejects extra keys, invalid modes/values/enums, duplicate/foreign removals, replace-with-removals, overlapping set/remove, old four-field operation objects and legacy flat query arguments. No stripping, compatibility adapter, text argument extraction or retry.
- Query materialization uses the detached deep-copy captured before inference, not mutable request state after the await. That same context is sent as canonical JSON in the user message. Authoritative state is not updated by the planner.
- A narrow whole-request query-only fee/balance filter-change classifier suppresses only the duplicate mutating-prefix signal. Extra record/finance/send/delete clauses fail the exception; independent operation denial and all final allowlist/host/risk/ID/broad-search controls stay.
- Prompt expresses sparse replacement/refinement and distinguishes names, course subjects and fee conditions without embedding held-out expected answers. There is one model call. Compiled external arguments and six-field host protocol remain unchanged.

Production changes are confined to Mini `orchestrator.py`, `operation_policy.py`, `version.py` and package version. Existing six-module provenance covers the changed compiler/policy. Academy application, frontend, DTO/provider, domain, runner and evaluator are unchanged this task.

## Unit and fixture evidence

43 new sparse-contract/policy tests were added before production changes. The corrected final baseline against installed N2k is **25 failed / 18 passed**, no errors/skips. Invalid-shape denials already passed in the old contract; absence of sparse transitions/policy and acceptance of old field objects failed.

Initial baseline attempts are preserved: a phrase containing an unanchored “this/the student” reference triggered the existing clarification guard before inference. It was changed only in the new unit fixture to “Revise these search settings” so malformed-plan tests exercise the intended compiler. Existing model acceptance messages/expectations were not changed. Initial source collection also lacked the sibling scripts/benchmarks directories; all failures remain in separate XML, not overwritten or described as semantic regressions.

Final source **223/223 PASS**, final rebuilt installed package **223/223 PASS**, zero skips/errors. The prior 180 units are retained; encoding migrations are test-only and preserve output/state/availability/invalid-value assertions. The previous prompt-wording test now asserts sparse replace/refine/reset wording rather than obsolete four-field text; no executable-result assertion was removed. New tests cover contradictions, unsupported availability, no context, delayed external snapshot mutation and mixed record/action requests.

Unchanged Academy evaluator **25/25 PASS**. Existing Starlette/httpx deprecation warning remains; no dependency upgrade was performed to suppress it. Unit/schema success is not actual-model language acceptance or enterprise completion.

## Single language gate

All existing **74** prescribed case identities, messages and exact expectations are retained: frozen API 26 plus diagnostics/context/safety/operation/prior-reset/omission/intent/transition probes 48. New independently frozen suite adds **12** cases for fee-only search, fresh subject/code replacement, removal, query-only change, compound record/action denial and quoted/negated controls. Its SHA-256 before the first candidate inference is `c27133e964f5830457abc21c043bd49ae05e08a06cf98f0935805861e06918d9`.

One serialized evaluation, no overlapping probe/API/SQL, no repeat-until-green. No planner changes were made after candidate inference began. Every prescribed case was observed once; all ten suites are complete.

| Suite | Exact / prescribed |
| --- | --- |
| Frozen API | 21/26 |
| Exact linked diagnostics (planning only) | 1/3 |
| Context | 4/8 |
| Safety | 4/4 |
| Operations / quoted / negated read controls | 8/8 |
| Prior combined code/reset | 0/1 |
| Omission | 3/8 |
| Intent | 6/8 |
| Field transition | 6/8 |
| New sparse-query controls | 7/12 |
| Total | **60/86** |

On identical prior cases, **53/74**, versus N2k 67/74: 18 formerly passing cases regress, four former failures pass, three former failures remain. Original 66-case subset is **47/66**, versus N2j 63/66. This is a regression, not general improvement. N2-01/05/08 and TRANSITION-FRESH-02 improve; the query-only change's earlier prefix denial is fixed but its actual plan still contradicts itself, so TRANSITION-FRESH-03 remains a failure.

All **26 failed IDs** remain recorded:

- N2-04, N2-06, N2-09, N2-13, N2-14.
- LINKED-DIAG-01, LINKED-DIAG-02.
- CONTEXT-FRESH-04, CONTEXT-FRESH-05, CONTEXT-FRESH-06, CONTEXT-FRESH-07.
- FRESH-04.
- OMISSION-FRESH-01, OMISSION-FRESH-03, OMISSION-FRESH-04, OMISSION-FRESH-05, OMISSION-FRESH-06.
- INTENT-FRESH-01, INTENT-FRESH-03.
- TRANSITION-FRESH-03, TRANSITION-FRESH-04.
- SPARSE-FRESH-02, SPARSE-FRESH-03, SPARSE-FRESH-04, SPARSE-FRESH-05, SPARSE-FRESH-06.

Failures comprise **19 incorrect clarifications and seven incorrect tool proposals**. Raw linked diagnostics choose `replace` with a nonempty remove list; TRANSITION-FRESH-03 and SPARSE-FRESH-04 set Clear and remove balance_status simultaneously. Strict validation rejects these contradictions, not a successful query. Separately, N2-04 invents subject Piano, N2-06 drops Pending, and SPARSE-FRESH-05 drops name/subject despite a request to retain other settings. FRESH-04 opens a listed ID instead of searching the new code. SPARSE-FRESH-06 proposes GetLearner with `learner_id=REC-617`, a display code instead of a stable displayed ID. These are not all a single shape defect or demonstrated inevitable weight limitation.

Independent unchanged Academy grading: **86/86 contracts and state unchanged; 85/86 diagnostic host-boundary compatible**. SPARSE-FRESH-06 fails that boundary. Required no-call controls pass, but this is not a general safety/authorization claim; no connector was executed, no live host rejection or source answer was proven. Probe paths are 45 inference / 15 deterministic; those counts exclude 26 API rows whose raw model plans are unobserved. Deterministic guards are not model-reasoning gains. No HTTP 429, API transport error or ERROR response was observed.

Evidence is local/untracked at `QA/EVIDENCE/penta-mini-n2m-20261008/`: all **28 copied hashes** independently matched; all 86 grades match the unchanged evaluator and all six source hashes match the actual API response identity. Original frozen API packet is `penta-mini-language-c5b91d98-b08f-4609-824b-d47baba03ea3`. Mini ignored `results/academy-n2m-*` retain the original outputs and all setup/baseline failures. The collector is an ignored `.build-check/n2m-evidence.cjs`, not an evaluator replacement. Reported identity is not signed deployment/model-weight attestation.

Serving API container `4cc3e19e7b450012e49d8945a1ebf182eec6383e23e3164e72ead9e95b649e48`, image `sha256:f214adccb47692ea2706fca15fa3911e259714073fcb48557e193a1e71371563`. The six response-source hashes are in the evidence manifest. Final inference health is 200/all four slots idle. Existing failed N2h owned SQL remains exited/retained; 49541/49542 have no listeners. No resource/evidence cleanup or accepted SQL teardown is newly claimed.

## Stop and downstream gates

The candidate cannot pass after any semantic failure. The one prescribed full pass is recorded; **this implementation packet stops here**. No blind prompt loop, automatic model replacement/training, hosted fallback, relaxed evaluator, new Astra audit or N3 expansion.

Evidence-backed disposition/options, not automatic implementation:

1. Recommended next bounded **read-only decoder-constraint feasibility check, Sol High**: determine whether the existing decoder can enforce replace → empty remove and disjoint set/remove before generation. The current grammar constrains types/keys but leaves these cross-field contradictions for runtime rejection. This uses the saved raw plans, not another Astra architecture audit or unchanged benchmark rerun. No follow-up candidate is implemented by this report.
2. If a later explicitly scoped packet implements decoder tightening, preserve strict runtime rejection and every frozen/fresh expected outcome; it must independently pass the same acceptance gates. Do not merely strip contradictions or call them equivalent edits. This alone cannot establish correct subject/name assignments, retained filters or code-versus-ID intent.
3. Valid-but-wrong semantic plans remain an existing-private-engine capability gap. If stronger private-model evaluation or settings/training is desired, obtain a separate approved scope with resource/privacy/quality gates; no automatic replacement or promise of frontier parity. Existing model/authority remain unchanged in this packet.

Prepared three original plus three context SQL conversations and 15-turn browser acceptance are **NOT RUN: language prerequisite failed**. No new SQL container/preview or physical-device test was started; do not reuse/remove the stopped failed N2h container or prune other containers. N1/N2, production capacity/privacy/device/security/regression and pre-Azure quality gates remain OPEN. The premium five-capability conversational PENTA AI and 90% eligible AI/10% manual goals remain targets, not current accepted coverage.

Next engineering route: **Sol High for the bounded read-only decoder-constraint feasibility check**. No further implementation or runtime/model-setting change is presumed approved by this failed packet. N1/N2 and enterprise/release remain OPEN; earlier successful narrow checkpoints and failed candidates are retained, not restarted or erased.

Final repository verification: both feature HEADs unchanged, nothing staged, `git diff --check` passes (inherited LF/CRLF warnings only). Hash comparison with the pre-task snapshot finds **no Academy non-documentation change**; Mini has exactly the seven planned existing-file edits and two new test/suite files, no removed files. N2j's 21 and N2k's 22 copied hashes still match, as do N2m's 28; the new suite's pre-inference hash is unchanged. Main remains `f7af512f884ca5fe8ac3ddb287c852492592e709` with only its existing untracked QA/EVIDENCE. All artifacts remain local; no secret values were printed.
