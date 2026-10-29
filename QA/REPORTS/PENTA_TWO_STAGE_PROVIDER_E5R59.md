# E5-R59 — inactive intent-to-draft-plan provider

20 October 2026. Route: Sol High. Continues the existing enterprise/PENTA program.

Follow-up [R60real full-plan result](PENTA_FULL_PLAN_E5R60.md) is now complete:
fresh6/12exact,exposed7/12exact/NOT ACCEPTED. The offline implementation below
remains valid; its pending runtime-test steps are superseded by that result/next decision.

## Completed implementation, not runtime qualification

- [x] Added `D:/PENTA AI Models/src/penta_mini/operational/intent_provider.py`.
  A trusted current Turn is cloned and bound to exact minimized messages/contract.
  The caller explicitly supplies one private native admission gate shared by both stages.
- [x] Stage1 uses unchanged R56 intent instruction/schema with64output tokens.
  Stage2 uses the existing compact draft prompt and intent-restricted current catalog
  schema with448output tokens. No expected answer, inferred authority, keyword fallback,
  answer repair, model retry or reduced/merged proposal substitution.
- [x] A single absolute30s deadline covers gate waiting, both stages and validation.
  Native workers additionally enforce the same absolute deadline before native requests.
  The existing native lease/definitive-result/quarantine behavior is retained.
- [x] Each instance is one-shot: maximum2generation invocations, no retry or concurrent
  duplicate turn. Cancellation stops the waiter promptly, blocks later stages, keeps
  native ownership tracked until it settles, and discards late results. `drain()` is explicit.
- [x] Final reply passes strict Proposal validation, request/intent/edit-mode binding,
  current capability tool/question filtering, and the existing mandatory reducer.
  The reducer checks draft revision/state, fields/types, duplicate tools, reference choices,
  required/clearable fields and field preservation. It is not replaced by a schema.
- [x] Combined provider fingerprint differs from either native stage and binds trusted
  Turn, model/runtime, prompts, schemas and the aggregate budget. Prior qualification
  identities do not automatically authorize this strategy.
- [x] Unresolved executing/unknown host operations are rejected before model work;
  they need host status/reconciliation, not a new write attempt.

There is no execute/save/send method, host mutation, approval grant, API registration,
serving import or default selection. The existing host remains responsible for current
tenant/actor/permission checks, CAS, approval, deterministic finance, domain transaction
integrity and required audit. A model upgrade never expands those rights.

## Verification

Added `tests/test_intent_provider_e5r59.py`: **66 synthetic mocked-native tests**.
Expanded related suite: **318 PASS in12.00s**, no skips; one pre-existing
Starlette/httpx deprecation warning. Counts overlap; do not add66+318.
Evidence: `D:/PENTA-Models/operations/e5r59-checks/regression.xml`.

The mocked transport exercises the real bounded native bridge for template/token,
HTTP response/schema and lease paths, not a mock that bypasses the bridge. It covers:

- All20previous authored development/evaluation oracles for prepare/refine/replace,
  clear, zero/false/decimal values, pause/resume/cancel/status/clarify/unsupported.
- Unbound message/schema rejection; caller context mutation during an await; distinct identity.
- Model authority fields, wrong request/revision/kind/mode, unknown fields/tools,
  wrong-capability tools/questions, invented references, repeated tools and prohibited clearing.
- Invalid/duplicate intent JSON, unavailable intent state and unhealthy native gate.
- Per-stage request failures, non-stop completion, HTTP429/500, token overflow,
  caller cancellation in either stage, absolute shared timeout, queue-time budget,
  cancellation-suppressing late transport, native ownership and no duplicate/retry.

An explicit negative-boundary test supplies a **catalog-valid but linguistically wrong**
East-zone update for the user's northern-zone request. Structural checks cannot determine
the user's meaning; the test confirms no semantic repair or actual host mutation occurs.
This is a deliberately scripted counterexample, not real-model evidence or an approval
to use the provider. Fresh model semantics and host authorization remain mandatory.

No real model inference, GPU trial, build, SQL/API/browser/device testing or training
was run in this batch. R58's real3B intent score remains11/12/NOT ACCEPTED; do not
describe mocked correct proposals as an improvement in actual model intelligence.

## Frozen review pins

`D:/PENTA AI Models/operations/intent-provider-source-pins-e5r59.json` records10
reviewed source/test hashes and fixed budgets. This is not a qualification/release manifest.
Provider SHA256 `af346f7e0efdd9a40348da2cdfa276e22eb3a1c8f23cbca52d8e6f7b4b1ea184`.
Test SHA256 `daa9fb02f305498efaa90fec9e254922d78641243a1a52cef11b5c1bbdf2a424`.
The R56 instruction/intent contract/evaluator, existing native bridge/reducer/host
rules and all prior frozen source pins were not edited. Only the new module composes them.

## Next execution path

- [ ] Sol High: author and freeze fresh full-plan ambiguity/transition/field-preservation/
  adversarial cases before any model calls. New model-unseen synthetic cases authored
  here are not blinded independent or third-party production acceptance.
- [ ] Verify actual native grammar conversion, both-stage template/token fits,
  model/source/profile identity, outgoing schemas and aggregate budgets in an isolated
  bounded harness. Keep existing resource guards/restoration and exact scoring.
- [ ] Run one separately bounded real3B full-plan comparison, with exposed R56cases
  retained as regressions and fresh cases scored separately. No training on answers.
- [ ] Only after engine qualification, return through current Academy host/SQL/API/
  authorization/approval/audit/browser/Manual-fallback integration and remaining
  enterprise/device/pre-Azure release gates. Do not activate writes or deploy early.

No model/tier/default, application/domain/database, main, Git stage/commit/push,
Docker restart/pruning or Azure changes. All original services remain running.
BUG-DATA-0001 remains OPEN. This batch does not complete enterprise testing or90%AI.
