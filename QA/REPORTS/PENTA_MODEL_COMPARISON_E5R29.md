# E5-R29 — actual guarded local model comparison

19 October2026. **COMPARISON EXECUTED; NO MODEL ACCEPTED.**
Owner approved E5-R28 and requested all models. Continued the existing CORE-01
program, preserving the owner local-cap/Azure deferral and exact safety gates.
Academy `codex/penta-search` HEAD `b1f8cb6f4209da7fbd14f8089fcb6792438684c4`;
Mini `codex/academy-language-contract` HEAD `2f8806967709a1a7ec29e5176e7c31968adbf6aa`.
Both dirty worktrees preserved; no publication, merge or Azure.

## Actual result — not a complete benchmark score

Trial `e5r29-af6927bab4b7`, recorded elapsed **466.82seconds**, bounded60minutes.
Four sequential text-model trials loaded and passed actual tokenizer-fit preflight
for all72cases, with unchanged2048context/512output/2CPU threads/one slot and
**30second** native deadline. One attempt per case, no retry or score repair.

| Model | Dispatched | Complete raw answers | Exact | Stopped outcome | Unrun of72 |
| --- | ---: | ---: | ---: | --- | ---: |
| Qwen3.5-2B Q4_0 |4|3|1|Two semantic mismatches, then fourth request deadline/quarantine|68|
| Ministral3-3B Q4_K_M |1|0|0|First request deadline/quarantine|71|
| Granite4.2-3B Q4_K_M |1|0|0|First request deadline/quarantine|71|
| Qwen3.5-4B Q4_K_M |1|0|0|First request deadline/quarantine|71|
| Qwen3.5-9B |0|0|—|Deferred above existing local placement ceiling|Not loaded|
| Gemma4-E4B |0|0|—|Deferred above existing local placement ceiling|Not loaded|
| Qwen3 embedding0.6B |0|0|—|Not a generative chat/planning model; separate retrieval qualification|Not run|

**Seven actual generation requests, three complete raw answers, one exact,
two semantic mismatches, four uncertain deadline outcomes and281unrun case/model
pairs.** These are not1/288 or1/72 benchmark accuracy scores. All7 attempted
cases belonged to the new fresh group; development/original operational/holdout
groups were not reached. Their existing results remain unchanged.

Native outcomes returned at30.046–30.051seconds without a complete response;
saved native logs show task cancellation at the bound. Each evaluator stopped
when its real native gate became quarantined; it did not reset the gate to run
more cases or reinterpret unrun cases as wrong answers. Runner exits0 in the four
model receipts mean the evaluator recorded its result, **not qualification PASS**.
The orchestration terminal session was unavailable on continuation; no observed
top-level exit code is claimed. Completed durable receipt, restored originals,
matching source/receipt hashes and absent owned resources were independently checked.

## Observed defects and limits

- 2B fresh F29-01 was exact in29.623s. F29-02 returned a whole `replace`, copying
  unchanged memo instead of the requested `refine`; remains an exact failure even
  where the final visible values happen to match. F29-03 stored the user's clear
  instruction as memo text instead of removing the field. It remains failed,
  not repaired in the host or credited as a canonical match.
- F29-04 then exceeded30s. Median of the four dispatched2B calls27.488s includes
  the censored timeout; it is not a successful-response-only latency statistic.
  Two other complete calls took25.353s and24.577s. There is little latency margin.
- Other models yielded no complete response under the bound: their semantic
  accuracy is **unmeasured**, not proven worse than2B or zero-percent accurate.
  Qwen4B native log shows it still processing the prompt during much of the bound.
  More RAM alone is not demonstrated to fix decoding speed or semantic errors.
- All72template/tokenizer checks fit on every loaded model: largest inputs
 2B908, Ministral967, Granite905 and4B908tokens; adding512 stays below2048.
  Context truncation is not the observed explanation. Actual native logs identify
  the runtime's two-thread CPU configuration, no GPU, and cancellation.
- New native services bind127.0.0.1 within `network=none`; the evaluator shares only
  the owned native network namespace. No public port/provider/API key, arbitrary
  egress or hosted fallback; native no-API-key warning is not production ingress
  acceptance. Synthetic fixtures only, no Academy SQL, tools or customer prompts.

## What changed

Mini only: additive offline evaluator/lifecycle scripts,12 independently authored
fresh synthetic cases and7 evaluator regressions. Existing engine/planner/decoder/
generation schema, defaults/weights/tier selection and host approval rules unchanged.
Fresh expectations frozen in the staged source BEFORE model output; never packed
into model messages. The14 exposed development cases remain development; previous
22holdout cases are retained regression, not newly unseen data. No training.

Each model planned12fresh +14development +24existing operational +22holdout=72.
The same existing full Proposal+Decision oracles are used for development/fresh/
holdout; the existing24 operational predicate is retained without silently making
it more or less strict. Token preflight completes before any generation.

One offline initial preflight failed on ambiguous Ministral artifact selection:
its vision-projector filename also ends in `.gguf`. New runner now rejects all
`mmproj` files and verifies exact text-model size/hash. This happened before the
trial, no inference or container mutation; no production source fix was needed.

## Safety, restoration and verification

- [x]354selected Python tests PASS (347retained +7new),0failed/errors/skipped.
  Final actual JUnit:12.42s, one inherited Starlette/httpx deprecation warning.
