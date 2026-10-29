# PENTA Mini N2g — response-bound runtime provenance

Date: 2026-10-08. Route: Codex / Sol High. Local engineering gate PASS;
linked SQL/browser acceptance NOT RUN. N1/N2, enterprise and release remain OPEN.

## Scope and defect

Academy worktree `D:\AcademyDesk-codex-p0`, `codex/penta-search`, HEAD
`d3c4ccfd6ea8344a73e390e866b43f512cb51e17`. Existing Mini worktree
`D:\PENTA AI Models`, `codex/academy-language-contract`, HEAD
`a50bbd354ec0e8938064ad2d753c1d4beccfa0fe`. Both HEADs unchanged; N2d/e/f
candidate work retained. No main edit, commit, push, merge or Azure deployment.

The host audit hard-coded service `0.1.0` / planner `planner-0.1`, although
the serving candidate had changed. Learning-event `model_revision` also stored
the expected weight artifact rather than observed runtime evidence. That could
mislabel new outcomes and replay/non-planning events. Historical rows are not
rewritten or retrospectively attested by this repair.

## Implementation

- Mini API/package **0.1.8**, planner **planner-0.7**, protocol **0.1**. The
  successful chat response includes `X-Penta-Mini-Runtime`: service/planner/
  protocol plus SHA256 of the imported API, version, orchestrator, operation
  policy, inference provider and admission gate modules. Identity is captured
  once at process import; no source-file reread per response. The strict
  six-field chat JSON body is unchanged.
- Academy parses exactly one printable-ASCII header, at most 1,536 characters,
  with required string fields, no duplicate/unknown keys, bounded versions,
  exact protocol and six lowercase SHA256 values. Missing/invalid metadata
  returns `RuntimeIdentityFailed`, no prepared tool plan. No preflight request,
  retry or cached version: identity travels with the actual planning response.
- Completion and post-planning denial audits serialize bounded response identity
  and `ResponseReported`. Events without an observed planning response—including
  Created/Claimed/Replayed—use null versions and `NotObserved`. Valid identity
  may remain available on a later body-schema/transport failure.
- Learning events use observed planner or `unverified`; model revision is always
  `unverified`. Expected artifact stays separately labelled `expectedModelRevision`,
  `runtimeModelAttested=false`, `training_eligible=false`. No raw prompts,
  assistant prose or argument values are added to audit metadata.
- The SQL harness now requires real RESULT audit runtime identity to agree with
  flattened versions and learning metadata. Original source/authority/state and
  context expectations remain intact. This assertion is prepared, **not SQL-proven**.

This is **response-reported source identity, not signed attestation**, deployment
approval, an approved-version allowlist or proof of model weights. Any valid
reported version remains subject to the same registry, RBAC, tenant, state,
quota and audit checks. Local live verification independently compares exact
source hashes; production provenance/pinning remains a separate gate. Older
Mini processes without the header fail closed against this updated host.

No frontend/schema/migration, tools, permissions, model weights, prompt,
inference settings, context size, admission tuning, domain writes, training or
hosted fallback changes. Existing inference image/container unchanged.

## Validation and retained evidence

Local evidence: `QA/EVIDENCE/penta-mini-n2g-20261008/`; intentionally untracked.
`manifest.json` records file hashes and safe container ID/image/status only.

| Check | Outcome |
| --- | --- |
| Academy missing-runtime baseline | 1 failing new test before repair; TRX retained |
| Mini missing-header baseline | 1 failing new test before repair; XML retained |
| Full API suite | **1,122/1,122 PASS**, 15 added cases; prior 1,107 retained |
| Installed Linux Mini suite | **137/137 PASS**, 3 added cases; inherited deprecation warning |
| Unchanged Academy language evaluator | **25/25 PASS** |
| SQL harness build | PASS, zero warnings/errors |
| Real .NET provider → existing Mini API | **2/2 exact**, all six hashes match local candidate |
| Whitespace validation | `git diff --check` PASS in both worktrees |

API cases cover missing/malformed/oversized/duplicate/private metadata, invalid
versions/protocol/hash, per-response version changes, and serialization across
seven audit event/runtime combinations. Original provider body/schema/gate
tests keep their expectations; their fake successful HTTP responses now include
synthetic valid identity. Mini tests cover unchanged JSON schema, denial/ERROR
responses and absence of per-call file reads. No evaluator/runner weakening.

The live check sent two fixed synthetic prompts only: unsupported deletion and
a synthetic student-name lookup proposal. No connector, academy data lookup or
domain action executed. Its learning metadata is explicitly a serialization
contract example, not an observed successful academy operation. The temporary
probe initially referenced the wrong project path and failed compilation; that
setup error was corrected before any HTTP check, not counted as product behavior.

Final API container `e49605389d29ebff6048128e238f6f789ff7d718b0d70ea00a2375917d41becb`,
image `sha256:1b4540fce42c5f24a081cc6e9435981c309e74097e589a38a0e632ac551e941b`.
API source SHA `77f9236ea35b2ded5ed2513d6a042d537f55f02b8cb52acbb64c745ad5821e57`;
version SHA `c7a3a81cc8d9df715b94d678040190fbf35e2018b0e6ef90ea814c5cfc71b1ba`.
Other four module hashes are unchanged from N2f and retained in the manifest.
Only the existing API was rebuilt/recreated after inference-idle proof.
No new SQL/preview or container pruning; existing API/inference remain local.
Final readiness returned `inference_ready=true`; four inference slots, zero
active. Ports 49541/49542 have no listeners. Main remains at `f7af512` with only
its existing untracked QA/EVIDENCE. Both worktree whitespace checks pass,
staging remains empty, `.build-check` / Mini `results` remain ignored.

## Next / limits

Next **Sol High**: prepared N2d correction/reset acceptance through disposable
SQL and the 15-turn linked production-export browser journey, with the new
provenance assertions and exact owned-resource teardown. No concurrent language
benchmark. Persisted real SQL audit provenance is not inferred from InMemory
serialization or synthetic transport checks. No SQL/browser/TypeScript/export
rerun in this packet; frontend is unchanged.

N2f native Windows/distributed admission, alias/direct backend bypass, fault
markers across container recreation, production throughput/fairness/recovery,
model/deployment attestation, private history/privacy/key-ring, physical devices,
open P0 issues and pre-Azure gates remain OPEN. No general-intelligence parity
or 90% automation claim; continue the existing paired product/testing roadmap,
not a new audit or N3/domain expansion.
