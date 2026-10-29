# E5-R57 — controlled 3B intent comparison

20 October 2026. Route: Sol High. This continues the existing program; it is not a new testing plan.

Historical R57 result/preparation below. The corrected run has now finished:
[E58 final result](PENTA_3B_INTENT_RESULT_E5R58.md),11/12classifier/NOT ACCEPTED,
originals restored/noactive trial. Its final next-path supersedes E58prepared notices here.

## Final result — stopped by verifier; no classifier score

One approved local run `e5r57-1fbe9d2228fa` completed in 82.25 seconds.
Result `FAILED_TRIAL_RUNTIME`: the initial exact-embedded-template comparator rejected
the runtime's own compatibility transformation before the evaluator started.
**Zero classifier requests, no accuracy score, NOT ACCEPTED.** Do not report this as 0/12 model quality.
The model did load; logs show 27/27 GPU layers and actual batch/microbatch 128/128.
Host RAM admission passed three consecutive samples (minimum 5372608512 bytes).
The GPU reserve gate passed, but live process CUDA-map proof was not reached; do not claim it passed.

The original 25-container/config/host/mount/state inventory was restored exactly;
both original services healthy with four idle slots. All 55 frozen staged source files
remain unchanged. Only this run's owned temporary containers were removed; original
containers/volumes/evidence were preserved. No active trial remains.
Trial receipt SHA256: `bb9ba665c45e38c35736c6ebc97738f49e5c69cc062548fe5a7b51bb190f6f6b`.
Original evidence: `D:/PENTA AI Models/operations/evidence/e5r57-1fbe9d2228fa/trial.json`.

## Completed preparation

- [x] Exact downloaded Ministral-3-3B-Instruct-2512 Q4_K_M file rehashed:
  `9ed150d4367e68df0ac8e1540f6ddc65b42d0ee26378329d1ecbca60f93fc5f8`, 2147023008 bytes.
- [x] Bounded GGUF metadata inspection: architecture `mistral3`, 26 transformer blocks.
  Upstream config calls its text model `ministral3`; corrected preflight assumption before any load.
  Two read-only preparation checks failed on that assumption; no inference or pause occurred in them.
- [x] Embedded template hash `d28d7df94f0fd7e8d0075a22c473333d6e7dd2bc4c36c83e8b975300a0fb94bc`;
  publisher template hash `0701cfbdc2b7d44fdbad104dff604faee4b0543e8247624568777fe465746f9b`.
  They differ: use the hashed artifact's embedded template, not a Qwen template or silent override.
  The initial `/props` exact-embedded comparison failed. See the source-proven correction below;
  no publisher/Qwen template override is introduced.
- [x] 235 focused tests pass in 4.81 seconds; no skips. One existing Starlette/httpx deprecation warning.
  Includes 20 new offline guard/launch/metadata checks and the retained 215 related tests (not additive reruns).
- [x] Existing R56 prompt, cases, schema, evaluator, exact scoring and source pins unchanged.

## Execution boundary

Single model; verified existing native sm75 binary, no rebuild. Original lifecycle reused:
host RAM admission is weight + 768 MiB process + 1 GiB host reserve = 4026071200 bytes,
three consecutive acceptable samples within 45 seconds. GPU admission is weight + 1536 MiB
= 3583.560699 MiB free on the unchanged 4096 MiB Quadro T1000. No relaxed guard or CPU fallback.
Same 2048 context, one slot, four threads, batch/microbatch 128, 64 output tokens,
30-second request deadline, 150-second load deadline, 480-second evaluation cap and 900-second lifecycle budget.
Exact read-only launch, no external network, full GPU offload, actual `/proc/1/maps` candidate library,
actual template and batch receipts must pass before generation. No mmproj/image model is loaded.

Twelve now-exposed R56 synthetic English evaluation cases: intent classification only.
This is a controlled comparison with the prior 2B score of 5/12, not fresh blinded acceptance,
workflow qualification, multilingual evidence or training data. No answer-based tuning or retries.
Even 12/12 here would not qualify operational plans, host authorization or production deployment.

## Remaining

- [x] Read actual load and restoration receipts; classifier did not run.
- [x] Diagnose verifier mismatch offline from saved evidence and pinned runtime source.
- [x] Prepare distinct E5-R58 exact-source verifier and read-only preflight.
- [ ] Execute one separately bounded corrected E5-R58 comparison; NOT RUN this continuation.
- [ ] Select the next reasoning/qualification step from evidence, without promoting a tier/default.
- [ ] Full-plan, fresh safety, linked host/domain/role/Manual/browser/device/enterprise and pre-Azure gates.

## E5-R58 preparation — completed offline, no model reload

The source archive at revision `11fe02151f79c41d0d4af7da708755d73b9c0da6` was rehashed
to `b6dfbe4469265b84dbb9257c8e68fa47c41d479908892720ccab470ff5958535`.
`common/chat.cpp:801` applies a specific Mistral template compatibility substitution:
the assistant empty-content/tool-call guard becomes `{%- if false %}`. The native
`common/jinja/lexer.cpp:56` removes exactly one trailing LF. Saved `/props` differs
from the embedded template by exactly those two transformations, nothing else.
This is a runtime formatting/compatibility path, not an Academy permission or policy change.

Reverified source pins:

- `common/chat.cpp`: `5bc92318d3b926c9b4491bf3a6ac7770843dadee218e84cbd5a5f8fdb1011840`
- `common/jinja/lexer.cpp`: `3d6774bc2dac477e10ddbee69eff403df36f5925c8512ccf8a249bde5c26dbd7`
- Exact transformed runtime template: `8a292f27fb149a8699d1f56623407437c6212897b26c96e58c877b29545f6142`

`operations/tools/run_intent_3b_e5r58.py` verifies the original embedded hash, exact
source/archive hashes, one exact substitution, one final LF removal, and exact resulting
template/hash. It rejects extra whitespace, new instructions, different control tokens,
unknown source/model templates and other changes. No fuzzy normalization or model-output repair.
R57's default comparator remains exact; the separate E58 wrapper explicitly selects the
reviewed verifier and distinct run prefix. All other launch/resource/security/scoring guards remain.

37 focused verifier/launch tests pass; expanded related suite **252 PASS in 9.83s**,
no skips, one existing Starlette/httpx deprecation warning. Counts overlap; do not add them.
JUnit: `D:/PENTA-Models/operations/e5r58-checks/regression.xml`.
Read-only E58 preflight passed: artifact rehashed, same 12 R56 cases/prompt/evaluator;
zero new model loads/generations. It does not establish actual live E58 CUDA-map/template/
token/schema/semantic acceptance. No automatic retry has run.

Next route: **Sol High**, one bounded E58 comparison under the same original limits,
followed by actual receipts/restoration and a reasoning decision. Do not rebuild, tune on
answers, relax the 4B GPU guard, change defaults, or open Azure to get a passing result.

No serving settings, model tiers, application/domain code, database, main, Git stage/commit/push
or Azure changes. BUG-DATA-0001 remains OPEN. Two-stage operational provider remains unimplemented.