- [x] All4eligible text weights size/SHA256 verified. Above-cap weights were
  inventoried, not rehashed/loaded here; earlier model-library evidence retained.
- [x] Each model had3consecutive passing memory samples before load, without a
  lowered guard. Final-three minimums/required bytes:2B5128183808/3093922048;
  Ministral5462695936/4026071200; Granite5409173504/4123059744;
 4B5713674240/4619986080. Earlier below-guard samples remain recorded, not hidden.
- [x] Both exact verified-idle original PENTA containers temporarily stopped,
  recorded config identities restored, original running/stopped container inventory
  preserved. Final receipt says original_containers_restored=true,
  baseline_restored=true, restored native4idle slots. Fresh inspect confirms
  original IDs/images/running states; inference healthy.
- [x] Only exact labeled trial containers removed, logs retained first. No
  `e5r29-af6927bab4b7` owned resources remain. No prune/evidence deletion or other
  app termination, WSL reset, memory trimming or paid resources.
- [x]47frozen stage/current source pins and4evaluation receipt hashes match.
  No modifications after outputs to frozen fixtures/oracles/runtime/evaluator.
- [x] Diff checks PASS; no staged files/commit/push/main/Azure. Academy application
  source and normal API DLL unchanged; existing E5-R27 SQL/browser evidence retained
  **NOT rerun** for this model-only trial. No fresh host acceptance implied.

Final Academy local probes: web3000 onboarding and API5092 health were unavailable.
No claim that the developer Academy services remain healthy is made for this
checkpoint. The trial runner controls only the recorded PENTA containers and
did not stop/start Academy processes; the cause of their unavailability has not
been diagnosed. Their startup is a separate local-app step, not model acceptance.

## Durable evidence / pins

Mini `operations/evidence/e5r29-af6927bab4b7/`: `frozen-plan.json`, `trial.json`,
47file staged source, each model's config/evaluation and owned-container logs.
Raw synthetic model outputs are retained in per-model `evaluation.json`.
Academy `QA/EVIDENCE/penta-model-comparison-e5r29/python-final.xml` remains local.

| File | SHA256 |
| --- | --- |
| Mini trial.json | `05791DC7A1BAFE2FB7FA5816698DE04010DF072F244921A3F310F897AD5C35EF` |
| Mini transitions-fresh-e5r29.json | `54DC7769F834D5BDFC5F52EA6D0CC784EBBC864899818236BDDBBFCD00F4114A` |
| Mini evaluate_transitions_e5r29.py | `DC795005ACB26440CD94C4E9D9B8F7E6F51CFD41DCFCDFABA15E217CAB70DFC0` |
| Mini run_transitions_e5r29.py | `F196C50DAD808FD7D602484155EDA4B699FB87AB08E8C87EADC0FC477B168604` |
| Mini test_transitions_trial_e5r29.py | `CC85E231A769545BC7831696AC83A42AFA2B53F89C68F822687E80D0851E4853` |
|2B evaluation.json|`e685ce30da4224e847181203f6ed609c7951f56b80038b7f5689d4ddbaa3abac`|
|Ministral evaluation.json|`eb8e3f967584209625594be4107b07efde9ed09fdcb24aec1c96ec3946d28638`|
|Granite evaluation.json|`ae1dbf7c27705573adf73a6c56ac6dd502e9caff73a28ed28fbb27b4548c33b6`|
|4B evaluation.json|`13c38805ee9b99992f475d7c2d9236d4dbec15d5554f48176e546c72983c5f61`|
|Academy python-final.xml|`511139DBC98BA0226A77DD1A93FDD8CAD0A5FE3C58CC217C82E4FE0FA84D669C`|

Commands from Mini root with `PYTHONPATH=src`, `PYTHONDONTWRITEBYTECODE=1`:
`python operations/tools/run_transitions_e5r29.py` (preflight), then
`python operations/tools/run_transitions_e5r29.py --run` once. Selected pytest list
is E5-R18's nine suites plus `tests/test_transitions_trial_e5r29.py`,
`-q -p no:cacheprovider --junitxml=D:/AcademyDesk-codex-p0/QA/EVIDENCE/penta-model-comparison-e5r29/python-final.xml`.
Temporary shell environment restored afterwards; no serving config changed.

## Next decision / no automatic rerun

CORE-01 remains **BLOCKED / NOT ACCEPTED**. Changed general instructions did not
resolve the observed2B edit semantics and this CPU placement missed the30s bound
for all four models. Do not rerun identical settings, raise the deadline, use raw
model prose as success, patch the host with benchmark-specific answers, promote a
larger model or claim the90% goal/production gate from these partial results.

Next Sol High work separates **semantic improvement** (general retained-field/
explicit clear reasoning on development-only cases, no frozen-answer training)
from **runtime performance** (reviewed local acceleration/serving proposal with
measured resources and exact source/profile pins). Any new model/profile/compute
trial needs its own bounded decision. Existing private Azure evaluation remains
deferred; no VM/cost or training approval inferred from “run all models.”
The now-exposed12fresh cases become regression controls, not future unseen holdout.

Academy integration stays unavailable for real-model writes. Durable history,
broader families/roles, Manual parity, providers/privacy/devices/enterprise/security
and exact pre-Azure gates remain OPEN. BUG-DATA-0001 stays OPEN. This concludes
the approved bounded local comparison, not the product or enterprise test program.
