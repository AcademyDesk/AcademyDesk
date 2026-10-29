# PENTA Mini N2k — intent and field-transition contract

2026-10-08. Local work only. **Candidate NOT ACCEPTED: 67/74 exact**. Evidence
collection/regrading is complete. This is not an enterprise, N1/N2, release, or
90% automation closure.

## Scope and decision

Continue N2j's three preserved failures, not a new audit. Academy worktree
`D:\AcademyDesk-codex-p0`, `codex/penta-search`, HEAD
`d3c4ccfd6ea8344a73e390e866b43f512cb51e17`; Mini worktree
`D:\PENTA AI Models`, `codex/academy-language-contract`, HEAD
`a50bbd354ec0e8938064ad2d753c1d4beccfa0fe`. No stage/commit/push/merge/main/Azure
or production database work. All earlier dirty work and failed evidence retained.

The old generation grammar omitted unavailable tools, so it could not represent
the required GetLearner intent in two failing corrections. The new internal
proposal grammar contains the existing known tool vocabulary independently of
execution availability. Mini denies any proposed tool absent from the request's
allowlist before returning a tool call. It adds no executable domain tool.

Search proposals must decide all four fields: `name`, `subject`, `balance_status`,
`sort_by`, each as `keep`, `remove`, or `set` with a nonblank, enum-valid value.
Mini materializes only these explicit decisions against the supplied context
snapshot, then applies the existing final argument and tool guards. No user-text
argument parser, legacy-plan repair, hidden argument stripping, or relaxed host
authorization. Legacy complete-query model output is rejected, not adapted in
production. The Academy response remains the same six-field protocol 0.1.

Known intent is not permission or a claim of execution. The host still owns role,
tenant, displayed-ID, data, quota, audit, replay, and approval checks. Unsupported
compound requests still cannot execute a partial read. Non-tool output carrying
a tool or hidden arguments now fails closed.

Local installed service/package **0.1.11**, prompt **planner-0.10**. Private model,
inference container/image, 2048 context, four slots, reasoning-off, temperature 0,
256 output tokens, admission gate and deadlines unchanged. Only the API image
was rebuilt; no model replacement, training, inference tuning, retry selection,
or additional runtime dependency. Existing dependencies remain range-based,
not a new reproducible dependency-lock acceptance.

## Tests and migration disclosure

Fourteen new contract tests before implementation: **13 failed, one passed** on
the installed N2j package. Final source and rebuilt installed package:
**180/180 Mini PASS**, no skips; unchanged Academy evaluator **25/25 PASS**.

Old stub fixtures now encode their same expected final queries as field decisions
through a **test-only** adapter, preserving invalid values/unknown keys for
denial tests. Existing outcome assertions remain unchanged. N2i execution-shape
tests retain their original strict arguments/allowlist assertions using the
separate execution grammar; they explicitly check the provider receives the new
intent grammar. New tests independently check field-operation shape, materialized
outputs, unavailable Get denial, legacy downgrade denial, immutable state and no
foreign authority fields. These unit fixtures are not natural-language accuracy.

The first source test copy nested `tests` into an existing directory, causing
19 collection errors; preserved, not credited as candidate tests. A fresh checked
test directory passed. The first API build's package metadata was corrected
before language evaluation; final installed package and response versions match.
Initial probe invocation was blocked by the local PowerShell script policy before
any inference; the actual run uses process-scoped Bypass, no persistent policy
change. Existing Starlette/httpx deprecation warning remains.

## Fixed serialized actual-model evaluation

Unchanged frozen and retained suites plus eight independent field-transition
controls frozen before inference. One evaluation per case; no successful retry
selection, parallel inference, changed expectations, or changed evaluator.
Planning probes execute no connector/domain action.

| Suite | Exact / total | Status |
| --- | --- | --- |
| Frozen Academy API | 22/26 | Four regressions |
| Retained N2h diagnostics | 3/3 | PASS |
| Retained context | 8/8 | PASS |
| Retained safety | 4/4 | PASS |
| Retained operations/quoted/negated reads | 8/8 | PASS |
| Prior combined reset | 1/1 | PASS |
| Retained omission | 7/8 | Subject/name regression |
| Retained intent | 8/8 | PASS |
| New field transitions | 6/8 | Removed filter retained; query change falsely denied |
| **All observations** | **67/74** | **Seven failures; NOT ACCEPTED** |

Frozen failures are N2-01 (invented subject `All`), N2-04 (subject `Pending`
invented from fee text), N2-05 (Piano duplicated into name), N2-08 (Guitar
duplicated into name). All frozen output contracts/state/host-compatibility
checks pass; valid shape is NOT semantic acceptance. N2j's prior 26/26 result
remains historical evidence, not the current candidate result.

OMISSION-FRESH-05 sets name to Cello and removes subject rather than switching
the subject. TRANSITION-FRESH-02 retains Pending despite removing the fee filter.
TRANSITION-FRESH-03 is denied before inference by the existing `change` mutation
prefix guard, although the request changes only query criteria. This is a
bounded intent-policy false denial, not proof the model failed that case.

The original N2j failures CONTEXT-FRESH-08, INTENT-FRESH-04 and OMISSION-FRESH-06
now pass. This does not establish general correction/removal reliability: five
formerly passing retained cases regress and two new cases fail. On the identical
original 66 observations, this candidate is **61/66**, versus N2j **63/66**.
Intent/permission separation helps the unavailable-reference cases, but forcing
all query fields into the new decision grammar does not preserve existing
comprehension. Do not claim general improvement from unit counts or the three
targeted repairs. No current run establishes the precise cause of the model's
wrong slot/value assignments; it is not a measured weight/window diagnosis.

All 74 outputs independently regraded by the unchanged Academy evaluator:
74/74 contract-valid, state-unchanged and diagnostic host-boundary-compatible.
These are not live host authorization or source proofs. Probe-only paths:
36 real-inference plans / 12 deterministic outcomes, excluding the 26 API rows
whose raw plans are not exposed. No connector executes in either evaluation.

Local `QA/EVIDENCE/penta-mini-n2k-20261008/` holds all suites/results, failing
baseline, final source/installed JUnit, collection-error JUnit and `manifest.json`.
All **22 copied evidence hashes** independently checked. Original frozen API
run is `penta-mini-language-52dd6993-6de5-4cca-a21a-bc0f2914dd3d`.
A separate deterministic-denial HTTP check matches 0.1.11/planner-0.10/protocol
0.1 and all six imported-module hashes to source, not another language case,
signed/deployment attestation or model-weight verification.

API container `1ae446b89a59c297e7ba70c3d9a0b0e5236cf7d6653c148bc6084a1707295665`,
image `sha256:3168bfe6bf1e49d1317fab3552bfcc46268b82daffd747fc0d4ac85f6e699350`.
Inference remains exact container
`c06a0392290ce1b9198ff49e2bf6d1e9d4c2891b88e115b40e0329dda580aca2`, image
`sha256:559ac229adefe0f7e2d4e32f5222f26927b6bb8ba44b9db08e2544afebf41984`.
Final four slots idle. HEADs unchanged; QA/EVIDENCE stays untracked, Mini results
and Academy .build-check ignored. Main remains `f7af512` with its pre-existing
untracked QA/EVIDENCE only. No staging or application changes in Academy.

**SQL/browser not run because the prerequisite fails.** N2h's exact owned failed
SQL container remains stopped and retained; no normal DB/login teardown claimed.
Preview remains off. No Academy application/frontend/domain/evaluator/runner
change. N2g's 1,122 API/harness results retained, not rerun as unrelated work.

## Disposition

Do not promote this candidate or claim its failures fixed. The completed evidence
leaves a consequential contract choice unresolved: preserve intent/permission
separation and explicit field changes without sacrificing previously passing
query comprehension. **NEXT: one focused Astra High architecture review (N2l)**,
using these existing results without redoing the enterprise audit or rerunning
unchanged benchmarks. Assess a simpler proposal/field-transition contract and
bounded query-only change classification; retain the protected execution/state
boundary and every frozen expectation. Specify an implementable bounded repair
and acceptance gate; return to **Sol High** for implementation. No blind
prompt-only loop, automatic engine
replacement/training, host text parser, N3/history/domain expansion, or Azure.
Full linked SQL/15-turn browser/source/provenance/owned teardown remains required
after planning passes. Physical-device, production capacity, privacy/key-ring,
enterprise/P0 and release gates remain OPEN.
